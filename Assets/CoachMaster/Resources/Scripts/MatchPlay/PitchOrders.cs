using UnityEngine;

namespace CoachMaster.MatchPlay
{
    public static class PitchOrders
    {
        public static bool UserHome = true;
        public static float Attack;
        public static float Calm;
        public static float Tempo;
        public static int HomeGoals;
        public static int AwayGoals;

        public static void Set(bool userHome, string kind, int homeGoals, int awayGoals)
        {
            UserHome = userHome;
            HomeGoals = homeGoals;
            AwayGoals = awayGoals;
            Attack = 0f;
            Calm = 0f;
            Tempo = 0f;
            switch (kind)
            {
                case "motivate":
                    Attack = 0.08f;
                    break;
                case "calm":
                    Calm = 0.1f;
                    break;
                case "demand":
                    Attack = 0.12f;
                    Tempo = 0.05f;
                    break;
                case "safe":
                    Calm = 0.08f;
                    Attack = -0.04f;
                    break;
                case "press":
                    Tempo = 0.12f;
                    Attack = 0.04f;
                    break;
                default:
                    Tempo = -0.1f;
                    Calm = 0.05f;
                    break;
            }
        }

        public static void SyncScore(int homeGoals, int awayGoals)
        {
            HomeGoals = homeGoals;
            AwayGoals = awayGoals;
        }

        public static float Speed(bool home, bool hasBall)
        {
            if (home != UserHome)
                return 1f;
            var behind = UserHome ? AwayGoals - HomeGoals : HomeGoals - AwayGoals;
            var speed = 1f;
            if (Attack > 0f)
                speed += behind > 0 ? Attack * 2.4f : Attack * 0.55f;
            if (Tempo > 0f && !hasBall)
                speed += Tempo * (behind >= 0 ? 1.5f : 0.45f);
            if (Tempo < 0f && hasBall)
                speed += Tempo * 0.4f;
            return Mathf.Clamp(speed, 0.86f, 1.34f);
        }

        public static float Miss(bool home)
        {
            if (home != UserHome)
                return 1f;
            var behind = UserHome ? AwayGoals - HomeGoals : HomeGoals - AwayGoals;
            var miss = 1f;
            if (Calm > 0f)
                miss -= behind <= 0 ? 0.28f : 0.08f;
            if (Attack > 0.05f && behind > 0)
                miss -= 0.16f;
            if (Attack > 0.05f && behind < 0)
                miss += 0.14f;
            if (Tempo < 0f && behind <= 0)
                miss -= 0.1f;
            return Mathf.Clamp(miss, 0.62f, 1.22f);
        }

        public static string FitLine(bool english, string kind)
        {
            var behind = UserHome ? AwayGoals - HomeGoals : HomeGoals - AwayGoals;
            if (kind == "calm" && behind <= 0)
                return english ? " The team settles and treats the ball better." : " A equipa assenta e trata melhor a bola.";
            if (kind == "motivate" && behind > 0)
                return english ? " It lifts them while they are chasing the score." : " Dá outro gás à equipa, que vai atrás do resultado.";
            if (kind == "demand" && behind > 0)
                return english ? " The demand lands because they need the goal." : " A exigência pega porque a equipa precisa do golo.";
            if ((kind == "safe" || kind == "slow") && behind < 0)
                return english ? " Protecting the lead makes them harder to break." : " Proteger a vantagem torna a equipa mais difícil de furar.";
            if (kind == "press" && behind >= 0)
                return english ? " The press gets to the ball earlier." : " A pressão chega mais cedo à bola.";
            if (kind == "slow" && behind == 0)
                return english ? " They slow the game and pick the pass." : " Abranda o jogo e escolhe melhor o passe.";
            return english ? " They hear it, but the moment asked for something else." : " Os jogadores ouvem, mas o momento pedia outra coisa.";
        }
    }
}
