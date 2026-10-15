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
            "Senegal", "RD Congo", "Gana", "Marrocos", "Nigéria"
        };

        public static string Nation(int seed)
        {
            var index = seed % Nations.Length;
            if (index < 0)
                index += Nations.Length;
            return Nations[index];
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
                    used.Add(player.PortraitPath);
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
                pair.Value.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                var pool = new List<Sprite>(Pool(pair.Key));
                if (pool.Count == 0)
                    pool.AddRange(AnyPool());
                Shuffle(pool, pair.Key);
                var cursor = 0;
                foreach (var player in pair.Value)
                {
                    Sprite sprite = null;
                    while (cursor < pool.Count && used.Contains(Key(pair.Key, pool[cursor])))
                        cursor++;
                    if (cursor < pool.Count)
                    {
                        sprite = pool[cursor];
                        cursor++;
                        used.Add(Key(pair.Key, sprite));
                    }
                    else if (pool.Count > 0)
                        sprite = pool[Mod(player.FullName.GetHashCode(), pool.Count)];
                    if (sprite == null)
                        continue;
                    var key = Key(pair.Key, sprite);
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
            if (sprite == null)
            {
                var pool = AllFaces();
                if (pool.Length > 0)
                    sprite = pool[Mod((player.Id + player.FullName).GetHashCode(), pool.Length)];
            }
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
            if (sprite == null)
                return null;
            var pivot = sprite.pivot;
            var rect = sprite.rect;
            if (rect.width < 2f || rect.height < 2f)
                return sprite;
            if (Mathf.Abs(pivot.x - rect.width * 0.5f) < 2f && Mathf.Abs(pivot.y - rect.height * 0.5f) < 2f)
                return sprite;
            try
            {
                var made = Sprite.Create(sprite.texture, rect, new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
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
                var loaded = Resources.LoadAll<Sprite>(folder);
                foreach (var sprite in loaded)
                {
                    if (sprite != null)
                        list.Add(sprite);
                }

                if (list.Count > 0)
                    break;
            }

            cached = list.ToArray();
            Pools[nation] = cached;
            return cached;
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
