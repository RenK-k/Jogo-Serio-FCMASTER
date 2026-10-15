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
    }

    public static class CareerMoments
    {
        public static void Begin(CareerState career)
        {
            career.Upcoming.Clear();
            career.Current = null;
            QueueUntil(career, 1);
            Pull(career);
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
            career.Notebook.Insert(0, career.Calendar.ClockText + "  " + player.FullName + " — conversa no centro de treinos.");
        }

        public static void AfterMatch(CareerState career, int homeGoals, int awayGoals, bool userHome)
        {
            career.Current = null;
            career.Upcoming.Clear();
            var when = career.Calendar.CurrentDate.Date.AddHours(19);
            if (when < career.Calendar.CurrentDate)
                when = career.Calendar.CurrentDate.AddHours(2);
            career.Calendar.SetInstant(when);
            var tone = userHome
                ? (homeGoals > awayGoals ? "O balneário fala baixo, mas há sorrisos contidos." : homeGoals == awayGoals ? "Ninguém grita. Ninguém festeja." : "Há toalhas no chão e ninguém se olha.")
                : (awayGoals > homeGoals ? "No autocarro, dois jogadores ainda discutem o golo sofrido." : "A viagem de regresso é longa e quase muda.");
            career.Current = new CareerMoment
            {
                When = when,
                Kind = MomentKind.Note,
                Title = "Depois do apito",
                Body = tone + "\n\nO adjunto espera que sejas tu a puxar a primeira conversa. Ninguém te entrega um relatório de humor."
            };
            career.Notebook.Insert(0, "Jogo terminado. O que sentiste no balneário fica contigo.");
        }

        static void Pull(CareerState career)
        {
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
            TryAdd(career, notBefore, day.AddHours(9), "Chegada", Arrival(career), MomentKind.Note, null, null);
            TryAdd(career, notBefore, day.AddHours(11), "Treino", TrainingClue(career), MomentKind.Note, null, null);
            if (match == null)
                TryAdd(career, notBefore, day.AddHours(16), "Tarde no clube", Afternoon(career), MomentKind.Note, null, null);
            else
            {
                career.Upcoming.Add(new CareerMoment
                {
                    When = day.AddHours(16),
                    Title = "Hora do jogo",
                    Body = "O relvado está aberto. O que viste nas horas anteriores é tudo o que tens. Ninguém te mostra cansaço numa barra.",
                    Kind = MomentKind.Match,
                    Fixture = match
                });
            }

            if ((day.Day + career.Club.Name.Length) % 2 == 0)
                TryTalk(career, notBefore, day.AddHours(13));
            else
                TryAdd(career, notBefore, day.AddHours(18), "Corredor", StaffClue(career), MomentKind.Note, null, null);

            return career.Upcoming.Count - before;
        }

        static void TryAdd(CareerState career, DateTime notBefore, DateTime when, string title, string body, MomentKind kind, string playerId, Fixture fixture)
        {
            if (when < notBefore)
                return;
            career.Upcoming.Add(new CareerMoment
            {
                When = when,
                Title = title,
                Body = body,
                Kind = kind,
                PlayerId = playerId,
                Fixture = fixture
            });
        }

        static void TryTalk(CareerState career, DateTime notBefore, DateTime when)
        {
            if (when < notBefore)
                return;
            var player = PickPlayer(career, when);
            if (player == null)
                return;
            career.Upcoming.Add(BuildTalk(player, when));
        }

        static CareerMoment BuildTalk(Player player, DateTime when)
        {
            var tired = player.Fatigue >= 68;
            var low = player.Morale < 42;
            var high = player.Morale > 74;
            string opener;
            if (tired && low)
                opener = player.FullName + " encosta-se à parede do corredor. \"Se me meteres hoje outra vez a fazer o mesmo, não sei se as pernas aguentam. Não é desculpa. É o que eu sinto.\"";
            else if (tired)
                opener = player.FullName + " bebe água devagar. \"O corpo está pesado. Não me peças para te dizer um número. Olha para mim no último exercício.\"";
            else if (low)
                opener = player.FullName + " evita o teu olhar. \"No balneário falam de mim como se eu já não contasse. Queria ouvir isso de ti, não dos outros.\"";
            else if (high)
                opener = player.FullName + " procura-te de propósito. \"Estou bem. Se precisares de alguém para puxar o grupo esta tarde, eu fico.\"";
            else
                opener = player.FullName + " fala baixo, ao pé da porta. \"Não tenho nada de especial para dizer. Só queria saber se me viste no treino.\"";

            return new CareerMoment
            {
                When = when,
                Title = "Conversa · " + player.FullName,
                Body = opener,
                Kind = MomentKind.Talk,
                PlayerId = player.Id,
                Replies = new[]
                {
                    "Ouve sem interromper e tira-o do exercício mais duro.",
                    "Diz que conta contigo, mas que o grupo precisa dele na mesma.",
                    "Cortas a conversa e mandas-o voltar ao trabalho."
                },
                FollowUps = new[]
                {
                    "Ele assente, quase aliviado. No exercício seguinte já não arrasta os pés da mesma maneira.",
                    "Fica em silêncio um segundo. Depois volta para o meio dos colegas sem pedir mais nada.",
                    "A mandíbula fecha-se. Não discute. Também não volta a olhar para ti até ao fim da tarde."
                },
                MoraleDelta = new[] { low ? 8 : 4, 2, -10 },
                FatigueDelta = new[] { -12, -4, 6 }
            };
        }

        static string Arrival(CareerState career)
        {
            return "Passas o portão do centro de treinos. " + career.Club.DressingRoomAtmosphere +
                   " Ninguém te entrega uma folha com estados de espírito. Tens de ouvir o que dizem e o que evitam dizer.";
        }

        static string TrainingClue(CareerState career)
        {
            var players = Squad(career);
            Player heaviest = null;
            Player quietest = null;
            foreach (var player in players)
            {
                if (heaviest == null || player.Fatigue > heaviest.Fatigue)
                    heaviest = player;
                if (quietest == null || player.Morale < quietest.Morale)
                    quietest = player;
            }

            var lines = "O adjunto não te dá percentagens. Aponta com o queixo.\n\n";
            if (heaviest != null)
                lines += ClueBody(heaviest) + "\n";
            if (quietest != null && quietest != heaviest)
                lines += ClueMood(quietest);
            return lines;
        }

        static string Afternoon(CareerState career)
        {
            return career.Club.PublicTeamState + "\n\nPassam as horas de almoço. Um grupo fica na mesa, outro sai logo. Repara em quem não se senta com o capitão.";
        }

        static string StaffClue(CareerState career)
        {
            var physio = "O fisioterapeuta";
            foreach (var member in career.Club.Staff)
            {
                if (member.Role == StaffRole.Physiotherapist)
                    physio = member.FullName;
            }

            return physio + " cruza-se contigo no corredor e baixa a voz. \"Há quem esteja a compensar com o corpo o que não diz na boca. Não vou pôr nomes num papel. Se quiseres, chama-os um a um.\"";
        }

        public static string ClueBody(Player player)
        {
            if (player.Fatigue >= 75)
                return player.FullName + " pede para apertar os atacadores duas vezes e chega sempre um passo atrasado à fila.";
            if (player.Fatigue >= 55)
                return player.FullName + " cumpre, mas o último sprint fica curto. Não se queixa. Também não pede para repetir.";
            return player.FullName + " muda de direção sem perder o pé. Não parece estar a poupar-se.";
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
            return players[when.Day % players.Count];
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
