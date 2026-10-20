using System.Collections.Generic;

namespace CoachMaster.Domain
{
    public sealed class WorldState
    {
        public List<League> Leagues { get; } = new List<League>();
        public Dictionary<string, Club> Clubs { get; } = new Dictionary<string, Club>();
        public Dictionary<string, Player> Players { get; } = new Dictionary<string, Player>();
        public HashSet<string> LoadedLeagueIds { get; } = new HashSet<string>();

        public IEnumerable<Club> ClubsInLeague(string leagueId)
        {
            foreach (var club in Clubs.Values)
            {
                if (club.LeagueId == leagueId)
                    yield return club;
            }
        }
    }
}
