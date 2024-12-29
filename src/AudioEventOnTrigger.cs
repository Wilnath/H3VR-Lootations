using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Lootations
{
    public class AudioEventOnTrigger : MonoBehaviour
    {
        public GameObject TriggerOwner;
        public AudioEvent TriggerAudioEvent;

        public void Awake()
        {
            if (TriggerOwner == null)
            {
                TriggerOwner = gameObject;
            }

            // TODO: This entire order of operations is getting very annoying,
            // there should be some utility function that can do most of this boring error checking for me
            ILootTrigger trig = TriggerOwner.GetComponent<ILootTrigger>();
            if (trig == null)
            {
                Lootations.Logger.LogError("AudioEvent missing trigger");
                return;
            }

            trig.OnTriggered += Triggered;
        }

        public void Triggered(ILootTrigger _)
        {
            SM.PlayGenericSound(TriggerAudioEvent, gameObject.transform.position);
        }
    }
}
