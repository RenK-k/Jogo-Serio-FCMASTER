using System.Collections.Generic;

namespace CoachMaster.Domain
{
    public sealed class Player
    {
        public string Id { get; set; }
        public string ClubId { get; set; }
        public string FullName { get; set; }
        public int Age { get; set; }
        public int SquadNumber { get; set; }
        public PlayerPosition Position { get; set; }
        public PreferredFoot PreferredFoot { get; set; }
        public string Nationality { get; set; }
        public bool IsCaptain { get; set; }
        public bool IsViceCaptain { get; set; }

        public int Morale { get; set; }
        public int Fatigue { get; set; }
        public int Fitness { get; set; }
        public int Form { get; set; }
        public int Motivation { get; set; }
        public int HeightCm { get; set; }
        public int WeightKg { get; set; }
        public int WeakFootQuality { get; set; }
        public int TrainingRating { get; set; }
        public InjuryState Injury { get; set; } = new InjuryState();
        public PersonalityTrait Personality { get; set; }
        public int Potential { get; set; }
        public List<PlayerPosition> AlternatePositions { get; set; } = new List<PlayerPosition>();
        public List<PlayerTrait> Traits { get; set; } = new List<PlayerTrait>();
        public PlayerAttributes Attributes { get; set; } = new PlayerAttributes();

        public bool HasTrait(PlayerTrait trait)
        {
            return Traits != null && Traits.Contains(trait);
        }
        public string PortraitPath { get; set; }
        public string Duty { get; set; }
    }
}
