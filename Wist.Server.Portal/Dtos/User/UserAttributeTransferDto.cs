namespace Wist.Server.Portal.Dtos
{
    public class UserAttributeTransferDto : UserAttributeDto
	{
		public string Target { get; set; }

		public string Payload { get; set; }

        public string ExtraInfo { get; set; }

        public string ImageContent { get; set; }
    }
}
