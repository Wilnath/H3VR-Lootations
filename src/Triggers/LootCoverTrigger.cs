using UnityEngine;
using FistVR;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Lootations
{
    public class LootCoverTrigger : FVRInteractiveObject, ILootTrigger, IDistantManipulable, ITriggerDataReceiver
    {
        public GameObject LootObjectOwner;
        public Transform Root;
        public Transform Hinge;

        private float startAngle = 0f;
        private Vector3 offset = Vector3.zero;

        public float MinRot = 0.0f;
        public float MaxRot = 45.0f;

        public event ILootTrigger.OnTriggeredDelegate OnTriggered;

        public override void Awake()
        {
            base.Awake();
            startAngle = Hinge.transform.localEulerAngles.z;
            if (Root == null || Hinge == null)
            {
                Lootations.Logger.LogError("CoverTrigger doesn't have a root or hinge, this is misconfigured!");
                gameObject.SetActive(false);
                return;
            }
            LootObject.HookTrigger(LootObjectOwner, this);
        }

        public void Trigger()
        {
            OnTriggered?.Invoke(this);
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            offset = Vector3.zero;
            base.BeginInteraction(hand);
            GetComponent<AudioSource>()?.Play();
            Trigger();
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);
            Vector3 relative = Root.InverseTransformPoint(hand.Input.Pos + offset);
            float angle = Mathf.Atan2(relative.y, relative.x) * Mathf.Rad2Deg;
            angle = Mathf.Clamp(angle, MinRot, MaxRot);
            Hinge.transform.localEulerAngles = new Vector3(0, 0, angle);
            if (Networking.IsConnected())
            {
                List<byte> data = BitConverter.GetBytes(angle).ToList();
                Networking.SendTriggerDataUDP(this, data);
            }
        }

        public void LootReset()
        {
            // Reset rotation.
            Hinge.transform.localEulerAngles = new Vector3(0, 0, startAngle);
        }

        void OnDrawGizmos()
        {
            if (Root == null)
                return;

            Gizmos.color = new Color(0.0f, 0.6f, 0.0f, 0.5f);
            Gizmos.DrawLine(Root.transform.position, Root.transform.position + new Vector3(0, Root.transform.forward.y * Mathf.Sin(MinRot), Root.transform.forward.z * Mathf.Cos(MinRot)) * 0.25f);
            Gizmos.DrawLine(Root.transform.position, Root.transform.position + new Vector3(0, Root.transform.forward.y * Mathf.Sin(MaxRot), Root.transform.forward.z * Mathf.Cos(MaxRot)) * 0.25f);
        }

        public void OnDistantInteract(FVRViveHand hand)
        {
            offset = hand.m_grabHit.point - hand.Input.Pos;
        }

        public void ReceiveTriggerData(List<byte> data)
        {
            float angle = BitConverter.ToSingle(data.ToArray(), 0);
            Lootations.Logger.LogDebug("LIGMA called with data " + data.ToString() + " converted " + (angle));
            Hinge.transform.localEulerAngles = new Vector3(0, 0, angle);
        }
    }
}
