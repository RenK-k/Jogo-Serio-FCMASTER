using System;
using UnityEngine;

namespace CoachMaster.Domain
{
    public static class IndividualOrders
    {
        public static string For(PlayerPosition position, int seed)
        {
            var odd = seed % 2 == 0;
            switch (position)
            {
                case PlayerPosition.Goalkeeper:
                    return "Ficar na linha";
                case PlayerPosition.RightBack:
                case PlayerPosition.LeftBack:
                    return odd ? "Subir" : "Ficar atrás";
                case PlayerPosition.CentreBack:
                    return "Marcar";
                case PlayerPosition.DefensiveMidfielder:
                    return "Ficar atrás";
                case PlayerPosition.CentralMidfielder:
                    return odd ? "Equilibrar" : "Chegar à área";
                case PlayerPosition.AttackingMidfielder:
                    return "Chegar à área";
                case PlayerPosition.RightWinger:
                case PlayerPosition.LeftWinger:
                    return odd ? "Cortar para dentro" : "Ficar aberto";
                case PlayerPosition.Striker:
                    return odd ? "Ficar na área" : "Mover-se";
                default:
                    return "Equilibrar";
            }
        }

        public static Vector2 Offset(string duty, bool attacking, Vector2 slot)
        {
            var shift = Vector2.zero;
            if (duty == "Cortar para dentro" && attacking)
                shift.x = slot.x < 0.5f ? 0.12f : -0.12f;
            else if (duty == "Ficar aberto" && attacking)
                shift.x = slot.x < 0.5f ? -0.07f : 0.07f;
            else if (duty == "Subir" && attacking)
                shift.y = 0.12f;
            else if (duty == "Ficar atrás")
                shift.y = attacking ? -0.08f : -0.03f;
            else if (duty == "Chegar à área" && attacking)
                shift.y = 0.11f;
            else if (duty == "Ficar na área" && attacking)
                shift.y = 0.05f;
            else if (duty == "Mover-se" && attacking)
                shift.x = slot.x < 0.5f ? 0.08f : -0.08f;
            else if (duty == "Marcar" && !attacking)
                shift.y = -0.02f;
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
                    return new[] { "Ficar na linha" };
                case PlayerPosition.RightBack:
                case PlayerPosition.LeftBack:
                    return new[] { "Subir", "Ficar atrás", "Equilibrar" };
                case PlayerPosition.CentreBack:
                    return new[] { "Marcar", "Ficar atrás", "Equilibrar" };
                case PlayerPosition.DefensiveMidfielder:
                    return new[] { "Ficar atrás", "Equilibrar", "Chegar à área" };
                case PlayerPosition.CentralMidfielder:
                    return new[] { "Equilibrar", "Chegar à área", "Ficar atrás" };
                case PlayerPosition.AttackingMidfielder:
                    return new[] { "Chegar à área", "Equilibrar", "Mover-se" };
                case PlayerPosition.RightWinger:
                case PlayerPosition.LeftWinger:
                    return new[] { "Cortar para dentro", "Ficar aberto", "Mover-se" };
                case PlayerPosition.Striker:
                    return new[] { "Ficar na área", "Mover-se", "Chegar à área" };
                default:
                    return new[] { "Equilibrar", "Mover-se", "Chegar à área" };
            }
        }

        static string[] All()
        {
            return new[] { "Cortar para dentro", "Ficar aberto", "Subir", "Ficar atrás", "Chegar à área", "Equilibrar", "Marcar", "Ficar na área", "Mover-se", "Ficar na linha" };
        }
    }
}
