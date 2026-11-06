using System;
using System.Collections.Generic;
using CoachMaster.Domain;
using CoachMaster.MatchPlay;
using CoachMaster.UI;

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
        public int[] FormDelta;
        public int[] SquadMoraleDelta;
        public int[] DressingRoomTensionDelta;
        public int[] InjuryDays;
        public int[] UnavailableDays;
        public bool[] ToggleDuty;
        public string IncidentType;
        public string SecondaryPlayerId;
        public int[] SecondaryMoraleDelta;
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
            if (career.Current != null && !string.IsNullOrEmpty(career.Current.IncidentType) && !career.Current.Answered)
            {
                if (career.Current.Kind == MomentKind.Talk)
                    career.Current.Kind = MomentKind.Note;
                Answer(career, 0);
            }
            if (career.Current != null && career.Current.Kind == MomentKind.Match && !career.Current.Fixture.Played)
                return;

            SeasonSchedule.NoteInternationals(career);
            if (SeasonSchedule.DueToClose(career))
            {
                SeasonSchedule.ForceClose(career);
                Begin(career);
                return;
            }

            if (SeasonSchedule.TryRoll(career))
            {
                Begin(career);
                return;
            }

            Pull(career);
        }

        public static bool SkipConversationsForCurrentDay(CareerState career, string note)
        {
            if (career == null || career.Current == null || career.Current.Kind != MomentKind.Talk || career.Current.Answered)
                return false;

            var day = career.Current.When.Date;
            career.Current.Answered = true;
            for (var i = career.Upcoming.Count - 1; i >= 0; i--)
            {
                var moment = career.Upcoming[i];
                if (moment != null && moment.Kind == MomentKind.Talk && moment.When.Date == day)
                    career.Upcoming.RemoveAt(i);
            }

            if (!string.IsNullOrEmpty(note))
                career.Notebook.Insert(0, career.Calendar.ClockText + "  " + note);
            return true;
        }

        public static bool CanSkipCurrentDay(CareerState career)
        {
            if (career == null || career.Current == null || career.Current.Kind == MomentKind.Match)
                return false;
            var day = career.Current.When.Date;
            for (var i = 0; i < career.Upcoming.Count; i++)
            {
                var moment = career.Upcoming[i];
                if (moment != null && moment.Kind == MomentKind.Match && moment.Fixture != null
                    && !moment.Fixture.Played && moment.When.Date == day)
                    return false;
            }
            foreach (var fixture in career.Fixtures)
                if (fixture != null && !fixture.Played && !fixture.Walkover
                    && (fixture.Involves(career.Coach.ClubId) || CareerOnline.OtherHuman(career, fixture)) && fixture.Date.Date == day)
                    return false;
            return true;
        }

        public static bool SkipCurrentDay(CareerState career, string note)
        {
            if (!CanSkipCurrentDay(career)) return false;
            var skippedDay = career.Current.When.Date;
            if (!string.IsNullOrEmpty(note))
                career.Notebook.Insert(0, career.Calendar.ClockText + "  " + note);
            career.Current = null;
            for (var i = career.Upcoming.Count - 1; i >= 0; i--)
                if (career.Upcoming[i] != null && career.Upcoming[i].When.Date == skippedDay)
                    career.Upcoming.RemoveAt(i);
            career.Calendar.SetInstant(skippedDay.AddDays(1).AddHours(8));
            return true;
        }

        public static void Answer(CareerState career, int choice)
        {
            var moment = career.Current;
            var automaticIncident = moment != null && moment.Kind != MomentKind.Talk && !string.IsNullOrEmpty(moment.IncidentType);
            if (moment == null || (moment.Kind != MomentKind.Talk && !automaticIncident) || moment.Answered)
                return;
            if (moment.Replies == null || choice < 0 || choice >= moment.Replies.Length)
                return;
            if (string.IsNullOrEmpty(moment.PlayerId) || !career.World.Players.TryGetValue(moment.PlayerId, out var player))
                return;

            moment.Answered = true;
            var effect = SpokenEffect(player, choice, moment.Replies.Length);
            var morale = At(moment.MoraleDelta, effect);
            var fatigue = At(moment.FatigueDelta, effect);
            var form = At(moment.FormDelta, effect);
            var squadMorale = At(moment.SquadMoraleDelta, effect);
            var tension = At(moment.DressingRoomTensionDelta, effect);
            player.Morale = Clamp(player.Morale + morale);
            player.Fatigue = Clamp(player.Fatigue + fatigue);
            player.Form = Clamp(player.Form + form);

            if (squadMorale != 0)
            {
                foreach (var id in career.Club.PlayerIds)
                {
                    if (id == player.Id || id == moment.SecondaryPlayerId)
                        continue;
                    if (career.World.Players.TryGetValue(id, out var teammate))
                        teammate.Morale = Clamp(teammate.Morale + squadMorale);
                }
            }

            if (tension != 0)
                career.Club.DressingRoomTension = ClampRange(career.Club.DressingRoomTension + tension, 0, 100);

            if (!string.IsNullOrEmpty(moment.SecondaryPlayerId) && career.World.Players.TryGetValue(moment.SecondaryPlayerId, out var secondary))
            {
                var secondaryMorale = At(moment.SecondaryMoraleDelta, effect);
                secondary.Morale = Clamp(secondary.Morale + secondaryMorale);
            }

            var injuryDays = At(moment.InjuryDays, effect);
            if (injuryDays > 0)
            {
                if (player.Injury == null)
                    player.Injury = new InjuryState();
                player.Injury.IsInjured = true;
                player.Injury.Description = "Lesão muscular";
                player.Injury.DaysRemaining = injuryDays;
            }

            var unavailableDays = At(moment.UnavailableDays, effect);
            if (unavailableDays > 0)
            {
                var unavailableUntil = career.Calendar.CurrentDate.Date.AddDays(unavailableDays - 1).Ticks;
                if (moment.IncidentType == "betting" && choice == 1)
                {
                    var nextFixture = career.NextUserFixture();
                    if (nextFixture != null)
                        unavailableUntil = Math.Max(unavailableUntil, nextFixture.Date.Date.Ticks);
                }
                player.UnavailableUntilTicks = unavailableUntil;
                player.AbsenceKind = moment.IncidentType == "betting" ? "ban" : "pessoal";
            }

            var dutyChanged = At(moment.ToggleDuty, effect);
            if (dutyChanged)
            {
                var options = IndividualOrders.ForPosition(player.Position);
                if (options != null && options.Length > 0)
                {
                    var current = Array.IndexOf(options, player.Duty);
                    var next = current < 0 ? 0 : (current + 1) % options.Length;
                    player.Duty = options[next];
                }
                else
                    dutyChanged = false;
            }

            var english = LocalizationManager.Current == GameLanguage.English;
            var followUp = automaticIncident
                ? IncidentPlayerResponse(player, moment.IncidentType, english)
                : moment.FollowUps != null && choice < moment.FollowUps.Length ? moment.FollowUps[choice] : "";
            if (effect != choice && !automaticIncident)
                followUp += effect < choice
                    ? " Com este jogador, a frase mais dura foi a que fez sentido."
                    : " Com este jogador, essa frase não colou.";
            var impact = new List<string>();
            AddImpact(impact, "moral", morale);
            AddImpact(impact, "forma", form);
            AddImpact(impact, "fadiga", fatigue);
            AddImpact(impact, "moral do grupo", squadMorale);
            if (tension != 0)
                impact.Add("tensão do balneário " + Signed(tension));
            if (injuryDays > 0)
                impact.Add("lesão: " + injuryDays + " dias fora");
            if (unavailableDays > 0)
                impact.Add(moment.IncidentType == "betting" && choice == 1
                    ? "afastamento do próximo jogo"
                    : "dispensa: " + unavailableDays + (unavailableDays == 1 ? " dia" : " dias"));
            if (dutyChanged)
                impact.Add("instrução: " + player.Duty);
            if (automaticIncident)
                moment.Body += "\n\n" + followUp;
            else
                moment.Body += "\n\nTu: " + moment.Replies[choice] + "\n\n" + followUp;
            if (impact.Count > 0)
                moment.Body += "\n\nImpacto: " + string.Join(" · ", impact) + ".";
            if (!automaticIncident)
            {
                career.Notebook.Insert(0, career.Calendar.ClockText + "  " + player.FullName + " — conversa no centro de treinos.");
                var trustDelta = CareerRelationships.ApplyConversation(career, player, morale, form, tension, injuryDays);
                CareerMemories.RecordConversation(career, moment, player, choice, morale, form, tension, trustDelta, injuryDays);
                CareerNotificationQueue.Enqueue(english ? "YOUR CHOICE MATTERS" : "A TUA ESCOLHA FICA",
                    english ? "Trust with " + player.FullName + ": " + CareerRelationships.TrustOf(career, player.Id) + "/100."
                        : "Confiança de " + player.FullName + ": " + CareerRelationships.TrustOf(career, player.Id) + "/100.");
            }

            if (!string.IsNullOrEmpty(moment.IncidentType))
            {
                var marker = "personal-event|" + moment.When.Date.Ticks;
                if (!career.Notebook.Contains(marker)) career.Notebook.Add(marker);
                var playerReaction = IncidentPlayerResponse(player, moment.IncidentType, english);
                var teamReaction = IncidentTeamResponseForNews(career, player, moment.IncidentType, english);
                var headline = Newsroom.PublishPersonalIncident(career, moment, player);
                career.Board.Insert(0, new CoachNote
                {
                    Day = career.Calendar.WeekdayName,
                    Hour = career.Calendar.ClockText,
                    When = career.Calendar.CurrentDate,
                    Text = (english
                        ? "✉ " + player.FullName + " responds to the incident: " + playerReaction + " " + teamReaction
                        : "✉ " + headline + " · " + playerReaction + " " + teamReaction)
                        + " [[player-incident:" + player.Id + "|" + moment.IncidentType + "|" + moment.When.Ticks + "]]"
                });
                while (career.Board.Count > 40) career.Board.RemoveAt(career.Board.Count - 1);
            }
        }

        public static string IncidentPlayerResponse(Player player, string incidentType, bool english)
        {
            var personality = player == null ? PersonalityTrait.Reserved : player.Personality;
            switch ((incidentType ?? "").ToLowerInvariant())
            {
                case "betting":
                    if (personality == PersonalityTrait.Professional || personality == PersonalityTrait.Leader)
                        return english ? "I made a serious mistake. I understand the rules and will cooperate fully. I'm sorry I put the squad in this position."
                            : "Cometi um erro sério. Percebo as regras e vou cooperar totalmente. Lamento ter colocado o grupo nesta posição.";
                    if (personality == PersonalityTrait.Outspoken || personality == PersonalityTrait.Temperamental)
                        return english ? "I shouldn't have placed that bet. I'll answer the questions honestly, and I know the group deserves better from me."
                            : "Não devia ter feito aquela aposta. Vou responder com honestidade e sei que o grupo merece mais de mim.";
                    return english ? "I made a bad decision and I'm sorry. I'll cooperate with the review and speak to the team myself."
                        : "Tomei uma má decisão e peço desculpa. Vou cooperar com a investigação e falar com a equipa.";
                case "drunk":
                    return personality == PersonalityTrait.Outspoken
                        ? (english ? "I let everyone down by turning up like that. No excuses; I'll put it right." : "Desiludi toda a gente ao aparecer assim. Não há desculpas; vou corrigir a situação.")
                        : (english ? "I know I let the group down. I'm sorry, and I'll make sure it doesn't affect the next session." : "Sei que desiludi o grupo. Peço desculpa e vou garantir que isto não afeta o próximo treino.");
                case "bereavement":
                    return english ? "Thank you for giving me space to be with my family. I appreciate the support from everyone at the club."
                        : "Obrigado por me deixarem estar com a minha família. Agradeço o apoio de todos no clube.";
                case "viral":
                    return personality == PersonalityTrait.Outspoken || personality == PersonalityTrait.Temperamental
                        ? (english ? "The 2-2-6 was a joke, not my tactical masterplan. At least the lads got a laugh out of it." : "O 2-2-6 era uma piada, não o meu plano tático. Ao menos os rapazes riram-se.")
                        : (english ? "I didn't expect the team sheet to travel that far. The lads have already promised to make me explain the shape on the pitch." : "Não esperava que a equipa desenhada circulasse tanto. Os rapazes já disseram que vou ter de explicar a tática no campo.");
                case "family":
                    return english ? "It's a special day for my family. Thanks for helping me balance it with the team's plans."
                        : "É um dia especial para a minha família. Obrigado por me ajudarem a conciliá-lo com os planos da equipa.";
                case "charity":
                    return english ? "The kids were brilliant. I'm glad the club let me spend time with them." : "As crianças foram incríveis. Fico contente por o clube me ter deixado passar tempo com elas.";
                default:
                    return english ? "I appreciate the club checking in. I'll keep you updated." : "Agradeço que o clube se preocupe. Vou mantendo-vos a par.";
            }
        }

        public static string IncidentTeamResponseForNews(CareerState career, Player player, string incidentType, bool english)
        {
            Player captain = null;
            if (career != null && career.Club != null)
                foreach (var id in career.Club.PlayerIds)
                    if (career.World.Players.TryGetValue(id, out var candidate) && candidate.IsCaptain)
                    {
                        captain = candidate;
                        break;
                    }
            var captainName = captain == null ? (english ? "The senior players" : "Os jogadores mais experientes") : captain.FullName;
            switch ((incidentType ?? "").ToLowerInvariant())
            {
                case "betting": return english
                    ? captainName + " want the facts established, but have asked the dressing room to avoid rumours until the review is complete."
                    : captainName + " querem que os factos sejam apurados e pediram ao balneário que evite rumores até a análise terminar.";
                case "drunk": return english
                    ? captainName + " told the group to move on after the apology and keep the focus on training."
                    : captainName + " pediu ao grupo que aceitasse o pedido de desculpa e mantivesse o foco no treino.";
                case "bereavement": return english
                    ? "The squad sent its support and agreed to dedicate the match to the player's family."
                    : "O plantel enviou o seu apoio e combinou dedicar o jogo à família do jogador.";
                case "viral": return english
                    ? "The dressing room shared the joke and then got back to preparing the real shape."
                    : "O balneário alinhou na brincadeira e depois voltou à preparação da formação verdadeira.";
                case "family": return english
                    ? "Teammates congratulated the family and adjusted the session around the player's plans."
                    : "Os colegas felicitaram a família e ajustaram o treino aos planos do jogador.";
                case "charity": return english
                    ? "Teammates praised the visit and asked the club to organise another one together."
                    : "Os colegas elogiaram a visita e pediram ao clube que organize outra em conjunto.";
                default: return english ? "The dressing room offered its support." : "O balneário mostrou o seu apoio.";
            }
        }

        static int SpokenEffect(Player player, int choice, int count)
        {
            if (player == null || count < 3 || choice < 0 || choice >= count)
                return choice;
            var blunt = choice == count - 1;
            var soft = choice == 0;
            var leads = player.HasTrait(PlayerTrait.Lideranca) || (player.Attributes != null && player.Attributes.Decisions >= 15);
            var sensitive = player.Age <= 21 || player.HasTrait(PlayerTrait.JogadorDeEquipa);
            if (blunt && leads && player.Morale <= 62)
                return 0;
            if (blunt && sensitive && (player.Attributes == null || player.Attributes.Composure <= 12))
                return count - 1;
            if (soft && leads && player.Morale >= 48)
                return 1;
            if (soft && sensitive)
                return 0;
            return choice;
        }

        static int At(int[] values, int index)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : 0;
        }

        static bool At(bool[] values, int index)
        {
            return values != null && index >= 0 && index < values.Length && values[index];
        }

        static string Signed(int value)
        {
            return value > 0 ? "+" + value : value.ToString();
        }

        static void AddImpact(List<string> impact, string label, int delta)
        {
            if (delta != 0)
                impact.Add(label + " " + Signed(delta));
        }

        public static void AfterMatch(CareerState career, int homeGoals, int awayGoals, bool userHome, string homeName, string awayName, string motm = null, string motmMark = null, MatchSession match = null)
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
            Newsroom.PublishMatch(career, homeName, awayName, homeGoals, awayGoals, userHome, motm, motmMark, match);
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
            if (!string.IsNullOrEmpty(career.Current.IncidentType) && !career.Current.Answered)
                Answer(career, 0);
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
            var match = MatchOn(career, when.Date);
            var moment = BuildPersonalIncident(career, player, when, match) ?? BuildTalk(career, player, when);
            if (moment != null)
                career.Upcoming.Add(moment);
        }

        static CareerMoment BuildPersonalIncident(CareerState career, Player player, DateTime when, Fixture match)
        {
            if (career == null || player == null || career.Notebook == null)
                return null;
            var date = when.Date;
            var dayKey = "personal-event|" + date.Ticks;
            if (career.Notebook.Contains(dayKey))
                return null;
            foreach (var line in career.Notebook)
            {
                if (line == null || !line.StartsWith("personal-event|", StringComparison.Ordinal)) continue;
                if (long.TryParse(line.Substring("personal-event|".Length), out var ticks)
                    && Math.Abs((date - new DateTime(ticks).Date).TotalDays) < 21)
                    return null;
            }

            var roll = StableHash(career.Club.Id + "|" + player.Id + "|" + date.Ticks + "|personal") % 10000;
            if (roll >= 160)
                return null;

            string type;
            if (match != null && roll < 8) type = "bereavement";
            else if (roll < 48) type = "betting";
            else if (roll < 83) type = "drunk";
            else if (roll < 116) type = "viral";
            else if (roll < 140) type = "family";
            else type = "charity";

            var name = player.FullName;
            var moment = new CareerMoment
            {
                When = when,
                Title = "Acontecimento pessoal · " + name,
                Kind = MomentKind.Note,
                PlayerId = player.Id,
                IncidentType = type,
                BoardLine = null
            };
            switch (type)
            {
                case "bereavement":
                    moment.Body = name + " recebe a notícia de que o pai faleceu. O funeral é hoje, dia do jogo. Encontra-te no corredor e pede apoio para estar com a família.";
                    moment.Replies = new[] { "Dou-lhe dispensa para estar com a família. O substituto entra no onze.", "Organizo apoio do clube e deixo-o decidir se consegue jogar.", "Peço-lhe que cumpra o compromisso do jogo. Tratamos do resto amanhã." };
                    moment.FollowUps = new[] { "Vai para junto da família. O grupo prepara-se para jogar por ele.", "A equipa oferece apoio e ele decide afastar-se do jogo.", "Fica com o grupo, mas a notícia pesa-lhe durante todo o dia." };
                    moment.MoraleDelta = new[] { 8, 5, -12 };
                    moment.FatigueDelta = new[] { -1, 0, 2 };
                    moment.FormDelta = new[] { -2, -2, -6 };
                    moment.SquadMoraleDelta = new[] { 2, 2, -2 };
                    moment.DressingRoomTensionDelta = new[] { -1, -1, 6 };
                    moment.InjuryDays = new[] { 0, 0, 0 };
                    moment.UnavailableDays = new[] { 1, 1, 0 };
                    break;
                case "betting":
                    moment.Body = name + " é identificado numa investigação interna por ter feito apostas em futebol. O clube ainda está a confirmar os factos e a imprensa já pede uma posição.";
                    moment.Replies = new[] { "Investigo em privado com o jogador e o responsável de integridade.", "Afasto-o do próximo jogo enquanto esclarecemos o caso.", "Digo que é assunto pessoal e não faço perguntas." };
                    moment.FollowUps = new[] { "Coopera com a investigação. O balneário percebe que o caso está a ser tratado.", "Aceita ficar de fora enquanto a situação é revista.", "A resposta aumenta a especulação e deixa o grupo desconfortável." };
                    moment.MoraleDelta = new[] { 1, -4, -8 };
                    moment.FatigueDelta = new[] { 0, -2, 0 };
                    moment.FormDelta = new[] { 1, -3, -5 };
                    moment.SquadMoraleDelta = new[] { 0, 1, -2 };
                    moment.DressingRoomTensionDelta = new[] { -1, 0, 5 };
                    moment.InjuryDays = new[] { 0, 0, 0 };
                    moment.UnavailableDays = new[] { 0, 1, 0 };
                    break;
                case "drunk":
                    moment.Body = name + " chega atrasado ao treino depois de uma noite longa. Está bem, mas o cansaço é visível e um vídeo à porta de um bar já circula nas redes.";
                    moment.Replies = new[] { "Mando-o para casa descansar e peço ao médico que o avalie.", "Aplico uma multa e deixo-o treinar com carga reduzida.", "Exijo que faça o treino completo para servir de exemplo." };
                    moment.FollowUps = new[] { "Descansa, aceita a avaliação e regressa ao grupo no dia seguinte.", "Cumpre a sessão leve. O balneário percebe o limite que definiste.", "Acaba o treino exausto e a equipa questiona a decisão." };
                    moment.MoraleDelta = new[] { 2, -3, -8 };
                    moment.FatigueDelta = new[] { -8, -3, 10 };
                    moment.FormDelta = new[] { -1, -2, -7 };
                    moment.SquadMoraleDelta = new[] { 0, 0, -2 };
                    moment.DressingRoomTensionDelta = new[] { -1, 1, 6 };
                    moment.InjuryDays = new[] { 0, 0, 3 };
                    moment.UnavailableDays = new[] { 0, 0, 0 };
                    break;
                case "viral":
                    moment.Body = name + " troca as folhas da palestra por uma escalação desenhada pelo roupeiro. O vídeo do capitão a tentar explicar o 2-2-6 tornou-se viral antes do pequeno-almoço.";
                    moment.Replies = new[] { "Rio-me com o grupo e depois confirmo o onze verdadeiro.", "Aproveito a atenção para promover o próximo jogo.", "Acabo com a brincadeira e imponho uma multa simbólica." };
                    moment.FollowUps = new[] { "O balneário descontrai. Até o roupeiro guarda uma cópia da escalação.", "O clube ganha atenção e o jogador aparece no vídeo oficial a pedir desculpa ao treinador.", "A brincadeira acaba. O capitão devolve-te o quadro, ainda a rir." };
                    moment.MoraleDelta = new[] { 6, 4, -4 };
                    moment.FatigueDelta = new[] { 0, 0, 0 };
                    moment.FormDelta = new[] { 2, 1, -1 };
                    moment.SquadMoraleDelta = new[] { 3, 1, -1 };
                    moment.DressingRoomTensionDelta = new[] { -2, 0, 2 };
                    moment.InjuryDays = new[] { 0, 0, 0 };
                    moment.UnavailableDays = new[] { 0, 0, 0 };
                    break;
                case "family":
                    moment.Body = name + " conta-te que acabou de ser pai. Está feliz, mas quase não dormiu e pede para ajustar a carga de hoje, véspera de jogo.";
                    moment.Replies = new[] { "Dou-lhe a manhã para a família e reduzo a carga.", "Ajusto o treino, mas mantenho-o disponível para o jogo.", "A vida pessoal fica à porta; a sessão é igual para todos." };
                    moment.FollowUps = new[] { "Sorri ao receber uma fotografia da família. O grupo prepara uma pequena surpresa.", "Treina com cuidado e agradece a flexibilidade.", "Cumpre o treino, embora a felicidade dê lugar à frustração." };
                    moment.MoraleDelta = new[] { 9, 6, -6 };
                    moment.FatigueDelta = new[] { -5, -2, 5 };
                    moment.FormDelta = new[] { 1, 1, -3 };
                    moment.SquadMoraleDelta = new[] { 2, 1, -1 };
                    moment.DressingRoomTensionDelta = new[] { -2, -1, 4 };
                    moment.InjuryDays = new[] { 0, 0, 0 };
                    moment.UnavailableDays = new[] { 0, 0, 0 };
                    break;
                default:
                    moment.Body = name + " organizou uma visita surpresa a uma associação local e levou equipamento para os miúdos. O adjunto pergunta se deve proteger o tempo de recuperação antes do próximo jogo.";
                    moment.Replies = new[] { "Apoio a iniciativa e ajusto o horário do treino.", "Agradeço o gesto e peço que regresse a tempo da preparação.", "Cancelo a visita; hoje a prioridade é só o futebol." };
                    moment.FollowUps = new[] { "A iniciativa aparece nas notícias locais e o grupo fica orgulhoso.", "A visita termina cedo. O jogador promete repetir na folga.", "Regressa ao treino, mas o balneário não percebe a decisão." };
                    moment.MoraleDelta = new[] { 7, 4, -5 };
                    moment.FatigueDelta = new[] { 1, 0, -1 };
                    moment.FormDelta = new[] { 2, 1, -2 };
                    moment.SquadMoraleDelta = new[] { 2, 1, -1 };
                    moment.DressingRoomTensionDelta = new[] { -2, -1, 4 };
                    moment.InjuryDays = new[] { 0, 0, 0 };
                    moment.UnavailableDays = new[] { 0, 0, 0 };
                    break;
            }
            return moment;
        }

        static int StableHash(string value)
        {
            unchecked
            {
                var hash = 23;
                for (var i = 0; i < value.Length; i++) hash = hash * 31 + value[i];
                return hash & 0x7fffffff;
            }
        }

        static CareerMoment BuildTalk(CareerState career, Player player, DateTime when)
        {
            var other = Other(career, player);
            var injured = player.Injury != null && player.Injury.IsInjured;
            var worn = player.Fatigue >= 72;
            var unhappy = player.Morale < 40;
            if (!injured && !worn && !unhappy)
                return null;
            var scene = injured ? 3 : worn ? 0 : 2;
            string opener;
            string[] replies;
            string[] follow;
            int[] morale;
            int[] fatigue;
            int[] form;
            int[] squadMorale;
            int[] tension;
            int[] injuryDays;
            int[] secondaryMorale = new int[3];
            bool[] toggleDuty = new bool[3];
            string secondaryPlayerId = null;
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
                    form = new[] { 2, 0, -2 };
                    squadMorale = new[] { 1, 0, -1 };
                    tension = new[] { -2, 1, 4 };
                    injuryDays = new[] { 0, 3, 7 };
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
                    form = new[] { 1, 0, -2 };
                    squadMorale = new[] { 2, -1, -2 };
                    tension = new[] { -6, 2, 8 };
                    injuryDays = new[] { 0, 0, 0 };
                    secondaryPlayerId = other.Id;
                    secondaryMorale = new[] { -1, 0, 5 };
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
                    form = new[] { 3, 1, -3 };
                    squadMorale = new[] { 1, 0, -1 };
                    tension = new[] { -2, 0, 5 };
                    injuryDays = new[] { 0, 0, 0 };
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
                    form = new[] { 0, -1, -3 };
                    squadMorale = new[] { 1, 0, -2 };
                    tension = new[] { -2, 2, 6 };
                    injuryDays = new[] { 5, 10, 18 };
                    break;
                case 4:
                    opener = "O " + career.Club.Tactics.Formation + " está no quadro. " + player.FullName + " aponta para o seu lado.\n\n\"A instrução que tenho é " + DutyOf(player) + ". Se me deixares jogar assim, o lado abre-se. Se me prenderes à linha, o jogo morre.\"";
                    replies = new[]
                    {
                        "\"Cumpre a instrução que já está no quadro. É essa que o grupo treinou.\"",
                        "\"Ajusta a instrução. Quero outro movimento deste lado.\"",
                        "\"Repete o mesmo lance. A instrução não muda hoje.\""
                    };
                    follow = new[]
                    {
                        "Treina o lance duas vezes e o lado percebe o que queres.",
                        "A instrução muda. No primeiro minuto engana-se. No segundo já não.",
                        "O lance repete-se igual. Ele cumpre, sem acreditar."
                    };
                    morale = new[] { 5, 1, -4 };
                    fatigue = new[] { 2, 3, 4 };
                    form = new[] { 1, 2, -2 };
                    squadMorale = new[] { 1, 0, -1 };
                    tension = new[] { -2, 0, 4 };
                    injuryDays = new[] { 0, 0, 0 };
                    toggleDuty = new[] { false, true, false };
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
                    form = new[] { 1, -1, -3 };
                    squadMorale = new[] { 1, -1, -3 };
                    tension = new[] { -8, 7, 13 };
                    injuryDays = new[] { 0, 0, 0 };
                    secondaryPlayerId = other.Id;
                    secondaryMorale = new[] { 3, -1, -4 };
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
                    form = new[] { 2, 0, -2 };
                    squadMorale = new[] { 2, 0, -1 };
                    tension = new[] { -3, 0, 4 };
                    injuryDays = new[] { 0, 0, 0 };
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
                FormDelta = form,
                SquadMoraleDelta = squadMorale,
                DressingRoomTensionDelta = tension,
                InjuryDays = injuryDays,
                ToggleDuty = toggleDuty,
                SecondaryPlayerId = secondaryPlayerId,
                SecondaryMoraleDelta = secondaryMorale,
                BoardLine = player.FullName + " procurou-te antes de voltares ao relvado."
            };
            if (scene == 3)
                moment.BoardLine = player.FullName + " queixou-se da coxa no treino.";
            var trust = CareerRelationships.TrustOf(career, player.Id);
            if (trust <= 32)
            {
                moment.Body = "A confiança entre vocês está baixa. " + moment.Body;
                if (morale.Length > 0)
                    morale[0] = Math.Max(-8, morale[0] - 2);
            }
            else if (trust >= 76)
            {
                moment.Body = "A confiança construída entre vocês nota-se na conversa. " + moment.Body;
                if (morale.Length > 0)
                    morale[0] = Math.Min(12, morale[0] + 1);
            }
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
                if (!fixture.Played && !fixture.Walkover && fixture.Involves(career.Coach.ClubId) && fixture.Date.Date == day.Date)
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

        static int ClampRange(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
