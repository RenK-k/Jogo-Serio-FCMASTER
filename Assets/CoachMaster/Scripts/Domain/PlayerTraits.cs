using System.Collections.Generic;

namespace CoachMaster.Domain
{
    public static class PlayerTraits
    {
        public static void Assign(Player player, int seed)
        {
            if (player == null)
                return;
            player.Potential = Ceiling(player, seed);
            player.Traits = Pick(player, seed);
        }

        public static string Label(PlayerTrait trait)
        {
            switch (trait)
            {
                case PlayerTrait.Rapido: return "Rápido";
                case PlayerTrait.RemateEmJeito: return "Remate em jeito";
                case PlayerTrait.ImuneAPressao: return "Imune à pressão";
                case PlayerTrait.Driblador: return "Driblador";
                case PlayerTrait.Finalizador: return "Finalizador";
                case PlayerTrait.Visionario: return "Visionário";
                case PlayerTrait.Marcador: return "Marcador";
                case PlayerTrait.Trabalhador: return "Trabalhador";
                case PlayerTrait.Frieza: return "Frieza";
                case PlayerTrait.Potente: return "Remate potente";
                case PlayerTrait.Cruzador: return "Cruzador";
                case PlayerTrait.Cabeceador: return "Jogo aéreo";
                default: return "Equilibrado";
            }
        }

        public static string Effect(PlayerTrait trait)
        {
            switch (trait)
            {
                case PlayerTrait.Rapido: return "Ataca o espaço em corrida.";
                case PlayerTrait.RemateEmJeito: return "Procura o remate e a bola pode curvar.";
                case PlayerTrait.ImuneAPressao: return "O passe aguenta quando está pressionado.";
                case PlayerTrait.Driblador: return "Conduz e tenta ultrapassar.";
                case PlayerTrait.Finalizador: return "Remata quando chega à área.";
                case PlayerTrait.Visionario: return "Procura o passe que parte a linha.";
                case PlayerTrait.Marcador: return "Fecha o portador da bola.";
                case PlayerTrait.Trabalhador: return "Chega mais às segundas bolas.";
                case PlayerTrait.Frieza: return "Decide melhor no último terço.";
                case PlayerTrait.Potente: return "O remate sai tenso.";
                case PlayerTrait.Cruzador: return "Procura a linha e o cruzamento.";
                case PlayerTrait.Cabeceador: return "Ganha bolas altas.";
                default: return "Joga de forma equilibrada.";
            }
        }

        public static string List(Player player)
        {
            if (player == null || player.Traits == null || player.Traits.Count == 0)
                return "Sem traço marcado.";
            var text = Label(player.Traits[0]);
            for (var i = 1; i < player.Traits.Count; i++)
                text += "  ·  " + Label(player.Traits[i]);
            return text;
        }

        public static string HiddenPotential(Player player, bool known)
        {
            if (player == null)
                return "Potencial escondido.";
            if (!known)
                return "Potencial escondido. Sem um relatório de perto, o teto fica por descobrir.";
            var now = PlayerAttributes.Overall(player);
            var gap = player.Potential - now;
            if (player.Age >= 30 || gap <= 1)
                return "O teto parece perto do que já mostra. O número exacto continua escondido.";
            if (gap >= 12)
                return "Há um teto alto, ainda por cumprir. O número exacto continua escondido.";
            if (gap >= 6)
                return "Há margem para crescer. O número exacto continua escondido.";
            return "Pode afinar mais um pouco. O número exacto continua escondido.";
        }

        static int Ceiling(Player player, int seed)
        {
            var now = PlayerAttributes.Overall(player);
            var room = player.Age <= 19 ? 18 : player.Age <= 21 ? 14 : player.Age <= 23 ? 10 : player.Age <= 26 ? 6 : player.Age <= 29 ? 3 : 0;
            var roll = room == 0 ? 0 : (seed & 0x7fffffff) % (room + 1);
            var attr = player.Attributes;
            if (attr != null)
            {
                var sharp = 0;
                if (attr.Pace >= 16) sharp++;
                if (attr.Dribbling >= 16) sharp++;
                if (attr.Finishing >= 16) sharp++;
                if (attr.Passing >= 16) sharp++;
                if (attr.Composure >= 16) sharp++;
                if (sharp >= 3)
                    roll += 4;
            }

            var ceiling = now + roll;
            if (ceiling < now)
                ceiling = now;
            if (ceiling > 94)
                ceiling = 94;
            return ceiling;
        }

        static List<PlayerTrait> Pick(Player player, int seed)
        {
            var attr = player.Attributes;
            var ranked = new List<KeyValuePair<PlayerTrait, int>>();
            if (player.Position == PlayerPosition.Goalkeeper)
            {
                ranked.Add(new KeyValuePair<PlayerTrait, int>(PlayerTrait.Frieza, attr == null ? 10 : attr.Composure));
                ranked.Add(new KeyValuePair<PlayerTrait, int>(PlayerTrait.ImuneAPressao, attr == null ? 10 : (attr.Composure + attr.Decisions) / 2));
                ranked.Add(new KeyValuePair<PlayerTrait, int>(PlayerTrait.Trabalhador, attr == null ? 10 : attr.WorkRate));
                ranked.Add(new KeyValuePair<PlayerTrait, int>(PlayerTrait.Marcador, attr == null ? 10 : attr.Reflexes));
            }
            else
            {
                var pace = attr == null ? 10 : attr.Pace;
                var finish = attr == null ? 10 : attr.Finishing;
                var dribble = attr == null ? 10 : attr.Dribbling;
                var pass = attr == null ? 10 : attr.Passing;
                var tackle = attr == null ? 10 : attr.Tackling;
                var cross = attr == null ? 10 : attr.Crossing;
                var cool = attr == null ? 10 : attr.Composure;
                var work = attr == null ? 10 : attr.WorkRate;
                var mind = attr == null ? 10 : attr.Decisions;
                var power = attr == null ? 10 : attr.Strength;
                ranked.Add(new KeyValuePair<PlayerTrait, int>(PlayerTrait.Rapido, pace));
                ranked.Add(new KeyValuePair<PlayerTrait, int>(PlayerTrait.RemateEmJeito, (finish + cool) / 2));
                ranked.Add(new KeyValuePair<PlayerTrait, int>(PlayerTrait.ImuneAPressao, cool));
                ranked.Add(new KeyValuePair<PlayerTrait, int>(PlayerTrait.Driblador, dribble));
                ranked.Add(new KeyValuePair<PlayerTrait, int>(PlayerTrait.Finalizador, finish));
                ranked.Add(new KeyValuePair<PlayerTrait, int>(PlayerTrait.Visionario, (pass + mind) / 2));
                ranked.Add(new KeyValuePair<PlayerTrait, int>(PlayerTrait.Marcador, tackle));
                ranked.Add(new KeyValuePair<PlayerTrait, int>(PlayerTrait.Trabalhador, work));
                ranked.Add(new KeyValuePair<PlayerTrait, int>(PlayerTrait.Frieza, (cool + mind) / 2));
                ranked.Add(new KeyValuePair<PlayerTrait, int>(PlayerTrait.Potente, (power + finish) / 2));
                ranked.Add(new KeyValuePair<PlayerTrait, int>(PlayerTrait.Cruzador, cross));
                ranked.Add(new KeyValuePair<PlayerTrait, int>(PlayerTrait.Cabeceador, (power + (attr == null ? 10 : attr.Positioning)) / 2));
            }

            ranked.Sort((a, b) => b.Value.CompareTo(a.Value));
            var overall = PlayerAttributes.Overall(player);
            var count = overall >= 80 || ranked[0].Value >= 17 ? 3 : ranked[0].Value >= 14 ? 2 : 1;
            var picked = new List<PlayerTrait>();
            for (var i = 0; i < ranked.Count && picked.Count < count; i++)
            {
                if (i > 0 && ranked[i].Value < 12)
                    break;
                picked.Add(ranked[i].Key);
            }

            if (picked.Count == 0)
                picked.Add(ranked[(seed & 0x7fffffff) % ranked.Count].Key);
            return picked;
        }
    }
}
