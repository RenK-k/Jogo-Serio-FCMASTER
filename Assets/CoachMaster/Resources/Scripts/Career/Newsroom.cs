using System.Collections.Generic;
using CoachMaster.Domain;
using CoachMaster.MatchPlay;
using UnityEngine;

namespace CoachMaster.Career
{
    public sealed class PaperStory
    {
        public string Paper;
        public string Headline;
        public string Deck;
        public string Body;
        public string EnglishHeadline;
        public string EnglishBody;
        public string Published;
        public string Kicker;
        public string HomeClubId;
        public string AwayClubId;
        public string Motm;
        public string Quote;
        public string QuoteBy;
        public long DateTicks;
    }

    public static class Newsroom
    {
        sealed class MatchReport
        {
            public string Portuguese;
            public string English;
        }

        public static List<PaperStory> Edition(CareerState career)
        {
            if (career == null)
                return new List<PaperStory>();
            PurgeCareerMemoryStories(career);
            EnsureSeed(career);
            RemoveDuplicateStories(career);
            return career.Press;
        }

        static void RemoveDuplicateStories(CareerState career)
        {
            if (career == null || career.Press == null)
                return;
            var seen = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < career.Press.Count;)
            {
                var story = career.Press[i];
                if (story == null)
                {
                    career.Press.RemoveAt(i);
                    continue;
                }
                var key = (story.Paper ?? string.Empty).Trim() + "|"
                    + (story.Headline ?? string.Empty).Trim();
                if (!seen.Add(key))
                {
                    career.Press.RemoveAt(i);
                    continue;
                }
                i++;
            }
        }

        public static void PurgeCareerMemoryStories(CareerState career)
        {
            if (career == null || career.Press == null)
                return;
            for (var i = career.Press.Count - 1; i >= 0; i--)
            {
                var story = career.Press[i];
                if (story == null || story.Kicker == "MEMORY"
                    || story.Paper == "Memórias da época" || story.Paper == "Memórias da carreira")
                    career.Press.RemoveAt(i);
                else if (story.Kicker != null && story.Kicker.StartsWith("PERSONAL_", System.StringComparison.Ordinal))
                {
                    // Older saves stored the manager's response as a second quote, which made
                    // it appear again in the story card under the manager's name.
                    story.Quote = null;
                    story.QuoteBy = null;
                }
                else if (story.Kicker == "ENTREVISTA" && !string.IsNullOrEmpty(story.Quote)
                    && !string.IsNullOrEmpty(story.Body))
                {
                    // Older previews embedded the player's quote in the article body and also
                    // rendered it in the dedicated quote panel. Keep it in the panel only.
                    var quoteStart = story.Body.IndexOf("\"" + story.Quote + "\"", System.StringComparison.Ordinal);
                    if (quoteStart >= 0)
                    {
                        var paragraphStart = story.Body.LastIndexOf("\n\n", quoteStart, System.StringComparison.Ordinal);
                        story.Body = story.Body.Substring(0, paragraphStart >= 0 ? paragraphStart : quoteStart).TrimEnd();
                    }
                }
                else if (story.Kicker == "MERCADO" && !string.IsNullOrEmpty(story.Motm))
                {
                    story.Quote = "O meu foco é ajudar a equipa. Se surgir algum contacto, o meu representante fala com o clube.";
                    story.QuoteBy = story.Motm;
                    if (!string.IsNullOrEmpty(story.Body))
                    {
                        var quoteStart = story.Body.IndexOf("\n\n\"", System.StringComparison.Ordinal);
                        var clubParagraph = quoteStart < 0 ? -1 : story.Body.IndexOf("\n\nNo ", quoteStart, System.StringComparison.Ordinal);
                        if (quoteStart >= 0 && clubParagraph >= 0)
                            story.Body = story.Body.Substring(0, quoteStart) + story.Body.Substring(clubParagraph);
                    }
                }
            }
        }

        public static void EnsureSeed(CareerState career)
        {
            if (career.Press.Count > 0)
                return;
            var club = career.Club;
            var coach = career.Coach.Name;
            var next = career.NextUserFixture();
            var opponent = next == null ? null : career.World.Clubs[next.OpponentId(club.Id)];
            var venue = next == null ? "" : next.IsHome(club.Id) ? "em casa" : "fora";
            Publish(career, "A Gazeta do Relvado",
                opponent == null ? club.ShortName + " ainda sem jogo marcado" : coach + " assume o " + club.ShortName + " " + venue + " frente ao " + opponent.ShortName,
                club.PublicReputation + "\n\n" + club.PublicTeamState + "\n\nO novo treinador, " + coach + ", herda um balneário que " +
                (club.DressingRoomTension >= 60 ? "não se abre à primeira conversa." : "ainda deixa a porta entreaberta.") + "\n\n" + club.PublicCoachVacancyNote);

            var rival = Rival(career, club);
            if (rival != null)
            {
                Publish(career, "O Bancada", rival.ShortName + " não esconde o que quer desta época",
                    rival.PublicReputation + "\n\n" + rival.PublicTeamState + "\n\nQuem acompanha o " + rival.Name + " aponta " + BestName(career, rival) +
                    " como o nome que desequilibra. A identidade continua a ser " + rival.Tactics.Formation + ".");
            }

            var leagueName = career.League == null ? "A liga" : career.League.Name;
            Publish(career, "Diário da Liga", leagueName + ": a semana de arranque",
                (career.League == null ? "A época está aberta." : career.League.Description) + "\n\nNo " + club.Name + ", a direção repete: " + FirstObjective(club) +
                "\n\n\"" + "Vamos avaliar o plantel no relvado, não nas capas." + "\"\n— " + coach);
            PublishTransfer(career);
        }

        public static void Publish(CareerState career, string paper, string headline, string body)
        {
            if (career == null)
                return;
            var when = career.Calendar.CurrentDate;
            career.Press.Insert(0, new PaperStory
            {
                Paper = paper,
                Headline = headline,
                Body = body,
                DateTicks = when.Ticks,
                Published = when.ToString("dddd, d 'de' MMMM", System.Globalization.CultureInfo.GetCultureInfo("pt-PT")),
                Deck = when.ToString("d MMM", System.Globalization.CultureInfo.GetCultureInfo("pt-PT"))
            });
        }

        public static string PublishPersonalIncident(CareerState career, CareerMoment moment, Player player)
        {
            if (career == null || moment == null || player == null)
                return "";

            var type = (moment.IncidentType ?? "personal").ToUpperInvariant();
            var headline = PersonalHeadline(moment.IncidentType, player.FullName);
            var englishHeadline = PersonalHeadlineEnglish(moment.IncidentType, player.FullName);
            var playerResponsePt = CareerMoments.IncidentPlayerResponse(player, moment.IncidentType, false);
            var playerResponseEn = CareerMoments.IncidentPlayerResponse(player, moment.IncidentType, true);
            var teamResponsePt = CareerMoments.IncidentTeamResponseForNews(career, player, moment.IncidentType, false);
            var teamResponseEn = CareerMoments.IncidentTeamResponseForNews(career, player, moment.IncidentType, true);
            var storyPt = PersonalIncidentFacts(moment.IncidentType, player.FullName, false);
            var storyEn = PersonalIncidentFacts(moment.IncidentType, player.FullName, true);
            var story = new PaperStory
            {
                Paper = "Bastidores do Futebol",
                Headline = headline,
                EnglishHeadline = englishHeadline,
                Deck = "Vida para lá do relvado",
                Body = storyPt + "\n\n" + playerResponsePt + "\n\n" + teamResponsePt,
                EnglishBody = storyEn + "\n\n" + playerResponseEn + "\n\n" + teamResponseEn,
                Kicker = "PERSONAL_" + type,
                HomeClubId = career.Club.Id,
                Motm = player.FullName
            };
            Stamp(career, story);
            career.Press.Insert(0, story);
            TrimPress(career);
            return headline;
        }

        static string PersonalHeadlineEnglish(string incidentType, string playerName)
        {
            switch ((incidentType ?? "").ToLowerInvariant())
            {
                case "bereavement": return playerName + " receives the club's support after a family bereavement";
                case "betting": return "Club reviews betting concern involving " + playerName;
                case "drunk": return playerName + " apologises after arriving tired following a night out";
                case "viral": return playerName + "'s 2-2-6 team sheet becomes a dressing-room joke";
                case "family": return playerName + " welcomes a new arrival in the family";
                case "charity": return playerName + " surprises local children with a charity visit";
                default: return "Club supports " + playerName + " through a personal matter";
            }
        }

        static string PersonalIncidentFacts(string incidentType, string playerName, bool english)
        {
            switch ((incidentType ?? "").ToLowerInvariant())
            {
                case "bereavement": return english
                    ? playerName + " received news of a family bereavement on a matchday. The club gave the player time to be with family and adjusted the squad's plans."
                    : playerName + " recebeu a notícia de uma perda familiar num dia de jogo. O clube deu-lhe tempo para estar com a família e ajustou os planos do plantel.";
                case "betting": return english
                    ? "An internal integrity review is examining a football betting concern involving " + playerName + ". The club has not reached a conclusion and is giving the player a fair process."
                    : "Uma análise interna de integridade está a averiguar uma possível aposta em futebol que envolve " + playerName + ". O clube ainda não chegou a uma conclusão e está a garantir um processo justo.";
                case "drunk": return english
                    ? playerName + " arrived late and visibly tired after a night out. Staff checked the player's condition, adjusted the session and dealt with the matter privately."
                    : playerName + " chegou atrasado e visivelmente cansado depois de uma noite fora. O staff avaliou o seu estado, ajustou o treino e tratou do assunto em privado.";
                case "viral": return english
                    ? "A joking 2-2-6 team sheet shared by " + playerName + " spread around the squad. The players had their laugh, then returned to work on the actual formation."
                    : "Uma folha tática 2-2-6 partilhada por " + playerName + " espalhou-se pelo plantel. Os jogadores riram-se e depois voltaram ao trabalho na formação verdadeira.";
                case "family": return english
                    ? playerName + " welcomed a new baby. The club adjusted the day's workload so the player could be with family without disrupting the team's preparation."
                    : playerName + " recebeu um novo bebé na família. O clube ajustou a carga do dia para que pudesse estar com a família sem prejudicar a preparação da equipa.";
                case "charity": return english
                    ? playerName + " made an unannounced visit to a local children's charity and brought equipment. Teammates have asked to join a future visit."
                    : playerName + " visitou sem aviso uma associação local de apoio a crianças e levou equipamento. Os colegas já perguntaram se podem participar numa próxima visita.";
                default: return english
                    ? "The club is handling a personal matter involving " + playerName + " and has made support available."
                    : "O clube está a acompanhar uma situação pessoal de " + playerName + " e disponibilizou apoio.";
            }
        }

        public static string PersonalIncidentArticle(CareerState career, PaperStory story, bool english)
        {
            if (story == null)
                return "";

            var name = string.IsNullOrWhiteSpace(story.Motm)
                ? (english ? "A player" : "Um jogador")
                : story.Motm;
            var type = story.Kicker != null && story.Kicker.StartsWith("PERSONAL_", System.StringComparison.Ordinal)
                ? story.Kicker.Substring("PERSONAL_".Length).ToLowerInvariant()
                : "personal";
            var article = PersonalIncidentFacts(type, name, english);

            Player player = null;
            if (career != null && career.World != null && career.World.Players != null)
                foreach (var candidate in career.World.Players.Values)
                    if (candidate != null && candidate.FullName == name)
                    {
                        player = candidate;
                        break;
                    }

            var squadReaction = CareerMoments.IncidentTeamResponseForNews(career, player, type, english);
            return string.IsNullOrWhiteSpace(squadReaction) ? article : article + "\n\n" + squadReaction;
        }

        static string PersonalHeadline(string incidentType, string playerName)
        {
            switch ((incidentType ?? "").ToLowerInvariant())
            {
                case "bereavement": return playerName + " recebe apoio do clube após uma perda familiar";
                case "betting": return "Clube apura possível aposta de " + playerName;
                case "drunk": return playerName + " chega cansado ao treino após uma noite longa";
                case "viral": return "Escalação 2-2-6 de " + playerName + " torna-se viral";
                case "family": return playerName + " celebra a chegada de um bebé";
                case "charity": return playerName + " surpreende associação local com visita";
                default: return "O clube acompanha uma situação pessoal de " + playerName;
            }
        }

        public static void PublishMatch(CareerState career, string home, string away, int homeGoals, int awayGoals, bool userHome, string motm = null, string motmMark = null, MatchSession match = null)
        {
            if (career == null)
                return;
            var score = homeGoals + "–" + awayGoals;
            var draw = homeGoals == awayGoals;
            var userWon = userHome ? homeGoals > awayGoals : awayGoals > homeGoals;
            var headline = MatchHeadline(home, away, homeGoals, awayGoals);
            var englishHeadline = "Match report: " + home + " " + score + " " + away;
            Club homeClub = null;
            Club awayClub = null;
            foreach (var club in career.World.Clubs.Values)
            {
                if (homeClub == null && club.ShortName == home)
                    homeClub = club;
                if (awayClub == null && club.ShortName == away)
                    awayClub = club;
            }

            MaybePressLeak(career, homeClub, headline);
            MaybePressLeak(career, awayClub, headline);
            if (string.IsNullOrEmpty(motm))
                motm = BestName(career, career.Club);
            if (string.IsNullOrEmpty(motmMark))
                motmMark = draw ? "7,2" : userWon ? "7,8" : "6,6";
            var reports = BuildMatchReport(career, home, away, homeGoals, awayGoals, userHome, motm, motmMark, match);
            var story = new PaperStory
            {
                Paper = "A Gazeta do Relvado",
                Headline = headline,
                EnglishHeadline = englishHeadline,
                Body = reports.Portuguese,
                EnglishBody = reports.English,
                Kicker = "JOGO",
                HomeClubId = homeClub == null ? null : homeClub.Id,
                AwayClubId = awayClub == null ? null : awayClub.Id,
                Motm = motm + "  ·  " + motmMark
            };
            Stamp(career, story);
            career.Press.Insert(0, story);
        }

        static string MatchHeadline(string home, string away, int homeGoals, int awayGoals)
        {
            var score = homeGoals + "–" + awayGoals;
            if (homeGoals == awayGoals)
                return "Empate deixa tudo em aberto entre " + home + " e " + away;
            var winner = homeGoals > awayGoals ? home : away;
            var loser = homeGoals > awayGoals ? away : home;
            return System.Math.Abs(homeGoals - awayGoals) >= 3
                ? winner + " impõe-se com autoridade frente a " + loser
                : winner + " vence " + loser + " por " + score;
        }

        static MatchReport BuildMatchReport(CareerState career, string home, string away,
            int homeGoals, int awayGoals, bool userHome, string motm, string motmMark, MatchSession match)
        {
            var score = homeGoals + "–" + awayGoals;
            var competitionPt = match == null || match.Fixture == null
                ? "Jogo oficial" : SeasonSchedule.Competition(match.Fixture.LeagueId);
            var competitionEn = match == null || match.Fixture == null
                ? "Competitive fixture" : CompetitionEnglish(match.Fixture.LeagueId);
            var totalHomeXg = 0f;
            var totalAwayXg = 0f;
            var homeShots = match == null ? 0 : match.HomeShots;
            var awayShots = match == null ? 0 : match.AwayShots;
            var homePasses = 0;
            var awayPasses = 0;
            var homeCompleted = 0;
            var awayCompleted = 0;
            var homePossession = match == null ? 0f : match.HomePossession;
            var awayPossession = match == null ? 0f : match.AwayPossession;
            if (match != null)
            {
                for (var i = 0; i < match.Shots.Count; i++)
                {
                    var shot = match.Shots[i];
                    if (shot == null) continue;
                    if (shot.Home) totalHomeXg += shot.ExpectedGoals;
                    else totalAwayXg += shot.ExpectedGoals;
                }
                for (var i = 0; i < match.Passes.Count; i++)
                {
                    var pass = match.Passes[i];
                    if (pass == null || !pass.Resolved) continue;
                    if (pass.Home)
                    {
                        homePasses++;
                        if (pass.Completed) homeCompleted++;
                    }
                    else
                    {
                        awayPasses++;
                        if (pass.Completed) awayCompleted++;
                    }
                }
            }

            var totalPossession = homePossession + awayPossession;
            var homePoss = totalPossession > 0.01f ? Mathf.RoundToInt(homePossession / totalPossession * 100f) : 50;
            var awayPoss = totalPossession > 0.01f ? 100 - homePoss : 50;
            var userXg = userHome ? totalHomeXg : totalAwayXg;
            var opponentXg = userHome ? totalAwayXg : totalHomeXg;
            var userShots = userHome ? homeShots : awayShots;
            var opponentShots = userHome ? awayShots : homeShots;
            var shape = "—";
            TacticalIdentity matchTactics = match == null ? null : userHome ? match.HomeTactics : match.AwayTactics;
            if (matchTactics != null && !string.IsNullOrEmpty(matchTactics.Formation))
                shape = matchTactics.Formation;
            else if (career.Club != null && career.Club.Tactics != null && !string.IsNullOrEmpty(career.Club.Tactics.Formation))
                shape = career.Club.Tactics.Formation;
            var analysisPt = userXg > opponentXg + 0.45f
                ? "A equipa criou ocasiões de maior qualidade; a diferença esteve em transformar a presença no último terço em golos."
                : opponentXg > userXg + 0.45f
                    ? "O adversário chegou a ocasiões mais claras. A prioridade agora é proteger melhor a zona central e controlar as transições."
                    : userShots > opponentShots + 4
                        ? "A equipa rematou mais, mas a produção de ocasiões ficou próxima. Vale a pena trabalhar a escolha do último passe e do remate."
                        : opponentShots > userShots + 4
                            ? "O adversário rematou mais vezes. A equipa precisa de encurtar o espaço entre linhas e sair com mais segurança após recuperar a bola."
                            : "As ocasiões foram equilibradas. O resultado decidiu-se nos detalhes de execução dentro das áreas.";
            var analysisEn = userXg > opponentXg + 0.45f
                ? "The team created the higher-quality chances; the difference was turning final-third presence into goals."
                : opponentXg > userXg + 0.45f
                    ? "The opposition reached the clearer chances. The next priority is protecting the central channel and managing transitions."
                    : userShots > opponentShots + 4
                        ? "The team took more shots, but chance quality stayed close. The final pass and shot selection are the next areas to work on."
                        : opponentShots > userShots + 4
                            ? "The opposition shot more often. The team needs to close the space between the lines and use the ball more securely after regaining it."
                            : "The chances were evenly balanced. Execution in both penalty areas decided the result.";

            var statsPt = homeShots + " remates (xG " + totalHomeXg.ToString("0.00", System.Globalization.CultureInfo.GetCultureInfo("pt-PT")) + ") · "
                + homePoss + "% posse contra " + awayPoss + "% · passes completos " + PassAccuracy(homeCompleted, homePasses) + "%";
            var statsAwayPt = awayShots + " remates (xG " + totalAwayXg.ToString("0.00", System.Globalization.CultureInfo.GetCultureInfo("pt-PT")) + ") · "
                + awayPoss + "% posse · passes completos " + PassAccuracy(awayCompleted, awayPasses) + "%";
            var statsEn = homeShots + " shots (" + totalHomeXg.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " xG) · "
                + homePoss + "% possession · " + PassAccuracy(homeCompleted, homePasses) + "% pass completion";
            var statsAwayEn = awayShots + " shots (" + totalAwayXg.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " xG) · "
                + awayPoss + "% possession · " + PassAccuracy(awayCompleted, awayPasses) + "% pass completion";
            var pt = competitionPt + " · " + home + " " + score + " " + away + ".\n\n"
                + home + ": " + statsPt + ".\n" + away + ": " + statsAwayPt + ".\n\n"
                + analysisPt + "\n\n" + "Foco para o próximo jogo: "
                + (userXg > opponentXg + 0.45f ? "melhorar a eficácia na área." : opponentXg > userXg + 0.45f
                    ? "reduzir as ocasiões concedidas entre linhas." : "trabalhar a decisão no último terço.")
                + "\n\nJogador da partida: " + motm + " (" + motmMark + "). " + "Esquema da equipa: " + shape + ".";
            var en = competitionEn + " · " + home + " " + score + " " + away + ".\n\n"
                + home + ": " + statsEn + ".\n" + away + ": " + statsAwayEn + ".\n\n"
                + analysisEn + "\n\n" + "Next-match focus: "
                + (userXg > opponentXg + 0.45f ? "improve finishing in the box." : opponentXg > userXg + 0.45f
                    ? "limit the chances conceded between the lines." : "work on decision-making in the final third.")
                + "\n\nPlayer of the match: " + motm + " (" + motmMark + "). Team shape: " + shape + ".";
            return new MatchReport { Portuguese = pt, English = en };
        }

        static int PassAccuracy(int completed, int attempts)
        {
            return attempts <= 0 ? 0 : Mathf.RoundToInt(completed / (float)attempts * 100f);
        }

        static string CompetitionEnglish(string id)
        {
            if (string.IsNullOrEmpty(id)) return "Competitive fixture";
            if (id == "league:campeoes") return "Champions League";
            if (id.StartsWith("cup:", System.StringComparison.OrdinalIgnoreCase))
            {
                var parts = id.ToLowerInvariant().Split(':');
                var country = parts.Length > 1 ? parts[1] : "";
                var cupType = parts.Length > 2 ? parts[2] : "taca";
                switch (country)
                {
                    case "inglesa": return cupType == "liga" ? "EFL Cup" : "FA Cup";
                    case "francesa": return cupType == "liga" ? "Coupe de la Ligue" : "Coupe de France";
                    case "espanhola": return cupType == "liga" ? "Spanish Super Cup" : "Copa del Rey";
                    case "bundesliga": return cupType == "liga" ? "German Super Cup" : "DFB-Pokal";
                    case "eredivisie": return cupType == "liga" ? "Johan Cruyff Shield" : "KNVB Cup";
                    case "brasileira": return cupType == "liga" ? "Brazilian Super Cup" : "Copa do Brasil";
                    case "atlas":
                    case "horizonte": return cupType == "liga" ? "Taça da Liga" : "Taça de Portugal";
                }
                return "Cup tie";
            }
            switch (id)
            {
                case "league:atlas": return "Liga Portugal";
                case "league:horizonte": return "Liga Portugal 2";
                case "league:inglesa": return "Premier League";
                case "league:inglesa-championship": return "Championship";
                case "league:inglesa-league-one": return "League One";
                case "league:inglesa-2": return "League Two";
                case "league:francesa": return "Ligue 1";
                case "league:bundesliga": return "Bundesliga";
                case "league:eredivisie": return "Eredivisie";
                case "league:brasileira": return "Brasileirão Série A";
                case "league:espanhola": return "LaLiga";
                case "league:espanhola-2": return "LaLiga Hypermotion";
            }
            return id.IndexOf("cup", System.StringComparison.OrdinalIgnoreCase) >= 0
                || id.IndexOf("taca", System.StringComparison.OrdinalIgnoreCase) >= 0
                ? "Cup tie" : "League match";
        }

        public static void PublishPreview(CareerState career, Fixture fixture)
        {
            if (career == null || fixture == null)
                return;
            var home = career.World.Clubs[fixture.HomeClubId];
            var away = career.World.Clubs[fixture.AwayClubId];
            var headline = "Antevisão: " + home.ShortName + "–" + away.ShortName;
            foreach (var existing in career.Press)
            {
                if (existing.Headline == headline)
                    return;
            }
            var user = career.Club;
            var opp = fixture.IsHome(user.Id) ? away : home;
            var where = fixture.IsHome(user.Id) ? "em casa" : "fora";
            var star = BestName(career, user);
            var their = BestName(career, opp);
            var coach = career.Coach.Name;
            var body = "Antevisão  ·  " + home.ShortName + "–" + away.ShortName + "\n\n" +
                user.Name + " joga " + where + " frente ao " + opp.Name + ", que se apresenta em " + opp.Tactics.Formation + ".\n\n" +
                "No " + user.ShortName + ", " + star + " é o nome que o banco aponta para desequilibrar. Do outro lado, " + their + " é quem o adjunto pede para não perder de vista.\n\n" +
                "\"" + (user.DressingRoomTension >= 60 ? "O balneário precisa do jogo, não de mais conversa." : "Quero o grupo junto desde o primeiro minuto.") + "\"\n— " + coach + ", antes do jogo.";
            var story = new PaperStory
            {
                Paper = "O Bancada",
                Headline = headline,
                Body = body,
                Kicker = "ENTREVISTA",
                HomeClubId = home.Id,
                AwayClubId = away.Id,
                Motm = star,
                Quote = "Se a bola me chegar na área, não vou pensar duas vezes.",
                QuoteBy = star
            };
            Stamp(career, story);
            career.Press.Insert(0, story);
        }

        public static void PublishTransfer(CareerState career)
        {
            if (career == null)
                return;
            Club seller = null;
            foreach (var club in career.World.Clubs.Values)
            {
                if (club.Id == career.Club.Id || club.LeagueId != career.Club.LeagueId)
                    continue;
                seller = club;
                break;
            }

            if (seller == null)
                return;
            var name = BestName(career, seller);
            foreach (var existing in career.Press)
                if (existing != null && existing.Kicker == "MERCADO" && existing.Motm == name
                    && (career.Calendar.CurrentDate.Date - new System.DateTime(existing.DateTicks).Date).Days < 14)
                    return;
            var body = "Mercado  ·  " + seller.ShortName + "\n\n" +
                "O nome de " + name + " voltou a surgir nas conversas do mercado. Os contactos são tratados em privado e ainda não foi anunciado qualquer acordo entre os clubes.\n\n" +
                "O jogador mantém-se concentrado no trabalho diário enquanto o seu representante trata de qualquer contacto.";
            var story = new PaperStory
            {
                Paper = "Diário da Liga",
                Headline = name + " volta a ser ligado ao " + career.Club.ShortName,
                Body = body,
                Kicker = "MERCADO",
                HomeClubId = career.Club.Id,
                AwayClubId = seller.Id,
                Motm = name,
                Quote = "O meu foco é ajudar a equipa. Se surgir algum contacto, o meu representante fala com o clube.",
                QuoteBy = name
            };
            Stamp(career, story);
            career.Press.Insert(0, story);
        }

        public static void PublishTransferDeal(CareerState career, TransferRecord deal)
        {
            if (career == null || deal == null || career.World == null
                || !career.World.Players.TryGetValue(deal.PlayerId ?? "", out var player)
                || !career.World.Clubs.TryGetValue(deal.FromClubId ?? "", out var seller)
                || !career.World.Clubs.TryGetValue(deal.ToClubId ?? "", out var buyer))
                return;
            var fee = deal.Fee.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
            var story = new PaperStory
            {
                Paper = "The Football Wire",
                Headline = player.FullName + " joins " + buyer.ShortName + " from " + seller.ShortName,
                Deck = "Transfer confirmed",
                Body = player.FullName + " has completed a move from " + seller.Name + " to " + buyer.Name + ".\n\n"
                    + "Fee: €" + fee + " · Weekly wage: €" + deal.WeeklyWage.ToString("N0")
                    + " · Contract: " + deal.ContractYears + " years\n\n"
                    + "The deal was completed during the transfer window. " + buyer.ShortName + " beat competing interest to secure the player.",
                Kicker = "GLOBAL TRANSFER",
                HomeClubId = buyer.Id,
                AwayClubId = seller.Id,
                Motm = player.FullName,
                Quote = "The club moved quickly and got its deal over the line.",
                QuoteBy = buyer.ShortName
            };
            Stamp(career, story);
            career.Press.Insert(0, story);
            TrimPress(career);
        }

        public static bool PublishGlobalRoundup(CareerState career)
        {
            if (career == null || career.Calendar == null || career.World == null)
                return false;
            var date = career.Calendar.CurrentDate.Date;
            if (career.LastWorldNewsTicks > 0 && (date - new System.DateTime(career.LastWorldNewsTicks).Date).Days < 3)
                return false;

            Fixture latest = null;
            foreach (var fixture in career.Fixtures)
            {
                if (!fixture.Played || fixture.Walkover || fixture.Involves(career.Club.Id))
                    continue;
                if (latest == null || fixture.Date > latest.Date)
                    latest = fixture;
            }

            PaperStory story;
            if (latest != null && career.World.Clubs.TryGetValue(latest.HomeClubId, out var home)
                && career.World.Clubs.TryGetValue(latest.AwayClubId, out var away))
            {
                var headline = "Around the leagues: " + home.ShortName + " " + latest.HomeGoals + "–" + latest.AwayGoals + " " + away.ShortName;
                var homePosition = TablePosition(career, latest.LeagueId, home.Id, out var homeRow);
                var awayPosition = TablePosition(career, latest.LeagueId, away.Id, out var awayRow);
                var competitionPt = SeasonSchedule.Competition(latest.LeagueId);
                var competitionEn = CompetitionEnglish(latest.LeagueId);
                Club winner = latest.HomeGoals == latest.AwayGoals ? null : latest.HomeGoals > latest.AwayGoals ? home : away;
                var loser = winner == home ? away : home;
                var anglePt = winner == null
                    ? "Os pontos foram divididos, sem alteração na diferença entre os dois clubes."
                    : winner.ShortName + " levou os três pontos; " + loser.ShortName + " terá de responder no próximo compromisso.";
                var angleEn = winner == null
                    ? "The points were shared, leaving the gap between the clubs unchanged."
                    : winner.ShortName + " took all three points; " + loser.ShortName + " will need a response in their next fixture.";
                var positionsPt = TableContext(home.ShortName, homePosition, homeRow, away.ShortName, awayPosition, awayRow, false);
                var positionsEn = TableContext(home.ShortName, homePosition, homeRow, away.ShortName, awayPosition, awayRow, true);
                story = new PaperStory
                {
                    Paper = "The Football Wire",
                    Headline = headline,
                    EnglishHeadline = headline,
                    Deck = "World football",
                    Body = competitionPt + " · " + home.Name + " " + latest.HomeGoals + "–" + latest.AwayGoals + " " + away.Name + ".\n\n"
                        + anglePt + (string.IsNullOrEmpty(positionsPt) ? "" : "\n\n" + positionsPt),
                    EnglishBody = competitionEn + " · " + home.Name + " " + latest.HomeGoals + "–" + latest.AwayGoals + " " + away.Name + ".\n\n"
                        + angleEn + (string.IsNullOrEmpty(positionsEn) ? "" : "\n\n" + positionsEn),
                    Kicker = "GLOBAL",
                    HomeClubId = home.Id,
                    AwayClubId = away.Id
                };
            }
            else
            {
                var best = career.World.Clubs[career.Club.Id];
                foreach (var club in career.World.Clubs.Values)
                    if (club.Id != career.Club.Id && club.Reputation > best.Reputation)
                        best = club;
                var bestPosition = TablePosition(career, best.LeagueId, best.Id, out var bestRow);
                var league = career.World.Leagues.Find(item => item.Id == best.LeagueId);
                var leagueName = league == null ? best.LeagueId : league.Name;
                var englishLeagueName = CompetitionEnglish(best.LeagueId);
                var snapshotPt = bestRow == null || bestRow.Played <= 0
                    ? best.Name + " continua entre os clubes de maior reputação do futebol nacional."
                    : best.Name + " soma " + bestRow.Points + " pontos em " + bestRow.Played + " jogos, com " + bestRow.GoalsFor + " golos marcados e " + bestRow.GoalsAgainst + " sofridos."
                        + (bestPosition > 0 ? " Ocupa o " + bestPosition + ".º lugar de " + leagueName + "." : "");
                var snapshotEn = bestRow == null || bestRow.Played <= 0
                    ? best.Name + " remain among the country's highest-reputation clubs."
                    : best.Name + " have " + bestRow.Points + " points from " + bestRow.Played + " matches, scoring " + bestRow.GoalsFor + " and conceding " + bestRow.GoalsAgainst + "."
                        + (bestPosition > 0 ? " They sit " + Ordinal(bestPosition) + " in " + englishLeagueName + "." : "");
                story = new PaperStory
                {
                    Paper = "The Football Wire",
                    Headline = "The wider game: " + best.ShortName + " set to shape the season",
                    EnglishHeadline = "League watch: " + best.ShortName,
                    Deck = "World football",
                    Body = snapshotPt,
                    EnglishBody = snapshotEn,
                    Kicker = "GLOBAL",
                    HomeClubId = best.Id
                };
            }
            Stamp(career, story);
            career.Press.Insert(0, story);
            career.LastWorldNewsTicks = date.Ticks;
            TrimPress(career);
            return true;
        }

        static int TablePosition(CareerState career, string leagueId, string clubId, out LeagueTableRow found)
        {
            found = null;
            var rows = new List<LeagueTableRow>();
            foreach (var row in career.Table)
                if (row != null && row.LeagueId == leagueId && row.Played > 0)
                    rows.Add(row);
            rows.Sort((a, b) =>
            {
                var points = b.Points.CompareTo(a.Points);
                if (points != 0) return points;
                var difference = b.GoalDifference.CompareTo(a.GoalDifference);
                if (difference != 0) return difference;
                return b.GoalsFor.CompareTo(a.GoalsFor);
            });
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].ClubId != clubId) continue;
                found = rows[i];
                return i + 1;
            }
            return 0;
        }

        static string TableContext(string home, int homePosition, LeagueTableRow homeRow,
            string away, int awayPosition, LeagueTableRow awayRow, bool english)
        {
            if (homeRow == null || awayRow == null || homeRow.Played <= 0 || awayRow.Played <= 0)
                return "";
            if (english)
                return home + " are " + Ordinal(homePosition) + " on " + homeRow.Points + " points; "
                    + away + " are " + Ordinal(awayPosition) + " on " + awayRow.Points + ".";
            return home + " ocupa o " + homePosition + ".º lugar com " + homeRow.Points + " pontos; "
                + away + " está em " + awayPosition + ".º com " + awayRow.Points + ".";
        }

        static string Ordinal(int position)
        {
            if (position % 100 >= 11 && position % 100 <= 13) return position + "th";
            switch (position % 10)
            {
                case 1: return position + "st";
                case 2: return position + "nd";
                case 3: return position + "rd";
                default: return position + "th";
            }
        }

        static void TrimPress(CareerState career)
        {
            while (career.Press.Count > 100)
                career.Press.RemoveAt(career.Press.Count - 1);
        }

        static void Stamp(CareerState career, PaperStory story)
        {
            var when = career.Calendar.CurrentDate;
            story.DateTicks = when.Ticks;
            story.Published = when.ToString("dddd, d 'de' MMMM", System.Globalization.CultureInfo.GetCultureInfo("pt-PT"));
            story.Deck = when.ToString("d MMM", System.Globalization.CultureInfo.GetCultureInfo("pt-PT"));
        }

        static string FirstObjective(Club club)
        {
            if (club.BoardObjectives == null || club.BoardObjectives.Count == 0)
                return "manter o clube competitivo.";
            return club.BoardObjectives[0];
        }

        static Club Rival(CareerState career, Club club)
        {
            Club best = null;
            foreach (var other in career.World.Clubs.Values)
            {
                if (other.Id == club.Id || other.LeagueId != club.LeagueId)
                    continue;
                if (best == null || other.Reputation > best.Reputation)
                    best = other;
            }

            return best;
        }

        static void MaybePressLeak(CareerState career, Club club, string salt)
        {
            if (career == null || club == null || career.Club == null || club.Id == career.Club.Id || career.Notebook == null)
                return;
            var key = "leak|" + club.Id;
            if (career.Notebook.Contains(key))
                return;
            unchecked
            {
                var hash = 17;
                var text = club.Id + "|" + (salt ?? "");
                for (var i = 0; i < text.Length; i++)
                    hash = hash * 31 + text[i];
                if ((hash & 7) != 0)
                    return;
            }
            career.Notebook.Add(key);
        }

        static string BestName(CareerState career, Club club)
        {
            Player best = null;
            var score = -1;
            foreach (var id in club.PlayerIds)
            {
                if (!career.World.Players.TryGetValue(id, out var player) || player.Attributes == null)
                    continue;
                var mark = player.Attributes.Finishing + player.Attributes.Pace + player.Attributes.Passing;
                if (mark > score)
                {
                    score = mark;
                    best = player;
                }
            }

            return best == null ? "um jogador do meio" : best.FullName;
        }
    }
}
