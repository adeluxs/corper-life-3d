using UnityEngine;

namespace CorperLife
{
    /// <summary>Third-person walking, relative to the camera.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float walkSpeed = 3.4f;
        public float turnSpeed = 720f;

        public bool IsMoving { get; private set; }

        CharacterController cc;
        LimbAnimator anim;
        VirtualJoystick stick;
        Transform cam;
        float verticalVel;

        public void Setup(VirtualJoystick joystick, Transform cameraTransform)
        {
            stick = joystick;
            cam = cameraTransform;
        }

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            anim = GetComponentInChildren<LimbAnimator>();
        }

        void Update()
        {
            var gm = GameManager.I;
            Vector2 input = (stick != null && gm != null && !gm.Paused) ? stick.Value : Vector2.zero;

            Vector3 move = Vector3.zero;
            if (input.sqrMagnitude > 0.01f && cam != null)
            {
                Vector3 fwd = cam.forward; fwd.y = 0; fwd.Normalize();
                Vector3 right = cam.right; right.y = 0; right.Normalize();
                move = fwd * input.y + right * input.x;
                if (move.sqrMagnitude > 1f) move.Normalize();

                var target = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
            }

            // A tired corper walks slower.
            float energyFactor = gm != null && gm.State != null ? Mathf.Lerp(0.6f, 1f, gm.State.needs.energy / 60f) : 1f;
            Vector3 velocity = move * walkSpeed * energyFactor;

            if (cc.isGrounded) verticalVel = -1f;
            else verticalVel -= 20f * Time.deltaTime;
            velocity.y = verticalVel;

            cc.Move(velocity * Time.deltaTime);
            IsMoving = move.sqrMagnitude > 0.01f;
            if (anim != null) anim.speed01 = Mathf.MoveTowards(anim.speed01, move.magnitude, Time.deltaTime * 6f);
        }

        public void Teleport(Vector3 pos, Quaternion rot)
        {
            cc.enabled = false;
            transform.SetPositionAndRotation(pos, rot);
            cc.enabled = true;
        }
    }
}
