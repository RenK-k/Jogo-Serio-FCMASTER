using System;
using System.Collections.Generic;
using CoachMaster.Domain;
using CoachMaster.UI;
using UnityEngine;

namespace CoachMaster.Data
{
    public static class WorldCatalog
    {
        const int PlayersPerClub = 25;

        public static WorldState CreateShell()
        {
            var world = new WorldState();
            world.Leagues.Add(new League
            {
                Id = Ids.League("atlas"),
                Name = "Liga Portugal",
                ShortName = "Liga Portugal",
                Tier = LeagueTier.First,
                Description = "A principal competição de Portugal. Dezoito clubes disputam o título e a permanência.",
                ClubCount = 18
            });
            world.Leagues.Add(new League
            {
                Id = Ids.League("horizonte"),
                Name = "Liga Portugal 2",
                ShortName = "Liga Portugal 2",
                Tier = LeagueTier.Second,
                Description = "A segunda liga de Portugal. Dezoito clubes lutam pela subida à Liga Portugal.",
                ClubCount = 18
            });
            AddDivision(world, "inglesa", "Premier League", "Inglaterra", LeagueTier.First, 20);
            AddDivision(world, "inglesa-championship", "Championship", "Inglaterra", LeagueTier.Second, 24);
            AddDivision(world, "inglesa-league-one", "League One", "Inglaterra", LeagueTier.Third, 24);
            AddDivision(world, "inglesa-2", "League Two", "Inglaterra", LeagueTier.Fourth, 24);
            AddDivision(world, "francesa", "Ligue 1", "França", LeagueTier.First, 18);
            AddDivision(world, "bundesliga", "Bundesliga", "Alemanha", LeagueTier.First, 18);
            AddDivision(world, "eredivisie", "Eredivisie", "Países Baixos", LeagueTier.First, 18);
            AddDivision(world, "brasileira", "Brasileirão Série A", "Brasil", LeagueTier.First, 20);
            AddDivision(world, "espanhola", "LaLiga", "Espanha", LeagueTier.First, 20);
            AddDivision(world, "espanhola-2", "LaLiga Hypermotion", "Espanha", LeagueTier.Second, 22);
            world.Leagues.Add(new League
            {
                Id = Ids.League("campeoes"),
                Name = "Liga dos Campeões",
                ShortName = "Campeões",
                Tier = LeagueTier.Continental,
                Description = "Os melhores de cada primeira divisão. Não se escolhe clube aqui: a classificação aparece na carreira.",
                ClubCount = 14
            });
            return world;
        }

        static void AddDivision(WorldState world, string slug, string name, string country, LeagueTier tier, int clubCount)
        {
            world.Leagues.Add(new League
            {
                Id = Ids.League(slug),
                Name = name,
                ShortName = name,
                Tier = tier,
                Description = tier == LeagueTier.First
                    ? "Primeira divisão de " + country + ". " + clubCount + " clubes."
                    : DivisionLabel(tier) + " de " + country + ". " + clubCount + " clubes.",
                ClubCount = clubCount
            });
        }

        static string DivisionLabel(LeagueTier tier)
        {
            switch (tier)
            {
                case LeagueTier.Second: return "Segunda divisão";
                case LeagueTier.Third: return "Terceira divisão";
                case LeagueTier.Fourth: return "Quarta divisão";
                default: return "Divisão";
            }
        }

        public static WorldState Create()
        {
            var world = CreateShell();
            EnsureAllClubs(world);
            return world;
        }

        public static void EnsureAllClubs(WorldState world)
        {
            foreach (var league in world.Leagues)
                EnsureLeagueClubs(world, league.Id, false);

            FaceBook.Deal(world);
        }

        public static void EnsureLeagueClubs(WorldState world, string leagueId)
        {
            EnsureLeagueClubs(world, leagueId, true);
        }

        static void EnsureLeagueClubs(WorldState world, string leagueId, bool dealFaces)
        {
            if (world.LoadedLeagueIds.Contains(leagueId))
                return;

            if (leagueId == Ids.League("atlas"))
                AddAtlasClubs(world, leagueId);
            else if (leagueId == Ids.League("horizonte"))
                AddHorizonteClubs(world, leagueId);
            else if (leagueId != Ids.League("campeoes"))
                AddGeneratedDivision(world, leagueId);

            if (leagueId != Ids.League("campeoes"))
                PadLeague(world, leagueId);

            world.LoadedLeagueIds.Add(leagueId);
            if (dealFaces)
                FaceBook.Deal(world);
        }

        static void AddAtlasClubs(WorldState world, string leagueId)
        {
            AddClub(world, leagueId, new ClubDefinition
            {
                Slug = "norte-maritimo",
                Name = "FC Norte Marítimo",
                ShortName = "Norte Marítimo",
                City = "Porto Branco",
                Stadium = "Estádio das Marés",
                FoundedYear = 1921,
                History = "Clube histórico da costa norte, identificado com o porto e com uma massa adepta exigente. Viveu o auge nos anos 80 e tenta agora recuperar protagonismo.",
                PublicReputation = "Clube de tradição, ainda visto como candidato a lugares cimeiros.",
                Reputation = 78,
                Finance = Fin(4200000, 180000, 900000, "A tesouraria aguenta o presente, mas a direção pede contenção."),
                Tactics = Identity("4-3-3", "Pressão alta", "Construção curta", "Laterais ofensivos", "Linha defensiva alta", "Hélder Paiva"),
                BoardObjectives = Obj("Terminar no primeiro terço da tabela", "Manter a identidade ofensiva do clube", "Integrar dois jogadores da formação"),
                DressingRoomAtmosphere = "O grupo parece fechado sobre si próprio.",
                PublicTeamState = "A pré-época arranca com um plantel experiente, mas o ambiente no centro de treinos não é o de um recomeço tranquilo.",
                HiddenInternalProblems = "O capitão perdeu influência após um conflito com o antigo treinador. Há um grupo de jogadores mais novos que o ignora.",
                PublicCoachVacancyNote = "O clube procura treinador após a saída de Hélder Paiva no final da época.",
                HiddenDismissalTruth = "Paiva foi despedido depois de perder o balneário e de uma sequência de jogos sem plano claro no último terço da época.",
                DressingRoomTension = 62,
                Staff = Staff("norte-maritimo", "Rui Lemos", "Paulo Cerveira", "Nuno Batista", "Carla Mendes"),
                Players = new[]
                {
                    "1|GR|André Lobo|31|R", "12|GR|Tiago Serrão|24|R",
                    "2|LD|Miguel Faro|27|R", "3|DC|Rui Capitão|33|R|C", "4|DC|Sérgio Dantas|29|R", "15|DC|Ivan Kolar|26|R",
                    "5|LE|Bruno Vale|28|L", "6|MDC|Pedro Nolasco|30|R", "8|MC|João Pires|25|R|V", "10|MC|Mateo Ruiz|27|R",
                    "7|ED|Daniel Costa|23|R", "11|EE|Yannick Tavares|26|R", "9|PL|Hélder Simões|32|R",
                    "18|PL|Lucas Freire|22|R", "14|MO|Afonso Reis|21|L", "16|LD|Hugo Brito|24|R",
                    "20|LE|Kelvin Dias|22|L", "21|MC|Omar Silva|20|R"
                },
                Relations = new[] { "3:8:-35", "3:14:-20", "8:10:40" }
            });

            AddClub(world, leagueId, new ClubDefinition
            {
                Slug = "sporting-colina",
                Name = "Sporting da Colina",
                ShortName = "Colina",
                City = "Colina Verde",
                Stadium = "Campo da Quinta",
                FoundedYear = 1908,
                History = "Um dos clubes mais antigos do país. A Colina cultiva a imagem de casa de formação e de futebol apoiado no meio-campo.",
                PublicReputation = "Instituição respeitada, com exigência alta dentro e fora de campo.",
                Reputation = 82,
                Finance = Fin(6100000, 210000, 1500000, "Situação estável, com margem para investir com critério."),
                Tactics = Identity("4-2-3-1", "Pressão média", "Construção curta", "Laterais equilibrados", "Linha média", "Vítor Gama"),
                BoardObjectives = Obj("Disputar o título", "Manter a formação como prioridade", "Evitar um futebol excessivamente direto"),
                DressingRoomAtmosphere = "Há respeito pelo clube, mas também impaciência.",
                PublicTeamState = "O plantel é talentoso. A última época ficou aquém das expectativas da direção.",
                HiddenInternalProblems = "Dois médios disputam o mesmo lugar e deixaram de se falar. O avançado titular quer mais protagonismo.",
                PublicCoachVacancyNote = "Vítor Gama deixou o cargo após uma época considerada insuficiente.",
                HiddenDismissalTruth = "Gama recusou ceder a pressões da direção para alinhar um avançado contratado a peso de ouro e acabou isolado.",
                DressingRoomTension = 48,
                Staff = Staff("sporting-colina", "Miguel Hortênsia", "José Paulo", "Elisa Rocha", "Hugo Faria"),
                Players = new[]
                {
                    "1|GR|Rui Palmeira|29|R", "13|GR|Nuno Caeiro|22|R",
                    "2|LD|Tomás Leal|26|R", "4|DC|Gonçalo Mota|31|R|C", "5|DC|Luka Petrov|28|R", "23|DC|Rafael Nunes|23|R",
                    "3|LE|Diogo Amaral|27|L", "6|MDC|Samuel Cruz|30|R", "8|MDC|Ivo Queirós|24|R", "10|MO|Bernardo Sá|25|L|V",
                    "7|ED|Kevin Lopes|22|R", "11|EE|Filipe Monteiro|28|R", "9|PL|André Varela|29|R",
                    "18|PL|Mauro Pinho|21|R", "14|MC|César Vidal|26|R", "16|LD|Eric Santos|24|R",
                    "19|EE|Jonas Freire|20|R", "21|MC|Paulo Raminhos|19|R"
                },
                Relations = new[] { "6:8:-45", "9:10:-25", "4:10:30" }
            });

            AddClub(world, leagueId, new ClubDefinition
            {
                Slug = "uniao-valverde",
                Name = "União de Valverde",
                ShortName = "Valverde",
                City = "Valverde",
                Stadium = "Municipal do Vale",
                FoundedYear = 1946,
                History = "Clube de uma cidade industrial, conhecido pela intensidade e por um público próximo do relvado. Raramente passa despercebido.",
                PublicReputation = "Casa difícil, com reputação de grupo competitivo.",
                Reputation = 64,
                Finance = Fin(800000, 95000, 150000, "A direção admite um orçamento curto para esta época."),
                Tactics = Identity("4-4-2", "Pressão alta", "Jogo direto", "Laterais ofensivos", "Linha alta", "Manuel Correia"),
                BoardObjectives = Obj("Garantir a manutenção", "Manter a intensidade que o público exige", "Controlar os custos do plantel"),
                DressingRoomAtmosphere = "O grupo está inquieto.",
                PublicTeamState = "Vários jogadores da última época saíram. O plantel mistura veteranos e jovens ainda por afirmar.",
                HiddenInternalProblems = "Há um núcleo veterano que resiste a qualquer mudança de métodos. O fisioterapeuta já alertou internamente para sobrecarga.",
                PublicCoachVacancyNote = "Manuel Correia foi dispensado depois de uma época irregular.",
                HiddenDismissalTruth = "Correia insistiu em treinos muito intensos até ao fim da época e o plantel acusou o esforço nos jogos decisivos.",
                DressingRoomTension = 71,
                Staff = Staff("uniao-valverde", "Carlos Bento", "Mário Luz", "Sónia Pires", "Rui Galvão"),
                Players = new[]
                {
                    "1|GR|Paulo Matos|34|R", "12|GR|Eduardo Lima|23|R",
                    "2|LD|Nuno Barros|29|R", "4|DC|Hélder Cunha|32|R|C", "5|DC|Marko Ilic|27|R", "3|DC|Vítor Ramos|24|R",
                    "6|LE|Ricardo Neves|28|L", "8|MC|Tiago Borges|31|R", "10|MC|André Fogaça|26|R|V", "7|ED|Wilson Dias|25|R",
                    "11|EE|Jorge Antunes|30|L", "9|PL|Sérgio Lameira|33|R", "19|PL|Rafael Gomes|22|R",
                    "14|MDC|Lucas Pimenta|21|R", "15|LD|Ivo Martins|23|R", "16|LE|Pedro Galego|20|L",
                    "18|MC|Daniel Rocha|24|R", "21|ED|Kevin Moreira|19|R"
                },
                Relations = new[] { "4:10:-15", "9:19:-30", "8:4:25" }
            });

            AddClub(world, leagueId, new ClubDefinition
            {
                Slug = "atletico-ribeira",
                Name = "Atlético Ribeira",
                ShortName = "Ribeira",
                City = "Ribeira Nova",
                Stadium = "Estádio do Cais",
                FoundedYear = 1957,
                History = "Clube ribeirinho com uma base adepta jovem. Nos últimos anos apostou em jogadores técnicos e numa construção mais paciente.",
                PublicReputation = "Clube em crescimento, ainda a consolidar-se entre os grandes.",
                Reputation = 70,
                Finance = Fin(2500000, 140000, 600000, "Há investimento, mas a direção quer retorno desportivo imediato."),
                Tactics = Identity("3-4-3", "Pressão alta", "Construção curta", "Ala ofensivos", "Linha alta", "Sofia Menezes"),
                BoardObjectives = Obj("Lutar pela qualificação europeia", "Não abdicar da construção apoiada", "Afirmar a identidade do projeto"),
                DressingRoomAtmosphere = "Curiosidade misturada com incerteza.",
                PublicTeamState = "O plantel está habituado a um sistema específico. Qualquer mudança será sentida.",
                HiddenInternalProblems = "Três jogadores do corredor central sentem-se desvalorizados após a última época.",
                PublicCoachVacancyNote = "Sofia Menezes saiu por acordo mútuo após desentendimentos com a direção.",
                HiddenDismissalTruth = "A direção queria um futebol mais direto. Menezes recusou e saiu.",
                DressingRoomTension = 44,
                Staff = Staff("atletico-ribeira", "Pedro Alvim", "Rui Lopes", "Gonçalo Esteves", "Inês Varanda"),
                Players = new[]
                {
                    "1|GR|Diogo Faria|28|R", "13|GR|Telmo Vieira|23|R",
                    "3|DC|Artur Pinheiro|30|R|C", "4|DC|Nélson Cruz|27|R", "5|DC|David Moreira|25|L",
                    "7|LD|Rodrigo Cunha|24|R", "11|LE|Fábio Lima|26|L", "6|MDC|Paulo Henriques|29|R",
                    "8|MC|Simão Reis|22|R", "10|MO|Leandro Vaz|27|L|V", "17|ED|Bryan Costa|23|R",
                    "21|EE|Otávio Silva|24|R", "9|PL|Mauro Andrade|28|R", "19|PL|Guilherme Tavares|21|R",
                    "14|MC|Rui Gaspar|20|R", "16|DC|Hugo Lemos|22|R", "18|LD|Ivo Paredes|19|R",
                    "22|EE|Salvador Neto|20|L"
                },
                Relations = new[] { "3:10:-18", "6:8:-28", "10:9:22" }
            });

            AddClub(world, leagueId, new ClubDefinition
            {
                Slug = "real-monte-alto",
                Name = "Real de Monte Alto",
                ShortName = "Monte Alto",
                City = "Monte Alto",
                Stadium = "Estádio da Serra",
                FoundedYear = 1933,
                History = "Clube de interior com uma identidade defensiva muito marcada. Ganha poucos jogos por goleada e sofre ainda menos.",
                PublicReputation = "Organizado, difícil de bater, por vezes acusado de ser demasiado conservador.",
                Reputation = 68,
                Finance = Fin(1900000, 120000, 350000, "Contas equilibradas e uma direção avessa a riscos."),
                Tactics = Identity("5-4-1", "Pressão baixa", "Construção longa", "Laterais defensivos", "Linha baixa", "Alberto Sousa"),
                BoardObjectives = Obj("Ficar a meio da tabela", "Manter a solidez defensiva", "Evitar experiências táticas bruscas"),
                DressingRoomAtmosphere = "Disciplinado, quase silencioso.",
                PublicTeamState = "O grupo está habituado a um método rígido. O antigo treinador deixou pouca margem de interpretação.",
                HiddenInternalProblems = "Os avançados estão frustrados com a falta de bola. Dois centrais discordam sobre quem deve sair a jogar.",
                PublicCoachVacancyNote = "Alberto Sousa reformou-se no final da época, deixando o cargo vago.",
                HiddenDismissalTruth = "A reforma foi a versão pública. Internamente, a direção queria um futebol menos previsível e acelerou a saída.",
                DressingRoomTension = 39,
                Staff = Staff("real-monte-alto", "João Sequeira", "Luís Brito", "Teresa Calado", "Mário Pinho"),
                Players = new[]
                {
                    "1|GR|César Pinto|32|R", "12|GR|André Gama|25|R",
                    "2|LD|Paulo Robalo|29|R", "3|DC|Rui Esteves|34|R|C", "4|DC|Jorge Cid|28|R", "5|DC|Milan Horvat|27|R",
                    "6|LE|Nuno Freire|30|L", "8|MC|Vítor Lage|31|R", "10|MC|Bruno Azevedo|26|R|V",
                    "7|ED|Pedro Guedes|24|R", "11|EE|Ricardo Teles|27|L", "9|PL|Hélder Paixão|33|R",
                    "14|MDC|Tiago Moura|23|R", "15|DC|Daniel Alves|22|R", "16|LD|Kevin Ramos|21|R",
                    "18|PL|Ivo Cunha|20|R", "19|MC|Salvador Dias|19|R", "21|LE|Gonçalo Pires|24|L"
                },
                Relations = new[] { "3:4:-22", "9:8:-40", "10:11:18" }
            });

            AddClub(world, leagueId, new ClubDefinition
            {
                Slug = "academica-prado",
                Name = "Académica do Prado",
                ShortName = "Prado",
                City = "Prado",
                Stadium = "Estádio Universitário",
                FoundedYear = 1919,
                History = "Clube ligado à universidade da cidade. Vive da formação e de um estilo associado à inteligência tática.",
                PublicReputation = "Respeitado pela formação, com menos peso mediático do que os rivais da costa.",
                Reputation = 60,
                Finance = Fin(1100000, 88000, 200000, "O orçamento é modesto e depende de vendas pontuais da formação."),
                Tactics = Identity("4-3-3", "Pressão média", "Construção curta", "Laterais equilibrados", "Linha média", "Fernando Vale"),
                BoardObjectives = Obj("Manutenção confortável", "Dar minutos a jogadores da formação", "Manter a identidade de jogo apoiado"),
                DressingRoomAtmosphere = "Jovem e ainda por sedimentar.",
                PublicTeamState = "Há muitos jogadores novos no grupo. Falta perceber quem assume o vestuário.",
                HiddenInternalProblems = "O capitão é respeitado pelos mais velhos, mas os recém-chegados não o reconhecem como referência.",
                PublicCoachVacancyNote = "Fernando Vale foi despedido após uma última volta abaixo do esperado.",
                HiddenDismissalTruth = "Vale perdeu a confiança de vários jovens do plantel e a direção interpretou isso como perda de autoridade.",
                DressingRoomTension = 51,
                Staff = Staff("academica-prado", "Helena Duarte", "Paulo Reis", "André Cabral", "Liliana Costa"),
                Players = new[]
                {
                    "1|GR|João Magalhães|26|R", "12|GR|Rui Tavares|21|R",
                    "2|LD|Afonso Pinto|23|R", "3|DC|Miguel Borges|28|R|C", "4|DC|Tomás Vieira|22|R", "5|LE|Diogo Laranjeiro|24|L",
                    "6|MDC|Pedro Cunha|25|R", "8|MC|Rafael Antunes|22|R|V", "10|MC|Simão Lopes|20|L",
                    "7|ED|Francisco Melo|21|R", "11|EE|Gabriel Rocha|23|R", "9|PL|Nuno Saraiva|27|R",
                    "14|PL|Leandro Filipe|19|R", "15|DC|Ivo Martins|20|R", "16|LD|Bruno Caeiro|22|R",
                    "18|MO|Mateus Silva|18|R", "19|MC|Daniel Pinho|19|R", "21|LE|Hugo Neves|21|L"
                },
                Relations = new[] { "3:8:12", "3:10:-26", "9:14:-10" }
            });

            AddClub(world, leagueId, new ClubDefinition
            {
                Slug = "estrela-alcantara",
                Name = "Estrela de Alcântara",
                ShortName = "Alcântara",
                City = "Alcântara",
                Stadium = "Estádio da Luzinha",
                FoundedYear = 1962,
                History = "Clube urbano, de bairro, com um público barulhento. Oscila entre épocas ambiciosas e crises internas.",
                PublicReputation = "Imprevisível. Capaz do melhor e do pior na mesma época.",
                Reputation = 58,
                Finance = Fin(-400000, 90000, 0, "A direção fala abertamente de um momento financeiro delicado."),
                Tactics = Identity("4-3-3", "Pressão alta", "Jogo misto", "Laterais ofensivos", "Linha alta", "Paulo Rangel"),
                BoardObjectives = Obj("Evitar a descida", "Acalmar o ambiente interno", "Recuperar a relação com a massa adepta"),
                DressingRoomAtmosphere = "Tenso.",
                PublicTeamState = "A última época foi agitada. O plantel chegou à pré-época com um treinador novo pela terceira vez em dois anos.",
                HiddenInternalProblems = "Dois jogadores discutiram em público no último mês. Há um atraso salarial conhecido internamente.",
                PublicCoachVacancyNote = "Paulo Rangel foi despedido após uma sequência de resultados negativos.",
                HiddenDismissalTruth = "Rangel acusou a direção de interferir nas convocatórias. No dia seguinte foi dispensado.",
                DressingRoomTension = 84,
                Staff = Staff("estrela-alcantara", "Sérgio Mota", "Carlos Vila", "Beatriz Nunes", "Paulo Guedes"),
                Players = new[]
                {
                    "1|GR|Mário Santos|33|R", "24|GR|Ivo Rebelo|22|R",
                    "2|LD|Ricardo Paiva|28|R", "3|DC|Hélder Fonseca|31|R|C", "4|DC|Bruno Cid|26|R", "6|LE|Anderson Lima|29|L",
                    "5|MDC|Pedro Tavares|30|R", "8|MC|Luís Faria|27|R|V", "10|MO|Dany Costa|24|R",
                    "7|ED|Wilson Teixeira|25|R", "11|EE|Jota Mendes|23|R", "9|PL|Rui Laranjo|32|R",
                    "17|PL|Kevin Almeida|21|R", "14|MC|Nuno Gregório|22|R", "15|DC|Salvador Reis|20|R",
                    "16|LD|Tiago Varela|24|R", "19|EE|Eric Pires|19|L", "21|MDC|Omar Cunha|20|R"
                },
                Relations = new[] { "3:9:-50", "8:10:-32", "5:8:20" }
            });

            AddClub(world, leagueId, new ClubDefinition
            {
                Slug = "ferroviario",
                Name = "Clube Ferroviário",
                ShortName = "Ferroviário",
                City = "Estação Nova",
                Stadium = "Campo da Linha",
                FoundedYear = 1938,
                History = "Fundado por trabalhadores da linha férrea. Mantém uma identidade operária e um estilo baseado no coletivo.",
                PublicReputation = "Clube sólido, pouco mediático, difícil de quebrar.",
                Reputation = 55,
                Finance = Fin(650000, 78000, 80000, "Vive dentro das possibilidades. Não há margem para erros de mercado."),
                Tactics = Identity("4-1-4-1", "Pressão média", "Construção curta", "Laterais equilibrados", "Linha média", "José Cardoso"),
                BoardObjectives = Obj("Manutenção", "Preservar o espírito coletivo", "Dar continuidade à formação local"),
                DressingRoomAtmosphere = "Unido na aparência, cauteloso na verdade.",
                PublicTeamState = "O grupo conhece-se bem. A saída do treinador surpreendeu muita gente fora do clube.",
                HiddenInternalProblems = "O médio defensivo sente que carrega a equipa sozinho. O avançado pede um parceiro e não o tem.",
                PublicCoachVacancyNote = "José Cardoso saiu após oito épocas no cargo.",
                HiddenDismissalTruth = "Cardoso pediu um reforço que a direção recusou. A relação quebrou-se em silêncio e a saída foi apresentada como decisão mútua.",
                DressingRoomTension = 36,
                Staff = Staff("ferroviario", "António Lobo", "Rui Campos", "Cátia Mendes", "Vítor Soares"),
                Players = new[]
                {
                    "1|GR|Paulo Cunha|30|R", "12|GR|André Novo|23|R",
                    "2|LD|João Barata|27|R", "3|DC|Sérgio Maia|32|R|C", "4|DC|Rui Paredes|28|R", "5|LE|Nuno Lopes|26|L",
                    "6|MDC|Pedro Gama|29|R|V", "8|MC|Daniel Vila|25|R", "10|MC|Ivo Santos|24|R",
                    "7|ED|Bruno Reis|22|R", "11|EE|Fábio Costa|27|R", "9|PL|Hélder Vaz|31|R",
                    "14|PL|Tomás Pires|21|R", "15|DC|Luka Marin|23|R", "16|LD|Ricardo Neves|20|R",
                    "18|MC|Afonso Lima|19|R", "19|EE|Kevin Duarte|20|L", "21|MDC|Miguel Teles|22|R"
                },
                Relations = new[] { "6:9:-24", "3:6:35", "9:14:10" }
            });
        }

        static void AddHorizonteClubs(WorldState world, string leagueId)
        {
            AddClub(world, leagueId, new ClubDefinition
            {
                Slug = "serra-nova",
                Name = "GD Serra Nova",
                ShortName = "Serra Nova",
                City = "Serra Nova",
                Stadium = "Municipal da Serra",
                FoundedYear = 1974,
                History = "Clube de montanha, recém-chegado ao primeiro plano da segunda liga. Cresceu com uma direção ambiciosa e um público fiel.",
                PublicReputation = "Promovido com mérito, ainda a adaptar-se ao rigor da Liga Portugal 2.",
                Reputation = 42,
                Finance = Fin(320000, 52000, 40000, "O orçamento é curto. Qualquer despesa extra precisa de aprovação da direção."),
                Tactics = Identity("4-4-2", "Pressão média", "Jogo direto", "Laterais ofensivos", "Linha média", "Rui Tavares"),
                BoardObjectives = Obj("Lutar pela subida", "Manter o grupo unido", "Não perder a identidade da casa"),
                DressingRoomAtmosphere = "Entusiasmado, por vezes ingénuo.",
                PublicTeamState = "O plantel acredita na subida. Falta perceber se essa crença se aguenta ao primeiro revés.",
                HiddenInternalProblems = "Dois avançados não se entendem na área. O capitão cobre falhas que a direção ainda não vê.",
                PublicCoachVacancyNote = "Rui Tavares saiu para um clube da Liga Portugal, deixando o cargo vago.",
                HiddenDismissalTruth = "A versão pública é uma saída por convite. Por dentro, Tavares já tinha perdido a paciência com interferências da direção nos treinos.",
                DressingRoomTension = 33,
                Staff = Staff("serra-nova", "Paulo Minhoto", "José Rolo", "Ana Borges", "Hugo Vale"),
                Players = new[]
                {
                    "1|GR|Tiago Rola|28|R", "12|GR|Nuno Dias|22|R",
                    "2|LD|Pedro Fontes|26|R", "3|DC|Rui Lage|31|R|C", "4|DC|Bruno Pinho|27|R", "5|LE|Ivo Costa|24|L",
                    "6|MC|Daniel Matos|25|R", "8|MC|João Serra|23|R|V", "7|ED|Wilson Gomes|24|R",
                    "11|EE|Fábio Novo|26|R", "9|PL|Sérgio Monte|29|R", "19|PL|André Cunha|22|R",
                    "14|MDC|Lucas Vieira|21|R", "15|DC|Mário Teles|23|R", "16|LD|Kevin Lima|20|R",
                    "18|MC|Rafael Pires|19|R", "21|EE|Tomás Rios|20|L", "10|MO|Diogo Barreto|22|R"
                },
                Relations = new[] { "9:19:-38", "3:8:28", "6:8:-12" }
            });

            AddClub(world, leagueId, new ClubDefinition
            {
                Slug = "portuarios",
                Name = "Portuários FC",
                ShortName = "Portuários",
                City = "Cais Velho",
                Stadium = "Estádio do Doca",
                FoundedYear = 1951,
                History = "Clube portuário, físico e orgulhoso. Costuma ser candidato à subida e raramente aceita épocas cinzentas.",
                PublicReputation = "Candidato natural à Liga Portugal, com pressão alta da massa adepta.",
                Reputation = 50,
                Finance = Fin(900000, 70000, 120000, "Há alguma folga, mas a direção quer o regresso à primeira liga sem despesismo."),
                Tactics = Identity("4-3-3", "Pressão alta", "Construção mista", "Laterais ofensivos", "Linha alta", "Carlos Magalhães"),
                BoardObjectives = Obj("Subir de divisão", "Ganhar em casa com autoridade", "Recuperar jogadores-chave da última época"),
                DressingRoomAtmosphere = "Ambicioso e impaciente.",
                PublicTeamState = "O plantel sente que devia já estar na Liga Portugal. Essa ideia está presente em quase todas as conversas.",
                HiddenInternalProblems = "O guarda-redes titular e o capitão não se falam desde um golo sofrido no playoff. O grupo dividiu-se.",
                PublicCoachVacancyNote = "Carlos Magalhães foi despedido após falhar a subida no último jogo.",
                HiddenDismissalTruth = "Magalhães foi responsabilizado pela gestão do grupo no playoff, não apenas pelo resultado.",
                DressingRoomTension = 77,
                Staff = Staff("portuarios", "Vítor Caldas", "Nuno Graça", "Rita Sousa", "Paulo Neves"),
                Players = new[]
                {
                    "1|GR|Rui Doca|32|R", "13|GR|André Marinho|24|R",
                    "2|LD|Miguel Cais|28|R", "3|DC|Hélder Porto|33|R|C", "4|DC|Ivan Petrov|27|R", "5|LE|Bruno Mar|26|L",
                    "6|MDC|Pedro Âncora|29|R", "8|MC|João Docas|25|R|V", "10|MC|Daniel Farol|24|R",
                    "7|ED|Wilson Praia|23|R", "11|EE|Yannick Costa|26|R", "9|PL|Sérgio Marés|30|R",
                    "18|PL|Lucas Pescador|21|R", "14|MO|Afonso Cunha|22|L", "15|DC|Tiago Molhe|23|R",
                    "16|LD|Ivo Norte|20|R", "19|EE|Kevin Baía|19|R", "21|MC|Omar Leite|20|R"
                },
                Relations = new[] { "1:3:-55", "3:8:-20", "8:10:30" }
            });

            AddClub(world, leagueId, new ClubDefinition
            {
                Slug = "laranjais",
                Name = "Os Laranjais",
                ShortName = "Laranjais",
                City = "Vila Laranja",
                Stadium = "Campo das Laranjeiras",
                FoundedYear = 1981,
                History = "Clube relativamente novo, com uma identidade ofensiva e uma direção que gosta de ser vista. Vive de picos de entusiasmo.",
                PublicReputation = "Atraente para ver, irregular para confiar.",
                Reputation = 38,
                Finance = Fin(210000, 48000, 25000, "As contas estão justas. A direção promete estabilidade, não reforços caros."),
                Tactics = Identity("4-2-3-1", "Pressão alta", "Construção curta", "Laterais ofensivos", "Linha alta", "Pedro Laranja"),
                BoardObjectives = Obj("Chegar aos lugares de subida", "Manter um futebol ofensivo", "Afirmar o clube fora da região"),
                DressingRoomAtmosphere = "Leve na superfície.",
                PublicTeamState = "Há talento no ataque e dúvidas atrás. O antigo treinador deixou um grupo habituado a arriscar.",
                HiddenInternalProblems = "A defesa sente-se exposta e culpa o modelo. Os criativos recusam baixar o ritmo.",
                PublicCoachVacancyNote = "Pedro Laranja saiu após um desentendimento público com a direção.",
                HiddenDismissalTruth = "A direção pediu menos riscos. Laranja respondeu numa conferência de imprensa e foi dispensado no dia seguinte.",
                DressingRoomTension = 58,
                Staff = Staff("laranjais", "Rui Cidral", "Paulo Flor", "Marta Vieira", "João Pêssego"),
                Players = new[]
                {
                    "1|GR|Nuno Flor|27|R", "12|GR|Diogo Pomar|21|R",
                    "2|LD|Tomás Cid|24|R", "3|DC|Rui Pomar|29|R|C", "4|DC|André Sebe|25|R", "5|LE|Ivo Lima|23|L",
                    "6|MDC|Pedro Casca|26|R", "8|MDC|Daniel Rama|22|R", "10|MO|Bernardo Sol|24|L|V",
                    "7|ED|Kevin Lima|21|R", "11|EE|Fábio Doce|25|R", "9|PL|Mauro Citrino|28|R",
                    "18|PL|Lucas Verde|20|R", "14|MC|Afonso Folha|19|R", "15|DC|Salvador Pé|22|R",
                    "16|LD|Hugo Ramo|20|R", "19|EE|Jonas Flor|18|L", "21|MC|Paulo Sumo|19|R"
                },
                Relations = new[] { "3:10:-30", "6:10:-18", "9:10:25" }
            });

            AddClub(world, leagueId, new ClubDefinition
            {
                Slug = "vila-do-rio",
                Name = "Vila do Rio",
                ShortName = "Vila do Rio",
                City = "Vila do Rio",
                Stadium = "Margem Sul",
                FoundedYear = 1969,
                History = "Clube de uma vila ribeirinha, conhecido por formar laterais e médios. A subida não é obsessão, mas a dignidade é.",
                PublicReputation = "Sério, paciente, pouco dado a protagonismos.",
                Reputation = 36,
                Finance = Fin(180000, 41000, 15000, "Vive com o que tem. A formação é a principal fonte de oxigénio."),
                Tactics = Identity("4-3-3", "Pressão média", "Construção curta", "Laterais ofensivos", "Linha média", "António Ribeira"),
                BoardObjectives = Obj("Manutenção tranquila", "Dar espaço aos da casa", "Crescer sem perder a identidade"),
                DressingRoomAtmosphere = "Calmo, quase demasiado.",
                PublicTeamState = "O plantel é jovem. Há poucos líderes óbvios à primeira vista.",
                HiddenInternalProblems = "Ninguém quer o braçadeira. O antigo capitão saiu e o grupo ainda não escolheu uma referência.",
                PublicCoachVacancyNote = "António Ribeira saiu para cuidar da família, segundo o comunicado do clube.",
                HiddenDismissalTruth = "A saída familiar é verdadeira, mas Ribeira também estava desgastado com a falta de condições de treino.",
                DressingRoomTension = 29,
                Staff = Staff("vila-do-rio", "Luís Cais", "Rui Ponte", "Carla Lezíria", "Nuno Esteiro"),
                Players = new[]
                {
                    "1|GR|João Praia|25|R", "12|GR|Rui Cais|20|R",
                    "2|LD|Afonso Lezíria|22|R", "3|DC|Miguel Esteiro|27|R|C", "4|DC|Tomás Várzea|23|R", "5|LE|Diogo Margem|24|L",
                    "6|MDC|Pedro Caniçal|26|R", "8|MC|Rafael Sapal|21|R|V", "10|MC|Simão Broa|20|L",
                    "7|ED|Francisco Mouchão|21|R", "11|EE|Gabriel Lezíria|22|R", "9|PL|Nuno Triz|28|R",
                    "14|PL|Leandro Cais|19|R", "15|DC|Ivo Palha|20|R", "16|LD|Bruno Rio|22|R",
                    "18|MO|Mateus Calha|18|R", "19|MC|Daniel Poldra|19|R", "21|LE|Hugo Vala|21|L"
                },
                Relations = new[] { "3:8:8", "9:14:15", "6:10:-8" }
            });

            AddClub(world, leagueId, new ClubDefinition
            {
                Slug = "sc-penhas",
                Name = "Sport Clube Penhas",
                ShortName = "Penhas",
                City = "Penhas",
                Stadium = "Alto da Pedra",
                FoundedYear = 1944,
                History = "Clube de altitude, habituado ao frio e a jogos rijos. A casa vale por muitos pontos.",
                PublicReputation = "Difícil em casa, limitado fora. Respeitado pelo caráter.",
                Reputation = 40,
                Finance = Fin(260000, 47000, 20000, "A direção gere o clube com prudência e pouca tolerância a dívidas."),
                Tactics = Identity("4-4-2", "Pressão média", "Jogo direto", "Laterais defensivos", "Linha média", "Manuel Pedra"),
                BoardObjectives = Obj("Ficar na primeira metade da tabela", "Tornar o Alto da Pedra ainda mais difícil", "Evitar lesões da pré-época"),
                DressingRoomAtmosphere = "Durão.",
                PublicTeamState = "O grupo está habituado a um futebol de combate. Há dúvidas sobre o que o novo treinador vai pedir.",
                HiddenInternalProblems = "Dois jogadores regressam de lesões mal geridas. O preparador físico e o antigo treinador não se entendiam.",
                PublicCoachVacancyNote = "Manuel Pedra foi despedido após uma pré-época anterior marcada por várias baixas físicas.",
                HiddenDismissalTruth = "A direção considerou que Pedra ignorou alertas da equipa clínica.",
                DressingRoomTension = 61,
                Staff = Staff("sc-penhas", "José Rocha", "Paulo Granito", "Helena Serra", "Rui Fraga"),
                Players = new[]
                {
                    "1|GR|Paulo Fraga|31|R", "12|GR|André Rocha|23|R",
                    "2|LD|Nuno Pedra|28|R", "4|DC|Hélder Serra|32|R|C", "5|DC|Marko Berg|27|R", "3|DC|Vítor Cume|24|R",
                    "6|LE|Ricardo Gelo|28|L", "8|MC|Tiago Penha|30|R", "10|MC|André Cume|26|R|V",
                    "7|ED|Wilson Rocha|25|R", "11|EE|Jorge Neve|29|L", "9|PL|Sérgio Alto|33|R",
                    "19|PL|Rafael Pedra|22|R", "14|MDC|Lucas Fraga|21|R", "15|LD|Ivo Serra|23|R",
                    "16|LE|Pedro Cume|20|L", "18|MC|Daniel Rocha|24|R", "21|ED|Kevin Gelo|19|R"
                },
                Relations = new[] { "4:8:20", "9:19:-16", "5:3:-12" }
            });

            AddClub(world, leagueId, new ClubDefinition
            {
                Slug = "independente-bruma",
                Name = "Independente de Bruma",
                ShortName = "Bruma",
                City = "Bruma",
                Stadium = "Campo da Bruma",
                FoundedYear = 1978,
                History = "Clube independente, fundado por sócios que se recusaram a fundir com o vizinho. Vive dessa memória e de um orgulho teimoso.",
                PublicReputation = "Pequeno, orgulhoso, difícil de convencer a mudar.",
                Reputation = 34,
                Finance = Fin(120000, 36000, 8000, "O clube vive no limite. Qualquer decisão tem de ser explicada aos sócios."),
                Tactics = Identity("4-5-1", "Pressão baixa", "Construção longa", "Laterais defensivos", "Linha baixa", "Francisco Bruma"),
                BoardObjectives = Obj("Evitar a descida", "Respeitar a história do clube", "Não gastar o que não se tem"),
                DressingRoomAtmosphere = "Desconfiado com estranhos.",
                PublicTeamState = "O plantel é curto e conhece-se de trás para a frente. Um treinador de fora será sempre posto à prova.",
                HiddenInternalProblems = "Há um núcleo de veteranos que decide mais do que o organograma sugere.",
                PublicCoachVacancyNote = "Francisco Bruma deixou o cargo após seis épocas, por decisão da assembleia.",
                HiddenDismissalTruth = "A assembleia usou os resultados como pretexto. O verdadeiro tema foi a recusa de Bruma em promover o filho de um dirigente.",
                DressingRoomTension = 67,
                Staff = Staff("independente-bruma", "Carlos Nevoeiro", "Mário Cerro", "Sónia Bruma", "Rui Nortada"),
                Players = new[]
                {
                    "1|GR|Paulo Nevoeiro|34|R", "12|GR|Eduardo Cerro|23|R",
                    "2|LD|Nuno Bruma|29|R", "4|DC|Hélder Nortada|32|R|C", "5|DC|Bruno Cerro|27|R", "6|LE|Ricardo Nevoeiro|28|L",
                    "8|MC|Tiago Bruma|31|R", "10|MC|André Cerro|26|R|V", "7|ED|Wilson Nortada|25|R",
                    "11|EE|Jorge Bruma|30|L", "9|PL|Sérgio Nevoeiro|33|R", "19|PL|Rafael Cerro|22|R",
                    "14|MDC|Lucas Nortada|21|R", "15|LD|Ivo Bruma|23|R", "16|LE|Pedro Cerro|20|L",
                    "18|MC|Daniel Nevoeiro|24|R", "21|ED|Kevin Bruma|19|R", "3|DC|Vítor Nortada|24|R"
                },
                Relations = new[] { "4:10:-22", "4:8:40", "9:19:-14" }
            });
        }

        static ClubFinance Fin(int balance, int wages, int transfers, string publicText)
        {
            return new ClubFinance
            {
                Balance = balance,
                WageBudget = wages,
                TransferBudget = transfers,
                PublicSituation = publicText
            };
        }

        static List<string> Obj(params string[] items) => new List<string>(items);

        static TacticalIdentity Identity(string formation, string pressing, string buildUp, string fullBacks, string line, string coach)
        {
            return new TacticalIdentity
            {
                Formation = formation,
                Pressing = pressing,
                BuildUp = buildUp,
                FullBacks = fullBacks,
                DefensiveLine = line,
                FormerCoachName = coach,
                Summary = $"{formation} · {pressing} · {buildUp} · {fullBacks} · {line}"
            };
        }

        static List<StaffMember> Staff(string slug, string assistant, string gk, string fitness, string physio, int reputation = 74)
        {
            var clubId = Ids.Club(slug);
            var list = new List<StaffMember>
            {
                new StaffMember { Id = Ids.Staff(slug, "adjunto"), ClubId = clubId, FullName = StaffName(assistant, slug, 0), Role = StaffRole.AssistantCoach },
                new StaffMember { Id = Ids.Staff(slug, "gr"), ClubId = clubId, FullName = StaffName(gk, slug, 1), Role = StaffRole.GoalkeeperCoach },
                new StaffMember { Id = Ids.Staff(slug, "fisico"), ClubId = clubId, FullName = StaffName(fitness, slug, 2), Role = StaffRole.FitnessCoach },
                new StaffMember { Id = Ids.Staff(slug, "fisio"), ClubId = clubId, FullName = StaffName(physio, slug, 3), Role = StaffRole.Physiotherapist },
                new StaffMember { Id = Ids.Staff(slug, "olheiro"), ClubId = clubId, FullName = StaffName(FaceBook.PersonName(StaffNation(slug), Hash(slug + ":olheiro")), slug, 4), Role = StaffRole.Scout }
            };
            var floor = reputation >= 70 ? 13 : 10;
            foreach (var member in list)
                member.Judgement = floor + Hash(member.FullName + member.Role) % 5;
            return list;
        }

        static string StaffName(string fullName, string clubSlug, int roleIndex)
        {
            var neutralFirstNames = new[] { "Alex", "Gabi", "Dani", "Sam", "Noa", "Ariel", "Cris", "Niki", "Robin", "Luca", "Sasha", "Eli", "Noor", "Rory" };
            var source = string.IsNullOrWhiteSpace(fullName) ? "" : fullName.Trim();
            var separator = source.LastIndexOf(' ');
            var familyName = separator >= 0 ? source.Substring(separator + 1) : source;
            var seed = Hash((clubSlug ?? "") + ":staff:" + roleIndex);
            var givenName = neutralFirstNames[(seed & 0x7fffffff) % neutralFirstNames.Length];
            return string.IsNullOrEmpty(familyName) ? givenName : givenName + " " + familyName;
        }

        static void AddClub(WorldState world, string leagueId, ClubDefinition def)
        {
            var clubId = Ids.Club(def.Slug);
            var kit = KitSpec(def.Slug);
            var club = new Club
            {
                Id = clubId,
                LeagueId = leagueId,
                Name = def.Name,
                ShortName = def.ShortName,
                City = def.City,
                Stadium = def.Stadium,
                FoundedYear = def.FoundedYear,
                History = def.History,
                PublicReputation = def.PublicReputation,
                Reputation = def.Reputation,
                KitSlug = def.Slug,
                JerseyPrimaryHex = kit.Primary,
                JerseySecondaryHex = kit.Secondary,
                JerseyShortsHex = kit.Shorts,
                KitPattern = kit.Pattern,
                Finance = def.Finance,
                Tactics = def.Tactics,
                BoardObjectives = def.BoardObjectives,
                DressingRoomAtmosphere = def.DressingRoomAtmosphere,
                PublicTeamState = def.PublicTeamState,
                HiddenInternalProblems = def.HiddenInternalProblems,
                PublicCoachVacancyNote = def.PublicCoachVacancyNote,
                HiddenDismissalTruth = def.HiddenDismissalTruth,
                DressingRoomTension = def.DressingRoomTension,
                Staff = def.Staff
            };

            var numberToId = new Dictionary<int, string>();
            foreach (var raw in def.Players)
            {
                var player = ParsePlayer(clubId, def.Slug, raw, def.Reputation, leagueId);
                world.Players.Add(player.Id, player);
                club.PlayerIds.Add(player.Id);
                numberToId[player.SquadNumber] = player.Id;
            }

            foreach (var rel in def.Relations)
            {
                var bits = rel.Split(':');
                club.Relations.Add(new PlayerRelation
                {
                    PlayerAId = numberToId[int.Parse(bits[0])],
                    PlayerBId = numberToId[int.Parse(bits[1])],
                    Affinity = int.Parse(bits[2])
                });
            }

            while (club.PlayerIds.Count < PlayersPerClub)
                AddSquadPlayer(world, club, def.Slug, def.Reputation);

            world.Clubs.Add(club.Id, club);
        }

        static void AddSquadPlayer(WorldState world, Club club, string slug, int reputation)
        {
            var number = 20;
            while (number < 100 && ContainsSquadNumber(world, club, number))
                number++;
            if (number >= 100)
                return;

            var index = club.PlayerIds.Count;
            var extraPositions = new[] { "DC", "MDC", "GR", "MC", "PL", "LD", "LE" };
            var position = extraPositions[(index - 18 + extraPositions.Length) % extraPositions.Length];
            var age = 18 + index % 7;
            var foot = index % 3 == 0 ? "L" : "R";
            var raw = number + "|" + position + "|" + slug + ":squad:" + index + "|" + age + "|" + foot;
            var player = ParsePlayer(club.Id, slug, raw, reputation, club.LeagueId);
            world.Players.Add(player.Id, player);
            club.PlayerIds.Add(player.Id);
        }

        static bool ContainsSquadNumber(WorldState world, Club club, int number)
        {
            foreach (var id in club.PlayerIds)
            {
                if (world.Players.TryGetValue(id, out var player) && player.SquadNumber == number)
                    return true;
            }

            return false;
        }

        public static void PaintClub(Club club)
        {
            if (club == null || string.IsNullOrEmpty(club.KitSlug))
                return;
            var kit = KitSpec(club.KitSlug);
            club.JerseyPrimaryHex = kit.Primary;
            club.JerseySecondaryHex = kit.Secondary;
            club.JerseyShortsHex = kit.Shorts;
            club.KitPattern = kit.Pattern;
        }

        static KitLook KitSpec(string slug)
        {
            switch (slug)
            {
                case "norte-maritimo": return new KitLook("#1769AA", "#F4F4F0", "#0E3F6A", KitPattern.HorizontalHoops);
                case "sporting-colina": return new KitLook("#16804A", "#F6F1D5", "#0F4C2C", KitPattern.VerticalStripes);
                case "uniao-valverde": return new KitLook("#F4F1E8", "#C93036", "#C93036", KitPattern.ChestBand);
                case "atletico-ribeira": return new KitLook("#C92A35", "#172A4B", "#172A4B", KitPattern.VerticalStripes);
                case "real-monte-alto": return new KitLook("#E5B627", "#1C3053", "#1C3053", KitPattern.Sash);
                case "academica-prado": return new KitLook("#242424", "#F2F0E8", "#1A1A1A", KitPattern.Solid);
                case "estrela-alcantara": return new KitLook("#C13B46", "#F4E8D8", "#F4E8D8", KitPattern.VerticalStripes);
                case "ferroviario": return new KitLook("#781D37", "#E4C7B5", "#4A1222", KitPattern.HorizontalHoops);
                case "serra-nova": return new KitLook("#327649", "#E9E3C2", "#E9E3C2", KitPattern.Solid);
                case "portuarios": return new KitLook("#267AA8", "#E7A03A", "#E7A03A", KitPattern.Halves);
                case "laranjais": return new KitLook("#E87825", "#193C66", "#193C66", KitPattern.Solid);
                case "vila-do-rio": return new KitLook("#35A9B2", "#F4EBDD", "#F4EBDD", KitPattern.ChestBand);
                case "sc-penhas": return new KitLook("#E2C32E", "#245B40", "#245B40", KitPattern.VerticalStripes);
                case "independente-bruma": return new KitLook("#737D89", "#202A3B", "#202A3B", KitPattern.Pinstripe);
                default: return GeneratedKit(slug);
            }
        }

        static readonly string[] ForeignPrimaries =
        {
            "#D62828", "#2255D7", "#149447", "#F2C230",
            "#F07818", "#7D35BA", "#20BCD1", "#20232A",
            "#F0F0EA", "#D7448C", "#00695C", "#13295D",
            "#795548", "#A6C82A", "#7E1B27", "#9AA2AE",
            "#00B8A9", "#536D1E", "#E65932", "#36454F",
            "#43A047", "#B085C8", "#F0A830", "#4D7A91",
            "#FF6A00", "#FFD000", "#7CFF00", "#00C853",
            "#00B0FF", "#2962FF", "#6200EA", "#FF1744",
            "#FF4081", "#8D6E63", "#C6A15B", "#B71C1C",
            "#E65100", "#F9A825", "#2E7D32", "#01579B",
            "#1A237E", "#6A1B9A", "#880E4F", "#FF8A65",
            "#69F0AE", "#18FFFF", "#82B1FF", "#EA80FC"
        };

        static KitLook GeneratedKit(string slug)
        {
            var slot = KitSlot(slug);
            if (slot < 0)
                slot = 0;
            var primary = ForeignPrimaries[slot % ForeignPrimaries.Length];
            ColorUtility.TryParseHtmlString(primary, out var primaryColor);
            var luminance = primaryColor.r * 0.2126f + primaryColor.g * 0.7152f + primaryColor.b * 0.0722f;
            var secondary = luminance < 0.42f ? "#F4F1E8" : "#12151C";
            var patterns = new[]
            {
                KitPattern.Solid,
                KitPattern.VerticalStripes,
                KitPattern.HorizontalHoops,
                KitPattern.ChestBand,
                KitPattern.Sash,
                KitPattern.Halves,
                KitPattern.Pinstripe
            };
            var shorts = secondary;
            return new KitLook(primary, secondary, shorts, patterns[slot % patterns.Length]);
        }

        static int KitSlot(string slug)
        {
            if (string.IsNullOrEmpty(slug))
                return 0;
            var dash = slug.LastIndexOf('-');
            var index = 0;
            if (dash >= 0)
            {
                var suffix = slug.Substring(dash + 1);
                if (suffix.StartsWith("x", StringComparison.OrdinalIgnoreCase))
                    suffix = suffix.Substring(1);
                int.TryParse(suffix, out index);
            }
            if (index < 0)
                index = 0;
            return index;
        }

        readonly struct KitLook
        {
            public readonly string Primary;
            public readonly string Secondary;
            public readonly string Shorts;
            public readonly KitPattern Pattern;

            public KitLook(string primary, string secondary, string shorts, KitPattern pattern)
            {
                Primary = primary;
                Secondary = secondary;
                Shorts = shorts;
                Pattern = pattern;
            }
        }

        static Player ParsePlayer(string clubId, string slug, string raw, int clubReputation, string leagueId)
        {
            var bits = raw.Split('|');
            var number = int.Parse(bits[0]);
            var seed = Hash($"{slug}:{number}:{bits[2]}");
            var nationality = FaceBook.NationForClub(slug, seed, clubReputation, leagueId);
            var player = new Player
            {
                Id = Ids.Player(slug, number),
                ClubId = clubId,
                FullName = FaceBook.PersonName(nationality, seed),
                Age = int.Parse(bits[3]),
                SquadNumber = number,
                Position = ParsePosition(bits[1]),
                PreferredFoot = bits[4] == "L" ? PreferredFoot.Left : bits[4] == "B" ? PreferredFoot.Both : PreferredFoot.Right,
                Nationality = nationality,
                IsCaptain = bits.Length > 5 && bits[5] == "C",
                IsViceCaptain = bits.Length > 5 && bits[5] == "V",
                Morale = Range(seed, 38, 86),
                Fatigue = Range(seed >> 3, 6, 24),
                Fitness = Range(seed >> 5, 62, 94),
                Form = Range(seed >> 7, 40, 78),
                Motivation = Range(seed >> 9, 45, 88),
                HeightCm = Range(seed >> 11, ParsePosition(bits[1]) == PlayerPosition.Goalkeeper || ParsePosition(bits[1]) == PlayerPosition.CentreBack ? 181 : 167, ParsePosition(bits[1]) == PlayerPosition.Goalkeeper || ParsePosition(bits[1]) == PlayerPosition.CentreBack ? 198 : 190),
                WeakFootQuality = Range(seed >> 13, 1, 5),
                TrainingRating = Range(seed >> 15, 58, 88),
                Injury = new InjuryState(),
                Personality = (PersonalityTrait)(seed % 7),
                Attributes = PlayerAttributes.Roll(seed, ParsePosition(bits[1]), clubReputation),
                Duty = IndividualOrders.For(ParsePosition(bits[1]), seed)
            };
            player.WeightKg = Range(seed >> 17, System.Math.Max(58, player.HeightCm - 112), System.Math.Max(68, player.HeightCm - 94));
            PlayerPositionProfile.Assign(player, seed);
            PlayerTraits.Assign(player, seed);
            return player;
        }

        static PlayerPosition ParsePosition(string code)
        {
            switch (code)
            {
                case "GR": return PlayerPosition.Goalkeeper;
                case "LD": return PlayerPosition.RightBack;
                case "DC": return PlayerPosition.CentreBack;
                case "LE": return PlayerPosition.LeftBack;
                case "MDC": return PlayerPosition.DefensiveMidfielder;
                case "MC": return PlayerPosition.CentralMidfielder;
                case "MO": return PlayerPosition.AttackingMidfielder;
                case "ED": return PlayerPosition.RightWinger;
                case "EE": return PlayerPosition.LeftWinger;
                case "PL": return PlayerPosition.Striker;
                default: return PlayerPosition.CentralMidfielder;
            }
        }

        static void PadLeague(WorldState world, string leagueId)
        {
            var slug = leagueId.StartsWith("league:") ? leagueId.Substring(7) : leagueId;
            var target = TargetClubs(slug);
            if (target < 1)
                return;
            var have = 0;
            foreach (var club in world.Clubs.Values)
            {
                if (club.LeagueId == leagueId)
                    have++;
            }

            var country = LeagueNationality(slug);
            var tier = DivisionTier(slug);
            var first = tier == LeagueTier.First;
            for (var i = have; i < target; i++)
            {
                var clubSlug = slug + "-x" + i;
                var name = UniqueClubName(world, leagueId, ExtraName(slug, i));
                var reputation = first ? 58 + (target - i) : 43 + (target - i);
                AddClub(world, leagueId, new ClubDefinition
                {
                    Slug = clubSlug,
                    Name = name,
                    ShortName = name,
                    City = country,
                    Stadium = "Estádio de " + name,
                    FoundedYear = 1904 + i * 3,
                    History = name + " entra na " + DivisionLabel(tier).ToLowerInvariant() + " de " + country + ".",
                    PublicReputation = first ? "Clube da primeira divisão." : "Clube a lutar pela subida.",
                    Reputation = reputation,
                    Finance = Fin(first ? 2800000 : 900000, first ? 110000 : 48000, first ? 400000 : 120000, "As contas acompanham o tamanho do clube."),
                    Tactics = Identity(i % 2 == 0 ? "4-3-3" : "4-4-2", "Pressão média", "Construção curta", "Laterais equilibrados", "Linha média", "Treinador anterior"),
                    BoardObjectives = Obj("Pontuar com regularidade", "Manter o balneário calmo"),
                    DressingRoomAtmosphere = "O grupo espera um plano claro.",
                    PublicTeamState = "A pré-época ainda não mostrou o verdadeiro nível.",
                    HiddenInternalProblems = "Há um ou outro desentendimento, nada que feche o balneário.",
                    PublicCoachVacancyNote = "O banco está vago.",
                    HiddenDismissalTruth = "O anterior saiu por resultados e por falta de plano.",
                    DressingRoomTension = 28 + i,
                    Staff = Staff(clubSlug,
                        FaceBook.PersonName(country, Hash(clubSlug + ":adjunto")),
                        FaceBook.PersonName(country, Hash(clubSlug + ":gr")),
                        FaceBook.PersonName(country, Hash(clubSlug + ":fisico")),
                        FaceBook.PersonName(country, Hash(clubSlug + ":fisio")),
                        reputation),
                    Players = Squad(clubSlug),
                    Relations = new string[0]
                });
            }

            foreach (var league in world.Leagues)
            {
                if (league.Id != leagueId)
                    continue;
                league.ClubCount = target;
                break;
            }
        }

        static int TargetClubs(string slug)
        {
            switch (slug)
            {
                case "atlas":
                case "horizonte": return 18;
                case "inglesa": return 20;
                case "inglesa-championship":
                case "inglesa-league-one":
                case "inglesa-2": return 24;
                case "francesa":
                case "bundesliga":
                case "eredivisie": return 18;
                case "brasileira":
                case "espanhola": return 20;
                case "espanhola-2": return 22;
                default: return 0;
            }
        }

        static string LeagueNationality(string slug)
        {
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

        static LeagueTier DivisionTier(string slug)
        {
            switch (slug)
            {
                case "horizonte":
                case "inglesa-championship":
                case "espanhola-2": return LeagueTier.Second;
                case "inglesa-league-one": return LeagueTier.Third;
                case "inglesa-2": return LeagueTier.Fourth;
                default: return LeagueTier.First;
            }
        }

        static string ExtraName(string slug, int index)
        {
            string[] bank;
            if (slug.StartsWith("inglesa"))
                bank = new[] { "Stonebridge", "Fairhaven", "East Wharf", "Lockside", "Moorfield", "Queenswell", "Blackwater", "Highgate", "South Pier", "New Alden", "Westmere", "Kingswell", "Red Docks", "Old Market", "Hilltown", "River End", "Coal Lane", "Lowgate", "Severn Vale", "Northgate", "Ashcombe", "Wickford", "Lakeside", "Bramble" };
            else if (slug.StartsWith("francesa"))
                bank = new[] { "Bellevue", "Saint-Marc", "Port-Neuf", "Les Ormes", "Cap Ouest", "Ville Haute", "Quai Blanc", "Roche Claire", "Bois Vert", "Anse", "Val Fleuri", "Mont-Rouge" };
            else if (slug.StartsWith("espanhola"))
                bank = new[] { "Villa Sol", "Puerto Nuevo", "Los Pinos", "Ribera Alta", "Monte Claro", "Campo Sur", "La Caleta", "Oliva", "Travesía Norte", "Bahía Chica", "Valle Blanco", "San Telmo", "Costa Verde", "Las Eras" };
            else if (slug == "bundesliga")
                bank = new[] { "Waldheim", "Neustadt", "Rheintor", "Hohenfeld", "Eichen", "Sonnenbruck", "Nordhafen", "Kronberg", "Talburg", "Eisenstadt", "Lindenau", "Falken", "Grunwald", "Westmark", "Bergstadt", "Seeburg", "Altenbruck", "Rosenheim" };
            else if (slug == "eredivisie")
                bank = new[] { "Noordhaven", "Rijnstad", "Zilverdam", "Waterwijk", "Oostpoort", "Veldhoven", "Duinrade", "Nieuwdam", "Westerveld", "Havenburg", "Groenendaal", "Meerstad", "Kasteelwijk", "Zuiddam", "Rietlanden", "Bergzicht", "Lindehof", "Veenstad" };
            else if (slug == "brasileira")
                bank = new[] { "Aurora do Sul", "Atlético de Campinas", "União de Belém", "Porto Dourado", "Estrela de Recife", "Vila Nova do Norte", "Ferroviário Central", "Real de Goiânia", "Costa Verde", "Metropolitano", "São Bento do Vale", "Nacional de Manaus", "Independente do Recife", "Juventude da Serra", "América do Litoral", "Operário do Oeste", "Ceará do Sul", "Ponte do Sol", "Mar Azul", "Vitória do Cerrado" };
            else
                bank = new[] { "Atlético da Ribeira", "União do Cais", "Sporting da Serra", "Nacional do Oeste", "Oriental", "Real das Dunas", "Racing do Vale", "Olímpico", "Estrela do Sul", "Vitória da Foz", "Académica de Santa Clara", "Desportivo de Valongo", "Fabril da Costa", "Clube do Pinhal", "Atlético de Viseu", "União de Alvor", "Sporting de Monção", "Estrela do Mondego" };
            var name = bank[index % bank.Length];
            return index < bank.Length ? name : name + " " + (index / bank.Length + 2);
        }

        static string UniqueClubName(WorldState world, string leagueId, string candidate)
        {
            var name = candidate;
            var suffix = 2;
            while (HasClubName(world, leagueId, name))
                name = candidate + " " + suffix++;
            return name;
        }

        static bool HasClubName(WorldState world, string leagueId, string name)
        {
            foreach (var club in world.Clubs.Values)
            {
                if (club.LeagueId == leagueId && string.Equals(club.Name, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        static void AddGeneratedDivision(WorldState world, string leagueId)
        {
            var slug = leagueId.StartsWith("league:") ? leagueId.Substring(7) : leagueId;
            var names = DivisionNames(slug);
            var tier = DivisionTier(slug);
            var first = tier == LeagueTier.First;
            var country = LeagueNationality(slug);
            for (var i = 0; i < names.Length; i++)
            {
                var clubSlug = slug + "-" + i;
                var reputation = first ? 78 - i : tier == LeagueTier.Second ? 58 - i / 2 : tier == LeagueTier.Third ? 49 - i / 2 : 42 - i / 2;
                var name = UniqueClubName(world, leagueId, names[i]);
                AddClub(world, leagueId, new ClubDefinition
                {
                    Slug = clubSlug,
                    Name = name,
                    ShortName = name,
                    City = country,
                    Stadium = "Estádio de " + name,
                    FoundedYear = 1898 + i * 7,
                    History = name + " disputa a " + DivisionLabel(tier).ToLowerInvariant() + " de " + country + ".",
                    PublicReputation = first ? "Clube da primeira divisão." : "Clube a lutar pela subida.",
                    Reputation = reputation,
                    Finance = Fin(first ? 5000000 - i * 180000 : tier == LeagueTier.Second ? 2200000 - i * 60000 : 1200000 - i * 35000, first ? 160000 : tier == LeagueTier.Second ? 85000 : 52000, first ? 900000 : tier == LeagueTier.Second ? 320000 : 180000, "As contas acompanham o tamanho do clube."),
                    Tactics = Identity(i % 2 == 0 ? "4-3-3" : "4-4-2", "Pressão média", "Construção curta", "Laterais equilibrados", "Linha média", "Treinador anterior"),
                    BoardObjectives = Obj("Pontuar com regularidade", "Manter o balneário calmo"),
                    DressingRoomAtmosphere = "O grupo espera um plano claro.",
                    PublicTeamState = "A pré-época ainda não mostrou o verdadeiro nível.",
                    HiddenInternalProblems = "Há um ou outro desentendimento, nada que feche o balneário.",
                    PublicCoachVacancyNote = "O banco está vago.",
                    HiddenDismissalTruth = "O anterior saiu por resultados e por falta de plano.",
                    DressingRoomTension = 30 + i * 3,
                    Staff = Staff(clubSlug,
                        FaceBook.PersonName(country, Hash(clubSlug + ":adjunto")),
                        FaceBook.PersonName(country, Hash(clubSlug + ":gr")),
                        FaceBook.PersonName(country, Hash(clubSlug + ":fisico")),
                        FaceBook.PersonName(country, Hash(clubSlug + ":fisio")),
                        reputation),
                    Players = Squad(clubSlug),
                    Relations = new string[0]
                });
            }
        }

        static string[] DivisionNames(string slug)
        {
            var count = TargetClubs(slug);
            var names = new string[count];
            if (slug == "inglesa-championship" || slug == "inglesa-league-one")
            {
                var towns = new[] { "Stonebridge", "Fairhaven", "East Wharf", "Lockside", "Moorfield", "Queenswell", "Blackwater", "Highgate", "South Pier", "New Alden", "Westmere", "Kingswell", "Red Docks", "Old Market", "Hilltown", "River End", "Coal Lane", "Lowgate", "Severn Vale", "Northgate", "Ashcombe", "Wickford", "Lakeside", "Bramble" };
                var suffix = slug == "inglesa-championship" ? "Town" : "United";
                for (var i = 0; i < count; i++)
                    names[i] = towns[i] + " " + suffix;
                return names;
            }

            string[] baseNames;
            switch (slug)
            {
                case "inglesa": baseNames = new[] { "Northbridge", "Royal Wharf", "Millford", "Ashfield", "Crown Park", "Severn", "Harbour", "Port Albion" }; break;
                case "inglesa-2": baseNames = new[] { "Red Docks", "Old Market", "Hilltown", "River End", "Coal Lane", "Westmere", "Kingswell", "Lowgate" }; break;
                case "francesa": baseNames = new[] { "Nord Lumière", "Saint-Rive", "Montclair", "Port-Cèdre", "Val d'Or", "Roche Bleue", "Cap Levant", "Atelier" }; break;
                case "espanhola": baseNames = new[] { "Costa Real", "Monte Sol", "Río Alto", "Puerto Sur", "Villa Norte", "Sierra", "Bahía", "Campo Viejo" }; break;
                case "espanhola-2": baseNames = new[] { "Río Chico", "Pueblo", "La Cuesta", "Mar Menor", "Olivares", "Travesía", "Altozano", "Ribera" }; break;
                default: baseNames = new string[0]; break;
            }

            for (var i = 0; i < count; i++)
                names[i] = i < baseNames.Length ? baseNames[i] : ExtraName(slug, i - baseNames.Length);
            return names;
        }

        static string[] Squad(string slug)
        {
            return new[]
            {
                "1|GR|" + slug + "0|29|R", "12|GR|" + slug + "1|22|R",
                "2|LD|" + slug + "2|26|R", "3|DC|" + slug + "3|30|R|C", "4|DC|" + slug + "4|27|R", "5|LE|" + slug + "5|25|L",
                "6|MDC|" + slug + "6|28|R", "8|MC|" + slug + "7|24|R", "10|MO|" + slug + "8|23|R",
                "7|ED|" + slug + "9|26|R", "11|EE|" + slug + "10|24|L", "9|PL|" + slug + "11|29|R",
                "14|MC|" + slug + "12|21|R", "15|DC|" + slug + "13|22|R", "16|PL|" + slug + "14|20|R",
                "17|LD|" + slug + "15|23|R", "18|LE|" + slug + "16|22|L", "19|MC|" + slug + "17|19|R"
            };
        }

        static string StaffNation(string slug)
        {
            if (string.IsNullOrEmpty(slug))
                return "Portugal";
            if (slug.StartsWith("francesa"))
                return "França";
            if (slug.StartsWith("espanhola"))
                return "Espanha";
            if (slug.StartsWith("inglesa"))
                return "Inglaterra";
            return LeagueNationality(slug);
        }

        static int Hash(string value)
        {
            unchecked
            {
                var hash = 23;
                foreach (var c in value)
                    hash = hash * 31 + c;
                return hash & 0x7fffffff;
            }
        }

        static int Range(int seed, int min, int max)
        {
            return min + seed % (max - min + 1);
        }

        sealed class ClubDefinition
        {
            public string Slug;
            public string Name;
            public string ShortName;
            public string City;
            public string Stadium;
            public int FoundedYear;
            public string History;
            public string PublicReputation;
            public int Reputation;
            public ClubFinance Finance;
            public TacticalIdentity Tactics;
            public List<string> BoardObjectives;
            public string DressingRoomAtmosphere;
            public string PublicTeamState;
            public string HiddenInternalProblems;
            public string PublicCoachVacancyNote;
            public string HiddenDismissalTruth;
            public int DressingRoomTension;
            public List<StaffMember> Staff;
            public string[] Players;
            public string[] Relations;
        }
    }
}
