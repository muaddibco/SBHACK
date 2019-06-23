namespace Wist.Server.Portal.Dtos
{
    public class IdentityAttributeDto
    {
		public uint AttributeType { get; set; }
		public string Content { get; set; }
        public string OriginatingCommitment { get; set; }
	}
}
