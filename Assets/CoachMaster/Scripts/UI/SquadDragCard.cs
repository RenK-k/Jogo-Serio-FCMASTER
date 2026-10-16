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
        Vector2 _grab;
        CanvasGroup _fade;
        float _fadeAlpha = 1f;
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
            var source = transform as RectTransform;
            if (_canvas == null || source == null)
                return;
            var clone = Instantiate(gameObject, _canvas.transform, false);
            clone.name = "DragGhost";
            foreach (var extra in clone.GetComponents<SquadDragCard>())
                Destroy(extra);
            foreach (var button in clone.GetComponents<Button>())
                Destroy(button);
            var group = clone.GetComponent<CanvasGroup>();
            if (group == null)
                group = clone.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 1f;
            foreach (var graphic in clone.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
            _ghost = clone.GetComponent<RectTransform>();
            _ghost.anchorMin = _ghost.anchorMax = new Vector2(0.5f, 0.5f);
            _ghost.pivot = new Vector2(0.5f, 0.5f);
            _ghost.sizeDelta = source.rect.size.sqrMagnitude > 4f ? source.rect.size : new Vector2(96f, 72f);
            var canvasRect = _canvas.transform as RectTransform;
            var camera = eventData.pressEventCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(camera, source.position), camera, out var cardLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, camera, out var pointerLocal);
            _grab = cardLocal - pointerLocal;
            _ghost.localPosition = cardLocal;
            _ghost.SetAsLastSibling();
            _fade = GetComponent<CanvasGroup>();
            if (_fade == null)
                _fade = gameObject.AddComponent<CanvasGroup>();
            _fadeAlpha = _fade.alpha;
            _fade.alpha = 0.28f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateGhost(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            DropGhost();
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
            DropGhost();
            if (!_dragging)
                return;
            _dragging = false;
            _dragActive = false;
            _blockFrame = Time.frameCount;
        }

        void DropGhost()
        {
            if (_ghost != null)
                Destroy(_ghost.gameObject);
            _ghost = null;
            if (_fade != null)
                _fade.alpha = _fadeAlpha;
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
            _ghost.localPosition = local + _grab;
        }
    }
}
