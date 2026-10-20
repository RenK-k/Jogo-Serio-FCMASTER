namespace CoachMaster.Domain
{
    public sealed class League
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public LeagueTier Tier { get; set; }
        public string Description { get; set; }
        public int ClubCount { get; set; }
    }
}
