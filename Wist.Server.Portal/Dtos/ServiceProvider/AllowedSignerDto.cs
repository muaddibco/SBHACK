namespace Wist.Server.Portal.Dtos.ServiceProvider
{
    public class AllowedSignerDto
    {
        public ulong AllowedSignerId { get; set; }

        public string GroupOwner { get; set; }

        public string GroupName { get; set; }
    }
}
