namespace CoachMaster.Domain
{
    public static class TacticPresets
    {
        public static readonly string[] Names =
        {
            "Equilíbrio",
            "Jogo pelas alas",
            "Pressão alta",
            "Contra-ataque",
            "Posse e construção"
        };

        public static void Apply(TacticalIdentity tactics, int index)
        {
            if (tactics == null)
                return;
            if (index < 0 || index >= Names.Length)
                index = 0;
            tactics.Preset = Names[index];
            switch (index)
            {
                case 1:
                    tactics.Pressing = "Pressão média";
                    tactics.DefensiveLine = "Linha defensiva média";
                    tactics.BuildUp = "Jogo misto";
                    tactics.FullBacks = "Laterais sobem";
                    tactics.Width = "Largura larga";
                    tactics.Tempo = "Ritmo normal";
                    tactics.PassingStyle = "Passe misto";
                    tactics.AttackIdea = "Cruzamentos";
                    tactics.Marking = "Marcação mista";
                    break;
                case 2:
                    tactics.Pressing = "Pressão muito alta";
                    tactics.DefensiveLine = "Linha muito alta";
                    tactics.BuildUp = "Construção curta";
                    tactics.FullBacks = "Laterais sobem";
                    tactics.Width = "Largura normal";
                    tactics.Tempo = "Ritmo rápido";
                    tactics.PassingStyle = "Passe arriscado";
                    tactics.AttackIdea = "Equilibrado";
                    tactics.Marking = "Marcação ao homem";
                    break;
                case 3:
                    tactics.Pressing = "Pressão baixa";
                    tactics.DefensiveLine = "Linha defensiva baixa";
                    tactics.BuildUp = "Construção longa";
                    tactics.FullBacks = "Laterais ficam";
                    tactics.Width = "Largura normal";
                    tactics.Tempo = "Ritmo rápido";
                    tactics.PassingStyle = "Passe arriscado";
                    tactics.AttackIdea = "Contra-ataque";
                    tactics.Marking = "Marcação zonal";
                    break;
                case 4:
                    tactics.Pressing = "Pressão média";
                    tactics.DefensiveLine = "Linha defensiva média";
                    tactics.BuildUp = "Construção curta";
                    tactics.FullBacks = "Laterais equilibram";
                    tactics.Width = "Largura normal";
                    tactics.Tempo = "Ritmo lento";
                    tactics.PassingStyle = "Passe seguro";
                    tactics.AttackIdea = "Posse";
                    tactics.Marking = "Marcação zonal";
                    break;
                default:
                    tactics.Pressing = "Pressão média";
                    tactics.DefensiveLine = "Linha defensiva média";
                    tactics.BuildUp = "Jogo misto";
                    tactics.FullBacks = "Laterais equilibram";
                    tactics.Width = "Largura normal";
                    tactics.Tempo = "Ritmo normal";
                    tactics.PassingStyle = "Passe misto";
                    tactics.AttackIdea = "Equilibrado";
                    tactics.Marking = "Marcação mista";
                    break;
            }

            tactics.Summary = tactics.Formation + " · " + tactics.Preset;
        }

        public static int IndexOf(TacticalIdentity tactics)
        {
            var name = Active(tactics);
            for (var i = 0; i < Names.Length; i++)
            {
                if (Names[i] == name)
                    return i;
            }

            return 0;
        }

        public static string Describe(int index)
        {
            switch (index)
            {
                case 1:
                    return "Abre o jogo nas alas. Os laterais sobem, a largura é grande e os cruzamentos procuram a área. A pressão e a linha ficam a meio.";
                case 2:
                    return "Pressão muito alta e linha muito alta. A construção é curta e o ritmo é rápido, para recuperar a bola ainda no meio-campo adversário.";
                case 3:
                    return "Bloco baixo e pouca pressão. A saída é longa. Quando a bola é recuperada, a equipa ataca o espaço em contra-ataque.";
                case 4:
                    return "Posse curta e segura, ritmo lento e laterais equilibrados. A equipa espera o buraco em vez de forçar o remate.";
                default:
                    return "Bloco a meio, pressão média e jogo misto. A equipa tanto constrói como procura a profundidade, sem cair num extremo.";
            }
        }

        public static string Active(TacticalIdentity tactics)
        {
            if (tactics == null)
                return Names[0];
            if (!string.IsNullOrEmpty(tactics.Preset))
                return tactics.Preset;
            var idea = tactics.AttackIdea ?? "";
            var press = tactics.Pressing ?? "";
            var line = tactics.DefensiveLine ?? "";
            if (idea.IndexOf("Cruz", System.StringComparison.OrdinalIgnoreCase) >= 0 || (tactics.Width != null && tactics.Width.Contains("larga") && tactics.FullBacks != null && tactics.FullBacks.Contains("sobem")))
                return Names[1];
            if (press.Contains("muito") && press.Contains("alta") || (line.Contains("muito") && line.Contains("alta")))
                return Names[2];
            if (idea.IndexOf("Contra", System.StringComparison.OrdinalIgnoreCase) >= 0 || idea.IndexOf("direto", System.StringComparison.OrdinalIgnoreCase) >= 0 || idea.IndexOf("longas", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return Names[3];
            if (idea.IndexOf("Posse", System.StringComparison.OrdinalIgnoreCase) >= 0 || idea.IndexOf("Tiki", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return Names[4];
            return Names[0];
        }
    }
}
