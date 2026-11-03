using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using CoachMaster.Career;
using CoachMaster.Domain;
using CoachMaster.UI;
using UnityEngine;

namespace CoachMaster.Data
{
    public sealed class ModPackageFile
    {
        public string SourcePath;
        public string DestinationPath;
    }

    public sealed class ModPackageCopyJob : IDisposable
    {
        FileStream _source;
        FileStream _destination;
        string _destinationPath;
        string _temporaryPath;

        public long TotalBytes { get; private set; }
        public long CopiedBytes { get; private set; }
        public bool IsComplete { get; private set; }

        public bool Open(ModPackageFile file, out string error)
        {
            error = "";
            try
            {
                if (!ModFolder.GetPackageDestination(file, out var sourcePath, out var destinationPath, out error)) return false;
                var sourceFullPath = Path.GetFullPath(sourcePath);
                if (string.Equals(sourceFullPath, Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase))
                {
                    IsComplete = true;
                    return true;
                }
                _destinationPath = destinationPath;
                _temporaryPath = destinationPath + ".codex-importing";
                TotalBytes = new FileInfo(sourceFullPath).Length;
                _source = new FileStream(sourceFullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 256 * 1024, FileOptions.SequentialScan);
                _destination = new FileStream(_temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, 256 * 1024, FileOptions.SequentialScan);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                Dispose();
                return false;
            }
        }

        public bool CopyChunk(byte[] buffer, out int bytesCopied, out string error)
        {
            bytesCopied = 0;
            error = "";
            if (IsComplete) return true;
            if (_source == null || _destination == null)
            {
                error = "The package copy was not opened.";
                return false;
            }
            try
            {
                bytesCopied = _source.Read(buffer, 0, buffer.Length);
                if (bytesCopied > 0)
                {
                    _destination.Write(buffer, 0, bytesCopied);
                    CopiedBytes += bytesCopied;
                    return true;
                }
                _destination.Flush();
                DisposeStreams();
                if (File.Exists(_destinationPath))
                {
                    try
                    {
                        File.Replace(_temporaryPath, _destinationPath, null);
                    }
                    catch
                    {
                        var backupPath = _destinationPath + ".codex-backup";
                        if (File.Exists(backupPath)) File.Delete(backupPath);
                        File.Move(_destinationPath, backupPath);
                        try
                        {
                            File.Move(_temporaryPath, _destinationPath);
                            File.Delete(backupPath);
                        }
                        catch
                        {
                            if (!File.Exists(_destinationPath) && File.Exists(backupPath)) File.Move(backupPath, _destinationPath);
                            throw;
                        }
                    }
                }
                else File.Move(_temporaryPath, _destinationPath);
                IsComplete = true;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                Dispose();
                DeleteTemporaryFile();
                return false;
            }
        }

        void DisposeStreams()
        {
            _source?.Dispose();
            _source = null;
            _destination?.Dispose();
            _destination = null;
        }

        public void Dispose()
        {
            DisposeStreams();
            if (!IsComplete) DeleteTemporaryFile();
        }

        void DeleteTemporaryFile()
        {
            try { if (!string.IsNullOrEmpty(_temporaryPath) && File.Exists(_temporaryPath)) File.Delete(_temporaryPath); }
            catch { }
        }
    }

    public static class ModFolder
    {
        static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        public static string Root()
        {
            var project = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Mods");
            if (Directory.Exists(project))
                return project;
            var saved = Path.Combine(Application.persistentDataPath, "Mods");
            Directory.CreateDirectory(saved);
            return saved;
        }

        public static void Apply(CareerState career)
        {
            if (career?.World == null)
                return;
            Apply(career.World);
        }

        public static void Apply(WorldState world)
        {
            if (world == null)
                return;
            WorldCatalog.NormalizeGeneratedStaff(world);
            WorldExtraCatalog.EnsureSponsors(world);
            ApplyClubs(world);
            ApplyPlayers(world);
            ApplyManagers(world);
            ApplyStaff(world);
            ApplySponsors(world);
            FaceBook.InvalidateImportedCaches();
        }

        public static Sprite Portrait(string fullName)
        {
            return Picture("caras", fullName);
        }

        public static Sprite PortraitFile(string fileKey)
        {
            return Picture("caras", fileKey);
        }

        public static List<string> PortraitKeys()
        {
            return ImageKeys("caras");
        }

        public static Sprite Crest(string clubName)
        {
            return Picture("emblemas", clubName);
        }

        public static List<string> CrestKeys()
        {
            return ImageKeys("emblemas");
        }

        public static Sprite SponsorLogo(string logoKey)
        {
            return Picture("patrocinadores", logoKey);
        }

        public static List<string> SponsorLogoKeys()
        {
            return ImageKeys("patrocinadores");
        }

        public static string ImportImageFile(string sourcePath, string folderName)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(folderName)) return null;
            try
            {
                var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
                if (extension != ".png" && extension != ".jpg" && extension != ".jpeg") return null;
                var source = Path.GetFullPath(sourcePath);
                if (!File.Exists(source)) return null;
                var folder = Path.Combine(Root(), folderName);
                Directory.CreateDirectory(folder);
                var originalName = Path.GetFileNameWithoutExtension(source);
                foreach (var invalid in Path.GetInvalidFileNameChars()) originalName = originalName.Replace(invalid, '_');
                originalName = originalName.Trim().TrimEnd('.');
                if (string.IsNullOrWhiteSpace(originalName)) originalName = "custom-image";
                var key = originalName;
                var destination = Path.Combine(folder, key + extension);
                if (string.Equals(source, Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase)) return key;
                var suffix = 2;
                while (File.Exists(Path.Combine(folder, key + ".png"))
                    || File.Exists(Path.Combine(folder, key + ".jpg"))
                    || File.Exists(Path.Combine(folder, key + ".jpeg")))
                {
                    key = originalName + "-" + suffix++;
                    destination = Path.Combine(folder, key + extension);
                }
                File.Copy(source, destination, false);
                return key;
            }
            catch
            {
                return null;
            }
        }

        public static List<ModPackageFile> EnumeratePackageFiles(string selectedFolder, out string error)
        {
            error = "";
            var files = new List<ModPackageFile>();
            if (string.IsNullOrWhiteSpace(selectedFolder) || !Directory.Exists(selectedFolder))
            {
                error = "The selected folder could not be found.";
                return files;
            }

            var sourceRoot = Path.GetFullPath(selectedFolder);
            var nestedMods = Path.Combine(sourceRoot, "Mods");
            if (Directory.Exists(nestedMods)) sourceRoot = Path.GetFullPath(nestedMods);

            AddTextPackageFile(files, sourceRoot, new[] { "equipas.txt", "clubs.txt", "teams.txt" }, "equipas.txt");
            AddTextPackageFile(files, sourceRoot, new[] { "jogadores.txt", "players.txt" }, "jogadores.txt");
            AddTextPackageFile(files, sourceRoot, new[] { "treinadores.txt", "managers.txt", "coaches.txt" }, "treinadores.txt");
            AddTextPackageFile(files, sourceRoot, new[] { "staff.txt", "equipa-tecnica.txt" }, "staff.txt");
            AddTextPackageFile(files, sourceRoot, new[] { "sponsors.txt", "patrocinadores.txt" }, "sponsors.txt");

            AddImagePackageFiles(files, sourceRoot, new[] { "caras", "faces" }, "caras");
            AddImagePackageFiles(files, sourceRoot, new[] { "emblemas", "badges", "crests" }, "emblemas");
            AddImagePackageFiles(files, sourceRoot, new[] { "patrocinadores", "sponsors" }, "patrocinadores");

            if (files.Count == 0)
                error = "No supported mod files were found. Check the folder names in the mod guide.";
            return files;
        }

        public static bool CopyPackageFile(ModPackageFile file, out string error)
        {
            error = "";
            if (file == null || string.IsNullOrWhiteSpace(file.SourcePath) || string.IsNullOrWhiteSpace(file.DestinationPath))
            {
                error = "Invalid package file.";
                return false;
            }
            try
            {
                var source = Path.GetFullPath(file.SourcePath);
                var root = Path.GetFullPath(Root());
                var destination = Path.GetFullPath(Path.Combine(root, file.DestinationPath));
                if (!File.Exists(source) || !destination.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    error = "A package file could not be read safely.";
                    return false;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                if (!string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
                    File.Copy(source, destination, true);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static void RefreshImportedImages()
        {
            Sprites.Clear();
        }

        public static bool GetPackageDestination(ModPackageFile file, out string source, out string destination, out string error)
        {
            source = null;
            destination = null;
            error = "";
            if (file == null || string.IsNullOrWhiteSpace(file.SourcePath) || string.IsNullOrWhiteSpace(file.DestinationPath))
            {
                error = "Invalid package file.";
                return false;
            }
            try
            {
                source = Path.GetFullPath(file.SourcePath);
                var root = Path.GetFullPath(Root());
                destination = Path.GetFullPath(Path.Combine(root, file.DestinationPath));
                if (!File.Exists(source) || !destination.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    error = "A package file could not be read safely.";
                    return false;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static void CreateTemplates()
        {
            var root = Root();
            Directory.CreateDirectory(root);
            WriteTemplate(root, "MODS-LEIA-ME.txt",
                "COACH MASTER MODS / MODS DO COACH MASTER\r\n\r\n"
                + "HOW TO INSTALL: In Career Setup > Database Preview > Mods (or Settings > Mods & Database), choose the folder that contains these files and image folders, then choose Import. The folder may also contain one child folder named Mods. The game copies supported files into its Mods folder and replaces files with the same name; unrelated files are kept. Import is career data only and does not run code. When imported from Database Preview, the changes are also applied to that career draft.\r\n\r\n"
                + "PACKAGE LAYOUT (names are case-insensitive):\r\n"
                + "YourPackage/\r\n  jogadores.txt (or players.txt)\r\n  equipas.txt (or clubs.txt / teams.txt)\r\n  treinadores.txt (or managers.txt / coaches.txt)\r\n  staff.txt (or equipa-tecnica.txt)\r\n  sponsors.txt (or patrocinadores.txt)\r\n  caras/ (or faces/)\r\n  emblemas/ (or badges/ / crests/)\r\n  patrocinadores/ (or sponsors/)\r\n\r\n"
                + "IMAGE RULES: PNG, JPG and JPEG are supported. Name each image with the exact Portrait key, Custom Crest key, or Sponsor Logo key used in its data row, without the extension. Use unique names. Example: caras/face_104.png pairs with portrait key face_104. Files are matched by key, not by the image's visual contents.\r\n\r\n"
                + "TEXT RULES: Save as UTF-8. Put one existing database record on each line. Separate fields with | and keep empty fields between separators so later columns stay in the right place. A line whose first non-space character is # is a comment. Use IDs from Database Preview; names are accepted as a fallback. Text in brackets below describes a field and must not be copied literally. Empty optional fields keep existing values, except a blank player club ID makes that player a free agent.\r\n\r\n"
                + "jogadores.txt / players.txt (49 columns, indexes 0-48):\r\n"
                + "0 database player ID or current full name | 1 full name | 2 nationality | 3 position enum | 4 age | 5 shirt number | 6 potential | 7 height cm | 8 weight kg | 9 preferred foot (Left/Right/Both) | 10 weekly wage | 11 contract date YYYY-MM-DD | 12 morale | 13 fatigue | 14 fitness | 15 form | 16 motivation | 17 weak foot quality 1-5 | 18 training rating | 19 training focus | 20 training progress | 21 training intensity 0-3 | 22 training streak | 23 personality enum | 24 traits, comma-separated enum names | 25 alternate position enums, comma-separated | 26 captain true/false | 27 vice-captain true/false | 28 duty | 29 injury days | 30 injury description | 31 pace | 32 stamina | 33 strength | 34 passing | 35 finishing | 36 dribbling | 37 tackling | 38 crossing | 39 positioning | 40 composure | 41 work rate | 42 decisions | 43 reflexes | 44 handling | 45 portrait key | 46 reputation 1-100 | 47 new club database ID/name (blank means free agent) | 48 unavailable-until date ticks (0 means available; ticks are .NET DateTime ticks).\r\n"
                + "Positions and enum values use the database spelling, for example Striker, Goalkeeper, LeftWinger, RightBack, Right, Outspoken. Attributes use 1-20. Current overall is calculated from the attributes and position; potential cannot be below that overall. The player key in column 0 stays the same when you rename the player.\r\n\r\n"
                + "equipas.txt / clubs.txt / teams.txt (46 columns, indexes 0-45):\r\n"
                + "0 database club ID or current name | 1 club name | 2 short name | 3 city | 4 stadium | 5 founded year | 6 reputation 1-100 | 7 primary #RRGGBB | 8 secondary #RRGGBB | 9 shorts #RRGGBB | 10 history | 11 public reputation | 12 balance | 13 wage budget | 14 transfer budget | 15 manager name | 16 formation | 17 pressing | 18 build-up | 19 fullbacks | 20 defensive line | 21 public team state | 22 dressing-room atmosphere | 23 internal problems | 24 board objectives, comma-separated | 25 custom crest image key | 26 kit pattern enum | 27 league ID | 28 width | 29 tempo | 30 passing style | 31 attack idea | 32 marking | 33 transfer spending this season | 34 ticket income this season | 35 shirt income this season | 36 sponsor income this season | 37 player sales this season | 38 public financial situation | 39 penalty taker player ID | 40 corner taker player ID | 41 free-kick taker player ID | 42 starting lineup player IDs, comma-separated | 43 bench player IDs, comma-separated | 44 crest resource path | 45 optional legacy shorts colour override.\r\n"
                + "treinadores.txt / managers.txt / coaches.txt: club database ID or current name | manager name.\r\n"
                + "staff.txt (new full format): club ID | staff database ID | role enum (AssistantCoach/GoalkeeperCoach/FitnessCoach/Physiotherapist/Scout) | full name | judgement 1-20 | weekly wage | contract date YYYY-MM-DD | portrait key. Legacy format is club ID | role | full name | judgement | weekly wage | contract date | portrait key.\r\n"
                + "sponsors.txt / patrocinadores.txt: one row per sponsor; rows for a club replace that club's sponsor list. club ID or current name | sponsor name | weekly income | logo key | logo colour #RRGGBB | active true/false (optional).\r\n\r\n"
                + "Use the files in Database Preview to look up IDs and exact enum values. Rows only edit existing players, clubs and staff; sponsor rows can define the club's full sponsor list. The *_template.txt files repeat the column order for quick copying.\r\n");
            WriteTemplate(root, "jogadores-template.txt", "# 0 player ID | 1 full name | 2 nationality | 3 position | 4 age | 5 number | 6 potential | 7 height cm | 8 weight kg | 9 foot | 10 weekly wage | 11 contract YYYY-MM-DD | 12 morale | 13 fatigue | 14 fitness | 15 form | 16 motivation | 17 weak foot | 18 training rating | 19 focus | 20 progress | 21 intensity | 22 streak | 23 personality | 24 traits | 25 alternate positions | 26 captain | 27 vice-captain | 28 duty | 29 injury days | 30 injury description | 31 pace | 32 stamina | 33 strength | 34 passing | 35 finishing | 36 dribbling | 37 tackling | 38 crossing | 39 positioning | 40 composure | 41 work rate | 42 decisions | 43 reflexes | 44 handling | 45 portrait key | 46 reputation | 47 club ID (blank=free agent) | 48 unavailable-until ticks. See MODS-LEIA-ME.txt for rules.\r\n");
            WriteTemplate(root, "equipas-template.txt", "# 0 club ID | 1 name | 2 short name | 3 city | 4 stadium | 5 founded | 6 reputation | 7 primary hex | 8 secondary hex | 9 shorts hex | 10 history | 11 public reputation | 12 balance | 13 wage budget | 14 transfer budget | 15 manager | 16 formation | 17 pressing | 18 build-up | 19 fullbacks | 20 defensive line | 21 public team state | 22 atmosphere | 23 internal problems | 24 board objectives | 25 crest key | 26 kit pattern | 27 league ID | 28 width | 29 tempo | 30 passing style | 31 attack idea | 32 marking | 33 transfer spending | 34 ticket income | 35 shirt income | 36 sponsor income | 37 player sales | 38 financial situation | 39 penalty taker ID | 40 corner taker ID | 41 free-kick taker ID | 42 lineup player IDs | 43 bench player IDs | 44 crest resource path | 45 legacy shorts hex. See MODS-LEIA-ME.txt for rules.\r\n");
            WriteTemplate(root, "treinadores-template.txt", "# club ID or current name | manager name\r\n");
            WriteTemplate(root, "staff-template.txt", "# club ID | staff ID | role enum | full name | judgement 1-20 | weekly wage | contract YYYY-MM-DD | portrait key\r\n");
            WriteTemplate(root, "sponsors-template.txt", "# club ID or current name | sponsor name | weekly income | logo key | logo colour #RRGGBB | active true/false\r\n");
        }

        static void WriteTemplate(string root, string file, string contents)
        {
            var path = Path.Combine(root, file);
            if (!File.Exists(path)) File.WriteAllText(path, contents, new System.Text.UTF8Encoding(false));
        }

        static void AddTextPackageFile(List<ModPackageFile> files, string root, string[] aliases, string destination)
        {
            for (var i = 0; i < aliases.Length; i++)
            {
                var path = Path.Combine(root, aliases[i]);
                if (!File.Exists(path))
                {
                    if (!Directory.Exists(root)) continue;
                    var matches = Directory.GetFiles(root, aliases[i], SearchOption.AllDirectories);
                    if (matches.Length == 0) continue;
                    path = matches[0];
                }
                files.Add(new ModPackageFile { SourcePath = path, DestinationPath = destination });
                return;
            }
        }

        static void AddImagePackageFiles(List<ModPackageFile> files, string root, string[] aliases, string destination)
        {
            if (!Directory.Exists(root)) return;
            var directories = new List<string>();
            foreach (var path in Directory.GetDirectories(root, "*", SearchOption.AllDirectories))
                for (var i = 0; i < aliases.Length; i++)
                    if (string.Equals(Path.GetFileName(path), aliases[i], StringComparison.OrdinalIgnoreCase))
                    {
                        directories.Add(path);
                        break;
                    }
            foreach (var sourceFolder in directories)
            foreach (var path in Directory.GetFiles(sourceFolder, "*", SearchOption.TopDirectoryOnly))
            {
                var extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension != ".png" && extension != ".jpg" && extension != ".jpeg") continue;
                files.Add(new ModPackageFile
                {
                    SourcePath = path,
                    DestinationPath = Path.Combine(destination, Path.GetFileName(path))
                });
            }
        }

        static List<string> ImageKeys(string folderName)
        {
            var result = new List<string>();
            var folder = Path.Combine(Root(), folderName);
            if (!Directory.Exists(folder)) return result;
            foreach (var path in Directory.GetFiles(folder))
            {
                var extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension != ".png" && extension != ".jpg" && extension != ".jpeg") continue;
                result.Add(Path.GetFileNameWithoutExtension(path));
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        static void ApplySponsors(WorldState world)
        {
            var replaced = new HashSet<string>();
            var entries = ReadLines("sponsors.txt");
            for (var i = 0; i < entries.Count; i++)
            {
                var bits = entries[i].Split('|');
                if (bits.Length < 2) continue;
                var club = FindClub(world, bits[0]);
                if (club == null) continue;
                if (club.Sponsors == null) club.Sponsors = new List<SponsorContract>();
                if (replaced.Add(club.Id)) club.Sponsors.Clear();
                int.TryParse(bits.Length > 2 ? bits[2] : "0", out var weekly);
                var name = bits[1].Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;
                club.Sponsors.Add(new SponsorContract
                {
                    Id = "mod:sponsor:" + club.Id + ":" + club.Sponsors.Count,
                    Name = name,
                    WeeklyIncome = Mathf.Clamp(weekly, 0, 10000000),
                    LogoKey = bits.Length > 3 ? bits[3].Trim() : "",
                    LogoHex = bits.Length > 4 && Hex(bits[4], out var color) ? color : "#20D8F4",
                    IsActive = bits.Length > 5 ? ParseBool(bits[5], true) : true
                });
            }
        }

        static void ApplyClubs(WorldState world)
        {
            foreach (var line in ReadLines("equipas.txt"))
            {
                var bits = line.Split('|');
                if (bits.Length < 2)
                    continue;
                var club = FindClub(world, bits[0]);
                if (club == null)
                    continue;
                if (!string.IsNullOrWhiteSpace(bits[1]))
                    club.Name = bits[1].Trim();
                if (bits.Length > 2 && !string.IsNullOrWhiteSpace(bits[2]))
                    club.ShortName = bits[2].Trim();
                if (bits.Length > 3 && !string.IsNullOrWhiteSpace(bits[3]))
                    club.City = bits[3].Trim();
                if (bits.Length > 4 && !string.IsNullOrWhiteSpace(bits[4]))
                    club.Stadium = bits[4].Trim();
                if (bits.Length > 5 && int.TryParse(bits[5], out var year))
                    club.FoundedYear = Mathf.Clamp(year, 1800, 2100);
                if (bits.Length > 6 && int.TryParse(bits[6], out var reputation))
                    club.Reputation = Mathf.Clamp(reputation, 1, 100);
                if (bits.Length > 9 && Hex(bits[7], out var primary) && Hex(bits[8], out var secondary) && Hex(bits[9], out var shorts))
                {
                    club.JerseyPrimaryHex = primary;
                    club.JerseySecondaryHex = secondary;
                    club.JerseyShortsHex = shorts;
                    WorldCatalog.EnsureThreeColors(club);
                    club.KitSlug = "mod:" + club.JerseyPrimaryHex + ":" + club.JerseySecondaryHex + ":" + club.JerseyShortsHex;
                }
                if (bits.Length > 10 && !string.IsNullOrWhiteSpace(bits[10])) club.History = bits[10].Trim();
                if (bits.Length > 11 && !string.IsNullOrWhiteSpace(bits[11])) club.PublicReputation = bits[11].Trim();
                if (club.Finance == null) club.Finance = new ClubFinance();
                SetClubInt(bits, 12, 0, int.MaxValue, value => club.Finance.Balance = value);
                SetClubInt(bits, 13, 0, int.MaxValue, value => club.Finance.WageBudget = value);
                SetClubInt(bits, 14, 0, int.MaxValue, value => club.Finance.TransferBudget = value);
                if (club.Tactics == null)
                    club.Tactics = new TacticalIdentity();
                if (bits.Length > 15 && !string.IsNullOrWhiteSpace(bits[15])) club.Tactics.FormerCoachName = bits[15].Trim();
                if (bits.Length > 16 && !string.IsNullOrWhiteSpace(bits[16])) club.Tactics.Formation = bits[16].Trim();
                if (bits.Length > 17 && !string.IsNullOrWhiteSpace(bits[17])) club.Tactics.Pressing = bits[17].Trim();
                if (bits.Length > 18 && !string.IsNullOrWhiteSpace(bits[18])) club.Tactics.BuildUp = bits[18].Trim();
                if (bits.Length > 19 && !string.IsNullOrWhiteSpace(bits[19])) club.Tactics.FullBacks = bits[19].Trim();
                if (bits.Length > 20 && !string.IsNullOrWhiteSpace(bits[20])) club.Tactics.DefensiveLine = bits[20].Trim();
                if (bits.Length > 21 && !string.IsNullOrWhiteSpace(bits[21])) club.PublicTeamState = bits[21].Trim();
                if (bits.Length > 22 && !string.IsNullOrWhiteSpace(bits[22])) club.DressingRoomAtmosphere = bits[22].Trim();
                if (bits.Length > 23 && !string.IsNullOrWhiteSpace(bits[23])) club.HiddenInternalProblems = bits[23].Trim();
                if (bits.Length > 24 && !string.IsNullOrWhiteSpace(bits[24]))
                    club.BoardObjectives = SplitList(bits[24]);
                if (bits.Length > 25 && !string.IsNullOrWhiteSpace(bits[25])) club.CustomCrestKey = bits[25].Trim();
                if (bits.Length > 26 && Enum.TryParse(bits[26].Trim(), true, out KitPattern pattern)) club.KitPattern = pattern;
                if (bits.Length > 27 && !string.IsNullOrWhiteSpace(bits[27])) club.LeagueId = bits[27].Trim();
                if (bits.Length > 28 && !string.IsNullOrWhiteSpace(bits[28])) club.Tactics.Width = bits[28].Trim();
                if (bits.Length > 29 && !string.IsNullOrWhiteSpace(bits[29])) club.Tactics.Tempo = bits[29].Trim();
                if (bits.Length > 30 && !string.IsNullOrWhiteSpace(bits[30])) club.Tactics.PassingStyle = bits[30].Trim();
                if (bits.Length > 31 && !string.IsNullOrWhiteSpace(bits[31])) club.Tactics.AttackIdea = bits[31].Trim();
                if (bits.Length > 32 && !string.IsNullOrWhiteSpace(bits[32])) club.Tactics.Marking = bits[32].Trim();
                SetClubInt(bits, 33, 0, int.MaxValue, value => club.Finance.TransferSpentThisSeason = value);
                SetClubInt(bits, 34, 0, int.MaxValue, value => club.Finance.TicketIncomeThisSeason = value);
                SetClubInt(bits, 35, 0, int.MaxValue, value => club.Finance.ShirtIncomeThisSeason = value);
                SetClubInt(bits, 36, 0, int.MaxValue, value => club.Finance.SponsorIncomeThisSeason = value);
                SetClubInt(bits, 37, 0, int.MaxValue, value => club.Finance.PlayerSalesThisSeason = value);
                if (bits.Length > 38 && !string.IsNullOrWhiteSpace(bits[38])) club.Finance.PublicSituation = bits[38].Trim();
                if (bits.Length > 39 && !string.IsNullOrWhiteSpace(bits[39])) club.PenaltyTakerId = bits[39].Trim();
                if (bits.Length > 40 && !string.IsNullOrWhiteSpace(bits[40])) club.CornerTakerId = bits[40].Trim();
                if (bits.Length > 41 && !string.IsNullOrWhiteSpace(bits[41])) club.FreeKickTakerId = bits[41].Trim();
                if (bits.Length > 42 && !string.IsNullOrWhiteSpace(bits[42])) club.ManualLineup = SplitList(bits[42]);
                if (bits.Length > 43 && !string.IsNullOrWhiteSpace(bits[43])) club.ManualBenchOrder = SplitList(bits[43]);
                if (bits.Length > 44 && !string.IsNullOrWhiteSpace(bits[44])) club.CrestResourcePath = bits[44].Trim();
                if (bits.Length > 45 && !string.IsNullOrWhiteSpace(bits[45])) club.JerseyShortsHex = bits[45].Trim();
                club.Tactics.Summary = string.Join(" · ", new[] { club.Tactics.Formation, club.Tactics.Pressing, club.Tactics.BuildUp, club.Tactics.FullBacks, club.Tactics.DefensiveLine });
            }
        }

        static void ApplyPlayers(WorldState world)
        {
            foreach (var line in ReadLines("jogadores.txt"))
            {
                var bits = line.Split('|');
                if (bits.Length < 2 || string.IsNullOrWhiteSpace(bits[0]))
                    continue;
                var player = FindPlayer(world, bits[0]);
                if (player == null) continue;
                if (player.Attributes == null) player.Attributes = new PlayerAttributes();
                if (bits.Length >= 46)
                    ApplyFullPlayerRow(world, player, bits);
                else
                    ApplyLegacyPlayerRow(player, bits);
            }
        }

        static void ApplyLegacyPlayerRow(Player player, string[] bits)
        {
            if (PositionOf(bits[1], out var position)) player.Position = position;
            SetPlayerInt(bits, 2, 15, 50, value => player.Age = value);
            if (bits.Length > 3 && !string.IsNullOrWhiteSpace(bits[3])) player.Nationality = bits[3].Trim();
            Set(player, bits, 4, value => player.Attributes.Pace = value);
            Set(player, bits, 5, value => player.Attributes.Stamina = value);
            Set(player, bits, 6, value => player.Attributes.Strength = value);
            Set(player, bits, 7, value => player.Attributes.Passing = value);
            Set(player, bits, 8, value => player.Attributes.Finishing = value);
            Set(player, bits, 9, value => player.Attributes.Dribbling = value);
            Set(player, bits, 10, value => player.Attributes.Tackling = value);
            Set(player, bits, 11, value => player.Attributes.Crossing = value);
            Set(player, bits, 12, value => player.Attributes.Positioning = value);
            Set(player, bits, 13, value => player.Attributes.Composure = value);
            Set(player, bits, 14, value => player.Attributes.WorkRate = value);
            Set(player, bits, 15, value => player.Attributes.Decisions = value);
            Set(player, bits, 16, value => player.Attributes.Reflexes = value);
            Set(player, bits, 17, value => player.Attributes.Handling = value);
            SetPlayerInt(bits, 18, 15, 99, value => player.Potential = Mathf.Max(value, PlayerAttributes.OverallAtPosition(player, player.Position)));
            SetPlayerInt(bits, 19, 1, 99, value => player.SquadNumber = value);
            SetPlayerInt(bits, 20, 130, 220, value => player.HeightCm = value);
            SetPlayerInt(bits, 21, 35, 150, value => player.WeightKg = value);
            SetFoot(player, bits, 22);
            SetPlayerInt(bits, 23, 0, int.MaxValue, value => player.WeeklyWage = value);
            if (bits.Length > 24) SetContractDate(player, bits[24]);
            SetPlayerInt(bits, 25, 0, 100, value => player.Morale = value);
            SetPlayerInt(bits, 26, 0, 100, value => player.Fatigue = value);
            SetPlayerInt(bits, 27, 0, 100, value => player.Fitness = value);
            SetPlayerInt(bits, 28, 0, 100, value => player.Form = value);
            SetPlayerInt(bits, 29, 0, 100, value => player.Motivation = value);
            SetPlayerInt(bits, 30, 1, 5, value => player.WeakFootQuality = value);
            SetPlayerInt(bits, 31, 0, 100, value => player.TrainingRating = value);
            SetPlayerText(bits, 32, value => player.TrainingFocus = value);
            SetPlayerInt(bits, 33, 0, 100, value => player.TrainingProgress = value);
            SetPlayerInt(bits, 34, 0, 3, value => player.TrainingIntensity = value);
            SetPlayerInt(bits, 35, 0, 100, value => player.TrainingStreak = value);
            if (bits.Length > 36 && Enum.TryParse(bits[36].Trim(), true, out PersonalityTrait personality)) player.Personality = personality;
            if (bits.Length > 37 && !string.IsNullOrWhiteSpace(bits[37])) player.Traits = ParseTraits(bits[37]);
            if (bits.Length > 38 && !string.IsNullOrWhiteSpace(bits[38])) player.AlternatePositions = ParsePositions(bits[38]);
            if (bits.Length > 39) player.IsCaptain = ParseBool(bits[39], player.IsCaptain);
            if (bits.Length > 40) player.IsViceCaptain = ParseBool(bits[40], player.IsViceCaptain);
            SetPlayerText(bits, 41, value => player.Duty = value);
            SetInjury(bits, player, 42, 43);
        }

        static void ApplyFullPlayerRow(WorldState world, Player player, string[] bits)
        {
            SetPlayerText(bits, 1, value => player.FullName = value);
            SetPlayerText(bits, 2, value => player.Nationality = value);
            if (PositionOf(bits[3], out var position)) player.Position = position;
            SetPlayerInt(bits, 4, 15, 50, value => player.Age = value);
            SetPlayerInt(bits, 5, 1, 99, value => player.SquadNumber = value);
            var importedPotential = player.Potential;
            SetPlayerInt(bits, 6, 15, 99, value => importedPotential = value);
            SetPlayerInt(bits, 7, 130, 220, value => player.HeightCm = value);
            SetPlayerInt(bits, 8, 35, 150, value => player.WeightKg = value);
            SetFoot(player, bits, 9);
            SetPlayerInt(bits, 10, 0, int.MaxValue, value => player.WeeklyWage = value);
            if (bits.Length > 11) SetContractDate(player, bits[11]);
            SetPlayerInt(bits, 12, 0, 100, value => player.Morale = value);
            SetPlayerInt(bits, 13, 0, 100, value => player.Fatigue = value);
            SetPlayerInt(bits, 14, 0, 100, value => player.Fitness = value);
            SetPlayerInt(bits, 15, 0, 100, value => player.Form = value);
            SetPlayerInt(bits, 16, 0, 100, value => player.Motivation = value);
            SetPlayerInt(bits, 17, 1, 5, value => player.WeakFootQuality = value);
            SetPlayerInt(bits, 18, 0, 100, value => player.TrainingRating = value);
            SetPlayerText(bits, 19, value => player.TrainingFocus = value);
            SetPlayerInt(bits, 20, 0, 100, value => player.TrainingProgress = value);
            SetPlayerInt(bits, 21, 0, 3, value => player.TrainingIntensity = value);
            SetPlayerInt(bits, 22, 0, 100, value => player.TrainingStreak = value);
            if (bits.Length > 23 && Enum.TryParse(bits[23].Trim(), true, out PersonalityTrait personality)) player.Personality = personality;
            if (bits.Length > 24 && !string.IsNullOrWhiteSpace(bits[24])) player.Traits = ParseTraits(bits[24]);
            if (bits.Length > 25 && !string.IsNullOrWhiteSpace(bits[25])) player.AlternatePositions = ParsePositions(bits[25]);
            if (bits.Length > 26) player.IsCaptain = ParseBool(bits[26], player.IsCaptain);
            if (bits.Length > 27) player.IsViceCaptain = ParseBool(bits[27], player.IsViceCaptain);
            SetPlayerText(bits, 28, value => player.Duty = value);
            SetInjury(bits, player, 29, 30);
            Set(player, bits, 31, value => player.Attributes.Pace = value);
            Set(player, bits, 32, value => player.Attributes.Stamina = value);
            Set(player, bits, 33, value => player.Attributes.Strength = value);
            Set(player, bits, 34, value => player.Attributes.Passing = value);
            Set(player, bits, 35, value => player.Attributes.Finishing = value);
            Set(player, bits, 36, value => player.Attributes.Dribbling = value);
            Set(player, bits, 37, value => player.Attributes.Tackling = value);
            Set(player, bits, 38, value => player.Attributes.Crossing = value);
            Set(player, bits, 39, value => player.Attributes.Positioning = value);
            Set(player, bits, 40, value => player.Attributes.Composure = value);
            Set(player, bits, 41, value => player.Attributes.WorkRate = value);
            Set(player, bits, 42, value => player.Attributes.Decisions = value);
            Set(player, bits, 43, value => player.Attributes.Reflexes = value);
            Set(player, bits, 44, value => player.Attributes.Handling = value);
            player.Potential = Mathf.Max(importedPotential, PlayerAttributes.OverallAtPosition(player, player.Position));
            SetPlayerText(bits, 45, value => player.PortraitPath = value);
            SetPlayerInt(bits, 46, 1, 100, value => player.Reputation = value);
            if (bits.Length > 47) SetPlayerClub(world, player, bits[47]);
            if (bits.Length > 48 && long.TryParse(bits[48], NumberStyles.Integer, CultureInfo.InvariantCulture, out var unavailableUntil)
                && unavailableUntil >= 0 && unavailableUntil <= DateTime.MaxValue.Ticks)
                player.UnavailableUntilTicks = unavailableUntil;
        }

        static void SetPlayerClub(WorldState world, Player player, string clubKey)
        {
            if (world?.Clubs == null || player == null) return;
            var targetClub = string.IsNullOrWhiteSpace(clubKey) ? null : FindClub(world, clubKey);
            if (!string.IsNullOrWhiteSpace(clubKey) && targetClub == null) return;
            if (!string.IsNullOrWhiteSpace(player.ClubId) && world.Clubs.TryGetValue(player.ClubId, out var previousClub))
            {
                previousClub.PlayerIds?.Remove(player.Id);
                previousClub.ManualLineup?.Remove(player.Id);
                previousClub.ManualBenchOrder?.Remove(player.Id);
            }
            player.ClubId = targetClub == null ? null : targetClub.Id;
            if (targetClub != null)
            {
                if (targetClub.PlayerIds == null) targetClub.PlayerIds = new List<string>();
                if (!targetClub.PlayerIds.Contains(player.Id)) targetClub.PlayerIds.Add(player.Id);
            }
        }

        static Player FindPlayer(WorldState world, string key)
        {
            if (world?.Players == null || string.IsNullOrWhiteSpace(key)) return null;
            foreach (var player in world.Players.Values)
                if (player != null && (string.Equals(player.Id, key.Trim(), StringComparison.OrdinalIgnoreCase)
                    || string.Equals(player.FullName, key.Trim(), StringComparison.OrdinalIgnoreCase))) return player;
            return null;
        }

        static void SetPlayerText(string[] bits, int index, Action<string> write)
        {
            if (bits != null && index < bits.Length && write != null && !string.IsNullOrWhiteSpace(bits[index])) write(bits[index].Trim());
        }

        static void SetFoot(Player player, string[] bits, int index)
        {
            if (player == null || bits == null || index >= bits.Length) return;
            var foot = bits[index].Trim();
            if (foot.Equals("left", StringComparison.OrdinalIgnoreCase) || foot.Equals("esquerdo", StringComparison.OrdinalIgnoreCase)) player.PreferredFoot = PreferredFoot.Left;
            else if (foot.Equals("both", StringComparison.OrdinalIgnoreCase) || foot.Equals("ambos", StringComparison.OrdinalIgnoreCase)) player.PreferredFoot = PreferredFoot.Both;
            else if (foot.Equals("right", StringComparison.OrdinalIgnoreCase) || foot.Equals("direito", StringComparison.OrdinalIgnoreCase)) player.PreferredFoot = PreferredFoot.Right;
        }

        static void SetInjury(string[] bits, Player player, int daysIndex, int descriptionIndex)
        {
            if (player == null) return;
            if (bits != null && daysIndex < bits.Length && int.TryParse(bits[daysIndex], NumberStyles.Integer, CultureInfo.InvariantCulture, out var days))
            {
                player.Injury = player.Injury ?? new InjuryState();
                player.Injury.DaysRemaining = Mathf.Clamp(days, 0, 3650);
                player.Injury.IsInjured = player.Injury.DaysRemaining > 0;
            }
            if (bits != null && descriptionIndex < bits.Length && !string.IsNullOrWhiteSpace(bits[descriptionIndex]))
            {
                player.Injury = player.Injury ?? new InjuryState();
                player.Injury.Description = bits[descriptionIndex].Trim();
            }
        }

        static void ApplyManagers(WorldState world)
        {
            foreach (var line in ReadLines("treinadores.txt"))
            {
                var bits = line.Split('|');
                if (bits.Length < 2 || string.IsNullOrWhiteSpace(bits[1])) continue;
                var club = FindClub(world, bits[0]);
                if (club == null) continue;
                if (club.Tactics == null) club.Tactics = new TacticalIdentity();
                club.Tactics.FormerCoachName = bits[1].Trim();
            }
        }

        static void ApplyStaff(WorldState world)
        {
            foreach (var line in ReadLines("staff.txt"))
            {
                var bits = line.Split('|');
                if (bits.Length < 3) continue;
                var club = FindClub(world, bits[0]);
                if (club == null || club.Staff == null) continue;
                var fullSchema = bits.Length >= 8;
                var role = ParseStaffRole(fullSchema ? bits[2] : bits[1]);
                var member = fullSchema
                    ? club.Staff.Find(candidate => candidate != null && string.Equals(candidate.Id, bits[1].Trim(), StringComparison.OrdinalIgnoreCase))
                    : club.Staff.Find(candidate => candidate != null && candidate.Role == role);
                if (member == null) continue;
                var nameIndex = fullSchema ? 3 : 2;
                var judgementIndex = fullSchema ? 4 : 3;
                var wageIndex = fullSchema ? 5 : 4;
                var contractIndex = fullSchema ? 6 : 5;
                var portraitIndex = fullSchema ? 7 : 6;
                if (nameIndex < bits.Length && !string.IsNullOrWhiteSpace(bits[nameIndex])) member.FullName = bits[nameIndex].Trim();
                member.Role = role;
                if (judgementIndex < bits.Length && int.TryParse(bits[judgementIndex], NumberStyles.Integer, CultureInfo.InvariantCulture, out var judgement))
                    member.Judgement = Mathf.Clamp(judgement, 1, 20);
                SetPlayerInt(bits, wageIndex, 0, int.MaxValue, value => member.WeeklyWage = value);
                if (contractIndex < bits.Length && DateTime.TryParseExact(bits[contractIndex], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var contractEnd))
                    member.ContractEndTicks = contractEnd.Ticks;
                else if (contractIndex < bits.Length && long.TryParse(bits[contractIndex], NumberStyles.Integer, CultureInfo.InvariantCulture, out var contractTicks)
                    && contractTicks >= 0 && contractTicks <= DateTime.MaxValue.Ticks) member.ContractEndTicks = contractTicks;
                SetPlayerText(bits, portraitIndex, value => member.PortraitPath = value);
            }
        }

        static StaffRole ParseStaffRole(string text)
        {
            switch ((text ?? "").Trim().ToLowerInvariant())
            {
                case "goalkeepercoach": case "goalkeeper coach": case "treinador de guarda-redes": return StaffRole.GoalkeeperCoach;
                case "fitnesscoach": case "fitness coach": case "preparador físico": return StaffRole.FitnessCoach;
                case "physiotherapist": case "fisioterapeuta": return StaffRole.Physiotherapist;
                case "scout": case "olheiro": return StaffRole.Scout;
                default: return StaffRole.AssistantCoach;
            }
        }

        static void SetPlayerInt(string[] bits, int index, int minimum, int maximum, Action<int> write)
        {
            if (bits == null || index >= bits.Length || write == null) return;
            if (int.TryParse(bits[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                write(Mathf.Clamp(value, minimum, maximum));
        }

        static void SetClubInt(string[] bits, int index, int minimum, int maximum, Action<int> write)
        {
            SetPlayerInt(bits, index, minimum, maximum, write);
        }

        static void SetContractDate(Player player, string text)
        {
            if (player == null || string.IsNullOrWhiteSpace(text)) return;
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks) && ticks > 0 && ticks <= DateTime.MaxValue.Ticks)
                player.ContractEndTicks = ticks;
            else if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
                player.ContractEndTicks = date.Ticks;
        }

        static List<string> SplitList(string text)
        {
            var values = new List<string>();
            foreach (var value in (text ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                if (!string.IsNullOrWhiteSpace(value)) values.Add(value.Trim());
            return values;
        }

        static List<PlayerTrait> ParseTraits(string text)
        {
            var result = new List<PlayerTrait>();
            foreach (var token in SplitList(text))
                if (Enum.TryParse(token, true, out PlayerTrait trait) && !result.Contains(trait)) result.Add(trait);
            return result;
        }

        static List<PlayerPosition> ParsePositions(string text)
        {
            var result = new List<PlayerPosition>();
            foreach (var token in SplitList(text))
                if (PositionOf(token, out var position) && !result.Contains(position)) result.Add(position);
            return result;
        }

        static bool ParseBool(string text, bool fallback)
        {
            if (bool.TryParse(text, out var value)) return value;
            if (text == "1" || string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "sim", StringComparison.OrdinalIgnoreCase)) return true;
            if (text == "0" || string.Equals(text, "no", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "não", StringComparison.OrdinalIgnoreCase)) return false;
            return fallback;
        }

        static void Set(Player player, string[] bits, int index, Action<int> write)
        {
            if (player?.Attributes == null || index >= bits.Length)
                return;
            int value;
            if (int.TryParse(bits[index], out value))
                write(Mathf.Clamp(value, 1, 20));
        }

        static Club FindClub(WorldState world, string name)
        {
            if (world?.Clubs == null || string.IsNullOrWhiteSpace(name))
                return null;
            var wanted = name.Trim();
            foreach (var club in world.Clubs.Values)
            {
                if (club == null)
                    continue;
                if (string.Equals(club.Name, wanted, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(club.ShortName, wanted, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(club.Id, wanted, StringComparison.OrdinalIgnoreCase))
                    return club;
            }
            return null;
        }

        static bool PositionOf(string text, out PlayerPosition position)
        {
            switch ((text ?? "").Trim().ToUpperInvariant())
            {
                case "GR":
                case "GK":
                case "GOALKEEPER":
                    position = PlayerPosition.Goalkeeper;
                    return true;
                case "DD":
                case "LD":
                case "RIGHTBACK":
                    position = PlayerPosition.RightBack;
                    return true;
                case "DC":
                case "CENTREBACK":
                    position = PlayerPosition.CentreBack;
                    return true;
                case "DE":
                case "LE":
                case "LEFTBACK":
                    position = PlayerPosition.LeftBack;
                    return true;
                case "MDF":
                case "DEFENSIVEMIDFELDER":
                    position = PlayerPosition.DefensiveMidfielder;
                    return true;
                case "MC":
                case "CENTRALMIDFIELDER":
                    position = PlayerPosition.CentralMidfielder;
                    return true;
                case "MO":
                case "ATTACKINGMIDFIELDER":
                    position = PlayerPosition.AttackingMidfielder;
                    return true;
                case "ED":
                case "MD":
                case "RIGHTWINGER":
                    position = PlayerPosition.RightWinger;
                    return true;
                case "EE":
                case "ME":
                case "LEFTWINGER":
                    position = PlayerPosition.LeftWinger;
                    return true;
                case "PL":
                case "ST":
                case "STRIKER":
                    position = PlayerPosition.Striker;
                    return true;
                case "LEFTMIDFIELDER":
                    position = PlayerPosition.LeftMidfielder;
                    return true;
                case "RIGHTMIDFIELDER":
                    position = PlayerPosition.RightMidfielder;
                    return true;
                default:
                    position = PlayerPosition.CentralMidfielder;
                    return false;
            }
        }

        static List<string> ReadLines(string fileName)
        {
            var result = new List<string>();
            var path = Path.Combine(Root(), fileName);
            if (!File.Exists(path))
                return result;
            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch
            {
                return result;
            }
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                var trimmed = line.Trim();
                if (i == 0) trimmed = trimmed.TrimStart('\uFEFF');
                if (trimmed.StartsWith("#"))
                    continue;
                result.Add(trimmed);
            }
            return result;
        }

        static Sprite Picture(string folder, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            var png = Path.Combine(Root(), folder, name.Trim() + ".png");
            var jpg = Path.Combine(Root(), folder, name.Trim() + ".jpg");
            var jpeg = Path.Combine(Root(), folder, name.Trim() + ".jpeg");
            var path = File.Exists(png) ? png : File.Exists(jpg) ? jpg : File.Exists(jpeg) ? jpeg : "";
            if (string.IsNullOrEmpty(path))
                return null;
            Sprite sprite;
            if (Sprites.TryGetValue(path, out sprite) && sprite != null)
                return sprite;
            try
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(File.ReadAllBytes(path)))
                    return null;
                texture.name = Path.GetFileNameWithoutExtension(path);
                sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                sprite.name = texture.name;
                Sprites[path] = sprite;
                return sprite;
            }
            catch
            {
                return null;
            }
        }

        static bool Hex(string text, out string hex)
        {
            hex = (text ?? "").Trim();
            if (hex.Length == 6)
                hex = "#" + hex;
            if (hex.Length != 7 || hex[0] != '#')
            {
                hex = "";
                return false;
            }
            return true;
        }
    }
}
