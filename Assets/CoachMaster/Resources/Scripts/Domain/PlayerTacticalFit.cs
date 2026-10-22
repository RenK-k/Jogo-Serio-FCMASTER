using System.Collections.Generic;
using CoachMaster.Domain;

namespace CoachMaster.Domain
{
    public sealed class PlayerFitReport
    {
        public float RoleScore;
        public float TacticalScore;
        public string StrongestAttribute;
        public int StrongestValue;
        public string DevelopmentAttribute;
        public int DevelopmentValue;

        public string RoleGrade => Grade(RoleScore);
        public string TacticalGrade => Grade(TacticalScore);

        static string Grade(float score)
        {
            if (score >= 17f) return "S";
            if (score >= 15f) return "A";
            if (score >= 13f) return "B";
            if (score >= 11f) return "C";
            if (score >= 9f) return "D";
            if (score >= 7f) return "E";
            return "F";
        }
    }

    public static class PlayerTacticalFit
    {
        public static PlayerFitReport Evaluate(Player player, TacticalIdentity tactic, string duty)
        {
            var report = new PlayerFitReport();
            if (player == null || player.Attributes == null)
                return report;
            var required = RequiredAttributes(player.Position);
            if (required.Count == 0)
                return report;

            var weighted = 0f;
            var weights = 0f;
            var strongest = int.MinValue;
            var weakest = int.MaxValue;
            foreach (var item in required)
            {
                var value = PlayerAttributes.CurrentValue(player, item.Key);
                weighted += value * item.Value;
                weights += item.Value;
                if (value > strongest)
                {
                    strongest = value;
                    report.StrongestAttribute = item.Key;
                    report.StrongestValue = value;
                }
                if (value < weakest)
                {
                    weakest = value;
                    report.DevelopmentAttribute = item.Key;
                    report.DevelopmentValue = value;
                }
            }
            report.RoleScore = weights <= 0f ? 0f : weighted / weights;
            report.TacticalScore = DutyScore(player, duty);

            if (tactic != null)
            {
                var pressure = tactic.Pressing ?? "";
                var line = tactic.DefensiveLine ?? "";
                var buildup = tactic.BuildUp ?? "";
                if (pressure.Contains("muito") && pressure.Contains("alta"))
                    report.TacticalScore += (PlayerAttributes.CurrentValue(player, "Resistência") + PlayerAttributes.CurrentValue(player, "Entrega") - 20f) * 0.12f;
                if (line.Contains("muito") && line.Contains("alta") && (player.Position == PlayerPosition.CentreBack || player.Position == PlayerPosition.Goalkeeper))
                    report.TacticalScore += (PlayerAttributes.CurrentValue(player, "Velocidade") - 10f) * 0.12f;
                if (buildup.Contains("curta"))
                    report.TacticalScore += (PlayerAttributes.CurrentValue(player, "Passe") + PlayerAttributes.CurrentValue(player, "Decisão") - 20f) * 0.07f;
            }
            report.TacticalScore = UnityEngine.Mathf.Clamp(report.TacticalScore, 1f, 20f);
            return report;
        }

        static float DutyScore(Player player, string duty)
        {
            var text = duty ?? "";
            if (player.Position == PlayerPosition.Goalkeeper)
                return text.Contains("cruzamentos") || text.Contains("cruzamento")
                    ? Average(player, "Reflexos", "Posicionamento", "Manejo")
                    : Average(player, "Reflexos", "Manejo", "Posicionamento");
            if (text.Contains("Cortar para dentro"))
                return Average(player, "Drible", "Finalização", "Decisão");
            if (text.Contains("Ficar aberto"))
                return Average(player, "Cruzamento", "Velocidade", "Drible");
            if (text.Contains("Subir") || text.Contains("Chegar à área"))
                return Average(player, "Resistência", "Posicionamento", "Velocidade", "Finalização");
            if (text.Contains("Defensivo") || text.Contains("Marcar") || text.Contains("Ficar atrás"))
                return Average(player, "Desarme", "Posicionamento", "Força", "Decisão");
            if (text.Contains("desmarcação") || text.Contains("desmarcacao") || text.Contains("Mover-se"))
                return Average(player, "Velocidade", "Posicionamento", "Decisão");
            if (text.Contains("Sair com a bola"))
                return Average(player, "Passe", "Drible", "Decisão");
            if (text.Contains("cruzamentos"))
                return Average(player, "Reflexos", "Posicionamento", "Manejo");
            return Average(player, "Passe", "Posicionamento", "Decisão");
        }

        static float Average(Player player, params string[] keys)
        {
            var total = 0f;
            for (var i = 0; i < keys.Length; i++)
                total += PlayerAttributes.CurrentValue(player, keys[i]);
            return keys.Length == 0 ? 0f : total / keys.Length;
        }

        static List<KeyValuePair<string, float>> RequiredAttributes(PlayerPosition position)
        {
            switch (position)
            {
                case PlayerPosition.Goalkeeper:
                    return Weights("Reflexos", 1.3f, "Manejo", 1.1f, "Posicionamento", 1.1f, "Decisão", 0.8f, "Frieza", 0.7f);
                case PlayerPosition.RightBack:
                case PlayerPosition.LeftBack:
                    return Weights("Velocidade", 1.1f, "Resistência", 1.1f, "Desarme", 1f, "Cruzamento", 0.9f, "Posicionamento", 0.9f);
                case PlayerPosition.CentreBack:
                    return Weights("Força", 1.1f, "Desarme", 1.2f, "Posicionamento", 1.2f, "Decisão", 0.9f, "Frieza", 0.8f);
                case PlayerPosition.DefensiveMidfielder:
                    return Weights("Desarme", 1.1f, "Passe", 1f, "Resistência", 1f, "Posicionamento", 1f, "Decisão", 0.9f);
                case PlayerPosition.CentralMidfielder:
                    return Weights("Passe", 1.1f, "Resistência", 1f, "Drible", 0.8f, "Decisão", 1.1f, "Entrega", 0.8f);
                case PlayerPosition.RightMidfielder:
                case PlayerPosition.LeftMidfielder:
                    return Weights("Resistência", 1.1f, "Cruzamento", 1f, "Passe", 1f, "Posicionamento", 0.9f, "Entrega", 1f, "Decisão", 0.9f);
                case PlayerPosition.AttackingMidfielder:
                    return Weights("Passe", 1.1f, "Drible", 1f, "Finalização", 0.8f, "Frieza", 0.9f, "Decisão", 1.2f);
                case PlayerPosition.RightWinger:
                case PlayerPosition.LeftWinger:
                    return Weights("Velocidade", 1.1f, "Drible", 1.1f, "Cruzamento", 0.9f, "Finalização", 0.8f, "Passe", 0.8f);
                case PlayerPosition.Striker:
                    return Weights("Finalização", 1.2f, "Posicionamento", 1.1f, "Frieza", 0.9f, "Velocidade", 0.9f, "Força", 0.7f);
                default:
                    return new List<KeyValuePair<string, float>>();
            }
        }

        static List<KeyValuePair<string, float>> Weights(params object[] pairs)
        {
            var result = new List<KeyValuePair<string, float>>();
            for (var i = 0; i + 1 < pairs.Length; i += 2)
                result.Add(new KeyValuePair<string, float>((string)pairs[i], (float)pairs[i + 1]));
            return result;
        }
    }
}
