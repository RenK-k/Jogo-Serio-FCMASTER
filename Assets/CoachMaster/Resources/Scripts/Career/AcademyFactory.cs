using System;
using System.Collections.Generic;
using CoachMaster.Domain;
using CoachMaster.UI;
using UnityEngine;

namespace CoachMaster.Career
{
    public static class AcademyFactory
    {
        static readonly PlayerPosition[] Positions =
        {
            PlayerPosition.Goalkeeper, PlayerPosition.RightBack, PlayerPosition.CentreBack,
            PlayerPosition.CentreBack, PlayerPosition.LeftBack, PlayerPosition.DefensiveMidfielder,
            PlayerPosition.CentralMidfielder, PlayerPosition.CentralMidfielder,
            PlayerPosition.AttackingMidfielder, PlayerPosition.RightWinger,
            PlayerPosition.LeftWinger, PlayerPosition.Striker
        };

        public static readonly string[] AgeGroups = { "U20", "U18", "U16" };
        const int SquadSize = 22;
        const int FixturesPerGroup = 18;

        public static void Ensure(CareerState career, string clubId)
        {
            if (career == null || career.World == null || string.IsNullOrEmpty(clubId)
                || !career.World.Clubs.TryGetValue(clubId, out var club))
                return;

            career.AcademyClubIds.Add(clubId);
            for (var i = career.AcademyPlayerIds.Count - 1; i >= 0; i--)
                if (club.PlayerIds.Contains(career.AcademyPlayerIds[i]))
                    career.AcademyPlayerIds.RemoveAt(i);
            var nationality = Nationality(club.LeagueId);
            var clubSeed = StableHash(club.Id);
            for (var groupIndex = 0; groupIndex < AgeGroups.Length; groupIndex++)
            {
                var group = AgeGroups[groupIndex];
                var currentCount = 0;
                foreach (var id in career.AcademyPlayerIds)
                {
                    if (!career.World.Players.TryGetValue(id, out var existing) || existing.ClubId != clubId || GroupForAge(existing.Age) != group)
                        continue;
                    currentCount++;
                }

                var slot = 1;
                while (currentCount < SquadSize && slot <= SquadSize + 256)
                {
                    var id = "academy:" + club.Id + ":" + group.ToLowerInvariant() + ":" + slot;
                    var seed = unchecked(clubSeed + groupIndex * 104729 + slot * 7919);
                    if (career.World.Players.TryGetValue(id, out var existingPlayer))
                    {
                        if (existingPlayer.ClubId == clubId && (club.PlayerIds == null || !club.PlayerIds.Contains(id)))
                        {
                            existingPlayer.FullName = FaceBook.PersonName(nationality, seed);
                            existingPlayer.Nationality = nationality;
                            if (string.IsNullOrEmpty(existingPlayer.TrainingFocus))
                                existingPlayer.TrainingFocus = TrainingFocusFor(existingPlayer.Position);
                        }
                        if (!career.AcademyPlayerIds.Contains(id) && existingPlayer.ClubId == clubId
                            && (club.PlayerIds == null || !club.PlayerIds.Contains(id)))
                            career.AcademyPlayerIds.Add(id);
                        if (existingPlayer.ClubId == clubId && GroupForAge(existingPlayer.Age) == group
                            && (club.PlayerIds == null || !club.PlayerIds.Contains(id)))
                            currentCount++;
                        slot++;
                        continue;
                    }

                    var age = AgeForGroup(group, slot);
                    var position = Positions[(slot - 1 + groupIndex * 3) % Positions.Length];
                    var player = new Player
                    {
                        Id = id,
                        ClubId = club.Id,
                        FullName = FaceBook.PersonName(nationality, seed),
                        Age = age,
                        SquadNumber = slot,
                        Position = position,
                        PreferredFoot = (seed & 1) == 0 ? PreferredFoot.Right : PreferredFoot.Left,
                        Nationality = nationality,
                        Reputation = 18 + Math.Abs(seed % 22),
                        WeeklyWage = 100 + Math.Abs(seed % 700),
                        ContractEndTicks = new DateTime(2029 + age % 3, 6, 30).Ticks,
                        Morale = 65 + Math.Abs(seed % 26),
                        Fatigue = 0,
                        Fitness = 75 + Math.Abs(seed % 26),
                        Form = 50 + Math.Abs(seed % 31),
                        Motivation = 65 + Math.Abs(seed % 31),
                        HeightCm = 158 + Math.Abs(seed % 35),
                        WeightKg = 48 + Math.Abs((seed >> 8) % 30),
                        WeakFootQuality = 1 + Math.Abs((seed >> 12) % 5),
                        TrainingRating = 55 + Math.Abs((seed >> 14) % 40),
                        TrainingFocus = TrainingFocusFor(position),
                        Injury = new InjuryState(),
                        Personality = (PersonalityTrait)Math.Abs(seed % 7),
                        Attributes = PlayerAttributes.Roll(seed, position, Math.Max(35, club.Reputation - 20)),
                        Traits = new List<PlayerTrait>()
                    };
                    player.Potential = Math.Min(92, PlayerAttributes.Overall(player) + 8 + Math.Abs((seed >> 4) % 16));
                    PlayerTraits.Normalize(player);
                    career.World.Players[id] = player;
                    if (!career.AcademyPlayerIds.Contains(id))
                        career.AcademyPlayerIds.Add(id);
                    currentCount++;
                    slot++;
                }
            }
        }

        public static void EnsureSchedule(CareerState career, string clubId)
        {
            if (career == null || career.World == null || career.Calendar == null || string.IsNullOrEmpty(clubId)
                || !career.World.Clubs.TryGetValue(clubId, out var club))
                return;

            var hasUnplayed = false;
            var cycle = 0;
            foreach (var fixture in career.AcademyFixtures)
            {
                if (fixture.ClubId != clubId) continue;
                if (!fixture.Played) hasUnplayed = true;
                if (!string.IsNullOrEmpty(fixture.Id))
                {
                    var lastColon = fixture.Id.LastIndexOf(':');
                    var cycleColon = lastColon > 0 ? fixture.Id.LastIndexOf(':', lastColon - 1) : -1;
                    if (cycleColon >= 0 && int.TryParse(fixture.Id.Substring(cycleColon + 1, lastColon - cycleColon - 1), out var fixtureCycle))
                        cycle = Math.Max(cycle, fixtureCycle);
                }
            }
            if (hasUnplayed)
                return;

            var opponents = new List<Club>();
            foreach (var other in career.World.Clubs.Values)
                if (other.Id != clubId && other.LeagueId == club.LeagueId)
                    opponents.Add(other);
            opponents.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            if (opponents.Count == 0)
                return;

            var start = career.Calendar.CurrentDate.Date.AddDays(7);
            for (var groupIndex = 0; groupIndex < AgeGroups.Length; groupIndex++)
            {
                for (var round = 0; round < FixturesPerGroup; round++)
                {
                    var opponent = opponents[(round + groupIndex * 3) % opponents.Count];
                    var home = round % 2 == 0;
                    career.AcademyFixtures.Add(new AcademyFixture
                    {
                        Id = "academy-match:" + clubId + ":" + AgeGroups[groupIndex] + ":" + (cycle + 1) + ":" + (round + 1),
                        ClubId = clubId,
                        AgeGroup = AgeGroups[groupIndex],
                        Date = start.AddDays(round * 7 + groupIndex * 2),
                        HomeClubId = home ? clubId : opponent.Id,
                        AwayClubId = home ? opponent.Id : clubId
                    });
                }
            }
            career.AcademyScheduleClubIds.Add(clubId);
        }

        public static string GroupForAge(int age) => age >= 19 ? "U20" : age >= 17 ? "U18" : "U16";

        static int AgeForGroup(string group, int slot)
        {
            if (group == "U20") return 19 + slot % 2;
            if (group == "U18") return 17 + slot % 2;
            return 15 + slot % 2;
        }

        static string Nationality(string leagueId)
        {
            if (string.IsNullOrEmpty(leagueId)) return "Portugal";
            if (leagueId.Contains("inglesa")) return "Inglaterra";
            if (leagueId.Contains("francesa")) return "França";
            if (leagueId.Contains("espanhola")) return "Espanha";
            if (leagueId.Contains("bundesliga")) return "Alemanha";
            if (leagueId.Contains("eredivisie")) return "Países Baixos";
            if (leagueId.Contains("brasileira")) return "Brasil";
            return "Portugal";
        }

        static string TrainingFocusFor(PlayerPosition position)
        {
            switch (position)
            {
                case PlayerPosition.Goalkeeper: return "positioning";
                case PlayerPosition.RightBack:
                case PlayerPosition.LeftBack:
                case PlayerPosition.CentreBack:
                case PlayerPosition.DefensiveMidfielder: return "defending";
                case PlayerPosition.RightWinger:
                case PlayerPosition.LeftWinger: return "pace";
                case PlayerPosition.Striker: return "finishing";
                default: return "passing";
            }
        }

        static int StableHash(string value)
        {
            unchecked
            {
                var hash = 17;
                for (var i = 0; i < value.Length; i++)
                    hash = hash * 31 + value[i];
                return hash & 0x7fffffff;
            }
        }
    }

    public static class AcademySeason
    {
        public static bool Advance(CareerState career)
        {
            if (career == null || career.Calendar == null || career.Club == null)
                return false;
            AcademyFactory.Ensure(career, career.Club.Id);
            AcademyFactory.EnsureSchedule(career, career.Club.Id);
            var changed = false;
            foreach (var fixture in career.AcademyFixtures)
            {
                if (fixture.Played || fixture.Date.Date > career.Calendar.CurrentDate.Date)
                    continue;
                if (!career.World.Clubs.TryGetValue(fixture.ClubId, out var club)
                    || !career.World.Clubs.TryGetValue(fixture.HomeClubId, out var home)
                    || !career.World.Clubs.TryGetValue(fixture.AwayClubId, out var away))
                    continue;

                var strength = AverageStrength(career, fixture.ClubId, fixture.AgeGroup);
                var homeStrength = fixture.HomeClubId == club.Id ? strength : Mathf.Clamp(35 + home.Reputation / 3, 35, 75);
                var awayStrength = fixture.AwayClubId == club.Id ? strength : Mathf.Clamp(35 + away.Reputation / 3, 35, 75);
                var seed = StableHash(fixture.Id) ^ (fixture.Date.DayOfYear * 7919);
                var random = new System.Random(seed);
                fixture.HomeGoals = Goals(random, homeStrength - awayStrength + 3);
                fixture.AwayGoals = Goals(random, awayStrength - homeStrength);
                fixture.Played = true;
                RecordAppearances(career, fixture, random);
                changed = true;
            }
            return changed;
        }

        static int AverageStrength(CareerState career, string clubId, string group)
        {
            var total = 0;
            var count = 0;
            foreach (var id in career.AcademyPlayerIds)
            {
                if (!career.World.Players.TryGetValue(id, out var player) || player.ClubId != clubId || AcademyFactory.GroupForAge(player.Age) != group)
                    continue;
                total += PlayerAttributes.Overall(player);
                count++;
            }
            return count == 0 ? 45 : total / count;
        }

        static void RecordAppearances(CareerState career, AcademyFixture fixture, System.Random random)
        {
            var squad = new List<Player>();
            foreach (var id in career.AcademyPlayerIds)
                if (career.World.Players.TryGetValue(id, out var player) && player.ClubId == fixture.ClubId && AcademyFactory.GroupForAge(player.Age) == fixture.AgeGroup)
                    squad.Add(player);
            if (squad.Count == 0)
                return;

            var eleven = new List<Player>();
            var bench = new List<Player>();
            var formation = career.World.Clubs.TryGetValue(fixture.ClubId, out var fixtureClub)
                && fixtureClub.Tactics != null ? fixtureClub.Tactics.Formation : "4-3-3";
            if (fixture.ClubId == career.Club.Id && career.AcademyTactics.TryGetValue(fixture.AgeGroup, out var academyTactics)
                && academyTactics != null && !string.IsNullOrEmpty(academyTactics.Formation))
                formation = academyTactics.Formation;
            StartingShape.Split(squad, formation, eleven, bench);
            var competitionId = "academy-" + fixture.AgeGroup;
            foreach (var player in eleven)
                AddStats(career, player.Id, competitionId, 1, 0, 0);
            var homeScore = fixture.HomeClubId == fixture.ClubId ? fixture.HomeGoals : fixture.AwayGoals;
            var awayScore = fixture.AwayClubId == fixture.ClubId ? fixture.HomeGoals : fixture.AwayGoals;
            var scored = homeScore;
            for (var i = 0; i < scored; i++)
            {
                var scorer = AttackingPlayer(eleven, random);
                if (scorer == null) continue;
                AddStats(career, scorer.Id, competitionId, 0, 1, 1);
                if (random.Next(100) < 72)
                {
                    var assistPool = new List<Player>(eleven);
                    assistPool.Remove(scorer);
                    if (assistPool.Count > 0)
                        AddStats(career, assistPool[random.Next(assistPool.Count)].Id, competitionId, 0, 0, 1);
                }
            }
            for (var i = 0; i < Math.Min(5, bench.Count); i++)
                AddStats(career, bench[i].Id, competitionId, 1, 0, 0);
        }

        static Player AttackingPlayer(List<Player> eleven, System.Random random)
        {
            var options = new List<Player>();
            foreach (var player in eleven)
                if (player.Position == PlayerPosition.Striker || player.Position == PlayerPosition.LeftWinger
                    || player.Position == PlayerPosition.RightWinger || player.Position == PlayerPosition.AttackingMidfielder
                    || player.Position == PlayerPosition.LeftMidfielder || player.Position == PlayerPosition.RightMidfielder)
                    options.Add(player);
            if (options.Count == 0) options.AddRange(eleven);
            return options.Count == 0 ? null : options[random.Next(options.Count)];
        }

        static void AddStats(CareerState career, string playerId, string competitionId, int apps, int goals, int assists)
        {
            SeasonLine line = null;
            foreach (var item in career.Season)
                if (item.PlayerId == playerId && item.CompetitionId == competitionId) { line = item; break; }
            if (line == null)
            {
                line = new SeasonLine { PlayerId = playerId, CompetitionId = competitionId };
                career.Season.Add(line);
            }
            line.Apps += apps;
            line.Goals += goals;
            line.Assists += assists;
            if (apps > 0)
            {
                line.RatingSum += 6.0f + goals * 0.6f + assists * 0.3f;
                line.Rated++;
            }
        }

        static int Goals(System.Random random, int edge)
        {
            var roll = random.NextDouble() + edge / 180.0;
            if (roll > 0.92) return 3;
            if (roll > 0.72) return 2;
            if (roll > 0.42) return 1;
            return 0;
        }

        static int StableHash(string value)
        {
            unchecked
            {
                var hash = 17;
                for (var i = 0; i < value.Length; i++) hash = hash * 31 + value[i];
                return hash & 0x7fffffff;
            }
        }
    }
}
