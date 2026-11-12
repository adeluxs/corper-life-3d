using System.Collections.Generic;
using UnityEngine;

namespace CorperLife
{
    /// <summary>
    /// A background corps member. Follows the camp timetable: crowds the parade ground at drill,
    /// the hall at lectures, and wanders the market and hostels in free time.
    /// </summary>
    public class NpcCorper : MonoBehaviour
    {
        public WorldBuilder world;
        LimbAnimator anim;
        Vector3 goal;
        float waitTimer;
        float speed;
        string lastSlot;

        void Start()
        {
            anim = GetComponentInChildren<LimbAnimator>();
            speed = Random.Range(1.1f, 1.7f);
            PickGoal(null);
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.State == null || gm.Paused) { if (anim) anim.speed01 = 0; return; }

            var slot = CampSchedule.Current(gm.State.minuteOfDay);
            string slotId = slot?.id;
            if (slotId != lastSlot)
            {
                lastSlot = slotId;
                PickGoal(slot);
            }

            Vector3 to = goal - transform.position;
            to.y = 0;
            if (to.magnitude < 0.4f)
            {
                if (anim) anim.speed01 = Mathf.MoveTowards(anim.speed01, 0, Time.deltaTime * 4);
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0 && (slot == null || slot.location == null)) PickGoal(slot);
                return;
            }

            var dir = to.normalized;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 360 * Time.deltaTime);
            transform.position += dir * speed * Time.deltaTime;
            if (anim) anim.speed01 = Mathf.MoveTowards(anim.speed01, speed / 1.7f, Time.deltaTime * 4);
            if (anim) anim.SetMarching(slotId == "drill");
        }

        void PickGoal(ScheduleSlot slot)
        {
            Vector3 center;
            float spread;
            if (slot != null && slot.location != null && world.TryGetLocation(slot.location, out center, out spread))
            {
                spread *= 0.8f;
            }
            else
            {
                var all = world.LocationPoints;
                var pick = all[Random.Range(0, all.Count)];
                center = pick.position;
                spread = pick.radius * 0.8f;
            }
            Vector2 r = Random.insideUnitCircle * spread;
            goal = center + new Vector3(r.x, 0, r.y);
            waitTimer = Random.Range(3f, 12f);
        }
    }

    public class LocationPoint
    {
        public string id;
        public Vector3 position;
        public float radius;
    }
}
