using System;
using System.Collections.Generic;
using CoachMaster.Domain;

namespace CoachMaster.Career
{
    public static class SeasonSchedule
    {
        public static void Build(CareerState career)
        {
            var invited = Qualifiers(career);
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

                var anchor = SeasonAnchor(career);
                AddRoundRobin(career, league.Id, clubs, anchor.AddDays(34), false, 0);
                AddRoundRobin(career, league.Id, clubs, anchor.AddDays(34 + clubs.Count * 7), true, 80);
                AddCup(career, "cup:" + Slug(league.Id) + ":taca", clubs, anchor.AddDays(48));
                AddCup(career, "cup:" + Slug(league.Id) + ":liga", clubs, anchor.AddDays(69));
            }

            AddChampions(career, invited);
            career.Fixtures.Sort((a, b) => a.Date.CompareTo(b.Date));
        }

        static void AddChampions(CareerState career, List<Club> invited)
        {
            var leagueId = "league:campeoes";
            if (invited == null)
                invited = new List<Club>();

            foreach (var club in invited)
                career.Table.Add(new LeagueTableRow { ClubId = club.Id, LeagueId = leagueId });
            AddRoundRobin(career, leagueId, invited, SeasonAnchor(career).AddDays(20), false, 0);
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

        public static void SimulateOthers(CareerState career, DateTime upTo, Fixture skip, bool skipUser = false)
        {
            var rng = new System.Random(upTo.DayOfYear * 17 + upTo.Month);
            foreach (var fixture in career.Fixtures)
            {
                if (fixture.Played || ReferenceEquals(fixture, skip) || fixture.Date.Date > upTo.Date)
                    continue;
                if (skipUser && fixture.Involves(career.Coach.ClubId))
                    continue;
                if (fixture.Involves(career.Coach.ClubId) && fixture.Date.Date == upTo.Date)
                    continue;

                if (fixture.Walkover || !career.World.Clubs.ContainsKey(fixture.HomeClubId) || !career.World.Clubs.ContainsKey(fixture.AwayClubId))
                    continue;
                var home = career.World.Clubs[fixture.HomeClubId].Reputation;
                var away = career.World.Clubs[fixture.AwayClubId].Reputation;
                fixture.HomeGoals = Goals(rng, home - away);
                fixture.AwayGoals = Goals(rng, away - home);
                if (IsCup(fixture.LeagueId) && fixture.HomeGoals == fixture.AwayGoals)
                {
                    if (home >= away)
                        fixture.HomeGoals++;
                    else
                        fixture.AwayGoals++;
                }

                ApplyResult(career, fixture);
                if (!IsCup(fixture.LeagueId))
                {
                    SeasonBook.CreditScoreline(career, fixture.HomeClubId, fixture.HomeGoals, fixture.AwayGoals);
                    SeasonBook.CreditScoreline(career, fixture.AwayClubId, fixture.AwayGoals, fixture.HomeGoals);
                }
            }

            AdvanceCups(career);
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

        public static bool TryRoll(CareerState career)
        {
            if (career == null || HasPendingUser(career))
                return false;

            var guard = 0;
            while (guard++ < 16)
            {
                var before = career.Fixtures.Count;
                SimulateRest(career);
                AdvanceCups(career);
                if (HasPendingUser(career))
                    return false;
                if (career.Fixtures.Count == before && AllPlayed(career))
                {
                    CloseSeason(career);
                    return true;
                }

                if (career.Fixtures.Count == before)
                    break;
            }

            return false;
        }

        public static string Competition(string leagueId)
        {
            if (string.IsNullOrEmpty(leagueId))
                return "";
            if (leagueId == "league:campeoes")
                return "Liga dos Campeões";
            if (!IsCup(leagueId))
                return "";
            var slug = leagueId.Substring(4);
            var which = "taca";
            var cut = slug.LastIndexOf(':');
            if (cut >= 0)
            {
                which = slug.Substring(cut + 1);
                slug = slug.Substring(0, cut);
            }

            string main;
            string league;
            CupPair(slug, out main, out league);
            return which == "liga" ? league : main;
        }

        static void SimulateRest(CareerState career)
        {
            SimulateOthers(career, career.Calendar.CurrentDate.AddYears(3), null, true);
        }

        static bool HasPendingUser(CareerState career)
        {
            foreach (var fixture in career.Fixtures)
            {
                if (!fixture.Played && !fixture.Walkover && fixture.Involves(career.Coach.ClubId))
                    return true;
            }

            return false;
        }

        static bool AllPlayed(CareerState career)
        {
            if (career.Fixtures.Count == 0)
                return false;
            foreach (var fixture in career.Fixtures)
            {
                if (!fixture.Played && !fixture.Walkover)
                    return false;
            }

            return true;
        }

        static void CloseSeason(CareerState career)
        {
            PublishAwards(career);
            Promote(career);
            RestSquads(career);
            var next = NextSeasonStart(career);
            career.Calendar.SetInstant(next);
            career.Season.Clear();
            Build(career);
            Newsroom.Publish(career, "Diário da Liga", "Arranca a época " + next.Year + "/" + (next.Year + 1),
                "O calendário novo está fechado. Ligas, taças e Liga dos Campeões voltam a contar do zero. O plantel regressa de férias com as pernas mais leves.");
        }

        static void PublishAwards(CareerState career)
        {
            foreach (var league in career.World.Leagues)
            {
                if (league.Tier == LeagueTier.Continental)
                    continue;
                var rows = Ranked(career, league.Id);
                if (rows.Count == 0 || rows[0].Played == 0)
                    continue;
                var champ = ClubName(career, rows[0].ClubId);
                var yours = rows[0].ClubId == career.Coach.ClubId ? " É o teu clube." : "";
                Newsroom.Publish(career, "Diário da Liga", champ + " é campeão da " + league.Name,
                    champ + " fecha a " + league.Name + " com " + rows[0].Points + " pontos, " + rows[0].Won + " vitórias e " + rows[0].GoalsFor + " golos marcados." + yours);
            }

            var champs = Ranked(career, "league:campeoes");
            if (champs.Count > 0 && champs[0].Played > 0)
            {
                var name = ClubName(career, champs[0].ClubId);
                Newsroom.Publish(career, "A Gazeta do Relvado", name + " levanta a Liga dos Campeões",
                    "A prova dos primeiros classificados de cada país fica em " + name + ", com " + champs[0].Points + " pontos.");
            }

            var cups = new List<string>();
            foreach (var fixture in career.Fixtures)
            {
                if (!IsCup(fixture.LeagueId) || cups.Contains(fixture.LeagueId))
                    continue;
                cups.Add(fixture.LeagueId);
            }

            foreach (var cupId in cups)
            {
                var winner = CupWinner(career, cupId);
                if (string.IsNullOrEmpty(winner))
                    continue;
                var title = Competition(cupId);
                var name = ClubName(career, winner);
                var yours = winner == career.Coach.ClubId ? " O teu clube fica com o troféu." : "";
                Newsroom.Publish(career, "O Bancada", name + " vence a " + title, name + " leva a " + title + " nesta época." + yours);
            }

            var golden = Ballon(career, out var goals, out var average);
            if (golden != null)
            {
                Newsroom.Publish(career, "A Gazeta do Relvado", "Bola de Ouro para " + golden.FullName,
                    golden.FullName + ", do " + ClubName(career, golden.ClubId) + ", fecha com média " + average.ToString("0.00") + " e " + goals + " golos. O prémio vai para quem pesou mais na época.");
            }

            var boot = TopScorer(career);
            if (boot != null)
            {
                career.World.Players.TryGetValue(boot.PlayerId, out var player);
                var who = player == null ? "O melhor marcador" : player.FullName;
                var club = player == null ? "" : ClubName(career, player.ClubId);
                Newsroom.Publish(career, "Diário da Liga", "Bota de Ouro: " + who + " com " + boot.Goals + " golos",
                    who + (string.IsNullOrEmpty(club) ? "" : ", do " + club + ",") + " termina como melhor marcador da época, com " + boot.Goals + " golos.");
            }

            var moves = PromotionText(career);
            if (!string.IsNullOrEmpty(moves))
                Newsroom.Publish(career, "Diário da Liga", "Sobe e desce fechado", moves);
        }

        static void Promote(CareerState career)
        {
            SwapEnds(career, "league:atlas", "league:horizonte");
            SwapEnds(career, "league:inglesa", "league:inglesa-2");
            SwapEnds(career, "league:francesa", "league:francesa-2");
            SwapEnds(career, "league:espanhola", "league:espanhola-2");
        }

        static string PromotionText(CareerState career)
        {
            var text = "";
            text += PairMove(career, "Liga Portugal", "league:atlas", "league:horizonte");
            text += PairMove(career, "Inglaterra", "league:inglesa", "league:inglesa-2");
            text += PairMove(career, "França", "league:francesa", "league:francesa-2");
            text += PairMove(career, "Espanha", "league:espanhola", "league:espanhola-2");
            return text.Trim();
        }

        static string PairMove(CareerState career, string country, string firstId, string secondId)
        {
            var top = Ranked(career, firstId);
            var second = Ranked(career, secondId);
            if (top.Count < 3 || second.Count < 2 || top[0].Played == 0 || second[0].Played == 0)
                return "";
            var downA = ClubName(career, top[top.Count - 1].ClubId);
            var downB = ClubName(career, top[top.Count - 2].ClubId);
            var upA = ClubName(career, second[0].ClubId);
            var upB = ClubName(career, second[1].ClubId);
            return country + ": sobem " + upA + " e " + upB + ". Descem " + downB + " e " + downA + ".\n";
        }

        static void SwapEnds(CareerState career, string firstId, string secondId)
        {
            var top = Ranked(career, firstId);
            var second = Ranked(career, secondId);
            if (top.Count < 3 || second.Count < 2 || top[0].Played == 0 || second[0].Played == 0)
                return;
            var down = new[] { top[top.Count - 1].ClubId, top[top.Count - 2].ClubId };
            var up = new[] { second[0].ClubId, second[1].ClubId };
            Move(career, down[0], secondId);
            Move(career, down[1], secondId);
            Move(career, up[0], firstId);
            Move(career, up[1], firstId);
        }

        static void Move(CareerState career, string clubId, string leagueId)
        {
            if (career.World.Clubs.TryGetValue(clubId, out var club))
                club.LeagueId = leagueId;
        }

        static void RestSquads(CareerState career)
        {
            foreach (var player in career.World.Players.Values)
            {
                player.Age = player.Age < 42 ? player.Age + 1 : player.Age;
                player.Fatigue = 10;
                if (player.Morale < 70)
                    player.Morale += 6;
                if (player.Morale > 90)
                    player.Morale = 90;
                if (player.Injury != null)
                {
                    player.Injury.IsInjured = false;
                    player.Injury.DaysRemaining = 0;
                    player.Injury.Description = "";
                }
            }
        }

        static List<Club> Qualifiers(CareerState career)
        {
            var invited = new List<Club>();
            foreach (var league in career.World.Leagues)
            {
                if (league.Tier != LeagueTier.First)
                    continue;
                var rows = Ranked(career, league.Id);
                if (rows.Count > 0 && rows[0].Played > 0)
                {
                    for (var i = 0; i < rows.Count && i < 2; i++)
                    {
                        if (career.World.Clubs.TryGetValue(rows[i].ClubId, out var club))
                            invited.Add(club);
                    }

                    continue;
                }

                var clubs = Clubs(career, league.Id);
                clubs.Sort((a, b) => b.Reputation.CompareTo(a.Reputation));
                for (var i = 0; i < clubs.Count && i < 2; i++)
                    invited.Add(clubs[i]);
            }

            return invited;
        }

        static void AddCup(CareerState career, string cupId, List<Club> clubs, DateTime start)
        {
            var ids = new List<string>();
            foreach (var club in clubs)
                ids.Add(club.Id);
            for (var i = ids.Count - 1; i > 0; i--)
            {
                var j = Mix(ids[i], i) % (i + 1);
                var swap = ids[i];
                ids[i] = ids[j];
                ids[j] = swap;
            }

            PairCup(career, cupId, 0, ids, start);
        }

        public static void AdvanceCups(CareerState career)
        {
            var cups = new List<string>();
            foreach (var fixture in career.Fixtures)
            {
                if (!IsCup(fixture.LeagueId) || cups.Contains(fixture.LeagueId))
                    continue;
                cups.Add(fixture.LeagueId);
            }

            foreach (var cupId in cups)
            {
                var round = -1;
                foreach (var fixture in career.Fixtures)
                {
                    if (fixture.LeagueId != cupId)
                        continue;
                    var found = RoundOf(fixture.Id);
                    if (found > round)
                        round = found;
                }

                if (round < 0 || !RoundComplete(career, cupId, round))
                    continue;
                if (HasRound(career, cupId, round + 1))
                    continue;
                var winners = Winners(career, cupId, round);
                if (winners.Count < 2)
                    continue;
                DateTime last = SeasonAnchor(career);
                foreach (var fixture in career.Fixtures)
                {
                    if (fixture.LeagueId == cupId && fixture.Date > last)
                        last = fixture.Date;
                }

                PairCup(career, cupId, round + 1, winners, last.AddDays(18));
            }
        }

        static void PairCup(CareerState career, string cupId, int round, List<string> ids, DateTime date)
        {
            var alive = new List<string>(ids);
            if (alive.Count % 2 == 1)
            {
                var bye = alive[alive.Count - 1];
                alive.RemoveAt(alive.Count - 1);
                career.Fixtures.Add(new Fixture
                {
                    Id = "fx:" + cupId + ":r" + round + ":bye",
                    Date = date,
                    LeagueId = cupId,
                    HomeClubId = bye,
                    AwayClubId = bye,
                    Played = true,
                    HomeGoals = 1,
                    AwayGoals = 0
                });
            }

            for (var i = 0; i + 1 < alive.Count; i += 2)
            {
                var when = OpenDate(career, alive[i], alive[i + 1], date);
                career.Fixtures.Add(new Fixture
                {
                    Id = "fx:" + cupId + ":r" + round + ":" + (i / 2),
                    Date = when,
                    LeagueId = cupId,
                    HomeClubId = alive[i],
                    AwayClubId = alive[i + 1]
                });
            }
        }

        static DateTime OpenDate(CareerState career, string home, string away, DateTime date)
        {
            var guard = 0;
            while (guard++ < 21)
            {
                var busy = false;
                foreach (var fixture in career.Fixtures)
                {
                    if (fixture.Walkover || fixture.Date.Date != date.Date)
                        continue;
                    if (fixture.Involves(home) || fixture.Involves(away))
                    {
                        busy = true;
                        break;
                    }
                }

                if (!busy)
                    return date;
                date = date.AddDays(1);
            }

            return date;
        }

        static bool RoundComplete(CareerState career, string cupId, int round)
        {
            var any = false;
            foreach (var fixture in career.Fixtures)
            {
                if (fixture.LeagueId != cupId || RoundOf(fixture.Id) != round)
                    continue;
                any = true;
                if (!fixture.Played && !fixture.Walkover)
                    return false;
            }

            return any;
        }

        static bool HasRound(CareerState career, string cupId, int round)
        {
            foreach (var fixture in career.Fixtures)
            {
                if (fixture.LeagueId == cupId && RoundOf(fixture.Id) == round)
                    return true;
            }

            return false;
        }

        static List<string> Winners(CareerState career, string cupId, int round)
        {
            var list = new List<string>();
            foreach (var fixture in career.Fixtures)
            {
                if (fixture.LeagueId != cupId || RoundOf(fixture.Id) != round || !fixture.Played)
                    continue;
                var winner = WinnerOf(career, fixture);
                if (!string.IsNullOrEmpty(winner))
                    list.Add(winner);
            }

            return list;
        }

        static string CupWinner(CareerState career, string cupId)
        {
            var round = -1;
            foreach (var fixture in career.Fixtures)
            {
                if (fixture.LeagueId != cupId)
                    continue;
                var found = RoundOf(fixture.Id);
                if (found > round)
                    round = found;
            }

            if (round < 0 || !RoundComplete(career, cupId, round))
                return null;
            var winners = Winners(career, cupId, round);
            return winners.Count == 1 ? winners[0] : null;
        }

        static string WinnerOf(CareerState career, Fixture fixture)
        {
            if (fixture.Walkover)
                return fixture.HomeClubId;
            if (fixture.HomeGoals != fixture.AwayGoals)
                return fixture.HomeGoals > fixture.AwayGoals ? fixture.HomeClubId : fixture.AwayClubId;
            var home = career.World.Clubs.ContainsKey(fixture.HomeClubId) ? career.World.Clubs[fixture.HomeClubId].Reputation : 0;
            var away = career.World.Clubs.ContainsKey(fixture.AwayClubId) ? career.World.Clubs[fixture.AwayClubId].Reputation : 0;
            return home >= away ? fixture.HomeClubId : fixture.AwayClubId;
        }

        static int RoundOf(string id)
        {
            if (string.IsNullOrEmpty(id))
                return 0;
            var mark = id.LastIndexOf(":r", StringComparison.Ordinal);
            if (mark < 0 || mark + 2 >= id.Length)
                return 0;
            var tail = id.Substring(mark + 2);
            var cut = tail.IndexOf(':');
            if (cut >= 0)
                tail = tail.Substring(0, cut);
            int round;
            return int.TryParse(tail, out round) ? round : 0;
        }

        static SeasonLine TopScorer(CareerState career)
        {
            SeasonLine best = null;
            foreach (var line in career.Season)
            {
                if (line.Goals <= 0)
                    continue;
                if (best == null || line.Goals > best.Goals)
                    best = line;
            }

            return best;
        }

        static Player Ballon(CareerState career, out int goals, out float average)
        {
            goals = 0;
            average = 0f;
            Player best = null;
            var score = -1f;
            foreach (var line in career.Season)
            {
                if (line.Rated < 3 || !career.World.Players.TryGetValue(line.PlayerId, out var player))
                    continue;
                var mean = line.RatingSum / line.Rated;
                var mark = mean * 8f + line.Goals * 1.4f + line.Assists * 0.6f;
                if (mark <= score)
                    continue;
                score = mark;
                best = player;
                goals = line.Goals;
                average = mean;
            }

            return best;
        }

        static List<LeagueTableRow> Ranked(CareerState career, string leagueId)
        {
            var rows = new List<LeagueTableRow>();
            foreach (var row in career.Table)
            {
                if (row.LeagueId == leagueId)
                    rows.Add(row);
            }

            rows.Sort((a, b) =>
            {
                var points = b.Points.CompareTo(a.Points);
                if (points != 0)
                    return points;
                var diff = b.GoalDifference.CompareTo(a.GoalDifference);
                if (diff != 0)
                    return diff;
                return b.GoalsFor.CompareTo(a.GoalsFor);
            });
            return rows;
        }

        static List<Club> Clubs(CareerState career, string leagueId)
        {
            var clubs = new List<Club>();
            foreach (var club in career.World.Clubs.Values)
            {
                if (club.LeagueId == leagueId)
                    clubs.Add(club);
            }

            return clubs;
        }

        static string ClubName(CareerState career, string clubId)
        {
            return career.World.Clubs.TryGetValue(clubId, out var club) ? club.ShortName : clubId;
        }

        static void CupPair(string slug, out string main, out string league)
        {
            if (slug.StartsWith("inglesa"))
            {
                main = "FA Cup";
                league = "EFL Cup";
            }
            else if (slug.StartsWith("francesa"))
            {
                main = "Coupe de France";
                league = "Coupe de la Ligue";
            }
            else if (slug.StartsWith("espanhola"))
            {
                main = "Copa del Rey";
                league = "Copa de la Liga";
            }
            else
            {
                main = "Taça de Portugal";
                league = "Taça da Liga";
            }

            if (slug.EndsWith("-2") || slug == "horizonte")
            {
                main += " (2ª)";
                league += " (2ª)";
            }
        }

        static DateTime SeasonAnchor(CareerState career)
        {
            var date = career != null && career.Calendar != null ? career.Calendar.CurrentDate : GameCalendar.SeasonStart;
            var year = date.Month >= 6 ? date.Year : date.Year - 1;
            return new DateTime(year, 7, 13);
        }

        static DateTime NextSeasonStart(CareerState career)
        {
            var date = career.Calendar.CurrentDate;
            var year = date.Month >= 6 ? date.Year + 1 : date.Year;
            return new DateTime(year, 7, 13, 8, 0, 0);
        }

        static string Slug(string leagueId)
        {
            return leagueId.StartsWith("league:") ? leagueId.Substring(7) : leagueId;
        }

        static bool IsCup(string leagueId)
        {
            return !string.IsNullOrEmpty(leagueId) && leagueId.StartsWith("cup:");
        }

        static int Mix(string value, int salt)
        {
            unchecked
            {
                var hash = 17 + salt;
                if (value != null)
                {
                    foreach (var c in value)
                        hash = hash * 31 + c;
                }

                return hash & 0x7fffffff;
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
