using CoachMaster.Domain;

namespace CoachMaster.Career
{
    public static class StaffProfile
    {
        static readonly string[] Nations =
        {
            "Portugal", "Espanha", "França", "Itália", "Alemanha", "Brasil", "Argentina", "Países Baixos", "Inglaterra", "Bélgica"
        };

        static readonly string[] EnglishNations =
        {
            "Portugal", "Spain", "France", "Italy", "Germany", "Brazil", "Argentina", "Netherlands", "England", "Belgium"
        };

        public static int Spread(string id, StaffRole role, int reputation)
        {
            var center = reputation >= 82 ? 16 : reputation >= 74 ? 13 : reputation >= 62 ? 10 : reputation >= 48 ? 7 : 4;
            var swing = Hash((id ?? "") + ":" + role) % 11 - 5;
            var value = center + swing;
            if (value < 1)
                value = 1;
            if (value > 20)
                value = 20;
            return value;
        }

        public static int Skill(StaffMember member, string key, int bias)
        {
            var baseline = member == null || member.Judgement < 1 ? 8 : member.Judgement;
            var swing = Hash((member == null ? "x" : member.Id) + ":" + member.Role + ":" + key) % 11 - 5;
            var value = baseline + bias + swing;
            if (value < 1)
                value = 1;
            if (value > 20)
                value = 20;
            return value;
        }

        public static int Bias(StaffRole role, string key)
        {
            if (role == StaffRole.GoalkeeperCoach && key == "Goalkeeping")
                return 5;
            if (role == StaffRole.GoalkeeperCoach && (key == "Attacking" || key == "Possession"))
                return -4;
            if (role == StaffRole.FitnessCoach && key == "Fitness")
                return 5;
            if (role == StaffRole.FitnessCoach && key == "Tactical")
                return -3;
            if (role == StaffRole.Physiotherapist && key == "Fitness")
                return 4;
            if (role == StaffRole.Physiotherapist && key == "Youngsters")
                return 2;
            if (role == StaffRole.Scout && (key == "Potential" || key == "Judging"))
                return 5;
            if (role == StaffRole.Scout && key == "Motivation")
                return -3;
            if (role == StaffRole.AssistantCoach && (key == "Tactical" || key == "Motivation"))
                return 3;
            return 0;
        }

        public static string Word(int value, bool english)
        {
            if (value >= 18)
                return english ? "Excellent" : "Excelente";
            if (value >= 15)
                return english ? "Good" : "Bom";
            if (value >= 12)
                return english ? "Competent" : "Competente";
            if (value >= 9)
                return english ? "Reasonable" : "Razoável";
            if (value >= 5)
                return english ? "Poor" : "Fraco";
            return english ? "Unsuited" : "Inadequado";
        }

        public static string BestLine(StaffMember member, bool english)
        {
            var bestName = "";
            var best = -1;
            foreach (var skill in Skills)
            {
                var value = Skill(member, skill.Key, Bias(member == null ? StaffRole.Scout : member.Role, skill.Key));
                if (value <= best)
                    continue;
                best = value;
                bestName = english ? skill.English : skill.Portuguese;
            }

            return bestName + "  ·  " + Word(best < 0 ? 1 : best, english);
        }

        public static int Age(StaffMember member)
        {
            return 29 + Hash((member == null ? "" : member.Id) + ":age") % 28;
        }

        public static string Nation(StaffMember member, bool english)
        {
            var list = english ? EnglishNations : Nations;
            return list[Hash((member == null ? "" : member.Id) + ":nation") % list.Length];
        }

        public static string Personality(StaffMember member, bool english)
        {
            var options = english
                ? new[] { "Balanced", "Demanding", "Calm", "Attacking", "Cautious", "Motivator" }
                : new[] { "Equilibrado", "Exigente", "Calmo", "Ofensivo", "Cauteloso", "Motivador" };
            return options[Hash((member == null ? "" : member.Id) + ":persona") % options.Length];
        }

        public static string Formations(StaffMember member)
        {
            var shapes = new[] { "4-3-3", "4-2-3-1", "4-4-2", "3-4-3", "5-4-1" };
            var first = Hash((member == null ? "" : member.Id) + ":f1") % shapes.Length;
            var second = Hash((member == null ? "" : member.Id) + ":f2") % shapes.Length;
            if (second == first)
                second = (second + 1) % shapes.Length;
            return shapes[first] + "   ·   " + shapes[second];
        }

        public static string Style(StaffMember member, bool english)
        {
            var options = english
                ? new[] { "Wing play", "Possession", "Direct", "High press", "Low block", "Set pieces" }
                : new[] { "Jogo pelas alas", "Posse", "Direto", "Pressão alta", "Bloco baixo", "Bolas paradas" };
            return options[Hash((member == null ? "" : member.Id) + ":style") % options.Length];
        }

        public readonly struct SkillName
        {
            public SkillName(string key, string portuguese, string english)
            {
                Key = key;
                Portuguese = portuguese;
                English = english;
            }

            public string Key { get; }
            public string Portuguese { get; }
            public string English { get; }
        }

        public static readonly SkillName[] Skills =
        {
            new SkillName("Attacking", "Ataque", "Attacking"),
            new SkillName("Defending", "Defesa", "Defending"),
            new SkillName("Fitness", "Físico", "Fitness"),
            new SkillName("Goalkeeping", "Guarda-redes", "Goalkeeping"),
            new SkillName("Possession", "Posse", "Possession"),
            new SkillName("SetPieces", "Bolas paradas", "Set pieces"),
            new SkillName("Tactical", "Tática", "Tactical"),
            new SkillName("Technical", "Técnica", "Technical"),
            new SkillName("Youngsters", "Jovens", "Young players"),
            new SkillName("Motivation", "Motivação", "Motivating"),
            new SkillName("Determination", "Determinação", "Determination"),
            new SkillName("Judging", "Avaliar jogadores", "Judging players"),
            new SkillName("Potential", "Avaliar potencial", "Judging potential"),
            new SkillName("Negotiation", "Negociação", "Negotiating")
        };

        static int Hash(string text)
        {
            unchecked
            {
                var hash = 23;
                if (text != null)
                {
                    foreach (var c in text)
                        hash = hash * 31 + c;
                }

                return hash & 0x7fffffff;
            }
        }
    }
}
