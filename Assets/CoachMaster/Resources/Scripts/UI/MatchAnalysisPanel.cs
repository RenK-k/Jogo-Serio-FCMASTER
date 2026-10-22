using System.Collections.Generic;
using CoachMaster.MatchPlay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    public sealed class MatchAnalysisPanel : MonoBehaviour
    {
        enum ViewMode { Shots, Momentum, Passing, Heatmap, AveragePositions }

        MatchSession _match;
        RectTransform _visual;
        Text _summary;
        Button[] _views;
        Dropdown _teamFilter;
        Dropdown _periodFilter;
        Dropdown _playerFilter;
        Button _backToResult;
        Button _closeButton;
        Image _backdrop;
        Text _windowTitle;
        RectTransform _cardRect;
        RectTransform _parentRect;
        MatchAnalysisWindowDrag _dragHandle;
        readonly List<MatchAnalysisWindowResize> _resizeHandles = new List<MatchAnalysisWindowResize>();
        MatchAnalysisDismiss _dismissLayer;
        readonly List<string> _playerOptionIds = new List<string>();
        System.Action _returnToResult;
        System.Action _dismissAction;
        ViewMode _mode;
        int _team;
        int _period;
        string _playerId;
        bool _visible;
        bool _finalMode;
        bool _modeInitialized;
        int _languageStamp = -1;
        int _dataStamp = -1;
        float _nextDataCheckTime;
        bool _dirty = true;

        public static MatchAnalysisPanel Attach(Transform parent, MatchSession match)
        {
            if (parent == null)
                return null;
            var panel = parent.GetComponent<MatchAnalysisPanel>();
            if (panel == null)
                panel = parent.gameObject.AddComponent<MatchAnalysisPanel>();
            panel._match = match;
            panel.Build(parent);
            panel._dirty = true;
            return panel;
        }

        void Build(Transform parent)
        {
            if (_visual != null)
                return;
            _parentRect = parent as RectTransform;
            _backdrop = BroadcastUi.Solid(parent, "MatchAnalysisBackdrop", new Color(0.015f, 0.01f, 0.045f, 0.88f), true);
            BroadcastUi.Stretch(_backdrop.rectTransform);
            _backdrop.gameObject.SetActive(false);
            _dismissLayer = _backdrop.gameObject.AddComponent<MatchAnalysisDismiss>();
            _dismissLayer.Initialize(() =>
            {
                if (_dismissAction != null) _dismissAction();
                else SetVisible(false);
            });
            var card = BroadcastUi.Panel(parent, "MatchAnalysis", new Color(0.07f, 0.035f, 0.15f, 0.96f));
            _cardRect = card.rectTransform;
            _cardRect.anchorMin = _cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            _cardRect.pivot = new Vector2(0.5f, 0.5f);
            var english = LocalizationManager.Current == GameLanguage.English;
            var modeLabels = english
                ? new[] { "SHOTS", "MOMENTUM", "PASSES", "HEATMAP", "AVG POSITIONS" }
                : new[] { "REMATES", "MOMENTO", "PASSES", "MAPA DE CALOR", "POSIÇÕES MÉDIAS" };
            _views = new Button[modeLabels.Length];
            for (var i = 0; i < modeLabels.Length; i++)
            {
                var index = i;
                _views[i] = BroadcastUi.Button(card.transform, "View" + i, modeLabels[i], () =>
                {
                    _mode = (ViewMode)index;
                    _dirty = true;
                    Refresh();
                }, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 10);
                var x0 = 0.025f + i * 0.194f;
                BroadcastUi.Place(_views[i].GetComponent<RectTransform>(), x0, 0.83f, x0 + 0.184f, 0.91f);
            }

            _teamFilter = CreateDropdown(card.transform, "TeamFilter", 0.025f, 0.745f, 0.33f, 0.82f, TeamOptions(), OnTeamSelected);
            _periodFilter = CreateDropdown(card.transform, "PeriodFilter", 0.345f, 0.745f, 0.67f, 0.82f, PeriodOptions(), OnPeriodSelected);
            _playerFilter = CreateDropdown(card.transform, "PlayerFilter", 0.685f, 0.745f, 0.975f, 0.82f, PlayerOptions(), OnPlayerSelected);
            _summary = BroadcastUi.Label(card.transform, "Summary", "", 10, UiTheme.Muted, TextAnchor.MiddleCenter);
            BroadcastUi.Place(_summary.rectTransform, 0.025f, 0.67f, 0.975f, 0.735f);
            _backToResult = BroadcastUi.Button(card.transform, "BackToResult", english ? "BACK TO RESULT" : "VOLTAR AO RESULTADO", () => _returnToResult?.Invoke(), new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 10);
            BroadcastUi.Place(_backToResult.GetComponent<RectTransform>(), 0.64f, 0.025f, 0.94f, 0.09f);
            _backToResult.gameObject.SetActive(false);
            var visual = BroadcastUi.Zone(card.transform, "Visual", 0.035f, 0.11f, 0.965f, 0.66f);
            _visual = visual.GetComponent<RectTransform>();
            var header = BroadcastUi.Solid(card.transform, "WindowHeader", new Color(0.04f, 0.02f, 0.1f, 1f), true);
            BroadcastUi.Place(header.rectTransform, 0.01f, 0.92f, 0.99f, 0.995f);
            _windowTitle = BroadcastUi.Label(card.transform, "WindowTitle", english ? "MATCH ANALYSIS" : "ANÁLISE DO JOGO", 14, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            _windowTitle.raycastTarget = false;
            BroadcastUi.Place(_windowTitle.rectTransform, 0.035f, 0.925f, 0.75f, 0.99f);
            _closeButton = BroadcastUi.Button(card.transform, "CloseMatchAnalysis", "×", () =>
            {
                if (_dismissAction != null) _dismissAction();
                else SetVisible(false);
            }, new Color(1f, 1f, 1f, 0.12f), Color.white, 18);
            BroadcastUi.Place(_closeButton.GetComponent<RectTransform>(), 0.94f, 0.925f, 0.99f, 0.99f);
            _dragHandle = header.gameObject.AddComponent<MatchAnalysisWindowDrag>();
            _dragHandle.Initialize(_cardRect, _parentRect);
            AddResizeHandle(card.transform, "ResizeLeft", new Vector2(0f, 0.04f), new Vector2(0f, 0.96f), new Vector2(18f, 0f), true, false, false, false);
            AddResizeHandle(card.transform, "ResizeRight", new Vector2(1f, 0.04f), new Vector2(1f, 0.96f), new Vector2(18f, 0f), false, true, false, false);
            AddResizeHandle(card.transform, "ResizeBottom", new Vector2(0.04f, 0f), new Vector2(0.96f, 0f), new Vector2(0f, 18f), false, false, true, false);
            AddResizeHandle(card.transform, "ResizeTop", new Vector2(0.04f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, 18f), false, false, false, true);
            AddResizeHandle(card.transform, "ResizeBottomLeft", Vector2.zero, Vector2.zero, new Vector2(28f, 28f), true, false, true, false);
            AddResizeHandle(card.transform, "ResizeBottomRight", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(28f, 28f), false, true, true, false);
            AddResizeHandle(card.transform, "ResizeTopLeft", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, 28f), true, false, false, true);
            AddResizeHandle(card.transform, "ResizeTopRight", Vector2.one, Vector2.one, new Vector2(28f, 28f), false, true, false, true);
            _closeButton.transform.SetAsLastSibling();
            SetFinalMode(false);
            SetVisible(false);
            RefreshControls();
        }

        void AddResizeHandle(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 sizeDelta, bool left, bool right, bool bottom, bool top)
        {
            var handleImage = BroadcastUi.Solid(parent, name, new Color(0f, 0f, 0f, 0f), true);
            var rect = handleImage.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;
            var resize = handleImage.gameObject.AddComponent<MatchAnalysisWindowResize>();
            resize.Initialize(_cardRect, _parentRect, left, right, bottom, top);
            _resizeHandles.Add(resize);
        }

        Dropdown CreateDropdown(Transform parent, string name, float x0, float y0, float x1, float y1,
            List<string> options, System.Action<int> changed)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Dropdown));
            root.transform.SetParent(parent, false);
            var image = root.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.1f);
            var rect = root.GetComponent<RectTransform>();
            BroadcastUi.Place(rect, x0, y0, x1, y1);
            var dropdown = root.GetComponent<Dropdown>();
            dropdown.transition = Selectable.Transition.None;
            dropdown.targetGraphic = image;
            var caption = BroadcastUi.Label(root.transform, "Label", "", 10, Color.white, TextAnchor.MiddleLeft);
            caption.raycastTarget = false;
            BroadcastUi.Place(caption.rectTransform, 0.04f, 0.05f, 0.86f, 0.95f);
            var arrow = BroadcastUi.Label(root.transform, "Arrow", "▼", 8, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            arrow.raycastTarget = false;
            BroadcastUi.Place(arrow.rectTransform, 0.87f, 0.05f, 0.98f, 0.95f);
            var templateObject = new GameObject("Template", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            templateObject.transform.SetParent(root.transform, false);
            var template = templateObject.GetComponent<RectTransform>();
            template.anchorMin = new Vector2(0f, 0f);
            template.anchorMax = new Vector2(1f, 0f);
            template.pivot = new Vector2(0.5f, 1f);
            template.anchoredPosition = new Vector2(0f, -2f);
            template.sizeDelta = new Vector2(0f, 220f);
            var templateImage = templateObject.GetComponent<Image>();
            templateImage.color = new Color(0.08f, 0.035f, 0.16f, 1f);
            var scroll = templateObject.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(templateObject.transform, false);
            var viewport = viewportObject.GetComponent<RectTransform>();
            BroadcastUi.Stretch(viewport);
            viewportObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;
            var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportObject.transform, false);
            var content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 0f;
            var fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;

            var itemObject = new GameObject("Item", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
            itemObject.transform.SetParent(contentObject.transform, false);
            var itemRect = itemObject.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0f, 30f);
            var itemBackground = itemObject.GetComponent<Image>();
            itemBackground.color = new Color(1f, 1f, 1f, 0.04f);
            var toggle = itemObject.GetComponent<Toggle>();
            toggle.targetGraphic = itemBackground;
            var checkObject = new GameObject("Item Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            checkObject.transform.SetParent(itemObject.transform, false);
            var checkRect = checkObject.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0f, 0.5f);
            checkRect.anchorMax = new Vector2(0f, 0.5f);
            checkRect.sizeDelta = new Vector2(14f, 14f);
            checkRect.anchoredPosition = new Vector2(12f, 0f);
            var checkImage = checkObject.GetComponent<Image>();
            checkImage.color = UiTheme.Accent;
            toggle.graphic = checkImage;
            var itemText = BroadcastUi.Label(itemObject.transform, "Item Label", "", 10, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(itemText.rectTransform, 0.08f, 0.04f, 0.97f, 0.96f);
            itemText.raycastTarget = false;
            dropdown.template = template;
            dropdown.captionText = caption;
            dropdown.itemText = itemText;
            dropdown.AddOptions(options);
            dropdown.onValueChanged.AddListener(index => changed?.Invoke(index));
            dropdown.RefreshShownValue();
            templateObject.SetActive(false);
            return dropdown;
        }

        List<string> TeamOptions()
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            return new List<string>
            {
                english ? "BOTH TEAMS" : "AMBAS AS EQUIPAS",
                _match == null ? (english ? "HOME TEAM" : "EQUIPA DA CASA") : _match.Home.ShortName,
                _match == null ? (english ? "AWAY TEAM" : "EQUIPA VISITANTE") : _match.Away.ShortName
            };
        }

        List<string> PeriodOptions()
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            return new List<string> { english ? "FULL MATCH" : "JOGO TODO", english ? "FIRST HALF" : "PRIMEIRA PARTE", english ? "SECOND HALF" : "SEGUNDA PARTE" };
        }

        List<string> PlayerOptions()
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            _playerOptionIds.Clear();
            _playerOptionIds.Add(null);
            var options = new List<string> { english ? "ALL PLAYERS" : "TODOS OS JOGADORES" };
            if (_match == null)
                return options;
            var players = new List<CoachMaster.Domain.Player>();
            foreach (var player in _match.People.Values)
            {
                if (player == null || (_team == 1 && player.ClubId != _match.Home.Id) || (_team == 2 && player.ClubId != _match.Away.Id))
                    continue;
                players.Add(player);
            }
            players.Sort((a, b) =>
            {
                var number = a.SquadNumber.CompareTo(b.SquadNumber);
                return number != 0 ? number : string.CompareOrdinal(a.FullName, b.FullName);
            });
            foreach (var player in players)
            {
                _playerOptionIds.Add(player.Id);
                options.Add(player.FullName);
            }
            return options;
        }

        void OnTeamSelected(int value)
        {
            _team = Mathf.Clamp(value, 0, 2);
            _playerId = null;
            _playerFilter.ClearOptions();
            _playerFilter.AddOptions(PlayerOptions());
            _playerFilter.SetValueWithoutNotify(0);
            _playerFilter.RefreshShownValue();
            _dirty = true;
            Refresh();
        }

        void OnPeriodSelected(int value)
        {
            _period = Mathf.Clamp(value, 0, 2);
            _dirty = true;
            Refresh();
        }

        void OnPlayerSelected(int value)
        {
            _playerId = value > 0 && value < _playerOptionIds.Count ? _playerOptionIds[value] : null;
            _dirty = true;
            Refresh();
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            if (_backdrop != null)
                _backdrop.gameObject.SetActive(visible && _finalMode);
            if (_cardRect != null)
            {
                _cardRect.gameObject.SetActive(visible);
                if (visible)
                    _cardRect.SetAsLastSibling();
            }
            if (_backdrop != null)
                _backdrop.color = new Color(0.015f, 0.01f, 0.045f, _finalMode ? 0.88f : 0f);
        }

        public void SetDismissAction(System.Action action) => _dismissAction = action;

        public bool IsVisible => _visible;

        public void SetFinalMode(bool finalMode)
        {
            var modeChanged = !_modeInitialized || _finalMode != finalMode;
            _finalMode = finalMode;
            _modeInitialized = true;
            if (_cardRect == null || _parentRect == null)
                return;
            if (modeChanged)
            {
                var size = _parentRect.rect.size;
                if (size.x < 10f || size.y < 10f)
                    size = new Vector2(Screen.width, Screen.height);
                var factor = finalMode ? 0.94f : 0.36f;
                var height = finalMode ? 0.92f : 0.42f;
                _cardRect.sizeDelta = new Vector2(size.x * factor, size.y * height);
                _cardRect.anchoredPosition = finalMode ? Vector2.zero : new Vector2(size.x * 0.29f, -size.y * 0.24f);
            }
            if (_backdrop != null)
            {
                _backdrop.color = new Color(0.015f, 0.01f, 0.045f, finalMode ? 0.88f : 0f);
                _backdrop.gameObject.SetActive(_visible && finalMode);
            }
            if (_dragHandle != null)
                _dragHandle.enabled = !finalMode;
            for (var i = 0; i < _resizeHandles.Count; i++)
                if (_resizeHandles[i] != null)
                    _resizeHandles[i].enabled = !finalMode;
            if (_windowTitle != null)
                _windowTitle.text = LocalizationManager.Current == GameLanguage.English
                    ? (finalMode ? "FULL-TIME MATCH ANALYSIS" : "LIVE MATCH ANALYSIS")
                    : (finalMode ? "ANÁLISE FINAL DO JOGO" : "ANÁLISE DO JOGO AO VIVO");
        }

        public void SetBackToResult(System.Action action, bool visible)
        {
            SetReturnAction(action, visible, "BACK TO RESULT", "VOLTAR AO RESULTADO");
        }

        public void SetReturnAction(System.Action action, bool visible, string englishLabel, string portugueseLabel)
        {
            _returnToResult = action;
            if (_backToResult != null)
            {
                _backToResult.GetComponentInChildren<Text>().text = LocalizationManager.Current == GameLanguage.English
                    ? englishLabel : portugueseLabel;
                _backToResult.gameObject.SetActive(visible);
            }
        }

        public void SetMatch(MatchSession match)
        {
            if (_match == match)
                return;
            _match = match;
            _dataStamp = -1;
            _dirty = true;
        }

        public void Refresh()
        {
            if (_match == null || _visual == null)
                return;

            if (_languageStamp != (int)LocalizationManager.Current)
                _dirty = true;
            if (!_dirty && Time.unscaledTime < _nextDataCheckTime)
                return;

            _nextDataCheckTime = Time.unscaledTime + 0.2f;
            var stamp = DataStamp();
            if (!_dirty && stamp == _dataStamp)
                return;

            RefreshControls();
            _dataStamp = stamp;
            _dirty = false;
            DrawCurrentView();
        }

        void RefreshControls()
        {
            if (_teamFilter == null)
                return;
            var english = LocalizationManager.Current == GameLanguage.English;
            var languageStamp = (int)LocalizationManager.Current;
            if (_languageStamp != languageStamp)
            {
                _dirty = true;
                _languageStamp = languageStamp;
                _teamFilter.ClearOptions();
                _teamFilter.AddOptions(TeamOptions());
                _periodFilter.ClearOptions();
                _periodFilter.AddOptions(PeriodOptions());
                _playerFilter.ClearOptions();
                _playerFilter.AddOptions(PlayerOptions());
            }
            if (_windowTitle != null)
                _windowTitle.text = english
                    ? (_finalMode ? "FULL-TIME MATCH ANALYSIS" : "LIVE MATCH ANALYSIS")
                    : (_finalMode ? "ANÁLISE FINAL DO JOGO" : "ANÁLISE DO JOGO AO VIVO");
            var labels = english
                ? new[] { "SHOTS", "MOMENTUM", "PASSES", "HEATMAP", "AVG POSITIONS" }
                : new[] { "REMATES", "MOMENTO", "PASSES", "MAPA DE CALOR", "POSIÇÕES MÉDIAS" };
            for (var i = 0; i < _views.Length; i++)
                _views[i].GetComponentInChildren<Text>().text = labels[i];
            _teamFilter.SetValueWithoutNotify(Mathf.Clamp(_team, 0, 2));
            _periodFilter.SetValueWithoutNotify(Mathf.Clamp(_period, 0, 2));
            var selectedPlayer = string.IsNullOrEmpty(_playerId) ? 0 : _playerOptionIds.IndexOf(_playerId);
            if (selectedPlayer < 0)
            {
                _playerId = null;
                selectedPlayer = 0;
            }
            _playerFilter.SetValueWithoutNotify(selectedPlayer);
            _teamFilter.RefreshShownValue();
            _periodFilter.RefreshShownValue();
            _playerFilter.RefreshShownValue();
            for (var i = 0; i < _views.Length; i++)
            {
                var active = i == (int)_mode;
                _views[i].GetComponent<Image>().color = active ? UiTheme.Accent : new Color(0.22f, 0.12f, 0.38f, 1f);
                _views[i].GetComponentInChildren<Text>().color = active ? UiTheme.ButtonText : Color.white;
            }
        }

        int DataStamp()
        {
            var resolved = 0;
            for (var i = 0; i < _match.Passes.Count; i++)
                if (_match.Passes[i].Resolved)
                    resolved++;
            return _match.Shots.Count * 101 + _match.Passes.Count * 37 + resolved * 17 + _match.PositionSamples.Count * 7 + _match.Threats.Count * 23;
        }

        bool IncludeTeam(bool home) => _team == 0 || (_team == 1 && home) || (_team == 2 && !home);

        bool IncludePeriod(float minute)
        {
            return _period == 0 || (_period == 1 && minute < 45f) || (_period == 2 && minute >= 45f);
        }

        void DrawCurrentView()
        {
            for (var i = _visual.childCount - 1; i >= 0; i--)
                Destroy(_visual.GetChild(i).gameObject);
            switch (_mode)
            {
                case ViewMode.Momentum: DrawMomentum(); break;
                case ViewMode.Passing: DrawPassingNetwork(); break;
                case ViewMode.Heatmap: DrawHeatmap(); break;
                case ViewMode.AveragePositions: DrawAveragePositions(); break;
                default: DrawShots(); break;
            }
        }

        void DrawShots()
        {
            BroadcastUi.DrawPitch(_visual, new Color(0.11f, 0.42f, 0.22f));
            var homeXg = 0f;
            var awayXg = 0f;
            var shown = 0;
            foreach (var shot in _match.Shots)
            {
                if (!IncludeTeam(shot.Home) || !IncludePeriod(shot.Minute)
                    || (!string.IsNullOrEmpty(_playerId) && _playerId != shot.ShooterId))
                    continue;
                shown++;
                if (shot.Home) homeXg += shot.ExpectedGoals; else awayXg += shot.ExpectedGoals;
                var at = PitchPoint(shot.Origin);
                var color = shot.Home ? UiTheme.Accent : new Color(1f, 0.55f, 0.25f);
                var size = shot.Outcome == "goal" ? 19f : shot.Outcome == "saved" ? 15f : 12f;
                var dot = BroadcastUi.Solid(_visual, "Shot", color);
                dot.sprite = BroadcastUi.Circle;
                dot.rectTransform.anchorMin = at;
                dot.rectTransform.anchorMax = at;
                dot.rectTransform.sizeDelta = new Vector2(size, size);
                var mark = shot.Outcome == "goal" ? "G" : shot.Outcome == "saved" ? "S" : shot.Outcome == "post" ? "P" : "×";
                var label = BroadcastUi.Label(_visual, "ShotOutcome", mark, 8, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                label.rectTransform.anchorMin = at;
                label.rectTransform.anchorMax = at;
                label.rectTransform.sizeDelta = new Vector2(size, size);
            }
            var english = LocalizationManager.Current == GameLanguage.English;
            _summary.text = (english ? "SHOTS " : "REMATES ") + shown + "   ·   xG  " + homeXg.ToString("0.00") + " – " + awayXg.ToString("0.00")
                + "   ·   " + (english ? "G goal  S saved  P post  × missed" : "G golo  S defendido  P poste  × falhado");
        }

        void DrawMomentum()
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var homeBins = new float[18];
            var awayBins = new float[18];
            foreach (var threat in _match.Threats)
            {
                if (!IncludeTeam(threat.Home) || !IncludePeriod(threat.Minute))
                    continue;
                var bin = Mathf.Clamp((int)(threat.Minute / 5f), 0, 17);
                if (threat.Home) homeBins[bin] += threat.Weight; else awayBins[bin] += threat.Weight;
            }
            var max = 0.1f;
            for (var i = 0; i < homeBins.Length; i++)
                max = Mathf.Max(max, Mathf.Max(homeBins[i], awayBins[i]));
            var baseLine = BroadcastUi.Solid(_visual, "MomentumBaseline", new Color(1f, 1f, 1f, 0.55f));
            BroadcastUi.Place(baseLine.rectTransform, 0.04f, 0.49f, 0.96f, 0.505f);
            for (var i = 0; i < 18; i++)
            {
                var center = 0.055f + i * 0.05235f;
                AddMomentumBar("Home", center, 0.5f, 0.41f * homeBins[i] / max, UiTheme.Accent);
                AddMomentumBar("Away", center, 0.5f - 0.41f * awayBins[i] / max, 0.41f * awayBins[i] / max, new Color(1f, 0.55f, 0.25f));
            }
            AddChartLabel("Start", english ? "0'" : "0'", 0.02f, 0.005f, 0.12f, 0.08f);
            AddChartLabel("Half", "45'", 0.45f, 0.005f, 0.55f, 0.08f);
            AddChartLabel("End", "90'", 0.88f, 0.005f, 0.98f, 0.08f);
            AddChartLabel("HomeName", _match.Home.ShortName, 0.02f, 0.87f, 0.45f, 0.98f, UiTheme.Accent);
            AddChartLabel("AwayName", _match.Away.ShortName, 0.55f, 0.87f, 0.98f, 0.98f, new Color(1f, 0.55f, 0.25f), TextAnchor.MiddleRight);
            _summary.text = english
                ? "Attacking pressure from recorded shots and completed entries into the final third or box."
                : "Pressão ofensiva baseada em remates e entradas concluídas no último terço ou na área.";
        }

        void AddMomentumBar(string name, float center, float yMin, float height, Color color)
        {
            if (height <= 0.001f)
                return;
            var bar = BroadcastUi.Solid(_visual, name, color);
            bar.rectTransform.anchorMin = new Vector2(center - 0.012f, yMin);
            bar.rectTransform.anchorMax = new Vector2(center + 0.012f, yMin + height);
            bar.rectTransform.offsetMin = Vector2.zero;
            bar.rectTransform.offsetMax = Vector2.zero;
        }

        void DrawPassingNetwork()
        {
            BroadcastUi.DrawPitch(_visual, new Color(0.11f, 0.42f, 0.22f));
            var positions = AveragePositions();
            var links = new Dictionary<string, PassLink>();
            foreach (var pass in _match.Passes)
            {
                if (!pass.Resolved || !pass.Completed || !IncludeTeam(pass.Home) || !IncludePeriod(pass.Minute) || string.IsNullOrEmpty(pass.ActualReceiverId))
                    continue;
                if (!string.IsNullOrEmpty(_playerId) && _playerId != pass.PasserId && _playerId != pass.ActualReceiverId)
                    continue;
                var key = (pass.Home ? "H|" : "A|") + pass.PasserId + "|" + pass.ActualReceiverId;
                if (!links.TryGetValue(key, out var link))
                {
                    link = new PassLink { Home = pass.Home, From = pass.PasserId, To = pass.ActualReceiverId, FallbackFrom = pass.Origin, FallbackTo = pass.Target };
                    links[key] = link;
                }
                link.Count++;
            }

            foreach (var link in links.Values)
            {
                var from = positions.TryGetValue(link.From, out var fromPos) ? fromPos : link.FallbackFrom;
                var to = positions.TryGetValue(link.To, out var toPos) ? toPos : link.FallbackTo;
                var color = link.Home ? UiTheme.Accent : new Color(1f, 0.55f, 0.25f);
                var selected = string.IsNullOrEmpty(_playerId) || _playerId == link.From || _playerId == link.To;
                color.a = selected ? 0.28f + Mathf.Min(0.5f, link.Count * 0.08f) : 0.09f;
                DrawLine(PitchPoint(from), PitchPoint(to), color, 1.5f + Mathf.Min(4f, link.Count));
            }

            var drawn = new HashSet<string>();
            foreach (var pair in positions)
            {
                var home = IsHomePlayer(pair.Key);
                if (!IncludeTeam(home) || !drawn.Add(pair.Key))
                    continue;
                var at = PitchPoint(pair.Value);
                var color = home ? UiTheme.Accent : new Color(1f, 0.55f, 0.25f);
                if (_playerId == pair.Key)
                    color = Color.white;
                var player = _match.People.TryGetValue(pair.Key, out var found) ? found : null;
                var number = player == null ? "·" : player.SquadNumber.ToString();
                DrawPlayerMarker("Player_" + pair.Key, number, at, color, 22f);
            }
            _summary.text = LocalizationManager.Current == GameLanguage.English
                ? "Completed passes only · use the player list to show passes involving one player."
                : "Só passes concluídos · usa a lista para ver os passes de um jogador.";
        }

        Dictionary<string, Vector2> AveragePositions()
        {
            var sum = new Dictionary<string, Vector2>();
            var count = new Dictionary<string, int>();
            foreach (var sample in _match.PositionSamples)
            {
                if (!IncludePeriod(sample.Minute))
                    continue;
                AddPositions(sample.HomePlayerIds, sample.HomePositions, true, sum, count);
                AddPositions(sample.AwayPlayerIds, sample.AwayPositions, false, sum, count);
            }
            var result = new Dictionary<string, Vector2>();
            foreach (var pair in sum)
                result[pair.Key] = pair.Value / Mathf.Max(1, count[pair.Key]);
            return result;
        }

        void DrawAveragePositions()
        {
            BroadcastUi.DrawPitch(_visual, new Color(0.11f, 0.42f, 0.22f));
            var positions = AveragePositions();
            var english = LocalizationManager.Current == GameLanguage.English;
            var count = 0;
            foreach (var pair in positions)
            {
                if (!IncludeTeam(IsHomePlayer(pair.Key)))
                    continue;
                count++;
                var at = PitchPoint(pair.Value);
                var home = IsHomePlayer(pair.Key);
                var player = _match.People.TryGetValue(pair.Key, out var found) ? found : null;
                DrawPlayerMarker("AveragePosition_" + pair.Key,
                    player == null ? "·" : player.SquadNumber.ToString(), at,
                    pair.Key == _playerId ? Color.white : home ? UiTheme.Accent : new Color(1f, 0.55f, 0.25f), 24f);
            }
            AddChartLabel("HomeName", _match.Home.ShortName, 0.02f, 0.87f, 0.45f, 0.98f, UiTheme.Accent);
            AddChartLabel("AwayName", _match.Away.ShortName, 0.55f, 0.87f, 0.98f, 0.98f,
                new Color(1f, 0.55f, 0.25f), TextAnchor.MiddleRight);
            _summary.text = count == 0
                ? (english ? "No player positions were recorded for this filter." : "Não há posições registadas para este filtro.")
                : (english ? "Average positions from recorded live movement · choose a player from the list to isolate their position."
                    : "Posições médias do movimento registado no jogo · escolhe um jogador na lista para o isolar.");
        }

        void DrawPlayerMarker(string name, string number, Vector2 at, Color color, float size)
        {
            var marker = BroadcastUi.Solid(_visual, name, color);
            marker.sprite = BroadcastUi.Circle;
            marker.raycastTarget = false;
            marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = at;
            marker.rectTransform.sizeDelta = new Vector2(size, size);
            var label = BroadcastUi.Label(marker.transform, "Number", number, 8, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.raycastTarget = false;
            BroadcastUi.Stretch(label.rectTransform);
        }

        void AddPositions(string[] ids, Vector2[] points, bool home, Dictionary<string, Vector2> sum, Dictionary<string, int> count)
        {
            if (!IncludeTeam(home) || ids == null || points == null)
                return;
            var length = Mathf.Min(ids.Length, points.Length);
            for (var i = 0; i < length; i++)
            {
                var id = ids[i];
                if (string.IsNullOrEmpty(id) || ( !string.IsNullOrEmpty(_playerId) && _playerId != id))
                    continue;
                sum[id] = sum.TryGetValue(id, out var total) ? total + points[i] : points[i];
                count[id] = count.TryGetValue(id, out var samples) ? samples + 1 : 1;
            }
        }

        bool IsHomePlayer(string id)
        {
            for (var i = 0; i < _match.HomeOnField.Count; i++)
                if (_match.HomeOnField[i] == id)
                    return true;
            foreach (var sample in _match.PositionSamples)
                if (sample.HomePlayerIds != null)
                    for (var i = 0; i < sample.HomePlayerIds.Length; i++)
                        if (sample.HomePlayerIds[i] == id)
                            return true;
            return false;
        }

        void DrawHeatmap()
        {
            BroadcastUi.DrawPitch(_visual, new Color(0.11f, 0.42f, 0.22f));
            var home = new int[10, 8];
            var away = new int[10, 8];
            foreach (var sample in _match.PositionSamples)
            {
                if (!IncludePeriod(sample.Minute))
                    continue;
                AccumulateHeat(sample.HomePlayerIds, sample.HomePositions, true, home);
                AccumulateHeat(sample.AwayPlayerIds, sample.AwayPositions, false, away);
            }
            var max = 1;
            for (var x = 0; x < 10; x++)
                for (var y = 0; y < 8; y++)
                    max = Mathf.Max(max, Mathf.Max(home[x, y], away[x, y]));
            DrawHeatCells(home, true, max);
            DrawHeatCells(away, false, max);
            _summary.text = LocalizationManager.Current == GameLanguage.English
                ? "Position samples are recorded from the live player movement every 30 seconds of match time."
                : "As posições vêm do movimento real dos jogadores, registado a cada 30 segundos de jogo.";
        }

        void AccumulateHeat(string[] ids, Vector2[] points, bool home, int[,] grid)
        {
            if (!IncludeTeam(home) || ids == null || points == null)
                return;
            var length = Mathf.Min(ids.Length, points.Length);
            for (var i = 0; i < length; i++)
            {
                if (!string.IsNullOrEmpty(_playerId) && _playerId != ids[i])
                    continue;
                var x = Mathf.Clamp((int)(points[i].y * 10f), 0, 9);
                var y = Mathf.Clamp((int)(points[i].x * 8f), 0, 7);
                grid[x, y]++;
            }
        }

        void DrawHeatCells(int[,] grid, bool home, int max)
        {
            var teamColor = home ? UiTheme.Accent : new Color(1f, 0.55f, 0.25f);
            for (var x = 0; x < 10; x++)
            {
                for (var y = 0; y < 8; y++)
                {
                    var value = grid[x, y];
                    if (value <= 0)
                        continue;
                    var spot = new Vector2(0.04f + (x + 0.5f) * 0.092f, 0.04f + (y + 0.5f) * 0.115f);
                    var marker = BroadcastUi.Solid(_visual, "Heat", teamColor);
                    marker.sprite = BroadcastUi.Circle;
                    marker.rectTransform.anchorMin = spot;
                    marker.rectTransform.anchorMax = spot;
                    marker.rectTransform.sizeDelta = new Vector2(40f, 40f);
                    var color = teamColor;
                    color.a = 0.08f + 0.28f * value / max;
                    marker.color = color;
                }
            }
        }

        void DrawLine(Vector2 from, Vector2 to, Color color, float thickness)
        {
            var area = _visual.rect;
            var delta = new Vector2((to.x - from.x) * area.width, (to.y - from.y) * area.height);
            var midpoint = (from + to) * 0.5f;
            var line = BroadcastUi.Solid(_visual, "PassLink", color);
            var rect = line.rectTransform;
            rect.anchorMin = midpoint;
            rect.anchorMax = midpoint;
            rect.sizeDelta = new Vector2(delta.magnitude, thickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        static Vector2 PitchPoint(Vector2 position)
        {
            return new Vector2(Mathf.Clamp01(0.04f + position.y * 0.92f), Mathf.Clamp01(0.04f + position.x * 0.92f));
        }

        void AddChartLabel(string name, string text, float x0, float y0, float x1, float y1, Color? color = null, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var label = BroadcastUi.Label(_visual, name, text, 10, color ?? Color.white, anchor, FontStyle.Bold);
            BroadcastUi.Place(label.rectTransform, x0, y0, x1, y1);
        }

        string PlayerName(string id)
        {
            return _match != null && _match.Names.TryGetValue(id, out var name) ? name : id;
        }

        sealed class PassLink
        {
            public bool Home;
            public string From;
            public string To;
            public Vector2 FallbackFrom;
            public Vector2 FallbackTo;
            public int Count;
        }
    }

    public sealed class MatchAnalysisWindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        RectTransform _window;
        RectTransform _bounds;
        Canvas _canvas;

        public void Initialize(RectTransform window, RectTransform bounds)
        {
            _window = window;
            _bounds = bounds;
            _canvas = bounds == null ? null : bounds.GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData)
        {
            if (_window == null || _bounds == null)
                return;
            var scale = _canvas == null ? 1f : Mathf.Max(0.01f, _canvas.scaleFactor);
            var position = _window.anchoredPosition + eventData.delta / scale;
            var maxX = Mathf.Max(0f, (_bounds.rect.width - _window.rect.width) * 0.5f);
            var maxY = Mathf.Max(0f, (_bounds.rect.height - _window.rect.height) * 0.5f);
            _window.anchoredPosition = new Vector2(Mathf.Clamp(position.x, -maxX, maxX), Mathf.Clamp(position.y, -maxY, maxY));
        }
    }

    public sealed class MatchAnalysisWindowResize : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        RectTransform _window;
        RectTransform _bounds;
        Canvas _canvas;
        Vector2 _pointerStart;
        Vector2 _sizeStart;
        Vector2 _positionStart;
        bool _left;
        bool _right;
        bool _bottom;
        bool _top;

        public void Initialize(RectTransform window, RectTransform bounds, bool left, bool right, bool bottom, bool top)
        {
            _window = window;
            _bounds = bounds;
            _canvas = bounds == null ? null : bounds.GetComponentInParent<Canvas>();
            _left = left;
            _right = right;
            _bottom = bottom;
            _top = top;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_window == null)
                return;
            _pointerStart = eventData.position;
            _sizeStart = _window.sizeDelta;
            _positionStart = _window.anchoredPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_window == null || _bounds == null)
                return;
            var scale = _canvas == null ? 1f : Mathf.Max(0.01f, _canvas.scaleFactor);
            var pointerDelta = (eventData.position - _pointerStart) / scale;
            var boundsSize = _bounds.rect.size;
            var widthDelta = _right ? pointerDelta.x : _left ? -pointerDelta.x : 0f;
            var heightDelta = _top ? pointerDelta.y : _bottom ? -pointerDelta.y : 0f;
            var minWidth = Mathf.Min(220f, boundsSize.x * 0.2f);
            var minHeight = Mathf.Min(160f, boundsSize.y * 0.2f);
            var width = Mathf.Clamp(_sizeStart.x + widthDelta, minWidth, boundsSize.x * 0.96f);
            var height = Mathf.Clamp(_sizeStart.y + heightDelta, minHeight, boundsSize.y * 0.96f);
            var appliedWidthDelta = width - _sizeStart.x;
            var appliedHeightDelta = height - _sizeStart.y;
            var position = _positionStart;
            if (_left) position.x -= appliedWidthDelta * 0.5f;
            else if (_right) position.x += appliedWidthDelta * 0.5f;
            if (_bottom) position.y -= appliedHeightDelta * 0.5f;
            else if (_top) position.y += appliedHeightDelta * 0.5f;
            var maxX = Mathf.Max(0f, (boundsSize.x - width) * 0.5f);
            var maxY = Mathf.Max(0f, (boundsSize.y - height) * 0.5f);
            position.x = Mathf.Clamp(position.x, -maxX, maxX);
            position.y = Mathf.Clamp(position.y, -maxY, maxY);
            _window.anchoredPosition = position;
            _window.sizeDelta = new Vector2(width, height);
        }
    }

    public sealed class MatchAnalysisDismiss : MonoBehaviour, IPointerClickHandler
    {
        System.Action _dismiss;

        public void Initialize(System.Action dismiss) => _dismiss = dismiss;

        public void OnPointerClick(PointerEventData eventData) => _dismiss?.Invoke();
    }
}
