using UnityEngine;

namespace NETRunner92
{
    public sealed class LatchRelay : HospitalInteractable
    {
        static readonly int Emission = Shader.PropertyToID("_EmissiveColor");
        [SerializeField] Renderer indicator;
        public bool Linked { get; private set; }
        public override (string Key, string Text) Prompt => (HospitalKeys.Label(GameAction.Tablet), Linked ? "Szervizrelé csatlakoztatva" : "LATCH-07 · Szervizrelé");

        public void Configure(Renderer lamp) => indicator = lamp;
        public override void Use(FirstPersonController player) => player.GetComponent<LatchViewmodel>()?.Toggle(this);

        public bool TryLink(FirstPersonController player)
        {
            var eye = player.Eyes.transform.position;
            var contact = transform.position;
            if ((eye - contact).sqrMagnitude > HospitalInteraction.Reach * HospitalInteraction.Reach
                || Physics.Linecast(eye, contact, ~(1 << 2), QueryTriggerInteraction.Ignore)) return false;
            RestoreLink();
            return true;
        }

        internal void RestoreLink()
        {
            Linked = true;
            if (!indicator) return;
            var block = new MaterialPropertyBlock();
            indicator.GetPropertyBlock(block);
            block.SetColor(Emission, new Color(.19f, 1f, .79f) * 2f);
            indicator.SetPropertyBlock(block);
        }
    }
}
