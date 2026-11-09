using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace NETRunner92.Editor
{
    public static partial class HospitalSceneBuilder
    {
        static void AddFixtures(Fixture[] list, Transform root)
        {
            var counts = new int[5];
            foreach (var source in list)
            {
                int floor = Mathf.Clamp(source.floor, 0, 4);
                int i = counts[floor]++;
                var o = Node(source.name, root);
                o.position = V(source.position);
                Vector3 d = V(source.direction);
                if (d.sqrMagnitude > 0.1f)
                    o.rotation = Quaternion.LookRotation(d, Mathf.Abs(d.y) > 0.97f ? Vector3.forward : Vector3.up);
                var l = o.gameObject.AddComponent<Light>();
                l.type = floor == 0 && source.size < 3.4f ? LightType.Spot : LightType.Point;
                l.spotAngle = 116f;
                l.innerSpotAngle = 78f;
                l.color = C(source.color);
                float power = floor == 2 ? .55f : floor == 4 ? 1.1f : .82f;
                l.intensity = Mathf.Clamp(source.energy * 7.5f * power, 40, 1400);
                l.range = floor == 0 ? 8.5f : Mathf.Clamp(3.0f + source.size * .65f, 3.8f, 6.0f);
                l.shadows = floor == 0 && i % 7 == 1 ? LightShadows.Soft : LightShadows.None;
                o.gameObject.AddComponent<HDAdditionalLightData>().volumetricDimmer = floor == 3 ? .22f : .12f;
                if (source.name.Contains("lockout", StringComparison.OrdinalIgnoreCase))
                    o.gameObject.AddComponent<PracticalPulse>().Configure(PracticalPulse.Pattern.Emergency, .5f, .64f, 0);
                else if (floor < 4 && i % 13 == 3)
                    o.gameObject.AddComponent<PracticalPulse>().Configure(PracticalPulse.Pattern.Intermittent, .61f, .9f, i);
            }
        }

        static void AddAtmosphere(Transform root, FirstPersonController player)
        {
            var moon = Directional(root, "Weak overcast fill", new Vector3(48, -37, 0), 9.5f, true);
            var bolt = Directional(root, "Storm lightning", new Vector3(39, 138, 0), 0f, false);
            var global = Node("ZONE_Exterior_Quarantine", root).gameObject.AddComponent<Volume>();
            global.isGlobal = true;
            global.sharedProfile = Profile("Storm exterior", true, 105f, 3.25f);
            string[] titles = { "Reception", "PatientWard", "Surgery", "Research" };
            float[] dustDensity = { .53f, .78f, .26f, .04f };
            float[] haze = { 175, 130, 220, 400 };
            float[] exposure = { 3.45f, 3.85f, 4f, 4.10f };
            var zones = new List<EnvironmentalZone>();
            for (int f = 1; f <= 4; f++)
            {
                string label = $"Level{f:00}_{titles[f-1]}";
                var profile = Profile(label, false, haze[f-1], exposure[f-1]);
                zones.Add(Zone(root, "ZONE_" + label, new Vector3(0, (f - 1) * 4.2f + 2.01f, 0),
                    new Vector3(71.7f, 4.0f, f == 4 ? 27.5f : 29.7f), f, dustDensity[f-1], profile));
            }
            var receptionProfile = zones[0].GetComponent<Volume>().sharedProfile;
            zones.Add(Zone(root, "ZONE_Public_Intake_Wing", new Vector3(0, 1.9f, -17.18f),
                new Vector3(23.7f, 3.8f, 4.0f), 1, .47f, receptionProfile));
            zones.Add(Zone(root, "ZONE_Ambulance_Intake_Wing", new Vector3(-20, 1.9f, 17.2f),
                new Vector3(15.7f, 3.8f, 3.5f), 1, .45f, receptionProfile));

            var fog = Node("Reception local volume", root);
            fog.position = new Vector3(-1.1f, 1.7f, -10.4f);
            var local = fog.gameObject.AddComponent<LocalVolumetricFog>();
            var p = local.parameters;
            p.size = new Vector3(19f, 3.15f, 9.0f);
            p.albedo = new Color(.59f, .68f, .74f);
            p.meanFreePath = 52f;
            p.anisotropy = .19f;
            local.parameters = p;

            var rain = Particles(root, "Rain streaks", 950, true, new Color(.53f,.66f,.76f,.39f), 0, Vector3.one);
            var splashes = Particles(root, "Surface rain impacts", 160, false, new Color(.52f,.65f,.73f,.19f), 0, Vector3.one);
            var dust = Particles(root, "Indoor air motes", 220, false, new Color(.67f,.72f,.69f,.21f), 4.7f, new Vector3(5,2.3f,5));
            var steam = Particles(root, "Damaged valve vapor", 45, false, new Color(.55f,.67f,.67f,.08f), 2.3f, new Vector3(.16f,.06f,.15f));
            steam.transform.position = new Vector3(10.8f, .68f, -1.82f);

            var mixer = Mixer();
            var exterior = Audio(root, "Rain and wind", "Rain_Wind", true, 0, Vector3.zero, mixer, "Exterior");
            var interior = Audio(root, "Clinical ventilation", "Ventilation", true, 0, Vector3.zero, mixer, "Interior");
            var thunder = Audio(root, "Distant thunder", null, false, .65f, new Vector3(-29,14,-55), mixer, "Mechanical");
            thunder.minDistance = 27; thunder.maxDistance = 115;
            var monitor = Audio(root, "Distant monitor", null, false, 1f, Vector3.zero, mixer, "Interior");
            var creak = Audio(root, "Fence and structure", null, false, .7f, new Vector3(-6,1.7f,-39.8f), mixer, "Mechanical");
            creak.minDistance = 8;
            root.gameObject.AddComponent<HospitalAtmosphere>().Configure(player.Eyes.transform, zones.ToArray(),
                exterior, interior, thunder, monitor, creak, Clip("Distant_Thunder"), Clip("Medical_Pulse"),
                Clip("Gate_Creak"), bolt, rain, splashes, dust, steam);
        }

        static Light Directional(Transform root, string name, Vector3 angle, float power, bool shadow)
        {
            var t = Node(name, root);
            t.rotation = Quaternion.Euler(angle);
            var l = t.gameObject.AddComponent<Light>();
            l.type = LightType.Directional; l.intensity = power;
            l.color = new Color(.64f, .77f, 1f);
            l.shadows = shadow ? LightShadows.Soft : LightShadows.None;
            t.gameObject.AddComponent<HDAdditionalLightData>().volumetricDimmer = .40f;
            return l;
        }

        static EnvironmentalZone Zone(Transform parent, string name, Vector3 position, Vector3 size, int floor, float dust, VolumeProfile p)
        {
            var t = Node(name, parent);
            t.position = position;
            var box = t.gameObject.AddComponent<BoxCollider>();
            box.size = size; box.isTrigger = true;
            var v = t.gameObject.AddComponent<Volume>();
            v.isGlobal = false; v.priority = floor + 1; v.blendDistance = 1.35f; v.sharedProfile = p;
            var zone = t.gameObject.AddComponent<EnvironmentalZone>();
            zone.Configure(size, v.blendDistance, floor, dust);
            return zone;
        }

        static VolumeProfile Profile(string label, bool global, float distance, float ev)
        {
            string path = Atmos + "/Fog/" + Safe(label) + ".asset";
            if (AssetDatabase.LoadMainAssetAtPath(path)) AssetDatabase.DeleteAsset(path);
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            p.name = label;
            AssetDatabase.CreateAsset(p, path);
            if (global)
            {
                var environment = p.Add<VisualEnvironment>(true);
                environment.skyType.Override((int)SkyType.HDRI);
                environment.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
                var sky = p.Add<HDRISky>(true);
                sky.hdriSky.Override(Sky());
                sky.multiplier.Override(1.1f);
                p.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
                p.Add<Bloom>(true).intensity.Override(.14f);
                var reflection = p.Add<ScreenSpaceReflection>(true);
                reflection.enabled.Override(true);
                reflection.enabledTransparent.Override(false);
                p.Add<ScreenSpaceAmbientOcclusion>(true).intensity.Override(.45f);
            }
            var exposure = p.Add<Exposure>(true);
            exposure.mode.Override(ExposureMode.Fixed);
            exposure.fixedExposure.Override(ev);
            var fog = p.Add<Fog>(true);
            fog.enabled.Override(true);
            fog.enableVolumetricFog.Override(true);
            fog.colorMode.Override(FogColorMode.ConstantColor);
            fog.color.Override(new Color(.44f,.54f,.65f));
            fog.meanFreePath.Override(distance);
            fog.baseHeight.Override(-.9f);
            fog.maximumHeight.Override(16f);
            fog.maxFogDistance.Override(130f);
            fog.depthExtent.Override(global ? 52f : 29f);
            foreach (var c in p.components) AssetDatabase.AddObjectToAsset(c, path);
            EditorUtility.SetDirty(p);
            return p;
        }

        static Cubemap Sky()
        {
            string path = RenderAssets + "/Overcast_Luminance.asset";
            var map = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if (map) return map;
            const int n = 32;
            map = new Cubemap(n, TextureFormat.RGBAHalf, true);
            for (int f = 0; f < 6; f++)
            {
                var pixels = new Color[n*n];
                for (int y=0; y<n; y++)
                for (int x=0; x<n; x++)
                {
                    float u = 2f * (x+.5f)/n-1, v = 2f*(y+.5f)/n-1;
                    var d = f switch {
                        0 => new Vector3(1,-v,-u), 1 => new Vector3(-1,-v,u), 2 => new Vector3(u,1,v),
                        3 => new Vector3(u,-1,-v), 4 => new Vector3(u,-v,1), _ => new Vector3(-u,-v,-1)
                    };
                    d.Normalize();
                    float k = Mathf.Lerp(.035f,.38f,Mathf.Clamp01(.49f+d.y*.75f)) *
                        (.76f+ .38f*Mathf.PerlinNoise((1+d.x)*3.7f,(1+d.z)*3.4f));
                    pixels[y*n+x] = new Color(.64f*k,.77f*k,k);
                }
                map.SetPixels(pixels,(CubemapFace)f);
            }
            map.Apply();
            AssetDatabase.CreateAsset(map,path);
            return map;
        }

        static ParticleSystem Particles(Transform root, string label, int cap, bool streak, Color color, float life, Vector3 size)
        {
            var o = Node(label,root);
            var p = o.gameObject.AddComponent<ParticleSystem>();
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = p.main;
            main.loop = true; main.playOnAwake = true; main.maxParticles = cap;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = life > 0 ? life : 1f;
            main.startSpeed = life > 0 ? .09f : 0f;
            main.startSize = label.Contains("vapor") ? new ParticleSystem.MinMaxCurve(.15f,.27f) : new ParticleSystem.MinMaxCurve(.012f,.025f);
            main.gravityModifier = label.Contains("impacts") ? .44f : 0;
            var emission = p.emission;
            emission.rateOverTime = 0;
            var shape = p.shape;
            shape.enabled = life > 0;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = size;
            var r = o.GetComponent<ParticleSystemRenderer>();
            r.renderMode = streak ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            r.lengthScale = streak ? 8f : 1f;
            r.velocityScale = streak ? .015f : 0f;
            r.shadowCastingMode = ShadowCastingMode.Off;
            string path = Atmos + "/VFX/" + Safe(label) + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("HDRP/Unlit")); AssetDatabase.CreateAsset(m, path); }
            m.SetFloat("_SurfaceType", 1f);
            m.SetColor("_UnlitColor", color);
            HDMaterial.ValidateMaterial(m);
            EditorUtility.SetDirty(m);
            r.sharedMaterial = m;
            return p;
        }

        static AudioMixer Mixer()
        {
            string path = Atmos + "/Audio/Northline_Ambience.mixer";
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
            if (mixer) return mixer;
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.Audio.AudioMixerController")).FirstOrDefault(t => t != null);
            if (type == null) { Debug.LogWarning("AudioMixer authoring API unavailable"); return null; }
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            mixer = (AudioMixer)type.GetMethod("CreateMixerControllerAtPath", all).Invoke(null,new object[]{path});
            try
            {
                var master = type.GetProperty("masterGroup", all).GetValue(mixer);
                foreach (string label in new[] {"Exterior","Interior","Mechanical"})
                {
                    var group = type.GetMethod("CreateNewGroup", all).Invoke(mixer,new object[] { label,true });
                    type.GetMethod("AddChildToParent", all).Invoke(mixer,new[]{group,master});
                }
                EditorUtility.SetDirty(mixer);
            }
            catch (Exception ex) { Debug.LogWarning("Ambience group routing: " + ex.GetBaseException().Message); }
            return mixer;
        }

        static AudioClip Clip(string label) => AssetDatabase.LoadAssetAtPath<AudioClip>(Atmos + "/Audio/" + label + ".wav");

        static AudioSource Audio(Transform parent,string name,string clip,bool loop,float spatial,Vector3 pos, AudioMixer mixer,string group)
        {
            var t = Node(name,parent);
            t.position = pos;
            var a = t.gameObject.AddComponent<AudioSource>();
            if (clip != null) a.clip = Clip(clip);
            a.loop = loop; a.playOnAwake = loop; a.spatialBlend = spatial;
            a.dopplerLevel = 0; a.minDistance = 3.2f; a.maxDistance = 41f;
            if (mixer) a.outputAudioMixerGroup = mixer.FindMatchingGroups(group).FirstOrDefault();
            return a;
        }
    }
}

