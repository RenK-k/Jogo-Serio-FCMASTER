using System;
using System.IO;
using CoachMaster.App;
using CoachMaster.Career;
using CoachMaster.Data;
using CoachMaster.Domain;
using CoachMaster.MatchPlay;
using CoachMaster.UI.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CoachMaster.EditorTools
{
    public static class SceneBaker
    {
        const string Marker = "Assets/CoachMaster/Editor/scenes-baked.flag";
        static bool _running;

        [InitializeOnLoadMethod]
        static void Schedule()
        {
            EditorApplication.delayCall += TryAuto;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += TryAuto;
            };
        }

        static void TryAuto()
        {
            if (_running || EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(Marker))
                return;
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                Debug.Log("A scene aberta tem alterações por gravar. Quando quiseres, usa o menu Coach Master → Gravar ecrãs nas scenes.");
                return;
            }

            BakeAll();
        }

        [MenuItem("Coach Master/Gravar ecrãs nas scenes")]
        public static void BakeAll()
        {
            if (_running)
                return;
            _running = true;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var session = new GameSession();
                session.Draft.LeagueId = Ids.League("atlas");
                session.PrepareLeague(session.Draft.LeagueId);
                foreach (var club in session.Catalog.ClubsInLeague(session.Draft.LeagueId))
                {
                    session.Draft.ClubId = club.Id;
                    break;
                }

                Paint("Assets/Scenes/MainMenu.unity", canvas => canvas.GetComponent<MainMenuController>().BakeDesign(session));
                Paint("Assets/Scenes/LeagueSelect.unity", canvas => canvas.GetComponent<LeagueSelectController>().BakeDesign(session));
                Paint("Assets/Scenes/TeamSelect.unity", canvas => canvas.GetComponent<TeamSelectController>().BakeDesign(session));
                Paint("Assets/Scenes/ClubInfo.unity", canvas => canvas.GetComponent<ClubInfoController>().BakeDesign(session));
                Paint("Assets/Scenes/CreateCareer.unity", canvas => canvas.GetComponent<CreateCareerController>().BakeDesign(session));

                var career = CareerFactory.Create("Mister", session.Draft.ClubId, WorldCatalog.Create());
                Paint("Assets/Scenes/CareerHub.unity", canvas => canvas.GetComponent<CareerHubController>().BakeDesign(career));
                Fixture fixture = null;
                foreach (var item in career.Fixtures)
                {
                    if (!item.Involves(career.Coach.ClubId))
                        continue;
                    fixture = item;
                    break;
                }

                var match = MatchSession.Create(career, fixture);
                Paint("Assets/Scenes/MatchDay.unity", canvas => canvas.GetComponent<MatchDayController>().BakeDesign(career, match));
                var folder = Path.GetDirectoryName(Marker);
                if (!string.IsNullOrEmpty(folder))
                    Directory.CreateDirectory(folder);
                File.WriteAllText(Marker, "ok");
                AssetDatabase.Refresh();
                Debug.Log("Ecrãs gravados nas scenes. Podes mover os objetos dentro de Design.");
            }
            catch (Exception error)
            {
                Debug.LogError("Não consegui gravar os ecrãs nas scenes: " + error);
            }
            finally
            {
                _running = false;
                if (setup != null && setup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        static void Paint(string path, Action<Canvas> build)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            build(canvas);
            DropLooseRoots();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void DropLooseRoots()
        {
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || transform.parent != null)
                    continue;
                var go = transform.gameObject;
                if (go.GetComponent<Camera>() != null || go.GetComponent<Canvas>() != null || go.GetComponent<UnityEngine.EventSystems.EventSystem>() != null)
                    continue;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
