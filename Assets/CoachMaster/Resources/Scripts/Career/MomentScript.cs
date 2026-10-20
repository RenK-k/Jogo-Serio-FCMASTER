using System;
using System.Collections.Generic;
using CoachMaster.Domain;

namespace CoachMaster.Career
{
    public static class MomentScript
    {
        public static string Compose(CareerState career, DateTime when, string slot, out string board, out string playerId)
        {
            board = null;
            playerId = null;
            var squad = Squad(career);
            if (squad.Count == 0)
                return career.Club.DressingRoomAtmosphere;

            var seed = Hash(career.Club.Id + "|" + career.Coach.Name + "|" + when.ToString("yyyyMMdd") + "|" + slot);
            var focus = squad[seed % squad.Count];
            playerId = focus.Id;
            var other = squad[(seed / 5) % squad.Count];
            var captain = Captain(squad);
            var staff = StaffVoice(career, seed);
            var place = Pick(Places, seed);
            var gesture = Gesture(focus, seed / 11);

            string body;
            switch (slot)
            {
                case "treino":
                    body = staff + " não te passa uma folha. " + place + ", " + gesture + " " + Contrast(other, seed / 13) + "\n\nO " + career.Club.Tactics.Formation + " continua a ser o desenho do relvado. Ninguém te confirma se o grupo ainda acredita nele.";
                    if (focus.Fatigue >= 60)
                        board = focus.FullName + " ficou curto no exercício. " + Voice(focus);
                    break;
                case "tarde":
                    body = career.Club.PublicTeamState + "\n\n" + Meal(captain, focus, other, seed);
                    if (focus != captain && focus.Morale + 20 < captain.Morale)
                        board = focus.FullName + " come à parte. " + Voice(focus);
                    else if (career.Club.DressingRoomTension >= 65 && seed % 2 == 0)
                        board = "A mesa partiu-se. " + captain.FullName + " ficou com metade e " + focus.FullName + " com a outra.";
                    break;
                case "clube":
                    body = staff + " encontra-te " + place + ". " + Private(focus, other, seed) + "\n\nNão assina por baixo. Diz que, se quiseres nomes, tens de os chamar tu.";
                    if (focus.Morale < 45)
                        board = focus.FullName + " anda afastado. " + Voice(focus);
                    break;
                default:
                    body = "Entras no " + career.Club.Name + ". " + career.Club.DressingRoomAtmosphere + " " + gesture + " " + Contrast(other, seed / 9);
                    if (seed % 4 == 0)
                        board = focus.FullName + " chegou cedo. " + other.FullName + " entrou sem falar. " + Voice(focus);
                    break;
            }

            body += "\n\n" + focus.FullName + " " + Voice(focus) + " " + Habit(focus, seed);
            return body;
        }

        public static string Voice(Player player)
        {
            if (player == null)
                return "fica na dele.";
            switch (player.Personality)
            {
                case PersonalityTrait.Outspoken:
                    return "diz o que pensa, mesmo à frente do grupo.";
                case PersonalityTrait.Leader:
                    return "puxa os outros sem esperar que lho peçam.";
                case PersonalityTrait.Temperamental:
                    return "fecha a cara à primeira contrariedade.";
                case PersonalityTrait.Professional:
                    return "faz o trabalho e deixa o resto em silêncio.";
                case PersonalityTrait.Insecure:
                    return "olha para o lado antes de falar.";
                case PersonalityTrait.Ambitious:
                    return "mede o dia pelo lugar que quer ocupar.";
                default:
                    return "observa e só entra quando o chamam.";
            }
        }

        static string Habit(Player player, int seed)
        {
            if (player.HasTrait(PlayerTrait.Trabalhador))
                return "Fica no relvado depois dos outros.";
            if (player.HasTrait(PlayerTrait.Driblador))
                return "No último exercício ainda tenta um drible a mais.";
            if (player.HasTrait(PlayerTrait.Finalizador) || player.HasTrait(PlayerTrait.RemateEmJeito))
                return "Pede a última bola para rematar, não para passar.";
            if (player.HasTrait(PlayerTrait.Marcador))
                return "Sai do treino a falar da marcação, não do golo.";
            if (player.Position == PlayerPosition.Goalkeeper)
                return "Fica à baliza quando o resto já vai para o balneário.";
            var lines = new[]
            {
                "Come sozinho quando o dia corre mal.",
                "Procura o capitão no corredor, sem fazer cena.",
                "Ri-se alto no aquecimento e cala-se no balneário.",
                "Guarda o lugar do costume e não o troca."
            };
            return lines[System.Math.Abs(seed) % lines.Length];
        }

        public static string MatchCall(CareerState career, Fixture fixture)
        {
            var opp = career.World.Clubs[fixture.OpponentId(career.Coach.ClubId)];
            var where = fixture.IsHome(career.Coach.ClubId) ? "O " + career.Club.Stadium + " já tem gente nas bancadas." : "O autocarro pára no " + opp.Stadium + ".";
            var pressing = string.IsNullOrEmpty(opp.Tactics.Pressing) ? "um método próprio" : opp.Tactics.Pressing.ToLowerInvariant();
            return where + " Do outro lado está o " + opp.Name + ", " + opp.Tactics.Formation + ", " + pressing + ".\n\nO que viste hoje no centro de treinos é o relatório. Não há outro.";
        }

        public static string MatchBoard(CareerState career, Fixture fixture)
        {
            var opp = career.World.Clubs[fixture.OpponentId(career.Coach.ClubId)];
            var venue = fixture.IsHome(career.Coach.ClubId) ? "em casa" : "fora";
            return "Dia de jogo " + venue + " com o " + opp.ShortName + ".";
        }

        static string Meal(Player captain, Player focus, Player other, int seed)
        {
            if (focus != captain && focus.Morale < 48)
                return "Na mesa, " + captain.FullName + " puxa a cadeira e " + focus.FullName + " senta-se na outra ponta, de costas. " + other.FullName + " " + Pick(Sidelines, seed) + ".";
            if (focus.Morale > 72)
                return focus.FullName + " fica até ao café e puxa " + other.FullName + " para a conversa. O capitão " + captain.FullName + " " + Pick(CaptainActs, seed) + ".";
            return "No almoço o plantel divide-se, sem discussão. " + focus.FullName + " " + Pick(Sidelines, seed) + ". " + captain.FullName + " " + Pick(CaptainActs, seed / 3) + ".";
        }

        static string Private(Player focus, Player other, int seed)
        {
            var lines = new[]
            {
                focus.FullName + " compensa com o corpo o que não diz. O staff não põe isto num relatório.",
                "Se " + other.FullName + " voltar a sair do exercício a meio, o grupo repara. O staff já reparou.",
                "Há dois balneários dentro deste. " + focus.FullName + " escolheu um deles, e não foi o do capitão.",
                "Não é lesão. " + focus.FullName + " protege-se. Isso vê-se no último sprint, não num papel."
            };
            return lines[seed % lines.Length];
        }

        static string Gesture(Player player, int seed)
        {
            if (player.Fatigue >= 70)
                return player.FullName + " " + Pick(TiredActs, seed) + ".";
            if (player.Morale < 40)
                return player.FullName + " " + Pick(LowActs, seed) + ".";
            if (player.Morale > 75)
                return player.FullName + " " + Pick(HighActs, seed) + ".";
            return player.FullName + " " + Pick(QuietActs, seed) + ".";
        }

        static string Contrast(Player player, int seed)
        {
            return "Ao lado, " + player.FullName + " " + Pick(QuietActs, seed) + ".";
        }

        static string StaffVoice(CareerState career, int seed)
        {
            if (career.Club.Staff == null || career.Club.Staff.Count == 0)
                return "Alguém da equipa técnica";
            return career.Club.Staff[seed % career.Club.Staff.Count].FullName;
        }

        static Player Captain(List<Player> squad)
        {
            foreach (var player in squad)
            {
                if (player.IsCaptain)
                    return player;
            }

            return squad[0];
        }

        static List<Player> Squad(CareerState career)
        {
            var list = new List<Player>();
            foreach (var id in career.Club.PlayerIds)
            {
                if (career.World.Players.TryGetValue(id, out var player))
                    list.Add(player);
            }

            return list;
        }

        static string Pick(string[] lines, int seed)
        {
            var index = seed % lines.Length;
            if (index < 0)
                index += lines.Length;
            return lines[index];
        }

        static int Hash(string value)
        {
            unchecked
            {
                var hash = 23;
                foreach (var c in value)
                    hash = hash * 31 + c;
                return hash & 0x7fffffff;
            }
        }

        static readonly string[] Places =
        {
            "à porta do balneário",
            "no corredor do ginásio",
            "junto à linha lateral",
            "à saída do refeitório",
            "no parque de estacionamento do centro"
        };

        static readonly string[] TiredActs =
        {
            "aperta os atacadores duas vezes e chega um passo atrás da fila",
            "pede água antes do exercício acabar",
            "corta o último sprint e finge que foi o apito",
            "apoia as mãos nos joelhos mais tempo do que os outros"
        };

        static readonly string[] LowActs =
        {
            "responde sem virar a cabeça",
            "deixa a bola para outro sem discutir",
            "muda de sítio quando o grupo se junta",
            "treina no limite do círculo, sozinho"
        };

        static readonly string[] HighActs =
        {
            "junta a bola e chama os outros pelo nome",
            "fica a repetir o exercício depois do apito",
            "mete-se entre dois que iam discutir",
            "é o primeiro a entrar no relvado"
        };

        static readonly string[] QuietActs =
        {
            "cumpre e não pede mais nada",
            "fala só quando lhe perguntam",
            "faz o exercício e sai sem olhar para o banco",
            "está presente, sem liderar nem fugir"
        };

        static readonly string[] Sidelines =
        {
            "come depressa e levanta-se",
            "olha para o telemóvel o almoço inteiro",
            "fica na mesa mas não entra na conversa",
            "sai com o grupo dos mais novos"
        };

        static readonly string[] CaptainActs =
        {
            "não chama ninguém de volta",
            "fala baixo com os que ficaram",
            "olha para a cadeira vazia e não comenta",
            "muda de assunto quando o nome do outro aparece"
        };
    }
}
