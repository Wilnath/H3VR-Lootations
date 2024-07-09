using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;

namespace Lootations
{
    [HarmonyPatch(typeof(FVRPhysicalObject), nameof(FVRPhysicalObject.BeginInteraction))]
    internal class FVRPhysicalObjectPatch
    {
        static void Postfix(FVRPhysicalObject __instance)
        {
            LootManager.OnPhysicalObjectPickup(__instance.gameObject);
            //Lootations.Logger.LogDebug("PHYS OBJECT PICKED UP");
        }
    }
}
