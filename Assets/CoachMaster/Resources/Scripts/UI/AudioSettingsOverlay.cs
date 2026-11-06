using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using CoachMaster.App;
using CoachMaster.Career;
using CoachMaster.Data;
using CoachMaster.UI.Scenes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    public sealed class AudioSettingsOverlay : MonoBehaviour
    {
        static readonly string[] Categories =
        {
            "Ecrã e gráficos", "Jogabilidade", "Dia de jogo", "Notificações", "Som", "Realismo", "Carreira e saves", "Acessibilidade", "Mods e dados", "Tema e cores", "Legal"
        };

        static readonly string[] PageNotes =
        {
            "Imagem e desempenho. A escala dos menus está em Acessibilidade.",
            "Controlos que alteram a preparação e o ritmo da partida.",
            "Pausas automáticas e informação visível durante o jogo.",
            "Avisos de carreira. Os acontecimentos continuam no histórico.",
            "Volumes separados; usa esquerda/direita no comando para ajustar.",
            "Ajustes da simulação guardados dentro da carreira.",
            "O jogo guarda cada carreira neste computador. O gestor abre o último save manual ou o automático.",
            "Escala do interface, movimento e idioma.",
            "Importa pastas de mods, consulta os formatos aceites e cria ficheiros modelo.",
            "Personaliza as cores principais e pré-visualiza a página inicial.",
            "Termos, licenças, regras de modding e avisos. Nada disto altera a carreira."
        };
        static readonly string[] EnglishPageNotes =
        {
            "Image and performance. Menu scale is in Accessibility.",
            "Controls that affect preparation and match pace.",
            "Automatic pauses and information shown during a match.",
            "Career alerts. Events remain in the history.",
            "Separate volume controls; use left/right on a controller.",
            "Simulation settings saved in the career.",
            "Each career is saved on this computer. Manage manual and automatic saves.",
            "Interface scale, motion and language.",
            "Import mod folders, read the accepted formats and create templates.",
            "Choose the main interface colours and preview the home screen.",
            "Terms, licences, modding rules and notices. None of this changes the career."
        };

        GameObject _root;
        GameObject _confirm;
        GameObject _saveManager;
        GameObject _controls;
        int _controlTab;
        InputActionRebindingExtensions.RebindingOperation _rebind;
        Text _rebindStatus;
        GameObject _musicManager;
        InputField _saveName;
        InputField _musicPath;
        RectTransform _musicRows;
        Text _musicStatus;
        Text _modStatus;
        Text _modPath;
        Image _modProgressFill;
        Button _importModButton;
        string _selectedModFolder;
        Coroutine _modImportRoutine;
        GameObject[] _pages;
        Button[] _tabs;
        Text _now;
        Text _feedback;
        Button _apply;
        Button _defaults;
        Button _saveCareer;
        Button _menu;
        Coroutine _settingsSaveRoutine;
        CareerState _career;
        int _page;
        int _restoreSlot = 1;
        string _pendingNamedSaveId;
        bool _pendingLoadManual;
        string _selectedSlot;
        Color[] _themeDraft;
        int _selectedThemeRole;
        Button[] _themeRoleButtons;
        Slider[] _themeSliders;
        Text[] _themeSliderValues;
        Image _themeColorChip;
        Button _themePreviewOpenButton;
        List<Text> _themePreviewTexts;
        HueSaturationWheel _themeWheel;
        int _legalDoc;
        Text _legalPageTitle;
        Text _legalPageNote;
        Text _legalHeading;
        Text _legalBody;
        Button[] _legalTabs;
        InputField _themeHexInput;
        GameObject _themePreviewModal;
        Image _themePreviewBackground;
        Image _themePreviewHeader;
        Image _themePreviewCard;
        Image _themePreviewNewsCard;
        Image _themePreviewSecondaryCard;
        Image _themePreviewAccentBar;
        Image _themePreviewAction;
        Text _themePreviewTitle;
        Text _themePreviewBody;
        Text _themePreviewActionText;
        readonly Dictionary<Button, Func<string>> _buttonValues = new Dictionary<Button, Func<string>>();
        readonly List<Action> _refreshValues = new List<Action>();

        public static bool IsVisible { get; private set; }
        public Canvas SettingsCanvas => _root == null ? null : _root.GetComponent<Canvas>();

        void Update()
        {
            GameAudio.Tick();
            if (Keyboard.current == null)
                return;
            if (!EventSystemTyping() && Keyboard.current.f11Key.wasPressedThisFrame)
            {
                DisplayPrefs.ToggleFullscreen();
                PersistSettings();
                if (_root != null && _root.activeSelf)
                    Refresh();
            }

        }

        public void Toggle()
        {
            if (_root != null && _root.activeSelf)
            {
                ApplyAndClose();
                return;
            }

            if (_root == null)
                Build();
            BeginEdit();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            IsVisible = true;
            ControlChrome.SettingsOpened();
            SelectPage(_page);
            Refresh();
        }

        void BeginEdit()
        {
            _career = CurrentCareer();
            _themeDraft = UiTheme.EditablePalette();
            if (_themeRoleButtons != null)
                RefreshThemeEditor();
            if (_feedback != null)
                _feedback.text = LocalizationManager.Current == GameLanguage.English
                    ? "Settings apply immediately and save automatically."
                    : "As definições aplicam-se e guardam-se automaticamente.";
        }

        void Build()
        {
            var canvasGo = new GameObject("GameSettings", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
            scaler.matchWidthOrHeight = 0.5f;
            _root = canvasGo;

            var dim = BroadcastUi.Solid(canvasGo.transform, "Dim", new Color(0f, 0f, 0f, 0f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            BroadcastUi.Catch(dim, ApplyAndClose);
            var card = BroadcastUi.Panel(canvasGo.transform, "SettingsCard", new Color(0.1f, 0.04f, 0.2f, 1f), true);
            BroadcastUi.Place(card.rectTransform, 0.025f, 0.035f, 0.975f, 0.965f);
            var title = BroadcastUi.Label(card.transform, "Title", "DEFINIÇÕES", 30, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.035f, 0.91f, 0.52f, 0.99f);

            var side = BroadcastUi.Panel(card.transform, "SettingsNavigation", new Color(1f, 1f, 1f, 0.045f));
            BroadcastUi.Place(side.rectTransform, 0.025f, 0.145f, 0.235f, 0.895f);
            var content = BroadcastUi.Panel(card.transform, "SettingsContent", new Color(1f, 1f, 1f, 0.045f));
            BroadcastUi.Place(content.rectTransform, 0.255f, 0.145f, 0.975f, 0.895f);

            _tabs = new Button[Categories.Length];
            _pages = new GameObject[Categories.Length];
            var tabStep = 0.90f / Categories.Length;
            var tabHeight = tabStep - 0.012f;
            for (var i = 0; i < Categories.Length; i++)
            {
                var index = i;
                var top = 0.96f - i * tabStep;
                _tabs[i] = BroadcastUi.Button(side.transform, "Category" + i, CategoryName(i), () => SelectPage(index),
                    new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 12);
                BroadcastUi.Place(_tabs[i].GetComponent<RectTransform>(), 0.055f, top - tabHeight, 0.945f, top);
                var tabLabel = _tabs[i].GetComponentInChildren<Text>();
                if (tabLabel != null)
                {
                    tabLabel.resizeTextForBestFit = true;
                    tabLabel.resizeTextMinSize = 9;
                    tabLabel.resizeTextMaxSize = 13;
                }
            }

            BuildDisplayPage(content.transform);
            BuildGameplayPage(content.transform);
            BuildMatchPage(content.transform);
            BuildNotificationPage(content.transform);
            BuildAudioPage(content.transform);
            BuildRealismPage(content.transform);
            BuildCareerPage(content.transform);
            BuildAccessibilityPage(content.transform);
            BuildModsPage(content.transform);
            BuildThemePage(content.transform);
            BuildLegalPage(content.transform);

            _feedback = BroadcastUi.Label(card.transform, "SettingsFeedback", "", 12, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(_feedback.rectTransform, 0.27f, 0.105f, 0.95f, 0.145f);
            _menu = FooterButton(card.transform, "MainMenu", "MENU", LeaveToMenu, 0.025f, 0.025f, 0.19f, 0.09f, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white);
            _defaults = FooterButton(card.transform, "Defaults", "REPOR PADRÃO", RestoreDefaults, 0.205f, 0.025f, 0.4f, 0.09f, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white);
            _saveCareer = FooterButton(card.transform, "SaveCareer", "GUARDAR CARREIRA", SaveCareer, 0.415f, 0.025f, 0.7f, 0.09f, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white);
            _apply = FooterButton(card.transform, "CloseSettings", "FECHAR", ApplyAndClose, 0.82f, 0.025f, 0.975f, 0.09f, UiTheme.Accent, UiTheme.ButtonText);
            GameRoot.ApplyUiScale();
            _root.SetActive(false);
            ApplyCareerButtons();
        }

        void BuildDisplayPage(Transform parent)
        {
            var page = Page(parent, 0, "ECRÃ E GRÁFICOS");
            AddOption(page.transform, "Mode", "Modo de ecrã", "Janela, janela sem moldura ou ecrã inteiro exclusivo.", () => DisplayPrefs.WindowLabel, DisplayPrefs.CycleWindowMode, 0.76f);
            AddOption(page.transform, "Resolution", "Resolução", "A resolução escolhida é usada no modo de janela.", () => DisplayPrefs.ResolutionLabel, DisplayPrefs.CycleResolution, 0.65f);
            AddOption(page.transform, "VSync", "Sincronização vertical", "Reduz rasgos de imagem; pode limitar os FPS à frequência do ecrã.", () => DisplayPrefs.VsyncLabel, DisplayPrefs.ToggleVsync, 0.54f);
            AddOption(page.transform, "FPS", "Limite de fotogramas", "30, 60, 120, 144, 165, 240 ou sem limite.", () => DisplayPrefs.FrameRateLabel, DisplayPrefs.CycleFrameRate, 0.43f);
            AddOption(page.transform, "Quality", "Qualidade", "Usa os níveis de qualidade disponíveis neste projeto Unity.", () => DisplayPrefs.QualityLabel, DisplayPrefs.CycleQuality, 0.32f);
            AddOption(page.transform, "Background", "Imagem de fundo", "Automático, fundo principal, uma liga, ou um ficheiro teu. O ficheiro tem de ser PNG, JPG ou JPEG.", () => BackgroundPrefs.Label, () => { BackgroundPrefs.Cycle(); Refresh(); }, 0.21f);
            var format = BroadcastUi.Label(page.transform, "BackgroundFormat", LocalizationManager.Current == GameLanguage.English ? "PNG, JPG or JPEG." : "PNG, JPG ou JPEG.", 12, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(format.rectTransform, 0.04f, 0.08f, 0.5f, 0.16f);
            var own = BroadcastUi.Button(page.transform, "OwnBackground", LocalizationManager.Current == GameLanguage.English ? "ADD BACKGROUND" : "ADICIONAR FUNDO", ChooseUserBackground, UiTheme.Accent, UiTheme.ButtonText, 13);
            BroadcastUi.Place(own.GetComponent<RectTransform>(), 0.52f, 0.08f, 0.75f, 0.16f);
            var remove = BroadcastUi.Button(page.transform, "RemoveBackground", LocalizationManager.Current == GameLanguage.English ? "DELETE MY BACKGROUND" : "APAGAR O MEU FUNDO", DeleteUserBackground,
                new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 12);
            remove.interactable = BackgroundPrefs.HasUserImage;
            BroadcastUi.Place(remove.GetComponent<RectTransform>(), 0.77f, 0.08f, 0.96f, 0.16f);
        }

        void BuildGameplayPage(Transform parent)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var page = Page(parent, 1, english ? "GAMEPLAY" : "JOGABILIDADE");
            AddOption(page.transform, "AutoXI", "Escolha automática do onze", "Ignora o onze guardado e escolhe a equipa a partir da formação e posições.", () => OnOff(SettingsPrefs.AutomaticSquadSelection), () => SettingsPrefs.AutomaticSquadSelection = !SettingsPrefs.AutomaticSquadSelection, 0.76f);
            AddOption(page.transform, "Training", english ? "Automatic training" : "Treino automático",
                english ? "On: general development and individual focuses apply. Off: only assigned training focuses take effect." : "Ligado aplica desenvolvimento geral e focos individuais; desligado, só os focos atribuídos produzem treino.",
                () => OnOff(SettingsPrefs.AutomaticTraining), () => SettingsPrefs.AutomaticTraining = !SettingsPrefs.AutomaticTraining, 0.63f);
            AddOption(page.transform, "Speed", "Velocidade inicial do jogo", "Também pode ser alterada durante a partida.", () => SettingsPrefs.MatchSpeedLabel, CycleMatchSpeed, 0.50f);
            AddOption(page.transform, "Highlights", english ? "Match event cards" : "Momentos apresentados",
                english ? "Choose which event cards appear at 8×; this does not change the match result." : "Escolhe que cartões de acontecimentos aparecem a 8×; não altera o resultado da partida.",
                () => SettingsPrefs.MatchHighlightsLabel, () => SettingsPrefs.MatchHighlights = (SettingsPrefs.MatchHighlights + 1) % 4, 0.37f);
            AddText(page.transform, "MatchSpeedHelp", "O ritmo e os momentos escolhidos aplicam-se à apresentação; não alteram o resultado matemático da partida.", 15, UiTheme.Muted, 0.12f, 0.22f);
            var commands = BroadcastUi.Button(page.transform, "Commands", english ? "CONTROLS" : "CONTROLOS", OpenControls, UiTheme.Accent, UiTheme.ButtonText, 14);
            BroadcastUi.Place(commands.GetComponent<RectTransform>(), 0.04f, 0.02f, 0.48f, 0.11f);
        }

        void CycleMatchSpeed()
        {
            SettingsPrefs.MatchSpeedIndex = (SettingsPrefs.MatchSpeedIndex + 1) % 5;
            var career = CurrentCareer();
            if (career != null)
                career.MatchSpeedIndex = SettingsPrefs.MatchSpeedIndex;
        }

        void BuildMatchPage(Transform parent)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var page = Page(parent, 2, english ? "MATCHDAY" : "DIA DE JOGO");
            AddOption(page.transform, "PauseGoal", "Pausar depois de um golo", "A pausa começa depois de a bola entrar e o golo ser confirmado.", () => OnOff(SettingsPrefs.PauseOnGoal), () => SettingsPrefs.PauseOnGoal = !SettingsPrefs.PauseOnGoal, 0.76f);
            AddOption(page.transform, "PauseRed", "Pausar depois de um vermelho", "Abre a pausa para poderes rever a equipa e as táticas.", () => OnOff(SettingsPrefs.PauseOnRedCard), () => SettingsPrefs.PauseOnRedCard = !SettingsPrefs.PauseOnRedCard, 0.63f);
            AddOption(page.transform, "Ratings", english ? "Live ratings" : "Pontuações ao vivo",
                english ? "Shows or hides the player ratings page during a match." : "Mostra ou oculta a página de classificações dos jogadores durante a partida.",
                () => OnOff(SettingsPrefs.LiveRatings), () => SettingsPrefs.LiveRatings = !SettingsPrefs.LiveRatings, 0.50f);
            AddText(page.transform, "HalfTimeInfo", "O jogo já para sempre no intervalo para a conversa e escolhas do balneário.", 15, UiTheme.Muted, 0.10f, 0.24f);
        }

        void BuildNotificationPage(Transform parent)
        {
            var page = Page(parent, 3, "NOTIFICAÇÕES");
            AddOption(page.transform, "Alerts", "Avisos de consequências", "Ocultar o aviso não apaga a decisão do histórico da carreira.", () => OnOff(SettingsPrefs.DecisionAlerts), () => SettingsPrefs.DecisionAlerts = !SettingsPrefs.DecisionAlerts, 0.76f);
            AddOption(page.transform, "AlertDuration", "Duração do aviso", "Controla quanto tempo fica visível a mensagem de consequência.", () => SettingsPrefs.AlertDurationLabel, () => SettingsPrefs.AlertDurationIndex = (SettingsPrefs.AlertDurationIndex + 1) % 3, 0.63f);
            AddText(page.transform, "NotificationHelp", "A versão atual tem avisos de carreira e acontecimentos importantes. Não mostra categorias sem eventos próprios implementados.", 15, UiTheme.Muted, 0.10f, 0.48f);
        }

        void BuildAudioPage(Transform parent)
        {
            var page = Page(parent, 4, "SOM E MÚSICA");
            var pageNote = page.transform.Find("PageNote");
            if (pageNote != null)
                pageNote.gameObject.SetActive(false);
            _now = BroadcastUi.Label(page.transform, "Now", "", 13, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(_now.rectTransform, 0.04f, 0.87f, 0.94f, 0.92f);
            var previous = BroadcastUi.Button(page.transform, "PreviousTrack", "ANTERIOR", () => { GameAudio.PreviousTrack(); Refresh(); }, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 12);
            BroadcastUi.Place(previous.GetComponent<RectTransform>(), 0.04f, 0.80f, 0.32f, 0.86f);
            var repeat = BroadcastUi.Button(page.transform, "RepeatTrack", "REPETIR", () => { GameAudio.Repeat = !GameAudio.Repeat; PersistSettings(); Refresh(); }, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 12);
            BroadcastUi.Place(repeat.GetComponent<RectTransform>(), 0.36f, 0.80f, 0.64f, 0.86f);
            _buttonValues[repeat] = () => GameAudio.Repeat ? "REPETIÇÃO LIGADA" : "REPETIR";
            var next = BroadcastUi.Button(page.transform, "NextTrack", "SEGUINTE", () => { GameAudio.NextTrack(); PersistSettings(); Refresh(); }, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 12);
            BroadcastUi.Place(next.GetComponent<RectTransform>(), 0.68f, 0.80f, 0.96f, 0.86f);
            AddVolume(page.transform, "Master", "Volume geral", "Nível comum a todos os sons.", GameAudio.MasterVolume, GameAudio.SetMasterVolume, 0.68f);
            AddVolume(page.transform, "Music", "Música", "Música dos menus.", GameAudio.MusicVolume, GameAudio.SetMusicVolume, 0.57f);
            AddVolume(page.transform, "MatchEffects", "Efeitos do jogo", "Apito, bola, cartões e outros efeitos da partida.", GameAudio.EffectVolume, GameAudio.SetEffectVolume, 0.46f);
            AddVolume(page.transform, "Crowd", "Público", "Ambiente e reação das bancadas.", GameAudio.CrowdVolume, GameAudio.SetCrowdVolume, 0.35f);
            AddVolume(page.transform, "MenuSounds", "Interface", "Cliques e sons dos menus.", GameAudio.MenuVolume, GameAudio.SetMenuVolume, 0.24f);
            var manage = BroadcastUi.Button(page.transform, "ManageMusic", LocalizationManager.Current == GameLanguage.English ? "MANAGE MUSIC" : "GERIR MÚSICA", OpenMusicManager, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 12);
            BroadcastUi.Place(manage.GetComponent<RectTransform>(), 0.04f, 0.145f, 0.49f, 0.215f);
            _buttonValues[manage] = () => LocalizationManager.Current == GameLanguage.English ? "MANAGE MUSIC" : "GERIR MÚSICA";
            AddCompactOption(page.transform, "MuteBackground", "Em segundo plano", () => OnOff(SettingsPrefs.MuteWhenBackground), () => SettingsPrefs.MuteWhenBackground = !SettingsPrefs.MuteWhenBackground, 0.04f, 0.04f, 0.49f);
            AddCompactOption(page.transform, "MuteAll", "Silenciar tudo", () => OnOff(GameAudio.MuteAll), () => GameAudio.SetMuteAll(!GameAudio.MuteAll), 0.51f, 0.04f, 0.96f);
        }

        void OpenMusicManager()
        {
            if (_musicManager != null)
                Destroy(_musicManager);
            _musicManager = new GameObject("MusicManager", typeof(RectTransform));
            _musicManager.transform.SetParent(_root.transform, false);
            var root = _musicManager.GetComponent<RectTransform>();
            BroadcastUi.Stretch(root);
            var dim = BroadcastUi.Solid(root, "Dim", new Color(0f, 0f, 0f, 0f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            BroadcastUi.Catch(dim, CloseMusicManager);
            var card = BroadcastUi.Panel(root, "MusicManagerCard", new Color(0.1f, 0.04f, 0.2f, 1f));
            BroadcastUi.Place(card.rectTransform, 0.12f, 0.08f, 0.88f, 0.92f);
            var english = LocalizationManager.Current == GameLanguage.English;
            var title = BroadcastUi.Label(card.transform, "Title", english ? "MUSIC LIBRARY" : "BIBLIOTECA DE MÚSICA", 22, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.035f, 0.91f, 0.72f, 0.99f);
            var help = BroadcastUi.Label(card.transform, "Help", english
                ? "Choose a local MP3, WAV, OGG or AIFF file. Imported tracks are copied into the game's music folder."
                : "Escolhe um ficheiro MP3, WAV, OGG ou AIFF. As faixas importadas são copiadas para a pasta de música do jogo.", 12, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(help.rectTransform, 0.035f, 0.85f, 0.96f, 0.91f);
            _musicPath = UiFactory.Input(card.transform, english ? "Optional file path" : "Caminho do ficheiro (opcional)");
            _musicPath.characterLimit = 1024;
            _musicPath.textComponent.fontSize = 13;
            _musicPath.placeholder.GetComponent<Text>().fontSize = 13;
            BroadcastUi.Place(_musicPath.GetComponent<RectTransform>(), 0.035f, 0.775f, 0.69f, 0.84f);
            var browse = BroadcastUi.Button(card.transform, "BrowseMusic", english ? "BROWSE" : "PROCURAR", ChooseMusicFile, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 12);
            BroadcastUi.Place(browse.GetComponent<RectTransform>(), 0.71f, 0.775f, 0.96f, 0.84f);
            var add = BroadcastUi.Button(card.transform, "ImportMusic", english ? "ADD TRACK" : "ADICIONAR FAIXA", ImportMusicFile, UiTheme.Accent, UiTheme.ButtonText, 12);
            BroadcastUi.Place(add.GetComponent<RectTransform>(), 0.71f, 0.695f, 0.96f, 0.765f);
            _musicStatus = BroadcastUi.Label(card.transform, "Status", "", 12, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(_musicStatus.rectTransform, 0.035f, 0.695f, 0.69f, 0.765f);
            var scroll = BroadcastUi.Scroll(card.transform, "MusicTracks", out _musicRows);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.025f, 0.16f, 0.975f, 0.68f);
            var close = BroadcastUi.Button(card.transform, "CloseMusicManager", english ? "DONE" : "CONCLUÍDO", CloseMusicManager, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 13);
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.72f, 0.055f, 0.96f, 0.13f);
            RefreshMusicManager();
        }

        void RefreshMusicManager()
        {
            if (_musicRows == null)
                return;
            for (var i = _musicRows.childCount - 1; i >= 0; i--)
                Destroy(_musicRows.GetChild(i).gameObject);
            var tracks = GameAudio.MusicLibrary();
            if (tracks.Count == 0)
            {
                var empty = BroadcastUi.Label(_musicRows, "Empty", LocalizationManager.Current == GameLanguage.English ? "No music tracks found." : "Não foram encontradas faixas de música.", 13, UiTheme.Muted);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
                return;
            }

            var english = LocalizationManager.Current == GameLanguage.English;
            foreach (var track in tracks)
            {
                var row = BroadcastUi.Panel(_musicRows, "Track_" + track.Id, new Color(1f, 1f, 1f, 0.055f));
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 50f;
                var label = BroadcastUi.Label(row.transform, "Name", (track.IsUserTrack ? (english ? "YOUR TRACK  ·  " : "TUA FAIXA  ·  ") : "") + track.Name, 13, Color.white, TextAnchor.MiddleLeft);
                BroadcastUi.Place(label.rectTransform, 0.025f, 0.04f, 0.63f, 0.96f);
                var toggle = BroadcastUi.Button(row.transform, "Enabled", track.Enabled
                    ? (english ? "ON" : "LIGADA") : (english ? "OFF" : "DESLIGADA"), () =>
                    {
                        GameAudio.SetMusicTrackEnabled(track.Id, !track.Enabled);
                        RefreshMusicManager();
                    }, track.Enabled ? UiTheme.Accent : new Color(0.22f, 0.12f, 0.38f, 1f), track.Enabled ? UiTheme.ButtonText : Color.white, 11);
                BroadcastUi.Place(toggle.GetComponent<RectTransform>(), 0.66f, 0.14f, track.IsUserTrack ? 0.81f : 0.96f, 0.86f);
                if (track.IsUserTrack)
                {
                    var remove = BroadcastUi.Button(row.transform, "Remove", english ? "REMOVE" : "REMOVER", () =>
                    {
                        GameAudio.RemoveUserMusic(track.Id);
                        RefreshMusicManager();
                    }, new Color(0.48f, 0.12f, 0.2f, 1f), Color.white, 10);
                    BroadcastUi.Place(remove.GetComponent<RectTransform>(), 0.83f, 0.14f, 0.98f, 0.86f);
                }
            }
        }

        void ChooseMusicFile()
        {
            var path = WindowsMusicPicker.Open();
            if (string.IsNullOrEmpty(path))
            {
                if (_musicStatus != null)
                    _musicStatus.text = LocalizationManager.Current == GameLanguage.English
                        ? "Paste a file path if the file picker is unavailable."
                        : "Cola o caminho do ficheiro se a janela de seleção não estiver disponível.";
                return;
            }
            if (_musicPath != null)
                _musicPath.text = path;
        }

        void ImportMusicFile()
        {
            var path = WindowsMusicPicker.Open();
            if (string.IsNullOrEmpty(path))
            {
                if (_musicStatus != null)
                    _musicStatus.text = LocalizationManager.Current == GameLanguage.English
                        ? "No file was chosen."
                        : "Nenhum ficheiro foi escolhido.";
                return;
            }
            if (_musicPath != null)
                _musicPath.text = path;
            if (_musicStatus != null)
                _musicStatus.text = LocalizationManager.Current == GameLanguage.English ? "Importing…" : "A importar…";
            GameAudio.ImportUserMusic(this, path, (success, result) =>
            {
                if (_musicStatus != null)
                    _musicStatus.text = success
                        ? (LocalizationManager.Current == GameLanguage.English ? "Added: " : "Adicionada: ") + result
                        : LocalizationManager.Current == GameLanguage.English ? result : "Não foi possível importar a faixa. Verifica o caminho e o formato.";
                if (success)
                {
                    _musicPath.text = "";
                    RefreshMusicManager();
                }
            });
        }

        void CloseMusicManager()
        {
            if (_musicManager != null)
                _musicManager.SetActive(false);
            _musicPath = null;
            _musicRows = null;
            _musicStatus = null;
        }

        void BuildRealismPage(Transform parent)
        {
            var page = Page(parent, 5, "SIMULAÇÃO E REALISMO");
            AddOption(page.transform, "RealismPreset", "Predefinição", "Casual e Hardcore ajustam em conjunto lesões e rigor disciplinar.", CurrentPresetLabel, CycleRealismPreset, 0.76f);
            AddOption(page.transform, "Injuries", "Frequência de lesões", "Afeta a probabilidade de lesão em colisões, considerando fadiga e traits.", CurrentInjuryLabel, CycleInjury, 0.63f);
            AddOption(page.transform, "Referee", "Rigor disciplinar", "Afeta a frequência de amarelos depois de faltas.", CurrentRefereeLabel, CycleReferee, 0.50f);
            AddText(page.transform, "RealismHelp", "Estas opções ficam guardadas na carreira e aplicam-se a partidas novas. Só alteram os sistemas já suportados.", 15, UiTheme.Muted, 0.12f, 0.34f);
        }

        void BuildCareerPage(Transform parent)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var page = Page(parent, 6, english ? "CAREER & SAVES" : "CARREIRA E SAVES");
            var open = BroadcastUi.Button(page.transform, "OpenSaves", english ? "SAVE MANAGER" : "GESTOR DE SAVES", OpenSaveManager, UiTheme.Accent, UiTheme.ButtonText, 16);
            BroadcastUi.Place(open.GetComponent<RectTransform>(), 0.18f, 0.72f, 0.82f, 0.84f);
            var online = BroadcastUi.Button(page.transform, "OpenOnline", "ONLINE", OpenOnlineFromSettings, new Color(0.10f, 0.24f, 0.38f, 1f), Color.white, 16);
            BroadcastUi.Place(online.GetComponent<RectTransform>(), 0.18f, 0.56f, 0.82f, 0.68f);
            AddText(page.transform, "SaveHelp", english
                ? "Each career has its own save. The manager opens your most recent manual or automatic save; you can rename or delete saved careers."
                : "Cada carreira tem o seu save. O gestor abre a gravação manual ou automática mais recente; podes mudar o nome ou apagar carreiras guardadas.",
                15, UiTheme.Muted, 0.18f, 0.24f);
        }

        void BuildAccessibilityPage(Transform parent)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var page = Page(parent, 7, english ? "INTERFACE & ACCESSIBILITY" : "INTERFACE E ACESSIBILIDADE");
            AddOption(page.transform, "Scale", english ? "Interface scale" : "Escala do interface", english ? "Changes menu and text scale; player cards keep their own size." : "Aumenta ou reduz os menus e texto; as cartas mantêm o tamanho próprio.", () => Percent(SettingsPrefs.UiScale), () => { SettingsPrefs.UiScaleIndex = (SettingsPrefs.UiScaleIndex + 1) % 5; GameRoot.ApplyUiScale(); }, 0.76f);
            AddOption(page.transform, "Motion", english ? "Reduce animations" : "Reduzir animações", english ? "Shortens menu fades and loading transitions." : "Encurta os fades dos menus e as transições de carregamento.", () => OnOff(SettingsPrefs.ReduceMotion), () => SettingsPrefs.ReduceMotion = !SettingsPrefs.ReduceMotion, 0.63f);
            AddOption(page.transform, "Language", english ? "Language" : "Idioma", english ? "Choose the game language." : "Escolhe o idioma do jogo.", LanguageLabel, CycleLanguage, 0.50f);
            AddOption(page.transform, "ControllerHints", english ? "Control hints" : "Ajuda de controlos", english ? "Show or hide the navigation guide on the player conversations screen." : "Mostra ou oculta o guia de navegação no ecrã das conversas com jogadores.", () => OnOff(SettingsPrefs.ControllerHints), () => SettingsPrefs.ControllerHints = !SettingsPrefs.ControllerHints, 0.37f);
            AddOption(page.transform, "SkipDayConfirmation",
                english ? "Confirm before skipping a day" : "Confirmar antes de saltar o dia",
                english ? "Restore the confirmation prompt after choosing not to show it again." : "Volta a mostrar a confirmação se escolheste não a voltar a mostrar.",
                () => OnOff(SettingsPrefs.SkipDayConfirmation), () => SettingsPrefs.SkipDayConfirmation = !SettingsPrefs.SkipDayConfirmation, 0.24f);
        }

        void BuildModsPage(Transform parent)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var page = Page(parent, 8, english ? "MODS & DATABASE" : "MODS E BASE DE DADOS");
            var guidePanel = BroadcastUi.Panel(page.transform, "ModsGuidePanel", new Color(1f, 1f, 1f, 0.045f));
            BroadcastUi.Place(guidePanel.rectTransform, 0.035f, 0.16f, 0.59f, 0.80f);
            var guide = BroadcastUi.Label(guidePanel.transform, "ModsGuide",
                english
                    ? "IMPORT A COMPLETE MOD FOLDER\nChoose the folder that contains the mod files. A nested Mods folder is also detected. Supported files are copied into this game's Mods folder; matching names are replaced and unrelated files stay untouched.\n\nFILES\nclubs.txt / equipas.txt\nplayers.txt / jogadores.txt\nmanagers.txt / treinadores.txt\nstaff.txt\nsponsors.txt\n\nIMAGES\ncaras/ or faces/\nemblemas/, badges/ or crests/\npatrocinadores/ or sponsors/\nUse PNG, JPG or JPEG. Image file names are the portrait, crest or logo keys.\n\nFor bulk editing, use the exact field order in MODS-LEIA-ME.txt. Rows are UTF-8, one record per line, fields separated by |. Use IDs from Database Preview; current names also work. # starts a comment. Missing optional values keep the existing value. Import the folder, then load or start a career to apply its database changes."
                    : "IMPORTAR UMA PASTA DE MOD COMPLETA\nEscolhe a pasta que contém os ficheiros do mod. Também é detetada uma pasta Mods no interior. Os ficheiros suportados são copiados para a pasta Mods do jogo; ficheiros com o mesmo nome são substituídos e os restantes mantêm-se.\n\nFICHEIROS\nequipas.txt / clubs.txt\njogadores.txt / players.txt\ntreinadores.txt / managers.txt\nstaff.txt\nsponsors.txt\n\nIMAGENS\ncaras/ ou faces/\nemblemas/, badges/ ou crests/\npatrocinadores/ ou sponsors/\nUsa PNG, JPG ou JPEG. O nome do ficheiro é a chave do rosto, emblema ou logótipo.\n\nPara editar em lote, segue a ordem exata em MODS-LEIA-ME.txt. As linhas são UTF-8, um registo por linha, campos separados por |. Usa os IDs da Pré-visualização da base de dados; os nomes atuais também funcionam. # inicia um comentário. Campos opcionais vazios mantêm o valor existente. Importa a pasta e depois carrega ou inicia uma carreira para aplicar as alterações.",
                12, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.Place(guide.rectTransform, 0.025f, 0.025f, 0.975f, 0.975f);

            var pathTitle = BroadcastUi.Label(page.transform, "ModsPathTitle", english ? "GAME MODS FOLDER" : "PASTA DE MODS DO JOGO", 12, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(pathTitle.rectTransform, 0.63f, 0.76f, 0.96f, 0.81f);
            _modPath = BroadcastUi.Label(page.transform, "ModsPath", ModFolder.Root(), 11, UiTheme.Muted, TextAnchor.UpperLeft);
            BroadcastUi.Place(_modPath.rectTransform, 0.63f, 0.68f, 0.96f, 0.76f);

            var choose = BroadcastUi.Button(page.transform, "ChooseModFolder", english ? "CHOOSE MOD FOLDER" : "ESCOLHER PASTA DE MODS", ChooseModFolder, UiTheme.Accent, UiTheme.ButtonText, 13);
            BroadcastUi.Place(choose.GetComponent<RectTransform>(), 0.63f, 0.57f, 0.96f, 0.66f);
            _importModButton = BroadcastUi.Button(page.transform, "ImportModFolder", english ? "IMPORT FOLDER" : "IMPORTAR PASTA", ImportModFolder, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 13);
            BroadcastUi.Place(_importModButton.GetComponent<RectTransform>(), 0.63f, 0.46f, 0.96f, 0.55f);
            var templates = BroadcastUi.Button(page.transform, "CreateModTemplates", english ? "CREATE GUIDE & TEMPLATES" : "CRIAR GUIA E MODELOS", CreateModTemplates, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 12);
            BroadcastUi.Place(templates.GetComponent<RectTransform>(), 0.63f, 0.35f, 0.96f, 0.44f);
            var open = BroadcastUi.Button(page.transform, "OpenModsFolder", english ? "OPEN GAME MODS FOLDER" : "ABRIR PASTA DE MODS", OpenModsFolder, new Color(0.10f, 0.24f, 0.38f, 1f), Color.white, 12);
            BroadcastUi.Place(open.GetComponent<RectTransform>(), 0.63f, 0.24f, 0.96f, 0.33f);

            var track = BroadcastUi.Panel(page.transform, "ModsProgressTrack", new Color(1f, 1f, 1f, 0.12f));
            BroadcastUi.Place(track.rectTransform, 0.63f, 0.16f, 0.96f, 0.205f);
            var fill = BroadcastUi.Panel(track.transform, "ModsProgressFill", UiTheme.Accent);
            BroadcastUi.Place(fill.rectTransform, 0f, 0f, 0f, 1f);
            _modProgressFill = fill;
            _modStatus = BroadcastUi.Label(page.transform, "ModsStatus", english ? "Choose a folder to begin." : "Escolhe uma pasta para começar.", 12, UiTheme.Muted, TextAnchor.UpperLeft);
            BroadcastUi.Place(_modStatus.rectTransform, 0.63f, 0.06f, 0.96f, 0.15f);
        }

        void BuildThemePage(Transform parent)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var page = Page(parent, 9, english ? "THEME COLOURS" : "CORES DO TEMA");
            _themeDraft = UiTheme.EditablePalette();
            _selectedThemeRole = Mathf.Clamp(_selectedThemeRole, 0, _themeDraft.Length - 1);
            _themeRoleButtons = new Button[_themeDraft.Length];

            var roleNames = english
                ? new[] { "BACKGROUND", "PANELS", "ACCENT", "TEXT" }
                : new[] { "FUNDO", "PAINÉIS", "DESTAQUE", "TEXTO" };
            for (var i = 0; i < roleNames.Length; i++)
            {
                var role = i;
                var x0 = 0.035f + i * 0.117f;
                _themeRoleButtons[i] = BroadcastUi.Button(page.transform, "ThemeRole" + i, roleNames[i],
                    () => SelectThemeRole(role), _themeDraft[i], ThemeButtonText(_themeDraft[i]), 10);
                BroadcastUi.Place(_themeRoleButtons[i].GetComponent<RectTransform>(), x0, 0.72f, x0 + 0.11f, 0.81f);
            }

            var selectedLabel = BroadcastUi.Label(page.transform, "ThemeSelectedRole",
                english ? "EDIT SELECTED COLOUR" : "EDITAR COR",
                12, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(selectedLabel.rectTransform, 0.04f, 0.64f, 0.48f, 0.70f);
            var colorChip = BroadcastUi.Panel(page.transform, "ThemeColorChip", _themeDraft[_selectedThemeRole]);
            BroadcastUi.Place(colorChip.rectTransform, 0.39f, 0.63f, 0.47f, 0.70f);
            _themeColorChip = colorChip;

            var wheelObject = new GameObject("HueSaturationWheel", typeof(RectTransform), typeof(CanvasRenderer), typeof(HueSaturationWheel));
            var wheelRect = wheelObject.GetComponent<RectTransform>();
            wheelRect.SetParent(page.transform, false);
            wheelRect.anchorMin = wheelRect.anchorMax = new Vector2(0.26f, 0.51f);
            wheelRect.pivot = new Vector2(0.5f, 0.5f);
            wheelRect.sizeDelta = new Vector2(200f, 200f);
            _themeWheel = wheelObject.GetComponent<HueSaturationWheel>();
            _themeWheel.SetColorChanged(SetThemeHueSaturation);
            var markerObject = new GameObject("WheelSelection", typeof(RectTransform), typeof(Image), typeof(Outline));
            var wheelMarkerRect = markerObject.GetComponent<RectTransform>();
            wheelMarkerRect.SetParent(wheelRect, false);
            wheelMarkerRect.sizeDelta = new Vector2(18f, 18f);
            var markerImage = markerObject.GetComponent<Image>();
            markerImage.sprite = BroadcastUi.Circle;
            markerImage.color = Color.white;
            markerImage.raycastTarget = false;
            var markerOutline = markerObject.GetComponent<Outline>();
            markerOutline.effectColor = new Color(0.04f, 0.04f, 0.07f, 1f);
            markerOutline.effectDistance = new Vector2(2f, 2f);

            _themeSliders = new Slider[1];
            _themeSliderValues = new Text[1];
            AddThemeSlider(page.transform, 3, english ? "BRIGHTNESS" : "LUMINOSIDADE", 0.20f);
            var hexTitle = BroadcastUi.Label(page.transform, "HexColourTitle", english ? "HEX COLOUR" : "COR HEX",
                10, UiTheme.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(hexTitle.rectTransform, 0.04f, 0.13f, 0.16f, 0.19f);
            _themeHexInput = UiFactory.Input(page.transform, "#RRGGBB");
            _themeHexInput.characterLimit = 7;
            BroadcastUi.Place(_themeHexInput.GetComponent<RectTransform>(), 0.16f, 0.12f, 0.34f, 0.19f);
            _themeHexInput.onEndEdit.AddListener(ApplyThemeHex);

            var apply = BroadcastUi.Button(page.transform, "ApplyTheme", english ? "APPLY THEME" : "APLICAR TEMA",
                ApplyThemeDraft, UiTheme.Accent, UiTheme.ButtonText, 12);
            BroadcastUi.Place(apply.GetComponent<RectTransform>(), 0.35f, 0.12f, 0.49f, 0.19f);
            var reset = BroadcastUi.Button(page.transform, "ResetTheme", english ? "RESTORE DEFAULT COLOURS" : "REPOR CORES PADRÃO",
                ResetThemeDraft, UiTheme.PanelAlt, UiTheme.Text, 11);
            BroadcastUi.Place(reset.GetComponent<RectTransform>(), 0.04f, 0.07f, 0.49f, 0.12f);
            var previewButton = BroadcastUi.Button(page.transform, "PreviewHomeTheme",
                english ? "PREVIEW HOME TAB" : "PRÉ-VISUALIZAR INÍCIO", OpenThemeHomePreview,
                _themeDraft[2], ThemeButtonText(_themeDraft[2]), 15);
            _themePreviewOpenButton = previewButton;
            BroadcastUi.Place(previewButton.GetComponent<RectTransform>(), 0.56f, 0.42f, 0.95f, 0.56f);
            var previewHelp = BroadcastUi.Label(page.transform, "PreviewHomeHelp",
                english ? "See your current colours on the career home screen." : "Vê as cores escolhidas no ecrã inicial da carreira.",
                14, UiTheme.Muted, TextAnchor.MiddleCenter);
            BroadcastUi.Place(previewHelp.rectTransform, 0.56f, 0.32f, 0.95f, 0.41f);

            RefreshThemeEditor();
        }

        void AddThemeSlider(Transform parent, int channel, string label, float y)
        {
            var row = BroadcastUi.Panel(parent, "ThemeChannel" + channel, new Color(1f, 1f, 1f, 0.045f));
            BroadcastUi.Place(row.rectTransform, 0.035f, y, 0.49f, y + 0.105f);
            var title = BroadcastUi.Label(row.transform, "ChannelName", label, 11, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.025f, 0.48f, 0.28f, 0.96f);
            var value = BroadcastUi.Label(row.transform, "ChannelValue", "0", 11, UiTheme.Accent, TextAnchor.MiddleRight, FontStyle.Bold);
            BroadcastUi.Place(value.rectTransform, 0.82f, 0.48f, 0.97f, 0.96f);
            var sliderIndex = channel == 3 ? 0 : channel;
            _themeSliderValues[sliderIndex] = value;

            var sliderRoot = new GameObject("ChannelSlider", typeof(RectTransform), typeof(Slider));
            var sliderRect = sliderRoot.GetComponent<RectTransform>();
            sliderRect.SetParent(row.transform, false);
            BroadcastUi.Place(sliderRect, 0.025f, 0.12f, 0.975f, 0.43f);
            var track = BroadcastUi.Solid(sliderRoot.transform, "Track", new Color(1f, 1f, 1f, 0.18f));
            track.sprite = BroadcastUi.Round;
            track.type = Image.Type.Sliced;
            BroadcastUi.Stretch(track.rectTransform);
            var fill = BroadcastUi.Solid(sliderRoot.transform, "Fill", UiTheme.Accent);
            fill.sprite = BroadcastUi.Round;
            fill.type = Image.Type.Sliced;
            fill.raycastTarget = false;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.offsetMin = new Vector2(1f, 1f);
            fill.rectTransform.offsetMax = new Vector2(-1f, -1f);
            var handle = BroadcastUi.Solid(sliderRoot.transform, "Handle", UiTheme.Text, true);
            handle.sprite = BroadcastUi.Circle;
            handle.rectTransform.sizeDelta = new Vector2(18f, 18f);

            var slider = sliderRoot.GetComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.onValueChanged.AddListener(amount => SetThemeComponent(channel, amount));
            _themeSliders[channel == 3 ? 0 : channel] = slider;
        }

        void SelectThemeRole(int role)
        {
            _selectedThemeRole = Mathf.Clamp(role, 0, _themeDraft.Length - 1);
            RefreshThemeEditor();
        }

        void SetThemeComponent(int channel, float amount)
        {
            if (_themeDraft == null || _themeDraft.Length < 4)
                return;
            var color = _themeDraft[_selectedThemeRole];
            Color.RGBToHSV(color, out var hue, out var saturation, out var value);
            if (channel == 3) value = amount;
            else if (channel == 0) color.r = amount;
            else if (channel == 1) color.g = amount;
            else color.b = amount;
            if (channel == 3)
                color = Color.HSVToRGB(hue, saturation, value);
            color.a = 1f;
            SetThemeDraftColor(color);
        }

        void SetThemeHueSaturation(float hue, float saturation)
        {
            if (_themeDraft == null || _themeDraft.Length < 4) return;
            Color.RGBToHSV(_themeDraft[_selectedThemeRole], out _, out _, out var value);
            SetThemeDraftColor(Color.HSVToRGB(hue, saturation, value));
        }

        void SetThemeDraftColor(Color color)
        {
            color.a = 1f;
            _themeDraft[_selectedThemeRole] = color;
            if (_themeColorChip != null) _themeColorChip.color = color;
            if (_themeRoleButtons != null && _selectedThemeRole < _themeRoleButtons.Length && _themeRoleButtons[_selectedThemeRole] != null)
            {
                var roleButton = _themeRoleButtons[_selectedThemeRole];
                roleButton.image.color = color;
                var roleText = roleButton.GetComponentInChildren<Text>();
                if (roleText != null) roleText.color = ThemeButtonText(color);
            }
            if (_themeHexInput != null && !_themeHexInput.isFocused)
                _themeHexInput.SetTextWithoutNotify("#" + ColorUtility.ToHtmlStringRGB(color));
            RefreshThemeEditor();
        }

        void ApplyThemeHex(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            value = value.Trim();
            if (!value.StartsWith("#")) value = "#" + value;
            if (ColorUtility.TryParseHtmlString(value, out var parsed))
                SetThemeDraftColor(parsed);
            else
                RefreshThemeEditor();
        }

        void OpenThemeHomePreview()
        {
            if (_themePreviewModal != null)
                Destroy(_themePreviewModal);
            _themePreviewModal = new GameObject("ThemeHomePreviewModal", typeof(RectTransform));
            _themePreviewModal.transform.SetParent(_root.transform, false);
            var modalRect = _themePreviewModal.GetComponent<RectTransform>();
            BroadcastUi.Stretch(modalRect);
            var dim = BroadcastUi.Solid(modalRect, "PreviewBackdrop", new Color(0f, 0f, 0f, 0.76f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            BroadcastUi.Catch(dim, CloseThemeHomePreview);

            var screen = BroadcastUi.Panel(modalRect, "HomePreview", _themeDraft[0], true);
            BroadcastUi.Place(screen.rectTransform, 0.075f, 0.105f, 0.925f, 0.895f);
            _themePreviewBackground = screen;
            var header = BroadcastUi.Panel(screen.transform, "PreviewHeader", UiTheme.Header);
            BroadcastUi.Place(header.rectTransform, 0.025f, 0.84f, 0.975f, 0.965f);
            _themePreviewHeader = header;
            var crest = BroadcastUi.Panel(header.transform, "PreviewClubMark", _themeDraft[2]);
            BroadcastUi.Place(crest.rectTransform, 0.02f, 0.17f, 0.09f, 0.83f);
            _themePreviewAccentBar = crest;
            var club = BroadcastUi.Label(header.transform, "PreviewClubName", "NORTHBRIDGE FC", 20, _themeDraft[3], TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(club.rectTransform, 0.11f, 0.17f, 0.54f, 0.83f);
            var date = BroadcastUi.Label(header.transform, "PreviewDate",
                LocalizationManager.Current == GameLanguage.English ? "Sunday, 12 September 2026  ·  Premier League 2026/27" : "domingo, 12 de setembro de 2026  ·  Premier League 2026/27",
                12, UiTheme.Muted, TextAnchor.MiddleRight);
            BroadcastUi.Place(date.rectTransform, 0.55f, 0.17f, 0.98f, 0.83f);

            var title = BroadcastUi.Label(screen.transform, "PreviewTitle",
                LocalizationManager.Current == GameLanguage.English ? "HOME  ·  CONTINUE CAREER" : "INÍCIO  ·  CONTINUAR CARREIRA",
                18, _themeDraft[3], TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.04f, 0.72f, 0.88f, 0.82f);
            _themePreviewTitle = title;
            var close = BroadcastUi.Button(screen.transform, "CloseHomePreview",
                LocalizationManager.Current == GameLanguage.English ? "CLOSE" : "FECHAR", CloseThemeHomePreview,
                UiTheme.PanelAlt, UiTheme.Text, 11);
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.88f, 0.72f, 0.96f, 0.82f);

            var career = BroadcastUi.Panel(screen.transform, "ContinueCareerCard", _themeDraft[1]);
            BroadcastUi.Place(career.rectTransform, 0.04f, 0.51f, 0.96f, 0.69f);
            _themePreviewCard = career;
            var careerText = BroadcastUi.Label(career.transform, "PreviewBody",
                LocalizationManager.Current == GameLanguage.English
                    ? "CONTINUE CAREER   ·   Northbridge FC\nGame date  ·  12 September 2026       Last saved today  ·  Play time 4 h 20 min"
                    : "CONTINUAR CARREIRA   ·   Northbridge FC\nData do jogo  ·  12 de setembro de 2026       Guardado hoje  ·  Tempo de jogo 4 h 20 min",
                14, _themeDraft[3], TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(careerText.rectTransform, 0.035f, 0.12f, 0.965f, 0.88f);
            _themePreviewBody = careerText;

            var news = BroadcastUi.Panel(screen.transform, "NewsCard", UiTheme.PanelAlt);
            BroadcastUi.Place(news.rectTransform, 0.04f, 0.21f, 0.60f, 0.47f);
            _themePreviewNewsCard = news;
            var newsText = BroadcastUi.Label(news.transform, "NewsText",
                LocalizationManager.Current == GameLanguage.English
                    ? "LATEST NEWS\nThe academy's young winger earns his first senior start after a run of strong training sessions."
                    : "ÚLTIMAS NOTÍCIAS\nO jovem extremo da academia estreia-se na equipa principal depois de uma série de bons treinos.",
                14, _themeDraft[3], TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(newsText.rectTransform, 0.035f, 0.12f, 0.965f, 0.88f);

            var fixtures = BroadcastUi.Panel(screen.transform, "NextFixtures", UiTheme.PanelAlt);
            BroadcastUi.Place(fixtures.rectTransform, 0.62f, 0.21f, 0.96f, 0.47f);
            var fixtureText = BroadcastUi.Label(fixtures.transform, "FixtureText",
                LocalizationManager.Current == GameLanguage.English
                    ? "UPCOMING FIXTURES\nSat 15:00  ·  Northbridge FC vs East Wharf\nWed 20:00  ·  Kingsport vs Northbridge FC"
                    : "PRÓXIMOS JOGOS\nSáb 15:00  ·  Northbridge FC vs East Wharf\nQua 20:00  ·  Kingsport vs Northbridge FC",
                13, _themeDraft[3], TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(fixtureText.rectTransform, 0.04f, 0.10f, 0.96f, 0.90f);

            var secondary = BroadcastUi.Panel(screen.transform, "StatusCard", _themeDraft[1]);
            BroadcastUi.Place(secondary.rectTransform, 0.04f, 0.08f, 0.60f, 0.17f);
            _themePreviewSecondaryCard = secondary;
            var statusText = BroadcastUi.Label(secondary.transform, "StatusText",
                LocalizationManager.Current == GameLanguage.English ? "SQUAD  ·  Fitness good   ·   Next match in 3 days" : "PLANTEL  ·  Boa condição física   ·   Próximo jogo dentro de 3 dias",
                12, _themeDraft[3], TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Stretch(statusText.rectTransform);
            var action = BroadcastUi.Panel(screen.transform, "PreviewAction", _themeDraft[2]);
            BroadcastUi.Place(action.rectTransform, 0.68f, 0.08f, 0.96f, 0.17f);
            _themePreviewAction = action;
            var actionText = BroadcastUi.Label(action.transform, "PreviewActionText",
                LocalizationManager.Current == GameLanguage.English ? "OPEN INBOX" : "ABRIR NOTIFICAÇÕES",
                12, ThemeButtonText(_themeDraft[2]), TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Stretch(actionText.rectTransform);
            _themePreviewActionText = actionText;
            _themePreviewTexts = new List<Text>(_themePreviewModal.GetComponentsInChildren<Text>(true));
            RefreshThemePreview();
            ControlChrome.FocusWithin(_themePreviewModal.transform);
        }

        void CloseThemeHomePreview()
        {
            if (_themePreviewModal != null)
                Destroy(_themePreviewModal);
            _themePreviewModal = null;
            _themePreviewBackground = null;
            _themePreviewHeader = null;
            _themePreviewCard = null;
            _themePreviewNewsCard = null;
            _themePreviewSecondaryCard = null;
            _themePreviewAccentBar = null;
            _themePreviewAction = null;
            _themePreviewTitle = null;
            _themePreviewBody = null;
            _themePreviewActionText = null;
            _themePreviewTexts = null;
            ControlChrome.FocusWithin(_root == null ? null : _root.transform);
        }

        void RefreshThemeEditor()
        {
            if (_themeDraft == null || _themeDraft.Length < 4)
                return;
            if (_themeRoleButtons != null)
                for (var i = 0; i < _themeRoleButtons.Length; i++)
                {
                    var button = _themeRoleButtons[i];
                    if (button == null) continue;
                    button.image.color = _themeDraft[i];
                    var label = button.GetComponentInChildren<Text>();
                    if (label != null) label.color = ThemeButtonText(_themeDraft[i]);
                    var outline = button.GetComponent<Outline>();
                    if (outline != null) Destroy(outline);
                    if (i == _selectedThemeRole)
                    {
                        outline = button.gameObject.AddComponent<Outline>();
                        outline.effectColor = UiTheme.Text;
                        outline.effectDistance = new Vector2(2f, 2f);
                    }
                }
            if (_themeColorChip != null)
                _themeColorChip.color = _themeDraft[_selectedThemeRole];
            Color.RGBToHSV(_themeDraft[_selectedThemeRole], out var hue, out var saturation, out var brightness);
            if (_themeSliders != null && _themeSliders.Length > 0 && _themeSliders[0] != null)
                _themeSliders[0].SetValueWithoutNotify(brightness);
            if (_themeSliderValues != null && _themeSliderValues.Length > 0 && _themeSliderValues[0] != null)
                _themeSliderValues[0].text = Mathf.RoundToInt(brightness * 100f) + "%";
            if (_themeWheel != null) _themeWheel.SetSelection(hue, saturation);
            if (_themeHexInput != null && !_themeHexInput.isFocused)
                _themeHexInput.SetTextWithoutNotify("#" + ColorUtility.ToHtmlStringRGB(_themeDraft[_selectedThemeRole]));
            RefreshThemePreview();
        }

        void RefreshThemePreview()
        {
            if (_themeDraft == null || _themeDraft.Length < 4)
                return;
            var canvas = _themeDraft[0];
            var panel = _themeDraft[1];
            var accent = _themeDraft[2];
            var text = _themeDraft[3];
            if (_themePreviewBackground != null) _themePreviewBackground.color = canvas;
            if (_themePreviewHeader != null) _themePreviewHeader.color = Color.Lerp(canvas, Color.black, 0.42f);
            if (_themePreviewCard != null) _themePreviewCard.color = panel;
            if (_themePreviewNewsCard != null) _themePreviewNewsCard.color = Color.Lerp(panel, Color.white, 0.12f);
            if (_themePreviewSecondaryCard != null) _themePreviewSecondaryCard.color = Color.Lerp(panel, Color.white, 0.12f);
            if (_themePreviewAccentBar != null) _themePreviewAccentBar.color = accent;
            if (_themePreviewAction != null) _themePreviewAction.color = accent;
            if (_themePreviewTexts != null)
                for (var i = 0; i < _themePreviewTexts.Count; i++)
                    if (_themePreviewTexts[i] != null)
                        _themePreviewTexts[i].color = text;
            if (_themePreviewTitle != null) _themePreviewTitle.color = text;
            if (_themePreviewBody != null) _themePreviewBody.color = Color.Lerp(text, panel, 0.28f);
            if (_themePreviewActionText != null) _themePreviewActionText.color = ThemeButtonText(accent);
            if (_themePreviewOpenButton != null)
            {
                _themePreviewOpenButton.image.color = accent;
                var label = _themePreviewOpenButton.GetComponentInChildren<Text>();
                if (label != null) label.color = ThemeButtonText(accent);
            }
        }

        void ApplyThemeDraft()
        {
            UiTheme.ApplyPalette(_themeDraft);
            _themeDraft = UiTheme.EditablePalette();
            PersistSettings();
            RefreshThemeEditor();
            if (_feedback != null)
                _feedback.text = LocalizationManager.Current == GameLanguage.English
                    ? "Theme colours saved and applied."
                    : "Cores do tema guardadas e aplicadas.";
        }

        void ResetThemeDraft()
        {
            UiTheme.ApplyPalette(UiTheme.DefaultPalette());
            _themeDraft = UiTheme.EditablePalette();
            PersistSettings();
            RefreshThemeEditor();
            if (_feedback != null)
                _feedback.text = LocalizationManager.Current == GameLanguage.English
                    ? "Default theme colours restored and applied."
                    : "Cores padrão do tema restauradas e aplicadas.";
        }

        static Color ThemeButtonText(Color background)
        {
            var brightness = background.r * 0.2126f + background.g * 0.7152f + background.b * 0.0722f;
            return brightness > 0.58f ? Color.black : Color.white;
        }

        void ChooseModFolder()
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            var folder = WindowsMusicPicker.OpenFolder(english ? "Choose mod folder" : "Escolher pasta de mods");
            if (string.IsNullOrWhiteSpace(folder)) return;
            _selectedModFolder = folder;
            var files = ModFolder.EnumeratePackageFiles(folder, out var error);
            if (_modStatus != null)
                _modStatus.text = files.Count > 0
                    ? (english ? files.Count + " supported files ready to import." : files.Count + " ficheiros compatíveis prontos a importar.")
                    : error;
            if (_modProgressFill != null) BroadcastUi.Place(_modProgressFill.rectTransform, 0f, 0f, 0f, 1f);
        }

        void ImportModFolder()
        {
            if (string.IsNullOrWhiteSpace(_selectedModFolder))
            {
                if (_modStatus != null) _modStatus.text = LocalizationManager.Current == GameLanguage.English ? "Choose a package folder first." : "Escolhe primeiro a pasta do pacote.";
                return;
            }
            var files = ModFolder.EnumeratePackageFiles(_selectedModFolder, out var error);
            if (files.Count == 0)
            {
                if (_modStatus != null) _modStatus.text = error;
                return;
            }
            if (_modImportRoutine != null) StopCoroutine(_modImportRoutine);
            if (_modProgressFill != null) BroadcastUi.Place(_modProgressFill.rectTransform, 0f, 0f, 0f, 1f);
            _modImportRoutine = StartCoroutine(ImportModFiles(files));
        }

        IEnumerator ImportModFiles(List<ModPackageFile> files)
        {
            var english = LocalizationManager.Current == GameLanguage.English;
            if (_importModButton != null) _importModButton.interactable = false;
            long totalWork = 0;
            for (var i = 0; i < files.Count; i++)
            {
                long length = 0;
                try { length = new FileInfo(files[i].SourcePath).Length; }
                catch { }
                totalWork += Math.Max(1L, length);
            }
            var completedWork = 0L;
            var buffer = new byte[256 * 1024];
            var importedAll = true;
            var copiedAny = false;
            for (var i = 0; i < files.Count; i++)
            {
                var job = new ModPackageCopyJob();
                if (!job.Open(files[i], out var error))
                {
                    if (_modStatus != null) _modStatus.text = (english ? "Import stopped: " : "Importação interrompida: ") + error;
                    importedAll = false;
                    break;
                }
                while (!job.IsComplete)
                {
                    if (!job.CopyChunk(buffer, out _, out error))
                    {
                        if (_modStatus != null) _modStatus.text = (english ? "Import stopped: " : "Importação interrompida: ") + error;
                        importedAll = false;
                        break;
                    }
                    var fileWeight = Math.Max(1L, job.TotalBytes);
                    var currentFileWork = job.TotalBytes > 0 ? job.CopiedBytes : job.IsComplete ? 1L : 0L;
                    var progress = totalWork <= 0 ? 0f : Mathf.Clamp01((completedWork + currentFileWork) / (float)totalWork);
                    if (_modProgressFill != null) BroadcastUi.Place(_modProgressFill.rectTransform, 0f, 0f, progress, 1f);
                    if (_modStatus != null) _modStatus.text = english
                        ? "Importing " + (i + 1) + " / " + files.Count + " · " + Path.GetFileName(files[i].SourcePath) + " · " + Mathf.RoundToInt(progress * 100f) + "%"
                        : "A importar " + (i + 1) + " / " + files.Count + " · " + Path.GetFileName(files[i].SourcePath) + " · " + Mathf.RoundToInt(progress * 100f) + "%";
                    yield return null;
                }
                completedWork += Math.Max(1L, job.TotalBytes);
                if (job.IsComplete) copiedAny = true;
                job.Dispose();
                if (!importedAll) break;
            }
            if (copiedAny)
            {
                ModFolder.RefreshImportedImages();
                FaceBook.InvalidateImportedCaches();
            }
            if (importedAll)
            {
                if (_modProgressFill != null) BroadcastUi.Place(_modProgressFill.rectTransform, 0f, 0f, 1f, 1f);
                if (_modStatus != null) _modStatus.text = english
                    ? "Import complete. Changes apply when a career loads."
                    : "Importação concluída. As alterações aplicam-se ao carregar uma carreira.";
            }
            if (_importModButton != null) _importModButton.interactable = true;
            _modImportRoutine = null;
        }

        void CreateModTemplates()
        {
            try
            {
                ModFolder.CreateTemplates();
                if (_modStatus != null) _modStatus.text = LocalizationManager.Current == GameLanguage.English
                    ? "The guide and starter templates are ready in the game Mods folder. Existing templates were kept."
                    : "O guia e os modelos estão prontos na pasta Mods do jogo. Os modelos existentes foram mantidos.";
            }
            catch (Exception exception)
            {
                if (_modStatus != null) _modStatus.text = exception.Message;
            }
        }

        void OpenModsFolder()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = "\"" + ModFolder.Root() + "\"", UseShellExecute = true });
            }
            catch (Exception exception)
            {
                if (_modStatus != null) _modStatus.text = exception.Message;
            }
#else
            if (_modStatus != null) _modStatus.text = LocalizationManager.Current == GameLanguage.English ? "Open this folder from the displayed path." : "Abre esta pasta através do caminho mostrado.";
#endif
        }

        void BuildLegalPage(Transform parent)
        {
            var page = Page(parent, 10, "LEGAL");
            _legalPageTitle = page.transform.Find("PageTitle").GetComponent<Text>();
            _legalPageNote = page.transform.Find("PageNote").GetComponent<Text>();
            DropLocalizer(_legalPageTitle);
            DropLocalizer(_legalPageNote);

            var names = LegalTabNames(false);
            _legalTabs = new Button[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                var index = i;
                var x0 = 0.035f + i * 0.19f;
                _legalTabs[i] = BroadcastUi.Button(page.transform, "LegalTab" + i, names[i], () => ShowLegal(index),
                    new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 12);
                BroadcastUi.Place(_legalTabs[i].GetComponent<RectTransform>(), x0, 0.755f, x0 + 0.175f, 0.835f);
                DropLocalizer(_legalTabs[i].GetComponentInChildren<Text>());
            }

            _legalHeading = BroadcastUi.Label(page.transform, "LegalHeading", "", 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            DropLocalizer(_legalHeading);
            BroadcastUi.Place(_legalHeading.rectTransform, 0.04f, 0.68f, 0.96f, 0.745f);

            var scroll = BroadcastUi.Scroll(page.transform, "LegalScroll", out var content);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.03f, 0.02f, 0.97f, 0.67f);
            var paper = BroadcastUi.Panel(content, "LegalPaper", new Color(1f, 1f, 1f, 0.035f));
            _legalBody = BroadcastUi.Label(paper.transform, "LegalBody", "", 14, Color.white, TextAnchor.UpperLeft);
            _legalBody.lineSpacing = 1.05f;
            DropLocalizer(_legalBody);
            BroadcastUi.Stretch(_legalBody.rectTransform);
            _legalBody.rectTransform.offsetMin = new Vector2(16f, 14f);
            _legalBody.rectTransform.offsetMax = new Vector2(-16f, -14f);
            ShowLegal(0);
        }

        static void DropLocalizer(Text label)
        {
            if (label == null)
                return;
            var localizer = label.GetComponent<LocalizedText>();
            if (localizer == null)
                return;
            localizer.enabled = false;
            UnityEngine.Object.Destroy(localizer);
        }

        void ShowLegal(int index)
        {
            _legalDoc = Mathf.Clamp(index, 0, 4);
            PaintLegal(true);
        }

        void PaintLegal()
        {
            PaintLegal(false);
        }

        void PaintLegal(bool jump)
        {
            if (_legalBody == null)
                return;
            var english = LocalizationManager.Current == GameLanguage.English;
            if (_legalPageTitle != null)
                _legalPageTitle.text = "LEGAL";
            if (_legalPageNote != null)
                _legalPageNote.text = english ? EnglishPageNotes[10] : PageNotes[10];
            var names = LegalTabNames(english);
            var headings = LegalHeadings(english);
            for (var i = 0; i < _legalTabs.Length; i++)
            {
                var active = i == _legalDoc;
                _legalTabs[i].GetComponent<Image>().color = active ? UiTheme.Accent : new Color(0.22f, 0.12f, 0.38f, 1f);
                var label = _legalTabs[i].GetComponentInChildren<Text>();
                if (label == null)
                    continue;
                label.text = names[i];
                label.color = active ? UiTheme.ButtonText : Color.white;
            }
            _legalHeading.text = headings[_legalDoc];
            var body = LegalBody(english, _legalDoc);
            BroadcastUi.SetWrapped(_legalBody, body, 280f);
            var paper = _legalBody.transform.parent.GetComponent<LayoutElement>();
            if (paper == null)
                paper = _legalBody.transform.parent.gameObject.AddComponent<LayoutElement>();
            paper.preferredHeight = Mathf.Max(320f, _legalBody.preferredHeight + 36f);
            paper.minHeight = paper.preferredHeight;
            if (!jump)
                return;
            var scroll = _legalBody.GetComponentInParent<ScrollRect>();
            if (scroll != null)
                scroll.verticalNormalizedPosition = 1f;
        }

        static string[] LegalTabNames(bool english)
        {
            return english
                ? new[] { "TERMS", "LICENCES", "MODDING", "NOTICES", "PRIVACY" }
                : new[] { "TERMOS", "LICENÇAS", "MODDING", "AVISOS", "PRIVACIDADE" };
        }

        static string[] LegalHeadings(bool english)
        {
            return english
                ? new[] { "Terms of use", "Licences", "Modding guidelines", "Disclaimers", "Privacy" }
                : new[] { "Termos de uso", "Licenças", "Regras de modding", "Avisos", "Privacidade" };
        }

        static string LegalBody(bool english, int index)
        {
            switch (index)
            {
                case 0: return english ? TermsEnglish() : TermsPortuguese();
                case 1: return english ? LicencesEnglish() : LicencesPortuguese();
                case 2: return english ? ModdingEnglish() : ModdingPortuguese();
                case 3: return english ? NoticesEnglish() : NoticesPortuguese();
                default: return english ? PrivacyEnglish() : PrivacyPortuguese();
            }
        }

        static string TermsPortuguese()
        {
            return "O Coach Master é um jogo de carreira de futebol feito para jogar neste computador. Abrir as definições e continuar a jogar significa que aceitas estes termos.\n\n"
                + "O jogo é para uso pessoal. Podes jogar, guardar carreiras e mudar a apresentação. Não podes vender o jogo, apresentá-lo como produto oficial de uma liga, de um clube ou de outra marca, nem distribuir a pasta do projeto como se fosse tua.\n\n"
                + "Cada carreira fica neste computador. O save manual e o automático podem substituir um ficheiro anterior. Antes de importar um mod ou de apagar uma carreira, faz uma cópia se quiseres ficar com aquele percurso.\n\n"
                + "Não há garantia de que uma carreira, um mod ou um resultado se mantenham iguais depois de uma atualização. O jogo é fornecido como está. Lesões, transferências, despedimentos e resultados fazem parte da simulação.\n\n"
                + "Música, fundos e mods que adicionas têm de ser ficheiros que tens direito a usar. O jogo não verifica esses direitos por ti.";
        }

        static string TermsEnglish()
        {
            return "Coach Master is a football career game meant to be played on this computer. Opening settings and carrying on means you accept these terms.\n\n"
                + "The game is for personal use. You may play, keep careers and change the presentation. You may not sell the game, present it as an official product of a league, a club or another brand, or pass the project folder off as your own.\n\n"
                + "Each career stays on this computer. A manual or automatic save can replace an older file. Copy a career before importing a mod or deleting it if you want to keep that run.\n\n"
                + "A career, a mod or a result may change after an update. The game is provided as it is. Injuries, transfers, sackings and results are part of the simulation.\n\n"
                + "Music, backgrounds and mods you add must be files you have the right to use. The game does not check those rights for you.";
        }

        static string LicencesPortuguese()
        {
            return "O Coach Master corre em Unity. Unity é uma marca da Unity Technologies. Este jogo não é feito, patrocinado nem aprovado pela Unity. Os pacotes do motor, incluindo o render e o sistema de input, continuam sujeitos às licenças da Unity.\n\n"
                + "A simulação de campo em 3D inclui código e arte de terceiros que já vinham com essa parte do projeto. Esses ficheiros mantêm a licença de origem. Esta página não passa esses direitos para outra pessoa.\n\n"
                + "A chuva usa o pacote Rainy VFX, de AIKStudio. Pode seguir dentro de uma build do jogo. Os ficheiros soltos desse pacote não podem ser revendidos nem redistribuídos como asset à parte.\n\n"
                + "A interface, a carreira, os textos e as regras de gestão feitos neste projeto são a camada do Coach Master. Isso não apaga as licenças das partes de terceiros.\n\n"
                + "Faixas de música e imagens de fundo que importas continuam a ser tuas, ou de quem te deu autorização. O jogo só as lê neste computador.";
        }

        static string LicencesEnglish()
        {
            return "Coach Master runs on Unity. Unity is a trademark of Unity Technologies. This game is not made, sponsored or approved by Unity. Engine packages, including the renderer and the input system, stay under Unity's own licences.\n\n"
                + "The 3D pitch simulation includes third-party code and art that already belonged to that part of the project. Those files keep their original licence. This page does not pass those rights on to anyone else.\n\n"
                + "Rain uses the Rainy VFX package by AIKStudio. It may ship inside a build of the game. The loose files of that package may not be resold or redistributed as a separate asset.\n\n"
                + "The interface, career, writing and management rules made in this project are the Coach Master layer. That does not erase third-party licences.\n\n"
                + "Music tracks and background images you import remain yours, or remain with whoever gave you permission. The game only reads them on this computer.";
        }

        static string ModdingPortuguese()
        {
            return "Um mod é uma pasta com ficheiros de texto e imagens. A importação copia o que o jogo reconhece para a pasta Mods deste computador. Um ficheiro com o mesmo nome é substituído. O resto fica como está.\n\n"
                + "Ficheiros lidos: equipas.txt ou clubs.txt, jogadores.txt ou players.txt, treinadores.txt ou managers.txt, staff.txt e sponsors.txt. Imagens em PNG, JPG ou JPEG, nas pastas caras ou faces, emblemas, badges ou crests, e patrocinadores ou sponsors. O nome do ficheiro é a chave do rosto, do emblema ou do logótipo.\n\n"
                + "Cada linha é um registo em UTF-8. Os campos separam-se com |. Uma linha que começa por # é um comentário. A ordem dos campos é a do guia MODS-LEIA-ME.txt. Um campo opcional vazio mantém o valor que já existia. IDs da pré-visualização da base de dados são a forma segura de apontar a um clube ou a um jogador. O nome atual também pode servir.\n\n"
                + "Não juntes executáveis, instaladores nem ficheiros que o jogo não pediu. Não publiques a pasta inteira do jogo como se fosse o teu mod. Se partilhares um mod, descreve o que muda e inclui só os ficheiros novos.\n\n"
                + "Nomes, caras, emblemas, equipamentos e músicas reais são responsabilidade de quem cria o mod. Só os uses se tiveres esse direito. Importa primeiro numa cópia da carreira: um mod pode mudar plantéis, contratos e a leitura dos dados.";
        }

        static string ModdingEnglish()
        {
            return "A mod is a folder of text files and images. Import copies what the game recognises into the Mods folder on this computer. A file with the same name is replaced. Everything else stays as it was.\n\n"
                + "Files read: clubs.txt or equipas.txt, players.txt or jogadores.txt, managers.txt or treinadores.txt, staff.txt and sponsors.txt. Images in PNG, JPG or JPEG, in faces or caras, badges, crests or emblemas, and sponsors or patrocinadores. The file name is the key for the face, crest or logo.\n\n"
                + "Each line is one UTF-8 record. Fields are separated by |. A line that starts with # is a comment. Field order is the one in MODS-LEIA-ME.txt. An empty optional field keeps the value already there. IDs from the database preview are the safe way to point at a club or a player. The current name can also work.\n\n"
                + "Do not add executables, installers or files the game did not ask for. Do not publish the whole game folder as if it were your mod. If you share a mod, say what it changes and include only the new files.\n\n"
                + "Real names, faces, crests, kits and music are the mod author's responsibility. Use them only when you have that right. Import into a copy of the career first: a mod can change squads, contracts and how the data is read.";
        }

        static string NoticesPortuguese()
        {
            return "O Coach Master não é FIFA, EA SPORTS FC, PES, Football Manager, nem um jogo da FIFA, da UEFA, de uma liga ou de um clube. Não há afiliação, patrocínio nem licença oficial dessas marcas.\n\n"
                + "Nomes, caras, cores e emblemas podem ser fictícios ou ter sido trazidos por um mod ou por uma imagem tua. Uma parecença com uma pessoa ou um clube real não significa que essa pessoa ou esse clube participem no jogo.\n\n"
                + "Resultados, lesões, mercado, moral e classificações são regras de jogo. Não são previsões, scouting real nem aconselhamento. A classe do jogador, de Histórico a Profissional em Desenvolvimento, lê o overall atual e mexe-se com a forma. Não é um certificado.\n\n"
                + "A sala online partilha a carreira na rede local, entre computadores que tu abres. Não é um serviço oficial, não cria uma conta e não guarda a carreira num servidor do jogo.\n\n"
                + "Se alguma destas frases estiver em conflito com a licença de um pacote de terceiros, vale a licença desse pacote para os ficheiros dele.";
        }

        static string NoticesEnglish()
        {
            return "Coach Master is not FIFA, EA SPORTS FC, PES, Football Manager, or a game from FIFA, UEFA, a league or a club. There is no affiliation, sponsorship or official licence from those brands.\n\n"
                + "Names, faces, colours and crests may be fictional or may have arrived through a mod or an image you added. A resemblance to a real person or club does not mean that person or club takes part in the game.\n\n"
                + "Results, injuries, the market, morale and tables are game rules. They are not predictions, real scouting or advice. A player's class, from Historic down to Developing Professional, reads the current overall and moves with form. It is not a certificate.\n\n"
                + "The online room shares a career on the local network, between computers you open. It is not an official service, it does not create an account, and it does not store the career on a game server.\n\n"
                + "If one of these lines conflicts with a third-party package licence, that package's licence applies to its own files.";
        }

        static string PrivacyPortuguese()
        {
            return "Não há conta de jogador. O Coach Master não pede nome, email nem uma palavra-passe para entrar.\n\n"
                + "As carreiras, a pasta Mods, a música importada e os fundos que escolhes ficam no disco deste computador. As definições de ecrã, som, idioma e controlos também ficam neste computador.\n\n"
                + "A sala online só comunica com a rede local enquanto a tens aberta. Não envia a carreira para um servidor do jogo. Quem estiver nessa rede pode receber o que a sala partilhar, por isso abre-a só com as pessoas que queres.\n\n"
                + "Apagar uma carreira remove esse save neste computador. Apagar um fundo ou uma faixa tira-os das definições. O jogo não mantém uma cópia na nuvem.\n\n"
                + "Imagens de jogadores são lidas para o retrato e, no jogo em 3D, para aproximar o tom de pele do modelo. Esse cálculo fica na partida. Não é enviado para lado nenhum.";
        }

        static string PrivacyEnglish()
        {
            return "There is no player account. Coach Master does not ask for a name, an email or a password to start.\n\n"
                + "Careers, the Mods folder, imported music and backgrounds you choose stay on this computer's disk. Display, audio, language and control settings stay on this computer too.\n\n"
                + "The online room only talks to the local network while you have it open. It does not send the career to a game server. Anyone on that network can receive what the room shares, so open it only with the people you want.\n\n"
                + "Deleting a career removes that save on this computer. Deleting a background or a track removes it from settings. The game does not keep a cloud copy.\n\n"
                + "Player photos are read for the portrait and, in the 3D match, to approximate the model's skin tone. That stays inside the match. It is not sent anywhere.";
        }

        GameObject Page(Transform parent, int index, string heading)
        {
            var page = BroadcastUi.Panel(parent, "SettingsPage" + index, new Color(0f, 0f, 0f, 0.001f));
            BroadcastUi.Place(page.rectTransform, 0.025f, 0.025f, 0.975f, 0.975f);
            _pages[index] = page.gameObject;
            var title = BroadcastUi.Label(page.transform, "PageTitle", heading, 18, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.04f, 0.92f, 0.94f, 0.99f);
            var noteText = LocalizationManager.Current == GameLanguage.English ? EnglishPageNotes[index] : PageNotes[index];
            var note = BroadcastUi.Label(page.transform, "PageNote", noteText, 13, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(note.rectTransform, 0.04f, 0.86f, 0.94f, 0.92f);
            return page.gameObject;
        }

        static string CategoryName(int index)
        {
            if (LocalizationManager.Current != GameLanguage.English) return Categories[index];
            switch (index)
            {
                case 0: return "Display & graphics";
                case 1: return "Gameplay";
                case 2: return "Match day";
                case 3: return "Notifications";
                case 4: return "Audio";
                case 5: return "Realism";
                case 6: return "Career & saves";
                case 7: return "Accessibility";
                case 8: return "Mods & database";
                case 9: return "Theme & colours";
                default: return "Legal";
            }
        }

        void AddOption(Transform parent, string name, string title, string description, Func<string> value, Action change, float y)
        {
            var row = BroadcastUi.Panel(parent, "Option" + name, new Color(1f, 1f, 1f, 0.045f));
            BroadcastUi.Place(row.rectTransform, 0.035f, y, 0.965f, y + 0.096f);
            var heading = BroadcastUi.Label(row.transform, "Title", title, 15, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.025f, 0.58f, 0.6f, 0.97f);
            var hint = BroadcastUi.Label(row.transform, "Description", description, 11, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(hint.rectTransform, 0.025f, 0.02f, 0.61f, 0.55f);
            var button = BroadcastUi.Button(row.transform, "Value", value(), change, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 13);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                change?.Invoke();
                PersistSettings();
                Refresh();
            });
            BroadcastUi.Place(button.GetComponent<RectTransform>(), 0.64f, 0.17f, 0.98f, 0.83f);
            _buttonValues[button] = value;
        }

        void AddAction(Transform parent, string name, string title, string description, Action action, float y)
        {
            var row = BroadcastUi.Panel(parent, "Action" + name, new Color(1f, 1f, 1f, 0.045f));
            BroadcastUi.Place(row.rectTransform, 0.035f, y, 0.965f, y + 0.096f);
            var heading = BroadcastUi.Label(row.transform, "Title", title, 15, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.025f, 0.58f, 0.6f, 0.97f);
            var hint = BroadcastUi.Label(row.transform, "Description", description, 11, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(hint.rectTransform, 0.025f, 0.02f, 0.61f, 0.55f);
            var button = BroadcastUi.Button(row.transform, "Do", "ABRIR", action, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 13);
            BroadcastUi.Place(button.GetComponent<RectTransform>(), 0.64f, 0.17f, 0.98f, 0.83f);
            button.interactable = CurrentCareer() != null;
            _refreshValues.Add(() => button.interactable = CurrentCareer() != null);
        }

        void AddCompactOption(Transform parent, string name, string title, Func<string> value, Action change, float x0, float y, float x1)
        {
            var button = BroadcastUi.Button(parent, "Compact" + name, title + "  ·  " + value(), change, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 11);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                change?.Invoke();
                PersistSettings();
                Refresh();
            });
            BroadcastUi.Place(button.GetComponent<RectTransform>(), x0, y, x1, y + 0.09f);
            _buttonValues[button] = () => title + "  ·  " + value();
        }

        void AddVolume(Transform parent, string name, string title, string description, float value, UnityEngine.Events.UnityAction<float> changed, float y)
        {
            var row = BroadcastUi.Panel(parent, "Volume" + name, new Color(1f, 1f, 1f, 0.045f));
            BroadcastUi.Place(row.rectTransform, 0.035f, y, 0.965f, y + 0.078f);
            var heading = BroadcastUi.Label(row.transform, "Title", title, 14, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.025f, 0.58f, 0.55f, 0.97f);
            var hint = BroadcastUi.Label(row.transform, "Description", description, 11, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(hint.rectTransform, 0.025f, 0.02f, 0.59f, 0.55f);
            var valueText = BroadcastUi.Label(row.transform, "Percent", Percent(value), 12, UiTheme.AccentSoft, TextAnchor.MiddleRight, FontStyle.Bold);
            BroadcastUi.Place(valueText.rectTransform, 0.84f, 0.52f, 0.97f, 0.96f);
            var track = BroadcastUi.Solid(row.transform, "Track", new Color(1f, 1f, 1f, 0.17f), true);
            track.sprite = BroadcastUi.Round;
            track.type = Image.Type.Sliced;
            BroadcastUi.Place(track.rectTransform, 0.62f, 0.2f, 0.97f, 0.48f);
            var trackButton = track.gameObject.AddComponent<Button>();
            trackButton.transition = Selectable.Transition.None;
            var trackNavigation = trackButton.navigation;
            trackNavigation.mode = Navigation.Mode.None;
            trackButton.navigation = trackNavigation;
            trackButton.targetGraphic = track;
            var fill = BroadcastUi.Solid(track.transform, "Fill", new Color(0.93f, 0.95f, 0.98f, 1f));
            fill.sprite = BroadcastUi.Round;
            fill.type = Image.Type.Sliced;
            fill.raycastTarget = false;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            var slider = track.gameObject.AddComponent<DragVolume>();
            slider.Setup(fill, null, value, amount =>
            {
                changed(amount);
                valueText.text = Percent(amount);
                ScheduleSettingsSave();
            });
            _refreshValues.Add(() =>
            {
                var current = name == "Master" ? GameAudio.MasterVolume
                    : name == "Music" ? GameAudio.MusicVolume
                    : name == "MatchEffects" ? GameAudio.EffectVolume
                    : name == "Crowd" ? GameAudio.CrowdVolume : GameAudio.MenuVolume;
                slider.Set(current);
                valueText.text = Percent(current);
            });
        }

        void AddText(Transform parent, string name, string text, int size, Color color, float y, float height)
        {
            var note = BroadcastUi.Label(parent, name, text, size, color, TextAnchor.UpperLeft);
            BroadcastUi.Place(note.rectTransform, 0.045f, y, 0.95f, y + height);
        }

        Button FooterButton(Transform parent, string name, string text, Action action, float x0, float y0, float x1, float y1, Color color, Color textColor)
        {
            var button = BroadcastUi.Button(parent, name, text, action, color, textColor, 13);
            BroadcastUi.Place(button.GetComponent<RectTransform>(), x0, y0, x1, y1);
            return button;
        }

        void SelectPage(int index)
        {
            if (_pages == null || _pages.Length == 0)
                return;
            _page = Mathf.Clamp(index, 0, _pages.Length - 1);
            for (var i = 0; i < _pages.Length; i++)
                if (_pages[i] != null)
                    _pages[i].SetActive(i == _page);
            for (var i = 0; i < _tabs.Length; i++)
            {
                var active = i == _page;
                _tabs[i].GetComponent<Image>().color = active ? UiTheme.Accent : new Color(0.22f, 0.12f, 0.38f, 1f);
                var label = _tabs[i].GetComponentInChildren<Text>();
                if (label != null)
                    label.color = active ? UiTheme.ButtonText : Color.white;
            }
            ApplyCareerButtons();
            Refresh();
        }

        void Refresh()
        {
            foreach (var pair in _buttonValues)
            {
                if (pair.Key == null || pair.Key.GetComponentInChildren<Text>() == null)
                    continue;
                pair.Key.GetComponentInChildren<Text>().text = pair.Value();
            }
            for (var i = 0; i < _refreshValues.Count; i++)
                _refreshValues[i]();
            if (_now != null)
                _now.text = GameAudio.Repeat ? "A repetir  ·  " + GameAudio.CurrentTrackName() : "A tocar  ·  " + GameAudio.CurrentTrackName();
            if (_tabs != null)
            {
                for (var i = 0; i < _tabs.Length; i++)
                {
                    var tabLabel = _tabs[i] == null ? null : _tabs[i].GetComponentInChildren<Text>();
                    if (tabLabel != null)
                        tabLabel.text = CategoryName(i);
                }
            }
            PaintLegal();
            ApplyCareerButtons();
            MatchDayController.RefreshSettings();
        }

        void ApplyCareerButtons()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            var hasCareer = CurrentCareer() != null && (scene == SceneNames.CareerHub || scene == SceneNames.MatchDay);
            if (_saveCareer != null)
                _saveCareer.gameObject.SetActive(hasCareer);
            if (_menu != null)
                _menu.gameObject.SetActive(hasCareer);
        }

        string CurrentPresetLabel()
        {
            var career = CurrentCareer();
            return SettingsPrefs.RealismPresetLabel(career == null ? SettingsPrefs.DefaultRealismPreset : career.RealismPreset).ToUpperInvariant();
        }

        string CurrentInjuryLabel()
        {
            var career = CurrentCareer();
            return SettingsPrefs.FrequencyLabel(career == null ? SettingsPrefs.DefaultInjuryFrequency : career.InjuryFrequency).ToUpperInvariant();
        }

        string CurrentRefereeLabel()
        {
            var career = CurrentCareer();
            return SettingsPrefs.FrequencyLabel(career == null ? SettingsPrefs.DefaultRefereeStrictness : career.RefereeStrictness).ToUpperInvariant();
        }

        void CycleRealismPreset()
        {
            var career = CurrentCareer();
            var current = career == null ? SettingsPrefs.DefaultRealismPreset : career.RealismPreset;
            SettingsPrefs.ApplyRealismPreset(career, (current + 1) % 4);
            Refresh();
        }

        void CycleInjury()
        {
            var career = CurrentCareer();
            if (career == null)
            {
                SettingsPrefs.DefaultInjuryFrequency = (SettingsPrefs.DefaultInjuryFrequency + 1) % 3;
                SettingsPrefs.DefaultRealismPreset = 4;
            }
            else
            {
                career.InjuryFrequency = (career.InjuryFrequency + 1) % 3;
                career.RealismPreset = 4;
            }
            Refresh();
        }

        void CycleReferee()
        {
            var career = CurrentCareer();
            if (career == null)
            {
                SettingsPrefs.DefaultRefereeStrictness = (SettingsPrefs.DefaultRefereeStrictness + 1) % 3;
                SettingsPrefs.DefaultRealismPreset = 4;
            }
            else
            {
                career.RefereeStrictness = (career.RefereeStrictness + 1) % 3;
                career.RealismPreset = 4;
            }
            Refresh();
        }

        void PersistSettings()
        {
            var career = CurrentCareer();
            SettingsPrefs.CaptureCareerGameplay(career);
            SettingsPrefs.Commit();
            MatchDayController.RefreshSettings();
            var careerSaved = career == null || CareerStore.TrySave(career);
            if (_feedback != null)
                _feedback.text = careerSaved
                    ? (LocalizationManager.Current == GameLanguage.English ? "Settings saved." : "Definições guardadas.")
                    : (LocalizationManager.Current == GameLanguage.English ? "Settings applied, but the career save failed." : "Definições aplicadas, mas não foi possível guardar a carreira.");
        }

        void ScheduleSettingsSave()
        {
            if (_settingsSaveRoutine != null)
                StopCoroutine(_settingsSaveRoutine);
            _settingsSaveRoutine = StartCoroutine(SaveSettingsAfterDrag());
        }

        IEnumerator SaveSettingsAfterDrag()
        {
            yield return new WaitForSecondsRealtime(0.4f);
            _settingsSaveRoutine = null;
            PersistSettings();
        }

        void ApplyAndClose()
        {
            CloseControls();
            if (_root == null || !_root.activeSelf)
                return;
            if (_themePreviewModal != null)
                CloseThemeHomePreview();
            if (_settingsSaveRoutine != null)
            {
                StopCoroutine(_settingsSaveRoutine);
                _settingsSaveRoutine = null;
            }
            if (_musicManager != null)
                _musicManager.SetActive(false);
            PersistSettings();
            _root.SetActive(false);
            IsVisible = false;
            ControlChrome.SettingsClosed();
            MatchDayController.RefreshSettings();
        }

        public void CloseFromControl()
        {
            if (_themePreviewModal != null) { CloseThemeHomePreview(); return; }
            if (_confirm != null) { Destroy(_confirm); return; }
            if (_controls != null) { CloseControls(); return; }
            if (_saveManager != null) { Destroy(_saveManager); return; }
            if (_musicManager != null && _musicManager.activeSelf) { CloseMusicManager(); return; }
            ApplyAndClose();
        }

        void RestoreDefaults()
        {
            SettingsPrefs.RestoreDefaults();
            UiTheme.ApplyPalette(UiTheme.DefaultPalette());
            _themeDraft = UiTheme.EditablePalette();
            if (_themeRoleButtons != null)
                RefreshThemeEditor();
            var career = CurrentCareer();
            SettingsPrefs.ApplyRealismPreset(career, 1);
            GameRoot.ApplyUiScale();
            SelectPage(_page);
            PersistSettings();
            Refresh();
            if (_feedback != null)
                _feedback.text = LocalizationManager.Current == GameLanguage.English
                    ? "Default settings restored and saved."
                    : "Valores padrão restaurados e guardados.";
        }

        void SaveCareer()
        {
            var career = CurrentCareer();
            if (career == null)
                return;
            var saved = CareerStore.TrySave(career);
            if (_feedback != null)
                _feedback.text = saved ? "Carreira guardada." : "Não foi possível guardar. A gravação anterior foi mantida.";
        }

        void OpenOnlineFromSettings()
        {
            var hub = FindFirstObjectByType<CareerHubController>();
            if (hub == null)
            {
                if (_feedback != null)
                    _feedback.text = LocalizationManager.Current == GameLanguage.English
                        ? "Open a career first."
                        : "Abre uma carreira primeiro.";
                return;
            }
            ApplyAndClose();
            hub.OpenOnlineFromSettings();
        }

        void ChooseUserBackground()
        {
            var path = WindowsMusicPicker.OpenImage();
            if (string.IsNullOrEmpty(path))
            {
                if (_feedback != null)
                    _feedback.text = LocalizationManager.Current == GameLanguage.English ? "No image was chosen." : "Nenhuma imagem foi escolhida.";
                return;
            }
            if (!BackgroundPrefs.SetUserImage(path))
            {
                if (_feedback != null)
                    _feedback.text = LocalizationManager.Current == GameLanguage.English ? "Use a PNG, JPG or JPEG image." : "Usa uma imagem PNG, JPG ou JPEG.";
                return;
            }
            Refresh();
            if (_feedback != null)
                _feedback.text = LocalizationManager.Current == GameLanguage.English ? "Your background is on." : "O teu fundo está ligado.";
        }

        void DeleteUserBackground()
        {
            if (!BackgroundPrefs.HasUserImage)
                return;
            if (!BackgroundPrefs.DeleteUserImage())
            {
                if (_feedback != null)
                    _feedback.text = LocalizationManager.Current == GameLanguage.English
                        ? "Your background could not be deleted."
                        : "Não foi possível apagar o teu fundo.";
                return;
            }
            Refresh();
            if (_feedback != null)
                _feedback.text = LocalizationManager.Current == GameLanguage.English
                    ? "Your background was deleted."
                    : "O teu fundo foi apagado.";
        }

        string LanguageLabel()
        {
            return LocalizationManager.Current == GameLanguage.English ? "ENGLISH" : "PORTUGUÊS";
        }

        void CycleLanguage()
        {
            var next = LocalizationManager.Current == GameLanguage.English ? GameLanguage.Portuguese : GameLanguage.English;
            LocalizationManager.Select(next);
            Refresh();
        }

        void OpenSaveManager()
        {
            var active = CurrentCareer();
            if (active != null)
                CareerStore.TrySave(active);
            if (_saveManager != null)
                Destroy(_saveManager);
            _saveManager = new GameObject("NamedSaveManager", typeof(RectTransform));
            _saveManager.transform.SetParent(_root.transform, false);
            var root = _saveManager.GetComponent<RectTransform>();
            BroadcastUi.Stretch(root);
            var dim = BroadcastUi.Solid(root, "Dim", new Color(0f, 0f, 0f, 0f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            BroadcastUi.Catch(dim, () => Destroy(_saveManager));
            var card = BroadcastUi.Panel(root, "Card", new Color(0.1f, 0.04f, 0.2f, 1f), true);
            BroadcastUi.Place(card.rectTransform, 0.16f, 0.08f, 0.84f, 0.92f);
            var english = LocalizationManager.Current == GameLanguage.English;
            var title = BroadcastUi.Label(card.transform, "Title", english ? "SAVE MANAGER" : "GESTOR DE SAVES", 21, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.04f, 0.91f, 0.7f, 0.98f);
            var close = BroadcastUi.Button(card.transform, "CloseSaves", english ? "DONE" : "FECHAR", () => Destroy(_saveManager), new Color(1f, 1f, 1f, 0.12f), Color.white, 14);
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.78f, 0.91f, 0.96f, 0.98f);

            var slots = CareerStore.ListSlots();
            if (!string.IsNullOrEmpty(_selectedSlot))
            {
                var still = false;
                for (var i = 0; i < slots.Count; i++)
                    if (slots[i].Id == _selectedSlot)
                        still = true;
                if (!still)
                    _selectedSlot = null;
            }
            if (string.IsNullOrEmpty(_selectedSlot) && active != null)
                _selectedSlot = active.FileId;
            if (string.IsNullOrEmpty(_selectedSlot) && slots.Count > 0)
                _selectedSlot = slots[0].Id;

            var list = BroadcastUi.Scroll(card.transform, "CareerTabs", out var tabs);
            BroadcastUi.Place(list.GetComponent<RectTransform>(), 0.04f, 0.42f, 0.96f, 0.89f);
            if (slots.Count == 0)
            {
                var empty = BroadcastUi.Label(tabs, "Empty", english ? "No saved careers yet." : "Ainda não há carreiras guardadas.", 15, UiTheme.Muted, TextAnchor.MiddleLeft);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
            }
            CareerStore.CareerSlotInfo chosen = null;
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                var on = slot.Id == _selectedSlot;
                if (on)
                    chosen = slot;
                var id = slot.Id;
                var button = BroadcastUi.Button(tabs, "Career" + i, slot.Label, () =>
                {
                    _selectedSlot = id;
                    OpenSaveManager();
                }, on ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), on ? UiTheme.ButtonText : Color.white, 15);
                button.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
            }

            if (chosen == null)
                return;
            _saveName = UiFactory.Input(card.transform, "Nome da carreira");
            _saveName.text = chosen.Label;
            _saveName.characterLimit = 80;
            BroadcastUi.Place(_saveName.GetComponent<RectTransform>(), 0.04f, 0.31f, 0.62f, 0.39f);
            var renameId = chosen.Id;
            var rename = BroadcastUi.Button(card.transform, "Rename", "MUDAR NOME", () =>
            {
                CareerStore.RenameSlot(renameId, _saveName == null ? "" : _saveName.text, CurrentCareer());
                OpenSaveManager();
            }, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 14);
            BroadcastUi.Place(rename.GetComponent<RectTransform>(), 0.66f, 0.31f, 0.96f, 0.39f);

            var manualWhen = chosen.HasManual ? chosen.ManualAt.ToString("dd/MM/yyyy HH:mm") : "ainda não existe";
            var autoWhen = chosen.HasAuto ? chosen.AutoAt.ToString("dd/MM/yyyy HH:mm") : "ainda não existe";
            var manual = BroadcastUi.Button(card.transform, "LoadManual", "ÚLTIMO SAVE MANUAL  ·  " + manualWhen, () => AskLoadSlot(renameId, true),
                chosen.HasManual ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.08f), chosen.HasManual ? UiTheme.ButtonText : Color.white, 14);
            manual.interactable = chosen.HasManual;
            BroadcastUi.Place(manual.GetComponent<RectTransform>(), 0.04f, 0.18f, 0.96f, 0.28f);
            var auto = BroadcastUi.Button(card.transform, "LoadAuto", "ÚLTIMO SAVE AUTOMÁTICO  ·  " + autoWhen, () => AskLoadSlot(renameId, false),
                chosen.HasAuto ? new Color(0.1f, 0.24f, 0.38f, 1f) : new Color(1f, 1f, 1f, 0.08f), Color.white, 14);
            auto.interactable = chosen.HasAuto;
            BroadcastUi.Place(auto.GetComponent<RectTransform>(), 0.04f, 0.07f, 0.62f, 0.16f);
            var remove = BroadcastUi.Button(card.transform, "DeleteCareer", "APAGAR", () => AskDeleteSlot(renameId, chosen.Label), new Color(0.28f, 0.08f, 0.12f, 1f), Color.white, 14);
            BroadcastUi.Place(remove.GetComponent<RectTransform>(), 0.66f, 0.07f, 0.96f, 0.16f);
        }

        void AskLoadSlot(string id, bool manual)
        {
            _pendingNamedSaveId = id;
            _pendingLoadManual = manual;
            if (_confirm != null)
                Destroy(_confirm);
            _confirm = new GameObject("ConfirmLoadNamedCareer", typeof(RectTransform));
            _confirm.transform.SetParent(_root.transform, false);
            var rect = _confirm.GetComponent<RectTransform>();
            BroadcastUi.Stretch(rect);
            var dim = BroadcastUi.Solid(rect, "Dim", new Color(0f, 0f, 0f, 0f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            BroadcastUi.Catch(dim, () => Destroy(_confirm));
            var card = BroadcastUi.Panel(rect, "Card", new Color(0.1f, 0.04f, 0.2f, 1f), true);
            BroadcastUi.Place(card.rectTransform, 0.29f, 0.34f, 0.71f, 0.68f);
            var which = manual ? "manual" : "automático";
            var message = BroadcastUi.Label(card.transform, "Message", "Abrir o último save " + which + " desta carreira? A carreira que está aberta passa a ser esta.",
                17, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            BroadcastUi.Place(message.rectTransform, 0.06f, 0.38f, 0.94f, 0.92f);
            var yes = BroadcastUi.Button(card.transform, "Load", "CARREGAR", ConfirmLoadNamedCareer, UiTheme.Accent, UiTheme.ButtonText, 14);
            BroadcastUi.Place(yes.GetComponent<RectTransform>(), 0.06f, 0.08f, 0.47f, 0.31f);
            var no = BroadcastUi.Button(card.transform, "Cancel", "CANCELAR", () => Destroy(_confirm), new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 15);
            BroadcastUi.Place(no.GetComponent<RectTransform>(), 0.53f, 0.08f, 0.94f, 0.31f);
        }

        void AskDeleteSlot(string id, string label)
        {
            if (_confirm != null)
                Destroy(_confirm);
            _confirm = new GameObject("ConfirmDeleteCareer", typeof(RectTransform));
            _confirm.transform.SetParent(_root.transform, false);
            var rect = _confirm.GetComponent<RectTransform>();
            BroadcastUi.Stretch(rect);
            var dim = BroadcastUi.Solid(rect, "Dim", new Color(0f, 0f, 0f, 0f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            BroadcastUi.Catch(dim, () => Destroy(_confirm));
            var card = BroadcastUi.Panel(rect, "Card", new Color(0.1f, 0.04f, 0.2f, 1f), true);
            BroadcastUi.Place(card.rectTransform, 0.29f, 0.34f, 0.71f, 0.68f);
            var message = BroadcastUi.Label(card.transform, "Message", "Apagar “" + label + "”? Os saves manual e automático desta carreira deixam de existir.",
                17, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            BroadcastUi.Place(message.rectTransform, 0.06f, 0.38f, 0.94f, 0.92f);
            var yes = BroadcastUi.Button(card.transform, "Delete", "APAGAR", () =>
            {
                var active = CurrentCareer();
                var wasActive = active != null && active.FileId == id;
                CareerStore.DeleteSlot(id);
                if (wasActive)
                {
                    CareerStore.DeleteActiveFile();
                    if (GameRoot.Instance != null && GameRoot.Instance.Session != null)
                        GameRoot.Instance.Session.ClearCareer();
                    if (_saveManager != null)
                        Destroy(_saveManager);
                    if (_confirm != null)
                        Destroy(_confirm);
                    LeaveToMenu();
                    return;
                }
                if (_selectedSlot == id)
                    _selectedSlot = null;
                if (_confirm != null)
                    Destroy(_confirm);
                OpenSaveManager();
            }, new Color(0.28f, 0.08f, 0.12f, 1f), Color.white, 14);
            BroadcastUi.Place(yes.GetComponent<RectTransform>(), 0.06f, 0.08f, 0.47f, 0.31f);
            var no = BroadcastUi.Button(card.transform, "Cancel", "CANCELAR", () => Destroy(_confirm), new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 15);
            BroadcastUi.Place(no.GetComponent<RectTransform>(), 0.53f, 0.08f, 0.94f, 0.31f);
        }

        void ConfirmLoadNamedCareer()
        {
            var id = _pendingNamedSaveId;
            var manual = _pendingLoadManual;
            _pendingNamedSaveId = null;
            var session = GameRoot.Instance == null ? null : GameRoot.Instance.Session;
            var loaded = session == null ? null : CareerStore.TryLoadSlot(session, id, manual);
            if (loaded == null)
            {
                if (_feedback != null)
                    _feedback.text = "Não foi possível abrir esse save. A carreira atual continua ativa.";
                if (_confirm != null)
                    Destroy(_confirm);
                return;
            }
            var saved = CareerStore.TrySave(loaded);
            if (GameRoot.Instance.Session != null)
                GameRoot.Instance.Session.Restore(loaded);
            GameRoot.Instance.LiveMatch = null;
            if (_saveManager != null)
                Destroy(_saveManager);
            if (_confirm != null)
                Destroy(_confirm);
            _root.SetActive(false);
            IsVisible = false;
            ControlChrome.SettingsClosed();
            GameRoot.Instance.Open(SceneNames.CareerHub);
            if (!saved)
                UnityEngine.Debug.LogWarning("O save foi carregado, mas não foi possível atualizar a gravação principal.");
        }

        void OpenControls()
        {
            CloseControls();
            if (_root == null)
                return;
            var english = LocalizationManager.Current == GameLanguage.English;
            _controls = new GameObject("ControlsPage", typeof(RectTransform));
            _controls.transform.SetParent(_root.transform, false);
            BroadcastUi.Stretch(_controls.GetComponent<RectTransform>());
            var dim = BroadcastUi.Solid(_controls.transform, "Dim", new Color(0.03f, 0.02f, 0.06f, 1f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            var title = BroadcastUi.Label(_controls.transform, "Title", english ? "CONTROLS" : "CONTROLOS", 28, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.06f, 0.9f, 0.7f, 0.97f);
            _rebindStatus = BroadcastUi.Label(_controls.transform, "Status", english ? "Press a key or a button. Escape cancels." : "Carrega numa tecla ou num botão. Escape cancela.", 14, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(_rebindStatus.rectTransform, 0.06f, 0.845f, 0.7f, 0.895f);
            var reset = BroadcastUi.Button(_controls.transform, "ResetControls", english ? "DEFAULTS" : "PADRÃO", () =>
            {
                CancelRebind();
                ControlBindings.ResetAll();
                OpenControls();
            }, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 14);
            BroadcastUi.Place(reset.GetComponent<RectTransform>(), 0.72f, 0.9f, 0.86f, 0.96f);
            var close = BroadcastUi.Button(_controls.transform, "CloseControls", english ? "BACK" : "VOLTAR", CloseControls, UiTheme.Accent, UiTheme.ButtonText, 14);
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.87f, 0.9f, 0.96f, 0.96f);

            var tabNames = english
                ? new[] { "MOVEMENT", "MATCH", "MENUS" }
                : new[] { "MOVIMENTO", "PARTIDA", "MENUS" };
            for (var t = 0; t < tabNames.Length; t++)
            {
                var index = t;
                var tab = BroadcastUi.Button(_controls.transform, "ControlTab" + t, tabNames[t], () =>
                {
                    CancelRebind();
                    _controlTab = index;
                    OpenControls();
                }, _controlTab == t ? UiTheme.Accent : new Color(0.22f, 0.12f, 0.38f, 1f), _controlTab == t ? UiTheme.ButtonText : Color.white, 14);
                var x0 = 0.06f + t * 0.22f;
                BroadcastUi.Place(tab.GetComponent<RectTransform>(), x0, 0.77f, x0 + 0.2f, 0.835f);
            }

            var padHead = BroadcastUi.Label(_controls.transform, "PadHead", english ? "CONTROLLER" : "COMANDO", 13, UiTheme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(padHead.rectTransform, 0.34f, 0.72f, 0.62f, 0.765f);
            var keyHead = BroadcastUi.Label(_controls.transform, "KeyHead", english ? "KEYBOARD" : "TECLADO", 13, UiTheme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(keyHead.rectTransform, 0.66f, 0.72f, 0.96f, 0.765f);

            var list = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup));
            list.transform.SetParent(_controls.transform, false);
            var listRect = list.GetComponent<RectTransform>();
            BroadcastUi.Place(listRect, 0.05f, 0.04f, 0.95f, 0.71f);
            var layout = list.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            FillControlRows(list.transform, english);
        }

        void FillControlRows(Transform parent, bool english)
        {
            var tabs = new[] { "move", "match", "menu" };
            var selected = tabs[Mathf.Clamp(_controlTab, 0, tabs.Length - 1)];
            var pairs = ControlBindings.Pairs();
            var shown = 0;
            for (var i = 0; i < pairs.Count; i++)
            {
                var pair = pairs[i];
                if (pair.Tab != selected)
                    continue;
                var line = new GameObject("Row" + shown, typeof(RectTransform), typeof(LayoutElement));
                line.transform.SetParent(parent, false);
                line.GetComponent<LayoutElement>().preferredHeight = 44f;
                var label = BroadcastUi.Label(line.transform, "Name", pair.Title, 16, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(label.rectTransform, 0.02f, 0.08f, 0.32f, 0.92f);
                PlaceBind(line.transform, pair.Pad, pair.Pads, 0.34f, english ? "CONTROLLER" : "COMANDO");
                if (pair.Keys.Count == 0)
                    PlaceBind(line.transform, null, null, 0.66f, english ? "KEYBOARD" : "TECLADO");
                else if (pair.Keys.Count == 1)
                    PlaceBind(line.transform, pair.Keys[0], null, 0.66f, pair.Keys[0].Title);
                else
                {
                    var span = 0.3f / pair.Keys.Count;
                    for (var k = 0; k < pair.Keys.Count; k++)
                        PlaceBind(line.transform, pair.Keys[k], null, 0.66f + k * span, pair.Keys[k].Title, span - 0.008f);
                }
                shown++;
            }
        }

        void PlaceBind(Transform line, ControlBindings.BindingRow row, System.Collections.Generic.List<ControlBindings.BindingRow> mirrors, float x, string waiting, float width = 0.28f)
        {
            var current = row == null || row.Action == null ? "—" : row.Action.GetBindingDisplayString(row.BindingIndex);
            var captured = row;
            var extra = mirrors;
            var button = BroadcastUi.Button(line, "Bind" + x, string.IsNullOrEmpty(current) ? "—" : current, () => StartRebind(captured, extra, waiting), new Color(1f, 1f, 1f, 0.12f), Color.white, 14);
            BroadcastUi.Place(button.GetComponent<RectTransform>(), x, 0.08f, x + width, 0.92f);
        }

        void StartRebind(ControlBindings.BindingRow row, System.Collections.Generic.List<ControlBindings.BindingRow> mirrors, string waiting)
        {
            if (row == null || row.Action == null)
                return;
            CancelRebind();
            if (_rebindStatus != null)
                _rebindStatus.text = LocalizationManager.Current == GameLanguage.English
                    ? "Waiting for " + waiting
                    : "À espera de " + waiting;
            var operation = row.Action.PerformInteractiveRebinding(row.BindingIndex)
                .WithControlsExcluding("<Mouse>")
                .WithControlsExcluding("<Pen>")
                .WithControlsExcluding("<Touchscreen>")
                .WithCancelingThrough("<Keyboard>/escape");
            if (row.Keyboard)
                operation.WithControlsExcluding("<Gamepad>").WithControlsExcluding("<XInputController>");
            else
                operation.WithControlsExcluding("<Keyboard>");
            _rebind = operation.OnComplete(finished =>
            {
                finished.Dispose();
                _rebind = null;
                ControlBindings.MirrorPad(row, mirrors);
                if (row.Gameplay)
                    ControlBindings.SaveGame(row.Asset);
                else
                    ControlBindings.SaveUi(row.Asset);
                OpenControls();
            }).OnCancel(finished =>
            {
                finished.Dispose();
                _rebind = null;
                if (_rebindStatus != null)
                    _rebindStatus.text = LocalizationManager.Current == GameLanguage.English ? "Cancelled." : "Cancelado.";
            }).Start();
        }

        void CancelRebind()
        {
            if (_rebind == null)
                return;
            _rebind.Cancel();
            _rebind.Dispose();
            _rebind = null;
        }

        void CloseControls()
        {
            CancelRebind();
            if (_controls != null)
                Destroy(_controls);
            _controls = null;
            _rebindStatus = null;
        }

        void LeaveToMenu()
        {
            ApplyAndClose();
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == SceneNames.MainMenu)
            {
                var menu = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
                if (menu != null)
                    menu.RefreshAfterDelete();
                return;
            }
            if (GameRoot.Instance != null)
                GameRoot.Instance.Open(SceneNames.MainMenu);
        }

        void ConfirmRestoreAutoSave()
        {
            if (!CareerStore.HasAutoSave(_restoreSlot))
            {
                if (_feedback != null)
                    _feedback.text = "Esta cópia ainda não existe. Avança dias suficientes para preencher o espaço.";
                return;
            }
            if (_confirm != null)
                Destroy(_confirm);
            _confirm = new GameObject("ConfirmRestoreAutoSave", typeof(RectTransform));
            _confirm.transform.SetParent(_root.transform, false);
            var rect = _confirm.GetComponent<RectTransform>();
            BroadcastUi.Stretch(rect);
            var dim = BroadcastUi.Solid(rect, "Dim", new Color(0f, 0f, 0f, 0f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            BroadcastUi.Catch(dim, () => Destroy(_confirm));
            var card = BroadcastUi.Panel(rect, "Card", new Color(0.1f, 0.04f, 0.2f, 1f), true);
            BroadcastUi.Place(card.rectTransform, 0.29f, 0.36f, 0.71f, 0.66f);
            var message = BroadcastUi.Label(card.transform, "Message", "Restaurar a cópia " + _restoreSlot + "? A carreira ativa será substituída pelo ponto guardado.", 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(message.rectTransform, 0.06f, 0.38f, 0.94f, 0.92f);
            var yes = BroadcastUi.Button(card.transform, "Restore", "RESTAURAR", RestoreAutoSave, UiTheme.Accent, UiTheme.ButtonText, 15);
            BroadcastUi.Place(yes.GetComponent<RectTransform>(), 0.06f, 0.08f, 0.47f, 0.31f);
            var no = BroadcastUi.Button(card.transform, "Cancel", "CANCELAR", () => Destroy(_confirm), new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 15);
            BroadcastUi.Place(no.GetComponent<RectTransform>(), 0.53f, 0.08f, 0.94f, 0.31f);
        }

        void RestoreAutoSave()
        {
            var session = GameRoot.Instance == null ? null : GameRoot.Instance.Session;
            var loaded = session == null ? null : CareerStore.TryLoadAutoSave(session, _restoreSlot);
            if (loaded == null)
            {
                if (_feedback != null)
                    _feedback.text = "Não foi possível abrir o autosave. A gravação principal continua intacta.";
                if (_confirm != null)
                    Destroy(_confirm);
                return;
            }
            GameRoot.Instance.LiveMatch = null;
            _root.SetActive(false);
            IsVisible = false;
            if (_confirm != null)
                Destroy(_confirm);
            GameRoot.Instance.Open(SceneNames.CareerHub);
        }

        static CareerState CurrentCareer()
        {
            return GameRoot.Instance == null || GameRoot.Instance.Session == null ? null : GameRoot.Instance.Session.Career;
        }

        string RestoreSlotLabel()
        {
            var slots = SettingsPrefs.AutosaveSlots;
            _restoreSlot = Mathf.Clamp(_restoreSlot, 1, slots);
            var career = CurrentCareer();
            return CareerStore.HasAutoSave(_restoreSlot)
                ? CareerStore.AutoSaveLabel(_restoreSlot, career == null ? null : career.World)
                : "CÓPIA " + _restoreSlot + " · VAZIA";
        }

        void CycleRestoreSlot()
        {
            _restoreSlot = _restoreSlot % SettingsPrefs.AutosaveSlots + 1;
            Refresh();
        }

        static string Percent(float value) => Mathf.RoundToInt(value * 100f) + "%";
        static string OnOff(bool value) => value ? "LIGADO" : "DESLIGADO";
        static bool EventSystemTyping()
        {
            var current = EventSystem.current;
            if (current == null || current.currentSelectedGameObject == null)
                return false;
            return current.currentSelectedGameObject.GetComponentInParent<InputField>() != null;
        }
    }

    static class WindowsMusicPicker
    {
        public static string OpenFolder(string title)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var info = new BrowseInfo
            {
                owner = GetActiveWindow(),
                displayName = new StringBuilder(260),
                title = title,
                flags = 0x0001 | 0x0040
            };
            var item = SHBrowseForFolderW(ref info);
            if (item == IntPtr.Zero) return null;
            try
            {
                var path = new StringBuilder(1024);
                return SHGetPathFromIDListW(item, path) ? path.ToString() : null;
            }
            finally
            {
                CoTaskMemFree(item);
            }
#else
            return null;
#endif
        }

        public static string Open()
        {
            return OpenFiltered("Ficheiros de música\0*.mp3;*.wav;*.ogg;*.aiff;*.aif\0Todos os ficheiros\0*.*\0\0", "Escolher música", "mp3");
        }

        public static string OpenImage()
        {
            return OpenFiltered("Imagens\0*.png;*.jpg;*.jpeg\0Todos os ficheiros\0*.*\0\0", "Escolher fundo", "png");
        }

        public static string OpenImage(string title)
        {
            return OpenFiltered("Imagens\0*.png;*.jpg;*.jpeg\0Todos os ficheiros\0*.*\0\0", title, "png");
        }

        static string OpenFiltered(string filter, string title, string extension)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var dialog = new OpenFileName();
            dialog.filter = Marshal.StringToHGlobalUni(filter);
            try
            {
                dialog.structSize = Marshal.SizeOf(dialog);
                dialog.dlgOwner = GetActiveWindow();
                dialog.file = new string(new char[1024]);
                dialog.maxFile = dialog.file.Length;
                dialog.fileTitle = new string(new char[256]);
                dialog.maxFileTitle = dialog.fileTitle.Length;
                dialog.title = title;
                dialog.defExt = extension;
                dialog.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008;
                if (!GetOpenFileName(dialog))
                    return null;
                var chosen = dialog.file == null ? "" : dialog.file.Trim('\0', ' ');
                return string.IsNullOrEmpty(chosen) ? null : chosen;
            }
            finally
            {
                if (dialog.filter != IntPtr.Zero)
                    Marshal.FreeHGlobal(dialog.filter);
            }
#else
            return null;
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct BrowseInfo
        {
            public IntPtr owner;
            public IntPtr root;
            [MarshalAs(UnmanagedType.LPWStr)] public StringBuilder displayName;
            [MarshalAs(UnmanagedType.LPWStr)] public string title;
            public uint flags;
            public IntPtr callback;
            public IntPtr parameter;
            public int image;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHBrowseForFolderW")]
        static extern IntPtr SHBrowseForFolderW(ref BrowseInfo info);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHGetPathFromIDListW")]
        static extern bool SHGetPathFromIDListW(IntPtr item, StringBuilder path);

        [DllImport("ole32.dll")]
        static extern void CoTaskMemFree(IntPtr memory);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        class OpenFileName
        {
            public int structSize;
            public IntPtr dlgOwner;
            public IntPtr instance;
            public IntPtr filter;
            public string customFilter;
            public int maxCustFilter;
            public int filterIndex;
            public string file;
            public int maxFile;
            public string fileTitle;
            public int maxFileTitle;
            public string initialDir;
            public string title;
            public int flags;
            public short fileOffset;
            public short fileExtension;
            public string defExt;
            public IntPtr custData;
            public IntPtr hook;
            public string templateName;
            public IntPtr reservedPtr;
            public int reservedInt;
            public int flagsEx;
        }

        [DllImport("Comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

        [DllImport("user32.dll")]
        static extern IntPtr GetActiveWindow();
#endif
    }

    public sealed class DragVolume : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        Image _fill;
        Image _handle;
        UnityEngine.Events.UnityAction<float> _changed;

        public void Setup(Image fill, Image handle, float value, UnityEngine.Events.UnityAction<float> changed)
        {
            _fill = fill;
            _handle = handle;
            _changed = changed;
            Set(value);
        }

        public void Set(float value)
        {
            value = Mathf.Clamp01(value);
            if (_fill != null)
                _fill.rectTransform.anchorMax = new Vector2(value, 1f);
            if (_handle != null)
            {
                _handle.rectTransform.anchorMin = new Vector2(value, 0.5f);
                _handle.rectTransform.anchorMax = new Vector2(value, 0.5f);
            }
        }

        public void Step(float amount)
        {
            var current = _fill == null ? 0f : _fill.rectTransform.anchorMax.x;
            current = Mathf.Clamp01(current + amount);
            Set(current);
            _changed?.Invoke(current);
        }

        public void OnPointerDown(PointerEventData eventData) => Apply(eventData);
        public void OnDrag(PointerEventData eventData) => Apply(eventData);

        void Apply(PointerEventData eventData)
        {
            var rect = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out var local))
                return;
            var width = rect.rect.width;
            if (width < 1f)
                return;
            var value = Mathf.Clamp01((local.x - rect.rect.xMin) / width);
            Set(value);
            if (_changed != null)
                _changed.Invoke(value);
        }
    }

    public sealed class HueSaturationWheel : MaskableGraphic, IPointerDownHandler, IDragHandler
    {
        const int Segments = 72;
        const int Rings = 14;
        float _hue;
        float _saturation;
        RectTransform _marker;
        System.Action<float, float> _changed;

        public void SetColorChanged(System.Action<float, float> changed) => _changed = changed;

        public void SetSelection(float hue, float saturation)
        {
            _hue = Mathf.Repeat(hue, 1f);
            _saturation = Mathf.Clamp01(saturation);
            if (_marker == null)
                _marker = transform.Find("WheelSelection") as RectTransform;
            if (_marker != null)
            {
                var radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
                var angle = _hue * Mathf.PI * 2f;
                _marker.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius * _saturation);
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            var rect = rectTransform.rect;
            var center = rect.center;
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            if (radius <= 0f)
                return;
            for (var ring = 0; ring < Rings; ring++)
            {
                var inner = radius * ring / Rings;
                var outer = radius * (ring + 1) / Rings;
                for (var segment = 0; segment < Segments; segment++)
                {
                    var a0 = segment * Mathf.PI * 2f / Segments;
                    var a1 = (segment + 1) * Mathf.PI * 2f / Segments;
                    var hue0 = segment / (float)Segments;
                    var hue1 = (segment + 1f) / Segments;
                    var sat0 = inner / radius;
                    var sat1 = outer / radius;
                    AddWheelQuad(vertices,
                        center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * inner,
                        center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * inner,
                        center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * outer,
                        center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * outer,
                        Color.HSVToRGB(hue0, sat0, 1f), Color.HSVToRGB(hue1, sat0, 1f),
                        Color.HSVToRGB(hue1, sat1, 1f), Color.HSVToRGB(hue0, sat1, 1f));
                }
            }
        }

        static void AddWheelQuad(VertexHelper vertices, Vector2 a, Vector2 b, Vector2 c, Vector2 d,
            Color colorA, Color colorB, Color colorC, Color colorD)
        {
            var start = vertices.currentVertCount;
            var va = UIVertex.simpleVert; va.position = a; va.color = colorA;
            var vb = UIVertex.simpleVert; vb.position = b; vb.color = colorB;
            var vc = UIVertex.simpleVert; vc.position = c; vc.color = colorC;
            var vd = UIVertex.simpleVert; vd.position = d; vd.color = colorD;
            vertices.AddVert(va); vertices.AddVert(vb); vertices.AddVert(vc); vertices.AddVert(vd);
            vertices.AddTriangle(start, start + 1, start + 2);
            vertices.AddTriangle(start, start + 2, start + 3);
        }

        public void OnPointerDown(PointerEventData eventData) => Pick(eventData);
        public void OnDrag(PointerEventData eventData) => Pick(eventData);

        void Pick(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position,
                    eventData.pressEventCamera, out var local))
                return;
            var rect = rectTransform.rect;
            var dx = local.x - rect.center.x;
            var dy = local.y - rect.center.y;
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            if (radius <= 0f)
                return;
            _saturation = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / radius);
            _hue = Mathf.Repeat(Mathf.Atan2(dy, dx) / (Mathf.PI * 2f), 1f);
            SetSelection(_hue, _saturation);
            _changed?.Invoke(_hue, _saturation);
        }
    }
}
