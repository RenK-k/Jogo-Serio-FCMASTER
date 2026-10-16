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

        public Fixture NextUserFixture()
        {
            foreach (var fixture in Fixtures)
            {
                if (!fixture.Played && fixture.Involves(Coach.ClubId) && fixture.Date.Date >= Calendar.CurrentDate.Date)
                    return fixture;
            }

            foreach (var fixture in Fixtures)
            {
                if (!fixture.Played && fixture.Involves(Coach.ClubId))
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
}
