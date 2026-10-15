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
}
