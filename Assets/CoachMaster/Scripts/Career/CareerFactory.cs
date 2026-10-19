using CoachMaster.Domain;

namespace CoachMaster.Career
{
    public static class CareerFactory
    {
        public static CareerState Create(string coachName, string clubId, WorldState world)
        {
            CareerNotificationQueue.Clear();
            if (world == null || !world.Clubs.ContainsKey(clubId))
                throw new System.ArgumentException($"Clube desconhecido: {clubId}");

            var career = new CareerState
            {
                World = world,
                Coach = new Coach
                {
                    Name = coachName.Trim(),
                    ClubId = clubId
                },
                Calendar = new GameCalendar(GameCalendar.Takeover),
                Knowledge = new CoachKnowledge()
            };
            career.SupporterApproval = SupporterReactions.StartingApproval(career.Club);
            SeasonSchedule.Build(career);
            CareerMoments.Begin(career);
            return career;
        }
    }
}
