using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hexacorp.K7
{
    /// <summary>Asset integration helpers. AI, navigation and hacking rules belong to the game.</summary>
    public sealed class K7Robot : MonoBehaviour
    {
        public enum SensorState { Patrol, Suspicious, Hostile, Hacked }
        [Header("Gameplay sockets")]
        public Transform visionOrigin;
        public Transform hackPoint;
        public Transform audioOrigin;
        public Transform headPoint;
        public Transform chestPoint;
        public Transform handLeft;
        public Transform handRight;
        public Transform footLeft;
        public Transform footRight;
        [Header("Sensor emission")]
        [Min(0)] public float emissionIntensity = 5f;
        [SerializeField] private SensorState sensorState = SensorState.Hostile;
        public SensorState State => sensorState;
        private readonly List<LightSlot> lightSlots = new List<LightSlot>();
        private MaterialPropertyBlock block;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private struct LightSlot { public Renderer renderer; public int index; }

        private void Awake() { BindSockets(); CacheLights(); SetState(sensorState); }

        public void BindSockets()
        {
            visionOrigin = Find("VisionOrigin"); hackPoint = Find("HackPoint");
            audioOrigin = Find("AudioOrigin"); headPoint = Find("HeadPoint");
            chestPoint = Find("ChestPoint"); handLeft = Find("Hand_L_Point");
            handRight = Find("Hand_R_Point"); footLeft = Find("Foot_L_Point");
            footRight = Find("Foot_R_Point");
        }

        public Transform Find(string socketName)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == socketName) return t;
            return null;
        }

        private void CacheLights()
        {
            lightSlots.Clear();
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    if (materials[i] != null && materials[i].name.StartsWith("MAT_K7_StatusLight", StringComparison.Ordinal))
                        lightSlots.Add(new LightSlot { renderer = renderer, index = i });
            }
            if (block == null) block = new MaterialPropertyBlock();
        }

        public void SetState(SensorState state)
        {
            sensorState = state;
            if (block == null) CacheLights();
            Color color;
            switch (state)
            {
                case SensorState.Suspicious: color = new Color(1f, .30f, .015f); break;
                case SensorState.Hostile: color = new Color(1f, .012f, .007f); break;
                case SensorState.Hacked: color = new Color(.015f, .26f, 1f); break;
                default: color = new Color(.65f, .82f, 1f); break;
            }
            foreach (var slot in lightSlots)
            {
                if (slot.renderer == null) continue;
                block.Clear();
                slot.renderer.GetPropertyBlock(block, slot.index);
                block.SetColor(EmissionColor, color * emissionIntensity);
                slot.renderer.SetPropertyBlock(block, slot.index);
            }
        }

        /// <summary>Hide a damage panel consistently across every LOD, e.g. Armor_Chest.</summary>
        public bool SetArmorVisible(string basePartName, bool visible)
        {
            if (string.IsNullOrEmpty(basePartName) || !basePartName.StartsWith("Armor_", StringComparison.Ordinal)) return false;
            bool found = false;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r.name == basePartName || r.name.StartsWith(basePartName + "_LOD", StringComparison.Ordinal))
                {
                    // GameObject activation survives subsequent LODGroup renderer changes.
                    r.gameObject.SetActive(visible);
                    found = true;
                }
            }
            return found;
        }
    }
}
