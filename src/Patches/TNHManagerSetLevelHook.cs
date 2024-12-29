using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using FistVR;

namespace Lootations
{
    [HarmonyPatch(typeof(TNH_Manager), nameof(TNH_Manager.SetLevel))]
    public class TNHManagerSetLevelHook
    {
        public static void Postfix(int level)
        {
            return;
        }
    }
}
