using System.Collections.Generic;
using System.Text;
using CoachMaster.Domain;
using CoachMaster.MatchPlay;

namespace CoachMaster.Career
{
    public sealed class SeasonRank
    {
        public string Name;
        public string Club;
        public string Value;
    }

    public sealed class SeasonLine
    {
        public string PlayerId;
        public string CompetitionId;
        public int Goals;
        public int Assists;
        public int Apps;
        public float RatingSum;
        public int Rated;
    }

    public static class SeasonBook
    {
        public static void CreditLive(CareerState career, MatchSession match)
        {
            if (career == null || match == null || match.CareerStatsCredited)
                return;
            match.CareerStatsCredited = true;
            var competitionId = match.Fixture == null ? null : match.Fixture.LeagueId;
            foreach (var pair in match.Ratings)
            {
                if (match.Appeared != null && match.Appeared.Contains(pair.Key))
                {
                    Touch(career, competitionId, pair.Key, 0, 0, 1, pair.Value);
                    if (career.World.Players.TryGetValue(pair.Key, out var player))
                    {
                        var targetForm = System.Math.Max(10, System.Math.Min(99, (int)System.Math.Round(50f + (pair.Value - 6.5f) * 11f)));
                        player.Form = (int)System.Math.Round(player.Form * 0.72f + targetForm * 0.28f);
                    }
                }
            }

            foreach (var pair in match.GoalsScored)
                Touch(career, competitionId, pair.Key, pair.Value, 0, 0, 0f);
            foreach (var pair in match.Assists)
                Touch(career, competitionId, pair.Key, 0, pair.Value, 0, 0f);
        }

        public static void CreditScoreline(CareerState career, string competitionId, string clubId, int goals, int conceded)
        {
            if (career == null || string.IsNullOrEmpty(clubId) || !career.World.Clubs.TryGetValue(clubId, out var club))
                return;
            var squad = new List<Player>();
            foreach (var id in club.PlayerIds)
            {
                if (career.World.Players.TryGetValue(id, out var player))
                    squad.Add(player);
            }

            if (squad.Count == 0)
                return;
            var eleven = new List<Player>();
            var bench = new List<Player>();
            StartingShape.Resolve(squad, club, eleven, bench);
            if (eleven.Count == 0)
                return;

            var side = new List<Player>();
            foreach (var player in eleven)
                if (player.Position != PlayerPosition.Goalkeeper)
                    side.Add(player);
            if (side.Count == 0)
                side.AddRange(eleven);
            side.Sort((a, b) => Finish(b).CompareTo(Finish(a)));
            var seed = clubId.GetHashCode() ^ (goals * 17) ^ conceded;
            var rng = new System.Random(seed & 0x7fffffff);
            for (var g = 0; g < goals; g++)
            {
                var pick = side[rng.Next(0, System.Math.Min(4, side.Count))];
                var assist = side.Count > 1 ? side[rng.Next(0, side.Count)] : null;
                Touch(career, competitionId, pick.Id, 1, 0, 0, 0f);
                if (assist != null && assist.Id != pick.Id && rng.NextDouble() < 0.7)
                    Touch(career, competitionId, assist.Id, 0, 1, 0, 0f);
            }

            var mark = goals > conceded ? 7.1f : goals == conceded ? 6.6f : 6.2f;
            for (var i = 0; i < eleven.Count; i++)
                Touch(career, competitionId, eleven[i].Id, 0, 0, 1, mark + (eleven[i].Position == PlayerPosition.Goalkeeper && conceded == 0 ? 0.2f : 0f));
        }

        public static void Backfill(CareerState career)
        {
            if (career == null)
                return;

            var creditedCompetitions = new HashSet<string>();
            foreach (var line in career.Season)
            {
                if (!string.IsNullOrEmpty(line.CompetitionId))
                    creditedCompetitions.Add(line.CompetitionId);
            }

            var playedCompetitions = new HashSet<string>();
            foreach (var fixture in career.Fixtures)
            {
                if (fixture.Played && !string.IsNullOrEmpty(fixture.LeagueId))
                    playedCompetitions.Add(fixture.LeagueId);
            }

            foreach (var competitionId in playedCompetitions)
            {
                if (creditedCompetitions.Contains(competitionId))
                    continue;
                foreach (var fixture in career.Fixtures)
                {
                    if (!fixture.Played || fixture.LeagueId != competitionId)
                        continue;
                    CreditScoreline(career, competitionId, fixture.HomeClubId, fixture.HomeGoals, fixture.AwayGoals);
                    CreditScoreline(career, competitionId, fixture.AwayClubId, fixture.AwayGoals, fixture.HomeGoals);
                }
            }
        }

        public static void ReconcileCurrentSeason(CareerState career)
        {
            if (career == null)
                return;

            var corrections = new Dictionary<string, SeasonLine>();
            var playedByTeam = new Dictionary<string, int>();
            foreach (var fixture in career.Fixtures)
            {
                if (!fixture.Played || string.IsNullOrEmpty(fixture.LeagueId))
                    continue;
                AddTeamMatch(playedByTeam, fixture.LeagueId, fixture.HomeClubId, 1);
                AddTeamMatch(playedByTeam, fixture.LeagueId, fixture.AwayClubId, 1);
            }

            foreach (var line in career.Season)
            {
                if (line == null || !career.World.Players.TryGetValue(line.PlayerId, out var player) || player == null)
                    continue;
                var competitionId = line.CompetitionId;
                if (string.IsNullOrEmpty(competitionId)
                    && career.World.Clubs.TryGetValue(player.ClubId, out var owner))
                {
                    competitionId = owner.LeagueId;
                    line.CompetitionId = competitionId;
                }

                var key = TeamCompetitionKey(competitionId, player.ClubId);
                if (!playedByTeam.TryGetValue(key, out var maximumApps) || maximumApps < 1)
                    continue;

                var previousApps = line.Apps;
                var previousRatings = line.Rated;
                if (line.Apps > maximumApps)
                    line.Apps = maximumApps;

                var validRatings = System.Math.Min(line.Rated, line.Apps);
                var ratingSum = line.RatingSum;
                if (validRatings != line.Rated)
                {
                    var average = line.Rated > 0 ? line.RatingSum / line.Rated : 0f;
                    line.Rated = validRatings;
                    line.RatingSum = average * validRatings;
                }

                var removedApps = previousApps - line.Apps;
                var removedRatings = previousRatings - line.Rated;
                var removedRatingSum = ratingSum - line.RatingSum;
                if (removedApps > 0 || removedRatings > 0 || removedRatingSum > 0.001f)
                {
                    if (!corrections.TryGetValue(line.PlayerId, out var correction))
                    {
                        correction = new SeasonLine { PlayerId = line.PlayerId };
                        corrections.Add(line.PlayerId, correction);
                    }
                    correction.Apps += removedApps;
                    correction.Rated += removedRatings;
                    correction.RatingSum += removedRatingSum;
                }
            }

            foreach (var line in career.CareerTotals)
            {
                if (line == null || !corrections.TryGetValue(line.PlayerId, out var correction))
                    continue;
                line.Apps = System.Math.Max(0, line.Apps - correction.Apps);
                line.Rated = System.Math.Max(0, line.Rated - correction.Rated);
                line.RatingSum = System.Math.Max(0f, line.RatingSum - correction.RatingSum);
            }
        }

        static void AddTeamMatch(Dictionary<string, int> totals, string competitionId, string clubId, int amount)
        {
            var key = TeamCompetitionKey(competitionId, clubId);
            totals.TryGetValue(key, out var current);
            totals[key] = current + amount;
        }

        static string TeamCompetitionKey(string competitionId, string clubId)
        {
            return (competitionId ?? string.Empty) + "|" + (clubId ?? string.Empty);
        }

        public static string List(CareerState career, string leagueId, string kind)
        {
            var rows = Ranks(career, leagueId, kind);
            if (rows.Count == 0)
                return "Ainda não há números nesta lista.";
            var text = new StringBuilder();
            for (var i = 0; i < rows.Count; i++)
                text.Append(i + 1).Append(". ").Append(rows[i].Name).Append("  (").Append(rows[i].Club).Append(")  —  ").Append(rows[i].Value).Append('\n');
            return text.ToString().Trim();
        }

        public static List<SeasonRank> Ranks(CareerState career, string leagueId, string kind)
        {
            var rows = new List<SeasonLine>();
            foreach (var line in career.Season)
            {
                if (!career.World.Players.TryGetValue(line.PlayerId, out var player))
                    continue;
                if (line.CompetitionId != leagueId)
                    continue;
                if (kind == "media" && line.Rated < 1)
                    continue;
                if (kind == "golos" && line.Goals <= 0)
                    continue;
                if (kind == "assist" && line.Assists <= 0)
                    continue;
                rows.Add(line);
            }

            if (kind == "golos")
                rows.Sort((a, b) => b.Goals.CompareTo(a.Goals));
            else if (kind == "assist")
                rows.Sort((a, b) => b.Assists.CompareTo(a.Assists));
            else
                rows.Sort((a, b) => Average(b).CompareTo(Average(a)));

            var shown = new List<SeasonRank>();
            var count = rows.Count < 8 ? rows.Count : 8;
            for (var i = 0; i < count; i++)
            {
                var line = rows[i];
                career.World.Players.TryGetValue(line.PlayerId, out var player);
                var clubName = "";
                if (player != null && career.World.Clubs.TryGetValue(player.ClubId, out var club))
                    clubName = club.ShortName;
                var value = kind == "golos" ? line.Goals + " golos" : kind == "assist" ? line.Assists + " assistências" : Average(line).ToString("0.00");
                shown.Add(new SeasonRank
                {
                    Name = player == null ? line.PlayerId : player.FullName,
                    Club = clubName,
                    Value = value
                });
            }

            return shown;
        }

        static float Average(SeasonLine line)
        {
            return line.Rated <= 0 ? 0f : line.RatingSum / line.Rated;
        }

        static int Finish(Player player)
        {
            return player.Attributes == null ? 0 : player.Attributes.Finishing + player.Attributes.Composure;
        }

        static void Touch(CareerState career, string competitionId, string playerId, int goals, int assists, int apps, float rating)
        {
            if (string.IsNullOrEmpty(playerId))
                return;
            if (string.IsNullOrEmpty(competitionId) && career.World.Players.TryGetValue(playerId, out var owner) && career.World.Clubs.TryGetValue(owner.ClubId, out var ownerClub))
                competitionId = ownerClub.LeagueId;
            AddTo(career.Season, playerId, competitionId, goals, assists, apps, rating);
            AddTo(career.CareerTotals, playerId, null, goals, assists, apps, rating);
        }

        static void AddTo(List<SeasonLine> lines, string playerId, string competitionId, int goals, int assists, int apps, float rating)
        {
            SeasonLine line = null;
            foreach (var item in lines)
            {
                if (item.PlayerId != playerId || item.CompetitionId != competitionId)
                    continue;
                line = item;
                break;
            }

            if (line == null)
            {
                line = new SeasonLine { PlayerId = playerId, CompetitionId = competitionId };
                lines.Add(line);
            }

            line.Goals += goals;
            line.Assists += assists;
            line.Apps += apps;
            if (rating > 0f)
            {
                line.RatingSum += rating;
                line.Rated++;
            }
        }
    }
}
