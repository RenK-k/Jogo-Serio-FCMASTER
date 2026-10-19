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
        public string HomeCaptainId { get; set; }
        public string AwayCaptainId { get; set; }
        public int HomeGoals { get; set; }
        public int AwayGoals { get; set; }
        public float Minute { get; set; }
        public bool HomeHasBall { get; set; } = true;
        public int BallHolder { get; set; }
        public MatchPhase Phase { get; set; } = MatchPhase.FirstHalf;
        public string LastEvent { get; set; } = "O árbitro dá início à partida.";
        public string TeamTalk { get; set; }
        public string Flash { get; set; }
        public string FlashPlayerId { get; set; }
        public Dictionary<string, int> GoalsScored { get; } = new Dictionary<string, int>();
        public Dictionary<string, int> Assists { get; } = new Dictionary<string, int>();
        public Dictionary<string, int> PenaltyGoals { get; } = new Dictionary<string, int>();
        public Dictionary<string, int> Bookings { get; } = new Dictionary<string, int>();
        public int FlashSerial { get; set; }
        public float AttackBoost { get; set; }
        public float DefenseBoost { get; set; }
        public bool Finished { get; set; }
        public bool CareerStatsCredited { get; set; }
        public Dictionary<string, string> Names { get; } = new Dictionary<string, string>();
        public Dictionary<string, Player> People { get; } = new Dictionary<string, Player>();
        public Dictionary<string, float> Ratings { get; } = new Dictionary<string, float>();
        public HashSet<string> Appeared { get; } = new HashSet<string>();
        public List<MatchNote> Notes { get; } = new List<MatchNote>();
        public float HomePossession { get; set; }
        public float AwayPossession { get; set; }
        public int HomeShots { get; set; }
        public int AwayShots { get; set; }
        public int HomeChances { get; set; }
        public int AwayChances { get; set; }

        public void AddNote(string text, string kind, string playerId = null)
        {
            var minute = (int)Minute;
            if (minute < 0)
                minute = 0;
            Notes.Add(new MatchNote { Minute = minute, Text = text, Kind = kind, PlayerId = playerId });
            LastEvent = text;
        }

        public float RatingOf(string id)
        {
            return Ratings.TryGetValue(id, out var value) ? value : 6f;
        }

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
            foreach (var id in match.HomeOnField)
                match.Appeared.Add(id);
            foreach (var id in match.AwayOnField)
                match.Appeared.Add(id);
            foreach (var player in career.World.Players.Values)
            {
                match.Names[player.Id] = player.FullName;
                match.People[player.Id] = player;
            }
            match.HomeCaptainId = SelectCaptain(match.HomeOnField, match.People);
            match.AwayCaptainId = SelectCaptain(match.AwayOnField, match.People);
            SeedRatings(match, match.HomeOnField, 6.4f);
            SeedRatings(match, match.AwayOnField, 6.4f);
            SeedRatings(match, match.HomeBench, 6f);
            SeedRatings(match, match.AwayBench, 6f);
            return match;
        }

        public void ReplaceCaptain(bool home)
        {
            if (home)
                HomeCaptainId = SelectCaptain(HomeOnField, People);
            else
                AwayCaptainId = SelectCaptain(AwayOnField, People);
        }

        static string SelectCaptain(List<string> players, Dictionary<string, Player> people)
        {
            if (players == null || players.Count == 0)
                return null;
            for (var i = 0; i < players.Count; i++)
            {
                if (people.TryGetValue(players[i], out var designated) && designated.IsCaptain)
                    return players[i];
            }

            var selected = players[0];
            var best = float.MinValue;
            for (var i = 0; i < players.Count; i++)
            {
                if (!people.TryGetValue(players[i], out var player) || player == null)
                    continue;
                var score = player.IsViceCaptain ? 100f : 0f;
                if (player.HasTrait(PlayerTrait.Lideranca)) score += 26f;
                if (player.Personality == PersonalityTrait.Leader) score += 20f;
                if (player.HasTrait(PlayerTrait.JogadorDeEquipa)) score += 10f;
                if (player.HasTrait(PlayerTrait.HomemDeUmClube)) score += 8f;
                score += player.Age * 0.08f + PlayerAttributes.Overall(player) * 0.025f;
                if (score > best)
                {
                    best = score;
                    selected = players[i];
                }
            }
            return selected;
        }

        static void SeedRatings(MatchSession match, List<string> ids, float value)
        {
            foreach (var id in ids)
                match.Ratings[id] = value;
        }

        static void FillSquad(CareerState career, Club club, List<string> onField, List<string> bench)
        {
            var players = new List<Player>();
            foreach (var id in club.PlayerIds)
                players.Add(career.World.Players[id]);
            var available = new List<Player>();
            var hurt = new List<Player>();
            foreach (var player in players)
            {
                if (player.Injury != null && player.Injury.IsInjured)
                    hurt.Add(player);
                else
                    available.Add(player);
            }

            var eleven = new List<Player>();
            var rest = new List<Player>();
            StartingShape.Resolve(available, club, eleven, rest);
            foreach (var player in eleven)
                onField.Add(player.Id);
            foreach (var player in rest)
                bench.Add(player.Id);
            foreach (var player in hurt)
                bench.Add(player.Id);
            while (onField.Count < 11 && bench.Count > 0)
            {
                onField.Add(bench[0]);
                bench.RemoveAt(0);
            }
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
                Width = string.IsNullOrEmpty(src.Width) ? "Largura normal" : src.Width,
                Tempo = string.IsNullOrEmpty(src.Tempo) ? "Ritmo normal" : src.Tempo,
                PassingStyle = string.IsNullOrEmpty(src.PassingStyle) ? "Passe misto" : src.PassingStyle,
                AttackIdea = string.IsNullOrEmpty(src.AttackIdea) ? "Equilibrado" : src.AttackIdea,
                Marking = string.IsNullOrEmpty(src.Marking) ? "Marcação mista" : src.Marking,
                Preset = src.Preset,
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

    public sealed class MatchNote
    {
        public int Minute;
        public string Text;
        public string Kind;
        public string PlayerId;
    }
}
