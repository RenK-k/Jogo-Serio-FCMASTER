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
            var loader = SceneManager.LoadSceneAsync(DEFAULT_SCENE_NAME, LoadSceneMode.Single);
            if (loader == null)
                return;

            while (!loader.isDone) {
                await Task.Yield();
            }
        }

        public static async Task<SceneInstance> LoadScene (AssetReference sceneAsset) {
            Debug.Log($"[SceneLoader] Load scene {sceneAsset}");

#if UNITY_EDITOR
            var mode = CareerTacticLock.Active ? LoadSceneMode.Additive : LoadSceneMode.Single;
            if (await EditorContent.LoadScene(sceneAsset, mode))
                return default;
#endif
            var loader = await Addressables.LoadSceneAsync(sceneAsset, UnityEngine.SceneManagement.LoadSceneMode.Single).Task;
            var activator = loader.ActivateAsync();

            while (!activator.isDone) {
                await Task.Yield();
            }

            return loader;
        }
    }
}
