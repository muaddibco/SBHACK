namespace Wist.Server.Portal.Dtos.ServiceProvider
{
	public class DocumentSignatureDto
    {
        public ulong DocumentId { get; set; }
        public ulong SignatureId { get; set; }
        public string DocumentHash { get; set; }
        public ulong DocumentRecordHeight { get; set; }
		public ulong SignatureRecordHeight { get; set; }
	}
}
