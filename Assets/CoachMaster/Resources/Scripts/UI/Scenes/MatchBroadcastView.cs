using System;
using System.Collections.Generic;
using System.Globalization;
using CoachMaster.Career;
using CoachMaster.Domain;
using CoachMaster.MatchPlay;
using CoachMaster.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Scenes
{
    public sealed class MatchBroadcastView : MonoBehaviour
    {
        const int StatCount = 9;
        const int RowCount = 6;

        MatchSession _match;
        CareerState _career;
        Action _speed;
        Text _clock;
        Text _score;
        Text _dugout;
        Text _xg;
        Text _commentary;
        Text _paceLabel;
        Button _suggestionButton;
        Button _calloutButton;
        Button _ignoreButton;
        GameObject _calloutMenu;
        RectTransform _plot;
        int _drawnShots = -1;
        int _deskCard;
        float _nextAdviceMinute = -1f;
        string _adviceKind = "";
        string _advicePlayerId;
        bool _adviceReady;
        readonly List<Image> _plotMarks = new List<Image>();
        readonly Image[] _homeMarks = new Image[11];
        readonly Text[] _homeNames = new Text[11];
        readonly RectTransform[] _homeHits = new RectTransform[11];
        readonly Image[] _awayMarks = new Image[11];
        readonly Text[] _awayNames = new Text[11];
        readonly RectTransform[] _awayHits = new RectTransform[11];
        readonly StatRow[] _homeStats = new StatRow[StatCount];
        readonly StatRow[] _awayStats = new StatRow[StatCount];
        readonly EventRow[] _events = new EventRow[RowCount];
        readonly ScoreRow[] _scores = new ScoreRow[RowCount];
        readonly Image[] _momentum = new Image[12];

        sealed class StatRow
        {
            public Text Value;
            public Image Plate;
        }

        sealed class EventRow
        {
            public GameObject Root;
            public Text Minute;
            public Image Icon;
            public Text Name;
        }

        sealed class ScoreRow
        {
            public GameObject Root;
            public Image HomeCrest;
            public Text Home;
            public Text Score;
            public Image AwayCrest;
            public Text Away;
        }

        public static void Attach(Transform root, MatchSession match, CareerState career, RectTransform pitch, Action leave, Action speed, Action dugout, Action stats)
        {
            if (!Application.isPlaying || root == null || match == null || root.Find("BroadcastDesk") != null)
                return;

            Hide(root, "Wordmark");
            Hide(root, "Scoreboard");
            Hide(root, "Tabs");
            Hide(root, "Plan");
            Hide(root, "Ticker");
            Hide(root, "Footer");
            Hide(root, "Fitness/HomeXi");
            Hide(root, "Fitness/AwayXi");
            Hide(root, "Stats");
            Hide(root, "Ratings");

            var desk = new GameObject("BroadcastDesk", typeof(RectTransform));
            desk.transform.SetParent(root, false);
            BroadcastUi.Stretch(desk.GetComponent<RectTransform>());
            var view = desk.AddComponent<MatchBroadcastView>();
            view._match = match;
            view._career = career;
            view._speed = speed;
            view.Build(pitch, leave, stats);
            desk.transform.SetAsLastSibling();
        }

        static void Hide(Transform root, string path)
        {
            var item = root.Find(path);
            if (item != null)
                item.gameObject.SetActive(false);
        }

        void Build(RectTransform pitch, Action leave, Action stats)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var bar = Card("Top", 0f, 0.925f, 1f, 1f, "");
            var leaveButton = BroadcastUi.Button(bar, "Leave", english ? "Leave match" : "Sair do jogo", leave, new Color(0.78f, 0.22f, 0.62f), Color.white, 15);
            BroadcastUi.Place(leaveButton.GetComponent<RectTransform>(), 0.012f, 0.16f, 0.12f, 0.84f);
            var pill = BroadcastUi.Panel(bar, "ScorePill", new Color(0.1f, 0.09f, 0.14f, 1f), false);
            BroadcastUi.Place(pill.rectTransform, 0.34f, 0.12f, 0.66f, 0.88f);
            _clock = BroadcastUi.Label(pill.transform, "Clock", "00:00", 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(_clock.rectTransform, 0.02f, 0f, 0.22f, 1f);
            var homeName = BroadcastUi.Label(pill.transform, "Home", _match.Home.ShortName, 16, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
            BroadcastUi.Place(homeName.rectTransform, 0.22f, 0f, 0.4f, 1f);
            _score = BroadcastUi.Label(pill.transform, "Score", "0 - 0", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(_score.rectTransform, 0.4f, 0f, 0.6f, 1f);
            var awayName = BroadcastUi.Label(pill.transform, "Away", _match.Away.ShortName, 16, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(awayName.rectTransform, 0.6f, 0f, 0.82f, 1f);
            var pause = BroadcastUi.Button(pill.transform, "Pause", "II", leave, new Color(1f, 1f, 1f, 0.08f), Color.white, 16);
            BroadcastUi.Place(pause.GetComponent<RectTransform>(), 0.84f, 0.18f, 0.98f, 0.82f);
            var pace = BroadcastUi.Button(bar, "Pace", "2×", _speed, new Color(1f, 1f, 1f, 0.08f), Color.white, 15);
            BroadcastUi.Place(pace.GetComponent<RectTransform>(), 0.9f, 0.16f, 0.98f, 0.84f);
            _paceLabel = pace.GetComponentInChildren<Text>();

            Card("HomeShape", 0.008f, 0.5f, 0.2f, 0.915f, (_match.HomeTactics == null ? "4-3-3" : _match.HomeTactics.Formation));
            BuildShape("HomeShape", true);
            Card("Dugout", 0.208f, 0.5f, 0.4f, 0.915f, english ? "ASSISTANT" : "ADJUNTO");
            _dugout = Line(transform.Find("Dugout"), "Body", 15, 0.06f, 0.22f, 0.94f, 0.84f);
            _suggestionButton = BroadcastUi.Button(transform.Find("Dugout"), "DoIt", english ? "USE SUGGESTION" : "USAR SUGESTÃO",
                ApplyAdvice, new Color(0.72f, 0.2f, 0.72f), Color.white, 11);
            BroadcastUi.Place(_suggestionButton.GetComponent<RectTransform>(), 0.035f, 0.055f, 0.37f, 0.18f);
            _calloutButton = BroadcastUi.Button(transform.Find("Dugout"), "Callout", english ? "TEAM TALK ▾" : "DISCURSO ▾",
                ToggleCalloutMenu, new Color(0.13f, 0.2f, 0.28f), Color.white, 11);
            BroadcastUi.Place(_calloutButton.GetComponent<RectTransform>(), 0.39f, 0.055f, 0.73f, 0.18f);
            _ignoreButton = BroadcastUi.Button(transform.Find("Dugout"), "Dismiss", english ? "IGNORE" : "IGNORAR",
                DismissAdvice, new Color(0.16f, 0.15f, 0.2f), Color.white, 11);
            BroadcastUi.Place(_ignoreButton.GetComponent<RectTransform>(), 0.75f, 0.055f, 0.965f, 0.18f);
            SetAdviceControls(false);
            Card("Events", 0.408f, 0.5f, 0.64f, 0.915f, english ? "MATCH EVENTS" : "ACONTECIMENTOS");
            BuildEvents();
            Card("Xg", 0.648f, 0.5f, 0.8f, 0.915f, english ? "xG GRAPH" : "HISTÓRIA DO xG");
            var swap = BroadcastUi.Button(transform.Find("Xg"), "Swap", "▼", CycleDeskCard, new Color(1f, 1f, 1f, 0.08f), Color.white, 16);
            BroadcastUi.Place(swap.GetComponent<RectTransform>(), 0.78f, 0.84f, 0.96f, 0.98f);
            _xg = BroadcastUi.Label(transform.Find("Xg"), "Caption", "", 12, new Color(0.75f, 0.78f, 0.86f), TextAnchor.LowerLeft);
            BroadcastUi.Place(_xg.rectTransform, 0.06f, 0.04f, 0.94f, 0.2f);
            var plotGo = new GameObject("Plot", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            plotGo.transform.SetParent(transform.Find("Xg"), false);
            _plot = plotGo.GetComponent<RectTransform>();
            var plotImage = plotGo.GetComponent<Image>();
            plotImage.color = new Color(0f, 0f, 0f, 0.01f);
            plotImage.raycastTarget = false;
            BroadcastUi.Place(_plot, 0.08f, 0.22f, 0.94f, 0.82f);
            Card("AwayShape", 0.808f, 0.5f, 0.992f, 0.915f, (_match.AwayTactics == null ? "4-3-3" : _match.AwayTactics.Formation));
            BuildShape("AwayShape", false);

            Card("Momentum", 0.008f, 0.11f, 0.2f, 0.49f, english ? "MOMENTUM" : "MOMENTO");
            BuildMomentum();

            Card("HomeStats", 0.208f, 0.11f, 0.36f, 0.49f, _match.Home.ShortName);
            BuildStats("HomeStats", _homeStats, english);
            var homeView = BroadcastUi.Button(transform.Find("HomeStats"), "View", english ? "View stats" : "Ver estatísticas", stats, new Color(1f, 1f, 1f, 0.08f), Color.white, 12);
            BroadcastUi.Place(homeView.GetComponent<RectTransform>(), 0.08f, 0.03f, 0.92f, 0.14f);

            var slot = BroadcastUi.Zone(transform, "PitchSlot", 0.368f, 0.11f, 0.7f, 0.49f);
            if (pitch != null)
            {
                pitch.SetParent(slot, false);
                BroadcastUi.Stretch(pitch);
                var zone = transform.parent.Find("Fitness/PitchZone");
                if (zone != null)
                    zone.gameObject.SetActive(false);
            }

            Card("AwayStats", 0.708f, 0.11f, 0.86f, 0.49f, _match.Away.ShortName);
            BuildStats("AwayStats", _awayStats, english);
            var awayView = BroadcastUi.Button(transform.Find("AwayStats"), "View", english ? "View stats" : "Ver estatísticas", stats, new Color(1f, 1f, 1f, 0.08f), Color.white, 12);
            BroadcastUi.Place(awayView.GetComponent<RectTransform>(), 0.08f, 0.03f, 0.92f, 0.14f);

            Card("Latest", 0.868f, 0.11f, 0.992f, 0.49f, english ? "LATEST SCORES" : "ÚLTIMOS RESULTADOS");
            BuildScores();

            var comment = BroadcastUi.Panel(transform, "Commentary", new Color(0.45f, 0.9f, 0.96f, 1f), false);
            BroadcastUi.Place(comment.rectTransform, 0.08f, 0.018f, 0.92f, 0.09f);
            _commentary = BroadcastUi.Label(comment.transform, "Line", "", 18, new Color(0.05f, 0.12f, 0.16f), TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Stretch(_commentary.rectTransform);
            _commentary.horizontalOverflow = HorizontalWrapMode.Wrap;
            _commentary.verticalOverflow = VerticalWrapMode.Truncate;
            Paint();
        }

        Transform Card(string name, float x0, float y0, float x1, float y1, string title)
        {
            var panel = BroadcastUi.Panel(transform, name, new Color(0.07f, 0.065f, 0.1f, 1f), false);
            BroadcastUi.Place(panel.rectTransform, x0, y0, x1, y1);
            if (!string.IsNullOrEmpty(title))
            {
                var heading = BroadcastUi.Label(panel.transform, "Title", title, 12, new Color(0.62f, 0.66f, 0.76f), TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(heading.rectTransform, 0.05f, 0.86f, 0.95f, 0.98f);
                heading.horizontalOverflow = HorizontalWrapMode.Wrap;
                heading.verticalOverflow = VerticalWrapMode.Truncate;
            }
            return panel.transform;
        }

        static Text Line(Transform parent, string name, int size, float x0, float y0, float x1, float y1)
        {
            var label = BroadcastUi.Label(parent, name, "", size, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.Place(label.rectTransform, x0, y0, x1, y1);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        void BuildShape(string card, bool home)
        {
            var host = transform.Find(card);
            var pitch = BroadcastUi.Zone(host, "Mini", 0.06f, 0.04f, 0.94f, 0.84f);
            BroadcastUi.DrawVerticalPitch(pitch, new Color(0.1f, 0.42f, 0.2f));
            var marks = home ? _homeMarks : _awayMarks;
            var names = home ? _homeNames : _awayNames;
            BroadcastUi.PitchMarks(_match.Home, _match.Away, out var homeMark, out var awayMark);
            var color = home ? homeMark : awayMark;
            for (var i = 0; i < 11; i++)
            {
                var dot = BroadcastUi.Solid(pitch, "P" + i, color);
                dot.sprite = BroadcastUi.Circle;
                dot.rectTransform.sizeDelta = new Vector2(18f, 18f);
                marks[i] = dot;
                var name = BroadcastUi.Label(pitch, "N" + i, "", 10, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                name.horizontalOverflow = HorizontalWrapMode.Overflow;
                name.verticalOverflow = VerticalWrapMode.Truncate;
                name.raycastTarget = false;
                names[i] = name;
                var hit = BroadcastUi.Solid(pitch, "Hit" + i, new Color(1f, 1f, 1f, 0.001f), true);
                hit.rectTransform.sizeDelta = new Vector2(46f, 46f);
                var slot = i;
                var side = home;
                BroadcastUi.Hit(hit, () => PickShape(side, slot));
                (home ? _homeHits : _awayHits)[i] = hit.rectTransform;
            }
        }

        void PickShape(bool home, int slot)
        {
            if (_match == null)
                return;
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            if (slot < 0 || squad == null || slot >= squad.Count)
                return;
            var panel = UnityEngine.Object.FindFirstObjectByType<MatchAnalysisPanel>();
            if (panel == null)
                return;
            panel.SetVisible(true);
            panel.FocusPlayer(squad[slot]);
            var card = panel.transform.Find("MatchAnalysis");
            if (card != null)
                card.SetAsLastSibling();
        }

        void BuildEvents()
        {
            var host = transform.Find("Events");
            for (var i = 0; i < RowCount; i++)
            {
                var y1 = 0.84f - i * 0.125f;
                var row = BroadcastUi.Zone(host, "Event" + i, 0.04f, y1 - 0.11f, 0.96f, y1);
                var minute = BroadcastUi.Label(row, "Minute", "", 13, new Color(0.75f, 0.78f, 0.86f), TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(minute.rectTransform, 0f, 0f, 0.18f, 1f);
                var icon = BroadcastUi.Solid(row, "Icon", Color.white);
                icon.sprite = BroadcastUi.Circle;
                BroadcastUi.Place(icon.rectTransform, 0.2f, 0.22f, 0.32f, 0.78f);
                var mark = BroadcastUi.Label(icon.transform, "Mark", "", 11, new Color(0.08f, 0.08f, 0.1f), TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Stretch(mark.rectTransform);
                var name = BroadcastUi.Label(row, "Name", "", 13, Color.white, TextAnchor.MiddleLeft);
                BroadcastUi.Place(name.rectTransform, 0.36f, 0f, 1f, 1f);
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                name.verticalOverflow = VerticalWrapMode.Truncate;
                _events[i] = new EventRow { Root = row.gameObject, Minute = minute, Icon = icon, Name = name };
            }
        }

        void BuildStats(string card, StatRow[] rows, bool english)
        {
            var labels = english
                ? new[] { "Shots", "On target", "xG", "Distance", "Passes", "Corners", "Fouls", "Yellows", "Possession" }
                : new[] { "Remates", "À baliza", "xG", "Distância", "Passes", "Cantos", "Faltas", "Amarelos", "Posse" };
            var host = transform.Find(card);
            for (var i = 0; i < StatCount; i++)
            {
                var y1 = 0.84f - i * 0.075f;
                var row = BroadcastUi.Zone(host, "Stat" + i, 0.05f, y1 - 0.068f, 0.95f, y1);
                var label = BroadcastUi.Label(row, "Label", labels[i], 12, new Color(0.78f, 0.8f, 0.86f), TextAnchor.MiddleLeft);
                BroadcastUi.Place(label.rectTransform, 0f, 0f, 0.62f, 1f);
                label.verticalOverflow = VerticalWrapMode.Truncate;
                var plate = BroadcastUi.Solid(row, "Plate", new Color(1f, 1f, 1f, 0f));
                BroadcastUi.Place(plate.rectTransform, 0.64f, 0.12f, 1f, 0.88f);
                var value = BroadcastUi.Label(plate.transform, "Value", "0", 13, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Stretch(value.rectTransform);
                rows[i] = new StatRow { Value = value, Plate = plate };
            }
        }

        void BuildScores()
        {
            var host = transform.Find("Latest");
            for (var i = 0; i < RowCount; i++)
            {
                var y1 = 0.84f - i * 0.125f;
                var row = BroadcastUi.Zone(host, "Score" + i, 0.03f, y1 - 0.11f, 0.97f, y1);
                var homeCrest = BroadcastUi.Solid(row, "HomeCrest", Color.white);
                BroadcastUi.Place(homeCrest.rectTransform, 0f, 0.15f, 0.14f, 0.85f);
                homeCrest.preserveAspect = true;
                var home = BroadcastUi.Label(row, "Home", "", 11, Color.white, TextAnchor.MiddleLeft);
                BroadcastUi.Place(home.rectTransform, 0.15f, 0f, 0.4f, 1f);
                home.verticalOverflow = VerticalWrapMode.Truncate;
                var score = BroadcastUi.Label(row, "Score", "", 12, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(score.rectTransform, 0.4f, 0f, 0.58f, 1f);
                var awayCrest = BroadcastUi.Solid(row, "AwayCrest", Color.white);
                BroadcastUi.Place(awayCrest.rectTransform, 0.58f, 0.15f, 0.72f, 0.85f);
                awayCrest.preserveAspect = true;
                var away = BroadcastUi.Label(row, "Away", "", 11, Color.white, TextAnchor.MiddleLeft);
                BroadcastUi.Place(away.rectTransform, 0.73f, 0f, 1f, 1f);
                away.verticalOverflow = VerticalWrapMode.Truncate;
                _scores[i] = new ScoreRow { Root = row.gameObject, HomeCrest = homeCrest, Home = home, Score = score, AwayCrest = awayCrest, Away = away };
                row.gameObject.SetActive(false);
            }
        }

        void BuildMomentum()
        {
            var host = transform.Find("Momentum");
            var axis = BroadcastUi.Solid(host, "Axis", new Color(1f, 1f, 1f, 0.2f));
            BroadcastUi.Place(axis.rectTransform, 0.08f, 0.46f, 0.92f, 0.48f);
            for (var i = 0; i < _momentum.Length; i++)
            {
                var bar = BroadcastUi.Solid(host, "Tick" + i, new Color(0.45f, 0.9f, 0.96f));
                var x = 0.1f + i * 0.07f;
                bar.rectTransform.anchorMin = new Vector2(x, 0.47f);
                bar.rectTransform.anchorMax = new Vector2(x + 0.045f, 0.47f);
                bar.rectTransform.offsetMin = Vector2.zero;
                bar.rectTransform.offsetMax = Vector2.zero;
                _momentum[i] = bar;
            }
        }

        void Update()
        {
            if (_match == null)
                return;
            if (transform.parent != null)
            {
                Hide(transform.parent, "Fitness/HomeXi");
                Hide(transform.parent, "Fitness/AwayXi");
                Hide(transform.parent, "Footer");
                Hide(transform.parent, "Tabs");
                Hide(transform.parent, "Ticker");
                Hide(transform.parent, "Stats");
                Hide(transform.parent, "Ratings");
                Hide(transform.parent, "Plan");
            }
            if (_paceLabel != null)
            {
                var mark = SettingsPrefs.MatchSpeedLabel;
                if (_paceLabel.text != mark)
                    _paceLabel.text = mark;
            }
            if (Time.unscaledTime < _next)
                return;
            _next = Time.unscaledTime + 0.2f;
            Paint();
        }

        float _next;

        void Paint()
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var minute = Mathf.FloorToInt(Mathf.Max(0f, _match.Minute));
            var second = Mathf.FloorToInt(Mathf.Repeat(_match.Minute, 1f) * 60f);
            _clock.text = minute.ToString("00") + ":" + second.ToString("00");
            _score.text = _match.HomeGoals + "  -  " + _match.AwayGoals;
            PaintShape(true);
            PaintShape(false);
            PaintStats(english);
            PaintEvents();
            PaintScores();
            PaintMomentum();
            PaintAdvice(english);
            _commentary.text = string.IsNullOrEmpty(_match.LastEvent) ? "" : LocalizationManager.Translate(_match.LastEvent);
            PaintXg(english);
        }

        void PaintShape(bool home)
        {
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            var formation = home
                ? (_match.HomeTactics == null ? "4-3-3" : _match.HomeTactics.Formation)
                : (_match.AwayTactics == null ? "4-3-3" : _match.AwayTactics.Formation);
            var marks = home ? _homeMarks : _awayMarks;
            var names = home ? _homeNames : _awayNames;
            for (var i = 0; i < 11; i++)
            {
                var spot = MatchEngine.Slot(true, i, formation);
                var shown = i < squad.Count;
                marks[i].gameObject.SetActive(shown);
                names[i].gameObject.SetActive(shown);
                if (!shown)
                    continue;
                marks[i].rectTransform.anchorMin = marks[i].rectTransform.anchorMax = new Vector2(spot.x, spot.y);
                var hits = home ? _homeHits : _awayHits;
                if (hits[i] != null)
                {
                    hits[i].anchorMin = hits[i].anchorMax = new Vector2(spot.x, spot.y);
                    hits[i].sizeDelta = new Vector2(46f, 46f);
                    hits[i].SetAsLastSibling();
                }
                names[i].rectTransform.anchorMin = new Vector2(spot.x - 0.16f, spot.y + 0.035f);
                names[i].rectTransform.anchorMax = new Vector2(spot.x + 0.16f, spot.y + 0.1f);
                names[i].rectTransform.offsetMin = Vector2.zero;
                names[i].rectTransform.offsetMax = Vector2.zero;
                if (_match.People.TryGetValue(squad[i], out var player))
                {
                    var parts = player.FullName.Split(' ');
                    names[i].text = parts.Length == 0 ? player.FullName : parts[parts.Length - 1];
                    var number = marks[i].transform.Find("Num");
                    if (number == null)
                    {
                        var label = BroadcastUi.Label(marks[i].rectTransform, "Num", player.SquadNumber.ToString(), 9, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold);
                        BroadcastUi.Stretch(label.rectTransform);
                    }
                }
            }
        }

        void PaintStats(bool english)
        {
            var home = Count(true);
            var away = Count(false);
            var homeText = Format(home, english);
            var awayText = Format(away, english);
            for (var i = 0; i < StatCount; i++)
            {
                _homeStats[i].Value.text = homeText[i];
                _awayStats[i].Value.text = awayText[i];
                var homeWins = home[i] > away[i] + 0.001f;
                var awayWins = away[i] > home[i] + 0.001f;
                _homeStats[i].Plate.color = homeWins ? new Color(0.45f, 0.9f, 0.96f, 0.28f) : new Color(1f, 1f, 1f, 0f);
                _awayStats[i].Plate.color = awayWins ? new Color(0.45f, 0.9f, 0.96f, 0.28f) : new Color(1f, 1f, 1f, 0f);
            }
        }

        float[] Count(bool home)
        {
            var values = new float[StatCount];
            for (var i = 0; i < _match.Shots.Count; i++)
            {
                var shot = _match.Shots[i];
                if (shot.Home != home)
                    continue;
                values[0] += 1f;
                values[2] += shot.ExpectedGoals;
                if (shot.Outcome == "goal" || shot.Outcome == "saved")
                    values[1] += 1f;
            }
            var passes = 0f;
            var completed = 0f;
            for (var i = 0; i < _match.Passes.Count; i++)
            {
                var pass = _match.Passes[i];
                if (pass.Home != home || !pass.Resolved)
                    continue;
                passes += 1f;
                if (pass.Completed)
                    completed += 1f;
            }
            values[4] = passes <= 0f ? 0f : completed * 100f / passes;
            var clubId = home ? _match.Home.Id : _match.Away.Id;
            for (var i = 0; i < _match.Notes.Count; i++)
            {
                var note = _match.Notes[i];
                if (string.IsNullOrEmpty(note.PlayerId) || !_match.People.TryGetValue(note.PlayerId, out var player) || player.ClubId != clubId)
                    continue;
                if (note.Kind == "corner")
                    values[5] += 1f;
                else if (note.Kind == "foul")
                    values[6] += 1f;
                else if (note.Kind == "yellow")
                    values[7] += 1f;
            }
            var total = _match.HomePossession + _match.AwayPossession;
            values[8] = total <= 0.01f ? 50f : (home ? _match.HomePossession : _match.AwayPossession) / total * 100f;
            values[3] = Distance(home);
            return values;
        }

        static string[] Format(float[] values, bool english)
        {
            return new[]
            {
                values[0].ToString("0"),
                values[1].ToString("0"),
                values[2].ToString("0.00"),
                values[3].ToString("0.0") + " km",
                Mathf.RoundToInt(values[4]) + "%",
                values[5].ToString("0"),
                values[6].ToString("0"),
                values[7].ToString("0"),
                Mathf.RoundToInt(values[8]) + "%"
            };
        }

        float Distance(bool home)
        {
            var metres = 0f;
            Vector2[] previous = null;
            for (var i = 0; i < _match.PositionSamples.Count; i++)
            {
                var sample = home ? _match.PositionSamples[i].HomePositions : _match.PositionSamples[i].AwayPositions;
                if (sample == null)
                    continue;
                if (previous != null)
                {
                    var count = Mathf.Min(previous.Length, sample.Length);
                    for (var p = 0; p < count; p++)
                    {
                        var delta = sample[p] - previous[p];
                        metres += Mathf.Sqrt(delta.y * delta.y * 105f * 105f + delta.x * delta.x * 68f * 68f);
                    }
                }
                previous = sample;
            }
            return metres / 1000f;
        }

        void PaintEvents()
        {
            var shown = 0;
            for (var i = _match.Notes.Count - 1; i >= 0 && shown < RowCount; i--)
            {
                var note = _match.Notes[i];
                if (note.Kind == "touch" || note.Kind == "pass" || note.Kind == "solo" || note.Kind == "half" || note.Kind == "full")
                    continue;
                var row = _events[shown];
                row.Root.SetActive(true);
                row.Minute.text = note.Minute + "'";
                row.Icon.color = EventColor(note.Kind);
                var mark = row.Icon.transform.Find("Mark");
                if (mark != null)
                {
                    var label = mark.GetComponent<Text>();
                    if (label != null)
                        label.text = EventMark(note.Kind);
                }
                var who = "";
                if (!string.IsNullOrEmpty(note.PlayerId) && _match.People.TryGetValue(note.PlayerId, out var player))
                    who = player.FullName;
                else
                    who = LocalizationManager.Translate(note.Text);
                row.Name.text = who;
                shown++;
            }
            for (var i = shown; i < RowCount; i++)
                _events[i].Root.SetActive(false);
        }

        static string EventMark(string kind)
        {
            switch (kind)
            {
                case "goal": return "G";
                case "yellow": return "A";
                case "red": return "V";
                case "sub": return "S";
                case "injury": return "+";
                case "corner": return "C";
                case "foul": return "F";
                default: return "•";
            }
        }

        static Color EventColor(string kind)
        {
            switch (kind)
            {
                case "goal": return new Color(0.95f, 0.82f, 0.2f);
                case "yellow": return new Color(0.95f, 0.82f, 0.15f);
                case "red": return new Color(0.85f, 0.15f, 0.15f);
                case "sub": return new Color(0.35f, 0.7f, 1f);
                case "injury": return new Color(1f, 0.45f, 0.2f);
                case "corner": return new Color(0.4f, 0.85f, 0.55f);
                case "foul": return new Color(0.85f, 0.85f, 0.9f);
                default: return new Color(0.55f, 0.6f, 0.7f);
            }
        }

        void PaintScores()
        {
            if (_career == null)
                return;
            var lines = new List<Fixture>();
            foreach (var fixture in _career.Fixtures)
            {
                if (!fixture.Played || fixture.Walkover || fixture == _match.Fixture)
                    continue;
                if (!string.IsNullOrEmpty(fixture.HomeClubId) && fixture.HomeClubId == fixture.AwayClubId)
                    continue;
                lines.Add(fixture);
            }
            lines.Sort((a, b) => b.Date.CompareTo(a.Date));
            var count = Mathf.Min(RowCount, lines.Count);
            for (var i = 0; i < count; i++)
            {
                var fixture = lines[i];
                _career.World.Clubs.TryGetValue(fixture.HomeClubId, out var home);
                _career.World.Clubs.TryGetValue(fixture.AwayClubId, out var away);
                var row = _scores[i];
                row.Root.SetActive(true);
                row.Home.text = home == null ? "" : home.ShortName;
                row.Away.text = away == null ? "" : away.ShortName;
                row.Score.text = fixture.HomeGoals + "-" + fixture.AwayGoals;
                row.HomeCrest.sprite = home == null ? null : CrestStudio.For(home);
                row.AwayCrest.sprite = away == null ? null : CrestStudio.For(away);
                row.HomeCrest.enabled = row.HomeCrest.sprite != null;
                row.AwayCrest.enabled = row.AwayCrest.sprite != null;
            }
            for (var i = count; i < RowCount; i++)
                _scores[i].Root.SetActive(false);
        }

        void PaintMomentum()
        {
            var peak = 1;
            var home = new int[12];
            var away = new int[12];
            for (var i = 0; i < _match.Shots.Count; i++)
            {
                var slice = Mathf.Clamp(Mathf.FloorToInt(_match.Shots[i].Minute / 8f), 0, 11);
                if (_match.Shots[i].Home)
                    home[slice]++;
                else
                    away[slice]++;
                peak = Mathf.Max(peak, Mathf.Max(home[slice], away[slice]));
            }
            for (var i = 0; i < 12; i++)
            {
                var delta = home[i] - away[i];
                var height = Mathf.Clamp(Mathf.Abs(delta) / (float)peak, 0.04f, 0.38f);
                var x = 0.1f + i * 0.07f;
                if (delta >= 0)
                {
                    _momentum[i].rectTransform.anchorMin = new Vector2(x, 0.47f);
                    _momentum[i].rectTransform.anchorMax = new Vector2(x + 0.045f, 0.47f + height);
                    _momentum[i].color = new Color(0.45f, 0.75f, 1f);
                }
                else
                {
                    _momentum[i].rectTransform.anchorMin = new Vector2(x, 0.47f - height);
                    _momentum[i].rectTransform.anchorMax = new Vector2(x + 0.045f, 0.47f);
                    _momentum[i].color = new Color(1f, 0.55f, 0.35f);
                }
            }
        }

        void CycleDeskCard()
        {
            _deskCard = (_deskCard + 1) % 3;
            _drawnShots = -1;
            Paint();
        }

        void PaintXg(bool english)
        {
            var titles = english
                ? new[] { "xG GRAPH", "SHOT MAP", "PASS MAP" }
                : new[] { "HISTÓRIA DO xG", "MAPA DE REMATES", "MAPA DE PASSES" };
            var title = transform.Find("Xg/Title")?.GetComponent<Text>();
            if (title != null)
                title.text = titles[_deskCard];
            var home = 0f;
            var away = 0f;
            for (var i = 0; i < _match.Shots.Count; i++)
            {
                if (_match.Shots[i].Home)
                    home += _match.Shots[i].ExpectedGoals;
                else
                    away += _match.Shots[i].ExpectedGoals;
            }
            if (_deskCard == 0)
                _xg.text = (english ? "Goals " : "Golos ") + _match.HomeGoals + "-" + _match.AwayGoals + "   xG " + home.ToString("0.00", CultureInfo.InvariantCulture) + " - " + away.ToString("0.00", CultureInfo.InvariantCulture);
            else if (_deskCard == 1)
                _xg.text = (english ? "Shots placed on the pitch. ▼ swaps the card." : "Remates no campo. ▼ troca o cartão.");
            else
                _xg.text = (english ? "Completed passes. ▼ swaps the card." : "Passes completos. ▼ troca o cartão.");
            var stamp = _match.Shots.Count + _match.Passes.Count * 17 + _deskCard * 1000;
            if (_plot == null || _drawnShots == stamp)
                return;
            if (_deskCard != 0 && _plot.rect.width < 20f)
                return;
            _drawnShots = stamp;
            for (var i = 0; i < _plotMarks.Count; i++)
            {
                if (_plotMarks[i] != null)
                    Destroy(_plotMarks[i].gameObject);
            }
            _plotMarks.Clear();
            var mini = _plot.Find("MiniPitch");
            if (_deskCard == 0)
            {
                if (mini != null)
                    mini.gameObject.SetActive(false);
            }
            else
            {
                if (mini == null)
                {
                    var pitch = BroadcastUi.Zone(_plot, "MiniPitch", 0f, 0f, 1f, 1f);
                    BroadcastUi.DrawVerticalPitch(pitch, new Color(0.1f, 0.42f, 0.2f));
                    mini = pitch;
                }
                mini.gameObject.SetActive(true);
                mini.SetAsFirstSibling();
            }
            if (_deskCard == 1)
            {
                for (var i = 0; i < _match.Shots.Count; i++)
                {
                    var shot = _match.Shots[i];
                    var mark = BroadcastUi.Solid(_plot, "Shot" + i, shot.Home ? new Color(0.45f, 0.75f, 1f) : new Color(1f, 0.55f, 0.35f));
                    mark.sprite = BroadcastUi.Circle;
                    mark.raycastTarget = false;
                    var origin = new Vector2(Mathf.Clamp01(shot.Origin.x), Mathf.Clamp01(shot.Origin.y));
                    mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = origin;
                    mark.rectTransform.sizeDelta = new Vector2(10f, 10f);
                    _plotMarks.Add(mark);
                }
                return;
            }
            if (_deskCard == 2)
            {
                var shown = 0;
                var width = Mathf.Max(80f, _plot.rect.width);
                var height = Mathf.Max(80f, _plot.rect.height);
                for (var i = _match.Passes.Count - 1; i >= 0 && shown < 24; i--)
                {
                    var pass = _match.Passes[i];
                    if (pass == null || !pass.Resolved || !pass.Completed)
                        continue;
                    var origin = new Vector2(Mathf.Clamp01(pass.Origin.x), Mathf.Clamp01(pass.Origin.y));
                    var target = new Vector2(Mathf.Clamp01(pass.Target.x), Mathf.Clamp01(pass.Target.y));
                    var delta = target - origin;
                    var length = new Vector2(delta.x * width, delta.y * height).magnitude;
                    var mid = (origin + target) * 0.5f;
                    var color = pass.Home ? new Color(0.55f, 0.82f, 1f, 0.9f) : new Color(1f, 0.62f, 0.4f, 0.9f);
                    var line = BroadcastUi.Solid(_plot, "Pass" + shown, color);
                    line.raycastTarget = false;
                    line.rectTransform.anchorMin = line.rectTransform.anchorMax = mid;
                    line.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    line.rectTransform.sizeDelta = new Vector2(Mathf.Clamp(length, 4f, Mathf.Max(width, height)), 2.5f);
                    line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y * height, delta.x * width) * Mathf.Rad2Deg);
                    _plotMarks.Add(line);
                    shown++;
                }
                return;
            }
            var runningHome = 0f;
            var runningAway = 0f;
            var peak = Mathf.Max(0.2f, Mathf.Max(home, away));
            var seenHome = false;
            var seenAway = false;
            var prevHome = Vector2.zero;
            var prevAway = Vector2.zero;
            for (var i = 0; i < _match.Shots.Count; i++)
            {
                var shot = _match.Shots[i];
                if (shot.Home)
                    runningHome += shot.ExpectedGoals;
                else
                    runningAway += shot.ExpectedGoals;
                var point = new Vector2(Mathf.Clamp01(shot.Minute / 90f), Mathf.Clamp01((shot.Home ? runningHome : runningAway) / peak));
                var color = shot.Home ? new Color(0.45f, 0.75f, 1f) : new Color(1f, 0.55f, 0.35f);
                var previous = shot.Home ? prevHome : prevAway;
                var seen = shot.Home ? seenHome : seenAway;
                if (seen)
                {
                    var flat = BroadcastUi.Solid(_plot, "Flat" + i, color);
                    flat.raycastTarget = false;
                    flat.rectTransform.anchorMin = new Vector2(Mathf.Min(previous.x, point.x), point.y - 0.012f);
                    flat.rectTransform.anchorMax = new Vector2(Mathf.Max(previous.x, point.x), point.y + 0.012f);
                    flat.rectTransform.offsetMin = Vector2.zero;
                    flat.rectTransform.offsetMax = Vector2.zero;
                    _plotMarks.Add(flat);
                    var rise = BroadcastUi.Solid(_plot, "Rise" + i, color);
                    rise.raycastTarget = false;
                    rise.rectTransform.anchorMin = new Vector2(point.x - 0.008f, Mathf.Min(previous.y, point.y));
                    rise.rectTransform.anchorMax = new Vector2(point.x + 0.008f, Mathf.Max(previous.y, point.y));
                    rise.rectTransform.offsetMin = Vector2.zero;
                    rise.rectTransform.offsetMax = Vector2.zero;
                    _plotMarks.Add(rise);
                }
                if (shot.Home)
                {
                    seenHome = true;
                    prevHome = point;
                }
                else
                {
                    seenAway = true;
                    prevAway = point;
                }
            }
        }

        void PaintAdvice(bool english)
        {
            if (!_adviceReady && _match.Minute >= _nextAdviceMinute)
                PrepareAdvice(english);
        }

        void PrepareAdvice(bool english)
        {
            var userHome = _career != null && _career.Coach != null && _career.Coach.ClubId == _match.Home.Id;
            var club = _career != null && _career.Club != null ? _career.Club : (userHome ? _match.Home : _match.Away);
            var assistant = StaffReport.Find(club, StaffRole.AssistantCoach);
            var attack = StaffProfile.Skill(assistant, "Attacking", StaffProfile.Bias(assistant == null ? StaffRole.AssistantCoach : assistant.Role, "Attacking"));
            var defend = StaffProfile.Skill(assistant, "Defending", StaffProfile.Bias(assistant == null ? StaffRole.AssistantCoach : assistant.Role, "Defending"));
            var motivate = StaffProfile.Skill(assistant, "Motivation", StaffProfile.Bias(assistant == null ? StaffRole.AssistantCoach : assistant.Role, "Motivation"));
            var judging = StaffProfile.Skill(assistant, "Judging", StaffProfile.Bias(assistant == null ? StaffRole.AssistantCoach : assistant.Role, "Judging"));
            var userGoals = userHome ? _match.HomeGoals : _match.AwayGoals;
            var otherGoals = userHome ? _match.AwayGoals : _match.HomeGoals;
            var weak = assistant == null || assistant.Judgement < 9;
            if (!weak && otherGoals > userGoals && attack >= defend)
                _adviceKind = "attack";
            else if (!weak && userGoals > otherGoals && _match.Minute >= 70f && defend >= attack)
                _adviceKind = "deep";
            else if (judging >= motivate)
                _adviceKind = "mark";
            else
                _adviceKind = "motivate";
            if (weak && _match.Minute > 20f)
                _adviceKind = "attack";

            _advicePlayerId = null;
            if (_adviceKind == "attack")
                _dugout.text = english
                    ? assistantName(assistant, true) + " sees room behind " + OpponentName(userHome) + "'s line. Raise the tempo and attack the space."
                    : assistantName(assistant, false) + " vê espaço nas costas da defesa do " + OpponentName(userHome) + ". Sobe o ritmo e ataca esse espaço.";
            else if (_adviceKind == "deep")
                _dugout.text = english
                    ? assistantName(assistant, true) + " wants the block compact against " + OpponentName(userHome) + ". Protect the lead and slow the game."
                    : assistantName(assistant, false) + " quer o bloco compacto contra " + OpponentName(userHome) + ". Protege a vantagem e abranda o jogo.";
            else if (_adviceKind == "mark")
            {
                _advicePlayerId = BestOpponent(userHome);
                var name = _advicePlayerId != null && _match.People.TryGetValue(_advicePlayerId, out var player)
                    ? BroadcastUi.ShortName(player.FullName) : OpponentName(userHome);
                _dugout.text = english
                    ? assistantName(assistant, true) + " has identified " + name + " as their main threat. Stay tight and deny the first pass."
                    : assistantName(assistant, false) + " identificou " + name + " como principal ameaça. Fecha o espaço e impede o primeiro passe.";
            }
            else
                _dugout.text = english
                    ? assistantName(assistant, true) + " sees the team losing its edge. A short, positive reminder should lift the group."
                    : assistantName(assistant, false) + " acha que o grupo precisa de uma palavra. Motiva-os.";
            _adviceReady = true;
            SetAdviceControls(true);
        }

        static string assistantName(StaffMember assistant, bool english = false)
        {
            if (assistant == null || string.IsNullOrEmpty(assistant.FullName))
                return english ? "The assistant" : "O adjunto";
            var parts = assistant.FullName.Split(' ');
            return parts.Length == 0 ? assistant.FullName : parts[0];
        }

        string OpponentName(bool userHome)
        {
            return userHome ? _match.Away.ShortName : _match.Home.ShortName;
        }

        string BestOpponent(bool userHome)
        {
            var squad = userHome ? _match.AwayOnField : _match.HomeOnField;
            string bestId = null;
            var best = -1;
            for (var i = 0; i < squad.Count; i++)
            {
                if (!_match.People.TryGetValue(squad[i], out var player))
                    continue;
                var overall = PlayerAttributes.Overall(player);
                if (overall <= best)
                    continue;
                best = overall;
                bestId = player.Id;
            }
            return bestId;
        }

        void ApplyAdvice()
        {
            if (!_adviceReady)
                return;
            var userHome = _career != null && _career.Coach != null && _career.Coach.ClubId == _match.Home.Id;
            var squad = userHome ? _match.HomeOnField : _match.AwayOnField;
            var appliedText = _dugout.text;
            if (_adviceKind == "motivate")
            {
                for (var i = 0; i < squad.Count; i++)
                {
                    if (_match.People.TryGetValue(squad[i], out var player))
                        player.Morale = Mathf.Clamp(player.Morale + 4, 1, 99);
                }
            }
            else if (_adviceKind == "attack")
            {
                if (userHome)
                    _match.AttackBoost = Mathf.Min(0.12f, _match.AttackBoost + 0.05f);
                else
                    _match.AwayAttackBoost = Mathf.Min(0.12f, _match.AwayAttackBoost + 0.05f);
            }
            else if (_adviceKind == "deep")
            {
                SetTeamBoost(userHome, true, -0.035f);
                SetTeamBoost(userHome, false, 0.025f);
            }
            else if (_adviceKind == "mark")
                SetTeamBoost(userHome, false, 0.025f);
            ConsumeAdvice(appliedText);
        }

        void DismissAdvice()
        {
            if (!_adviceReady)
                return;
            _match.AddNote(LocalizationManager.Current == GameLanguage.English
                ? "The manager ignored the assistant's suggestion."
                : "O treinador ignorou a sugestão do adjunto.", "order");
            ConsumeAdvice(null);
        }

        void ToggleCalloutMenu()
        {
            if (!_adviceReady)
                return;
            if (_calloutMenu != null)
            {
                Destroy(_calloutMenu);
                _calloutMenu = null;
                return;
            }

            var english = LocalizationManager.Current == GameLanguage.English;
            var host = transform.Find("Dugout");
            var menu = BroadcastUi.Panel(host, "CalloutMenu", new Color(0.055f, 0.045f, 0.12f, 0.99f), true);
            _calloutMenu = menu.gameObject;
            menu.transform.SetAsLastSibling();
            BroadcastUi.Place(menu.rectTransform, 0.035f, 0.205f, 0.965f, 0.72f);
            var keys = new[] { "motivate", "calm", "demand", "safe", "press", "slow" };
            var labels = english
                ? new[] { "MOTIVATE", "CALM DOWN", "DEMAND MORE", "PLAY IT SAFE", "PRESS HIGHER", "SLOW IT DOWN" }
                : new[] { "MOTIVAR", "ACALMAR", "EXIGIR MAIS", "JOGAR PELO SEGURO", "PRESSIONAR MAIS ALTO", "ABRANDAR O JOGO" };
            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                var yTop = 0.97f - i * 0.16f;
                var option = BroadcastUi.Button(menu.transform, "Callout" + i, labels[i],
                    () => ApplyCallout(key), new Color(1f, 1f, 1f, i % 2 == 0 ? 0.1f : 0.055f), Color.white, 10);
                BroadcastUi.Place(option.GetComponent<RectTransform>(), 0.035f, yTop - 0.145f, 0.965f, yTop - 0.01f);
            }
        }

        void ApplyCallout(string kind)
        {
            if (!_adviceReady)
                return;
            var english = LocalizationManager.Current == GameLanguage.English;
            var userHome = _career != null && _career.Coach != null && _career.Coach.ClubId == _match.Home.Id;
            var squad = userHome ? _match.HomeOnField : _match.AwayOnField;
            var message = "";
            switch (kind)
            {
                case "motivate":
                    foreach (var id in squad)
                        if (_match.People.TryGetValue(id, out var motivated))
                            motivated.Morale = Mathf.Clamp(motivated.Morale + 4, 1, 99);
                    SetTeamBoost(userHome, true, 0.015f);
                    message = english ? "The manager urged the players to believe and keep going." : "O treinador pediu aos jogadores que acreditassem e continuassem.";
                    break;
                case "calm":
                    foreach (var id in squad)
                        if (_match.People.TryGetValue(id, out var settled))
                            settled.Morale = Mathf.Clamp(settled.Morale + 2, 1, 99);
                    SetTeamBoost(userHome, false, 0.01f);
                    message = english ? "The manager asked the team to stay calm and keep its shape." : "O treinador pediu calma e organização à equipa.";
                    break;
                case "demand":
                    SetTeamBoost(userHome, true, 0.035f);
                    message = english ? "The manager demanded more intensity and sharper decisions." : "O treinador exigiu mais intensidade e melhores decisões.";
                    break;
                case "safe":
                    SetTeamBoost(userHome, true, -0.035f);
                    SetTeamBoost(userHome, false, 0.03f);
                    message = english ? "The manager told the team to protect its shape and play safely." : "O treinador pediu à equipa que protegesse a estrutura e jogasse pelo seguro.";
                    break;
                case "press":
                    SetTeamBoost(userHome, true, 0.02f);
                    SetTeamBoost(userHome, false, -0.012f);
                    message = english ? "The manager called for a higher press and an immediate reaction." : "O treinador pediu pressão mais alta e reação imediata.";
                    break;
                default:
                    SetTeamBoost(userHome, true, -0.02f);
                    SetTeamBoost(userHome, false, 0.012f);
                    message = english ? "The manager asked the team to keep the ball and take time out of the game." : "O treinador pediu para manter a bola e controlar o ritmo.";
                    break;
            }
            ConsumeAdvice(message);
        }

        void SetTeamBoost(bool home, bool attack, float amount)
        {
            if (home)
            {
                if (attack) _match.AttackBoost = Mathf.Clamp(_match.AttackBoost + amount, -0.08f, 0.12f);
                else _match.DefenseBoost = Mathf.Clamp(_match.DefenseBoost + amount, 0f, 0.12f);
            }
            else
            {
                if (attack) _match.AwayAttackBoost = Mathf.Clamp(_match.AwayAttackBoost + amount, -0.08f, 0.12f);
                else _match.AwayDefenseBoost = Mathf.Clamp(_match.AwayDefenseBoost + amount, 0f, 0.12f);
            }
        }

        void ConsumeAdvice(string note)
        {
            if (!string.IsNullOrEmpty(note))
                _match.AddNote(note, "order");
            if (_calloutMenu != null)
            {
                Destroy(_calloutMenu);
                _calloutMenu = null;
            }
            _dugout.text = "";
            _adviceReady = false;
            _nextAdviceMinute = _match.Minute + 8f;
            SetAdviceControls(false);
        }

        void SetAdviceControls(bool active)
        {
            if (_suggestionButton != null) _suggestionButton.interactable = active;
            if (_calloutButton != null) _calloutButton.interactable = active;
            if (_ignoreButton != null) _ignoreButton.interactable = active;
        }
    }
}
