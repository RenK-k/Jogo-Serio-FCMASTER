
using FStudio.UI.Events;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace FStudio.UI {
    public class BigLoadingPanel : EventPanel<BigLoadingEvent> {
        protected override async void OnEventCalled(BigLoadingEvent eventObject) {
            if (eventObject == null) {
                await Task.Delay(250);
                QuietEllipsis();
                if (canvasGroup != null) {
                    canvasGroup.alpha = 0f;
                    canvasGroup.blocksRaycasts = false;
                    canvasGroup.interactable = false;
                }
                IsActive = false;
                if (gameObject.activeSelf)
                    gameObject.SetActive(false);
            } else {
                QuietEllipsis();
                Appear();
            }
        }

        static void QuietEllipsis() {
            var labels = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < labels.Length; i++) {
                if (labels[i] != null && labels[i].overflowMode == TextOverflowModes.Ellipsis)
                    labels[i].overflowMode = TextOverflowModes.Truncate;
            }
        }
    }
}