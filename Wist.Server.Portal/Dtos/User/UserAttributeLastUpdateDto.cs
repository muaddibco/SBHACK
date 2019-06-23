namespace Wist.Server.Portal.Dtos
{
	public class UserAttributeLastUpdateDto
	{
		public string AssetId { get; set; }

		public string LastBlindingFactor { get; set; }

		public string LastCommitment { get; set; }

		public string LastTransactionKey { get; set; }

		public string LastDestinationKey { get; set; }
    }
}
