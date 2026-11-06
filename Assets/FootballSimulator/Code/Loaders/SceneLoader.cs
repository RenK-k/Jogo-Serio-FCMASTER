using System.Threading.Tasks;
using FStudio.MatchEngine.Tactics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;

using UnityEngine.SceneManagement;

namespace FStudio.Loaders {
    public static class SceneLoader {
        private const string DEFAULT_SCENE_NAME = "DefaultScene";

        public static async Task LoadDefaultScene () {
            if (FStudio.MatchEngine.Tactics.CareerTacticLock.Active) {
                await UnloadMatchScene();
                return;
            }

            var loader = SceneManager.LoadSceneAsync(DEFAULT_SCENE_NAME, LoadSceneMode.Single);
            if (loader == null)
                return;

            while (!loader.isDone) {
                await Task.Yield();
            }
        }

        static string _matchScene;

        public static void NoteMatchScene(string sceneName) {
            _matchScene = sceneName;
        }

        public static async Task UnloadMatchScene() {
            if (string.IsNullOrEmpty(_matchScene))
                return;
            var scene = SceneManager.GetSceneByName(_matchScene);
            _matchScene = null;
            if (!scene.IsValid() || !scene.isLoaded)
                return;
            var active = SceneManager.GetActiveScene();
            if (active == scene) {
                for (var i = 0; i < SceneManager.sceneCount; i++) {
                    var other = SceneManager.GetSceneAt(i);
                    if (other.isLoaded && other != scene) {
                        SceneManager.SetActiveScene(other);
                        break;
                    }
                }
            }
            var operation = SceneManager.UnloadSceneAsync(scene);
            while (operation != null && !operation.isDone)
                await Task.Yield();
        }

        public static async Task<SceneInstance> LoadScene (AssetReference sceneAsset) {
            Debug.Log($"[SceneLoader] Load scene {sceneAsset}");

            var mode = FStudio.MatchEngine.Tactics.CareerTacticLock.Active ? LoadSceneMode.Additive : LoadSceneMode.Single;
#if UNITY_EDITOR
            if (await EditorContent.LoadScene(sceneAsset, mode))
                return default;
#endif
            var loader = await Addressables.LoadSceneAsync(sceneAsset, mode).Task;
            var activator = loader.ActivateAsync();

            while (!activator.isDone) {
                await Task.Yield();
            }

            return loader;
        }
    }
}
