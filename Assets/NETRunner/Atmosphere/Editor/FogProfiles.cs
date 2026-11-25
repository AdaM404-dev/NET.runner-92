using NetRunner.SceneBuilding.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace NetRunner.Atmosphere.Editor
{
    /// <summary>
    /// The volume profiles of the hospital's atmosphere zones: fixed exposure
    /// and volumetric fog for every zone, and for the storm outside also the
    /// overcast sky, tone mapping, bloom, reflections and ambient occlusion.
    /// The sky is a small generated cube map of an evenly overcast night.
    /// </summary>
    public static class FogProfiles
    {
        private const string Folder = HospitalAssets.Atmosphere + "/Fog/";
        private const string SkyPath = HospitalAssets.Rendering + "/Overcast_Luminance.asset";

        private const float SkyMultiplier = 1.1f;
        private const float BloomIntensity = .14f;
        private const float AmbientOcclusionIntensity = .45f;

        // The fog: a constant blue-grey from just below the ground to 16 m,
        // drawn out to 130 m, its volumetric depth longer outside.
        private const float FogBase = -.9f;
        private const float FogTop = 16f;
        private const float FogDistance = 130f;
        private const float OutdoorFogDepth = 52f;
        private const float IndoorFogDepth = 29f;

        // The sky's cube map: 32 texels a face, dark below the horizon and
        // brighter towards the zenith, with a little cloud noise.
        private const int SkySize = 32;
        private const int Faces = 6;
        private const float NightFloor = .035f;
        private const float NightCeiling = .38f;
        private const float HorizonLift = .49f;
        private const float ZenithGain = .75f;
        private const float CloudBase = .76f;
        private const float CloudDepth = .38f;
        private const float CloudScaleX = 3.7f;
        private const float CloudScaleZ = 3.4f;

        private static readonly Color FogColor = new Color(.44f, .54f, .65f);
        private static readonly Color SkyTint = new Color(.64f, .77f, 1f);

        /// <summary>
        /// Creates the profile <paramref name="label"/>, replacing an older one
        /// of that name: fog thinning over <paramref name="distance"/> metres,
        /// a fixed exposure of <paramref name="exposure"/>, and the storm's sky
        /// and effects when <paramref name="isOutdoors"/>.
        /// </summary>
        public static VolumeProfile Create(string label, bool isOutdoors, float distance, float exposure)
        {
            string path = Folder + SceneParts.AssetName(label) + ".asset";
            if (AssetDatabase.LoadMainAssetAtPath(path))
            {
                AssetDatabase.DeleteAsset(path);
            }

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = label;
            AssetDatabase.CreateAsset(profile, path);
            if (isOutdoors)
            {
                AddStorm(profile);
            }

            Exposure fixedExposure = profile.Add<Exposure>(true);
            fixedExposure.mode.Override(ExposureMode.Fixed);
            fixedExposure.fixedExposure.Override(exposure);
            Fog fog = profile.Add<Fog>(true);
            fog.enabled.Override(true);
            fog.enableVolumetricFog.Override(true);
            fog.colorMode.Override(FogColorMode.ConstantColor);
            fog.color.Override(FogColor);
            fog.meanFreePath.Override(distance);
            fog.baseHeight.Override(FogBase);
            fog.maximumHeight.Override(FogTop);
            fog.maxFogDistance.Override(FogDistance);
            fog.depthExtent.Override(isOutdoors ? OutdoorFogDepth : IndoorFogDepth);
            foreach (VolumeComponent component in profile.components)
            {
                AssetDatabase.AddObjectToAsset(component, path);
            }

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void AddStorm(VolumeProfile profile)
        {
            VisualEnvironment environment = profile.Add<VisualEnvironment>(true);
            environment.skyType.Override((int)SkyType.HDRI);
            environment.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
            HDRISky sky = profile.Add<HDRISky>(true);
            sky.hdriSky.Override(Sky());
            sky.multiplier.Override(SkyMultiplier);
            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
            profile.Add<Bloom>(true).intensity.Override(BloomIntensity);
            ScreenSpaceReflection reflection = profile.Add<ScreenSpaceReflection>(true);
            reflection.enabled.Override(true);
            reflection.enabledTransparent.Override(false);
            profile.Add<ScreenSpaceAmbientOcclusion>(true).intensity.Override(AmbientOcclusionIntensity);
        }

        private static Cubemap Sky()
        {
            Cubemap map = AssetDatabase.LoadAssetAtPath<Cubemap>(SkyPath);
            if (map)
            {
                return map;
            }

            map = new Cubemap(SkySize, TextureFormat.RGBAHalf, true);
            for (int face = 0; face < Faces; face++)
            {
                var pixels = new Color[SkySize * SkySize];
                for (int y = 0; y < SkySize; y++)
                {
                    for (int x = 0; x < SkySize; x++)
                    {
                        float u = 2f * (x + .5f) / SkySize - 1;
                        float v = 2f * (y + .5f) / SkySize - 1;
                        Vector3 direction = FaceDirection(face, u, v);
                        direction.Normalize();
                        float brightness = Mathf.Lerp(NightFloor, NightCeiling, Mathf.Clamp01(HorizonLift + direction.y * ZenithGain)) *
                            (CloudBase + CloudDepth * Mathf.PerlinNoise((1 + direction.x) * CloudScaleX, (1 + direction.z) * CloudScaleZ));
                        pixels[y * SkySize + x] = new Color(SkyTint.r * brightness, SkyTint.g * brightness, brightness);
                    }
                }

                map.SetPixels(pixels, (CubemapFace)face);
            }

            map.Apply();
            AssetDatabase.CreateAsset(map, SkyPath);
            return map;
        }

        // The direction through texel (u, v) of a cube face, in Unity's face order.
        private static Vector3 FaceDirection(int face, float u, float v)
        {
            switch (face)
            {
                case 0:
                    return new Vector3(1, -v, -u);
                case 1:
                    return new Vector3(-1, -v, u);
                case 2:
                    return new Vector3(u, 1, v);
                case 3:
                    return new Vector3(u, -1, -v);
                case 4:
                    return new Vector3(u, -v, 1);
                default:
                    return new Vector3(-u, -v, -1);
            }
        }
    }
}
