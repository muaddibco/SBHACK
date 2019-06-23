using System.Linq;
using Wist.Blockchain.Core.DataModel.UtxoConfidential;
using Wist.Client.Common.Interfaces;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Crypto.ConfidentialAssets;
using Wist.Core.ExtensionMethods;
using Wist.Client.Common.Interfaces.Inputs;
using Wist.Client.DataModel.Enums;
using System;

namespace Wist.Server.Portal.Services
{
    [RegisterDefaultImplementation(typeof(IRelationsProofsValidationService), Lifetime = LifetimeManagement.Singleton)]
    public class RelationsProofsValidationService : IRelationsProofsValidationService
    {
        private readonly IGatewayService _gatewayService;
        private readonly IUtxoClientCryptoService _utxoClientCryptoService;
        private readonly IAssetsService _assetsService;

        public RelationsProofsValidationService(IGatewayService gatewayService, IAssetsService assetsService)
        {
            _gatewayService = gatewayService;
            _assetsService = assetsService;
        }

        public RelationProofsValidationResults VerifyRelationProofs(GroupsRelationsProofs relationsProofs, IUtxoClientCryptoService clientCryptoService)
        {
            //TODO: need to add eligibility proofs

            RelationProofsValidationResults validationResults = new RelationProofsValidationResults();

            clientCryptoService.DecodeEcdhTuple(relationsProofs.EcdhTuple, relationsProofs.TransactionPublicKey, out byte[] sessionKey, out byte[] imageHash);

            RelationProofSession proofSession = _gatewayService.PopRelationProofSession(sessionKey.ToHexString());

            byte[] image = Convert.FromBase64String(proofSession.ImageContent);
            validationResults.ImageContent = proofSession.ImageContent;
            byte[] imageHashFromSession = ConfidentialAssetsHelper.FastHash256(image);

            validationResults.IsImageCorrect = imageHashFromSession.Equals32(imageHash);

            foreach (var relationEntry in proofSession.RelationEntries)
            {
                bool isRelationContentMatching = false;

                foreach (var relationProof in relationsProofs.RelationProofs)
                {
                    byte[] registrationCommitment = relationProof.RelationProof.AssetCommitments[0];
                    byte[] groupNameCommitment = _gatewayService.GetEmployeeRecordGroup(relationProof.GroupOwner, registrationCommitment);
                    bool isRelationProofCorrect = groupNameCommitment != null ? ConfidentialAssetsHelper.VerifySurjectionProof(relationProof.RelationProof, relationsProofs.AssetCommitment) : false;

                    if (isRelationProofCorrect)
                    {
                        byte[] relationAssetId = _assetsService.GenerateAssetId(AttributeType.EmployeeGroup, relationProof.GroupOwner.ToHexString() + relationEntry.RelatedAssetName);
                        if (ConfidentialAssetsHelper.VerifyIssuanceSurjectionProof(relationProof.GroupNameProof, groupNameCommitment, new byte[][] { relationAssetId }))
                        {
                            isRelationContentMatching = true;
                            break;
                        }
                    }
                }

                validationResults.ValidationResults.Add(new RelationProofValidationResult { RelatedAttributeOwner = relationEntry.RelatedAssetOwnerName, RelatedAttributeContent = relationEntry.RelatedAssetName, IsRelationCorrect = isRelationContentMatching });
            }

            return validationResults;
        }
    }
}
