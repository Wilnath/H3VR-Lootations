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
        private static List<LootObject> LootTriggers = new();
        private static List<LootSpawnPoint> LootSpawns = new();
        private static List<LootObjectRandomizer> ObjectSpawns = new List<LootObjectRandomizer>();
        private static int spawnPointsActivated = 0;

        public static readonly Vector2 MAG_AMOUNT_RANGE = new Vector2(2, 4);

        public static readonly float Y_SPAWN_INCREMENT = 0.05f;

        static LootManager()
        {
            SR_Manager.SupplyPointChangeEvent += OnSupplyPointChange;
        }

        public static bool AddLootable(LootSpawnPoint lootable)
        {
            LootSpawns.Add(lootable);
            return true;
        }

        public static void RemoveLootable(LootSpawnPoint lootable)
        {
            if (LootSpawns.Contains(lootable))
            {
                LootSpawns.Remove(lootable);
            }
        }

        public static bool AddTrigger(LootObject trigger)
        {
            LootTriggers.Add(trigger);
            return true;
        }

        public static bool RemoveTrigger(LootObject trigger)
        {
            if (!LootTriggers.Contains(trigger))
            {
                return false;
            }
            LootTriggers.Add(trigger);
            return true;
        }

        public static bool AddRandomObject(LootObjectRandomizer obj)
        {
            ObjectSpawns.Add(obj);
            return true;
        }

        public static void OnPhysicalObjectPickup(GameObject obj)
        {

            if (spawnedLoot.ContainsKey(obj))
            {
                Lootations.Logger.LogDebug("Removed object from tracked spawned loot pool.");
                spawnedLoot[obj].StopTrackingObject(obj);
                spawnedLoot.Remove(obj);
                /*if (Networking.IsClient())
                {
                    Networking.SendItemGrab(obj);
                }*/
            }
        }

        public static void OnSceneSwitched()
        {
            Lootations.Logger.LogInfo("Removing track of spawned items.");
            spawnedLoot.Clear();
            LootSpawns.Clear();
            LootTriggers.Clear();
            ObjectSpawns.Clear();
        }

        /*public static void StopTrackingNetworkId(int trackingId)
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
        }*/

        private static void ShuffleSpawns()
        {
            LootSpawns = LootSpawns.OrderBy(_ => Random.Range(0f, 1f)).ToList();
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
            for (int i = 0; i < LootSpawns.Count; i++)
            {
                LootSpawnPoint point = LootSpawns[i];
                point.Reset();
            }

            // Reroll loot object.
            for (int i = 0; i < ObjectSpawns.Count; i++)
            {
                LootObjectRandomizer randomObject = ObjectSpawns[i];
                randomObject.RollAndSpawn();
            }

            for (int i = 0; i < LootTriggers.Count; i++)
            {
                LootObject trigger = LootTriggers[i];
                trigger.Reset();
            }

            // Set loot spawns to correct table.
            for (int i = 0; i < LootSpawns.Count; i++)
            {
                LootSpawnPoint lootable = LootSpawns[i];
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
