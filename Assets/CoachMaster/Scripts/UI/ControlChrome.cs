using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    public sealed class ControlChrome : MonoBehaviour
    {
        public static bool UsingKeyboard { get; private set; }
        readonly List<Button> _buttons = new List<Button>();
        int _index = -1;
        Text _hint;
        float _nextScan;

        void Update()
        {
            DetectDevice();
            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + 0.35f;
                EnsureHint();
                RefreshButtons();
            }

            if (_hint != null)
                _hint.text = UsingKeyboard
                    ? "Rato  Selecionar     Enter  Avançar     Esc  Voltar     M  Música"
                    : "A  Selecionar     B  Voltar     Direção  Navegar";

            if (Typing())
                return;

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

            if (prev) Move(-1);
            else if (next) Move(1);
            else if (confirm) PressCurrent();
            else if (back) PressNamed("Back");
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

        static void DetectDevice()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                UsingKeyboard = true;
            var pad = Gamepad.current;
            if (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame || pad.dpad.up.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame || pad.leftStick.ReadValue().sqrMagnitude > 0.5f))
                UsingKeyboard = false;
            else if (pad == null && Keyboard.current != null)
                UsingKeyboard = true;
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
                if (text.gameObject.name == "Hint" || text.gameObject.name == "ControlHint")
                {
                    _hint = text;
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
            _hint.color = UiTheme.Muted;
            _hint.raycastTarget = false;
        }

        void RefreshButtons()
        {
            _buttons.Clear();
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;
            foreach (var button in canvas.GetComponentsInChildren<Button>(false))
            {
                if (button.IsActive() && button.interactable)
                    _buttons.Add(button);
            }

            if (_index >= _buttons.Count)
                _index = _buttons.Count - 1;
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
            _index = (_index + delta + _buttons.Count) % _buttons.Count;
            Paint();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(_buttons[_index].gameObject);
        }

        void PressCurrent()
        {
            if (_index < 0 || _index >= _buttons.Count || IsBack(_buttons[_index]))
            {
                var pick = ForwardIndex();
                if (pick < 0)
                    return;
                _index = pick;
                Paint();
                _buttons[pick].onClick.Invoke();
                return;
            }

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
            for (var i = 0; i < _buttons.Count; i++)
            {
                var colors = _buttons[i].colors;
                colors.normalColor = i == _index ? UiTheme.Accent : Color.white;
                _buttons[i].colors = colors;
            }
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
