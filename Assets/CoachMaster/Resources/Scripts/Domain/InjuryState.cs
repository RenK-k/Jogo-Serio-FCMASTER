namespace CoachMaster.Domain
{
    public sealed class InjuryState
    {
        public bool IsInjured { get; set; }
        public string Description { get; set; }
        public int DaysRemaining { get; set; }
    }
}
