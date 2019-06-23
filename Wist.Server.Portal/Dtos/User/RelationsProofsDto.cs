namespace Wist.Server.Portal.Dtos.User
{
    public class RelationsProofsDto : UserAttributeTransferDto
    {
        public string TargetViewKey { get; set; }

        public GroupRelationDto[] Relations { get; set; }
    }
}
