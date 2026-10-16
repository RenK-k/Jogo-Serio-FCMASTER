using System;
using System.Collections.Generic;
using CoachMaster.Domain;

namespace CoachMaster.Career
{
    public enum MomentKind
    {
        Note,
        Talk,
        Match
    }

    public sealed class CareerMoment
    {
        public DateTime When;
        public string Title;
        public string Body;
        public MomentKind Kind;
        public string PlayerId;
        public Fixture Fixture;
        public string[] Replies;
        public string[] FollowUps;
        public int[] MoraleDelta;
        public int[] FatigueDelta;
        public bool Answered;
        public string BoardLine;
    }

    public static class CareerMoments
    {
        public static void Begin(CareerState career)
        {
            career.Upcoming.Clear();
            career.Current = null;
            QueueUntil(career, 1);
            Pull(career);
            Newsroom.EnsureSeed(career);
        }

        public static void Continue(CareerState career)
        {
            if (career.Current != null && career.Current.Kind == MomentKind.Match && !career.Current.Fixture.Played)
                return;

            Pull(career);
        }

        public static void Answer(CareerState career, int choice)
        {
            var moment = career.Current;
            if (moment == null || moment.Kind != MomentKind.Talk || moment.Answered)
                return;
            if (moment.Replies == null || choice < 0 || choice >= moment.Replies.Length)
                return;

            moment.Answered = true;
            var player = career.World.Players[moment.PlayerId];
            player.Morale = Clamp(player.Morale + moment.MoraleDelta[choice]);
            player.Fatigue = Clamp(player.Fatigue + moment.FatigueDelta[choice]);
            moment.Body += "\n\nTu: " + moment.Replies[choice] + "\n\n" + moment.FollowUps[choice];
            if (moment.FollowUps[choice].Contains("fisioterapeuta") || moment.FollowUps[choice].Contains("coxear") || moment.FollowUps[choice].Contains("não apoia"))
            {
                player.Injury.IsInjured = true;
                player.Injury.Description = "Lesão muscular";
                player.Injury.DaysRemaining = choice == 0 ? 8 : 16;
            }

            if (moment.FollowUps[choice].Contains("Muda o movimento"))
            {
                if (player.Duty == "Cortar para dentro")
                    player.Duty = "Ficar aberto";
                else if (player.Duty == "Ficar aberto")
                    player.Duty = "Cortar para dentro";
            }
            career.Notebook.Insert(0, career.Calendar.ClockText + "  " + player.FullName + " — conversa no centro de treinos.");
        }

        public static void AfterMatch(CareerState career, int homeGoals, int awayGoals, bool userHome, string homeName, string awayName, string motm = null, string motmMark = null)
        {
            career.Current = null;
            career.Upcoming.Clear();
            var when = career.Calendar.CurrentDate.Date.AddHours(19);
            if (when < career.Calendar.CurrentDate)
                career.Calendar.SetInstant(when);
            var userGoals = userHome ? homeGoals : awayGoals;
            var oppGoals = userHome ? awayGoals : homeGoals;
            var score = homeName + " " + homeGoals + "-" + awayGoals + " " + awayName;
            string verdict;
            string tone;
            if (userGoals > oppGoals)
            {
                verdict = "Vitória";
                tone = userHome
                    ? "Ganharam. " + score + ". O balneário fala baixo, mas há sorrisos contidos."
                    : "Ganharam fora. " + score + ". No autocarro ainda se fala do golo, desta vez a rir.";
            }
            else if (userGoals == oppGoals)
            {
                verdict = "Empate";
                tone = userHome
                    ? "Empate. " + score + ". Ninguém grita. Ninguém festeja."
                    : "Empate fora. " + score + ". A viagem de regresso é longa e quase muda.";
            }
            else
            {
                verdict = "Derrota";
                tone = userHome
                    ? "Perderam em casa. " + score + ". Há toalhas no chão e ninguém se olha."
                    : "Perderam fora. " + score + ". No autocarro, dois jogadores ainda discutem o golo sofrido.";
            }
            career.Current = new CareerMoment
            {
                When = when,
                Kind = MomentKind.Note,
                Title = verdict + "  ·  Depois do apito",
                Body = tone + "\n\nO adjunto espera que sejas tu a puxar a primeira conversa. Ninguém te entrega um relatório de humor."
            };
            career.Notebook.Insert(0, "Jogo terminado. O que sentiste no balneário fica contigo.");
            career.Board.Insert(0, new CoachNote
            {
                Day = career.Calendar.WeekdayName,
                Hour = career.Calendar.ClockText,
                When = career.Calendar.CurrentDate,
                Text = tone
            });
            Newsroom.PublishMatch(career, homeName, awayName, homeGoals, awayGoals, userHome, motm, motmMark);
            if (career.Calendar.CurrentDate.Day % 4 == 0)
                Newsroom.PublishTransfer(career);
        }

        static void Pull(CareerState career)
        {
            if (career.Current != null && !string.IsNullOrEmpty(career.Current.BoardLine))
            {
                career.Board.Insert(0, new CoachNote
                {
                    Day = career.Calendar.WeekdayName,
                    Hour = career.Calendar.ClockText,
                    When = career.Calendar.CurrentDate,
                    Text = career.Current.BoardLine
                });
                career.Current.BoardLine = null;
            }

            QueueUntil(career, 1);
            if (career.Upcoming.Count == 0)
            {
                career.Current = null;
                return;
            }

            career.Current = career.Upcoming[0];
            career.Upcoming.RemoveAt(0);
            career.Calendar.SetInstant(career.Current.When);
            if (career.Current.Kind != MomentKind.Talk)
                career.Notebook.Insert(0, career.Calendar.ClockText + "  " + career.Current.Title);
        }

        static void QueueUntil(CareerState career, int minimum)
        {
            var guard = 0;
            while (career.Upcoming.Count < minimum && guard < 40)
            {
                guard++;
                var from = career.Upcoming.Count == 0
                    ? career.Calendar.CurrentDate
                    : career.Upcoming[career.Upcoming.Count - 1].When.AddHours(1);
                var day = from.Date;
                var added = AddDay(career, day, from);
                if (added == 0)
                    day = day.AddDays(1);
                if (added == 0)
                    AddDay(career, day, day.AddHours(8));
            }

            career.Upcoming.Sort((a, b) => a.When.CompareTo(b.When));
        }

        static int AddDay(CareerState career, DateTime day, DateTime notBefore)
        {
            var before = career.Upcoming.Count;
            var match = MatchOn(career, day);
            TryAdd(career, notBefore, day.AddHours(9), "Chegada", "chegada");
            TryAdd(career, notBefore, day.AddHours(11), "Treino", "treino");
            if (match == null)
                TryAdd(career, notBefore, day.AddHours(16), "Tarde no clube", "tarde");
            else
            {
                var kick = day.AddHours(16);
                if (kick > notBefore)
                {
                    career.Upcoming.Add(new CareerMoment
                    {
                        When = kick,
                        Title = "Hora do jogo",
                        Body = MomentScript.MatchCall(career, match),
                        Kind = MomentKind.Match,
                        Fixture = match,
                        BoardLine = MomentScript.MatchBoard(career, match)
                    });
                }
                Newsroom.PublishPreview(career, match);
            }

            if ((day.Day + career.Club.Name.Length) % 2 == 0)
                TryTalk(career, notBefore, day.AddHours(13));
            else
                TryAdd(career, notBefore, day.AddHours(18), "No clube", "clube");
            return career.Upcoming.Count - before;
        }

        static void TryAdd(CareerState career, DateTime notBefore, DateTime when, string title, string slot)
        {
            if (when <= notBefore)
                return;
            career.Upcoming.Add(new CareerMoment
            {
                When = when,
                Title = title,
                Body = MomentScript.Compose(career, when, slot, out var board, out var playerId),
                Kind = MomentKind.Note,
                PlayerId = playerId,
                BoardLine = board
            });
        }

        static void TryTalk(CareerState career, DateTime notBefore, DateTime when)
        {
            if (when <= notBefore)
                return;
            var player = PickPlayer(career, when);
            if (player == null)
                return;
            career.Upcoming.Add(BuildTalk(career, player, when));
        }

        static CareerMoment BuildTalk(CareerState career, Player player, DateTime when)
        {
            var other = Other(career, player);
            var seed = player.FullName.Length * 17 + when.DayOfYear + career.Club.DressingRoomTension + career.Board.Count;
            var scene = seed % 7;
            string opener;
            string[] replies;
            string[] follow;
            int[] morale;
            int[] fatigue;
            if (player.Injury != null && player.Injury.IsInjured)
                scene = 3;
            else if (player.Fatigue >= 72)
                scene = 0;
            else if (player.Morale < 38)
                scene = 2;

            switch (scene)
            {
                case 0:
                    opener = player.FullName + " pára a meio do exercício e apoia as mãos nos joelhos.\n\n\"As pernas não aguentam mais este bloco. Se continuar, saio a coxear.\"";
                    replies = new[]
                    {
                        "\"Sai. O preparador vê-te agora e o último exercício fica feito sem ti.\"",
                        "\"Faz mais um sprint. Se for teatro, nota-se. Se for verdade, paras a seguir.\"",
                        "\"O grupo não pára por uma pessoa. Volta para a fila.\""
                    };
                    follow = new[]
                    {
                        "Sai sem discutir. No banco, alonga a coxa e não volta a pedir.",
                        "Faz o sprint curto. Não reclama. Também não te olha.",
                        "Volta para a fila. O resto do grupo abranda sem tu mandares."
                    };
                    morale = new[] { 6, -4, -8 };
                    fatigue = new[] { -14, 8, 4 };
                    break;
                case 1:
                    opener = player.FullName + " aponta para " + other.FullName + ", à frente do grupo.\n\n\"No balneário já não se ouve o capitão. Hoje, " + other.FullName + " e outro discutiram à frente de toda a gente. Alguém tem de pôr ordem.\"";
                    replies = new[]
                    {
                        "\"Chamo o capitão. Isto resolve-se aqui, à frente do grupo, e acaba hoje.\"",
                        "\"O exercício muda. A discussão fica para depois, se ainda existir.\"",
                        "\"Fico do lado de " + other.FullName + ". O outro cala-se e volta ao lugar.\""
                    };
                    follow = new[]
                    {
                        "O capitão fala baixo. A discussão acaba, mas o vestiário fica mais tenso.",
                        "Ninguém insiste. O exercício seguinte é feito em silêncio.",
                        other.FullName + " agradece com a cabeça. O outro vira as costas."
                    };
                    morale = new[] { 3, -2, -9 };
                    fatigue = new[] { 0, -2, 2 };
                    break;
                case 2:
                    opener = player.FullName + " espera-te à porta do balneário.\n\n\"Não jogo. Se a ideia é continuar assim, diz-me de uma vez. Não me deixes a adivinhar até sábado.\"";
                    replies = new[]
                    {
                        "\"Contas para o próximo jogo. O lugar é teu, se o corpo responder.\"",
                        "\"A decisão vê-se no treino, não neste corredor. Mostra-me hoje.\"",
                        "\"Entra. Não tenho resposta para ti agora.\""
                    };
                    follow = new[]
                    {
                        "Assente, quase aliviado. O resto da tarde treina como se o lugar já fosse dele.",
                        "Fica à espera de mais. Não recebe. Volta na mesma.",
                        "Entra, senta-se e não volta a tocar na bola."
                    };
                    morale = new[] { 8, 1, -11 };
                    fatigue = new[] { -4, 0, 2 };
                    break;
                case 3:
                    opener = player.FullName + " senta-se no relvado e segura a coxa.\n\n\"Não é cansaço. Qualquer coisa cedeu no último corte. Não consigo apoiar o pé.\"";
                    replies = new[]
                    {
                        "\"Paro o treino. O fisioterapeuta vê-te já.\"",
                        "\"Acaba este exercício e falamos a seguir.\"",
                        "\"É dor de esforço. Continua e diz-me se piorar.\""
                    };
                    follow = new[]
                    {
                        "O fisioterapeuta tira-o. Fica de fora dos próximos dias.",
                        "Acaba a coxear. À noite a coxa está dura.",
                        "Continua. A meio do exercício já não apoia o pé."
                    };
                    morale = new[] { 2, -3, -8 };
                    fatigue = new[] { -6, 6, 10 };
                    break;
                case 4:
                    opener = "O " + career.Club.Tactics.Formation + " está no quadro. " + player.FullName + " aponta para o seu lado.\n\n\"A instrução que tenho é " + DutyOf(player) + ". Se me deixares jogar assim, o lado abre-se. Se me prenderes à linha, o jogo morre.\"";
                    replies = new[]
                    {
                        "\"Cumpre a instrução que já está no quadro. É essa que o grupo treinou.\"",
                        "\"Inverte. Se cortas, ficas aberto. Se ficas aberto, cortas.\"",
                        "\"Repete o mesmo lance. A instrução não muda hoje.\""
                    };
                    follow = new[]
                    {
                        "Treina o lance duas vezes e o lado percebe o que queres.",
                        "Muda o movimento. No primeiro minuto engana-se. No segundo já não.",
                        "O lance repete-se igual. Ele cumpre, sem acreditar."
                    };
                    morale = new[] { 5, 1, -4 };
                    fatigue = new[] { 2, 3, 4 };
                    break;
                case 5:
                    opener = player.FullName + " e " + other.FullName + " empurram-se junto à garrafa de água.\n\n" + player.FullName + ": \"Isto vem de ontem. Ele fechou-me o espaço no último exercício e ainda acha que foi falta minha.\"";
                    replies = new[]
                    {
                        "Separas os dois e falas com cada um, longe do grupo.",
                        "\"Resolvam isso entre vocês. Eu tenho um treino para orientar.\"",
                        "\"Os dois fazem uma série extra. A discussão acabou.\""
                    };
                    follow = new[]
                    {
                        "Separam-se. Não se falam, mas o treino continua.",
                        "Afastam-se cinco minutos depois, ainda zangados.",
                        "Fazem a série. O resto do grupo olha para ti, não para eles."
                    };
                    morale = new[] { 2, -3, -6 };
                    fatigue = new[] { 0, 0, 8 };
                    break;
                default:
                    opener = "Antes do jogo, " + player.FullName + " pede-te um minuto no corredor.\n\n\"Não me chega ouvir a formação. Quero saber se conto contigo quando a coisa apertar.\"";
                    replies = new[]
                    {
                        "\"Contas. Quando apertar, a bola passa por ti.\"",
                        "\"Conta quem estiver melhor às três da tarde. Isso inclui-te, se fores tu.\"",
                        "Não respondes. Entras para a sala e deixas a pergunta no corredor."
                    };
                    follow = new[]
                    {
                        "Sai da conversa direito. No aquecimento é o primeiro a falar.",
                        "Encolhe os ombros. Vai aquecer na mesma.",
                        "Fica no corredor mais um segundo. Depois entra atrás de ti."
                    };
                    morale = new[] { 7, 0, -5 };
                    fatigue = new[] { -2, 0, 1 };
                    break;
            }

            var moment = new CareerMoment
            {
                When = when,
                Title = "Conversa · " + player.FullName,
                Body = opener,
                Kind = MomentKind.Talk,
                PlayerId = player.Id,
                Replies = replies,
                FollowUps = follow,
                MoraleDelta = morale,
                FatigueDelta = fatigue,
                BoardLine = player.FullName + " procurou-te antes de voltares ao relvado."
            };
            if (scene == 3)
                moment.BoardLine = player.FullName + " queixou-se da coxa no treino.";
            return moment;
        }

        static string DutyOf(Player player)
        {
            return string.IsNullOrEmpty(player.Duty) ? "sem instrução definida" : player.Duty;
        }

        static string Trim(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Length > 90 ? value.Substring(0, 90) : value;
        }

        static Player Other(CareerState career, Player player)
        {
            foreach (var id in career.Club.PlayerIds)
            {
                var other = career.World.Players[id];
                if (other.Id != player.Id)
                    return other;
            }

            return player;
        }

        public static string ClueMood(Player player)
        {
            if (player.Morale < 35)
                return player.FullName + " treina no limite do grupo. Quando alguém fala, responde sem virar a cabeça.";
            if (player.Morale < 50)
                return player.FullName + " faz o que lhe pedem e desaparece assim que o exercício acaba.";
            if (player.Morale > 75)
                return player.FullName + " é quem junta a bola e chama os outros pelo nome.";
            return player.FullName + " está presente, sem liderar nem fugir.";
        }

        static Player PickPlayer(CareerState career, DateTime when)
        {
            var players = Squad(career);
            if (players.Count == 0)
                return null;
            return players[(when.Day + career.Board.Count + career.Club.DressingRoomTension) % players.Count];
        }

        static List<Player> Squad(CareerState career)
        {
            var list = new List<Player>();
            foreach (var id in career.Club.PlayerIds)
                list.Add(career.World.Players[id]);
            return list;
        }

        static Fixture MatchOn(CareerState career, DateTime day)
        {
            foreach (var fixture in career.Fixtures)
            {
                if (!fixture.Played && fixture.Involves(career.Coach.ClubId) && fixture.Date.Date == day.Date)
                    return fixture;
            }

            return null;
        }

        static int Clamp(int value)
        {
            if (value < 1) return 1;
            if (value > 99) return 99;
            return value;
        }
    }
}
