using FistVR;
using SupplyRaid;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lootations
{
    public class LootCardScanTrigger : MonoBehaviour, ILootTrigger
    {
        public GameObject LootObjectOwner;
        public int KeycardTier = 1;
        public event ILootTrigger.OnTriggeredDelegate OnTriggered;

        public void Awake()
        {
            LootObject.Hook(LootObjectOwner, this);
        }

        private void OnTriggerEnter(Collider other)
        {
            WW_Keycard cardComponent = other.GetComponent<WW_Keycard>();
            if (cardComponent != null)
            {
                if (cardComponent.TierType == KeycardTier)
                {
                    OnTriggered?.Invoke();
                }
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.0f, 0.6f, 0.0f, 0.5f);
        }

        public void LootReset()
        {
        }
    }
}

