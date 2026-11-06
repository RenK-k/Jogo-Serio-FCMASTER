using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

[InitializeOnLoad]
static class AddressablesFastPlay
{
    static AddressablesFastPlay()
    {
        EditorApplication.delayCall += Apply;
    }

    static void Apply()
    {
        var registered = AssetDatabase.LoadAssetAtPath<AddressableAssetSettingsDefaultObject>("Assets/AddressableAssetsData/DefaultObject.asset");
        if (registered != null && !EditorBuildSettings.TryGetConfigObject(AddressableAssetSettingsDefaultObject.kDefaultConfigObjectName, out AddressableAssetSettingsDefaultObject _))
            EditorBuildSettings.AddConfigObject(AddressableAssetSettingsDefaultObject.kDefaultConfigObjectName, registered, true);

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
            return;

        for (var i = 0; i < settings.DataBuilders.Count; i++)
        {
            if (settings.GetDataBuilder(i) is BuildScriptFastMode)
            {
                settings.ActivePlayModeDataBuilderIndex = i;
                break;
            }
        }

        for (var i = 0; i < settings.DataBuilders.Count; i++)
        {
            if (settings.GetDataBuilder(i) is BuildScriptPackedMode)
            {
                settings.ActivePlayerDataBuilderIndex = i;
                break;
            }
        }

        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(settings, out string guid, out long _))
            PlayerPrefs.SetString(Addressables.kAddressablesRuntimeDataPath, "GUID:" + guid);
    }
}
