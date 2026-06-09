using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace SaferTogether.UnityClient
{
    // draggable bolt that snaps to its target
    [Preserve]
    public sealed class DraggableBolt : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        private RectTransform rect;
        private RectTransform track;
        private Image image;
        private float startT;      // x fraction at the start (left)
        private float targetT;     // x fraction where it locks (right)
        private float snapThreshold;
        private float rowY;        // the bolt's row (fixed y)
        private float currentT;
        private bool locked;
        private Color unlockedColor;
        private Color lockedColor;
        private Action onLocked;
        private Func<bool> canLock;

        public bool IsLocked => locked;

        // set up the track, colors and callbacks
        public void Configure(
            RectTransform trackRect,
            Image boltImage,
            float start,
            float target,
            float snap,
            float row,
            Color unlocked,
            Color lockedTint,
            Func<bool> canLockGate,
            Action lockedCallback)
        {
            rect = GetComponent<RectTransform>();
            track = trackRect;
            image = boltImage;
            startT = start;
            targetT = target;
            snapThreshold = snap;
            rowY = row;
            unlockedColor = unlocked;
            lockedColor = lockedTint;
            canLock = canLockGate;
            onLocked = lockedCallback;

            SetT(startT);

            if (image != null)
            {
                image.color = unlockedColor;
            }
        }

        // brighten/dim the bolt to show if it's the next one allowed to lock
        public void SetActiveHighlight(bool active)
        {
            if (locked || image == null)
            {
                return;
            }

            image.color = active ? unlockedColor : new Color(unlockedColor.r, unlockedColor.g, unlockedColor.b, 0.35f);
        }

        // drag the bolt along the track while you hold it
        public void OnDrag(PointerEventData eventData)
        {
            if (locked || track == null || rect == null)
            {
                return;
            }

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(track, eventData.position, eventData.pressEventCamera, out local))
            {
                return;
            }

            float width = track.rect.width;
            if (width <= 0.0001f)
            {
                return;
            }

            // turn the drag x into a clamped track value
            float t = (local.x / width) + track.pivot.x;
            SetT(Mathf.Clamp(t, Mathf.Min(startT, targetT), Mathf.Max(startT, targetT)));
        }

        // let go: snap+lock if close enough and allowed, otherwise slide back to start
        public void OnEndDrag(PointerEventData eventData)
        {
            if (locked)
            {
                return;
            }

            if (Mathf.Abs(currentT - targetT) <= snapThreshold && (canLock == null || canLock()))
            {
                // close enough and allowed: snap in and lock
                SetT(targetT);
                locked = true;

                if (image != null)
                {
                    image.color = lockedColor;
                }

                onLocked?.Invoke();
            }
            else
            {
                // too far, or not this bolt's turn: slide back
                SetT(startT);
            }
        }

        // move the bolt to an x fraction, keeping it on its row
        private void SetT(float t)
        {
            currentT = t;

            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(t, rowY);
            rect.anchorMax = new Vector2(t, rowY);
        }
    }
}
