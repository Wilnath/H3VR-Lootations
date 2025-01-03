﻿using FistVR;
using SupplyRaid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Lootations
{
    public class LootObject : MonoBehaviour
    {
        public GameObject[] LootSpawnPoints;
        private LootSpawnPoint[] spawnPoints;
        public bool OneShot = false;
        public bool HasResetDistanceGrace = true;

        [HideInInspector]
        public bool Triggered = false;

        public delegate void TriggerResetDelegate();
        public event TriggerResetDelegate OnTriggerReset;

        public delegate void LootObjectTriggeredDelegate();
        public event LootObjectTriggeredDelegate LootObjectTriggered;

        public readonly float PLAYER_PROXIMITY_GRACE_DISTANCE = 25f;

        public static void HookTrigger(GameObject lootObjectOwner, MonoBehaviour trigger)
        {
            LootObject lootObject;
            if (lootObjectOwner == null)
            {
                lootObject = trigger.GetComponent<LootObject>();
            } 
            else
            {
                lootObject = lootObjectOwner.GetComponent<LootObject>();
            }
            if (lootObject == null)
            {
                Lootations.Logger.LogError("Cannot hook loot trigger on object without one: " + lootObject.name);
                return;
            }
            if (trigger is ILootTrigger) 
            {
                ILootTrigger lootTrigger = trigger as ILootTrigger;
                lootObject.OnTriggerReset += lootTrigger.LootReset;
                lootTrigger.OnTriggered += lootObject.Trigger;
                LootManager.AddLootTrigger(lootTrigger);
            }
        }

        public void Awake()
        {
            if (LootSpawnPoints == null)
            {
                Lootations.Logger.LogWarning("LootTrigger skipped initialization due to missing loot points.");
            }
            spawnPoints = Utilities.GameObjectsToPoints(LootSpawnPoints);
            LootManager.AddLootObject(this);
        }

        public void EnsureOwnerOfSpawnedItems()
        {
            foreach (GameObject spawnPointObj in LootSpawnPoints)
            {
                LootSpawnPoint spawnPoint = spawnPointObj.GetComponent<LootSpawnPoint>();
                if (spawnPoint == null)
                {
                    return;
                }

                foreach (GameObject spawnedItem in spawnPoint.SpawnedLoot)
                {

                }
            }
        }

        public void Trigger(ILootTrigger trigger)
        {
            if (Triggered)
            {
                return;
            }
            Triggered = true;
            LootObjectTriggered?.Invoke();
            if (Lootations.h3mpEnabled && Networking.IsConnected())
            {
                Networking.SendTriggerActivated(LootManager.GetLootTriggerId(trigger));
                if (Networking.IsClient())
                {
                    return;
                }
            }
            SpawnItemsAtLootSpawns();
        }

        private void SpawnItemsAtLootSpawns()
        {
            foreach (var item in spawnPoints)
            {
                // I think this got added sometime due to a mystical one-off error? No idea why we're nullchecking here
                if (item is null)
                {
                    Lootations.Logger.LogError("Object: " + name + " contained non loot spawn point!");
                    continue;
                }
                item.Trigger();
            }
        }

        public void Reset()
        {
            if (OneShot)
            {
                return;
            }
            Triggered = false;
            OnTriggerReset?.Invoke();
        }

        public void OnDestroy()
        {
            LootManager.RemoveLootObject(this);
        }
    }
}
