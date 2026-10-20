using System;
using System.Collections.Generic;
using CoachMaster.Domain;

namespace CoachMaster.Career
{
    public static class CareerJobOffers
    {
        struct Candidate
        {
            public Club Club;
            public int Score;
        }

        public static void Generate(CareerState career)
        {
            if (career == null || career.CompletedSeasons < 1 || career.Club == null)
                return;

            for (var i = career.JobOffers.Count - 1; i >= 0; i--)
            {
                if (!career.JobOffers[i].Resolved)
                    career.JobOffers[i].Resolved = true;
            }

            var current = career.Club;
            var currentTier = Tier(career, current.LeagueId);
            var standing = Standing(career, current.LeagueId, current.Id, out var leagueSize);
            var expectedReputation = current.Reputation;
            if (standing > 0 && leagueSize > 0)
            {
                if (standing <= Math.Max(1, leagueSize / 4))
                    expectedReputation += 14;
                else if (standing <= Math.Max(1, leagueSize / 2))
                    expectedReputation += 6;
                else if (standing > leagueSize * 3 / 4)
                    expectedReputation -= 8;
            }

            var candidates = new List<Candidate>();
            foreach (var club in career.World.Clubs.Values)
            {
                if (club == null || club.Id == current.Id)
                    continue;
                var tier = Tier(career, club.LeagueId);
                if (tier == 0 || Math.Abs(tier - currentTier) > 1)
                    continue;
                if (club.Reputation > expectedReputation + 20)
                    continue;

                var score = Math.Abs(club.Reputation - expectedReputation)
                    + Math.Abs(tier - currentTier) * 16
                    + StableTieBreak(career.Coach.Name, club.Id);
                candidates.Add(new Candidate { Club = club, Score = score });
            }

            candidates.Sort((a, b) =>
            {
                var score = a.Score.CompareTo(b.Score);
                return score != 0 ? score : string.CompareOrdinal(a.Club.Id, b.Club.Id);
            });

            var count = Math.Min(3, candidates.Count);
            for (var i = 0; i < count; i++)
            {
                var target = candidates[i].Club;
                var reason = target.Reputation > current.Reputation + 8
                    ? "A boa época chamou a atenção da direção. Querem conversar sobre um projeto com objetivos maiores."
                    : Tier(career, target.LeagueId) < currentTier
                        ? "A direção procura alguém capaz de levar o clube a uma divisão acima. O teu trabalho entrou na lista."
                        : "A direção gostou do trabalho da última época e quer convidar-te para liderar o próximo projeto.";
                career.JobOffers.Add(new CareerJobOffer
                {
                    ClubId = target.Id,
                    ReceivedAtTicks = career.Calendar.CurrentDate.Ticks,
                    Reason = reason
                });
            }

            if (count > 0)
            {
                Newsroom.Publish(career, "Mercado de Treinadores", "Chegaram propostas para o mister",
                    count == 1
                        ? "Uma direção contactou o teu representante depois da época que fizeste. A proposta está nas mensagens do telemóvel."
                        : count + " direções contactaram o teu representante depois da época que fizeste. As propostas estão nas mensagens do telemóvel.");
            }
        }

        public static bool Accept(CareerState career, CareerJobOffer offer)
        {
            if (career == null || offer == null || offer.Resolved
                || !career.World.Clubs.TryGetValue(offer.ClubId ?? "", out var target))
                return false;

            var previous = career.Club;
            previous.PublicCoachVacancyNote = "A direção procura um treinador para liderar o próximo projeto.";
            target.PublicCoachVacancyNote = "";
            career.Coach.ClubId = target.Id;
            career.SupporterApproval = SupporterReactions.StartingApproval(target);
            CareerMemories.Record(career, new FootballMemory
            {
                WhenTicks = career.Calendar.CurrentDate.Ticks,
                Importance = 4,
                Kind = "mudanca-de-clube",
                ClubId = target.Id,
                RelatedClubId = previous.Id,
                Title = "Novo capítulo no " + target.Name,
                Detail = "O treinador deixou o " + previous.Name + " e aceitou liderar o " + target.Name + "."
            });
            CareerNotificationQueue.Enqueue("NOVO CAPÍTULO", "Assumiste o " + target.Name + ". A tua relação com a nova bancada começa agora.");
            foreach (var pending in career.JobOffers)
            {
                if (pending.Resolved)
                    continue;
                pending.Resolved = true;
                pending.Accepted = pending == offer;
            }

            career.Notebook.Insert(0, career.Calendar.ClockText + "  ·  Assumiste o comando do " + target.Name + ".");
            return true;
        }

        public static void Decline(CareerState career, CareerJobOffer offer)
        {
            if (career == null || offer == null || offer.Resolved)
                return;
            offer.Resolved = true;
            offer.Accepted = false;
            career.Notebook.Insert(0, career.Calendar.ClockText + "  ·  Recusaste a proposta do " + ClubName(career, offer.ClubId) + ".");
        }

        static string ClubName(CareerState career, string clubId)
        {
            return career.World.Clubs.TryGetValue(clubId ?? "", out var club) ? club.Name : "clube";
        }

        static int Tier(CareerState career, string leagueId)
        {
            foreach (var league in career.World.Leagues)
            {
                if (league.Id == leagueId)
                    return league.Tier == LeagueTier.Continental ? 0 : (int)league.Tier;
            }
            return 0;
        }

        static int Standing(CareerState career, string leagueId, string clubId, out int leagueSize)
        {
            var rows = new List<LeagueTableRow>();
            foreach (var row in career.Table)
            {
                if (row.LeagueId == leagueId)
                    rows.Add(row);
            }
            rows.Sort((a, b) =>
            {
                var points = b.Points.CompareTo(a.Points);
                if (points != 0) return points;
                var difference = (b.GoalsFor - b.GoalsAgainst).CompareTo(a.GoalsFor - a.GoalsAgainst);
                if (difference != 0) return difference;
                var goals = b.GoalsFor.CompareTo(a.GoalsFor);
                return goals != 0 ? goals : string.CompareOrdinal(a.ClubId, b.ClubId);
            });
            leagueSize = rows.Count;
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].ClubId == clubId)
                    return i + 1;
            }
            return 0;
        }

        static int StableTieBreak(string coach, string clubId)
        {
            unchecked
            {
                var hash = 17;
                var value = (coach ?? "") + ":" + (clubId ?? "");
                for (var i = 0; i < value.Length; i++)
                    hash = hash * 31 + value[i];
                return (hash & 0x7fffffff) % 9;
            }
        }
    }
}
