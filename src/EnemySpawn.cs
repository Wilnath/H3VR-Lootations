using SupplyRaid;
using System.Collections.Generic;
using System.Linq;
using Sodalite.Api;
using FistVR;
using System.Text;
using UnityEngine;
using static FistVR.TNH_PatrolChallenge;

namespace Lootations
{
    public class EnemySpawn : MonoBehaviour
    {
        public bool UseGlobalSpawnSettings = true;

        // TODO: Config
        public float PlayerDistanceToDespawn = 125f;
        public float PlayerDistanceToSpawn = 100f;
        public float PlayerSpawnGrace = 25f;

        private List<Sosig> spawnedSosigs = new();
        private bool spawned = false;

        public void Awake()
        {
            if (Utilities.PlayerWithinDistance(transform.position, PlayerSpawnGrace))
            {
                Lootations.Logger.LogDebug("Skipped enemy spawn to grace");
                spawned = true;
            }
        }

        public void Update()
        {
            if (spawned)
            {
                return;
            }

            if (Utilities.PlayerWithinDistance(transform.position, PlayerDistanceToSpawn))
            {
                Spawn();
                Lootations.Logger.LogDebug("Attempting to spawn enemies");
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

            // Construct the spawn options struct
            SosigAPI.SpawnOptions options = new()
            {
                SpawnActivated = true,
                SpawnState = Sosig.SosigOrder.GuardPoint,
                IFF = parameters.IFF,
                SpawnWithFullAmmo = true,
                EquipmentMode = SosigAPI.SpawnOptions.EquipmentSlots.All,
                SosigTargetPosition = transform.position,
                SosigTargetRotation = transform.eulerAngles
            };

            for(int i = 0; i < parameters.squadSize; i++)
            {
                SosigEnemyID id = SR_Global.GetRandomSosigIDFromPool(parameters.pool);
                Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                Vector3 offset = new Vector3(Random.Range(-1.0f, 1.0f), 0, Random.Range(-1.0f, 1.0f));
                Sosig sosig = SosigAPI.Spawn(IM.Instance.odicSosigObjsByID[id], options, transform.position + offset, rotation);
                spawnedSosigs.Add(sosig);

                if (SR_Manager.instance.isActiveAndEnabled)
                {
                    // Could technically make this update, but should play out around the same
                    foreach (var item in spawnedSosigs)
                    {
                        item.StateSightRangeMults *= SR_Manager.sosigSightMultiplier;
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
