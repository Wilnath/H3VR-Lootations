using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using HarmonyLib;

namespace Lootations
{
    [HarmonyPatch(typeof(SteamVR_LoadLevel), nameof(SteamVR_LoadLevel.LoadLevel))]
    internal class SteamVRLoadLevelPatch
    {
        static void Postfix()
        {
            LootManager.OnSceneSwitched();
        }
    }
}
