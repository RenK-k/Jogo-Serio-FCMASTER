using UnityEngine;

namespace CoachMaster.Domain
{
    public struct PlayerGrade
    {
        public string Code;
        public Color Tint;

        public static PlayerGrade Of(int overall)
        {
            if (overall >= 90)
                return new PlayerGrade { Code = "HI", Tint = new Color(1f, 0.82f, 0.22f, 1f) };
            if (overall >= 85)
                return new PlayerGrade { Code = "CM", Tint = new Color(0.78f, 0.48f, 1f, 1f) };
            if (overall >= 80)
                return new PlayerGrade { Code = "IC", Tint = new Color(0.28f, 0.74f, 1f, 1f) };
            if (overall >= 75)
                return new PlayerGrade { Code = "JU", Tint = new Color(0.30f, 0.88f, 0.48f, 1f) };
            if (overall >= 70)
                return new PlayerGrade { Code = "SD", Tint = new Color(1f, 0.55f, 0.22f, 1f) };
            return new PlayerGrade { Code = "PD", Tint = new Color(0.62f, 0.78f, 0.86f, 1f) };
        }

        public string Name(bool english)
        {
            switch (Code)
            {
                case "HI": return english ? "Historic" : "Histórico";
                case "CM": return english ? "World Class" : "Classe Mundial";
                case "IC": return english ? "International" : "Internacional";
                case "JU": return english ? "Useful Player" : "Jogador Útil";
                case "SD": return english ? "Second Division" : "Segunda Divisão";
                default: return english ? "Developing Professional" : "Profissional em Desenvolvimento";
            }
        }

        public static string Shift(int baseline, int current, bool english)
        {
            var from = Of(baseline);
            var to = Of(current);
            if (from.Code == to.Code)
                return (english ? "BASE " : "BASE ") + from.Code + (english ? "   ·   FORM steady" : "   ·   FORMA estável");
            var up = current > baseline;
            if (english)
                return "BASE " + from.Code + (up ? "   ·   FORM rises to " : "   ·   FORM drops to ") + to.Code;
            return "BASE " + from.Code + (up ? "   ·   FORMA sobe para " : "   ·   FORMA desce para ") + to.Code;
        }
    }
}
