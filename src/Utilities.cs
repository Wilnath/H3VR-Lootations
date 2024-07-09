using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using FistVR;
using System.Data;

namespace Lootations
{
    internal static class Utilities
    {
        public static LootSpawnPoint[] gameObjectsToPoints(GameObject[] gameObjects)
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
        
        public static bool PositonsWithinDistance(Vector3 a, Vector3 b, float distance)
        {
            // https://docs.unity3d.com/ScriptReference/Vector3-sqrMagnitude.html
            Vector3 offset = b - a;
            return offset.sqrMagnitude < distance * distance;
        }

        public static bool PlayerWithinDistance(Vector3 a, float distance)
        {
            return PositonsWithinDistance(GetPlayerPosition(), a, distance);
        }
    }
}
