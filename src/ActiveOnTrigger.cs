using System.Collections.Generic;
using UnityEngine;

namespace Lootations
{
    public class ActiveOnTrigger : MonoBehaviour
    {
        public GameObject TriggerOwner;
        public GameObject[] ToActivate;
        public GameObject[] ToDeactivate;

        public void Start()
        {
            if (TriggerOwner == null) 
            {
                TriggerOwner = gameObject;
            }
            ILootTrigger obj = TriggerOwner.GetComponent<ILootTrigger>();
            if (obj == null)
            {
                Lootations.Logger.LogError("ActiveOnTrigger could not find a trigger on specified GameObject.");
                return;
            }
            obj.OnTriggered += Triggered;
        }

        private void Triggered(ILootTrigger _)
        {
            foreach (GameObject obj in ToActivate)
            {
                obj.SetActive(true);
            }
            foreach (GameObject obj in ToDeactivate)
            {
                obj.SetActive(false);
            }
        }
    }
}
