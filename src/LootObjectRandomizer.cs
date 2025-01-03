﻿using FistVR;
using H3MP.Networking;
using SupplyRaid;
using UnityEngine;

namespace Lootations
{
    public class LootObjectRandomizer : MonoBehaviour
    {
        public GameObject[] Objects = new GameObject[0];
        public bool RandomizeRotation = false;
        public Vector3 VisualizationSize = new Vector3(1, 1, 1);

        private GameObject spawnedObject;

        public readonly float PLAYER_PROXIMITY_GRACE_DISTANCE = 35f;
        public bool ResetGrace = true;

        public void Awake()
        {
            // Let the manager decide when this awakens.
            LootManager.AddRandomObject(this);
            if (Objects.Length == 0)
            {
                Lootations.Logger.LogError("LootObjectRandomizer has Objects of length 0.");
            }
        }

        public void Destroy()
        {
            if (spawnedObject != null)
                Destroy(spawnedObject);

            spawnedObject = null;
        }

        public void RollAndSpawn()
        {
            if (Objects.Length == 0)
            {
                Lootations.Logger.LogWarning("ObjectRandomizer told to spawn with objects of length 0.");
                return;
            }

            if (spawnedObject != null)
                Destroy(spawnedObject);

            int randomIndex = Random.Range(0, Objects.Length);
            spawnedObject = Objects[randomIndex];

            if (spawnedObject == null)
            {
                Lootations.Logger.LogDebug("ObjectRandomizer rolled null");
                return;
            }

            spawnedObject = Instantiate(spawnedObject, transform);

            if (RandomizeRotation)
            {
                spawnedObject.transform.Rotate(new Vector3(0, Random.Range(0f, 360f), 0));
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawCube(transform.position + new Vector3(0f, VisualizationSize.y / 2, 0f), VisualizationSize);
        }
    }
}
