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
    public sealed class CareerHubController : MonoBehaviour
    {
        static readonly string[] SocialAccountNames =
        {
            "The Away End", "Matchday Mic", "Touchline Wire", "The Football Desk",
            "The Golden Ball Desk", "Transfer Ledger", "Tactical Review", "Saturday Scout",
            "The League Room", "Home End Noise", "Extra Time", "Football Numbers",
            "The Dugout Desk", "The Terrace", "Fixture List", "The Press Box",
            "Boot Room Notes", "The 90th Minute", "Full-Time Report", "The Fan Verdict",
            "Ballon d'Or Bulletin", "Transfer Room", "North Stand Media", "Matchday Briefing"
        };
        static readonly string[] SocialAccountHandles =
        {
            "@theawayend", "@matchdaymic", "@touchlinewire", "@football_desk",
            "@goldenballdesk", "@transferledger", "@tacticalreview", "@saturdayscout",
            "@theleagueroom", "@homeendnoise", "@extratime", "@footballnumbers",
            "@dugoutdesk", "@theterrace", "@fixturelist", "@pressboxfootball",
            "@bootroomnotes", "@the90thminute", "@fulltimereport", "@fanverdict",
            "@ballondorbulletin", "@transferroom", "@northstandmedia", "@matchdaybriefing"
        };
        GameObject[] _pages;
        Button[] _tabs;
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
        Text _teamTitle;
        Text _roles;
        int _pane;
        int _teamZone = 2;
        Image _pitchBg;
        GameObject _listPage;
        GameObject _marketPage;
        RectTransform _listHost;
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
        RectTransform _staffHost;
        RectTransform _calendar;
        GameObject _modal;
        Text _modalTitle;
        Text _modalLead;
        RectTransform _modalBody;
        Button _modalAction;
        Text _modalActionLabel;
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
                    else if (i == 3) FillTeam();
                    else if (i == 4) FillStaff();
                    else if (i == 5) FillCalendar();
                    else if (i == 6) FillStandings();
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

            return posts;
        }

        PhoneNote SocialPost(int index, int seed, string text, int likes, int reposts, string age, int authorIndex = -1)
        {
            if (LocalizationManager.Current == GameLanguage.English && age.StartsWith("há ", StringComparison.Ordinal))
                age = age.Substring(3).Replace(" h", "h ago");
            var account = authorIndex >= 0
                ? authorIndex % SocialAccountNames.Length
                : (index * 7 + seed % SocialAccountNames.Length) % SocialAccountNames.Length;
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
            var firstReply = (replySeed & 0x7fffffff) % SocialAccountNames.Length;
            for (var i = 0; i < comments.Length; i++)
            {
                var account = (firstReply + i * 7) % SocialAccountNames.Length;
                if (note != null && SocialAccountHandles[account] == note.AuthorHandle)
                    account = (account + 1) % SocialAccountNames.Length;
                var row = BroadcastUi.Panel(host, "Reply" + i, new Color(1f, 1f, 1f, 0.035f));
                var top = 0.365f - i * 0.105f;
                BroadcastUi.Place(row.rectTransform, 0.04f, top - 0.1f, 0.96f, top);
                var avatar = BroadcastUi.Solid(row.transform, "Avatar", Color.white);
                avatar.sprite = BroadcastUi.Circle;
                avatar.raycastTarget = false;
                BroadcastUi.Place(avatar.rectTransform, 0.015f, 0.14f, 0.105f, 0.86f);
                var mask = avatar.gameObject.AddComponent<Mask>();
                var verifiedCompany = IsPublisherAccount(account);
                if (verifiedCompany)
                {
                    avatar.color = PublisherColor(account);
                    mask.showMaskGraphic = true;
                    AddPublisherLogo(avatar.transform, account, 10);
                }
                else
                {
                    mask.showMaskGraphic = false;
                    var faceObject = new GameObject("Face", typeof(RectTransform), typeof(Image));
                    faceObject.transform.SetParent(avatar.transform, false);
                    var faceRect = faceObject.GetComponent<RectTransform>();
                    BroadcastUi.Stretch(faceRect);
                    var face = faceObject.GetComponent<Image>();
                    face.sprite = FaceBook.SocialAvatar(SocialAccountHandles[account]);
                    face.preserveAspect = true;
                    face.raycastTarget = false;
                }
                var name = BroadcastUi.Label(row.transform, "Name", SocialAccountNames[account], 11, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 9;
                name.resizeTextMaxSize = 11;
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                name.verticalOverflow = VerticalWrapMode.Truncate;
                BroadcastUi.Place(name.rectTransform, 0.125f, 0.6f, 0.68f, 0.98f);
                if (verifiedCompany)
                {
                    var check = BroadcastUi.Solid(row.transform, "Verified", new Color(0.12f, 0.62f, 0.95f, 1f));
                    check.sprite = BroadcastUi.Circle;
                    var checkRect = check.rectTransform;
                    checkRect.anchorMin = checkRect.anchorMax = new Vector2(0.71f, 0.79f);
                    checkRect.sizeDelta = new Vector2(13f, 13f);
                    checkRect.anchoredPosition = Vector2.zero;
                    var tick = BroadcastUi.Label(check.transform, "Tick", "✓", 8, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                    BroadcastUi.Stretch(tick.rectTransform);
                }
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
            var labels = new[] { "Início", "Jornais", "Quadro", "Equipa", "Staff", "Calendário", "Tabela" };
            _tabs = new Button[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                var x0 = 0.004f + i * 0.142f;
                _tabs[i] = Tab(bar, "Tab" + i, labels[i], x0, x0 + 0.136f, i);
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
            _pages = new GameObject[7];
            _pages[0] = BuildHome(host);
            _pages[1] = BuildNews(host);
            _pages[2] = BuildBoard(host);
            _pages[3] = BuildTeam(host);
            _pages[4] = BuildStaff(host);
            _pages[5] = BuildCalendar(host);
            _pages[6] = BuildStandings(host);
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
            var title = BroadcastUi.Label(page.transform, "Title", "QUADRO DO MISTER", 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.03f, 0.9f, 0.6f, 0.98f);
            var hint = BroadcastUi.Label(page.transform, "Hint", "Escreve tu o resumo do que aconteceu. Isto não é o jornal.", 14, UiTheme.Muted, TextAnchor.MiddleRight);
            BroadcastUi.Place(hint.rectTransform, 0.35f, 0.9f, 0.97f, 0.98f);
            _noteField = UiFactory.Input(page.transform, "O que ficou deste momento...");
            _noteField.characterLimit = 180;
            BroadcastUi.Place(_noteField.GetComponent<RectTransform>(), 0.03f, 0.78f, 0.78f, 0.89f);
            var save = BroadcastUi.Button(page.transform, "SaveNote", "GUARDAR", SaveNote, UiTheme.Accent, UiTheme.ButtonText, 16);
            BroadcastUi.Place(save.GetComponent<RectTransform>(), 0.8f, 0.78f, 0.97f, 0.89f);
            _noteError = BroadcastUi.Label(page.transform, "Error", "", 14, UiTheme.Danger, TextAnchor.MiddleLeft);
            BroadcastUi.Place(_noteError.rectTransform, 0.03f, 0.72f, 0.97f, 0.78f);
            var scroll = BroadcastUi.Scroll(page.transform, "Scroll", out _boardContent);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.02f, 0.03f, 0.98f, 0.72f);
            FillBoard();
            return page.gameObject;
        }

        GameObject BuildTeam(Transform host)
        {
            var page = BroadcastUi.Panel(host, "Team", new Color(0.08f, 0.03f, 0.16f, 0.45f));
            BroadcastUi.Stretch(page.rectTransform);
            var title = BroadcastUi.Label(page.transform, "Title", "PLANTEL  ·  " + _career.Club.Tactics.Formation, 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            _teamTitle = title;
            BroadcastUi.Place(title.rectTransform, 0.5f, 0.93f, 0.78f, 0.99f);
            var caveat = BroadcastUi.Label(page.transform, "Caveat", "O cansaço é a leitura do preparador. Pode estar errado.", 13, UiTheme.Muted, TextAnchor.MiddleRight);
            BroadcastUi.Place(caveat.rectTransform, 0.72f, 0.93f, 0.99f, 0.99f);
            var pitchBg = BroadcastUi.Panel(page.transform, "PitchBg", new Color(0.05f, 0.16f, 0.08f, 1f));
            _pitchBg = pitchBg;
            BroadcastUi.Place(pitchBg.rectTransform, 0.01f, 0.34f, 0.6f, 0.92f);
            BroadcastUi.DrawPitch(pitchBg.rectTransform, new Color(0.12f, 0.46f, 0.22f));
            _pitch = BroadcastUi.Zone(page.transform, "Pitch", 0.01f, 0.34f, 0.6f, 0.92f);
            var labels = new[] { "FORMAÇÕES", "FUNÇÕES", "INSTRUÇÕES", "TÁTICAS" };
            _paneTabs = new Button[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                var pane = i;
                var y1 = 0.93f - i * 0.075f;
                _paneTabs[i] = BroadcastUi.Button(page.transform, "Pane" + i, labels[i], () => SelectPane(pane), new Color(1f, 1f, 1f, 0.08f), Color.white, 14);
                BroadcastUi.Place(_paneTabs[i].GetComponent<RectTransform>(), 0.63f, y1 - 0.068f, 0.98f, y1);
            }

            _sheet = BroadcastUi.Zone(page.transform, "Sheet", 0.63f, 0.02f, 0.98f, 0.6f);
            _tacticsBody = BroadcastUi.Zone(page.transform, "Tactics", 0.63f, 0.02f, 0.98f, 0.6f);
            _tacHost = _tacticsBody.gameObject;
            _tacHost.SetActive(false);
            _subsTab = BroadcastUi.Button(page.transform, "SubsTab", "SUPLENTES", () => { _reserves = false; FillTeam(); }, UiTheme.Accent, UiTheme.ButtonText, 14);
            BroadcastUi.Place(_subsTab.GetComponent<RectTransform>(), 0.01f, 0.26f, 0.18f, 0.33f);
            _reservesTab = BroadcastUi.Button(page.transform, "ReservesTab", "RESERVAS", () => { _reserves = true; FillTeam(); }, new Color(1f, 1f, 1f, 0.08f), Color.white, 14);
            BroadcastUi.Place(_reservesTab.GetComponent<RectTransform>(), 0.19f, 0.26f, 0.36f, 0.33f);
            _subs = BroadcastUi.Zone(page.transform, "Subs", 0.01f, 0.01f, 0.6f, 0.25f);
            var zoneNames = new[] { "LISTA", "MERCADO", "TÁTICA" };
            _zoneButtons = new Button[zoneNames.Length];
            for (var i = 0; i < zoneNames.Length; i++)
            {
                var zone = i;
                var x = 0.01f + i * 0.16f;
                var button = BroadcastUi.Button(page.transform, "Zone" + i, zoneNames[i], () => ShowTeamZone(zone), i == 2 ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), i == 2 ? UiTheme.ButtonText : Color.white, 14);
                _zoneButtons[i] = button;
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
            var search = UiFactory.Input(market.transform, "Procurar jogador pelo nome");
            _marketSearch = search;
            BroadcastUi.Place(search.GetComponent<RectTransform>(), 0.02f, 0.9f, 0.98f, 0.98f);
            search.onValueChanged.AddListener(value =>
            {
                if (_teamZone == 1)
                    FillMarket();
            });
            _listPage.SetActive(false);
            _marketPage.SetActive(false);
            FillTeam();
            return page.gameObject;
        }

        void ShowTeamZone(int zone)
        {
            _teamZone = zone;
            if (_zoneButtons != null)
            {
                for (var i = 0; i < _zoneButtons.Length; i++)
                {
                    if (_zoneButtons[i] == null)
                        continue;
                    _zoneButtons[i].GetComponent<Image>().color = i == zone ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f);
                    var label = _zoneButtons[i].GetComponentInChildren<Text>();
                    if (label != null)
                        label.color = i == zone ? UiTheme.ButtonText : Color.white;
                }
            }
            var tactics = zone == 2;
            if (_pitchBg != null)
                _pitchBg.gameObject.SetActive(tactics);
            if (_pitch != null)
                _pitch.gameObject.SetActive(tactics);
            if (_subs != null)
                _subs.gameObject.SetActive(tactics);
            if (_subsTab != null)
                _subsTab.gameObject.SetActive(tactics);
            if (_reservesTab != null)
                _reservesTab.gameObject.SetActive(tactics);
            if (_teamTitle != null)
                _teamTitle.gameObject.SetActive(tactics);
            if (_paneTabs != null)
            {
                foreach (var tab in _paneTabs)
                {
                    if (tab != null)
                        tab.gameObject.SetActive(tactics);
                }
            }

            if (_sheet != null)
                _sheet.gameObject.SetActive(tactics && _pane != 3);
            if (_tacHost != null)
                _tacHost.SetActive(tactics && _pane == 3);
            if (_listPage != null)
                _listPage.SetActive(zone == 0);
            if (_marketPage != null)
                _marketPage.SetActive(zone == 1);
            if (zone == 0)
                FillSquadList();
            if (zone == 1)
                FillMarket();
        }

        void FillSquadList()
        {
            Clear(_listHost);
            foreach (var player in Squad(_career.Club))
            {
                var row = BroadcastUi.Panel(_listHost, "P", new Color(0.1f, 0.04f, 0.2f, 0.92f), true);
                BroadcastUi.Preferred(row, 64);
                var face = FaceImage(row.transform, player, 42f);
                face.rectTransform.anchorMin = face.rectTransform.anchorMax = new Vector2(0.04f, 0.5f);
                face.rectTransform.sizeDelta = new Vector2(42, 42);
                var hurt = player.Injury != null && player.Injury.IsInjured ? "   ·   lesionado" : "";
                var line = BroadcastUi.Label(row.transform, "Line", player.SquadNumber + "   " + player.FullName + "   ·   " + PresentationLabels.Position(player.Position) + hurt, 16, Color.white, TextAnchor.MiddleLeft);
                BroadcastUi.Place(line.rectTransform, 0.08f, 0.12f, 0.62f, 0.88f);
                var captured = player;
                var write = BroadcastUi.Button(row.transform, "Note", "NOTA", () => OpenPlayerNote(captured), new Color(1f, 1f, 1f, 0.08f), Color.white, 13);
                BroadcastUi.Place(write.GetComponent<RectTransform>(), 0.64f, 0.18f, 0.78f, 0.82f);
                var file = BroadcastUi.Button(row.transform, "File", "FICHA", () => OpenPlayer(captured), UiTheme.Accent, UiTheme.ButtonText, 13);
                BroadcastUi.Place(file.GetComponent<RectTransform>(), 0.8f, 0.18f, 0.97f, 0.82f);
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
            var head = BroadcastUi.Label(_marketHost, "Head", title, 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Preferred(head, 32);
        }

        void EmptyLine(string text)
        {
            var empty = BroadcastUi.Label(_marketHost, "Empty", text, 14, UiTheme.Muted, TextAnchor.MiddleLeft);
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
                    ? "The scout will return with the report on " + LocalizationManager.FormatDate(new DateTime(job.Due), "d 'de' MMMM") + "."
                    : "O olheiro volta com o relatório a " + LocalizationManager.FormatDate(new DateTime(job.Due), "d 'de' MMMM") + ".";
            else
                read = "Ainda sem relatório.";
            var clubName = _career.World.Clubs.ContainsKey(player.ClubId) ? _career.World.Clubs[player.ClubId].ShortName : "";
            var line = BroadcastUi.Label(row.transform, "Line", player.FullName + "   " + PresentationLabels.Position(player.Position) + "   " + clubName + "\n" + read, 14, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(line.rectTransform, 0.07f, 0.08f, 0.62f, 0.92f);
            var captured = player;
            if (own)
            {
                var off = BroadcastUi.Button(row.transform, "Off", "RETIRAR", () =>
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
                var drop = BroadcastUi.Button(row.transform, "Drop", "TIRAR", () =>
                {
                    _career.Targets.Remove(captured.Id);
                    FillMarket();
                }, new Color(1f, 1f, 1f, 0.08f), Color.white, 13);
                BroadcastUi.Place(drop.GetComponent<RectTransform>(), 0.64f, 0.18f, 0.78f, 0.82f);
            }
            else
            {
                var add = BroadcastUi.Button(row.transform, "Add", "LISTA", () =>
                {
                    _career.Targets.Add(captured.Id);
                    _career.Listed.Remove(captured.Id);
                    FillMarket();
                }, new Color(1f, 1f, 1f, 0.08f), Color.white, 13);
                BroadcastUi.Place(add.GetComponent<RectTransform>(), 0.64f, 0.18f, 0.78f, 0.82f);
            }

            if (job != null && !seen)
            {
                var wait = BroadcastUi.Label(row.transform, "Wait", "À ESPERA", 13, UiTheme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(wait.rectTransform, 0.8f, 0.18f, 0.97f, 0.82f);
                return;
            }

            if (!seen)
            {
                var watch = BroadcastUi.Button(row.transform, "Scout", "OLHEIRO", () => AskScout(captured), UiTheme.Accent, UiTheme.ButtonText, 13);
                BroadcastUi.Place(watch.GetComponent<RectTransform>(), 0.8f, 0.18f, 0.97f, 0.82f);
                return;
            }

            var sign = BroadcastUi.Button(row.transform, "Sign", "CONTRATAR", () => Sign(captured), new Color(0.15f, 0.55f, 0.28f, 1f), Color.white, 13);
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
            var cost = MarketValue(player);
            if (_career.Club.Finance != null && _career.Club.Finance.TransferBudget < cost)
                return;
            var previousClubId = player.ClubId;
            var previousClubName = "mercado de jogadores";
            if (_career.World.Clubs.TryGetValue(previousClubId ?? "", out var from))
            {
                previousClubName = from.Name;
                from.PlayerIds.Remove(player.Id);
            }
            player.ClubId = _career.Club.Id;
            if (!_career.Club.PlayerIds.Contains(player.Id))
                _career.Club.PlayerIds.Add(player.Id);
            if (_career.Club.Finance != null)
                _career.Club.Finance.TransferBudget -= cost;
            _career.Listed.Remove(player.Id);
            _career.Targets.Remove(player.Id);
            _career.ScoutedPlayers.Remove(player.Id);
            CareerMemories.Record(_career, new FootballMemory
            {
                WhenTicks = _career.Calendar.CurrentDate.Ticks,
                Importance = 3,
                Kind = "contratacao",
                ClubId = _career.Club.Id,
                RelatedClubId = previousClubId,
                PlayerId = player.Id,
                Title = "Novo reforço: " + player.FullName,
                Detail = player.FullName + " chegou ao " + _career.Club.Name + " vindo de " + previousClubName + "."
            });
            CareerNotificationQueue.Enqueue("REFORÇO CONFIRMADO", player.FullName + " juntou-se ao " + _career.Club.Name + ". A contratação fica registada na história do clube.");
            CareerStore.Save(_career);
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
            var close = BroadcastUi.CloseX(card.transform, "Close", FillTactics);
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.84f, 0.86f, 0.96f, 0.97f);
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

        GameObject BuildStaff(Transform host)
        {
            var page = BroadcastUi.Panel(host, "Staff", new Color(0.08f, 0.03f, 0.16f, 0.45f));
            BroadcastUi.Stretch(page.rectTransform);
            var title = BroadcastUi.Label(page.transform, "Title", "EQUIPA TÉCNICA", 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.04f, 0.9f, 0.5f, 0.98f);
            _vibe = BroadcastUi.Label(page.transform, "Vibe", "", 16, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(_vibe.rectTransform, 0.04f, 0.8f, 0.96f, 0.9f);
            _staffHost = BroadcastUi.Zone(page.transform, "Line", 0.02f, 0.08f, 0.98f, 0.78f);
            FillStaff();
            return page.gameObject;
        }

        GameObject BuildCalendar(Transform host)
        {
            var page = BroadcastUi.Panel(host, "Calendar", new Color(0.08f, 0.03f, 0.16f, 0.45f));
            BroadcastUi.Stretch(page.rectTransform);
            var title = BroadcastUi.Label(page.transform, "Title", "CALENDÁRIO  ·  nota do adjunto em cada jogo", 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.03f, 0.92f, 0.62f, 0.99f);
            var trip = BroadcastUi.Button(page.transform, "Trip", "MARCAR VIAGEM", OpenTrips, UiTheme.Accent, UiTheme.ButtonText, 14);
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
            var close = BroadcastUi.CloseX(card.transform, "Close", CloseModal);
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.9f, 0.9f, 0.97f, 0.98f);
            _modalAction = BroadcastUi.Button(card.transform, "Action", "", null, UiTheme.Accent, UiTheme.ButtonText, 16);
            BroadcastUi.Place(_modalAction.GetComponent<RectTransform>(), 0.32f, 0.04f, 0.78f, 0.13f);
            _modalActionLabel = _modalAction.GetComponentInChildren<Text>();
            _modal = modal.gameObject;
            _modal.SetActive(false);
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
            if (elapsedDays > 0 && SettingsPrefs.AutomaticTraining)
                ApplyTraining(elapsedDays);
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
                _noteError.text = "Escreve pelo menos uma frase.";
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
            if (index == 1)
                FillNews();
            if (index == 6)
                FillStandings();
            for (var i = 0; i < _pages.Length; i++)
            {
                _pages[i].SetActive(i == index);
                var label = _tabs[i].GetComponentInChildren<Text>();
                label.color = i == index ? UiTheme.ButtonText : Color.white;
                label.fontStyle = i == index ? FontStyle.Bold : FontStyle.Normal;
                _tabs[i].GetComponent<Image>().color = i == index ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.06f);
            }

            if (_tabBar != null)
                _tabBar.SetAsLastSibling();
            if (_play != null)
                _play.transform.SetAsLastSibling();
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
                var label = BroadcastUi.Label(line, "N", (i + 1) + "  " + name, 14, mineRow ? UiTheme.Score : Color.white, TextAnchor.MiddleLeft, mineRow ? FontStyle.Bold : FontStyle.Normal);
                BroadcastUi.Place(label.rectTransform, 0.16f, 0.08f, 0.72f, 0.92f);
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
                var paperLabel = string.IsNullOrEmpty(story.Kicker) ? PaperName(story.Paper) : LocalizationManager.Translate(story.Kicker) + "  ·  " + PaperName(story.Paper);
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
            if (LocalizationManager.Current != GameLanguage.English)
                return paper;
            if (paper == "A Gazeta do Relvado") return "The Touchline Gazette";
            if (paper == "O Bancada") return "The Stand";
            if (paper == "Diário da Liga") return "League Daily";
            return paper;
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
            if (LocalizationManager.Current != GameLanguage.English || story == null)
                return story == null ? "" : LocalizationManager.Translate(story.Headline);
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
            if (LocalizationManager.Current != GameLanguage.English || story == null)
                return story == null ? "" : story.Quote;
            if (story.Kicker == "MERCADO") return "I won't comment on names. Any discussion is with the club, not the papers.";
            if (story.Kicker == "ENTREVISTA") return "If the ball reaches me in the box, I won't think twice.";
            return "We went looking for the goal. The result reflects what we saw from the touchline.";
        }

        string PressBody(PaperStory story)
        {
            if (LocalizationManager.Current != GameLanguage.English || story == null)
                return story == null ? "" : LocalizationManager.Translate(story.Body);
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

        Fixture PressFixture(PaperStory story)
        {
            foreach (var fixture in _career.Fixtures)
                if (fixture.Played && fixture.HomeClubId == story.HomeClubId && fixture.AwayClubId == story.AwayClubId)
                    return fixture;
            return null;
        }

        void OpenMemories()
        {
            var memories = new List<FootballMemory>(_career.Memories);
            memories.Sort((a, b) => b.WhenTicks.CompareTo(a.WhenTicks));
            OpenModal("Memórias da carreira", memories.Count == 0
                ? "Os momentos importantes ficam guardados aqui ao longo da época."
                : memories.Count + " momentos guardados · a história acompanha as tuas decisões.", false);
            if (memories.Count == 0)
            {
                AddLine("Ainda não há momentos registados. Estreias, grandes jogos, decisões e mudanças importantes aparecerão aqui.");
                return;
            }

            for (var i = 0; i < memories.Count; i++)
            {
                var memory = memories[i];
                if (memory == null)
                    continue;
                var date = memory.WhenTicks > 0 && memory.WhenTicks <= DateTime.MaxValue.Ticks
                    ? LocalizationManager.FormatDate(new DateTime(memory.WhenTicks), "d MMM yyyy")
                    : "Data não disponível";
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
            var digest = BroadcastUi.Panel(_boardContent, "Digest", new Color(0.1f, 0.28f, 0.34f, 0.92f));
            BroadcastUi.Preferred(digest, 240);
            var head = BroadcastUi.Label(digest.transform, "Head", "ESTADO DO DIA", 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(head.rectTransform, 0.04f, 0.72f, 0.96f, 0.94f);
            var body = BroadcastUi.Label(digest.transform, "Body", ClubDigest(), 16, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.Place(body.rectTransform, 0.04f, 0.06f, 0.96f, 0.72f);

            var report = BroadcastUi.Panel(_boardContent, "WeekReport", new Color(0.16f, 0.08f, 0.28f, 0.94f));
            BroadcastUi.Preferred(report, 110);
            var reportHead = BroadcastUi.Label(report.transform, "Head", "RELATÓRIO DA SEMANA", 14, UiTheme.Score, TextAnchor.MiddleLeft, FontStyle.Bold);
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
                    var weekLabel = BroadcastUi.Label(_boardContent, "Week", "SEMANA DE " + LocalizationManager.FormatDate(start, "d 'de' MMMM").ToUpperInvariant(), 15, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                    BroadcastUi.Preferred(weekLabel, 32);
                }

                var key = when.ToString("yyyy-MM-dd");
                if (dayKey != key)
                {
                    dayKey = key;
                    var dayLabel = BroadcastUi.Label(_boardContent, "DayHead", LocalizationManager.FormatDate(when, "dddd, d 'de' MMMM"), 14, UiTheme.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
                    BroadcastUi.Preferred(dayLabel, 26);
                }

                var card = BroadcastUi.Panel(_boardContent, "Note", new Color(0.14f, 0.06f, 0.28f, 0.92f));
                BroadcastUi.Preferred(card, 84);
                var hour = BroadcastUi.Label(card.transform, "Hour", note.Hour, 22, UiTheme.Score, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(hour.rectTransform, 0.03f, 0.2f, 0.18f, 0.8f);
                var noteText = BroadcastUi.Label(card.transform, "Text", note.Text, 16, Color.white, TextAnchor.MiddleLeft);
                BroadcastUi.Place(noteText.rectTransform, 0.2f, 0.1f, 0.97f, 0.9f);
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
            PlaceShape(_pitch, _career.Club, true, 92f, 78f);
            if (_teamTitle != null)
                _teamTitle.text = "PLANTEL  ·  " + _career.Club.Tactics.Formation;
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
                var empty = BroadcastUi.Label(_subs, "Empty", _reserves ? "Sem reservas." : "Sem suplentes.", 16, UiTheme.Muted, TextAnchor.MiddleCenter);
                empty.rectTransform.anchorMin = empty.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                empty.rectTransform.sizeDelta = new Vector2(240, 32);
            }

            for (var i = 0; i < shown.Count; i++)
            {
                var card = PersonCard(_subs, shown[i], _career.Club, true, -1, true, 96f, 68f);
                card.anchorMin = card.anchorMax = new Vector2((i + 0.5f) / Mathf.Max(1, shown.Count), 0.5f);
                card.sizeDelta = new Vector2(96, 68);
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
                var y = rows == 1 ? 0.5f : 0.12f + row * (0.76f / (rows - 1));
                for (var col = 0; col < count && cursor < eleven.Count; col++)
                {
                    var fieldIndex = cursor;
                    var card = PersonCard(host, eleven[cursor], club, fatigue, fieldIndex, false, width, height);
                    card.anchorMin = card.anchorMax = new Vector2((count - col) / (count + 1f), y);
                    card.sizeDelta = new Vector2(width, height);
                    cursor++;
                }
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
                var count = Mathf.Max(1, staff.Count);
                var step = 1f / (count + 1f);
                card.rectTransform.anchorMin = card.rectTransform.anchorMax = new Vector2(step * (i + 1), 0.48f);
                card.rectTransform.sizeDelta = new Vector2(count > 4 ? 200 : 240, 200);
                var captured = member;
                BroadcastUi.Hit(card, () => OpenStaff(captured));
                var portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                var portraitRect = portraitObject.GetComponent<RectTransform>();
                portraitRect.SetParent(card.transform, false);
                BroadcastUi.Place(portraitRect, 0.04f, 0.52f, 0.31f, 0.95f);
                var portrait = portraitObject.GetComponent<Image>();
                portrait.sprite = FaceBook.For(member);
                portrait.preserveAspect = true;
                portrait.raycastTarget = false;

                var name = BroadcastUi.Label(card.transform, "Name", member.FullName.ToUpperInvariant(), 15, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(name.rectTransform, 0.32f, 0.62f, 0.97f, 0.93f);
                var role = BroadcastUi.Label(card.transform, "Role", PresentationLabels.StaffRole(member.Role), 12, UiTheme.Accent, TextAnchor.MiddleCenter);
                BroadcastUi.Place(role.rectTransform, 0.31f, 0.43f, 0.97f, 0.62f);
                var read = BroadcastUi.Label(card.transform, "Read", StaffReport.Quality(member), 12, UiTheme.Muted, TextAnchor.UpperCenter);
                BroadcastUi.Place(read.rectTransform, 0.08f, 0.08f, 0.92f, 0.37f);
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
            _tabs = new Button[7];
            for (var i = 0; i < _tabs.Length; i++)
            {
                var index = i;
                _tabs[i] = SceneBind.Wire(root.Find("Tabs/Tab" + i), () => Show(index));
            }
            RefreshCareerTabLabels();

            var pageNames = new[] { "Home", "News", "Board", "Team", "Staff", "Calendar", "Standings" };
            _pages = new GameObject[pageNames.Length];
            for (var i = 0; i < pageNames.Length; i++)
                _pages[i] = root.Find("Pages/" + pageNames[i]).gameObject;

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
            _noteField = SceneBind.Get<InputField>(root, "Pages/Board/Input");
            _noteError = SceneBind.Get<Text>(root, "Pages/Board/Error");
            _boardContent = root.Find("Pages/Board/Scroll/Content") as RectTransform;
            SceneBind.Wire(root.Find("Pages/Board/SaveNote"), SaveNote);

            var team = root.Find("Pages/Team");
            _teamTitle = SceneBind.Get<Text>(team, "Title");
            _pitchBg = SceneBind.Get<Image>(team, "PitchBg");
            _pitch = team.Find("Pitch") as RectTransform;
            _subs = team.Find("Subs") as RectTransform;
            _sheet = team.Find("Sheet") as RectTransform;
            _tacticsBody = team.Find("Tactics") as RectTransform;
            _tacHost = _tacticsBody.gameObject;
            _subsTab = SceneBind.Wire(team.Find("SubsTab"), () => { _reserves = false; FillTeam(); });
            _reservesTab = SceneBind.Wire(team.Find("ReservesTab"), () => { _reserves = true; FillTeam(); });
            _paneTabs = new Button[4];
            for (var i = 0; i < _paneTabs.Length; i++)
            {
                var pane = i;
                _paneTabs[i] = SceneBind.Wire(team.Find("Pane" + i), () => SelectPane(pane));
            }

            _zoneButtons = new Button[3];
            for (var i = 0; i < _zoneButtons.Length; i++)
            {
                var zone = i;
                _zoneButtons[i] = SceneBind.Wire(team.Find("Zone" + i), () => ShowTeamZone(zone));
            }

            _listPage = team.Find("ListPage").gameObject;
            _listHost = team.Find("ListPage/ListScroll/Content") as RectTransform;
            _marketPage = team.Find("MarketPage").gameObject;
            _marketHost = team.Find("MarketPage/MarketScroll/Content") as RectTransform;
            _marketSearch = SceneBind.Get<InputField>(team, "MarketPage/Input");
            if (_marketSearch != null)
            {
                _marketSearch.onValueChanged.RemoveAllListeners();
                _marketSearch.onValueChanged.AddListener(value =>
                {
                    if (_teamZone == 1)
                        FillMarket();
                });
            }

            _vibe = SceneBind.Get<Text>(root, "Pages/Staff/Vibe");
            _staffHost = root.Find("Pages/Staff/Line") as RectTransform;
            _calendar = root.Find("Pages/Calendar/Scroll/Content") as RectTransform;
            SceneBind.Wire(root.Find("Pages/Calendar/Trip"), OpenTrips);
            _tableContent = root.Find("Pages/Standings/Grid") as RectTransform;
            _play = SceneBind.Wire(root.Find("PlayMatch"), OnContinue);
            _playLabel = _play.GetComponentInChildren<Text>();
            _modal = root.Find("Modal").gameObject;
            _modalTitle = SceneBind.Get<Text>(root, "Modal/Card/Title");
            _modalLead = SceneBind.Get<Text>(root, "Modal/Card/Lead");
            _modalBody = root.Find("Modal/Card/Scroll/Content") as RectTransform;
            _modalAction = SceneBind.Get<Button>(root, "Modal/Card/Action");
            _modalActionLabel = _modalAction.GetComponentInChildren<Text>();
            SceneBind.Wire(root.Find("Modal/Card/Close"), CloseModal);
            SceneBind.Wire(root.Find("Modal/Dim"), CloseModal);
            _modal.SetActive(false);
        }

        void RefreshCareerTabLabels()
        {
            if (_tabs == null)
                return;
            var sources = new[] { "Início", "Jornais", "Quadro", "Equipa", "Staff", "Calendário", "Tabela" };
            for (var i = 0; i < _tabs.Length && i < sources.Length; i++)
            {
                if (_tabs[i] == null)
                    continue;
                var label = _tabs[i].GetComponentInChildren<Text>();
                if (label != null)
                    label.text = LocalizationManager.Translate(sources[i]);
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
            AddLine(AssistantNote(opp));
            AddHead(LocalizationManager.Translate("PLANTEL") + "  ·  " + opp.Tactics.Formation);
            var pitch = BroadcastUi.Panel(_modalBody, "Shape", new Color(0.1f, 0.04f, 0.2f, 0.72f));
            BroadcastUi.Preferred(pitch, 520);
            PlaceShape(pitch.transform, opp, false, 84f, 72f);

            if (scouted)
                return;
            _modalAction.onClick.RemoveAllListeners();
            _modalActionLabel.text = LocalizationManager.Translate("ANALISAR O PLANTEL  ·  3 DIAS");
            _modalAction.onClick.AddListener(() => StartScout(opp, fixture));
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
            var clubName = _career.World.Clubs.TryGetValue(player.ClubId, out var playerClub) ? playerClub.Name : "Clube desconhecido";
            OpenModal(player.FullName, clubName + "  ·  " + PresentationLabels.Position(player.Position) + "  ·  " + player.Nationality + "  ·  " + player.Age + " anos", false);
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
            var own = player.ClubId == _career.Club.Id;
            var scouted = own || _career.ScoutedPlayers.Contains(player.Id);
            var rating = PlayerAttributes.CurrentOverallAtPosition(player, player.Position);
            var baseline = PlayerAttributes.OverallAtPosition(player, player.Position);
            var delta = rating - baseline;
            var current = PlayerAttributes.Current(player);
            if (player.Traits == null)
                PlayerTraits.Assign(player, player.Id.GetHashCode());

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
            var positionLabel = ProfileText(header.transform, "Position", "#" + player.SquadNumber + "  ·  " + PresentationLabels.Position(player.Position) + (player.IsCaptain ? "  ·  CAPITÃO" : player.IsViceCaptain ? "  ·  SUBCAPITÃO" : ""), 16, UiTheme.Accent, FontStyle.Bold, 0.16f, 0.62f, 0.42f, 0.9f);
            var clubLabel = ProfileText(header.transform, "Club", (_career.World.Clubs.TryGetValue(player.ClubId, out var club) ? club.Name : "Clube desconhecido") + "  ·  " + player.Age + " anos", 13, Color.white, FontStyle.Bold, 0.12f, 0.38f, 0.43f, 0.61f);
            var positions = new List<string> { PresentationLabels.Position(player.Position) };
            for (var i = 0; i < player.AlternatePositions.Count; i++)
                positions.Add(PresentationLabels.Position(player.AlternatePositions[i]));
            var positionDetails = "POSIÇÕES  " + string.Join("  ·  ", positions) + "\n" + string.Join("   ", positions.ConvertAll(p => p + " " + PlayerAttributes.CurrentOverallAtPosition(player, PositionFromLabel(p))));
            ProfileText(header.transform, "AltPositions", positionDetails, 12, UiTheme.Muted, FontStyle.Normal, 0.12f, 0.05f, 0.43f, 0.36f);

            var overallCard = ProfilePanelAt(header.transform, "Overall", new Color(0.04f, 0.09f, 0.12f, 1f), 0.445f, 0.08f, 0.555f, 0.92f);
            ProfileText(overallCard.transform, "Value", rating.ToString(), 38, Color.white, FontStyle.Bold, 0.04f, 0.39f, 0.96f, 0.91f, TextAnchor.MiddleCenter);
            ProfileText(overallCard.transform, "Label", "OVR ATUAL", 9, UiTheme.Accent, FontStyle.Bold, 0.02f, 0.08f, 0.98f, 0.34f, TextAnchor.MiddleCenter);
            ProfileText(header.transform, "BaseOVR", "BASE " + baseline + "   ·   FORMA " + (delta >= 0 ? "+" : "") + delta, 12, UiTheme.Muted, FontStyle.Bold, 0.57f, 0.72f, 0.78f, 0.94f);
            ProfileText(header.transform, "Potential", PlayerTraits.HiddenPotential(player, scouted), 11, UiTheme.AccentSoft, FontStyle.Normal, 0.57f, 0.47f, 0.78f, 0.7f);
            ProfileText(header.transform, "MarketValue", "VALOR DE MERCADO\n" + Euro(MarketValue(player)), 12, Color.white, FontStyle.Bold, 0.57f, 0.27f, 0.78f, 0.46f);
            var info = new[]
            {
                "ALTURA  " + (player.HeightCm > 0 ? player.HeightCm + " cm" : "—"),
                "PESO  " + (player.WeightKg > 0 ? player.WeightKg + " kg" : "—"),
                "PÉ  " + PresentationLabels.Foot(player.PreferredFoot) + "  ·  PÉ FRACO  " + WeakFootLabel(player.WeakFootQuality),
                "PERSONALIDADE  " + PresentationLabels.Personality(player.Personality),
                "REPUTAÇÃO  " + (_career.World.Clubs.TryGetValue(player.ClubId, out club) ? club.Reputation.ToString() : "—")
            };
            ProfileText(header.transform, "PhysicalAndInfo", string.Join("\n", info), 12, Color.white, FontStyle.Normal, 0.79f, 0.05f, 0.985f, 0.95f);

            var technical = new[] { "Cruzamento", "Drible", "Finalização", "Passe", "Desarme" };
            var mental = new[] { "Posicionamento", "Frieza", "Decisão", "Entrega", "Moral", "Forma" };
            var physical = keeper
                ? new[] { "Velocidade", "Resistência", "Força", "Reflexos", "Manejo" }
                : new[] { "Velocidade", "Resistência", "Força" };
            BuildDenseAttributeColumn("Técnica", technical, player, 0.005f, mainBottom, 0.196f, mainTop);
            BuildDenseAttributeColumn("Mental", mental, player, 0.201f, mainBottom, 0.392f, mainTop);
            BuildDenseAttributeColumn("Físico", physical, player, 0.397f, mainBottom, 0.588f, mainTop);

            var radarPanel = ProfilePanel("Radar", 0.593f, mainBottom, 0.79f, mainTop);
            ProfileText(radarPanel.transform, "Heading", "PERFIL", 12, UiTheme.Accent, FontStyle.Bold, 0.04f, 0.87f, 0.96f, 0.99f, TextAnchor.MiddleCenter);
            var radarObject = new GameObject("PlayerRadar", typeof(RectTransform), typeof(CanvasRenderer), typeof(PlayerRadarGraphic));
            radarObject.transform.SetParent(radarPanel.transform, false);
            var radarRect = radarObject.GetComponent<RectTransform>();
            BroadcastUi.Place(radarRect, 0.13f, 0.1f, 0.87f, 0.84f);
            RadarLabel(radarPanel.transform, "AttackAxis", "ATAQUE", 0.34f, 0.82f, 0.66f, 0.96f);
            RadarLabel(radarPanel.transform, "TechniqueAxis", "TÉCNICA", 0.68f, 0.62f, 0.99f, 0.76f);
            RadarLabel(radarPanel.transform, "MentalAxis", "MENTAL", 0.68f, 0.17f, 0.99f, 0.31f);
            RadarLabel(radarPanel.transform, "DefenceAxis", "DEFESA", 0.005f, 0.21f, 0.29f, 0.36f);
            RadarLabel(radarPanel.transform, "PhysicalAxis", "FÍSICO", 0.005f, 0.62f, 0.29f, 0.76f);
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
            ProfileText(traitsPanel.transform, "ProsTitle", "TRAÇOS E PONTOS FORTES", 11, UiTheme.Accent, FontStyle.Bold, 0.05f, 0.87f, 0.95f, 0.99f);
            var traitY = 0.83f;
            var traitStep = player.Traits.Count > 4 ? 0.115f : 0.145f;
            if (player.Traits.Count == 0)
                ProfileText(traitsPanel.transform, "NoTraits", "Sem traços especiais.", 11, UiTheme.Muted, FontStyle.Normal, 0.05f, 0.68f, 0.95f, 0.84f);
            else
            {
                for (var i = 0; i < player.Traits.Count; i++)
                {
                    var trait = player.Traits[i];
                    var issue = trait == PlayerTrait.PropensoALesoes;
                    var traitLabel = PlayerTraits.Label(trait);
                    var effect = PlayerTraits.Effect(trait);
                    ProfileText(traitsPanel.transform, "Trait" + i, (issue ? "ATENÇÃO  ·  " : "•  ") + traitLabel + "\n" + effect, 11, issue ? UiTheme.FitnessLow : Color.white, FontStyle.Normal, 0.05f, traitY - traitStep, 0.95f, traitY);
                    traitY -= traitStep;
                }
            }
            var injury = player.Injury != null && player.Injury.IsInjured
                ? player.Injury.Description + "  ·  " + player.Injury.DaysRemaining + " dias"
                : "Sem lesão reportada";
            var physio = Member(_career.Club, StaffRole.Physiotherapist);
            ProfileText(traitsPanel.transform, "Medical", (physio == null ? "Fisioterapia" : physio.FullName) + "\n" + injury, 10, player.Injury != null && player.Injury.IsInjured ? UiTheme.FitnessLow : UiTheme.Muted, FontStyle.Normal, 0.05f, 0.04f, 0.95f, Mathf.Max(0.1f, traitY - 0.015f));

            var status = ProfilePanel("Status", 0.005f, 0.205f, 0.995f, 0.325f);
            var statusItems = new[]
            {
                new KeyValuePair<string, string>("FITNESS", Mathf.Clamp(player.Fitness - player.Fatigue / 2, 0, 100) + "%"),
                new KeyValuePair<string, string>("DISCIPLINA", "Sem registo"),
                new KeyValuePair<string, string>("MORAL", MoraleSummary(player.Morale)),
                new KeyValuePair<string, string>("FORMA", player.Form + "/100"),
                new KeyValuePair<string, string>("TREINO", (player.TrainingRating <= 0 ? 70 : player.TrainingRating) + "/100")
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
            ProfileText(statsPanel.transform, "Heading", "ÉPOCA · TODAS AS COMPETIÇÕES", 11, UiTheme.Accent, FontStyle.Bold, 0.03f, 0.7f, 0.97f, 0.98f);
            var seasonAverage = rated > 0 ? (ratingSum / rated).ToString("0.00", CultureInfo.InvariantCulture) : "—";
            var seasonStats = LocalizationManager.Current == GameLanguage.English
                ? "Apps  " + apps + "     Goals  " + goals + "     Assists  " + assists + "     Avg  " + seasonAverage
                : "J  " + apps + "     G  " + goals + "     A  " + assists + "     MÉDIA  " + seasonAverage;
            ProfileText(statsPanel.transform, "SeasonStats", seasonStats, 11, Color.white, FontStyle.Bold, 0.03f, 0.4f, 0.97f, 0.68f);
            ProfileText(statsPanel.transform, "CareerHeading", "CARREIRA", 10, UiTheme.Accent, FontStyle.Bold, 0.03f, 0.18f, 0.97f, 0.39f);
            var careerAverage = careerRated > 0 ? (careerRatingSum / careerRated).ToString("0.00", CultureInfo.InvariantCulture) : "—";
            var careerStats = LocalizationManager.Current == GameLanguage.English
                ? "Apps  " + careerApps + "     Goals  " + careerGoals + "     Assists  " + careerAssists + "     Avg  " + careerAverage
                : "J  " + careerApps + "     G  " + careerGoals + "     A  " + careerAssists + "     MÉDIA  " + careerAverage;
            ProfileText(statsPanel.transform, "CareerStats", careerStats, 11, Color.white, FontStyle.Bold, 0.03f, 0.015f, 0.97f, 0.2f);

            var notePanel = ProfilePanel("ScoutAndCoach", 0.545f, 0.015f, 0.745f, 0.19f);
            var scout = Member(_career.Club, StaffRole.Scout);
            var scoutLine = _career.ScoutedPlayers.Contains(player.Id)
                ? (scout == null ? "Olheiro" : scout.FullName) + " · relatório: OVR " + StaffReport.Read(scout, player.Id, "overall", PlayerAttributes.CurrentOverall(player), 40, 94)
                : own ? "Adjunto: " + Tendency(player) : ScoutJobFor(player.Id) != null ? "Relatório do olheiro em curso" : "Sem relatório de observação";
            var fitnessValue = StaffReport.Read(fitness, player.Id, "cansaco", player.Fatigue, 0, 100);
            ProfileText(notePanel.transform, "Heading", "RELATÓRIO TÉCNICO", 10, UiTheme.Accent, FontStyle.Bold, 0.04f, 0.68f, 0.96f, 0.98f);
            ProfileText(notePanel.transform, "Note", scoutLine + "\nPreparador: cansaço " + TiredWord(fitnessValue) + "\nLeitura: " + (reader == null ? "adjunto" : reader.FullName) + " · " + StaffReport.Confidence(reader), 10, Color.white, FontStyle.Normal, 0.04f, 0.04f, 0.96f, 0.68f);

            var actionPanel = ProfilePanel("PlayerActions", 0.75f, 0.015f, 0.995f, 0.19f);
            var captured = player;
            if (own)
            {
                var forSale = _career.Listed.Contains(player.Id);
                var forLoan = _career.Loans.Contains(player.Id);
                ProfileButton(actionPanel.transform, "Transfer", forSale ? "RETIRAR DA VENDA" : "PÔR À VENDA", () =>
                {
                    if (_career.Listed.Contains(captured.Id)) _career.Listed.Remove(captured.Id);
                    else { _career.Listed.Add(captured.Id); _career.Loans.Remove(captured.Id); }
                    OpenPlayer(captured);
                }, 0.03f, 0.55f, 0.49f, 0.96f);
                ProfileButton(actionPanel.transform, "Loan", forLoan ? "RETIRAR DO EMPRÉSTIMO" : "LISTA DE EMPRÉSTIMOS", () =>
                {
                    if (_career.Loans.Contains(captured.Id)) _career.Loans.Remove(captured.Id);
                    else { _career.Loans.Add(captured.Id); _career.Listed.Remove(captured.Id); }
                    OpenPlayer(captured);
                }, 0.51f, 0.55f, 0.97f, 0.96f);
            }
            else
            {
                ClaimForeignTargets();
                var wanted = _career.Targets.Contains(player.Id);
                ProfileButton(actionPanel.transform, "Target", wanted ? "TIRAR DA LISTA" : "SEGUIR JOGADOR", () =>
                {
                    if (_career.Targets.Contains(captured.Id)) _career.Targets.Remove(captured.Id);
                    else _career.Targets.Add(captured.Id);
                    _career.Listed.Remove(captured.Id);
                    _career.Loans.Remove(captured.Id);
                    OpenPlayer(captured);
                }, 0.03f, 0.55f, 0.49f, 0.96f);
                var job = ScoutJobFor(player.Id);
                var scoutLabel = scouted ? "RELATÓRIO PRONTO" : job != null ? "OLHEIRO EM CURSO" : "PEDIR OLHEIRO";
                ProfileButton(actionPanel.transform, "Scout", scoutLabel, () =>
                {
                    AskScout(captured);
                    OpenPlayer(captured);
                }, 0.51f, 0.55f, 0.97f, 0.96f);
            }
            var personalNote = UserNote(player.Id);
            ProfileText(actionPanel.transform, "Note", string.IsNullOrEmpty(personalNote) ? "Sem notas do treinador." : personalNote, 9, UiTheme.Muted, FontStyle.Normal, 0.04f, 0.05f, 0.96f, 0.48f);

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
            ProfileText(panel.transform, "Heading", title.ToUpperInvariant(), 11, UiTheme.Accent, FontStyle.Bold, 0.04f, 0.87f, 0.96f, 0.99f);
            var gap = 0.78f / stats.Length;
            for (var i = 0; i < stats.Length; i++)
            {
                var yTop = 0.84f - i * gap;
                var yBottom = yTop - gap + 0.015f;
                var row = BroadcastUi.Zone(panel.transform, "Stat" + i, 0.04f, yBottom, 0.96f, yTop);
                var value = stats[i] == "Moral" ? Mathf.RoundToInt(player.Morale / 5f)
                    : stats[i] == "Forma" ? Mathf.RoundToInt(player.Form / 5f)
                    : PlayerAttributes.CurrentValue(player, stats[i]);
                ProfileText(row, "Name", stats[i], 9, Color.white, FontStyle.Normal, 0f, 0.52f, 0.6f, 1f);
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
            if (SettingsPrefs.AutomaticTraining)
                ApplyTraining(days);
            _career.Calendar.AdvanceDays(days);
        }

        void ApplyTraining(int days)
        {
            foreach (var player in _career.World.Players.Values)
            {
                var current = player.TrainingRating <= 0 ? 70 : player.TrainingRating;
                var youthBonus = player.Age <= 21 ? 6 : player.Age <= 25 ? 3 : 0;
                var target = Mathf.Clamp(48 + player.Motivation / 3 + player.Morale / 8 + youthBonus, 50, 94);
                var blend = 1f - Mathf.Pow(0.92f, days);
                player.TrainingRating = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(current, target, blend)), 1, 100);
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
                    if (child.name == "Title" || child.name == "Lead" || child.name == "Scroll" || child.name == "Close" || child.name == "Action")
                        continue;
                    Destroy(child.gameObject);
                }
            }
            BroadcastUi.Place(_modalTitle.rectTransform, 0.08f, 0.88f, 0.96f, 0.97f);
            BroadcastUi.Place(_modalLead.rectTransform, 0.05f, 0.78f, 0.95f, 0.88f);
            var scroll = _modal.transform.Find("Card/Scroll") as RectTransform;
            if (scroll != null)
                BroadcastUi.Place(scroll, 0.04f, 0.16f, 0.96f, 0.72f);
            _modalTitle.text = title;
            _modalLead.text = lead;
            _modalAction.gameObject.SetActive(action);
            _modalAction.onClick.RemoveAllListeners();
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
