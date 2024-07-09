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

        private static Harmony harmonyInstance;
        
        private void Awake()
        {
            Logger = base.Logger;

            harmonyInstance = new Harmony("LootationsHarmonyInstance");
            harmonyInstance.PatchAll();

            TableManager.Initialize();
            h3mpEnabled = Chainloader.PluginInfos.ContainsKey("VIP.TommySoucy.H3MP");
            // Your plugin's ID, Name, and Version are available here.
            Logger.LogMessage($"{Id} {Name} {Version} Awakened");

            SetupConfig();
        }
        
        // The line below allows access to your plugin's logger from anywhere in your code, including outside of this file.
        // Use it with 'YourPlugin.Logger.LogInfo(message)' (or any of the other Log* methods)
        internal new static ManualLogSource Logger { get; private set; }

        private void SetupConfig()
        {
            ItemCullingDistance = Config.Bind("Performance", "ItemCullingDistance", 50f, "The distance at which items are set to be inactive.");
            SosigSpawnDistance = Config.Bind("Performance", "SosigSpawnDistance", 75f, "How far away you have to be for sausages to spawn in.");
            SosigDeSpawnDistance = Config.Bind("Performance", "SosigDeSpawnDistance", 150f, "TODO");
            ProximitySpawnDistance = Config.Bind("Performance", "ProximitySpawnDistance", 50f, "Distance until Proximity triggers activate, can be expensive if too far.\nNote: Some proximity triggers override this setting.");
        }
    }
}
