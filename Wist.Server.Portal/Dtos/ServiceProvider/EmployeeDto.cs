using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Wist.Server.Portal.Dtos
{
	public class EmployeeDto
	{
		public ulong EmployeeId { get; set; }
		public string Description { get; set; }
		public string RawRootAttribute { get; set; }
		public string AssetId { get; set; }
        public string RegistrationCommitment { get; set; }
		public ulong GroupId { get; set; }
	}
}
