using System.Collections.Generic;
using FStudio.MatchEngine;
using UnityEngine;

namespace CoachMaster.MatchPlay
{
    public sealed class ModernStadiumDress : MonoBehaviour
    {
        const string StadiumPath = "Assets/CoachMaster/Futebol3d/arte/stadium/modern-stadium/source/Modern Stadium1.2.dae";
        GameObject _stadium;
        float _hideUntil;

        void Update()
        {
            if (MatchManager.Current == null)
                return;
            if (_stadium == null)
                Build();
            if (_stadium != null && Time.unscaledTime < _hideUntil)
                HideForeign();
        }

        void Build()
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(StadiumPath);
            if (prefab == null)
                return;
            _stadium = Instantiate(prefab);
            _stadium.name = "ModernStadium";
            var cameras = _stadium.GetComponentsInChildren<Camera>(true);
            for (var i = 0; i < cameras.Length; i++)
                Destroy(cameras[i].gameObject);
            var colliders = _stadium.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
            Paint(_stadium);
            Fit(_stadium, MatchManager.Current);
            _hideUntil = Time.unscaledTime + 8f;
            HideForeign();
#endif
        }

        static void Paint(GameObject stadium)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                return;
            var renderers = stadium.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var source = renderers[i].sharedMaterials;
                var next = new Material[source.Length];
                for (var m = 0; m < source.Length; m++)
                {
                    var material = source[m];
                    if (material == null)
                        continue;
                    if (material.shader != null && material.shader.name.Contains("Universal Render Pipeline"))
                    {
                        next[m] = material;
                        continue;
                    }

                    var painted = new Material(shader);
                    painted.name = material.name;
                    Texture texture = null;
                    if (material.HasProperty("_BaseMap"))
                        texture = material.GetTexture("_BaseMap");
                    if (texture == null && material.HasProperty("_MainTex"))
                        texture = material.GetTexture("_MainTex");
                    if (texture != null)
                    {
                        painted.SetTexture("_BaseMap", texture);
                        painted.SetColor("_BaseColor", Color.white);
                    }
                    else if (material.HasProperty("_Color"))
                        painted.SetColor("_BaseColor", material.GetColor("_Color"));
                    else if (material.HasProperty("_BaseColor"))
                        painted.SetColor("_BaseColor", material.GetColor("_BaseColor"));
                    next[m] = painted;
                }

                renderers[i].sharedMaterials = next;
                renderers[i].enabled = true;
            }
        }

        static void Fit(GameObject stadium, MatchManager field)
        {
            var renderers = stadium.GetComponentsInChildren<Renderer>(true);
            var grass = Grass(renderers);
            if (grass == null)
                return;
            var bounds = grass.bounds;
            if (bounds.size.x < 0.01f || bounds.size.z < 0.01f)
                return;
            if (bounds.size.z > bounds.size.x)
                stadium.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            bounds = grass.bounds;
            var pivot = new GameObject("ModernStadiumPivot");
            stadium.transform.SetParent(pivot.transform, true);
            var scaleX = field.fieldEndX / bounds.size.x;
            var scaleZ = field.fieldEndY / bounds.size.z;
            pivot.transform.localScale = new Vector3(scaleX, (scaleX + scaleZ) * 0.5f, scaleZ);
            bounds = grass.bounds;
            var center = new Vector3(field.fieldEndX * 0.5f, 0f, field.fieldEndY * 0.5f);
            pivot.transform.position += new Vector3(center.x - bounds.center.x, -bounds.max.y, center.z - bounds.center.z);
        }

        static Renderer Grass(Renderer[] renderers)
        {
            Renderer named = null;
            Renderer flattest = null;
            var best = 0f;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                    continue;
                if (named == null && IsGrass(renderer))
                    named = renderer;
                var size = renderer.bounds.size;
                var score = size.x * size.z / Mathf.Max(0.25f, size.y);
                if (score > best)
                {
                    best = score;
                    flattest = renderer;
                }
            }

            return named != null ? named : flattest;
        }

        static bool IsGrass(Renderer renderer)
        {
            if (NameLooksLikeGrass(renderer.name) || NameLooksLikeGrass(renderer.gameObject.name))
                return true;
            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null && NameLooksLikeGrass(materials[i].name))
                    return true;
            }

            return false;
        }

        static bool NameLooksLikeGrass(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            var text = name.ToLowerInvariant();
            return text.Contains("trawa") || text.Contains("relva") || text.Contains("grass");
        }

        void HideForeign()
        {
            if (_stadium == null)
                return;
            var ours = new HashSet<Renderer>(_stadium.GetComponentsInChildren<Renderer>(true));
            var player = LayerMask.NameToLayer("Player");
            var ball = LayerMask.NameToLayer("Ball");
            var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || ours.Contains(renderer))
                    continue;
                var layer = renderer.gameObject.layer;
                if (layer == player || layer == ball)
                    continue;
                var scene = renderer.gameObject.scene.name;
                if (scene.IndexOf("Stadium", System.StringComparison.OrdinalIgnoreCase) >= 0 || Under(renderer.transform, "Field"))
                    renderer.enabled = false;
            }
        }

        static bool Under(Transform transform, string name)
        {
            while (transform != null)
            {
                if (transform.name == name)
                    return true;
                transform = transform.parent;
            }

            return false;
        }
    }
}
