using CoachMaster.Domain;

namespace CoachMaster.Presentation
{
    public static class PresentationLabels
    {
        public static string Position(PlayerPosition position)
        {
            switch (position)
            {
                case PlayerPosition.Goalkeeper: return "GR";
                case PlayerPosition.RightBack: return "LD";
                case PlayerPosition.CentreBack: return "DC";
                case PlayerPosition.LeftBack: return "LE";
                case PlayerPosition.DefensiveMidfielder: return "MDC";
                case PlayerPosition.CentralMidfielder: return "MC";
                case PlayerPosition.AttackingMidfielder: return "MO";
                case PlayerPosition.RightWinger: return "ED";
                case PlayerPosition.LeftWinger: return "EE";
                case PlayerPosition.Striker: return "PL";
                default: return "?";
            }
        }

        public static string StaffRole(StaffRole role)
        {
            switch (role)
            {
                case CoachMaster.Domain.StaffRole.AssistantCoach: return "Treinador adjunto";
                case CoachMaster.Domain.StaffRole.GoalkeeperCoach: return "Treinador de guarda-redes";
                case CoachMaster.Domain.StaffRole.FitnessCoach: return "Preparador físico";
                case CoachMaster.Domain.StaffRole.Physiotherapist: return "Fisioterapeuta";
                case CoachMaster.Domain.StaffRole.Scout: return "Olheiro";
                default: return role.ToString();
            }
        }

        public static string Foot(PreferredFoot foot)
        {
            switch (foot)
            {
                case PreferredFoot.Left: return "Esquerdo";
                case PreferredFoot.Both: return "Ambos";
                default: return "Direito";
            }
        }

        public static string Personality(PersonalityTrait personality)
        {
            switch (personality)
            {
                case PersonalityTrait.Reserved: return "Reservado";
                case PersonalityTrait.Outspoken: return "Franco";
                case PersonalityTrait.Leader: return "Líder";
                case PersonalityTrait.Temperamental: return "Temperamental";
                case PersonalityTrait.Professional: return "Profissional";
                case PersonalityTrait.Insecure: return "Inseguro";
                case PersonalityTrait.Ambitious: return "Ambicioso";
                default: return "Equilibrado";
            }
        }
    }
}
