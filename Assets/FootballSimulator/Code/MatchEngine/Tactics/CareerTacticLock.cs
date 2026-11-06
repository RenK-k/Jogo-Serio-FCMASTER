using System;

namespace FStudio.MatchEngine.Tactics {
    public static class CareerTacticLock {
        public static bool Active;
        public static TacticPresetTypes Home = TacticPresetTypes.Balanced;
        public static TacticPresetTypes Away = TacticPresetTypes.Balanced;
        public static Action ReturnToMatchDay;
        public static Action WatchSimulation;
    }
}
