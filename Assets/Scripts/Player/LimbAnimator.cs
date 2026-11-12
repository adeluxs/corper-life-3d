using UnityEngine;

namespace CorperLife
{
    /// <summary>
    /// Procedural walk cycle: swings arms and legs and bobs the body.
    /// The Blender character has separate limbs pivoted at the hips and shoulders.
    /// A full skeletal rig with recorded animations can replace this later.
    /// </summary>
    public class LimbAnimator : MonoBehaviour
    {
        public float speed01;   // 0 = idle, 1 = full walk

        Transform legL, legR, armL, armR, body;
        Quaternion legLBase, legRBase, armLBase, armRBase;
        Vector3 bodyBase;
        float phase;
        float marchBoost;

        void Awake()
        {
            legL = Find("LegL"); legR = Find("LegR");
            armL = Find("ArmL"); armR = Find("ArmR");
            body = Find("Body");
            if (legL) legLBase = legL.localRotation;
            if (legR) legRBase = legR.localRotation;
            if (armL) armLBase = armL.localRotation;
            if (armR) armRBase = armR.localRotation;
            if (body) bodyBase = body.localPosition;
            phase = Random.value * 10f;
        }

        Transform Find(string name)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == name || t.name.StartsWith(name + ".")) return t;
            return null;
        }

        /// <summary>Exaggerated arm swing for parade marching.</summary>
        public void SetMarching(bool on) => marchBoost = on ? 1f : 0f;

        void Update()
        {
            phase += Time.deltaTime * Mathf.Lerp(0f, 9f, speed01);
            float swing = Mathf.Sin(phase) * 32f * speed01;
            float armSwing = swing * (0.8f + marchBoost * 0.9f);

            if (legL) legL.localRotation = legLBase * Quaternion.AngleAxis(swing, Vector3.right);
            if (legR) legR.localRotation = legRBase * Quaternion.AngleAxis(-swing, Vector3.right);
            if (armL) armL.localRotation = armLBase * Quaternion.AngleAxis(-armSwing, Vector3.right);
            if (armR) armR.localRotation = armRBase * Quaternion.AngleAxis(armSwing, Vector3.right);

            if (body)
            {
                float idleBreath = Mathf.Sin(Time.time * 1.6f + phase) * 0.006f * (1f - speed01);
                float bob = Mathf.Abs(Mathf.Sin(phase)) * 0.035f * speed01;
                body.localPosition = bodyBase + new Vector3(0, bob + idleBreath, 0);
            }
        }
    }
}
