using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Wist.Client.Common.Interfaces;
using Wist.Client.DataModel.Services;
using Wist.Server.Portal.Services;
using Wist.Core.ExtensionMethods;
using Wist.Client.DataModel.Enums;
using Wist.Server.Portal.Dtos;
using Wist.Core.Configuration;
using Wist.Server.Portal.Configuration;
using Wist.Crypto.ConfidentialAssets;
using System.Text;
using Wist.Client.Common.Identities;
using Wist.Client.Common.Interfaces.Inputs;
using System.Globalization;
using Flurl;
using Flurl.Http;
using System.Net.Http;
using Unity.Interception.Utilities;
using Wist.Client.DataModel.Model;
using Wist.Blockchain.Core.DataModel;
using System.Collections.Specialized;
using Wist.Client.Common.Interfaces.Outputs;
using Wist.Server.Portal.Dtos.User;
using Microsoft.AspNetCore.SignalR;
using Wist.Server.Portal.Hubs;

namespace Wist.Server.Portal.Controllers
{
    [Authorize(Roles = "puser")]
	[ApiController]
	[Route("[controller]")]
	public class UserController : ControllerBase
	{
        private readonly IDocumentSignatureVerifier _documentSignatureVerifier;
        private readonly IAccountsService _accountsService;
        private readonly IExecutionContextManager _executionContextManager;
		private readonly IAssetsService _assetsService;
		private readonly IIdentityAttributesService _identityAttributesService;
		private readonly IDataAccessService _dataAccessService;
        private readonly IGatewayService _gatewayService;
        private readonly IHubContext<IdentitiesHub> _idenitiesHubContext;
        private readonly IPortalConfiguration _portalConfiguration;

        public UserController(IDocumentSignatureVerifier documentSignatureVerifier, IAccountsService accountsService, IExecutionContextManager executionContextManager, IAssetsService assetsService, IIdentityAttributesService identityAttributesService, IDataAccessService externalDataAccessService, IGatewayService gatewayService, IConfigurationService configurationService, IHubContext<IdentitiesHub> idenitiesHubContext)
		{
            _documentSignatureVerifier = documentSignatureVerifier;
            _accountsService = accountsService;
            _executionContextManager = executionContextManager;
			_assetsService = assetsService;
			_identityAttributesService = identityAttributesService;
			_dataAccessService = externalDataAccessService;
            _gatewayService = gatewayService;
            _idenitiesHubContext = idenitiesHubContext;
            _portalConfiguration = configurationService.Get<IPortalConfiguration>();
        }

		[HttpPost("ValidateAttribute")]
		public IActionResult ValidateAttribute([FromBody] UserAttributeDto userAttribute)
		{
			ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

			byte[] assetIdExpected = userAttribute.AssetId.HexStringToByteArray();
			AttributeType attributeType = Enum.Parse<AttributeType>(userAttribute.AttributeType);

			byte[] assetIdActual = _assetsService.GenerateAssetId(attributeType, userAttribute.Content);

			userAttribute.Validated = assetIdExpected.Equals32(assetIdActual);

			if(!userAttribute.Validated)
			{
				userAttribute.Content = null;
			}

			_dataAccessService.UpdateUserAttributeContent(accountId, userAttribute.OriginalCommitment.HexStringToByteArray(), userAttribute.Content);

			return Ok(userAttribute);
		}

		[HttpGet("GetUserAttributes")]
		public IActionResult GetUserAttributes()
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

            IEnumerable<UserRootAttribute> userAttributes = _dataAccessService.GetUserAttributes(accountId).Where(u => !u.IsOverriden);

            IEnumerable<UserAttributeDto> attributes = userAttributes.Select(c => GetUserAttributeDto(c));

            return Ok(attributes);
        }

        private static UserAttributeDto GetUserAttributeDto(UserRootAttribute c)
        {
            return new UserAttributeDto
            {
                AttributeType = Enum.GetName(typeof(AttributeType), c.AttributeType),
                Content = c.Content,
                OriginalBlindingFactor = c.OriginalBlindingFactor.ToHexString(),
                OriginalCommitment = c.OriginalCommitment.ToHexString(),
                OriginatingCommitment = c.IssuanceCommitment.ToHexString(),
                LastBlindingFactor = c.LastBlindingFactor.ToHexString(),
                LastCommitment = c.LastCommitment.ToHexString(),
                AssetId = c.AssetId.ToHexString(),
                Validated = !string.IsNullOrEmpty(c.Content),
                Source = c.Source,
                LastDestinationKey = c.LastDestinationKey.ToHexString(),
                LastTransactionKey = c.LastTransactionKey.ToHexString(),
                IsOverriden = c.IsOverriden
            };
        }

        [HttpPost("SendCompromisedProofs")]
        public IActionResult SendCompromisedProofs([FromBody] UnauthorizedUseDto unauthorizedUse)
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

            UserRootAttribute rootAttribute = _dataAccessService.GetUserAttributes(accountId).FirstOrDefault();

            UtxoPersistency utxoPersistency =  _executionContextManager.ResolveUtxoExecutionServices(accountId);

            byte[] target = unauthorizedUse.Target.HexStringToByteArray();
            byte[] compromisedKeyImage = unauthorizedUse.KeyImage.HexStringToByteArray();
            byte[] issuer = rootAttribute.Source.HexStringToByteArray();
            byte[] assetId = rootAttribute.AssetId;
            byte[] originalBlindingFactor = rootAttribute.OriginalBlindingFactor;
            byte[] originalCommitment = rootAttribute.OriginalCommitment;
            byte[] lastTransactionKey = rootAttribute.LastTransactionKey;
            byte[] lastBlindingFactor = rootAttribute.LastBlindingFactor;
            byte[] lastCommitment = rootAttribute.LastCommitment;
            byte[] lastDestinationKey = rootAttribute.LastDestinationKey;

            RequestInput requestInput = new RequestInput
            {
                AssetId = assetId,
                EligibilityBlindingFactor = originalBlindingFactor,
                EligibilityCommitment = originalCommitment,
                Issuer = issuer,
                PrevAssetCommitment = lastCommitment,
                PrevBlindingFactor = lastBlindingFactor,
                PrevDestinationKey = lastDestinationKey,
                PrevTransactionKey = lastTransactionKey,
                Target = target
            };

            OutputModel[] outputModels = _gatewayService.GetOutputs(_portalConfiguration.RingSize + 1);
            byte[][] issuanceCommitments = _gatewayService.GetIssuanceCommitments(issuer, _portalConfiguration.RingSize + 1);
            RequestResult requestResult = utxoPersistency.TransactionsService.SendCompromisedProofs(requestInput, compromisedKeyImage, outputModels, issuanceCommitments).Result;

            return Ok(requestResult.Result);
        }

		[HttpPost("SendDocumentSignRequest")]
		public IActionResult SendDocumentSignRequest([FromBody] UserAttributeTransferDto userAttributeTransfer)
		{
			ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);
			UtxoPersistency utxoPersistency = _executionContextManager.ResolveUtxoExecutionServices(accountId);

			bool proceed = true;

			if (!string.IsNullOrEmpty(userAttributeTransfer.ImageContent) && !string.IsNullOrEmpty(userAttributeTransfer.Content))
			{
				string sourceImage = _dataAccessService.GetUserAssociatedAttributes(accountId).FirstOrDefault(t => t.Item1 == AttributeType.PassportPhoto)?.Item2;
				BiometricPersonDataForSignatureDto biometricPersonDataForSignature = new BiometricPersonDataForSignatureDto
				{
					ImageSource = sourceImage,
					ImageTarget = userAttributeTransfer.ImageContent
				};

				try
				{
					BiometricSignedVerificationDto biometricSignedVerification = $"{Request.Scheme}://{Request.Host.ToUriComponent()}/biometric/".AppendPathSegment("SignPersonFaceVerification").PostJsonAsync(biometricPersonDataForSignature).ReceiveJson<BiometricSignedVerificationDto>().Result;
				}
				catch (FlurlHttpException)
				{
					proceed = false;
				}
				//Tuple<bool, bool> faceRes = VerifyFaceImage(userAttributeTransfer.ImageContent, userAttributeTransfer.Content);

				proceed = true; // faceRes.Item1;
			}

			if (proceed)
			{
				SendDocumentSignRequest(userAttributeTransfer, utxoPersistency.TransactionsService);

				return Ok(true);
			}

			return Ok(false);
		}

        [HttpPost("SendEmployeeRequest")]
        public IActionResult SendEmployeeRequest([FromBody] UserAttributeTransferDto userAttributeTransfer)
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);
            UtxoPersistency utxoPersistency = _executionContextManager.ResolveUtxoExecutionServices(accountId);

            bool proceed = true;

            if (!string.IsNullOrEmpty(userAttributeTransfer.ImageContent) && !string.IsNullOrEmpty(userAttributeTransfer.Content))
            {
                string sourceImage = _dataAccessService.GetUserAssociatedAttributes(accountId).FirstOrDefault(t => t.Item1 == AttributeType.PassportPhoto)?.Item2;
                BiometricPersonDataForSignatureDto biometricPersonDataForSignature = new BiometricPersonDataForSignatureDto
                {
                    ImageSource = sourceImage,
                    ImageTarget = userAttributeTransfer.ImageContent
                };

                try
                {
                    BiometricSignedVerificationDto biometricSignedVerification = $"{Request.Scheme}://{Request.Host.ToUriComponent()}/biometric/".AppendPathSegment("SignPersonFaceVerification").PostJsonAsync(biometricPersonDataForSignature).ReceiveJson<BiometricSignedVerificationDto>().Result;
                }
                catch (FlurlHttpException)
                {
                    proceed = false;
                }
                //Tuple<bool, bool> faceRes = VerifyFaceImage(userAttributeTransfer.ImageContent, userAttributeTransfer.Content);

                proceed = true; // faceRes.Item1;
            }

            if (proceed)
            {
                SendEmployeeRequest(userAttributeTransfer, utxoPersistency.TransactionsService);

                string[] categoryEntries = userAttributeTransfer.ExtraInfo.Split("/");

                foreach (string categoryEntry in categoryEntries)
                {
                    string groupOwnerName = categoryEntry.Split("|")[0];
                    string groupName = categoryEntry.Split("|")[1];

                    ulong groupRelationId = _dataAccessService.AddUserGroupRelation(accountId, groupOwnerName, userAttributeTransfer.Target, groupName);

                    if (groupRelationId > 0)
                    {
                        GroupRelationDto groupRelationDto = new GroupRelationDto
                        {
                            GroupRelationId = groupRelationId,
                            GroupOwnerName = groupOwnerName,
                            GroupOwnerKey = userAttributeTransfer.Target,
                            GroupName = groupName
                        };

                        _idenitiesHubContext.Clients.Group(accountId.ToString(CultureInfo.InvariantCulture)).SendAsync("PushGroupRelation", groupRelationDto);
                    }
                }


                return Ok(true);
            }

            return Ok(false);
        }

        [HttpPost("SendOnboardingRequest")]
		public IActionResult SendOnboardingRequest([FromBody] UserAttributeTransferDto userAttributeTransfer)
		{
			ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);
			UtxoPersistency utxoPersistency = _executionContextManager.ResolveUtxoExecutionServices(accountId);

            bool proceed = true;

            if (!string.IsNullOrEmpty(userAttributeTransfer.ImageContent) && !string.IsNullOrEmpty(userAttributeTransfer.Content))
            {
				string sourceImage = _dataAccessService.GetUserAssociatedAttributes(accountId).FirstOrDefault(t => t.Item1 == AttributeType.PassportPhoto)?.Item2;
				BiometricPersonDataForSignatureDto biometricPersonDataForSignature = new BiometricPersonDataForSignatureDto
				{
					ImageSource = sourceImage,
					ImageTarget = userAttributeTransfer.ImageContent
				};

				try
				{
					BiometricSignedVerificationDto biometricSignedVerification = $"{Request.Scheme}://{Request.Host.ToUriComponent()}/biometric/".AppendPathSegment("SignPersonFaceVerification").PostJsonAsync(biometricPersonDataForSignature).ReceiveJson<BiometricSignedVerificationDto>().Result;
				}
				catch (FlurlHttpException)
				{
					proceed = false;
				}
				//Tuple<bool, bool> faceRes = VerifyFaceImage(userAttributeTransfer.ImageContent, userAttributeTransfer.Content);

				proceed = true; // faceRes.Item1;
            }

            if (proceed)
            {
				SendOnboardingRequest(userAttributeTransfer, utxoPersistency.TransactionsService);

				return Ok(true);
			}

			return Ok(false);
		}

        [HttpPost("SendOnboardingWithValidationsRequest")]
		public IActionResult SendOnboardingWithValidationsRequest([FromBody] UserAttributeTransferWithValidationsDto userAttributeTransferWithValidations)
		{
			ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);
			bool res = false;

			UtxoPersistency utxoPersistency = _executionContextManager.ResolveUtxoExecutionServices(accountId);

			var rootAttribute = _dataAccessService.GetUserAttributes(accountId).FirstOrDefault(u => !u.IsOverriden && u.AttributeType == _identityAttributesService.GetRootAttributeType().Item1);

			string blindingFactorSeedString = $"{rootAttribute.Content}{userAttributeTransferWithValidations.Password}";
			byte[] blindingFactorSeed = ConfidentialAssetsHelper.FastHash256(Encoding.ASCII.GetBytes(blindingFactorSeedString));
			byte[] blindingFactor = ConfidentialAssetsHelper.ReduceScalar32(blindingFactorSeed);
			byte[] blindingPoint = ConfidentialAssetsHelper.GetPublicKey(blindingFactor);
			byte[] rootNonBlindedCommitment = ConfidentialAssetsHelper.GetNonblindedAssetCommitment(rootAttribute.AssetId);
			byte[] rootOriginatingCommitment = ConfidentialAssetsHelper.SumCommitments(rootNonBlindedCommitment, blindingPoint);

			byte[] target = userAttributeTransferWithValidations.UserAttributeTransfer.Target.HexStringToByteArray();
			_dataAccessService.GetAccountId(target, out ulong spAccountId);

			AssociatedProofPreparation[] associatedProofPreparations = null;

            IEnumerable<SpIdenitityValidation> spIdenitityValidations = _dataAccessService.GetSpIdenitityValidations(spAccountId);

            if (spIdenitityValidations != null && spIdenitityValidations.Count() > 0)
			{
				associatedProofPreparations = new AssociatedProofPreparation[spIdenitityValidations.Count()];

				var associatedAttributes = _dataAccessService.GetUserAssociatedAttributes(accountId);

				int index = 0;
				foreach (var validation in spIdenitityValidations)
				{
					string attrContent = associatedAttributes.FirstOrDefault(a => a.Item1 == validation.AttributeType)?.Item2 ?? string.Empty;
					byte[] groupId = _identityAttributesService.GetGroupId(validation.AttributeType);
					byte[] assetId = validation.AttributeType != AttributeType.DateOfBirth ? _assetsService.GenerateAssetId(validation.AttributeType, attrContent) : rootAttribute.AssetId;
					byte[] associatedBlindingFactor = validation.AttributeType != AttributeType.DateOfBirth ? ConfidentialAssetsHelper.GetRandomSeed() : null;
					byte[] associatedCommitment = validation.AttributeType != AttributeType.DateOfBirth ? ConfidentialAssetsHelper.GetAssetCommitment(assetId, associatedBlindingFactor) : null;
					byte[] associatedNonBlindedCommitment = ConfidentialAssetsHelper.GetNonblindedAssetCommitment(assetId);
					byte[] associatedOriginatingCommitment = ConfidentialAssetsHelper.SumCommitments(associatedNonBlindedCommitment, blindingPoint);

					AssociatedProofPreparation associatedProofPreparation = new AssociatedProofPreparation { GroupId = groupId, Commitment = associatedCommitment, CommitmentBlindingFactor = associatedBlindingFactor, OriginatingAssociatedCommitment = associatedOriginatingCommitment, OriginatingBlindingFactor = blindingFactor, OriginatingRootCommitment = rootOriginatingCommitment };

					associatedProofPreparations[index++] = associatedProofPreparation;
				}
			}

			SendOnboardingRequest(userAttributeTransferWithValidations.UserAttributeTransfer, utxoPersistency.TransactionsService, associatedProofPreparations);

			return Ok(res);
		}

		private void SendOnboardingRequest(UserAttributeTransferDto userAttributeTransfer, IUtxoTransactionsService transactionsService, AssociatedProofPreparation[] associatedProofPreparations = null)
		{
			byte[] target = userAttributeTransfer.Target.HexStringToByteArray();
			byte[] issuer = userAttributeTransfer.Source.HexStringToByteArray();
			byte[] payload = userAttributeTransfer.Payload.HexStringToByteArray();
			byte[] assetId = userAttributeTransfer.AssetId.HexStringToByteArray();
			byte[] originalBlindingFactor = userAttributeTransfer.OriginalBlindingFactor.HexStringToByteArray();
			byte[] originalCommitment = userAttributeTransfer.OriginalCommitment.HexStringToByteArray();
			byte[] lastTransactionKey = userAttributeTransfer.LastTransactionKey.HexStringToByteArray();
			byte[] lastBlindingFactor = userAttributeTransfer.LastBlindingFactor.HexStringToByteArray();
			byte[] lastCommitment = userAttributeTransfer.LastCommitment.HexStringToByteArray();
			byte[] lastDestinationKey = userAttributeTransfer.LastDestinationKey.HexStringToByteArray();

            RequestInput requestInput = new RequestInput
            {
                AssetId = assetId,
                EligibilityBlindingFactor = originalBlindingFactor,
                EligibilityCommitment = originalCommitment,
                Issuer = issuer,
                PrevAssetCommitment = lastCommitment,
                PrevBlindingFactor = lastBlindingFactor,
                PrevDestinationKey = lastDestinationKey,
                PrevTransactionKey = lastTransactionKey,
                Target = target,
                Payload = payload
            };

            OutputModel[] outputModels = _gatewayService.GetOutputs(_portalConfiguration.RingSize + 1);
            byte[][] issuanceCommitments = _gatewayService.GetIssuanceCommitments(issuer, _portalConfiguration.RingSize + 1);
            RequestResult requestResult = transactionsService.SendOnboardingRequest(requestInput, associatedProofPreparations, outputModels, issuanceCommitments).Result;
		}

		private void SendDocumentSignRequest(UserAttributeTransferDto userAttributeTransfer, IUtxoTransactionsService transactionsService, AssociatedProofPreparation[] associatedProofPreparations = null)
		{
			byte[] target = userAttributeTransfer.Target.HexStringToByteArray();
			byte[] issuer = userAttributeTransfer.Source.HexStringToByteArray();
			byte[] payload = userAttributeTransfer.Payload.HexStringToByteArray();
			byte[] assetId = userAttributeTransfer.AssetId.HexStringToByteArray();
			byte[] originalBlindingFactor = userAttributeTransfer.OriginalBlindingFactor.HexStringToByteArray();
			byte[] originalCommitment = userAttributeTransfer.OriginalCommitment.HexStringToByteArray();
			byte[] lastTransactionKey = userAttributeTransfer.LastTransactionKey.HexStringToByteArray();
			byte[] lastBlindingFactor = userAttributeTransfer.LastBlindingFactor.HexStringToByteArray();
			byte[] lastCommitment = userAttributeTransfer.LastCommitment.HexStringToByteArray();
			byte[] lastDestinationKey = userAttributeTransfer.LastDestinationKey.HexStringToByteArray();
			string[] extraInfo = userAttributeTransfer.ExtraInfo.Split('|');
			byte[] groupIssuer = extraInfo[0].HexStringToByteArray();
			byte[] groupAssetId = _assetsService.GenerateAssetId(AttributeType.EmployeeGroup, extraInfo[0] + extraInfo[1]);
			byte[] documentHash = extraInfo[2].HexStringToByteArray();
			ulong documentRecordHeight = ulong.Parse(extraInfo[3]);

			DocumentSignRequestInput requestInput = new DocumentSignRequestInput
			{
				AssetId = assetId,
				EligibilityBlindingFactor = originalBlindingFactor,
				EligibilityCommitment = originalCommitment,
				Issuer = issuer,
				PrevAssetCommitment = lastCommitment,
				PrevBlindingFactor = lastBlindingFactor,
				PrevDestinationKey = lastDestinationKey,
				PrevTransactionKey = lastTransactionKey,
				Target = target,
				Payload = payload,
				GroupIssuer = groupIssuer,
				GroupAssetId = groupAssetId,
				DocumentHash = documentHash,
				DocumentRecordHeight = documentRecordHeight
			};

			OutputModel[] outputModels = _gatewayService.GetOutputs(_portalConfiguration.RingSize + 1);
			byte[][] issuanceCommitments = _gatewayService.GetIssuanceCommitments(issuer, _portalConfiguration.RingSize + 1);
			RequestResult requestResult = transactionsService.SendDocumentSignRequest(requestInput, associatedProofPreparations, outputModels, issuanceCommitments).Result;
		}

		private void SendEmployeeRequest(UserAttributeTransferDto userAttributeTransfer, IUtxoTransactionsService transactionsService, AssociatedProofPreparation[] associatedProofPreparations = null)
        {
            byte[] target = userAttributeTransfer.Target.HexStringToByteArray();
            byte[] issuer = userAttributeTransfer.Source.HexStringToByteArray();
            byte[] payload = userAttributeTransfer.Payload.HexStringToByteArray();
            byte[] assetId = userAttributeTransfer.AssetId.HexStringToByteArray();
            byte[] originalBlindingFactor = userAttributeTransfer.OriginalBlindingFactor.HexStringToByteArray();
            byte[] originalCommitment = userAttributeTransfer.OriginalCommitment.HexStringToByteArray();
            byte[] lastTransactionKey = userAttributeTransfer.LastTransactionKey.HexStringToByteArray();
            byte[] lastBlindingFactor = userAttributeTransfer.LastBlindingFactor.HexStringToByteArray();
            byte[] lastCommitment = userAttributeTransfer.LastCommitment.HexStringToByteArray();
            byte[] lastDestinationKey = userAttributeTransfer.LastDestinationKey.HexStringToByteArray();

            string[] categoryEntries = userAttributeTransfer.ExtraInfo.Split("/");
            foreach (string categoryEntry in categoryEntries)
            {
                string groupName = categoryEntry.Split("|")[1];
                bool isRegistered = "true".Equals(categoryEntry.Split("|")[2], StringComparison.InvariantCultureIgnoreCase);

                if (!isRegistered)
                {
                    byte[] groupAssetId = _assetsService.GenerateAssetId(AttributeType.EmployeeGroup, userAttributeTransfer.Target + groupName);
                    EmployeeRequestInput requestInput = new EmployeeRequestInput
                    {
                        AssetId = assetId,
                        EligibilityBlindingFactor = originalBlindingFactor,
                        EligibilityCommitment = originalCommitment,
                        Issuer = issuer,
                        PrevAssetCommitment = lastCommitment,
                        PrevBlindingFactor = lastBlindingFactor,
                        PrevDestinationKey = lastDestinationKey,
                        PrevTransactionKey = lastTransactionKey,
                        Target = target,
                        Payload = payload,
                        GroupAssetId = groupAssetId
                    };

                    OutputModel[] outputModels = _gatewayService.GetOutputs(_portalConfiguration.RingSize + 1);
                    byte[][] issuanceCommitments = _gatewayService.GetIssuanceCommitments(issuer, _portalConfiguration.RingSize + 1);
                    RequestResult requestResult = transactionsService.SendEmployeeRegistrationRequest(requestInput, associatedProofPreparations, outputModels, issuanceCommitments).Result;
                }
            }
        }

        [HttpGet("GetUserAssociatedAttributes")]
		public IActionResult GetUserAssociatedAttributes()
		{
			ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

			List<Tuple<AttributeType, string>> associatedAttributes = _dataAccessService.GetUserAssociatedAttributes(accountId).ToList();
			IEnumerable<Tuple<AttributeType, string>> associatedAttributeTypes = _identityAttributesService.GetAssociatedAttributeTypes();

			foreach (Tuple<AttributeType, string> associatedAttributeType in associatedAttributeTypes.Where(t => associatedAttributes.All(a => a.Item1 != t.Item1)))
			{
				associatedAttributes.Add(new Tuple<AttributeType, string>(associatedAttributeType.Item1, ""));
			}

			return Ok(associatedAttributes.Select(
				a => new UserAssociatedAttributeDto
				{
					AttributeType = ((uint)a.Item1).ToString(CultureInfo.InvariantCulture),
					AttributeTypeName = associatedAttributeTypes.FirstOrDefault(t => t.Item1 == a.Item1)?.Item2??a.Item1.ToString(),
					Content = a.Item2
				}));
		}

		[HttpPost("UpdateUserAssociatedAttributes")]
		public IActionResult UpdateUserAssociatedAttributes([FromBody] UserAssociatedAttributeDto[] userAssociatedAttributeDtos)
		{
			ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

			_dataAccessService.UpdateUserAssociatedAttributes(accountId, userAssociatedAttributeDtos.Select(a => new Tuple<AttributeType, string>((AttributeType)uint.Parse(a.AttributeType, CultureInfo.InvariantCulture), a.Content)));

			return Ok();
		}

        [HttpPost("SendAuthenticationRequest")]
		public IActionResult SendAuthenticationRequest([FromBody] UserAttributeTransferDto userAttributeTransfer)
		{
			bool res = false;
			ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

			UtxoPersistency utxoPersistency =  _executionContextManager.ResolveUtxoExecutionServices(accountId);

			byte[] target = userAttributeTransfer.Target.HexStringToByteArray();
			byte[] issuer = userAttributeTransfer.Source.HexStringToByteArray();
			byte[] payload = userAttributeTransfer.Payload.HexStringToByteArray();
			byte[] assetId = userAttributeTransfer.AssetId.HexStringToByteArray();
			byte[] originalBlindingFactor = userAttributeTransfer.OriginalBlindingFactor.HexStringToByteArray();
			byte[] originalCommitment = userAttributeTransfer.OriginalCommitment.HexStringToByteArray();
			byte[] lastTransactionKey = userAttributeTransfer.LastTransactionKey.HexStringToByteArray();
			byte[] lastBlindingFactor = userAttributeTransfer.LastBlindingFactor.HexStringToByteArray();
			byte[] lastCommitment = userAttributeTransfer.LastCommitment.HexStringToByteArray();
			byte[] lastDestinationKey = userAttributeTransfer.LastDestinationKey.HexStringToByteArray();


            RequestInput requestInput = new RequestInput { AssetId = assetId, EligibilityBlindingFactor = originalBlindingFactor, EligibilityCommitment = originalCommitment, Issuer = issuer, PrevAssetCommitment = lastCommitment, PrevBlindingFactor = lastBlindingFactor, PrevDestinationKey = lastDestinationKey, PrevTransactionKey = lastTransactionKey, Target = target, Payload = payload };

            OutputModel[] outputModels = _gatewayService.GetOutputs(_portalConfiguration.RingSize + 1);
            byte[][] issuanceCommitments = _gatewayService.GetIssuanceCommitments(issuer, _portalConfiguration.RingSize + 1);
            RequestResult requestResult = utxoPersistency.TransactionsService.SendAuthenticationRequest(requestInput, outputModels, issuanceCommitments).Result;

			res = true;
			return Ok(res);
		}

        [HttpGet("GetUserDetails")]
        public IActionResult GetUserDetails()
        {
			ulong userId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

			Client.DataModel.Model.Account account = _accountsService.GetById(userId);

            if(account != null)
            {
                return Ok(new
                {
                    Id = userId.ToString(CultureInfo.InvariantCulture),
                    account.AccountInfo,
                    PublicSpendKey = account.PublicSpendKey.ToHexString(),
                    PublicViewKey = account.PublicViewKey.ToHexString(),
					account.IsCompromised
                });
            }

            return BadRequest();
        }

        [HttpPost("RequestForIdentity")]
        public IActionResult RequestForIdentity([FromBody] RequestForIdentityDto requestForIdentity)
        {
			ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);
			Account account = _accountsService.GetById(accountId);

			string blindingFactorSeedString = $"{requestForIdentity.IdCardContent}{requestForIdentity.Password}";
			byte[] blindingFactorSeed = ConfidentialAssetsHelper.FastHash256(Encoding.ASCII.GetBytes(blindingFactorSeedString));
			byte[] blindingFactor = ConfidentialAssetsHelper.ReduceScalar32(blindingFactorSeed);
			byte[] blindingPoint = ConfidentialAssetsHelper.GetPublicKey(blindingFactor);

			IdentityRequestDto identityRequest = new IdentityRequestDto
			{
				RequesterPublicSpendKey = account.PublicSpendKey.ToHexString(),
				RequesterPublicViewKey = account.PublicViewKey.ToHexString(),
				RootAttributeContent = requestForIdentity.IdCardContent,
				BlindingPoint = blindingPoint.ToHexString(),
				FaceImageContent = requestForIdentity.ImageContent
			};

			byte[] b = Convert.FromBase64String(requestForIdentity.Target);
			string uri = Encoding.UTF8.GetString(b);
			HttpResponseMessage httpResponse = uri.PostJsonAsync(identityRequest).Result;

			if (httpResponse.IsSuccessStatusCode)
			{
				//TODO: this step should be done if Identity Provider API returned OK
				_dataAccessService.UpdateUserAssociatedAttributes(accountId, new List<Tuple<AttributeType, string>> { new Tuple<AttributeType, string>(AttributeType.PassportPhoto, requestForIdentity.ImageContent) });
				return Ok();
			}

			return BadRequest(httpResponse.Content.ReadAsAsync<string>().Result);
        }

        [HttpGet("GetActionType")]
        public IActionResult GetActionType([FromQuery]string actionInfo)
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);
			UtxoPersistency utxoPersistency = _executionContextManager.ResolveUtxoExecutionServices(accountId);

			byte[] b = Convert.FromBase64String(actionInfo);
			string actionDecoded = Encoding.UTF8.GetString(b);

            if(actionDecoded.StartsWith("addr://"))
            {
                return GetProofActionType(actionDecoded);
            }
            else if (actionDecoded.StartsWith("sig://"))
            {
                return GetSignatureValidationActionType(actionDecoded);
            }
            else
            { 
                if (actionDecoded.Contains("ProcessRootIdentityRequest", StringComparison.InvariantCultureIgnoreCase))
                {
                    return Ok(new { Action = "1", ActionInfo = actionInfo });
                }
                else
                {
                    return GetServiceProviderActionType(utxoPersistency.ClientCryptoService, actionDecoded);
                }
            }
        }

        private IActionResult GetProofActionType(string actionDecoded)
        {
            return Ok(new { Action = "8", ActionInfo = actionDecoded.Replace("addr://", "") });
        }

        private IActionResult GetSignatureValidationActionType(string actionDecoded)
        {
            return Ok(new { Action = "7", ActionInfo = actionDecoded.Replace("sig://", "") });
        }

        private IActionResult GetServiceProviderActionType(IUtxoClientCryptoService clientCryptoService, string actionDecoded)
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

			UriBuilder uriBuilder = new UriBuilder(actionDecoded);
			string actionType = uriBuilder.Uri.ParseQueryString()["actionType"];

			string registrationKey;
			byte[] targetBytes = uriBuilder.Uri.ParseQueryString()["publicKey"]?.HexStringToByteArray();
            UserRootAttribute rootAttribute = _dataAccessService.GetUserAttributes(accountId).FirstOrDefault();

            if (actionType == "0") // Login and register
            {
                clientCryptoService.GetBoundedCommitment(rootAttribute.AssetId, targetBytes, out byte[] blindingFactor, out byte[] assetCommitment);
                registrationKey = assetCommitment.ToHexString();
				NameValueCollection queryParams = uriBuilder.Uri.ParseQueryString();
				queryParams["registrationKey"] = registrationKey;
				uriBuilder.Query = queryParams.ToString();
			}
            else if (actionType == "1") // employee registration
			{
                registrationKey = rootAttribute.AssetId.ToHexString();
				NameValueCollection queryParams = uriBuilder.Uri.ParseQueryString();
				queryParams["registrationKey"] = registrationKey;
				uriBuilder.Query = queryParams.ToString();
			}
			else if (actionType == "2") // document sign
			{
			}

			ServiceProviderActionAndValidationsDto serviceProviderActionAndValidations = uriBuilder.Uri.ToString().GetJsonAsync<ServiceProviderActionAndValidationsDto>().Result;

			string validationsExpression = string.Empty;

			if ((serviceProviderActionAndValidations.Validations?.Count ?? 0) > 0)
			{
				validationsExpression = ":" + serviceProviderActionAndValidations.Validations.JoinStrings("|");
			}

			if (actionType == "2")
			{
				return Ok(new { Action = "6", ActionInfo = $"{serviceProviderActionAndValidations.PublicKey}:{serviceProviderActionAndValidations.SessionKey}:{serviceProviderActionAndValidations.ExtraInfo}{validationsExpression}" });
			}
			else
			{
				if (serviceProviderActionAndValidations.IsRegistered)
				{
					return Ok(new { Action = actionType == "0" ? "3" : "5", ActionInfo = $"{serviceProviderActionAndValidations.PublicKey}:{serviceProviderActionAndValidations.SessionKey}:{serviceProviderActionAndValidations.ExtraInfo}{validationsExpression}" });
				}
				else
				{
					return Ok(new { Action = actionType == "0" ? "2" : "4", ActionInfo = $"{serviceProviderActionAndValidations.PublicKey}:{serviceProviderActionAndValidations.SessionKey}:{serviceProviderActionAndValidations.ExtraInfo}{validationsExpression}" });
				}
			}
		}

        [HttpGet("GetSpValidations/{spTarget}")]
        public IActionResult GetSpValidations(string actionInfo)
        {
            UserAttributeTransferWithValidationsDto userAttributeTransferWithValidationsDto = new UserAttributeTransferWithValidationsDto
            {
                Validations = new List<string>()
            };


            string[] actionInfoParts = actionInfo.Split(':');
            byte[] target = actionInfoParts[0].HexStringToByteArray();
            _dataAccessService.GetAccountId(target, out ulong spAccountId);

            IEnumerable<SpIdenitityValidation> spIdenitityValidations = _dataAccessService.GetSpIdenitityValidations(spAccountId);

            if (spIdenitityValidations != null && spIdenitityValidations.Count() > 0)
            {
                IEnumerable<Tuple<AttributeType, string>> attributeDescriptions = _identityAttributesService.GetAssociatedAttributeTypes();
                IEnumerable<Tuple<ValidationType, string>> validationDescriptions = _identityAttributesService.GetAssociatedValidationTypes();

                List<string> validations = new List<string>();

                foreach (SpIdenitityValidation spIdenitityValidation in spIdenitityValidations)
                {
                    if (spIdenitityValidation.AttributeType != AttributeType.DateOfBirth)
                    {
                        userAttributeTransferWithValidationsDto.Validations.Add(attributeDescriptions.FirstOrDefault(d => d.Item1 == spIdenitityValidation.AttributeType)?.Item2 ?? spIdenitityValidation.AttributeType.ToString());
                    }
                    else
                    {
                        userAttributeTransferWithValidationsDto.Validations.Add(validationDescriptions.FirstOrDefault(d => d.Item1 == spIdenitityValidation.ValidationType)?.Item2 ?? spIdenitityValidation.ValidationType.ToString());
                    }
                }
            }

            return Ok(userAttributeTransferWithValidationsDto);
        }

        [HttpPost("ClearCompromised")]
        public IActionResult ClearCompromised()
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

            _dataAccessService.ClearAccountCompromised(accountId);
            
            return Ok();
        }

        [HttpGet("GetDocumentSignatureVerification")]
        public IActionResult GetDocumentSignatureVerification([FromQuery] string documentCreator, [FromQuery] string documentHash, [FromQuery] ulong documentRecordHeight, [FromQuery] ulong signatureRecordBlockHeight)
        {
            DocumentSignatureVerification signatureVerification = _documentSignatureVerifier.Verify(documentCreator.HexStringToByteArray(), documentHash.HexStringToByteArray(), documentRecordHeight, signatureRecordBlockHeight);

            return Ok(signatureVerification);
        }

        [HttpGet("GetGroupRelations")]
        public IActionResult GetGroupRelations()
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

            return Ok(_dataAccessService.GetUserGroupRelations(accountId)?.Select(g => new GroupRelationDto { GroupRelationId = g.UserGroupRelationId, GroupOwnerName = g.GroupOwnerName, GroupOwnerKey = g.GroupOwnerKey, GroupName = g.GroupName})??Array.Empty<GroupRelationDto>());
        }

        [HttpDelete("DeleteGroupRelation/{grouprelationId}")]
        public IActionResult DeleteGroupRelation(ulong grouprelationId)
        {
            _dataAccessService.RemoveUserGroupRelation(grouprelationId);

            return Ok();
        }

        [HttpPost("SendRelationsProofs")]
        public IActionResult SendRelationsProofs([FromBody] RelationsProofsDto relationsProofs)
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);
            UtxoPersistency utxoPersistency = _executionContextManager.ResolveUtxoExecutionServices(accountId);

            byte[] target = relationsProofs.Target.HexStringToByteArray();
            byte[] targetViewKey = relationsProofs.TargetViewKey.HexStringToByteArray();
            byte[] issuer = relationsProofs.Source.HexStringToByteArray();
            byte[] assetId = relationsProofs.AssetId.HexStringToByteArray();
            byte[] originalBlindingFactor = relationsProofs.OriginalBlindingFactor.HexStringToByteArray();
            byte[] originalCommitment = relationsProofs.OriginalCommitment.HexStringToByteArray();
            byte[] lastTransactionKey = relationsProofs.LastTransactionKey.HexStringToByteArray();
            byte[] lastBlindingFactor = relationsProofs.LastBlindingFactor.HexStringToByteArray();
            byte[] lastCommitment = relationsProofs.LastCommitment.HexStringToByteArray();
            byte[] lastDestinationKey = relationsProofs.LastDestinationKey.HexStringToByteArray();
            byte[] imageContent = Convert.FromBase64String(relationsProofs.ImageContent);

            string sessionKey = _gatewayService.PushRelationProofSession(
                new RelationProofSession
                {
                    ImageContent = relationsProofs.ImageContent,
                    RelationEntries = relationsProofs.Relations.Select(r => new RelationEntry { RelatedAssetOwnerName = r.GroupOwnerName, RelatedAssetOwnerKey = r.GroupOwnerKey, RelatedAssetName = r.GroupName }).ToArray()
                });

            RelationsProofsInput requestInput = new RelationsProofsInput
            {
                AssetId = assetId,
                EligibilityBlindingFactor = originalBlindingFactor,
                EligibilityCommitment = originalCommitment,
                Issuer = issuer,
                PrevAssetCommitment = lastCommitment,
                PrevBlindingFactor = lastBlindingFactor,
                PrevDestinationKey = lastDestinationKey,
                PrevTransactionKey = lastTransactionKey,
                Target = target,
                TargetViewKey = targetViewKey,
                Payload = sessionKey.HexStringToByteArray(),
                ImageHash = ConfidentialAssetsHelper.FastHash256(imageContent),
                Relations = relationsProofs.Relations.Select(r => new Relation { RelatedAssetOwner = r.GroupOwnerKey.HexStringToByteArray(), RelatedAssetId = _assetsService.GenerateAssetId(AttributeType.EmployeeGroup, r.GroupOwnerKey + r.GroupName)}).ToArray()
            };

            OutputModel[] outputModels = _gatewayService.GetOutputs(_portalConfiguration.RingSize + 1);
            byte[][] issuanceCommitments = _gatewayService.GetIssuanceCommitments(issuer, _portalConfiguration.RingSize + 1);

            utxoPersistency.TransactionsService.SendRelationsProofs(requestInput, outputModels, issuanceCommitments);

            return Ok();
        }
    }
}