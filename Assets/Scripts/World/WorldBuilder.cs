using System.Collections.Generic;
using UnityEngine;

namespace CorperLife
{
    /// <summary>
    /// Loads the Blender-made camp, finds the "LOC_" and "SPAWN_" markers in it,
    /// spawns the player and background corpers, and tracks which location the player is in.
    /// </summary>
    public class WorldBuilder : MonoBehaviour
    {
        public const int NpcCount = 28;

        public Transform Player { get; private set; }
        public PlayerController PlayerCtrl { get; private set; }
        public CameraController Cam { get; private set; }
        public bool PlayerIsMoving => PlayerCtrl != null && PlayerCtrl.IsMoving;
        public List<LocationPoint> LocationPoints { get; } = new List<LocationPoint>();

        readonly Dictionary<string, Transform> markers = new Dictionary<string, Transform>();
        readonly List<Renderer> skinRenderers = new List<Renderer>();
        readonly List<GameObject> kitParts = new List<GameObject>();
        readonly List<GameObject> muftiParts = new List<GameObject>();
        readonly List<GameObject> hairM = new List<GameObject>();
        readonly List<GameObject> hairF = new List<GameObject>();

        /// <summary>Turn the model if it ever imports facing backwards (Blender -Y should become Unity +Z).</summary>
        public const float ModelYawOffset = 180f;
        float locTimer;

        public void Build(VirtualJoystick joystick)
        {
            // ---- Camp ----
            var campPrefab = Resources.Load<GameObject>("Models/camp");
            GameObject camp;
            if (campPrefab != null) camp = Instantiate(campPrefab);
            else
            {
                Debug.LogWarning("Camp model missing, using a plain ground.");
                camp = GameObject.CreatePrimitive(PrimitiveType.Plane);
                camp.transform.localScale = new Vector3(20, 1, 20);
            }
            camp.name = "Camp";

            foreach (var t in camp.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("LOC_") || t.name.StartsWith("SPAWN_"))
                {
                    string key = t.name.Split(new[] { "__" }, System.StringSplitOptions.None)[0];
                    key = key.Split('.')[0];
                    markers[key] = t;
                    if (key.StartsWith("LOC_"))
                    {
                        LocationPoints.Add(new LocationPoint
                        {
                            id = key.Substring(4),
                            position = new Vector3(t.position.x, 0, t.position.z),
                            radius = ParseRadius(t.name)
                        });
                    }
                }
            }

            foreach (var mf in camp.GetComponentsInChildren<MeshFilter>(true))
            {
                string n = mf.gameObject.name;
                if (n.Contains("NOCOL") || n.StartsWith("Tree") || n.StartsWith("Flag")) continue;
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }

            // ---- Player ----
            Player = SpawnCorper("Player").transform;
            var cc = Player.gameObject.AddComponent<CharacterController>();
            cc.height = 1.75f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.9f, 0);
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

            var sun = FindSun();
            var cycle = gameObject.AddComponent<DayNightCycle>();
            cycle.sun = sun;

            // ---- Background corpers ----
            if (LocationPoints.Count > 0)
            {
                for (int i = 0; i < NpcCount; i++)
                {
                    var p = LocationPoints[Random.Range(0, LocationPoints.Count)];
                    Vector2 r = Random.insideUnitCircle * p.radius;
                    var npc = SpawnCorper("Corper_" + i);
                    npc.transform.position = p.position + new Vector3(r.x, 0, r.y);
                    npc.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                    DressNpc(npc, i);
                    npc.AddComponent<NpcCorper>().world = this;
                }
            }

            PlacePlayerAt("SPAWN_player");
        }

        static float ParseRadius(string name)
        {
            int i = name.IndexOf("__r", System.StringComparison.Ordinal);
            if (i < 0) return 6f;
            string num = "";
            for (int j = i + 3; j < name.Length && (char.IsDigit(name[j]) || name[j] == '_'); j++)
                num += name[j] == '_' ? '.' : name[j];
            return float.TryParse(num, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var r) ? r : 6f;
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

        /// <summary>Applies skin tone, hair and outfit (mufti before kit collection, NYSC kit after).</summary>
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
            bool kit = rng.Next(10) < 8;
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
                var pos = new Vector3(t.position.x, 0.05f, t.position.z);
                PlayerCtrl.Teleport(pos, Quaternion.Euler(0, t.eulerAngles.y, 0));
            }
            else PlayerCtrl.Teleport(new Vector3(0, 0.05f, 0), Quaternion.identity);
            Cam.SnapBehind();
        }

        public bool TryGetLocation(string id, out Vector3 center, out float radius)
        {
            foreach (var p in LocationPoints)
                if (p.id == id) { center = p.position; radius = p.radius; return true; }
            center = Vector3.zero;
            radius = 0;
            return false;
        }

        public void SetPreview(bool on)
        {
            if (Cam != null) Cam.previewMode = on;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.State == null || Player == null) return;
            locTimer -= Time.deltaTime;
            if (locTimer > 0) return;
            locTimer = 0.25f;

            string inside = null;
            float best = float.MaxValue;
            Vector3 pp = Player.position;
            pp.y = 0;
            foreach (var p in LocationPoints)
            {
                float d = Vector3.Distance(pp, p.position);
                if (d <= p.radius && d < best) { best = d; inside = p.id; }
            }
            if (inside != gm.CurrentLocation) gm.SetLocation(inside);
        }
    }
}
