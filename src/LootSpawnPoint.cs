using UnityEngine;
using FistVR;
using System.Collections;
using H3MP.Networking;
using Sodalite;
using Sodalite.Api;
using SupplyRaid;
using System.Collections.Generic;

namespace Lootations
{
    public class LootSpawnPoint : MonoBehaviour
    {
        public string[] ObjectIds { get; set; } = [];
        public string TableName { get; set; } = "NONE";
        public string[] ProgressiveLootTables = [
            "Level 0",
            "Level 1",
            "Level 2",
            "Level 3",
            "Level 4",
        ];
        public float SpawnRadius = 0.25f;

        [Header("Instance Settings")]
        public bool UseInstanceSettings = false;
        public float CullDistance = 250f;

        public delegate void LootRolledDelegate(string[] newLoot);
        public event LootRolledDelegate LootRolled;

        private bool HasSpawnedLoot = false;
        private bool Culled = false;
        private float cullDistance = 50f;
        private List<GameObject> spawnedLoot = new List<GameObject>();

        private readonly static float CULL_MIN_TIMER = 0.5f;
        private readonly static float CULL_RANDOM_DELAY_MAX = 2.5f;
        private Coroutine CullUpdateRoutine;

        private void Awake()
        {
            bool active = LootManager.AddLootable(this);
            gameObject.SetActive(active);
            if (Lootations.CullingEnabled.Value)
            {
                CullUpdateRoutine = StartCoroutine(CullUpdate());
                if (UseInstanceSettings)
                {
                    cullDistance = CullDistance;
                }
                else
                {
                    cullDistance = Lootations.ItemCullingDistance.Value;
                }
            }

            if (SR_Manager.instance != null)
            {
                SetLevel(SR_Manager.instance.CurrentCaptures);
            } 
            else
            {
                SetLevel(0);
            }

        }
        
        IEnumerator CullUpdate()
        {
            while (true)
            {
                if (HasSpawnedLoot)
                {
                    if (Culled)
                    {
                        if (Utilities.PlayerWithinDistance(transform.position, cullDistance))
                        {
                            foreach (GameObject go in spawnedLoot) 
                            {
                                go.SetActive(true);
                            }
                            Culled = false;
                        }
                    }
                    else
                    {
                        if (!Utilities.PlayerWithinDistance(transform.position, cullDistance))
                        {
                            foreach (GameObject go in spawnedLoot) 
                            {
                                go.SetActive(false);
                            }
                            Culled = true;
                        }
                    }
                }
                yield return new WaitForSeconds(CULL_MIN_TIMER + Random.Range(0.0f, CULL_RANDOM_DELAY_MAX));
            }
        }

        public void StopTrackingObject(GameObject obj)
        {
            if (spawnedLoot.Contains(obj))
            {
                spawnedLoot.Remove(obj);
            }
        }

        public void SetLevel(int level)
        {
            if (ProgressiveLootTables.Length == 0)
            {
                Lootations.Logger.LogWarning("Spawn point has no loot tables at all.");
                return;
            }
            level = Mathf.Clamp(level, 0, ProgressiveLootTables.Length - 1);
            TableName = ProgressiveLootTables[level];
        }

        public void Reset()
        {
            foreach(GameObject go in spawnedLoot)
            {
                Destroy(go);
            }
            spawnedLoot = new List<GameObject>();
            HasSpawnedLoot = false;
            Culled = false;
        }

        // Spawns contained item.
        public virtual void Trigger()
        {
            ObjectIds = TableManager.GetTable(TableName).RollObjectId();
            LootRolled?.Invoke(ObjectIds);
            StartCoroutine(SpawnLoot());
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.0f, 0.6f, 0.0f, 0.5f);
            //Gizmos.DrawSphere(transform.position, 0.1f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * SpawnRadius);
            Gizmos.color = new Color(0.6f, 0.0f, 0.0f, 0.5f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * -1 * SpawnRadius);
            Gizmos.DrawLine(transform.position, transform.position + transform.right * SpawnRadius);
            Gizmos.DrawLine(transform.position, transform.position + transform.right * -1 * SpawnRadius);
        }

        void OnDestroy()
        {
            LootManager.RemoveLootable(this);
            StopCoroutine(CullUpdateRoutine);
        }

        public IEnumerator SpawnLoot()
        {

            if (Networking.isClient())
            {
                yield break;
            }

            if (ObjectIds.Length == 0)
            {
                Lootations.Logger.LogWarning("Skipped spawn due to empty lootable.");
                yield break;
            }

            for (int i = 0; i < ObjectIds.Length; i++)
            {
                string objectId = ObjectIds[i];
                // Get the object and wait for it to load
                if (!IM.OD.TryGetValue(objectId, out FVRObject obj))
                {
                    Lootations.Logger.LogError($"No object found with id '{objectId}'.");
                    yield break;
                }

                Transform transform = gameObject.transform;

                // If the object is a firearm, spawn some mags or shots
                if (obj != null && obj.Category == FVRObject.ObjectCategory.Firearm)
                {
                    FVRObject mag;
                    if (obj.HasMagazine())
                    {
                        mag = FirearmAPI.GetSmallestMagazine(obj);
                    }
                    else
                    {
                        mag = obj.CompatibleSingleRounds[0];
                    }

                    int spawnAmount = Random.Range((int)LootManager.MAG_AMOUNT_RANGE.x, (int)LootManager.MAG_AMOUNT_RANGE.y);
                    for (int j = 0; j < spawnAmount; j++)
                    {
                        var magCallback = mag.GetGameObjectAsync();
                        yield return magCallback;

                        Vector3 randomMagOffset = new Vector3(
                            Random.Range(-SpawnRadius, SpawnRadius),
                            j * LootManager.Y_SPAWN_INCREMENT,
                            Random.Range(-SpawnRadius, SpawnRadius)
                        );
                        // TODO: Add mag + weapon spawn specific locations like guncases
                        GameObject spawnedMagObj = Instantiate(magCallback.Result, transform.position + randomMagOffset, transform.rotation * LootManager.RandomRotation());
                        LootManager.spawnedLoot.Add(spawnedMagObj, this);
                        spawnedLoot.Add(spawnedMagObj);
                        spawnedMagObj.SetActive(true);
                        yield return null;
                    }
                }

                var callback = obj.GetGameObjectAsync();
                yield return callback;

                if (callback == null)
                {
                    Lootations.Logger.LogError("Failed getting the object or... something.");
                    yield break;
                }

                HasSpawnedLoot = true;

                // TODO: Randomize spawn location if several items
                // Instantiate it at our position and rotation
                GameObject spawnedObj = Instantiate(callback.Result, transform.position, transform.rotation * LootManager.RandomRotation());
                LootManager.spawnedLoot.Add(spawnedObj, this);
                spawnedObj.SetActive(true);
                spawnedLoot.Add(spawnedObj);
                Rigidbody objRb = spawnedObj.GetComponent<Rigidbody>();
                objRb.isKinematic = false;
                yield return null;
            }
        }
    }
}
