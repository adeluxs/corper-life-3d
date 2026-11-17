using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CorperLife
{
    /// <summary>A building you can walk into: floor area, door points and roof that hides when you are inside.</summary>
    public class Building
    {
        public string name;
        public Rect area;               // x/z rectangle of the floor
        public Vector3 doorOut;         // just outside the door
        public Vector3 doorIn;          // just inside the door
        public readonly List<Renderer> roofs = new List<Renderer>();

        public bool Contains(Vector3 p, float shrink = 0.15f) =>
            p.x > area.xMin + shrink && p.x < area.xMax - shrink && p.z > area.yMin + shrink && p.z < area.yMax - shrink;
    }

    /// <summary>A named place. Circle (outdoors) or rectangle (inside a building).</summary>
    public class LocationPoint
    {
        public string id;
        public Vector3 position;
        public float radius;
        public bool isBox;
        public float hx, hz;

        public bool Contains(Vector3 p)
        {
            if (isBox) return Mathf.Abs(p.x - position.x) <= hx && Mathf.Abs(p.z - position.z) <= hz;
            p.y = 0;
            return Vector3.Distance(p, position) <= radius;
        }

        public float DistanceTo(Vector3 p)
        {
            p.y = 0;
            return Vector3.Distance(p, position);
        }

        public Vector3 RandomPoint(float fill = 0.8f)
        {
            if (isBox)
                return position + new Vector3(Random.Range(-hx, hx) * fill, 0, Random.Range(-hz, hz) * fill);
            Vector2 r = Random.insideUnitCircle * radius * fill;
            return position + new Vector3(r.x, 0, r.y);
        }
    }

    /// <summary>
    /// Loads a Blender-made area (the camp, later the town), reads its markers
    /// (LOC_, SPAWN_, DOOR_), spawns the player and background corpers,
    /// tracks which location and building the player is in, and hides roofs indoors.
    /// </summary>
    public class WorldBuilder : MonoBehaviour
    {
        public const int NpcCount = 28;

        /// <summary>Turn the model if it ever imports facing backwards (Blender -Y should become Unity +Z).</summary>
        public const float ModelYawOffset = 180f;

        public Transform Player { get; private set; }
        public PlayerController PlayerCtrl { get; private set; }
        public CameraController Cam { get; private set; }
        public bool PlayerIsMoving => PlayerCtrl != null && PlayerCtrl.IsMoving;
        public string AreaName { get; private set; }
        public Building PlayerBuilding { get; private set; }
        public List<LocationPoint> LocationPoints { get; } = new List<LocationPoint>();
        public List<Building> Buildings { get; } = new List<Building>();

        readonly Dictionary<string, Transform> markers = new Dictionary<string, Transform>();
        readonly List<Renderer> skinRenderers = new List<Renderer>();
        readonly List<GameObject> kitParts = new List<GameObject>();
        readonly List<GameObject> muftiParts = new List<GameObject>();
        readonly List<GameObject> hairM = new List<GameObject>();
        readonly List<GameObject> hairF = new List<GameObject>();
        readonly List<GameObject> npcs = new List<GameObject>();
        GameObject area;
        float locTimer;

        static readonly Regex BoxRx = new Regex(@"__b(\d+(?:\.\d+)?)x(\d+(?:\.\d+)?)");
        static readonly Regex RadRx = new Regex(@"__r(\d+)(?:_(\d+))?");

        public void Build(VirtualJoystick joystick)
        {
            // ---- Player ----
            Player = SpawnCorper("Player").transform;
            var cc = Player.gameObject.AddComponent<CharacterController>();
            cc.height = 1.75f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.stepOffset = 0.35f;
            PlayerCtrl = Player.gameObject.AddComponent<PlayerController>();
            CollectCustomisableParts(Player.gameObject);

            // ---- Camera and light ----
            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            var camera = camGo.GetComponent<Camera>();
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 400f;
            Cam = camGo.GetComponent<CameraController>();
            if (Cam == null) Cam = camGo.AddComponent<CameraController>();
            Cam.target = Player;
            PlayerCtrl.Setup(joystick, camGo.transform);

            var cycle = gameObject.AddComponent<DayNightCycle>();
            cycle.sun = FindSun();

            LoadArea("camp");
        }

        /// <summary>Replaces the current map (e.g. "camp" or "town") and respawns the background corpers.</summary>
        public void LoadArea(string modelName)
        {
            if (area != null) Destroy(area);
            foreach (var n in npcs) if (n != null) Destroy(n);
            npcs.Clear();
            markers.Clear();
            LocationPoints.Clear();
            Buildings.Clear();
            PlayerBuilding = null;
            if (Cam != null) Cam.indoor = false;
            AreaName = modelName;

            var prefab = Resources.Load<GameObject>("Models/" + modelName);
            if (prefab != null) area = Instantiate(prefab);
            else
            {
                Debug.LogWarning(modelName + " model missing, using a plain ground.");
                area = GameObject.CreatePrimitive(PrimitiveType.Plane);
                area.transform.localScale = new Vector3(20, 1, 20);
            }
            area.name = modelName;

            var all = area.GetComponentsInChildren<Transform>(true);
            var byName = new Dictionary<string, Transform>();
            foreach (var t in all) byName[t.name] = t;

            foreach (var t in all)
            {
                if (t.name.StartsWith("LOC_") || t.name.StartsWith("SPAWN_"))
                {
                    string key = t.name.Split(new[] { "__" }, System.StringSplitOptions.None)[0].Split('.')[0];
                    markers[key] = t;
                    if (key.StartsWith("LOC_")) LocationPoints.Add(ParseLocation(key.Substring(4), t));
                }
                else if (t.name.StartsWith("DOOR_"))
                {
                    string bname = t.name.Substring(5).Split('.')[0];
                    if (byName.TryGetValue(bname + "_Floor", out var floor)) AddBuilding(bname, floor, t, all);
                }
            }

            foreach (var mf in area.GetComponentsInChildren<MeshFilter>(true))
            {
                string n = mf.gameObject.name;
                if (n.Contains("NOCOL") || n.StartsWith("Tree") || n.StartsWith("Flag")) continue;
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }

            SpawnNpcs();
            PlacePlayerAt("SPAWN_player");
        }

        static LocationPoint ParseLocation(string id, Transform t)
        {
            var lp = new LocationPoint
            {
                id = id,
                position = new Vector3(t.position.x, 0, t.position.z),
                radius = 6f
            };
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var mb = BoxRx.Match(t.name);
            if (mb.Success)
            {
                lp.isBox = true;
                // Half-sizes are symmetric, so the Blender-to-Unity axis flip does not matter.
                lp.hx = float.Parse(mb.Groups[1].Value, inv);
                lp.hz = float.Parse(mb.Groups[2].Value, inv);
                lp.radius = Mathf.Max(lp.hx, lp.hz);
                return lp;
            }
            var mr = RadRx.Match(t.name);
            if (mr.Success)
            {
                string num = mr.Groups[1].Value + (mr.Groups[2].Success ? "." + mr.Groups[2].Value : "");
                lp.radius = float.Parse(num, inv);
            }
            return lp;
        }

        void AddBuilding(string bname, Transform floor, Transform door, Transform[] all)
        {
            var rend = floor.GetComponent<Renderer>();
            if (rend == null) return;
            var b = rend.bounds;
            var bld = new Building
            {
                name = bname,
                area = new Rect(b.min.x, b.min.z, b.size.x, b.size.z),
                doorOut = new Vector3(door.position.x, 0, door.position.z)
            };
            // Step straight through the wall on the side the door is on.
            float dx = bld.doorOut.x - b.center.x;
            float dz = bld.doorOut.z - b.center.z;
            bool xSide = Mathf.Abs(dx) - b.extents.x > Mathf.Abs(dz) - b.extents.z;
            Vector3 inward = xSide ? new Vector3(-Mathf.Sign(dx), 0, 0) : new Vector3(0, 0, -Mathf.Sign(dz));
            bld.doorIn = bld.doorOut + inward * 3.6f;

            foreach (var t in all)
                if (t.name.StartsWith(bname + "_Roof"))
                {
                    var r = t.GetComponent<Renderer>();
                    if (r != null) bld.roofs.Add(r);
                }
            Buildings.Add(bld);
        }

        public Building BuildingAt(Vector3 p)
        {
            foreach (var b in Buildings) if (b.Contains(p)) return b;
            return null;
        }

        /// <summary>A walking route that goes through doors instead of through walls.</summary>
        public List<Vector3> Route(Vector3 from, Vector3 to)
        {
            var path = new List<Vector3>();
            var a = BuildingAt(from);
            var b = BuildingAt(to);
            if (a != null && a != b) { path.Add(a.doorIn); path.Add(a.doorOut); }
            if (b != null && b != a) { path.Add(b.doorOut); path.Add(b.doorIn); }
            path.Add(to);
            return path;
        }

        void SpawnNpcs()
        {
            if (LocationPoints.Count == 0) return;
            for (int i = 0; i < NpcCount; i++)
            {
                var p = LocationPoints[Random.Range(0, LocationPoints.Count)];
                var npc = SpawnCorper("Corper_" + i);
                npc.transform.position = p.RandomPoint();
                npc.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                DressNpc(npc, i);
                npc.AddComponent<NpcCorper>().world = this;
                npcs.Add(npc);
            }
        }

        GameObject SpawnCorper(string name)
        {
            var prefab = Resources.Load<GameObject>("Models/corper");
            GameObject go;
            if (prefab != null)
            {
                go = new GameObject();
                var model = Instantiate(prefab, go.transform);
                model.name = "Model";
                // Keep the FBX's own upright rotation and only add the facing offset on top.
                model.transform.localRotation = Quaternion.Euler(0, ModelYawOffset, 0) * prefab.transform.localRotation;
            }
            else
            {
                go = new GameObject();
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.name = "Body";
                capsule.transform.SetParent(go.transform, false);
                capsule.transform.localPosition = new Vector3(0, 0.9f, 0);
                Destroy(capsule.GetComponent<Collider>());
            }
            go.name = name;
            foreach (var col in go.GetComponentsInChildren<Collider>()) Destroy(col);
            go.AddComponent<LimbAnimator>();
            return go;
        }

        void CollectCustomisableParts(GameObject root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name;
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.name.StartsWith("Skin")) { skinRenderers.Add(r); break; }
                if (n.Contains("Kit")) kitParts.Add(r.gameObject);
                if (n.Contains("Mufti")) muftiParts.Add(r.gameObject);
                if (n.StartsWith("HairM")) hairM.Add(r.gameObject);
                if (n.StartsWith("HairF")) hairF.Add(r.gameObject);
            }
        }

        /// <summary>Applies skin tone, hair and outfit (mufti before kit collection, camp whites after).</summary>
        public void ApplyProfile(PlayerProfile p, bool wearingKit)
        {
            Color skin = Palette.SkinTones[Mathf.Clamp(p.skinTone, 0, Palette.SkinTones.Length - 1)];
            foreach (var r in skinRenderers)
            {
                var mats = r.materials;
                foreach (var m in mats)
                    if (m.name.StartsWith("Skin")) m.color = skin;
            }
            foreach (var g in kitParts) g.SetActive(wearingKit);
            foreach (var g in muftiParts) g.SetActive(!wearingKit);
            foreach (var g in hairM) g.SetActive(!p.isFemale);
            foreach (var g in hairF) g.SetActive(p.isFemale);
        }

        void DressNpc(GameObject npc, int seed)
        {
            var rng = new System.Random(seed * 7919);
            Color skin = Palette.SkinTones[rng.Next(Palette.SkinTones.Length)];
            bool female = rng.Next(2) == 0;
            // In camp most corpers are in whites; in town most people wear mufti.
            bool kit = AreaName == "camp" ? rng.Next(10) < 8 : rng.Next(10) < 2;
            foreach (var r in npc.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name;
                if (n.Contains("Kit")) r.gameObject.SetActive(kit);
                if (n.Contains("Mufti")) r.gameObject.SetActive(!kit);
                if (n.StartsWith("HairM")) r.gameObject.SetActive(!female);
                if (n.StartsWith("HairF")) r.gameObject.SetActive(female);
                var mats = r.materials;
                foreach (var m in mats)
                    if (m.name.StartsWith("Skin")) m.color = skin;
            }
            npc.transform.localScale = Vector3.one * (female ? 0.95f : 1f) * (float)(0.94 + rng.NextDouble() * 0.12);
        }

        Light FindSun()
        {
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) return l;
            var go = new GameObject("Sun");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            return light;
        }

        public void PlacePlayerAt(string marker)
        {
            if (Player == null) return;
            if (markers.TryGetValue(marker, out var t))
            {
                var pos = new Vector3(t.position.x, 0.25f, t.position.z);
                PlayerCtrl.Teleport(pos, Quaternion.Euler(0, t.eulerAngles.y, 0));
            }
            else PlayerCtrl.Teleport(new Vector3(0, 0.25f, 0), Quaternion.identity);
            Cam.SnapBehind();
        }

        public bool HasMarker(string marker) => markers.ContainsKey(marker);

        /// <summary>A location with this id. When there are several (four hostels), one is picked at random.</summary>
        public LocationPoint FindLocation(string id)
        {
            var matches = new List<LocationPoint>();
            foreach (var p in LocationPoints) if (p.id == id) matches.Add(p);
            return matches.Count == 0 ? null : matches[Random.Range(0, matches.Count)];
        }

        public bool TryGetLocation(string id, out Vector3 center, out float radius)
        {
            var p = FindLocation(id);
            center = p != null ? p.position : Vector3.zero;
            radius = p != null ? p.radius : 0f;
            return p != null;
        }

        public void SetPreview(bool on)
        {
            if (Cam != null) Cam.previewMode = on;
        }

        void Update()
        {
            if (Player == null) return;

            // Roofs: hide the roof of the building you are in.
            var inside = BuildingAt(Player.position);
            if (inside != PlayerBuilding)
            {
                if (PlayerBuilding != null) foreach (var r in PlayerBuilding.roofs) if (r) r.enabled = true;
                if (inside != null) foreach (var r in inside.roofs) if (r) r.enabled = false;
                PlayerBuilding = inside;
                if (Cam != null) Cam.indoor = inside != null;
            }

            var gm = GameManager.I;
            if (gm == null || gm.State == null) return;
            locTimer -= Time.deltaTime;
            if (locTimer > 0) return;
            locTimer = 0.25f;

            string found = null;
            float best = float.MaxValue;
            foreach (var p in LocationPoints)
            {
                if (!p.Contains(Player.position)) continue;
                // Rooms win over outdoor circles; otherwise the nearest centre wins.
                float d = p.DistanceTo(Player.position) - (p.isBox ? 1000f : 0f);
                if (d < best) { best = d; found = p.id; }
            }
            if (found != gm.CurrentLocation) gm.SetLocation(found);
        }
    }
}
