using FistVR;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lootations
{
    [HarmonyPatch(typeof(IM), nameof(IM.GenerateItemDBs))]
    public class GenerateItemDBsPatch
    {
        public static void Postfix()
        {
            TableManager.Initialize();
        }
    }
}
