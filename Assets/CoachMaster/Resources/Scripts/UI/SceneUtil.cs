using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    public static class SceneUtil
    {
        public static T Find<T>(Transform root, string path) where T : Component
        {
            var t = root.Find(path);
            if (t == null)
            {
                Debug.LogError("Objeto em falta na cena: " + path, root);
                return null;
            }

            return t.GetComponent<T>();
        }

        public static Transform FindT(Transform root, string path)
        {
            var t = root.Find(path);
            if (t == null)
                Debug.LogError("Objeto em falta na cena: " + path, root);
            return t;
        }

        public static void Rebuild(Transform content, Transform template, int count, Action<GameObject, int> bind)
        {
            template.gameObject.SetActive(false);
            for (var i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i);
                if (child != template)
                    UnityEngine.Object.Destroy(child.gameObject);
            }

            for (var i = 0; i < count; i++)
            {
                var row = UnityEngine.Object.Instantiate(template, content);
                row.gameObject.SetActive(true);
                row.name = template.name + "_" + i;
                bind(row.gameObject, i);
            }
        }

        public static void BindButton(Transform root, string path, Action click)
        {
            var button = Find<Button>(root, path);
            if (button == null)
                return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => click());
        }
    }
}
