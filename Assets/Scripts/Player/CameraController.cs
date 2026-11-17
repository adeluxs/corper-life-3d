using UnityEngine;
using UnityEngine.EventSystems;

namespace CorperLife
{
    /// <summary>Third-person orbit camera. Drag anywhere not on a button to look around.</summary>
    public class CameraController : MonoBehaviour
    {
        public Transform target;
        public float distance = 6.5f;
        public float height = 1.6f;
        public float yaw;
        public float pitch = 18f;
        public bool previewMode;   // close-up front view for the character creator
        public bool indoor;        // inside a building: look down into the room (roof is hidden)

        float indoorBlend;

        int dragFinger = -1;
        Vector2 lastPos;

        void LateUpdate()
        {
            if (target == null) return;

            if (previewMode)
            {
                Vector3 front = target.position + target.forward * 3.2f + Vector3.up * 1.15f + target.right * -0.9f;
                transform.position = Vector3.Lerp(transform.position, front, Time.deltaTime * 6f);
                transform.LookAt(target.position + Vector3.up * 1.0f + target.right * -0.9f);
                return;
            }

            HandleLook();

            // Indoors the camera rises above the walls and looks down, like a doll's house.
            indoorBlend = Mathf.MoveTowards(indoorBlend, indoor ? 1f : 0f, Time.deltaTime * 2.5f);
            float usePitch = Mathf.Lerp(pitch, Mathf.Max(pitch, 56f), indoorBlend);
            float useDist = Mathf.Lerp(distance, 8.5f, indoorBlend);

            var rot = Quaternion.Euler(usePitch, yaw, 0);
            Vector3 pivot = target.position + Vector3.up * height;
            Vector3 desired = pivot - rot * Vector3.forward * useDist;

            // Outdoors, pull in if a wall is between the camera and the player.
            if (indoorBlend < 0.5f && Physics.SphereCast(pivot, 0.25f, (desired - pivot).normalized, out var hit, useDist, ~0, QueryTriggerInteraction.Ignore))
                if (hit.transform != target && !hit.transform.IsChildOf(target))
                    desired = pivot + (desired - pivot).normalized * Mathf.Max(1.2f, hit.distance - 0.2f);

            transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * 10f);
            transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        }

        void HandleLook()
        {
            var gm = GameManager.I;
            if (gm != null && gm.Paused) { dragFinger = -1; return; }

            Vector2 delta = Vector2.zero;

            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Began && dragFinger < 0 &&
                        (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(t.fingerId)))
                    {
                        dragFinger = t.fingerId;
                        lastPos = t.position;
                    }
                    else if (t.fingerId == dragFinger)
                    {
                        if (t.phase == TouchPhase.Moved) { delta = t.position - lastPos; lastPos = t.position; }
                        if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) dragFinger = -1;
                    }
                }
            }
            else if (Input.GetMouseButton(1) ||
                     (Input.GetMouseButton(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())))
            {
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) lastPos = Input.mousePosition;
                delta = (Vector2)Input.mousePosition - lastPos;
                lastPos = Input.mousePosition;
            }

            float scale = 180f / Mathf.Max(Screen.height, 1);
            yaw += delta.x * scale;
            pitch = Mathf.Clamp(pitch - delta.y * scale * 0.6f, 5f, 60f);
        }

        public void SnapBehind()
        {
            if (target == null) return;
            yaw = target.eulerAngles.y;
            var rot = Quaternion.Euler(pitch, yaw, 0);
            Vector3 pivot = target.position + Vector3.up * height;
            transform.position = pivot - rot * Vector3.forward * distance;
            transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        }
    }
}
