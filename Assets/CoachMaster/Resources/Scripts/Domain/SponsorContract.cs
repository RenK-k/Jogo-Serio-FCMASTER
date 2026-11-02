namespace CoachMaster.Domain
{
    public sealed class SponsorContract
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int WeeklyIncome { get; set; }
        public string LogoKey { get; set; }
        public string LogoHex { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
