using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    public sealed class MenuFigures : MonoBehaviour
    {
        const string IndexKey = "cm_menu_silhouette";
        static readonly string[] SceneNames = { "SilhuetaNey1", "SilhuetaNey2", "SilhuetaShev1" };
        static readonly string[] Paths = { "silhuetas/ney1", "silhuetas/ney2", "silhuetas/shev1" };
        static bool _pickedThisLaunch;
        static int _spriteIndex;

        public static void Ensure(Transform root)
        {
            if (root == null)
                return;
            Hide(root.Find("SilhouetteSmaller"));
            Hide(root.Find("SilhouetteBigger"));
            Hide(root.Find("SilhouetteSize"));
            var placed = Collect(root);
            if (placed.Length == 0)
                placed = CreateDefaults(root);
            if (!Application.isPlaying || placed.Length == 0)
                return;
            var chosen = NextIndex(placed.Length);
            for (var i = 0; i < placed.Length; i++)
                placed[i].gameObject.SetActive(i == chosen);
            Hide(root.Find("MenuFigures"));
        }

        static Transform[] CreateDefaults(Transform root)
        {
            var sprites = LoadFrames();
            var made = new System.Collections.Generic.List<Transform>();
            for (var i = 0; i < SceneNames.Length && i < sprites.Length; i++)
            {
                var go = new GameObject(SceneNames[i], typeof(RectTransform), typeof(Image));
                go.transform.SetParent(root, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(-0.1f, -0.13f);
                rect.anchorMax = new Vector2(0.62f, 1.13f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.anchoredPosition = new Vector2(-7f, 0f);
                var image = go.GetComponent<Image>();
                image.sprite = sprites[i];
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.color = Color.white;
                PlaceBehindTitle(root, go.transform);
                made.Add(go.transform);
            }

            return made.ToArray();
        }

        static int NextIndex(int count)
        {
            if (count <= 0)
                return 0;
            if (!_pickedThisLaunch)
            {
                _spriteIndex = Mathf.Abs(PlayerPrefs.GetInt(IndexKey, 0));
                PlayerPrefs.SetInt(IndexKey, _spriteIndex + 1);
                PlayerPrefs.Save();
                _pickedThisLaunch = true;
            }

            return _spriteIndex % count;
        }

        static Transform[] Collect(Transform root)
        {
            var found = new System.Collections.Generic.List<Transform>();
            for (var i = 0; i < SceneNames.Length; i++)
            {
                var item = root.Find(SceneNames[i]);
                if (item != null)
                    found.Add(item);
            }

            return found.ToArray();
        }

        static void PlaceBehindTitle(Transform root, Transform host)
        {
            var title = root.Find("Title");
            var shade = root.Find("TitleShade");
            var insert = title != null ? title.GetSiblingIndex() : host.GetSiblingIndex();
            if (shade != null && shade.GetSiblingIndex() < insert)
                insert = shade.GetSiblingIndex();
            host.SetSiblingIndex(insert);
        }

        static void Hide(Transform item)
        {
            if (item != null)
                item.gameObject.SetActive(false);
        }

        static Sprite[] LoadFrames()
        {
            var found = new System.Collections.Generic.List<Sprite>();
            for (var i = 0; i < Paths.Length; i++)
            {
                var sprite = Resources.Load<Sprite>(Paths[i]);
                if (sprite != null)
                    found.Add(sprite);
            }

            return found.ToArray();
        }
    }
}
