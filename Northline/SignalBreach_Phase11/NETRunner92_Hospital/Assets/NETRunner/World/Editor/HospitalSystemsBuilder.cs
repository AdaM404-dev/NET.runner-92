using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NetRunner.Enemy.Editor;
using NetRunner.Player;
using NetRunner.SceneBuilding.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetRunner.World.Editor
{
    /// <summary>
    /// Installs the hospital's working systems in the saved scene: turns the
    /// source model's 108 door frames and leaves into <see cref="HospitalDoor"/>s,
    /// gives the player <see cref="HospitalInteraction"/>, adds the stair
    /// access with the fire isolation that closes the flights not yet
    /// reached, the west lift, residual power and the four K-7 patrols. Does
    /// nothing when the systems are already there.
    /// </summary>
    public static class HospitalSystemsBuilder
    {
        private const string DoorPattern = @"^F\d{2}_D\d{2}_";
        private const string FrameSuffix = "_Frame";
        private const string LiftLobbyMark = "_Lift_Lobby_";
        private const int DoorKeyLength = 7;
        private const int ExpectedDoors = 108;
        private const int LiftLandings = 4;

        // Door leaves get a thin box collider at least this size; the aiming
        // trigger fills the frame, 10 cm narrower and 13 cm lower, 28 cm deep.
        private static readonly Vector3 ThinnestLeaf = new Vector3(.045f, .04f, .045f);
        private const float AimInset = .10f;
        private const float AimHeadroom = .13f;
        private const float AimDepth = .28f;
        private const float AimCentre = .49f;

        // Sliding lift leaves open to 1.53 widths from the centre.
        private const float LiftLeafTravel = 1.53f;

        // The fire isolation: on each closed flight, 2.10 m above the lower
        // floor at z = -9.26, a bulkhead and shutters on both flights.
        private const int IsolatedLegs = 3;
        private const float IsolationHeight = 2.10f;
        private const float IsolationZ = -9.26f;
        private const float ShutterSide = 1.185f;
        private const float ShutterLevel = 1.91f;
        private const float ShutterZ = 3.98f;

        private static readonly float[] GuidePosts = { -1.20f, 1.20f };
        private static readonly Color PanelColor = new Color(.27f, .34f, .35f);
        private static readonly Color FrameColor = new Color(.13f, .17f, .18f);
        private static readonly Color AmberColor = new Color(.56f, .26f, .047f);
        private static readonly Color AmberGlow = new Color(1f, .38f, .05f) * .7f;
        private const float PanelMetallic = .48f;
        private const float FrameMetallic = .62f;
        private const float AmberMetallic = .26f;

        /// <summary>Installs the systems in the hospital scene and saves it.</summary>
        [MenuItem("NET.runner-92/Install Hospital Systems")]
        public static void Install()
        {
            Scene scene = EditorSceneManager.OpenScene(HospitalAssets.ScenePath);
            if (GameObject.Find(HospitalHierarchy.Systems))
            {
                return;
            }

            Transform model = GameObject.Find(HospitalHierarchy.ImportedEnvironment).transform.Find(HospitalHierarchy.Hospital);
            Transform collision = GameObject.Find(HospitalHierarchy.Collision).transform.Find(HospitalHierarchy.Hospital);
            FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
            GameObject robot = K7PatrolBuilder.BuildPrefab();
            Transform systems = SceneParts.Node(HospitalHierarchy.Systems);
            Dictionary<string, MeshRenderer> geometry = model.GetComponentsInChildren<MeshRenderer>().ToDictionary(r => r.name);
            Dictionary<string, Collider> colliders = collision.GetComponentsInChildren<Collider>().ToDictionary(c => c.name);
            HospitalDoor[] westLift = AddDoors(SceneParts.Node("Doors", systems), geometry, colliders, out int count);
            if (!player.GetComponent<HospitalInteraction>())
            {
                player.gameObject.AddComponent<HospitalInteraction>();
            }

            StairAccess access = SceneParts.Node("Alternating stair access", systems).gameObject.AddComponent<StairAccess>();
            access.Configure(player);
            Material panel = SceneParts.SystemMaterial("Isolation panel", PanelColor, PanelMetallic);
            Material frame = SceneParts.SystemMaterial("Machine frame", FrameColor, FrameMetallic);
            Material amber = SceneParts.SystemMaterial("Isolation amber", AmberColor, AmberMetallic, AmberGlow);
            AddFireIsolation(access.transform, panel, frame, amber);
            HospitalLiftBuilder.Add(systems, player, access, westLift, panel, frame);
            ResidualPowerBuilder.Add(systems, geometry.Values.ToArray());
            K7PatrolBuilder.AddPatrols(systems, robot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"NET92 SYSTEMS | {count} closed doors | 3 fire isolations | 1 lift | 4 K-7 patrols");
        }

        // Each door in the source model is a frame and one to three leaves
        // named F<floor>_D<number>_…. A single leaf swings on hinges; pairs
        // and stacks slide apart. Returns the west lift's four landing doors.
        private static HospitalDoor[] AddDoors(Transform doors, Dictionary<string, MeshRenderer> geometry,
            Dictionary<string, Collider> colliders, out int count)
        {
            var westLift = new HospitalDoor[LiftLandings];
            IOrderedEnumerable<IGrouping<string, MeshRenderer>> groups = geometry.Values.Where(r => Regex.IsMatch(r.name, DoorPattern))
                .GroupBy(r => r.name[..DoorKeyLength]).OrderBy(g => g.Key);
            count = 0;
            foreach (IGrouping<string, MeshRenderer> group in groups)
            {
                MeshRenderer frame = group.Single(r => r.name.EndsWith(FrameSuffix));
                MeshRenderer[] leaves = group.Where(r => r != frame).OrderBy(r => r.name).ToArray();
                Bounds bounds = frame.bounds;
                bool isLateral = bounds.size.x > bounds.size.z;
                Vector3 at = frame.transform.position;
                bool isLift = frame.name.Contains(LiftLobbyMark);
                Transform root = SceneParts.Node(frame.name[..^FrameSuffix.Length], doors);
                root.SetPositionAndRotation(at, Quaternion.Euler(0, isLateral ? 0 : -90f, 0));
                float width = isLateral ? bounds.size.x : bounds.size.z;
                bool isHinged = leaves.Length == 1;
                var panels = new HospitalDoor.Panel[leaves.Length];
                for (int i = 0; i < leaves.Length; i++)
                {
                    panels[i] = Leaf(leaves[i], leaves.Length, root, colliders, isHinged, isLateral, isLift);
                }

                HospitalDoor door = root.gameObject.AddComponent<HospitalDoor>();
                door.Configure(panels, width, isHinged, isLift, isLift && at.x > 0);
                BoxCollider aim = root.gameObject.AddComponent<BoxCollider>();
                aim.isTrigger = true;
                aim.center = new Vector3(0, bounds.size.y * AimCentre, 0);
                aim.size = new Vector3(width - AimInset, bounds.size.y - AimHeadroom, AimDepth);
                if (isLift && at.x < 0)
                {
                    westLift[Mathf.RoundToInt(at.y / StairAccess.FloorHeight)] = door;
                }

                count++;
            }

            if (count != ExpectedDoors || westLift.Any(d => !d))
            {
                throw new InvalidDataException($"Hospital doors {count}, expected 108 with four lift landings.");
            }

            return westLift;
        }

        // Moves a leaf under its door with a box collider of its own and works
        // out its shut pose: the model shows doors open, so a hinged leaf
        // turns back across the frame and a sliding one moves to the centre.
        private static HospitalDoor.Panel Leaf(MeshRenderer leaf, int leafCount, Transform root, Dictionary<string, Collider> colliders,
            bool isHinged, bool isLateral, bool isLift)
        {
            Bounds leafBounds = leaf.bounds;
            Object.DestroyImmediate(colliders[leaf.name].gameObject);
            leaf.transform.SetParent(root, true);
            leaf.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
            Bounds meshBounds = leaf.GetComponent<MeshFilter>().sharedMesh.bounds;
            BoxCollider box = leaf.gameObject.AddComponent<BoxCollider>();
            box.center = meshBounds.center;
            box.size = Vector3.Max(meshBounds.size, ThinnestLeaf);
            Vector3 open = leaf.transform.localPosition;
            Quaternion openRotation = leaf.transform.localRotation;
            Vector3 shut = open;
            Quaternion shutRotation = openRotation;
            if (isHinged)
            {
                Vector3 arm = leafBounds.center - leaf.transform.position;
                arm.y = 0;
                float inward = -Mathf.Sign(open.x);
                float yaw = Vector3.SignedAngle(arm, root.right * inward, Vector3.up);
                shutRotation = Quaternion.AngleAxis(yaw, Vector3.up) * openRotation;
            }
            else
            {
                float span = isLateral ? leafBounds.size.x : leafBounds.size.z;
                float side = Mathf.Sign(open.x);
                int stack = leafCount == 2 ? 0 : (leaf.name[^1] - '1');
                shut.x = side * span * (stack + .5f);
                if (isLift)
                {
                    open.x = side * span * LiftLeafTravel;
                }
            }

            return new HospitalDoor.Panel(leaf.transform, shut, open, shutRotation, openRotation);
        }

        // On each of the three flights between floors: a closed bulkhead across
        // the landing with guide posts and an amber stripe, and a shutter at
        // the lower and the upper end of the flight.
        private static void AddFireIsolation(Transform access, Material panel, Material frame, Material amber)
        {
            for (int leg = 0; leg < IsolatedLegs; leg++)
            {
                float core = -StairAccess.CoreForLeg(leg);
                float y = leg * StairAccess.FloorHeight + IsolationHeight;
                Transform gate = SceneParts.Node($"Fire isolation · {(core < 0 ? "West" : "East")} L{leg + 1}–L{leg + 2}", access);
                gate.position = new Vector3(core, y, IsolationZ);
                SceneParts.Block(gate, "Closed fire bulkhead", new Vector3(0, 1.12f, 0), new Vector3(.18f, 2.3f, 2.52f), panel);
                foreach (float z in GuidePosts)
                {
                    SceneParts.Block(gate, "Guide post", new Vector3(0, 1.18f, z), new Vector3(.29f, 2.36f, .12f), frame, false);
                }

                SceneParts.Block(gate, "Isolation warning stripe", new Vector3(0, 1.36f, 0), new Vector3(.184f, .10f, 2.34f), amber, false);
                for (int end = 0; end < 2; end++)
                {
                    float sign = end == 0 ? -1f : 1f;
                    Transform closure = SceneParts.Node(end == 0 ? "Lower flight shutter" : "Upper flight shutter", gate);
                    closure.localPosition = new Vector3(sign * ShutterSide, sign * ShutterLevel, ShutterZ);
                    SceneParts.Block(closure, "Sealed stair flight", new Vector3(0, 1.12f, 0), new Vector3(2.04f, 2.32f, .18f), panel);
                    SceneParts.Block(closure, "Lockout stripe", new Vector3(0, 1.35f, 0), new Vector3(1.93f, .10f, .184f), amber, false);
                }
            }
        }
    }
}
