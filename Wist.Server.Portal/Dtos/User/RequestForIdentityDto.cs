namespace Wist.Server.Portal.Dtos
{
    public class RequestForIdentityDto
    {
        public string Target { get; set; }

        public string IdCardContent { get; set; }

		public string Password { get; set; }

		public string ImageContent { get; set; }
    }
}
