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

        public static int OverallAtPosition(Player player, PlayerPosition position)
        {
            var attr = player == null ? null : player.Attributes;
            if (attr == null)
                return 50;
            if (position == PlayerPosition.Goalkeeper)
            {
                var keeper = (attr.Reflexes * 1.35f + attr.Handling * 1.25f + attr.Positioning * 1.1f + attr.Composure + attr.Decisions) / 5.7f;
                return UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(42f + keeper / 20f * 52f), 42, 94);
            }

            float weighted;
            float weight;
            switch (position)
            {
                case PlayerPosition.CentreBack:
                    weighted = attr.Strength * 1.3f + attr.Tackling * 1.4f + attr.Positioning * 1.35f + attr.Pace + attr.Passing * 0.8f + attr.Composure + attr.Decisions * 1.1f + attr.Stamina;
                    weight = 8.95f; break;
                case PlayerPosition.RightBack:
                case PlayerPosition.LeftBack:
                    weighted = attr.Pace * 1.2f + attr.Stamina * 1.1f + attr.Tackling * 1.1f + attr.Crossing * 1.25f + attr.Positioning + attr.Passing + attr.Dribbling * 0.8f + attr.Decisions;
                    weight = 8.45f; break;
                case PlayerPosition.DefensiveMidfielder:
                    weighted = attr.Tackling * 1.3f + attr.Passing * 1.15f + attr.Stamina * 1.2f + attr.Positioning * 1.1f + attr.Decisions * 1.25f + attr.Strength + attr.Composure;
                    weight = 8f; break;
                case PlayerPosition.CentralMidfielder:
                    weighted = attr.Passing * 1.3f + attr.Stamina * 1.1f + attr.Dribbling + attr.Decisions * 1.25f + attr.WorkRate + attr.Positioning + attr.Composure;
                    weight = 7.65f; break;
                case PlayerPosition.AttackingMidfielder:
                    weighted = attr.Passing * 1.25f + attr.Dribbling * 1.2f + attr.Finishing + attr.Positioning * 1.15f + attr.Composure * 1.2f + attr.Decisions * 1.2f + attr.Pace * 0.8f;
                    weight = 7.8f; break;
                case PlayerPosition.LeftMidfielder:
                case PlayerPosition.RightMidfielder:
                    weighted = attr.Pace + attr.Stamina * 1.2f + attr.Crossing * 1.1f + attr.Passing * 1.15f + attr.Positioning + attr.Decisions + attr.WorkRate * 1.1f;
                    weight = 7.55f; break;
                case PlayerPosition.RightWinger:
                case PlayerPosition.LeftWinger:
                    weighted = attr.Pace * 1.25f + attr.Dribbling * 1.25f + attr.Crossing * 1.15f + attr.Finishing + attr.Passing + attr.Decisions + attr.Stamina * 0.8f;
                    weight = 7.45f; break;
                case PlayerPosition.Striker:
                    weighted = attr.Finishing * 1.45f + attr.Positioning * 1.25f + attr.Composure * 1.2f + attr.Pace + attr.Strength + attr.Dribbling * 0.8f + attr.Decisions;
                    weight = 7.7f; break;
                default:
                    return Overall(player);
            }

            var baseline = UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(42f + weighted / weight / 20f * 52f), 42, 94);
            return baseline;
        }

        public static int CurrentOverall(Player player)
        {
            return CurrentOverallAtPosition(player, player == null ? PlayerPosition.CentralMidfielder : player.Position);
        }

        public static int CurrentOverallAtPosition(Player player, PlayerPosition position)
        {
            var baseline = OverallAtPosition(player, position);
            if (player == null)
                return baseline;

            var form = (player.Form - 60) * 0.07f;
            var morale = (player.Morale - 60) * 0.06f;
            var fitness = (player.Fitness - 80) * 0.035f;
            var fatigue = player.Fatigue * 0.03f;
            var training = ((player.TrainingRating <= 0 ? 70 : player.TrainingRating) - 70) * 0.05f;
            var change = UnityEngine.Mathf.Clamp(form + morale + fitness - fatigue + training, -4f, 4f);
            var positionPenalty = PlayerPositionProfile.Penalty(player, position);
            return UnityEngine.Mathf.Clamp(baseline + UnityEngine.Mathf.RoundToInt(change) + positionPenalty, 42, 94);
        }

        public static int CurrentValue(Player player, string key)
        {
            if (player == null || player.Attributes == null)
                return 10;

            var value = player.Attributes.Value(key);
            var training = ((player.TrainingRating <= 0 ? 70 : player.TrainingRating) - 70) / 30f;
            var form = (player.Form - 60) / 12f;
            var morale = (player.Morale - 60) / 12f;
            var physical = (player.Fitness - 80) / 30f - player.Fatigue / 40f;
            float change;
            switch (key)
            {
                case "Velocidade":
                case "Resistência":
                case "Força":
                    change = physical * 1.15f + training * 0.35f;
                    break;
                case "Posicionamento":
                case "Frieza":
                case "Decisão":
                case "Entrega":
                    change = form * 0.95f + morale * 0.8f + training * 0.35f;
                    break;
                default:
                    change = form * 0.85f + morale * 0.65f + training * 0.65f;
                    break;
            }

            return UnityEngine.Mathf.Clamp(value + UnityEngine.Mathf.RoundToInt(change), 1, 20);
        }

        public static PlayerAttributes Current(Player player)
        {
            if (player == null || player.Attributes == null)
                return null;
            return new PlayerAttributes
            {
                Pace = CurrentValue(player, "Velocidade"),
                Stamina = CurrentValue(player, "Resistência"),
                Strength = CurrentValue(player, "Força"),
                Passing = CurrentValue(player, "Passe"),
                Finishing = CurrentValue(player, "Finalização"),
                Dribbling = CurrentValue(player, "Drible"),
                Tackling = CurrentValue(player, "Desarme"),
                Crossing = CurrentValue(player, "Cruzamento"),
                Positioning = CurrentValue(player, "Posicionamento"),
                Composure = CurrentValue(player, "Frieza"),
                WorkRate = CurrentValue(player, "Entrega"),
                Decisions = CurrentValue(player, "Decisão"),
                Reflexes = CurrentValue(player, "Reflexos"),
                Handling = CurrentValue(player, "Manejo")
            };
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
