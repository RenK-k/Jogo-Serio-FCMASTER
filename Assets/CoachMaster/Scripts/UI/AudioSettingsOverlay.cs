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
            if (Keyboard.current == null || !Keyboard.current.mKey.wasPressedThisFrame)
                return;
            if (EventSystemTyping())
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

            var dim = BroadcastUi.Solid(canvasGo.transform, "Dim", new Color(0.03f, 0.01f, 0.08f, 0.72f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            var card = BroadcastUi.Panel(canvasGo.transform, "Card", new Color(0.1f, 0.04f, 0.2f, 0.98f), true);
            BroadcastUi.Place(card.rectTransform, 0.08f, 0.06f, 0.92f, 0.94f);
            var title = BroadcastUi.Label(card.transform, "Title", "MÚSICA", 32, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.04f, 0.9f, 0.5f, 0.98f);
            var close = BroadcastUi.CloseX(card.transform, "CloseAudio", Toggle);
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.9f, 0.9f, 0.97f, 0.98f);

            _now = BroadcastUi.Label(card.transform, "Now", "", 20, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(_now.rectTransform, 0.04f, 0.82f, 0.7f, 0.9f);
            _repeat = BroadcastUi.Button(card.transform, "Repeat", "REPETIR", () =>
            {
                GameAudio.Repeat = !GameAudio.Repeat;
                Refresh();
            }, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 16);
            BroadcastUi.Place(_repeat.GetComponent<RectTransform>(), 0.72f, 0.82f, 0.96f, 0.9f);

            LabelAt(card.transform, "Música", 0.74f);
            _musicSlider = Volume(card.transform, "Music", 0.73f, GameAudio.MusicVolume, GameAudio.SetMusicVolume);
            LabelAt(card.transform, "Efeitos", 0.66f);
            _effectSlider = Volume(card.transform, "Effects", 0.65f, GameAudio.EffectVolume, GameAudio.SetEffectVolume);
            LabelAt(card.transform, "Público", 0.58f);
            _crowdSlider = Volume(card.transform, "Crowd", 0.57f, GameAudio.CrowdVolume, GameAudio.SetCrowdVolume);

            _save = Step(card.transform, "Guardar", ConfirmSave, 0.5f, 0.22f);
            _quit = Step(card.transform, "Sair", LeaveToMenu, 0.74f, 0.22f);
            ApplyCareerButtons();

            Step(card.transform, "Anterior", () => { GameAudio.PreviousTrack(); Refresh(); }, 0.04f, 0.05f, 0.44f, 0.13f);
            Step(card.transform, "Seguinte", () => { GameAudio.NextTrack(); Refresh(); }, 0.52f, 0.05f, 0.44f, 0.13f);
        }

        static void LabelAt(Transform parent, string text, float y)
        {
            var label = BroadcastUi.Label(parent, text, text, 16, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(label.rectTransform, 0.04f, y, 0.24f, y + 0.05f);
        }

        DragVolume Volume(Transform parent, string name, float y, float value, UnityEngine.Events.UnityAction<float> changed)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(DragVolume));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            BroadcastUi.Place(rect, 0.26f, y, 0.96f, y + 0.045f);
            var bg = go.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.16f);
            bg.raycastTarget = true;
            var fill = BroadcastUi.Solid(go.transform, "Fill", UiTheme.Accent);
            fill.raycastTarget = false;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            var handle = BroadcastUi.Solid(go.transform, "Handle", Color.white);
            handle.raycastTarget = false;
            handle.rectTransform.anchorMin = new Vector2(Mathf.Clamp01(value), 0.5f);
            handle.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value), 0.5f);
            handle.rectTransform.sizeDelta = new Vector2(14f, 28f);
            var bar = go.GetComponent<DragVolume>();
            bar.Setup(fill, handle, value, changed);
            return bar;
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
        }

        static bool EventSystemTyping()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null || UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == null)
                return false;
            var field = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<InputField>();
            return field != null && field.isFocused;
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
