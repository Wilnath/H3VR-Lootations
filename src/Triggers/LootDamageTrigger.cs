using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Lootations
{
    public class LootDamageTrigger : MonoBehaviour, IFVRDamageable, ILootTrigger
    {
        public GameObject LootObjectOwner;
        public Damage.DamageClass triggerDamageType = Damage.DamageClass.Melee;
        public float triggerDamageSize = 10f;

        public event ILootTrigger.OnTriggeredDelegate OnTriggered;

        public void Awake()
        {
            LootObject.Hook(LootObjectOwner, this);
        }

        // Event for when we are damaged by something in the game
        void IFVRDamageable.Damage(Damage dam)
        {
            // Doesn't seem to quite work as expected right now
            if (dam.Class != triggerDamageType) // || dam.damageSize < triggerDamageSize)
            {
                return;
            }
            OnTriggered?.Invoke();
            GetComponent<AudioSource>()?.Play();
        }

        public void Reset()
        {
        }
    }
}
