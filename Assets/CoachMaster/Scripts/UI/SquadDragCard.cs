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

        static bool _dragActive;
        static float _dragSince;
        static int _blockFrame = -1;
        static Player _pendingPlayer;
        static int _pendingIndex = -1;
        static int _pendingFrom = -1;
        bool _dragging;

        public static bool Holding
        {
            get
            {
                if (_dragActive && Time.unscaledTime > _dragSince + 8f)
                    _dragActive = false;
                return _dragActive || Time.frameCount <= _blockFrame;
            }
        }

        public static void Release()
        {
            _dragActive = false;
            _blockFrame = -1;
            _pendingPlayer = null;
            _pendingIndex = -1;
            _pendingFrom = -1;
        }

        public static bool ConsumeSwap(out Player player, out int fieldIndex, out int fromField)
        {
            player = _pendingPlayer;
            fieldIndex = _pendingIndex;
            fromField = _pendingFrom;
            _pendingPlayer = null;
            _pendingIndex = -1;
            _pendingFrom = -1;
            return player != null && fieldIndex >= 0;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Player == null || (!Bench && FieldIndex < 0))
                return;
            _dragging = true;
            _dragActive = true;
            _dragSince = Time.unscaledTime;
            _canvas = GetComponentInParent<Canvas>();
            var image = GetComponent<Image>();
            if (image == null)
                return;
            var go = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _ghost = go.GetComponent<RectTransform>();
            _ghost.SetParent(_canvas.transform, false);
            var source = transform as RectTransform;
            _ghost.sizeDelta = source != null && source.rect.size.sqrMagnitude > 4f ? source.rect.size : new Vector2(120f, 90f);
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
            if (!_dragging)
                return;
            _dragging = false;
            _dragActive = false;
            _blockFrame = Time.frameCount;
            if (Player == null || EventSystem.current == null)
                return;
            if (Bench && OnSwapWithField == null)
                return;
            var target = FindTarget(eventData);
            if (target == null)
                return;
            _pendingPlayer = Player;
            _pendingIndex = target.FieldIndex;
            _pendingFrom = Bench ? -1 : FieldIndex;
        }

        void OnDisable()
        {
            if (_ghost != null)
                Destroy(_ghost.gameObject);
            _ghost = null;
            if (!_dragging)
                return;
            _dragging = false;
            _dragActive = false;
            _blockFrame = Time.frameCount;
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
