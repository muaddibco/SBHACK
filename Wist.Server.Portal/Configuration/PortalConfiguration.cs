using Wist.Core.Architecture;
using Wist.Core.Architecture.Enums;
using Wist.Core.Configuration;

namespace Wist.Server.Portal.Configuration
{
    [RegisterExtension(typeof(IConfigurationSection), Lifetime = LifetimeManagement.Singleton)]
    public class PortalConfiguration : ConfigurationSectionBase, IPortalConfiguration
    {
        public const string SECTION_NAME = "AppSettings";

        public PortalConfiguration(IApplicationContext applicationContext) : base(applicationContext, SECTION_NAME)
        {

        }

        public string Secret { get; set; }
        public string FacePersonGroupId { get; set; }
        public string GatewayUri { get; set; }
        public string BiometricUri { get; set; }
        public ushort RingSize { get; set; }
        public bool DemoMode { get; set; }
    }
}
