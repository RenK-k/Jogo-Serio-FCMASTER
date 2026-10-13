using System.Collections.Generic;

namespace CoachMaster.Domain
{
    public sealed class Club
    {
        public string Id { get; set; }
        public string LeagueId { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string City { get; set; }
        public string Stadium { get; set; }
        public int FoundedYear { get; set; }
        public string History { get; set; }
        public string PublicReputation { get; set; }
        public int Reputation { get; set; }
        public string JerseyPrimaryHex { get; set; }
        public string JerseySecondaryHex { get; set; }
        public ClubFinance Finance { get; set; }
        public TacticalIdentity Tactics { get; set; }
        public List<string> BoardObjectives { get; set; } = new List<string>();
        public string DressingRoomAtmosphere { get; set; }
        public string PublicTeamState { get; set; }
        public string HiddenInternalProblems { get; set; }
        public string PublicCoachVacancyNote { get; set; }
        public string HiddenDismissalTruth { get; set; }
        public int DressingRoomTension { get; set; }
        public List<string> PlayerIds { get; set; } = new List<string>();
        public List<StaffMember> Staff { get; set; } = new List<StaffMember>();
        public List<PlayerRelation> Relations { get; set; } = new List<PlayerRelation>();
    }
}
