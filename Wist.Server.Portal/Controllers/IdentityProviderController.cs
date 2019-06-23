using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using Wist.Client.Common.Entities;
using Wist.Client.Common.Interfaces;
using Wist.Client.DataModel.Enums;
using Wist.Client.DataModel.Model;
using Wist.Client.DataModel.Services;
using Wist.Server.Portal.Dtos;
using Wist.Server.Portal.Hubs;
using Wist.Server.Portal.Services;
using Wist.Core.ExtensionMethods;
using System.Globalization;
using Account = Wist.Client.DataModel.Model.Account;
using Wist.Core.Configuration;
using Wist.Server.Portal.Configuration;
using Flurl;
using Flurl.Http;

namespace Wist.Server.Portal.Controllers
{
    [Authorize(Roles = "puser")]
	[ApiController]
	[Route("[controller]")]
	public class IdentityProviderController : ControllerBase
	{
		private readonly IExecutionContextManager _executionContextManager;
		private readonly IAssetsService _assetsService;
		private readonly IDataAccessService _externalDataAccessService;
		private readonly IIdentityAttributesService _identityAttributesService;
		private readonly IAccountsService _accountsService;
		private readonly IDataAccessService _dataAccessService;
		private readonly IHubContext<IdentitiesHub> _hubContext;
        private readonly IPortalConfiguration _portalConfiguration;

        public IdentityProviderController(
            IExecutionContextManager executionContextManager, 
            IAssetsService assetsService,
            IDataAccessService dataAccessService,
            IDataAccessService externalDataAccessService, 
			IIdentityAttributesService identityAttributesService,
			IFacesService facesService,
			IAccountsService accountsService,
            IConfigurationService configurationService,
			IHubContext<IdentitiesHub> hubContext)
		{
            _dataAccessService = dataAccessService;
			_executionContextManager = executionContextManager;
			_assetsService = assetsService;
			_externalDataAccessService = externalDataAccessService;
			_identityAttributesService = identityAttributesService;
			_accountsService = accountsService;
			_hubContext = hubContext;
            _portalConfiguration = configurationService.Get<IPortalConfiguration>();
		}


        [AllowAnonymous]
        [HttpGet("All")]
        public IActionResult GetAll()
        {
            var identityProviders = _accountsService.GetAll().Where(a => a.AccountType == AccountType.IdentityProvider).Select(a => new IdentityProviderInfoDto
            {
                Id = a.AccountId.ToString(CultureInfo.InvariantCulture),
                Description = a.AccountInfo,
                Target = a.PublicSpendKey.ToHexString()
            });

            return Ok(identityProviders);
        }

        [AllowAnonymous]
        [HttpGet("ById/{accountId}")]
        public IActionResult GetById(ulong accountId)
        {
            Account account = _accountsService.GetById(accountId);

            if(account == null)
            {
                return BadRequest();
            }

            var identityProvider = new IdentityProviderInfoDto
            {
                Id = account.AccountId.ToString(CultureInfo.InvariantCulture),
                Description = account.AccountInfo,
                Target = account.PublicSpendKey.ToHexString()
            };

            return Ok(identityProvider);
        }

        [HttpPost("CreateIdentity")]
        public IActionResult CreateIdentity([FromBody]IdentityDto identity)
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

            StatePersistency statePersistency = _executionContextManager.ResolveStateExecutionServices(accountId);
            Account account = _accountsService.GetById(accountId);

            byte[] assetId = _assetsService.GenerateAssetId((AttributeType)identity.RootAttribute.AttributeType, identity.RootAttribute.Content);
            statePersistency.TransactionsService.IssueBlindedAsset(assetId, 0UL.ToByteArray(32), out byte[] originatingCommitment);
            identity.RootAttribute.OriginatingCommitment = originatingCommitment.ToHexString();

            Identity identityDb = _externalDataAccessService.CreateIdentity(accountId, identity.Description, new IdentityAttribute { AttributeType = AttributeType.IdCard, Content = identity.RootAttribute.Content, Subject = ClaimSubject.User, Commitment = originatingCommitment });
            identity.Id = identityDb.IdentityId.ToString(CultureInfo.InvariantCulture);

            string imageContent = null;

            foreach (var identityAttributeDto in identity.AssociatedAttributes)
            {
                IdentityAttribute identityAttribute = new IdentityAttribute
                {
                    AttributeType = (AttributeType)identityAttributeDto.AttributeType,
                    Content = identityAttributeDto.Content,
                    Subject = ClaimSubject.User
                };
                _externalDataAccessService.AddAssociatedIdentityAttribute(identityDb.IdentityId, ref identityAttribute);

                if (((AttributeType)identityAttributeDto.AttributeType) == AttributeType.PassportPhoto)
                {
                    imageContent = identityAttributeDto.Content;
                }
            }

            if (!string.IsNullOrEmpty(identity.RootAttribute.Content) && !string.IsNullOrEmpty(imageContent))
            {
                $"{Request.Scheme}://{Request.Host.ToUriComponent()}/biometric/".AppendPathSegment("RegisterPerson").PostJsonAsync(new BiometricPersonDataDto { Requester = account.PublicSpendKey.ToHexString(), PersonData = identity.RootAttribute.Content, ImageString = imageContent });
            }

            _hubContext.Clients.Group(User.Identity.Name).SendAsync("PushIdentity", identity);

            return Ok();
        }

		[HttpGet("GetIdentityById/{id}")]
		public IActionResult GetIdentityById(ulong id)
		{
			Identity identity = _externalDataAccessService.GetIdentity(id);

			if(identity != null)
            {
                return base.Ok(GetIdentityDto(identity));
            }

            return BadRequest();
		}

        private IdentityDto GetIdentityDto(Identity identity)
        {
            return new IdentityDto
            {
                Id = identity.IdentityId.ToString(CultureInfo.InvariantCulture),
                Description = identity.Description,
                RootAttribute = new IdentityAttributeDto
                {
                    Content = $"{identity.RootAttribute.Content}",
                    AttributeType = (uint)identity.RootAttribute.AttributeType,
                    OriginatingCommitment = identity.RootAttribute.Commitment.ToHexString()
                },
                AssociatedAttributes = identity.AssociatedAttributes.Select(
                                    a => new IdentityAttributeDto
                                    {
                                        AttributeType = (uint)a.AttributeType,
                                        Content = a.Content,
                                        OriginatingCommitment = a.Commitment?.ToHexString()
                                    }).ToList(),
                //NumberOfTransfers = _dataAccessService.GetOutcomingTransactionsCountByOriginatingCommitment(identity.RootAttribute.Commitment)
            };
        }

        [HttpGet("GetAllIdentities/{accountId}")]
		public IActionResult GetAllIdentities(ulong accountId)
		{
			IEnumerable<Identity> identities = _externalDataAccessService.GetIdentities(accountId);

            return Ok(identities?.Select(identity => GetIdentityDto(identity)));
		}

		[HttpPost("SendAssetIdNew")]
		public IActionResult SendAssetIdNew([FromBody]AttributeTransferDetails attributeTransferDetails)
		{
			ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

			StatePersistency statePersistency = _executionContextManager.ResolveStateExecutionServices(accountId);

			return Ok(statePersistency.TransactionsService.TransferAssetToUtxo(_assetsService.GenerateAssetId(AttributeType.IdCard, attributeTransferDetails.Id), 
			new ConfidentialAccount
			{
				PublicSpendKey = attributeTransferDetails.PublicSpendKey.HexStringToByteArray(),
				PublicViewKey = attributeTransferDetails.PublicViewKey.HexStringToByteArray()
			}));
		}

		[AllowAnonymous]
		[HttpGet("GetAttributesSchema")]
		public IActionResult GetAttributesSchema()
		{
			Tuple<AttributeType, string> root = _identityAttributesService.GetRootAttributeType();
			IEnumerable<Tuple<AttributeType, string>> associated = _identityAttributesService.GetAssociatedAttributeTypes();

			IdentityAttributesSchemaDto schemaDto = new IdentityAttributesSchemaDto
			{
				RootAttribute = new IdentityAttributeSchemaDto { AttributeType = (uint)root.Item1, Name = root.Item2 },
				AssociatedAttributes = associated.Select(a => new IdentityAttributeSchemaDto { AttributeType = (uint)a.Item1, Name = a.Item2 }).ToList()
			};

			return Ok(schemaDto);
		}

		[AllowAnonymous]
		[HttpPost("ProcessRootIdentityRequest/{issuer}")]
		public IActionResult ProcessRootIdentityRequest(string issuer, [FromBody] IdentityRequestDto identityRequest)
		{
			Account identityProviderAccount = _accountsService.GetByPublicKey(issuer.HexStringToByteArray());
			StatePersistency statePersistency = _executionContextManager.ResolveStateExecutionServices(identityProviderAccount.AccountId);

			Tuple<bool, bool> proceed = VerifyFaceImage(identityRequest.FaceImageContent, identityRequest.RootAttributeContent, issuer);
			if (proceed.Item1)
			{
				byte[] rootAssetId = _assetsService.GenerateAssetId(AttributeType.IdCard, identityRequest.RootAttributeContent);
                byte[] faceImageAssetId = _assetsService.GenerateAssetId(AttributeType.PassportPhoto, identityRequest.FaceImageContent);

				ProcessIssuingAssociatedAttributes(identityRequest, statePersistency.TransactionsService, rootAssetId, faceImageAssetId);

				return TransferAssetToUtxo(statePersistency.TransactionsService, new ConfidentialAccount { PublicSpendKey = identityRequest.RequesterPublicSpendKey.HexStringToByteArray(), PublicViewKey = identityRequest.RequesterPublicViewKey.HexStringToByteArray() }, rootAssetId);
			}
			else
			{
				if (!proceed.Item2)
				{
					return BadRequest(new { Message = $"Failed to find person with ID Card number {identityRequest.RootAttributeContent}" });
				}

				return BadRequest(new { Message = "Captured face does not match to registered one" });
			}
		}

		private void ProcessIssuingAssociatedAttributes(IdentityRequestDto identityRequest, IStateTransactionsService transactionsService, byte[] rootAssetId, byte[] faceImage)
		{
			byte[] blindingPoint = identityRequest.BlindingPoint.HexStringToByteArray();
			Identity identity = _externalDataAccessService.GetIdentityByRootAttribute(identityRequest.RootAttributeContent);
			if (identity != null)
			{
				foreach (var identityAttribute in identity.AssociatedAttributes)
				{
					ProcessIssuingAssociatedAttribute(identityAttribute, blindingPoint, rootAssetId, transactionsService);
				}
			}
		}

		private void ProcessIssuingAssociatedAttribute(IdentityAttribute identityAttribute, byte[] blindingPoint, byte[] rootAssetId, IStateTransactionsService transactionsService)
		{
			byte[] assetId = _assetsService.GenerateAssetId(identityAttribute.AttributeType, identityAttribute.Content);
			byte[] groupId = null;
			switch (identityAttribute.AttributeType)
			{
				case AttributeType.PlaceOfBirth:
					groupId = _identityAttributesService.GetGroupId(identityAttribute.AttributeType, identityAttribute.Content);
					break;
				case AttributeType.DateOfBirth:
					groupId = _identityAttributesService.GetGroupId(identityAttribute.AttributeType, DateTime.ParseExact(identityAttribute.Content, "yyyy-MM-dd", null));
					break;
				default:
					groupId = _identityAttributesService.GetGroupId(identityAttribute.AttributeType);
					break;
			}

			transactionsService.IssueAssociatedAsset(assetId, groupId, blindingPoint, rootAssetId, out byte[] originatingCommitment);

			_externalDataAccessService.UpdateAssociatedIdentityAttributeCommitment(identityAttribute.AttributeId, originatingCommitment);

		}

		private Tuple<bool, bool> VerifyFaceImage(string imageContent, string idContent, string publicKey)
		{
			if (!string.IsNullOrEmpty(imageContent))
			{
                try
                {
                    BiometricResultDto biometricResult = $"{Request.Scheme}://{Request.Host.ToUriComponent()}/biometric/".AppendPathSegment("VerifyPersonFace").PostJsonAsync(new BiometricPersonDataDto { Requester = publicKey, PersonData = idContent, ImageString = imageContent }).ReceiveJson<BiometricResultDto>().Result;


					return new Tuple<bool, bool>(biometricResult.Result, true);
                }
                catch (FlurlHttpException)
                {
                    return new Tuple<bool, bool>(false, false);
                }
			}

			return new Tuple<bool, bool>(true, false);
		}

		private IActionResult TransferAssetToUtxo(IStateTransactionsService transactionsService, ConfidentialAccount account, byte[] rootAssetId)
		{
			try
			{
				bool sent = transactionsService.TransferAssetToUtxo(rootAssetId, account);

				if (sent)
				{
					return Ok();
				}
				else
				{
					throw new Exception("Sending failed");
				}
			}
			catch (Exception ex)
			{
				return BadRequest(new { ex.Message });
			}
		}
	}
}