using UnityEngine;

namespace NETRunner92
{
    public sealed class ResidualPower : MonoBehaviour
    {
        static readonly int Emissive = Shader.PropertyToID("_EmissiveColor");
        [SerializeField] Renderer display;
        [SerializeField] Light fixture;
        [SerializeField] float phase;
        [SerializeField] int slot;
        Color emission;
        float nominal;
        MaterialPropertyBlock block;
#if UNITY_EDITOR
        internal Light DiagnosticFixture => fixture;
#endif

        public void Configure(Renderer screen, int materialIndex, Light lamp, float offset)
        {
            display = screen;
            slot = materialIndex;
            fixture = lamp;
            phase = offset;
        }

        void Awake()
        {
            if (fixture) nominal = fixture.intensity;
            if (!display) return;
            emission = display.sharedMaterials[slot].GetColor(Emissive);
            block = new MaterialPropertyBlock();
            display.GetPropertyBlock(block, slot);
        }

        void Update()
        {
            float time = Time.time + phase;
            float cycle = Mathf.Repeat(time, 11.5f + phase * .09f);
            float factor;
            if (fixture)
            {
                factor = cycle < .13f || cycle > .29f && cycle < .43f ? .04f
                    : cycle < 1.20f && cycle > .64f ? .27f : 1f;
                fixture.intensity = nominal * factor;
            }
            else factor = cycle < .28f ? .18f : .78f + Mathf.Sin(time * 3.1f) * .20f;
            if (GameOptions.Active.reducedFlashing)
            {
                factor = 1;
                if (fixture) fixture.intensity = nominal;
            }
            if (!display) return;
            block.SetColor(Emissive, emission * factor);
            display.SetPropertyBlock(block, slot);
        }
    }
}
