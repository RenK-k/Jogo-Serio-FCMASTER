using CoachMaster.Career;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    public sealed class ConsequenceToast : MonoBehaviour
    {
        CanvasGroup _group;
        Text _heading;
        Text _message;
        float _age;
        bool _visible;

        public static void Attach(RectTransform root)
        {
            if (root == null)
                return;
            var toast = root.GetComponent<ConsequenceToast>();
            if (toast == null)
                toast = root.gameObject.AddComponent<ConsequenceToast>();
            toast.Build(root);
        }

        void Build(RectTransform root)
        {
            if (_group != null)
                return;

            var panel = BroadcastUi.Panel(root, "ConsequenceNotice", new Color(0.045f, 0.055f, 0.11f, 0.97f));
            BroadcastUi.Place(panel.rectTransform, 0.68f, 0.815f, 0.985f, 0.975f);
            panel.raycastTarget = false;
            _group = panel.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            var accent = BroadcastUi.Solid(panel.transform, "Accent", UiTheme.Accent);
            BroadcastUi.Place(accent.rectTransform, 0f, 0f, 0.012f, 1f);
            var eyebrow = BroadcastUi.Label(panel.transform, "Eyebrow", "ISTO PODE TER CONSEQUÊNCIAS", 12, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(eyebrow.rectTransform, 0.055f, 0.75f, 0.96f, 0.94f);
            _heading = BroadcastUi.Label(panel.transform, "Heading", "", 18, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(_heading.rectTransform, 0.055f, 0.44f, 0.96f, 0.74f);
            _message = BroadcastUi.Label(panel.transform, "Message", "", 14, new Color(0.84f, 0.86f, 0.92f), TextAnchor.MiddleLeft);
            BroadcastUi.Place(_message.rectTransform, 0.055f, 0.07f, 0.96f, 0.42f);
            panel.transform.SetAsLastSibling();
        }

        void Update()
        {
            if (_group == null)
                return;

            if (!SettingsPrefs.DecisionAlerts)
            {
                while (CareerNotificationQueue.TryDequeue(out _)) { }
                _visible = false;
                _group.alpha = 0f;
                return;
            }

            if (!_visible && CareerNotificationQueue.TryDequeue(out var notice))
            {
                _heading.text = notice.Heading;
                _message.text = notice.Message;
                _age = 0f;
                _visible = true;
                _group.transform.SetAsLastSibling();
            }

            if (!_visible)
                return;

            _age += Time.unscaledDeltaTime;
            var fadeIn = SettingsPrefs.ReduceMotion ? 0.04f : 0.22f;
            var hold = SettingsPrefs.AlertDuration;
            var fadeOut = SettingsPrefs.ReduceMotion ? 0.04f : 0.45f;
            var fade = _age < fadeIn
                ? _age / fadeIn
                : _age < hold ? 1f : 1f - (_age - hold) / fadeOut;
            _group.alpha = Mathf.Clamp01(fade);
            if (_age >= hold + fadeOut)
            {
                _group.alpha = 0f;
                _visible = false;
            }
        }
    }
}
