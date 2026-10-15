using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CoachMaster.App;
using CoachMaster.Data;
using CoachMaster.Domain;
using UnityEngine;

namespace CoachMaster.Career
{
    public static class CareerStore
    {
        static string PathFile => Path.Combine(Application.persistentDataPath, "coach-master-career.txt");

        public static bool Exists => File.Exists(PathFile);

        public static void Save(CareerState career)
        {
            if (career == null || career.Coach == null)
                return;
            var lines = new List<string>
            {
                "coach|" + Esc(career.Coach.Name) + "|" + career.Coach.ClubId + "|" + career.Calendar.CurrentDate.Ticks
            };
            var tactics = career.Club.Tactics;
            lines.Add("tactic|" + Esc(tactics.Formation) + "|" + Esc(tactics.Pressing) + "|" + Esc(tactics.BuildUp) + "|" + Esc(tactics.FullBacks) + "|" + Esc(tactics.DefensiveLine) + "|" + Esc(tactics.Width) + "|" + Esc(tactics.Tempo) + "|" + Esc(tactics.PassingStyle));
            lines.Add("roles|" + career.Club.PenaltyTakerId + "|" + career.Club.CornerTakerId + "|" + career.Club.FreeKickTakerId);
            lines.Add("lineup|" + string.Join(",", career.Club.ManualLineup));
            foreach (var player in career.World.Players.Values)
            {
                var hurt = player.Injury != null && player.Injury.IsInjured ? "1" : "0";
                var days = player.Injury == null ? 0 : player.Injury.DaysRemaining;
                var desc = player.Injury == null ? "" : player.Injury.Description;
                lines.Add("player|" + player.Id + "|" + player.Morale + "|" + player.Fatigue + "|" + Esc(player.Duty) + "|" + (player.IsCaptain ? "1" : "0") + "|" + hurt + "|" + days + "|" + Esc(desc) + "|" + player.ClubId);
            }

            foreach (var fixture in career.Fixtures)
                lines.Add("fixture|" + fixture.Id + "|" + (fixture.Played ? "1" : "0") + "|" + fixture.HomeGoals + "|" + fixture.AwayGoals);
            foreach (var row in career.Table)
                lines.Add("table|" + row.LeagueId + "|" + row.ClubId + "|" + row.Played + "|" + row.Won + "|" + row.Drawn + "|" + row.Lost + "|" + row.GoalsFor + "|" + row.GoalsAgainst + "|" + row.Points);
            foreach (var story in career.Press)
                lines.Add("press|" + Esc(story.Paper) + "|" + Esc(story.Headline) + "|" + Esc(story.Deck) + "|" + Esc(story.Published) + "|" + Esc(story.Kicker) + "|" + story.HomeClubId + "|" + story.AwayClubId + "|" + Esc(story.Motm) + "|" + Esc(story.Quote) + "|" + Esc(story.QuoteBy) + "|" + Esc(story.Body));
            foreach (var note in career.Board)
                lines.Add("board|" + Esc(note.Day) + "|" + Esc(note.Hour) + "|" + note.When.Ticks + "|" + Esc(note.Text));
            foreach (var line in career.Notebook)
                lines.Add("note|" + Esc(line));
            lines.Add("listed|" + string.Join(",", career.Listed));
            lines.Add("loans|" + string.Join(",", career.Loans));
            lines.Add("scouted|" + string.Join(",", career.ScoutedPlayers));
            foreach (var job in career.ScoutJobs)
                lines.Add("sjob|" + job.PlayerId + "|" + job.Due);
            foreach (var note in career.PlayerNotes)
                lines.Add("pnote|" + note.PlayerId + "|" + Esc(note.Text));
            File.WriteAllLines(PathFile, lines, Encoding.UTF8);
        }

        public static CareerState TryLoad(GameSession session)
        {
            if (session == null || !Exists)
                return null;
            string[] lines;
            try
            {
                lines = File.ReadAllLines(PathFile, Encoding.UTF8);
            }
            catch
            {
                return null;
            }

            string coach = null;
            string clubId = null;
            long ticks = 0;
            foreach (var raw in lines)
            {
                var bits = Split(raw);
                if (bits.Length > 2 && bits[0] == "coach")
                {
                    coach = Un(bits[1]);
                    clubId = bits[2];
                    long.TryParse(bits.Length > 3 ? bits[3] : "0", out ticks);
                    break;
                }
            }

            if (string.IsNullOrEmpty(coach) || string.IsNullOrEmpty(clubId))
                return null;
            CareerState career;
            try
            {
                career = CareerFactory.Create(coach, clubId, WorldCatalog.Create());
            }
            catch
            {
                return null;
            }

            career.Press.Clear();
            career.Board.Clear();
            career.Notebook.Clear();
            foreach (var raw in lines)
            {
                var bits = Split(raw);
                if (bits.Length == 0)
                    continue;
                switch (bits[0])
                {
                    case "tactic":
                        ApplyTactic(career, bits);
                        break;
                    case "roles":
                        if (bits.Length > 3)
                        {
                            career.Club.PenaltyTakerId = bits[1];
                            career.Club.CornerTakerId = bits[2];
                            career.Club.FreeKickTakerId = bits[3];
                        }
                        break;
                    case "lineup":
                        career.Club.ManualLineup.Clear();
                        if (bits.Length > 1 && !string.IsNullOrEmpty(bits[1]))
                            career.Club.ManualLineup.AddRange(bits[1].Split(','));
                        break;
                    case "player":
                        ApplyPlayer(career, bits);
                        break;
                    case "fixture":
                        ApplyFixture(career, bits);
                        break;
                    case "table":
                        ApplyTable(career, bits);
                        break;
                    case "press":
                        if (bits.Length > 11)
                        {
                            career.Press.Add(new PaperStory
                            {
                                Paper = Un(bits[1]),
                                Headline = Un(bits[2]),
                                Deck = Un(bits[3]),
                                Published = Un(bits[4]),
                                Kicker = Un(bits[5]),
                                HomeClubId = bits[6],
                                AwayClubId = bits[7],
                                Motm = Un(bits[8]),
                                Quote = Un(bits[9]),
                                QuoteBy = Un(bits[10]),
                                Body = Un(bits[11])
                            });
                        }
                        break;
                    case "board":
                        if (bits.Length > 4)
                        {
                            long when;
                            long.TryParse(bits[3], out when);
                            career.Board.Add(new CoachNote
                            {
                                Day = Un(bits[1]),
                                Hour = Un(bits[2]),
                                When = when == 0 ? career.Calendar.CurrentDate : new System.DateTime(when),
                                Text = Un(bits[4])
                            });
                        }
                        break;
                    case "note":
                        if (bits.Length > 1)
                            career.Notebook.Add(Un(bits[1]));
                        break;
                    case "listed":
                        career.Listed.Clear();
                        AddIds(career.Listed, bits);
                        break;
                    case "loans":
                        career.Loans.Clear();
                        AddIds(career.Loans, bits);
                        break;
                    case "scouted":
                        career.ScoutedPlayers.Clear();
                        AddIds(career.ScoutedPlayers, bits);
                        break;
                    case "sjob":
                        if (bits.Length > 2)
                        {
                            long due;
                            long.TryParse(bits[2], out due);
                            career.ScoutJobs.Add(new ScoutJob { PlayerId = bits[1], Due = due });
                        }
                        break;
                    case "pnote":
                        if (bits.Length > 2)
                            career.PlayerNotes.Add(new PlayerNote { PlayerId = bits[1], Text = Un(bits[2]) });
                        break;
                }
            }

            if (ticks > 0)
                career.Calendar.SetInstant(new System.DateTime(ticks));
            career.Current = null;
            career.Upcoming.Clear();
            CareerMoments.Continue(career);
            session.Restore(career);
            return career;
        }

        static void ApplyTactic(CareerState career, string[] bits)
        {
            if (bits.Length < 9 || career.Club.Tactics == null)
                return;
            var tactics = career.Club.Tactics;
            tactics.Formation = Un(bits[1]);
            tactics.Pressing = Un(bits[2]);
            tactics.BuildUp = Un(bits[3]);
            tactics.FullBacks = Un(bits[4]);
            tactics.DefensiveLine = Un(bits[5]);
            tactics.Width = Un(bits[6]);
            tactics.Tempo = Un(bits[7]);
            tactics.PassingStyle = Un(bits[8]);
            tactics.Summary = tactics.Formation + " · " + tactics.Pressing + " · " + tactics.BuildUp;
        }

        static void ApplyPlayer(CareerState career, string[] bits)
        {
            if (bits.Length < 10 || !career.World.Players.TryGetValue(bits[1], out var player))
                return;
            int.TryParse(bits[2], out var morale);
            int.TryParse(bits[3], out var fatigue);
            player.Morale = morale;
            player.Fatigue = fatigue;
            player.Duty = Un(bits[4]);
            player.IsCaptain = bits[5] == "1";
            if (player.Injury == null)
                player.Injury = new InjuryState();
            player.Injury.IsInjured = bits[6] == "1";
            int.TryParse(bits[7], out var days);
            player.Injury.DaysRemaining = days;
            player.Injury.Description = Un(bits[8]);
            if (!string.IsNullOrEmpty(bits[9]))
                player.ClubId = bits[9];
        }

        static void ApplyFixture(CareerState career, string[] bits)
        {
            if (bits.Length < 5)
                return;
            foreach (var fixture in career.Fixtures)
            {
                if (fixture.Id != bits[1])
                    continue;
                fixture.Played = bits[2] == "1";
                int.TryParse(bits[3], out var home);
                int.TryParse(bits[4], out var away);
                fixture.HomeGoals = home;
                fixture.AwayGoals = away;
                return;
            }
        }

        static void ApplyTable(CareerState career, string[] bits)
        {
            if (bits.Length < 10)
                return;
            foreach (var row in career.Table)
            {
                if (row.LeagueId != bits[1] || row.ClubId != bits[2])
                    continue;
                int.TryParse(bits[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var played);
                int.TryParse(bits[4], out var won);
                int.TryParse(bits[5], out var drawn);
                int.TryParse(bits[6], out var lost);
                int.TryParse(bits[7], out var gf);
                int.TryParse(bits[8], out var ga);
                int.TryParse(bits[9], out var pts);
                row.Played = played;
                row.Won = won;
                row.Drawn = drawn;
                row.Lost = lost;
                row.GoalsFor = gf;
                row.GoalsAgainst = ga;
                row.Points = pts;
                return;
            }
        }

        static string[] Split(string raw)
        {
            return string.IsNullOrEmpty(raw) ? new string[0] : raw.Split('|');
        }

        static void AddIds(HashSet<string> target, string[] bits)
        {
            if (bits.Length < 2 || string.IsNullOrEmpty(bits[1]))
                return;
            foreach (var id in bits[1].Split(','))
            {
                if (!string.IsNullOrEmpty(id))
                    target.Add(id);
            }
        }

        static string Esc(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("|", "/");
        }

        static string Un(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Replace("\\n", "\n").Replace("\\\\", "\\");
        }
    }
}
