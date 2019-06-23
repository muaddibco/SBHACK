namespace Wist.Server.Portal.Dtos
{
    public class AccountDto
	{
		public ulong AccountId { get; set; }
		public byte AccountType { get; set; }
		public string AccountInfo { get; set; }
		public string Password { get; set; }
		public string Token { get; set; }
		public string PublicViewKey { get; set; }
		public string PublicSpendKey { get; set; }
    }
}
