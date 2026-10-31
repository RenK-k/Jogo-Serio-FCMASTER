using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

namespace CoachMaster.UI
{
    public static class LeagueBackdrop
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static readonly Dictionary<Image, string> FallbackIds = new Dictionary<Image, string>();

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

        public static void PaintPrincipal(Transform root)
        {
            if (root == null)
                return;
            var sprite = Principal();
            if (sprite == null)
                return;
            var imageTransform = root.Find("PrincipalArt");
            var image = imageTransform == null ? null : imageTransform.GetComponent<Image>();
            if (image == null)
            {
                image = BroadcastUi.Solid(root, "PrincipalArt", Color.white, false);
                BroadcastUi.Stretch(image.rectTransform);
                image.transform.SetSiblingIndex(BackgroundLayerIndex(root));
            }
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = false;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            if (sprite.texture != null)
                sprite.texture.filterMode = FilterMode.Bilinear;

            var shadeTransform = root.Find("PrincipalShade");
            var shade = shadeTransform == null ? null : shadeTransform.GetComponent<Image>();
            if (shade == null)
            {
                shade = BroadcastUi.Solid(root, "PrincipalShade", new Color(0.01f, 0.08f, 0.16f, 0.18f), false);
                BroadcastUi.Stretch(shade.rectTransform);
            }
            shade.color = new Color(0.01f, 0.08f, 0.16f, 0.18f);
            shade.raycastTarget = false;
            shade.transform.SetSiblingIndex(image.transform.GetSiblingIndex() + 1);
        }

        static Sprite Principal()
        {
            if (Cache.TryGetValue("principal", out var cached))
                return cached;
            var all = Resources.LoadAll<Sprite>("Ligas/fundo principal");
            Sprite best = null;
            if (all != null)
            {
                for (var i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && (best == null || all[i].rect.width * all[i].rect.height > best.rect.width * best.rect.height))
                        best = all[i];
                }
            }
            if (best == null)
                best = Resources.Load<Sprite>("Ligas/fundo principal");
            Cache["principal"] = best;
            return best;
        }

        public static void Paint(Transform root, string leagueId)
        {
            if (root == null)
                return;
            var imageTransform = root.Find("LeagueArt");
            var image = imageTransform == null ? null : imageTransform.GetComponent<Image>();
            if (image == null)
            {
                image = BroadcastUi.Solid(root, "LeagueArt", Color.white, false);
                BroadcastUi.Stretch(image.rectTransform);
                FallbackIds[image] = leagueId;
                image.transform.SetSiblingIndex(BackgroundLayerIndex(root));
            }
            else if (!string.IsNullOrEmpty(leagueId))
                FallbackIds[image] = leagueId;

            var shadeTransform = root.Find("LeagueShade");
            var shade = shadeTransform == null ? null : shadeTransform.GetComponent<Image>();
            if (shade == null)
            {
                shade = BroadcastUi.Solid(root, "LeagueShade", new Color(0.04f, 0.02f, 0.1f, 0.5f), false);
                BroadcastUi.Stretch(shade.rectTransform);
            }
            shade.transform.SetSiblingIndex(image.transform.GetSiblingIndex() + 1);
            Apply(image);
        }

        static int BackgroundLayerIndex(Transform root)
        {
            var index = 0;
            for (var i = 0; i < root.childCount; i++)
            {
                var name = root.GetChild(i).name;
                if (name == "Backdrop" || name.StartsWith("Stripe") || name == "TopShade" || name == "Floor")
                    index = i + 1;
            }
            return index;
        }

        static void Apply(Image image)
        {
            if (image == null)
                return;
            FallbackIds.TryGetValue(image, out var fallbackId);
            var chosen = BackgroundPrefs.SelectedLeagueId;
            if (chosen == BackgroundPrefs.UserMode)
            {
                var user = BackgroundPrefs.UserSprite();
                if (user != null)
                {
                    image.sprite = user;
                    image.color = Color.white;
                    image.preserveAspect = false;
                    image.type = Image.Type.Simple;
                    return;
                }
                chosen = BackgroundPrefs.DefaultMode;
            }
            if (chosen == BackgroundPrefs.DefaultMode)
            {
                var principal = Principal();
                if (principal != null)
                {
                    image.sprite = principal;
                    image.color = Color.white;
                    image.preserveAspect = false;
                    image.type = Image.Type.Simple;
                    return;
                }
            }
            var effectiveId = chosen ?? fallbackId;
            if (string.IsNullOrEmpty(effectiveId))
                return;
            var sprite = First(effectiveId);
            if (sprite == null)
            {
                if (BackgroundPrefs.SelectedLeagueId == null)
                    RemoveLayer(image);
                return;
            }
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = false;
            image.type = Image.Type.Simple;
            sprite.texture.filterMode = FilterMode.Bilinear;
            var aspect = image.GetComponent<AspectRatioFitter>();
            if (aspect == null)
                aspect = image.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = sprite.rect.width / sprite.rect.height;
        }

        static void RemoveLayer(Image image)
        {
            if (image == null)
                return;
            var root = image.transform.parent;
            var shade = root == null ? null : root.Find("LeagueShade");
            FallbackIds.Remove(image);
            DestroyObject(image.gameObject);
            if (shade != null)
                DestroyObject(shade.gameObject);
        }

        static void DestroyObject(GameObject target)
        {
            if (target == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }

        public static void RefreshCurrentScene()
        {
            var images = Object.FindObjectsByType<Image>(FindObjectsSortMode.None);
            var found = false;
            for (var i = 0; i < images.Length; i++)
            {
                var image = images[i];
                if (image == null || image.name != "LeagueArt")
                    continue;
                found = true;
                FallbackIds.TryGetValue(image, out var fallbackId);
                if (BackgroundPrefs.SelectedLeagueId == null && string.IsNullOrEmpty(fallbackId))
                    RemoveLayer(image);
                else
                    Apply(image);
            }

            if (found || BackgroundPrefs.SelectedLeagueId == null)
                return;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var activeScene = SceneManager.GetActiveScene();
            for (var i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                if (canvas == null || canvas.gameObject.scene != activeScene)
                    continue;
                var root = canvas.transform.Find("Design") ?? canvas.transform.Find("Runtime");
                if (root == null)
                    continue;
                Paint(root, null);
                break;
            }
        }
    }

    public static class BackgroundPrefs
    {
        const string PreferenceKey = "cm_background_index";
        const string UserPathKey = "cm_background_user";
        static Sprite UserCached;
        static readonly string[] LeagueIds =
        {
            null,
            DefaultMode,
            UserMode,
            "league:atlas",
            "league:espanhola",
            "league:espanhola-2",
            "league:inglesa",
            "league:inglesa-championship",
            "league:inglesa-2"
        };
        static readonly string[] Labels =
        {
            "FUNDO  ·  AUTOMÁTICO",
            "FUNDO  ·  PRINCIPAL",
            "FUNDO  ·  O TEU",
            "FUNDO  ·  PORTUGAL",
            "FUNDO  ·  ESPANHA",
            "FUNDO  ·  ESPANHA II",
            "FUNDO  ·  INGLATERRA",
            "FUNDO  ·  CHAMPIONSHIP",
            "FUNDO  ·  LEAGUE TWO"
        };

        public static string SelectedLeagueId
        {
            get
            {
                var index = Mathf.Clamp(PlayerPrefs.GetInt(PreferenceKey, 0), 0, LeagueIds.Length - 1);
                return LeagueIds[index];
            }
        }

        public static string Label
        {
            get
            {
                var index = Mathf.Clamp(PlayerPrefs.GetInt(PreferenceKey, 0), 0, Labels.Length - 1);
                return Labels[index];
            }
        }

        public const string UserMode = "user";
        public const string DefaultMode = "default";

        public static bool SetUserImage(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;
            try
            {
                var folder = Path.Combine(Application.persistentDataPath, "CoachMaster", "Backgrounds");
                Directory.CreateDirectory(folder);
                var dest = Path.Combine(folder, "user-background" + Path.GetExtension(path));
                File.Copy(path, dest, true);
                PlayerPrefs.SetString(UserPathKey, dest);
                PlayerPrefs.SetInt(PreferenceKey, IndexOf(UserMode));
                PlayerPrefs.Save();
                UserCached = null;
                LeagueBackdrop.RefreshCurrentScene();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool HasUserImage
        {
            get
            {
                var path = PlayerPrefs.GetString(UserPathKey, "");
                return !string.IsNullOrEmpty(path) && File.Exists(path);
            }
        }

        public static bool DeleteUserImage()
        {
            var path = PlayerPrefs.GetString(UserPathKey, "");
            var folder = Path.GetFullPath(Path.Combine(Application.persistentDataPath, "CoachMaster", "Backgrounds"));
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    var fullPath = Path.GetFullPath(path);
                    if (string.Equals(Path.GetDirectoryName(fullPath), folder, System.StringComparison.OrdinalIgnoreCase)
                        && Path.GetFileName(fullPath).StartsWith("user-background", System.StringComparison.OrdinalIgnoreCase))
                        File.Delete(fullPath);
                }
                PlayerPrefs.DeleteKey(UserPathKey);
                if (SelectedLeagueId == UserMode)
                    PlayerPrefs.SetInt(PreferenceKey, IndexOf(null));
                PlayerPrefs.Save();
                if (UserCached != null)
                {
                    if (UserCached.texture != null) Object.Destroy(UserCached.texture);
                    Object.Destroy(UserCached);
                }
                UserCached = null;
                LeagueBackdrop.RefreshCurrentScene();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static Sprite UserSprite()
        {
            var path = PlayerPrefs.GetString(UserPathKey, "");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;
            if (UserCached != null)
                return UserCached;
            try
            {
                var data = File.ReadAllBytes(path);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(data))
                    return null;
                texture.filterMode = FilterMode.Bilinear;
                UserCached = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                return UserCached;
            }
            catch
            {
                return null;
            }
        }

        static int IndexOf(string id)
        {
            for (var i = 0; i < LeagueIds.Length; i++)
                if (LeagueIds[i] == id)
                    return i;
            return 0;
        }

        public static void Cycle()
        {
            var current = Mathf.Clamp(PlayerPrefs.GetInt(PreferenceKey, 0), 0, LeagueIds.Length - 1);
            PlayerPrefs.SetInt(PreferenceKey, (current + 1) % LeagueIds.Length);
            PlayerPrefs.Save();
            LeagueBackdrop.RefreshCurrentScene();
        }
    }
}
