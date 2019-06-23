using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using Wist.Blockchain.Core.DataModel.Transactional;
using Wist.Client.Common.Interfaces;
using Wist.Client.DataModel.Enums;
using Wist.Core.Models;
using Wist.Core.ExtensionMethods;
using Wist.Server.Portal.Dtos;
using Wist.Server.Portal.Hubs;
using Wist.Client.DataModel.Services;
using Wist.Client.Common.Communication.SynchronizerNotifications;
using System.Threading.Tasks.Dataflow;
using System.Globalization;
using Wist.Client.DataModel.Model;
using Wist.Core.Tracking;
using Wist.Blockchain.Core.DataModel.UtxoConfidential;

namespace Wist.Server.Portal.Services
{
	public class UserIdentitiesUpdater : IObserver<SynchronizerNotificationBase>
	{
		private readonly ulong _accountId;
		private readonly IUtxoClientCryptoService _clientCryptoService;
		private readonly IAssetsService _assetsService;
		private readonly IDataAccessService _dataAccessService;
		private readonly IHubContext<IdentitiesHub> _idenitiesHubContext;
        private readonly IRelationsProofsValidationService _relationsProofsValidationService;
        private readonly ITrackingService _trackingService;

		public UserIdentitiesUpdater(ulong accountId, IUtxoClientCryptoService clientCryptoService, IAssetsService assetsService, IDataAccessService externalDataAccessService, IHubContext<IdentitiesHub> idenitiesHubContext, IRelationsProofsValidationService relationsProofsValidationService, ITrackingService trackingService)
		{
			_accountId = accountId;
			_clientCryptoService = clientCryptoService;
			_assetsService = assetsService;
			_dataAccessService = externalDataAccessService;
			_idenitiesHubContext = idenitiesHubContext;
            _relationsProofsValidationService = relationsProofsValidationService;
            _trackingService = trackingService;
			PipeIn = new ActionBlock<PacketBase>(p => 
			{
                try
                {
                    if (p is TransferAssetToUtxo packet)
                    {
                        _clientCryptoService.DecodeEcdhTuple(packet.TransferredAsset.EcdhTuple, packet.TransactionPublicKey, out byte[] blindingFactor, out byte[] assetId);
                        AttributeType attributeType = _assetsService.GetAttributeType(assetId);

                        _idenitiesHubContext.Clients.Group(_accountId.ToString(CultureInfo.InvariantCulture)).SendAsync("PushAttribute", new UserAttributeDto { AttributeType = attributeType.ToString(), Source = packet.Signer.ArraySegment.Array.ToHexString(), AssetId = assetId.ToHexString(), OriginalBlindingFactor = blindingFactor.ToHexString(), OriginalCommitment = packet.TransferredAsset.AssetCommitment.ToHexString(), LastBlindingFactor = blindingFactor.ToHexString(), LastCommitment = packet.TransferredAsset.AssetCommitment.ToHexString(), LastTransactionKey = packet.TransactionPublicKey.ToHexString(), LastDestinationKey = packet.DestinationKey.ToHexString(), Validated = false, IsOverriden = false });
                    }
                    else if (p is GroupsRelationsProofs relationsProofs && _clientCryptoService.CheckTarget(relationsProofs.DestinationKey2, relationsProofs.TransactionPublicKey))
                    {
                        RelationProofsValidationResults validationResults = _relationsProofsValidationService.VerifyRelationProofs(relationsProofs, _clientCryptoService);

                        _idenitiesHubContext.Clients.Group(_accountId.ToString(CultureInfo.InvariantCulture)).SendAsync("PushRelationValidation", validationResults);
                    }
                }
                catch
                {
                }
			});
		}

		public ITargetBlock<PacketBase> PipeIn { get; set; }

		public void OnCompleted() => throw new NotImplementedException();
		public void OnError(Exception error) => throw new NotImplementedException();

        public void OnNext(SynchronizerNotificationBase value)
        {
            ProcessEligibilityCommitmentsDisabled(value);

            NotifyUserAttributeLastUpdate(value);

            NotifyCompromisedKeyImage(value);
        }

		private void ProcessEligibilityCommitmentsDisabled(SynchronizerNotificationBase value)
		{
			if (value is EligibilityCommitmentsDisabled eligibilityCommitmentsDisabled)
			{
				_trackingService.TrackEvent($"{nameof(UserIdentitiesUpdater)}");
				IEnumerable<UserRootAttribute> userRootAttributes = _dataAccessService.GetUserAttributes(_accountId).Where(u => eligibilityCommitmentsDisabled.DisabledIds.Contains(u.UserAttributeId));

				foreach (UserRootAttribute userAttribute in userRootAttributes)
				{
					NotifyAttributeUpdate(userAttribute);
				}
			}
		}

        private void NotifyCompromisedKeyImage(SynchronizerNotificationBase value)
        {
            if (value is CompromisedKeyImage compromisedKeyImage)
            {
                _dataAccessService.SetAccountCompromised(_accountId);
                _idenitiesHubContext.Clients.Group(_accountId.ToString(CultureInfo.InvariantCulture)).SendAsync("PushUnauthorizedUse", new UnauthorizedUseDto { KeyImage = compromisedKeyImage.KeyImage.ToHexString(), Target = compromisedKeyImage.Target.ToHexString() });
            }
        }

        private void NotifyUserAttributeLastUpdate(SynchronizerNotificationBase value)
        {
            if (value is UserAttributeStateUpdate userAttributeStateUpdate)
            {
                _idenitiesHubContext.Clients.Group(_accountId.ToString(CultureInfo.InvariantCulture)).SendAsync("PushUserAttributeLastUpdate",
                    new UserAttributeLastUpdateDto
                    {
                        AssetId = userAttributeStateUpdate.AssetId.ToHexString(),
                        LastBlindingFactor = userAttributeStateUpdate.BlindingFactor.ToHexString(),
                        LastCommitment = userAttributeStateUpdate.AssetCommitment.ToHexString(),
                        LastTransactionKey = userAttributeStateUpdate.TransactionKey.ToHexString(),
                        LastDestinationKey = userAttributeStateUpdate.DestinationKey.ToHexString()
                    });
            }
        }

        private void NotifyAttributeUpdate(UserRootAttribute userAttribute)
        {
            UserAttributeDto userAttributeDto = new UserAttributeDto
            {
                AttributeType = ((AttributeType)userAttribute.AttributeType).ToString(),
                Source = userAttribute.Source,
                AssetId = userAttribute.AssetId.ToHexString(),
                OriginalCommitment = userAttribute.OriginalCommitment.ToHexString(),
                OriginatingCommitment = userAttribute.IssuanceCommitment.ToHexString(),
                LastCommitment = userAttribute.LastCommitment.ToHexString(),
                Content = userAttribute.Content,
                LastBlindingFactor = userAttribute.LastBlindingFactor.ToHexString(),
                LastDestinationKey = userAttribute.LastDestinationKey.ToHexString(),
                LastTransactionKey = userAttribute.LastTransactionKey.ToHexString(),
                OriginalBlindingFactor = userAttribute.OriginalBlindingFactor.ToHexString(),
                Validated = false,
                IsOverriden = true
            };

            _idenitiesHubContext.Clients.Group(_accountId.ToString(CultureInfo.InvariantCulture)).SendAsync("PushUserAttributeUpdate", userAttributeDto);
        }
	}
}
