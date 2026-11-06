using System;
using System.Collections.Generic;
using CoachMaster.App;
using CoachMaster.Domain;

namespace CoachMaster.Career
{
    public static class PlayerAvailability
    {
        public static bool IsOut(Player player, CareerState career = null)
        {
            return Reason(player, career) != "";
        }

        public static string Reason(Player player, CareerState career = null)
        {
            if (player == null)
                return "";
            if (player.Injury != null && player.Injury.IsInjured)
                return "injury";
            career = career ?? Live();
            var today = career != null && career.Calendar != null
                ? career.Calendar.CurrentDate.Date.Ticks
                : DateTime.Today.Ticks;
            if (player.UnavailableUntilTicks < today)
                return "";
            return string.IsNullOrEmpty(player.AbsenceKind) ? "away" : player.AbsenceKind;
        }

        static CareerState Live()
        {
            var root = App.GameRoot.Instance;
            return root == null || root.Session == null ? null : root.Session.Career;
        }

        public static string IconResource(string reason)
        {
            if (reason == "injury")
                return "cruz vermelha";
            if (reason == "ban")
                return "cVermelho";
            if (reason == "away" || reason == "selecao" || reason == "ferias" || reason == "pessoal")
                return "ferias";
            return "";
        }

        public static void Cover(CareerState career, Club club)
        {
            if (career == null || career.World == null || club == null || club.ManualLineup == null)
                return;
            var formation = club.Tactics == null ? "4-3-3" : club.Tactics.Formation;
            var slots = StartingShape.SlotPositions(formation);
            var used = new HashSet<string>();
            for (var i = 0; i < club.ManualLineup.Count; i++)
                used.Add(club.ManualLineup[i]);
            if (club.ManualBenchOrder != null)
            {
                for (var i = club.ManualBenchOrder.Count - 1; i >= 0; i--)
                {
                    if (!career.World.Players.TryGetValue(club.ManualBenchOrder[i], out var benched) || IsOut(benched, career))
                        club.ManualBenchOrder.RemoveAt(i);
                    else
                        used.Add(club.ManualBenchOrder[i]);
                }
            }

            if (club.ManualLineup.Count != 11)
                return;
            for (var i = 0; i < club.ManualLineup.Count; i++)
            {
                if (!career.World.Players.TryGetValue(club.ManualLineup[i], out var current) || !IsOut(current, career))
                    continue;
                var slot = i < slots.Length ? slots[i] : current.Position;
                var replacement = BestReserve(career, club, slot, used);
                if (replacement == null)
                    continue;
                if (club.ManualBenchOrder != null)
                    club.ManualBenchOrder.Remove(replacement.Id);
                used.Remove(club.ManualLineup[i]);
                club.ManualLineup[i] = replacement.Id;
                used.Add(replacement.Id);
            }
        }

        public static void NoteCards(CareerState career, CoachMaster.MatchPlay.MatchSession match)
        {
            if (career == null || match == null || match.Fixture == null)
                return;
            var competition = match.Fixture.LeagueId ?? "";
            var limit = !string.IsNullOrEmpty(competition) && competition.StartsWith("cup:") ? 3 : 5;
            foreach (var pair in match.Bookings)
            {
                if (!career.World.Players.TryGetValue(pair.Key, out var player) || player == null)
                    continue;
                var until = NextFixture(career, player.ClubId, match.Fixture.Date);
                if (pair.Value >= 2 || match.SentOffPlayers.Contains(pair.Key))
                {
                    Suspend(player, until);
                    continue;
                }

                var yellows = Yellows(player, competition) + 1;
                if (yellows >= limit)
                {
                    SetYellows(player, competition, 0);
                    Suspend(player, until);
                }
                else
                    SetYellows(player, competition, yellows);
            }
        }

        static void Suspend(Player player, long until)
        {
            if (player.UnavailableUntilTicks < until)
                player.UnavailableUntilTicks = until;
            player.AbsenceKind = "ban";
        }

        static Player BestReserve(CareerState career, Club club, PlayerPosition slot, HashSet<string> used)
        {
            Player best = null;
            var bestScore = int.MinValue;
            foreach (var id in club.PlayerIds)
            {
                if (used.Contains(id) || !career.World.Players.TryGetValue(id, out var player) || IsOut(player, career))
                    continue;
                var score = Fit(player, slot);
                if (score <= bestScore)
                    continue;
                bestScore = score;
                best = player;
            }
            return best;
        }

        static int Fit(Player player, PlayerPosition slot)
        {
            PlayerPositionProfile.Ensure(player);
            var overall = PlayerAttributes.CurrentOverallAtPosition(player, slot);
            if (player.Position == slot)
                return 1000 + overall;
            if (player.AlternatePositions != null && player.AlternatePositions.Contains(slot))
                return 500 + overall;
            return overall;
        }

        static long NextFixture(CareerState career, string clubId, DateTime after)
        {
            Fixture next = null;
            foreach (var fixture in career.Fixtures)
            {
                if (fixture == null || fixture.Played || !fixture.Involves(clubId) || fixture.Date.Date <= after.Date)
                    continue;
                if (next == null || fixture.Date < next.Date)
                    next = fixture;
            }
            return next == null ? after.Date.AddDays(7).Ticks : next.Date.Date.Ticks;
        }

        static int Yellows(Player player, string competition)
        {
            if (string.IsNullOrEmpty(player.YellowBook) || string.IsNullOrEmpty(competition))
                return 0;
            var rows = player.YellowBook.Split(';');
            for (var i = 0; i < rows.Length; i++)
            {
                var cut = rows[i].IndexOf('=');
                if (cut <= 0 || rows[i].Substring(0, cut) != competition)
                    continue;
                return int.TryParse(rows[i].Substring(cut + 1), out var count) ? count : 0;
            }
            return 0;
        }

        static void SetYellows(Player player, string competition, int count)
        {
            var rows = new List<string>();
            if (!string.IsNullOrEmpty(player.YellowBook))
            {
                var existing = player.YellowBook.Split(';');
                for (var i = 0; i < existing.Length; i++)
                {
                    var cut = existing[i].IndexOf('=');
                    if (cut <= 0 || existing[i].Substring(0, cut) == competition)
                        continue;
                    rows.Add(existing[i]);
                }
            }
            if (count > 0 && !string.IsNullOrEmpty(competition))
                rows.Add(competition + "=" + count);
            player.YellowBook = string.Join(";", rows);
        }
    }
}
