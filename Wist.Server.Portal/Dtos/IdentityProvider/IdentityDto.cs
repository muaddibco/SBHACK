using System.Collections.Generic;

namespace Wist.Server.Portal.Dtos
{
	public class IdentityDto
	{
        public int NumberOfTransfers { get; set; }
		public string Id { get; set; }
		public string Description { get; set; }

		public IdentityAttributeDto RootAttribute { get; set; }

		public List<IdentityAttributeDto> AssociatedAttributes { get; set; }
	}
}
