using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting;

namespace SaferTogether.UnityClient
{
    // dial that turns dragging into a number from 0 to 99
    [Preserve]
    public sealed class RotaryDial : MonoBehaviour, IDragHandler
    {
        private RectTransform dial;
        private RectTransform area;     // parent, used to turn the pointer into local coords
        private int current;
        private Action<int> onNumberChanged;

        public int Current => current;

        // set up the dial and the callback it fires when the number changes
        public void Configure(RectTransform dialRect, Action<int> numberChanged)
        {
            dial = dialRect;
            area = dial.parent as RectTransform;
            onNumberChanged = numberChanged;
            current = 0;
        }

        // spin the dial to follow the finger and work out the 0-99 number
        public void OnDrag(PointerEventData eventData)
        {
            if (dial == null || area == null)
            {
                return;
            }

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out local))
            {
                return;
            }

            Vector2 direction = local - dial.anchoredPosition;
            if (direction.sqrMagnitude < 1f)
            {
                return;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg; // 0 = right, 90 = up
            dial.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);          // point the marker at the finger

            float fromTop = (((90f - angle) % 360f) + 360f) % 360f;              // 0 at the top, going clockwise
            int number = Mathf.RoundToInt(fromTop / 3.6f) % 100;                 // 360 / 100 = 3.6 deg per number

            if (number == current)
            {
                return;
            }

            current = number;
            onNumberChanged?.Invoke(number);
        }
    }
}
