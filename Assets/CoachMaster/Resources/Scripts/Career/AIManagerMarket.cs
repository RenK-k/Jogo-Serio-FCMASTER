using System;
using System.Collections.Generic;
using CoachMaster.Domain;

namespace CoachMaster.Career
{
    public static class AIManagerMarket
    {
        public static bool Advance(CareerState career)
        {
            if (career == null || career.Calendar == null || career.World == null)
                return false;
            var today = career.Calendar.CurrentDate.Date;
            var changed = SettleAppointments(career, today);
            if (today.DayOfWeek != DayOfWeek.Monday || today.Month < 10)
                return changed;

            var season = today.Month >= 7 ? today.Year : today.Year - 1;
            var leagues = new Dictionary<string, List<LeagueTableRow>>();
            foreach (var row in career.Table)
            {
                if (!leagues.TryGetValue(row.LeagueId ?? "", out var rows))
                {
                    rows = new List<LeagueTableRow>();
                    leagues[row.LeagueId ?? ""] = rows;
                }
                rows.Add(row);
            }

            foreach (var club in career.World.Clubs.Values)
            {
                if (club == null || club.Id == career.Coach?.ClubId || HasPendingAppointment(career, club.Id))
                    continue;
                var marker = "manager-fired|" + club.Id + "|" + season;
                if (career.Notebook.Contains(marker) || !leagues.TryGetValue(club.LeagueId ?? "", out var rows) || rows.Count < 4)
                    continue;
                rows.Sort((a, b) =>
                {
                    var points = b.Points.CompareTo(a.Points);
                    if (points != 0) return points;
                    var difference = (b.GoalsFor - b.GoalsAgainst).CompareTo(a.GoalsFor - a.GoalsAgainst);
                    return difference != 0 ? difference : b.GoalsFor.CompareTo(a.GoalsFor);
                });
                var standing = 0;
                LeagueTableRow clubRow = null;
                for (var i = 0; i < rows.Count; i++)
                    if (rows[i].ClubId == club.Id)
                    {
                        standing = i + 1;
                        clubRow = rows[i];
                        break;
                    }
                if (clubRow == null || clubRow.Played < 7)
                    continue;

                var bottomQuarter = standing >= Math.Max(3, (rows.Count * 3 + 3) / 4);
                var weakPointsRate = clubRow.Points / (float)Math.Max(1, clubRow.Played) < 0.9f;
                if (!bottomQuarter && !weakPointsRate)
                    continue;
                var pressure = bottomQuarter ? 34 : 18;
                if (club.Reputation >= 75) pressure += 12;
                if (clubRow.Points / (float)Math.Max(1, clubRow.Played) < 0.65f) pressure += 18;
                if (Stable(club.Id + "|" + season + "|" + today.DayOfYear) % 100 >= pressure)
                    continue;

                club.PublicCoachVacancyNote = "A direção procura treinador depois de uma sequência de resultados abaixo das expectativas.";
                club.HiddenDismissalTruth = "O treinador foi despedido após a equipa ficar abaixo dos objetivos definidos para a época.";
                career.Notebook.Add(marker);
                career.Notebook.Add("manager-hire|" + club.Id + "|" + today.AddDays(14).Ticks);
                Newsroom.Publish(career, "Mercado de Treinadores", "Treinador despedido após maus resultados",
                    "A direção decidiu mudar de treinador depois de a equipa ficar abaixo das metas da época. O clube prepara agora a sucessão.");
                changed = true;
            }
            return changed;
        }

        static bool SettleAppointments(CareerState career, DateTime today)
        {
            var changed = false;
            for (var i = career.Notebook.Count - 1; i >= 0; i--)
            {
                var line = career.Notebook[i];
                if (string.IsNullOrEmpty(line) || !line.StartsWith("manager-hire|", StringComparison.Ordinal))
                    continue;
                var parts = line.Split('|');
                if (parts.Length < 3 || !long.TryParse(parts[2], out var due) || new DateTime(due).Date > today)
                    continue;
                if (career.World.Clubs.TryGetValue(parts[1], out var club))
                {
                    club.PublicCoachVacancyNote = "";
                    club.HiddenDismissalTruth = "";
                    Newsroom.Publish(career, "Mercado de Treinadores", "Novo treinador assume o cargo",
                        "O clube confirmou a contratação de um novo treinador depois de concluir o processo de escolha.");
                }
                career.Notebook.RemoveAt(i);
                changed = true;
            }
            return changed;
        }

        static bool HasPendingAppointment(CareerState career, string clubId)
        {
            var prefix = "manager-hire|" + clubId + "|";
            foreach (var line in career.Notebook)
                if (line != null && line.StartsWith(prefix, StringComparison.Ordinal))
                    return true;
            return false;
        }

        static int Stable(string value)
        {
            unchecked
            {
                var hash = 23;
                for (var i = 0; i < value.Length; i++) hash = hash * 31 + value[i];
                return hash & 0x7fffffff;
            }
        }
    }
}
