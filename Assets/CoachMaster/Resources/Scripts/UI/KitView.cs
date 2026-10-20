using System.Collections.Generic;
using CoachMaster.Data;
using CoachMaster.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    public static class KitView
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static RectTransform Create(Transform parent, Club club, Vector2 size)
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
            artRect.offsetMin = new Vector2(8, 8);
            artRect.offsetMax = new Vector2(-8, -8);
            Apply(artGo.GetComponent<Image>(), club);

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
                hint.text = club == null ? "" : club.ShortName;
        }

        public static Sprite GetSprite(Club club)
        {
            if (club == null)
                return null;
            WorldCatalog.PaintClub(club);
            var key = (string.IsNullOrEmpty(club.KitSlug) ? "placeholder" : club.KitSlug) + "|" + club.JerseyPrimaryHex + "|" + club.KitPattern;
            if (Cache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var generated = KitGenerator.Create(club);
            var sprite = Sprite.Create(generated, new Rect(0, 0, generated.width, generated.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }
    }
}
