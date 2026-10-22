using UnityEngine;

namespace CoachMaster.UI
{
    /// <summary>Keeps player cards at a steady screen size when the general UI scale changes.</summary>
    public sealed class UiScaleInvariant : MonoBehaviour
    {
        Vector3 _baseScale;
        bool _captured;

        void Awake() => Capture();

        void OnEnable() => Apply();

        void Capture()
        {
            if (_captured)
                return;
            _baseScale = transform.localScale;
            _captured = true;
        }

        public void Apply()
        {
            Capture();
            var scale = Mathf.Max(0.01f, SettingsPrefs.UiScale);
            transform.localScale = _baseScale / scale;
        }

        public static void ApplyAll()
        {
            var items = FindObjectsByType<UiScaleInvariant>(FindObjectsSortMode.None);
            for (var i = 0; i < items.Length; i++)
                if (items[i] != null)
                    items[i].Apply();
        }
    }
}
