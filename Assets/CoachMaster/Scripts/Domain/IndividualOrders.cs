using System;
using UnityEngine;

namespace CoachMaster.Domain
{
    public static class IndividualOrders
    {
        public static string For(PlayerPosition position, int seed)
        {
            var options = ForPosition(position);
            if (options.Length == 0)
                return "Equilibrar";
            var index = Mathf.Abs(seed) % options.Length;
            return options[index];
        }

        public static Vector2 Offset(string duty, bool attacking, Vector2 slot)
        {
            var shift = Vector2.zero;
            var text = duty ?? "";
            var inward = text.Contains("Cortar para dentro");
            var wide = text.Contains("Ficar aberto");
            var offensive = text.Contains("ofensivo") || text == "Subir" || text == "Chegar à área" || text == "Ficar na área";
            var defensive = text.Contains("Defensivo") || text.Contains("defensivo") || text == "Ficar atrás" || text == "Marcar";
            var balanced = text.Contains("equilibrado") || text == "Equilibrar" || text == "Mover-se";
            var unmarked = text.Contains("desmarca");
            if (inward && attacking)
                shift.x = (slot.x < 0.5f ? 1f : -1f) * (offensive ? 0.16f : balanced ? 0.1f : 0.12f);
            else if (wide && attacking)
                shift.x = (slot.x < 0.5f ? -1f : 1f) * (defensive ? 0.04f : 0.08f);
            if (unmarked && attacking)
            {
                shift.x += slot.x < 0.5f ? -0.06f : 0.06f;
                shift.y += 0.03f;
            }
            if ((text == "Subir" || text.Contains("Subir")) && attacking)
                shift.y += offensive && !balanced ? 0.14f : 0.08f;
            if (text.Contains("Chegar à área") && attacking)
                shift.y += offensive && !balanced ? 0.14f : 0.08f;
            if (text == "Ficar na área" && attacking)
                shift.y += 0.06f;
            if (text.Contains("Mover-se") && attacking)
                shift.x += slot.x < 0.5f ? 0.05f : -0.05f;
            if (defensive)
                shift.y -= attacking ? 0.07f : 0.04f;
            else if (text == "Ficar atrás")
                shift.y -= attacking ? 0.08f : 0.03f;
            if (text == "Sair com a bola" && attacking)
                shift.y += 0.05f;
            if (balanced && !defensive && !offensive)
                shift.y += attacking ? 0.02f : 0f;
            return shift;
        }

        public static string Next(string duty)
        {
            var options = All();
            var index = Array.IndexOf(options, duty);
            return options[(index + 1 + options.Length) % options.Length];
        }

        public static string[] ForPosition(PlayerPosition position)
        {
            switch (position)
            {
                case PlayerPosition.Goalkeeper:
                    return new[] { "Ficar na linha", "Sair nos cruzamentos" };
                case PlayerPosition.RightBack:
                case PlayerPosition.LeftBack:
                    return new[] { "Subir equilibrado", "Subir ofensivo", "Equilibrar", "Ficar atrás", "Defensivo", "Só desmarcação" };
                case PlayerPosition.CentreBack:
                    return new[] { "Marcar", "Defensivo", "Equilibrar", "Sair com a bola" };
                case PlayerPosition.DefensiveMidfielder:
                    return new[] { "Ficar atrás", "Defensivo", "Equilibrar", "Só desmarcação", "Chegar à área equilibrado" };
                case PlayerPosition.CentralMidfielder:
                    return new[] { "Equilibrar", "Só desmarcação", "Chegar à área equilibrado", "Chegar à área ofensivo", "Defensivo" };
                case PlayerPosition.AttackingMidfielder:
                    return new[] { "Chegar à área equilibrado", "Chegar à área ofensivo", "Só desmarcação", "Equilibrar", "Cortar para dentro equilibrado" };
                case PlayerPosition.RightWinger:
                case PlayerPosition.LeftWinger:
                    return new[] { "Cortar para dentro equilibrado", "Cortar para dentro ofensivo", "Ficar aberto equilibrado", "Ficar aberto defensivo", "Só desmarcação", "Defensivo" };
                case PlayerPosition.Striker:
                    return new[] { "Ficar na área", "Só desmarcação", "Mover-se equilibrado", "Mover-se ofensivo", "Chegar à área ofensivo", "Defensivo" };
                default:
                    return new[] { "Equilibrar", "Só desmarcação", "Defensivo" };
            }
        }

        static string[] All()
        {
            return new[]
            {
                "Cortar para dentro equilibrado", "Cortar para dentro ofensivo", "Ficar aberto equilibrado", "Ficar aberto defensivo",
                "Subir equilibrado", "Subir ofensivo", "Ficar atrás", "Chegar à área equilibrado", "Chegar à área ofensivo",
                "Equilibrar", "Defensivo", "Só desmarcação", "Marcar", "Ficar na área", "Mover-se equilibrado", "Mover-se ofensivo",
                "Sair com a bola", "Ficar na linha", "Sair nos cruzamentos"
            };
        }
    }
}
