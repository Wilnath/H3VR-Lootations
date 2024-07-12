using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Lootations
{
    public class LootPhysicsTrigger : FVRPhysicalObject, ILootTrigger
    {
        [Header("Loot Trigger")]
        public GameObject LootObjectOwner;
        public event ILootTrigger.OnTriggeredDelegate OnTriggered;

        private Vector3 startingPosition;
        private Quaternion startingRotation;
        private Vector3 startingScale;

        public override void Awake()
        {
            startingPosition = transform.localPosition;
            startingRotation = transform.localRotation;
            startingScale = transform.localScale;
            LootObject.Hook(LootObjectOwner, this);
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            base.BeginInteraction(hand);
            OnTriggered?.Invoke();
        }

        public void LootReset()
        {
            transform.position = startingPosition;
            transform.rotation = startingRotation;
            transform.localScale = startingScale;
        }
    }
}
