﻿using UnityEngine;
using FistVR;

namespace Lootations
{
    public class LootCoverTrigger : FVRInteractiveObject, ILootTrigger
    {
        public GameObject LootObjectOwner;
        public Transform Root;
        public Transform Hinge;

        private float startAngle = 0f;

        public float MinRot = 0.0f;
        public float MaxRot = 45.0f;

        public event ILootTrigger.OnTriggeredDelegate OnTriggered;

        public override void Awake()
        {
            base.Awake();
            startAngle = Hinge.transform.localEulerAngles.z;
            LootObject.Hook(LootObjectOwner, this);
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            base.BeginInteraction(hand);
            GetComponent<AudioSource>()?.Play();
            OnTriggered?.Invoke();
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);
            Vector3 relative = Root.InverseTransformPoint(hand.Input.Pos);
            float angle = Mathf.Atan2(relative.y, relative.x) * Mathf.Rad2Deg;
            angle = Mathf.Clamp(angle, MinRot, MaxRot);
            Hinge.transform.localEulerAngles = new Vector3(0, 0, angle);
        }

        public void Reset()
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
    }
}