using System.Collections.Generic;

namespace Wist.Server.Portal.Dtos.ServiceProvider
{
    public class DocumentDto
    {
        public ulong DocumentId { get; set; }

        public string DocumentName { get; set; }

        public string Hash { get; set; }

        public List<AllowedSignerDto> AllowedSigners { get; set; }

        public List<DocumentSignatureDto> Signatures { get; set; }
    }
}
