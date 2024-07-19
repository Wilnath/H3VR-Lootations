using FistVR;
using SupplyRaid;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using H3MP.Tracking;
using Sodalite.Api;
using MapGenerator;

namespace Lootations
{
    public static class LootManager
    {
        private static readonly int MAX_SPAWNS = -1;

        private static int LootTriggerCounter = 0;

        public static Dictionary<GameObject, LootSpawnPoint> spawnedLoot = new();
        private static List<LootObject> LootObjects = new();
        private static List<LootSpawnPoint> LootSpawns = new();
        private static List<LootObjectRandomizer> ObjectSpawns = new List<LootObjectRandomizer>();
        private static Dictionary<int, ILootTrigger> LootTriggers = new();
        // horrible, actually disgusting, god please i want a different way to do this but i can't store ints in loottriggers
        // without screwing up the codebase
        private static Dictionary<ILootTrigger, int> LootTriggerIds = new();
        private static int spawnPointsActivated = 0;

        public static readonly Vector2 MAG_AMOUNT_RANGE = new Vector2(2, 4);

        public static readonly float Y_SPAWN_INCREMENT = 0.05f;

        static LootManager()
        {
            SR_Manager.SupplyPointChangeEvent += OnSupplyPointChange;
        }

        public static bool AddLootSpawn(LootSpawnPoint lootable)
        {
            LootSpawns.Add(lootable);
            return true;
        }

        public static void RemoveLootSpawn(LootSpawnPoint lootable)
        {
            if (LootSpawns.Contains(lootable))
            {
                LootSpawns.Remove(lootable);
            }
        }

        public static bool AddLootObject(LootObject obj)
        {
            LootObjects.Add(obj);
            return true;
        }

        public static ILootTrigger GetLootTriggerById(int id)
        {
            return LootTriggers[id];
        }

        public static int GetIdByLootTrigger(ILootTrigger trigger)
        {
            return LootTriggerIds[trigger];
        }

        public static bool AddLootTrigger(ILootTrigger trigger)
        {
            LootTriggers.Add(LootTriggerCounter, trigger);
            LootTriggerIds.Add(trigger, LootTriggerCounter);
            LootTriggerCounter++;
            return true;
        }

        public static bool RemoveLootTrigger(ILootTrigger trigger)
        {
            int id = LootTriggerIds[trigger];
            LootTriggerIds.Remove(trigger);
            LootTriggers.Remove(id);
            return true;
        }

        public static bool RemoveLootObject(LootObject trigger)
        {
            if (!LootObjects.Contains(trigger))
            {
                return false;
            }
            LootObjects.Add(trigger);
            return true;
        }

        public static bool AddRandomObject(LootObjectRandomizer obj)
        {
            ObjectSpawns.Add(obj);
            return true;
        }

        public static void OnPhysicalObjectPickup(GameObject obj)
        {
            if (Lootations.h3mpEnabled && Networking.IsClient())
            {
                Networking.SendItemGrab(obj);
            }
            if (spawnedLoot.ContainsKey(obj))
            {
                Lootations.Logger.LogDebug("Removed object from tracked spawned loot pool.");
                spawnedLoot[obj].StopTrackingObject(obj);
                spawnedLoot.Remove(obj);
            }
        }

        public static void OnSceneSwitched()
        {
            Lootations.Logger.LogInfo("Removing track of spawned items.");
            spawnedLoot.Clear();
            LootSpawns.Clear();
            LootObjects.Clear();
            ObjectSpawns.Clear();
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
            LootSpawns = LootSpawns.OrderBy(_ => Random.Range(0f, 1f)).ToList();
        }

        private static void OnSupplyPointChange()
        {
            if (Lootations.h3mpEnabled && Networking.IsConnected())
            {
                if (Networking.IsClient())
                {
                    // Await seed from host
                    Lootations.Logger.LogDebug("Awaiting reroll of loot as client.");
                    return;
                }
                else
                {
                    Lootations.Logger.LogDebug("Rolling loot as host.");
                    int seed = Time.frameCount;
                    RerollLoot(seed);
                    Networking.SendRerollLoot(seed);
                }
            }
            else
            {
                Lootations.Logger.LogDebug("Rolling loot as offline.");
                RerollLoot();
            }
        }

        public static void OnTNHLevelSet(int level)
        {
            // just needed something semi-random
            RerollLoot(Time.frameCount);
        }

        public static void RerollLoot(int seed = -1)
        {
            // Avoid unneccessarily respawning items
            /*if (captures == 0)
            {
                Lootations.Logger.LogInfo("Skipping loot respawn due to captures = 0");
                return;
            }*/
            if (seed == -1)
            {
                seed = Time.frameCount;
            }
            int level = 0;
            if (SR_Manager.instance.isActiveAndEnabled)
            {
                level = SR_Manager.instance.CurrentCaptures;
            }
            Random.InitState(seed);

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
                // TODO: MG dep
                if ((MAX_SPAWNS != -1 && i >= MAX_SPAWNS)
                    || (MG_Manager.instance.isActiveAndEnabled && MG_Manager.profile.srLootSpawns >= i))
                {
                    break;
                }
                LootObjectRandomizer randomObject = ObjectSpawns[i];
                randomObject.RollAndSpawn();
                spawnPointsActivated++;
            }

            for (int i = 0; i < LootObjects.Count; i++)
            {
                LootObject trigger = LootObjects[i];
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
                if (spawnPointsActivated < MAX_SPAWNS)
                {
                    Lootations.Logger.LogWarning("Spawned less items than max spawns allows for. Maybe up the interior props?");
                }
                else
                {
                    Lootations.Logger.LogInfo("Spawned as many random loot objects as max spawns allow for.");
                }
            }
        }

        public static Quaternion RandomRotation()
        {
            return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
    }
}
