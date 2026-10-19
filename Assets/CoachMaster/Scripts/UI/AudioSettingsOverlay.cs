using CoachMaster.App;
using CoachMaster.Career;
using CoachMaster.UI.Scenes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    public sealed class AudioSettingsOverlay : MonoBehaviour
    {
        GameObject _root;
        Text _now;

        DragVolume _musicSlider;
        DragVolume _effectSlider;
        DragVolume _crowdSlider;
        Button _repeat;

        Button _save;
        Button _quit;
        Button _window;
        Button _vsync;
        Button _quality;
        Button _fps;
        Button _background;
        float _savedUntil;
        Image _saveImage;

        void Update()
        {
            GameAudio.Tick();
            if (_saveImage != null && _savedUntil > 0f && Time.unscaledTime >= _savedUntil)
            {
                _saveImage.color = new Color(0.22f, 0.12f, 0.38f, 1f);
                _savedUntil = 0f;
            }
            if (Keyboard.current == null)
                return;

            if (!EventSystemTyping() && Keyboard.current.f11Key.wasPressedThisFrame)
            {
                DisplayPrefs.ToggleFullscreen();
                if (_root != null && _root.activeSelf)
                    Refresh();
            }

            if (!Keyboard.current.mKey.wasPressedThisFrame || EventSystemTyping())
                return;
            Toggle();
        }

        public void Toggle()
        {
            if (_root != null && _root.activeSelf)
            {
                _root.SetActive(false);
                return;
            }

            if (_root == null)
                Build();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            ApplyCareerButtons();
            Refresh();
        }

        void Build()
        {
            var canvasGo = new GameObject("AudioSettings", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            _root = canvasGo;

            var dim = BroadcastUi.Solid(canvasGo.transform, "Dim", new Color(0.03f, 0.01f, 0.08f, 0.94f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            var card = BroadcastUi.Panel(canvasGo.transform, "Card", new Color(0.1f, 0.04f, 0.2f, 1f), true);
            BroadcastUi.Place(card.rectTransform, 0.08f, 0.08f, 0.92f, 0.92f);
            var title = BroadcastUi.Label(card.transform, "Title", "DEFINIÇÕES", 32, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.04f, 0.9f, 0.5f, 0.98f);
            var close = BroadcastUi.CloseX(card.transform, "CloseAudio", Toggle);
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.9f, 0.9f, 0.97f, 0.98f);

            var sound = BroadcastUi.Panel(card.transform, "Sound", new Color(1f, 1f, 1f, 0.05f));
            BroadcastUi.Place(sound.rectTransform, 0.03f, 0.16f, 0.49f, 0.88f);
            var soundHead = BroadcastUi.Label(sound.transform, "Head", "SOM E MÚSICA", 18, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(soundHead.rectTransform, 0.06f, 0.88f, 0.94f, 0.98f);
            _now = BroadcastUi.Label(sound.transform, "Now", "", 16, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            _now.horizontalOverflow = HorizontalWrapMode.Wrap;
            _now.verticalOverflow = VerticalWrapMode.Truncate;
            BroadcastUi.Place(_now.rectTransform, 0.06f, 0.74f, 0.94f, 0.86f);
            _repeat = BroadcastUi.Button(sound.transform, "Repeat", "REPETIR", () =>
            {
                GameAudio.Repeat = !GameAudio.Repeat;
                Refresh();
            }, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 16);
            BroadcastUi.Place(_repeat.GetComponent<RectTransform>(), 0.06f, 0.62f, 0.94f, 0.72f);
            Caption(sound.transform, "Música", 0.54f);
            _musicSlider = VolumeWide(sound.transform, "Music", 0.46f, GameAudio.MusicVolume, GameAudio.SetMusicVolume);
            Caption(sound.transform, "Efeitos", 0.38f);
            _effectSlider = VolumeWide(sound.transform, "Effects", 0.3f, GameAudio.EffectVolume, GameAudio.SetEffectVolume);
            Caption(sound.transform, "Público", 0.22f);
            _crowdSlider = VolumeWide(sound.transform, "Crowd", 0.14f, GameAudio.CrowdVolume, GameAudio.SetCrowdVolume);
            Step(sound.transform, "Anterior", () => { GameAudio.PreviousTrack(); Refresh(); }, 0.06f, 0.02f, 0.42f, 0.1f);
            Step(sound.transform, "Seguinte", () => { GameAudio.NextTrack(); Refresh(); }, 0.52f, 0.02f, 0.42f, 0.1f);

            var screen = BroadcastUi.Panel(card.transform, "ScreenBox", new Color(1f, 1f, 1f, 0.05f));
            BroadcastUi.Place(screen.rectTransform, 0.51f, 0.16f, 0.97f, 0.88f);
            var screenHead = BroadcastUi.Label(screen.transform, "Head", "IMAGEM E DESEMPENHO", 18, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(screenHead.rectTransform, 0.06f, 0.88f, 0.94f, 0.98f);
            var hint = BroadcastUi.Label(screen.transform, "Hint", "F11 alterna entre janela e ecrã inteiro. Escolhe o fundo abaixo.", 15, UiTheme.Muted, TextAnchor.UpperLeft);
            BroadcastUi.Place(hint.rectTransform, 0.06f, 0.74f, 0.94f, 0.86f);
            _window = Step(screen.transform, "Janela", () => { DisplayPrefs.ToggleFullscreen(); Refresh(); }, 0.06f, 0.6f, 0.88f, 0.12f);
            Step(screen.transform, "1280x720", () => { DisplayPrefs.SetSize(1280, 720); Refresh(); }, 0.06f, 0.46f, 0.28f, 0.12f);
            Step(screen.transform, "1600x900", () => { DisplayPrefs.SetSize(1600, 900); Refresh(); }, 0.36f, 0.46f, 0.28f, 0.12f);
            Step(screen.transform, "1920x1080", () => { DisplayPrefs.SetSize(1920, 1080); Refresh(); }, 0.66f, 0.46f, 0.28f, 0.12f);
            _vsync = Step(screen.transform, "VSync", () => { DisplayPrefs.ToggleVsync(); Refresh(); }, 0.06f, 0.3f, 0.88f, 0.12f);
            _quality = Step(screen.transform, "Qualidade", () => { DisplayPrefs.CycleQuality(); Refresh(); }, 0.06f, 0.14f, 0.43f, 0.12f);
            _fps = Step(screen.transform, "Limite FPS", () => { DisplayPrefs.CycleFrameRate(); Refresh(); }, 0.51f, 0.14f, 0.43f, 0.12f);
            _background = Step(screen.transform, "Fundo", () => { BackgroundPrefs.Cycle(); Refresh(); }, 0.06f, 0.015f, 0.88f, 0.105f);

            _save = Step(card.transform, "Guardar", ConfirmSave, 0.03f, 0.03f, 0.46f, 0.1f);
            _quit = Step(card.transform, "Sair", LeaveToMenu, 0.51f, 0.03f, 0.46f, 0.1f);
            ApplyCareerButtons();
        }

        static void Caption(Transform parent, string text, float y)
        {
            var label = BroadcastUi.Label(parent, text, text, 15, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(label.rectTransform, 0.06f, y, 0.94f, y + 0.07f);
        }

        DragVolume VolumeWide(Transform parent, string name, float y, float value, UnityEngine.Events.UnityAction<float> changed)
        {
            var background = BroadcastUi.Solid(parent, name, new Color(1f, 1f, 1f, 0.16f));
            background.sprite = BroadcastUi.Round;
            background.type = Image.Type.Sliced;
            background.raycastTarget = true;
            BroadcastUi.Place(background.rectTransform, 0.06f, y, 0.94f, y + 0.055f);

            var fill = BroadcastUi.Solid(background.transform, "Fill", UiTheme.Accent);
            fill.sprite = BroadcastUi.Round;
            fill.type = Image.Type.Sliced;
            fill.raycastTarget = false;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            var slider = background.gameObject.AddComponent<DragVolume>();
            slider.Setup(fill, null, value, changed);
            return slider;
        }

        void ConfirmSave()
        {
            var career = GameRoot.Instance == null ? null : GameRoot.Instance.Session.Career;
            CareerStore.Save(career);
            if (_save == null)
                return;
            _saveImage = _save.GetComponent<Image>();
            if (_saveImage != null)
                _saveImage.color = new Color(0.15f, 0.72f, 0.32f, 1f);
            _savedUntil = Time.unscaledTime + 1f;
        }

        void LeaveToMenu()
        {
            var career = GameRoot.Instance == null ? null : GameRoot.Instance.Session.Career;
            CareerStore.Save(career);
            if (_root != null)
                _root.SetActive(false);
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == SceneNames.MainMenu)
            {
                MainMenuController.LeaveGame();
                return;
            }

            GameRoot.Instance.Open(SceneNames.MainMenu);
        }

        void ApplyCareerButtons()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            var inCareer = scene == SceneNames.CareerHub || scene == SceneNames.MatchDay;
            if (_save != null)
                _save.gameObject.SetActive(inCareer);
            if (_quit != null)
                _quit.gameObject.SetActive(inCareer);
        }

        static Button Step(Transform parent, string label, UnityEngine.Events.UnityAction action, float x, float y, float width = 0.2f, float height = 0.09f)
        {
            var button = BroadcastUi.Button(parent, label, label, null, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 18);
            button.onClick.AddListener(() =>
            {
                if (!BroadcastUi.AllowClick())
                    return;
                action();
            });
            BroadcastUi.Place(button.GetComponent<RectTransform>(), x, y, x + width, y + height);
            return button;
        }

        void Refresh()
        {
            if (_now == null)
                return;
            _now.text = GameAudio.Repeat ? "A repetir   " + GameAudio.CurrentTrackName() : "A tocar   " + GameAudio.CurrentTrackName();
            if (_repeat != null)
            {
                _repeat.GetComponent<Image>().color = GameAudio.Repeat ? UiTheme.Accent : new Color(0.22f, 0.12f, 0.38f, 1f);
                _repeat.GetComponentInChildren<Text>().color = GameAudio.Repeat ? UiTheme.ButtonText : Color.white;
            }
            if (_musicSlider != null)
                _musicSlider.Set(GameAudio.MusicVolume);
            if (_effectSlider != null)
                _effectSlider.Set(GameAudio.EffectVolume);
            if (_crowdSlider != null)
                _crowdSlider.Set(GameAudio.CrowdVolume);
            SetCaption(_window, DisplayPrefs.WindowLabel);
            SetCaption(_vsync, DisplayPrefs.VsyncLabel);
            SetCaption(_quality, DisplayPrefs.QualityLabel);
            SetCaption(_fps, DisplayPrefs.FrameRateLabel);
            SetCaption(_background, BackgroundPrefs.Label);
        }

        static bool EventSystemTyping()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null || UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == null)
                return false;
            var selected = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            return selected.GetComponentInParent<InputField>() != null;
        }

        void SetCaption(Button button, string caption)
        {
            if (button == null)
                return;
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.text = caption;
        }
    }

    public sealed class DragVolume : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        Image _fill;
        Image _handle;
        UnityEngine.Events.UnityAction<float> _changed;

        public void Setup(Image fill, Image handle, float value, UnityEngine.Events.UnityAction<float> changed)
        {
            _fill = fill;
            _handle = handle;
            _changed = changed;
            Set(value);
        }

        public void Set(float value)
        {
            value = Mathf.Clamp01(value);
            if (_fill != null)
                _fill.rectTransform.anchorMax = new Vector2(value, 1f);
            if (_handle != null)
            {
                _handle.rectTransform.anchorMin = new Vector2(value, 0.5f);
                _handle.rectTransform.anchorMax = new Vector2(value, 0.5f);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Apply(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Apply(eventData);
        }

        void Apply(PointerEventData eventData)
        {
            var rect = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out var local))
                return;
            var width = rect.rect.width;
            if (width < 1f)
                return;
            var value = Mathf.Clamp01((local.x - rect.rect.xMin) / width);
            Set(value);
            if (_changed != null)
                _changed.Invoke(value);
        }
    }
}
