using System;
using NetRunner.SceneBuilding.Editor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace NetRunner.Atmosphere.Editor
{
    /// <summary>
    /// Turns the source model's light fixtures into Unity lights: spots for
    /// the small outdoor fittings, points elsewhere, each floor at its own
    /// strength. A few cast soft shadows outdoors; the lockout lamps pulse as
    /// emergency lights and about one fixture in thirteen indoors flickers.
    /// </summary>
    public static class PracticalLights
    {
        private const int Floors = 5;
        private const int Grounds = 0;
        private const int WardFloor = 2;
        private const int ResearchFloor = 4;
        private const string LockoutMark = "lockout";

        // Outdoor fittings smaller than this are spots, aimed down their direction.
        private const float LargestSpot = 3.4f;
        private const float SpotAngle = 116f;
        private const float InnerSpotAngle = 78f;

        // A direction shorter than this means the fixture shines all round;
        // one this close to vertical looks along the floor's forward instead.
        private const float AimedLength = .1f;
        private const float Vertical = .97f;

        // Intensity: the source energy times this, times the floor's strength, within these limits.
        private const float EnergyScale = 7.5f;
        private const float WardStrength = .55f;
        private const float ResearchStrength = 1.1f;
        private const float FloorStrength = .82f;
        private const float MinimumIntensity = 40f;
        private const float MaximumIntensity = 1400f;

        // Range: fixed outdoors; indoors it grows with the fitting's size.
        private const float GroundsRange = 8.5f;
        private const float RangeBase = 3.0f;
        private const float RangePerSize = .65f;
        private const float MinimumRange = 3.8f;
        private const float MaximumRange = 6.0f;

        // Every seventh outdoor light casts shadows; every thirteenth indoor one flickers.
        private const int ShadowEvery = 7;
        private const int ShadowSlot = 1;
        private const int FlickerEvery = 13;
        private const int FlickerSlot = 3;

        // How strongly a light lights the volumetric fog: more in surgery's haze.
        private const int SurgeryFloor = 3;
        private const float SurgeryFogDimmer = .22f;
        private const float FogDimmer = .12f;

        private const float EmergencyRate = .5f;
        private const float EmergencyLow = .64f;
        private const float FlickerRate = .61f;
        private const float FlickerLow = .9f;

        /// <summary>Adds a light for each fixture in <paramref name="fixtures"/> under <paramref name="root"/>.</summary>
        public static void Add(PracticalLight[] fixtures, Transform root)
        {
            var counts = new int[Floors];
            foreach (PracticalLight source in fixtures)
            {
                int floor = Mathf.Clamp(source.Floor, Grounds, ResearchFloor);
                int index = counts[floor]++;
                Transform fixture = SceneParts.Node(source.Name, root);
                fixture.position = source.Position;
                Vector3 aim = source.Direction;
                if (aim.sqrMagnitude > AimedLength)
                {
                    fixture.rotation = Quaternion.LookRotation(aim, Mathf.Abs(aim.y) > Vertical ? Vector3.forward : Vector3.up);
                }

                Light light = fixture.gameObject.AddComponent<Light>();
                light.type = floor == Grounds && source.Size < LargestSpot ? LightType.Spot : LightType.Point;
                light.spotAngle = SpotAngle;
                light.innerSpotAngle = InnerSpotAngle;
                light.color = source.Color;
                float strength = floor == WardFloor ? WardStrength : floor == ResearchFloor ? ResearchStrength : FloorStrength;
                light.intensity = Mathf.Clamp(source.Energy * EnergyScale * strength, MinimumIntensity, MaximumIntensity);
                light.range = floor == Grounds ? GroundsRange : Mathf.Clamp(RangeBase + source.Size * RangePerSize, MinimumRange, MaximumRange);
                light.shadows = floor == Grounds && index % ShadowEvery == ShadowSlot ? LightShadows.Soft : LightShadows.None;
                fixture.gameObject.AddComponent<HDAdditionalLightData>().volumetricDimmer = floor == SurgeryFloor ? SurgeryFogDimmer : FogDimmer;
                if (source.Name.Contains(LockoutMark, StringComparison.OrdinalIgnoreCase))
                {
                    fixture.gameObject.AddComponent<PracticalPulse>().Configure(PracticalPulse.Pattern.Emergency, EmergencyRate, EmergencyLow, 0);
                }
                else if (floor < ResearchFloor && index % FlickerEvery == FlickerSlot)
                {
                    fixture.gameObject.AddComponent<PracticalPulse>().Configure(PracticalPulse.Pattern.Intermittent, FlickerRate, FlickerLow, index);
                }
            }
        }
    }
}
