using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CoachMaster.EditorTools
{
    [InitializeOnLoad]
    static class PlayFromMainMenu
    {
        const string StartScene = "Assets/Scenes/MainMenu.unity";

        static PlayFromMainMenu()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScene);
            if (scene != null)
                EditorSceneManager.playModeStartScene = scene;
        }
    }
}
