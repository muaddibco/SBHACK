using System.Collections.Generic;

namespace Wist.Server.Portal.Dtos
{
    public class UserAttributeTransferWithValidationsDto
	{
		public UserAttributeTransferDto UserAttributeTransfer { get; set; }

		public string Password { get; set; }

		public List<string> Validations { get; set; }
	}
}
