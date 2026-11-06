using System;
using UnityEngine;

namespace FStudio.MatchEngine.Tactics {
    public static class CareerTacticLock {
        public static bool Active;
        public static bool Portuguese;
        public static Sprite HomeCrest;
        public static Sprite AwayCrest;
        public static TacticPresetTypes Home = TacticPresetTypes.Balanced;
        public static TacticPresetTypes Away = TacticPresetTypes.Balanced;
        public static Action ReturnToMatchDay;
        public static Action WatchSimulation;
        public static Action<string> Cast;
        public static bool CareerChoice;
        public static bool VersusHuman;
        public static Shared.Responses.MatchCreateRequest.UserTeam OwnSide;
        public static string HomeName = "";
        public static string AwayName = "";
        public static string HomeVote = "";
        public static string AwayVote = "";
    }
}
