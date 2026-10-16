using System;

namespace CoachMaster.Domain
{
    public sealed class Fixture
    {
        public string Id { get; set; }
        public DateTime Date { get; set; }
        public string LeagueId { get; set; }
        public string HomeClubId { get; set; }
        public string AwayClubId { get; set; }
        public bool Played { get; set; }
        public bool Restored { get; set; }
        public int HomeGoals { get; set; }
        public int AwayGoals { get; set; }

        public bool Involves(string clubId) => HomeClubId == clubId || AwayClubId == clubId;

        public string OpponentId(string clubId) => HomeClubId == clubId ? AwayClubId : HomeClubId;

        public bool IsHome(string clubId) => HomeClubId == clubId;
    }

    public sealed class LeagueTableRow
    {
        public string ClubId { get; set; }
        public int Played { get; set; }
        public int Won { get; set; }
        public int Drawn { get; set; }
        public int Lost { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int Points { get; set; }
        public string LeagueId { get; set; }
        public int GoalDifference => GoalsFor - GoalsAgainst;
    }
}
