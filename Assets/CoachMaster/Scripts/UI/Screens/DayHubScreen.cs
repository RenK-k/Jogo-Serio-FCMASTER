using System.Collections.Generic;
using CoachMaster.Career;
using CoachMaster.Domain;
using CoachMaster.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CoachMaster.UI.Screens
{
    public sealed class DayHubScreen : GameScreen
    {
        protected override void Build(ScreenRouter router)
        {
            var career = router.Session.Career;
            var club = career.Club;
            UiFactory.FullScreen(Root.transform, "Background", UiTheme.Canvas);

            var page = UiFactory.Column(Root.transform, "CareerPage", 0, new RectOffset(44, 44, 30, 30), true, true);
            var header = UiFactory.Panel(page.transform, "Header", UiTheme.Header);
            UiFactory.LayoutElement(header.GetComponent<Image>(), 100);
            var headerRow = UiFactory.Row(header, "HeaderRow", 16, new RectOffset(16, 24, 10, 10), true);
            var kit = KitView.Create(headerRow.transform, club, new Vector2(64, 80), false);
            var kitLe = kit.gameObject.AddComponent<LayoutElement>();
            kitLe.minWidth = 64;
            kitLe.preferredWidth = 64;
            kitLe.flexibleWidth = 0;
            var headerColumn = UiFactory.Column(headerRow.transform, "HeaderContent", 4, new RectOffset(0, 0, 8, 8), false, true);
            var headerColLe = headerColumn.gameObject.AddComponent<LayoutElement>();
            headerColLe.flexibleWidth = 1;
            var clubName = UiFactory.Label(headerColumn.transform, "ClubName", club.Name.ToUpperInvariant(), 28, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.LayoutElement(clubName, 34);
            var details = UiFactory.Label(headerColumn.transform, "CareerDetails",
                $"{career.Calendar.LongDateText}     ·     {career.League.Name} {career.Calendar.SeasonLabel}     ·     Mister {career.Coach.Name}",
                16, UiTheme.Muted, TextAnchor.MiddleLeft);
            UiFactory.LayoutElement(details, 24);

            var tabsRow = new GameObject("PageTabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabsRow.transform.SetParent(page.transform, false);
            UiFactory.LayoutElement(tabsRow.GetComponent<RectTransform>(), 36);
            var tabLayout = tabsRow.GetComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 7;
            tabLayout.padding = new RectOffset(0, 0, 0, 0);
            tabLayout.childAlignment = TextAnchor.MiddleLeft;
            tabLayout.childControlWidth = false;
            tabLayout.childControlHeight = false;
            tabLayout.childForceExpandWidth = false;
            tabLayout.childForceExpandHeight = false;

            var contentHost = new GameObject("TabContent", typeof(RectTransform));
            contentHost.transform.SetParent(page.transform, false);
            var contentLe = contentHost.AddComponent<LayoutElement>();
            contentLe.minHeight = 360;
            contentLe.flexibleHeight = 1;
            contentLe.preferredHeight = 720;

            var home = BuildHome(contentHost.transform, career, club);
            var news = BuildNews(contentHost.transform, career, club);
            var team = BuildTeam(contentHost.transform, router);
            var staff = BuildStaff(contentHost.transform, club);
            var tabButtons = new List<Button>();
            tabButtons.Add(CreateTab(tabsRow.transform, "Início", 150));
            tabButtons.Add(CreateTab(tabsRow.transform, "Notícias", 150));
            tabButtons.Add(CreateTab(tabsRow.transform, "Equipa", 150));
            tabButtons.Add(CreateTab(tabsRow.transform, "Staff", 150));
            var navigator = Root.AddComponent<CareerTabNavigator>();
            navigator.Configure(tabButtons, new[] { home.gameObject, news.gameObject, team.gameObject, staff.gameObject });

        }

        static RectTransform BuildHome(Transform parent, CareerState career, Club club)
        {
            var panel = UiFactory.Panel(parent, "HomePage", UiTheme.Panel);
            UiFactory.Stretch(panel);
            AddGoldFrame(panel);
            var column = UiFactory.Column(panel, "HomeContent", 12, new RectOffset(24, 24, 20, 20), true, true);

            var heading = UiFactory.Label(column.transform, "Welcome", "VISÃO GERAL", 24, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.LayoutElement(heading, 36);

            var scroll = UiFactory.Scroll(column.transform, out var content);
            var scrollLe = scroll.gameObject.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1;
            scrollLe.minHeight = 180;
            AddInfoCard(content, "O clube", $"{club.PublicTeamState}\nIdentidade atual: {club.Tactics.Formation} · {club.Tactics.Summary}", 100);
            AddInfoCard(content, "Calendário", $"{career.Calendar.LongDateText}\nÉ o primeiro dia da tua carreira. Ainda não existem jogos registados no calendário.", 92);
            var staffHeading = UiFactory.Label(content, "StaffHeading", "O que diz o staff", 21, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.LayoutElement(staffHeading, 32);
            foreach (var member in club.Staff)
                AddStaffCard(content, member, 76);
            return panel;
        }

        static RectTransform BuildNews(Transform parent, CareerState career, Club club)
        {
            var panel = UiFactory.Panel(parent, "NewsPage", UiTheme.Panel);
            UiFactory.Stretch(panel);
            AddGoldFrame(panel);
            var column = UiFactory.Column(panel, "NewsContent", 10, new RectOffset(24, 24, 18, 18), true, true);
            var heading = UiFactory.Label(column.transform, "NewsHeading", "NOTÍCIAS DO CLUBE", 24, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.LayoutElement(heading, 34);
            var scroll = UiFactory.Scroll(column.transform, out var content);
            var scrollLe = scroll.gameObject.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1;
            scrollLe.minHeight = 180;

            AddInfoCard(content, "Novo treinador assume o cargo",
                $"{career.Calendar.LongDateText}: {career.Coach.Name} inicia o trabalho no {club.Name}. O primeiro dia é dedicado a conhecer o clube e o plantel.",
                92);
            AddInfoCard(content, "O desafio da época",
                $"{club.Name} compete na {career.League.Name}. {club.PublicReputation}",
                86);
            AddInfoCard(content, "Objetivos da direção",
                string.Join("\n", club.BoardObjectives),
                100);
            AddInfoCard(content, "A saída do antigo treinador",
                club.PublicCoachVacancyNote,
                86);
            return panel;
        }

        static RectTransform BuildTeam(Transform parent, ScreenRouter router)
        {
            var career = router.Session.Career;
            var panel = UiFactory.Panel(parent, "TeamPage", UiTheme.Panel);
            UiFactory.Stretch(panel);
            AddGoldFrame(panel);
            var column = UiFactory.Column(panel, "TeamContent", 8, new RectOffset(18, 18, 16, 16), true, true);
            var heading = UiFactory.Label(column.transform, "TeamHeading", "PLANTEL · VISTA TÁTICA", 24, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.LayoutElement(heading, 34);
            var teamArea = new GameObject("TeamArea", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            teamArea.transform.SetParent(column.transform, false);
            var areaLayout = teamArea.GetComponent<HorizontalLayoutGroup>();
            areaLayout.spacing = 14;
            areaLayout.childAlignment = TextAnchor.UpperCenter;
            areaLayout.childControlWidth = true;
            areaLayout.childControlHeight = true;
            areaLayout.childForceExpandWidth = false;
            areaLayout.childForceExpandHeight = true;
            var areaElement = teamArea.AddComponent<LayoutElement>();
            areaElement.minHeight = 240;
            areaElement.flexibleHeight = 1;

            var field = UiFactory.Panel(teamArea.transform, "Pitch", new Color(0.055f, 0.24f, 0.17f, 1f));
            var fieldElement = field.gameObject.AddComponent<LayoutElement>();
            fieldElement.minWidth = 280;
            fieldElement.flexibleWidth = 1;
            fieldElement.flexibleHeight = 1;
            AddPitchMarkings(field.transform);
            AddTacticsSidebar(teamArea.transform, career.Club);
            ColorUtility.TryParseHtmlString(career.Club.JerseyPrimaryHex, out var jerseyPrimary);
            ColorUtility.TryParseHtmlString(career.Club.JerseySecondaryHex, out var jerseySecondary);
            var samePositionCount = new Dictionary<PlayerPosition, int>();
            var samePositionIndex = new Dictionary<PlayerPosition, int>();
            foreach (var id in career.Club.PlayerIds)
            {
                var player = career.World.Players[id];
                samePositionCount[player.Position] = samePositionCount.TryGetValue(player.Position, out var count) ? count + 1 : 1;
            }
            foreach (var id in career.Club.PlayerIds)
            {
                var player = career.World.Players[id];
                samePositionIndex.TryGetValue(player.Position, out var index);
                samePositionIndex[player.Position] = index + 1;
                var count = samePositionCount[player.Position];
                var spread = count <= 1 ? 0f : (index - (count - 1) * 0.5f) * 0.085f;
                var position = PitchPosition(player.Position);
                position.x = Mathf.Clamp(position.x + spread, 0.08f, 0.92f);
                AddPlayerMarker(field.transform, player, position, jerseyPrimary, jerseySecondary);
            }
            var note = UiFactory.Label(column.transform, "TeamNote", "Cada camisola representa um jogador do plantel. C identifica o capitão.", 14, UiTheme.Muted, TextAnchor.MiddleLeft);
            UiFactory.LayoutElement(note, 22);
            return panel;
        }

        static RectTransform BuildStaff(Transform parent, Club club)
        {
            var panel = UiFactory.Panel(parent, "StaffPage", UiTheme.Panel);
            UiFactory.Stretch(panel);
            AddGoldFrame(panel);
            var column = UiFactory.Column(panel, "StaffContent", 12, new RectOffset(24, 24, 20, 20), true, true);
            var heading = UiFactory.Label(column.transform, "StaffHeading", "EQUIPA TÉCNICA", 24, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.LayoutElement(heading, 38);
            var intro = UiFactory.Label(column.transform, "StaffIntro", "Conhece as pessoas que vão trabalhar contigo no clube.", 16, UiTheme.Muted, TextAnchor.MiddleLeft);
            UiFactory.LayoutElement(intro, 28);
            var scroll = UiFactory.Scroll(column.transform, out var content);
            var scrollLe = scroll.gameObject.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1;
            scrollLe.minHeight = 180;
            foreach (var member in club.Staff)
                AddStaffCard(content, member, 100);
            return panel;
        }

        static void AddInfoCard(Transform parent, string title, string body, float height)
        {
            var card = UiFactory.Panel(parent, title, UiTheme.PanelAlt);
            UiFactory.LayoutElement(card.GetComponent<Image>(), height);
            var column = UiFactory.Column(card, "CardContent", 6, new RectOffset(16, 16, 12, 12), true, true);
            var heading = UiFactory.Label(column.transform, "Heading", title, 19, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.LayoutElement(heading, 24);
            var text = UiFactory.Label(column.transform, "Body", body, 16, UiTheme.Muted, TextAnchor.UpperLeft);
            UiFactory.LayoutElement(text, height - 42);
        }

        static void AddStaffCard(Transform parent, StaffMember member, float height)
        {
            var card = UiFactory.Panel(parent, "Staff_" + member.Id, UiTheme.PanelAlt);
            UiFactory.LayoutElement(card.GetComponent<Image>(), height);
            var column = UiFactory.Column(card, "StaffContent", 4, new RectOffset(16, 16, 10, 10), true, true);
            var heading = UiFactory.Label(column.transform, "NameRole", $"{member.FullName}  ·  {PresentationLabels.StaffRole(member.Role)}", 17, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.LayoutElement(heading, 24);
            var quote = UiFactory.Label(column.transform, "Comment", $"“{StaffComment(member.Role)}”", 15, UiTheme.Text, TextAnchor.UpperLeft);
            UiFactory.LayoutElement(quote, height - 38);
        }

        static string StaffComment(StaffRole role)
        {
            switch (role)
            {
                case StaffRole.AssistantCoach: return "Vou ajudar-te a conhecer o grupo e a preparar o trabalho da equipa.";
                case StaffRole.GoalkeeperCoach: return "Vou acompanhar os guarda-redes e partilhar contigo o que observar no treino.";
                case StaffRole.FitnessCoach: return "Quero observar como o plantel responde ao trabalho antes de aumentar a carga.";
                case StaffRole.Physiotherapist: return "Se alguém se queixar ou parecer limitado, falo contigo antes de avançar.";
                default: return "Estou aqui para ajudar a equipa técnica.";
            }
        }

        static void AddPlayerMarker(Transform field, Player player, Vector2 position, Color primary, Color secondary)
        {
            var marker = new GameObject("Player_" + player.SquadNumber, typeof(RectTransform));
            var rect = marker.GetComponent<RectTransform>();
            rect.SetParent(field, false);
            rect.anchorMin = position;
            rect.anchorMax = position;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(112, 66);
            var jerseyObject = new GameObject("Jersey", typeof(RectTransform), typeof(CanvasRenderer), typeof(JerseyGraphic));
            jerseyObject.transform.SetParent(marker.transform, false);
            var jersey = jerseyObject.GetComponent<JerseyGraphic>();
            jersey.color = primary;
            jersey.raycastTarget = false;
            var jerseyRect = jersey.rectTransform;
            jerseyRect.anchorMin = new Vector2(0.5f, 1f);
            jerseyRect.anchorMax = new Vector2(0.5f, 1f);
            jerseyRect.pivot = new Vector2(0.5f, 1f);
            jerseyRect.anchoredPosition = new Vector2(0, -2);
            jerseyRect.sizeDelta = new Vector2(38, 34);
            AddJerseyStripe(jerseyObject.transform, "StripeLeft", secondary, -4);
            AddJerseyStripe(jerseyObject.transform, "StripeRight", secondary, 4);
            var number = UiFactory.Label(marker.transform, "ShirtNumber", player.SquadNumber.ToString(), 12, UiTheme.ButtonText, TextAnchor.MiddleCenter, FontStyle.Bold);
            number.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            number.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            number.rectTransform.pivot = new Vector2(0.5f, 1f);
            number.rectTransform.anchoredPosition = new Vector2(0, -10);
            number.rectTransform.sizeDelta = new Vector2(20, 18);
            var name = UiFactory.Label(marker.transform, "PlayerName",
                player.IsCaptain ? $"{player.FullName} <color=#FFC20E><b>C</b></color>" : player.FullName,
                12, UiTheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            name.supportRichText = true;
            name.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            name.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            name.rectTransform.pivot = new Vector2(0.5f, 0f);
            name.rectTransform.anchoredPosition = Vector2.zero;
            name.rectTransform.sizeDelta = new Vector2(112, 25);
        }

        static void AddJerseyStripe(Transform jersey, string name, Color color, float x)
        {
            var stripe = new GameObject(name, typeof(RectTransform), typeof(Image));
            stripe.transform.SetParent(jersey, false);
            var rect = stripe.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.2f);
            rect.anchorMax = new Vector2(0.5f, 0.73f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0);
            rect.sizeDelta = new Vector2(2, 0);
            var image = stripe.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        static Vector2 PitchPosition(PlayerPosition position)
        {
            switch (position)
            {
                case PlayerPosition.Goalkeeper: return new Vector2(0.5f, 0.10f);
                case PlayerPosition.RightBack: return new Vector2(0.82f, 0.27f);
                case PlayerPosition.CentreBack: return new Vector2(0.5f, 0.28f);
                case PlayerPosition.LeftBack: return new Vector2(0.18f, 0.27f);
                case PlayerPosition.DefensiveMidfielder: return new Vector2(0.5f, 0.43f);
                case PlayerPosition.CentralMidfielder: return new Vector2(0.5f, 0.53f);
                case PlayerPosition.AttackingMidfielder: return new Vector2(0.5f, 0.64f);
                case PlayerPosition.RightWinger: return new Vector2(0.78f, 0.76f);
                case PlayerPosition.LeftWinger: return new Vector2(0.22f, 0.76f);
                case PlayerPosition.Striker: return new Vector2(0.5f, 0.87f);
                default: return new Vector2(0.5f, 0.5f);
            }
        }

        static void AddPitchMarkings(Transform field)
        {
            var frame = field.gameObject.AddComponent<Outline>();
            frame.effectColor = new Color(0.82f, 0.91f, 0.78f, 0.75f);
            frame.effectDistance = new Vector2(2, -2);
            frame.useGraphicAlpha = true;
            AddFieldLine(field, "Halfway", new Vector2(0, 0.5f), new Vector2(1, 0.5f), 2);
            AddFieldLine(field, "LeftBoundary", new Vector2(0.035f, 0.04f), new Vector2(0.035f, 0.96f), 2);
            AddFieldLine(field, "RightBoundary", new Vector2(0.965f, 0.04f), new Vector2(0.965f, 0.96f), 2);
            AddFieldLine(field, "TopBoundary", new Vector2(0.035f, 0.96f), new Vector2(0.965f, 0.96f), 2);
            AddFieldLine(field, "BottomBoundary", new Vector2(0.035f, 0.04f), new Vector2(0.965f, 0.04f), 2);
            AddFieldLine(field, "CentreCircleTop", new Vector2(0.43f, 0.56f), new Vector2(0.57f, 0.56f), 1);
            AddFieldLine(field, "CentreCircleBottom", new Vector2(0.43f, 0.44f), new Vector2(0.57f, 0.44f), 1);
            AddFieldLine(field, "GoalBoxTop", new Vector2(0.36f, 0.96f), new Vector2(0.64f, 0.96f), 2);
            AddFieldLine(field, "GoalBoxBottom", new Vector2(0.36f, 0.04f), new Vector2(0.64f, 0.04f), 2);
        }

        static void AddFieldLine(Transform parent, string name, Vector2 from, Vector2 to, float thickness)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = from;
            rect.anchorMax = to;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.sizeDelta = Mathf.Approximately(from.x, to.x) ? new Vector2(thickness, 0) : new Vector2(0, thickness);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.83f, 0.91f, 0.80f, 0.65f);
            image.raycastTarget = false;
        }

        static void AddGoldFrame(Transform panel)
        {
            var frame = panel.gameObject.AddComponent<Outline>();
            frame.effectColor = UiTheme.Accent;
            frame.effectDistance = new Vector2(2, -2);
            frame.useGraphicAlpha = true;
        }

        static void AddTacticsSidebar(Transform parent, Club club)
        {
            var sidebar = UiFactory.Panel(parent, "TacticsSidebar", UiTheme.PanelAlt);
            var size = sidebar.gameObject.AddComponent<LayoutElement>();
            size.minWidth = 300;
            size.preferredWidth = 330;
            size.flexibleWidth = 0;
            size.flexibleHeight = 1;

            var column = UiFactory.Column(sidebar, "TacticsContent", 8, new RectOffset(16, 16, 16, 16), true, true);
            var heading = UiFactory.Label(column.transform, "TacticsHeading", "Tática atual", 21, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.LayoutElement(heading, 30);
            var scroll = UiFactory.Scroll(column.transform, out var content);
            var scrollElement = scroll.gameObject.AddComponent<LayoutElement>();
            scrollElement.flexibleHeight = 1;
            scrollElement.minHeight = 180;

            var tactics = club.Tactics;
            var formation = AddTacticValue(content, "Formação", tactics.Formation);
            var pressing = AddTacticValue(content, "Pressão", tactics.Pressing);
            var buildUp = AddTacticValue(content, "Construção", tactics.BuildUp);
            var fullBacks = AddTacticValue(content, "Laterais", tactics.FullBacks);
            var defensiveLine = AddTacticValue(content, "Linha defensiva", tactics.DefensiveLine);

            var editor = UiFactory.Column(content, "TacticsEditor", 5, new RectOffset(0, 0, 0, 0), controlChildHeight: true);
            UiFactory.LayoutElement(editor, 205);
            var refreshSummary = (System.Action)(() => tactics.Summary = $"{tactics.Formation} · {tactics.Pressing} · {tactics.BuildUp} · {tactics.FullBacks} · {tactics.DefensiveLine}");
            AddTacticOption(editor.transform, "Formação", new[] { "4-3-3", "4-2-3-1", "4-4-2", "3-4-3", "5-4-1", "4-1-4-1" }, () => tactics.Formation, value => tactics.Formation = value, formation, refreshSummary);
            AddTacticOption(editor.transform, "Pressão", new[] { "Pressão alta", "Pressão média", "Pressão baixa" }, () => tactics.Pressing, value => tactics.Pressing = value, pressing, refreshSummary);
            AddTacticOption(editor.transform, "Construção", new[] { "Construção curta", "Jogo misto", "Construção longa" }, () => tactics.BuildUp, value => tactics.BuildUp = value, buildUp, refreshSummary);
            AddTacticOption(editor.transform, "Laterais", new[] { "Laterais ofensivos", "Laterais equilibrados", "Laterais defensivos" }, () => tactics.FullBacks, value => tactics.FullBacks = value, fullBacks, refreshSummary);
            AddTacticOption(editor.transform, "Linha", new[] { "Linha defensiva alta", "Linha defensiva média", "Linha defensiva baixa" }, () => tactics.DefensiveLine, value => tactics.DefensiveLine = value, defensiveLine, refreshSummary);

            var toggle = UiFactory.Button(content, "Alterar tática", () => editor.gameObject.SetActive(!editor.gameObject.activeSelf), UiTheme.Accent);
            UiFactory.LayoutElement(toggle, 42);
            editor.gameObject.SetActive(false);
        }

        static Text AddTacticValue(Transform parent, string title, string value)
        {
            var label = UiFactory.Label(parent, title, $"{title}: {value}", 15, UiTheme.Text, TextAnchor.MiddleLeft);
            UiFactory.LayoutElement(label, 25);
            return label;
        }

        static void AddTacticOption(Transform parent, string title, string[] options, System.Func<string> getValue,
            System.Action<string> setValue, Text currentValue, System.Action refreshSummary)
        {
            var button = UiFactory.Button(parent, title, () => { }, UiTheme.Panel);
            UiFactory.LayoutElement(button, 34);
            var label = button.GetComponentInChildren<Text>();
            label.fontSize = 13;
            label.alignment = TextAnchor.MiddleLeft;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                var index = System.Array.IndexOf(options, getValue());
                var next = options[(index + 1 + options.Length) % options.Length];
                setValue(next);
                refreshSummary();
                label.text = $"{title}: {next}";
                currentValue.text = $"{title}: {next}";
            });
            label.text = $"{title}: {getValue()}  ›";
        }

        static Button CreateTab(Transform parent, string label, float width)
        {
            var button = UiFactory.Button(parent, label, () => { }, UiTheme.PanelAlt);
            var layout = button.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = width;
            layout.preferredWidth = width;
            layout.minHeight = 32;
            layout.preferredHeight = 32;
            button.GetComponentInChildren<Text>().fontSize = 16;
            return button;
        }
    }

    public sealed class CareerTabNavigator : MonoBehaviour
    {
        static readonly Color ActiveTab = UiTheme.Accent;
        static readonly Color InactiveTab = UiTheme.PanelAlt;
        readonly List<Button> _buttons = new List<Button>();
        GameObject[] _pages;
        int _selected;

        public void Configure(IReadOnlyList<Button> buttons, GameObject[] pages)
        {
            _buttons.Clear();
            _buttons.AddRange(buttons);
            _pages = pages;
            for (var i = 0; i < _buttons.Count; i++)
            {
                var index = i;
                _buttons[i].onClick.RemoveAllListeners();
                _buttons[i].onClick.AddListener(() => Select(index));
            }
            Select(0);
        }

        void Update()
        {
            if (_pages == null || _pages.Length < 2 || Keyboard.current == null)
                return;
            if (Keyboard.current.xKey.wasPressedThisFrame)
                Select((_selected - 1 + _pages.Length) % _pages.Length);
            else if (Keyboard.current.cKey.wasPressedThisFrame)
                Select((_selected + 1) % _pages.Length);
        }

        void Select(int index)
        {
            if (_pages == null || _pages.Length == 0)
                return;
            _selected = (index + _pages.Length) % _pages.Length;
            for (var i = 0; i < _pages.Length; i++)
                _pages[i].SetActive(i == _selected);
            for (var i = 0; i < _buttons.Count; i++)
            {
                var selected = i == _selected;
                var colors = _buttons[i].colors;
                colors.normalColor = selected ? ActiveTab : InactiveTab;
                colors.highlightedColor = Color.Lerp(colors.normalColor, Color.white, 0.12f);
                _buttons[i].colors = colors;
                var label = _buttons[i].GetComponentInChildren<Text>();
                if (label != null)
                    label.color = selected ? UiTheme.ButtonText : UiTheme.Text;
            }
        }
    }
}
