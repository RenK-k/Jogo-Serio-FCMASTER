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
                    var day = UnityEngine.Object.FindAnyObjectByType<CoachMaster.UI.Scenes.MatchDayController>();
                    if (day != null)
                        day.ToggleFromInput();
                }
                return true;
            }

            MatchPause.Pause();
            return true;
        }
    }
}
