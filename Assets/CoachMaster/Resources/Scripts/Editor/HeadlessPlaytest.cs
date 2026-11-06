using System;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using CoachMaster.Career;
using CoachMaster.Data;
using CoachMaster.Domain;
using CoachMaster.MatchPlay;
using CoachMaster.MatchPlay.Spatial;
using UnityEditor;
using UnityEngine;

namespace CoachMaster.EditorTools
{
    public static class HeadlessPlaytest
    {
        public static void RunFromCommandLine()
        {
            var failures = 0;
            var report = new StringBuilder();
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "HeadlessReports"));
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, "playtest-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".txt");
            try
            {
                failures += OnlineRules(report);
                failures += Lan(report);
                failures += Ball(report);
                failures += Mods(report);
                failures += SpatialContact(report);
            }
            catch (Exception error)
            {
                failures++;
                report.AppendLine("FAIL excepcao inicial: " + error);
            }

            File.WriteAllText(path, report.ToString(), Encoding.UTF8);
            try
            {
                var summary = MatchSimulationBatch.Run(20);
                report.AppendLine("LOTE " + summary);
                var text = File.ReadAllText(summary);
                var mark = "\"failures\": ";
                var at = text.IndexOf(mark, StringComparison.Ordinal);
                var count = -1;
                if (at >= 0)
                {
                    var end = text.IndexOf(',', at + mark.Length);
                    int.TryParse(text.Substring(at + mark.Length, end - at - mark.Length).Trim(), out count);
                }
                report.AppendLine("LOTE falhas=" + count.ToString(CultureInfo.InvariantCulture));
                if (count != 0)
                    failures++;
            }
            catch (Exception error)
            {
                failures++;
                report.AppendLine("FAIL lote: " + error);
            }

            report.AppendLine("TOTAL falhas=" + failures.ToString(CultureInfo.InvariantCulture));
            File.WriteAllText(path, report.ToString(), Encoding.UTF8);
            Debug.Log(report.ToString());
            if (Application.isBatchMode)
                EditorApplication.Exit(failures > 0 ? 1 : 0);
        }

        static int OnlineRules(StringBuilder report)
        {
            var failed = 0;
            var soloCareer = Sample(null);
            soloCareer.Online = null;
            failed += Expect(report, "solo nao bloqueia", !CareerOnline.Check(soloCareer, false).Stop);
            var one = Sample(null);
            one.Online = new CareerOnline();
            one.Online.Seats.Add(Seat("Ana", "casa", true));
            failed += Expect(report, "um so lugar nao bloqueia", !CareerOnline.Check(one, false).Stop);

            var wait = Sample(new DateTime(2026, 8, 16, 15, 0, 0));
            var first = CareerOnline.Check(wait, false);
            failed += Expect(report, "dia sem jogo pede o outro continuar", first.Wait == OnlineWait.Partner && first.Message.Contains("Bruno"));
            wait.Online.Seats[1].ContinueReady = true;
            var second = CareerOnline.Check(wait, false);
            failed += Expect(report, "os dois prontos deixam avancar", !second.Stop && !wait.Online.Seats[0].ContinueReady && !wait.Online.Seats[1].ContinueReady);

            var sameDay = Sample(new DateTime(2026, 8, 15, 15, 0, 0), new DateTime(2026, 8, 15, 18, 0, 0));
            var homeBlocked = CareerOnline.Check(sameDay, true);
            failed += Expect(report, "quem joga mais tarde espera", homeBlocked.Wait == OnlineWait.OtherMatch && homeBlocked.Message.Contains("Bruno"));
            sameDay.Coach.ClubId = "fora";
            sameDay.Online.Seats[0].Local = false;
            sameDay.Online.Seats[1].Local = true;
            var awayStarts = CareerOnline.Check(sameDay, true);
            failed += Expect(report, "quem tem o jogo mais cedo pode comecar", !awayStarts.Stop);
            sameDay.Fixtures[0].Played = true;
            sameDay.Coach.ClubId = "casa";
            sameDay.Online.Seats[0].Local = true;
            sameDay.Online.Seats[1].Local = false;
            var laterStarts = CareerOnline.Check(sameDay, true);
            failed += Expect(report, "depois desse jogo o outro pode comecar", !laterStarts.Stop && CareerOnline.OwnsUpcoming(sameDay));

            var clock = Sample(null, new DateTime(2026, 8, 15, 18, 0, 0));
            clock.Online.Mode = "hora";
            var ahead = DateTime.Now.AddMinutes(25);
            if (ahead.Date == DateTime.Now.Date && ahead > DateTime.Now)
            {
                clock.Online.KickoffHour = ahead.Hour;
                clock.Online.KickoffMinute = ahead.Minute;
                var blocked = CareerOnline.Check(clock, true);
                failed += Expect(report, "hora marcada ainda nao chegou", blocked.Wait == OnlineWait.Clock);
            }
            else
                report.AppendLine("NOTA hora futura nao coube no dia de hoje");
            var behind = DateTime.Now.AddMinutes(-20);
            if (behind.Date != DateTime.Now.Date)
                behind = DateTime.Now.Date;
            clock.Online.KickoffHour = behind.Hour;
            clock.Online.KickoffMinute = behind.Minute;
            failed += Expect(report, "hora marcada ja passou", !CareerOnline.Check(clock, true).Stop);

            var packed = wait.Online.Pack();
            var parsed = CareerOnline.Parse(packed);
            failed += Expect(report, "sala gravada e lida", parsed.Seats.Count == 2 && parsed.Seats[1].ClubId == "fora" && parsed.Seats[1].Color == "#E07A3D");
            failed += Expect(report, "jogo do outro humano nao e simulado como teu", CareerOnline.OtherHuman(wait, wait.Fixtures[0]));
            CareerOnline.PendingClub = "fora";
            CareerOnline.BindMachine(wait);
            failed += Expect(report, "convidado fica no clube dele", wait.Coach.ClubId == "fora" && wait.Online.Seats[1].Local);
            CareerOnline.PendingClub = null;
            var unprotected = Sample(new DateTime(2026, 8, 16, 15, 0, 0));
            unprotected.Online = null;
            failed += Expect(report, "sem sala o outro clube nao fica protegido", !CareerOnline.OtherHuman(unprotected, unprotected.Fixtures[0]));
            return failed;
        }

        static int Lan(StringBuilder report)
        {
            TcpClient client = null;
            try
            {
                LanRoom.Stop();
                const int port = 47123;
                LanRoom.StartHost(port);
                if (!LanRoom.IsHost)
                {
                    report.AppendLine("FAIL servidor nao abriu: " + LanRoom.Status);
                    return 1;
                }

                client = new TcpClient();
                var connected = false;
                var until = DateTime.UtcNow.AddSeconds(3);
                while (DateTime.UtcNow < until && !connected)
                {
                    try
                    {
                        client.Connect("127.0.0.1", port);
                        connected = true;
                    }
                    catch
                    {
                        client = new TcpClient();
                        Thread.Sleep(30);
                    }
                }
                if (!connected)
                {
                    report.AppendLine("FAIL cliente nao ligou ao servidor local");
                    return 1;
                }

                var stream = client.GetStream();
                var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
                writer.WriteLine("JOIN|Teste");
                string join = null;
                until = DateTime.UtcNow.AddSeconds(3);
                while (DateTime.UtcNow < until && string.IsNullOrEmpty(join))
                {
                    string line;
                    if (LanRoom.TryTake(out line))
                        join = line;
                    else
                        Thread.Sleep(20);
                }
                if (join == null || !join.StartsWith("JOIN|", StringComparison.Ordinal) || !join.EndsWith("|Teste", StringComparison.Ordinal))
                {
                    report.AppendLine("FAIL convite nao chegou: " + (join ?? "(vazio)"));
                    return 1;
                }
                report.AppendLine("PASS convite chegou ao anfitriao: " + join);
                var bits = join.Split('|');
                int id;
                if (bits.Length < 2 || !int.TryParse(bits[1], out id))
                {
                    report.AppendLine("FAIL convite sem numero de ligacao");
                    return 1;
                }
                LanRoom.Remember(id, "fora");
                LanRoom.Reply(id, "SEAT|fora|#E07A3D|SALA");
                stream.ReadTimeout = 2000;
                var reader = new StreamReader(stream, Encoding.UTF8);
                var seat = reader.ReadLine();
                if (seat != "SEAT|fora|#E07A3D|SALA")
                {
                    report.AppendLine("FAIL resposta da sala: " + seat);
                    return 1;
                }
                report.AppendLine("PASS anfitriao respondeu com o lugar e a cor");
                writer.WriteLine("READY");
                string ready = null;
                until = DateTime.UtcNow.AddSeconds(3);
                while (DateTime.UtcNow < until && string.IsNullOrEmpty(ready))
                {
                    string line;
                    if (LanRoom.TryTake(out line))
                        ready = line;
                    else
                        Thread.Sleep(20);
                }
                if (ready != "READY|" + id.ToString(CultureInfo.InvariantCulture))
                {
                    report.AppendLine("FAIL continuar do convidado: " + (ready ?? "(vazio)"));
                    return 1;
                }
                report.AppendLine("PASS continuar do convidado chegou");
                return 0;
            }
            catch (Exception error)
            {
                report.AppendLine("FAIL rede: " + error.GetType().Name + ": " + error.Message);
                return 1;
            }
            finally
            {
                if (client != null)
                {
                    try { client.Close(); }
                    catch { }
                }
                LanRoom.Stop();
            }
        }

        static int SpatialContact(StringBuilder report)
        {
            var failed = 0;
            var elite = new AttributeScale
            {
                Passing = 85f, Vision = 80f, Composure = 78f, FirstTouch = 80f, Crossing = 70f,
                Finishing = 84f, Dribbling = 70f, Tackling = 40f, Anticipation = 60f, Strength = 70f,
                Agility = 72f, Marking = 40f, Aggression = 40f, Reflexes = 40f, Handling = 40f, Positioning = 70f
            };
            var average = elite;
            average.Passing = 50f;
            average.Vision = 50f;
            average.Composure = 50f;
            average.Finishing = 50f;
            var keeper = new AttributeScale { Reflexes = 70f, Handling = 70f, Positioning = 68f };
            var shortPass = ContactModel.PassMissChance(elite, 12f, 0.1f, false, false, false);
            var longPass = ContactModel.PassMissChance(average, 42f, 0.85f, true, true, false);
            failed += Expect(report, "passe curto falha menos que o longo pressionado", shortPass < longPass);
            failed += Expect(report, "passe curto ainda pode falhar", shortPass >= 0.02f && shortPass < 0.12f);
            var close = ContactModel.ShotChance(new Vector2(0.5f, 0.9f), true, 0, 2f, elite, keeper, 0.1f);
            var far = ContactModel.ShotChance(new Vector2(0.2f, 0.62f), true, 2, 1f, average, keeper, 0.7f);
            failed += Expect(report, "remate perto vale mais que remate de longe", close > far);
            failed += Expect(report, "xG de um remate cabe no modelo", close > 0.04f && close < 0.62f && far > 0.005f && far < close);
            var rngA = new System.Random(42);
            var rngB = new System.Random(42);
            var strong = new AttributeScale { Tackling = 85f, Anticipation = 80f, Strength = 82f, Marking = 80f, Aggression = 40f };
            var weak = new AttributeScale { Dribbling = 35f, Composure = 40f, Agility = 40f, Strength = 45f };
            var one = ContactModel.Duel(rngA, strong, weak, false);
            var two = ContactModel.Duel(rngB, strong, weak, false);
            failed += Expect(report, "duelo com a mesma semente repete", one == two);
            failed += Expect(report, "duelo devolve um resultado", one == "won" || one == "foul" || one == "none");
            var lane = ContactModel.PointSegmentMeters(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.8f));
            failed += Expect(report, "bola na linha do passe está ao alcance", lane < 0.2f);
            return failed;
        }

        static int Ball(StringBuilder report)
        {
            try
            {
                var career = PitchCareer();
                var match = MatchSession.Create(career, career.Fixtures[0]);
                var engine = new CoachMaster.MatchPlay.MatchEngine(match, career.Coach.ClubId, 1f, 1f, 24681357);
                var previous = engine.Ball;
                var ballMetres = 0f;
                var playerMetres = 0f;
                var homePrevious = new Vector2[11];
                var seen = false;
                var invalid = 0;
                var passSamples = 0;
                var carrierNearTarget = 0;
                for (var tick = 0; tick < 700 && !match.Finished; tick++)
                {
                    if (match.Phase == MatchPhase.HalfTime)
                        engine.StartSecondHalf();
                    engine.Tick(0.2f, 0.68f);
                    if (!Finite(engine.Ball))
                        invalid++;
                    ballMetres += Metres(previous, engine.Ball);
                    previous = engine.Ball;
                    for (var i = 0; i < 11; i++)
                    {
                        var home = engine.MotionOf(true, i);
                        var away = engine.MotionOf(false, i);
                        if (!Finite(home) || !Finite(away))
                            invalid++;
                        if (seen)
                            playerMetres += Metres(homePrevious[i], home);
                        homePrevious[i] = home;
                    }
                    seen = true;
                    if (engine.ActionKind == "pass" || engine.ActionKind == "cross")
                    {
                        passSamples++;
                        var carrier = engine.MotionOf(match.HomeHasBall, Mathf.Clamp(match.BallHolder, 0, 10));
                        if (Vector2.Distance(carrier, engine.BallTarget) < 0.12f)
                            carrierNearTarget++;
                    }
                }

                report.AppendLine("BOLA metros=" + ballMetres.ToString("0.0", CultureInfo.InvariantCulture)
                    + " jogadores=" + playerMetres.ToString("0.0", CultureInfo.InvariantCulture)
                    + " minuto=" + match.Minute.ToString("0.0", CultureInfo.InvariantCulture)
                    + " golos=" + match.HomeGoals + "-" + match.AwayGoals
                    + " remates=" + (match.HomeShots + match.AwayShots)
                    + " passes=" + passSamples
                    + " portador-no-alvo=" + carrierNearTarget
                    + " invalidos=" + invalid);
                var failed = 0;
                failed += Expect(report, "bola e jogadores com coordenadas validas", invalid == 0);
                failed += Expect(report, "a bola andou no campo", ballMetres > 30f && match.Minute > 8f);
                failed += Expect(report, "os jogadores andaram", playerMetres > 80f);
                return failed;
            }
            catch (Exception error)
            {
                report.AppendLine("FAIL bola: " + error);
                return 1;
            }
        }

        static int Mods(StringBuilder report)
        {
            try
            {
                var career = Sample(null);
                var before = career.World.Clubs["casa"].Name;
                ModFolder.Apply(career);
                var face = ModFolder.Portrait("Jogador Que Nao Existe");
                var crest = ModFolder.Crest("Casa");
                return Expect(report, "mods vazios nao mudam o clube nem inventam cara", career.World.Clubs["casa"].Name == before && face == null && crest == null);
            }
            catch (Exception error)
            {
                report.AppendLine("FAIL mods: " + error.Message);
                return 1;
            }
        }

        static CareerState Sample(DateTime? first, DateTime? second = null)
        {
            var world = new WorldState();
            world.Clubs["casa"] = new Club { Id = "casa", Name = "Casa", ShortName = "Casa" };
            world.Clubs["fora"] = new Club { Id = "fora", Name = "Fora", ShortName = "Fora" };
            world.Clubs["bot"] = new Club { Id = "bot", Name = "Bot", ShortName = "Bot" };
            var career = new CareerState
            {
                World = world,
                Coach = new Coach { Name = "Ana", ClubId = "casa" },
                Calendar = new GameCalendar(new DateTime(2026, 8, 15, 11, 0, 0))
            };
            career.Online = new CareerOnline { Mode = "tempo-real", Port = 7777, RoomCode = "SALA" };
            career.Online.Seats.Add(Seat("Ana", "casa", true));
            career.Online.Seats.Add(Seat("Bruno", "fora", false));
            if (first.HasValue)
            {
                career.Fixtures.Add(new Fixture
                {
                    Id = "a",
                    HomeClubId = "fora",
                    AwayClubId = "bot",
                    Date = first.Value
                });
            }
            if (second.HasValue)
            {
                career.Fixtures.Add(new Fixture
                {
                    Id = "b",
                    HomeClubId = "casa",
                    AwayClubId = "bot",
                    Date = second.Value
                });
            }
            return career;
        }

        static OnlineSeat Seat(string name, string club, bool local)
        {
            return new OnlineSeat { Name = name, ClubId = club, Color = local ? "#F4F1E8" : "#E07A3D", Local = local };
        }

        static CareerState PitchCareer()
        {
            var world = new WorldState();
            var home = Side("sim-home", "Casa");
            var away = Side("sim-away", "Fora");
            world.Clubs[home.Id] = home;
            world.Clubs[away.Id] = away;
            Squad(world, home, 1000);
            Squad(world, away, 2000);
            var career = new CareerState
            {
                World = world,
                Coach = new Coach { Name = "Simulacao", ClubId = home.Id },
                Calendar = new GameCalendar(new DateTime(2026, 8, 1, 15, 0, 0))
            };
            career.Fixtures.Add(new Fixture
            {
                Id = "bola",
                HomeClubId = home.Id,
                AwayClubId = away.Id,
                LeagueId = "league:sim",
                Date = new DateTime(2026, 8, 1, 15, 0, 0)
            });
            return career;
        }

        static Club Side(string id, string name)
        {
            var tactics = new TacticalIdentity { Formation = "4-3-3" };
            TacticPresets.Apply(tactics, 0);
            tactics.Formation = "4-3-3";
            return new Club
            {
                Id = id,
                Name = name,
                ShortName = name,
                LeagueId = "league:sim",
                Reputation = 62,
                Tactics = tactics
            };
        }

        static readonly PlayerPosition[] Xi =
        {
            PlayerPosition.Goalkeeper, PlayerPosition.RightBack, PlayerPosition.CentreBack, PlayerPosition.CentreBack, PlayerPosition.LeftBack,
            PlayerPosition.DefensiveMidfielder, PlayerPosition.CentralMidfielder, PlayerPosition.CentralMidfielder,
            PlayerPosition.RightWinger, PlayerPosition.LeftWinger, PlayerPosition.Striker,
            PlayerPosition.CentreBack, PlayerPosition.CentralMidfielder, PlayerPosition.Striker, PlayerPosition.LeftBack, PlayerPosition.RightMidfielder
        };

        static void Squad(WorldState world, Club club, int seedBase)
        {
            for (var i = 0; i < Xi.Length; i++)
            {
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
                    Fitness = 80,
                    Form = 60,
                    Fatigue = 20,
                    Attributes = PlayerAttributes.Roll(seedBase + i * 17, Xi[i], 62),
                    IsCaptain = i == 6
                };
                world.Players[player.Id] = player;
                club.PlayerIds.Add(player.Id);
            }
        }

        static bool Finite(Vector2 value)
        {
            return !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsInfinity(value.x) && !float.IsInfinity(value.y);
        }

        static float Metres(Vector2 from, Vector2 to)
        {
            var dx = (to.x - from.x) * 105f;
            var dy = (to.y - from.y) * 68f;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        static int Expect(StringBuilder report, string name, bool ok)
        {
            report.AppendLine((ok ? "PASS " : "FAIL ") + name);
            return ok ? 0 : 1;
        }
    }
}
