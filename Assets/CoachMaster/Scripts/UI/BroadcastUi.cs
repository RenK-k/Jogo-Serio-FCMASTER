using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    public static class BroadcastUi
    {
        static Sprite _round;
        static Sprite _circle;
        static Sprite _ring;
        static Texture2D _brandTexture;

        public static Sprite Round => _round != null ? _round : (_round = MakeRound());
        public static Sprite Circle => _circle != null ? _circle : (_circle = MakeDisc(false));
        public static Sprite Ring => _ring != null ? _ring : (_ring = MakeDisc(true));
        public static Texture2D BrandTexture => _brandTexture != null ? _brandTexture : (_brandTexture = Resources.Load<Texture2D>("fcmasterGIFAnimated") ?? Resources.Load<Texture2D>("fCMASTER"));
        public static Rect BrandUvRect
        {
            get
            {
                var texture = BrandTexture;
                if (texture == null || texture.width < 2048 || texture.height < 2048)
                    return new Rect(0.25f, 0.41f, 0.5f, 0.35f);
                const float columns = 8f;
                const float rows = 8f;
                var insetX = 0.5f / 2048f;
                var insetY = 0.5f / 2048f;
                return new Rect(insetX, 1f - 1f / rows + insetY, 1f / columns - insetX * 2f, 1f / rows - insetY * 2f);
            }
        }

        public static void ApplyBrand(RawImage image)
        {
            if (image == null)
                return;
            image.texture = BrandTexture;
            image.uvRect = BrandUvRect;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        public static RectTransform Mount(Component host)
        {
            var canvas = host.GetComponent<Canvas>();
            if (canvas == null)
                canvas = host.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Canvas em falta para montar o ecrã.");
                return null;
            }

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                var scale = Mathf.Clamp(SettingsPrefs.UiScale, 0.8f, 1.2f);
                scaler.referenceResolution = new Vector2(1920f / scale, 1080f / scale);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
            }

            canvas.pixelPerfect = true;

            var root = canvas.transform;
            var design = root.Find("Design") as RectTransform;
            if (design != null)
            {
                for (var i = root.childCount - 1; i >= 0; i--)
                {
                    var child = root.GetChild(i).gameObject;
                    if (child.transform == design)
                        continue;
                    child.SetActive(false);
                }

                RestoreShapes(design);
                ConsequenceToast.Attach(design);
                return design;
            }

            var runtime = new GameObject("Runtime", typeof(RectTransform));
            var runtimeRect = runtime.GetComponent<RectTransform>();
            runtimeRect.SetParent(root, false);
            Stretch(runtimeRect);
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                if (child == runtime)
                    continue;
                child.SetActive(false);
            }

            ConsequenceToast.Attach(runtimeRect);
            return runtimeRect;
        }

        public static RectTransform CreateDesign(Transform canvas)
        {
            if (canvas == null)
                return null;
            var stale = new System.Collections.Generic.List<GameObject>();
            for (var i = 0; i < canvas.childCount; i++)
                stale.Add(canvas.GetChild(i).gameObject);
            for (var i = 0; i < stale.Count; i++)
            {
                if (stale[i] == null)
                    continue;
                stale[i].SetActive(false);
                UnityEngine.Object.DestroyImmediate(stale[i]);
            }
            var go = new GameObject("Design", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(canvas, false);
            Stretch(rect);
            return rect;
        }

        public static void Backdrop(Transform parent)
        {
            var bg = Solid(parent, "Backdrop", UiTheme.Canvas, true);
            Stretch(bg.rectTransform);
            for (var i = 0; i < 16; i++)
            {
                var stripe = Solid(parent, "Stripe" + i, i % 2 == 0 ? UiTheme.Stripe : UiTheme.StripeAlt);
                var x = i / 16f;
                Place(stripe.rectTransform, x, 0f, x + (1f / 15f), 1f);
            }

            if (BackgroundPrefs.SelectedLeagueId != null)
                LeagueBackdrop.Paint(parent, null);

            var shade = Solid(parent, "TopShade", new Color(0.05f, 0.02f, 0.1f, 0.28f));
            Place(shade.rectTransform, 0f, 0.9f, 1f, 1f);
            var floor = Solid(parent, "Floor", UiTheme.Footer, true);
            Place(floor.rectTransform, 0f, 0f, 1f, 0.055f);
        }

        public static void ChampionsNight(Transform parent)
        {
            var bg = Solid(parent, "Backdrop", new Color(0.03f, 0.1f, 0.28f, 1f), true);
            Stretch(bg.rectTransform);
            var wash = Solid(parent, "Night", new Color(0.05f, 0.16f, 0.42f, 1f));
            Stretch(wash.rectTransform);
            for (var i = 0; i < 42; i++)
            {
                var star = Solid(parent, "Star" + i, new Color(1f, 1f, 1f, 0.35f + (i % 5) * 0.12f));
                star.sprite = Circle;
                var x = ((i * 47) % 100) / 100f;
                var y = ((i * 29) % 100) / 100f;
                star.rectTransform.anchorMin = star.rectTransform.anchorMax = new Vector2(x, y);
                var size = 3f + (i % 4) * 2f;
                star.rectTransform.sizeDelta = new Vector2(size, size);
            }

            var floor = Solid(parent, "Floor", new Color(0.02f, 0.06f, 0.16f, 1f), true);
            Place(floor.rectTransform, 0f, 0f, 1f, 0.055f);
        }

        public static RectTransform Zone(Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, x0, y0, x1, y1);
            return rect;
        }

        public static Image Solid(Transform parent, string name, Color color, bool raycast = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        public static Image Panel(Transform parent, string name, Color color, bool raycast = false)
        {
            var image = Solid(parent, name, color, raycast);
            image.sprite = Round;
            image.type = Image.Type.Sliced;
            return image;
        }

        public static Text Label(Transform parent, string name, string content, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = UiTheme.Font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = content;
            if (go.GetComponentInParent<InputField>() == null || name == "Placeholder")
            {
                var localizer = go.AddComponent<LocalizedText>();
                localizer.ApplyNow();
            }
            return text;
        }

        static float _clickLock;

        public static float ClickLock => _clickLock;

        public static bool AllowClick()
        {
            if (Time.unscaledTime < _clickLock)
                return false;
            _clickLock = Time.unscaledTime + 0.3f;
            return true;
        }

        public static Button Button(Transform parent, string name, string label, Action onClick, Color color, Color textColor, int fontSize = 20)
        {
            var image = Panel(parent, name, color, true);
            var button = image.gameObject.AddComponent<Button>();
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = SettingsPrefs.ReduceMotion ? 0f : 0.08f;
            button.colors = colors;
            if (onClick != null)
                button.onClick.AddListener(() =>
                {
                    if (SquadDragCard.Holding || !AllowClick())
                        return;
                    GameAudio.Click();
                    onClick();
                });
            var text = Label(image.transform, "Label", label, fontSize, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(12, 4);
            text.rectTransform.offsetMax = new Vector2(-12, -4);
            return button;
        }

        public static Button CloseX(Transform parent, string name, Action onClick)
        {
            var image = Solid(parent, name, new Color(1f, 1f, 1f, 0f), true);
            var button = image.gameObject.AddComponent<Button>();
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            if (onClick != null)
                button.onClick.AddListener(() =>
                {
                    if (!AllowClick())
                        return;
                    onClick();
                });
            var text = Label(image.transform, "Label", "X", 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(text.rectTransform);
            return button;
        }

        public static Button Hit(Image image, Action onClick)
        {
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = SettingsPrefs.ReduceMotion ? 0f : 0.08f;
            button.colors = colors;
            if (onClick != null)
                button.onClick.AddListener(() =>
                {
                    if (SquadDragCard.Holding || !AllowClick())
                        return;
                    GameAudio.Click();
                    onClick();
                });
            return button;
        }

        public static ScrollRect Scroll(Transform parent, string name, out RectTransform content)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var bg = go.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.01f);
            bg.raycastTarget = true;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content = contentGo.GetComponent<RectTransform>();
            content.SetParent(rect, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = go.GetComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = rect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 36f;
            return scroll;
        }

        public static void Wordmark(Transform parent, string league)
        {
            var block = Zone(parent, "Wordmark", 0.36f, 0.915f, 0.64f, 0.995f);
            var markObject = new GameObject("Mark", typeof(RectTransform), typeof(RawImage));
            markObject.transform.SetParent(block, false);
            var mark = markObject.GetComponent<RawImage>();
            ApplyBrand(mark);
            mark.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            mark.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            mark.rectTransform.sizeDelta = new Vector2(48, 48);
            mark.rectTransform.anchoredPosition = new Vector2(-118, 2);
            var title = Label(block, "League", string.IsNullOrEmpty(league) ? "COACH MASTER" : league.ToUpperInvariant(), 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(title.rectTransform, 0.12f, 0.42f, 0.95f, 0.95f);
            var sub = Label(block, "Brand", "COACH MASTER", 11, UiTheme.AccentSoft, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(sub.rectTransform, 0.12f, 0.05f, 0.95f, 0.48f);
        }

        public static Image Meter(Transform parent, string name, float width, out Image fill)
        {
            var bg = Solid(parent, name, new Color(1f, 1f, 1f, 0.16f));
            var le = bg.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.minWidth = width;
            le.preferredHeight = 8;
            le.minHeight = 8;
            le.flexibleWidth = 0;
            fill = Solid(bg.transform, "Fill", UiTheme.Fitness);
            Stretch(fill.rectTransform);
            fill.rectTransform.anchorMax = new Vector2(0.75f, 1f);
            return bg;
        }

        public static void SetMeter(Image fill, float amount)
        {
            amount = Mathf.Clamp01(amount);
            fill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.04f, amount), 1f);
            fill.color = amount >= 0.72f ? UiTheme.Fitness : amount >= 0.5f ? UiTheme.FitnessLow : UiTheme.FitnessBad;
        }

        public static void DrawPitch(RectTransform pitch, Color? grassColor = null)
        {
            var grass = Solid(pitch, "Grass", grassColor ?? UiTheme.Pitch, false);
            Stretch(grass.rectTransform);
            var line = new Color(1f, 1f, 1f, 0.88f);
            Rule(pitch, "Border", 0.015f, 0.04f, 0.985f, 0.96f, line, false);
            VLine(pitch, 0.5f, 0.04f, 0.96f, line);
            Box(pitch, 0.015f, 0.22f, 0.16f, 0.78f, line);
            Box(pitch, 0.015f, 0.36f, 0.07f, 0.64f, line);
            Box(pitch, 0.84f, 0.22f, 0.985f, 0.78f, line);
            Box(pitch, 0.93f, 0.36f, 0.985f, 0.64f, line);
            Box(pitch, 0f, 0.4f, 0.03f, 0.6f, line);
            Box(pitch, 0.97f, 0.4f, 1f, 0.6f, line);
            Net(pitch, 0f, 0.4f, 0.03f, 0.6f, new Color(1f, 1f, 1f, 0.55f));
            Net(pitch, 0.97f, 0.4f, 1f, 0.6f, new Color(1f, 1f, 1f, 0.55f));
            Dot(pitch, new Vector2(0.12f, 0.5f), 8f, line);
            Dot(pitch, new Vector2(0.88f, 0.5f), 8f, line);
            var ring = Solid(pitch, "CenterRing", line);
            ring.sprite = Ring;
            ring.type = Image.Type.Simple;
            ring.preserveAspect = true;
            ring.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            ring.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            ring.rectTransform.sizeDelta = new Vector2(92, 92);
            Dot(pitch, new Vector2(0.5f, 0.5f), 8f, line);
        }

        public static void RestoreShapes(Transform root)
        {
            if (root == null)
                return;
            var images = root.GetComponentsInChildren<Image>(true);
            for (var i = 0; i < images.Length; i++)
            {
                var image = images[i];
                if (image == null)
                    continue;
                var name = image.gameObject.name;
                if (name == "CenterRing")
                {
                    image.sprite = Ring;
                    image.type = Image.Type.Simple;
                    image.preserveAspect = true;
                    image.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    image.rectTransform.sizeDelta = new Vector2(92, 92);
                    continue;
                }

                if (name == "Ball" || name == "BallShadow" || name == "Dot" || name == "Pill" || TokenName(name))
                {
                    image.sprite = Circle;
                    image.type = Image.Type.Simple;
                    continue;
                }

                if (image.GetComponent<Button>() != null || (image.sprite == null && image.type == Image.Type.Sliced))
                {
                    image.sprite = Round;
                    image.type = Image.Type.Sliced;
                }
            }

            var brandMark = root.Find("Wordmark/Mark");
            if (brandMark != null)
            {
                var image = brandMark.GetComponent<Image>();
                var raw = brandMark.GetComponent<RawImage>();
                if (raw != null)
                {
                    ApplyBrand(raw);
                    raw.rectTransform.sizeDelta = new Vector2(48f, 48f);
                }
                else
                {
                    if (image != null)
                    {
                        image.enabled = false;
                        image.rectTransform.sizeDelta = new Vector2(48f, 48f);
                    }

                    var logoObject = brandMark.Find("BrandTexture");
                    raw = logoObject == null ? null : logoObject.GetComponent<RawImage>();
                    if (raw == null)
                    {
                        logoObject = brandMark.Find("BrandTextureGraphic");
                        raw = logoObject == null ? null : logoObject.GetComponent<RawImage>();
                    }

                    if (raw == null)
                    {
                        var created = new GameObject("BrandTextureGraphic", typeof(RectTransform), typeof(RawImage));
                        created.transform.SetParent(brandMark, false);
                        logoObject = created.transform;
                        raw = created.GetComponent<RawImage>();
                    }

                    ApplyBrand(raw);
                    var logoRect = raw.rectTransform;
                    logoRect.anchorMin = Vector2.zero;
                    logoRect.anchorMax = Vector2.one;
                    logoRect.offsetMin = Vector2.zero;
                    logoRect.offsetMax = Vector2.zero;
                }

                var oldLetters = brandMark.Find("Mono");
                if (oldLetters != null)
                    oldLetters.gameObject.SetActive(false);
            }
        }

        static bool TokenName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length < 2 || name.Length > 3)
                return false;
            if (name[0] != 'H' && name[0] != 'A')
                return false;
            for (var i = 1; i < name.Length; i++)
            {
                if (name[i] < '0' || name[i] > '9')
                    return false;
            }

            return true;
        }

        public static void Preferred(Component component, float height, float width = -1f, float flexWidth = -1f)
        {
            var le = component.GetComponent<LayoutElement>();
            if (le == null)
                le = component.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            if (width >= 0f)
            {
                le.minWidth = width;
                le.preferredWidth = width;
                le.flexibleWidth = 0f;
            }

            if (flexWidth >= 0f)
                le.flexibleWidth = flexWidth;
        }

        public static void SetWrapped(Text text, string value, float minHeight)
        {
            text.text = value ?? "";
            Canvas.ForceUpdateCanvases();
            var le = text.GetComponent<LayoutElement>();
            if (le == null)
                le = text.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = Mathf.Max(minHeight, text.preferredHeight + 8f);
            le.minHeight = le.preferredHeight;
        }

        public static void Place(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        public static void Stretch(RectTransform rect)
        {
            Place(rect, 0f, 0f, 1f, 1f);
        }

        public static Color Parse(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex))
                return fallback;
            return ColorUtility.TryParseHtmlString(hex, out var color) ? color : fallback;
        }

        public static string ShortName(string full)
        {
            if (string.IsNullOrWhiteSpace(full))
                return "—";
            var parts = full.Trim().Split(' ');
            return parts[parts.Length - 1];
        }

        public static VerticalLayoutGroup Column(Transform parent, string name, int spacing, RectOffset padding)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return layout;
        }

        static void Rule(Transform parent, string name, float x0, float y0, float x1, float y1, Color color, bool fill)
        {
            if (fill)
            {
                var box = Solid(parent, name, color);
                Place(box.rectTransform, x0, y0, x1, y1);
                return;
            }

            VLine(parent, x0, y0, y1, color);
            VLine(parent, x1, y0, y1, color);
            HLine(parent, y0, x0, x1, color);
            HLine(parent, y1, x0, x1, color);
        }

        static void Box(Transform parent, float x0, float y0, float x1, float y1, Color color)
        {
            Rule(parent, "Box", x0, y0, x1, y1, color, false);
        }

        static void Net(Transform parent, float x0, float y0, float x1, float y1, Color color)
        {
            for (var i = 1; i <= 5; i++)
            {
                var x = Mathf.Lerp(x0, x1, i / 6f);
                VLine(parent, x, y0, y1, color);
            }

            for (var i = 1; i <= 4; i++)
            {
                var y = Mathf.Lerp(y0, y1, i / 5f);
                HLine(parent, y, x0, x1, color);
            }
        }

        static void VLine(Transform parent, float x, float y0, float y1, Color color)
        {
            var line = Solid(parent, "V", color);
            Place(line.rectTransform, x - 0.0022f, y0, x + 0.0022f, y1);
        }

        static void HLine(Transform parent, float y, float x0, float x1, Color color)
        {
            var line = Solid(parent, "H", color);
            Place(line.rectTransform, x0, y - 0.004f, x1, y + 0.004f);
        }

        static void Dot(Transform parent, Vector2 anchor, float size, Color color)
        {
            var dot = Solid(parent, "Dot", color);
            dot.sprite = Circle;
            dot.rectTransform.anchorMin = anchor;
            dot.rectTransform.anchorMax = anchor;
            dot.rectTransform.sizeDelta = new Vector2(size, size);
        }

        static Sprite MakeRound()
        {
            const int size = 48;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var radius = 16f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var px = Inside(x, y, size, radius);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, px));
                }
            }

            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static float Inside(int x, int y, int size, float radius)
        {
            var cx = Mathf.Clamp(x, radius, size - 1 - radius);
            var cy = Mathf.Clamp(y, radius, size - 1 - radius);
            var dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            return Mathf.Clamp01(radius - dist + 0.6f);
        }

        static Sprite MakeDisc(bool ring)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha;
                    if (ring)
                        alpha = dist > 22f && dist < 27f ? Mathf.Clamp01(2f - Mathf.Abs(dist - 24.5f)) : 0f;
                    else
                        alpha = Mathf.Clamp01(30.5f - dist);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
