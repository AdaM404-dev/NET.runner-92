using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NetRunner.Core;
using NetRunner.SceneBuilding.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace NetRunner.Atmosphere.Editor
{
    /// <summary>
    /// Builds the hospital's atmosphere: the overcast moonlight and the
    /// lightning, the storm volume outside and one zone per floor and wing,
    /// the haze in reception, rain, splashes, dust and steam, and the
    /// ambience sounds routed through the Northline mixer. All of it is
    /// wired to one <see cref="HospitalAtmosphere"/> that plays it.
    /// </summary>
    public static class HospitalAtmosphereBuilder
    {
        private const string MixerPath = HospitalAssets.Atmosphere + "/Audio/Northline_Ambience.mixer";
        private const string AudioFolder = HospitalAssets.Atmosphere + "/Audio/";
        private const string ParticleFolder = HospitalAssets.Atmosphere + "/VFX/";
        private const string UnlitShader = "HDRP/Unlit";
        private const string ExteriorGroup = "Exterior";
        private const string InteriorGroup = "Interior";
        private const string MechanicalGroup = "Mechanical";

        // The two directional lights: a weak blue fill with shadows, and the
        // lightning, dark until a strike.
        private const float FillIntensity = 9.5f;
        private const float LightningIntensity = 0f;
        private const float DirectionalFogDimmer = .40f;

        // The storm outside: fog thinning over 105 m, exposure 3.25.
        private const float StormHaze = 105f;
        private const float StormExposure = 3.25f;

        // A floor's zone: centred 2.01 m above the floor, 71.7 by 4 m, and
        // 29.7 m deep (27.5 m on the research floor).
        private const float ZoneCentreHeight = 2.01f;
        private const float ZoneWidth = 71.7f;
        private const float ZoneHeight = 4.0f;
        private const float ZoneDepth = 29.7f;
        private const float ResearchZoneDepth = 27.5f;
        private const int ResearchFloor = 4;
        private const float ZoneBlend = 1.35f;

        // The local haze over the reception desk.
        private const float ReceptionMeanFreePath = 52f;
        private const float ReceptionAnisotropy = .19f;

        // Particle systems: rain and splashes are emitted by HospitalAtmosphere
        // itself; dust and steam spawn in a box and drift slowly.
        private const int RainCap = 950;
        private const int SplashCap = 160;
        private const int DustCap = 220;
        private const int SteamCap = 45;
        private const float DustLife = 4.7f;
        private const float SteamLife = 2.3f;
        private const float DefaultLife = 1f;
        private const float DriftSpeed = .09f;
        private const float SplashGravity = .44f;
        private const float StreakLength = 8f;
        private const float StreakVelocityScale = .015f;
        private const float TransparentSurface = 1f;

        // Sound: thunder and the creaking fence are placed in the world.
        private const float ThunderSpatial = .65f;
        private const float MonitorSpatial = 1f;
        private const float CreakSpatial = .7f;
        private const float ThunderNear = 27f;
        private const float ThunderFar = 115f;
        private const float CreakNear = 8f;
        private const float DefaultNear = 3.2f;
        private const float DefaultFar = 41f;

        private static readonly Vector3 FillAngle = new Vector3(48, -37, 0);
        private static readonly Vector3 LightningAngle = new Vector3(39, 138, 0);
        private static readonly Color MoonColor = new Color(.64f, .77f, 1f);

        // Floors 1 to 4: their zone names, dust density, fog distance and exposure.
        private static readonly string[] FloorTitles = { "Reception", "PatientWard", "Surgery", "Research" };
        private static readonly float[] FloorDust = { .53f, .78f, .26f, .04f };
        private static readonly float[] FloorHaze = { 175, 130, 220, 400 };
        private static readonly float[] FloorExposure = { 3.45f, 3.85f, 4f, 4.10f };

        // The two ground-floor wings share reception's profile.
        private static readonly Vector3 PublicWingCentre = new Vector3(0, 1.9f, -17.18f);
        private static readonly Vector3 PublicWingSize = new Vector3(23.7f, 3.8f, 4.0f);
        private const float PublicWingDust = .47f;
        private static readonly Vector3 AmbulanceWingCentre = new Vector3(-20, 1.9f, 17.2f);
        private static readonly Vector3 AmbulanceWingSize = new Vector3(15.7f, 3.8f, 3.5f);
        private const float AmbulanceWingDust = .45f;

        private static readonly Vector3 ReceptionHazeCentre = new Vector3(-1.1f, 1.7f, -10.4f);
        private static readonly Vector3 ReceptionHazeSize = new Vector3(19f, 3.15f, 9.0f);
        private static readonly Color ReceptionHazeAlbedo = new Color(.59f, .68f, .74f);

        private static readonly Color RainColor = new Color(.53f, .66f, .76f, .39f);
        private static readonly Color SplashColor = new Color(.52f, .65f, .73f, .19f);
        private static readonly Color DustColor = new Color(.67f, .72f, .69f, .21f);
        private static readonly Color SteamColor = new Color(.55f, .67f, .67f, .08f);
        private static readonly Vector3 DustBox = new Vector3(5, 2.3f, 5);
        private static readonly Vector3 SteamBox = new Vector3(.16f, .06f, .15f);
        private static readonly Vector3 SteamValve = new Vector3(10.8f, .68f, -1.82f);
        private static readonly ParticleSystem.MinMaxCurve DropSize = new ParticleSystem.MinMaxCurve(.012f, .025f);
        private static readonly ParticleSystem.MinMaxCurve PuffSize = new ParticleSystem.MinMaxCurve(.15f, .27f);

        private static readonly Vector3 ThunderPosition = new Vector3(-29, 14, -55);
        private static readonly Vector3 FencePosition = new Vector3(-6, 1.7f, -39.8f);

        /// <summary>
        /// Adds the atmosphere under <paramref name="root"/>; <paramref name="listener"/>
        /// is the player's eyes, where HospitalAtmosphere measures which zone the player is in.
        /// </summary>
        public static void Add(Transform root, Transform listener)
        {
            Directional(root, "Weak overcast fill", FillAngle, FillIntensity, true);
            Light lightning = Directional(root, "Storm lightning", LightningAngle, LightningIntensity, false);
            Volume storm = SceneParts.Node("ZONE_Exterior_Quarantine", root).gameObject.AddComponent<Volume>();
            storm.isGlobal = true;
            storm.sharedProfile = FogProfiles.Create("Storm exterior", true, StormHaze, StormExposure);
            var zones = new List<EnvironmentalZone>();
            for (int floor = 1; floor <= FloorTitles.Length; floor++)
            {
                string label = $"Level{floor:00}_{FloorTitles[floor - 1]}";
                VolumeProfile profile = FogProfiles.Create(label, false, FloorHaze[floor - 1], FloorExposure[floor - 1]);
                zones.Add(Zone(root, "ZONE_" + label, new Vector3(0, (floor - 1) * StairRules.FloorHeight + ZoneCentreHeight, 0),
                    new Vector3(ZoneWidth, ZoneHeight, floor == ResearchFloor ? ResearchZoneDepth : ZoneDepth), floor,
                    FloorDust[floor - 1], profile));
            }

            VolumeProfile receptionProfile = zones[0].GetComponent<Volume>().sharedProfile;
            zones.Add(Zone(root, "ZONE_Public_Intake_Wing", PublicWingCentre, PublicWingSize, 1, PublicWingDust, receptionProfile));
            zones.Add(Zone(root, "ZONE_Ambulance_Intake_Wing", AmbulanceWingCentre, AmbulanceWingSize, 1, AmbulanceWingDust,
                receptionProfile));
            AddReceptionHaze(root);

            ParticleSystem rain = Particles(root, "Rain streaks", RainCap, true, RainColor, 0, Vector3.one, DropSize, 0);
            ParticleSystem splashes = Particles(root, "Surface rain impacts", SplashCap, false, SplashColor, 0, Vector3.one,
                DropSize, SplashGravity);
            ParticleSystem dust = Particles(root, "Indoor air motes", DustCap, false, DustColor, DustLife, DustBox, DropSize, 0);
            ParticleSystem steam = Particles(root, "Damaged valve vapor", SteamCap, false, SteamColor, SteamLife, SteamBox,
                PuffSize, 0);
            steam.transform.position = SteamValve;

            AudioMixer mixer = Mixer();
            AudioSource exterior = Audio(root, "Rain and wind", "Rain_Wind", true, 0, Vector3.zero, mixer, ExteriorGroup);
            AudioSource interior = Audio(root, "Clinical ventilation", "Ventilation", true, 0, Vector3.zero, mixer, InteriorGroup);
            AudioSource thunder = Audio(root, "Distant thunder", null, false, ThunderSpatial, ThunderPosition, mixer, MechanicalGroup);
            thunder.minDistance = ThunderNear;
            thunder.maxDistance = ThunderFar;
            AudioSource monitor = Audio(root, "Distant monitor", null, false, MonitorSpatial, Vector3.zero, mixer, InteriorGroup);
            AudioSource creak = Audio(root, "Fence and structure", null, false, CreakSpatial, FencePosition, mixer, MechanicalGroup);
            creak.minDistance = CreakNear;
            root.gameObject.AddComponent<HospitalAtmosphere>().Configure(listener, zones.ToArray(),
                exterior, interior, thunder, monitor, creak, Clip("Distant_Thunder"), Clip("Medical_Pulse"),
                Clip("Gate_Creak"), lightning, rain, splashes, dust, steam);
        }

        private static Light Directional(Transform root, string name, Vector3 angle, float intensity, bool castsShadows)
        {
            Transform node = SceneParts.Node(name, root);
            node.rotation = Quaternion.Euler(angle);
            Light light = node.gameObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = MoonColor;
            light.shadows = castsShadows ? LightShadows.Soft : LightShadows.None;
            node.gameObject.AddComponent<HDAdditionalLightData>().volumetricDimmer = DirectionalFogDimmer;
            return light;
        }

        private static EnvironmentalZone Zone(Transform parent, string name, Vector3 position, Vector3 size, int floor, float dust,
            VolumeProfile profile)
        {
            Transform node = SceneParts.Node(name, parent);
            node.position = position;
            BoxCollider box = node.gameObject.AddComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = true;
            Volume volume = node.gameObject.AddComponent<Volume>();
            volume.isGlobal = false;
            volume.priority = floor + 1;
            volume.blendDistance = ZoneBlend;
            volume.sharedProfile = profile;
            EnvironmentalZone zone = node.gameObject.AddComponent<EnvironmentalZone>();
            zone.Configure(size, volume.blendDistance, floor, dust);
            return zone;
        }

        private static void AddReceptionHaze(Transform root)
        {
            Transform node = SceneParts.Node("Reception local volume", root);
            node.position = ReceptionHazeCentre;
            LocalVolumetricFog haze = node.gameObject.AddComponent<LocalVolumetricFog>();
            LocalVolumetricFogArtistParameters parameters = haze.parameters;
            parameters.size = ReceptionHazeSize;
            parameters.albedo = ReceptionHazeAlbedo;
            parameters.meanFreePath = ReceptionMeanFreePath;
            parameters.anisotropy = ReceptionAnisotropy;
            haze.parameters = parameters;
        }

        // A looping system that starts empty; life 0 means HospitalAtmosphere
        // emits it by hand, otherwise it spawns in a box of `size`.
        private static ParticleSystem Particles(Transform root, string label, int cap, bool isStreak, Color color, float life,
            Vector3 size, ParticleSystem.MinMaxCurve startSize, float gravity)
        {
            Transform node = SceneParts.Node(label, root);
            ParticleSystem particles = node.gameObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.maxParticles = cap;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = life > 0 ? life : DefaultLife;
            main.startSpeed = life > 0 ? DriftSpeed : 0f;
            main.startSize = startSize;
            main.gravityModifier = gravity;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = life > 0;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = size;
            ParticleSystemRenderer renderer = node.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = isStreak ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            renderer.lengthScale = isStreak ? StreakLength : 1f;
            renderer.velocityScale = isStreak ? StreakVelocityScale : 0f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            string path = ParticleFolder + SceneParts.AssetName(label) + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find(UnlitShader));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetFloat("_SurfaceType", TransparentSurface);
            material.SetColor("_UnlitColor", color);
            HDMaterial.ValidateMaterial(material);
            EditorUtility.SetDirty(material);
            renderer.sharedMaterial = material;
            return particles;
        }

        // The mixer with its Exterior, Interior and Mechanical groups. Unity
        // has no public API for creating a mixer, so this uses the editor's
        // internal AudioMixerController; without it the sounds play unrouted.
        private static AudioMixer Mixer()
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer)
            {
                return mixer;
            }

            Type type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.Audio.AudioMixerController"))
                .FirstOrDefault(t => t != null);
            if (type == null)
            {
                Debug.LogWarning("AudioMixer authoring API unavailable");
                return null;
            }

            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            mixer = (AudioMixer)type.GetMethod("CreateMixerControllerAtPath", all).Invoke(null, new object[] { MixerPath });
            try
            {
                object master = type.GetProperty("masterGroup", all).GetValue(mixer);
                foreach (string label in new[] { ExteriorGroup, InteriorGroup, MechanicalGroup })
                {
                    object group = type.GetMethod("CreateNewGroup", all).Invoke(mixer, new object[] { label, true });
                    type.GetMethod("AddChildToParent", all).Invoke(mixer, new[] { group, master });
                }

                EditorUtility.SetDirty(mixer);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Ambience group routing: " + exception.GetBaseException().Message);
            }

            return mixer;
        }

        private static AudioClip Clip(string label)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + label + ".wav");
        }

        private static AudioSource Audio(Transform parent, string name, string clip, bool isLoop, float spatial, Vector3 position,
            AudioMixer mixer, string group)
        {
            Transform node = SceneParts.Node(name, parent);
            node.position = position;
            AudioSource source = node.gameObject.AddComponent<AudioSource>();
            if (clip != null)
            {
                source.clip = Clip(clip);
            }

            source.loop = isLoop;
            source.playOnAwake = isLoop;
            source.spatialBlend = spatial;
            source.dopplerLevel = 0;
            source.minDistance = DefaultNear;
            source.maxDistance = DefaultFar;
            if (mixer)
            {
                source.outputAudioMixerGroup = mixer.FindMatchingGroups(group).FirstOrDefault();
            }

            return source;
        }
    }
}
