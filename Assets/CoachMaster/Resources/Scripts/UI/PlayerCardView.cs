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
            var number = BroadcastUi.Label(card.transform, "BackNumber", player.SquadNumber.ToString(), 80,
                new Color(shirt.r, shirt.g, shirt.b, 0.22f), TextAnchor.MiddleCenter, FontStyle.Bold);
            number.resizeTextForBestFit = true;
            number.resizeTextMinSize = 10;
            number.resizeTextMaxSize = 160;
            number.horizontalOverflow = HorizontalWrapMode.Overflow;
            number.verticalOverflow = VerticalWrapMode.Overflow;
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

            if (player.Injury != null && player.Injury.IsInjured)
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

            var overall = BroadcastUi.Label(card.transform, "Overall",
                PlayerAttributes.CurrentOverallAtPosition(player, position).ToString(), 13, Color.white,
                TextAnchor.MiddleRight, FontStyle.Bold);
            overall.resizeTextForBestFit = true;
            overall.resizeTextMinSize = 8;
            overall.resizeTextMaxSize = 13;
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
    }
}
