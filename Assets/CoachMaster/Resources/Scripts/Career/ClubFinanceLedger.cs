using System;
using System.Collections.Generic;
using CoachMaster.Domain;

namespace CoachMaster.Career
{
    public static class ClubFinanceLedger
    {
        const int MaxTransactions = 240;

        public static void SettleWeeklyPayroll(CareerState career, DateTime fromDate, DateTime toDate)
        {
            if (career == null || career.World == null || toDate.Date <= fromDate.Date)
                return;

            var daysUntilMonday = ((int)DayOfWeek.Monday - (int)fromDate.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0)
                daysUntilMonday = 7;
            var payday = fromDate.Date.AddDays(daysUntilMonday);
            while (payday <= toDate.Date)
            {
                foreach (var club in career.World.Clubs.Values)
                {
                    if (club == null || club.Finance == null || club.PlayerIds == null)
                        continue;
                    long payroll = 0;
                    foreach (var playerId in club.PlayerIds)
                        if (career.World.Players.TryGetValue(playerId, out var player) && player != null)
                            payroll += Math.Max(0, player.WeeklyWage);
                    if (payroll <= 0)
                        continue;

                    club.Finance.Balance = (int)Math.Max(int.MinValue, (long)club.Finance.Balance - payroll);
                    var amount = -(int)Math.Min(int.MaxValue, payroll);
                    Record(club, payday, amount, "wages", "Weekly payroll");
                }
                payday = payday.AddDays(7);
            }
        }

        public static void Record(Club club, DateTime date, int amount, string category, string description, string playerId = null)
        {
            if (club == null || club.Finance == null || amount == 0)
                return;

            club.Finance.Transactions.Add(new FinanceTransaction
            {
                DateTicks = date.Ticks,
                Amount = amount,
                Category = category ?? "other",
                Description = description ?? "",
                PlayerId = playerId ?? ""
            });
            if (club.Finance.Transactions.Count > MaxTransactions)
                club.Finance.Transactions.RemoveAt(0);
        }

        public static List<Player> TopShirtSellers(CareerState career, Club club, int count = 3)
        {
            var result = new List<Player>();
            if (career == null || club == null || career.World == null || club.PlayerIds == null)
                return result;

            var contributions = new Dictionary<string, int>();
            foreach (var line in career.Season)
            {
                if (line == null || string.IsNullOrEmpty(line.PlayerId)) continue;
                contributions.TryGetValue(line.PlayerId, out var score);
                contributions[line.PlayerId] = score + line.Goals * 14 + line.Assists * 10 + line.Apps;
            }

            var candidates = new List<Player>();
            foreach (var id in club.PlayerIds)
                if (career.World.Players.TryGetValue(id, out var player))
                    candidates.Add(player);

            candidates.Sort((a, b) =>
            {
                var score = ShirtScore(b, contributions).CompareTo(ShirtScore(a, contributions));
                return score != 0 ? score : string.CompareOrdinal(a.Id, b.Id);
            });

            for (var i = 0; i < candidates.Count && i < Math.Max(0, count); i++)
                result.Add(candidates[i]);
            return result;
        }

        static int ShirtScore(Player player, Dictionary<string, int> contributions)
        {
            if (player == null) return int.MinValue;
            var position = player.Position;
            var attackingOrMidfield = position == PlayerPosition.Striker
                || position == PlayerPosition.LeftWinger || position == PlayerPosition.RightWinger
                || position == PlayerPosition.AttackingMidfielder || position == PlayerPosition.CentralMidfielder
                || position == PlayerPosition.DefensiveMidfielder || position == PlayerPosition.LeftMidfielder
                || position == PlayerPosition.RightMidfielder;
            contributions.TryGetValue(player.Id ?? "", out var production);
            return PlayerAttributes.Overall(player) * 4 + player.Form * 2 + player.Reputation
                + production * 3 + (attackingOrMidfield ? 80 : 0);
        }
    }
}
