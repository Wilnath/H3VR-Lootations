using FistVR;
using JetBrains.Annotations;
using SupplyRaid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Lootations
{
    public class LootProximityTrigger : MonoBehaviour, ILootTrigger
    {
        public GameObject LootObjectOwner;

        public event ILootTrigger.OnTriggeredDelegate OnTriggered;

        [Header("Instance Settings")]
        public bool UseInstanceSettings = false;
        public float TriggerDistance = 250f;

        private float triggerDistance;

        public void Awake()
        {
            LootObject.HookTrigger(LootObjectOwner, this);
            if (UseInstanceSettings)
            {
                triggerDistance = TriggerDistance;
            }
            else
            {
                triggerDistance = Lootations.ProximitySpawnDistance.Value;
            }
        }

        public void Trigger()
        {
            OnTriggered?.Invoke(this);
        }

        public void LootReset()
        {
            // TODO
            return;
        }

        public void Update()
        {
            if (Utilities.PlayerWithinDistance(transform.position, triggerDistance))
            {
                Trigger();
                //enabled = false;
            }
        }
    }
}
