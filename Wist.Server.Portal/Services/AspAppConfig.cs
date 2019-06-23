using Microsoft.Extensions.Configuration;
using Wist.Core.Configuration;

namespace Wist.Server.Portal.Services
{
	public class AspAppConfig : IAppConfig
	{
		private readonly IConfiguration _configuration;

		public AspAppConfig(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public bool GetBool(string key, bool required = true)
		{
			ExtractSectionAndProperty(key, out string section, out string name);
			bool value = _configuration.GetSection(section).GetValue<bool>(name);

			return value;
		}

		public long GetLong(string key, bool required = true)
		{
			ExtractSectionAndProperty(key, out string section, out string name);
			long value = _configuration.GetSection(section).GetValue<long>(name);

			return value;
		}

		public string GetString(string key, bool required = true)
		{
			ExtractSectionAndProperty(key, out string section, out string name);
			string value = _configuration.GetSection(section).GetValue<string>(name);

			return value;
		}

		private void ExtractSectionAndProperty(string key, out string section, out string name)
		{
			string[] pair = key.Split(':');
			section = pair[0];
			name = pair[1];
		}
	}
}
