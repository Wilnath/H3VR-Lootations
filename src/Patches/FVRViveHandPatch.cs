using FistVR;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lootations
{
    [HarmonyPatch(typeof(FVRViveHand), nameof(FVRViveHand.BeginFlick))]
    internal class FVRViveHandPatch
    {
        public static void Prefix(FVRPhysicalObject o)
        {
            LootManager.OnGrabbityHandFlick(o);
        }
    }
}
