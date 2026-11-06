using FStudio.Input;
using FStudio.MatchEngine.Tactics;
using UnityEngine.InputSystem;

namespace FStudio.MatchEngine.Input {
    internal class GeneralUserInput : InputListener {
        public GeneralUserInput(string actionMap, int playerIndex, int layer = 0) : base(actionMap, playerIndex, layer) {
            RegisterAction("Pause", PauseInput);
        }

        ~GeneralUserInput() {
            Clear();
        }

        private bool PauseInput(InputAction.CallbackContext ctx) {
            if (CareerTacticLock.Active) {
                if (CoachMaster.UI.Scenes.SimulatorMatchHost.PitchVisible) {
                    var root = CoachMaster.App.GameRoot.Instance;
                    if (root != null)
                        root.GetComponent<CoachMaster.UI.AudioSettingsOverlay>()?.Toggle();
                }
                return true;
            }

            MatchPause.Pause();
            return true;
        }
    }
}
