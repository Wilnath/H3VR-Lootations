using System.Collections.Generic;
using UnityEngine;

namespace Lootations
{
    public class EnableHook : MonoBehaviour
    {
        public GameObject LootObjectOwner;
        public bool Inverted = false;

        public void Awake()
        {
            LootObject obj = LootObjectOwner.GetComponent<LootObject>();
            if (obj == null)
            {
                Lootations.Logger.LogError("VisibilityHook could not find LootObject.");
                return;
            }
            obj.LootObjectTriggered += Triggered;
            obj.OnTriggerReset += Reset;
            SetEnabled(true);
        }

        private void Reset()
        {
            SetEnabled(true);
        }

        private void Triggered()
        {
            SetEnabled(false);
        }

        private void SetEnabled(bool visible)
        {
            if (Inverted)
            {
                visible = !visible;
            }
            gameObject.SetActive(visible);
        }
    }
}
