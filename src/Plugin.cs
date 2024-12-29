using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using BepInEx.Configuration;
using UnityEngine.SceneManagement;

namespace Lootations
{
    [BepInAutoPlugin]
    [BepInProcess("h3vr.exe")]
    public partial class Lootations : BaseUnityPlugin
    {
        public static bool h3mpEnabled = false;

        public static ConfigEntry<int> MaxRandomLootObjectSpawns;
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

            SetupConfig();

            h3mpEnabled = Chainloader.PluginInfos.ContainsKey("VIP.TommySoucy.H3MP");
            SceneManager.activeSceneChanged += OnSceneSwitched;
            if (h3mpEnabled)
            {
                Logger.LogDebug("H3MP Detected as enabled");
            }
            else
            {
                Logger.LogDebug("Starting with no H3MP");
            }
        }

        private void OnSceneSwitched(Scene old_scene, Scene new_scene)
        {
            Logger.LogDebug("Scene switch occurred");
            LootManager.OnSceneSwitched();
            SceneManager.activeSceneChanged += Networking.OnSceneSwitched;
        }
        
        // The line below allows access to your plugin's logger from anywhere in your code, including outside of this file.
        // Use it with 'YourPlugin.Logger.LogInfo(message)' (or any of the other Log* methods)
        internal new static ManualLogSource Logger { get; private set; }

        private void SetupConfig()
        {
            MaxRandomLootObjectSpawns = Config.Bind("Gameplay", "MaxRandomLootObjectSpawns", 250, "The maximum amount of random loot objects that will spawn per level. -1 to disable");
            DisabledModDrops = Config.Bind("Gameplay", "DisabledModDrops", "", "Plugin names that should count as not being loaded for loot tables. Seperate plugin names with a colon");

            SosigSpawnDistance = Config.Bind("Performance", "SosigSpawnDistance", 75f, "How far away you have to be for sausages to spawn in.");
            SosigDeSpawnDistance = Config.Bind("Performance", "SosigDeSpawnDistance", 150f, "TODO");
            ProximitySpawnDistance = Config.Bind("Performance", "ProximitySpawnDistance", 25f, "Distance until Proximity triggers activate, can be expensive if too far.\nNote: Some proximity triggers override this setting.");

            CullingEnabled = Config.Bind("Experimental", "Culling", true, "If untouched items are to be disabled after player is set distance away from the loot spawn point.");
            ItemCullingDistance = Config.Bind("Experimental", "ItemCullingDistance", 50f, "The distance at which items are set to be inactive.");
        }
    }
}
