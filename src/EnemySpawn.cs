using SupplyRaid;
using System.Collections.Generic;
using System.Linq;
using Sodalite.Api;
using FistVR;
using System.Text;
using UnityEngine;
using static FistVR.TNH_PatrolChallenge;
using UnityEngine.AI;

namespace Lootations
{
    public class EnemySpawn : MonoBehaviour
    {
        public bool LimitSpawns = false;
        public int SpawnAmountLimit = 1;

        public bool UseGlobalSpawnSettings = true;
        public Vector3 SpawnRandomization = new Vector3(0, 0, 0);

        // TODO: Config
        public float PlayerDistanceToDespawn = 350f;
        public float PlayerDistanceToSpawn = 250f;
        public float PlayerSpawnGrace = 75f;

        private List<Sosig> spawnedSosigs = new();
        private bool spawned = false;

        public void Awake()
        {
            if (Utilities.PlayerWithinDistance(transform.position, PlayerSpawnGrace))
            {
                Lootations.Logger.LogDebug("Skipped enemy spawn to grace: player");
                spawned = true;
                return;
            }

            if (SR_Manager.instance.isActiveAndEnabled) 
            {
                Vector3 stationPos = SR_Manager.instance.supplyPoints[SR_Manager.instance.playerSupplyID].respawn.position;
                if (Utilities.PositionsWithinDistance(stationPos, transform.position, PlayerSpawnGrace))
                {
                    Lootations.Logger.LogDebug("Skipped enemy spawn to grace: station");
                    spawned = true;
                    return;
                }
            }
        }

        public void Update()
        {
            if (spawned || (Lootations.h3mpEnabled && Networking.IsClient()))
            {
                return;
            }

            if (Utilities.PlayerWithinDistance(transform.position, PlayerDistanceToSpawn))
            {
                Spawn();
                spawned = true;
            }
        }

        //IFF, SquadSize, id
        private class SpawnParameters 
        {
            public int IFF = 0;
            public SosigEnemyID[] pool = { SosigEnemyID.M_Popsicles_Scout };
            public int squadSize = 3;
        }

        private SpawnParameters GetSpawnParameters()
        {
            SpawnParameters parameters = new SpawnParameters();
            // TODO: IFF?
            if (SR_Manager.instance.isActiveAndEnabled)
            {
                FactionLevel currentLevel = SR_Manager.GetFactionLevel();
                parameters.IFF = 1;
                parameters.squadSize = Random.Range(currentLevel.squadSizeMin, currentLevel.squadSizeMax);
                parameters.pool = currentLevel.squadPool.sosigEnemyID;
                return parameters;
            }
            if (GM.TNH_Manager.isActiveAndEnabled)
            {
                List<Patrol> patrols = GM.TNH_Manager.m_curLevel.PatrolChallenge.Patrols;
                Patrol p = patrols[Random.Range(0, patrols.Count)];
                parameters.IFF = 1;
                parameters.squadSize = p.PatrolSize;
                parameters.pool = [p.EType, p.LType];
                return parameters;
            }
            return parameters;
        }

        /// <summary>Spawns the Sosig with the configured options.</summary>
        public void Spawn()
        {
            SpawnParameters parameters = GetSpawnParameters();

            Vector3 spawnOffset = new Vector3(
                Random.Range(0f, SpawnRandomization.x),
                Random.Range(0f, SpawnRandomization.y),
                Random.Range(0f, SpawnRandomization.z)
            );
            Vector3 spawnPosition = transform.position + spawnOffset;

            // Construct the spawn options struct
            SosigAPI.SpawnOptions options = new()
            {
                SpawnActivated = true,
                SpawnState = Sosig.SosigOrder.GuardPoint,
                IFF = parameters.IFF,
                SpawnWithFullAmmo = true,
                EquipmentMode = SosigAPI.SpawnOptions.EquipmentSlots.All,
                SosigTargetPosition = spawnPosition,
                SosigTargetRotation = transform.eulerAngles
            };

            for(int i = 0; i < parameters.squadSize; i++)
            {
                if (LimitSpawns && i >= SpawnAmountLimit)
                {
                    Lootations.Logger.LogDebug("Hit enemy spawn limit");
                    break;
                }
                SosigEnemyID id = SR_Global.GetRandomSosigIDFromPool(parameters.pool);
                Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                if (!Utilities.SampleNavMesh(transform.position, 0.5f, out Vector3 spawnPos))
                {
                    Lootations.Logger.LogDebug("Could not find spawn position for EnemySpawn sosig");
                    continue;
                }
                Sosig sosig = SosigAPI.Spawn(IM.Instance.odicSosigObjsByID[id], options, spawnPos, rotation);
                spawnedSosigs.Add(sosig);

                if (SR_Manager.instance.isActiveAndEnabled)
                {
                    // Could technically make this update, but should play out around the same
                    foreach (var item in spawnedSosigs)
                    {
                        item.StateSightRangeMults *= SR_Manager.sosigSightMultiplier;
                    }

                    if (!SR_Manager.profile.sosigWeapons)
                    {
                        DisableSosigWeapons(sosig);
                    }

                    SR_Manager.instance.AddCustomSosig(sosig);
                }
                else if (GM.TNH_Manager.isActiveAndEnabled)
                {
                    GM.TNH_Manager.AddToMiscEnemies(sosig.gameObject);
                }
                // if sandbox, no need to track it at all.
            }
        }

        private void DisableSosigWeapons(Sosig sosig)
        {
            foreach (SosigInventory.Slot slot in sosig.Inventory.Slots)
            {
                if (slot.HeldObject != null)
                {
                    FVRPhysicalObject component = slot.HeldObject.GetComponent<FVRPhysicalObject>();
                    if (component != null)
                    {
                        component.IsPickUpLocked = true;
                    }
                }
            }

            foreach (SosigHand hand in sosig.Hands)
            {
                if (hand.HeldObject != null)
                {
                    FVRPhysicalObject component2 = hand.HeldObject.GetComponent<FVRPhysicalObject>();
                    if (component2 != null)
                    {
                        component2.IsPickUpLocked = true;
                    }
                }
            }
        }

        public void OnDestroy()
        {
            foreach (Sosig sosig in spawnedSosigs)
            {
                sosig?.KillSosig();
            }

            spawnedSosigs.Clear();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            if (UseGlobalSpawnSettings)
            {
                Gizmos.DrawSphere(transform.position, 0.25f);
            }
            else
            {
                Gizmos.DrawSphere(transform.position, PlayerDistanceToSpawn);
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.1f);
        }
    }
}
