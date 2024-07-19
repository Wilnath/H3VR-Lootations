using System;
using UnityEngine;
using H3MP;
using H3MP.Networking;
using H3MP.Tracking;


/*
 * (Client does not spawn objectrandomizer prefabs due to desync)
 * ObjectRandomizer prefabs should be placed the same on host and client
 * this means the RNG is synchronized, so use the RNG here to assign IDs
 * Client can send trigger activation of things they interact with (this requires a trigger ID)
 * loot objects must somehow communicate trigger ids?
 * Server can take trigger ids, clients don't ask for spawn only communicate stuff they trigger
 * Spawns only happen on serverside
 * Items that spawn have their own id in LootManager
 * (or use H3MP tracked object data, it SHOULD work??)
 * (host can not double spawn incase of already spawned)
 */
// sr compass code !

namespace Lootations
{
    public static class Networking
    {
        // TODO dict?
        private static readonly string PACKET_TYPE_STRING_ID = "Lootations_ItemGrab";
        private static int itemGrabPacketId = -1;
        private static readonly string REROLL_LOOT_STRING_ID = "Lootations_RerollLoot";
        private static int rerollLootPacketId = -1;
        private static readonly string TRIGGER_ACTIVATED_STRING_ID= "Lootations_TriggerActivated";
        private static int triggerActivatedPacketId = -1;

        public static void InitializeNetworking()
        {
            SetupPacketTypes();
        }

        // TODO: MaaAAAaaan. this is verbose af
        private static void SetupPacketTypes()
        {
            if (Mod.registeredCustomPacketIDs.ContainsKey(PACKET_TYPE_STRING_ID))
            {
                itemGrabPacketId = Mod.registeredCustomPacketIDs[PACKET_TYPE_STRING_ID];
            }
            else
            {
                itemGrabPacketId = Server.RegisterCustomPacketType(PACKET_TYPE_STRING_ID);
            }
            Mod.customPacketHandlers[itemGrabPacketId] = ReceiveItemGrab;

            if (Mod.registeredCustomPacketIDs.ContainsKey(REROLL_LOOT_STRING_ID))
            {
                rerollLootPacketId = Mod.registeredCustomPacketIDs[REROLL_LOOT_STRING_ID];
            }
            else
            {
                rerollLootPacketId = Server.RegisterCustomPacketType(REROLL_LOOT_STRING_ID);
            }
            Mod.customPacketHandlers[rerollLootPacketId] = ReceiveRerollLoot;

            if (Mod.registeredCustomPacketIDs.ContainsKey(TRIGGER_ACTIVATED_STRING_ID))
            {
                triggerActivatedPacketId = Mod.registeredCustomPacketIDs[TRIGGER_ACTIVATED_STRING_ID];
            }
            else
            {
                triggerActivatedPacketId = Server.RegisterCustomPacketType(TRIGGER_ACTIVATED_STRING_ID);
            }
            Mod.customPacketHandlers[triggerActivatedPacketId] = ReceiveTriggerActivated;
        }

        public static bool IsConnected()
        {
            // h3mp exists, currently in a server 
            return Lootations.h3mpEnabled && Mod.managerObject != null;
        }
        
        public static bool IsClient()
        {
            return IsConnected() && !ThreadManager.host;
        }

        public static bool IsHost()
        {
            return IsConnected() && ThreadManager.host;
        }

        // Probably all stolen from Packer
        public static int[] GetPlayerIds()
        {
            int playerCount = GameManager.players.Count;
            int[] playerIds = new int[playerCount];
            int i = 0;
            foreach (var kvp in GameManager.players)
            {
                playerIds[i] = kvp.Key;
                i++;
            }
            return playerIds;
        }

        public static Vector3 GetPlayerPosition(int id)
        {
            return PlayerData.GetPlayer(id).head.transform.position;
        }

        public static void SendItemGrab(GameObject obj)
        {
            //Lootations.Logger.LogDebug("Attempting to send item grab.");
            if (IsClient())
            {
                TrackedObjectData data = obj.GetComponent<TrackedItem>().data;
                if (data == null)
                {
                    Lootations.Logger.LogError("Tried to get tracking of object that doesn't have it!");
                }
                Packet packet = new Packet(itemGrabPacketId);
                packet.Write(data.trackedID);
                ClientSend.SendTCPData(packet, true);
            } 
            else
            {
                //Lootations.Logger.LogDebug("Not connected to server, or is host. Cancelling item grab");
            }
        }

        public static void ReceiveItemGrab(int clientId, Packet packet)
        {
            Lootations.Logger.LogDebug("Item grab packet received");
            int trackingId = packet.ReadInt();
            LootManager.StopTrackingNetworkId(trackingId);
        }

        public static void SendRerollLoot(int seed)
        {
            Packet packet = new Packet(rerollLootPacketId);
            packet.Write(seed);
            ServerSend.SendTCPDataToAll(packet, true);
        }

        public static void ReceiveRerollLoot(int clientId, Packet p)
        {
            int seed = p.ReadInt();
            LootManager.RerollLoot(seed);
        }

        public static void SendTriggerActivated(int id)
        {
            Packet packet = new Packet();
            packet.Write(id);
            if (IsClient())
            {
                ClientSend.SendTCPData(packet, true);
            }
            else
            {
                ServerSend.SendTCPDataToAll(packet, true);
            }
        }

        public static void ReceiveTriggerActivated(int clientId, Packet packet)
        {
            int id = packet.ReadInt();
            ILootTrigger trigger = LootManager.GetLootTriggerById(id);
            trigger.Trigger();
            // If host, trigger triggers and is then broadcasted to all
        }
    }
}
