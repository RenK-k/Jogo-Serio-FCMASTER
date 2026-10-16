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
            if (career == null || match == null)
                return;
            var seen = new HashSet<string>();
            foreach (var pair in match.Ratings)
            {
                Touch(career, pair.Key, 0, 0, 1, pair.Value);
                seen.Add(pair.Key);
            }

            foreach (var pair in match.GoalsScored)
                Touch(career, pair.Key, pair.Value, 0, seen.Contains(pair.Key) ? 0 : 1, 0f);
            foreach (var pair in match.Assists)
                Touch(career, pair.Key, 0, pair.Value, 0, 0f);
        }

        public static void CreditScoreline(CareerState career, string clubId, int goals, int conceded)
        {
            if (career == null || string.IsNullOrEmpty(clubId) || !career.World.Clubs.TryGetValue(clubId, out var club))
                return;
            var side = new List<Player>();
            foreach (var id in club.PlayerIds)
            {
                if (career.World.Players.TryGetValue(id, out var player) && player.Position != PlayerPosition.Goalkeeper)
                    side.Add(player);
            }

            if (side.Count == 0)
                return;
            side.Sort((a, b) => Finish(b).CompareTo(Finish(a)));
            var seed = clubId.GetHashCode() ^ (goals * 17) ^ conceded;
            var rng = new System.Random(seed < 0 ? -seed : seed);
            for (var g = 0; g < goals; g++)
            {
                var pick = side[rng.Next(0, System.Math.Min(4, side.Count))];
                var assist = side.Count > 1 ? side[rng.Next(0, side.Count)] : null;
                Touch(career, pick.Id, 1, 0, 0, 0f);
                if (assist != null && assist.Id != pick.Id && rng.NextDouble() < 0.7)
                    Touch(career, assist.Id, 0, 1, 0, 0f);
            }

            var apps = System.Math.Min(10, side.Count);
            var mark = goals > conceded ? 7.1f : goals == conceded ? 6.6f : 6.2f;
            for (var i = 0; i < apps; i++)
                Touch(career, side[i].Id, 0, 0, 1, mark + (i == 0 ? 0.4f : 0f));
        }

        public static void Backfill(CareerState career)
        {
            if (career == null || career.Season.Count > 0)
                return;
            foreach (var fixture in career.Fixtures)
            {
                if (!fixture.Played)
                    continue;
                CreditScoreline(career, fixture.HomeClubId, fixture.HomeGoals, fixture.AwayGoals);
                CreditScoreline(career, fixture.AwayClubId, fixture.AwayGoals, fixture.HomeGoals);
            }
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
                if (!career.World.Clubs.TryGetValue(player.ClubId, out var club) || club.LeagueId != leagueId)
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

        static void Touch(CareerState career, string playerId, int goals, int assists, int apps, float rating)
        {
            if (string.IsNullOrEmpty(playerId))
                return;
            SeasonLine line = null;
            foreach (var item in career.Season)
            {
                if (item.PlayerId != playerId)
                    continue;
                line = item;
                break;
            }

            if (line == null)
            {
                line = new SeasonLine { PlayerId = playerId };
                career.Season.Add(line);
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
