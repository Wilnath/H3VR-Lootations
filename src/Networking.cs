using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using H3MP;
using H3MP.Networking;
using H3MP.Tracking;

namespace Lootations
{
    public static class Networking
    {
        private static readonly string PACKET_TYPE_STRING_ID = "Lootations_ItemGrab";
        private static int itemGrabPacketId = -1;

        public static void SetupPacketTypes()
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
        }

        public static bool isConnected()
        {
            // h3mp exists, currently in a server 
            return Lootations.h3mpEnabled && Mod.managerObject != null;
        }
        
        public static bool isClient()
        {
            return isConnected() && !ThreadManager.host;
        }

        public static bool isHost()
        {
            return isConnected() && ThreadManager.host;
        }

        public static void ReceiveItemGrab(int clientId, Packet packet)
        {
            Lootations.Logger.LogDebug("Item grab packet received");
            int trackingId = packet.ReadInt();
            LootManager.StopTrackingNetworkId(trackingId);
        }

        public static void SendItemGrab(GameObject obj)
        {
            //Lootations.Logger.LogDebug("Attempting to send item grab.");
            if (isClient())
            {
                TrackedObjectData data = obj.GetComponent<TrackedItem>().data;
                if (data == null)
                {
                    Lootations.Logger.LogError("Tried to get tracking of object that doesn't have it!");
                }
                Packet packet = new Packet(itemGrabPacketId);
                packet.Write(data.trackedID);
            } 
            else
            {
                //Lootations.Logger.LogDebug("Not connected to server, or is host. Cancelling item grab");
            }
        }
    }
}
