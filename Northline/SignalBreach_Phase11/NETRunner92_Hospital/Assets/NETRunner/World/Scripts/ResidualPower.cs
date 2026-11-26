using NetRunner.Core;
using NetRunner.Settings;
using UnityEngine;

namespace NetRunner.World
{
    /// <summary>
    /// A failing light or screen on the hospital's residual power: it dims
    /// and recovers on its own cycle, from Core's <see cref="PowerFlicker"/>.
    /// A lamp scales its light; a screen scales its glow, and a lamp's glow
    /// follows its light when it has one.
    /// </summary>
    public sealed class ResidualPower : MonoBehaviour
    {
        private static readonly int Emissive = Shader.PropertyToID("_EmissiveColor");

        [SerializeField] private Renderer display;
        [SerializeField] private Light fixture;
        [SerializeField] private float phase;
        [SerializeField] private int slot;

        [SerializeField] private Preferences preferences;

        private Color emission;
        private float nominal;
        private MaterialPropertyBlock block;

#if UNITY_EDITOR
        /// <summary>The light, for the hospital checks.</summary>
        public Light DiagnosticFixture => fixture;
#endif

        public void SetPreferences(Preferences settings) => preferences = settings;

        private void Awake()
        {
            if (fixture)
            {
                nominal = fixture.intensity;
            }

            if (!display)
            {
                return;
            }

            emission = display.sharedMaterials[slot].GetColor(Emissive);
            block = new MaterialPropertyBlock();
            display.GetPropertyBlock(block, slot);
        }

        private void Update()
        {
            float factor = preferences && preferences.Active.ReduceFlashing
                ? 1f : PowerFlicker.Level(Time.time, phase, fixture);
            if (fixture)
            {
                fixture.intensity = nominal * factor;
            }

            if (!display)
            {
                return;
            }

            block.SetColor(Emissive, emission * factor);
            display.SetPropertyBlock(block, slot);
        }

        /// <summary>Sets the glowing renderer and its material slot, the light and the phase in seconds; the scene builder calls it.</summary>
        public void Configure(Renderer screen, int materialIndex, Light lamp, float offset)
        {
            display = screen;
            slot = materialIndex;
            fixture = lamp;
            phase = offset;
        }
    }
}
