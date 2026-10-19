using System;
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
                "coach|" + Esc(career.Coach.Name) + "|" + career.Coach.ClubId + "|" + career.Calendar.CurrentDate.Ticks,
                "careerprogress|" + career.CompletedSeasons,
                "fanmood|" + career.SupporterApproval
            };
            foreach (var memory in career.Memories)
                lines.Add("memory|" + memory.WhenTicks + "|" + memory.Importance + "|" + Esc(memory.Kind) + "|" + memory.ClubId + "|" + memory.CompetitionId + "|" + memory.RelatedClubId + "|" + memory.PlayerId + "|" + Esc(memory.Title) + "|" + Esc(memory.Detail) + "|" + memory.Value);
            foreach (var trust in career.ManagerTrust)
                lines.Add("trust|" + trust.Key + "|" + trust.Value);
            foreach (var offer in career.JobOffers)
                lines.Add("joboffer|" + offer.ClubId + "|" + offer.ReceivedAtTicks + "|" + (offer.Resolved ? "1" : "0") + "|" + (offer.Accepted ? "1" : "0") + "|" + Esc(offer.Reason));
            var tactics = career.Club.Tactics;
            lines.Add("tactic|" + Esc(tactics.Formation) + "|" + Esc(tactics.Pressing) + "|" + Esc(tactics.BuildUp) + "|" + Esc(tactics.FullBacks) + "|" + Esc(tactics.DefensiveLine) + "|" + Esc(tactics.Width) + "|" + Esc(tactics.Tempo) + "|" + Esc(tactics.PassingStyle) + "|" + Esc(tactics.AttackIdea) + "|" + Esc(tactics.Marking));
            lines.Add("roles|" + career.Club.PenaltyTakerId + "|" + career.Club.CornerTakerId + "|" + career.Club.FreeKickTakerId);
            lines.Add("lineup|" + string.Join(",", career.Club.ManualLineup));
            foreach (var player in career.World.Players.Values)
            {
                var hurt = player.Injury != null && player.Injury.IsInjured ? "1" : "0";
                var days = player.Injury == null ? 0 : player.Injury.DaysRemaining;
                var desc = player.Injury == null ? "" : player.Injury.Description;
                lines.Add("player|" + player.Id + "|" + player.Morale + "|" + player.Fatigue + "|" + Esc(player.Duty) + "|" + (player.IsCaptain ? "1" : "0") + "|" + hurt + "|" + days + "|" + Esc(desc) + "|" + player.ClubId + "|" + player.Age + "|" + TraitCodes(player) + "|" + player.Form + "|" + player.Fitness + "|" + player.TrainingRating + "|" + player.HeightCm + "|" + player.WeightKg + "|" + player.WeakFootQuality + "|" + PlayerPositionProfile.Encode(player));
            }

            foreach (var club in career.World.Clubs.Values)
            {
                lines.Add("club|" + club.Id + "|" + club.LeagueId);
                lines.Add("clubstate|" + club.Id + "|" + club.DressingRoomTension);
            }

            foreach (var fixture in career.Fixtures)
                lines.Add("fixture|" + fixture.Id + "|" + (fixture.Played ? "1" : "0") + "|" + fixture.HomeGoals + "|" + fixture.AwayGoals + "|" + fixture.LeagueId + "|" + fixture.HomeClubId + "|" + fixture.AwayClubId + "|" + fixture.Date.Ticks);
            foreach (var row in career.Table)
                lines.Add("table|" + row.LeagueId + "|" + row.ClubId + "|" + row.Played + "|" + row.Won + "|" + row.Drawn + "|" + row.Lost + "|" + row.GoalsFor + "|" + row.GoalsAgainst + "|" + row.Points);
            foreach (var story in career.Press)
                lines.Add("press|" + Esc(story.Paper) + "|" + Esc(story.Headline) + "|" + Esc(story.Deck) + "|" + Esc(story.Published) + "|" + Esc(story.Kicker) + "|" + story.HomeClubId + "|" + story.AwayClubId + "|" + Esc(story.Motm) + "|" + Esc(story.Quote) + "|" + Esc(story.QuoteBy) + "|" + Esc(story.Body));
            foreach (var note in career.Board)
                lines.Add("board|" + Esc(note.Day) + "|" + Esc(note.Hour) + "|" + note.When.Ticks + "|" + Esc(note.Text));
            foreach (var line in career.Notebook)
                lines.Add("note|" + Esc(line));
            lines.Add("listed|" + string.Join(",", career.Listed));
            lines.Add("targets|" + string.Join(",", career.Targets));
            lines.Add("loans|" + string.Join(",", career.Loans));
            lines.Add("scouted|" + string.Join(",", career.ScoutedPlayers));
            foreach (var job in career.ScoutJobs)
                lines.Add("sjob|" + job.PlayerId + "|" + job.Due);
            foreach (var note in career.PlayerNotes)
                lines.Add("pnote|" + note.PlayerId + "|" + Esc(note.Text));
            foreach (var line in career.Season)
                lines.Add("stat|" + line.PlayerId + "|" + line.Goals + "|" + line.Assists + "|" + line.Apps + "|" + line.RatingSum.ToString(CultureInfo.InvariantCulture) + "|" + line.Rated + "|" + line.CompetitionId);
            foreach (var line in career.CareerTotals)
                lines.Add("careerstat|" + line.PlayerId + "|" + line.Goals + "|" + line.Assists + "|" + line.Apps + "|" + line.RatingSum.ToString(CultureInfo.InvariantCulture) + "|" + line.Rated);
            File.WriteAllLines(PathFile, lines, Encoding.UTF8);
        }

        public static CareerState TryLoad(GameSession session)
        {
            if (session == null || !Exists)
                return null;
            CareerNotificationQueue.Clear();
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

            var generatedFixtures = new List<Fixture>(career.Fixtures);
            var generatedTable = new List<LeagueTableRow>(career.Table);
            var savedClubIds = new HashSet<string>();
            foreach (var raw in lines)
            {
                var bits = Split(raw);
                if (bits.Length > 1 && bits[0] == "club")
                    savedClubIds.Add(bits[1]);
            }

            career.Press.Clear();
            career.Board.Clear();
            career.Notebook.Clear();
            career.CareerTotals.Clear();
            career.Memories.Clear();
            career.ManagerTrust.Clear();
            var hasCareerTotals = false;
            var hasCareerProgress = false;
            var savedCalendar = false;
            foreach (var raw in lines)
            {
                if (raw.StartsWith("fixture|"))
                    savedCalendar = true;
            }

            if (savedCalendar)
            {
                career.Fixtures.Clear();
                career.Table.Clear();
            }

            foreach (var raw in lines)
            {
                var bits = Split(raw);
                if (bits.Length == 0)
                    continue;
                switch (bits[0])
                {
                    case "careerprogress":
                        hasCareerProgress = true;
                        if (bits.Length > 1)
                        {
                            int.TryParse(bits[1], out var completedSeasons);
                            career.CompletedSeasons = Math.Max(0, completedSeasons);
                        }
                        break;
                    case "fanmood":
                        if (bits.Length > 1 && int.TryParse(bits[1], out var supporterApproval))
                            career.SupporterApproval = Math.Max(0, Math.Min(100, supporterApproval));
                        break;
                    case "trust":
                        if (bits.Length > 2 && int.TryParse(bits[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var managerTrust))
                            career.ManagerTrust[bits[1]] = Math.Max(0, Math.Min(100, managerTrust));
                        break;
                    case "memory":
                        if (bits.Length > 9)
                        {
                            long whenTicks;
                            int importance;
                            long.TryParse(bits[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out whenTicks);
                            int.TryParse(bits[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out importance);
                            career.Memories.Add(new FootballMemory
                            {
                                WhenTicks = whenTicks,
                                Importance = Math.Max(1, Math.Min(5, importance)),
                                Kind = Un(bits[3]),
                                ClubId = bits[4],
                                CompetitionId = bits[5],
                                RelatedClubId = bits[6],
                                PlayerId = bits[7],
                                Title = Un(bits[8]),
                                Detail = Un(bits[9]),
                                Value = bits.Length > 10 && int.TryParse(bits[10], NumberStyles.Integer, CultureInfo.InvariantCulture, out var memoryValue) ? memoryValue : 0
                            });
                        }
                        break;
                    case "joboffer":
                        if (bits.Length > 5)
                        {
                            long received;
                            long.TryParse(bits[2], out received);
                            career.JobOffers.Add(new CareerJobOffer
                            {
                                ClubId = bits[1],
                                ReceivedAtTicks = received,
                                Resolved = bits[3] == "1",
                                Accepted = bits[4] == "1",
                                Reason = Un(bits[5])
                            });
                        }
                        break;
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
                    case "club":
                        if (bits.Length > 2 && career.World.Clubs.TryGetValue(bits[1], out var savedClub))
                            savedClub.LeagueId = bits[2];
                        break;
                    case "clubstate":
                        if (bits.Length > 2 && career.World.Clubs.TryGetValue(bits[1], out var stateClub) && int.TryParse(bits[2], out var tension))
                            stateClub.DressingRoomTension = Math.Max(0, Math.Min(100, tension));
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
                    case "targets":
                        career.Targets.Clear();
                        AddIds(career.Targets, bits);
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
                    case "stat":
                        if (bits.Length > 6)
                        {
                            int goals, assists, apps, rated;
                            float sum;
                            int.TryParse(bits[2], out goals);
                            int.TryParse(bits[3], out assists);
                            int.TryParse(bits[4], out apps);
                            float.TryParse(bits[5], NumberStyles.Float, CultureInfo.InvariantCulture, out sum);
                            int.TryParse(bits[6], out rated);
                            var competitionId = bits.Length > 7 ? bits[7] : null;
                            if (string.IsNullOrEmpty(competitionId) && career.World.Players.TryGetValue(bits[1], out var statPlayer) && career.World.Clubs.TryGetValue(statPlayer.ClubId, out var statClub))
                                competitionId = statClub.LeagueId;
                            career.Season.Add(new SeasonLine { PlayerId = bits[1], CompetitionId = competitionId, Goals = goals, Assists = assists, Apps = apps, RatingSum = sum, Rated = rated });
                        }
                        break;
                    case "careerstat":
                        if (bits.Length > 6)
                        {
                            int goals, assists, apps, rated;
                            float sum;
                            int.TryParse(bits[2], out goals);
                            int.TryParse(bits[3], out assists);
                            int.TryParse(bits[4], out apps);
                            float.TryParse(bits[5], NumberStyles.Float, CultureInfo.InvariantCulture, out sum);
                            int.TryParse(bits[6], out rated);
                            career.CareerTotals.Add(new SeasonLine { PlayerId = bits[1], Goals = goals, Assists = assists, Apps = apps, RatingSum = sum, Rated = rated });
                            hasCareerTotals = true;
                        }
                        break;
                }
            }

            if (!hasCareerTotals)
            {
                foreach (var line in career.Season)
                    career.CareerTotals.Add(new SeasonLine { PlayerId = line.PlayerId, Goals = line.Goals, Assists = line.Assists, Apps = line.Apps, RatingSum = line.RatingSum, Rated = line.Rated });
            }

            if (ticks > 0)
                career.Calendar.SetInstant(new System.DateTime(ticks));
            if (!hasCareerProgress && ticks > 0)
            {
                var date = career.Calendar.CurrentDate;
                var completed = date.Year - GameCalendar.SeasonStart.Year;
                if (date.Month < GameCalendar.SeasonStart.Month)
                    completed--;
                career.CompletedSeasons = Math.Max(0, completed);
            }
            if (savedCalendar)
                MergeExpandedWorld(career, generatedFixtures, generatedTable, savedClubIds);
            career.Current = null;
            career.Upcoming.Clear();
            CareerMoments.Continue(career);
            SeasonSchedule.RebuildTable(career);
            SeasonBook.Backfill(career);
            SeasonBook.ReconcileCurrentSeason(career);
            session.Restore(career);
            return career;
        }

        static void MergeExpandedWorld(CareerState career, List<Fixture> generatedFixtures, List<LeagueTableRow> generatedTable, HashSet<string> savedClubIds)
        {
            if (career == null || savedClubIds == null || savedClubIds.Count == 0)
                return;

            foreach (var generatedRow in generatedTable)
            {
                var exists = false;
                foreach (var row in career.Table)
                {
                    if (row.LeagueId == generatedRow.LeagueId && row.ClubId == generatedRow.ClubId)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    career.Table.Add(new LeagueTableRow { LeagueId = generatedRow.LeagueId, ClubId = generatedRow.ClubId });
            }

            foreach (var generated in generatedFixtures)
            {
                if (generated == null || generated.Played || generated.Walkover || generated.Date.Date < career.Calendar.CurrentDate.Date)
                    continue;
                if (!career.World.Clubs.ContainsKey(generated.HomeClubId) || !career.World.Clubs.ContainsKey(generated.AwayClubId))
                    continue;
                if (savedClubIds.Contains(generated.HomeClubId) && savedClubIds.Contains(generated.AwayClubId))
                    continue;
                if (HasFixturePair(career.Fixtures, generated))
                    continue;

                var date = generated.Date.Date;
                var placed = false;
                for (var week = 0; week < 52; week++, date = date.AddDays(7))
                {
                    if (ClubBusy(career.Fixtures, generated.HomeClubId, date) || ClubBusy(career.Fixtures, generated.AwayClubId, date))
                        continue;
                    career.Fixtures.Add(new Fixture
                    {
                        Id = "migrated:" + generated.Id,
                        Date = date,
                        LeagueId = generated.LeagueId,
                        HomeClubId = generated.HomeClubId,
                        AwayClubId = generated.AwayClubId
                    });
                    placed = true;
                    break;
                }

                if (!placed)
                    continue;
            }

            career.Fixtures.Sort((a, b) => a.Date.CompareTo(b.Date));
        }

        static bool HasFixturePair(List<Fixture> fixtures, Fixture target)
        {
            foreach (var fixture in fixtures)
            {
                if (fixture.LeagueId == target.LeagueId && fixture.HomeClubId == target.HomeClubId && fixture.AwayClubId == target.AwayClubId)
                    return true;
            }

            return false;
        }

        static bool ClubBusy(List<Fixture> fixtures, string clubId, System.DateTime date)
        {
            foreach (var fixture in fixtures)
            {
                if (!fixture.Walkover && fixture.Date.Date == date.Date && fixture.Involves(clubId))
                    return true;
            }

            return false;
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
            if (bits.Length > 9)
                tactics.AttackIdea = Un(bits[9]);
            if (bits.Length > 10)
                tactics.Marking = Un(bits[10]);
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
            if (bits.Length > 10 && int.TryParse(bits[10], out var age) && age > 15)
                player.Age = age;
            if (bits.Length > 12 && int.TryParse(bits[12], out var form)) player.Form = System.Math.Max(1, System.Math.Min(99, form));
            if (bits.Length > 13 && int.TryParse(bits[13], out var fitness)) player.Fitness = System.Math.Max(1, System.Math.Min(100, fitness));
            if (bits.Length > 14 && int.TryParse(bits[14], out var training)) player.TrainingRating = System.Math.Max(1, System.Math.Min(100, training));
            if (bits.Length > 15 && int.TryParse(bits[15], out var height) && height > 130) player.HeightCm = height;
            if (bits.Length > 16 && int.TryParse(bits[16], out var weight) && weight > 35) player.WeightKg = weight;
            if (bits.Length > 17 && int.TryParse(bits[17], out var weakFoot)) player.WeakFootQuality = System.Math.Max(1, System.Math.Min(5, weakFoot));
            if (bits.Length > 18) PlayerPositionProfile.Decode(player, bits[18]);
            else PlayerPositionProfile.Ensure(player);
            if (bits.Length > 11)
            {
                player.Traits.Clear();
                if (!string.IsNullOrEmpty(bits[11]))
                {
                    var traits = bits[11].Split(',');
                    for (var i = 0; i < traits.Length; i++)
                    {
                        if (Enum.TryParse(traits[i], out PlayerTrait trait) && Enum.IsDefined(typeof(PlayerTrait), trait) && !player.Traits.Contains(trait))
                            player.Traits.Add(trait);
                    }
                }
            }
        }

        static void ApplyFixture(CareerState career, string[] bits)
        {
            if (bits.Length < 5)
                return;
            if (bits.Length >= 6 && bits[5].IndexOf(":preseason", StringComparison.OrdinalIgnoreCase) >= 0)
                return;
            int.TryParse(bits[3], out var homeGoals);
            int.TryParse(bits[4], out var awayGoals);
            var played = bits[2] == "1";
            if (bits.Length >= 9)
            {
                long ticks;
                long.TryParse(bits[8], out ticks);
                var when = ticks == 0 ? System.DateTime.MinValue : new System.DateTime(ticks).Date;
                foreach (var fixture in career.Fixtures)
                {
                    if (fixture.Restored || fixture.LeagueId != bits[5] || fixture.HomeClubId != bits[6] || fixture.AwayClubId != bits[7])
                        continue;
                    if (when != System.DateTime.MinValue && fixture.Date.Date != when)
                        continue;
                    StampFixture(fixture, played, homeGoals, awayGoals);
                    return;
                }
            }

            foreach (var fixture in career.Fixtures)
            {
                if (fixture.Id != bits[1] || fixture.Restored)
                    continue;
                StampFixture(fixture, played, homeGoals, awayGoals);
                return;
            }

            if (bits.Length < 9)
                return;
            long createdTicks;
            long.TryParse(bits[8], out createdTicks);
            career.Fixtures.Add(new Fixture
            {
                Id = bits[1],
                Played = played,
                HomeGoals = homeGoals,
                AwayGoals = awayGoals,
                LeagueId = bits[5],
                HomeClubId = bits[6],
                AwayClubId = bits[7],
                Date = createdTicks == 0 ? career.Calendar.CurrentDate.Date : new System.DateTime(createdTicks),
                Restored = true
            });
        }

        static void StampFixture(Fixture fixture, bool played, int homeGoals, int awayGoals)
        {
            fixture.Restored = true;
            fixture.Played = played;
            fixture.HomeGoals = homeGoals;
            fixture.AwayGoals = awayGoals;
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

            int.TryParse(bits[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var freshPlayed);
            int.TryParse(bits[4], out var freshWon);
            int.TryParse(bits[5], out var freshDrawn);
            int.TryParse(bits[6], out var freshLost);
            int.TryParse(bits[7], out var freshGf);
            int.TryParse(bits[8], out var freshGa);
            int.TryParse(bits[9], out var freshPts);
            career.Table.Add(new LeagueTableRow
            {
                LeagueId = bits[1],
                ClubId = bits[2],
                Played = freshPlayed,
                Won = freshWon,
                Drawn = freshDrawn,
                Lost = freshLost,
                GoalsFor = freshGf,
                GoalsAgainst = freshGa,
                Points = freshPts
            });
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

        static string TraitCodes(Player player)
        {
            if (player == null || player.Traits == null || player.Traits.Count == 0)
                return "";
            var codes = new List<string>();
            for (var i = 0; i < player.Traits.Count; i++)
                codes.Add(player.Traits[i].ToString());
            return string.Join(",", codes.ToArray());
        }

        static string Un(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Replace("\\n", "\n").Replace("\\\\", "\\");
        }
    }
}
