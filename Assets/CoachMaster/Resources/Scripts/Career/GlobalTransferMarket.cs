using System;
using System.Collections.Generic;
using CoachMaster.Domain;
using UnityEngine;

namespace CoachMaster.Career
{
    public static class GlobalTransferMarket
    {
        public static List<Club> Contenders(CareerState career, Player player, bool includeUser = true)
        {
            var result = new List<Club>();
            if (career == null || player == null || career.World == null)
                return result;
            var baseValue = MarketValue(player);
            if (!career.World.Clubs.TryGetValue(player.ClubId ?? "", out var seller))
                return result;
            var scored = new List<KeyValuePair<int, Club>>();
            foreach (var club in career.World.Clubs.Values)
            {
                if (club == null || club.Id == player.ClubId || (!includeUser && club.Id == career.Club.Id)
                    || club.Finance == null || club.Finance.TransferBudget < baseValue || club.PlayerIds.Count >= 28)
                    continue;
                var sameLeague = club.LeagueId == seller.LeagueId;
                var sameCountryTier = sameLeague ? 16 : 0;
                var positionalNeed = PositionNeed(career, club, player.Position) * 5;
                var score = 100 - Math.Abs(club.Reputation - PlayerAttributes.Overall(player)) + sameCountryTier + positionalNeed;
                score += StableHash(club.Id + player.Id + career.Calendar.CurrentDate.DayOfYear) % 11;
                scored.Add(new KeyValuePair<int, Club>(score, club));
            }
            scored.Sort((a, b) => b.Key.CompareTo(a.Key));
            for (var i = 0; i < Math.Min(3, scored.Count); i++)
                result.Add(scored[i].Value);
            return result;
        }

        public static int RequiredFee(CareerState career, Player player)
        {
            var value = MarketValue(player);
            var contenders = Contenders(career, player);
            var premium = Mathf.Min(0.2f, contenders.Count * 0.05f);
            return Mathf.Max(value, Mathf.RoundToInt(value * (1f + premium)));
        }

        public static bool Advance(CareerState career)
        {
            if (career == null || career.Calendar == null || career.World == null)
                return false;
            var date = career.Calendar.CurrentDate.Date;
            if (career.LastGlobalTransferTicks > 0 && (date - new DateTime(career.LastGlobalTransferTicks).Date).Days < 7)
                return false;
            if (!TransferWindow(date))
                return false;

            career.LastGlobalTransferTicks = date.Ticks;
            var random = new System.Random(StableHash(date.Year + ":" + date.DayOfYear + ":transfer-window"));
            if (random.Next(100) >= 34)
                return true;

            var candidates = new List<Player>();
            foreach (var club in career.World.Clubs.Values)
            {
                if (club == null || club.Id == career.Club.Id || club.PlayerIds == null || club.PlayerIds.Count < 15)
                    continue;
                foreach (var id in club.PlayerIds)
                {
                    if (!career.World.Players.TryGetValue(id, out var player) || player.Age < 18 || player.Age > 32
                        || player.Id.StartsWith("academy:", StringComparison.Ordinal) || player.IsCaptain)
                        continue;
                    candidates.Add(player);
                }
            }
            candidates.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            var completed = 0;
            for (var attempt = 0; attempt < 48 && candidates.Count > 0 && completed < 3; attempt++)
            {
                var candidateIndex = random.Next(candidates.Count);
                var player = candidates[candidateIndex];
                candidates.RemoveAt(candidateIndex);
                if (!career.World.Clubs.TryGetValue(player.ClubId ?? "", out var seller))
                    continue;
                var buyers = Contenders(career, player, false);
                if (buyers.Count == 0)
                    continue;
                var buyer = buyers[random.Next(Math.Min(3, buyers.Count))];
                var fee = Mathf.Max(80000, Mathf.RoundToInt(MarketValue(player) * (1f + Math.Min(0.16f, buyers.Count * 0.04f))));
                if (buyer.Finance == null || seller.Finance == null || buyer.Finance.TransferBudget < fee || seller.PlayerIds.Count <= 14)
                    continue;
                if (ScheduleMove(career, player, buyer, fee, 3, 0, "ai", 3 + random.Next(6)))
                    completed++;
            }
            return true;
        }

        public static bool MovePending(CareerState career, string playerId)
        {
            if (career == null || string.IsNullOrEmpty(playerId))
                return false;
            var prefix = "job|move|" + playerId + "|";
            foreach (var line in career.Notebook)
                if (line != null && line.StartsWith(prefix, StringComparison.Ordinal))
                    return true;
            return false;
        }

        public static bool ScheduleMove(CareerState career, Player player, Club buyer, int fee, int years, int wage, string type, int days)
        {
            if (career == null || player == null || buyer == null || career.Calendar == null || career.World == null)
                return false;
            if (MovePending(career, player.Id) || player.ClubId == buyer.Id)
                return false;
            if (!career.World.Clubs.TryGetValue(player.ClubId ?? "", out var seller) || seller == null)
                return false;
            if (!seller.PlayerIds.Contains(player.Id) || seller.PlayerIds.Count <= 11)
                return false;
            if (buyer.Finance == null || buyer.Finance.TransferBudget < fee)
                return false;
            days = Mathf.Clamp(days, 2, 21);
            var due = career.Calendar.CurrentDate.Date.AddDays(days);
            career.Notebook.Add("job|move|" + player.Id + "|" + buyer.Id + "|" + fee + "|" + Mathf.Clamp(years, 1, 5) + "|" + Math.Max(0, wage) + "|" + (string.IsNullOrEmpty(type) ? "ai" : type) + "|" + due.Ticks);
            return true;
        }

        public static bool SettleMoves(CareerState career)
        {
            if (career == null || career.Calendar == null || career.World == null)
                return false;
            var now = career.Calendar.CurrentDate.Date;
            var changed = false;
            for (var i = career.Notebook.Count - 1; i >= 0; i--)
            {
                var line = career.Notebook[i];
                if (line == null || !line.StartsWith("job|move|", StringComparison.Ordinal))
                    continue;
                var bits = line.Split('|');
                if (bits.Length < 9 || !long.TryParse(bits[8], out var due) || new DateTime(due).Date > now)
                    continue;
                career.Notebook.RemoveAt(i);
                changed = true;
                if (!career.World.Players.TryGetValue(bits[2], out var player)
                    || !career.World.Clubs.TryGetValue(player.ClubId ?? "", out var seller)
                    || !career.World.Clubs.TryGetValue(bits[3], out var buyer))
                {
                    Desk(career, "✉ Uma transferência marcada não se concretizou.");
                    continue;
                }
                int.TryParse(bits[4], out var fee);
                int.TryParse(bits[5], out var years);
                int.TryParse(bits[6], out var wage);
                var record = CompleteDeal(career, player, seller, buyer, fee, years, bits[7]);
                if (record == null)
                {
                    Desk(career, "✉ A transferência de " + player.FullName + " caiu. O plantel e o orçamento mantêm-se.");
                    continue;
                }
                if (wage > 0)
                {
                    player.WeeklyWage = wage;
                    record.WeeklyWage = wage;
                }
                Newsroom.PublishTransferDeal(career, record);
                if (career.Coach != null && (seller.Id == career.Coach.ClubId || buyer.Id == career.Coach.ClubId))
                    Desk(career, "✉ " + player.FullName + " oficializou a mudança para o " + buyer.Name + ".");
            }
            return changed;
        }

        static void Desk(CareerState career, string text)
        {
            career.Board.Insert(0, new CoachNote
            {
                Day = career.Calendar.WeekdayName,
                Hour = career.Calendar.ClockText,
                When = career.Calendar.CurrentDate,
                Text = text
            });
            while (career.Board.Count > 40)
                career.Board.RemoveAt(career.Board.Count - 1);
        }

        public static TransferRecord CompleteDeal(CareerState career, Player player, Club seller, Club buyer, int fee, int contractYears, string type)
        {
            if (career == null || player == null || seller == null || buyer == null || seller.Id == buyer.Id)
                return null;
            if (!seller.PlayerIds.Contains(player.Id) || buyer.Finance == null || seller.Finance == null
                || buyer.Finance.TransferBudget < fee || seller.PlayerIds.Count <= 11)
                return null;

            var oldWage = Math.Max(0, player.WeeklyWage);
            var newWage = Math.Max(oldWage, Mathf.RoundToInt(oldWage * (1f + Mathf.Clamp(buyer.Reputation - seller.Reputation, -20, 40) / 100f)));
            seller.PlayerIds.Remove(player.Id);
            seller.ManualLineup.Remove(player.Id);
            seller.ManualBenchOrder.Remove(player.Id);
            if (seller.PenaltyTakerId == player.Id) seller.PenaltyTakerId = null;
            if (seller.CornerTakerId == player.Id) seller.CornerTakerId = null;
            if (seller.FreeKickTakerId == player.Id) seller.FreeKickTakerId = null;
            buyer.PlayerIds.Add(player.Id);
            player.ClubId = buyer.Id;
            player.SquadNumber = NextSquadNumber(career, buyer);
            player.IsCaptain = false;
            player.IsViceCaptain = false;
            player.WeeklyWage = newWage;
            player.ContractEndTicks = career.Calendar.CurrentDate.Date.AddYears(Mathf.Clamp(contractYears, 1, 5)).Ticks;
            seller.Finance.Balance += fee;
            seller.Finance.TransferBudget += fee;
            seller.Finance.PlayerSalesThisSeason += fee;
            buyer.Finance.Balance -= fee;
            buyer.Finance.TransferBudget -= fee;
            buyer.Finance.TransferSpentThisSeason += fee;
            ClubFinanceLedger.Record(seller, career.Calendar.CurrentDate, fee, "player-sale",
                player.FullName, player.Id);
            ClubFinanceLedger.Record(buyer, career.Calendar.CurrentDate, -fee, "transfer-fee",
                player.FullName, player.Id);

            var record = new TransferRecord
            {
                DateTicks = career.Calendar.CurrentDate.Ticks,
                PlayerId = player.Id,
                FromClubId = seller.Id,
                ToClubId = buyer.Id,
                Fee = fee,
                WeeklyWage = player.WeeklyWage,
                ContractYears = Mathf.Clamp(contractYears, 1, 5),
                AgeAtTransfer = player.Age,
                OverallAtTransfer = PlayerAttributes.Overall(player),
                Type = type ?? "Transfer"
            };
            career.TransferHistory.Insert(0, record);
            if (career.TransferHistory.Count > 300)
                career.TransferHistory.RemoveAt(career.TransferHistory.Count - 1);
            return record;
        }

        public static int MarketValue(Player player)
        {
            var overall = PlayerAttributes.Overall(player);
            var baseValue = 1100000.0 * Math.Exp((overall - 60) * 0.105);
            var ageFactor = player.Age <= 21 ? 1.45 : player.Age <= 25 ? 1.2 : player.Age <= 29 ? 1.0 : player.Age <= 32 ? 0.7 : 0.42;
            var potentialFactor = player.Age <= 23 ? 1.0 + Math.Max(0, player.Potential - overall) * 0.025 : 1.0;
            return Mathf.Clamp(Mathf.RoundToInt((float)(baseValue * ageFactor * potentialFactor)), 80000, 850000000);
        }

        static int PositionNeed(CareerState career, Club club, PlayerPosition position)
        {
            var count = 0;
            foreach (var id in club.PlayerIds)
                if (career.World.Players.TryGetValue(id, out var player) && player.Position == position)
                    count++;
            return Mathf.Clamp(4 - count, 0, 4);
        }

        static bool TransferWindow(DateTime date) => date.Month == 1 || date.Month == 7 || date.Month == 8;

        static int NextSquadNumber(CareerState career, Club club)
        {
            var used = new HashSet<int>();
            foreach (var id in club.PlayerIds)
                if (career.World.Players.TryGetValue(id, out var player) && player.SquadNumber > 0)
                    used.Add(player.SquadNumber);
            for (var number = 1; number <= 99; number++)
                if (!used.Contains(number)) return number;
            return 99;
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
