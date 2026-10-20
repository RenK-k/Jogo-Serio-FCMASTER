using System.Collections.Generic;
using CoachMaster.Career;
using CoachMaster.Domain;
using UnityEngine;

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
        public bool TeamTalkApplied { get; set; }
        public string Flash { get; set; }
        public string FlashPlayerId { get; set; }
        public Dictionary<string, int> GoalsScored { get; } = new Dictionary<string, int>();
        public Dictionary<string, int> Assists { get; } = new Dictionary<string, int>();
        public Dictionary<string, int> PenaltyGoals { get; } = new Dictionary<string, int>();
        public Dictionary<string, int> Bookings { get; } = new Dictionary<string, int>();
        public HashSet<string> SentOffPlayers { get; } = new HashSet<string>();
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
        public List<MatchShotEvent> Shots { get; } = new List<MatchShotEvent>();
        public List<MatchPassEvent> Passes { get; } = new List<MatchPassEvent>();
        public List<MatchThreatEvent> Threats { get; } = new List<MatchThreatEvent>();
        public List<MatchPositionSample> PositionSamples { get; } = new List<MatchPositionSample>();
        public float HomePossession { get; set; }
        public float AwayPossession { get; set; }
        public int HomeShots { get; set; }
        public int AwayShots { get; set; }
        public int HomeChances { get; set; }
        public int AwayChances { get; set; }

        public MatchShotEvent RecordShot(bool home, string shooterId, Vector2 origin, Vector2 target, float xg, string outcome, bool header)
        {
            var shot = new MatchShotEvent
            {
                Minute = Minute,
                Home = home,
                ShooterId = shooterId,
                Origin = origin,
                Target = target,
                ExpectedGoals = Mathf.Clamp01(xg),
                Outcome = outcome ?? "miss",
                Header = header
            };
            Shots.Add(shot);
            Threats.Add(new MatchThreatEvent
            {
                Minute = Minute,
                Home = home,
                Position = origin,
                Kind = "shot",
                Weight = Mathf.Clamp(shot.ExpectedGoals + (shot.Outcome == "goal" ? 0.35f : 0f), 0.1f, 1.5f)
            });
            return shot;
        }

        public MatchPassEvent RecordPass(bool home, string passerId, string intendedReceiverId, Vector2 origin, Vector2 target, bool throughBall)
        {
            var pass = new MatchPassEvent
            {
                Minute = Minute,
                Home = home,
                PasserId = passerId,
                IntendedReceiverId = intendedReceiverId,
                Origin = origin,
                Target = target,
                ThroughBall = throughBall
            };
            Passes.Add(pass);
            return pass;
        }

        public void ResolvePass(MatchPassEvent pass, bool completed, bool intercepted, string receiverId)
        {
            if (pass == null || pass.Resolved)
                return;
            pass.Completed = completed;
            pass.Intercepted = intercepted;
            pass.ActualReceiverId = completed ? receiverId : null;
            pass.Resolved = true;
            if (!completed)
                return;
            var depth = pass.Home ? pass.Target.y : 1f - pass.Target.y;
            if (depth < 0.67f)
                return;
            var inCentralBoxLane = depth >= 0.82f && Mathf.Abs(pass.Target.x - 0.5f) <= 0.22f;
            Threats.Add(new MatchThreatEvent
            {
                Minute = Minute,
                Home = pass.Home,
                Position = pass.Target,
                Kind = inCentralBoxLane ? "box-entry" : "final-third-entry",
                Weight = inCentralBoxLane ? 0.65f : 0.28f
            });
        }

        public void CapturePositions(float minute, Vector2[] home, Vector2[] away, string[] homeIds, string[] awayIds)
        {
            if (home == null || away == null || homeIds == null || awayIds == null)
                return;
            PositionSamples.Add(new MatchPositionSample
            {
                Minute = minute,
                HomePositions = (Vector2[])home.Clone(),
                AwayPositions = (Vector2[])away.Clone(),
                HomePlayerIds = (string[])homeIds.Clone(),
                AwayPlayerIds = (string[])awayIds.Clone()
            });
        }

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

        public static MatchSession Create(CareerState career, Fixture fixture, bool automaticUserSelection = false)
        {
            var match = new MatchSession
            {
                Fixture = fixture,
                Home = career.World.Clubs[fixture.HomeClubId],
                Away = career.World.Clubs[fixture.AwayClubId]
            };
            match.HomeTactics = Copy(match.Home.Tactics);
            match.AwayTactics = Copy(match.Away.Tactics);
            FillSquad(career, match.Home, match.HomeOnField, match.HomeBench, automaticUserSelection && match.Home.Id == career.Coach.ClubId);
            FillSquad(career, match.Away, match.AwayOnField, match.AwayBench, automaticUserSelection && match.Away.Id == career.Coach.ClubId);
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

        static void FillSquad(CareerState career, Club club, List<string> onField, List<string> bench, bool ignoreManualLineup)
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
            StartingShape.Resolve(available, club, eleven, rest, ignoreManualLineup);
            foreach (var player in eleven)
                onField.Add(player.Id);
            foreach (var player in rest)
                bench.Add(player.Id);
            foreach (var player in hurt)
                bench.Add(player.Id);
            while (onField.Count < 11 && bench.Count > 0)
            {
                var availableIndex = -1;
                for (var i = 0; i < bench.Count; i++)
                {
                    if (career.World.Players.TryGetValue(bench[i], out var candidate)
                        && candidate != null && (candidate.Injury == null || !candidate.Injury.IsInjured))
                    {
                        availableIndex = i;
                        break;
                    }
                }

                if (availableIndex < 0)
                    break;
                onField.Add(bench[availableIndex]);
                bench.RemoveAt(availableIndex);
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

    public sealed class MatchShotEvent
    {
        public float Minute;
        public bool Home;
        public string ShooterId;
        public Vector2 Origin;
        public Vector2 Target;
        public float ExpectedGoals;
        public string Outcome;
        public bool Header;
    }

    public sealed class MatchPassEvent
    {
        public float Minute;
        public bool Home;
        public string PasserId;
        public string IntendedReceiverId;
        public string ActualReceiverId;
        public Vector2 Origin;
        public Vector2 Target;
        public bool ThroughBall;
        public bool Completed;
        public bool Intercepted;
        public bool Resolved;
    }

    public sealed class MatchThreatEvent
    {
        public float Minute;
        public bool Home;
        public Vector2 Position;
        public string Kind;
        public float Weight;
    }

    public sealed class MatchPositionSample
    {
        public float Minute;
        public Vector2[] HomePositions;
        public Vector2[] AwayPositions;
        public string[] HomePlayerIds;
        public string[] AwayPlayerIds;
    }
}
