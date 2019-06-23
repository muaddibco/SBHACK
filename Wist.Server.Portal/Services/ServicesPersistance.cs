using System.Threading;
using Wist.Client.Common.Interfaces;

namespace Wist.Server.Portal.Services
{
	public abstract class PersistencyBase
	{
		public ulong AccountId { get; set; }
		public IPacketsProvider PacketsProvider { get; set; }
		public IWalletSynchronizer WalletSynchronizer { get; set; }

		public CancellationTokenSource CancellationTokenSource { get; set; }
	}

    public class StatePersistency : PersistencyBase
    {
        public IStateTransactionsService TransactionsService { get; set; }
        public IStateClientCryptoService ClientCryptoService { get; set; }
    }

    public class UtxoPersistency : PersistencyBase
    {
        public IUtxoTransactionsService TransactionsService { get; set; }
        public IUtxoClientCryptoService ClientCryptoService { get; set; }
    }
}
