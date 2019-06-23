﻿namespace Wist.Server.Portal.Dtos
{
	public class IdentityRequestDto
	{
		public string BlindingPoint { get; set; }
		public string RootAttributeContent { get; set; }
		public string FaceImageContent { get; set; }
		public string RequesterPublicSpendKey { get; set; }
		public string RequesterPublicViewKey { get; set; }
	}
}
