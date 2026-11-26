using System;
using System.Linq;
using System.Text.RegularExpressions;
using NetRunner.Atmosphere;
using NetRunner.SceneBuilding.Editor;
using UnityEngine;

namespace NetRunner.World.Editor
{
    /// <summary>
    /// Adds the hospital's residual power: on each floor one failing
    /// concourse strip light, with the glowing strip next to it, dips and
    /// recovers on a staggered cycle; and every concourse index readout and
    /// transfer console display flickers on its own phase.
    /// </summary>
    public static class ResidualPowerBuilder
    {
        private const string ReadoutSuffix = "_Concourse_Index_Readout";
        private const string ConsoleSuffix = "_Transfer_Console_Display";
        private const string EmissiveColor = "_EmissiveColor";

        // A material glows when its emissive colour is brighter than this.
        private const float GlowThreshold = .04f;

        // Seconds between the failing lights' cycles, and between the readouts'.
        private const float LightStagger = 2.23f;
        private const float ReadoutStagger = 1.74f;

        // Floors are 4.2 m apart; a lamp belongs to the floor whose height is within half of that.
        private const float HalfFloor = 2.1f;

        // A concourse lamp and its fixture: F01_L_Strip_Concourse_350 on L1,
        // F02_L_Concourse_466 above; the Restricted concourse on L4 is not one.
        private static readonly Regex ConcourseLamp = new Regex(@"^F0\d_L_(Strip_)?Concourse_\d+$");

        // Where on each floor, L1 to L4, the failing light is picked: the concourse lamp nearest this point.
        private static readonly Vector3[] FailingLights =
        {
            new Vector3(-16, 3.5f, 0), new Vector3(19, 7.7f, 0), new Vector3(-14, 11.9f, 0), new Vector3(16, 16.1f, 0),
        };

        /// <summary>Adds the failing lights and readouts under <paramref name="systems"/>; <paramref name="geometry"/> is the building's renderers.</summary>
        public static void Add(Transform systems, MeshRenderer[] geometry)
        {
            Transform root = SceneParts.Node("Residual power", systems);
            Light[] fixtures = GameObject.Find(HospitalHierarchy.HospitalPracticals).GetComponentsInChildren<Light>()
                .Where(l => ConcourseLamp.IsMatch(l.name)).ToArray();
            for (int i = 0; i < FailingLights.Length; i++)
            {
                // Each floor's own lamp: searching the whole building put all four on L1 (found for #79).
                Vector3 wanted = FailingLights[i];
                Light lamp = fixtures.Where(l => Mathf.Abs(l.transform.position.y - wanted.y) < HalfFloor)
                    .OrderBy(l => (l.transform.position - wanted).sqrMagnitude).First();
                PracticalPulse pulse = lamp.GetComponent<PracticalPulse>();
                if (pulse)
                {
                    UnityEngine.Object.DestroyImmediate(pulse);
                }

                MeshRenderer strip = geometry.Where(r => ConcourseLamp.IsMatch(r.name))
                    .OrderBy(r => (r.transform.position - lamp.transform.position).sqrMagnitude).FirstOrDefault();
                int slot = EmissiveSlot(strip);
                SceneParts.Node($"Failing practical L{i + 1:00}", root).gameObject.AddComponent<ResidualPower>()
                    .Configure(slot >= 0 ? strip : null, slot, lamp, i * LightStagger);
            }

            MeshRenderer[] readouts = geometry.Where(r => r.name.EndsWith(ReadoutSuffix) || r.name.EndsWith(ConsoleSuffix))
                .OrderBy(r => r.name).ToArray();
            for (int i = 0; i < readouts.Length; i++)
            {
                MeshRenderer readout = readouts[i];
                int slot = EmissiveSlot(readout);
                if (slot >= 0)
                {
                    SceneParts.Node(readout.name, root).gameObject.AddComponent<ResidualPower>().Configure(readout, slot, null, i * ReadoutStagger);
                }
            }
        }

        // The index of the renderer's first glowing material, or -1.
        private static int EmissiveSlot(MeshRenderer renderer)
        {
            if (!renderer)
            {
                return -1;
            }

            return Array.FindIndex(renderer.sharedMaterials, m =>
                m && m.HasProperty(EmissiveColor) && m.GetColor(EmissiveColor).maxColorComponent > GlowThreshold);
        }
    }
}
