using System;
using System.Collections.Generic;
using CoachMaster.Data;
using CoachMaster.Domain;
using CoachMaster.UI;

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

            var weeklyDismissalMarker = "manager-fired-week|" + today.Ticks;
            if (career.Notebook.Contains(weeklyDismissalMarker))
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

                var oldManager = WorldCatalog.CurrentManagerName(club);
                var english = LocalizationManager.Current == GameLanguage.English;
                club.PublicCoachVacancyNote = english
                    ? "The board is looking for a manager after a run of results below expectations."
                    : "A direção procura treinador depois de uma sequência de resultados abaixo das expectativas.";
                club.HiddenDismissalTruth = english
                    ? oldManager + " was dismissed after the club fell short of its season objectives."
                    : oldManager + " foi despedido depois de o clube ficar abaixo dos objetivos da época.";
                career.Notebook.Add(marker);
                career.Notebook.Add(weeklyDismissalMarker);
                var appointmentName = WorldCatalog.GenerateManagerName(club, today.Year + ":" + today.DayOfYear);
                career.Notebook.Add("manager-hire|" + club.Id + "|" + today.AddDays(14).Ticks + "|" + appointmentName + "|" + oldManager);
                var title = english ? oldManager + " leaves " + club.Name : oldManager + " deixa o " + club.Name;
                var detail = english
                    ? club.Name + " have dismissed manager " + oldManager + " after a poor run. The club will appoint a successor in two weeks."
                    : club.Name + " despediu o treinador " + oldManager + " após uma série negativa. O clube deverá anunciar um sucessor dentro de duas semanas.";
                Newsroom.Publish(career, english ? "Manager Market" : "Mercado de Treinadores", title, detail);
                career.Board.Insert(0, new CoachNote
                {
                    Day = today.ToString("dd/MM/yyyy"),
                    Hour = "09:00",
                    When = today.AddHours(9),
                    Text = "✉ " + detail
                });
                changed = true;
                break;
            }
            return changed;
        }

        static bool SettleAppointments(CareerState career, DateTime today)
        {
            var changed = false;
            var dailyAppointmentMarker = "manager-appointed-day|" + today.Ticks;
            if (career.Notebook.Contains(dailyAppointmentMarker))
                return false;
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
                    var newManager = parts.Length > 3 ? parts[3] : WorldCatalog.GenerateManagerName(club, today.Ticks.ToString());
                    var oldManager = parts.Length > 4 ? parts[4] : WorldCatalog.CurrentManagerName(club);
                    if (club.Tactics != null)
                        club.Tactics.FormerCoachName = newManager;
                    club.PublicCoachVacancyNote = "";
                    club.HiddenDismissalTruth = "";
                    var english = LocalizationManager.Current == GameLanguage.English;
                    var title = english ? newManager + " appointed by " + club.Name : newManager + " assume o comando do " + club.Name;
                    var detail = english
                        ? club.Name + " have appointed " + newManager + " as manager" + (string.IsNullOrEmpty(oldManager) ? "." : " after dismissing " + oldManager + ".")
                        : club.Name + " anunciou " + newManager + " como treinador" + (string.IsNullOrEmpty(oldManager) ? "." : " após a saída de " + oldManager + ".");
                    Newsroom.Publish(career, english ? "Manager Market" : "Mercado de Treinadores", title, detail);
                    career.Board.Insert(0, new CoachNote
                    {
                        Day = today.ToString("dd/MM/yyyy"),
                        Hour = "09:00",
                        When = today.AddHours(9),
                        Text = "✉ " + detail
                    });
                }
                career.Notebook.RemoveAt(i);
                career.Notebook.Add(dailyAppointmentMarker);
                changed = true;
                break;
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
