using Wist.Core.Configuration;

namespace Wist.Server.Portal.Configuration
{
    public interface IPortalConfiguration : IConfigurationSection
    {
        string Secret { get; set; }
        string FacePersonGroupId { get; set; }
        ushort RingSize { get; set; }
        string GatewayUri { get; set; }
        string BiometricUri { get; set; }
        bool DemoMode { get; set; }
    }
}
