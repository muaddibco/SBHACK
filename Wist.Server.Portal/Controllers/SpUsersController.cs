using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wist.Crypto.ConfidentialAssets;
using Wist.Core.ExtensionMethods;
using Wist.Server.Portal.Services;
using Wist.Client.DataModel.Model;
using Wist.Client.DataModel.Services;
using Wist.Server.Portal.Dtos;
using System.Collections.Generic;
using Wist.Client.Common.Interfaces;
using System.Linq;
using Wist.Client.DataModel.Enums;
using System;
using Wist.Server.Portal.Dtos.ServiceProvider;

namespace Wist.Server.Portal.Controllers
{
	[Authorize(Roles = "spuser")]
	[ApiController]
	[Route("[controller]")]
	public class SpUsersController : ControllerBase
	{
		private readonly IAccountsService _accountsService;
		private readonly IDataAccessService _dataAccessService;
		private readonly IIdentityAttributesService _identityAttributesService;

		public SpUsersController(IAccountsService accountsService, IDataAccessService dataAccessService, IIdentityAttributesService identityAttributesService)
		{
			_accountsService = accountsService;
			_dataAccessService = dataAccessService;
			_identityAttributesService = identityAttributesService;
		}


		[AllowAnonymous]
		[HttpGet("GetSessionInfo/{spId}")]
		public IActionResult GetSessionInfo(ulong spId)
		{
			string nonce = ConfidentialAssetsHelper.GetRandomSeed().ToHexString();
			Account spAccount = _accountsService.GetById(spId);

			return Ok(new
			{
				publicKey = spAccount.PublicSpendKey.ToHexString(),
				sessionKey = nonce,
			});
		}

		[AllowAnonymous]
		[HttpGet("GetDocuments/{spId}")]
		public IActionResult GetDocuments(ulong spId)
		{
			IEnumerable<DocumentDto> documents = _dataAccessService.GetSpDocuments(spId)
                .Select(d => 
                new DocumentDto
                {
                    DocumentId = d.SpDocumentId,
                    DocumentName = d.DocumentName,
                    Hash = d.Hash,
                    Signatures = (d.DocumentSignatures?.Select(s => new DocumentSignatureDto { DocumentId = d.SpDocumentId, DocumentHash = d.Hash, SignatureId = s.SpDocumentSignatureId, DocumentRecordHeight = s.DocumentRecordHeight, SignatureRecordHeight = s.SignatureRecordHeight})??Array.Empty<DocumentSignatureDto>()).ToList()
                });

			return Ok(documents);
		}

		[AllowAnonymous]
		[HttpGet("GetActionInfo")]
		public IActionResult GetActionInfo([FromQuery]int actionType, [FromQuery]string publicKey, [FromQuery]string sessionKey, [FromQuery]string registrationKey)
		{
			Account spAccount = _accountsService.GetByPublicKey(publicKey.HexStringToByteArray());
			bool isRegistered = false;
			string extraInfo = null;
			List<string> validityInfo = new List<string>();
            string[] details = Array.Empty<string>();

			// Onboarding & Login
			if (actionType == 0)
			{
				ServiceProviderRegistration serviceProviderRegistration = _dataAccessService.GetServiceProviderRegistration(spAccount.AccountId, registrationKey.HexStringToByteArray()); ;
				isRegistered = serviceProviderRegistration != null;
			}
			// Employee registration
			else if (actionType == 1)
			{
				List<SpEmployee> spEmployees = _dataAccessService.GetSpEmployees(spAccount.AccountId, registrationKey);
                extraInfo = "";

                foreach (SpEmployee spEmployee in spEmployees)
                {
                    if(!string.IsNullOrEmpty(extraInfo))
                    {
                        extraInfo += "/";
                    }
                    extraInfo += $"{spAccount.AccountInfo}|{spEmployee?.SpEmployeeGroup?.GroupName}|{!string.IsNullOrEmpty(spEmployee.RegistrationCommitment)}";
                }

                isRegistered = spEmployees.Count > 0;
			}
			// Document sign
			else if (actionType == 2)
			{
				SpDocument spDocument = _dataAccessService.GetSpDocument(spAccount.AccountId, registrationKey);
				if (spDocument != null)
				{
					isRegistered = true;
					extraInfo = $"{spDocument.DocumentName}|{spDocument.Hash}|{spDocument.LastChangeRecordHeight}";

					foreach (var allowedSigner in spDocument.AllowedSigners)
					{
						validityInfo.Add($"{allowedSigner.GroupIssuer};{allowedSigner.GroupName}");
					}
				}
			}

			if (actionType == 0 || actionType == 1)
			{
				IEnumerable<SpIdenitityValidation> spIdenitityValidations = _dataAccessService.GetSpIdenitityValidations(spAccount.AccountId);

				if (spIdenitityValidations != null && spIdenitityValidations.Count() > 0)
				{
					IEnumerable<Tuple<AttributeType, string>> attributeDescriptions = _identityAttributesService.GetAssociatedAttributeTypes();
					IEnumerable<Tuple<ValidationType, string>> validationDescriptions = _identityAttributesService.GetAssociatedValidationTypes();

					List<string> validations = new List<string>();

					foreach (SpIdenitityValidation spIdenitityValidation in spIdenitityValidations)
					{
						if (spIdenitityValidation.AttributeType != AttributeType.DateOfBirth)
						{
							validityInfo.Add(attributeDescriptions.FirstOrDefault(d => d.Item1 == spIdenitityValidation.AttributeType)?.Item2 ?? spIdenitityValidation.AttributeType.ToString());
						}
						else
						{
							validityInfo.Add(validationDescriptions.FirstOrDefault(d => d.Item1 == spIdenitityValidation.ValidationType)?.Item2 ?? spIdenitityValidation.ValidationType.ToString());
						}
					}
				}
			}

			ServiceProviderActionAndValidationsDto serviceProviderActionAndValidations = new ServiceProviderActionAndValidationsDto
			{
				IsRegistered = isRegistered,
				PublicKey = publicKey,
				SessionKey = sessionKey,
				ExtraInfo = extraInfo,
				Validations = validityInfo
			};

			return Ok(serviceProviderActionAndValidations);
        }
    }
}
