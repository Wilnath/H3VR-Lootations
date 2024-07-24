using UnityEngine;
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

        private bool JustInteracted = false;

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
            if (!JustInteracted)
            {
                Hinge.transform.localEulerAngles = new Vector3(0, 0, MaxRot);
            }
            OnTriggered?.Invoke(this);
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            base.BeginInteraction(hand);
            GetComponent<AudioSource>()?.Play();
            JustInteracted = true;
            Trigger();
            JustInteracted = false;
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);
            Vector3 relative = Root.InverseTransformPoint(hand.Input.Pos);
            float angle = Mathf.Atan2(relative.y, relative.x) * Mathf.Rad2Deg;
            angle = Mathf.Clamp(angle, MinRot, MaxRot);
            Hinge.transform.localEulerAngles = new Vector3(0, 0, angle);
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
    }
}
