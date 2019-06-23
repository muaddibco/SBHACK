using System.Collections.Generic;

namespace Wist.Server.Portal.Dtos
{
    public class IdentityAttributeSchemaDto
	{
		public string Name { get; set; }

		public uint AttributeType { get; set; }

		public List<IdentityAttributeValidationSchemaDto> AvailableValidations { get; set; }
	}
}
