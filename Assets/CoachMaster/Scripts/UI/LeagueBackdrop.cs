using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace CoachMaster.UI
{
    public static class LeagueBackdrop
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static string Folder(string leagueId)
        {
            var slug = leagueId ?? "atlas";
            var cut = slug.LastIndexOf(':');
            if (cut >= 0 && cut < slug.Length - 1)
                slug = slug.Substring(cut + 1);

            if (slug == "inglesa-championship")
                slug = "inglesa-2";
            else if (slug == "inglesa-league-one")
                slug = "inglesa-3";
            else if (slug == "inglesa-2")
                slug = "inglesa-4";
            return "Ligas/" + slug;
        }

        public static Sprite First(string leagueId)
        {
            var folder = Folder(leagueId);
            if (Cache.TryGetValue(folder, out var cached))
                return cached;

            var all = Resources.LoadAll<Sprite>(folder);
            Sprite best = null;
            if (all != null)
            {
                for (var i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && (best == null || all[i].rect.width * all[i].rect.height > best.rect.width * best.rect.height))
                        best = all[i];
                }
            }

            Cache[folder] = best;
            return best;
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
            sprite.texture.filterMode = FilterMode.Bilinear;
            var aspect = image.GetComponent<AspectRatioFitter>();
            if (aspect == null)
                aspect = image.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = sprite.rect.width / sprite.rect.height;
            var shade = BroadcastUi.Solid(root, "LeagueShade", new Color(0.04f, 0.02f, 0.1f, 0.5f), false);
            BroadcastUi.Stretch(shade.rectTransform);
        }
    }
}
