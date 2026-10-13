namespace CoachMaster.Domain
{
    public sealed class PlayerRelation
    {
        public string PlayerAId { get; set; }
        public string PlayerBId { get; set; }
        public int Affinity { get; set; }
    }
}
