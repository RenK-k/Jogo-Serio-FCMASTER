using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    public static class SceneBind
    {
        public static T Get<T>(Transform root, string path) where T : Component
        {
            var target = root == null ? null : root.Find(path);
            if (target == null)
            {
                Debug.LogError("Em falta na scene: " + path);
                return null;
            }

            return target.GetComponent<T>();
        }

        public static Button Wire(Component target, Action act)
        {
            if (target == null || act == null)
                return null;
            var button = target.GetComponent<Button>();
            if (button == null)
                return null;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (!BroadcastUi.AllowClick())
                    return;
                act();
            });
            return button;
        }
    }
}
