using System.Collections.Generic;
using CoachMaster.Domain;
using UnityEngine;

namespace CoachMaster.UI
{
    public static class FaceBook
    {
        static readonly Dictionary<string, Sprite[]> Pools = new Dictionary<string, Sprite[]>();
        static readonly Dictionary<string, Sprite[]> LoadedFolders = new Dictionary<string, Sprite[]>();
        static readonly Dictionary<string, Sprite> ByPlayer = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Sprite> ByStaff = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Sprite> SocialFaces = new Dictionary<string, Sprite>();
        static readonly HashSet<string> UsedSocialFaces = new HashSet<string>();
        static readonly Dictionary<Sprite, string> FolderBySprite = new Dictionary<Sprite, string>();
        static readonly HashSet<string> UsedPortraitIds = new HashSet<string>();
        static readonly Dictionary<string, HashSet<string>> UsedStaffByClub = new Dictionary<string, HashSet<string>>();
        static Sprite[] _all;
        static Sprite[] _allAssignedPool;
        static Sprite[] _staffFaces;
        static Sprite _defaultPortrait;

        static readonly string[] Nations =
        {
            "Argentina", "Brasil", "Japão", "Coreia do Sul", "Turquia", "Itália", "Portugal",
            "Alemanha", "Espanha", "Bélgica", "França", "Inglaterra", "Países Baixos",
            "Senegal", "RD Congo", "Gana", "Marrocos", "Nigéria",
            "Escócia", "Irlanda", "Dinamarca", "Suécia", "Áustria", "Polónia", "Croácia",
            "Sérvia", "República Checa", "Hungria", "Suíça", "Roménia",
            "Uruguai", "Colômbia", "Chile", "Paraguai", "Equador", "Peru", "Venezuela", "Bolívia"
        };

        static readonly string[] SouthAmericanNations =
        {
            "Argentina", "Brasil", "Uruguai", "Colômbia", "Chile", "Paraguai", "Equador", "Peru", "Venezuela", "Bolívia"
        };

        static readonly string[] AfricanNations = { "Senegal", "RD Congo", "Gana", "Nigéria" };
        static readonly string[] AsianNations = { "Japão", "Coreia do Sul" };
        static readonly string[] NorthAfricanNations = { "Marrocos" };
        static readonly string[] BritishIrishNations = { "Inglaterra", "Escócia", "Irlanda" };
        static readonly string[] ScandinavianNations = { "Dinamarca", "Suécia" };
        static readonly string[] SpanMedNations = { "Portugal", "Espanha", "Itália" };
        static readonly string[] CentralEuropeanNations =
        {
            "Alemanha", "Áustria", "Polónia", "Croácia", "Sérvia", "República Checa", "Hungria", "Suíça", "Roménia"
        };
        static readonly string[] EuropeanNations =
        {
            "Portugal", "Alemanha", "Espanha", "Bélgica", "França", "Inglaterra", "Países Baixos",
            "Escócia", "Irlanda", "Dinamarca", "Suécia", "Áustria", "Polónia", "Croácia", "Sérvia",
            "República Checa", "Hungria", "Suíça", "Roménia", "Itália"
        };

        // Keep every imported folder addressable so broad regional packs can serve as a safe last resort.
        static readonly string[] FaceFolders =
        {
            "Kits/Players/Mixed Race", "Kits/Players/North Americans", "Kits/Players/Οceanians",
            "Kits/Players/Africa/Africans", "Kits/Players/Africa/DR Congo", "Kits/Players/Africa/Ghana",
            "Kits/Players/Africa/Morocco", "Kits/Players/Africa/Nigeria", "Kits/Players/Africa/North Africans/Arabs",
            "Kits/Players/Africa/North Africans", "Kits/Players/Africa/Senegal",
            "Kits/Players/Asia/Asians", "Kits/Players/Asia/Japan", "Kits/Players/Asia/South Korea",
            "Kits/Players/Europe/Belgium", "Kits/Players/Europe/British_Irish", "Kits/Players/Europe/Caucasian 1",
            "Kits/Players/Europe/Central European", "Kits/Players/Europe/England", "Kits/Players/Europe/Europeans",
            "Kits/Players/Europe/France", "Kits/Players/Europe/Germany", "Kits/Players/Europe/Italy",
            "Kits/Players/Europe/Netherlands", "Kits/Players/Europe/Portugal", "Kits/Players/Europe/Scandinavians",
            "Kits/Players/Europe/Spain", "Kits/Players/Europe/SpanMed", "Kits/Players/Middle East/Turkey",
            "Kits/Players/South American/Argentina", "Kits/Players/South American/Brazil",
            "Kits/Players/South American/South American/South Americans"
        };

        static readonly string[] StaffFolders =
        {
            "Kits/Staff", "Kits/Staff/Staff 1", "Kits/Staff/Staff 2", "Kits/Staff/Staff 3"
        };

        static readonly string[] NewgenFaceCategories =
        {
            "Africans", "Asians", "British_Irish", "Europeans", "Mixed Race", "North Africans",
            "North Americans", "Scandinavians", "South Americans", "Oceanians", "Οceanians"
        };

        public static string Nation(int seed)
        {
            var index = seed % Nations.Length;
            if (index < 0)
                index += Nations.Length;
            return Nations[index];
        }

        public static string NationForClub(string slug, int seed)
        {
            return NationForClub(slug, seed, 60, "");
        }

        public static string NationForClub(string slug, int seed, int reputation, string leagueId)
        {
            var home = "Portugal";
            var competition = (leagueId ?? "") + " " + (slug ?? "");
            if (competition.Contains("inglesa"))
                home = "Inglaterra";
            else if (competition.Contains("francesa"))
                home = "França";
            else if (competition.Contains("espanhola"))
                home = "Espanha";
            else if (competition.Contains("bundesliga"))
                home = "Alemanha";
            else if (competition.Contains("eredivisie"))
                home = "Países Baixos";
            else if (competition.Contains("brasileira"))
                home = "Brasil";
            else if (!string.IsNullOrEmpty(slug))
            {
                if (slug.StartsWith("inglesa"))
                    home = "Inglaterra";
                else if (slug.StartsWith("francesa"))
                    home = "França";
                else if (slug.StartsWith("espanhola"))
                    home = "Espanha";
            }

            var roll = Mod(seed, 100);
            var lowerDivision = competition.Contains("-2") || competition.Contains("horizonte") ||
                                competition.Contains("championship") || competition.Contains("league-one");
            var domesticChance = lowerDivision ? 90 : reputation >= 80 ? 54 : reputation >= 75 ? 61 : reputation >= 68 ? 70 : 81;
            if (roll < domesticChance)
                return home;
            var foreignRoll = roll - domesticChance;
            var foreignRange = 100 - domesticChance;
            if (foreignRoll < foreignRange * 0.72f)
                return Neighbour(home, seed / 3);
            if (foreignRoll < foreignRange * 0.96f)
                return Wider(seed / 5);
            return Rare(seed / 7);
        }

        static string Neighbour(string home, int seed)
        {
            string[] pool;
            if (home == "Inglaterra")
                pool = new[] { "França", "Bélgica", "Países Baixos", "Alemanha", "Escócia", "Irlanda" };
            else if (home == "França")
                pool = new[] { "Bélgica", "Senegal", "Marrocos", "Espanha", "Suíça", "Brasil" };
            else if (home == "Espanha")
                pool = new[] { "Argentina", "Brasil", "Uruguai", "Colômbia", "Marrocos", "Portugal" };
            else
                pool = new[] { "Brasil", "Espanha", "França", "Argentina", "Uruguai", "Colômbia" };
            return pool[Mod(seed, pool.Length)];
        }

        static string Wider(int seed)
        {
            var pool = new[] { "Brasil", "Argentina", "Uruguai", "Colômbia", "Chile", "Paraguai", "Equador", "Peru", "Venezuela", "Bolívia", "Nigéria", "Gana", "Senegal", "Itália", "Alemanha", "Polónia", "Croácia", "Áustria", "Dinamarca", "Suécia" };
            return pool[Mod(seed, pool.Length)];
        }

        static string Rare(int seed)
        {
            var pool = new[] { "Coreia do Sul", "Japão", "Turquia" };
            return pool[Mod(seed, pool.Length)];
        }

        public static string PersonName(string nationality, int seed)
        {
            string[] first;
            string[] last;
            Names(nationality, out first, out last);
            var given = first[Mod(seed, first.Length)];
            var family = last[Mod(seed / 17, last.Length)];
            return given + " " + family;
        }

        static int Mod(int value, int length)
        {
            var index = value % length;
            return index < 0 ? index + length : index;
        }

        public static void Deal(WorldState world)
        {
            if (world == null)
                return;

            Pools.Clear();
            ByPlayer.Clear();
            ByStaff.Clear();
            SocialFaces.Clear();
            UsedSocialFaces.Clear();
            UsedPortraitIds.Clear();
            UsedStaffByClub.Clear();
            _all = null;
            _allAssignedPool = null;
            _staffFaces = null;

            var players = new List<Player>(world.Players.Values);
            players.Sort((a, b) =>
            {
                var age = a.Age.CompareTo(b.Age);
                return age != 0 ? age : string.CompareOrdinal(a.Id, b.Id);
            });

            var used = new HashSet<string>();
            foreach (var player in players)
            {
                if (string.IsNullOrEmpty(player.PortraitPath))
                    continue;

                var sprite = Find(player.Nationality, player.PortraitPath);
                if (sprite == null)
                {
                    player.PortraitPath = null;
                    ByPlayer.Remove(player.Id);
                    continue;
                }

                var spriteId = SpriteId(sprite);
                if (string.IsNullOrEmpty(spriteId) || !used.Add(spriteId))
                {
                    player.PortraitPath = null;
                    ByPlayer.Remove(player.Id);
                    continue;
                }

                player.PortraitPath = spriteId;
                ByPlayer[player.Id] = Center(sprite);
                UsedPortraitIds.Add(spriteId);
            }

            var groups = new Dictionary<string, List<Player>>();
            foreach (var player in players)
            {
                if (!string.IsNullOrEmpty(player.PortraitPath))
                    continue;
                var nation = string.IsNullOrEmpty(player.Nationality) ? "Portugal" : player.Nationality;
                if (!groups.TryGetValue(nation, out var list))
                {
                    list = new List<Player>();
                    groups[nation] = list;
                }

                list.Add(player);
            }

            var nations = new List<string>(groups.Keys);
            nations.Sort((a, b) => string.CompareOrdinal(a, b));
            var fallbackPool = AnyPool();
            var fallbackCursor = 0;
            foreach (var nation in nations)
            {
                var group = groups[nation];
                group.Sort((a, b) =>
                {
                    var age = a.Age.CompareTo(b.Age);
                    return age != 0 ? age : string.CompareOrdinal(a.Id, b.Id);
                });

                var preferredPool = new List<Sprite>(Pool(nation));
                preferredPool.AddRange(Spare(nation));
                preferredPool.Sort((a, b) =>
                {
                    var ageOrder = Youth(a).CompareTo(Youth(b));
                    return ageOrder != 0 ? ageOrder : string.CompareOrdinal(SpriteId(a), SpriteId(b));
                });
                var preferredCursor = 0;
                foreach (var player in group)
                {
                    var sprite = TakeFresh(preferredPool, used, ref preferredCursor);
                    if (sprite == null)
                        sprite = TakeFresh(fallbackPool, used, ref fallbackCursor);
                    if (sprite == null)
                        continue;
                    var spriteId = SpriteId(sprite);
                    if (string.IsNullOrEmpty(spriteId) || !used.Add(spriteId))
                        continue;
                    player.PortraitPath = spriteId;
                    ByPlayer[player.Id] = Center(sprite);
                    UsedPortraitIds.Add(spriteId);
                }
            }

            DealStaff(world);
        }

        public static int UniqueFaceCount()
        {
            var seen = new HashSet<string>();
            foreach (var sprite in AllFaces())
            {
                if (sprite == null)
                    continue;
                var id = SpriteId(sprite);
                if (!string.IsNullOrEmpty(id))
                    seen.Add(id);
            }

            return seen.Count;
        }

        public static int FaceCountForNation(string nation)
        {
            var seen = new HashSet<string>();
            foreach (var sprite in Pool(nation))
            {
                if (sprite == null)
                    continue;
                var id = SpriteId(sprite);
                if (!string.IsNullOrEmpty(id))
                    seen.Add(id);
            }

            return seen.Count;
        }

        public static Sprite SocialAvatar(string identity)
        {
            if (string.IsNullOrEmpty(identity))
                identity = "fan";
            if (SocialFaces.TryGetValue(identity, out var cached))
                return cached;

            var pool = new List<Sprite>(AnyPool());
            if (pool.Count == 0)
                pool.AddRange(AllFaces());
            pool.Sort((a, b) => string.CompareOrdinal(SpriteId(a), SpriteId(b)));
            if (pool.Count == 0)
                return null;

            var start = StableIndex(identity, pool.Count);
            Sprite chosen = null;
            for (var pass = 0; pass < 2 && chosen == null; pass++)
            {
                for (var i = 0; i < pool.Count; i++)
                {
                    var candidate = pool[(start + i) % pool.Count];
                    if (candidate == null)
                        continue;
                    var id = SpriteId(candidate);
                    if (UsedSocialFaces.Contains(id) || (pass == 0 && UsedPortraitIds.Contains(id)))
                        continue;
                    chosen = candidate;
                    UsedSocialFaces.Add(id);
                    break;
                }
            }

            if (chosen == null)
                chosen = pool[start];
            chosen = Center(chosen);
            SocialFaces[identity] = chosen;
            return chosen;
        }

        static int StableIndex(string value, int count)
        {
            unchecked
            {
                var hash = 17;
                for (var i = 0; i < value.Length; i++)
                    hash = hash * 31 + value[i];
                return Mod(hash, count);
            }
        }

        public static Sprite For(Player player)
        {
            if (player == null)
                return null;
            if (ByPlayer.TryGetValue(player.Id, out var cached) && cached != null)
                return cached;

            var sprite = Find(player.Nationality, player.PortraitPath);
            var spriteId = SpriteId(sprite);
            if (sprite == null || UsedPortraitIds.Contains(spriteId))
            {
                sprite = TakeUnusedFallback();
                spriteId = SpriteId(sprite);
                if (sprite != null)
                {
                    player.PortraitPath = spriteId;
                    UsedPortraitIds.Add(spriteId);
                }
            }
            else
                UsedPortraitIds.Add(spriteId);

            if (sprite == null)
                sprite = DefaultPortrait();
            sprite = Center(sprite);
            if (sprite != null)
                ByPlayer[player.Id] = sprite;
            return sprite;
        }

        public static Sprite For(StaffMember member)
        {
            if (member == null)
                return DefaultPortrait();

            var key = StaffKey(member);
            if (ByStaff.TryGetValue(key, out var cached) && cached != null)
                return cached;

            if (!UsedStaffByClub.TryGetValue(member.ClubId ?? "", out var used))
            {
                used = new HashSet<string>();
                UsedStaffByClub[member.ClubId ?? ""] = used;
            }

            var sprite = FindStaff(member.PortraitPath);
            var spriteId = SpriteId(sprite);
            if (sprite == null || used.Contains(spriteId))
                sprite = TakeStaff(member.ClubId, key, used);
            if (sprite == null)
            {
                member.PortraitPath = null;
                sprite = DefaultPortrait();
            }
            else
            {
                spriteId = SpriteId(sprite);
                member.PortraitPath = spriteId;
                used.Add(spriteId);
                sprite = Center(sprite);
            }

            if (sprite != null)
                ByStaff[key] = sprite;
            return sprite;
        }

        static void DealStaff(WorldState world)
        {
            var clubs = new List<Club>(world.Clubs.Values);
            clubs.Sort((a, b) => string.CompareOrdinal(a == null ? "" : a.Id, b == null ? "" : b.Id));
            for (var c = 0; c < clubs.Count; c++)
            {
                var club = clubs[c];
                if (club == null || club.Staff == null)
                    continue;

                var used = new HashSet<string>();
                UsedStaffByClub[club.Id ?? ""] = used;
                var members = new List<StaffMember>(club.Staff);
                members.Sort((a, b) =>
                {
                    var role = (a == null ? 0 : (int)a.Role).CompareTo(b == null ? 0 : (int)b.Role);
                    return role != 0 ? role : string.CompareOrdinal(StaffKey(a), StaffKey(b));
                });

                for (var i = 0; i < members.Count; i++)
                {
                    var member = members[i];
                    if (member == null)
                        continue;
                    var key = StaffKey(member);
                    var sprite = FindStaff(member.PortraitPath);
                    var id = SpriteId(sprite);
                    if (sprite == null || used.Contains(id))
                        sprite = TakeStaff(club.Id, key, used);

                    if (sprite == null)
                    {
                        member.PortraitPath = null;
                        ByStaff[key] = DefaultPortrait();
                        continue;
                    }

                    id = SpriteId(sprite);
                    member.PortraitPath = id;
                    used.Add(id);
                    ByStaff[key] = Center(sprite);
                }
            }
        }

        static string StaffKey(StaffMember member)
        {
            if (member == null)
                return "staff";
            if (!string.IsNullOrEmpty(member.Id))
                return member.Id;
            return (member.ClubId ?? "") + "|" + member.Role + "|" + (member.FullName ?? "");
        }

        static Sprite TakeStaff(string clubId, string staffKey, HashSet<string> used)
        {
            var pool = AllStaffFaces();
            if (pool.Length == 0)
                return null;
            var start = StableIndex((clubId ?? "") + "|" + staffKey, pool.Length);
            for (var i = 0; i < pool.Length; i++)
            {
                var candidate = pool[(start + i) % pool.Length];
                if (candidate != null && used.Add(SpriteId(candidate)))
                    return candidate;
            }
            return null;
        }

        static Sprite[] AllStaffFaces()
        {
            if (_staffFaces != null)
                return _staffFaces;

            var all = new List<Sprite>();
            var seen = new HashSet<string>();
            for (var folderIndex = 0; folderIndex < StaffFolders.Length; folderIndex++)
            {
                foreach (var sprite in Load(StaffFolders[folderIndex]))
                {
                    var id = SpriteId(sprite);
                    if (sprite != null && !string.IsNullOrEmpty(id) && seen.Add(id))
                        all.Add(sprite);
                }
            }

            _staffFaces = all.ToArray();
            System.Array.Sort(_staffFaces, (a, b) => string.CompareOrdinal(SpriteId(a), SpriteId(b)));
            return _staffFaces;
        }

        static Sprite FindStaff(string portraitPath)
        {
            if (string.IsNullOrEmpty(portraitPath))
                return null;
            var mark = portraitPath.IndexOf('|');
            var name = mark < 0 ? portraitPath : portraitPath.Substring(mark + 1);
            if (mark > 0)
            {
                var folder = portraitPath.Substring(0, mark);
                if (folder.StartsWith("Kits/Staff"))
                {
                    foreach (var sprite in Load(folder))
                    {
                        if (sprite != null && sprite.name == name)
                            return sprite;
                    }
                }
            }

            foreach (var sprite in AllStaffFaces())
            {
                if (sprite != null && sprite.name == name)
                    return sprite;
            }
            return null;
        }

        static Sprite DefaultPortrait()
        {
            if (_defaultPortrait != null)
                return _defaultPortrait;
            var texture = Resources.Load<Texture2D>("PlayerFaces/default-silhouette");
            if (texture == null)
                return null;
            _defaultPortrait = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            _defaultPortrait.name = "default-silhouette";
            return _defaultPortrait;
        }

        static Sprite TakeUnusedFallback()
        {
            var pool = new List<Sprite>(AnyPool());
            pool.Sort((a, b) => Youth(a).CompareTo(Youth(b)));
            for (var i = 0; i < pool.Count; i++)
            {
                var sprite = pool[i];
                var id = SpriteId(sprite);
                if (sprite != null && !string.IsNullOrEmpty(id) && !UsedPortraitIds.Contains(id))
                    return sprite;
            }

            return null;
        }

        static Sprite[] AllFaces()
        {
            if (_all != null)
                return _all;

            foreach (var folder in AllFaceFolders())
                Load(folder);

            var loaded = Resources.LoadAll<Sprite>("Kits/Players");
            if (loaded == null)
            {
                _all = new Sprite[0];
                return _all;
            }

            var faces = new List<Sprite>();
            for (var i = 0; i < loaded.Length; i++)
            {
                var folder = loaded[i] != null && FolderBySprite.TryGetValue(loaded[i], out var sourceFolder) ? sourceFolder : "";
                if (IsFace(loaded[i]) && !IsDuplicateFace(folder, loaded[i]))
                    faces.Add(loaded[i]);
            }
            _all = faces.ToArray();
            return _all;
        }

        static Sprite Center(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
                return sprite;
            var tex = sprite.texture;
            var full = new Rect(0f, 0f, tex.width, tex.height);
            var rect = sprite.rect;
            var pivot = sprite.pivot;
            var covers = Mathf.Abs(rect.x) < 1f && Mathf.Abs(rect.y) < 1f && Mathf.Abs(rect.width - full.width) < 2f && Mathf.Abs(rect.height - full.height) < 2f;
            var centered = Mathf.Abs(pivot.x - rect.width * 0.5f) < 2f && Mathf.Abs(pivot.y - rect.height * 0.5f) < 2f;
            if (covers && centered)
                return sprite;
            try
            {
                var made = Sprite.Create(tex, full, new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                made.name = sprite.name;
                return made;
            }
            catch
            {
                return sprite;
            }
        }

        static Sprite[] AnyPool()
        {
            if (_allAssignedPool != null)
                return _allAssignedPool;

            var all = new List<Sprite>();
            var seen = new HashSet<string>();
            foreach (var sprite in AllFaces())
            {
                if (sprite != null && seen.Add(SpriteId(sprite)))
                    all.Add(sprite);
            }

            all.Sort((a, b) =>
            {
                var ageOrder = Youth(a).CompareTo(Youth(b));
                return ageOrder != 0 ? ageOrder : string.CompareOrdinal(SpriteId(a), SpriteId(b));
            });
            _allAssignedPool = all.ToArray();
            return _allAssignedPool;
        }

        static string[] AllFaceFolders()
        {
            var folders = new List<string>(FaceFolders);
            for (var version = 2; version <= 7; version++)
            {
                var name = "Newgen Revolution V" + version;
                var root = "Kits/Players/" + name;
                if (version >= 3)
                    root += "/" + name;
                for (var category = 0; category < NewgenFaceCategories.Length; category++)
                    folders.Add(root + "/" + NewgenFaceCategories[category]);
                folders.Add(root + "/North Africans/Arabic");
                folders.Add(root + "/North Africans/Arabs");
            }
            return folders.ToArray();
        }

        static Sprite TakeFresh(IList<Sprite> pool, HashSet<string> used, ref int cursor)
        {
            if (pool == null)
                return null;
            while (cursor < pool.Count)
            {
                var sprite = pool[cursor++];
                if (sprite == null || used.Contains(SpriteId(sprite)))
                    continue;
                return sprite;
            }

            return null;
        }

        static List<Sprite> Spare(string nation)
        {
            var spare = new List<Sprite>();
            var region = RegionOf(nation);
            foreach (var other in Nations)
            {
                if (other == nation || RegionOf(other) != region)
                    continue;
                spare.AddRange(Pool(other));
            }

            spare.Sort((a, b) => Youth(a).CompareTo(Youth(b)));
            return spare;
        }

        static int Youth(Sprite sprite)
        {
            if (sprite == null)
                return 99999;
            var name = sprite.name ?? "";
            var lower = name.ToLowerInvariant();
            var isNewgenPack = FolderBySprite.TryGetValue(sprite, out var folder) && folder.Contains("Newgen Revolution");
            var regen = lower.Contains("u18") || lower.Contains("u21") || lower.Contains("young") || lower.Contains("junior") || lower.StartsWith("kt1") || isNewgenPack ? 0 : 1;
            var id = 0;
            var digits = 0;
            for (var i = name.Length - 1; i >= 0 && name[i] >= '0' && name[i] <= '9' && digits < 5; i--)
            {
                id += (name[i] - '0') * Pow10(digits);
                digits++;
            }

            return regen * 100000 + id;
        }

        static int Pow10(int digits)
        {
            var value = 1;
            for (var i = 0; i < digits; i++)
                value *= 10;
            return value;
        }

        static Sprite Find(string nation, string portraitPath)
        {
            if (string.IsNullOrEmpty(portraitPath))
                return null;
            var mark = portraitPath.IndexOf('|');
            var name = mark < 0 ? portraitPath : portraitPath.Substring(mark + 1);
            if (mark > 0)
            {
                var folder = portraitPath.Substring(0, mark);
                if (folder.StartsWith("Kits/Players/"))
                {
                    foreach (var sprite in Load(folder))
                    {
                        if (sprite != null && sprite.name == name)
                            return sprite;
                    }
                }
            }

            foreach (var sprite in Pool(nation))
            {
                if (sprite != null && sprite.name == name)
                    return sprite;
            }

            foreach (var sprite in AllFaces())
            {
                if (sprite != null && sprite.name == name)
                    return sprite;
            }

            return null;
        }

        static Sprite[] Pool(string nation)
        {
            if (string.IsNullOrEmpty(nation))
                nation = "Portugal";
            if (Pools.TryGetValue(nation, out var cached))
                return cached;

            var list = new List<Sprite>();
            foreach (var folder in Folders(nation))
            {
                foreach (var sprite in Load(folder))
                {
                    if (sprite != null)
                        list.Add(sprite);
                }

                if (list.Count > 0)
                    break;
            }

            AddShare(list, nation);
            cached = list.ToArray();
            Pools[nation] = cached;
            return cached;
        }

        static Sprite[] Load(string folder)
        {
            if (LoadedFolders.TryGetValue(folder, out var cached))
                return cached;

            var loaded = Resources.LoadAll<Sprite>(folder);
            if (loaded == null)
            {
                cached = new Sprite[0];
                LoadedFolders[folder] = cached;
                return cached;
            }
            var faces = new List<Sprite>();
            for (var i = 0; i < loaded.Length; i++)
            {
                if (!IsFace(loaded[i]))
                    continue;
                if (!FolderBySprite.ContainsKey(loaded[i]))
                    FolderBySprite[loaded[i]] = folder;
                if (IsDuplicateFace(folder, loaded[i]))
                    continue;
                faces.Add(loaded[i]);
            }

            cached = faces.ToArray();
            LoadedFolders[folder] = cached;
            return cached;
        }

        static bool IsFace(Sprite sprite)
        {
            return sprite != null && !(sprite.name ?? "").ToLowerInvariant().Contains("countries");
        }

        static bool IsDuplicateFace(string folder, Sprite sprite)
        {
            if (sprite == null)
                return false;
            var name = (sprite.name ?? "").ToLowerInvariant();
            if (name.Contains("sumlugzpccvaxydlfljasg"))
                return folder != "Kits/Players/Africa/North Africans/Arabs";
            if (name.Contains("wmain-qimg-a356ef74a0a2f8b698ddf5b0d6a4197d-c"))
                return folder != "Kits/Players/Newgen Revolution V2/British_Irish";
            return false;
        }

        static string SpriteId(Sprite sprite)
        {
            if (sprite == null)
                return "";
            return FolderBySprite.TryGetValue(sprite, out var folder)
                ? folder + "|" + sprite.name
                : sprite.name;
        }

        static void AddShare(List<Sprite> list, string nation)
        {
            Share(list, nation, "Kits/Players/South American/South American/South Americans", SouthAmericanNations);
            Share(list, nation, "Kits/Players/Africa/Africans", AfricanNations);
            Share(list, nation, "Kits/Players/Africa/North Africans", NorthAfricanNations);
            Share(list, nation, "Kits/Players/Africa/North Africans/Arabs", NorthAfricanNations);
            Share(list, nation, "Kits/Players/Asia/Asians", AsianNations);
            Share(list, nation, "Kits/Players/Europe/British_Irish", BritishIrishNations);
            Share(list, nation, "Kits/Players/Europe/Scandinavians", ScandinavianNations);
            Share(list, nation, "Kits/Players/Europe/SpanMed", SpanMedNations);
            Share(list, nation, "Kits/Players/Europe/Central European", CentralEuropeanNations);
            Share(list, nation, "Kits/Players/Europe/Europeans", EuropeanNations);
            Share(list, nation, "Kits/Players/Europe/Caucasian 1", EuropeanNations);

            ShareGenerations(list, nation, "South Americans", SouthAmericanNations);
            ShareGenerations(list, nation, "Africans", AfricanNations);
            ShareGenerations(list, nation, "North Africans", NorthAfricanNations);
            ShareGenerations(list, nation, "Asians", AsianNations);
            ShareGenerations(list, nation, "British_Irish", BritishIrishNations);
            ShareGenerations(list, nation, "Scandinavians", ScandinavianNations);
            ShareGenerations(list, nation, "Europeans", EuropeanNations);
        }

        static void ShareGenerations(List<Sprite> list, string nation, string category, string[] recipients)
        {
            for (var version = 2; version <= 7; version++)
            {
                var name = "Newgen Revolution V" + version;
                var root = "Kits/Players/" + name;
                if (version >= 3)
                    root += "/" + name;
                Share(list, nation, root + "/" + category, recipients);
                if (category == "North Africans")
                {
                    Share(list, nation, root + "/North Africans/Arabic", recipients);
                    Share(list, nation, root + "/North Africans/Arabs", recipients);
                }
            }
        }

        static void Share(List<Sprite> list, string nation, string folder, string[] recipients)
        {
            for (var i = 0; i < recipients.Length; i++)
            {
                if (recipients[i] == nation)
                {
                    Take(list, folder, i, recipients.Length);
                    return;
                }
            }
        }

        static void Take(List<Sprite> dest, string folder, int bucket, int buckets)
        {
            var all = new List<Sprite>(Load(folder));
            all.Sort((a, b) => string.CompareOrdinal(a == null ? "" : a.name, b == null ? "" : b.name));
            var alreadyAdded = new HashSet<string>();
            for (var i = 0; i < dest.Count; i++)
            {
                if (dest[i] != null)
                    alreadyAdded.Add(SpriteId(dest[i]));
            }
            for (var i = 0; i < all.Count; i++)
            {
                if (all[i] != null && i % buckets == bucket && alreadyAdded.Add(SpriteId(all[i])))
                    dest.Add(all[i]);
            }
        }

        static string RegionOf(string nation)
        {
            switch (nation)
            {
                case "Argentina":
                case "Brasil":
                case "Uruguai":
                case "Colômbia":
                case "Chile":
                case "Paraguai":
                case "Equador":
                case "Peru":
                case "Venezuela":
                case "Bolívia":
                    return "South American";
                case "Japão":
                case "Coreia do Sul":
                    return "Asia";
                case "Turquia":
                    return "Middle East";
                case "Senegal":
                case "RD Congo":
                case "Gana":
                case "Marrocos":
                case "Nigéria":
                    return "Africa";
                default:
                    return "Europe";
            }
        }

        static string[] Folders(string nation)
        {
            var folderNation = FolderNation(nation);
            var region = Region(folderNation);
            return new[]
            {
                "Kits/Players/" + region + "/" + folderNation,
                "Kits/Players/" + region + "/" + Compact(folderNation),
                "Kits/Players/" + Compact(region) + "/" + Compact(folderNation)
            };
        }

        static string FolderNation(string nation)
        {
            switch (nation)
            {
                case "Brasil": return "Brazil";
                case "Japão": return "Japan";
                case "Coreia do Sul": return "South Korea";
                case "Turquia": return "Turkey";
                case "Itália": return "Italy";
                case "Alemanha": return "Germany";
                case "Espanha": return "Spain";
                case "Bélgica": return "Belgium";
                case "França": return "France";
                case "Inglaterra": return "England";
                case "Países Baixos": return "Netherlands";
                case "RD Congo": return "DR Congo";
                case "Gana": return "Ghana";
                case "Marrocos": return "Morocco";
                case "Nigéria": return "Nigeria";
                default: return string.IsNullOrEmpty(nation) ? "Portugal" : nation;
            }
        }

        static string Region(string nation)
        {
            switch (nation)
            {
                case "Argentina":
                case "Brazil":
                case "Uruguai":
                case "Colômbia":
                case "Chile":
                case "Paraguai":
                case "Equador":
                case "Peru":
                case "Venezuela":
                case "Bolívia":
                    return "South American";
                case "Japan":
                case "South Korea":
                    return "Asia";
                case "Turkey":
                    return "Middle East";
                case "Senegal":
                case "DR Congo":
                case "Ghana":
                case "Morocco":
                case "Nigeria":
                    return "Africa";
                default:
                    return "Europe";
            }
        }

        static string Compact(string value)
        {
            return string.IsNullOrEmpty(value) ? "" : value.Replace(" ", "").ToLowerInvariant();
        }

        static string Key(string nation, Sprite sprite)
        {
            return nation + "|" + sprite.name;
        }

        static void Shuffle(List<Sprite> pool, string nation)
        {
            var rng = Hash(nation);
            for (var i = pool.Count - 1; i > 0; i--)
            {
                rng = rng * 1103515245 + 12345;
                var j = (int)((rng & 0x7fffffff) % (i + 1));
                var swap = pool[i];
                pool[i] = pool[j];
                pool[j] = swap;
            }
        }

        static int Hash(string value)
        {
            unchecked
            {
                var hash = 23;
                foreach (var c in value)
                    hash = hash * 31 + c;
                return hash & 0x7fffffff;
            }
        }

        static void Names(string nationality, out string[] first, out string[] last)
        {
            switch (FolderNation(nationality))
            {
                case "Brazil":
                    first = new[] { "Lucas", "Gabriel", "Matheus", "Rafael", "Gustavo", "Felipe", "Thiago", "Leonardo", "Vinícius", "Caio", "Henrique", "Eduardo", "Marcelo", "Danilo", "Rodrigo" };
                    last = new[] { "Silva", "Santos", "Oliveira", "Souza", "Lima", "Pereira", "Costa", "Ferreira", "Almeida", "Nascimento", "Araújo", "Barbosa", "Ribeiro", "Carvalho", "Gomes" };
                    return;
                case "Argentina":
                    first = new[] { "Mateo", "Santiago", "Lautaro", "Facundo", "Agustín", "Nicolás", "Joaquín", "Franco", "Valentín", "Ignacio", "Martín", "Gonzalo", "Leandro", "Ezequiel", "Emiliano" };
                    last = new[] { "González", "Rodríguez", "Gómez", "Fernández", "López", "Martínez", "Díaz", "Pérez", "García", "Romero", "Sosa", "Álvarez", "Torres", "Ruiz", "Herrera" };
                    return;
                case "Uruguai":
                case "Colômbia":
                case "Chile":
                case "Paraguai":
                case "Equador":
                case "Peru":
                case "Venezuela":
                case "Bolívia":
                    first = new[] { "Mateo", "Santiago", "Thiago", "Tomás", "Nicolás", "Julián", "Lucas", "Facundo", "Diego", "Martín", "Benjamín", "Emiliano", "Joaquín", "Gabriel", "Agustín" };
                    last = new[] { "González", "Rodríguez", "Gómez", "Fernández", "López", "Martínez", "Díaz", "Pérez", "García", "Romero", "Torres", "Ruiz", "Herrera", "Castro", "Rojas" };
                    return;
                case "Japan":
                    first = new[] { "Haruto", "Sota", "Yuto", "Haruki", "Riku", "Minato", "Kaito", "Ren", "Yuma", "Asahi", "Hayato", "Takumi", "Daiki", "Kenta", "Shota" };
                    last = new[] { "Sato", "Suzuki", "Takahashi", "Tanaka", "Watanabe", "Ito", "Yamamoto", "Nakamura", "Kobayashi", "Kato", "Yoshida", "Yamada", "Sasaki", "Saito", "Kimura" };
                    return;
                case "South Korea":
                    first = new[] { "Min-jun", "Seo-jun", "Do-yun", "Ha-jun", "Ji-ho", "Si-woo", "Jun-seo", "Hyun-woo", "Sung-min", "Jae-won", "Min-ho", "Jin-woo", "Dong-hyun", "Woo-jin", "Kyung-soo" };
                    last = new[] { "Kim", "Lee", "Park", "Choi", "Jung", "Kang", "Cho", "Yoon", "Jang", "Lim", "Han", "Oh", "Seo", "Shin", "Kwon" };
                    return;
                case "Turkey":
                    first = new[] { "Yusuf", "Emir", "Ömer", "Kerem", "Aras", "Burak", "Emre", "Hakan", "Serkan", "Deniz", "Baran", "Kaan", "Oğuz", "Cem", "Mert" };
                    last = new[] { "Yılmaz", "Kaya", "Demir", "Şahin", "Çelik", "Yıldız", "Yıldırım", "Öztürk", "Aydın", "Özdemir", "Arslan", "Doğan", "Kılıç", "Aslan", "Çetin" };
                    return;
                case "Italy":
                    first = new[] { "Lorenzo", "Alessandro", "Francesco", "Andrea", "Matteo", "Leonardo", "Marco", "Luca", "Davide", "Simone", "Federico", "Giovanni", "Niccolò", "Riccardo", "Tommaso" };
                    last = new[] { "Rossi", "Russo", "Ferrari", "Esposito", "Bianchi", "Romano", "Colombo", "Ricci", "Marino", "Greco", "Gallo", "Conti", "De Luca", "Costa", "Giordano" };
                    return;
                case "Germany":
                    first = new[] { "Leon", "Paul", "Finn", "Elias", "Felix", "Jonas", "Lukas", "Tim", "Niklas", "Moritz", "Julian", "Tobias", "Florian", "Maximilian", "Jan" };
                    last = new[] { "Müller", "Schmidt", "Schneider", "Fischer", "Weber", "Meyer", "Wagner", "Becker", "Schulz", "Hoffmann", "Schäfer", "Koch", "Bauer", "Richter", "Klein" };
                    return;
                case "Spain":
                    first = new[] { "Hugo", "Martín", "Alejandro", "Pablo", "Manuel", "Álvaro", "Adrián", "Mario", "Diego", "Javier", "Sergio", "Carlos", "Jorge", "Iker", "Marc" };
                    last = new[] { "García", "Rodríguez", "González", "Fernández", "López", "Martínez", "Sánchez", "Pérez", "Gómez", "Jiménez", "Ruiz", "Hernández", "Díaz", "Moreno", "Álvarez" };
                    return;
                case "Belgium":
                    first = new[] { "Arthur", "Louis", "Noah", "Liam", "Jules", "Victor", "Oscar", "Mathis", "Nathan", "Théo", "Romain", "Julien", "Sander", "Dries", "Maxime" };
                    last = new[] { "Peeters", "Janssens", "Maes", "Jacobs", "Mertens", "Willems", "Claes", "Goossens", "Wouters", "Dubois", "Lambert", "Laurent", "Simon", "Dupont", "Renard" };
                    return;
                case "France":
                    first = new[] { "Léo", "Raphaël", "Louis", "Arthur", "Jules", "Adam", "Hugo", "Nathan", "Enzo", "Mathis", "Théo", "Antoine", "Julien", "Nicolas", "Paul" };
                    last = new[] { "Martin", "Bernard", "Thomas", "Petit", "Robert", "Richard", "Durand", "Dubois", "Moreau", "Laurent", "Simon", "Michel", "Lefebvre", "Leroy", "Roux" };
                    return;
                case "England":
                    first = new[] { "Oliver", "George", "Harry", "Jack", "Charlie", "Thomas", "William", "James", "Alfie", "Freddie", "Theo", "Joshua", "Edward", "Archie", "Henry" };
                    last = new[] { "Smith", "Jones", "Taylor", "Brown", "Williams", "Wilson", "Johnson", "Davies", "Robinson", "Wright", "Thompson", "Evans", "Walker", "White", "Green" };
                    return;
                case "Netherlands":
                    first = new[] { "Daan", "Sem", "Bram", "Luuk", "Thijs", "Stijn", "Sven", "Lars", "Ruben", "Joost", "Milan", "Jesse", "Mees", "Finn", "Levi" };
                    last = new[] { "de Jong", "Jansen", "de Vries", "Bakker", "Janssen", "Visser", "Smit", "Meijer", "de Boer", "Mulder", "Bos", "Vos", "van Dijk", "de Groot", "Hendriks" };
                    return;
                case "Senegal":
                    first = new[] { "Mamadou", "Ibrahima", "Ousmane", "Moussa", "Cheikh", "Amadou", "Pape", "Lamine", "Sadio", "Babacar", "Abdoulaye", "Serigne", "Demba", "Assane", "Modou" };
                    last = new[] { "Diallo", "Ndiaye", "Diop", "Fall", "Sow", "Ba", "Sarr", "Faye", "Gueye", "Sy", "Kane", "Cissé", "Mbaye", "Thiam", "Diouf" };
                    return;
                case "DR Congo":
                    first = new[] { "Fiston", "Glody", "Chancel", "Youssouf", "Cédric", "Junior", "Fabrice", "Héritier", "Nelson", "Bienvenu", "Didier", "Alain", "Patient", "Jonathan", "Christian" };
                    last = new[] { "Mbemba", "Mukendi", "Ilunga", "Kabongo", "Kasongo", "Kalala", "Mputu", "Banza", "Kabasele", "Lumumba", "Tshibola", "Bakambu", "Ndongala", "Kalonji", "Mwamba" };
                    return;
                case "Ghana":
                    first = new[] { "Kwame", "Kofi", "Yaw", "Kojo", "Emmanuel", "Isaac", "Samuel", "Prince", "Richmond", "Abdul", "Felix", "Francis", "Godwin", "Daniel", "Joseph" };
                    last = new[] { "Mensah", "Owusu", "Boateng", "Asante", "Appiah", "Osei", "Darko", "Addo", "Frimpong", "Agyeman", "Gyamfi", "Opoku", "Amponsah", "Quansah", "Adu" };
                    return;
                case "Morocco":
                    first = new[] { "Youssef", "Amine", "Hamza", "Ayoub", "Mehdi", "Anas", "Zakaria", "Achraf", "Sofiane", "Walid", "Karim", "Nabil", "Reda", "Soufiane", "Hicham" };
                    last = new[] { "El Amrani", "Benali", "Alaoui", "Idrissi", "Bennani", "Tazi", "Berrada", "Cherkaoui", "El Fassi", "Ouazzani", "Bouziane", "Chraibi", "El Mansouri", "Zahraoui", "El Khattabi" };
                    return;
                case "Nigeria":
                    first = new[] { "Chinedu", "Emeka", "Tunde", "Olumide", "Ibrahim", "Samuel", "Victor", "Chukwuemeka", "Oluwaseun", "Adewale", "Taiwo", "Kehinde", "Emmanuel", "Daniel", "Peter" };
                    last = new[] { "Okafor", "Okonkwo", "Adeyemi", "Balogun", "Nwosu", "Eze", "Obi", "Adebayo", "Okeke", "Musa", "Lawal", "Abdullahi", "Chukwu", "Nwachukwu", "Ogunleye" };
                    return;
                default:
                    first = new[] { "Rui", "Tomás", "Gonçalo", "Diogo", "João", "Miguel", "André", "Bruno", "Pedro", "Nuno", "Afonso", "Francisco", "Tiago", "Hugo", "Martim" };
                    last = new[] { "Silva", "Santos", "Ferreira", "Pereira", "Costa", "Oliveira", "Rodrigues", "Martins", "Sousa", "Fernandes", "Gomes", "Lopes", "Marques", "Alves", "Ribeiro" };
                    return;
            }
        }
    }
}
