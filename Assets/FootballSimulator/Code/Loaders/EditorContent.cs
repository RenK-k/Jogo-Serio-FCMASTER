#if UNITY_EDITOR
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace FStudio.Loaders {
    public static class EditorContent {
        public static bool TryLoad<T>(AssetReference reference, out T asset) where T : Object {
            asset = null;
            if (reference == null || string.IsNullOrEmpty(reference.AssetGUID))
                return false;
            var path = AssetDatabase.GUIDToAssetPath(reference.AssetGUID);
            if (string.IsNullOrEmpty(path))
                return false;
            asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null;
        }

        public static GameObject Instantiate(AssetReference reference, Transform parent) {
            if (!TryLoad<GameObject>(reference, out var prefab))
                return null;
            return parent == null ? Object.Instantiate(prefab) : Object.Instantiate(prefab, parent);
        }

        public static async Task<bool> LoadScene(AssetReference reference, LoadSceneMode mode) {
            if (reference == null || string.IsNullOrEmpty(reference.AssetGUID))
                return false;
            var path = AssetDatabase.GUIDToAssetPath(reference.AssetGUID);
            if (string.IsNullOrEmpty(path))
                return false;
            var operation = EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(mode));
            if (operation == null)
                return false;
            while (!operation.isDone)
                await Task.Yield();
            return true;
        }
    }
}
#endif
