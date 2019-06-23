using CommonServiceLocator;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity;
using Wist.Client.Common;
using Wist.Client.Common.Interfaces;
using Wist.Core.Configuration;
using Wist.Server.Portal.Configuration;

namespace Wist.Server.Portal
{
	public class WebApiBootstrapper : ClientBootstrapper
	{
		private readonly string[] _catalogItems = new string[] { "Wist.Server.Portal.dll" };

		public WebApiBootstrapper(CancellationToken ct) : base(ct)
		{
		}

		public void SetContainer(IUnityContainer unityContainer)
		{
			Container = unityContainer;
		}

		protected override IEnumerable<string> EnumerateCatalogItems(string rootFolder)
		{
			return base.EnumerateCatalogItems(rootFolder).Concat(_catalogItems);
		}

		public override void Initialize()
		{
			base.Initialize();

			IGatewayService gatewayService = ServiceLocator.Current.GetInstance<IGatewayService>();
			IPortalConfiguration configuration = ServiceLocator.Current.GetInstance<IConfigurationService>().Get<IPortalConfiguration>();
			gatewayService.Initialize(configuration.GatewayUri, _cancellationToken);
		}
	}
}
