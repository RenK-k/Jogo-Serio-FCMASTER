using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

using FStudio.Utilities;
using FStudio.Animation;

using System;

namespace FStudio {
    public class CameraTransition : SceneObjectSingleton <CameraTransition>, IDynamicFloat  {
        [SerializeField] private RawImage rawImage = default;
        [SerializeField] private CanvasGroup canvasGroup = default;
        [SerializeField] private AnimationCurve fadeOutCurve = default;
        [SerializeField] private float fadeOutSpeed = default;

        public void StartTransition (Action onCompleted = null) {
            if (Camera.main == null || rawImage == null) {
                onCompleted?.Invoke();
                return;
            }
            TakePicture();
            SetFloat(1);

            //Fade out.
            new DynamicFloatAction(this, 1, 0, fadeOutSpeed, fadeOutCurve).
                GetQuery ().
                Start (this, onCompleted);
        }

        private void TakePicture () {
            var camera = Camera.main;
            if (camera == null || rawImage == null)
                return;
            var description = new RenderTextureDescriptor(Mathf.Max(8, Screen.width), Mathf.Max(8, Screen.height), RenderTextureFormat.ARGB32, 16);
            description.depthStencilFormat = GraphicsFormat.D16_UNorm;
            RenderTexture renderTexture = new RenderTexture(description);
            var previous = camera.targetTexture;
            camera.targetTexture = renderTexture;
            var previousActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            try {
                camera.Render();
                rawImage.texture = renderTexture;
            }
            finally {
                camera.targetTexture = previous;
                RenderTexture.active = previousActive;
            }
        }

        public float GetFloat() {
            return canvasGroup.alpha;
        }

        public void SetFloat(float dynamicFloat) {
            canvasGroup.alpha = dynamicFloat;
        }
    }
}
