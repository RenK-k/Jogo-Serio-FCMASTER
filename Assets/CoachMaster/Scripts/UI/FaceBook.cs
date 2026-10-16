using System.Collections.Generic;
using CoachMaster.Domain;
using UnityEngine;

namespace CoachMaster.UI
{
    public static class FaceBook
    {
        static readonly Dictionary<string, Sprite[]> Pools = new Dictionary<string, Sprite[]>();
        static readonly Dictionary<string, Sprite> ByPlayer = new Dictionary<string, Sprite>();
        static Sprite[] _all;

        static readonly string[] Nations =
        {
            "Argentina", "Brasil", "Japão", "Coreia do Sul", "Turquia", "Itália", "Portugal",
            "Alemanha", "Espanha", "Bélgica", "França", "Inglaterra", "Países Baixos",
            "Senegal", "RD Congo", "Gana", "Marrocos", "Nigéria",
            "Escócia", "Irlanda", "Dinamarca", "Suécia", "Áustria", "Polónia", "Croácia",
            "Sérvia", "República Checa", "Hungria", "Suíça", "Roménia",
            "Uruguai", "Colômbia", "Chile", "Paraguai", "Equador", "Peru", "Venezuela", "Bolívia"
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
            var home = "Portugal";
            if (!string.IsNullOrEmpty(slug))
            {
                if (slug.StartsWith("inglesa"))
                    home = "Inglaterra";
                else if (slug.StartsWith("francesa"))
                    home = "França";
                else if (slug.StartsWith("espanhola"))
                    home = "Espanha";
            }

            var roll = Mod(seed, 100);
            if (roll < 74)
                return home;
            if (roll < 90)
                return Neighbour(home, seed / 3);
            if (roll < 98)
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
            var pool = new[] { "Brasil", "Argentina", "Uruguai", "Chile", "Peru", "Equador", "Nigéria", "Gana", "Senegal", "Itália", "Alemanha", "Polónia", "Croácia", "Áustria", "Dinamarca", "Suécia" };
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
            var used = new HashSet<string>();
            foreach (var player in world.Players.Values)
            {
                if (!string.IsNullOrEmpty(player.PortraitPath))
                {
                    var mark = player.PortraitPath.LastIndexOf('|');
                    used.Add(mark < 0 ? player.PortraitPath : player.PortraitPath.Substring(mark + 1));
                }
            }

            var groups = new Dictionary<string, List<Player>>();
            foreach (var player in world.Players.Values)
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

            foreach (var pair in groups)
            {
                pair.Value.Sort((a, b) =>
                {
                    var age = a.Age.CompareTo(b.Age);
                    return age != 0 ? age : string.CompareOrdinal(a.Id, b.Id);
                });
                var pool = new List<Sprite>(Pool(pair.Key));
                pool.Sort((a, b) => Youth(a).CompareTo(Youth(b)));
                foreach (var player in pair.Value)
                {
                    var sprite = TakeFresh(pool, used);
                    if (sprite == null)
                        sprite = TakeFresh(Spare(pair.Key), used);
                    if (sprite == null)
                        continue;
                    var key = sprite.name;
                    used.Add(key);
                    player.PortraitPath = key;
                    ByPlayer[player.Id] = Center(sprite);
                }
            }
        }

        public static Sprite For(Player player)
        {
            if (player == null)
                return null;
            if (ByPlayer.TryGetValue(player.Id, out var cached) && cached != null)
                return cached;

            var sprite = Find(player.Nationality, player.PortraitPath);
            sprite = Center(sprite);
            if (sprite != null)
                ByPlayer[player.Id] = sprite;
            return sprite;
        }

        static Sprite[] AllFaces()
        {
            if (_all != null)
                return _all;
            var loaded = Resources.LoadAll<Sprite>("Kits/Players");
            _all = loaded ?? new Sprite[0];
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
            var all = new List<Sprite>();
            foreach (var nation in Nations)
            {
                foreach (var sprite in Pool(nation))
                {
                    if (sprite != null)
                        all.Add(sprite);
                }
            }

            return all.ToArray();
        }

        static Sprite TakeFresh(List<Sprite> pool, HashSet<string> used)
        {
            if (pool == null)
                return null;
            for (var i = 0; i < pool.Count; i++)
            {
                var sprite = pool[i];
                if (sprite == null || used.Contains(sprite.name))
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
            var regen = name.StartsWith("KT1") ? 0 : 8000;
            var id = 0;
            var digits = 0;
            for (var i = name.Length - 1; i >= 0 && name[i] >= '0' && name[i] <= '9' && digits < 5; i--)
            {
                id += (name[i] - '0') * Pow10(digits);
                digits++;
            }

            return regen + id;
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
            var mark = portraitPath.LastIndexOf('|');
            var name = mark < 0 ? portraitPath : portraitPath.Substring(mark + 1);
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
            var loaded = Resources.LoadAll<Sprite>(folder);
            return loaded ?? new Sprite[0];
        }

        static void AddShare(List<Sprite> list, string nation)
        {
            switch (nation)
            {
                case "Portugal": Take(list, "Kits/Players/Europe/SpanMed", 0, 3); break;
                case "Espanha": Take(list, "Kits/Players/Europe/SpanMed", 1, 3); break;
                case "Itália": Take(list, "Kits/Players/Europe/SpanMed", 2, 3); break;
                case "Inglaterra": Take(list, "Kits/Players/Europe/Caucasian 1", 0, 8); break;
                case "França": Take(list, "Kits/Players/Europe/Caucasian 1", 1, 8); break;
                case "Países Baixos": Take(list, "Kits/Players/Europe/Caucasian 1", 2, 8); break;
                case "Bélgica": Take(list, "Kits/Players/Europe/Caucasian 1", 3, 8); break;
                case "Escócia": Take(list, "Kits/Players/Europe/Caucasian 1", 4, 8); break;
                case "Irlanda": Take(list, "Kits/Players/Europe/Caucasian 1", 5, 8); break;
                case "Dinamarca": Take(list, "Kits/Players/Europe/Caucasian 1", 6, 8); break;
                case "Suécia": Take(list, "Kits/Players/Europe/Caucasian 1", 7, 8); break;
                case "Alemanha": Take(list, "Kits/Players/Europe/Central European", 0, 9); break;
                case "Áustria": Take(list, "Kits/Players/Europe/Central European", 1, 9); break;
                case "Polónia": Take(list, "Kits/Players/Europe/Central European", 2, 9); break;
                case "Croácia": Take(list, "Kits/Players/Europe/Central European", 3, 9); break;
                case "Sérvia": Take(list, "Kits/Players/Europe/Central European", 4, 9); break;
                case "República Checa": Take(list, "Kits/Players/Europe/Central European", 5, 9); break;
                case "Hungria": Take(list, "Kits/Players/Europe/Central European", 6, 9); break;
                case "Suíça": Take(list, "Kits/Players/Europe/Central European", 7, 9); break;
                case "Roménia": Take(list, "Kits/Players/Europe/Central European", 8, 9); break;
                case "Argentina": Take(list, "Kits/Players/South American/South American", 0, 10); break;
                case "Brasil": Take(list, "Kits/Players/South American/South American", 1, 10); break;
                case "Uruguai": Take(list, "Kits/Players/South American/South American", 2, 10); break;
                case "Colômbia": Take(list, "Kits/Players/South American/South American", 3, 10); break;
                case "Chile": Take(list, "Kits/Players/South American/South American", 4, 10); break;
                case "Paraguai": Take(list, "Kits/Players/South American/South American", 5, 10); break;
                case "Equador": Take(list, "Kits/Players/South American/South American", 6, 10); break;
                case "Peru": Take(list, "Kits/Players/South American/South American", 7, 10); break;
                case "Venezuela": Take(list, "Kits/Players/South American/South American", 8, 10); break;
                case "Bolívia": Take(list, "Kits/Players/South American/South American", 9, 10); break;
            }
        }

        static void Take(List<Sprite> dest, string folder, int bucket, int buckets)
        {
            var all = new List<Sprite>(Load(folder));
            all.Sort((a, b) => string.CompareOrdinal(a == null ? "" : a.name, b == null ? "" : b.name));
            for (var i = 0; i < all.Count; i++)
            {
                if (all[i] != null && i % buckets == bucket)
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
