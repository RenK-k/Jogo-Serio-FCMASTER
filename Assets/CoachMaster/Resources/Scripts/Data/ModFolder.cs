using System;
using System.Collections.Generic;
using System.IO;
using CoachMaster.Career;
using CoachMaster.Domain;
using UnityEngine;

namespace CoachMaster.Data
{
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
            WorldCatalog.NormalizeGeneratedStaff(career.World);
            ApplyClubs(career.World);
            ApplyPlayers(career.World);
        }

        public static Sprite Portrait(string fullName)
        {
            return Picture("caras", fullName);
        }

        public static Sprite Crest(string clubName)
        {
            return Picture("emblemas", clubName);
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
            }
        }

        static void ApplyPlayers(WorldState world)
        {
            foreach (var line in ReadLines("jogadores.txt"))
            {
                var bits = line.Split('|');
                if (bits.Length < 2 || string.IsNullOrWhiteSpace(bits[0]))
                    continue;
                foreach (var player in world.Players.Values)
                {
                    if (player == null || !string.Equals(player.FullName, bits[0].Trim(), StringComparison.OrdinalIgnoreCase))
                        continue;
                    PlayerPosition position;
                    if (PositionOf(bits[1], out position))
                        player.Position = position;
                    if (bits.Length > 2 && int.TryParse(bits[2], out var age))
                        player.Age = Mathf.Clamp(age, 15, 50);
                    if (bits.Length > 3 && !string.IsNullOrWhiteSpace(bits[3]))
                        player.Nationality = bits[3].Trim();
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
                }
            }
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
                    || string.Equals(club.ShortName, wanted, StringComparison.OrdinalIgnoreCase))
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
            var path = File.Exists(png) ? png : File.Exists(jpg) ? jpg : "";
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
                sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
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
