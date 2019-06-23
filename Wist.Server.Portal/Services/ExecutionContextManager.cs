using CommonServiceLocator;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Wist.Blockchain.Core.Parsers;
using Wist.Client.Common.Communication;
using Wist.Client.Common.Crypto;
using Wist.Client.Common.Interfaces;
using Wist.Client.DataModel.Services;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Configuration;
using Wist.Core.Logging;
using Wist.Core.Tracking;
using Wist.Crypto.ConfidentialAssets;
using Wist.Server.Portal.Hubs;

namespace Wist.Server.Portal.Services
{
	[RegisterDefaultImplementation(typeof(IExecutionContextManager), Lifetime =	LifetimeManagement.Singleton)]
	public class ExecutionContextManager : IExecutionContextManager
	{
		private readonly Dictionary<ulong, StatePersistency> _statePersistencyItems = new Dictionary<ulong, StatePersistency>();
		private readonly Dictionary<ulong, UtxoPersistency> _utxoPersistencyItems = new Dictionary<ulong, UtxoPersistency>();
        private readonly Dictionary<ulong, ICollection<IDisposable>> _accountIdCancellationList;
        private readonly IHubContext<IdentitiesHub> _identitiesHubContext;
		private readonly IAssetsService _assetsService;
		private readonly IDataAccessService _dataAccessService;
		private readonly IIdentityAttributesService _identityAttributesService;
		private readonly IBlockParsersRepositoriesRepository _blockParsersRepositoriesRepository;
		private readonly IAppConfig _appConfig;
        private readonly IGatewayService _gatewayService;
		private readonly ITrackingService _trackingService;
        private readonly ILoggerService _loggerService;
        private readonly IRelationsProofsValidationService _relationsProofsValidationService;

        public ExecutionContextManager(IHubContext<IdentitiesHub> identitiesHubContext, IAssetsService assetsService, IDataAccessService dataAccessService, IIdentityAttributesService identityAttributesService, IBlockParsersRepositoriesRepository blockParsersRepositoriesRepository, IAppConfig appConfig, IGatewayService gatewayService, ITrackingService trackingService, ILoggerService loggerService, IRelationsProofsValidationService relationsProofsValidationService)
		{
            _accountIdCancellationList = new Dictionary<ulong, ICollection<IDisposable>>();
            _identitiesHubContext = identitiesHubContext;
			_assetsService = assetsService;
			_dataAccessService = dataAccessService;
			_identityAttributesService = identityAttributesService;
			_blockParsersRepositoriesRepository = blockParsersRepositoriesRepository;
			_appConfig = appConfig;
            _gatewayService = gatewayService;
			_trackingService = trackingService;
            _loggerService = loggerService;
            _relationsProofsValidationService = relationsProofsValidationService;
        }

        public void InitializeStateExecutionServices(ulong accountId, byte[] secretKey)
		{
			if (_statePersistencyItems.ContainsKey(accountId))
				return;

			IPacketsProvider packetsProvider = ServiceLocator.Current.GetInstance<IPacketsProvider>();
			IStateTransactionsService transactionsService = ServiceLocator.Current.GetInstance<StateTransactionsService>();
			IStateClientCryptoService clientCryptoService = ServiceLocator.Current.GetInstance<StateClientCryptoService>();
			IWalletSynchronizer walletSynchronizer = ServiceLocator.Current.GetInstance<StateWalletSynchronizer>();

			CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

			packetsProvider.Initialize(accountId, cancellationTokenSource.Token);
			clientCryptoService.Initialize(secretKey);
			transactionsService.Initialize(clientCryptoService, _gatewayService.GetLastBlockHeight(ConfidentialAssetsHelper.GetPublicKey(secretKey)));
			transactionsService.PipeOutTransactions.LinkTo(_gatewayService.PipeInTransactions);

			ServiceProviderUpdater updater = new ServiceProviderUpdater(accountId, clientCryptoService, _assetsService, _dataAccessService, _identityAttributesService, _blockParsersRepositoriesRepository, _gatewayService, transactionsService, _identitiesHubContext, _appConfig, _loggerService);

			walletSynchronizer.Initialize(accountId, clientCryptoService, _gatewayService, cancellationTokenSource.Token);

			packetsProvider.PipeOut.LinkTo(walletSynchronizer.PipeIn);
			walletSynchronizer.PipeOut.LinkTo(updater.PipeIn);

			walletSynchronizer.Start();
			packetsProvider.Start();

			var state = new StatePersistency
			{
				AccountId = accountId,
				PacketsProvider = packetsProvider,
				TransactionsService = transactionsService,
				ClientCryptoService = clientCryptoService,
				WalletSynchronizer = walletSynchronizer,
				CancellationTokenSource = cancellationTokenSource
			};

			_statePersistencyItems.Add(accountId, state);
		}

		public void InitializeUtxoExecutionServices(ulong accountId, byte[] secretSpendKey, byte[] secretViewKey, byte[] pwdSecretKey)
        {
            if (_utxoPersistencyItems.ContainsKey(accountId))
                return;

			IPacketsProvider packetsProvider = ServiceLocator.Current.GetInstance<IPacketsProvider>();
			IUtxoTransactionsService transactionsService = ServiceLocator.Current.GetInstance<UtxoTransactionsService>();
            IUtxoClientCryptoService clientCryptoService = ServiceLocator.Current.GetInstance<UtxoClientCryptoService>();
            UtxoWalletSynchronizer walletSynchronizer = ServiceLocator.Current.GetInstance<UtxoWalletSynchronizer>();

			CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

			packetsProvider.Initialize(accountId, cancellationTokenSource.Token);
			clientCryptoService.Initialize(secretSpendKey, secretViewKey, pwdSecretKey);
			transactionsService.Initialize(clientCryptoService);
            transactionsService.PipeOutTransactions.LinkTo(_gatewayService.PipeInTransactions);
            transactionsService.PipeOutKeyImages.LinkTo(walletSynchronizer.PipeInKeyImages);

            UserIdentitiesUpdater userIdentitiesUpdater = new UserIdentitiesUpdater(accountId, clientCryptoService, _assetsService, _dataAccessService, _identitiesHubContext, _relationsProofsValidationService, _trackingService);
            
            walletSynchronizer.Initialize(accountId, clientCryptoService, _gatewayService, cancellationTokenSource.Token);

			packetsProvider.PipeOut.LinkTo(walletSynchronizer.PipeIn);
			walletSynchronizer.PipeOut.LinkTo(userIdentitiesUpdater.PipeIn);

			walletSynchronizer.Start();
			packetsProvider.Start();

            AddSubscriberToDictionary(accountId, walletSynchronizer.Subscribe(userIdentitiesUpdater));

            var state = new UtxoPersistency
            {
                AccountId = accountId,
				PacketsProvider = packetsProvider,
                TransactionsService = transactionsService,
                ClientCryptoService = clientCryptoService,
                WalletSynchronizer = walletSynchronizer,
                CancellationTokenSource = cancellationTokenSource
            };
            _utxoPersistencyItems.Add(accountId, state);
        }

        public void Clean(ulong accountId)
        {
            if (_accountIdCancellationList.ContainsKey(accountId))
            {
                _accountIdCancellationList[accountId].ToList().ForEach(t => t.Dispose());

                if (_utxoPersistencyItems.ContainsKey(accountId))
                {
                    _utxoPersistencyItems.Remove(accountId);
                }
                if (_statePersistencyItems.ContainsKey(accountId))
                {
                    _statePersistencyItems.Remove(accountId);
                }
            }
        }

        public StatePersistency ResolveStateExecutionServices(ulong accountId)
        {
            StatePersistency statePersistency = _statePersistencyItems[accountId];

            return statePersistency;
		}

		public UtxoPersistency ResolveUtxoExecutionServices(ulong accountId)
		{
			UtxoPersistency utxoPersistency = _utxoPersistencyItems[accountId];

            return utxoPersistency;
        }

		public void UnregisterExecutionServices(ulong accountId)
		{
			if(_statePersistencyItems.ContainsKey(accountId))
			{
				StatePersistency persistency = _statePersistencyItems[accountId];
				persistency.CancellationTokenSource.Cancel();
				persistency.WalletSynchronizer.Dispose();

				_statePersistencyItems.Remove(accountId);
				persistency.TransactionsService = null;
				persistency.WalletSynchronizer = null;
				persistency.ClientCryptoService = null;
			}
			else if (_utxoPersistencyItems.ContainsKey(accountId))
			{
				UtxoPersistency persistency = _utxoPersistencyItems[accountId];
				persistency.CancellationTokenSource.Cancel();
				persistency.WalletSynchronizer.Dispose();

				_utxoPersistencyItems.Remove(accountId);
				persistency.TransactionsService = null;
				persistency.WalletSynchronizer = null;
				persistency.ClientCryptoService = null;
			}
		}

        private void AddSubscriberToDictionary(ulong accountId, IDisposable disposable)
        {
            if (_accountIdCancellationList.ContainsKey(accountId))
            {
                if (!_accountIdCancellationList[accountId].Contains(disposable))
                {
                    _accountIdCancellationList[accountId].Add(disposable);
                }
            }
            else
            {
                _accountIdCancellationList.Add(accountId, new List<IDisposable>() { disposable });
            }
        }
	}
}
