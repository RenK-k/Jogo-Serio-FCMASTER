using CoachMaster.App;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Scenes
{
    public sealed class MainMenuController : MonoBehaviour
    {
        void Start()
        {
            GameRoot.Require();
            GameRoot.EnsureUiInput();

            var bound = false;
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (button.gameObject.name != "NovaCarreira")
                    continue;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OpenCareer);
                bound = true;
            }

            if (!bound)
                SceneUtil.BindButton(transform, "CareerTile/NovaCarreira", OpenCareer);
        }

        static void OpenCareer()
        {
            GameRoot.Require().Open(SceneNames.LeagueSelect);
        }
    }
}
