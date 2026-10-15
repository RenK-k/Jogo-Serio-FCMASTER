using System.Collections.Generic;
using CoachMaster.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    public static class KitView
    {
        public const string Folder = "Assets/CoachMaster/Resources/Kits";
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static RectTransform Create(Transform parent, Club club, Vector2 size, bool showReplaceHint = false)
        {
            var root = new GameObject("Kit_" + (club?.KitSlug ?? "placeholder"), typeof(RectTransform));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;

            var frame = UiFactory.Panel(root.transform, "Frame", UiTheme.KitStage);
            UiFactory.Stretch(frame);
            var artGo = new GameObject("Art", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var artRect = artGo.GetComponent<RectTransform>();
            artRect.SetParent(frame, false);
            UiFactory.Stretch(artRect);
            artRect.offsetMin = new Vector2(8, showReplaceHint ? 22 : 8);
            artRect.offsetMax = new Vector2(-8, -8);
            Apply(artGo.GetComponent<Image>(), club);

            if (showReplaceHint && club != null)
            {
                var hint = UiFactory.Label(
                    root.transform,
                    "Slot",
                    $"{Folder}/{club.KitSlug}.png",
                    11,
                    UiTheme.Muted,
                    TextAnchor.MiddleCenter);
                hint.rectTransform.anchorMin = new Vector2(0f, 0f);
                hint.rectTransform.anchorMax = new Vector2(1f, 0f);
                hint.rectTransform.pivot = new Vector2(0.5f, 0f);
                hint.rectTransform.anchoredPosition = new Vector2(0, 4);
                hint.rectTransform.sizeDelta = new Vector2(0, 16);
            }

            return rect;
        }

        public static void Apply(Image target, Club club, Text hint = null)
        {
            if (target == null)
                return;
            target.sprite = GetSprite(club);
            target.preserveAspect = true;
            target.color = Color.white;
            if (hint != null)
                hint.text = club == null ? Folder : $"{Folder}/{club.KitSlug}.png";
        }

        public static Sprite GetSprite(Club club)
        {
            var key = club != null && !string.IsNullOrEmpty(club.KitSlug) ? club.KitSlug : "placeholder";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var custom = LoadCustom(key);
            if (custom != null)
            {
                Cache[key] = custom;
                return custom;
            }

            if (club == null)
                return null;

            var generated = KitGenerator.Create(club);
            var sprite = Sprite.Create(generated, new Rect(0, 0, generated.width, generated.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        static Sprite LoadCustom(string key)
        {
            if (string.IsNullOrEmpty(key) || key == "placeholder")
                return null;

            var imported = Resources.Load<Sprite>("Kits/" + key);
            if (imported != null)
                return imported;

            var texture = Resources.Load<Texture2D>("Kits/" + key);
            if (texture == null)
                return null;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
