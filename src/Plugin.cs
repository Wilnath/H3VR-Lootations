using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using H3MP;
using H3MP.Networking;
using BepInEx.Configuration;

namespace Lootations
{
    [BepInAutoPlugin]
    [BepInProcess("h3vr.exe")]
    public partial class Lootations : BaseUnityPlugin
    {
        public static bool h3mpEnabled = false;

        public static ConfigEntry<float> ProximitySpawnDistance;
        public static ConfigEntry<float> ItemCullingDistance;
        public static ConfigEntry<float> SosigSpawnDistance;
        public static ConfigEntry<float> SosigDeSpawnDistance;
        public static ConfigEntry<bool> CullingEnabled;
        public static ConfigEntry<string> DisabledModDrops;

        private static Harmony harmonyInstance;
        
        private void Awake()
        {
            Logger = base.Logger;

            harmonyInstance = new Harmony("LootationsHarmonyInstance");
            harmonyInstance.PatchAll();

            h3mpEnabled = Chainloader.PluginInfos.ContainsKey("VIP.TommySoucy.H3MP");

            SetupConfig();
        }
        
        // The line below allows access to your plugin's logger from anywhere in your code, including outside of this file.
        // Use it with 'YourPlugin.Logger.LogInfo(message)' (or any of the other Log* methods)
        internal new static ManualLogSource Logger { get; private set; }

        private void SetupConfig()
        {
            SosigSpawnDistance = Config.Bind("Performance", "SosigSpawnDistance", 75f, "How far away you have to be for sausages to spawn in.");
            SosigDeSpawnDistance = Config.Bind("Performance", "SosigDeSpawnDistance", 150f, "TODO");
            ProximitySpawnDistance = Config.Bind("Performance", "ProximitySpawnDistance", 50f, "Distance until Proximity triggers activate, can be expensive if too far.\nNote: Some proximity triggers override this setting.");

            CullingEnabled = Config.Bind("Experimental", "Culling", true, "If untouched items are to be disabled after player is set distance away from the loot spawn point.");
            ItemCullingDistance = Config.Bind("Experimental", "ItemCullingDistance", 50f, "The distance at which items are set to be inactive.");
            DisabledModDrops = Config.Bind("Mods", "DisabledModDrops", "", "Plugin names that should count as not being loaded for loot tables. Seperate plugin names with a colon");
        }
    }
}
