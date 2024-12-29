using System;
using UnityEngine;
using System.Collections.Generic;
using Valve.VR;
using UnityEngine.SceneManagement;

namespace Lootations
{
    // awful, awful, awful boilerplate class
    public static class Networking
    {
        public static bool IsConnected()
        {
            if (!Lootations.h3mpEnabled)
            {
                return false;
            }
            return H3MPIsConnected();
        }

        private static bool H3MPIsConnected()
        {
            return H3MPNetworking.IsConnected();
        }

        public static bool IsClient()
        {
            return IsConnected() && H3MPIsClient();
        }

        private static bool H3MPIsClient()
        {
            return H3MPNetworking.IsClient();
        }

        public static bool IsHost()
        {
            return IsConnected() && H3MPIsHost();
        }

        private static bool H3MPIsHost()
        {
            return H3MPNetworking.IsHost();
        }

        public static void OnSceneSwitched(Scene oldScene, Scene newScene)
        {
            if (Lootations.h3mpEnabled)
            {
                H3MPInitializeNetworking();
            }
        }

        private static void H3MPInitializeNetworking()
        {
            H3MPNetworking.InitializeNetworking();
        }


        public static void OnPhysicalObjectPickup(GameObject obj)
        {
            if (IsClient()) 
            {
                H3MPSendItemGrab(obj);
            }
        }

        private static void H3MPSendItemGrab(GameObject obj)
        {
            H3MPNetworking.SendItemGrab(obj);
        }

        public static void OnRerollLoot(int seed)
        {
            if (IsHost())
            {
                H3MPSendRerollLoot(seed);
            }
        }

        private static void H3MPSendRerollLoot(int seed)
        {
            H3MPNetworking.SendRerollLoot(seed);
        }

        public static int[] GetPlayerIds()
        {
            if (IsConnected())
            {
                return H3MPGetPlayerIds();
            }
            return [];
        }

        private static int[] H3MPGetPlayerIds()
        {
            return H3MPNetworking.GetPlayerIds();
        }

        public static Vector3 GetPlayerPosition(int id)
        {
            if (IsConnected())
            {
                return H3MPGetPlayerPosition(id);
            }
            return Vector3.zero;
        }

        private static Vector3 H3MPGetPlayerPosition(int id)
        {
            return H3MPNetworking.GetPlayerPosition(id);
        }

        public static void SendTriggerActivated(int id)
        {
            if (IsConnected())
            {
                H3MPSendTriggerActivated(id);
            }
        }

        private static void H3MPSendTriggerActivated(int id)
        {
            H3MPNetworking.SendTriggerActivated(id);
        }

        public static void SendTriggerDataTCP(ILootTrigger trigger, List<byte> data)
        {
            if (IsConnected())
            {
                H3MPSendTriggerData(trigger, data, true);
            }
        }

        public static void SendTriggerDataUDP(ILootTrigger trigger, List<byte> data)
        {
            if (IsConnected())
            {
                H3MPSendTriggerData(trigger, data, false);
            }
        }

        private static void H3MPSendTriggerData(ILootTrigger trigger, List<byte> data, bool sendTCP)
        {

            if (sendTCP)
            {
                H3MPNetworking.SendTriggerDataTCP(trigger, data);
            }
            else
            {
                H3MPNetworking.SendTriggerDataUDP(trigger, data);
            }
        }
    }
}
