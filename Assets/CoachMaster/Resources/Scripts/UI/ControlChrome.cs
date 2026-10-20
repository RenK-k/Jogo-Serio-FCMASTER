using System.Collections.Generic;
using CoachMaster.App;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    public interface IControlBackHandler
    {
        bool HandleControlBack();
    }

    public sealed class ControlChrome : MonoBehaviour
    {
        public static bool UsingKeyboard { get; private set; } = true;
        readonly List<Selectable> _buttons = new List<Selectable>();
        readonly Dictionary<Selectable, Outline> _focusOutlines = new Dictionary<Selectable, Outline>();
        int _index = -1;
        Text _hint;
        Selectable _focusedButton;
        Vector3 _focusedScale;
        float _nextScan;
        float _nextStickMove;
        Vector2Int _stickDirection;
        bool _navigationActive;
        InputField _keyboardField;

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

        public static void SettingsOpened()
        {
            var chrome = GameRoot.Instance == null ? null : GameRoot.Instance.GetComponent<ControlChrome>();
            if (chrome == null)
                return;
            chrome.ClearFocus();
            chrome._buttons.Clear();
            chrome._index = -1;
            chrome._nextScan = 0f;
            chrome._hint = null;
            if (!UsingKeyboard)
                chrome._navigationActive = true;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        public static void SettingsClosed()
        {
            var chrome = GameRoot.Instance == null ? null : GameRoot.Instance.GetComponent<ControlChrome>();
            if (chrome == null)
                return;
            chrome.ClearFocus();
            chrome._buttons.Clear();
            chrome._index = -1;
            chrome._nextScan = 0f;
            chrome._hint = null;
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
            if (!UsingKeyboard && Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            {
                var gameRoot = GameRoot.Instance;
                if (gameRoot != null)
                    gameRoot.GetComponent<AudioSettingsOverlay>()?.Toggle();
                return;
            }
            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + 0.35f;
                EnsureHint();
                if (_hint != null && _hint.gameObject.activeSelf != SettingsPrefs.ControllerHints)
                    _hint.gameObject.SetActive(SettingsPrefs.ControllerHints);
                RefreshButtons();
            }

            if (_hint != null)
            {
                _hint.text = LocalizationManager.Translate("M · DEFINIÇÕES");
            }

            if (Typing())
            {
                var cancelText = UsingKeyboard
                    ? Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame
                    : Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
                if (cancelText)
                    EndTextEntry();
                return;
            }

            SyncFocus();

            TryMouse();

            var back = UsingKeyboard
                ? Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame
                : Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
            var confirm = UsingKeyboard
                ? Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
                : Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
            var direction = Vector2Int.zero;
            if (UsingKeyboard && Keyboard.current != null)
            {
                if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame) direction.y = 1;
                else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame) direction.y = -1;
                if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame) direction.x = -1;
                else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame) direction.x = 1;
                _stickDirection = Vector2Int.zero;
            }
            else if (!UsingKeyboard && Gamepad.current != null)
            {
                var pad = Gamepad.current;
                if (pad.dpad.up.wasPressedThisFrame) direction.y = 1;
                else if (pad.dpad.down.wasPressedThisFrame) direction.y = -1;
                if (pad.dpad.left.wasPressedThisFrame) direction.x = -1;
                else if (pad.dpad.right.wasPressedThisFrame) direction.x = 1;

                if (direction != Vector2Int.zero)
                {
                    _stickDirection = Vector2Int.zero;
                    _nextStickMove = 0f;
                }
                else
                {
                    var horizontal = pad.leftStick.x.ReadValue();
                    var vertical = pad.leftStick.y.ReadValue();
                    var absX = Mathf.Abs(horizontal);
                    var absY = Mathf.Abs(vertical);
                    // Resolve the dominant stick axis. The old 0.72 ratio let a
                    // nearly diagonal stick lean count as vertical too often,
                    // which made menu navigation feel as if every direction
                    // moved sideways on some controllers.
                    if (absY >= 0.48f && absY > absX)
                        direction.y = vertical > 0f ? 1 : -1;
                    else if (absX >= 0.48f)
                    {
                        direction.x = horizontal > 0f ? 1 : -1;
                    }

                    if (direction == Vector2Int.zero)
                    {
                        _stickDirection = Vector2Int.zero;
                        _nextStickMove = 0f;
                    }
                    else if (direction != _stickDirection)
                    {
                        _stickDirection = direction;
                        _nextStickMove = Time.unscaledTime + 0.2f;
                    }
                    else if (Time.unscaledTime < _nextStickMove)
                        direction = Vector2Int.zero;
                    else
                        _nextStickMove = Time.unscaledTime + 0.14f;
                }
            }

            var volumeAdjusted = direction.x < 0 && AdjustFocusedVolume(-0.05f);
            if (!volumeAdjusted && direction.x > 0)
                volumeAdjusted = AdjustFocusedVolume(0.05f);
            if (!volumeAdjusted)
            {
                if (direction != Vector2Int.zero) Move(direction);
                else if (confirm) PressCurrent();
                else if (back) PressBack();
            }
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
            if (_hint != null && !AudioSettingsOverlay.IsVisible)
                return;
            Canvas canvas = null;
            if (AudioSettingsOverlay.IsVisible && GameRoot.Instance != null)
                canvas = GameRoot.Instance.GetComponent<AudioSettingsOverlay>()?.SettingsCanvas;
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>();
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
            var previousIndex = _index;
            _buttons.Clear();
            Canvas canvas = null;
            if (AudioSettingsOverlay.IsVisible && GameRoot.Instance != null)
                canvas = GameRoot.Instance.GetComponent<AudioSettingsOverlay>()?.SettingsCanvas;
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;
            EnsureControllerTextButtons(canvas);
            foreach (var button in canvas.GetComponentsInChildren<Selectable>(false))
            {
                if (button is InputField || !button.IsActive() || !button.interactable || IsBack(button))
                    continue;
                var navigation = button.navigation;
                if (navigation.mode != Navigation.Mode.None)
                {
                    navigation.mode = Navigation.Mode.None;
                    button.navigation = navigation;
                }
                _buttons.Add(button);
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
                if (_index >= 0 && _index < _buttons.Count && (_index != previousIndex || _buttons[_index] != previous))
                    ScrollFocusedIntoView(_buttons[_index]);
            }
            else
            {
                _index = -1;
                ClearFocus();
            }
        }

        bool AdjustFocusedVolume(float amount)
        {
            if (_index < 0 || _index >= _buttons.Count)
                return false;
            var volume = _buttons[_index].GetComponent<DragVolume>();
            if (volume != null)
                volume.Step(amount);
            else if (_buttons[_index] is Slider slider)
                slider.value = Mathf.Clamp(slider.value + (slider.maxValue - slider.minValue) * amount, slider.minValue, slider.maxValue);
            else if (_buttons[_index] is Dropdown dropdown)
                dropdown.value = Mathf.Clamp(dropdown.value + (amount > 0f ? 1 : -1), 0, dropdown.options.Count - 1);
            else
                return false;
            GameAudio.Click();
            return true;
        }

        int SelectedButtonIndex()
        {
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
                return -1;
            var button = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
            if (button == null)
                button = EventSystem.current.currentSelectedGameObject.GetComponentInParent<Selectable>();
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

        void Move(Vector2Int direction)
        {
            if (_buttons.Count == 0)
                return;
            _navigationActive = true;
            if (_index < 0 || _index >= _buttons.Count)
                _index = ForwardIndex();
            if (_index < 0)
                return;

            var currentRect = _buttons[_index].transform as RectTransform;
            var origin = currentRect == null ? Vector2.zero : RectTransformUtility.WorldToScreenPoint(null, currentRect.TransformPoint(currentRect.rect.center));
            var best = -1;
            var bestScore = float.MaxValue;
            var wrap = -1;
            var wrapScore = float.MaxValue;
            var horizontal = direction.x != 0;
            var sign = horizontal ? direction.x : direction.y;
            for (var i = 0; i < _buttons.Count; i++)
            {
                if (i == _index) continue;
                var candidateRect = _buttons[i].transform as RectTransform;
                if (candidateRect == null) continue;
                var delta = (Vector2)RectTransformUtility.WorldToScreenPoint(null, candidateRect.TransformPoint(candidateRect.rect.center)) - origin;
                var forward = (horizontal ? delta.x : delta.y) * sign;
                var cross = Mathf.Abs(horizontal ? delta.y : delta.x);
                if (forward > 2f)
                {
                    var score = forward + cross * 2.4f + cross * cross / Mathf.Max(24f, forward);
                    if (score < bestScore)
                    {
                        best = i;
                        bestScore = score;
                    }
                }
                else if (forward < -2f)
                {
                    var score = -forward + cross * 3f;
                    if (score < wrapScore)
                    {
                        wrap = i;
                        wrapScore = score;
                    }
                }
            }

            if (best >= 0)
                _index = best;
            else if (wrap >= 0)
                _index = wrap;
            else
                return;
            Paint();
            if (!UsingKeyboard && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(_buttons[_index].gameObject);
            else if (UsingKeyboard && EventSystem.current != null && !Typing())
                EventSystem.current.SetSelectedGameObject(null);
            ScrollFocusedIntoView(_buttons[_index]);
        }

        void PressBack()
        {
            if (AudioSettingsOverlay.IsVisible)
            {
                GameRoot.Instance.GetComponent<AudioSettingsOverlay>()?.CloseFromControl();
                return;
            }

            var selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            var openDropdown = selected == null ? null : selected.GetComponentInParent<Dropdown>();
            var dropdownTemplate = openDropdown == null ? null : openDropdown.transform.Find("Template");
            if (dropdownTemplate != null && dropdownTemplate.gameObject.activeSelf)
            {
                openDropdown.Hide();
                EventSystem.current.SetSelectedGameObject(openDropdown.gameObject);
                return;
            }

            var handlers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            for (var i = 0; i < handlers.Length; i++)
            {
                if (handlers[i] is IControlBackHandler handler && handler.HandleControlBack())
                    return;
            }

            PressNamed("Back");
        }

        static void ScrollFocusedIntoView(Selectable button)
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

            var selected = _buttons[_index];
            if (selected is Button button)
                button.onClick.Invoke();
            else if (selected is Dropdown dropdown)
                dropdown.Show();
            else if (selected is Toggle toggle)
                toggle.isOn = !toggle.isOn;
            else if (selected is InputField field)
                OpenTextEntry(field);
            else
                ExecuteEvents.Execute(selected.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
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

        static bool IsBack(Selectable button)
        {
            var name = button.gameObject.name;
            return name == "Back" || name == "CloseAudio" || name == "FECHAR"
                || name.StartsWith("Close", System.StringComparison.OrdinalIgnoreCase)
                || name == "ModalClose";
        }

        void EnsureControllerTextButtons(Canvas canvas)
        {
            foreach (var field in canvas.GetComponentsInChildren<InputField>(false))
            {
                if (field == null || field.transform.Find("ControllerKeyboard") != null)
                    continue;
                var keyboardButton = BroadcastUi.Button(field.transform, "ControllerKeyboard", "Aa",
                    () => OpenTextEntry(field), new Color(0.22f, 0.12f, 0.38f, 0.98f), Color.white, 11);
                BroadcastUi.Place(keyboardButton.GetComponent<RectTransform>(), 0.86f, 0.08f, 0.99f, 0.92f);
                ReserveTextInputSpace(field.textComponent == null ? null : field.textComponent.rectTransform);
                var placeholder = field.placeholder == null ? null : field.placeholder.transform as RectTransform;
                ReserveTextInputSpace(placeholder);
            }
        }

        static void ReserveTextInputSpace(RectTransform text)
        {
            if (text == null) return;
            var max = text.offsetMax;
            max.x = Mathf.Min(max.x, -46f);
            text.offsetMax = max;
        }

        void OpenTextEntry(InputField field)
        {
            if (field == null) return;
            _keyboardField = field;
            field.Select();
            field.ActivateInputField();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(field.gameObject);
            if (UsingKeyboard)
                return;

            if (TouchScreenKeyboard.isSupported)
                return;

            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
            {
                try
                {
                    if (System.Diagnostics.Process.GetProcessesByName("osk").Length == 0)
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("osk.exe") { UseShellExecute = true });
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning("Could not open the Windows on-screen keyboard: " + exception.Message);
                }
            }
        }

        void EndTextEntry()
        {
            if (_keyboardField == null && EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
                _keyboardField = EventSystem.current.currentSelectedGameObject.GetComponent<InputField>();
            if (_keyboardField != null)
            {
                var keyboardButton = _keyboardField.transform.Find("ControllerKeyboard");
                _keyboardField.DeactivateInputField();
                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(keyboardButton == null ? _keyboardField.gameObject : keyboardButton.gameObject);
            }
            _keyboardField = null;
            _navigationActive = true;
            _nextScan = 0f;
        }

        void PressNamed(string name)
        {
            foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (button != null && button.IsActive() && button.interactable && button.gameObject.name == name)
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
