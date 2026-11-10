using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using NetRunner.Player;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine;

namespace NetRunner.World.Editor
{
    public static partial class HospitalSceneBuilder
    {
        const string SystemsRoot = "Hospital systems — circulation and patrols";

        [MenuItem("NET.runner-92/Install Hospital Systems")]
        public static void InstallHospitalSystems()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            if (GameObject.Find(SystemsRoot)) return;
            var model = GameObject.Find("Northline Medical — imported environment").transform.Find("Hospital");
            var collision = GameObject.Find("Collision — renderer-free").transform.Find("Hospital");
            var player = UnityEngine.Object.FindAnyObjectByType<FirstPersonController>();
            var robot = BuildPatrolPrefab();
            var systems = Node(SystemsRoot);
            var doors = Node("Doors", systems);
            var geometry = model.GetComponentsInChildren<MeshRenderer>().ToDictionary(r => r.name);
            var colliders = collision.GetComponentsInChildren<Collider>().ToDictionary(c => c.name);
            var westLift = new HospitalDoor[4];
            var groups = geometry.Values.Where(r => Regex.IsMatch(r.name, @"^F\d{2}_D\d{2}_"))
                .GroupBy(r => r.name[..7]).OrderBy(g => g.Key);
            int count = 0;
            foreach (var group in groups)
            {
                var frame = group.Single(r => r.name.EndsWith("_Frame"));
                var leaves = group.Where(r => r != frame).OrderBy(r => r.name).ToArray();
                var bounds = frame.bounds;
                bool lateralX = bounds.size.x > bounds.size.z;
                var at = frame.transform.position;
                bool lift = frame.name.Contains("_Lift_Lobby_");
                var root = Node(frame.name[..^6], doors);
                root.SetPositionAndRotation(at, Quaternion.Euler(0, lateralX ? 0 : -90f, 0));
                float width = lateralX ? bounds.size.x : bounds.size.z;
                var panels = new HospitalDoor.Panel[leaves.Length];
                bool hinged = leaves.Length == 1;
                for (int i = 0; i < leaves.Length; i++)
                {
                    var leaf = leaves[i];
                    var leafBounds = leaf.bounds;
                    var oldCollider = colliders[leaf.name];
                    UnityEngine.Object.DestroyImmediate(oldCollider.gameObject);
                    leaf.transform.SetParent(root, true);
                    leaf.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
                    var meshBounds = leaf.GetComponent<MeshFilter>().sharedMesh.bounds;
                    var box = leaf.gameObject.AddComponent<BoxCollider>();
                    box.center = meshBounds.center;
                    box.size = Vector3.Max(meshBounds.size, new Vector3(.045f, .04f, .045f));
                    var open = leaf.transform.localPosition;
                    var openRotation = leaf.transform.localRotation;
                    var shut = open;
                    var shutRotation = openRotation;
                    if (hinged)
                    {
                        Vector3 arm = leafBounds.center - leaf.transform.position;
                        arm.y = 0;
                        float inward = -Mathf.Sign(open.x);
                        float yaw = Vector3.SignedAngle(arm, root.right * inward, Vector3.up);
                        shutRotation = Quaternion.AngleAxis(yaw, Vector3.up) * openRotation;
                    }
                    else
                    {
                        float span = lateralX ? leafBounds.size.x : leafBounds.size.z;
                        float side = Mathf.Sign(open.x);
                        int stack = leaves.Length == 2 ? 0 : (leaf.name[^1] - '1');
                        shut.x = side * span * (stack + .5f);
                        if (lift) open.x = side * span * 1.53f;
                    }
                    panels[i] = new HospitalDoor.Panel { leaf = leaf.transform, closed = shut, open = open,
                        closedRotation = shutRotation, openRotation = openRotation };
                }

                var door = root.gameObject.AddComponent<HospitalDoor>();
                door.Configure(panels, width, hinged, lift, lift && at.x > 0);
                var aim = root.gameObject.AddComponent<BoxCollider>();
                aim.isTrigger = true;
                aim.center = new Vector3(0, bounds.size.y * .49f, 0);
                aim.size = new Vector3(width - .10f, bounds.size.y - .13f, .28f);
                if (lift && at.x < 0) westLift[Mathf.RoundToInt(at.y / StairAccess.FloorHeight)] = door;
                count++;
            }
            if (count != 108 || westLift.Any(d => !d)) throw new InvalidDataException($"Hospital doors {count}, expected 108 with four lift landings.");
            var playerInput = player.GetComponent<HospitalInteraction>();
            if (!playerInput) player.gameObject.AddComponent<HospitalInteraction>();

            var access = Node("Alternating stair access", systems).gameObject.AddComponent<StairAccess>();
            access.Configure(player);
            var panelMetal = SystemMaterial("Isolation panel", new Color(.27f, .34f, .35f), .48f);
            var edge = SystemMaterial("Machine frame", new Color(.13f, .17f, .18f), .62f);
            var amber = SystemMaterial("Isolation amber", new Color(.56f, .26f, .047f), .26f,
                new Color(1f, .38f, .05f) * .7f);
            for (int leg = 0; leg < 3; leg++)
            {
                float core = -StairAccess.CoreForLeg(leg);
                float y = leg * StairAccess.FloorHeight + 2.10f;
                var gate = Node($"Fire isolation · {(core < 0 ? "West" : "East")} L{leg + 1}–L{leg + 2}", access.transform);
                gate.position = new Vector3(core, y, -9.26f);
                Block(gate, "Closed fire bulkhead", new Vector3(0, 1.12f, 0), new Vector3(.18f, 2.3f, 2.52f), panelMetal);
                foreach (float z in new[] { -1.20f, 1.20f })
                    Block(gate, "Guide post", new Vector3(0, 1.18f, z), new Vector3(.29f, 2.36f, .12f), edge, false);
                Block(gate, "Isolation warning stripe", new Vector3(0, 1.36f, 0), new Vector3(.184f, .10f, 2.34f), amber, false);
                for (int end = 0; end < 2; end++)
                {
                    float sign = end == 0 ? -1f : 1f;
                    float level = sign * 1.91f;
                    var closure = Node(end == 0 ? "Lower flight shutter" : "Upper flight shutter", gate);
                    closure.localPosition = new Vector3(sign * 1.185f, level, 3.98f);
                    Block(closure, "Sealed stair flight", new Vector3(0, 1.12f, 0), new Vector3(2.04f, 2.32f, .18f), panelMetal);
                    Block(closure, "Lockout stripe", new Vector3(0, 1.35f, 0), new Vector3(1.93f, .10f, .184f), amber, false);
                }
            }

            MakeLift(systems, player, access, westLift, panelMetal, edge);
            AddResidualPower(systems, geometry.Values.ToArray());
            AddK7Patrols(systems, robot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"NET92 SYSTEMS | {count} closed doors | 3 fire isolations | 1 lift | 4 K-7 patrols");
        }

        public static void BuildInteractiveWindows()
        {
            InstallHospitalSystems();
            BuildWindows("Northline/Builds/Phase_05_Interactive/NETRunner92_Hospital.exe");
        }

        static Material SystemMaterial(string name, Color color, float metallic, Color emission = default)
        {
            string path = Root + "/Environment/Materials/System_" + Safe(name) + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m)
            {
                m = new Material(Shader.Find("HDRP/Lit")) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", .38f);
            m.SetColor("_EmissiveColor", emission);
            HDMaterial.ValidateMaterial(m);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Transform Block(Transform parent, string name, Vector3 position, Vector3 size, Material material,
            bool solid = true)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            block.name = name;
            block.SetParent(parent, false);
            block.localPosition = position;
            block.localScale = size;
            block.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!solid) UnityEngine.Object.DestroyImmediate(block.GetComponent<Collider>());
            return block;
        }

        static void MakeLift(Transform systems, FirstPersonController player, StairAccess access,
            HospitalDoor[] doors, Material metal, Material edge)
        {
            var root = Node("West passenger lift", systems);
            var lift = root.gameObject.AddComponent<HospitalLift>();
            var car = Node("Moving cabin", root);
            car.position = new Vector3(-2f, 0f, 7.87f);
            var lightMat = SystemMaterial("Lift keypad and luminaire", new Color(.25f, .45f, .5f), .14f,
                new Color(.24f, .72f, .85f) * 2f);
            Block(car, "Cabin deck", new Vector3(0, -.075f, -.01f), new Vector3(2.64f, .15f, 3.35f), edge);
            Block(car, "Cabin ceiling", new Vector3(0, 2.56f, 0), new Vector3(2.64f, .12f, 3.34f), metal);
            Block(car, "Cabin rear", new Vector3(0, 1.25f, 1.62f), new Vector3(2.63f, 2.56f, .12f), metal);
            foreach (float side in new[] { -1f, 1f })
            {
                Block(car, "Cabin side wall", new Vector3(side * 1.26f, 1.25f, 0), new Vector3(.12f, 2.55f, 3.26f), metal);
                Block(car, "Front return", new Vector3(side * 1.065f, 1.26f, -1.56f),
                    new Vector3(.40f, 2.52f, .10f), metal);
            }
            Block(car, "Front header", new Vector3(0, 2.41f, -1.56f), new Vector3(1.77f, .22f, .10f), edge);
            Block(car, "Cabin luminaire", new Vector3(0, 2.48f, .10f), new Vector3(.52f, .045f, 1.28f), lightMat, false);
            var cabinLight = Node("Cabin practical", car);
            cabinLight.localPosition = new Vector3(0, 2.37f, .05f);
            var light = cabinLight.gameObject.AddComponent<Light>();
            light.range = 3.4f;
            light.intensity = 135f;
            light.color = new Color(.72f, .87f, 1f);
            var door = Node("Cabin interlock", car);
            door.localPosition = Vector3.back * 1.59f;
            var panels = new HospitalDoor.Panel[2];
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                var panel = Block(door, "Cabin sliding leaf", Vector3.zero, new Vector3(.86f, 2.30f, .052f), metal);
                panels[i] = new HospitalDoor.Panel { leaf = panel,
                    closed = new Vector3(side * .43f, 1.16f, 0), open = new Vector3(side * 1.28f, 1.16f, .11f),
                    closedRotation = Quaternion.identity, openRotation = Quaternion.identity };
            }
            var carDoor = door.gameObject.AddComponent<HospitalDoor>();
            carDoor.Configure(panels, 1.72f, false, true, false);
            lift.Configure(access, player, car, carDoor, doors);
            for (int i = 0; i < 4; i++)
            {
                float y = i * StairAccess.FloorHeight;
                var call = Block(root, $"West lift call L{i + 1}", new Vector3(-.88f, y + 1.28f, 5.98f),
                    new Vector3(.24f, .28f, .12f), lightMat);
                call.gameObject.AddComponent<HospitalLiftButton>().Configure(lift, i, false);
                var button = Block(car, $"Cabin level {i + 1}", new Vector3(.91f, 1.27f + i * .19f, 1.53f),
                    new Vector3(.17f, .14f, .075f), lightMat);
                button.gameObject.AddComponent<HospitalLiftButton>().Configure(lift, i, true);
            }
        }
    }
}
