using System.Collections.Generic;

namespace Wist.Server.Portal.Dtos
{
    public class ServiceProviderActionAndValidationsDto
    {
        public bool IsRegistered { get; set; }

        public string PublicKey { get; set; }

        public string SessionKey { get; set; }

        public string ExtraInfo { get; set; }

        public List<string> Validations { get; set; }
    }
}
