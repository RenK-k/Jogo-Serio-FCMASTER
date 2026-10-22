using CoachMaster.Domain;

namespace CoachMaster.Presentation
{
    public static class PresentationLabels
    {
        public static string Position(PlayerPosition position)
        {
            return Position(position, false);
        }

        public static string Position(PlayerPosition position, bool english)
        {
            switch (position)
            {
                case PlayerPosition.Goalkeeper: return english ? "GK" : "GR";
                case PlayerPosition.RightBack: return english ? "RB" : "LD";
                case PlayerPosition.CentreBack: return english ? "CB" : "DC";
                case PlayerPosition.LeftBack: return english ? "LB" : "LE";
                case PlayerPosition.DefensiveMidfielder: return english ? "DM" : "MDC";
                case PlayerPosition.CentralMidfielder: return english ? "CM" : "MC";
                case PlayerPosition.AttackingMidfielder: return english ? "AM" : "MO";
                case PlayerPosition.RightWinger: return english ? "RW" : "ED";
                case PlayerPosition.LeftWinger: return english ? "LW" : "EE";
                case PlayerPosition.LeftMidfielder: return english ? "LM" : "ME";
                case PlayerPosition.RightMidfielder: return english ? "RM" : "MD";
                case PlayerPosition.Striker: return english ? "ST" : "PL";
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
