using System.Collections.Generic;
using CoachMaster.Domain;

namespace CoachMaster.Career
{
    public sealed class PaperStory
    {
        public string Paper;
        public string Headline;
        public string Deck;
        public string Body;
        public string Published;
        public string Kicker;
        public string HomeClubId;
        public string AwayClubId;
        public string Motm;
        public string Quote;
        public string QuoteBy;
    }

    public static class Newsroom
    {
        public static List<PaperStory> Edition(CareerState career)
        {
            EnsureSeed(career);
            return career.Press;
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
                Published = when.ToString("dddd, d 'de' MMMM", System.Globalization.CultureInfo.GetCultureInfo("pt-PT")),
                Deck = when.ToString("d MMM", System.Globalization.CultureInfo.GetCultureInfo("pt-PT"))
            });
        }

        public static void PublishMatch(CareerState career, string home, string away, int homeGoals, int awayGoals, bool userHome, string motm = null, string motmMark = null)
        {
            var score = homeGoals + "–" + awayGoals;
            var userWon = userHome ? homeGoals > awayGoals : awayGoals > homeGoals;
            var draw = homeGoals == awayGoals;
            var verdict = draw ? "Empate" : userWon ? "Vitória" : "Derrota";
            var headline = verdict + "  " + home + " " + score + " " + away;
            Club homeClub = null;
            Club awayClub = null;
            foreach (var club in career.World.Clubs.Values)
            {
                if (homeClub == null && club.ShortName == home)
                    homeClub = club;
                if (awayClub == null && club.ShortName == away)
                    awayClub = club;
            }

            if (string.IsNullOrEmpty(motm))
                motm = BestName(career, career.Club);
            if (string.IsNullOrEmpty(motmMark))
                motmMark = draw ? "7,2" : userWon ? "7,8" : "6,6";
            var coach = career.Coach.Name;
            var quote = draw
                ? "O jogo esteve partido ao meio. Saímos com a sensação de que os três pontos estavam ali."
                : userWon
                    ? "Fomos nós a procurar a baliza. O resultado explica o que se viu do banco."
                    : "Faltou-nos a última decisão. Não vou esconder que o balneário saiu em silêncio.";
            var playerQuote = "Tentei chegar à área sempre que a bola passou do meio. O resto foi o grupo.";
            var shape = career.Club.Tactics.Formation;
            var body = home + " e " + away + " fecharam " + score + ".\n\n" +
                "O " + career.Club.Name + " alinhou em " + shape + ". " +
                (draw ? "Nenhum dos lados conseguiu desfazer o equilíbrio nos últimos minutos." : userWon ? "A vantagem nasceu de quem chegou mais vezes à área." : "O adversário foi mais concreto no último terço.") +
                "\n\nJogador da partida: " + motm + " (" + motmMark + ").\n\n" +
                "\"" + quote + "\"\n— " + coach + ", treinador\n\n" +
                "\"" + playerQuote + "\"\n— " + motm + ", jogador da partida";
            var story = new PaperStory
            {
                Paper = "A Gazeta do Relvado",
                Headline = headline,
                Body = body,
                Kicker = "JOGO",
                HomeClubId = homeClub == null ? null : homeClub.Id,
                AwayClubId = awayClub == null ? null : awayClub.Id,
                Motm = motm + "  ·  " + motmMark,
                Quote = quote,
                QuoteBy = coach
            };
            Stamp(career, story);
            career.Press.Insert(0, story);
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
                "\"" + (user.DressingRoomTension >= 60 ? "O balneário precisa do jogo, não de mais conversa." : "Quero o grupo junto desde o primeiro minuto.") + "\"\n— " + coach + ", antes do jogo\n\n" +
                "\"" + "Se a bola me chegar na área, não vou pensar duas vezes." + "\"\n— " + star;
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
            var body = "Mercado  ·  " + seller.ShortName + "\n\n" +
                "O nome de " + name + " voltou a circular nos corredores. O " + career.Club.ShortName + " ouve, a direção ainda não assina nada.\n\n" +
                "\"" + "Não comento nomes. Se houver conversa, é com o clube, não com o jornal." + "\"\n— " + career.Coach.Name + "\n\n" +
                "No " + seller.Name + ", a resposta foi curta: o jogador concentra-se no próximo jogo. Não há acordo, nem desmentido firme.";
            var story = new PaperStory
            {
                Paper = "Diário da Liga",
                Headline = name + " volta a ser ligado ao " + career.Club.ShortName,
                Body = body,
                Kicker = "MERCADO",
                HomeClubId = career.Club.Id,
                AwayClubId = seller.Id,
                Motm = name,
                Quote = "Não comento nomes. Se houver conversa, é com o clube, não com o jornal.",
                QuoteBy = career.Coach.Name
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
                story = new PaperStory
                {
                    Paper = "The Football Wire",
                    Headline = headline,
                    Deck = "World football",
                    Body = home.Name + " and " + away.Name + " met in a fixture with wider league implications. "
                        + "The result moves both clubs closer to their season targets, while the rest of the table continues to take shape.",
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
                story = new PaperStory
                {
                    Paper = "The Football Wire",
                    Headline = "The wider game: " + best.ShortName + " set to shape the season",
                    Deck = "World football",
                    Body = best.Name + " are among the clubs attracting attention as the season develops. Their squad, finances and results will influence the race across the league.",
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

        static void TrimPress(CareerState career)
        {
            while (career.Press.Count > 100)
                career.Press.RemoveAt(career.Press.Count - 1);
        }

        static void Stamp(CareerState career, PaperStory story)
        {
            var when = career.Calendar.CurrentDate;
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
