using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    public static class LeagueBackdrop
    {
        public static string Folder(string leagueId)
        {
            var slug = leagueId ?? "atlas";
            var cut = slug.LastIndexOf(':');
            if (cut >= 0 && cut < slug.Length - 1)
                slug = slug.Substring(cut + 1);
            return "Ligas/" + slug;
        }

        public static Sprite First(string leagueId)
        {
            var all = Resources.LoadAll<Sprite>(Folder(leagueId));
            if (all == null || all.Length == 0)
                return null;
            return all[0];
        }

        public static string ForFixture(string fixtureLeagueId, string clubLeagueId)
        {
            if (!string.IsNullOrEmpty(fixtureLeagueId) && fixtureLeagueId.Contains("campeoes"))
                return "league:campeoes";
            if (!string.IsNullOrEmpty(fixtureLeagueId) && !fixtureLeagueId.StartsWith("cup:") && First(fixtureLeagueId) != null)
                return fixtureLeagueId;
            return string.IsNullOrEmpty(clubLeagueId) ? "league:atlas" : clubLeagueId;
        }

        public static void Paint(Transform root, string leagueId)
        {
            var sprite = First(leagueId);
            if (sprite == null || root == null)
                return;
            var image = BroadcastUi.Solid(root, "LeagueArt", Color.white, false);
            BroadcastUi.Stretch(image.rectTransform);
            image.sprite = sprite;
            image.preserveAspect = false;
            image.type = Image.Type.Simple;
            var shade = BroadcastUi.Solid(root, "LeagueShade", new Color(0.04f, 0.02f, 0.1f, 0.5f), false);
            BroadcastUi.Stretch(shade.rectTransform);
        }
    }
}
