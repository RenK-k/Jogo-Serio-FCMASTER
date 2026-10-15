using System;
using System.Collections.Generic;
using CoachMaster.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoachMaster.UI
{
    public sealed class SquadDragCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Player Player;
        public bool Bench;
        public int FieldIndex = -1;
        public Action<Player, int> OnSwapWithField;

        Canvas _canvas;
        RectTransform _ghost;
        static readonly List<RaycastResult> Hits = new List<RaycastResult>();

        public static bool Holding { get; private set; }
        bool _releaseHold;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!Bench || Player == null)
                return;
            Holding = true;
            _releaseHold = false;
            _canvas = GetComponentInParent<Canvas>();
            var image = GetComponent<Image>();
            if (image == null)
                return;
            var go = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _ghost = go.GetComponent<RectTransform>();
            _ghost.SetParent(_canvas.transform, false);
            _ghost.sizeDelta = (transform as RectTransform).sizeDelta;
            var ghostImage = go.GetComponent<Image>();
            ghostImage.color = new Color(image.color.r, image.color.g, image.color.b, 0.72f);
            ghostImage.raycastTarget = false;
            go.GetComponent<CanvasGroup>().blocksRaycasts = false;
            UpdateGhost(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateGhost(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_ghost != null)
                Destroy(_ghost.gameObject);
            _ghost = null;
            _releaseHold = true;
            if (!Bench || Player == null || OnSwapWithField == null || EventSystem.current == null)
                return;
            var target = FindTarget(eventData);
            if (target != null)
                OnSwapWithField(Player, target.FieldIndex);
        }

        void LateUpdate()
        {
            if (!_releaseHold)
                return;
            _releaseHold = false;
            Holding = false;
        }

        SquadDragCard FindTarget(PointerEventData eventData)
        {
            Hits.Clear();
            EventSystem.current.RaycastAll(eventData, Hits);
            for (var i = 0; i < Hits.Count; i++)
            {
                var card = Hits[i].gameObject.GetComponentInParent<SquadDragCard>();
                if (card == null || card.Bench || card.FieldIndex < 0 || card == this)
                    continue;
                return card;
            }

            var cards = FindObjectsByType<SquadDragCard>(FindObjectsSortMode.None);
            for (var i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                if (card == null || card.Bench || card.FieldIndex < 0 || card == this)
                    continue;
                var rect = card.transform as RectTransform;
                if (rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, eventData.position, eventData.pressEventCamera))
                    return card;
            }

            return null;
        }

        void UpdateGhost(PointerEventData eventData)
        {
            if (_ghost == null || _canvas == null)
                return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out var local);
            _ghost.localPosition = local;
        }
    }
}
