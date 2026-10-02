using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace NetRunner.MenuPrototype.Editor
{
    // Rebuilds only this prototype scene. Existing scene/build settings are never saved.
    public static class MenuPrototypeBuilder
    {
        public const string Folder = "Assets/NETRunner/MainMenu";
        public const string ScenePath = Folder + "/Scenes/NETRunner_MainMenu_Prototype.unity";
        private static Transform environment;
        private static Material concrete;
        private static Material steel;
        private static Material graphite;
        private static Material amber;
        private static Material ceramic;
        private static Material lightStrip;
        private static Material screen;
        private static Material rust;

        [MenuItem("NET.runner/Menu Prototype/Create or rebuild standalone scene")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Stop Play mode before rebuilding the prototype.");
            }
            // Refuse dirty scenes rather than saving or discarding unrelated work.
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                {
                    throw new InvalidOperationException("Save or close your unsaved scene before rebuilding. This tool never saves existing scenes.");
                }
            }
            foreach (string child in new[] { "Scenes", "Materials", "UI", "Audio", "VFX", "Prefabs" })
            {
                Directory.CreateDirectory(Folder + "/" + child);
            }
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "NETRunner_MainMenu_Prototype";
            var settings = LoadOrCreate<MenuPrototypeSettings>(Folder + "/UI/MenuPrototypeSettings.asset");
            var panel = LoadOrCreate<PanelSettings>(Folder + "/UI/MenuPanelSettings.asset");
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            panel.sortingOrder = 10;
            string themePath = Folder + "/UI/UnityDefaultRuntimeTheme.tss";
            if (!File.Exists(themePath))
            {
                File.WriteAllText(themePath, "@import url(\"unity-theme://default\");\n");
                AssetDatabase.ImportAsset(themePath);
            }
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);
            EditorUtility.SetDirty(panel);
            PrepareMaterials();
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.28f, 0.30f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.045f, 0.06f, 0.067f);
            RenderSettings.fogDensity = 0.018f;
            RenderSettings.reflectionIntensity = 0.25f;
            environment = new GameObject("Facility / Electrical service bay 04").transform;
            BuildArchitecture();
            Transform shutter = BuildShutter();
            Transform rotor = BuildMachinery();
            Renderer indicator = BuildControlBank();
            BuildCeilingLights();
            BuildDust();
            PrefabUtility.SaveAsPrefabAsset(environment.gameObject, Folder + "/Prefabs/ElectricalServiceBay.prefab");

            var root = new GameObject("NET access node 92 / Prototype controllers");
            var view = root.AddComponent<MenuView>();
            view.settings = settings;
            view.styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(Folder + "/UI/MenuTerminal.uss");
            root.GetComponent<UIDocument>().panelSettings = panel;
            var audio = root.AddComponent<MenuAudioController>();
            ConfigureAudio(audio, root.transform);
            root.AddComponent<AudioListener>();
            var feeds = root.AddComponent<SurveillanceCameraController>();
            feeds.settings = settings;
            feeds.view = view;
            feeds.audioController = audio;
            feeds.feeds = new[]
            {
                Feed("CAM_02", "SERVICE BAY / INTERNAL NETWORK", new Vector3(-5.1f, 3.7f, -7.5f), new Vector3(1.8f, 1.4f, 4.5f)),
                Feed("CAM_04", "POWER DISTRIBUTION / INTERNAL NETWORK", new Vector3(-5.2f, 3.8f, -3.5f), new Vector3(3.7f, 1.2f, 5)),
                Feed("CAM_07", "VENTILATION / INTERNAL NETWORK", new Vector3(-2.2f, 3f, -5.8f), new Vector3(-5.3f, 1.8f, -1)),
                Feed("CAM_11", "RESTRICTED ACCESS / INTERNAL NETWORK", new Vector3(3.3f, 2.7f, 3.7f), new Vector3(0, 1.6f, 8))
            };
            foreach (var feed in feeds.feeds)
            {
                feed.camera.transform.SetParent(root.transform);
            }
            var boot = root.AddComponent<MenuBootSequence>();
            boot.settings = settings;
            var logs = root.AddComponent<SystemLogController>();
            logs.settings = settings;
            var loading = root.AddComponent<LoadingScreenController>();
            loading.settings = settings;
            loading.logs = logs;
            var events = root.AddComponent<FacilityEventController>();
            events.settings = settings;
            events.view = view;
            var eventRoot = new GameObject("Rare facility events").transform;
            eventRoot.SetParent(root.transform);
            var relay = new GameObject("Event / unexplained relay power").AddComponent<RelayLightEvent>();
            relay.transform.SetParent(eventRoot);
            relay.target = Point("Relay spill / usually dormant", new Vector3(4.2f, 2.3f, 4.7f), new Color(0.43f, 0.67f, 0.68f), 0, 4);
            relay.indicator = indicator;
            relay.offMaterial = graphite;
            relay.onMaterial = screen;
            relay.telemetry = "BUS_09 / UNSCHEDULED RELAY CYCLE";
            var motor = new GameObject("Event / extraction unit restart").AddComponent<MachineryEvent>();
            motor.transform.SetParent(eventRoot);
            motor.rotor = rotor;
            motor.audioController = audio;
            motor.telemetry = "VENTILATION_04 / MANUAL OVERRIDE RECEIVED";
            var door = new GameObject("Event / distant shutter travel").AddComponent<ShutterEvent>();
            door.transform.SetParent(eventRoot);
            door.shutter = shutter;
            door.telemetry = "RESTRICTED ACCESS / ACTUATOR RESPONSE";
            events.events = new FacilityEvent[] { relay, motor, door };
            var controller = root.AddComponent<MainMenuController>();
            controller.view = view;
            controller.boot = boot;
            controller.loading = loading;
            controller.surveillance = feeds;
            controller.facilityEvents = events;
            controller.audioController = audio;
            CreatePostProcessing(root.transform);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = root;
            Debug.Log("Standalone menu scene created: " + ScenePath);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static Material Material(string name, Color color, float metallic, float smoothness, float emission = 0)
        {
            string path = Folder + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            if (emission > 0)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void PrepareMaterials()
        {
            concrete = Material("NR_Concrete", new Color(0.30f, 0.33f, 0.34f), 0, 0.17f);
            steel = Material("NR_GalvanizedSteel", new Color(0.25f, 0.29f, 0.30f), 0.65f, 0.32f);
            graphite = Material("NR_Graphite", new Color(0.07f, 0.10f, 0.115f), 0.45f, 0.25f);
            ceramic = Material("NR_Ceramic", new Color(0.49f, 0.50f, 0.46f), 0.1f, 0.27f);
            amber = Material("NR_AmberLamp", new Color(0.70f, 0.29f, 0.06f), 0.1f, 0.45f, 2);
            lightStrip = Material("NR_Fluorescent", new Color(0.66f, 0.77f, 0.74f), 0, 0.4f, 2);
            screen = Material("NR_TerminalScreen", new Color(0.20f, 0.43f, 0.45f), 0.1f, 0.4f, 1.7f);
            rust = Material("NR_SafetyPaint", new Color(0.41f, 0.32f, 0.19f), 0.15f, 0.2f);
            string path = Folder + "/Materials/ConcreteGrain.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(256, 256, TextureFormat.RGB24, false);
                var random = new System.Random(92);
                var pixels = new Color[256 * 256];
                for (int y = 0; y < 256; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        float noise = 0.81f + (float)random.NextDouble() * 0.12f + Mathf.PerlinNoise(x * 0.065f, y * 0.065f) * 0.07f;
                        pixels[y * 256 + x] = new Color(noise, noise, noise);
                    }
                }
                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }
            concrete.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            concrete.SetTextureScale("_BaseMap", new Vector2(5, 5));
            EditorUtility.SetDirty(concrete);
        }

        private static GameObject Box(string name, Vector3 position, Vector3 size, Material material, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent != null ? parent : environment, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject Cylinder(string name, Vector3 position, Vector3 size, Material material, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent != null ? parent : environment, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void BuildArchitecture()
        {
            Box("Concrete slab", new Vector3(0, -0.15f, 0), new Vector3(12, 0.3f, 20), concrete);
            Box("West wall", new Vector3(-6, 2.5f, 0), new Vector3(0.25f, 5, 20), concrete);
            Box("East wall", new Vector3(6, 2.5f, 0), new Vector3(0.25f, 5, 20), concrete);
            Box("Rear wall", new Vector3(0, 2.5f, 10), new Vector3(12, 5, 0.25f), concrete);
            Box("Front wall", new Vector3(0, 2.5f, -10), new Vector3(12, 5, 0.25f), concrete);
            Box("Ceiling", new Vector3(0, 5.1f, 0), new Vector3(12, 0.2f, 20), graphite);
            for (int z = -8; z <= 8; z += 4)
            {
                foreach (int side in new[] { -1, 1 })
                {
                    Box("Load bearing column", new Vector3(side * 5.7f, 2.5f, z), new Vector3(0.32f, 5, 0.45f), steel);
                    Box("Column foot", new Vector3(side * 5.7f, 0.12f, z), new Vector3(0.56f, 0.24f, 0.69f), graphite);
                    Box("Impact marker", new Vector3(side * 5.48f, 0.8f, z), new Vector3(0.055f, 0.18f, 0.5f), rust);
                }
                Box("Ceiling crossbeam", new Vector3(0, 4.8f, z), new Vector3(11.7f, 0.24f, 0.18f), steel);
                Box("Slab expansion joint", new Vector3(0, 0.005f, z), new Vector3(11.7f, 0.01f, 0.016f), graphite);
            }
            for (int x = -4; x <= 4; x += 4)
            {
                Box("Longitudinal expansion joint", new Vector3(x, 0.005f, 0), new Vector3(0.016f, 0.01f, 19), graphite);
            }
            foreach (float x in new[] { -3.7f, 2.5f })
            {
                Box("Service lane paint", new Vector3(x, 0.012f, 0), new Vector3(0.07f, 0.01f, 17), rust);
            }
            for (int i = 0; i < 3; i++)
            {
                var pipe = Cylinder("Overhead utility pipe", new Vector3(-4.5f + i * 0.35f, 4.45f, 0), new Vector3(0.14f, 9, 0.14f), steel);
                pipe.transform.localRotation = Quaternion.Euler(90, 0, 0);
                for (int z = -7; z <= 7; z += 2)
                {
                    var collar = Cylinder("Pipe coupling", new Vector3(-4.5f + i * 0.35f, 4.45f, z), new Vector3(0.22f, 0.045f, 0.22f), graphite);
                    collar.transform.localRotation = Quaternion.Euler(90, 0, 0);
                }
            }
            foreach (float x in new[] { 3.8f, 4.5f })
            {
                Box("Cable tray side rail", new Vector3(x, 4.25f, 0), new Vector3(0.06f, 0.18f, 18), steel);
            }
            for (int z = -8; z <= 8; z++)
            {
                Box("Cable tray rung", new Vector3(4.15f, 4.21f, z), new Vector3(0.7f, 0.04f, 0.07f), graphite);
            }
            for (int i = 0; i < 4; i++)
            {
                Box("Bundled cable", new Vector3(3.95f + i * 0.11f, 4.22f, 0), new Vector3(0.055f, 0.055f, 18), graphite);
            }
            for (int i = 0; i < 3; i++)
            {
                Box("Service crate", new Vector3(-4.7f, 0.36f + i * 0.73f, 5.5f), new Vector3(1.2f, 0.7f, 1.1f), graphite);
                Box("Crate latch", new Vector3(-4.09f, 0.36f + i * 0.73f, 5.5f), new Vector3(0.02f, 0.13f, 0.22f), steel);
            }
            Box("Restricted access lintel", new Vector3(0, 3.8f, 7.7f), new Vector3(3.6f, 0.4f, 0.5f), steel);
            foreach (int side in new[] { -1, 1 })
            {
                Box("Restricted access jamb", new Vector3(side * 1.9f, 1.9f, 7.7f), new Vector3(0.3f, 3.8f, 0.5f), steel);
                Cylinder("Protective bollard", new Vector3(side * 2.5f, 0.6f, 6.9f), new Vector3(0.17f, 0.6f, 0.17f), rust);
            }
            Text("SECTOR 04", new Vector3(2.8f, 3.4f, 9.82f), Quaternion.identity, 0.075f, new Color(0.62f, 0.63f, 0.57f));
            Text("RESTRICTED ACCESS", new Vector3(-1.4f, 3.65f, 7.39f), Quaternion.identity, 0.036f, new Color(0.68f, 0.60f, 0.41f));
            Text("POWER DISTRIBUTION\nAUTHORIZED PERSONNEL ONLY", new Vector3(5.83f, 3.35f, 1.4f), Quaternion.Euler(0, -90, 0), 0.038f, new Color(0.58f, 0.60f, 0.57f));
            Box("Security camera housing", new Vector3(5.25f, 3.75f, 5.8f), new Vector3(0.26f, 0.2f, 0.5f), ceramic);
            Box("Camera lens", new Vector3(5.25f, 3.75f, 5.53f), new Vector3(0.14f, 0.12f, 0.04f), graphite);
            Box("Camera bracket", new Vector3(5.57f, 3.7f, 5.8f), new Vector3(0.4f, 0.09f, 0.08f), steel);
            Box("Drain channel", new Vector3(0.2f, 0.016f, 3), new Vector3(0.3f, 0.02f, 4), graphite);
            for (int i = 0; i < 20; i++)
            {
                Box("Drain grating", new Vector3(0.2f, 0.031f, 1.1f + i * 0.2f), new Vector3(0.29f, 0.015f, 0.018f), steel);
            }
        }

        private static Transform BuildShutter()
        {
            var pivot = new GameObject("Shutter carriage / independently actuated").transform;
            pivot.SetParent(environment, false);
            pivot.localPosition = new Vector3(0, 2.9f, 7.75f);
            Box("Shutter panel", Vector3.zero, new Vector3(3.4f, 1.8f, 0.12f), graphite, pivot);
            for (int i = 0; i < 13; i++)
            {
                Box("Shutter slat", new Vector3(0, -0.83f + i * 0.14f, -0.08f), new Vector3(3.38f, 0.026f, 0.04f), steel, pivot);
            }
            Box("Door threshold", new Vector3(0, 0.025f, 7.6f), new Vector3(3.5f, 0.05f, 0.3f), steel);
            Point("Distant utility light", new Vector3(0, 2.4f, 9.3f), new Color(0.56f, 0.61f, 0.53f), 8.8f, 5);
            return pivot;
        }

        private static Transform BuildMachinery()
        {
            Box("Extraction unit", new Vector3(-4.8f, 1.6f, -0.5f), new Vector3(1.8f, 3.2f, 2.6f), graphite);
            Box("Extraction face plate", new Vector3(-4.8f, 2f, -1.84f), new Vector3(1.55f, 1.7f, 0.1f), steel);
            var rotor = new GameObject("Ventilation rotor").transform;
            rotor.SetParent(environment, false);
            rotor.localPosition = new Vector3(-4.8f, 2.15f, -1.96f);
            var hub = Cylinder("Motor hub", Vector3.zero, new Vector3(0.3f, 0.08f, 0.3f), ceramic, rotor);
            hub.transform.localRotation = Quaternion.Euler(90, 0, 0);
            for (int i = 0; i < 4; i++)
            {
                var blade = Box("Fan blade", Quaternion.Euler(0, 0, i * 90) * new Vector3(0.3f, 0, 0), new Vector3(0.55f, 0.15f, 0.035f), steel, rotor);
                blade.transform.localRotation = Quaternion.Euler(0, 0, i * 90 + 20);
            }
            for (int i = 0; i < 10; i++)
            {
                Box("Protective grille", new Vector3(-5.48f + i * 0.15f, 2.15f, -2.03f), new Vector3(0.022f, 1.4f, 0.025f), graphite);
            }
            Box("Duct riser", new Vector3(-4.8f, 4.05f, -0.5f), new Vector3(1.1f, 1.25f, 1.25f), steel);
            Box("Maintenance placard", new Vector3(-4.8f, 0.85f, -1.87f), new Vector3(0.6f, 0.2f, 0.02f), rust);
            Text("EXTRACTION / 04", new Vector3(-5.3f, 0.9f, -1.91f), Quaternion.identity, 0.022f, Color.black);
            return rotor;
        }

        private static Renderer BuildControlBank()
        {
            for (int i = 0; i < 4; i++)
            {
                float z = -0.5f + i * 1.6f;
                Box("Power cabinet " + i, new Vector3(5.14f, 1.38f, z), new Vector3(1.1f, 2.76f, 1.4f), steel);
                Box("Cabinet door", new Vector3(4.56f, 1.4f, z), new Vector3(0.045f, 2.5f, 1.26f), graphite);
                Box("Service handle", new Vector3(4.5f, 1.25f, z + 0.46f), new Vector3(0.065f, 0.26f, 0.06f), ceramic);
                Box("Meter window", new Vector3(4.49f, 2.1f, z), new Vector3(0.06f, 0.2f, 0.34f), i == 1 ? screen : graphite);
                for (int j = 0; j < 6; j++)
                {
                    Box("Cabinet cooling slot", new Vector3(4.52f, 0.39f + j * 0.08f, z), new Vector3(0.02f, 0.025f, 0.77f), steel);
                }
                Box("Power conduit", new Vector3(5.15f, 3.5f, z), new Vector3(0.14f, 1.45f, 0.14f), graphite);
            }
            var lamp = Cylinder("Amber warning lens", new Vector3(3.6f, 2.7f, 7.4f), new Vector3(0.12f, 0.08f, 0.12f), amber);
            lamp.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Point("Status lamp spill", new Vector3(3.6f, 2.7f, 7.2f), new Color(0.77f, 0.36f, 0.11f), 10.4f, 3.5f);
            Box("Remote terminal housing", new Vector3(4.2f, 2.1f, 5.3f), new Vector3(0.65f, 0.72f, 0.36f), ceramic);
            var display = Box("Remote terminal / dormant", new Vector3(4.2f, 2.15f, 5.1f), new Vector3(0.51f, 0.43f, 0.025f), graphite);
            return display.GetComponent<Renderer>();
        }

        private static void BuildCeilingLights()
        {
            foreach (int z in new[] { -5, 1, 6 })
            {
                Box("Fluorescent fixture", new Vector3(0, 4.7f, z), new Vector3(2.2f, 0.11f, 0.45f), steel);
                foreach (float x in new[] { -0.12f, 0.12f })
                {
                    Box("Fluorescent tube", new Vector3(0, 4.62f, z + x), new Vector3(1.95f, 0.05f, 0.05f), lightStrip);
                }
                var go = new GameObject("Overhead pool " + z);
                go.transform.SetParent(environment);
                go.transform.position = new Vector3(0, 4.48f, z);
                go.transform.rotation = Quaternion.Euler(90, 0, 0);
                var light = go.AddComponent<Light>();
                light.type = LightType.Spot;
                light.color = new Color(0.66f, 0.78f, 0.76f);
                light.intensity = z == 1 ? 165f : 105f;
                light.range = 12;
                light.spotAngle = 112;
                light.innerSpotAngle = 72;
                light.shadows = z == 1 ? LightShadows.Soft : LightShadows.None;
                go.AddComponent<UniversalAdditionalLightData>();
                Point("Fixture bounce " + z, new Vector3(0, 3.9f, z), new Color(0.57f, 0.65f, 0.64f), 18, 9);
            }
        }

        private static Light Point(string name, Vector3 position, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(environment);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            go.AddComponent<UniversalAdditionalLightData>();
            return light;
        }

        private static void Text(string value, Vector3 position, Quaternion rotation, float scale, Color color)
        {
            var go = new GameObject("Facility label / " + value.Replace('\n', ' '));
            go.transform.SetParent(environment);
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = Vector3.one * scale;
            var text = go.AddComponent<TextMesh>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 64;
            text.characterSize = 0.25f;
            text.color = color;
            text.anchor = TextAnchor.UpperLeft;
            go.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        }

        private static void BuildDust()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/VFX/NR_Dust.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                material.SetFloat("_Surface", 1);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = 3000;
                AssetDatabase.CreateAsset(material, Folder + "/VFX/NR_Dust.mat");
            }
            var go = new GameObject("Sparse suspended dust / max 35 particles");
            go.transform.SetParent(environment);
            go.transform.position = new Vector3(0, 2.5f, 1);
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop();
            var main = particles.main;
            main.startLifetime = 16;
            main.startSpeed = 0.035f;
            main.startSize = 0.011f;
            main.startColor = new Color(0.68f, 0.76f, 0.72f, 0.16f);
            main.maxParticles = 35;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;
            var emission = particles.emission;
            emission.rateOverTime = 2;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(8, 3.5f, 12);
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        }

        private static SurveillanceCameraController.Feed Feed(string id, string location, Vector3 position, Vector3 target)
        {
            var go = new GameObject(id + " / fixed surveillance viewpoint");
            go.transform.position = position;
            go.transform.LookAt(target);
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.047f, 0.055f);
            camera.fieldOfView = 63;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 40;
            camera.enabled = id == "CAM_02";
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            return new SurveillanceCameraController.Feed { id = id, location = location, camera = camera };
        }

        private static void CreatePostProcessing(Transform parent)
        {
            string path = Folder + "/VFX/SurveillanceVolume.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
                var color = profile.Add<ColorAdjustments>(true);
                color.saturation.value = -35;
                color.contrast.value = 9;
                color.postExposure.value = 0.5f;
                var tone = profile.Add<Tonemapping>(true);
                tone.mode.value = TonemappingMode.ACES;
                var vignette = profile.Add<Vignette>(true);
                vignette.intensity.value = 0.22f;
                vignette.smoothness.value = 0.65f;
                var bloom = profile.Add<Bloom>(true);
                bloom.intensity.value = 0.08f;
                bloom.threshold.value = 1.2f;
                foreach (var component in profile.components)
                {
                    AssetDatabase.AddObjectToAsset(component, profile);
                }
                EditorUtility.SetDirty(profile);
            }
            var go = new GameObject("Local surveillance grading / no project settings changes");
            go.transform.SetParent(parent);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        private static void ConfigureAudio(MenuAudioController audio, Transform parent)
        {
            audio.electricalHum = Audio("Electrical hum / continuous", "ElectricalHum", 4, 0, true, parent);
            audio.ventilation = Audio("Ventilation / continuous", "Ventilation", 4, 1, true, parent);
            audio.machinery = Audio("Distant machinery / event driven", "Machinery", 4, 2, true, parent);
            audio.uiRelay = Audio("UI relay / selection only", "Relay", 0.075f, 3, false, parent);
            audio.signalInterference = Audio("Signal interference / camera switch only", "Signal", 0.12f, 4, false, parent);
        }

        private static AudioSource Audio(string name, string file, float duration, int mode, bool loop, Transform parent)
        {
            const int rate = 22050;
            string path = Folder + "/Audio/" + file + ".wav";
            if (!File.Exists(path))
            {
                int count = Mathf.RoundToInt(duration * rate);
                var random = new System.Random(92 + mode);
                using (var writer = new BinaryWriter(File.Create(path)))
                {
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                    writer.Write(36 + count * 2);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                    writer.Write(16);
                    writer.Write((short)1);
                    writer.Write((short)1);
                    writer.Write(rate);
                    writer.Write(rate * 2);
                    writer.Write((short)2);
                    writer.Write((short)16);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                    writer.Write(count * 2);
                    double lowNoise = 0;
                    for (int i = 0; i < count; i++)
                    {
                        double t = (double)i / rate;
                        double noise = random.NextDouble() * 2 - 1;
                        lowNoise = lowNoise * 0.96 + noise * 0.04;
                        double sample;
                        if (mode == 0)
                        {
                            sample = Math.Sin(t * Math.PI * 100) * 0.25 + Math.Sin(t * Math.PI * 200) * 0.06;
                        }
                        else if (mode == 1)
                        {
                            sample = lowNoise * 0.7;
                        }
                        else if (mode == 2)
                        {
                            sample = Math.Sin(t * Math.PI * 86) * 0.16 + lowNoise * 0.3;
                        }
                        else
                        {
                            double envelope = Math.Exp(-t * (mode == 3 ? 85 : 38));
                            sample = (mode == 3 ? Math.Sin(t * Math.PI * 1800) * 0.12 + noise * 0.15 : noise * 0.18) * envelope;
                        }
                        // Fade loop boundaries to avoid periodic clicks.
                        double edge = loop ? Math.Min(1, Math.Min(t, duration - t) / 0.05) : 1;
                        writer.Write((short)(sample * edge * 32767));
                    }
                }
                AssetDatabase.ImportAsset(path);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                var sampleSettings = importer.defaultSampleSettings;
                sampleSettings.loadType = AudioClipLoadType.DecompressOnLoad;
                sampleSettings.compressionFormat = AudioCompressionFormat.PCM;
                importer.defaultSampleSettings = sampleSettings;
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            var source = go.AddComponent<AudioSource>();
            source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0;
            return source;
        }
    }
}
