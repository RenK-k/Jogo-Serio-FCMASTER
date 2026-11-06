using CoachMaster.Domain;
using UnityEngine;

namespace CoachMaster.MatchPlay.Spatial
{
    /// <summary>
    /// Reads the stored 1–20 attributes as 0–100. Missing FootySim fields are blends of those ratings, not new database columns.
    /// </summary>
    public struct AttributeScale
    {
        public float Pace;
        public float Stamina;
        public float Strength;
        public float Passing;
        public float Finishing;
        public float Dribbling;
        public float Tackling;
        public float Crossing;
        public float Positioning;
        public float Composure;
        public float WorkRate;
        public float Decisions;
        public float Reflexes;
        public float Handling;
        public float Vision;
        public float FirstTouch;
        public float Marking;
        public float Heading;
        public float Anticipation;
        public float Agility;
        public float Aggression;
        public float Teamwork;

        public static AttributeScale From(Player player)
        {
            var scale = new AttributeScale();
            if (player == null || player.Attributes == null)
            {
                scale.Pace = scale.Stamina = scale.Strength = scale.Passing = scale.Finishing = 50f;
                scale.Dribbling = scale.Tackling = scale.Crossing = scale.Positioning = scale.Composure = 50f;
                scale.WorkRate = scale.Decisions = scale.Reflexes = scale.Handling = 50f;
            }
            else
            {
                scale.Pace = Rise(player, "Velocidade");
                scale.Stamina = Rise(player, "Resistência");
                scale.Strength = Rise(player, "Força");
                scale.Passing = Rise(player, "Passe");
                scale.Finishing = Rise(player, "Finalização");
                scale.Dribbling = Rise(player, "Drible");
                scale.Tackling = Rise(player, "Desarme");
                scale.Crossing = Rise(player, "Cruzamento");
                scale.Positioning = Rise(player, "Posicionamento");
                scale.Composure = Rise(player, "Frieza");
                scale.WorkRate = Rise(player, "Entrega");
                scale.Decisions = Rise(player, "Decisão");
                scale.Reflexes = Rise(player, "Reflexos");
                scale.Handling = Rise(player, "Manejo");
            }

            scale.Vision = Mean(scale.Passing, scale.Decisions);
            scale.FirstTouch = Mean(scale.Dribbling, scale.Composure);
            scale.Marking = Mean(scale.Tackling, scale.Positioning);
            scale.Heading = Mean(scale.Finishing, scale.Strength);
            scale.Anticipation = Mean(scale.Decisions, scale.Positioning);
            scale.Agility = Mean(scale.Pace, scale.Dribbling);
            scale.Aggression = Mathf.Min(100f, Mean(scale.Tackling, scale.WorkRate));
            scale.Teamwork = Mean(scale.WorkRate, scale.Decisions);

            if (player != null)
            {
                if (player.HasTrait(PlayerTrait.Visionario))
                    scale.Vision = Lift(scale.Vision, 12f);
                if (player.HasTrait(PlayerTrait.CriadorDeJogo))
                    scale.Vision = Lift(scale.Vision, 6f);
                if (player.HasTrait(PlayerTrait.Marcador))
                    scale.Marking = Lift(scale.Marking, 10f);
                if (player.HasTrait(PlayerTrait.Cabeceador))
                    scale.Heading = Lift(scale.Heading, 12f);
                if (player.HasTrait(PlayerTrait.Cruzador))
                    scale.Crossing = Lift(scale.Crossing, 8f);
                if (player.HasTrait(PlayerTrait.Driblador) || player.HasTrait(PlayerTrait.DribladorTecnico))
                    scale.Dribbling = Lift(scale.Dribbling, 8f);
                if (player.HasTrait(PlayerTrait.ImuneAPressao))
                    scale.Composure = Lift(scale.Composure, 8f);
                if (player.HasTrait(PlayerTrait.CarrinhoArriscado))
                    scale.Aggression = Lift(scale.Aggression, 14f);
                if (player.HasTrait(PlayerTrait.Trabalhador))
                    scale.WorkRate = Lift(scale.WorkRate, 6f);
            }

            return scale;
        }

        public float KeeperStopping => Mean(Reflexes, Handling);
        public float KeeperPositioning => Positioning;

        static float Rise(Player player, string key)
        {
            return Mathf.Clamp(PlayerAttributes.CurrentValue(player, key) * 5f, 5f, 100f);
        }

        static float Mean(float a, float b)
        {
            return (a + b) * 0.5f;
        }

        static float Lift(float value, float amount)
        {
            return Mathf.Min(100f, value + amount);
        }
    }
}
