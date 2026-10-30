using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CoachMaster.Career;
using CoachMaster.Domain;
using CoachMaster.MatchPlay;
using UnityEditor;
using UnityEngine;

namespace CoachMaster.EditorTools
{
    public static class MatchSimulationBatch
    {
        const float TickDt = 0.2f;
        const float MinutesPerSecond = 0.68f;
        const int MaxTicks = 12000;
        const int SeedsPerScenario = 20;

        static readonly Scenario[] Scenarios =
        {
            new Scenario("equilibradas", "4-3-3", "4-3-3", 62, 62, 0, 0, 20, 20, 0, 0, false),
            new Scenario("casa-forte", "4-3-3", "4-3-3", 78, 42, 0, 0, 12, 55, 0, 0, false),
            new Scenario("formacoes", "4-3-3", "5-4-1", 62, 62, 0, 0, 20, 20, 0, 0, false),
            new Scenario("taticas", "4-3-3", "4-4-2", 62, 62, 2, 3, 20, 20, 0, 0, false),
            new Scenario("fadiga-e-substituicao", "4-3-3", "4-3-3", 62, 62, 0, 0, 88, 20, 0, 0, true),
            new Scenario("extremos", "4-3-3", "5-4-1", 62, 62, 0, 0, 20, 20, 20, 1, false)
        };

        [MenuItem("Coach Master/Simular 6 partidas de controlo")]
        public static void RunSmoke()
        {
            var path = Run(1);
            Debug.Log("Lote de controlo gravado em " + path);
            EditorUtility.RevealInFinder(path);
        }

        [MenuItem("Coach Master/Simular 120 partidas")]
        public static void RunFull()
        {
            var path = Run(SeedsPerScenario);
            Debug.Log("Lote de 120 partidas gravado em " + path);
            EditorUtility.RevealInFinder(path);
        }

        public static void RunFromCommandLine()
        {
            var path = Run(SeedsPerScenario);
            Debug.Log("Lote gravado em " + path);
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        public static string Run(int seeds)
        {
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "MatchSimReports"));
            Directory.CreateDirectory(folder);
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var jsonlPath = Path.Combine(folder, "matches-" + stamp + ".jsonl");
            var summaryPath = Path.Combine(folder, "summary-" + stamp + ".json");
            var rows = new List<MatchRow>();
            var failures = 0;
            foreach (var scenario in Scenarios)
            {
                for (var seed = 1; seed <= seeds; seed++)
                {
                    MatchRow row;
                    try
                    {
                        row = Play(scenario, seed);
                    }
                    catch (Exception error)
                    {
                        row = new MatchRow
                        {
                            Scenario = scenario.Name,
                            Seed = seed,
                            Finished = false,
                            Error = error.GetType().Name + ": " + error.Message
                        };
                    }

                    if (!row.Finished || !string.IsNullOrEmpty(row.Error) || row.Incoherent)
                        failures++;
                    rows.Add(row);
                    File.AppendAllText(jsonlPath, row.ToJson() + "\n", Encoding.UTF8);
                }
            }

            File.WriteAllText(summaryPath, Summary(rows, seeds, failures, jsonlPath), Encoding.UTF8);
            return summaryPath;
        }

        static MatchRow Play(Scenario scenario, int seed)
        {
            var career = BuildCareer(scenario, seed);
            var fixture = career.Fixtures[0];
            var match = MatchSession.Create(career, fixture);
            var engine = new MatchEngine(match, career.Coach.ClubId, 1f, 1f, StableSeed(fixture.Id));
            var row = new MatchRow
            {
                Scenario = scenario.Name,
                Seed = seed,
                FixtureId = fixture.Id,
                HomeFormation = scenario.HomeFormation,
                AwayFormation = scenario.AwayFormation,
                HomePreset = TacticPresets.Names[scenario.HomePreset],
                AwayPreset = TacticPresets.Names[scenario.AwayPreset],
                HomeOverall = MeanOverall(match, true),
                AwayOverall = MeanOverall(match, false),
                HomeFatigue = scenario.HomeFatigue,
                AwayFatigue = scenario.AwayFatigue
            };
            var homePrevious = new Vector2[11];
            var awayPrevious = new Vector2[11];
            var havePrevious = false;
            var substituted = false;
            var ticks = 0;
            var stuck = 0;
            var lastMinute = match.Minute;
            while (ticks < MaxTicks && !match.Finished)
            {
                var phase = match.Phase;
                engine.Tick(TickDt, MinutesPerSecond);
                ticks++;
                if (match.Phase == MatchPhase.HalfTime)
                    engine.StartSecondHalf();
                if (!substituted && scenario.SubstituteAt60 && match.Minute >= 60f && match.HomeBench.Count > 0)
                {
                    substituted = true;
                    row.SubstitutionApplied = engine.TrySub(true, 10, 0);
                }

                Observe(engine, match, row, ref homePrevious, ref awayPrevious, ref havePrevious);
                if (Mathf.Abs(match.Minute - lastMinute) < 0.0001f && phase == match.Phase && match.Phase != MatchPhase.FullTime)
                    stuck++;
                else
                    stuck = 0;
                lastMinute = match.Minute;
                if (stuck > 400)
                {
                    row.Error = "O minuto deixou de avançar.";
                    break;
                }
            }

            row.Ticks = ticks;
            row.Finished = match.Finished && match.Phase == MatchPhase.FullTime;
            if (!row.Finished && string.IsNullOrEmpty(row.Error))
                row.Error = ticks >= MaxTicks ? "A partida não terminou dentro do limite." : "A partida parou antes do apito final.";
            FillBox(match, row);
            return row;
        }

        static void Observe(MatchEngine engine, MatchSession match, MatchRow row, ref Vector2[] homePrevious, ref Vector2[] awayPrevious, ref bool havePrevious)
        {
            if (!IsFinite(engine.Ball))
                row.InvalidCoordinates++;
            for (var i = 0; i < 11; i++)
            {
                var home = engine.MotionOf(true, i);
                var away = engine.MotionOf(false, i);
                if (!IsFinite(home) || !IsFinite(away))
                    row.InvalidCoordinates++;
                if (havePrevious)
                {
                    if (PitchMetres(homePrevious[i], home) > 40f)
                        row.AbruptMoves++;
                    if (PitchMetres(awayPrevious[i], away) > 40f)
                        row.AbruptMoves++;
                    row.HomeMetres += PitchMetres(homePrevious[i], home);
                    row.AwayMetres += PitchMetres(awayPrevious[i], away);
                }

                homePrevious[i] = home;
                awayPrevious[i] = away;
            }

            havePrevious = true;
            var passing = engine.ActionKind == "pass" || engine.ActionKind == "cross";
            if (!passing)
                return;
            row.PassFlightSamples++;
            var carrier = engine.MotionOf(match.HomeHasBall, Mathf.Clamp(match.BallHolder, 0, 10));
            if (Vector2.Distance(carrier, engine.BallTarget) < 0.12f)
                row.CarrierNearPassTarget++;
        }

        static void FillBox(MatchSession match, MatchRow row)
        {
            row.HomeGoals = match.HomeGoals;
            row.AwayGoals = match.AwayGoals;
            row.Minute = match.Minute;
            row.HomeShots = match.HomeShots;
            row.AwayShots = match.AwayShots;
            row.ShotEvents = match.Shots.Count;
            row.PositionSamples = match.PositionSamples.Count;
            var goalsNoted = 0;
            foreach (var note in match.Notes)
            {
                if (note.Kind == "goal") goalsNoted++;
                else if (note.Kind == "foul") row.Fouls++;
                else if (note.Kind == "yellow") row.Yellows++;
                else if (note.Kind == "red") row.Reds++;
                else if (note.Kind == "corner") row.Corners++;
                else if (note.Kind == "throwin") row.ThrowIns++;
                else if (note.Kind == "offside") row.Offsides++;
                else if (note.Kind == "injury") row.Injuries++;
                else if (note.Kind == "sub") row.Substitutions++;
            }

            row.GoalNotes = goalsNoted;
            foreach (var shot in match.Shots)
            {
                if (shot.Outcome == "goal" || shot.Outcome == "saved")
                    row.ShotsOnTarget++;
            }

            foreach (var pass in match.Passes)
            {
                row.Passes++;
                if (pass.Completed) row.PassesCompleted++;
                else if (!pass.Resolved) row.PassesUnresolved++;
            }

            row.Incoherent = row.HomeGoals + row.AwayGoals != goalsNoted
                || row.HomeShots + row.AwayShots != row.ShotEvents
                || row.PassesCompleted > row.Passes
                || row.PassesUnresolved > row.Passes;
            if (row.Incoherent && string.IsNullOrEmpty(row.Error))
                row.Error = "O marcador, os remates ou os passes não batem com os eventos.";
        }

        static CareerState BuildCareer(Scenario scenario, int seed)
        {
            var world = new WorldState();
            var home = Club("sim-home", "Casa", scenario.HomeFormation, scenario.HomePreset, scenario.HomeReputation);
            var away = Club("sim-away", "Fora", scenario.AwayFormation, scenario.AwayPreset, scenario.AwayReputation);
            world.Clubs[home.Id] = home;
            world.Clubs[away.Id] = away;
            AddSquad(world, home, seed * 1000, scenario.HomeReputation, scenario.HomeFatigue, scenario.FlatHome);
            AddSquad(world, away, seed * 1000 + 500, scenario.AwayReputation, scenario.AwayFatigue, scenario.FlatAway);
            var career = new CareerState
            {
                World = world,
                Coach = new Coach { Name = "Simulacao", ClubId = home.Id },
                Calendar = new GameCalendar(new DateTime(2026, 8, 1))
            };
            career.Fixtures.Add(new Fixture
            {
                Id = "sim|" + scenario.Name + "|" + seed.ToString(CultureInfo.InvariantCulture),
                HomeClubId = home.Id,
                AwayClubId = away.Id,
                LeagueId = "league:sim",
                Date = new DateTime(2026, 8, 1)
            });
            return career;
        }

        static Club Club(string id, string name, string formation, int preset, int reputation)
        {
            var tactics = new TacticalIdentity { Formation = formation };
            TacticPresets.Apply(tactics, preset);
            tactics.Formation = formation;
            return new Club
            {
                Id = id,
                Name = name,
                ShortName = name,
                LeagueId = "league:sim",
                Reputation = reputation,
                Tactics = tactics,
                JerseyPrimaryHex = "#2244aa",
                JerseySecondaryHex = "#ffffff",
                JerseyShortsHex = "#ffffff"
            };
        }

        static readonly PlayerPosition[] Xi =
        {
            PlayerPosition.Goalkeeper, PlayerPosition.RightBack, PlayerPosition.CentreBack, PlayerPosition.CentreBack, PlayerPosition.LeftBack,
            PlayerPosition.DefensiveMidfielder, PlayerPosition.CentralMidfielder, PlayerPosition.CentralMidfielder,
            PlayerPosition.RightWinger, PlayerPosition.LeftWinger, PlayerPosition.Striker,
            PlayerPosition.CentreBack, PlayerPosition.CentralMidfielder, PlayerPosition.Striker, PlayerPosition.LeftBack, PlayerPosition.RightMidfielder
        };

        static void AddSquad(WorldState world, Club club, int seedBase, int reputation, int fatigue, int flat)
        {
            for (var i = 0; i < Xi.Length; i++)
            {
                var seed = seedBase + i * 17;
                var player = new Player
                {
                    Id = club.Id + "-" + (i + 1).ToString(CultureInfo.InvariantCulture),
                    ClubId = club.Id,
                    FullName = "Jogador " + (i + 1).ToString(CultureInfo.InvariantCulture),
                    Age = 24,
                    SquadNumber = i + 1,
                    Position = Xi[i],
                    Nationality = "Portugal",
                    Morale = 60,
                    Fitness = fatigue >= 80 ? 45 : 80,
                    Form = 60,
                    Fatigue = fatigue,
                    Attributes = PlayerAttributes.Roll(seed, Xi[i], reputation),
                    IsCaptain = i == 6
                };
                if (flat > 0)
                    Force(player.Attributes, flat);
                world.Players[player.Id] = player;
                club.PlayerIds.Add(player.Id);
            }
        }

        static void Force(PlayerAttributes attributes, int value)
        {
            attributes.Pace = value;
            attributes.Stamina = value;
            attributes.Strength = value;
            attributes.Passing = value;
            attributes.Finishing = value;
            attributes.Dribbling = value;
            attributes.Tackling = value;
            attributes.Crossing = value;
            attributes.Positioning = value;
            attributes.Composure = value;
            attributes.WorkRate = value;
            attributes.Decisions = value;
            attributes.Reflexes = value;
            attributes.Handling = value;
        }

        static int MeanOverall(MatchSession match, bool home)
        {
            var ids = home ? match.HomeOnField : match.AwayOnField;
            if (ids.Count == 0)
                return 0;
            var sum = 0;
            foreach (var id in ids)
                sum += match.People.TryGetValue(id, out var player) ? PlayerAttributes.Overall(player) : 0;
            return sum / ids.Count;
        }

        static int StableSeed(string value)
        {
            unchecked
            {
                var hash = 2166136261;
                var text = value ?? "";
                for (var i = 0; i < text.Length; i++)
                    hash = (hash ^ text[i]) * 16777619;
                return (int)hash;
            }
        }

        static bool IsFinite(Vector2 value)
        {
            return !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsInfinity(value.x) && !float.IsInfinity(value.y);
        }

        static float PitchMetres(Vector2 from, Vector2 to)
        {
            var dx = (to.x - from.x) * 105f;
            var dy = (to.y - from.y) * 68f;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        static string Summary(List<MatchRow> rows, int seeds, int failures, string jsonlPath)
        {
            var builder = new StringBuilder();
            builder.Append("{\n");
            builder.Append("  \"seedsPerScenario\": ").Append(seeds.ToString(CultureInfo.InvariantCulture)).Append(",\n");
            builder.Append("  \"matches\": ").Append(rows.Count.ToString(CultureInfo.InvariantCulture)).Append(",\n");
            builder.Append("  \"failures\": ").Append(failures.ToString(CultureInfo.InvariantCulture)).Append(",\n");
            builder.Append("  \"tickDt\": 0.2,\n");
            builder.Append("  \"minutesPerSecond\": 0.68,\n");
            builder.Append("  \"assumptions\": \"A distância trata o campo normalizado como 105 por 68 metros. Um salto acima de 40 metros entre ticks de 0,2 s conta como movimento abrupto. O portador perto do destino do passe é uma amostra em que a ação é passe ou cruzamento e o portador está a menos de 0,12 do alvo da bola. substitutionApplied é só a substituição pedida pelo teste ao minuto 60. substitutions conta todas as notas de substituição, incluindo lesões.\",\n");
            builder.Append("  \"matchesFile\": ").Append(Quote(jsonlPath)).Append(",\n");
            builder.Append("  \"scenarios\": [\n");
            for (var s = 0; s < Scenarios.Length; s++)
            {
                var name = Scenarios[s].Name;
                var subset = new List<MatchRow>();
                foreach (var row in rows)
                {
                    if (row.Scenario == name)
                        subset.Add(row);
                }

                builder.Append("    { \"name\": ").Append(Quote(name));
                builder.Append(", \"played\": ").Append(subset.Count.ToString(CultureInfo.InvariantCulture));
                builder.Append(", \"finished\": ").Append(Count(subset, item => item.Finished).ToString(CultureInfo.InvariantCulture));
                builder.Append(", \"meanGoals\": ").Append(Num(Mean(subset, item => item.HomeGoals + item.AwayGoals)));
                builder.Append(", \"meanShots\": ").Append(Num(Mean(subset, item => item.HomeShots + item.AwayShots)));
                builder.Append(", \"meanShotsOnTarget\": ").Append(Num(Mean(subset, item => item.ShotsOnTarget)));
                builder.Append(", \"meanPasses\": ").Append(Num(Mean(subset, item => item.Passes)));
                builder.Append(", \"passCompletion\": ").Append(Num(Rate(subset)));
                builder.Append(", \"meanFouls\": ").Append(Num(Mean(subset, item => item.Fouls)));
                builder.Append(", \"meanYellows\": ").Append(Num(Mean(subset, item => item.Yellows)));
                builder.Append(", \"meanHomeMetres\": ").Append(Num(Mean(subset, item => item.HomeMetres)));
                builder.Append(", \"meanAwayMetres\": ").Append(Num(Mean(subset, item => item.AwayMetres)));
                builder.Append(", \"meanAbruptMoves\": ").Append(Num(Mean(subset, item => item.AbruptMoves)));
                builder.Append(", \"carrierFollowsPass\": ").Append(Num(FollowRate(subset)));
                builder.Append(", \"meanHomeGoals\": ").Append(Num(Mean(subset, item => item.HomeGoals)));
                builder.Append(", \"meanAwayGoals\": ").Append(Num(Mean(subset, item => item.AwayGoals)));
                builder.Append(", \"meanSubstitutions\": ").Append(Num(Mean(subset, item => item.Substitutions)));
                builder.Append(", \"scriptedSubstitutions\": ").Append(Count(subset, item => item.SubstitutionApplied).ToString(CultureInfo.InvariantCulture));
                builder.Append(" }");
                if (s < Scenarios.Length - 1)
                    builder.Append(',');
                builder.Append('\n');
            }

            builder.Append("  ]\n}\n");
            return builder.ToString();
        }

        static int Count(List<MatchRow> rows, Func<MatchRow, bool> predicate)
        {
            var count = 0;
            foreach (var row in rows)
            {
                if (predicate(row))
                    count++;
            }

            return count;
        }

        static double Mean(List<MatchRow> rows, Func<MatchRow, double> value)
        {
            if (rows.Count == 0)
                return 0;
            double sum = 0;
            foreach (var row in rows)
                sum += value(row);
            return sum / rows.Count;
        }

        static double Rate(List<MatchRow> rows)
        {
            double passes = 0;
            double completed = 0;
            foreach (var row in rows)
            {
                passes += row.Passes;
                completed += row.PassesCompleted;
            }

            return passes <= 0 ? 0 : completed / passes;
        }

        static double FollowRate(List<MatchRow> rows)
        {
            double samples = 0;
            double near = 0;
            foreach (var row in rows)
            {
                samples += row.PassFlightSamples;
                near += row.CarrierNearPassTarget;
            }

            return samples <= 0 ? 0 : near / samples;
        }

        static string Num(double value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        static string Quote(string value)
        {
            return "\"" + (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        struct Scenario
        {
            public string Name;
            public string HomeFormation;
            public string AwayFormation;
            public int HomeReputation;
            public int AwayReputation;
            public int HomePreset;
            public int AwayPreset;
            public int HomeFatigue;
            public int AwayFatigue;
            public int FlatHome;
            public int FlatAway;
            public bool SubstituteAt60;

            public Scenario(string name, string homeFormation, string awayFormation, int homeReputation, int awayReputation, int homePreset, int awayPreset, int homeFatigue, int awayFatigue, int flatHome, int flatAway, bool substituteAt60)
            {
                Name = name;
                HomeFormation = homeFormation;
                AwayFormation = awayFormation;
                HomeReputation = homeReputation;
                AwayReputation = awayReputation;
                HomePreset = homePreset;
                AwayPreset = awayPreset;
                HomeFatigue = homeFatigue;
                AwayFatigue = awayFatigue;
                FlatHome = flatHome;
                FlatAway = flatAway;
                SubstituteAt60 = substituteAt60;
            }
        }

        sealed class MatchRow
        {
            public string Scenario;
            public int Seed;
            public string FixtureId;
            public string HomeFormation;
            public string AwayFormation;
            public string HomePreset;
            public string AwayPreset;
            public int HomeOverall;
            public int AwayOverall;
            public int HomeFatigue;
            public int AwayFatigue;
            public bool Finished;
            public bool Incoherent;
            public bool SubstitutionApplied;
            public int Ticks;
            public float Minute;
            public int HomeGoals;
            public int AwayGoals;
            public int GoalNotes;
            public int HomeShots;
            public int AwayShots;
            public int ShotEvents;
            public int ShotsOnTarget;
            public int Passes;
            public int PassesCompleted;
            public int PassesUnresolved;
            public int Fouls;
            public int Yellows;
            public int Reds;
            public int Corners;
            public int ThrowIns;
            public int Offsides;
            public int Injuries;
            public int Substitutions;
            public int PositionSamples;
            public int InvalidCoordinates;
            public int AbruptMoves;
            public int PassFlightSamples;
            public int CarrierNearPassTarget;
            public float HomeMetres;
            public float AwayMetres;
            public string Error;

            public string ToJson()
            {
                var builder = new StringBuilder();
                builder.Append('{');
                Field(builder, "scenario", Scenario, true);
                Field(builder, "seed", Seed);
                Field(builder, "fixtureId", FixtureId);
                Field(builder, "homeFormation", HomeFormation);
                Field(builder, "awayFormation", AwayFormation);
                Field(builder, "homePreset", HomePreset);
                Field(builder, "awayPreset", AwayPreset);
                Field(builder, "homeOverall", HomeOverall);
                Field(builder, "awayOverall", AwayOverall);
                Field(builder, "homeFatigue", HomeFatigue);
                Field(builder, "awayFatigue", AwayFatigue);
                Field(builder, "finished", Finished);
                Field(builder, "incoherent", Incoherent);
                Field(builder, "substitutionApplied", SubstitutionApplied);
                Field(builder, "ticks", Ticks);
                Field(builder, "minute", Minute);
                Field(builder, "homeGoals", HomeGoals);
                Field(builder, "awayGoals", AwayGoals);
                Field(builder, "goalNotes", GoalNotes);
                Field(builder, "homeShots", HomeShots);
                Field(builder, "awayShots", AwayShots);
                Field(builder, "shotEvents", ShotEvents);
                Field(builder, "shotsOnTarget", ShotsOnTarget);
                Field(builder, "passes", Passes);
                Field(builder, "passesCompleted", PassesCompleted);
                Field(builder, "passesUnresolved", PassesUnresolved);
                Field(builder, "fouls", Fouls);
                Field(builder, "yellows", Yellows);
                Field(builder, "reds", Reds);
                Field(builder, "corners", Corners);
                Field(builder, "throwIns", ThrowIns);
                Field(builder, "offsides", Offsides);
                Field(builder, "injuries", Injuries);
                Field(builder, "substitutions", Substitutions);
                Field(builder, "positionSamples", PositionSamples);
                Field(builder, "invalidCoordinates", InvalidCoordinates);
                Field(builder, "abruptMoves", AbruptMoves);
                Field(builder, "passFlightSamples", PassFlightSamples);
                Field(builder, "carrierNearPassTarget", CarrierNearPassTarget);
                Field(builder, "homeMetres", HomeMetres);
                Field(builder, "awayMetres", AwayMetres);
                Field(builder, "error", Error ?? "");
                builder.Append('}');
                return builder.ToString();
            }

            static void Field(StringBuilder builder, string name, string value, bool first = false)
            {
                if (!first)
                    builder.Append(',');
                builder.Append('"').Append(name).Append("\":").Append(Quote(value));
            }

            static void Field(StringBuilder builder, string name, int value)
            {
                builder.Append(",\"").Append(name).Append("\":").Append(value.ToString(CultureInfo.InvariantCulture));
            }

            static void Field(StringBuilder builder, string name, float value)
            {
                builder.Append(",\"").Append(name).Append("\":").Append(value.ToString("0.####", CultureInfo.InvariantCulture));
            }

            static void Field(StringBuilder builder, string name, bool value)
            {
                builder.Append(",\"").Append(name).Append("\":").Append(value ? "true" : "false");
            }
        }
    }
}
