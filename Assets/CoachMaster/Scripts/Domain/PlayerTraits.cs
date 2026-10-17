using System;
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

        public static bool TryDevelop(Player player, int seed)
        {
            if (player == null || player.Age > 23 || player.Traits == null || player.Traits.Count >= 4)
                return false;
            var gap = player.Potential - PlayerAttributes.Overall(player);
            if (gap < 1)
                return false;

            var chance = player.Age <= 18 ? 0.16 : player.Age <= 20 ? 0.105 : 0.04;
            if (gap >= 8)
                chance += 0.05;
            else if (gap <= 2)
                chance *= 0.45;
            var random = new Random(seed);
            if (random.NextDouble() >= chance)
                return false;

            var candidates = Candidates(player);
            for (var i = candidates.Count - 1; i >= 0; i--)
            {
                var blocked = player.Traits.Contains(candidates[i].Key);
                for (var j = 0; !blocked && j < player.Traits.Count; j++)
                    blocked = Conflicts(candidates[i].Key, player.Traits[j]);
                if (blocked)
                    candidates.RemoveAt(i);
            }
            var trait = Draw(candidates, random);
            if (!trait.HasValue)
                return false;
            player.Traits.Add(trait.Value);
            return true;
        }

        public static int DevelopmentSeed(string playerId, int seasonYear)
        {
            unchecked
            {
                var hash = 17;
                var value = playerId ?? string.Empty;
                for (var i = 0; i < value.Length; i++)
                    hash = hash * 31 + value[i];
                return hash * 31 + seasonYear;
            }
        }

        public static string Label(PlayerTrait trait)
        {
            switch (trait)
            {
                case PlayerTrait.Rapido: return "Rápido";
                case PlayerTrait.RemateEmJeito: return "Remate colocado";
                case PlayerTrait.ImuneAPressao: return "Resistente à pressão";
                case PlayerTrait.Driblador: return "Driblador";
                case PlayerTrait.Finalizador: return "Finalizador";
                case PlayerTrait.Visionario: return "Visionário";
                case PlayerTrait.Marcador: return "Marcador";
                case PlayerTrait.Trabalhador: return "Trabalhador";
                case PlayerTrait.Frieza: return "Frieza";
                case PlayerTrait.Potente: return "Remate potente";
                case PlayerTrait.Cruzador: return "Cruzador";
                case PlayerTrait.Cabeceador: return "Jogo aéreo";
                case PlayerTrait.RemateRasteiro: return "Remate rasteiro";
                case PlayerTrait.RemateDeLonge: return "Remate de longe";
                case PlayerTrait.RemateDeTrivela: return "Remate de trivela";
                case PlayerTrait.LancamentoLongo: return "Lançamento longo";
                case PlayerTrait.LivrePotente: return "Livres potentes";
                case PlayerTrait.Efeito: return "Efeito";
                case PlayerTrait.PasseDePrimeira: return "Passe de primeira";
                case PlayerTrait.Flair: return "Flair";
                case PlayerTrait.PropensoALesoes: return "Propenso a lesões";
                case PlayerTrait.ResistenteALesoes: return "Resistente a lesões";
                case PlayerTrait.Chilenas: return "Chilenas";
                case PlayerTrait.Lideranca: return "Liderança";
                case PlayerTrait.HomemDeUmClube: return "Homem de um só clube";
                case PlayerTrait.JogadorDeEquipa: return "Jogador de equipa";
                case PlayerTrait.DribladorTecnico: return "Driblador técnico";
                case PlayerTrait.DribladorRapido: return "Driblador veloz";
                case PlayerTrait.PassadorLongo: return "Passador longo";
                case PlayerTrait.RematadorDeLonge: return "Rematador de longe";
                case PlayerTrait.CriadorDeJogo: return "Criador de jogo";
                case PlayerTrait.CarrinhoArriscado: return "Entrada de carrinho";
                case PlayerTrait.GuardaRedesSaiDaBaliza: return "Sai da baliza";
                case PlayerTrait.GuardaRedesSaiAosCruzamentos: return "Sai aos cruzamentos";
                case PlayerTrait.GuardaRedesDefendeComOsPes: return "Defende com os pés";
                default: return "Equilibrado";
            }
        }

        public static string Effect(PlayerTrait trait)
        {
            switch (trait)
            {
                case PlayerTrait.Rapido: return "Ataca o espaço em corrida.";
                case PlayerTrait.RemateEmJeito: return "Procura remates colocados e pode dar-lhes efeito.";
                case PlayerTrait.ImuneAPressao: return "Mantém a qualidade das decisões sob pressão.";
                case PlayerTrait.Driblador: return "Conduz e tenta ultrapassar o adversário.";
                case PlayerTrait.Finalizador: return "Ataca a área e aproveita ocasiões de golo.";
                case PlayerTrait.Visionario: return "Procura passes que ultrapassam linhas defensivas.";
                case PlayerTrait.Marcador: return "Fecha linhas e acompanha o portador da bola.";
                case PlayerTrait.Trabalhador: return "Esforça-se mais na pressão e nas segundas bolas.";
                case PlayerTrait.Frieza: return "Decide com calma perto da baliza.";
                case PlayerTrait.Potente: return "Dá mais força aos remates, com menor precisão.";
                case PlayerTrait.Cruzador: return "Procura zonas de cruzamento e melhora a sua execução.";
                case PlayerTrait.Cabeceador: return "Tem vantagem nas disputas aéreas.";
                case PlayerTrait.RemateRasteiro: return "Procura remates baixos quando tem espaço.";
                case PlayerTrait.RemateDeLonge: return "Arrisca remates de fora da área.";
                case PlayerTrait.RemateDeTrivela: return "Tenta rematar com a parte exterior do pé em situações adequadas.";
                case PlayerTrait.LancamentoLongo: return "Executa lançamentos laterais mais longos e procura colegas na área.";
                case PlayerTrait.LivrePotente: return "Faz a preparação mais longa e bate livres diretos com força.";
                case PlayerTrait.Efeito: return "Dá mais efeito a passes rasteiros e executa trivela com maior precisão.";
                case PlayerTrait.PasseDePrimeira: return "Erra menos passes de primeira e combinações rápidas.";
                case PlayerTrait.Flair: return "Arrisca fintas e passes criativos quando a situação o permite.";
                case PlayerTrait.PropensoALesoes: return "Tem maior risco de lesão em choques e disputas físicas.";
                case PlayerTrait.ResistenteALesoes: return "Tem menor risco de lesão em choques e disputas físicas.";
                case PlayerTrait.Chilenas: return "Tenta chilenas quando recebe uma bola aérea em boa posição.";
                case PlayerTrait.Lideranca: return "É uma escolha forte para assumir a braçadeira.";
                case PlayerTrait.HomemDeUmClube: return "Valoriza a lealdade ao clube e é candidato à braçadeira.";
                case PlayerTrait.JogadorDeEquipa: return "Privilegia o grupo e é candidato à braçadeira.";
                case PlayerTrait.DribladorTecnico: return "Tenta conduções longas e fintas técnicas quando há espaço.";
                case PlayerTrait.DribladorRapido: return "Usa a velocidade para conduzir e atacar espaço livre.";
                case PlayerTrait.PassadorLongo: return "Procura passes longos para progredir quando há um alvo livre.";
                case PlayerTrait.RematadorDeLonge: return "Procura remates de longe, incluindo em bolas no ar.";
                case PlayerTrait.CriadorDeJogo: return "Os colegas procuram-no mais e ele decide a circulação mais depressa.";
                case PlayerTrait.CarrinhoArriscado: return "Recorre mais ao carrinho, com maior risco de falta.";
                case PlayerTrait.GuardaRedesSaiDaBaliza: return "Sai para cruzamentos apenas quando chega claramente primeiro.";
                case PlayerTrait.GuardaRedesSaiAosCruzamentos: return "Ataca cruzamentos quando prevê chegar à bola por pouco antes do rival.";
                case PlayerTrait.GuardaRedesDefendeComOsPes: return "Usa os pés com mais frequência em remates de curta distância.";
                default: return "Joga de forma equilibrada.";
            }
        }

        public static string List(Player player)
        {
            if (player == null || player.Traits == null || player.Traits.Count == 0)
                return "Sem traços especiais.";
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

            return Math.Min(94, Math.Max(now, now + roll));
        }

        static List<PlayerTrait> Pick(Player player, int seed)
        {
            var random = new Random(seed);
            var roll = random.NextDouble();
            if (PlayerAttributes.Overall(player) >= 80)
                roll = Math.Max(0d, roll - 0.035d);
            var count = roll < 0.18 ? 0 : roll < 0.66 ? 1 : roll < 0.92 ? 2 : 3;
            var picked = new List<PlayerTrait>();
            var candidates = Candidates(player);
            while (picked.Count < count && candidates.Count > 0)
            {
                var trait = Draw(candidates, random);
                if (!trait.HasValue)
                    break;
                picked.Add(trait.Value);
                candidates.RemoveAll(item => item.Key == trait.Value || Conflicts(item.Key, trait.Value));
            }
            return picked;
        }

        static List<KeyValuePair<PlayerTrait, double>> Candidates(Player player)
        {
            var result = new List<KeyValuePair<PlayerTrait, double>>();
            var a = player.Attributes;
            var pace = Value(a == null ? 10 : a.Pace);
            var finish = Value(a == null ? 10 : a.Finishing);
            var dribble = Value(a == null ? 10 : a.Dribbling);
            var pass = Value(a == null ? 10 : a.Passing);
            var tackle = Value(a == null ? 10 : a.Tackling);
            var cross = Value(a == null ? 10 : a.Crossing);
            var cool = Value(a == null ? 10 : a.Composure);
            var work = Value(a == null ? 10 : a.WorkRate);
            var mind = Value(a == null ? 10 : a.Decisions);
            var strength = Value(a == null ? 10 : a.Strength);
            var position = Value(a == null ? 10 : a.Positioning);
            var keeper = player.Position == PlayerPosition.Goalkeeper;
            var attack = player.Position == PlayerPosition.Striker || player.Position == PlayerPosition.LeftWinger || player.Position == PlayerPosition.RightWinger || player.Position == PlayerPosition.AttackingMidfielder;
            var wide = player.Position == PlayerPosition.LeftWinger || player.Position == PlayerPosition.RightWinger || player.Position == PlayerPosition.LeftBack || player.Position == PlayerPosition.RightBack;
            var middle = player.Position == PlayerPosition.CentralMidfielder || player.Position == PlayerPosition.DefensiveMidfielder || player.Position == PlayerPosition.AttackingMidfielder;
            var defender = player.Position == PlayerPosition.CentreBack || player.Position == PlayerPosition.LeftBack || player.Position == PlayerPosition.RightBack || player.Position == PlayerPosition.DefensiveMidfielder;
            var overall = PlayerAttributes.Overall(player);

            if (keeper)
            {
                Add(result, PlayerTrait.Frieza, 0.5 + cool * 0.12);
                Add(result, PlayerTrait.ImuneAPressao, 0.5 + cool * 0.1);
                Add(result, PlayerTrait.Trabalhador, 0.5 + work * 0.08);
                Add(result, PlayerTrait.Lideranca, LeadershipWeight(player, overall));
                Add(result, PlayerTrait.JogadorDeEquipa, 0.7 + (player.Personality == PersonalityTrait.Professional ? 0.8 : 0));
                Add(result, PlayerTrait.HomemDeUmClube, 0.5 + (player.Age >= 27 ? 0.5 : 0));
                Add(result, PlayerTrait.PropensoALesoes, 0.38 + (a != null && a.Stamina < 9 ? 0.35 : 0));
                Add(result, PlayerTrait.ResistenteALesoes, 0.45 + strength * 0.045);
                Add(result, PlayerTrait.GuardaRedesSaiDaBaliza, 0.8 + mind * 0.09 + pace * 0.04);
                Add(result, PlayerTrait.GuardaRedesSaiAosCruzamentos, 0.75 + (a == null ? 0 : a.Handling * 0.08) + mind * 0.04);
                Add(result, PlayerTrait.GuardaRedesDefendeComOsPes, 0.6 + (a == null ? 0 : a.Reflexes * 0.07));
                return result;
            }

            Add(result, PlayerTrait.Rapido, (wide || attack ? 0.8 : 0.35) + pace * 0.085);
            Add(result, PlayerTrait.RemateEmJeito, (attack ? 0.8 : 0.12) + finish * 0.045 + cool * 0.035);
            Add(result, PlayerTrait.ImuneAPressao, 0.5 + cool * 0.09);
            Add(result, PlayerTrait.Driblador, (wide || attack ? 0.85 : 0.3) + dribble * 0.085);
            Add(result, PlayerTrait.Finalizador, (attack ? 0.9 : 0.12) + finish * 0.075 + position * 0.025);
            Add(result, PlayerTrait.Visionario, (middle ? 0.95 : 0.38) + pass * 0.05 + mind * 0.055);
            Add(result, PlayerTrait.Marcador, (defender ? 0.9 : 0.38) + tackle * 0.08);
            Add(result, PlayerTrait.Trabalhador, 0.45 + work * 0.085);
            Add(result, PlayerTrait.Frieza, (attack ? 0.65 : 0.28) + cool * 0.06 + mind * 0.03);
            Add(result, PlayerTrait.Potente, (attack || middle ? 0.4 : 0.1) + strength * 0.065 + finish * 0.025);
            Add(result, PlayerTrait.Cruzador, (wide ? 0.95 : 0.18) + cross * 0.08);
            Add(result, PlayerTrait.Cabeceador, (player.Position == PlayerPosition.Striker || player.Position == PlayerPosition.CentreBack ? 0.85 : 0.16) + strength * 0.055 + position * 0.035);
            Add(result, PlayerTrait.RemateRasteiro, (attack ? 0.55 : middle ? 0.28 : 0.08) + finish * 0.035);
            Add(result, PlayerTrait.RemateDeLonge, (middle ? 0.72 : attack ? 0.35 : 0.1) + finish * 0.035 + mind * 0.025);
            Add(result, PlayerTrait.RemateDeTrivela, (wide || attack ? 0.35 : 0.08) + dribble * 0.025 + finish * 0.025);
            Add(result, PlayerTrait.LancamentoLongo, (player.Position == PlayerPosition.LeftBack || player.Position == PlayerPosition.RightBack || player.Position == PlayerPosition.CentreBack ? 0.28 : 0.04) + strength * 0.045);
            Add(result, PlayerTrait.LivrePotente, (attack || middle ? 0.32 : 0.08) + strength * 0.035 + pass * 0.02);
            Add(result, PlayerTrait.Efeito, 0.26 + pass * 0.04 + cross * 0.025);
            Add(result, PlayerTrait.PasseDePrimeira, (middle ? 0.58 : 0.25) + pass * 0.045 + mind * 0.025);
            Add(result, PlayerTrait.Flair, (wide || attack ? 0.42 : 0.12) + dribble * 0.035 + mind * 0.02);
            Add(result, PlayerTrait.PropensoALesoes, 0.25 + (a != null && a.Stamina < 9 ? 0.45 : 0) + (work >= 16 ? 0.12 : 0));
            Add(result, PlayerTrait.ResistenteALesoes, 0.3 + strength * 0.04 + (a == null ? 0 : a.Stamina * 0.025));
            Add(result, PlayerTrait.Chilenas, (attack ? 0.18 : 0.015) + finish * 0.012 + dribble * 0.012 + position * 0.01);
            Add(result, PlayerTrait.Lideranca, LeadershipWeight(player, overall));
            Add(result, PlayerTrait.HomemDeUmClube, 0.36 + (player.Age >= 27 ? 0.35 : 0) + (player.Personality == PersonalityTrait.Professional ? 0.2 : 0));
            Add(result, PlayerTrait.JogadorDeEquipa, 0.55 + (player.Personality == PersonalityTrait.Professional ? 0.55 : 0) + (player.Personality == PersonalityTrait.Leader ? 0.3 : 0));
            Add(result, PlayerTrait.DribladorTecnico, (wide || attack ? 0.55 : 0.16) + dribble * 0.045 + mind * 0.02);
            Add(result, PlayerTrait.DribladorRapido, (wide || attack ? 0.5 : 0.12) + pace * 0.04 + dribble * 0.025);
            Add(result, PlayerTrait.PassadorLongo, 0.2 + pass * 0.045 + mind * 0.025 + (middle ? 0.25 : 0));
            Add(result, PlayerTrait.RematadorDeLonge, (middle ? 0.42 : attack ? 0.3 : 0.05) + finish * 0.035 + mind * 0.025);
            Add(result, PlayerTrait.CriadorDeJogo, (middle ? 0.75 : 0.2) + pass * 0.04 + mind * 0.045);
            Add(result, PlayerTrait.CarrinhoArriscado, (defender ? 0.55 : 0.12) + tackle * 0.04 + (player.Personality == PersonalityTrait.Temperamental ? 0.3 : 0));
            return result;
        }

        static double LeadershipWeight(Player player, int overall)
        {
            var value = 0.28 + (player.IsCaptain ? 1.3 : 0) + (player.IsViceCaptain ? 0.6 : 0) + (player.Personality == PersonalityTrait.Leader ? 1.1 : 0);
            if (player.Age >= 27)
                value += 0.42;
            if (overall >= 78)
                value += 0.2;
            return value;
        }

        static int Value(int stat)
        {
            return stat < 1 ? 1 : stat > 20 ? 20 : stat;
        }

        static void Add(List<KeyValuePair<PlayerTrait, double>> values, PlayerTrait trait, double weight)
        {
            if (weight > 0.02)
                values.Add(new KeyValuePair<PlayerTrait, double>(trait, weight));
        }

        static PlayerTrait? Draw(List<KeyValuePair<PlayerTrait, double>> values, Random random)
        {
            if (values == null || values.Count == 0)
                return null;
            var total = 0d;
            for (var i = 0; i < values.Count; i++)
                total += values[i].Value;
            var roll = random.NextDouble() * total;
            for (var i = 0; i < values.Count; i++)
            {
                roll -= values[i].Value;
                if (roll <= 0d)
                    return values[i].Key;
            }
            return values[values.Count - 1].Key;
        }

        static bool Conflicts(PlayerTrait left, PlayerTrait right)
        {
            return (left == PlayerTrait.PropensoALesoes && right == PlayerTrait.ResistenteALesoes)
                || (left == PlayerTrait.ResistenteALesoes && right == PlayerTrait.PropensoALesoes)
                || (left == PlayerTrait.RemateDeLonge && right == PlayerTrait.RematadorDeLonge)
                || (left == PlayerTrait.RematadorDeLonge && right == PlayerTrait.RemateDeLonge)
                || (left == PlayerTrait.GuardaRedesSaiDaBaliza && right == PlayerTrait.GuardaRedesSaiAosCruzamentos)
                || (left == PlayerTrait.GuardaRedesSaiAosCruzamentos && right == PlayerTrait.GuardaRedesSaiDaBaliza);
        }
    }
}
