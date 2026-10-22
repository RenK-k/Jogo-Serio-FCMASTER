using System.Collections.Generic;
using CoachMaster.Domain;

namespace CoachMaster.Career
{
    public sealed class CareerState
    {
        public WorldState World { get; set; }
        public Coach Coach { get; set; }
        public GameCalendar Calendar { get; set; }
        public CoachKnowledge Knowledge { get; set; }
        public List<Fixture> Fixtures { get; } = new List<Fixture>();
        public List<LeagueTableRow> Table { get; } = new List<LeagueTableRow>();
        public List<CareerMoment> Upcoming { get; } = new List<CareerMoment>();
        public CareerMoment Current { get; set; }
        public List<string> Notebook { get; } = new List<string>();
        public List<CoachNote> Board { get; } = new List<CoachNote>();
        public List<PaperStory> Press { get; } = new List<PaperStory>();
        public HashSet<string> Scouted { get; } = new HashSet<string>();
        public HashSet<string> Listed { get; } = new HashSet<string>();
        public HashSet<string> Targets { get; } = new HashSet<string>();
        public HashSet<string> ScoutedPlayers { get; } = new HashSet<string>();
        public HashSet<string> Loans { get; } = new HashSet<string>();
        public List<ScoutJob> ScoutJobs { get; } = new List<ScoutJob>();
        public List<PlayerNote> PlayerNotes { get; } = new List<PlayerNote>();
        public List<SeasonLine> Season { get; } = new List<SeasonLine>();
        public List<SeasonLine> CareerTotals { get; } = new List<SeasonLine>();
        public List<string> AcademyPlayerIds { get; } = new List<string>();
        public Dictionary<string, TacticalIdentity> AcademyTactics { get; } = new Dictionary<string, TacticalIdentity>();
        public Dictionary<string, List<string>> AcademySquadOrders { get; } = new Dictionary<string, List<string>>();
        public Dictionary<string, string> AcademyRoleAssignments { get; } = new Dictionary<string, string>();
        public HashSet<string> AcademyClubIds { get; } = new HashSet<string>();
        public HashSet<string> AcademyScheduleClubIds { get; } = new HashSet<string>();
        public List<AcademyFixture> AcademyFixtures { get; } = new List<AcademyFixture>();
        public List<TransferRecord> TransferHistory { get; } = new List<TransferRecord>();
        public List<CareerTrophy> Trophies { get; } = new List<CareerTrophy>();
        public long LastGlobalTransferTicks { get; set; }
        public long LastWorldNewsTicks { get; set; }
        public int CompletedSeasons { get; set; }
        public List<CareerJobOffer> JobOffers { get; } = new List<CareerJobOffer>();
        public List<FootballMemory> Memories { get; } = new List<FootballMemory>();
        public int SupporterApproval { get; set; } = 55;
        public Dictionary<string, int> ManagerTrust { get; } = new Dictionary<string, int>();
        public int InjuryFrequency { get; set; } = 1;
        public int RefereeStrictness { get; set; } = 1;
        public int RealismPreset { get; set; } = 1;
        public long LastAutoSaveDateTicks { get; set; }

        public Fixture NextUserFixture()
        {
            foreach (var fixture in Fixtures)
            {
                if (!fixture.Played && !fixture.Walkover && fixture.Involves(Coach.ClubId) && fixture.Date.Date >= Calendar.CurrentDate.Date)
                    return fixture;
            }

            foreach (var fixture in Fixtures)
            {
                if (!fixture.Played && !fixture.Walkover && fixture.Involves(Coach.ClubId))
                    return fixture;
            }

            return null;
        }

        public Club Club => World.Clubs[Coach.ClubId];
        public League League
        {
            get
            {
                foreach (var league in World.Leagues)
                {
                    if (league.Id == Club.LeagueId)
                        return league;
                }

                return null;
            }
        }
    }

    public sealed class PlayerNote
    {
        public string PlayerId { get; set; }
        public string Text { get; set; }
    }

    public sealed class ScoutJob
    {
        public string PlayerId { get; set; }
        public long Due { get; set; }
    }

    public sealed class CareerJobOffer
    {
        public string ClubId { get; set; }
        public long ReceivedAtTicks { get; set; }
        public string Reason { get; set; }
        public bool Resolved { get; set; }
        public bool Accepted { get; set; }
    }

    public sealed class AcademyFixture
    {
        public string Id { get; set; }
        public string ClubId { get; set; }
        public string AgeGroup { get; set; }
        public System.DateTime Date { get; set; }
        public string HomeClubId { get; set; }
        public string AwayClubId { get; set; }
        public bool Played { get; set; }
        public int HomeGoals { get; set; }
        public int AwayGoals { get; set; }
    }

    public sealed class TransferRecord
    {
        public long DateTicks { get; set; }
        public string PlayerId { get; set; }
        public string FromClubId { get; set; }
        public string ToClubId { get; set; }
        public int Fee { get; set; }
        public int WeeklyWage { get; set; }
        public int ContractYears { get; set; }
        public int AgeAtTransfer { get; set; }
        public int OverallAtTransfer { get; set; }
        public string Type { get; set; }
    }

    public sealed class CareerTrophy
    {
        public string ClubId { get; set; }
        public string CompetitionId { get; set; }
        public int SeasonStartYear { get; set; }
        public long DateTicks { get; set; }
        public string Title { get; set; }
    }
}
