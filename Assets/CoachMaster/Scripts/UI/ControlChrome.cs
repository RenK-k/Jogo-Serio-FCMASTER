using System.Collections.Generic;
using CoachMaster.App;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    public sealed class ControlChrome : MonoBehaviour
    {
        public static bool UsingKeyboard { get; private set; } = true;
        readonly List<Button> _buttons = new List<Button>();
        readonly Dictionary<Button, Outline> _focusOutlines = new Dictionary<Button, Outline>();
        int _index = -1;
        Text _hint;
        Button _focusedButton;
        Vector3 _focusedScale;
        float _nextScan;
        float _nextStickMove;
        int _stickDirection;
        bool _navigationActive;

        public static void ResetForScene()
        {
            var chrome = GameRoot.Instance == null ? null : GameRoot.Instance.GetComponent<ControlChrome>();
            if (chrome == null)
                return;
            chrome.ClearFocus();
            chrome._index = -1;
            chrome._buttons.Clear();
            chrome._navigationActive = false;
            chrome._nextScan = 0f;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        public static void DisableAutomaticNavigation()
        {
            var buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                    continue;
                var navigation = buttons[i].navigation;
                if (navigation.mode == Navigation.Mode.None)
                    continue;
                navigation.mode = Navigation.Mode.None;
                buttons[i].navigation = navigation;
            }
        }

        void Update()
        {
            if (GameRoot.IsLoading)
            {
                if (_hint != null)
                    _hint.gameObject.SetActive(false);
                return;
            }

            DetectDevice();
            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + 0.35f;
                EnsureHint();
                if (_hint != null && !_hint.gameObject.activeSelf)
                    _hint.gameObject.SetActive(true);
                RefreshButtons();
            }

            if (_hint != null)
                _hint.text = UsingKeyboard
                    ? "RATO / ↑ ↓  NAVEGAR     ENTER  ESCOLHER     ESC  VOLTAR     1–3  RESPONDER"
                    : "D-PAD / ANALÓGICO ESQUERDO  NAVEGAR     A  ESCOLHER     B  VOLTAR";

            if (Typing())
                return;

            SyncFocus();

            TryMouse();

            var back = UsingKeyboard
                ? Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame
                : Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
            var confirm = UsingKeyboard
                ? Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
                : Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
            var prev = UsingKeyboard
                ? Keyboard.current != null && (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
                : Gamepad.current != null && (Gamepad.current.dpad.up.wasPressedThisFrame || Gamepad.current.dpad.left.wasPressedThisFrame);
            var next = UsingKeyboard
                ? Keyboard.current != null && (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
                : Gamepad.current != null && (Gamepad.current.dpad.down.wasPressedThisFrame || Gamepad.current.dpad.right.wasPressedThisFrame);

            var stickMove = 0;
            if (prev || next)
                _stickDirection = 0;
            else if (!UsingKeyboard && Gamepad.current != null)
            {
                var stick = Gamepad.current.leftStick.ReadValue();
                var axis = Mathf.Abs(stick.y) >= Mathf.Abs(stick.x) ? stick.y : stick.x;
                var direction = Mathf.Abs(axis) >= 0.48f ? (axis > 0f ? -1 : 1) : 0;
                if (direction == 0)
                {
                    _stickDirection = 0;
                    _nextStickMove = 0f;
                }
                else if (direction != _stickDirection)
                {
                    _stickDirection = direction;
                    _nextStickMove = Time.unscaledTime + 0.18f;
                    stickMove = direction;
                }
                else if (Time.unscaledTime >= _nextStickMove)
                {
                    _nextStickMove = Time.unscaledTime + 0.13f;
                    stickMove = direction;
                }
            }

            if (prev) Move(-1);
            else if (next) Move(1);
            else if (stickMove != 0) Move(stickMove);
            else if (confirm) PressCurrent();
            else if (back) PressNamed("Back");
        }

        void LateUpdate()
        {
            if (!UsingKeyboard || EventSystem.current == null || Typing())
                return;
            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected != null && (selected.GetComponent<Button>() != null || selected.GetComponentInParent<Button>() != null))
                EventSystem.current.SetSelectedGameObject(null);
        }

        public static bool Digit(int number)
        {
            if (!UsingKeyboard || Keyboard.current == null)
                return false;
            if (number == 1) return Keyboard.current.digit1Key.wasPressedThisFrame;
            if (number == 2) return Keyboard.current.digit2Key.wasPressedThisFrame;
            if (number == 3) return Keyboard.current.digit3Key.wasPressedThisFrame;
            return false;
        }

        void DetectDevice()
        {
            var keyboardInput = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
            var mouse = Mouse.current;
            var mouseInput = mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.delta.ReadValue().sqrMagnitude > 25f);
            var pad = Gamepad.current;
            var padInput = pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame || pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame || pad.dpad.up.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame || pad.dpad.left.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame || pad.leftStick.ReadValue().sqrMagnitude > 0.23f || pad.rightStick.ReadValue().sqrMagnitude > 0.5f);

            if (padInput)
            {
                if (UsingKeyboard)
                {
                    ClearFocus();
                    _index = -1;
                    _nextScan = 0f;
                }
                UsingKeyboard = false;
                _navigationActive = true;
            }
            else if (keyboardInput || mouseInput)
            {
                var switchedFromPad = !UsingKeyboard;
                UsingKeyboard = true;
                if (switchedFromPad || mouseInput)
                {
                    ClearFocus();
                    _index = -1;
                    _navigationActive = false;
                }
                if (mouseInput && EventSystem.current != null && !Typing())
                    EventSystem.current.SetSelectedGameObject(null);
            }
        }

        void EnsureHint()
        {
            if (_hint != null)
                return;
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;
            foreach (var text in canvas.GetComponentsInChildren<Text>(true))
            {
                if (text.gameObject.name == "ControlHint" && text.gameObject.activeInHierarchy)
                {
                    _hint = text;
                    _hint.color = UiTheme.Accent;
                    _hint.fontStyle = FontStyle.Bold;
                    _hint.raycastTarget = false;
                    return;
                }
            }

            var go = new GameObject("ControlHint", typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, 36f);
            rect.anchoredPosition = new Vector2(0f, 8f);
            _hint = go.GetComponent<Text>();
            _hint.font = UiTheme.Font;
            _hint.fontSize = 16;
            _hint.alignment = TextAnchor.MiddleCenter;
            _hint.color = UiTheme.Accent;
            _hint.fontStyle = FontStyle.Bold;
            _hint.raycastTarget = false;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = false;
        }

        void RefreshButtons()
        {
            var previous = _focusedButton;
            _buttons.Clear();
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;
            foreach (var button in canvas.GetComponentsInChildren<Button>(false))
            {
                if (button.IsActive() && button.interactable)
                {
                    var navigation = button.navigation;
                    if (navigation.mode != Navigation.Mode.None)
                    {
                        navigation.mode = Navigation.Mode.None;
                        button.navigation = navigation;
                    }
                    _buttons.Add(button);
                }
            }

            if (_index >= _buttons.Count)
                _index = _buttons.Count - 1;

            if (previous != null && !_buttons.Contains(previous))
                ClearFocus();

            var selectedIndex = UsingKeyboard ? -1 : SelectedButtonIndex();
            if (selectedIndex >= 0)
                _index = selectedIndex;
            else if (_navigationActive && _index < 0 && _buttons.Count > 0)
            {
                _index = ForwardIndex();
                if (_index >= 0 && EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(_buttons[_index].gameObject);
            }

            if (_navigationActive)
            {
                Paint();
                if (_index >= 0 && _index < _buttons.Count)
                    ScrollFocusedIntoView(_buttons[_index]);
            }
            else
            {
                _index = -1;
                ClearFocus();
            }
        }

        int SelectedButtonIndex()
        {
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
                return -1;
            var button = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
            if (button == null)
                button = EventSystem.current.currentSelectedGameObject.GetComponentInParent<Button>();
            return button == null ? -1 : _buttons.IndexOf(button);
        }

        void SyncFocus()
        {
            if (UsingKeyboard || !_navigationActive)
                return;
            var selectedIndex = SelectedButtonIndex();
            if (selectedIndex < 0 || selectedIndex == _index)
                return;
            _index = selectedIndex;
            Paint();
            if (_index >= 0 && _index < _buttons.Count)
                ScrollFocusedIntoView(_buttons[_index]);
        }

        DragVolume _drag;

        void TryMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;
            var screen = mouse.position.ReadValue();

            if (_drag != null)
            {
                if (EventSystem.current != null)
                {
                    var dragData = new PointerEventData(EventSystem.current);
                    dragData.position = screen;
                    if (mouse.leftButton.isPressed)
                        _drag.OnDrag(dragData);
                }

                if (mouse.leftButton.wasReleasedThisFrame)
                    _drag = null;
                return;
            }

            if (!mouse.leftButton.wasPressedThisFrame)
                return;

            if (EventSystem.current == null)
                return;
            var data = new PointerEventData(EventSystem.current);
            data.position = screen;
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            for (var i = 0; i < hits.Count; i++)
            {
                var drag = hits[i].gameObject.GetComponentInParent<DragVolume>();
                if (drag == null)
                    continue;
                _drag = drag;
                drag.OnPointerDown(data);
                return;
            }
        }

        void Move(int delta)
        {
            if (_buttons.Count == 0)
                return;
            _navigationActive = true;
            _index = (_index + delta + _buttons.Count) % _buttons.Count;
            Paint();
            if (!UsingKeyboard && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(_buttons[_index].gameObject);
            else if (UsingKeyboard && EventSystem.current != null && !Typing())
                EventSystem.current.SetSelectedGameObject(null);
            ScrollFocusedIntoView(_buttons[_index]);
        }

        static void ScrollFocusedIntoView(Button button)
        {
            if (button == null)
                return;
            var scroll = button.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.content == null || !button.transform.IsChildOf(scroll.content))
                return;
            var viewport = scroll.viewport != null ? scroll.viewport : scroll.transform as RectTransform;
            if (viewport == null)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            Canvas.ForceUpdateCanvases();
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, button.transform);
            var view = viewport.rect;
            var content = scroll.content.rect;
            if (scroll.vertical && content.height > view.height + 1f)
            {
                var range = content.height - view.height;
                var position = scroll.verticalNormalizedPosition;
                if (bounds.max.y > view.yMax)
                    position += (bounds.max.y - view.yMax) / range;
                else if (bounds.min.y < view.yMin)
                    position += (bounds.min.y - view.yMin) / range;
                scroll.verticalNormalizedPosition = Mathf.Clamp01(position);
            }

            if (scroll.horizontal && content.width > view.width + 1f)
            {
                var range = content.width - view.width;
                var position = scroll.horizontalNormalizedPosition;
                if (bounds.min.x < view.xMin)
                    position += (bounds.min.x - view.xMin) / range;
                else if (bounds.max.x > view.xMax)
                    position += (bounds.max.x - view.xMax) / range;
                scroll.horizontalNormalizedPosition = Mathf.Clamp01(position);
            }
        }

        void PressCurrent()
        {
            _navigationActive = true;
            if (_index < 0 && _buttons.Count == 0)
                RefreshButtons();
            if (_index < 0 && _buttons.Count > 0)
            {
                _index = ForwardIndex();
                Paint();
            }
            if (_index < 0 || _index >= _buttons.Count)
                return;

            _buttons[_index].onClick.Invoke();
        }

        int ForwardIndex()
        {
            var prefer = new[] { "NovaCarreira", "Continuar", "Confirm", "Choose", "Select", "PlayMatch" };
            foreach (var name in prefer)
            {
                for (var i = 0; i < _buttons.Count; i++)
                {
                    if (_buttons[i].gameObject.name == name)
                        return i;
                }
            }

            for (var i = 0; i < _buttons.Count; i++)
            {
                if (!IsBack(_buttons[i]))
                    return i;
            }

            return -1;
        }

        static bool IsBack(Button button)
        {
            var name = button.gameObject.name;
            return name == "Back" || name == "CloseAudio" || name == "FECHAR";
        }

        void PressNamed(string name)
        {
            foreach (var button in _buttons)
            {
                if (button.gameObject.name == name)
                {
                    button.onClick.Invoke();
                    return;
                }
            }
        }

        void Paint()
        {
            if (_index < 0 || _index >= _buttons.Count)
            {
                ClearFocus();
                return;
            }

            var next = _buttons[_index];
            if (next == _focusedButton)
                return;
            ClearFocus();
            _focusedButton = next;
            _focusedScale = next.transform.localScale;
            next.transform.localScale = _focusedScale * 1.035f;

            var graphic = next.targetGraphic != null ? next.targetGraphic : next.GetComponent<Graphic>();
            if (graphic == null)
                return;
            if (!_focusOutlines.TryGetValue(next, out var outline) || outline == null)
            {
                outline = graphic.GetComponent<Outline>();
                if (outline == null)
                    outline = graphic.gameObject.AddComponent<Outline>();
                _focusOutlines[next] = outline;
            }
            outline.effectColor = UiTheme.Accent;
            outline.effectDistance = new Vector2(4f, -4f);
            outline.useGraphicAlpha = false;
            outline.enabled = true;
        }

        void ClearFocus()
        {
            if (_focusedButton == null)
                return;
            if (_focusOutlines.TryGetValue(_focusedButton, out var outline) && outline != null)
                outline.enabled = false;
            _focusedButton.transform.localScale = _focusedScale;
            _focusedButton = null;
        }

        static bool Typing()
        {
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
                return false;
            var field = EventSystem.current.currentSelectedGameObject.GetComponent<InputField>();
            return field != null && field.isFocused;
        }
    }
}
