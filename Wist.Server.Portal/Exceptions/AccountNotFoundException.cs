using System;
using Wist.Server.Portal.Properties;

namespace Wist.Server.Portal.Exceptions
{

	[Serializable]
	public class AccountNotFoundException : Exception
	{
		public AccountNotFoundException() { }
		public AccountNotFoundException(ulong accountId) : base(string.Format(Resources.ERR_ACCOUNT_NOT_FOUND, accountId)) { }
		public AccountNotFoundException(ulong accountId, Exception inner) : base(string.Format(Resources.ERR_ACCOUNT_NOT_FOUND, accountId), inner) { }
		protected AccountNotFoundException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
