using UnityEngine;
using FistVR;
using System.Collections;
using Sodalite.Api;
using System.Collections.Generic;

namespace Lootations
{
    public class LootSpawnPoint : MonoBehaviour
    {
        public string TableName = "NONE";
        public float SpawnRadius = 0.25f;
        public bool RandomizeYRotation = true;

        [Header("Instance Settings")]
        public bool UseInstanceSettings = false;
        public float CullDistance = 250f;
        public bool RaycastDownOnSpawn = true;
        public bool LockPhysicsOnSpawn = false;
        public List<GameObject> SpawnedLoot = new List<GameObject>();

        public delegate void LootRolledDelegate(List<string> newLoot);
        public event LootRolledDelegate LootRolled;

        private bool HasSpawnedLoot = false;
        private bool Culled = false;
        private float cullDistance = 50f;

        private readonly static float CULL_MIN_TIMER = 0.5f;
        private readonly static float CULL_RANDOM_DELAY_MAX = 2.5f;
        private Coroutine CullUpdateRoutine;

        private void Awake()
        {
            bool active = LootManager.AddLootSpawn(this);
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
                            foreach (GameObject go in SpawnedLoot) 
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
                            foreach (GameObject go in SpawnedLoot) 
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
            if (SpawnedLoot.Contains(obj))
            {
                SpawnedLoot.Remove(obj);
            }
        }

        public void Reset()
        {
            if (Lootations.h3mpEnabled && Networking.IsClient())
            {
                return;
            }

            foreach(GameObject go in SpawnedLoot)
            {
                Destroy(go);
            }

            SpawnedLoot = new List<GameObject>();
            HasSpawnedLoot = false;
            Culled = false;
        }

        // Spawns contained item.
        public virtual void Trigger()
        {
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
            LootManager.RemoveLootSpawn(this);
            if (CullUpdateRoutine != null)
            {
                StopCoroutine(CullUpdateRoutine);
            }
        }

        public GameObject ConstructAmmoBox(FVRFireArmRound baseRound, MetaTags tags)
        {
            Lootations.Logger.LogDebug("Constructing ammo box.");

            GameObject ammoBoxObj = AM.GetAmmoBox(baseRound.RoundType);
            ammoBoxObj = Instantiate(ammoBoxObj, transform.position, transform.rotation);
            CartridgeBox cartridgeBox = ammoBoxObj.GetComponent<CartridgeBox>();
            cartridgeBox.ConfigureShapeForRoundType(baseRound.RoundType, baseRound.RoundClass);

            return ammoBoxObj;
        }

        public IEnumerator SpawnLoot()
        {
            if (Lootations.h3mpEnabled && Networking.IsClient())
            {
                yield break;
            }

            LootTable table = TableManager.GetTable(TableName);
            if (table == null)
            {
                Lootations.Logger.LogError("Spawn point tried getting non-existant table " + TableName);
                yield break;
            }

            MetaTags tags = new MetaTags();
            List<string> objectIds = table.RollObjectId(ref tags);
            if (objectIds.Count == 0)
            {
                Lootations.Logger.LogDebug("Skipped spawn due to rolling nothing.");
                yield break;
            }
            LootRolled?.Invoke(objectIds);

            for (int i = 0; i < objectIds.Count; i++)
            {
                string objectId = objectIds[i];
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

                        // TODO: Add mag + weapon spawn specific locations like guncases
                        GameObject spawnedMagObj = SpawnItem(magCallback.Result, true);
                        yield return null;
                    }
                }

                var callback = obj.GetGameObjectAsync();
                yield return callback;

                var roundComponent = callback.Result.GetComponent<FVRFireArmRound>();
                GameObject spawnedObj;
                if (tags.CartridgesAsAmmoBoxes && roundComponent != null)
                {
                    spawnedObj = SpawnItem(ConstructAmmoBox(roundComponent, tags), false, true);
                }
                else
                {
                    spawnedObj = SpawnItem(callback.Result);
                }


                HasSpawnedLoot = true;
                tags.MagLoadRange.Apply(spawnedObj);
                yield return null;
            }
        }

        private static readonly float RAYCAST_DISTANCE = 2f;
        private GameObject SpawnItem(GameObject obj, bool randomOffset = false, bool alreadySpawnedObj = false)
        {
            // Leaving out most of the function commented, incomplete feature

            Bounds bounds = new Bounds(obj.transform.GetChild(0).transform.position, Vector3.zero);
            LayerMask mask = LayerMask.NameToLayer("Default");

            GameObject spawnedObj = null;
            if (!alreadySpawnedObj)
            {
                spawnedObj = Instantiate(obj, transform.position, Quaternion.identity);
            } 
            else
            {
                spawnedObj = obj;
            }
            
            obj.transform.rotation = Quaternion.identity;

            foreach (var rend in obj.GetComponentsInChildren<Renderer>())
            {
                if (rend.gameObject.layer == mask && rend.enabled)
                {
                    bounds.Encapsulate(rend.bounds);
                }
            }
            /*foreach (var coll in obj.GetComponentsInChildren<Collider>())
            {
                if (coll.gameObject.layer == mask)
                {
                    Lootations.Logger.LogDebug("Accounting for collider!");
                    bounds.Encapsulate(coll.bounds);
                }
            }*/

            // One day this decided to work. Not even sure what I changed, but I am afraid to touch anything here now
            Quaternion spawnRotation = transform.rotation;
            float yOffset = 0f;
            Vector3 size = bounds.size;
            float[] planeSizes = { size.x * size.y, size.x * size.z, size.y * size.z };
            if (planeSizes[0] >= planeSizes[1] && planeSizes[0] >= planeSizes[2])
            {
                spawnRotation = Quaternion.Euler(90, 0, 0);// * Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                yOffset = size.z;
            }
            else if (planeSizes[1] >= planeSizes[0] && planeSizes[1] >= planeSizes[2])
            {
                spawnRotation = Quaternion.Euler(0, 0, 0);// Random.Range(0f, 360f), 0);
                yOffset = size.y;
            }
            else if (planeSizes[2] >= planeSizes[0] && planeSizes[2] >= planeSizes[1])
            {
                spawnRotation = Quaternion.Euler(0, 0, 90);// * Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                yOffset = size.x;
            }

            Vector3 spawnOffset = Vector3.zero;
            if (randomOffset)
            {
                spawnOffset = new Vector3(Random.Range(-SpawnRadius, SpawnRadius), 0, Random.Range(-SpawnRadius, SpawnRadius));
            }

            Vector3 spawnPos = transform.position + spawnOffset;
            if (RaycastDownOnSpawn)
            {
                bool isRaycastValid = Physics.Raycast(spawnPos, transform.TransformDirection(Vector3.down), out RaycastHit hit, RAYCAST_DISTANCE, Physics.AllLayers);
                if (isRaycastValid)
                {
                    spawnPos = hit.point + new Vector3(0, yOffset, 0);
                }
            }

            spawnedObj.transform.position = spawnPos;
            spawnedObj.transform.rotation = spawnRotation;

            if (RandomizeYRotation)
            {
                spawnedObj.transform.Rotate(Vector3.up, Random.Range(0f, 360f), Space.World);
            }


            // FIXME: do this for children too
            if (LockPhysicsOnSpawn)
            {
                spawnedObj.transform.SetParent(transform);
                foreach (FVRPhysicalObject physObj in spawnedObj.GetComponentsInChildren<FVRPhysicalObject>())
                {
                    physObj.RootRigidbody.constraints = RigidbodyConstraints.FreezeAll;
                }
            }

            LootManager.spawnedLoot.Add(spawnedObj, this);
            SpawnedLoot.Add(spawnedObj);
            spawnedObj.SetActive(true);

            return spawnedObj;
        }
    }
}
