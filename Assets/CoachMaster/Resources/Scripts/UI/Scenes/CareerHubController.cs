using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CoachMaster.App;
using CoachMaster.Career;
using CoachMaster.Data;
using CoachMaster.Domain;
using CoachMaster.MatchPlay;
using CoachMaster.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CoachMaster.UI.Scenes
{
    public sealed class CareerHubController : MonoBehaviour, IControlBackHandler
    {
        const int MaxClubStaff = 12;
        static readonly string[] SocialAccountNames =
        {
            "Jamie Cole", "Alex Turner", "Touchline Wire", "The Football Desk",
            "The Golden Ball Desk", "Transfer Ledger", "Tactical Review", "Saturday Scout",
            "The League Room", "Sam Carter", "Jordan Mills", "Football Numbers",
            "The Dugout Desk", "Taylor Shaw", "Fixture List", "The Press Box",
            "Boot Room Notes", "The 90th Minute", "Casey Reid", "The Fan Verdict",
            "Ballon d'Or Bulletin", "Transfer Room", "North Stand Media", "Matchday Briefing"
        };
        static readonly string[] SocialAccountHandles =
        {
            "@jamiecole", "@alextturner", "@touchlinewire", "@football_desk",
            "@goldenballdesk", "@transferledger", "@tacticalreview", "@saturdayscout",
            "@theleagueroom", "@samcarter", "@jordanmills", "@footballnumbers",
            "@dugoutdesk", "@taylorshaw", "@fixturelist", "@pressboxfootball",
            "@bootroomnotes", "@the90thminute", "@caseyreid", "@fanverdict",
            "@ballondorbulletin", "@transferroom", "@northstandmedia", "@matchdaybriefing"
        };
        GameObject[] _pages;
        Button[] _tabs;
        Button[] _officeTabs;
        GameObject[] _officeViews;
        RectTransform _academyContent;
        Button[] _academyAgeTabs;
        Button[] _academyViewTabs;
        int _academyAgeIndex;
        int _academyViewIndex;
        int _academyPane;
        int _academyBenchGroup;
        string _academyArm;
        string _academyArmGroup;
        int _academyLayoutPassesRemaining;
        string _academySelectedBenchPlayerId = "";
        string _academyLastBenchClickId = "";
        float _academyLastBenchClickAt;
        long _lastCareerSimulationDayTicks;
        RectTransform _trainingContent;
        RectTransform _trainingFocusContent;
        RectTransform _trainingDistributionContent;
        InputField _trainingSearch;
        Button _trainingTeamTab;
        Button _trainingIndividualTab;
        RectTransform _officeBudgetContent;
        int _officeTab;
        Button[] _transferTabs;
        Button[] _replies;
        Text _clock;
        Text _alertLabel;
        RectTransform _phoneSheet;
        RectTransform _phoneList;
        GameObject _phoneApps;
        GameObject _phonePage;
        Text _phoneAppTitle;
        float _phoneSlide;
        bool _phoneOpen;
        string _phoneApp = "Mensagens";
        bool _phoneInThread;
        PhoneNote _activePhoneNote;
        int _memoryShelf = 1;
        int _boardWeekOffset;
        int _boardWeekDay = -1;
        string _tacticSelection = "preset";
        int _tacticPhase;
        int _socialFeedDaySeed = int.MinValue;
        readonly HashSet<string> _phoneAnswered = new HashSet<string>();
        Text _dateLine;
        Text _playLabel;
        Button _play;
        bool _reserves;
        Button _subsTab;
        Button _reservesTab;
        Button _subsPrev;
        Button _subsNext;
        Text _teamTitle;
        Text _benchSwapHint;
        Text _roles;
        int _pane;
        Image _pitchBg;
        GameObject _listPage;
        GameObject _marketPage;
        RectTransform _listHost;
        GameObject _teamRosterPage;
        RectTransform _teamRosterHost;
        RectTransform _marketHost;
        InputField _marketSearch;
        GameObject _transferListPage;
        RectTransform _transferListHost;
        GameObject _transferMarketPage;
        RectTransform _transferMarketHost;
        InputField _transferSearch;
        Transform _transfersRoot;
        Button[] _paneTabs;
        Button[] _zoneButtons;
        RectTransform _sheet;
        Text _noteError;
        Text _vibe;
        InputField _noteField;
        RectTransform _newsContent;
        RectTransform _tableContent;
        RectTransform _tabBar;
        RectTransform _boardContent;
        RectTransform _careerRecordContent;
        RectTransform _inboxMessages;
        RectTransform _inboxNews;
        RectTransform _inboxSide;
        GameObject _staffSheet;
        RectTransform _pitch;
        RectTransform _subs;
        RectTransform _subsViewport;
        ScrollRect _subsScroll;
        Player _pendingBenchSwap;
        bool _pendingBenchSwapFromReserves;
        RectTransform _staffHost;
        RectTransform _scoutHost;
        RectTransform _calendar;
        GameObject _modal;
        Text _modalTitle;
        Text _modalLead;
        Text _staffNegotiationStatus;
        RectTransform _modalBody;
        Button _modalAction;
        Text _modalActionLabel;
        Button _modalClose;
        Action _dossierReturnAction;
        Action _sheetReturn;
        Club _dossierClub;
        int _dossierTab;
        string _dossierYouth;
        Action _dossierBack;
        Button _keyboardScoutBack;
        Player _profileModalPlayer;
        bool _profileModalOrders;
        GameObject _tacHost;
        string _arm;
        string _orderId;
        RectTransform _tacticsBody;
        int _modalLineSize = 16;
        CareerState _career;
        StaffMember _offer;
        int _transferPage;
        string _transferHistoryClubId = "";
        int _transferHistorySeasonStart = -1;
        bool _transferHistoryFeeDescending = true;
        bool _transferHistoryDateDescending = true;
        bool _transferHistorySortByFee;
        int _financeMetricSelection;
        int _teamZone;
        int _hubPage;
        DateTime _calendarMonth;
        bool _individualTraining;
        bool _focusModalFrame;
        bool _academyViewFocused;
        List<Player> _registrationOrder;
        GameObject _focusMenu;
        int _renewalAttempt;
        string _boardPlayerId;
        int _boardFee;
        int _boardWage;
        int _boardYears;
        int _boardGap;
        int _boardWageGap;
        StaffOfferCandidate _boardStaffCandidate;
        string _boardStaffPhoneKey;
        bool _enterPressRoom;
        readonly Dictionary<int, string> _joinNames = new Dictionary<int, string>();

        void Start()
        {
            LocalizationManager.ApplyScene();
            LocalizationManager.LanguageChanged += OnLanguageChanged;
            _career = GameRoot.Require().Session.Career;
            if (_career == null)
            {
                GameRoot.Instance.Open(SceneNames.MainMenu);
                return;
            }
            Newsroom.PurgeCareerMemoryStories(_career);

            if (LanRoom.PendingShare)
            {
                LanRoom.PendingShare = false;
                LanRoom.HoldShare = false;
                string ignored;
                while (LanRoom.TryTake(out ignored)) { }
                if (LanRoom.Connected && _career.Online != null && _career.Online.Active)
                    LanRoom.Share(CareerStore.ReadText());
            }
            else if (LanRoom.HoldShare)
                LanRoom.HoldShare = false;

            if (_career.Current == null && _career.Upcoming.Count == 0)
                CareerMoments.Begin(_career);

            var designed = transform.Find("Design") as RectTransform;
            if (designed != null)
            {
                BroadcastUi.Mount(this);
                BindDesign(designed);
                Show(0);
                Refresh();
                FillTeam();
                FillAcademy();
                FillStaff();
                FillCalendar();
                if (_phoneSheet != null)
                    _phoneSheet.gameObject.SetActive(false);
                return;
            }

            var root = BroadcastUi.Mount(this);
            BroadcastUi.Backdrop(root);
            LeagueBackdrop.Paint(root, HubLeagueId());
            BuildHeader(root);
            BuildPhone(root);
            BuildTabs(root);
            BuildPages(root);
            RefreshCareerTabLabels();
            BuildPlay(root);
            BuildModal(root);
            Show(0);
            Refresh();
            if (_phoneSheet != null)
                _phoneSheet.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
        }

        void OnLanguageChanged()
        {
            if (_career == null)
                return;
            RefreshCareerTabLabels();
            RefreshLocalizedPresentation();
            FillBoard();
            FillInbox();
            if (_pages != null)
            {
                for (var i = 0; i < _pages.Length; i++)
                {
                    if (!_pages[i].activeSelf)
                        continue;
                    if (i == 1) FillNews();
                    else if (i == 2) FillAcademy();
                    else if (i == 3) { if (_teamZone == 1) FillTeamRoster(); else FillTeam(); }
                    else if (i == 4) FillTraining();
                    else if (i == 5) ShowTransfersZone(_transferPage);
                    else if (i == 6) ShowOfficeTab(_officeTab);
                    else if (i == 7) FillStandings();
                    break;
                }
            }
            if (_profileModalPlayer != null && _modal != null && _modal.activeSelf)
                OpenPlayer(_profileModalPlayer, _profileModalOrders);
            if (!_phoneOpen || _phonePage == null || !_phonePage.activeSelf)
                return;
            if (_phoneInThread && _activePhoneNote != null)
                OpenThread(_activePhoneNote);
            else
                FillPhone(_phoneApp);
        }

        void Update()
        {
            if (PollOnline())
                return;

            if (_keyboardScoutBack != null && _keyboardScoutBack.gameObject.activeSelf != ControlChrome.UsingKeyboard)
                _keyboardScoutBack.gameObject.SetActive(ControlChrome.UsingKeyboard);

            if (_career != null && _career.Calendar != null)
            {
                var dayTicks = _career.Calendar.CurrentDate.Date.Ticks;
                if (_lastCareerSimulationDayTicks != dayTicks)
                {
                    _lastCareerSimulationDayTicks = dayTicks;
                    var academyPlayerCount = _career.AcademyPlayerIds.Count;
                    var academyFixtureCount = _career.AcademyFixtures.Count;
                    var changed = AcademySeason.Advance(_career);
                    changed |= academyPlayerCount != _career.AcademyPlayerIds.Count || academyFixtureCount != _career.AcademyFixtures.Count;
                    changed |= AIManagerMarket.Advance(_career);
                    changed |= GlobalTransferMarket.SettleMoves(_career);
                    changed |= GlobalTransferMarket.Advance(_career);
                    changed |= Newsroom.PublishGlobalRoundup(_career);
                    changed |= NoticeTransferWindow();
                    if (changed)
                    {
                        CareerStore.Save(_career);
                        if (_pages != null && _pages.Length > 2 && _pages[2] != null && _pages[2].activeSelf) FillAcademy();
                        if (_pages != null && _pages.Length > 1 && _pages[1] != null && _pages[1].activeSelf) FillNews();
                        if (_transferPage == 2 && _pages != null && _pages.Length > 5 && _pages[5] != null && _pages[5].activeSelf) FillTransferHistory();
                    }
                }
            }

            if (_phoneSheet != null && _phoneSheet.gameObject.activeSelf)
                _phoneSheet.gameObject.SetActive(false);

            ReadPadTabs();

            if (_phoneOpen && _phoneApp == "X" && !_phoneInThread && _phonePage != null && _phonePage.activeSelf && _career != null)
            {
                var daySeed = _career.Calendar.CurrentDate.DayOfYear + _career.Calendar.CurrentDate.Year * 13;
                if (_socialFeedDaySeed != daySeed)
                    FillPhone("X");
            }

            if (_career == null || _career.Current == null || _career.Current.Kind != MomentKind.Talk || _career.Current.Answered)
                return;
            if (ControlChrome.Digit(1)) Answer(0);
            else if (ControlChrome.Digit(2)) Answer(1);
            else if (ControlChrome.Digit(3)) Answer(2);
        }

        void LateUpdate()
        {
            if (_focusModalFrame && _modal != null && _modal.activeSelf)
            {
                _focusModalFrame = false;
                ControlChrome.FocusWithin(_modal.transform.Find("Card"));
            }
            if (_academyLayoutPassesRemaining > 0 && _academyContent != null
                && _pages != null && _pages.Length > 2 && _pages[2] != null && _pages[2].activeSelf)
            {
                _academyLayoutPassesRemaining--;
                Canvas.ForceUpdateCanvases();
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_academyContent);
                FillAcademy();
            }

            if (_career != null && SquadDragCard.ConsumeBenchGroupDrop(out var benchPlayer, out var fromReserves, out var toReserves, out var benchContext))
            {
                if (!string.IsNullOrEmpty(benchContext) && benchContext.StartsWith("Academy:", StringComparison.Ordinal))
                {
                    _academySelectedBenchPlayerId = benchPlayer == null ? "" : benchPlayer.Id;
                    _academyBenchGroup = toReserves ? 1 : 0;
                    FillAcademy();
                }
                else if (benchPlayer != null && fromReserves != toReserves)
                {
                    _pendingBenchSwap = benchPlayer;
                    _pendingBenchSwapFromReserves = fromReserves;
                    _reserves = toReserves;
                    FillTeam();
                }
            }
            if (_career != null && SquadDragCard.ConsumeSwap(out var incoming, out var fieldIndex, out var fromField, out var swapContext))
            {
                if (!string.IsNullOrEmpty(swapContext) && swapContext.StartsWith("Academy:", StringComparison.Ordinal))
                    SwapAcademyFieldOrder(swapContext.Substring("Academy:".Length), incoming, fromField, fieldIndex);
                else if (fromField >= 0)
                    SwapPositions(fromField, fieldIndex);
                else
                    SwapStarter(incoming, fieldIndex);
            }
        }

        void BuildHeader(Transform root)
        {
            var bar = BroadcastUi.Zone(root, "Header", 0.03f, 0.9f, 0.7f, 0.98f);
            var crest = CrestImage(bar, _career.Club, 52f);
            crest.rectTransform.anchorMin = crest.rectTransform.anchorMax = new Vector2(0.04f, 0.5f);
            var name = BroadcastUi.Label(bar, "ClubName", _career.Club.Name.ToUpperInvariant(), 28, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(name.rectTransform, 0.1f, 0.48f, 0.98f, 0.95f);
            _dateLine = BroadcastUi.Label(bar, "Date", "", 18, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(_dateLine.rectTransform, 0.1f, 0.05f, 0.98f, 0.48f);

            var clockCard = BroadcastUi.Panel(root, "ClockCard", new Color(0.05f, 0.02f, 0.12f, 0.72f));
            BroadcastUi.Place(clockCard.rectTransform, 0.78f, 0.9f, 0.97f, 0.98f);
            var caption = BroadcastUi.Label(clockCard.transform, "Caption", "HORA", 13, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(caption.rectTransform, 0.08f, 0.68f, 0.92f, 0.94f);
            _clock = BroadcastUi.Label(clockCard.transform, "Clock", "18:00", 32, UiTheme.Score, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(_clock.rectTransform, 0.04f, 0.08f, 0.96f, 0.72f);
            EnsureAlert(root);
        }

        void BuildPhone(Transform root)
        {
            var shell = BroadcastUi.Panel(root, "Phone", new Color(0.07f, 0.07f, 0.08f, 1f));
            _phoneSheet = shell.rectTransform;
            BroadcastUi.Place(_phoneSheet, 0.4f, 0.935f, 0.6f, 1.655f);
            var screen = BroadcastUi.Panel(_phoneSheet, "Screen", new Color(0.03f, 0.05f, 0.12f, 1f));
            BroadcastUi.Place(screen.rectTransform, 0.06f, 0.1f, 0.94f, 0.96f);
            var notch = BroadcastUi.Solid(screen.transform, "Notch", new Color(0.07f, 0.07f, 0.08f, 1f));
            notch.rectTransform.anchorMin = notch.rectTransform.anchorMax = new Vector2(0.5f, 0.96f);
            notch.rectTransform.sizeDelta = new Vector2(54, 10);
            _phoneApps = new GameObject("Apps", typeof(RectTransform));
            _phoneApps.transform.SetParent(screen.transform, false);
            BroadcastUi.Stretch(_phoneApps.GetComponent<RectTransform>());
            var apps = new[]
            {
                "Mensagens", "X"
            };
            for (var i = 0; i < apps.Length; i++)
            {
                var name = apps[i];
                var col = i % 2;
                var x0 = 0.08f + col * 0.46f;
                var color = AppColor(name);
                var icon = BroadcastUi.Button(_phoneApps.transform, "App" + i, name, () => OpenPhoneApp(name), color, Color.white, 13);
                BroadcastUi.Place(icon.GetComponent<RectTransform>(), x0, 0.68f, x0 + 0.38f, 0.9f);
            }

            _phonePage = new GameObject("Page", typeof(RectTransform));
            _phonePage.transform.SetParent(screen.transform, false);
            BroadcastUi.Stretch(_phonePage.GetComponent<RectTransform>());
            _phoneAppTitle = BroadcastUi.Label(_phonePage.transform, "AppTitle", "Mensagens", 16, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(_phoneAppTitle.rectTransform, 0.06f, 0.88f, 0.94f, 0.98f);
            var scroll = BroadcastUi.Scroll(_phonePage.transform, "Msgs", out _phoneList);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.04f, 0.03f, 0.96f, 0.86f);
            _phonePage.SetActive(false);
            var chin = BroadcastUi.Button(_phoneSheet, "Chin", "", PressChin, new Color(0.12f, 0.12f, 0.13f, 1f), Color.white, 12);
            BroadcastUi.Place(chin.GetComponent<RectTransform>(), 0f, 0f, 1f, 0.09f);
            var pill = BroadcastUi.Solid(chin.transform, "Pill", new Color(0.92f, 0.92f, 0.94f, 1f));
            pill.rectTransform.anchorMin = pill.rectTransform.anchorMax = new Vector2(0.5f, 0.55f);
            pill.rectTransform.sizeDelta = new Vector2(46, 5);
            pill.raycastTarget = false;
        }

        static Color AppColor(string name)
        {
            if (name == "X") return new Color(0.08f, 0.1f, 0.13f, 1f);
            if (name == "Plantel") return new Color(0.16f, 0.55f, 0.32f, 1f);
            if (name == "Mercado") return new Color(0.72f, 0.5f, 0.12f, 1f);
            if (name == "Staff") return new Color(0.38f, 0.24f, 0.62f, 1f);
            if (name == "Calendário") return new Color(0.7f, 0.28f, 0.22f, 1f);
            if (name == "Jornais") return new Color(0.2f, 0.38f, 0.7f, 1f);
            return new Color(0.12f, 0.62f, 0.7f, 1f);
        }

        void PressChin()
        {
            if (!_phoneOpen)
            {
                _phoneOpen = true;
                ShowPhoneHome();
                _phoneSheet.SetAsLastSibling();
                return;
            }

            if (_phoneInThread)
            {
                _phoneInThread = false;
                _activePhoneNote = null;
                if (_phoneAppTitle != null)
                    _phoneAppTitle.text = _phoneApp == "X" ? "X   PARA TI" : _phoneApp;
                FillPhone(_phoneApp);
                return;
            }

            if (_phonePage != null && _phonePage.activeSelf)
            {
                ShowPhoneHome();
                return;
            }

            _phoneOpen = false;
        }

        void ShowPhoneHome()
        {
            if (_phoneApps != null)
                _phoneApps.SetActive(true);
            if (_phonePage != null)
                _phonePage.SetActive(false);
        }

        void OpenPhoneApp(string name)
        {
            if (_phoneApps != null)
                _phoneApps.SetActive(false);
            if (_phonePage != null)
                _phonePage.SetActive(true);
            _phoneApp = name;
            _phoneInThread = false;
            if (_phoneAppTitle != null)
                _phoneAppTitle.text = name == "X" ? "X   PARA TI" : name;
            FillPhone(name);
        }

        void FillPhone(string app)
        {
            if (_phoneList == null)
                return;
            if (app == "X" && _career != null && _career.Calendar != null)
                _socialFeedDaySeed = _career.Calendar.CurrentDate.DayOfYear + _career.Calendar.CurrentDate.Year * 13;
            Clear(_phoneList);
            var notes = app == "X" ? SocialNotes() : PhoneNotes();
            var shown = 0;
            for (var i = 0; i < notes.Count; i++)
            {
                var note = notes[i];
                if (!PhoneMatches(app, note.Channel))
                    continue;
                shown++;
                var captured = note;
                var social = note.Channel == "social";
                var row = BroadcastUi.Panel(_phoneList, "Msg" + i, social ? new Color(0.06f, 0.07f, 0.09f, 1f) : new Color(1f, 1f, 1f, 0.06f));
                var socialHeight = social ? Mathf.Clamp(154f + note.Text.Length * 0.3f, 168f, 224f) : 92f;
                BroadcastUi.Preferred(row, socialHeight);
                BroadcastUi.Hit(row, () => OpenThread(captured));
                var left = social ? 0.17f : 0.05f;
                if (social)
                {
                    AddSocialAuthor(row.transform, note, false);
                }
                if (!social)
                {
                    var tag = note.ReplyGroup == "job-offer" ? (LocalizationManager.Current == GameLanguage.English ? "OFFER" : "PROPOSTA")
                        : note.Channel == "jogador" ? (LocalizationManager.Current == GameLanguage.English ? "PLAYER" : "JOGADOR")
                        : note.Channel == "clube" ? (LocalizationManager.Current == GameLanguage.English ? "CLUB" : "CLUBE")
                        : note.Channel == "staff" ? "STAFF"
                        : note.Channel == "press" ? (LocalizationManager.Current == GameLanguage.English ? "PRESS" : "IMPRENSA")
                        : note.Channel == "jornal" ? (LocalizationManager.Current == GameLanguage.English ? "NEWS" : "JORNAL")
                        : (LocalizationManager.Current == GameLanguage.English ? "SCHEDULE" : "AGENDA");
                    var who = BroadcastUi.Label(row.transform, "Who", note.Who, 15, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                    who.raycastTarget = false;
                    BroadcastUi.Place(who.rectTransform, left, 0.62f, 0.72f, 0.95f);
                    var kind = BroadcastUi.Label(row.transform, "Tag", tag, 11, UiTheme.Muted, TextAnchor.MiddleRight, FontStyle.Bold);
                    kind.raycastTarget = false;
                    BroadcastUi.Place(kind.rectTransform, 0.72f, 0.62f, 0.96f, 0.95f);
                }
                var body = BroadcastUi.Label(row.transform, "Body", note.Text, social ? 13 : 14, Color.white, TextAnchor.UpperLeft);
                body.raycastTarget = false;
                body.horizontalOverflow = HorizontalWrapMode.Wrap;
                body.verticalOverflow = VerticalWrapMode.Truncate;
                if (social)
                {
                    body.resizeTextForBestFit = true;
                    body.resizeTextMinSize = 10;
                    body.resizeTextMaxSize = 13;
                }
                BroadcastUi.Place(body.rectTransform, left, social ? 0.16f : 0.06f, 0.96f, social ? 0.6f : 0.6f);
                if (social && !string.IsNullOrEmpty(note.Meta))
                {
                    var meta = BroadcastUi.Label(row.transform, "Reactions", note.Meta, 11, UiTheme.Muted, TextAnchor.MiddleLeft);
                    BroadcastUi.Place(meta.rectTransform, left, 0.015f, 0.95f, 0.155f);
                }
            }

            if (shown == 0)
            {
                var empty = BroadcastUi.Label(_phoneList, "Empty", "Sem mensagens nesta app.", 14, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Preferred(empty, 48);
            }
        }

        static bool PhoneMatches(string app, string channel)
        {
            if (app == "Mensagens")
                return channel != "social";
            if (app == "X")
                return channel == "social";
            return false;
        }

        void OpenThread(PhoneNote note)
        {
            if (_phoneList == null)
                return;
            _phoneInThread = true;
            _activePhoneNote = note;
            if (_phoneAppTitle != null)
                _phoneAppTitle.text = note.Who;
            Clear(_phoneList);
            var card = BroadcastUi.Panel(_phoneList, "Thread", new Color(1f, 1f, 1f, 0.06f));
            var social = note.Channel == "social";
            BroadcastUi.Preferred(card, social ? 420 : 150);
            if (social)
                AddSocialAuthor(card.transform, note, true);
            var body = BroadcastUi.Label(card.transform, "Body", note.Text, social ? 14 : 15, Color.white, TextAnchor.UpperLeft);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;
            if (social)
            {
                body.resizeTextForBestFit = true;
                body.resizeTextMinSize = 11;
                body.resizeTextMaxSize = 14;
            }
            BroadcastUi.Place(body.rectTransform, 0.06f, social ? 0.48f : 0.08f, 0.94f, social ? 0.69f : 0.92f);
            if (social && !string.IsNullOrEmpty(note.Meta))
            {
                var meta = BroadcastUi.Label(card.transform, "Reactions", note.Meta, 12, UiTheme.Muted, TextAnchor.MiddleLeft);
                meta.resizeTextForBestFit = true;
                meta.resizeTextMinSize = 9;
                meta.resizeTextMaxSize = 12;
                meta.horizontalOverflow = HorizontalWrapMode.Wrap;
                meta.verticalOverflow = VerticalWrapMode.Truncate;
                BroadcastUi.Place(meta.rectTransform, 0.06f, 0.42f, 0.94f, 0.48f);
            }
            var key = note.Key;
            if (note.Channel == "social")
            {
                AddSocialReplies(card.transform, note);
                return;
            }

            if (note.Channel == "press" && note.ReplyGroup == "press-conference")
            {
                if (PhoneAnswered(key))
                {
                    AddPhoneLine(LocalizationManager.Current == GameLanguage.English
                        ? "The press conference has already been dealt with."
                        : "A conferência de imprensa já foi resolvida.");
                    return;
                }
                AddPressConferenceReplies(note, key);
                return;
            }

            if (PhoneAnswered(key))
            {
                AddPhoneLine("Já respondeste a esta mensagem.");
                return;
            }

            if (note.Channel == "jogador" && _career.World.Players.TryGetValue(note.PlayerId ?? "", out var player))
            {
                AddPlayerReplies(player, note.ReplyGroup, key);
                return;
            }

            if (note.Channel == "staff" && note.ReplyGroup == "staff-offer")
            {
                AddStaffOfferReplies(note, key);
                return;
            }

            if (note.Channel == "clube")
                AddClubReplies(note, key);
        }

        void AddPlayerReplies(Player player, string group, string key)
        {
            if (group == "injury")
            {
                AddPhoneChoice("A prioridade é recuperares sem pressa.", () => AnswerPlayer(player, key, 3, "Combinei que a recuperação viria antes do regresso."));
                AddPhoneChoice("O fisioterapeuta vai manter-me informado.", () => AnswerPlayer(player, key, 2, "Pedi ao fisioterapeuta uma atualização regular."));
                AddPhoneChoice("Falamos quando estiveres perto do regresso.", () => AnswerPlayer(player, key, 2, "Marquei uma conversa para quando estiver mais perto do regresso."));
                return;
            }

            if (group == "fatigue")
            {
                AddPhoneChoice("Vamos ajustar a tua carga com o preparador.", () => AnswerPlayer(player, key, 3, "Pedi ao preparador para rever a carga de treino."));
                AddPhoneChoice("Hoje recuperas; depois reavaliamos.", () => AnswerPlayer(player, key, 3, "Combinei reavaliar o cansaço depois da recuperação."));
                AddPhoneChoice("Avisa-me se o cansaço piorar.", () => AnswerPlayer(player, key, 2, "Pedi-lhe que me avisasse se o cansaço aumentasse."));
                return;
            }

            if (group == "selection")
            {
                AddPhoneChoice("Vou voltar a avaliar-te no treino.", () => AnswerPlayer(player, key, 2, "Disse-lhe que voltaria a avaliar a sua forma no treino."));
                AddPhoneChoice("Mantém o foco; a oportunidade pode surgir.", () => AnswerPlayer(player, key, 1, "Pedi-lhe que se mantivesse preparado para uma oportunidade."));
                AddPhoneChoice("Vamos rever o teu papel com o adjunto.", () => AnswerPlayer(player, key, 2, "Combinei rever a sua função com o adjunto."));
                return;
            }

            if (group == "leadership")
            {
                AddPhoneChoice("Ajuda-me a perceber o que está a inquietar o grupo.", () => AnswerPlayer(player, key, 4, "Pedi-lhe que me ajudasse a perceber o ambiente no balneário."));
                AddPhoneChoice("Fala com o grupo antes do próximo treino.", () => AnswerPlayer(player, key, 3, "Combinei que falaria com o grupo antes do próximo treino."));
                AddPhoneChoice("Vamos explicar o plano juntos à equipa.", () => AnswerPlayer(player, key, 3, "Combinei alinhar o plano com o jogador e explicá-lo ao grupo."));
                return;
            }

            if (group == "ready")
            {
                AddPhoneChoice("Prepara-te; posso precisar de ti.", () => AnswerPlayer(player, key, 3, "Disse-lhe para se preparar para o próximo jogo."));
                AddPhoneChoice("Revemos a tua função no treino.", () => AnswerPlayer(player, key, 2, "Combinei rever a sua função no treino."));
                AddPhoneChoice("O adjunto explica-te o plano.", () => AnswerPlayer(player, key, 1, "Pedi ao adjunto que lhe explicasse o plano."));
                return;
            }

            if (group == "morale")
            {
                AddPhoneChoice("Vamos conversar e esclarecer o teu papel.", () => AnswerPlayer(player, key, 4, "Marquei uma conversa para perceber o que está a afetar a confiança do jogador."));
                AddPhoneChoice("Vou ouvir também a perspetiva do adjunto.", () => AnswerPlayer(player, key, 2, "Pedi ao adjunto uma avaliação do papel e do momento do jogador."));
                AddPhoneChoice("Mostra no treino que estás preparado para ajudar.", () => AnswerPlayer(player, key, 1, "Expliquei que a resposta em campo também conta para recuperar espaço no grupo."));
                return;
            }

            AddPhoneChoice("Vou ouvir-te e falamos amanhã.", () => AnswerPlayer(player, key, 4, "Marquei uma conversa para ouvir o jogador."));
            AddPhoneChoice("Explico-te melhor o teu papel na equipa.", () => AnswerPlayer(player, key, 3, "Expliquei que iria clarificar o seu papel."));
            AddPhoneChoice("A oportunidade depende do treino.", () => AnswerPlayer(player, key, 1, "Reforcei que as oportunidades dependem do trabalho no treino."));
        }

        void AddClubReplies(PhoneNote note, string key)
        {
            if (note.ReplyGroup == "job-offer")
            {
                var offer = FindJobOffer(note.ClubId);
                if (offer == null)
                {
                    AddPhoneLine("Esta proposta já não está disponível.");
                    return;
                }

                AddPhoneChoice("ACEITAR  ·  assumir o clube", () => AcceptJobOffer(offer));
                AddPhoneChoice("RECUSAR proposta", () => DeclineJobOffer(offer, key));
                return;
            }

            var isFixtureMessage = note.ReplyGroup == "fixture"
                || note.Text.IndexOf("jogamos a ", System.StringComparison.OrdinalIgnoreCase) >= 0
                || note.Text.IndexOf("próximo jogo é nosso", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (isFixtureMessage)
            {
                var next = _career.Fixtures.Find(fixture => !fixture.Played && !fixture.Walkover && fixture.Involves(_career.Club.Id) && fixture.OpponentId(_career.Club.Id) == note.ClubId);
                var reply = next != null && next.IsHome(_career.Club.Id)
                    ? "Vamos preparar a equipa para vos receber."
                    : "Vamos preparar a deslocação.";
                AddPhoneChoice("Até lá. Vai ser um bom jogo.", () => AnswerClub(note, key, "Respondi que nos encontraríamos no dia do jogo."));
                AddPhoneChoice(reply, () => AnswerClub(note, key, reply));
                AddPhoneChoice("Falamos depois do apito final.", () => AnswerClub(note, key, "Disse que voltaríamos a falar depois do jogo."));
                return;
            }

            if (note.ReplyGroup == "loan")
            {
                var english = LocalizationManager.Current == GameLanguage.English;
                AddPhoneChoice(english ? "ACCEPT LOAN OFFER" : "ACEITAR EMPRÉSTIMO",
                    () => ResolveIncomingOffer(note, key, note.OfferAmount, false));
                AddPhoneChoice(english ? "COUNTER THE LOAN FEE" : "CONTRA-PROPOR VALOR",
                    () => OpenIncomingOfferNegotiation(note, key, true));
                AddPhoneChoice(english ? "REJECT THE APPROACH" : "RECUSAR A ABORDAGEM",
                    () => AnswerClub(note, key, LocalizationManager.Current == GameLanguage.English ? "I rejected the loan approach." : "Recusei a abordagem de empréstimo."));
                return;
            }

            if (note.ReplyGroup == "transfer")
            {
                var english = LocalizationManager.Current == GameLanguage.English;
                AddPhoneChoice(english ? "ACCEPT OFFER" : "ACEITAR PROPOSTA",
                    () => ResolveIncomingOffer(note, key, note.OfferAmount, false));
                AddPhoneChoice(english ? "COUNTEROFFER" : "CONTRA-PROPOSTA",
                    () => OpenIncomingOfferNegotiation(note, key, false));
                AddPhoneChoice(english ? "REJECT OFFER" : "RECUSAR PROPOSTA",
                    () => AnswerClub(note, key, LocalizationManager.Current == GameLanguage.English ? "I rejected the transfer offer and kept the player." : "Recusei a proposta e mantive o jogador no plantel."));
                return;
            }

            if (note.ReplyGroup.StartsWith("transfer-response-", StringComparison.Ordinal)
                || note.ReplyGroup.StartsWith("loan-response-", StringComparison.Ordinal))
            {
                var english = LocalizationManager.Current == GameLanguage.English;
                var loanResponse = note.ReplyGroup.StartsWith("loan-response-", StringComparison.Ordinal);
                var rejected = note.ReplyGroup.EndsWith("-rejected", StringComparison.Ordinal);
                if (rejected)
                {
                    AddPhoneChoice(english ? "GOT IT" : "PERCEBIDO", () => AnswerClub(note, key,
                        english ? "Negotiations ended; the player stays at the club." : "A negociação terminou; o jogador mantém-se no clube."));
                    return;
                }
                AddPhoneChoice(english ? (loanResponse ? "ACCEPT LOAN TERMS" : "ACCEPT AGREED FEE")
                    : (loanResponse ? "ACEITAR EMPRÉSTIMO" : "ACEITAR VALOR ACORDADO"),
                    () => CompleteIncomingOffer(note, key, note.OfferAmount, loanResponse));
                if (note.ReplyGroup.EndsWith("-countered", StringComparison.Ordinal) && note.OfferRound < 2)
                    AddPhoneChoice(english ? "SEND ONE MORE COUNTER" : "ENVIAR OUTRA CONTRA-PROPOSTA",
                        () => QueueIncomingCounter(note, key, Mathf.RoundToInt(note.OfferAmount * (loanResponse ? 1.15f : 1.08f))));
                AddPhoneChoice(english ? "REJECT TERMS" : "RECUSAR TERMOS", () => AnswerClub(note, key,
                    english ? "I rejected the revised terms and kept the player." : "Recusei os novos termos e mantive o jogador no plantel."));
                return;
            }

            AddPhoneChoice("Obrigado pela mensagem. Vou analisar o assunto.", () => AnswerClub(note, key, "Agradeci a mensagem e disse que iria analisar o assunto."));
            AddPhoneChoice("Falem primeiro com o meu adjunto.", () => AnswerClub(note, key, "Encaminhei o contacto para o adjunto avaliar."));
            AddPhoneChoice("Agora não é uma boa altura para conversarmos.", () => AnswerClub(note, key, "Adiei a conversa para outro momento."));
        }

        void AddStaffOfferReplies(PhoneNote note, string key)
        {
            var week = 0;
            int.TryParse(note.Meta, out week);
            var english = LocalizationManager.Current == GameLanguage.English;
            var offers = StaffOfferCandidates(week);
            foreach (var offer in offers)
            {
                var candidate = offer;
                var role = LocalizationManager.Translate(PresentationLabels.StaffRole(candidate.Member.Role));
                var label = (english ? "NEGOTIATE  ·  " : "NEGOCIAR  ·  ") + candidate.Member.FullName + "  ·  " + role
                    + "  ·  " + Euro(candidate.AskingWage) + (english ? "/wk" : "/sem");
                AddPhoneChoice(label, () => OpenStaffContractOffer(candidate, key));
            }
            AddPhoneChoice(english ? "NOT NOW" : "FALAR MAIS TARDE", () => AnswerClub(note, key,
                english ? "I will review the staff candidates later." : "Vou rever as candidaturas da equipa técnica mais tarde."));
        }

        void AddPressConferenceReplies(PhoneNote note, string key)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            AddPhoneChoice(english ? "ATTEND THE PRESS CONFERENCE" : "IR À CONFERÊNCIA DE IMPRENSA", () =>
            {
                _enterPressRoom = true;
                OpenUrgentDuty();
            });
            AddPhoneChoice(english ? "SEND THE ASSISTANT" : "ENVIAR O ADJUNTO", () => FinishDuty("press", true));
            AddPhoneChoice(english ? "DO NOT ATTEND" : "NÃO IR", () => FinishDuty("press", false, true));
        }

        bool PhoneAnswered(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (_phoneAnswered.Contains(key)) return true;
            var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key));
            return _career != null && _career.Notebook.Contains("phone-answer|" + encoded);
        }

        void MarkPhoneAnswered(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            _phoneAnswered.Add(key);
            if (_career == null) return;
            var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key));
            var marker = "phone-answer|" + encoded;
            if (!_career.Notebook.Contains(marker))
                _career.Notebook.Add(marker);
        }

        void OpenIncomingOfferNegotiation(PhoneNote note, string phoneKey, bool loan)
        {
            if (note == null || string.IsNullOrEmpty(note.PlayerId)
                || !_career.World.Players.TryGetValue(note.PlayerId, out var player)
                || !_career.World.Clubs.TryGetValue(note.ClubId ?? "", out var buyer))
                return;
            var previous = transform.Find("ReceivedOfferSheet");
            if (previous != null) Destroy(previous.gameObject);
            var english = LocalizationManager.Current == GameLanguage.English;
            var titleText = loan ? (english ? "LOAN OFFER NEGOTIATION" : "NEGOCIAÇÃO · OFERTA DE EMPRÉSTIMO")
                : (english ? "TRANSFER OFFER NEGOTIATION" : "NEGOCIAÇÃO · PROPOSTA DE TRANSFERÊNCIA");
            var sheet = BroadcastUi.Panel(transform, "ReceivedOfferSheet", new Color(0.055f, 0.075f, 0.11f, 1f), true);
            BroadcastUi.Place(sheet.rectTransform, 0.035f, 0.07f, 0.965f, 0.96f);
            BroadcastUi.ApplySandPaper(sheet, 0.1f);
            sheet.transform.SetAsLastSibling();
            var close = BroadcastUi.Button(sheet.transform, "Close", "×", () => Destroy(sheet.gameObject), new Color(1f, 1f, 1f, 0.12f), Color.white, 20);
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.94f, 0.91f, 0.985f, 0.98f);
            var heading = BroadcastUi.Label(sheet.transform, "Title", titleText, 24, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.025f, 0.9f, 0.82f, 0.985f);

            var header = BroadcastUi.Panel(sheet.transform, "Header", new Color(0.09f, 0.12f, 0.17f, 1f), false);
            BroadcastUi.Place(header.rectTransform, 0.025f, 0.72f, 0.975f, 0.87f);
            var faceSlot = BroadcastUi.Zone(header.transform, "Face", 0.015f, 0.08f, 0.12f, 0.92f);
            FaceImage(faceSlot, player, 60f);
            var playerName = BroadcastUi.Label(header.transform, "Player", player.FullName, 22, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(playerName.rectTransform, 0.14f, 0.48f, 0.58f, 0.94f);
            var playerMeta = BroadcastUi.Label(header.transform, "PlayerMeta", player.Age + (english ? " years  ·  " : " anos  ·  ")
                + ProfilePosition(player.Position, english) + "  ·  " + (english ? "OVR " : "GER ") + PlayerAttributes.Overall(player), 14, UiTheme.Accent, TextAnchor.MiddleLeft);
            BroadcastUi.Place(playerMeta.rectTransform, 0.14f, 0.08f, 0.58f, 0.5f);
            var crestSlot = BroadcastUi.Zone(header.transform, "ClubCrest", 0.61f, 0.12f, 0.69f, 0.88f);
            CrestImage(crestSlot, buyer, 38f);
            var clubName = BroadcastUi.Label(header.transform, "Club", buyer.Name, 18, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(clubName.rectTransform, 0.7f, 0.12f, 0.98f, 0.88f);

            var terms = BroadcastUi.Panel(sheet.transform, "Terms", new Color(0.08f, 0.1f, 0.15f, 1f), false);
            BroadcastUi.Place(terms.rectTransform, 0.025f, 0.23f, 0.46f, 0.68f);
            SectionTitle(terms.transform, english ? "OPENING TERMS" : "TERMOS INICIAIS");
            ContractRow(terms.transform, "Value", loan ? (english ? "Loan fee" : "Valor do empréstimo") : (english ? "Transfer fee" : "Valor da transferência"), Euro(note.OfferAmount), 0.76f);
            ContractRow(terms.transform, "Market", english ? "Estimated market value" : "Valor de mercado estimado", Euro(GlobalTransferMarket.MarketValue(player)), 0.59f);
            ContractRow(terms.transform, "Wage", loan ? (english ? "Weekly wage paid by borrower" : "Salário semanal pago pelo destino")
                : (english ? "Weekly wage offered" : "Salário semanal proposto"), Euro(note.OfferWeeklyWage) + (english ? "/wk" : "/sem"), 0.42f);
            ContractRow(terms.transform, "Length", loan ? (english ? "Loan term" : "Duração do empréstimo") : (english ? "Contract" : "Contrato"),
                loan ? (english ? "Until season end" : "Até ao fim da época") : note.OfferYears + (english ? " years" : " anos"), 0.25f);

            var counter = BroadcastUi.Panel(sheet.transform, "CounterTerms", new Color(0.08f, 0.1f, 0.15f, 1f), false);
            BroadcastUi.Place(counter.rectTransform, 0.48f, 0.23f, 0.975f, 0.68f);
            SectionTitle(counter.transform, english ? "YOUR RESPONSE" : "A TUA RESPOSTA");
            var description = BroadcastUi.Label(counter.transform, "Description", loan
                ? (english ? "Accept the proposed loan, ask for a higher fee through the end of the season, or turn it down. The borrowing club covers the player's weekly wage."
                    : "Aceita o empréstimo, pede um valor maior até ao fim da época ou recusa. O clube de destino paga o salário semanal.")
                : (english ? "Accept the fee or ask for a higher price. If accepted, the move is scheduled and the player stays until the agreed arrival date."
                    : "Aceita o valor ou pede mais. Se houver acordo, a mudança fica marcada e o jogador continua até à data de saída."),
                15, Color.white, TextAnchor.UpperLeft);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            BroadcastUi.Place(description.rectTransform, 0.05f, 0.08f, 0.95f, 0.9f);

            var accept = BroadcastUi.Button(sheet.transform, "Accept", english ? "ACCEPT OFFER" : "ACEITAR PROPOSTA",
                () => ResolveIncomingOffer(note, phoneKey, note.OfferAmount, false), UiTheme.Accent, UiTheme.ButtonText, 14);
            BroadcastUi.Place(accept.GetComponent<RectTransform>(), 0.025f, 0.08f, 0.32f, 0.18f);
            var counterButton = BroadcastUi.Button(sheet.transform, "Counter", english ? "COUNTEROFFER" : "CONTRA-PROPOSTA",
                () => ResolveIncomingOffer(note, phoneKey, Mathf.RoundToInt(note.OfferAmount * (loan ? 1.3f : 1.15f)), true), new Color(0.2f, 0.25f, 0.34f, 1f), Color.white, 14);
            BroadcastUi.Place(counterButton.GetComponent<RectTransform>(), 0.34f, 0.08f, 0.66f, 0.18f);
            var reject = BroadcastUi.Button(sheet.transform, "Reject", english ? "REJECT" : "RECUSAR",
                () =>
                {
                    var current = transform.Find("ReceivedOfferSheet");
                    if (current != null) Destroy(current.gameObject);
                    AnswerClub(note, phoneKey, loan
                        ? (english ? "I rejected the loan offer and kept the player." : "Recusei o empréstimo e mantive o jogador.")
                        : (english ? "I rejected the transfer bid and kept the player." : "Recusei a proposta e mantive o jogador."));
                }, new Color(0.38f, 0.12f, 0.16f, 1f), Color.white, 14);
            BroadcastUi.Place(reject.GetComponent<RectTransform>(), 0.68f, 0.08f, 0.975f, 0.18f);
        }

        void ResolveIncomingOffer(PhoneNote note, string phoneKey, int fee, bool counter)
        {
            if (note == null || _career == null || !_career.World.Players.TryGetValue(note.PlayerId ?? "", out var player)
                || !_career.World.Clubs.TryGetValue(note.ClubId ?? "", out var buyer))
                return;
            var english = LocalizationManager.Current == GameLanguage.English;
            var loan = note.ReplyGroup == "loan";
            if (counter)
            {
                QueueIncomingCounter(note, phoneKey, fee);
                var counterSheet = transform.Find("ReceivedOfferSheet");
                if (counterSheet != null) Destroy(counterSheet.gameObject);
                return;
            }
            if (!TransferWindowOpen())
            {
                AnswerClub(note, phoneKey, english ? "The transfer window closed before we reached terms." : "O mercado fechou antes de chegarmos a acordo.");
            }
            else if (player.ClubId != _career.Club.Id || _career.Club.PlayerIds.Count <= 11)
            {
                AnswerClub(note, phoneKey, english ? "The player is no longer available for a move." : "O jogador já não está disponível para sair.");
            }
            else if (buyer.Finance == null || buyer.Finance.TransferBudget < fee)
            {
                AnswerClub(note, phoneKey, english ? "The club could not fund the proposed terms." : "O clube não conseguiu financiar os termos propostos.");
            }
            else if (loan)
            {
                var currentDate = _career.Calendar.CurrentDate.Date;
                var seasonEnd = new DateTime(currentDate.Month >= 7 ? currentDate.Year + 1 : currentDate.Year, 6, 30);
                var daysUntilReturn = Math.Max(30, (seasonEnd - currentDate.AddDays(5)).Days);
                if (!GlobalTransferMarket.ScheduleLoan(_career, player, buyer, fee, 5, daysUntilReturn))
                    AnswerClub(note, phoneKey, english ? "The loan could not be registered. The squad and budget are unchanged." : "Não foi possível registar o empréstimo. O plantel e o orçamento mantêm-se.");
                else
                    AnswerClub(note, phoneKey, english
                        ? "Loan accepted for " + Euro(fee) + ". " + player.FullName + " joins " + buyer.Name + " in five days and returns after the agreed spell."
                        : "Empréstimo aceite por " + Euro(fee) + ". " + player.FullName + " segue para o " + buyer.Name + " daqui a cinco dias e regressa no fim do prazo.");
            }
            else if (!GlobalTransferMarket.ScheduleMove(_career, player, buyer, fee, Mathf.Clamp(note.OfferYears, 1, 5), note.OfferWeeklyWage, "out", 5))
                AnswerClub(note, phoneKey, english ? "The transfer could not be registered. The squad and budget are unchanged." : "Não foi possível registar a transferência. O plantel e o orçamento mantêm-se.");
            else
                AnswerClub(note, phoneKey, english
                    ? "Offer accepted for " + Euro(fee) + ". " + player.FullName + " stays until the move is completed in five days."
                    : "Proposta aceite por " + Euro(fee) + ". " + player.FullName + " continua no plantel até à mudança, daqui a cinco dias.");
            var sheet = transform.Find("ReceivedOfferSheet");
            if (sheet != null) Destroy(sheet.gameObject);
            CareerStore.Save(_career);
        }

        void QueueIncomingCounter(PhoneNote note, string phoneKey, int fee)
        {
            if (note == null || _career == null || !_career.World.Players.TryGetValue(note.PlayerId ?? "", out var player)
                || !_career.World.Clubs.TryGetValue(note.ClubId ?? "", out var buyer))
                return;
            var english = LocalizationManager.Current == GameLanguage.English;
            var loan = note.ReplyGroup == "loan" || note.ReplyGroup.StartsWith("loan-response-", StringComparison.Ordinal);
            if (!TransferWindowOpen() || player.ClubId != _career.Club.Id || buyer.Finance == null)
            {
                AnswerClub(note, phoneKey, english ? "The offer is no longer available to negotiate." : "A proposta já não está disponível para negociação.");
                return;
            }

            fee = Math.Max(note.OfferAmount + 1, fee);
            var marketValue = Math.Max(1, GlobalTransferMarket.MarketValue(player));
            var round = note.OfferRound + 1;
            var responseKind = "rejected";
            var responseFee = 0;
            var comparisonValue = loan ? Math.Max(1, note.OfferAmount) : marketValue;
            var acceptable = loan ? comparisonValue * (round == 1 ? 1.25f : 1.35f)
                : marketValue * (round == 1 ? 1.22f : 1.3f);
            if (fee <= buyer.Finance.TransferBudget && fee <= acceptable)
            {
                responseKind = "accepted";
                responseFee = fee;
            }
            else if (round < 3 && fee <= buyer.Finance.TransferBudget
                && fee <= comparisonValue * (loan ? 1.8f : 1.6f))
            {
                responseFee = loan
                    ? Mathf.RoundToInt(comparisonValue * 1.12f)
                    : Mathf.RoundToInt(marketValue * (round == 1 ? 1.08f : 1.14f));
                responseFee = Mathf.Min(fee - 1, Mathf.Max(note.OfferAmount, responseFee));
                if (responseFee > 0 && responseFee <= buyer.Finance.TransferBudget)
                    responseKind = "countered";
                else
                    responseFee = 0;
            }

            var delay = 2 + StableOfferRoll(note.Key + "|reply|" + fee) % 3;
            var dueTicks = _career.Calendar.CurrentDate.Date.AddDays(delay).Ticks;
            var line = "job|incoming-counter|" + (loan ? "loan" : "transfer") + "|" + player.Id + "|" + buyer.Id + "|"
                + note.OfferAmount + "|" + fee + "|" + Math.Max(0, note.OfferWeeklyWage) + "|" + Math.Max(0, note.OfferYears) + "|"
                + responseFee + "|" + responseKind + "|" + round + "|" + dueTicks;
            if (!_career.Notebook.Contains(line))
                _career.Notebook.Add(line);
            MarkPhoneAnswered(phoneKey);
            _career.Notebook.Insert(0, _career.Calendar.ClockText + "  " + (english
                ? "You sent a counteroffer to " + buyer.Name + ". They will reply in " + delay + " days."
                : "Enviaste uma contraproposta ao " + buyer.Name + ". A resposta deverá chegar dentro de " + delay + " dias."));
            ClosePhoneChoices();
            AddPhoneLine(english ? "Counteroffer sent. Waiting for the club to respond." : "Contraproposta enviada. Aguardas agora a resposta do clube.");
            CareerStore.Save(_career);
        }

        void CompleteIncomingOffer(PhoneNote note, string phoneKey, int fee, bool loan)
        {
            if (note == null || _career == null || !_career.World.Players.TryGetValue(note.PlayerId ?? "", out var player)
                || !_career.World.Clubs.TryGetValue(note.ClubId ?? "", out var buyer))
                return;
            var english = LocalizationManager.Current == GameLanguage.English;
            if (!TransferWindowOpen() || player.ClubId != _career.Club.Id || buyer.Finance == null
                || buyer.Finance.TransferBudget < fee)
            {
                AnswerClub(note, phoneKey, english ? "The agreed terms are no longer available. The player stays at the club." : "Os termos acordados já não estão disponíveis. O jogador mantém-se no clube.");
                return;
            }
            if (loan)
            {
                var date = _career.Calendar.CurrentDate.Date;
                var seasonEnd = new DateTime(date.Month >= 7 ? date.Year + 1 : date.Year, 6, 30);
                var returnDays = Math.Max(30, (seasonEnd - date.AddDays(5)).Days);
                if (!GlobalTransferMarket.ScheduleLoan(_career, player, buyer, fee, 5, returnDays))
                {
                    AnswerClub(note, phoneKey, english ? "The loan could not be registered. No money was charged." : "Não foi possível registar o empréstimo. Não foi debitado dinheiro.");
                    return;
                }
                AnswerClub(note, phoneKey, english ? "Loan terms accepted. The move is scheduled for five days from now." : "Termos do empréstimo aceites. A mudança ficou marcada para daqui a cinco dias.");
            }
            else
            {
                if (!GlobalTransferMarket.ScheduleMove(_career, player, buyer, fee, Mathf.Clamp(note.OfferYears, 1, 5), note.OfferWeeklyWage, "out", 5))
                {
                    AnswerClub(note, phoneKey, english ? "The transfer could not be registered. No money was charged." : "Não foi possível registar a transferência. Não foi debitado dinheiro.");
                    return;
                }
                AnswerClub(note, phoneKey, english ? "Transfer terms accepted. The move is scheduled for five days from now." : "Termos da transferência aceites. A mudança ficou marcada para daqui a cinco dias.");
            }
        }

        static int StableOfferRoll(string value)
        {
            unchecked
            {
                var hash = 17;
                for (var i = 0; i < value.Length; i++) hash = hash * 31 + value[i];
                return hash & 0x7fffffff;
            }
        }

        void AddPhoneChoice(string label, Action action)
        {
            var button = BroadcastUi.Button(_phoneList, "Choice", LocalizationManager.Translate(label), action, new Color(0.16f, 0.45f, 0.62f, 1f), Color.white, 14);
            var layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 48;
            layout.minHeight = 48;
        }

        void AddPhoneLine(string text)
        {
            var row = BroadcastUi.Panel(_phoneList, "Note", new Color(0.12f, 0.45f, 0.55f, 0.35f));
            BroadcastUi.Preferred(row, 64);
            var label = BroadcastUi.Label(row.transform, "Body", LocalizationManager.Translate(text), 14, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(label.rectTransform, 0.06f, 0.08f, 0.94f, 0.92f);
        }

        static int SpokenPhone(Player player, int morale)
        {
            if (player == null)
                return morale;
            var leader = player.HasTrait(PlayerTrait.Lideranca) || (player.Attributes != null && player.Attributes.Decisions >= 15);
            var sensitive = player.Age <= 21 || player.HasTrait(PlayerTrait.JogadorDeEquipa);
            if (morale <= 1 && leader)
                return 4;
            if (morale >= 3 && leader && player.Morale >= 50)
                return 1;
            if (morale <= 1 && sensitive)
                return -2;
            return morale;
        }

        void AnswerPlayer(Player player, string key, int morale, string line)
        {
            if (PhoneAnswered(key))
                return;
            morale = SpokenPhone(player, morale);
            var trustDelta = 0;
            if (player != null)
            {
                player.Morale = Mathf.Clamp(player.Morale + morale, 1, 99);
                trustDelta = CareerRelationships.ApplyConversation(_career, player, morale, 0, 0, 0);
                var promise = line.IndexOf("combinei", StringComparison.OrdinalIgnoreCase) >= 0
                    || line.IndexOf("marquei", StringComparison.OrdinalIgnoreCase) >= 0
                    || line.IndexOf("prometi", StringComparison.OrdinalIgnoreCase) >= 0;
                line = LocalizationManager.Translate(line);
                if (morale >= 3 || Mathf.Abs(trustDelta) >= 2 || promise)
                {
                    var memory = CareerMemories.Record(_career, new FootballMemory
                    {
                        WhenTicks = _career.Calendar.CurrentDate.Ticks,
                        Importance = 3,
                        Kind = "conversa-telemovel",
                        ClubId = _career.Club.Id,
                        CompetitionId = _career.Club.LeagueId,
                        PlayerId = player.Id,
                        Title = (LocalizationManager.Current == GameLanguage.English ? "Phone conversation with " : "Conversa no telemóvel com ") + player.FullName,
                        Detail = LocalizationManager.Current == GameLanguage.English
                            ? line + " Morale " + (morale > 0 ? "+" : "") + morale + "; manager trust " + (trustDelta > 0 ? "+" : "") + trustDelta + "."
                            : line + " Moral " + (morale > 0 ? "+" : "") + morale + "; confiança no treinador " + (trustDelta > 0 ? "+" : "") + trustDelta + "."
                    });
                    if (memory != null)
                        CareerNotificationQueue.Enqueue(
                            LocalizationManager.Current == GameLanguage.English ? "YOUR ANSWER MATTERS" : "A TUA RESPOSTA FICA",
                            LocalizationManager.Current == GameLanguage.English
                                ? player.FullName + " will remember how you responded. Trust is now " + CareerRelationships.TrustOf(_career, player.Id) + "/100."
                                : player.FullName + " vai lembrar-se da forma como respondeste. A confiança está em " + CareerRelationships.TrustOf(_career, player.Id) + "/100.");
                }
                CareerStore.Save(_career);
            }
            MarkPhoneAnswered(key);
            _career.Notebook.Insert(0, _career.Calendar.ClockText + "  " + (player == null
                ? (LocalizationManager.Current == GameLanguage.English ? "Squad" : "Plantel") : player.FullName) + " — " + line);
            ClosePhoneChoices();
            AddPhoneLine(line);
            CareerStore.Save(_career);
        }

        void AnswerClub(PhoneNote note, string key, string line)
        {
            if (PhoneAnswered(key))
                return;
            var refusedResponse = line.IndexOf("recus", StringComparison.OrdinalIgnoreCase) >= 0
                || line.IndexOf("mantive", StringComparison.OrdinalIgnoreCase) >= 0
                || line.IndexOf("fechei", StringComparison.OrdinalIgnoreCase) >= 0
                || line.IndexOf("I rejected", StringComparison.OrdinalIgnoreCase) >= 0
                || line.IndexOf("kept the player", StringComparison.OrdinalIgnoreCase) >= 0
                || line.IndexOf("negotiations ended", StringComparison.OrdinalIgnoreCase) >= 0
                || line.IndexOf("player stays at the club", StringComparison.OrdinalIgnoreCase) >= 0
                || line.IndexOf("negociação terminou", StringComparison.OrdinalIgnoreCase) >= 0
                || line.IndexOf("jogador mantém-se no clube", StringComparison.OrdinalIgnoreCase) >= 0;
            line = LocalizationManager.Translate(line);
            MarkPhoneAnswered(key);
            _career.Notebook.Insert(0, _career.Calendar.ClockText + "  " + note.Who + " — " + line);
            var incomingDealReply = note != null && (note.ReplyGroup == "transfer" || note.ReplyGroup == "loan"
                || note.ReplyGroup.StartsWith("transfer-response-", StringComparison.Ordinal)
                || note.ReplyGroup.StartsWith("loan-response-", StringComparison.Ordinal));
            if (incomingDealReply
                && !string.IsNullOrEmpty(note.PlayerId)
                && _career.World.Players.TryGetValue(note.PlayerId, out var player))
            {
                var refused = refusedResponse;
                var loan = note.ReplyGroup == "loan" || note.ReplyGroup.StartsWith("loan-response-", StringComparison.Ordinal);
                var kind = loan ? "abordagem-emprestimo" : "abordagem-transferencia";
                var english = LocalizationManager.Current == GameLanguage.English;
                var subject = loan ? (english ? "loan" : "empréstimo") : (english ? "transfer" : "transferência");
                var memory = CareerMemories.Record(_career, new FootballMemory
                {
                    WhenTicks = _career.Calendar.CurrentDate.Ticks,
                    Importance = refused ? 3 : 2,
                    Kind = kind,
                    ClubId = _career.Club.Id,
                    CompetitionId = _career.Club.LeagueId,
                    RelatedClubId = note.ClubId,
                    PlayerId = player.Id,
                    Title = refused
                        ? (english ? "The manager stood by " : "O treinador protegeu ") + player.FullName
                        : (english ? "Approach for " : "Uma abordagem por ") + player.FullName,
                    Detail = english
                        ? note.Who + " contacted the club about a " + subject + " for " + player.FullName + ". Manager's decision: " + line
                        : note.Who + " contactou o clube sobre um " + subject + " de " + player.FullName + ". Decisão do treinador: " + line
                });
                if (memory != null)
                {
                    CareerNotificationQueue.Enqueue(
                        english ? "TRANSFER DECISION" : "DECISÃO DE MERCADO",
                        english ? player.FullName + ": " + line + " This decision has been recorded."
                            : player.FullName + ": " + line + " Esta decisão ficou no histórico.");
                    CareerStore.Save(_career);
                }
            }
            RemoveIncomingCounterReply(note);
            ClosePhoneChoices();
            AddPhoneLine(line);
            CareerStore.Save(_career);
        }

        void RemoveIncomingCounterReply(PhoneNote note)
        {
            if (_career == null || note == null || string.IsNullOrEmpty(note.Meta)
                || !(note.ReplyGroup.StartsWith("transfer-response-", StringComparison.Ordinal)
                    || note.ReplyGroup.StartsWith("loan-response-", StringComparison.Ordinal))) return;
            for (var i = _career.Notebook.Count - 1; i >= 0; i--)
            {
                var line = _career.Notebook[i];
                if (string.IsNullOrEmpty(line) || !line.StartsWith("job|incoming-counter|", StringComparison.Ordinal)) continue;
                var bits = line.Split('|');
                if (bits.Length > 12 && bits[3] == note.PlayerId && bits[4] == note.ClubId && bits[12] == note.Meta)
                    _career.Notebook.RemoveAt(i);
            }
        }

        CareerJobOffer FindJobOffer(string clubId)
        {
            foreach (var offer in _career.JobOffers)
            {
                if (!offer.Resolved && offer.ClubId == clubId)
                    return offer;
            }
            return null;
        }

        void AcceptJobOffer(CareerJobOffer offer)
        {
            if (!CareerJobOffers.Accept(_career, offer))
                return;
            CareerStore.Save(_career);
            GameRoot.Instance.Open(SceneNames.CareerHub);
        }

        void DeclineJobOffer(CareerJobOffer offer, string key)
        {
            CareerJobOffers.Decline(_career, offer);
            MarkPhoneAnswered(key);
            CareerStore.Save(_career);
            ClosePhoneChoices();
            AddPhoneLine("Recusaste a proposta. As outras continuam disponíveis.");
        }

        void ClosePhoneChoices()
        {
            if (_phoneList == null)
                return;
            for (var i = _phoneList.childCount - 1; i >= 0; i--)
            {
                var child = _phoneList.GetChild(i);
                if (child.name == "Choice")
                    Destroy(child.gameObject);
            }
        }

        bool HasIncomingCounterPending(string playerId)
        {
            if (_career == null || string.IsNullOrEmpty(playerId)) return false;
            foreach (var line in _career.Notebook)
            {
                if (string.IsNullOrEmpty(line) || !line.StartsWith("job|incoming-counter|", StringComparison.Ordinal)) continue;
                var bits = line.Split('|');
                if (bits.Length > 3 && bits[3] == playerId) return true;
            }
            return false;
        }

        bool HasNotebookPrefix(string prefix)
        {
            if (_career == null || string.IsNullOrEmpty(prefix)) return false;
            foreach (var line in _career.Notebook)
                if (line != null && line.StartsWith(prefix, StringComparison.Ordinal)) return true;
            return false;
        }

        void AddStoredWeeklyIncomingOffers(List<PhoneNote> list, int week, bool english)
        {
            var transferPrefix = "job|incoming-offer-week|" + week + "|";
            var loanPrefix = "job|incoming-loan-week|" + week + "|";
            foreach (var line in _career.Notebook)
            {
                if (string.IsNullOrEmpty(line)) continue;
                var bits = line.Split('|');
                if (line.StartsWith(transferPrefix, StringComparison.Ordinal) && bits.Length >= 6
                    && int.TryParse(bits[5], out var bid)
                    && _career.World.Players.TryGetValue(bits[3], out var player) && player.ClubId == _career.Club.Id
                    && _career.World.Clubs.TryGetValue(bits[4], out var buyer))
                {
                    list.Add(new PhoneNote(buyer.Name, english
                        ? buyer.Name + " has made an opening bid of " + Euro(bid) + " for " + player.FullName + ". The fee and contract are open to negotiation."
                        : buyer.Name + " apresentou uma primeira proposta de " + Euro(bid) + " por " + player.FullName + ". O valor e o contrato podem ser negociados.",
                        "clube", player.Id, buyer.Id, "transfer", week.ToString())
                    {
                        OfferAmount = bid,
                        OfferWeeklyWage = Math.Max(player.WeeklyWage, Mathf.RoundToInt(player.WeeklyWage * 1.08f)),
                        OfferYears = 3
                    });
                }
                else if (line.StartsWith(loanPrefix, StringComparison.Ordinal) && bits.Length >= 6
                    && int.TryParse(bits[5], out var loanFee)
                    && _career.World.Players.TryGetValue(bits[3], out var youth) && youth.ClubId == _career.Club.Id
                    && _career.World.Clubs.TryGetValue(bits[4], out var lender))
                {
                    list.Add(new PhoneNote(lender.Name, english
                        ? lender.Name + " has offered a loan fee of " + Euro(loanFee) + " for " + youth.FullName + " until the end of the season. They will cover the player's weekly wage."
                        : lender.Name + " oferece " + Euro(loanFee) + " pelo empréstimo de " + youth.FullName + " até ao fim da época. O clube cobre o salário semanal.",
                        "clube", youth.Id, lender.Id, "loan", week.ToString())
                    {
                        OfferAmount = loanFee,
                        OfferWeeklyWage = youth.WeeklyWage,
                        OfferYears = 0
                    });
                }
            }
        }

        void AddReadyCounterReplies(List<PhoneNote> list, bool english)
        {
            if (_career == null || _career.Calendar == null) return;
            foreach (var line in _career.Notebook)
            {
                if (string.IsNullOrEmpty(line) || !line.StartsWith("job|incoming-counter|", StringComparison.Ordinal)) continue;
                var bits = line.Split('|');
                if (bits.Length < 13 || !long.TryParse(bits[12], out var dueTicks)
                    || new DateTime(dueTicks).Date > _career.Calendar.CurrentDate.Date
                    || !_career.World.Players.TryGetValue(bits[3], out var player)
                    || !_career.World.Clubs.TryGetValue(bits[4], out var buyer)) continue;
                int.TryParse(bits[6], out var requestedFee);
                int.TryParse(bits[7], out var wage);
                int.TryParse(bits[8], out var years);
                int.TryParse(bits[9], out var responseFee);
                int.TryParse(bits[11], out var round);
                var loan = bits[2] == "loan";
                var kind = bits[10];
                var group = (loan ? "loan" : "transfer") + "-response-" + kind;
                var text = kind == "accepted"
                    ? (english ? buyer.Name + " has accepted your counteroffer of " + Euro(requestedFee) + " for " + player.FullName + ". Review the agreed terms and confirm or decline." : buyer.Name + " aceitou a contraproposta de " + Euro(requestedFee) + " por " + player.FullName + ". Confirma ou recusa os termos acordados.")
                    : kind == "countered"
                        ? (english ? buyer.Name + " has replied with a revised offer of " + Euro(responseFee) + " for " + player.FullName + ". You can accept, counter once more or walk away." : buyer.Name + " respondeu com uma proposta revista de " + Euro(responseFee) + " por " + player.FullName + ". Podes aceitar, voltar a contrapropor ou desistir.")
                        : (english ? buyer.Name + " has withdrawn its approach for " + player.FullName + ". No agreement was reached." : buyer.Name + " retirou a abordagem por " + player.FullName + ". Não houve acordo.");
                var note = new PhoneNote(buyer.Name, text, "clube", player.Id, buyer.Id, group, dueTicks.ToString())
                {
                    OfferAmount = kind == "countered" ? responseFee : requestedFee,
                    OfferWeeklyWage = wage,
                    OfferYears = years,
                    OfferRound = round,
                    Important = true
                };
                list.Add(note);
            }
        }

        List<PhoneNote> PhoneNotes()
        {
            var list = new List<PhoneNote>();
            var english = LocalizationManager.Current == GameLanguage.English;
            OfferWeeklyStaff();
            var deskNotes = 0;
            if (PressDutyOpen())
            {
                var nextFixture = _career.NextUserFixture();
                var press = new PhoneNote(english ? "Press officer" : "Assessor de imprensa",
                    english ? "A press conference is scheduled before the next match. Choose whether to attend, send the assistant, or skip it."
                        : "Há uma conferência de imprensa antes do próximo jogo. Escolhe se vais, se envias o adjunto ou se não compareces.",
                    "press", null, _career.Club.Id, "press-conference", nextFixture == null ? "" : nextFixture.Id);
                press.Important = true;
                list.Add(press);
            }
            if (SquadDutyOpen())
            {
                var competitionName = CompetitionTitle(PendingRegistration());
                var squadNote = new PhoneNote(english ? "Board" : "Direção",
                    english ? "Register the squad for " + competitionName + " before it starts. The list lasts the whole season."
                        : "Inscreve o plantel na " + competitionName + " antes de a competição começar. A lista vale a época toda.",
                    "urgente", null, _career.Club.Id);
                squadNote.Important = true;
                list.Add(squadNote);
            }
            foreach (var note in _career.Board)
            {
                if (note == null || string.IsNullOrEmpty(note.Text) || !note.Text.StartsWith("✉") || deskNotes >= 4)
                    continue;
                var raw = note.Text.Substring(1).Trim();
                if (raw.IndexOf("\nxi|", StringComparison.Ordinal) >= 0
                    || raw.StartsWith("Sugestão do adjunto", StringComparison.Ordinal)
                    || raw.StartsWith("Adjunto\n", StringComparison.Ordinal))
                    continue;
                var meta = "";
                var marker = raw.LastIndexOf("[[", StringComparison.Ordinal);
                if (marker >= 0 && raw.EndsWith("]]", StringComparison.Ordinal))
                {
                    meta = raw.Substring(marker + 2, raw.Length - marker - 4);
                    raw = raw.Substring(0, marker).Trim();
                }
                raw = LocalizationManager.Translate(raw);
                if (meta.StartsWith("staff-offers:", StringComparison.Ordinal))
                {
                    var week = meta.Substring("staff-offers:".Length);
                    list.Add(new PhoneNote(english ? "Staff recruitment" : "Recrutamento de staff", raw, "staff", null, _career.Club.Id, "staff-offer", week));
                }
                else
                    list.Add(new PhoneNote(english ? "Board" : "Direção", raw, "gabinete", null, _career.Club.Id, null, meta));
                deskNotes++;
            }
            foreach (var offer in _career.JobOffers)
            {
                if (offer.Resolved || !_career.World.Clubs.TryGetValue(offer.ClubId ?? "", out var hiringClub))
                    continue;
                list.Add(new PhoneNote(hiringClub.Name, offer.Reason, "clube", null, hiringClub.Id, "job-offer"));
            }

            var club = _career.Club;
            var squad = new List<Player>();
            foreach (var id in club.PlayerIds)
            {
                if (_career.World.Players.TryGetValue(id, out var player))
                    squad.Add(player);
            }

            Fixture next = null;
            foreach (var fixture in _career.Fixtures)
            {
                if (fixture.Played || fixture.Walkover || !fixture.Involves(club.Id))
                    continue;
                if (next == null || fixture.Date < next.Date)
                    next = fixture;
            }

            Club foe = null;
            if (next != null)
                _career.World.Clubs.TryGetValue(next.OpponentId(club.Id), out foe);

            var seen = new HashSet<string>();
            Player low = null;
            Player tired = null;
            Player captain = null;
            Player star = null;
            Player youth = null;
            foreach (var player in squad)
            {
                if (low == null || player.Morale < low.Morale)
                    low = player;
                if (tired == null || player.Fatigue > tired.Fatigue)
                    tired = player;
                if (player.IsCaptain)
                    captain = player;
                if (star == null || PlayerAttributes.Overall(player) > PlayerAttributes.Overall(star))
                    star = player;
                if (player.Position != PlayerPosition.Goalkeeper && player.Age <= 21 && (youth == null || player.Age < youth.Age))
                    youth = player;
            }

            if (low != null && low.Morale < 72)
                AddPlayerNote(list, seen, low, PlayerVoice(low, club));
            if (tired != null && tired.Fatigue >= 58)
                AddPlayerNote(list, seen, tired, english
                    ? tired.FullName.Split(' ')[0] + " says their legs feel heavy. Playing again straight away could affect their performance."
                    : tired.FullName.Split(' ')[0] + " escreveu: as pernas estão pesadas. Se jogar seguido, o rendimento cai.");
            var listedShown = 0;
            foreach (var id in _career.Listed)
            {
                if (listedShown >= 3 || !_career.World.Players.TryGetValue(id, out var listed) || listed.ClubId != club.Id)
                    continue;
                AddPlayerNote(list, seen, listed, ListingReaction(listed, english));
                listedShown++;
            }
            if (captain != null)
                AddPlayerNote(list, seen, captain, captain.Morale >= 60
                    ? (english ? "The squad is behind you. I can speak to them before training if you want." : "O grupo está contigo. Se quiseres, falo com eles antes do treino.")
                    : (english ? "The dressing room is unsettled. A word from you before the next match could help." : "O balneário está inquieto. Vale a pena uma palavra tua antes do próximo jogo."));
            foreach (var player in squad)
            {
                if (player.Position == PlayerPosition.Goalkeeper || seen.Contains(player.Id))
                    continue;
                if (player.Personality != PersonalityTrait.Ambitious && player.Personality != PersonalityTrait.Outspoken)
                    continue;
                var benched = club.ManualLineup != null && club.ManualLineup.Count >= 11 && !club.ManualLineup.Contains(player.Id);
                if (!benched && player.Morale >= 55)
                    continue;
                AddPlayerNote(list, seen, player, PlayerVoice(player, club));
                break;
            }

            Club buyer = null;
            Club lender = null;
            foreach (var other in _career.World.Clubs.Values)
            {
                if (other.Id == club.Id || other.LeagueId != club.LeagueId)
                    continue;
                if (buyer == null || other.Reputation > buyer.Reputation)
                    buyer = other;
                if (other.Reputation < club.Reputation && (lender == null || other.Reputation < lender.Reputation))
                    lender = other;
            }

            var transferWindow = TransferWindowOpen();
            var offerWeek = _career.Calendar.CurrentDate.Year * 100 + ((_career.Calendar.CurrentDate.DayOfYear - 1) / 7);
            var weeklyOfferPrefix = "job|incoming-offer-week|" + offerWeek + "|";
            var weeklyLoanPrefix = "job|incoming-loan-week|" + offerWeek + "|";
            AddStoredWeeklyIncomingOffers(list, offerWeek, english);
            if (transferWindow && star != null && _career.Club.PlayerIds.Count > 11 && !GlobalTransferMarket.MovePending(_career, star.Id)
                && !HasIncomingCounterPending(star.Id) && !HasNotebookPrefix(weeklyOfferPrefix))
            {
                var contenders = GlobalTransferMarket.Contenders(_career, star, false);
                var value = GlobalTransferMarket.MarketValue(star);
                if (contenders.Count > 0)
                {
                    var interestedClub = contenders[0];
                    var multiplier = interestedClub.Reputation >= club.Reputation ? 1.08f : 0.98f;
                    var bid = Mathf.Clamp(Mathf.RoundToInt(value * multiplier), 80000, interestedClub.Finance.TransferBudget);
                    var bidNote = new PhoneNote(interestedClub.Name, english
                        ? interestedClub.Name + " has made an opening bid of " + Euro(bid) + " for " + star.FullName + ". The fee and contract are open to negotiation."
                        : interestedClub.Name + " apresentou uma primeira proposta de " + Euro(bid) + " por " + star.FullName + ". O valor e o contrato podem ser negociados.",
                        "clube", star.Id, interestedClub.Id, "transfer", offerWeek.ToString())
                    {
                        OfferAmount = bid,
                        OfferWeeklyWage = Math.Max(star.WeeklyWage, Mathf.RoundToInt(star.WeeklyWage * 1.08f)),
                        OfferYears = 3
                    };
                    list.Add(bidNote);
                    _career.Notebook.Add(weeklyOfferPrefix + star.Id + "|" + interestedClub.Id + "|" + bid);
                    CareerStore.Save(_career);
                }
            }
            if (transferWindow && lender != null && youth != null && lender.Id != (buyer == null ? "" : buyer.Id)
                && _career.Club.PlayerIds.Count > 11 && !GlobalTransferMarket.MovePending(_career, youth.Id)
                && !HasIncomingCounterPending(youth.Id) && !HasNotebookPrefix(weeklyLoanPrefix))
            {
                var loanFee = Mathf.Max(10000, Mathf.RoundToInt(GlobalTransferMarket.MarketValue(youth) * 0.045f));
                if (lender.Finance != null && lender.Finance.TransferBudget >= loanFee)
                {
                    var loanNote = new PhoneNote(lender.Name, english
                        ? lender.Name + " has offered a loan fee of " + Euro(loanFee) + " for " + youth.FullName + " until the end of the season. They will cover the player's weekly wage."
                        : lender.Name + " oferece " + Euro(loanFee) + " pelo empréstimo de " + youth.FullName + " até ao fim da época. O clube cobre o salário semanal.",
                        "clube", youth.Id, lender.Id, "loan", offerWeek.ToString())
                    {
                        OfferAmount = loanFee,
                        OfferWeeklyWage = youth.WeeklyWage,
                        OfferYears = 0
                    };
                    list.Add(loanNote);
                    _career.Notebook.Add(weeklyLoanPrefix + youth.Id + "|" + lender.Id + "|" + loanFee);
                    CareerStore.Save(_career);
                }
            }
            AddReadyCounterReplies(list, english);

            var assistant = StaffReport.Find(club, StaffRole.AssistantCoach);
            list.Add(new PhoneNote(assistant == null ? (english ? "Assistant" : "Adjunto") : assistant.FullName, foe == null
                ? (english ? "The squad is waiting for your instructions for the next training session." : "O plantel está à espera da tua palavra para o próximo treino.")
                : (english ? "Our next match is against " + foe.Name + ". Do you want to confirm the lineup or make changes?" : "O próximo jogo é com o " + foe.Name + ". Queres fechar o onze ou mexer na linha?"), "staff", null, null));
            if (tired != null && tired.Fatigue >= 58)
            {
                var fitness = StaffReport.Find(club, StaffRole.FitnessCoach);
                list.Add(new PhoneNote(fitness == null ? (english ? "Fitness coach" : "Preparador") : fitness.FullName, english
                    ? tired.FullName + " is feeling the workload. A lighter day could help prevent an injury."
                    : tired.FullName + " acusa o esforço. Um dia mais leve evita uma lesão.", "staff", tired.Id, null));
            }

            if (_career.Targets.Count > 0)
            {
                string targetId = null;
                foreach (var id in _career.Targets)
                {
                    targetId = id;
                    break;
                }

                if (targetId != null && _career.World.Players.TryGetValue(targetId, out var target))
                {
                    Club owner = null;
                    if (!string.IsNullOrEmpty(target.ClubId))
                        _career.World.Clubs.TryGetValue(target.ClubId, out owner);
                    var scout = StaffReport.Find(club, StaffRole.Scout);
                    list.Add(new PhoneNote(scout == null ? (english ? "Scout" : "Olheiro") : scout.FullName, english
                        ? target.FullName + " is still on your shortlist" + (owner == null ? "." : ". " + owner.Name + " may listen to an offer.")
                        : target.FullName + " continua na tua lista" + (owner == null ? "." : ". O " + owner.Name + " pode ouvir uma oferta."), "staff", target.Id, owner == null ? null : owner.Id));
                }
            }

            var physio = StaffReport.Find(club, StaffRole.Physiotherapist);
            foreach (var player in squad)
            {
                if (player.Injury == null || !player.Injury.IsInjured)
                    continue;
                var hurt = string.IsNullOrEmpty(player.Injury.Description) ? (english ? "injury" : "lesão") : LocalizationManager.Translate(player.Injury.Description);
                list.Add(new PhoneNote(physio == null ? (english ? "Physiotherapist" : "Fisioterapeuta") : physio.FullName, english
                    ? player.FullName + ": " + hurt + ". " + player.Injury.DaysRemaining + " days remaining."
                    : player.FullName + ": " + hurt + ". Faltam " + player.Injury.DaysRemaining + " dias.", "staff", player.Id, null));
                break;
            }

            if (next != null && foe != null)
                list.Add(new PhoneNote(english ? "Calendar" : "Calendário", LocalizationManager.FormatDate(next.Date, "dd/MM") + "  ·  " + (english ? (next.IsHome(club.Id) ? "Home" : "Away") : (next.IsHome(club.Id) ? "Casa" : "Fora")) + (english ? " vs " : " contra ") + foe.Name + ".", "calendario", null, foe.Id));
            else
                list.Add(new PhoneNote(english ? "Calendar" : "Calendário", english ? "No match is scheduled for the next few days." : "Não há jogo marcado nos próximos dias.", "calendario", null, null));

            if (_career.Press.Count > 0 && !string.IsNullOrEmpty(_career.Press[0].Headline))
                list.Add(new PhoneNote(string.IsNullOrEmpty(_career.Press[0].Paper) ? (english ? "Press" : "Imprensa") : PaperName(_career.Press[0].Paper), PressHeadline(_career.Press[0]), "jornal", null, null));
            else
                list.Add(new PhoneNote(english ? "Press" : "Imprensa", english ? "Today's papers haven't gone to press yet." : "Os jornais ainda não fecharam a edição de hoje.", "jornal", null, null));

            return list;
        }

        List<PhoneNote> SocialNotes()
        {
            var posts = new List<PhoneNote>();
            var english = LocalizationManager.Current == GameLanguage.English;
            var club = _career.Club;
            var daySeed = _career.Calendar.CurrentDate.DayOfYear + _career.Calendar.CurrentDate.Year * 13;
            var recent = new List<Fixture>();
            foreach (var fixture in _career.Fixtures)
            {
                if (fixture.Played && !fixture.Walkover && fixture.Involves(club.Id))
                    recent.Add(fixture);
            }
            recent.Sort((a, b) => b.Date.CompareTo(a.Date));

            var standing = 0;
            var sameLeague = new List<LeagueTableRow>();
            foreach (var row in _career.Table)
            {
                if (row.LeagueId == club.LeagueId)
                    sameLeague.Add(row);
            }
            sameLeague.Sort((a, b) =>
            {
                var points = b.Points.CompareTo(a.Points);
                if (points != 0) return points;
                var difference = b.GoalDifference.CompareTo(a.GoalDifference);
                return difference != 0 ? difference : b.GoalsFor.CompareTo(a.GoalsFor);
            });
            for (var i = 0; i < sameLeague.Count; i++)
            {
                if (sameLeague[i].ClubId == club.Id)
                {
                    standing = i + 1;
                    break;
                }
            }

            var localStar = BestPlayer(club.PlayerIds);
            if (recent.Count > 0)
            {
                var last = recent[0];
                var home = last.HomeClubId == club.Id;
                var scored = home ? last.HomeGoals : last.AwayGoals;
                var conceded = home ? last.AwayGoals : last.HomeGoals;
                var result = scored > conceded ? (english ? "win" : "vitória") : scored == conceded ? (english ? "draw" : "empate") : (english ? "defeat" : "derrota");
                var rivalId = last.OpponentId(club.Id);
                var rivalName = _career.World.Clubs.TryGetValue(rivalId, out var rival) ? rival.ShortName : (english ? "the opposition" : "o adversário");
                posts.Add(SocialPost(0, daySeed,
                    english
                        ? "After that " + result + " against " + rivalName + " (" + scored + "–" + conceded + "), people are already calling for the manager's head. Easy, lads, there's a whole season left."
                        : "Depois desta " + result + " com o " + rivalName + " (" + scored + "–" + conceded + "), já há quem peça a cabeça do treinador. Calma, malta, ainda há época.",
                    daySeed % 83 + 18, daySeed % 19 + 2, "há 2 h"));
            }
            else if (standing > 0 && sameLeague[standing - 1].Played > 0)
            {
                posts.Add(SocialPost(0, daySeed,
                    english
                        ? "We're " + standing + (standing == 1 ? "st" : standing == 2 ? "nd" : standing == 3 ? "rd" : "th") + " and people already say the season is over. Maybe check the table before holding the funeral."
                        : "Estamos em " + standing + ".º e ainda há quem diga que a época está perdida. Olhem para a tabela antes de fazerem funeral.",
                    daySeed % 61 + 12, daySeed % 13 + 1, "há 1 h"));
            }
            else
            {
                posts.Add(SocialPost(0, daySeed,
                    english ? "The season has barely started and people are already predicting disaster. Let the team play first." : "A época ainda agora começou e já há gente a prever o desastre. Deixem a equipa jogar primeiro.",
                    daySeed % 48 + 9, daySeed % 11 + 1, "há 3 h"));
            }

            if (localStar != null)
            {
                var scorer = BestSeasonPlayer(club.LeagueId, "golos");
                if (scorer != null)
                {
                    var line = SeasonLineFor(scorer.Id, club.LeagueId);
                    posts.Add(SocialPost(1, daySeed,
                        english
                            ? scorer.FullName + " has " + line.Goals + " goals. Keep this up and he'll have the whole stand eating their words by May."
                            : scorer.FullName + " leva " + line.Goals + " golos. Se continuar assim, ainda acaba a época a calar muita gente nesta bancada.",
                        daySeed % 120 + 30, daySeed % 24 + 4, "há 4 h"));
                }
                else
                {
                    posts.Add(SocialPost(1, daySeed,
                        english
                            ? localStar.FullName + " has the quality to be the face of this project. Now show it in the big games too."
                            : localStar.FullName + " tem qualidade para ser a cara deste projeto. Agora quero ver isso também nos jogos grandes.",
                        daySeed % 74 + 14, daySeed % 16 + 2, "há 5 h"));
                }
            }

            var awardCandidate = BestSeasonPlayer(null, "impacto") ?? BestPlayer(_career.World.Players.Keys);
            if (awardCandidate != null)
            {
                var awardLine = SeasonLineFor(awardCandidate.Id, null);
                if (awardLine != null && awardLine.Rated == 0 && awardLine.Goals == 0 && awardLine.Assists == 0)
                    awardLine = null;
                var awardText = awardLine == null
                    ? english
                        ? "Ballon d'Or watchlist: " + awardCandidate.FullName + " is one of the names to follow this season."
                        : "Ainda é cedo para a Bola de Ouro, mas " + awardCandidate.FullName + " é daqueles que vale a pena seguir esta época."
                    : english
                        ? "Ballon d'Or Bulletin: " + awardCandidate.FullName + " has " + awardLine.Goals + " goals and " + awardLine.Assists + " assists, putting them in the early award conversation."
                        : "Com " + awardLine.Goals + " golos e " + awardLine.Assists + " assistências, " + awardCandidate.FullName + " já merece entrar na conversa da Bola de Ouro.";
                posts.Add(SocialPost(2, daySeed, awardText, daySeed % 210 + 54, daySeed % 39 + 7, "há 6 h", 20));
            }

            var next = _career.NextUserFixture();
            if (next != null)
            {
                var opponentId = next.OpponentId(club.Id);
                var opponentName = _career.World.Clubs.TryGetValue(opponentId, out var opponent) ? opponent.ShortName : (english ? "the next opponent" : "o próximo adversário");
                posts.Add(SocialPost(3, daySeed,
                    english
                        ? "Against " + opponentName + " we'll find out if this team learned anything. No excuses and please, no parking the bus in front of goal."
                        : "Contra " + opponentName + " é que vamos perceber se esta equipa aprendeu alguma coisa. Sem desculpas e sem autocarro à frente da baliza, por favor.",
                    daySeed % 96 + 21, daySeed % 21 + 3, "há 8 h"));
            }
            else
            {
                posts.Add(SocialPost(3, daySeed,
                    english ? "No match scheduled yet. The feed is already debating next week's lineup like the manager reads this." : "Sem jogo marcado para já. O feed já está a discutir o onze da próxima jornada como se o treinador lesse isto.",
                    daySeed % 96 + 21, daySeed % 21 + 3, "há 8 h"));
            }

            LeagueTableRow clubRow = null;
            for (var i = 0; i < sameLeague.Count; i++)
            {
                if (sameLeague[i].ClubId == club.Id)
                {
                    clubRow = sameLeague[i];
                    break;
                }
            }
            if (clubRow != null)
            {
                posts.Add(SocialPost(4, daySeed,
                    english
                        ? club.ShortName + " are " + standing + (standing == 1 ? "st" : standing == 2 ? "nd" : standing == 3 ? "rd" : "th") + " with " + clubRow.Points + " points from " + clubRow.Played + " matches. The table doesn't lie; the pub just rounds everything up."
                        : club.ShortName + " está no " + standing + ".º lugar, com " + clubRow.Points + " pontos em " + clubRow.Played + " jogos. A tabela não mente; a malta do café é que arredonda tudo.",
                    daySeed % 145 + 26, daySeed % 28 + 4, "há 3 h"));
                posts.Add(SocialPost(5, daySeed,
                    english
                        ? "So far " + club.ShortName + " have scored " + clubRow.GoalsFor + " and conceded " + clubRow.GoalsAgainst + ". We all know Monday's pub topic already."
                        : "Até agora: " + clubRow.GoalsFor + " golos marcados e " + clubRow.GoalsAgainst + " sofridos pelo " + club.ShortName + ". Já sabem qual é o tema da conversa de segunda-feira.",
                    daySeed % 118 + 17, daySeed % 22 + 2, "há 4 h"));
            }
            else
            {
                posts.Add(SocialPost(4, daySeed,
                    english ? "The table is still taking shape. Naturally, everyone already has a definitive opinion about a season that just started." : "A tabela ainda está a ganhar forma. Já há opiniões definitivas sobre uma época que mal começou, claro.",
                    daySeed % 145 + 26, daySeed % 28 + 4, "há 3 h"));
                posts.Add(SocialPost(5, daySeed,
                    english ? "Today's serious debate: go direct or play out from the back? Official answer: depends who's watching." : "Discussão séria do dia: futebol direto ou sair a jogar? A resposta oficial é que depende de quem está a ver.",
                    daySeed % 118 + 17, daySeed % 22 + 2, "há 4 h"));
            }

            var bestInLeague = BestSeasonPlayer(club.LeagueId, "impacto");
            var bestLine = bestInLeague == null ? null : SeasonLineFor(bestInLeague.Id, club.LeagueId);
            if (bestLine != null && bestLine.Rated > 0)
            {
                posts.Add(SocialPost(6, daySeed,
                    english
                        ? bestInLeague.FullName + " has " + bestLine.Goals + " goals and " + bestLine.Assists + " assists. Too early for awards, but plenty to talk about at the pub."
                        : bestInLeague.FullName + " soma " + bestLine.Goals + " golos e " + bestLine.Assists + " assistências. Ainda é cedo para prémios, mas já dá conversa para o café.",
                    daySeed % 132 + 23, daySeed % 26 + 3, "há 5 h"));
            }
            else
            {
                posts.Add(SocialPost(6, daySeed,
                    english ? "We need more matches before drawing conclusions. But if we wait for the data, half this feed will have nothing to post." : "Ainda faltam jogos para tirar conclusões. Mas se esperarmos pelos dados, metade do feed fica sem assunto.",
                    daySeed % 132 + 23, daySeed % 26 + 3, "há 5 h"));
            }

            var dailyDebates = english
                ? new[]
                {
                    "The formation on paper isn't the issue; does anyone show between the lines when the ball reaches midfield?",
                    "People want a 90-minute high press, then act shocked when the team runs out of legs.",
                    "Today it's the ref's fault, tomorrow the pitch. By Wednesday no one remembers the score.",
                    "One misplaced forward pass and suddenly everyone's a tactics expert until next matchday.",
                    "The best signing might be getting more out of the squad we already have. Sadly, that makes for a boring transfer saga.",
                    "Fans want academy kids until one misplaces a pass. Then they want a veteran by yesterday."
                }
                : new[]
                {
                    "O problema não é a formação no papel; é ver se alguém aparece entre linhas quando a bola chega ao meio-campo.",
                    "Há quem queira pressão alta durante 90 minutos e depois fique surpreendido quando a equipa acaba sem pernas.",
                    "Hoje a culpa é do árbitro, amanhã é do relvado e na quarta-feira já ninguém se lembra do resultado.",
                    "Um passe para a frente que corre mal dá logo debate tático até à próxima jornada.",
                    "O melhor reforço pode ser recuperar quem já está no plantel. Mas isso não dá uma saga de transferências.",
                    "A malta pede miúdos da formação até um deles falhar um passe. Aí já querem um veterano para ontem."
                };
            posts.Add(SocialPost(7, daySeed, club.ShortName + ": " + dailyDebates[daySeed % dailyDebates.Length],
                daySeed % 102 + 15, daySeed % 20 + 2, "há 6 h"));

            var leaderName = "";
            if (sameLeague.Count > 0 && sameLeague[0].Played > 0
                && _career.World.Clubs.TryGetValue(sameLeague[0].ClubId, out var leagueLeader))
                leaderName = leagueLeader.ShortName;
            var hasLeagueLeader = !string.IsNullOrEmpty(leaderName);
            var leagueTalk = hasLeagueLeader
                ? new[]
                {
                    english ? "The race for the top will change twenty more times. For now, " + leaderName + " lead the table." : "A corrida pelo topo ainda vai mudar vinte vezes. Para já, " + leaderName + " lidera a tabela.",
                    english ? "Everyone's watching " + leaderName + " at the top, but there's so much season left this will turn around." : "Toda a gente olha para " + leaderName + " no topo, mas ainda falta tanto calendário que isto vai dar voltas.",
                    english ? "If the season ended today, " + leaderName + " would be smiling. Good thing no one's handing out trophies yet." : "Se a época acabasse hoje, " + leaderName + " sorria. Ainda bem que ninguém entrega troféus já."
                }
                : new[]
                {
                    english ? "The table is still taking shape. People are already calculating the title race before we've played enough matches." : "A tabela ainda está a ganhar forma. Já há contas para o título antes de haver jogos suficientes.",
                    english ? "No clear leader yet. The feed, meanwhile, has already picked three favourites." : "Ainda não há líder claro na classificação. O feed, por outro lado, já escolheu três favoritos.",
                    english ? "Once we've played enough matches, the table will tell the story. Until then, it's guesswork." : "Quando houver jogos suficientes, a tabela começa a contar a história. Até lá, são palpites."
                };
            posts.Add(SocialPost(8, daySeed, leagueTalk[daySeed % leagueTalk.Length],
                daySeed % 174 + 31, daySeed % 33 + 5, "há 7 h"));

            var fanLines = english
                ? new[]
                {
                    "Renew our best player now, and the person who makes the stadium coffee.",
                    "My prediction stands: I'll believe until the next back-pass near our own box.",
                    "If it goes well, it was the plan. If it goes badly, it was obvious at the press conference.",
                    "I'll take any result as long as they don't make me believe and concede in stoppage time.",
                    "Football is simple: win, play well and don't ruin my weekend. Easy, boss."
                }
                : new[]
                {
                    "Renovem já com o melhor jogador e com a pessoa que trata do café no estádio.",
                    "O meu prognóstico mantém-se: vou acreditar até ao próximo passe para trás junto à área.",
                    "Se correr bem, foi o plano. Se correr mal, era óbvio desde a conferência de imprensa.",
                    "Aguento qualquer resultado, desde que não me façam acreditar e depois marquem aos 90+.",
                    "O futebol é simples: ganhar, jogar bem e não me estragar o fim de semana. Fácil, mister."
                };
            posts.Add(SocialPost(9, daySeed, fanLines[daySeed % fanLines.Length],
                daySeed % 89 + 11, daySeed % 18 + 1, "há 9 h"));

            Club rumourSeller = null;
            Player rumourPlayer = null;
            foreach (var candidateClub in _career.World.Clubs.Values)
            {
                if (candidateClub.Id == club.Id || candidateClub.LeagueId != club.LeagueId)
                    continue;
                var candidatePlayer = BestPlayer(candidateClub.PlayerIds);
                if (candidatePlayer == null)
                    continue;
                rumourSeller = candidateClub;
                rumourPlayer = candidatePlayer;
                break;
            }
            if (rumourPlayer != null && rumourSeller != null)
                posts.Add(SocialPost(10, daySeed,
                    english
                        ? "Transfer Room: " + rumourPlayer.FullName + " is being linked with " + club.ShortName + ". No bid has been confirmed; for now, it is only transfer talk."
                        : "Transfer Room: " + rumourPlayer.FullName + " está a ser associado ao " + club.ShortName + ". Não há proposta confirmada; para já, é conversa de mercado.",
                    daySeed % 128 + 24, daySeed % 23 + 3, "há 7 h", 21));

            Fixture worldResult = null;
            foreach (var fixture in _career.Fixtures)
                if (fixture.Played && !fixture.Walkover && !fixture.Involves(club.Id)
                    && (worldResult == null || fixture.Date > worldResult.Date))
                    worldResult = fixture;
            if (worldResult != null
                && _career.World.Clubs.TryGetValue(worldResult.HomeClubId, out var globalHome)
                && _career.World.Clubs.TryGetValue(worldResult.AwayClubId, out var globalAway))
            {
                var globalPost = english
                    ? globalHome.ShortName + " " + worldResult.HomeGoals + "–" + worldResult.AwayGoals + " " + globalAway.ShortName + ". Three points that could matter when the table gets tight."
                    : globalHome.ShortName + " " + worldResult.HomeGoals + "–" + worldResult.AwayGoals + " " + globalAway.ShortName + ". Três pontos que podem pesar quando a tabela apertar.";
                posts.Add(SocialPost(11, daySeed, globalPost, daySeed % 176 + 39, daySeed % 31 + 6, "há 2 h", 3));
            }

            if (_career.TransferHistory.Count > 0)
            {
                var deal = _career.TransferHistory[0];
                if (_career.World.Players.TryGetValue(deal.PlayerId ?? "", out var moved)
                    && _career.World.Clubs.TryGetValue(deal.FromClubId ?? "", out var oldClub)
                    && _career.World.Clubs.TryGetValue(deal.ToClubId ?? "", out var newClub))
                {
                    var movement = english
                        ? moved.FullName + " has completed a move from " + oldClub.ShortName + " to " + newClub.ShortName + ". The reported fee is " + Euro(deal.Fee) + "."
                        : moved.FullName + " transferiu-se do " + oldClub.ShortName + " para o " + newClub.ShortName + ". O valor reportado é " + Euro(deal.Fee) + ".";
                    posts.Add(SocialPost(12, daySeed, movement, daySeed % 204 + 58, daySeed % 38 + 8, "há 1 h", 5));
                }
            }

            return posts;
        }

        PhoneNote SocialPost(int index, int seed, string text, int likes, int reposts, string age, int authorIndex = -1)
        {
            if (LocalizationManager.Current == GameLanguage.English && age.StartsWith("há ", StringComparison.Ordinal))
                age = age.Substring(3).Replace(" h", "h ago");
            var opinionAccounts = new[] { 0, 1, 9, 10, 13, 19 };
            var account = authorIndex >= 0
                ? authorIndex % SocialAccountNames.Length
                : opinionAccounts[Mathf.Abs(index * 7 + seed) % opinionAccounts.Length];
            var meta = "♡ " + likes + "     ↻ " + reposts + "     ·     " + age;
            var handle = SocialAccountHandles[account];
            var note = new PhoneNote(SocialAccountNames[account], text, "social", null, null, null, meta);
            note.AuthorHandle = handle;
            note.AvatarKey = handle;
            note.Verified = IsPublisherAccount(account);
            return note;
        }

        static bool IsPublisherAccount(int account)
        {
            switch (account)
            {
                case 2: case 3: case 4: case 5: case 6: case 7: case 8:
                case 11: case 12: case 14: case 15: case 16: case 17: case 18:
                case 20: case 21: case 22: case 23:
                    return true;
                default:
                    return false;
            }
        }

        static Color PublisherColor(int account)
        {
            if (account == 4 || account == 20)
                return new Color(0.52f, 0.36f, 0.08f, 1f);
            if (account == 5 || account == 21)
                return new Color(0.08f, 0.25f, 0.4f, 1f);
            return Color.HSVToRGB(Mathf.Repeat(account * 0.173f, 1f), 0.7f, 0.72f);
        }

        static void AddPublisherLogo(Transform parent, int account, int fontSize)
        {
            if (account == 4 || account == 20)
            {
                var ball = BroadcastUi.Solid(parent, "GoldenBall", new Color(0.98f, 0.77f, 0.2f, 1f));
                ball.sprite = BroadcastUi.Circle;
                var rect = ball.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(fontSize * 1.55f, fontSize * 1.55f);
                rect.anchoredPosition = Vector2.zero;
                var star = BroadcastUi.Label(ball.transform, "Star", "★", fontSize, new Color(0.24f, 0.15f, 0.03f), TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Stretch(star.rectTransform);
                return;
            }

            var words = SocialAccountNames[account].Replace("The ", "").Split(new[] { ' ', '\'', '’' }, StringSplitOptions.RemoveEmptyEntries);
            var initials = "";
            for (var i = 0; i < words.Length && initials.Length < 3; i++)
                if (words[i].Length > 0)
                    initials += char.ToUpperInvariant(words[i][0]);
            var mark = BroadcastUi.Label(parent, "BrandInitials", initials, fontSize, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Stretch(mark.rectTransform);
        }

        void AddSocialAuthor(Transform host, PhoneNote note, bool expanded)
        {
            var mask = BroadcastUi.Solid(host, "ProfilePhoto", Color.white);
            mask.sprite = BroadcastUi.Circle;
            mask.raycastTarget = false;
            BroadcastUi.Place(mask.rectTransform, expanded ? 0.04f : 0.035f, expanded ? 0.72f : 0.68f, expanded ? 0.2f : 0.145f, 0.97f);
            var crop = mask.gameObject.AddComponent<Mask>();
            var account = Array.IndexOf(SocialAccountHandles, note.AuthorHandle);
            if (note.Verified && IsPublisherAccount(account))
            {
                mask.color = PublisherColor(account);
                crop.showMaskGraphic = true;
                AddPublisherLogo(mask.transform, account, expanded ? 20 : 15);
            }
            else
            {
                crop.showMaskGraphic = false;
                var faceObject = new GameObject("Face", typeof(RectTransform), typeof(Image));
                faceObject.transform.SetParent(mask.transform, false);
                var faceRect = faceObject.GetComponent<RectTransform>();
                BroadcastUi.Stretch(faceRect);
                var photo = faceObject.GetComponent<Image>();
                photo.sprite = FaceBook.SocialAvatar(note.AvatarKey);
                photo.preserveAspect = true;
                photo.raycastTarget = false;
            }

            var name = BroadcastUi.Label(host, "Author", note.Who, expanded ? 16 : 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 9;
            name.resizeTextMaxSize = expanded ? 16 : 14;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            BroadcastUi.Place(name.rectTransform, expanded ? 0.23f : 0.16f, 0.81f, expanded ? 0.68f : 0.59f, 0.98f);
            var handle = BroadcastUi.Label(host, "Handle", note.AuthorHandle ?? "@adepto", 11, UiTheme.Muted, TextAnchor.MiddleLeft);
            handle.horizontalOverflow = HorizontalWrapMode.Wrap;
            handle.resizeTextForBestFit = true;
            handle.resizeTextMinSize = 8;
            handle.resizeTextMaxSize = 11;
            handle.verticalOverflow = VerticalWrapMode.Truncate;
            BroadcastUi.Place(handle.rectTransform, expanded ? 0.23f : 0.16f, 0.71f, expanded ? 0.82f : 0.75f, 0.8f);
            if (note.Verified && IsPublisherAccount(account))
            {
                var source = BroadcastUi.Panel(host, "PublisherType", new Color(0.04f, 0.39f, 0.48f, 0.92f));
                BroadcastUi.Place(source.rectTransform, expanded ? 0.83f : 0.77f, 0.715f, expanded ? 0.98f : 0.96f, 0.805f);
                var sourceText = BroadcastUi.Label(source.transform, "Label",
                    LocalizationManager.Current == GameLanguage.English ? "MEDIA" : "IMPRENSA", 8, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                sourceText.raycastTarget = false;
                BroadcastUi.Stretch(sourceText.rectTransform);
            }
            if (note.Verified)
            {
                var check = BroadcastUi.Solid(host, "VerifiedBadge", new Color(0.12f, 0.62f, 0.95f, 1f));
                check.sprite = BroadcastUi.Circle;
                var badgeRect = check.rectTransform;
                var badgeX = expanded ? 0.71f : 0.64f;
                badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(badgeX, 0.895f);
                badgeRect.sizeDelta = new Vector2(expanded ? 18f : 16f, expanded ? 18f : 16f);
                badgeRect.anchoredPosition = Vector2.zero;
                var tick = BroadcastUi.Label(check.transform, "Tick", "✓", 11, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Stretch(tick.rectTransform);
            }
        }

        void AddSocialReplies(Transform host, PhoneNote note)
        {
            var heading = BroadcastUi.Label(host, "RepliesTitle", "CONVERSA DA BANCADA", 11, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.06f, 0.375f, 0.94f, 0.42f);
            var comments = SocialComments(note);
            var replySeed = 17;
            var replySource = (note == null ? "" : note.Who + note.Text);
            unchecked
            {
                for (var i = 0; i < replySource.Length; i++)
                    replySeed = replySeed * 31 + replySource[i];
            }
            var opinionAccounts = new[] { 0, 1, 9, 10, 13, 19 };
            var firstReply = (replySeed & 0x7fffffff) % opinionAccounts.Length;
            for (var i = 0; i < comments.Length; i++)
            {
                var account = opinionAccounts[(firstReply + i * 3) % opinionAccounts.Length];
                if (note != null && SocialAccountHandles[account] == note.AuthorHandle)
                    account = opinionAccounts[(Array.IndexOf(opinionAccounts, account) + 1) % opinionAccounts.Length];
                var row = BroadcastUi.Panel(host, "Reply" + i, new Color(1f, 1f, 1f, 0.035f));
                var top = 0.365f - i * 0.105f;
                BroadcastUi.Place(row.rectTransform, 0.04f, top - 0.1f, 0.96f, top);
                var avatar = BroadcastUi.Solid(row.transform, "Avatar", Color.white);
                avatar.sprite = BroadcastUi.Circle;
                avatar.raycastTarget = false;
                BroadcastUi.Place(avatar.rectTransform, 0.015f, 0.14f, 0.105f, 0.86f);
                var mask = avatar.gameObject.AddComponent<Mask>();
                mask.showMaskGraphic = false;
                var faceObject = new GameObject("Face", typeof(RectTransform), typeof(Image));
                faceObject.transform.SetParent(avatar.transform, false);
                var faceRect = faceObject.GetComponent<RectTransform>();
                BroadcastUi.Stretch(faceRect);
                var face = faceObject.GetComponent<Image>();
                face.sprite = FaceBook.SocialAvatar(SocialAccountHandles[account]);
                face.preserveAspect = true;
                face.raycastTarget = false;
                var name = BroadcastUi.Label(row.transform, "Name", SocialAccountNames[account], 11, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 9;
                name.resizeTextMaxSize = 11;
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                name.verticalOverflow = VerticalWrapMode.Truncate;
                BroadcastUi.Place(name.rectTransform, 0.125f, 0.6f, 0.68f, 0.98f);
                var message = BroadcastUi.Label(row.transform, "Text", comments[i], 10, Color.white, TextAnchor.MiddleLeft);
                message.horizontalOverflow = HorizontalWrapMode.Wrap;
                message.verticalOverflow = VerticalWrapMode.Truncate;
                message.resizeTextForBestFit = true;
                message.resizeTextMinSize = 8;
                message.resizeTextMaxSize = 10;
                BroadcastUi.Place(message.rectTransform, 0.125f, 0.04f, 0.96f, 0.58f);
            }
        }

        static string[] SocialComments(PhoneNote note)
        {
            var text = note == null ? "" : note.Text.ToLowerInvariant();
            if (LocalizationManager.Current == GameLanguage.English)
            {
                if (text.Contains("defeat") || text.Contains("lost"))
                    return new[] { "Saw the score and went for a walk before sending a four-minute voice note.", "Our defence went out for milk and left the goal unattended.", "Next match is soon. Good, I don't know how to live without suffering." };
                if (text.Contains("draw") || text.Contains("1–1") || text.Contains("1-1"))
                    return new[] { "One point for the table, three hours of arguments at the pub.", "A draw where everyone is right in the group chat.", "Neither happy nor sad. I've entered tactical analysis mode." };
                if (text.Contains("win") || text.Contains("won"))
                    return new[] { "We won and I've always believed in this project. Always.", "Three points! Even my coffee came without sugar and didn't ruin my day.", "The stand has forgiven the last few matches. Winning does that to memory." };
                if (text.Contains("ballon d'or"))
                    return new[] { "Too early for the Ballon d'Or. First, let's win the warm-up without losing the ball.", "With those numbers, they can pick the pub table tonight.", "Keep this up and even my nan will learn to pronounce the name." };
                if (text.Contains("scored") || text.Contains("conceded"))
                    return new[] { "Scoring is good. Not conceding would also be a lovely idea.", "Our goal difference is doing overtime.", "The attack deserves dinner out; the defence needs a short meeting." };
                if (text.Contains("goals") || text.Contains("assists"))
                    return new[] { "The numbers are there. Now the pub pundit needs to admit they were wrong.", "Goals and assists? Turns out training does something after all.", "I said they were good before the highlights, just so we're clear." };
                if (text.Contains("transfer") || text.Contains("offer"))
                    return new[] { "This rumour has already lasted three pre-seasons.", "If we sign a striker, I'll pay for their Uber!", "My cousin has football boots. Does that count?" };
                if (text.Contains("against ") || text.Contains("match scheduled") || text.Contains("next match"))
                    return new[] { "Shirt's ready. Now the team just has to not ruin my afternoon.", "Simple plan against them: score first, then pretend to stay calm.", "I want courage on the pitch and fewer passes to their goalkeeper." };
                if (text.Contains("table") || text.Contains("place") || text.Contains("points") || text.Contains("lead"))
                    return new[] { "The table doesn't lie, but my uncle has an explanation for every column.", "Position noted. Now nobody let the manager read the comments.", "Lots of season left; I've done the maths fourteen times already." };
                if (text.Contains("formation") || text.Contains("midfield") || text.Contains("press") || text.Contains("tactic") || text.Contains("play out") || text.Contains("direct"))
                    return new[] { "A high press is lovely until your lungs request a sub on 60 minutes.", "My sofa 4-3-3 is still unbeaten, just saying.", "A tactical debate with fewer pub shouts. Almost." };
                if (text.Contains("press conference") || text.Contains("plan") || text.Contains("excuses") || text.Contains("process"))
                    return new[] { "Everyone has a plan at the press conference. On Sunday, just make the pass.", "The manager said ‘process’ and the whole pub started coughing.", "If it works, it was the plan; if not, blame the grass." };
                if (text.Contains("renew") || text.Contains("contract"))
                    return new[] { "Renew now, before a club with more zeroes on the cheque turns up.", "The renewal is urgent. Would my mind change after one bad pass? Never.", "If they stay, I'll buy them a coffee. No sugar; the budget's tight." };
                if (text.Contains("performance") || text.Contains("missed") || text.Contains("mistake") || text.Contains("poor") || text.Contains("lost the ball"))
                    return new[] { "The rating was so low the report card arrived folded.", "Didn't get one thing right today. The pub wouldn't ask for another pass either.", "Give them a match to respond… or a GPS to find the goal." };
                if (text.Contains("quality") || text.Contains("big game") || text.Contains("face of this project"))
                    return new[] { "The quality is there. Show it when the stadium is boiling, too.", "Already the face of the project; now show up in the big games.", "The pub has already given them the armband, the award and a reserved table." };
                return new[] { "I agree with this until the next back-pass.", "Read the post and formed a strong opinion without watching the match.", "The pub has three better plans and none of them involve running." };
            }
            if (text.Contains("derrota") || text.Contains("perdemos"))
                return new[] { "Hoje vi o resultado e fui dar uma volta para não mandar um áudio de quatro minutos.", "A defesa foi buscar pão e deixou a baliza sozinha.", "Próxima jornada é já. Ainda bem, que não sei viver sem sofrer." };
            if (text.Contains("empate") || text.Contains("1–1") || text.Contains("1-1"))
                return new[] { "Um ponto para a tabela, três para a discussão no café.", "Empate daqueles em que todos têm razão no WhatsApp.", "Nem feliz nem triste: fiquei em modo análise tática." };
            if (text.Contains("vitória"))
                return new[] { "Ganhámos e eu já estou a dizer que sempre acreditei neste projeto.", "Três pontos! Hoje até o meu café saiu sem açúcar e não me estragou o dia.", "A bancada já perdoou os últimos jogos. A memória é curta quando se ganha." };
            if (text.Contains("bola de ouro"))
                return new[] { "Bola de Ouro ainda é cedo; para já que leve o prémio de não perder a bola no aquecimento.", "Com esses números já pode escolher a mesa no café.", "Se continuar assim, até a minha avó aprende a pronunciar o nome." };
            if (text.Contains("golos marcados") || text.Contains("golos sofridos"))
                return new[] { "Marcar golos é bom. Evitar sofrer também era uma ideia bonita.", "A diferença de golos está a fazer horas extra.", "O ataque merece jantar fora; a defesa, uma reunião curta." };
            if ((text.Contains("leva ") && text.Contains("golos")) || text.Contains("assistências"))
                return new[] { "Os números estão aí. Agora falta o comentador do café admitir que estava enganado.", "Golos e assistências? Parece que afinal treinar também dá resultado.", "Eu dizia que era bom antes de aparecer no resumo, atenção." };
            if (text.Contains("transferência") || text.Contains("proposta"))
                return new[] { "Este rumor já fez três pré-épocas.", "Se vier ponta, pago-lhe o Uber!", "O meu primo tem chuteiras. Serve?" };
            if (text.Contains("contra ") || text.Contains("jogo marcado") || text.Contains("próxima jornada"))
                return new[] { "Já preparei a camisola. Só falta a equipa não me estragar a tarde.", "Contra esse adversário, o meu plano é simples: marcar primeiro e fingir calma.", "Quero coragem no relvado e menos passes para o guarda-redes deles." };
            if (text.Contains("tabela") || text.Contains("lugar") || text.Contains("pontos em ") || text.Contains("lidera") || text.Contains("corrida pelo topo"))
                return new[] { "A tabela não mente, mas o meu tio já tem uma explicação para cada coluna.", "Lugar registado. Agora ninguém deixe o treinador ler os comentários.", "Ainda falta muito calendário; eu já fiz as contas catorze vezes." };
            if (text.Contains("formação") || text.Contains("meio-campo") || text.Contains("pressão") || text.Contains("tático") || text.Contains("sair a jogar") || text.Contains("futebol direto"))
                return new[] { "Pressão alta é linda até o pulmão pedir substituição aos 60.", "O meu 4-3-3 do sofá continua invicto, atenção.", "Finalmente discussão tática com menos gritos que no café. Quase." };
            if (text.Contains("conferência") || text.Contains("plano") || text.Contains("desculpas") || text.Contains("processo"))
                return new[] { "Na conferência toda a gente tem plano. No domingo quero é ver o passe sair.", "O treinador disse ‘processo’ e o café inteiro começou a tossir.", "Se correr bem foi o plano; se correr mal foi a relva, aposto." };
            if (text.Contains("renovem") || text.Contains("renovar") || text.Contains("contrato"))
                return new[] { "Renovem já, antes que apareça um clube com mais zeros no cheque.", "A renovação é urgente. A minha opinião muda quando ele falha um passe? Nunca.", "Se ficar, pago-lhe um café. Sem açúcar, que o orçamento não estica." };
            if (text.Contains("desempenho") || text.Contains("falhou") || text.Contains("falhas") || text.Contains("fraco") || text.Contains("manco") || text.Contains("não marcou") || text.Contains("nao marcou") || text.Contains("perdeu a bola"))
                return new[] { "A nota foi tão baixa que o boletim veio dobrado.", "Hoje não acertou uma. Nem no café pediam outro passe.", "Dêem-lhe um jogo para responder… ou um GPS para encontrar a baliza." };
            if (text.Contains("qualidade") || text.Contains("jogos grandes") || text.Contains("cara deste projeto"))
                return new[] { "Qualidade tem. Agora quero ver isso quando o estádio estiver a ferver.", "Já é a cara do projeto; falta só a cara aparecer nos jogos grandes.", "No café já lhe deram a braçadeira, o prémio e uma mesa cativa." };
            return new[] { "Concordo com isto até ao próximo passe para trás.", "Li o post e já tenho uma opinião forte sem ver o jogo.", "No café já há três planos melhores e nenhum inclui correr." };
        }

        Player BestSeasonPlayer(string leagueId, string kind)
        {
            Player best = null;
            var bestScore = float.MinValue;
            foreach (var line in _career.Season)
            {
                if (!string.IsNullOrEmpty(leagueId) && line.CompetitionId != leagueId)
                    continue;
                if (kind == "golos" && line.Goals <= 0)
                    continue;
                if (!_career.World.Players.TryGetValue(line.PlayerId, out var player))
                    continue;
                var score = kind == "golos" ? line.Goals : line.Rated > 0
                    ? line.RatingSum / line.Rated * 2f + line.Goals * 1.4f + line.Assists * 0.9f
                    : line.Goals * 1.4f + line.Assists * 0.9f;
                if (score <= bestScore)
                    continue;
                best = player;
                bestScore = score;
            }
            return best;
        }

        SeasonLine SeasonLineFor(string playerId, string leagueId)
        {
            SeasonLine best = null;
            foreach (var line in _career.Season)
            {
                if (line.PlayerId != playerId || (!string.IsNullOrEmpty(leagueId) && line.CompetitionId != leagueId))
                    continue;
                if (best == null || line.Goals + line.Assists > best.Goals + best.Assists)
                    best = line;
            }
            return best;
        }

        Player BestPlayer(IEnumerable<string> playerIds)
        {
            Player best = null;
            foreach (var id in playerIds)
            {
                if (!_career.World.Players.TryGetValue(id, out var player))
                    continue;
                if (best == null || PlayerAttributes.Overall(player) > PlayerAttributes.Overall(best))
                    best = player;
            }
            return best;
        }

        static void AddPlayerNote(List<PhoneNote> list, HashSet<string> seen, Player player, string text)
        {
            if (player == null || seen.Contains(player.Id) || string.IsNullOrEmpty(text))
                return;
            seen.Add(player.Id);
            list.Add(new PhoneNote(player.FullName, text, "jogador", player.Id, player.ClubId, PlayerReplyGroup(player, text)));
        }

        static int ListingShock(Player player)
        {
            switch (player.Personality)
            {
                case PersonalityTrait.Temperamental: return 8;
                case PersonalityTrait.Ambitious: return 2;
                case PersonalityTrait.Professional: return 3;
                case PersonalityTrait.Insecure: return 7;
                case PersonalityTrait.Leader: return 5;
                default: return 4;
            }
        }

        static string ListingReaction(Player player, bool english)
        {
            var name = player.FullName.Split(' ')[0];
            switch (player.Personality)
            {
                case PersonalityTrait.Ambitious:
                    return english
                        ? name + ": if I'm for sale, I want a club that plays me. Don't leave me hanging."
                        : name + ": se estou à venda, quero um clube onde jogue. Não me deixem aqui à espera.";
                case PersonalityTrait.Temperamental:
                    return english
                        ? name + ": you put me on the list without a word. The dressing room heard it before I did."
                        : name + ": meteste-me na lista sem me dizeres nada. O balneário soube antes de mim.";
                case PersonalityTrait.Professional:
                    return english
                        ? name + ": I'll keep working. Just tell me if I should still prepare as a starter."
                        : name + ": continuo a trabalhar. Diz-me só se ainda me preparo como titular.";
                case PersonalityTrait.Insecure:
                    return english
                        ? name + ": so I'm the one you want to sell. I don't know what I did wrong."
                        : name + ": então sou eu que queres vender. Não sei o que fiz de errado.";
                case PersonalityTrait.Leader:
                    return english
                        ? name + ": I'll keep the group calm, but they will ask why their captain is listed."
                        : name + ": mantenho o grupo calmo, mas vão perguntar porque é que o capitão está à venda.";
                default:
                    return english
                        ? name + ": I saw my name on the transfer list. I needed to hear it from you."
                        : name + ": vi o meu nome na lista de transferências. Queria ter ouvido isto de ti.";
            }
        }

        static string PlayerReplyGroup(Player player, string text)
        {
            if (player.Injury != null && player.Injury.IsInjured)
                return "injury";
            if (!string.IsNullOrEmpty(text) && text.IndexOf("pernas", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "fatigue";
            if (!string.IsNullOrEmpty(text) && (text.IndexOf("lugar", System.StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("onze", System.StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("agente", System.StringComparison.OrdinalIgnoreCase) >= 0))
                return "selection";
            if (player.Morale < 48)
                return "morale";
            if (player.Personality == PersonalityTrait.Leader)
                return "leadership";
            if (player.Personality == PersonalityTrait.Professional)
                return "ready";
            return "role";
        }

        static string PlayerVoice(Player player, Club club)
        {
            var first = player.FullName.Split(' ')[0];
            var benched = club.ManualLineup != null && club.ManualLineup.Count >= 11 && !club.ManualLineup.Contains(player.Id);
            if (LocalizationManager.Current == GameLanguage.English)
            {
                if (player.Injury != null && player.Injury.IsInjured)
                    return first + ": I'm out for now. I'd like to know the plan for when I'm back.";
                if (player.Morale < 40 && player.Personality == PersonalityTrait.Temperamental)
                    return "I don't understand my role. We need to talk before this gets worse.";
                if (player.Morale < 40 && player.Personality == PersonalityTrait.Insecure)
                    return "I feel like you don't count on me anymore. Tell me if I'm wrong.";
                if (player.Morale < 48)
                    return "The squad can see how I'm feeling. I need an honest conversation.";
                if (benched && (player.Personality == PersonalityTrait.Ambitious || player.Personality == PersonalityTrait.Outspoken))
                    return "I want my place in the team. If I'm left out again, my agent will ask why.";
                if (player.Personality == PersonalityTrait.Professional)
                    return "I'm ready. Tell me what you need from me in the next match.";
                if (player.Personality == PersonalityTrait.Leader)
                    return "The squad listens to you. Give me a clear plan and I'll handle the dressing room.";
                return "Boss, when you have a moment, can we talk about my role?";
            }
            if (player.Injury != null && player.Injury.IsInjured)
                return first + ": estou de fora. Quero saber o plano para quando voltar.";
            if (player.Morale < 40 && player.Personality == PersonalityTrait.Temperamental)
                return "Não estou a perceber o meu papel. Ou falamos, ou isto vai azedar.";
            if (player.Morale < 40 && player.Personality == PersonalityTrait.Insecure)
                return "Sinto que já não contas comigo. Diz-me se estou errado.";
            if (player.Morale < 48)
                return "O balneário nota o meu estado. Preciso de uma conversa clara.";
            if (benched && (player.Personality == PersonalityTrait.Ambitious || player.Personality == PersonalityTrait.Outspoken))
                return "Quero o lugar. Se o onze não me incluir, o meu agente vai perguntar porquê.";
            if (player.Personality == PersonalityTrait.Professional)
                return "Estou pronto. Diz-me o que queres de mim no próximo jogo.";
            if (player.Personality == PersonalityTrait.Leader)
                return "O grupo ouve-te. Se o plano estiver claro, eu trato do resto do balneário.";
            return "Mister, quando puderes, troca duas palavras comigo sobre o meu papel.";
        }

        sealed class PhoneNote
        {
            public string Who;
            public string Text;
            public string Channel;
            public string PlayerId;
            public string ClubId;
            public string Key;
            public string ReplyGroup;
            public string Meta;
            public string AuthorHandle;
            public string AvatarKey;
            public bool Verified;
            public bool Important;
            public int OfferAmount;
            public int OfferWeeklyWage;
            public int OfferYears;
            public int OfferRound;

            public PhoneNote(string who, string text, string channel, string playerId, string clubId, string replyGroup = null, string meta = null)
            {
                Who = who;
                Text = text;
                Channel = channel;
                PlayerId = playerId;
                ClubId = clubId;
                ReplyGroup = replyGroup ?? channel;
                Meta = meta;
                Key = channel + "|" + (playerId ?? "") + "|" + (clubId ?? "") + "|" + who + "|" + ReplyGroup + "|" + (meta ?? "");
            }
        }

        sealed class StaffOfferCandidate
        {
            public StaffMember Member;
            public int AskingWage;
            public int ContractYears;
            public int Week;
        }

        void BuildTabs(Transform root)
        {
            var bar = BroadcastUi.Zone(root, "Tabs", 0.03f, 0.825f, 0.97f, 0.89f);
            _tabBar = bar;
            var labels = new[] { "INÍCIO", "JORNAIS", "ACADEMIA", "EQUIPA", "TREINO", "TRANSFERÊNCIAS", "ESCRITÓRIO", "TABELA" };
            _tabs = new Button[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                var x0 = 0.004f + i * 0.124f;
                _tabs[i] = Tab(bar, "Tab" + i, labels[i], x0, x0 + 0.12f, i);
            }
        }

        Button Tab(Transform parent, string name, string label, float x0, float x1, int index)
        {
            var button = BroadcastUi.Button(parent, name, label, () => Show(index), new Color(1f, 1f, 1f, 0.06f), UiTheme.Muted, 16);
            BroadcastUi.Place(button.GetComponent<RectTransform>(), x0, 0.08f, x1, 0.92f);
            return button;
        }

        void BuildPages(Transform root)
        {
            var host = BroadcastUi.Zone(root, "Pages", 0.03f, 0.075f, 0.97f, 0.815f);
            host.gameObject.AddComponent<RectMask2D>();
            _pages = new GameObject[8];
            _pages[0] = BuildHome(host);
            _pages[1] = BuildNews(host);
            _pages[2] = BuildAcademy(host);
            _pages[3] = BuildTeam(host);
            _pages[4] = BuildTraining(host);
            _pages[5] = BuildTransfers(host);
            _pages[6] = BuildOffice(host);
            _pages[7] = BuildStandings(host);
        }

        GameObject BuildHome(Transform host)
        {
            var page = BroadcastUi.Panel(host, "Home", new Color(0f, 0f, 0f, 0f));
            BroadcastUi.Stretch(page.rectTransform);
            return page.gameObject;
        }

        GameObject BuildNews(Transform host)
        {
            var page = BroadcastUi.Panel(host, "News", new Color(0.08f, 0.03f, 0.16f, 0.5f));
            BroadcastUi.Stretch(page.rectTransform);
            var title = BroadcastUi.Label(page.transform, "Title", "JORNAIS DO DIA", 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.03f, 0.9f, 0.7f, 0.98f);
            var scroll = BroadcastUi.Scroll(page.transform, "Scroll", out _newsContent);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.02f, 0.03f, 0.98f, 0.88f);
            return page.gameObject;
        }

        void BuildCareerRecord(Transform host)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var title = BroadcastUi.Label(host, "Title", english ? "CAREER RECORD" : "REGISTO DA CARREIRA", 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.03f, 0.9f, 0.7f, 0.98f);
            var scroll = BroadcastUi.Scroll(host, "RecordScroll", out _careerRecordContent);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.02f, 0.03f, 0.98f, 0.88f);
            FillCareerRecord();
        }

        GameObject BuildBoard(Transform host)
        {
            var page = BroadcastUi.Panel(host, "Board", new Color(0.08f, 0.03f, 0.16f, 0.5f));
            BroadcastUi.Stretch(page.rectTransform);
            var english = LocalizationManager.Current == GameLanguage.English;
            var title = BroadcastUi.Label(page.transform, "Title", english ? "BOARD" : "QUADRO", 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.03f, 0.9f, 0.6f, 0.98f);
            var hint = BroadcastUi.Label(page.transform, "Hint", english ? "Write a personal note about what happened. This is not the news feed." : "Escreve uma nota pessoal sobre o que aconteceu. Isto não é o jornal.", 14, UiTheme.Muted, TextAnchor.MiddleRight);
            BroadcastUi.Place(hint.rectTransform, 0.35f, 0.9f, 0.97f, 0.98f);
            _noteField = UiFactory.Input(page.transform, english ? "What stayed with you from this moment?" : "O que ficou contigo deste momento?");
            _noteField.characterLimit = 180;
            BroadcastUi.Place(_noteField.GetComponent<RectTransform>(), 0.03f, 0.78f, 0.78f, 0.89f);
            var save = BroadcastUi.Button(page.transform, "SaveNote", english ? "SAVE NOTE" : "GUARDAR NOTA", SaveNote, UiTheme.Accent, UiTheme.ButtonText, 16);
            BroadcastUi.Place(save.GetComponent<RectTransform>(), 0.8f, 0.78f, 0.97f, 0.89f);
            _noteError = BroadcastUi.Label(page.transform, "Error", "", 14, UiTheme.Danger, TextAnchor.MiddleLeft);
            BroadcastUi.Place(_noteError.rectTransform, 0.03f, 0.72f, 0.97f, 0.78f);
            var scroll = BroadcastUi.Scroll(page.transform, "Scroll", out _boardContent);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.02f, 0.03f, 0.98f, 0.72f);
            FillBoard();
            return page.gameObject;
        }

        GameObject BuildAcademy(Transform host)
        {
            var page = BroadcastUi.Panel(host, "Academy", new Color(0.08f, 0.03f, 0.16f, 0.5f));
            BroadcastUi.Stretch(page.rectTransform);
            BuildAcademyContents(page.transform);
            return page.gameObject;
        }

        void BuildAcademyContents(Transform page)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var title = BroadcastUi.Label(page, "Title", english ? "ACADEMY" : "ACADEMIA", 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.03f, 0.92f, 0.62f, 0.99f);
            var intro = BroadcastUi.Label(page, "Intro", english
                ? "Youth prospects · players can be promoted from age 16"
                : "Jovens promessas · promoção possível a partir dos 16 anos", 14, UiTheme.Muted, TextAnchor.MiddleRight);
            BroadcastUi.Place(intro.rectTransform, 0.35f, 0.92f, 0.97f, 0.99f);
            _academyAgeTabs = new Button[AcademyFactory.AgeGroups.Length];
            for (var i = 0; i < _academyAgeTabs.Length; i++)
            {
                var index = i;
                _academyAgeTabs[i] = BroadcastUi.Button(page, "AcademyAge" + i, AcademyFactory.AgeGroups[i], () =>
                {
                    _academyAgeIndex = index;
                    _academySelectedBenchPlayerId = "";
                    FillAcademy();
                }, new Color(1f, 1f, 1f, 0.08f), Color.white, 13);
                BroadcastUi.Place(_academyAgeTabs[i].GetComponent<RectTransform>(), 0.02f + i * 0.16f, 0.84f, 0.17f + i * 0.16f, 0.9f);
            }
            var views = english ? new[] { "TACTICS", "SQUAD", "FIXTURES" } : new[] { "TÁTICAS", "PLANTEL", "JOGOS" };
            _academyViewTabs = new Button[views.Length];
            for (var i = 0; i < _academyViewTabs.Length; i++)
            {
                var index = i;
                _academyViewTabs[i] = BroadcastUi.Button(page, "AcademyView" + i, views[i], () =>
                {
                    _academyViewIndex = index;
                    FillAcademy();
                }, new Color(1f, 1f, 1f, 0.08f), Color.white, 13);
                var x0 = 0.55f + i * 0.145f;
                BroadcastUi.Place(_academyViewTabs[i].GetComponent<RectTransform>(), x0, 0.84f, x0 + 0.135f, 0.9f);
            }
            var scroll = BroadcastUi.Scroll(page, "AcademyScroll", out _academyContent);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.02f, 0.025f, 0.98f, 0.82f);
            FillAcademy();
            _academyLayoutPassesRemaining = 2;
        }

        GameObject BuildTraining(Transform host)
        {
            var page = BroadcastUi.Panel(host, "Training", new Color(0.08f, 0.03f, 0.16f, 0.5f));
            BroadcastUi.Stretch(page.rectTransform);
            var english = LocalizationManager.Current == GameLanguage.English;
            var title = BroadcastUi.Label(page.transform, "Title", english ? "TRAINING" : "TREINO", 18, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.025f, 0.92f, 0.4f, 0.99f);
            _individualTraining = true;
            _trainingSearch = UiFactory.Input(page.transform, english ? "Search players" : "Procurar jogadores");
            _trainingSearch.name = "TrainingSearch";
            BroadcastUi.Place(_trainingSearch.GetComponent<RectTransform>(), 0.025f, 0.84f, 0.72f, 0.91f);
            _trainingSearch.onValueChanged.AddListener(_ => FillTraining());
            var scroll = BroadcastUi.Scroll(page.transform, "TrainingScroll", out var content);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.025f, 0.025f, 0.725f, 0.82f);
            _trainingContent = content;
            _trainingFocusContent = BroadcastUi.Zone(page.transform, "TrainingFocusOverview", 0.745f, 0.515f, 0.975f, 0.82f);
            _trainingDistributionContent = BroadcastUi.Zone(page.transform, "TrainingFocusDistribution", 0.745f, 0.025f, 0.975f, 0.495f);
            RefreshTrainingModeTabs();
            FillTraining();
            return page.gameObject;
        }

        void FillTraining()
        {
            if (_trainingContent == null || _career == null)
                return;
            Clear(_trainingContent);
            var english = LocalizationManager.Current == GameLanguage.English;
            var query = _trainingSearch == null ? "" : _trainingSearch.text.Trim();
            var focusNames = english
                ? new[] { "Finishing", "Passing", "Pace", "Defending", "Strength", "Positioning", "Fitness", "Rest" }
                : new[] { "Finalização", "Passe", "Velocidade", "Defesa", "Força", "Posicionamento", "Condição", "Descanso" };
            var focusKeys = new[] { "finishing", "passing", "pace", "defending", "strength", "positioning", "fitness", "rest" };
            var visible = new List<Player>();
            foreach (var player in Squad(_career.Club))
                if (string.IsNullOrEmpty(query) || player.FullName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    visible.Add(player);
            var columns = BroadcastUi.Panel(_trainingContent, "TrainingColumns", new Color(0.02f, 0.045f, 0.07f, 0.98f), true);
            BroadcastUi.Preferred(columns, 34);
            var columnNames = english
                ? new[] { "PLAYER", "FOCUS", "INTENSITY", "PROGRESS", "MARK", "SESSIONS", "TREND" }
                : new[] { "JOGADOR", "FOCO", "INTENSIDADE", "PROGRESSO", "NOTA", "SESSÕES", "TENDÊNCIA" };
            var columnBounds = new[] { 0.06f, 0.22f, 0.4f, 0.5f, 0.66f, 0.79f, 0.93f, 0.995f };
            for (var column = 0; column < columnNames.Length; column++)
            {
                var label = BroadcastUi.Label(columns.transform, "Column" + column, columnNames[column], 9, UiTheme.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(label.rectTransform, columnBounds[column], 0.08f, columnBounds[column + 1], 0.92f);
            }

            for (var i = 0; i < visible.Count; i++)
            {
                var player = visible[i];
                var row = BroadcastUi.Panel(_trainingContent, "TrainingPlayer" + i, new Color(0.035f, 0.075f, 0.1f, 0.94f), true);
                BroadcastUi.Preferred(row, 58);
                var portraitGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                portraitGo.transform.SetParent(row.transform, false);
                BroadcastUi.Place(portraitGo.GetComponent<RectTransform>(), 0.006f, 0.12f, 0.05f, 0.88f);
                var portrait = portraitGo.GetComponent<Image>();
                portrait.sprite = FaceBook.For(player);
                portrait.preserveAspect = true;
                portrait.raycastTarget = false;
                var positionLabel = BroadcastUi.Label(row.transform, "Position", ProfilePosition(player.Position, english), 11, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(positionLabel.rectTransform, columnBounds[0], 0.16f, columnBounds[0] + 0.045f, 0.84f);
                var name = BroadcastUi.Label(row.transform, "Name", player.FullName, 11, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(name.rectTransform, columnBounds[0] + 0.048f, 0.16f, columnBounds[1] - 0.008f, 0.84f);
                var playerKeys = FocusKeys(player.Position);
                var current = Array.IndexOf(playerKeys, player.TrainingFocus ?? "");
                if (current < 0)
                {
                    current = 0;
                    player.TrainingFocus = playerKeys[0];
                }
                var captured = player;
                var focus = BroadcastUi.Button(row.transform, "Focus", FocusName(player.TrainingFocus, english) + "  ▼", () => OpenFocusMenu(captured), new Color(1f, 1f, 1f, 0.08f), Color.white, 11);
                BroadcastUi.Place(focus.GetComponent<RectTransform>(), columnBounds[1], 0.12f, columnBounds[2] - 0.008f, 0.88f);
                var focusText = focus.GetComponentInChildren<Text>();
                focusText.fontSize = 10;
                focusText.resizeTextForBestFit = false;
                var isRest = player.TrainingFocus == "rest";
                var isInjured = player.Injury != null && player.Injury.IsInjured;
                var intensityNames = english ? new[] { "Low", "Normal", "High" } : new[] { "Baixa", "Normal", "Alta" };
                var intensityIndex = Mathf.Clamp(player.TrainingIntensity, 0, 2);
                var intensityTone = intensityIndex == 2 ? new Color(0.72f, 0.18f, 0.16f, 1f) : intensityIndex == 0 ? new Color(0.12f, 0.42f, 0.28f, 1f) : new Color(1f, 1f, 1f, 0.08f);
                var intensity = BroadcastUi.Button(row.transform, "Intensity", intensityNames[intensityIndex], () =>
                {
                    var next = (captured.TrainingIntensity + 1) % 3;
                    captured.TrainingIntensity = next;
                    CareerStore.Save(_career);
                    FillTraining();
                }, intensityTone, Color.white, 11);
                BroadcastUi.Place(intensity.GetComponent<RectTransform>(), columnBounds[2], 0.12f, columnBounds[3] - 0.008f, 0.88f);
                var progressValue = Mathf.Clamp(player.TrainingProgress, 0, 99);
                var progress = progressValue / 100f;
                var bar = BroadcastUi.Zone(row.transform, "Progress", columnBounds[3], 0.43f, columnBounds[4] - 0.045f, 0.62f);
                var fill = BroadcastUi.Panel(bar, "Fill", UiTheme.Accent);
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.02f, progress), 1f);
                fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
                var value = BroadcastUi.Label(row.transform, "ProgressValue", progressValue + "%", 10, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(value.rectTransform, columnBounds[4] - 0.045f, 0.12f, columnBounds[4], 0.88f);
                var markLabel = isInjured ? (english ? "INJURED" : "LESIONADO") : WeekMark(player).ToString();
                var projection = BroadcastUi.Label(row.transform, "Projected", markLabel, 14, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(projection.rectTransform, columnBounds[4], 0.12f, columnBounds[5] - 0.006f, 0.88f);
                var sessions = isRest || isInjured ? "—" : player.TrainingStreak.ToString();
                var matchExperience = BroadcastUi.Label(row.transform, "MatchExperience", sessions, 12, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(matchExperience.rectTransform, columnBounds[5], 0.12f, columnBounds[6] - 0.006f, 0.88f);
                var rising = !isRest && !isInjured && player.TrainingStreak >= 3 && player.Fatigue < 70;
                var trend = BroadcastUi.Label(row.transform, "Trend", isRest || isInjured ? "—" : rising ? "↗" : player.Fatigue > 75 ? "↘" : "·", 15, rising ? UiTheme.Fitness : player.Fatigue > 75 ? UiTheme.Danger : UiTheme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(trend.rectTransform, columnBounds[6], 0.12f, columnBounds[7], 0.88f);
            }
            if (visible.Count == 0)
            {
                var empty = BroadcastUi.Label(_trainingContent, "NoTrainingPlayers", english ? "No players found." : "Não foram encontrados jogadores.", 14, UiTheme.Muted, TextAnchor.MiddleCenter);
                BroadcastUi.Preferred(empty, 48);
            }
            FillTrainingSummary(visible, focusNames, focusKeys);
        }

        void FillTrainingSummary(List<Player> players, string[] focusNames, string[] focusKeys)
        {
            if (_trainingFocusContent == null || _trainingDistributionContent == null)
                return;
            var english = LocalizationManager.Current == GameLanguage.English;
            Clear(_trainingFocusContent);
            var overview = BroadcastUi.Panel(_trainingFocusContent, "Overview", new Color(0.035f, 0.075f, 0.1f, 0.96f), false);
            BroadcastUi.Stretch(overview.rectTransform);
            var title = BroadcastUi.Label(overview.transform, "Title", english ? "TRAINING FOCUS" : "FOCO DE TREINO", 12, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.045f, 0.82f, 0.96f, 0.98f);
            var ringObject = new GameObject("PlayerCountRing", typeof(RectTransform), typeof(Image));
            ringObject.transform.SetParent(overview.transform, false);
            var ringRect = ringObject.GetComponent<RectTransform>();
            ringRect.anchorMin = ringRect.anchorMax = new Vector2(0.32f, 0.48f);
            ringRect.sizeDelta = new Vector2(92f, 92f);
            var ring = ringObject.GetComponent<Image>();
            ring.sprite = BroadcastUi.Ring;
            ring.preserveAspect = true;
            ring.color = UiTheme.Accent;
            ring.raycastTarget = false;
            var count = BroadcastUi.Label(overview.transform, "Players", players.Count.ToString(), 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(count.rectTransform, 0.18f, 0.39f, 0.46f, 0.58f);
            var playerCaption = BroadcastUi.Label(overview.transform, "PlayersCaption", english ? "PLAYERS" : "JOGADORES", 8, UiTheme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(playerCaption.rectTransform, 0.14f, 0.25f, 0.5f, 0.39f);
            var attributeCount = 0;
            var recoveryCount = 0;
            foreach (var player in players)
                if (player.TrainingFocus == "rest") recoveryCount++;
                else attributeCount++;
            var breakdown = BroadcastUi.Label(overview.transform, "OverviewBreakdown",
                (english ? "Attribute" : "Atributo") + "   " + attributeCount + "\n" + (english ? "Recovery" : "Recuperação") + "   " + recoveryCount
                + "\n" + (english ? "High intensity is faster and riskier." : "Intensidade alta acelera e aumenta o risco."),
                11, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(breakdown.rectTransform, 0.54f, 0.27f, 0.96f, 0.65f);

            Clear(_trainingDistributionContent);
            var distribution = BroadcastUi.Panel(_trainingDistributionContent, "Distribution", new Color(0.035f, 0.075f, 0.1f, 0.96f), false);
            BroadcastUi.Stretch(distribution.rectTransform);
            var distributionTitle = BroadcastUi.Label(distribution.transform, "Title", english ? "DISTRIBUTION BY FOCUS" : "DISTRIBUIÇÃO POR FOCO", 11, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(distributionTitle.rectTransform, 0.045f, 0.88f, 0.96f, 0.99f);
            var counts = new Dictionary<string, int>();
            var maxCount = 1;
            foreach (var player in players)
            {
                var key = string.IsNullOrEmpty(player.TrainingFocus) ? "rest" : player.TrainingFocus;
                if (!counts.ContainsKey(key))
                    counts[key] = 0;
                counts[key]++;
                maxCount = Mathf.Max(maxCount, counts[key]);
            }
            var shown = 0;
            foreach (var pair in counts)
            {
                if (shown >= 8)
                    break;
                var top = 0.85f - shown * 0.105f;
                var row = BroadcastUi.Zone(distribution.transform, "Focus" + shown, 0.035f, top - 0.09f, 0.965f, top);
                var label = BroadcastUi.Label(row, "Name", FocusName(pair.Key, english) + "   ·   " + FocusMix(pair.Key, english), 9, Color.white, TextAnchor.MiddleLeft);
                BroadcastUi.Place(label.rectTransform, 0f, 0f, 0.78f, 1f);
                var value = BroadcastUi.Label(row, "Count", pair.Value.ToString(), 9, UiTheme.Muted, TextAnchor.MiddleRight, FontStyle.Bold);
                BroadcastUi.Place(value.rectTransform, 0.78f, 0f, 0.9f, 1f);
                var track = BroadcastUi.Solid(row, "Track", new Color(1f, 1f, 1f, 0.12f));
                BroadcastUi.Place(track.rectTransform, 0.92f, 0.34f, 1f, 0.66f);
                var bar = BroadcastUi.Solid(track.transform, "Fill", UiTheme.Accent);
                BroadcastUi.Stretch(bar.rectTransform);
                bar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(pair.Value / (float)maxCount), 1f);
                shown++;
            }
        }

        void RefreshTrainingModeTabs()
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var placeholder = _trainingSearch == null ? null : _trainingSearch.placeholder as Text;
            if (placeholder != null) placeholder.text = english ? "Search players" : "Procurar jogadores";
            if (_trainingTeamTab != null)
            {
                _trainingTeamTab.GetComponentInChildren<Text>().text = english ? "TEAM" : "EQUIPA";
                _trainingTeamTab.GetComponent<Image>().color = _individualTraining ? new Color(1f, 1f, 1f, 0.08f) : UiTheme.Accent;
                _trainingTeamTab.GetComponentInChildren<Text>().color = _individualTraining ? Color.white : UiTheme.ButtonText;
            }
            if (_trainingIndividualTab != null)
            {
                _trainingIndividualTab.GetComponentInChildren<Text>().text = english ? "INDIVIDUAL" : "INDIVIDUAL";
                _trainingIndividualTab.GetComponent<Image>().color = _individualTraining ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f);
                _trainingIndividualTab.GetComponentInChildren<Text>().color = _individualTraining ? UiTheme.ButtonText : Color.white;
            }
        }

        GameObject BuildOffice(Transform host)
        {
            var page = BroadcastUi.Panel(host, "Office", new Color(0.08f, 0.03f, 0.16f, 0.5f));
            BroadcastUi.Stretch(page.rectTransform);
            BuildOfficeContents(page.transform);
            return page.gameObject;
        }

        void BuildOfficeContents(Transform page)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var title = BroadcastUi.Label(page, "OfficeTitle", english ? "OFFICE" : "ESCRITÓRIO", 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.02f, 0.935f, 0.28f, 0.995f);
            var tabNames = english
                ? new[] { "BUDGET", "MY CAREER", "BOARD", "CALENDAR", "STAFF" }
                : new[] { "ORÇAMENTO", "A MINHA CARREIRA", "QUADRO", "CALENDÁRIO", "EQUIPA TÉCNICA" };
            _officeTabs = new Button[tabNames.Length];
            _officeViews = new GameObject[tabNames.Length];
            for (var i = 0; i < tabNames.Length; i++)
            {
                var captured = i;
                var x0 = 0.008f + i * 0.198f;
                _officeTabs[i] = BroadcastUi.Button(page, "OfficeTab" + i, tabNames[i], () => ShowOfficeTab(captured),
                    new Color(1f, 1f, 1f, 0.08f), Color.white, 12);
                BroadcastUi.Place(_officeTabs[i].GetComponent<RectTransform>(), x0, 0.855f, x0 + 0.19f, 0.925f);
                var view = BroadcastUi.Zone(page, "OfficeView" + i, 0.01f, 0.015f, 0.99f, 0.835f);
                _officeViews[i] = view.gameObject;
                view.gameObject.SetActive(false);
            }

            BuildOfficeBudget(_officeViews[0].transform);
            BuildCareerRecord(_officeViews[1].transform);
            BuildBoard(_officeViews[2].transform);
            var calendarView = _officeViews[3].transform;
            var calendarTitle = BroadcastUi.Label(calendarView, "CalendarHint", english
                ? "Open the calendar to see crests, results and the transfer window."
                : "Abre o calendário para ver emblemas, resultados e a janela de transferências.", 14, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(calendarTitle.rectTransform, 0.04f, 0.62f, 0.96f, 0.82f);
            var openMonth = BroadcastUi.Button(calendarView, "OpenCalendar", english ? "OPEN CALENDAR" : "ABRIR CALENDÁRIO", OpenCalendarPopup, UiTheme.Accent, UiTheme.ButtonText, 16);
            BroadcastUi.Place(openMonth.GetComponent<RectTransform>(), 0.22f, 0.32f, 0.78f, 0.52f);
            var staffTitle = BroadcastUi.Label(_officeViews[4].transform, "StaffTitle", english ? "CLUB STAFF" : "EQUIPA TÉCNICA DO CLUBE", 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(staffTitle.rectTransform, 0.025f, 0.91f, 0.62f, 0.99f);
            var staffScroll = BroadcastUi.Scroll(_officeViews[4].transform, "StaffScroll", out _staffHost);
            BroadcastUi.Place(staffScroll.GetComponent<RectTransform>(), 0.01f, 0.02f, 0.99f, 0.9f);
            ShowOfficeTab(_officeTab);
        }

        void BuildOfficeBudget(Transform parent)
        {
            var scroll = BroadcastUi.Scroll(parent, "BudgetScroll", out _officeBudgetContent);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.015f, 0.02f, 0.985f, 0.98f);
            FillOfficeBudget();
        }

        void ShowOfficeTab(int index)
        {
            if (_officeViews == null || _officeTabs == null)
                return;
            _officeTab = Mathf.Clamp(index, 0, _officeViews.Length - 1);
            for (var i = 0; i < _officeViews.Length; i++)
            {
                _officeViews[i].SetActive(i == _officeTab);
                var active = i == _officeTab;
                _officeTabs[i].GetComponent<Image>().color = active ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f);
                var label = _officeTabs[i].GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.color = active ? UiTheme.ButtonText : Color.white;
                    label.fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
                }
            }

            if (_officeTab == 0) FillOfficeBudget();
            else if (_officeTab == 1) FillCareerRecord();
            else if (_officeTab == 2) FillBoard();
            else if (_officeTab == 3) FillCalendar();
            else if (_officeTab == 4) FillStaff();
        }

        GameObject BuildTeam(Transform host)
        {
            var page = BroadcastUi.Panel(host, "Team", new Color(0.08f, 0.03f, 0.16f, 0.45f));
            BroadcastUi.Stretch(page.rectTransform);
            var title = BroadcastUi.Label(page.transform, "Title", (LocalizationManager.Current == GameLanguage.English ? "SQUAD  ·  " : "PLANTEL  ·  ") + _career.Club.Tactics.Formation, 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            _teamTitle = title;
            BroadcastUi.Place(title.rectTransform, 0.45f, 0.93f, 0.69f, 0.99f);
            var caveat = BroadcastUi.Label(page.transform, "Caveat", LocalizationManager.Current == GameLanguage.English
                ? "Fatigue is the fitness coach's estimate and may be wrong."
                : "O cansaço é a leitura do preparador. Pode estar errado.", 13, UiTheme.Muted, TextAnchor.MiddleRight);
            BroadcastUi.Place(caveat.rectTransform, 0.70f, 0.93f, 0.99f, 0.99f);
            var pitchBg = BroadcastUi.Panel(page.transform, "PitchBg", new Color(0.05f, 0.16f, 0.08f, 1f));
            _pitchBg = pitchBg;
            BroadcastUi.Place(pitchBg.rectTransform, 0.03f, 0.31f, 0.42f, 0.92f);
            BroadcastUi.DrawVerticalHalfPitch(pitchBg.rectTransform, new Color(0.12f, 0.46f, 0.22f));
            _pitch = BroadcastUi.Zone(page.transform, "Pitch", 0.03f, 0.31f, 0.42f, 0.92f);
            ConfigureTeamPitch();
            var labels = LocalizationManager.Current == GameLanguage.English
                ? new[] { "ROLES", "TACTICS" }
                : new[] { "FUNÇÕES", "TÁTICAS" };
            var paneIds = new[] { 1, 3 };
            _paneTabs = new Button[labels.Length];
            if (_pane == 0 || _pane == 2)
                _pane = 1;
            for (var i = 0; i < labels.Length; i++)
            {
                var pane = paneIds[i];
                var y1 = 0.93f - i * 0.075f;
                _paneTabs[i] = BroadcastUi.Button(page.transform, "Pane" + pane, labels[i], () => SelectPane(pane), new Color(1f, 1f, 1f, 0.08f), Color.white, 14);
                BroadcastUi.Place(_paneTabs[i].GetComponent<RectTransform>(), 0.45f, y1 - 0.068f, 0.98f, y1);
            }

            _sheet = BroadcastUi.Zone(page.transform, "Sheet", 0.45f, 0.02f, 0.98f, 0.6f);
            _tacticsBody = BroadcastUi.Zone(page.transform, "Tactics", 0.45f, 0.02f, 0.98f, 0.6f);
            _tacHost = _tacticsBody.gameObject;
            _tacHost.SetActive(false);
            _subsTab = BroadcastUi.Button(page.transform, "SubsTab", LocalizationManager.Current == GameLanguage.English ? "SUBSTITUTES" : "SUPLENTES", () => SelectBenchGroup(false), UiTheme.Accent, UiTheme.ButtonText, 13);
            BroadcastUi.Place(_subsTab.GetComponent<RectTransform>(), 0.03f, 0.235f, 0.222f, 0.295f);
            _reservesTab = BroadcastUi.Button(page.transform, "ReservesTab", LocalizationManager.Current == GameLanguage.English ? "RESERVES" : "RESERVAS", () => SelectBenchGroup(true), new Color(1f, 1f, 1f, 0.08f), Color.white, 13);
            BroadcastUi.Place(_reservesTab.GetComponent<RectTransform>(), 0.228f, 0.235f, 0.42f, 0.295f);
            ConfigureTeamSubstitutes(page.transform, null);
            var zoneNames = LocalizationManager.Current == GameLanguage.English
                ? new[] { "TACTICS", "SQUAD" } : new[] { "TÁTICAS", "PLANTEL" };
            _zoneButtons = new Button[zoneNames.Length];
            for (var i = 0; i < zoneNames.Length; i++)
            {
                var zone = i;
                var x = 0.01f + i * 0.16f;
                var button = BroadcastUi.Button(page.transform, "Zone" + i, zoneNames[i], () => ShowTeamZone(zone), i == 0 ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), i == 0 ? UiTheme.ButtonText : Color.white, 14);
                _zoneButtons[i] = button;
                button.gameObject.SetActive(true);
                BroadcastUi.Place(button.GetComponent<RectTransform>(), x, 0.93f, x + 0.15f, 0.99f);
            }

            var list = BroadcastUi.Panel(page.transform, "ListPage", new Color(0.05f, 0.02f, 0.12f, 0.2f));
            BroadcastUi.Place(list.rectTransform, 0.01f, 0.02f, 0.99f, 0.91f);
            BroadcastUi.Scroll(list.transform, "ListScroll", out var listContent);
            var listScroll = list.transform.Find("ListScroll");
            BroadcastUi.Place(listScroll.GetComponent<RectTransform>(), 0.01f, 0.02f, 0.99f, 0.98f);
            _listPage = list.gameObject;
            _listHost = listContent;
            var market = BroadcastUi.Panel(page.transform, "MarketPage", new Color(0.05f, 0.02f, 0.12f, 0.2f));
            BroadcastUi.Place(market.rectTransform, 0.01f, 0.02f, 0.99f, 0.91f);
            BroadcastUi.Scroll(market.transform, "MarketScroll", out var marketContent);
            var marketScroll = market.transform.Find("MarketScroll");
            BroadcastUi.Place(marketScroll.GetComponent<RectTransform>(), 0.01f, 0.02f, 0.99f, 0.88f);
            _marketPage = market.gameObject;
            _marketHost = marketContent;
            BuildTeamRoster(page.transform);
            var search = UiFactory.Input(market.transform, "Procurar jogador pelo nome");
            _marketSearch = search;
            BroadcastUi.Place(search.GetComponent<RectTransform>(), 0.02f, 0.9f, 0.98f, 0.98f);
            search.onValueChanged.AddListener(value =>
            {
                if (_transferPage == 1)
                    FillMarket();
            });
            _listPage.SetActive(false);
            _marketPage.SetActive(false);
            FillTeam();
            return page.gameObject;
        }

        void ShowTeamZone(int zone)
        {
            _teamZone = Mathf.Clamp(zone, 0, 1);
            var tactical = _teamZone == 0;
            if (!tactical)
                _pendingBenchSwap = null;
            if (_pitchBg != null) _pitchBg.gameObject.SetActive(tactical);
            if (_pitch != null) _pitch.gameObject.SetActive(tactical);
            if (_subs != null) _subs.gameObject.SetActive(tactical);
            if (_subsViewport != null) _subsViewport.gameObject.SetActive(tactical);
            if (_subsTab != null) _subsTab.gameObject.SetActive(tactical);
            if (_reservesTab != null) _reservesTab.gameObject.SetActive(tactical);
            if (_subsPrev != null) _subsPrev.gameObject.SetActive(tactical && _subsPrev.interactable);
            if (_subsNext != null) _subsNext.gameObject.SetActive(tactical && _subsNext.interactable);
            if (_benchSwapHint != null) _benchSwapHint.gameObject.SetActive(tactical && _pendingBenchSwap != null);
            if (_teamTitle != null)
            {
                _teamTitle.gameObject.SetActive(tactical);
                _teamTitle.text = (LocalizationManager.Current == GameLanguage.English ? "SQUAD  ·  " : "PLANTEL  ·  ") + _career.Club.Tactics.Formation;
            }
            if (_paneTabs != null)
                foreach (var tab in _paneTabs) if (tab != null) tab.gameObject.SetActive(tactical);
            if (_sheet != null) _sheet.gameObject.SetActive(tactical && _pane != 3);
            if (_tacHost != null) _tacHost.SetActive(tactical && _pane == 3);
            if (_teamRosterPage != null) _teamRosterPage.SetActive(!tactical);
            if (_listPage != null) _listPage.SetActive(false);
            if (_marketPage != null) _marketPage.SetActive(false);
            if (!tactical) FillTeamRoster();
            if (_zoneButtons != null)
                for (var i = 0; i < _zoneButtons.Length; i++)
                {
                    var selected = i == _teamZone;
                    _zoneButtons[i].GetComponent<Image>().color = selected ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f);
                    var label = _zoneButtons[i].GetComponentInChildren<Text>();
                    if (label != null) label.color = selected ? UiTheme.ButtonText : Color.white;
                }
        }

        void BuildTeamRoster(Transform team)
        {
            if (team == null) return;
            _teamRosterPage = team.Find("TeamRosterPage")?.gameObject;
            if (_teamRosterPage == null)
            {
                var panel = BroadcastUi.Panel(team, "TeamRosterPage", new Color(0.05f, 0.02f, 0.12f, 0.25f));
                _teamRosterPage = panel.gameObject;
            }
            BroadcastUi.Place(_teamRosterPage.GetComponent<RectTransform>(), 0.01f, 0.02f, 0.99f, 0.91f);
            var scroll = _teamRosterPage.transform.Find("RosterScroll") as RectTransform;
            if (scroll == null)
            {
                var created = BroadcastUi.Scroll(_teamRosterPage.transform, "RosterScroll", out _teamRosterHost);
                scroll = created.GetComponent<RectTransform>();
            }
            else
                _teamRosterHost = scroll.Find("Content") as RectTransform;
            if (_teamRosterHost == null && scroll != null)
                _teamRosterHost = scroll.GetComponentInChildren<VerticalLayoutGroup>(true)?.transform as RectTransform;
            if (scroll != null) BroadcastUi.Place(scroll, 0.01f, 0.01f, 0.99f, 0.99f);
            _teamRosterPage.SetActive(false);
        }

        void ConfigureTeamPitch()
        {
            if (_pitchBg != null)
            {
                BroadcastUi.Place(_pitchBg.rectTransform, 0.03f, 0.31f, 0.42f, 0.92f);
                Clear(_pitchBg.rectTransform);
                BroadcastUi.DrawVerticalHalfPitch(_pitchBg.rectTransform, new Color(0.12f, 0.46f, 0.22f));
            }
            if (_pitch != null)
                BroadcastUi.Place(_pitch, 0.03f, 0.31f, 0.42f, 0.92f);
        }

        void ConfigureTeamSubstitutes(Transform page, RectTransform content)
        {
            if (page == null)
                return;

            var viewport = page.Find("SubsViewport") as RectTransform;
            if (viewport == null)
            {
                var viewportObject = new GameObject("SubsViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
                viewport = viewportObject.GetComponent<RectTransform>();
                viewport.SetParent(page, false);
            }
            BroadcastUi.Place(viewport, 0.06f, 0.035f, 0.39f, 0.20f);
            var viewportImage = viewport.GetComponent<Image>();
            if (viewportImage == null) viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            viewportImage.raycastTarget = true;
            if (viewport.GetComponent<RectMask2D>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();

            if (content == null)
            {
                var contentObject = new GameObject("SubsContent", typeof(RectTransform));
                content = contentObject.GetComponent<RectTransform>();
            }
            content.name = "SubsContent";
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);
            var contentImage = content.GetComponent<Image>();
            if (contentImage != null)
            {
                contentImage.color = new Color(0f, 0f, 0f, 0f);
                contentImage.raycastTarget = false;
            }

            _subsViewport = viewport;
            _subs = content;
            _subsScroll = viewport.GetComponent<ScrollRect>();
            if (_subsScroll == null) _subsScroll = viewport.gameObject.AddComponent<ScrollRect>();
            _subsScroll.content = content;
            _subsScroll.viewport = viewport;
            _subsScroll.horizontal = true;
            _subsScroll.vertical = false;
            _subsScroll.movementType = ScrollRect.MovementType.Clamped;
            _subsScroll.inertia = true;
            _subsScroll.scrollSensitivity = 40f;

            _subsPrev = EnsureTeamScrollButton(page, "SubsPrev", "‹", () => ScrollSubsPage(-1f));
            _subsNext = EnsureTeamScrollButton(page, "SubsNext", "›", () => ScrollSubsPage(1f));
            if (_subsPrev != null) BroadcastUi.Place(_subsPrev.GetComponent<RectTransform>(), 0.03f, 0.07f, 0.055f, 0.16f);
            if (_subsNext != null) BroadcastUi.Place(_subsNext.GetComponent<RectTransform>(), 0.395f, 0.07f, 0.42f, 0.16f);

            var hintTransform = page.Find("BenchSwapHint");
            if (hintTransform != null)
                _benchSwapHint = hintTransform.GetComponent<Text>();
            if (_benchSwapHint == null)
                _benchSwapHint = BroadcastUi.Label(page, "BenchSwapHint", "", 11, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(_benchSwapHint.rectTransform, 0.03f, 0.198f, 0.42f, 0.23f);
            _benchSwapHint.raycastTarget = false;
            _benchSwapHint.gameObject.SetActive(false);
        }

        Button EnsureTeamScrollButton(Transform page, string name, string label, Action action)
        {
            var existing = page.Find(name);
            var button = existing != null && existing.GetComponent<Button>() != null
                ? SceneBind.Wire(existing, action)
                : BroadcastUi.Button(page, name, label, action, new Color(1f, 1f, 1f, 0.12f), Color.white, 19);
            if (button == null)
                return null;
            button.gameObject.SetActive(true);
            var text = button.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
            return button;
        }

        void ScrollSubsPage(float direction)
        {
            if (_subs == null || _subsViewport == null)
                return;
            var maxOffset = Mathf.Max(0f, _subs.rect.width - _subsViewport.rect.width);
            if (maxOffset <= 1f)
                return;
            var offset = Mathf.Clamp(-_subs.anchoredPosition.x + direction * _subsViewport.rect.width * 0.78f, 0f, maxOffset);
            _subs.anchoredPosition = new Vector2(-offset, _subs.anchoredPosition.y);
        }

        void FillTeamRoster()
        {
            if (_teamRosterHost == null) return;
            Clear(_teamRosterHost);
            var english = LocalizationManager.Current == GameLanguage.English;
            var title = BroadcastUi.Label(_teamRosterHost, "SquadTitle", english ? "FIRST TEAM · CONTRACTS" : "PLANTEL PRINCIPAL · CONTRATOS", 15, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Preferred(title, 30);
            var roster = Squad(_career.Club);
            for (var i = 0; i < roster.Count; i++)
            {
                var player = roster[i];
                var row = BroadcastUi.Panel(_teamRosterHost, "Player" + i, new Color(0.1f, 0.04f, 0.2f, 0.92f), true);
                BroadcastUi.Preferred(row, 62);
                var face = FaceImage(row.transform, player, 42f);
                face.rectTransform.anchorMin = face.rectTransform.anchorMax = new Vector2(0.035f, 0.5f);
                face.rectTransform.sizeDelta = new Vector2(42, 42);
                var end = player.ContractEndTicks > 0 && player.ContractEndTicks <= DateTime.MaxValue.Ticks
                    ? LocalizationManager.FormatDate(new DateTime(player.ContractEndTicks), "dd/MM/yyyy") : "—";
                var text = BroadcastUi.Label(row.transform, "Details", player.FullName + "  ·  " + PresentationLabels.Position(player.Position)
                    + "  ·  " + Euro(player.WeeklyWage) + (english ? "/wk" : "/semana") + "  ·  " + (english ? "Until " : "Até ") + end,
                    14, Color.white, TextAnchor.MiddleLeft);
                BroadcastUi.Place(text.rectTransform, 0.075f, 0.12f, 0.66f, 0.88f);
                var captured = player;
                var profile = BroadcastUi.Button(row.transform, "Profile", english ? "PROFILE" : "FICHA", () => OpenPlayer(captured), UiTheme.Accent, UiTheme.ButtonText, 12);
                BroadcastUi.Place(profile.GetComponent<RectTransform>(), 0.68f, 0.18f, 0.82f, 0.82f);
                var renew = BroadcastUi.Button(row.transform, "Renew", english ? "NEGOTIATE" : "NEGOCIAR", () => OpenRenewalNegotiation(captured), new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 12);
                BroadcastUi.Place(renew.GetComponent<RectTransform>(), 0.835f, 0.18f, 0.98f, 0.82f);
            }
        }

        void FillSquadList(RectTransform target = null)
        {
            var host = target ?? _transferListHost ?? _listHost;
            if (host == null) return;
            Clear(host);
            var english = LocalizationManager.Current == GameLanguage.English;
            var tracked = BroadcastUi.Label(host, "TrackedTitle", english ? "SHORTLIST" : "LISTA DE OBSERVAÇÃO", 15, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Preferred(tracked, 30);
            var hasTargets = false;
            foreach (var id in _career.Targets)
            {
                if (!_career.World.Players.TryGetValue(id, out var player) || player.ClubId == _career.Club.Id) continue;
                hasTargets = true;
                AddShortlistRow(player, false, host);
            }
            if (!hasTargets)
            {
                var empty = BroadcastUi.Label(host, "NoTargets", english ? "No players shortlisted. Add a player from the transfer market." : "Ainda não há jogadores na lista. Adiciona alguém no mercado de transferências.", 13, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Preferred(empty, 32);
            }

        }

        void AddShortlistRow(Player player, bool own, RectTransform target = null)
        {
            var host = target ?? _transferListHost ?? _listHost;
            if (host == null) return;
            var english = LocalizationManager.Current == GameLanguage.English;
            var row = BroadcastUi.Panel(host, "P", new Color(0.1f, 0.04f, 0.2f, 0.92f), true);
            BroadcastUi.Preferred(row, 64);
            var face = FaceImage(row.transform, player, 42f);
            face.rectTransform.anchorMin = face.rectTransform.anchorMax = new Vector2(0.04f, 0.5f);
            face.rectTransform.sizeDelta = new Vector2(42, 42);
            var end = player.ContractEndTicks > 0 && player.ContractEndTicks <= DateTime.MaxValue.Ticks ? new DateTime(player.ContractEndTicks).ToString("dd/MM/yyyy") : "—";
            var line = BroadcastUi.Label(row.transform, "Line", player.SquadNumber + "   " + player.FullName + "   ·   " + PresentationLabels.Position(player.Position) + "   ·   " + (english ? "Ends " : "Termina ") + end, 15, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(line.rectTransform, 0.08f, 0.12f, own ? 0.58f : 0.52f, 0.88f);
            var captured = player;
            var file = BroadcastUi.Button(row.transform, "File", english ? "PROFILE" : "FICHA", () => OpenPlayer(captured), UiTheme.Accent, UiTheme.ButtonText, 13);
            BroadcastUi.Place(file.GetComponent<RectTransform>(), own ? 0.6f : 0.535f, 0.18f, own ? 0.76f : 0.66f, 0.82f);
            if (own)
            {
                var renew = BroadcastUi.Button(row.transform, "Renew", english ? "NEGOTIATE" : "NEGOCIAR", () => OpenRenewalNegotiation(captured), new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 12);
                BroadcastUi.Place(renew.GetComponent<RectTransform>(), 0.78f, 0.18f, 0.97f, 0.82f);
            }
            else
            {
                var negotiate = BroadcastUi.Button(row.transform, "Negotiate", english ? "NEGOTIATE" : "NEGOCIAR",
                    () => OpenTransferNegotiation(captured), new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 12);
                BroadcastUi.Place(negotiate.GetComponent<RectTransform>(), 0.675f, 0.18f, 0.82f, 0.82f);
                var remove = BroadcastUi.Button(row.transform, "Remove", english ? "REMOVE" : "TIRAR", () =>
                {
                    _career.Targets.Remove(captured.Id);
                    FillSquadList(host);
                }, new Color(1f, 1f, 1f, 0.08f), Color.white, 11);
                BroadcastUi.Place(remove.GetComponent<RectTransform>(), 0.835f, 0.18f, 0.97f, 0.82f);
            }
        }

        void FillMarket(InputField searchField = null, RectTransform target = null)
        {
            var host = target ?? _transferMarketHost ?? _marketHost;
            if (host == null) return;
            var search = searchField ?? _transferSearch ?? _marketSearch;
            Clear(host);
            CompleteScouts();
            EnsureScout(_career.Club);
            ClaimForeignTargets();
            var scout = Member(_career.Club, StaffRole.Scout);
            Section(host, "A MINHA LISTA");
            var wanted = false;
            foreach (var id in _career.Targets)
            {
                if (!_career.World.Players.TryGetValue(id, out var player) || player.ClubId == _career.Club.Id)
                    continue;
                wanted = true;
                MarketRow(host, player, false, scout, true, search);
            }

            if (!wanted)
                EmptyLine(host, "Ainda não marcaste jogadores de outros clubes.");

            Section(host, "PLANTEL À VENDA");
            var any = false;
            foreach (var id in _career.Listed)
            {
                if (!_career.World.Players.TryGetValue(id, out var player) || player.ClubId != _career.Club.Id)
                    continue;
                any = true;
                MarketRow(host, player, true, scout, false, search);
            }

            if (!any)
                EmptyLine(host, "Ainda não puseste ninguém do plantel à venda.");

            Section(host, "LISTA DE EMPRÉSTIMOS");
            var loaned = false;
            foreach (var id in _career.Loans)
            {
                if (!_career.World.Players.TryGetValue(id, out var player) || player.ClubId != _career.Club.Id)
                    continue;
                loaned = true;
                MarketRow(host, player, true, scout, false, search);
            }

            if (!loaned)
                EmptyLine(host, "Ainda não puseste ninguém na lista de empréstimos.");

            Section(host, LocalizationManager.Current == GameLanguage.English ? "FREE AGENTS" : "JOGADORES LIVRES");
            var freeAgents = new List<Player>();
            foreach (var candidate in _career.World.Players.Values)
                if (candidate != null && string.IsNullOrEmpty(candidate.ClubId) && candidate.Age >= 18)
                    freeAgents.Add(candidate);
            freeAgents.Sort((a, b) =>
            {
                var overall = PlayerAttributes.Overall(b).CompareTo(PlayerAttributes.Overall(a));
                return overall != 0 ? overall : string.CompareOrdinal(a.Id, b.Id);
            });
            var freeAgentCount = Math.Min(8, freeAgents.Count);
            for (var i = 0; i < freeAgentCount; i++)
                MarketRow(host, freeAgents[i], false, scout, _career.Targets.Contains(freeAgents[i].Id), search);
            if (freeAgentCount == 0)
                EmptyLine(host, LocalizationManager.Current == GameLanguage.English ? "No free agents are currently available." : "Não há jogadores livres disponíveis neste momento.");

            var query = search == null || search.text == null ? "" : search.text.Trim();
            Section(host, query.Length < 2 ? "PROCURAR NOUTROS CLUBES" : "RESULTADOS");
            if (query.Length < 2)
            {
                EmptyLine(host, "Escreve pelo menos duas letras do nome.");
                return;
            }

            var shown = 0;
            foreach (var player in _career.World.Players.Values)
            {
                if (player.ClubId == _career.Club.Id || string.IsNullOrEmpty(player.ClubId) || shown >= 40)
                    continue;
                if (player.FullName == null || player.FullName.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                shown++;
                MarketRow(host, player, false, scout, _career.Targets.Contains(player.Id), search);
            }

            if (shown == 0)
                EmptyLine(host, "Nenhum jogador com esse nome.");
        }

        void Section(RectTransform host, string title)
        {
            var head = BroadcastUi.Label(host, "Head", LocalizationManager.Translate(title), 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Preferred(head, 32);
        }

        void EmptyLine(RectTransform host, string text)
        {
            var empty = BroadcastUi.Label(host, "Empty", LocalizationManager.Translate(text), 14, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Preferred(empty, 28);
        }

        void MarketRow(RectTransform host, Player player, bool own, StaffMember scout, bool targeted, InputField search)
        {
            var row = BroadcastUi.Panel(host, "M", new Color(0.1f, 0.04f, 0.2f, 0.92f), true);
            BroadcastUi.Preferred(row, 64);
            var face = FaceImage(row.transform, player, 36f);
            face.rectTransform.anchorMin = face.rectTransform.anchorMax = new Vector2(0.03f, 0.5f);
            face.rectTransform.sizeDelta = new Vector2(36, 36);
            var job = ScoutJobFor(player.Id);
            var seen = _career.ScoutedPlayers.Contains(player.Id);
            string read;
            if (own)
                read = _career.Loans.Contains(player.Id) ? "Na lista de empréstimos." : "No plantel, à venda.";
            else if (targeted && !seen)
                read = "Na tua lista. Ainda sem relatório do olheiro.";
            else if (seen)
                read = "Relatório do olheiro: overall " + StaffReport.Read(scout, player.Id, "overall", PlayerAttributes.CurrentOverall(player), 40, 94) + "  ·  " + Euro(MarketValue(player));
            else if (job != null)
                read = LocalizationManager.Current == GameLanguage.English
                    ? "The scout will return with the report on " + LocalizationManager.FormatDate(new DateTime(job.Due), "d MMMM") + "."
                    : "O olheiro volta com o relatório a " + LocalizationManager.FormatDate(new DateTime(job.Due), "d 'de' MMMM") + ".";
            else
                read = "Ainda sem relatório.";
            read = LocalizationManager.Translate(read);
            var clubName = _career.World.Clubs.ContainsKey(player.ClubId) ? _career.World.Clubs[player.ClubId].ShortName
                : string.IsNullOrEmpty(player.ClubId) ? (LocalizationManager.Current == GameLanguage.English ? "FREE AGENT" : "SEM CLUBE") : "";
            var interest = GlobalTransferMarket.Contenders(_career, player);
            if (!own && interest.Count > 0)
            {
                var rivals = new List<string>();
                for (var i = 0; i < interest.Count; i++)
                    if (interest[i].Id != _career.Club.Id) rivals.Add(interest[i].ShortName);
                if (rivals.Count > 0)
                    read += (LocalizationManager.Current == GameLanguage.English ? " · Also wanted by " : " · Também interessa a ") + string.Join(", ", rivals);
            }
            var line = BroadcastUi.Label(row.transform, "Line", player.FullName + "   " + PresentationLabels.Position(player.Position) + "   " + clubName + "\n" + read, 14, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(line.rectTransform, 0.07f, 0.08f, 0.62f, 0.92f);
            var captured = player;
            BroadcastUi.Hit(row, () => OpenPlayer(captured));
            if (own)
            {
                var off = BroadcastUi.Button(row.transform, "Off", LocalizationManager.Translate("RETIRAR"), () =>
                {
                    _career.Listed.Remove(captured.Id);
                    _career.Loans.Remove(captured.Id);
                    FillMarket(search, host);
                }, new Color(1f, 1f, 1f, 0.08f), Color.white, 13);
                BroadcastUi.Place(off.GetComponent<RectTransform>(), 0.78f, 0.18f, 0.97f, 0.82f);
                return;
            }

            if (targeted)
            {
                var drop = BroadcastUi.Button(row.transform, "Drop", LocalizationManager.Translate("TIRAR"), () =>
                {
                    _career.Targets.Remove(captured.Id);
                    FillMarket(search, host);
                }, new Color(1f, 1f, 1f, 0.08f), Color.white, 13);
                BroadcastUi.Place(drop.GetComponent<RectTransform>(), 0.64f, 0.18f, 0.78f, 0.82f);
            }
            else
            {
                var add = BroadcastUi.Button(row.transform, "Add", LocalizationManager.Translate("LISTA"), () =>
                {
                    _career.Targets.Add(captured.Id);
                    _career.Listed.Remove(captured.Id);
                    FillMarket(search, host);
                }, new Color(1f, 1f, 1f, 0.08f), Color.white, 13);
                BroadcastUi.Place(add.GetComponent<RectTransform>(), 0.64f, 0.18f, 0.78f, 0.82f);
            }

            if (job != null && !seen)
            {
                var wait = BroadcastUi.Label(row.transform, "Wait", LocalizationManager.Translate("À ESPERA"), 13, UiTheme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(wait.rectTransform, 0.8f, 0.18f, 0.97f, 0.82f);
                return;
            }

            if (!seen)
            {
                var watch = BroadcastUi.Button(row.transform, "Scout", LocalizationManager.Translate("OLHEIRO"), () => AskScout(captured), UiTheme.Accent, UiTheme.ButtonText, 13);
                BroadcastUi.Place(watch.GetComponent<RectTransform>(), 0.8f, 0.18f, 0.97f, 0.82f);
                return;
            }

            var sign = BroadcastUi.Button(row.transform, "Sign", LocalizationManager.Current == GameLanguage.English ? "NEGOTIATE" : "NEGOCIAR", () => OpenTransferNegotiation(captured), new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 13);
            BroadcastUi.Place(sign.GetComponent<RectTransform>(), 0.8f, 0.18f, 0.97f, 0.82f);
        }

        void ClaimForeignTargets()
        {
            var move = new List<string>();
            foreach (var id in _career.Listed)
            {
                if (_career.World.Players.TryGetValue(id, out var player) && player.ClubId != _career.Club.Id)
                    move.Add(id);
            }

            foreach (var id in move)
            {
                _career.Listed.Remove(id);
                _career.Targets.Add(id);
            }
        }

        void AskScout(Player player)
        {
            if (player == null || ScoutJobFor(player.Id) != null || _career.ScoutedPlayers.Contains(player.Id))
                return;
            _career.ScoutJobs.Add(new ScoutJob
            {
                PlayerId = player.Id,
                Due = _career.Calendar.CurrentDate.Date.AddDays(3).Ticks
            });
            FillMarket();
        }

        ScoutJob ScoutJobFor(string playerId)
        {
            foreach (var job in _career.ScoutJobs)
            {
                if (job.PlayerId == playerId)
                    return job;
            }

            return null;
        }

        void CompleteScouts()
        {
            var changedScout = false;
            var now = _career.Calendar.CurrentDate.Date;
            for (var i = _career.ScoutJobs.Count - 1; i >= 0; i--)
            {
                var job = _career.ScoutJobs[i];
                if (new DateTime(job.Due).Date > now)
                    continue;
                _career.ScoutedPlayers.Add(job.PlayerId);
                _career.ScoutJobs.RemoveAt(i);
                changedScout = true;
                if (_career.World.Players.TryGetValue(job.PlayerId, out var player))
                {
                    _career.Board.Insert(0, new CoachNote
                    {
                        Day = _career.Calendar.WeekdayName,
                        Hour = _career.Calendar.ClockText,
                        When = _career.Calendar.CurrentDate,
                        Text = "✉ O olheiro entregou o relatório de " + player.FullName + ".\n[[jogador:" + player.Id + "]]"
                    });
                }
            }
            if (ResolveDeskJobs() || changedScout)
                CareerStore.Save(_career);
        }

        static int MarketValue(Player player)
        {
            var overall = PlayerAttributes.Overall(player);
            var millions = 3.0 * System.Math.Exp(0.2 * (overall - 70));
            var age = player.Age;
            var factor = 1.0;
            if (age <= 21)
                factor = 1.7;
            else if (age <= 24)
                factor = 1.35;
            else if (age <= 27)
                factor = 1.1;
            else if (age <= 30)
                factor = 0.9;
            else if (age <= 32)
                factor = 0.6;
            else
                factor = 0.35;
            if (player.Injury != null && player.Injury.IsInjured)
                factor *= 0.7;
            var gap = player.Potential - overall;
            if (player.Age <= 23 && gap > 0)
                factor *= 1.0 + gap * 0.035;
            var value = (int)(millions * 1000000.0 * factor);
            value = Mathf.Max(80000, value);
            if (value >= 1000000000)
                return (int)(System.Math.Round(value / 25000000.0, System.MidpointRounding.AwayFromZero) * 25000000.0);
            if (value >= 100000000)
                return (int)(System.Math.Round(value / 10000000.0, System.MidpointRounding.AwayFromZero) * 10000000.0);
            if (value >= 25000000)
                return (int)(System.Math.Round(value / 5000000.0, System.MidpointRounding.AwayFromZero) * 5000000.0);
            if (value >= 5000000)
                return (int)(System.Math.Round(value / 1000000.0, System.MidpointRounding.AwayFromZero) * 1000000.0);
            if (value >= 1000000)
                return (int)(System.Math.Round(value / 500000.0, System.MidpointRounding.AwayFromZero) * 500000.0);
            return (int)(System.Math.Round(value / 50000.0, System.MidpointRounding.AwayFromZero) * 50000.0);
        }

        string FinanceLine(Club club, bool english)
        {
            if (club == null || club.Finance == null)
                return "";
            if (!ClubFinancesKnown(club))
                return "\n" + (english ? "Budgets stay private until the press leaks them." : "As verbas só aparecem se a imprensa as divulgar.");
            return "\n" + (english ? "Transfer budget  " : "Orçamento de transferências  ") + Euro(club.Finance.TransferBudget)
                + "   ·   " + (english ? "Club balance  " : "Saldo do clube  ") + Euro(club.Finance.Balance);
        }

        bool ClubFinancesKnown(Club club)
        {
            if (club == null || _career == null || _career.Club == null)
                return false;
            if (club.Id == _career.Club.Id)
                return true;
            return _career.Notebook != null && _career.Notebook.Contains("leak|" + club.Id);
        }

        string SystemFit(Player player)
        {
            if (player == null)
                return "F";
            var score = PlayerAttributes.CurrentOverallAtPosition(player, player.Position);
            var tactics = _career == null || _career.Club == null ? null : _career.Club.Tactics;
            if (tactics != null)
            {
                var press = tactics.Pressing ?? "";
                if (press.IndexOf("alta", StringComparison.OrdinalIgnoreCase) >= 0 || press.IndexOf("high", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += (PlayerAttributes.CurrentValue(player, "Velocidade") + PlayerAttributes.CurrentValue(player, "Entrega") - 20) / 5;
                var build = tactics.BuildUp ?? "";
                if (build.IndexOf("curta", StringComparison.OrdinalIgnoreCase) >= 0 || build.IndexOf("short", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += (PlayerAttributes.CurrentValue(player, "Passe") - 10) / 4;
                var line = tactics.DefensiveLine ?? "";
                if (line.IndexOf("alta", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("high", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += (PlayerAttributes.CurrentValue(player, "Velocidade") - 10) / 5;
            }
            score = Mathf.Clamp(score, 1, 99);
            if (score >= 90) return "S";
            if (score >= 86) return "A+";
            if (score >= 82) return "A";
            if (score >= 78) return "A-";
            if (score >= 74) return "B+";
            if (score >= 70) return "B";
            if (score >= 66) return "B-";
            if (score >= 62) return "C+";
            if (score >= 58) return "C";
            if (score >= 54) return "C-";
            if (score >= 50) return "D";
            if (score >= 46) return "E";
            return "F";
        }

        static Color FitGradeColor(string grade)
        {
            switch (grade)
            {
                case "S": return new Color(1f, 0.82f, 0.25f);
                case "A+": return new Color(0.45f, 0.95f, 0.55f);
                case "A": return new Color(0.25f, 0.82f, 0.45f);
                case "A-": return new Color(0.2f, 0.7f, 0.4f);
                case "B+": return new Color(0.45f, 0.75f, 1f);
                case "B": return new Color(0.3f, 0.58f, 0.95f);
                case "B-": return new Color(0.25f, 0.45f, 0.82f);
                case "C+": return new Color(0.95f, 0.85f, 0.35f);
                case "C": return new Color(0.9f, 0.72f, 0.25f);
                case "C-": return new Color(0.85f, 0.58f, 0.2f);
                case "D": return new Color(0.9f, 0.45f, 0.25f);
                case "E": return new Color(0.85f, 0.3f, 0.28f);
                default: return new Color(0.65f, 0.65f, 0.7f);
            }
        }

        static string Euro(int value)
        {
            var en = CultureInfo.GetCultureInfo("en-GB");
            var pt = CultureInfo.GetCultureInfo("pt-PT");
            if (LocalizationManager.Current == GameLanguage.English)
            {
                if (value >= 1000000000)
                    return "€" + (value / 1000000000f).ToString("0.0", en) + "bn";
                if (value >= 10000000)
                    return "€" + (value / 1000000f).ToString("0.0", en) + "M";
                if (value >= 1000000)
                    return "€" + (value / 1000000f).ToString("0.00", en) + "M";
                return "€" + value.ToString("N0", en);
            }
            if (value >= 1000000000)
                return (value / 1000000000f).ToString("0.0", pt) + " mil milhões €";
            if (value >= 10000000)
                return (value / 1000000f).ToString("0.0", pt) + "M €";
            if (value >= 1000000)
                return (value / 1000000f).ToString("0,00", pt) + "M €";
            return value.ToString("N0", pt) + " €";
        }

        int WeeklyWageRoom()
        {
            if (_career == null || _career.Club == null || _career.Club.Finance == null)
                return 0;
            var budget = ClubFinanceLedger.EnsureWeeklyWageBudget(_career, _career.Club);
            var committed = ClubFinanceLedger.WeeklyPayroll(_career, _career.Club, _career.Calendar.CurrentDate);
            return (int)Math.Max(0L, (long)budget - committed);
        }

        bool TransferWindowOpen()
        {
            var month = _career == null || _career.Calendar == null ? 0 : _career.Calendar.CurrentDate.Month;
            return month == 1 || month == 7 || month == 8;
        }

        void OpenFreeAgentNegotiation(Player player)
        {
            if (player == null || _career == null || _career.Club == null || !string.IsNullOrEmpty(player.ClubId))
                return;
            var previous = transform.Find("TransferSheet");
            if (previous != null) Destroy(previous.gameObject);
            var english = LocalizationManager.Current == GameLanguage.English;
            var sheet = BroadcastUi.Panel(transform, "TransferSheet", new Color(0.08f, 0.03f, 0.16f, 0.98f), true);
            BroadcastUi.Place(sheet.rectTransform, 0.08f, 0.12f, 0.92f, 0.9f);
            BroadcastUi.ApplySandPaper(sheet, 0.1f);
            sheet.transform.SetAsLastSibling();
            var close = BroadcastUi.Button(sheet.transform, "Close", "×", () => Destroy(sheet.gameObject), new Color(1f, 1f, 1f, 0.1f), Color.white, 20);
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.94f, 0.9f, 0.985f, 0.975f);

            var header = BroadcastUi.Panel(sheet.transform, "Header", new Color(0.12f, 0.05f, 0.24f, 1f), false);
            BroadcastUi.Place(header.rectTransform, 0.02f, 0.79f, 0.92f, 0.97f);
            var faceSlot = BroadcastUi.Zone(header.transform, "FaceSlot", 0.015f, 0.08f, 0.12f, 0.92f);
            FaceImage(faceSlot, player, 64f);
            var title = BroadcastUi.Label(header.transform, "Title", english ? "FREE AGENT · CONTRACT TALKS" : "JOGADOR LIVRE · NEGOCIAÇÃO DE CONTRATO", 19, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.15f, 0.58f, 0.95f, 0.92f);
            var name = BroadcastUi.Label(header.transform, "Name", player.FullName, 23, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(name.rectTransform, 0.15f, 0.1f, 0.95f, 0.62f);

            var wageAsk = Math.Max(250, player.WeeklyWage);
            var wageRoom = WeeklyWageRoom();
            var details = BroadcastUi.Panel(sheet.transform, "Details", new Color(0.1f, 0.04f, 0.2f, 1f), false);
            BroadcastUi.Place(details.rectTransform, 0.02f, 0.22f, 0.32f, 0.76f);
            SectionTitle(details.transform, english ? "PLAYER & CLUB" : "JOGADOR E CLUBE");
            ContractRow(details.transform, "Age", english ? "Age · position" : "Idade · posição", player.Age + " · " + ProfilePosition(player.Position, english), 0.72f);
            ContractRow(details.transform, "Overall", "Overall", PlayerAttributes.Overall(player).ToString(), 0.59f);
            ContractRow(details.transform, "Fee", english ? "Transfer fee" : "Valor de transferência", english ? "No fee" : "Sem custo", 0.46f);
            ContractRow(details.transform, "Ask", english ? "Wage requested" : "Salário pedido", Euro(wageAsk) + (english ? "/wk" : "/sem"), 0.33f);
            ContractRow(details.transform, "Room", english ? "Available wage room" : "Folga salarial", Euro(wageRoom) + (english ? "/wk" : "/sem"), 0.2f);
            var note = BroadcastUi.Label(details.transform, "Note", english
                ? "There is no transfer fee. Weekly wages still count against the wage budget and are paid from the club balance."
                : "Não há valor de transferência. O salário semanal conta para o orçamento salarial e é pago pelo saldo do clube.", 12, UiTheme.Muted, TextAnchor.UpperLeft);
            note.horizontalOverflow = HorizontalWrapMode.Wrap;
            BroadcastUi.Place(note.rectTransform, 0.05f, 0.025f, 0.95f, 0.16f);

            var offers = BroadcastUi.Panel(sheet.transform, "Offers", new Color(0.1f, 0.04f, 0.2f, 1f), false);
            BroadcastUi.Place(offers.rectTransform, 0.34f, 0.22f, 0.98f, 0.76f);
            SectionTitle(offers.transform, english ? "CONTRACT PROPOSALS" : "PROPOSTAS DE CONTRATO");
            var wages = new[] { Mathf.Max(250, Mathf.RoundToInt(wageAsk * 0.9f)), wageAsk, Mathf.RoundToInt(wageAsk * 1.15f) };
            var terms = new[] { 2, 3, 4 };
            var offerNames = english ? new[] { "LOWER WAGE", "MEET THE ASK", "STRONG OFFER" } : new[] { "SALÁRIO MAIS BAIXO", "ACEITAR O PEDIDO", "PROPOSTA FORTE" };
            var pendingBoard = NotebookHas("budget-live") || DeskDue("budget", out _);
            var boardWaiting = DeskDue("budget-wait", out var boardWaitUntil)
                && new DateTime(boardWaitUntil).Date > _career.Calendar.CurrentDate.Date;
            for (var i = 0; i < wages.Length; i++)
            {
                var wage = wages[i];
                var years = terms[i];
                var gap = Math.Max(0, wage - wageRoom);
                var canAskBoard = gap > 0 && gap <= 250000 && !pendingBoard && !boardWaiting;
                var canOffer = gap == 0;
                var x0 = 0.025f + i * 0.325f;
                var card = BroadcastUi.Panel(offers.transform, "Offer" + i, new Color(0.14f, 0.06f, 0.28f, 1f), true);
                BroadcastUi.Place(card.rectTransform, x0, 0.12f, x0 + 0.305f, 0.82f);
                var offerTitle = BroadcastUi.Label(card.transform, "Title", offerNames[i], 12, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(offerTitle.rectTransform, 0.06f, 0.77f, 0.94f, 0.95f);
                var amount = BroadcastUi.Label(card.transform, "Wage", Euro(wage) + (english ? " / week" : " / semana"), 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(amount.rectTransform, 0.06f, 0.54f, 0.94f, 0.76f);
                var length = BroadcastUi.Label(card.transform, "Length", years + (english ? "-year contract" : " anos de contrato"), 13, UiTheme.Muted, TextAnchor.MiddleCenter);
                BroadcastUi.Place(length.rectTransform, 0.06f, 0.4f, 0.94f, 0.55f);
                var buttonLabel = canOffer ? (english ? "OFFER CONTRACT" : "PROPOR CONTRATO")
                    : canAskBoard ? (english ? "ASK BOARD FOR WAGE ROOM" : "PEDIR REFORÇO SALARIAL")
                    : (english ? "OVER WAGE BUDGET" : "ACIMA DA VERBA SALARIAL");
                var captured = player;
                var button = BroadcastUi.Button(card.transform, "OfferButton", buttonLabel, () =>
                {
                    if (canOffer)
                    {
                        ResolveFreeAgentOffer(captured, wage, years);
                        return;
                    }
                    if (canAskBoard)
                    {
                        var openSheet = transform.Find("TransferSheet");
                        if (openSheet != null) Destroy(openSheet.gameObject);
                        OpenBoardMeeting(captured, 0, wage, years, 0, gap);
                        return;
                    }
                    OpenTransferRefusal(captured, english
                        ? "The weekly wage is above the club's available wage budget. Nothing was signed."
                        : "O salário semanal ultrapassa o orçamento salarial disponível. Não foi assinado nenhum contrato.");
                }, canOffer || canAskBoard ? UiTheme.Accent : new Color(0.25f, 0.12f, 0.16f, 1f), canOffer || canAskBoard ? UiTheme.ButtonText : Color.white, 11);
                BroadcastUi.Place(button.GetComponent<RectTransform>(), 0.06f, 0.08f, 0.94f, 0.32f);
            }
            var footer = BroadcastUi.Label(sheet.transform, "Footer", english
                ? "The player can accept, reject or come back with a wage counter-offer."
                : "O jogador pode aceitar, recusar ou responder com uma contraproposta salarial.", 13, UiTheme.Muted, TextAnchor.MiddleCenter);
            BroadcastUi.Place(footer.rectTransform, 0.03f, 0.04f, 0.97f, 0.16f);
        }

        void ResolveFreeAgentOffer(Player player, int wage, int years)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var sheet = transform.Find("TransferSheet");
            if (sheet != null) Destroy(sheet.gameObject);
            if (player == null || _career == null || _career.Club == null || !string.IsNullOrEmpty(player.ClubId)
                || GlobalTransferMarket.MovePending(_career, player.Id))
                return;
            var room = WeeklyWageRoom();
            var gap = Math.Max(0, wage - room);
            if (gap > 0)
            {
                if (gap <= 250000 && !NotebookHas("budget-live") && !DeskDue("budget", out _)
                    && !(DeskDue("budget-wait", out var waitUntil) && new DateTime(waitUntil).Date > _career.Calendar.CurrentDate.Date))
                {
                    OpenBoardMeeting(player, 0, wage, years, 0, gap);
                    return;
                }
                OpenTransferRefusal(player, english ? "The club cannot cover that weekly wage. No contract was signed." : "O clube não consegue suportar esse salário semanal. Não foi assinado nenhum contrato.");
                return;
            }

            var asked = Math.Max(250, player.WeeklyWage);
            var ratio = wage / (float)asked;
            var acceptance = ratio >= 1.12f ? 94 : ratio >= 1f ? 80 : ratio >= 0.92f ? 58 : 32;
            acceptance = Mathf.Clamp(acceptance + (player.Morale - 50) / 10, 10, 97);
            var seed = player.Id + "|free-agent|" + wage + "|" + years + "|" + _career.Calendar.CurrentDate.Ticks + "|" + (++_renewalAttempt);
            var roll = 0;
            unchecked { foreach (var c in seed) roll = roll * 31 + c; }
            if ((roll & 0x7fffffff) % 100 >= acceptance)
            {
                var counter = Mathf.Max(asked, Mathf.RoundToInt(wage * 1.1f));
                OpenTransferRefusal(player, english
                    ? player.FullName + " rejected the terms and countered at " + Euro(counter) + "/week. Nothing was charged."
                    : player.FullName + " recusou os termos e contrapropôs " + Euro(counter) + "/semana. Não foi debitado nada.");
                return;
            }

            if (!GlobalTransferMarket.SignFreeAgent(_career, player, _career.Club, wage, years))
            {
                OpenTransferRefusal(player, english ? "The player is no longer available or the wage budget changed. No contract was signed." : "O jogador já não está disponível ou o orçamento salarial mudou. Não foi assinado nenhum contrato.");
                return;
            }
            var text = english
                ? player.FullName + " accepted a " + years + "-year contract at " + Euro(wage) + " per week. No transfer fee was paid."
                : player.FullName + " aceitou um contrato de " + years + " anos por " + Euro(wage) + " por semana. Não foi pago qualquer valor de transferência.";
            Newsroom.Publish(_career, english ? "Transfer Journal" : "Diário de Transferências",
                (english ? "Free transfer · " : "Transferência livre · ") + player.FullName, text);
            PostDesk("✉ " + text);
            CareerStore.Save(_career);
            FillMarket();
            OpenModal(english ? "CONTRACT AGREED" : "CONTRATO ACORDADO", text, true);
            _modalActionLabel.text = english ? "DONE" : "CONCLUÍDO";
            _modalAction.onClick.RemoveAllListeners();
            _modalAction.onClick.AddListener(() => { CloseModal(); Refresh(); });
        }

        void OpenTransferNegotiation(Player player)
        {
            if (player == null || _career == null || player.ClubId == _career.Club.Id)
                return;
            if (string.IsNullOrEmpty(player.ClubId))
            {
                OpenFreeAgentNegotiation(player);
                return;
            }
            var previous = transform.Find("TransferSheet");
            if (previous != null)
                Destroy(previous.gameObject);
            var english = LocalizationManager.Current == GameLanguage.English;
            var sheet = BroadcastUi.Panel(transform, "TransferSheet", new Color(0.08f, 0.03f, 0.16f, 0.98f), true);
            BroadcastUi.Place(sheet.rectTransform, 0.04f, 0.08f, 0.96f, 0.96f);
            BroadcastUi.ApplySandPaper(sheet, 0.1f);
            sheet.transform.SetAsLastSibling();
            var close = BroadcastUi.Button(sheet.transform, "Close", "×", () => Destroy(sheet.gameObject), new Color(1f, 1f, 1f, 0.1f), Color.white, 20);
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.94f, 0.9f, 0.985f, 0.975f);

            var header = BroadcastUi.Panel(sheet.transform, "Header", new Color(0.12f, 0.05f, 0.24f, 1f), false);
            BroadcastUi.Place(header.rectTransform, 0.015f, 0.86f, 0.92f, 0.98f);
            var faceSlot = BroadcastUi.Zone(header.transform, "FaceSlot", 0.01f, 0.08f, 0.08f, 0.92f);
            FaceImage(faceSlot, player, 56f);
            var name = BroadcastUi.Label(header.transform, "Name", player.FullName, 22, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(name.rectTransform, 0.1f, 0.48f, 0.55f, 0.92f);
            var meta = BroadcastUi.Label(header.transform, "Meta", player.Age + (english ? " years  ·  " : " anos  ·  ") + ProfilePosition(player.Position, english), 14, new Color(0.78f, 0.8f, 0.88f), TextAnchor.MiddleLeft);
            BroadcastUi.Place(meta.rectTransform, 0.1f, 0.08f, 0.48f, 0.48f);
            _career.World.Clubs.TryGetValue(player.ClubId ?? "", out var seller);
            if (seller != null)
            {
                var crestSlot = BroadcastUi.Zone(header.transform, "Crest", 0.5f, 0.18f, 0.58f, 0.82f);
                CrestImage(crestSlot, seller, 36f);
                var clubName = BroadcastUi.Label(header.transform, "Club", seller.ShortName, 16, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(clubName.rectTransform, 0.6f, 0.18f, 0.98f, 0.82f);
            }

            var finance = _career.Club.Finance;
            var asking = GlobalTransferMarket.RequiredFee(_career, player);
            var budget = finance == null ? 0 : finance.TransferBudget;
            var balance = finance == null ? 0 : finance.Balance;
            var room = WeeklyWageRoom();
            var window = TransferWindowOpen();
            var sellerCanSell = seller != null && seller.PlayerIds != null && seller.PlayerIds.Count > 11;
            var boardWaitActive = DeskDue("budget-wait", out var boardWaitUntil)
                && new DateTime(boardWaitUntil).Date > _career.Calendar.CurrentDate.Date;
            var boardRequestPending = NotebookHas("budget-live") || DeskDue("budget", out _);

            var details = BroadcastUi.Panel(sheet.transform, "Details", new Color(0.1f, 0.04f, 0.2f, 1f), false);
            BroadcastUi.Place(details.rectTransform, 0.015f, 0.2f, 0.4f, 0.84f);
            SectionTitle(details.transform, english ? "THE DEAL" : "O NEGÓCIO");
            ContractRow(details.transform, "Ask", english ? "Club's asking price" : "Pedido do clube", Euro(asking), 0.78f);
            ContractRow(details.transform, "Budget", english ? "Transfer budget" : "Orçamento de transferências", Euro(budget), 0.66f);
            ContractRow(details.transform, "Balance", english ? "Club balance" : "Saldo do clube", Euro(balance), 0.54f);
            ContractRow(details.transform, "Wages", english ? "Weekly wage room" : "Folga na massa salarial", room >= int.MaxValue / 2 ? "—" : Euro(room) + (english ? " /wk" : " /sem"), 0.42f);
            ContractRow(details.transform, "Window", english ? "Market" : "Mercado", window ? (english ? "Open" : "Aberto") : (english ? "Closed" : "Fechado"), 0.3f);
            ContractRow(details.transform, "Seller", english ? "Selling club" : "Clube vendedor", sellerCanSell ? (english ? "Can sell" : "Pode vender") : (english ? "Squad would drop below 11" : "O plantel ficava abaixo de 11"), 0.18f);
            var rule = BroadcastUi.Label(details.transform, "Rule", english
                ? "Nothing is signed, and nothing leaves the budget, until a proposal is accepted."
                : "Nada se assina e nada sai do orçamento enquanto uma proposta não for aceite.", 13, new Color(0.78f, 0.8f, 0.88f), TextAnchor.UpperLeft);
            rule.horizontalOverflow = HorizontalWrapMode.Wrap;
            BroadcastUi.Place(rule.rectTransform, 0.05f, 0.02f, 0.95f, 0.14f);

            var middle = BroadcastUi.Panel(sheet.transform, "Middle", new Color(0.1f, 0.04f, 0.2f, 1f), false);
            BroadcastUi.Place(middle.rectTransform, 0.41f, 0.2f, 0.67f, 0.84f);
            SectionTitle(middle.transform, english ? "WHAT THE MOVE COSTS" : "O QUE A MUDANÇA CUSTA");
            var currentWage = Math.Max(250, player.WeeklyWage);
            ContractRow(middle.transform, "Now", english ? "Wage at his club" : "Salário no clube dele", Euro(currentWage) + (english ? " /wk" : " /sem"), 0.74f);
            ContractRow(middle.transform, "Low", english ? "Below the asking price" : "Abaixo do pedido", Euro(Mathf.RoundToInt(asking * 0.82f)), 0.6f);
            ContractRow(middle.transform, "Level", english ? "The asking price" : "O pedido", Euro(asking), 0.48f);
            ContractRow(middle.transform, "High", english ? "To get it done" : "Para fechar já", Euro(Mathf.RoundToInt(asking * 1.12f)), 0.36f);
            var note = BroadcastUi.Label(middle.transform, "Note", english
                ? "A low fee is often refused. The wage has to fit the wage bill as well as the fee fitting the budget and the balance."
                : "Um valor baixo é recusado muitas vezes. O salário tem de caber na massa salarial, e o valor no orçamento e no saldo.", 13, Color.white, TextAnchor.UpperLeft);
            note.horizontalOverflow = HorizontalWrapMode.Wrap;
            BroadcastUi.Place(note.rectTransform, 0.05f, 0.06f, 0.95f, 0.28f);

            var right = BroadcastUi.Panel(sheet.transform, "Right", new Color(0.1f, 0.04f, 0.2f, 1f), false);
            BroadcastUi.Place(right.rectTransform, 0.68f, 0.2f, 0.985f, 0.84f);
            SectionTitle(right.transform, english ? "AFTER A YES" : "SE DISSEREM QUE SIM");
            ContractRow(right.transform, "Fee", english ? "Fee leaves both the budget and the balance" : "O valor sai do orçamento e do saldo", "", 0.7f);
            ContractRow(right.transform, "Pay", english ? "The agreed wage starts that week" : "O salário acordado começa nessa semana", "", 0.56f);
            ContractRow(right.transform, "Years", english ? "Contract length is part of the offer" : "A duração entra na proposta", "", 0.42f);
            var block = !window ? (english ? "The window is closed. January, July and August." : "O mercado está fechado. Janeiro, julho e agosto.")
                : !sellerCanSell ? (english ? "The selling club cannot go below 11 players." : "O clube vendedor não pode ficar com menos de 11.")
                : budget <= 0 ? (english ? "There is no transfer budget." : "Não há orçamento de transferências.")
                : (english ? "Pick an offer the club can actually pay." : "Escolhe uma proposta que o clube consiga pagar.");
            var blockLabel = BroadcastUi.Label(right.transform, "Block", block, 14, Color.white, TextAnchor.UpperLeft);
            blockLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            BroadcastUi.Place(blockLabel.rectTransform, 0.05f, 0.08f, 0.95f, 0.32f);

            var fees = new[] { Mathf.Max(80000, Mathf.RoundToInt(asking * 0.82f)), asking, Mathf.RoundToInt(asking * 1.12f) };
            var wages = new[] { currentWage, Mathf.Max(currentWage, Mathf.RoundToInt(currentWage * 1.08f)), Mathf.Max(currentWage, Mathf.RoundToInt(currentWage * 1.22f)) };
            var terms = new[] { 2, 3, 4 };
            var titles = english ? new[] { "Below the price", "The asking price", "Get it done" } : new[] { "Abaixo do valor", "O pedido do clube", "Para fechar já" };
            for (var i = 0; i < fees.Length; i++)
            {
                var fee = fees[i];
                var wage = wages[i];
                var years = terms[i];
                var wageFits = room >= int.MaxValue / 2 || wage <= room;
                var cashFits = finance != null && fee <= budget && fee <= balance;
                var affordable = window && sellerCanSell && cashFits && wageFits;
                var shortfall = finance == null ? fee : Math.Max(0, fee - Math.Min(budget, balance));
                var wageShortfall = room >= int.MaxValue / 2 ? 0 : Math.Max(0, wage - room);
                var canAskBoard = !affordable && window && sellerCanSell
                    && !boardWaitActive && !boardRequestPending
                    && ((shortfall > 0 && shortfall <= 12000000) || (wageShortfall > 0 && wageShortfall <= 250000));
                var x0 = 0.015f + i * 0.27f;
                var card = BroadcastUi.Panel(sheet.transform, "Offer" + i, new Color(0.14f, 0.06f, 0.28f, 1f), true);
                BroadcastUi.Place(card.rectTransform, x0, 0.02f, x0 + 0.25f, 0.17f);
                var heading = BroadcastUi.Label(card.transform, "H", titles[i], 13, new Color(0.85f, 0.8f, 0.95f), TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(heading.rectTransform, 0.06f, 0.68f, 0.94f, 0.94f);
                var money = BroadcastUi.Label(card.transform, "M", Euro(fee) + "   ·   " + Euro(wage) + (english ? "/wk" : "/sem") + "   ·   " + years + (english ? " yrs" : " anos"), 12, Color.white, TextAnchor.MiddleLeft);
                BroadcastUi.Place(money.rectTransform, 0.06f, 0.38f, 0.94f, 0.66f);
                var capturedFee = fee;
                var capturedWage = wage;
                var capturedYears = years;
                var captured = player;
                var capturedGap = shortfall;
                var capturedWageGap = wageShortfall;
                var buttonLabel = affordable
                    ? (english ? "PROPOSE" : "PROPOR")
                    : canAskBoard
                        ? (english ? "ASK THE BOARD" : "PEDIR VERBA")
                        : (english ? "CANNOT PAY" : "SEM VERBA");
                var button = BroadcastUi.Button(card.transform, "Go", buttonLabel, () =>
                {
                    if (affordable)
                    {
                        ResolveTransferOffer(captured, capturedFee, capturedWage, capturedYears);
                        return;
                    }
                    if (canAskBoard)
                    {
                        var sheetOpen = transform.Find("TransferSheet");
                        if (sheetOpen != null)
                            Destroy(sheetOpen.gameObject);
                        OpenBoardMeeting(captured, capturedFee, capturedWage, capturedYears, capturedGap, capturedWageGap);
                        return;
                    }
                    OpenTransferRefusal(captured, english ? "This offer does not fit the budget, the balance or the wage bill. Nothing was charged." : "Esta proposta não cabe no orçamento, no saldo ou na massa salarial. Não foi debitado nada.");
                }, affordable || canAskBoard ? new Color(0.85f, 0.15f, 0.55f, 1f) : new Color(0.25f, 0.12f, 0.16f, 1f), Color.white, 13);
                BroadcastUi.Place(button.GetComponent<RectTransform>(), 0.06f, 0.08f, 0.94f, 0.34f);
            }
        }

        void OpenTransferRefusal(Player player, string reason)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var sheet = transform.Find("TransferSheet");
            if (sheet != null)
                Destroy(sheet.gameObject);
            OpenModal(english ? "No deal" : "Sem acordo", player == null ? "" : player.FullName, true);
            AddLine(reason);
            _modalActionLabel.text = english ? "BACK" : "VOLTAR";
            _modalAction.onClick.RemoveAllListeners();
            _modalAction.onClick.AddListener(() =>
            {
                CloseModal();
                if (player != null)
                    OpenTransferNegotiation(player);
            });
        }

        void ResolveTransferOffer(Player player, int fee, int wage, int years)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var sheet = transform.Find("TransferSheet");
            if (sheet != null)
                Destroy(sheet.gameObject);
            if (player == null || _career == null || player.ClubId == _career.Club.Id || !_career.ScoutedPlayers.Contains(player.Id))
                return;
            if (!TransferWindowOpen())
            {
                OpenTransferRefusal(player, english ? "The window is closed. Nothing was charged." : "O mercado está fechado. Não foi debitado nada.");
                return;
            }
            if (!_career.World.Clubs.TryGetValue(player.ClubId ?? "", out var seller) || seller.PlayerIds.Count <= 11)
            {
                OpenTransferRefusal(player, english ? "The selling club would drop below 11 players. Nothing was charged." : "O clube vendedor ficava com menos de 11 jogadores. Não foi debitado nada.");
                return;
            }
            var finance = _career.Club.Finance;
            var room = WeeklyWageRoom();
            var wageFits = room >= int.MaxValue / 2 || wage <= room;
            var available = finance == null ? 0 : Math.Min(finance.TransferBudget, finance.Balance);
            var gap = Math.Max(0, fee - available);
            var wageGap = room >= int.MaxValue / 2 ? 0 : Math.Max(0, wage - room);
            if (finance == null || !wageFits || gap > 0)
            {
                var canCoverFee = gap == 0 || gap <= 12000000;
                var canCoverWages = wageGap == 0 || wageGap <= 250000;
                if (finance != null && canCoverFee && canCoverWages && (gap > 0 || wageGap > 0))
                {
                    if (DeskDue("budget-wait", out var wait) && new DateTime(wait).Date > _career.Calendar.CurrentDate.Date)
                    {
                        OpenTransferRefusal(player, english
                            ? "The board already answered a money request. The next one is on " + LocalizationManager.FormatDate(new DateTime(wait), "d MMM") + "."
                            : "A direção já respondeu a um pedido de dinheiro. O próximo é a " + LocalizationManager.FormatDate(new DateTime(wait), "d MMM") + ".");
                        return;
                    }
                    OpenBoardMeeting(player, fee, wage, years, gap, wageGap);
                    return;
                }
                OpenTransferRefusal(player, !wageFits
                    ? (english ? "The wage does not fit the wage budget. The board will not raise it in this meeting." : "O salário não cabe no orçamento salarial. A direção não mexe nisso nesta reunião.")
                    : (english ? "The club cannot pay this fee and wage. Nothing was charged." : "O clube não consegue pagar este valor e este salário. Não foi debitado nada."));
                return;
            }
            var asking = Mathf.Max(1, GlobalTransferMarket.RequiredFee(_career, player));
            var ratio = fee / (float)asking;
            var acceptance = ratio >= 1.08f ? 90 : ratio >= 0.98f ? 72 : ratio >= 0.9f ? 38 : 14;
            acceptance = Mathf.Clamp(acceptance + (player.Morale - 50) / 8, 8, 94);
            var seed = player.Id + "|" + fee + "|" + wage + "|" + _career.Calendar.CurrentDate.Ticks + "|" + (++_renewalAttempt);
            var roll = 0;
            unchecked { foreach (var c in seed) roll = roll * 31 + c; }
            if ((roll & 0x7fffffff) % 100 >= acceptance)
            {
                OpenTransferRefusal(player, english
                    ? "The selling club refused " + Euro(fee) + ". The budget is unchanged."
                    : "O clube vendedor recusou " + Euro(fee) + ". O orçamento mantém-se.");
                return;
            }
            var arrival = _career.Calendar.CurrentDate.Date.AddDays(5);
            if (!GlobalTransferMarket.ScheduleMove(_career, player, _career.Club, fee, years, wage, "in", 5))
            {
                OpenTransferRefusal(player, english ? "The deal did not go through. Nothing was charged." : "O negócio não avançou. Não foi debitado nada.");
                return;
            }
            _career.Targets.Remove(player.Id);
            CareerStore.Save(_career);
            OpenModal(english ? "Deal agreed" : "Acordo alcançado", player.FullName, true);
            AddLine(english
                ? player.FullName + " stays at " + seller.Name + " until " + LocalizationManager.FormatDate(arrival, "d MMM") + ". The fee is only paid when he arrives."
                : player.FullName + " continua no " + seller.Name + " até " + LocalizationManager.FormatDate(arrival, "d MMM") + ". O valor só sai quando ele chegar.");
            _modalActionLabel.text = english ? "DONE" : "CONCLUÍDO";
            _modalAction.onClick.RemoveAllListeners();
            _modalAction.onClick.AddListener(() => { CloseModal(); Refresh(); });
        }

        Club SaleBuyer(Player player, out int offerValue)
        {
            offerValue = 0;
            if (player == null || _career == null || _career.Club.PlayerIds.Count <= 11)
                return null;
            offerValue = Mathf.Max(80000, Mathf.RoundToInt(MarketValue(player) * 0.82f));
            Club best = null;
            var bestScore = int.MaxValue;
            foreach (var club in _career.World.Clubs.Values)
            {
                if (club == null || club.Id == _career.Club.Id || club.Finance == null || club.Finance.TransferBudget < offerValue)
                    continue;
                var score = Mathf.Abs(club.Reputation - _career.Club.Reputation) * 4
                    + Mathf.Abs(club.Reputation - PlayerAttributes.Overall(player));
                if (score >= bestScore)
                    continue;
                best = club;
                bestScore = score;
            }
            if (best == null)
                offerValue = 0;
            return best;
        }

        void CompletePlayerSale(Player player, Club buyer, int fee)
        {
            if (player == null || buyer == null || _career == null || player.ClubId != _career.Club.Id
                || !_career.Listed.Contains(player.Id) || _career.Club.PlayerIds.Count <= 11
                || buyer.Finance == null || buyer.Finance.TransferBudget < fee)
                return;

            var years = player.ContractEndTicks > _career.Calendar.CurrentDate.Ticks
                ? Mathf.Clamp(new DateTime(player.ContractEndTicks).Year - _career.Calendar.CurrentDate.Year, 1, 5) : 1;
            var arrival = _career.Calendar.CurrentDate.Date.AddDays(4);
            if (!GlobalTransferMarket.ScheduleMove(_career, player, buyer, fee, years, player.WeeklyWage, "out", 4))
                return;
            _career.Listed.Remove(player.Id);
            CareerStore.Save(_career);
            CloseModal();
            FillTeam();
            FillMarket();
            var english = LocalizationManager.Current == GameLanguage.English;
            CareerNotificationQueue.Enqueue(english ? "TRANSFER AGREED" : "TRANSFERÊNCIA ACORDADA",
                english ? player.FullName + " stays until " + LocalizationManager.FormatDate(arrival, "d MMM") + ". He joins " + buyer.Name + " that day."
                    : player.FullName + " continua no plantel até " + LocalizationManager.FormatDate(arrival, "d MMM") + ". Nesse dia segue para o " + buyer.Name + ".");
            return;
        }

        void OpenPlayerNote(Player player)
        {
            OpenModal(player.FullName, "Nota tua. O staff continua com a leitura dele.", false);
            var field = UiFactory.Input(_modalBody, "A tua nota");
            BroadcastUi.Preferred(field.GetComponent<RectTransform>(), 48);
            field.text = UserNote(player.Id);
            var save = BroadcastUi.Button(_modalBody, "SaveNote", "GUARDAR NOTA", () =>
            {
                SetUserNote(player.Id, field.text);
                CloseModal();
                FillSquadList();
            }, UiTheme.Accent, UiTheme.ButtonText, 16);
            BroadcastUi.Preferred(save.GetComponent<RectTransform>(), 42);
        }

        string UserNote(string playerId)
        {
            foreach (var note in _career.PlayerNotes)
            {
                if (note.PlayerId == playerId)
                    return note.Text;
            }

            return "";
        }

        void SetUserNote(string playerId, string text)
        {
            foreach (var note in _career.PlayerNotes)
            {
                if (note.PlayerId != playerId)
                    continue;
                note.Text = text;
                return;
            }

            _career.PlayerNotes.Add(new PlayerNote { PlayerId = playerId, Text = text });
        }

        static StaffMember Member(Club club, StaffRole role)
        {
            if (club == null || club.Staff == null)
                return null;
            foreach (var member in club.Staff)
            {
                if (member.Role == role)
                    return member;
            }

            return null;
        }

        static void EnsureScout(Club club)
        {
            if (club == null || club.Staff == null || Member(club, StaffRole.Scout) != null)
                return;
            club.Staff.Add(new StaffMember
            {
                Id = club.Id + ":olheiro",
                ClubId = club.Id,
                FullName = "Olheiro do clube",
                Role = StaffRole.Scout,
                Judgement = StaffProfile.Spread(club.Id + ":olheiro", StaffRole.Scout, club.Reputation)
            });
        }

        static void VaryStaff(Club club)
        {
            if (club == null || club.Staff == null || club.Staff.Count == 0)
                return;
            var lowest = 99;
            var highest = 0;
            foreach (var member in club.Staff)
            {
                if (member == null)
                    continue;
                if (member.Judgement < lowest)
                    lowest = member.Judgement;
                if (member.Judgement > highest)
                    highest = member.Judgement;
            }

            if (highest - lowest > 4)
                return;
            foreach (var member in club.Staff)
            {
                if (member == null)
                    continue;
                member.Judgement = StaffProfile.Spread(member.Id, member.Role, club.Reputation);
            }
        }

        void SelectPane(int pane)
        {
            _pane = pane;
            if (pane != 1)
                _arm = null;
            if (_tacHost != null)
                _tacHost.SetActive(pane == 3);
            if (_sheet != null)
                _sheet.gameObject.SetActive(pane != 3);
            if (pane == 3)
                FillTactics();
            FillSheet();
        }

        void FillTactics()
        {
            if (_tacticsBody == null)
                return;
            Clear(_tacticsBody);
            var t = _career.Club.Tactics;
            var presetMatch = TacticPresets.MatchingPresetIndex(t);
            var presetName = presetMatch == 2 ? "Gegenpressing" : presetMatch < 0 ? TacticPresets.CustomName : TacticPresets.Names[Mathf.Clamp(presetMatch, 0, TacticPresets.Names.Length - 1)];
            var english = LocalizationManager.Current == GameLanguage.English;
            var summary = BroadcastUi.Label(_tacticsBody, "Summary",
                LocalizationManager.Translate(presetName) + "\n" + t.Formation + "\n"
                    + LocalizationManager.Translate(t.Pressing) + " · " + LocalizationManager.Translate(t.DefensiveLine),
                13, Color.white, TextAnchor.UpperLeft);
            summary.horizontalOverflow = HorizontalWrapMode.Wrap;
            BroadcastUi.Place(summary.rectTransform, 0.04f, 0.42f, 0.96f, 0.96f);
            var open = BroadcastUi.Button(_tacticsBody, "OpenSheet", english ? "EDIT TACTIC" : "EDITAR TÁTICA", OpenTacticsSheet, UiTheme.Accent, UiTheme.ButtonText, 14);
            BroadcastUi.Place(open.GetComponent<RectTransform>(), 0.04f, 0.08f, 0.96f, 0.32f);
        }

        void ShowTacticChoices(string title, string help, string[] options, string current, Action<string> pick, Transform host = null)
        {
            var parent = host != null ? host : _tacticsBody;
            if (parent == null)
                return;
            var previous = parent.Find("Choices");
            if (previous != null)
                Destroy(previous.gameObject);
            var layer = BroadcastUi.Zone(parent, "Choices", 0f, 0f, 1f, 1f);
            var dim = BroadcastUi.Solid(layer, "Dim", new Color(0f, 0f, 0f, 0f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            BroadcastUi.Catch(dim, () => Destroy(layer.gameObject));
            var card = BroadcastUi.Panel(layer, "Card", new Color(0.12f, 0.05f, 0.24f, 0.98f), true);
            BroadcastUi.Place(card.rectTransform, 0.02f, 0.04f, 0.98f, 0.96f);
            var head = BroadcastUi.Label(card.transform, "Title", title.ToUpperInvariant(), 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(head.rectTransform, 0.06f, 0.86f, 0.78f, 0.97f);
            var note = BroadcastUi.Label(card.transform, "Help", help, 13, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.Place(note.rectTransform, 0.06f, 0.74f, 0.94f, 0.86f);
            var room = 0.68f;
            var step = Mathf.Min(0.1f, room / Mathf.Max(1, options.Length));
            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var on = option == current;
                var y1 = 0.7f - i * step;
                var row = BroadcastUi.Button(card.transform, "Opt" + i, option, () =>
                {
                    pick(option);
                    _career.Club.Tactics.Summary = _career.Club.Tactics.Formation + " · " + _career.Club.Tactics.Pressing + " · " + _career.Club.Tactics.BuildUp;
                    if (_teamTitle != null)
                        _teamTitle.text = "PLANTEL  ·  " + _career.Club.Tactics.Formation;
                    if (host != null)
                        OpenTacticsSheet();
                    else
                        FillTactics();
                }, on ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), on ? UiTheme.ButtonText : Color.white, 14);
                BroadcastUi.Place(row.GetComponent<RectTransform>(), 0.06f, y1 - step + 0.008f, 0.94f, y1 - 0.004f);
            }
        }

        void OpenTacticsSheet()
        {
            var previous = transform.Find("TacticsSheet");
            if (previous != null)
                Destroy(previous.gameObject);
            var contractOpen = transform.Find("ContractSheet");
            if (contractOpen != null)
                Destroy(contractOpen.gameObject);
            var sheet = BroadcastUi.Solid(transform, "TacticsSheet", new Color(0.08f, 0.025f, 0.16f, 0.995f), true);
            BroadcastUi.Place(sheet.rectTransform, 0.015f, 0.105f, 0.985f, 0.985f);
            BroadcastUi.ApplySandPaper(sheet);
            var tacticToolbar = BroadcastUi.Panel(sheet.transform, "TacticToolbar", new Color(0.13f, 0.08f, 0.27f, 1f), false);
            BroadcastUi.Place(tacticToolbar.rectTransform, 0.006f, 0.92f, 0.994f, 0.998f);
            sheet.transform.SetAsLastSibling();
            if (_career.Club.Tactics == null)
                _career.Club.Tactics = new TacticalIdentity();
            var t = _career.Club.Tactics;
            if (string.IsNullOrEmpty(t.Formation))
                t.Formation = "4-3-3";
            if (string.IsNullOrEmpty(_tacticSelection) || _tacticSelection == "preset")
                _tacticSelection = "home";

            var englishTactics = LocalizationManager.Current == GameLanguage.English;
            var title = BroadcastUi.Label(sheet.transform, "Title", englishTactics ? "TACTICS" : "TÁTICA", 18, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.015f, 0.925f, 0.12f, 0.99f);
            var forms = new[] { "4-3-3", "4-2-3-1", "4-4-2", "3-4-3", "5-4-1" };
            for (var i = 0; i < forms.Length; i++)
            {
                var formation = forms[i];
                var on = t.Formation == formation;
                var x0 = 0.13f + i * 0.1f;
                var formButton = BroadcastUi.Button(sheet.transform, "Form" + i, formation, () =>
                    CommitTactic(item => item.Formation = formation),
                    on ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), on ? UiTheme.ButtonText : Color.white, 13);
                BroadcastUi.Place(formButton.GetComponent<RectTransform>(), x0, 0.93f, x0 + 0.092f, 0.985f);
            }
            var close = BroadcastUi.Button(sheet.transform, "Close", "×", () => Destroy(sheet.gameObject), new Color(1f, 1f, 1f, 0.12f), Color.white, 20);
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.94f, 0.93f, 0.985f, 0.985f);

            var presetIndex = new[] { 0, 2, 1, 3, 4 };
            var presetLabels = new[] { "Equilíbrio", "Gegenpressing", "Jogo pelas alas", "Contra-ataque", "Posse" };
            var activePreset = TacticPresets.MatchingPresetIndex(t);
            for (var i = 0; i < presetIndex.Length; i++)
            {
                var index = presetIndex[i];
                var on = activePreset == index;
                var x0 = 0.015f + i * 0.16f;
                var chip = BroadcastUi.Button(sheet.transform, "Preset" + i, LocalizationManager.Translate(presetLabels[i]) + (on ? "  ✓" : ""), () =>
                {
                    _tacticSelection = "xi";
                    CommitTactic(item => TacticPresets.Apply(item, index));
                }, on ? UiTheme.Accent : new Color(0.22f, 0.12f, 0.38f, 1f), on ? UiTheme.ButtonText : Color.white, 13);
                var chipText = chip.GetComponentInChildren<Text>();
                if (chipText != null)
                {
                    chipText.resizeTextForBestFit = true;
                    chipText.resizeTextMinSize = 10;
                    chipText.resizeTextMaxSize = 13;
                }
                BroadcastUi.Place(chip.GetComponent<RectTransform>(), x0, 0.855f, x0 + 0.15f, 0.915f);
            }
            if (activePreset < 0)
            {
                var custom = BroadcastUi.Label(sheet.transform, "Custom",
                    LocalizationManager.Current == GameLanguage.English ? "ACTIVE TACTIC  ·  CUSTOM" : "TÁTICA ATIVA  ·  PERSONALIZADA",
                    12, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(custom.rectTransform, 0.82f, 0.855f, 0.98f, 0.915f);
            }

            var savedCount = Mathf.Min(5, _career.SavedTactics.Count);
            for (var i = 0; i < savedCount; i++)
            {
                var savedIndex = i;
                var saved = _career.SavedTactics[i];
                if (saved == null || saved.Tactics == null) continue;
                var x0 = 0.015f + i * 0.112f;
                var savedButton = BroadcastUi.Button(sheet.transform, "SavedTactic" + i,
                    (englishTactics ? "SAVED " : "GUARDADA ") + (i + 1) + "  ·  " + saved.Tactics.Formation,
                    () => ApplySavedTactic(savedIndex), new Color(1f, 1f, 1f, 0.12f), Color.white, 11);
                var savedLabel = savedButton.GetComponentInChildren<Text>();
                if (savedLabel != null) { savedLabel.resizeTextForBestFit = true; savedLabel.resizeTextMinSize = 8; savedLabel.resizeTextMaxSize = 11; }
                BroadcastUi.Place(savedButton.GetComponent<RectTransform>(), x0, 0.78f, x0 + 0.105f, 0.84f);
            }
            var playerRoles = BroadcastUi.Button(sheet.transform, "PlayerRoles",
                englishTactics ? "PLAYER ROLES" : "FUNÇÕES DOS JOGADORES", () =>
                {
                    _tacticSelection = "xi";
                    OpenTacticsSheet();
                }, _tacticSelection == "xi" || _tacticSelection.StartsWith("player:")
                    ? UiTheme.Accent : new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 11);
            var rolesLabel = playerRoles.GetComponentInChildren<Text>();
            if (rolesLabel != null) { rolesLabel.resizeTextForBestFit = true; rolesLabel.resizeTextMinSize = 8; rolesLabel.resizeTextMaxSize = 11; }
            BroadcastUi.Place(playerRoles.GetComponent<RectTransform>(), 0.59f, 0.78f, 0.75f, 0.84f);
            var saveTactic = BroadcastUi.Button(sheet.transform, "SaveTactic",
                englishTactics ? "SAVE CUSTOM" : "GUARDAR PERSONALIZADA", SaveCurrentTactic,
                new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 11);
            var saveLabel = saveTactic.GetComponentInChildren<Text>();
            if (saveLabel != null) { saveLabel.resizeTextForBestFit = true; saveLabel.resizeTextMinSize = 8; saveLabel.resizeTextMaxSize = 11; }
            BroadcastUi.Place(saveTactic.GetComponent<RectTransform>(), 0.77f, 0.78f, 0.985f, 0.84f);

            var eleven = new List<Player>();
            var bench = new List<Player>();
            StartingShape.Resolve(Squad(_career.Club), _career.Club, eleven, bench);
            var positions = StartingShape.SlotPositions(t.Formation);
            Player picked = null;
            var pickedSlot = -1;
            if (_tacticSelection.StartsWith("player:"))
            {
                var id = _tacticSelection.Substring("player:".Length);
                for (var i = 0; i < eleven.Count && i < 11; i++)
                {
                    if (eleven[i].Id != id)
                        continue;
                    picked = eleven[i];
                    pickedSlot = i;
                    break;
                }
                if (picked == null)
                    _tacticSelection = "home";
            }

            var pitch = BroadcastUi.Panel(sheet.transform, "PitchCard", new Color(0.07f, 0.28f, 0.13f, 1f), true);
            BroadcastUi.Place(pitch.rectTransform, 0.012f, 0.02f, 0.3f, 0.75f);
            var center = BroadcastUi.Panel(sheet.transform, "Center", new Color(0.08f, 0.09f, 0.12f, 1f), true);
            BroadcastUi.Place(center.rectTransform, 0.31f, 0.02f, 0.8f, 0.75f);
            var overview = BroadcastUi.Panel(sheet.transform, "Overview", new Color(0.07f, 0.08f, 0.11f, 1f), true);
            BroadcastUi.Place(overview.rectTransform, 0.81f, 0.02f, 0.988f, 0.75f);
            PaintTacticPitch(pitch.transform, t, pickedSlot, eleven, picked);

            if (picked != null)
                PaintPlayerDuties(center.transform, overview.transform, picked, pickedSlot < positions.Length ? positions[pickedSlot] : picked.Position);
            else if (_tacticSelection == "xi")
                PaintXiList(center.transform, overview.transform, eleven, positions, t);
            else if (_tacticSelection != "home")
                PaintTacticOptions(center.transform, overview.transform, t, _tacticSelection);
            else
                PaintTacticHome(center.transform, overview.transform, t);
        }

        void PaintTacticHome(Transform center, Transform overview, TacticalIdentity t)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var heading = BroadcastUi.Label(center, "InstructionHeading", english ? "TEAM INSTRUCTIONS" : "INSTRUÇÕES DA EQUIPA", 17, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.035f, 0.9f, 0.96f, 0.98f);
            var inButton = BroadcastUi.Button(center, "InPossession", english ? "IN POSSESSION" : "COM BOLA", () =>
            {
                _tacticPhase = 0;
                OpenTacticsSheet();
            }, _tacticPhase == 0 ? UiTheme.Accent : new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 12);
            BroadcastUi.Place(inButton.GetComponent<RectTransform>(), 0.035f, 0.81f, 0.49f, 0.885f);
            var outButton = BroadcastUi.Button(center, "OutPossession", english ? "OUT OF POSSESSION" : "SEM BOLA", () =>
            {
                _tacticPhase = 1;
                OpenTacticsSheet();
            }, _tacticPhase == 1 ? UiTheme.Accent : new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 12);
            BroadcastUi.Place(outButton.GetComponent<RectTransform>(), 0.51f, 0.81f, 0.965f, 0.885f);

            var keys = _tacticPhase == 0
                ? new[] { "pass", "tempo", "width", "build", "backs", "idea" }
                : new[] { "press", "line", "mark" };
            for (var i = 0; i < keys.Length; i++)
            {
                string title;
                string[] options;
                string current;
                TacticCategory(keys[i], t, out title, out options, out current);
                var key = keys[i];
                var column = i % 3;
                var row = i / 3;
                var x0 = 0.035f + column * 0.315f;
                var y1 = 0.77f - row * 0.32f;
                TacticTile(center, "Instruction" + key, LocalizationManager.Translate(title), LocalizationManager.Translate(current), TacticSymbol(key), false,
                    () =>
                    {
                        _tacticSelection = key;
                        OpenTacticsSheet();
                    }, x0, y1 - 0.285f, x0 + 0.295f, y1);
            }
            PaintTacticOverview(overview, t, null, null, null);
        }

        void PaintTacticOptions(Transform center, Transform overview, TacticalIdentity t, string key)
        {
            string title;
            string[] options;
            string current;
            TacticCategory(key, t, out title, out options, out current);
            var english = LocalizationManager.Current == GameLanguage.English;
            var back = BroadcastUi.Button(center, "BackToInstructions", english ? "‹  TEAM INSTRUCTIONS" : "‹  INSTRUÇÕES", () =>
            {
                _tacticSelection = "home";
                OpenTacticsSheet();
            }, new Color(0.15f, 0.17f, 0.21f, 1f), Color.white, 11);
            BroadcastUi.Place(back.GetComponent<RectTransform>(), 0.68f, 0.9f, 0.965f, 0.98f);
            var heading = BroadcastUi.Label(center, "OptionHeading", LocalizationManager.Translate(title), 17, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.035f, 0.9f, 0.66f, 0.98f);
            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var column = i % 3;
                var row = i / 3;
                var x0 = 0.035f + column * 0.315f;
                var y1 = 0.82f - row * 0.235f;
                TacticTile(center, "Option" + i, LocalizationManager.Translate(title), LocalizationManager.Translate(option), TacticSymbol(key), option == current,
                    () => ApplyTacticOption(key, option), x0, y1 - 0.21f, x0 + 0.295f, y1);
            }
            PaintTacticOverview(overview, t, LocalizationManager.Translate(title), LocalizationManager.Translate(current),
                TacticBlurb(current, LocalizationManager.Current == GameLanguage.English));
        }

        void PaintXiList(Transform center, Transform overview, List<Player> eleven, PlayerPosition[] positions, TacticalIdentity t)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var back = BroadcastUi.Button(center, "BackToInstructions", english ? "‹  TEAM INSTRUCTIONS" : "‹  INSTRUÇÕES", () =>
            {
                _tacticSelection = "home";
                OpenTacticsSheet();
            }, new Color(0.15f, 0.17f, 0.21f, 1f), Color.white, 11);
            BroadcastUi.Place(back.GetComponent<RectTransform>(), 0.68f, 0.9f, 0.965f, 0.98f);
            var heading = BroadcastUi.Label(center, "PlayerRolesHeading", english ? "PLAYER ROLES" : "FUNÇÕES DOS JOGADORES", 17, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.035f, 0.9f, 0.66f, 0.98f);
            var count = Mathf.Min(11, eleven.Count);
            for (var i = 0; i < count; i++)
            {
                var player = eleven[i];
                var column = i % 3;
                var row = i / 3;
                var x0 = 0.035f + column * 0.315f;
                var y1 = 0.85f - row * 0.18f;
                var position = i < positions.Length ? positions[i] : player.Position;
                var captured = player;
                var duty = string.IsNullOrEmpty(player.Duty) ? IndividualOrders.For(position, StableOfferRoll(player.Id ?? player.FullName)) : player.Duty;
                TacticTile(center, "Xi" + i, player.SquadNumber + "  ·  " + BroadcastUi.ShortName(player.FullName),
                    LocalizationManager.Translate(PresentationLabels.Position(position)) + "  ·  " + LocalizationManager.Translate(duty), "●", !string.IsNullOrEmpty(duty),
                    () =>
                    {
                        _tacticSelection = "player:" + captured.Id;
                        OpenTacticsSheet();
                    }, x0, y1 - 0.16f, x0 + 0.295f, y1);
            }
            PaintTacticOverview(overview, t, english ? "PLAYER ROLES" : "FUNÇÕES", null,
                english ? "Select a player card on the pitch or here to choose the role and see its key attributes." : "Seleciona um jogador no campo ou aqui para escolher a função e consultar os atributos-chave.");
        }

        void PaintPlayerDuties(Transform center, Transform overview, Player player, PlayerPosition position)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var options = IndividualOrders.ForPosition(position);
            var back = BroadcastUi.Button(center, "BackToPlayers", english ? "‹  PLAYERS" : "‹  JOGADORES", () =>
            {
                _tacticSelection = "xi";
                OpenTacticsSheet();
            }, new Color(0.15f, 0.17f, 0.21f, 1f), Color.white, 11);
            BroadcastUi.Place(back.GetComponent<RectTransform>(), 0.68f, 0.9f, 0.965f, 0.98f);
            var heading = BroadcastUi.Label(center, "DutyHeading", english ? "INDIVIDUAL ROLE" : "FUNÇÃO INDIVIDUAL", 17, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.035f, 0.9f, 0.66f, 0.98f);
            var columns = 3;
            var rows = Mathf.CeilToInt(options.Length / (float)columns);
            var cardGap = 0.02f;
            var cardHeight = Mathf.Min(0.32f, (0.76f - Mathf.Max(0, rows - 1) * cardGap) / Mathf.Max(1, rows));
            var rowStep = cardHeight + cardGap;
            for (var i = 0; i < options.Length; i++)
            {
                var duty = options[i];
                var column = i % columns;
                var row = i / columns;
                var x0 = 0.035f + column * 0.315f;
                var y1 = 0.84f - row * rowStep;
                var captured = player;
                var chosen = duty;
                TacticTile(center, "Duty" + i, english ? "ROLE" : "FUNÇÃO", LocalizationManager.Translate(duty), TacticSymbol("duty"), player.Duty == duty,
                    () =>
                    {
                        captured.Duty = chosen;
                        _tacticSelection = "player:" + captured.Id;
                        OpenTacticsSheet();
                    }, x0, y1 - cardHeight, x0 + 0.295f, y1);
            }

            var role = string.IsNullOrEmpty(player.Duty) ? (english ? "Unassigned" : "Sem função") : player.Duty;
            var shownPosition = LocalizationManager.Translate(PresentationLabels.Position(position));
            var roleFit = PlayerTacticalFit.Evaluate(player, _career.Club.Tactics, string.IsNullOrEmpty(player.Duty) ? "Equilibrar" : player.Duty);
            var strongest = LocalizationManager.Translate(roleFit.StrongestAttribute ?? "—");
            var development = LocalizationManager.Translate(roleFit.DevelopmentAttribute ?? "—");
            var attributes = DutyAttributes(position, player.Duty);
            var values = new string[attributes.Length];
            for (var i = 0; i < attributes.Length; i++)
                values[i] = LocalizationManager.Translate(attributes[i]) + " " + PlayerAttributes.CurrentValue(player, attributes[i]);
            var fitText = english
                ? "ROLE FIT " + roleFit.RoleGrade + " (" + roleFit.RoleScore.ToString("0.0") + "/20)\nSYSTEM FIT " + roleFit.TacticalGrade + " (" + roleFit.TacticalScore.ToString("0.0") + "/20)\nBest " + strongest + " " + roleFit.StrongestValue + " · Develop " + development + " " + roleFit.DevelopmentValue
                : "ADEQUAÇÃO " + roleFit.RoleGrade + " (" + roleFit.RoleScore.ToString("0.0") + "/20)\nSISTEMA " + roleFit.TacticalGrade + " (" + roleFit.TacticalScore.ToString("0.0") + "/20)\nPonto forte " + strongest + " " + roleFit.StrongestValue + " · A desenvolver " + development + " " + roleFit.DevelopmentValue;
            var details = fitText + "\n\n" + (english ? "KEY ATTRIBUTES\n" : "ATRIBUTOS-CHAVE\n") + string.Join("  ·  ", values)
                + "\n\n" + DutyBlurb(player.Duty, position, english);
            PaintTacticOverview(overview, _career.Club.Tactics, BroadcastUi.ShortName(player.FullName) + " · " + shownPosition,
                LocalizationManager.Translate(role), details);
        }

        void PaintTacticOverview(Transform host, TacticalIdentity t, string title, string value, string description)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var label = BroadcastUi.Label(host, "OverviewHeading", english ? "OVERVIEW" : "VISTA GERAL", 16, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(label.rectTransform, 0.06f, 0.91f, 0.94f, 0.98f);
            var badge = BroadcastUi.Button(host, "SelectedBadge", english ? "SELECTED" : "SELECIONADO", null,
                new Color(0.39f, 0.05f, 0.82f, 1f), Color.white, 9);
            BroadcastUi.Place(badge.GetComponent<RectTransform>(), 0.58f, 0.84f, 0.94f, 0.9f);

            if (!string.IsNullOrEmpty(title))
            {
                var selected = BroadcastUi.Label(host, "SelectedTitle", title, 12, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                selected.resizeTextForBestFit = true;
                selected.resizeTextMinSize = 9;
                selected.resizeTextMaxSize = 12;
                BroadcastUi.Place(selected.rectTransform, 0.06f, 0.77f, 0.94f, 0.84f);
            }
            if (!string.IsNullOrEmpty(value))
            {
                var current = BroadcastUi.Label(host, "SelectedValue", value, 13, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                current.resizeTextForBestFit = true;
                current.resizeTextMinSize = 9;
                current.resizeTextMaxSize = 13;
                BroadcastUi.Place(current.rectTransform, 0.06f, 0.7f, 0.94f, 0.77f);
            }

            if (!string.IsNullOrEmpty(description))
            {
                var body = BroadcastUi.Label(host, "Description", LocalizationManager.Translate(description), 11, Color.white, TextAnchor.UpperLeft);
                body.horizontalOverflow = HorizontalWrapMode.Wrap;
                body.resizeTextForBestFit = true;
                body.resizeTextMinSize = 8;
                body.resizeTextMaxSize = 11;
                BroadcastUi.Place(body.rectTransform, 0.06f, 0.34f, 0.94f, string.IsNullOrEmpty(value) ? 0.82f : 0.68f);
            }
            else if (t != null)
            {
                var line = 0.67f;
                TacticOverviewLine(host, "Formation", english ? "FORMATION" : "FORMAÇÃO", t.Formation, line); line -= 0.058f;
                TacticOverviewLine(host, "Preset", english ? "STYLE" : "ESTILO", LocalizationManager.Translate(TacticPresetName(t)), line); line -= 0.058f;
                TacticOverviewLine(host, "Press", english ? "PRESSING" : "PRESSÃO", LocalizationManager.Translate(t.Pressing), line); line -= 0.058f;
                TacticOverviewLine(host, "Line", english ? "DEFENSIVE LINE" : "LINHA", LocalizationManager.Translate(t.DefensiveLine), line); line -= 0.058f;
                TacticOverviewLine(host, "Mark", english ? "MARKING" : "MARCAÇÃO", LocalizationManager.Translate(t.Marking), line); line -= 0.058f;
                TacticOverviewLine(host, "Build", english ? "BUILD-UP" : "CONSTRUÇÃO", LocalizationManager.Translate(t.BuildUp), line); line -= 0.058f;
                TacticOverviewLine(host, "Width", english ? "WIDTH" : "LARGURA", LocalizationManager.Translate(t.Width), line); line -= 0.058f;
                TacticOverviewLine(host, "Tempo", english ? "TEMPO" : "RITMO", LocalizationManager.Translate(t.Tempo), line); line -= 0.058f;
                TacticOverviewLine(host, "Pass", english ? "PASSING" : "PASSE", LocalizationManager.Translate(t.PassingStyle), line);
            }

            if (t != null && !string.IsNullOrEmpty(value))
            {
                var heat = TacticHeat(value);
                TacticBar(host, "Intensity", english ? "INTENSITY" : "INTENSIDADE", heat, 0.22f);
                TacticBar(host, "Risk", english ? "RISK" : "RISCO", Mathf.Clamp01(heat * 0.9f), 0.14f);
                TacticBar(host, "Space", english ? "SPACE" : "ESPAÇO", Mathf.Clamp01(1.05f - heat), 0.06f);
            }
        }

        static void TacticOverviewLine(Transform host, string name, string label, string value, float y)
        {
            var row = BroadcastUi.Label(host, "Overview" + name, label + "  " + (string.IsNullOrEmpty(value) ? "—" : value), 9, Color.white, TextAnchor.MiddleLeft);
            row.resizeTextForBestFit = true;
            row.resizeTextMinSize = 7;
            row.resizeTextMaxSize = 9;
            BroadcastUi.Place(row.rectTransform, 0.07f, y - 0.052f, 0.94f, y);
        }

        static string TacticSymbol(string key)
        {
            if (key == "tempo") return "1×";
            if (key == "width") return "↔";
            if (key == "press") return "↑";
            if (key == "line") return "—";
            if (key == "mark") return "●";
            if (key == "backs") return "↗";
            if (key == "build") return "↖";
            if (key == "pass") return "→";
            if (key == "idea") return "⚽";
            return "◇";
        }

        static void TacticTile(Transform host, string name, string title, string value, string symbol, bool selected, Action click,
            float x0, float y0, float x1, float y1)
        {
            var edge = BroadcastUi.Panel(host, name, selected ? new Color(0.62f, 0.22f, 0.95f, 1f) : new Color(0.31f, 0.34f, 0.4f, 1f), true);
            BroadcastUi.Place(edge.rectTransform, x0, y0, x1, y1);
            BroadcastUi.Hit(edge, click);
            var surface = BroadcastUi.Panel(edge.transform, "Surface", new Color(0.105f, 0.12f, 0.16f, 1f), false);
            BroadcastUi.Place(surface.rectTransform, 0.008f, 0.012f, 0.992f, 0.988f);
            surface.raycastTarget = false;
            var titleLabel = BroadcastUi.Label(edge.transform, "Title", (title ?? "").ToUpperInvariant(), 11, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            titleLabel.resizeTextForBestFit = true;
            titleLabel.resizeTextMinSize = 8;
            titleLabel.resizeTextMaxSize = 11;
            BroadcastUi.Place(titleLabel.rectTransform, 0.075f, 0.76f, 0.925f, 0.95f);
            var glyph = BroadcastUi.Label(edge.transform, "Glyph", symbol ?? "→", 28, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(glyph.rectTransform, 0.12f, 0.3f, 0.88f, 0.73f);
            var valueLabel = BroadcastUi.Label(edge.transform, "Value", value ?? "", 11, Color.white, TextAnchor.MiddleCenter);
            valueLabel.resizeTextForBestFit = true;
            valueLabel.resizeTextMinSize = 8;
            valueLabel.resizeTextMaxSize = 11;
            BroadcastUi.Place(valueLabel.rectTransform, 0.055f, 0.05f, 0.945f, 0.28f);
        }

        void PaintTacticPitch(Transform host, TacticalIdentity tactics, int highlight, List<Player> eleven, Player picked)
        {
            var formation = tactics == null || string.IsNullOrEmpty(tactics.Formation) ? "4-3-3" : tactics.Formation;
            var pitch = host as RectTransform;
            BroadcastUi.DrawVerticalHalfPitch(pitch, new Color(0.12f, 0.46f, 0.22f));
            var caption = BroadcastUi.Label(host, "Shape", formation, 14, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            caption.raycastTarget = false;
            BroadcastUi.Place(caption.rectTransform, 0.2f, 0.9f, 0.8f, 0.98f);
            var english = LocalizationManager.Current == GameLanguage.English;
            var phases = english
                ? new[] { "BUILD-UP", "PROGRESSION", "FINAL THIRD" }
                : new[] { "CONSTRUÇÃO", "PROGRESSÃO", "ÚLTIMO TERÇO" };
            var phaseY = new[] { 0.16f, 0.5f, 0.84f };
            for (var i = 0; i < phases.Length; i++)
            {
                var phase = BroadcastUi.Label(host, "Phase" + i, phases[i], 9, new Color(1f, 1f, 1f, 0.68f), TextAnchor.MiddleCenter, FontStyle.Bold);
                phase.raycastTarget = false;
                phase.rectTransform.anchorMin = phase.rectTransform.anchorMax = new Vector2(0.06f, phaseY[i]);
                phase.rectTransform.sizeDelta = new Vector2(150f, 18f);
                phase.rectTransform.anchoredPosition = Vector2.zero;
                phase.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            var push = TacticPush(tactics);
            for (var i = 0; i < 11; i++)
            {
                var slot = MatchEngine.Slot(true, i, formation);
                slot.y = Mathf.Clamp(slot.y + push, 0.04f, 0.9f);
                var duty = i < eleven.Count && eleven[i] != null ? eleven[i].Duty : "";
                var position = i < eleven.Count && eleven[i] != null ? eleven[i].Position : PlayerPosition.CentralMidfielder;
                var aim = DutyAim(duty, slot, position);
                var drift = aim.normalized * (i == highlight ? 0.07f : 0.035f);
                var ghost = BroadcastUi.Panel(host, "Ghost" + i, new Color(0.55f, 0.9f, 0.82f, i == highlight ? 0.7f : 0.28f), false);
                var ghostSize = 0.03f;
                BroadcastUi.Place(ghost.rectTransform, slot.x - ghostSize, slot.y - ghostSize, slot.x + ghostSize, slot.y + ghostSize);
                var motion = ghost.gameObject.AddComponent<TacticDrift>();
                motion.Setup(drift, i * 0.45f);
                var selected = i == highlight;
                var arrow = BroadcastUi.Label(host, "Arrow" + i, "→", 16, new Color(1f, 1f, 1f, selected ? 0.95f : 0.55f), TextAnchor.MiddleCenter, FontStyle.Bold);
                arrow.raycastTarget = false;
                var angle = Mathf.Atan2(aim.y, aim.x == 0f ? 0.01f : aim.x) * Mathf.Rad2Deg;
                arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                var ax = slot.x + aim.normalized.x * 0.055f;
                var ay = slot.y + aim.normalized.y * 0.055f;
                BroadcastUi.Place(arrow.rectTransform, ax - 0.028f, ay - 0.02f, ax + 0.028f, ay + 0.02f);
                if (i < eleven.Count && eleven[i] != null)
                {
                    var captured = eleven[i];
                    var card = PersonCard(host, captured, _career.Club, false, i, false, 72f, 56f, position, null, () =>
                    {
                        _tacticSelection = "player:" + captured.Id;
                        OpenTacticsSheet();
                    }, selected ? new Color(0.12f, 0.36f, 0.38f, 1f) : (Color?)null);
                    card.anchorMin = card.anchorMax = slot;
                    card.anchoredPosition = Vector2.zero;
                }
                if (selected && eleven[i] != null)
                {
                    var note = BroadcastUi.Panel(host, "PostIt", new Color(0.98f, 0.92f, 0.55f, 0.96f), false);
                    BroadcastUi.Place(note.rectTransform, 0.08f, 0.02f, 0.92f, 0.16f);
                    var sticky = BroadcastUi.Label(note.transform, "Text", eleven[i].SquadNumber + "  " + BroadcastUi.ShortName(eleven[i].FullName) + "\n" + (string.IsNullOrEmpty(eleven[i].Duty) ? "Sem função" : eleven[i].Duty), 12, new Color(0.15f, 0.1f, 0.05f), TextAnchor.MiddleLeft, FontStyle.Bold);
                    sticky.raycastTarget = false;
                    sticky.horizontalOverflow = HorizontalWrapMode.Wrap;
                    BroadcastUi.Place(sticky.rectTransform, 0.06f, 0.08f, 0.94f, 0.92f);
                }
            }
            var ballSlot = MatchEngine.Slot(true, 8, formation);
            ballSlot.y = Mathf.Clamp(ballSlot.y + push, 0.08f, 0.86f);
            var ball = BroadcastUi.Panel(host, "Ball", Color.white, false);
            ball.sprite = BroadcastUi.Circle;
            ball.type = Image.Type.Simple;
            BroadcastUi.Place(ball.rectTransform, ballSlot.x + 0.03f, ballSlot.y - 0.045f, ballSlot.x + 0.07f, ballSlot.y - 0.01f);
        }

        static float TacticPush(TacticalIdentity tactics)
        {
            if (tactics == null)
                return 0f;
            var push = 0f;
            var press = tactics.Pressing ?? "";
            var line = tactics.DefensiveLine ?? "";
            if (press.IndexOf("muito", StringComparison.OrdinalIgnoreCase) >= 0)
                push += 0.08f;
            else if (press.IndexOf("alta", StringComparison.OrdinalIgnoreCase) >= 0)
                push += 0.05f;
            else if (press.IndexOf("baixa", StringComparison.OrdinalIgnoreCase) >= 0)
                push -= 0.04f;
            if (line.IndexOf("muito", StringComparison.OrdinalIgnoreCase) >= 0)
                push += 0.05f;
            else if (line.IndexOf("alta", StringComparison.OrdinalIgnoreCase) >= 0)
                push += 0.03f;
            else if (line.IndexOf("baixa", StringComparison.OrdinalIgnoreCase) >= 0)
                push -= 0.05f;
            return push;
        }

        static Vector2 DutyAim(string duty, Vector2 slot, PlayerPosition position)
        {
            var text = duty ?? "";
            var toCenter = slot.x < 0.5f ? 1f : -1f;
            var toWide = -toCenter;
            var x = 0f;
            var y = 0.25f;
            if (text.IndexOf("Cortar para dentro", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                x = toCenter;
                y = 0.45f;
            }
            else if (text.IndexOf("Ficar aberto", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                x = toWide;
                y = 0.35f;
            }
            else if (text.IndexOf("Chegar", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("área", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("Subir", StringComparison.OrdinalIgnoreCase) >= 0)
                y = 1f;
            else if (text.IndexOf("Defensivo", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("Ficar atrás", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("Marcar", StringComparison.OrdinalIgnoreCase) >= 0)
                y = -0.9f;
            else if (text.IndexOf("desmarca", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                x = toWide * 0.55f;
                y = 0.75f;
            }
            else if (text.IndexOf("Mover-se", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                x = toCenter * 0.45f;
                y = 0.55f;
            }
            else if (position == PlayerPosition.RightWinger || position == PlayerPosition.LeftWinger
                || position == PlayerPosition.RightMidfielder || position == PlayerPosition.LeftMidfielder)
                y = 1f;
            else if (position == PlayerPosition.AttackingMidfielder)
            {
                x = toCenter * 0.85f;
                y = 0.4f;
            }
            else if (position == PlayerPosition.Striker)
                y = 0.7f;
            var aim = new Vector2(x, y);
            if (aim.sqrMagnitude < 0.01f)
                aim = new Vector2(0f, 0.4f);
            return aim;
        }

        void CommitTactic(Action<TacticalIdentity> edit)
        {
            if (_career.Club.Tactics == null)
                _career.Club.Tactics = new TacticalIdentity();
            edit(_career.Club.Tactics);
            var t = _career.Club.Tactics;
            var match = TacticPresets.MatchingPresetIndex(t);
            t.Preset = match < 0 ? TacticPresets.CustomName : TacticPresets.Names[match];
            t.Summary = (string.IsNullOrEmpty(t.Formation) ? "4-3-3" : t.Formation) + " · " + t.Preset;
            OpenTacticsSheet();
        }

        void SaveCurrentTactic()
        {
            if (_career == null || _career.Club == null || _career.Club.Tactics == null)
                return;
            if (_career.SavedTactics.Count >= 5)
                _career.SavedTactics.RemoveAt(0);
            var english = LocalizationManager.Current == GameLanguage.English;
            var savedTactic = CopyTactic(_career.Club.Tactics);
            savedTactic.Preset = TacticPresets.CustomName;
            _career.SavedTactics.Add(new CareerSavedTactic
            {
                Name = (english ? "Custom " : "Personalizada ") + (_career.SavedTactics.Count + 1),
                Tactics = savedTactic
            });
            CareerStore.Save(_career);
            OpenTacticsSheet();
        }

        void ApplySavedTactic(int index)
        {
            if (_career == null || index < 0 || index >= _career.SavedTactics.Count)
                return;
            var saved = _career.SavedTactics[index];
            if (saved == null || saved.Tactics == null)
                return;
            _career.Club.Tactics = CopyTactic(saved.Tactics);
            _career.Club.Tactics.Preset = TacticPresets.CustomName;
            _tacticSelection = "home";
            CareerStore.Save(_career);
            OpenTacticsSheet();
        }

        static TacticalIdentity CopyTactic(TacticalIdentity source)
        {
            if (source == null) return new TacticalIdentity { Formation = "4-3-3" };
            return new TacticalIdentity
            {
                Formation = source.Formation,
                Pressing = source.Pressing,
                BuildUp = source.BuildUp,
                FullBacks = source.FullBacks,
                DefensiveLine = source.DefensiveLine,
                Width = source.Width,
                Tempo = source.Tempo,
                PassingStyle = source.PassingStyle,
                AttackIdea = source.AttackIdea,
                Marking = source.Marking,
                Preset = source.Preset,
                Summary = source.Summary
            };
        }

        void ApplyTacticOption(string key, string value)
        {
            CommitTactic(t =>
            {
                switch (key)
                {
                    case "press": t.Pressing = value; break;
                    case "line": t.DefensiveLine = value; break;
                    case "mark": t.Marking = value; break;
                    case "build": t.BuildUp = value; break;
                    case "backs": t.FullBacks = value; break;
                    case "width": t.Width = value; break;
                    case "tempo": t.Tempo = value; break;
                    case "pass": t.PassingStyle = value; break;
                    case "idea": t.AttackIdea = value; break;
                }
            });
        }

        static void TacticCategory(string key, TacticalIdentity t, out string title, out string[] options, out string current)
        {
            switch (key)
            {
                case "press":
                    title = "Pressão";
                    options = new[] { "Pressão muito alta", "Pressão alta", "Pressão média", "Pressão baixa" };
                    current = t.Pressing;
                    return;
                case "line":
                    title = "Linha defensiva";
                    options = new[] { "Linha muito alta", "Linha defensiva alta", "Linha defensiva média", "Linha defensiva baixa", "Linha muito baixa" };
                    current = t.DefensiveLine;
                    return;
                case "mark":
                    title = "Marcação";
                    options = new[] { "Marcação zonal", "Marcação mista", "Marcação ao homem" };
                    current = string.IsNullOrEmpty(t.Marking) ? "Marcação mista" : t.Marking;
                    return;
                case "build":
                    title = "Construção";
                    options = new[] { "Construção curta", "Jogo misto", "Construção longa" };
                    current = t.BuildUp;
                    return;
                case "backs":
                    title = "Laterais";
                    options = new[] { "Laterais ficam", "Laterais equilibram", "Laterais sobem" };
                    current = string.IsNullOrEmpty(t.FullBacks) ? "Laterais equilibram" : t.FullBacks;
                    return;
                case "width":
                    title = "Largura";
                    options = new[] { "Largura estreita", "Largura normal", "Largura larga" };
                    current = string.IsNullOrEmpty(t.Width) ? "Largura normal" : t.Width;
                    return;
                case "tempo":
                    title = "Ritmo";
                    options = new[] { "Ritmo lento", "Ritmo normal", "Ritmo rápido" };
                    current = string.IsNullOrEmpty(t.Tempo) ? "Ritmo normal" : t.Tempo;
                    return;
                case "pass":
                    title = "Passe";
                    options = new[] { "Passe seguro", "Passe misto", "Passe arriscado" };
                    current = string.IsNullOrEmpty(t.PassingStyle) ? "Passe misto" : t.PassingStyle;
                    return;
                default:
                    title = "Ideia de jogo";
                    options = new[] { "Equilibrado", "Tiki-taka", "Posse", "Cruzamentos", "Jogo direto", "Bolas longas", "Remates de longe", "Contra-ataque" };
                    current = string.IsNullOrEmpty(t.AttackIdea) ? "Equilibrado" : t.AttackIdea;
                    return;
            }
        }

        static string TacticPresetName(TacticalIdentity t)
        {
            var match = TacticPresets.MatchingPresetIndex(t);
            if (match == 2)
                return "Gegenpressing";
            if (match < 0)
                return "Personalizada";
            return TacticPresets.Names[match];
        }

        static float TacticHeat(string value)
        {
            var text = value ?? "";
            if (text.Contains("muito") || text.Contains("rápido") || text.Contains("arriscado") || text.Contains("ofensivo") || text.Contains("Tiki") || text.Contains("direto") || text.Contains("longas") || text.Contains("longe"))
                return 0.92f;
            if (text.Contains("alta") || text.Contains("sobem") || text.Contains("larga") || text.Contains("Cruz") || text.Contains("curta") || text.Contains("homem"))
                return 0.74f;
            if (text.Contains("baixa") || text.Contains("lento") || text.Contains("seguro") || text.Contains("ficam") || text.Contains("estreita") || text.Contains("zonal"))
                return 0.32f;
            return 0.52f;
        }

        static string TacticBlurb(string value, bool english)
        {
            var text = value ?? "";
            if (english)
            {
                if (text.Contains("Pressão muito alta")) return "The team presses immediately after losing the ball, high in the opposition half. It can win possession early, but leaves space behind the back line.";
                if (text.Contains("Pressão alta")) return "The press starts high with a line of cover behind it. Players close nearby options without chasing every ball to the last line.";
                if (text.Contains("Pressão baixa")) return "The team waits in a deeper block and presses once the ball enters its half, protecting space behind the defence.";
                if (text.Contains("Linha defensiva") && text.Contains("alta")) return "The back line steps up to compress the pitch and support the press, accepting more risk from balls played in behind.";
                if (text.Contains("Linha defensiva") && text.Contains("baixa")) return "The back line drops closer to the box, protecting depth and inviting the opponent to build in front of it.";
                if (text.Contains("ao homem")) return "Players track their assigned opponents to disrupt combinations, with the risk of being pulled out of position.";
                if (text.Contains("zonal")) return "Players defend spaces rather than following opponents, helping the team keep its shape as attackers rotate.";
                if (text.Contains("Construção curta")) return "Build from the goalkeeper and defenders with short passes. It keeps the ball, but a turnover can expose the team.";
                if (text.Contains("longa")) return "Play over the first line of pressure toward the forward or a wide channel, then compete for the second ball.";
                if (text.Contains("sobem")) return "Full-backs join the attack and provide width. Midfielders need to cover the channels they leave behind.";
                if (text.Contains("ficam")) return "Full-backs hold their positions, keeping defensive cover behind the attack and limiting overlaps.";
                if (text.Contains("larga")) return "The team stretches the pitch and uses the wide channels to create room for crosses and one-on-ones.";
                if (text.Contains("estreita")) return "The team attacks through central spaces, leaving the flanks for full-backs to use.";
                if (text.Contains("rápido")) return "Move the ball forward quickly with fewer touches. It creates fast attacks but can increase turnovers.";
                if (text.Contains("lento")) return "Circulate patiently until an opening appears, making the opponent move before accelerating.";
                if (text.Contains("arriscado")) return "Look for passes that break lines and create advantages, accepting a greater chance of losing possession.";
                if (text.Contains("seguro")) return "Prioritise passes to feet and retain possession, waiting for a clear route forward.";
                if (text.Contains("Contra")) return "Defend with patience, then attack the space behind the opposition as soon as possession is won.";
                if (text.Contains("Cruz")) return "Use the wide areas to reach the byline and deliver crosses into the penalty area.";
                if (text.Contains("Posse") || text.Contains("Tiki")) return "Keep the ball through short combinations, draw the opposition out, and accelerate when a lane opens.";
                if (text.Contains("direto") || text.Contains("longas")) return "Reach the attacking area with fewer passes, using the forward as a target and competing for second balls.";
                return "A balanced approach: keep the team compact, use the ball sensibly, and adjust one instruction at a time.";
            }
            if (text.Contains("muito alta") && text.Contains("Press"))
                return "A equipa sai à bola assim que a perde, ainda no meio-campo adversário. É o motor do gegenpressing: recuperar cedo e atacar com o bloco alto.";
            if (text.Contains("Pressão alta"))
                return "A pressão começa cedo, mas há uma linha de cobertura. A equipa não persegue cada bola até à última linha.";
            if (text.Contains("Pressão baixa"))
                return "O bloco espera. A pressão só chega quando a bola entra na sua metade, para não abrir espaço nas costas.";
            if (text.Contains("muito alta") || (text.Contains("Linha") && text.Contains("alta") && text.Contains("muito")))
                return "A linha sobe quase até ao meio-campo. Ganha terreno e encurta o campo, e fica exposta a uma bola nas costas.";
            if (text.Contains("Linha") && text.Contains("baixa"))
                return "A linha recua e protege a área. O adversário tem a bola, a equipa tem o espaço compacto.";
            if (text.Contains("ao homem"))
                return "Cada jogador segue o seu adversário. Fecha tabelas e desmarcações, e pode ser arrastado para fora da zona.";
            if (text.Contains("zonal"))
                return "A equipa defende zonas, não homens. Mantém a forma mesmo quando o adversário troca de posições.";
            if (text.Contains("curta"))
                return "A saída é curta, pelo guarda-redes e pelos centrais. Pede frieza e oferece o contra-ataque se a perda for à frente.";
            if (text.Contains("longa"))
                return "A primeira bola salta a pressão. Procura o avançado ou a ala e joga o segundo lance lá à frente.";
            if (text.Contains("sobem"))
                return "Os laterais entram no ataque e dão linha de fundo. O meio-campo tem de cobrir o corredor que eles deixam.";
            if (text.Contains("ficam"))
                return "Os laterais seguram a linha. A equipa ataca com menos largura e com mais gente atrás da bola.";
            if (text.Contains("larga"))
                return "Os extremos abrem no limite do campo. O jogo procura o cruzamento e estica a defesa adversária.";
            if (text.Contains("estreita"))
                return "A equipa joga por dentro. Os corredores ficam para os laterais e o miolo enche de gente.";
            if (text.Contains("rápido"))
                return "A posse anda para a frente depressa. Menos toques, mais profundidade, mais perdas também.";
            if (text.Contains("lento"))
                return "A equipa circula até o buraco aparecer. O ritmo baixo cansa o bloco adversário e pede paciência.";
            if (text.Contains("arriscado"))
                return "O passe parte linhas. Ganha superioridade quando entra e entrega a bola quando é lido.";
            if (text.Contains("seguro"))
                return "O passe fica no pé. A posse conserva-se e a progressão espera uma linha aberta.";
            if (text.Contains("Contra"))
                return "Sem bola a equipa espera. Com bola, a primeira opção é o espaço nas costas da defesa.";
            if (text.Contains("Cruz"))
                return "A jogada acaba na linha de fundo. Os cruzamentos procuram a área, não o remate de fora.";
            if (text.Contains("Posse") || text.Contains("Tiki"))
                return "A bola não se perde de graça. A equipa atrai, troca, e só acelera quando a defesa já saiu do sítio.";
            if (text.Contains("direto") || text.Contains("longas"))
                return "Poucos passes até à área. A referência é o avançado e a segunda bola.";
            return "A equipa fica a meio: nem se esconde, nem persegue cada perda. Serve de base para afinares uma instrução de cada vez.";
        }

        static string DutyBlurb(string duty, PlayerPosition position, bool english)
        {
            var text = duty ?? "";
            if (text.Contains("Cortar para dentro ofensivo"))
                return english
                    ? "WITH THE BALL: Attack the half-space early, carry inside at defenders and look for a shot or a final pass.\nWITHOUT THE BALL: Make earlier diagonal runs beyond the full-back and into the box."
                    : "COM BOLA: Ataca cedo o meio-espaço, conduz para cima dos defesas e procura o remate ou passe final.\nSEM BOLA: Faz desmarcações diagonais mais cedo, nas costas do lateral e para dentro da área.";
            if (text.Contains("Cortar para dentro"))
                return english
                    ? "WITH THE BALL: Move inside from the touchline, combine between full-back and centre-back, then carry or shoot when the lane opens.\nWITHOUT THE BALL: Hold a wide starting position, then make diagonal runs into the half-space."
                    : "COM BOLA: Sai da linha para receber entre o lateral e o central; combina, conduz por dentro ou remata quando abre espaço.\nSEM BOLA: Parte aberto e procura desmarcações diagonais para o meio-espaço.";
            if (text.Contains("Ficar aberto defensivo"))
                return english
                    ? "WITH THE BALL: Keep the width and recycle possession with a safe pass; attack space only when the counter is on.\nWITHOUT THE BALL: Track the opposing full-back and help protect your own defensive channel."
                    : "COM BOLA: Mantém a largura e circula com um passe seguro; ataca o espaço quando houver oportunidade para o contra-ataque.\nSEM BOLA: Acompanha o lateral adversário e ajuda a proteger o teu corredor defensivo.";
            if (text.Contains("Só desmarcação"))
            {
                var wideRunner = position == PlayerPosition.LeftWinger || position == PlayerPosition.RightWinger
                    || position == PlayerPosition.LeftMidfielder || position == PlayerPosition.RightMidfielder;
                if (wideRunner)
                    return english
                        ? "WITH THE BALL: Attack the outside lane when space opens, take on the full-back and cross when a teammate reaches the box.\nWITHOUT THE BALL: Make short, repeated runs down the wing to stretch the defence."
                        : "COM BOLA: Ataca o corredor exterior quando abre espaço, tenta ultrapassar o lateral e cruza quando um colega chega à área.\nSEM BOLA: Faz desmarcações curtas e repetidas pela ala para esticar a defesa.";
                return english
                    ? "WITH THE BALL: Use a short pass or one-two to keep the move flowing; carry forward when the next lane is clear.\nWITHOUT THE BALL: Make sharp runs beyond the marker to offer a forward passing option without abandoning the team's shape."
                    : "COM BOLA: Combina com um passe curto ou uma tabela e conduz quando o corredor seguinte está livre.\nSEM BOLA: Desmarca-te para lá do marcador e oferece uma linha de passe sem abandonar a estrutura da equipa.";
            }
            if (text.Contains("Ficar aberto"))
                return english
                    ? "WITH THE BALL: Stay near the touchline, use pace to attack open space, take on the full-back and cross when a teammate is arriving.\nWITHOUT THE BALL: Stretch the back line and offer a run down the outside channel."
                    : "COM BOLA: Fica junto à linha, acelera no espaço, tenta ultrapassar o lateral e cruza quando há colegas a chegar.\nSEM BOLA: Estica a defesa e oferece uma corrida pelo corredor exterior.";
            if (text.Contains("Subir ofensivo"))
                return english
                    ? "WITH THE BALL: Overlap aggressively, sprint into open space and cross when the box is occupied.\nWITHOUT THE BALL: Recover quickly to protect the outside channel after the run."
                    : "COM BOLA: Faz a sobreposição com frequência, arranca pelo espaço livre e cruza quando a área está ocupada.\nSEM BOLA: Recupera depressa para proteger o corredor exterior depois da subida.";
            if (text.Contains("Subir equilibrado"))
                return english
                    ? "WITH THE BALL: Choose the overlap when the winger has control and the midfield can cover; keep the cross selective.\nWITHOUT THE BALL: Recover into the back four and protect the space behind the run."
                    : "COM BOLA: Faz a sobreposição quando o extremo tem controlo e o meio-campo dá cobertura; escolhe os cruzamentos.\nSEM BOLA: Regressa à linha defensiva e protege o espaço deixado pela subida.";
            if (text.Contains("Chegar à área"))
                return english
                    ? "WITH THE BALL: Keep combinations moving and choose a late pass or shot rather than forcing the final action.\nWITHOUT THE BALL: Time a run beyond the midfield line and arrive near the penalty spot for cut-backs and rebounds."
                    : "COM BOLA: Mantém as combinações e escolhe o passe ou remate final sem forçar.\nSEM BOLA: Cronometra a corrida para além do meio-campo e aparece na área para receber recuos ou segundas bolas.";
            if (text.Contains("Ficar na área"))
                return english
                    ? "WITH THE BALL: Shield it under pressure and lay it off to a nearby teammate.\nWITHOUT THE BALL: Stay on the last line, attack crosses with strength and timing, and finish acrobatically only when the player has that trait."
                    : "COM BOLA: Protege a posse sob pressão e solta a bola num colega próximo.\nSEM BOLA: Mantém-se na última linha, ataca cruzamentos com força e timing e tenta uma chilena apenas se tiver esse traço.";
            if (text.Contains("Sair com a bola"))
                return english
                    ? "WITH THE BALL: Carry forward only when the first line is open; otherwise find a composed short pass.\nWITHOUT THE BALL: Hold the defensive line and keep cover behind the advancing full-back."
                    : "COM BOLA: Conduz quando há espaço na primeira fase; se não, procura um passe curto e seguro.\nSEM BOLA: Mantém a linha defensiva e cobre o espaço deixado pelo lateral.";
            if (text.Contains("Sair nos cruzamentos"))
                return english
                    ? "WITH THE BALL: Release quickly to a free defender or full-back.\nWITHOUT THE BALL: Claim crosses when the goalkeeper can arrive first and protect the space behind the defence."
                    : "COM BOLA: Solta depressa para um defesa ou lateral livre.\nSEM BOLA: Sai aos cruzamentos quando pode chegar primeiro e protege o espaço nas costas da defesa.";
            if (text.Contains("Ficar na linha"))
                return english
                    ? "WITH THE BALL: Choose a safe distribution and avoid forcing a pass through pressure.\nWITHOUT THE BALL: Prioritise the goal line and let the defenders manage depth."
                    : "COM BOLA: Escolhe uma reposição segura e evita forçar passes sob pressão.\nSEM BOLA: Protege a baliza e deixa a linha defensiva controlar a profundidade.";
            if (text.Contains("Subir ofensivo"))
                return english
                    ? "WITH THE BALL: Overlap on the wing, sprint into free space and cross when the box is occupied.\nWITHOUT THE BALL: Recover quickly and cover the outside channel."
                    : "COM BOLA: Faz a sobreposição, arranca pelo espaço livre e cruza quando a área está ocupada.\nSEM BOLA: Recupera depressa e fecha o corredor exterior.";
            if (text.Contains("Ficar atrás") || text.Contains("Defensivo") || text == "Marcar")
                return english
                    ? "WITH THE BALL: Prefer a safe pass and keep the team balanced behind the attack.\nWITHOUT THE BALL: Protect the central lane, track the assigned threat and delay the counter-attack."
                    : "COM BOLA: Prefere o passe seguro e mantém equilíbrio atrás do ataque.\nSEM BOLA: Protege o corredor central, acompanha a ameaça marcada e atrasa o contra-ataque.";
            if (string.IsNullOrEmpty(text))
                return english
                    ? "WITH THE BALL: Take a touch to assess pressure, scan for teammates and choose a pass, one-two or carry; shoot when the space and role call for it.\nWITHOUT THE BALL: Hold the assigned position, then move to offer a passing lane or cover. Wide roles can sprint, take on a defender and cross when space opens."
                    : "COM BOLA: Domina para avaliar a pressão, procura colegas e escolhe entre passe, tabela ou condução; remata quando o espaço e a função o justificam.\nSEM BOLA: Mantém a posição e mexe-te para oferecer uma linha de passe ou dar cobertura. Nas alas, acelera, tenta ultrapassar o defesa e cruza quando surge espaço.";
            return english
                ? "WITH THE BALL: Take a touch to read pressure, look for a short pass or one-two, and carry or shoot when the opening is there.\nWITHOUT THE BALL: Keep the team shape and support the nearest runner. Wide players may sprint, take on a defender and cross when space opens. This role changes decisions, not the listed position."
                : "COM BOLA: Domina para ler a pressão, procura um passe curto ou uma tabela e conduz ou remata quando aparece espaço.\nSEM BOLA: Mantém a estrutura e apoia a desmarcação mais próxima. Nas alas pode acelerar, tentar ultrapassar o defesa e cruzar quando há espaço. A função muda as decisões, não a posição indicada.";
        }

        static string[] DutyAttributes(PlayerPosition position, string duty)
        {
            var text = duty ?? "";
            if (text.Contains("Cortar para dentro"))
                return new[] { "Drible", "Velocidade", "Decisão", "Finalização", "Passe" };
            if (text.Contains("Ficar aberto"))
                return new[] { "Velocidade", "Cruzamento", "Drible", "Resistência", "Decisão" };
            if (text.Contains("desmarca"))
                return new[] { "Velocidade", "Posicionamento", "Decisão", "Resistência", "Finalização" };
            if (text.Contains("Chegar à área") || text.Contains("Ficar na área"))
                return new[] { "Posicionamento", "Finalização", "Decisão", "Frieza", "Força" };
            if (text.Contains("Subir"))
                return new[] { "Resistência", "Velocidade", "Cruzamento", "Decisão", "Desarme" };
            if (text.Contains("Sair com a bola"))
                return new[] { "Passe", "Decisão", "Posicionamento", "Desarme", "Frieza" };
            if (text.Contains("Sair nos cruzamentos") || text.Contains("Ficar na linha"))
                return new[] { "Reflexos", "Manejo", "Posicionamento", "Decisão", "Frieza" };
            if (text.Contains("Defensivo") || text.Contains("Ficar atrás") || text == "Marcar")
                return new[] { "Posicionamento", "Desarme", "Decisão", "Força", "Entrega" };
            switch (position)
            {
                case PlayerPosition.Goalkeeper:
                    return new[] { "Reflexos", "Manejo", "Posicionamento", "Frieza" };
                case PlayerPosition.CentreBack:
                    return new[] { "Desarme", "Posicionamento", "Força", "Frieza" };
                case PlayerPosition.RightBack:
                case PlayerPosition.LeftBack:
                    return new[] { "Velocidade", "Cruzamento", "Resistência", "Desarme" };
                case PlayerPosition.DefensiveMidfielder:
                    return new[] { "Posicionamento", "Passe", "Desarme", "Entrega" };
                case PlayerPosition.AttackingMidfielder:
                case PlayerPosition.RightWinger:
                case PlayerPosition.LeftWinger:
                    return new[] { "Drible", "Velocidade", "Passe", "Finalização" };
                case PlayerPosition.Striker:
                    return new[] { "Finalização", "Posicionamento", "Frieza", "Força" };
                default:
                    return new[] { "Passe", "Decisão", "Resistência", "Entrega" };
            }
        }

        static void TacticHeading(Transform parent, string title, string subtitle)
        {
            var head = BroadcastUi.Label(parent, "Head", title, 22, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            head.horizontalOverflow = HorizontalWrapMode.Wrap;
            head.resizeTextForBestFit = true;
            head.resizeTextMinSize = 14;
            head.resizeTextMaxSize = 22;
            BroadcastUi.Place(head.rectTransform, 0.06f, 0.82f, 0.94f, 0.96f);
            var sub = BroadcastUi.Label(parent, "Sub", subtitle, 13, new Color(0.75f, 0.9f, 0.86f, 1f), TextAnchor.MiddleLeft);
            sub.resizeTextForBestFit = true;
            sub.resizeTextMinSize = 10;
            sub.resizeTextMaxSize = 13;
            sub.horizontalOverflow = HorizontalWrapMode.Wrap;
            BroadcastUi.Place(sub.rectTransform, 0.06f, 0.74f, 0.94f, 0.82f);
        }

        static void TacticChips(Transform parent, string left, string right)
        {
            var a = BroadcastUi.Panel(parent, "ChipA", new Color(1f, 1f, 1f, 0.08f), false);
            BroadcastUi.Place(a.rectTransform, 0.06f, 0.66f, 0.48f, 0.74f);
            var aText = BroadcastUi.Label(a.transform, "T", left ?? "", 12, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            aText.resizeTextForBestFit = true;
            aText.resizeTextMinSize = 9;
            aText.resizeTextMaxSize = 12;
            BroadcastUi.Stretch(aText.rectTransform);
            var b = BroadcastUi.Panel(parent, "ChipB", new Color(1f, 1f, 1f, 0.08f), false);
            BroadcastUi.Place(b.rectTransform, 0.52f, 0.66f, 0.94f, 0.74f);
            var bText = BroadcastUi.Label(b.transform, "T", right ?? "", 12, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            bText.resizeTextForBestFit = true;
            bText.resizeTextMinSize = 9;
            bText.resizeTextMaxSize = 12;
            BroadcastUi.Stretch(bText.rectTransform);
        }

        static void TacticBar(Transform parent, string name, string label, float fill, float y0)
        {
            var caption = BroadcastUi.Label(parent, name + "L", label, 13, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(caption.rectTransform, 0.06f, y0, 0.4f, y0 + 0.06f);
            var track = BroadcastUi.Panel(parent, name + "T", new Color(1f, 1f, 1f, 0.1f), false);
            BroadcastUi.Place(track.rectTransform, 0.42f, y0 + 0.012f, 0.94f, y0 + 0.048f);
            var bar = BroadcastUi.Panel(track.transform, "Fill", new Color(0.55f, 0.82f, 0.74f, 1f), false);
            BroadcastUi.Place(bar.rectTransform, 0f, 0.12f, Mathf.Clamp(fill, 0.06f, 1f), 0.88f);
        }

        static void TacticRow(Transform parent, string name, string label, bool on, Action click, float y0, float y1, string badgeText = null)
        {
            var button = BroadcastUi.Button(parent, name, label, click,
                on ? new Color(0.08f, 0.4f, 0.34f, 1f) : new Color(1f, 1f, 1f, 0.045f), Color.white, 13);
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.alignment = TextAnchor.MiddleLeft;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 10;
                text.resizeTextMaxSize = 14;
                text.rectTransform.offsetMin = new Vector2(36f, 2f);
            }
            var radio = BroadcastUi.Panel(button.transform, "Radio", badgeText == null
                ? (on ? new Color(0.55f, 0.95f, 0.8f, 1f) : new Color(1f, 1f, 1f, 0.28f))
                : (on ? new Color(0.03f, 0.65f, 0.46f, 1f) : new Color(0.02f, 0.035f, 0.06f, 0.9f)), false);
            BroadcastUi.Place(radio.rectTransform, 0.03f, 0.28f, 0.07f, 0.72f);
            if (badgeText != null)
            {
                var badge = BroadcastUi.Label(radio.transform, "Number", badgeText, 10, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                badge.resizeTextForBestFit = true;
                badge.resizeTextMinSize = 7;
                badge.resizeTextMaxSize = 10;
                BroadcastUi.Stretch(badge.rectTransform);
            }
            BroadcastUi.Place(button.GetComponent<RectTransform>(), 0.02f, y0, 0.98f, y1);
        }

        GameObject BuildTransfers(Transform host)
        {
            var page = BroadcastUi.Panel(host, "Transfers", new Color(0.08f, 0.03f, 0.16f, 0.45f));
            BroadcastUi.Stretch(page.rectTransform);
            ConfigureTransfersPage(page.transform);
            return page.gameObject;
        }

        void ConfigureTransfersPage(Transform page)
        {
            if (page == null)
                return;
            _transfersRoot = page.Find("TransfersRoot");
            if (_transfersRoot == null)
            {
                var rootObject = new GameObject("TransfersRoot", typeof(RectTransform));
                rootObject.transform.SetParent(page, false);
                _transfersRoot = rootObject.transform;
                BroadcastUi.Stretch(rootObject.GetComponent<RectTransform>());
            }
            for (var i = 0; i < page.childCount; i++)
            {
                var child = page.GetChild(i);
                if (child != _transfersRoot)
                    child.gameObject.SetActive(false);
            }

            var title = _transfersRoot.Find("TransfersTitle")?.GetComponent<Text>();
            if (title == null)
                title = BroadcastUi.Label(_transfersRoot, "TransfersTitle", "TRANSFERS", 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.text = LocalizationManager.Current == GameLanguage.English ? "transfers" : "transferências";
            BroadcastUi.Place(title.rectTransform, 0.03f, 0.91f, 0.5f, 0.99f);

            if (_transferListPage == null)
            {
                var existingList = _transfersRoot.Find("TransferListPage");
                if (existingList != null)
                {
                    _transferListPage = existingList.gameObject;
                    _transferListHost = existingList.Find("ListScroll/Content") as RectTransform;
                }
                else
                {
                    var list = BroadcastUi.Panel(_transfersRoot, "TransferListPage", new Color(0.05f, 0.02f, 0.12f, 0.2f));
                    BroadcastUi.Scroll(list.transform, "ListScroll", out var transferListHost);
                    _transferListPage = list.gameObject;
                    _transferListHost = transferListHost;
                }
            }
            if (_transferMarketPage == null)
            {
                var existingMarket = _transfersRoot.Find("TransferMarketPage");
                if (existingMarket != null)
                {
                    _transferMarketPage = existingMarket.gameObject;
                    _transferMarketHost = existingMarket.Find("MarketScroll/Content") as RectTransform;
                }
                else
                {
                    var market = BroadcastUi.Panel(_transfersRoot, "TransferMarketPage", new Color(0.05f, 0.02f, 0.12f, 0.2f));
                    BroadcastUi.Scroll(market.transform, "MarketScroll", out var transferMarketHost);
                    _transferMarketPage = market.gameObject;
                    _transferMarketHost = transferMarketHost;
                }
            }
            BroadcastUi.Place(_transferListPage.GetComponent<RectTransform>(), 0.02f, 0.02f, 0.98f, 0.82f);
            BroadcastUi.Place(_transferMarketPage.GetComponent<RectTransform>(), 0.02f, 0.02f, 0.98f, 0.82f);
            if (_transferListHost == null)
                _transferListHost = _transferListPage.transform.Find("ListScroll/Content") as RectTransform;
            if (_transferMarketHost == null)
                _transferMarketHost = _transferMarketPage.transform.Find("MarketScroll/Content") as RectTransform;
            if (_transferSearch == null)
            {
                var search = _transferMarketPage.transform.Find("Input");
                _transferSearch = search == null ? UiFactory.Input(_transferMarketPage.transform, "Search players by name") : search.GetComponent<InputField>();
                _transferSearch.name = "Input";
            }
            if (_transferSearch != null)
            {
                BroadcastUi.Place(_transferSearch.GetComponent<RectTransform>(), 0.02f, 0.9f, 0.98f, 0.98f);
                _transferSearch.onValueChanged.RemoveAllListeners();
                _transferSearch.onValueChanged.AddListener(_ => { if (_transferPage == 1) FillMarket(_transferSearch, _transferMarketHost); });
            }
            var listScroll = _transferListPage.transform.Find("ListScroll");
            if (listScroll != null) BroadcastUi.Place(listScroll.GetComponent<RectTransform>(), 0.01f, 0.02f, 0.99f, 0.98f);
            var marketScroll = _transferMarketPage.transform.Find("MarketScroll");
            if (marketScroll != null) BroadcastUi.Place(marketScroll.GetComponent<RectTransform>(), 0.01f, 0.02f, 0.99f, 0.88f);

            _transferTabs = new Button[3];
            var labels = LocalizationManager.Current == GameLanguage.English ? new[] { "SHORTLIST", "TRANSFER MARKET", "GLOBAL HISTORY" } : new[] { "LISTA", "MERCADO DE TRANSFERÊNCIAS", "HISTÓRICO GLOBAL" };
            for (var i = 0; i < _transferTabs.Length; i++)
            {
                var tab = i;
                var existing = _transfersRoot.Find("TransferTab" + i);
                _transferTabs[i] = existing == null
                    ? BroadcastUi.Button(_transfersRoot, "TransferTab" + i, labels[i], () => ShowTransfersZone(tab), new Color(1f, 1f, 1f, 0.08f), Color.white, 14)
                    : SceneBind.Wire(existing, () => ShowTransfersZone(tab));
                _transferTabs[i].GetComponentInChildren<Text>().text = labels[i];
                var x0 = 0.03f + i * 0.2f;
                BroadcastUi.Place(_transferTabs[i].GetComponent<RectTransform>(), x0, 0.835f, x0 + 0.19f, 0.9f);
            }
            ShowTransfersZone(_transferPage);
        }

        void ShowTransfersZone(int zone)
        {
            _transferPage = Mathf.Clamp(zone, 0, 2);
            if (_transferTabs != null)
                for (var i = 0; i < _transferTabs.Length; i++)
                {
                    if (_transferTabs[i] == null) continue;
                    var tabText = _transferTabs[i].GetComponentInChildren<Text>();
                    if (tabText != null)
                        tabText.text = LocalizationManager.Current == GameLanguage.English
                        ? (i == 0 ? "SHORTLIST" : i == 1 ? "TRANSFER MARKET" : "GLOBAL HISTORY").ToUpperInvariant()
                            : (i == 0 ? "LISTA" : i == 1 ? "MERCADO DE TRANSFERÊNCIAS" : "HISTÓRICO GLOBAL").ToUpperInvariant();
                    _transferTabs[i].GetComponent<Image>().color = i == _transferPage ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f);
                    var label = _transferTabs[i].GetComponentInChildren<Text>();
                    if (label != null) label.color = i == _transferPage ? UiTheme.ButtonText : Color.white;
                }
            if (_transferListPage != null) _transferListPage.SetActive(_transferPage == 0);
            if (_transferMarketPage != null) _transferMarketPage.SetActive(_transferPage != 0);
            if (_transferMarketPage != null)
            {
                var background = _transferMarketPage.GetComponent<Image>();
                if (background != null)
                    background.color = _transferPage == 2
                        ? new Color(0f, 0f, 0f, 0f)
                        : new Color(0.05f, 0.02f, 0.12f, 0.2f);
                var viewport = _transferMarketPage.transform.Find("MarketScroll")?.GetComponent<Image>();
                if (viewport != null)
                    viewport.color = _transferPage == 2
                        ? new Color(0f, 0f, 0f, 0f)
                        : new Color(0f, 0f, 0f, 0.01f);
            }
            if (_transferSearch != null) _transferSearch.gameObject.SetActive(_transferPage == 1);
            if (_transferPage == 0) FillSquadList(_transferListHost);
            else if (_transferPage == 1) FillMarket(_transferSearch, _transferMarketHost);
            else FillTransferHistory(_transferMarketHost);
        }

        void FillTransferHistory(RectTransform target = null)
        {
            var host = target ?? _transferMarketHost ?? _marketHost;
            if (host == null || _career == null) return;
            Clear(host);
            var english = LocalizationManager.Current == GameLanguage.English;

            var seasons = new List<int> { -1 };
            foreach (var record in _career.TransferHistory)
            {
                if (record.DateTicks <= 0 || record.DateTicks > DateTime.MaxValue.Ticks) continue;
                var year = TransferSeasonStart(new DateTime(record.DateTicks));
                if (!seasons.Contains(year)) seasons.Add(year);
            }
            seasons.Sort(1, seasons.Count - 1, Comparer<int>.Create((a, b) => b.CompareTo(a)));
            if (!seasons.Contains(_transferHistorySeasonStart)) _transferHistorySeasonStart = -1;
            var labels = new List<string>();
            for (var i = 0; i < seasons.Count; i++)
                labels.Add(seasons[i] < 0 ? (english ? "ALL SEASONS" : "TODAS AS ÉPOCAS") : seasons[i] + "/" + (seasons[i] + 1));
            var controls = BroadcastUi.Panel(host, "HistoryControls", new Color(0f, 0f, 0f, 0f), false);
            BroadcastUi.Preferred(controls, 48);
            var seasonIndex = Mathf.Max(0, seasons.IndexOf(_transferHistorySeasonStart));
            CreateHistorySeasonDropdown(controls.transform, labels, seasonIndex, index =>
            {
                _transferHistorySeasonStart = seasons[Mathf.Clamp(index, 0, seasons.Count - 1)];
                FillTransferHistory(host);
            });
            var fee = BroadcastUi.Button(controls.transform, "FeeSort", (english ? "FEE " : "VALOR ") + (_transferHistoryFeeDescending ? "↓" : "↑"), () =>
            {
                _transferHistoryFeeDescending = _transferHistorySortByFee ? !_transferHistoryFeeDescending : true;
                _transferHistorySortByFee = true;
                FillTransferHistory(host);
            }, new Color(1f, 1f, 1f, 0.08f), Color.white, 12);
            BroadcastUi.Place(fee.GetComponent<RectTransform>(), 0.46f, 0.08f, 0.71f, 0.92f);
            var date = BroadcastUi.Button(controls.transform, "DateSort", (english ? "DATE " : "DATA ") + (_transferHistoryDateDescending ? "↓" : "↑"), () =>
            {
                _transferHistoryDateDescending = !_transferHistorySortByFee ? !_transferHistoryDateDescending : true;
                _transferHistorySortByFee = false;
                FillTransferHistory(host);
            }, new Color(1f, 1f, 1f, 0.08f), Color.white, 12);
            BroadcastUi.Place(date.GetComponent<RectTransform>(), 0.73f, 0.08f, 0.98f, 0.92f);

            var filtered = new List<TransferRecord>();
            foreach (var record in _career.TransferHistory)
            {
                var recordDate = record.DateTicks > 0 && record.DateTicks <= DateTime.MaxValue.Ticks
                    ? new DateTime(record.DateTicks) : DateTime.MinValue;
                if (_transferHistorySeasonStart >= 0 && TransferSeasonStart(recordDate) != _transferHistorySeasonStart) continue;
                filtered.Add(record);
            }
            filtered.Sort((a, b) =>
            {
                var primary = _transferHistorySortByFee
                    ? (_transferHistoryFeeDescending ? b.Fee.CompareTo(a.Fee) : a.Fee.CompareTo(b.Fee))
                    : (_transferHistoryDateDescending ? b.DateTicks.CompareTo(a.DateTicks) : a.DateTicks.CompareTo(b.DateTicks));
                if (primary != 0) return primary;
                return _transferHistorySortByFee
                    ? (_transferHistoryDateDescending ? b.DateTicks.CompareTo(a.DateTicks) : a.DateTicks.CompareTo(b.DateTicks))
                    : (_transferHistoryFeeDescending ? b.Fee.CompareTo(a.Fee) : a.Fee.CompareTo(b.Fee));
            });
            if (filtered.Count == 0)
            {
                var empty = BroadcastUi.Label(host, "NoHistory", _career.TransferHistory.Count == 0
                    ? (english ? "No completed transfers have been recorded yet." : "Ainda não há transferências concluídas registadas.")
                    : (english ? "No transfers were recorded in this season." : "Não há transferências registadas nesta época."),
                    14, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Preferred(empty, 42);
                return;
            }
            for (var i = 0; i < filtered.Count; i++)
            {
                var deal = filtered[i];
                if (!_career.World.Players.TryGetValue(deal.PlayerId ?? "", out var player)
                    || !_career.World.Clubs.TryGetValue(deal.FromClubId ?? "", out var from)
                    || !_career.World.Clubs.TryGetValue(deal.ToClubId ?? "", out var to)) continue;
                var row = BroadcastUi.Panel(host, "GlobalDeal" + i, new Color(0.1f, 0.04f, 0.2f, 0.92f), true);
                BroadcastUi.Preferred(row, 92);
                var playerRef = player;
                var headline = BroadcastUi.Button(row.transform, "Player", player.FullName + "  ·  " + PresentationLabels.Position(player.Position)
                    + "  ·  " + (english ? "Age " : "Idade ") + deal.AgeAtTransfer + "  ·  OVR " + deal.OverallAtTransfer,
                    () => OpenPlayer(playerRef), new Color(1f, 1f, 1f, 0.02f), Color.white, 14);
                BroadcastUi.Place(headline.GetComponent<RectTransform>(), 0.02f, 0.56f, 0.66f, 0.95f);
                var dealDate = deal.DateTicks > 0 && deal.DateTicks <= DateTime.MaxValue.Ticks ? new DateTime(deal.DateTicks) : DateTime.MinValue;
                var details = BroadcastUi.Label(row.transform, "Details", LocalizationManager.FormatDate(dealDate, "dd MMM yyyy")
                    + "   ·   " + (english ? "Fee " : "Valor ") + Euro(deal.Fee)
                    + "   ·   " + (english ? "Wage " : "Salário ") + Euro(deal.WeeklyWage) + (english ? "/wk" : "/sem")
                    + "   ·   " + deal.ContractYears + (english ? " years" : " anos") + "   ·   " + LocalizationManager.Translate(deal.Type),
                    12, UiTheme.Muted, TextAnchor.MiddleLeft);
                details.resizeTextForBestFit = true;
                details.resizeTextMinSize = 9;
                details.resizeTextMaxSize = 12;
                BroadcastUi.Place(details.rectTransform, 0.02f, 0.08f, 0.64f, 0.52f);
                var seller = BroadcastUi.Button(row.transform, "Seller", from.ShortName, () => OpenClubDossier(from), new Color(1f, 1f, 1f, 0.06f), Color.white, 11);
                BroadcastUi.Place(seller.GetComponent<RectTransform>(), 0.66f, 0.14f, 0.81f, 0.48f);
                var buyer = BroadcastUi.Button(row.transform, "Buyer", to.ShortName, () => OpenClubDossier(to), new Color(1f, 1f, 1f, 0.06f), Color.white, 11);
                BroadcastUi.Place(buyer.GetComponent<RectTransform>(), 0.82f, 0.14f, 0.98f, 0.48f);
            }
        }

        void FillTransferHistoryLegacy()
        {
            if (_marketHost == null || _career == null) return;
            Clear(_marketHost);
            var english = LocalizationManager.Current == GameLanguage.English;
            var title = BroadcastUi.Label(_marketHost, "HistoryTitle", english ? "COMPLETED TRANSFERS AROUND THE WORLD" : "TRANSFERÊNCIAS CONCLUÍDAS NO MUNDO", 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Preferred(title, 34);

            var clubFilter = BroadcastUi.Button(_marketHost, "HistoryClubFilter", TransferHistoryClubLabel(english) + (english ? "  ·  CHANGE ▶" : "  ·  ALTERAR ▶"),
                () => CycleTransferHistoryClub(), new Color(1f, 1f, 1f, 0.08f), Color.white, 12);
            BroadcastUi.Preferred(clubFilter.GetComponent<RectTransform>(), 38);
            var seasonFilter = BroadcastUi.Button(_marketHost, "HistorySeasonFilter", TransferHistorySeasonLabel(english) + (english ? "  ·  CHANGE ▶" : "  ·  ALTERAR ▶"),
                () => CycleTransferHistorySeason(), new Color(1f, 1f, 1f, 0.08f), Color.white, 12);
            BroadcastUi.Preferred(seasonFilter.GetComponent<RectTransform>(), 38);

            var filtered = new List<TransferRecord>();
            var incoming = 0L;
            var outgoing = 0L;
            foreach (var record in _career.TransferHistory)
            {
                var date = record.DateTicks > 0 && record.DateTicks <= DateTime.MaxValue.Ticks
                    ? new DateTime(record.DateTicks) : DateTime.MinValue;
                if (_transferHistorySeasonStart >= 0 && TransferSeasonStart(date) != _transferHistorySeasonStart)
                    continue;
                if (!string.IsNullOrEmpty(_transferHistoryClubId)
                    && record.FromClubId != _transferHistoryClubId && record.ToClubId != _transferHistoryClubId)
                    continue;
                filtered.Add(record);
                if (!string.IsNullOrEmpty(_transferHistoryClubId))
                {
                    if (record.ToClubId == _transferHistoryClubId) incoming += record.Fee;
                    if (record.FromClubId == _transferHistoryClubId) outgoing += record.Fee;
                }
            }

            if (!string.IsNullOrEmpty(_transferHistoryClubId))
            {
                var totals = BroadcastUi.Label(_marketHost, "HistoryTotals",
                    (english ? "IN  " : "ENTRADAS  ") + Euro((int)Math.Min(int.MaxValue, incoming))
                    + "     " + (english ? "OUT  " : "SAÍDAS  ") + Euro((int)Math.Min(int.MaxValue, outgoing)),
                    12, UiTheme.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Preferred(totals, 30);
            }

            if (filtered.Count == 0)
            {
                var empty = BroadcastUi.Label(_marketHost, "NoHistory", _career.TransferHistory.Count == 0
                    ? (english ? "No completed transfers have been recorded yet." : "Ainda não há transferências concluídas registadas.")
                    : (english ? "No transfers match these filters." : "Não há transferências para estes filtros."),
                    14, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Preferred(empty, 42);
                return;
            }

            for (var i = 0; i < filtered.Count; i++)
            {
                var deal = filtered[i];
                if (!_career.World.Players.TryGetValue(deal.PlayerId ?? "", out var player)
                    || !_career.World.Clubs.TryGetValue(deal.FromClubId ?? "", out var from)
                    || !_career.World.Clubs.TryGetValue(deal.ToClubId ?? "", out var to))
                    continue;
                var row = BroadcastUi.Panel(_marketHost, "Deal" + i, new Color(0.1f, 0.04f, 0.2f, 0.92f), true);
                BroadcastUi.Preferred(row, 104);
                var date = deal.DateTicks > 0 && deal.DateTicks <= DateTime.MaxValue.Ticks ? new DateTime(deal.DateTicks) : _career.Calendar.CurrentDate;
                var playerRef = player;
                var headline = BroadcastUi.Button(row.transform, "Player", player.FullName + "  ·  " + PresentationLabels.Position(player.Position)
                    + "  ·  " + (english ? "Age " : "Idade ") + deal.AgeAtTransfer + "  ·  OVR " + deal.OverallAtTransfer,
                    () => OpenPlayer(playerRef), new Color(1f, 1f, 1f, 0.02f), Color.white, 15);
                BroadcastUi.Place(headline.GetComponent<RectTransform>(), 0.025f, 0.55f, 0.97f, 0.94f);
                var details = BroadcastUi.Label(row.transform, "Details", LocalizationManager.FormatDate(date, "dd MMM yyyy")
                    + "   ·   " + (english ? "Fee " : "Valor ") + Euro(deal.Fee)
                    + "   ·   " + (english ? "Wage " : "Salário ") + Euro(deal.WeeklyWage) + (english ? "/wk" : "/sem")
                    + "   ·   " + deal.ContractYears + (english ? " years" : " anos")
                    + "   ·   " + deal.Type, 13, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Place(details.rectTransform, 0.025f, 0.12f, 0.64f, 0.51f);
                var seller = BroadcastUi.Button(row.transform, "Seller", from.ShortName, () => OpenClubDossier(from), new Color(1f, 1f, 1f, 0.06f), Color.white, 12);
                BroadcastUi.Place(seller.GetComponent<RectTransform>(), 0.66f, 0.14f, 0.79f, 0.48f);
                var buyer = BroadcastUi.Button(row.transform, "Buyer", to.ShortName, () => OpenClubDossier(to), new Color(1f, 1f, 1f, 0.06f), Color.white, 12);
                BroadcastUi.Place(buyer.GetComponent<RectTransform>(), 0.8f, 0.14f, 0.97f, 0.48f);
            }
        }

        Dropdown CreateHistorySeasonDropdown(Transform parent, List<string> options, int value, Action<int> changed)
        {
            var root = new GameObject("SeasonDropdown", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Dropdown));
            root.transform.SetParent(parent, false);
            var image = root.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.1f);
            image.raycastTarget = true;
            BroadcastUi.Place(root.GetComponent<RectTransform>(), 0.02f, 0.08f, 0.44f, 0.92f);
            var dropdown = root.GetComponent<Dropdown>();
            dropdown.transition = Selectable.Transition.None;
            dropdown.targetGraphic = image;
            var caption = BroadcastUi.Label(root.transform, "Caption", "", 11, Color.white, TextAnchor.MiddleLeft);
            caption.raycastTarget = false;
            BroadcastUi.Place(caption.rectTransform, 0.04f, 0.04f, 0.86f, 0.96f);
            var arrow = BroadcastUi.Label(root.transform, "Arrow", "▼", 9, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            arrow.raycastTarget = false;
            BroadcastUi.Place(arrow.rectTransform, 0.87f, 0.04f, 0.98f, 0.96f);

            var templateObject = new GameObject("Template", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            templateObject.transform.SetParent(root.transform, false);
            var templateRect = templateObject.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, -2f);
            templateRect.sizeDelta = new Vector2(0f, 210f);
            templateObject.GetComponent<Image>().color = new Color(0.08f, 0.035f, 0.16f, 1f);
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
            var contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;
            var layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = contentRect;

            var item = new GameObject("Item", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle), typeof(LayoutElement));
            item.transform.SetParent(contentObject.transform, false);
            item.GetComponent<LayoutElement>().preferredHeight = 32f;
            var itemImage = item.GetComponent<Image>();
            itemImage.color = new Color(1f, 1f, 1f, 0.04f);
            var toggle = item.GetComponent<Toggle>();
            toggle.targetGraphic = itemImage;
            var check = new GameObject("Item Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            check.transform.SetParent(item.transform, false);
            var checkRect = check.GetComponent<RectTransform>();
            checkRect.anchorMin = checkRect.anchorMax = new Vector2(0f, 0.5f);
            checkRect.sizeDelta = new Vector2(14f, 14f);
            checkRect.anchoredPosition = new Vector2(12f, 0f);
            var checkImage = check.GetComponent<Image>();
            checkImage.color = UiTheme.Accent;
            toggle.graphic = checkImage;
            var itemText = BroadcastUi.Label(item.transform, "Item Label", "", 11, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(itemText.rectTransform, 0.08f, 0.04f, 0.97f, 0.96f);
            itemText.raycastTarget = false;
            dropdown.template = templateRect;
            dropdown.captionText = caption;
            dropdown.itemText = itemText;
            dropdown.AddOptions(options);
            dropdown.SetValueWithoutNotify(Mathf.Clamp(value, 0, options.Count - 1));
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(index => changed?.Invoke(index));
            templateObject.SetActive(false);
            return dropdown;
        }

        string TransferHistoryClubLabel(bool english)
        {
            if (string.IsNullOrEmpty(_transferHistoryClubId))
                return english ? "CLUB · ALL CLUBS" : "CLUBE · TODOS";
            return _career.World.Clubs.TryGetValue(_transferHistoryClubId, out var club)
                ? (english ? "CLUB · " : "CLUBE · ") + club.ShortName
                : (english ? "CLUB · ALL CLUBS" : "CLUBE · TODOS");
        }

        string TransferHistorySeasonLabel(bool english)
        {
            if (_transferHistorySeasonStart < 0)
                return english ? "SEASON · ALL SEASONS" : "ÉPOCA · TODAS";
            return (english ? "SEASON · " : "ÉPOCA · ") + _transferHistorySeasonStart + "/" + (_transferHistorySeasonStart + 1);
        }

        void CycleTransferHistoryClub()
        {
            var options = new List<Club> { null };
            foreach (var club in _career.World.Clubs.Values)
                if (club != null && !options.Contains(club)) options.Add(club);
            options.Sort(1, options.Count - 1, Comparer<Club>.Create((a, b) => string.Compare(a.ShortName, b.ShortName, StringComparison.Ordinal)));
            var index = options.FindIndex(club => club == null ? string.IsNullOrEmpty(_transferHistoryClubId) : club.Id == _transferHistoryClubId);
            _transferHistoryClubId = options[(index + 1 + options.Count) % options.Count]?.Id ?? "";
            FillTransferHistory();
        }

        void CycleTransferHistorySeason()
        {
            var starts = new List<int> { -1 };
            foreach (var record in _career.TransferHistory)
            {
                if (record.DateTicks <= 0 || record.DateTicks > DateTime.MaxValue.Ticks) continue;
                var start = TransferSeasonStart(new DateTime(record.DateTicks));
                if (!starts.Contains(start)) starts.Add(start);
            }
            starts.Sort(1, starts.Count - 1, Comparer<int>.Create((a, b) => b.CompareTo(a)));
            var index = starts.IndexOf(_transferHistorySeasonStart);
            _transferHistorySeasonStart = starts[(index + 1 + starts.Count) % starts.Count];
            FillTransferHistory();
        }

        static int TransferSeasonStart(DateTime date)
        {
            if (date == DateTime.MinValue) return -1;
            return date.Month >= 7 ? date.Year : date.Year - 1;
        }

        GameObject BuildCalendar(Transform host)
        {
            var page = BroadcastUi.Panel(host, "Calendar", new Color(0.08f, 0.03f, 0.16f, 0.45f));
            BroadcastUi.Stretch(page.rectTransform);
            var english = LocalizationManager.Current == GameLanguage.English;
            var title = BroadcastUi.Label(page.transform, "Title", english ? "CALENDAR  ·  assistant notes for each match" : "CALENDÁRIO  ·  nota do adjunto em cada jogo", 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.03f, 0.92f, 0.62f, 0.99f);
            var month = BroadcastUi.Button(page.transform, "OpenCalendar", english ? "CALENDAR" : "CALENDÁRIO", OpenCalendarPopup, UiTheme.Accent, UiTheme.ButtonText, 14);
            BroadcastUi.Place(month.GetComponent<RectTransform>(), 0.64f, 0.91f, 0.80f, 0.99f);
            var trip = BroadcastUi.Button(page.transform, "Trip", english ? "PLAN TRAVEL" : "MARCAR VIAGEM", OpenTrips, UiTheme.Accent, UiTheme.ButtonText, 14);
            BroadcastUi.Place(trip.GetComponent<RectTransform>(), 0.81f, 0.91f, 0.97f, 0.99f);
            var scroll = BroadcastUi.Scroll(page.transform, "Scroll", out _calendar);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.02f, 0.03f, 0.98f, 0.9f);
            FillCalendar();
            return page.gameObject;
        }

        GameObject BuildStandings(Transform host)
        {
            var page = BroadcastUi.Panel(host, "Standings", new Color(0.08f, 0.03f, 0.16f, 0.45f));
            BroadcastUi.Stretch(page.rectTransform);
            var title = BroadcastUi.Label(page.transform, "Title", "CLASSIFICAÇÕES", 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.03f, 0.92f, 0.22f, 0.99f);
            var legend = BroadcastUi.Label(page.transform, "Legend", "J jogos   ·   V vitórias   ·   E empates   ·   D derrotas   ·   GM   ·   GS   ·   Pts", 13, UiTheme.Muted, TextAnchor.MiddleRight);
            BroadcastUi.Place(legend.rectTransform, 0.24f, 0.92f, 0.98f, 0.99f);
            _tableContent = BroadcastUi.Zone(page.transform, "Grid", 0.01f, 0.02f, 0.99f, 0.9f);
            return page.gameObject;
        }

        void BuildPlay(Transform root)
        {
            var play = BroadcastUi.Button(root, "PlayMatch", "CONTINUAR", OnContinue, UiTheme.Accent, UiTheme.ButtonText, 20);
            _play = play;
            BroadcastUi.Place(play.GetComponent<RectTransform>(), 0.16f, 0.01f, 0.68f, 0.07f);
            _playLabel = play.GetComponentInChildren<Text>();
            BroadcastUi.Place(play.GetComponent<RectTransform>(), 0.16f, 0.01f, 0.84f, 0.07f);
        }

        void BuildModal(Transform root)
        {
            var modal = BroadcastUi.Zone(root, "Modal", 0f, 0f, 1f, 1f);
            var dim = BroadcastUi.Solid(modal, "Dim", new Color(0f, 0f, 0f, 0f), true);
            dim.color = new Color(0f, 0f, 0f, 0.58f);
            BroadcastUi.Stretch(dim.rectTransform);
            var card = BroadcastUi.Panel(modal, "Card", new Color(0.09f, 0.035f, 0.18f, 1f), true);
            BroadcastUi.Place(card.rectTransform, 0.04f, 0.03f, 0.96f, 0.97f);
            _modalTitle = BroadcastUi.Label(card.transform, "Title", "", 26, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(_modalTitle.rectTransform, 0.14f, 0.88f, 0.96f, 0.97f);
            _modalLead = BroadcastUi.Label(card.transform, "Lead", "", 15, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(_modalLead.rectTransform, 0.05f, 0.8f, 0.95f, 0.88f);
            var scroll = BroadcastUi.Scroll(card.transform, "Scroll", out _modalBody);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.04f, 0.16f, 0.96f, 0.72f);
            _modalAction = BroadcastUi.Button(card.transform, "Action", "", null, UiTheme.Accent, UiTheme.ButtonText, 16);
            BroadcastUi.Place(_modalAction.GetComponent<RectTransform>(), 0.16f, 0.035f, 0.84f, 0.12f);
            _modalActionLabel = _modalAction.GetComponentInChildren<Text>();
            _modalClose = EnsureCloseButton(card.transform, "ModalClose", CloseModal);
            _modal = modal.gameObject;
            _modal.SetActive(false);
        }

        static Button EnsureCloseButton(Transform card, string name, Action close)
        {
            if (card == null) return null;
            var existing = card.Find(name);
            var button = existing == null || existing.GetComponent<Button>() == null
                ? BroadcastUi.Button(card, name, "×", close, new Color(1f, 1f, 1f, 0.12f), Color.white, 24)
                : SceneBind.Wire(existing, close);
            if (button == null) return null;
            button.gameObject.SetActive(true);
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = "×";
                label.fontSize = 24;
                label.fontStyle = FontStyle.Bold;
            }
            BroadcastUi.Place(button.GetComponent<RectTransform>(), 0.935f, 0.895f, 0.98f, 0.97f);
            return button;
        }

        public bool HandleControlBack()
        {
            if (_modal != null && _modal.activeSelf)
            {
                CloseModal();
                return true;
            }
            if (_tacticsBody != null && _tacticsBody.Find("Choices") != null)
            {
                FillTactics();
                return true;
            }
            var transferSheet = transform.Find("TransferSheet");
            if (transferSheet != null)
            {
                Destroy(transferSheet.gameObject);
                return true;
            }
            var contractSheet = transform.Find("ContractSheet");
            if (contractSheet != null)
            {
                Destroy(contractSheet.gameObject);
                return true;
            }
            var tacticsSheet = transform.Find("TacticsSheet");
            if (tacticsSheet != null)
            {
                var choices = tacticsSheet.Find("Choices");
                if (choices != null)
                    Destroy(choices.gameObject);
                else
                    Destroy(tacticsSheet.gameObject);
                return true;
            }
            return false;
        }

        void OnContinue()
        {
            if (PressDutyOpen())
                return;
            if (SquadDutyOpen())
            {
                OpenUrgentDuty();
                return;
            }
            var moment = _career.Current;
            if (moment != null && moment.Kind == MomentKind.Talk && !moment.Answered)
            {
                OpenTalkChoices();
                return;
            }
            var startingMatch = moment != null && moment.Kind == MomentKind.Match && moment.Fixture != null && !moment.Fixture.Played;
            var gate = CareerOnline.Check(_career, startingMatch);
            if (HoldGuest(startingMatch, gate))
                return;
            if (gate.Stop)
            {
                OpenModal("ONLINE", gate.Message, false, true);
                return;
            }
            if (startingMatch)
            {
                SeasonSchedule.SimulateOthers(_career, moment.Fixture.Date, moment.Fixture);
                if (SettingsPrefs.AutosaveBeforeMatch)
                    CareerStore.AutoSave(_career, SettingsPrefs.AutosaveSlots);
                GameRoot.Instance.LiveMatch = MatchSession.Create(_career, moment.Fixture, SettingsPrefs.AutomaticSquadSelection);
                GameRoot.Instance.Open(SceneNames.MatchDay);
                return;
            }

            AdvanceShared();
        }

        void AdvanceShared()
        {
            var previousDay = _career.Calendar.CurrentDate.Date;
            CareerMoments.Continue(_career);
            var elapsedDays = (_career.Calendar.CurrentDate.Date - previousDay).Days;
            if (elapsedDays > 0)
            {
                AdvancePlayerSystems(elapsedDays);
                ClubFinanceLedger.SettleWeeklyPayroll(_career, previousDay, _career.Calendar.CurrentDate.Date);
                ClubFinanceLedger.SettleWeeklyCommercialIncome(_career, previousDay, _career.Calendar.CurrentDate.Date);
            }
            if (SettingsPrefs.AutosaveEnabled)
                CareerStore.AutoSaveIfDue(_career, SettingsPrefs.AutosaveDays, SettingsPrefs.AutosaveSlots);
            CareerStore.Save(_career);
            if (_career.Online != null && _career.Online.Active && LanRoom.Connected)
                LanRoom.Share(CareerStore.ReadText());
            Refresh();
            FillNews();
        }

        void Answer(int choice)
        {
            CareerMoments.Answer(_career, choice);
            CareerStore.Save(_career);
            CloseModal();
            Refresh();
        }

        void OpenTalkChoices()
        {
            var moment = _career == null ? null : _career.Current;
            if (moment == null || moment.Kind != MomentKind.Talk || moment.Answered || moment.Replies == null)
                return;
            var english = LocalizationManager.Current == GameLanguage.English;
            OpenModal(CareerMomentTitle(moment), CareerMomentText(moment), false, true);
            SizeModal(0.22f, 0.22f, 0.78f, 0.78f);
            for (var i = 0; i < moment.Replies.Length && i < 3; i++)
            {
                var choice = i;
                var button = BroadcastUi.Button(_modalBody, "Talk" + i, (i + 1) + "   " + CareerMomentReply(moment, i), () => Answer(choice), new Color(0.1f, 0.05f, 0.2f, 0.95f), Color.white, 15);
                BroadcastUi.Preferred(button, 48);
            }
        }

        void SkipConversations()
        {
            if (PressDutyOpen())
                return;
            if (SettingsPrefs.SkipDayConfirmation)
            {
                var english = LocalizationManager.Current == GameLanguage.English;
                OpenModal(english ? "Skip the whole day?" : "Saltar o dia inteiro?",
                    english ? "All remaining conversations and day events will be skipped." : "As conversas e os acontecimentos restantes deste dia serão ignorados.", true, true);
                var dontAsk = false;
                var choice = BroadcastUi.Panel(_modalBody, "DontAskAgain", new Color(1f, 1f, 1f, 0.06f), true);
                BroadcastUi.Preferred(choice.rectTransform, 48f);
                var box = BroadcastUi.Solid(choice.transform, "CheckBox", new Color(1f, 1f, 1f, 0.16f));
                BroadcastUi.Place(box.rectTransform, 0.025f, 0.24f, 0.09f, 0.76f);
                var check = BroadcastUi.Label(box.transform, "Check", "✓", 15, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Stretch(check.rectTransform);
                check.gameObject.SetActive(false);
                var label = BroadcastUi.Label(choice.transform, "Label",
                    english ? "Don't ask me again" : "Não voltar a perguntar", 14, Color.white, TextAnchor.MiddleLeft);
                BroadcastUi.Place(label.rectTransform, 0.12f, 0.08f, 0.97f, 0.92f);
                BroadcastUi.Hit(choice, () =>
                {
                    dontAsk = !dontAsk;
                    check.gameObject.SetActive(dontAsk);
                });
                _modalActionLabel.text = english ? "SKIP DAY" : "SALTAR DIA";
                _modalAction.onClick.RemoveAllListeners();
                _modalAction.onClick.AddListener(() =>
                {
                    if (dontAsk)
                    {
                        SettingsPrefs.SkipDayConfirmation = false;
                        SettingsPrefs.Commit();
                    }
                    CloseModal();
                    PerformSkipDay();
                });
                return;
            }
            PerformSkipDay();
        }

        void PerformSkipDay()
        {
            if (PressDutyOpen())
                return;
            var english = LocalizationManager.Current == GameLanguage.English;
            var note = english ? "Skipped the remaining events for this day." : "Eventos restantes do dia ignorados.";
            var previousDay = _career.Calendar.CurrentDate.Date;
            if (!CareerMoments.SkipCurrentDay(_career, note))
                return;
            CareerMoments.Continue(_career);
            var currentDay = _career.Calendar.CurrentDate.Date;
            var elapsedDays = Math.Max(1, (currentDay - previousDay).Days);
            AdvancePlayerSystems(elapsedDays);
            ClubFinanceLedger.SettleWeeklyPayroll(_career, previousDay, currentDay);
            ClubFinanceLedger.SettleWeeklyCommercialIncome(_career, previousDay, currentDay);
            if (SettingsPrefs.AutosaveEnabled)
                CareerStore.AutoSaveIfDue(_career, SettingsPrefs.AutosaveDays, SettingsPrefs.AutosaveSlots);
            CareerStore.Save(_career);
            Refresh();
            FillNews();
        }

        void SaveNote()
        {
            var text = _noteField.text == null ? "" : _noteField.text.Trim();
            if (text.Length < 2)
            {
                _noteError.text = LocalizationManager.Current == GameLanguage.English ? "Write at least a short note." : "Escreve pelo menos uma frase.";
                return;
            }

            _noteError.text = "";
            _career.Board.Insert(0, new CoachNote
            {
                Day = LocalizationManager.FormatDate(_career.Calendar.CurrentDate, "dddd, d 'de' MMMM"),
                Hour = _career.Calendar.ClockText,
                When = _career.Calendar.CurrentDate,
                Text = text
            });
            _noteField.text = "";
            FillBoard();
        }

        void EnsureInbox(Transform page)
        {
            if (page == null)
                return;
            var pageImage = page.GetComponent<Image>();
            if (pageImage != null)
                pageImage.color = new Color(0f, 0f, 0f, 0f);
            if (page.Find("InboxMessages") != null && _inboxMessages != null)
                return;
            for (var i = page.childCount - 1; i >= 0; i--)
                Destroy(page.GetChild(i).gameObject);
            var english = LocalizationManager.Current == GameLanguage.English;
            _inboxMessages = InboxColumn(page, "InboxMessages", 0.008f, 0.012f, 0.3f, 0.988f, english ? "MESSAGES" : "MENSAGENS");
            _inboxNews = InboxColumn(page, "InboxNews", 0.308f, 0.012f, 0.64f, 0.988f, english ? "NEWS" : "NOTÍCIAS");
            _inboxSide = InboxColumn(page, "InboxSide", 0.648f, 0.012f, 0.992f, 0.988f, english ? "TODAY" : "HOJE");
        }

        RectTransform InboxColumn(Transform page, string name, float x0, float y0, float x1, float y1, string title)
        {
            var column = BroadcastUi.Panel(page, name, new Color(0.05f, 0.03f, 0.1f, 0.72f));
            BroadcastUi.Place(column.rectTransform, x0, y0, x1, y1);
            var heading = BroadcastUi.Label(column.transform, "Heading", title, 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.04f, 0.93f, 0.96f, 0.99f);
            var scroll = BroadcastUi.Scroll(column.transform, "Scroll", out var content);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.03f, 0.02f, 0.97f, 0.92f);
            return content;
        }

        static void SetColumnHeading(Transform page, string column, string title)
        {
            var heading = page == null ? null : page.Find(column + "/Heading");
            var text = heading == null ? null : heading.GetComponent<Text>();
            if (text != null)
                text.text = title;
        }

        void FillInbox()
        {
            if (_pages == null || _pages.Length == 0 || _pages[0] == null || _career == null)
                return;
            EnsureInbox(_pages[0].transform);
            if (_inboxMessages == null || _inboxNews == null || _inboxSide == null)
                return;
            Clear(_inboxMessages);
            Clear(_inboxNews);
            Clear(_inboxSide);
            var english = LocalizationManager.Current == GameLanguage.English;
            SetColumnHeading(_pages[0].transform, "InboxMessages", english ? "MESSAGES" : "MENSAGENS");
            SetColumnHeading(_pages[0].transform, "InboxNews", english ? "NEWS" : "NOTÍCIAS");
            SetColumnHeading(_pages[0].transform, "InboxSide", english ? "TODAY" : "HOJE");
            var anyMessage = false;
            foreach (var note in PhoneNotes())
            {
                if (note.Channel == "jogador")
                    continue;
                anyMessage = true;
                var captured = note;
                var row = BroadcastUi.Panel(_inboxMessages, "Msg", new Color(0.1f, 0.06f, 0.18f, 0.94f));
                BroadcastUi.Preferred(row, 72);
                BroadcastUi.Hit(row, () => OpenDeskMessage(captured));
                var who = BroadcastUi.Label(row.transform, "Who", note.Who, 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(who.rectTransform, 0.04f, 0.5f, note.Important ? 0.84f : 0.96f, 0.94f);
                var text = BroadcastUi.Label(row.transform, "Text", note.Text, 13, Color.white, TextAnchor.MiddleLeft);
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                BroadcastUi.Place(text.rectTransform, 0.04f, 0.06f, note.Important ? 0.84f : 0.96f, 0.5f);
                if (note.Important)
                    AddDutyMark(row.transform);
                PassClicksTo(row.transform);
            }
            if (!anyMessage)
                BroadcastUi.Preferred(BroadcastUi.Label(_inboxMessages, "Empty", english ? "No messages today." : "Não há mensagens hoje.", 14, UiTheme.Muted), 40);

            var shownStories = 0;
            foreach (var story in Newsroom.Edition(_career))
            {
                if (shownStories >= 6)
                    break;
                shownStories++;
                var captured = story;
                var card = BroadcastUi.Panel(_inboxNews, "Story", new Color(0.1f, 0.06f, 0.18f, 0.94f), true);
                BroadcastUi.Preferred(card, 86);
                BroadcastUi.Hit(card, () => OpenStory(captured));
                var paper = BroadcastUi.Label(card.transform, "Paper", PaperName(story.Paper), 12, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(paper.rectTransform, 0.04f, 0.62f, 0.96f, 0.94f);
                var headline = BroadcastUi.Label(card.transform, "Headline", PressHeadline(story), 15, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(headline.rectTransform, 0.04f, 0.08f, 0.96f, 0.62f);
            }
            if (shownStories == 0)
                BroadcastUi.Preferred(BroadcastUi.Label(_inboxNews, "Empty", english ? "The papers are quiet." : "Os jornais ainda não fecharam.", 14, UiTheme.Muted), 40);

            var fixtures = new List<Fixture>();
            foreach (var fixture in _career.Fixtures)
            {
                if (fixture.Played || fixture.Walkover || fixture.Date.Date < _career.Calendar.CurrentDate.Date
                    || fixture.HomeClubId == fixture.AwayClubId || !fixture.Involves(_career.Club.Id))
                    continue;
                fixtures.Add(fixture);
            }
            fixtures.Sort((a, b) => a.Date.CompareTo(b.Date));
            var fixturesButton = BroadcastUi.Button(_inboxSide, "FixturesHead", english ? "FIXTURES" : "PRÓXIMOS JOGOS", OpenCalendar, new Color(0f, 0f, 0f, 0f), UiTheme.Score, 13);
            BroadcastUi.Preferred(fixturesButton, 28);
            var fixtureCount = Mathf.Min(3, fixtures.Count);
            for (var i = 0; i < fixtureCount; i++)
            {
                var fixture = fixtures[i];
                _career.World.Clubs.TryGetValue(fixture.HomeClubId, out var home);
                _career.World.Clubs.TryGetValue(fixture.AwayClubId, out var away);
                var line = BroadcastUi.Panel(_inboxSide, "Fixture" + i, new Color(0.08f, 0.05f, 0.16f, 0.92f), true);
                BroadcastUi.Preferred(line, 48);
                BroadcastUi.Hit(line, OpenCalendar);
                AddInboxCrest(line.transform, home, 0.02f);
                AddInboxCrest(line.transform, away, 0.84f);
                var caption = LocalizationManager.FormatDate(fixture.Date, "dd/MM") + "  ·  " + FixtureKickoff(fixture)
                    + "  ·  " + (home == null ? "" : home.ShortName) + "  –  " + (away == null ? "" : away.ShortName)
                    + "\n" + FixtureCompetition(fixture);
                var label = BroadcastUi.Label(line.transform, "Text", caption, 12, Color.white, TextAnchor.MiddleLeft);
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 9;
                label.resizeTextMaxSize = 12;
                BroadcastUi.Place(label.rectTransform, 0.14f, 0.04f, 0.82f, 0.96f);
                PassClicksTo(line.transform);
            }
            if (fixtureCount == 0)
                BroadcastUi.Preferred(BroadcastUi.Label(_inboxSide, "NoFixture", english ? "No match scheduled." : "Não há jogo marcado.", 14, UiTheme.Muted), 26);

            SeasonSchedule.RebuildTable(_career);
            var leagueId = _career.Club.LeagueId;
            var rows = new List<LeagueTableRow>();
            foreach (var row in _career.Table)
                if (row.LeagueId == leagueId)
                    rows.Add(row);
            rows.Sort((a, b) =>
            {
                var points = b.Points.CompareTo(a.Points);
                if (points != 0)
                    return points;
                var diff = b.GoalDifference.CompareTo(a.GoalDifference);
                return diff != 0 ? diff : b.GoalsFor.CompareTo(a.GoalsFor);
            });
            var leagueName = _career.League == null ? "" : LocalizationManager.Translate(_career.League.Name);
            var tableTitle = (english ? "TABLE" : "TABELA") + (string.IsNullOrEmpty(leagueName) ? "" : "  ·  " + leagueName);
            var tableButton = BroadcastUi.Button(_inboxSide, "TableHead", tableTitle, () => OpenLeagueStats(leagueId), new Color(0f, 0f, 0f, 0f), UiTheme.Score, 13);
            BroadcastUi.Preferred(tableButton, 28);
            var tableCount = Mathf.Min(8, rows.Count);
            var shownRows = new List<int>();
            for (var i = 0; i < tableCount; i++)
                shownRows.Add(i);
            for (var i = tableCount; i < rows.Count; i++)
            {
                if (OtherHumanSeat(rows[i].ClubId) != null)
                    shownRows.Add(i);
            }
            for (var n = 0; n < shownRows.Count; n++)
            {
                var i = shownRows[n];
                var standing = rows[i];
                _career.World.Clubs.TryGetValue(standing.ClubId, out var club);
                var mine = standing.ClubId == _career.Club.Id;
                var guest = OtherHumanSeat(standing.ClubId);
                var guestColor = guest == null ? Color.white : BroadcastUi.Parse(guest.Color, new Color(0.88f, 0.48f, 0.24f, 1f));
                var line = BroadcastUi.Panel(_inboxSide, "Table" + i, mine ? new Color(0.16f, 0.1f, 0.28f, 1f) : guest != null ? new Color(guestColor.r * 0.35f, guestColor.g * 0.35f, guestColor.b * 0.35f, 0.96f) : new Color(0.08f, 0.05f, 0.16f, 0.92f), true);
                BroadcastUi.Preferred(line, 32);
                BroadcastUi.Hit(line, () => OpenLeagueStats(leagueId));
                AddInboxCrest(line.transform, club, 0.1f);
                var caption = (i + 1) + "    " + (club == null ? standing.ClubId : club.ShortName);
                var label = BroadcastUi.Label(line.transform, "Text", caption, 13, mine ? UiTheme.Accent : guest != null ? guestColor : Color.white, TextAnchor.MiddleLeft, mine || guest != null ? FontStyle.Bold : FontStyle.Normal);
                BroadcastUi.Place(label.rectTransform, 0.24f, 0.05f, 0.78f, 0.95f);
                var points = BroadcastUi.Label(line.transform, "Points", standing.Points.ToString(), 13, mine || guest != null ? label.color : Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
                BroadcastUi.Place(points.rectTransform, 0.78f, 0.05f, 0.96f, 0.95f);
                PassClicksTo(line.transform);
            }

            var finance = _career.Club.Finance;
            if (finance != null)
            {
                var wages = 0;
                foreach (var id in _career.Club.PlayerIds)
                    if (_career.World.Players.TryGetValue(id, out var player))
                        wages += player.WeeklyWage;
                var money = BroadcastUi.Panel(_inboxSide, "Money", new Color(0.07f, 0.12f, 0.18f, 0.96f));
                BroadcastUi.Preferred(money, 176);
                var pendingFees = PendingIncomingFees();
                var transferText = (english ? "TRANSFER BUDGET" : "ORÇAMENTO DE TRANSFERÊNCIAS") + "\n" + (english ? "Available  " : "Disponível  ") + Euro(finance.TransferBudget);
                if (pendingFees > 0)
                    transferText += "\n" + (english ? "Agreed, still to pay  " : "Acordado, ainda por pagar  ") + Euro(pendingFees);
                var transfer = BroadcastUi.Label(money.transform, "Transfer", transferText, 14, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                transfer.horizontalOverflow = HorizontalWrapMode.Wrap;
                BroadcastUi.Place(transfer.rectTransform, 0.06f, 0.52f, 0.94f, 0.96f);
                var weeklyBudget = ClubFinanceLedger.EnsureWeeklyWageBudget(_career, _career.Club);
                var wage = BroadcastUi.Label(money.transform, "Wages",
                    (english ? "WEEKLY WAGE BUDGET  " : "ORÇAMENTO SALARIAL SEMANAL  ") + Euro(weeklyBudget)
                    + "\n" + (english ? "Current squad payroll  " : "Folha salarial atual  ") + Euro(wages) + (english ? "/wk" : "/semana"),
                    13, UiTheme.Muted, TextAnchor.MiddleLeft);
                wage.resizeTextForBestFit = true;
                wage.resizeTextMinSize = 10;
                wage.resizeTextMaxSize = 13;
                wage.horizontalOverflow = HorizontalWrapMode.Wrap;
                BroadcastUi.Place(wage.rectTransform, 0.06f, 0.03f, 0.94f, 0.48f);
            }
        }

        void Refresh()
        {
            RefreshLocalizedPresentation();
            CompleteScouts();
            FillBoard();
            FillCareerRecord();
            FillInbox();
        }

        void RefreshLocalizedPresentation()
        {
            var moment = _career.Current;
            var season = _career.League != null ? LocalizationManager.Translate(_career.League.Name) + " " + _career.Calendar.SeasonLabel : _career.Calendar.SeasonLabel;
            _clock.text = _career.Calendar.ClockText;
            RefreshAlert();
            _dateLine.text = LocalizationManager.FormatDate(_career.Calendar.CurrentDate, "dddd, d 'de' MMMM") + "    ·    " + season + "    ·    " + LocalizationManager.Translate("Mister") + " " + _career.Coach.Name;
            var talking = moment != null && moment.Kind == MomentKind.Talk && !moment.Answered && moment.Replies != null;
            if (_play != null)
            {
                _play.onClick.RemoveAllListeners();
                _play.interactable = true;
                if (PressDutyOpen())
                {
                    _play.interactable = false;
                    _playLabel.text = LocalizationManager.Current == GameLanguage.English
                        ? "PRESS MESSAGE REQUIRED"
                        : "ABRE A MENSAGEM DA CONFERÊNCIA";
                }
                else if (SquadDutyOpen())
                {
                    _play.onClick.AddListener(OpenUrgentDuty);
                    _playLabel.text = LocalizationManager.Current == GameLanguage.English ? "DEAL WITH IT" : "TRATAR DO URGENTE";
                }
                else if (talking)
                {
                    _play.onClick.AddListener(OpenTalkChoices);
                    _playLabel.text = LocalizationManager.Current == GameLanguage.English ? "ANSWER" : "RESPONDER";
                }
                else
                {
                    _play.onClick.AddListener(OnContinue);
                    if (moment != null && moment.Kind == MomentKind.Match)
                        _playLabel.text = LocalizationManager.Current == GameLanguage.English
                            ? "PLAY    " + EnglishMatchLabel(_career)
                            : LocalizationManager.Translate("JOGAR    " + MatchLabel(_career, moment.Fixture));
                    else
                        _playLabel.text = LocalizationManager.Current == GameLanguage.English ? "CONTINUE" : LocalizationManager.Translate("CONTINUAR");
                }
            }

            if (_vibe != null)
                _vibe.text = LocalizationManager.Translate(VibeLine());
        }

        string CareerMomentText(CareerMoment moment)
        {
            if (moment == null)
                return LocalizationManager.Current == GameLanguage.English ? "The season is quiet." : "A época ficou em silêncio.";
            if (LocalizationManager.Current != GameLanguage.English)
                return LocalizationManager.Translate(moment.Body);

            if (moment.Kind == MomentKind.Talk)
            {
                var playerName = moment.PlayerId != null && _career.World.Players.TryGetValue(moment.PlayerId, out var player)
                    ? player.FullName
                    : "The player";
                if (!string.IsNullOrEmpty(moment.IncidentType))
                    return PersonalIncidentBodyEnglish(moment.IncidentType, playerName);
                var source = moment.Body ?? "";
                var trust = source.StartsWith("A confiança entre vocês está baixa.", StringComparison.Ordinal)
                    ? "Trust between you is low. "
                    : source.StartsWith("A confiança construída entre vocês nota-se na conversa.", StringComparison.Ordinal)
                        ? "The trust you've built is showing in this conversation. "
                        : "";
                string summary;
                if (source.Contains("As pernas não aguentam mais este bloco"))
                    summary = playerName + " has stopped during training, worried that another hard drill could leave them limping.";
                else if (source.Contains("Alguém tem de pôr ordem"))
                    summary = playerName + " says a dressing-room argument has gone too far and asks you to step in.";
                else if (source.Contains("Não jogo. Se a ideia é continuar assim"))
                    summary = playerName + " wants a clear answer about their place in the team before the weekend.";
                else if (source.Contains("Qualquer coisa cedeu no último corte"))
                    summary = playerName + " has gone down holding their thigh and says they cannot put weight on the leg.";
                else if (source.Contains("A instrução que tenho é "))
                    summary = playerName + " has questioned their current tactical instruction and warned that the flank could be exposed.";
                else if (source.Contains("Isto vem de ontem."))
                    summary = playerName + " and a teammate have clashed during training over yesterday's drill.";
                else
                    summary = playerName + " wants to know whether they can count on you when the match gets difficult.";

                var responseMarker = "\n\nTu: ";
                var responseStart = source.IndexOf(responseMarker, StringComparison.Ordinal);
                if (responseStart >= 0)
                {
                    responseStart += responseMarker.Length;
                    var responseEnd = source.IndexOf("\n\n", responseStart, StringComparison.Ordinal);
                    var selectedResponse = responseEnd < 0 ? source.Substring(responseStart) : source.Substring(responseStart, responseEnd - responseStart);
                    var responseIndex = Array.IndexOf(moment.Replies ?? new string[0], selectedResponse);
                    var englishResponse = responseIndex >= 0 ? CareerMomentReply(moment, responseIndex) : "You responded to the situation.";
                    return trust + summary + "\n\nYou: " + englishResponse
                        + "\n\nYour response has affected the player's morale, fitness and form.";
                }
                return trust + summary;
            }

            if (moment.Kind == MomentKind.Match && moment.Fixture != null)
            {
                var fixture = moment.Fixture;
                var opponentId = fixture.OpponentId(_career.Coach.ClubId);
                var opponent = _career.World.Clubs.TryGetValue(opponentId, out var club) ? club : null;
                var venue = fixture.IsHome(_career.Coach.ClubId)
                    ? _career.Club.Stadium + " is filling up."
                    : "The bus has arrived at " + (opponent == null ? "the away ground" : opponent.Stadium) + ".";
                return venue + " Today's opponent is " + (opponent == null ? "the opposition" : opponent.Name)
                    + (opponent == null ? "." : ", playing " + opponent.Tactics.Formation + ".")
                    + " Your training-ground briefing is the only report you get.";
            }

            var sourceTitle = LocalizationManager.SourceForEnglish(moment.Title);
            if (string.IsNullOrEmpty(sourceTitle))
                sourceTitle = moment.Title;
            switch ((sourceTitle ?? "").Trim().ToUpperInvariant())
            {
                case "TREINO":
                    return "The staff is watching training. Small gestures around the squad tell you more than a formal report.";
                case "TARDE NO CLUBE":
                    return "The squad spends the afternoon at the club. Who sits together, and who keeps their distance, says plenty.";
                case "NO CLUBE":
                    return "You arrive at " + _career.Club.Name + ". The dressing-room mood shows in small gestures, while the team keeps working on " + _career.Club.Tactics.Formation + ".";
                case "CHEGADA":
                case "ARRIVAL":
                    return "The squad arrives at the training ground. Watch who moves with the group and who keeps to themselves.";
            }

            return "The day at " + _career.Club.Name + " unfolds quietly. Pay attention to the squad's mood and the conversations around the club.";
        }

        string CareerMomentTitle(CareerMoment moment)
        {
            if (LocalizationManager.Current != GameLanguage.English)
                return moment == null ? "DIA EM ABERTO" : LocalizationManager.Translate(moment.Title.ToUpperInvariant());
            if (moment == null)
                return "DAY OFF";
            if (moment.Kind == MomentKind.Talk)
            {
                var playerName = moment.PlayerId != null && _career.World.Players.TryGetValue(moment.PlayerId, out var player)
                    ? player.FullName
                    : TalkTitleName(moment.Title);
                if (!string.IsNullOrEmpty(moment.IncidentType))
                    return PersonalIncidentTitleEnglish(moment.IncidentType) + " · " + playerName.ToUpperInvariant();
                return "TEAM TALK · " + playerName.ToUpperInvariant();
            }
            if (moment.Kind == MomentKind.Match)
                return "MATCH DAY";

            var sourceTitle = LocalizationManager.SourceForEnglish(moment.Title);
            if (string.IsNullOrEmpty(sourceTitle))
                sourceTitle = moment.Title;
            switch ((sourceTitle ?? "").Trim().ToUpperInvariant())
            {
                case "TREINO": return "TRAINING";
                case "TARDE NO CLUBE": return "AFTERNOON AT THE CLUB";
                case "NO CLUBE": return "AT THE CLUB";
                case "CHEGADA":
                case "ARRIVAL": return "ARRIVAL";
                default: return "CLUB UPDATE";
            }
        }

        static string PersonalIncidentTitleEnglish(string type)
        {
            switch ((type ?? "").ToLowerInvariant())
            {
                case "bereavement": return "FAMILY BEREAVEMENT";
                case "betting": return "INTEGRITY INVESTIGATION";
                case "drunk": return "A LONG NIGHT";
                case "viral": return "THE 2-2-6 TEAM SHEET";
                case "family": return "A NEW ARRIVAL";
                case "charity": return "A GOOD DEED";
                default: return "PERSONAL NEWS";
            }
        }

        static string PersonalIncidentBodyEnglish(string type, string name)
        {
            switch ((type ?? "").ToLowerInvariant())
            {
                case "bereavement": return name + " has been told that his father has died. The funeral is today, on matchday. He asks for your support to be with his family.";
                case "betting": return name + " has been named in an internal investigation into football betting. The club is checking the facts, and reporters are already asking questions.";
                case "drunk": return name + " has arrived late after a long night out. He is safe, but clearly tired, and a clip outside a bar is circulating online.";
                case "viral": return name + " swapped the team talk notes for a kitman's 2-2-6 formation. A video of the captain trying to explain it went viral before breakfast.";
                case "family": return name + " has just become a parent. He is delighted but short on sleep, and asks to adjust today's workload before the match.";
                case "charity": return name + " arranged a surprise visit to a local charity and brought equipment for the children. Your assistant asks whether to protect his recovery time.";
                default: return name + " has brought you a personal matter that needs the club's support.";
            }
        }

        static string[] PersonalIncidentRepliesEnglish(string type)
        {
            switch ((type ?? "").ToLowerInvariant())
            {
                case "bereavement": return new[] { "Give him leave to be with his family. The replacement starts.", "Arrange support and let him decide if he can play.", "Ask him to honour the match commitment. We will deal with the rest tomorrow." };
                case "betting": return new[] { "Handle it privately with the player and the integrity officer.", "Keep him out of the next match while the facts are checked.", "Call it personal and ask no questions." };
                case "drunk": return new[] { "Send him home to rest and ask the doctor to check him.", "Fine him and reduce his training load.", "Make him complete the full session to set an example." };
                case "viral": return new[] { "Laugh with the group, then confirm the real team sheet.", "Use the attention to promote the next match.", "End the joke and issue a small fine." };
                case "family": return new[] { "Give him the morning with his family and reduce the workload.", "Adjust training but keep him available for the match.", "Personal life stays at the door; the session is the same for everyone." };
                case "charity": return new[] { "Support the visit and adjust the training schedule.", "Thank him and ask him to return in time for preparation.", "Cancel the visit; football is the only priority today." };
                default: return new[] { "Give him the support he needs.", "Make a measured adjustment and review it tomorrow.", "Keep the normal schedule." };
            }
        }

        static string TalkTitleName(string title)
        {
            if (string.IsNullOrEmpty(title))
                return "THE SQUAD";
            var separator = title.IndexOf('·');
            if (separator >= 0)
                return title.Substring(separator + 1).Trim();
            return title.StartsWith("Conversa", StringComparison.OrdinalIgnoreCase)
                || title.StartsWith("Team Talk", StringComparison.OrdinalIgnoreCase)
                ? "THE SQUAD"
                : title.Trim();
        }

        string CareerMomentReply(CareerMoment moment, int index)
        {
            if (moment == null || moment.Replies == null || index < 0 || index >= moment.Replies.Length)
                return "";
            if (LocalizationManager.Current != GameLanguage.English)
                return LocalizationManager.Translate(moment.Replies[index]);

            if (!string.IsNullOrEmpty(moment.IncidentType))
            {
                var incidentReplies = PersonalIncidentRepliesEnglish(moment.IncidentType);
                return incidentReplies[Mathf.Clamp(index, 0, incidentReplies.Length - 1)];
            }

            var source = moment.Body ?? "";
            string[] replies;
            if (source.Contains("As pernas não aguentam mais este bloco"))
                replies = new[]
                {
                    "Step out. The fitness coach will check you now; you can sit out the final drill.",
                    "One more sprint. If you're faking it, we'll know. If it's real, stop afterwards.",
                    "The group doesn't stop for one person. Get back in line."
                };
            else if (source.Contains("Alguém tem de pôr ordem"))
            {
                if (index == 2)
                {
                    var reply = moment.Replies[index];
                    var start = reply.IndexOf("Fico do lado de ", StringComparison.Ordinal);
                    var end = reply.IndexOf(". O outro", start < 0 ? 0 : start, StringComparison.Ordinal);
                    var teammate = start >= 0 && end > start
                        ? reply.Substring(start + "Fico do lado de ".Length, end - start - "Fico do lado de ".Length)
                        : "your teammate";
                    return "I'll back " + teammate + ". The other player can keep quiet and get back in line.";
                }
                replies = new[]
                {
                    "I'll bring in the captain. We settle this here, in front of the squad, and it's over today.",
                    "We'll change the drill. The argument can wait, if it still matters afterwards.",
                    "I'll back your teammate. The other player can keep quiet and get back in line."
                };
            }
            else if (source.Contains("Não jogo. Se a ideia é continuar assim"))
                replies = new[] { "You're in contention for the next match. The place is yours if your body holds up.", "We'll decide in training, not in this corridor. Show me today.", "Go in. I don't have an answer for you now." };
            else if (source.Contains("Qualquer coisa cedeu no último corte"))
                replies = new[] { "I'm stopping training. The physio will see you now.", "Finish this drill, then we'll talk.", "It's just exertion. Carry on and tell me if it gets worse." };
            else if (source.Contains("A instrução que tenho é "))
                replies = new[] { "Follow the instruction on the board. That's what the squad trained.", "Adjust the instruction. I want a different movement from this side.", "Run the same move again. The instruction won't change today." };
            else if (source.Contains("Isto vem de ontem."))
                replies = new[] { "Separate them and speak to each one away from the group.", "Sort it out yourselves. I've got a training session to run.", "Both of you do an extra set. The argument is over." };
            else
                replies = new[] { "You can count on me. When things get tight, the ball will come through you.", "The team sheet is based on who's best at three o'clock. That could be you, if you earn it.", "I don't answer. I head into the room and leave the question in the corridor." };
            return replies[Mathf.Clamp(index, 0, replies.Length - 1)];
        }

        void ReadPadTabs()
        {
            if (AudioSettingsOverlay.IsVisible)
                return;
            if (_modal != null && _modal.activeSelf)
                return;
            if (transform.Find("TacticsSheet") != null || transform.Find("ContractSheet") != null)
                return;
            var pad = Gamepad.current;
            if (pad == null)
                return;
            if (pad.rightShoulder.wasPressedThisFrame)
                BumpHubTab(1);
            else if (pad.leftShoulder.wasPressedThisFrame)
                BumpHubTab(-1);
            else if (pad.rightTrigger.wasPressedThisFrame)
                BumpSmallTab(1);
            else if (pad.leftTrigger.wasPressedThisFrame)
                BumpSmallTab(-1);
        }

        void BumpHubTab(int direction)
        {
            if (_tabs == null || _tabs.Length == 0)
                return;
            GameAudio.Click();
            Show((_hubPage + direction + _tabs.Length) % _tabs.Length);
        }

        void BumpSmallTab(int direction)
        {
            if (_hubPage == 3)
            {
                BumpTeamTab(direction);
                return;
            }
            if (_hubPage == 2)
            {
                BumpAcademyStrip(direction);
                return;
            }
            if (_hubPage == 4)
                return;
            if (_hubPage == 5 && _transferTabs != null && _transferTabs.Length > 0)
            {
                GameAudio.Click();
                ShowTransfersZone((_transferPage + direction + _transferTabs.Length) % _transferTabs.Length);
                return;
            }
            if (_hubPage == 6 && _officeViews != null && _officeViews.Length > 0)
            {
                GameAudio.Click();
                ShowOfficeTab((_officeTab + direction + _officeViews.Length) % _officeViews.Length);
            }
        }

        void BumpAcademyStrip(int direction)
        {
            var selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            if (selected != null && selected.name.StartsWith("AcademyFormation"))
            {
                var forms = new[] { "4-3-3", "4-2-3-1", "4-4-2", "3-4-3", "5-4-1" };
                var group = AcademyFactory.AgeGroups[Mathf.Clamp(_academyAgeIndex, 0, AcademyFactory.AgeGroups.Length - 1)];
                var tactics = AcademyTactics(group);
                var current = Array.IndexOf(forms, tactics == null ? "" : tactics.Formation);
                if (current < 0)
                    current = 0;
                var next = Mathf.Clamp(current + direction, 0, forms.Length - 1);
                if (next != current && tactics != null)
                {
                    tactics.Formation = forms[next];
                    tactics.Summary = forms[next] + " · " + tactics.Pressing + " · " + tactics.BuildUp;
                    CareerStore.Save(_career);
                    FillAcademy();
                    GameAudio.Click();
                }
                return;
            }

            var ages = AcademyFactory.AgeGroups.Length;
            var slot = _academyViewFocused ? ages + _academyViewIndex : _academyAgeIndex;
            var nextSlot = Mathf.Clamp(slot + direction, 0, ages + 2);
            if (nextSlot == slot)
                return;
            if (nextSlot < ages)
            {
                _academyViewFocused = false;
                _academyAgeIndex = nextSlot;
            }
            else
            {
                _academyViewFocused = true;
                _academyViewIndex = nextSlot - ages;
            }
            FillAcademy();
            GameAudio.Click();
        }

        void BumpTeamTab(int direction)
        {
            var paneIds = new[] { 1, 3 };
            GameAudio.Click();
            var slots = paneIds.Length + 1;
            var index = _teamZone == 0 ? System.Array.IndexOf(paneIds, _pane) : paneIds.Length;
            if (index < 0)
                index = 0;
            var next = index + direction;
            if (next < 0 || next >= slots)
                return;
            if (next >= paneIds.Length)
                ShowTeamZone(1);
            else
            {
                if (_teamZone != 0)
                    ShowTeamZone(0);
                SelectPane(paneIds[next]);
            }
        }

        void Show(int index)
        {
            _hubPage = index;
            var tacticsSheet = transform.Find("TacticsSheet");
            if (tacticsSheet != null && index != 3)
                Destroy(tacticsSheet.gameObject);
            var contractSheet = transform.Find("ContractSheet");
            if (contractSheet != null)
                Destroy(contractSheet.gameObject);
            var transferSheet = transform.Find("TransferSheet");
            if (transferSheet != null)
                Destroy(transferSheet.gameObject);
            if (index == 2)
            {
                FillAcademy();
                _academyLayoutPassesRemaining = 2;
            }
            if (index == 4) FillTraining();
            if (index == 5) ShowTransfersZone(_transferPage);
            if (index == 1)
                FillNews();
            if (index == 6)
                ShowOfficeTab(_officeTab);
            if (index == 7)
                FillStandings();
            for (var i = 0; i < _pages.Length; i++)
            {
                _pages[i].SetActive(i == index);
                if (_tabs == null || i >= _tabs.Length || _tabs[i] == null) continue;
                var label = _tabs[i].GetComponentInChildren<Text>();
                label.color = i == index ? UiTheme.ButtonText : Color.white;
                label.fontStyle = i == index ? FontStyle.Bold : FontStyle.Normal;
                _tabs[i].GetComponent<Image>().color = i == index ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.06f);
            }

            if (_tabBar != null)
                _tabBar.SetAsLastSibling();
            if (_play != null)
            {
                _play.gameObject.SetActive(index == 0);
                _play.transform.SetAsLastSibling();
            }
            if (_modal != null && _modal.activeSelf)
                _modal.transform.SetAsLastSibling();
        }

        void FillStandings()
        {
            SeasonSchedule.RebuildTable(_career);
            Clear(_tableContent);
            var scroll = BroadcastUi.Scroll(_tableContent, "LeagueScroll", out var content);
            BroadcastUi.Stretch(scroll.GetComponent<RectTransform>());
            var leagues = new List<League>(_career.World.Leagues);
            const int columns = 2;
            for (var i = 0; i < leagues.Count; i++)
            {
                var rowIndex = i / columns;
                var row = content.Find("StandRow" + rowIndex) as RectTransform;
                if (row == null)
                {
                    row = BroadcastUi.Zone(content, "StandRow" + rowIndex, 0f, 0f, 1f, 1f);
                    BroadcastUi.Preferred(row, 280f);
                }
                var col = i % columns;
                var x0 = col == 0 ? 0.008f : 0.508f;
                var x1 = col == 0 ? 0.492f : 0.992f;
                var cell = BroadcastUi.Zone(row, "LeagueCell" + i, x0, 0.03f, x1, 0.97f);
                TableCard(cell, leagues[i].Id, 0f, 1f);
            }
        }

        void TableCard(Transform parent, string leagueId, float y0, float y1)
        {
            var card = BroadcastUi.Panel(parent, "T", new Color(0.06f, 0.02f, 0.14f, 0.92f), false);
            BroadcastUi.Place(card.rectTransform, 0.02f, y0, 0.98f, y1);
            var title = leagueId;
            foreach (var league in _career.World.Leagues)
            {
                if (league.Id == leagueId)
                    title = LocalizationManager.Translate(league.Name);
            }

            var head = BroadcastUi.Label(card.transform, "League", title.ToUpperInvariant(), 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            head.horizontalOverflow = HorizontalWrapMode.Wrap;
            head.verticalOverflow = VerticalWrapMode.Truncate;
            BroadcastUi.Place(head.rectTransform, 0.04f, 0.86f, 0.68f, 0.98f);
            var moreId = leagueId;
            var more = BroadcastUi.Button(card.transform, "More", "DETALHES", () => OpenLeagueStats(moreId), new Color(1f, 1f, 1f, 0.12f), Color.white, 12);
            BroadcastUi.Place(more.GetComponent<RectTransform>(), 0.7f, 0.86f, 0.97f, 0.98f);
            var siglas = BroadcastUi.Label(card.transform, "Siglas", "PONTOS", 11, UiTheme.Accent, TextAnchor.MiddleRight, FontStyle.Bold);
            BroadcastUi.Place(siglas.rectTransform, 0.7f, 0.78f, 0.96f, 0.85f);
            var rows = new List<LeagueTableRow>();
            foreach (var row in _career.Table)
            {
                if (row.LeagueId == leagueId)
                    rows.Add(row);
            }

            rows.Sort((a, b) =>
            {
                var points = b.Points.CompareTo(a.Points);
                if (points != 0)
                    return points;
                var diff = b.GoalDifference.CompareTo(a.GoalDifference);
                return diff != 0 ? diff : b.GoalsFor.CompareTo(a.GoalsFor);
            });
            var indexes = new List<int>();
            for (var i = 0; i < rows.Count && indexes.Count < 4; i++)
                indexes.Add(i);
            for (var i = 0; i < rows.Count; i++)
            {
                if ((rows[i].ClubId == _career.Club.Id || OtherHumanSeat(rows[i].ClubId) != null) && !indexes.Contains(i))
                    indexes.Add(i);
            }
            if (indexes.Count == 4 && rows.Count > 4)
                indexes.Add(4);
            var step = 0.62f / Mathf.Max(5, indexes.Count);
            for (var n = 0; n < indexes.Count; n++)
            {
                var i = indexes[n];
                var row = rows[i];
                var y = 0.74f - (n + 1) * step;
                var mineRow = row.ClubId == _career.Club.Id;
                var guest = OtherHumanSeat(row.ClubId);
                var marked = mineRow || guest != null;
                var tone = mineRow ? UiTheme.Score : guest != null ? BroadcastUi.Parse(guest.Color, new Color(0.88f, 0.48f, 0.24f, 1f)) : Color.white;
                var line = BroadcastUi.Zone(card.transform, "R" + i, 0.03f, y, 0.97f, y + step - 0.01f);
                Club club = null;
                if (_career.World.Clubs.ContainsKey(row.ClubId))
                    club = _career.World.Clubs[row.ClubId];
                var crest = CrestImage(line, club, 22f);
                BroadcastUi.Place(crest.rectTransform, 0.01f, 0.12f, 0.1f, 0.88f);
                crest.preserveAspect = true;
                var name = club == null ? row.ClubId : club.ShortName;
                if (club != null)
                {
                    var selectedClub = club;
                    var place = BroadcastUi.Label(line, "Place", (i + 1).ToString(), 20, tone, TextAnchor.MiddleCenter, FontStyle.Bold);
                    BroadcastUi.Place(place.rectTransform, 0.11f, 0.02f, 0.22f, 0.98f);
                    var clubButton = BroadcastUi.Button(line, "N", name, () => OpenClubDossier(selectedClub), new Color(1f, 1f, 1f, 0.01f), tone, 15);
                    var caption = clubButton.GetComponentInChildren<Text>();
                    caption.fontStyle = marked ? FontStyle.Bold : FontStyle.Normal;
                    caption.alignment = TextAnchor.MiddleLeft;
                    caption.horizontalOverflow = HorizontalWrapMode.Overflow;
                    caption.verticalOverflow = VerticalWrapMode.Overflow;
                    caption.resizeTextForBestFit = true;
                    caption.resizeTextMinSize = 11;
                    caption.resizeTextMaxSize = 15;
                    caption.rectTransform.offsetMin = Vector2.zero;
                    caption.rectTransform.offsetMax = Vector2.zero;
                    BroadcastUi.Place(clubButton.GetComponent<RectTransform>(), 0.22f, 0.04f, 0.74f, 0.96f);
                }
                else
                {
                    var place = BroadcastUi.Label(line, "Place", (i + 1).ToString(), 20, tone, TextAnchor.MiddleCenter, FontStyle.Bold);
                    BroadcastUi.Place(place.rectTransform, 0.11f, 0.02f, 0.22f, 0.98f);
                    var label = BroadcastUi.Label(line, "N", name, 15, tone, TextAnchor.MiddleLeft, marked ? FontStyle.Bold : FontStyle.Normal);
                    label.horizontalOverflow = HorizontalWrapMode.Wrap;
                    label.verticalOverflow = VerticalWrapMode.Truncate;
                    BroadcastUi.Place(label.rectTransform, 0.22f, 0.04f, 0.74f, 0.96f);
                }
                var numbers = BroadcastUi.Label(line, "S", row.Points + " pts", 14, tone, TextAnchor.MiddleRight, FontStyle.Bold);
                BroadcastUi.Place(numbers.rectTransform, 0.74f, 0.04f, 0.98f, 0.96f);
            }

            if (rows.Count > indexes.Count)
            {
                var rest = LocalizationManager.Current == GameLanguage.English
                    ? "+" + (rows.Count - indexes.Count) + " clubs in Details"
                    : "+" + (rows.Count - indexes.Count) + " clubes em Detalhes";
                var moreLine = BroadcastUi.Label(card.transform, "Rest", rest, 12, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Place(moreLine.rectTransform, 0.05f, 0.02f, 0.95f, 0.1f);
            }
        }

        void FillNews()
        {
            Clear(_newsContent);
            foreach (var story in Newsroom.Edition(_career))
            {
                var card = BroadcastUi.Panel(_newsContent, "Story", new Color(0.14f, 0.06f, 0.28f, 0.92f), true);
                BroadcastUi.Preferred(card, 118);
                var captured = story;
                BroadcastUi.Hit(card, () => OpenStory(captured));
                var paperLabel = string.IsNullOrEmpty(story.Kicker) ? PaperName(story.Paper) : PressKicker(story.Kicker) + "  ·  " + PaperName(story.Paper);
                var paper = BroadcastUi.Label(card.transform, "Paper", paperLabel.ToUpperInvariant(), 13, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(paper.rectTransform, 0.04f, 0.72f, 0.7f, 0.94f);
                var open = BroadcastUi.Label(card.transform, "Open", LocalizationManager.Translate("ABRIR"), 13, UiTheme.Score, TextAnchor.MiddleRight, FontStyle.Bold);
                BroadcastUi.Place(open.rectTransform, 0.7f, 0.72f, 0.96f, 0.94f);
                var headline = BroadcastUi.Label(card.transform, "Headline", PressHeadline(story), 20, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(headline.rectTransform, 0.04f, 0.34f, 0.96f, 0.72f);
                var deck = BroadcastUi.Label(card.transform, "Deck", PressDate(string.IsNullOrEmpty(story.Published) ? story.Deck : story.Published), 14, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Place(deck.rectTransform, 0.04f, 0.08f, 0.96f, 0.34f);
            }

            // Recalculate the full card stack before ScrollRect clamps its bounds. Without
            // this, late-created/replaced stories can leave the bottom of the list unreachable.
            if (_newsContent != null)
            {
                var newsScroll = _newsContent.GetComponentInParent<ScrollRect>();
                if (newsScroll != null)
                {
                    newsScroll.StopMovement();
                    newsScroll.horizontal = false;
                    newsScroll.vertical = true;
                    newsScroll.movementType = ScrollRect.MovementType.Clamped;
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_newsContent);
                    if (newsScroll.viewport != null)
                        LayoutRebuilder.ForceRebuildLayoutImmediate(newsScroll.viewport);
                    newsScroll.Rebuild(CanvasUpdate.PostLayout);
                    newsScroll.verticalNormalizedPosition = 1f;
                }
            }
        }

        string PaperName(string paper)
        {
            if (paper == "The Football Wire") return LocalizationManager.Current == GameLanguage.English ? paper : "A Voz do Futebol";
            if (LocalizationManager.Current != GameLanguage.English) return paper;
            if (paper == "A Gazeta do Relvado") return "The Touchline Gazette";
            if (paper == "O Bancada") return "The Stand";
            if (paper == "Diário da Liga") return "League Daily";
            if (paper == "Bastidores do Futebol") return "Football Behind the Scenes";
            return paper;
        }

        string PressKicker(string kicker)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            if (kicker == "GLOBAL") return english ? "WORLD FOOTBALL" : "FUTEBOL MUNDIAL";
            if (kicker == "GLOBAL TRANSFER") return english ? "TRANSFER CONFIRMED" : "TRANSFERÊNCIA CONFIRMADA";
            if (kicker != null && kicker.StartsWith("PERSONAL_", StringComparison.Ordinal))
                return english ? "PERSONAL MATTER" : "VIDA PESSOAL";
            return LocalizationManager.Translate(kicker);
        }

        string PressDate(string date)
        {
            if (LocalizationManager.Current != GameLanguage.English || string.IsNullOrEmpty(date))
                return date;
            return date.Replace("segunda-feira", "Monday").Replace("terça-feira", "Tuesday")
                .Replace("quarta-feira", "Wednesday").Replace("quinta-feira", "Thursday")
                .Replace("sexta-feira", "Friday").Replace("sábado", "Saturday").Replace("domingo", "Sunday")
                .Replace("janeiro", "January").Replace("fevereiro", "February").Replace("março", "March")
                .Replace("abril", "April").Replace("maio", "May").Replace("junho", "June")
                .Replace("julho", "July").Replace("agosto", "August").Replace("setembro", "September")
                .Replace("outubro", "October").Replace("novembro", "November").Replace("dezembro", "December")
                .Replace(" de ", " ");
        }

        string PressHeadline(PaperStory story)
        {
            if (story == null) return "";
            var english = LocalizationManager.Current == GameLanguage.English;
            if (story.Kicker == "JOGO" && english && !string.IsNullOrEmpty(story.EnglishHeadline))
                return story.EnglishHeadline;
            if (story.Kicker != null && story.Kicker.StartsWith("PERSONAL_", StringComparison.Ordinal))
                return english ? PersonalStoryHeadlineEnglish(story) : LocalizationManager.Translate(story.Headline);
            if (story.Kicker == "GLOBAL" && english && !string.IsNullOrEmpty(story.EnglishHeadline))
                return story.EnglishHeadline;
            if (story.Kicker == "GLOBAL TRANSFER")
            {
                var name = story.Motm ?? "the player";
                var from = story.AwayClubId != null && _career.World.Clubs.TryGetValue(story.AwayClubId, out var seller) ? seller.ShortName : "another club";
                var to = story.HomeClubId != null && _career.World.Clubs.TryGetValue(story.HomeClubId, out var buyer) ? buyer.ShortName : "a new club";
                return english ? name + " joins " + to + " from " + from : name + " assina pelo " + to + " vindo do " + from;
            }
            if (story.Kicker == "GLOBAL")
            {
                var globalFixture = PressFixture(story);
                if (globalFixture != null && _career.World.Clubs.TryGetValue(globalFixture.HomeClubId, out var home) && _career.World.Clubs.TryGetValue(globalFixture.AwayClubId, out var away))
                    return english ? "Around the leagues: " + home.ShortName + " " + globalFixture.HomeGoals + "–" + globalFixture.AwayGoals + " " + away.ShortName
                        : "Pelo mundo: " + home.ShortName + " " + globalFixture.HomeGoals + "–" + globalFixture.AwayGoals + " " + away.ShortName;
                return english ? story.Headline : "Futebol mundial: as histórias que estão a marcar a época";
            }
            if (!english)
                return LocalizationManager.Translate(story.Headline);
            if (story.Kicker == "JOGO")
            {
                var fixture = PressFixture(story);
                if (fixture != null)
                {
                    var home = _career.World.Clubs[fixture.HomeClubId];
                    var away = _career.World.Clubs[fixture.AwayClubId];
                    return "Match report: " + home.ShortName + " " + fixture.HomeGoals + "–" + fixture.AwayGoals + " " + away.ShortName;
                }
                var homeName = story.HomeClubId != null && _career.World.Clubs.TryGetValue(story.HomeClubId, out var homeClub) ? homeClub.ShortName : "Home side";
                var awayName = story.AwayClubId != null && _career.World.Clubs.TryGetValue(story.AwayClubId, out var awayClub) ? awayClub.ShortName : "Away side";
                return "Full-time report: " + homeName + " vs " + awayName;
            }
            if (story.Kicker == "ENTREVISTA")
            {
                var home = story.HomeClubId != null && _career.World.Clubs.TryGetValue(story.HomeClubId, out var h) ? h.ShortName : "Home side";
                var away = story.AwayClubId != null && _career.World.Clubs.TryGetValue(story.AwayClubId, out var a) ? a.ShortName : "Away side";
                return "Match preview: " + home + " vs " + away;
            }
            if (story.Kicker == "MERCADO")
            {
                var destination = story.HomeClubId != null && _career.World.Clubs.TryGetValue(story.HomeClubId, out var club) ? club.ShortName : _career.Club.ShortName;
                return "Transfer watch: " + story.Motm + " linked with " + destination;
            }
            if (story.Paper == "A Gazeta do Relvado")
                return "Manager " + _career.Coach.Name + " takes charge at " + _career.Club.ShortName;
            if (story.Paper == "O Bancada")
                return "Rivals set their sights on the new season";
            if (story.Paper == "Diário da Liga")
                return (_career.League == null ? "League" : LocalizationManager.Translate(_career.League.Name)) + ": the new season begins";
            return PaperName(story.Paper) + ": the latest club and league news";
        }

        string PressQuote(PaperStory story)
        {
            if (story == null)
                return "";
            if (story.Kicker != null && story.Kicker.StartsWith("PERSONAL_", StringComparison.Ordinal)
                && LocalizationManager.Current == GameLanguage.English)
                return "We are supporting the player and will handle the matter with care.";
            if (story.Kicker == "GLOBAL TRANSFER")
                return LocalizationManager.Current == GameLanguage.English
                    ? "The club moved quickly and got its deal over the line."
                    : "O clube avançou rapidamente e fechou o negócio.";
            if (LocalizationManager.Current != GameLanguage.English)
                return story == null ? "" : story.Quote;
            if (story.Kicker == "MERCADO") return "My focus is on helping the team. My representative can handle any contact with the club.";
            if (story.Kicker == "ENTREVISTA") return "If the ball reaches me in the box, I won't think twice.";
            return "We went looking for the goal. The result reflects what we saw from the touchline.";
        }

        string PressBody(PaperStory story)
        {
            if (story == null) return "";
            var english = LocalizationManager.Current == GameLanguage.English;
            if (story.Kicker != null && story.Kicker.StartsWith("PERSONAL_", StringComparison.Ordinal))
                return PersonalStoryBody(story, english);
            if (story.Kicker == "JOGO")
            {
                if (english && !string.IsNullOrEmpty(story.EnglishBody))
                    return story.EnglishBody;
                var match = PressFixture(story);
                if (match != null)
                {
                    var home = _career.World.Clubs[match.HomeClubId];
                    var away = _career.World.Clubs[match.AwayClubId];
                    var userGoals = match.IsHome(_career.Club.Id) ? match.HomeGoals : match.AwayGoals;
                    var opponentGoals = match.IsHome(_career.Club.Id) ? match.AwayGoals : match.HomeGoals;
                    var competition = LocalizationManager.Translate(SeasonSchedule.Competition(match.LeagueId));
                    var resultLine = userGoals > opponentGoals
                        ? (english ? "The result rewards the side that found the winning goal." : "O resultado premeia a equipa que encontrou o golo da vitória.")
                        : userGoals < opponentGoals
                            ? (english ? "The opposition left with the points after scoring more goals." : "O adversário saiu com os pontos depois de marcar mais golos.")
                            : (english ? "Neither side managed to separate itself on the scoreboard." : "Nenhuma das equipas conseguiu desfazer a igualdade no marcador.");
                    return competition + " · " + home.Name + " " + match.HomeGoals + "–" + match.AwayGoals + " " + away.Name + ".\n\n"
                        + resultLine + (string.IsNullOrEmpty(story.Motm) ? "" : "\n\n" + (english ? "Player of the match: " : "Jogador da partida: ") + story.Motm + ".");
                }
                return english ? "The match report records the final result and the main events from the fixture."
                    : "O relatório regista o resultado final e os principais acontecimentos da partida.";
            }
            if (story.Kicker == "GLOBAL TRANSFER")
            {
                var playerName = story.Motm ?? (english ? "The player" : "O jogador");
                var from = story.AwayClubId != null && _career.World.Clubs.TryGetValue(story.AwayClubId, out var seller) ? seller : null;
                var to = story.HomeClubId != null && _career.World.Clubs.TryGetValue(story.HomeClubId, out var buyer) ? buyer : null;
                var transfer = FindTransferRecord(story);
                if (english)
                    return playerName + " completed a move from " + (from == null ? "another club" : from.Name) + " to " + (to == null ? "a new club" : to.Name) + ".\n\n"
                        + "Fee: " + (transfer == null ? "undisclosed" : Euro(transfer.Fee)) + " · Weekly wage: " + (transfer == null ? "undisclosed" : Euro(transfer.WeeklyWage))
                        + " · Contract: " + (transfer == null ? "undisclosed" : transfer.ContractYears + " years") + ". The signing attracted interest from several clubs.";
                return playerName + " mudou-se do " + (from == null ? "clube anterior" : from.Name) + " para o " + (to == null ? "novo clube" : to.Name) + ".\n\n"
                    + "Valor: " + (transfer == null ? "não divulgado" : Euro(transfer.Fee)) + " · Salário semanal: " + (transfer == null ? "não divulgado" : Euro(transfer.WeeklyWage))
                    + " · Contrato: " + (transfer == null ? "não divulgado" : transfer.ContractYears + " anos") + ". Vários clubes acompanharam a contratação.";
            }
            if (story.Kicker == "GLOBAL")
            {
                if (english && !string.IsNullOrEmpty(story.EnglishBody))
                    return story.EnglishBody;
                if (!english && !string.IsNullOrEmpty(story.Body))
                    return story.Body;
                var fixture = PressFixture(story);
                if (fixture != null && _career.World.Clubs.TryGetValue(fixture.HomeClubId, out var home) && _career.World.Clubs.TryGetValue(fixture.AwayClubId, out var away))
                    return english
                        ? home.Name + " and " + away.Name + " played out a " + fixture.HomeGoals + "–" + fixture.AwayGoals + " result. The points shift the league picture and keep the wider title and survival races moving."
                        : home.Name + " e " + away.Name + " terminaram com " + fixture.HomeGoals + "–" + fixture.AwayGoals + ". Os pontos mexem com a classificação e mantêm em aberto as lutas pelo título e pela permanência.";
                var featured = story.HomeClubId != null && _career.World.Clubs.TryGetValue(story.HomeClubId, out var globalClub) ? globalClub : null;
                return english
                    ? (featured == null ? "Clubs across the leagues" : featured.Name) + " are drawing attention as the season develops. Results, recruitment and finances will shape the wider football picture."
                    : (featured == null ? "Vários clubes das ligas" : featured.Name) + " estão a chamar a atenção nesta fase da época. Resultados, contratações e finanças vão moldar o futebol para lá do teu clube.";
            }
            if (!english)
                return LocalizationManager.Translate(story.Body);
            if (story.Kicker == "ENTREVISTA")
            {
                var opponent = story.AwayClubId != null && _career.World.Clubs.TryGetValue(story.AwayClubId, out var away) ? away : null;
                return _career.Club.Name + " face " + (opponent == null ? "their next opponent" : opponent.Name) + ". The opposition are expected to use "
                    + (opponent == null ? "their usual shape" : opponent.Tactics.Formation) + ".\n\nThe assistant has highlighted " + (story.Motm ?? "a key player") + " as a possible difference-maker.";
            }
            if (story.Kicker == "MERCADO")
            {
                var seller = story.AwayClubId != null && _career.World.Clubs.TryGetValue(story.AwayClubId, out var owner) ? owner : null;
                return "Transfer talk around " + (story.Motm ?? "the player") + " has resurfaced. "
                    + (seller == null ? "The discussions are being handled privately, and no agreement has been announced." : seller.Name + " have made no public announcement, and no agreement has been announced.")
                    + " The player's camp has declined to discuss details while the situation remains informal.";
            }
            var next = _career.NextUserFixture();
            var opponentClub = next == null ? null : _career.World.Clubs[next.OpponentId(_career.Club.Id)];
            var nextText = opponentClub == null ? "No match has been scheduled yet." : "The next fixture is against " + opponentClub.Name + ".";
            return _career.Club.Name + " begin the season with " + _career.Coach.Name + " in charge. " + nextText
                + " The squad's tactical identity is " + LocalizationManager.Translate(_career.Club.Tactics.Summary) + ". The board expects the club to keep progressing.";
        }

        static string PersonalStoryHeadlineEnglish(PaperStory story)
        {
            var name = story.Motm ?? "A player";
            switch (story.Kicker.Substring("PERSONAL_".Length))
            {
                case "BEREAVEMENT": return name + " receives club support after a family bereavement";
                case "BETTING": return "Club investigates a possible betting breach involving " + name;
                case "DRUNK": return name + " arrives tired after a long night out";
                case "VIRAL": return name + "'s 2-2-6 team sheet goes viral";
                case "FAMILY": return name + " celebrates a new arrival in the family";
                case "CHARITY": return name + " surprises a local charity with a visit";
                default: return "Club supports " + name + " through a personal matter";
            }
        }

        static string PersonalStoryBody(PaperStory story, bool english)
        {
            var name = story.Motm ?? (english ? "The player" : "O jogador");
            var type = story.Kicker.Substring("PERSONAL_".Length);
            string body;
            if (english)
            {
                switch (type)
                {
                    case "BEREAVEMENT": body = name + " has suffered a family bereavement on matchday. The club has made support available and respected the player's need to be with family."; break;
                    case "BETTING": body = "The club is reviewing an integrity concern involving " + name + ". No conclusion has been reached, and the player is entitled to a fair process."; break;
                    case "DRUNK": body = name + " arrived late and visibly tired after a night out. The staff checked on the player's wellbeing and adjusted the response to the circumstances."; break;
                    case "VIRAL": body = "A joke team sheet showing a 2-2-6 formation made the rounds online. The squad took it in good humour and returned to match preparation."; break;
                    case "FAMILY": body = name + " has welcomed a new baby. The club adjusted the day's workload so the player could balance family and preparation."; break;
                    case "CHARITY": body = name + " arranged a surprise visit to a local charity and brought equipment for the children. The club praised the initiative."; break;
                    default: body = "The club is supporting " + name + " through a personal matter."; break;
                }
                return body + "\n\nThe manager chose a response focused on the player and the squad.";
            }

            switch (type)
            {
                case "BEREAVEMENT": body = name + " sofreu uma perda familiar no dia do jogo. O clube disponibilizou apoio e respeitou a necessidade de estar com a família."; break;
                case "BETTING": body = "O clube está a analisar uma questão de integridade que envolve " + name + ". Ainda não há conclusões e o jogador tem direito a um processo justo."; break;
                case "DRUNK": body = name + " chegou atrasado e visivelmente cansado após uma noite longa. A equipa médica verificou o seu estado e o staff ajustou a resposta."; break;
                case "VIRAL": body = "Uma brincadeira com uma formação 2-2-6 espalhou-se online. O balneário levou a situação com humor e regressou à preparação."; break;
                case "FAMILY": body = name + " recebeu um novo bebé na família. O clube ajustou a carga do dia para conciliar a família e a preparação."; break;
                case "CHARITY": body = name + " organizou uma visita surpresa a uma associação local e levou equipamento para as crianças. O clube elogiou a iniciativa."; break;
                default: body = "O clube está a apoiar " + name + " numa situação pessoal."; break;
            }
            return body + "\n\nA resposta do treinador procurou proteger o jogador e o grupo.";
        }

        TransferRecord FindTransferRecord(PaperStory story)
        {
            foreach (var record in _career.TransferHistory)
                if (record.FromClubId == story.AwayClubId && record.ToClubId == story.HomeClubId
                    && _career.World.Players.TryGetValue(record.PlayerId ?? "", out var player) && player.FullName == story.Motm)
                    return record;
            return null;
        }

        Fixture PressFixture(PaperStory story)
        {
            var reportDate = story.DateTicks > 0 ? new DateTime(story.DateTicks).Date : DateTime.MinValue;
            Fixture latestBeforeDate = null;
            foreach (var fixture in _career.Fixtures)
            {
                if (!fixture.Played || fixture.HomeClubId != story.HomeClubId || fixture.AwayClubId != story.AwayClubId)
                    continue;
                if (story.Kicker == "JOGO")
                {
                    if (reportDate == DateTime.MinValue || fixture.Date.Date == reportDate)
                        return fixture;
                    continue;
                }
                if (reportDate != DateTime.MinValue && fixture.Date.Date > reportDate)
                    continue;
                if (latestBeforeDate == null || fixture.Date > latestBeforeDate.Date)
                    latestBeforeDate = fixture;
            }
            return latestBeforeDate;
        }

        void OpenMemories()
        {
            EnsureLandmarkMemories();
            var english = LocalizationManager.Current == GameLanguage.English;
            var memories = new List<FootballMemory>();
            foreach (var memory in _career.Memories)
            {
                if (memory != null && MemoryOnShelf(memory, _memoryShelf))
                    memories.Add(memory);
            }
            memories.Sort((a, b) => b.WhenTicks.CompareTo(a.WhenTicks));
            OpenModal(english ? "CAREER MEMORIES" : "MEMÓRIAS DA CARREIRA", memories.Count + (english ? " in this shelf" : " nesta prateleira"), false);
            var labels = english
                ? new[] { "ALL", "IMPORTANT", "TITLES", "MATCHES", "TRANSFERS", "SQUAD" }
                : new[] { "TODAS", "IMPORTANTES", "TÍTULOS", "JOGOS", "TRANSFERÊNCIAS", "PLANTEL" };
            for (var shelf = 0; shelf < labels.Length; shelf++)
            {
                var captured = shelf;
                AddShelfButton(labels[shelf], captured == _memoryShelf, () =>
                {
                    _memoryShelf = captured;
                    OpenMemories();
                });
            }
            if (memories.Count == 0)
            {
                AddLine(english
                    ? "Nothing on this shelf yet."
                    : "Ainda não há nada nesta prateleira.");
                return;
            }

            for (var i = 0; i < memories.Count; i++)
            {
                var memory = memories[i];
                var date = memory.WhenTicks > 0 && memory.WhenTicks <= DateTime.MaxValue.Ticks
                    ? LocalizationManager.FormatDate(new DateTime(memory.WhenTicks), "d MMM yyyy")
                    : english ? "Date unavailable" : "Data não disponível";
                var club = _career.World.Clubs.TryGetValue(memory.ClubId ?? "", out var memoryClub) ? memoryClub.ShortName : _career.Club.ShortName;
                var importance = new string('★', Mathf.Clamp(memory.Importance, 1, 5));
                var memoryTitle = LocalizationManager.Translate(memory.Title ?? "");
                var memoryDetail = LocalizationManager.Translate(memory.Detail ?? "");
                if (english)
                {
                    memoryTitle = memoryTitle.Replace("Melhor jogo:", "Best match:").Replace("Pior jogo:", "Worst match:")
                        .Replace(" frente a ", " against ");
                    memoryDetail = memoryDetail.Replace("A maior vantagem da carreira", "Biggest win of the career")
                        .Replace("A derrota mais pesada da carreira", "Heaviest defeat of the career")
                        .Replace(" contra ", " against ");
                }
                var competition = string.IsNullOrEmpty(memory.CompetitionId)
                    ? "" : "  ·  " + LocalizationManager.Translate(SeasonSchedule.Competition(memory.CompetitionId));
                AddHead(MemoryShelfName(memory, english) + "   " + importance + "   " + memoryTitle);
                AddLine(date + "  ·  " + club + competition + "\n" + memoryDetail);
            }
        }

        static bool MemoryOnShelf(FootballMemory memory, int shelf)
        {
            if (memory == null)
                return false;
            if (shelf <= 1)
                return MemoryIsLandmark(memory);
            var group = MemoryGroup(memory);
            if (shelf == 2)
                return group == "titulos";
            if (shelf == 3)
                return group == "jogos" && MemoryIsLandmark(memory);
            if (shelf == 4)
                return group == "transferencias" && memory.Importance >= 5;
            return group == "plantel" && memory.Importance >= 5;
        }

        static bool MemoryIsLandmark(FootballMemory memory)
        {
            if (memory == null)
                return false;
            var kind = (memory.Kind ?? "").ToLowerInvariant();
            if (kind.Contains("titulo") || kind.Contains("melhor-jogo") || kind.Contains("pior-jogo") || kind.Contains("taca") || kind.Contains("surpresa") || kind.Contains("vitoria-tardia"))
                return true;
            if (memory.Importance >= 5)
                return true;
            if (memory.Importance >= 4 && MemoryGroup(memory) == "titulos")
                return true;
            var title = (memory.Title ?? "").ToLowerInvariant();
            return title.Contains("campe") || title.Contains("título") || title.Contains("titulo") || title.Contains("taça") || title.Contains("taca");
        }

        static string MemoryGroup(FootballMemory memory)
        {
            var kind = (memory.Kind ?? "").ToLowerInvariant();
            if (kind.Contains("titulo"))
                return "titulos";
            if (kind.Contains("transfer") || kind.Contains("venda") || kind.Contains("contrat") || kind.Contains("mudanca") || kind.Contains("emprest"))
                return "transferencias";
            if (kind.Contains("match") || kind.Contains("jogo") || kind.Contains("golo") || kind.Contains("resultado"))
                return "jogos";
            return "plantel";
        }

        static string MemoryShelfName(FootballMemory memory, bool english)
        {
            switch (MemoryGroup(memory))
            {
                case "titulos": return english ? "TITLE" : "TÍTULO";
                case "jogos": return english ? "MATCH" : "JOGO";
                case "transferencias": return english ? "TRANSFER" : "TRANSFERÊNCIA";
                default: return english ? "SQUAD" : "PLANTEL";
            }
        }

        void FillBoard()
        {
            Clear(_boardContent);
            if (_boardContent == null)
                return;
            var english = LocalizationManager.Current == GameLanguage.English;
            var today = _career.Calendar.CurrentDate.Date;

            var digest = BroadcastUi.Panel(_boardContent, "Digest", new Color(0.1f, 0.28f, 0.34f, 0.92f));
            BroadcastUi.Preferred(digest, 150);
            var head = BroadcastUi.Label(digest.transform, "Head", english ? "TODAY'S STATUS" : "ESTADO DO DIA", 15, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(head.rectTransform, 0.04f, 0.72f, 0.7f, 0.96f);
            var dayName = BroadcastUi.Label(digest.transform, "When", LocalizationManager.FormatDate(today, "dddd, d MMMM"), 13, Color.white, TextAnchor.MiddleRight);
            BroadcastUi.Place(dayName.rectTransform, 0.45f, 0.72f, 0.96f, 0.96f);
            var body = BroadcastUi.Label(digest.transform, "Body", ClubDigest(), 15, Color.white, TextAnchor.UpperLeft);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;
            BroadcastUi.Place(body.rectTransform, 0.04f, 0.08f, 0.96f, 0.7f);

            var weekBar = BroadcastUi.Panel(_boardContent, "WeekBar", new Color(0.12f, 0.05f, 0.22f, 0.96f));
            BroadcastUi.Preferred(weekBar, 116);
            var weekHead = BroadcastUi.Label(weekBar.transform, "Head", english ? "WEEK STATUS" : "ESTADO DA SEMANA", 15, UiTheme.Score, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(weekHead.rectTransform, 0.04f, 0.69f, 0.62f, 0.94f);
            var openWeek = BroadcastUi.Button(weekBar.transform, "OpenWeek", english ? "OPEN" : "ABRIR", OpenWeekBoard, UiTheme.Accent, UiTheme.ButtonText, 14);
            BroadcastUi.Place(openWeek.GetComponent<RectTransform>(), 0.72f, 0.68f, 0.96f, 0.94f);
            var weekReport = BroadcastUi.Label(weekBar.transform, "Review", WeekReport(), 12, Color.white, TextAnchor.UpperLeft);
            weekReport.horizontalOverflow = HorizontalWrapMode.Wrap;
            weekReport.verticalOverflow = VerticalWrapMode.Truncate;
            weekReport.resizeTextForBestFit = true;
            weekReport.resizeTextMinSize = 10;
            weekReport.resizeTextMaxSize = 12;
            BroadcastUi.Place(weekReport.rectTransform, 0.04f, 0.08f, 0.96f, 0.64f);
        }

        void OpenWeekBoard()
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var moment = _career.Current;
            var talking = moment != null && moment.Kind == MomentKind.Talk && !moment.Answered && moment.Replies != null;
            var today = _career.Calendar.CurrentDate.Date;
            var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
            if (_boardWeekOffset > 0) _boardWeekOffset = 0;
            if (_boardWeekOffset < -16) _boardWeekOffset = -16;
            var weekStart = monday.AddDays(_boardWeekOffset * 7);
            if (_boardWeekDay < 0 || _boardWeekDay > 6)
                _boardWeekDay = ((int)today.DayOfWeek + 6) % 7;
            var selected = weekStart.AddDays(_boardWeekDay);
            OpenModal(english ? "WEEK STATUS" : "ESTADO DA SEMANA", "", false);
            SizeModal(0.14f, 0.1f, 0.86f, 0.9f);
            if (_modalTitle != null) _modalTitle.gameObject.SetActive(false);
            if (_modalLead != null) _modalLead.gameObject.SetActive(false);
            var scroll = _modal.transform.Find("Card/Scroll");
            if (scroll != null)
                scroll.gameObject.SetActive(false);
            var card = _modal.transform.Find("Card");
            var report = BroadcastUi.Zone(card, "WeekBody", 0.03f, 0.05f, 0.97f, 0.96f);
            var reportHead = BroadcastUi.Label(report, "Head", english ? "WEEK" : "ESTADO DA SEMANA", 15, UiTheme.Score, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(reportHead.rectTransform, 0.04f, 0.88f, 0.42f, 0.98f);
            var prev = BroadcastUi.Button(report.transform, "PrevWeek", "‹", () => { _boardWeekOffset--; OpenWeekBoard(); }, new Color(1f, 1f, 1f, 0.08f), Color.white, 18);
            BroadcastUi.Place(prev.GetComponent<RectTransform>(), 0.5f, 0.88f, 0.58f, 0.98f);
            var weekTitle = BroadcastUi.Label(report.transform, "WeekTitle", LocalizationManager.FormatDate(weekStart, "d MMM") + "  –  " + LocalizationManager.FormatDate(weekStart.AddDays(6), "d MMM"), 13, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(weekTitle.rectTransform, 0.58f, 0.88f, 0.86f, 0.98f);
            var next = BroadcastUi.Button(report.transform, "NextWeek", "›", () => { if (_boardWeekOffset < 0) { _boardWeekOffset++; OpenWeekBoard(); } }, new Color(1f, 1f, 1f, 0.08f), Color.white, 18);
            next.interactable = _boardWeekOffset < 0;
            BroadcastUi.Place(next.GetComponent<RectTransform>(), 0.87f, 0.88f, 0.96f, 0.98f);
            var dayNames = english
                ? new[] { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" }
                : new[] { "SEG", "TER", "QUA", "QUI", "SEX", "SÁB", "DOM" };
            for (var i = 0; i < 7; i++)
            {
                var day = i;
                var on = day == _boardWeekDay;
                var chip = BroadcastUi.Button(report.transform, "Day" + i, dayNames[i] + "\n" + weekStart.AddDays(i).Day, () =>
                {
                    _boardWeekDay = day;
                    OpenWeekBoard();
                }, on ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), on ? UiTheme.ButtonText : Color.white, 11);
                var chipText = chip.GetComponentInChildren<Text>();
                if (chipText != null)
                {
                    chipText.alignment = TextAnchor.MiddleCenter;
                    chipText.horizontalOverflow = HorizontalWrapMode.Wrap;
                    chipText.resizeTextForBestFit = true;
                    chipText.resizeTextMinSize = 9;
                    chipText.resizeTextMaxSize = 12;
                }
                var x0 = 0.03f + i * 0.138f;
                BroadcastUi.Place(chip.GetComponent<RectTransform>(), x0, 0.74f, x0 + 0.128f, 0.86f);
            }

            if (talking && selected == today)
            {
                var talkTitle = BroadcastUi.Label(report.transform, "LiveTitle", CareerMomentTitle(moment), 13, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(talkTitle.rectTransform, 0.04f, 0.62f, 0.7f, 0.72f);
                var talkBody = BroadcastUi.Label(report.transform, "LiveBody", CareerMomentText(moment), 12, Color.white, TextAnchor.UpperLeft);
                talkBody.horizontalOverflow = HorizontalWrapMode.Wrap;
                talkBody.verticalOverflow = VerticalWrapMode.Truncate;
                BroadcastUi.Place(talkBody.rectTransform, 0.04f, 0.28f, 0.96f, 0.62f);
                var hint = BroadcastUi.Label(report, "TalkHint", english ? "Continue opens the answers." : "Continuar abre as respostas.", 13, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(hint.rectTransform, 0.04f, 0.16f, 0.96f, 0.26f);
            }
            var conversations = talking && selected == today ? new List<PhoneNote>() : BoardConversations(selected, selected == today);
            var room = 0.7f;
            var shown = talking && selected == today ? 0 : Mathf.Min(4, conversations.Count);
            if (shown == 0 && !(talking && selected == today))
            {
                var empty = BroadcastUi.Label(report.transform, "EmptyTalks", english ? "No conversations on this day." : "Não há conversas neste dia.", 14, UiTheme.Muted, TextAnchor.MiddleCenter);
                BroadcastUi.Place(empty.rectTransform, 0.06f, 0.28f, 0.94f, 0.62f);
            }
            else
            {
                var step = room / shown;
                for (var i = 0; i < shown; i++)
                {
                    var y1 = 0.72f - i * step;
                    var line = BroadcastUi.Panel(report.transform, "Talk" + i, new Color(1f, 1f, 1f, 0.05f));
                    BroadcastUi.Place(line.rectTransform, 0.03f, y1 - step + 0.012f, 0.97f, y1 - 0.006f);
                    var who = BroadcastUi.Label(line.transform, "Who", conversations[i].Who, 13, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                    BroadcastUi.Place(who.rectTransform, 0.03f, 0.5f, 0.97f, 0.95f);
                    var said = BroadcastUi.Label(line.transform, "Text", conversations[i].Text, 12, Color.white, TextAnchor.MiddleLeft);
                    said.horizontalOverflow = HorizontalWrapMode.Wrap;
                    said.verticalOverflow = VerticalWrapMode.Truncate;
                    BroadcastUi.Place(said.rectTransform, 0.03f, 0.06f, 0.97f, 0.5f);
                }
            }
        }

        List<PhoneNote> BoardConversations(DateTime day, bool includeTodayPlayers)
        {
            var lines = new List<PhoneNote>();
            if (includeTodayPlayers)
            {
                foreach (var note in PhoneNotes())
                    if (note.Channel == "jogador")
                        lines.Add(new PhoneNote(note.Who, LocalizationManager.Translate(note.Text), note.Channel,
                            note.PlayerId, note.ClubId, note.ReplyGroup, note.Meta));
            }
            foreach (var note in _career.Board)
            {
                if (note == null || string.IsNullOrEmpty(note.Text))
                    continue;
                var when = note.When == default ? _career.Calendar.CurrentDate : note.When;
                if (when.Date != day.Date)
                    continue;
                var text = note.Text.StartsWith("✉") ? note.Text.Substring(1).Trim() : note.Text;
                var marker = text.LastIndexOf("[[", StringComparison.Ordinal);
                if (marker >= 0 && text.EndsWith("]]", StringComparison.Ordinal))
                    text = text.Substring(0, marker).Trim();
                text = LocalizationManager.Translate(text);
                var english = LocalizationManager.Current == GameLanguage.English;
                var boardLabel = string.IsNullOrEmpty(note.Hour) ? (english ? "Board" : "Quadro") : note.Hour;
                lines.Add(new PhoneNote(boardLabel, text, "quadro", null, _career.Club.Id));
                if (lines.Count >= 8)
                    break;
            }
            return lines;
        }

        void FillCareerRecord()
        {
            Clear(_careerRecordContent);
            if (_careerRecordContent == null || _career == null)
                return;
            AddCareerRecord();
        }

        void AddCareerRecord()
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var panel = BroadcastUi.Panel(_careerRecordContent, "CareerRecord", new Color(0.07f, 0.16f, 0.2f, 0.94f));
            BroadcastUi.Preferred(panel, 220);
            var heading = BroadcastUi.Label(panel.transform, "Heading", english ? "CAREER RECORD" : "REGISTO DA CARREIRA", 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.025f, 0.72f, 0.975f, 0.96f);
            var played = 0;
            var wins = 0;
            var draws = 0;
            var losses = 0;
            foreach (var fixture in _career.Fixtures)
            {
                if (!fixture.Played || fixture.Walkover || fixture.HomeClubId == fixture.AwayClubId || !fixture.Involves(_career.Club.Id)) continue;
                played++;
                var forGoals = fixture.IsHome(_career.Club.Id) ? fixture.HomeGoals : fixture.AwayGoals;
                var againstGoals = fixture.IsHome(_career.Club.Id) ? fixture.AwayGoals : fixture.HomeGoals;
                if (forGoals > againstGoals) wins++;
                else if (forGoals == againstGoals) draws++;
                else losses++;
            }
            var detail = english
                ? "Seasons managed\n" + _career.CompletedSeasons + "\n\nCurrent-club record\n" + wins + "–" + draws + "–" + losses + "  (" + played + " matches)\n\nSupporter approval  " + _career.SupporterApproval + "%\nCareer moments  " + _career.Memories.Count
                : "Épocas como treinador\n" + _career.CompletedSeasons + "\n\nRegisto no clube atual\n" + wins + "–" + draws + "–" + losses + "  (" + played + " jogos)\n\nAprovação dos adeptos  " + _career.SupporterApproval + "%\nMomentos  " + _career.Memories.Count;
            var body = BroadcastUi.Label(panel.transform, "Body", detail, 16, Color.white, TextAnchor.UpperLeft);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            BroadcastUi.Place(body.rectTransform, 0.025f, 0.12f, 0.78f, 0.7f);
            var memories = BroadcastUi.Button(panel.transform, "Memories", english ? "CAREER MEMORIES" : "MEMÓRIAS DA CARREIRA", OpenMemories,
                new Color(1f, 1f, 1f, 0.08f), UiTheme.Score, 12);
            BroadcastUi.Place(memories.GetComponent<RectTransform>(), 0.79f, 0.17f, 0.975f, 0.66f);
        }

        void AddOfficeSection(RectTransform target, string title, string detail, float height)
        {
            if (target == null) return;
            var panel = BroadcastUi.Panel(target, title, new Color(0.07f, 0.16f, 0.2f, 0.94f));
            BroadcastUi.Preferred(panel, height);
            var heading = BroadcastUi.Label(panel.transform, "Heading", title, 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.025f, 0.72f, 0.975f, 0.96f);
            var body = BroadcastUi.Label(panel.transform, "Body", detail, 14, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.Place(body.rectTransform, 0.025f, 0.04f, 0.975f, 0.72f);
        }

        void FillOfficeBudget()
        {
            Clear(_officeBudgetContent);
            if (_officeBudgetContent == null) return;
            var english = LocalizationManager.Current == GameLanguage.English;
            var finance = _career.Club.Finance;
            if (finance == null)
            {
                var unavailable = BroadcastUi.Label(_officeBudgetContent, "Unavailable", english ? "Financial data is not available." : "Dados financeiros indisponíveis.", 16, UiTheme.Muted, TextAnchor.MiddleCenter);
                BroadcastUi.Preferred(unavailable, 70);
                return;
            }

            var current = finance.TransferBudget;
            var spent = finance.TransferSpentThisSeason;
            var total = Math.Max(0L, (long)current + spent);
            var overview = BroadcastUi.Panel(_officeBudgetContent, "BudgetOverview", new Color(0.07f, 0.12f, 0.2f, 0.98f));
            BroadcastUi.Preferred(overview, 350);

            var leftHead = BroadcastUi.Label(overview.transform, "CurrentHead", english ? "CURRENT BUDGET" : "ORÇAMENTO DISPONÍVEL", 13, UiTheme.Fitness, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(leftHead.rectTransform, 0.025f, 0.68f, 0.34f, 0.9f);
            var leftValue = BroadcastUi.Label(overview.transform, "CurrentValue", Euro(current), 24, UiTheme.Fitness, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(leftValue.rectTransform, 0.025f, 0.4f, 0.34f, 0.68f);
            var leftCaption = BroadcastUi.Label(overview.transform, "CurrentCaption", english ? "Available to spend" : "Disponível para gastar", 12, UiTheme.Muted, TextAnchor.MiddleCenter);
            BroadcastUi.Place(leftCaption.rectTransform, 0.025f, 0.24f, 0.34f, 0.4f);

            var rightHead = BroadcastUi.Label(overview.transform, "SpentHead", english ? "SPENT THIS SEASON" : "GASTO ESTA ÉPOCA", 13, UiTheme.Danger, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(rightHead.rectTransform, 0.66f, 0.68f, 0.975f, 0.9f);
            var rightValue = BroadcastUi.Label(overview.transform, "SpentValue", Euro(spent), 24, UiTheme.Danger, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(rightValue.rectTransform, 0.66f, 0.4f, 0.975f, 0.68f);
            var rightCaption = BroadcastUi.Label(overview.transform, "SpentCaption", english ? "Transfer fees" : "Taxas de transferências", 12, UiTheme.Muted, TextAnchor.MiddleCenter);
            BroadcastUi.Place(rightCaption.rectTransform, 0.66f, 0.24f, 0.975f, 0.4f);

            var ringObject = new GameObject("BudgetRing", typeof(RectTransform), typeof(CanvasRenderer), typeof(FinanceDonutGraphic));
            var ringRect = ringObject.GetComponent<RectTransform>();
            ringRect.SetParent(overview.transform, false);
            ringRect.anchorMin = ringRect.anchorMax = new Vector2(0.5f, 0.55f);
            ringRect.sizeDelta = new Vector2(320f, 320f);
            var ring = ringObject.GetComponent<FinanceDonutGraphic>();
            ring.SetValues(new[] { (float)Math.Max(0, current), (float)Math.Max(0, spent) },
                new[] { UiTheme.Fitness, UiTheme.Danger }, new Color(1f, 1f, 1f, 0.12f));
            ring.raycastTarget = false;
            var ringHit = BroadcastUi.Solid(overview.transform, "BudgetRingHit", new Color(0f, 0f, 0f, 0f), true);
            BroadcastUi.Place(ringHit.rectTransform, 0.5f, 0.55f, 0.5f, 0.55f);
            ringHit.rectTransform.sizeDelta = ringRect.sizeDelta;
            BroadcastUi.Hit(ringHit, ShowFinanceDetails);
            var totalHead = BroadcastUi.Label(overview.transform, "TotalHead", english ? "TOTAL" : "TOTAL", 13, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            totalHead.raycastTarget = false;
            BroadcastUi.Place(totalHead.rectTransform, 0.39f, 0.61f, 0.61f, 0.78f);
            var totalValue = BroadcastUi.Label(overview.transform, "TotalValue", Euro((int)Math.Min(int.MaxValue, total)), 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            totalValue.raycastTarget = false;
            BroadcastUi.Place(totalValue.rectTransform, 0.36f, 0.43f, 0.64f, 0.63f);
            var totalCaption = BroadcastUi.Label(overview.transform, "TotalCaption", english ? "Tap for breakdown" : "Clica para ver detalhes", 10, UiTheme.Muted, TextAnchor.MiddleCenter);
            totalCaption.raycastTarget = false;
            BroadcastUi.Place(totalCaption.rectTransform, 0.35f, 0.31f, 0.65f, 0.43f);

            AddOfficeSection(_officeBudgetContent, english ? "BOARD EXPECTATIONS" : "EXPECTATIVAS DA DIREÇÃO", BoardExpectations(english), 100);

            var scoutPanel = BroadcastUi.Panel(_officeBudgetContent, "Scouting", new Color(0.07f, 0.12f, 0.2f, 0.94f));
            BroadcastUi.Preferred(scoutPanel, 142);
            var scoutTitle = BroadcastUi.Label(scoutPanel.transform, "Title", english ? "SCOUT REPORTS" : "RELATÓRIOS DOS OLHEIROS", 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(scoutTitle.rectTransform, 0.025f, 0.79f, 0.975f, 0.98f);
            _scoutHost = BroadcastUi.Zone(scoutPanel.transform, "Reports", 0.01f, 0.05f, 0.99f, 0.78f);
            BuildScoutSummary(_scoutHost);
        }

        string BoardExpectations(bool english)
        {
            var objectives = _career.Club.BoardObjectives;
            if (objectives == null || objectives.Count == 0)
                return english ? "Keep the club stable and meet the season target." : "Manter o clube estável e cumprir os objetivos da época.";
            var translated = new List<string>();
            for (var i = 0; i < objectives.Count; i++)
                translated.Add(LocalizationManager.Translate(objectives[i]));
            return string.Join("\n", translated);
        }

        int[] FinanceEstimates()
        {
            var finance = _career.Club.Finance;
            var ticketIncome = finance == null ? 0 : finance.TicketIncomeThisSeason;
            var shirtIncome = finance == null ? 0 : finance.ShirtIncomeThisSeason;
            var sponsorIncome = finance == null ? 0 : finance.SponsorIncomeThisSeason;
            var seasonYear = _career.Calendar.CurrentDate.Month >= 7 ? _career.Calendar.CurrentDate.Year : _career.Calendar.CurrentDate.Year - 1;
            var seasonStart = new DateTime(seasonYear, 7, 1);
            long transferIncome = finance == null ? 0 : finance.PlayerSalesThisSeason;
            long transferCosts = finance == null ? 0 : finance.TransferSpentThisSeason;
            long otherIncome = 0;
            long wages = 0;
            long travel = 0;
            long otherExpenses = 0;
            if (finance != null)
                foreach (var transaction in finance.Transactions)
                {
                    if (transaction == null || transaction.DateTicks < seasonStart.Ticks || transaction.DateTicks > _career.Calendar.CurrentDate.Ticks)
                        continue;
                    if (transaction.Category == "loan-income" && transaction.Amount > 0)
                        transferIncome += transaction.Amount;
                    else if (transaction.Category == "loan-fee" && transaction.Amount < 0)
                        transferCosts -= transaction.Amount;
                    else if (transaction.Category == "wages" && transaction.Amount < 0)
                        wages -= transaction.Amount;
                    else if (transaction.Category == "travel" && transaction.Amount < 0)
                        travel -= transaction.Amount;
                    else if (transaction.Amount > 0 && transaction.Category != "ticket" && transaction.Category != "shirt"
                        && transaction.Category != "sponsor" && transaction.Category != "player-sale" && transaction.Category != "loan-income")
                        otherIncome += transaction.Amount;
                    else if (transaction.Amount < 0 && transaction.Category != "transfer-fee" && transaction.Category != "loan-fee")
                        otherExpenses -= transaction.Amount;
                }
            return new[]
            {
                ticketIncome,
                shirtIncome,
                sponsorIncome,
                (int)Math.Min(int.MaxValue, transferIncome),
                (int)Math.Min(int.MaxValue, otherIncome),
                (int)Math.Min(int.MaxValue, transferCosts),
                (int)Math.Min(int.MaxValue, wages),
                (int)Math.Min(int.MaxValue, travel),
                (int)Math.Min(int.MaxValue, otherExpenses)
            };
        }

        void ShowFinanceDetails()
        {
            ShowFinanceDetails(0);
        }

        void ShowFinanceDetails(int tab)
        {
            ShowFinanceDetails(tab, _financeMetricSelection);
        }

        void ShowFinanceDetails(int tab, int metric)
        {
            _financeMetricSelection = Mathf.Clamp(metric, 0, 2);
            var english = LocalizationManager.Current == GameLanguage.English;
            var finance = _career.Club.Finance;
            if (finance == null) return;
            var estimate = FinanceEstimates();
            OpenModal(english ? "FINANCE BREAKDOWN" : "DETALHE FINANCEIRO",
                english ? "Season accounts · " + _career.Club.Name : "Contas da época · " + _career.Club.Name, false);
            PrepareFinanceDashboard();
            var tabs = BroadcastUi.Zone(_modalBody, "FinanceTabs", 0.015f, 0.91f, 0.985f, 0.995f);
            var labels = english ? new[] { "OVERVIEW", "TRANSACTIONS", "BUDGET" } : new[] { "VISÃO GERAL", "TRANSAÇÕES", "ORÇAMENTO" };
            for (var i = 0; i < labels.Length; i++)
            {
                var selected = i;
                var button = BroadcastUi.Button(tabs, "FinanceTab" + i, labels[i], () => ShowFinanceDetails(selected),
                    i == tab ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), i == tab ? UiTheme.ButtonText : Color.white, 12);
                BroadcastUi.Place(button.GetComponent<RectTransform>(), i / 3f + 0.004f, 0.04f, (i + 1) / 3f - 0.004f, 0.96f);
            }

            var dashboard = BroadcastUi.Zone(_modalBody, "FinanceDashboard", 0.015f, 0.015f, 0.985f, 0.89f);
            var income = Math.Max(0L, (long)estimate[0] + estimate[1] + estimate[2] + estimate[3] + estimate[4]);
            var expenses = Math.Max(0L, (long)estimate[5] + estimate[6] + estimate[7] + estimate[8]);
            var net = income - expenses;
            if (tab == 0) BuildFinanceOverview(dashboard, finance, estimate, income, expenses, net, english, _financeMetricSelection);
            else if (tab == 1) BuildFinanceTransactions(dashboard, finance, english);
            else BuildFinanceBudget(dashboard, finance, english);
        }

        void PrepareFinanceDashboard()
        {
            var layout = _modalBody.GetComponent<VerticalLayoutGroup>();
            if (layout != null) layout.enabled = false;
            var fitter = _modalBody.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
            var scrollRect = _modal.transform.Find("Card/Scroll")?.GetComponent<ScrollRect>();
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
            }
            var body = _modalBody;
            body.anchorMin = new Vector2(0f, 1f);
            body.anchorMax = new Vector2(1f, 1f);
            body.pivot = new Vector2(0.5f, 1f);
            body.anchoredPosition = Vector2.zero;
            var viewportHeight = scrollRect != null && scrollRect.viewport != null ? scrollRect.viewport.rect.height : 0f;
            body.sizeDelta = new Vector2(0f, Mathf.Max(viewportHeight, 320f));
        }

        void BuildFinanceOverview(Transform host, ClubFinance finance, int[] estimate, long income, long expenses, long net, bool english, int selectedMetric)
        {
            var left = BroadcastUi.Zone(host, "SummaryCards", 0.005f, 0.035f, 0.31f, 0.985f);
            var metricTitles = english ? new[] { "PROFIT", "EARNINGS", "EXPENSES" } : new[] { "LUCRO", "RECEITAS", "DESPESAS" };
            var values = new[]
            {
                new { Value = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, net)), Color = net >= 0 ? UiTheme.Fitness : UiTheme.Danger },
                new { Value = (int)Math.Min(int.MaxValue, income), Color = UiTheme.Fitness },
                new { Value = (int)Math.Min(int.MaxValue, expenses), Color = UiTheme.Danger }
            };
            for (var i = 0; i < values.Length; i++)
            {
                var selected = i;
                var y1 = 0.995f - i / 3f;
                var y0 = 0.675f - i / 3f;
                var card = BroadcastUi.Panel(left, "FinanceMetric" + i, selectedMetric == i
                    ? new Color(0.91f, 0.92f, 0.9f, 1f) : new Color(1f, 1f, 1f, 0.11f), true);
                BroadcastUi.Place(card.rectTransform, 0.01f, y0, 0.99f, y1 - 0.015f);
                BroadcastUi.Hit(card, () => ShowFinanceDetails(0, selected));
                var activeText = selectedMetric == i ? new Color(0.08f, 0.09f, 0.1f) : Color.white;
                var title = BroadcastUi.Label(card.transform, "Title", metricTitles[i], 20, activeText, TextAnchor.MiddleLeft, FontStyle.Bold);
                title.raycastTarget = false;
                BroadcastUi.Place(title.rectTransform, 0.055f, 0.62f, 0.9f, 0.92f);
                var display = i == 2 ? -values[i].Value : values[i].Value;
                var amount = BroadcastUi.Label(card.transform, "Amount", SignedEuro(display), 23, values[i].Color, TextAnchor.MiddleLeft, FontStyle.Bold);
                amount.raycastTarget = false;
                BroadcastUi.Place(amount.rectTransform, 0.055f, 0.12f, 0.9f, 0.62f);
                var icon = BroadcastUi.Label(card.transform, "Icon", i == 0 ? "◉" : i == 1 ? "↑" : "↓", 28, new Color(0.08f, 0.09f, 0.1f, 0.28f), TextAnchor.MiddleRight, FontStyle.Bold);
                icon.raycastTarget = false;
                BroadcastUi.Place(icon.rectTransform, 0.72f, 0.24f, 0.95f, 0.72f);
            }

            var chart = BroadcastUi.Panel(host, "ProfitChart", new Color(0.055f, 0.075f, 0.1f, 0.97f));
            BroadcastUi.Place(chart.rectTransform, 0.325f, 0.035f, 0.995f, 0.985f);
            var heading = BroadcastUi.Label(chart.transform, "Heading", selectedMetric == 0
                ? (english ? "SEASON PROFIT" : "RESULTADO DA ÉPOCA")
                : selectedMetric == 1 ? (english ? "EARNINGS BREAKDOWN" : "DETALHE DAS RECEITAS")
                : (english ? "EXPENSES BREAKDOWN" : "DETALHE DAS DESPESAS"), 24, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.035f, 0.84f, 0.95f, 0.97f);
            var donutObject = new GameObject("IncomeExpenseDonut", typeof(RectTransform), typeof(CanvasRenderer), typeof(FinanceDonutGraphic));
            donutObject.transform.SetParent(chart.transform, false);
            var donutRect = donutObject.GetComponent<RectTransform>();
            donutRect.anchorMin = donutRect.anchorMax = new Vector2(0.5f, 0.55f);
            donutRect.sizeDelta = new Vector2(Mathf.Clamp(Screen.height * 0.22f, 150f, 230f), Mathf.Clamp(Screen.height * 0.22f, 150f, 230f));
            var categoryNames = selectedMetric == 0
                ? (english ? new[] { "Earnings", "Expenses" } : new[] { "Receitas", "Despesas" })
                : selectedMetric == 1
                    ? (english ? new[] { "Ticket sales", "Shirt revenue", "Sponsorship", "Transfer income", "Other income" } : new[] { "Bilheteira", "Receita de camisolas", "Patrocínios", "Receitas de transferências", "Outras receitas" })
                    : (english ? new[] { "Transfers", "Wages", "Away travel", "Other expenses" } : new[] { "Transferências", "Salários", "Viagens fora", "Outras despesas" });
            var categoryValues = selectedMetric == 0
                ? new[] { (float)income, (float)expenses }
                : selectedMetric == 1
                    ? new[] { (float)Math.Max(0, estimate[0]), (float)Math.Max(0, estimate[1]), (float)Math.Max(0, estimate[2]), (float)Math.Max(0, estimate[3]), (float)Math.Max(0, estimate[4]) }
                    : new[] { (float)Math.Max(0, estimate[5]), (float)Math.Max(0, estimate[6]), (float)Math.Max(0, estimate[7]), (float)Math.Max(0, estimate[8]) };
            var categoryColors = selectedMetric == 0
                ? new[] { UiTheme.Fitness, UiTheme.Danger }
                : selectedMetric == 1
                    ? new[] { new Color(0.16f, 0.78f, 0.44f), new Color(0.11f, 0.73f, 0.88f), new Color(0.82f, 0.66f, 0.2f), new Color(0.36f, 0.57f, 0.98f), new Color(0.6f, 0.72f, 0.9f) }
                    : new[] { UiTheme.Danger, new Color(0.94f, 0.38f, 0.18f), new Color(0.72f, 0.34f, 0.84f), new Color(0.65f, 0.35f, 0.28f) };
            donutObject.GetComponent<FinanceDonutGraphic>().SetValues(categoryValues, categoryColors, new Color(1f, 1f, 1f, 0.12f));
            var selectedTotal = selectedMetric == 0 ? net : selectedMetric == 1 ? income : -expenses;
            var centerValue = BroadcastUi.Label(chart.transform, "NetValue", Euro((int)Math.Max(int.MinValue, Math.Min(int.MaxValue, selectedTotal))), 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(centerValue.rectTransform, 0.37f, 0.48f, 0.63f, 0.62f);
            var centerTitle = BroadcastUi.Label(chart.transform, "NetTitle", selectedMetric == 0 ? (english ? "PROFIT" : "LUCRO")
                : selectedMetric == 1 ? (english ? "EARNINGS" : "RECEITAS") : (english ? "EXPENSES" : "DESPESAS"), 10, UiTheme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(centerTitle.rectTransform, 0.37f, 0.62f, 0.63f, 0.7f);
            for (var i = 0; i < categoryNames.Length; i++)
            {
                var x0 = 0.025f + i * (0.95f / categoryNames.Length);
                var x1 = 0.025f + (i + 1) * (0.95f / categoryNames.Length);
                var category = BroadcastUi.Label(chart.transform, "Category" + i,
                    categoryNames[i] + "\n" + (selectedMetric == 2 || (selectedMetric == 0 && i == 1) ? "−" : "+") + Euro((int)Math.Min(int.MaxValue, (long)categoryValues[i])),
                    14, categoryColors[i], TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(category.rectTransform, x0, 0.16f, x1, 0.255f);
            }
            var weeklyContext = BroadcastUi.Label(chart.transform, "WeeklyRevenueContext",
                (english ? "Weekly shirts  +" : "Camisolas/semana  +")
                + Euro(ClubFinanceLedger.WeeklyShirtIncome(_career, _career.Club))
                + (english ? "     ·     Sponsors  +" : "     ·     Patrocínios  +")
                + Euro(ClubFinanceLedger.WeeklySponsorIncome(_career.Club))
                + (english ? "     ·     Ticket income per home match  +" : "     ·     Bilheteira por jogo em casa  +")
                + Euro(Math.Max(12000, _career.Club.Reputation * 650))
                + (english ? "     ·     Transfer budget is affected only by transfer fees" : "     ·     O orçamento de transferências só é afetado por transferências"),
                12, UiTheme.Muted, TextAnchor.MiddleCenter);
            weeklyContext.resizeTextForBestFit = true;
            weeklyContext.resizeTextMinSize = 9;
            weeklyContext.resizeTextMaxSize = 12;
            BroadcastUi.Place(weeklyContext.rectTransform, 0.04f, 0.275f, 0.96f, 0.335f);
            var accountingNote = BroadcastUi.Label(chart.transform, "AccountingNote",
                english ? "Profit includes recorded income and costs, including wages and away travel. Payroll and travel do not reduce the transfer budget."
                    : "O lucro inclui receitas e custos registados, incluindo salários e viagens fora. Salários e viagens não reduzem o orçamento de transferências.",
                10, UiTheme.Muted, TextAnchor.MiddleCenter);
            accountingNote.resizeTextForBestFit = true;
            accountingNote.resizeTextMinSize = 8;
            accountingNote.resizeTextMaxSize = 10;
            BroadcastUi.Place(accountingNote.rectTransform, 0.04f, 0.34f, 0.96f, 0.39f);
            var footer = BroadcastUi.Panel(chart.transform, "ValueFooter", new Color(0f, 0f, 0f, 0.2f));
            BroadcastUi.Place(footer.rectTransform, 0.015f, 0.015f, 0.985f, 0.135f);
            var balance = finance.Balance;
            var worth = BroadcastUi.Label(footer.transform, "Worth", (english ? "CLUB BALANCE   " : "SALDO DO CLUBE   ") + Euro(balance), 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(worth.rectTransform, 0.02f, 0.08f, 0.49f, 0.92f);
            var projection = BroadcastUi.Label(footer.transform, "Projection", (english ? "PROJECTED BALANCE   " : "SALDO PROJETADO   ") + Euro((int)Math.Max(int.MinValue, Math.Min(int.MaxValue, ProjectedClubBalance(finance)))), 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(projection.rectTransform, 0.51f, 0.08f, 0.98f, 0.92f);
        }

        long ProjectedClubBalance(ClubFinance finance)
        {
            if (finance == null || _career == null || _career.Club == null)
                return 0;
            var club = _career.Club;
            var today = _career.Calendar.CurrentDate.Date;
            var seasonYear = today.Month >= 7 ? today.Year : today.Year - 1;
            var seasonEnd = new DateTime(seasonYear + 1, 6, 30);

            var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0)
                daysUntilMonday = 7;
            var firstPayday = today.AddDays(daysUntilMonday);
            long projected = finance.Balance;
            for (var payday = firstPayday; payday <= seasonEnd; payday = payday.AddDays(7))
            {
                projected += ClubFinanceLedger.WeeklyShirtIncome(_career, club);
                projected += ClubFinanceLedger.WeeklySponsorIncome(club);
                projected -= ProjectedWeeklyPayroll(club, payday);
            }
            foreach (var fixture in _career.Fixtures)
            {
                if (fixture == null || fixture.Played || fixture.Walkover || !fixture.Involves(club.Id)
                    || fixture.Date.Date < today || fixture.Date.Date > seasonEnd)
                    continue;
                if (fixture.HomeClubId == club.Id)
                    projected += Math.Max(12000, club.Reputation * 650);
                else
                    projected -= 6500 + Math.Max(0, 90 - club.Reputation) * 180;
            }
            foreach (var line in _career.Notebook)
            {
                if (string.IsNullOrEmpty(line)) continue;
                var bits = line.Split('|');
                if (bits.Length >= 9 && bits[0] == "job" && bits[1] == "move"
                    && long.TryParse(bits[8], out var transferTicks))
                {
                    var due = new DateTime(transferTicks).Date;
                    if (due <= today || due > seasonEnd) continue;
                    int.TryParse(bits[4], out var fee);
                    if (bits[3] == club.Id) projected -= Math.Max(0, fee);
                    else if (_career.World.Players.TryGetValue(bits[2], out var movingPlayer)
                        && movingPlayer.ClubId == club.Id)
                        projected += Math.Max(0, fee);
                }
                else if (bits.Length >= 8 && bits[0] == "job" && bits[1] == "loanmove"
                    && long.TryParse(bits[5], out var loanTicks))
                {
                    var due = new DateTime(loanTicks).Date;
                    if (due <= today || due > seasonEnd) continue;
                    int.TryParse(bits[4], out var fee);
                    if (bits[3] == club.Id) projected -= Math.Max(0, fee);
                    else if (_career.World.Players.TryGetValue(bits[2], out var loanPlayer)
                        && loanPlayer.ClubId == club.Id)
                        projected += Math.Max(0, fee);
                }
            }
            return projected;
        }

        long ProjectedWeeklyPayroll(Club club, DateTime payday)
        {
            var payroll = ClubFinanceLedger.WeeklyPayroll(_career, club, payday);
            foreach (var line in _career.Notebook)
            {
                if (string.IsNullOrEmpty(line)) continue;
                var bits = line.Split('|');
                if (bits.Length >= 9 && bits[0] == "job" && bits[1] == "move"
                    && long.TryParse(bits[8], out var moveTicks) && payday.Date >= new DateTime(moveTicks).Date
                    && _career.World.Players.TryGetValue(bits[2], out var movingPlayer))
                {
                    var oldWage = Math.Max(0, movingPlayer.WeeklyWage);
                    var buyerId = bits[3];
                    int.TryParse(bits[6], out var offeredWage);
                    var sellerId = movingPlayer.ClubId;
                    var newWage = offeredWage > 0 ? offeredWage : oldWage;
                    if (offeredWage <= 0 && _career.World.Clubs.TryGetValue(buyerId, out var buyer)
                        && _career.World.Clubs.TryGetValue(sellerId ?? "", out var seller))
                        newWage = Math.Max(oldWage, Mathf.RoundToInt(oldWage * (1f + Mathf.Clamp(buyer.Reputation - seller.Reputation, -20, 40) / 100f)));
                    if (club.Id == sellerId) payroll -= oldWage;
                    if (club.Id == buyerId) payroll += newWage;
                }
                else if (bits.Length >= 8 && bits[0] == "job" && bits[1] == "loanmove"
                    && long.TryParse(bits[5], out var loanTicks) && long.TryParse(bits[6], out var returnTicks)
                    && payday.Date >= new DateTime(loanTicks).Date && payday.Date < new DateTime(returnTicks).Date
                    && _career.World.Players.TryGetValue(bits[2], out var loanPlayer))
                {
                    var wage = Math.Max(0, loanPlayer.WeeklyWage);
                    if (club.Id == loanPlayer.ClubId) payroll -= wage;
                    if (club.Id == bits[3]) payroll += wage;
                }
                else if (bits.Length >= 8 && bits[0] == "job" && bits[1] == "active-loan"
                    && long.TryParse(bits[6], out var loanReturnTicks) && payday.Date >= new DateTime(loanReturnTicks).Date
                    && _career.World.Players.TryGetValue(bits[2], out var returnedPlayer))
                {
                    var wage = bits.Length > 7 && int.TryParse(bits[7], out var originalWage) ? Math.Max(0, originalWage) : Math.Max(0, returnedPlayer.WeeklyWage);
                    if (club.Id == bits[3]) payroll += wage;
                    if (club.Id == bits[4]) payroll -= Math.Max(0, returnedPlayer.WeeklyWage);
                }
            }
            return Math.Max(0L, payroll);
        }

        void BuildFinanceTransactions(Transform host, ClubFinance finance, bool english)
        {
            var header = BroadcastUi.Panel(host, "TransactionsHeader", new Color(0.055f, 0.075f, 0.1f, 0.97f));
            BroadcastUi.Place(header.rectTransform, 0.005f, 0.84f, 0.995f, 0.985f);
            var title = BroadcastUi.Label(header.transform, "Title", english ? "SEASON TRANSACTIONS" : "TRANSAÇÕES DA ÉPOCA", 18, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.02f, 0.08f, 0.98f, 0.92f);
            var topSellers = ClubFinanceLedger.TopShirtSellers(_career, _career.Club);
            var sellers = BroadcastUi.Panel(host, "TopShirtSellers", new Color(0.07f, 0.12f, 0.2f, 0.98f));
            BroadcastUi.Place(sellers.rectTransform, 0.005f, 0.65f, 0.995f, 0.825f);
            var sellerTitle = BroadcastUi.Label(sellers.transform, "Title",
                (english ? "SHIRT REVENUE  " : "RECEITA DE CAMISOLAS  ") + Euro(finance.ShirtIncomeThisSeason)
                + "     ·     " + (english ? "TICKETS  " : "BILHETEIRA  ") + Euro(finance.TicketIncomeThisSeason)
                + "     ·     " + (english ? "SPONSORS  " : "PATROCÍNIOS  ") + Euro(finance.SponsorIncomeThisSeason)
                + "     ·     " + (english ? "TOP SELLERS" : "MAIS VENDIDAS"),
                12, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(sellerTitle.rectTransform, 0.02f, 0.65f, 0.98f, 0.95f);
            for (var i = 0; i < 3; i++)
            {
                var player = i < topSellers.Count ? topSellers[i] : null;
                var name = player != null ? BroadcastUi.ShortName(player.FullName) : (english ? "No sales recorded" : "Sem vendas registadas");
                var x0 = 0.025f + i * 0.325f;
                var seller = BroadcastUi.Panel(sellers.transform, "Seller" + i, new Color(1f, 1f, 1f, 0.05f));
                BroadcastUi.Place(seller.rectTransform, x0, 0.07f, x0 + 0.31f, 0.61f);
                var rank = BroadcastUi.Label(seller.transform, "Rank", (i + 1).ToString("00"), 14, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(rank.rectTransform, 0.025f, 0.12f, 0.17f, 0.88f);
                var label = BroadcastUi.Label(seller.transform, "Name", name, 13, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 9;
                label.resizeTextMaxSize = 13;
                BroadcastUi.Place(label.rectTransform, 0.2f, 0.12f, 0.97f, 0.88f);
            }

            var list = BroadcastUi.Panel(host, "TransactionList", new Color(0.055f, 0.075f, 0.1f, 0.97f));
            BroadcastUi.Place(list.rectTransform, 0.005f, 0.015f, 0.995f, 0.635f);
            var shown = 0;
            var seasonYear = _career.Calendar.CurrentDate.Month >= 7 ? _career.Calendar.CurrentDate.Year : _career.Calendar.CurrentDate.Year - 1;
            var seasonStart = new DateTime(seasonYear, 7, 1);
            for (var i = finance.Transactions.Count - 1; i >= 0 && shown < 12; i--)
            {
                var transaction = finance.Transactions[i];
                if (transaction == null || transaction.DateTicks < seasonStart.Ticks
                    || transaction.DateTicks > _career.Calendar.CurrentDate.Ticks || transaction.Category == "shirt")
                    continue;
                var date = transaction.DateTicks > 0 && transaction.DateTicks <= DateTime.MaxValue.Ticks
                    ? LocalizationManager.FormatDate(new DateTime(transaction.DateTicks), "dd MMM") : "—";
                var row = BroadcastUi.Panel(list.transform, "Transaction" + shown, new Color(1f, 1f, 1f, shown % 2 == 0 ? 0.055f : 0.015f));
                var rowTop = 0.98f - shown * 0.081f;
                BroadcastUi.Place(row.rectTransform, 0.012f, rowTop - 0.075f, 0.988f, rowTop);
                var detail = BroadcastUi.Label(row.transform, "Detail", date + "   ·   " + FinanceCategoryLabel(transaction.Category, english)
                    + "   ·   " + transaction.Description, 12, Color.white, TextAnchor.MiddleLeft);
                detail.resizeTextForBestFit = true;
                detail.resizeTextMinSize = 9;
                detail.resizeTextMaxSize = 12;
                BroadcastUi.Place(detail.rectTransform, 0.02f, 0.08f, 0.76f, 0.92f);
                var amount = BroadcastUi.Label(row.transform, "Amount", SignedEuro(transaction.Amount), 13,
                    transaction.Amount >= 0 ? UiTheme.Fitness : UiTheme.Danger, TextAnchor.MiddleRight, FontStyle.Bold);
                BroadcastUi.Place(amount.rectTransform, 0.77f, 0.08f, 0.98f, 0.92f);
                shown++;
            }
            if (shown == 0)
            {
                var empty = BroadcastUi.Label(list.transform, "Empty", english ? "Transactions will appear here as the season progresses." : "As transações aparecem aqui ao longo da época.", 15, UiTheme.Muted, TextAnchor.MiddleCenter);
                BroadcastUi.Stretch(empty.rectTransform);
            }
        }

        void BuildFinanceBudget(Transform host, ClubFinance finance, bool english)
        {
            var panel = BroadcastUi.Panel(host, "TransferBudget", new Color(0.055f, 0.075f, 0.1f, 0.97f));
            BroadcastUi.Place(panel.rectTransform, 0.005f, 0.025f, 0.995f, 0.985f);
            var total = Math.Max(0L, (long)finance.TransferBudget + finance.TransferSpentThisSeason);
            var title = BroadcastUi.Label(panel.transform, "Title", english ? "TRANSFER BUDGET" : "ORÇAMENTO DE TRANSFERÊNCIAS", 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.24f, 0.87f, 0.76f, 0.98f);
            var ringObject = new GameObject("BudgetDonut", typeof(RectTransform), typeof(CanvasRenderer), typeof(FinanceDonutGraphic));
            ringObject.transform.SetParent(panel.transform, false);
            var ringRect = ringObject.GetComponent<RectTransform>();
            ringRect.anchorMin = ringRect.anchorMax = new Vector2(0.5f, 0.62f);
            ringRect.sizeDelta = new Vector2(148f, 148f);
            ringObject.GetComponent<FinanceDonutGraphic>().SetValues(
                new[] { (float)Math.Max(0, finance.TransferBudget), (float)Math.Max(0, finance.TransferSpentThisSeason) },
                new[] { UiTheme.Fitness, UiTheme.Danger }, new Color(1f, 1f, 1f, 0.14f));
            var centerTitle = BroadcastUi.Label(panel.transform, "TotalTitle", english ? "AVAILABLE" : "DISPONÍVEL", 12, UiTheme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(centerTitle.rectTransform, 0.4f, 0.68f, 0.6f, 0.76f);
            var center = BroadcastUi.Label(panel.transform, "Total", Euro(finance.TransferBudget), 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(center.rectTransform, 0.38f, 0.56f, 0.62f, 0.68f);
            var currentTitle = BroadcastUi.Label(panel.transform, "CurrentTitle", english ? "SEASON" : "ÉPOCA", 14, UiTheme.Fitness, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(currentTitle.rectTransform, 0.02f, 0.66f, 0.28f, 0.76f);
            var current = BroadcastUi.Label(panel.transform, "Current", Euro((int)Math.Min(int.MaxValue, total)), 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(current.rectTransform, 0.02f, 0.52f, 0.28f, 0.66f);
            var spentTitle = BroadcastUi.Label(panel.transform, "SpentTitle", english ? "SPENT THIS SEASON" : "GASTO ESTA ÉPOCA", 14, UiTheme.Danger, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(spentTitle.rectTransform, 0.72f, 0.66f, 0.98f, 0.76f);
            var spent = BroadcastUi.Label(panel.transform, "Spent", Euro(finance.TransferSpentThisSeason), 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(spent.rectTransform, 0.72f, 0.52f, 0.98f, 0.66f);
            var note = BroadcastUi.Label(panel.transform, "BudgetNote", english
                ? "Weekly wages come from the club balance and do not reduce the transfer budget."
                : "Os salários saem semanalmente do saldo do clube e não reduzem o orçamento de transferências.",
                13, UiTheme.Muted, TextAnchor.MiddleCenter);
            note.horizontalOverflow = HorizontalWrapMode.Wrap;
            note.verticalOverflow = VerticalWrapMode.Truncate;
            note.resizeTextForBestFit = true;
            note.resizeTextMinSize = 11;
            note.resizeTextMaxSize = 14;
            BroadcastUi.Place(note.rectTransform, 0.08f, 0.2f, 0.92f, 0.36f);
            var pending = DeskDue("budget", out _);
            var hasWait = DeskDue("budget-wait", out var waitUntil);
            var waiting = !pending && hasWait && new DateTime(waitUntil).Date > _career.Calendar.CurrentDate.Date;
            var noteText = pending
                ? (english ? "A board meeting is already booked. Open it from your inbox." : "A reunião com a direção já está marcada. Abre-a nas mensagens.")
                : waiting
                    ? (english ? "The board will not hear another request until " : "A direção só ouve outro pedido a ") + LocalizationManager.FormatDate(new DateTime(waitUntil), "d MMM") + "."
                    : (english ? "If an incoming transfer or staff offer exceeds its budget, request extra funds during that negotiation." : "Se uma contratação de jogador ou staff ultrapassar a verba, pede reforço durante a negociação.");
            note.text = noteText;
            BroadcastUi.Place(note.rectTransform, 0.08f, 0.04f, 0.92f, 0.36f);
        }

        string FinanceCategoryLabel(string category, bool english)
        {
            switch (category)
            {
                case "ticket": return english ? "Ticket sales" : "Receita de bilheteira";
                case "shirt": return english ? "Shirt sale" : "Venda de camisola";
                case "sponsor": return english ? "Sponsorship income" : "Receita de patrocínio";
                case "player-sale": return english ? "Player sale" : "Venda de jogador";
                case "transfer-fee": return english ? "Transfer fee" : "Taxa de transferência";
                case "wages": return english ? "Weekly wages" : "Salários semanais";
                case "travel": return english ? "Away travel" : "Viagem fora";
                case "loan-fee": return english ? "Loan fee" : "Custo de empréstimo";
                case "loan-income": return english ? "Loan income" : "Receita de empréstimo";
                case "board": return english ? "Board contribution" : "Verba da direção";
                default: return english ? "Other" : "Outro";
            }
        }

        string SignedEuro(int amount)
        {
            var magnitude = (int)Math.Min(int.MaxValue, Math.Abs((long)amount));
            return (amount < 0 ? "−" : "+") + Euro(magnitude);
        }

        void FillAcademy()
        {
            if (_academyContent == null || _career == null) return;
            var existingPlayers = _career.AcademyPlayerIds.Count;
            var existingFixtures = _career.AcademyFixtures.Count;
            var existingTactics = _career.AcademyTactics.Count;
            var academyChanged = AcademyFactory.Ensure(_career, _career.Club.Id);
            AcademyFactory.EnsureSchedule(_career, _career.Club.Id);
            foreach (var ageGroup in AcademyFactory.AgeGroups)
                AcademyTactics(ageGroup);
            if (academyChanged || existingPlayers != _career.AcademyPlayerIds.Count || existingFixtures != _career.AcademyFixtures.Count
                || existingTactics != _career.AcademyTactics.Count)
                CareerStore.Save(_career);
            if (_academyAgeTabs != null)
                for (var i = 0; i < _academyAgeTabs.Length; i++)
                    _academyAgeTabs[i].GetComponent<Image>().color = i == _academyAgeIndex ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f);
            if (_academyViewTabs != null)
                for (var i = 0; i < _academyViewTabs.Length; i++)
                    _academyViewTabs[i].GetComponent<Image>().color = i == _academyViewIndex ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f);
            Clear(_academyContent);
            var english = LocalizationManager.Current == GameLanguage.English;
            var group = AcademyFactory.AgeGroups[Mathf.Clamp(_academyAgeIndex, 0, AcademyFactory.AgeGroups.Length - 1)];
            var viewNames = english ? new[] { "TACTICS", "SQUAD", "FIXTURES" } : new[] { "TÁTICAS", "PLANTEL", "JOGOS" };
            var heading = BroadcastUi.Label(_academyContent, "GroupHeading", group + "  ·  " + viewNames[Mathf.Clamp(_academyViewIndex, 0, viewNames.Length - 1)], 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Preferred(heading, 30);
            if (_academyViewIndex == 2)
            {
                FillAcademyFixtures(group);
                return;
            }

            var prospects = AcademyPlayers(group, _career.Club.Id);
            if (prospects.Count == 0)
            {
                var empty = BroadcastUi.Label(_academyContent, "Empty", english ? "No youth players are currently in the academy." : "Não há jovens jogadores na academia neste momento.", 16, UiTheme.Muted, TextAnchor.MiddleCenter);
                BroadcastUi.Preferred(empty, 60);
                return;
            }

            if (_academyViewIndex == 0)
            {
                FillAcademyTactics(group, prospects);
                return;
            }

            FillAcademyRoster(prospects, english);
        }

        List<Player> AcademyPlayers(string group, string clubId)
        {
            var players = new List<Player>();
            foreach (var id in _career.AcademyPlayerIds)
                if (_career.World.Players.TryGetValue(id, out var player) && player.ClubId == clubId && AcademyFactory.GroupForAge(player.Age) == group)
                    players.Add(player);
            players.Sort((a, b) => PlayerAttributes.Overall(b).CompareTo(PlayerAttributes.Overall(a)));
            return players;
        }

        void SplitAcademySquad(string group, List<Player> squad, string formation, List<Player> eleven, List<Player> bench)
        {
            eleven.Clear();
            bench.Clear();
            if (squad == null || squad.Count == 0) return;
            if (!_career.AcademySquadOrders.TryGetValue(group, out var saved) || saved == null || saved.Count == 0)
            {
                StartingShape.Split(squad, formation, eleven, bench);
                saved = new List<string>();
                foreach (var player in eleven) saved.Add(player.Id);
                foreach (var player in bench) saved.Add(player.Id);
                _career.AcademySquadOrders[group] = saved;
                CareerStore.Save(_career);
                return;
            }

            var remaining = new List<Player>(squad);
            var ordered = new List<Player>(squad.Count);
            for (var i = 0; i < saved.Count; i++)
            {
                var index = remaining.FindIndex(player => player.Id == saved[i]);
                if (index < 0) continue;
                ordered.Add(remaining[index]);
                remaining.RemoveAt(index);
            }
            remaining.Sort((a, b) => PlayerAttributes.Overall(b).CompareTo(PlayerAttributes.Overall(a)));
            ordered.AddRange(remaining);
            saved.Clear();
            foreach (var player in ordered) saved.Add(player.Id);
            var starterCount = Mathf.Min(11, ordered.Count);
            for (var i = 0; i < starterCount; i++) eleven.Add(ordered[i]);
            for (var i = starterCount; i < ordered.Count; i++) bench.Add(ordered[i]);
        }

        void SwapAcademyFieldOrder(string group, Player moving, int fromField, int toField)
        {
            if (moving == null || toField < 0 || _career == null) return;
            var players = AcademyPlayers(group, _career.Club.Id);
            var tactics = AcademyTactics(group);
            var eleven = new List<Player>();
            var bench = new List<Player>();
            SplitAcademySquad(group, players, tactics.Formation, eleven, bench);
            var order = _career.AcademySquadOrders[group];
            var source = order.IndexOf(moving.Id);
            if (source < 0 || toField >= eleven.Count || (fromField >= 0 && fromField >= eleven.Count)) return;
            var target = toField;
            var swap = order[source];
            order[source] = order[target];
            order[target] = swap;
            CareerStore.Save(_career);
            FillAcademy();
        }

        void HandleAcademyBenchClick(Player player, string group)
        {
            if (player == null) return;
            if (!string.IsNullOrEmpty(_academySelectedBenchPlayerId)
                && _academySelectedBenchPlayerId != player.Id)
            {
                var orderKey = AcademyFactory.AgeGroups[Mathf.Clamp(_academyAgeIndex, 0, AcademyFactory.AgeGroups.Length - 1)];
                if (_career.AcademySquadOrders.TryGetValue(orderKey, out var order))
                {
                    var first = order.IndexOf(_academySelectedBenchPlayerId);
                    var second = order.IndexOf(player.Id);
                    if (first >= 11 && second >= 11)
                    {
                        var swap = order[first];
                        order[first] = order[second];
                        order[second] = swap;
                        _academySelectedBenchPlayerId = "";
                        CareerStore.Save(_career);
                        FillAcademy();
                        return;
                    }
                }
            }

            if (_academyLastBenchClickId == player.Id && Time.unscaledTime - _academyLastBenchClickAt <= 0.42f)
            {
                _academySelectedBenchPlayerId = player.Id;
                _academyBenchGroup = 0;
                FillAcademy();
            }
            _academyLastBenchClickId = player.Id;
            _academyLastBenchClickAt = Time.unscaledTime;
        }

        void FillAcademyRoster(List<Player> shown, bool english)
        {
            for (var i = 0; i < shown.Count; i++)
            {
                var player = shown[i];
                var row = BroadcastUi.Panel(_academyContent, "Prospect" + i, new Color(0.13f, 0.06f, 0.27f, 0.94f));
                BroadcastUi.Preferred(row, 88);
                var portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                var portraitRect = portraitObject.GetComponent<RectTransform>();
                portraitRect.SetParent(row.transform, false);
                BroadcastUi.Place(portraitRect, 0.015f, 0.08f, 0.09f, 0.92f);
                var portrait = portraitObject.GetComponent<Image>();
                portrait.sprite = FaceBook.For(player);
                portrait.preserveAspect = true;
                portrait.raycastTarget = false;

                var name = BroadcastUi.Label(row.transform, "Name", player.FullName.ToUpperInvariant(), 15, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(name.rectTransform, 0.105f, 0.53f, 0.58f, 0.94f);
                var position = PresentationLabels.Position(player.Position, english);
                var profile = BroadcastUi.Label(row.transform, "Profile", (english ? "AGE " : "IDADE ") + player.Age + "  ·  " + LocalizationManager.Translate(player.Nationality) + "  ·  " + position,
                    12, UiTheme.Accent, TextAnchor.MiddleLeft);
                BroadcastUi.Place(profile.rectTransform, 0.105f, 0.12f, 0.58f, 0.52f);
                var line = FindSeasonLine(player.Id);
                var stats = BroadcastUi.Label(row.transform, "Stats", (english ? "Apps " : "J  ") + (line == null ? 0 : line.Apps)
                    + "   " + (english ? "Goals " : "Golos ") + (line == null ? 0 : line.Goals)
                    + "   " + (english ? "Assists " : "Assist. ") + (line == null ? 0 : line.Assists)
                    + "\n" + (english ? "OVR " : "NÍV. ") + PlayerAttributes.Overall(player) + "   " + (english ? "Potential " : "Potencial ") + player.Potential,
                    13, Color.white, TextAnchor.MiddleLeft);
                BroadcastUi.Place(stats.rectTransform, 0.59f, 0.13f, 0.76f, 0.89f);
                var profileButton = BroadcastUi.Button(row.transform, "Profile", english ? "PROFILE" : "FICHA", () => OpenPlayer(player), new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 11);
                BroadcastUi.Place(profileButton.GetComponent<RectTransform>(), 0.77f, 0.55f, 0.985f, 0.9f);
                var promote = BroadcastUi.Button(row.transform, "Promote", player.Age >= 16
                    ? (english ? "PROMOTE" : "PROMOVER")
                    : (english ? "AVAILABLE AT 16" : "DISPONÍVEL AOS 16"),
                    player.Age >= 16 ? () => PromoteAcademyPlayer(player) : (System.Action)null,
                    player.Age >= 16 ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), player.Age >= 16 ? UiTheme.ButtonText : UiTheme.Muted, 11);
                promote.interactable = player.Age >= 16;
                BroadcastUi.Place(promote.GetComponent<RectTransform>(), 0.77f, 0.1f, 0.985f, 0.46f);
            }
        }

        TacticalIdentity AcademyTactics(string group)
        {
            if (!_career.AcademyTactics.TryGetValue(group, out var tactics) || tactics == null)
            {
                var source = _career.Club.Tactics;
                tactics = new TacticalIdentity
                {
                    Formation = source.Formation,
                    Pressing = source.Pressing,
                    BuildUp = source.BuildUp,
                    FullBacks = source.FullBacks,
                    DefensiveLine = source.DefensiveLine,
                    Width = source.Width,
                    Tempo = source.Tempo,
                    PassingStyle = source.PassingStyle,
                    AttackIdea = source.AttackIdea,
                    Marking = source.Marking
                };
                _career.AcademyTactics[group] = tactics;
            }
            return tactics;
        }

        void FillAcademyTactics(string group, List<Player> squad)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var club = _career.Club;
            var tactics = AcademyTactics(group);
            var panel = BroadcastUi.Panel(_academyContent, "AcademyTacticsPanel", new Color(0.05f, 0.02f, 0.12f, 0.55f));
            BroadcastUi.Preferred(panel, 570f);

            var field = BroadcastUi.Zone(panel.transform, "AcademyPitch", 0.095f, 0.32f, 0.345f, 0.98f);
            BroadcastUi.DrawVerticalHalfPitch(field, new Color(0.12f, 0.46f, 0.22f));
            PlaceAcademyShape(field, squad, club, group);

            var benchHeading = BroadcastUi.Label(panel.transform, "BenchHeading", _academyBenchGroup == 0
                ? (english ? "SUBSTITUTES" : "SUPLENTES") : (english ? "RESERVES" : "RESERVAS"),
                12, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(benchHeading.rectTransform, 0.015f, 0.275f, 0.42f, 0.315f);
            if (!string.IsNullOrEmpty(_academySelectedBenchPlayerId)
                && _career.World.Players.TryGetValue(_academySelectedBenchPlayerId, out var selectedBench))
                benchHeading.text = (english ? "SELECTED: " : "SELECIONADO: ") + selectedBench.FullName
                    + (english ? " · choose a replacement" : " · escolhe quem trocar");
            var subs = BroadcastUi.Button(panel.transform, "AcademySubsTab", english ? "SUBSTITUTES" : "SUPLENTES",
                () => { _academyBenchGroup = 0; FillAcademy(); }, _academyBenchGroup == 0 ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f),
                _academyBenchGroup == 0 ? UiTheme.ButtonText : Color.white, 10);
            BroadcastUi.Place(subs.GetComponent<RectTransform>(), 0.015f, 0.23f, 0.21f, 0.275f);
            var reserves = BroadcastUi.Button(panel.transform, "AcademyReservesTab", english ? "RESERVES" : "RESERVAS",
                () => { _academyBenchGroup = 1; FillAcademy(); }, _academyBenchGroup == 1 ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f),
                _academyBenchGroup == 1 ? UiTheme.ButtonText : Color.white, 10);
            BroadcastUi.Place(reserves.GetComponent<RectTransform>(), 0.215f, 0.23f, 0.42f, 0.275f);

            var eleven = new List<Player>();
            var bench = new List<Player>();
            SplitAcademySquad(group, squad, tactics.Formation, eleven, bench);
            var groupPlayers = new List<Player>();
            for (var i = 0; i < bench.Count; i++)
                if ((_academyBenchGroup == 0 && i < 7) || (_academyBenchGroup == 1 && i >= 7))
                    groupPlayers.Add(bench[i]);
            for (var i = 0; i < groupPlayers.Count; i++)
            {
                var x0 = 0.015f + i * (0.405f / groupPlayers.Count);
                var x1 = 0.015f + (i + 1) * (0.405f / groupPlayers.Count);
                AcademyBenchCard(panel.transform, groupPlayers[i], group, _academyBenchGroup == 1, x0 + 0.003f, x1 - 0.003f);
            }

            if (_academyPane == 0 || _academyPane == 2)
                _academyPane = 1;
            var paneNames = english ? new[] { "ROLES", "TACTICS" } : new[] { "FUNÇÕES", "TÁTICAS" };
            var academyPanes = new[] { 1, 3 };
            for (var i = 0; i < paneNames.Length; i++)
            {
                var pane = academyPanes[i];
                var x0 = 0.44f + i * 0.26f;
                var tab = BroadcastUi.Button(panel.transform, "AcademyPane" + pane, paneNames[i], () =>
                {
                    _academyPane = pane;
                    FillAcademy();
                }, pane == _academyPane ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f),
                    pane == _academyPane ? UiTheme.ButtonText : Color.white, 10);
                BroadcastUi.Place(tab.GetComponent<RectTransform>(), x0, 0.91f, x0 + 0.24f, 0.975f);
            }
            var body = BroadcastUi.Zone(panel.transform, "AcademyTacticSheet", 0.44f, 0.25f, 0.98f, 0.9f);
            if (_academyPane == 1)
                FillAcademyRoles(body, group, squad, english);
            else
                FillAcademyTacticOptions(body, group, tactics, english);
        }

        void AcademyBenchCard(Transform host, Player player, string ageGroup, bool inReserves, float x0, float x1)
        {
            var selected = _academySelectedBenchPlayerId == player.Id;
            var panel = host as RectTransform;
            Canvas.ForceUpdateCanvases();
            var size = panel == null ? Vector2.one : panel.rect.size;
            var card = PersonCard(host, player, _career.Club, true, -1, true,
                Mathf.Max(1f, size.x * (x1 - x0)), Mathf.Max(1f, size.y * 0.2f), player.Position,
                "Academy:" + ageGroup, () => HandleAcademyBenchClick(player, ageGroup),
                selected ? new Color(0.12f, 0.36f, 0.38f, 1f) : (Color?)null, inReserves);
            card.anchorMin = card.anchorMax = new Vector2((x0 + x1) * 0.5f, 0.125f);
            card.anchoredPosition = Vector2.zero;
            var drag = card.GetComponent<SquadDragCard>();
            if (drag != null)
                drag.BenchReserves = inReserves;
        }

        void FillAcademyFormations(RectTransform host, TacticalIdentity tactics, List<Player> squad, bool english)
        {
            var forms = new[] { "4-3-3", "4-2-3-1", "4-4-2", "3-4-3", "5-4-1" };
            for (var i = 0; i < forms.Length; i++)
            {
                var formation = forms[i];
                var y1 = 0.95f - i * 0.17f;
                var button = BroadcastUi.Button(host, "AcademyFormation" + i, formation, () =>
                {
                    tactics.Formation = formation;
                    tactics.Summary = formation + " · " + tactics.Pressing + " · " + tactics.BuildUp;
                    CareerStore.Save(_career);
                    FillAcademy();
                }, tactics.Formation == formation ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f),
                    tactics.Formation == formation ? UiTheme.ButtonText : Color.white, 17);
                BroadcastUi.Place(button.GetComponent<RectTransform>(), 0.025f, y1 - 0.13f, 0.975f, y1);
            }
            var hint = BroadcastUi.Label(host, "AcademyFormationHint", english ? "The youth XI follows this formation and its role preferences." : "O onze jovem segue esta formação e as respetivas funções.", 12, UiTheme.Muted, TextAnchor.MiddleCenter);
            BroadcastUi.Place(hint.rectTransform, 0.03f, 0.005f, 0.97f, 0.11f);
        }

        void FillAcademyRoles(RectTransform host, string group, List<Player> squad, bool english)
        {
            var roles = new[] { "captain", "penalty", "corner", "freekick" };
            var labels = english ? new[] { "CAPTAIN", "PENALTIES", "CORNERS", "FREE KICKS" }
                : new[] { "CAPITÃO", "PENÁLTIS", "CANTOS", "LIVRES" };
            for (var i = 0; i < roles.Length; i++)
            {
                var key = group + ":" + roles[i];
                var id = _career.AcademyRoleAssignments.TryGetValue(key, out var assigned) ? assigned : "";
                var who = squad.Find(p => p.Id == id);
                var label = labels[i] + "  ·  " + (who == null ? (english ? "Choose player" : "Escolher jogador") : BroadcastUi.ShortName(who.FullName));
                var role = roles[i];
                var armed = _academyArm == role && _academyArmGroup == group;
                var button = BroadcastUi.Button(host, "AcademyRole" + i, label, () =>
                {
                    _academyArm = role;
                    _academyArmGroup = group;
                    FillAcademy();
                }, armed ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), armed ? UiTheme.ButtonText : Color.white, 14);
                var y1 = 0.86f - i * 0.18f;
                BroadcastUi.Place(button.GetComponent<RectTransform>(), 0.025f, y1 - 0.15f, 0.975f, y1);
            }
            var hint = BroadcastUi.Label(host, "AcademyRoleHint",
                string.IsNullOrEmpty(_academyArm) || _academyArmGroup != group
                    ? (english ? "Click a role, then a player in the lineup." : "Clica numa função e depois num jogador do onze.")
                    : (english ? "Now click a player in the lineup." : "Agora clica num jogador do onze."),
                13, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(hint.rectTransform, 0.04f, 0.02f, 0.96f, 0.14f);
        }

        void AssignAcademyRole(Player player, string group)
        {
            if (player == null || string.IsNullOrEmpty(group) || string.IsNullOrEmpty(_academyArm) || _academyArmGroup != group)
                return;
            _career.AcademyRoleAssignments[group + ":" + _academyArm] = player.Id;
            _academyArm = null;
            _academyArmGroup = null;
            CareerStore.Save(_career);
            FillAcademy();
        }

        void FillAcademyInstructions(RectTransform host, List<Player> squad, bool english)
        {
            var scroll = BroadcastUi.Scroll(host, "AcademyOrdersScroll", out var content);
            BroadcastUi.Stretch(scroll.GetComponent<RectTransform>());
            for (var i = 0; i < squad.Count; i++)
            {
                var player = squad[i];
                var button = BroadcastUi.Button(content, "AcademyOrder" + i,
                    player.FullName + "  ·  " + LocalizationManager.Translate(string.IsNullOrEmpty(player.Duty) ? "Equilibrar" : player.Duty),
                    () => ShowAcademyInstructionPicker(player, english), new Color(1f, 1f, 1f, 0.08f), Color.white, 11);
                BroadcastUi.Preferred(button.GetComponent<RectTransform>(), 30);
            }
        }

        void ShowAcademyInstructionPicker(Player player, bool english)
        {
            if (player == null) return;
            var options = IndividualOrders.ForPosition(player.Position);
            OpenModal(english ? "INDIVIDUAL INSTRUCTION" : "INSTRUÇÃO INDIVIDUAL", player.FullName, false);
            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var button = BroadcastUi.Button(_modalBody, "AcademyDuty" + i, LocalizationManager.Translate(option), () =>
                {
                    player.Duty = option;
                    CareerStore.Save(_career);
                    CloseModal();
                    FillAcademy();
                }, option == player.Duty ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), Color.white, 13);
                BroadcastUi.Preferred(button.GetComponent<RectTransform>(), 38);
            }
        }

        void FillAcademyTacticOptions(RectTransform host, string group, TacticalIdentity tactics, bool english)
        {
            var forms = new[] { "4-3-3", "4-2-3-1", "4-4-2", "3-4-3", "5-4-1" };
            for (var f = 0; f < forms.Length; f++)
            {
                var formation = forms[f];
                var x0 = 0.02f + f * 0.196f;
                var formButton = BroadcastUi.Button(host, "AcademyFormation" + f, formation, () =>
                {
                    tactics.Formation = formation;
                    tactics.Summary = formation + " · " + tactics.Pressing + " · " + tactics.BuildUp;
                    CareerStore.Save(_career);
                    FillAcademy();
                }, tactics.Formation == formation ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f),
                    tactics.Formation == formation ? UiTheme.ButtonText : Color.white, 12);
                BroadcastUi.Place(formButton.GetComponent<RectTransform>(), x0, 0.9f, x0 + 0.18f, 0.99f);
            }
            var rows = new[]
            {
                new { Title = english ? "PRESSING" : "PRESSÃO", Values = new[] { "Pressão muito alta", "Pressão alta", "Pressão média", "Pressão baixa" }, Current = tactics.Pressing },
                new { Title = english ? "DEFENSIVE LINE" : "LINHA DEFENSIVA", Values = new[] { "Linha muito alta", "Linha defensiva alta", "Linha defensiva média", "Linha defensiva baixa", "Linha muito baixa" }, Current = tactics.DefensiveLine },
                new { Title = english ? "BUILD-UP" : "CONSTRUÇÃO", Values = new[] { "Construção curta", "Jogo misto", "Construção longa" }, Current = tactics.BuildUp },
                new { Title = english ? "FULL-BACKS" : "LATERAIS", Values = new[] { "Laterais ficam", "Laterais equilibram", "Laterais sobem" }, Current = tactics.FullBacks },
                new { Title = english ? "WIDTH" : "LARGURA", Values = new[] { "Largura estreita", "Largura normal", "Largura larga" }, Current = tactics.Width },
                new { Title = english ? "TEMPO" : "RITMO", Values = new[] { "Ritmo lento", "Ritmo normal", "Ritmo rápido" }, Current = tactics.Tempo },
                new { Title = english ? "PASSING RISK" : "RISCO NO PASSE", Values = new[] { "Passe seguro", "Passe misto", "Passe arriscado" }, Current = tactics.PassingStyle },
                new { Title = english ? "MARKING" : "MARCAÇÃO", Values = new[] { "Marcação zonal", "Marcação mista", "Marcação ao homem" }, Current = tactics.Marking },
                new { Title = english ? "ATTACKING IDEA" : "IDEIA DE JOGO", Values = new[] { "Equilibrado", "Tiki-taka", "Posse", "Cruzamentos", "Jogo direto", "Bolas longas", "Remates de longe", "Contra-ataque" }, Current = tactics.AttackIdea }
            };
            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                var index = i;
                var button = BroadcastUi.Button(host, "AcademyTactic" + i, row.Title + "\n" + LocalizationManager.Translate(string.IsNullOrEmpty(row.Current) ? "—" : row.Current),
                    () => ShowAcademyTacticPicker(group, index, row.Title, row.Values, row.Current, english),
                    new Color(1f, 1f, 1f, 0.08f), Color.white, 10);
                var top = 0.88f - i * 0.095f;
                BroadcastUi.Place(button.GetComponent<RectTransform>(), 0.02f, top - 0.088f, 0.98f, top);
            }
        }

        void ShowAcademyTacticPicker(string group, int index, string title, string[] options, string current, bool english)
        {
            var tactics = AcademyTactics(group);
            OpenModal(title, english ? "Choose how this age group will play." : "Escolhe como este escalão vai jogar.", false, true);
            var tacticCard = _modal.transform.Find("Card") as RectTransform;
            if (tacticCard != null)
            {
                BroadcastUi.Place(tacticCard, 0.24f, 0.16f, 0.76f, 0.84f);
                BroadcastUi.Place(_modalTitle.rectTransform, 0.08f, 0.82f, 0.88f, 0.98f);
                BroadcastUi.Place(_modalLead.rectTransform, 0.08f, 0.72f, 0.92f, 0.82f);
                var optionsScroll = tacticCard.Find("Scroll") as RectTransform;
                if (optionsScroll != null) BroadcastUi.Place(optionsScroll, 0.06f, 0.06f, 0.94f, 0.68f);
                if (_modalClose != null) BroadcastUi.Place(_modalClose.GetComponent<RectTransform>(), 0.92f, 0.85f, 0.98f, 0.98f);
            }
            AddHead(english ? "AVAILABLE OPTIONS" : "OPÇÕES DISPONÍVEIS");
            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var button = BroadcastUi.Button(_modalBody, "AcademyTacticOption" + i, LocalizationManager.Translate(option), () =>
                {
                    switch (index)
                    {
                        case 0: tactics.Pressing = option; break;
                        case 1: tactics.DefensiveLine = option; break;
                        case 2: tactics.BuildUp = option; break;
                        case 3: tactics.FullBacks = option; break;
                        case 4: tactics.Width = option; break;
                        case 5: tactics.Tempo = option; break;
                        case 6: tactics.PassingStyle = option; break;
                        case 7: tactics.Marking = option; break;
                        case 8: tactics.AttackIdea = option; break;
                    }
                    tactics.Summary = tactics.Formation + " · " + tactics.Pressing + " · " + tactics.BuildUp;
                    CareerStore.Save(_career);
                    CloseModal();
                    FillAcademy();
                }, current == option ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), Color.white, 12);
                BroadcastUi.Preferred(button.GetComponent<RectTransform>(), 34);
            }
        }

        void FillAcademyFixtures(string group)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var fixtures = new List<AcademyFixture>();
            foreach (var fixture in _career.AcademyFixtures)
                if (fixture.ClubId == _career.Club.Id && fixture.AgeGroup == group)
                    fixtures.Add(fixture);
            fixtures.Sort((a, b) => a.Date.CompareTo(b.Date));
            var played = 0;
            var won = 0;
            var drawn = 0;
            var lost = 0;
            var goalsFor = 0;
            var goalsAgainst = 0;
            var form = "";
            var formCount = 0;
            for (var i = fixtures.Count - 1; i >= 0; i--)
            {
                var fixture = fixtures[i];
                if (!fixture.Played)
                    continue;
                var oursHome = fixture.HomeClubId == _career.Club.Id;
                var scored = oursHome ? fixture.HomeGoals : fixture.AwayGoals;
                var conceded = oursHome ? fixture.AwayGoals : fixture.HomeGoals;
                played++;
                goalsFor += scored;
                goalsAgainst += conceded;
                var mark = scored > conceded ? (english ? "W" : "V") : scored == conceded ? (english ? "D" : "E") : (english ? "L" : "D");
                if (scored > conceded) won++;
                else if (scored == conceded) drawn++;
                else lost++;
                if (formCount < 5)
                {
                    form = mark + (form.Length == 0 ? "" : "  ") + form;
                    formCount++;
                }
            }

            var board = BroadcastUi.Panel(_academyContent, "YouthResults", new Color(0.1f, 0.04f, 0.2f, 0.96f));
            BroadcastUi.Preferred(board, 420f);
            var record = BroadcastUi.Label(board.transform, "Record",
                (english ? "RESULTS" : "RESULTADOS") + "\n" + won + (english ? " wins" : " vitórias") + "   ·   " + drawn + (english ? " draws" : " empates") + "   ·   " + lost + (english ? " losses" : " derrotas")
                + "\n" + (english ? "Goals " : "Golos ") + goalsFor + "–" + goalsAgainst
                + "\n" + (english ? "Last games  " : "Últimos jogos  ") + (form.Length == 0 ? "—" : form),
                16, Color.white, TextAnchor.UpperLeft);
            record.horizontalOverflow = HorizontalWrapMode.Wrap;
            BroadcastUi.Place(record.rectTransform, 0.03f, 0.62f, 0.97f, 0.96f);

            var lines = new List<SeasonLine>();
            var competition = "academy-" + group;
            foreach (var line in _career.Season)
                if (line != null && line.CompetitionId == competition && line.Apps > 0)
                    lines.Add(line);
            var byRating = new List<SeasonLine>(lines);
            byRating.Sort((a, b) => RatingOf(b).CompareTo(RatingOf(a)));
            var byGoals = new List<SeasonLine>(lines);
            byGoals.Sort((a, b) => b.Goals != a.Goals ? b.Goals.CompareTo(a.Goals) : b.Assists.CompareTo(a.Assists));
            WriteYouthRank(board.transform, "Ratings", 0.03f, 0.5f, english ? "BEST RATINGS" : "MELHORES NOTAS", byRating, true, english);
            WriteYouthRank(board.transform, "Goals", 0.52f, 0.97f, english ? "MOST GOALS" : "MAIS GOLOS", byGoals, false, english);
            if (played == 0)
            {
                var wait = BroadcastUi.Label(board.transform, "Waiting", english ? "No academy matches played in this group yet." : "Ainda não há jogos disputados neste escalão.", 14, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Place(wait.rectTransform, 0.03f, 0.02f, 0.97f, 0.12f);
            }
        }

        static float RatingOf(SeasonLine line)
        {
            return line == null || line.Rated <= 0 ? 0f : line.RatingSum / line.Rated;
        }

        void WriteYouthRank(Transform board, string name, float x0, float x1, string title, List<SeasonLine> lines, bool rating, bool english)
        {
            var head = BroadcastUi.Label(board, name + "Head", title, 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(head.rectTransform, x0, 0.5f, x1, 0.58f);
            var shown = 0;
            for (var i = 0; i < lines.Count && shown < 5; i++)
            {
                var line = lines[i];
                if (!rating && line.Goals <= 0)
                    continue;
                if (!_career.World.Players.TryGetValue(line.PlayerId, out var player))
                    continue;
                var y1 = 0.48f - shown * 0.08f;
                var value = rating
                    ? RatingOf(line).ToString("0.0")
                    : line.Goals + (english ? " goals" : " golos");
                var row = BroadcastUi.Label(board, name + shown, (shown + 1) + "   " + BroadcastUi.ShortName(player.FullName) + "    " + value, 14, Color.white, TextAnchor.MiddleLeft);
                row.horizontalOverflow = HorizontalWrapMode.Wrap;
                row.verticalOverflow = VerticalWrapMode.Truncate;
                BroadcastUi.Place(row.rectTransform, x0, y1 - 0.07f, x1, y1);
                shown++;
            }
            if (shown == 0)
            {
                var empty = BroadcastUi.Label(board, name + "Empty", "—", 14, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Place(empty.rectTransform, x0, 0.36f, x1, 0.44f);
            }
        }

        void OpenAcademyTeam(Club club, string group)
        {
            if (club == null || string.IsNullOrEmpty(group) || _career == null)
                return;
            var before = _career.AcademyPlayerIds.Count;
            AcademyFactory.Ensure(_career, club.Id);
            if (before != _career.AcademyPlayerIds.Count)
                CareerStore.Save(_career);
            OpenClubDossier(club, 1, group, null);
        }

        void PlaceAcademyShape(RectTransform host, List<Player> squad, Club club, string group)
        {
            if (host == null || squad == null || club == null)
                return;
            var formation = club.Id == _career.Club.Id
                ? AcademyTactics(group).Formation
                : club.Tactics == null ? "4-3-3" : club.Tactics.Formation;
            var eleven = new List<Player>();
            var bench = new List<Player>();
            if (club.Id == _career.Club.Id)
                SplitAcademySquad(group, squad, formation, eleven, bench);
            else
                StartingShape.Split(squad, formation, eleven, bench);
            Canvas.ForceUpdateCanvases();
            var bounds = host.rect.size;
            var cardWidth = Mathf.Clamp(bounds.x * 0.19f, 82f, 104f);
            var cardHeight = Mathf.Clamp(bounds.y * 0.19f, 72f, 88f);
            var padX = cardWidth * 0.5f / Mathf.Max(1f, bounds.x) + 0.015f;
            var padY = cardHeight * 0.5f / Mathf.Max(1f, bounds.y) + 0.02f;
            var slotPositions = StartingShape.SlotPositions(formation);
            for (var index = 0; index < eleven.Count && index < 11; index++)
            {
                var player = eleven[index];
                var position = index < slotPositions.Length ? slotPositions[index] : player.Position;
                var slot = MatchEngine.Slot(true, index, formation);
                slot.x = Mathf.Clamp(slot.x, padX, 1f - padX);
                slot.y = Mathf.Clamp(slot.y, padY, 1f - padY);
                var interactive = club.Id == _career.Club.Id;
                Action pickRole = null;
                if (interactive && _academyArmGroup == group && !string.IsNullOrEmpty(_academyArm))
                {
                    var captured = player;
                    var capturedGroup = group;
                    pickRole = () => AssignAcademyRole(captured, capturedGroup);
                }
                var card = PersonCard(host, player, club, true, interactive ? index : -1, false,
                    cardWidth, cardHeight, position, interactive ? "Academy:" + group : null, pickRole);
                card.name = "YouthSlot" + player.Id;
                card.anchorMin = card.anchorMax = slot;
                card.anchoredPosition = Vector2.zero;
                if (!interactive)
                {
                    var drag = card.GetComponent<SquadDragCard>();
                    if (drag != null)
                        drag.enabled = false;
                }
            }
        }

        SeasonLine FindSeasonLine(string playerId)
        {
            foreach (var line in _career.Season)
                if (line.PlayerId == playerId)
                    return line;
            return null;
        }

        void PromoteAcademyPlayer(Player player)
        {
            if (player == null || player.Age < 16 || _career == null || _career.Club == null
                || !_career.AcademyPlayerIds.Contains(player.Id) || _career.Club.PlayerIds.Contains(player.Id))
                return;
            _career.Club.PlayerIds.Add(player.Id);
            player.ClubId = _career.Club.Id;
            var used = new HashSet<int>();
            foreach (var id in _career.Club.PlayerIds)
                if (_career.World.Players.TryGetValue(id, out var squadPlayer) && squadPlayer.Id != player.Id && squadPlayer.SquadNumber > 0)
                    used.Add(squadPlayer.SquadNumber);
            player.SquadNumber = 1;
            while (used.Contains(player.SquadNumber) && player.SquadNumber < 99)
                player.SquadNumber++;
            _career.AcademyPlayerIds.Remove(player.Id);
            CareerStore.Save(_career);
            if (_profileModalPlayer != null && _profileModalPlayer.Id == player.Id)
                CloseModal();
            FillAcademy();
            FillTeam();
        }

        void BuildScoutSummary(Transform host)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var rows = 0;
            foreach (var job in _career.ScoutJobs)
            {
                if (!_career.World.Players.TryGetValue(job.PlayerId, out var player)) continue;
                var label = BroadcastUi.Label(host, "ActiveReport", (english ? "In progress: " : "Em curso: ") + player.FullName, 13, Color.white, TextAnchor.MiddleLeft);
                BroadcastUi.Place(label.rectTransform, 0.03f, 0.48f - rows * 0.23f, 0.97f, 0.72f - rows * 0.23f);
                rows++;
                if (rows >= 2) break;
            }
            foreach (var id in _career.ScoutedPlayers)
            {
                if (!_career.World.Players.TryGetValue(id, out var player)) continue;
                var scout = Member(_career.Club, StaffRole.Scout);
                var rating = StaffReport.Read(scout, player.Id, "overall", PlayerAttributes.CurrentOverall(player), 40, 94);
                var report = BroadcastUi.Label(host, "CompletedReport", (english ? "Ready: " : "Pronto: ") + player.FullName + "  ·  OVR " + rating + "  ·  " + Euro(MarketValue(player)), 13, Color.white, TextAnchor.MiddleLeft);
                BroadcastUi.Place(report.rectTransform, 0.03f, 0.48f - rows * 0.23f, 0.97f, 0.72f - rows * 0.23f);
                rows++;
                if (rows >= 3) break;
            }
            if (rows == 0)
            {
                var empty = BroadcastUi.Label(host, "Empty", english ? "No completed or active scouting reports yet." : "Ainda não há relatórios de observação ativos ou concluídos.", 13, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Place(empty.rectTransform, 0.03f, 0.12f, 0.97f, 0.65f);
            }
        }

        string WeekReport()
        {
            var start = _career.Calendar.CurrentDate.Date.AddDays(-(((int)_career.Calendar.CurrentDate.DayOfWeek + 6) % 7));
            var count = 0;
            foreach (var note in _career.Board)
            {
                var when = note.When == default ? _career.Calendar.CurrentDate : note.When;
                if (when.Date >= start && when.Date < start.AddDays(7))
                    count++;
            }
            var english = LocalizationManager.Current == GameLanguage.English;
            var wins = 0;
            var draws = 0;
            var losses = 0;
            var goalsFor = 0;
            var goalsAgainst = 0;
            foreach (var fixture in _career.Fixtures)
            {
                if (fixture == null || !fixture.Played || fixture.Walkover || !fixture.Involves(_career.Club.Id)
                    || fixture.Date.Date < start || fixture.Date.Date > _career.Calendar.CurrentDate.Date)
                    continue;
                var scored = fixture.IsHome(_career.Club.Id) ? fixture.HomeGoals : fixture.AwayGoals;
                var conceded = fixture.IsHome(_career.Club.Id) ? fixture.AwayGoals : fixture.HomeGoals;
                goalsFor += scored;
                goalsAgainst += conceded;
                if (scored > conceded) wins++;
                else if (scored == conceded) draws++;
                else losses++;
            }

            var analysis = wins + draws + losses == 0
                ? (english ? "There was no first-team result to judge this week; preparation and recovery have been the priority." : "Esta semana não houve resultado da equipa principal para avaliar; a prioridade foi a preparação e a recuperação.")
                : losses > wins
                    ? (english ? "The results fell short. We need a more compact rest-defence and cleaner decisions after losing the ball." : "Os resultados ficaram aquém. Precisamos de proteger melhor a equipa quando perde a bola e decidir com mais critério.")
                    : wins >= 2
                        ? (english ? "The results support the current plan. Keep the shape and build on the chances the team is already creating." : "Os resultados confirmam o plano atual. Mantém a estrutura e melhora a partir das oportunidades que a equipa já cria.")
                        : (english ? "The signs are mixed. Keep the structure, but review the final pass and the timing of the press before the next fixture." : "Os sinais são mistos. Mantém a estrutura, mas revê o último passe e o momento da pressão antes do próximo jogo.");

            var fatigued = 0;
            var lowMorale = 0;
            foreach (var player in Squad(_career.Club))
            {
                if (player.Fatigue >= 70) fatigued++;
                if (player.Morale < 40) lowMorale++;
            }
            if (fatigued >= 3)
                analysis += english ? " Rotation is needed: " + fatigued + " players are carrying heavy fatigue." : " É preciso rodar: " + fatigued + " jogadores acumulam fadiga elevada.";
            else if (lowMorale >= 2)
                analysis += english ? " Two or more players need a direct conversation about their role." : " Dois ou mais jogadores precisam de uma conversa direta sobre o seu papel.";

            var date = LocalizationManager.FormatDate(start, english ? "d MMMM" : "d 'de' MMMM");
            var matchSummary = wins + (english ? "W " : "V ") + draws + (english ? "D " : "E ") + losses + (english ? "L" : "D");
            var results = wins + draws + losses == 0
                ? (english ? "No first-team matches completed." : "Sem jogos da equipa principal concluídos.")
                : (english ? "First-team record: " : "Registo da equipa principal: ") + matchSummary
                    + (english ? " · goals " : " · golos ") + goalsFor + "–" + goalsAgainst + ".";
            return english
                ? "Week of " + date + " · " + count + " board updates. " + results + " Coach's review: " + analysis + " Next: " + EnglishMatchLabel(_career)
                : "Semana de " + date + " · " + count + " atualizações à direção. " + results + " Análise do treinador: " + analysis + " Próximo: " + NextMatchText(_career);
        }

        void KeepCaptainOnPitch()
        {
            var eleven = Eleven();
            foreach (var player in eleven)
            {
                if (player.IsCaptain)
                    return;
            }

            if (eleven.Count == 0)
                return;
            foreach (var player in Squad(_career.Club))
                player.IsCaptain = false;
            eleven[eleven.Count > 1 ? 1 : 0].IsCaptain = true;
        }

        void FillTeam()
        {
            Clear(_pitch);
            Clear(_subs);
            KeepCaptainOnPitch();
            PlaceTeamLineup(_pitch);
            if (_teamTitle != null)
                _teamTitle.text = (LocalizationManager.Current == GameLanguage.English ? "SQUAD  ·  " : "PLANTEL  ·  ") + _career.Club.Tactics.Formation;
            if (_subsTab != null) _subsTab.GetComponentInChildren<Text>().text = LocalizationManager.Current == GameLanguage.English ? "SUBSTITUTES" : "SUPLENTES";
            if (_reservesTab != null) _reservesTab.GetComponentInChildren<Text>().text = LocalizationManager.Current == GameLanguage.English ? "RESERVES" : "RESERVAS";
            var bench = BenchOf(_career.Club);
            var shown = new List<Player>();
            if (_reserves)
            {
                for (var i = 7; i < bench.Count; i++)
                    shown.Add(bench[i]);
            }
            else
            {
                for (var i = 0; i < bench.Count && i < 7; i++)
                    shown.Add(bench[i]);
            }

            if (shown.Count == 0)
            {
                var empty = BroadcastUi.Label(_subs, "Empty", LocalizationManager.Current == GameLanguage.English
                    ? (_reserves ? "No reserves." : "No substitutes.") : (_reserves ? "Sem reservas." : "Sem suplentes."), 16, UiTheme.Muted, TextAnchor.MiddleCenter);
                empty.rectTransform.anchorMin = empty.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                empty.rectTransform.sizeDelta = new Vector2(240, 32);
            }

            const float cardWidth = 96f;
            const float cardHeight = 76f;
            const float cardGap = 8f;
            const float sidePadding = 12f;
            Canvas.ForceUpdateCanvases();
            var viewportWidth = _subsViewport == null ? 0f : _subsViewport.rect.width;
            if (viewportWidth <= 1f)
                viewportWidth = Screen.width * 0.33f;
            var contentWidth = Mathf.Max(viewportWidth, sidePadding * 2f + shown.Count * cardWidth + Mathf.Max(0, shown.Count - 1) * cardGap);
            _subs.anchorMin = new Vector2(0f, 0f);
            _subs.anchorMax = new Vector2(0f, 1f);
            _subs.pivot = new Vector2(0f, 0.5f);
            _subs.sizeDelta = new Vector2(contentWidth, 0f);
            _subs.anchoredPosition = Vector2.zero;
            if (_subsScroll != null)
            {
                _subsScroll.StopMovement();
                _subsScroll.horizontalNormalizedPosition = 0f;
            }
            for (var i = 0; i < shown.Count; i++)
            {
                var card = PersonCard(_subs, shown[i], _career.Club, true, -1, true, cardWidth, cardHeight);
                var x = sidePadding + cardWidth * 0.5f + i * (cardWidth + cardGap);
                card.anchorMin = card.anchorMax = new Vector2(x / contentWidth, 0.5f);
                card.sizeDelta = new Vector2(cardWidth, cardHeight);
            }

            var canScrollSubs = contentWidth > viewportWidth + 1f;
            if (_subsPrev != null)
            {
                _subsPrev.interactable = canScrollSubs;
                _subsPrev.gameObject.SetActive(_teamZone == 0 && canScrollSubs);
            }
            if (_subsNext != null)
            {
                _subsNext.interactable = canScrollSubs;
                _subsNext.gameObject.SetActive(_teamZone == 0 && canScrollSubs);
            }
            if (_benchSwapHint != null)
            {
                _benchSwapHint.text = _pendingBenchSwap == null ? "" : LocalizationManager.Current == GameLanguage.English
                    ? "Choose a player to swap with " + BroadcastUi.ShortName(_pendingBenchSwap.FullName) + "."
                    : "Escolhe o jogador para trocar com " + BroadcastUi.ShortName(_pendingBenchSwap.FullName) + ".";
                _benchSwapHint.gameObject.SetActive(_teamZone == 0 && _pendingBenchSwap != null);
            }

            if (_roles != null)
                _roles.text = RoleLine(_career.Club);
            if (_pane == 3)
                FillTactics();
            FillSheet();
            if (_subsTab != null)
            {
                _subsTab.GetComponent<Image>().color = _reserves ? new Color(1f, 1f, 1f, 0.08f) : UiTheme.Accent;
                _reservesTab.GetComponent<Image>().color = _reserves ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f);
            }
        }

        void SelectBenchGroup(bool reserves)
        {
            if (_pendingBenchSwap != null && reserves == _pendingBenchSwapFromReserves)
                _pendingBenchSwap = null;
            _reserves = reserves;
            FillTeam();
        }

        void FillSheet()
        {
            if (_sheet == null)
                return;
            Clear(_sheet);
            var paneIds = new[] { 1, 3 };
            if (_pane == 0 || _pane == 2)
                _pane = 1;
            for (var i = 0; i < _paneTabs.Length && i < paneIds.Length; i++)
            {
                if (_paneTabs[i] == null)
                    continue;
                var on = paneIds[i] == _pane;
                _paneTabs[i].GetComponent<Image>().color = on ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f);
                _paneTabs[i].GetComponentInChildren<Text>().color = on ? UiTheme.ButtonText : Color.white;
            }

            if (_pane == 0)
                SheetFormations();
            else if (_pane == 1)
                SheetRoles();
            else             if (_pane == 2)
                SheetOrders();
            if (_tacHost != null)
                _tacHost.SetActive(_pane == 3);
            if (_sheet != null)
                _sheet.gameObject.SetActive(_pane != 3);
        }

        void SheetFormations()
        {
            var forms = new[] { "4-3-3", "4-2-3-1", "4-4-2", "3-4-3", "5-4-1" };
            for (var i = 0; i < forms.Length; i++)
            {
                var formation = forms[i];
                var on = _career.Club.Tactics.Formation == formation;
                var y = 0.82f - i * 0.16f;
                var button = BroadcastUi.Button(_sheet, "F" + i, formation, () =>
                {
                    _career.Club.Tactics.Formation = formation;
                    _career.Club.Tactics.Summary = formation + " · " + _career.Club.Tactics.Pressing;
                    _career.Club.ManualLineup.Clear();
                    FillTeam();
                }, on ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), on ? UiTheme.ButtonText : Color.white, 18);
                BroadcastUi.Place(button.GetComponent<RectTransform>(), 0.06f, y, 0.94f, y + 0.14f);
            }
        }

        void SheetRoles()
        {
            var squad = Squad(_career.Club);
            ArmButton("CAPITÃO", "captain", Who(CaptainOf(Eleven())), 0.78f);
            ArmButton("PENÁLTIS", "penalty", Who(PlayerById(squad, _career.Club.PenaltyTakerId) ?? Best(squad, p => p.Attributes.Finishing)), 0.58f);
            ArmButton("CANTOS", "corner", Who(PlayerById(squad, _career.Club.CornerTakerId) ?? Best(squad, p => p.Attributes.Crossing)), 0.38f);
            ArmButton("LIVRES", "freekick", Who(PlayerById(squad, _career.Club.FreeKickTakerId) ?? Best(squad, p => p.Attributes.Passing + p.Attributes.Composure)), 0.18f);
            var hint = BroadcastUi.Label(_sheet, "Hint", _arm == null ? "Clica numa função e depois num titular." : "Agora clica num jogador do 11 inicial.", 14, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(hint.rectTransform, 0.04f, 0.02f, 0.96f, 0.16f);
        }

        void ArmButton(string title, string key, string who, float y)
        {
            var on = _arm == key;
            var button = BroadcastUi.Button(_sheet, title, title + "   " + who, () =>
            {
                _arm = key;
                FillSheet();
            }, on ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), on ? UiTheme.ButtonText : Color.white, 16);
            BroadcastUi.Place(button.GetComponent<RectTransform>(), 0.04f, y, 0.96f, y + 0.16f);
        }

        List<Player> Eleven()
        {
            var eleven = new List<Player>();
            var bench = new List<Player>();
            StartingShape.Resolve(Squad(_career.Club), _career.Club, eleven, bench);
            return eleven;
        }

        void CycleCaptain(List<Player> squad)
        {
            if (squad.Count == 0)
                return;
            var index = 0;
            for (var i = 0; i < squad.Count; i++)
            {
                if (!squad[i].IsCaptain)
                    continue;
                index = i;
                break;
            }

            foreach (var player in squad)
                player.IsCaptain = false;
            squad[(index + 1) % squad.Count].IsCaptain = true;
            FillTeam();
        }

        void CycleId(List<Player> squad, string current, Action<string> set)
        {
            if (squad.Count == 0)
                return;
            var index = 0;
            for (var i = 0; i < squad.Count; i++)
            {
                if (squad[i].Id != current)
                    continue;
                index = i;
                break;
            }

            set(squad[(index + 1) % squad.Count].Id);
            FillTeam();
        }

        Player CaptainOf(List<Player> squad)
        {
            foreach (var player in squad)
            {
                if (player.IsCaptain)
                    return player;
            }

            return squad.Count == 0 ? null : squad[0];
        }

        static Player PlayerById(List<Player> squad, string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            foreach (var player in squad)
            {
                if (player.Id == id)
                    return player;
            }

            return null;
        }

        static Player Best(List<Player> squad, Func<Player, int> score)
        {
            Player best = null;
            var mark = -1;
            foreach (var player in squad)
            {
                if (player.Attributes == null)
                    continue;
                var value = score(player);
                if (value <= mark)
                    continue;
                mark = value;
                best = player;
            }

            return best;
        }

        void SheetOrders()
        {
            var player = string.IsNullOrEmpty(_orderId) ? null : PlayerById(Squad(_career.Club), _orderId);
            if (player == null)
            {
                var hint = BroadcastUi.Label(_sheet, "Hint", "Clica num titular. As instruções da posição aparecem aqui.", 16, Color.white, TextAnchor.UpperLeft);
                BroadcastUi.Place(hint.rectTransform, 0.04f, 0.2f, 0.96f, 0.8f);
                return;
            }

            var head = BroadcastUi.Label(_sheet, "Who", player.SquadNumber + "  " + player.FullName, 18, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(head.rectTransform, 0.04f, 0.88f, 0.96f, 0.98f);
            var pos = BroadcastUi.Label(_sheet, "Pos", PresentationLabels.Position(player.Position), 14, UiTheme.Accent, TextAnchor.MiddleLeft);
            BroadcastUi.Place(pos.rectTransform, 0.04f, 0.8f, 0.96f, 0.88f);
            var options = IndividualOrders.ForPosition(player.Position);
            var duty = string.IsNullOrEmpty(player.Duty) ? "Equilibrar" : player.Duty;
            var step = options.Length > 5 ? 0.09f : 0.11f;
            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var on = option == duty;
                var y = 0.7f - i * step;
                var row = BroadcastUi.Button(_sheet, "Duty" + i, option, () =>
                {
                    player.Duty = option;
                    FillTeam();
                }, on ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), on ? UiTheme.ButtonText : Color.white, 16);
                BroadcastUi.Place(row.GetComponent<RectTransform>(), 0.04f, y, 0.96f, y + step - 0.012f);
            }

            var fit = PlayerTacticalFit.Evaluate(player, _career.Club.Tactics, duty);
            var english = LocalizationManager.Current == GameLanguage.English;
            var strongest = LocalizationManager.Translate(fit.StrongestAttribute);
            var development = LocalizationManager.Translate(fit.DevelopmentAttribute);
            var fitText = english
                ? "ROLE FIT  " + fit.RoleGrade + "  (" + fit.RoleScore.ToString("0.0") + "/20)   ·   SYSTEM FIT  " + fit.TacticalGrade + "  (" + fit.TacticalScore.ToString("0.0") + "/20)\nStrength: " + strongest + " " + fit.StrongestValue + "   ·   Development area: " + development + " " + fit.DevelopmentValue + "\nTactical familiarity is not modelled yet. Fit explains suitability; it does not boost match attributes."
                : "ADEQUAÇÃO À FUNÇÃO  " + fit.RoleGrade + "  (" + fit.RoleScore.ToString("0.0") + "/20)   ·   ADEQUAÇÃO AO SISTEMA  " + fit.TacticalGrade + "  (" + fit.TacticalScore.ToString("0.0") + "/20)\nPonto forte: " + strongest + " " + fit.StrongestValue + "   ·   A desenvolver: " + development + " " + fit.DevelopmentValue + "\nA familiaridade tática ainda não é modelada. A nota explica a adequação e não aumenta atributos no jogo.";
            var fitLabel = BroadcastUi.Label(_sheet, "PlayerTacticalFit", fitText, 10, UiTheme.Muted, TextAnchor.UpperLeft);
            BroadcastUi.Place(fitLabel.rectTransform, 0.04f, 0.015f, 0.96f, 0.22f);
        }

        string RoleLine(Club club)
        {
            var squad = Squad(club);
            Player captain = null;
            Player penalties = null;
            Player corners = null;
            Player freeKicks = null;
            foreach (var player in squad)
            {
                if (player.IsCaptain)
                    captain = player;
                if (penalties == null || player.Attributes.Finishing > penalties.Attributes.Finishing)
                    penalties = player;
                if (corners == null || player.Attributes.Crossing > corners.Attributes.Crossing)
                    corners = player;
                if (freeKicks == null || player.Attributes.Passing + player.Attributes.Composure > freeKicks.Attributes.Passing + freeKicks.Attributes.Composure)
                    freeKicks = player;
            }

            return "CAPITÃO   " + Who(captain) + "      PENÁLTIS   " + Who(penalties) + "      CANTOS   " + Who(corners) + "      LIVRES   " + Who(freeKicks);
        }

        static string Who(Player player)
        {
            return player == null ? "—" : BroadcastUi.ShortName(player.FullName);
        }

        string ClubDigest()
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var hurt = new List<string>();
            var tired = new List<string>();
            var low = new List<string>();
            foreach (var squadMember in Squad(_career.Club))
            {
                if (squadMember.Injury != null && squadMember.Injury.IsInjured)
                    hurt.Add(BroadcastUi.ShortName(squadMember.FullName));
                if (squadMember.Fatigue >= 70)
                    tired.Add(BroadcastUi.ShortName(squadMember.FullName));
                if (squadMember.Morale < 40)
                    low.Add(squadMember.FullName);
            }

            var captain = "";
            foreach (var captainCandidate in Squad(_career.Club))
            {
                if (captainCandidate.IsCaptain)
                    captain = captainCandidate.FullName;
            }

            var recent = new List<Fixture>();
            foreach (var fixture in _career.Fixtures)
                if (fixture != null && fixture.Played && !fixture.Walkover && fixture.Involves(_career.Club.Id)
                    && fixture.Date.Date <= _career.Calendar.CurrentDate.Date)
                    recent.Add(fixture);
            recent.Sort((a, b) => b.Date.CompareTo(a.Date));
            var form = "";
            for (var i = 0; i < recent.Count && i < 3; i++)
            {
                var fixture = recent[i];
                var scored = fixture.IsHome(_career.Club.Id) ? fixture.HomeGoals : fixture.AwayGoals;
                var conceded = fixture.IsHome(_career.Club.Id) ? fixture.AwayGoals : fixture.HomeGoals;
                var result = scored > conceded ? (english ? "W" : "V")
                    : scored == conceded ? (english ? "D" : "E") : (english ? "L" : "D");
                form += (form.Length == 0 ? "" : " · ") + result;
            }

            var fixtureLabel = english ? EnglishMatchLabel(_career) : NextMatchText(_career);
            var formation = _career.Club.Tactics == null || string.IsNullOrEmpty(_career.Club.Tactics.Formation)
                ? "4-3-3" : _career.Club.Tactics.Formation;
            var diagnosis = english
                ? "The group is settled. Keep the current shape and protect the wide areas."
                : "O grupo está estável. Mantém a estrutura e protege os corredores.";
            if (hurt.Count > 0)
                diagnosis = english
                    ? "Plan around the unavailable players; avoid asking the remaining defenders to cover extra ground."
                    : "Prepara a equipa sem os lesionados; evita pedir aos defesas disponíveis que cubram terreno a mais.";
            else if (tired.Count >= 3)
                diagnosis = english
                    ? "Several players are carrying fatigue. Rotate the most tired starters before the next match."
                    : "Há vários jogadores cansados. Roda os titulares mais fatigados antes do próximo jogo.";
            else if (low.Count >= 2 || _career.Club.DressingRoomTension >= 65)
                diagnosis = english
                    ? "Confidence is fragile. Keep the message clear and speak to the players whose morale has dipped."
                    : "A confiança está frágil. Mantém a mensagem clara e fala com quem perdeu moral.";

            var recentLine = form.Length == 0
                ? (english ? "Recent form: no completed results yet." : "Forma recente: ainda sem resultados.")
                : (english ? "Last " : "Últimos ") + form.Length + (english ? " results: " : " jogos: ") + form;
            var captainLabel = english ? (captain.Length == 0 ? "no captain recorded" : captain) : (captain.Length == 0 ? "sem capitão registado" : captain);
            var text = english
                ? "Captain: " + captainLabel + " · " + recentLine + "\nNext: " + fixtureLabel + "\nSquad check — injured: " + (hurt.Count == 0 ? "none" : string.Join(", ", hurt))
                    + "; fatigue: " + (tired.Count == 0 ? "under control" : string.Join(", ", tired))
                    + "; low morale: " + (low.Count == 0 ? "none" : string.Join(", ", low))
                    + "\nCoach's read (" + formation + ", dressing room " + RoomWord(_career.Club.DressingRoomTension) + "): " + diagnosis
                : "Capitão: " + captainLabel + " · " + recentLine + "\nPróximo: " + fixtureLabel + "\nPlantel — lesionados: " + (hurt.Count == 0 ? "nenhum" : string.Join(", ", hurt))
                    + "; fadiga: " + (tired.Count == 0 ? "controlada" : string.Join(", ", tired))
                    + "; moral baixa: " + (low.Count == 0 ? "nenhuma" : string.Join(", ", low))
                    + "\nLeitura do treinador (" + formation + ", balneário " + RoomWord(_career.Club.DressingRoomTension) + "): " + diagnosis;
            return text;
        }

        static string RoomWord(int tension)
        {
            if (LocalizationManager.Current == GameLanguage.English)
            {
                if (tension >= 80) return "terrible";
                if (tension >= 65) return "poor";
                if (tension >= 50) return "unsettled";
                if (tension >= 35) return "good";
                if (tension >= 20) return "very good";
                return "excellent";
            }
            if (tension >= 80)
                return "péssimo";
            if (tension >= 65)
                return "mau";
            if (tension >= 50)
                return "instável";
            if (tension >= 35)
                return "bom";
            if (tension >= 20)
                return "muito bom";
            return "excelente";
        }

        void PlaceShape(Transform host, Club club, bool fatigue, float width, float height)
        {
            var eleven = new List<Player>();
            var bench = new List<Player>();
            StartingShape.Resolve(Squad(club), club, eleven, bench);
            var formation = club.Tactics == null ? "4-3-3" : club.Tactics.Formation;
            var lines = StartingShape.Lines(formation);
            var cursor = 0;
            var rows = lines.Length;
            for (var row = 0; row < rows; row++)
            {
                var count = lines[row];
                var x = rows == 1 ? 0.5f : 0.09f + row * (0.82f / (rows - 1));
                for (var col = 0; col < count && cursor < eleven.Count; col++)
                {
                    var fieldIndex = cursor;
                    var card = PersonCard(host, eleven[cursor], club, fatigue, fieldIndex, false, width, height);
                    var y = 0.12f + (col + 1f) * 0.76f / (count + 1f);
                    card.anchorMin = card.anchorMax = new Vector2(x, y);
                    card.sizeDelta = new Vector2(width, height);
                    cursor++;
                }
            }
        }

        void PlaceVerticalShape(RectTransform host, Club club, bool fatigue, float width, float height)
        {
            var eleven = new List<Player>();
            var bench = new List<Player>();
            StartingShape.Resolve(Squad(club), club, eleven, bench);
            var formation = club.Tactics == null ? "4-3-3" : club.Tactics.Formation;
            for (var index = 0; index < eleven.Count && index < 11; index++)
            {
                var card = PersonCard(host, eleven[index], club, fatigue, index, false, width, height);
                card.anchorMin = card.anchorMax = MatchEngine.Slot(true, index, formation);
                card.sizeDelta = new Vector2(width, height);
            }
        }

        void PlaceTeamLineup(RectTransform host)
        {
            if (host == null)
                return;
            var eleven = new List<Player>();
            var bench = new List<Player>();
            StartingShape.Resolve(Squad(_career.Club), _career.Club, eleven, bench);
            Canvas.ForceUpdateCanvases();
            var field = host.rect.size;
            if (field.x <= 1f || field.y <= 1f)
                field = new Vector2(Screen.width * 0.39f, Screen.height * 0.61f);
            var width = Mathf.Clamp(field.x * 0.145f, 76f, 100f);
            var height = Mathf.Clamp(field.y * 0.2f, 58f, 78f);
            var padX = width * 0.5f / field.x + 0.012f;
            var padY = height * 0.5f / field.y + 0.015f;
            for (var index = 0; index < eleven.Count && index < 11; index++)
            {
                var card = PersonCard(host, eleven[index], _career.Club, true, index, false, width, height);
                var slot = MatchEngine.Slot(true, index, _career.Club.Tactics.Formation);
                slot.x = Mathf.Clamp(slot.x, padX, 1f - padX);
                slot.y = Mathf.Clamp(slot.y, padY, 1f - padY);
                card.anchorMin = card.anchorMax = slot;
                card.sizeDelta = new Vector2(width, height);
            }
        }

        List<Player> BenchOf(Club club)
        {
            var eleven = new List<Player>();
            var bench = new List<Player>();
            StartingShape.Resolve(Squad(club), club, eleven, bench);
            return bench;
        }

        void OnPerson(Player player, bool bench)
        {
            if (SquadDragCard.Holding)
                return;
            if (player == null)
                return;
            if (bench && _pendingBenchSwap != null)
            {
                if (_reserves != _pendingBenchSwapFromReserves && player.Id != _pendingBenchSwap.Id)
                {
                    StartingShape.SwapBenchPlayers(_career.Club, Squad(_career.Club), _pendingBenchSwap, player);
                    CareerStore.Save(_career);
                }
                _pendingBenchSwap = null;
                FillTeam();
                return;
            }
            if (!bench && _pane == 1 && !string.IsNullOrEmpty(_arm))
            {
                AssignArmed(player);
                return;
            }

            if (!bench && _pane == 2)
            {
                _orderId = player.Id;
                FillSheet();
                return;
            }

            OpenPlayer(player, false);
        }

        void AssignArmed(Player player)
        {
            if (player == null)
                return;
            var eleven = Eleven();
            var onPitch = false;
            foreach (var starter in eleven)
            {
                if (starter.Id == player.Id)
                    onPitch = true;
            }

            if (!onPitch)
                return;
            if (_arm == "captain")
            {
                foreach (var other in Squad(_career.Club))
                    other.IsCaptain = other.Id == player.Id;
            }
            else if (_arm == "penalty")
                _career.Club.PenaltyTakerId = player.Id;
            else if (_arm == "corner")
                _career.Club.CornerTakerId = player.Id;
            else if (_arm == "freekick")
                _career.Club.FreeKickTakerId = player.Id;
            _arm = null;
            FillTeam();
        }

        void SwapStarter(Player benchPlayer, int fieldIndex)
        {
            StartingShape.SwapWithBench(_career.Club, Squad(_career.Club), fieldIndex, benchPlayer);
            FillTeam();
        }

        void SwapPositions(int fromIndex, int toIndex)
        {
            StartingShape.SwapPositions(_career.Club, Squad(_career.Club), fromIndex, toIndex);
            FillTeam();
        }

        void FillStaff()
        {
            Clear(_staffHost);
            EnsureScout(_career.Club);
            VaryStaff(_career.Club);
            OfferWeeklyStaff();
            var english = LocalizationManager.Current == GameLanguage.English;
            var counts = BroadcastUi.Panel(_staffHost, "StaffCounts", new Color(0.08f, 0.04f, 0.16f, 0.96f));
            BroadcastUi.Preferred(counts, 58);
            var summary = BroadcastUi.Label(counts.transform, "Counts", StaffCountsLine(english), 14, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            summary.resizeTextForBestFit = true;
            summary.resizeTextMinSize = 10;
            summary.resizeTextMaxSize = 14;
            BroadcastUi.Place(summary.rectTransform, 0.03f, 0.1f, 0.7f, 0.9f);
            if (LatestStaffOffer(out var recommended))
            {
                var full = ClubStaffCount() >= MaxClubStaff;
                var hire = BroadcastUi.Button(counts.transform, "HireRec", full
                    ? (english ? "STAFF LIMIT REACHED" : "LIMITE DE STAFF ATINGIDO")
                    : (english ? "NEGOTIATE WITH " + recommended.FullName : "NEGOCIAR COM " + recommended.FullName),
                    () => OpenStaffContractOffer(new StaffOfferCandidate { Member = recommended, AskingWage = StaffAskingWage(recommended), ContractYears = 2 }, null), UiTheme.Accent, UiTheme.ButtonText, 12);
                BroadcastUi.Place(hire.GetComponent<RectTransform>(), 0.72f, 0.16f, 0.97f, 0.84f);
                hire.interactable = !full;
            }
            var staff = _career.Club.Staff;
            for (var i = 0; i < staff.Count; i++)
            {
                var member = staff[i];
                var card = BroadcastUi.Panel(_staffHost, "Staff" + i, new Color(0.16f, 0.07f, 0.3f, 0.94f), true);
                BroadcastUi.Preferred(card, 76);
                var captured = member;
                BroadcastUi.Hit(card, () => OpenStaff(captured));
                var portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                var portraitRect = portraitObject.GetComponent<RectTransform>();
                portraitRect.SetParent(card.transform, false);
                BroadcastUi.Place(portraitRect, 0.015f, 0.08f, 0.075f, 0.92f);
                var portrait = portraitObject.GetComponent<Image>();
                portrait.sprite = FaceBook.For(member);
                portrait.preserveAspect = true;
                portrait.raycastTarget = false;

                var name = BroadcastUi.Label(card.transform, "Name", member.FullName, 15, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(name.rectTransform, 0.09f, 0.47f, 0.55f, 0.9f);
                var role = BroadcastUi.Label(card.transform, "Role", LocalizationManager.Translate(PresentationLabels.StaffRole(member.Role)), 12, UiTheme.Accent, TextAnchor.MiddleLeft);
                BroadcastUi.Place(role.rectTransform, 0.09f, 0.08f, 0.55f, 0.47f);
                var read = BroadcastUi.Label(card.transform, "Read", StaffProfile.BestLine(member, LocalizationManager.Current == GameLanguage.English), 12, UiTheme.Muted, TextAnchor.MiddleRight);
                BroadcastUi.Place(read.rectTransform, 0.56f, 0.12f, 0.98f, 0.88f);
            }

            if (_vibe != null)
                _vibe.text = VibeLine();
        }

        public void BakeDesign(CareerState career)
        {
            _career = career;
            var root = BroadcastUi.CreateDesign(transform);
            BroadcastUi.Backdrop(root);
            LeagueBackdrop.Paint(root, HubLeagueId());
            BuildHeader(root);
            BuildPhone(root);
            BuildTabs(root);
            BuildPages(root);
            BuildPlay(root);
            BuildModal(root);
        }

        void BindDesign(Transform root)
        {
            var pagesRect = root.Find("Pages") as RectTransform;
            if (pagesRect != null)
                BroadcastUi.Place(pagesRect, 0.03f, 0.075f, 0.97f, 0.815f);
            var settingsHint = root.Find("SettingsHint") as RectTransform;
            if (settingsHint != null)
                settingsHint.gameObject.SetActive(false);
            var clubName = SceneBind.Get<Text>(root, "Header/ClubName");
            if (clubName != null)
                clubName.text = _career.Club.Name.ToUpperInvariant();
            var crest = SceneBind.Get<Image>(root, "Header/Crest");
            if (crest != null)
                crest.sprite = CrestStudio.For(_career.Club);
            LeagueBackdrop.Paint(root, HubLeagueId());

            _dateLine = SceneBind.Get<Text>(root, "Header/Date");
            _clock = SceneBind.Get<Text>(root, "ClockCard/Clock");
            EnsureAlert(root);
            _phoneSheet = root.Find("Phone") as RectTransform;
            _phoneApps = root.Find("Phone/Screen/Apps").gameObject;
            _phonePage = root.Find("Phone/Screen/Page").gameObject;
            _phoneAppTitle = SceneBind.Get<Text>(root, "Phone/Screen/Page/AppTitle");
            _phoneList = root.Find("Phone/Screen/Page/Msgs/Content") as RectTransform;
            SceneBind.Wire(root.Find("Phone/Chin"), PressChin);
            var apps = new[] { "Mensagens", "X" };
            for (var i = 0; i < 6; i++)
            {
                var node = root.Find("Phone/Screen/Apps/App" + i);
                if (node == null)
                    continue;
                if (i >= apps.Length)
                {
                    node.gameObject.SetActive(false);
                    continue;
                }

                var app = apps[i];
                node.gameObject.SetActive(true);
                var label = node.GetComponentInChildren<Text>(true);
                if (label != null)
                    label.text = app;
                var appButton = node.GetComponent<Button>();
                if (appButton != null && appButton.targetGraphic != null)
                    appButton.targetGraphic.color = AppColor(app);
                SceneBind.Wire(node, () => OpenPhoneApp(app));
            }

            _tabBar = root.Find("Tabs") as RectTransform;
            _tabs = new Button[8];
            for (var i = 0; i < 7; i++)
            {
                var index = i;
                _tabs[i] = SceneBind.Wire(root.Find("Tabs/Tab" + i), () => Show(index));
            }
            var designTab = root.Find("Tabs/Tab7");
            _tabs[7] = designTab == null
                ? Tab(_tabBar, "Tab7", "TREINO", 0f, 0.12f, 7)
                : SceneBind.Wire(designTab, () => Show(7));
            for (var i = 0; i < _tabs.Length; i++)
            {
                var x0 = 0.004f + i * 0.124f;
                BroadcastUi.Place(_tabs[i].GetComponent<RectTransform>(), x0, 0.08f, x0 + 0.12f, 0.92f);
            }
            RefreshCareerTabLabels();

            _pages = new GameObject[8];
            _pages[0] = root.Find("Pages/Home").gameObject;
            _pages[1] = root.Find("Pages/News").gameObject;
            _pages[2] = root.Find("Pages/Board").gameObject;
            _pages[3] = root.Find("Pages/Team").gameObject;
            _pages[5] = root.Find("Pages/Staff").gameObject;
            _pages[6] = root.Find("Pages/Calendar").gameObject;
            _pages[7] = root.Find("Pages/Standings").gameObject;
            _pages[4] = BuildTraining(root.Find("Pages"));
            Clear(_pages[2].GetComponent<RectTransform>());
            BuildAcademyContents(_pages[2].transform);
            Clear(_pages[6].GetComponent<RectTransform>());
            BuildOfficeContents(_pages[6].transform);
            RefreshCareerTabLabels();

            _replies = new Button[3];

            _newsContent = root.Find("Pages/News/Scroll/Content") as RectTransform;
            var newsMemoriesButton = root.Find("Pages/News/Memories");
            if (newsMemoriesButton != null)
                newsMemoriesButton.gameObject.SetActive(false);

            var team = root.Find("Pages/Team");
            _teamTitle = SceneBind.Get<Text>(team, "Title");
            if (_teamTitle != null) BroadcastUi.Place(_teamTitle.rectTransform, 0.45f, 0.93f, 0.69f, 0.99f);
            var teamCaveat = SceneBind.Get<Text>(team, "Caveat");
            if (teamCaveat != null) BroadcastUi.Place(teamCaveat.rectTransform, 0.70f, 0.93f, 0.99f, 0.99f);
            _pitchBg = SceneBind.Get<Image>(team, "PitchBg");
            _pitch = team.Find("Pitch") as RectTransform;
            ConfigureTeamPitch();
            _subs = team.Find("Subs") as RectTransform;
            _sheet = team.Find("Sheet") as RectTransform;
            _tacticsBody = team.Find("Tactics") as RectTransform;
            _tacHost = _tacticsBody.gameObject;
            _subsTab = SceneBind.Wire(team.Find("SubsTab"), () => SelectBenchGroup(false));
            _reservesTab = SceneBind.Wire(team.Find("ReservesTab"), () => SelectBenchGroup(true));
            if (_subsTab != null) BroadcastUi.Place(_subsTab.GetComponent<RectTransform>(), 0.03f, 0.235f, 0.222f, 0.295f);
            if (_reservesTab != null) BroadcastUi.Place(_reservesTab.GetComponent<RectTransform>(), 0.228f, 0.235f, 0.42f, 0.295f);
            var bakedPanes = new[] { 1, 3 };
            var bakedLabels = LocalizationManager.Current == GameLanguage.English
                ? new[] { "ROLES", "TACTICS" } : new[] { "FUNÇÕES", "TÁTICAS" };
            _paneTabs = new Button[bakedPanes.Length];
            if (_pane == 0 || _pane == 2)
                _pane = 1;
            for (var i = 0; i < bakedPanes.Length; i++)
            {
                var pane = bakedPanes[i];
                var node = team.Find("Pane" + pane) ?? team.Find("Pane" + i);
                _paneTabs[i] = SceneBind.Wire(node, () => SelectPane(pane));
                if (_paneTabs[i] != null)
                {
                    var caption = _paneTabs[i].GetComponentInChildren<Text>();
                    if (caption != null)
                        caption.text = bakedLabels[i];
                    var y1 = 0.93f - i * 0.075f;
                    BroadcastUi.Place(_paneTabs[i].GetComponent<RectTransform>(), 0.45f, y1 - 0.068f, 0.98f, y1);
                }
            }
            for (var hidden = 0; hidden < 4; hidden++)
            {
                if (hidden == 1 || hidden == 3)
                    continue;
                var extra = team.Find("Pane" + hidden);
                if (extra != null && ( _paneTabs[0] == null || extra != _paneTabs[0].transform) && (_paneTabs[1] == null || extra != _paneTabs[1].transform))
                    extra.gameObject.SetActive(false);
            }
            if (_sheet != null) BroadcastUi.Place(_sheet, 0.45f, 0.02f, 0.98f, 0.6f);
            if (_tacticsBody != null) BroadcastUi.Place(_tacticsBody, 0.45f, 0.02f, 0.98f, 0.6f);
            ConfigureTeamSubstitutes(team, _subs);

            _zoneButtons = new Button[2];
            for (var i = 0; i < _zoneButtons.Length; i++)
            {
                var zone = i;
                _zoneButtons[i] = SceneBind.Wire(team.Find("Zone" + i), () => ShowTeamZone(zone));
                if (_zoneButtons[i] != null)
                {
                    _zoneButtons[i].gameObject.SetActive(true);
                    var x0 = 0.01f + i * 0.16f;
                    BroadcastUi.Place(_zoneButtons[i].GetComponent<RectTransform>(), x0, 0.93f, x0 + 0.15f, 0.99f);
                    var label = _zoneButtons[i].GetComponentInChildren<Text>();
                    if (label != null) label.text = LocalizationManager.Current == GameLanguage.English
                        ? (i == 0 ? "TACTICS" : "SQUAD") : (i == 0 ? "TÁTICAS" : "PLANTEL");
                }
            }
            var unusedZone = team.Find("Zone2");
            if (unusedZone != null) unusedZone.gameObject.SetActive(false);

            _listPage = team.Find("ListPage").gameObject;
            _listHost = team.Find("ListPage/ListScroll/Content") as RectTransform;
            _marketPage = team.Find("MarketPage").gameObject;
            _marketHost = team.Find("MarketPage/MarketScroll/Content") as RectTransform;
            _marketSearch = SceneBind.Get<InputField>(team, "MarketPage/Input");
            BuildTeamRoster(team);
            if (_marketSearch != null)
            {
                _marketSearch.onValueChanged.RemoveAllListeners();
                _marketSearch.onValueChanged.AddListener(value =>
                {
                    if (_transferPage == 1)
                        FillMarket();
                });
            }
            ShowTeamZone(0);

            var transferPage = root.Find("Pages/Staff");
            if (transferPage != null)
                ConfigureTransfersPage(transferPage);
            var legacyVibe = transferPage == null ? null : transferPage.Find("Vibe");
            if (legacyVibe != null) legacyVibe.gameObject.SetActive(false);
            var legacyLine = transferPage == null ? null : transferPage.Find("Line");
            if (legacyLine != null) legacyLine.gameObject.SetActive(false);
            _vibe = null;
            _tableContent = root.Find("Pages/Standings/Grid") as RectTransform;
            _play = SceneBind.Wire(root.Find("PlayMatch"), OnContinue);
            BroadcastUi.Place(_play.GetComponent<RectTransform>(), 0.16f, 0.01f, 0.84f, 0.07f);
            _playLabel = _play.GetComponentInChildren<Text>();
            var onlineButton = root.Find("Online");
            if (onlineButton != null)
                onlineButton.gameObject.SetActive(false);
            _modal = root.Find("Modal").gameObject;
            _modalTitle = SceneBind.Get<Text>(root, "Modal/Card/Title");
            _modalLead = SceneBind.Get<Text>(root, "Modal/Card/Lead");
            _modalBody = root.Find("Modal/Card/Scroll/Content") as RectTransform;
            _modalAction = SceneBind.Get<Button>(root, "Modal/Card/Action");
            _modalActionLabel = _modalAction.GetComponentInChildren<Text>();
            var closeButton = root.Find("Modal/Card/Close");
            if (closeButton != null) closeButton.gameObject.SetActive(false);
            _modalClose = EnsureCloseButton(root.Find("Modal/Card"), "ModalClose", CloseModal);
            SceneBind.Wire(root.Find("Modal/Dim"), CloseModal);
            _modal.SetActive(false);
        }

        void RefreshCareerTabLabels()
        {
            if (_tabs == null)
                return;
            var english = LocalizationManager.Current == GameLanguage.English;
            if (_pages != null && _pages.Length > 2)
            {
                var boardTitle = _pages[2].transform.Find("Title")?.GetComponent<Text>();
                if (boardTitle != null) boardTitle.text = english ? "ACADEMY" : "ACADEMIA";
            }
            if (_pages != null && _pages.Length > 5)
            {
                var transfersTitle = _pages[5].transform.Find("TransfersTitle")?.GetComponent<Text>();
                if (transfersTitle != null) transfersTitle.text = english ? "TRANSFERS" : "TRANSFERÊNCIAS";
            }
            var officeTitle = _pages != null && _pages.Length > 6
                ? _pages[6].transform.Find("OfficeTitle")?.GetComponent<Text>() : null;
            if (officeTitle != null) officeTitle.text = english ? "OFFICE" : "ESCRITÓRIO";
            var trainingTitle = _pages != null && _pages.Length > 4 ? _pages[4].transform.Find("Title")?.GetComponent<Text>() : null;
            if (trainingTitle != null) trainingTitle.text = english ? "TRAINING" : "TREINO";
            var academyIntro = _pages != null && _pages.Length > 2
                ? _pages[2].transform.Find("Intro")?.GetComponent<Text>() : null;
            if (academyIntro != null) academyIntro.text = english
                ? "Youth prospects · players can be promoted from age 16"
                : "Jovens promessas · promoção possível a partir dos 16 anos";
            if (_academyViewTabs != null)
            {
                var academyViews = english ? new[] { "TACTICS", "SQUAD", "FIXTURES" } : new[] { "TÁTICAS", "PLANTEL", "JOGOS" };
                for (var i = 0; i < _academyViewTabs.Length && i < academyViews.Length; i++)
                {
                    var label = _academyViewTabs[i].GetComponentInChildren<Text>();
                    if (label != null) label.text = academyViews[i];
                }
            }
            var careerPage = _officeViews != null && _officeViews.Length > 1 ? _officeViews[1].transform.Find("Board") : null;
            if (careerPage != null)
            {
                var title = careerPage.Find("Title")?.GetComponent<Text>();
                if (title != null) title.text = english ? "MY CAREER" : "A MINHA CARREIRA";
                var hint = careerPage.Find("Hint")?.GetComponent<Text>();
                if (hint != null) hint.text = english ? "Write a personal note about what happened. This is not the news feed." : "Escreve uma nota pessoal sobre o que aconteceu. Isto não é o jornal.";
                var placeholder = careerPage.Find("Input/Placeholder")?.GetComponent<Text>();
                if (placeholder != null) placeholder.text = english ? "What stayed with you from this moment?" : "O que ficou contigo deste momento?";
                var save = careerPage.Find("SaveNote")?.GetComponentInChildren<Text>();
                if (save != null) save.text = english ? "SAVE NOTE" : "GUARDAR NOTA";
            }
            var staffTitle = _officeViews != null && _officeViews.Length > 3
                ? _officeViews[3].transform.Find("StaffTitle")?.GetComponent<Text>() : null;
            if (staffTitle != null) staffTitle.text = english ? "CLUB STAFF" : "EQUIPA TÉCNICA DO CLUBE";
            var calendarTitle = _officeViews != null && _officeViews.Length > 2
                ? _officeViews[2].transform.Find("Calendar/Title")?.GetComponent<Text>() : null;
            if (calendarTitle != null) calendarTitle.text = english ? "CALENDAR  ·  assistant notes for each match" : "CALENDÁRIO  ·  nota do adjunto em cada jogo";
            var tripLabel = _officeViews != null && _officeViews.Length > 2
                ? _officeViews[2].transform.Find("Calendar/Trip")?.GetComponentInChildren<Text>() : null;
            if (tripLabel != null) tripLabel.text = english ? "PLAN TRAVEL" : "MARCAR VIAGEM";
            RefreshTrainingModeTabs();
            if (_zoneButtons != null)
                for (var i = 0; i < _zoneButtons.Length; i++)
                {
                    var label = _zoneButtons[i] == null ? null : _zoneButtons[i].GetComponentInChildren<Text>();
                    if (label != null) label.text = english ? (i == 0 ? "TACTICS" : "SQUAD") : (i == 0 ? "TÁTICAS" : "PLANTEL");
                }
            var sources = LocalizationManager.Current == GameLanguage.English
                ? new[] { "HOME", "NEWS", "ACADEMY", "TEAM", "TRAINING", "TRANSFERS", "OFFICE", "STANDINGS" }
                : new[] { "INÍCIO", "JORNAIS", "ACADEMIA", "EQUIPA", "TREINO", "TRANSFERÊNCIAS", "ESCRITÓRIO", "TABELA" };
            for (var i = 0; i < _tabs.Length && i < sources.Length; i++)
            {
                if (_tabs[i] == null)
                    continue;
                var label = _tabs[i].GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = sources[i];
                    label.resizeTextForBestFit = true;
                    label.resizeTextMinSize = 10;
                    label.resizeTextMaxSize = 16;
                }
            }

            var officeLabels = english
                ? new[] { "BUDGET", "MY CAREER", "BOARD", "CALENDAR", "STAFF" }
                : new[] { "ORÇAMENTO", "A MINHA CARREIRA", "QUADRO", "CALENDÁRIO", "EQUIPA TÉCNICA" };
            for (var i = 0; _officeTabs != null && i < _officeTabs.Length && i < officeLabels.Length; i++)
            {
                var label = _officeTabs[i] == null ? null : _officeTabs[i].GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = officeLabels[i];
                    label.resizeTextForBestFit = true;
                    label.resizeTextMinSize = 10;
                    label.resizeTextMaxSize = 14;
                }
            }
        }

        RectTransform PersonCard(Transform parent, Player player, Club club, bool showFatigue, int fieldIndex, bool bench,
            float cardWidth, float cardHeight, PlayerPosition? displayPosition = null, string dragContext = null,
            Action clickOverride = null, Color? background = null, bool? benchReserves = null)
        {
            var position = displayPosition ?? CardPosition(player, club, fieldIndex);
            float? fatigue = null;
            if (showFatigue)
            {
                var coach = StaffReport.Find(_career.Club, StaffRole.FitnessCoach);
                var fatigueTruth = bench ? Mathf.Min(player.Fatigue, 22) : player.Fatigue;
                fatigue = StaffReport.Read(coach, player.Id, "cansaco", fatigueTruth, 0, 100);
            }

            var card = PlayerCardView.Create(parent, "P" + player.SquadNumber, player, club, position,
                showFatigue, "", background, fatigue);
            var rect = card.rectTransform;
            rect.sizeDelta = new Vector2(cardWidth, cardHeight);
            BroadcastUi.Hit(card, clickOverride ?? (() => OnPerson(player, bench)));
            if (bench || fieldIndex >= 0)
            {
                var drag = card.gameObject.AddComponent<SquadDragCard>();
                drag.Player = player;
                drag.Bench = bench;
                drag.BenchReserves = benchReserves ?? _reserves;
                drag.FieldIndex = fieldIndex;
                drag.Context = dragContext ?? "";
                if (bench)
                    drag.OnSwapWithField = SwapStarter;
            }
            return rect;
        }

        static PlayerPosition CardPosition(Player player, Club club, int fieldIndex)
        {
            if (player == null || club == null || fieldIndex < 0)
                return player == null ? PlayerPosition.CentralMidfielder : player.Position;
            var slots = StartingShape.SlotPositions(club.Tactics == null ? "4-3-3" : club.Tactics.Formation);
            return fieldIndex < slots.Length ? slots[fieldIndex] : player.Position;
        }

        void FillCalendar()
        {
            Clear(_calendar);
            var english = LocalizationManager.Current == GameLanguage.English;
            var weeklyPlan = BroadcastUi.Panel(_calendar, "WeeklyPlan", new Color(0.035f, 0.075f, 0.1f, 0.96f), true);
            BroadcastUi.Preferred(weeklyPlan, 82);
            var planTitle = BroadcastUi.Label(weeklyPlan.transform, "PlanTitle", english ? "WEEKLY PLAN" : "PLANO SEMANAL", 11, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(planTitle.rectTransform, 0.02f, 0.72f, 0.24f, 0.98f);
            var tuesday = NextWeekday(_career.Calendar.CurrentDate, DayOfWeek.Tuesday);
            var wednesday = NextWeekday(_career.Calendar.CurrentDate, DayOfWeek.Wednesday);
            var trainButton = BroadcastUi.Button(weeklyPlan.transform, "TuesdayTraining",
                (english ? "TUESDAY  ·  TEAM TRAINING  ·  " : "TERÇA  ·  TREINO DE EQUIPA  ·  ") + LocalizationManager.FormatDate(tuesday, "dd MMM"),
                () => Show(4), new Color(1f, 1f, 1f, 0.07f), Color.white, 11);
            BroadcastUi.Place(trainButton.GetComponent<RectTransform>(), 0.02f, 0.08f, 0.49f, 0.7f);
            var inspectButton = BroadcastUi.Button(weeklyPlan.transform, "WednesdayInspection",
                (english ? "WEDNESDAY  ·  INSPECT A CLUB  ·  " : "QUARTA  ·  OBSERVAR UM CLUBE  ·  ") + LocalizationManager.FormatDate(wednesday, "dd MMM"),
                OpenNextClubInspection, new Color(1f, 1f, 1f, 0.07f), Color.white, 11);
            BroadcastUi.Place(inspectButton.GetComponent<RectTransform>(), 0.51f, 0.08f, 0.98f, 0.7f);
        }

        void OpenCalendarPopup()
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            OpenModal(english ? "CALENDAR" : "CALENDÁRIO", "", false);
            SizeModal(0.06f, 0.04f, 0.94f, 0.96f);
            var card = _modal == null ? null : _modal.transform.Find("Card");
            if (_modalLead != null)
                _modalLead.gameObject.SetActive(false);
            var scroll = card == null ? null : card.Find("Scroll");
            if (scroll != null)
                scroll.gameObject.SetActive(false);
            if (_modalAction != null)
                _modalAction.gameObject.SetActive(false);
            if (card != null)
                BroadcastUi.Place(_modalTitle.rectTransform, 0.16f, 0.935f, 0.84f, 0.99f);
            PaintMonthCalendar(english);
            if (_modalClose != null)
                _modalClose.transform.SetAsLastSibling();
        }

        void PaintMonthCalendar(bool english)
        {
            var card = _modal == null ? null : _modal.transform.Find("Card");
            var host = card != null ? card : (_modalBody != null ? _modalBody : (Transform)_calendar);
            if (host == null)
                return;
            var previousBoard = host.Find("MonthBoard");
            if (previousBoard != null)
                Destroy(previousBoard.gameObject);
            if (_calendarMonth.Year < 2000)
                _calendarMonth = new DateTime(_career.Calendar.CurrentDate.Year, _career.Calendar.CurrentDate.Month, 1);
            var monthStart = new DateTime(_calendarMonth.Year, _calendarMonth.Month, 1);
            var culture = CultureInfo.GetCultureInfo(english ? "en-GB" : "pt-PT");
            var board = BroadcastUi.Panel(host, "MonthBoard", new Color(0.05f, 0.03f, 0.12f, 0.96f), false);
            BroadcastUi.Place(board.rectTransform, 0.02f, 0.015f, 0.98f, 0.88f);
            var title = BroadcastUi.Label(board.transform, "Month", monthStart.ToString("MMMM yyyy", culture).ToUpperInvariant(), 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.22f, 0.93f, 0.78f, 0.995f);
            var previous = BroadcastUi.Button(board.transform, "PrevMonth", "‹", () =>
            {
                _calendarMonth = monthStart.AddMonths(-1);
                OpenCalendarPopup();
            }, new Color(1f, 1f, 1f, 0.08f), Color.white, 22);
            BroadcastUi.Place(previous.GetComponent<RectTransform>(), 0.02f, 0.93f, 0.1f, 0.995f);
            var next = BroadcastUi.Button(board.transform, "NextMonth", "›", () =>
            {
                _calendarMonth = monthStart.AddMonths(1);
                OpenCalendarPopup();
            }, new Color(1f, 1f, 1f, 0.08f), Color.white, 22);
            BroadcastUi.Place(next.GetComponent<RectTransform>(), 0.9f, 0.93f, 0.98f, 0.995f);

            var weekdays = english
                ? new[] { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" }
                : new[] { "SEG", "TER", "QUA", "QUI", "SEX", "SÁB", "DOM" };
            for (var d = 0; d < 7; d++)
            {
                var x0 = 0.015f + d * 0.139f;
                var head = BroadcastUi.Label(board.transform, "Weekday" + d, weekdays[d], 13, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(head.rectTransform, x0, 0.875f, x0 + 0.132f, 0.92f);
            }

            var byDay = new Dictionary<DateTime, List<Fixture>>();
            foreach (var fixture in _career.Fixtures)
            {
                if (fixture == null || fixture.Walkover || _career.Coach == null || !fixture.Involves(_career.Coach.ClubId))
                    continue;
                var day = fixture.Date.Date;
                if (!byDay.TryGetValue(day, out var list))
                {
                    list = new List<Fixture>();
                    byDay[day] = list;
                }
                list.Add(fixture);
            }

            var lead = ((int)monthStart.DayOfWeek + 6) % 7;
            var gridStart = monthStart.AddDays(-lead);
            var today = _career.Calendar.CurrentDate.Date;
            for (var i = 0; i < 42; i++)
            {
                var day = gridStart.AddDays(i);
                var col = i % 7;
                var row = i / 7;
                var x0 = 0.015f + col * 0.139f;
                var y1 = 0.86f - row * 0.128f;
                var inMonth = day.Month == monthStart.Month;
                var window = day.Month == 1 || day.Month == 7 || day.Month == 8;
                var tone = !inMonth ? new Color(0.08f, 0.05f, 0.14f, 0.55f)
                    : day == today ? new Color(0.22f, 0.12f, 0.38f, 1f)
                    : window ? new Color(0.28f, 0.2f, 0.08f, 0.96f)
                    : new Color(0.12f, 0.06f, 0.22f, 0.96f);
                byDay.TryGetValue(day, out var matches);
                var fixture = matches != null && matches.Count > 0 ? matches[0] : null;
                var cell = BroadcastUi.Panel(board.transform, "Day" + i, tone, fixture != null);
                BroadcastUi.Place(cell.rectTransform, x0, y1 - 0.12f, x0 + 0.132f, y1);
                var number = BroadcastUi.Label(cell.transform, "Num", day.Day.ToString(), 14, inMonth ? Color.white : UiTheme.Muted, TextAnchor.UpperLeft, FontStyle.Bold);
                number.raycastTarget = false;
                BroadcastUi.Place(number.rectTransform, 0.06f, 0.62f, 0.42f, 0.96f);
                if (fixture != null && _career.World.Clubs.TryGetValue(fixture.OpponentId(_career.Coach.ClubId), out var opponent))
                {
                    var capturedOpponent = opponent;
                    BroadcastUi.Hit(cell, () => OpenClubDossier(capturedOpponent));
                    var badge = CrestImage(cell.transform, opponent, 28f);
                    badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(0.5f, 0.42f);
                    badge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    badge.rectTransform.anchoredPosition = Vector2.zero;
                    badge.rectTransform.sizeDelta = new Vector2(28f, 28f);
                    badge.raycastTarget = false;
                    var competitionSprite = CrestStudio.ForCompetition(fixture.LeagueId);
                    if (competitionSprite != null)
                    {
                        var competitionObject = new GameObject("CompetitionCrest", typeof(RectTransform), typeof(Image));
                        var competitionRect = competitionObject.GetComponent<RectTransform>();
                        competitionRect.SetParent(cell.transform, false);
                        competitionRect.anchorMin = competitionRect.anchorMax = new Vector2(0.84f, 0.82f);
                        competitionRect.pivot = new Vector2(0.5f, 0.5f);
                        competitionRect.anchoredPosition = Vector2.zero;
                        competitionRect.sizeDelta = new Vector2(19f, 19f);
                        var competitionImage = competitionObject.GetComponent<Image>();
                        competitionImage.sprite = competitionSprite;
                        competitionImage.preserveAspect = true;
                        competitionImage.raycastTarget = false;
                    }
                    if (fixture.Played)
                    {
                        var userHome = fixture.IsHome(_career.Coach.ClubId);
                        var userGoals = userHome ? fixture.HomeGoals : fixture.AwayGoals;
                        var oppGoals = userHome ? fixture.AwayGoals : fixture.HomeGoals;
                        string mark;
                        Color markColor;
                        if (userGoals > oppGoals)
                        {
                            mark = english ? "W" : "V";
                            markColor = new Color(0.25f, 0.78f, 0.38f);
                        }
                        else if (userGoals < oppGoals)
                        {
                            mark = english ? "L" : "D";
                            markColor = new Color(0.9f, 0.22f, 0.22f);
                        }
                        else
                        {
                            mark = english ? "D" : "E";
                            markColor = new Color(0.95f, 0.78f, 0.22f);
                        }
                        var verdict = BroadcastUi.Label(cell.transform, "Result", mark, 18, markColor, TextAnchor.MiddleCenter, FontStyle.Bold);
                        verdict.raycastTarget = false;
                        BroadcastUi.Place(verdict.rectTransform, 0.62f, 0.02f, 0.96f, 0.38f);
                    }
                    else
                    {
                        var details = BroadcastUi.Label(cell.transform, "FixtureDetails", FixtureKickoff(fixture) + "\n" + FixtureCompetition(fixture),
                            8, Color.white, TextAnchor.MiddleCenter);
                        details.resizeTextForBestFit = true;
                        details.resizeTextMinSize = 6;
                        details.resizeTextMaxSize = 8;
                        details.raycastTarget = false;
                        BroadcastUi.Place(details.rectTransform, 0.04f, 0.02f, 0.96f, 0.25f);
                    }
                }
                else if (inMonth && ((day.Month == 1 && (day.Day == 1 || day.Day == 31)) || (day.Month == 7 && day.Day == 1) || (day.Month == 8 && day.Day == 31)))
                {
                    var opens = day.Day == 1;
                    var tag = BroadcastUi.Label(cell.transform, "Window", opens ? (english ? "OPEN" : "ABRE") : (english ? "CLOSE" : "FECHA"), 11, new Color(0.95f, 0.78f, 0.28f), TextAnchor.MiddleCenter, FontStyle.Bold);
                    tag.raycastTarget = false;
                    BroadcastUi.Place(tag.rectTransform, 0.08f, 0.18f, 0.92f, 0.55f);
                }
            }

            var legend = BroadcastUi.Label(board.transform, "Legend", english
                ? "W win    D draw    L loss     gold days: transfer window"
                : "V vitória    E empate    D derrota     dias dourados: janela de transferências",
                13, UiTheme.Muted, TextAnchor.MiddleCenter);
            BroadcastUi.Place(legend.rectTransform, 0.04f, 0.002f, 0.96f, 0.045f);
        }

        bool NoticeTransferWindow()
        {
            if (_career == null || _career.Calendar == null)
                return false;
            var day = _career.Calendar.CurrentDate.Date;
            var opens = (day.Month == 1 && day.Day == 1) || (day.Month == 7 && day.Day == 1);
            var closes = (day.Month == 1 && day.Day == 31) || (day.Month == 8 && day.Day == 31);
            if (!opens && !closes)
                return false;
            var key = "window-note|" + day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            foreach (var line in _career.Notebook)
                if (line == key)
                    return false;
            _career.Notebook.Add(key);
            var english = LocalizationManager.Current == GameLanguage.English;
            if (opens)
            {
                PostDesk(english
                    ? "✉ The transfer window is open. Signings can be agreed until " + (day.Month == 1 ? "31 January" : "31 August") + "."
                    : "✉ A janela de transferências está aberta. Dá para fechar contratações até " + (day.Month == 1 ? "31 de janeiro" : "31 de agosto") + ".");
                Newsroom.Publish(_career, english ? "The Football Wire" : "Diário da Liga",
                    english ? "Transfer window opens" : "Abre a janela de transferências",
                    english
                        ? "Clubs can agree deals from today. The window stays open through " + (day.Month == 1 ? "January" : "July and August") + "."
                        : "Os clubes podem fechar negócios a partir de hoje. A janela fica aberta " + (day.Month == 1 ? "durante janeiro" : "em julho e agosto") + ".");
            }
            else
            {
                PostDesk(english
                    ? "✉ The transfer window closes today. After midnight, new signings wait for the next window."
                    : "✉ A janela de transferências fecha hoje. Depois da meia-noite, novas contratações esperam pela próxima janela.");
                Newsroom.Publish(_career, english ? "The Football Wire" : "Diário da Liga",
                    english ? "Transfer window closes tonight" : "A janela de transferências fecha hoje",
                    english
                        ? "Deals agreed today can still be completed. Tomorrow the market shuts until the next window."
                        : "Os acordos de hoje ainda podem ser concluídos. Amanhã o mercado fecha até à próxima janela.");
            }
            return true;
        }

        static DateTime NextWeekday(DateTime date, DayOfWeek day)
        {
            var offset = ((int)day - (int)date.DayOfWeek + 7) % 7;
            return date.Date.AddDays(offset);
        }

        void OpenNextClubInspection()
        {
            var fixture = _career == null ? null : _career.NextUserFixture();
            if (fixture != null)
            {
                OpenFixture(fixture);
                return;
            }

            if (_career?.World == null || _career.Club == null)
                return;
            Club target = null;
            foreach (var club in _career.World.Clubs.Values)
                if (club.Id != _career.Club.Id && club.LeagueId == _career.Club.LeagueId
                    && (target == null || club.Reputation > target.Reputation))
                    target = club;
            if (target != null)
                OpenClubDossier(target);
        }

        void OpenStory(PaperStory story)
        {
            OpenModal(PaperName(story.Paper), PressDate(string.IsNullOrEmpty(story.Published) ? story.Deck : story.Published), false);
            Club home = null;
            Club away = null;
            if (!string.IsNullOrEmpty(story.HomeClubId) && _career.World.Clubs.ContainsKey(story.HomeClubId))
                home = _career.World.Clubs[story.HomeClubId];
            if (!string.IsNullOrEmpty(story.AwayClubId) && _career.World.Clubs.ContainsKey(story.AwayClubId))
                away = _career.World.Clubs[story.AwayClubId];
            if (home != null || away != null)
            {
                var band = BroadcastUi.Panel(_modalBody, "Clubs", new Color(0.05f, 0.02f, 0.12f, 0.95f), true);
                BroadcastUi.Preferred(band, 88);
                if (home != null)
                {
                    var left = CrestImage(band.transform, home, 52f);
                    left.rectTransform.anchorMin = left.rectTransform.anchorMax = new Vector2(0.12f, 0.5f);
                    left.rectTransform.sizeDelta = new Vector2(52, 52);
                    var leftName = BroadcastUi.Label(band.transform, "Home", home.ShortName, 16, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                    BroadcastUi.Place(leftName.rectTransform, 0.2f, 0.2f, 0.42f, 0.8f);
                }

                if (away != null)
                {
                    var right = CrestImage(band.transform, away, 52f);
                    right.rectTransform.anchorMin = right.rectTransform.anchorMax = new Vector2(0.88f, 0.5f);
                    right.rectTransform.sizeDelta = new Vector2(52, 52);
                    var rightName = BroadcastUi.Label(band.transform, "Away", away.ShortName, 16, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
                    BroadcastUi.Place(rightName.rectTransform, 0.58f, 0.2f, 0.8f, 0.8f);
                }
            }

            var headline = PressHeadline(story);
            var highlight = BroadcastUi.Label(_modalBody, "Headline", headline, 22, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            BroadcastUi.SetWrapped(highlight, headline, 48);
            if (story.Kicker == "JOGO" && !string.IsNullOrEmpty(story.Motm))
            {
                var motm = BroadcastUi.Label(_modalBody, "Motm", LocalizationManager.Translate("JOGADOR DA PARTIDA") + "   " + story.Motm, 16, UiTheme.Score, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Preferred(motm, 28);
            }

            if (!string.IsNullOrEmpty(story.Quote))
            {
                var quote = BroadcastUi.Panel(_modalBody, "Quote", new Color(0.14f, 0.06f, 0.28f, 0.95f), true);
                BroadcastUi.Preferred(quote, 88);
                var line = BroadcastUi.Label(quote.transform, "Q", "“" + PressQuote(story) + "”", 16, Color.white, TextAnchor.UpperLeft);
                BroadcastUi.Place(line.rectTransform, 0.05f, 0.32f, 0.95f, 0.92f);
                var by = BroadcastUi.Label(quote.transform, "By", "— " + story.QuoteBy, 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(by.rectTransform, 0.05f, 0.06f, 0.95f, 0.32f);
            }

            if (home != null)
            {
                var clubButton = BroadcastUi.Button(_modalBody, "HomeClubProfile", LocalizationManager.Current == GameLanguage.English ? "VIEW " + home.ShortName : "VER " + home.ShortName,
                    () => OpenNewsClubDossier(home, story), new Color(0.16f, 0.08f, 0.29f, 1f), Color.white, 13);
                BroadcastUi.Preferred(clubButton.GetComponent<RectTransform>(), 38);
            }
            if (away != null)
            {
                var clubButton = BroadcastUi.Button(_modalBody, "AwayClubProfile", LocalizationManager.Current == GameLanguage.English ? "VIEW " + away.ShortName : "VER " + away.ShortName,
                    () => OpenNewsClubDossier(away, story), new Color(0.16f, 0.08f, 0.29f, 1f), Color.white, 13);
                BroadcastUi.Preferred(clubButton.GetComponent<RectTransform>(), 38);
            }

            var pressBody = PressBody(story);
            var body = BroadcastUi.Label(_modalBody, "Body", pressBody, 17, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.SetWrapped(body, pressBody, 160);
            SizeModal(0.24f, 0.2f, 0.76f, 0.8f);
        }

        void OpenFixture(Fixture fixture)
        {
            var opp = _career.World.Clubs[fixture.OpponentId(_career.Coach.ClubId)];
            var formation = opp.Tactics == null ? "4-3-3" : opp.Tactics.Formation;
            var venue = LocalizationManager.Translate(fixture.IsHome(_career.Coach.ClubId) ? "Casa" : "Fora");
            var scouted = _career.Scouted.Contains(opp.Id);
            OpenModal(opp.Name, venue + "  ·  " + LocalizationManager.FormatDate(fixture.Date, "dd MMMM") + "  ·  "
                + FixtureKickoff(fixture) + "  ·  " + FixtureCompetition(fixture) + "  ·  " + formation, scouted == false);
            SizeModal(0.22f, 0.28f, 0.78f, 0.74f);
            BroadcastUi.Place(_modalTitle.rectTransform, 0.06f, 0.88f, 0.84f, 0.97f);
            var crest = CrestImage(_modal.transform.Find("Card"), opp, 56f);
            crest.rectTransform.anchorMin = crest.rectTransform.anchorMax = new Vector2(0.93f, 0.92f);
            crest.rectTransform.anchoredPosition = Vector2.zero;
            var clubCardButton = BroadcastUi.Button(_modalBody, "ClubProfile", LocalizationManager.Current == GameLanguage.English ? "OPEN CLUB PROFILE" : "ABRIR PERFIL DO CLUBE",
                () => OpenClubDossier(opp), new Color(0.16f, 0.08f, 0.29f, 1f), Color.white, 13);
            BroadcastUi.Preferred(clubCardButton.GetComponent<RectTransform>(), 36);
            AddLine(AssistantNote(opp));
            if (scouted)
            {
                AddLine(LocalizationManager.Current == GameLanguage.English
                    ? "The scout report is ready. Open the club profile for the full read."
                    : "O relatório do olheiro já está pronto. A ficha do clube tem a leitura completa.");
                return;
            }
            var days = FreeAnalysisDays();
            _modalAction.onClick.RemoveAllListeners();
            _modalActionLabel.text = days > 0
                ? LocalizationManager.Translate("ANALISAR O PLANTEL  ·  " + days + " DIAS")
                : LocalizationManager.Translate("SEM TEMPO ANTES DO PRÓXIMO JOGO");
            _modalAction.interactable = days > 0;
            _modalAction.onClick.AddListener(() => StartScout(opp, fixture));
            BroadcastUi.Place(_modalAction.GetComponent<RectTransform>(), 0.14f, 0.06f, 0.86f, 0.2f);
        }

        void OpenClubDossier(Club club)
        {
            OpenClubDossier(club, 0, null, null);
        }

        void OpenClubDossier(Club club, int selectedTab)
        {
            OpenClubDossier(club, selectedTab, null, null);
        }

        void OpenNewsClubDossier(Club club, PaperStory story)
        {
            if (story == null) return;
            OpenClubDossier(club, 0, null, () => OpenStory(story));
        }

        void OpenClubDossier(Club club, int selectedTab, string youthGroup, Action returnTo)
        {
            if (club == null || _career == null) return;
            CompleteScouts();
            var english = LocalizationManager.Current == GameLanguage.English;
            var city = LocalizationManager.Translate(club.City);
            var league = "";
            foreach (var item in _career.World.Leagues)
                if (item.Id == club.LeagueId) { league = LocalizationManager.Translate(item.Name); break; }
            var lead = (string.IsNullOrEmpty(league) ? city : league + "  ·  " + city) + "  ·  " + (english ? "Founded " : "Fundado em ") + club.FoundedYear;
            var manager = ManagerOf(club);
            if (!string.IsNullOrEmpty(manager))
                lead += (english ? "  ·  Manager " : "  ·  Treinador ") + manager;
            OpenModal(club.Name, lead, false);
            _dossierClub = club;
            _dossierTab = selectedTab;
            _dossierYouth = youthGroup;
            _dossierBack = returnTo;
            _dossierReturnAction = returnTo;
            var crest = CrestImage(_modal.transform.Find("Card"), club, 48f);
            crest.rectTransform.anchorMin = crest.rectTransform.anchorMax = new Vector2(0.05f, 0.925f);
            BroadcastUi.Place(_modalTitle.rectTransform, 0.09f, 0.88f, 0.91f, 0.97f);
            if (!string.IsNullOrEmpty(manager))
            {
                _modalLead.horizontalOverflow = HorizontalWrapMode.Wrap;
                _modalLead.verticalOverflow = VerticalWrapMode.Overflow;
                BroadcastUi.Place(_modalLead.rectTransform, 0.05f, 0.74f, 0.95f, 0.88f);
                var sheetScroll = _modal.transform.Find("Card/Scroll") as RectTransform;
                if (sheetScroll != null)
                    BroadcastUi.Place(sheetScroll, 0.04f, 0.08f, 0.96f, 0.73f);
            }

            var labels = english
                ? new[] { "OVERVIEW", "SQUAD", "MATCHES", "PERFORMANCE", "TRANSFERS", "HISTORY" }
                : new[] { "VISÃO GERAL", "PLANTEL", "JOGOS", "DESEMPENHO", "TRANSFERÊNCIAS", "HISTÓRIA" };
            var tabRow = BroadcastUi.Panel(_modalBody, "ClubProfileTabs", new Color(0f, 0f, 0f, 0f), false);
            var tabRowImage = tabRow.GetComponent<Image>();
            if (tabRowImage != null)
            {
                tabRowImage.raycastTarget = false;
                tabRowImage.color = Color.clear;
            }
            BroadcastUi.Preferred(tabRow, 38);
            for (var i = 0; i < labels.Length; i++)
            {
                var tab = i;
                var x0 = i / (float)labels.Length;
                var x1 = (i + 1) / (float)labels.Length;
                var button = BroadcastUi.Button(tabRow.transform, "ClubTab" + i, labels[i],
                    () => OpenClubDossier(club, tab, youthGroup, returnTo), tab == selectedTab ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.07f),
                    tab == selectedTab ? UiTheme.ButtonText : Color.white, 9);
                BroadcastUi.Place(button.GetComponent<RectTransform>(), x0 + 0.003f, 0.08f, x1 - 0.003f, 0.92f);
            }

            switch (Mathf.Clamp(selectedTab, 0, labels.Length - 1))
            {
                case 0: BuildClubOverview(club, city, league, english); break;
                case 1:
                    if (string.IsNullOrEmpty(youthGroup)) BuildClubSquad(club, english);
                    else BuildYouthClubSquad(club, youthGroup, english);
                    break;
                case 2: BuildClubMatches(club, english); break;
                case 3: BuildClubPerformance(club, english); break;
                case 4: BuildClubTransfers(club, english); break;
                case 5: BuildClubHistory(club, city, league, english); break;
            }
            FitClubSheet();
        }

        void BuildClubOverview(Club club, string city, string league, bool english)
        {
            var roster = Squad(club);
            roster.Sort((a, b) => PlayerAttributes.Overall(b).CompareTo(PlayerAttributes.Overall(a)));
            var overview = BroadcastUi.Panel(_modalBody, "ClubOverviewGrid", new Color(0.035f, 0.075f, 0.1f, 0.96f), false);
            BroadcastUi.Preferred(overview, 320f);
            var manager = ManagerOf(club);
            var summary = (string.IsNullOrEmpty(manager) ? "" : (english ? "Manager  " : "Treinador  ") + manager + "\n")
                + (english ? "Reputation  " : "Reputação  ") + ReputationStars(club.Reputation)
                + "   ·   " + (english ? "First-team players  " : "Jogadores seniores  ") + roster.Count
                + FinanceLine(club, english);
            OverviewBlock(overview.transform, "Summary", english ? "CLUB OVERVIEW" : "VISÃO GERAL DO CLUBE", summary, 0.025f, 0.71f, 0.64f, 0.97f);

            SeasonSchedule.RebuildTable(_career);
            var leagueRows = new List<LeagueTableRow>();
            foreach (var row in _career.Table)
                if (row.LeagueId == club.LeagueId) leagueRows.Add(row);
            leagueRows.Sort((a, b) => b.Points != a.Points ? b.Points.CompareTo(a.Points) : b.GoalDifference.CompareTo(a.GoalDifference));
            var place = leagueRows.FindIndex(row => row.ClubId == club.Id) + 1;
            var table = leagueRows.Find(row => row.ClubId == club.Id);
            var leagueText = table == null
                ? (english ? "League results are not available yet." : "A classificação da liga ainda não está disponível.")
                : league + "  ·  " + (english ? "Position " : "Lugar ") + place + "/" + leagueRows.Count
                    + "   ·   " + (english ? "Played " : "Jogos ") + table.Played + "   W " + table.Won + "   D " + table.Drawn + "   L " + table.Lost
                    + "   ·   " + (english ? "Points " : "Pontos ") + table.Points;
            var recent = new List<Fixture>();
            foreach (var fixture in _career.Fixtures)
                if (fixture.Played && fixture.Involves(club.Id)) recent.Add(fixture);
            recent.Sort((a, b) => b.Date.CompareTo(a.Date));
            for (var i = 0; i < Mathf.Min(2, recent.Count); i++)
            {
                var fixture = recent[i];
                var opponentId = fixture.OpponentId(club.Id);
                var opponent = _career.World.Clubs.TryGetValue(opponentId, out var other) ? other.ShortName : opponentId;
                leagueText += "\n" + LocalizationManager.FormatDate(fixture.Date, "dd MMM") + "  ·  " + FixtureKickoff(fixture)
                    + "  ·  " + FixtureCompetition(fixture) + "  ·  " + opponent + "  ·  "
                    + (fixture.IsHome(club.Id) ? fixture.HomeGoals + "–" + fixture.AwayGoals : fixture.AwayGoals + "–" + fixture.HomeGoals);
            }
            OverviewBlock(overview.transform, "League", english ? "LEAGUE POSITION  ·  RECENT RESULTS" : "POSIÇÃO NA LIGA  ·  RESULTADOS RECENTES", leagueText, 0.025f, 0.48f, 0.64f, 0.68f);

            if (roster.Count > 0)
            {
                var bestPotential = roster[0];
                var topScorer = roster[0];
                var topScorerGoals = -1;
                foreach (var player in roster)
                {
                    if (player.Potential > bestPotential.Potential) bestPotential = player;
                    var goals = 0;
                    foreach (var line in _career.Season)
                        if (line.PlayerId == player.Id) goals += line.Goals;
                    if (goals > topScorerGoals) { topScorerGoals = goals; topScorer = player; }
                }
                var highlights = (english ? "Best player  " : "Melhor jogador  ") + roster[0].FullName + "  ·  " + PlayerAttributes.Overall(roster[0])
                    + "\n" + (english ? "Highest potential  " : "Maior potencial  ") + bestPotential.FullName + "  ·  " + bestPotential.Potential
                    + "\n" + (english ? "Top scorer  " : "Melhor marcador  ") + topScorer.FullName + "  ·  " + Mathf.Max(0, topScorerGoals);
                OverviewBlock(overview.transform, "Highlights", english ? "SQUAD HIGHLIGHTS" : "DESTAQUES DO PLANTEL", highlights, 0.025f, 0.22f, 0.64f, 0.45f);
            }
            var clubTactics = club.Tactics ?? new TacticalIdentity { Formation = "4-3-3" };
            var identity = LocalizationManager.Translate(clubTactics.Formation + " · " + clubTactics.Pressing + " · " + clubTactics.BuildUp
                + " · " + clubTactics.FullBacks + " · " + clubTactics.DefensiveLine);
            OverviewBlock(overview.transform, "Identity", english ? "TACTICS" : "TÁTICAS", identity, 0.025f, 0.035f, 0.32f, 0.19f);
            var expectations = club.BoardObjectives == null || club.BoardObjectives.Count == 0
                ? (english ? "The board expects steady progress." : "A direção espera uma evolução consistente.")
                : string.Join("\n", club.BoardObjectives.ConvertAll(item => "· " + LocalizationManager.Translate(item)));
            OverviewBlock(overview.transform, "Expectations", english ? "BOARD EXPECTATIONS" : "EXPECTATIVAS DA DIREÇÃO", expectations, 0.34f, 0.035f, 0.64f, 0.19f);

            var field = BroadcastUi.Panel(overview.transform, "FirstTeamShape", new Color(0.06f, 0.25f, 0.14f, 1f));
            BroadcastUi.Place(field.rectTransform, 0.67f, 0.035f, 0.98f, 0.97f);
            var pitch = BroadcastUi.Zone(field.transform, "VerticalHalfPitch", 0.04f, 0.02f, 0.96f, 0.98f);
            BroadcastUi.DrawVerticalHalfPitch(pitch, new Color(0.12f, 0.46f, 0.22f));
            PlaceVerticalShape(pitch, club, false, 66f, 62f);
        }

        static void OverviewBlock(Transform parent, string name, string title, string body, float x0, float y0, float x1, float y1)
        {
            var panel = BroadcastUi.Panel(parent, name, new Color(0.07f, 0.16f, 0.2f, 0.94f));
            BroadcastUi.Place(panel.rectTransform, x0, y0, x1, y1);
            var heading = BroadcastUi.Label(panel.transform, "Title", title, 12, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.04f, 0.68f, 0.96f, 0.98f);
            var details = BroadcastUi.Label(panel.transform, "Details", body, 12, Color.white, TextAnchor.UpperLeft);
            details.resizeTextForBestFit = true;
            details.resizeTextMinSize = 10;
            details.resizeTextMaxSize = 12;
            BroadcastUi.Place(details.rectTransform, 0.04f, 0.04f, 0.96f, 0.68f);
        }

        void BuildClubSquad(Club club, bool english)
        {
            var roster = Squad(club);
            roster.Sort((a, b) => PlayerAttributes.Overall(b).CompareTo(PlayerAttributes.Overall(a)));
            var list = BroadcastUi.Panel(_modalBody, "ClubSquadList", new Color(0.03f, 0.05f, 0.08f, 0.98f), true);
            BroadcastUi.Preferred(list, 420f);
            var header = BroadcastUi.Label(list.transform, "Head", english ? "PLAYER" : "JOGADOR", 11, UiTheme.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(header.rectTransform, 0.065f, 0.94f, 0.38f, 0.995f);
            var headPos = BroadcastUi.Label(list.transform, "HeadPos", "POS", 11, UiTheme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(headPos.rectTransform, 0.38f, 0.94f, 0.46f, 0.995f);
            var headAge = BroadcastUi.Label(list.transform, "HeadAge", english ? "AGE" : "IDADE", 11, UiTheme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(headAge.rectTransform, 0.46f, 0.94f, 0.53f, 0.995f);
            var headAbility = BroadcastUi.Label(list.transform, "HeadAbility", english ? "ABL" : "HAB", 11, UiTheme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(headAbility.rectTransform, 0.53f, 0.94f, 0.61f, 0.995f);
            var headPotential = BroadcastUi.Label(list.transform, "HeadPotential", "POT", 11, UiTheme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(headPotential.rectTransform, 0.61f, 0.94f, 0.69f, 0.995f);
            var headWage = BroadcastUi.Label(list.transform, "HeadWage", english ? "WAGE" : "SALÁRIO", 11, UiTheme.Muted, TextAnchor.MiddleRight, FontStyle.Bold);
            BroadcastUi.Place(headWage.rectTransform, 0.69f, 0.94f, 0.84f, 0.995f);
            var headContract = BroadcastUi.Label(list.transform, "HeadContract", english ? "EXPIRES" : "CONTRATO", 11, UiTheme.Muted, TextAnchor.MiddleRight, FontStyle.Bold);
            BroadcastUi.Place(headContract.rectTransform, 0.84f, 0.94f, 0.98f, 0.995f);
            var count = Mathf.Max(1, roster.Count);
            var top = 0.93f;
            var step = top / count;
            for (var i = 0; i < roster.Count; i++)
            {
                var player = roster[i];
                var y1 = top - i * step;
                var y0 = y1 - step + 0.003f;
                var tone = i % 2 == 0 ? new Color(1f, 1f, 1f, 0.04f) : new Color(1f, 1f, 1f, 0.015f);
                var row = BroadcastUi.Panel(list.transform, "Squad" + i, tone, true);
                BroadcastUi.Place(row.rectTransform, 0.008f, y0, 0.992f, y1);
                var captured = player;
                BroadcastUi.Hit(row, () => OpenPlayerReturning(captured, ReopenClubSheet));
                PassClicksTo(row.transform);
                var faceGo = new GameObject("Face", typeof(RectTransform), typeof(Image));
                faceGo.transform.SetParent(row.transform, false);
                BroadcastUi.Place(faceGo.GetComponent<RectTransform>(), 0.012f, 0.1f, 0.055f, 0.9f);
                var face = faceGo.GetComponent<Image>();
                face.sprite = FaceBook.For(player);
                face.preserveAspect = true;
                face.raycastTarget = false;
                var name = BroadcastUi.Label(row.transform, "Name", player.FullName, 13, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 9;
                name.resizeTextMaxSize = 13;
                BroadcastUi.Place(name.rectTransform, 0.065f, 0.08f, 0.38f, 0.92f);
                var pos = BroadcastUi.Label(row.transform, "Pos", ProfilePosition(player.Position, english), 12, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(pos.rectTransform, 0.38f, 0.08f, 0.46f, 0.92f);
                var age = BroadcastUi.Label(row.transform, "Age", player.Age.ToString(), 12, Color.white, TextAnchor.MiddleCenter);
                BroadcastUi.Place(age.rectTransform, 0.46f, 0.08f, 0.53f, 0.92f);
                var ability = BroadcastUi.Label(row.transform, "Ability", PlayerAttributes.Overall(player).ToString(), 13, UiTheme.Score, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(ability.rectTransform, 0.53f, 0.08f, 0.61f, 0.92f);
                var potential = BroadcastUi.Label(row.transform, "Potential", player.Potential.ToString(), 12, Color.white, TextAnchor.MiddleCenter);
                BroadcastUi.Place(potential.rectTransform, 0.61f, 0.08f, 0.69f, 0.92f);
                var wage = BroadcastUi.Label(row.transform, "Wage", Euro(player.WeeklyWage), 11, Color.white, TextAnchor.MiddleRight);
                BroadcastUi.Place(wage.rectTransform, 0.69f, 0.08f, 0.84f, 0.92f);
                var end = player.ContractEndTicks > 0 && player.ContractEndTicks <= DateTime.MaxValue.Ticks
                    ? new DateTime(player.ContractEndTicks).ToString("MM/yyyy") : "—";
                var contract = BroadcastUi.Label(row.transform, "Contract", end, 11, UiTheme.Muted, TextAnchor.MiddleRight);
                BroadcastUi.Place(contract.rectTransform, 0.84f, 0.08f, 0.98f, 0.92f);
            }
        }

        void BuildYouthClubSquad(Club club, string ageGroup, bool english)
        {
            var before = _career.AcademyPlayerIds.Count;
            AcademyFactory.Ensure(_career, club.Id);
            if (before != _career.AcademyPlayerIds.Count)
                CareerStore.Save(_career);
            var roster = AcademyPlayers(ageGroup, club.Id);
            var seniorFormation = club.Tactics == null ? "4-3-3" : club.Tactics.Formation;
            var formation = club.Id == _career.Club.Id ? AcademyTactics(ageGroup).Formation : seniorFormation;
            AddHead((english ? ageGroup + " YOUTH XI  ·  " : "ONZE " + ageGroup + "  ·  ")
                + formation);
            var eleven = new List<Player>();
            var bench = new List<Player>();
            StartingShape.Split(roster, formation, eleven, bench);
            AddDossierLineup(club, eleven, formation, english, "YouthClubPitch");
            var substitutes = bench.GetRange(0, Mathf.Min(7, bench.Count));
            AddDossierPlayerCards("YouthClubSubstitutes", english ? "SUBSTITUTES" : "SUPLENTES", substitutes, club, english);
            if (bench.Count > substitutes.Count)
                AddLine((english ? "Reserves  ·  " : "Reservas  ·  ") + (bench.Count - substitutes.Count));
        }

        void AddDossierLineup(Club club, List<Player> eleven, string formation, bool english, string objectName)
        {
            var field = BroadcastUi.Panel(_modalBody, objectName, new Color(0.06f, 0.25f, 0.14f, 1f));
            BroadcastUi.Preferred(field, 240f);
            var pitch = BroadcastUi.Zone(field.transform, "VerticalHalfPitch", 0.18f, 0.02f, 0.82f, 0.98f);
            BroadcastUi.DrawVerticalHalfPitch(pitch, new Color(0.12f, 0.46f, 0.22f));
            Canvas.ForceUpdateCanvases();
            var size = pitch.rect.size;
            var cardWidth = Mathf.Clamp(size.x * 0.14f, 64f, 96f);
            var cardHeight = Mathf.Clamp(size.y * 0.14f, 56f, 84f);
            var padX = cardWidth * 0.5f / Mathf.Max(1f, size.x) + 0.012f;
            var padY = cardHeight * 0.5f / Mathf.Max(1f, size.y) + 0.018f;
            var positions = StartingShape.SlotPositions(formation);
            for (var i = 0; i < eleven.Count && i < 11; i++)
            {
                var slot = MatchEngine.Slot(true, i, formation);
                slot.x = Mathf.Clamp(slot.x, padX, 1f - padX);
                slot.y = Mathf.Clamp(slot.y, padY, 1f - padY);
                var position = i < positions.Length ? positions[i] : eleven[i].Position;
                AddDossierPlayerCard(pitch, eleven[i], club, slot, cardWidth, cardHeight, english, displayPosition: position);
            }
        }

        void AddDossierPlayerCards(string objectName, string title, List<Player> players, Club club, bool english)
        {
            if (players == null || players.Count == 0) return;
            AddHead(title + "  ·  " + players.Count);
            var row = BroadcastUi.Panel(_modalBody, objectName, new Color(0.035f, 0.075f, 0.1f, 0.96f));
            BroadcastUi.Preferred(row, 88f);
            for (var i = 0; i < players.Count; i++)
            {
                var x0 = i / (float)players.Count;
                var x1 = (i + 1) / (float)players.Count;
                var player = players[i];
                AddDossierPlayerCard(row.transform, player, club, new Vector2((x0 + x1) * 0.5f, 0.5f),
                    (x1 - x0) * row.rectTransform.rect.width - 8f, 132f, english, x0 + 0.004f, x1 - 0.004f, 0.06f, 0.94f);
            }
        }

        void AddDossierPlayerCard(Transform host, Player player, Club club, Vector2 anchor, float width, float height,
            bool english, float x0 = -1f, float x1 = -1f, float y0 = -1f, float y1 = -1f, PlayerPosition? displayPosition = null)
        {
            var card = PersonCard(host, player, club, true, -1, false, width, height,
                displayPosition ?? player.Position, clickOverride: () => OpenPlayerReturning(player, ReopenClubSheet));
            if (x0 >= 0f)
            {
                card.anchorMin = new Vector2(x0, y0);
                card.anchorMax = new Vector2(x1, y1);
                card.sizeDelta = Vector2.zero;
                card.offsetMin = Vector2.zero;
                card.offsetMax = Vector2.zero;
            }
            else
            {
                card.anchorMin = card.anchorMax = anchor;
                card.sizeDelta = new Vector2(width, height);
            }
            card.anchoredPosition = Vector2.zero;
        }

        void BuildClubMatches(Club club, bool english)
        {
            AddClubRecentMatches(club, english, 6);
            var upcoming = new List<Fixture>();
            foreach (var fixture in _career.Fixtures)
                if (!fixture.Played && !fixture.Walkover && fixture.Date.Date >= _career.Calendar.CurrentDate.Date && fixture.Involves(club.Id))
                    upcoming.Add(fixture);
            upcoming.Sort((a, b) => a.Date.CompareTo(b.Date));
            AddHead(english ? "UPCOMING FIXTURES" : "PRÓXIMOS JOGOS");
            for (var i = 0; i < Mathf.Min(4, upcoming.Count); i++)
            {
                var fixture = upcoming[i];
                _career.World.Clubs.TryGetValue(fixture.HomeClubId, out var home);
                _career.World.Clubs.TryGetValue(fixture.AwayClubId, out var away);
                var row = BroadcastUi.Panel(_modalBody, "Next" + i, new Color(0.08f, 0.05f, 0.16f, 0.94f), true);
                BroadcastUi.Preferred(row, 52);
                AddInboxCrest(row.transform, home, 0.03f);
                AddInboxCrest(row.transform, away, 0.82f);
                var caption = LocalizationManager.FormatDate(fixture.Date, "dd MMM") + "  ·  " + FixtureKickoff(fixture)
                    + "  ·  " + (fixture.IsHome(club.Id) ? (english ? "Home" : "Casa") : (english ? "Away" : "Fora"))
                    + "\n" + (home == null ? "" : home.ShortName) + "  –  " + (away == null ? "" : away.ShortName)
                    + "  ·  " + FixtureCompetition(fixture);
                var label = BroadcastUi.Label(row.transform, "Text", caption, 12, Color.white, TextAnchor.MiddleLeft);
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 9;
                label.resizeTextMaxSize = 12;
                BroadcastUi.Place(label.rectTransform, 0.16f, 0.04f, 0.8f, 0.96f);
            }
            if (upcoming.Count == 0)
                AddLine(english ? "No upcoming fixtures." : "Sem próximos jogos.");
        }

        void BuildClubPerformance(Club club, bool english)
        {
            if (club.Id != _career.Club.Id && !_career.Scouted.Contains(club.Id))
            {
                var nextFixture = _career.NextUserFixture();
                var freeDays = nextFixture == null ? 7 : (nextFixture.Date.Date - _career.Calendar.CurrentDate.Date).Days;
                var analysisDays = Mathf.Max(0, Mathf.Min(3, freeDays - 1));
                var pending = ClubScoutDue(club.Id, out var dueTicks);
                var daysLeft = pending ? Mathf.Max(0, (new DateTime(dueTicks).Date - _career.Calendar.CurrentDate.Date).Days) : analysisDays;
                var locked = BroadcastUi.Panel(_modalBody, "ClubAnalysisLocked", new Color(0.045f, 0.07f, 0.095f, 0.98f));
                BroadcastUi.Preferred(locked, 280f);
                var heading = BroadcastUi.Label(locked.transform, "Heading", english ? "CLUB PERFORMANCE" : "DESEMPENHO DO CLUBE", 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(heading.rectTransform, 0.08f, 0.78f, 0.92f, 0.94f);
                var iconObject = new GameObject("LockedChart", typeof(RectTransform), typeof(CanvasRenderer), typeof(FinanceDonutGraphic));
                iconObject.transform.SetParent(locked.transform, false);
                var iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.52f);
                iconRect.sizeDelta = new Vector2(84f, 84f);
                iconObject.GetComponent<FinanceDonutGraphic>().SetValues(new[] { 1f }, new[] { new Color(0.2f, 0.85f, 0.42f) }, new Color(1f, 1f, 1f, 0.1f));
                var message = BroadcastUi.Label(locked.transform, "Message", english
                    ? "A scout report unlocks the club's results, tactical trends and player fit."
                    : "O relatório do olheiro desbloqueia resultados, tendências táticas e afinidade dos jogadores.",
                    15, UiTheme.Muted, TextAnchor.MiddleCenter);
                message.horizontalOverflow = HorizontalWrapMode.Wrap;
                BroadcastUi.Place(message.rectTransform, 0.1f, 0.3f, 0.9f, 0.44f);
                var scoutLabel = pending
                    ? english ? "ANALYSIS IN PROGRESS · " + daysLeft + " DAYS" : "EM ANÁLISE · FALTAM " + daysLeft + " DIAS"
                    : analysisDays > 0
                    ? english ? "ANALYZE CLUB · " + analysisDays + " DAYS" : "ANALISAR CLUBE · " + analysisDays + " DIAS"
                    : english ? "NO TIME BEFORE THE NEXT MATCH" : "SEM TEMPO ANTES DO PRÓXIMO JOGO";
                var scoutButton = BroadcastUi.Button(locked.transform, "ScoutClub", scoutLabel, () => AnalyzeClub(club),
                    pending || analysisDays <= 0 ? new Color(1f, 1f, 1f, 0.08f) : UiTheme.Accent,
                    pending || analysisDays <= 0 ? Color.white : UiTheme.ButtonText, 15);
                scoutButton.interactable = !pending && analysisDays > 0;
                BroadcastUi.Place(scoutButton.GetComponent<RectTransform>(), 0.22f, 0.06f, 0.78f, 0.22f);
                if (_modalAction != null)
                    _modalAction.gameObject.SetActive(false);
                return;
            }
            if (_modalAction != null) _modalAction.gameObject.SetActive(false);
            var played = 0;
            var wins = 0;
            var draws = 0;
            var losses = 0;
            var goalsFor = 0;
            var goalsAgainst = 0;
            foreach (var fixture in _career.Fixtures)
            {
                if (!fixture.Played || !fixture.Involves(club.Id)) continue;
                var home = fixture.IsHome(club.Id);
                var scored = home ? fixture.HomeGoals : fixture.AwayGoals;
                var conceded = home ? fixture.AwayGoals : fixture.HomeGoals;
                played++;
                goalsFor += scored;
                goalsAgainst += conceded;
                if (scored > conceded) wins++;
                else if (scored == conceded) draws++;
                else losses++;
            }

            SeasonSchedule.RebuildTable(_career);
            var leaguePosition = 0;
            var leagueClubCount = 0;
            var leagueRows = new List<LeagueTableRow>();
            foreach (var row in _career.Table)
                if (row.LeagueId == club.LeagueId) leagueRows.Add(row);
            leagueClubCount = leagueRows.Count;
            leagueRows.Sort((a, b) =>
            {
                var points = b.Points.CompareTo(a.Points);
                return points != 0 ? points : b.GoalDifference.CompareTo(a.GoalDifference);
            });
            leaguePosition = leagueRows.FindIndex(row => row.ClubId == club.Id) + 1;

            var panel = BroadcastUi.Panel(_modalBody, "ClubPerformanceDashboard", new Color(0.045f, 0.07f, 0.095f, 0.98f));
            BroadcastUi.Preferred(panel, 320f);
            var title = BroadcastUi.Label(panel.transform, "Title", english ? "SEASON PERFORMANCE" : "DESEMPENHO DA ÉPOCA", 18, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.02f, 0.91f, 0.98f, 0.99f);
            var resultCard = BroadcastUi.Panel(panel.transform, "ResultsChart", new Color(0.07f, 0.12f, 0.2f, 0.98f));
            BroadcastUi.Place(resultCard.rectTransform, 0.015f, 0.2f, 0.49f, 0.89f);
            var resultTitle = BroadcastUi.Label(resultCard.transform, "Title", english ? "RESULTS" : "RESULTADOS", 13, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(resultTitle.rectTransform, 0.06f, 0.87f, 0.94f, 0.98f);
            var donutObject = new GameObject("ResultsDonut", typeof(RectTransform), typeof(CanvasRenderer), typeof(FinanceDonutGraphic));
            donutObject.transform.SetParent(resultCard.transform, false);
            var donutRect = donutObject.GetComponent<RectTransform>();
            donutRect.anchorMin = donutRect.anchorMax = new Vector2(0.5f, 0.55f);
            donutRect.sizeDelta = new Vector2(112f, 112f);
            donutObject.GetComponent<FinanceDonutGraphic>().SetValues(new[] { (float)wins, (float)draws, (float)losses },
                new[] { UiTheme.Fitness, new Color(0.95f, 0.69f, 0.16f), UiTheme.Danger }, new Color(1f, 1f, 1f, 0.12f));
            var center = BroadcastUi.Label(resultCard.transform, "Matches", played.ToString(), 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(center.rectTransform, 0.4f, 0.52f, 0.6f, 0.66f);
            var centerLabel = BroadcastUi.Label(resultCard.transform, "MatchesLabel", english ? "MATCHES" : "JOGOS", 9, UiTheme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(centerLabel.rectTransform, 0.38f, 0.45f, 0.62f, 0.53f);
            var resultLegend = BroadcastUi.Label(resultCard.transform, "Legend", (english ? "WINS " : "VITÓRIAS ") + wins + "     "
                + (english ? "DRAWS " : "EMPATES ") + draws + "     " + (english ? "LOSSES " : "DERROTAS ") + losses,
                11, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(resultLegend.rectTransform, 0.03f, 0.16f, 0.97f, 0.28f);
            var record = BroadcastUi.Label(resultCard.transform, "Record", (english ? "Goals " : "Golos ") + goalsFor + "–" + goalsAgainst
                + "    ·    " + (english ? "Difference " : "Diferença ") + (goalsFor - goalsAgainst), 12, UiTheme.Muted, TextAnchor.MiddleCenter);
            BroadcastUi.Place(record.rectTransform, 0.03f, 0.045f, 0.97f, 0.15f);

            var tacticsCard = BroadcastUi.Panel(panel.transform, "TacticalFit", new Color(0.07f, 0.12f, 0.2f, 0.98f));
            BroadcastUi.Place(tacticsCard.rectTransform, 0.505f, 0.2f, 0.985f, 0.89f);
            var tacticsTitle = BroadcastUi.Label(tacticsCard.transform, "Title", english ? "TACTICAL IDENTITY & PLAYER FIT" : "IDENTIDADE TÁTICA E AFINIDADE", 13, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(tacticsTitle.rectTransform, 0.04f, 0.87f, 0.96f, 0.98f);
            var tactics = club.Tactics ?? new TacticalIdentity { Formation = "4-3-3" };
            var identity = BroadcastUi.Label(tacticsCard.transform, "Identity", tactics.Formation + "   ·   " + LocalizationManager.Translate(tactics.Pressing)
                + "   ·   " + LocalizationManager.Translate(tactics.BuildUp) + "\n" + LocalizationManager.Translate(tactics.Width)
                + "   ·   " + LocalizationManager.Translate(tactics.Tempo) + "   ·   " + LocalizationManager.Translate(tactics.DefensiveLine),
                11, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(identity.rectTransform, 0.04f, 0.7f, 0.96f, 0.86f);
            var winRate = played == 0 ? 0f : wins * 100f / played;
            var roster = Squad(club);
            var preferences = new Dictionary<string, int>();
            var fits = new List<KeyValuePair<Player, float>>();
            foreach (var player in roster)
            {
                var options = IndividualOrders.ForPosition(player.Position);
                var bestOption = string.IsNullOrEmpty(player.Duty) ? "" : player.Duty;
                var bestScore = -1f;
                for (var i = 0; i < options.Length; i++)
                {
                    var score = PlayerTacticalFit.Evaluate(player, tactics, options[i]).TacticalScore;
                    if (score <= bestScore) continue;
                    bestScore = score;
                    bestOption = options[i];
                }
                if (string.IsNullOrEmpty(bestOption)) continue;
                preferences.TryGetValue(bestOption, out var count);
                preferences[bestOption] = count + 1;
                fits.Add(new KeyValuePair<Player, float>(player, bestScore));
            }
            fits.Sort((a, b) => b.Value.CompareTo(a.Value));
            var shownFit = Mathf.Min(5, fits.Count);
            for (var i = 0; i < shownFit; i++)
            {
                var player = fits[i].Key;
                var best = "";
                var options = IndividualOrders.ForPosition(player.Position);
                var bestScore = -1f;
                for (var optionIndex = 0; optionIndex < options.Length; optionIndex++)
                {
                    var score = PlayerTacticalFit.Evaluate(player, tactics, options[optionIndex]).TacticalScore;
                    if (score <= bestScore) continue;
                    bestScore = score;
                    best = options[optionIndex];
                }
                var row = BroadcastUi.Panel(tacticsCard.transform, "Fit" + i, new Color(1f, 1f, 1f, 0.045f));
                var top = 0.67f - i * 0.12f;
                BroadcastUi.Place(row.rectTransform, 0.035f, top - 0.105f, 0.965f, top);
                var name = BroadcastUi.Label(row.transform, "Player", BroadcastUi.ShortName(player.FullName) + "  ·  " + PresentationLabels.Position(player.Position, english), 11, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(name.rectTransform, 0.025f, 0.4f, 0.55f, 0.95f);
                var order = BroadcastUi.Label(row.transform, "Order", LocalizationManager.Translate(best) + "  ·  " + bestScore.ToString("0.0") + "/20", 10, UiTheme.Accent, TextAnchor.MiddleRight);
                BroadcastUi.Place(order.rectTransform, 0.56f, 0.4f, 0.98f, 0.95f);
                var track = BroadcastUi.Solid(row.transform, "Track", new Color(1f, 1f, 1f, 0.12f));
                BroadcastUi.Place(track.rectTransform, 0.025f, 0.12f, 0.975f, 0.32f);
                var fill = BroadcastUi.Solid(track.transform, "Fill", UiTheme.Fitness);
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(bestScore / 20f), 1f);
                fill.rectTransform.offsetMin = Vector2.zero;
                fill.rectTransform.offsetMax = Vector2.zero;
            }
            var metrics = BroadcastUi.Zone(panel.transform, "PerformanceMetrics", 0.015f, 0.025f, 0.985f, 0.17f);
            var goalsPerMatch = played == 0 ? 0f : goalsFor / (float)played;
            var concededPerMatch = played == 0 ? 0f : goalsAgainst / (float)played;
            AddPerformanceMetric(metrics, "Wins", english ? "WIN RATE" : "VITÓRIAS", winRate.ToString("0") + "%", winRate / 100f, 0f, 0.24f, 0.08f, 0.96f);
            AddPerformanceMetric(metrics, "Scoring", english ? "GOALS / MATCH" : "GOLOS / JOGO", goalsPerMatch.ToString("0.0"), goalsPerMatch / 5f, 0.255f, 0.495f, 0.08f, 0.96f);
            AddPerformanceMetric(metrics, "Defence", english ? "CONCEDED / MATCH" : "SOFRIDOS / JOGO", concededPerMatch.ToString("0.0"), 1f - concededPerMatch / 5f, 0.51f, 0.75f, 0.08f, 0.96f);
            AddPerformanceMetric(metrics, "Position", english ? "LEAGUE POSITION" : "LUGAR NA LIGA", leaguePosition <= 0 ? "—" : leaguePosition.ToString(),
                leaguePosition <= 0 ? 0f : 1f - (leaguePosition - 1f) / Mathf.Max(1f, leagueClubCount - 1f), 0.765f, 1f, 0.08f, 0.96f);
            AddScoutReading(club, tactics, english);
        }

        void AddScoutReading(Club club, TacticalIdentity tactics, bool english)
        {
            if (club == null || _career == null || _career.Club == null || club.Id == _career.Club.Id)
                return;
            var panel = BroadcastUi.Panel(_modalBody, "ScoutReading", new Color(0.07f, 0.1f, 0.16f, 0.98f));
            BroadcastUi.Preferred(panel, 268f);
            var title = BroadcastUi.Label(panel.transform, "Title", english ? "WHAT THE SCOUT NOTICED" : "O QUE O OLHEIRO REPAROU", 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.04f, 0.86f, 0.96f, 0.98f);
            var body = BroadcastUi.Label(panel.transform, "Body", ScoutAdvice(club, tactics, english), 14, Color.white, TextAnchor.UpperLeft);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            BroadcastUi.Place(body.rectTransform, 0.04f, 0.34f, 0.96f, 0.84f);
            var shares = AttackShares(club, tactics);
            var names = english ? new[] { "LEFT", "CENTRE", "RIGHT" } : new[] { "ESQUERDA", "CENTRO", "DIREITA" };
            for (var i = 0; i < 3; i++)
            {
                var y1 = 0.28f - i * 0.09f;
                var name = BroadcastUi.Label(panel.transform, "Side" + i, names[i] + "  " + shares[i] + "%", 12, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(name.rectTransform, 0.04f, y1 - 0.07f, 0.28f, y1);
                var track = BroadcastUi.Solid(panel.transform, "Track" + i, new Color(1f, 1f, 1f, 0.12f));
                BroadcastUi.Place(track.rectTransform, 0.3f, y1 - 0.055f, 0.94f, y1 - 0.015f);
                var fill = BroadcastUi.Solid(track.transform, "Fill", i == 1 ? UiTheme.Accent : UiTheme.Fitness);
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = new Vector2(shares[i] / 100f, 1f);
                fill.rectTransform.offsetMin = Vector2.zero;
                fill.rectTransform.offsetMax = Vector2.zero;
            }
        }

        static string ScoutAdvice(Club club, TacticalIdentity tactics, bool english)
        {
            var formation = tactics == null || string.IsNullOrEmpty(tactics.Formation) ? "4-3-3" : tactics.Formation;
            var name = club == null ? "" : club.ShortName;
            var pressing = tactics == null || string.IsNullOrEmpty(tactics.Pressing) ? "" : tactics.Pressing;
            var fullBacks = tactics == null || string.IsNullOrEmpty(tactics.FullBacks) ? "" : tactics.FullBacks;
            var wide = fullBacks.IndexOf("sobem", StringComparison.OrdinalIgnoreCase) >= 0 || formation == "3-4-3" || formation == "4-3-3";
            if (english)
            {
                if (formation == "5-4-1")
                    return name + " sit in a " + formation + ". The block is narrow and the spare man is the right midfielder, who breaks when the ball turns. I would keep an inside forward on our left, a winger holding the right touchline, and an attacking full-back only on the side they leave empty. Their pressing is " + pressing + ".";
                if (formation == "4-2-3-1")
                    return name + " play " + formation + ". The number 10 drops into the pocket between our lines. The left side attacks with an inside forward; the right stays a wide winger. A holding midfielder on the 10, plus an offensive full-back on our right, closes the lane they like. Pressing: " + pressing + ".";
                if (wide)
                    return name + " attack in a " + formation + ". I watched the left side: inside forward cutting in, full-back overlapping. The right is a traditional winger on the touchline. If we match that with an inside forward on our left, a winger on our right and an attacking full-back behind him, their left overload loses the free man. " + pressing + ".";
                return name + " use " + formation + ". Both strikers take turns dropping, and the full-backs stay. The space is between the centre-backs when one steps out. An inside forward from the left, arriving late, hurts them more than a second winger. " + pressing + ".";
            }
            if (formation == "5-4-1")
                return "O " + name + " fecha em " + formation + ". O bloco é estreito e o homem livre é o médio direito, que sai quando a bola vira. Eu entrava com avançado interior na nossa esquerda, extremo a segurar a linha direita, e lateral ofensivo só no lado que eles deixam vazio. A pressão deles é " + pressing + ".";
            if (formation == "4-2-3-1")
                return "O " + name + " joga em " + formation + ". O 10 cai no intervalo entre as linhas. A esquerda ataca com um avançado interior; a direita fica um extremo aberto. Um médio de contenção em cima do 10, e um lateral ofensivo na nossa direita, fecha o corredor que eles gostam. Pressão: " + pressing + ".";
            if (wide)
                return "O " + name + " ataca em " + formation + ". Vi o lado esquerdo: avançado interior a cortar para dentro e o lateral a subir. Na direita fica um extremo na linha. Se respondermos com avançado interior na nossa esquerda, extremo na direita e lateral ofensivo por trás dele, a superioridade deles perde o homem livre. " + pressing + ".";
            return "O " + name + " usa " + formation + ". Os dois avançados descem à vez e os laterais ficam. O espaço aparece entre os centrais quando um sai. Um avançado interior a chegar da esquerda magoa-os mais do que um segundo extremo. " + pressing + ".";
        }

        static int[] AttackShares(Club club, TacticalIdentity tactics)
        {
            var formation = tactics == null || string.IsNullOrEmpty(tactics.Formation) ? "4-3-3" : tactics.Formation;
            int left;
            int centre;
            int right;
            if (formation == "4-2-3-1") { left = 28; centre = 46; right = 26; }
            else if (formation == "5-4-1") { left = 24; centre = 34; right = 42; }
            else if (formation == "4-4-2") { left = 31; centre = 40; right = 29; }
            else if (formation == "3-4-3") { left = 40; centre = 18; right = 42; }
            else { left = 41; centre = 22; right = 37; }
            var seed = 0;
            var key = (club == null ? "" : club.Id) + formation;
            foreach (var c in key)
                seed = seed * 31 + c;
            var nudge = (seed & 7) - 3;
            left = Mathf.Clamp(left + nudge, 12, 60);
            right = Mathf.Clamp(right - nudge, 12, 60);
            centre = Mathf.Clamp(100 - left - right, 10, 70);
            var fix = 100 - (left + centre + right);
            centre += fix;
            return new[] { left, centre, right };
        }

        void AddPerformanceMetric(Transform parent, string objectName, string title, string value, float fill, float x0, float x1, float y0, float y1)
        {
            var card = BroadcastUi.Panel(parent, objectName, new Color(0.07f, 0.12f, 0.2f, 0.98f));
            BroadcastUi.Place(card.rectTransform, x0, y0, x1, y1);
            var heading = BroadcastUi.Label(card.transform, "Title", title, 9, UiTheme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.025f, 0.65f, 0.975f, 0.94f);
            var number = BroadcastUi.Label(card.transform, "Value", value, 19, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(number.rectTransform, 0.025f, 0.3f, 0.975f, 0.7f);
            var bar = BroadcastUi.Solid(card.transform, "Track", new Color(1f, 1f, 1f, 0.14f));
            BroadcastUi.Place(bar.rectTransform, 0.1f, 0.1f, 0.9f, 0.19f);
            var fillBar = BroadcastUi.Solid(bar.transform, "Fill", UiTheme.Fitness);
            fillBar.rectTransform.anchorMin = Vector2.zero;
            fillBar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fill), 1f);
            fillBar.rectTransform.offsetMin = Vector2.zero;
            fillBar.rectTransform.offsetMax = Vector2.zero;
        }

        void BuildClubTransfers(Club club, bool english)
        {
            AddHead(english ? "TRANSFER HISTORY" : "HISTÓRICO DE TRANSFERÊNCIAS");
            var shown = 0;
            foreach (var deal in _career.TransferHistory)
            {
                if (deal.FromClubId != club.Id && deal.ToClubId != club.Id) continue;
                if (!_career.World.Players.TryGetValue(deal.PlayerId ?? "", out var player)
                    || !_career.World.Clubs.TryGetValue(deal.FromClubId ?? "", out var from)
                    || !_career.World.Clubs.TryGetValue(deal.ToClubId ?? "", out var to)) continue;
                var date = deal.DateTicks > 0 ? LocalizationManager.FormatDate(new DateTime(deal.DateTicks), "dd MMM yyyy") : (english ? "Date unavailable" : "Data indisponível");
                AddLine(date + "  ·  " + player.FullName + "\n" + from.ShortName + " → " + to.ShortName
                    + "  ·  " + Euro(deal.Fee) + "  ·  " + Euro(deal.WeeklyWage) + (english ? "/wk" : "/sem")
                    + "  ·  " + deal.ContractYears + (english ? " years" : " anos") + "  ·  " + LocalizationManager.Translate(deal.Type));
                if (++shown >= 8) break;
            }
            if (shown == 0)
                AddLine(english ? "No recorded transfers for this club yet." : "Ainda não há transferências registadas para este clube.");
        }

        void BuildClubHistory(Club club, string city, string league, bool english)
        {
            var dashboard = BroadcastUi.Panel(_modalBody, "ClubHistoryDashboard", new Color(0.045f, 0.07f, 0.095f, 0.98f));
            BroadcastUi.Preferred(dashboard, 320f);
            var title = BroadcastUi.Label(dashboard.transform, "Title", english ? "CLUB HISTORY" : "HISTÓRIA DO CLUBE", 18, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.02f, 0.91f, 0.98f, 0.99f);
            var history = string.IsNullOrEmpty(club.History)
                ? (english ? club.Name + " represent " + city + " and compete in " + league + "." : club.Name + " representa " + city + " e compete em " + league + ".")
                : LocalizationManager.Translate(club.History);
            var summary = BroadcastUi.Panel(dashboard.transform, "ClubStory", new Color(0.07f, 0.12f, 0.2f, 0.98f));
            BroadcastUi.Place(summary.rectTransform, 0.015f, 0.71f, 0.985f, 0.885f);
            var founded = BroadcastUi.Label(summary.transform, "Founded", (english ? "FOUNDED " : "FUNDADO EM ") + club.FoundedYear
                + "   ·   " + ReputationStars(club.Reputation) + "   ·   " + LocalizationManager.Translate(club.PublicReputation), 12, UiTheme.Score, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(founded.rectTransform, 0.025f, 0.68f, 0.97f, 0.96f);
            var story = BroadcastUi.Label(summary.transform, "Story", history, 12, Color.white, TextAnchor.MiddleLeft);
            story.resizeTextForBestFit = true;
            story.resizeTextMinSize = 10;
            story.resizeTextMaxSize = 12;
            BroadcastUi.Place(story.rectTransform, 0.025f, 0.08f, 0.97f, 0.68f);

            var trophies = new List<FootballMemory>();
            var events = new List<FootballMemory>();
            foreach (var memory in _career.Memories)
            {
                if (memory == null || memory.ClubId != club.Id) continue;
                var isTrophy = !string.IsNullOrEmpty(memory.Kind) && memory.Kind.IndexOf("titulo", StringComparison.OrdinalIgnoreCase) >= 0;
                var displayMemory = new FootballMemory
                {
                    WhenTicks = memory.WhenTicks,
                    Importance = memory.Importance,
                    Kind = memory.Kind,
                    ClubId = memory.ClubId,
                    CompetitionId = memory.CompetitionId,
                    RelatedClubId = memory.RelatedClubId,
                    PlayerId = memory.PlayerId,
                    Title = isTrophy ? ClubHistoryTrophyTitle(memory.CompetitionId, memory.Title, english) : LocalizationManager.Translate(memory.Title),
                    Detail = isTrophy && english
                        ? "Season winner · " + LocalizationManager.Translate(SeasonSchedule.Competition(memory.CompetitionId))
                        : LocalizationManager.Translate(memory.Detail),
                    Value = memory.Value
                };
                if (isTrophy) trophies.Add(displayMemory);
                events.Add(displayMemory);
            }
            foreach (var trophy in _career.Trophies)
            {
                if (trophy == null || trophy.ClubId != club.Id) continue;
                var alreadyShown = trophies.Exists(memory => memory.CompetitionId == trophy.CompetitionId
                    && (memory.WhenTicks <= 0 || memory.WhenTicks > DateTime.MaxValue.Ticks || trophy.DateTicks <= 0 || trophy.DateTicks > DateTime.MaxValue.Ticks
                        || new DateTime(memory.WhenTicks).Year == new DateTime(trophy.DateTicks).Year));
                if (alreadyShown) continue;
                var trophyMemory = new FootballMemory
                {
                    ClubId = club.Id,
                    CompetitionId = trophy.CompetitionId,
                    WhenTicks = trophy.DateTicks,
                    Kind = "titulo",
                    Title = ClubHistoryTrophyTitle(trophy.CompetitionId, trophy.Title, english),
                    Detail = english ? "Competition winner · season " + trophy.SeasonStartYear + "/" + (trophy.SeasonStartYear + 1)
                        : "Vencedor da competição · época " + trophy.SeasonStartYear + "/" + (trophy.SeasonStartYear + 1)
                };
                trophies.Add(trophyMemory);
                events.Add(trophyMemory);
            }
            foreach (var fixture in _career.Fixtures)
            {
                if (fixture == null || !fixture.Played || !fixture.Involves(club.Id)) continue;
                var opponentId = fixture.OpponentId(club.Id);
                var opponent = _career.World.Clubs.TryGetValue(opponentId, out var other) ? other.ShortName : opponentId;
                var goalsFor = fixture.IsHome(club.Id) ? fixture.HomeGoals : fixture.AwayGoals;
                var goalsAgainst = fixture.IsHome(club.Id) ? fixture.AwayGoals : fixture.HomeGoals;
                var result = goalsFor > goalsAgainst ? (english ? "Win" : "Vitória")
                    : goalsFor == goalsAgainst ? (english ? "Draw" : "Empate") : (english ? "Defeat" : "Derrota");
                events.Add(new FootballMemory
                {
                    ClubId = club.Id,
                    WhenTicks = fixture.Date.Ticks,
                    Kind = "match",
                    Title = result + " " + goalsFor + "–" + goalsAgainst + " " + (english ? "against " : "frente a ") + opponent,
                    Detail = LocalizationManager.Translate(SeasonSchedule.Competition(fixture.LeagueId))
                });
            }
            foreach (var deal in _career.TransferHistory)
            {
                if (deal == null || (deal.FromClubId != club.Id && deal.ToClubId != club.Id)
                    || !_career.World.Players.TryGetValue(deal.PlayerId ?? "", out var movedPlayer)) continue;
                var fromName = _career.World.Clubs.TryGetValue(deal.FromClubId ?? "", out var fromClub) ? fromClub.ShortName : "—";
                var toName = _career.World.Clubs.TryGetValue(deal.ToClubId ?? "", out var toClub) ? toClub.ShortName : "—";
                events.Add(new FootballMemory
                {
                    ClubId = club.Id,
                    WhenTicks = deal.DateTicks,
                    Kind = "transfer",
                    Title = (english ? "Transfer · " : "Transferência · ") + movedPlayer.FullName,
                    Detail = fromName + " → " + toName + " · " + Euro(deal.Fee)
                });
            }
            trophies.Sort((a, b) => b.WhenTicks.CompareTo(a.WhenTicks));
            events.Sort((a, b) => b.WhenTicks.CompareTo(a.WhenTicks));
            var trophyPanel = BroadcastUi.Panel(dashboard.transform, "Trophies", new Color(0.07f, 0.12f, 0.2f, 0.98f));
            BroadcastUi.Place(trophyPanel.rectTransform, 0.015f, 0.43f, 0.985f, 0.69f);
            var trophyHeading = BroadcastUi.Label(trophyPanel.transform, "Heading", english ? "TROPHIES" : "TROFÉUS", 12, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(trophyHeading.rectTransform, 0.02f, 0.78f, 0.98f, 0.98f);
            var shownTrophies = Mathf.Min(3, trophies.Count);
            for (var i = 0; i < shownTrophies; i++)
            {
                var trophy = trophies[i];
                var x0 = 0.02f + i * 0.325f;
                var card = BroadcastUi.Panel(trophyPanel.transform, "Trophy" + i, new Color(0.14f, 0.11f, 0.045f, 0.92f));
                BroadcastUi.Place(card.rectTransform, x0, 0.06f, x0 + 0.31f, 0.76f);
                var iconObject = new GameObject("Cup", typeof(RectTransform), typeof(CanvasRenderer), typeof(TrophySilhouette));
                iconObject.transform.SetParent(card.transform, false);
                var iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.18f, 0.53f);
                iconRect.sizeDelta = new Vector2(48f, 48f);
                iconObject.GetComponent<TrophySilhouette>().color = new Color(1f, 0.77f, 0.2f);
                var name = BroadcastUi.Label(card.transform, "Name", trophy.Title, 10, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 8;
                name.resizeTextMaxSize = 10;
                BroadcastUi.Place(name.rectTransform, 0.36f, 0.37f, 0.98f, 0.78f);
                var date = BroadcastUi.Label(card.transform, "Date", trophy.WhenTicks > 0 && trophy.WhenTicks <= DateTime.MaxValue.Ticks
                    ? new DateTime(trophy.WhenTicks).Year.ToString() : "—", 11, UiTheme.Score, TextAnchor.MiddleLeft);
                BroadcastUi.Place(date.rectTransform, 0.36f, 0.12f, 0.98f, 0.38f);
            }
            if (shownTrophies == 0)
            {
                var empty = BroadcastUi.Label(trophyPanel.transform, "NoTrophies", english
                    ? "Trophy records appear when this club wins a competition in your career."
                    : "Os troféus aparecem quando este clube vencer uma competição durante a carreira.", 12, UiTheme.Muted, TextAnchor.MiddleCenter);
                BroadcastUi.Place(empty.rectTransform, 0.04f, 0.1f, 0.96f, 0.74f);
            }

            var timeline = BroadcastUi.Panel(dashboard.transform, "Timeline", new Color(0.07f, 0.12f, 0.2f, 0.98f));
            BroadcastUi.Place(timeline.rectTransform, 0.015f, 0.025f, 0.985f, 0.41f);
            var timelineHeading = BroadcastUi.Label(timeline.transform, "Heading", english ? "MILESTONES" : "MARCOS HISTÓRICOS", 12, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(timelineHeading.rectTransform, 0.02f, 0.86f, 0.98f, 0.99f);
            var shownEvents = 0;
            for (var i = 0; i < events.Count && shownEvents < 4; i++)
            {
                var memory = events[i];
                var date = memory.WhenTicks > 0 && memory.WhenTicks <= DateTime.MaxValue.Ticks
                    ? new DateTime(memory.WhenTicks).ToString("dd MMM yyyy") : "—";
                var yTop = 0.83f - shownEvents * 0.195f;
                var row = BroadcastUi.Panel(timeline.transform, "Event" + shownEvents, new Color(1f, 1f, 1f, 0.035f));
                BroadcastUi.Place(row.rectTransform, 0.02f, yTop - 0.175f, 0.98f, yTop);
                var line = BroadcastUi.Label(row.transform, "Text", date + "   ·   " + memory.Title + "\n" + memory.Detail, 10, Color.white, TextAnchor.MiddleLeft);
                line.resizeTextForBestFit = true;
                line.resizeTextMinSize = 8;
                line.resizeTextMaxSize = 10;
                BroadcastUi.Place(line.rectTransform, 0.02f, 0.04f, 0.98f, 0.96f);
                shownEvents++;
            }
            if (shownEvents == 0)
            {
                var foundation = BroadcastUi.Label(timeline.transform, "Foundation", (english ? "Founded in " : "Fundado em ") + club.FoundedYear
                    + "   ·   " + (english ? "A new chapter begins with your career." : "Uma nova fase começa com esta carreira."),
                    12, Color.white, TextAnchor.MiddleCenter);
                BroadcastUi.Place(foundation.rectTransform, 0.04f, 0.18f, 0.96f, 0.76f);
            }
        }

        string ClubHistoryTrophyTitle(string competitionId, string sourceTitle, bool english)
        {
            if (!english) return LocalizationManager.Translate(sourceTitle);
            if (competitionId == "league:campeoes") return "Champions League winner";
            var competition = LocalizationManager.Translate(SeasonSchedule.Competition(competitionId));
            if (string.IsNullOrEmpty(competition)) return LocalizationManager.Translate(sourceTitle);
            return !string.IsNullOrEmpty(competitionId) && competitionId.StartsWith("cup:", StringComparison.Ordinal)
                ? "Cup winner · " + competition
                : "League champion · " + competition;
        }

        void BuildClubFacilities(Club club, string city, string league, bool english)
        {
            AddHead(english ? "STADIUM" : "ESTÁDIO");
            AddLine(LocalizationManager.Translate(club.Stadium) + "\n" + city + "  ·  " + league);
            AddHead(english ? "CLUB DETAILS" : "DETALHES DO CLUBE");
            AddLine((english ? "Founded  " : "Fundado em  ") + club.FoundedYear
                + "\n" + (english ? "First-team squad  " : "Plantel principal  ") + Squad(club).Count
                + "\n" + (english ? "Coaching staff  " : "Equipa técnica  ") + (club.Staff == null ? 0 : club.Staff.Count));
            AddLine(english ? "Stadium capacity and training-ground facilities are not recorded in this career database." : "A capacidade do estádio e as instalações de treino não estão registadas nesta carreira.");
        }

        void AddClubPitch(Club club, float height, float cardWidth, float cardHeight)
        {
            var field = BroadcastUi.Panel(_modalBody, "ClubField", new Color(0.06f, 0.25f, 0.14f, 1f));
            BroadcastUi.Preferred(field, height);
            var verticalPitch = BroadcastUi.Zone(field.transform, "VerticalHalfPitch", 0.31f, 0.015f, 0.69f, 0.985f);
            BroadcastUi.DrawVerticalHalfPitch(verticalPitch, new Color(0.12f, 0.46f, 0.22f));
            PlaceVerticalShape(verticalPitch, club, false, cardWidth, cardHeight);
        }

        void AddClubLeaguePosition(Club club, string league, bool english)
        {
            AddHead(english ? "LEAGUE POSITION" : "POSIÇÃO NA LIGA");
            SeasonSchedule.RebuildTable(_career);
            var leagueRows = new List<LeagueTableRow>();
            foreach (var row in _career.Table)
                if (row.LeagueId == club.LeagueId) leagueRows.Add(row);
            leagueRows.Sort((a, b) =>
            {
                var points = b.Points.CompareTo(a.Points);
                if (points != 0) return points;
                var difference = b.GoalDifference.CompareTo(a.GoalDifference);
                return difference != 0 ? difference : b.GoalsFor.CompareTo(a.GoalsFor);
            });
            var place = leagueRows.FindIndex(row => row.ClubId == club.Id) + 1;
            var clubTable = leagueRows.Find(row => row.ClubId == club.Id);
            if (clubTable != null)
                AddLine(league + "  ·  " + (english ? "Position " : "Lugar ") + place + " / " + leagueRows.Count
                    + "\n" + (english ? "Played  " : "Jogos  ") + clubTable.Played + "   W " + clubTable.Won + "   D " + clubTable.Drawn + "   L " + clubTable.Lost
                    + "   ·   " + (english ? "Points  " : "Pontos  ") + clubTable.Points);
            else
                AddLine(english ? "League results are not available yet." : "A classificação da liga ainda não está disponível.");
        }

        void AddClubRecentMatches(Club club, bool english, int limit)
        {
            AddHead(english ? "RECENT MATCHES" : "JOGOS RECENTES");
            var recent = new List<Fixture>();
            foreach (var fixture in _career.Fixtures)
                if (fixture.Played && fixture.Involves(club.Id))
                    recent.Add(fixture);
            recent.Sort((a, b) => b.Date.CompareTo(a.Date));
            for (var i = 0; i < Mathf.Min(limit, recent.Count); i++)
            {
                var fixture = recent[i];
                var opponentId = fixture.OpponentId(club.Id);
                var opponent = _career.World.Clubs.TryGetValue(opponentId, out var other) ? other.ShortName : opponentId;
                var goalsFor = fixture.IsHome(club.Id) ? fixture.HomeGoals : fixture.AwayGoals;
                var goalsAgainst = fixture.IsHome(club.Id) ? fixture.AwayGoals : fixture.HomeGoals;
                AddLine(LocalizationManager.FormatDate(fixture.Date, "dd MMM") + "  ·  " + FixtureKickoff(fixture)
                    + "  ·  " + FixtureCompetition(fixture) + "  ·  " + opponent + "  ·  " + goalsFor + "–" + goalsAgainst);
            }
            if (recent.Count == 0)
                AddLine(english ? "No matches have been played yet." : "Ainda não há jogos realizados.");
        }

        void StartScout(Club opponent, Fixture fixture)
        {
            var days = FreeAnalysisDays();
            if (days <= 0)
            {
                AddLine("Não há dias livres antes do jogo. A análise fica por fazer.");
                _modalAction.gameObject.SetActive(false);
                return;
            }
            if (!QueueClubScout(opponent, days))
            {
                AddLine("A análise a este clube já está a decorrer.");
                return;
            }
            CareerStore.Save(_career);
            Refresh();
            OpenFixture(fixture);
        }

        void AnalyzeClub(Club club)
        {
            if (club == null || _career == null) return;
            var english = LocalizationManager.Current == GameLanguage.English;
            var days = FreeAnalysisDays();
            if (days <= 0)
            {
                if (_modalActionLabel != null)
                    _modalActionLabel.text = english ? "NO TIME BEFORE THE NEXT MATCH" : "SEM TEMPO ANTES DO PRÓXIMO JOGO";
                if (_modalAction != null) _modalAction.interactable = false;
                return;
            }
            if (!QueueClubScout(club, days))
            {
                OpenClubDossier(club, 3);
                return;
            }
            CareerStore.Save(_career);
            Refresh();
            OpenClubDossier(club, 3);
        }

        int FreeAnalysisDays()
        {
            var next = _career.NextUserFixture();
            var free = next == null ? 7 : (next.Date.Date - _career.Calendar.CurrentDate.Date).Days;
            return Mathf.Max(0, Mathf.Min(3, free - 1));
        }

        bool QueueClubScout(Club club, int days)
        {
            if (club == null || days <= 0 || ClubScoutDue(club.Id, out _))
                return false;
            var due = _career.Calendar.CurrentDate.Date.AddDays(days);
            _career.Notebook.Add("job|scout|" + club.Id + "|" + due.Ticks);
            PostDesk("✉ O olheiro começou a análise ao " + club.ShortName + ". O relatório fica pronto a " + LocalizationManager.FormatDate(due, "d MMM") + ".");
            return true;
        }

        bool ClubScoutDue(string clubId, out long due)
        {
            due = 0;
            if (string.IsNullOrEmpty(clubId))
                return false;
            var prefix = "job|scout|" + clubId + "|";
            foreach (var line in _career.Notebook)
            {
                if (line != null && line.StartsWith(prefix) && long.TryParse(line.Substring(prefix.Length), out due))
                    return true;
            }
            return false;
        }

        bool DeskDue(string kind, out long due)
        {
            due = 0;
            var found = false;
            var prefix = "job|" + kind + "|";
            foreach (var line in _career.Notebook)
            {
                if (line == null || !line.StartsWith(prefix))
                    continue;
                var tail = line.Substring(prefix.Length);
                var bits = tail.Split('|');
                if (long.TryParse(bits[bits.Length - 1], out var ticks) && ticks > due)
                {
                    due = ticks;
                    found = true;
                }
            }
            return found;
        }

        bool ResolveDeskJobs()
        {
            if (_career == null)
                return false;
            var now = _career.Calendar.CurrentDate.Date;
            var changed = false;
            for (var i = _career.Notebook.Count - 1; i >= 0; i--)
            {
                var line = _career.Notebook[i];
                if (string.IsNullOrEmpty(line) || !line.StartsWith("job|"))
                    continue;
                var bits = line.Split('|');
                if (bits.Length >= 4 && bits[1] == "scout" && long.TryParse(bits[3], out var scoutDue) && new DateTime(scoutDue).Date <= now)
                {
                    _career.Notebook.RemoveAt(i);
                    _career.Scouted.Add(bits[2]);
                    var name = _career.World.Clubs.TryGetValue(bits[2], out var club) ? club.ShortName : bits[2];
                    PostDesk("✉ Análise ao " + name + " concluída. O relatório já pode ser aberto na ficha do clube.\n[[clube:" + bits[2] + "]]");
                    changed = true;
                }
                else if (bits.Length >= 3 && bits[1] == "budget" && long.TryParse(bits[2], out var budgetDue) && new DateTime(budgetDue).Date <= now)
                {
                    _career.Notebook.RemoveAt(i);
                    ResolveBudgetMeeting();
                    changed = true;
                }
            }
            return changed;
        }

        void ResolveBudgetMeeting()
        {
            if (!NotebookHas("budget-live"))
                _career.Notebook.Add("budget-live|general");
            PostDesk("✉ A direção está na sala. A reunião sobre a verba pode começar.");
            OpenBoardMeeting(null, 0, 0, 0, 0);
        }

        bool NotebookHas(string token)
        {
            if (_career == null || string.IsNullOrEmpty(token))
                return false;
            foreach (var line in _career.Notebook)
                if (!string.IsNullOrEmpty(line) && line.StartsWith(token, StringComparison.Ordinal))
                    return true;
            return false;
        }

        void RemoveNotebookPrefix(string token)
        {
            if (_career == null || string.IsNullOrEmpty(token))
                return;
            for (var i = _career.Notebook.Count - 1; i >= 0; i--)
                if (!string.IsNullOrEmpty(_career.Notebook[i]) && _career.Notebook[i].StartsWith(token, StringComparison.Ordinal))
                    _career.Notebook.RemoveAt(i);
        }

        int PendingIncomingFees()
        {
            var sum = 0;
            if (_career == null || _career.Club == null)
                return 0;
            foreach (var line in _career.Notebook)
            {
                if (string.IsNullOrEmpty(line) || !line.StartsWith("job|move|", StringComparison.Ordinal))
                    continue;
                var bits = line.Split('|');
                if (bits.Length < 8 || bits[3] != _career.Club.Id || bits[7] != "in")
                    continue;
                if (int.TryParse(bits[4], out var fee))
                    sum += Math.Max(0, fee);
            }
            return sum;
        }

        void OpenBoardMeeting(Player player, int fee, int wage, int years, int gap, int wageGap = 0,
            StaffOfferCandidate staffCandidate = null, string staffPhoneKey = null)
        {
            _boardStaffCandidate = staffCandidate;
            _boardStaffPhoneKey = staffPhoneKey;
            _boardPlayerId = player == null ? null : player.Id;
            _boardFee = fee;
            _boardWage = wage;
            _boardYears = years;
            _boardGap = gap;
            _boardWageGap = wageGap;
            var english = LocalizationManager.Current == GameLanguage.English;
            var feeGapText = gap > 0
                ? (english ? "The fee is " + Euro(fee) + "; the club is short " + Euro(gap) + "." : "O valor é " + Euro(fee) + "; faltam " + Euro(gap) + ".")
                : (english ? "The transfer fee is covered." : "O valor da transferência está coberto.");
            var wageGapText = wageGap > 0
                ? (english ? " The wage budget is also short " + Euro(wageGap) + "/week." : " Também faltam " + Euro(wageGap) + "/semana na verba salarial.")
                : "";
            var opener = _boardStaffCandidate != null && wageGap > 0
                ? (english
                    ? "The club has " + Euro(Math.Max(0, StaffWeeklyWageRoom())) + "/week in wage room, but this contract needs another " + Euro(wageGap) + ". The board must approve the weekly commitment."
                    : "O clube tem " + Euro(Math.Max(0, StaffWeeklyWageRoom())) + "/semana de folga salarial, mas este contrato precisa de mais " + Euro(wageGap) + ". A direção tem de aprovar o compromisso semanal.")
                : gap > 0 || wageGap > 0
                ? (english
                    ? "The president closes the folder. \"" + feeGapText + wageGapText + " Tell me why this room should find the rest.\""
                    : "O presidente fecha a pasta. \"" + feeGapText + wageGapText + " Diz-me porque é que esta sala há de arranjar o resto.\"")
                : (english
                    ? "The president does not sit down. \"You asked for more transfer money. I have the results and the dressing room in front of me. Talk.\""
                    : "O presidente não se senta. \"Pediste mais verba de transferências. Tenho os resultados e o balneário à frente. Fala.\"");
            OpenModal(english ? "BOARD MEETING" : "REUNIÃO COM A DIREÇÃO", opener, false);
            if (_modalLead != null)
            {
                _modalLead.horizontalOverflow = HorizontalWrapMode.Wrap;
                _modalLead.verticalOverflow = VerticalWrapMode.Overflow;
            }
            var lines = english
                ? new[]
                {
                    "The last results give us the argument. If the season stalls, what the board asked for stays unfinished.",
                    "The dressing room already knows there is no money. If I leave this room empty-handed, tomorrow's conversation is not with me.",
                    "I am not asking for a favour. Either the money appears, or I deal with my exit at the end of the month."
                }
                : new[]
                {
                    "Os últimos resultados dão-nos argumento. Se a época parar, o que a direção pediu fica por cumprir.",
                    "O balneário já sabe que não há dinheiro. Se sair daqui de mãos vazias, a conversa de amanhã não é comigo.",
                    "Não estou a pedir um favor. Ou o valor aparece, ou trato da saída no fim do mês."
                };
            for (var i = 0; i < lines.Length; i++)
            {
                var choice = i;
                var row = BroadcastUi.Button(_modalBody, "Board" + i, lines[i], () => ApplyBoardChoice(choice), new Color(0.1f, 0.05f, 0.18f, 0.96f), Color.white, 14);
                var caption = row.GetComponentInChildren<Text>();
                if (caption != null)
                {
                    caption.horizontalOverflow = HorizontalWrapMode.Wrap;
                    caption.verticalOverflow = VerticalWrapMode.Overflow;
                    caption.alignment = TextAnchor.MiddleLeft;
                }
                BroadcastUi.Preferred(row.GetComponent<RectTransform>(), 64);
            }
            if (_modalAction != null)
            {
                _modalActionLabel.text = english ? "LEAVE THE ROOM" : "SAIR DA SALA";
                _modalAction.onClick.RemoveAllListeners();
                _modalAction.onClick.AddListener(() =>
                {
                    _boardPlayerId = null;
                    _boardGap = 0;
                    _boardWageGap = 0;
                    _boardStaffCandidate = null;
                    _boardStaffPhoneKey = null;
                    CloseModal();
                });
            }
        }

        void ApplyBoardChoice(int choice)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var approval = _career.SupporterApproval;
            var tension = _career.Club == null ? 40 : _career.Club.DressingRoomTension;
            var won = false;
            string reply;
            if (choice == 0)
            {
                won = approval >= 46 && approval < 72;
                reply = won
                    ? (english ? "The president turns a page and signs. \"It will do. Do not come back this month.\"" : "O presidente vira uma página e assina. \"Serve. Não voltes este mês.\"")
                    : approval >= 72
                        ? (english ? "\"We are fine. I will not inflate the budget because the speech was polite.\"" : "\"Estamos bem. Não inflaciono o orçamento porque o discurso foi simpático.\"")
                        : (english ? "\"With these results, that argument does not reach this room.\"" : "\"Com estes resultados, esse argumento não chega a esta sala.\"");
            }
            else if (choice == 1)
            {
                won = tension >= 56;
                reply = won
                    ? (english ? "\"The dressing room already reached me. The money stays. You sort the rest inside.\"" : "\"O balneário já me chegou. Fica o valor. O resto resolves tu lá dentro.\"")
                    : (english ? "\"Do not bring me the dressing room. I will speak to the captain. No money leaves this table.\"" : "\"Não me venhas com o balneário. Eu falo com o capitão. Desta mesa não sai dinheiro.\"");
            }
            else
            {
                won = approval >= 63 || (approval >= 54 && tension >= 48);
                reply = won
                    ? (english ? "He looks at the window. \"I do not like being pushed. I also do not want this season to die here.\"" : "Olha para a janela. \"Não gosto de ser empurrado. Também não quero que esta época morra aqui.\"")
                    : (english ? "\"Raise your voice again and the conversation ends with you, not with the budget.\"" : "\"Levanta outra vez a voz e a conversa acaba contigo, não com o orçamento.\"");
            }
            var transferGrant = 0;
            var wageGrant = 0;
            var cashGrant = 0;
            if (won && _career.Club != null && _career.Club.Finance != null)
            {
                if (_boardGap > 0)
                    transferGrant = _boardGap;
                else if (_boardStaffCandidate == null && string.IsNullOrEmpty(_boardPlayerId))
                    transferGrant = Mathf.Clamp(_career.Club.Reputation * 42000, 800000, 6500000);
                wageGrant = _boardWageGap;
                _career.Club.Finance.TransferBudget = (int)Math.Min(int.MaxValue, (long)_career.Club.Finance.TransferBudget + transferGrant);
                _career.Club.Finance.WageBudget = (int)Math.Min(int.MaxValue, (long)_career.Club.Finance.WageBudget + wageGrant);
                cashGrant = transferGrant;
                _career.Club.Finance.Balance = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, (long)_career.Club.Finance.Balance + cashGrant));
                if (cashGrant > 0)
                    ClubFinanceLedger.Record(_career.Club, _career.Calendar.CurrentDate, cashGrant, "board", english ? "Board meeting" : "Reunião com a direção");
            }
            RemoveNotebookPrefix("budget-live");
            var staffCandidate = _boardStaffCandidate;
            var staffPhoneKey = _boardStaffPhoneKey;
            var waitKey = staffCandidate != null ? "staff-budget-wait" : "budget-wait";
            _career.Notebook.Add("job|" + waitKey + "|" + _career.Calendar.CurrentDate.Date.AddDays(10).Ticks);
            var playerId = _boardPlayerId;
            var fee = _boardFee;
            var wage = _boardWage;
            var years = _boardYears;
            _boardPlayerId = null;
            _boardGap = 0;
            _boardWageGap = 0;
            _boardStaffCandidate = null;
            _boardStaffPhoneKey = null;
            CloseModal();
            var outcome = reply;
            if (transferGrant > 0)
                outcome += english ? "\n\n" + Euro(transferGrant) + " enters the transfer budget." : "\n\nEntram " + Euro(transferGrant) + " no orçamento de transferências.";
            if (wageGrant > 0)
                outcome += english ? "\n\nWeekly wage room rises by " + Euro(wageGrant) + "." : "\n\nA folga salarial semanal aumenta " + Euro(wageGrant) + ".";
            if (cashGrant <= 0 && wageGrant <= 0)
                outcome += english ? "\n\nNothing was added." : "\n\nNão entrou nada.";
            PostDesk("✉ " + outcome.Replace("\n\n", " "));
            CareerStore.Save(_career);
            Refresh();
            var approvalGranted = cashGrant > 0 || wageGrant > 0;
            if (approvalGranted && !string.IsNullOrEmpty(playerId) && _career.World.Players.TryGetValue(playerId, out var player))
            {
                OpenModal(english ? "THE ROOM ANSWERS" : "A SALA RESPONDE", outcome, true);
                _modalActionLabel.text = english ? "CONTINUE THE DEAL" : "CONTINUAR O NEGÓCIO";
                _modalAction.onClick.RemoveAllListeners();
                _modalAction.onClick.AddListener(() =>
                {
                    CloseModal();
                    if (string.IsNullOrEmpty(player.ClubId))
                        ResolveFreeAgentOffer(player, wage, years);
                    else
                        ResolveTransferOffer(player, fee, wage, years);
                });
                return;
            }
            if (approvalGranted && staffCandidate != null)
            {
                OpenModal(english ? "THE ROOM ANSWERS" : "A SALA RESPONDE", outcome, true);
                _modalActionLabel.text = english ? "RETURN TO NEGOTIATION" : "VOLTAR À NEGOCIAÇÃO";
                _modalAction.onClick.RemoveAllListeners();
                _modalAction.onClick.AddListener(() =>
                {
                    CloseModal();
                    OpenStaffContractOffer(staffCandidate, staffPhoneKey);
                });
                return;
            }
            OpenModal(english ? "THE ROOM ANSWERS" : "A SALA RESPONDE", outcome, true);
            _modalActionLabel.text = english ? "LEAVE" : "SAIR";
            _modalAction.onClick.RemoveAllListeners();
            _modalAction.onClick.AddListener(() => { CloseModal(); Refresh(); });
        }

        void PostDesk(string text)
        {
            _career.Board.Insert(0, new CoachNote
            {
                Day = _career.Calendar.WeekdayName,
                Hour = _career.Calendar.ClockText,
                When = _career.Calendar.CurrentDate,
                Text = text
            });
            while (_career.Board.Count > 40)
                _career.Board.RemoveAt(_career.Board.Count - 1);
        }

        void MaybeTrainingReport()
        {
            var now = _career.Calendar.CurrentDate;
            long last = 0;
            foreach (var line in _career.Notebook)
            {
                if (line != null && line.StartsWith("job|train-report|") && long.TryParse(line.Substring("job|train-report|".Length), out var ticks))
                    last = Math.Max(last, ticks);
            }
            if (last > 0 && now.Ticks - last < TimeSpan.FromDays(7).Ticks)
                return;
            for (var i = _career.Notebook.Count - 1; i >= 0; i--)
                if (_career.Notebook[i] != null && _career.Notebook[i].StartsWith("job|train-report|"))
                    _career.Notebook.RemoveAt(i);
            _career.Notebook.Add("job|train-report|" + now.Ticks);
            var coach = StaffReport.Find(_career.Club, StaffRole.FitnessCoach);
            var coachName = coach == null ? "Preparador físico" : coach.FullName;
            var english = LocalizationManager.Current == GameLanguage.English;
            var squad = Squad(_career.Club);
            var ranked = new List<Player>(squad);
            ranked.Sort((a, b) => WeekMark(b).CompareTo(WeekMark(a)));
            var totalMark = 0;
            var injured = 0;
            var highMarks = 0;
            var lowMarks = 0;
            var focusCounts = new Dictionary<string, int>();
            foreach (var player in squad)
            {
                var mark = WeekMark(player);
                totalMark += mark;
                if (mark >= 8) highMarks++;
                if (mark <= 4) lowMarks++;
                if (player.Injury != null && player.Injury.IsInjured) injured++;
                var focus = FocusName(player.TrainingFocus, english);
                focusCounts.TryGetValue(focus, out var count);
                focusCounts[focus] = count + 1;
            }
            var averageMark = squad.Count == 0 ? 0f : (float)totalMark / squad.Count;
            var text = "✉ " + coachName + (english ? "  ·  weekly training report" : "  ·  relatório semanal de treino");
            text += "\n" + (english ? "Squad review: " : "Resumo do plantel: ") + squad.Count
                + (english ? " players · average " : " jogadores · média ") + averageMark.ToString("0.0") + "/10"
                + (english ? " · " : " · ") + highMarks + (english ? " strong sessions · " : " sessões fortes · ")
                + lowMarks + (english ? " need follow-up." : " a acompanhar.");
            if (ranked.Count > 0)
            {
                var best = new List<string>();
                for (var i = 0; i < ranked.Count && i < 3; i++)
                    best.Add(BroadcastUi.ShortName(ranked[i].FullName) + " " + WeekMark(ranked[i]) + "/10 · " + FocusName(ranked[i].TrainingFocus, english));
                text += "\n" + (english ? "Standouts: " : "Destaques: ") + string.Join("; ", best);
                ranked.Sort((a, b) => WeekMark(a).CompareTo(WeekMark(b)));
                var followUp = new List<string>();
                for (var i = 0; i < ranked.Count && i < 3; i++)
                    followUp.Add(BroadcastUi.ShortName(ranked[i].FullName) + " " + WeekMark(ranked[i]) + "/10"
                        + (ranked[i].Fatigue >= 65 ? (english ? " · fatigue " : " · cansaço ") + ranked[i].Fatigue + "%" : "")
                        + (ranked[i].Injury != null && ranked[i].Injury.IsInjured ? (english ? " · injured" : " · lesionado") : ""));
                if (lowMarks > 0 || injured > 0)
                    text += "\n" + (english ? "Follow-up: " : "Acompanhar: ") + string.Join("; ", followUp);
            }
            var focusSummary = new List<string>();
            foreach (var pair in focusCounts)
                focusSummary.Add(pair.Key + " " + pair.Value);
            if (focusSummary.Count > 0)
                text += "\n" + (english ? "Focus distribution: " : "Focos de trabalho: ") + string.Join(" · ", focusSummary);
            text += "\n" + (english ? "Individual notes" : "Notas individuais");
            for (var i = 0; i < squad.Count; i++)
            {
                var player = squad[i];
                text += "\n" + BroadcastUi.ShortName(player.FullName) + "  " + WeekMark(player) + "/10 · "
                    + FocusName(player.TrainingFocus, english) + (english ? " · fatigue " : " · cansaço ") + player.Fatigue + "%"
                    + (player.Injury != null && player.Injury.IsInjured ? " · " + (english ? "injured" : "lesionado") : "");
            }
            text += injured == 0
                ? (english ? "\nNo player is currently out through injury." : "\nNão há jogadores atualmente afastados por lesão.")
                : "\n" + injured + (english ? " player(s) currently unavailable through injury." : " jogador(es) indisponível(is) por lesão.");
            PostDesk(text);
        }

        int WeekMark(Player player)
        {
            if (player == null)
                return 0;
            var raw = player.TrainingRating <= 0 ? 6f : player.TrainingRating / 10f;
            if (player.Fatigue > 75) raw -= 2f;
            else if (player.Fatigue > 55) raw -= 1f;
            if (player.Morale >= 70) raw += 0.5f;
            if (player.Morale < 40) raw -= 1f;
            if (player.TrainingStreak >= 3) raw += 0.5f;
            if (player.Injury != null && player.Injury.IsInjured) raw = Mathf.Min(raw, 3f);
            if (player.TrainingFocus == "rest") raw = Mathf.Min(raw, 6f);
            return Mathf.Clamp(Mathf.RoundToInt(raw), 0, 10);
        }

        static string[] FocusKeys(PlayerPosition position)
        {
            switch (position)
            {
                case PlayerPosition.Goalkeeper:
                    return new[] { "gk-handling", "gk-quickness", "gk-agility", "gk-reflex", "rest" };
                case PlayerPosition.CentreBack:
                case PlayerPosition.RightBack:
                case PlayerPosition.LeftBack:
                    return new[] { "def-marking", "def-aerial", "def-build", "def-pace", "rest" };
                case PlayerPosition.Striker:
                case PlayerPosition.RightWinger:
                case PlayerPosition.LeftWinger:
                    return new[] { "att-finish", "att-movement", "att-pace", "att-link", "rest" };
                default:
                    return new[] { "mid-pass", "mid-press", "mid-carry", "mid-arrive", "rest" };
            }
        }

        static string FocusName(string key, bool english)
        {
            switch (key)
            {
                case "gk-handling": return english ? "GK handling" : "Manejo de GR";
                case "gk-quickness": return english ? "Quickness" : "Rapidez";
                case "gk-agility": return english ? "Agility and balance" : "Agilidade e equilíbrio";
                case "gk-reflex": return english ? "Reflexes" : "Reflexos";
                case "def-marking": return english ? "Marking" : "Marcação";
                case "def-aerial": return english ? "Aerial duels" : "Jogo aéreo";
                case "def-build": return english ? "Playing out" : "Saída de bola";
                case "def-pace": return english ? "Recovery pace" : "Velocidade de recuperação";
                case "mid-pass": return english ? "Passing" : "Passe";
                case "mid-press": return english ? "Pressing" : "Pressão";
                case "mid-carry": return english ? "Carrying" : "Condução";
                case "mid-arrive": return english ? "Arriving late" : "Chegada à área";
                case "att-finish": return english ? "Finishing" : "Finalização";
                case "att-movement": return english ? "Movement" : "Movimentação";
                case "att-pace": return english ? "Pace" : "Velocidade";
                case "att-link": return english ? "Link play" : "Ligação";
                case "rest": return english ? "Rest" : "Descanso";
                default: return key ?? "";
            }
        }

        static string FocusMix(string key, bool english)
        {
            switch (key)
            {
                case "gk-handling": return english ? "half passing, half defending" : "metade passe, metade defesa";
                case "gk-quickness": return english ? "pace" : "velocidade";
                case "gk-agility": return english ? "positioning" : "posicionamento";
                case "gk-reflex": return english ? "defending" : "defesa";
                case "def-marking": return english ? "defending" : "defesa";
                case "def-aerial": return english ? "strength" : "força";
                case "def-build": return english ? "passing" : "passe";
                case "def-pace": return english ? "pace" : "velocidade";
                case "mid-pass": return english ? "passing" : "passe";
                case "mid-press": return english ? "half defending, half fitness" : "metade defesa, metade condição";
                case "mid-carry": return english ? "positioning" : "posicionamento";
                case "mid-arrive": return english ? "finishing" : "finalização";
                case "att-finish": return english ? "finishing" : "finalização";
                case "att-movement": return english ? "positioning" : "posicionamento";
                case "att-pace": return english ? "pace" : "velocidade";
                case "att-link": return english ? "half passing, half finishing" : "metade passe, metade finalização";
                case "rest": return english ? "recovery" : "recuperação";
                default: return "";
            }
        }

        void OpenFocusMenu(Player player)
        {
            if (player == null)
                return;
            if (_focusMenu != null)
            {
                var same = _focusMenu.name == "FocusMenu" + player.Id;
                Destroy(_focusMenu);
                _focusMenu = null;
                if (same)
                    return;
            }
            var english = LocalizationManager.Current == GameLanguage.English;
            var page = _trainingContent == null ? null : _trainingContent.parent;
            if (page == null)
                return;
            var menu = BroadcastUi.Panel(page, "FocusMenu" + player.Id, new Color(0.06f, 0.04f, 0.12f, 0.98f), true);
            _focusMenu = menu.gameObject;
            BroadcastUi.Place(menu.rectTransform, 0.22f, 0.28f, 0.62f, 0.78f);
            var keys = FocusKeys(player.Position);
            var title = BroadcastUi.Label(menu.transform, "Title", ProfilePosition(player.Position, english) + "   ·   " + player.FullName, 13, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.05f, 0.84f, 0.78f, 0.98f);
            var closeFocus = BroadcastUi.Button(menu.transform, "CloseFocus", "×", () =>
            {
                if (_focusMenu != null)
                    Destroy(_focusMenu);
                _focusMenu = null;
            }, new Color(1f, 1f, 1f, 0.12f), Color.white, 16);
            BroadcastUi.Place(closeFocus.GetComponent<RectTransform>(), 0.8f, 0.84f, 0.96f, 0.98f);
            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                var on = key == player.TrainingFocus;
                var y1 = 0.8f - i * 0.14f;
                var button = BroadcastUi.Button(menu.transform, "Focus" + i, FocusName(key, english) + "\n" + FocusMix(key, english), () =>
                {
                    player.TrainingFocus = key;
                    CareerStore.Save(_career);
                    if (_focusMenu != null)
                        Destroy(_focusMenu);
                    FillTraining();
                }, on ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), on ? UiTheme.ButtonText : Color.white, 12);
                var caption = button.GetComponentInChildren<Text>();
                caption.alignment = TextAnchor.MiddleLeft;
                caption.horizontalOverflow = HorizontalWrapMode.Wrap;
                caption.resizeTextForBestFit = false;
                BroadcastUi.Place(button.GetComponent<RectTransform>(), 0.05f, y1 - 0.12f, 0.95f, y1);
            }
            menu.transform.SetAsLastSibling();
        }

        static int ReadTrainingAttribute(Player player)
        {
            var attributes = player == null ? null : player.Attributes;
            if (attributes == null)
                return 10;
            switch (player.TrainingFocus)
            {
                case "finishing":
                case "att-finish":
                case "mid-arrive": return attributes.Finishing;
                case "passing":
                case "mid-pass":
                case "def-build": return attributes.Passing;
                case "pace":
                case "att-pace":
                case "def-pace":
                case "gk-quickness": return attributes.Pace;
                case "defending":
                case "def-marking":
                case "gk-reflex": return attributes.Tackling;
                case "strength":
                case "def-aerial": return attributes.Strength;
                case "positioning":
                case "gk-agility":
                case "mid-carry":
                case "att-movement": return attributes.Positioning;
                case "fitness": return attributes.Stamina;
                case "gk-handling": return (attributes.Passing + attributes.Tackling) / 2;
                case "mid-press": return (attributes.Tackling + attributes.Stamina) / 2;
                case "att-link": return (attributes.Passing + attributes.Finishing) / 2;
                default: return 10;
            }
        }

        static void RaiseTrainingAttribute(Player player)
        {
            var attributes = player == null ? null : player.Attributes;
            if (attributes == null)
                return;
            var alternate = (player.TrainingStreak & 1) == 0;
            switch (player.TrainingFocus)
            {
                case "finishing":
                case "att-finish":
                case "mid-arrive": attributes.Finishing = Mathf.Min(20, attributes.Finishing + 1); break;
                case "passing":
                case "mid-pass":
                case "def-build": attributes.Passing = Mathf.Min(20, attributes.Passing + 1); break;
                case "pace":
                case "att-pace":
                case "def-pace":
                case "gk-quickness": attributes.Pace = Mathf.Min(20, attributes.Pace + 1); break;
                case "defending":
                case "def-marking":
                case "gk-reflex": attributes.Tackling = Mathf.Min(20, attributes.Tackling + 1); break;
                case "strength":
                case "def-aerial": attributes.Strength = Mathf.Min(20, attributes.Strength + 1); break;
                case "positioning":
                case "gk-agility":
                case "mid-carry":
                case "att-movement": attributes.Positioning = Mathf.Min(20, attributes.Positioning + 1); break;
                case "fitness": attributes.Stamina = Mathf.Min(20, attributes.Stamina + 1); break;
                case "gk-handling":
                    if (alternate) attributes.Passing = Mathf.Min(20, attributes.Passing + 1);
                    else attributes.Tackling = Mathf.Min(20, attributes.Tackling + 1);
                    break;
                case "mid-press":
                    if (alternate) attributes.Tackling = Mathf.Min(20, attributes.Tackling + 1);
                    else attributes.Stamina = Mathf.Min(20, attributes.Stamina + 1);
                    break;
                case "att-link":
                    if (alternate) attributes.Passing = Mathf.Min(20, attributes.Passing + 1);
                    else attributes.Finishing = Mathf.Min(20, attributes.Finishing + 1);
                    break;
            }
        }

        static int TrainingRoll(string id, int day)
        {
            unchecked
            {
                var hash = 17;
                if (!string.IsNullOrEmpty(id))
                    for (var i = 0; i < id.Length; i++)
                        hash = hash * 31 + id[i];
                return ((hash ^ (day * 397)) & 0x7fffffff) % 100;
            }
        }

        void EnsureAlert(Transform root)
        {
            if (root == null)
                return;
            var clockCard = root.Find("ClockCard") as RectTransform;
            if (clockCard == null)
                clockCard = root.Find("Header/ClockCard") as RectTransform;
            if (clockCard == null)
                return;
            var existing = clockCard.parent.Find("Alert");
            if (existing == null)
            {
                var button = BroadcastUi.Button(clockCard.parent, "Alert", "", OpenSettingsFromClock, new Color(0.16f, 0.1f, 0.28f, 1f), Color.white, 16);
                var rect = button.GetComponent<RectTransform>();
                var width = Mathf.Clamp((clockCard.anchorMax.x - clockCard.anchorMin.x) * 0.55f, 0.04f, 0.06f);
                BroadcastUi.Place(rect, clockCard.anchorMin.x - 0.008f - width, clockCard.anchorMin.y, clockCard.anchorMin.x - 0.008f, clockCard.anchorMax.y);
                _alertLabel = button.GetComponentInChildren<Text>();
                PaintGear(button.transform);
            }
            else
            {
                var button = existing.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(OpenSettingsFromClock);
                }
                _alertLabel = existing.GetComponentInChildren<Text>();
                PaintGear(existing);
            }
            RefreshAlert();
        }

        void OpenSettingsFromClock()
        {
            if (GameRoot.Instance == null)
                return;
            GameRoot.Instance.GetComponent<AudioSettingsOverlay>()?.Toggle();
        }

        static Sprite _settingsMark;

        static Sprite SettingsMark()
        {
            if (_settingsMark != null)
                return _settingsMark;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var angle = Mathf.Atan2(dy, dx);
                    var tooth = ((angle + Mathf.PI) / (Mathf.PI * 2f) * 8f) % 1f;
                    var outer = tooth < 0.55f ? 30f : 22f;
                    var filled = distance <= outer && distance >= 10f;
                    texture.SetPixel(x, y, filled ? Color.white : Color.clear);
                }
            }
            texture.Apply();
            _settingsMark = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _settingsMark;
        }

        static void PaintGear(Transform parent)
        {
            if (parent == null)
                return;
            var previous = parent.Find("Gear");
            Image image;
            if (previous == null)
            {
                var gear = new GameObject("Gear", typeof(RectTransform), typeof(Image));
                gear.transform.SetParent(parent, false);
                image = gear.GetComponent<Image>();
                var rect = gear.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.16f, 0.16f);
                rect.anchorMax = new Vector2(0.84f, 0.84f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
                image = previous.GetComponent<Image>();
            if (image == null)
                return;
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.color = Color.white;
            image.sprite = SettingsMark();
        }

        void RefreshAlert()
        {
            if (_alertLabel != null)
                _alertLabel.text = "";
        }

        void OpenCalendar()
        {
            Show(6);
            ShowOfficeTab(3);
        }

        bool PressDutyOpen()
        {
            var next = _career == null ? null : _career.NextUserFixture();
            if (next == null)
                return false;
            var days = (next.Date.Date - _career.Calendar.CurrentDate.Date).Days;
            return days >= 0 && days <= 1 && !DutyDone("press", next.Id);
        }

        bool SquadDutyOpen()
        {
            return PendingRegistration() != null;
        }

        string PendingRegistration()
        {
            return null;
        }

        string CompetitionTitle(string id)
        {
            if (string.IsNullOrEmpty(id) || _career == null || _career.World == null)
                return "Competição";
            foreach (var league in _career.World.Leagues)
                if (league != null && league.Id == id)
                    return LocalizationManager.Translate(league.Name);
            if (id.EndsWith(":liga", StringComparison.Ordinal))
                return "Taça da Liga";
            if (id.StartsWith("cup:", StringComparison.Ordinal))
                return "Taça";
            return id;
        }

        bool PressAlreadyOpened()
        {
            var next = _career == null ? null : _career.NextUserFixture();
            return next != null && _career.Notebook != null && _career.Notebook.Contains("press-opened|" + next.Id);
        }

        void MarkPressOpened()
        {
            var next = _career == null ? null : _career.NextUserFixture();
            if (next == null || _career.Notebook == null)
                return;
            var key = "press-opened|" + next.Id;
            if (!_career.Notebook.Contains(key))
                _career.Notebook.Add(key);
        }

        bool DutyDone(string kind, string id)
        {
            return _career.Notebook.Contains("duty-done|" + kind + "|" + id);
        }

        void FinishDuty(string kind, bool assistant, bool skipped = false)
        {
            if (kind == "squad")
            {
                var competition = PendingRegistration();
                var season = TransferSeasonStart(_career.Calendar.CurrentDate);
                if (!string.IsNullOrEmpty(competition))
                {
                    var key = "duty-done|squad|" + competition + "|" + season;
                    if (!_career.Notebook.Contains(key))
                        _career.Notebook.Add(key);
                }
            }
            else
            {
                var next = _career.NextUserFixture();
                if (next == null)
                    return;
                var key = "duty-done|" + kind + "|" + next.Id;
                if (!_career.Notebook.Contains(key))
                    _career.Notebook.Add(key);
                if (kind == "press" && (assistant || skipped))
                {
                    if (_activePhoneNote != null && _activePhoneNote.ReplyGroup == "press-conference")
                        MarkPhoneAnswered(_activePhoneNote.Key);
                    var english = LocalizationManager.Current == GameLanguage.English;
                    var line = skipped
                        ? (english ? "✉ The press conference did not take place." : "✉ A conferência de imprensa não se realizou.")
                        : (english ? "✉ The assistant attended the press conference." : "✉ O adjunto foi à conferência de imprensa.");
                    PostDesk(line);
                }
                else if (kind == "press" && _activePhoneNote != null && _activePhoneNote.ReplyGroup == "press-conference")
                    MarkPhoneAnswered(_activePhoneNote.Key);
            }
            CareerStore.Save(_career);
            CloseModal();
            Refresh();
        }

        List<string> SeedRegistration()
        {
            var eleven = new List<Player>();
            var bench = new List<Player>();
            StartingShape.Resolve(Squad(_career.Club), _career.Club, eleven, bench);
            var ids = new List<string>();
            for (var i = 0; i < eleven.Count && ids.Count < 18; i++)
                if (eleven[i] != null)
                    ids.Add(eleven[i].Id);
            for (var i = 0; i < bench.Count && ids.Count < 18; i++)
                if (bench[i] != null && !ids.Contains(bench[i].Id))
                    ids.Add(bench[i].Id);
            return ids;
        }

        List<string> CompetitionIds(bool seedIfMissing)
        {
            var competition = PendingRegistration();
            var ids = new List<string>();
            if (competition == null || _career.Notebook == null)
                return seedIfMissing ? SeedRegistration() : ids;
            var prefix = "squad-reg|" + competition + "|" + TransferSeasonStart(_career.Calendar.CurrentDate) + "|";
            for (var i = 0; i < _career.Notebook.Count; i++)
            {
                var line = _career.Notebook[i];
                if (line == null || !line.StartsWith(prefix))
                    continue;
                var body = line.Substring(prefix.Length);
                if (!string.IsNullOrEmpty(body))
                {
                    var parts = body.Split(',');
                    for (var p = 0; p < parts.Length; p++)
                        if (!string.IsNullOrEmpty(parts[p]))
                            ids.Add(parts[p]);
                }
                return ids;
            }
            return seedIfMissing ? SeedRegistration() : ids;
        }

        void StoreRegistration(List<string> ids)
        {
            var competition = PendingRegistration();
            if (competition == null || _career.Notebook == null)
                return;
            var prefix = "squad-reg|" + competition + "|" + TransferSeasonStart(_career.Calendar.CurrentDate) + "|";
            for (var i = _career.Notebook.Count - 1; i >= 0; i--)
                if (_career.Notebook[i] != null && _career.Notebook[i].StartsWith(prefix))
                    _career.Notebook.RemoveAt(i);
            _career.Notebook.Add(prefix + string.Join(",", ids ?? new List<string>()));
        }

        void ToggleRegistration(string id)
        {
            var ids = CompetitionIds(true);
            var squad = Squad(_career.Club);
            if (ids.Contains(id))
                ids.Remove(id);
            else
            {
                Player adding = null;
                for (var i = 0; i < squad.Count; i++)
                    if (squad[i].Id == id)
                        adding = squad[i];
                if (adding != null && !CountsAsUnder21(adding))
                {
                    int seniors;
                    int foreign;
                    CountRegistration(ids, squad, out seniors, out foreign);
                    seniors++;
                    if (!AssociationGrown(adding))
                        foreign++;
                    if (seniors > 25 || foreign > 17)
                        return;
                }
                ids.Add(id);
            }
            StoreRegistration(ids);
            OpenUrgentDuty();
        }

        void CountRegistration(List<string> ids, List<Player> squad, out int seniors, out int foreign)
        {
            seniors = 0;
            foreign = 0;
            if (ids == null || squad == null)
                return;
            for (var i = 0; i < squad.Count; i++)
            {
                var player = squad[i];
                if (player == null || !ids.Contains(player.Id) || CountsAsUnder21(player))
                    continue;
                seniors++;
                if (!AssociationGrown(player))
                    foreign++;
            }
        }

        static bool CountsAsUnder21(Player player)
        {
            return player != null && player.Age <= 21;
        }

        bool AssociationGrown(Player player)
        {
            if (player == null || _career == null || _career.Club == null || string.IsNullOrEmpty(player.Nationality))
                return false;
            return player.Nationality == LeagueNation(_career.Club.LeagueId);
        }

        static string LeagueNation(string slug)
        {
            if (string.IsNullOrEmpty(slug))
                return "Portugal";
            if (slug.StartsWith("inglesa"))
                return "Inglaterra";
            if (slug.StartsWith("francesa"))
                return "França";
            if (slug.StartsWith("espanhola"))
                return "Espanha";
            if (slug == "bundesliga")
                return "Alemanha";
            if (slug == "eredivisie")
                return "Países Baixos";
            if (slug == "brasileira")
                return "Brasil";
            return "Portugal";
        }

        void OpenRegistrationSheet()
        {
            if (_modal == null || !_modal.activeSelf || _registrationOrder == null)
                return;
            var selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            if (selected == null || !selected.name.StartsWith("Reg"))
                return;
            int index;
            if (!int.TryParse(selected.name.Substring(3), out index) || index < 0 || index >= _registrationOrder.Count)
                return;
            OpenPlayerReturning(_registrationOrder[index], OpenUrgentDuty);
        }

        void OpenPressChoice()
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            OpenModal(english ? "PRESS CONFERENCE" : "CONFERÊNCIA DE IMPRENSA",
                english ? "You left the room. The assistant can attend, or the club can skip the conference."
                    : "Saíste da sala. O adjunto pode ir à conferência, ou o clube não comparece.",
                false);
            SizeModal(0.26f, 0.3f, 0.74f, 0.72f);
            var attend = BroadcastUi.Button(_modalBody, "AssistantGoes", english ? "THE ASSISTANT GOES" : "O ADJUNTO VAI", () => FinishDuty("press", true), UiTheme.Accent, UiTheme.ButtonText, 16);
            BroadcastUi.Preferred(attend, 48);
            var skip = BroadcastUi.Button(_modalBody, "SkipPress", english ? "NOBODY GOES" : "NÃO IR", () => FinishDuty("press", false, true), new Color(0.3f, 0.18f, 0.44f, 1f), Color.white, 16);
            BroadcastUi.Preferred(skip, 48);
            var back = BroadcastUi.Button(_modalBody, "EnterRoom", english ? "BACK INTO THE ROOM" : "VOLTAR À SALA", () =>
            {
                _enterPressRoom = true;
                OpenUrgentDuty();
            }, new Color(1f, 1f, 1f, 0.08f), Color.white, 16);
            BroadcastUi.Preferred(back, 48);
        }

        void OpenUrgentDuty()
        {
            var enterRoom = _enterPressRoom;
            _enterPressRoom = false;
            if (!enterRoom && PressDutyOpen() && PressAlreadyOpened())
            {
                OpenPressChoice();
                return;
            }
            var english = LocalizationManager.Current == GameLanguage.English;
            var press = PressDutyOpen();
            var competitionName = CompetitionTitle(PendingRegistration());
            OpenModal(press ? (english ? "PRESS CONFERENCE" : "CONFERÊNCIA DE IMPRENSA") : (english ? "REGISTER THE SQUAD" : "INSCREVER O PLANTEL"),
                press
                    ? (english ? "The press is in the room. Answer, or leave it to the assistant." : "A imprensa está na sala. Responde, ou deixa o adjunto tratar.")
                    : (english
                        ? competitionName + " · one list for the whole season, before the competition starts. 25 seniors, at most 17 without association training. Under-21s sit outside that limit."
                        : competitionName + " · uma lista para a época toda, antes de a competição começar. 25 seniores e no máximo 17 sem formação na associação. Os sub-21 ficam fora desse limite."),
                false);
            SizeModal(press ? 0.05f : 0.1f, press ? 0.04f : 0.06f, press ? 0.95f : 0.9f, press ? 0.96f : 0.94f);
            if (press)
            {
                var next = _career.NextUserFixture();
                Club opponent = null;
                if (next != null)
                    _career.World.Clubs.TryGetValue(next.OpponentId(_career.Club.Id), out opponent);
                var formation = _career.Club.Tactics == null || string.IsNullOrEmpty(_career.Club.Tactics.Formation) ? "4-3-3" : _career.Club.Tactics.Formation;
                var rival = opponent == null ? (english ? "the next opponent" : "o próximo adversário") : opponent.ShortName;
                BuildPressConference(english, rival, formation);
                return;
            }
            else
            {
                var chosen = CompetitionIds(true);
                var squad = Squad(_career.Club);
                squad.Sort((a, b) =>
                {
                    var under = (CountsAsUnder21(a) ? 1 : 0).CompareTo(CountsAsUnder21(b) ? 1 : 0);
                    if (under != 0) return under;
                    var grown = AssociationGrown(b).CompareTo(AssociationGrown(a));
                    return grown != 0 ? grown : a.Position.CompareTo(b.Position);
                });
                _registrationOrder = squad;
                int seniors;
                int foreign;
                CountRegistration(chosen, squad, out seniors, out foreign);
                AddLine((english ? "Click a card to include it. Right click or Square opens the file." : "Clica no cartão para incluir. Clique direito ou quadrado abre a ficha.") + "   ·   " + (english ? "Seniors " : "Seniores ") + seniors + "/25    ·    " + (english ? "Without association training " : "Sem formação ") + foreign + "/17");
                PaintRegistrationGrid(squad, chosen, english);
                var ready = chosen.Count >= 11 && seniors <= 25 && foreign <= 17;
                var confirm = BroadcastUi.Button(_modalBody, "ConfirmSquad", (english ? "REGISTER " : "REGISTAR ") + seniors + "/25", () =>
                {
                    var ids = CompetitionIds(true);
                    int seniorCount;
                    int foreignCount;
                    CountRegistration(ids, Squad(_career.Club), out seniorCount, out foreignCount);
                    if (ids.Count < 11 || seniorCount > 25 || foreignCount > 17)
                        return;
                    StoreRegistration(ids);
                    FinishDuty("squad", false);
                }, ready ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), ready ? UiTheme.ButtonText : Color.white, 14);
                confirm.interactable = ready;
                BroadcastUi.Preferred(confirm, 40);
            }
            var leave = BroadcastUi.Button(_modalBody, "LeaveAssistant", english ? "LEAVE IT TO THE ASSISTANT" : "DEIXAR AO ADJUNTO", () =>
            {
                if (!press)
                    StoreRegistration(SeedRegistration());
                FinishDuty(press ? "press" : "squad", true);
            }, new Color(0.3f, 0.18f, 0.44f, 1f), Color.white, 14);
            BroadcastUi.Preferred(leave, 40);
        }

        void BuildPressConference(bool english, string rival, string formation)
        {
            MarkPressOpened();
            var card = _modal.transform.Find("Card");
            var scroll = card.Find("Scroll");
            if (scroll != null)
                scroll.gameObject.SetActive(false);
            if (_modalLead != null)
                _modalLead.gameObject.SetActive(false);
            var room = BroadcastUi.Zone(card, "PressRoom", 0.03f, 0.16f, 0.97f, 0.86f);
            var reporterNation = WorldCatalog.NationalityForLeagueId(_career.Club.LeagueId);
            var reporters = new[]
            {
                FaceBook.PersonName(reporterNation, StableOfferRoll(_career.Club.LeagueId + ":press:0")),
                FaceBook.PersonName(reporterNation, StableOfferRoll(_career.Club.LeagueId + ":press:1")),
                FaceBook.PersonName(reporterNation, StableOfferRoll(_career.Club.LeagueId + ":press:2")),
                FaceBook.PersonName(reporterNation, StableOfferRoll(_career.Club.LeagueId + ":press:3")),
                FaceBook.PersonName(reporterNation, StableOfferRoll(_career.Club.LeagueId + ":press:4")),
                FaceBook.PersonName(reporterNation, StableOfferRoll(_career.Club.LeagueId + ":press:5")),
                FaceBook.PersonName(reporterNation, StableOfferRoll(_career.Club.LeagueId + ":press:6"))
            };
            var desks = english
                ? new[] { "Touchline", "Radio", "Television", "Press", "Local paper", "Football weekly", "Press" }
                : new[] { "Bancada", "Rádio", "Televisão", "Imprensa", "Jornal local", "Revista de futebol", "Imprensa" };
            var questions = english
                ? new List<string>
                {
                    "What is the tactical plan in the " + formation + " against " + rival + "?",
                    "What did you learn from the last result, and what changes this week?",
                    "Several players have had a heavy schedule. How will fitness affect selection?",
                    "Are you ready to give a young player a bigger role this season?",
                    "What kind of performance do you want the supporters to see today?",
                    "Will the club look to strengthen the squad before the window closes?"
                }
                : new List<string>
                {
                    "Qual é o plano tático no " + formation + " frente ao " + rival + "?",
                    "O que retirou do último resultado e o que muda esta semana?",
                    "Vários jogadores tiveram uma sequência exigente. A condição física vai pesar na escolha?",
                    "Está preparado para dar mais espaço a um jovem esta época?",
                    "Que exibição quer que os adeptos vejam hoje?",
                    "O clube ainda vai procurar reforços antes do fecho do mercado?"
                };
            var positive = english
                ? new List<string> { "The shape gives us a clear way to press and create chances. The players know their jobs.", "We reviewed the details, learned from them and are ready to respond.", "We will protect the group and pick a side with enough energy to execute the plan.", "If a young player earns the opportunity, age will not hold them back.", "Intensity, courage on the ball and a team that works for each other.", "We are always looking for ways to improve, but every decision has to fit the club." }
                : new List<string> { "O sistema dá-nos uma forma clara de pressionar e criar ocasiões. Cada jogador conhece a sua tarefa.", "Revimos os detalhes, aprendemos com eles e estamos prontos para responder.", "Vamos proteger o grupo e escolher uma equipa com energia para cumprir o plano.", "Se um jovem conquistar a oportunidade, a idade não o vai impedir.", "Intensidade, coragem com bola e uma equipa que trabalha em conjunto.", "Procuramos sempre melhorar, mas cada decisão tem de fazer sentido para o clube." };
            var neutral = english
                ? new List<string> { "We have a plan, but we will adapt to what happens on the pitch.", "We have moved on and are focused on the next test.", "We assess each player individually and make the final call closer to kick-off.", "The academy is part of the plan. The right moment matters more than a headline.", "A disciplined performance and good decisions in both boxes.", "We will assess the squad and the market before deciding." }
                : new List<string> { "Temos um plano, mas vamos adaptar-nos ao que acontecer no relvado.", "Seguimos em frente e estamos focados no próximo desafio.", "Avaliamos cada jogador individualmente e decidimos mais perto do apito inicial.", "A academia faz parte do plano. O momento certo vale mais do que uma manchete.", "Uma exibição disciplinada e boas decisões nas duas áreas.", "Vamos avaliar o plantel e o mercado antes de decidir." };
            var negative = english
                ? new List<string> { "The last performance was short of our standard. We have to be more decisive.", "The result was not good enough and nobody is hiding from that.", "Some players need to respond. Selection is earned, not promised.", "Potential is not enough; the young players have to show they are ready.", "I expect more from us, and the supporters are right to demand it.", "If the right player is available at the right terms, we will act." }
                : new List<string> { "A última exibição ficou abaixo do nosso padrão. Temos de ser mais decisivos.", "O resultado não chegou e ninguém se esconde disso.", "Alguns jogadores têm de responder. A titularidade conquista-se, não se promete.", "O potencial não chega; os jovens têm de mostrar que estão preparados.", "Exijo mais da equipa, e os adeptos têm razão em pedir isso.", "Se aparecer o jogador certo nas condições certas, vamos agir." };

            PaperStory recentIncident = null;
            var recentIncidentTicks = 0L;
            for (var i = 0; i < _career.Press.Count; i++)
            {
                var candidate = _career.Press[i];
                if (candidate == null || candidate.Kicker == null || !candidate.Kicker.StartsWith("PERSONAL_", StringComparison.Ordinal)
                    || candidate.DateTicks <= 0 || candidate.DateTicks > _career.Calendar.CurrentDate.Ticks)
                    continue;
                if ((_career.Calendar.CurrentDate - new DateTime(candidate.DateTicks).Date).TotalDays > 30)
                    continue;
                var marker = "press-personal-covered|" + candidate.DateTicks;
                if (_career.Notebook.Contains(marker)) continue;
                recentIncident = candidate;
                recentIncidentTicks = candidate.DateTicks;
                break;
            }
            var incidentQuestionIndex = -1;
            if (recentIncident != null)
            {
                incidentQuestionIndex = questions.Count;
                var incidentPlayerName = string.IsNullOrEmpty(recentIncident.Motm) ? (english ? "a player" : "um jogador") : recentIncident.Motm;
                questions.Add(english
                    ? "A recent personal story involving " + incidentPlayerName + " has drawn attention. What can you say to protect the player and the squad?"
                    : "Um acontecimento pessoal recente envolvendo " + incidentPlayerName + " chamou a atenção. O que pode dizer para proteger o jogador e o grupo?");
                positive.Add(english ? "The player's wellbeing comes first. We are supporting them and will respect their privacy." : "O bem-estar do jogador vem primeiro. Estamos a apoiá-lo e vamos respeitar a sua privacidade.");
                neutral.Add(english ? "We are handling it internally and will not discuss private details." : "Estamos a tratar do assunto internamente e não vamos discutir detalhes privados.");
                negative.Add(english ? "I will not discuss it. The player has to deal with the consequences." : "Não vou falar disso. O jogador tem de lidar com as consequências.");
            }
            var step = 0;
            var who = BroadcastUi.Label(room, "Who", "", 16, new Color(0.85f, 0.93f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(who.rectTransform, 0.02f, 0.88f, 0.98f, 0.98f);
            var question = BroadcastUi.Panel(room, "Question", new Color(0.16f, 0.48f, 0.86f, 1f), false);
            BroadcastUi.Place(question.rectTransform, 0.02f, 0.08f, 0.48f, 0.84f);
            var ask = BroadcastUi.Label(question.transform, "Ask", "", 18, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            ask.horizontalOverflow = HorizontalWrapMode.Wrap;
            ask.verticalOverflow = VerticalWrapMode.Truncate;
            BroadcastUi.Stretch(ask.rectTransform);
            ask.rectTransform.offsetMin = new Vector2(16f, 16f);
            ask.rectTransform.offsetMax = new Vector2(-16f, -16f);
            var tones = english ? new[] { "POSITIVE", "NEUTRAL", "NEGATIVE" } : new[] { "POSITIVO", "NEUTRO", "NEGATIVO" };
            var toneColors = new[] { new Color(0.2f, 0.72f, 0.38f, 1f), new Color(0.75f, 0.78f, 0.86f, 1f), new Color(0.9f, 0.35f, 0.32f, 1f) };
            var captions = new Text[3];
            Action show = null;
            show = () =>
            {
                who.text = reporters[step] + "   ·   " + desks[step] + "      " + (step + 1) + "/" + questions.Count;
                ask.text = questions[step];
                captions[0].text = positive[step];
                captions[1].text = neutral[step];
                captions[2].text = negative[step];
            };
            for (var i = 0; i < 3; i++)
            {
                var y1 = 0.84f - i * 0.26f;
                var toneIndex = i;
                var tone = BroadcastUi.Label(room, "Tone" + i, tones[i], 12, toneColors[i], TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(tone.rectTransform, 0.52f, y1 - 0.06f, 0.98f, y1);
                var button = BroadcastUi.Button(room, "Press" + i, "", () =>
                {
                    if (step == incidentQuestionIndex && recentIncident != null)
                        ApplyPressIncidentResponse(recentIncident, recentIncidentTicks, toneIndex);
                    step++;
                    if (step >= questions.Count)
                        FinishDuty("press", false);
                    else
                        show();
                }, new Color(1f, 1f, 1f, 0.08f), Color.white, 15);
                captions[i] = button.GetComponentInChildren<Text>();
                captions[i].alignment = TextAnchor.MiddleLeft;
                captions[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                captions[i].verticalOverflow = VerticalWrapMode.Truncate;
                BroadcastUi.Place(button.GetComponent<RectTransform>(), 0.52f, y1 - 0.22f, 0.98f, y1 - 0.07f);
            }
            show();
            if (_modalAction == null)
                return;
            _modalAction.gameObject.SetActive(true);
            BroadcastUi.Place(_modalAction.GetComponent<RectTransform>(), 0.16f, 0.03f, 0.84f, 0.13f);
            _modalActionLabel.text = english ? "LEAVE IT TO THE ASSISTANT" : "DEIXAR AO ADJUNTO";
            _modalAction.onClick.RemoveAllListeners();
            _modalAction.onClick.AddListener(() => FinishDuty("press", true));
        }

        void ApplyPressIncidentResponse(PaperStory story, long storyTicks, int tone)
        {
            if (story == null || _career == null || storyTicks <= 0)
                return;
            var marker = "press-personal-covered|" + storyTicks;
            if (_career.Notebook.Contains(marker))
                return;
            _career.Notebook.Add(marker);
            foreach (var player in _career.World.Players.Values)
            {
                if (player == null || player.FullName != story.Motm)
                    continue;
                player.Morale = Mathf.Clamp(player.Morale + (tone == 0 ? 4 : tone == 1 ? 1 : -5), 0, 100);
                break;
            }
            if (tone == 2)
                _career.Club.DressingRoomTension = Mathf.Clamp(_career.Club.DressingRoomTension + 2, 0, 100);
            else if (tone == 0)
                _career.SupporterApproval = Mathf.Clamp(_career.SupporterApproval + 1, 0, 100);
        }

        static void AddDutyMark(Transform row)
        {
            var mark = new GameObject("DutyMark", typeof(RectTransform), typeof(CanvasRenderer), typeof(DutyAlertMark));
            mark.transform.SetParent(row, false);
            var rect = mark.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.9f, 0.62f);
            rect.anchorMax = new Vector2(0.98f, 0.96f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            mark.GetComponent<DutyAlertMark>().raycastTarget = false;
            var bang = BroadcastUi.Label(row, "Bang", "!", 13, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            bang.raycastTarget = false;
            BroadcastUi.Place(bang.rectTransform, 0.9f, 0.64f, 0.98f, 0.94f);
        }

        static void PassClicksTo(Transform row)
        {
            if (row == null)
                return;
            var graphics = row.GetComponentsInChildren<Graphic>(true);
            for (var i = 0; i < graphics.Length; i++)
                if (graphics[i].transform != row)
                    graphics[i].raycastTarget = false;
        }

        static void AddInboxCrest(Transform parent, Club club, float x)
        {
            if (parent == null || club == null)
                return;
            var badge = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(parent, false);
            BroadcastUi.Place(badge.GetComponent<RectTransform>(), x, 0.12f, x + 0.12f, 0.88f);
            var image = badge.GetComponent<Image>();
            image.sprite = CrestStudio.For(club);
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        void OpenAssistantLineup(PhoneNote note)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            OpenModal("Adjunto", note == null ? "" : note.Text, false);
            var card = _modal.transform.Find("Card");
            var scroll = card.Find("Scroll");
            if (scroll != null)
                scroll.gameObject.SetActive(false);
            var pitch = BroadcastUi.Zone(card, "SuggestedXi", 0.3f, 0.2f, 0.7f, 0.78f);
            BroadcastUi.DrawVerticalPitch(pitch, new Color(0.1f, 0.42f, 0.2f));
            var formation = _career.Club.Tactics == null || string.IsNullOrEmpty(_career.Club.Tactics.Formation) ? "4-3-3" : _career.Club.Tactics.Formation;
            var ids = note == null || string.IsNullOrEmpty(note.Meta) ? new string[0] : note.Meta.Split(',');
            var color = BroadcastUi.Parse(_career.Club.JerseyPrimaryHex, Color.white);
            for (var i = 0; i < ids.Length && i < 11; i++)
            {
                var id = ids[i].Trim();
                if (!_career.World.Players.TryGetValue(id, out var player))
                    continue;
                var slot = MatchEngine.Slot(true, i, formation);
                var dot = BroadcastUi.Solid(pitch, "P" + i, color);
                dot.sprite = BroadcastUi.Circle;
                dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(slot.x, slot.y);
                dot.rectTransform.sizeDelta = new Vector2(28f, 28f);
                var number = BroadcastUi.Label(dot.rectTransform, "Num", player.SquadNumber.ToString(), 12, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Stretch(number.rectTransform);
                var parts = player.FullName.Split(' ');
                var surname = parts.Length == 0 ? player.FullName : parts[parts.Length - 1];
                var name = BroadcastUi.Label(pitch, "N" + i, surname, 11, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                name.rectTransform.anchorMin = new Vector2(slot.x - 0.16f, slot.y + 0.04f);
                name.rectTransform.anchorMax = new Vector2(slot.x + 0.16f, slot.y + 0.1f);
                name.rectTransform.offsetMin = Vector2.zero;
                name.rectTransform.offsetMax = Vector2.zero;
            }
            if (_modalLead != null)
            {
                _modalLead.text = (english ? "The assistant leaves this eleven. Follow the suggestion to put these players in the squad." : "O adjunto deixa este onze. Seguir a sugestão mete estes jogadores no plantel.") + "\n" + formation;
                _modalLead.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            if (_modalAction == null)
                return;
            _modalAction.gameObject.SetActive(true);
            _modalActionLabel.text = english ? "FOLLOW THE SUGGESTION" : "SEGUIR SUGESTÃO";
            _modalAction.onClick.RemoveAllListeners();
            var meta = note == null ? "" : note.Meta;
            _modalAction.onClick.AddListener(() =>
            {
                ApplySuggestedXi(meta);
                CloseModal();
                Show(3);
            });
        }

        void ApplySuggestedXi(string meta)
        {
            if (_career == null || _career.Club == null || string.IsNullOrEmpty(meta))
                return;
            var lineup = new List<string>();
            var pieces = meta.Split(',');
            for (var i = 0; i < pieces.Length; i++)
            {
                var id = pieces[i].Trim();
                if (id.Length > 0 && _career.World.Players.ContainsKey(id) && !lineup.Contains(id))
                    lineup.Add(id);
            }
            if (lineup.Count != 11)
                return;
            _career.Club.ManualLineup = lineup;
            var bench = new List<string>();
            foreach (var player in Squad(_career.Club))
                if (player != null && !lineup.Contains(player.Id))
                    bench.Add(player.Id);
            _career.Club.ManualBenchOrder = bench;
            CareerStore.Save(_career);
        }

        void OpenDeskMessage(PhoneNote note)
        {
            if (note == null)
                return;
            if (note.Channel == "press" && note.ReplyGroup == "press-conference")
            {
                _activePhoneNote = note;
                OpenModal(note.Who, note.Text, false);
                SizeModal(0.28f, 0.3f, 0.72f, 0.72f);
                var english = LocalizationManager.Current == GameLanguage.English;
                var attend = BroadcastUi.Button(_modalBody, "AttendConference", english ? "ATTEND THE CONFERENCE" : "IR À CONFERÊNCIA",
                    () =>
                    {
                        _enterPressRoom = true;
                        OpenUrgentDuty();
                    }, UiTheme.Accent, UiTheme.ButtonText, 14);
                BroadcastUi.Preferred(attend, 48);
                var assistant = BroadcastUi.Button(_modalBody, "SendAssistant", english ? "SEND THE ASSISTANT" : "ENVIAR O ADJUNTO",
                    () => FinishDuty("press", true), new Color(0.3f, 0.18f, 0.44f, 1f), Color.white, 14);
                BroadcastUi.Preferred(assistant, 48);
                var skip = BroadcastUi.Button(_modalBody, "SkipConference", english ? "DO NOT ATTEND" : "NÃO IR",
                    () => FinishDuty("press", false, true), new Color(1f, 1f, 1f, 0.08f), Color.white, 14);
                BroadcastUi.Preferred(skip, 48);
                return;
            }
            if (note.Channel == "urgente")
            {
                var aboutPress = note.Text != null && (note.Text.IndexOf("conferência", StringComparison.OrdinalIgnoreCase) >= 0 || note.Text.IndexOf("press conference", StringComparison.OrdinalIgnoreCase) >= 0);
                if (aboutPress && PressDutyOpen() && PressAlreadyOpened())
                    OpenPressChoice();
                else
                    OpenUrgentDuty();
                return;
            }
            if (note.Channel == "adjunto-onze")
            {
                OpenAssistantLineup(note);
                return;
            }
            OpenModal(note.Who, note.Text, false);
            FitMessageModal(note.Text);
            if (_modalLead != null)
            {
                _modalLead.horizontalOverflow = HorizontalWrapMode.Wrap;
                _modalLead.verticalOverflow = VerticalWrapMode.Truncate;
                _modalLead.resizeTextForBestFit = true;
                _modalLead.resizeTextMinSize = 13;
                _modalLead.resizeTextMaxSize = 18;
                BroadcastUi.Place(_modalLead.rectTransform, 0.08f, 0.28f, 0.92f, 0.78f);
            }
            if (_modalTitle != null)
                BroadcastUi.Place(_modalTitle.rectTransform, 0.06f, 0.8f, 0.78f, 0.94f);
            if (_modalClose != null)
            {
                var closeLabel = _modalClose.GetComponentInChildren<Text>();
                if (closeLabel != null)
                {
                    closeLabel.fontSize = 20;
                    closeLabel.alignment = TextAnchor.MiddleCenter;
                    closeLabel.rectTransform.offsetMin = Vector2.zero;
                    closeLabel.rectTransform.offsetMax = Vector2.zero;
                }
                BroadcastUi.Place(_modalClose.GetComponent<RectTransform>(), 0.8f, 0.78f, 0.94f, 0.94f);
            }
            if (_modalAction == null)
                return;
            string label = null;
            Action go = null;
            if (note.Meta != null && note.Meta.StartsWith("jogador:", StringComparison.Ordinal))
            {
                var playerId = note.Meta.Substring("jogador:".Length);
                label = "VER FICHA";
                go = () =>
                {
                    CloseModal();
                    if (_career.World.Players.TryGetValue(playerId, out var player))
                        OpenPlayer(player);
                };
            }
            else if (note.Meta != null && note.Meta.StartsWith("clube:", StringComparison.Ordinal))
            {
                var clubId = note.Meta.Substring("clube:".Length);
                label = "VER CLUBE";
                go = () =>
                {
                    CloseModal();
                    if (_career.World.Clubs.TryGetValue(clubId, out var scoutedClub))
                        OpenClubDossier(scoutedClub, 3);
                };
            }
            else if (note.Text != null && note.Text.IndexOf("reunião sobre a verba", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                label = "ENTRAR NA REUNIÃO";
                go = () =>
                {
                    CloseModal();
                    OpenBoardMeeting(null, 0, 0, 0, 0);
                };
            }
            else if (note.Channel == "calendario")
            {
                label = "VER CALENDÁRIO";
                go = () => { CloseModal(); OpenCalendar(); };
            }
            else if (note.Channel == "jornal")
            {
                label = "VER NOTÍCIAS";
                go = () => { CloseModal(); Show(1); };
            }
            else if (note.Channel == "clube" && !string.IsNullOrEmpty(note.ClubId) && _career.World.Clubs.TryGetValue(note.ClubId, out var club))
            {
                label = "VER CLUBE";
                go = () => { CloseModal(); OpenClubDossier(club); };
            }
            else
            {
                label = "FECHAR";
                go = CloseModal;
            }
            _modalAction.gameObject.SetActive(true);
            _modalActionLabel.text = label;
            _modalAction.onClick.RemoveAllListeners();
            _modalAction.onClick.AddListener(() => go());
        }

        void EnsureLandmarkMemories()
        {
            Fixture best = null;
            Fixture worst = null;
            var bestMargin = 0;
            var worstMargin = 0;
            foreach (var fixture in _career.Fixtures)
            {
                if (!fixture.Played || fixture.Walkover || fixture.HomeClubId == fixture.AwayClubId || !fixture.Involves(_career.Club.Id))
                    continue;
                var home = fixture.IsHome(_career.Club.Id);
                var goalsFor = home ? fixture.HomeGoals : fixture.AwayGoals;
                var goalsAgainst = home ? fixture.AwayGoals : fixture.HomeGoals;
                if (goalsFor > goalsAgainst && goalsFor - goalsAgainst > bestMargin)
                {
                    bestMargin = goalsFor - goalsAgainst;
                    best = fixture;
                }
                if (goalsAgainst > goalsFor && goalsAgainst - goalsFor > worstMargin)
                {
                    worstMargin = goalsAgainst - goalsFor;
                    worst = fixture;
                }
            }
            var changed = RememberLandmark(best, true, bestMargin);
            if (RememberLandmark(worst, false, worstMargin))
                changed = true;
            if (changed)
                CareerStore.Save(_career);
        }

        bool RememberLandmark(Fixture fixture, bool win, int margin)
        {
            if (fixture == null || margin <= 0)
                return false;
            var kind = win ? "melhor-jogo" : "pior-jogo";
            var home = fixture.IsHome(_career.Club.Id);
            var goalsFor = home ? fixture.HomeGoals : fixture.AwayGoals;
            var goalsAgainst = home ? fixture.AwayGoals : fixture.HomeGoals;
            var opponentId = fixture.OpponentId(_career.Club.Id);
            var opponent = _career.World.Clubs.TryGetValue(opponentId, out var club) ? club.ShortName : opponentId;
            var score = goalsFor + "–" + goalsAgainst;
            var title = (win ? "Melhor jogo: " : "Pior jogo: ") + score + " frente a " + opponent;
            FootballMemory existing = null;
            foreach (var memory in _career.Memories)
                if (memory != null && memory.Kind == kind)
                    existing = memory;
            if (existing != null && existing.Title == title)
                return false;
            if (existing != null)
                _career.Memories.Remove(existing);
            CareerMemories.Record(_career, new FootballMemory
            {
                WhenTicks = fixture.Date.Ticks,
                Importance = 5,
                Kind = kind,
                ClubId = _career.Club.Id,
                CompetitionId = fixture.LeagueId,
                RelatedClubId = opponentId,
                Title = title,
                Detail = LocalizationManager.FormatDate(fixture.Date, "d MMM yyyy") + " · " + (win ? "A maior vantagem da carreira" : "A derrota mais pesada da carreira") + ": " + score + " contra " + opponent + "."
            });
            return true;
        }

        void FitClubSheet()
        {
            var scroll = _modal == null ? null : _modal.transform.Find("Card/Scroll") as RectTransform;
            if (scroll == null || _modalBody == null)
                return;
            var scrollRect = scroll.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.vertical = true;
                scrollRect.horizontal = false;
            }
            var fitter = _modalBody.GetComponent<ContentSizeFitter>();
            if (fitter != null)
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var body = _modalBody as RectTransform;
            body.anchorMin = new Vector2(0f, 1f);
            body.anchorMax = new Vector2(1f, 1f);
            body.pivot = new Vector2(0.5f, 1f);
            body.offsetMin = Vector2.zero;
            body.offsetMax = Vector2.zero;
            var layout = _modalBody.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
            }
            var names = new[] { "ClubOverviewGrid", "ClubSquadList", "ClubSquadPitch", "YouthClubPitch", "ClubAnalysisLocked", "ClubPerformanceDashboard", "ClubHistoryDashboard" };
            for (var i = 0; i < names.Length; i++)
            {
                var panel = _modalBody.Find(names[i]);
                if (panel == null)
                    continue;
                var element = panel.GetComponent<LayoutElement>() ?? panel.gameObject.AddComponent<LayoutElement>();
                element.flexibleHeight = 0f;
                element.minHeight = panel.name == "ClubSquadList" ? 460f : 180f;
                if (panel.name == "ClubSquadList")
                    element.preferredHeight = 0f;
                else if (element.preferredHeight > 360f)
                    element.preferredHeight = 280f;
            }
            Canvas.ForceUpdateCanvases();
            var pitch = _modalBody.Find("ClubSquadPitch/VerticalHalfPitch") as RectTransform
                ?? _modalBody.Find("YouthClubPitch/VerticalHalfPitch") as RectTransform;
            if (pitch == null)
                return;
            var size = pitch.rect.size;
            if (size.x < 40f || size.y < 40f)
                return;
            var cardWidth = Mathf.Clamp(size.x * 0.15f, 68f, 112f);
            var cardHeight = Mathf.Clamp(size.y * 0.15f, 58f, 96f);
            for (var i = 0; i < pitch.childCount; i++)
            {
                var child = pitch.GetChild(i) as RectTransform;
                if (child == null || child.name.Length < 2 || child.name[0] != 'P' || !char.IsDigit(child.name[1]))
                    continue;
                child.sizeDelta = new Vector2(cardWidth, cardHeight);
            }
        }

        void OpenDutyModal(Player player)
        {
            var duty = string.IsNullOrEmpty(player.Duty) ? "Equilibrar" : player.Duty;
            OpenModal(player.FullName, PresentationLabels.Position(player.Position) + "  ·  instrução actual: " + duty, false);
            _modalLineSize = 17;
            var face = FaceImage(_modal.transform.Find("Card"), player, 72f);
            face.rectTransform.anchorMin = face.rectTransform.anchorMax = new Vector2(0.07f, 0.88f);
            BroadcastUi.Place(_modalTitle.rectTransform, 0.16f, 0.9f, 0.96f, 0.98f);
            BroadcastUi.Place(_modalLead.rectTransform, 0.16f, 0.78f, 0.96f, 0.89f);
            AddLine("Escolhe a ordem individual para esta posição. Só aparecem opções que o adjunto considera válidas.");
            var options = IndividualOrders.ForPosition(player.Position);
            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var on = option == duty;
                var row = BroadcastUi.Button(_modalBody, "Duty" + i, option, () =>
                {
                    player.Duty = option;
                    CloseModal();
                    FillTeam();
                }, on ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), on ? UiTheme.ButtonText : Color.white, 17);
                BroadcastUi.Preferred(row.GetComponent<RectTransform>(), 44);
            }

            var profile = BroadcastUi.Button(_modalBody, "Profile", "VER FICHA COMPLETA", () => OpenPlayer(player), new Color(1f, 1f, 1f, 0.1f), Color.white, 16);
            BroadcastUi.Preferred(profile.GetComponent<RectTransform>(), 44);
        }

        void ReopenClubSheet()
        {
            if (_dossierClub != null)
                OpenClubDossier(_dossierClub, _dossierTab, _dossierYouth, _dossierBack);
        }

        void OpenPlayerReturning(Player player, Action back)
        {
            _sheetReturn = back;
            OpenPlayer(player);
        }

        void PaintRegistrationGrid(List<Player> squad, List<string> chosen, bool english)
        {
            var columns = 6;
            var rows = Mathf.Max(1, Mathf.CeilToInt(squad.Count / (float)columns));
            var grid = BroadcastUi.Panel(_modalBody, "RegGrid", new Color(0.012f, 0.025f, 0.04f, 0.96f));
            BroadcastUi.Preferred(grid, rows * 84f);
            for (var i = 0; i < squad.Count; i++)
            {
                var player = squad[i];
                var id = player.Id;
                var captured = player;
                var on = chosen.Contains(player.Id);
                var col = i % columns;
                var rowIndex = i / columns;
                var x0 = col / (float)columns;
                var x1 = (col + 1) / (float)columns;
                var y1 = 1f - rowIndex / (float)rows;
                var y0 = y1 - 1f / rows;
                var card = BroadcastUi.Button(grid.transform, "Reg" + i, "", () => ToggleRegistration(id),
                    on ? new Color(0.05f, 0.28f, 0.34f, 1f) : new Color(0.07f, 0.08f, 0.12f, 1f), Color.white, 11);
                BroadcastUi.Place(card.GetComponent<RectTransform>(), x0 + 0.008f, y0 + 0.02f, x1 - 0.008f, y1 - 0.02f);
                var sheetClick = card.gameObject.AddComponent<SquadSheetClick>();
                sheetClick.Bind(() => OpenPlayerReturning(captured, OpenUrgentDuty));
                var faceGo = new GameObject("Face", typeof(RectTransform), typeof(Image));
                faceGo.transform.SetParent(card.transform, false);
                var face = faceGo.GetComponent<Image>();
                face.sprite = FaceBook.For(player);
                face.preserveAspect = true;
                face.raycastTarget = false;
                BroadcastUi.Place(face.rectTransform, 0.08f, 0.42f, 0.42f, 0.94f);
                var mark = BroadcastUi.Label(card.transform, "Mark", on ? "✓" : "", 16, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                mark.raycastTarget = false;
                BroadcastUi.Place(mark.rectTransform, 0.62f, 0.72f, 0.96f, 0.96f);
                var bits = string.IsNullOrEmpty(player.FullName) ? new[] { "—" } : player.FullName.Split(' ');
                var surname = bits[bits.Length - 1];
                var name = BroadcastUi.Label(card.transform, "Name", surname, 11, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                name.raycastTarget = false;
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 8;
                name.resizeTextMaxSize = 12;
                BroadcastUi.Place(name.rectTransform, 0.04f, 0.2f, 0.96f, 0.42f);
                var tag = CountsAsUnder21(player) ? "SUB-21" : AssociationGrown(player) ? (english ? "TRAINED" : "FORMADO") : (english ? "FOREIGN" : "SEM FORM.");
                var meta = BroadcastUi.Label(card.transform, "Meta", ProfilePosition(player.Position, english) + "  ·  " + tag, 9, on ? Color.white : UiTheme.Muted, TextAnchor.MiddleCenter);
                meta.raycastTarget = false;
                meta.resizeTextForBestFit = true;
                meta.resizeTextMinSize = 7;
                meta.resizeTextMaxSize = 9;
                BroadcastUi.Place(meta.rectTransform, 0.04f, 0.04f, 0.96f, 0.22f);
            }
        }

        void OpenPlayer(Player player, bool orders = false)
        {
            if (player == null)
                return;
            var back = _sheetReturn;
            _sheetReturn = null;
            PlayerPositionProfile.Ensure(player);
            _modalLineSize = 16;
            var keeper = player.Position == PlayerPosition.Goalkeeper;
            var reader = StaffReport.Find(_career.Club, keeper ? StaffRole.GoalkeeperCoach : StaffRole.AssistantCoach);
            var fitness = StaffReport.Find(_career.Club, StaffRole.FitnessCoach);
            OpenModal(player.FullName, "", false);
            if (!orders && back != null)
                _dossierReturnAction = back;
            if (!orders)
            {
                _modalTitle.gameObject.SetActive(false);
                _modalLead.gameObject.SetActive(false);
            }
            _profileModalPlayer = player;
            _profileModalOrders = orders;
            var cardRoot = _modal.transform.Find("Card");
            if (orders)
            {
                var frameGo = new GameObject("Portrait", typeof(RectTransform));
                var frame = frameGo.GetComponent<RectTransform>();
                frame.SetParent(cardRoot, false);
                BroadcastUi.Place(frame, 0.025f, 0.7f, 0.2f, 0.98f);
                var face = FaceImage(frame, player, 205f);
                face.preserveAspect = true;
                BroadcastUi.Place(face.rectTransform, 0.025f, 0.02f, 0.975f, 0.98f);
                frame.SetAsFirstSibling();
                BroadcastUi.Place(_modalTitle.rectTransform, 0.21f, 0.9f, 0.86f, 0.98f);
                BroadcastUi.Place(_modalLead.rectTransform, 0.255f, 0.74f, 0.87f, 0.89f);
                AddHead("Instrução individual");
                var duty = string.IsNullOrEmpty(player.Duty) ? "Equilibrar" : player.Duty;
                var options = IndividualOrders.ForPosition(player.Position);
                for (var i = 0; i < options.Length; i++)
                {
                    var option = options[i];
                    var on = option == duty;
                    var row = BroadcastUi.Button(_modalBody, "Duty" + i, option, () =>
                    {
                        player.Duty = option;
                        OpenPlayer(player, true);
                    }, on ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), on ? UiTheme.ButtonText : Color.white, 16);
                    BroadcastUi.Preferred(row.GetComponent<RectTransform>(), 40);
                }
                return;
            }

            PreparePlayerSheet();
            BuildPlayerSheet(player, keeper, fitness, reader);
        }

        void PreparePlayerSheet()
        {
            var scrollRect = _modal.transform.Find("Card/Scroll");
            if (scrollRect != null)
            {
                BroadcastUi.Place(scrollRect as RectTransform, 0.02f, 0.02f, 0.98f, 0.88f);
                var scroll = scrollRect.GetComponent<ScrollRect>();
                if (scroll != null)
                {
                    scroll.horizontal = false;
                    scroll.vertical = false;
                }
            }

            var layout = _modalBody.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
                layout.enabled = false;
            var fitter = _modalBody.GetComponent<ContentSizeFitter>();
            if (fitter != null)
                fitter.enabled = false;
            var bodyRect = _modalBody as RectTransform;
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.pivot = new Vector2(0.5f, 0.5f);
            bodyRect.offsetMin = Vector2.zero;
            bodyRect.offsetMax = Vector2.zero;
        }

        void BuildPlayerSheet(Player player, bool keeper, StaffMember fitness, StaffMember reader)
        {
            const float mainBottom = 0.32f;
            const float mainTop = 0.66f;
            var english = LocalizationManager.Current == GameLanguage.English;
            var own = _career.Club.PlayerIds.Contains(player.Id);
            var academyOwned = player.ClubId == _career.Club.Id && _career.AcademyPlayerIds.Contains(player.Id);
            var scouted = own || academyOwned || _career.ScoutedPlayers.Contains(player.Id);
            var rating = PlayerAttributes.CurrentOverallAtPosition(player, player.Position);
            var baseline = PlayerAttributes.OverallAtPosition(player, player.Position);
            var delta = rating - baseline;
            _keyboardScoutBack = null;

            var header = ProfilePanel("Identity", 0.005f, scouted ? 0.68f : 0.8f, 0.995f, 0.995f);
            BroadcastUi.ApplySandPaper(header.GetComponent<Image>(), 0.1f);
            var portrait = new GameObject("Portrait", typeof(RectTransform)).GetComponent<RectTransform>();
            portrait.SetParent(header.transform, false);
            BroadcastUi.Place(portrait, 0.015f, 0.28f, 0.2f, 0.99f);
            var face = FaceImage(portrait, player, 120f);
            face.preserveAspect = true;
            BroadcastUi.Stretch(face.rectTransform);

            var flag = BroadcastUi.Solid(header.transform, "CountryFlag", Color.white);
            flag.sprite = FlagStudio.For(player.Nationality);
            flag.preserveAspect = true;
            flag.raycastTarget = false;
            BroadcastUi.Place(flag.rectTransform, 0.205f, 0.65f, 0.23f, 0.88f);
            var positionLabel = ProfileText(header.transform, "Position", "#" + player.SquadNumber + "  ·  " + ProfilePosition(player.Position, english) + (player.IsCaptain ? (english ? "  ·  CAPTAIN" : "  ·  CAPITÃO") : player.IsViceCaptain ? (english ? "  ·  VICE-CAPTAIN" : "  ·  SUBCAPITÃO") : ""), 15, UiTheme.Accent, FontStyle.Bold, 0.24f, 0.62f, 0.43f, 0.79f);
            var profileName = ProfileText(header.transform, "PlayerName", player.FullName, 22, Color.white, FontStyle.Bold, 0.24f, 0.81f, 0.43f, 0.98f);
            profileName.resizeTextForBestFit = true;
            profileName.resizeTextMinSize = 14;
            profileName.resizeTextMaxSize = 22;
            var clubShown = _career.World.Clubs.TryGetValue(player.ClubId, out var club) ? club.Name : (english ? "Unknown club" : "Clube desconhecido");
            if (scouted)
                clubShown += "  ·  " + player.Age + (english ? " years" : " anos");
            var clubLabel = ProfileText(header.transform, "Club", clubShown, 13, Color.white, FontStyle.Bold, 0.24f, 0.46f, 0.43f, 0.6f);
            var positions = new List<PlayerPosition> { player.Position };
            for (var i = 0; i < player.AlternatePositions.Count; i++)
                positions.Add(player.AlternatePositions[i]);
            var positionNames = new List<string>();
            var positionRatings = new List<string>();
            foreach (var position in positions)
            {
                var label = ProfilePosition(position, english);
                positionNames.Add(label);
                positionRatings.Add(label + " " + PlayerAttributes.CurrentOverallAtPosition(player, position));
            }
            if (scouted)
            {
                var positionDetails = (english ? "POSITIONS  " : "POSIÇÕES  ") + string.Join("  ·  ", positionNames)
                    + "\n" + string.Join("   ", positionRatings);
                ProfileText(header.transform, "AltPositions", positionDetails, 12, UiTheme.Muted, FontStyle.Normal, 0.24f, 0.08f, 0.43f, 0.46f);
            }
            if (scouted)
                PaintRolePitch(header.transform, player, 0.02f, 0.025f, 0.19f, 0.25f);

            if (!scouted)
            {
                var locked = BroadcastUi.Panel(_modalBody, "ScoutingRequired", new Color(0.015f, 0.035f, 0.05f, 1f));
                BroadcastUi.ApplySandPaper(locked, 0.1f);
                BroadcastUi.Place(locked.rectTransform, 0.005f, 0.015f, 0.995f, 0.78f);
                ProfileText(locked.transform, "Title", english ? "SCOUTING REPORT REQUIRED" : "É NECESSÁRIO UM RELATÓRIO DO OLHEIRO", 20, UiTheme.Accent, FontStyle.Bold, 0.05f, 0.72f, 0.95f, 0.9f, TextAnchor.MiddleCenter);
                ProfileText(locked.transform, "Body", english
                    ? "Detailed attributes, traits, personality, market value and contract terms will be revealed when the scout finishes the report."
                    : "Os atributos, traços, personalidade, valor de mercado e contrato só serão revelados quando o olheiro terminar o relatório.",
                    15, Color.white, FontStyle.Normal, 0.12f, 0.48f, 0.88f, 0.68f, TextAnchor.MiddleCenter);
                var job = ScoutJobFor(player.Id);
                ProfileText(locked.transform, "Status", job == null
                    ? (english ? "No report has been requested." : "Ainda não foi pedido nenhum relatório.")
                    : (english ? "The scout is working on this report." : "O olheiro está a preparar o relatório."),
                    14, UiTheme.Muted, FontStyle.Bold, 0.12f, 0.34f, 0.88f, 0.44f, TextAnchor.MiddleCenter);

                _keyboardScoutBack = BroadcastUi.Button(locked.transform, "KeyboardBack", english ? "BACK" : "VOLTAR", CloseModal,
                    new Color(1f, 1f, 1f, 0.12f), Color.white, 13);
                BroadcastUi.Place(_keyboardScoutBack.GetComponent<RectTransform>(), 0.05f, 0.08f, 0.27f, 0.23f);
                _keyboardScoutBack.gameObject.SetActive(ControlChrome.UsingKeyboard);

                var reportButton = BroadcastUi.Button(locked.transform, "RequestScoutReport", job == null
                    ? (english ? "REQUEST REPORT" : "PEDIR RELATÓRIO")
                    : (english ? "SCOUTING IN PROGRESS" : "RELATÓRIO EM CURSO"),
                    job == null ? (Action)(() => { var back = _dossierReturnAction; AskScout(player); _sheetReturn = back; OpenPlayer(player); }) : null,
                    UiTheme.Accent, UiTheme.ButtonText, 14);
                reportButton.interactable = job == null;
                BroadcastUi.Place(reportButton.GetComponent<RectTransform>(), ControlChrome.UsingKeyboard ? 0.55f : 0.2f, 0.08f, 0.95f, 0.23f);
                return;
            }

            var overallCard = ProfilePanelAt(header.transform, "Overall", new Color(0.04f, 0.09f, 0.12f, 1f), 0.445f, 0.08f, 0.555f, 0.92f);
            ProfileText(overallCard.transform, "Value", rating.ToString(), 38, Color.white, FontStyle.Bold, 0.04f, 0.39f, 0.96f, 0.91f, TextAnchor.MiddleCenter);
            ProfileText(overallCard.transform, "Label", english ? "CURRENT OVR" : "OVR ATUAL", 9, UiTheme.Accent, FontStyle.Bold, 0.02f, 0.08f, 0.98f, 0.34f, TextAnchor.MiddleCenter);
            if (scouted)
            {
                ProfileText(header.transform, "BaseOVR", (english ? "BASE " : "BASE ") + baseline + (english ? "   ·   FORM " : "   ·   FORMA ") + (delta >= 0 ? "+" : "") + delta, 12, UiTheme.Muted, FontStyle.Bold, 0.57f, 0.72f, 0.78f, 0.94f);
                ProfileText(header.transform, "Potential", LocalizationManager.Translate(PlayerTraits.HiddenPotential(player, true)), 11, UiTheme.AccentSoft, FontStyle.Normal, 0.57f, 0.47f, 0.78f, 0.7f);
                ProfileText(header.transform, "MarketValue", (english ? "MARKET VALUE\n" : "VALOR DE MERCADO\n") + Euro(MarketValue(player)), 12, Color.white, FontStyle.Bold, 0.57f, 0.27f, 0.78f, 0.46f);
                ProfileText(header.transform, "ReputationStars", (english ? "REPUTATION  " : "REPUTAÇÃO  ") + ReputationStars(player.Reputation), 14, new Color(1f, 0.78f, 0.15f), FontStyle.Bold, 0.57f, 0.08f, 0.78f, 0.25f);
            }
            else
                ProfileText(header.transform, "MarketValue", (english ? "MARKET VALUE\n" : "VALOR DE MERCADO\n") + Euro(MarketValue(player)), 12, Color.white, FontStyle.Bold, 0.57f, 0.08f, 0.78f, 0.46f, TextAnchor.MiddleCenter);
            var fitGrade = SystemFit(player);
            var fit = ProfileText(header.transform, "SystemFit", (english ? "FIT  " : "ENCAIXE  ") + fitGrade, 18, FitGradeColor(fitGrade), FontStyle.Bold, 0.8f, 0.8f, 0.985f, 0.96f, TextAnchor.MiddleLeft);
            fit.resizeTextForBestFit = true;
            fit.resizeTextMinSize = 12;
            fit.resizeTextMaxSize = 18;
            fit.horizontalOverflow = HorizontalWrapMode.Wrap;
            var contractDate = player.ContractEndTicks > 0 && player.ContractEndTicks <= DateTime.MaxValue.Ticks
                ? LocalizationManager.FormatDate(new DateTime(player.ContractEndTicks), "dd/MM/yyyy")
                : "—";
            var info = !scouted
                ? new[]
                {
                    (english ? "HEIGHT  " : "ALTURA  ") + (player.HeightCm > 0 ? player.HeightCm + " cm" : "—"),
                    (english ? "WEIGHT  " : "PESO  ") + (player.WeightKg > 0 ? player.WeightKg + " kg" : "—"),
                    (english ? "AGE  " : "IDADE  ") + player.Age
                }
                : new[]
                {
                    (english ? "HEIGHT  " : "ALTURA  ") + (player.HeightCm > 0 ? player.HeightCm + " cm" : "—"),
                    (english ? "WEIGHT  " : "PESO  ") + (player.WeightKg > 0 ? player.WeightKg + " kg" : "—"),
                    (english ? "FOOT  " : "PÉ  ") + LocalizationManager.Translate(PresentationLabels.Foot(player.PreferredFoot)) + (english ? "  ·  WEAK FOOT  " : "  ·  PÉ FRACO  ") + LocalizationManager.Translate(WeakFootLabel(player.WeakFootQuality)),
                    (english ? "PERSONALITY  " : "PERSONALIDADE  ") + LocalizationManager.Translate(PresentationLabels.Personality(player.Personality)),
                    (english ? "WEEKLY WAGE  " : "SALÁRIO SEMANAL  ") + Euro(player.WeeklyWage),
                    (english ? "CONTRACT UNTIL  " : "CONTRATO ATÉ  ") + contractDate
                };
            var grown = AssociationGrown(player);
            if (scouted)
                info = new[]
                {
                    (english ? "HEIGHT  " : "ALTURA  ") + (player.HeightCm > 0 ? player.HeightCm + " cm" : "—"),
                    (english ? "WEIGHT  " : "PESO  ") + (player.WeightKg > 0 ? player.WeightKg + " kg" : "—"),
                    (english ? "FOOT  " : "PÉ  ") + LocalizationManager.Translate(PresentationLabels.Foot(player.PreferredFoot)),
                    (english ? "WEAK FOOT  " : "PÉ FRACO  ") + LocalizationManager.Translate(WeakFootLabel(player.WeakFootQuality)),
                    (english ? "PERSONALITY  " : "PERSONALIDADE  ") + LocalizationManager.Translate(PresentationLabels.Personality(player.Personality)),
                    (english ? "WAGE  " : "SALÁRIO  ") + Euro(player.WeeklyWage),
                    (english ? "CONTRACT  " : "CONTRATO  ") + contractDate,
                    (english ? "ELIGIBILITY  " : "ELEGIBILIDADE  ") + (grown ? (english ? "Homegrown" : "Formado na associação") : (english ? "Not homegrown" : "Sem formação na associação"))
                };
            var infoLabel = ProfileText(header.transform, "PhysicalAndInfo", string.Join("\n", info), 12, Color.white, FontStyle.Normal, 0.8f, 0.04f, 0.985f, 0.78f);
            infoLabel.resizeTextForBestFit = true;
            infoLabel.resizeTextMinSize = 10;
            infoLabel.resizeTextMaxSize = 13;

            var current = PlayerAttributes.Current(player);
            if (player.Traits == null)
                PlayerTraits.Assign(player, player.Id.GetHashCode());

            var technical = new[] { "Cruzamento", "Drible", "Finalização", "Passe", "Desarme" };
            var mental = new[] { "Posicionamento", "Frieza", "Decisão", "Entrega", "Moral", "Forma" };
            var physical = keeper
                ? new[] { "Velocidade", "Resistência", "Força", "Reflexos", "Manejo" }
                : new[] { "Velocidade", "Resistência", "Força" };
            var technicalTitle = english ? "TECHNICAL" : "Técnica";
            var mentalTitle = english ? "MENTAL" : "Mental";
            var physicalTitle = english ? "PHYSICAL" : "Físico";
            BuildDenseAttributeColumn(technicalTitle, technical, player, 0.005f, mainBottom, 0.196f, mainTop);
            BuildDenseAttributeColumn(mentalTitle, mental, player, 0.201f, mainBottom, 0.392f, mainTop);
            BuildDenseAttributeColumn(physicalTitle, physical, player, 0.397f, mainBottom, 0.588f, mainTop);

            var radarPanel = ProfilePanel("Radar", 0.593f, mainBottom, 0.79f, mainTop);
            ProfileText(radarPanel.transform, "Heading", LocalizationManager.Translate("PERFIL"), 12, UiTheme.Accent, FontStyle.Bold, 0.04f, 0.87f, 0.96f, 0.99f, TextAnchor.MiddleCenter);
            var radarObject = new GameObject("PlayerRadar", typeof(RectTransform), typeof(CanvasRenderer), typeof(PlayerRadarGraphic));
            radarObject.transform.SetParent(radarPanel.transform, false);
            var radarRect = radarObject.GetComponent<RectTransform>();
            BroadcastUi.Place(radarRect, 0.13f, 0.1f, 0.87f, 0.84f);
            RadarLabel(radarPanel.transform, "AttackAxis", LocalizationManager.Translate("ATAQUE"), 0.34f, 0.82f, 0.66f, 0.96f);
            RadarLabel(radarPanel.transform, "TechniqueAxis", LocalizationManager.Translate("TÉCNICA"), 0.68f, 0.62f, 0.99f, 0.76f);
            RadarLabel(radarPanel.transform, "MentalAxis", LocalizationManager.Translate("MENTAL"), 0.68f, 0.17f, 0.99f, 0.31f);
            RadarLabel(radarPanel.transform, "DefenceAxis", LocalizationManager.Translate("DEFESA"), 0.005f, 0.21f, 0.29f, 0.36f);
            RadarLabel(radarPanel.transform, "PhysicalAxis", LocalizationManager.Translate("FÍSICO"), 0.005f, 0.62f, 0.29f, 0.76f);
            var scores = PlayerRadarScores(player, current);
            var radar = radarObject.GetComponent<PlayerRadarGraphic>();
            radar.color = UiTheme.Accent;
            radar.raycastTarget = false;
            radar.canvasRenderer.cullTransparentMesh = false;
            radar.SetValues(scores[0], scores[1], scores[2], scores[3], scores[4]);
            radar.SetAllDirty();
            Canvas.ForceUpdateCanvases();
            radar.SetVerticesDirty();
            DrawRadarOverlay(radarRect, scores);

            var traitsPanel = ProfilePanel("Traits", 0.795f, mainBottom, 0.995f, mainTop);
            ProfileText(traitsPanel.transform, "ProsTitle", LocalizationManager.Translate("TRAÇOS E PONTOS FORTES"), 11, UiTheme.Accent, FontStyle.Bold, 0.05f, 0.87f, 0.95f, 0.99f);
            var traitY = 0.83f;
            var traitStep = player.Traits.Count > 4 ? 0.115f : 0.145f;
            if (player.Traits.Count == 0)
                ProfileText(traitsPanel.transform, "NoTraits", LocalizationManager.Translate("Sem traços especiais."), 11, UiTheme.Muted, FontStyle.Normal, 0.05f, 0.68f, 0.95f, 0.84f);
            else
            {
                for (var i = 0; i < player.Traits.Count; i++)
                {
                    var trait = player.Traits[i];
                    var issue = trait == PlayerTrait.PropensoALesoes;
                    var traitLabel = LocalizationManager.Translate(PlayerTraits.Label(trait));
                    var effect = LocalizationManager.Translate(PlayerTraits.Effect(trait));
                    ProfileText(traitsPanel.transform, "Trait" + i, (issue ? LocalizationManager.Translate("ATENÇÃO") + "  ·  " : "•  ") + traitLabel + "\n" + effect, 11, issue ? UiTheme.FitnessLow : Color.white, FontStyle.Normal, 0.05f, traitY - traitStep, 0.95f, traitY);
                    traitY -= traitStep;
                }
            }
            var injury = player.Injury != null && player.Injury.IsInjured
                ? LocalizationManager.Translate(player.Injury.Description) + "  ·  " + player.Injury.DaysRemaining + (english ? " days" : " dias")
                : LocalizationManager.Translate("Sem lesão reportada");
            var physio = Member(_career.Club, StaffRole.Physiotherapist);
            ProfileText(traitsPanel.transform, "Medical", (physio == null ? LocalizationManager.Translate("Fisioterapia") : physio.FullName) + "\n" + injury, 10, player.Injury != null && player.Injury.IsInjured ? UiTheme.FitnessLow : UiTheme.Muted, FontStyle.Normal, 0.05f, 0.04f, 0.95f, Mathf.Max(0.1f, traitY - 0.015f));

            var status = ProfilePanel("Status", 0.005f, 0.205f, 0.995f, 0.325f);
            var statusItems = new[]
            {
                new KeyValuePair<string, string>("FITNESS", Mathf.Clamp(player.Fitness - player.Fatigue / 2, 0, 100) + "%"),
                new KeyValuePair<string, string>(LocalizationManager.Translate("DISCIPLINA"), LocalizationManager.Translate("Sem registo")),
                new KeyValuePair<string, string>(LocalizationManager.Translate("MORAL"), LocalizationManager.Translate(MoraleSummary(player.Morale))),
                new KeyValuePair<string, string>(LocalizationManager.Translate("FORMA"), player.Form + "/100"),
                new KeyValuePair<string, string>(LocalizationManager.Translate("TREINO"), (player.TrainingRating <= 0 ? 70 : player.TrainingRating) + "/100")
            };
            for (var i = 0; i < statusItems.Length; i++)
            {
                var x0 = i / (float)statusItems.Length;
                var x1 = (i + 1) / (float)statusItems.Length;
                var tile = ProfilePanelAt(status.transform, "Status" + i, new Color(0.06f, 0.1f, 0.13f, 1f), x0 + 0.004f, 0.08f, x1 - 0.004f, 0.92f);
                ProfileText(tile.transform, "Name", statusItems[i].Key, 10, UiTheme.Muted, FontStyle.Bold, 0.05f, 0.52f, 0.95f, 0.9f, TextAnchor.MiddleCenter);
                ProfileText(tile.transform, "Value", statusItems[i].Value, 12, Color.white, FontStyle.Bold, 0.05f, 0.08f, 0.95f, 0.52f, TextAnchor.MiddleCenter);
            }

            var statsPanel = ProfilePanel("SeasonNumbers", 0.005f, 0.015f, 0.54f, 0.19f);
            var apps = 0;
            var goals = 0;
            var assists = 0;
            var ratingSum = 0f;
            var rated = 0;
            foreach (var line in _career.Season)
            {
                if (line.PlayerId != player.Id)
                    continue;
                apps += line.Apps;
                goals += line.Goals;
                assists += line.Assists;
                ratingSum += line.RatingSum;
                rated += line.Rated;
            }
            var careerApps = 0;
            var careerGoals = 0;
            var careerAssists = 0;
            var careerRatingSum = 0f;
            var careerRated = 0;
            foreach (var line in _career.CareerTotals)
            {
                if (line.PlayerId != player.Id)
                    continue;
                careerApps += line.Apps;
                careerGoals += line.Goals;
                careerAssists += line.Assists;
                careerRatingSum += line.RatingSum;
                careerRated += line.Rated;
            }
            ProfileText(statsPanel.transform, "Heading", english ? "SEASON · ALL COMPETITIONS" : "ÉPOCA · TODAS AS COMPETIÇÕES", 11, UiTheme.Accent, FontStyle.Bold, 0.03f, 0.7f, 0.97f, 0.98f);
            var seasonAverage = rated > 0 ? (ratingSum / rated).ToString("0.00", CultureInfo.InvariantCulture) : "—";
            var seasonStats = LocalizationManager.Current == GameLanguage.English
                ? "Apps  " + apps + "     Goals  " + goals + "     Assists  " + assists + "     Avg  " + seasonAverage
                : "J  " + apps + "     G  " + goals + "     A  " + assists + "     MÉDIA  " + seasonAverage;
            ProfileText(statsPanel.transform, "SeasonStats", seasonStats, 11, Color.white, FontStyle.Bold, 0.03f, 0.4f, 0.97f, 0.68f);
            ProfileText(statsPanel.transform, "CareerHeading", english ? "CAREER" : "CARREIRA", 10, UiTheme.Accent, FontStyle.Bold, 0.03f, 0.18f, 0.97f, 0.39f);
            var careerAverage = careerRated > 0 ? (careerRatingSum / careerRated).ToString("0.00", CultureInfo.InvariantCulture) : "—";
            var careerStats = LocalizationManager.Current == GameLanguage.English
                ? "Apps  " + careerApps + "     Goals  " + careerGoals + "     Assists  " + careerAssists + "     Avg  " + careerAverage
                : "J  " + careerApps + "     G  " + careerGoals + "     A  " + careerAssists + "     MÉDIA  " + careerAverage;
            ProfileText(statsPanel.transform, "CareerStats", careerStats, 11, Color.white, FontStyle.Bold, 0.03f, 0.015f, 0.97f, 0.2f);

            var notePanel = ProfilePanel("ScoutAndCoach", 0.545f, 0.015f, 0.745f, 0.19f);
            var scout = Member(_career.Club, StaffRole.Scout);
            var scoutLine = academyOwned
                ? (english ? "Academy player · full internal report" : "Jogador da academia · relatório interno completo")
                : _career.ScoutedPlayers.Contains(player.Id)
                ? (scout == null ? (english ? "Scout" : "Olheiro") : scout.FullName) + (english ? " · report: OVR " : " · relatório: OVR ") + StaffReport.Read(scout, player.Id, "overall", PlayerAttributes.CurrentOverall(player), 40, 94)
                : own ? (english ? "Assistant: " : "Adjunto: ") + Tendency(player) : ScoutJobFor(player.Id) != null ? (english ? "Scout report in progress" : "Relatório do olheiro em curso") : (english ? "No scouting report" : "Sem relatório de observação");
            var fitnessValue = StaffReport.Read(fitness, player.Id, "cansaco", player.Fatigue, 0, 100);
            ProfileText(notePanel.transform, "Heading", english ? "SCOUT REPORT" : "RELATÓRIO TÉCNICO", 10, UiTheme.Accent, FontStyle.Bold, 0.04f, 0.68f, 0.96f, 0.98f);
            ProfileText(notePanel.transform, "Note", scoutLine + "\n" + (english ? "Fitness coach: fatigue " : "Preparador: cansaço ") + TiredWord(fitnessValue) + "\n" + (english ? "Assessment: " : "Leitura: ") + (reader == null ? (english ? "assistant" : "adjunto") : reader.FullName) + " · " + StaffReport.Confidence(reader), 10, Color.white, FontStyle.Normal, 0.04f, 0.04f, 0.96f, 0.68f);

            var actionPanel = ProfilePanel("PlayerActions", 0.75f, 0.015f, 0.995f, 0.19f);
            var personalNote = UserNote(player.Id);
            var captured = player;
            if (own)
            {
                var forSale = _career.Listed.Contains(player.Id);
                var forLoan = _career.Loans.Contains(player.Id);
                ProfileButton(actionPanel.transform, "Transfer", forSale
                    ? (english ? "REMOVE FROM LIST" : "RETIRAR DA VENDA")
                    : (english ? "LIST FOR SALE" : "PÔR À VENDA"), () =>
                {
                    if (_career.Listed.Contains(captured.Id))
                    {
                        _career.Listed.Remove(captured.Id);
                        captured.Morale = Mathf.Clamp(captured.Morale + 3, 1, 99);
                        CareerMemories.Record(_career, new FootballMemory
                        {
                            WhenTicks = _career.Calendar.CurrentDate.Ticks,
                            Importance = 2,
                            Kind = "transferencia",
                            ClubId = _career.Club.Id,
                            PlayerId = captured.Id,
                            Title = english ? "Taken off the list" : "Saiu da lista de transferências",
                            Detail = captured.FullName + (english ? " is no longer listed for sale." : " deixou de estar à venda.")
                        });
                    }
                    else
                    {
                        _career.Listed.Add(captured.Id);
                        _career.Loans.Remove(captured.Id);
                        captured.Morale = Mathf.Clamp(captured.Morale - ListingShock(captured), 1, 99);
                        CareerMemories.Record(_career, new FootballMemory
                        {
                            WhenTicks = _career.Calendar.CurrentDate.Ticks,
                            Importance = 3,
                            Kind = "transferencia",
                            ClubId = _career.Club.Id,
                            PlayerId = captured.Id,
                            Title = english ? "Transfer listed" : "Na lista de transferências",
                            Detail = ListingReaction(captured, english)
                        });
                    }
                    OpenPlayer(captured);
                }, 0.03f, 0.55f, 0.49f, 0.96f);
                ProfileButton(actionPanel.transform, "Loan", forLoan ? (english ? "REMOVE LOAN LISTING" : "RETIRAR DO EMPRÉSTIMO") : (english ? "LIST FOR LOAN" : "LISTA DE EMPRÉSTIMOS"), () =>
                {
                    if (_career.Loans.Contains(captured.Id))
                        _career.Loans.Remove(captured.Id);
                    else
                    {
                        _career.Loans.Add(captured.Id);
                        _career.Listed.Remove(captured.Id);
                        captured.Morale = Mathf.Clamp(captured.Morale - 2, 1, 99);
                        CareerMemories.Record(_career, new FootballMemory
                        {
                            WhenTicks = _career.Calendar.CurrentDate.Ticks,
                            Importance = 2,
                            Kind = "emprestimo",
                            ClubId = _career.Club.Id,
                            PlayerId = captured.Id,
                            Title = english ? "Loan listed" : "Na lista de empréstimos",
                            Detail = captured.FullName.Split(' ')[0] + (english
                                ? " asked where you see them next season."
                                : " perguntou onde o vês na próxima época.")
                        });
                    }
                    OpenPlayer(captured);
                }, 0.51f, 0.55f, 0.97f, 0.96f);
                var offerValue = 0;
                var buyer = forSale ? SaleBuyer(captured, out offerValue) : null;
                if (buyer != null)
                {
                    var buyerClub = buyer;
                    var price = offerValue;
                    var offerLabel = english
                        ? "ACCEPT OFFER  ·  " + Euro(price)
                        : "ACEITAR OFERTA  ·  " + Euro(price);
                    ProfileButton(actionPanel.transform, "AcceptSaleOffer", offerLabel, () => CompletePlayerSale(captured, buyerClub, price),
                        0.03f, 0.05f, 0.49f, 0.31f);
                }
                else
                    ProfileButton(actionPanel.transform, "RenewContract", english ? "NEGOTIATE CONTRACT" : "NEGOCIAR CONTRATO", () => OpenRenewalNegotiation(captured), 0.03f, 0.05f, 0.49f, 0.31f);
                var noteText = buyer != null
                    ? (english ? "Offer from " : "Proposta do ") + buyer.Name + "  ·  " + Euro(offerValue)
                        + (string.IsNullOrEmpty(personalNote) ? "" : "\n" + personalNote)
                    : string.IsNullOrEmpty(personalNote)
                        ? (forSale
                            ? (english ? "No club can currently match this player's fee." : "Nenhum clube consegue pagar o valor deste jogador neste momento.")
                            : english ? "No manager notes." : "Sem notas do treinador.")
                        : personalNote;
                ProfileText(actionPanel.transform, "Note", noteText, 9, UiTheme.Muted, FontStyle.Normal, 0.51f, 0.04f, 0.97f, 0.48f);
            }
            else if (academyOwned)
            {
                ProfileText(actionPanel.transform, "AcademyTag", english ? "YOUTH SQUAD" : "FORMAÇÃO", 15, UiTheme.Accent, FontStyle.Bold, 0.04f, 0.56f, 0.96f, 0.91f, TextAnchor.MiddleCenter);
                if (player.Age >= 16)
                    ProfileButton(actionPanel.transform, "PromoteAcademy", english ? "PROMOTE TO FIRST TEAM" : "PROMOVER À EQUIPA PRINCIPAL",
                        () => PromoteAcademyPlayer(captured), 0.03f, 0.05f, 0.97f, 0.49f);
                else
                    ProfileText(actionPanel.transform, "PromoteAge", english ? "Promotion available at 16" : "Promoção disponível aos 16", 12, UiTheme.Muted, FontStyle.Normal, 0.04f, 0.05f, 0.96f, 0.49f, TextAnchor.MiddleCenter);
            }
            else
            {
                ClaimForeignTargets();
                var wanted = _career.Targets.Contains(player.Id);
                ProfileButton(actionPanel.transform, "Target", wanted ? (english ? "REMOVE FROM SHORTLIST" : "TIRAR DA LISTA") : (english ? "SHORTLIST PLAYER" : "SEGUIR JOGADOR"), () =>
                {
                    if (_career.Targets.Contains(captured.Id)) _career.Targets.Remove(captured.Id);
                    else _career.Targets.Add(captured.Id);
                    _career.Listed.Remove(captured.Id);
                    _career.Loans.Remove(captured.Id);
                    OpenPlayer(captured);
                }, 0.03f, 0.55f, 0.49f, 0.96f);
                var job = ScoutJobFor(player.Id);
                var scoutLabel = scouted ? (english ? "REPORT READY" : "RELATÓRIO PRONTO") : job != null ? (english ? "SCOUTING" : "OLHEIRO EM CURSO") : (english ? "REQUEST SCOUT" : "PEDIR OLHEIRO");
                ProfileButton(actionPanel.transform, "Scout", scoutLabel, () =>
                {
                    AskScout(captured);
                    OpenPlayer(captured);
                }, 0.51f, 0.55f, 0.97f, 0.96f);
                var noteText = string.IsNullOrEmpty(personalNote) ? (english ? "No manager notes." : "Sem notas do treinador.") : personalNote;
                ProfileText(actionPanel.transform, "Note", noteText, 9, UiTheme.Muted, FontStyle.Normal, 0.04f, 0.05f, 0.96f, 0.48f);
            }

        }

        static string ReputationStars(int reputation)
        {
            var halfStars = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp(reputation, 0, 100) / 10f), 0, 10);
            var result = "";
            var full = halfStars / 2;
            for (var i = 0; i < full; i++) result += "★";
            if (halfStars % 2 != 0) result += "½";
            var empty = 5 - full - (halfStars % 2);
            for (var i = 0; i < empty; i++) result += "☆";
            return result;
        }

        static string ProfilePosition(PlayerPosition position, bool english)
        {
            switch (position)
            {
                case PlayerPosition.Goalkeeper: return english ? "GK" : "GR";
                case PlayerPosition.RightBack: return english ? "RB" : "LD";
                case PlayerPosition.CentreBack: return english ? "CB" : "DC";
                case PlayerPosition.LeftBack: return english ? "LB" : "LE";
                case PlayerPosition.DefensiveMidfielder: return english ? "DM" : "MDC";
                case PlayerPosition.CentralMidfielder: return english ? "CM" : "MC";
                case PlayerPosition.AttackingMidfielder: return english ? "AM" : "MO";
                case PlayerPosition.RightWinger: return english ? "RW" : "ED";
                case PlayerPosition.LeftWinger: return english ? "LW" : "EE";
                case PlayerPosition.RightMidfielder: return english ? "RM" : "MD";
                case PlayerPosition.LeftMidfielder: return english ? "LM" : "ME";
                case PlayerPosition.Striker: return english ? "ST" : "PL";
                default: return "?";
            }
        }

void OpenContractSheet(Player player, string page)
        {
            if (player == null)
                return;
            var previous = transform.Find("ContractSheet");
            if (previous != null)
                Destroy(previous.gameObject);
            var english = LocalizationManager.Current == GameLanguage.English;
            var sheet = BroadcastUi.Panel(transform, "ContractSheet", new Color(0.08f, 0.09f, 0.11f, 0.98f), true);
            BroadcastUi.Place(sheet.rectTransform, 0.03f, 0.08f, 0.97f, 0.96f);
            BroadcastUi.ApplySandPaper(sheet, 0.1f);
            sheet.transform.SetAsLastSibling();
            var close = BroadcastUi.Button(sheet.transform, "Close", "×", () => Destroy(sheet.gameObject), new Color(1f, 1f, 1f, 0.1f), Color.white, 20);
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.94f, 0.9f, 0.985f, 0.975f);

            var header = BroadcastUi.Panel(sheet.transform, "Header", new Color(0.12f, 0.13f, 0.16f, 1f), false);
            BroadcastUi.Place(header.rectTransform, 0.015f, 0.86f, 0.92f, 0.98f);
            var faceSlot = BroadcastUi.Zone(header.transform, "FaceSlot", 0.01f, 0.08f, 0.1f, 0.92f);
            FaceImage(faceSlot, player, 64f);
            var name = BroadcastUi.Label(header.transform, "Name", player.FullName, 22, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(name.rectTransform, 0.12f, 0.48f, 0.62f, 0.92f);
            var meta = BroadcastUi.Label(header.transform, "Meta", player.Age + (english ? " year old  ·  " : " anos  ·  ") + ProfilePosition(player.Position, english), 14, new Color(0.75f, 0.78f, 0.82f, 1f), TextAnchor.MiddleLeft);
            BroadcastUi.Place(meta.rectTransform, 0.12f, 0.08f, 0.55f, 0.48f);
            if (_career.World.Clubs.TryGetValue(player.ClubId ?? "", out var club) && club != null)
            {
                var crestSlot = BroadcastUi.Zone(header.transform, "CrestSlot", 0.58f, 0.2f, 0.66f, 0.8f);
                CrestImage(crestSlot, club, 36f);
                var clubName = BroadcastUi.Label(header.transform, "Club", club.ShortName, 16, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(clubName.rectTransform, 0.67f, 0.2f, 0.98f, 0.8f);
            }

            var now = _career.Calendar.CurrentDate.Date;
            var hasEnd = player.ContractEndTicks > 0 && player.ContractEndTicks < DateTime.MaxValue.Ticks;
            var end = hasEnd ? new DateTime(player.ContractEndTicks).Date : now;
            var start = hasEnd ? end.AddYears(-3) : now;
            if (start > now)
                start = now;
            var leftSpan = hasEnd ? end - now : TimeSpan.Zero;
            var yearsLeft = hasEnd ? Math.Max(0, (int)(leftSpan.TotalDays / 365.25)) : 0;
            var daysLeft = hasEnd ? Math.Max(0, leftSpan.Days - yearsLeft * 365) : 0;
            var culture = CultureInfo.GetCultureInfo(english ? "en-GB" : "pt-PT");
            var squad = Squad(_career.Club);
            var squadWeekly = 0;
            for (var i = 0; i < squad.Count; i++)
                squadWeekly += Math.Max(0, squad[i].WeeklyWage);
            var budget = ClubFinanceLedger.EnsureWeeklyWageBudget(_career, _career.Club);
            float share;
            if (budget > squadWeekly * 8 && budget > 0)
                share = player.WeeklyWage * 52f / budget;
            else if (budget > 0)
                share = player.WeeklyWage / (float)budget;
            else
                share = squadWeekly <= 0 ? 0f : player.WeeklyWage / (float)squadWeekly;
            share = Mathf.Clamp(share, 0f, 1f);
            var mix = 17;
            var seed = player.Id ?? "";
            for (var i = 0; i < seed.Length; i++)
                mix = mix * 31 + seed[i];
            mix &= 0x7fffffff;
            var loyalty = player.WeeklyWage * (4 + mix % 5);
            var appearance = Math.Max(500, player.WeeklyWage / (8 + mix % 6));
            var goalBonus = Math.Max(800, player.WeeklyWage / (3 + mix % 4));
            var assistBonus = Math.Max(600, player.WeeklyWage / (6 + mix % 5));
            var unusedBonus = Math.Max(200, player.WeeklyWage / (18 + mix % 10));
            var drop = 25 + (mix % 4) * 10;
            var extensionYears = 1 + mix % 2;

            var details = BroadcastUi.Panel(sheet.transform, "Details", new Color(0.05f, 0.06f, 0.08f, 1f), false);
            BroadcastUi.Place(details.rectTransform, 0.015f, 0.16f, 0.4f, 0.84f);
            SectionTitle(details.transform, english ? "CONTRACT DETAILS" : "DETALHES DO CONTRATO");
            var wageLabel = BroadcastUi.Label(details.transform, "WageLabel", english ? "Wage" : "Salário", 13, new Color(0.7f, 0.74f, 0.78f, 1f), TextAnchor.MiddleLeft);
            BroadcastUi.Place(wageLabel.rectTransform, 0.05f, 0.78f, 0.4f, 0.86f);
            var pill = BroadcastUi.Panel(details.transform, "WagePill", new Color(0.58f, 0.22f, 0.95f, 1f), false);
            BroadcastUi.Place(pill.rectTransform, 0.38f, 0.78f, 0.78f, 0.88f);
            var pillText = BroadcastUi.Label(pill.transform, "V", ContractMoney(player.WeeklyWage) + (english ? " p/w" : " /sem"), 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Stretch(pillText.rectTransform);
            var tax = BroadcastUi.Label(details.transform, "Tax", ContractMoney(Mathf.RoundToInt(player.WeeklyWage * 0.54f)) + (english ? " p/w (after tax)" : " /sem (depois de impostos)"), 12, new Color(0.65f, 0.68f, 0.72f, 1f), TextAnchor.MiddleLeft);
            BroadcastUi.Place(tax.rectTransform, 0.05f, 0.7f, 0.95f, 0.78f);
            var percentLabel = BroadcastUi.Label(details.transform, "PercentLabel", english ? "Percentage of overall wage budget" : "Percentagem da massa salarial", 12, new Color(0.7f, 0.74f, 0.78f, 1f), TextAnchor.MiddleLeft);
            BroadcastUi.Place(percentLabel.rectTransform, 0.05f, 0.62f, 0.78f, 0.7f);
            var percent = BroadcastUi.Label(details.transform, "Percent", (share * 100f).ToString("0.0", culture) + "%", 14, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
            BroadcastUi.Place(percent.rectTransform, 0.7f, 0.62f, 0.95f, 0.7f);
            var track = BroadcastUi.Panel(details.transform, "Track", new Color(1f, 1f, 1f, 0.08f), false);
            BroadcastUi.Place(track.rectTransform, 0.05f, 0.56f, 0.95f, 0.61f);
            var fill = BroadcastUi.Panel(track.transform, "Fill", new Color(0.58f, 0.22f, 0.95f, 1f), false);
            BroadcastUi.Place(fill.rectTransform, 0f, 0.15f, Mathf.Clamp(share <= 0f ? 0.04f : share, 0.04f, 1f), 0.85f);
            ContractRow(details.transform, "Start", english ? "Start date" : "Início", hasEnd ? start.ToString("dd/MM/yyyy", culture) : "—", 0.46f);
            ContractRow(details.transform, "Expiry", english ? "Expiry date" : "Fim", hasEnd ? end.ToString("dd/MM/yyyy", culture) : "—", 0.36f);
            ContractRow(details.transform, "Left", english ? "Time left on contract" : "Tempo restante", hasEnd ? yearsLeft + (english ? " years, " : " anos, ") + daysLeft + (english ? " days" : " dias") : "—", 0.26f);
            ContractRow(details.transform, "Type", english ? "Contract type" : "Tipo de contrato", english ? "Full time contract" : "Contrato a tempo inteiro", 0.16f);

            var middle = BroadcastUi.Panel(sheet.transform, "Middle", new Color(0.05f, 0.06f, 0.08f, 1f), false);
            BroadcastUi.Place(middle.rectTransform, 0.41f, 0.16f, 0.67f, 0.84f);
            SectionTitle(middle.transform, english ? "BONUSES" : "BÓNUS");
            ContractRow(middle.transform, "Loyalty", english ? "Remaining loyalty bonus" : "Lealdade por pagar", ContractMoney(loyalty), 0.74f);
            ContractRow(middle.transform, "Appearance", english ? "Appearance fee" : "Prémio de utilização", ContractMoney(appearance), 0.64f);
            ContractRow(middle.transform, "Goal", english ? "Goal bonus" : "Prémio de golo", ContractMoney(goalBonus), 0.54f);
            ContractRow(middle.transform, "Assist", english ? "Assist bonus" : "Prémio de assistência", ContractMoney(assistBonus), 0.44f);
            ContractRow(middle.transform, "Unused", english ? "Unused substitute fee" : "Suplente não utilizado", ContractMoney(unusedBonus), 0.34f);
            var clauses = BroadcastUi.Label(middle.transform, "Clauses", english ? "CLAUSES" : "CLÁUSULAS", 13, new Color(0.7f, 0.74f, 0.78f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(clauses.rectTransform, 0.05f, 0.22f, 0.95f, 0.3f);
            ContractRow(middle.transform, "Drop", english ? "Relegation wage drop" : "Corte por despromoção", drop + "%", 0.12f);
            ContractRow(middle.transform, "Extend", english ? "Optional extension by club" : "Extensão opcional do clube", extensionYears + (english ? " year" : extensionYears == 1 ? " ano" : " anos"), 0.02f);

            var right = BroadcastUi.Panel(sheet.transform, "Right", new Color(0.07f, 0.08f, 0.1f, 1f), false);
            BroadcastUi.Place(right.rectTransform, 0.68f, 0.16f, 0.985f, 0.84f);
            var eleven = new List<Player>();
            var bench = new List<Player>();
            StartingShape.Resolve(squad, _career.Club, eleven, bench);
            var inEleven = false;
            var onBench = false;
            for (var i = 0; i < eleven.Count; i++)
                if (eleven[i].Id == player.Id)
                    inEleven = true;
            for (var i = 0; i < bench.Count && i < 7; i++)
                if (bench[i].Id == player.Id)
                    onBench = true;
            var playing = inEleven ? (english ? "Regular starter" : "Titular habitual") : onBench ? (english ? "Squad rotation" : "Rotação") : (english ? "Fringe player" : "Suplente");
            var agreed = inEleven ? 8 : onBench ? 5 : 2;
            var playCard = BroadcastUi.Panel(right.transform, "Play", new Color(0.1f, 0.11f, 0.14f, 1f), false);
            BroadcastUi.Place(playCard.rectTransform, 0.04f, 0.78f, 0.96f, 0.97f);
            var playTitle = BroadcastUi.Label(playCard.transform, "T", english ? "PLAYING TIME" : "TEMPO DE JOGO", 11, new Color(0.95f, 0.78f, 0.25f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(playTitle.rectTransform, 0.06f, 0.68f, 0.94f, 0.96f);
            var playValue = BroadcastUi.Label(playCard.transform, "V", playing, 16, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(playValue.rectTransform, 0.06f, 0.34f, 0.94f, 0.68f);
            var playNote = BroadcastUi.Label(playCard.transform, "N", english ? "Matches agreed" : "Jogos acordados", 11, new Color(0.7f, 0.74f, 0.78f, 1f), TextAnchor.MiddleLeft);
            BroadcastUi.Place(playNote.rectTransform, 0.06f, 0.02f, 0.48f, 0.32f);
            DiamondRow(playCard.transform, "Agreed", agreed, 10, 0.5f, 0.08f, 0.28f);

            var report = BroadcastUi.Panel(right.transform, "Report", new Color(0.1f, 0.16f, 0.13f, 1f), false);
            BroadcastUi.Place(report.rectTransform, 0.04f, 0.58f, 0.96f, 0.76f);
            var overall = PlayerAttributes.Overall(player);
            var reportText = overall >= 80 ? (english ? "Important player in the side." : "Jogador importante no onze.") : player.Potential >= overall + 8 ? (english ? "Important player who could still improve." : "Jogador importante que ainda pode evoluir.") : (english ? "Squad player with a defined role." : "Jogador de plantel com função definida.");
            var reportTitle = BroadcastUi.Label(report.transform, "T", english ? "COACH REPORT" : "RELATÓRIO DO TREINADOR", 11, new Color(0.45f, 0.9f, 0.55f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(reportTitle.rectTransform, 0.06f, 0.72f, 0.94f, 0.96f);
            var reportBody = BroadcastUi.Label(report.transform, "B", reportText, 12, Color.white, TextAnchor.MiddleLeft);
            reportBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            BroadcastUi.Place(reportBody.rectTransform, 0.06f, 0.48f, 0.94f, 0.74f);
            var ca = BroadcastUi.Label(report.transform, "CA", english ? "Current ability" : "Capacidade atual", 12, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(ca.rectTransform, 0.06f, 0.24f, 0.55f, 0.48f);
            DiamondRow(report.transform, "CAStars", AbilityStars(overall), 5, 0.58f, 0.28f, 0.46f);
            var pa = BroadcastUi.Label(report.transform, "PA", english ? "Potential ability" : "Potencial", 12, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(pa.rectTransform, 0.06f, 0.02f, 0.55f, 0.26f);
            DiamondRow(report.transform, "PAStars", AbilityStars(player.Potential), 5, 0.58f, 0.04f, 0.22f);

            var permit = BroadcastUi.Panel(right.transform, "Permit", new Color(0.1f, 0.11f, 0.14f, 1f), false);
            BroadcastUi.Place(permit.rectTransform, 0.04f, 0.46f, 0.96f, 0.56f);
            var permitTitle = BroadcastUi.Label(permit.transform, "T", english ? "WORK PERMIT" : "AUTORIZAÇÃO DE TRABALHO", 11, new Color(0.7f, 0.74f, 0.78f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(permitTitle.rectTransform, 0.06f, 0.55f, 0.94f, 0.95f);
            var permitState = BroadcastUi.Label(permit.transform, "S", (english ? "Active" : "Ativo") + (hasEnd ? (english ? " until " : " até ") + end.ToString("dd/MM/yyyy", culture) : ""), 13, new Color(0.45f, 0.9f, 0.55f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(permitState.rectTransform, 0.06f, 0.05f, 0.94f, 0.55f);

            var contracts = BroadcastUi.Panel(right.transform, "Contracts", new Color(0.1f, 0.11f, 0.14f, 1f), false);
            BroadcastUi.Place(contracts.rectTransform, 0.04f, 0.03f, 0.96f, 0.44f);
            var contractTitle = BroadcastUi.Label(contracts.transform, "T", english ? "CONTRACTS" : "CONTRATOS", 11, new Color(0.7f, 0.74f, 0.78f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(contractTitle.rectTransform, 0.06f, 0.82f, 0.55f, 0.98f);
            var contractCount = BroadcastUi.Label(contracts.transform, "C", squad.Count + (english ? " contracted players" : " jogadores sob contrato"), 13, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
            BroadcastUi.Place(contractCount.rectTransform, 0.4f, 0.82f, 0.94f, 0.98f);
            var buckets = new[] { 0, 0, 0, 0 };
            for (var i = 0; i < squad.Count; i++)
            {
                var member = squad[i];
                if (member.ContractEndTicks <= 0)
                {
                    buckets[3]++;
                    continue;
                }
                var months = (new DateTime(member.ContractEndTicks).Date - now).TotalDays / 30.0;
                if (months < 6) buckets[0]++;
                else if (months < 12) buckets[1]++;
                else if (months < 24) buckets[2]++;
                else buckets[3]++;
            }
            var maxBucket = 1;
            for (var i = 0; i < buckets.Length; i++)
                if (buckets[i] > maxBucket)
                    maxBucket = buckets[i];
            var bucketLabels = new[] { "0-6M", "6-12M", "1-2A", "2A+" };
            for (var i = 0; i < buckets.Length; i++)
            {
                var x0 = 0.08f + i * 0.23f;
                var height = 0.06f + 0.34f * buckets[i] / maxBucket;
                var column = BroadcastUi.Panel(contracts.transform, "Col" + i, new Color(0.2f, 0.75f, 0.82f, 1f), false);
                BroadcastUi.Place(column.rectTransform, x0, 0.22f, x0 + 0.14f, 0.22f + height);
                var countLabel = BroadcastUi.Label(contracts.transform, "N" + i, buckets[i].ToString(), 12, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(countLabel.rectTransform, x0 - 0.02f, 0.22f + height, x0 + 0.16f, 0.22f + height + 0.1f);
                var bucket = BroadcastUi.Label(contracts.transform, "L" + i, bucketLabels[i], 11, new Color(0.7f, 0.74f, 0.78f, 1f), TextAnchor.MiddleCenter);
                BroadcastUi.Place(bucket.rectTransform, x0 - 0.02f, 0.08f, x0 + 0.16f, 0.2f);
            }

            if (page == "ofertas")
                PaintContractOffers(sheet.transform, player, english);
            else if (page == "agente")
                PaintAgentAsk(sheet.transform, player, english, Mathf.Max(250, Mathf.RoundToInt(player.WeeklyWage * 1.18f)));
            else if (page == "acoes")
                PaintContractActions(sheet.transform, player, english);
            else
            {
                var discuss = BroadcastUi.Button(sheet.transform, "Discuss", english ? "DISCUSS NEW CONTRACT WITH AGENT" : "FALAR DE UM NOVO CONTRATO COM O AGENTE", () => OpenContractSheet(player, "agente"), new Color(0.85f, 0.15f, 0.55f, 1f), Color.white, 13);
                BroadcastUi.Place(discuss.GetComponent<RectTransform>(), 0.015f, 0.025f, 0.46f, 0.13f);
                var enter = BroadcastUi.Button(sheet.transform, "Enter", english ? "ENTER CONTRACT NEGOTIATIONS" : "ENTRAR EM NEGOCIAÇÕES", () => OpenContractSheet(player, "ofertas"), new Color(0.16f, 0.17f, 0.2f, 1f), Color.white, 13);
                BroadcastUi.Place(enter.GetComponent<RectTransform>(), 0.48f, 0.025f, 0.74f, 0.13f);
                var actions = BroadcastUi.Button(sheet.transform, "Actions", english ? "ACTIONS" : "AÇÕES", () => OpenContractSheet(player, "acoes"), new Color(0.16f, 0.17f, 0.2f, 1f), Color.white, 13);
                BroadcastUi.Place(actions.GetComponent<RectTransform>(), 0.76f, 0.025f, 0.985f, 0.13f);
            }
        }

        void PaintContractOffers(Transform sheet, Player player, bool english)
        {
            var raises = new[] { 5, 12, 20 };
            var terms = new[] { 2, 3, 4 };
            var titles = english ? new[] { "Cautious", "The agent's line", "Make him stay" } : new[] { "Prudente", "A linha do agente", "Para ficar" };
            for (var i = 0; i < raises.Length; i++)
            {
                var increase = raises[i];
                var term = terms[i];
                var wage = Mathf.Max(250, Mathf.RoundToInt(player.WeeklyWage * (1f + increase / 100f)));
                var x0 = 0.015f + i * 0.27f;
                var card = BroadcastUi.Panel(sheet, "Offer" + i, new Color(0.1f, 0.11f, 0.14f, 1f), true);
                BroadcastUi.Place(card.rectTransform, x0, 0.02f, x0 + 0.25f, 0.145f);
                var heading = BroadcastUi.Label(card.transform, "H", titles[i], 12, new Color(0.7f, 0.74f, 0.78f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(heading.rectTransform, 0.06f, 0.68f, 0.94f, 0.96f);
                var pill = BroadcastUi.Panel(card.transform, "Pill", new Color(0.58f, 0.22f, 0.95f, 1f), false);
                BroadcastUi.Place(pill.rectTransform, 0.06f, 0.32f, 0.62f, 0.64f);
                var pillText = BroadcastUi.Label(pill.transform, "V", ContractMoney(wage), 14, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Stretch(pillText.rectTransform);
                var termLabel = BroadcastUi.Label(card.transform, "Y", term + (english ? " years" : " anos"), 13, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(termLabel.rectTransform, 0.66f, 0.32f, 0.96f, 0.64f);
                var offer = BroadcastUi.Button(card.transform, "Go", "+" + increase + "%", () => ResolveRenewalOffer(player, wage, term, increase), new Color(0.85f, 0.15f, 0.55f, 1f), Color.white, 13);
                BroadcastUi.Place(offer.GetComponent<RectTransform>(), 0.06f, 0.06f, 0.94f, 0.28f);
            }
            var back = BroadcastUi.Button(sheet, "BackOffers", english ? "BACK" : "VOLTAR", () => OpenContractSheet(player, "ficha"), new Color(0.16f, 0.17f, 0.2f, 1f), Color.white, 13);
            BroadcastUi.Place(back.GetComponent<RectTransform>(), 0.83f, 0.02f, 0.985f, 0.145f);
        }

        void PaintAgentAsk(Transform sheet, Player player, bool english, int wage)
        {
            var card = BroadcastUi.Panel(sheet, "Agent", new Color(0.12f, 0.08f, 0.16f, 1f), true);
            BroadcastUi.Place(card.rectTransform, 0.015f, 0.02f, 0.72f, 0.145f);
            var heading = BroadcastUi.Label(card.transform, "H", english ? "THE AGENT'S OPENING LINE" : "A PEDIDO DO AGENTE", 12, new Color(0.95f, 0.55f, 0.75f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.03f, 0.62f, 0.7f, 0.94f);
            var pill = BroadcastUi.Panel(card.transform, "Pill", new Color(0.58f, 0.22f, 0.95f, 1f), false);
            BroadcastUi.Place(pill.rectTransform, 0.62f, 0.55f, 0.96f, 0.9f);
            var pillText = BroadcastUi.Label(pill.transform, "V", ContractMoney(wage) + (english ? " p/w" : " /sem"), 14, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Stretch(pillText.rectTransform);
            var note = BroadcastUi.Label(card.transform, "N", english ? "He will sit down if the offer starts here. Three years." : "Senta-se à mesa se a conversa começar aqui. Três anos.", 13, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(note.rectTransform, 0.03f, 0.08f, 0.6f, 0.55f);
            var talk = BroadcastUi.Button(card.transform, "Talk", english ? "OPEN THE TABLE" : "ABRIR A MESA", () => OpenContractSheet(player, "ofertas"), new Color(0.85f, 0.15f, 0.55f, 1f), Color.white, 13);
            BroadcastUi.Place(talk.GetComponent<RectTransform>(), 0.62f, 0.08f, 0.96f, 0.48f);
            var back = BroadcastUi.Button(sheet, "BackAgent", english ? "BACK" : "VOLTAR", () => OpenContractSheet(player, "ficha"), new Color(0.16f, 0.17f, 0.2f, 1f), Color.white, 13);
            BroadcastUi.Place(back.GetComponent<RectTransform>(), 0.74f, 0.02f, 0.985f, 0.145f);
        }

        void PaintContractActions(Transform sheet, Player player, bool english)
        {
            var listed = _career.Listed.Contains(player.Id);
            var card = BroadcastUi.Panel(sheet, "Menu", new Color(0.1f, 0.11f, 0.14f, 1f), true);
            BroadcastUi.Place(card.rectTransform, 0.62f, 0.02f, 0.985f, 0.145f);
            var toggle = BroadcastUi.Button(card.transform, "List", listed ? (english ? "REMOVE FROM THE LIST" : "RETIRAR DA LISTA") : (english ? "LIST FOR SALE" : "COLOCAR À VENDA"), () =>
            {
                ToggleContractListing(player);
                OpenContractSheet(player, "ficha");
            }, listed ? new Color(0.35f, 0.18f, 0.18f, 1f) : new Color(0.85f, 0.15f, 0.55f, 1f), Color.white, 13);
            BroadcastUi.Place(toggle.GetComponent<RectTransform>(), 0.04f, 0.52f, 0.96f, 0.92f);
            var back = BroadcastUi.Button(card.transform, "Back", english ? "CLOSE" : "FECHAR", () => OpenContractSheet(player, "ficha"), new Color(0.16f, 0.17f, 0.2f, 1f), Color.white, 13);
            BroadcastUi.Place(back.GetComponent<RectTransform>(), 0.04f, 0.08f, 0.96f, 0.44f);
        }

        void ToggleContractListing(Player player)
        {
            if (player == null)
                return;
            var english = LocalizationManager.Current == GameLanguage.English;
            if (_career.Listed.Contains(player.Id))
            {
                _career.Listed.Remove(player.Id);
                player.Morale = Mathf.Clamp(player.Morale + 3, 1, 99);
            }
            else
            {
                _career.Listed.Add(player.Id);
                _career.Loans.Remove(player.Id);
                player.Morale = Mathf.Clamp(player.Morale - ListingShock(player), 1, 99);
            }
            CareerMemories.Record(_career, new FootballMemory
            {
                WhenTicks = _career.Calendar.CurrentDate.Ticks,
                Importance = 3,
                Kind = "transferencia",
                ClubId = _career.Club.Id,
                PlayerId = player.Id,
                Title = _career.Listed.Contains(player.Id) ? (english ? "Listed for sale" : "Colocado à venda") : (english ? "Taken off the list" : "Saiu da lista"),
                Detail = player.FullName
            });
            CareerStore.Save(_career);
        }

        static void SectionTitle(Transform parent, string text)
        {
            var label = BroadcastUi.Label(parent, "Section", text, 13, new Color(0.7f, 0.74f, 0.78f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(label.rectTransform, 0.05f, 0.9f, 0.95f, 0.98f);
        }

        static void ContractRow(Transform parent, string name, string left, string right, float y)
        {
            var row = BroadcastUi.Panel(parent, name, new Color(1f, 1f, 1f, 0.035f), false);
            BroadcastUi.Place(row.rectTransform, 0.04f, y, 0.96f, y + 0.08f);
            var a = BroadcastUi.Label(row.transform, "L", left, 13, new Color(0.78f, 0.8f, 0.84f, 1f), TextAnchor.MiddleLeft);
            BroadcastUi.Place(a.rectTransform, 0.04f, 0.08f, 0.62f, 0.92f);
            var b = BroadcastUi.Label(row.transform, "R", right, 14, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
            b.resizeTextForBestFit = true;
            b.resizeTextMinSize = 10;
            b.resizeTextMaxSize = 14;
            BroadcastUi.Place(b.rectTransform, 0.6f, 0.08f, 0.96f, 0.92f);
        }

        static void DiamondRow(Transform parent, string name, int filled, int total, float x, float y0, float y1)
        {
            var width = 0.94f - x;
            var step = width / Mathf.Max(1, total);
            for (var i = 0; i < total; i++)
            {
                var diamond = BroadcastUi.Panel(parent, name + i, i < filled ? new Color(0.98f, 0.78f, 0.2f, 1f) : new Color(1f, 1f, 1f, 0.14f), false);
                var x0 = x + i * step;
                BroadcastUi.Place(diamond.rectTransform, x0, y0, x0 + step * 0.62f, y1);
                diamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
        }

        static int AbilityStars(int value)
        {
            if (value <= 20)
                return Mathf.Clamp(Mathf.RoundToInt(value / 4f), 1, 5);
            return Mathf.Clamp(Mathf.RoundToInt(value / 18f), 1, 5);
        }

        static string ContractMoney(int value)
        {
            var amount = Math.Max(0, value);
            if (amount >= 1000000)
                return "€" + (amount / 1000000f).ToString("0.0", CultureInfo.InvariantCulture) + "M";
            if (amount >= 1000)
                return "€" + Mathf.RoundToInt(amount / 1000f) + "K";
            return "€" + amount;
        }

        void OpenRenewalNegotiation(Player player)
        {
            if (player == null || player.ClubId != _career.Club.Id)
                return;
            OpenContractSheet(player, "ficha");
        }



        void ResolveRenewalOffer(Player player, int wage, int years, int increase)
        {
            var contractSheet = transform.Find("ContractSheet");
            if (contractSheet != null)
                Destroy(contractSheet.gameObject);
            if (player == null || player.ClubId != _career.Club.Id) return;
            var english = LocalizationManager.Current == GameLanguage.English;
            var acceptance = Mathf.Clamp(82 - increase * 2 + (player.Morale - 50) / 3 + (player.Age <= 27 ? 5 : 0), 12, 92);
            var seed = player.Id + "|" + _career.Calendar.CurrentDate.Ticks + "|" + increase + "|" + (++_renewalAttempt);
            var roll = 0;
            unchecked { foreach (var c in seed) roll = roll * 31 + c; }
            if ((roll & 0x7fffffff) % 100 < acceptance)
            {
                var date = _career.Calendar.CurrentDate.Date.AddYears(years);
                player.ContractEndTicks = new DateTime(date.Year, 6, 30).Ticks;
                player.WeeklyWage = wage;
                CareerStore.Save(_career);
                OpenModal(english ? "Deal agreed" : "Acordo alcançado", player.FullName, true);
                AddLine(english
                    ? "The player accepted " + Euro(wage) + "/week for " + years + " years."
                    : "O jogador aceitou " + Euro(wage) + "/semana por " + years + " anos.");
                _modalActionLabel.text = english ? "DONE" : "CONCLUÍDO";
                _modalAction.onClick.RemoveAllListeners();
                _modalAction.onClick.AddListener(() => { CloseModal(); OpenPlayer(player); });
            }
            else
            {
                OpenModal(english ? "Offer rejected" : "Proposta recusada", player.FullName, true);
                AddLine(english
                    ? "The player rejected the offer. His current contract remains unchanged; you can try a different wage and term."
                    : "O jogador recusou a proposta. O contrato atual mantém-se; podes negociar outro salário e duração.");
                _modalActionLabel.text = english ? "TRY AGAIN" : "TENTAR DE NOVO";
                _modalAction.onClick.RemoveAllListeners();
                _modalAction.onClick.AddListener(() => OpenRenewalNegotiation(player));
            }
            CareerStore.Save(_career);
        }

        Image ProfilePanel(string name, float x0, float y0, float x1, float y1)
        {
            return ProfilePanelAt(_modalBody, name, new Color(0.035f, 0.075f, 0.1f, 0.96f), x0, y0, x1, y1);
        }

        static Image ProfilePanelAt(Transform parent, string name, Color color, float x0, float y0, float x1, float y1)
        {
            var panel = BroadcastUi.Panel(parent, name, color);
            BroadcastUi.Place(panel.rectTransform, x0, y0, x1, y1);
            return panel;
        }

        void PaintRolePitch(Transform parent, Player player, float x0, float y0, float x1, float y1)
        {
            if (parent == null || player == null)
                return;
            var pitch = BroadcastUi.Zone(parent, "RolePitch", x0, y0, x1, y1);
            BroadcastUi.DrawVerticalPitch(pitch, new Color(0.08f, 0.32f, 0.16f));
            var formation = _career != null && _career.Club != null && _career.Club.Tactics != null && !string.IsNullOrEmpty(_career.Club.Tactics.Formation)
                ? _career.Club.Tactics.Formation
                : "4-3-3";
            var slots = StartingShape.SlotPositions(formation);
            for (var i = 0; i < 11; i++)
            {
                var spot = MatchEngine.Slot(true, i, formation);
                var role = slots != null && i < slots.Length ? slots[i] : PlayerPosition.CentralMidfielder;
                var here = role == player.Position;
                var dot = BroadcastUi.Solid(pitch, "Dot" + i, here ? UiTheme.Accent : new Color(0.85f, 0.88f, 0.92f, 0.85f));
                dot.sprite = BroadcastUi.Circle;
                dot.raycastTarget = false;
                dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(spot.x, spot.y);
                dot.rectTransform.sizeDelta = here ? new Vector2(14f, 14f) : new Vector2(7f, 7f);
            }
        }

        static Text ProfileText(Transform parent, string name, string value, int size, Color color, FontStyle style, float x0, float y0, float x1, float y1, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var label = BroadcastUi.Label(parent, name, value, size, color, anchor, style);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            BroadcastUi.Place(label.rectTransform, x0, y0, x1, y1);
            return label;
        }

        void BuildDenseAttributeColumn(string title, string[] stats, Player player, float x0, float y0, float x1, float y1)
        {
            var panel = ProfilePanel(title, x0, y0, x1, y1);
            ProfileText(panel.transform, "Heading", LocalizationManager.Translate(title).ToUpperInvariant(), 11, UiTheme.Accent, FontStyle.Bold, 0.04f, 0.87f, 0.96f, 0.99f);
            var gap = 0.78f / stats.Length;
            for (var i = 0; i < stats.Length; i++)
            {
                var yTop = 0.84f - i * gap;
                var yBottom = yTop - gap + 0.015f;
                var row = BroadcastUi.Zone(panel.transform, "Stat" + i, 0.04f, yBottom, 0.96f, yTop);
                var value = stats[i] == "Moral" ? Mathf.RoundToInt(player.Morale / 5f)
                    : stats[i] == "Forma" ? Mathf.RoundToInt(player.Form / 5f)
                    : PlayerAttributes.CurrentValue(player, stats[i]);
                ProfileText(row, "Name", LocalizationManager.Translate(stats[i]), 9, Color.white, FontStyle.Normal, 0f, 0.48f, 0.68f, 1f);
                var number = ProfileText(row, "Number", value.ToString(), 12, Color.white, FontStyle.Bold, 0.68f, 0.42f, 0.99f, 1f, TextAnchor.MiddleRight);
                number.horizontalOverflow = HorizontalWrapMode.Overflow;
                number.verticalOverflow = VerticalWrapMode.Overflow;
                var track = BroadcastUi.Solid(row, "Track", new Color(1f, 1f, 1f, 0.13f));
                BroadcastUi.Place(track.rectTransform, 0f, 0.05f, 1f, 0.34f);
                var fill = BroadcastUi.Solid(track.transform, "Fill", value >= 15 ? UiTheme.Fitness : value >= 10 ? UiTheme.Accent : UiTheme.FitnessLow);
                BroadcastUi.Stretch(fill.rectTransform);
                fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value / 20f), 1f);
            }
        }

        static void ProfileButton(Transform parent, string name, string caption, Action onClick, float x0, float y0, float x1, float y1)
        {
            var button = BroadcastUi.Button(parent, name, caption, onClick, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 10);
            BroadcastUi.Place(button.GetComponent<RectTransform>(), x0, y0, x1, y1);
        }

        static float[] PlayerRadarScores(Player player, PlayerAttributes a)
        {
            var attack = (a.Finishing * 1.2f + a.Positioning + a.Pace + a.Dribbling * 0.5f + a.Passing * 0.3f) / 4f;
            var technique = (a.Passing + a.Dribbling + a.Crossing + a.Decisions + a.Composure) / 5f;
            var mental = (a.Composure + a.Decisions + a.WorkRate + a.Positioning) / 4f;
            var defence = (a.Tackling + a.Positioning + a.Strength + a.WorkRate) / 4f;
            var physical = (a.Pace + a.Stamina + a.Strength) / 3f;
            switch (player.Position)
            {
                case PlayerPosition.Goalkeeper:
                    attack = (a.Passing + a.Decisions + a.Composure) / 3f;
                    technique = (a.Passing + a.Decisions + a.Handling) / 3f;
                    mental = (a.Composure + a.Decisions + a.Positioning) / 3f;
                    defence = (a.Reflexes * 1.25f + a.Handling * 1.2f + a.Positioning) / 3.45f;
                    physical = (a.Strength + a.Stamina + a.Pace) / 3f;
                    break;
                case PlayerPosition.RightWinger:
                case PlayerPosition.LeftWinger:
                    attack = (a.Pace * 1.2f + a.Dribbling * 1.2f + a.Finishing + a.Crossing + a.Positioning * 0.6f) / 5f;
                    defence = (a.Tackling * 0.65f + a.Positioning * 0.75f + a.Strength * 0.6f + a.WorkRate) / 3f;
                    break;
                case PlayerPosition.Striker:
                    attack = (a.Finishing * 1.35f + a.Positioning * 1.3f + a.Composure + a.Pace + a.Strength * 0.5f) / 5.15f;
                    defence = (a.Tackling * 0.5f + a.Positioning * 0.7f + a.WorkRate * 1.1f) / 2.3f;
                    break;
                case PlayerPosition.CentreBack:
                    attack = (a.Passing + a.Decisions + a.Strength * 0.3f) / 2.3f;
                    defence = (a.Tackling * 1.3f + a.Positioning * 1.25f + a.Strength + a.WorkRate * 0.8f) / 4.35f;
                    break;
                case PlayerPosition.LeftBack:
                case PlayerPosition.RightBack:
                    attack = (a.Pace + a.Crossing * 1.1f + a.Dribbling * 0.7f + a.Passing) / 3.8f;
                    defence = (a.Tackling + a.Positioning + a.WorkRate + a.Stamina * 0.3f) / 3.3f;
                    break;
                case PlayerPosition.DefensiveMidfielder:
                    attack = (a.Passing + a.Decisions + a.Dribbling * 0.5f) / 2.5f;
                    defence = (a.Tackling * 1.15f + a.Positioning * 1.2f + a.WorkRate + a.Strength * 0.5f) / 3.85f;
                    break;
                case PlayerPosition.CentralMidfielder:
                case PlayerPosition.RightMidfielder:
                case PlayerPosition.LeftMidfielder:
                case PlayerPosition.AttackingMidfielder:
                    attack = (a.Passing * 1.1f + a.Dribbling + a.Finishing * 0.65f + a.Decisions * 1.1f) / 3.85f;
                    defence = (a.Tackling * 0.8f + a.Positioning + a.WorkRate) / 2.8f;
                    break;
            }
            return new[] { attack / 20f, technique / 20f, mental / 20f, defence / 20f, physical / 20f };
        }

        static PlayerPosition PositionFromLabel(string label)
        {
            foreach (PlayerPosition position in Enum.GetValues(typeof(PlayerPosition)))
                if (PresentationLabels.Position(position) == label)
                    return position;
            return PlayerPosition.CentralMidfielder;
        }

        static string MoraleSummary(int morale)
        {
            if (morale >= 82) return "Excelente";
            if (morale >= 68) return "Bom";
            if (morale >= 48) return "Estável";
            if (morale >= 30) return "Baixo";
            return "Muito baixo";
        }

        void BuildPlayerSummary(Player player, bool keeper)
        {
            var panel = BroadcastUi.Panel(_modalBody, "PlayerOverview", new Color(0.035f, 0.075f, 0.1f, 0.96f));
            BroadcastUi.Preferred(panel, 210f);
            var rating = PlayerAttributes.CurrentOverallAtPosition(player, player.Position);
            var baseline = PlayerAttributes.OverallAtPosition(player, player.Position);
            var change = rating - baseline;
            var delta = change > 0 ? "+" + change : change.ToString();
            var ovrTitle = BroadcastUi.Label(panel.transform, "DynamicLabel", "OVERALL ATUAL", 12, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(ovrTitle.rectTransform, 0.04f, 0.78f, 0.32f, 0.96f);
            var ovr = BroadcastUi.Label(panel.transform, "DynamicOverall", rating.ToString(), 38, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(ovr.rectTransform, 0.04f, 0.5f, 0.27f, 0.82f);
            var baseLine = BroadcastUi.Label(panel.transform, "BaseOverall", "Base " + baseline + "   ·   forma " + delta, 13, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(baseLine.rectTransform, 0.04f, 0.35f, 0.33f, 0.54f);
            var condition = BroadcastUi.Label(panel.transform, "Condition", "Moral " + player.Morale + "   ·   treino " + (player.TrainingRating <= 0 ? 70 : player.TrainingRating), 12, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(condition.rectTransform, 0.04f, 0.21f, 0.33f, 0.38f);
            var potential = PlayerTraits.HiddenPotential(player, player.ClubId == _career.Club.Id || _career.ScoutedPlayers.Contains(player.Id));
            var value = BroadcastUi.Label(panel.transform, "MarketValue", "Potencial " + potential + "   ·   " + Euro(MarketValue(player)), 12, UiTheme.AccentSoft, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(value.rectTransform, 0.04f, 0.05f, 0.34f, 0.24f);

            var radarObject = new GameObject("PlayerRadar", typeof(RectTransform), typeof(CanvasRenderer), typeof(PlayerRadarGraphic));
            radarObject.transform.SetParent(panel.transform, false);
            var radarRect = radarObject.GetComponent<RectTransform>();
            BroadcastUi.Place(radarRect, 0.36f, 0.12f, 0.65f, 0.9f);
            var radar = radarObject.GetComponent<PlayerRadarGraphic>();
            radar.color = UiTheme.Accent;
            radar.raycastTarget = false;
            radar.canvasRenderer.cullTransparentMesh = false;
            var current = PlayerAttributes.Current(player);
            var attack = (current.Finishing + current.Positioning + current.Pace) / 60f;
            var technique = (current.Passing + current.Dribbling + current.Crossing) / 60f;
            var mental = (current.Composure + current.Decisions + current.WorkRate) / 60f;
            var defending = (current.Tackling + current.Positioning + current.Strength) / 60f;
            var physical = (current.Pace + current.Stamina + current.Strength) / 60f;
            if (keeper)
            {
                defending = (current.Reflexes + current.Handling + current.Positioning) / 60f;
                attack = (current.Passing + current.Decisions + current.Composure) / 60f;
            }
            radar.SetValues(attack, technique, mental, defending, physical);
            radar.SetAllDirty();
            Canvas.ForceUpdateCanvases();
            radar.SetVerticesDirty();
            DrawRadarOverlay(radarRect, new[] { attack, technique, mental, defending, physical });
            RadarLabel(panel.transform, "Ataque", "ATAQUE", 0.455f, 0.87f, 0.555f, 0.98f);
            RadarLabel(panel.transform, "Tecnica", "TÉCNICA", 0.545f, 0.64f, 0.68f, 0.77f);
            RadarLabel(panel.transform, "Mental", "MENTAL", 0.455f, 0.02f, 0.555f, 0.14f);
            RadarLabel(panel.transform, "Defesa", "DEFESA", 0.33f, 0.35f, 0.455f, 0.48f);
            RadarLabel(panel.transform, "Fisico", "FÍSICO", 0.33f, 0.66f, 0.455f, 0.79f);

            var details = new[]
            {
                "Idade  " + player.Age + " anos",
                "Altura  " + (player.HeightCm > 0 ? player.HeightCm + " cm" : "—"),
                "Peso  " + (player.WeightKg > 0 ? player.WeightKg + " kg" : "—"),
                "Pé preferido  " + PresentationLabels.Foot(player.PreferredFoot),
                "Pé fraco  " + WeakFootLabel(player.WeakFootQuality)
            };
            for (var i = 0; i < details.Length; i++)
            {
                var line = BroadcastUi.Label(panel.transform, "Physical" + i, details[i], 13, i == 0 ? UiTheme.Accent : Color.white, TextAnchor.MiddleLeft, i == 0 ? FontStyle.Bold : FontStyle.Normal);
                BroadcastUi.Place(line.rectTransform, 0.72f, 0.79f - i * 0.16f, 0.97f, 0.94f - i * 0.16f);
            }
            var fatigue = BroadcastUi.Label(panel.transform, "ConditionLabel", "Condição  " + (100 - player.Fatigue) + "%", 12, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(fatigue.rectTransform, 0.72f, 0.03f, 0.97f, 0.18f);
        }

        static void RadarLabel(Transform parent, string name, string text, float x0, float y0, float x1, float y1)
        {
            var label = BroadcastUi.Label(parent, name, text, 9, UiTheme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(label.rectTransform, x0, y0, x1, y1);
        }

        static void DrawRadarOverlay(RectTransform host, float[] values)
        {
            if (host == null || values == null || values.Length < 5)
                return;
            var rect = host.rect;
            if (rect.width <= 1f || rect.height <= 1f)
                return;
            var center = rect.center;
            var radius = Mathf.Min(rect.width, rect.height) * 0.42f;
            var outer = new Vector2[5];
            var data = new Vector2[5];
            for (var i = 0; i < 5; i++)
            {
                var angle = Mathf.PI * 0.5f - i * Mathf.PI * 2f / 5f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                outer[i] = center + direction * radius;
                data[i] = center + direction * radius * Mathf.Max(0.08f, Mathf.Clamp01(values[i]));
            }

            var grid = new Color(0.72f, 0.86f, 0.94f, 0.9f);
            for (var ring = 1; ring <= 3; ring++)
            {
                var points = new Vector2[5];
                for (var i = 0; i < 5; i++)
                    points[i] = center + (outer[i] - center) * (ring / 3f);
                for (var i = 0; i < 5; i++)
                    RadarStroke(host, "Grid" + ring + "_" + i, points[i], points[(i + 1) % 5], 2.2f, grid);
            }

            for (var i = 0; i < 5; i++)
                RadarStroke(host, "Axis" + i, center, outer[i], 1.8f, grid);
            for (var i = 0; i < 5; i++)
            {
                RadarStroke(host, "Value" + i, data[i], data[(i + 1) % 5], 3.4f, UiTheme.Accent);
                var node = BroadcastUi.Solid(host, "ValueNode" + i, Color.white);
                node.sprite = BroadcastUi.Circle;
                node.raycastTarget = false;
                var nodeRect = node.rectTransform;
                nodeRect.anchorMin = nodeRect.anchorMax = new Vector2(0.5f, 0.5f);
                nodeRect.sizeDelta = new Vector2(8f, 8f);
                nodeRect.anchoredPosition = data[i];
            }
        }

        static void RadarStroke(Transform parent, string name, Vector2 start, Vector2 end, float thickness, Color tint)
        {
            var direction = end - start;
            if (direction.sqrMagnitude < 0.001f)
                return;
            var image = BroadcastUi.Solid(parent, name, tint);
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(direction.magnitude, thickness);
            rect.anchoredPosition = (start + end) * 0.5f;
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        void AddPlayerSeasonStats(string playerId)
        {
            var apps = 0;
            var goals = 0;
            var assists = 0;
            var ratingSum = 0f;
            var rated = 0;
            if (_career.Season != null)
            {
                foreach (var line in _career.Season)
                {
                    if (line == null || line.PlayerId != playerId)
                        continue;
                    apps += line.Apps;
                    goals += line.Goals;
                    assists += line.Assists;
                    ratingSum += line.RatingSum;
                    rated += line.Rated;
                }
            }

            AddHead("Época actual · todas as competições");
            AddLine("Jogos " + apps + "   ·   Golos " + goals + "   ·   Assistências " + assists
                + "   ·   Média " + (rated > 0 ? (ratingSum / rated).ToString("0.00", CultureInfo.InvariantCulture) : "—"));
        }

        void BuildPlayerAttributeColumns(Player player, bool keeper)
        {
            var panel = BroadcastUi.Panel(_modalBody, "AttributeReport", new Color(0.035f, 0.075f, 0.1f, 0.96f));
            BroadcastUi.Preferred(panel, 300f);
            var technical = new[]
            {
                new KeyValuePair<string, string>("Cruzamento", "Cruzamento"),
                new KeyValuePair<string, string>("Drible", "Drible"),
                new KeyValuePair<string, string>("Finalização", "Finalização"),
                new KeyValuePair<string, string>("Passe", "Passe"),
                new KeyValuePair<string, string>("Desarme", "Desarme")
            };
            var mental = new[]
            {
                new KeyValuePair<string, string>("Posicionamento", "Posicionamento"),
                new KeyValuePair<string, string>("Frieza", "Frieza"),
                new KeyValuePair<string, string>("Decisão", "Decisão"),
                new KeyValuePair<string, string>("Entrega", "Entrega"),
                new KeyValuePair<string, string>("Moral", "Moral"),
                new KeyValuePair<string, string>("Forma", "Forma")
            };
            var physical = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Velocidade", "Velocidade"),
                new KeyValuePair<string, string>("Resistência", "Resistência"),
                new KeyValuePair<string, string>("Força", "Força")
            };
            if (keeper)
            {
                physical.Add(new KeyValuePair<string, string>("Reflexos", "Reflexos"));
                physical.Add(new KeyValuePair<string, string>("Manejo", "Manejo"));
            }

            BuildAttributeColumn(panel.transform, "Técnicos", technical, player, 0.01f, 0.33f);
            BuildAttributeColumn(panel.transform, "Mentais", mental, player, 0.34f, 0.66f);
            BuildAttributeColumn(panel.transform, "Físicos", physical.ToArray(), player, 0.67f, 0.99f);
        }

        void BuildAttributeColumn(Transform parent, string title, KeyValuePair<string, string>[] stats, Player player, float left, float right)
        {
            var column = BroadcastUi.Panel(parent, title, new Color(0.06f, 0.1f, 0.13f, 0.98f));
            BroadcastUi.Place(column.rectTransform, left, 0.03f, right, 0.97f);
            var heading = BroadcastUi.Label(column.transform, "Heading", title.ToUpperInvariant(), 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.05f, 0.84f, 0.95f, 0.98f);
            var step = stats.Length > 5 ? 0.125f : 0.145f;
            var y = 0.73f;
            for (var i = 0; i < stats.Length; i++)
            {
                var key = stats[i].Key;
                var value = key == "Moral" ? Mathf.RoundToInt(player.Morale / 5f)
                    : key == "Forma" ? Mathf.RoundToInt(player.Form / 5f)
                    : PlayerAttributes.CurrentValue(player, key);
                var row = BroadcastUi.Zone(column.transform, "Stat" + i, 0.04f, y - 0.07f, 0.96f, y + 0.045f);
                var label = BroadcastUi.Label(row, "Name", stats[i].Value, 13, Color.white, TextAnchor.MiddleLeft);
                BroadcastUi.Place(label.rectTransform, 0f, 0f, 0.49f, 1f);
                var number = BroadcastUi.Label(row, "Number", value.ToString(), 14, UiTheme.Score, TextAnchor.MiddleRight, FontStyle.Bold);
                BroadcastUi.Place(number.rectTransform, 0.49f, 0f, 0.66f, 1f);
                var track = BroadcastUi.Solid(row, "Track", new Color(1f, 1f, 1f, 0.16f));
                BroadcastUi.Place(track.rectTransform, 0.7f, 0.36f, 1f, 0.64f);
                var fill = BroadcastUi.Solid(track.transform, "Fill", UiTheme.Fitness);
                BroadcastUi.Stretch(fill.rectTransform);
                var amount = Mathf.Clamp01(value / 20f);
                fill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.04f, amount), 1f);
                fill.color = amount >= 0.75f ? UiTheme.Fitness : amount >= 0.5f ? UiTheme.Accent : UiTheme.FitnessLow;
                y -= step;
            }
        }

        void OpenStaff(StaffMember member)
        {
            if (member == null)
                return;
            _offer = null;
            if (_staffSheet != null)
                Destroy(_staffSheet);
            var english = LocalizationManager.Current == GameLanguage.English;
            var host = transform.Find("Design") as Transform ?? transform;
            var sheet = BroadcastUi.Panel(host, "StaffSheet", new Color(0.05f, 0.025f, 0.1f, 0.98f), true);
            _staffSheet = sheet.gameObject;
            BroadcastUi.Place(sheet.rectTransform, 0.2f, 0.14f, 0.8f, 0.88f);
            BroadcastUi.ApplySandPaper(sheet, 0.1f);
            sheet.transform.SetAsLastSibling();

            var close = BroadcastUi.Button(sheet.transform, "CloseStaff", "×", CloseStaffSheet, new Color(1f, 1f, 1f, 0.12f), Color.white, 22);
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.94f, 0.92f, 0.985f, 0.985f);
            var name = BroadcastUi.Label(sheet.transform, "Name", member.FullName, 26, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(name.rectTransform, 0.03f, 0.9f, 0.7f, 0.985f);
            var role = BroadcastUi.Label(sheet.transform, "Role", LocalizationManager.Translate(PresentationLabels.StaffRole(member.Role)), 16, UiTheme.Accent, TextAnchor.MiddleLeft);
            BroadcastUi.Place(role.rectTransform, 0.03f, 0.845f, 0.7f, 0.91f);

            var portrait = BroadcastUi.Solid(sheet.transform, "Portrait", Color.white);
            portrait.sprite = FaceBook.For(member);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            BroadcastUi.Place(portrait.rectTransform, 0.04f, 0.58f, 0.2f, 0.82f);
            StaffFact(sheet.transform, "Age", (english ? "Age" : "Idade") + "   " + StaffProfile.Age(member), 0.03f, 0.44f, 0.28f, 0.51f);
            StaffFact(sheet.transform, "Nation", StaffProfile.Nation(member, english), 0.03f, 0.37f, 0.28f, 0.44f);
            StaffFact(sheet.transform, "Personality", (english ? "Personality" : "Personalidade") + "   " + StaffProfile.Personality(member, english), 0.03f, 0.3f, 0.28f, 0.37f);
            StaffFact(sheet.transform, "Shapes", (english ? "Shapes" : "Formações") + "   " + StaffProfile.Formations(member), 0.03f, 0.2f, 0.28f, 0.3f);
            StaffFact(sheet.transform, "Style", (english ? "Style" : "Estilo") + "   " + StaffProfile.Style(member, english), 0.03f, 0.1f, 0.28f, 0.2f);

            var coaching = BroadcastUi.Label(sheet.transform, "Coaching", english ? "COACHING" : "TREINO", 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(coaching.rectTransform, 0.31f, 0.78f, 0.62f, 0.84f);
            var mental = BroadcastUi.Label(sheet.transform, "Mental", english ? "MENTAL" : "MENTAL", 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(mental.rectTransform, 0.64f, 0.78f, 0.97f, 0.84f);
            for (var i = 0; i < StaffProfile.Skills.Length; i++)
            {
                var column = i < 7 ? 0 : 1;
                var row = i % 7;
                var x0 = column == 0 ? 0.31f : 0.64f;
                var y = 0.7f - row * 0.085f;
                var skill = StaffProfile.Skills[i];
                var value = StaffProfile.Skill(member, skill.Key, StaffProfile.Bias(member.Role, skill.Key));
                var line = BroadcastUi.Zone(sheet.transform, "Skill" + i, x0, y, x0 + 0.3f, y + 0.07f);
                var skillName = BroadcastUi.Label(line, "Name", english ? skill.English : skill.Portuguese, 14, Color.white, TextAnchor.MiddleLeft);
                BroadcastUi.Place(skillName.rectTransform, 0f, 0f, 0.62f, 1f);
                var word = BroadcastUi.Label(line, "Word", StaffProfile.Word(value, english), 14, UiTheme.Score, TextAnchor.MiddleRight, FontStyle.Bold);
                BroadcastUi.Place(word.rectTransform, 0.62f, 0f, 1f, 1f);
            }

            var withYou = BroadcastUi.Label(sheet.transform, "WithYou", member.ClubId == _career.Club.Id
                ? (english ? "On your staff. This profile changes what the assistant asks for in matches." : "Está no teu plantel técnico. Esta ficha muda o que o adjunto pede nos jogos.")
                : (english ? "No working relationship with you yet." : "Ainda não trabalha contigo."), 14, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.Place(withYou.rectTransform, 0.31f, 0.08f, 0.68f, 0.16f);
            var best = BroadcastUi.Label(sheet.transform, "Best", StaffProfile.BestLine(member, english), 14, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(best.rectTransform, 0.31f, 0.02f, 0.68f, 0.08f);
            var captured = member;
            var fire = BroadcastUi.Button(sheet.transform, "Fire", english ? "SACK" : "DESPEDIR", () =>
            {
                CloseStaffSheet();
                SackStaff(captured);
            }, new Color(0.42f, 0.1f, 0.14f, 1f), Color.white, 14);
            BroadcastUi.Place(fire.GetComponent<RectTransform>(), 0.7f, 0.03f, 0.97f, 0.12f);
        }

        string StaffCountsLine(bool english)
        {
            return CountRole(StaffRole.AssistantCoach) + (english ? " assistants  ·  " : " adjuntos  ·  ")
                + CountRole(StaffRole.GoalkeeperCoach) + (english ? " goalkeeping  ·  " : " guarda-redes  ·  ")
                + CountRole(StaffRole.FitnessCoach) + (english ? " fitness  ·  " : " físicos  ·  ")
                + CountRole(StaffRole.Physiotherapist) + (english ? " physios  ·  " : " fisioterapeutas  ·  ")
                + CountRole(StaffRole.Scout) + (english ? " scouts" : " olheiros")
                + "   ·   " + ClubStaffCount() + "/" + MaxClubStaff + (english ? " places" : " lugares");
        }

        int ClubStaffCount()
        {
            var count = 0;
            if (_career?.Club?.Staff == null)
                return count;
            foreach (var member in _career.Club.Staff)
                if (member != null)
                    count++;
            return count;
        }

        int CountRole(StaffRole role)
        {
            var count = 0;
            if (_career?.Club?.Staff == null)
                return 0;
            foreach (var member in _career.Club.Staff)
                if (member != null && member.Role == role)
                    count++;
            return count;
        }

        void SackStaff(StaffMember member)
        {
            if (member == null || _career?.Club?.Staff == null)
                return;
            var english = LocalizationManager.Current == GameLanguage.English;
            _career.Club.Staff.Remove(member);
            _career.Club.DressingRoomTension = Mathf.Clamp(_career.Club.DressingRoomTension + 3, 0, 100);
            PostDesk(english
                ? "✉ " + member.FullName + " left the " + PresentationLabels.StaffRole(member.Role).ToLowerInvariant() + " role."
                : "✉ " + member.FullName + " saiu da função de " + PresentationLabels.StaffRole(member.Role).ToLowerInvariant() + ".");
            CareerStore.Save(_career);
            FillStaff();
            Refresh();
        }

        void OfferWeeklyStaff()
        {
            if (_career == null || _career.Club == null || _career.Calendar == null)
                return;
            var week = _career.Calendar.CurrentDate.Year * 100 + ((_career.Calendar.CurrentDate.DayOfYear - 1) / 7);
            var key = "staff-rec|" + week;
            if (NotebookHas(key))
                return;
            _career.Notebook.Add(key);
            var roles = new[] { StaffRole.AssistantCoach, StaffRole.GoalkeeperCoach, StaffRole.FitnessCoach, StaffRole.Physiotherapist, StaffRole.Scout };
            var seed = week + _career.Club.Reputation;
            var english = LocalizationManager.Current == GameLanguage.English;
            var offered = new List<StaffOfferCandidate>();
            for (var i = 0; i < 3; i++)
            {
                var role = roles[Mathf.Abs(seed + i * 2) % roles.Length];
                var member = StaffReport.Offer(_career.Club, role);
                if (member == null)
                    continue;
                var candidate = new StaffOfferCandidate
                {
                    Member = member,
                    AskingWage = StaffAskingWage(member),
                    ContractYears = 2 + i % 2,
                    Week = week
                };
                offered.Add(candidate);
                _career.Notebook.Add("staff-offer|" + week + "|" + member.Id + "|" + member.FullName + "|" + (int)member.Role + "|" + member.Judgement + "|" + candidate.AskingWage + "|" + candidate.ContractYears);
            }
            if (offered.Count == 0)
                return;
            var list = new List<string>();
            foreach (var candidate in offered)
                list.Add(candidate.Member.FullName + " · " + LocalizationManager.Translate(PresentationLabels.StaffRole(candidate.Member.Role))
                    + " · " + candidate.Member.Judgement + "/20 · " + Euro(candidate.AskingWage) + (english ? "/wk" : "/sem"));
            PostDesk((english
                ? "✉ The assistant has shortlisted three staff candidates. Open this message to review the list and negotiate a contract.\n"
                : "✉ O adjunto selecionou candidatos para a equipa técnica. Abre esta mensagem para ver a lista e negociar um contrato.\n")
                + string.Join("\n", list) + "\n[[staff-offers:" + week + "]]");
            CareerStore.Save(_career);
        }

        bool LatestStaffOffer(out StaffMember offer)
        {
            offer = null;
            if (_career == null)
                return false;
            var week = _career.Calendar.CurrentDate.Year * 100 + ((_career.Calendar.CurrentDate.DayOfYear - 1) / 7);
            var candidates = StaffOfferCandidates(week);
            if (candidates.Count == 0)
                return false;
            offer = candidates[candidates.Count - 1].Member;
            return true;
        }

        void HireRecommended(StaffMember member)
        {
            HireRecommended(member, StaffAskingWage(member), 2, member == null ? "" : member.Id);
        }

        void HireRecommended(StaffMember member, int weeklyWage, int contractYears, string offerId)
        {
            if (member == null || _career?.Club == null)
                return;
            if (_career.Club.Staff == null)
                _career.Club.Staff = new List<StaffMember>();
            if (ClubStaffCount() >= MaxClubStaff)
            {
                PostDesk(LocalizationManager.Current == GameLanguage.English
                    ? "✉ The staff roster is full. Free a place before hiring someone else."
                    : "✉ O limite da equipa técnica foi atingido. Liberta um lugar antes de contratar mais alguém.");
                FillStaff();
                return;
            }
            foreach (var existing in _career.Club.Staff)
                if (existing != null && existing.Id == member.Id)
                    return;
            member.ClubId = _career.Club.Id;
            member.WeeklyWage = Math.Max(0, weeklyWage);
            member.ContractEndTicks = _career.Calendar.CurrentDate.Date.AddYears(Mathf.Clamp(contractYears, 1, 5)).Ticks;
            _career.Club.Staff.Add(member);
            var resolved = "staff-offer-resolved|" + offerId;
            if (!string.IsNullOrEmpty(offerId) && !_career.Notebook.Contains(resolved))
                _career.Notebook.Add(resolved);
            PostDesk(LocalizationManager.Current == GameLanguage.English
                ? "✉ " + member.FullName + " joined as " + LocalizationManager.Translate(PresentationLabels.StaffRole(member.Role))
                    + " on " + Euro(member.WeeklyWage) + "/week until " + LocalizationManager.FormatDate(new DateTime(member.ContractEndTicks), "d MMM yyyy") + "."
                : "✉ " + member.FullName + " entrou como " + PresentationLabels.StaffRole(member.Role)
                    + " por " + Euro(member.WeeklyWage) + "/semana até " + LocalizationManager.FormatDate(new DateTime(member.ContractEndTicks), "d MMM yyyy") + ".");
            CareerStore.Save(_career);
            FillStaff();
        }

        int StaffAskingWage(StaffMember member)
        {
            if (member == null || _career?.Club == null) return 500;
            var multiplier = member.Role == StaffRole.AssistantCoach ? 1.15f
                : member.Role == StaffRole.GoalkeeperCoach ? 0.9f
                : member.Role == StaffRole.Physiotherapist ? 0.95f
                : member.Role == StaffRole.Scout ? 0.9f : 0.85f;
            var basePay = 350f + _career.Club.Reputation * 32f;
            var quality = 0.8f + Mathf.Clamp(member.Judgement, 1, 20) * 0.025f;
            return Mathf.Max(350, Mathf.RoundToInt(basePay * quality * multiplier / 50f) * 50);
        }

        List<StaffOfferCandidate> StaffOfferCandidates(int week)
        {
            var result = new List<StaffOfferCandidate>();
            if (_career == null) return result;
            if (_career.Club == null) return result;
            if (_career.Club.Staff == null) _career.Club.Staff = new List<StaffMember>();
            foreach (var line in _career.Notebook)
            {
                if (string.IsNullOrEmpty(line) || !line.StartsWith("staff-offer|", StringComparison.Ordinal))
                    continue;
                var bits = line.Split('|');
                StaffMember member;
                int asking;
                int years;
                int offerWeek;
                if (bits.Length >= 8)
                {
                    if (!int.TryParse(bits[1], out offerWeek) || offerWeek != week) continue;
                    int.TryParse(bits[4], out var role);
                    int.TryParse(bits[5], out var judgement);
                    int.TryParse(bits[6], out asking);
                    int.TryParse(bits[7], out years);
                    member = new StaffMember { Id = bits[2], ClubId = _career.Club.Id, FullName = bits[3], Role = (StaffRole)role, Judgement = judgement };
                }
                else if (bits.Length >= 5)
                {
                    int.TryParse(bits[3], out var role);
                    int.TryParse(bits[4], out var judgement);
                    member = new StaffMember { Id = bits[1], ClubId = _career.Club.Id, FullName = bits[2], Role = (StaffRole)role, Judgement = judgement };
                    asking = StaffAskingWage(member);
                    years = 2;
                    offerWeek = week;
                }
                else continue;
                if (_career.Notebook.Contains("staff-offer-resolved|" + member.Id)) continue;
                var alreadyHired = false;
                foreach (var existing in _career.Club.Staff)
                    if (existing != null && existing.Id == member.Id) { alreadyHired = true; break; }
                if (alreadyHired) continue;
                result.Add(new StaffOfferCandidate { Member = member, AskingWage = asking, ContractYears = years, Week = offerWeek });
            }
            return result;
        }

        void OpenStaffContractOffer(StaffOfferCandidate candidate, string phoneKey)
        {
            if (candidate?.Member == null || _career?.Club == null)
                return;
            if (ClubStaffCount() >= MaxClubStaff)
            {
                var englishLimit = LocalizationManager.Current == GameLanguage.English;
                var fullMessage = englishLimit
                    ? "The club has reached its staff limit. Remove a staff member before hiring another."
                    : "O clube atingiu o limite de staff. Remove um membro antes de contratar outro.";
                if (!string.IsNullOrEmpty(phoneKey)) AddPhoneLine(fullMessage);
                else PostDesk("✉ " + fullMessage);
                return;
            }
            var previous = transform.Find("StaffNegotiation");
            if (previous != null) Destroy(previous.gameObject);
            var english = LocalizationManager.Current == GameLanguage.English;
            var sheet = BroadcastUi.Panel(transform, "StaffNegotiation", new Color(0.055f, 0.075f, 0.11f, 1f), true);
            BroadcastUi.Place(sheet.rectTransform, 0.035f, 0.07f, 0.965f, 0.96f);
            BroadcastUi.ApplySandPaper(sheet, 0.1f);
            sheet.transform.SetAsLastSibling();
            var close = BroadcastUi.Button(sheet.transform, "Close", "×", () => Destroy(sheet.gameObject), new Color(1f, 1f, 1f, 0.12f), Color.white, 20);
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.94f, 0.91f, 0.985f, 0.98f);
            var title = BroadcastUi.Label(sheet.transform, "Title", english ? "STAFF CONTRACT NEGOTIATION" : "NEGOCIAÇÃO DE CONTRATO · STAFF", 24, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.025f, 0.9f, 0.82f, 0.985f);

            var header = BroadcastUi.Panel(sheet.transform, "Candidate", new Color(0.09f, 0.12f, 0.17f, 1f), false);
            BroadcastUi.Place(header.rectTransform, 0.025f, 0.69f, 0.975f, 0.87f);
            var portrait = BroadcastUi.Solid(header.transform, "Portrait", Color.white);
            portrait.sprite = FaceBook.For(candidate.Member);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            BroadcastUi.Place(portrait.rectTransform, 0.018f, 0.08f, 0.13f, 0.92f);
            var name = BroadcastUi.Label(header.transform, "Name", candidate.Member.FullName, 22, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(name.rectTransform, 0.15f, 0.48f, 0.58f, 0.94f);
            var role = BroadcastUi.Label(header.transform, "Role", LocalizationManager.Translate(PresentationLabels.StaffRole(candidate.Member.Role))
                + "   ·   " + (english ? "Judgement " : "Avaliação ") + candidate.Member.Judgement + "/20", 15, UiTheme.Accent, TextAnchor.MiddleLeft);
            BroadcastUi.Place(role.rectTransform, 0.15f, 0.08f, 0.58f, 0.5f);
            var ask = BroadcastUi.Label(header.transform, "Ask", (english ? "ASKING WAGE  " : "SALÁRIO PEDIDO  ") + Euro(candidate.AskingWage) + (english ? "/week" : "/semana"), 16, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
            BroadcastUi.Place(ask.rectTransform, 0.62f, 0.2f, 0.97f, 0.78f);

            var room = StaffWeeklyWageRoom();
            var details = BroadcastUi.Panel(sheet.transform, "Terms", new Color(0.08f, 0.1f, 0.15f, 1f), false);
            BroadcastUi.Place(details.rectTransform, 0.025f, 0.15f, 0.34f, 0.65f);
            SectionTitle(details.transform, english ? "CONTRACT TERMS" : "TERMOS DO CONTRATO");
            ContractRow(details.transform, "Role", english ? "Role" : "Função", LocalizationManager.Translate(PresentationLabels.StaffRole(candidate.Member.Role)), 0.76f);
            ContractRow(details.transform, "Asked", english ? "Weekly wage requested" : "Salário semanal pedido", Euro(candidate.AskingWage), 0.59f);
            ContractRow(details.transform, "Room", english ? "Available wage room" : "Folga salarial", Euro(room) + (english ? "/wk" : "/sem"), 0.42f);
            ContractRow(details.transform, "Length", english ? "Contract length" : "Duração", english ? "2–4 years" : "2–4 anos", 0.25f);
            var note = BroadcastUi.Label(details.transform, "Note", english
                ? "Wages are paid weekly. A signing adds one staff member, up to the club limit."
                : "Os salários são pagos semanalmente. A contratação acrescenta um membro até ao limite do clube.", 13, UiTheme.Muted, TextAnchor.UpperLeft);
            note.horizontalOverflow = HorizontalWrapMode.Wrap;
            BroadcastUi.Place(note.rectTransform, 0.05f, 0.03f, 0.95f, 0.16f);

            var offers = BroadcastUi.Panel(sheet.transform, "OfferChoices", new Color(0.08f, 0.1f, 0.15f, 1f), false);
            BroadcastUi.Place(offers.rectTransform, 0.36f, 0.15f, 0.975f, 0.65f);
            SectionTitle(offers.transform, english ? "MAKE AN OFFER" : "APRESENTAR PROPOSTA");
            _staffNegotiationStatus = BroadcastUi.Label(offers.transform, "Status", english
                ? "Choose a weekly wage and contract length. The candidate may accept or turn it down."
                : "Escolhe o salário semanal e a duração. O candidato pode aceitar ou recusar.", 14, UiTheme.Muted, TextAnchor.MiddleLeft);
            _staffNegotiationStatus.horizontalOverflow = HorizontalWrapMode.Wrap;
            BroadcastUi.Place(_staffNegotiationStatus.rectTransform, 0.04f, 0.76f, 0.96f, 0.94f);
            var wages = new[] { Mathf.RoundToInt(candidate.AskingWage * 0.9f), candidate.AskingWage, Mathf.RoundToInt(candidate.AskingWage * 1.12f) };
            var years = new[] { 2, Mathf.Clamp(candidate.ContractYears, 2, 3), 4 };
            var labels = new[] { english ? "CAUTIOUS" : "PRUDENTE", english ? "ASKING LINE" : "VALOR PEDIDO", english ? "IMPROVED" : "MELHORADA" };
            for (var i = 0; i < wages.Length; i++)
            {
                var offerWage = wages[i];
                var contractYears = years[i];
                var x0 = 0.035f + i * 0.32f;
                var card = BroadcastUi.Panel(offers.transform, "Offer" + i, new Color(0.12f, 0.15f, 0.21f, 1f), true);
                BroadcastUi.Place(card.rectTransform, x0, 0.08f, x0 + 0.29f, 0.7f);
                var caption = BroadcastUi.Label(card.transform, "Caption", labels[i], 12, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(caption.rectTransform, 0.06f, 0.7f, 0.94f, 0.94f);
                var money = BroadcastUi.Label(card.transform, "Money", Euro(offerWage) + (english ? "/wk" : "/sem"), 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(money.rectTransform, 0.04f, 0.43f, 0.96f, 0.72f);
                var term = BroadcastUi.Label(card.transform, "Years", contractYears + (english ? " years" : " anos"), 14, UiTheme.Muted, TextAnchor.MiddleCenter);
                BroadcastUi.Place(term.rectTransform, 0.04f, 0.28f, 0.96f, 0.45f);
                var propose = BroadcastUi.Button(card.transform, "Propose", english ? "PROPOSE" : "PROPOR",
                    () => ResolveStaffContract(candidate, offerWage, contractYears, phoneKey), UiTheme.Accent, UiTheme.ButtonText, 13);
                BroadcastUi.Place(propose.GetComponent<RectTransform>(), 0.06f, 0.06f, 0.94f, 0.26f);
                propose.interactable = ClubStaffCount() < MaxClubStaff;
            }
        }

        int StaffWeeklyWageRoom()
        {
            if (_career?.Club == null) return 0;
            var budget = ClubFinanceLedger.EnsureWeeklyWageBudget(_career, _career.Club);
            long payroll = 0;
            foreach (var player in Squad(_career.Club))
                payroll += Math.Max(0, player.WeeklyWage);
            if (_career.Club.Staff != null)
                foreach (var member in _career.Club.Staff)
                    if (member != null)
                        payroll += Math.Max(0, member.WeeklyWage);
            return Mathf.Max(0, (int)Math.Min(int.MaxValue, budget - payroll));
        }

        void ResolveStaffContract(StaffOfferCandidate candidate, int wage, int years, string phoneKey)
        {
            if (candidate?.Member == null || _career?.Club == null) return;
            var english = LocalizationManager.Current == GameLanguage.English;
            if (ClubStaffCount() >= MaxClubStaff)
            {
                if (_staffNegotiationStatus != null)
                    _staffNegotiationStatus.text = english
                        ? "The staff roster is full. Remove a staff member before hiring another."
                        : "A equipa técnica está completa. Remove um membro antes de contratar outro.";
                return;
            }
            if (wage > StaffWeeklyWageRoom())
            {
                var gap = wage - StaffWeeklyWageRoom();
                if (gap > 0 && gap <= 250000)
                {
                    if (DeskDue("staff-budget-wait", out var wait) && new DateTime(wait).Date > _career.Calendar.CurrentDate.Date)
                    {
                        if (_staffNegotiationStatus != null)
                            _staffNegotiationStatus.text = english
                                ? "The board has already reviewed a staff wage request. You can ask again on " + LocalizationManager.FormatDate(new DateTime(wait), "d MMM") + "."
                                : "A direção já analisou um pedido salarial do staff. Podes voltar a pedir a " + LocalizationManager.FormatDate(new DateTime(wait), "d MMM") + ".";
                        return;
                    }
                    OpenBoardMeeting(null, 0, wage, years, 0, gap, candidate, phoneKey);
                    return;
                }
                if (_staffNegotiationStatus != null)
                    _staffNegotiationStatus.text = english
                        ? "That wage exceeds the available room by too much for a board request."
                        : "Esse salário ultrapassa demasiado a folga disponível para pedir reforço à direção.";
                return;
            }
            var accepted = wage >= candidate.AskingWage && years >= candidate.ContractYears;
            var counter = !accepted && wage >= Mathf.RoundToInt(candidate.AskingWage * 0.75f);
            var counterWage = Math.Max(candidate.AskingWage, wage);
            var counterYears = Math.Max(candidate.ContractYears, years);
            if (accepted)
            {
                if (_staffNegotiationStatus != null)
                    _staffNegotiationStatus.text = english
                        ? candidate.Member.FullName + " accepts " + Euro(wage) + "/week for " + years + " years. Confirm the agreement to complete the signing."
                        : candidate.Member.FullName + " aceita " + Euro(wage) + "/semana por " + years + " anos. Confirma o acordo para concluir a contratação.";
            }
            else if (counter)
            {
                if (_staffNegotiationStatus != null)
                    _staffNegotiationStatus.text = english
                        ? candidate.Member.FullName + " replies with a counter-offer: " + Euro(counterWage) + "/week for " + counterYears + " years."
                        : candidate.Member.FullName + " responde com uma contraproposta: " + Euro(counterWage) + "/semana por " + counterYears + " anos.";
            }
            else if (_staffNegotiationStatus != null)
                _staffNegotiationStatus.text = english
                    ? candidate.Member.FullName + " rejects the offer. Raise the wage or try another candidate."
                    : candidate.Member.FullName + " recusa a proposta. Aumenta o salário ou tenta outro candidato.";

            ShowStaffNegotiationResponse(candidate, wage, years, phoneKey, accepted, counter, counterWage, counterYears);
        }

        void ShowStaffNegotiationResponse(StaffOfferCandidate candidate, int offeredWage, int offeredYears, string phoneKey,
            bool accepted, bool counter, int counterWage, int counterYears)
        {
            var offers = transform.Find("StaffNegotiation/OfferChoices");
            if (offers == null)
                return;
            for (var i = offers.childCount - 1; i >= 0; i--)
            {
                var child = offers.GetChild(i);
                if (child.name.StartsWith("Offer", StringComparison.Ordinal) || child.name.StartsWith("Response", StringComparison.Ordinal))
                    Destroy(child.gameObject);
            }

            var english = LocalizationManager.Current == GameLanguage.English;
            if (accepted)
            {
                var confirm = BroadcastUi.Button(offers, "ResponseConfirm", english ? "FINALISE CONTRACT" : "FINALIZAR CONTRATO",
                    () => FinalizeStaffContract(candidate, offeredWage, offeredYears, phoneKey), UiTheme.Accent, UiTheme.ButtonText, 13);
                BroadcastUi.Place(confirm.GetComponent<RectTransform>(), 0.035f, 0.2f, 0.48f, 0.52f);
                var revise = BroadcastUi.Button(offers, "ResponseRevise", english ? "REVISE OFFER" : "REFORMULAR PROPOSTA",
                    () => OpenStaffContractOffer(candidate, phoneKey), new Color(1f, 1f, 1f, 0.08f), Color.white, 13);
                BroadcastUi.Place(revise.GetComponent<RectTransform>(), 0.52f, 0.2f, 0.965f, 0.52f);
                return;
            }

            if (counter)
            {
                var counterButton = BroadcastUi.Button(offers, "ResponseCounter", english
                    ? "ACCEPT COUNTER-OFFER  ·  " + Euro(counterWage) + "/wk  ·  " + counterYears + "y"
                    : "ACEITAR CONTRAPROPOSTA  ·  " + Euro(counterWage) + "/sem  ·  " + counterYears + " anos",
                    () => FinalizeStaffContract(candidate, counterWage, counterYears, phoneKey), UiTheme.Accent, UiTheme.ButtonText, 12);
                BroadcastUi.Place(counterButton.GetComponent<RectTransform>(), 0.035f, 0.2f, 0.52f, 0.52f);
                counterButton.interactable = counterWage <= StaffWeeklyWageRoom() && ClubStaffCount() < MaxClubStaff;
                var revise = BroadcastUi.Button(offers, "ResponseRevise", english ? "MAKE ANOTHER OFFER" : "FAZER OUTRA PROPOSTA",
                    () => OpenStaffContractOffer(candidate, phoneKey), new Color(1f, 1f, 1f, 0.08f), Color.white, 12);
                BroadcastUi.Place(revise.GetComponent<RectTransform>(), 0.56f, 0.2f, 0.965f, 0.52f);
                return;
            }

            var retry = BroadcastUi.Button(offers, "ResponseRetry", english ? "TRY ANOTHER OFFER" : "TENTAR OUTRA PROPOSTA",
                () => OpenStaffContractOffer(candidate, phoneKey), new Color(1f, 1f, 1f, 0.08f), Color.white, 13);
            BroadcastUi.Place(retry.GetComponent<RectTransform>(), 0.2f, 0.2f, 0.8f, 0.52f);
        }

        void FinalizeStaffContract(StaffOfferCandidate candidate, int wage, int years, string phoneKey)
        {
            if (candidate?.Member == null || _career?.Club == null) return;
            var english = LocalizationManager.Current == GameLanguage.English;
            if (ClubStaffCount() >= MaxClubStaff)
            {
                if (_staffNegotiationStatus != null)
                    _staffNegotiationStatus.text = english ? "The staff roster is full. Remove a staff member before signing." : "A equipa técnica está completa. Remove um membro antes de assinar.";
                return;
            }
            if (wage > StaffWeeklyWageRoom())
            {
                if (_staffNegotiationStatus != null)
                    _staffNegotiationStatus.text = english ? "That wage is above the club's available wage budget." : "Esse salário ultrapassa a verba salarial disponível.";
                return;
            }

            HireRecommended(candidate.Member, wage, years, candidate.Member.Id);
            if (!string.IsNullOrEmpty(phoneKey))
            {
                var remaining = StaffOfferCandidates(candidate.Week);
                ClosePhoneChoices();
                if (remaining.Count == 0)
                {
                    MarkPhoneAnswered(phoneKey);
                    AddPhoneLine(english ? "Contract agreed with " + candidate.Member.FullName + "." : "Contrato acordado com " + candidate.Member.FullName + ".");
                }
                else if (_activePhoneNote != null)
                    OpenThread(_activePhoneNote);
            }
            if (!string.IsNullOrEmpty(phoneKey)) CareerStore.Save(_career);
            var sheet = transform.Find("StaffNegotiation");
            if (sheet != null) Destroy(sheet.gameObject);
            _staffNegotiationStatus = null;
        }

        void CloseStaffSheet()
        {
            if (_staffSheet == null)
                return;
            Destroy(_staffSheet);
            _staffSheet = null;
        }

        static void StaffFact(Transform parent, string name, string text, float x0, float y0, float x1, float y1)
        {
            var label = BroadcastUi.Label(parent, name, text, 14, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(label.rectTransform, x0, y0, x1, y1);
        }

        void OpenMeeting(StaffMember current)
        {
            _offer = StaffReport.Offer(_career.Club, current.Role);
            _modalLineSize = 20;
            OpenModal(LocalizationManager.Current == GameLanguage.English ? "Report to the board" : "Relatório à direção", PresentationLabels.StaffRole(current.Role), true);
            AddLine(LocalizationManager.Current == GameLanguage.English
                ? "The report is ready as if you had dictated it to your assistant. The board will read it after you sign."
                : "O texto já está escrito, como se o tivesses ditado ao adjunto. A direção só lê depois da tua assinatura.");
            AddLine(ReportText(current, _offer));
            _modalAction.onClick.RemoveAllListeners();
            _modalActionLabel.text = LocalizationManager.Translate("ASSINAR E ENTREGAR");
            var captured = current;
            _modalAction.onClick.AddListener(() => DeliverReport(captured));
        }

        void DeliverReport(StaffMember current)
        {
            if (_offer == null)
                return;
            _career.Board.Insert(0, new CoachNote
            {
                Day = _career.Calendar.WeekdayName,
                Hour = _career.Calendar.ClockText,
                Text = LocalizationManager.Current == GameLanguage.English
                    ? "I signed the request to replace " + current.FullName + ". The decision is now with the board."
                    : "Assinei o pedido de saída de " + current.FullName + ". A decisão fica com o presidente."
            });
            if (!PresidentAgrees(current, _offer))
            {
                _career.Club.DressingRoomTension = Mathf.Clamp(_career.Club.DressingRoomTension + 4, 0, 100);
                OpenModal(LocalizationManager.Current == GameLanguage.English ? "The board said no" : "A direção não autoriza", current.FullName, false);
                AddLine(LocalizationManager.Current == GameLanguage.English
                    ? "The board has rejected the request. " + current.FullName + " remains under contract; only the board can terminate it."
                    : "O presidente ouviu o pedido e recusou a rescisão. O contrato de " + current.FullName + " mantém-se. Quem tem autoridade para o terminar é a direção, não o treinador.");
                AddLine(VibeLine());
                _offer = null;
                FillStaff();
                FillBoard();
                Refresh();
                return;
            }

            ConfirmHire();
        }

        bool PresidentAgrees(StaffMember current, StaffMember offer)
        {
            var gap = offer.Judgement - current.Judgement;
            var score = 48 + gap * 9 - _career.Club.DressingRoomTension / 4;
            if (score < 18) score = 18;
            if (score > 82) score = 82;
            var roll = 0;
            var text = current.Id + "|" + offer.FullName + "|" + _career.Club.DressingRoomTension + "|" + _career.Calendar.CurrentDate.DayOfYear;
            unchecked
            {
                foreach (var c in text)
                    roll = roll * 31 + c;
            }

            return (roll & 0x7fffffff) % 100 < score;
        }

        static string ReportText(StaffMember current, StaffMember offer)
        {
            if (LocalizationManager.Current == GameLanguage.English)
                return "I ask the board to consider replacing " + current.FullName + ", " + PresentationLabels.StaffRole(current.Role).ToLowerInvariant() + ". "
                    + LocalizationManager.Translate(StaffReport.Quality(current)) + " I propose " + offer.FullName + " as a replacement. The decision to end the contract is yours.";
            return "Peço que a direção avalie a saída de " + current.FullName + ", " + PresentationLabels.StaffRole(current.Role).ToLowerInvariant() + ". " +
                   StaffReport.Quality(current) + " Proponho " + offer.FullName + " para o lugar. A decisão de terminar o contrato é vossa.";
        }

        void ConfirmHire()
        {
            if (_offer == null)
                return;
            var staff = _career.Club.Staff;
            for (var i = 0; i < staff.Count; i++)
            {
                if (staff[i].Role != _offer.Role)
                    continue;
                var cost = 6 + Mathf.Max(0, _offer.Judgement - staff[i].Judgement);
                staff[i] = _offer;
                _career.Club.DressingRoomTension = Mathf.Clamp(_career.Club.DressingRoomTension + cost, 0, 100);
                break;
            }

            _offer = null;
            CloseModal();
            FillStaff();
            FillTeam();
            FillCalendar();
            Refresh();
        }

        void OpenTrips()
        {
            var tension = _career.Club.DressingRoomTension;
            var next = _career.NextUserFixture();
            var free = next == null ? 4 : (next.Date.Date - _career.Calendar.CurrentDate.Date).Days;
            if (LocalizationManager.Current == GameLanguage.English)
            {
                OpenModal("Plan a team trip", free <= 0 ? "The next match is imminent. There is no time to arrange a trip." : "There are " + free + " day(s) before the next match. Your assistant suggests an activity for the squad.", false);
                if (free <= 0) return;
                TripRow("Short break", "Pair players from different groups in shared rooms. Best when the dressing room is divided.", () => RunTrip(0));
                TripRow("Squad lunch", "Bring everyone to one table. Give quieter players a chance to sit with the talkers.", () => RunTrip(1));
                TripRow("Day in town", "No training. The squad gets some rest and spends the day together.", () => RunTrip(2));
                TripRow("Tactical camp", "A neutral training ground, focused on the plan. It builds familiarity without changing morale.", () => RunTrip(3));
                if (tension >= 55)
                    AddBody("The dressing room is " + RoomWord(tension) + ". The short break is the best option for bringing estranged teammates together.");
                return;
            }
            OpenModal("Marcar viagem", free <= 0 ? "O próximo jogo é já. A viagem fica para depois." : "Há " + free + " dia(s) antes do próximo jogo. O adjunto sugere o que o grupo precisa.", false);
            if (free <= 0)
                return;
            TripRow("Férias curtas", "Dois de grupos diferentes em cada quarto. Serve quando o balneário está partido.", () => RunTrip(0));
            TripRow("Almoço de grupo", "Uma mesa só. Quem anda calado senta-se ao lado de quem fala.", () => RunTrip(1));
            TripRow("Dia na cidade", "Sem treino. O cansaço desce e o grupo sai junto.", () => RunTrip(2));
            TripRow("Estágio tático", "Campo neutro, só o plano. Não mexe no humor, fixa a ideia de jogo.", () => RunTrip(3));
            if (tension >= 55)
                AddBody("O vestiário está " + RoomWord(tension) + ". As férias curtas são a opção que mais junta gente que não se fala.");
        }

        void TripRow(string title, string detail, Action run)
        {
            var row = BroadcastUi.Button(_modalBody, title, title + "\n" + detail, run, new Color(0.16f, 0.07f, 0.3f, 1f), Color.white, 14);
            BroadcastUi.Preferred(row, 72);
        }

        void RunTrip(int kind)
        {
            if (PressDutyOpen())
                return;
            var squad = Squad(_career.Club);
            squad.Sort((a, b) => a.Morale.CompareTo(b.Morale));
            if (LocalizationManager.Current == GameLanguage.English)
            {
                var englishText = "The squad activity is arranged.";
                if (kind == 0)
                {
                    var pairs = 0;
                    for (var i = 0; i < squad.Count / 2; i++)
                    {
                        var low = squad[i];
                        var high = squad[squad.Count - 1 - i];
                        if (low == null || high == null || low.Id == high.Id) continue;
                        low.Morale = Mathf.Clamp(low.Morale + 8, 1, 99);
                        high.Morale = Mathf.Clamp(high.Morale + 4, 1, 99);
                        pairs++;
                    }
                    _career.Club.DressingRoomTension = Mathf.Clamp(_career.Club.DressingRoomTension - 14, 0, 100);
                    englishText = "Short break. " + pairs + " rooms paired low-morale players with teammates from another group. The dressing room is now " + RoomWord(_career.Club.DressingRoomTension) + ".";
                }
                else if (kind == 1)
                {
                    foreach (var player in squad)
                        if (player.Morale < 50) player.Morale = Mathf.Clamp(player.Morale + 6, 1, 99);
                    _career.Club.DressingRoomTension = Mathf.Clamp(_career.Club.DressingRoomTension - 8, 0, 100);
                    englishText = "Squad lunch. The quieter players sat with the ones who keep the conversation going. The dressing room is now " + RoomWord(_career.Club.DressingRoomTension) + ".";
                }
                else if (kind == 2)
                {
                    foreach (var player in squad)
                    {
                        player.Morale = Mathf.Clamp(player.Morale + 4, 1, 99);
                        player.Fatigue = Mathf.Max(1, player.Fatigue - 8);
                    }
                    _career.Club.DressingRoomTension = Mathf.Clamp(_career.Club.DressingRoomTension - 6, 0, 100);
                    englishText = "A day in town, with no training. Fatigue dropped and the squad spent time together. The dressing room is now " + RoomWord(_career.Club.DressingRoomTension) + ".";
                }
                else
                    englishText = "Tactical camp at a neutral ground. The plan stays the same: " + TacticPresets.Active(_career.Club.Tactics) + ".";

                AdvanceCareerDays(1);
                _career.Board.Insert(0, new CoachNote { Day = _career.Calendar.WeekdayName, Hour = _career.Calendar.ClockText, When = _career.Calendar.CurrentDate, Text = englishText });
                if (SettingsPrefs.AutosaveEnabled)
                    CareerStore.AutoSaveIfDue(_career, SettingsPrefs.AutosaveDays, SettingsPrefs.AutosaveSlots);
                CareerStore.Save(_career);
                CloseModal();
                FillBoard();
                Refresh();
                return;
            }
            var text = "A viagem ficou marcada.";
            if (kind == 0)
            {
                var pairs = 0;
                for (var i = 0; i < squad.Count / 2; i++)
                {
                    var low = squad[i];
                    var high = squad[squad.Count - 1 - i];
                    if (low == null || high == null || low.Id == high.Id)
                        continue;
                    low.Morale = Mathf.Clamp(low.Morale + 8, 1, 99);
                    high.Morale = Mathf.Clamp(high.Morale + 4, 1, 99);
                    pairs++;
                }

                _career.Club.DressingRoomTension = Mathf.Clamp(_career.Club.DressingRoomTension - 14, 0, 100);
                text = "Férias curtas. " + pairs + " quartos juntaram alguém de moral baixa com alguém do outro grupo. O vestiário fica " + RoomWord(_career.Club.DressingRoomTension) + ".";
            }
            else if (kind == 1)
            {
                foreach (var player in squad)
                {
                    if (player.Morale < 50)
                        player.Morale = Mathf.Clamp(player.Morale + 6, 1, 99);
                }

                _career.Club.DressingRoomTension = Mathf.Clamp(_career.Club.DressingRoomTension - 8, 0, 100);
                text = "Almoço de grupo. Quem falava pouco sentou-se ao lado de quem puxa a conversa. O vestiário fica " + RoomWord(_career.Club.DressingRoomTension) + ".";
            }
            else if (kind == 2)
            {
                foreach (var player in squad)
                {
                    player.Morale = Mathf.Clamp(player.Morale + 4, 1, 99);
                    player.Fatigue = Mathf.Max(1, player.Fatigue - 8);
                }

                _career.Club.DressingRoomTension = Mathf.Clamp(_career.Club.DressingRoomTension - 6, 0, 100);
                text = "Dia na cidade, sem treino. O cansaço desce e o vestiário fica " + RoomWord(_career.Club.DressingRoomTension) + ".";
            }
            else
            {
                text = "Estágio tático em campo neutro. O plano mantém-se: " + TacticPresets.Active(_career.Club.Tactics) + ".";
            }

            AdvanceCareerDays(1);
            _career.Board.Insert(0, new CoachNote
            {
                Day = _career.Calendar.WeekdayName,
                Hour = _career.Calendar.ClockText,
                When = _career.Calendar.CurrentDate,
                Text = text
            });
            if (SettingsPrefs.AutosaveEnabled)
                CareerStore.AutoSaveIfDue(_career, SettingsPrefs.AutosaveDays, SettingsPrefs.AutosaveSlots);
            CareerStore.Save(_career);
            CloseModal();
            Refresh();
            FillCalendar();
            FillBoard();
        }

        void AdvanceCareerDays(int days)
        {
            if (PressDutyOpen())
                return;
            days = Mathf.Max(1, days);
            var previousDay = _career.Calendar.CurrentDate.Date;
            AdvancePlayerSystems(days);
            _career.Calendar.AdvanceDays(days);
            var advancedThrough = _career.Calendar.CurrentDate.Date;
            for (var day = previousDay.AddDays(1); day <= advancedThrough; day = day.AddDays(1))
                if (day.Day == 1)
                    WorldCatalog.SimulateManagerAppointments(_career.World, day, _career.Club.Id);
            ClubFinanceLedger.SettleWeeklyPayroll(_career, previousDay, _career.Calendar.CurrentDate.Date);
            ClubFinanceLedger.SettleWeeklyCommercialIncome(_career, previousDay, _career.Calendar.CurrentDate.Date);
        }

        void AdvancePlayerSystems(int days)
        {
            days = Mathf.Max(1, days);
            foreach (var player in _career.World.Players.Values)
            {
                var resting = player.ClubId == _career.Club.Id && player.TrainingFocus == "rest";
                player.Fatigue = Mathf.Max(0, player.Fatigue - (resting ? 8 : 3) * days);
                if (player.Injury == null || !player.Injury.IsInjured)
                    continue;
                player.Injury.DaysRemaining = Mathf.Max(0, player.Injury.DaysRemaining - days);
                if (player.Injury.DaysRemaining == 0)
                {
                    player.Injury.IsInjured = false;
                    player.Injury.Description = "";
                }
            }

            if (SettingsPrefs.AutomaticTraining)
                ApplyTraining(days);
            else
                ApplyFocusedTraining(days);
            MaybeTrainingReport();
        }

        void ApplyTraining(int days)
        {
            foreach (var player in _career.World.Players.Values)
            {
                if (player.Injury != null && player.Injury.IsInjured)
                    continue;
                var current = player.TrainingRating <= 0 ? 70 : player.TrainingRating;
                var youthBonus = player.Age <= 21 ? 6 : player.Age <= 25 ? 3 : 0;
                var target = Mathf.Clamp(48 + player.Motivation / 3 + player.Morale / 8 + youthBonus, 50, 94);
                var blend = 1f - Mathf.Pow(0.92f, days);
                player.TrainingRating = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(current, target, blend)), 1, 100);
            }
            ApplyFocusedTraining(days);
        }

        void ApplyFocusedTraining(int days)
        {
            days = Mathf.Max(1, days);
            var trainees = Squad(_career.Club);
            foreach (var id in _career.AcademyPlayerIds)
            {
                if (!_career.World.Players.TryGetValue(id, out var youth) || youth.ClubId != _career.Club.Id)
                    continue;
                if (trainees.Find(player => player.Id == youth.Id) == null)
                    trainees.Add(youth);
            }
            var today = _career.Calendar.CurrentDate.DayOfYear + _career.Calendar.CurrentDate.Year * 400;
            foreach (var player in trainees)
            {
                if (player.Injury != null && player.Injury.IsInjured)
                    continue;
                var intensity = Mathf.Clamp(player.TrainingIntensity, 0, 2);
                if (player.TrainingFocus == "rest")
                {
                    player.Fatigue = Mathf.Max(0, player.Fatigue - 4 * days);
                    continue;
                }
                if (string.IsNullOrEmpty(player.TrainingFocus))
                    continue;
                var sessions = Mathf.Min(days, 14);
                for (var session = 0; session < sessions; session++)
                {
                    var attr = ReadTrainingAttribute(player);
                    var gain = player.Age <= 21 ? 28 : player.Age <= 25 ? 20 : 14;
                    if (attr >= 18) gain = Mathf.RoundToInt(gain * 0.28f);
                    else if (attr >= 16) gain = Mathf.RoundToInt(gain * 0.45f);
                    else if (attr >= 14) gain = Mathf.RoundToInt(gain * 0.7f);
                    if (intensity == 0) gain = Mathf.RoundToInt(gain * 0.6f);
                    if (intensity == 2) gain = Mathf.RoundToInt(gain * 1.6f);
                    var tired = player.Fatigue > 80 || player.Morale < 35;
                    if (tired)
                    {
                        player.TrainingStreak = 0;
                        gain = Mathf.Max(1, gain / 3);
                    }
                    player.TrainingProgress += Mathf.Max(1, gain);
                    var load = intensity == 2 ? 5 : intensity == 0 ? 1 : 2;
                    player.Fatigue = Mathf.Min(100, player.Fatigue + load);
                    var roll = TrainingRoll(player.Id, today - (sessions - 1 - session));
                    var chance = intensity == 2 ? (player.Fatigue > 55 ? 6 : 3) : intensity == 1 && player.Fatigue > 75 ? 2 : 0;
                    if (roll < chance)
                    {
                        if (player.Injury == null)
                            player.Injury = new InjuryState();
                        player.Injury.IsInjured = true;
                        player.Injury.DaysRemaining = 5 + (roll % 8);
                        player.Injury.Description = "Carga de treino";
                        player.TrainingStreak = 0;
                        break;
                    }
                    while (player.TrainingProgress >= 100)
                    {
                        player.TrainingProgress -= 100;
                        if (tired)
                            continue;
                        player.TrainingStreak++;
                        var need = attr >= 18 ? 8 : attr >= 16 ? 6 : attr >= 14 ? 4 : 3;
                        if (player.TrainingStreak < need)
                            continue;
                        player.TrainingStreak = 0;
                        RaiseTrainingAttribute(player);
                        attr = ReadTrainingAttribute(player);
                    }
                }
            }
        }

        void OpenLeagueStats(string leagueId)
        {
            var name = leagueId;
            foreach (var league in _career.World.Leagues)
            {
                if (league.Id == leagueId)
                    name = LocalizationManager.Translate(league.Name);
            }

            OpenModal(name, "A tabela completa e os números da época.", false);
            BroadcastUi.Place(_modalLead.rectTransform, 0.05f, 0.82f, 0.7f, 0.88f);
            BroadcastUi.Place(_modalTitle.rectTransform, 0.14f, 0.88f, 0.91f, 0.97f);
            var card = _modal.transform.Find("Card");
            var competitionBadge = CrestStudio.ForCompetition(leagueId);
            if (competitionBadge != null)
            {
                var badgeObject = new GameObject("CompetitionBadge", typeof(RectTransform), typeof(Image));
                badgeObject.transform.SetParent(card, false);
                var badgeImage = badgeObject.GetComponent<Image>();
                badgeImage.sprite = competitionBadge;
                badgeImage.preserveAspect = true;
                badgeImage.raycastTarget = false;
                BroadcastUi.Place(badgeObject.GetComponent<RectTransform>(), 0.055f, 0.89f, 0.115f, 0.97f);
            }
            var tabs = BroadcastUi.Zone(card, "StatsTabs", 0.04f, 0.72f, 0.96f, 0.81f);
            var scroll = card.Find("Scroll") as RectTransform;
            if (scroll != null)
                BroadcastUi.Place(scroll, 0.04f, 0.08f, 0.96f, 0.7f);
            var labels = new[] { "Tabela", "Marcadores", "Assistências", "Médias", "Clubes" };
            var buttons = new Button[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                var page = i;
                var x0 = i / (float)labels.Length;
                var x1 = (i + 1) / (float)labels.Length;
                buttons[i] = BroadcastUi.Button(tabs, "Tab" + i, labels[i], () =>
                {
                    PaintLeaguePage(leagueId, page, buttons);
                }, new Color(1f, 1f, 1f, 0.08f), Color.white, 14);
                BroadcastUi.Place(buttons[i].GetComponent<RectTransform>(), x0 + 0.008f, 0.08f, x1 - 0.008f, 0.92f);
            }

            PaintLeaguePage(leagueId, 0, buttons);
        }

        void PaintLeaguePage(string leagueId, int page, Button[] tabs)
        {
            Clear(_modalBody);
            if (tabs != null)
            {
                for (var i = 0; i < tabs.Length; i++)
                {
                    if (tabs[i] == null)
                        continue;
                    var on = i == page;
                    tabs[i].GetComponent<Image>().color = on ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f);
                    var label = tabs[i].GetComponentInChildren<Text>();
                    if (label != null)
                        label.color = on ? UiTheme.ButtonText : Color.white;
                }
            }

            if (page == 0)
            {
                FillFullTable(leagueId);
                return;
            }

            if (page == 1)
            {
                AddHead("Melhores marcadores");
                AddRanks(SeasonBook.Ranks(_career, leagueId, "golos"));
                return;
            }

            if (page == 2)
            {
                AddHead("Assistências");
                AddRanks(SeasonBook.Ranks(_career, leagueId, "assist"));
                return;
            }

            if (page == 3)
            {
                AddHead("Melhores médias");
                AddRanks(SeasonBook.Ranks(_career, leagueId, "media"));
                return;
            }

            AddHead("Clubes com mais golos");
            AddClubRanks(leagueId, "gm");
            AddHead("Clubes com menos golos sofridos");
            AddClubRanks(leagueId, "gs");
            AddHead("Mais vitórias");
            AddClubRanks(leagueId, "v");
            AddHead("Melhor diferença de golos");
            AddClubRanks(leagueId, "dg");
        }

        void FillFullTable(string leagueId)
        {
            var rows = new List<LeagueTableRow>();
            foreach (var row in _career.Table)
            {
                if (row.LeagueId == leagueId)
                    rows.Add(row);
            }

            rows.Sort((a, b) =>
            {
                var points = b.Points.CompareTo(a.Points);
                if (points != 0)
                    return points;
                var diff = b.GoalDifference.CompareTo(a.GoalDifference);
                return diff != 0 ? diff : b.GoalsFor.CompareTo(a.GoalsFor);
            });
            if (rows.Count == 0)
            {
                AddLine("Ainda não há jogos fechados.");
                return;
            }

            AddHead("Classificação");
            AddStandingsHeader();
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                _career.World.Clubs.TryGetValue(row.ClubId, out var club);
                AddStandingsRow(i + 1, row, club);
            }
        }

        void AddStandingsHeader()
        {
            var row = BroadcastUi.Panel(_modalBody, "StandingsHeader", new Color(0.22f, 0.12f, 0.38f, 1f), true);
            BroadcastUi.Preferred(row, 34);
            var headings = LocalizationManager.Current == GameLanguage.English
                ? new[] { "PTS", "P", "W", "D", "L", "GF", "GA" }
                : new[] { "PTS", "J", "V", "E", "D", "GM", "GS" };
            for (var i = 0; i < headings.Length; i++)
            {
                var x0 = 0.39f + i * 0.084f;
                var label = BroadcastUi.Label(row.transform, "Heading" + i, headings[i], 12, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(label.rectTransform, x0, 0.05f, x0 + 0.084f, 0.95f);
            }
        }

        void AddStandingsRow(int place, LeagueTableRow stats, Club club)
        {
            var guest = OtherHumanSeat(stats.ClubId);
            var guestColor = guest == null ? Color.white : BroadcastUi.Parse(guest.Color, new Color(0.88f, 0.48f, 0.24f, 1f));
            var mine = stats.ClubId == _career.Club.Id;
            var tone = mine
                ? new Color(0.18f, 0.12f, 0.3f, 1f)
                : guest != null
                    ? new Color(guestColor.r * 0.45f, guestColor.g * 0.45f, guestColor.b * 0.45f, 1f)
                    : new Color(0.1f, 0.04f, 0.2f, 0.94f);
            var row = BroadcastUi.Panel(_modalBody, "Standing" + place + stats.ClubId, tone, true);
            BroadcastUi.Preferred(row, 36);
            if (club != null)
            {
                var captured = club;
                BroadcastUi.Hit(row, () => OpenClubDossier(captured));
            }
            var index = BroadcastUi.Label(row.transform, "Place", place.ToString(), 14, place == 1 ? UiTheme.Score : UiTheme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(index.rectTransform, 0.005f, 0.05f, 0.065f, 0.95f);
            if (club != null)
            {
                var badge = new GameObject("Badge", typeof(RectTransform), typeof(Image));
                badge.transform.SetParent(row.transform, false);
                var badgeRect = badge.GetComponent<RectTransform>();
                BroadcastUi.Place(badgeRect, 0.075f, 0.16f, 0.125f, 0.84f);
                var image = badge.GetComponent<Image>();
                image.sprite = CrestStudio.For(club);
                image.preserveAspect = true;
                image.raycastTarget = false;
            }

            var name = BroadcastUi.Label(row.transform, "Club", club == null ? stats.ClubId : club.ShortName, 14, mine ? UiTheme.Score : guest != null ? guestColor : Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 9;
            name.resizeTextMaxSize = 14;
            BroadcastUi.Place(name.rectTransform, 0.135f, 0.05f, 0.385f, 0.95f);
            var values = new[]
            {
                stats.Points.ToString(), stats.Played.ToString(), stats.Won.ToString(),
                stats.Drawn.ToString(), stats.Lost.ToString(), stats.GoalsFor.ToString(), stats.GoalsAgainst.ToString()
            };
            for (var i = 0; i < values.Length; i++)
            {
                var x0 = 0.39f + i * 0.084f;
                var value = BroadcastUi.Label(row.transform, "Stat" + i, values[i], 14, Color.white, TextAnchor.MiddleCenter, i == 0 ? FontStyle.Bold : FontStyle.Normal);
                BroadcastUi.Place(value.rectTransform, x0, 0.05f, x0 + 0.084f, 0.95f);
            }
            PassClicksTo(row.transform);
        }

        void AddRanks(List<SeasonRank> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                AddLine("Ainda não há números nesta lista.");
                return;
            }

            for (var i = 0; i < rows.Count; i++)
                AddRankRow(i + 1, rows[i].Name, rows[i].Club, rows[i].Value);
        }

        void AddClubRanks(string leagueId, string kind)
        {
            var rows = new List<LeagueTableRow>();
            foreach (var row in _career.Table)
            {
                if (row.LeagueId == leagueId && row.Played > 0)
                    rows.Add(row);
            }

            if (kind == "gm")
                rows.Sort((a, b) => b.GoalsFor.CompareTo(a.GoalsFor));
            else if (kind == "gs")
                rows.Sort((a, b) => a.GoalsAgainst.CompareTo(b.GoalsAgainst));
            else if (kind == "v")
                rows.Sort((a, b) => b.Won.CompareTo(a.Won));
            else
                rows.Sort((a, b) => b.GoalDifference.CompareTo(a.GoalDifference));
            if (rows.Count == 0)
            {
                AddLine("Ainda não há jogos fechados.");
                return;
            }

            var shown = Mathf.Min(5, rows.Count);
            for (var i = 0; i < shown; i++)
            {
                var row = rows[i];
                _career.World.Clubs.TryGetValue(row.ClubId, out var ranked);
                var club = ranked == null ? row.ClubId : ranked.ShortName;
                var value = LocalizationManager.Current == GameLanguage.English
                    ? kind == "gm" ? row.GoalsFor + " goals" : kind == "gs" ? row.GoalsAgainst + " conceded" : kind == "v" ? row.Won + " wins" : (row.GoalDifference > 0 ? "+" : "") + row.GoalDifference
                    : kind == "gm" ? row.GoalsFor + " golos" : kind == "gs" ? row.GoalsAgainst + " sofridos" : kind == "v" ? row.Won + " vitórias" : (row.GoalDifference > 0 ? "+" : "") + row.GoalDifference;
                var captured = ranked;
                AddRankRow(i + 1, club, "", value, captured == null ? (Action)null : () => OpenClubDossier(captured));
            }
        }

        void AddRankRow(int place, string name, string club, string value, Action click = null)
        {
            var tone = place == 1
                ? new Color(0.42f, 0.28f, 0.08f, 0.96f)
                : place == 2
                    ? new Color(0.22f, 0.2f, 0.24f, 0.96f)
                    : place == 3
                        ? new Color(0.28f, 0.14f, 0.08f, 0.96f)
                        : new Color(0.1f, 0.04f, 0.2f, 0.94f);
            var row = BroadcastUi.Panel(_modalBody, "Rank" + place + name, tone, true);
            var layout = row.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 48;
            layout.minHeight = 48;
            var placeColor = place == 1 ? new Color(1f, 0.82f, 0.35f) : place == 2 ? new Color(0.86f, 0.88f, 0.92f) : place == 3 ? new Color(0.86f, 0.58f, 0.34f) : UiTheme.Muted;
            var index = BroadcastUi.Label(row.transform, "Place", place.ToString(), 20, placeColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(index.rectTransform, 0.02f, 0.12f, 0.12f, 0.88f);
            var who = BroadcastUi.Label(row.transform, "Who", name, 16, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(who.rectTransform, 0.14f, string.IsNullOrEmpty(club) ? 0.12f : 0.4f, 0.72f, 0.9f);
            if (!string.IsNullOrEmpty(club))
            {
                var side = BroadcastUi.Label(row.transform, "Club", club, 13, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Place(side.rectTransform, 0.14f, 0.08f, 0.72f, 0.42f);
            }

            var mark = BroadcastUi.Label(row.transform, "Value", value, 16, UiTheme.Score, TextAnchor.MiddleRight, FontStyle.Bold);
            BroadcastUi.Place(mark.rectTransform, 0.68f, 0.12f, 0.97f, 0.88f);
            if (click != null)
            {
                BroadcastUi.Hit(row, click);
                PassClicksTo(row.transform);
            }
        }

        void AddBody(string text)
        {
            var label = BroadcastUi.Label(_modalBody, "Body", text, 15, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.Preferred(label, 88);
        }

        void OpenModal(string title, string lead, bool action, bool compact = false)
        {
            _dossierReturnAction = null;
            _profileModalPlayer = null;
            _profileModalOrders = false;
            if (_modalTitle != null) _modalTitle.gameObject.SetActive(true);
            if (_modalLead != null) _modalLead.gameObject.SetActive(true);
            if (_modalLineSize <= 0)
                _modalLineSize = 16;
            Clear(_modalBody);
            var pressRoom = _modal.transform.Find("Card/PressRoom");
            if (pressRoom != null)
                Destroy(pressRoom.gameObject);
            var suggestedXi = _modal.transform.Find("Card/SuggestedXi");
            if (suggestedXi != null)
                Destroy(suggestedXi.gameObject);
            var modalScroll = _modal.transform.Find("Card/Scroll");
            if (modalScroll != null)
                modalScroll.gameObject.SetActive(true);
            if (modalScroll != null)
            {
                var modalScrollComponent = modalScroll.GetComponent<ScrollRect>();
                if (modalScrollComponent != null)
                {
                    modalScrollComponent.horizontal = false;
                    modalScrollComponent.vertical = true;
                }
            }
            var bodyLayout = _modalBody.GetComponent<VerticalLayoutGroup>();
            if (bodyLayout != null)
                bodyLayout.enabled = true;
            var bodyFitter = _modalBody.GetComponent<ContentSizeFitter>();
            if (bodyFitter != null)
            {
                bodyFitter.enabled = true;
                bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            var bodyRect = _modalBody as RectTransform;
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.offsetMin = Vector2.zero;
            bodyRect.offsetMax = Vector2.zero;
            var card = _modal.transform.Find("Card");
            if (card != null)
            {
                BroadcastUi.Place(card as RectTransform, compact ? 0.28f : 0.04f, compact ? 0.32f : 0.03f,
                    compact ? 0.72f : 0.96f, compact ? 0.68f : 0.97f);
                for (var i = card.childCount - 1; i >= 0; i--)
                {
                    var child = card.GetChild(i);
                    if (child.name == "Title" || child.name == "Lead" || child.name == "Scroll" || child.name == "Close" || child.name == "ModalClose" || child.name == "Action" || child.name == "SandPaperGrain")
                        continue;
                    Destroy(child.gameObject);
                }
                BroadcastUi.ApplySandPaper(card.GetComponent<Image>(), 0.1f);
            }
            BroadcastUi.Place(_modalTitle.rectTransform,
                compact ? 0.06f : 0.08f, compact ? 0.8f : 0.88f,
                compact ? 0.84f : 0.91f, compact ? 0.96f : 0.97f);
            BroadcastUi.Place(_modalLead.rectTransform, compact ? 0.06f : 0.05f,
                compact ? 0.58f : 0.78f, compact ? 0.94f : 0.95f, compact ? 0.78f : 0.88f);
            var scroll = _modal.transform.Find("Card/Scroll") as RectTransform;
            if (scroll != null)
            {
                scroll.gameObject.SetActive(true);
                BroadcastUi.Place(scroll, compact ? 0.06f : 0.04f, compact ? 0.26f : 0.14f,
                    compact ? 0.94f : 0.96f, compact ? 0.55f : 0.76f);
            }
            _modalTitle.text = title;
            _modalLead.text = lead;
            _modalTitle.fontSize = compact ? 22 : 26;
            _modalLead.fontSize = compact ? 14 : 15;
            _modalAction.gameObject.SetActive(action);
            _modalAction.onClick.RemoveAllListeners();
            _modalActionLabel.fontSize = compact ? 14 : 16;
            if (compact)
                BroadcastUi.Place(_modalAction.GetComponent<RectTransform>(), 0.14f, 0.05f, 0.86f, 0.2f);
            else
                BroadcastUi.Place(_modalAction.GetComponent<RectTransform>(), 0.16f, 0.035f, 0.84f, 0.12f);
            if (_modalClose != null)
            {
                _modalClose.gameObject.SetActive(true);
                BroadcastUi.Place(_modalClose.GetComponent<RectTransform>(), compact ? 0.91f : 0.935f,
                    compact ? 0.84f : 0.895f, compact ? 0.98f : 0.98f, compact ? 0.97f : 0.97f);
            }
            _modal.SetActive(true);
            _modal.transform.SetAsLastSibling();
            _focusModalFrame = true;
        }

        void FitMessageModal(string text)
        {
            var length = string.IsNullOrEmpty(text) ? 0 : text.Length;
            if (length < 160)
                SizeModal(0.3f, 0.38f, 0.7f, 0.64f);
            else if (length < 360)
                SizeModal(0.24f, 0.3f, 0.76f, 0.72f);
            else
                SizeModal(0.18f, 0.16f, 0.82f, 0.84f);
        }

        void SizeModal(float x0, float y0, float x1, float y1)
        {
            var card = _modal == null ? null : _modal.transform.Find("Card") as RectTransform;
            if (card != null)
                BroadcastUi.Place(card, x0, y0, x1, y1);
        }

        void CloseModal()
        {
            var returnTo = _dossierReturnAction;
            _dossierReturnAction = null;
            _offer = null;
            _profileModalPlayer = null;
            _profileModalOrders = false;
            if (_modal != null)
                _modal.SetActive(false);
            ControlChrome.FocusWithin(null);
            if (_modalClose != null)
                _modalClose.gameObject.SetActive(false);
            if (returnTo != null)
                returnTo();
        }

        void AddHead(string title)
        {
            var label = BroadcastUi.Label(_modalBody, "Head", title.ToUpperInvariant(), 15, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Preferred(label, 28);
        }

        void AddShelfButton(string label, bool selected, Action action)
        {
            var button = BroadcastUi.Button(_modalBody, "Shelf", label, action,
                selected ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f),
                selected ? new Color(0.08f, 0.08f, 0.1f) : Color.white, 14);
            var layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 34;
            layout.minHeight = 34;
        }

        void AddLine(string value)
        {
            var label = BroadcastUi.Label(_modalBody, "Line", value, _modalLineSize, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.SetWrapped(label, value, _modalLineSize >= 19 ? 44 : 36);
        }

        void AddStat(string name, int value, bool danger)
        {
            var rowGo = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            rowGo.transform.SetParent(_modalBody, false);
            var row = rowGo.GetComponent<RectTransform>();
            rowGo.GetComponent<LayoutElement>().preferredHeight = 28;
            rowGo.GetComponent<LayoutElement>().minHeight = 28;
            var label = BroadcastUi.Label(row, "Label", name, 16, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(label.rectTransform, 0f, 0f, 0.42f, 1f);
            var number = BroadcastUi.Label(row, "Value", value.ToString(), 16, UiTheme.Score, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(number.rectTransform, 0.42f, 0f, 0.52f, 1f);
            var bg = BroadcastUi.Solid(row, "Bar", new Color(1f, 1f, 1f, 0.14f));
            BroadcastUi.Place(bg.rectTransform, 0.54f, 0.32f, 0.98f, 0.68f);
            var fill = BroadcastUi.Solid(bg.transform, "Fill", Color.white);
            BroadcastUi.Stretch(fill.rectTransform);
            var amount = Mathf.Clamp01(value / 20f);
            fill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.04f, amount), 1f);
            if (danger)
                fill.color = amount >= 0.7f ? UiTheme.FitnessBad : amount >= 0.45f ? UiTheme.FitnessLow : UiTheme.Fitness;
            else
                fill.color = amount >= 0.75f ? UiTheme.Fitness : amount >= 0.5f ? UiTheme.Accent : UiTheme.FitnessLow;
        }

        int Read(StaffMember reader, Player player, string stat)
        {
            var truth = PlayerAttributes.CurrentValue(player, stat);
            return StaffReport.Read(reader, player.Id, stat, truth, 1, 20);
        }

        static string WeakFootLabel(int quality)
        {
            switch (Mathf.Clamp(quality, 1, 5))
            {
                case 1: return "Muito fraco";
                case 2: return "Fraco";
                case 3: return "Razoável";
                case 4: return "Forte";
                default: return "Muito forte";
            }
        }

        string AssistantNote(Club opponent)
        {
            var assistant = StaffReport.Find(_career.Club, StaffRole.AssistantCoach);
            Player best = null;
            var bestMark = -1;
            var bestStat = "jogo";
            foreach (var id in opponent.PlayerIds)
            {
                if (!_career.World.Players.TryGetValue(id, out var player) || player.Attributes == null)
                    continue;
                Consider(assistant, player, "finalização", player.Attributes.Finishing, ref best, ref bestMark, ref bestStat);
                Consider(assistant, player, "velocidade", player.Attributes.Pace, ref best, ref bestMark, ref bestStat);
                Consider(assistant, player, "passe", player.Attributes.Passing, ref best, ref bestMark, ref bestStat);
            }

            if (best == null)
                return LocalizationManager.Current == GameLanguage.English
                    ? "The assistant has not finished an assessment of this team."
                    : "O adjunto ainda não fechou uma leitura desta equipa.";

            var form = best.Form;
            var pressing = opponent.Tactics.Pressing ?? "";
            if (LocalizationManager.Current == GameLanguage.English)
            {
                var englishStat = bestStat == "finalização" ? "finishing" : bestStat == "velocidade" ? "pace" : "passing";
                var line = opponent.Tactics.DefensiveLine ?? "";
                var deepBlock = line.Contains("baixa");
                if (deepBlock && opponent.Reputation < 70)
                    return "They will sit deep. The danger is set pieces and the counter, not " + best.FullName + " on his own.";
                if (pressing.Contains("alta") && form >= 60)
                    return "They press high and are in good form. If the first pass goes wrong, " + best.FullName + " will be on top of the build-up.";
                if (opponent.Reputation >= 75 && form < 48)
                    return "The name carries more weight than the current form. " + best.FullName + " is quiet lately, but the team remains dangerous elsewhere.";
                if (opponent.Reputation < 55 && form >= 70)
                    return "They are underdogs, but arrive in good form. " + best.FullName + " is carrying " + opponent.ShortName + " at the moment.";
                if (bestMark >= 16 && form >= 65)
                    return "They are in excellent form. " + best.FullName + " can decide games with " + englishStat + ", but is not the only threat.";
                if (opponent.Reputation >= 70)
                    return "A well-known side. The assistant has flagged " + best.FullName + " for " + englishStat + ", though they may not be the decisive player.";
                if (form < 45)
                    return "They have looked short of confidence. " + best.FullName + " stands out on paper, not over the last week.";
                return "A balanced team. " + best.FullName + " stands out for " + englishStat + " without being impossible to contain.";
            }

            var lowBlock = opponent.Tactics.DefensiveLine != null && opponent.Tactics.DefensiveLine.Contains("baixa");
            if (lowBlock && opponent.Reputation < 70)
                return "Vão fechar-se. O perigo não é o " + best.FullName + " isolado, é a bola parada e o contra-ataque.";
            if (pressing.Contains("alta") && form >= 60)
                return "Pressionam alto e andam por cima. Se o primeiro passe falha, " + best.FullName + " aparece em cima da saída.";
            if (opponent.Reputation >= 75 && form < 48)
                return "O nome pesa mais do que o momento. " + best.FullName + " anda apagado, mas a equipa continua perigosa noutro sítio.";
            if (opponent.Reputation < 55 && form >= 70)
                return "Não é favorito, mas chega numa fase boa. " + best.FullName + " está a arrastar o " + opponent.ShortName + ".";
            if (bestMark >= 16 && form >= 65)
                return "Fase muito boa. " + best.FullName + " decide jogos na " + bestStat + ". Não é o único problema.";
            if (opponent.Reputation >= 70)
                return "Equipa de nome. O adjunto aponta " + best.FullName + " pela " + bestStat + ", sem garantir que seja quem desequilibra no sábado.";
            if (form < 45)
                return "Andam pouco convencidos. " + best.FullName + " é o nome do papel, não o da última semana.";
            return "Equipa equilibrada. " + best.FullName + " destaca-se na " + bestStat + ", sem ser incontornável.";
        }

        static void Consider(StaffMember assistant, Player player, string stat, int truth, ref Player best, ref int bestMark, ref string bestStat)
        {
            var read = StaffReport.Read(assistant, player.Id, stat, truth, 1, 20);
            if (read <= bestMark)
                return;
            best = player;
            bestMark = read;
            bestStat = stat;
        }

        string VibeLine()
        {
            var tension = _career.Club.DressingRoomTension;
            if (LocalizationManager.Current == GameLanguage.English)
            {
                if (tension >= 75) return "The dressing room has closed ranks. Another signing could make things worse.";
                if (tension >= 55) return "Conversations stop when you walk in. The group is not fully behind you yet.";
                return "The squad still lets you in. A staff change could shift that balance.";
            }
            if (tension >= 75)
                return "O balneário fechou-se. Outra contratação agora pesa ainda mais.";
            if (tension >= 55)
                return "Há conversas que param quando entras. O grupo ainda não é teu.";
            return "O balneário ainda te deixa entrar. Uma troca de staff mexe com isso.";
        }

        static string TensionWord(int tension)
        {
            if (tension >= 75) return "fechado";
            if (tension >= 55) return "tenso";
            return "aberto";
        }

        static string TiredWord(int fatigue)
        {
            if (fatigue >= 75) return "alto";
            if (fatigue >= 50) return "visível";
            if (fatigue >= 30) return "controlado";
            return "baixo";
        }

        static string NextMatchText(CareerState career)
        {
            var next = career.NextUserFixture();
            return next == null ? "Época sem jogos por jogar." : MatchLabel(career, next);
        }

        static string EnglishMatchLabel(CareerState career)
        {
            var next = career.NextUserFixture();
            if (next == null) return "No remaining matches this season.";
            var opponent = career.World.Clubs[next.OpponentId(career.Coach.ClubId)];
            return (next.IsHome(career.Coach.ClubId) ? "Home" : "Away") + " vs " + opponent.Name
                + " · " + LocalizationManager.FormatDate(next.Date, "dd MMM") + " · " + FixtureKickoff(next)
                + " · " + FixtureCompetition(next);
        }

        static string MatchLabel(CareerState career, Fixture fixture)
        {
            var opp = career.World.Clubs[fixture.OpponentId(career.Coach.ClubId)];
            var english = LocalizationManager.Current == GameLanguage.English;
            var venue = fixture.IsHome(career.Coach.ClubId) ? (english ? "Home" : "Casa") : (english ? "Away" : "Fora");
            return venue + " vs " + opp.Name + " · " + LocalizationManager.FormatDate(fixture.Date, "dd MMM")
                + " · " + FixtureKickoff(fixture) + " · " + FixtureCompetition(fixture);
        }

        static string FixtureCompetition(Fixture fixture)
        {
            return fixture == null ? "" : LocalizationManager.Translate(SeasonSchedule.Competition(fixture.LeagueId));
        }

        static string FixtureKickoff(Fixture fixture)
        {
            if (fixture == null)
                return "15:00";
            if (fixture.Date.TimeOfDay != TimeSpan.Zero)
                return fixture.Date.ToString("HH:mm", CultureInfo.InvariantCulture);
            if (fixture.LeagueId == "league:campeoes")
                return "20:00";
            if (!string.IsNullOrEmpty(fixture.LeagueId) && fixture.LeagueId.StartsWith("cup:", StringComparison.Ordinal))
                return "19:45";
            var day = fixture.Date.DayOfWeek;
            return day == DayOfWeek.Saturday || day == DayOfWeek.Sunday ? "15:00" : "19:45";
        }

        string HubLeagueId()
        {
            var next = _career == null ? null : _career.NextUserFixture();
            var fixtureLeague = next == null ? null : next.LeagueId;
            var clubLeague = _career != null && _career.Club != null ? _career.Club.LeagueId : null;
            return LeagueBackdrop.ForFixture(fixtureLeague, clubLeague);
        }

        List<Player> Squad(Club club)
        {
            var list = new List<Player>();
            if (club == null || club.PlayerIds == null || _career == null || _career.World == null)
                return list;
            foreach (var id in club.PlayerIds)
            {
                if (_career.World.Players.TryGetValue(id, out var player))
                    list.Add(player);
            }

            return list;
        }

        string Marks(Player player, Club club)
        {
            var bits = "";
            if (player.IsCaptain)
                bits = "C";
            if (player.IsViceCaptain)
                bits = bits.Length == 0 ? "VC" : bits + " VC";
            if (IsAce(club, player))
                bits = bits.Length == 0 ? "ÁS" : bits + " · ÁS";
            return bits;
        }

        bool IsAce(Club club, Player player)
        {
            var best = player;
            var bestScore = AceScore(player);
            foreach (var other in Squad(club))
            {
                var score = AceScore(other);
                if (score > bestScore)
                {
                    best = other;
                    bestScore = score;
                }
            }

            return best == player;
        }

        static int AceScore(Player player)
        {
            var stats = player.Attributes;
            if (stats == null)
                return 0;
            if (player.Position == PlayerPosition.Goalkeeper)
                return stats.Reflexes + stats.Handling + stats.Positioning;
            return stats.Finishing + stats.Passing + stats.Pace + stats.Composure;
        }

        static string Tendency(Player player)
        {
            if (player == null || player.Attributes == null)
                return "O adjunto ainda não tem leitura deste jogador.";
            var attr = player.Attributes;
            var overall = PlayerAttributes.Overall(player);
            var lines = new List<string>();
            if (attr.Passing <= 9)
                lines.Add("Falha passes com regularidade.");
            else if (attr.Passing >= 16)
                lines.Add("O passe sai limpo quase sempre.");
            else
                lines.Add("O passe aguenta nas curtas e falha mais nas longas.");
            if (attr.Finishing <= 8)
                lines.Add("Remata pouco e falha a baliza com frequência.");
            else if (attr.Finishing >= 15 || player.HasTrait(PlayerTrait.RemateEmJeito))
                lines.Add("Procura o remate, também de longe, e a bola pode curvar.");
            else if (attr.Finishing >= 12)
                lines.Add("Remata quando chega a uma boa posição.");
            else
                lines.Add("Evita o remate se não estiver perto da baliza.");
            if (attr.Dribbling >= 15 || player.HasTrait(PlayerTrait.Driblador))
                lines.Add("Gosta de conduzir e driblar.");
            if (attr.Pace >= 16 || player.HasTrait(PlayerTrait.Rapido))
                lines.Add("Ataca o espaço em corrida.");
            if (overall >= 84)
                lines.Add("O overall alto decide melhor e erra menos.");
            else if (overall <= 68)
                lines.Add("O overall baixo perde mais bolas e falha mais ações.");
            return string.Join(" ", lines);
        }

        static string NationCode(string nation)
        {
            if (string.IsNullOrEmpty(nation))
                return "";
            switch (nation)
            {
                case "Argentina": return "ARG";
                case "Brasil": return "BRA";
                case "Japão": return "JPN";
                case "Coreia do Sul": return "KOR";
                case "Turquia": return "TUR";
                case "Itália": return "ITA";
                case "Portugal": return "POR";
                case "Alemanha": return "ALE";
                case "Espanha": return "ESP";
                case "Bélgica": return "BEL";
                case "França": return "FRA";
                case "Inglaterra": return "ING";
                case "Países Baixos": return "HOL";
                case "Senegal": return "SEN";
                case "RD Congo": return "COD";
                case "Gana": return "GHA";
                case "Marrocos": return "MAR";
                case "Nigéria": return "NGA";
                default:
                    var clean = nation.Replace(" ", "");
                    return clean.Length <= 3 ? clean.ToUpperInvariant() : clean.Substring(0, 3).ToUpperInvariant();
            }
        }

        static Image FaceImage(Transform parent, Player player, float size)
        {
            var go = new GameObject("Face", typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = FaceBook.For(player);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        static Image CrestImage(Transform parent, Club club, float size)
        {
            var go = new GameObject("Crest", typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = CrestStudio.For(club);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        static void Clear(RectTransform content)
        {
            if (content == null)
                return;
            for (var i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i).gameObject;
                child.SetActive(false);
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        bool PollOnline()
        {
            if (_career == null)
                return false;
            while (LanRoom.TryTake(out var line))
            {
                if (string.IsNullOrEmpty(line))
                    continue;
                if (line.StartsWith("JOIN|"))
                {
                    var bits = line.Split(new[] { '|' }, 3);
                    int id;
                    if (bits.Length > 1 && int.TryParse(bits[1], out id))
                        NoteJoin(id, bits.Length > 2 ? bits[2] : "");
                }
                else if (line.StartsWith("PICK|"))
                {
                    var bits = line.Split(new[] { '|' }, 3);
                    int id;
                    if (bits.Length > 2 && int.TryParse(bits[1], out id))
                        NotePick(id, bits[2]);
                }
                else if (line.StartsWith("LIST|"))
                    ShowClubPick(line);
                else if (line.StartsWith("READY|"))
                {
                    int id;
                    if (int.TryParse(line.Substring(6), out id))
                        NoteReady(id);
                }
                else if (line.StartsWith("SEAT|"))
                {
                    var bits = line.Split('|');
                    if (bits.Length > 1)
                        CareerOnline.PendingClub = bits[1];
                }
                else if (line.StartsWith("FILE|"))
                {
                    try
                    {
                        var text = Encoding.UTF8.GetString(Convert.FromBase64String(line.Substring(5)));
                        var club = CareerOnline.PendingClub;
                        if (string.IsNullOrEmpty(club) && _career.Coach != null)
                            club = _career.Coach.ClubId;
                        CareerStore.AdoptShared(GameRoot.Require().Session, text, club);
                        LanRoom.HoldShare = true;
                        GameRoot.Instance.Open(SceneNames.CareerHub);
                        return true;
                    }
                    catch (Exception error)
                    {
                        Debug.LogWarning("Carreira online inválida: " + error.Message);
                    }
                }
                else if (line.StartsWith("ERRO|"))
                    OpenModal("ONLINE", line.Substring(5), false, true);
            }
            return false;
        }

        OnlineSeat OtherHumanSeat(string clubId)
        {
            if (_career?.Online == null || _career.Coach == null || string.IsNullOrEmpty(clubId) || clubId == _career.Coach.ClubId)
                return null;
            for (var i = 0; i < _career.Online.Seats.Count; i++)
            {
                var seat = _career.Online.Seats[i];
                if (seat != null && seat.ClubId == clubId)
                    return seat;
            }
            return null;
        }

        string ManagerOf(Club club)
        {
            if (club == null || _career == null)
                return "";
            if (_career.Online != null)
            {
                for (var i = 0; i < _career.Online.Seats.Count; i++)
                {
                    var seat = _career.Online.Seats[i];
                    if (seat != null && seat.ClubId == club.Id && !string.IsNullOrEmpty(seat.Name))
                        return seat.Name;
                }
            }
            if (_career.Coach != null && _career.Coach.ClubId == club.Id)
                return _career.Coach.Name ?? "";
            return "";
        }

        bool SeatHolds(string clubId)
        {
            if (_career?.Online == null || string.IsNullOrEmpty(clubId))
                return false;
            for (var i = 0; i < _career.Online.Seats.Count; i++)
            {
                var seat = _career.Online.Seats[i];
                if (seat != null && seat.ClubId == clubId && (seat.Local || LanRoom.ClubTaken(seat.ClubId)))
                    return true;
            }
            return false;
        }

        void NoteJoin(int id, string name)
        {
            if (_career.Online == null || _career.Online.Seats.Count == 0)
            {
                LanRoom.Reply(id, "ERRO|Ainda ninguém deu host nesta carreira.");
                return;
            }
            var clean = CareerOnline.Clean(name);
            _joinNames[id] = string.IsNullOrEmpty(clean) ? "Convidado" : clean;
            LanRoom.Reply(id, ClubList());
        }

        string ClubList()
        {
            var hostClub = _career.Coach == null ? "" : _career.Coach.ClubId;
            var clubs = new List<Club>();
            if (_career.World != null)
            {
                foreach (var club in _career.World.Clubs.Values)
                {
                    if (club == null || club.Id == hostClub || SeatHolds(club.Id))
                        continue;
                    clubs.Add(club);
                }
            }
            clubs.Sort((a, b) =>
            {
                var league = string.CompareOrdinal(a.LeagueId, b.LeagueId);
                return league != 0 ? league : string.CompareOrdinal(a.Name, b.Name);
            });
            var line = "LIST";
            for (var i = 0; i < clubs.Count; i++)
            {
                var club = clubs[i];
                line += "|" + club.Id + "," + CareerOnline.Clean(club.Name) + "," + CareerOnline.Clean(club.LeagueId);
            }
            return line;
        }

        void ShowClubPick(string line)
        {
            var bits = (line ?? "").Split('|');
            OpenModal("ESCOLHE O TEU CLUBE", "O anfitrião abriu a carreira. Escolhe o clube que vais treinar. O teu clube fica marcado com a tua cor nas tabelas.", false);
            var shown = 0;
            var league = "";
            for (var i = 1; i < bits.Length; i++)
            {
                var parts = bits[i].Split(',');
                if (parts.Length < 2 || string.IsNullOrEmpty(parts[0]))
                    continue;
                var group = parts.Length > 2 ? parts[2] : "";
                if (group != league)
                {
                    league = group;
                    var caption = LeagueCaption(group);
                    var heading = BroadcastUi.Label(_modalBody, "League" + shown, caption, 14, UiTheme.Score, TextAnchor.MiddleLeft, FontStyle.Bold);
                    BroadcastUi.Preferred(heading, 28);
                }
                shown++;
                var clubId = parts[0];
                var clubName = parts[1];
                var pick = BroadcastUi.Button(_modalBody, "Pick" + shown, clubName, () =>
                {
                    LanRoom.Send("PICK|" + clubId);
                    OpenModal("ONLINE", "Escolheste " + clubName + ". A carreira do anfitrião está a abrir neste clube.", false, true);
                }, new Color(0.08f, 0.20f, 0.32f, 0.95f), Color.white, 15);
                BroadcastUi.Preferred(pick, 40);
            }
            if (shown == 0)
                BroadcastUi.Preferred(BroadcastUi.Label(_modalBody, "NoClub", "Não há clubes livres nesta carreira.", 16, UiTheme.Muted), 40);
        }

        string LeagueCaption(string leagueId)
        {
            if (_career?.World?.Leagues != null)
            {
                foreach (var league in _career.World.Leagues)
                {
                    if (league != null && league.Id == leagueId)
                        return LocalizationManager.Translate(league.Name);
                }
            }
            return string.IsNullOrEmpty(leagueId) ? "Liga" : leagueId;
        }

        void NotePick(int id, string clubId)
        {
            clubId = (clubId ?? "").Trim();
            if (_career?.Online == null || _career.World == null || !_career.World.Clubs.ContainsKey(clubId) || SeatHolds(clubId))
            {
                LanRoom.Reply(id, "ERRO|Esse clube já tem treinador ou não está nesta carreira.");
                return;
            }
            var online = _career.Online;
            OnlineSeat seat = null;
            for (var i = 0; i < online.Seats.Count; i++)
            {
                var candidate = online.Seats[i];
                if (candidate != null && !candidate.Local && candidate.ClubId == clubId && !LanRoom.ClubTaken(candidate.ClubId))
                {
                    seat = candidate;
                    break;
                }
            }
            if (seat == null)
            {
                if (online.Seats.Count >= CareerOnline.SeatColors.Length)
                {
                    LanRoom.Reply(id, "ERRO|A sala já está cheia.");
                    return;
                }
                seat = new OnlineSeat
                {
                    ClubId = clubId,
                    Color = CareerOnline.SeatColors[online.Seats.Count],
                    Local = false
                };
                online.Seats.Add(seat);
            }
            var joined = "";
            _joinNames.TryGetValue(id, out joined);
            seat.Name = string.IsNullOrEmpty(joined) ? "Convidado" : joined;
            seat.ClubId = clubId;
            seat.Connected = true;
            LanRoom.Remember(id, clubId);
            CareerStore.Save(_career);
            LanRoom.Reply(id, "SEAT|" + seat.ClubId + "|" + seat.Color + "|" + (online.RoomCode ?? ""));
            var text = CareerStore.ReadText();
            if (!string.IsNullOrEmpty(text))
                LanRoom.Reply(id, "FILE|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(text)));
            Refresh();
            OpenOnline();
        }

        void NoteReady(int id)
        {
            if (_career?.Online == null)
                return;
            var clubId = LanRoom.ClubFor(id);
            for (var i = 0; i < _career.Online.Seats.Count; i++)
            {
                var seat = _career.Online.Seats[i];
                if (seat != null && seat.ClubId == clubId)
                    seat.ContinueReady = true;
            }
            var earliest = CareerOnline.Earliest(_career);
            if (earliest != null && _career.Calendar != null && earliest.Date.Date <= _career.Calendar.CurrentDate.Date)
                return;
            if (!CareerOnline.EveryoneReady(_career))
                return;
            if (PressDutyOpen() || SquadDutyOpen())
                return;
            var moment = _career.Current;
            if (moment != null && moment.Kind == MomentKind.Talk && !moment.Answered)
                return;
            if (moment != null && moment.Kind == MomentKind.Match && moment.Fixture != null && !moment.Fixture.Played)
                return;
            CareerOnline.ClearReady(_career);
            AdvanceShared();
        }

        bool HoldGuest(bool startingMatch, OnlineGate gate)
        {
            if (!LanRoom.IsGuest || _career.Online == null || !_career.Online.Active)
                return false;
            if (!gate.Stop && (startingMatch || CareerOnline.OwnsUpcoming(_career)))
                return false;
            if (gate.Wait == OnlineWait.None || gate.Wait == OnlineWait.Partner)
                LanRoom.Send("READY");
            OpenModal("ONLINE", string.IsNullOrEmpty(gate.Message) ? "Continuar enviado ao anfitrião." : gate.Message, false, true);
            return true;
        }

        public void OpenOnlineFromSettings()
        {
            OpenOnline();
        }

        void OpenOnline()
        {
            if (_career == null || _modalBody == null)
                return;
            var online = _career.Online;
            var english = LocalizationManager.Current == GameLanguage.English;
            var isGuest = LanRoom.IsGuest;
            var isHost = LanRoom.IsHost && !isGuest;
            var hostIp = LanRoom.LocalIPv4Address();
            var lead = isHost
                ? (english ? "Share this IP with your guest:\n" : "Partilha este IP com o convidado:\n")
                    + (string.IsNullOrEmpty(hostIp)
                        ? (english ? "No local IP address found." : "Não foi encontrado um IP local.")
                        : hostIp)
                : isGuest
                    ? (english ? "Connected to the host. Choose your club to join the career." : "Ligado ao anfitrião. Escolhe o teu clube para entrar na carreira.")
                    : (english ? "Host this career, or enter the host’s IP to join." : "Dá host a esta carreira ou introduz o IP do anfitrião para entrar.");
            if (!isHost && !isGuest && !string.IsNullOrEmpty(LanRoom.Status))
                lead += "\n" + LanRoom.Status;
            OpenModal("ONLINE", lead, false);
            if (!isGuest && !isHost && (online == null || online.Seats.Count == 0))
            {
                var host = BroadcastUi.Button(_modalBody, "Host", english ? "HOST THIS CAREER" : "DAR HOST DESTA CARREIRA", BeginHosting, new Color(0.10f, 0.24f, 0.38f, 0.95f), Color.white, 16);
                BroadcastUi.Preferred(host, 48);
            }
            else if (!isGuest && !isHost && online != null && online.Seats.Count > 0)
            {
                var reopen = BroadcastUi.Button(_modalBody, "Reopen", english ? "OPEN THIS CAREER AS HOST" : "ABRIR O SERVIDOR DESTA CARREIRA", BeginHosting, new Color(0.10f, 0.24f, 0.38f, 0.95f), Color.white, 16);
                BroadcastUi.Preferred(reopen, 48);
            }
            if (online != null)
            {
                for (var i = 0; i < online.Seats.Count; i++)
                {
                    var seat = online.Seats[i];
                    if (seat == null)
                        continue;
                    var clubName = seat.ClubId;
                    if (_career.World != null && _career.World.Clubs.TryGetValue(seat.ClubId, out var club) && club != null)
                        clubName = club.Name;
                    var caption = (string.IsNullOrEmpty(seat.Name) ? (english ? "Coach" : "Treinador") : seat.Name) + "  ·  " + clubName + (seat.Local ? (english ? "  ·  you" : "  ·  tu") : "");
                    var tint = BroadcastUi.Parse(seat.Color, new Color(0.88f, 0.48f, 0.24f, 1f));
                    if (i > 0 && isHost)
                    {
                        var clubId = seat.ClubId;
                        var remove = BroadcastUi.Button(_modalBody, "Seat" + i, caption + (english ? "   ·   REMOVE" : "   ·   TIRAR"), () => RemoveSeat(clubId), tint, Color.black, 15);
                        BroadcastUi.Preferred(remove, 44);
                    }
                    else
                    {
                        var row = BroadcastUi.Label(_modalBody, "Seat" + i, caption, 16, tint, TextAnchor.MiddleLeft, FontStyle.Bold);
                        BroadcastUi.Preferred(row, 36);
                    }
                }
                if (isHost && online.Seats.Count >= 1)
                {
                    var live = BroadcastUi.Button(_modalBody, "Realtime", english ? "REAL TIME" : "TEMPO REAL", () => SetOnlineMode("tempo-real", 20, 0), new Color(0.12f, 0.08f, 0.22f, 0.95f), Color.white, 15);
                    BroadcastUi.Preferred(live, 42);
                    var evening = BroadcastUi.Button(_modalBody, "Clock20", english ? "MATCHES AT 20:00" : "JOGOS ÀS 20:00", () => SetOnlineMode("hora", 20, 0), new Color(0.12f, 0.08f, 0.22f, 0.95f), Color.white, 15);
                    BroadcastUi.Preferred(evening, 42);
                    var late = BroadcastUi.Button(_modalBody, "Clock21", english ? "MATCHES AT 21:30" : "JOGOS ÀS 21:30", () => SetOnlineMode("hora", 21, 30), new Color(0.12f, 0.08f, 0.22f, 0.95f), Color.white, 15);
                    BroadcastUi.Preferred(late, 42);
                    if (online.Seats.Count >= 2)
                    {
                        var swap = BroadcastUi.Button(_modalBody, "Swap", english ? "SWITCH CLUB ON THIS COMPUTER" : "MUDAR DE LUGAR NESTE COMPUTADOR", SwitchSeat, new Color(0.16f, 0.10f, 0.28f, 0.95f), Color.white, 15);
                        BroadcastUi.Preferred(swap, 44);
                    }
                    var stop = BroadcastUi.Button(_modalBody, "StopOnline", english ? "CLOSE ROOM" : "FECHAR SALA", StopOnline, new Color(0.28f, 0.08f, 0.12f, 0.95f), Color.white, 15);
                    BroadcastUi.Preferred(stop, 44);
                    if (online.Seats.Count < 2)
                    {
                        var waiting = BroadcastUi.Label(_modalBody, "Waiting", english ? "Waiting for the guest to connect and choose a club." : "À espera que o convidado entre e escolha o clube.", 15, UiTheme.Muted, TextAnchor.MiddleLeft);
                        BroadcastUi.Preferred(waiting, 32);
                    }
                }
            }
            if (!isHost && !isGuest)
            {
                var field = UiFactory.Input(_modalBody, english ? "Host IP address" : "IP do anfitrião");
                field.characterLimit = 64;
                BroadcastUi.Preferred(field, 48);
                var join = BroadcastUi.Button(_modalBody, "Join", english ? "JOIN HOST CAREER" : "ENTRAR NA CARREIRA DO ANFITRIÃO", () => JoinHost(field), new Color(0.10f, 0.24f, 0.38f, 0.95f), Color.white, 16);
                BroadcastUi.Preferred(join, 48);
            }
        }

        void EnsureHost()
        {
            if (_career.Online == null)
                _career.Online = new CareerOnline();
            var online = _career.Online;
            if (online.Port <= 0)
                online.Port = CareerOnline.DefaultPort;
            if (string.IsNullOrEmpty(online.RoomCode))
                online.RoomCode = CareerOnline.MakeCode();
            if (online.Seats.Count == 0)
            {
                online.Seats.Add(new OnlineSeat
                {
                    Name = _career.Coach == null ? "Anfitrião" : _career.Coach.Name,
                    ClubId = _career.Coach == null ? "" : _career.Coach.ClubId,
                    Color = CareerOnline.SeatColors[0],
                    Local = true
                });
                CareerOnline.PendingClub = online.Seats[0].ClubId;
            }
        }

        void BeginHosting()
        {
            EnsureHost();
            for (var i = _career.Online.Seats.Count - 1; i >= 1; i--)
            {
                var seat = _career.Online.Seats[i];
                if (seat == null || seat.Local || LanRoom.ClubTaken(seat.ClubId))
                    continue;
                _career.Online.Seats.RemoveAt(i);
            }
            LanRoom.StartHost(_career.Online.Port);
            CareerStore.Save(_career);
            OpenOnline();
        }

        void SetOnlineMode(string mode, int hour, int minute)
        {
            EnsureHost();
            _career.Online.Mode = mode == "hora" ? "hora" : "tempo-real";
            _career.Online.KickoffHour = Mathf.Clamp(hour, 0, 23);
            _career.Online.KickoffMinute = Mathf.Clamp(minute, 0, 59);
            CareerStore.Save(_career);
            OpenOnline();
        }

        void SwitchSeat()
        {
            var online = _career.Online;
            if (online == null || online.Seats.Count < 2 || LanRoom.IsGuest)
                return;
            var index = 0;
            for (var i = 0; i < online.Seats.Count; i++)
            {
                if (online.Seats[i] != null && online.Seats[i].Local)
                    index = i;
            }
            var nextIndex = index;
            for (var step = 0; step < online.Seats.Count; step++)
            {
                nextIndex = (nextIndex + 1) % online.Seats.Count;
                var candidate = online.Seats[nextIndex];
                if (candidate != null && !candidate.Connected && _career.World.Clubs.ContainsKey(candidate.ClubId))
                    break;
            }
            var next = online.Seats[nextIndex];
            if (next == null || next.Connected || next.Local)
                return;
            for (var i = 0; i < online.Seats.Count; i++)
            {
                if (online.Seats[i] != null)
                    online.Seats[i].Local = false;
            }
            next.Local = true;
            CareerOnline.PendingClub = next.ClubId;
            _career.Coach.ClubId = next.ClubId;
            CareerMoments.Begin(_career);
            CareerStore.Save(_career);
            CloseModal();
            Refresh();
        }

        void RemoveSeat(string clubId)
        {
            var online = _career.Online;
            if (online == null)
                return;
            for (var i = online.Seats.Count - 1; i >= 1; i--)
            {
                if (online.Seats[i] != null && online.Seats[i].ClubId == clubId)
                    online.Seats.RemoveAt(i);
            }
            CareerStore.Save(_career);
            OpenOnline();
        }

        void StopOnline()
        {
            _career.Online = null;
            CareerOnline.PendingClub = null;
            LanRoom.Stop();
            CareerStore.Save(_career);
            OpenOnline();
        }

        void JoinHost(InputField field)
        {
            var ip = field == null ? "" : field.text;
            var port = _career.Online != null && _career.Online.Port > 0 ? _career.Online.Port : CareerOnline.DefaultPort;
            LanRoom.Join(ip, port, _career.Coach == null ? "Convidado" : _career.Coach.Name);
            var english = LocalizationManager.Current == GameLanguage.English;
            OpenModal("ONLINE", english ? "Connecting to the host. You’ll choose your club when the host responds." : "A ligar ao anfitrião. Escolhes o teu clube quando a sala responder.", false, true);
        }
    }

    sealed class SquadSheetClick : MonoBehaviour, IPointerClickHandler
    {
        Action _open;

        public void Bind(Action open) => _open = open;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button == PointerEventData.InputButton.Right)
                _open?.Invoke();
        }
    }

    sealed class TacticDrift : MonoBehaviour
    {
        RectTransform _rect;
        Vector2 _min;
        Vector2 _max;
        Vector2 _delta;
        float _phase;

        public void Setup(Vector2 delta, float phase)
        {
            _rect = transform as RectTransform;
            if (_rect == null)
                return;
            _min = _rect.anchorMin;
            _max = _rect.anchorMax;
            _delta = delta;
            _phase = phase;
        }

        void Update()
        {
            if (_rect == null)
                return;
            var wave = Mathf.Sin(Time.unscaledTime * 1.5f + _phase) * 0.5f + 0.5f;
            var shift = _delta * wave;
            _rect.anchorMin = _min + shift;
            _rect.anchorMax = _max + shift;
        }
    }

    sealed class DutyAlertMark : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var area = rectTransform.rect;
            var color = new Color(0.86f, 0.12f, 0.16f, 1f);
            var tip = UIVertex.simpleVert;
            tip.color = color;
            tip.position = new Vector3((area.xMin + area.xMax) * 0.5f, area.yMax, 0f);
            var left = UIVertex.simpleVert;
            left.color = color;
            left.position = new Vector3(area.xMin, area.yMin, 0f);
            var right = UIVertex.simpleVert;
            right.color = color;
            right.position = new Vector3(area.xMax, area.yMin, 0f);
            vh.AddVert(tip);
            vh.AddVert(left);
            vh.AddVert(right);
            vh.AddTriangle(0, 1, 2);
        }
    }
}
