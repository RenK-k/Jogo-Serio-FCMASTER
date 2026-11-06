using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CoachMaster.App;
using CoachMaster.Data;
using CoachMaster.Domain;
using CoachMaster.UI;
using UnityEngine;

namespace CoachMaster.Career
{
    public static class CareerStore
    {
        static string PathFile => Path.Combine(Application.persistentDataPath, "coach-master-career.txt");
        static string AutoSavePath(int slot) => Path.Combine(Application.persistentDataPath, "coach-master-autosave-" + slot + ".txt");
        const string NamedSavePrefix = "coach-master-named-";

        public sealed class NamedSaveInfo
        {
            public string Id { get; internal set; }
            public string Name { get; internal set; }
            public string CoachName { get; internal set; }
            public string ClubId { get; internal set; }
            public DateTime SavedAt { get; internal set; }
        }

        public static bool Exists => File.Exists(PathFile);
        public static bool HasAutoSave(int slot = 1) => File.Exists(AutoSavePath(Mathf.Clamp(slot, 1, 5)));

        public static DateTime LastPlayedAt(CareerState career)
        {
            if (career == null)
                return DateTime.MinValue;
            if (career != null && career.LastPlayedUtcTicks > 0)
            {
                try { return new DateTime(career.LastPlayedUtcTicks, DateTimeKind.Utc).ToLocalTime(); }
                catch { }
            }

            var best = DateTime.MinValue;
            try
            {
                var root = Application.persistentDataPath;
                if (Directory.Exists(root))
                {
                    foreach (var path in Directory.GetFiles(root, "coach-master-*.txt", SearchOption.TopDirectoryOnly))
                    {
                        var matches = !string.IsNullOrEmpty(career.FileId)
                            ? ReadCareerFileId(path) == career.FileId
                            : SavedForCareer(path, career);
                        if (!matches)
                            continue;
                        var written = File.GetLastWriteTime(path);
                        if (written > best) best = written;
                    }
                }
            }
            catch { }
            return best;
        }

        static bool SavedForCareer(string path, CareerState career)
        {
            if (career?.Coach == null || !File.Exists(path))
                return false;
            try
            {
                foreach (var line in File.ReadLines(path, Encoding.UTF8))
                {
                    if (string.IsNullOrEmpty(line) || !line.StartsWith("coach|", StringComparison.Ordinal))
                        continue;
                    var bits = Split(line);
                    return bits.Length > 2 && Un(bits[1]) == career.Coach.Name && bits[2] == career.Coach.ClubId;
                }
            }
            catch { }
            return false;
        }

        public static string AutoSaveLabel(int slot, WorldState world)
        {
            slot = Mathf.Clamp(slot, 1, 5);
            var path = AutoSavePath(slot);
            if (!File.Exists(path))
                return "CÓPIA " + slot + " · VAZIA";
            try
            {
                var lines = File.ReadAllLines(path, Encoding.UTF8);
                for (var i = 0; i < lines.Length; i++)
                {
                    var bits = Split(lines[i]);
                    if (bits.Length < 4 || bits[0] != "coach") continue;
                    var coach = Un(bits[1]);
                    var clubName = bits[2];
                    if (world != null && world.Clubs.TryGetValue(bits[2], out var club) && club != null)
                        clubName = club.ShortName;
                    var savedDate = long.TryParse(bits[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks) && ticks > 0
                        ? new DateTime(ticks).ToString("dd/MM/yyyy") : "data antiga";
                    return clubName + " · " + coach + " · " + savedDate;
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("Não foi possível ler o nome do autosave: " + error.Message);
            }
            return "CÓPIA " + slot + " · GRAVAÇÃO ANTIGA";
        }

        public static void Save(CareerState career)
        {
            TrySave(career);
        }

        public static bool TrySave(CareerState career)
        {
            try
            {
                EnsureIdentity(career);
                if (!SaveTo(career, PathFile))
                    return false;
                MirrorSlot(career, true);
                return true;
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("Não foi possível guardar a carreira. A versão anterior foi mantida: " + error.Message);
                return false;
            }
        }

        public static bool SaveNamed(CareerState career, string name, out NamedSaveInfo info)
        {
            info = null;
            if (career == null || career.Coach == null || career.Club == null)
                return false;
            name = string.IsNullOrWhiteSpace(name) ? "Career · " + career.Club.ShortName : name.Trim();
            if (name.Length > 80)
                name = name.Substring(0, 80).Trim();
            var id = Guid.NewGuid().ToString("N");
            var path = NamedSavePath(id);
            var staging = path + ".new";
            var metaPath = path + ".meta";
            var metaStaging = metaPath + ".new";
            var savedAt = DateTime.Now;
            try
            {
                if (!SaveTo(career, staging))
                    return false;
                var meta = Encode(name) + "|" + savedAt.Ticks.ToString(CultureInfo.InvariantCulture) + "|"
                    + career.Coach.ClubId + "|" + Encode(career.Coach.Name);
                File.WriteAllText(metaStaging, meta, Encoding.UTF8);
                File.Move(staging, path);
                File.Move(metaStaging, metaPath);
                info = new NamedSaveInfo { Id = id, Name = name, CoachName = career.Coach.Name, ClubId = career.Coach.ClubId, SavedAt = savedAt };
                return true;
            }
            catch (Exception error)
            {
                if (File.Exists(staging)) File.Delete(staging);
                if (File.Exists(metaStaging)) File.Delete(metaStaging);
                if (File.Exists(path) && !File.Exists(metaPath)) File.Delete(path);
                Debug.LogWarning("Não foi possível criar a gravação nomeada: " + error.Message);
                return false;
            }
        }

        public static List<NamedSaveInfo> ListNamedSaves()
        {
            var result = new List<NamedSaveInfo>();
            try
            {
                var files = Directory.GetFiles(Application.persistentDataPath, NamedSavePrefix + "*.txt", SearchOption.TopDirectoryOnly);
                foreach (var path in files)
                {
                    var file = Path.GetFileNameWithoutExtension(path);
                    var id = file.Substring(NamedSavePrefix.Length);
                    if (!Guid.TryParseExact(id, "N", out _))
                        continue;
                    var metaPath = path + ".meta";
                    if (!File.Exists(metaPath))
                        continue;
                    var bits = File.ReadAllText(metaPath, Encoding.UTF8).Split('|');
                    if (bits.Length < 4 || !long.TryParse(bits[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
                        continue;
                    result.Add(new NamedSaveInfo
                    {
                        Id = id,
                        Name = Decode(bits[0]),
                        SavedAt = new DateTime(ticks),
                        ClubId = bits[2],
                        CoachName = Decode(bits[3])
                    });
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("Não foi possível ler as gravações nomeadas: " + error.Message);
            }
            result.Sort((a, b) => b.SavedAt.CompareTo(a.SavedAt));
            return result;
        }

        public static CareerState TryLoadNamed(GameSession session, string id)
        {
            if (string.IsNullOrEmpty(id) || !Guid.TryParseExact(id, "N", out _))
                return null;
            return TryLoadFrom(session, NamedSavePath(id));
        }

        static string NamedSavePath(string id) => Path.Combine(Application.persistentDataPath, NamedSavePrefix + id + ".txt");

        public sealed class CareerSlotInfo
        {
            public string Id;
            public string Label;
            public bool HasManual;
            public bool HasAuto;
            public DateTime ManualAt;
            public DateTime AutoAt;
            public bool NamedOnly;
        }

        public static void EnsureIdentity(CareerState career)
        {
            if (career == null)
                return;
            if (string.IsNullOrEmpty(career.FileId) || !Guid.TryParseExact(career.FileId, "N", out _))
                career.FileId = Guid.NewGuid().ToString("N");
            if (string.IsNullOrWhiteSpace(career.SaveLabel))
            {
                var club = "Carreira";
                if (career.World != null && career.Coach != null && career.World.Clubs.TryGetValue(career.Coach.ClubId ?? "", out var found) && found != null)
                    club = found.ShortName;
                var coach = career.Coach == null ? "" : career.Coach.Name;
                career.SaveLabel = string.IsNullOrEmpty(coach) ? club : club + " · " + coach;
            }
        }

        static string SlotPath(string id, bool manual) => Path.Combine(Application.persistentDataPath, "coach-master-slot-" + id + (manual ? "-manual" : "-auto") + ".txt");

        static string SlotLabelPath(string id) => Path.Combine(Application.persistentDataPath, "coach-master-slot-" + id + ".label");

        static void MirrorSlot(CareerState career, bool manual)
        {
            if (career == null || string.IsNullOrEmpty(career.FileId))
                return;
            var source = manual ? PathFile : AutoSavePath(1);
            if (!File.Exists(source))
                return;
            try
            {
                File.Copy(source, SlotPath(career.FileId, manual), true);
                File.WriteAllText(SlotLabelPath(career.FileId), career.SaveLabel ?? "", Encoding.UTF8);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Não foi possível atualizar a carreira guardada: " + error.Message);
            }
        }

        public static List<CareerSlotInfo> ListSlots()
        {
            var map = new Dictionary<string, CareerSlotInfo>();
            try
            {
                var root = Application.persistentDataPath;
                if (Directory.Exists(root))
                {
                    foreach (var path in Directory.GetFiles(root, "coach-master-slot-*-manual.txt", SearchOption.TopDirectoryOnly))
                        TouchSlot(map, path, true);
                    foreach (var path in Directory.GetFiles(root, "coach-master-slot-*-auto.txt", SearchOption.TopDirectoryOnly))
                        TouchSlot(map, path, false);
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("Não foi possível ler as carreiras: " + error.Message);
            }
            foreach (var named in ListNamedSaves())
            {
                if (map.ContainsKey(named.Id))
                    continue;
                map[named.Id] = new CareerSlotInfo
                {
                    Id = named.Id,
                    Label = named.Name,
                    HasManual = true,
                    ManualAt = named.SavedAt,
                    NamedOnly = true
                };
            }
            AttachRotatingAutosaves(map);
            var list = new List<CareerSlotInfo>(map.Values);
            list.Sort((a, b) =>
            {
                var left = a.ManualAt > a.AutoAt ? a.ManualAt : a.AutoAt;
                var right = b.ManualAt > b.AutoAt ? b.ManualAt : b.AutoAt;
                return right.CompareTo(left);
            });
            return list;
        }

        static void TouchSlot(Dictionary<string, CareerSlotInfo> map, string path, bool manual)
        {
            var file = Path.GetFileNameWithoutExtension(path);
            const string prefix = "coach-master-slot-";
            if (!file.StartsWith(prefix, StringComparison.Ordinal))
                return;
            var body = file.Substring(prefix.Length);
            var suffix = manual ? "-manual" : "-auto";
            if (!body.EndsWith(suffix, StringComparison.Ordinal))
                return;
            var id = body.Substring(0, body.Length - suffix.Length);
            if (!Guid.TryParseExact(id, "N", out _))
                return;
            if (!map.TryGetValue(id, out var info))
            {
                info = new CareerSlotInfo { Id = id, Label = "Carreira" };
                map[id] = info;
                var labelPath = SlotLabelPath(id);
                if (File.Exists(labelPath))
                {
                    var label = File.ReadAllText(labelPath, Encoding.UTF8).Trim();
                    if (!string.IsNullOrEmpty(label))
                        info.Label = label;
                }
            }
            var when = File.GetLastWriteTime(path);
            if (manual)
            {
                info.HasManual = true;
                info.ManualAt = when;
            }
            else
            {
                info.HasAuto = true;
                info.AutoAt = when;
            }
        }

        public static bool RenameSlot(string id, string label, CareerState active)
        {
            label = (label ?? "").Trim();
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(label))
                return false;
            if (label.Length > 80)
                label = label.Substring(0, 80).Trim();
            if (active != null && active.FileId == id)
            {
                active.SaveLabel = label;
                TrySave(active);
            }
            try
            {
                if (File.Exists(SlotLabelPath(id)) || File.Exists(SlotPath(id, true)) || File.Exists(SlotPath(id, false)))
                    File.WriteAllText(SlotLabelPath(id), label, Encoding.UTF8);
                var metaPath = NamedSavePath(id) + ".meta";
                if (File.Exists(metaPath))
                {
                    var bits = File.ReadAllText(metaPath, Encoding.UTF8).Split('|');
                    if (bits.Length >= 4)
                    {
                        bits[0] = Encode(label);
                        File.WriteAllText(metaPath, string.Join("|", bits), Encoding.UTF8);
                    }
                }
                return true;
            }
            catch (Exception error)
            {
                Debug.LogWarning("Não foi possível mudar o nome da carreira: " + error.Message);
                return false;
            }
        }

        public static void DeleteSlot(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;
            try
            {
                var paths = new[]
                {
                    SlotPath(id, true), SlotPath(id, false), SlotLabelPath(id),
                    NamedSavePath(id), NamedSavePath(id) + ".meta"
                };
                for (var i = 0; i < paths.Length; i++)
                    if (File.Exists(paths[i]))
                        File.Delete(paths[i]);
                for (var slot = 1; slot <= 5; slot++)
                {
                    var auto = AutoSavePath(slot);
                    if (ReadCareerFileId(auto) == id && File.Exists(auto))
                        File.Delete(auto);
                }
                if (ReadCareerFileId(PathFile) == id && File.Exists(PathFile))
                    File.Delete(PathFile);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Não foi possível apagar a carreira: " + error.Message);
            }
        }

        public static CareerState TryLoadSlot(GameSession session, string id, bool manual)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            if (!File.Exists(SlotPath(id, manual)))
            {
                if (manual)
                    return TryLoadNamed(session, id);
                var rotating = NewestAutoSaveFor(id);
                return string.IsNullOrEmpty(rotating) ? null : TryLoadFrom(session, rotating);
            }
            if (!manual)
            {
                var rotating = NewestAutoSaveFor(id);
                var slotFile = SlotPath(id, false);
                if (!string.IsNullOrEmpty(rotating) && File.Exists(rotating)
                    && File.GetLastWriteTime(rotating) > File.GetLastWriteTime(slotFile))
                    return TryLoadFrom(session, rotating);
            }
            return TryLoadFrom(session, SlotPath(id, manual));
        }

        static void AttachRotatingAutosaves(Dictionary<string, CareerSlotInfo> map)
        {
            string orphanId = null;
            DateTime orphanAt = DateTime.MinValue;
            for (var slot = 1; slot <= 5; slot++)
            {
                var path = AutoSavePath(slot);
                if (!File.Exists(path))
                    continue;
                var when = File.GetLastWriteTime(path);
                var id = ReadCareerFileId(path);
                if (string.IsNullOrEmpty(id))
                {
                    if (when > orphanAt)
                    {
                        orphanAt = when;
                        orphanId = path;
                    }
                    continue;
                }
                NoteAuto(map, id, when, ReadCareerLabel(path));
            }
            if (orphanAt == DateTime.MinValue)
                return;
            var fallback = ReadCareerFileId(PathFile);
            if (string.IsNullOrEmpty(fallback))
            {
                CareerSlotInfo newest = null;
                foreach (var info in map.Values)
                    if (info.HasManual && (newest == null || info.ManualAt > newest.ManualAt))
                        newest = info;
                if (newest != null)
                    fallback = newest.Id;
            }
            if (!string.IsNullOrEmpty(fallback))
                NoteAuto(map, fallback, orphanAt, ReadCareerLabel(orphanId));
        }

        static void NoteAuto(Dictionary<string, CareerSlotInfo> map, string id, DateTime when, string label)
        {
            if (string.IsNullOrEmpty(id) || !Guid.TryParseExact(id, "N", out _))
                return;
            if (!map.TryGetValue(id, out var info))
            {
                info = new CareerSlotInfo { Id = id, Label = string.IsNullOrEmpty(label) ? "Carreira" : label };
                map[id] = info;
            }
            if (!info.HasAuto || when > info.AutoAt)
            {
                info.HasAuto = true;
                info.AutoAt = when;
            }
        }

        static string NewestAutoSaveFor(string id)
        {
            string best = null;
            var bestAt = DateTime.MinValue;
            var mainId = ReadCareerFileId(PathFile);
            for (var slot = 1; slot <= 5; slot++)
            {
                var path = AutoSavePath(slot);
                if (!File.Exists(path))
                    continue;
                var fileId = ReadCareerFileId(path);
                var matches = fileId == id || (string.IsNullOrEmpty(fileId) && (mainId == id || string.IsNullOrEmpty(mainId)));
                if (!matches)
                    continue;
                var when = File.GetLastWriteTime(path);
                if (best == null || when > bestAt)
                {
                    best = path;
                    bestAt = when;
                }
            }
            return best;
        }

        static string ReadCareerFileId(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;
            try
            {
                foreach (var line in File.ReadLines(path, Encoding.UTF8))
                {
                    if (string.IsNullOrEmpty(line) || !line.StartsWith("careerfile|", StringComparison.Ordinal))
                        continue;
                    var bits = Split(line);
                    if (bits.Length > 1 && Guid.TryParseExact(bits[1], "N", out _))
                        return bits[1];
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("Não foi possível ler a carreira do autosave: " + error.Message);
            }
            return null;
        }

        static string ReadCareerLabel(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;
            try
            {
                foreach (var line in File.ReadLines(path, Encoding.UTF8))
                {
                    if (string.IsNullOrEmpty(line) || !line.StartsWith("careerfile|", StringComparison.Ordinal))
                        continue;
                    var bits = Split(line);
                    if (bits.Length > 2 && !string.IsNullOrEmpty(bits[2]))
                        return bits[2];
                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }

        public static void DeleteActiveFile()
        {
            try
            {
                if (File.Exists(PathFile))
                    File.Delete(PathFile);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Não foi possível apagar a gravação ativa: " + error.Message);
            }
        }

        static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? ""));

        static string Decode(string value)
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(value ?? "")); }
            catch { return ""; }
        }

        public static void AutoSave(CareerState career, int slots)
        {
            if (career == null || career.Coach == null)
                return;
            EnsureIdentity(career);
            slots = Mathf.Clamp(slots, 1, 5);
            var first = AutoSavePath(1);
            var staging = first + ".new";
            var previousTicks = career.LastAutoSaveDateTicks;
            career.LastAutoSaveDateTicks = career.Calendar.CurrentDate.Ticks;
            try
            {
                if (File.Exists(staging))
                    File.Delete(staging);
                if (!SaveTo(career, staging))
                {
                    career.LastAutoSaveDateTicks = previousTicks;
                    return;
                }
            }
            catch (System.Exception error)
            {
                career.LastAutoSaveDateTicks = previousTicks;
                Debug.LogWarning("Não foi possível criar o autosave: " + error.Message);
                return;
            }
            if (!File.Exists(staging))
            {
                career.LastAutoSaveDateTicks = previousTicks;
                return;
            }
            try
            {
                for (var i = slots; i > 1; i--)
                {
                    var source = AutoSavePath(i - 1);
                    var target = AutoSavePath(i);
                    if (File.Exists(source))
                        File.Copy(source, target, true);
                }
                var firstBackup = first + ".rollback";
                if (File.Exists(first))
                {
                    if (File.Exists(firstBackup))
                        File.Delete(firstBackup);
                    File.Replace(staging, first, firstBackup);
                    if (File.Exists(firstBackup))
                        File.Delete(firstBackup);
                }
                else
                    File.Move(staging, first);
                MirrorSlot(career, false);
                for (var i = slots + 1; i <= 5; i++)
                {
                    var unused = AutoSavePath(i);
                    if (File.Exists(unused))
                        File.Delete(unused);
                }
            }
            catch (System.Exception error)
            {
                career.LastAutoSaveDateTicks = previousTicks;
                Debug.LogWarning("Não foi possível guardar o autosave: " + error.Message);
                if (File.Exists(staging))
                    File.Delete(staging);
                var firstBackup = first + ".rollback";
                if (File.Exists(firstBackup) && !File.Exists(first))
                    File.Move(firstBackup, first);
            }
        }

        public static void AutoSaveIfDue(CareerState career, int intervalDays, int slots)
        {
            if (career == null || career.Calendar == null)
                return;
            intervalDays = Mathf.Max(1, intervalDays);
            var lastTicks = career.LastAutoSaveDateTicks;
            if (lastTicks <= 0)
                lastTicks = career.Calendar.CurrentDate.Ticks;
            var days = (career.Calendar.CurrentDate.Date - new System.DateTime(lastTicks).Date).Days;
            if (days < intervalDays)
                return;
            AutoSave(career, slots);
        }

        static bool SaveTo(CareerState career, string outputPath)
        {
            if (career == null || career.Coach == null)
                return false;
            career.LastPlayedUtcTicks = DateTime.UtcNow.Ticks;
            var lines = new List<string>
            {
                "coach|" + Esc(career.Coach.Name) + "|" + career.Coach.ClubId + "|" + career.Calendar.CurrentDate.Ticks,
                "careerprogress|" + career.CompletedSeasons,
                "fanmood|" + career.SupporterApproval,
                "simulation|" + Mathf.Clamp(career.InjuryFrequency, 0, 2) + "|" + Mathf.Clamp(career.RefereeStrictness, 0, 2) + "|" + Mathf.Clamp(career.RealismPreset, 0, 4),
                "matchspeed|" + Mathf.Clamp(career.MatchSpeedIndex, 0, 4),
                "gameplay|" + (career.AutomaticSquadSelection ? "1" : "0") + "|" + (career.AutomaticTraining ? "1" : "0") + "|" + Mathf.Clamp(career.MatchHighlights, 0, 3) + "|" + (career.PauseOnGoal ? "1" : "0") + "|" + (career.PauseOnRedCard ? "1" : "0") + "|" + (career.PauseOnInjury ? "1" : "0") + "|" + (career.LiveRatings ? "1" : "0"),
                "databasecustomization|" + (career.DatabaseCustomizationEnabled ? "1" : "0"),
                "autosave|" + career.LastAutoSaveDateTicks,
                "lastplayed|" + career.LastPlayedUtcTicks,
                "playtime|" + Math.Max(0d, career.PlayTimeSeconds).ToString("R", CultureInfo.InvariantCulture),
                "careerfile|" + (career.FileId ?? "") + "|" + Esc(career.SaveLabel),
                "financeledger|3"
            };
            lines.Add("academy|" + string.Join(",", career.AcademyPlayerIds));
            foreach (var academyTactic in career.AcademyTactics)
            {
                var t = academyTactic.Value;
                if (t == null) continue;
                lines.Add("academytactic|" + Esc(academyTactic.Key) + "|" + Esc(t.Formation) + "|" + Esc(t.Pressing) + "|" + Esc(t.BuildUp) + "|" + Esc(t.FullBacks) + "|" + Esc(t.DefensiveLine) + "|" + Esc(t.Width) + "|" + Esc(t.Tempo) + "|" + Esc(t.PassingStyle) + "|" + Esc(t.AttackIdea) + "|" + Esc(t.Marking));
            }
            foreach (var order in career.AcademySquadOrders)
                lines.Add("academysquadorder|" + Esc(order.Key) + "|" + string.Join(",", order.Value));
            foreach (var role in career.AcademyRoleAssignments)
                lines.Add("academyrole|" + Esc(role.Key) + "|" + role.Value);
            foreach (var academyClubId in career.AcademyClubIds)
                lines.Add("academyclub|" + academyClubId);
            lines.Add("scoutedclub|" + string.Join(",", career.Scouted));
            foreach (var academyClubId in career.AcademyScheduleClubIds)
                lines.Add("academyschedule|" + academyClubId);
            lines.Add("globaltransferdate|" + career.LastGlobalTransferTicks);
            lines.Add("worldnewsdate|" + career.LastWorldNewsTicks);
            foreach (var fixture in career.AcademyFixtures)
                lines.Add("academyfixture|" + Esc(fixture.Id) + "|" + fixture.ClubId + "|" + fixture.AgeGroup + "|" + fixture.Date.Ticks + "|" + fixture.HomeClubId + "|" + fixture.AwayClubId + "|" + (fixture.Played ? "1" : "0") + "|" + fixture.HomeGoals + "|" + fixture.AwayGoals);
            foreach (var transfer in career.TransferHistory)
                lines.Add("transfer|" + transfer.DateTicks + "|" + transfer.PlayerId + "|" + transfer.FromClubId + "|" + transfer.ToClubId + "|" + transfer.Fee + "|" + transfer.WeeklyWage + "|" + transfer.ContractYears + "|" + transfer.AgeAtTransfer + "|" + transfer.OverallAtTransfer + "|" + Esc(transfer.Type));
            foreach (var trophy in career.Trophies)
                lines.Add("trophy|" + trophy.ClubId + "|" + trophy.CompetitionId + "|" + trophy.SeasonStartYear + "|" + trophy.DateTicks + "|" + Esc(trophy.Title));
            foreach (var memory in career.Memories)
                lines.Add("memory|" + memory.WhenTicks + "|" + memory.Importance + "|" + Esc(memory.Kind) + "|" + memory.ClubId + "|" + memory.CompetitionId + "|" + memory.RelatedClubId + "|" + memory.PlayerId + "|" + Esc(memory.Title) + "|" + Esc(memory.Detail) + "|" + memory.Value);
            foreach (var trust in career.ManagerTrust)
                lines.Add("trust|" + trust.Key + "|" + trust.Value);
            foreach (var offer in career.JobOffers)
                lines.Add("joboffer|" + offer.ClubId + "|" + offer.ReceivedAtTicks + "|" + (offer.Resolved ? "1" : "0") + "|" + (offer.Accepted ? "1" : "0") + "|" + Esc(offer.Reason));
            var tactics = career.Club.Tactics;
            lines.Add("tactic|" + Esc(tactics.Formation) + "|" + Esc(tactics.Pressing) + "|" + Esc(tactics.BuildUp) + "|" + Esc(tactics.FullBacks) + "|" + Esc(tactics.DefensiveLine) + "|" + Esc(tactics.Width) + "|" + Esc(tactics.Tempo) + "|" + Esc(tactics.PassingStyle) + "|" + Esc(tactics.AttackIdea) + "|" + Esc(tactics.Marking));
            foreach (var saved in career.SavedTactics)
            {
                var savedTactics = saved == null ? null : saved.Tactics;
                if (savedTactics == null) continue;
                lines.Add("savedtactic|" + Esc(saved.Name) + "|" + Esc(savedTactics.Formation) + "|" + Esc(savedTactics.Pressing) + "|" + Esc(savedTactics.BuildUp) + "|" + Esc(savedTactics.FullBacks) + "|" + Esc(savedTactics.DefensiveLine) + "|" + Esc(savedTactics.Width) + "|" + Esc(savedTactics.Tempo) + "|" + Esc(savedTactics.PassingStyle) + "|" + Esc(savedTactics.AttackIdea) + "|" + Esc(savedTactics.Marking)
                    + "|" + string.Join(",", saved.Lineup ?? new System.Collections.Generic.List<string>())
                    + "|" + string.Join(",", saved.Bench ?? new System.Collections.Generic.List<string>())
                    + "|" + saved.CaptainId + "|" + saved.PenaltyTakerId + "|" + saved.CornerTakerId + "|" + saved.FreeKickTakerId
                    + "|" + Esc(saved.Duties));
            }
            lines.Add("roles|" + career.Club.PenaltyTakerId + "|" + career.Club.CornerTakerId + "|" + career.Club.FreeKickTakerId);
            lines.Add("lineup|" + string.Join(",", career.Club.ManualLineup));
            lines.Add("benchorder|" + string.Join(",", career.Club.ManualBenchOrder));
            foreach (var player in career.World.Players.Values)
            {
                var hurt = player.Injury != null && player.Injury.IsInjured ? "1" : "0";
                var days = player.Injury == null ? 0 : player.Injury.DaysRemaining;
                var desc = player.Injury == null ? "" : player.Injury.Description;
                lines.Add("player|" + player.Id + "|" + player.Morale + "|" + player.Fatigue + "|" + Esc(player.Duty) + "|" + (player.IsCaptain ? "1" : "0") + "|" + hurt + "|" + days + "|" + Esc(desc) + "|" + player.ClubId + "|" + player.Age + "|" + TraitCodes(player) + "|" + player.Form + "|" + player.Fitness + "|" + player.TrainingRating + "|" + player.HeightCm + "|" + player.WeightKg + "|" + player.WeakFootQuality + "|" + PlayerPositionProfile.Encode(player) + "|" + player.Reputation + "|" + player.WeeklyWage + "|" + player.ContractEndTicks + "|" + Esc(player.TrainingFocus) + "|" + player.TrainingProgress + "|" + player.TrainingIntensity + "|" + player.TrainingStreak + "|" + player.UnavailableUntilTicks + "|" + Esc(player.AbsenceKind) + "|" + Esc(player.YellowBook));
                if (career.DatabaseCustomizationEnabled)
                {
                    var a = player.Attributes ?? new PlayerAttributes();
                    lines.Add("playerdatabase|" + player.Id + "|" + Esc(player.FullName) + "|" + Esc(player.Nationality) + "|" + Esc((player.PortraitPath ?? "").Replace("|", "~")) + "|" + player.Potential
                        + "|" + a.Pace + "|" + a.Stamina + "|" + a.Strength + "|" + a.Passing + "|" + a.Finishing + "|" + a.Dribbling + "|" + a.Tackling + "|" + a.Crossing
                        + "|" + a.Positioning + "|" + a.Composure + "|" + a.WorkRate + "|" + a.Decisions + "|" + a.Reflexes + "|" + a.Handling);
                }
            }

            foreach (var club in career.World.Clubs.Values)
            {
                lines.Add("club|" + club.Id + "|" + club.LeagueId);
                if (career.DatabaseCustomizationEnabled)
                {
                    lines.Add("clubdatabase|" + club.Id + "|" + Esc(club.Name) + "|" + Esc(club.ShortName) + "|" + Esc((club.CrestResourcePath ?? "").Replace("|", "~")) + "|" + Esc(club.CustomCrestKey)
                        + "|" + Esc(club.JerseyPrimaryHex) + "|" + Esc(club.JerseySecondaryHex) + "|" + Esc(club.JerseyShortsHex) + "|" + club.Reputation);
                    lines.Add("coachdatabase|" + club.Id + "|" + Esc(club.Tactics == null ? "" : club.Tactics.FormerCoachName));
                    lines.Add("sponsorset|" + club.Id);
                    if (club.Sponsors != null)
                        foreach (var sponsor in club.Sponsors)
                            if (sponsor != null)
                                lines.Add("sponsor|" + club.Id + "|" + Esc(sponsor.Id) + "|" + Esc(sponsor.Name) + "|" + sponsor.WeeklyIncome + "|" + Esc(sponsor.LogoKey) + "|" + Esc(sponsor.LogoHex) + "|" + (sponsor.IsActive ? "1" : "0"));
                }
                lines.Add("clubstate|" + club.Id + "|" + club.DressingRoomTension);
                lines.Add("clubmanager|" + club.Id + "|" + Esc(club.PublicCoachVacancyNote) + "|" + Esc(club.HiddenDismissalTruth));
                lines.Add("squad|" + club.Id + "|" + string.Join(",", club.PlayerIds));
                lines.Add("stafflist|" + club.Id);
                if (club.Staff != null)
                    foreach (var member in club.Staff)
                        if (member != null)
                            lines.Add("staff|" + club.Id + "|" + Esc(member.Id) + "|" + Esc(member.FullName) + "|" + (int)member.Role + "|" + member.Judgement + "|" + member.WeeklyWage + "|" + member.ContractEndTicks + "|" + Esc(member.PortraitPath));
                if (club.Finance != null)
                {
                    lines.Add("clubfinance|" + club.Id + "|" + club.Finance.Balance + "|" + club.Finance.WageBudget + "|" + club.Finance.TransferBudget + "|" + club.Finance.TransferSpentThisSeason + "|" + club.Finance.TicketIncomeThisSeason + "|" + club.Finance.ShirtIncomeThisSeason + "|" + club.Finance.PlayerSalesThisSeason + "|" + club.Finance.SponsorIncomeThisSeason);
                    foreach (var transaction in club.Finance.Transactions)
                        lines.Add("financetransaction|" + club.Id + "|" + transaction.DateTicks + "|" + transaction.Amount + "|" + Esc(transaction.Category) + "|" + Esc(transaction.Description) + "|" + Esc(transaction.PlayerId));
                }
            }

            foreach (var fixture in career.Fixtures)
                lines.Add("fixture|" + fixture.Id + "|" + (fixture.Played ? "1" : "0") + "|" + fixture.HomeGoals + "|" + fixture.AwayGoals + "|" + fixture.LeagueId + "|" + fixture.HomeClubId + "|" + fixture.AwayClubId + "|" + fixture.Date.Ticks);
            foreach (var row in career.Table)
                lines.Add("table|" + row.LeagueId + "|" + row.ClubId + "|" + row.Played + "|" + row.Won + "|" + row.Drawn + "|" + row.Lost + "|" + row.GoalsFor + "|" + row.GoalsAgainst + "|" + row.Points);
            foreach (var story in career.Press)
                lines.Add("press|" + Esc(story.Paper) + "|" + Esc(story.Headline) + "|" + Esc(story.Deck) + "|" + Esc(story.Published) + "|" + Esc(story.Kicker) + "|" + story.HomeClubId + "|" + story.AwayClubId + "|" + Esc(story.Motm) + "|" + Esc(story.Quote) + "|" + Esc(story.QuoteBy) + "|" + Esc(story.Body) + "|" + story.DateTicks);
            foreach (var note in career.Board)
                lines.Add("board|" + Esc(note.Day) + "|" + Esc(note.Hour) + "|" + note.When.Ticks + "|" + Esc(note.Text));
            foreach (var line in career.Notebook)
                lines.Add("note|" + Esc(line));
            if (career.Online != null && career.Online.Seats.Count > 0)
            {
                lines.Add("online|" + career.Online.Pack());
                foreach (var seat in career.Online.Seats)
                {
                    if (seat == null || career.World == null || !career.World.Clubs.TryGetValue(seat.ClubId, out var seatClub) || seatClub == null)
                        continue;
                    var seatTactics = seatClub.Tactics;
                    if (seatTactics != null)
                        lines.Add("seattactic|" + seat.ClubId + "|" + Esc(seatTactics.Formation) + "|" + Esc(seatTactics.Pressing) + "|" + Esc(seatTactics.BuildUp) + "|" + Esc(seatTactics.FullBacks) + "|" + Esc(seatTactics.DefensiveLine) + "|" + Esc(seatTactics.Width) + "|" + Esc(seatTactics.Tempo) + "|" + Esc(seatTactics.PassingStyle) + "|" + Esc(seatTactics.AttackIdea) + "|" + Esc(seatTactics.Marking));
                    lines.Add("seatlineup|" + seat.ClubId + "|" + string.Join(",", seatClub.ManualLineup));
                    lines.Add("seatbench|" + seat.ClubId + "|" + string.Join(",", seatClub.ManualBenchOrder));
                    lines.Add("seatroles|" + seat.ClubId + "|" + seatClub.PenaltyTakerId + "|" + seatClub.CornerTakerId + "|" + seatClub.FreeKickTakerId);
                }
            }
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
            var tempPath = outputPath + ".tmp";
            var backupPath = outputPath + ".backup";
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllLines(tempPath, lines, Encoding.UTF8);
            if (!File.Exists(outputPath))
            {
                File.Move(tempPath, outputPath);
                return true;
            }

            try
            {
                if (File.Exists(backupPath))
                    File.Delete(backupPath);
                File.Replace(tempPath, outputPath, backupPath);
            }
            catch
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                Debug.LogWarning("Não foi possível atualizar o ficheiro de carreira. A versão anterior foi mantida.");
                return false;
            }
            return true;
        }

        public static string ReadText()
        {
            try
            {
                return File.Exists(PathFile) ? File.ReadAllText(PathFile, Encoding.UTF8) : "";
            }
            catch (Exception error)
            {
                Debug.LogWarning("Não foi possível ler a carreira para a rede: " + error.Message);
                return "";
            }
        }

        public static void AdoptShared(GameSession session, string text, string clubId)
        {
            if (session == null || string.IsNullOrEmpty(text))
                return;
            try
            {
                var folder = Path.GetDirectoryName(PathFile);
                if (!string.IsNullOrEmpty(folder))
                    Directory.CreateDirectory(folder);
                var backup = PathFile + ".antes-online";
                if (!File.Exists(backup) && File.Exists(PathFile))
                    File.Copy(PathFile, backup);
                File.WriteAllText(PathFile, text, Encoding.UTF8);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Não foi possível receber a carreira online: " + error.Message);
                return;
            }
            CareerOnline.PendingClub = clubId;
            TryLoad(session);
        }

        public static CareerState TryLoad(GameSession session)
        {
            var career = TryLoadFrom(session, PathFile);
            if (career != null)
                return career;
            var backup = PathFile + ".backup";
            return File.Exists(backup) ? TryLoadFrom(session, backup) : null;
        }

        public static CareerState TryLoadMostRecent(GameSession session)
        {
            if (session == null)
                return null;
            var slots = ListSlots();
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null || string.IsNullOrEmpty(slot.Id))
                    continue;
                var loadManual = slot.HasManual && (!slot.HasAuto || slot.ManualAt >= slot.AutoAt);
                var career = TryLoadSlot(session, slot.Id, loadManual);
                if (career != null)
                    return career;
            }
            return TryLoad(session);
        }

        public static CareerState TryLoadAutoSave(GameSession session, int slot = 1)
        {
            return TryLoadFrom(session, AutoSavePath(Mathf.Clamp(slot, 1, 5)));
        }

        static CareerState TryLoadFrom(GameSession session, string path)
        {
            if (session == null || !File.Exists(path))
                return null;
            CareerNotificationQueue.Clear();
            string[] lines;
            try
            {
                lines = File.ReadAllLines(path, Encoding.UTF8);
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
                ModFolder.Apply(career);
            }
            catch
            {
                return null;
            }

            var hasSavedAcademyPlayers = false;
            var hasFinanceLedger = false;
            var financeLedgerVersion = 0;
            foreach (var raw in lines)
            {
                if (raw.StartsWith("academy|"))
                    hasSavedAcademyPlayers = true;
                else if (raw.StartsWith("academyclub|"))
                    AcademyFactory.Ensure(career, raw.Substring("academyclub|".Length));
                else if (raw.StartsWith("financeledger|"))
                {
                    hasFinanceLedger = true;
                    var versionBits = raw.Split('|');
                    if (versionBits.Length > 1) int.TryParse(versionBits[1], out financeLedgerVersion);
                }
            }
            if (hasSavedAcademyPlayers)
                career.AcademyPlayerIds.Clear();

            var generatedFixtures = new List<Fixture>(career.Fixtures);
            var generatedTable = new List<LeagueTableRow>(career.Table);
            var savedClubIds = new HashSet<string>();
            var savedStaffClubIds = new HashSet<string>();
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
                    case "simulation":
                        if (bits.Length > 3)
                        {
                            int.TryParse(bits[1], out var injuryFrequency);
                            int.TryParse(bits[2], out var refereeStrictness);
                            int.TryParse(bits[3], out var realismPreset);
                            career.InjuryFrequency = Mathf.Clamp(injuryFrequency, 0, 2);
                            career.RefereeStrictness = Mathf.Clamp(refereeStrictness, 0, 2);
                            career.RealismPreset = Mathf.Clamp(realismPreset, 0, 4);
                        }
                        break;
                    case "matchspeed":
                        if (bits.Length > 1 && int.TryParse(bits[1], out var savedSpeed))
                            career.MatchSpeedIndex = Mathf.Clamp(savedSpeed, 0, 4);
                        break;
                    case "gameplay":
                        if (bits.Length > 7)
                        {
                            career.AutomaticSquadSelection = bits[1] == "1";
                            career.AutomaticTraining = bits[2] == "1";
                            if (int.TryParse(bits[3], out var savedHighlights))
                                career.MatchHighlights = Mathf.Clamp(savedHighlights, 0, 3);
                            career.PauseOnGoal = bits[4] == "1";
                            career.PauseOnRedCard = bits[5] == "1";
                            career.PauseOnInjury = bits[6] == "1";
                            career.LiveRatings = bits[7] == "1";
                        }
                        break;
                    case "databasecustomization":
                        career.DatabaseCustomizationEnabled = bits.Length > 1 && bits[1] == "1";
                        break;
                    case "autosave":
                        if (bits.Length > 1 && long.TryParse(bits[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var autosaveTicks))
                            career.LastAutoSaveDateTicks = autosaveTicks;
                        break;
                    case "lastplayed":
                        if (bits.Length > 1 && long.TryParse(bits[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var lastPlayedTicks))
                            career.LastPlayedUtcTicks = lastPlayedTicks;
                        break;
                    case "playtime":
                        if (bits.Length > 1 && double.TryParse(bits[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var playTime))
                            career.PlayTimeSeconds = Math.Max(0d, playTime);
                        break;
                    case "careerfile":
                        if (bits.Length > 1 && Guid.TryParseExact(bits[1], "N", out _))
                            career.FileId = bits[1];
                        if (bits.Length > 2)
                            career.SaveLabel = Un(bits[2]);
                        break;
                    case "financeledger":
                        hasFinanceLedger = true;
                        break;
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
                    case "academytactic":
                        if (bits.Length > 11)
                        {
                            career.AcademyTactics[Un(bits[1])] = new TacticalIdentity
                            {
                                Formation = Un(bits[2]), Pressing = Un(bits[3]), BuildUp = Un(bits[4]),
                                FullBacks = Un(bits[5]), DefensiveLine = Un(bits[6]), Width = Un(bits[7]),
                                Tempo = Un(bits[8]), PassingStyle = Un(bits[9]), AttackIdea = Un(bits[10]), Marking = Un(bits[11])
                            };
                        }
                        break;
                    case "savedtactic":
                        if (bits.Length > 11)
                        {
                            var savedTactic = new CareerSavedTactic
                            {
                                Name = Un(bits[1]),
                                Tactics = new TacticalIdentity
                                {
                                    Formation = Un(bits[2]), Pressing = Un(bits[3]), BuildUp = Un(bits[4]),
                                    FullBacks = Un(bits[5]), DefensiveLine = Un(bits[6]), Width = Un(bits[7]),
                                    Tempo = Un(bits[8]), PassingStyle = Un(bits[9]), AttackIdea = Un(bits[10]), Marking = Un(bits[11]),
                                    Preset = TacticPresets.CustomName
                                }
                            };
                            if (bits.Length > 12 && !string.IsNullOrEmpty(bits[12]))
                                savedTactic.Lineup.AddRange(bits[12].Split(','));
                            if (bits.Length > 13 && !string.IsNullOrEmpty(bits[13]))
                                savedTactic.Bench.AddRange(bits[13].Split(','));
                            if (bits.Length > 14) savedTactic.CaptainId = bits[14];
                            if (bits.Length > 15) savedTactic.PenaltyTakerId = bits[15];
                            if (bits.Length > 16) savedTactic.CornerTakerId = bits[16];
                            if (bits.Length > 17) savedTactic.FreeKickTakerId = bits[17];
                            if (bits.Length > 18) savedTactic.Duties = Un(bits[18]);
                            career.SavedTactics.Add(savedTactic);
                        }
                        break;
                    case "academysquadorder":
                        if (bits.Length > 2)
                        {
                            var group = Un(bits[1]);
                            if (!career.AcademySquadOrders.TryGetValue(group, out var order))
                            {
                                order = new List<string>();
                                career.AcademySquadOrders[group] = order;
                            }
                            order.Clear();
                            if (!string.IsNullOrEmpty(bits[2])) order.AddRange(bits[2].Split(','));
                        }
                        break;
                    case "scoutedclub":
                        career.Scouted.Clear();
                        AddIds(career.Scouted, bits);
                        break;
                    case "academyrole":
                        if (bits.Length > 2)
                            career.AcademyRoleAssignments[Un(bits[1])] = bits[2];
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
                    case "benchorder":
                        career.Club.ManualBenchOrder.Clear();
                        if (bits.Length > 1 && !string.IsNullOrEmpty(bits[1]))
                            career.Club.ManualBenchOrder.AddRange(bits[1].Split(','));
                        break;
                    case "player":
                        ApplyPlayer(career, bits);
                        break;
                    case "playerdatabase":
                        ApplyPlayerDatabase(career, bits);
                        break;
                    case "club":
                        if (bits.Length > 2 && career.World.Clubs.TryGetValue(bits[1], out var savedClub))
                            savedClub.LeagueId = bits[2];
                        break;
                    case "clubdatabase":
                        ApplyClubDatabase(career, bits);
                        break;
                    case "coachdatabase":
                        if (bits.Length > 2 && career.World.Clubs.TryGetValue(bits[1], out var namedCoachClub) && namedCoachClub.Tactics != null)
                            namedCoachClub.Tactics.FormerCoachName = Un(bits[2]);
                        break;
                    case "sponsorset":
                        if (bits.Length > 1 && career.World.Clubs.TryGetValue(bits[1], out var sponsorClub))
                        {
                            if (sponsorClub.Sponsors == null) sponsorClub.Sponsors = new List<SponsorContract>();
                            sponsorClub.Sponsors.Clear();
                        }
                        break;
                    case "sponsor":
                        if (bits.Length > 7 && career.World.Clubs.TryGetValue(bits[1], out var savedSponsorClub))
                        {
                            if (savedSponsorClub.Sponsors == null) savedSponsorClub.Sponsors = new List<SponsorContract>();
                            int.TryParse(bits[4], out var sponsorWeekly);
                            savedSponsorClub.Sponsors.Add(new SponsorContract
                            {
                                Id = Un(bits[2]), Name = Un(bits[3]), WeeklyIncome = Math.Max(0, sponsorWeekly),
                                LogoKey = Un(bits[5]), LogoHex = Un(bits[6]), IsActive = bits[7] == "1"
                            });
                        }
                        break;
                    case "clubstate":
                        if (bits.Length > 2 && career.World.Clubs.TryGetValue(bits[1], out var stateClub) && int.TryParse(bits[2], out var tension))
                            stateClub.DressingRoomTension = Math.Max(0, Math.Min(100, tension));
                        break;
                    case "clubmanager":
                        if (bits.Length > 3 && career.World.Clubs.TryGetValue(bits[1], out var managerClub))
                        {
                            managerClub.PublicCoachVacancyNote = Un(bits[2]);
                            managerClub.HiddenDismissalTruth = Un(bits[3]);
                        }
                        break;
                    case "staff":
                        if (bits.Length > 8 && career.World.Clubs.TryGetValue(bits[1], out var staffClub))
                        {
                            if (staffClub.Staff == null)
                                staffClub.Staff = new List<StaffMember>();
                            if (savedStaffClubIds.Add(bits[1]))
                                staffClub.Staff.Clear();
                            int.TryParse(bits[4], out var staffRole);
                            int.TryParse(bits[5], out var staffJudgement);
                            int.TryParse(bits[6], out var staffWage);
                            long.TryParse(bits[7], out var staffContractEnd);
                            staffClub.Staff.Add(new StaffMember
                            {
                                Id = Un(bits[2]),
                                ClubId = bits[1],
                                FullName = Un(bits[3]),
                                Role = (StaffRole)staffRole,
                                Judgement = staffJudgement,
                                WeeklyWage = Math.Max(0, staffWage),
                                ContractEndTicks = staffContractEnd,
                                PortraitPath = Un(bits[8])
                            });
                        }
                        break;
                    case "stafflist":
                        if (bits.Length > 1 && career.World.Clubs.TryGetValue(bits[1], out var savedStaffClub))
                        {
                            if (savedStaffClub.Staff == null)
                                savedStaffClub.Staff = new List<StaffMember>();
                            savedStaffClub.Staff.Clear();
                            savedStaffClubIds.Add(bits[1]);
                        }
                        break;
                    case "squad":
                        if (bits.Length > 1 && career.World.Clubs.TryGetValue(bits[1], out var squadClub))
                        {
                            squadClub.PlayerIds.Clear();
                            if (bits.Length > 2 && !string.IsNullOrEmpty(bits[2]))
                                squadClub.PlayerIds.AddRange(bits[2].Split(','));
                        }
                        break;
                    case "clubfinance":
                        if (bits.Length > 5 && career.World.Clubs.TryGetValue(bits[1], out var financeClub) && financeClub.Finance != null)
                        {
                            int.TryParse(bits[2], out var balance);
                            int.TryParse(bits[3], out var wageBudget);
                            int.TryParse(bits[4], out var transferBudget);
                            int.TryParse(bits[5], out var transferSpent);
                            financeClub.Finance.Balance = balance;
                            financeClub.Finance.WageBudget = wageBudget;
                            financeClub.Finance.TransferBudget = transferBudget;
                            financeClub.Finance.TransferSpentThisSeason = Math.Max(0, transferSpent);
                            if (bits.Length > 6 && int.TryParse(bits[6], out var ticketIncome)) financeClub.Finance.TicketIncomeThisSeason = Math.Max(0, ticketIncome);
                            if (bits.Length > 7 && int.TryParse(bits[7], out var shirtIncome)) financeClub.Finance.ShirtIncomeThisSeason = Math.Max(0, shirtIncome);
                            if (bits.Length > 8 && int.TryParse(bits[8], out var playerSales)) financeClub.Finance.PlayerSalesThisSeason = Math.Max(0, playerSales);
                            if (bits.Length > 9 && int.TryParse(bits[9], out var sponsorIncome)) financeClub.Finance.SponsorIncomeThisSeason = Math.Max(0, sponsorIncome);
                        }
                        break;
                    case "financetransaction":
                        if (bits.Length > 5 && career.World.Clubs.TryGetValue(bits[1], out var transactionClub)
                            && transactionClub.Finance != null && long.TryParse(bits[2], out var transactionTicks)
                            && int.TryParse(bits[3], out var transactionAmount))
                        {
                            transactionClub.Finance.Transactions.Add(new FinanceTransaction
                            {
                                DateTicks = transactionTicks,
                                Amount = transactionAmount,
                                Category = Un(bits[4]),
                                Description = Un(bits[5]),
                                PlayerId = bits.Length > 6 ? Un(bits[6]) : ""
                            });
                            if (transactionClub.Finance.Transactions.Count > 1200)
                                transactionClub.Finance.Transactions.RemoveAt(0);
                        }
                        break;
                    case "academyclub":
                        if (bits.Length > 1)
                            career.AcademyClubIds.Add(bits[1]);
                        break;
                    case "academyschedule":
                        if (bits.Length > 1)
                            career.AcademyScheduleClubIds.Add(bits[1]);
                        break;
                    case "globaltransferdate":
                        if (bits.Length > 1 && long.TryParse(bits[1], out var transferTicks)) career.LastGlobalTransferTicks = transferTicks;
                        break;
                    case "worldnewsdate":
                        if (bits.Length > 1 && long.TryParse(bits[1], out var newsTicks)) career.LastWorldNewsTicks = newsTicks;
                        break;
                    case "academyfixture":
                        if (bits.Length > 9 && long.TryParse(bits[4], out var academyDateTicks))
                        {
                            int.TryParse(bits[8], out var academyHomeGoals);
                            int.TryParse(bits[9], out var academyAwayGoals);
                            career.AcademyFixtures.Add(new AcademyFixture
                            {
                                Id = Un(bits[1]),
                                ClubId = bits[2],
                                AgeGroup = bits[3],
                                Date = new DateTime(academyDateTicks),
                                HomeClubId = bits[5],
                                AwayClubId = bits[6],
                                Played = bits[7] == "1",
                                HomeGoals = academyHomeGoals,
                                AwayGoals = academyAwayGoals
                            });
                        }
                        break;
                    case "transfer":
                        if (bits.Length > 10 && long.TryParse(bits[1], out var transferDateTicks))
                        {
                            int.TryParse(bits[5], out var fee);
                            int.TryParse(bits[6], out var weeklyWage);
                            int.TryParse(bits[7], out var contractYears);
                            int.TryParse(bits[8], out var transferAge);
                            int.TryParse(bits[9], out var transferOverall);
                            career.TransferHistory.Add(new TransferRecord
                            {
                                DateTicks = transferDateTicks,
                                PlayerId = bits[2],
                                FromClubId = bits[3],
                                ToClubId = bits[4],
                                Fee = Math.Max(0, fee),
                                WeeklyWage = Math.Max(0, weeklyWage),
                                ContractYears = Math.Max(0, contractYears),
                                AgeAtTransfer = Math.Max(0, transferAge),
                                OverallAtTransfer = Math.Max(0, transferOverall),
                                Type = Un(bits[10])
                            });
                        }
                        break;
                    case "trophy":
                        if (bits.Length > 5 && int.TryParse(bits[3], out var trophySeason) && long.TryParse(bits[4], out var trophyDate))
                            career.Trophies.Add(new CareerTrophy
                            {
                                ClubId = bits[1], CompetitionId = bits[2], SeasonStartYear = trophySeason,
                                DateTicks = trophyDate, Title = Un(bits[5])
                            });
                        break;
                    case "academy":
                        if (bits.Length > 1 && !string.IsNullOrEmpty(bits[1]))
                            career.AcademyPlayerIds.AddRange(bits[1].Split(','));
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
                                Body = Un(bits[11]),
                                DateTicks = bits.Length > 12 && long.TryParse(bits[12], NumberStyles.Integer, CultureInfo.InvariantCulture, out var pressTicks)
                                    ? pressTicks : 0
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
                    case "online":
                        if (raw.Length > 7)
                            career.Online = CareerOnline.Parse(raw.Substring(7));
                        break;
                    case "seattactic":
                        if (bits.Length > 9 && career.World != null && career.World.Clubs.TryGetValue(bits[1], out var seatTacticClub) && seatTacticClub.Tactics != null)
                        {
                            var seatTactics = seatTacticClub.Tactics;
                            seatTactics.Formation = Un(bits[2]);
                            seatTactics.Pressing = Un(bits[3]);
                            seatTactics.BuildUp = Un(bits[4]);
                            seatTactics.FullBacks = Un(bits[5]);
                            seatTactics.DefensiveLine = Un(bits[6]);
                            seatTactics.Width = Un(bits[7]);
                            seatTactics.Tempo = Un(bits[8]);
                            seatTactics.PassingStyle = Un(bits[9]);
                            if (bits.Length > 10)
                                seatTactics.AttackIdea = Un(bits[10]);
                            if (bits.Length > 11)
                                seatTactics.Marking = Un(bits[11]);
                            seatTactics.Summary = seatTactics.Formation + " · " + seatTactics.Pressing + " · " + seatTactics.BuildUp;
                        }
                        break;
                    case "seatlineup":
                        if (bits.Length > 1 && career.World != null && career.World.Clubs.TryGetValue(bits[1], out var seatLineupClub))
                        {
                            seatLineupClub.ManualLineup.Clear();
                            if (bits.Length > 2 && !string.IsNullOrEmpty(bits[2]))
                                seatLineupClub.ManualLineup.AddRange(bits[2].Split(','));
                        }
                        break;
                    case "seatbench":
                        if (bits.Length > 1 && career.World != null && career.World.Clubs.TryGetValue(bits[1], out var seatBenchClub))
                        {
                            seatBenchClub.ManualBenchOrder.Clear();
                            if (bits.Length > 2 && !string.IsNullOrEmpty(bits[2]))
                                seatBenchClub.ManualBenchOrder.AddRange(bits[2].Split(','));
                        }
                        break;
                    case "seatroles":
                        if (bits.Length > 4 && career.World != null && career.World.Clubs.TryGetValue(bits[1], out var seatRoleClub))
                        {
                            seatRoleClub.PenaltyTakerId = bits[2];
                            seatRoleClub.CornerTakerId = bits[3];
                            seatRoleClub.FreeKickTakerId = bits[4];
                        }
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
            if (!hasFinanceLedger)
                ReconcileLegacyFinance(career);
            if (financeLedgerVersion < 2)
                UpgradePremierLeagueFinances(career);
            if (financeLedgerVersion < 3)
                NormalizeLegacyWeeklyWages(career);
            if (career.World != null && career.World.Clubs != null)
            {
                foreach (var club in career.World.Clubs.Values)
                {
                    WorldCatalog.RefreshPresentedClub(club);
                    WorldCatalog.EnsureThreeColors(club);
                }
            }
            CareerOnline.BindMachine(career);
            SettingsPrefs.ApplyCareerGameplay(career);
            if (career.LastAutoSaveDateTicks <= 0)
                career.LastAutoSaveDateTicks = career.Calendar.CurrentDate.Ticks;
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
            SeasonSchedule.SpreadCalendar(career);
            SeasonBook.Backfill(career);
            SeasonBook.ReconcileCurrentSeason(career);
            session.Restore(career);
            return career;
        }

        static void ReconcileLegacyFinance(CareerState career)
        {
            if (career == null || career.Calendar == null) return;
            var year = career.Calendar.CurrentDate.Month >= 7 ? career.Calendar.CurrentDate.Year : career.Calendar.CurrentDate.Year - 1;
            var seasonStart = new System.DateTime(year, 7, 1);
            foreach (var fixture in career.Fixtures)
            {
                if (!fixture.Played || fixture.Date.Date < seasonStart || fixture.Date.Date > career.Calendar.CurrentDate.Date
                    || !career.World.Clubs.TryGetValue(fixture.HomeClubId, out var home) || home.Finance == null)
                    continue;
                var ticketIncome = System.Math.Max(12000, home.Reputation * 650);
                var shirtIncome = System.Math.Max(1800, home.Reputation * 95);
                home.Finance.TicketIncomeThisSeason += ticketIncome;
                home.Finance.ShirtIncomeThisSeason += shirtIncome;
                home.Finance.Balance += ticketIncome + shirtIncome;
            }
        }

        static void NormalizeLegacyWeeklyWages(CareerState career)
        {
            if (career == null || career.World == null)
                return;

            foreach (var player in career.World.Players.Values)
                if (player != null && player.WeeklyWage > 0)
                    player.WeeklyWage = Math.Max(250, player.WeeklyWage / 6);

            foreach (var club in career.World.Clubs.Values)
            {
                if (club == null || club.Finance == null)
                    continue;
                long balanceCorrection = 0;
                foreach (var transaction in club.Finance.Transactions)
                {
                    if (transaction == null || transaction.Category != "wages" || transaction.Amount >= 0)
                        continue;
                    var correctedAmount = transaction.Amount / 6;
                    balanceCorrection += correctedAmount - transaction.Amount;
                    transaction.Amount = correctedAmount;
                }
                club.Finance.Balance = (int)Math.Max(int.MinValue,
                    Math.Min(int.MaxValue, (long)club.Finance.Balance + balanceCorrection));
            }
        }

        static void UpgradePremierLeagueFinances(CareerState career)
        {
            if (career == null || career.World == null) return;
            foreach (var club in career.World.Clubs.Values)
            {
                if (club == null || club.LeagueId != "league:inglesa" || club.Finance == null) continue;
                var transferAllocation = 10000000 + club.Reputation * 450000;
                club.Finance.TransferBudget = System.Math.Max(club.Finance.TransferBudget, transferAllocation - club.Finance.TransferSpentThisSeason);
                club.Finance.Balance = System.Math.Max(club.Finance.Balance, transferAllocation + club.Reputation * 100000);
                club.Finance.WageBudget = System.Math.Max(club.Finance.WageBudget, 500000 + club.Reputation * 9000);
            }
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
            if (bits.Length > 19 && int.TryParse(bits[19], out var reputation)) player.Reputation = System.Math.Max(0, System.Math.Min(100, reputation));
            if (bits.Length > 20 && int.TryParse(bits[20], out var wage)) player.WeeklyWage = System.Math.Max(0, wage);
            if (bits.Length > 21 && long.TryParse(bits[21], out var contractEnd) && contractEnd > 0) player.ContractEndTicks = contractEnd;
            if (bits.Length > 22) player.TrainingFocus = Un(bits[22]);
            if (bits.Length > 23 && int.TryParse(bits[23], out var trainingProgress)) player.TrainingProgress = System.Math.Max(0, trainingProgress % 100);
            if (bits.Length > 24 && int.TryParse(bits[24], out var intensity)) player.TrainingIntensity = System.Math.Max(0, System.Math.Min(2, intensity));
            else player.TrainingIntensity = 1;
            if (bits.Length > 25 && int.TryParse(bits[25], out var streak)) player.TrainingStreak = System.Math.Max(0, streak);
            if (bits.Length > 26 && long.TryParse(bits[26], NumberStyles.Integer, CultureInfo.InvariantCulture, out var unavailableUntil)) player.UnavailableUntilTicks = unavailableUntil;
            if (bits.Length > 27) player.AbsenceKind = Un(bits[27]);
            if (bits.Length > 28) player.YellowBook = Un(bits[28]);
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
            PlayerTraits.Normalize(player);
        }

        static void ApplyPlayerDatabase(CareerState career, string[] bits)
        {
            if (bits.Length < 20 || !career.World.Players.TryGetValue(bits[1], out var player))
                return;
            player.FullName = Un(bits[2]);
            player.Nationality = Un(bits[3]);
            var portrait = Un(bits[4]).Replace("~", "|");
            player.PortraitPath = portrait;
            int.TryParse(bits[5], out var potential);
            if (player.Attributes == null)
                player.Attributes = new PlayerAttributes();
            var values = new int[14];
            for (var i = 0; i < values.Length; i++)
                if (!int.TryParse(bits[6 + i], out values[i]))
                    return;
            player.Attributes.Pace = Mathf.Clamp(values[0], 1, 20);
            player.Attributes.Stamina = Mathf.Clamp(values[1], 1, 20);
            player.Attributes.Strength = Mathf.Clamp(values[2], 1, 20);
            player.Attributes.Passing = Mathf.Clamp(values[3], 1, 20);
            player.Attributes.Finishing = Mathf.Clamp(values[4], 1, 20);
            player.Attributes.Dribbling = Mathf.Clamp(values[5], 1, 20);
            player.Attributes.Tackling = Mathf.Clamp(values[6], 1, 20);
            player.Attributes.Crossing = Mathf.Clamp(values[7], 1, 20);
            player.Attributes.Positioning = Mathf.Clamp(values[8], 1, 20);
            player.Attributes.Composure = Mathf.Clamp(values[9], 1, 20);
            player.Attributes.WorkRate = Mathf.Clamp(values[10], 1, 20);
            player.Attributes.Decisions = Mathf.Clamp(values[11], 1, 20);
            player.Attributes.Reflexes = Mathf.Clamp(values[12], 1, 20);
            player.Attributes.Handling = Mathf.Clamp(values[13], 1, 20);
            player.Potential = Mathf.Clamp(potential, PlayerAttributes.Overall(player), 99);
            FaceBook.RefreshPortrait(player);
        }

        static void ApplyClubDatabase(CareerState career, string[] bits)
        {
            if (bits.Length < 10 || !career.World.Clubs.TryGetValue(bits[1], out var club))
                return;
            club.Name = Un(bits[2]);
            club.ShortName = Un(bits[3]);
            club.CrestResourcePath = Un(bits[4]).Replace("~", "|");
            club.CustomCrestKey = Un(bits[5]);
            club.JerseyPrimaryHex = Un(bits[6]);
            club.JerseySecondaryHex = Un(bits[7]);
            club.JerseyShortsHex = Un(bits[8]);
            if (int.TryParse(bits[9], out var reputation))
                club.Reputation = Mathf.Clamp(reputation, 1, 100);
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
