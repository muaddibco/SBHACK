using System.Collections.Generic;
using Wist.Client.Common.Entities;
using Wist.Client.DataModel.Enums;
using Wist.Core.Architecture;
using Account = Wist.Client.DataModel.Model.Account;

namespace Wist.Server.Portal.Services
{
	[ServiceContract]
    public interface IAccountsService
	{
		AccountDescriptor Authenticate(ulong accountId, string password);
		List<Account> GetAll();
		Account GetById(ulong id);
        Account GetByPublicKey(byte[] publicKey);
		void Create(AccountType accountType, string accountInfo, string password);
		void Update(AccountDescriptor user, string password = null);
		void Delete(ulong id);
        void Clean(ulong accountId);

        ulong DuplicateAccount(ulong id, string accountInfo);
	}
}
