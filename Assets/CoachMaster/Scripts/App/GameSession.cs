using CoachMaster.Career;
using CoachMaster.Data;
using CoachMaster.Domain;

namespace CoachMaster.App
{
    public sealed class GameSession
    {
        public WorldState Catalog { get; }
        public NewCareerDraft Draft { get; } = new NewCareerDraft();
        public CareerState Career { get; private set; }

        public GameSession()
        {
            Catalog = WorldCatalog.Create();
        }

        public void StartCareer()
        {
            Career = CareerFactory.Create(Draft.CoachName, Draft.ClubId);
        }

        public void ClearCareer()
        {
            Career = null;
            Draft.Reset();
        }
    }

    public sealed class NewCareerDraft
    {
        public string LeagueId;
        public string ClubId;
        public string CoachName = "";

        public void Reset()
        {
            LeagueId = null;
            ClubId = null;
            CoachName = "";
        }
    }
}
