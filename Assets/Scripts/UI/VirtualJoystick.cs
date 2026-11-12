using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CorperLife
{
    /// <summary>On-screen thumbstick for walking. Keyboard WASD also works in the editor.</summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        RectTransform baseRt, knob;
        Vector2 input;
        const float Radius = 120f;

        public Vector2 Value
        {
            get
            {
                Vector2 kb = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                return kb.sqrMagnitude > 0.01f ? Vector2.ClampMagnitude(kb, 1f) : input;
            }
        }

        public void Build()
        {
            var hit = gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0); // invisible touch area

            var b = UIFactory.Panel("Base", transform, new Color(1, 1, 1, 0.12f));
            baseRt = b.rectTransform;
            baseRt.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            baseRt.sizeDelta = new Vector2(Radius * 2, Radius * 2);
            b.raycastTarget = false;

            var k = UIFactory.Panel("Knob", baseRt, new Color(1, 1, 1, 0.45f));
            knob = k.rectTransform;
            knob.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            knob.sizeDelta = new Vector2(110, 110);
            k.raycastTarget = false;
        }

        public void OnPointerDown(PointerEventData e) => OnDrag(e);

        public void OnDrag(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRt, e.position, e.pressEventCamera, out var local);
            var clamped = Vector2.ClampMagnitude(local, Radius);
            knob.anchoredPosition = clamped;
            input = clamped / Radius;
        }

        public void OnPointerUp(PointerEventData e)
        {
            input = Vector2.zero;
            knob.anchoredPosition = Vector2.zero;
        }

        void OnDisable() => input = Vector2.zero;
    }
}
