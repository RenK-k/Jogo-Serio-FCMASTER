using System.Collections.Generic;

namespace CoachMaster.Domain
{
    public static class PlayerPositionProfile
    {
        public static void Assign(Player player, int seed)
        {
            if (player == null)
                return;
            if (player.AlternatePositions == null)
                player.AlternatePositions = new List<PlayerPosition>();
            player.AlternatePositions.Clear();

            var candidates = Candidates(player.Position);
            if (candidates == null || candidates.Length == 0)
                return;

            var ranked = new List<RankedPosition>();
            for (var i = 0; i < candidates.Length; i++)
            {
                var role = candidates[i];
                var score = RoleScore(player, role);
                score += (Mix(seed, (int)role) % 100) * 0.0001f;
                ranked.Add(new RankedPosition(role, score));
            }
            ranked.Sort((a, b) => b.Score.CompareTo(a.Score));
            for (var i = 0; i < ranked.Count && i < 2; i++)
                player.AlternatePositions.Add(ranked[i].Position);
        }

        public static void Ensure(Player player)
        {
            if (player == null)
                return;
            if (player.AlternatePositions == null || player.AlternatePositions.Count == 0)
                Assign(player, StableSeed(player.Id));
        }

        public static int Penalty(Player player, PlayerPosition deployed)
        {
            if (player == null || player.Position == deployed)
                return 0;
            Ensure(player);
            if (player.AlternatePositions.Contains(deployed))
                return 0;
            if (player.Position == PlayerPosition.Goalkeeper || deployed == PlayerPosition.Goalkeeper)
                return -24;
            if (IsAdjacent(player.Position, deployed))
                return -4;
            return -8;
        }

        public static string Encode(Player player)
        {
            Ensure(player);
            var values = new List<string>();
            for (var i = 0; i < player.AlternatePositions.Count; i++)
                values.Add(player.AlternatePositions[i].ToString());
            return string.Join(",", values.ToArray());
        }

        public static void Decode(Player player, string encoded)
        {
            if (player == null)
                return;
            if (player.AlternatePositions == null)
                player.AlternatePositions = new List<PlayerPosition>();
            player.AlternatePositions.Clear();
            if (!string.IsNullOrEmpty(encoded))
            {
                var values = encoded.Split(',');
                for (var i = 0; i < values.Length; i++)
                {
                    if (System.Enum.TryParse(values[i], out PlayerPosition position)
                        && System.Enum.IsDefined(typeof(PlayerPosition), position)
                        && position != player.Position
                        && !player.AlternatePositions.Contains(position))
                        player.AlternatePositions.Add(position);
                }
            }
            Ensure(player);
        }

        static PlayerPosition[] Candidates(PlayerPosition position)
        {
            switch (position)
            {
                case PlayerPosition.RightBack: return new[] { PlayerPosition.CentreBack, PlayerPosition.LeftBack, PlayerPosition.RightWinger, PlayerPosition.DefensiveMidfielder };
                case PlayerPosition.CentreBack: return new[] { PlayerPosition.RightBack, PlayerPosition.LeftBack, PlayerPosition.DefensiveMidfielder };
                case PlayerPosition.LeftBack: return new[] { PlayerPosition.CentreBack, PlayerPosition.RightBack, PlayerPosition.LeftWinger, PlayerPosition.DefensiveMidfielder };
                case PlayerPosition.DefensiveMidfielder: return new[] { PlayerPosition.CentralMidfielder, PlayerPosition.CentreBack, PlayerPosition.AttackingMidfielder };
                case PlayerPosition.CentralMidfielder: return new[] { PlayerPosition.DefensiveMidfielder, PlayerPosition.AttackingMidfielder, PlayerPosition.RightMidfielder, PlayerPosition.LeftMidfielder };
                case PlayerPosition.AttackingMidfielder: return new[] { PlayerPosition.CentralMidfielder, PlayerPosition.Striker, PlayerPosition.RightWinger, PlayerPosition.LeftWinger };
                case PlayerPosition.RightWinger: return new[] { PlayerPosition.RightMidfielder, PlayerPosition.LeftWinger, PlayerPosition.AttackingMidfielder, PlayerPosition.Striker };
                case PlayerPosition.LeftWinger: return new[] { PlayerPosition.LeftMidfielder, PlayerPosition.RightWinger, PlayerPosition.AttackingMidfielder, PlayerPosition.Striker };
                case PlayerPosition.RightMidfielder: return new[] { PlayerPosition.RightWinger, PlayerPosition.CentralMidfielder, PlayerPosition.DefensiveMidfielder, PlayerPosition.AttackingMidfielder };
                case PlayerPosition.LeftMidfielder: return new[] { PlayerPosition.LeftWinger, PlayerPosition.CentralMidfielder, PlayerPosition.DefensiveMidfielder, PlayerPosition.AttackingMidfielder };
                case PlayerPosition.Striker: return new[] { PlayerPosition.AttackingMidfielder, PlayerPosition.RightWinger, PlayerPosition.LeftWinger };
                default: return new PlayerPosition[0];
            }
        }

        static float RoleScore(Player player, PlayerPosition role)
        {
            var a = player.Attributes;
            if (a == null)
                return 0f;
            switch (role)
            {
                case PlayerPosition.Goalkeeper: return (a.Reflexes + a.Handling + a.Positioning) / 3f;
                case PlayerPosition.RightBack:
                case PlayerPosition.LeftBack: return (a.Pace + a.Stamina + a.Tackling + a.Crossing + a.Positioning) / 5f;
                case PlayerPosition.CentreBack: return (a.Strength + a.Tackling + a.Positioning + a.Composure + a.Decisions) / 5f;
                case PlayerPosition.DefensiveMidfielder: return (a.Tackling + a.Passing + a.Stamina + a.Positioning + a.Decisions) / 5f;
                case PlayerPosition.CentralMidfielder: return (a.Passing + a.Stamina + a.Dribbling + a.Decisions + a.WorkRate) / 5f;
                case PlayerPosition.RightMidfielder:
                case PlayerPosition.LeftMidfielder: return (a.Passing + a.Stamina + a.Crossing + a.Positioning + a.WorkRate) / 5f;
                case PlayerPosition.AttackingMidfielder: return (a.Passing + a.Dribbling + a.Finishing + a.Composure + a.Decisions) / 5f;
                case PlayerPosition.RightWinger:
                case PlayerPosition.LeftWinger: return (a.Pace + a.Dribbling + a.Crossing + a.Finishing + a.Passing) / 5f;
                case PlayerPosition.Striker: return (a.Finishing + a.Positioning + a.Composure + a.Pace + a.Strength) / 5f;
                default: return 0f;
            }
        }

        static bool IsAdjacent(PlayerPosition source, PlayerPosition target)
        {
            if (source == target)
                return true;
            var candidates = Candidates(source);
            for (var i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] == target)
                    return true;
            }
            return false;
        }

        static int StableSeed(string value)
        {
            unchecked
            {
                var hash = 17;
                if (value != null)
                    for (var i = 0; i < value.Length; i++) hash = hash * 31 + value[i];
                return hash & 0x7fffffff;
            }
        }

        static int Mix(int seed, int value)
        {
            unchecked { return (seed * 397) ^ (value * 7919); }
        }

        struct RankedPosition
        {
            public readonly PlayerPosition Position;
            public readonly float Score;
            public RankedPosition(PlayerPosition position, float score) { Position = position; Score = score; }
        }
    }
}
