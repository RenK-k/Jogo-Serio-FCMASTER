using FStudio.MatchEngine;
using UnityEngine;

namespace CoachMaster.MatchPlay
{
    public sealed class ModernStadiumDress : MonoBehaviour
    {
        bool _ready;
        GameObject _rain;

        void Update()
        {
            if (_ready)
                return;
            var cover = GameObject.Find("PitchGrass");
            if (cover != null)
                Destroy(cover);
            ShowFieldGrass();
            if (SimulatorCareer.Rain && _rain == null)
            {
                var match = MatchManager.Current;
                var length = match != null ? Mathf.Max(10f, match.fieldEndX) : 85f;
                var width = match != null ? Mathf.Max(10f, match.fieldEndY) : 60f;
                _rain = SpawnRain(length, width);
            }
            _ready = !SimulatorCareer.Rain || _rain != null;
        }

        static void ShowFieldGrass()
        {
            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || renderer.gameObject.name != "Grass")
                    continue;
                renderer.gameObject.SetActive(true);
                renderer.enabled = true;
            }
        }

        void LateUpdate()
        {
            if (_rain == null)
                return;
            var view = Camera.main;
            if (view == null)
                return;
            var point = view.transform.position;
            point.y = Mathf.Max(point.y, 12f);
            _rain.transform.position = point;
        }

        static GameObject SpawnRain(float length, float width)
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Rainy VFX/Prefab/Particle System.prefab");
            if (prefab == null)
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Rainy VFX/Prefab/Particle System (1).prefab");
            if (prefab == null)
                return null;
            var rain = Instantiate(prefab);
            rain.name = "MatchRain";
            rain.transform.position = new Vector3(length * 0.5f, 18f, width * 0.5f);
            var systems = rain.GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null)
                    continue;
                systems[i].gameObject.SetActive(true);
                systems[i].Play(true);
            }
            return rain;
#else
            return null;
#endif
        }
    }
}
