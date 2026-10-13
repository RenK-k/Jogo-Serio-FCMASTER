using CoachMaster.Domain;

namespace CoachMaster.Career
{
    public sealed class CareerState
    {
        public WorldState World { get; set; }
        public Coach Coach { get; set; }
        public GameCalendar Calendar { get; set; }
        public CoachKnowledge Knowledge { get; set; }

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
