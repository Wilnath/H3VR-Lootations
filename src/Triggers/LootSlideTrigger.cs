using FistVR;
using UnityEngine;

namespace Lootations
{
    public class LootSlideTrigger : FVRInteractiveObject, ILootTrigger
    {
        public GameObject LootObjectOwner;
        public GameObject Root;
        public GameObject Hinge;
        public float MaxOffset = 0.25f;

        public event ILootTrigger.OnTriggeredDelegate OnTriggered;

        public override void Awake()
        {
            if (Root == null || Hinge == null)
            {
                Lootations.Logger.LogError("SlideTrigger doesn't have a root or hinge, this is misconfigured!");
                gameObject.SetActive(false);
                return;
            }
            LootObject.Hook(LootObjectOwner, this);
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            OnTriggered?.Invoke();
            base.BeginInteraction(hand);
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);
            Vector3 vec = Root.transform.InverseTransformPoint(hand.Input.Pos);
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
    }
}

