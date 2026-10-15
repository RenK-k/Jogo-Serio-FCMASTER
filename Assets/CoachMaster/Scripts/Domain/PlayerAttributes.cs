namespace CoachMaster.Domain
{
    public sealed class PlayerAttributes
    {
        public int Pace;
        public int Stamina;
        public int Strength;
        public int Passing;
        public int Finishing;
        public int Dribbling;
        public int Tackling;
        public int Crossing;
        public int Positioning;
        public int Composure;
        public int WorkRate;
        public int Decisions;
        public int Reflexes;
        public int Handling;

        public static PlayerAttributes Roll(int seed, PlayerPosition position, int clubReputation = 55)
        {
            var tier = clubReputation >= 74 ? 3 : clubReputation >= 62 ? 2 : clubReputation >= 48 ? 1 : 0;
            var lift = tier * 2;
            var attr = new PlayerAttributes
            {
                Pace = Band(seed, 8 + lift, 16 + tier),
                Stamina = Band(seed >> 2, 8 + lift, 17 + tier),
                Strength = Band(seed >> 4, 7 + lift, 16 + tier),
                Passing = Band(seed >> 6, 7 + lift, 16 + tier),
                Finishing = Band(seed >> 8, 6 + lift, 15 + tier),
                Dribbling = Band(seed >> 10, 6 + lift, 16 + tier),
                Tackling = Band(seed >> 12, 6 + lift, 16 + tier),
                Crossing = Band(seed >> 14, 6 + lift, 15 + tier),
                Positioning = Band(seed >> 16, 8 + lift, 16 + tier),
                Composure = Band(seed >> 18, 7 + lift, 16 + tier),
                WorkRate = Band(seed >> 20, 8 + lift, 17 + tier),
                Decisions = Band(seed >> 22, 7 + lift, 16 + tier),
                Reflexes = Band(seed >> 1, 4 + lift / 2, 12 + tier),
                Handling = Band(seed >> 3, 4 + lift / 2, 12 + tier)
            };

            if (position == PlayerPosition.Goalkeeper)
            {
                attr.Reflexes = Band(seed, 11, 18);
                attr.Handling = Band(seed >> 5, 10, 18);
                attr.Finishing = Band(seed >> 8, 2, 7);
                attr.Tackling = Band(seed >> 9, 3, 8);
            }
            else if (position == PlayerPosition.Striker || position == PlayerPosition.LeftWinger || position == PlayerPosition.RightWinger)
            {
                attr.Finishing += 2;
                attr.Pace += 1;
                attr.Tackling -= 2;
            }
            else if (position == PlayerPosition.CentreBack || position == PlayerPosition.LeftBack || position == PlayerPosition.RightBack)
            {
                attr.Tackling += 2;
                attr.Strength += 1;
                attr.Finishing -= 2;
            }

            ClampAll(attr);
            return attr;
        }

        public static int Overall(Player player)
        {
            var attr = player == null ? null : player.Attributes;
            if (attr == null)
                return 50;
            float mean;
            if (player.Position == PlayerPosition.Goalkeeper)
                mean = (attr.Reflexes + attr.Handling + attr.Positioning + attr.Composure + attr.Decisions) / 5f;
            else
                mean = (attr.Pace + attr.Stamina + attr.Strength + attr.Passing + attr.Finishing + attr.Dribbling + attr.Tackling + attr.Crossing + attr.Positioning + attr.Composure + attr.WorkRate + attr.Decisions) / 12f;
            var rating = 42f + mean / 20f * 52f;
            if (rating < 42) return 42;
            if (rating > 94) return 94;
            return (int)System.Math.Round(rating);
        }

        public int Value(string key)
        {
            switch (key)
            {
                case "Velocidade": return Pace;
                case "Resistência": return Stamina;
                case "Força": return Strength;
                case "Passe": return Passing;
                case "Finalização": return Finishing;
                case "Drible": return Dribbling;
                case "Desarme": return Tackling;
                case "Cruzamento": return Crossing;
                case "Posicionamento": return Positioning;
                case "Frieza": return Composure;
                case "Entrega": return WorkRate;
                case "Decisão": return Decisions;
                case "Reflexos": return Reflexes;
                case "Manejo": return Handling;
                default: return 10;
            }
        }

        static int Band(int seed, int min, int max)
        {
            var span = max - min + 1;
            var value = min + (seed & 0x7fffffff) % span;
            return value;
        }

        static void ClampAll(PlayerAttributes attr)
        {
            attr.Pace = Clamp(attr.Pace);
            attr.Stamina = Clamp(attr.Stamina);
            attr.Strength = Clamp(attr.Strength);
            attr.Passing = Clamp(attr.Passing);
            attr.Finishing = Clamp(attr.Finishing);
            attr.Dribbling = Clamp(attr.Dribbling);
            attr.Tackling = Clamp(attr.Tackling);
            attr.Crossing = Clamp(attr.Crossing);
            attr.Positioning = Clamp(attr.Positioning);
            attr.Composure = Clamp(attr.Composure);
            attr.WorkRate = Clamp(attr.WorkRate);
            attr.Decisions = Clamp(attr.Decisions);
            attr.Reflexes = Clamp(attr.Reflexes);
            attr.Handling = Clamp(attr.Handling);
        }

        static int Clamp(int value)
        {
            if (value < 1) return 1;
            if (value > 20) return 20;
            return value;
        }
    }
}
