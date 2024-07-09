using FistVR;
using UnityEngine;

namespace Lootations
{
    public class LootSlideTrigger : FVRInteractiveObject, ILootTrigger
    {
        public GameObject LootObjectOwner;
        public GameObject Root;
        public float MaxOffset = 0.25f;

        public event ILootTrigger.OnTriggeredDelegate OnTriggered;

        public override void Awake()
        {
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
            Vector3 vec = Root.transform.position - hand.Input.Pos;
            Vector3 proj = Vector3.Project(vec, Root.transform.forward);
            float offset = Mathf.Clamp(proj.z, 0, MaxOffset);
            transform.localPosition = new Vector3(0, 0, offset);
        }

        public void Reset()
        {
            transform.localPosition = new Vector3(0, 0, 0);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.0f, 0.6f, 0.0f, 0.5f);
            Gizmos.DrawLine(Root.transform.position, Root.transform.position + Root.transform.forward * MaxOffset);
        }
    }
}

