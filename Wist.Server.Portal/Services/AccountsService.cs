using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Wist.Core.ExtensionMethods;
using Wist.Client.Common.Entities;
using Wist.Client.DataModel.Enums;
using Wist.Client.DataModel.Services;
using Wist.Crypto.ConfidentialAssets;
using Account = Wist.Client.DataModel.Model.Account;
using Wist.Server.Portal.Exceptions;
using Chaos.NaCl;
using Wist.Client.Common.Interfaces;
using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;

namespace Wist.Server.Portal.Services
{
	[RegisterDefaultImplementation(typeof(IAccountsService), Lifetime = LifetimeManagement.Singleton)]
    public class AccountsService : IAccountsService
	{
		private readonly IDataAccessService _dataAccessService;
		private readonly IExecutionContextManager _executionContextManager;
		private readonly IIdentityAttributesService _identityAttributesService;
        private readonly IGatewayService _gatewayService;

		public AccountsService(IDataAccessService dataAccessService, IExecutionContextManager executionContextManager, IIdentityAttributesService identityAttributesService, IGatewayService gatewayService)
		{
			_dataAccessService = dataAccessService;
			_executionContextManager = executionContextManager;
			_identityAttributesService = identityAttributesService;
            _gatewayService = gatewayService;
		}

		public AccountDescriptor Authenticate(ulong accountId, string password)
		{
			Account account = _dataAccessService.GetAccount(accountId);

			if(account == null)
			{
				throw new AccountNotFoundException(accountId);
			}

			AccountDescriptor accountDescriptor = null;

			if (account.AccountType == AccountType.User)
            {
                accountDescriptor = AuthenticateUtxoAccount(new AuthenticationInput { Password = password, Account = account });
            }
            else
            {
                accountDescriptor = AuthenticateStateAccount(new AuthenticationInput { Password = password, Account = account });
            }

            return accountDescriptor;
		}

        private AccountDescriptor AuthenticateStateAccount(AuthenticationInput authenticationInput)
        {
            AccountDescriptor accountDescriptor = null;
            byte[] key = DecryptStateKeys(authenticationInput.Account, authenticationInput.Password);
            if (key != null)
            {
                accountDescriptor = new AccountDescriptor { AccountType = authenticationInput.Account.AccountType, SecretSpendKey = key, PublicSpendKey = authenticationInput.Account.PublicSpendKey, AccountInfo = authenticationInput.Account.AccountInfo, AccountId = authenticationInput.Account.AccountId };

                _executionContextManager.InitializeStateExecutionServices(authenticationInput.Account.AccountId, accountDescriptor.SecretSpendKey);
            }

            return accountDescriptor;
        }

        private AccountDescriptor AuthenticateUtxoAccount(AuthenticationInput authenticationInput)
        {
            AccountDescriptor accountDescriptor = null;
            Tuple<byte[], byte[]> keys = DecryptUtxoKeys(authenticationInput.Account, authenticationInput.Password);
            if (keys != null)
            {
                accountDescriptor = new AccountDescriptor { AccountType = authenticationInput.Account.AccountType, SecretSpendKey = keys.Item1, SecretViewKey = keys.Item2, PublicSpendKey = authenticationInput.Account.PublicSpendKey, PublicViewKey = authenticationInput.Account.PublicViewKey, AccountInfo = authenticationInput.Account.AccountInfo, AccountId = authenticationInput.Account.AccountId };

                byte[] pwdBytes = Encoding.ASCII.GetBytes(authenticationInput.Password);

                byte[] pwdHash = ConfidentialAssetsHelper.FastHash256(pwdBytes);

                _executionContextManager.InitializeUtxoExecutionServices(authenticationInput.Account.AccountId, accountDescriptor.SecretSpendKey, accountDescriptor.SecretViewKey, pwdHash);
            }

            return accountDescriptor;
        }

        public void Clean(ulong accountId)
        {
            _executionContextManager.Clean(accountId);
        }

		public void Create(AccountType accountType, string accountInfo, string password)
		{
			ulong accountId = StoreEncryptedAccount(accountType, accountInfo, password);
		}

		public void Delete(ulong accountId)
		{
            _dataAccessService.RemoveAccount(accountId);
		}

		public List<Account> GetAll()
		{
			return _dataAccessService.GetAccounts();
		}

		public Account GetById(ulong accountId)
		{
			return _dataAccessService.GetAccount(accountId);
		}

        public Account GetByPublicKey(byte[] publicKey)
        {
            return _dataAccessService.GetAccount(publicKey);
        }

        public void Update(AccountDescriptor user, string password = null) => throw new NotImplementedException();


		public ulong DuplicateAccount(ulong id, string accountInfo)
		{
			ulong accountIdNew = _dataAccessService.DuplicateUserAccount(id, accountInfo);
            _dataAccessService.DuplicateAssociatedAttributes(id, accountIdNew);
            return accountIdNew;
		}

		#region Private Functions

		private ulong StoreEncryptedAccount(AccountType accountType, string accountInfo, string passphrase)
		{
			byte[] secretSpendKey = ConfidentialAssetsHelper.GetRandomSeed();
			byte[] secretViewKey = (accountType == AccountType.User) ? ConfidentialAssetsHelper.GetRandomSeed() : null;
			byte[] publicSpendKey = (accountType == AccountType.User) ? ConfidentialAssetsHelper.GetPublicKey(secretSpendKey) : ConfidentialAssetsHelper.GetPublicKey(Ed25519.SecretKeyFromSeed(secretSpendKey));
            byte[] publicViewKey = (accountType == AccountType.User) ? ConfidentialAssetsHelper.GetPublicKey(secretViewKey) : null;
            byte[] secretSpendKeyEnc = null;
            byte[] secretViewKeyEnc = null;

            using (var aes = Aes.Create())
			{
				aes.IV = _dataAccessService.GetAesInitializationVector();
				byte[] passphraseBytes = Encoding.ASCII.GetBytes(passphrase);
				aes.Key = SHA256.Create().ComputeHash(passphraseBytes);
				aes.Padding = PaddingMode.None;

                secretSpendKeyEnc = aes.CreateEncryptor().TransformFinalBlock(secretSpendKey, 0, secretSpendKey.Length);

                if (accountType == AccountType.User)
				{
					secretViewKeyEnc = aes.CreateEncryptor().TransformFinalBlock(secretViewKey, 0, secretViewKey.Length);
				}
				else
				{
					secretViewKeyEnc = null;
				}
			}

			ulong accountId = _dataAccessService.AddAccount((byte)accountType, accountInfo, secretSpendKeyEnc, secretViewKeyEnc, publicSpendKey, publicViewKey, _gatewayService.GetLastRegistryCombinedBlock().Height);

			return accountId;
		}

		private Tuple<byte[], byte[]> DecryptUtxoKeys(Account account, string passphrase)
        {
            byte[] publicSpendKeyBuf, publicViewKeyBuf;

            Tuple<byte[], byte[]> keys = GetSecretKeys(account, passphrase);

            publicSpendKeyBuf = ConfidentialAssetsHelper.GetPublicKey(keys.Item1);
            publicViewKeyBuf = ConfidentialAssetsHelper.GetPublicKey(keys.Item2);

            bool res = publicSpendKeyBuf.Equals32(account.PublicSpendKey) && publicViewKeyBuf.Equals32(account.PublicViewKey);

            return res ? keys : null;
        }

        private Tuple<byte[], byte[]> GetSecretKeys(Account account, string passphrase)
        {
            using (var aes = Aes.Create())
            {
                aes.IV = _dataAccessService.GetAesInitializationVector();
                byte[] passphraseBytes = Encoding.ASCII.GetBytes(passphrase);
                aes.Key = SHA256.Create().ComputeHash(passphraseBytes);
                aes.Padding = PaddingMode.None;

                byte[] secretSpendKey = aes.CreateDecryptor().TransformFinalBlock(account.SecretSpendKey, 0, account.SecretSpendKey.Length);
                byte[] secretViewKey = aes.CreateDecryptor().TransformFinalBlock(account.SecretViewKey, 0, account.SecretViewKey.Length);

                return new Tuple<byte[], byte[]>(secretSpendKey, secretViewKey);
            }
        }

        private byte[] DecryptStateKeys(Account account, string passphrase)
		{
            byte[] secretSpendKey;

            using (var aes = Aes.Create())
			{
				aes.IV = _dataAccessService.GetAesInitializationVector();
				byte[] passphraseBytes = Encoding.ASCII.GetBytes(passphrase);
				aes.Key = SHA256.Create().ComputeHash(passphraseBytes);
				aes.Padding = PaddingMode.None;

				secretSpendKey = aes.CreateDecryptor().TransformFinalBlock(account.SecretSpendKey, 0, account.SecretSpendKey.Length);
			}

            byte[] publicSpendKeyBuf = ConfidentialAssetsHelper.GetPublicKey(Ed25519.SecretKeyFromSeed(secretSpendKey));

			bool res = publicSpendKeyBuf.Equals32(account.PublicSpendKey);

            return res ? secretSpendKey : null;
		}

        #endregion Private Functions	

        private class AuthenticationInput
        {
            public Account Account { get; set; }
            public string Password { get; set; }
        }
    }
}
