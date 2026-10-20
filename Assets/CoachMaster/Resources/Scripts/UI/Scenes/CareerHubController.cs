using System;
using System.Collections.Generic;
using System.Globalization;
using CoachMaster.App;
using CoachMaster.Career;
using CoachMaster.Domain;
using CoachMaster.MatchPlay;
using CoachMaster.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Scenes
{
    public sealed class CareerHubController : MonoBehaviour, IControlBackHandler
    {
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
        int _socialFeedDaySeed = int.MinValue;
        readonly HashSet<string> _phoneAnswered = new HashSet<string>();
        Text _dateLine;
        Text _homeTitle;
        Text _homeBody;
        Text _nextChip;
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
        Image _homeFace;
        Button[] _paneTabs;
        Button[] _zoneButtons;
        RectTransform _sheet;
        Text _noteError;
        Text _vibe;
        InputField _noteField;
        RectTransform _newsContent;
        Button _memoriesButton;
        RectTransform _tableContent;
        RectTransform _tabBar;
        RectTransform _boardContent;
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
        RectTransform _modalBody;
        Button _modalAction;
        Text _modalActionLabel;
        Button _modalClose;
        Button _keyboardScoutBack;
        Player _profileModalPlayer;
        bool _profileModalOrders;
        GameObject _tacHost;
        string _arm;
        string _orderId;
        RectTransform _tacticsBody;
        int _modalLineSize = 16;
        Transform _cardTemplate;
        CareerState _career;
        StaffMember _offer;
        int _transferPage;
        int _teamZone;
        bool _individualTraining;
        int _renewalAttempt;

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
                    _phoneSheet.SetAsLastSibling();
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
            var hint = BroadcastUi.Label(root, "ControlHint", LocalizationManager.Translate("M · DEFINIÇÕES"), 15, UiTheme.Muted, TextAnchor.MiddleRight);
            BroadcastUi.Place(hint.rectTransform, 0.42f, 0.008f, 0.98f, 0.048f);
            Show(0);
            Refresh();
            if (_phoneSheet != null)
                _phoneSheet.SetAsLastSibling();
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
                    changed |= GlobalTransferMarket.Advance(_career);
                    changed |= Newsroom.PublishGlobalRoundup(_career);
                    if (changed)
                    {
                        CareerStore.Save(_career);
                        if (_pages != null && _pages.Length > 2 && _pages[2] != null && _pages[2].activeSelf) FillAcademy();
                        if (_pages != null && _pages.Length > 1 && _pages[1] != null && _pages[1].activeSelf) FillNews();
                        if (_transferPage == 2 && _pages != null && _pages.Length > 5 && _pages[5] != null && _pages[5].activeSelf) FillTransferHistory();
                    }
                }
            }

            if (_phoneSheet != null)
            {
                var target = _phoneOpen ? 1f : 0f;
                _phoneSlide = Mathf.MoveTowards(_phoneSlide, target, Time.deltaTime * 3.4f);
                var y0 = Mathf.Lerp(0.935f, 0.16f, _phoneSlide);
                BroadcastUi.Place(_phoneSheet, 0.4f, y0, 0.6f, y0 + 0.72f);
            }

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
            if (_career != null && SquadDragCard.ConsumeBenchGroupDrop(out var benchPlayer, out var fromReserves, out var toReserves))
            {
                if (benchPlayer != null && fromReserves != toReserves)
                {
                    _pendingBenchSwap = benchPlayer;
                    _pendingBenchSwapFromReserves = fromReserves;
                    _reserves = toReserves;
                    FillTeam();
                }
            }
            if (_career != null && SquadDragCard.ConsumeSwap(out var incoming, out var fieldIndex, out var fromField))
            {
                if (fromField >= 0)
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
                    var tag = note.ReplyGroup == "job-offer" ? "PROPOSTA" : note.Channel == "jogador" ? "JOGADOR" : note.Channel == "clube" ? "CLUBE" : note.Channel == "staff" ? "STAFF" : note.Channel == "jornal" ? "JORNAL" : "AGENDA";
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

            if (_phoneAnswered.Contains(key))
            {
                AddPhoneLine("Já respondeste a esta mensagem.");
                return;
            }

            if (note.Channel == "jogador" && _career.World.Players.TryGetValue(note.PlayerId ?? "", out var player))
            {
                AddPlayerReplies(player, note.ReplyGroup, key);
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
                AddPhoneChoice("Enviem a duração e as condições por escrito.", () => AnswerClub(note, key, "Pedi os termos completos do empréstimo."));
                AddPhoneChoice("Nesta fase, queremos mantê-lo no plantel.", () => AnswerClub(note, key, "Recusei o empréstimo nesta fase."));
                AddPhoneChoice("Podemos ouvir uma proposta com minutos garantidos.", () => AnswerClub(note, key, "Disse que ouviria uma proposta com garantias de utilização."));
                return;
            }

            if (note.ReplyGroup == "transfer")
            {
                AddPhoneChoice("O jogador não está disponível para transferência.", () => AnswerClub(note, key, "Recusei a abordagem e mantive o jogador no plantel."));
                AddPhoneChoice("Enviem uma proposta formal para avaliarmos.", () => AnswerClub(note, key, "Pedi uma proposta formal pelo jogador."));
                AddPhoneChoice("Neste momento não queremos negociar.", () => AnswerClub(note, key, "Fechei as conversas de transferência por agora."));
                return;
            }

            AddPhoneChoice("Obrigado pela mensagem. Vou analisar o assunto.", () => AnswerClub(note, key, "Agradeci a mensagem e disse que iria analisar o assunto."));
            AddPhoneChoice("Falem primeiro com o meu adjunto.", () => AnswerClub(note, key, "Encaminhei o contacto para o adjunto avaliar."));
            AddPhoneChoice("Agora não é uma boa altura para conversarmos.", () => AnswerClub(note, key, "Adiei a conversa para outro momento."));
        }

        void AddPhoneChoice(string label, Action action)
        {
            var button = BroadcastUi.Button(_phoneList, "Choice", label, action, new Color(0.16f, 0.45f, 0.62f, 1f), Color.white, 14);
            var layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 48;
            layout.minHeight = 48;
        }

        void AddPhoneLine(string text)
        {
            var row = BroadcastUi.Panel(_phoneList, "Note", new Color(0.12f, 0.45f, 0.55f, 0.35f));
            BroadcastUi.Preferred(row, 64);
            var label = BroadcastUi.Label(row.transform, "Body", text, 14, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(label.rectTransform, 0.06f, 0.08f, 0.94f, 0.92f);
        }

        void AnswerPlayer(Player player, string key, int morale, string line)
        {
            if (_phoneAnswered.Contains(key))
                return;
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
            _phoneAnswered.Add(key);
            _career.Notebook.Insert(0, _career.Calendar.ClockText + "  " + (player == null
                ? (LocalizationManager.Current == GameLanguage.English ? "Squad" : "Plantel") : player.FullName) + " — " + line);
            ClosePhoneChoices();
            AddPhoneLine(line);
        }

        void AnswerClub(PhoneNote note, string key, string line)
        {
            if (_phoneAnswered.Contains(key))
                return;
            var refusedResponse = line.IndexOf("recus", StringComparison.OrdinalIgnoreCase) >= 0
                || line.IndexOf("mantive", StringComparison.OrdinalIgnoreCase) >= 0
                || line.IndexOf("fechei", StringComparison.OrdinalIgnoreCase) >= 0;
            line = LocalizationManager.Translate(line);
            _phoneAnswered.Add(key);
            _career.Notebook.Insert(0, _career.Calendar.ClockText + "  " + note.Who + " — " + line);
            if (note != null && (note.ReplyGroup == "transfer" || note.ReplyGroup == "loan")
                && !string.IsNullOrEmpty(note.PlayerId)
                && _career.World.Players.TryGetValue(note.PlayerId, out var player))
            {
                var refused = refusedResponse;
                var kind = note.ReplyGroup == "loan" ? "abordagem-emprestimo" : "abordagem-transferencia";
                var english = LocalizationManager.Current == GameLanguage.English;
                var subject = note.ReplyGroup == "loan" ? (english ? "loan" : "empréstimo") : (english ? "transfer" : "transferência");
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
            ClosePhoneChoices();
            AddPhoneLine(line);
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
            _phoneAnswered.Add(key);
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

        List<PhoneNote> PhoneNotes()
        {
            var list = new List<PhoneNote>();
            var english = LocalizationManager.Current == GameLanguage.English;
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

            if (buyer != null && star != null && buyer.Id != club.Id)
                list.Add(new PhoneNote(buyer.Name, english
                    ? "We're interested in " + star.FullName + ". Make them available and we'll submit a serious offer."
                    : "Temos interesse em " + star.FullName + ". Se o abrires à transferência, fazemos uma proposta a sério.", "clube", star.Id, buyer.Id, "transfer"));
            if (lender != null && youth != null && lender.Id != (buyer == null ? "" : buyer.Id))
                list.Add(new PhoneNote(lender.Name, english
                    ? youth.FullName + " would fit our squad. Would you consider a loan until the end of the season?"
                    : "O " + youth.FullName + " encaixa no nosso plantel. Aceitam um empréstimo até ao fim da época?", "clube", youth.Id, lender.Id, "loan"));
            if (foe != null)
                list.Add(new PhoneNote(foe.Name, english
                    ? "Confirmed: we play on " + LocalizationManager.FormatDate(next.Date, "dd/MM") + (next.IsHome(club.Id) ? " at your ground. Get the team ready; we'll talk after the whistle." : " at our ground. See you there.")
                    : "Está confirmado: jogamos a " + LocalizationManager.FormatDate(next.Date, "dd/MM") + (next.IsHome(club.Id) ? " no teu campo. Prepara a equipa; falamos depois do apito." : " no nosso campo. Esperamos por vocês."), "clube", null, foe.Id, "fixture"));

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
                list.Add(new PhoneNote(string.IsNullOrEmpty(_career.Press[0].Paper) ? (english ? "Press" : "Imprensa") : _career.Press[0].Paper, LocalizationManager.Translate(_career.Press[0].Headline), "jornal", null, null));
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

            public PhoneNote(string who, string text, string channel, string playerId, string clubId, string replyGroup = null, string meta = null)
            {
                Who = who;
                Text = text;
                Channel = channel;
                PlayerId = playerId;
                ClubId = clubId;
                ReplyGroup = replyGroup ?? channel;
                Meta = meta;
                Key = channel + "|" + (playerId ?? "") + "|" + (clubId ?? "") + "|" + who + "|" + ReplyGroup;
            }
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
            var host = BroadcastUi.Zone(root, "Pages", 0.03f, 0.145f, 0.97f, 0.815f);
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
            var page = BroadcastUi.Panel(host, "Home", new Color(0.08f, 0.03f, 0.16f, 0.55f));
            BroadcastUi.Stretch(page.rectTransform);
            _homeTitle = BroadcastUi.Label(page.transform, "Title", "", 30, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(_homeTitle.rectTransform, 0.16f, 0.86f, 0.96f, 0.97f);
            var faceGo = new GameObject("EventFace", typeof(RectTransform), typeof(Image));
            var faceRect = faceGo.GetComponent<RectTransform>();
            faceRect.SetParent(page.transform, false);
            faceRect.anchorMin = faceRect.anchorMax = new Vector2(0.06f, 0.9f);
            faceRect.sizeDelta = new Vector2(72, 72);
            _homeFace = faceGo.GetComponent<Image>();
            _homeFace.preserveAspect = true;
            _homeFace.raycastTarget = false;
            _homeFace.gameObject.SetActive(false);
            var scroll = BroadcastUi.Scroll(page.transform, "Scroll", out var content);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.03f, 0.34f, 0.97f, 0.85f);
            _homeBody = BroadcastUi.Label(content, "Body", "", 20, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.Preferred(_homeBody, 180);
            _nextChip = BroadcastUi.Label(page.transform, "Next", "", 18, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(_nextChip.rectTransform, 0.04f, 0.22f, 0.96f, 0.32f);
            _replies = new Button[3];
            for (var i = 0; i < 3; i++)
            {
                var choice = i;
                _replies[i] = BroadcastUi.Button(page.transform, "Reply" + i, "", () => Answer(choice), new Color(0.16f, 0.08f, 0.3f, 0.95f), Color.white, 16);
                var y = 0.22f - i * 0.09f;
                BroadcastUi.Place(_replies[i].GetComponent<RectTransform>(), 0.04f, y, 0.96f, y + 0.08f);
                _replies[i].gameObject.SetActive(false);
            }

            return page.gameObject;
        }

        GameObject BuildNews(Transform host)
        {
            var page = BroadcastUi.Panel(host, "News", new Color(0.08f, 0.03f, 0.16f, 0.5f));
            BroadcastUi.Stretch(page.rectTransform);
            var title = BroadcastUi.Label(page.transform, "Title", "JORNAIS DO DIA", 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.03f, 0.9f, 0.7f, 0.98f);
            EnsureMemoriesButton(page.transform);
            var scroll = BroadcastUi.Scroll(page.transform, "Scroll", out _newsContent);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.02f, 0.03f, 0.98f, 0.88f);
            return page.gameObject;
        }

        void EnsureMemoriesButton(Transform newsPage)
        {
            if (newsPage == null)
                return;
            var existing = newsPage.Find("Memories");
            _memoriesButton = existing == null ? null : existing.GetComponent<Button>();
            if (_memoriesButton == null)
                _memoriesButton = BroadcastUi.Button(newsPage, "Memories", "MEMÓRIAS", OpenMemories,
                    new Color(1f, 1f, 1f, 0.08f), UiTheme.Accent, 13);
            else
            {
                _memoriesButton.onClick.RemoveAllListeners();
                _memoriesButton.onClick.AddListener(OpenMemories);
            }
            BroadcastUi.Place(_memoriesButton.GetComponent<RectTransform>(), 0.73f, 0.9f, 0.97f, 0.98f);
        }

        GameObject BuildBoard(Transform host)
        {
            var page = BroadcastUi.Panel(host, "Board", new Color(0.08f, 0.03f, 0.16f, 0.5f));
            BroadcastUi.Stretch(page.rectTransform);
            var english = LocalizationManager.Current == GameLanguage.English;
            var title = BroadcastUi.Label(page.transform, "Title", english ? "MY CAREER" : "A MINHA CARREIRA", 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
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
                    FillAcademy();
                }, new Color(1f, 1f, 1f, 0.08f), Color.white, 13);
                BroadcastUi.Place(_academyAgeTabs[i].GetComponent<RectTransform>(), 0.02f + i * 0.16f, 0.84f, 0.17f + i * 0.16f, 0.9f);
            }
            var views = english ? new[] { "STARTING XI", "BENCH", "LINE-UP", "FIXTURES" } : new[] { "ONZE INICIAL", "BANCO", "ONZE NO CAMPO", "JOGOS" };
            _academyViewTabs = new Button[views.Length];
            for (var i = 0; i < _academyViewTabs.Length; i++)
            {
                var index = i;
                _academyViewTabs[i] = BroadcastUi.Button(page, "AcademyView" + i, views[i], () =>
                {
                    _academyViewIndex = index;
                    FillAcademy();
                }, new Color(1f, 1f, 1f, 0.08f), Color.white, 13);
                var x0 = 0.51f + i * 0.12f;
                BroadcastUi.Place(_academyViewTabs[i].GetComponent<RectTransform>(), x0, 0.84f, x0 + 0.115f, 0.9f);
            }
            var scroll = BroadcastUi.Scroll(page, "AcademyScroll", out _academyContent);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.02f, 0.025f, 0.98f, 0.82f);
            FillAcademy();
        }

        GameObject BuildTraining(Transform host)
        {
            var page = BroadcastUi.Panel(host, "Training", new Color(0.08f, 0.03f, 0.16f, 0.5f));
            BroadcastUi.Stretch(page.rectTransform);
            var english = LocalizationManager.Current == GameLanguage.English;
            var title = BroadcastUi.Label(page.transform, "Title", english ? "TRAINING" : "TREINO", 18, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.025f, 0.92f, 0.4f, 0.99f);
            _trainingTeamTab = BroadcastUi.Button(page.transform, "TeamTraining", english ? "TEAM" : "EQUIPA", () => { _individualTraining = false; RefreshTrainingModeTabs(); FillTraining(); }, UiTheme.Accent, UiTheme.ButtonText, 14);
            BroadcastUi.Place(_trainingTeamTab.GetComponent<RectTransform>(), 0.55f, 0.92f, 0.74f, 0.99f);
            _trainingIndividualTab = BroadcastUi.Button(page.transform, "IndividualTraining", english ? "INDIVIDUAL" : "INDIVIDUAL", () => { _individualTraining = true; RefreshTrainingModeTabs(); FillTraining(); }, new Color(1f, 1f, 1f, 0.08f), Color.white, 14);
            BroadcastUi.Place(_trainingIndividualTab.GetComponent<RectTransform>(), 0.755f, 0.92f, 0.975f, 0.99f);
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
                ? new[] { "PLAYER", "FOCUS", "TYPE", "PROGRESS", "PROJECTED", "MATCH EXPERIENCE", "TREND" }
                : new[] { "JOGADOR", "FOCO", "TIPO", "PROGRESSO", "PREVISÃO", "EXPERIÊNCIA DE JOGO", "TENDÊNCIA" };
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
                var name = BroadcastUi.Label(row.transform, "Name", player.FullName, 11, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(name.rectTransform, columnBounds[0], 0.16f, columnBounds[1] - 0.008f, 0.84f);
                var current = Array.IndexOf(focusKeys, player.TrainingFocus ?? "");
                if (current < 0) current = i % focusNames.Length;
                player.TrainingFocus = focusKeys[current];
                var captured = player;
                var focus = BroadcastUi.Button(row.transform, "Focus", focusNames[current], () =>
                {
                    var index = Array.IndexOf(focusKeys, captured.TrainingFocus ?? "");
                    var nextFocus = focusKeys[(index + 1 + focusKeys.Length) % focusKeys.Length];
                    if (_individualTraining)
                        captured.TrainingFocus = nextFocus;
                    else
                        foreach (var teammate in Squad(_career.Club)) teammate.TrainingFocus = nextFocus;
                    CareerStore.Save(_career);
                    FillTraining();
                }, new Color(1f, 1f, 1f, 0.08f), Color.white, 12);
                BroadcastUi.Place(focus.GetComponent<RectTransform>(), columnBounds[1], 0.12f, columnBounds[2] - 0.008f, 0.88f);
                focus.GetComponentInChildren<Text>().fontSize = 10;
                var isRest = player.TrainingFocus == "rest";
                var isInjured = player.Injury != null && player.Injury.IsInjured;
                var type = BroadcastUi.Label(row.transform, "FocusType", isRest ? (english ? "RECOVERY" : "RECUPERAÇÃO") : (english ? "ATTRIBUTE" : "ATRIBUTO"), 9, UiTheme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(type.rectTransform, columnBounds[2], 0.12f, columnBounds[3] - 0.008f, 0.88f);
                var progressValue = Mathf.Clamp(player.TrainingProgress, 0, 99);
                var progress = progressValue / 100f;
                var bar = BroadcastUi.Zone(row.transform, "Progress", columnBounds[3], 0.43f, columnBounds[4] - 0.045f, 0.62f);
                var fill = BroadcastUi.Panel(bar, "Fill", UiTheme.Accent);
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.02f, progress), 1f);
                fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
                var value = BroadcastUi.Label(row.transform, "ProgressValue", progressValue + "%", 10, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(value.rectTransform, columnBounds[4] - 0.045f, 0.12f, columnBounds[4], 0.88f);
                var expectedIncrease = isRest || isInjured ? 0 : player.Age <= 24 ? 14 : 7;
                var projected = isRest || isInjured ? progressValue : Mathf.Min(100, progressValue + expectedIncrease);
                var projectedLabel = isInjured ? (english ? "INJURED" : "LESIONADO")
                    : isRest ? (english ? "RECOVERY" : "RECUPERAÇÃO") : projected + "%";
                var projection = BroadcastUi.Label(row.transform, "Projected", projectedLabel, 10, Color.white, TextAnchor.MiddleCenter);
                BroadcastUi.Place(projection.rectTransform, columnBounds[4], 0.12f, columnBounds[5] - 0.006f, 0.88f);
                var seasonLine = FindSeasonLine(player.Id);
                var appearances = seasonLine == null ? 0 : seasonLine.Apps;
                var experience = appearances < 3 ? (english ? "INSUFFICIENT" : "INSUFICIENTE")
                    : appearances < 8 ? (english ? "DEVELOPING" : "A DESENVOLVER")
                    : (english ? "SUFFICIENT" : "SUFICIENTE");
                var matchExperience = BroadcastUi.Label(row.transform, "MatchExperience", experience, 9,
                    appearances < 3 ? UiTheme.Danger : appearances < 8 ? UiTheme.FitnessLow : UiTheme.Fitness,
                    TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(matchExperience.rectTransform, columnBounds[5], 0.12f, columnBounds[6] - 0.006f, 0.88f);
                var trend = BroadcastUi.Label(row.transform, "Trend", isRest || isInjured ? "—" : "↗", 15, isRest || isInjured ? UiTheme.Muted : UiTheme.Fitness, TextAnchor.MiddleCenter, FontStyle.Bold);
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
                (english ? "Attribute" : "Atributo") + "   " + attributeCount + "\n" + (english ? "Recovery" : "Recuperação") + "   " + recoveryCount,
                11, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(breakdown.rectTransform, 0.54f, 0.27f, 0.96f, 0.65f);

            Clear(_trainingDistributionContent);
            var distribution = BroadcastUi.Panel(_trainingDistributionContent, "Distribution", new Color(0.035f, 0.075f, 0.1f, 0.96f), false);
            BroadcastUi.Stretch(distribution.rectTransform);
            var distributionTitle = BroadcastUi.Label(distribution.transform, "Title", english ? "DISTRIBUTION BY FOCUS" : "DISTRIBUIÇÃO POR FOCO", 11, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(distributionTitle.rectTransform, 0.045f, 0.88f, 0.96f, 0.99f);
            var maxCount = 1;
            var counts = new int[focusKeys.Length];
            foreach (var player in players)
            {
                var index = Array.IndexOf(focusKeys, player.TrainingFocus ?? "");
                if (index >= 0) counts[index]++;
                maxCount = Mathf.Max(maxCount, index >= 0 ? counts[index] : 0);
            }
            for (var i = 0; i < focusNames.Length; i++)
            {
                var top = 0.85f - i * 0.105f;
                var row = BroadcastUi.Zone(distribution.transform, "Focus" + i, 0.035f, top - 0.09f, 0.965f, top);
                var label = BroadcastUi.Label(row, "Name", focusNames[i], 9, Color.white, TextAnchor.MiddleLeft);
                BroadcastUi.Place(label.rectTransform, 0f, 0f, 0.52f, 1f);
                var value = BroadcastUi.Label(row, "Count", counts[i].ToString(), 9, UiTheme.Muted, TextAnchor.MiddleRight, FontStyle.Bold);
                BroadcastUi.Place(value.rectTransform, 0.52f, 0f, 0.64f, 1f);
                var track = BroadcastUi.Solid(row, "Track", new Color(1f, 1f, 1f, 0.12f));
                BroadcastUi.Place(track.rectTransform, 0.69f, 0.34f, 1f, 0.66f);
                var bar = BroadcastUi.Solid(track.transform, "Fill", UiTheme.Accent);
                BroadcastUi.Stretch(bar.rectTransform);
                bar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(counts[i] / (float)maxCount), 1f);
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
                ? new[] { "BUDGET", "MY CAREER", "CALENDAR", "STAFF" }
                : new[] { "ORÇAMENTO", "A MINHA CARREIRA", "CALENDÁRIO", "EQUIPA TÉCNICA" };
            _officeTabs = new Button[tabNames.Length];
            _officeViews = new GameObject[tabNames.Length];
            for (var i = 0; i < tabNames.Length; i++)
            {
                var captured = i;
                var x0 = 0.01f + i * 0.247f;
                _officeTabs[i] = BroadcastUi.Button(page, "OfficeTab" + i, tabNames[i], () => ShowOfficeTab(captured),
                    new Color(1f, 1f, 1f, 0.08f), Color.white, 14);
                BroadcastUi.Place(_officeTabs[i].GetComponent<RectTransform>(), x0, 0.855f, x0 + 0.237f, 0.925f);
                var view = BroadcastUi.Zone(page, "OfficeView" + i, 0.01f, 0.015f, 0.99f, 0.835f);
                _officeViews[i] = view.gameObject;
                view.gameObject.SetActive(false);
            }

            BuildOfficeBudget(_officeViews[0].transform);
            BuildBoard(_officeViews[1].transform);
            BuildCalendar(_officeViews[2].transform);
            var staffTitle = BroadcastUi.Label(_officeViews[3].transform, "StaffTitle", english ? "CLUB STAFF" : "EQUIPA TÉCNICA DO CLUBE", 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(staffTitle.rectTransform, 0.025f, 0.91f, 0.975f, 0.99f);
            var staffScroll = BroadcastUi.Scroll(_officeViews[3].transform, "StaffScroll", out _staffHost);
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
            else if (_officeTab == 1) FillBoard();
            else if (_officeTab == 2) FillCalendar();
            else if (_officeTab == 3) FillStaff();
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
                ? new[] { "FORMATIONS", "ROLES", "INSTRUCTIONS", "TACTICS" }
                : new[] { "FORMAÇÕES", "FUNÇÕES", "INSTRUÇÕES", "TÁTICAS" };
            _paneTabs = new Button[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                var pane = i;
                var y1 = 0.93f - i * 0.075f;
                _paneTabs[i] = BroadcastUi.Button(page.transform, "Pane" + i, labels[i], () => SelectPane(pane), new Color(1f, 1f, 1f, 0.08f), Color.white, 14);
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

        void FillSquadList()
        {
            Clear(_listHost);
            var english = LocalizationManager.Current == GameLanguage.English;
            var tracked = BroadcastUi.Label(_listHost, "TrackedTitle", english ? "SHORTLIST" : "LISTA DE OBSERVAÇÃO", 15, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Preferred(tracked, 30);
            var hasTargets = false;
            foreach (var id in _career.Targets)
            {
                if (!_career.World.Players.TryGetValue(id, out var target) || target.ClubId == _career.Club.Id) continue;
                hasTargets = true;
                AddShortlistRow(target, false);
            }
            if (!hasTargets)
            {
                var empty = BroadcastUi.Label(_listHost, "NoTargets", english ? "No players shortlisted. Add a player from the transfer market." : "Ainda não há jogadores na lista. Adiciona alguém no mercado de transferências.", 13, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Preferred(empty, 32);
            }

        }

        void AddShortlistRow(Player player, bool own)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var row = BroadcastUi.Panel(_listHost, "P", new Color(0.1f, 0.04f, 0.2f, 0.92f), true);
            BroadcastUi.Preferred(row, 64);
            var face = FaceImage(row.transform, player, 42f);
            face.rectTransform.anchorMin = face.rectTransform.anchorMax = new Vector2(0.04f, 0.5f);
            face.rectTransform.sizeDelta = new Vector2(42, 42);
            var end = player.ContractEndTicks > 0 && player.ContractEndTicks <= DateTime.MaxValue.Ticks ? new DateTime(player.ContractEndTicks).ToString("dd/MM/yyyy") : "—";
            var line = BroadcastUi.Label(row.transform, "Line", player.SquadNumber + "   " + player.FullName + "   ·   " + PresentationLabels.Position(player.Position) + "   ·   " + (english ? "Ends " : "Termina ") + end, 15, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(line.rectTransform, 0.08f, 0.12f, own ? 0.58f : 0.72f, 0.88f);
            var captured = player;
            var file = BroadcastUi.Button(row.transform, "File", english ? "PROFILE" : "FICHA", () => OpenPlayer(captured), UiTheme.Accent, UiTheme.ButtonText, 13);
            BroadcastUi.Place(file.GetComponent<RectTransform>(), own ? 0.6f : 0.76f, 0.18f, own ? 0.76f : 0.97f, 0.82f);
            if (own)
            {
                var renew = BroadcastUi.Button(row.transform, "Renew", english ? "NEGOTIATE" : "NEGOCIAR", () => OpenRenewalNegotiation(captured), new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 12);
                BroadcastUi.Place(renew.GetComponent<RectTransform>(), 0.78f, 0.18f, 0.97f, 0.82f);
            }
        }

        void FillMarket()
        {
            Clear(_marketHost);
            CompleteScouts();
            EnsureScout(_career.Club);
            ClaimForeignTargets();
            var scout = Member(_career.Club, StaffRole.Scout);
            Section("A MINHA LISTA");
            var wanted = false;
            foreach (var id in _career.Targets)
            {
                if (!_career.World.Players.TryGetValue(id, out var player) || player.ClubId == _career.Club.Id)
                    continue;
                wanted = true;
                MarketRow(player, false, scout, true);
            }

            if (!wanted)
                EmptyLine("Ainda não marcaste jogadores de outros clubes.");

            Section("PLANTEL À VENDA");
            var any = false;
            foreach (var id in _career.Listed)
            {
                if (!_career.World.Players.TryGetValue(id, out var player) || player.ClubId != _career.Club.Id)
                    continue;
                any = true;
                MarketRow(player, true, scout, false);
            }

            if (!any)
                EmptyLine("Ainda não puseste ninguém do plantel à venda.");

            Section("LISTA DE EMPRÉSTIMOS");
            var loaned = false;
            foreach (var id in _career.Loans)
            {
                if (!_career.World.Players.TryGetValue(id, out var player) || player.ClubId != _career.Club.Id)
                    continue;
                loaned = true;
                MarketRow(player, true, scout, false);
            }

            if (!loaned)
                EmptyLine("Ainda não puseste ninguém na lista de empréstimos.");

            var query = _marketSearch == null || _marketSearch.text == null ? "" : _marketSearch.text.Trim();
            Section(query.Length < 2 ? "PROCURAR NOUTROS CLUBES" : "RESULTADOS");
            if (query.Length < 2)
            {
                EmptyLine("Escreve pelo menos duas letras do nome.");
                return;
            }

            var shown = 0;
            foreach (var player in _career.World.Players.Values)
            {
                if (player.ClubId == _career.Club.Id || shown >= 40)
                    continue;
                if (player.FullName == null || player.FullName.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                shown++;
                MarketRow(player, false, scout, _career.Targets.Contains(player.Id));
            }

            if (shown == 0)
                EmptyLine("Nenhum jogador com esse nome.");
        }

        void Section(string title)
        {
            var head = BroadcastUi.Label(_marketHost, "Head", LocalizationManager.Translate(title), 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Preferred(head, 32);
        }

        void EmptyLine(string text)
        {
            var empty = BroadcastUi.Label(_marketHost, "Empty", LocalizationManager.Translate(text), 14, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Preferred(empty, 28);
        }

        void MarketRow(Player player, bool own, StaffMember scout, bool targeted)
        {
            var row = BroadcastUi.Panel(_marketHost, "M", new Color(0.1f, 0.04f, 0.2f, 0.92f), true);
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
            var clubName = _career.World.Clubs.ContainsKey(player.ClubId) ? _career.World.Clubs[player.ClubId].ShortName : "";
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
                    FillMarket();
                }, new Color(1f, 1f, 1f, 0.08f), Color.white, 13);
                BroadcastUi.Place(off.GetComponent<RectTransform>(), 0.78f, 0.18f, 0.97f, 0.82f);
                return;
            }

            if (targeted)
            {
                var drop = BroadcastUi.Button(row.transform, "Drop", LocalizationManager.Translate("TIRAR"), () =>
                {
                    _career.Targets.Remove(captured.Id);
                    FillMarket();
                }, new Color(1f, 1f, 1f, 0.08f), Color.white, 13);
                BroadcastUi.Place(drop.GetComponent<RectTransform>(), 0.64f, 0.18f, 0.78f, 0.82f);
            }
            else
            {
                var add = BroadcastUi.Button(row.transform, "Add", LocalizationManager.Translate("LISTA"), () =>
                {
                    _career.Targets.Add(captured.Id);
                    _career.Listed.Remove(captured.Id);
                    FillMarket();
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

            var sign = BroadcastUi.Button(row.transform, "Sign", LocalizationManager.Translate("CONTRATAR"), () => Sign(captured), new Color(0.15f, 0.55f, 0.28f, 1f), Color.white, 13);
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
            var now = _career.Calendar.CurrentDate.Date;
            for (var i = _career.ScoutJobs.Count - 1; i >= 0; i--)
            {
                var job = _career.ScoutJobs[i];
                if (new DateTime(job.Due).Date > now)
                    continue;
                _career.ScoutedPlayers.Add(job.PlayerId);
                _career.ScoutJobs.RemoveAt(i);
                if (_career.World.Players.TryGetValue(job.PlayerId, out var player))
                {
                    _career.Board.Insert(0, new CoachNote
                    {
                        Day = _career.Calendar.WeekdayName,
                        Hour = _career.Calendar.ClockText,
                        When = _career.Calendar.CurrentDate,
                        Text = "O olheiro entregou o relatório de " + player.FullName + "."
                    });
                }
            }
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

        static string Euro(int value)
        {
            if (LocalizationManager.Current == GameLanguage.English)
            {
                if (value >= 1000000000)
                    return "€" + (value / 1000000000f).ToString("0.0", CultureInfo.GetCultureInfo("en-GB")) + "bn";
                if (value >= 1000000)
                    return "€" + Mathf.RoundToInt(value / 1000000f) + "M";
                return "€" + value.ToString("N0", CultureInfo.GetCultureInfo("en-GB"));
            }
            if (value >= 1000000000)
                return (value / 1000000000f).ToString("0.0", CultureInfo.GetCultureInfo("pt-PT")) + " mil milhões €";
            if (value >= 1000000)
                return Mathf.RoundToInt(value / 1000000f) + "M €";
            return value.ToString("N0", CultureInfo.GetCultureInfo("pt-PT")) + " €";
        }

        void Sign(Player player)
        {
            if (player == null || player.ClubId == _career.Club.Id || !_career.ScoutedPlayers.Contains(player.Id))
                return;
            if (!_career.World.Clubs.TryGetValue(player.ClubId ?? "", out var seller) || seller.PlayerIds.Count <= 11)
                return;
            var cost = GlobalTransferMarket.RequiredFee(_career, player);
            if (_career.Club.Finance != null && _career.Club.Finance.TransferBudget < cost)
                return;
            var previousClubId = seller.Id;
            var previousClubName = seller.Name;
            var deal = GlobalTransferMarket.CompleteDeal(_career, player, seller, _career.Club, cost, 3, "User transfer");
            if (deal == null) return;
            _career.Listed.Remove(player.Id);
            _career.Targets.Remove(player.Id);
            _career.ScoutedPlayers.Remove(player.Id);
            Newsroom.PublishTransferDeal(_career, deal);
            CareerMemories.Record(_career, new FootballMemory
            {
                WhenTicks = _career.Calendar.CurrentDate.Ticks,
                Importance = 3,
                Kind = "contratacao",
                ClubId = _career.Club.Id,
                RelatedClubId = previousClubId,
                PlayerId = player.Id,
                Title = "Novo reforço: " + player.FullName,
                Detail = player.FullName + " chegou ao " + _career.Club.Name + " vindo de " + previousClubName + " por " + Euro(cost) + "."
            });
            CareerNotificationQueue.Enqueue("REFORÇO CONFIRMADO", player.FullName + " juntou-se ao " + _career.Club.Name + ". A contratação fica registada na história do clube.");
            CareerStore.Save(_career);
            FillMarket();
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

            var seller = _career.Club;
            var years = player.ContractEndTicks > _career.Calendar.CurrentDate.Ticks
                ? Mathf.Clamp(new DateTime(player.ContractEndTicks).Year - _career.Calendar.CurrentDate.Year, 1, 5) : 1;
            var deal = GlobalTransferMarket.CompleteDeal(_career, player, seller, buyer, fee, years, "User sale");
            if (deal == null) return;
            _career.Listed.Remove(player.Id);
            _career.Loans.Remove(player.Id);
            Newsroom.PublishTransferDeal(_career, deal);

            var english = LocalizationManager.Current == GameLanguage.English;
            CareerMemories.Record(_career, new FootballMemory
            {
                WhenTicks = _career.Calendar.CurrentDate.Ticks,
                Importance = 3,
                Kind = "venda",
                ClubId = seller.Id,
                RelatedClubId = buyer.Id,
                PlayerId = player.Id,
                Title = english ? "Player sold: " + player.FullName : "Jogador vendido: " + player.FullName,
                Detail = english
                    ? player.FullName + " joined " + buyer.Name + " for " + Euro(fee) + "."
                    : player.FullName + " assinou pelo " + buyer.Name + " por " + Euro(fee) + "."
            });
            CareerNotificationQueue.Enqueue(english ? "TRANSFER ACCEPTED" : "TRANSFERÊNCIA ACEITE",
                english ? player.FullName + " has joined " + buyer.Name + ". Fee received: " + Euro(fee) + "."
                    : player.FullName + " assinou pelo " + buyer.Name + ". Valor recebido: " + Euro(fee) + ".");
            CareerStore.Save(_career);
            CloseModal();
            FillTeam();
            FillMarket();
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
                Judgement = 14
            });
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
            var index = 0;
            const int count = 9;
            TacticButton(index++, count, "Pressão", "Quando a equipa sai à bola sem posse.", new[] { "Pressão muito alta", "Pressão alta", "Pressão média", "Pressão baixa" }, t.Pressing, value => t.Pressing = value);
            TacticButton(index++, count, "Linha defensiva", "Altura do bloco sem bola.", new[] { "Linha muito alta", "Linha defensiva alta", "Linha defensiva média", "Linha defensiva baixa", "Linha muito baixa" }, t.DefensiveLine, value => t.DefensiveLine = value);
            TacticButton(index++, count, "Construção", "Como a bola sai da defesa.", new[] { "Construção curta", "Jogo misto", "Construção longa" }, t.BuildUp, value => t.BuildUp = value);
            TacticButton(index++, count, "Laterais", "O que os laterais fazem com a posse.", new[] { "Laterais ficam", "Laterais equilibram", "Laterais sobem" }, string.IsNullOrEmpty(t.FullBacks) ? "Laterais equilibram" : t.FullBacks, value => t.FullBacks = value);
            TacticButton(index++, count, "Largura", "Onde a equipa ocupa o campo.", new[] { "Largura estreita", "Largura normal", "Largura larga" }, string.IsNullOrEmpty(t.Width) ? "Largura normal" : t.Width, value => t.Width = value);
            TacticButton(index++, count, "Ritmo", "Velocidade com que a posse avança.", new[] { "Ritmo lento", "Ritmo normal", "Ritmo rápido" }, string.IsNullOrEmpty(t.Tempo) ? "Ritmo normal" : t.Tempo, value => t.Tempo = value);
            TacticButton(index++, count, "Risco no passe", "Seguro fica curto. Arriscado parte linhas.", new[] { "Passe seguro", "Passe misto", "Passe arriscado" }, string.IsNullOrEmpty(t.PassingStyle) ? "Passe misto" : t.PassingStyle, value => t.PassingStyle = value);
            TacticButton(index++, count, "Marcação", "Zonal guarda zonas. Ao homem segue o adversário.", new[] { "Marcação zonal", "Marcação mista", "Marcação ao homem" }, string.IsNullOrEmpty(t.Marking) ? "Marcação mista" : t.Marking, value => t.Marking = value);
            TacticButton(index, count, "Ideia de jogo", "O que a equipa procura com a bola.", new[] { "Equilibrado", "Tiki-taka", "Posse", "Cruzamentos", "Jogo direto", "Bolas longas", "Remates de longe", "Contra-ataque" }, string.IsNullOrEmpty(t.AttackIdea) ? "Equilibrado" : t.AttackIdea, value => t.AttackIdea = value);
        }

        void TacticButton(int index, int count, string title, string help, string[] options, string current, Action<string> pick)
        {
            var step = 1f / count;
            var y1 = 1f - index * step;
            var shown = string.IsNullOrEmpty(current) ? "escolher" : current;
            var button = BroadcastUi.Button(_tacticsBody, "Tac" + index, title.ToUpperInvariant() + "\n" + shown, () =>
            {
                ShowTacticChoices(title, help, options, current, pick);
            }, new Color(1f, 1f, 1f, 0.08f), Color.white, 13);
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.alignment = TextAnchor.MiddleCenter;
            BroadcastUi.Place(button.GetComponent<RectTransform>(), 0.02f, y1 - step + 0.008f, 0.98f, y1 - 0.006f);
        }

        void ShowTacticChoices(string title, string help, string[] options, string current, Action<string> pick)
        {
            var layer = BroadcastUi.Zone(_tacticsBody, "Choices", 0f, 0f, 1f, 1f);
            var dim = BroadcastUi.Solid(layer, "Dim", new Color(0.04f, 0.01f, 0.1f, 0.55f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            BroadcastUi.Hit(dim, FillTactics);
            var card = BroadcastUi.Panel(layer, "Card", new Color(0.12f, 0.05f, 0.24f, 0.98f), true);
            BroadcastUi.Place(card.rectTransform, 0.02f, 0.04f, 0.98f, 0.96f);
            var head = BroadcastUi.Label(card.transform, "Title", title.ToUpperInvariant(), 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(head.rectTransform, 0.06f, 0.86f, 0.78f, 0.97f);
            var note = BroadcastUi.Label(card.transform, "Help", help, 13, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.Place(note.rectTransform, 0.06f, 0.74f, 0.94f, 0.86f);
            var room = 0.7f;
            var step = room / Mathf.Max(1, options.Length);
            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var on = option == current;
                var y1 = 0.72f - i * step;
                var row = BroadcastUi.Button(card.transform, "Opt" + i, option, () =>
                {
                    pick(option);
                    _career.Club.Tactics.Summary = _career.Club.Tactics.Formation + " · " + _career.Club.Tactics.Pressing + " · " + _career.Club.Tactics.BuildUp;
                    FillTactics();
                    if (_teamTitle != null)
                        _teamTitle.text = "PLANTEL  ·  " + _career.Club.Tactics.Formation;
                }, on ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), on ? UiTheme.ButtonText : Color.white, 14);
                BroadcastUi.Place(row.GetComponent<RectTransform>(), 0.06f, y1 - step + 0.008f, 0.94f, y1 - 0.004f);
            }
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
            var oldTitle = page.Find("Title");
            if (oldTitle != null) oldTitle.gameObject.SetActive(false);
            var oldVibe = page.Find("Vibe");
            if (oldVibe != null) oldVibe.gameObject.SetActive(false);
            var oldLine = page.Find("Line");
            if (oldLine != null) oldLine.gameObject.SetActive(false);
            var title = page.Find("TransfersTitle")?.GetComponent<Text>();
            if (title == null)
                title = BroadcastUi.Label(page, "TransfersTitle", "TRANSFERS", 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.text = LocalizationManager.Current == GameLanguage.English ? "transfers" : "transferências";
            BroadcastUi.Place(title.rectTransform, 0.03f, 0.91f, 0.5f, 0.99f);

            if (_listPage == null)
            {
                var list = BroadcastUi.Panel(page, "ListPage", new Color(0.05f, 0.02f, 0.12f, 0.2f));
                BroadcastUi.Scroll(list.transform, "ListScroll", out _listHost);
                _listPage = list.gameObject;
            }
            if (_marketPage == null)
            {
                var market = BroadcastUi.Panel(page, "MarketPage", new Color(0.05f, 0.02f, 0.12f, 0.2f));
                BroadcastUi.Scroll(market.transform, "MarketScroll", out _marketHost);
                _marketPage = market.gameObject;
            }
            _listPage.transform.SetParent(page, false);
            _marketPage.transform.SetParent(page, false);
            BroadcastUi.Place(_listPage.GetComponent<RectTransform>(), 0.02f, 0.02f, 0.98f, 0.82f);
            BroadcastUi.Place(_marketPage.GetComponent<RectTransform>(), 0.02f, 0.02f, 0.98f, 0.82f);
            if (_marketSearch == null)
            {
                var search = _marketPage.transform.Find("Input");
                _marketSearch = search == null ? UiFactory.Input(_marketPage.transform, "Search players by name") : search.GetComponent<InputField>();
                _marketSearch.name = "Input";
            }
            if (_marketSearch != null)
            {
                BroadcastUi.Place(_marketSearch.GetComponent<RectTransform>(), 0.02f, 0.9f, 0.98f, 0.98f);
                _marketSearch.onValueChanged.RemoveAllListeners();
                _marketSearch.onValueChanged.AddListener(_ => { if (_transferPage == 1) FillMarket(); });
            }
            var listScroll = _listPage.transform.Find("ListScroll");
            if (listScroll != null) BroadcastUi.Place(listScroll.GetComponent<RectTransform>(), 0.01f, 0.02f, 0.99f, 0.98f);
            var marketScroll = _marketPage.transform.Find("MarketScroll");
            if (marketScroll != null) BroadcastUi.Place(marketScroll.GetComponent<RectTransform>(), 0.01f, 0.02f, 0.99f, 0.88f);

            _transferTabs = new Button[3];
            var labels = LocalizationManager.Current == GameLanguage.English ? new[] { "SHORTLIST", "TRANSFER MARKET", "GLOBAL HISTORY" } : new[] { "LISTA", "MERCADO DE TRANSFERÊNCIAS", "HISTÓRICO GLOBAL" };
            for (var i = 0; i < _transferTabs.Length; i++)
            {
                var tab = i;
                var existing = page.Find("TransferTab" + i);
                _transferTabs[i] = existing == null
                    ? BroadcastUi.Button(page, "TransferTab" + i, labels[i], () => ShowTransfersZone(tab), new Color(1f, 1f, 1f, 0.08f), Color.white, 14)
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
            if (_listPage != null) _listPage.SetActive(_transferPage == 0);
            if (_marketPage != null) _marketPage.SetActive(_transferPage != 0);
            if (_marketSearch != null) _marketSearch.gameObject.SetActive(_transferPage == 1);
            if (_transferPage == 0) FillSquadList();
            else if (_transferPage == 1) FillMarket();
            else FillTransferHistory();
        }

        void FillTransferHistory()
        {
            if (_marketHost == null || _career == null) return;
            Clear(_marketHost);
            var english = LocalizationManager.Current == GameLanguage.English;
            var title = BroadcastUi.Label(_marketHost, "HistoryTitle", english ? "COMPLETED TRANSFERS AROUND THE WORLD" : "TRANSFERÊNCIAS CONCLUÍDAS NO MUNDO", 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Preferred(title, 34);
            if (_career.TransferHistory.Count == 0)
            {
                var empty = BroadcastUi.Label(_marketHost, "NoHistory", english ? "No completed transfers have been recorded yet." : "Ainda não há transferências concluídas registadas.", 14, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Preferred(empty, 42);
                return;
            }

            for (var i = 0; i < _career.TransferHistory.Count; i++)
            {
                var deal = _career.TransferHistory[i];
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

        GameObject BuildCalendar(Transform host)
        {
            var page = BroadcastUi.Panel(host, "Calendar", new Color(0.08f, 0.03f, 0.16f, 0.45f));
            BroadcastUi.Stretch(page.rectTransform);
            var english = LocalizationManager.Current == GameLanguage.English;
            var title = BroadcastUi.Label(page.transform, "Title", english ? "CALENDAR  ·  assistant notes for each match" : "CALENDÁRIO  ·  nota do adjunto em cada jogo", 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.03f, 0.92f, 0.62f, 0.99f);
            var trip = BroadcastUi.Button(page.transform, "Trip", english ? "PLAN TRAVEL" : "MARCAR VIAGEM", OpenTrips, UiTheme.Accent, UiTheme.ButtonText, 14);
            BroadcastUi.Place(trip.GetComponent<RectTransform>(), 0.64f, 0.91f, 0.97f, 0.99f);
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
            var legend = BroadcastUi.Label(page.transform, "Legend", "J jogos    V vitórias    E empates    D derrotas    GM golos marcados    GS golos sofridos    Pts pontos", 13, UiTheme.Muted, TextAnchor.MiddleRight);
            BroadcastUi.Place(legend.rectTransform, 0.22f, 0.92f, 0.98f, 0.99f);
            _tableContent = BroadcastUi.Zone(page.transform, "Grid", 0.01f, 0.02f, 0.99f, 0.9f);
            return page.gameObject;
        }

        void BuildPlay(Transform root)
        {
            var play = BroadcastUi.Button(root, "PlayMatch", "CONTINUAR", OnContinue, UiTheme.Accent, UiTheme.ButtonText, 20);
            _play = play;
            BroadcastUi.Place(play.GetComponent<RectTransform>(), 0.3f, 0.03f, 0.7f, 0.12f);
            _playLabel = play.GetComponentInChildren<Text>();
        }

        void BuildModal(Transform root)
        {
            var modal = BroadcastUi.Zone(root, "Modal", 0f, 0f, 1f, 1f);
            var dim = BroadcastUi.Solid(modal, "Dim", new Color(0.03f, 0.01f, 0.08f, 0.78f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            BroadcastUi.Hit(dim, CloseModal);
            var card = BroadcastUi.Panel(modal, "Card", new Color(0.12f, 0.05f, 0.24f, 0.98f), true);
            BroadcastUi.Place(card.rectTransform, 0.04f, 0.03f, 0.96f, 0.97f);
            _modalTitle = BroadcastUi.Label(card.transform, "Title", "", 26, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(_modalTitle.rectTransform, 0.14f, 0.88f, 0.96f, 0.97f);
            _modalLead = BroadcastUi.Label(card.transform, "Lead", "", 15, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(_modalLead.rectTransform, 0.05f, 0.8f, 0.95f, 0.88f);
            var scroll = BroadcastUi.Scroll(card.transform, "Scroll", out _modalBody);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.04f, 0.16f, 0.96f, 0.72f);
            _modalAction = BroadcastUi.Button(card.transform, "Action", "", null, UiTheme.Accent, UiTheme.ButtonText, 16);
            BroadcastUi.Place(_modalAction.GetComponent<RectTransform>(), 0.32f, 0.04f, 0.78f, 0.13f);
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
            return false;
        }

        void OnContinue()
        {
            var moment = _career.Current;
            if (moment != null && moment.Kind == MomentKind.Talk && !moment.Answered)
                return;
            if (moment != null && moment.Kind == MomentKind.Match && moment.Fixture != null && !moment.Fixture.Played)
            {
                SeasonSchedule.SimulateOthers(_career, moment.Fixture.Date, moment.Fixture);
                if (SettingsPrefs.AutosaveBeforeMatch)
                    CareerStore.AutoSave(_career, SettingsPrefs.AutosaveSlots);
                GameRoot.Instance.LiveMatch = MatchSession.Create(_career, moment.Fixture, SettingsPrefs.AutomaticSquadSelection);
                GameRoot.Instance.Open(SceneNames.MatchDay);
                return;
            }

            var previousDay = _career.Calendar.CurrentDate.Date;
            CareerMoments.Continue(_career);
            var elapsedDays = (_career.Calendar.CurrentDate.Date - previousDay).Days;
            if (elapsedDays > 0)
                AdvancePlayerSystems(elapsedDays);
            if (SettingsPrefs.AutosaveEnabled)
                CareerStore.AutoSaveIfDue(_career, SettingsPrefs.AutosaveDays, SettingsPrefs.AutosaveSlots);
            CareerStore.Save(_career);
            Refresh();
            FillNews();
        }

        void Answer(int choice)
        {
            CareerMoments.Answer(_career, choice);
            CareerStore.Save(_career);
            Refresh();
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

        void Refresh()
        {
            RefreshLocalizedPresentation();
            CompleteScouts();
            FillBoard();
        }

        void RefreshLocalizedPresentation()
        {
            var moment = _career.Current;
            var season = _career.League != null ? LocalizationManager.Translate(_career.League.Name) + " " + _career.Calendar.SeasonLabel : _career.Calendar.SeasonLabel;
            _clock.text = _career.Calendar.ClockText;
            _dateLine.text = LocalizationManager.FormatDate(_career.Calendar.CurrentDate, "dddd, d 'de' MMMM") + "    ·    " + season + "    ·    " + LocalizationManager.Translate("Mister") + " " + _career.Coach.Name;
            _homeTitle.text = CareerMomentTitle(moment);
            Player facePlayer = null;
            if (moment != null && !string.IsNullOrEmpty(moment.PlayerId))
                _career.World.Players.TryGetValue(moment.PlayerId, out facePlayer);
            if (_homeFace != null)
            {
                _homeFace.gameObject.SetActive(facePlayer != null);
                if (facePlayer != null)
                    _homeFace.sprite = FaceBook.For(facePlayer);
            }
            BroadcastUi.Place(_homeTitle.rectTransform, facePlayer == null ? 0.04f : 0.16f, 0.86f, 0.96f, 0.97f);
            var body = moment == null ? "A época ficou em silêncio." : CareerMomentText(moment);
            BroadcastUi.SetWrapped(_homeBody, body, 80);
            _nextChip.text = LocalizationManager.Current == GameLanguage.English
                ? "Next match    " + EnglishMatchLabel(_career)
                : LocalizationManager.Translate("Próximo jogo    " + NextMatchText(_career));
            var talking = moment != null && moment.Kind == MomentKind.Talk && !moment.Answered && moment.Replies != null;
            if (moment != null && moment.Kind == MomentKind.Match)
                _playLabel.text = LocalizationManager.Current == GameLanguage.English
                    ? "PLAY    " + EnglishMatchLabel(_career)
                    : LocalizationManager.Translate("JOGAR    " + MatchLabel(_career, moment.Fixture));
            else if (talking)
                _playLabel.text = LocalizationManager.Current == GameLanguage.English ? "CHOOSE A RESPONSE" : LocalizationManager.Translate("ESCOLHE UMA RESPOSTA");
            else
                _playLabel.text = LocalizationManager.Current == GameLanguage.English ? "CONTINUE" : LocalizationManager.Translate("CONTINUAR");
            if (_play != null)
                _play.interactable = !talking;

            _nextChip.gameObject.SetActive(!talking);
            for (var i = 0; i < _replies.Length; i++)
            {
                var show = talking && i < moment.Replies.Length;
                _replies[i].gameObject.SetActive(show);
                if (show)
                    _replies[i].GetComponentInChildren<Text>().text = (i + 1) + "    " + CareerMomentReply(moment, i);
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

        void Show(int index)
        {
            if (index == 2) FillAcademy();
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
            const int columns = 4;
            var leagues = _career.World.Leagues;
            var rows = Mathf.CeilToInt(leagues.Count / (float)columns);
            var width = 0.99f / columns;
            var height = 0.96f / rows;
            for (var i = 0; i < leagues.Count; i++)
            {
                var x = i % columns;
                var y = i / columns;
                var x0 = x * width;
                var y1 = 0.99f - y * height;
                var y0 = y1 - height + 0.006f;
                var cell = BroadcastUi.Zone(_tableContent, "LeagueCell" + i, x0, y0, x0 + width - 0.006f, y1);
                TableCard(cell, leagues[i].Id, 0.02f, 0.98f);
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

            var head = BroadcastUi.Label(card.transform, "League", title.ToUpperInvariant(), 13, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(head.rectTransform, 0.04f, 0.9f, 0.62f, 0.99f);
            var moreId = leagueId;
            var more = BroadcastUi.Button(card.transform, "More", "DETALHES", () => OpenLeagueStats(moreId), new Color(1f, 1f, 1f, 0.12f), Color.white, 11);
            BroadcastUi.Place(more.GetComponent<RectTransform>(), 0.64f, 0.9f, 0.97f, 0.99f);
            var siglas = BroadcastUi.Label(card.transform, "Siglas", "PONTOS", 11, UiTheme.Accent, TextAnchor.MiddleRight, FontStyle.Bold);
            BroadcastUi.Place(siglas.rectTransform, 0.4f, 0.82f, 0.98f, 0.91f);
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
            var mineIndex = -1;
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].ClubId == _career.Club.Id)
                    mineIndex = i;
                if (i < 4)
                    indexes.Add(i);
            }

            if (mineIndex >= 4)
                indexes.Add(mineIndex);
            else if (rows.Count > 4)
                indexes.Add(4);
            var step = 0.62f / 5f;
            for (var n = 0; n < indexes.Count; n++)
            {
                var i = indexes[n];
                var row = rows[i];
                var y = 0.78f - (n + 1) * step;
                var mineRow = row.ClubId == _career.Club.Id;
                var line = BroadcastUi.Zone(card.transform, "R" + i, 0.03f, y, 0.97f, y + step - 0.008f);
                Club club = null;
                if (_career.World.Clubs.ContainsKey(row.ClubId))
                    club = _career.World.Clubs[row.ClubId];
                var crest = CrestImage(line, club, 18f);
                crest.rectTransform.anchorMin = crest.rectTransform.anchorMax = new Vector2(0.08f, 0.5f);
                crest.rectTransform.sizeDelta = new Vector2(18, 18);
                var name = club == null ? row.ClubId : club.ShortName;
                if (club != null)
                {
                    var selectedClub = club;
                    var clubButton = BroadcastUi.Button(line, "N", (i + 1) + "  " + name, () => OpenClubDossier(selectedClub), new Color(1f, 1f, 1f, 0.01f), mineRow ? UiTheme.Score : Color.white, 14);
                    clubButton.GetComponentInChildren<Text>().fontStyle = mineRow ? FontStyle.Bold : FontStyle.Normal;
                    BroadcastUi.Place(clubButton.GetComponent<RectTransform>(), 0.16f, 0.08f, 0.72f, 0.92f);
                }
                else
                {
                    var label = BroadcastUi.Label(line, "N", (i + 1) + "  " + name, 14, mineRow ? UiTheme.Score : Color.white, TextAnchor.MiddleLeft, mineRow ? FontStyle.Bold : FontStyle.Normal);
                    BroadcastUi.Place(label.rectTransform, 0.16f, 0.08f, 0.72f, 0.92f);
                }
                var numbers = BroadcastUi.Label(line, "S", row.Points + " pts", 14, mineRow ? UiTheme.Score : UiTheme.Muted, TextAnchor.MiddleRight, FontStyle.Bold);
                BroadcastUi.Place(numbers.rectTransform, 0.7f, 0.08f, 0.98f, 0.92f);
            }

            if (rows.Count > indexes.Count)
            {
                var moreLine = BroadcastUi.Label(card.transform, "Rest", "+" + (rows.Count - indexes.Count) + " clubes em Detalhes", 12, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Place(moreLine.rectTransform, 0.06f, 0.02f, 0.94f, 0.12f);
            }
        }

        void FillNews()
        {
            if (_memoriesButton != null)
            {
                var label = _memoriesButton.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = "MEMÓRIAS  ·  " + _career.Memories.Count;
            }
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
        }

        string PaperName(string paper)
        {
            if (paper == "The Football Wire") return LocalizationManager.Current == GameLanguage.English ? paper : "A Voz do Futebol";
            if (LocalizationManager.Current != GameLanguage.English) return paper;
            if (paper == "A Gazeta do Relvado") return "The Touchline Gazette";
            if (paper == "O Bancada") return "The Stand";
            if (paper == "Diário da Liga") return "League Daily";
            return paper;
        }

        string PressKicker(string kicker)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            if (kicker == "GLOBAL") return english ? "WORLD FOOTBALL" : "FUTEBOL MUNDIAL";
            if (kicker == "GLOBAL TRANSFER") return english ? "TRANSFER CONFIRMED" : "TRANSFERÊNCIA CONFIRMADA";
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
            if (story.Kicker == "GLOBAL TRANSFER")
                return LocalizationManager.Current == GameLanguage.English
                    ? "The club moved quickly and got its deal over the line."
                    : "O clube avançou rapidamente e fechou o negócio.";
            if (LocalizationManager.Current != GameLanguage.English)
                return story == null ? "" : story.Quote;
            if (story.Kicker == "MERCADO") return "I won't comment on names. Any discussion is with the club, not the papers.";
            if (story.Kicker == "ENTREVISTA") return "If the ball reaches me in the box, I won't think twice.";
            return "We went looking for the goal. The result reflects what we saw from the touchline.";
        }

        string PressBody(PaperStory story)
        {
            if (story == null) return "";
            var english = LocalizationManager.Current == GameLanguage.English;
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
            if (story.Kicker == "JOGO")
            {
                var fixture = PressFixture(story);
                if (fixture != null)
                {
                    var home = _career.World.Clubs[fixture.HomeClubId];
                    var away = _career.World.Clubs[fixture.AwayClubId];
                    var userGoals = fixture.IsHome(_career.Club.Id) ? fixture.HomeGoals : fixture.AwayGoals;
                    var opponentGoals = fixture.IsHome(_career.Club.Id) ? fixture.AwayGoals : fixture.HomeGoals;
                    var outcome = userGoals > opponentGoals ? "The team found a way to turn pressure into a result." :
                        userGoals < opponentGoals ? "The opposition were more clinical in the final third." :
                        "Neither side could break the deadlock in the closing minutes.";
                    return home.Name + " and " + away.Name + " finished " + fixture.HomeGoals + "–" + fixture.AwayGoals + ".\n\n"
                        + _career.Club.Name + " lined up in " + _career.Club.Tactics.Formation + ". " + outcome
                        + "\n\nPlayer of the match: " + (story.Motm ?? "—") + ".\n\n“" + PressQuote(story) + "”\n— " + (story.QuoteBy ?? _career.Coach.Name) + ", manager";
                }
            }
            if (story.Kicker == "ENTREVISTA")
            {
                var opponent = story.AwayClubId != null && _career.World.Clubs.TryGetValue(story.AwayClubId, out var away) ? away : null;
                return _career.Club.Name + " face " + (opponent == null ? "their next opponent" : opponent.Name) + ". The opposition are expected to use "
                    + (opponent == null ? "their usual shape" : opponent.Tactics.Formation) + ".\n\nThe assistant has highlighted " + (story.Motm ?? "a key player") + " as a possible difference-maker.\n\n“"
                    + PressQuote(story) + "”\n— " + (story.QuoteBy ?? _career.Coach.Name);
            }
            if (story.Kicker == "MERCADO")
            {
                var seller = story.AwayClubId != null && _career.World.Clubs.TryGetValue(story.AwayClubId, out var owner) ? owner : null;
                return "Transfer talk around " + (story.Motm ?? "the player") + " has resurfaced. " + _career.Club.ShortName + " are monitoring the situation, but no offer has been confirmed.\n\n“"
                    + PressQuote(story) + "”\n— " + _career.Coach.Name + "\n\n"
                    + (seller == null ? "The player's club has made no formal announcement." : seller.Name + " say the player is focused on the next match. There is no confirmed agreement.");
            }
            var next = _career.NextUserFixture();
            var opponentClub = next == null ? null : _career.World.Clubs[next.OpponentId(_career.Club.Id)];
            var nextText = opponentClub == null ? "No match has been scheduled yet." : "The next fixture is against " + opponentClub.Name + ".";
            return _career.Club.Name + " begin the season with " + _career.Coach.Name + " in charge. " + nextText
                + " The squad's tactical identity is " + LocalizationManager.Translate(_career.Club.Tactics.Summary) + ". The board expects the club to keep progressing.";
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
            foreach (var fixture in _career.Fixtures)
                if (fixture.Played && fixture.HomeClubId == story.HomeClubId && fixture.AwayClubId == story.AwayClubId)
                    return fixture;
            return null;
        }

        void OpenMemories()
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var memories = new List<FootballMemory>(_career.Memories);
            memories.Sort((a, b) => b.WhenTicks.CompareTo(a.WhenTicks));
            OpenModal(english ? "CAREER MEMORIES" : "MEMÓRIAS DA CARREIRA", memories.Count == 0
                ? (english ? "Important moments are saved here throughout your career." : "Os momentos importantes ficam guardados aqui ao longo da carreira.")
                : memories.Count + (english ? " moments saved · your story follows your decisions." : " momentos guardados · a história acompanha as tuas decisões."), false);
            if (memories.Count == 0)
            {
                AddLine(english
                    ? "No moments have been recorded yet. Debuts, big matches, decisions and important changes will appear here."
                    : "Ainda não há momentos registados. Estreias, grandes jogos, decisões e mudanças importantes aparecerão aqui.");
                return;
            }

            for (var i = 0; i < memories.Count; i++)
            {
                var memory = memories[i];
                if (memory == null)
                    continue;
                var date = memory.WhenTicks > 0 && memory.WhenTicks <= DateTime.MaxValue.Ticks
                    ? LocalizationManager.FormatDate(new DateTime(memory.WhenTicks), "d MMM yyyy")
                    : english ? "Date unavailable" : "Data não disponível";
                var club = _career.World.Clubs.TryGetValue(memory.ClubId ?? "", out var memoryClub) ? memoryClub.ShortName : _career.Club.ShortName;
                var importance = new string('★', Mathf.Clamp(memory.Importance, 1, 5));
                AddHead(importance + "   " + memory.Title);
                AddLine(date + "  ·  " + club + (string.IsNullOrEmpty(memory.CompetitionId) ? "" : "  ·  " + memory.CompetitionId)
                    + "\n" + (memory.Detail ?? ""));
            }
        }

        void FillBoard()
        {
            Clear(_boardContent);
            if (_boardContent == null)
                return;
            var english = LocalizationManager.Current == GameLanguage.English;
            var digest = BroadcastUi.Panel(_boardContent, "Digest", new Color(0.1f, 0.28f, 0.34f, 0.92f));
            BroadcastUi.Preferred(digest, 240);
            var head = BroadcastUi.Label(digest.transform, "Head", english ? "TODAY'S STATUS" : "ESTADO DO DIA", 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(head.rectTransform, 0.04f, 0.72f, 0.96f, 0.94f);
            var body = BroadcastUi.Label(digest.transform, "Body", ClubDigest(), 16, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.Place(body.rectTransform, 0.04f, 0.06f, 0.96f, 0.72f);

            var report = BroadcastUi.Panel(_boardContent, "WeekReport", new Color(0.16f, 0.08f, 0.28f, 0.94f));
            BroadcastUi.Preferred(report, 110);
            var reportHead = BroadcastUi.Label(report.transform, "Head", english ? "WEEKLY REPORT" : "RELATÓRIO DA SEMANA", 14, UiTheme.Score, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(reportHead.rectTransform, 0.04f, 0.68f, 0.96f, 0.94f);
            var reportBody = BroadcastUi.Label(report.transform, "Body", WeekReport(), 16, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.Place(reportBody.rectTransform, 0.04f, 0.06f, 0.96f, 0.68f);

            DateTime? week = null;
            string dayKey = null;
            foreach (var note in _career.Board)
            {
                var when = note.When == default ? _career.Calendar.CurrentDate : note.When;
                var start = when.Date.AddDays(-(((int)when.DayOfWeek + 6) % 7));
                if (week == null || week.Value != start)
                {
                    week = start;
                    dayKey = null;
                    var weekLabel = BroadcastUi.Label(_boardContent, "Week", (english ? "WEEK OF " : "SEMANA DE ") + LocalizationManager.FormatDate(start, "d MMMM").ToUpperInvariant(), 15, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                    BroadcastUi.Preferred(weekLabel, 32);
                }

                var key = when.ToString("yyyy-MM-dd");
                if (dayKey != key)
                {
                    dayKey = key;
                    var dayLabel = BroadcastUi.Label(_boardContent, "DayHead", LocalizationManager.FormatDate(when, "dddd, d MMMM"), 14, UiTheme.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
                    BroadcastUi.Preferred(dayLabel, 26);
                }

                var card = BroadcastUi.Panel(_boardContent, "Note", new Color(0.14f, 0.06f, 0.28f, 0.92f));
                BroadcastUi.Preferred(card, 84);
                var hour = BroadcastUi.Label(card.transform, "Hour", note.Hour, 22, UiTheme.Score, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(hour.rectTransform, 0.03f, 0.2f, 0.18f, 0.8f);
                var noteText = BroadcastUi.Label(card.transform, "Text", note.Text, 16, Color.white, TextAnchor.MiddleLeft);
                BroadcastUi.Place(noteText.rectTransform, 0.2f, 0.1f, 0.97f, 0.9f);
            }

            AddCareerRecord();
        }

        void AddCareerRecord()
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var panel = BroadcastUi.Panel(_boardContent, "CareerRecord", new Color(0.07f, 0.16f, 0.2f, 0.94f));
            BroadcastUi.Preferred(panel, 116);
            var heading = BroadcastUi.Label(panel.transform, "Heading", english ? "CAREER RECORD" : "REGISTO DA CARREIRA", 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.025f, 0.72f, 0.975f, 0.96f);
            var played = 0;
            var wins = 0;
            var draws = 0;
            var losses = 0;
            foreach (var fixture in _career.Fixtures)
            {
                if (!fixture.Played || !fixture.Involves(_career.Club.Id)) continue;
                played++;
                var forGoals = fixture.IsHome(_career.Club.Id) ? fixture.HomeGoals : fixture.AwayGoals;
                var againstGoals = fixture.IsHome(_career.Club.Id) ? fixture.AwayGoals : fixture.HomeGoals;
                if (forGoals > againstGoals) wins++;
                else if (forGoals == againstGoals) draws++;
                else losses++;
            }
            var detail = english
                ? "Seasons managed  " + _career.CompletedSeasons + "     Current-club record  " + wins + "–" + draws + "–" + losses + "  (" + played + " matches)     Supporter approval  " + _career.SupporterApproval + "%     Career moments  " + _career.Memories.Count
                : "Épocas como treinador  " + _career.CompletedSeasons + "     Registo no clube atual  " + wins + "–" + draws + "–" + losses + "  (" + played + " jogos)     Aprovação dos adeptos  " + _career.SupporterApproval + "%     Momentos  " + _career.Memories.Count;
            var body = BroadcastUi.Label(panel.transform, "Body", detail, 14, Color.white, TextAnchor.MiddleLeft);
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
            BroadcastUi.Preferred(overview, 310);

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

            var ringObject = new GameObject("BudgetRing", typeof(RectTransform), typeof(Image));
            var ringRect = ringObject.GetComponent<RectTransform>();
            ringRect.SetParent(overview.transform, false);
            ringRect.anchorMin = ringRect.anchorMax = new Vector2(0.5f, 0.55f);
            ringRect.sizeDelta = new Vector2(270f, 270f);
            var ring = ringObject.GetComponent<Image>();
            ring.sprite = BroadcastUi.Ring;
            ring.preserveAspect = true;
            ring.color = current > 0 ? UiTheme.Fitness : UiTheme.Danger;
            ring.raycastTarget = true;
            BroadcastUi.Hit(ring, ShowFinanceDetails);
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
            var weeklyWages = 0;
            foreach (var player in Squad(_career.Club)) weeklyWages += Mathf.Max(0, player.WeeklyWage);
            var seasonYear = _career.Calendar.CurrentDate.Month >= 7 ? _career.Calendar.CurrentDate.Year : _career.Calendar.CurrentDate.Year - 1;
            var elapsedWeeks = Mathf.Clamp((int)((_career.Calendar.CurrentDate.Date - new DateTime(seasonYear, 7, 1)).TotalDays / 7) + 1, 1, 52);
            var wageEstimate = weeklyWages * elapsedWeeks;
            var sales = finance == null ? 0 : finance.PlayerSalesThisSeason;
            var transferSpend = finance == null ? 0 : finance.TransferSpentThisSeason;
            var netEstimate = ticketIncome + shirtIncome + sales - wageEstimate - transferSpend;
            return new[] { ticketIncome, shirtIncome, wageEstimate, netEstimate };
        }

        void ShowFinanceDetails()
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var finance = _career.Club.Finance;
            if (finance == null) return;
            var estimate = FinanceEstimates();
            OpenModal(english ? "FINANCE BREAKDOWN" : "DETALHE FINANCEIRO",
                english ? "Current season figures for " + _career.Club.Name + "." : "Valores da época atual para " + _career.Club.Name + ".", false);
            AddHead(english ? "BUDGET" : "ORÇAMENTO");
            AddLine((english ? "Total allocation  " : "Orçamento total  ") + Euro((int)Math.Min(int.MaxValue, Math.Max(0L, (long)finance.TransferBudget + finance.TransferSpentThisSeason)))
                + "\n" + (english ? "Available  " : "Disponível  ") + Euro(finance.TransferBudget)
                + "\n" + (english ? "Spent on transfers  " : "Gasto em transferências  ") + Euro(finance.TransferSpentThisSeason));
            AddHead(english ? "INCOME AND COSTS" : "RECEITAS E DESPESAS");
            var playerSales = finance.PlayerSalesThisSeason > 0
                ? Euro(finance.PlayerSalesThisSeason)
                : (english ? "Not tracked" : "Não registadas");
            AddLine((english ? "Ticket income  " : "Bilheteira  ") + Euro(estimate[0])
                + "\n" + (english ? "Shirt sales  " : "Camisolas  ") + Euro(estimate[1])
                + "\n" + (english ? "Player sales  " : "Vendas de jogadores  ") + playerSales
                + "\n" + (english ? "Wages (estimate)  " : "Salários (estimativa)  ") + Euro(estimate[2])
                + "\n" + (english ? "Transfer spending  " : "Gasto em transferências  ") + Euro(finance.TransferSpentThisSeason)
                + "\n" + (english ? "Operating result (wages estimated)  " : "Resultado operacional (salários estimados)  ") + Euro(estimate[3]));
            AddHead(english ? "CLUB ACCOUNTS" : "CONTAS DO CLUBE");
            AddLine((english ? "Club balance  " : "Saldo do clube  ") + Euro(finance.Balance)
                + "\n" + (english ? "Wage budget  " : "Orçamento salarial  ") + Euro(finance.WageBudget));
            AddLine(english
                ? "The board handles wages. Payroll above its weekly allowance reduces the transfer allocation for the rest of the season."
                : "A direção gere os salários. Se a folha salarial ultrapassar o limite semanal, o orçamento de transferências diminui até ao fim da época.");
        }

        void FillAcademy()
        {
            if (_academyContent == null || _career == null) return;
            var existingPlayers = _career.AcademyPlayerIds.Count;
            var existingFixtures = _career.AcademyFixtures.Count;
            AcademyFactory.Ensure(_career, _career.Club.Id);
            AcademyFactory.EnsureSchedule(_career, _career.Club.Id);
            if (existingPlayers != _career.AcademyPlayerIds.Count || existingFixtures != _career.AcademyFixtures.Count)
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
            var viewNames = english ? new[] { "STARTING XI", "BENCH", "LINE-UP", "FIXTURES" } : new[] { "ONZE INICIAL", "BANCO", "ONZE NO CAMPO", "JOGOS" };
            var heading = BroadcastUi.Label(_academyContent, "GroupHeading", group + "  ·  " + viewNames[Mathf.Clamp(_academyViewIndex, 0, viewNames.Length - 1)], 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Preferred(heading, 30);
            if (_academyViewIndex == 3)
            {
                FillAcademyFixtures(group);
                return;
            }

            var prospects = new List<Player>();
            foreach (var id in _career.AcademyPlayerIds)
                if (_career.World.Players.TryGetValue(id, out var player) && player.ClubId == _career.Club.Id && AcademyFactory.GroupForAge(player.Age) == group)
                    prospects.Add(player);
            prospects.Sort((a, b) => PlayerAttributes.Overall(b).CompareTo(PlayerAttributes.Overall(a)));
            if (prospects.Count == 0)
            {
                var empty = BroadcastUi.Label(_academyContent, "Empty", english ? "No youth players are currently in the academy." : "Não há jovens jogadores na academia neste momento.", 16, UiTheme.Muted, TextAnchor.MiddleCenter);
                BroadcastUi.Preferred(empty, 60);
                return;
            }

            var starters = new List<Player>();
            var bench = new List<Player>();
            StartingShape.Split(prospects, _career.Club.Tactics.Formation, starters, bench);
            if (_academyViewIndex == 2)
            {
                FillAcademyLineup(prospects);
                return;
            }

            var shown = _academyViewIndex == 0 ? starters : bench;
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
                var position = PresentationLabels.Position(player.Position);
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

        void FillAcademyLineup(List<Player> prospects)
        {
            if (_academyContent == null || prospects == null || prospects.Count == 0)
                return;
            var fieldPanel = BroadcastUi.Panel(_academyContent, "AcademyLineupPanel", new Color(0.06f, 0.25f, 0.14f, 1f));
            BroadcastUi.Preferred(fieldPanel, 470f);
            var field = BroadcastUi.Zone(fieldPanel.transform, "AcademyLineupPitch", 0.39f, 0.015f, 0.61f, 0.985f);
            BroadcastUi.DrawVerticalHalfPitch(field, new Color(0.12f, 0.46f, 0.22f));
            PlaceAcademyShape(field, prospects, _career.Club);
        }

        void FillAcademyFixtures(string group)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var fixtures = new List<AcademyFixture>();
            foreach (var fixture in _career.AcademyFixtures)
                if (fixture.ClubId == _career.Club.Id && fixture.AgeGroup == group)
                    fixtures.Add(fixture);
            fixtures.Sort((a, b) => a.Date.CompareTo(b.Date));
            if (fixtures.Count == 0)
            {
                var empty = BroadcastUi.Label(_academyContent, "NoFixtures", english ? "No youth fixtures have been scheduled yet." : "Ainda não há jogos da formação marcados.", 15, UiTheme.Muted, TextAnchor.MiddleCenter);
                BroadcastUi.Preferred(empty, 60);
                return;
            }

            for (var i = 0; i < fixtures.Count; i++)
            {
                var fixture = fixtures[i];
                var row = BroadcastUi.Panel(_academyContent, "YouthFixture" + i, new Color(0.12f, 0.05f, 0.24f, 0.92f));
                BroadcastUi.Preferred(row, 58);
                var date = BroadcastUi.Label(row.transform, "Date", LocalizationManager.FormatDate(fixture.Date, "dd MMM yyyy"), 13, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Place(date.rectTransform, 0.025f, 0.12f, 0.19f, 0.88f);
                var home = _career.World.Clubs[fixture.HomeClubId];
                var away = _career.World.Clubs[fixture.AwayClubId];
                var homeButton = BroadcastUi.Button(row.transform, "Home", home.ShortName, () => OpenAcademyTeam(home, group), new Color(1f, 1f, 1f, 0.04f), Color.white, 13);
                BroadcastUi.Place(homeButton.GetComponent<RectTransform>(), 0.2f, 0.12f, 0.43f, 0.88f);
                var score = fixture.Played ? fixture.HomeGoals + "  –  " + fixture.AwayGoals : (english ? "v" : "vs");
                var result = BroadcastUi.Label(row.transform, "Score", score, 15, fixture.Played ? UiTheme.Score : UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(result.rectTransform, 0.44f, 0.12f, 0.56f, 0.88f);
                var awayButton = BroadcastUi.Button(row.transform, "Away", away.ShortName, () => OpenAcademyTeam(away, group), new Color(1f, 1f, 1f, 0.04f), Color.white, 13);
                BroadcastUi.Place(awayButton.GetComponent<RectTransform>(), 0.57f, 0.12f, 0.8f, 0.88f);
                var state = BroadcastUi.Label(row.transform, "State", fixture.Played ? (english ? "PLAYED" : "JOGADO") : (english ? "UPCOMING" : "POR JOGAR"), 11, UiTheme.Muted, TextAnchor.MiddleRight);
                BroadcastUi.Place(state.rectTransform, 0.81f, 0.12f, 0.98f, 0.88f);
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

            var squad = new List<Player>();
            foreach (var id in _career.AcademyPlayerIds)
                if (_career.World.Players.TryGetValue(id, out var player)
                    && player.ClubId == club.Id
                    && AcademyFactory.GroupForAge(player.Age) == group)
                    squad.Add(player);
            squad.Sort((a, b) => PlayerAttributes.Overall(b).CompareTo(PlayerAttributes.Overall(a)));

            var english = LocalizationManager.Current == GameLanguage.English;
            var leagueName = "";
            foreach (var league in _career.World.Leagues)
                if (league.Id == club.LeagueId) { leagueName = LocalizationManager.Translate(league.Name); break; }
            OpenModal(club.Name + "  ·  " + group,
                (string.IsNullOrEmpty(leagueName) ? "" : leagueName + "  ·  ") + (english ? "Youth squad" : "Plantel da formação"), false);
            AddHead((english ? "STARTING XI  ·  " : "ONZE INICIAL  ·  ") + club.Tactics.Formation);
            var pitchPanel = BroadcastUi.Panel(_modalBody, "YouthPitchPanel", new Color(0.06f, 0.25f, 0.14f, 1f));
            BroadcastUi.Preferred(pitchPanel, 360);
            var pitch = BroadcastUi.Zone(pitchPanel.transform, "YouthHalfPitch", 0.31f, 0.02f, 0.69f, 0.98f);
            BroadcastUi.DrawVerticalHalfPitch(pitch, new Color(0.12f, 0.46f, 0.22f));
            PlaceAcademyShape(pitch, squad, club);

            AddHead((english ? "SQUAD  ·  " : "PLANTEL  ·  ") + squad.Count);
            for (var i = 0; i < squad.Count; i++)
            {
                var player = squad[i];
                var row = BroadcastUi.Panel(_modalBody, "YouthPlayer" + i, new Color(0.035f, 0.075f, 0.1f, 0.96f), true);
                BroadcastUi.Preferred(row, 32);
                var name = BroadcastUi.Label(row.transform, "Name", player.SquadNumber + "  ·  " + player.FullName, 12, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(name.rectTransform, 0.025f, 0.08f, 0.48f, 0.92f);
                var position = BroadcastUi.Label(row.transform, "Position", PresentationLabels.Position(player.Position), 11, UiTheme.Accent, TextAnchor.MiddleCenter);
                BroadcastUi.Place(position.rectTransform, 0.49f, 0.08f, 0.66f, 0.92f);
                var age = BroadcastUi.Label(row.transform, "Age", (english ? "Age " : "Idade ") + player.Age, 11, UiTheme.Muted, TextAnchor.MiddleCenter);
                BroadcastUi.Place(age.rectTransform, 0.67f, 0.08f, 0.82f, 0.92f);
                var overall = BroadcastUi.Label(row.transform, "Overall", "OVR  " + PlayerAttributes.Overall(player), 11, UiTheme.Score, TextAnchor.MiddleRight, FontStyle.Bold);
                BroadcastUi.Place(overall.rectTransform, 0.82f, 0.08f, 0.975f, 0.92f);
            }
        }

        static void PlaceAcademyShape(RectTransform host, List<Player> squad, Club club)
        {
            if (host == null || squad == null || club == null)
                return;
            var eleven = new List<Player>();
            var bench = new List<Player>();
            StartingShape.Split(squad, club.Tactics.Formation, eleven, bench);
            for (var index = 0; index < eleven.Count && index < 11; index++)
            {
                var player = eleven[index];
                var card = BroadcastUi.Panel(host, "YouthSlot" + player.Id, new Color(0.1f, 0.04f, 0.18f, 0.94f));
                card.rectTransform.anchorMin = card.rectTransform.anchorMax = MatchEngine.Slot(true, index, club.Tactics.Formation);
                card.rectTransform.sizeDelta = new Vector2(66f, 72f);
                var portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                portraitObject.transform.SetParent(card.transform, false);
                BroadcastUi.Place(portraitObject.GetComponent<RectTransform>(), 0.24f, 0.45f, 0.76f, 0.96f);
                var portrait = portraitObject.GetComponent<Image>();
                portrait.sprite = FaceBook.For(player);
                portrait.preserveAspect = true;
                portrait.raycastTarget = false;
                var name = BroadcastUi.Label(card.transform, "Name", BroadcastUi.ShortName(player.FullName).ToUpperInvariant(), 8, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(name.rectTransform, 0.02f, 0.22f, 0.98f, 0.46f);
                var number = BroadcastUi.Label(card.transform, "Number", player.SquadNumber.ToString(), 9, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(number.rectTransform, 0.04f, 0.78f, 0.3f, 0.98f);
                var overall = BroadcastUi.Label(card.transform, "Overall", PlayerAttributes.Overall(player).ToString(), 9, UiTheme.Score, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(overall.rectTransform, 0.7f, 0.78f, 0.96f, 0.98f);
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
            if (player == null || player.Age < 16 || _career == null || _career.Club == null)
                return;
            if (!_career.Club.PlayerIds.Contains(player.Id))
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

            if (LocalizationManager.Current == GameLanguage.English)
                return "Since " + LocalizationManager.FormatDate(start, "d 'de' MMMM") + ", " + count + " notes were added to the board. Dressing room: " + RoomWord(_career.Club.DressingRoomTension) + ". Next match: " + EnglishMatchLabel(_career) + ".";
            return "Desde " + LocalizationManager.FormatDate(start, "d 'de' MMMM") + " ficaram " + count + " notas no quadro. Vestiário: " + RoomWord(_career.Club.DressingRoomTension) + ". Próximo jogo: " + NextMatchText(_career) + ".";
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

            const float cardWidth = 78f;
            const float cardHeight = 58f;
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
            for (var i = 0; i < _paneTabs.Length; i++)
            {
                var on = i == _pane;
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
            var hurt = new List<string>();
            var tired = new List<string>();
            var low = new List<string>();
            foreach (var player in Squad(_career.Club))
            {
                if (player.Injury != null && player.Injury.IsInjured)
                    hurt.Add(BroadcastUi.ShortName(player.FullName));
                if (player.Fatigue >= 70)
                    tired.Add(BroadcastUi.ShortName(player.FullName));
                if (player.Morale < 40)
                    low.Add(player.FullName);
            }

            var captain = "sem capitão";
            foreach (var player in Squad(_career.Club))
            {
                if (player.IsCaptain)
                    captain = player.FullName;
            }

            if (LocalizationManager.Current == GameLanguage.English)
            {
                var textEnglish = "Captain: " + (captain == "sem capitão" ? "none" : captain) + ".  Next match: " + EnglishMatchLabel(_career) + ".  Dressing room: " + RoomWord(_career.Club.DressingRoomTension) + ".";
                textEnglish += "\nInjured: " + (hurt.Count == 0 ? "none" : string.Join(", ", hurt));
                textEnglish += "\nFatigued: " + (tired.Count == 0 ? "no one above the limit" : string.Join(", ", tired));
                textEnglish += "\nLow morale: " + (low.Count == 0 ? "the squad is holding up" : string.Join(", ", low));
                return textEnglish;
            }

            var text = "Capitão: " + captain + ".  Próximo jogo: " + NextMatchText(_career) + ".  Vestiário: " + RoomWord(_career.Club.DressingRoomTension) + ".";
            text += "\nLesionados: " + (hurt.Count == 0 ? "nenhum" : string.Join(", ", hurt));
            text += "\nCansados: " + (tired.Count == 0 ? "ninguém acima do limite" : string.Join(", ", tired));
            text += "\nMoral em baixo: " + (low.Count == 0 ? "o grupo aguenta" : string.Join(", ", low));
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
            var lines = StartingShape.Lines(club.Tactics.Formation);
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
            for (var index = 0; index < eleven.Count && index < 11; index++)
            {
                var card = PersonCard(host, eleven[index], club, fatigue, index, false, width, height);
                card.anchorMin = card.anchorMax = MatchEngine.Slot(true, index, club.Tactics.Formation);
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
            var width = Mathf.Clamp(field.x * 0.115f, 60f, 76f);
            var height = Mathf.Clamp(field.y * 0.165f, 48f, 58f);
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
                var read = BroadcastUi.Label(card.transform, "Read", LocalizationManager.Translate(StaffReport.Quality(member)) + "  ·  " + LocalizationManager.Translate(StaffReport.Confidence(member)), 12, UiTheme.Muted, TextAnchor.MiddleRight);
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
            var hint = BroadcastUi.Label(root, "ControlHint", LocalizationManager.Translate("M · DEFINIÇÕES"), 15, UiTheme.Muted, TextAnchor.MiddleRight);
            BroadcastUi.Place(hint.rectTransform, 0.42f, 0.008f, 0.98f, 0.048f);
            var sample = Squad(_career.Club);
            if (sample.Count > 0)
            {
                var shell = PersonCard(root, sample[0], _career.Club, true, -1, false, 110f, 148f);
                shell.gameObject.name = "PlayerCardTemplate";
                shell.gameObject.SetActive(false);
                var drag = shell.GetComponent<SquadDragCard>();
                if (drag != null)
                    DestroyImmediate(drag);
                var button = shell.GetComponent<Button>();
                if (button != null)
                    DestroyImmediate(button);
            }
        }

        void BindDesign(Transform root)
        {
            _cardTemplate = root.Find("PlayerCardTemplate");
            var clubName = SceneBind.Get<Text>(root, "Header/ClubName");
            if (clubName != null)
                clubName.text = _career.Club.Name.ToUpperInvariant();
            var crest = SceneBind.Get<Image>(root, "Header/Crest");
            if (crest != null)
                crest.sprite = CrestStudio.For(_career.Club);
            LeagueBackdrop.Paint(root, HubLeagueId());

            _dateLine = SceneBind.Get<Text>(root, "Header/Date");
            _clock = SceneBind.Get<Text>(root, "ClockCard/Clock");
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

            _homeTitle = SceneBind.Get<Text>(root, "Pages/Home/Title");
            _homeFace = SceneBind.Get<Image>(root, "Pages/Home/EventFace");
            _homeBody = SceneBind.Get<Text>(root, "Pages/Home/Scroll/Content/Body");
            _nextChip = SceneBind.Get<Text>(root, "Pages/Home/Next");
            _replies = new Button[3];
            for (var i = 0; i < _replies.Length; i++)
            {
                var choice = i;
                _replies[i] = SceneBind.Wire(root.Find("Pages/Home/Reply" + i), () => Answer(choice));
            }

            _newsContent = root.Find("Pages/News/Scroll/Content") as RectTransform;
            EnsureMemoriesButton(root.Find("Pages/News"));

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
            _paneTabs = new Button[4];
            for (var i = 0; i < _paneTabs.Length; i++)
            {
                var pane = i;
                _paneTabs[i] = SceneBind.Wire(team.Find("Pane" + i), () => SelectPane(pane));
                if (_paneTabs[i] != null)
                {
                    var y1 = 0.93f - i * 0.075f;
                    BroadcastUi.Place(_paneTabs[i].GetComponent<RectTransform>(), 0.45f, y1 - 0.068f, 0.98f, y1);
                }
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
            _playLabel = _play.GetComponentInChildren<Text>();
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
                ? new[] { "BUDGET", "MY CAREER", "CALENDAR", "STAFF" }
                : new[] { "ORÇAMENTO", "A MINHA CARREIRA", "CALENDÁRIO", "EQUIPA TÉCNICA" };
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

        void ApplyCard(RectTransform card, Player player, Club club, bool showFatigue, int fieldIndex, bool bench)
        {
            var captured = player;
            BroadcastUi.Hit(card.GetComponent<Image>(), () => OnPerson(captured, bench));
            if (bench || fieldIndex >= 0)
            {
                var drag = card.gameObject.AddComponent<SquadDragCard>();
                drag.Player = player;
                drag.Bench = bench;
                drag.BenchReserves = bench && _reserves;
                drag.FieldIndex = fieldIndex;
                if (bench)
                    drag.OnSwapWithField = SwapStarter;
            }

            SetText(card, "BackNumber", player.SquadNumber.ToString());
            SetText(card, "Name", BroadcastUi.ShortName(player.FullName).ToUpperInvariant());
            SetText(card, "Position", PresentationLabels.Position(CardPosition(player, club, fieldIndex)));
            SetText(card, "Nation", NationCode(player.Nationality));
            var displayPosition = player.Position;
            if (fieldIndex >= 0)
            {
                var slots = StartingShape.SlotPositions(club.Tactics == null ? "4-3-3" : club.Tactics.Formation);
                if (fieldIndex < slots.Length)
                    displayPosition = slots[fieldIndex];
            }
            SetText(card, "Overall", PlayerAttributes.CurrentOverallAtPosition(player, displayPosition).ToString());
            SetText(card, "Extra", Marks(player, club));
            NudgeCardItem(card, "Name", 0.028f, TextAnchor.MiddleCenter);
            NudgeCardItem(card, "Position", 0.018f, TextAnchor.MiddleLeft);
            NudgeCardItem(card, "Nation", 0.018f, TextAnchor.MiddleCenter);
            NudgeCardItem(card, "Overall", 0.018f, TextAnchor.MiddleRight, 0.88f);
            SetNationFlag(card, player.Nationality);
            var face = card.Find("Face");
            if (face != null)
            {
                var faceRect = face as RectTransform;
                if (faceRect != null)
                {
                    var center = (faceRect.anchorMin + faceRect.anchorMax) * 0.5f;
                    var size = (faceRect.anchorMax - faceRect.anchorMin) * 1.1f;
                    center.y += 0.018f;
                    faceRect.anchorMin = center - size * 0.5f;
                    faceRect.anchorMax = center + size * 0.5f;
                }
                var image = face.GetComponent<Image>();
                image.sprite = FaceBook.For(player);
                image.preserveAspect = true;
            }

            var injury = card.Find("Injury");
            var hurt = player.Injury != null && player.Injury.IsInjured;
            if (hurt && injury == null)
            {
                var cross = BroadcastUi.Label(card, "Injury", "+", 22, new Color(0.92f, 0.16f, 0.18f), TextAnchor.MiddleCenter, FontStyle.Bold);
                cross.rectTransform.anchorMin = cross.rectTransform.anchorMax = new Vector2(0.88f, 0.86f);
                cross.rectTransform.sizeDelta = new Vector2(22, 22);
            }
            else if (injury != null)
                injury.gameObject.SetActive(hurt);

            var bar = card.Find("Fatigue");
            if (bar != null)
            {
                bar.gameObject.SetActive(showFatigue);
                if (showFatigue)
                {
                    var coach = StaffReport.Find(_career.Club, StaffRole.FitnessCoach);
                    var fatigueTruth = bench ? Mathf.Min(player.Fatigue, 22) : player.Fatigue;
                    var tired = StaffReport.Read(coach, player.Id, "cansaco", fatigueTruth, 0, 100);
                    var fresh = Mathf.Clamp01(1f - tired / 100f);
                    var fill = bar.Find("Fill");
                    if (fill != null)
                    {
                        var image = fill.GetComponent<Image>();
                        image.rectTransform.anchorMax = new Vector2(1f, Mathf.Max(0.08f, fresh));
                        image.color = fresh >= 0.72f ? UiTheme.Fitness : fresh >= 0.5f ? UiTheme.FitnessLow : UiTheme.FitnessBad;
                    }
                }
            }
        }

        static void SetText(Transform card, string child, string value)
        {
            var target = card.Find(child);
            if (target == null)
                return;
            var text = target.GetComponent<Text>();
            if (text != null)
                text.text = value;
        }

        static void NudgeCardItem(Transform card, string child, float up, TextAnchor alignment, float maxX = 1f)
        {
            var item = card.Find(child);
            var rect = item as RectTransform;
            if (rect != null)
            {
                rect.anchorMin += new Vector2(0f, up);
                rect.anchorMax = new Vector2(Mathf.Min(rect.anchorMax.x, maxX), rect.anchorMax.y + up);
            }

            var text = item == null ? null : item.GetComponent<Text>();
            if (text != null)
                text.alignment = alignment;
        }

        RectTransform PersonCard(Transform parent, Player player, Club club, bool showFatigue, int fieldIndex, bool bench, float cardWidth, float cardHeight)
        {
            if (_cardTemplate != null)
            {
                var copy = Instantiate(_cardTemplate.gameObject, parent);
                copy.SetActive(true);
                copy.name = "P" + player.SquadNumber;
                var rect = copy.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(cardWidth, cardHeight);
                ApplyCard(rect, player, club, showFatigue, fieldIndex, bench);
                return rect;
            }

            var card = BroadcastUi.Panel(parent, "P" + player.SquadNumber, new Color(0.1f, 0.04f, 0.18f, 0.94f), true);
            card.rectTransform.sizeDelta = new Vector2(cardWidth, cardHeight);
            if (card.GetComponent<RectMask2D>() == null)
                card.gameObject.AddComponent<RectMask2D>();
            var captured = player;
            if (bench)
            {
                var drag = card.gameObject.AddComponent<SquadDragCard>();
                drag.Player = player;
                drag.Bench = true;
                drag.BenchReserves = _reserves;
                drag.OnSwapWithField = SwapStarter;
            }

            BroadcastUi.Hit(card, () => OnPerson(captured, bench));
            var shirt = BroadcastUi.Parse(club.JerseyPrimaryHex, UiTheme.Accent);
            var backNumber = BroadcastUi.Label(card.transform, "BackNumber", player.SquadNumber.ToString(), 80, new Color(shirt.r, shirt.g, shirt.b, 0.22f), TextAnchor.MiddleCenter, FontStyle.Bold);
            backNumber.resizeTextForBestFit = true;
            backNumber.resizeTextMinSize = 10;
            backNumber.resizeTextMaxSize = 160;
            backNumber.horizontalOverflow = HorizontalWrapMode.Overflow;
            backNumber.verticalOverflow = VerticalWrapMode.Overflow;
            BroadcastUi.Place(backNumber.rectTransform, 0.02f, 0.02f, 0.98f, 0.98f);
            var face = FaceImage(card.transform, player, 16f);
            face.preserveAspect = true;
            var faceRect = face.rectTransform;
            var faceCenter = new Vector2(0.5f, 0.596f);
            var faceSize = new Vector2(0.792f, 0.528f) * 1.1f;
            faceRect.anchorMin = faceCenter - faceSize * 0.5f;
            faceRect.anchorMax = faceCenter + faceSize * 0.5f;
            faceRect.offsetMin = Vector2.zero;
            faceRect.offsetMax = Vector2.zero;
            faceRect.pivot = new Vector2(0.5f, 0.5f);
            if (!bench)
            {
                var drop = card.gameObject.AddComponent<SquadDragCard>();
                drop.Player = player;
                drop.Bench = false;
                drop.FieldIndex = fieldIndex;
            }
            var extra = BroadcastUi.Label(card.transform, "Extra", Marks(player, club), 11, UiTheme.Accent, TextAnchor.MiddleRight, FontStyle.Bold);
            BroadcastUi.Place(extra.rectTransform, 0.72f, 0.84f, 0.98f, 0.98f);
            if (player.Injury != null && player.Injury.IsInjured)
            {
                var cross = BroadcastUi.Label(card.transform, "Injury", "+", 22, new Color(0.92f, 0.16f, 0.18f), TextAnchor.MiddleCenter, FontStyle.Bold);
                cross.rectTransform.anchorMin = cross.rectTransform.anchorMax = new Vector2(0.88f, 0.86f);
                cross.rectTransform.sizeDelta = new Vector2(22, 22);
            }
            var name = BroadcastUi.Label(card.transform, "Name", BroadcastUi.ShortName(player.FullName).ToUpperInvariant(), 13, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            name.verticalOverflow = VerticalWrapMode.Overflow;
            BroadcastUi.Place(name.rectTransform, 0.04f, 0.215f, 0.96f, 0.375f);
            name.alignment = TextAnchor.MiddleCenter;
            if (showFatigue)
            {
                var coach = StaffReport.Find(_career.Club, StaffRole.FitnessCoach);
                var fatigueTruth = bench ? Mathf.Min(player.Fatigue, 22) : player.Fatigue;
                var tired = StaffReport.Read(coach, player.Id, "cansaco", fatigueTruth, 0, 100);
                var bg = BroadcastUi.Solid(card.transform, "Fatigue", new Color(1f, 1f, 1f, 0.16f));
                bg.sprite = BroadcastUi.Round;
                bg.type = Image.Type.Sliced;
                BroadcastUi.Place(bg.rectTransform, 0.9f, 0.12f, 0.96f, 0.86f);
                var fill = BroadcastUi.Solid(bg.transform, "Fill", UiTheme.Fitness);
                fill.sprite = BroadcastUi.Round;
                fill.type = Image.Type.Sliced;
                var fresh = Mathf.Clamp01(1f - tired / 100f);
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = new Vector2(1f, Mathf.Max(0.08f, fresh));
                fill.rectTransform.offsetMin = Vector2.zero;
                fill.rectTransform.offsetMax = Vector2.zero;
                fill.color = fresh >= 0.72f ? UiTheme.Fitness : fresh >= 0.5f ? UiTheme.FitnessLow : UiTheme.FitnessBad;
            }

            var position = BroadcastUi.Label(card.transform, "Position", PresentationLabels.Position(CardPosition(player, club, fieldIndex)), 12, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            position.horizontalOverflow = HorizontalWrapMode.Overflow;
            position.verticalOverflow = VerticalWrapMode.Overflow;
            BroadcastUi.Place(position.rectTransform, 0.04f, 0.04f, 0.34f, 0.24f);
            var nation = BroadcastUi.Solid(card.transform, "NationFlag", Color.white);
            nation.sprite = FlagStudio.For(player.Nationality);
            nation.preserveAspect = true;
            nation.raycastTarget = false;
            BroadcastUi.Place(nation.rectTransform, 0.34f, 0.04f, 0.66f, 0.24f);
            var overall = BroadcastUi.Label(card.transform, "Overall", PlayerAttributes.CurrentOverallAtPosition(player, CardPosition(player, club, fieldIndex)).ToString(), 13, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
            overall.horizontalOverflow = HorizontalWrapMode.Overflow;
            overall.verticalOverflow = VerticalWrapMode.Overflow;
            BroadcastUi.Place(overall.rectTransform, 0.66f, 0.04f, 0.88f, 0.24f);
            overall.transform.SetAsLastSibling();

            return card.rectTransform;
        }

        static PlayerPosition CardPosition(Player player, Club club, int fieldIndex)
        {
            if (player == null || club == null || fieldIndex < 0)
                return player == null ? PlayerPosition.CentralMidfielder : player.Position;
            var slots = StartingShape.SlotPositions(club.Tactics == null ? "4-3-3" : club.Tactics.Formation);
            return fieldIndex < slots.Length ? slots[fieldIndex] : player.Position;
        }

        static void SetNationFlag(Transform card, string nationality)
        {
            if (card == null)
                return;
            var textNode = card.Find("Nation");
            var source = textNode as RectTransform;
            if (textNode != null)
            {
                var label = textNode.GetComponent<Text>();
                if (label != null)
                    label.enabled = false;
            }

            var existing = card.Find("NationFlag");
            var flag = existing == null ? BroadcastUi.Solid(card, "NationFlag", Color.white) : existing.GetComponent<Image>();
            if (flag == null)
                return;
            flag.sprite = FlagStudio.For(nationality);
            flag.preserveAspect = true;
            flag.raycastTarget = false;
            if (source != null)
            {
                var target = flag.rectTransform;
                target.anchorMin = source.anchorMin;
                target.anchorMax = source.anchorMax;
                target.pivot = source.pivot;
                target.anchoredPosition = source.anchoredPosition;
                target.sizeDelta = source.sizeDelta;
                target.offsetMin = source.offsetMin;
                target.offsetMax = source.offsetMax;
            }
            flag.transform.SetAsLastSibling();
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
            var rows = new List<Fixture>();
            foreach (var fixture in _career.Fixtures)
            {
                if (fixture.Walkover || !fixture.Involves(_career.Coach.ClubId))
                    continue;
                rows.Add(fixture);
            }

            for (var i = 0; i < rows.Count; i++)
            {
                var fixture = rows[i];
                var opp = _career.World.Clubs[fixture.OpponentId(_career.Coach.ClubId)];
                var venue = LocalizationManager.Translate(fixture.IsHome(_career.Coach.ClubId) ? "CASA" : "FORA");
                var score = fixture.Played ? fixture.HomeGoals + " - " + fixture.AwayGoals : "vs";
                var userHome = fixture.IsHome(_career.Coach.ClubId);
                var userGoals = userHome ? fixture.HomeGoals : fixture.AwayGoals;
                var oppGoals = userHome ? fixture.AwayGoals : fixture.HomeGoals;
                var mark = "";
                var markColor = UiTheme.Muted;
                if (fixture.Played && userGoals > oppGoals)
                {
                    mark = LocalizationManager.Current == GameLanguage.English ? "W" : "V";
                    markColor = new Color(0.25f, 0.78f, 0.38f);
                }
                else if (fixture.Played && userGoals < oppGoals)
                {
                    mark = LocalizationManager.Current == GameLanguage.English ? "L" : "D";
                    markColor = new Color(0.86f, 0.22f, 0.22f);
                }
                var card = BroadcastUi.Panel(_calendar, "Fixture" + i, new Color(0.14f, 0.06f, 0.28f, 0.9f), true);
                BroadcastUi.Preferred(card, 104);
                var captured = fixture;
                BroadcastUi.Hit(card, () => OpenFixture(captured));
                var date = BroadcastUi.Label(card.transform, "Date", LocalizationManager.FormatDate(fixture.Date, "dd MMM").ToUpperInvariant(), 18, UiTheme.Score, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(date.rectTransform, 0.02f, 0.52f, 0.14f, 0.92f);
                var place = BroadcastUi.Label(card.transform, "Venue", venue, 13, UiTheme.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(place.rectTransform, 0.02f, 0.16f, 0.14f, 0.48f);
                var badge = CrestImage(card.transform, opp, 52f);
                badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(0.19f, 0.5f);
                badge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                badge.rectTransform.anchoredPosition = Vector2.zero;
                badge.rectTransform.sizeDelta = new Vector2(52, 52);
                var name = BroadcastUi.Label(card.transform, "Name", opp.Name, 20, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(name.rectTransform, 0.24f, 0.52f, 0.78f, 0.9f);
                var competition = SeasonSchedule.Competition(fixture.LeagueId);
                var noteText = string.IsNullOrEmpty(competition) ? AssistantNote(opp) : LocalizationManager.Translate(competition);
                var note = BroadcastUi.Label(card.transform, "Note", noteText, 14, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Place(note.rectTransform, 0.24f, 0.08f, 0.8f, 0.5f);
                var verdict = BroadcastUi.Label(card.transform, "Verdict", mark, 22, markColor, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(verdict.rectTransform, 0.78f, 0.2f, 0.86f, 0.8f);
                var result = BroadcastUi.Label(card.transform, "Score", score, 22, fixture.Played ? UiTheme.Score : Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
                BroadcastUi.Place(result.rectTransform, 0.84f, 0.2f, 0.98f, 0.8f);
            }
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
            var edition = Newsroom.Edition(_career);
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
            if (!string.IsNullOrEmpty(story.Motm))
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
                    () => OpenClubDossier(home), new Color(0.16f, 0.08f, 0.29f, 1f), Color.white, 13);
                BroadcastUi.Preferred(clubButton.GetComponent<RectTransform>(), 38);
            }
            if (away != null)
            {
                var clubButton = BroadcastUi.Button(_modalBody, "AwayClubProfile", LocalizationManager.Current == GameLanguage.English ? "VIEW " + away.ShortName : "VER " + away.ShortName,
                    () => OpenClubDossier(away), new Color(0.16f, 0.08f, 0.29f, 1f), Color.white, 13);
                BroadcastUi.Preferred(clubButton.GetComponent<RectTransform>(), 38);
            }

            var pressBody = PressBody(story);
            var body = BroadcastUi.Label(_modalBody, "Body", pressBody, 17, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.SetWrapped(body, pressBody, 220);
            AddHead(LocalizationManager.Translate("Resto da edição"));
            foreach (var other in edition)
            {
                if (other.Headline == story.Headline)
                    continue;
                var bar = BroadcastUi.Panel(_modalBody, "Bar", new Color(0.02f, 0.01f, 0.04f, 0.92f));
                BroadcastUi.Preferred(bar, 42);
                var line = BroadcastUi.Label(bar.transform, "Hidden", PressHeadline(other), 14, new Color(0.45f, 0.4f, 0.5f), TextAnchor.MiddleLeft);
                BroadcastUi.Place(line.rectTransform, 0.04f, 0.1f, 0.96f, 0.9f);
            }
        }

        void OpenFixture(Fixture fixture)
        {
            var opp = _career.World.Clubs[fixture.OpponentId(_career.Coach.ClubId)];
            var venue = LocalizationManager.Translate(fixture.IsHome(_career.Coach.ClubId) ? "Casa" : "Fora");
            var scouted = _career.Scouted.Contains(opp.Id);
            OpenModal(opp.Name, venue + "  ·  " + LocalizationManager.FormatDate(fixture.Date, "dd MMMM") + "  ·  " + opp.Tactics.Formation, scouted == false);
            var crest = CrestImage(_modal.transform.Find("Card"), opp, 56f);
            crest.rectTransform.anchorMin = crest.rectTransform.anchorMax = new Vector2(0.07f, 0.9f);
            var clubCardButton = BroadcastUi.Button(_modalBody, "ClubProfile", LocalizationManager.Current == GameLanguage.English ? "OPEN CLUB PROFILE" : "ABRIR PERFIL DO CLUBE",
                () => OpenClubDossier(opp), new Color(0.16f, 0.08f, 0.29f, 1f), Color.white, 13);
            BroadcastUi.Preferred(clubCardButton.GetComponent<RectTransform>(), 38);
            AddLine(AssistantNote(opp));
            AddHead(LocalizationManager.Translate("PLANTEL") + "  ·  " + opp.Tactics.Formation);
            var pitch = BroadcastUi.Panel(_modalBody, "Shape", new Color(0.1f, 0.04f, 0.2f, 0.72f));
            BroadcastUi.Preferred(pitch, 520);
            var verticalPitch = BroadcastUi.Zone(pitch.transform, "VerticalHalfPitch", 0.39f, 0.01f, 0.61f, 0.99f);
            BroadcastUi.DrawVerticalHalfPitch(verticalPitch, new Color(0.12f, 0.46f, 0.22f));
            PlaceVerticalShape(verticalPitch, opp, false, 64f, 60f);

            if (scouted)
                return;
            _modalAction.onClick.RemoveAllListeners();
            _modalActionLabel.text = LocalizationManager.Translate("ANALISAR O PLANTEL  ·  3 DIAS");
            _modalAction.onClick.AddListener(() => StartScout(opp, fixture));
        }

        void OpenClubDossier(Club club)
        {
            OpenClubDossier(club, 0);
        }

        void OpenClubDossier(Club club, int selectedTab)
        {
            if (club == null || _career == null) return;
            var english = LocalizationManager.Current == GameLanguage.English;
            var city = LocalizationManager.Translate(club.City);
            var league = "";
            foreach (var item in _career.World.Leagues)
                if (item.Id == club.LeagueId) { league = LocalizationManager.Translate(item.Name); break; }
            var lead = (string.IsNullOrEmpty(league) ? city : league + "  ·  " + city) + "  ·  " + (english ? "Founded " : "Fundado em ") + club.FoundedYear;
            OpenModal(club.Name, lead, false);
            var crest = CrestImage(_modal.transform.Find("Card"), club, 48f);
            crest.rectTransform.anchorMin = crest.rectTransform.anchorMax = new Vector2(0.05f, 0.925f);
            BroadcastUi.Place(_modalTitle.rectTransform, 0.09f, 0.88f, 0.91f, 0.97f);

            var labels = english
                ? new[] { "OVERVIEW", "SQUAD", "MATCHES", "FINANCES", "TRANSFERS", "HISTORY", "FACILITIES" }
                : new[] { "VISÃO GERAL", "PLANTEL", "JOGOS", "FINANÇAS", "TRANSFERÊNCIAS", "HISTÓRIA", "INSTALAÇÕES" };
            var tabRow = BroadcastUi.Panel(_modalBody, "ClubProfileTabs", new Color(0.035f, 0.075f, 0.1f, 0.96f), true);
            BroadcastUi.Preferred(tabRow, 38);
            for (var i = 0; i < labels.Length; i++)
            {
                var tab = i;
                var x0 = i / (float)labels.Length;
                var x1 = (i + 1) / (float)labels.Length;
                var button = BroadcastUi.Button(tabRow.transform, "ClubTab" + i, labels[i],
                    () => OpenClubDossier(club, tab), tab == selectedTab ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.07f),
                    tab == selectedTab ? UiTheme.ButtonText : Color.white, 9);
                BroadcastUi.Place(button.GetComponent<RectTransform>(), x0 + 0.003f, 0.08f, x1 - 0.003f, 0.92f);
            }

            switch (Mathf.Clamp(selectedTab, 0, labels.Length - 1))
            {
                case 0: BuildClubOverview(club, city, league, english); break;
                case 1: BuildClubSquad(club, english); break;
                case 2: BuildClubMatches(club, english); break;
                case 3: BuildClubFinances(club, english); break;
                case 4: BuildClubTransfers(club, english); break;
                case 5: BuildClubHistory(club, city, league, english); break;
                case 6: BuildClubFacilities(club, city, league, english); break;
            }
        }

        void BuildClubOverview(Club club, string city, string league, bool english)
        {
            var roster = Squad(club);
            roster.Sort((a, b) => PlayerAttributes.Overall(b).CompareTo(PlayerAttributes.Overall(a)));
            var overview = BroadcastUi.Panel(_modalBody, "ClubOverviewGrid", new Color(0.035f, 0.075f, 0.1f, 0.96f), false);
            BroadcastUi.Preferred(overview, 460f);
            var summary = (english ? "Reputation  " : "Reputação  ") + ReputationStars(club.Reputation)
                + "   ·   " + (english ? "First-team players  " : "Jogadores seniores  ") + roster.Count
                + (club.Finance == null ? "" : "\n" + (english ? "Transfer budget  " : "Orçamento de transferências  ") + Euro(club.Finance.TransferBudget)
                    + "   ·   " + (english ? "Club balance  " : "Saldo do clube  ") + Euro(club.Finance.Balance));
            OverviewBlock(overview.transform, "Summary", english ? "CLUB OVERVIEW" : "VISÃO GERAL DO CLUBE", summary, 0.025f, 0.75f, 0.64f, 0.97f);

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
                if (fixture.Played && fixture.Involves(club.Id) && fixture.LeagueId == club.LeagueId) recent.Add(fixture);
            recent.Sort((a, b) => b.Date.CompareTo(a.Date));
            for (var i = 0; i < Mathf.Min(2, recent.Count); i++)
            {
                var fixture = recent[i];
                var opponentId = fixture.OpponentId(club.Id);
                var opponent = _career.World.Clubs.TryGetValue(opponentId, out var other) ? other.ShortName : opponentId;
                leagueText += "\n" + LocalizationManager.FormatDate(fixture.Date, "dd MMM") + "  ·  " + opponent + "  ·  "
                    + (fixture.IsHome(club.Id) ? fixture.HomeGoals + "–" + fixture.AwayGoals : fixture.AwayGoals + "–" + fixture.HomeGoals);
            }
            OverviewBlock(overview.transform, "League", english ? "LEAGUE POSITION  ·  RECENT RESULTS" : "POSIÇÃO NA LIGA  ·  RESULTADOS RECENTES", leagueText, 0.025f, 0.48f, 0.64f, 0.72f);

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
            var identity = LocalizationManager.Translate(club.Tactics.Formation + " · " + club.Tactics.Pressing + " · " + club.Tactics.BuildUp
                + " · " + club.Tactics.FullBacks + " · " + club.Tactics.DefensiveLine);
            OverviewBlock(overview.transform, "Identity", english ? "TACTICS" : "TÁTICAS", identity, 0.025f, 0.035f, 0.32f, 0.19f);
            var expectations = club.BoardObjectives == null || club.BoardObjectives.Count == 0
                ? (english ? "The board expects steady progress." : "A direção espera uma evolução consistente.")
                : string.Join("\n", club.BoardObjectives.ConvertAll(item => "· " + LocalizationManager.Translate(item)));
            OverviewBlock(overview.transform, "Expectations", english ? "BOARD EXPECTATIONS" : "EXPECTATIVAS DA DIREÇÃO", expectations, 0.34f, 0.035f, 0.64f, 0.19f);

            var field = BroadcastUi.Panel(overview.transform, "FirstTeamShape", new Color(0.06f, 0.25f, 0.14f, 1f));
            BroadcastUi.Place(field.rectTransform, 0.67f, 0.035f, 0.98f, 0.97f);
            var pitch = BroadcastUi.Zone(field.transform, "VerticalHalfPitch", 0.04f, 0.02f, 0.96f, 0.98f);
            BroadcastUi.DrawVerticalHalfPitch(pitch, new Color(0.12f, 0.46f, 0.22f));
            PlaceVerticalShape(pitch, club, false, 48f, 52f);
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
            AddHead((english ? "FIRST TEAM  ·  " : "PLANTEL PRINCIPAL  ·  ") + roster.Count);
            AddClubPitch(club, 360f, 58f, 62f);
            for (var i = 0; i < roster.Count; i++)
            {
                var player = roster[i];
                var detail = BroadcastUi.Button(_modalBody, "SquadPlayer" + i,
                    player.SquadNumber + "  " + player.FullName + "  ·  " + PresentationLabels.Position(player.Position)
                    + "  ·  " + (english ? "Age " : "Idade ") + player.Age + "  ·  OVR " + PlayerAttributes.Overall(player),
                    () => OpenPlayer(player), new Color(0.1f, 0.04f, 0.2f, 0.88f), Color.white, 13);
                BroadcastUi.Preferred(detail.GetComponent<RectTransform>(), 35);
            }
        }

        void BuildClubMatches(Club club, bool english)
        {
            AddClubRecentMatches(club, english, 12);
            var upcoming = new List<Fixture>();
            foreach (var fixture in _career.Fixtures)
                if (!fixture.Played && fixture.Involves(club.Id) && fixture.LeagueId == club.LeagueId && !fixture.Walkover)
                    upcoming.Add(fixture);
            upcoming.Sort((a, b) => a.Date.CompareTo(b.Date));
            AddHead(english ? "UPCOMING FIXTURES" : "PRÓXIMOS JOGOS");
            for (var i = 0; i < Mathf.Min(10, upcoming.Count); i++)
            {
                var fixture = upcoming[i];
                var opponentId = fixture.OpponentId(club.Id);
                var opponent = _career.World.Clubs.TryGetValue(opponentId, out var other) ? other.ShortName : opponentId;
                AddLine(LocalizationManager.FormatDate(fixture.Date, "dd MMM") + "  ·  " + (fixture.IsHome(club.Id) ? (english ? "Home" : "Casa") : (english ? "Away" : "Fora")) + "  ·  " + opponent);
            }
            if (upcoming.Count == 0)
                AddLine(english ? "No upcoming league matches." : "Sem próximos jogos da liga.");
        }

        void BuildClubFinances(Club club, bool english)
        {
            AddHead(english ? "BUDGET" : "ORÇAMENTO");
            if (club.Finance == null)
            {
                AddLine(english ? "Financial information is unavailable." : "Informação financeira indisponível.");
                return;
            }
            AddLine((english ? "Transfer budget  " : "Orçamento de transferências  ") + Euro(club.Finance.TransferBudget)
                + "\n" + (english ? "Wage budget  " : "Orçamento salarial  ") + Euro(club.Finance.WageBudget)
                + "\n" + (english ? "Club balance  " : "Saldo do clube  ") + Euro(club.Finance.Balance)
                + "\n" + (english ? "Transfer spending  " : "Gasto em transferências  ") + Euro(club.Finance.TransferSpentThisSeason));
            AddHead(english ? "INCOME" : "RECEITAS");
            AddLine((english ? "Ticket income  " : "Bilheteira  ") + Euro(club.Finance.TicketIncomeThisSeason)
                + "\n" + (english ? "Shirt sales  " : "Vendas de camisolas  ") + Euro(club.Finance.ShirtIncomeThisSeason)
                + "\n" + (english ? "Player sales  " : "Vendas de jogadores  ") + Euro(club.Finance.PlayerSalesThisSeason));
            var wages = 0;
            foreach (var player in Squad(club)) wages += Mathf.Max(0, player.WeeklyWage);
            AddHead(english ? "WAGES" : "SALÁRIOS");
            AddLine((english ? "First-team weekly wages  " : "Salários semanais da equipa principal  ") + Euro(wages));
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
                if (++shown >= 24) break;
            }
            if (shown == 0)
                AddLine(english ? "No recorded transfers for this club yet." : "Ainda não há transferências registadas para este clube.");
        }

        void BuildClubHistory(Club club, string city, string league, bool english)
        {
            AddHead(english ? "CLUB HISTORY" : "HISTÓRIA DO CLUBE");
            AddLine(string.IsNullOrEmpty(club.History)
                ? (english ? club.Name + " represent " + city + " and compete in " + league + "." : club.Name + " representa " + city + " e compete em " + league + ".")
                : LocalizationManager.Translate(club.History));
            AddHead(english ? "REPUTATION" : "REPUTAÇÃO");
            AddLine(ReputationStars(club.Reputation) + "  ·  " + LocalizationManager.Translate(club.PublicReputation));
            AddHead(english ? "BOARD OBJECTIVES" : "OBJETIVOS DA DIREÇÃO");
            AddLine(club.BoardObjectives == null || club.BoardObjectives.Count == 0
                ? (english ? "The board expects steady progress." : "A direção espera uma evolução consistente.")
                : string.Join("\n", club.BoardObjectives.ConvertAll(item => "· " + LocalizationManager.Translate(item))));
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
                if (fixture.Played && fixture.Involves(club.Id) && fixture.LeagueId == club.LeagueId)
                    recent.Add(fixture);
            recent.Sort((a, b) => b.Date.CompareTo(a.Date));
            for (var i = 0; i < Mathf.Min(limit, recent.Count); i++)
            {
                var fixture = recent[i];
                var opponentId = fixture.OpponentId(club.Id);
                var opponent = _career.World.Clubs.TryGetValue(opponentId, out var other) ? other.ShortName : opponentId;
                var goalsFor = fixture.IsHome(club.Id) ? fixture.HomeGoals : fixture.AwayGoals;
                var goalsAgainst = fixture.IsHome(club.Id) ? fixture.AwayGoals : fixture.HomeGoals;
                AddLine(LocalizationManager.FormatDate(fixture.Date, "dd MMM") + "  ·  " + opponent + "  ·  " + goalsFor + "–" + goalsAgainst);
            }
            if (recent.Count == 0)
                AddLine(english ? "No league matches have been played yet." : "Ainda não há jogos de liga realizados.");
        }

        void StartScout(Club opponent, Fixture fixture)
        {
            var next = _career.NextUserFixture();
            var free = next == null ? 7 : (next.Date.Date - _career.Calendar.CurrentDate.Date).Days;
            if (free <= 1)
            {
                AddLine("Não há dias livres antes do jogo. A análise fica por fazer.");
                _modalAction.gameObject.SetActive(false);
                return;
            }

            var days = Mathf.Min(3, free - 1);
            AdvanceCareerDays(days);
            _career.Scouted.Add(opponent.Id);
            _career.Board.Insert(0, new CoachNote
            {
                Day = _career.Calendar.WeekdayName,
                Hour = _career.Calendar.ClockText,
                Text = LocalizationManager.Current == GameLanguage.English
                    ? "Scouting report on " + opponent.ShortName + " completed in " + days + " days. The report is in the calendar."
                    : "Análise ao " + opponent.ShortName + " fechada. Levou " + days + " dias. O relatório já está no calendário."
            });
            if (SettingsPrefs.AutosaveEnabled)
                CareerStore.AutoSaveIfDue(_career, SettingsPrefs.AutosaveDays, SettingsPrefs.AutosaveSlots);
            FillBoard();
            Refresh();
            OpenFixture(fixture);
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

        void OpenPlayer(Player player, bool orders = false)
        {
            if (player == null)
                return;
            PlayerPositionProfile.Ensure(player);
            _modalLineSize = 16;
            var keeper = player.Position == PlayerPosition.Goalkeeper;
            var reader = StaffReport.Find(_career.Club, keeper ? StaffRole.GoalkeeperCoach : StaffRole.AssistantCoach);
            var fitness = StaffReport.Find(_career.Club, StaffRole.FitnessCoach);
            OpenModal(player.FullName, "", false);
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
                BroadcastUi.Place(scrollRect as RectTransform, 0.025f, 0.035f, 0.975f, 0.76f);
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
            const float mainBottom = 0.34f;
            const float mainTop = 0.77f;
            var english = LocalizationManager.Current == GameLanguage.English;
            var own = _career.Club.PlayerIds.Contains(player.Id);
            var academyOwned = player.ClubId == _career.Club.Id && _career.AcademyPlayerIds.Contains(player.Id);
            var scouted = own || academyOwned || _career.ScoutedPlayers.Contains(player.Id);
            var rating = PlayerAttributes.CurrentOverallAtPosition(player, player.Position);
            var baseline = PlayerAttributes.OverallAtPosition(player, player.Position);
            var delta = rating - baseline;
            _keyboardScoutBack = null;

            var header = ProfilePanel("Identity", 0.005f, 0.79f, 0.995f, 0.995f);
            var portrait = new GameObject("Portrait", typeof(RectTransform)).GetComponent<RectTransform>();
            portrait.SetParent(header.transform, false);
            BroadcastUi.Place(portrait, 0.012f, 0.06f, 0.105f, 0.94f);
            var face = FaceImage(portrait, player, 120f);
            face.preserveAspect = true;
            BroadcastUi.Stretch(face.rectTransform);

            var flag = BroadcastUi.Solid(header.transform, "CountryFlag", Color.white);
            flag.sprite = FlagStudio.For(player.Nationality);
            flag.preserveAspect = true;
            flag.raycastTarget = false;
            BroadcastUi.Place(flag.rectTransform, 0.12f, 0.62f, 0.15f, 0.88f);
            var positionLabel = ProfileText(header.transform, "Position", "#" + player.SquadNumber + "  ·  " + ProfilePosition(player.Position, english) + (player.IsCaptain ? (english ? "  ·  CAPTAIN" : "  ·  CAPITÃO") : player.IsViceCaptain ? (english ? "  ·  VICE-CAPTAIN" : "  ·  SUBCAPITÃO") : ""), 16, UiTheme.Accent, FontStyle.Bold, 0.16f, 0.62f, 0.42f, 0.9f);
            var clubLabel = ProfileText(header.transform, "Club", (_career.World.Clubs.TryGetValue(player.ClubId, out var club) ? club.Name : (english ? "Unknown club" : "Clube desconhecido")) + "  ·  " + player.Age + (english ? " years" : " anos"), 13, Color.white, FontStyle.Bold, 0.12f, 0.38f, 0.43f, 0.61f);
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
                ProfileText(header.transform, "AltPositions", positionDetails, 12, UiTheme.Muted, FontStyle.Normal, 0.12f, 0.05f, 0.43f, 0.36f);
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
            ProfileText(header.transform, "PhysicalAndInfo", string.Join("\n", info), 11, Color.white, FontStyle.Normal, 0.79f, 0.05f, 0.985f, 0.95f);

            if (!scouted)
            {
                var locked = BroadcastUi.Panel(_modalBody, "ScoutingRequired", new Color(0.015f, 0.035f, 0.05f, 1f));
                BroadcastUi.Place(locked.rectTransform, 0.005f, 0.015f, 0.995f, 0.77f);
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
                    job == null ? (Action)(() => { AskScout(player); OpenPlayer(player); }) : null,
                    UiTheme.Accent, UiTheme.ButtonText, 14);
                reportButton.interactable = job == null;
                BroadcastUi.Place(reportButton.GetComponent<RectTransform>(), ControlChrome.UsingKeyboard ? 0.55f : 0.2f, 0.08f, 0.95f, 0.23f);
                return;
            }

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
                    if (_career.Listed.Contains(captured.Id)) _career.Listed.Remove(captured.Id);
                    else { _career.Listed.Add(captured.Id); _career.Loans.Remove(captured.Id); }
                    OpenPlayer(captured);
                }, 0.03f, 0.55f, 0.49f, 0.96f);
                ProfileButton(actionPanel.transform, "Loan", forLoan ? (english ? "REMOVE LOAN LISTING" : "RETIRAR DO EMPRÉSTIMO") : (english ? "LIST FOR LOAN" : "LISTA DE EMPRÉSTIMOS"), () =>
                {
                    if (_career.Loans.Contains(captured.Id)) _career.Loans.Remove(captured.Id);
                    else { _career.Loans.Add(captured.Id); _career.Listed.Remove(captured.Id); }
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
            if (!english)
                return PresentationLabels.Position(position);
            switch (position)
            {
                case PlayerPosition.Goalkeeper: return "GK";
                case PlayerPosition.RightBack: return "RB";
                case PlayerPosition.CentreBack: return "CB";
                case PlayerPosition.LeftBack: return "LB";
                case PlayerPosition.DefensiveMidfielder: return "DM";
                case PlayerPosition.CentralMidfielder: return "CM";
                case PlayerPosition.AttackingMidfielder: return "AM";
                case PlayerPosition.RightWinger: return "RW";
                case PlayerPosition.LeftWinger: return "LW";
                case PlayerPosition.Striker: return "ST";
                default: return "?";
            }
        }

        void OpenRenewalNegotiation(Player player)
        {
            if (player == null || player.ClubId != _career.Club.Id)
                return;
            var english = LocalizationManager.Current == GameLanguage.English;
            OpenModal(english ? "Contract negotiation" : "Negociação de contrato", player.FullName, true);
            AddLine(english
                ? "Current wage: " + Euro(player.WeeklyWage) + "/week. Choose a wage and term to make an offer."
                : "Salário atual: " + Euro(player.WeeklyWage) + "/semana. Escolhe o salário e a duração para apresentar uma proposta.");
            var raises = new[] { 5, 12, 20 };
            var years = new[] { 2, 3, 4 };
            for (var i = 0; i < raises.Length; i++)
            {
                var increase = raises[i];
                var term = years[i];
                var wage = Mathf.Max(250, Mathf.RoundToInt(player.WeeklyWage * (1f + increase / 100f)));
                var button = BroadcastUi.Button(_modalBody, "Offer" + i,
                    english ? "+" + increase + "%  ·  " + Euro(wage) + "/week  ·  " + term + " years"
                        : "+" + increase + "%  ·  " + Euro(wage) + "/semana  ·  " + term + " anos",
                    () => ResolveRenewalOffer(player, wage, term, increase), new Color(1f, 1f, 1f, 0.08f), Color.white, 14);
                BroadcastUi.Preferred(button, 48f);
            }
            _modalActionLabel.text = english ? "CLOSE" : "FECHAR";
            _modalAction.onClick.RemoveAllListeners();
            _modalAction.onClick.AddListener(CloseModal);
        }

        void ResolveRenewalOffer(Player player, int wage, int years, int increase)
        {
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
                var transferBudgetChange = GlobalTransferMarket.AdjustTransferBudgetForWage(_career, _career.Club, player.Id, wage);
                player.WeeklyWage = wage;
                CareerStore.Save(_career);
                OpenModal(english ? "Deal agreed" : "Acordo alcançado", player.FullName, true);
                AddLine(english
                    ? "The player accepted " + Euro(wage) + "/week for " + years + " years."
                    : "O jogador aceitou " + Euro(wage) + "/semana por " + years + " anos.");
                if (transferBudgetChange != 0)
                    AddLine(english
                        ? "The board adjusted the transfer budget by " + (transferBudgetChange > 0 ? "+" : "−") + Euro(Mathf.Abs(transferBudgetChange)) + " to account for the wage allowance."
                        : "A direção ajustou o orçamento de transferências em " + (transferBudgetChange > 0 ? "+" : "−") + Euro(Mathf.Abs(transferBudgetChange)) + " devido ao limite salarial.");
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
                ProfileText(row, "Name", LocalizationManager.Translate(stats[i]), 9, Color.white, FontStyle.Normal, 0f, 0.52f, 0.6f, 1f);
                ProfileText(row, "Number", value.ToString(), 10, UiTheme.Score, FontStyle.Bold, 0.6f, 0.52f, 0.78f, 1f, TextAnchor.MiddleRight);
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
            _offer = null;
            _modalLineSize = 20;
            OpenModal(member.FullName, PresentationLabels.StaffRole(member.Role), true);
            var portrait = BroadcastUi.Panel(_modalBody, "StaffPortrait", new Color(0.035f, 0.075f, 0.1f, 0.96f));
            BroadcastUi.Preferred(portrait, 96f);
            var portraitImage = BroadcastUi.Solid(portrait.transform, "Face", Color.white);
            BroadcastUi.Place(portraitImage.rectTransform, 0.43f, 0.03f, 0.57f, 0.97f);
            portraitImage.sprite = FaceBook.For(member);
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;
            AddLine(LocalizationManager.Translate(StaffReport.Quality(member)) + " " + LocalizationManager.Translate(StaffReport.Confidence(member)));
            AddLine(LocalizationManager.Current == GameLanguage.English
                ? "Requesting a replacement does not dismiss anyone. Only the board can end a contract."
                : "Pedir a saída não despede ninguém. Quem termina o contrato é a direção.");
            AddLine(VibeLine());
            _modalAction.onClick.RemoveAllListeners();
            _modalActionLabel.text = LocalizationManager.Translate("REUNIÃO COM O PRESIDENTE");
            var captured = member;
            _modalAction.onClick.AddListener(() => OpenMeeting(captured));
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
            days = Mathf.Max(1, days);
            AdvancePlayerSystems(days);
            _career.Calendar.AdvanceDays(days);
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
            foreach (var player in Squad(_career.Club))
            {
                if (player.Injury != null && player.Injury.IsInjured)
                    continue;
                if (string.IsNullOrEmpty(player.TrainingFocus) || player.TrainingFocus == "rest") continue;
                player.TrainingProgress += days * (player.Age <= 24 ? 2 : 1);
                while (player.TrainingProgress >= 100)
                {
                    player.TrainingProgress -= 100;
                    var attributes = player.Attributes;
                    if (attributes == null) break;
                    switch (player.TrainingFocus)
                    {
                        case "finishing": attributes.Finishing = Mathf.Min(20, attributes.Finishing + 1); break;
                        case "passing": attributes.Passing = Mathf.Min(20, attributes.Passing + 1); break;
                        case "pace": attributes.Pace = Mathf.Min(20, attributes.Pace + 1); break;
                        case "defending": attributes.Tackling = Mathf.Min(20, attributes.Tackling + 1); break;
                        case "strength": attributes.Strength = Mathf.Min(20, attributes.Strength + 1); break;
                        case "positioning": attributes.Positioning = Mathf.Min(20, attributes.Positioning + 1); break;
                        case "fitness": attributes.Stamina = Mathf.Min(20, attributes.Stamina + 1); break;
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
            var card = _modal.transform.Find("Card");
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
            var tone = stats.ClubId == _career.Club.Id
                ? new Color(0.18f, 0.12f, 0.3f, 1f)
                : new Color(0.1f, 0.04f, 0.2f, 0.94f);
            var row = BroadcastUi.Panel(_modalBody, "Standing" + place + stats.ClubId, tone, true);
            BroadcastUi.Preferred(row, 44);
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

            var name = BroadcastUi.Label(row.transform, "Club", club == null ? stats.ClubId : club.ShortName, 14, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
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
                var club = _career.World.Clubs.ContainsKey(row.ClubId) ? _career.World.Clubs[row.ClubId].ShortName : row.ClubId;
                var value = LocalizationManager.Current == GameLanguage.English
                    ? kind == "gm" ? row.GoalsFor + " goals" : kind == "gs" ? row.GoalsAgainst + " conceded" : kind == "v" ? row.Won + " wins" : (row.GoalDifference > 0 ? "+" : "") + row.GoalDifference
                    : kind == "gm" ? row.GoalsFor + " golos" : kind == "gs" ? row.GoalsAgainst + " sofridos" : kind == "v" ? row.Won + " vitórias" : (row.GoalDifference > 0 ? "+" : "") + row.GoalDifference;
                AddRankRow(i + 1, club, "", value);
            }
        }

        void AddRankRow(int place, string name, string club, string value)
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
        }

        void AddBody(string text)
        {
            var label = BroadcastUi.Label(_modalBody, "Body", text, 15, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.Preferred(label, 88);
        }

        void OpenModal(string title, string lead, bool action)
        {
            _profileModalPlayer = null;
            _profileModalOrders = false;
            if (_modalTitle != null) _modalTitle.gameObject.SetActive(true);
            if (_modalLead != null) _modalLead.gameObject.SetActive(true);
            if (_modalLineSize <= 0)
                _modalLineSize = 16;
            Clear(_modalBody);
            var modalScroll = _modal.transform.Find("Card/Scroll");
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
                BroadcastUi.Place(card as RectTransform, 0.04f, 0.03f, 0.96f, 0.97f);
                for (var i = card.childCount - 1; i >= 0; i--)
                {
                    var child = card.GetChild(i);
                    if (child.name == "Title" || child.name == "Lead" || child.name == "Scroll" || child.name == "Close" || child.name == "ModalClose" || child.name == "Action")
                        continue;
                    Destroy(child.gameObject);
                }
            }
            BroadcastUi.Place(_modalTitle.rectTransform, 0.08f, 0.88f, 0.91f, 0.97f);
            BroadcastUi.Place(_modalLead.rectTransform, 0.05f, 0.78f, 0.95f, 0.88f);
            var scroll = _modal.transform.Find("Card/Scroll") as RectTransform;
            if (scroll != null)
                BroadcastUi.Place(scroll, 0.04f, 0.14f, 0.96f, 0.76f);
            _modalTitle.text = title;
            _modalLead.text = lead;
            _modalAction.gameObject.SetActive(action);
            _modalAction.onClick.RemoveAllListeners();
            if (_modalClose != null)
            {
                _modalClose.gameObject.SetActive(true);
                BroadcastUi.Place(_modalClose.GetComponent<RectTransform>(), 0.935f, 0.895f, 0.98f, 0.97f);
            }
            _modal.SetActive(true);
            _modal.transform.SetAsLastSibling();
        }

        void CloseModal()
        {
            _offer = null;
            _profileModalPlayer = null;
            _profileModalOrders = false;
            if (_modal != null)
                _modal.SetActive(false);
            if (_modalClose != null)
                _modalClose.gameObject.SetActive(false);
        }

        void AddHead(string title)
        {
            var label = BroadcastUi.Label(_modalBody, "Head", title.ToUpperInvariant(), 15, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Preferred(label, 28);
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
            return (next.IsHome(career.Coach.ClubId) ? "Home" : "Away") + " vs " + opponent.Name;
        }

        static string MatchLabel(CareerState career, Fixture fixture)
        {
            var opp = career.World.Clubs[fixture.OpponentId(career.Coach.ClubId)];
            var venue = fixture.IsHome(career.Coach.ClubId) ? "Casa" : "Fora";
            return venue + " vs " + opp.Name;
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
    }
}
