using System;
using System.Collections.Generic;
using CoachMaster.Domain;
using CoachMaster.MatchPlay;

namespace CoachMaster.Career
{
    public sealed class FootballMemory
    {
        public long WhenTicks;
        public int Importance;
        public string Kind;
        public string ClubId;
        public string CompetitionId;
        public string RelatedClubId;
        public string PlayerId;
        public string Title;
        public string Detail;
        public int Value;
    }

    public static class CareerMemories
    {
        const int MaxMemories = 120;

        public static FootballMemory Record(CareerState career, FootballMemory memory)
        {
            if (career == null || memory == null || memory.Importance < 1 || string.IsNullOrEmpty(memory.Title))
                return null;

            for (var i = 0; i < career.Memories.Count; i++)
            {
                var existing = career.Memories[i];
                if (existing.WhenTicks == memory.WhenTicks && existing.Kind == memory.Kind && existing.ClubId == memory.ClubId && existing.Title == memory.Title)
                    return existing;
            }

            career.Memories.Insert(0, memory);
            while (career.Memories.Count > MaxMemories)
                career.Memories.RemoveAt(career.Memories.Count - 1);
            return memory;
        }

        public static void RecordConversation(CareerState career, CareerMoment moment, Player player, int choice, int moraleDelta, int formDelta, int tensionDelta, int trustDelta, int injuryDays)
        {
            if (career == null || moment == null || player == null)
                return;
            if (Math.Abs(moraleDelta) < 5 && Math.Abs(formDelta) < 5 && Math.Abs(tensionDelta) < 8 && Math.Abs(trustDelta) < 3 && injuryDays < 12)
                return;

            var reply = moment.Replies != null && choice >= 0 && choice < moment.Replies.Length ? moment.Replies[choice] : "conversaste com o jogador";
            var detail = "Conversa privada com " + player.FullName + ". Resposta escolhida: “" + reply + "”.";
            if (moment.FollowUps != null && choice >= 0 && choice < moment.FollowUps.Length && !string.IsNullOrEmpty(moment.FollowUps[choice]))
                detail += " Reação do jogador: " + moment.FollowUps[choice];
            if (moraleDelta != 0 || formDelta != 0 || tensionDelta != 0)
                detail += " Efeito imediato: moral " + Signed(moraleDelta) + ", forma " + Signed(formDelta) + " e tensão do balneário " + Signed(tensionDelta) + ".";
            if (trustDelta != 0)
                detail += " Confiança no treinador " + Signed(trustDelta) + ".";

            Record(career, new FootballMemory
            {
                WhenTicks = career.Calendar.CurrentDate.Ticks,
                Importance = Math.Min(5, 2 + Math.Max(Math.Abs(moraleDelta), Math.Max(Math.Abs(formDelta), Math.Abs(tensionDelta))) / 6),
                Kind = "conversa",
                ClubId = career.Coach.ClubId,
                CompetitionId = career.Club.LeagueId,
                PlayerId = player.Id,
                Title = "Conversa importante com " + player.FullName,
                Detail = detail
            });

            if (injuryDays >= 12 && player.Injury != null && player.Injury.IsInjured)
            {
                Record(career, new FootballMemory
                {
                    WhenTicks = career.Calendar.CurrentDate.Ticks,
                    Importance = injuryDays >= 21 ? 5 : 4,
                    Kind = "lesao-grave",
                    ClubId = career.Coach.ClubId,
                    CompetitionId = career.Club.LeagueId,
                    PlayerId = player.Id,
                    Title = "Lesão afasta " + player.FullName,
                    Detail = player.FullName + " vai ficar de fora durante " + injuryDays + " dias após uma decisão num momento de carreira.",
                    Value = injuryDays
                });
            }
        }

        public static void RecordMatch(CareerState career, MatchSession match, bool userHome, string playerOfTheMatchId)
        {
            if (career == null || match == null || match.Fixture == null || match.Home == null || match.Away == null)
                return;

            var club = career.Club;
            var opponent = userHome ? match.Away : match.Home;
            var userGoals = userHome ? match.HomeGoals : match.AwayGoals;
            var opponentGoals = userHome ? match.AwayGoals : match.HomeGoals;
            var won = userGoals > opponentGoals;
            var lost = userGoals < opponentGoals;
            var margin = Math.Abs(userGoals - opponentGoals);
            var totalGoals = userGoals + opponentGoals;
            var lateWinningGoal = false;
            foreach (var note in match.Notes)
            {
                if (won && margin == 1 && note != null && note.Kind == "goal" && note.Minute >= 80)
                {
                    lateWinningGoal = true;
                    break;
                }
            }

            var isCup = !string.IsNullOrEmpty(match.Fixture.LeagueId) &&
                (match.Fixture.LeagueId.IndexOf("cup", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 match.Fixture.LeagueId.IndexOf("campeoes", StringComparison.OrdinalIgnoreCase) >= 0);
            var upset = won && opponent.Reputation - club.Reputation >= 12;
            var decisiveCupResult = isCup && (won || (lost && margin >= 2));
            var memorable = (won && (lateWinningGoal || upset || margin >= 3 || (totalGoals >= 5 && margin >= 2))) ||
                            (lost && (decisiveCupResult || (club.Reputation - opponent.Reputation >= 15 && margin >= 2)));

            var approvalBefore = career.SupporterApproval;
            var reaction = SupporterReactions.ApplyResult(career, match.Fixture, match.HomeGoals, match.AwayGoals);
            var playerMoments = RecordPlayerMoments(career, match, userGoals, opponentGoals, opponent);
            var supporterMemory = RecordSupporterMilestone(career, match, approvalBefore, reaction);
            if (supporterMemory != null)
                playerMoments.Insert(0, supporterMemory);
            playerMoments.Sort((a, b) => b.Importance.CompareTo(a.Importance));
            FootballMemory memory = null;
            if (memorable)
            {
                var date = match.Fixture.Date.ToString("d MMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("pt-PT"));
                var result = match.Home.ShortName + " " + match.HomeGoals + "–" + match.AwayGoals + " " + match.Away.ShortName;
                var title = lateWinningGoal
                    ? "Vitória decidida nos minutos finais"
                    : upset
                        ? "Vitória contra as expectativas"
                        : decisiveCupResult
                            ? (won ? "Noite de taça: vitória importante" : "Eliminação que fica na memória")
                            : won
                                ? "Grande vitória frente a " + opponent.ShortName
                                : "Derrota pesada frente a " + opponent.ShortName;
                var detail = date + " · " + result + ". " +
                    (lateWinningGoal ? "Um golo depois dos 80 minutos decidiu a vitória. " : "") +
                    (upset ? "A equipa venceu um adversário com reputação bastante superior. " : "") +
                    (decisiveCupResult ? "O resultado teve peso na competição a eliminar. " : "") +
                    (string.IsNullOrEmpty(playerOfTheMatchId) || !match.People.TryGetValue(playerOfTheMatchId, out var star)
                        ? "A partida ficou registada na história da época."
                        : star.FullName + " foi o jogador da partida.");
                memory = Record(career, new FootballMemory
                {
                    WhenTicks = match.Fixture.Date.Ticks,
                    Importance = lateWinningGoal || upset || decisiveCupResult ? 4 : 3,
                    Kind = lateWinningGoal ? "vitoria-tardia" : upset ? "surpresa" : decisiveCupResult ? "taca" : "resultado-marcante",
                    ClubId = club.Id,
                    CompetitionId = match.Fixture.LeagueId,
                    RelatedClubId = opponent.Id,
                    PlayerId = playerOfTheMatchId,
                    Title = title,
                    Detail = detail
                });
            }

            if (memory != null)
            {
                Newsroom.Publish(career, "Memórias da época", memory.Title, memory.Detail + "\n\n" + reaction);
            }
            else if (!string.IsNullOrEmpty(reaction))
                Newsroom.Publish(career, "O Bancada", "A bancada fala após " + match.Home.ShortName + "–" + match.Away.ShortName, reaction);

            for (var i = 0; i < playerMoments.Count; i++)
                Newsroom.Publish(career, "Memórias da carreira", playerMoments[i].Title, playerMoments[i].Detail);

            var notification = playerMoments.Count > 0 ? playerMoments[0] : memory;
            if (notification != null)
                CareerNotificationQueue.Enqueue("MOMENTO QUE FICA", notification.Title + ". Este momento foi guardado no histórico da carreira.");
        }

        static FootballMemory RecordSupporterMilestone(CareerState career, MatchSession match, int before, string reaction)
        {
            var after = career.SupporterApproval;
            var crossedHigh = before < 76 && after >= 76;
            var crossedLow = before > 44 && after <= 44;
            var crossedCritical = before > 30 && after <= 30;
            if (!crossedHigh && !crossedLow && !crossedCritical)
                return null;

            var positive = crossedHigh;
            var title = positive ? "A bancada voltou a acreditar" : crossedCritical
                ? "A confiança dos adeptos está no limite"
                : "A paciência dos adeptos está a diminuir";
            return Record(career, new FootballMemory
            {
                WhenTicks = match.Fixture.Date.Ticks,
                Importance = crossedCritical || crossedHigh ? 4 : 3,
                Kind = positive ? "apoio-reforcado" : "pressao-adeptos",
                ClubId = career.Club.Id,
                CompetitionId = match.Fixture.LeagueId,
                RelatedClubId = match.Fixture.OpponentId(career.Club.Id),
                Title = title,
                Detail = (reaction ?? "") + " A relação com os adeptos ficou em " + after + "/100.",
                Value = after
            });
        }

        static List<FootballMemory> RecordPlayerMoments(CareerState career, MatchSession match, int userGoals, int opponentGoals, Club opponent)
        {
            var recorded = new List<FootballMemory>();
            var clubId = career.Club.Id;
            var matchDate = match.Fixture.Date;
            var dateTicks = matchDate.Ticks;

            foreach (var playerId in match.Appeared)
            {
                if (!match.People.TryGetValue(playerId, out var player) || player == null || player.ClubId != clubId)
                    continue;

                if (CareerApps(career, playerId) == 0 && !HasMemory(career, "estreia", playerId))
                {
                    Add("estreia", 3, player, "Estreia sénior de " + player.FullName,
                        player.FullName + " fez a primeira aparição sénior da carreira pelo " + career.Club.Name + ".");
                }

                RecordReturnFromInjury(player);

                if (!match.GoalsScored.TryGetValue(playerId, out var goals) || goals < 1)
                    continue;

                var previousGoals = CareerGoals(career, playerId);
                if (previousGoals == 0)
                    Add("primeiro-golo", 3, player, "Primeiro golo sénior de " + player.FullName,
                        player.FullName + " marcou o primeiro golo sénior da carreira.");
                if (goals >= 3)
                    Add("hat-trick", 4, player, "Hat-trick de " + player.FullName,
                        player.FullName + " marcou " + goals + " golos na mesma partida frente ao " + opponent.Name + ".");

                RecordGoalMilestone(player, previousGoals, goals, 50);
                RecordGoalMilestone(player, previousGoals, goals, 100);

                if (HasFormerClubLink(career, playerId, opponent.Id))
                    Add("golo-antigo-clube", 4, player, player.FullName + " marcou contra o antigo clube",
                        player.FullName + " marcou pelo " + career.Club.Name + " frente ao " + opponent.Name + ", clube de onde chegou.", opponent.Id);
            }

            if (userGoals <= opponentGoals)
            {
                foreach (var note in match.Notes)
                {
                    if (note == null || note.Kind != "penalty-miss" || string.IsNullOrEmpty(note.PlayerId)
                        || !match.People.TryGetValue(note.PlayerId, out var taker) || taker == null || taker.ClubId != clubId)
                        continue;
                    Add("penalti-decisivo-falhado", 4, taker, "Penálti decisivo falhado por " + taker.FullName,
                        taker.FullName + " falhou um penálti numa partida que terminou sem vitória do " + career.Club.Name + ". O lance ficou associado ao resultado.", opponent.Id);
                }
            }

            foreach (var note in match.Notes)
            {
                if (note == null || note.Kind != "injury" || string.IsNullOrEmpty(note.PlayerId)
                    || !match.People.TryGetValue(note.PlayerId, out var injured) || injured == null || injured.ClubId != clubId)
                    continue;
                var days = injured.Injury == null ? 0 : injured.Injury.DaysRemaining;
                if (days < 10)
                    continue;
                Add("lesao-grave", days >= 21 ? 5 : 4, injured, "Lesão afasta " + injured.FullName,
                    injured.FullName + " sofreu uma lesão muscular durante a partida e deverá ficar de fora durante " + days + " dias.", null, days);
            }

            recorded.Sort((a, b) => b.Importance.CompareTo(a.Importance));
            return recorded;

            void RecordGoalMilestone(Player player, int previousGoals, int matchGoals, int milestone)
            {
                if (previousGoals >= milestone || previousGoals + matchGoals < milestone)
                    return;
                Add("marco-golos-" + milestone, 4, player, milestone + " golos sénior: " + player.FullName,
                    player.FullName + " chegou aos " + milestone + " golos sénior ao serviço do " + career.Club.Name + ".");
            }

            void RecordReturnFromInjury(Player player)
            {
                FootballMemory latest = null;
                foreach (var item in career.Memories)
                {
                    if (item.PlayerId != player.Id || (item.Kind != "lesao-grave" && item.Kind != "regresso-lesao") || item.WhenTicks >= dateTicks)
                        continue;
                    if (latest == null || item.WhenTicks > latest.WhenTicks)
                        latest = item;
                }
                if (latest == null || latest.Kind != "lesao-grave" || latest.Value < 12
                    || dateTicks - latest.WhenTicks < TimeSpan.FromDays(latest.Value).Ticks)
                    return;
                Add("regresso-lesao", 3, player, "Regresso de " + player.FullName + " após uma lesão longa",
                    player.FullName + " voltou a jogar depois de ficar " + latest.Value + " dias afastado por lesão.");
            }

            void Add(string kind, int importance, Player player, string title, string detail, string relatedClubId = null, int value = 0)
            {
                var entry = Record(career, new FootballMemory
                {
                    WhenTicks = dateTicks,
                    Importance = importance,
                    Kind = kind,
                    ClubId = clubId,
                    CompetitionId = match.Fixture.LeagueId,
                    RelatedClubId = relatedClubId,
                    PlayerId = player == null ? null : player.Id,
                    Title = title,
                    Detail = detail,
                    Value = value
                });
                if (entry != null)
                    recorded.Add(entry);
            }
        }

        static bool HasFormerClubLink(CareerState career, string playerId, string clubId)
        {
            foreach (var memory in career.Memories)
                if (memory.PlayerId == playerId && memory.RelatedClubId == clubId && memory.Kind == "contratacao")
                    return true;
            return false;
        }

        static bool HasMemory(CareerState career, string kind, string playerId)
        {
            foreach (var memory in career.Memories)
                if (memory.Kind == kind && memory.PlayerId == playerId)
                    return true;
            return false;
        }

        static int CareerApps(CareerState career, string playerId)
        {
            foreach (var line in career.CareerTotals)
                if (line.PlayerId == playerId)
                    return line.Apps;
            return 0;
        }

        static int CareerGoals(CareerState career, string playerId)
        {
            foreach (var line in career.CareerTotals)
                if (line.PlayerId == playerId)
                    return line.Goals;
            return 0;
        }

        static string Signed(int value)
        {
            return value > 0 ? "+" + value : value.ToString();
        }
    }

    public static class CareerRelationships
    {
        public static int TrustOf(CareerState career, string playerId)
        {
            return career != null && career.ManagerTrust.TryGetValue(playerId ?? "", out var trust) ? trust : 50;
        }

        public static int ApplyConversation(CareerState career, Player player, int moraleDelta, int formDelta, int tensionDelta, int injuryDays)
        {
            if (career == null || player == null)
                return 0;
            var trust = TrustOf(career, player.Id);
            var change = (moraleDelta + formDelta) / 3;
            if (tensionDelta < 0) change++;
            else if (tensionDelta > 0) change--;
            if (injuryDays > 0) change--;
            if (player.Personality == PersonalityTrait.Insecure || player.Personality == PersonalityTrait.Temperamental)
                change = (int)Math.Round(change * 1.25f);
            else if (player.Personality == PersonalityTrait.Professional || player.Personality == PersonalityTrait.Leader)
                change = (int)Math.Round(change * 0.75f);
            change = Clamp(change, -6, 6);
            career.ManagerTrust[player.Id] = Clamp(trust + change, 0, 100);
            return change;
        }

        public static void ApplySelectionConsequences(CareerState career, MatchSession match, bool userHome)
        {
            if (career == null || match == null || match.Fixture == null)
                return;

            var bench = userHome ? match.HomeBench : match.AwayBench;
            var club = career.Club;
            var notified = false;
            for (var i = 0; i < bench.Count; i++)
            {
                var playerId = bench[i];
                if (match.Appeared.Contains(playerId) || !career.World.Players.TryGetValue(playerId, out var player) || player.ClubId != club.Id)
                    continue;

                var previousTrust = TrustOf(career, playerId);
                var isKeyPlayer = player.IsCaptain || PlayerAttributes.Overall(player) >= 74;
                var currentTrust = Clamp(previousTrust - (isKeyPlayer ? 2 : 1), 0, 100);
                career.ManagerTrust[playerId] = currentTrust;
                if (!isKeyPlayer || previousTrust <= 40 || currentTrust > 40)
                    continue;

                var memory = CareerMemories.Record(career, new FootballMemory
                {
                    WhenTicks = match.Fixture.Date.Ticks,
                    Importance = 3,
                    Kind = "tempo-de-jogo",
                    ClubId = club.Id,
                    CompetitionId = match.Fixture.LeagueId,
                    PlayerId = playerId,
                    Title = "A confiança de " + player.FullName + " foi abalada",
                    Detail = player.FullName + " voltou a ficar sem minutos numa partida. A confiança no treinador desceu para " + currentTrust + "/100."
                });
                if (memory != null && !notified)
                {
                    CareerNotificationQueue.Enqueue("DECISÃO DO PLANTEL", "A relação com " + player.FullName + " foi afetada por voltar a ficar no banco.");
                    notified = true;
                }
            }

            foreach (var playerId in match.Appeared)
            {
                if (!match.People.TryGetValue(playerId, out var player) || player == null || player.ClubId != club.Id)
                    continue;
                var previousTrust = TrustOf(career, playerId);
                var rating = match.RatingOf(playerId);
                var trustGain = rating >= 8f ? 2 : rating < 5.5f ? 0 : 1;
                var currentTrust = Clamp(previousTrust + trustGain, 0, 100);
                career.ManagerTrust[playerId] = currentTrust;
                if (previousTrust < 80 && currentTrust >= 80 && (player.IsCaptain || PlayerAttributes.Overall(player) >= 74))
                {
                    var memory = CareerMemories.Record(career, new FootballMemory
                    {
                        WhenTicks = match.Fixture.Date.Ticks,
                        Importance = 3,
                        Kind = "confianca-reforcada",
                        ClubId = club.Id,
                        CompetitionId = match.Fixture.LeagueId,
                        PlayerId = playerId,
                        Title = "O capitão confia no projeto",
                        Detail = player.FullName + " atingiu 80/100 de confiança no treinador depois de continuar a contar para a equipa."
                    });
                    if (memory != null && !notified)
                    {
                        CareerNotificationQueue.Enqueue("RELAÇÃO REFORÇADA", player.FullName + " reconhece a confiança que lhe tens dado.");
                        notified = true;
                    }
                }
            }
        }

        static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }

    public static class SupporterReactions
    {
        public static int StartingApproval(Club club)
        {
            return club == null ? 55 : Clamp(52 + (club.Reputation - 50) / 4, 38, 70);
        }

        public static string ApplyResult(CareerState career, Fixture fixture, int homeGoals, int awayGoals)
        {
            if (career == null || fixture == null)
                return "";

            var club = career.Club;
            var opponentId = fixture.OpponentId(club.Id);
            if (string.IsNullOrEmpty(opponentId) || !career.World.Clubs.TryGetValue(opponentId, out var opponent))
                return "";

            var clubGoals = fixture.IsHome(club.Id) ? homeGoals : awayGoals;
            var opponentGoals = fixture.IsHome(club.Id) ? awayGoals : homeGoals;
            var difference = club.Reputation - opponent.Reputation;
            var margin = Math.Abs(clubGoals - opponentGoals);
            var isCup = !string.IsNullOrEmpty(fixture.LeagueId) &&
                (fixture.LeagueId.IndexOf("cup", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 fixture.LeagueId.IndexOf("campeoes", StringComparison.OrdinalIgnoreCase) >= 0);
            var delta = 0;
            string reaction;

            if (clubGoals > opponentGoals)
            {
                delta = difference < -12 ? 8 : difference < 5 ? 5 : 3;
                if (margin >= 3) delta += 2;
                reaction = difference < -12
                    ? "A bancada celebrou uma vitória que poucos esperavam frente ao " + opponent.ShortName + "."
                    : "A vitória sobre o " + opponent.ShortName + " reforçou a confiança no grupo.";
            }
            else if (clubGoals == opponentGoals)
            {
                delta = difference > 12 ? -2 : 1;
                reaction = difference > 12
                    ? "Os adeptos queriam mais do que o empate frente ao " + opponent.ShortName + "."
                    : "O empate frente ao " + opponent.ShortName + " foi recebido com calma.";
            }
            else
            {
                delta = difference > 12 ? -8 : -4;
                if (margin >= 3) delta -= 2;
                reaction = difference > 12
                    ? "A bancada saiu frustrada: esperava uma resposta melhor contra o " + opponent.ShortName + "."
                    : "A derrota frente ao " + opponent.ShortName + " deixou os adeptos a pedir reação.";
            }

            if (isCup)
                delta += clubGoals > opponentGoals ? 2 : clubGoals < opponentGoals ? -2 : 0;
            career.SupporterApproval = Clamp(career.SupporterApproval + delta, 0, 100);

            if (Math.Abs(delta) >= 5)
                CareerNotificationQueue.Enqueue("A BANCADA TOMOU NOTA", reaction);

            if (career.SupporterApproval >= 76)
                reaction += " A relação com os adeptos está muito forte.";
            else if (career.SupporterApproval <= 30)
                reaction += " A confiança na equipa está a ficar frágil.";
            else if (career.SupporterApproval <= 44)
                reaction += " A paciência da bancada está a diminuir.";

            reaction += " Apoio atual: " + career.SupporterApproval + "/100.";

            return reaction;
        }

        static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
