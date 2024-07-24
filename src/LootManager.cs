using FistVR;
using SupplyRaid;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using H3MP.Tracking;
using Sodalite.Api;
using MapGenerator;
using H3MP.Networking;

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
            if (!LootTriggers.ContainsKey(id))
            {
                return null;
            }
            return LootTriggers[id];
        }

        public static int GetLootTriggerId(ILootTrigger trigger)
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

        public static bool RemoveLootObject(LootObject obj)
        {
            if (!LootObjects.Contains(obj))
            {
                return false;
            }
            LootObjects.Remove(obj);
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
            LootTriggers.Clear();
            LootTriggerIds.Clear();
            LootTriggerCounter = 0;
        }

        public static void StopTrackingNetworkId(int trackingId)
        {
            Lootations.Logger.LogDebug("Attempting to remove " + trackingId.ToString());
            if (Server.objects == null)
            {
                Lootations.Logger.LogError("that stuff is null even");
                return;
            }
            if (trackingId < 0 && trackingId >= Server.objects.Length)
            {
                Lootations.Logger.LogError("Clients.objects does not work like that it turns out");
                return;
            }
            TrackedObjectData obj = Server.objects[trackingId];
            if (obj == null)
            {
                Lootations.Logger.LogError("stop tracking null check obj");
                return;
            }
            TrackedObject physical = obj.physical;
            if (physical == null)
            {
                Lootations.Logger.LogError("stop tracking null check physical");
                return;
            }
            GameObject gameObj = physical.gameObject;
            if (gameObj == null)
            {
                Lootations.Logger.LogError("stop tracking null check gameObj");
                return;
            }
            if (spawnedLoot.ContainsKey(gameObj))
            {
                Lootations.Logger.LogDebug("REMOVED IT!");
                LootSpawnPoint point = spawnedLoot[gameObj];
                point.StopTrackingObject(gameObj);
            }
            return;
        }

        private static void ShuffleObjectRandomizers()
        {
            ObjectSpawns = ObjectSpawns.OrderBy(_ => Random.Range(0, int.MaxValue)).ToList();
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
            Lootations.Logger.LogDebug("Respawning objects with seed: " + seed.ToString());
            Random.InitState(seed);

            spawnPointsActivated = 0;
            ShuffleObjectRandomizers();
            spawnedLoot = new();

            for (int i = 0; i < LootSpawns.Count; i++)
            {
                LootSpawnPoint point = LootSpawns[i];
                point.Reset();
            }

            // Reroll loot object.
            bool hitLimit = false;
            for (int i = 0; i < ObjectSpawns.Count; i++)
            {
                if (MAX_SPAWNS != -1 && i >= MAX_SPAWNS)
                {
                    Lootations.Logger.LogDebug("Stopping respawn of loot objects, hit config limit of " + MAX_SPAWNS);
                    hitLimit = true;
                    break;
                }
                // TODO: MG dep
                if (MG_Manager.instance.isActiveAndEnabled && MG_Manager.profile.srLootSpawns != 0 && i >= MG_Manager.profile.srLootSpawns)
                {
                    Lootations.Logger.LogDebug("Stopping respawn of loot objects, hit MG profile limit of " + MG_Manager.profile.srLootSpawns);
                    hitLimit = true;
                    break;
                }
                LootObjectRandomizer randomObject = ObjectSpawns[i];
                randomObject.RollAndSpawn();
                spawnPointsActivated++;
            }

            if (!hitLimit)
            {
                Lootations.Logger.LogDebug("No limit hit for ObjectRandomizer rerolls.");
            }
            Lootations.Logger.LogDebug("Spawned " + spawnPointsActivated.ToString() + " objects.");

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
        }

        public static Quaternion RandomRotation()
        {
            return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
    }
}
