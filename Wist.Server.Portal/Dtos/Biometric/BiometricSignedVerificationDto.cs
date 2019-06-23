using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Wist.Server.Portal.Dtos
{
    public class BiometricSignedVerificationDto
    {
		public string PublicKey { get; set; }

		public string Signature { get; set; }
	}
}
