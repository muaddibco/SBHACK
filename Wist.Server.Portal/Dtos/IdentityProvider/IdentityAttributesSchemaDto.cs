using System.Collections.Generic;

namespace Wist.Server.Portal.Dtos
{
    public class IdentityAttributesSchemaDto
	{
		public IdentityAttributeSchemaDto RootAttribute { get; set; }

		public List<IdentityAttributeSchemaDto> AssociatedAttributes { get; set; }
	}
}
