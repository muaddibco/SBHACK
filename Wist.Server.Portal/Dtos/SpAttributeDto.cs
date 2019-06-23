﻿namespace Wist.Server.Portal.Dtos
{
	public class SpAttributeDto
	{
		public string AttributeType { get; set; }

		public string Source { get; set; }

		public string AssetId { get; set; }

		public string OriginalBlindingFactor { get; set; }

		public string OriginalCommitment { get; set; }

        public string IssuingCommitment { get; set; }

        public bool Validated { get; set; }

		public string Content { get; set; }

        public bool IsOverriden { get; set; }
    }
}
