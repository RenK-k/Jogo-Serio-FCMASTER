using CoachMaster.Data;
using CoachMaster.Domain;

namespace CoachMaster.Career
{
    public static class CareerFactory
    {
        public static CareerState Create(string coachName, string clubId)
        {
            var world = WorldCatalog.Create();
            if (!world.Clubs.ContainsKey(clubId))
                throw new System.ArgumentException($"Clube desconhecido: {clubId}");

            return new CareerState
            {
                World = world,
                Coach = new Coach
                {
                    Name = coachName.Trim(),
                    ClubId = clubId
                },
                Calendar = new GameCalendar(GameCalendar.SeasonStart),
                Knowledge = new CoachKnowledge()
            };
        }
    }
}
