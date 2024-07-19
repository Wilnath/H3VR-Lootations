using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using FistVR;
using System.Data;
using UnityEngine.AI;
using H3MP.Networking;

namespace Lootations
{
    internal static class Utilities
    {
        public static LootSpawnPoint[] GameObjectsToPoints(GameObject[] gameObjects)
        {
            if (gameObjects.Length == 0)
                return [];

            LootSpawnPoint[] lootSpawnPoints = new LootSpawnPoint[gameObjects.Length];
            for (int i = 0; i < gameObjects.Length; i++)
            {
                var item = gameObjects[i];
                var point = item.GetComponent<LootSpawnPoint>();
                if (point == null)
                {
                    Lootations.Logger.LogError("Object has loot spawn point set without its component");
                    continue;
                }
                lootSpawnPoints[i] = point;
            }

            return lootSpawnPoints;
        }

        // 30f is from packers code, hopefully good enough
        private static readonly float NAVMESH_QUERY_RANGE = 30.0f;
        public static bool FindPointOnNavmesh(Vector3 centre, out Vector3 result)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(centre, out hit, NAVMESH_QUERY_RANGE, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
            result = Vector3.zero;
            return false;
        }

        public static bool RandomPointOnNavmesh(Vector3 centre, float range, out Vector3 result)
        {
            // Copied from https://docs.unity3d.com/ScriptReference/AI.NavMesh.SamplePosition.html

            for (int i = 0; i < 30; i++)
            {
                Vector3 randomPoint = centre + Random.insideUnitSphere * range;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomPoint, out hit, 0.5f, NavMesh.AllAreas))
                {
                    result = hit.position;
                    return true;
                }
            }
            result = Vector3.zero;
            return false;
        }

        public static string GetRandomInStringArray(string[] arr) 
        {
            if (arr.Length == 0)
                return "";

            return arr[UnityEngine.Random.Range(0, arr.Length + 1)];
        }

        public static Vector3 GetPlayerPosition()
        {
            return GM.CurrentPlayerBody.Head.transform.position;
        }
        
        public static bool PositionsWithinDistance(Vector3 a, Vector3 b, float distance)
        {
            // https://docs.unity3d.com/ScriptReference/Vector3-sqrMagnitude.html
            Vector3 offset = b - a;
            return offset.sqrMagnitude < distance * distance;
        }

        /// <summary>
        /// Returns the Gamemanager player at index i, does not include the local player.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static PlayerData GetPlayer(int i)
        {
            //Do error Checks
            return PlayerData.GetPlayer(i);
        }

        public static bool PlayerWithinDistance(Vector3 a, float distance)
        {
            if (Lootations.h3mpEnabled && Networking.IsHost())
            {
                int[] ids = Networking.GetPlayerIds();
                foreach (var id in ids)
                {
                    if (PositionsWithinDistance(Networking.GetPlayerPosition(id), a, distance))
                    {
                        return true;
                    }
                }
            }
            return PositionsWithinDistance(GetPlayerPosition(), a, distance);
        }
    }
}
