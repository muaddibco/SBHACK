using System.Collections.Generic;

namespace Wist.Server.Portal.Dtos
{
    public class IdentityAttributeValidationSchemaDto
	{
		public ushort ValidationType { get; set; }

		public List<string> ValidationCriterionTypes { get; set; }
	}
}
