using BepInEx;
using FistVR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lootations
{
    public static class TableManager
    {
        private static Dictionary<string, LootTable> _lootTables = new();
        private readonly static LootTable NullTable = new LootTable("NONE", [ TableEntry.ObjectEntry("NONE", 1) ]);

        public static void Initialize()
        {
            Lootations.Logger.LogInfo("Start reading loot table files.");
            string[] lootTableFiles = Directory.GetFiles(Paths.PluginPath, "*.lttbl", SearchOption.AllDirectories);
            foreach (var lootTableFile in lootTableFiles)
            {
                ProcessFile(lootTableFile);
            }
            ValidateTables();
        }

        public static void AddLootTable(LootTable table)
        {
            if (_lootTables.ContainsKey(table.Name)) 
            {
                Lootations.Logger.LogError("Tried to write already existing loot table: " + table.Name);
                return;
            }
            _lootTables.Add(table.Name, table);
        }

        public static LootTable GetTable(string name)
        {
            if (!_lootTables.TryGetValue(name, out LootTable value))
            {
                Lootations.Logger.LogWarning("Attempt to access nonexistant table " + name);
                return NullTable;
            }
            return value;
        }

        private static void ProcessFile(string fileName)
        {
            string output = File.ReadAllText(fileName);
            List<LootTable> readTables = JsonConvert.DeserializeObject<List<LootTable>>(output);
            foreach (var table in readTables)
            {
                AddLootTable(table);
            }
        }

        private static void ValidateTables()
        {
            foreach (var table in _lootTables)
            {
            }
        }
    }
}
