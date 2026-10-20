using System;
using System.Collections.Generic;
using CoachMaster.App;
using CoachMaster.Career;
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
            "Ecrã e gráficos", "Jogabilidade", "Dia de jogo", "Notificações", "Som", "Realismo", "Carreira e saves", "Acessibilidade", "Avançadas"
        };

        static readonly string[] PageNotes =
        {
            "Imagem, desempenho e escala dos menus.",
            "Controlos que alteram a preparação e o ritmo da partida.",
            "Pausas automáticas e informação visível durante o jogo.",
            "Avisos de carreira. Os acontecimentos continuam no histórico.",
            "Volumes separados; usa esquerda/direita no comando para ajustar.",
            "Ajustes da simulação guardados dentro da carreira.",
            "Autosaves rotativos, gravação manual e recuperação.",
            "Escala do interface, movimento e ajuda de comando.",
            "Presets que alteram apenas as opções de simulação suportadas."
        };

        GameObject _root;
        GameObject _confirm;
        GameObject _saveManager;
        InputField _saveName;
        GameObject[] _pages;
        Button[] _tabs;
        Text _now;
        Text _feedback;
        Button _apply;
        Button _cancel;
        Button _defaults;
        Button _saveCareer;
        Button _menu;
        SettingsPrefs.Snapshot _snapshot;
        CareerState _career;
        int _originalInjury;
        int _originalReferee;
        int _originalPreset;
        int _page;
        int _restoreSlot = 1;
        string _pendingNamedSaveId;
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
                if (_root != null && _root.activeSelf)
                    Refresh();
            }

            if (!Keyboard.current.mKey.wasPressedThisFrame || EventSystemTyping())
                return;
            Toggle();
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
            _snapshot = SettingsPrefs.Capture();
            _career = CurrentCareer();
            if (_career != null)
            {
                _originalInjury = _career.InjuryFrequency;
                _originalReferee = _career.RefereeStrictness;
                _originalPreset = _career.RealismPreset;
            }
            if (_feedback != null)
                _feedback.text = "As alterações seguras aplicam-se de imediato. Carrega em Aplicar para as guardar.";
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
            scaler.referenceResolution = new Vector2(1920f / SettingsPrefs.UiScale, 1080f / SettingsPrefs.UiScale);
            scaler.matchWidthOrHeight = 0.5f;
            _root = canvasGo;

            var dim = BroadcastUi.Solid(canvasGo.transform, "Dim", new Color(0.03f, 0.01f, 0.08f, 0.96f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            var card = BroadcastUi.Panel(canvasGo.transform, "SettingsCard", new Color(0.1f, 0.04f, 0.2f, 1f), true);
            BroadcastUi.Place(card.rectTransform, 0.025f, 0.035f, 0.975f, 0.965f);
            var title = BroadcastUi.Label(card.transform, "Title", "DEFINIÇÕES", 30, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.035f, 0.91f, 0.52f, 0.99f);
            var close = BroadcastUi.CloseX(card.transform, "CloseSettings", ApplyAndClose);
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.94f, 0.91f, 0.99f, 0.99f);

            var side = BroadcastUi.Panel(card.transform, "SettingsNavigation", new Color(1f, 1f, 1f, 0.045f));
            BroadcastUi.Place(side.rectTransform, 0.025f, 0.145f, 0.235f, 0.895f);
            var content = BroadcastUi.Panel(card.transform, "SettingsContent", new Color(1f, 1f, 1f, 0.045f));
            BroadcastUi.Place(content.rectTransform, 0.255f, 0.145f, 0.975f, 0.895f);

            _tabs = new Button[Categories.Length];
            _pages = new GameObject[Categories.Length];
            for (var i = 0; i < Categories.Length; i++)
            {
                var index = i;
                var y = 0.91f - i * 0.085f;
                _tabs[i] = BroadcastUi.Button(side.transform, "Category" + i, Categories[i], () => SelectPage(index),
                    new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 14);
                BroadcastUi.Place(_tabs[i].GetComponent<RectTransform>(), 0.055f, y - 0.07f, 0.945f, y);
            }

            BuildDisplayPage(content.transform);
            BuildGameplayPage(content.transform);
            BuildMatchPage(content.transform);
            BuildNotificationPage(content.transform);
            BuildAudioPage(content.transform);
            BuildRealismPage(content.transform);
            BuildCareerPage(content.transform);
            BuildAccessibilityPage(content.transform);
            BuildAdvancedPage(content.transform);

            _feedback = BroadcastUi.Label(card.transform, "SettingsFeedback", "", 12, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(_feedback.rectTransform, 0.27f, 0.105f, 0.95f, 0.145f);
            _apply = FooterButton(card.transform, "Apply", "APLICAR", ApplyChanges, 0.025f, 0.025f, 0.205f, 0.09f, UiTheme.Accent, UiTheme.ButtonText);
            _cancel = FooterButton(card.transform, "Cancel", "CANCELAR", CancelChanges, 0.22f, 0.025f, 0.4f, 0.09f, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white);
            _defaults = FooterButton(card.transform, "Defaults", "REPOR PADRÃO", RestoreDefaults, 0.415f, 0.025f, 0.595f, 0.09f, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white);
            _saveCareer = FooterButton(card.transform, "SaveCareer", "GUARDAR CARREIRA", SaveCareer, 0.61f, 0.025f, 0.805f, 0.09f, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white);
            _menu = FooterButton(card.transform, "MainMenu", "MENU", LeaveToMenu, 0.82f, 0.025f, 0.975f, 0.09f, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white);
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
            AddOption(page.transform, "UI", "Escala do interface", "Ajusta todos os ecrãs sem alterar a resolução do jogo.", () => Percent(SettingsPrefs.UiScale), () => { SettingsPrefs.UiScaleIndex = (SettingsPrefs.UiScaleIndex + 1) % 5; GameRoot.ApplyUiScale(); Refresh(); }, 0.21f);
            AddOption(page.transform, "Background", "Imagem de fundo", "Automático acompanha a liga da carreira.", () => BackgroundPrefs.Label, () => { BackgroundPrefs.Cycle(); Refresh(); }, 0.10f);
        }

        void BuildGameplayPage(Transform parent)
        {
            var page = Page(parent, 1, "JOGABILIDADE");
            AddOption(page.transform, "AutoXI", "Escolha automática do onze", "Ignora o onze guardado e escolhe a equipa a partir da formação e posições.", () => OnOff(SettingsPrefs.AutomaticSquadSelection), () => SettingsPrefs.AutomaticSquadSelection = !SettingsPrefs.AutomaticSquadSelection, 0.76f);
            AddOption(page.transform, "Training", "Treino automático", "Quando desligado, avançar dias não altera a avaliação de treino dos jogadores.", () => OnOff(SettingsPrefs.AutomaticTraining), () => SettingsPrefs.AutomaticTraining = !SettingsPrefs.AutomaticTraining, 0.63f);
            AddOption(page.transform, "Speed", "Velocidade inicial do jogo", "Também pode ser alterada durante a partida.", () => SettingsPrefs.MatchSpeedLabel, () => SettingsPrefs.MatchSpeedIndex = (SettingsPrefs.MatchSpeedIndex + 1) % 4, 0.50f);
            AddOption(page.transform, "Highlights", "Momentos apresentados", "Define quantos anúncios de jogo interrompem brevemente a simulação.", () => SettingsPrefs.MatchHighlightsLabel, () => SettingsPrefs.MatchHighlights = (SettingsPrefs.MatchHighlights + 1) % 4, 0.37f);
            AddText(page.transform, "MatchSpeedHelp", "O ritmo e os momentos escolhidos aplicam-se à apresentação; não alteram o resultado matemático da partida.", 15, UiTheme.Muted, 0.10f, 0.24f);
        }

        void BuildMatchPage(Transform parent)
        {
            var page = Page(parent, 2, "DIA DE JOGO");
            AddOption(page.transform, "PauseGoal", "Pausar depois de um golo", "A pausa começa depois de a bola entrar e o golo ser confirmado.", () => OnOff(SettingsPrefs.PauseOnGoal), () => SettingsPrefs.PauseOnGoal = !SettingsPrefs.PauseOnGoal, 0.76f);
            AddOption(page.transform, "PauseRed", "Pausar depois de um vermelho", "Abre a pausa para poderes rever a equipa e as táticas.", () => OnOff(SettingsPrefs.PauseOnRedCard), () => SettingsPrefs.PauseOnRedCard = !SettingsPrefs.PauseOnRedCard, 0.63f);
            AddOption(page.transform, "PauseInjury", "Pausar depois de uma lesão", "Abre a pausa quando uma lesão é confirmada na partida.", () => OnOff(SettingsPrefs.PauseOnInjury), () => SettingsPrefs.PauseOnInjury = !SettingsPrefs.PauseOnInjury, 0.50f);
            AddOption(page.transform, "Ratings", "Pontuações ao vivo", "Mostra ou oculta a página de classificações dos jogadores.", () => OnOff(SettingsPrefs.LiveRatings), () => SettingsPrefs.LiveRatings = !SettingsPrefs.LiveRatings, 0.37f);
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
            _now = BroadcastUi.Label(page.transform, "Now", "", 13, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(_now.rectTransform, 0.04f, 0.80f, 0.94f, 0.85f);
            var previous = BroadcastUi.Button(page.transform, "PreviousTrack", "ANTERIOR", () => { GameAudio.PreviousTrack(); Refresh(); }, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 12);
            BroadcastUi.Place(previous.GetComponent<RectTransform>(), 0.04f, 0.73f, 0.32f, 0.79f);
            var repeat = BroadcastUi.Button(page.transform, "RepeatTrack", "REPETIR", () => { GameAudio.Repeat = !GameAudio.Repeat; Refresh(); }, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 12);
            BroadcastUi.Place(repeat.GetComponent<RectTransform>(), 0.36f, 0.73f, 0.64f, 0.79f);
            _buttonValues[repeat] = () => GameAudio.Repeat ? "REPETIÇÃO LIGADA" : "REPETIR";
            var next = BroadcastUi.Button(page.transform, "NextTrack", "SEGUINTE", () => { GameAudio.NextTrack(); Refresh(); }, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 12);
            BroadcastUi.Place(next.GetComponent<RectTransform>(), 0.68f, 0.73f, 0.96f, 0.79f);
            AddVolume(page.transform, "Master", "Volume geral", "Nível comum a todos os sons.", GameAudio.MasterVolume, GameAudio.SetMasterVolume, 0.61f);
            AddVolume(page.transform, "Music", "Música", "Música dos menus.", GameAudio.MusicVolume, GameAudio.SetMusicVolume, 0.50f);
            AddVolume(page.transform, "MatchEffects", "Efeitos do jogo", "Apito, bola, cartões e outros efeitos da partida.", GameAudio.EffectVolume, GameAudio.SetEffectVolume, 0.39f);
            AddVolume(page.transform, "Crowd", "Público", "Ambiente e reação das bancadas.", GameAudio.CrowdVolume, GameAudio.SetCrowdVolume, 0.28f);
            AddVolume(page.transform, "MenuSounds", "Interface", "Cliques e sons dos menus.", GameAudio.MenuVolume, GameAudio.SetMenuVolume, 0.17f);
            AddCompactOption(page.transform, "MuteBackground", "Em segundo plano", () => OnOff(SettingsPrefs.MuteWhenBackground), () => SettingsPrefs.MuteWhenBackground = !SettingsPrefs.MuteWhenBackground, 0.04f, 0.04f, 0.49f);
            AddCompactOption(page.transform, "MuteAll", "Silenciar tudo", () => OnOff(GameAudio.MuteAll), () => GameAudio.SetMuteAll(!GameAudio.MuteAll), 0.51f, 0.04f, 0.96f);
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
            var page = Page(parent, 6, "CARREIRA E SAVES");
            AddOption(page.transform, "Autosave", "Autosave automático", "Cria uma recuperação rotativa quando a carreira avança o número de dias escolhido.", () => OnOff(SettingsPrefs.AutosaveEnabled), () => SettingsPrefs.AutosaveEnabled = !SettingsPrefs.AutosaveEnabled, 0.76f);
            AddOption(page.transform, "BeforeMatch", "Guardar antes dos jogos", "Cria uma cópia rotativa antes de abrir uma partida, além do autosave por dias.", () => OnOff(SettingsPrefs.AutosaveBeforeMatch), () => SettingsPrefs.AutosaveBeforeMatch = !SettingsPrefs.AutosaveBeforeMatch, 0.63f);
            AddOption(page.transform, "AutosaveDays", "Frequência", "Intervalo medido em dias da carreira.", () => SettingsPrefs.AutosaveDaysLabel, () => SettingsPrefs.AutosaveDaysIndex = (SettingsPrefs.AutosaveDaysIndex + 1) % 4, 0.50f);
            AddOption(page.transform, "AutosaveSlots", "Cópias rotativas", "Número de recuperações mantidas à medida que os autosaves avançam.", () => SettingsPrefs.AutosaveSlotsLabel, () => SettingsPrefs.AutosaveSlotsIndex = (SettingsPrefs.AutosaveSlotsIndex + 1) % 3, 0.37f);
            AddOption(page.transform, "RestoreSlot", "Cópia a restaurar", "Escolhe entre as cópias rotativas disponíveis.", RestoreSlotLabel, CycleRestoreSlot, 0.24f);
            AddAction(page.transform, "LoadAutoSave", "Restaurar cópia selecionada", "Substitui a carreira ativa pelo ponto de recuperação selecionado.", ConfirmRestoreAutoSave, 0.11f);
            AddAction(page.transform, "NamedSaves", "Gestor de gravações", "Cria uma cópia com nome e permite abrir gravações anteriores.", OpenSaveManager, 0.005f);
        }

        void BuildAccessibilityPage(Transform parent)
        {
            var page = Page(parent, 7, "INTERFACE E ACESSIBILIDADE");
            AddOption(page.transform, "Scale", "Escala do interface", "Aumenta ou reduz todos os ecrãs. A resolução permanece igual.", () => Percent(SettingsPrefs.UiScale), () => { SettingsPrefs.UiScaleIndex = (SettingsPrefs.UiScaleIndex + 1) % 5; GameRoot.ApplyUiScale(); Refresh(); }, 0.76f);
            AddOption(page.transform, "Motion", "Reduzir animações", "Encurta os fades das mensagens e transições de carregamento.", () => OnOff(SettingsPrefs.ReduceMotion), () => SettingsPrefs.ReduceMotion = !SettingsPrefs.ReduceMotion, 0.63f);
            AddOption(page.transform, "ControllerHints", "Ajuda de teclado e comando", "Mostra os controlos de navegação no rodapé dos ecrãs.", () => OnOff(SettingsPrefs.ControllerHints), () => SettingsPrefs.ControllerHints = !SettingsPrefs.ControllerHints, 0.50f);
            AddOption(page.transform, "Language", "Idioma", "Escolhe o idioma do jogo.", LanguageLabel, CycleLanguage, 0.37f);
        }

        void BuildAdvancedPage(Transform parent)
        {
            var page = Page(parent, 8, "AVANÇADAS");
            AddOption(page.transform, "AdvancedPreset", "Preset de realismo", "Atualiza apenas as opções de simulação que já têm efeito no jogo.", CurrentPresetLabel, CycleRealismPreset, 0.76f);
            AddText(page.transform, "SupportedAdvanced", "O jogo ainda não tem sistemas de transferências automáticas de clubes, despedimentos de treinadores, conferências de imprensa ou mods. Não aparecem aqui controlos que não façam nada.", 15, UiTheme.Muted, 0.12f, 0.56f);
        }

        GameObject Page(Transform parent, int index, string heading)
        {
            var page = BroadcastUi.Panel(parent, "SettingsPage" + index, new Color(0f, 0f, 0f, 0.001f));
            BroadcastUi.Place(page.rectTransform, 0.025f, 0.025f, 0.975f, 0.975f);
            _pages[index] = page.gameObject;
            var title = BroadcastUi.Label(page.transform, "PageTitle", heading, 18, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.04f, 0.92f, 0.94f, 0.99f);
            var note = BroadcastUi.Label(page.transform, "PageNote", PageNotes[index], 13, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(note.rectTransform, 0.04f, 0.86f, 0.94f, 0.92f);
            return page.gameObject;
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
                Refresh();
            });
            BroadcastUi.Place(button.GetComponent<RectTransform>(), x0, y, x1, y + 0.09f);
            _buttonValues[button] = () => title + "  ·  " + value();
        }

        void AddVolume(Transform parent, string name, string title, string description, float value, UnityEngine.Events.UnityAction<float> changed, float y)
        {
            var row = BroadcastUi.Panel(parent, "Volume" + name, new Color(1f, 1f, 1f, 0.045f));
            BroadcastUi.Place(row.rectTransform, 0.035f, y, 0.965f, y + 0.105f);
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
            var trackNavigation = trackButton.navigation;
            trackNavigation.mode = Navigation.Mode.None;
            trackButton.navigation = trackNavigation;
            trackButton.targetGraphic = track;
            var fill = BroadcastUi.Solid(track.transform, "Fill", UiTheme.Accent);
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

        void ApplyChanges()
        {
            SettingsPrefs.Commit();
            var career = CurrentCareer();
            var careerSaved = career == null || CareerStore.TrySave(career);
            BeginEdit();
            if (_feedback != null)
                _feedback.text = careerSaved ? "Definições guardadas." : "Definições aplicadas, mas não foi possível guardar a carreira. A gravação anterior foi mantida.";
            Refresh();
        }

        void ApplyAndClose()
        {
            if (_root == null || !_root.activeSelf)
                return;
            CancelChanges();
        }

        public void CloseFromControl() => ApplyAndClose();

        void CancelChanges()
        {
            SettingsPrefs.Restore(_snapshot);
            var career = CurrentCareer();
            if (career != null)
            {
                career.InjuryFrequency = _originalInjury;
                career.RefereeStrictness = _originalReferee;
                career.RealismPreset = _originalPreset;
                CareerStore.Save(career);
            }
            GameRoot.ApplyUiScale();
            _root.SetActive(false);
            IsVisible = false;
            ControlChrome.SettingsClosed();
            MatchDayController.RefreshSettings();
        }

        void RestoreDefaults()
        {
            SettingsPrefs.RestoreDefaults();
            var career = CurrentCareer();
            SettingsPrefs.ApplyRealismPreset(career, 1);
            GameRoot.ApplyUiScale();
            SelectPage(_page);
            if (_feedback != null)
                _feedback.text = "Valores padrão carregados. Carrega em Aplicar para os guardar ou Cancelar para repor os anteriores.";
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
            if (_saveManager != null)
                Destroy(_saveManager);
            _saveManager = new GameObject("NamedSaveManager", typeof(RectTransform));
            _saveManager.transform.SetParent(_root.transform, false);
            var root = _saveManager.GetComponent<RectTransform>();
            BroadcastUi.Stretch(root);
            var dim = BroadcastUi.Solid(root, "Dim", new Color(0f, 0f, 0f, 0.78f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            var card = BroadcastUi.Panel(root, "Card", new Color(0.1f, 0.04f, 0.2f, 1f), true);
            BroadcastUi.Place(card.rectTransform, 0.22f, 0.12f, 0.78f, 0.88f);
            var title = BroadcastUi.Label(card.transform, "Title", "GESTOR DE GRAVAÇÕES", 21, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.06f, 0.9f, 0.8f, 0.98f);
            var close = BroadcastUi.CloseX(card.transform, "Close", () => Destroy(_saveManager));
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.9f, 0.9f, 0.97f, 0.98f);

            _saveName = UiFactory.Input(card.transform, "Nome desta gravação");
            _saveName.characterLimit = 80;
            BroadcastUi.Place(_saveName.GetComponent<RectTransform>(), 0.06f, 0.79f, 0.71f, 0.88f);
            var save = BroadcastUi.Button(card.transform, "SaveNamed", "CRIAR CÓPIA", SaveNamedCareer, UiTheme.Accent, UiTheme.ButtonText, 14);
            BroadcastUi.Place(save.GetComponent<RectTransform>(), 0.74f, 0.79f, 0.94f, 0.88f);

            var section = BroadcastUi.Label(card.transform, "SavedHeading", "GRAVAÇÕES GUARDADAS", 13, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(section.rectTransform, 0.06f, 0.72f, 0.94f, 0.78f);
            var scroll = BroadcastUi.Scroll(card.transform, "NamedSaveList", out var content);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.055f, 0.08f, 0.945f, 0.71f);

            var saves = CareerStore.ListNamedSaves();
            if (saves.Count == 0)
            {
                var empty = BroadcastUi.Label(content, "Empty", "Ainda não há cópias nomeadas.", 14, UiTheme.Muted, TextAnchor.MiddleLeft);
                var emptyLayout = empty.gameObject.AddComponent<LayoutElement>();
                emptyLayout.preferredHeight = 48f;
            }
            else
            {
                for (var i = 0; i < saves.Count; i++)
                {
                    var saveInfo = saves[i];
                    var clubName = saveInfo.ClubId;
                    var current = CurrentCareer();
                    if (current != null && current.World.Clubs.TryGetValue(saveInfo.ClubId, out var club))
                        clubName = club.ShortName;
                    var label = saveInfo.Name + "   ·   " + clubName + "   ·   " + saveInfo.SavedAt.ToString("dd/MM/yyyy HH:mm");
                    var button = BroadcastUi.Button(content, "NamedSave" + i, label, () => LoadNamedCareer(saveInfo.Id),
                        new Color(1f, 1f, 1f, 0.09f), Color.white, 13);
                    button.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
                }
            }

            var help = BroadcastUi.Label(card.transform, "SaveNote", "As cópias nomeadas ficam separadas dos autosaves e da gravação principal.", 12, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(help.rectTransform, 0.06f, 0.015f, 0.94f, 0.075f);
        }

        void SaveNamedCareer()
        {
            var career = CurrentCareer();
            if (career == null)
                return;
            var name = _saveName == null ? "" : _saveName.text;
            if (string.IsNullOrWhiteSpace(name))
            {
                var date = career.Calendar.CurrentDate;
                var seasonStart = date.Month >= 7 ? date.Year : date.Year - 1;
                name = career.Club.ShortName + " · " + career.Coach.Name + " · " + seasonStart + "/" + ((seasonStart + 1) % 100).ToString("00");
            }
            if (!CareerStore.SaveNamed(career, name, out var info))
            {
                if (_feedback != null)
                    _feedback.text = "Não foi possível criar a cópia nomeada. A gravação principal não foi alterada.";
                return;
            }
            if (_feedback != null)
                _feedback.text = "Cópia guardada: " + info.Name;
            OpenSaveManager();
        }

        void LoadNamedCareer(string id)
        {
            if (_confirm != null)
                Destroy(_confirm);
            _pendingNamedSaveId = id;
            var name = id;
            var saves = CareerStore.ListNamedSaves();
            for (var i = 0; i < saves.Count; i++)
                if (saves[i].Id == id)
                {
                    name = saves[i].Name;
                    break;
                }

            _confirm = new GameObject("ConfirmLoadNamedCareer", typeof(RectTransform));
            _confirm.transform.SetParent(_root.transform, false);
            var rect = _confirm.GetComponent<RectTransform>();
            BroadcastUi.Stretch(rect);
            var dim = BroadcastUi.Solid(rect, "Dim", new Color(0f, 0f, 0f, 0.78f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            var card = BroadcastUi.Panel(rect, "Card", new Color(0.1f, 0.04f, 0.2f, 1f), true);
            BroadcastUi.Place(card.rectTransform, 0.29f, 0.34f, 0.71f, 0.68f);
            var confirmMessage = LocalizationManager.Current == GameLanguage.English
                ? "Open “" + name + "”? The active career will be replaced. The saved copy will remain available."
                : "Abrir “" + name + "”? A carreira ativa será substituída. A cópia guardada mantém-se.";
            var message = BroadcastUi.Label(card.transform, "Message", confirmMessage,
                17, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            message.resizeTextForBestFit = true;
            message.resizeTextMinSize = 13;
            message.resizeTextMaxSize = 17;
            BroadcastUi.Place(message.rectTransform, 0.06f, 0.38f, 0.94f, 0.92f);
            var loadLabel = LocalizationManager.Current == GameLanguage.English ? "OPEN SAVED COPY" : "ABRIR CÓPIA";
            var yes = BroadcastUi.Button(card.transform, "Load", loadLabel, ConfirmLoadNamedCareer, UiTheme.Accent, UiTheme.ButtonText, 14);
            BroadcastUi.Place(yes.GetComponent<RectTransform>(), 0.06f, 0.08f, 0.47f, 0.31f);
            var no = BroadcastUi.Button(card.transform, "Cancel", "CANCELAR", () => Destroy(_confirm), new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 15);
            BroadcastUi.Place(no.GetComponent<RectTransform>(), 0.53f, 0.08f, 0.94f, 0.31f);
        }

        void ConfirmLoadNamedCareer()
        {
            var id = _pendingNamedSaveId;
            _pendingNamedSaveId = null;
            var session = GameRoot.Instance == null ? null : GameRoot.Instance.Session;
            var loaded = session == null ? null : CareerStore.TryLoadNamed(session, id);
            if (loaded == null)
            {
                if (_feedback != null)
                    _feedback.text = "Não foi possível abrir essa cópia. A carreira atual continua ativa.";
                if (_confirm != null)
                    Destroy(_confirm);
                return;
            }
            var saved = CareerStore.TrySave(loaded);
            RestoreUnappliedSettings();
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
                Debug.LogWarning("A cópia foi carregada, mas não foi possível atualizar a gravação principal. O backup anterior foi mantido.");
        }

        void LeaveToMenu()
        {
            CancelChanges();
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == SceneNames.MainMenu)
            {
                MainMenuController.LeaveGame();
                return;
            }
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
            var dim = BroadcastUi.Solid(rect, "Dim", new Color(0f, 0f, 0f, 0.78f), true);
            BroadcastUi.Stretch(dim.rectTransform);
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
            RestoreUnappliedSettings();
            GameRoot.Instance.LiveMatch = null;
            _root.SetActive(false);
            IsVisible = false;
            if (_confirm != null)
                Destroy(_confirm);
            GameRoot.Instance.Open(SceneNames.CareerHub);
        }

        void RestoreUnappliedSettings()
        {
            SettingsPrefs.Restore(_snapshot);
            GameRoot.ApplyUiScale();
            MatchDayController.RefreshSettings();
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
}
