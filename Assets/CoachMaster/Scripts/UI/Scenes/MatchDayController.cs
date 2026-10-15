using System;
using System.Collections.Generic;
using CoachMaster.App;
using CoachMaster.Career;
using CoachMaster.Domain;
using CoachMaster.MatchPlay;
using CoachMaster.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CoachMaster.UI.Scenes
{
    public sealed class MatchDayController : MonoBehaviour
    {
        enum BoardTab
        {
            Fitness,
            Ratings,
            Stats,
            Plan
        }

        MatchSession _match;
        MatchEngine _engine;
        CareerState _career;
        bool _paused;
        float _speed = 1.35f;
        BoardTab _tab;
        bool _homeXi = true;
        bool _awayXi = true;
        int _subOff = 10;
        int _subOn;
        int _noteCount = -1;
        bool _snap = true;
        float _slow;

        Text _score;
        Text _clock;
        Text _ticker;
        RectTransform _pitch;
        RectTransform _ball;
        Image _ballImage;
        Color _homeColor = Color.white;
        Color _awayColor = new Color(0.85f, 0.2f, 0.2f);
        Token[] _homeTokens;
        Token[] _awayTokens;
        SideColumn _homeCol;
        SideColumn _awayCol;
        GameObject _fitnessPage;
        GameObject _ratingsPage;
        GameObject _statsPage;
        GameObject _pause;
        GameObject _shape;
        RectTransform _shapeHost;
        Button[] _formationButtons;
        GameObject _halfTime;
        GameObject _fullTime;
        GameObject _footer;
            Text _pauseBody;
            Text _fullResult;
        Text _flash;
        Image _eventFace;
        Image _eventStrip;
        Text _eventWho;
        GameObject _eventCard;
        int _flashSeen = -1;
        float _flashLeft;
        string _heard;
        bool _halfWhistle;
        bool _fullWhistle;
        GameObject _tactics;
        GameObject _subsMenu;
        RectTransform _subHost;
        Button[] _pressButtons;
        Button[] _lineButtons;
        string[] _pressOptions;
        string[] _lineOptions;
        string _queueOff;
        string _queueOn;
        string _pickOff;
        string _pickOn;
        bool _subFromPause;
        RectTransform _timeline;
        Text _homePoss;
        Text _awayPoss;
        Text _homeShots;
        Text _awayShots;
        Text _homeChances;
        Text _awayChances;
        Text _homeRatings;
        Text _awayRatings;
        Button[] _tabs;
        Text _speedLabel;

        void Start()
        {
            _match = GameRoot.Require().LiveMatch;
            if (_match == null)
            {
                GameRoot.Instance.Open(SceneNames.CareerHub);
                return;
            }

            _career = GameRoot.Instance.Session.Career;
            _engine = new MatchEngine(_match);
            GameAudio.Kickoff();
            GameAudio.StartCrowd();
            Build();
        }

        void Update()
        {
            if (_match == null)
                return;

            if (_match.Phase == MatchPhase.HalfTime)
            {
                if (!_halfWhistle)
                {
                    _halfWhistle = true;
                    GameAudio.Whistle();
                }
                _halfTime.SetActive(true);
                _footer.SetActive(false);
                RefreshHud();
                return;
            }

            if (_match.Phase == MatchPhase.FullTime)
            {
                if (!_fullWhistle)
                {
                    _fullWhistle = true;
                    GameAudio.Whistle();
                    GameAudio.StopCrowd();
                }
                _fullTime.SetActive(true);
                _footer.SetActive(false);
                _fullResult.text = _match.Home.ShortName + "   " + _match.HomeGoals + "  -  " + _match.AwayGoals + "   " + _match.Away.ShortName;
                RefreshHud();
                return;
            }

            _halfTime.SetActive(false);
            _fullTime.SetActive(false);
            _footer.SetActive(true);
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && _match.Phase != MatchPhase.HalfTime && _match.Phase != MatchPhase.FullTime)
            {
                if (_subsMenu != null && _subsMenu.activeSelf)
                    CloseSubs();
                else if (_tactics != null && _tactics.activeSelf)
                    CloseTactics();
                else if (_paused)
                    ResumeMatch();
                else
                    OpenPause();
            }

            var overlay = (_pause != null && _pause.activeSelf) || (_tactics != null && _tactics.activeSelf) || (_subsMenu != null && _subsMenu.activeSelf);
            if (_paused && !overlay)
            {
                _paused = false;
                SetFooter(true);
            }

            if (!_paused)
                _engine.Tick(Time.deltaTime, _speed);
            TryQueuedSub();

            PlaceTokens();
            RefreshHud();
            _slow -= Time.deltaTime;
            if (_slow <= 0f)
            {
                _slow = 0.3f;
                RefreshSides();
                RefreshRatings();
                RefreshStats();
                RefreshPlan();
            }
        }

        void Build()
        {
            var root = BroadcastUi.Mount(this);
            BroadcastUi.Backdrop(root);
            var league = _career != null && _career.League != null ? _career.League.Name : "Liga";
            BroadcastUi.Wordmark(root, league);
            BuildScoreboard(root);
            BuildFitness(root);
            BuildRatings(root);
            BuildStats(root);
            BuildTabs(root);
            BuildFooter(root);
            BuildPause(root);
            BuildTactics(root);
            BuildShape(root);
            BuildSubs(root);
            BuildHalf(root);
            BuildFull(root);
            Show(BoardTab.Fitness);
        }

        void BuildScoreboard(Transform root)
        {
            var bar = BroadcastUi.Zone(root, "Scoreboard", 0.04f, 0.8f, 0.96f, 0.91f);
            var home = BroadcastUi.Zone(bar, "Home", 0.02f, 0.08f, 0.36f, 0.92f);
            TeamSide(home, _match.Home, true);
            _score = BroadcastUi.Label(bar, "Score", "0  -  0", 54, UiTheme.Score, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(_score.rectTransform, 0.36f, 0.28f, 0.64f, 0.92f);
            _clock = BroadcastUi.Label(bar, "Clock", "0:00", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(_clock.rectTransform, 0.36f, 0.02f, 0.64f, 0.34f);
            var away = BroadcastUi.Zone(bar, "Away", 0.64f, 0.08f, 0.98f, 0.92f);
            TeamSide(away, _match.Away, false);
            _ticker = BroadcastUi.Label(root, "Ticker", "", 16, UiTheme.Muted, TextAnchor.MiddleCenter);
            BroadcastUi.Place(_ticker.rectTransform, 0.18f, 0.755f, 0.82f, 0.795f);
            _flash = BroadcastUi.Label(root, "Flash", "", 42, UiTheme.Score, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(_flash.rectTransform, 0.3f, 0.58f, 0.7f, 0.7f);
            _flash.gameObject.SetActive(false);
            _eventCard = BroadcastUi.Panel(root, "EventCard", new Color(0.08f, 0.03f, 0.16f, 0.97f), true).gameObject;
            BroadcastUi.Place(_eventCard.GetComponent<RectTransform>(), 0.32f, 0.32f, 0.68f, 0.68f);
            var faceGo = new GameObject("EventFace", typeof(RectTransform), typeof(Image));
            var faceRect = faceGo.GetComponent<RectTransform>();
            faceRect.SetParent(_eventCard.transform, false);
            faceRect.anchorMin = faceRect.anchorMax = new Vector2(0.18f, 0.42f);
            faceRect.sizeDelta = new Vector2(140, 140);
            _eventFace = faceGo.GetComponent<Image>();
            _eventFace.preserveAspect = true;
            _eventStrip = BroadcastUi.Solid(_eventCard.transform, "Strip", new Color(0.95f, 0.82f, 0.12f));
            BroadcastUi.Place(_eventStrip.rectTransform, 0.62f, 0.22f, 0.72f, 0.78f);
            _eventStrip.gameObject.SetActive(false);
            _eventWho = BroadcastUi.Label(_eventCard.transform, "Who", "", 28, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(_eventWho.rectTransform, 0.28f, 0.08f, 0.94f, 0.28f);
            _eventCard.SetActive(false);
        }

        void TeamSide(RectTransform area, Club club, bool home)
        {
            var kit = new GameObject("Kit", typeof(RectTransform), typeof(Image));
            var kitRect = kit.GetComponent<RectTransform>();
            kitRect.SetParent(area, false);
            kitRect.anchorMin = kitRect.anchorMax = new Vector2(home ? 0.78f : 0.22f, 0.5f);
            kitRect.sizeDelta = new Vector2(42, 50);
            var kitImage = kit.GetComponent<Image>();
            kitImage.raycastTarget = false;
            kitImage.preserveAspect = true;
            KitView.Apply(kitImage, club);

            var dot = BroadcastUi.Solid(area, "Dot", BroadcastUi.Parse(club.JerseyPrimaryHex, Color.white));
            dot.sprite = BroadcastUi.Circle;
            dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(home ? 0.62f : 0.38f, 0.5f);
            dot.rectTransform.sizeDelta = new Vector2(16, 16);

            var name = BroadcastUi.Label(area, "Name", club.ShortName, 26, Color.white, home ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(name.rectTransform, home ? 0f : 0.46f, 0.1f, home ? 0.56f : 1f, 0.9f);
        }

        void BuildFitness(Transform root)
        {
            _fitnessPage = BroadcastUi.Zone(root, "Fitness", 0f, 0.12f, 1f, 0.75f).gameObject;
            var page = _fitnessPage.transform;
            _homeCol = BuildColumn(page, "HomeXi", 0.015f, 0.02f, 0.22f, 0.98f, true);
            var pitchZone = BroadcastUi.Zone(page, "PitchZone", 0.23f, 0.03f, 0.77f, 0.97f);
            _pitch = BroadcastUi.Zone(pitchZone, "Pitch", 0.04f, 0.04f, 0.96f, 0.96f);
            BroadcastUi.DrawPitch(_pitch, new Color(0.12f, 0.46f, 0.22f));
            _homeTokens = SpawnTokens(true, BroadcastUi.Parse(_match.Home.JerseyPrimaryHex, Color.white));
            _awayTokens = SpawnTokens(false, BroadcastUi.Parse(_match.Away.JerseyPrimaryHex, new Color(0.85f, 0.2f, 0.2f)));
            _homeColor = BroadcastUi.Parse(_match.Home.JerseyPrimaryHex, Color.white);
            _awayColor = BroadcastUi.Parse(_match.Away.JerseyPrimaryHex, new Color(0.85f, 0.2f, 0.2f));
            _ball = SpawnBall();
            _awayCol = BuildColumn(page, "AwayXi", 0.78f, 0.02f, 0.985f, 0.98f, false);
        }

        SideColumn BuildColumn(Transform parent, string name, float x0, float y0, float x1, float y1, bool home)
        {
            var area = BroadcastUi.Zone(parent, name, x0, y0, x1, y1);
            var col = new SideColumn();
            col.Root = area.gameObject;
            var xi = BroadcastUi.Button(area, home ? "HomeXiTab" : "AwayXiTab", "11 Inicial", () =>
            {
                if (home) _homeXi = true;
                else _awayXi = true;
                _slow = 0f;
            }, new Color(1f, 1f, 1f, 0.08f), Color.white, 14);
            BroadcastUi.Place(xi.GetComponent<RectTransform>(), 0.02f, 0.9f, 0.5f, 0.99f);
            var bench = BroadcastUi.Button(area, home ? "HomeBenchTab" : "AwayBenchTab", "Banco", () =>
            {
                if (home) _homeXi = false;
                else _awayXi = false;
                _slow = 0f;
            }, new Color(1f, 1f, 1f, 0.08f), UiTheme.Muted, 14);
            BroadcastUi.Place(bench.GetComponent<RectTransform>(), 0.52f, 0.9f, 0.98f, 0.99f);
            col.Xi = xi;
            col.Bench = bench;
            col.Numbers = new Text[11];
            col.Names = new Text[11];
            col.Fills = new Image[11];
            col.Scores = new Text[11];
            col.Marks = new Text[11];
            col.Cards = new Image[11];
            col.Balls = new Image[11];
            col.Boots = new Image[11];
            col.Pens = new Text[11];
            col.Rows = new GameObject[11];
            for (var i = 0; i < 11; i++)
            {
                var rowTop = 0.88f - i * 0.078f;
                var row = BroadcastUi.Zone(area, "Row" + i, 0.02f, rowTop - 0.07f, 0.98f, rowTop);
                col.Rows[i] = row.gameObject;
                if (home)
                {
                    col.Numbers[i] = BroadcastUi.Label(row, "Num", "", 16, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                    BroadcastUi.Place(col.Numbers[i].rectTransform, 0f, 0f, 0.16f, 1f);
                    col.Names[i] = BroadcastUi.Label(row, "Name", "", 16, Color.white, TextAnchor.MiddleLeft);
                    BroadcastUi.Place(col.Names[i].rectTransform, 0.16f, 0f, 0.5f, 1f);
                    col.Marks[i] = BroadcastUi.Label(row, "Card", "", 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                    col.Marks[i].gameObject.SetActive(false);
                    col.Cards[i] = BroadcastUi.Solid(row, "CardRect", new Color(0.95f, 0.82f, 0.15f));
                    BroadcastUi.Place(col.Cards[i].rectTransform, 0.5f, 0.2f, 0.548f, 0.8f);
                    col.Cards[i].gameObject.SetActive(false);
                    col.Balls[i] = BroadcastUi.Solid(row, "Ball", Color.white);
                    col.Balls[i].sprite = BroadcastUi.Circle;
                    col.Balls[i].rectTransform.anchorMin = col.Balls[i].rectTransform.anchorMax = new Vector2(0.58f, 0.5f);
                    col.Balls[i].rectTransform.sizeDelta = new Vector2(16, 16);
                    col.Balls[i].gameObject.SetActive(false);
                    col.Boots[i] = BroadcastUi.Solid(row, "Boot", new Color(1f, 1f, 1f, 0f));
                    var assist = BroadcastUi.Label(col.Boots[i].rectTransform, "A", "A", 13, new Color(0.95f, 0.7f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold);
                    BroadcastUi.Stretch(assist.rectTransform);
                    col.Boots[i].rectTransform.anchorMin = col.Boots[i].rectTransform.anchorMax = new Vector2(0.66f, 0.5f);
                    col.Boots[i].rectTransform.sizeDelta = new Vector2(16, 16);
                    col.Boots[i].gameObject.SetActive(false);
                    col.Pens[i] = BroadcastUi.Label(row, "Pen", "", 14, UiTheme.Score, TextAnchor.MiddleCenter, FontStyle.Bold);
                    BroadcastUi.Place(col.Pens[i].rectTransform, 0.7f, 0f, 0.76f, 1f);
                    col.Fills[i] = Bar(row, 0.74f);
                    col.Scores[i] = BroadcastUi.Label(row, "Score", "", 16, UiTheme.Score, TextAnchor.MiddleRight, FontStyle.Bold);
                    BroadcastUi.Place(col.Scores[i].rectTransform, 0.72f, 0f, 0.98f, 1f);
                    col.Scores[i].gameObject.SetActive(false);
                }
                else
                {
                    col.Fills[i] = Bar(row, 0.02f);
                    col.Scores[i] = BroadcastUi.Label(row, "Score", "", 16, UiTheme.Score, TextAnchor.MiddleLeft, FontStyle.Bold);
                    BroadcastUi.Place(col.Scores[i].rectTransform, 0.02f, 0f, 0.24f, 1f);
                    col.Scores[i].gameObject.SetActive(false);
                    col.Marks[i] = BroadcastUi.Label(row, "Card", "", 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                    col.Marks[i].gameObject.SetActive(false);
                    col.Pens[i] = BroadcastUi.Label(row, "Pen", "", 14, UiTheme.Score, TextAnchor.MiddleCenter, FontStyle.Bold);
                    BroadcastUi.Place(col.Pens[i].rectTransform, 0.08f, 0f, 0.16f, 1f);
                    col.Boots[i] = BroadcastUi.Solid(row, "Boot", new Color(1f, 1f, 1f, 0f));
                    var assistAway = BroadcastUi.Label(col.Boots[i].rectTransform, "A", "A", 13, new Color(0.95f, 0.7f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold);
                    BroadcastUi.Stretch(assistAway.rectTransform);
                    col.Boots[i].rectTransform.anchorMin = col.Boots[i].rectTransform.anchorMax = new Vector2(0.2f, 0.5f);
                    col.Boots[i].rectTransform.sizeDelta = new Vector2(16, 16);
                    BroadcastUi.Place(col.Boots[i].rectTransform, 0.16f, 0.22f, 0.24f, 0.78f);
                    col.Boots[i].gameObject.SetActive(false);
                    col.Balls[i] = BroadcastUi.Solid(row, "Ball", Color.white);
                    col.Balls[i].sprite = BroadcastUi.Circle;
                    col.Balls[i].rectTransform.anchorMin = col.Balls[i].rectTransform.anchorMax = new Vector2(0.28f, 0.5f);
                    col.Balls[i].rectTransform.sizeDelta = new Vector2(16, 16);
                    col.Balls[i].gameObject.SetActive(false);
                    col.Cards[i] = BroadcastUi.Solid(row, "CardRect", new Color(0.95f, 0.82f, 0.15f));
                    BroadcastUi.Place(col.Cards[i].rectTransform, 0.34f, 0.2f, 0.388f, 0.8f);
                    col.Cards[i].gameObject.SetActive(false);
                    col.Names[i] = BroadcastUi.Label(row, "Name", "", 16, Color.white, TextAnchor.MiddleRight);
                    BroadcastUi.Place(col.Names[i].rectTransform, 0.4f, 0f, 0.84f, 1f);
                    col.Numbers[i] = BroadcastUi.Label(row, "Num", "", 16, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
                    BroadcastUi.Place(col.Numbers[i].rectTransform, 0.84f, 0f, 1f, 1f);
                }
            }

            return col;
        }

        Image Bar(Transform row, float x)
        {
            var bg = BroadcastUi.Solid(row, "Bar", new Color(1f, 1f, 1f, 0.16f));
            BroadcastUi.Place(bg.rectTransform, x, 0.38f, x + 0.22f, 0.62f);
            var fill = BroadcastUi.Solid(bg.transform, "Fill", UiTheme.Fitness);
            BroadcastUi.Stretch(fill.rectTransform);
            fill.rectTransform.anchorMax = new Vector2(0.8f, 1f);
            return fill;
        }

        Token[] SpawnTokens(bool home, Color color)
        {
            var list = new Token[11];
            var ink = color.grayscale > 0.62f ? new Color(0.1f, 0.08f, 0.16f) : Color.white;
            for (var i = 0; i < 11; i++)
            {
                var go = new GameObject((home ? "H" : "A") + i, typeof(RectTransform), typeof(Image));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(_pitch, false);
                rect.sizeDelta = new Vector2(30, 30);
                var image = go.GetComponent<Image>();
                image.sprite = BroadcastUi.Circle;
                image.color = color;
                image.raycastTarget = false;
                var num = BroadcastUi.Label(rect, "N", "", 18, ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Stretch(num.rectTransform);
                list[i] = new Token { Rect = rect, Number = num };
            }

            return list;
        }

        RectTransform SpawnBall()
        {
            var go = new GameObject("Ball", typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_pitch, false);
            rect.sizeDelta = new Vector2(14, 14);
            var image = go.GetComponent<Image>();
            image.sprite = BroadcastUi.Circle;
            image.color = Color.white;
            image.raycastTarget = false;
            _ballImage = image;
            return rect;
        }

        void BuildRatings(Transform root)
        {
            _ratingsPage = BroadcastUi.Panel(root, "Ratings", new Color(0.08f, 0.03f, 0.16f, 0.35f)).gameObject;
            BroadcastUi.Place(_ratingsPage.GetComponent<RectTransform>(), 0.05f, 0.14f, 0.95f, 0.76f);
            var page = _ratingsPage.transform;
            var leftTitle = BroadcastUi.Label(page, "HomeTitle", _match.Home.ShortName.ToUpperInvariant(), 18, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(leftTitle.rectTransform, 0.04f, 0.9f, 0.46f, 0.98f);
            var rightTitle = BroadcastUi.Label(page, "AwayTitle", _match.Away.ShortName.ToUpperInvariant(), 18, UiTheme.Accent, TextAnchor.MiddleRight, FontStyle.Bold);
            BroadcastUi.Place(rightTitle.rectTransform, 0.54f, 0.9f, 0.96f, 0.98f);
            _homeRatings = BroadcastUi.Label(page, "HomeRatings", "", 18, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.Place(_homeRatings.rectTransform, 0.04f, 0.04f, 0.48f, 0.88f);
            _awayRatings = BroadcastUi.Label(page, "AwayRatings", "", 18, Color.white, TextAnchor.UpperRight);
            BroadcastUi.Place(_awayRatings.rectTransform, 0.52f, 0.04f, 0.96f, 0.88f);
        }

        void BuildStats(Transform root)
        {
            _statsPage = BroadcastUi.Zone(root, "Stats", 0.03f, 0.13f, 0.97f, 0.77f).gameObject;
            var page = _statsPage.transform;
            _homePoss = StatBlock(page, "HomePoss", 0.02f, 0.62f, 0.24f, 0.96f, "Posse de bola (%)");
            _homeShots = StatBlock(page, "HomeShots", 0.02f, 0.32f, 0.24f, 0.6f, "Remates");
            _homeChances = StatBlock(page, "HomeChances", 0.02f, 0.02f, 0.24f, 0.3f, "Oportunidades");
            _awayPoss = StatBlock(page, "AwayPoss", 0.76f, 0.62f, 0.98f, 0.96f, "Posse de bola (%)");
            _awayShots = StatBlock(page, "AwayShots", 0.76f, 0.32f, 0.98f, 0.6f, "Remates");
            _awayChances = StatBlock(page, "AwayChances", 0.76f, 0.02f, 0.98f, 0.3f, "Oportunidades");
            var card = BroadcastUi.Panel(page, "TimelineCard", new Color(0.12f, 0.05f, 0.22f, 0.55f));
            card.gameObject.SetActive(false);
            var scroll = BroadcastUi.Scroll(card.transform, "Timeline", out _timeline);
            BroadcastUi.Stretch(scroll.GetComponent<RectTransform>());
            scroll.GetComponent<RectTransform>().offsetMin = new Vector2(10, 10);
            scroll.GetComponent<RectTransform>().offsetMax = new Vector2(-10, -10);
        }

        Text StatBlock(Transform parent, string name, float x0, float y0, float x1, float y1, string caption)
        {
            var zone = BroadcastUi.Zone(parent, name, x0, y0, x1, y1);
            var cap = BroadcastUi.Label(zone, "Cap", caption, 16, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(cap.rectTransform, 0f, 0.62f, 1f, 0.95f);
            var value = BroadcastUi.Label(zone, "Value", "0", 64, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(value.rectTransform, 0f, 0.05f, 1f, 0.68f);
            return value;
        }

        void BuildTabs(Transform root)
        {
            var bar = BroadcastUi.Zone(root, "Tabs", 0.12f, 0.058f, 0.88f, 0.115f);
            _tabs = new Button[3];
            _tabs[0] = TabButton(bar, "TabFit", "C. física", 0.04f, 0.32f, BoardTab.Fitness);
            _tabs[1] = TabButton(bar, "TabRate", "Pontuações", 0.36f, 0.64f, BoardTab.Ratings);
            _tabs[2] = TabButton(bar, "TabStats", "Estatísticas", 0.68f, 0.96f, BoardTab.Stats);
        }

        Button TabButton(Transform parent, string name, string label, float x0, float x1, BoardTab tab)
        {
            var button = BroadcastUi.Button(parent, name, label, () => Show(tab), new Color(1f, 1f, 1f, 0f), UiTheme.Muted, 18);
            BroadcastUi.Place(button.GetComponent<RectTransform>(), x0, 0f, x1, 1f);
            return button;
        }

        void BuildFooter(Transform root)
        {
            _footer = BroadcastUi.Zone(root, "Footer", 0f, 0f, 1f, 0.055f).gameObject;
            var pause = BroadcastUi.Button(_footer.transform, "Pause", "Pausa", OpenPause, new Color(1f, 1f, 1f, 0f), Color.white, 16);
            BroadcastUi.Place(pause.GetComponent<RectTransform>(), 0.02f, 0.08f, 0.12f, 0.92f);
            var tactics = BroadcastUi.Button(_footer.transform, "QuickTactics", "Táticas rápidas", OpenTactics, new Color(1f, 1f, 1f, 0f), Color.white, 16);
            BroadcastUi.Place(tactics.GetComponent<RectTransform>(), 0.14f, 0.08f, 0.32f, 0.92f);
            var sub = BroadcastUi.Button(_footer.transform, "QuickSub", "Substituição", () => OpenSubs(false), new Color(1f, 1f, 1f, 0f), Color.white, 16);
            BroadcastUi.Place(sub.GetComponent<RectTransform>(), 0.34f, 0.08f, 0.5f, 0.92f);
            var speed = BroadcastUi.Button(_footer.transform, "Speed", "Ritmo x1", CycleSpeed, new Color(1f, 1f, 1f, 0f), UiTheme.Accent, 16);
            BroadcastUi.Place(speed.GetComponent<RectTransform>(), 0.52f, 0.08f, 0.66f, 0.92f);
            _speedLabel = speed.GetComponentInChildren<Text>();
            var hint = BroadcastUi.Label(_footer.transform, "ControlHint", "Enter  selecionar     Esc  pausa     Setas  navegar", 14, UiTheme.Muted, TextAnchor.MiddleRight);
            BroadcastUi.Place(hint.rectTransform, 0.66f, 0f, 0.98f, 1f);
        }

        void BuildPause(Transform root)
        {
            _pause = Overlay(root, "PausePanel", 0.12f, 0.14f, 0.88f, 0.86f, out var card);
            var dim = _pause.transform.Find("Dim");
            if (dim != null)
            {
                var shade = dim.GetComponent<Image>();
                var dimButton = BroadcastUi.Hit(shade, ResumeMatch);
                dimButton.transition = Selectable.Transition.None;
                shade.color = new Color(0.03f, 0.01f, 0.08f, 0.72f);
            }
            var title = BroadcastUi.Label(card, "Title", "JOGO EM PAUSA", 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.08f, 0.74f, 0.92f, 0.94f);
            _pauseBody = BroadcastUi.Label(card, "Tactics", "", 16, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.Place(_pauseBody.rectTransform, 0.1f, 0.44f, 0.9f, 0.72f);
            var resume = BroadcastUi.Button(card, "Resume", "RETOMAR", ResumeMatch, UiTheme.Accent, UiTheme.ButtonText, 18);
            BroadcastUi.Place(resume.GetComponent<RectTransform>(), 0.12f, 0.08f, 0.88f, 0.2f);
            var tactics = BroadcastUi.Button(card, "OpenTactics", "TÁTICAS", OpenTactics, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 16);
            BroadcastUi.Place(tactics.GetComponent<RectTransform>(), 0.12f, 0.24f, 0.48f, 0.38f);
            var sub = BroadcastUi.Button(card, "OpenSubs", "SUBSTITUIÇÕES", () => OpenSubs(true), new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 16);
            BroadcastUi.Place(sub.GetComponent<RectTransform>(), 0.52f, 0.24f, 0.88f, 0.38f);
            _pause.SetActive(false);
        }

        void BuildTactics(Transform root)
        {
            _tactics = Overlay(root, "TacticsPanel", 0.08f, 0.08f, 0.92f, 0.92f, out var card);
            var title = BroadcastUi.Label(card, "Title", "TÁTICAS RÁPIDAS", 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.08f, 0.86f, 0.84f, 0.97f);
            var formation = BroadcastUi.Button(card, "OpenFormation", "FORMAÇÃO", OpenShape, UiTheme.Accent, UiTheme.ButtonText, 16);
            BroadcastUi.Place(formation.GetComponent<RectTransform>(), 0.08f, 0.7f, 0.42f, 0.84f);
            var close = BroadcastUi.CloseX(card, "CloseTactics", CloseTactics);
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.88f, 0.88f, 0.97f, 0.98f);
            _pressOptions = new[] { "Pressão alta", "Pressão média", "Pressão baixa" };
            _lineOptions = new[] { "Linha defensiva alta", "Linha defensiva média", "Linha defensiva baixa" };
            var pressHint = BroadcastUi.Label(card, "PressHint", "Quanto cedo a equipa fecha o adversário com a posse.", 13, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(pressHint.rectTransform, 0.08f, 0.58f, 0.92f, 0.66f);
            _pressButtons = ChoiceRow(card, "Pressão", 0.44f, _pressOptions, value =>
            {
                _match.HomeTactics.Pressing = value;
                TouchSummary();
            }, true);
            var lineHint = BroadcastUi.Label(card, "LineHint", "Altura da linha defensiva e do bloco quando não há bola.", 13, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(lineHint.rectTransform, 0.08f, 0.34f, 0.92f, 0.42f);
            _lineButtons = ChoiceRow(card, "Linha", 0.2f, _lineOptions, value =>
            {
                _match.HomeTactics.DefensiveLine = value;
                TouchSummary();
            }, true);
            _tactics.SetActive(false);
        }

        void OpenTactics()
        {
            if (_match.Phase == MatchPhase.HalfTime || _match.Phase == MatchPhase.FullTime)
                return;
            _paused = true;
            _pause.SetActive(false);
            _tactics.SetActive(true);
            SetFooter(false);
            PaintChoices();
            RefreshPlan();
        }

        void CloseTactics()
        {
            if (_tactics != null)
                _tactics.SetActive(false);
            ResumeMatch();
        }

        void OpenPause()
        {
            if (_match.Phase == MatchPhase.HalfTime || _match.Phase == MatchPhase.FullTime)
                return;
            _paused = true;
            if (_tactics != null)
                _tactics.SetActive(false);
            if (_subsMenu != null)
                _subsMenu.SetActive(false);
            _pause.SetActive(true);
            _pause.transform.SetAsLastSibling();
            SetFooter(false);
            RefreshPlan();
        }

        void ResumeMatch()
        {
            _paused = false;
            if (_pause != null)
                _pause.SetActive(false);
            if (_tactics != null)
                _tactics.SetActive(false);
            if (_subsMenu != null)
                _subsMenu.SetActive(false);
            SetFooter(true);
        }

        void SetFooter(bool on)
        {
            if (_footer == null)
                return;
            foreach (var button in _footer.GetComponentsInChildren<Button>(true))
                button.interactable = on;
        }

        void TogglePause()
        {
            if (_paused)
                ResumeMatch();
            else
                OpenPause();
        }

        Button[] ChoiceRow(Transform card, string caption, float y, string[] options, Action<string> pick, bool fullLabel = false)
        {
            var buttons = new Button[options.Length];
            var label = BroadcastUi.Label(card, caption, caption.ToUpperInvariant(), 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(label.rectTransform, 0.08f, y + 0.12f, 0.4f, 0.2f);
            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var x0 = 0.08f + i * 0.3f;
                var text = fullLabel ? TacticLabel(option) : ShortChoice(option);
                buttons[i] = BroadcastUi.Button(card, caption + i, text, () => pick(option), new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, fullLabel ? 12 : 15);
                BroadcastUi.Place(buttons[i].GetComponent<RectTransform>(), x0, y, x0 + 0.27f, y + 0.1f);
            }

            return buttons;
        }

        static string TacticLabel(string option)
        {
            if (option.Contains("Pressão"))
                return option.Replace("Pressão ", "Pressão\n");
            if (option.Contains("Linha"))
                return option.Replace("Linha defensiva ", "Linha\n");
            return option;
        }

        static string ShortChoice(string option)
        {
            if (option.Contains("alta")) return "ALTA";
            if (option.Contains("baixa")) return "BAIXA";
            return "MÉDIA";
        }

        void BuildShape(Transform root)
        {
            _shape = Overlay(root, "Shape", 0.08f, 0.06f, 0.92f, 0.94f, out var card);
            var title = BroadcastUi.Label(card, "Title", "FORMAÇÃO", 24, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.04f, 0.9f, 0.5f, 0.98f);
            var close = BroadcastUi.Button(card, "CloseShape", "VOLTAR", () =>
            {
                _shape.SetActive(false);
                if (_tactics != null)
                    _tactics.SetActive(true);
            }, new Color(1f, 1f, 1f, 0.12f), Color.white, 16);
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.78f, 0.9f, 0.96f, 0.98f);
            var grass = BroadcastUi.Zone(card, "Grass", 0.18f, 0.22f, 0.82f, 0.88f);
            BroadcastUi.DrawPitch(grass, new Color(0.13f, 0.48f, 0.22f));
            _shapeHost = BroadcastUi.Zone(card, "Players", 0.18f, 0.22f, 0.82f, 0.88f);
            var forms = new[] { "4-3-3", "4-2-3-1", "4-4-2", "3-4-3", "5-4-1" };
            _formationButtons = new Button[forms.Length];
            for (var i = 0; i < forms.Length; i++)
            {
                var formation = forms[i];
                var x0 = 0.04f + i * 0.19f;
                _formationButtons[i] = BroadcastUi.Button(card, "Form" + i, formation, () => PickFormation(formation), new Color(1f, 1f, 1f, 0.1f), Color.white, 16);
                BroadcastUi.Place(_formationButtons[i].GetComponent<RectTransform>(), x0, 0.05f, x0 + 0.17f, 0.16f);
            }

            _shape.SetActive(false);
        }

        void OpenShape()
        {
            _pause.SetActive(false);
            _shape.SetActive(true);
            DrawShape();
        }

        void PickFormation(string formation)
        {
            _match.HomeTactics.Formation = formation;
            Reseat(_match.HomeOnField, _match.HomeBench, formation);
            TouchSummary();
            DrawShape();
            _slow = 0f;
        }

        void Reseat(List<string> onField, List<string> bench, string formation)
        {
            var players = new List<Player>();
            foreach (var id in onField)
            {
                var player = PlayerOf(id);
                if (player != null)
                    players.Add(player);
            }

            foreach (var id in bench)
            {
                var player = PlayerOf(id);
                if (player != null)
                    players.Add(player);
            }

            var eleven = new List<Player>();
            var rest = new List<Player>();
            StartingShape.Split(players, formation, eleven, rest);
            onField.Clear();
            bench.Clear();
            foreach (var player in eleven)
                onField.Add(player.Id);
            foreach (var player in rest)
                bench.Add(player.Id);
        }

        void DrawShape()
        {
            for (var i = _shapeHost.childCount - 1; i >= 0; i--)
                Destroy(_shapeHost.GetChild(i).gameObject);
            for (var i = 0; i < _match.HomeOnField.Count && i < 11; i++)
            {
                var slot = MatchEngine.Slot(true, i, _match.HomeTactics.Formation);
                var player = PlayerOf(_match.HomeOnField[i]);
                var anchor = new Vector2(slot.y, slot.x);
                var faceGo = new GameObject("Face" + i, typeof(RectTransform), typeof(Image));
                var faceRect = faceGo.GetComponent<RectTransform>();
                faceRect.SetParent(_shapeHost, false);
                faceRect.anchorMin = faceRect.anchorMax = anchor;
                faceRect.sizeDelta = new Vector2(36, 36);
                var face = faceGo.GetComponent<Image>();
                face.sprite = player == null ? PortraitStudio.For("x" + i) : FaceBook.For(player) ?? PortraitStudio.For(player.Id);
                face.preserveAspect = true;
                face.raycastTarget = false;
                var label = player == null ? (i + 1).ToString() : player.SquadNumber + "  " + BroadcastUi.ShortName(player.FullName);
                var text = BroadcastUi.Label(_shapeHost, "S" + i, label, 12, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                text.rectTransform.anchorMin = text.rectTransform.anchorMax = anchor + new Vector2(0f, -0.07f);
                text.rectTransform.sizeDelta = new Vector2(110, 22);
            }

            var forms = new[] { "4-3-3", "4-2-3-1", "4-4-2", "3-4-3", "5-4-1" };
            for (var i = 0; i < _formationButtons.Length; i++)
            {
                var on = _match.HomeTactics.Formation == forms[i];
                _formationButtons[i].GetComponent<Image>().color = on ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.1f);
                _formationButtons[i].GetComponentInChildren<Text>().color = on ? UiTheme.ButtonText : Color.white;
            }
        }

        void TouchSummary()
        {
            var t = _match.HomeTactics;
            t.Summary = t.Formation + " · " + t.Pressing + " · " + t.BuildUp;
            PaintChoices();
            RefreshPlan();
        }

        void PaintChoices()
        {
            PaintOption(_pressButtons, _pressOptions, _match.HomeTactics.Pressing);
            PaintOption(_lineButtons, _lineOptions, _match.HomeTactics.DefensiveLine);
        }

        static void PaintOption(Button[] buttons, string[] options, string current)
        {
            if (buttons == null)
                return;
            for (var i = 0; i < buttons.Length; i++)
            {
                var on = options != null && i < options.Length && options[i] == current;
                buttons[i].GetComponent<Image>().color = on ? UiTheme.Accent : new Color(0.22f, 0.12f, 0.38f, 1f);
                buttons[i].GetComponentInChildren<Text>().color = on ? UiTheme.ButtonText : Color.white;
            }
        }

        void BuildHalf(Transform root)
        {
            _halfTime = Modal(root, "HalfTime", out var card);
            var title = BroadcastUi.Label(card, "Title", "INTERVALO", 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.08f, 0.78f, 0.92f, 0.94f);
            var body = BroadcastUi.Label(card, "Body", "O balneário espera a tua palavra antes da segunda parte.", 18, UiTheme.Muted, TextAnchor.MiddleCenter);
            BroadcastUi.Place(body.rectTransform, 0.1f, 0.68f, 0.9f, 0.8f);
            TalkButton(card, "TalkCalm", "MANTER A CALMA", 0.54f, () =>
            {
                _match.DefenseBoost += 0.06f;
                Talk("Mantém a calma. Fechamos os espaços e saímos com critério.");
            });
            TalkButton(card, "TalkDemand", "EXIGIR INTENSIDADE", 0.36f, () =>
            {
                _match.AttackBoost += 0.04f;
                _match.DefenseBoost += 0.03f;
                Talk("Exijo mais intensidade. Quero cada segunda bola.");
            });
            TalkButton(card, "TalkAttack", "SUBIR AS LINHAS", 0.18f, () =>
            {
                _match.HomeTactics.Pressing = "Pressão alta";
                _match.HomeTactics.BuildUp = "Construção curta";
                _match.HomeTactics.DefensiveLine = "Linha defensiva alta";
                _match.AttackBoost += 0.08f;
                Talk("Vamos subir as linhas e atacar mais.");
            });
            _halfTime.SetActive(false);
        }

        void TalkButton(Transform card, string name, string label, float y, UnityEngine.Events.UnityAction action)
        {
            var button = BroadcastUi.Button(card, name, label, null, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 18);
            button.onClick.AddListener(action);
            BroadcastUi.Place(button.GetComponent<RectTransform>(), 0.1f, y, 0.9f, y + 0.14f);
        }

        void BuildFull(Transform root)
        {
            _fullTime = Modal(root, "FullTime", out var card);
            var title = BroadcastUi.Label(card, "Title", "FIM DE JOGO", 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.08f, 0.72f, 0.92f, 0.9f);
            _fullResult = BroadcastUi.Label(card, "Result", "", 32, UiTheme.Score, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(_fullResult.rectTransform, 0.08f, 0.4f, 0.92f, 0.7f);
            var next = BroadcastUi.Button(card, "Continue", "CONTINUAR", FinishMatch, UiTheme.Accent, UiTheme.ButtonText, 20);
            BroadcastUi.Place(next.GetComponent<RectTransform>(), 0.28f, 0.12f, 0.72f, 0.3f);
            _fullTime.SetActive(false);
        }

        GameObject Overlay(Transform root, string name, float x0, float y0, float x1, float y1, out Transform card)
        {
            var modal = BroadcastUi.Zone(root, name, 0f, 0f, 1f, 1f);
            var dim = BroadcastUi.Solid(modal, "Dim", new Color(0.03f, 0.01f, 0.08f, 0.72f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            var panel = BroadcastUi.Panel(modal, "Card", new Color(0.16f, 0.06f, 0.32f, 0.96f), true);
            BroadcastUi.Place(panel.rectTransform, x0, y0, x1, y1);
            card = panel.transform;
            return modal.gameObject;
        }

        GameObject Modal(Transform root, string name, out Transform card)
        {
            var modal = BroadcastUi.Zone(root, name, 0f, 0f, 1f, 1f);
            var dim = BroadcastUi.Solid(modal, "Dim", new Color(0.03f, 0.01f, 0.08f, 0.72f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            var panel = BroadcastUi.Panel(modal, "Card", new Color(0.16f, 0.06f, 0.32f, 0.96f), true);
            BroadcastUi.Place(panel.rectTransform, 0.27f, 0.24f, 0.73f, 0.76f);
            card = panel.transform;
            return modal.gameObject;
        }

        void Show(BoardTab tab)
        {
            _tab = tab;
            _fitnessPage.SetActive(true);
            _ratingsPage.SetActive(false);
            _statsPage.SetActive(tab == BoardTab.Stats);
            if (_homeCol != null && _homeCol.Root != null)
                _homeCol.Root.SetActive(tab != BoardTab.Stats);
            if (_awayCol != null && _awayCol.Root != null)
                _awayCol.Root.SetActive(tab != BoardTab.Stats);
            for (var i = 0; i < _tabs.Length; i++)
            {
                var label = _tabs[i].GetComponentInChildren<Text>();
                var on = (int)tab == i;
                label.color = on ? Color.white : UiTheme.Muted;
                label.fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
            }

            _slow = 0f;
        }

        void CycleSpeed()
        {
            if (_speed < 2f)
                _speed = 2.4f;
            else if (_speed < 4f)
                _speed = 4.2f;
            else
                _speed = 1.35f;
            var label = _speed < 2f ? "x1" : _speed < 4f ? "x2" : "x4";
            _speedLabel.text = "Ritmo " + label;
        }

        void OpenSubs(bool fromPause)
        {
            if (_match.HomeBench.Count == 0 || _match.HomeSubsLeft <= 0)
                return;
            _subFromPause = fromPause;
            _paused = true;
            _pause.SetActive(false);
            _tactics.SetActive(false);
            SetFooter(false);
            _pickOff = null;
            _pickOn = null;
            DrawSubLists();
            _subsMenu.SetActive(true);
        }

        void BuildSubs(Transform root)
        {
            _subsMenu = Overlay(root, "SubsMenu", 0.12f, 0.1f, 0.88f, 0.9f, out var card);
            var title = BroadcastUi.Label(card, "Title", "SUBSTITUIÇÃO", 24, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.04f, 0.9f, 0.6f, 0.98f);
            var close = BroadcastUi.CloseX(card, "CloseSubs", CloseSubs);
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.88f, 0.88f, 0.97f, 0.98f);
            var confirm = BroadcastUi.Button(card, "ConfirmSub", "CONFIRMAR", QueueSub, UiTheme.Accent, UiTheme.ButtonText, 16);
            BroadcastUi.Place(confirm.GetComponent<RectTransform>(), 0.32f, 0.03f, 0.68f, 0.12f);
            _subHost = BroadcastUi.Zone(card, "Lists", 0.03f, 0.14f, 0.97f, 0.88f);
            _subsMenu.SetActive(false);
        }

        void DrawSubLists()
        {
            for (var i = _subHost.childCount - 1; i >= 0; i--)
                Destroy(_subHost.GetChild(i).gameObject);
            var outTitle = BroadcastUi.Label(_subHost, "Out", "SAI", 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(outTitle.rectTransform, 0.02f, 0.92f, 0.46f, 1f);
            var inTitle = BroadcastUi.Label(_subHost, "In", "ENTRA", 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(inTitle.rectTransform, 0.52f, 0.92f, 0.96f, 1f);
            for (var i = 0; i < _match.HomeOnField.Count && i < 11; i++)
            {
                var id = _match.HomeOnField[i];
                var y = 0.84f - i * 0.075f;
                var button = SubButton(_subHost, id, true, 0.02f, y);
                if (id == _pickOff)
                    button.GetComponent<Image>().color = UiTheme.Accent;
            }

            for (var i = 0; i < _match.HomeBench.Count && i < 11; i++)
            {
                var id = _match.HomeBench[i];
                var y = 0.84f - i * 0.075f;
                var button = SubButton(_subHost, id, false, 0.52f, y);
                if (id == _pickOn)
                    button.GetComponent<Image>().color = UiTheme.Accent;
            }
        }

        Button SubButton(Transform parent, string id, bool off, float x, float y)
        {
            var player = PlayerOf(id);
            var label = player == null ? "Jogador" : player.SquadNumber + "  " + BroadcastUi.ShortName(player.FullName) + "  " + PresentationLabels.Position(player.Position);
            var button = BroadcastUi.Button(parent, (off ? "Off" : "On") + id, label, () =>
            {
                if (off) _pickOff = id;
                else _pickOn = id;
                DrawSubLists();
            }, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 14);
            BroadcastUi.Place(button.GetComponent<RectTransform>(), x, y - 0.06f, x + 0.44f, y);
            return button;
        }

        void QueueSub()
        {
            if (string.IsNullOrEmpty(_pickOff) || string.IsNullOrEmpty(_pickOn))
                return;
            _queueOff = _pickOff;
            _queueOn = _pickOn;
            _subsMenu.SetActive(false);
            _match.LastEvent = "Substituição pronta. Entra quando o jogo parar.";
            if (_engine.DeadBall)
                TryQueuedSub();
            if (!_subFromPause)
                ResumeMatch();
            else
            {
                _paused = true;
                _pause.SetActive(true);
                _pause.transform.SetAsLastSibling();
                SetFooter(false);
            }
        }

        void CloseSubs()
        {
            _subsMenu.SetActive(false);
            if (_subFromPause)
            {
                _pause.SetActive(true);
                _pause.transform.SetAsLastSibling();
            }
            else
                ResumeMatch();
        }

        void TryQueuedSub()
        {
            if (string.IsNullOrEmpty(_queueOff) || string.IsNullOrEmpty(_queueOn))
                return;
            if (!_engine.DeadBall && _match.Phase != MatchPhase.HalfTime)
                return;
            var off = _match.HomeOnField.IndexOf(_queueOff);
            var on = _match.HomeBench.IndexOf(_queueOn);
            _queueOff = null;
            _queueOn = null;
            if (off < 0 || on < 0)
                return;
            _engine.TrySub(true, off, on);
            _slow = 0f;
            RefreshPlan();
        }

        void Talk(string line)
        {
            _engine.ApplyTeamTalk(line);
            _halfTime.SetActive(false);
            _engine.StartSecondHalf();
            _paused = false;
            _footer.SetActive(true);
        }

        void FinishMatch()
        {
            var career = GameRoot.Instance.Session.Career;
            RecoverBench(_match.HomeBench);
            RecoverBench(_match.AwayBench);
            _match.Fixture.HomeGoals = _match.HomeGoals;
            _match.Fixture.AwayGoals = _match.AwayGoals;
            SeasonSchedule.ApplyResult(career, _match.Fixture);
            SeasonSchedule.SimulateOthers(career, _match.Fixture.Date, _match.Fixture.Id);
            var userHome = _match.Home.Id == career.Coach.ClubId;
            string motm = null;
            var best = -1f;
            foreach (var pair in _match.Ratings)
            {
                if (pair.Value <= best || !_match.People.ContainsKey(pair.Key))
                    continue;
                best = pair.Value;
                motm = _match.People[pair.Key].FullName;
            }
            foreach (var note in _match.Notes)
            {
                if (note.Kind == "red" || note.Kind == "goal" || note.Kind == "injury")
                    Newsroom.Publish(career, "A Gazeta do Relvado", note.Minute + "'  " + note.Text, "Aconteceu aos " + note.Minute + " minutos.\n\n" + note.Text);
            }
            CareerMoments.AfterMatch(career, _match.HomeGoals, _match.AwayGoals, userHome, _match.Home.ShortName, _match.Away.ShortName, motm, best < 0 ? null : best.ToString("0.0"));
            CareerStore.Save(career);
            GameRoot.Instance.LiveMatch = null;
            GameRoot.Instance.Open(SceneNames.CareerHub);
        }

        void RefreshHud()
        {
            _score.text = _match.HomeGoals + "  -  " + _match.AwayGoals;
            _clock.text = Clock(_match.Minute);
            _ticker.text = _match.LastEvent;
            if (_heard != _match.LastEvent)
            {
                _heard = _match.LastEvent;
                if (_heard != null && (_heard.Contains("Lançamento") || _heard.Contains("Lancamento")))
                    GameAudio.ThrowIn();
            }
            if (_match.FlashSerial != _flashSeen)
            {
                _flashSeen = _match.FlashSerial;
                _flash.text = _match.Flash;
                _flash.gameObject.SetActive(!string.IsNullOrEmpty(_match.Flash));
                _flash.transform.SetAsLastSibling();
                _flashLeft = 2.4f;
                GameAudio.ForFlash(_match.Flash);
                var person = PlayerOf(_match.FlashPlayerId);
                var text = _match.Flash ?? "";
                var important = text.Contains("CART") || text.Contains("LES") || text.Contains("GOLO") || text.Contains("FALTA") || text.Contains("PEN") || text.Contains("CANTO");
                _flash.transform.SetParent(_eventCard.transform, false);
                BroadcastUi.Place(_flash.rectTransform, 0.08f, 0.72f, 0.58f, 0.94f);
                _eventCard.SetActive(important);
                if (important)
                {
                    _eventCard.transform.SetAsLastSibling();
                    _eventFace.gameObject.SetActive(person != null);
                    if (person != null)
                    {
                        _eventFace.sprite = FaceBook.For(person) ?? PortraitStudio.For(person.Id);
                        _eventWho.text = person.SquadNumber + "   " + person.FullName;
                    }
                    else
                        _eventWho.text = "";
                    var card = text.Contains("VERMELHO") || text.Contains("AMARELO");
                    _eventStrip.gameObject.SetActive(card);
                    _eventStrip.color = text.Contains("VERMELHO") ? new Color(0.82f, 0.1f, 0.12f) : new Color(0.95f, 0.82f, 0.12f);
                }
            }

            if (_flashLeft > 0f)
            {
                _flashLeft -= Time.deltaTime;
                    if (_flashLeft <= 0f)
                    {
                        _flash.gameObject.SetActive(false);
                        if (_eventCard != null)
                            _eventCard.SetActive(false);
                        if (_eventStrip != null)
                            _eventStrip.gameObject.SetActive(false);
                    }
            }
        }

        void PlaceTokens()
        {
            for (var i = 0; i < 11; i++)
            {
                var homeOn = i < _match.HomeOnField.Count;
                var awayOn = i < _match.AwayOnField.Count;
                _homeTokens[i].Rect.gameObject.SetActive(homeOn);
                _awayTokens[i].Rect.gameObject.SetActive(awayOn);
                if (homeOn)
                {
                    var slot = _engine.PlayerSpot(true, i);
                    if (_match.HomeHasBall && i == _match.BallHolder)
                        slot = Vector2.Lerp(slot, _engine.Ball, 0.45f);
                    Move(_homeTokens[i], ToPitch(slot), _match.HomeHasBall && i == _match.BallHolder);
                    _homeTokens[i].Number.text = Shirt(_match.HomeOnField, i);
                    _homeTokens[i].Rect.GetComponent<Image>().color = _homeColor;
                }

                if (awayOn)
                {
                    var slot = _engine.PlayerSpot(false, i);
                    if (!_match.HomeHasBall && i == _match.BallHolder)
                        slot = Vector2.Lerp(slot, _engine.Ball, 0.45f);
                    Move(_awayTokens[i], ToPitch(slot), !_match.HomeHasBall && i == _match.BallHolder);
                    _awayTokens[i].Number.text = Shirt(_match.AwayOnField, i);
                    _awayTokens[i].Rect.GetComponent<Image>().color = _awayColor;
                }
            }

            var ball = ToPitch(_engine.Ball);
            _ball.anchorMin = ball;
            _ball.anchorMax = ball;
            _ball.anchoredPosition = Vector2.zero;
            if (_ballImage != null)
                _ballImage.color = Color.white;
            _snap = false;
        }

        static Vector2 ToPitch(Vector2 slot)
        {
            return new Vector2(slot.y, slot.x);
        }

        void Move(Token token, Vector2 anchor, bool hasBall)
        {
            var current = token.Rect.anchorMin;
            var next = _snap ? anchor : Vector2.Lerp(current, anchor, 1f - Mathf.Exp(-Time.deltaTime * 2.1f));
            token.Rect.anchorMin = next;
            token.Rect.anchorMax = next;
            token.Rect.anchoredPosition = Vector2.zero;
            token.Rect.localScale = Vector3.one * (hasBall ? 1.28f : 1f);
        }

        void RefreshSides()
        {
            Fill(_homeCol, _homeXi ? _match.HomeOnField : _match.HomeBench);
            Fill(_awayCol, _awayXi ? _match.AwayOnField : _match.AwayBench);
            PaintTab(_homeCol.Xi, _homeXi);
            PaintTab(_homeCol.Bench, !_homeXi);
            PaintTab(_awayCol.Xi, _awayXi);
            PaintTab(_awayCol.Bench, !_awayXi);
        }

        void Fill(SideColumn col, List<string> ids)
        {
            for (var i = 0; i < 11; i++)
            {
                var on = ids != null && i < ids.Count;
                col.Rows[i].SetActive(on);
                if (!on)
                    continue;
                var player = PlayerOf(ids[i]);
                var number = player == null ? (i + 1).ToString() : player.SquadNumber.ToString();
                var name = player == null ? NameAt(ids, i) : BroadcastUi.ShortName(player.FullName);
                if (player != null && player.IsCaptain)
                    name += "  C";
                col.Numbers[i].text = number;
                col.Names[i].text = name;
                var booking = 0;
                if (player != null)
                    _match.Bookings.TryGetValue(player.Id, out booking);
                col.Cards[i].gameObject.SetActive(booking > 0);
                col.Cards[i].color = booking == 2 ? new Color(0.85f, 0.12f, 0.14f) : new Color(0.95f, 0.82f, 0.12f);
                var id = player == null ? null : player.Id;
                col.Balls[i].gameObject.SetActive(Stat(_match.GoalsScored, id) > 0);
                col.Boots[i].gameObject.SetActive(Stat(_match.Assists, id) > 0);
                col.Pens[i].text = Stat(_match.PenaltyGoals, id) > 0 ? "P" : "";
                var ratings = _tab == BoardTab.Ratings;
                col.Fills[i].transform.parent.gameObject.SetActive(!ratings);
                col.Scores[i].gameObject.SetActive(ratings);
                if (ratings)
                    col.Scores[i].text = _match.RatingOf(ids[i]).ToString("0.0");
                else
                {
                    var tired = player == null ? 30f : player.Fatigue;
                    BroadcastUi.SetMeter(col.Fills[i], 1f - tired / 100f);
                }
            }
        }

        void RefreshRatings()
        {
            if (_tab != BoardTab.Ratings)
                return;
            _homeRatings.text = RatingBlock(_match.HomeOnField, _match.HomeBench);
            _awayRatings.text = RatingBlock(_match.AwayOnField, _match.AwayBench);
        }

        string RatingBlock(List<string> field, List<string> bench)
        {
            var lines = new List<string>();
            AppendRatings(lines, field);
            lines.Add("");
            lines.Add("Banco");
            AppendRatings(lines, bench);
            return string.Join("\n", lines);
        }

        void AppendRatings(List<string> lines, List<string> ids)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                var player = PlayerOf(ids[i]);
                var number = player == null ? (i + 1).ToString() : player.SquadNumber.ToString();
                var name = player == null ? NameAt(ids, i) : BroadcastUi.ShortName(player.FullName);
                lines.Add(number.PadLeft(2) + "   " + name.PadRight(16) + "   " + _match.RatingOf(ids[i]).ToString("0.0"));
            }
        }

        void RefreshStats()
        {
            var total = _match.HomePossession + _match.AwayPossession;
            var home = total <= 0.01f ? 50 : Mathf.RoundToInt(_match.HomePossession / total * 100f);
            _homePoss.text = home.ToString();
            _awayPoss.text = (100 - home).ToString();
            _homeShots.text = _match.HomeShots.ToString();
            _awayShots.text = _match.AwayShots.ToString();
            _homeChances.text = _match.HomeChances.ToString();
            _awayChances.text = _match.AwayChances.ToString();
            if (_noteCount == _match.Notes.Count && _timeline.childCount > 0)
                return;
            _noteCount = _match.Notes.Count;
            for (var i = _timeline.childCount - 1; i >= 0; i--)
                Destroy(_timeline.GetChild(i).gameObject);
            var first = NotesBefore(45);
            if (first.Count == 0)
                TimelineLine("SEM EVENTOS", true);
            else
                foreach (var note in first)
                    TimelineLine(note.Minute + "'    " + note.Text, note.Kind == "goal");
            if (_match.Phase != MatchPhase.FirstHalf)
            {
                TimelineLine("45:00", false);
                var second = NotesFrom(45);
                if (second.Count == 0)
                    TimelineLine("SEM EVENTOS", true);
                else
                    foreach (var note in second)
                        TimelineLine(note.Minute + "'    " + note.Text, note.Kind == "goal");
            }
        }

        List<MatchNote> NotesBefore(int minute)
        {
            var list = new List<MatchNote>();
            foreach (var note in _match.Notes)
            {
                if (note.Kind == "half" || note.Kind == "full")
                    continue;
                if (note.Minute < minute)
                    list.Add(note);
            }

            return list;
        }

        List<MatchNote> NotesFrom(int minute)
        {
            var list = new List<MatchNote>();
            foreach (var note in _match.Notes)
            {
                if (note.Kind == "half" || note.Kind == "full")
                    continue;
                if (note.Minute >= minute)
                    list.Add(note);
            }

            return list;
        }

        void TimelineLine(string value, bool strong)
        {
            var text = BroadcastUi.Label(_timeline, "Line", value, strong ? 20 : 16, strong ? Color.white : UiTheme.Muted, TextAnchor.MiddleCenter, strong ? FontStyle.Bold : FontStyle.Normal);
            BroadcastUi.Preferred(text, strong ? 36 : 28);
        }

        void RefreshPlan()
        {
            var off = NameAt(_match.HomeOnField, _subOff);
            var on = _match.HomeBench.Count == 0 ? "—" : NameAt(_match.HomeBench, _subOn % _match.HomeBench.Count);
            var body =
                "Formação  " + _match.HomeTactics.Formation + "\n\n" +
                _match.HomeTactics.Pressing + "\n" +
                _match.HomeTactics.BuildUp + "\n" +
                _match.HomeTactics.DefensiveLine + "\n\n" +
                "Substituições restantes: " + _match.HomeSubsLeft + "\n" +
                "Próxima troca: sai " + off + ", entra " + on + "\n\n" +
                "Adversário  " + _match.Away.ShortName + "  ·  " + _match.AwayTactics.Formation;
            if (_pauseBody != null)
                _pauseBody.text = body;
        }

        static void PaintTab(Button button, bool on)
        {
            var label = button.GetComponentInChildren<Text>();
            label.color = on ? Color.white : UiTheme.Muted;
            label.fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
        }

        static int Stat(Dictionary<string, int> map, string id)
        {
            if (map == null || string.IsNullOrEmpty(id))
                return 0;
            return map.TryGetValue(id, out var value) ? value : 0;
        }

        static Sprite _boot;
        static Sprite BootSprite()
        {
            if (_boot != null)
                return _boot;
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var clear = new Color(0, 0, 0, 0);
            var ink = Color.white;
            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    var sole = y < 4 && x > 1 && x < 15;
                    var upper = y >= 4 && y < 11 && x > 3 && x < 12;
                    var ankle = y >= 8 && y < 14 && x > 5 && x < 11;
                    tex.SetPixel(x, y, sole || upper || ankle ? ink : clear);
                }
            }
            tex.Apply();
            _boot = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
            return _boot;
        }

        Player PlayerOf(string id)
        {
            if (_career == null || string.IsNullOrEmpty(id))
                return null;
            return _career.World.Players.TryGetValue(id, out var player) ? player : null;
        }

        void RecoverBench(List<string> ids)
        {
            if (ids == null)
                return;
            foreach (var id in ids)
            {
                if (!_match.People.TryGetValue(id, out var player))
                    continue;
                player.Fatigue = Mathf.Max(5, player.Fatigue - 14);
            }
        }

        string Shirt(List<string> ids, int index)
        {
            var player = PlayerOf(ids[index]);
            return player == null ? (index + 1).ToString() : player.SquadNumber.ToString();
        }

        string NameAt(List<string> ids, int index)
        {
            if (ids == null || ids.Count == 0)
                return "—";
            index = Mathf.Clamp(index, 0, ids.Count - 1);
            return _match.Names.TryGetValue(ids[index], out var name) ? BroadcastUi.ShortName(name) : "Jogador";
        }

        static string Clock(float minute)
        {
            if (minute >= 90f)
            {
                var extra = Mathf.FloorToInt(minute - 90f);
                return extra > 0 ? "90:00  +" + extra : "90:00";
            }

            var whole = Mathf.FloorToInt(minute);
            var seconds = Mathf.FloorToInt((minute - whole) * 60f);
            return whole + ":" + seconds.ToString("00");
        }

        sealed class Token
        {
            public RectTransform Rect;
            public Text Number;
        }

        sealed class SideColumn
        {
            public GameObject Root;
            public Text[] Numbers;
            public Text[] Names;
            public Text[] Scores;
            public Image[] Cards;
            public Image[] Balls;
            public Image[] Boots;
            public Text[] Pens;
            public Text[] Marks;
            public Image[] Fills;
            public GameObject[] Rows;
            public Button Xi;
            public Button Bench;
        }
    }
}
