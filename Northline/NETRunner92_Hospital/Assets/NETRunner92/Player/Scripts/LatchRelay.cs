using UnityEngine;

namespace NETRunner92
{
    public sealed class LatchRelay : HospitalInteractable
    {
        static readonly int Emission = Shader.PropertyToID("_EmissiveColor");
        [SerializeField] Renderer indicator;
        public bool Linked { get; private set; }
        public override string Prompt => Linked ? "Q  Service relay linked" : "Q  Open LATCH-07 / service relay";

        public void Configure(Renderer lamp) => indicator = lamp;
        public override void Use(FirstPersonController player) => player.GetComponent<LatchViewmodel>()?.Toggle(this);

        public bool TryLink(FirstPersonController player)
        {
            var eye = player.Eyes.transform.position;
            var contact = transform.position;
            if ((eye - contact).sqrMagnitude > HospitalInteraction.Reach * HospitalInteraction.Reach
                || Physics.Linecast(eye, contact, ~(1 << 2), QueryTriggerInteraction.Ignore)) return false;
            Linked = true;
            var block = new MaterialPropertyBlock();
            indicator.GetPropertyBlock(block);
            block.SetColor(Emission, new Color(.19f, 1f, .79f) * 2f);
            indicator.SetPropertyBlock(block);
            return true;
        }
    }
}
