using CoachMaster.App;
using CoachMaster.Career;
using CoachMaster.Domain;
using CoachMaster.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    public static class PlayerCardView
    {
        static readonly Color DefaultCardColor = new Color(0.1f, 0.04f, 0.18f, 0.94f);

        public static Image Create(Transform parent, string objectName, Player player, Club club,
            PlayerPosition position, bool showFatigue, string extra = "", Color? background = null,
            float? fatiguePercent = null)
        {
            if (parent == null || player == null)
                return null;

            var card = BroadcastUi.Panel(parent, objectName, background ?? DefaultCardColor, true);
            if (card.GetComponent<UiScaleInvariant>() == null)
                card.gameObject.AddComponent<UiScaleInvariant>();
            if (card.GetComponent<RectMask2D>() == null)
                card.gameObject.AddComponent<RectMask2D>();

            var shirt = club == null ? UiTheme.Accent : BroadcastUi.Parse(club.JerseyPrimaryHex, UiTheme.Accent);
            var number = BroadcastUi.Label(card.transform, "BackNumber", player.SquadNumber.ToString(), 52,
                new Color(Mathf.Lerp(shirt.r, 1f, 0.7f), Mathf.Lerp(shirt.g, 1f, 0.7f), Mathf.Lerp(shirt.b, 1f, 0.7f), 0.52f),
                TextAnchor.MiddleCenter, FontStyle.Bold);
            number.font = UiTheme.Font;
            number.resizeTextForBestFit = true;
            number.resizeTextMinSize = 8;
            number.resizeTextMaxSize = 60;
            number.horizontalOverflow = HorizontalWrapMode.Overflow;
            number.verticalOverflow = VerticalWrapMode.Overflow;
            number.raycastTarget = false;
            BroadcastUi.Place(number.rectTransform, 0.02f, 0.02f, 0.98f, 0.98f);

            var faceObject = new GameObject("Face", typeof(RectTransform), typeof(Image));
            var face = faceObject.GetComponent<Image>();
            face.sprite = FaceBook.For(player);
            face.preserveAspect = true;
            face.raycastTarget = false;
            face.rectTransform.SetParent(card.transform, false);
            var faceCenter = new Vector2(0.5f, 0.614f);
            var faceSize = new Vector2(0.792f, 0.528f) * 1.21f;
            face.rectTransform.anchorMin = faceCenter - faceSize * 0.5f;
            face.rectTransform.anchorMax = faceCenter + faceSize * 0.5f;
            face.rectTransform.offsetMin = Vector2.zero;
            face.rectTransform.offsetMax = Vector2.zero;
            face.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            if (player.IsCaptain || player.IsViceCaptain)
            {
                var captainColor = player.IsCaptain ? new Color(0.96f, 0.72f, 0.18f, 0.98f) : new Color(0.62f, 0.72f, 0.86f, 0.98f);
                var captainBadge = BroadcastUi.Panel(card.transform, "CaptainBadge", captainColor, false);
                BroadcastUi.Place(captainBadge.rectTransform, 0.48f, 0.79f, 0.7f, 0.98f);
                var captainLabel = BroadcastUi.Label(captainBadge.transform, "Mark", player.IsCaptain ? "C" : "VC", 10,
                    new Color(0.04f, 0.04f, 0.06f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
                captainLabel.resizeTextForBestFit = true;
                captainLabel.resizeTextMinSize = 6;
                captainLabel.resizeTextMaxSize = 10;
                BroadcastUi.Stretch(captainLabel.rectTransform);
            }

            var reason = PlayerAvailability.Reason(player, Career());
            if (!string.IsNullOrEmpty(reason))
            {
                var iconName = PlayerAvailability.IconResource(reason);
                var sprites = string.IsNullOrEmpty(iconName) ? null : Resources.LoadAll<Sprite>(iconName);
                if (sprites != null && sprites.Length > 0)
                {
                    var badge = BroadcastUi.Solid(card.transform, "Absence", Color.white);
                    badge.sprite = sprites[0];
                    badge.preserveAspect = true;
                    badge.raycastTarget = false;
                    BroadcastUi.Place(badge.rectTransform, 0.72f, 0.76f, 0.98f, 0.98f);
                }
                var fade = card.GetComponent<CanvasGroup>();
                if (fade == null)
                    fade = card.gameObject.AddComponent<CanvasGroup>();
                fade.alpha = 0.42f;
            }

            var name = BroadcastUi.Label(card.transform, "Name", BroadcastUi.ShortName(player.FullName).ToUpperInvariant(),
                13, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 7;
            name.resizeTextMaxSize = 13;
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            name.verticalOverflow = VerticalWrapMode.Overflow;
            BroadcastUi.Place(name.rectTransform, 0.04f, 0.243f, 0.96f, 0.403f);

            if (!string.IsNullOrEmpty(extra))
            {
                var mark = BroadcastUi.Label(card.transform, "Extra", extra, 11, UiTheme.Accent,
                    TextAnchor.MiddleRight, FontStyle.Bold);
                mark.resizeTextForBestFit = true;
                mark.resizeTextMinSize = 7;
                mark.resizeTextMaxSize = 11;
                BroadcastUi.Place(mark.rectTransform, 0.72f, 0.84f, 0.98f, 0.98f);
            }

            if (player.Injury != null && player.Injury.IsInjured && string.IsNullOrEmpty(reason))
            {
                var injury = BroadcastUi.Label(card.transform, "Injury", "+", 22,
                    new Color(0.92f, 0.16f, 0.18f), TextAnchor.MiddleCenter, FontStyle.Bold);
                injury.rectTransform.anchorMin = injury.rectTransform.anchorMax = new Vector2(0.88f, 0.86f);
                injury.rectTransform.sizeDelta = new Vector2(22f, 22f);
            }

            var role = BroadcastUi.Label(card.transform, "Position", PresentationLabels.Position(position,
                LocalizationManager.Current == GameLanguage.English), 12, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            role.resizeTextForBestFit = true;
            role.resizeTextMinSize = 7;
            role.resizeTextMaxSize = 12;
            role.horizontalOverflow = HorizontalWrapMode.Overflow;
            role.verticalOverflow = VerticalWrapMode.Overflow;
            BroadcastUi.Place(role.rectTransform, 0.04f, 0.058f, 0.34f, 0.258f);

            var flag = BroadcastUi.Solid(card.transform, "NationFlag", Color.white);
            flag.sprite = FlagStudio.For(player.Nationality);
            flag.preserveAspect = true;
            flag.raycastTarget = false;
            BroadcastUi.Place(flag.rectTransform, 0.34f, 0.058f, 0.66f, 0.258f);

            var grade = PlayerGrade.Of(PlayerAttributes.CurrentOverallAtPosition(player, position));
            var overall = BroadcastUi.Label(card.transform, "Overall", grade.Code, 16, grade.Tint,
                TextAnchor.MiddleRight, FontStyle.Bold);
            overall.resizeTextForBestFit = true;
            overall.resizeTextMinSize = 10;
            overall.resizeTextMaxSize = 18;
            overall.horizontalOverflow = HorizontalWrapMode.Overflow;
            overall.verticalOverflow = VerticalWrapMode.Overflow;
            BroadcastUi.Place(overall.rectTransform, 0.66f, 0.058f, 0.88f, 0.258f);

            var fatigue = BroadcastUi.Solid(card.transform, "Fatigue", new Color(1f, 1f, 1f, 0.16f));
            fatigue.sprite = BroadcastUi.Round;
            fatigue.type = Image.Type.Sliced;
            fatigue.raycastTarget = false;
            BroadcastUi.Place(fatigue.rectTransform, 0.9f, 0.12f, 0.96f, 0.86f);
            fatigue.gameObject.SetActive(showFatigue);
            if (showFatigue)
            {
                var fresh = Mathf.Clamp01(1f - Mathf.Clamp(fatiguePercent ?? player.Fatigue, 0f, 100f) / 100f);
                var fill = BroadcastUi.Solid(fatigue.transform, "Fill",
                    fresh >= 0.72f ? UiTheme.Fitness : fresh >= 0.5f ? UiTheme.FitnessLow : UiTheme.FitnessBad);
                fill.sprite = BroadcastUi.Round;
                fill.type = Image.Type.Sliced;
                fill.raycastTarget = false;
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = new Vector2(1f, Mathf.Max(0.08f, fresh));
                fill.rectTransform.offsetMin = Vector2.zero;
                fill.rectTransform.offsetMax = Vector2.zero;
            }

            return card;
        }

        static CareerState Career()
        {
            var root = App.GameRoot.Instance;
            return root == null || root.Session == null ? null : root.Session.Career;
        }
    }
}
