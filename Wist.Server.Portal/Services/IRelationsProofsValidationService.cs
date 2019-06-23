using Wist.Blockchain.Core.DataModel.UtxoConfidential;
using Wist.Client.Common.Interfaces;
using Wist.Core.Architecture;

namespace Wist.Server.Portal.Services
{
    [ServiceContract]
    public interface IRelationsProofsValidationService
    {
        RelationProofsValidationResults VerifyRelationProofs(GroupsRelationsProofs relationsProofs, IUtxoClientCryptoService clientCryptoService);
    }
}
