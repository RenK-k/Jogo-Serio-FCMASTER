using System;
using System.Collections.Generic;
using CoachMaster.Domain;

namespace CoachMaster.Career
{
    public static class SeasonSchedule
    {
        public static void Build(CareerState career)
        {
            career.Fixtures.Clear();
            career.Table.Clear();
            foreach (var league in career.World.Leagues)
            {
                var clubs = new List<Club>();
                foreach (var club in career.World.Clubs.Values)
                {
                    if (club.LeagueId == league.Id)
                        clubs.Add(club);
                }

                clubs.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                foreach (var club in clubs)
                    career.Table.Add(new LeagueTableRow { ClubId = club.Id, LeagueId = league.Id });

                if (league.Tier == LeagueTier.Continental)
                    continue;

                AddRoundRobin(career, league.Id, clubs, GameCalendar.SeasonStart.AddDays(34), false, 0);
                AddRoundRobin(career, league.Id, clubs, GameCalendar.SeasonStart.AddDays(34 + clubs.Count * 7), true, 80);
            }

            AddChampions(career);
            career.Fixtures.Sort((a, b) => a.Date.CompareTo(b.Date));
        }

        static void AddChampions(CareerState career)
        {
            var leagueId = "league:campeoes";
            var invited = new List<Club>();
            foreach (var league in career.World.Leagues)
            {
                if (league.Tier != LeagueTier.First)
                    continue;
                var clubs = new List<Club>();
                foreach (var club in career.World.Clubs.Values)
                {
                    if (club.LeagueId == league.Id)
                        clubs.Add(club);
                }

                clubs.Sort((a, b) => b.Reputation.CompareTo(a.Reputation));
                for (var i = 0; i < clubs.Count && i < 2; i++)
                    invited.Add(clubs[i]);
            }

            foreach (var club in invited)
                career.Table.Add(new LeagueTableRow { ClubId = club.Id, LeagueId = leagueId });
            AddRoundRobin(career, leagueId, invited, GameCalendar.SeasonStart.AddDays(20), false, 0);
        }

        static void AddRoundRobin(CareerState career, string leagueId, List<Club> clubs, DateTime start, bool reverse, int seed)
        {
            var n = clubs.Count;
            if (n < 2)
                return;

            var ids = new List<string>();
            foreach (var club in clubs)
                ids.Add(club.Id);
            if (n % 2 == 1)
                ids.Add(null);

            var count = ids.Count;
            var rounds = count - 1;
            var date = start;
            for (var round = 0; round < rounds; round++)
            {
                for (var i = 0; i < count / 2; i++)
                {
                    var a = ids[i];
                    var b = ids[count - 1 - i];
                    if (a == null || b == null)
                        continue;
                    var home = reverse ? b : a;
                    var away = reverse ? a : b;
                    if ((round + i) % 2 == 1)
                    {
                        var swap = home;
                        home = away;
                        away = swap;
                    }

                    career.Fixtures.Add(new Fixture
                    {
                        Id = $"fx:{leagueId}:{seed++}",
                        Date = date,
                        LeagueId = leagueId,
                        HomeClubId = home,
                        AwayClubId = away
                    });
                }

                var last = ids[count - 1];
                ids.RemoveAt(count - 1);
                ids.Insert(1, last);
                date = date.AddDays(7);
            }
        }

        public static Fixture Find(CareerState career, Fixture fixture)
        {
            if (career == null || fixture == null)
                return fixture;
            foreach (var item in career.Fixtures)
            {
                if (ReferenceEquals(item, fixture))
                    return item;
            }

            foreach (var item in career.Fixtures)
            {
                if (item.LeagueId == fixture.LeagueId && item.HomeClubId == fixture.HomeClubId && item.AwayClubId == fixture.AwayClubId && item.Date.Date == fixture.Date.Date)
                    return item;
            }

            career.Fixtures.Add(fixture);
            return fixture;
        }

        public static void SimulateOthers(CareerState career, DateTime upTo, Fixture skip)
        {
            var rng = new System.Random(upTo.DayOfYear * 17 + upTo.Month);
            foreach (var fixture in career.Fixtures)
            {
                if (fixture.Played || ReferenceEquals(fixture, skip) || fixture.Date.Date > upTo.Date)
                    continue;
                if (fixture.Involves(career.Coach.ClubId) && fixture.Date.Date == upTo.Date)
                    continue;

                var home = career.World.Clubs[fixture.HomeClubId].Reputation;
                var away = career.World.Clubs[fixture.AwayClubId].Reputation;
                fixture.HomeGoals = Goals(rng, home - away);
                fixture.AwayGoals = Goals(rng, away - home);
                ApplyResult(career, fixture);
            }
        }

        static int Goals(System.Random rng, int edge)
        {
            var roll = rng.NextDouble() + edge / 220.0;
            if (roll > 0.92) return 3;
            if (roll > 0.72) return 2;
            if (roll > 0.42) return 1;
            return 0;
        }

        public static void ApplyResult(CareerState career, Fixture fixture)
        {
            fixture.Played = true;
            ApplyTeam(career, fixture.LeagueId, fixture.HomeClubId, fixture.HomeGoals, fixture.AwayGoals);
            ApplyTeam(career, fixture.LeagueId, fixture.AwayClubId, fixture.AwayGoals, fixture.HomeGoals);
        }

        public static void RebuildTable(CareerState career)
        {
            if (career == null)
                return;
            foreach (var row in career.Table)
            {
                row.Played = 0;
                row.Won = 0;
                row.Drawn = 0;
                row.Lost = 0;
                row.GoalsFor = 0;
                row.GoalsAgainst = 0;
                row.Points = 0;
            }

            foreach (var fixture in career.Fixtures)
            {
                if (!fixture.Played)
                    continue;
                ApplyTeam(career, fixture.LeagueId, fixture.HomeClubId, fixture.HomeGoals, fixture.AwayGoals);
                ApplyTeam(career, fixture.LeagueId, fixture.AwayClubId, fixture.AwayGoals, fixture.HomeGoals);
            }
        }

        static void ApplyTeam(CareerState career, string leagueId, string clubId, int scored, int conceded)
        {
            LeagueTableRow found = null;
            foreach (var row in career.Table)
            {
                if (row.ClubId != clubId || row.LeagueId != leagueId)
                    continue;
                found = row;
                break;
            }

            if (found == null)
                return;
            found.Played++;
            found.GoalsFor += scored;
            found.GoalsAgainst += conceded;
            if (scored > conceded)
            {
                found.Won++;
                found.Points += 3;
            }
            else if (scored == conceded)
            {
                found.Drawn++;
                found.Points += 1;
            }
            else
                found.Lost++;
        }
    }
}
