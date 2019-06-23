using Wist.Core.Architecture;

namespace Wist.Server.Portal.Services
{
	[ServiceContract]
    public interface IExecutionContextManager
	{
		void InitializeStateExecutionServices(ulong accountId, byte[] secretKey);
		void InitializeUtxoExecutionServices(ulong accountId, byte[] secretSpendKey, byte[] secretViewKey, byte[] pwdSecretKey);
        StatePersistency ResolveStateExecutionServices(ulong accountId);
		UtxoPersistency ResolveUtxoExecutionServices(ulong accountId);
		void UnregisterExecutionServices(ulong accountId);
        void Clean(ulong accountId);
    }
}