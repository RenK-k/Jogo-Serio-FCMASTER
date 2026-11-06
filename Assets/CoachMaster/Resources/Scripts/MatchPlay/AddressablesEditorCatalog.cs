using UnityEngine;
using UnityEngine.AddressableAssets;

static class AddressablesEditorCatalog
{
    const string SettingsGuid = "47241fa8069554f4a9021889dfcb6227";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void UseProjectAssets()
    {
#if UNITY_EDITOR
        PlayerPrefs.SetString(Addressables.kAddressablesRuntimeDataPath, "GUID:" + SettingsGuid);
        Addressables.InitializeAsync();
#endif
    }
}
