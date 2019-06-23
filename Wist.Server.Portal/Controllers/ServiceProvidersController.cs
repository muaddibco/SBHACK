using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using Wist.Server.Portal.Helpers;
using Wist.Server.Portal.Services;
using Wist.Core.ExtensionMethods;
using Wist.Server.Portal.Dtos;
using Wist.Client.DataModel.Services;
using Wist.Client.DataModel.Model;
using Wist.Client.Common.Interfaces;
using Wist.Client.DataModel.Enums;
using System.Globalization;
using Wist.Crypto.ConfidentialAssets;
using Wist.Server.Portal.Dtos.ServiceProvider;
using System.IO;
using Wist.Core.HashCalculations;
using Wist.Core;

namespace Wist.Server.Portal.Controllers
{
    [Authorize(Roles = "puser")]
	[ApiController]
	[Route("[controller]")]
	public class ServiceProvidersController : ControllerBase
    {
		private readonly IAccountsService _accountsService;
		private readonly IExecutionContextManager _executionContextManager;
		private readonly IDataAccessService _dataAccessService;
		private readonly IIdentityAttributesService _identityAttributesService;
        private readonly IAssetsService _assetsService;
        private readonly AppSettings _appSettings;
        private readonly IHashCalculation _hashCalculation;

		public ServiceProvidersController(IAccountsService accountsService, IExecutionContextManager executionContextManager, IDataAccessService dataAccessService, IIdentityAttributesService identityAttributesService, IHashCalculationsRepository hashCalculationsRepository, IAssetsService assetsService, IOptions<AppSettings> appSettings)
		{
			_accountsService = accountsService;
			_executionContextManager = executionContextManager;
			_dataAccessService = dataAccessService;
			_identityAttributesService = identityAttributesService;
            _assetsService = assetsService;
            _appSettings = appSettings.Value;
            _hashCalculation = hashCalculationsRepository.Create(Globals.DEFAULT_HASH);
		}

		[HttpGet("GetRegistrations")]
		public IActionResult GetRegistrations()
		{
			ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

			IEnumerable<ServiceProviderRegistration> registrations = _dataAccessService.GetServiceProviderRegistrations(accountId);

			return Ok(registrations.Select(r => 
			new ServiceProviderRegistrationDto {
				ServiceProviderRegistrationId = r.ServiceProviderRegistrationId.ToString(CultureInfo.InvariantCulture),
				Commitment = r.Commitment.ToHexString()
			}));
		}

		[AllowAnonymous]
		[HttpGet("GetAll")]
		public IActionResult GetAll()
		{
			var serviceProviders = _accountsService.GetAll().Where(a => a.AccountType == AccountType.ServiceProvider).Select(a => new ServiceProviderInfoDto
			{
				Id = a.AccountId.ToString(CultureInfo.InvariantCulture),
				Description = a.AccountInfo,
				Target = a.PublicSpendKey.ToHexString()
			});

			return Ok(serviceProviders);
		}
		[AllowAnonymous]
		[HttpGet("ById/{id}")]
		public IActionResult GetById(ulong id)
		{
			var serviceProvider = _accountsService.GetAll().FirstOrDefault(a => a.AccountId == id);

			return Ok(new ServiceProviderInfoDto { Id = id.ToString(CultureInfo.InvariantCulture), Description = serviceProvider.AccountInfo, Target = serviceProvider.PublicSpendKey.ToHexString() });
		}

		[AllowAnonymous]
		[HttpGet("GetIdentityAttributeValidationDescriptors")]
		public IActionResult GetIdentityAttributeValidationDescriptors()
		{
			return Ok(_identityAttributesService.GetIdentityAttributeValidationDescriptors());
		}

		[HttpGet("GetIdentityAttributeValidations")]
		public IActionResult GetIdentityAttributeValidations()
		{
			ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);
			IEnumerable<SpIdenitityValidation> spIdenitityValidations = _dataAccessService.GetSpIdenitityValidations(accountId);

			return Ok(spIdenitityValidations.Select(s => 
			{
				switch (s.AttributeType)
				{
					case AttributeType.PlaceOfBirth:
						return new IdentityAttributeValidationDefinitionDto
						{
							CriterionValue = s.GroupIdCriterion?.ToHexString(),
							AttributeType = ((uint)s.AttributeType).ToString(CultureInfo.InvariantCulture),
							ValidationType = ((ushort)s.ValidationType).ToString(CultureInfo.InvariantCulture)
						};
					case AttributeType.DateOfBirth:
						return new IdentityAttributeValidationDefinitionDto
						{
							CriterionValue = s.NumericCriterion.Value.ToString(CultureInfo.InvariantCulture),
							AttributeType = ((uint)s.AttributeType).ToString(CultureInfo.InvariantCulture),
							ValidationType = ((ushort)s.ValidationType).ToString(CultureInfo.InvariantCulture)
						};
					default:
						return new IdentityAttributeValidationDefinitionDto
						{
							AttributeType = ((uint)s.AttributeType).ToString(CultureInfo.InvariantCulture),
							ValidationType = ((ushort)s.ValidationType).ToString(CultureInfo.InvariantCulture)
						};
				}
			}));
		}

        [HttpPost("UpdateIdentityAttributeValidationDefinitions")]
        public IActionResult UpdateIdentityAttributeValidationDefinitions([FromBody] IdentityAttributeValidationDefinitionsDto identityAttributeValidationDefinitions)
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

            List<SpIdenitityValidation> spIdenitityValidations = identityAttributeValidationDefinitions.IdentityAttributeValidationDefinitions.Select(i => new SpIdenitityValidation { AccountId = accountId, AttributeType = (AttributeType)uint.Parse(i.AttributeType, CultureInfo.InvariantCulture), ValidationType = (ValidationType)ushort.Parse(i.ValidationType, CultureInfo.InvariantCulture), NumericCriterion = i.CriterionValue != null ? ushort.Parse(i.CriterionValue, CultureInfo.InvariantCulture) : new ushort?(), GroupIdCriterion = i.CriterionValue?.HexStringToByteArray() }).ToList();

            _dataAccessService.AdjustSpIdenitityValidations(accountId, spIdenitityValidations);

            return Ok();
        }

        [HttpGet("GetEmployeeGroups")]
        public IActionResult GetEmployeeGroups()
        {
            List<EmployeeGroupDto> employeeGroups = new List<EmployeeGroupDto>();

            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

            employeeGroups = _dataAccessService.GetSpEmployeeGroups(accountId).Select(g => new EmployeeGroupDto { GroupId = g.SpEmployeeGroupId, GroupName = g.GroupName }).ToList();

            return Ok(employeeGroups);
        }

        [HttpPost("AddEmployeeGroup")]
        public IActionResult AddEmployeeGroup([FromBody] EmployeeGroupDto employeeGroup)
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

            employeeGroup.GroupId = _dataAccessService.AddSpEmployeeGroup(accountId, employeeGroup.GroupName);

            return Ok(employeeGroup);
        }

        [HttpDelete("DeleteEmployeeGroup/{groupId}")]
        public IActionResult DeleteEmployeeGroup(ulong groupId)
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

            _dataAccessService.RemoveSpEmployeeGroup(accountId, groupId);

            return Ok();
        }

        [HttpGet("GetEmployees")]
        public IActionResult GetEmployees()
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

            return Ok(_dataAccessService.GetAllSpEmployees(accountId).Select(e => new EmployeeDto
            {
                EmployeeId = e.SpEmployeeId,
                Description = e.Description,
                RawRootAttribute = e.RootAttributeRaw,
                RegistrationCommitment = e.RegistrationCommitment,
                GroupId = e.SpEmployeeGroup?.SpEmployeeGroupId ?? 0
            }));
        }

        [HttpPost("AddEmployee")]
        public IActionResult AddEmployee([FromBody] EmployeeDto employee)
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);
            byte[] assetId = _assetsService.GenerateAssetId(AttributeType.IdCard, employee.RawRootAttribute);

            employee.EmployeeId = _dataAccessService.AddSpEmployee(accountId, employee.Description, employee.RawRootAttribute, assetId.ToHexString(), employee.GroupId);
            employee.AssetId = assetId.ToHexString();

            return Ok(employee);
        }

        [HttpPost("UpdateEmployee")]
        public IActionResult UpdateEmployee([FromBody] EmployeeDto employee)
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

            _dataAccessService.UpdateSpEmployeeCategory(accountId, employee.EmployeeId, employee.GroupId);
            StatePersistency statePersistency = _executionContextManager.ResolveStateExecutionServices(accountId);
            statePersistency.TransactionsService.IssueCancelEmployeeRecord(employee.RegistrationCommitment.HexStringToByteArray());

            return Ok(employee);
        }

        [HttpDelete("DeleteEmployee/{employeeId}")]
        public IActionResult DeleteEmployee(ulong employeeId)
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

            SpEmployee spEmployee = _dataAccessService.RemoveSpEmployee(accountId, employeeId);
            StatePersistency statePersistency = _executionContextManager.ResolveStateExecutionServices(accountId);

            if (!string.IsNullOrEmpty(spEmployee.RegistrationCommitment))
            {
                statePersistency.TransactionsService.IssueCancelEmployeeRecord(spEmployee.RegistrationCommitment.HexStringToByteArray());
            }

            return Ok();
        }

        [HttpGet("GetDocuments")]
        public IActionResult GetDocuments()
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

            IEnumerable<DocumentDto> documents = _dataAccessService.GetSpDocuments(accountId).Select(d => new DocumentDto
            {
                DocumentId = d.SpDocumentId,
                DocumentName = d.DocumentName,
                Hash = d.Hash,
                AllowedSigners = (d.AllowedSigners?.Select(s => new AllowedSignerDto
                {
                    AllowedSignerId = s.SpDocumentAllowedSignerId,
                    GroupName = s.GroupName,
                    GroupOwner = s.GroupIssuer
                }) ?? Array.Empty<AllowedSignerDto>()).ToList(),
                Signatures = (d.DocumentSignatures?.Select( s => new DocumentSignatureDto
                {
                    DocumentId = s.Document.SpDocumentId,
                    SignatureId = s.SpDocumentSignatureId,
                    DocumentHash = s.Document.Hash,
                    DocumentRecordHeight = s.DocumentRecordHeight,
                    SignatureRecordHeight = s.SignatureRecordHeight
                }) ?? Array.Empty<DocumentSignatureDto>()).ToList()
            });

            return Ok(documents);
        }

        [HttpPost("AddDocument")]
        public IActionResult AddDocument([FromBody] DocumentDto documentDto)
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

            documentDto.DocumentId = _dataAccessService.AddSpDocument(accountId, documentDto.DocumentName, documentDto.Hash);
            SpDocument document = _dataAccessService.GetSpDocument(accountId, documentDto.DocumentId);

            StatePersistency statePersistency = _executionContextManager.ResolveStateExecutionServices(accountId);
            statePersistency.TransactionsService.IssueDocumentRecord(document.Hash.HexStringToByteArray(), document.AllowedSigners?.Select(s => s.GroupCommitment.HexStringToByteArray()).ToArray());

            return Ok(documentDto);
        }

        [HttpDelete("DeleteDocument/{documentId}")]
        public IActionResult DeleteDocument(ulong documentId)
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

            _dataAccessService.RemoveSpDocument(accountId, documentId);

            return Ok();
        }

        [HttpPost("AddAllowedSigner/{documentId}")]
        public IActionResult AddAllowedSigner(ulong documentId, [FromBody] AllowedSignerDto allowedSigner)
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

			byte[] groupAssetId = _assetsService.GenerateAssetId(AttributeType.EmployeeGroup, allowedSigner.GroupOwner + allowedSigner.GroupName);
			byte[] blindingFactor = ConfidentialAssetsHelper.GetRandomSeed();
			byte[] groupCommitment = ConfidentialAssetsHelper.GetAssetCommitment(groupAssetId, blindingFactor);

            allowedSigner.AllowedSignerId = _dataAccessService.AddSpDocumentAllowedSigner(accountId, documentId, allowedSigner.GroupOwner, allowedSigner.GroupName, groupCommitment.ToHexString(), blindingFactor.ToHexString());

			SpDocument document = _dataAccessService.GetSpDocument(accountId, documentId);

			StatePersistency statePersistency = _executionContextManager.ResolveStateExecutionServices(accountId);
			statePersistency.TransactionsService.IssueDocumentRecord(document.Hash.HexStringToByteArray(), document.AllowedSigners.Select(s => s.GroupCommitment.HexStringToByteArray()).ToArray());

			return Ok(allowedSigner);
        }

        [HttpDelete("DeleteAllowedSigner/{allowedSignerId}")]
        public IActionResult DeleteAllowedSigner(ulong allowedSignerId)
        {
            ulong accountId = ulong.Parse(User.Identity.Name, CultureInfo.InvariantCulture);

            ulong documentId = _dataAccessService.RemoveSpDocumentAllowedSigner(accountId, allowedSignerId);
			SpDocument document = _dataAccessService.GetSpDocument(accountId, documentId);

			StatePersistency statePersistency = _executionContextManager.ResolveStateExecutionServices(accountId);
			statePersistency.TransactionsService.IssueDocumentRecord(document.Hash.HexStringToByteArray(), document.AllowedSigners.Select(s => s.GroupCommitment.HexStringToByteArray()).ToArray());

			return Ok();
        }

        [HttpPost("CalculateFileHash"), DisableRequestSizeLimit]
        public IActionResult CalculateFileHash()
        {
            var file = Request.Form.Files[0];

            if (file.Length > 0)
            {
                using (var stream = new MemoryStream())
                {
                    file.CopyTo(stream);

                    byte[] hash = _hashCalculation.CalculateHash(stream.ToArray());

                    return Ok(new { documentName = file.FileName, hash = hash.ToHexString() });
                }
            }
            else
            {
                return BadRequest();
            }
        }
    }
}
