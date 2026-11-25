using NetRunner.Core;
using NetRunner.SceneBuilding.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace NetRunner.Atmosphere.Editor
{
    /// <summary>
    /// Makes the hospital readable in the dark: adds low, shadowless point
    /// lights along each floor's concourse and stair landings and over the
    /// reception and ambulance desks, as if light bounced off the clinical
    /// fittings, and lowers the indoor zones' exposure to match. Replaces
    /// the lights it added before, so it can run again.
    /// </summary>
    public static class ClinicalFillLights
    {
        private const string GroupName = "Low-output clinical fill";
        private const string LightName = "Reflected clinical practical";
        private const string FogFolder = HospitalAssets.Atmosphere + "/Fog";
        private const int Floors = 4;
        private const float FogDimmer = .08f;

        // Along the concourse: every 8 m from x = -28 to 28, 2.86 m up.
        private const int ConcourseStart = -28;
        private const int ConcourseEnd = 28;
        private const int ConcourseStep = 8;
        private const float CeilingHeight = 2.86f;
        private const float ConcourseZ = .05f;
        private const float ConcourseRange = 6.6f;
        private const float WardPower = 190f;
        private const float ConcoursePower = 320f;
        private const int WardFloor = 1;
        private const int ResearchFloor = 3;

        // At both stair cores: the landing and, below the top floor, the flight.
        private const float LandingZ = -4.15f;
        private const float LandingPower = 230f;
        private const float LandingRange = 6.2f;
        private const float FlightHeight = 3.35f;
        private const float FlightZ = -9.09f;
        private const float FlightPower = 195f;
        private const float FlightRange = 5.2f;

        // Over the desks.
        private const float DeskPower = 440f;
        private const float DeskRange = 7.2f;

        // Indoor exposure after the fill: brighter on the wards and in research.
        private const string StormProfile = "Storm";
        private const string WardProfile = "Patient";
        private const string ResearchProfile = "Research";
        private const float WardExposure = 2.65f;
        private const float ResearchExposure = 2.80f;
        private const float IndoorExposure = 2.5f;

        private static readonly int[] StairCores = { -32, 32 };

        private static readonly Vector3[] Desks =
        {
            new Vector3(-4.5f, 2.93f, -10.15f), new Vector3(4.2f, 2.95f, -9.4f), new Vector3(-21.8f, 2.95f, 8.6f),
        };

        private static readonly Color ConcourseColor = new Color(.78f, .86f, 1f);
        private static readonly Color ResearchColor = new Color(.78f, .93f, 1f);
        private static readonly Color LandingColor = new Color(.74f, .84f, .94f);
        private static readonly Color FlightColor = new Color(.77f, .85f, .98f);
        private static readonly Color DeskColor = new Color(.77f, .87f, .99f);

        /// <summary>Adds the fill lights to the hospital scene, sets the indoor exposure and saves both.</summary>
        public static void Refine()
        {
            Scene scene = EditorSceneManager.OpenScene(HospitalAssets.ScenePath);
            Transform root = GameObject.Find(HospitalHierarchy.Atmosphere).transform;
            Transform previous = root.Find(GroupName);
            if (previous)
            {
                Object.DestroyImmediate(previous.gameObject);
            }

            Transform fills = SceneParts.Node(GroupName, root);
            for (int floor = 0; floor < Floors; floor++)
            {
                float elevation = floor * StairRules.FloorHeight;
                for (int x = ConcourseStart; x <= ConcourseEnd; x += ConcourseStep)
                {
                    Fill(fills, new Vector3(x, elevation + CeilingHeight, ConcourseZ), floor == WardFloor ? WardPower : ConcoursePower,
                        ConcourseRange, floor == ResearchFloor ? ResearchColor : ConcourseColor);
                }

                foreach (int x in StairCores)
                {
                    Fill(fills, new Vector3(x, elevation + CeilingHeight, LandingZ), LandingPower, LandingRange, LandingColor);
                    if (floor < ResearchFloor)
                    {
                        Fill(fills, new Vector3(x, elevation + FlightHeight, FlightZ), FlightPower, FlightRange, FlightColor);
                    }
                }
            }

            foreach (Vector3 desk in Desks)
            {
                Fill(fills, desk, DeskPower, DeskRange, DeskColor);
            }

            SetIndoorExposure();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static void SetIndoorExposure()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:VolumeProfile", new[] { FogFolder }))
            {
                VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(AssetDatabase.GUIDToAssetPath(guid));
                if (profile.name.Contains(StormProfile))
                {
                    continue;
                }

                if (profile.TryGet<Exposure>(out Exposure exposure))
                {
                    exposure.fixedExposure.Override(profile.name.Contains(WardProfile) ? WardExposure
                        : profile.name.Contains(ResearchProfile) ? ResearchExposure : IndoorExposure);
                    EditorUtility.SetDirty(exposure);
                }
            }
        }

        private static void Fill(Transform root, Vector3 position, float power, float range, Color color)
        {
            Transform node = SceneParts.Node(LightName, root);
            node.position = position;
            Light light = node.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = power;
            light.range = range;
            light.shadows = LightShadows.None;
            node.gameObject.AddComponent<HDAdditionalLightData>().volumetricDimmer = FogDimmer;
        }
    }
}
