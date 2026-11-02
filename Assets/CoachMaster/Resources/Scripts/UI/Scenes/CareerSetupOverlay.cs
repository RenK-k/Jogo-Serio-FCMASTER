using System;
using System.Collections.Generic;
using CoachMaster.App;
using CoachMaster.Career;
using CoachMaster.Data;
using CoachMaster.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Scenes
{
    public static class CareerSetupOverlay
    {
        static readonly string[] Speeds = { "0.5×", "1×", "2×", "4×", "8×" };

        public static void Open(Transform parent, GameSession session, Action startCareer)
        {
            if (parent == null || session == null) return;
            var previous = parent.Find("CareerSetupOverlay");
            if (previous != null) UnityEngine.Object.Destroy(previous.gameObject);
            var overlay = new GameObject("CareerSetupOverlay", typeof(RectTransform), typeof(Image));
            var root = overlay.GetComponent<RectTransform>();
            root.SetParent(parent, false);
            BroadcastUi.Stretch(root);
            var shade = overlay.GetComponent<Image>();
            shade.color = new Color(0.015f, 0.02f, 0.06f, 0.8f);
            shade.raycastTarget = true;
            var panel = BroadcastUi.Panel(root, "SetupPanel", new Color(0.055f, 0.09f, 0.17f, 1f), true);
            BroadcastUi.Place(panel.rectTransform, 0.12f, 0.08f, 0.88f, 0.92f);
            BroadcastUi.ApplySandPaper(panel, 0.035f);

            var english = LocalizationManager.Current == GameLanguage.English;
            var title = BroadcastUi.Label(panel.transform, "Title", english ? "CAREER SETUP" : "CONFIGURAR CARREIRA", 26, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.045f, 0.89f, 0.95f, 0.98f);
            var intro = BroadcastUi.Label(panel.transform, "Intro", english
                ? "Choose the simulation settings for this save. You can change these later in Settings."
                : "Escolhe as definições da simulação para esta carreira. Podes alterá-las mais tarde nas Definições.", 15, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(intro.rectTransform, 0.045f, 0.82f, 0.95f, 0.9f);

            var left = 0.045f;
            var right = 0.515f;
            var columnEnd = 0.485f;
            var rowY = new[] { 0.69f, 0.59f, 0.49f, 0.39f, 0.29f, 0.19f };
            GameObject activeHelp = null;
            void ShowSettingHelp(string heading, string description)
            {
                if (activeHelp != null) UnityEngine.Object.Destroy(activeHelp);
                var card = BroadcastUi.Panel(panel.transform, "SettingHelp", new Color(0.035f, 0.045f, 0.09f, 1f), true);
                activeHelp = card.gameObject;
                BroadcastUi.Place(card.rectTransform, 0.25f, 0.34f, 0.75f, 0.65f);
                var helpTitle = BroadcastUi.Label(card.transform, "HelpTitle", heading, 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(helpTitle.rectTransform, 0.05f, 0.76f, 0.88f, 0.96f);
                var helpBody = BroadcastUi.Label(card.transform, "HelpBody", description, 14, Color.white, TextAnchor.UpperLeft);
                BroadcastUi.Place(helpBody.rectTransform, 0.05f, 0.25f, 0.95f, 0.73f);
                var closeHelp = BroadcastUi.Button(card.transform, "CloseHelp", english ? "GOT IT" : "PERCEBI", () =>
                {
                    if (activeHelp != null) UnityEngine.Object.Destroy(activeHelp);
                    activeHelp = null;
                }, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 11);
                BroadcastUi.Place(closeHelp.GetComponent<RectTransform>(), 0.68f, 0.05f, 0.95f, 0.21f);
            }
            AddSetting(panel.transform, english ? "MATCH SPEED" : "VELOCIDADE DE JOGO",
                () => Speeds[Mathf.Clamp(session.Draft.MatchSpeedIndex, 0, Speeds.Length - 1)],
                () => session.Draft.MatchSpeedIndex = (session.Draft.MatchSpeedIndex + 1) % Speeds.Length, left, columnEnd, rowY[0],
                english ? "Changes how quickly match time and match events progress. 0.5× is the slowest setting; 8× is the fastest." : "Altera a rapidez do relógio e dos acontecimentos durante o jogo. 0,5× é a velocidade mais lenta; 8× é a mais rápida.", ShowSettingHelp);
            AddSetting(panel.transform, english ? "INJURY FREQUENCY" : "FREQUÊNCIA DE LESÕES",
                () => (english ? new[] { "Low", "Normal", "High" } : new[] { "Baixa", "Normal", "Alta" })[Mathf.Clamp(session.Draft.InjuryFrequency, 0, 2)],
                () => session.Draft.InjuryFrequency = (session.Draft.InjuryFrequency + 1) % 3, right, 0.955f, rowY[0],
                english ? "Sets how often players can suffer injuries during matches." : "Define a frequência com que os jogadores podem sofrer lesões durante os jogos.", ShowSettingHelp);
            AddSetting(panel.transform, english ? "REFEREE STRICTNESS" : "CRITÉRIO DA ARBITRAGEM",
                () => (english ? new[] { "Lenient", "Normal", "Strict" } : new[] { "Permissivo", "Normal", "Rigoroso" })[Mathf.Clamp(session.Draft.RefereeStrictness, 0, 2)],
                () => session.Draft.RefereeStrictness = (session.Draft.RefereeStrictness + 1) % 3, left, columnEnd, rowY[1],
                english ? "Changes how readily the referee calls fouls and gives cards." : "Altera a facilidade com que o árbitro assinala faltas e mostra cartões.", ShowSettingHelp);
            AddSetting(panel.transform, english ? "REALISM PRESET" : "PREDEFINIÇÃO DE REALISMO",
                () => (english ? new[] { "Arcade", "Accessible", "Balanced", "Realistic", "Strict" } : new[] { "Arcade", "Acessível", "Equilibrado", "Realista", "Rigoroso" })[Mathf.Clamp(session.Draft.RealismPreset, 0, 4)],
                () => session.Draft.RealismPreset = (session.Draft.RealismPreset + 1) % 5, right, 0.955f, rowY[1],
                english ? "Adjusts the overall simulation balance, including how demanding match decisions and outcomes feel." : "Ajusta o equilíbrio geral da simulação, incluindo a exigência das decisões e dos resultados dos jogos.", ShowSettingHelp);
            AddSetting(panel.transform, english ? "AUTO SELECT XI" : "ESCOLHA AUTOMÁTICA DO ONZE",
                () => OnOff(session.Draft.AutomaticSquadSelection, english),
                () => session.Draft.AutomaticSquadSelection = !session.Draft.AutomaticSquadSelection, left, columnEnd, rowY[2],
                english ? "Lets the game choose the starting eleven for you when preparing a match." : "Deixa o jogo escolher o onze inicial por ti ao preparar uma partida.", ShowSettingHelp);
            AddSetting(panel.transform, english ? "AUTOMATIC TRAINING" : "TREINO AUTOMÁTICO",
                () => OnOff(session.Draft.AutomaticTraining, english),
                () => session.Draft.AutomaticTraining = !session.Draft.AutomaticTraining, right, 0.955f, rowY[2],
                english ? "Allows the coaching staff to assign and manage the squad's training automatically." : "Permite que a equipa técnica escolha e gira automaticamente o treino do plantel.", ShowSettingHelp);
            AddSetting(panel.transform, english ? "MATCH HIGHLIGHTS" : "MOMENTOS APRESENTADOS",
                () => (english ? new[] { "All", "Extended", "Key", "Goals only" } : new[] { "Todos", "Alargados", "Chave", "Só golos" })[Mathf.Clamp(session.Draft.MatchHighlights, 0, 3)],
                () => session.Draft.MatchHighlights = (session.Draft.MatchHighlights + 1) % 4, left, columnEnd, rowY[3],
                english ? "Chooses how much of each match is shown: every moment, longer sequences, key moments, or goals only." : "Escolhe quanto do jogo é apresentado: todos os momentos, sequências longas, momentos-chave ou apenas golos.", ShowSettingHelp);
            AddSetting(panel.transform, english ? "PAUSE AFTER GOAL" : "PAUSAR DEPOIS DE GOLO",
                () => OnOff(session.Draft.PauseOnGoal, english),
                () => session.Draft.PauseOnGoal = !session.Draft.PauseOnGoal, right, 0.955f, rowY[3],
                english ? "Automatically pauses the live match after a goal so you can review the situation." : "Pausa automaticamente o jogo ao vivo depois de um golo para poderes rever a situação.", ShowSettingHelp);
            AddSetting(panel.transform, english ? "PAUSE AFTER RED CARD" : "PAUSAR DEPOIS DE VERMELHO",
                () => OnOff(session.Draft.PauseOnRedCard, english),
                () => session.Draft.PauseOnRedCard = !session.Draft.PauseOnRedCard, left, columnEnd, rowY[4],
                english ? "Automatically pauses the live match when a player is sent off." : "Pausa automaticamente o jogo ao vivo quando um jogador é expulso.", ShowSettingHelp);
            AddSetting(panel.transform, english ? "PAUSE AFTER INJURY" : "PAUSAR DEPOIS DE LESÃO",
                () => OnOff(session.Draft.PauseOnInjury, english),
                () => session.Draft.PauseOnInjury = !session.Draft.PauseOnInjury, right, 0.955f, rowY[4],
                english ? "Automatically pauses the live match when a player is injured." : "Pausa automaticamente o jogo ao vivo quando um jogador se lesiona.", ShowSettingHelp);
            AddSetting(panel.transform, english ? "LIVE RATINGS" : "PONTUAÇÕES AO VIVO",
                () => OnOff(session.Draft.LiveRatings, english),
                () => session.Draft.LiveRatings = !session.Draft.LiveRatings, left, columnEnd, rowY[5],
                english ? "Shows player performance ratings during the live match." : "Mostra as classificações de desempenho dos jogadores durante o jogo ao vivo.", ShowSettingHelp);

            var personalizeText = BroadcastUi.Label(panel.transform, "PersonalizeText", english ? "EDIT THE DATABASE BEFORE THIS CAREER" : "EDITAR A BASE DE DADOS ANTES DA CARREIRA", 15, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(personalizeText.rectTransform, 0.05f, 0.115f, 0.49f, 0.17f);
            var personalizeHelp = BroadcastUi.Button(panel.transform, "PersonalizeHelp", "?", () => ShowSettingHelp(
                english ? "DATABASE PERSONALIZATION" : "PERSONALIZAÇÃO DA BASE DE DADOS",
                english ? "Enables a preview editor for player names and faces, club names and badges, coaches, staff, and sponsors. Edits belong to this career and are locked once the career starts."
                    : "Ativa um editor de pré-visualização para nomes e rostos dos jogadores, nomes e emblemas dos clubes, treinadores, equipa técnica e patrocinadores. As alterações pertencem a esta carreira e ficam bloqueadas depois de começar."),
                new Color(1f, 1f, 1f, 0.08f), UiTheme.Accent, 12);
            BroadcastUi.Place(personalizeHelp.GetComponent<RectTransform>(), 0.50f, 0.115f, 0.545f, 0.17f);
            Button editor = null;
            Button personalize = null;
            personalize = BroadcastUi.Button(panel.transform, "Personalize", PersonalizeLabel(session.Draft.PersonalizeDatabase, english), () =>
            {
                session.Draft.PersonalizeDatabase = !session.Draft.PersonalizeDatabase;
                if (!session.Draft.PersonalizeDatabase)
                    session.Draft.PreviewWorld = null;
                personalize.GetComponentInChildren<Text>().text = PersonalizeLabel(session.Draft.PersonalizeDatabase, english);
                editor.interactable = session.Draft.PersonalizeDatabase;
            }, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 13);
            BroadcastUi.Place(personalize.GetComponent<RectTransform>(), 0.56f, 0.115f, 0.95f, 0.17f);
            editor = BroadcastUi.Button(panel.transform, "PreviewEditor", english ? "PREVIEW & EDIT DATABASE" : "PRÉ-VISUALIZAR E EDITAR BASE DE DADOS", null,
                new Color(0.12f, 0.39f, 0.5f, 1f), Color.white, 13);
            BroadcastUi.Place(editor.GetComponent<RectTransform>(), 0.05f, 0.035f, 0.53f, 0.095f);
            editor.interactable = session.Draft.PersonalizeDatabase;
            editor.onClick.RemoveAllListeners();
            editor.onClick.AddListener(() =>
            {
                if (!session.Draft.PersonalizeDatabase) return;
                CareerDatabaseEditorOverlay.Open(parent, session, Finish);
            });
            var start = BroadcastUi.Button(panel.transform, "StartCareer", english ? "START CAREER" : "COMEÇAR CARREIRA", Finish,
                UiTheme.Accent, UiTheme.ButtonText, 17);
            BroadcastUi.Place(start.GetComponent<RectTransform>(), 0.56f, 0.035f, 0.95f, 0.095f);
            var back = BroadcastUi.Button(panel.transform, "Back", english ? "BACK" : "VOLTAR", () => UnityEngine.Object.Destroy(overlay),
                new Color(1f, 1f, 1f, 0.1f), Color.white, 12);
            BroadcastUi.Place(back.GetComponent<RectTransform>(), 0.83f, 0.89f, 0.95f, 0.97f);

            void Finish()
            {
                if (overlay != null) UnityEngine.Object.Destroy(overlay);
                startCareer?.Invoke();
            }
        }

        static string PersonalizeLabel(bool enabled, bool english)
        {
            return enabled ? (english ? "ON · PREVIEW AVAILABLE" : "LIGADO · PRÉ-VISUALIZAÇÃO DISPONÍVEL")
                : (english ? "OFF · DEFAULT DATABASE" : "DESLIGADO · BASE ORIGINAL");
        }

        static string OnOff(bool value, bool english) => value ? (english ? "ON" : "LIGADO") : (english ? "OFF" : "DESLIGADO");

        static void AddSetting(Transform parent, string title, Func<string> value, Action change, float x0, float x1, float y, string description, Action<string, string> showHelp)
        {
            var row = BroadcastUi.Panel(parent, "Setting" + title.Replace(" ", ""), new Color(1f, 1f, 1f, 0.06f));
            BroadcastUi.Place(row.rectTransform, x0, y, x1, y + 0.085f);
            var heading = BroadcastUi.Label(row.transform, "Title", title, 11, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.025f, 0.05f, 0.5f, 0.95f);
            var current = BroadcastUi.Label(row.transform, "Value", value(), 11, UiTheme.Accent, TextAnchor.MiddleRight, FontStyle.Bold);
            BroadcastUi.Place(current.rectTransform, 0.51f, 0.05f, 0.84f, 0.95f);
            var button = BroadcastUi.Hit(row, () =>
            {
                change();
                current.text = value();
            });
            button.interactable = true;
            var help = BroadcastUi.Button(row.transform, "Help", "?", () => showHelp?.Invoke(title, description),
                new Color(1f, 1f, 1f, 0.08f), UiTheme.Accent, 12);
            BroadcastUi.Place(help.GetComponent<RectTransform>(), 0.87f, 0.08f, 0.98f, 0.92f);
        }
    }

    public sealed class CareerDatabaseEditorOverlay : MonoBehaviour
    {
        enum DatabaseTab { Players, Coaches, Staff, Sponsors, Teams }

        static readonly string[] ColorChoices = { "#B5122B", "#164B9A", "#14844A", "#D8B525", "#EEEEEE", "#171923", "#6A35A5", "#E36B25" };

        sealed class Entry
        {
            public string Key;
            public string Title;
            public string Subtitle;
            public string Kind;
        }

        GameSession _session;
        WorldState _world;
        Action _finish;
        RectTransform _listContent;
        RectTransform _detailRoot;
        InputField _search;
        Text _scopeLabel;
        Button _scopeBack;
        DatabaseTab _tab;
        int _page;
        string _selected;
        string _scopeLeagueId;
        string _scopeClubId;
        bool _suppressSearchRefresh;
        readonly Dictionary<DatabaseTab, Button> _tabButtons = new Dictionary<DatabaseTab, Button>();

        public static void Open(Transform parent, GameSession session, Action finish)
        {
            if (parent == null || session == null) return;
            var overlay = new GameObject("CareerDatabaseEditor", typeof(RectTransform), typeof(Image));
            var rect = overlay.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            BroadcastUi.Stretch(rect);
            var shade = overlay.GetComponent<Image>();
            shade.color = new Color(0.015f, 0.02f, 0.06f, 0.88f);
            shade.raycastTarget = true;
            var editor = overlay.AddComponent<CareerDatabaseEditorOverlay>();
            editor.Build(session, finish);
            overlay.transform.SetAsLastSibling();
        }

        void Build(GameSession session, Action finish)
        {
            _session = session;
            _world = session.PrepareCareerPreview();
            _finish = finish;
            var english = LocalizationManager.Current == GameLanguage.English;
            var panel = BroadcastUi.Panel(transform, "EditorPanel", new Color(0.055f, 0.09f, 0.17f, 1f), true);
            BroadcastUi.Place(panel.rectTransform, 0.025f, 0.035f, 0.975f, 0.965f);
            BroadcastUi.ApplySandPaper(panel, 0.03f);
            var title = BroadcastUi.Label(panel.transform, "Title", english ? "DATABASE PREVIEW" : "PRÉ-VISUALIZAÇÃO DA BASE DE DADOS", 23, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.025f, 0.93f, 0.7f, 0.99f);
            var summary = BroadcastUi.Label(panel.transform, "Summary", english
                ? "Edits apply only to this career. Names, faces, attributes, teams, badges and sponsorships are saved with it."
                : "As alterações aplicam-se apenas a esta carreira. Nomes, rostos, atributos, clubes, emblemas e patrocínios ficam guardados nela.", 13, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(summary.rectTransform, 0.025f, 0.88f, 0.96f, 0.93f);

            var tabBar = BroadcastUi.Zone(panel.transform, "Tabs", 0.02f, 0.81f, 0.98f, 0.875f);
            AddTab(tabBar, english ? "PLAYERS" : "JOGADORES", DatabaseTab.Players, 0);
            AddTab(tabBar, english ? "COACHES" : "TREINADORES", DatabaseTab.Coaches, 1);
            AddTab(tabBar, english ? "STAFF" : "EQUIPA TÉCNICA", DatabaseTab.Staff, 2);
            AddTab(tabBar, english ? "SPONSORS" : "PATROCINADORES", DatabaseTab.Sponsors, 3);
            AddTab(tabBar, english ? "CLUBS" : "CLUBES", DatabaseTab.Teams, 4);

            var listPanel = BroadcastUi.Panel(panel.transform, "DatabaseList", new Color(0.035f, 0.06f, 0.12f, 1f));
            BroadcastUi.Place(listPanel.rectTransform, 0.025f, 0.11f, 0.35f, 0.8f);
            _search = UiFactory.Input(listPanel.transform, english ? "Search the database" : "Pesquisar na base de dados");
            BroadcastUi.Place(_search.GetComponent<RectTransform>(), 0.025f, 0.91f, 0.975f, 0.985f);
            _search.onValueChanged.AddListener(_ =>
            {
                if (_suppressSearchRefresh) return;
                _page = 0;
                RefreshList();
            });
            _scopeBack = BroadcastUi.Button(listPanel.transform, "ScopeBack", english ? "‹ BACK" : "‹ VOLTAR", BackScope,
                new Color(1f, 1f, 1f, 0.08f), Color.white, 10);
            BroadcastUi.Place(_scopeBack.GetComponent<RectTransform>(), 0.025f, 0.825f, 0.29f, 0.9f);
            _scopeLabel = BroadcastUi.Label(listPanel.transform, "ScopeLabel", "", 11, UiTheme.Accent, TextAnchor.MiddleRight, FontStyle.Bold);
            BroadcastUi.Place(_scopeLabel.rectTransform, 0.31f, 0.825f, 0.975f, 0.9f);
            var scroll = BroadcastUi.Scroll(listPanel.transform, "Entries", out _listContent);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.015f, 0.105f, 0.985f, 0.815f);
            var pageBar = BroadcastUi.Zone(listPanel.transform, "PageBar", 0.02f, 0.015f, 0.98f, 0.095f);
            var prev = BroadcastUi.Button(pageBar, "Previous", english ? "‹ PREVIOUS" : "‹ ANTERIOR", () => { _page = Math.Max(0, _page - 1); RefreshList(); }, new Color(1f, 1f, 1f, 0.08f), Color.white, 11);
            BroadcastUi.Place(prev.GetComponent<RectTransform>(), 0f, 0f, 0.31f, 1f);
            var next = BroadcastUi.Button(pageBar, "Next", english ? "NEXT ›" : "SEGUINTE ›", () => { _page++; RefreshList(); }, new Color(1f, 1f, 1f, 0.08f), Color.white, 11);
            BroadcastUi.Place(next.GetComponent<RectTransform>(), 0.69f, 0f, 1f, 1f);

            var detail = BroadcastUi.Panel(panel.transform, "Details", new Color(0.035f, 0.06f, 0.12f, 1f));
            BroadcastUi.Place(detail.rectTransform, 0.365f, 0.11f, 0.975f, 0.8f);
            _detailRoot = detail.rectTransform;
            var hint = BroadcastUi.Label(detail.transform, "Hint", english
                ? "Choose an entry on the left to preview and edit it."
                : "Escolhe um elemento à esquerda para o pré-visualizar e editar.", 18, UiTheme.Muted, TextAnchor.MiddleCenter);
            BroadcastUi.Stretch(hint.rectTransform);

            var back = BroadcastUi.Button(panel.transform, "Back", english ? "BACK TO CAREER SETUP" : "VOLTAR À CONFIGURAÇÃO", () => Destroy(gameObject),
                new Color(1f, 1f, 1f, 0.1f), Color.white, 12);
            BroadcastUi.Place(back.GetComponent<RectTransform>(), 0.025f, 0.025f, 0.3f, 0.09f);
            var done = BroadcastUi.Button(panel.transform, "Finish", english ? "SAVE EDITS & START CAREER" : "GUARDAR E COMEÇAR CARREIRA", FinishCareer,
                UiTheme.Accent, UiTheme.ButtonText, 14);
            BroadcastUi.Place(done.GetComponent<RectTransform>(), 0.69f, 0.025f, 0.975f, 0.09f);
            RefreshList();
        }

        void AddTab(Transform parent, string label, DatabaseTab tab, int index)
        {
            var button = BroadcastUi.Button(parent, "Tab" + tab, label, () =>
            {
                _tab = tab;
                _page = 0;
                _selected = null;
                _scopeLeagueId = null;
                _scopeClubId = null;
                ClearSearch();
                UpdateTabHighlight();
                RefreshList();
            }, new Color(1f, 1f, 1f, 0.08f), Color.white, 12);
            _tabButtons[tab] = button;
            BroadcastUi.Place(button.GetComponent<RectTransform>(), index * 0.2f + 0.004f, 0.04f, (index + 1) * 0.2f - 0.004f, 0.96f);
            UpdateTabHighlight();
        }

        void UpdateTabHighlight()
        {
            foreach (var pair in _tabButtons)
            {
                var selected = pair.Key == _tab;
                var image = pair.Value.targetGraphic as Image;
                if (image != null) image.color = selected ? UiTheme.Accent : new Color(0.28f, 0.29f, 0.34f, 1f);
                var text = pair.Value.GetComponentInChildren<Text>();
                if (text != null) text.color = selected ? UiTheme.ButtonText : Color.white;
                var motion = pair.Value.GetComponent<UiButtonMotion>();
                if (motion != null) motion.SetChosen(selected);
            }
        }

        void BackScope()
        {
            if (_scopeClubId == FreeAgentsScope || !string.IsNullOrEmpty(_scopeClubId))
                _scopeClubId = null;
            else if (!string.IsNullOrEmpty(_scopeLeagueId))
                _scopeLeagueId = null;
            _selected = null;
            _page = 0;
            ClearSearch();
            RefreshList();
        }

        void ClearSearch()
        {
            if (_search == null || string.IsNullOrEmpty(_search.text)) return;
            _suppressSearchRefresh = true;
            _search.text = "";
            _suppressSearchRefresh = false;
        }

        List<Entry> Entries()
        {
            var entries = new List<Entry>();
            var english = LocalizationManager.Current == GameLanguage.English;
            var query = _search == null ? "" : (_search.text ?? "").Trim();
            var globalSearch = query.Length > 0 && string.IsNullOrEmpty(_scopeLeagueId) && string.IsNullOrEmpty(_scopeClubId);
            if (globalSearch)
            {
                switch (_tab)
                {
                    case DatabaseTab.Players:
                        foreach (var searchedPlayer in _world.Players.Values)
                        {
                            if (searchedPlayer == null) continue;
                            var playerClub = !string.IsNullOrEmpty(searchedPlayer.ClubId) && _world.Clubs.TryGetValue(searchedPlayer.ClubId, out var matchingClub) ? matchingClub : null;
                            var context = playerClub == null ? (english ? "Free agent" : "Jogador livre") : playerClub.Name + " · " + LeagueName(playerClub.LeagueId);
                            entries.Add(new Entry { Key = searchedPlayer.Id, Title = searchedPlayer.FullName, Subtitle = NationalityName(searchedPlayer.Nationality, english) + " · " + PositionName(searchedPlayer.Position, english) + " · " + context, Kind = "record" });
                        }
                        break;
                    case DatabaseTab.Coaches:
                        foreach (var managerClub in _world.Clubs.Values)
                            if (managerClub != null && managerClub.Tactics != null && !string.IsNullOrWhiteSpace(managerClub.Tactics.FormerCoachName))
                                entries.Add(new Entry { Key = "manager|" + managerClub.Id, Title = managerClub.Tactics.FormerCoachName, Subtitle = managerClub.Name + " · " + LeagueName(managerClub.LeagueId), Kind = "record" });
                        break;
                    case DatabaseTab.Staff:
                        foreach (var staffClub in _world.Clubs.Values)
                            if (staffClub != null && staffClub.Staff != null)
                                foreach (var staff in staffClub.Staff)
                                    if (staff != null)
                                        entries.Add(new Entry { Key = "staff|" + staffClub.Id + "|" + staff.Id, Title = staff.FullName, Subtitle = StaffRoleName(staff.Role, english) + " · " + staffClub.Name + " · " + LeagueName(staffClub.LeagueId), Kind = "record" });
                        break;
                    case DatabaseTab.Sponsors:
                        foreach (var sponsorClub in _world.Clubs.Values)
                            if (sponsorClub != null && sponsorClub.Sponsors != null)
                                foreach (var sponsor in sponsorClub.Sponsors)
                                    if (sponsor != null)
                                        entries.Add(new Entry { Key = "sponsor|" + sponsorClub.Id + "|" + sponsor.Id, Title = sponsor.Name, Subtitle = sponsorClub.Name + " · " + sponsor.WeeklyIncome.ToString("N0") + " /wk", Kind = "record" });
                        break;
                    case DatabaseTab.Teams:
                        foreach (var searchedClub in _world.Clubs.Values)
                            if (searchedClub != null)
                                entries.Add(new Entry { Key = searchedClub.Id, Title = searchedClub.Name, Subtitle = LeagueName(searchedClub.LeagueId), Kind = "record" });
                        break;
                }
            }
            else if (string.IsNullOrEmpty(_scopeLeagueId) && _scopeClubId != FreeAgentsScope)
            {
                var leagueIds = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var club in _world.Clubs.Values)
                    if (club != null && !string.IsNullOrWhiteSpace(club.LeagueId)) leagueIds.Add(club.LeagueId);
                foreach (var leagueId in leagueIds)
                {
                    var count = 0;
                    foreach (var club in _world.Clubs.Values)
                        if (club != null && club.LeagueId == leagueId) count++;
                    entries.Add(new Entry
                    {
                        Key = "league|" + leagueId,
                        Title = LeagueName(leagueId),
                        Subtitle = count + (english ? " clubs" : " clubes"),
                        Kind = "league"
                    });
                }
                if (_tab == DatabaseTab.Players)
                {
                    var freeAgentCount = 0;
                    foreach (var player in _world.Players.Values)
                        if (player != null && string.IsNullOrEmpty(player.ClubId)) freeAgentCount++;
                    entries.Add(new Entry { Key = FreeAgentsScope, Title = english ? "Free Agents" : "Jogadores Livres", Subtitle = freeAgentCount + (english ? " players" : " jogadores"), Kind = "free-agents" });
                }
            }
            else if (string.IsNullOrEmpty(_scopeClubId) && _scopeClubId != FreeAgentsScope)
            {
                foreach (var club in _world.Clubs.Values)
                    if (club != null && club.LeagueId == _scopeLeagueId)
                        entries.Add(new Entry { Key = club.Id, Title = club.Name, Subtitle = club.ShortName, Kind = _tab == DatabaseTab.Teams ? "club-leaf" : "club" });
            }
            else if (_tab == DatabaseTab.Players)
            {
                foreach (var player in _world.Players.Values)
                {
                    if (player == null) continue;
                    var belongs = _scopeClubId == FreeAgentsScope ? string.IsNullOrEmpty(player.ClubId) : player.ClubId == _scopeClubId;
                    if (belongs)
                        entries.Add(new Entry { Key = player.Id, Title = player.FullName, Subtitle = NationalityName(player.Nationality, english) + " · " + PositionName(player.Position, english) + " · " + PlayerAttributes.OverallAtPosition(player, player.Position), Kind = "record" });
                }
            }
            else if (_tab == DatabaseTab.Teams)
            {
                var clubId = _scopeClubId;
                if (!string.IsNullOrEmpty(clubId) && _world.Clubs.TryGetValue(clubId, out var club))
                    entries.Add(new Entry { Key = club.Id, Title = club.Name, Subtitle = club.ShortName + " · " + LeagueName(club.LeagueId), Kind = "record" });
            }
            else if (_tab == DatabaseTab.Coaches)
            {
                if (_world.Clubs.TryGetValue(_scopeClubId, out var coachClub) && coachClub.Tactics != null && !string.IsNullOrWhiteSpace(coachClub.Tactics.FormerCoachName))
                    entries.Add(new Entry { Key = "manager|" + coachClub.Id, Title = coachClub.Tactics.FormerCoachName, Subtitle = english ? "Club manager" : "Treinador principal", Kind = "record" });
            }
            else if (_tab == DatabaseTab.Staff)
            {
                if (_world.Clubs.TryGetValue(_scopeClubId, out var staffOwnerClub) && staffOwnerClub.Staff != null)
                    foreach (var staff in staffOwnerClub.Staff)
                        if (staff != null)
                            entries.Add(new Entry { Key = "staff|" + staffOwnerClub.Id + "|" + staff.Id, Title = staff.FullName, Subtitle = StaffRoleName(staff.Role, english), Kind = "record" });
            }
            else
            {
                if (_world.Clubs.TryGetValue(_scopeClubId, out var sponsorOwnerClub) && sponsorOwnerClub.Sponsors != null)
                    foreach (var sponsor in sponsorOwnerClub.Sponsors)
                        if (sponsor != null)
                            entries.Add(new Entry { Key = "sponsor|" + sponsorOwnerClub.Id + "|" + sponsor.Id, Title = sponsor.Name, Subtitle = sponsorOwnerClub.ShortName + " · " + sponsor.WeeklyIncome.ToString("N0") + " /wk", Kind = "record" });
            }
            entries.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));
            if (query.Length > 0)
                entries.RemoveAll(item => item.Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 && item.Subtitle.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0);
            return entries;
        }

        const string FreeAgentsScope = "__free-agents__";

        string LeagueName(string id)
        {
            if (_world?.Leagues != null)
                foreach (var league in _world.Leagues)
                    if (league != null && league.Id == id)
                        return string.IsNullOrWhiteSpace(league.ShortName) ? (league.Name ?? id ?? "") : league.ShortName;
            return id ?? "";
        }

        string ScopeTitle(bool english)
        {
            if (_scopeClubId == FreeAgentsScope) return english ? "Free agents" : "Jogadores livres";
            if (string.IsNullOrEmpty(_scopeLeagueId)) return english ? "Choose a league" : "Escolhe uma liga";
            if (string.IsNullOrEmpty(_scopeClubId)) return LeagueName(_scopeLeagueId) + (english ? " · choose a club" : " · escolhe um clube");
            var clubName = _world.Clubs.TryGetValue(_scopeClubId, out var club) ? club.Name : _scopeClubId;
            var category = _tab == DatabaseTab.Players ? (english ? "players" : "jogadores")
                : _tab == DatabaseTab.Coaches ? (english ? "manager" : "treinador")
                : _tab == DatabaseTab.Staff ? (english ? "staff" : "equipa técnica")
                : _tab == DatabaseTab.Sponsors ? (english ? "sponsors" : "patrocinadores")
                : (english ? "club" : "clube");
            return clubName + " · " + category;
        }

        void RefreshList()
        {
            if (_listContent == null) return;
            for (var i = _listContent.childCount - 1; i >= 0; i--)
                Destroy(_listContent.GetChild(i).gameObject);
            var entries = Entries();
            const int pageSize = 56;
            var pageCount = Math.Max(1, (entries.Count + pageSize - 1) / pageSize);
            _page = Mathf.Clamp(_page, 0, pageCount - 1);
            var start = _page * pageSize;
            var end = Math.Min(entries.Count, start + pageSize);
            var english = LocalizationManager.Current == GameLanguage.English;
            if (_scopeLabel != null) _scopeLabel.text = ScopeTitle(english);
            if (_scopeBack != null) _scopeBack.gameObject.SetActive(!string.IsNullOrEmpty(_scopeLeagueId) || !string.IsNullOrEmpty(_scopeClubId));
            for (var i = start; i < end; i++)
            {
                var item = entries[i];
                var captured = item;
                var row = BroadcastUi.Button(_listContent, "Entry" + i, item.Title + "\n" + item.Subtitle,
                    () => SelectEntry(captured),
                    (item.Kind == "record" || item.Kind == "club-leaf") && item.Key == _selected ? new Color(0.12f, 0.43f, 0.54f, 1f) : new Color(1f, 1f, 1f, 0.055f), Color.white, 12);
                row.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
                row.GetComponentInChildren<Text>().resizeTextForBestFit = true;
                row.GetComponentInChildren<Text>().resizeTextMinSize = 9;
                row.GetComponentInChildren<Text>().resizeTextMaxSize = 12;
                AddEntryImages(row, item);
                var rect = row.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(0f, 50f);
                var le = row.gameObject.AddComponent<LayoutElement>();
                le.minHeight = le.preferredHeight = 50f;
            }
            var pageText = BroadcastUi.Label(transform, "PageStatus", (english ? "Page " : "Página ") + (_page + 1) + " / " + pageCount + " · " + entries.Count + (english ? " entries" : " elementos"), 10, UiTheme.Muted, TextAnchor.MiddleCenter);
            var pageBar = transform.Find("EditorPanel/DatabaseList/PageBar") as RectTransform;
            if (pageBar != null)
            {
                var old = pageBar.Find("PageStatus");
                if (old != null) Destroy(old.gameObject);
                pageText.transform.SetParent(pageBar, false);
                BroadcastUi.Place(pageText.rectTransform, 0.32f, 0f, 0.68f, 1f);
            }
            else Destroy(pageText.gameObject);
            var recordEntries = entries.Exists(x => x.Kind == "record" || x.Kind == "club-leaf");
            if (recordEntries && (string.IsNullOrEmpty(_selected) || !entries.Exists(x => (x.Kind == "record" || x.Kind == "club-leaf") && x.Key == _selected)))
            {
                var firstRecord = entries.FindIndex(start, x => x.Kind == "record" || x.Kind == "club-leaf");
                if (firstRecord < 0) firstRecord = entries.FindIndex(x => x.Kind == "record" || x.Kind == "club-leaf");
                _selected = entries[firstRecord].Key;
                BuildDetails();
            }
            else if (entries.Count == 0)
                ShowEmptyDetails();
            else if (recordEntries && !string.IsNullOrEmpty(_selected)) BuildDetails();
            else ShowBrowseHint(english);
        }

        void SelectEntry(Entry entry)
        {
            if (entry == null) return;
            _page = 0;
            if (entry.Kind == "league")
            {
                _scopeLeagueId = entry.Key.Substring("league|".Length);
                _scopeClubId = null;
                _selected = null;
                ClearSearch();
            }
            else if (entry.Kind == "club")
            {
                _scopeClubId = entry.Key.StartsWith("club|", StringComparison.Ordinal) ? entry.Key.Substring("club|".Length) : entry.Key;
                _selected = null;
                ClearSearch();
            }
            else if (entry.Kind == "club-leaf")
            {
                _scopeClubId = entry.Key.StartsWith("club|", StringComparison.Ordinal) ? entry.Key.Substring("club|".Length) : entry.Key;
                _selected = _scopeClubId;
            }
            else if (entry.Kind == "free-agents")
            {
                _scopeLeagueId = null;
                _scopeClubId = FreeAgentsScope;
                _selected = null;
                ClearSearch();
            }
            else
            {
                _selected = entry.Key;
            }
            RefreshList();
        }

        void AddEntryImages(Button row, Entry entry)
        {
            if (row == null || entry == null) return;
            Sprite portrait = null;
            Sprite badge = null;
            if (entry.Kind == "league")
            {
                var id = entry.Key.StartsWith("league|", StringComparison.Ordinal) ? entry.Key.Substring(7) : entry.Key;
                badge = CrestStudio.ForCompetition(id);
            }
            else if (_tab == DatabaseTab.Players && _world.Players.TryGetValue(entry.Key, out var player))
            {
                portrait = FaceBook.For(player);
                if (!string.IsNullOrEmpty(player.ClubId) && _world.Clubs.TryGetValue(player.ClubId, out var playerClub))
                    badge = CrestStudio.For(playerClub);
            }
            else
            {
                var clubId = entry.Key;
                if (clubId.StartsWith("manager|", StringComparison.Ordinal)) clubId = clubId.Substring("manager|".Length);
                else if (clubId.StartsWith("staff|", StringComparison.Ordinal) || clubId.StartsWith("sponsor|", StringComparison.Ordinal))
                {
                    var parts = clubId.Split('|');
                    clubId = parts.Length > 1 ? parts[1] : "";
                }
                else if (clubId.StartsWith("club|", StringComparison.Ordinal)) clubId = clubId.Substring("club|".Length);
                if (_world.Clubs.TryGetValue(clubId, out var club)) badge = CrestStudio.For(club);
            }

            var text = row.GetComponentInChildren<Text>();
            if (text != null)
                BroadcastUi.Place(text.rectTransform, portrait != null || badge != null ? 0.17f : 0.035f, 0f, portrait != null && badge != null ? 0.82f : 0.97f, 1f);
            if (portrait != null) AddRowImage(row.transform, "Face", portrait, 0.025f, 0.15f, 0.84f);
            if (badge != null) AddRowImage(row.transform, "ClubBadge", badge, portrait != null ? 0.85f : 0.025f, portrait != null ? 0.975f : 0.15f, 0.72f);
        }

        static void AddRowImage(Transform parent, string name, Sprite sprite, float x0, float x1, float inset)
        {
            if (sprite == null) return;
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            BroadcastUi.Place(image.rectTransform, x0, 1f - inset, x1, inset);
        }

        void ShowBrowseHint(bool english)
        {
            ClearDetails();
            var message = english
                ? "Choose a league, then a club, to browse its " + TabNoun(true) + "."
                : "Escolhe uma liga e depois um clube para consultar " + TabNoun(false) + ".";
            var hint = BroadcastUi.Label(_detailRoot, "BrowseHint", message, 18, UiTheme.Muted, TextAnchor.MiddleCenter);
            BroadcastUi.Stretch(hint.rectTransform);
        }

        string TabNoun(bool english)
        {
            switch (_tab)
            {
                case DatabaseTab.Players: return english ? "players" : "os jogadores";
                case DatabaseTab.Coaches: return english ? "manager" : "o treinador";
                case DatabaseTab.Staff: return english ? "staff" : "a equipa técnica";
                case DatabaseTab.Sponsors: return english ? "sponsors" : "os patrocinadores";
                default: return english ? "club" : "clube";
            }
        }

        void ShowEmptyDetails()
        {
            ClearDetails();
            var empty = BroadcastUi.Label(_detailRoot, "Empty", LocalizationManager.Current == GameLanguage.English ? "No matching entries." : "Não foram encontrados elementos.", 17, UiTheme.Muted, TextAnchor.MiddleCenter);
            BroadcastUi.Stretch(empty.rectTransform);
        }

        void BuildDetails()
        {
            ClearDetails();
            if (string.IsNullOrEmpty(_selected)) { ShowEmptyDetails(); return; }
            switch (_tab)
            {
                case DatabaseTab.Players: BuildPlayerDetails(); break;
                case DatabaseTab.Coaches: BuildCoachDetails(); break;
                case DatabaseTab.Staff: BuildStaffDetails(); break;
                case DatabaseTab.Sponsors: BuildSponsorDetails(); break;
                case DatabaseTab.Teams: BuildTeamDetails(); break;
            }
        }

        void ClearDetails()
        {
            if (_detailRoot == null) return;
            for (var i = _detailRoot.childCount - 1; i >= 0; i--)
                Destroy(_detailRoot.GetChild(i).gameObject);
        }

        void BuildPlayerDetails()
        {
            if (!_world.Players.TryGetValue(_selected, out var player)) return;
            var english = LocalizationManager.Current == GameLanguage.English;
            if (player.Attributes == null) player.Attributes = new PlayerAttributes();
            AddDetailTitle(player.FullName, NationalityName(player.Nationality, english) + " · " + PositionName(player.Position, english) + " · " + player.Age + (english ? " years" : " anos"));
            var portrait = new GameObject("Face", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            portrait.transform.SetParent(_detailRoot, false);
            portrait.preserveAspect = true;
            portrait.sprite = FaceBook.For(player);
            BroadcastUi.Place(portrait.rectTransform, 0.035f, 0.59f, 0.22f, 0.83f);
            AddInput(english ? "PLAYER NAME" : "NOME DO JOGADOR", player.FullName, 0.25f, 0.70f, value => { player.FullName = value; RefreshList(); });
            var overall = PlayerAttributes.OverallAtPosition(player, player.Position);
            AddStepper(english ? "CURRENT OVERALL" : "NÍVEL ATUAL", overall.ToString(), 0.59f,
                () => { SetOverall(player, PlayerAttributes.OverallAtPosition(player, player.Position) - 1); BuildDetails(); },
                () => { SetOverall(player, PlayerAttributes.OverallAtPosition(player, player.Position) + 1); BuildDetails(); }, 0.25f, 0.96f);
            AddStepper(english ? "POTENTIAL" : "POTENCIAL", player.Potential.ToString(), 0.48f,
                () => { player.Potential = Mathf.Max(PlayerAttributes.OverallAtPosition(player, player.Position), player.Potential - 1); BuildDetails(); },
                () => { player.Potential = Mathf.Min(99, player.Potential + 1); BuildDetails(); }, 0.25f, 0.96f);
            AddButton(english ? "CHOOSE FACE FROM YOUR FILES" : "ESCOLHER ROSTO DOS TEUS FICHEIROS", 0.035f, 0.46f, 0.22f, () =>
            {
                var fileKey = ChooseImageFile("caras", english ? "Choose player face" : "Escolher rosto do jogador");
                if (string.IsNullOrEmpty(fileKey)) return;
                var imported = FaceBook.ModPortrait(fileKey);
                if (imported != null) FaceBook.SetPortrait(player, imported);
                BuildDetails();
            });
            var fields = player.Position == PlayerPosition.Goalkeeper
                ? new[] { "Reflexes", "Handling", "Positioning", "Composure", "Decisions", "Pace", "Passing", "Stamina", "Strength", "WorkRate", "Tackling", "Crossing", "Dribbling", "Finishing" }
                : new[] { "Pace", "Stamina", "Strength", "Passing", "Finishing", "Dribbling", "Tackling", "Crossing", "Positioning", "Composure", "WorkRate", "Decisions", "Reflexes", "Handling" };
            for (var i = 0; i < fields.Length; i++)
            {
                var column = i < 7 ? 0 : 1;
                var row = i % 7;
                var x0 = column == 0 ? 0.035f : 0.515f;
                var x1 = column == 0 ? 0.485f : 0.965f;
                var y = 0.395f - row * 0.052f;
                var field = fields[i];
                AddAttributeStepper(player, field, AttributeLabel(field, english), x0, x1, y);
            }
            var attributes = BroadcastUi.Label(_detailRoot, "AttributesHint", english
                ? "Position-weighted overall updates from these attributes. Form, fitness and morale also affect match-day attributes."
                : "O nível ponderado pela posição atualiza com estes atributos. A forma, condição física e moral também afetam os atributos no jogo.", 10, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(attributes.rectTransform, 0.035f, 0.015f, 0.965f, 0.055f);
        }

        static void SetOverall(Player player, int target)
        {
            target = Mathf.Clamp(target, 42, 94);
            var attributes = player.Attributes;
            if (attributes == null) player.Attributes = attributes = new PlayerAttributes();
            var fields = player.Position == PlayerPosition.Goalkeeper
                ? new[] { "Reflexes", "Handling", "Positioning", "Composure", "Decisions" }
                : new[] { "Pace", "Stamina", "Strength", "Passing", "Finishing", "Dribbling", "Tackling", "Crossing", "Positioning", "Composure", "WorkRate", "Decisions" };
            var blocked = new HashSet<string>();
            var direction = PlayerAttributes.OverallAtPosition(player, player.Position) < target ? 1 : -1;
            for (var pass = 0; pass < 600 && PlayerAttributes.OverallAtPosition(player, player.Position) != target; pass++)
            {
                var adjusted = false;
                foreach (var field in fields)
                {
                    if (blocked.Contains(field)) continue;
                    var value = AttributeValue(attributes, field);
                    var candidateValue = value + direction;
                    if (candidateValue < 1 || candidateValue > 20)
                    {
                        blocked.Add(field);
                        continue;
                    }
                    SetAttribute(attributes, field, candidateValue);
                    var candidateOverall = PlayerAttributes.OverallAtPosition(player, player.Position);
                    if ((direction > 0 && candidateOverall > target) || (direction < 0 && candidateOverall < target))
                    {
                        SetAttribute(attributes, field, value);
                        blocked.Add(field);
                        continue;
                    }
                    adjusted = true;
                    break;
                }
                if (!adjusted) break;
            }
            player.Potential = Mathf.Max(player.Potential, PlayerAttributes.OverallAtPosition(player, player.Position));
        }

        void AddAttributeStepper(Player player, string field, string label, float x0, float x1, float y)
        {
            var width = x1 - x0;
            var name = BroadcastUi.Label(_detailRoot, "AttributeName" + field, label, 11, Color.white, TextAnchor.MiddleLeft);
            BroadcastUi.Place(name.rectTransform, x0, y, x0 + width * 0.51f, y + 0.046f);
            var minus = BroadcastUi.Button(_detailRoot, "AttributeMinus" + field, "−", () =>
            {
                ChangePlayerAttribute(player, field, -1);
                BuildDetails();
            }, new Color(1f, 1f, 1f, 0.1f), Color.white, 13);
            BroadcastUi.Place(minus.GetComponent<RectTransform>(), x0 + width * 0.53f, y, x0 + width * 0.67f, y + 0.046f);
            var value = BroadcastUi.Label(_detailRoot, "AttributeValue" + field, AttributeValue(player.Attributes, field).ToString(), 12, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(value.rectTransform, x0 + width * 0.68f, y, x0 + width * 0.84f, y + 0.046f);
            var plus = BroadcastUi.Button(_detailRoot, "AttributePlus" + field, "+", () =>
            {
                ChangePlayerAttribute(player, field, 1);
                BuildDetails();
            }, new Color(1f, 1f, 1f, 0.1f), Color.white, 13);
            BroadcastUi.Place(plus.GetComponent<RectTransform>(), x0 + width * 0.85f, y, x1, y + 0.046f);
        }

        static void ChangePlayerAttribute(Player player, string field, int delta)
        {
            if (player == null) return;
            if (player.Attributes == null) player.Attributes = new PlayerAttributes();
            var value = Mathf.Clamp(AttributeValue(player.Attributes, field) + delta, 1, 20);
            SetAttribute(player.Attributes, field, value);
            player.Potential = Mathf.Max(player.Potential, PlayerAttributes.OverallAtPosition(player, player.Position));
        }

        static string AttributeLabel(string field, bool english)
        {
            switch (field)
            {
                case "Pace": return english ? "Pace" : "Velocidade";
                case "Stamina": return english ? "Stamina" : "Resistência";
                case "Strength": return english ? "Strength" : "Força";
                case "Passing": return english ? "Passing" : "Passe";
                case "Finishing": return english ? "Finishing" : "Finalização";
                case "Dribbling": return english ? "Dribbling" : "Drible";
                case "Tackling": return english ? "Tackling" : "Desarme";
                case "Crossing": return english ? "Crossing" : "Cruzamento";
                case "Positioning": return english ? "Positioning" : "Posicionamento";
                case "Composure": return english ? "Composure" : "Frieza";
                case "WorkRate": return english ? "Work rate" : "Entrega";
                case "Decisions": return english ? "Decisions" : "Decisão";
                case "Reflexes": return english ? "Reflexes" : "Reflexos";
                default: return english ? "Handling" : "Manejo";
            }
        }

        static int AttributeValue(PlayerAttributes a, string field)
        {
            switch (field)
            {
                case "Pace": return a.Pace;
                case "Stamina": return a.Stamina;
                case "Strength": return a.Strength;
                case "Passing": return a.Passing;
                case "Finishing": return a.Finishing;
                case "Dribbling": return a.Dribbling;
                case "Tackling": return a.Tackling;
                case "Crossing": return a.Crossing;
                case "Positioning": return a.Positioning;
                case "Composure": return a.Composure;
                case "WorkRate": return a.WorkRate;
                case "Decisions": return a.Decisions;
                case "Reflexes": return a.Reflexes;
                default: return a.Handling;
            }
        }

        static void SetAttribute(PlayerAttributes a, string field, int value)
        {
            switch (field)
            {
                case "Pace": a.Pace = value; break;
                case "Stamina": a.Stamina = value; break;
                case "Strength": a.Strength = value; break;
                case "Passing": a.Passing = value; break;
                case "Finishing": a.Finishing = value; break;
                case "Dribbling": a.Dribbling = value; break;
                case "Tackling": a.Tackling = value; break;
                case "Crossing": a.Crossing = value; break;
                case "Positioning": a.Positioning = value; break;
                case "Composure": a.Composure = value; break;
                case "WorkRate": a.WorkRate = value; break;
                case "Decisions": a.Decisions = value; break;
                case "Reflexes": a.Reflexes = value; break;
                case "Handling": a.Handling = value; break;
            }
        }

        void BuildCoachDetails()
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var bits = _selected.Split('|');
            if (bits.Length < 2) return;
            if (bits[0] == "manager" && _world.Clubs.TryGetValue(bits[1], out var club) && club.Tactics != null)
            {
                AddDetailTitle(club.Tactics.FormerCoachName, club.Name + " · " + (english ? "Manager" : "Treinador"));
                AddInput(english ? "MANAGER NAME" : "NOME DO TREINADOR", club.Tactics.FormerCoachName, 0.24f, 0.68f, value => { club.Tactics.FormerCoachName = value; RefreshList(); });
            }
        }

        void BuildStaffDetails()
        {
            var bits = _selected.Split('|');
            if (bits.Length < 3 || !_world.Clubs.TryGetValue(bits[1], out var club) || club.Staff == null) return;
            StaffMember member = null;
            foreach (var candidate in club.Staff)
                if (candidate != null && candidate.Id == bits[2]) { member = candidate; break; }
            if (member == null) return;
            var english = LocalizationManager.Current == GameLanguage.English;
            AddDetailTitle(member.FullName, club.Name + " · " + StaffRoleName(member.Role, english));
            AddInput(english ? "STAFF NAME" : "NOME DO STAFF", member.FullName, 0.24f, 0.68f, value => { member.FullName = value; RefreshList(); });
            AddStepper(english ? "JUDGEMENT" : "AVALIAÇÃO", member.Judgement.ToString(), 0.48f,
                () => { member.Judgement = Mathf.Max(1, member.Judgement - 1); BuildDetails(); },
                () => { member.Judgement = Mathf.Min(20, member.Judgement + 1); BuildDetails(); });
        }

        void BuildSponsorDetails()
        {
            var bits = _selected.Split('|');
            if (bits.Length < 3 || !_world.Clubs.TryGetValue(bits[1], out var club) || club.Sponsors == null) return;
            SponsorContract sponsor = null;
            foreach (var item in club.Sponsors)
                if (item != null && item.Id == bits[2]) { sponsor = item; break; }
            if (sponsor == null) return;
            var english = LocalizationManager.Current == GameLanguage.English;
            AddDetailTitle(sponsor.Name, club.Name + " · " + (english ? "sponsorship contract" : "contrato de patrocínio"));
            var logoTile = BroadcastUi.Panel(_detailRoot, "SponsorLogoTile", BroadcastUi.Parse(sponsor.LogoHex, new Color(0.12f, 0.2f, 0.32f)), true);
            BroadcastUi.Place(logoTile.rectTransform, 0.76f, 0.70f, 0.95f, 0.94f);
            var logo = ModFolder.SponsorLogo(sponsor.LogoKey);
            if (logo != null)
            {
                var image = new GameObject("SponsorLogo", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(logoTile.transform, false);
                image.sprite = logo;
                image.preserveAspect = true;
                BroadcastUi.Place(image.rectTransform, 0.08f, 0.08f, 0.92f, 0.92f);
            }
            else
            {
                var fallback = BroadcastUi.Label(logoTile.transform, "SponsorInitials", string.IsNullOrEmpty(sponsor.LogoKey) ? "SP" : sponsor.LogoKey.ToUpperInvariant(), 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Stretch(fallback.rectTransform);
            }
            var logoCaption = BroadcastUi.Label(_detailRoot, "LogoFile", (english ? "LOGO · " : "LOGÓTIPO · ") + (string.IsNullOrEmpty(sponsor.LogoKey) ? (english ? "COLOURED MARK" : "MARCA COM COR") : sponsor.LogoKey), 10, UiTheme.AccentSoft, TextAnchor.MiddleCenter);
            BroadcastUi.Place(logoCaption.rectTransform, 0.72f, 0.64f, 0.98f, 0.69f);
            var colorSwatch = new GameObject("SponsorColorPreview", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            colorSwatch.transform.SetParent(_detailRoot, false);
            colorSwatch.color = BroadcastUi.Parse(sponsor.LogoHex, UiTheme.Accent);
            BroadcastUi.Place(colorSwatch.rectTransform, 0.76f, 0.52f, 0.95f, 0.60f);
            AddInput(english ? "SPONSOR NAME" : "NOME DO PATROCINADOR", sponsor.Name, 0.04f, 0.69f, value => { sponsor.Name = value; RefreshList(); }, 0.70f);
            AddInput(english ? "WEEKLY INCOME (€)" : "RECEITA SEMANAL (€)", sponsor.WeeklyIncome.ToString(), 0.04f, 0.54f, value =>
            {
                if (int.TryParse(value, out var amount)) sponsor.WeeklyIncome = Mathf.Clamp(amount, 0, 10000000);
                RefreshList();
            }, 0.70f);
            AddButton(english ? "CHOOSE LOGO FROM YOUR FILES" : "ESCOLHER LOGÓTIPO DOS TEUS FICHEIROS", 0.04f, 0.36f, 0.96f, () =>
            {
                var fileKey = ChooseImageFile("patrocinadores", english ? "Choose sponsor logo" : "Escolher logótipo do patrocinador");
                if (string.IsNullOrEmpty(fileKey)) return;
                sponsor.LogoKey = fileKey;
                BuildDetails();
            });
            AddButton(english ? "CHANGE BRAND COLOUR" : "MUDAR COR DA MARCA", 0.04f, 0.22f, 0.96f, () =>
            {
                var color = Array.IndexOf(ColorChoices, sponsor.LogoHex);
                sponsor.LogoHex = ColorChoices[(color + 1 + ColorChoices.Length) % ColorChoices.Length];
                BuildDetails();
            });
            var add = BroadcastUi.Button(_detailRoot, "AddSponsor", english ? "ADD ANOTHER SPONSOR" : "ADICIONAR PATROCINADOR", () =>
            {
                if (club.Sponsors == null) club.Sponsors = new List<SponsorContract>();
                var sponsorNumber = club.Sponsors.Count + 1;
                var newSponsor = new SponsorContract { Id = "custom:" + club.Id + ":" + sponsorNumber, Name = english ? "New Sponsor" : "Novo patrocinador", WeeklyIncome = 10000, LogoHex = ColorChoices[(sponsorNumber - 1) % ColorChoices.Length] };
                club.Sponsors.Add(newSponsor);
                _selected = "sponsor|" + club.Id + "|" + newSponsor.Id;
                RefreshList();
            }, new Color(0.12f, 0.39f, 0.5f, 1f), Color.white, 12);
            BroadcastUi.Place(add.GetComponent<RectTransform>(), 0.04f, 0.06f, 0.96f, 0.16f);
        }

        void BuildTeamDetails()
        {
            if (!_world.Clubs.TryGetValue(_selected, out var club)) return;
            var english = LocalizationManager.Current == GameLanguage.English;
            AddDetailTitle(club.Name, LeagueName(club.LeagueId) + " · " + (english ? "club identity" : "identidade do clube"));
            var crest = new GameObject("Badge", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            crest.transform.SetParent(_detailRoot, false);
            crest.preserveAspect = true;
            crest.sprite = CrestStudio.For(club);
            BroadcastUi.Place(crest.rectTransform, 0.78f, 0.73f, 0.95f, 0.94f);
            AddInput(english ? "TEAM NAME" : "NOME DO CLUBE", club.Name, 0.04f, 0.69f, value => { club.Name = value; RefreshList(); }, 0.70f);
            AddInput(english ? "SHORT NAME" : "NOME CURTO", club.ShortName, 0.04f, 0.54f, value => { club.ShortName = value; RefreshList(); }, 0.70f);
            AddButton(english ? "CHOOSE BADGE FROM YOUR FILES" : "ESCOLHER EMBLEMA DOS TEUS FICHEIROS", 0.04f, 0.36f, 0.96f, () =>
            {
                var fileKey = ChooseImageFile("emblemas", english ? "Choose club badge" : "Escolher emblema do clube");
                if (string.IsNullOrEmpty(fileKey)) return;
                club.CustomCrestKey = fileKey;
                club.CrestResourcePath = "";
                BuildDetails();
            });
            AddButton(english ? "CHANGE PRIMARY COLOUR" : "MUDAR COR PRINCIPAL", 0.04f, 0.21f, 0.48f, () => CycleClubColor(club, true));
            AddButton(english ? "CHANGE SECONDARY COLOUR" : "MUDAR COR SECUNDÁRIA", 0.52f, 0.21f, 0.96f, () => CycleClubColor(club, false));
            var info = BroadcastUi.Label(_detailRoot, "TeamInfo", english
                ? "Club colours also update the generated kit and badge if no custom crest is selected."
                : "As cores do clube também atualizam o equipamento e o emblema gerado se não escolheres um emblema personalizado.", 12, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(info.rectTransform, 0.04f, 0.08f, 0.96f, 0.17f);
        }

        void CycleClubColor(Club club, bool primary)
        {
            var color = primary ? club.JerseyPrimaryHex : club.JerseySecondaryHex;
            var index = Array.IndexOf(ColorChoices, color);
            var next = ColorChoices[(index + 1 + ColorChoices.Length) % ColorChoices.Length];
            if (primary) club.JerseyPrimaryHex = next; else club.JerseySecondaryHex = next;
            WorldCatalog.EnsureThreeColors(club);
            club.KitSlug = "mod:" + club.JerseyPrimaryHex + ":" + club.JerseySecondaryHex + ":" + club.JerseyShortsHex;
            BuildDetails();
        }

        string ChooseImageFile(string folder, string title)
        {
            var sourcePath = CoachMaster.UI.WindowsMusicPicker.OpenImage(title);
            if (string.IsNullOrWhiteSpace(sourcePath)) return null;
            return ModFolder.ImportImageFile(sourcePath, folder);
        }

        static string StaffRoleName(StaffRole role, bool english)
        {
            switch (role)
            {
                case StaffRole.AssistantCoach: return english ? "Assistant coach" : "Treinador-adjunto";
                case StaffRole.GoalkeeperCoach: return english ? "Goalkeeper coach" : "Treinador de guarda-redes";
                case StaffRole.FitnessCoach: return english ? "Fitness coach" : "Preparador físico";
                case StaffRole.Physiotherapist: return english ? "Physiotherapist" : "Fisioterapeuta";
                case StaffRole.Scout: return english ? "Scout" : "Olheiro";
                default: return english ? "Staff" : "Equipa técnica";
            }
        }

        void AddDetailTitle(string heading, string subheading)
        {
            var title = BroadcastUi.Label(_detailRoot, "DetailTitle", heading, 24, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.04f, 0.88f, 0.75f, 0.98f);
            var subtitle = BroadcastUi.Label(_detailRoot, "DetailSubtitle", subheading, 13, UiTheme.AccentSoft, TextAnchor.MiddleLeft);
            BroadcastUi.Place(subtitle.rectTransform, 0.04f, 0.82f, 0.8f, 0.89f);
        }

        InputField AddInput(string label, string value, float x0, float y, Action<string> changed, float x1 = 0.96f)
        {
            var header = BroadcastUi.Label(_detailRoot, "FieldLabel" + y, label, 11, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(header.rectTransform, x0, y + 0.075f, x1, y + 0.12f);
            var field = UiFactory.Input(_detailRoot, label);
            field.text = value ?? "";
            BroadcastUi.Place(field.GetComponent<RectTransform>(), x0, y, x1, y + 0.075f);
            field.onEndEdit.AddListener(text => changed?.Invoke((text ?? "").Trim()));
            return field;
        }

        void AddStepper(string label, string value, float y, Action down, Action up, float x0 = 0.04f, float x1 = 0.96f)
        {
            var title = BroadcastUi.Label(_detailRoot, "StepTitle" + y, label, 11, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, x0, y + 0.09f, x1, y + 0.14f);
            var minus = BroadcastUi.Button(_detailRoot, "Minus" + y, "−", down, new Color(1f, 1f, 1f, 0.1f), Color.white, 18);
            BroadcastUi.Place(minus.GetComponent<RectTransform>(), x0, y, x0 + (x1 - x0) * 0.18f, y + 0.085f);
            var current = BroadcastUi.Label(_detailRoot, "StepValue" + y, value, 19, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(current.rectTransform, x0 + (x1 - x0) * 0.2f, y, x0 + (x1 - x0) * 0.8f, y + 0.085f);
            var plus = BroadcastUi.Button(_detailRoot, "Plus" + y, "+", up, new Color(1f, 1f, 1f, 0.1f), Color.white, 18);
            BroadcastUi.Place(plus.GetComponent<RectTransform>(), x0 + (x1 - x0) * 0.82f, y, x1, y + 0.085f);
        }

        static string PositionName(PlayerPosition position, bool english)
        {
            switch (position)
            {
                case PlayerPosition.Goalkeeper: return english ? "Goalkeeper" : "Guarda-redes";
                case PlayerPosition.RightBack: return english ? "Right back" : "Lateral direito";
                case PlayerPosition.CentreBack: return english ? "Centre-back" : "Defesa-central";
                case PlayerPosition.LeftBack: return english ? "Left back" : "Lateral esquerdo";
                case PlayerPosition.DefensiveMidfielder: return english ? "Defensive midfielder" : "Médio defensivo";
                case PlayerPosition.CentralMidfielder: return english ? "Central midfielder" : "Médio-centro";
                case PlayerPosition.AttackingMidfielder: return english ? "Attacking midfielder" : "Médio ofensivo";
                case PlayerPosition.RightWinger: return english ? "Right winger" : "Extremo direito";
                case PlayerPosition.LeftWinger: return english ? "Left winger" : "Extremo esquerdo";
                case PlayerPosition.LeftMidfielder: return english ? "Left midfielder" : "Médio esquerdo";
                case PlayerPosition.RightMidfielder: return english ? "Right midfielder" : "Médio direito";
                default: return english ? "Striker" : "Ponta de lança";
            }
        }

        static string NationalityName(string nationality, bool english)
        {
            if (!english) return nationality ?? "";
            switch (nationality)
            {
                case "Brasil": return "Brazil";
                case "Japão": return "Japan";
                case "Coreia do Sul": return "South Korea";
                case "Turquia": return "Turkey";
                case "Itália": return "Italy";
                case "Alemanha": return "Germany";
                case "Espanha": return "Spain";
                case "Bélgica": return "Belgium";
                case "Inglaterra": return "England";
                case "França": return "France";
                case "Países Baixos": return "Netherlands";
                case "RD Congo": return "DR Congo";
                case "Gana": return "Ghana";
                case "Nigéria": return "Nigeria";
                case "Escócia": return "Scotland";
                case "Irlanda": return "Ireland";
                case "Dinamarca": return "Denmark";
                case "Suécia": return "Sweden";
                case "Áustria": return "Austria";
                case "Polónia": return "Poland";
                case "Croácia": return "Croatia";
                case "Sérvia": return "Serbia";
                case "República Checa": return "Czechia";
                case "Hungria": return "Hungary";
                case "Suíça": return "Switzerland";
                case "Roménia": return "Romania";
                case "Uruguai": return "Uruguay";
                case "Colômbia": return "Colombia";
                case "Paraguai": return "Paraguay";
                case "Equador": return "Ecuador";
                case "Bolívia": return "Bolivia";
                default: return nationality ?? "";
            }
        }

        void AddButton(string label, float x0, float y, float x1, Action action)
        {
            var button = BroadcastUi.Button(_detailRoot, "Action" + y + x0, label, action, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 11);
            BroadcastUi.Place(button.GetComponent<RectTransform>(), x0, y, x1, y + 0.095f);
        }

        void FinishCareer()
        {
            var parent = transform.parent;
            Destroy(gameObject);
            _finish?.Invoke();
            if (parent != null)
            {
                var setup = parent.Find("CareerSetupOverlay");
                if (setup != null) Destroy(setup.gameObject);
            }
        }
    }
}
