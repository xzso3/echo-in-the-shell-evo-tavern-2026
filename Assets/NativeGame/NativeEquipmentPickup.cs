using UnityEngine;

namespace Echo.NativeGame
{
    [RequireComponent(typeof(NativeInteraction))]
    public sealed class NativeEquipmentPickup : MonoBehaviour
    {
        public NativeEquipment equipment;
        NativeInteraction interaction;

        void OnEnable()
        {
            interaction = GetComponent<NativeInteraction>();
            interaction.Confirmed += OnConfirmed;
        }

        void OnDisable()
        {
            if (interaction) interaction.Confirmed -= OnConfirmed;
        }

        void OnConfirmed(NativeInteraction source)
        {
            if (source != interaction || !equipment || !equipment.TryPickUp()) return;
            source.Consume();
            gameObject.SetActive(false);
        }
    }
}
