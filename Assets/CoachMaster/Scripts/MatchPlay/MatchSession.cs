using System.Collections.Generic;
using CoachMaster.Career;
using CoachMaster.Domain;

namespace CoachMaster.MatchPlay
{
    public sealed class MatchSession
    {
        public Fixture Fixture { get; set; }
        public Club Home { get; set; }
        public Club Away { get; set; }
        public TacticalIdentity HomeTactics { get; set; }
        public TacticalIdentity AwayTactics { get; set; }
        public List<string> HomeOnField { get; } = new List<string>();
        public List<string> AwayOnField { get; } = new List<string>();
        public List<string> HomeBench { get; } = new List<string>();
        public List<string> AwayBench { get; } = new List<string>();
        public int HomeSubsLeft { get; set; } = 5;
        public int AwaySubsLeft { get; set; } = 5;
        public int HomeGoals { get; set; }
        public int AwayGoals { get; set; }
        public float Minute { get; set; }
        public bool HomeHasBall { get; set; } = true;
        public int BallHolder { get; set; }
        public MatchPhase Phase { get; set; } = MatchPhase.FirstHalf;
        public string LastEvent { get; set; } = "O árbitro dá início à partida.";
        public string TeamTalk { get; set; }
        public float AttackBoost { get; set; }
        public float DefenseBoost { get; set; }
        public bool Finished { get; set; }
        public Dictionary<string, string> Names { get; } = new Dictionary<string, string>();

        public static MatchSession Create(CareerState career, Fixture fixture)
        {
            var match = new MatchSession
            {
                Fixture = fixture,
                Home = career.World.Clubs[fixture.HomeClubId],
                Away = career.World.Clubs[fixture.AwayClubId]
            };
            match.HomeTactics = Copy(match.Home.Tactics);
            match.AwayTactics = Copy(match.Away.Tactics);
            FillSquad(career, match.Home, match.HomeOnField, match.HomeBench);
            FillSquad(career, match.Away, match.AwayOnField, match.AwayBench);
            foreach (var player in career.World.Players.Values)
                match.Names[player.Id] = player.FullName;
            return match;
        }

        static void FillSquad(CareerState career, Club club, List<string> onField, List<string> bench)
        {
            var players = new List<Player>();
            foreach (var id in club.PlayerIds)
                players.Add(career.World.Players[id]);
            players.Sort((a, b) => PositionRank(a.Position).CompareTo(PositionRank(b.Position)));
            for (var i = 0; i < players.Count; i++)
            {
                if (onField.Count < 11)
                    onField.Add(players[i].Id);
                else
                    bench.Add(players[i].Id);
            }
        }

        static int PositionRank(PlayerPosition position)
        {
            return (int)position;
        }

        static TacticalIdentity Copy(TacticalIdentity src)
        {
            return new TacticalIdentity
            {
                Formation = src.Formation,
                Pressing = src.Pressing,
                BuildUp = src.BuildUp,
                FullBacks = src.FullBacks,
                DefensiveLine = src.DefensiveLine,
                FormerCoachName = src.FormerCoachName,
                Summary = src.Summary
            };
        }
    }

    public enum MatchPhase
    {
        FirstHalf,
        HalfTime,
        SecondHalf,
        FullTime
    }
}
