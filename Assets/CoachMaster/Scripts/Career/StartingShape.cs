using System.Collections.Generic;
using CoachMaster.Domain;

namespace CoachMaster.Career
{
    public static class StartingShape
    {
        public static void Split(IEnumerable<Player> squad, string formation, List<Player> eleven, List<Player> bench)
        {
            var pool = new List<Player>();
            foreach (var player in squad)
                pool.Add(player);
            pool.Sort((a, b) => a.SquadNumber.CompareTo(b.SquadNumber));
            PickFormation(pool, formation, eleven, bench);
        }

        public static void Resolve(IEnumerable<Player> squad, Club club, List<Player> eleven, List<Player> bench, bool ignoreManualLineup = false)
        {
            eleven.Clear();
            bench.Clear();
            var pool = new List<Player>();
            foreach (var player in squad)
                pool.Add(player);
            pool.Sort((a, b) => a.SquadNumber.CompareTo(b.SquadNumber));
            var formation = club == null || club.Tactics == null ? "4-3-3" : club.Tactics.Formation;
            if (!ignoreManualLineup && club != null && club.ManualLineup != null && club.ManualLineup.Count == 11)
            {
                foreach (var id in club.ManualLineup)
                {
                    Player found = null;
                    for (var i = 0; i < pool.Count; i++)
                    {
                        if (pool[i].Id != id)
                            continue;
                        found = pool[i];
                        pool.RemoveAt(i);
                        break;
                    }

                    if (found != null)
                        eleven.Add(found);
                }
            }

            if (eleven.Count != 11)
            {
                eleven.Clear();
                bench.Clear();
                pool.Clear();
                foreach (var player in squad)
                    pool.Add(player);
                pool.Sort((a, b) => a.SquadNumber.CompareTo(b.SquadNumber));
                PickFormation(pool, formation, eleven, bench);
                return;
            }

            bench.AddRange(pool);
        }

        public static void EnsureManualLineup(Club club, IEnumerable<Player> squad)
        {
            if (club == null)
                return;
            if (club.ManualLineup != null && club.ManualLineup.Count == 11)
                return;
            var eleven = new List<Player>();
            var bench = new List<Player>();
            Split(squad, club.Tactics.Formation, eleven, bench);
            club.ManualLineup = new List<string>();
            foreach (var player in eleven)
                club.ManualLineup.Add(player.Id);
        }

        public static void SwapWithBench(Club club, IEnumerable<Player> squad, int fieldIndex, Player benchPlayer)
        {
            if (club == null || benchPlayer == null || fieldIndex < 0 || fieldIndex >= 11)
                return;
            EnsureManualLineup(club, squad);
            var eleven = new List<Player>();
            var bench = new List<Player>();
            Resolve(squad, club, eleven, bench);
            if (fieldIndex >= eleven.Count)
                return;
            var benchIndex = -1;
            for (var i = 0; i < bench.Count; i++)
            {
                if (bench[i].Id != benchPlayer.Id)
                    continue;
                benchIndex = i;
                break;
            }

            if (benchIndex < 0)
                return;
            var starter = eleven[fieldIndex];
            var armband = starter.IsCaptain;
            eleven[fieldIndex] = benchPlayer;
            bench[benchIndex] = starter;
            if (armband)
            {
                starter.IsCaptain = false;
                benchPlayer.IsCaptain = true;
            }
            club.ManualLineup.Clear();
            foreach (var player in eleven)
                club.ManualLineup.Add(player.Id);
        }

        public static void SwapPositions(Club club, IEnumerable<Player> squad, int fromIndex, int toIndex)
        {
            if (club == null || fromIndex < 0 || toIndex < 0 || fromIndex >= 11 || toIndex >= 11 || fromIndex == toIndex)
                return;
            EnsureManualLineup(club, squad);
            var eleven = new List<Player>();
            var bench = new List<Player>();
            Resolve(squad, club, eleven, bench);
            if (fromIndex >= eleven.Count || toIndex >= eleven.Count)
                return;
            var moving = eleven[fromIndex];
            eleven[fromIndex] = eleven[toIndex];
            eleven[toIndex] = moving;
            club.ManualLineup.Clear();
            foreach (var player in eleven)
                club.ManualLineup.Add(player.Id);
        }

        static void PickFormation(List<Player> pool, string formation, List<Player> eleven, List<Player> bench)
        {
            foreach (var prefs in Pattern(formation))
            {
                var picked = Take(pool, prefs);
                if (picked != null)
                    eleven.Add(picked);
            }

            while (eleven.Count < 11 && pool.Count > 0)
            {
                eleven.Add(pool[0]);
                pool.RemoveAt(0);
            }

            bench.AddRange(pool);
        }

        public static int[] Lines(string formation)
        {
            var counts = new List<int> { 1 };
            if (!string.IsNullOrEmpty(formation))
            {
                foreach (var bit in formation.Split('-'))
                {
                    if (int.TryParse(bit, out var count) && count > 0)
                        counts.Add(count);
                }
            }

            var total = 0;
            foreach (var count in counts)
                total += count;
            if (total != 11)
                return new[] { 1, 4, 3, 3 };
            return counts.ToArray();
        }

        public static PlayerPosition[] SlotPositions(string formation)
        {
            var pattern = Pattern(formation);
            var result = new PlayerPosition[pattern.Length];
            for (var i = 0; i < pattern.Length; i++)
                result[i] = pattern[i] != null && pattern[i].Length > 0 ? pattern[i][0] : PlayerPosition.CentralMidfielder;
            return result;
        }

        static Player Take(List<Player> pool, PlayerPosition[] prefs)
        {
            foreach (var position in prefs)
            {
                for (var i = 0; i < pool.Count; i++)
                {
                    if (pool[i].Position != position)
                        continue;
                    var player = pool[i];
                    pool.RemoveAt(i);
                    return player;
                }
            }

            Player alternative = null;
            var alternativeScore = int.MinValue;
            foreach (var position in prefs)
            {
                for (var i = 0; i < pool.Count; i++)
                {
                    var player = pool[i];
                    PlayerPositionProfile.Ensure(player);
                    if (player.AlternatePositions == null || !player.AlternatePositions.Contains(position))
                        continue;
                    var score = PlayerAttributes.CurrentOverallAtPosition(player, position);
                    if (score <= alternativeScore)
                        continue;
                    alternative = player;
                    alternativeScore = score;
                }
            }

            if (alternative != null)
            {
                pool.Remove(alternative);
                return alternative;
            }

            if (pool.Count == 0)
                return null;
            var any = pool[0];
            pool.RemoveAt(0);
            return any;
        }

        static PlayerPosition[][] Pattern(string formation)
        {
            var gk = new[] { PlayerPosition.Goalkeeper };
            var rb = new[] { PlayerPosition.RightBack, PlayerPosition.CentreBack, PlayerPosition.LeftBack };
            var cb = new[] { PlayerPosition.CentreBack, PlayerPosition.RightBack, PlayerPosition.LeftBack };
            var lb = new[] { PlayerPosition.LeftBack, PlayerPosition.CentreBack, PlayerPosition.RightBack };
            var dm = new[] { PlayerPosition.DefensiveMidfielder, PlayerPosition.CentralMidfielder, PlayerPosition.AttackingMidfielder };
            var cm = new[] { PlayerPosition.CentralMidfielder, PlayerPosition.DefensiveMidfielder, PlayerPosition.AttackingMidfielder };
            var am = new[] { PlayerPosition.AttackingMidfielder, PlayerPosition.CentralMidfielder, PlayerPosition.RightWinger };
            var rw = new[] { PlayerPosition.RightWinger, PlayerPosition.Striker, PlayerPosition.AttackingMidfielder };
            var lw = new[] { PlayerPosition.LeftWinger, PlayerPosition.Striker, PlayerPosition.AttackingMidfielder };
            var st = new[] { PlayerPosition.Striker, PlayerPosition.AttackingMidfielder, PlayerPosition.RightWinger };

            switch (formation)
            {
                case "4-4-2":
                    return new[] { gk, rb, cb, cb, lb, rw, cm, cm, lw, st, st };
                case "4-2-3-1":
                    return new[] { gk, rb, cb, cb, lb, dm, dm, rw, am, lw, st };
                case "3-4-3":
                    return new[] { gk, cb, cb, cb, rb, cm, cm, lb, rw, st, lw };
                case "5-4-1":
                    return new[] { gk, rb, cb, cb, cb, lb, rw, cm, cm, lw, st };
                default:
                    return new[] { gk, rb, cb, cb, lb, cm, cm, cm, rw, st, lw };
            }
        }
    }
}
