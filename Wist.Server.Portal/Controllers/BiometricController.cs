using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Wist.Client.Common.Identities;
using Wist.Client.Common.Interfaces;
using Wist.Client.DataModel.Enums;
using Wist.Client.DataModel.Services;
using Wist.Core.Configuration;
using Wist.Server.Portal.Configuration;
using Wist.Server.Portal.Dtos;
using Wist.Core.ExtensionMethods;
using System.Linq;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace Wist.Server.Portal.Controllers
{
    [Route("[controller]")]
    public class BiometricController : ControllerBase
    {
        private readonly IFacesService _facesService;
        private readonly IDataAccessService _dataAccessService;
        private readonly IAssetsService _assetsService;
        private readonly IPortalConfiguration _portalConfiguration;

        public BiometricController(IFacesService facesService, IConfigurationService configurationService, IDataAccessService externalDataAccessService, IAssetsService assetsService)
        {
            _facesService = facesService;
            _facesService.Initialize();
            _dataAccessService = externalDataAccessService;
            _assetsService = assetsService;
            _portalConfiguration = configurationService.Get<IPortalConfiguration>();
        }

        [AllowAnonymous]
        [HttpPost("RegisterPerson")]
        public async Task<IActionResult> RegisterPerson([FromBody] BiometricPersonDataDto biometricPersonData)
        {
            PersonFaceData personFaceData = new PersonFaceData
            {
                PersonGroupId = _portalConfiguration.FacePersonGroupId + (_portalConfiguration.DemoMode ? string.Empty : biometricPersonData.Requester),
                PersonGuid = Guid.NewGuid(),
                UserData = biometricPersonData.PersonData,
                ImageContent = Convert.FromBase64String(biometricPersonData.ImageString)
            };

            Guid guid = await _facesService.AddPerson(personFaceData).ConfigureAwait(false);

            _dataAccessService.AddBiometricRecord(biometricPersonData.PersonData, guid);

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("VerifyPersonFace")]
        public async Task<IActionResult> VerifyPersonFace([FromBody] BiometricPersonDataDto biometricPersonData)
        {
            Guid guid = _dataAccessService.FindPersonGuid(biometricPersonData.PersonData);
            string personGroupId = _portalConfiguration.FacePersonGroupId + (_portalConfiguration.DemoMode ? string.Empty : biometricPersonData.Requester);
            if (guid == Guid.Empty)
            {
                if (_portalConfiguration.DemoMode)
                {
                    Person person = _facesService.GetPersons(personGroupId).FirstOrDefault(p => p.UserData.Equals(biometricPersonData.PersonData, StringComparison.InvariantCultureIgnoreCase));
                    if (person != null)
                    {
                        guid = person.PersonId;
                    }
                }
                else
                {
                    return BadRequest();
                }
            }

            byte[] imageContent = Convert.FromBase64String(biometricPersonData.ImageString);

            Tuple<bool, double> res = await _facesService.VerifyPerson(personGroupId, guid, imageContent).ConfigureAwait(false);

            return Ok(new { Result = res.Item1, Probability = res.Item2 });
        }

		[AllowAnonymous]
		[HttpPost("SignPersonFaceVerification")]
        public async Task<IActionResult> SignPersonFaceVerification([FromBody] BiometricPersonDataForSignatureDto biometricPersonData)
        {
            byte[] assetId = _assetsService.GenerateAssetId(AttributeType.PassportPhoto, biometricPersonData.ImageSource);
            Guid guid = _dataAccessService.FindPersonGuid(assetId.ToHexString());
            if(guid == Guid.Empty)
            {
                return BadRequest();
            }

            byte[] imageContent = Convert.FromBase64String(biometricPersonData.ImageTarget);
            byte[] auxBytes = Convert.FromBase64String(biometricPersonData.AuxMessage);

			byte[] msg = new byte[assetId.Length + auxBytes?.Length ?? 0];

            Array.Copy(assetId, 0, msg, 0, assetId.Length);

			if ((auxBytes?.Length ?? 0) > 0)
			{
				Array.Copy(auxBytes, 0, msg, assetId.Length, auxBytes.Length);
			}

            Tuple<bool, double> res = await _facesService.VerifyPerson(_portalConfiguration.FacePersonGroupId, guid, imageContent).ConfigureAwait(false);

            if(res.Item1)
            {
                Tuple<byte[], byte[]> signRes = _facesService.Sign(msg);

                return Ok(new BiometricSignedVerificationDto { PublicKey = signRes.Item1.ToHexString(), Signature = signRes.Item2.ToHexString() });
            }

            return BadRequest();
        }
    }
}
