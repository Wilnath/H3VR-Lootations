using FistVR;
using SupplyRaid;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using H3MP.Tracking;
using Sodalite.Api;

namespace Lootations
{
    public static class LootManager
    {
        private static readonly int MAX_SPAWNS = -1;

        public static Dictionary<GameObject, LootSpawnPoint> spawnedLoot = new();
        private static List<LootObject> lootTriggers = new();
        private static List<LootObjectRandomizer> objectSpawns = new List<LootObjectRandomizer>();
        private static List<LootSpawnPoint> lootSpawns = new();
        private static int spawnPointsActivated = 0;

        public static readonly Vector2 MAG_AMOUNT_RANGE = new Vector2(2, 4);

        public static readonly float Y_SPAWN_INCREMENT = 0f;

        static LootManager()
        {
            SR_Manager.SupplyPointChangeEvent += OnSupplyPointChange;
        }

        public static bool AddLootable(LootSpawnPoint lootable)
        {
            lootSpawns.Add(lootable);
            return true;
        }

        public static void RemoveLootable(LootSpawnPoint lootable)
        {
            if (lootSpawns.Contains(lootable))
            {
                lootSpawns.Remove(lootable);
            }
        }

        public static bool AddTrigger(LootObject trigger)
        {
            lootTriggers.Add(trigger);
            return true;
        }

        public static bool RemoveTrigger(LootObject trigger)
        {
            if (!lootTriggers.Contains(trigger))
            {
                return false;
            }
            lootTriggers.Add(trigger);
            return true;
        }

        public static bool AddRandomObject(LootObjectRandomizer obj)
        {
            objectSpawns.Add(obj);
            return true;
        }

        public static void OnPhysicalObjectPickup(GameObject obj)
        {
            if (Networking.isClient())
            {
                Networking.SendItemGrab(obj);
            }

            if (spawnedLoot.ContainsKey(obj))
            {
                Lootations.Logger.LogDebug("Removed object from tracked spawned loot pool.");
                spawnedLoot[obj].StopTrackingObject(obj);
                spawnedLoot.Remove(obj);
                Networking.SendItemGrab(obj);
            }
        }

        public static void OnSceneSwitched()
        {
            Lootations.Logger.LogInfo("Removing track of spawned items.");
            spawnedLoot.Clear();
            lootSpawns.Clear();
            lootTriggers.Clear();
            objectSpawns.Clear();
        }

        public static void StopTrackingNetworkId(int trackingId)
        {
            Lootations.Logger.LogDebug("Attempting to remove " + trackingId.ToString());
            foreach (var kvp in spawnedLoot)
            {
                GameObject obj = kvp.Key;
                TrackedObjectData data = obj.GetComponent<TrackedItem>().data;
                if (data != null && data.trackedID == trackingId)
                {
                    Lootations.Logger.LogDebug("Found object to remove! Removing a " + obj.name);
                    spawnedLoot.Remove(obj);
                    break;
                }
            }
        }

        private static void ShuffleSpawns()
        {
            lootSpawns = lootSpawns.OrderBy(_ => Random.Range(0f, 1f)).ToList();
        }

        private static void OnSupplyPointChange()
        {
            int captures = SR_Manager.instance.CurrentCaptures;
            RerollLoot(captures);
        }

        public static void OnTNHLevelSet(int level)
        {
            RerollLoot(level);
        }

        private static void RerollLoot(int level)
        {
            // Avoid unneccessarily respawning items
            /*if (captures == 0)
            {
                Lootations.Logger.LogInfo("Skipping loot respawn due to captures = 0");
                return;
            }*/

            spawnPointsActivated = 0;
            ShuffleSpawns();
            spawnedLoot = new();

            Lootations.Logger.LogDebug("Respawning objects.");
            for (int i = 0; i < lootSpawns.Count; i++)
            {
                LootSpawnPoint point = lootSpawns[i];
                point.Reset();
            }

            // Reroll loot object.
            for (int i = 0; i < objectSpawns.Count; i++)
            {
                LootObjectRandomizer randomObject = objectSpawns[i];
                randomObject.RollAndSpawn();
            }

            for (int i = 0; i < lootTriggers.Count; i++)
            {
                LootObject trigger = lootTriggers[i];
                trigger.Reset();
            }

            // Set loot spawns to correct table.
            for (int i = 0; i < lootSpawns.Count; i++)
            {
                LootSpawnPoint lootable = lootSpawns[i];
                lootable.SetLevel(level);
            }

            if (MAX_SPAWNS != -1 && spawnPointsActivated < MAX_SPAWNS)
            {
                Lootations.Logger.LogWarning("Spawned less items than max spawns allows for. Maybe up the spawn points?");
            }
        }

        public static Quaternion RandomRotation()
        {
            return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
    }
}
