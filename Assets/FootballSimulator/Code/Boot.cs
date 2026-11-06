using FStudio.Events;
using FStudio.Loaders;
using FStudio.MatchEngine.Tactics;
using FStudio.UI;
using FStudio.UI.Events;
using UnityEngine;

namespace FStudio {
    public class Boot : MonoBehaviour {
        private async void Start() {
            if (CareerTacticLock.Active)
                return;

            await UILoader.Current.GeneralUILoader.Load();
            await SceneLoader.LoadDefaultScene();

            // show main menu.
            EventManager.Trigger(new MainMenuEvent());
        }
    }
}
