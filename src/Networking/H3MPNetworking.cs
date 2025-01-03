﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using H3MP;
using H3MP.Networking;
using H3MP.Tracking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lootations
{
    public static class H3MPNetworking
    {
        private static readonly string ITEM_GRAB_STRING_ID = "Lootations_ItemGrab";
        private static readonly string REROLL_LOOT_STRING_ID = "Lootations_RerollLoot";
        private static readonly string TRIGGER_ACTIVATED_STRING_ID = "Lootations_TriggerActivated";
        private static readonly string TRIGGER_DATA_STRING_ID = "Lootations_TriggerData";

        private static Dictionary<string, Mod.CustomPacketHandler> packetHandlers = new() {
            { ITEM_GRAB_STRING_ID, ReceiveItemGrab },
            { REROLL_LOOT_STRING_ID, ReceiveRerollLoot },
            { TRIGGER_ACTIVATED_STRING_ID, ReceiveTriggerActivated },
            { TRIGGER_DATA_STRING_ID, ReceiveTriggerData },
        };

        private static Dictionary<string, int> packetIds = new();

        public static void OnSceneSwitched(Scene old_scene, Scene new_scene)
        {
            InitializeNetworking();
        }

        public static void InitializeNetworking()
        {
            if (!IsConnected())
            {
                return;
            }
            SetupPacketTypes();
        }

        private static void SetupPacketTypes()
        {
            Lootations.Logger.LogDebug("Setting up packet types.");
            foreach (var item in packetHandlers)
            {
                if (!packetIds.ContainsKey(item.Key))
                {
                    Lootations.Logger.LogDebug("Adding handler " + item.Key + " as default ID");
                    packetIds.Add(item.Key, -1);
                }
            }
            if (IsHost())
            {
                Lootations.Logger.LogDebug("Setting up packets as host.");
                foreach (var item in packetHandlers)
                {
                    Lootations.Logger.LogDebug("Setting up handler " + item.Key);
                    if (Mod.registeredCustomPacketIDs.ContainsKey(item.Key))
                    {
                        packetIds[item.Key] = Mod.registeredCustomPacketIDs[item.Key];
                        Lootations.Logger.LogDebug("Handler already existed as id " + packetIds[item.Key]);
                    }
                    else
                    {
                        packetIds[item.Key] = Server.RegisterCustomPacketType(item.Key);
                        Lootations.Logger.LogDebug("Handler registered as id " + packetIds[item.Key]);

                        // Called in Server.RegisterCustomPacketType
                        //ServerSend.RegisterCustomPacketType(item.Key, packetIds[item.Key]);
                    }
                    Mod.customPacketHandlers[packetIds[item.Key]] = item.Value;
                }
            }
            else if (IsClient()) 
            {
                Lootations.Logger.LogDebug("Setting up packets as client.");
                Mod.CustomPacketHandlerReceived += ReceiveClientPacketSync;
                foreach (var item in packetHandlers)
                {
                    Lootations.Logger.LogDebug("Processing handler " + item.Key.ToString());
                    if (Mod.registeredCustomPacketIDs.ContainsKey(item.Key))
                    {
                        packetIds[item.Key] = Mod.registeredCustomPacketIDs[item.Key];

                        // I mean this should be set at this point but SOMEHOW wasn't
                        Mod.customPacketHandlers[packetIds[item.Key]] = packetHandlers[item.Key];
                        Lootations.Logger.LogDebug("Handler already registered as id " + packetIds[item.Key]);
                    }
                    else
                    {
                        Lootations.Logger.LogDebug("Registering the handler as new, awaiting ID from host.");
                        ClientSend.RegisterCustomPacketType(item.Key);
                    }
                }
            }
        }
            
        public static void ReceiveClientPacketSync(string ID, int index)
        {
            Lootations.Logger.LogDebug("Got ClientPacketSync for HandlerID " + ID + " index: " + index);
            if (packetHandlers.ContainsKey(ID))
            {
                packetIds[ID] = index;
                Mod.customPacketHandlers[index] = packetHandlers[ID];
                Lootations.Logger.LogDebug("Successfully connected handling for packet ID: " + ID);
            } 
            else
            {
                //Lootations.Logger.LogError("Unknown handler id in custom packet handler sync: " + handlerID);
            }
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
                if (obj == null)
                {
                    Lootations.Logger.LogDebug("Obj is, somehow, null");
                    return;
                }
                TrackedItem item = obj.GetComponent<TrackedItem>();
                if (item == null)
                {
                    Lootations.Logger.LogDebug("Obj found but no TrackedItem");
                    return;
                }
                TrackedObjectData data = item.data;
                if (data == null)
                {
                    Lootations.Logger.LogDebug("Tried to get tracking of object that doesn't have it!");
                    return;
                }
                Packet packet = new Packet(packetIds[ITEM_GRAB_STRING_ID]);
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
            Packet packet = new Packet(packetIds[REROLL_LOOT_STRING_ID]);
            packet.Write(seed);
            ServerSend.SendTCPDataToAll(packet, true);
        }

        public static void ReceiveRerollLoot(int clientId, Packet p)
        {
            int seed = p.ReadInt();
            if (IsHost())
            {
                // shouldn't happen, but just in case
                Lootations.Logger.LogInfo("Ignoring reroll loot packet as host.");
                return;
            }
            LootManager.RerollLoot(seed);
        }

        public static void SendTriggerActivated(int id)
        {
            Packet packet = new Packet(packetIds[TRIGGER_ACTIVATED_STRING_ID]);
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
            if (trigger == null)
            {
                Lootations.Logger.LogError("Got packet to trigger non-existing loot trigger!");
                return;
            }
            trigger.Trigger();
            // If host, trigger triggers and is then broadcasted to all
        }


        public static void SendTriggerDataUDP(ILootTrigger trigger, List<byte> bytes)
        {
            Packet packet = new Packet(packetIds[TRIGGER_DATA_STRING_ID]);
            packet.Write(LootManager.GetLootTriggerId(trigger));
            packet.buffer.AddRange(bytes);
            if (IsClient())
            {
                ClientSend.SendUDPData(packet, true);
            }
            else
            {
                ServerSend.SendUDPDataToClients(packet, GetPlayerIds().ToList(), -1, true);
            }
        }

        public static void SendTriggerDataTCP(ILootTrigger trigger, List<byte> bytes)
        {
            Packet packet = new Packet(packetIds[TRIGGER_DATA_STRING_ID]);
            packet.Write(LootManager.GetLootTriggerId(trigger));
            packet.buffer.AddRange(bytes);
            if (IsClient())
            {
                ClientSend.SendTCPData(packet, true);
            }
            else
            {
                ServerSend.SendTCPDataToAll(packet, true);
            }
        }

        public static void ReceiveTriggerData(int clientId, Packet packet)
        {
            int id = packet.ReadInt();
            ILootTrigger trigger = LootManager.GetLootTriggerById(id);
            if (trigger == null)
            {
                Lootations.Logger.LogWarning("Got packet for non-existant trigger with id: " + id.ToString());
                return;
            }
            ITriggerDataReceiver dataReceiver = trigger as ITriggerDataReceiver;
            if (dataReceiver != null)
            {
                // only send data portion of the packet
                dataReceiver.ReceiveTriggerData(packet.buffer.GetRange(sizeof(int), packet.buffer.Count - sizeof(int)));
            }
            else
            {
                Lootations.Logger.LogWarning("Received trigger data for non data-receiver trigger with id: " + id.ToString());
                return;
            }
        }
    }
}
