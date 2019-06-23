using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using Wist.Blockchain.Core.DataModel.UtxoConfidential;
using Wist.Blockchain.Core.Enums;
using Wist.Client.Common.Interfaces;
using Wist.Client.DataModel.Enums;
using Wist.Client.DataModel.Services;
using Wist.Core.Cryptography;
using Wist.Core.Models;
using Wist.Core.ExtensionMethods;
using Wist.Crypto.ConfidentialAssets;
using Wist.Server.Portal.Dtos;
using Wist.Server.Portal.Hubs;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Wist.Core.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Wist.Blockchain.Core.DataModel.Transactional;
using Wist.Client.DataModel.Model;
using Wist.Blockchain.Core.DataModel.UtxoConfidential.Internal;
using Wist.Blockchain.Core.Parsers;
using Wist.Core.Identity;
using System.Threading.Tasks.Dataflow;
using System.Globalization;
using Flurl.Http;
using Wist.Server.Portal.Dtos.ServiceProvider;
using Wist.Core.Logging;

namespace Wist.Server.Portal.Services
{
	public class ServiceProviderUpdater
	{
		private readonly ulong _accountId;
        private readonly ILogger _logger;
		private readonly IStateClientCryptoService _clientCryptoService;
		private readonly IAssetsService _assetsService;
		private readonly IDataAccessService _dataAccessService;
		private readonly IIdentityAttributesService _identityAttributesService;
		private readonly IBlockParsersRepositoriesRepository _blockParsersRepositoriesRepository;
		private readonly IGatewayService _gatewayService;
        private readonly IStateTransactionsService _transactionsService;
        private readonly IHubContext<IdentitiesHub> _idenitiesHubContext;
		private readonly IAppConfig _appConfig;
		private readonly Dictionary<byte[], string> _keyImageToSessonKeyMap = new Dictionary<byte[], string>(new Byte32EqualityComparer());

		public ServiceProviderUpdater(ulong accountId, IStateClientCryptoService clientCryptoService, IAssetsService assetsService, IDataAccessService dataAccessService, IIdentityAttributesService identityAttributesService, IBlockParsersRepositoriesRepository blockParsersRepositoriesRepository, IGatewayService gatewayService, IStateTransactionsService transactionsService, IHubContext<IdentitiesHub> idenitiesHubContext, IAppConfig appConfig, ILoggerService loggerService)
		{
			_accountId = accountId;
			_clientCryptoService = clientCryptoService;
			_assetsService = assetsService;
			_dataAccessService = dataAccessService;
			_identityAttributesService = identityAttributesService;
			_blockParsersRepositoriesRepository = blockParsersRepositoriesRepository;
			_gatewayService = gatewayService;
            _transactionsService = transactionsService;
            _idenitiesHubContext = idenitiesHubContext;
			_appConfig = appConfig;
            _logger = loggerService.GetLogger(nameof(ServiceProviderUpdater));

			PipeIn = new ActionBlock<PacketBase>(p => 
			{
				if(p is DocumentSignRecord documentSignRecord)
				{
					ProcessDocumentSignRecord(documentSignRecord);
				}

				if (p is DocumentRecord documentRecord)
				{
					ProcessDocumentRecord(documentRecord);
				}

				if (p is DocumentSignRequest documentSignRequest)
				{
					ProcessDocumentSignRequest(documentSignRequest);
				}

                if(p is EmployeeRegistrationRequest employeeRegistrationRequest)
                {
                    ProcessEmployeeRegistrationRequest(employeeRegistrationRequest);
                }

				if (p is OnboardingRequest packet)
				{
					ProcessOnboarding(packet);
				}

				if (p is TransitionAuthenticationProofs transitionAuthentication)
				{
					ProcessAuthentication(transitionAuthentication);
				}

				if (p is TransitionCompromisedProofs compromisedProofs)
				{
					ProcessCompromisedProofs(compromisedProofs);
				}

				if (p is TransferAsset transferAsset)
				{
					ProcessTransferAsset(transferAsset);
				}
			});
		}

		public ITargetBlock<PacketBase> PipeIn { get; }

		private void ProcessDocumentSignRecord(DocumentSignRecord packet)
		{
			if(_dataAccessService.UpdateSpDocumentSignature(_accountId, packet.DocumentHash.ToHexString(), packet.RecordHeight, packet.BlockHeight, packet.RawData.ToArray()))
            {
                _logger.Info($"Document with hash {packet.DocumentHash.ToHexString()} was signed successfully");
            }
            else
            {
                _logger.Error($"Failed to update raw signature record of Document with hash {packet.DocumentHash.ToHexString()}");
            }
        }

		private void ProcessDocumentRecord(DocumentRecord packet)
		{
			_dataAccessService.UpdateSpDocumentChangeRecord(_accountId, packet.DocumentHash.ToHexString(), packet.BlockHeight);
		}

		private void ProcessDocumentSignRequest(DocumentSignRequest packet)
		{
			_clientCryptoService.DecodeEcdhTuple(packet.EcdhTuple, packet.TransactionPublicKey, out byte[] groupNameBlindingFactor, out byte[] documentHash, out byte[] issuer, out byte[] payload);
			string sessionKey = payload.ToHexString();
			SpDocument spDocument = _dataAccessService.GetSpDocument(_accountId, documentHash.ToHexString());

			if(spDocument == null)
			{
				_idenitiesHubContext.Clients.Group(sessionKey).SendAsync("PushDocumentNotFound");
			}

			bool isEligibilityCorrect = CheckEligibilityProofs(packet.AssetCommitment, packet.EligibilityProof, issuer);

			if (!isEligibilityCorrect)
			{
				_idenitiesHubContext.Clients.Group(sessionKey).SendAsync("PushDocumentSignIncorrect", new { Code = 2, Message = "Eligibility proofs were wrong" }).Wait();
				return;
			}

			if (!ConfidentialAssetsHelper.VerifySurjectionProof(packet.SignerGroupRelationProof, packet.AssetCommitment, documentHash, BitConverter.GetBytes(spDocument.LastChangeRecordHeight)))
			{
				_idenitiesHubContext.Clients.Group(sessionKey).SendAsync("PushDocumentSignIncorrect", new { Code = 2, Message = "Signer group relation proofs were wrong" }).Wait();
				return;
			}

			SurjectionProof signatureGroupProof = null;
            string groupIssuer = null;
			foreach (var allowedSigner in spDocument.AllowedSigners)
			{
				byte[] groupAssetId = _assetsService.GenerateAssetId(AttributeType.EmployeeGroup, allowedSigner.GroupIssuer + allowedSigner.GroupName);
				byte[] expectedGroupCommitment = ConfidentialAssetsHelper.GetAssetCommitment(groupAssetId, groupNameBlindingFactor);
				if(packet.AllowedGroupCommitment.Equals32(expectedGroupCommitment))
				{
					byte[] groupCommitment = _gatewayService.GetEmployeeRecordGroup(allowedSigner.GroupIssuer.HexStringToByteArray(), packet.SignerGroupRelationProof.AssetCommitments[0]);
					if (groupCommitment != null && ConfidentialAssetsHelper.VerifySurjectionProof(packet.AllowedGroupNameSurjectionProof, packet.AllowedGroupCommitment))
					{
						byte[] diffBF = ConfidentialAssetsHelper.GetDifferentialBlindingFactor(groupNameBlindingFactor, allowedSigner.BlindingFactor.HexStringToByteArray());
						byte[][] commitments = spDocument.AllowedSigners.Select(s => s.GroupCommitment.HexStringToByteArray()).ToArray();
						byte[] allowedGroupCommitment = allowedSigner.GroupCommitment.HexStringToByteArray();
						int index = 0;

						for (; index < commitments.Length; index++)
						{
							if (commitments[index].Equals32(allowedGroupCommitment))
							{
								break;
							}
						}

						signatureGroupProof = ConfidentialAssetsHelper.CreateSurjectionProof(packet.AllowedGroupCommitment, commitments, index, diffBF);
						groupIssuer = allowedSigner.GroupIssuer;
						break;
					}
				}
			}

			if(signatureGroupProof == null)
			{
				_idenitiesHubContext.Clients.Group(sessionKey).SendAsync("PushDocumentSignIncorrect", new { Code = 2, Message = "Signer group relation proofs were wrong" }).Wait();
				return;
			}

			_transactionsService.IssueDocumentSignRecord(documentHash, spDocument.LastChangeRecordHeight, packet.AssetCommitment, packet.SignerGroupRelationProof, packet.AllowedGroupCommitment, groupIssuer.HexStringToByteArray(), packet.AllowedGroupNameSurjectionProof, signatureGroupProof, out ulong signatureRecordHeight);
            ulong signatureId = _dataAccessService.AddSpDocumentSignature(_accountId, spDocument.SpDocumentId, spDocument.LastChangeRecordHeight, signatureRecordHeight);

            _idenitiesHubContext.Clients.Group(_accountId.ToString(CultureInfo.InvariantCulture))
				.SendAsync("PushDocumentSignature",
				new DocumentSignatureDto
				{
					DocumentId = spDocument.SpDocumentId,
                    DocumentHash = spDocument.Hash,
					DocumentRecordHeight = spDocument.LastChangeRecordHeight,
                    SignatureRecordHeight = signatureRecordHeight
				});

			_idenitiesHubContext.Clients.Group(sessionKey)
				.SendAsync("PushDocumentSignature",
				new DocumentSignatureDto
				{
					DocumentId = spDocument.SpDocumentId,
                    DocumentHash = spDocument.Hash,
                    DocumentRecordHeight = spDocument.LastChangeRecordHeight,
                    SignatureRecordHeight = signatureRecordHeight
                });
		}

		private void ProcessEmployeeRegistrationRequest(EmployeeRegistrationRequest packet)
        {
            _clientCryptoService.DecodeEcdhTuple(packet.EcdhTuple, packet.TransactionPublicKey, out byte[] blindingFactor, out byte[] assetId, out byte[] issuer, out byte[] payload);
            string sessionKey = payload.ToHexString();
            List<SpEmployee> spEmployees = _dataAccessService.GetSpEmployees(_accountId, assetId.ToHexString());

            bool categoryFound = false;
            bool alreadyRegistered = false;
            bool categoryProofsCorrect = false;
            ulong relationId = 0;

            foreach (SpEmployee item in spEmployees)
            {
                if(item.SpEmployeeGroup != null)
                {
                    categoryFound = true;
                }
                else
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(item.RegistrationCommitment) && item.RegistrationCommitment.Equals(packet.AssetCommitment.ToHexString()))
                {
                    alreadyRegistered = true;
                    break;
                }

                byte[] groupAssetId = _assetsService.GenerateAssetId(AttributeType.EmployeeGroup, _clientCryptoService.PublicKeys[0].ArraySegment.Array.ToHexString() + item.SpEmployeeGroup.GroupName);
                if (ConfidentialAssetsHelper.VerifyIssuanceSurjectionProof(packet.GroupSurjectionProof, packet.GroupCommitment, new byte[][] { groupAssetId }))
                {
                    categoryProofsCorrect = true;
                    relationId = item.SpEmployeeId;
                    break;
                }
            }

            if (!categoryFound)
            {
                _idenitiesHubContext.Clients.Group(sessionKey).SendAsync("PushEmployeeNotRegistered");
                return;
            }

            if(alreadyRegistered)
            {
                _idenitiesHubContext.Clients.Group(sessionKey).SendAsync("PushRelationAlreadyRegistered");
                return;
            }

            if (!categoryProofsCorrect)
            {
                _idenitiesHubContext.Clients.Group(sessionKey).SendAsync("PushEmployeeIncorrectRegistration", new { Code = 3, Message = "Group proofs were wrong" });
                return;
            }

            AttributeType attributeType = _assetsService.GetAttributeType(assetId);

            bool isEligibilityCorrect = CheckEligibilityProofs(packet.AssetCommitment, packet.EligibilityProof, issuer);

            if (!isEligibilityCorrect)
            {
                _idenitiesHubContext.Clients.Group(sessionKey).SendAsync("PushEmployeeIncorrectRegistration", new { Code = 2, Message = "Eligibility proofs were wrong" }).Wait();
                return;
            }

            IEnumerable<SpIdenitityValidation> spIdenitityValidations = _dataAccessService.GetSpIdenitityValidations(_accountId);
            if (!CheckSpIdentityValidations(packet.AssetCommitment, packet.AssociatedProofs, spIdenitityValidations, sessionKey))
            {
                return;
            }

            _dataAccessService.SetSpEmployeeRegistrationCommitment(_accountId, relationId, packet.AssetCommitment.ToHexString());
            _transactionsService.IssueEmployeeRecord(packet.AssetCommitment, packet.GroupCommitment);
            _idenitiesHubContext.Clients.Group(_accountId.ToString(CultureInfo.InvariantCulture)).SendAsync("PushEmployeeUpdate", new EmployeeDto { AssetId = assetId.ToHexString(), RegistrationCommitment = packet.AssetCommitment.ToHexString() });
            _idenitiesHubContext.Clients.Group(sessionKey).SendAsync("PushEmployeeRegistration", new { Commitment = packet.AssetCommitment.ToHexString() });
        }

        private void ProcessOnboarding(OnboardingRequest packet)
        {
            _clientCryptoService.DecodeEcdhTuple(packet.EcdhTuple, packet.TransactionPublicKey, out byte[] blindingFactor, out byte[] assetId, out byte[] issuer, out byte[] payload);
            string sessionKey = payload.ToHexString();

            if (_dataAccessService.GetServiceProviderRegistrationId(_accountId, packet.AssetCommitment, out ulong registrationId))
            {
                _idenitiesHubContext.Clients.Group(sessionKey).SendAsync("PushUserAlreadyRegistered", new ServiceProviderRegistrationDto { ServiceProviderRegistrationId = registrationId.ToString(CultureInfo.InvariantCulture), Commitment = packet.AssetCommitment.ToHexString() });
            }
            else
            {
                AttributeType attributeType = _assetsService.GetAttributeType(assetId);

                bool isEligibilityCorrect = CheckEligibilityProofs(packet.AssetCommitment, packet.EligibilityProof, issuer);

                if (!isEligibilityCorrect)
                {
                    _idenitiesHubContext.Clients.Group(sessionKey).SendAsync("PushSpAuthorizationFailed", new { Code = 2, Message = "Eligibility proofs were wrong" }).Wait();
                    return;
                }

                IEnumerable<SpIdenitityValidation> spIdenitityValidations = _dataAccessService.GetSpIdenitityValidations(_accountId);
                if (!CheckSpIdentityValidations(packet.AssetCommitment, packet.AssociatedProofs, spIdenitityValidations, sessionKey))
                {
                    return;
                }

                ulong id = _dataAccessService.AddServiceProviderRegistration(_accountId, packet.AssetCommitment);
                _idenitiesHubContext.Clients.Group(_accountId.ToString(CultureInfo.InvariantCulture)).SendAsync("PushRegistration", new ServiceProviderRegistrationDto { ServiceProviderRegistrationId = id.ToString(CultureInfo.InvariantCulture), Commitment = packet.AssetCommitment.ToHexString() });
                _idenitiesHubContext.Clients.Group(sessionKey).SendAsync("PushUserRegistration", new ServiceProviderRegistrationDto { ServiceProviderRegistrationId = id.ToString(CultureInfo.InvariantCulture), Commitment = packet.AssetCommitment.ToHexString() });
            }
        }

        private void ProcessTransferAsset(TransferAsset transferAsset)
        {
            _clientCryptoService.DecodeEcdhTuple(transferAsset.TransferredAsset.EcdhTuple, null, out byte[] blindingFactor, out byte[] assetId);
            AttributeType attributeType = _assetsService.GetAttributeType(assetId);
            _dataAccessService.StoreSpAttribute(_accountId, attributeType, assetId, transferAsset.Signer.Value.ToHexString(), blindingFactor, transferAsset.TransferredAsset.AssetCommitment, transferAsset.SurjectionProof.AssetCommitments[0]);

            _idenitiesHubContext.Clients.Group(_accountId.ToString(CultureInfo.InvariantCulture)).SendAsync("PushAttribute", new SpAttributeDto { AttributeType = attributeType.ToString(), Source = transferAsset.Signer.ArraySegment.Array.ToHexString(), AssetId = assetId.ToHexString(), OriginalBlindingFactor = blindingFactor.ToHexString(), OriginalCommitment = transferAsset.TransferredAsset.AssetCommitment.ToHexString(), IssuingCommitment = transferAsset.SurjectionProof.AssetCommitments[0].ToHexString(), Validated = false, IsOverriden = false });
        }

        private void ProcessCompromisedProofs(TransitionCompromisedProofs compromisedProofs)
        {
            if (_keyImageToSessonKeyMap.ContainsKey(compromisedProofs.CompromisedKeyImage))
            {
                _idenitiesHubContext.Clients.Group(_keyImageToSessonKeyMap[compromisedProofs.CompromisedKeyImage]).SendAsync("PushAuthorizationCompromised");
            }
        }

        private void ProcessAuthentication(TransitionAuthenticationProofs transitionAuthentication)
        {
            _clientCryptoService.DecodeEcdhTuple(transitionAuthentication.EncodedPayload, transitionAuthentication.TransactionPublicKey, out byte[] bf, out byte[] assetId, out byte[] issuer, out byte[] payload);
            string sessionKey = payload.ToHexString();

            bool isAuthenticationProofValid = ConfidentialAssetsHelper.VerifySurjectionProof(transitionAuthentication.AuthenticationProof, transitionAuthentication.AssetCommitment);

            if (isAuthenticationProofValid && _dataAccessService.GetServiceProviderRegistrationId(_accountId, transitionAuthentication.AuthenticationProof.AssetCommitments[0], out ulong id))
            {
                bool isEligibilityCorrect = CheckEligibilityProofs(transitionAuthentication.AssetCommitment, transitionAuthentication.EligibilityProof, issuer);

                if (isEligibilityCorrect)
                {
                    ProceedCorrectAuthentication(transitionAuthentication, sessionKey);
                }
                else
                {
                    _idenitiesHubContext.Clients.Group(sessionKey).SendAsync("PushSpAuthorizationFailed", new { Code = 2, Message = "Eligibility proofs were wrong" });
                }
            }
            else
            {
                _idenitiesHubContext.Clients.Group(sessionKey).SendAsync("PushSpAuthorizationFailed", new { Code = 1, Message = "User is not registered" });
            }
        }

        private void ProceedCorrectAuthentication(TransitionAuthenticationProofs transitionAuthentication, string sessionKey)
        {
            byte[] keyImage = transitionAuthentication.KeyImage.Value.ToArray();
            if (!_keyImageToSessonKeyMap.ContainsKey(keyImage))
            {
                _keyImageToSessonKeyMap.Add(keyImage, sessionKey);
            }

            //TODO: here goes logic of successfull authentication
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appConfig.GetString("appSettings:secret"));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                                new Claim(ClaimTypes.Name, sessionKey),
                                new Claim(ClaimTypes.Role, "spuser")
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            _idenitiesHubContext.Clients.Group(sessionKey).SendAsync("PushSpAuthorizationSucceeded", new { Token = tokenString });
        }

        private bool CheckSpIdentityValidations(byte[] commitment, AssociatedProofs[] associatedProofsList, IEnumerable<SpIdenitityValidation> spIdenitityValidations, string sessionKey)
        {
            if (spIdenitityValidations != null && spIdenitityValidations.Count() > 0)
            {
                IBlockParsersRepository transactionalBlockParsersRepo = _blockParsersRepositoriesRepository.GetBlockParsersRepository(PacketType.Transactional);
                IBlockParser issueAssociatedAttrBlockParser = transactionalBlockParsersRepo.GetInstance(BlockTypes.Transaction_IssueAssociatedBlindedAsset);

                if (associatedProofsList == null)
                {
                    _idenitiesHubContext.Clients.Group(sessionKey).SendAsync("PushSpAuthorizationFailed", new { Code = 3, Message = "Validation proofs were not provided" }).Wait();
                    return false;
                }

                foreach (var spIdentityValidation in spIdenitityValidations)
                {
                    if (!CheckSpIdentityValidation(commitment, associatedProofsList, spIdentityValidation, sessionKey))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool CheckSpIdentityValidation(byte[] commitment, AssociatedProofs[] associatedProofsList, SpIdenitityValidation spIdentityValidation, string sessionKey)
        {
            byte[] groupId = _identityAttributesService.GetGroupId(spIdentityValidation.AttributeType);

            AssociatedProofs associatedProofs = associatedProofsList.FirstOrDefault(P => P.AssociatedAssetGroupId.Equals32(groupId));
            if (associatedProofs == null)
            {
                _idenitiesHubContext.Clients.Group(sessionKey).SendAsync("PushSpAuthorizationFailed", new { Code = 3, Message = "Validation proofs were not complete" }).Wait();
                return false;
            }

            bool associatedProofValid;

            if (associatedProofs is AssociatedAssetProofs associatedAssetProofs)
            {
                associatedProofValid = ConfidentialAssetsHelper.VerifySurjectionProof(associatedAssetProofs.AssociationProofs, associatedAssetProofs.AssociatedAssetCommitment);
            }
            else
            {
                associatedProofValid = ConfidentialAssetsHelper.VerifySurjectionProof(associatedProofs.AssociationProofs, commitment);
            }

            bool rootProofValid = ConfidentialAssetsHelper.VerifySurjectionProof(associatedProofs.RootProofs, commitment);

            if (!rootProofValid || !associatedProofValid)
            {
                _idenitiesHubContext.Clients.Group(sessionKey).SendAsync("PushSpAuthorizationFailed", new { Code = 3, Message = "Validation proofs were not correct" }).Wait();
                return false;
            }

			//TODO: !!! adjust checking either against Gateway or against local database
			bool found = true; // associatedProofs.AssociationProofs.AssetCommitments.Any(a => associatedProofs.RootProofs.AssetCommitments.Any(r => _dataAccessService.CheckAssociatedAtributeExist(null, a, r)));

            if (!found)
            {
                _idenitiesHubContext.Clients.Group(sessionKey).SendAsync("PushSpAuthorizationFailed", new { Code = 3, Message = "Validation proofs were not correct" }).Wait();
                return false;
            }

            return true;
        }

        private bool CheckEligibilityProofs(byte[] assetCommitment, SurjectionProof eligibilityProofs, byte[] issuer)
		{
			bool isCommitmentCorrect = ConfidentialAssetsHelper.VerifySurjectionProof(eligibilityProofs, assetCommitment);

			if (!isCommitmentCorrect)
			{
				return false;
			}

			foreach (byte[] commitment in eligibilityProofs.AssetCommitments)
			{
				//TODO: make bulk check!
				if(!_gatewayService.IsRootAttributeValid(issuer, commitment))
				{
					return false;
				}
			}

			return true;
		}
	}
}
