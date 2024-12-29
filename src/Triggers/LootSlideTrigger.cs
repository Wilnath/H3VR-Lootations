using FistVR;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lootations
{
    public class LootSlideTrigger : FVRInteractiveObject, ILootTrigger, IDistantManipulable, ITriggerDataReceiver
    {
        public GameObject LootObjectOwner;
        public GameObject Root;
        public GameObject Hinge;
        public float MaxOffset = 0.25f;

        private Vector3 inputOffset = Vector3.zero;

        public event ILootTrigger.OnTriggeredDelegate OnTriggered;

        public override void Awake()
        {
            if (Root == null || Hinge == null)
            {
                Lootations.Logger.LogError("SlideTrigger doesn't have a root or hinge, this is misconfigured!");
                gameObject.SetActive(false);
                return;
            }
            LootObject.HookTrigger(LootObjectOwner, this);
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            base.BeginInteraction(hand);
            inputOffset = Vector3.zero;
            // ensure control of loot spawn points in a network
            Trigger();
        }

        public void Trigger()
        {
            OnTriggered?.Invoke(this);
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);
            Vector3 vec = Root.transform.InverseTransformPoint(hand.Input.Pos + inputOffset);
            float offset = Mathf.Clamp(vec.z, 0, MaxOffset);
            Hinge.transform.localPosition = new Vector3(0, 0, offset);
        }

        public void LootReset()
        {
            Hinge.transform.localPosition = new Vector3(0, 0, 0);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.0f, 0.6f, 0.0f, 0.5f);
            Gizmos.DrawLine(Root.transform.position, Root.transform.position + Root.transform.forward * MaxOffset);
        }

        public void OnDistantInteract(FVRViveHand hand)
        {
            BeginInteraction(hand);
            inputOffset = hand.m_grabHit.point - hand.Input.Pos;
        }

        public void ReceiveTriggerData(List<byte> data)
        {
            float angle = BitConverter.ToSingle(data.ToArray(), 0);
            Hinge.transform.localPosition = new Vector3(0, 0, angle);
        }
    }
}

