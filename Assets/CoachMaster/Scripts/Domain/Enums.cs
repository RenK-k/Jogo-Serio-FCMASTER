namespace CoachMaster.Domain
{
    public enum LeagueTier
    {
        First = 1,
        Second = 2
    }

    public enum PlayerPosition
    {
        Goalkeeper,
        RightBack,
        CentreBack,
        LeftBack,
        DefensiveMidfielder,
        CentralMidfielder,
        AttackingMidfielder,
        RightWinger,
        LeftWinger,
        Striker
    }

    public enum PreferredFoot
    {
        Right,
        Left,
        Both
    }

    public enum StaffRole
    {
        AssistantCoach,
        GoalkeeperCoach,
        FitnessCoach,
        Physiotherapist
    }

    public enum PersonalityTrait
    {
        Reserved,
        Outspoken,
        Leader,
        Temperamental,
        Professional,
        Insecure,
        Ambitious
    }

    public enum KitPattern
    {
        Solid,
        VerticalStripes,
        HorizontalHoops,
        Sash,
        Halves,
        ChestBand,
        Pinstripe
    }
}
