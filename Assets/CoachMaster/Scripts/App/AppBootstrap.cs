using CoachMaster.UI;
using CoachMaster.UI.Screens;
using UnityEngine;

namespace CoachMaster.App
{
    public sealed class AppBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var existing = FindFirstObjectByType<AppBootstrap>();
            if (existing != null)
                return;

            var go = new GameObject("CoachMaster");
            DontDestroyOnLoad(go);
            go.AddComponent<AppBootstrap>();
        }

        void Start()
        {
            var camera = Camera.main;
            if (camera != null)
                camera.backgroundColor = UiTheme.Canvas;

            var canvas = UiFactory.CreateCanvas();
            canvas.transform.SetParent(transform, false);

            var session = new GameSession();
            var router = new ScreenRouter(canvas.transform, session);
            router.Register("main", new MainMenuScreen());
            router.Register("leagues", new LeagueSelectScreen());
            router.Register("clubs", new ClubListScreen());
            router.Register("club", new ClubInfoScreen());
            router.Register("create", new CreateCareerScreen());
            router.Register("day", new DayHubScreen());
            router.Show("main");
        }
    }
}
