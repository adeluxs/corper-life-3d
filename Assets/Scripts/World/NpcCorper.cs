using System.Collections.Generic;
using UnityEngine;

namespace CorperLife
{
    /// <summary>
    /// A background person. In camp they follow the timetable: parade ground at drill,
    /// the hall at lectures, hostels at night. In town they go about their day.
    /// They walk through doors, not walls.
    /// </summary>
    public class NpcCorper : MonoBehaviour
    {
        public WorldBuilder world;
        LimbAnimator anim;
        readonly List<Vector3> route = new List<Vector3>();
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

            ScheduleSlot slot = null;
            if (gm.State.stage == LifeStage.Camp) slot = CampSchedule.Current(gm.State.minuteOfDay);
            string slotId = slot?.id ?? TownSlot(gm.State);
            if (slotId != lastSlot)
            {
                lastSlot = slotId;
                PickGoal(slot);
            }

            if (route.Count == 0)
            {
                if (anim) anim.speed01 = Mathf.MoveTowards(anim.speed01, 0, Time.deltaTime * 4);
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0 && (slot == null || slot.location == null)) PickGoal(slot);
                return;
            }

            Vector3 goal = route[0];
            Vector3 to = goal - transform.position;
            to.y = 0;
            if (to.magnitude < 0.35f)
            {
                route.RemoveAt(0);
                return;
            }

            var dir = to.normalized;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 360 * Time.deltaTime);
            var pos = transform.position + dir * speed * Time.deltaTime;
            pos.y = world.BuildingAt(pos) != null ? 0.18f : 0f;   // step up onto building floors
            transform.position = pos;
            if (anim)
            {
                anim.speed01 = Mathf.MoveTowards(anim.speed01, speed / 1.7f, Time.deltaTime * 4);
                anim.SetMarching(slotId == "drill");
            }
        }

        static string TownSlot(GameState s)
        {
            int h = s.Hour;
            if (h < 6 || h >= 21) return "night";
            if (h < 8) return "morning";
            if (h < 16) return "work";
            return "evening";
        }

        void PickGoal(ScheduleSlot slot)
        {
            LocationPoint target = null;
            if (slot != null && slot.location != null) target = world.FindLocation(slot.location);

            if (target == null && world.AreaName != "camp")
            {
                // Town routine: home at night, work in the day, market or wandering in the evening.
                string slotName = lastSlot ?? "work";
                string[] options =
                    slotName == "night" ? new[] { "lodge", "home" } :
                    slotName == "work" ? new[] { "ppa_school", "ppa_office", "ppa_hospital", "ppa_lga", "market", "street" } :
                    new[] { "market", "street", "buka", "lodge" };
                target = world.FindLocation(options[Random.Range(0, options.Length)]);
            }

            if (target == null && world.LocationPoints.Count > 0)
                target = world.LocationPoints[Random.Range(0, world.LocationPoints.Count)];
            if (target == null) return;

            route.Clear();
            route.AddRange(world.Route(transform.position, target.RandomPoint(0.75f)));
            waitTimer = Random.Range(3f, 12f);
        }
    }
}
