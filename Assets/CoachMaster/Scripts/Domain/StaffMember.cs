namespace CoachMaster.Domain
{
    public sealed class StaffMember
    {
        public string Id { get; set; }
        public string ClubId { get; set; }
        public string FullName { get; set; }
        public StaffRole Role { get; set; }
        public int Judgement { get; set; } = 10;
    }
}
