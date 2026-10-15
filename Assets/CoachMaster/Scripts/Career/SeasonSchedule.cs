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
                {
                    career.Table.Add(new LeagueTableRow { ClubId = club.Id });
                }

                AddRoundRobin(career, league.Id, clubs, GameCalendar.SeasonStart.AddDays(34), false);
                AddRoundRobin(career, league.Id, clubs, GameCalendar.SeasonStart.AddDays(34 + clubs.Count * 7), true);
            }

            career.Fixtures.Sort((a, b) => a.Date.CompareTo(b.Date));
        }

        static void AddRoundRobin(CareerState career, string leagueId, List<Club> clubs, DateTime start, bool reverse)
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
            var seed = 0;
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

        public static void SimulateOthers(CareerState career, DateTime upTo, string skipFixtureId)
        {
            var rng = new System.Random(upTo.DayOfYear * 17 + upTo.Month);
            foreach (var fixture in career.Fixtures)
            {
                if (fixture.Played || fixture.Id == skipFixtureId || fixture.Date.Date > upTo.Date)
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
            ApplyTeam(career, fixture.HomeClubId, fixture.HomeGoals, fixture.AwayGoals);
            ApplyTeam(career, fixture.AwayClubId, fixture.AwayGoals, fixture.HomeGoals);
        }

        static void ApplyTeam(CareerState career, string clubId, int scored, int conceded)
        {
            foreach (var row in career.Table)
            {
                if (row.ClubId != clubId)
                    continue;
                row.Played++;
                row.GoalsFor += scored;
                row.GoalsAgainst += conceded;
                if (scored > conceded)
                {
                    row.Won++;
                    row.Points += 3;
                }
                else if (scored == conceded)
                {
                    row.Drawn++;
                    row.Points += 1;
                }
                else
                    row.Lost++;
                return;
            }
        }
    }
}
