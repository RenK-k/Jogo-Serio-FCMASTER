using System;
using System.Collections.Generic;
using CoachMaster.Career;
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
                Name = "Porto FC",
                ShortName = "Porto",
                City = "Porto",
                Stadium = "Estádio do Dragão Azul",
                FoundedYear = 1894,
                History = "O dragão do norte. Clube grande, europeu, com o peso de quem discute o título todos os anos.",
                PublicReputation = "Gigante do norte. O título é a medida.",
                Reputation = 90,
                KitSlug = "marca:Porto FC",
                Finance = Fin(16000000, 620000, 9000000, "O cofre de um grande. A direção exige noites europeias."),
                Tactics = Identity("4-3-3", "Pressão alta", "Construção curta", "Laterais ofensivos", "Linha defensiva alta", "Hélder Paiva"),
                BoardObjectives = Obj("Lutar pelo título", "Manter o peso europeu", "Integrar dois jogadores da formação"),
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
                Name = "SP Lisboa FC",
                ShortName = "SP Lisboa",
                City = "Lisboa",
                Stadium = "Estádio do Leão",
                FoundedYear = 1942,
                History = "O leão de Lisboa. Casa de formação, verde e dourado, e um dos três grandes do país.",
                PublicReputation = "Grande de Lisboa. A formação é a identidade e o título é a exigência.",
                Reputation = 88,
                KitSlug = "marca:SP Lisboa FC",
                Finance = Fin(14000000, 560000, 7500000, "Contas de grande, com a formação como regra da casa."),
                Tactics = Identity("4-2-3-1", "Pressão média", "Construção curta", "Laterais equilibrados", "Linha média", "Vítor Gama"),
                BoardObjectives = Obj("Disputar o título", "Manter a formação como prioridade", "Não abdicar do meio-campo"),
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
                Name = "Vila do Conde FC",
                ShortName = "V. Conde",
                City = "Vila do Conde",
                Stadium = "Estádio da Fénix",
                FoundedYear = 1995,
                History = "A fénix de Vila do Conde. Clube da primeira divisão, sem o cofre dos três grandes, mas difícil de ignorar em casa.",
                PublicReputation = "Clube estabelecido da primeira. A manutenção com ambição chega.",
                Reputation = 66,
                KitSlug = "marca:Vila do Conde FC",
                Finance = Fin(2800000, 130000, 450000, "Orçamento de clube médio. Cada contratação pesa."),
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
                Name = "SL Lisboa FC",
                ShortName = "SL Lisboa",
                City = "Lisboa",
                Stadium = "Estádio da Águia",
                FoundedYear = 1998,
                History = "A águia de Lisboa. O clube com mais peso do país, vermelho e branco, habituado a medir a época pelo título.",
                PublicReputation = "O maior clube de Portugal. O título é obrigação.",
                Reputation = 91,
                KitSlug = "marca:SL Lisboa FC",
                Finance = Fin(18000000, 680000, 10000000, "O cofre do maior. A direção não aceita uma época sem título."),
                Tactics = Identity("3-4-3", "Pressão alta", "Construção curta", "Ala ofensivos", "Linha alta", "Sofia Menezes"),
                BoardObjectives = Obj("Lutar pelo título", "Manter o plantel de grande", "Afirmar a águia na Europa"),
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
            foreach (var member in list)
                member.Judgement = StaffProfile.Spread(member.Id, member.Role, reputation);
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
            var kitId = string.IsNullOrEmpty(def.KitSlug) ? def.Slug : def.KitSlug;
            var kit = KitSpec(kitId);
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
                KitSlug = kitId,
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
            if (!string.IsNullOrEmpty(slug) && slug.StartsWith("mod:"))
            {
                var parts = slug.Split(':');
                if (parts.Length >= 4)
                    return new KitLook(parts[1], parts[2], parts[3], KitPattern.Solid);
            }
            if (TryBrandKit(slug, out var branded))
                return branded;
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
            var shorts = ForeignPrimaries[(slot + 11) % ForeignPrimaries.Length];
            if (shorts == primary || shorts == secondary)
                shorts = ForeignPrimaries[(slot + 17) % ForeignPrimaries.Length];
            if (shorts == primary || shorts == secondary)
                shorts = luminance < 0.42f ? "#C4A35A" : "#1B3A4B";
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
            var overall = PlayerAttributes.Overall(player);
            var reputationNoise = (int)(seed % 19) - 9;
            var peakAllowance = player.Age >= 29 ? 8 : player.Age >= 25 ? 4 : 0;
            player.Reputation = Mathf.Clamp(Mathf.RoundToInt(overall * 0.58f + clubReputation * 0.32f + reputationNoise + peakAllowance), 5, 100);
            var wageFactor = Mathf.Clamp(clubReputation / 55f, 0.4f, 2.8f);
            player.WeeklyWage = Mathf.Max(250, Mathf.RoundToInt(12000f * Mathf.Exp(0.105f * (overall - 55)) * wageFactor));
            var contractSeed = System.Math.Abs((long)(seed >> 21));
            var contractYear = 2027 + (int)(contractSeed % 5);
            player.ContractEndTicks = new DateTime(contractYear, 6, 30).Ticks;
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
                var brand = FindBrand(names[i]);
                var reputation = brand != null
                    ? brand.Reputation
                    : first ? 78 - i : tier == LeagueTier.Second ? 58 - i / 2 : tier == LeagueTier.Third ? 49 - i / 2 : 42 - i / 2;
                var name = brand != null ? brand.Name : UniqueClubName(world, leagueId, names[i]);
                var balance = first ? 2500000 + reputation * 100000 : tier == LeagueTier.Second ? 900000 + reputation * 75000 : tier == LeagueTier.Third ? 400000 + reputation * 42000 : 200000 + reputation * 30000;
                var wages = first ? 140000 + reputation * 4500 : tier == LeagueTier.Second ? 55000 + reputation * 2100 : tier == LeagueTier.Third ? 30000 + reputation * 1500 : 18000 + reputation * 1000;
                var transfers = first ? 2500000 + reputation * 250000 : tier == LeagueTier.Second ? 600000 + reputation * 85000 : tier == LeagueTier.Third ? 250000 + reputation * 52000 : 100000 + reputation * 35000;
                if (slug == "inglesa" || (brand != null && brand.Reputation >= 88))
                {
                    balance = 12000000 + reputation * 300000;
                    wages = 500000 + reputation * 9000;
                    transfers = 10000000 + reputation * 450000;
                }
                AddClub(world, leagueId, new ClubDefinition
                {
                    Slug = clubSlug,
                    Name = name,
                    ShortName = brand != null ? brand.Short : name,
                    City = brand != null ? brand.City : country,
                    Stadium = brand != null ? brand.Stadium : "Estádio de " + name,
                    FoundedYear = brand != null ? brand.Year : 1898 + i * 7,
                    History = brand != null ? brand.History : name + " disputa a " + DivisionLabel(tier).ToLowerInvariant() + " de " + country + ".",
                    PublicReputation = brand != null ? brand.Standing : first ? "Clube da primeira divisão." : "Clube a lutar pela subida.",
                    Reputation = reputation,
                    KitSlug = brand != null ? brand.KitId : clubSlug,
                    Finance = Fin(balance, wages, transfers, "As contas acompanham o tamanho do clube."),
                    Tactics = Identity(i % 2 == 0 ? "4-3-3" : "4-4-2", "Pressão média", "Construção curta", "Laterais equilibrados", "Linha média", "Treinador anterior"),
                    BoardObjectives = brand != null && brand.Reputation >= 90
                        ? Obj("Lutar pelo título", "Manter o balneário dos grandes")
                        : brand != null && brand.Reputation >= 80
                        ? Obj("Lutar pelos lugares europeus", "Manter o balneário estável")
                        : Obj("Pontuar com regularidade", "Manter o balneário calmo"),
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
            if (slug == "inglesa" && names.Length >= 20)
            {
                names[0] = "Manchester Sky FC";
                names[1] = "Mersey Reds FC";
                names[2] = "Red Devils FC";
                names[3] = "London Gunners FC";
                names[4] = "Nlondo FC";
                names[5] = "White Lily FC";
                names[6] = "Newcastle FC";
                names[7] = "Aston Lions FC";
                names[8] = "Nottingham Mountains FC";
                names[9] = "Mersey Blues FC";
                names[10] = "Yorkshire FC";
                names[11] = "Brigull FC";
                names[12] = "SL Eagles FC";
                names[13] = "Fulham Whites FC";
                names[14] = "Rerrys FC";
                names[15] = "Wobee FC";
                names[16] = "Wearside FC";
                names[17] = "Suffolk Town FC";
                names[18] = "Coventry Blues FC";
                names[19] = "Hull Tigers FC";
            }
            else if (slug == "bundesliga" && names.Length > 0)
                names[0] = "Bavaria Munich FC";
            else if (slug == "espanhola" && names.Length > 1)
            {
                names[0] = "Real Football Madrid";
                names[1] = "Fútbol Barcelona";
            }
            return names;
        }

        public static string CrestPath(string clubName)
        {
            var brand = FindBrand(clubName);
            return brand == null ? null : brand.Crest;
        }

        static bool TryBrandKit(string kitId, out KitLook look)
        {
            look = default;
            if (string.IsNullOrEmpty(kitId))
                return false;
            foreach (var brand in Brands())
            {
                if (brand.KitId != kitId)
                    continue;
                look = new KitLook(brand.Primary, brand.Secondary, brand.Shorts, brand.Pattern);
                return true;
            }
            return false;
        }

        public static void EnsureThreeColors(Club club)
        {
            if (club == null)
                return;
            if (!SameColor(club.JerseyPrimaryHex, club.JerseySecondaryHex)
                && !SameColor(club.JerseyShortsHex, club.JerseyPrimaryHex)
                && !SameColor(club.JerseyShortsHex, club.JerseySecondaryHex)
                && !string.IsNullOrEmpty(club.JerseyPrimaryHex)
                && !string.IsNullOrEmpty(club.JerseySecondaryHex)
                && !string.IsNullOrEmpty(club.JerseyShortsHex))
                return;
            var kit = GeneratedKit(string.IsNullOrEmpty(club.KitSlug) ? "x-0" : club.KitSlug);
            if (SameColor(club.JerseyPrimaryHex, club.JerseySecondaryHex) || string.IsNullOrEmpty(club.JerseySecondaryHex))
                club.JerseySecondaryHex = SameColor(kit.Secondary, club.JerseyPrimaryHex) ? "#F4F1E8" : kit.Secondary;
            if (SameColor(club.JerseyShortsHex, club.JerseyPrimaryHex) || SameColor(club.JerseyShortsHex, club.JerseySecondaryHex) || string.IsNullOrEmpty(club.JerseyShortsHex))
                club.JerseyShortsHex = SameColor(kit.Shorts, club.JerseyPrimaryHex) || SameColor(kit.Shorts, club.JerseySecondaryHex) ? "#C4A35A" : kit.Shorts;
        }

        static bool SameColor(string a, string b)
        {
            return string.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase);
        }

        public static void RefreshPresentedClub(Club club)
        {
            if (club == null)
                return;
            var brand = FindBrand(club.Name);
            if (brand == null)
                return;
            club.Name = brand.Name;
            club.ShortName = brand.Short;
            club.City = brand.City;
            club.Stadium = brand.Stadium;
            club.FoundedYear = brand.Year;
            club.History = brand.History;
            club.PublicReputation = brand.Standing;
            club.Reputation = brand.Reputation;
            club.KitSlug = brand.KitId;
            PaintClub(club);
        }

        static string CanonicalBrand(string name)
        {
            switch (name)
            {
                case "Manchester Sky Blues": return "Manchester Sky FC";
                case "Mersey Reds": return "Mersey Reds FC";
                case "Manchester Red Devils": return "Red Devils FC";
                case "North London Gunners": return "London Gunners FC";
                case "Blue London FC": return "Nlondo FC";
                case "Newcastle": return "Newcastle FC";
                case "Nottingham Mountains":
                case "Nottingham mountains": return "Nottingham Mountains FC";
                case "Mersey Blues": return "Mersey Blues FC";
                case "Yorkshire Whites": return "Yorkshire FC";
                case "Seagulls FC": return "Brigull FC";
                case "South London Eagles": return "SL Eagles FC";
                case "Fulham Whites": return "Fulham Whites FC";
                case "South Coast Cherries": return "Rerrys FC";
                case "West London Bees": return "Wobee FC";
                case "Wearside Black Cats": return "Wearside FC";
                case "Coventry Sky Blues": return "Coventry Blues FC";
                case "Hull Tigers": return "Hull Tigers FC";
                case "Bavaria Munich": return "Bavaria Munich FC";
                case "FC Norte Marítimo": return "Porto FC";
                case "Sporting da Colina": return "SP Lisboa FC";
                case "Atlético Ribeira": return "SL Lisboa FC";
                case "União de Valverde": return "Vila do Conde FC";
                default: return name;
            }
        }

        static ClubBrand FindBrand(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            name = CanonicalBrand(name);
            foreach (var brand in Brands())
            {
                if (brand.Name == name)
                    return brand;
            }
            return null;
        }

        static ClubBrand[] Brands()
        {
            return new[]
            {
                Brand("Manchester Sky FC", "Sky FC", "Manchester", "Estádio do Céu", 2009, 96,
                    "#6CABDD", "#1C2C5B", "#FFFFFF", KitPattern.Solid, "Clubes/Inglaterra 1/MSFC",
                    "O clube mais rico do país. O céu azul compra plantéis de título.",
                    "Gigante financeiro. O título é a medida do ano."),
                Brand("Mersey Reds FC", "Mersey Reds", "Liverpool", "Estádio do Mersey", 1892, 95,
                    "#C8102E", "#F6EB61", "#FFFFFF", KitPattern.Solid, "Clubes/Inglaterra 1/mersey reds",
                    "A margem vermelha. Europeu de peso, com uma bancada que não perdoa um jogo morno.",
                    "Clube de topo europeu. Exige noites grandes."),
                Brand("Red Devils FC", "Red Devils", "Manchester", "Teatro dos Diabos", 1878, 94,
                    "#DA291C", "#FBE122", "#111111", KitPattern.Solid, "Clubes/Inglaterra 1/redDevils",
                    "O outro gigante de Manchester. Histórico, impaciente e sempre à procura do título.",
                    "Gigante histórico. A direção mede a época pelo título."),
                Brand("London Gunners FC", "Gunners", "Londres", "Castelo do Norte", 1980, 93,
                    "#EF0107", "#FFFFFF", "#9C824A", KitPattern.Solid, "Clubes/Inglaterra 1/London gunners",
                    "O canhão do norte de Londres. Clube grande, organizado e habituado a lutar pelo topo.",
                    "Clube grande. O topo da tabela é a expectativa."),
                Brand("Nlondo FC", "Nlondo", "Londres", "Ponte Azul", 1995, 91,
                    "#034694", "#FFFFFF", "#D1A32A", KitPattern.Solid, "Clubes/Inglaterra 1/NLONDO",
                    "O leão azul do norte de Londres. Dinheiro, estrelas e a obrigação de ganhar taças.",
                    "Clube de estrelas. Taças e lugares de topo."),
                Brand("White Lily FC", "White Lily", "Londres", "Estrada Branca", 1899, 88,
                    "#FFFFFF", "#132257", "#C8102E", KitPattern.Solid, "Clubes/Inglaterra 1/WhiteLily",
                    "O lírio branco do norte de Londres. Ataque bonito e a pressão de voltar ao topo.",
                    "Clube grande. Exige futebol e lugares europeus."),
                Brand("Newcastle FC", "Newcastle", "Newcastle", "Estádio das Riscas", 1892, 86,
                    "#241F20", "#FFFFFF", "#1B4F72", KitPattern.VerticalStripes, "Clubes/Inglaterra 1/newcastle",
                    "A cidade das riscas. Um clube grande do Norte, com bancada cheia e ambição europeia.",
                    "Clube grande do Norte. A Europa é o mínimo."),
                Brand("Aston Lions FC", "Aston Lions", "Birmingham", "Parque dos Leões", 2005, 84,
                    "#670E36", "#95BFE5", "#F5D76E", KitPattern.Solid, "Clubes/Inglaterra 1/aston lions",
                    "O leão de Birmingham. Clube histórico, de grená e azul, a querer voltar às noites grandes.",
                    "Clube estabelecido. Quer a Europa."),
                Brand("Nottingham Mountains FC", "Mountains", "Nottingham", "Cidade da Floresta", 1865, 83,
                    "#E31B23", "#FFFFFF", "#111111", KitPattern.Solid, "Clubes/Inglaterra 1/nottingham mountains",
                    "Um dos clubes mais antigos do país. Já foi europeu e ainda se vê como grande.",
                    "Clube histórico. Não aceita a mediocridade."),
                Brand("Mersey Blues FC", "Mersey Blues", "Liverpool", "Parque Azul", 1878, 81,
                    "#003399", "#FFFFFF", "#F6EB61", KitPattern.Solid, "Clubes/Inglaterra 1/mersley blues",
                    "O azul da outra margem. Clube tradicional, fiel e cansado de viver à sombra do vizinho.",
                    "Clube tradicional. Quer a metade de cima e uma taça."),
                Brand("Yorkshire FC", "Yorkshire", "Leeds", "Estádio de Yorkshire", 1954, 79,
                    "#FFFFFF", "#1D428A", "#FFCD00", KitPattern.Solid, "Clubes/Inglaterra 1/yorkshire",
                    "O branco de Yorkshire. Clube intenso, de bancada hostil, a querer fixar-se no topo.",
                    "Clube de primeira. A estabilidade no topo é o plano."),
                Brand("Brigull FC", "Brigull", "Brighton", "Ninho da Gaivota", 1987, 77,
                    "#0057B8", "#FFFFFF", "#E8C547", KitPattern.VerticalStripes, "Clubes/Inglaterra 1/brigull",
                    "A gaivota da costa sul. Clube moderno, bem gerido, sem o peso de um gigante.",
                    "Clube estável da primeira. Quer crescer sem loucura."),
                Brand("SL Eagles FC", "SL Eagles", "Londres", "Palácio do Sul", 1989, 76,
                    "#1B458F", "#C4122E", "#FFFFFF", KitPattern.VerticalStripes, "Clubes/Inglaterra 1/sl eagles",
                    "A águia do sul de Londres. Clube de bairro que chegou ao topo e quer lá ficar.",
                    "Clube da primeira. Manter-se e assustar os grandes."),
                Brand("Fulham Whites FC", "Fulham", "Londres", "Casa Branca", 1894, 75,
                    "#FFFFFF", "#000000", "#C4A35A", KitPattern.Solid, "Clubes/Inglaterra 1/fulham whites",
                    "O branco de Fulham. Clube de Londres, mais casa do que império.",
                    "Clube de Londres. A permanência tranquila chega."),
                Brand("Rerrys FC", "Rerrys", "Bournemouth", "Costa das Cerejas", 1897, 74,
                    "#DA291C", "#000000", "#FFFFFF", KitPattern.VerticalStripes, "Clubes/Inglaterra 1/Rerrys",
                    "As cerejas da costa sul. Clube pequeno no mapa, teimoso na primeira divisão.",
                    "Clube modesto da primeira. Sobreviver já é época boa."),
                Brand("Wobee FC", "Wobee", "Londres", "A Colmeia", 1975, 73,
                    "#0B3D2E", "#C4A35A", "#F4F1E8", KitPattern.Solid, "Clubes/Inglaterra 1/wobee",
                    "A abelha do oeste de Londres. Orçamento curto, identidade forte.",
                    "Clube combativo. Cada ponto na primeira conta."),
                Brand("Wearside FC", "Wearside", "Sunderland", "Estádio do Gato", 1879, 72,
                    "#111111", "#C5A46E", "#FFFFFF", KitPattern.Solid, "Clubes/Inglaterra 1/wearside",
                    "O gato preto de Wearside. Clube histórico do Nordeste, de novo entre os grandes.",
                    "Clube tradicional. Fixar-se na primeira é o mandato."),
                Brand("Suffolk Town FC", "Suffolk", "Ipswich", "Estádio do Cavalo", 1878, 70,
                    "#0044AA", "#FFFFFF", "#F2C230", KitPattern.Solid, "Clubes/Inglaterra 1/suffolk town",
                    "O cavalo de Suffolk. Clube de província, azul e branco, sem dinheiro de gigante.",
                    "Clube de província. A primeira divisão já é uma conquista."),
                Brand("Coventry Blues FC", "Coventry", "Coventry", "Estádio do Céu Azul", 2012, 68,
                    "#64C8F0", "#1B365D", "#FFFFFF", KitPattern.Solid, "Clubes/Inglaterra 1/coventry blues",
                    "O azul-céu de Coventry. Ainda a aprender o peso da primeira divisão.",
                    "Recém-chegado ao convívio dos grandes. Permanecer."),
                Brand("Hull Tigers FC", "Hull", "Hull", "Estádio do Tigre", 2013, 66,
                    "#F5A623", "#1A1A2E", "#FFFFFF", KitPattern.VerticalStripes, "Clubes/Inglaterra 1/hull tigers",
                    "O tigre do estuário. O clube mais pequeno desta primeira divisão.",
                    "O mais pequeno da primeira. Cada época é uma luta."),
                Brand("Bavaria Munich FC", "Bavaria", "Munique", "Arena da Baviera", 1846, 96,
                    "#DC052D", "#FFFFFF", "#0066B3", KitPattern.Solid, "Clubes/alemanha 1/bavaria munich",
                    "O clube da Baviera. Domina o país há anos e mede-se com os maiores da Europa.",
                    "O grande da Alemanha. O título é obrigação."),
                Brand("Real Football Madrid", "Madrid", "Madrid", "Estádio Real", 1902, 97,
                    "#FFFFFF", "#FEBE10", "#0A3D91", KitPattern.Solid, "Clubes/espanha 1/RF Madrid",
                    "A casa real do futebol. O clube mais pesado do continente, de branco.",
                    "O maior clube deste campeonato. Só o título serve."),
                Brand("Porto FC", "Porto", "Porto", "Estádio do Dragão Azul", 1894, 90,
                    "#0033A0", "#FFFFFF", "#A7C6ED", KitPattern.Solid, "Clubes/portugal 1/Porto",
                    "O dragão do norte. Clube grande, europeu, com o peso de quem discute o título todos os anos.",
                    "Gigante do norte. O título é a medida."),
                Brand("SP Lisboa FC", "SP Lisboa", "Lisboa", "Estádio do Leão", 1942, 88,
                    "#16804A", "#F6F1D5", "#C6A43A", KitPattern.VerticalStripes, "Clubes/portugal 1/SP Lisboa",
                    "O leão de Lisboa. Casa de formação, verde e dourado, e um dos três grandes do país.",
                    "Grande de Lisboa. A formação é a identidade e o título é a exigência."),
                Brand("SL Lisboa FC", "SL Lisboa", "Lisboa", "Estádio da Águia", 1998, 91,
                    "#E10600", "#FFFFFF", "#F0B429", KitPattern.Solid, "Clubes/portugal 1/SL lisboa",
                    "A águia de Lisboa. O clube com mais peso do país, habituado a medir a época pelo título.",
                    "O maior clube de Portugal. O título é obrigação."),
                Brand("Vila do Conde FC", "V. Conde", "Vila do Conde", "Estádio da Fénix", 1995, 66,
                    "#0E3B2E", "#C8102E", "#F5C518", KitPattern.Solid, "Clubes/portugal 1/vila do conde",
                    "A fénix de Vila do Conde. Primeira divisão, sem o cofre dos três grandes.",
                    "Clube estabelecido da primeira. A manutenção com ambição chega."),
                Brand("Fútbol Barcelona", "Barcelona", "Barcelona", "Estádio da Catedral", 1899, 96,
                    "#A50044", "#004D98", "#FFED00", KitPattern.Halves, "Clubes/espanha 1/Barcelona",
                    "O clube da cidade. Blaugrana, identidade forte, o único que discute o trono com Madrid.",
                    "Gigante. O título e a Europa são a conversa."),
            };
        }

        static ClubBrand Brand(
            string name, string shortName, string city, string stadium, int year, int reputation,
            string primary, string secondary, string shorts, KitPattern pattern, string crest,
            string history, string standing)
        {
            return new ClubBrand
            {
                Name = name,
                Short = shortName,
                City = city,
                Stadium = stadium,
                Year = year,
                Reputation = reputation,
                Primary = primary,
                Secondary = secondary,
                Shorts = shorts,
                Pattern = pattern,
                Crest = crest,
                KitId = "marca:" + name,
                History = history,
                Standing = standing
            };
        }

        sealed class ClubBrand
        {
            public string Name;
            public string Short;
            public string City;
            public string Stadium;
            public int Year;
            public int Reputation;
            public string Primary;
            public string Secondary;
            public string Shorts;
            public KitPattern Pattern;
            public string Crest;
            public string KitId;
            public string History;
            public string Standing;
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
            public string KitSlug;
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
