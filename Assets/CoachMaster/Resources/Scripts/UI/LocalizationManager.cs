using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    public enum GameLanguage
    {
        English = 0,
        Portuguese = 1
    }

    public static class LocalizationManager
    {
        const string LanguageKey = "cm_language";
        const string PromptKey = "cm_language_prompt_seen";
        static readonly Dictionary<string, string> English = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "DEFINIÇÕES", "SETTINGS" }, { "APLICAR", "APPLY" }, { "CANCELAR", "CANCEL" }, { "REPOR PADRÃO", "RESTORE DEFAULTS" },
            { "GUARDAR CARREIRA", "SAVE CAREER" }, { "MENU", "MENU" }, { "ABRIR", "OPEN" }, { "VOLTAR", "BACK" }, { "SEGUINTE", "NEXT" },
            { "ANTERIOR", "PREVIOUS" }, { "GUARDAR", "SAVE" }, { "SAIR", "EXIT" }, { "SIM", "YES" }, { "NÃO", "NO" },
            { "RESTAURAR", "RESTORE" }, { "ABRIR CÓPIA", "OPEN SAVED COPY" },
            { "GESTOR DE GRAVAÇÕES", "SAVE MANAGER" }, { "CRIAR CÓPIA", "CREATE COPY" }, { "GRAVAÇÕES GUARDADAS", "SAVED CAREERS" },
            { "As cópias nomeadas ficam separadas dos autosaves e da gravação principal.", "Named copies stay separate from autosaves and the main career save." },
            { "A gravação anterior foi mantida.", "The previous save was kept." },
            { "ECRÃ E GRÁFICOS", "DISPLAY & GRAPHICS" }, { "JOGABILIDADE", "GAMEPLAY" }, { "DIA DE JOGO", "MATCH DAY" },
            { "NOTIFICAÇÕES", "NOTIFICATIONS" }, { "SOM", "SOUND" }, { "REALISMO", "REALISM" }, { "CARREIRA E SAVES", "CAREER & SAVES" },
            { "INTERFACE E ACESSIBILIDADE", "INTERFACE & ACCESSIBILITY" }, { "AVANÇADAS", "ADVANCED" },
            { "Ecrã e gráficos", "Display & graphics" }, { "Jogabilidade", "Gameplay" }, { "Dia de jogo", "Match day" },
            { "Notificações", "Notifications" }, { "Som", "Sound" }, { "Som e música", "Sound & music" },
            { "Realismo", "Realism" }, { "Simulação e realismo", "Simulation & realism" }, { "Carreira e saves", "Career & saves" },
            { "Acessibilidade", "Accessibility" }, { "Avançadas", "Advanced" },
            { "SOM E MÚSICA", "SOUND & MUSIC" }, { "SIMULAÇÃO E REALISMO", "SIMULATION & REALISM" },
            { "Imagem, desempenho e escala dos menus.", "Image, performance and menu scaling." },
            { "Controlos que alteram a preparação e o ritmo da partida.", "Controls for match preparation and pace." },
            { "Pausas automáticas e informação visível durante o jogo.", "Automatic pauses and on-screen match information." },
            { "Avisos de carreira. Os acontecimentos continuam no histórico.", "Career alerts. Events remain in your history." },
            { "Volumes separados; usa esquerda/direita no comando para ajustar.", "Separate volume controls. Use left/right on your controller." },
            { "Ajustes da simulação guardados dentro da carreira.", "Simulation settings saved with this career." },
            { "Autosaves rotativos, gravação manual e recuperação.", "Rotating autosaves, manual saves and recovery." },
            { "Escala do interface, movimento e ajuda de comando.", "Interface scale, motion and controller help." },
            { "Presets que alteram apenas as opções de simulação suportadas.", "Presets only change simulation options supported by the game." },
            { "Modo de ecrã", "Display mode" }, { "Resolução", "Resolution" }, { "Sincronização vertical", "Vertical sync" },
            { "Limite de fotogramas", "Frame rate limit" }, { "Qualidade", "Quality" }, { "Escala do interface", "Interface scale" },
            { "Janela, janela sem moldura ou ecrã inteiro exclusivo.", "Choose windowed, borderless or exclusive fullscreen mode." },
            { "A resolução escolhida é usada no modo de janela.", "The selected resolution is used in windowed mode." },
            { "Usa os níveis de qualidade disponíveis neste projeto Unity.", "Uses the quality levels available in this project." },
            { "Ajusta todos os ecrãs sem alterar a resolução do jogo.", "Scales every screen without changing the game resolution." },
            { "Imagem de fundo", "Background image" }, { "Automático acompanha a liga da carreira.", "Automatic follows the career league." },
            { "Escolha automática do onze", "Automatic starting lineup" },
            { "Personalizada", "Custom" },
            { "Ignora o onze guardado e escolhe a equipa a partir da formação e posições.", "Ignores the saved lineup and selects players by formation and position." },
            { "Treino automático", "Automatic training" },
            { "Quando desligado, avançar dias não altera a avaliação de treino dos jogadores.", "When disabled, advancing days does not change players' training ratings." },
            { "Velocidade inicial do jogo", "Initial match speed" }, { "Também pode ser alterada durante a partida.", "Can also be changed during a match." },
            { "Momentos apresentados", "Highlights shown" },
            { "Define quantos anúncios de jogo interrompem brevemente a simulação.", "Sets how many match announcements briefly interrupt the simulation." },
            { "O ritmo e os momentos escolhidos aplicam-se à apresentação; não alteram o resultado matemático da partida.", "Match speed and selected highlights affect presentation only; they do not change the simulated result." },
            { "Pausar depois de um golo", "Pause after a goal" },
            { "A pausa começa depois de a bola entrar e o golo ser confirmado.", "The pause starts once the ball is in and the goal is confirmed." },
            { "Pausar depois de um vermelho", "Pause after a red card" },
            { "Abre a pausa para poderes rever a equipa e as táticas.", "Opens the pause menu so you can review the team and tactics." },
            { "Pausar depois de uma lesão", "Pause after an injury" },
            { "Abre a pausa quando uma lesão é confirmada na partida.", "Opens the pause menu when an injury is confirmed during a match." },
            { "Pontuações ao vivo", "Live ratings" }, { "Mostra ou oculta a página de classificações dos jogadores.", "Shows or hides the player ratings page." },
            { "O jogo já para sempre no intervalo para a conversa e escolhas do balneário.", "The match pauses at half-time for team talks and dressing-room decisions." },
            { "Avisos de consequências", "Consequence alerts" },
            { "Ocultar o aviso não apaga a decisão do histórico da carreira.", "Hiding an alert does not remove the decision from career history." },
            { "Duração do aviso", "Alert duration" }, { "Controla quanto tempo fica visível a mensagem de consequência.", "Sets how long consequence messages remain visible." },
            { "A versão atual tem avisos de carreira e acontecimentos importantes. Não mostra categorias sem eventos próprios implementados.", "Career alerts and important events appear here. Categories without their own events are hidden." },
            { "Repetição ligada", "Repeat on" }, { "REPETIÇÃO LIGADA", "REPEAT ON" },
            { "Volume geral", "Master volume" }, { "Nível comum a todos os sons.", "Shared level for all audio." },
            { "Música dos menus.", "Menu music." }, { "Apito, bola, cartões e outros efeitos da partida.", "Whistles, the ball, cards and other match effects." },
            { "Ambiente e reação das bancadas.", "Stadium ambience and crowd reactions." }, { "Cliques e sons dos menus.", "Menu clicks and sounds." },
            { "Em segundo plano", "In background" }, { "Silenciar tudo", "Mute all" },
            { "Predefinição", "Preset" }, { "Casual e Hardcore ajustam em conjunto lesões e rigor disciplinar.", "Casual and Hardcore adjust injuries and disciplinary strictness together." },
            { "Frequência de lesões", "Injury frequency" }, { "Afeta a probabilidade de lesão em colisões, considerando fadiga e traits.", "Affects injury risk in collisions, taking fatigue and traits into account." },
            { "Rigor disciplinar", "Disciplinary strictness" }, { "Afeta a frequência de amarelos depois de faltas.", "Affects how often yellow cards are given after fouls." },
            { "Estas opções ficam guardadas na carreira e aplicam-se a partidas novas. Só alteram os sistemas já suportados.", "These options are saved with the career and apply to new matches. Only supported systems are affected." },
            { "Autosave automático", "Automatic autosave" },
            { "Cria uma recuperação rotativa quando a carreira avança o número de dias escolhido.", "Creates a rotating recovery copy after the selected number of career days." },
            { "Guardar antes dos jogos", "Save before matches" },
            { "Cria uma cópia rotativa antes de abrir uma partida, além do autosave por dias.", "Creates a rotating copy before a match, in addition to day-based autosaves." },
            { "Frequência", "Frequency" }, { "Intervalo medido em dias da carreira.", "Interval measured in career days." },
            { "Cópias rotativas", "Rotating copies" }, { "Número de recuperações mantidas à medida que os autosaves avançam.", "Number of recovery copies kept as autosaves rotate." },
            { "Cópia a restaurar", "Copy to restore" }, { "Escolhe entre as cópias rotativas disponíveis.", "Choose from the available rotating copies." },
            { "Restaurar cópia selecionada", "Restore selected copy" },
            { "Substitui a carreira ativa pelo ponto de recuperação selecionado.", "Replaces the active career with the selected recovery point." },
            { "Gestor de gravações", "Save manager" }, { "Cria uma cópia com nome e permite abrir gravações anteriores.", "Creates a named copy and lets you open previous saves." },
            { "Nome desta gravação", "Name this save" }, { "CÓPIA", "SAVE" }, { "VAZIA", "EMPTY" }, { "DISPONÍVEL", "AVAILABLE" },
            { "REPETIR", "REPEAT" },
            { "A cópia foi carregada, mas não foi possível atualizar a gravação principal. O backup anterior foi mantido.", "The save was loaded, but the main career could not be updated. The previous backup was kept." },
            { "Cópia guardada: ", "Saved copy: " }, { "CÓPIA GUARDADA:", "SAVED COPY:" },
            { "Atualiza apenas as opções de simulação que já têm efeito no jogo.", "Updates only simulation options that already affect the game." },
            { "O jogo ainda não tem sistemas de transferências automáticas de clubes, despedimentos de treinadores, conferências de imprensa ou mods. Não aparecem aqui controlos que não façam nada.", "Automatic club transfers, manager dismissals, press conferences and mods are not implemented. This page only shows working settings." },
            { "Escolhe o idioma do jogo.", "Choose the game language." },
            { "As alterações seguras aplicam-se de imediato. Carrega em Aplicar para as guardar.", "Safe changes take effect immediately. Select Apply to save them." },
            { "Definições aplicadas, mas não foi possível guardar a carreira. A gravação anterior foi mantida.", "Settings applied, but the career could not be saved. The previous save was kept." },
            { "Valores padrão carregados. Carrega em Aplicar para os guardar ou Cancelar para repor os anteriores.", "Default values loaded. Select Apply to save them or Cancel to restore the previous values." },
            { "Definições guardadas.", "Settings saved." }, { "Carreira guardada.", "Career saved." },
            { "Não foi possível guardar. A gravação anterior foi mantida.", "Could not save. The previous save was kept." },
            { "Já respondeste a esta mensagem.", "You've already replied to this message." },
            { "Recusaste a proposta. As outras continuam disponíveis.", "You rejected the offer. The other offers are still available." },
            { "A prioridade é recuperares sem pressa.", "Your recovery comes first. Take the time you need." },
            { "O fisioterapeuta vai manter-me informado.", "The physio will keep me updated." },
            { "Falamos quando estiveres perto do regresso.", "We'll talk when you're closer to returning." },
            { "Vamos ajustar a tua carga com o preparador.", "We'll adjust your workload with the fitness coach." },
            { "Hoje recuperas; depois reavaliamos.", "Take today to recover; we'll reassess afterwards." },
            { "Avisa-me se o cansaço piorar.", "Tell me if the fatigue gets worse." },
            { "Vou voltar a avaliar-te no treino.", "I'll reassess you in training." },
            { "Mantém o foco; a oportunidade pode surgir.", "Stay focused; your chance may come." },
            { "Vamos rever o teu papel com o adjunto.", "We'll review your role with the assistant." },
            { "Combinei que a recuperação viria antes do regresso.", "I agreed that recovery comes before a return." },
            { "Pedi ao fisioterapeuta uma atualização regular.", "I asked the physio for regular updates." },
            { "Marquei uma conversa para quando estiver mais perto do regresso.", "I scheduled a conversation for when the return is closer." },
            { "Pedi ao preparador para rever a carga de treino.", "I asked the fitness coach to review the training load." },
            { "Combinei reavaliar o cansaço depois da recuperação.", "I agreed to reassess the fatigue after recovery." },
            { "Pedi-lhe que me avisasse se o cansaço aumentasse.", "I asked them to tell me if the fatigue got worse." },
            { "Disse-lhe que voltaria a avaliar a sua forma no treino.", "I said I'd reassess their form in training." },
            { "Pedi-lhe que se mantivesse preparado para uma oportunidade.", "I asked them to stay ready for an opportunity." },
            { "Combinei rever a sua função com o adjunto.", "I agreed to review their role with the assistant." },
            { "Pedi-lhe que me ajudasse a perceber o ambiente no balneário.", "I asked them to help me understand the dressing-room mood." },
            { "Combinei que falaria com o grupo antes do próximo treino.", "I agreed to speak to the squad before the next training session." },
            { "Combinei alinhar o plano com o jogador e explicá-lo ao grupo.", "I agreed to align the plan with the player and explain it to the squad." },
            { "Disse-lhe para se preparar para o próximo jogo.", "I told them to get ready for the next match." },
            { "Combinei rever a sua função no treino.", "I agreed to review their role in training." },
            { "Pedi ao adjunto que lhe explicasse o plano.", "I asked the assistant to explain the plan to them." },
            { "Marquei uma conversa para perceber o que está a afetar a confiança do jogador.", "I scheduled a conversation to understand what's affecting the player's confidence." },
            { "Pedi ao adjunto uma avaliação do papel e do momento do jogador.", "I asked the assistant to assess the player's role and current form." },
            { "Expliquei que a resposta em campo também conta para recuperar espaço no grupo.", "I explained that performances also matter when earning a place back in the squad." },
            { "Marquei uma conversa para ouvir o jogador.", "I scheduled a conversation to hear the player out." },
            { "Expliquei que iria clarificar o seu papel.", "I explained that I'd clarify their role." },
            { "Reforcei que as oportunidades dependem do trabalho no treino.", "I stressed that opportunities depend on work in training." },
            { "Ajuda-me a perceber o que está a inquietar o grupo.", "Help me understand what's troubling the squad." },
            { "Fala com o grupo antes do próximo treino.", "Speak to the squad before the next training session." },
            { "Vamos explicar o plano juntos à equipa.", "We'll explain the plan to the team together." },
            { "Prepara-te; posso precisar de ti.", "Be ready; I may need you." },
            { "Revemos a tua função no treino.", "We'll review your role in training." },
            { "O adjunto explica-te o plano.", "The assistant will explain the plan to you." },
            { "Vamos conversar e esclarecer o teu papel.", "Let's talk and clear up your role." },
            { "Vou ouvir também a perspetiva do adjunto.", "I'll also hear the assistant's view." },
            { "Mostra no treino que estás preparado para ajudar.", "Show in training that you're ready to help." },
            { "Vou ouvir-te e falamos amanhã.", "I'll hear you out, and we'll talk tomorrow." },
            { "Explico-te melhor o teu papel na equipa.", "I'll explain your role in the team more clearly." },
            { "A oportunidade depende do treino.", "Your chance depends on training." },
            { "Esta proposta já não está disponível.", "This offer is no longer available." },
            { "ACEITAR  ·  assumir o clube", "ACCEPT  ·  take charge" }, { "RECUSAR proposta", "REJECT offer" },
            { "Recusei o empréstimo nesta fase.", "I rejected the loan at this stage." },
            { "Disse que ouviria uma proposta com garantias de utilização.", "I said I'd consider an offer with guaranteed playing time." },
            { "Recusei a abordagem e mantive o jogador no plantel.", "I rejected the approach and kept the player in the squad." },
            { "Pedi uma proposta formal pelo jogador.", "I asked for a formal offer for the player." },
            { "Fechei as conversas de transferência por agora.", "I ended transfer discussions for now." },
            { "Agradeci a mensagem e disse que iria analisar o assunto.", "I thanked them for the message and said I'd consider it." },
            { "Encaminhei o contacto para o adjunto avaliar.", "I asked my assistant to review the enquiry." },
            { "Adiei a conversa para outro momento.", "I postponed the conversation." },
            { "Respondi que nos encontraríamos no dia do jogo.", "I said we'd meet on matchday." },
            { "Disse que voltaríamos a falar depois do jogo.", "I said we'd speak again after the match." },
            { "Pedi os termos completos do empréstimo.", "I asked for the full loan terms." },
            { "Até lá. Vai ser um bom jogo.", "See you then. It should be a good match." },
            { "Vamos preparar a equipa para vos receber.", "We'll get the team ready to host you." },
            { "Vamos preparar a deslocação.", "We'll get ready for the trip." },
            { "Falamos depois do apito final.", "We'll talk after the final whistle." },
            { "Enviem a duração e as condições por escrito.", "Send the duration and terms in writing." },
            { "Nesta fase, queremos mantê-lo no plantel.", "At this stage, we want to keep him in the squad." },
            { "Podemos ouvir uma proposta com minutos garantidos.", "We'd consider an offer that guarantees playing time." },
            { "O jogador não está disponível para transferência.", "The player is not available for transfer." },
            { "Enviem uma proposta formal para avaliarmos.", "Send a formal offer for us to consider." },
            { "Neste momento não queremos negociar.", "We don't want to negotiate at this time." },
            { "Obrigado pela mensagem. Vou analisar o assunto.", "Thanks for the message. I'll consider it." },
            { "Falem primeiro com o meu adjunto.", "Speak to my assistant first." },
            { "Agora não é uma boa altura para conversarmos.", "This isn't a good time to talk." },
            { "O que ficou deste momento...", "What stayed with you from this moment?" },
            { "Procurar jogador pelo nome", "Search players by name" },
            { "MODO  JANELA", "WINDOWED MODE" }, { "MODO  SEM MOLDURA", "BORDERLESS MODE" }, { "MODO  ECRÃ INTEIRO", "FULLSCREEN MODE" },
            { "VSYNC LIGADO", "VSYNC ON" }, { "VSYNC DESLIGADO", "VSYNC OFF" }, { "FPS  SEM LIMITE", "FPS  UNLIMITED" },
            { "LIGADO", "ON" }, { "DESLIGADO", "OFF" },
            { "FUNDO  ·  AUTOMÁTICO", "BACKGROUND  ·  AUTOMATIC" }, { "FUNDO  ·  PORTUGAL", "BACKGROUND  ·  PORTUGAL" },
            { "FUNDO  ·  ESPANHA", "BACKGROUND  ·  SPAIN" }, { "FUNDO  ·  ESPANHA II", "BACKGROUND  ·  SPAIN II" },
            { "FUNDO  ·  INGLATERRA", "BACKGROUND  ·  ENGLAND" }, { "FUNDO  ·  CHAMPIONSHIP", "BACKGROUND  ·  CHAMPIONSHIP" },
            { "FUNDO  ·  LEAGUE TWO", "BACKGROUND  ·  LEAGUE TWO" },
            { "EQUILIBRADO", "BALANCED" }, { "REALISTA", "REALISTIC" }, { "PERSONALIZADO", "CUSTOM" },
            { "BAIXA", "LOW" }, { "ALTA", "HIGH" },
            { "Reduzir animações", "Reduce motion" }, { "Ajuda de teclado e comando", "Keyboard and controller hints" },
            { "Idioma", "Language" }, { "Inglês", "English" }, { "Português", "Portuguese" },
            { "Aumenta ou reduz todos os ecrãs. A resolução permanece igual.", "Scales every screen. Resolution remains unchanged." },
            { "Encurta os fades das mensagens e transições de carregamento.", "Shortens message fades and loading transitions." },
            { "Mostra os controlos de navegação no rodapé dos ecrãs.", "Shows navigation controls in screen footers." },
            { "A navegação mantém o destaque visual do botão ativo quando o comando está a ser usado.", "Navigation keeps the active button highlighted while using a controller." },
            { "Uma carreira nova começa pela liga. O clube, o plantel e o balneário aparecem antes de assinares.", "A new career starts with a league. Review the club, squad and dressing room before signing." },
            { "Escolhe um clube, assume o banco e constrói a época. O jogo mostra o que se vê do banco: conversas, escolhas e o resultado.", "Choose a club, take the dugout and build your season. Follow the conversations, decisions and results from the touchline." },
            { "ENTRAR NO BANCO", "TAKE THE DUGOUT" }, { "NOVA CARREIRA", "NEW CAREER" }, { "CONTINUAR", "CONTINUE" },
            { "SIMULADOR\nDE TREINADOR", "COACHING\nSIMULATOR" },
            { "M · DEFINIÇÕES", "M · SETTINGS" },
            { "Altera o idioma dos menus e das mensagens suportadas.", "Choose the game language." },
            { "Clica para entrar     Enter  avançar     Esc  voltar", "Click to start     Enter  continue     Esc  back" },
            { "Escolhe o idioma", "Choose your language" }, { "Podes alterar esta opção nas definições.", "You can change this later in Settings." },
            { "A tocar", "Now playing" }, { "A repetir", "Repeating" }, { "Todos", "All" }, { "Alargados", "Extended" },
            { "Chave", "Key moments" }, { "Só golos", "Goals only" }, { "Todos os dias", "Every day" },
            { "A cada 3 dias", "Every 3 days" }, { "A cada semana", "Every week" }, { "A cada 2 semanas", "Every 2 weeks" },
            { "Curta", "Short" }, { "Normal", "Normal" }, { "Longa", "Long" }, { "Música", "Music" }, { "Público", "Crowd" },
            { "Efeitos do jogo", "Match effects" }, { "Interface", "Interface" }, { "Jogador", "Player" }, { "Plantel", "Squad" },
            { "Tática", "Tactics" }, { "Calendário", "Calendar" }, { "Classificação", "Standings" }, { "Mercado", "Market" },
            { "Treino", "Training" }, { "Moral", "Morale" }, { "Forma", "Form" }, { "Idade", "Age" },
            { "Altura", "Height" }, { "Peso", "Weight" }, { "Pé preferido", "Preferred foot" }, { "Pé fraco", "Weak foot" },
            { "Lesão", "Injury" }, { "Lesionado", "Injured" }, { "Intervalo", "Half-time" }, { "Fim do jogo.", "Full-time." },
            { "Lesão muscular", "Muscle injury" }, { "Clube desconhecido", "Unknown club" },
            { "anos", "years" }, { "dias", "days" },
            { "Direito", "Right" }, { "Esquerdo", "Left" }, { "Ambos", "Both" }, { "Reservado", "Reserved" },
            { "Franco", "Outspoken" }, { "Líder", "Leader" }, { "Temperamental", "Temperamental" },
            { "Profissional", "Professional" }, { "Inseguro", "Insecure" }, { "Ambicioso", "Ambitious" },
            { "Treinador adjunto", "Assistant coach" }, { "Treinador de guarda-redes", "Goalkeeper coach" },
            { "Preparador físico", "Fitness coach" }, { "Fisioterapeuta", "Physiotherapist" }, { "Olheiro", "Scout" },
            { "Adjunto", "Assistant" }, { "Preparador", "Fitness coach" }, { "Imprensa", "Press" },
            { "PROPOSTA", "OFFER" }, { "JOGADOR", "PLAYER" }, { "CLUBE", "CLUB" }, { "STAFF", "STAFF" },
            { "JORNAL", "PRESS" }, { "AGENDA", "SCHEDULE" }, { "CASA", "HOME" }, { "FORA", "AWAY" },
            { "Rápido", "Quick" }, { "Remate colocado", "Finesse shot" }, { "Resistente à pressão", "Pressure resistant" },
            { "Driblador", "Dribbler" }, { "Finalizador", "Finisher" }, { "Visionário", "Visionary" },
            { "Marcador", "Marker" }, { "Trabalhador", "Hard worker" }, { "Frieza", "Composure" },
            { "Remate potente", "Power shot" }, { "Cruzador", "Crossing specialist" }, { "Jogo aéreo", "Aerial threat" },
            { "Remate rasteiro", "Low shot" }, { "Remate de longe", "Long shot" }, { "Remate de trivela", "Trivela shot" },
            { "Lançamento longo", "Long throw" }, { "Livres potentes", "Powerful free kicks" }, { "Efeito", "Flair" },
            { "Passe de primeira", "One-touch passing" }, { "Propenso a lesões", "Injury prone" },
            { "Resistente a lesões", "Injury resistant" }, { "Chilenas", "Bicycle kicks" }, { "Liderança", "Leadership" },
            { "Homem de um só clube", "One-club player" }, { "Jogador de equipa", "Team player" },
            { "Driblador técnico", "Technical dribbler" }, { "Driblador veloz", "Rapid dribbler" },
            { "Passador longo", "Long passer" }, { "Rematador de longe", "Long-range shooter" },
            { "Criador de jogo", "Playmaker" }, { "Entrada de carrinho", "Slide-tackle specialist" },
            { "Sai da baliza", "Sweeper keeper" }, { "Sai aos cruzamentos", "Claims crosses" },
            { "Defende com os pés", "Saves with feet" }, { "Equilibrado", "Balanced" },
            { "Sem traços especiais.", "No special traits." }, { "Potencial escondido.", "Potential is hidden." },
            { "Joga de forma equilibrada.", "Plays a balanced game." },
            { "Ataca o espaço em corrida.", "Attacks space with runs." },
            { "Procura remates colocados e pode dar-lhes efeito.", "Looks for finesse shots and can add curl." },
            { "Mantém a qualidade das decisões sob pressão.", "Keeps decision quality under pressure." },
            { "Conduz e tenta ultrapassar o adversário.", "Carries the ball and takes on opponents." },
            { "Ataca a área e aproveita ocasiões de golo.", "Attacks the box and takes goal-scoring chances." },
            { "Procura passes que ultrapassam linhas defensivas.", "Looks for passes that break defensive lines." },
            { "Fecha linhas e acompanha o portador da bola.", "Closes passing lanes and tracks the ball carrier." },
            { "Esforça-se mais na pressão e nas segundas bolas.", "Works harder to press and win second balls." },
            { "Decide com calma perto da baliza.", "Makes calm decisions near goal." },
            { "Dá mais força aos remates, com menor precisão.", "Adds power to shots at the cost of some accuracy." },
            { "Procura zonas de cruzamento e melhora a sua execução.", "Finds crossing positions and delivers more accurately." },
            { "Tem vantagem nas disputas aéreas.", "Has an advantage in aerial duels." },
            { "Procura remates baixos quando tem espaço.", "Looks for low shots when space opens up." },
            { "Arrisca remates de fora da área.", "Attempts shots from outside the box." },
            { "Tenta rematar com a parte exterior do pé em situações adequadas.", "Uses the outside of the foot to shoot when suitable." },
            { "Executa lançamentos laterais mais longos e procura colegas na área.", "Takes longer throw-ins and looks for teammates in the box." },
            { "Faz a preparação mais longa e bate livres diretos com força.", "Uses a longer run-up and hits direct free kicks with power." },
            { "Dá mais efeito a passes rasteiros e executa trivela com maior precisão.", "Adds curl to ground passes and strikes trivela shots more accurately." },
            { "Erra menos passes de primeira e combinações rápidas.", "Makes fewer errors with one-touch passes and quick combinations." },
            { "Arrisca fintas e passes criativos quando a situação o permite.", "Attempts skill moves and creative passes when the situation allows." },
            { "Tem maior risco de lesão em choques e disputas físicas.", "Has a higher injury risk in collisions and physical duels." },
            { "Tem menor risco de lesão em choques e disputas físicas.", "Has a lower injury risk in collisions and physical duels." },
            { "Tenta chilenas quando recebe uma bola aérea em boa posição.", "Attempts bicycle kicks when a high ball arrives in a good position." },
            { "É uma escolha forte para assumir a braçadeira.", "Is a strong candidate to wear the captain’s armband." },
            { "Valoriza a lealdade ao clube e é candidato à braçadeira.", "Values club loyalty and could become captain." },
            { "Privilegia o grupo e é candidato à braçadeira.", "Puts the team first and could become captain." },
            { "Tenta conduções longas e fintas técnicas quando há espaço.", "Uses longer carries and technical dribbles when space is available." },
            { "Usa a velocidade para conduzir e atacar espaço livre.", "Uses pace to carry the ball into open space." },
            { "Procura passes longos para progredir quando há um alvo livre.", "Looks for long passes to progress when a target is free." },
            { "Procura remates de longe, incluindo em bolas no ar.", "Looks for long-range shots, including volleys." },
            { "Os colegas procuram-no mais e ele decide a circulação mais depressa.", "Teammates look for them more, and they move the ball on sooner." },
            { "Recorre mais ao carrinho, com maior risco de falta.", "Uses slide tackles more often, with a higher foul risk." },
            { "Sai para cruzamentos apenas quando chega claramente primeiro.", "Comes for crosses when clearly first to the ball." },
            { "Ataca cruzamentos quando prevê chegar à bola por pouco antes do rival.", "Attacks crosses when likely to reach the ball just before an opponent." },
            { "Usa os pés com mais frequência em remates de curta distância.", "Uses their feet more often to stop close-range shots." },
            { "Escolhe uma resposta", "Choose a response" }, { "CONVERSA DE BALNEÁRIO", "DRESSING-ROOM TALK" },
            { "ESCOLHE UMA RESPOSTA", "CHOOSE A RESPONSE" }, { "Hora do jogo", "Matchday" }, { "HORA DO JOGO", "MATCHDAY" },
            { "TREINO", "TRAINING" }, { "TARDE NO CLUBE", "AFTERNOON AT THE CLUB" }, { "NO CLUBE", "AT THE CLUB" }, { "CHEGADA", "ARRIVAL" },
            { "VITÓRIA  ·  DEPOIS DO APITO", "WIN  ·  AFTER THE WHISTLE" },
            { "EMPATE  ·  DEPOIS DO APITO", "DRAW  ·  AFTER THE WHISTLE" },
            { "DERROTA  ·  DEPOIS DO APITO", "DEFEAT  ·  AFTER THE WHISTLE" },
            { "Sai. O preparador vê-te agora e o último exercício fica feito sem ti.", "Step out. The fitness coach will check you now; you can sit out the final drill." },
            { "Faz mais um sprint. Se for teatro, nota-se. Se for verdade, paras a seguir.", "Do one more sprint. If you're acting, we'll know. If it's real, you stop straight after." },
            { "O grupo não pára por uma pessoa. Volta para a fila.", "The group doesn't stop for one person. Get back in line." },
            { "Chamo o capitão. Isto resolve-se aqui, à frente do grupo, e acaba hoje.", "I'll bring in the captain. We settle this here, in front of the squad, and it's over today." },
            { "O exercício muda. A discussão fica para depois, se ainda existir.", "We'll change the drill. The argument can wait, if it still matters afterwards." },
            { "Contas para o próximo jogo. O lugar é teu, se o corpo responder.", "You're in contention for the next match. The place is yours if your body holds up." },
            { "A decisão vê-se no treino, não neste corredor. Mostra-me hoje.", "Your place is decided in training, not in this corridor. Show me today." },
            { "Entra. Não tenho resposta para ti agora.", "Go inside. I don't have an answer for you right now." },
            { "Paro o treino. O fisioterapeuta vê-te já.", "I'm stopping training. The physio will see you now." },
            { "Acaba este exercício e falamos a seguir.", "Finish this drill and we'll talk afterwards." },
            { "É dor de esforço. Continua e diz-me se piorar.", "It's just strain. Carry on and tell me if it gets worse." },
            { "Cumpre a instrução que já está no quadro. É essa que o grupo treinou.", "Stick to the instruction on the board. That's what the squad has practised." },
            { "Ajusta a instrução. Quero outro movimento deste lado.", "Adjust the instruction. I want a different movement on this side." },
            { "Repete o mesmo lance. A instrução não muda hoje.", "Repeat the same move. The instruction stays as it is today." },
            { "Separas os dois e falas com cada um, longe do grupo.", "Separate them and speak to each one away from the squad." },
            { "Resolvam isso entre vocês. Eu tenho um treino para orientar.", "Sort this out between yourselves. I have a training session to run." },
            { "Os dois fazem uma série extra. A discussão acabou.", "You both do an extra set. The argument is over." },
            { "Contas. Quando apertar, a bola passa por ti.", "You can count on me. When it gets tough, the ball comes through you." },
            { "Conta quem estiver melhor às três da tarde. Isso inclui-te, se fores tu.", "I pick whoever is best at three o'clock. That includes you if it's you." },
            { "Não respondes. Entras para a sala e deixas a pergunta no corredor.", "You don't answer. You walk into the room and leave the question in the corridor." },
            { "BALNEÁRIO", "DRESSING ROOM" }, { "TÁTICAS", "TACTICS" }, { "SUBSTITUIÇÕES", "SUBSTITUTIONS" },
            { "SUPLENTES", "SUBSTITUTES" }, { "MOTIVAR", "MOTIVATE" }, { "EXIGIR", "DEMAND MORE" },
            { "ACALMAR", "REASSURE" }, { "REPREENDER", "CRITICISE" }, { "TÁTICA", "TACTICAL" },
            { "COMEÇAR 2.ª PARTE", "START SECOND HALF" }, { "DEIXAR AO ADJUNTO", "LET THE ASSISTANT DECIDE" },
            { "ÁS", "STAR" }, { "CAPITÃO", "CAPTAIN" }, { "PLANTEL", "SQUAD" },
            { "Ficar na área", "Stay in the box" }, { "Cortar para dentro ofensivo", "Attack inside" },
            { "Cortar para dentro equilibrado", "Balanced inside forward" }, { "Defensivo", "Defensive" },
            { "Só desmarcação", "Short runs only" }, { "FIM DE JOGO", "FULL-TIME" },
            { "GOLO!", "GOAL!" }, { "CARTÃO VERMELHO", "RED CARD" }, { "CARTÃO AMARELO", "YELLOW CARD" },
            { "Lançamento lateral.", "Throw-in." }, { "Pontapé de saída no meio-campo.", "Kick-off from the centre." },
            { "O árbitro dá início à partida.", "The referee starts the match." },
            { "? A carreira ativa será substituída pelo ponto guardado.", "? The active career will be replaced by the saved point." },
            { "À ESPERA", "WAITING" },
            { "Ainda não há cópias nomeadas.", "There are no named saves yet." },
            { "CALENDÁRIO  ·  nota do adjunto em cada jogo", "CALENDAR  ·  assistant's note for every match" },
            { "CLASSIFICAÇÕES", "STANDINGS" },
            { "Clica num titular. As instruções da posição aparecem aqui.", "Select a starter to see their positional instructions here." },
            { "Clica numa função e depois num titular.", "Select a role, then choose a starter." },
            { "COMEÇAR ÉPOCA", "START SEASON" },
            { "Como te vão tratar no balneário?", "How should the squad address you?" },
            { "Condição  ", "Fitness  " },
            { "EQUIPA TÉCNICA", "COACHING STAFF" },
            { "Escreve tu o resumo do que aconteceu. Isto não é o jornal.", "Write your own summary of what happened. This is not the news feed." },
            { "Esta cópia ainda não existe. Avança dias suficientes para preencher o espaço.", "This save does not exist yet. Advance enough career days to create it." },
            { "FORMAÇÃO", "FORMATION" },
            { "ISTO PODE TER CONSEQUÊNCIAS", "THIS ACTION HAS CONSEQUENCES" },
            { "J jogos    V vitórias    E empates    D derrotas    GM golos marcados    GS golos sofridos    Pts pontos", "P played    W wins    D draws    L losses    GF goals for    GA goals against    Pts points" },
            { "MEMÓRIAS", "MEMORIES" },
            { "Não foi possível abrir essa cópia. A carreira atual continua ativa.", "Could not open that save. The current career remains active." },
            { "Não foi possível abrir o autosave. A gravação principal continua intacta.", "Could not open the autosave. The main save remains intact." },
            { "Não foi possível criar a cópia nomeada. A gravação principal não foi alterada.", "Could not create the named save. The main save was not changed." },
            { "O cansaço é a leitura do preparador. Pode estar errado.", "Fatigue is the coach's estimate and may be inaccurate." },
            { "REUNIÃO COM O PRESIDENTE", "MEETING WITH THE CHAIRPERSON" },
            { "Enter  começar     Esc  voltar", "Enter  start     Esc  back" },
            { "Reduz rasgos de imagem; pode limitar os FPS à frequência do ecrã.", "Reduces screen tearing; may limit FPS to the display refresh rate." },
            { "RELATÓRIO DA SEMANA", "WEEKLY REPORT" },
            { "Sem substituições.", "No substitutions." },
            { "Substituição", "Substitution" },
            { "Táticas rápidas", "Quick tactics" },
            { "TÁTICAS RÁPIDAS", "QUICK TACTICS" },
            { "Pressão", "Pressing" }, { "Linha defensiva", "Defensive Line" },
            { "Construção", "Build-up" }, { "Laterais", "Full-backs" },
            { "Largura", "Width" }, { "Ritmo", "Tempo" },
            { "Risco no passe", "Passing Risk" }, { "Ideia de jogo", "Playing Style" },
            { "Quando a equipa sai à bola sem posse.", "How the team presses when it does not have the ball." },
            { "Altura do bloco sem bola.", "How high the team defends without the ball." },
            { "Como a bola sai da defesa.", "How the team builds play from the back." },
            { "O que os laterais fazem com a posse.", "How the full-backs behave in possession." },
            { "Onde a equipa ocupa o campo.", "How the team uses the width of the pitch." },
            { "Velocidade com que a posse avança.", "How quickly the team moves the ball forward." },
            { "Seguro fica curto. Arriscado parte linhas.", "Safe passing keeps it short; risky passing breaks lines." },
            { "Zonal guarda zonas. Ao homem segue o adversário.", "Zonal marking protects areas; man-marking follows opponents." },
            { "O que a equipa procura com a bola.", "What the team tries to do in possession." },
            { "A MINHA LISTA", "MY SHORTLIST" }, { "PLANTEL À VENDA", "SQUAD LISTED FOR TRANSFER" },
            { "LISTA DE EMPRÉSTIMOS", "LOAN LIST" }, { "PROCURAR NOUTROS CLUBES", "SEARCH OTHER CLUBS" },
            { "RESULTADOS", "RESULTS" },
            { "Ainda não marcaste jogadores de outros clubes.", "You have not shortlisted any players from other clubs yet." },
            { "Ainda não puseste ninguém do plantel à venda.", "You have not listed any players in your squad for transfer." },
            { "Ainda não puseste ninguém na lista de empréstimos.", "You have not listed any players for loan yet." },
            { "Escreve pelo menos duas letras do nome.", "Enter at least two letters of a player's name." },
            { "Nenhum jogador com esse nome.", "No players found with that name." },
            { "escolher", "choose" },
            { "Pressão muito alta", "Very High Press" }, { "Pressão média", "Medium Press" },
            { "Pressão baixa", "Low Press" }, { "Linha muito alta", "Very High Line" },
            { "Linha defensiva média", "Standard Defensive Line" }, { "Linha defensiva baixa", "Low Defensive Line" },
            { "Linha muito baixa", "Very Low Line" }, { "Largura estreita", "Narrow Width" },
            { "Largura normal", "Standard Width" }, { "Largura larga", "Wide Width" },
            { "Tiki-taka", "Tiki-taka" }, { "Posse", "Possession" },
            { "Cruzamentos", "Crosses" }, { "Jogo direto", "Direct Play" },
            { "Bolas longas", "Long Balls" }, { "Remates de longe", "Long-range Shots" },
            { "Ficar aberto", "Stay Wide" }, { "Ficar atrás", "Stay Back" },
            { "Marcação", "Marking" }, { "Narrow", "Narrow" },
            { "Equilíbrio", "Balanced" }, { "Jogo pelas alas", "Wing Play" },
            { "Pressão alta", "High Press" }, { "Contra-ataque", "Counter-Attack" },
            { "Posse e construção", "Possession and Build-up" },
            { "Jogo misto", "Mixed Play" },
            { "Construção curta", "Short Build-up" }, { "Construção mista", "Mixed Build-up" },
            { "Construção longa", "Long Build-up" },
            { "Linha defensiva alta", "High Defensive Line" },
            { "Linha alta", "High Line" }, { "Laterais sobem", "Full-backs Push Up" },
            { "Laterais ficam", "Full-backs Stay Back" }, { "Laterais equilibram", "Balanced Full-backs" },
            { "Ritmo rápido", "Fast Tempo" }, { "Ritmo normal", "Standard Tempo" }, { "Ritmo lento", "Slow Tempo" },
            { "Passe arriscado", "Risky Passing" }, { "Passe misto", "Mixed Passing" }, { "Passe seguro", "Safe Passing" },
            { "Marcação mista", "Mixed Marking" }, { "Marcação ao homem", "Man Marking" }, { "Marcação zonal", "Zonal Marking" },
            { "Abre o jogo nas alas. Os laterais sobem, a largura é grande e os cruzamentos procuram a área. A pressão e a linha ficam a meio.", "Uses the width of the pitch. Full-backs push forward and crosses target the box, with a balanced press and defensive line." },
            { "Pressão muito alta e linha muito alta. A construção é curta e o ritmo é rápido, para recuperar a bola ainda no meio-campo adversário.", "Uses a very high press and defensive line, with short build-up and a fast tempo to win the ball back in the opposition half." },
            { "Bloco baixo e pouca pressão. A saída é longa. Quando a bola é recuperada, a equipa ataca o espaço em contra-ataque.", "Sits deep and presses less. The team plays long when building and attacks the space on the counter after winning the ball." },
            { "Posse curta e segura, ritmo lento e laterais equilibrados. A equipa espera o buraco em vez de forçar o remate.", "Keeps possession with short, safe passes and a slow tempo. The team waits for an opening instead of forcing a shot." },
            { "Bloco a meio, pressão média e jogo misto. A equipa tanto constrói como procura a profundidade, sem cair num extremo.", "Uses a mid-block and medium press, mixing patient build-up with runs in behind without committing to either extreme." },
            { "Toca num jogador do onze. As instruções aparecem aqui.", "Select a starter to see their instructions here." },
            { "+ LESIONADO", "+ INJURED" },
            { "11 Inicial", "Starting XI" },
            { "30, 60, 120, 144, 165, 240 ou sem limite.", "30, 60, 120, 144, 165, 240 or unlimited." },
            { "Agora clica num jogador do 11 inicial.", "Now select a player in the starting XI." },
            { "ANALISAR O PLANTEL  ·  3 DIAS", "SQUAD ANALYSIS  ·  3 DAYS" },
            { "ASSINAR E ENTREGAR", "SIGN AND TAKE OVER" },
            { "ASSUMIR O BANCO", "TAKE THE DUGOUT" },
            { "CONVERSA DA BANCADA", "FAN CHAT" },
            { "DIA EM ABERTO", "DAY OFF" },
            { "Escolhe como falar com a equipa.", "Choose how to address the team." },
            { "Escreve o nome do treinador.", "Enter the manager's name." },
            { "Escreve pelo menos uma frase.", "Write at least one sentence." },
            { "ESTADO DO DIA", "TODAY'S STATUS" },
            { "GUARDAR NOTA", "SAVE NOTE" },
            { "JOGADOR DA PARTIDA", "PLAYER OF THE MATCH" },
            { "JOGAR", "PLAY" },
            { "ESCOLHER", "SELECT" }, { "Pausa", "Pause" }, { "Mensagens", "Messages" }, { "RESERVAS", "RESERVES" },
            { "JOGO EM PAUSA", "MATCH PAUSED" },
            { "JORNAIS DO DIA", "TODAY'S NEWS" },
            { "MARCAR VIAGEM", "SCHEDULE TRAVEL" },
            { "OVERALL ATUAL", "CURRENT OVERALL" },
            { "Potencial", "Potential" },
            { "Preset de realismo", "Realism preset" },
            { "QUADRO DO MISTER", "MANAGER'S NOTES" },
            { "Ritmo x1", "Speed x1" },
            { "Sem mensagens nesta app.", "No messages in this app." },
            { "Sem reservas.", "No reserves." },
            { "Sem suplentes.", "No substitutes." },
            { "SIMULAR RESTO DO JOGO", "SIMULATE REST OF MATCH" },
            { "VER CLUBE", "VIEW CLUB" },
            { "Escolha a liga", "Choose a league" }, { "LIGAS", "LEAGUES" }, { "CLUBES", "CLUBS" },
            { "1ª DIVISÃO", "1ST DIVISION" }, { "2ª DIVISÃO", "2ND DIVISION" },
            { "3ª DIVISÃO", "3RD DIVISION" }, { "4ª DIVISÃO", "4TH DIVISION" }, { "COMPETIÇÃO", "COMPETITION" },
            { "HORA", "TIME" }, { "Hora", "Time" }, { "DETALHES", "DETAILS" }, { "Detalhes", "Details" },
            { "ESTÁDIO", "STADIUM" }, { "HISTÓRIA", "HISTORY" }, { "REPUTAÇÃO", "REPUTATION" },
            { "FINANÇAS", "FINANCES" }, { "IDENTIDADE", "IDENTITY" }, { "CARGO VAGO", "MANAGER POSITION" },
            { "OBJECTIVOS", "OBJECTIVES" }, { "OBJETIVOS", "OBJECTIVES" },
            { "Classificações", "Standings" }, { "Melhores marcadores", "Top scorers" },
            { "Assistências", "Assists" }, { "Melhores médias", "Best ratings" },
            { "Clubes com mais golos", "Most goals scored" }, { "Clubes com menos golos sofridos", "Fewest goals conceded" },
            { "Mais vitórias", "Most wins" }, { "Melhor diferença de golos", "Best goal difference" },
            { "Ainda não há jogos fechados.", "No completed matches yet." },
            { "Ainda não há números nesta lista.", "There are no figures in this list yet." },
            { "A tabela completa e os números da época.", "The full table and season statistics." },
            { "JOGOS", "PLAYED" }, { "VITÓRIAS", "WINS" }, { "EMPATES", "DRAWS" }, { "DERROTAS", "LOSSES" },
            { "GOLOS MARCADOS", "GOALS FOR" }, { "GOLOS SOFRIDOS", "GOALS AGAINST" }, { "PONTOS", "POINTS" },
            { "Golos", "Goals" }, { "golos", "goals" }, { "sofridos", "conceded" }, { "vitórias", "wins" },
            { "Casa", "Home" }, { "Fora", "Away" },
            { "A análise fica por fazer.", "The opposition report has not been completed." },
            { "Não há dias livres antes do jogo. A análise fica por fazer.", "There are no free days before the match, so the report cannot be completed." },
            { "Marcar viagem", "Plan a team trip" }, { "Férias curtas", "Short break" }, { "Almoço de grupo", "Squad lunch" },
            { "Dia na cidade", "Day in town" }, { "Estágio tático", "Tactical camp" },
            { "O que diz o staff", "Staff briefing" },
            { "Conhece as pessoas que vão trabalhar contigo no clube.", "Meet the people who will work with you at the club." },
            { "Lê o que se passa com precisão.", "Reads the situation accurately." },
            { "Vê o que salta à vista e falha o resto.", "Spots the obvious, but misses the finer details." },
            { "A leitura chega atrasada e, por vezes, errada.", "The assessment can be late and sometimes wrong." },
            { "fala com convicção. Mesmo assim, não é infalível.", "speaks with confidence, but is not infallible." },
            { "acerta o essencial e perde o detalhe.", "gets the main points right, but misses details." },
            { "anda muitas vezes ao lado. Trata a leitura como um palpite.", "often gets it wrong. Treat the assessment as a guess." },
            { "Portugal", "Portugal" }, { "Inglaterra", "England" }, { "França", "France" }, { "Alemanha", "Germany" },
            { "Países Baixos", "the Netherlands" }, { "Brasil", "Brazil" }, { "Espanha", "Spain" },
            { "Itália", "Italy" }, { "Escócia", "Scotland" }, { "Suécia", "Sweden" }, { "Irlanda", "Ireland" },
            { "Coreia do Sul", "South Korea" }, { "Finanças", "Finances" },
            { "Início", "Home" }, { "Jornais", "News" }, { "Quadro", "Board" }, { "Equipa", "Team" }, { "Tabela", "Standings" },
            { "Marcadores", "Scorers" }, { "Médias", "Ratings" }, { "Clubes", "Clubs" },
            { "Resto da edição", "The rest of today's edition" }, { "JOGO", "MATCH" }, { "ENTREVISTA", "INTERVIEW" },
            { "MERCADO", "TRANSFER MARKET" },
            { "Taça de Portugal", "Portuguese Cup" }, { "Taça da Liga", "Portuguese League Cup" },
            { "Taça de Portugal (2ª)", "Portuguese Cup (2nd tier)" }, { "Taça da Liga (2ª)", "Portuguese League Cup (2nd tier)" },
            { "Copa del Rey", "Copa del Rey" }, { "Copa de la Liga", "League Cup" },
            { "Copa del Rey (2ª)", "Copa del Rey (second tier)" }, { "Copa de la Liga (2ª)", "League Cup (second tier)" },
            { "Brasileirão Série A", "Brazilian Série A" }, { "Liga dos Campeões", "Champions League" },
            { "EFL Cup", "EFL Cup" }, { "Coupe de France", "French Cup" }, { "Coupe de la Ligue", "French League Cup" },
            { "FA Cup (2ª)", "FA Cup (second tier)" }, { "EFL Cup (2ª)", "EFL Cup (second tier)" },
            { "Coupe de France (2ª)", "French Cup (second tier)" }, { "Coupe de la Ligue (2ª)", "French League Cup (second tier)" },
            { "A principal competição de Portugal. Dezoito clubes disputam o título e a permanência.", "Portugal's top competition. Eighteen clubs contest the title and fight to stay up." },
            { "A segunda liga de Portugal. Dezoito clubes lutam pela subida à Liga Portugal.", "Portugal's second tier. Eighteen clubs are chasing promotion to Liga Portugal." },
            { "Os melhores de cada primeira divisão. Não se escolhe clube aqui: a classificação aparece na carreira.", "The leading clubs from each top division. Choose a domestic league; the standings appear in your career." },
            { "VER FICHA COMPLETA", "VIEW FULL PROFILE" },
            { "X   PARA TI", "X   FOR YOU" }, { "Mister", "Manager" },
            { "Arrasta o suplente para o lugar. Restam ", "Drag a substitute into the slot. Remaining: " },
            { "A repetir  ·  ", "Repeating  ·  " },
            { "A tocar  ·  ", "Now playing  ·  " },
            { " clubes", " clubs" },
            { " clubes em Detalhes", " clubs in Details" },
            { " Cans. ", " Fatigue " },
            { "   ·   forma ", "   ·   form " },
            { "   ·   treino ", "   ·   training " },
            { "   ·   Mister ", "   ·   Manager " },
            { "SEMANA DE ", "WEEK OF " },
            { "CARREGAR", "LOADING" }, { "A carregar…", "Loading…" }
        };
        static readonly KeyValuePair<string, string>[] EnglishTerms = BuildEnglishTerms();

        public static event Action LanguageChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterInitialSceneLocalization()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyScene();
        }

        public static GameLanguage Current => (GameLanguage)Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, 0), 0, 1);
        public static bool HasChosenLanguage => PlayerPrefs.GetInt(PromptKey, 0) != 0;

        public static string Translate(string source)
        {
            if (string.IsNullOrEmpty(source) || Current != GameLanguage.English)
                return source;
            if (English.TryGetValue(source, out var exact))
                return exact;
            var trimmed = source.Trim();
            if (English.TryGetValue(trimmed, out exact))
                return source.StartsWith(" ", StringComparison.Ordinal) ? " " + exact : exact;
            exact = TranslateGenerated(source);
            if (exact != null)
                return TranslateDictionaryTerms(exact) ?? exact;
            exact = TranslateUiText(source);
            if (exact != null)
                return TranslateDictionaryTerms(exact) ?? exact;
            exact = TranslateMultiline(source);
            if (exact != null)
                return exact;
            exact = TranslateDictionaryTerms(source);
            if (exact != null)
                return exact;
            const string continuePrefix = "CONTINUAR  ·  ";
            if (source.StartsWith(continuePrefix, StringComparison.Ordinal))
                return "CONTINUE  ·  " + source.Substring(continuePrefix.Length);
            const string memoriesPrefix = "MEMÓRIAS  ·  ";
            if (source.StartsWith(memoriesPrefix, StringComparison.Ordinal))
                return "MEMORIES  ·  " + source.Substring(memoriesPrefix.Length);
            const string restorePromptPrefix = "Restaurar a cópia ";
            const string restorePromptSuffix = "? A carreira ativa será substituída pelo ponto guardado.";
            if (source.StartsWith(restorePromptPrefix, StringComparison.Ordinal) && source.EndsWith(restorePromptSuffix, StringComparison.Ordinal))
            {
                var slot = source.Substring(restorePromptPrefix.Length, source.Length - restorePromptPrefix.Length - restorePromptSuffix.Length);
                return "Restore save " + slot + "? The active career will be replaced by the saved point.";
            }
            const string savedCopyPrefix = "Cópia guardada: ";
            if (source.StartsWith(savedCopyPrefix, StringComparison.Ordinal))
                return "Saved copy: " + source.Substring(savedCopyPrefix.Length);
            const string restoreCopyPrefix = "Restaurar a cópia ";
            if (source.StartsWith(restoreCopyPrefix, StringComparison.Ordinal))
                return "Restore save " + source.Substring(restoreCopyPrefix.Length);
            const string nowPlayingPrefix = "A tocar  ·  ";
            if (source.StartsWith(nowPlayingPrefix, StringComparison.Ordinal))
                return "Now playing  ·  " + source.Substring(nowPlayingPrefix.Length);
            const string repeatingPrefix = "A repetir  ·  ";
            if (source.StartsWith(repeatingPrefix, StringComparison.Ordinal))
                return "Repeating  ·  " + source.Substring(repeatingPrefix.Length);
            const string weekPrefix = "SEMANA DE ";
            if (source.StartsWith(weekPrefix, StringComparison.Ordinal))
                return "WEEK OF " + source.Substring(weekPrefix.Length);
            const string nextMatchPrefix = "Próximo jogo    ";
            if (source.StartsWith(nextMatchPrefix, StringComparison.Ordinal))
                return "Next match    " + source.Substring(nextMatchPrefix.Length);
            const string fitnessPrefix = "Condição  ";
            if (source.StartsWith(fitnessPrefix, StringComparison.Ordinal))
                return "Fitness  " + source.Substring(fitnessPrefix.Length);
            return source;
        }

        static KeyValuePair<string, string>[] BuildEnglishTerms()
        {
            var terms = new List<KeyValuePair<string, string>>(English);
            terms.Sort((left, right) => right.Key.Length.CompareTo(left.Key.Length));
            return terms.ToArray();
        }

        static string TranslateDictionaryTerms(string source)
        {
            var translated = source;
            var changed = false;
            for (var i = 0; i < EnglishTerms.Length; i++)
                translated = ReplaceUiTerm(translated, EnglishTerms[i].Key, EnglishTerms[i].Value, ref changed);
            return string.Equals(translated, source, StringComparison.Ordinal) ? null : translated;
        }

        static string TranslateUiText(string source)
        {
            switch (source)
            {
                case "12 competições · 7 países · 244 clubes": return "12 competitions · 7 countries · 244 clubs";
                case "12 competições · 7 países · 244 clubes.": return "12 competitions · 7 countries · 244 clubs.";
                case "Novo treinador": return "New manager";
                case "O TEU NOME": return "YOUR NAME";
                case "Nome do treinador": return "Manager name";
                case "O clube, o plantel e o balneário aparecem antes de assinares.": return "Review the club, squad and dressing room before signing.";
                case "PONTUAÇÕES AO VIVO": return "LIVE RATINGS";
                case "ISTO VAI TER CONSEQUÊNCIAS": return "THIS WILL HAVE CONSEQUENCES";
                case "BANCO": return "BENCH";
                case "Banco": return "Bench";
                case "RETOMAR": return "CONTINUE";
                case "Retomar": return "Continue";
                case "HOUR": return "TIME";
                case "HORA": return "TIME";
                case "Hora": return "Time";
                case "ESTATÍSTICAS": return "STATISTICS";
                case "Estatísticas": return "Statistics";
                case "C. FÍSICA": return "FITNESS";
                case "Pontuações": return "Ratings";
                case "PERFIL": return "PROFILE";
                case "OVR ATUAL": return "CURRENT OVR";
                case "BASE": return "BASE";
                case "MÉDIA": return "AVERAGE";
                case "ESCOLHE UMA RESPOSTA": return "CHOOSE A RESPONSE";
                case "ESTATÍSTICAS DO JOGO": return "MATCH STATISTICS";
                case "FICHA TÉCNICA": return "PLAYER PROFILE";
                case "EQUIPA TÉCNICA": return "COACHING STAFF";
                case "Procurar jogador pelo nome": return "Search players by name";
                case "Na lista de empréstimos.": return "Listed for loan.";
                case "No plantel, à venda.": return "In the squad and available for transfer.";
                case "Na tua lista. Ainda sem relatório do olheiro.": return "On your shortlist. No scout report yet.";
                case "Ainda sem relatório.": return "No scouting report yet.";
                case "Relatório do olheiro em curso": return "Scouting report in progress";
                case "Sem relatório de observação": return "No scouting report";
                case "RETIRAR": return "REMOVE";
                case "TIRAR": return "REMOVE";
                case "LISTA": return "SHORTLIST";
                case "MERCADO": return "MARKET";
                case "TÁTICA": return "TACTICS";
                case "INSTRUÇÃO INDIVIDUAL": return "INDIVIDUAL INSTRUCTION";
                case "Instrução individual": return "Individual instruction";
                case "Escolhe a ordem individual para esta posição. Só aparecem opções que o adjunto considera válidas.": return "Choose an individual instruction for this position. Only options approved by the assistant are shown.";
                case "TREINADOR ADJUNTO": return "ASSISTANT COACH";
                case "FISIO": return "PHYSIO";
                case "Muito fraco": return "Very weak";
                case "Fraco": return "Weak";
                case "Razoável": return "Fair";
                case "Forte": return "Strong";
                case "Muito forte": return "Very strong";
                case "Pé esquerdo": return "Left foot";
                case "Pé direito": return "Right foot";
                case "Pé ambidestro": return "Both feet";
                case "Compete na divisão de": return "Competes in the " ;
                case "Golo": return "Goal";
                case "Canto.": return "Corner.";
                case "Lançamento.": return "Throw-in.";
                case "SEM EVENTOS": return "NO EVENTS";
                case "ÁREA": return "BOX";
                case "FORMAÇÃO": return "FORMATION";
                case "Técnica": return "Technical";
                case "Técnicos": return "Technical";
                case "Mentais": return "Mental";
                case "Físicos": return "Physical";
                case "CARREIRA": return "CAREER";
                case "ÉPOCA · TODAS AS COMPETIÇÕES": return "SEASON · ALL COMPETITIONS";
                case "Época actual · todas as competições": return "Current season · all competitions";
                case "TRAÇOS E PONTOS FORTES": return "TRAITS & STRENGTHS";
                case "Sem lesão reportada": return "No injury reported";
                case "Fisioterapia": return "Medical team";
                case "Sem registo": return "No record";
                case "DISCIPLINA": return "DISCIPLINE";
                case "FITNESS": return "FITNESS";
                case "PERSONALIDADE": return "PERSONALITY";
                case "REPUTAÇÃO": return "REPUTATION";
                case "Altura": return "Height";
                case "Peso": return "Weight";
                case "Idade": return "Age";
                case "Pé preferido": return "Preferred foot";
                case "Pé fraco": return "Weak foot";
                case "VISÃO GERAL": return "OVERVIEW";
                case "O clube": return "The club";
                case "Calendário": return "Calendar";
                case "O que diz o staff": return "Staff briefing";
                case "NOTÍCIAS DO CLUBE": return "CLUB NEWS";
                case "Novo treinador assume o cargo": return "New manager takes charge";
                case "O desafio da época": return "The season challenge";
                case "Objetivos da direção": return "Board objectives";
                case "A saída do antigo treinador": return "The previous manager's departure";
                case "PLANTEL · VISTA TÁTICA": return "SQUAD · TACTICAL VIEW";
                case "Cada camisola representa um jogador do plantel. C identifica o capitão.": return "Each shirt represents a squad player. C marks the captain.";
                case "Vou ajudar-te a conhecer o grupo e a preparar o trabalho da equipa.": return "I'll help you get to know the squad and prepare the team's work.";
                case "Vou acompanhar os guarda-redes e partilhar contigo o que observar no treino.": return "I'll work with the goalkeepers and share what I see in training.";
                case "Quero observar como o plantel responde ao trabalho antes de aumentar a carga.": return "I'll see how the squad responds before increasing the workload.";
                case "Se alguém se queixar ou parecer limitado, falo contigo antes de avançar.": return "If anyone has a problem or looks limited, I'll speak to you before we continue.";
                case "Estou aqui para ajudar a equipa técnica.": return "I'm here to support the coaching staff.";
                case "Esta ação pode ter consequências": return "This action may have consequences";
                case "Esta escolha pode ter consequências": return "This choice may have consequences";
                case "Equipa técnica": return "Coaching staff";
                case "Físico": return "Physical";
                case "Defesa": return "Defending";
                case "Ataque": return "Attacking";
                case "Compostura": return "Composure";
                case "Visão": return "Vision";
                case "Desarme": return "Tackling";
                case "Posicionamento": return "Positioning";
                case "Aceleração": return "Acceleration";
                case "Agilidade": return "Agility";
                case "Equilíbrio": return "Balance";
                case "Salto": return "Jumping";
                case "Agressividade": return "Aggression";
                case "Antecipação": return "Anticipation";
                case "Bravura": return "Bravery";
                case "Concentração": return "Concentration";
                case "Determinação": return "Determination";
                case "Liderança": return "Leadership";
                case "Desmarcação": return "Off the ball";
                case "Trabalho de equipa": return "Teamwork";
                case "Posse de bola (%)": return "Possession (%)";
                case "Remates": return "Shots";
                case "Oportunidades": return "Chances";
                case "Plano": return "Plan";
                case "Substituições restantes:": return "Substitutions remaining:";
                case "Próxima troca:": return "Next change:";
                case "Adversário": return "Opponent";
                case "Sem substituições.": return "No substitutions left.";
                case "Sem traços especiais.": return "No special traits.";
                case "Escolhe um clube, assume o banco e constrói a tua história.": return "Choose a club, take charge and build your story.";
                case "M · DEFINIÇÕES": return "M · SETTINGS";
            }

            const string clubPickSuffix = "  ·  escolhe o banco que vais assumir.";
            if (source.EndsWith(clubPickSuffix, StringComparison.OrdinalIgnoreCase))
                return source.Substring(0, source.Length - clubPickSuffix.Length) + "  ·  choose the club you want to manage.";
            const string takeOverPrefix = "Vais assumir o ";
            if (source.StartsWith(takeOverPrefix, StringComparison.OrdinalIgnoreCase))
                return "You are taking charge of " + source.Substring(takeOverPrefix.Length);

            var translated = source;
            var changed = false;
            translated = ReplaceUiTerm(translated, "TRAÇOS E PONTOS FORTES", "TRAITS & STRENGTHS", ref changed);
            translated = ReplaceUiTerm(translated, "VALOR DE MERCADO", "MARKET VALUE", ref changed);
            translated = ReplaceUiTerm(translated, "PÉ PREFERIDO", "PREFERRED FOOT", ref changed);
            translated = ReplaceUiTerm(translated, "PÉ FRACO", "WEAK FOOT", ref changed);
            translated = ReplaceUiTerm(translated, "PÉ", "FOOT", ref changed);
            translated = ReplaceUiTerm(translated, "ALTURA", "HEIGHT", ref changed);
            translated = ReplaceUiTerm(translated, "PESO", "WEIGHT", ref changed);
            translated = ReplaceUiTerm(translated, "IDADE", "AGE", ref changed);
            translated = ReplaceUiTerm(translated, "PERSONALIDADE", "PERSONALITY", ref changed);
            translated = ReplaceUiTerm(translated, "REPUTAÇÃO", "REPUTATION", ref changed);
            translated = ReplaceUiTerm(translated, "RELATÓRIO DO OLHEIRO", "SCOUT REPORT", ref changed);
            translated = ReplaceUiTerm(translated, "MUITO BAIXO", "VERY LOW", ref changed);
            translated = ReplaceUiTerm(translated, "EXCELENTE", "EXCELLENT", ref changed);
            translated = ReplaceUiTerm(translated, "ESTÁVEL", "STABLE", ref changed);
            translated = ReplaceUiTerm(translated, "BAIXO", "LOW", ref changed);
            translated = ReplaceUiTerm(translated, "BOM", "GOOD", ref changed);
            translated = ReplaceUiTerm(translated, "CONDIÇÃO", "FITNESS", ref changed);
            translated = ReplaceUiTerm(translated, "DISCIPLINA", "DISCIPLINE", ref changed);
            translated = ReplaceUiTerm(translated, "MORAL", "MORALE", ref changed);
            translated = ReplaceUiTerm(translated, "FORMA", "FORM", ref changed);
            translated = ReplaceUiTerm(translated, "TREINO", "TRAINING", ref changed);
            translated = ReplaceUiTerm(translated, "MÉDIA", "AVERAGE", ref changed);
            translated = ReplaceUiTerm(translated, "JOGOS", "APPEARANCES", ref changed);
            translated = ReplaceUiTerm(translated, "GOLOS", "GOALS", ref changed);
            translated = ReplaceUiTerm(translated, "ASSISTÊNCIAS", "ASSISTS", ref changed);
            translated = ReplaceUiTerm(translated, "ÉPOCA", "SEASON", ref changed);
            translated = ReplaceUiTerm(translated, "CARREIRA", "CAREER", ref changed);
            translated = ReplaceUiTerm(translated, "ATAQUE", "ATTACKING", ref changed);
            translated = ReplaceUiTerm(translated, "TÉCNICA", "TECHNICAL", ref changed);
            translated = ReplaceUiTerm(translated, "MENTAL", "MENTAL", ref changed);
            translated = ReplaceUiTerm(translated, "DEFESA", "DEFENDING", ref changed);
            translated = ReplaceUiTerm(translated, "FÍSICO", "PHYSICAL", ref changed);
            translated = ReplaceUiTerm(translated, "CRUZAMENTO", "CROSSING", ref changed);
            translated = ReplaceUiTerm(translated, "DRIBLE", "DRIBBLING", ref changed);
            translated = ReplaceUiTerm(translated, "FINALIZAÇÃO", "FINISHING", ref changed);
            translated = ReplaceUiTerm(translated, "PASSE", "PASSING", ref changed);
            translated = ReplaceUiTerm(translated, "DESARME", "TACKLING", ref changed);
            translated = ReplaceUiTerm(translated, "POSICIONAMENTO", "POSITIONING", ref changed);
            translated = ReplaceUiTerm(translated, "FRIEZA", "COMPOSURE", ref changed);
            translated = ReplaceUiTerm(translated, "DECISÃO", "DECISIONS", ref changed);
            translated = ReplaceUiTerm(translated, "ENTREGA", "WORK RATE", ref changed);
            translated = ReplaceUiTerm(translated, "VELOCIDADE", "PACE", ref changed);
            translated = ReplaceUiTerm(translated, "RESISTÊNCIA", "STAMINA", ref changed);
            translated = ReplaceUiTerm(translated, "FORÇA", "STRENGTH", ref changed);
            translated = ReplaceUiTerm(translated, "REFLEXOS", "REFLEXES", ref changed);
            translated = ReplaceUiTerm(translated, "MANEJO", "HANDLING", ref changed);
            translated = ReplaceUiTerm(translated, "ACELERAÇÃO", "ACCELERATION", ref changed);
            translated = ReplaceUiTerm(translated, "AGILIDADE", "AGILITY", ref changed);
            translated = ReplaceUiTerm(translated, "EQUILÍBRIO", "BALANCED", ref changed);
            translated = ReplaceUiTerm(translated, "SALTO", "JUMPING", ref changed);
            translated = ReplaceUiTerm(translated, "AGRESSIVIDADE", "AGGRESSION", ref changed);
            translated = ReplaceUiTerm(translated, "ANTECIPAÇÃO", "ANTICIPATION", ref changed);
            translated = ReplaceUiTerm(translated, "BRAVURA", "BRAVERY", ref changed);
            translated = ReplaceUiTerm(translated, "CONCENTRAÇÃO", "CONCENTRATION", ref changed);
            translated = ReplaceUiTerm(translated, "DETERMINAÇÃO", "DETERMINATION", ref changed);
            translated = ReplaceUiTerm(translated, "LIDERANÇA", "LEADERSHIP", ref changed);
            translated = ReplaceUiTerm(translated, "DESMARCAÇÃO", "OFF THE BALL", ref changed);
            translated = ReplaceUiTerm(translated, "TRABALHO DE EQUIPA", "TEAMWORK", ref changed);
            translated = ReplaceUiTerm(translated, "VISÃO", "VISION", ref changed);
            translated = ReplaceUiTerm(translated, "CANTOS", "CORNERS", ref changed);
            translated = ReplaceUiTerm(translated, "LIVRES DIRETOS", "FREE KICKS", ref changed);
            translated = ReplaceUiTerm(translated, "LANÇAMENTOS LONGOS", "LONG THROWS", ref changed);
            translated = ReplaceUiTerm(translated, "PENÁLTIS", "PENALTIES", ref changed);
            translated = ReplaceUiTerm(translated, "JOGO AÉREO", "AERIAL ABILITY", ref changed);
            translated = ReplaceUiTerm(translated, "ATENÇÃO", "NOTE", ref changed);
            translated = ReplaceUiTerm(translated, "FICHA", "PROFILE", ref changed);
            translated = ReplaceUiTerm(translated, "NOTA", "NOTE", ref changed);
            translated = ReplaceUiTerm(translated, "REPUTAÇÃO", "REPUTATION", ref changed);
            translated = ReplaceUiTerm(translated, "PÉ", "FOOT", ref changed);
            translated = ReplaceUiTerm(translated, "ESQUERDO", "LEFT", ref changed);
            translated = ReplaceUiTerm(translated, "DIREITO", "RIGHT", ref changed);
            translated = ReplaceUiTerm(translated, "AMBOS", "BOTH", ref changed);
            translated = ReplaceUiTerm(translated, "MUITO FRACO", "VERY WEAK", ref changed);
            translated = ReplaceUiTerm(translated, "MUITO FORTE", "VERY STRONG", ref changed);
            translated = ReplaceUiTerm(translated, "FRACO", "WEAK", ref changed);
            translated = ReplaceUiTerm(translated, "FORTE", "STRONG", ref changed);
            translated = ReplaceUiTerm(translated, "RAZOÁVEL", "FAIR", ref changed);
            translated = ReplaceUiTerm(translated, "FICHA TÉCNICA", "PLAYER PROFILE", ref changed);
            translated = ReplaceUiTerm(translated, "OVR ATUAL", "CURRENT OVR", ref changed);
            translated = ReplaceUiTerm(translated, "PERFIL", "PROFILE", ref changed);
            translated = ReplaceUiTerm(translated, "OVERALL", "OVERALL", ref changed);
            translated = ReplaceUiTerm(translated, "CAPITÃO", "CAPTAIN", ref changed);
            translated = ReplaceUiTerm(translated, "SUBCAPITÃO", "VICE-CAPTAIN", ref changed);
            translated = ReplaceUiTerm(translated, "POSIÇÕES", "POSITIONS", ref changed);
            translated = ReplaceUiTerm(translated, "ALTURA", "HEIGHT", ref changed);
            translated = ReplaceUiTerm(translated, "PESO", "WEIGHT", ref changed);
            translated = ReplaceUiTerm(translated, "GR", "GK", ref changed);
            translated = ReplaceUiTerm(translated, "LD", "RB", ref changed);
            translated = ReplaceUiTerm(translated, "DC", "CB", ref changed);
            translated = ReplaceUiTerm(translated, "LE", "LB", ref changed);
            translated = ReplaceUiTerm(translated, "MDC", "DM", ref changed);
            translated = ReplaceUiTerm(translated, "MC", "CM", ref changed);
            translated = ReplaceUiTerm(translated, "MO", "AM", ref changed);
            translated = ReplaceUiTerm(translated, "ED", "RW", ref changed);
            translated = ReplaceUiTerm(translated, "EE", "LW", ref changed);
            translated = ReplaceUiTerm(translated, "PL", "ST", ref changed);
            translated = ReplaceUiTerm(translated, "BANCO", "BENCH", ref changed);
            translated = ReplaceUiTerm(translated, "RETOMAR", "CONTINUE", ref changed);
            translated = ReplaceUiTerm(translated, "SUBSTITUIÇÕES", "SUBSTITUTIONS", ref changed);
            translated = ReplaceUiTerm(translated, "SUPLENTES", "SUBSTITUTES", ref changed);
            translated = ReplaceUiTerm(translated, "RESERVAS", "RESERVES", ref changed);
            translated = ReplaceUiTerm(translated, "TÁTICAS RÁPIDAS", "QUICK TACTICS", ref changed);
            translated = ReplaceUiTerm(translated, "TÁTICAS", "TACTICS", ref changed);
            translated = ReplaceUiTerm(translated, "FORMAÇÃO", "FORMATION", ref changed);
            translated = ReplaceUiTerm(translated, "ANÁLISE", "ANALYSIS", ref changed);
            translated = ReplaceUiTerm(translated, "OLHEIRO", "SCOUT", ref changed);
            translated = ReplaceUiTerm(translated, "LESÃO", "INJURY", ref changed);
            translated = ReplaceUiTerm(translated, "LESIONADO", "INJURED", ref changed);
            translated = ReplaceUiTerm(translated, "EQUILIBRAR", "BALANCED", ref changed);
            translated = ReplaceUiTerm(translated, "Equilibrar", "Balanced", ref changed);
            translated = ReplaceUiTerm(translated, "Ficar na linha", "Stay on line", ref changed);
            translated = ReplaceUiTerm(translated, "Sair nos cruzamentos", "Claim crosses", ref changed);
            translated = ReplaceUiTerm(translated, "Subir equilibrado", "Balanced overlap", ref changed);
            translated = ReplaceUiTerm(translated, "Subir ofensivo", "Attack-minded overlap", ref changed);
            translated = ReplaceUiTerm(translated, "Ficar atrás", "Stay back", ref changed);
            translated = ReplaceUiTerm(translated, "Defensivo", "Defensive", ref changed);
            translated = ReplaceUiTerm(translated, "Só desmarcação", "Make forward runs", ref changed);
            translated = ReplaceUiTerm(translated, "Marcar", "Mark", ref changed);
            translated = ReplaceUiTerm(translated, "Sair com a bola", "Bring the ball out", ref changed);
            translated = ReplaceUiTerm(translated, "Chegar à área equilibrado", "Balanced box runs", ref changed);
            translated = ReplaceUiTerm(translated, "Chegar à área ofensivo", "Attack the box", ref changed);
            translated = ReplaceUiTerm(translated, "Cortar para dentro equilibrado", "Balanced inside forward", ref changed);
            translated = ReplaceUiTerm(translated, "Cortar para dentro ofensivo", "Attack inside", ref changed);
            translated = ReplaceUiTerm(translated, "Ficar aberto equilibrado", "Balanced wide", ref changed);
            translated = ReplaceUiTerm(translated, "Ficar aberto defensivo", "Defensive wide", ref changed);
            translated = ReplaceUiTerm(translated, "Ficar na área", "Stay in the box", ref changed);
            translated = ReplaceUiTerm(translated, "Mover-se equilibrado", "Balanced movement", ref changed);
            translated = ReplaceUiTerm(translated, "Mover-se ofensivo", "Attack-minded movement", ref changed);
            translated = ReplaceUiTerm(translated, "TÁTICA", "TACTICS", ref changed);
            translated = ReplaceUiTerm(translated, "EQUILÍBRIO", "BALANCED", ref changed);
            translated = ReplaceUiTerm(translated, "JOGO PELAS ALAS", "WING PLAY", ref changed);
            translated = ReplaceUiTerm(translated, "CONTRA-ATAQUE", "COUNTER-ATTACK", ref changed);
            translated = ReplaceUiTerm(translated, "POSSE E CONSTRUÇÃO", "POSSESSION AND BUILD-UP", ref changed);
            translated = ReplaceUiTerm(translated, "PRESSÃO", "PRESSING", ref changed);
            translated = ReplaceUiTerm(translated, "LINHA DEFENSIVA", "DEFENSIVE LINE", ref changed);
            translated = ReplaceUiTerm(translated, "CONSTRUÇÃO", "BUILD-UP", ref changed);
            translated = ReplaceUiTerm(translated, "LATERAIS", "FULL-BACKS", ref changed);
            translated = ReplaceUiTerm(translated, "LARGURA", "WIDTH", ref changed);
            translated = ReplaceUiTerm(translated, "RITMO", "TEMPO", ref changed);
            translated = ReplaceUiTerm(translated, "RISCO NO PASSE", "PASSING RISK", ref changed);
            translated = ReplaceUiTerm(translated, "IDEIA DE JOGO", "PLAYING STYLE", ref changed);
            translated = ReplaceUiTerm(translated, "ZONAL", "ZONAL", ref changed);
            translated = ReplaceUiTerm(translated, "MISTA", "MIXED", ref changed);
            translated = ReplaceUiTerm(translated, "AO HOMEM", "MAN-TO-MAN", ref changed);
            translated = ReplaceUiTerm(translated, "ESTREITA", "NARROW", ref changed);
            translated = ReplaceUiTerm(translated, "LARGA", "WIDE", ref changed);
            translated = ReplaceUiTerm(translated, "MUITO ALTA", "VERY HIGH", ref changed);
            translated = ReplaceUiTerm(translated, "ALTA", "HIGH", ref changed);
            translated = ReplaceUiTerm(translated, "MÉDIA", "MEDIUM", ref changed);
            translated = ReplaceUiTerm(translated, "BAIXA", "LOW", ref changed);
            translated = ReplaceUiTerm(translated, "MUITO BAIXA", "VERY LOW", ref changed);
            translated = ReplaceUiTerm(translated, "CURTA", "SHORT", ref changed);
            translated = ReplaceUiTerm(translated, "LONGA", "LONG", ref changed);
            translated = ReplaceUiTerm(translated, "RÁPIDO", "FAST", ref changed);
            translated = ReplaceUiTerm(translated, "NORMAL", "STANDARD", ref changed);
            translated = ReplaceUiTerm(translated, "LENTO", "SLOW", ref changed);
            translated = ReplaceUiTerm(translated, "SEGURO", "SAFE", ref changed);
            translated = ReplaceUiTerm(translated, "ARRISCADO", "RISKY", ref changed);
            translated = ReplaceUiTerm(translated, "MISTO", "MIXED", ref changed);
            translated = ReplaceUiTerm(translated, "OFENSIVOS", "ATTACKING", ref changed);
            translated = ReplaceUiTerm(translated, "EQUILIBRADOS", "BALANCED", ref changed);
            translated = ReplaceUiTerm(translated, "DEFENSIVOS", "DEFENSIVE", ref changed);
            translated = ReplaceUiTerm(translated, "SOBEM", "PUSH UP", ref changed);
            translated = ReplaceUiTerm(translated, "FICAM", "STAY BACK", ref changed);
            translated = ReplaceUiTerm(translated, "ESCOLHER", "CHOOSE", ref changed);
            return changed ? translated : null;
        }

        static string TranslateMultiline(string source)
        {
            if (source.IndexOf('\n') < 0)
                return null;
            var lines = source.Split('\n');
            var changed = false;
            for (var i = 0; i < lines.Length; i++)
            {
                var hasCarriageReturn = lines[i].EndsWith("\r", StringComparison.Ordinal);
                var line = hasCarriageReturn ? lines[i].Substring(0, lines[i].Length - 1) : lines[i];
                var translated = Translate(line);
                if (translated == line)
                    continue;
                lines[i] = translated + (hasCarriageReturn ? "\r" : "");
                changed = true;
            }
            return changed ? string.Join("\n", lines) : null;
        }

        static string ReplaceUiTerm(string source, string term, string replacement, ref bool changed)
        {
            var start = 0;
            while (start < source.Length)
            {
                var index = source.IndexOf(term, start, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                    break;
                var end = index + term.Length;
                var beforeIsWord = index > 0 && (char.IsLetterOrDigit(source[index - 1]) || source[index - 1] == '_');
                var afterIsWord = end < source.Length && (char.IsLetterOrDigit(source[end]) || source[end] == '_');
                if (!beforeIsWord && !afterIsWord)
                {
                    var original = source.Substring(index, term.Length);
                    var value = original == original.ToUpperInvariant() ? replacement.ToUpperInvariant()
                        : char.IsUpper(original[0]) ? replacement : replacement.ToLowerInvariant();
                    source = source.Substring(0, index) + value + source.Substring(end);
                    changed = true;
                    start = index + value.Length;
                }
                else
                    start = end;
            }
            return source;
        }

        internal static string SourceForEnglish(string translated)
        {
            if (string.IsNullOrEmpty(translated))
                return translated;
            foreach (var pair in English)
                if (string.Equals(pair.Value, translated, StringComparison.Ordinal))
                    return pair.Key;
            return translated;
        }

        public static string FormatDate(DateTime date, string portugueseFormat)
        {
            if (Current == GameLanguage.English)
                return date.ToString(portugueseFormat.Replace(" 'de' ", " "), CultureInfo.GetCultureInfo("en-GB"));
            return date.ToString(portugueseFormat, CultureInfo.GetCultureInfo("pt-PT"));
        }

        static string TranslateGenerated(string source)
        {
            var stadiumPrefix = source.StartsWith("Estádio das ", StringComparison.Ordinal) ? "Estádio das "
                : source.StartsWith("Estádio dos ", StringComparison.Ordinal) ? "Estádio dos "
                : source.StartsWith("Estádio de ", StringComparison.Ordinal) ? "Estádio de "
                : source.StartsWith("Estádio do ", StringComparison.Ordinal) ? "Estádio do "
                : source.StartsWith("Estádio da ", StringComparison.Ordinal) ? "Estádio da "
                : source.StartsWith("Campo das ", StringComparison.Ordinal) ? "Campo das "
                : source.StartsWith("Campo dos ", StringComparison.Ordinal) ? "Campo dos "
                : source.StartsWith("Campo de ", StringComparison.Ordinal) ? "Campo de "
                : source.StartsWith("Campo do ", StringComparison.Ordinal) ? "Campo do "
                : source.StartsWith("Campo da ", StringComparison.Ordinal) ? "Campo da " : null;
            if (stadiumPrefix != null)
                return (stadiumPrefix.StartsWith("Campo", StringComparison.Ordinal) ? "Ground of " : "Stadium of ") + source.Substring(stadiumPrefix.Length);

            const string teamDivisionSuffix = " disputa a divisão de ";
            if (source.EndsWith(".", StringComparison.Ordinal))
            {
                var divisionAt = source.IndexOf(teamDivisionSuffix, StringComparison.Ordinal);
                if (divisionAt > 0)
                    return source.Substring(0, divisionAt) + " compete in " + TranslateCountry(source.Substring(divisionAt + teamDivisionSuffix.Length).TrimEnd('.')) + ".";
            }

            const string clubReputationPrefix = "Clube de ";
            const string clubReputationSuffix = " com reputação intermédia.";
            if (source.StartsWith(clubReputationPrefix, StringComparison.Ordinal) && source.EndsWith(clubReputationSuffix, StringComparison.Ordinal))
                return "Club from " + TranslateCountry(source.Substring(clubReputationPrefix.Length, source.Length - clubReputationPrefix.Length - clubReputationSuffix.Length)) + " with a mid-level reputation.";

            if (source == "Clube da primeira divisão.") return "Top-flight club.";
            if (source == "Clube a lutar pela subida.") return "Club fighting for promotion.";
            if (source == "A direção procura um treinador para liderar o projeto.") return "The board is looking for a manager to lead the project.";

            const string resolutionPrefix = "RESOLUÇÃO  ";
            if (source.StartsWith(resolutionPrefix, StringComparison.Ordinal))
                return "RESOLUTION  " + source.Substring(resolutionPrefix.Length);
            const string qualityPrefix = "QUALIDADE  ";
            if (source.StartsWith(qualityPrefix, StringComparison.Ordinal))
                return "QUALITY  " + source.Substring(qualityPrefix.Length);
            const string savePrefix = "CÓPIA ";
            if (source.StartsWith(savePrefix, StringComparison.Ordinal))
                return "SAVE " + source.Substring(savePrefix.Length)
                    .Replace(" · VAZIA", " · EMPTY")
                    .Replace(" · DISPONÍVEL", " · AVAILABLE")
                    .Replace(" · GRAVAÇÃO ANTIGA", " · OLD SAVE");
            if (source.StartsWith("Passe em profundidade de ", StringComparison.Ordinal))
                return ReplacePlayerPair(source, "Passe em profundidade de ", "Through ball from ");
            if (source.StartsWith("Passe criativo de ", StringComparison.Ordinal))
                return ReplacePlayerPair(source, "Passe criativo de ", "Creative pass from ");
            if (source.StartsWith("Passe de ", StringComparison.Ordinal))
            {
                const string deflectedSuffix = " sai desviado.";
                if (source.EndsWith(deflectedSuffix, StringComparison.Ordinal))
                    return "Pass from " + source.Substring("Passe de ".Length, source.Length - "Passe de ".Length - deflectedSuffix.Length) + " is deflected.";
                return ReplacePlayerPair(source, "Passe de ", "Pass from ");
            }
            if (source.StartsWith("GOLO! ", StringComparison.Ordinal))
                return "GOAL! " + TranslateGoalDescription(source.Substring(6));
            if (source.StartsWith("GOLO  ", StringComparison.Ordinal))
                return "GOAL  " + TranslateGoalDescription(source.Substring(6));
            if (source.StartsWith("Chilena de ", StringComparison.Ordinal)
                || source.StartsWith("Livre direto potente de ", StringComparison.Ordinal)
                || source.StartsWith("Remate de trivela de ", StringComparison.Ordinal)
                || source.StartsWith("Remate com flair de ", StringComparison.Ordinal)
                || source.StartsWith("Cabeceamento de ", StringComparison.Ordinal)
                || source.StartsWith("Remate rasteiro de ", StringComparison.Ordinal)
                || source.StartsWith("Remate de longe de ", StringComparison.Ordinal)
                || source.StartsWith("Remate potente de ", StringComparison.Ordinal)
                || source.StartsWith("Remate colocado de ", StringComparison.Ordinal)
                || source.StartsWith("Remate de ", StringComparison.Ordinal))
                return TranslateShotEvent(source);
            const string interceptionPrefix = "Interceção. ";
            const string interceptionSuffix = " corta o passe.";
            if (source.StartsWith(interceptionPrefix, StringComparison.Ordinal) && source.EndsWith(interceptionSuffix, StringComparison.Ordinal))
                return "Interception. " + source.Substring(interceptionPrefix.Length, source.Length - interceptionPrefix.Length - interceptionSuffix.Length) + " cuts out the pass.";
            const string secondYellowPrefix = "Segundo amarelo e expulsão para ";
            if (source.StartsWith(secondYellowPrefix, StringComparison.Ordinal))
                return "Second yellow and sent off: " + source.Substring(secondYellowPrefix.Length).TrimEnd('.') + ".";
            const string yellowPrefix = "Amarelo para ";
            if (source.StartsWith(yellowPrefix, StringComparison.Ordinal))
                return "Yellow card for " + source.Substring(yellowPrefix.Length).TrimEnd('.') + ".";
            const string redPrefix = "Vermelho para ";
            if (source.StartsWith(redPrefix, StringComparison.Ordinal))
                return "Red card for " + source.Substring(redPrefix.Length).TrimEnd('.') + ".";
            const string foulPrefix = "Falta de ";
            if (source.StartsWith(foulPrefix, StringComparison.Ordinal))
                return "Foul by " + source.Substring(foulPrefix.Length).TrimEnd('.') + ".";
            const string substitutionPrefix = "Substituição: sai ";
            const string substitutionSeparator = ", entra ";
            if (source.StartsWith(substitutionPrefix, StringComparison.Ordinal))
            {
                var split = source.IndexOf(substitutionSeparator, substitutionPrefix.Length, StringComparison.Ordinal);
                if (split > 0)
                    return "Substitution: " + source.Substring(substitutionPrefix.Length, split - substitutionPrefix.Length) + " off, "
                        + source.Substring(split + substitutionSeparator.Length).TrimEnd('.') + " on.";
            }
            const string cornerPrefix = "Canto. ";
            const string cornerSuffix = " vai bater o canto.";
            if (source.StartsWith(cornerPrefix, StringComparison.Ordinal) && source.EndsWith(cornerSuffix, StringComparison.Ordinal))
                return "Corner. " + source.Substring(cornerPrefix.Length, source.Length - cornerPrefix.Length - cornerSuffix.Length) + " will take it.";
            const string throwInPrefix = "Lançamento lateral. ";
            const string throwInSuffix = " vai buscar a bola.";
            if (source.StartsWith(throwInPrefix, StringComparison.Ordinal) && source.EndsWith(throwInSuffix, StringComparison.Ordinal))
                return "Throw-in. " + source.Substring(throwInPrefix.Length, source.Length - throwInPrefix.Length - throwInSuffix.Length) + " is retrieving the ball.";
            if (source == "Pontapé de baliza.") return "Goal kick.";
            if (source == "Pontapé de saída no meio-campo.") return "Kick-off from the centre.";
            if (source == "Segunda parte. O jogo recomeça no meio-campo.") return "Second half. Play restarts from the centre.";
            if (source == "Corte.") return "Clearance.";
            if (source == "Canto.") return "Corner.";
            if (source == "Lançamento.") return "Throw-in.";
            if (source == "Penálti.") return "Penalty.";
            if (source == "Fora de jogo.") return "Offside.";
            if (source.StartsWith("Fora de jogo de ", StringComparison.Ordinal))
                return "Offside: " + source.Substring("Fora de jogo de ".Length).TrimEnd('.') + ".";
            if (source.StartsWith("Penálti falhado por ", StringComparison.Ordinal))
                return "Penalty missed by " + source.Substring("Penálti falhado por ".Length).TrimEnd('.') + ".";
            if (source.StartsWith("Corte. ", StringComparison.Ordinal) && source.EndsWith(" recupera a bola.", StringComparison.Ordinal))
                return source.Substring("Corte. ".Length, source.Length - "Corte. ".Length - " recupera a bola.".Length) + " wins the ball back.";
            if (source.StartsWith("Lançamento longo de ", StringComparison.Ordinal))
                return ReplacePlayerPair(source, "Lançamento longo de ", "Long throw by ");
            if (source.StartsWith("Lançamento de ", StringComparison.Ordinal))
                return ReplacePlayerPair(source, "Lançamento de ", "Throw-in by ");
            if (source.StartsWith("Cruzamento de ", StringComparison.Ordinal))
                return TranslateCrossing(source, "Cruzamento de ", "Cross by ");
            if (source.StartsWith("Canto de ", StringComparison.Ordinal))
                return TranslateCrossing(source, "Canto de ", "Corner by ");
            if (source.EndsWith(" parte para o golo sozinho.", StringComparison.Ordinal))
                return source.Substring(0, source.Length - " parte para o golo sozinho.".Length) + " breaks through on goal alone.";
            if (source.EndsWith(" conduz para a área.", StringComparison.Ordinal))
                return source.Substring(0, source.Length - " conduz para a área.".Length) + " carries the ball into the box.";
            if (source.EndsWith(" conduz para a frente.", StringComparison.Ordinal))
                return source.Substring(0, source.Length - " conduz para a frente.".Length) + " carries the ball forward.";
            if (source.EndsWith(" corre a linha.", StringComparison.Ordinal))
                return source.Substring(0, source.Length - " corre a linha.".Length) + " runs down the line.";
            if (source.EndsWith(" não consegue continuar; a equipa tenta sair com a bola.", StringComparison.Ordinal))
                return source.Substring(0, source.Length - " não consegue continuar; a equipa tenta sair com a bola.".Length) + " cannot continue; the team tries to play out.";
            if (source.EndsWith(" recua para ganhar balanço no livre.", StringComparison.Ordinal))
                return source.Substring(0, source.Length - " recua para ganhar balanço no livre.".Length) + " takes a few steps back for the free kick.";
            if (source.StartsWith("Passe de ", StringComparison.Ordinal) && source.EndsWith(" sai desviado.", StringComparison.Ordinal))
                return "Pass from " + source.Substring("Passe de ".Length, source.Length - "Passe de ".Length - " sai desviado.".Length) + " is deflected.";
            if (source == "Intervalo. O plantel espera a tua conversa.") return "Half-time. The squad is waiting for your team talk.";
            if (source == "Fim do jogo.") return "Full-time.";
            if (source == "Dás confiança ao grupo e pedes que mantenha a coragem.") return "You give the squad confidence and ask them to stay brave.";
            if (source == "Exiges mais concentração e intensidade na segunda parte.") return "You demand more focus and intensity in the second half.";
            if (source == "Confrontas o que correu mal, sabendo que alguns podem reagir mal.") return "You address what went wrong, knowing some players may react badly.";
            if (source == "Explicas uma correção para a segunda parte com base no que aconteceu.") return "You explain a second-half adjustment based on what happened.";
            if (source == "Pedes calma e clareza para recuperar sem precipitação.") return "You ask for calm and clear heads as the team looks to recover.";
            if (source == "Pedes calma e controlo para gerir o jogo.") return "You ask for calm and control to manage the match.";
            const string squadPrefix = "PLANTEL  ·  ";
            if (source.StartsWith(squadPrefix, StringComparison.Ordinal)) return "SQUAD  ·  " + source.Substring(squadPrefix.Length);
            const string motmPrefix = "JOGADOR DA PARTIDA   ";
            if (source.StartsWith(motmPrefix, StringComparison.Ordinal)) return "PLAYER OF THE MATCH   " + source.Substring(motmPrefix.Length);
            const string playPrefix = "JOGAR    ";
            if (source.StartsWith(playPrefix, StringComparison.Ordinal)) return "PLAY    " + source.Substring(playPrefix.Length);
            const string moralePrefix = "Moral ";
            if (source.StartsWith(moralePrefix, StringComparison.Ordinal)) return "Morale " + source.Substring(moralePrefix.Length);
            const string potentialPrefix = "Potencial ";
            if (source.StartsWith(potentialPrefix, StringComparison.Ordinal)) return "Potential " + source.Substring(potentialPrefix.Length);
            const string speedPrefix = "Ritmo ";
            if (source.StartsWith(speedPrefix, StringComparison.Ordinal)) return "Speed " + source.Substring(speedPrefix.Length);
            const string dragSubPrefix = "Arrasta o suplente para o lugar. Restam ";
            if (source.StartsWith(dragSubPrefix, StringComparison.Ordinal)) return "Drag a substitute into the slot. Remaining: " + source.Substring(dragSubPrefix.Length);
            const string talkTitlePrefix = "CONVERSA · ";
            if (source.StartsWith(talkTitlePrefix, StringComparison.Ordinal)) return "TEAM TALK · " + source.Substring(talkTitlePrefix.Length);
            const string sideWithPlayerPrefix = "\"Fico do lado de ";
            const string sideWithPlayerSuffix = ". O outro cala-se e volta ao lugar.\"";
            if (source.StartsWith(sideWithPlayerPrefix, StringComparison.Ordinal) && source.EndsWith(sideWithPlayerSuffix, StringComparison.Ordinal))
            {
                var player = source.Substring(sideWithPlayerPrefix.Length, source.Length - sideWithPlayerPrefix.Length - sideWithPlayerSuffix.Length);
                return "\"I'll back " + player + ". The other player goes quiet and returns to their place.\"";
            }
            if (source.StartsWith("Ganharam. ", StringComparison.Ordinal))
                return TranslateAfterMatch(source, "Ganharam. ", ". O balneário fala baixo, mas há sorrisos contidos.", "Won. ", ". The dressing room is quiet, but a few smiles are showing.");
            if (source.StartsWith("Ganharam fora. ", StringComparison.Ordinal))
                return TranslateAfterMatch(source, "Ganharam fora. ", ". No autocarro ainda se fala do golo, desta vez a rir.", "Won away. ", ". The goal is still the topic on the bus, this time with laughter.");
            if (source.StartsWith("Empate. ", StringComparison.Ordinal))
                return TranslateAfterMatch(source, "Empate. ", ". Ninguém grita. Ninguém festeja.", "A draw. ", ". No one shouts. No one celebrates.");
            if (source.StartsWith("Empate fora. ", StringComparison.Ordinal))
                return TranslateAfterMatch(source, "Empate fora. ", ". A viagem de regresso é longa e quase muda.", "A draw away. ", ". The journey home is long and almost silent.");
            if (source.StartsWith("Perderam em casa. ", StringComparison.Ordinal))
                return TranslateAfterMatch(source, "Perderam em casa. ", ". Há toalhas no chão e ninguém se olha.", "Lost at home. ", ". Towels are on the floor and no one meets anyone's eyes.");
            if (source.StartsWith("Perderam fora. ", StringComparison.Ordinal))
                return TranslateAfterMatch(source, "Perderam fora. ", ". No autocarro, dois jogadores ainda discutem o golo sofrido.", "Lost away. ", ". Two players are still arguing about the goal on the bus.");
            var translatedComposite = false;
            if (source.Contains("   Cans. "))
            {
                source = source.Replace("   Cans. ", "   Fatigue ");
                translatedComposite = true;
            }
            if (source.Contains("   ·   forma "))
            {
                source = source.Replace("   ·   forma ", "   ·   form ");
                translatedComposite = true;
            }
            if (source.Contains("   ·   treino "))
            {
                source = source.Replace("   ·   treino ", "   ·   training ");
                translatedComposite = true;
            }
            if (source.Contains("   ·   Mister "))
            {
                source = source.Replace("   ·   Mister ", "   ·   Manager ");
                translatedComposite = true;
            }
            var tactics = source
                .Replace("Pressão alta", "High press")
                .Replace("Pressão média", "Medium press")
                .Replace("Pressão baixa", "Low press")
                .Replace("Construção curta", "Short build-up")
                .Replace("Construção mista", "Mixed build-up")
                .Replace("Construção longa", "Long build-up")
                .Replace("Laterais ofensivos", "Attacking full-backs")
                .Replace("Laterais equilibrados", "Balanced full-backs")
                .Replace("Laterais defensivos", "Defensive full-backs")
                .Replace("Alas ofensivos", "Attacking wing-backs")
                .Replace("Ala ofensivos", "Attacking wing-backs")
                .Replace("Linha defensiva alta", "High defensive line")
                .Replace("Linha alta", "High line")
                .Replace("Linha média", "Mid block")
                .Replace("Linha baixa", "Low block");
            if (!string.Equals(tactics, source, StringComparison.Ordinal))
            {
                source = tactics;
                translatedComposite = true;
            }
            if (source.EndsWith(" clubes em Detalhes", StringComparison.Ordinal)) return source.Substring(0, source.Length - " clubes em Detalhes".Length) + " clubs in Details";
            if (source.EndsWith(" clubes", StringComparison.Ordinal)) return source.Substring(0, source.Length - " clubes".Length) + " clubs";
            return translatedComposite ? source : null;
        }

        static string TranslateCountry(string source)
        {
            switch (source)
            {
                case "Inglaterra": return "England";
                case "França": return "France";
                case "Alemanha": return "Germany";
                case "Países Baixos": return "the Netherlands";
                case "Brasil": return "Brazil";
                case "Espanha": return "Spain";
                case "Portugal": return "Portugal";
                case "Itália": return "Italy";
                case "Escócia": return "Scotland";
                case "Suécia": return "Sweden";
                case "Irlanda": return "Ireland";
                case "Coreia do Sul": return "South Korea";
                default: return source;
            }
        }

        static string TranslateAfterMatch(string source, string prefix, string suffix, string englishPrefix, string englishSuffix)
        {
            var scoreStart = prefix.Length;
            var scoreEnd = source.IndexOf(suffix, scoreStart, StringComparison.Ordinal);
            if (scoreEnd < 0)
                return null;
            var score = source.Substring(scoreStart, scoreEnd - scoreStart);
            var result = englishPrefix + score + englishSuffix;
            var remainder = source.Substring(scoreEnd + suffix.Length);
            const string assistantLine = "\n\nO adjunto espera que sejas tu a puxar a primeira conversa. Ninguém te entrega um relatório de humor.";
            if (remainder.StartsWith(assistantLine, StringComparison.Ordinal))
                remainder = "\n\nYour assistant is waiting for you to start the first conversation. No one is handing you a mood report." + remainder.Substring(assistantLine.Length);
            return result + remainder;
        }

        static string ReplacePlayerPair(string source, string prefix, string englishPrefix)
        {
            var split = source.IndexOf(" para ", prefix.Length, StringComparison.Ordinal);
            if (split < 0 || !source.EndsWith(".", StringComparison.Ordinal))
                return englishPrefix + source.Substring(prefix.Length);
            return englishPrefix + source.Substring(prefix.Length, split - prefix.Length) + " to "
                + source.Substring(split + 6).TrimEnd('.') + ".";
        }

        static string TranslateCrossing(string source, string prefix, string englishPrefix)
        {
            const string outlet = " sai pela linha.";
            const string noTarget = " não encontra ninguém.";
            const string cleared = " é afastado.";
            const string takes = " vai bater o canto.";
            if (source.EndsWith(outlet, StringComparison.Ordinal))
                return englishPrefix + source.Substring(prefix.Length, source.Length - prefix.Length - outlet.Length) + " goes out of play.";
            if (source.EndsWith(noTarget, StringComparison.Ordinal))
                return englishPrefix + source.Substring(prefix.Length, source.Length - prefix.Length - noTarget.Length) + " finds no one.";
            if (source.EndsWith(cleared, StringComparison.Ordinal))
                return englishPrefix + source.Substring(prefix.Length, source.Length - prefix.Length - cleared.Length) + " is cleared.";
            if (source.EndsWith(takes, StringComparison.Ordinal))
                return "Corner. " + source.Substring(prefix.Length, source.Length - prefix.Length - takes.Length) + " will take it.";
            return ReplacePlayerPair(source, prefix, englishPrefix);
        }

        static string TranslateGoalDescription(string description)
        {
            return TranslateShotEvent(description)
                .Replace("de primeira", "first time")
                .Replace("de pé esquerdo", "with the left foot")
                .Replace("de pé direito", "with the right foot")
                .Replace("de penálti", "from the penalty spot")
                .Replace("de penalti", "from the penalty spot");
        }

        static string TranslateShotEvent(string source)
        {
            var translated = source
                .Replace("Livre direto potente de ", "Powerful free kick by ")
                .Replace("Remate de trivela de ", "Outside-foot shot by ")
                .Replace("Remate com flair de ", "Flair shot by ")
                .Replace("Cabeceamento de ", "Header by ")
                .Replace("Remate rasteiro de ", "Low shot by ")
                .Replace("Remate de longe de ", "Long-range shot by ")
                .Replace("Remate potente de ", "Powerful shot by ")
                .Replace("Remate colocado de ", "Placed shot by ")
                .Replace("Chilena de ", "Bicycle kick by ")
                .Replace("Remate de ", "Shot by ")
                .Replace("O guarda-redes defende com os pés.", "The goalkeeper saves with their feet.")
                .Replace("O guarda-redes defende.", "The goalkeeper makes the save.")
                .Replace("Grande defesa. ", "Great save. ")
                .Replace("Bate na trave.", "Hits the post.")
                .Replace("Passa ao lado da baliza.", "Goes just wide.")
                .Replace("Falha a baliza.", "Misses the target.");
            return translated;
        }

        public static void Select(GameLanguage language, bool markPromptSeen = false)
        {
            PlayerPrefs.SetInt(LanguageKey, language == GameLanguage.English ? 0 : 1);
            if (markPromptSeen)
            {
                PlayerPrefs.SetInt(PromptKey, 1);
                PlayerPrefs.Save();
            }
            ApplyScene();
            LanguageChanged?.Invoke();
        }

        public static void ApplyScene()
        {
            var texts = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < texts.Length; i++)
            {
                var text = texts[i];
                if (text == null || !text.gameObject.scene.IsValid())
                    continue;
                var input = text.GetComponentInParent<InputField>();
                if (input != null && text.gameObject.name != "Placeholder")
                    continue;
                var localizer = text.GetComponent<LocalizedText>();
                if (localizer == null)
                    localizer = text.gameObject.AddComponent<LocalizedText>();
                localizer.ApplyNow();
            }
        }

        public static void ShowFirstRunPrompt(Component host)
        {
            if (HasChosenLanguage || host == null)
                return;
            var canvas = host.GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;

            var go = new GameObject("LanguageFirstRun", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(canvas.transform, false);
            BroadcastUi.Stretch(go.GetComponent<RectTransform>());
            var overlayCanvas = go.GetComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 1000;
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;

            var dim = BroadcastUi.Solid(go.transform, "Dim", new Color(0.025f, 0.01f, 0.07f, 0.88f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            var card = BroadcastUi.Panel(go.transform, "LanguageCard", new Color(0.1f, 0.04f, 0.2f, 1f), true);
            BroadcastUi.Place(card.rectTransform, 0.31f, 0.34f, 0.69f, 0.66f);
            var heading = BroadcastUi.Label(card.transform, "Heading", "Escolhe o idioma", 28, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.06f, 0.63f, 0.94f, 0.9f);
            var note = BroadcastUi.Label(card.transform, "Note", "Podes alterar esta opção nas definições.", 16, UiTheme.Muted, TextAnchor.MiddleCenter);
            BroadcastUi.Place(note.rectTransform, 0.06f, 0.49f, 0.94f, 0.65f);

            var english = BroadcastUi.Button(card.transform, "English", "ENGLISH", () =>
            {
                Select(GameLanguage.English, true);
                UnityEngine.Object.Destroy(go);
            }, UiTheme.Accent, UiTheme.ButtonText, 18);
            BroadcastUi.Place(english.GetComponent<RectTransform>(), 0.06f, 0.16f, 0.47f, 0.42f);
            var portuguese = BroadcastUi.Button(card.transform, "Portuguese", "PORTUGUÊS", () =>
            {
                Select(GameLanguage.Portuguese, true);
                UnityEngine.Object.Destroy(go);
            }, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 18);
            BroadcastUi.Place(portuguese.GetComponent<RectTransform>(), 0.53f, 0.16f, 0.94f, 0.42f);
        }
    }

    public sealed class LocalizedText : MonoBehaviour
    {
        Text _text;
        string _source;
        string _lastApplied;
        GameLanguage _lastLanguage;

        void Awake()
        {
            _text = GetComponent<Text>();
            if (_text != null)
                _source = _text.text;
        }

        void LateUpdate()
        {
            if (_text == null)
                return;
            if (_text.text != _lastApplied)
            {
                _source = LocalizationManager.Current == GameLanguage.English
                    ? LocalizationManager.SourceForEnglish(_text.text)
                    : _text.text;
                ApplyNow();
            }
        }

        public void ApplyNow()
        {
            if (_text == null)
                _text = GetComponent<Text>();
            if (_text == null)
                return;
            var language = LocalizationManager.Current;
            if (_lastApplied != null && language != _lastLanguage)
            {
                if (_source == _lastApplied && _lastLanguage == GameLanguage.English)
                    _source = LocalizationManager.SourceForEnglish(_lastApplied);
                else if (_source == null)
                    _source = _lastApplied;
            }
            if (_source == null || (_lastApplied == null && _text.text.Length > 0))
                _source = _text.text;
            _text.text = LocalizationManager.Translate(_source);
            _lastApplied = _text.text;
            _lastLanguage = language;
        }
    }
}
