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
        GameObject _tacHost;
        string _arm;
        string _orderId;
        RectTransform _tacticsBody;
        int _modalLineSize = 16;
        CareerState _career;
        StaffMember _offer;

        void Start()
        {
            _career = GameRoot.Require().Session.Career;
            if (_career == null)
            {
                GameRoot.Instance.Open(SceneNames.MainMenu);
                return;
            }

            if (_career.Current == null && _career.Upcoming.Count == 0)
                CareerMoments.Begin(_career);

            var root = BroadcastUi.Mount(this);
            BroadcastUi.Backdrop(root);
            BuildHeader(root);
            BuildPhone(root);
            BuildTabs(root);
            BuildPages(root);
            BuildPlay(root);
            BuildModal(root);
            var hint = BroadcastUi.Label(root, "ControlHint", "Enter  continuar     1 2 3  responder     Setas  navegar", 15, UiTheme.Muted, TextAnchor.MiddleRight);
            BroadcastUi.Place(hint.rectTransform, 0.42f, 0.008f, 0.98f, 0.048f);
            Show(0);
            Refresh();
            if (_phoneSheet != null)
                _phoneSheet.SetAsLastSibling();
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
                new[] { "Mensagens", "0.15,0.72,0.82" },
                new[] { "Plantel", "0.18,0.62,0.34" },
                new[] { "Mercado", "0.85,0.62,0.18" },
                new[] { "Staff", "0.45,0.28,0.72" },
                new[] { "Calendário", "0.85,0.35,0.28" },
                new[] { "Jornais", "0.25,0.45,0.85" }
            };
            for (var i = 0; i < apps.Length; i++)
            {
                var name = apps[i][0];
                var col = i % 2;
                var row = i / 2;
                var x0 = 0.08f + col * 0.46f;
                var y1 = 0.9f - row * 0.28f;
                var color = AppColor(name);
                var icon = BroadcastUi.Button(_phoneApps.transform, "App" + i, name, () => OpenPhoneApp(name), color, Color.white, 13);
                BroadcastUi.Place(icon.GetComponent<RectTransform>(), x0, y1 - 0.22f, x0 + 0.38f, y1);
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
            if (_phoneAppTitle != null)
                _phoneAppTitle.text = name;
            FillPhone(name);
        }

        void FillPhone(string app)
        {
            if (_phoneList == null)
                return;
            Clear(_phoneList);
            var notes = PhoneNotes();
            for (var i = 0; i < notes.Count; i++)
            {
                var note = notes[i];
                if (!PhoneMatches(app, note.Who))
                    continue;
                var row = BroadcastUi.Panel(_phoneList, "Msg" + i, new Color(1f, 1f, 1f, 0.06f));
                BroadcastUi.Preferred(row, 86);
                var who = BroadcastUi.Label(row.transform, "Who", note.Who, 15, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(who.rectTransform, 0.04f, 0.58f, 0.96f, 0.94f);
                var body = BroadcastUi.Label(row.transform, "Body", note.Text, 14, Color.white, TextAnchor.UpperLeft);
                BroadcastUi.Place(body.rectTransform, 0.04f, 0.06f, 0.96f, 0.58f);
            }
        }

        static bool PhoneMatches(string app, string who)
        {
            if (app == "Mensagens")
                return true;
            if (app == "Plantel")
                return who.Contains("jogador");
            if (app == "Mercado")
                return who.Contains("clube");
            if (app == "Staff")
                return who.Contains("equipa");
            if (app == "Calendário")
                return who.Contains("Adjunto") || who.Contains("equipa");
            return true;
        }

        List<PhoneNote> PhoneNotes()
        {
            var list = new List<PhoneNote>();
            var club = _career.Club;
            Fixture next = null;
            foreach (var fixture in _career.Fixtures)
            {
                if (fixture.Played || !fixture.Involves(club.Id))
                    continue;
                if (next == null || fixture.Date < next.Date)
                    next = fixture;
            }

            var assistant = StaffReport.Find(club, StaffRole.AssistantCoach);
            var assistantName = assistant == null ? "Adjunto" : assistant.FullName;
            if (next != null && _career.World.Clubs.TryGetValue(next.OpponentId(club.Id), out var foe))
                list.Add(new PhoneNote(assistantName + "  ·  equipa técnica", "O próximo jogo é com o " + foe.Name + ". Queres fechar o onze ou mexer na linha?"));
            else
                list.Add(new PhoneNote(assistantName + "  ·  equipa técnica", "O plantel está à espera da tua palavra para o próximo treino."));

            var fitness = StaffReport.Find(club, StaffRole.FitnessCoach);
            Player tired = null;
            foreach (var id in club.PlayerIds)
            {
                if (!_career.World.Players.TryGetValue(id, out var player))
                    continue;
                if (tired == null || player.Fatigue > tired.Fatigue)
                    tired = player;
            }

            if (tired != null)
                list.Add(new PhoneNote((fitness == null ? "Preparador" : fitness.FullName) + "  ·  equipa técnica", tired.FullName + " acusa o esforço. Se jogar seguido, o rendimento cai."));

            Player voice = null;
            foreach (var id in club.PlayerIds)
            {
                if (_career.World.Players.TryGetValue(id, out var player) && player.Position != PlayerPosition.Goalkeeper)
                {
                    voice = player;
                    break;
                }
            }

            if (voice != null)
                list.Add(new PhoneNote(voice.FullName + "  ·  jogador", "Mister, quero minutos. Se o papel não estiver claro, o meu agente vai falar contigo."));

            Club buyer = null;
            foreach (var other in _career.World.Clubs.Values)
            {
                if (other.Id != club.Id)
                {
                    buyer = other;
                    break;
                }
            }

            if (buyer != null && voice != null)
                list.Add(new PhoneNote(buyer.Name + "  ·  clube", "Temos interesse em " + voice.FullName + ". Se o abrires à transferência, fazemos uma proposta."));

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
                    list.Add(new PhoneNote((scout == null ? "Olheiro" : scout.FullName) + "  ·  equipa técnica", target.FullName + " continua na tua lista" + (owner == null ? "." : ". O " + owner.Name + " pode ouvir uma oferta.")));
                }
            }

            var physio = StaffReport.Find(club, StaffRole.Physiotherapist);
            foreach (var id in club.PlayerIds)
            {
                if (!_career.World.Players.TryGetValue(id, out var player) || player.Injury == null || !player.Injury.IsInjured)
                    continue;
                list.Add(new PhoneNote((physio == null ? "Fisioterapeuta" : physio.FullName) + "  ·  equipa técnica", player.FullName + ": " + player.Injury.Description + ". Faltam " + player.Injury.DaysRemaining + " dias."));
                break;
            }

            return list;
        }

        struct PhoneNote
        {
            public string Who;
            public string Text;
            public PhoneNote(string who, string text)
            {
                Who = who;
                Text = text;
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
            var scroll = BroadcastUi.Scroll(page.transform, "Scroll", out _newsContent);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.02f, 0.03f, 0.98f, 0.88f);
            return page.gameObject;
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
            var scroll = BroadcastUi.Scroll(page.transform, "TacticsScroll", out _tacticsBody);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.63f, 0.02f, 0.98f, 0.6f);
            _tacHost = scroll.gameObject;
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
                read = "Relatório do olheiro: overall " + StaffReport.Read(scout, player.Id, "overall", PlayerAttributes.Overall(player), 40, 94) + "  ·  " + Euro(MarketValue(player));
            else if (job != null)
                read = "O olheiro volta com o relatório a " + new DateTime(job.Due).ToString("d 'de' MMMM", CultureInfo.GetCultureInfo("pt-PT")) + ".";
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
            return Mathf.Max(80000, value);
        }

        static string Euro(int value)
        {
            return value.ToString("N0", CultureInfo.GetCultureInfo("pt-PT")) + " €";
        }

        void Sign(Player player)
        {
            if (player == null || player.ClubId == _career.Club.Id || !_career.ScoutedPlayers.Contains(player.Id))
                return;
            var cost = MarketValue(player);
            if (_career.Club.Finance != null && _career.Club.Finance.TransferBudget < cost)
                return;
            if (_career.World.Clubs.TryGetValue(player.ClubId, out var from))
                from.PlayerIds.Remove(player.Id);
            player.ClubId = _career.Club.Id;
            if (!_career.Club.PlayerIds.Contains(player.Id))
                _career.Club.PlayerIds.Add(player.Id);
            if (_career.Club.Finance != null)
                _career.Club.Finance.TransferBudget -= cost;
            _career.Listed.Remove(player.Id);
            _career.Targets.Remove(player.Id);
            _career.ScoutedPlayers.Remove(player.Id);
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
            TacticBlock("Pressão", "Quando a equipa sai à bola sem posse. Alta recupera mais cedo e sofre mais faltas.", new[] { "Pressão muito alta", "Pressão alta", "Pressão média", "Pressão baixa" }, t.Pressing, value => t.Pressing = value);
            TacticBlock("Linha defensiva", "Altura do bloco. Muito alta aperta e obriga o adversário a recuar. Muito baixa protege a área.", new[] { "Linha muito alta", "Linha defensiva alta", "Linha defensiva média", "Linha defensiva baixa", "Linha muito baixa" }, t.DefensiveLine, value => t.DefensiveLine = value);
            TacticBlock("Construção", "Como a bola sai da defesa. Curta pede qualidade de passe; longa salta linhas.", new[] { "Construção curta", "Jogo misto", "Construção longa" }, t.BuildUp, value => t.BuildUp = value);
            TacticBlock("Laterais", "O que os laterais fazem com a posse.", new[] { "Laterais ficam", "Laterais equilibram", "Laterais sobem" }, string.IsNullOrEmpty(t.FullBacks) ? "Laterais equilibram" : t.FullBacks, value => t.FullBacks = value);
            TacticBlock("Largura", "Onde a equipa ocupa o campo no ataque.", new[] { "Largura estreita", "Largura normal", "Largura larga" }, string.IsNullOrEmpty(t.Width) ? "Largura normal" : t.Width, value => t.Width = value);
            TacticBlock("Ritmo", "Velocidade com que a posse avança.", new[] { "Ritmo lento", "Ritmo normal", "Ritmo rápido" }, string.IsNullOrEmpty(t.Tempo) ? "Ritmo normal" : t.Tempo, value => t.Tempo = value);
            TacticBlock("Risco no passe", "Seguro privilegia linhas curtas. Arriscado tenta partir linhas e sofre mais cortes.", new[] { "Passe seguro", "Passe misto", "Passe arriscado" }, string.IsNullOrEmpty(t.PassingStyle) ? "Passe misto" : t.PassingStyle, value => t.PassingStyle = value);
            TacticBlock("Marcação", "Zonal guarda zonas. Ao homem segue o adversário.", new[] { "Marcação zonal", "Marcação mista", "Marcação ao homem" }, string.IsNullOrEmpty(t.Marking) ? "Marcação mista" : t.Marking, value => t.Marking = value);
            TacticBlock("Ideia de jogo", "O que a equipa procura com a bola.", new[] { "Equilibrado", "Tiki-taka", "Posse", "Cruzamentos", "Jogo direto", "Bolas longas", "Remates de longe", "Contra-ataque" }, string.IsNullOrEmpty(t.AttackIdea) ? "Equilibrado" : t.AttackIdea, value => t.AttackIdea = value);
        }

        void TacticBlock(string title, string help, string[] options, string current, Action<string> pick)
        {
            var head = BroadcastUi.Label(_tacticsBody, "H", title.ToUpperInvariant(), 18, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Preferred(head, 28);
            var body = BroadcastUi.Label(_tacticsBody, "Help", help, 15, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.SetWrapped(body, help, 36);
            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var on = option == current;
                var button = BroadcastUi.Button(_tacticsBody, title + i, option, () =>
                {
                    pick(option);
                    _career.Club.Tactics.Summary = _career.Club.Tactics.Formation + " · " + _career.Club.Tactics.Pressing + " · " + _career.Club.Tactics.BuildUp;
                    FillTactics();
                    if (_teamTitle != null)
                        _teamTitle.text = "PLANTEL  ·  " + _career.Club.Tactics.Formation;
                }, on ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), on ? UiTheme.ButtonText : Color.white, 16);
                BroadcastUi.Preferred(button.GetComponent<RectTransform>(), 42);
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
            BroadcastUi.Place(title.rectTransform, 0.03f, 0.92f, 0.97f, 0.99f);
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
                GameRoot.Instance.LiveMatch = MatchSession.Create(_career, moment.Fixture);
                GameRoot.Instance.Open(SceneNames.MatchDay);
                return;
            }

            CareerMoments.Continue(_career);
            Refresh();
            FillNews();
        }

        void Answer(int choice)
        {
            CareerMoments.Answer(_career, choice);
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
                Day = _career.Calendar.WeekdayName + ", " + _career.Calendar.CurrentDate.ToString("d 'de' MMMM", CultureInfo.GetCultureInfo("pt-PT")),
                Hour = _career.Calendar.ClockText,
                When = _career.Calendar.CurrentDate,
                Text = text
            });
            _noteField.text = "";
            FillBoard();
        }

        void Refresh()
        {
            var moment = _career.Current;
            var season = _career.League != null ? _career.League.Name + " " + _career.Calendar.SeasonLabel : _career.Calendar.SeasonLabel;
            _clock.text = _career.Calendar.ClockText;
            _dateLine.text = _career.Calendar.WeekdayName + ", " + _career.Calendar.CurrentDate.ToString("d 'de' MMMM", CultureInfo.GetCultureInfo("pt-PT")) + "    ·    " + season + "    ·    Mister " + _career.Coach.Name;
            _homeTitle.text = moment == null ? "DIA EM ABERTO" : moment.Title.ToUpperInvariant();
            Player facePlayer = null;
            if (moment != null && !string.IsNullOrEmpty(moment.PlayerId))
                _career.World.Players.TryGetValue(moment.PlayerId, out facePlayer);
            if (_homeFace != null)
            {
                _homeFace.gameObject.SetActive(facePlayer != null);
                if (facePlayer != null)
                    _homeFace.sprite = FaceBook.For(facePlayer) ?? PortraitStudio.For(facePlayer.Id);
            }
            BroadcastUi.Place(_homeTitle.rectTransform, facePlayer == null ? 0.04f : 0.16f, 0.86f, 0.96f, 0.97f);
            var body = moment == null ? "A época ficou em silêncio." : moment.Body;
            BroadcastUi.SetWrapped(_homeBody, body, 80);
            _nextChip.text = "Próximo jogo    " + NextMatchText(_career);
            var talking = moment != null && moment.Kind == MomentKind.Talk && !moment.Answered && moment.Replies != null;
            if (moment != null && moment.Kind == MomentKind.Match)
                _playLabel.text = "JOGAR    " + MatchLabel(_career, moment.Fixture);
            else if (talking)
                _playLabel.text = "ESCOLHE UMA RESPOSTA";
            else
                _playLabel.text = "CONTINUAR";
            if (_play != null)
                _play.interactable = !talking;

            _nextChip.gameObject.SetActive(!talking);
            for (var i = 0; i < _replies.Length; i++)
            {
                var show = talking && i < moment.Replies.Length;
                _replies[i].gameObject.SetActive(show);
                if (show)
                    _replies[i].GetComponentInChildren<Text>().text = (i + 1) + "    " + moment.Replies[i];
            }

            if (_vibe != null)
                _vibe.text = VibeLine();
            CompleteScouts();
            FillBoard();
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
            var families = new List<string>();
            var mine = FamilyOf(_career.Club.LeagueId);
            families.Add(mine);
            foreach (var id in new[] { "atlas", "inglesa", "francesa", "espanhola" })
            {
                if (id != mine)
                    families.Add(id);
            }

            families.Add("campeoes");
            var width = 0.99f / families.Count;
            for (var i = 0; i < families.Count; i++)
            {
                var x0 = i * width;
                var col = BroadcastUi.Zone(_tableContent, "Col" + i, x0, 0f, x0 + width - 0.006f, 1f);
                if (families[i] == "campeoes")
                {
                    TableCard(col, "league:campeoes", 0.02f, 0.98f);
                    continue;
                }

                TableCard(col, LeagueKey(families[i], true), 0.51f, 0.98f);
                TableCard(col, LeagueKey(families[i], false), 0.02f, 0.48f);
            }
        }

        static string FamilyOf(string leagueId)
        {
            if (string.IsNullOrEmpty(leagueId))
                return "atlas";
            if (leagueId.Contains("inglesa"))
                return "inglesa";
            if (leagueId.Contains("francesa"))
                return "francesa";
            if (leagueId.Contains("espanhola"))
                return "espanhola";
            if (leagueId.Contains("campeoes"))
                return "campeoes";
            return "atlas";
        }

        static string LeagueKey(string family, bool first)
        {
            if (family == "atlas")
                return first ? "league:atlas" : "league:horizonte";
            return first ? "league:" + family : "league:" + family + "-2";
        }

        void TableCard(Transform parent, string leagueId, float y0, float y1)
        {
            var card = BroadcastUi.Panel(parent, "T", new Color(0.06f, 0.02f, 0.14f, 0.92f), false);
            BroadcastUi.Place(card.rectTransform, 0.02f, y0, 0.98f, y1);
            var title = leagueId;
            foreach (var league in _career.World.Leagues)
            {
                if (league.Id == leagueId)
                    title = league.Name;
            }

            var head = BroadcastUi.Label(card.transform, "League", title.ToUpperInvariant(), 13, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(head.rectTransform, 0.04f, 0.9f, 0.96f, 0.99f);
            var siglas = BroadcastUi.Label(card.transform, "Siglas", "Pts    J   V-E-D    GM-GS", 11, UiTheme.Accent, TextAnchor.MiddleRight, FontStyle.Bold);
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
            var step = rows.Count == 0 ? 0.1f : 0.7f / Mathf.Max(1, rows.Count);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var y = 0.78f - (i + 1) * step;
                var mineRow = row.ClubId == _career.Club.Id;
                var line = BroadcastUi.Zone(card.transform, "R" + i, 0.03f, y, 0.97f, y + step - 0.004f);
                Club club = null;
                if (_career.World.Clubs.ContainsKey(row.ClubId))
                    club = _career.World.Clubs[row.ClubId];
                var crest = CrestImage(line, club, 18f);
                crest.rectTransform.anchorMin = crest.rectTransform.anchorMax = new Vector2(0.08f, 0.5f);
                crest.rectTransform.sizeDelta = new Vector2(18, 18);
                var name = club == null ? row.ClubId : club.ShortName;
                var text = (i + 1) + "  " + name;
                var stats = row.Points + " pts   " + row.Played + "J  " + row.Won + "-" + row.Drawn + "-" + row.Lost + "  " + row.GoalsFor + "-" + row.GoalsAgainst;
                var label = BroadcastUi.Label(line, "N", text, 12, mineRow ? UiTheme.Score : Color.white, TextAnchor.MiddleLeft, mineRow ? FontStyle.Bold : FontStyle.Normal);
                BroadcastUi.Place(label.rectTransform, 0.16f, 0.08f, 0.48f, 0.92f);
                var numbers = BroadcastUi.Label(line, "S", stats, 11, mineRow ? UiTheme.Score : UiTheme.Muted, TextAnchor.MiddleRight);
                BroadcastUi.Place(numbers.rectTransform, 0.52f, 0.08f, 0.98f, 0.92f);
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
                var paper = BroadcastUi.Label(card.transform, "Paper", (string.IsNullOrEmpty(story.Kicker) ? story.Paper : story.Kicker + "  ·  " + story.Paper).ToUpperInvariant(), 13, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(paper.rectTransform, 0.04f, 0.72f, 0.7f, 0.94f);
                var open = BroadcastUi.Label(card.transform, "Open", "ABRIR", 13, UiTheme.Score, TextAnchor.MiddleRight, FontStyle.Bold);
                BroadcastUi.Place(open.rectTransform, 0.7f, 0.72f, 0.96f, 0.94f);
                var headline = BroadcastUi.Label(card.transform, "Headline", story.Headline, 20, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(headline.rectTransform, 0.04f, 0.34f, 0.96f, 0.72f);
                var deck = BroadcastUi.Label(card.transform, "Deck", string.IsNullOrEmpty(story.Published) ? story.Deck : story.Published, 14, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Place(deck.rectTransform, 0.04f, 0.08f, 0.96f, 0.34f);
            }
        }

        void FillBoard()
        {
            Clear(_boardContent);
            if (_boardContent == null)
                return;
            var digest = BroadcastUi.Panel(_boardContent, "Digest", new Color(0.1f, 0.28f, 0.34f, 0.92f));
            BroadcastUi.Preferred(digest, 150);
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
                    var weekLabel = BroadcastUi.Label(_boardContent, "Week", "SEMANA DE " + start.ToString("d 'de' MMMM", CultureInfo.GetCultureInfo("pt-PT")).ToUpperInvariant(), 15, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                    BroadcastUi.Preferred(weekLabel, 32);
                }

                var key = when.ToString("yyyy-MM-dd");
                if (dayKey != key)
                {
                    dayKey = key;
                    var dayLabel = BroadcastUi.Label(_boardContent, "DayHead", when.ToString("dddd, d 'de' MMMM", CultureInfo.GetCultureInfo("pt-PT")), 14, UiTheme.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
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

            return "Desde " + start.ToString("d 'de' MMMM", CultureInfo.GetCultureInfo("pt-PT")) + " ficaram " + count + " notas no quadro. Tensão do balneário: " + _career.Club.DressingRoomTension + ". Próximo jogo: " + NextMatchText(_career) + ".";
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
            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var on = option == duty;
                var y = 0.66f - i * 0.12f;
                var row = BroadcastUi.Button(_sheet, "Duty" + i, option, () =>
                {
                    player.Duty = option;
                    FillTeam();
                }, on ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), on ? UiTheme.ButtonText : Color.white, 16);
                BroadcastUi.Place(row.GetComponent<RectTransform>(), 0.04f, y, 0.96f, y + 0.1f);
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
                    low.Add(BroadcastUi.ShortName(player.FullName));
            }

            var captain = "sem capitão";
            foreach (var player in Squad(_career.Club))
            {
                if (player.IsCaptain)
                    captain = player.FullName;
            }

            var text = "Capitão: " + captain + ".  Próximo jogo: " + NextMatchText(_career) + ".  Vestiário: " + _career.Club.DressingRoomTension + ".";
            text += "\nLesionados: " + (hurt.Count == 0 ? "nenhum" : string.Join(", ", hurt));
            text += "\nCansados: " + (tired.Count == 0 ? "ninguém acima do limite" : string.Join(", ", tired));
            text += "\nMoral em baixo: " + (low.Count == 0 ? "o grupo aguenta" : string.Join(", ", low));
            return text;
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
                    card.anchorMin = card.anchorMax = new Vector2((col + 1f) / (count + 1f), y);
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
                var name = BroadcastUi.Label(card.transform, "Name", member.FullName.ToUpperInvariant(), 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(name.rectTransform, 0.06f, 0.52f, 0.94f, 0.88f);
                var role = BroadcastUi.Label(card.transform, "Role", PresentationLabels.StaffRole(member.Role), 14, UiTheme.Accent, TextAnchor.MiddleCenter);
                BroadcastUi.Place(role.rectTransform, 0.06f, 0.34f, 0.94f, 0.52f);
                var read = BroadcastUi.Label(card.transform, "Read", StaffReport.Quality(member), 14, UiTheme.Muted, TextAnchor.UpperCenter);
                BroadcastUi.Place(read.rectTransform, 0.08f, 0.08f, 0.92f, 0.34f);
            }

            if (_vibe != null)
                _vibe.text = VibeLine();
        }

        RectTransform PersonCard(Transform parent, Player player, Club club, bool showFatigue, int fieldIndex, bool bench, float cardWidth, float cardHeight)
        {
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
            var backNumber = BroadcastUi.Label(card.transform, "BackNumber", player.SquadNumber.ToString(), 26, new Color(shirt.r, shirt.g, shirt.b, 0.55f), TextAnchor.MiddleCenter, FontStyle.Bold);
            backNumber.horizontalOverflow = HorizontalWrapMode.Overflow;
            backNumber.verticalOverflow = VerticalWrapMode.Overflow;
            BroadcastUi.Place(backNumber.rectTransform, 0f, 0.58f, 1f, 0.94f);
            var face = FaceImage(card.transform, player, 16f);
            face.preserveAspect = true;
            var faceRect = face.rectTransform;
            faceRect.anchorMin = new Vector2(0.4f, 0.68f);
            faceRect.anchorMax = new Vector2(0.6f, 0.92f);
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
            BroadcastUi.Place(name.rectTransform, 0.04f, 0.28f, 0.96f, 0.5f);
            var meta = BroadcastUi.Label(card.transform, "Meta", PresentationLabels.Position(player.Position) + "   " + NationCode(player.Nationality) + "   " + PlayerAttributes.Overall(player), 12, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            meta.horizontalOverflow = HorizontalWrapMode.Overflow;
            meta.verticalOverflow = VerticalWrapMode.Overflow;
            BroadcastUi.Place(meta.rectTransform, 0.02f, 0.06f, 0.98f, 0.28f);
            if (showFatigue)
            {
                var coach = StaffReport.Find(_career.Club, StaffRole.FitnessCoach);
                var fatigueTruth = bench ? Mathf.Min(player.Fatigue, 22) : player.Fatigue;
                var tired = StaffReport.Read(coach, player.Id, "cansaco", fatigueTruth, 0, 100);
                var bg = BroadcastUi.Solid(card.transform, "Fatigue", new Color(1f, 1f, 1f, 0.16f));
                BroadcastUi.Place(bg.rectTransform, 0.9f, 0.1f, 0.97f, 0.9f);
                var fill = BroadcastUi.Solid(bg.transform, "Fill", UiTheme.Fitness);
                var fresh = Mathf.Clamp01(1f - tired / 100f);
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = new Vector2(1f, Mathf.Max(0.08f, fresh));
                fill.rectTransform.offsetMin = Vector2.zero;
                fill.rectTransform.offsetMax = Vector2.zero;
                fill.color = fresh >= 0.72f ? UiTheme.Fitness : fresh >= 0.5f ? UiTheme.FitnessLow : UiTheme.FitnessBad;
            }

            return card.rectTransform;
        }

        void FillCalendar()
        {
            Clear(_calendar);
            var rows = new List<Fixture>();
            foreach (var fixture in _career.Fixtures)
            {
                if (fixture.Involves(_career.Coach.ClubId))
                    rows.Add(fixture);
            }

            var culture = CultureInfo.GetCultureInfo("pt-PT");
            for (var i = 0; i < rows.Count; i++)
            {
                var fixture = rows[i];
                var opp = _career.World.Clubs[fixture.OpponentId(_career.Coach.ClubId)];
                var venue = fixture.IsHome(_career.Coach.ClubId) ? "CASA" : "FORA";
                var score = fixture.Played ? fixture.HomeGoals + " - " + fixture.AwayGoals : "vs";
                var userHome = fixture.IsHome(_career.Coach.ClubId);
                var userGoals = userHome ? fixture.HomeGoals : fixture.AwayGoals;
                var oppGoals = userHome ? fixture.AwayGoals : fixture.HomeGoals;
                var mark = "";
                var markColor = UiTheme.Muted;
                if (fixture.Played && userGoals > oppGoals)
                {
                    mark = "V";
                    markColor = new Color(0.25f, 0.78f, 0.38f);
                }
                else if (fixture.Played && userGoals < oppGoals)
                {
                    mark = "D";
                    markColor = new Color(0.86f, 0.22f, 0.22f);
                }
                var card = BroadcastUi.Panel(_calendar, "Fixture" + i, new Color(0.14f, 0.06f, 0.28f, 0.9f), true);
                BroadcastUi.Preferred(card, 104);
                var captured = fixture;
                BroadcastUi.Hit(card, () => OpenFixture(captured));
                var date = BroadcastUi.Label(card.transform, "Date", fixture.Date.ToString("dd MMM", culture).ToUpperInvariant(), 18, UiTheme.Score, TextAnchor.MiddleLeft, FontStyle.Bold);
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
                var note = BroadcastUi.Label(card.transform, "Note", AssistantNote(opp), 14, UiTheme.Muted, TextAnchor.MiddleLeft);
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
            OpenModal(story.Paper, string.IsNullOrEmpty(story.Published) ? story.Deck : story.Published, false);
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

            var highlight = BroadcastUi.Label(_modalBody, "Headline", story.Headline, 22, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            BroadcastUi.SetWrapped(highlight, story.Headline, 48);
            if (!string.IsNullOrEmpty(story.Motm))
            {
                var motm = BroadcastUi.Label(_modalBody, "Motm", "JOGADOR DA PARTIDA   " + story.Motm, 16, UiTheme.Score, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Preferred(motm, 28);
            }

            if (!string.IsNullOrEmpty(story.Quote))
            {
                var quote = BroadcastUi.Panel(_modalBody, "Quote", new Color(0.14f, 0.06f, 0.28f, 0.95f), true);
                BroadcastUi.Preferred(quote, 88);
                var line = BroadcastUi.Label(quote.transform, "Q", "“" + story.Quote + "”", 16, Color.white, TextAnchor.UpperLeft);
                BroadcastUi.Place(line.rectTransform, 0.05f, 0.32f, 0.95f, 0.92f);
                var by = BroadcastUi.Label(quote.transform, "By", "— " + story.QuoteBy, 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(by.rectTransform, 0.05f, 0.06f, 0.95f, 0.32f);
            }

            var body = BroadcastUi.Label(_modalBody, "Body", story.Body, 17, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.SetWrapped(body, story.Body, 220);
            AddHead("Resto da edição");
            foreach (var other in edition)
            {
                if (other.Headline == story.Headline)
                    continue;
                var bar = BroadcastUi.Panel(_modalBody, "Bar", new Color(0.02f, 0.01f, 0.04f, 0.92f));
                BroadcastUi.Preferred(bar, 42);
                var line = BroadcastUi.Label(bar.transform, "Hidden", other.Headline, 14, new Color(0.45f, 0.4f, 0.5f), TextAnchor.MiddleLeft);
                BroadcastUi.Place(line.rectTransform, 0.04f, 0.1f, 0.96f, 0.9f);
            }
        }

        void OpenFixture(Fixture fixture)
        {
            var opp = _career.World.Clubs[fixture.OpponentId(_career.Coach.ClubId)];
            var venue = fixture.IsHome(_career.Coach.ClubId) ? "Casa" : "Fora";
            var scouted = _career.Scouted.Contains(opp.Id);
            OpenModal(opp.Name, venue + "  ·  " + fixture.Date.ToString("dd MMMM") + "  ·  " + opp.Tactics.Formation, scouted == false);
            var crest = CrestImage(_modal.transform.Find("Card"), opp, 56f);
            crest.rectTransform.anchorMin = crest.rectTransform.anchorMax = new Vector2(0.07f, 0.9f);
            AddLine(AssistantNote(opp));
            AddHead("Plantel  ·  " + opp.Tactics.Formation);
            var pitch = BroadcastUi.Panel(_modalBody, "Shape", new Color(0.1f, 0.04f, 0.2f, 0.72f));
            BroadcastUi.Preferred(pitch, 520);
            PlaceShape(pitch.transform, opp, false, 84f, 72f);

            if (scouted)
                return;
            _modalAction.onClick.RemoveAllListeners();
            _modalActionLabel.text = "ANALISAR O PLANTEL  ·  3 DIAS";
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
            _career.Calendar.AdvanceDays(days);
            _career.Scouted.Add(opponent.Id);
            _career.Board.Insert(0, new CoachNote
            {
                Day = _career.Calendar.WeekdayName,
                Hour = _career.Calendar.ClockText,
                Text = "Análise ao " + opponent.ShortName + " fechada. Levou " + days + " dias. O relatório já está no calendário."
            });
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
            _modalLineSize = 16;
            var keeper = player.Position == PlayerPosition.Goalkeeper;
            var reader = StaffReport.Find(_career.Club, keeper ? StaffRole.GoalkeeperCoach : StaffRole.AssistantCoach);
            var fitness = StaffReport.Find(_career.Club, StaffRole.FitnessCoach);
            OpenModal(player.FullName, PresentationLabels.Position(player.Position) + "  ·  " + player.Nationality + "  ·  " + player.Age + " anos  ·  " + (string.IsNullOrEmpty(player.Duty) ? "Equilibrar" : player.Duty) + "\n" + CareerMoments.ClueMood(player), false);
            var cardRoot = _modal.transform.Find("Card");
            var frameGo = new GameObject("Portrait", typeof(RectTransform), typeof(RectMask2D));
            var frame = frameGo.GetComponent<RectTransform>();
            frame.SetParent(cardRoot, false);
            BroadcastUi.Place(frame, 0.02f, 0.72f, 0.22f, 0.975f);
            var face = FaceImage(frame, player, 180f);
            BroadcastUi.Stretch(face.rectTransform);
            face.rectTransform.offsetMin = Vector2.zero;
            face.rectTransform.offsetMax = Vector2.zero;
            BroadcastUi.Place(_modalTitle.rectTransform, 0.24f, 0.9f, 0.78f, 0.98f);
            BroadcastUi.Place(_modalLead.rectTransform, 0.24f, 0.74f, 0.96f, 0.89f);
            var badge = BroadcastUi.Solid(cardRoot, "OvrBadge", Color.white);
            badge.sprite = BroadcastUi.Circle;
            badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(0.9f, 0.91f);
            badge.rectTransform.sizeDelta = new Vector2(78, 78);
            var ovr = BroadcastUi.Label(badge.transform, "Ovr", PlayerAttributes.Overall(player).ToString(), 36, new Color(0.08f, 0.05f, 0.16f), TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Stretch(ovr.rectTransform);
            if (orders)
            {
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
            }

            AddHead("Traços");
            if (player.Traits == null || player.Traits.Count == 0)
                PlayerTraits.Assign(player, player.Id.GetHashCode());
            foreach (var trait in player.Traits)
                AddLine(PlayerTraits.Label(trait) + ". " + PlayerTraits.Effect(trait));
            AddHead("Como o staff o lê");
            var tired = StaffReport.Read(fitness, player.Id, "cansaco", player.Fatigue, 0, 100);
            AddLine((fitness == null ? "O preparador" : fitness.FullName) + " aponta cansaço " + TiredWord(tired) + ". " + StaffReport.Confidence(fitness));
            AddStat("Cansaço", Mathf.RoundToInt(tired / 5f), true);
            AddHead("Físico");
            AddStat("Velocidade", Read(fitness, player, "Velocidade"), false);
            AddStat("Resistência", Read(fitness, player, "Resistência"), false);
            AddStat("Força", Read(fitness, player, "Força"), false);
            if (keeper)
            {
                var gk = StaffReport.Find(_career.Club, StaffRole.GoalkeeperCoach);
                AddHead("Guarda-redes");
                AddStat("Reflexos", Read(gk, player, "Reflexos"), false);
                AddStat("Manejo", Read(gk, player, "Manejo"), false);
            }

            AddHead("Técnico");
            AddStat("Passe", Read(reader, player, "Passe"), false);
            AddStat("Finalização", Read(reader, player, "Finalização"), false);
            AddStat("Drible", Read(reader, player, "Drible"), false);
            AddStat("Desarme", Read(reader, player, "Desarme"), false);
            AddStat("Cruzamento", Read(reader, player, "Cruzamento"), false);
            AddHead("Leitura");
            AddLine(Tendency(player));
            var knownPotential = player.ClubId == _career.Club.Id || _career.ScoutedPlayers.Contains(player.Id);
            AddHead("Potencial");
            AddLine(PlayerTraits.HiddenPotential(player, knownPotential));
            AddHead("Mental");
            AddStat("Posicionamento", Read(reader, player, "Posicionamento"), false);
            AddStat("Frieza", Read(reader, player, "Frieza"), false);
            AddStat("Entrega", Read(reader, player, "Entrega"), false);
            AddStat("Decisão", Read(reader, player, "Decisão"), false);
            AddLine((reader == null ? "O adjunto" : reader.FullName) + " " + StaffReport.Confidence(reader));
            var physio = Member(_career.Club, StaffRole.Physiotherapist);
            if (player.Injury != null && player.Injury.IsInjured)
                AddLine((physio == null ? "O fisioterapeuta" : physio.FullName) + ": " + player.Injury.Description + ", " + player.Injury.DaysRemaining + " dias.");
            else
                AddLine((physio == null ? "O fisioterapeuta" : physio.FullName) + " não vê lesão.");
            var scout = Member(_career.Club, StaffRole.Scout);
            if (_career.ScoutedPlayers.Contains(player.Id))
                AddLine((scout == null ? "O olheiro" : scout.FullName) + " já tem relatório. Lê o overall em " + StaffReport.Read(scout, player.Id, "overall", PlayerAttributes.Overall(player), 40, 94) + ".");
            var note = UserNote(player.Id);
            AddHead("A tua nota");
            AddLine(string.IsNullOrEmpty(note) ? "Ainda não escreveste nada." : note);
            AddHead("Mercado");
            AddLine("Valor estimado: " + Euro(MarketValue(player)) + ".");
            var captured = player;
            var own = player.ClubId == _career.Club.Id;
            if (!own)
            {
                ClaimForeignTargets();
                var wanted = _career.Targets.Contains(player.Id);
                var follow = BroadcastUi.Button(_modalBody, "Target", wanted ? "TIRAR DA MINHA LISTA" : "ADICIONAR À MINHA LISTA", () =>
                {
                    if (_career.Targets.Contains(captured.Id))
                        _career.Targets.Remove(captured.Id);
                    else
                        _career.Targets.Add(captured.Id);
                    _career.Listed.Remove(captured.Id);
                    _career.Loans.Remove(captured.Id);
                    OpenPlayer(captured);
                }, wanted ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), wanted ? UiTheme.ButtonText : Color.white, 16);
                BroadcastUi.Preferred(follow.GetComponent<RectTransform>(), 42);
                return;
            }

            var onList = _career.Listed.Contains(player.Id);
            var onLoan = _career.Loans.Contains(player.Id);
            var transfer = BroadcastUi.Button(_modalBody, "Transfer", onList ? "TIRAR DA LISTA DE VENDA" : "PÔR À VENDA", () =>
            {
                if (_career.Listed.Contains(captured.Id))
                    _career.Listed.Remove(captured.Id);
                else
                {
                    _career.Listed.Add(captured.Id);
                    _career.Loans.Remove(captured.Id);
                }
                OpenPlayer(captured);
            }, onList ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), onList ? UiTheme.ButtonText : Color.white, 16);
            BroadcastUi.Preferred(transfer.GetComponent<RectTransform>(), 42);
            var loan = BroadcastUi.Button(_modalBody, "Loan", onLoan ? "TIRAR DA LISTA DE EMPRÉSTIMOS" : "LISTA DE EMPRÉSTIMOS", () =>
            {
                if (_career.Loans.Contains(captured.Id))
                    _career.Loans.Remove(captured.Id);
                else
                {
                    _career.Loans.Add(captured.Id);
                    _career.Listed.Remove(captured.Id);
                }
                OpenPlayer(captured);
            }, onLoan ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), onLoan ? UiTheme.ButtonText : Color.white, 16);
            BroadcastUi.Preferred(loan.GetComponent<RectTransform>(), 42);
        }

        void OpenStaff(StaffMember member)
        {
            _offer = null;
            _modalLineSize = 20;
            OpenModal(member.FullName, PresentationLabels.StaffRole(member.Role), true);
            AddLine(StaffReport.Quality(member) + " " + StaffReport.Confidence(member));
            AddLine("Pedir a saída não despede ninguém. Quem termina o contrato é a direção.");
            AddLine(VibeLine());
            _modalAction.onClick.RemoveAllListeners();
            _modalActionLabel.text = "REUNIÃO COM O PRESIDENTE";
            var captured = member;
            _modalAction.onClick.AddListener(() => OpenMeeting(captured));
        }

        void OpenMeeting(StaffMember current)
        {
            _offer = StaffReport.Offer(_career.Club, current.Role);
            _modalLineSize = 20;
            OpenModal("Relatório à direção", PresentationLabels.StaffRole(current.Role), true);
            AddLine("O texto já está escrito, como se o tivesses ditado ao adjunto. A direção só lê depois da tua assinatura.");
            AddLine(ReportText(current, _offer));
            _modalAction.onClick.RemoveAllListeners();
            _modalActionLabel.text = "ASSINAR E ENTREGAR";
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
                Text = "Assinei o pedido de saída de " + current.FullName + ". A decisão fica com o presidente."
            });
            if (!PresidentAgrees(current, _offer))
            {
                _career.Club.DressingRoomTension = Mathf.Clamp(_career.Club.DressingRoomTension + 4, 0, 100);
                OpenModal("A direção não autoriza", current.FullName, false);
                AddLine("O presidente ouviu o pedido e recusou a rescisão. O contrato de " + current.FullName + " mantém-se. Quem tem autoridade para o terminar é a direção, não o treinador.");
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

        void OpenModal(string title, string lead, bool action)
        {
            if (_modalLineSize <= 0)
                _modalLineSize = 16;
            Clear(_modalBody);
            var oldFace = _modal.transform.Find("Card/Face");
            if (oldFace != null)
                Destroy(oldFace.gameObject);
            var oldPortrait = _modal.transform.Find("Card/Portrait");
            if (oldPortrait != null)
                Destroy(oldPortrait.gameObject);
            var oldOvr = _modal.transform.Find("Card/Ovr");
            if (oldOvr != null)
                Destroy(oldOvr.gameObject);
            var oldBadge = _modal.transform.Find("Card/OvrBadge");
            if (oldBadge != null)
                Destroy(oldBadge.gameObject);
            BroadcastUi.Place(_modalTitle.rectTransform, 0.08f, 0.88f, 0.96f, 0.97f);
            BroadcastUi.Place(_modalLead.rectTransform, 0.05f, 0.78f, 0.95f, 0.88f);
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
            var truth = player.Attributes == null ? 10 : player.Attributes.Value(stat);
            return StaffReport.Read(reader, player.Id, stat, truth, 1, 20);
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
                return "O adjunto ainda não fechou uma leitura desta equipa.";

            var form = best.Form;
            var pressing = opponent.Tactics.Pressing ?? "";
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

        static string MatchLabel(CareerState career, Fixture fixture)
        {
            var opp = career.World.Clubs[fixture.OpponentId(career.Coach.ClubId)];
            var venue = fixture.IsHome(career.Coach.ClubId) ? "Casa" : "Fora";
            return venue + " vs " + opp.Name;
        }

        static List<Player> Squad(Club club)
        {
            var list = new List<Player>();
            var world = GameRoot.Instance.Session.Career.World;
            foreach (var id in club.PlayerIds)
            {
                if (world.Players.TryGetValue(id, out var player))
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
            image.sprite = FaceBook.For(player) ?? PortraitStudio.For(player == null ? "anon" : player.Id);
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
                Destroy(content.GetChild(i).gameObject);
        }
    }
}
