namespace CoachMaster.Domain
{
    public static class Ids
    {
        public static string League(string slug) => $"league:{slug}";
        public static string Club(string slug) => $"club:{slug}";
        public static string Player(string clubSlug, int squadNumber) => $"player:{clubSlug}:{squadNumber}";
        public static string Staff(string clubSlug, string roleSlug) => $"staff:{clubSlug}:{roleSlug}";
    }
}
