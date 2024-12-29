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
        private static Dictionary<string, LootTable> _lootTables;
        private readonly static LootTable NullTable = new LootTable("NONE", [ TableEntry.ObjectEntry("NONE", 1) ]);

        public static void Initialize()
        {
            _lootTables = new Dictionary<string, LootTable>();
            Lootations.Logger.LogInfo("Begun reading loot table files.");
            string[] lootTableFiles = Directory.GetFiles(Paths.PluginPath, "*.lttbl", SearchOption.AllDirectories);
            foreach (var lootTableFile in lootTableFiles)
            {
                ProcessFile(lootTableFile);
            }
            ValidateAllTables();
        }

        private static void ValidateAllTables()
        {
            foreach (var kvp in _lootTables)
            {
                var table = kvp.Value;
                RemoveZeroWeightEntries(table);
                ValidateObjectIds(kvp.Value);
            }
            foreach (var kvp in _lootTables)
            {
                kvp.Value.CalculateTotalWeight();
            }
            foreach (var kvp in _lootTables)
            {
                ValidateReferences(kvp.Value);
            }
        }

        private static void RemoveZeroWeightEntries(LootTable table)
        {
            MetaTags tags = new MetaTags();
            MetaTags.UpdateTags(table, ref tags);

            if (!tags.IsZeroWeightEntriesInvalid())
            {
                return;
            }
            
            for (int i = 0; i <  table.Entries.Count; i++)
            {
                var item = table.Entries[i];
                if (item.Weight == 0)
                {
                    table.Entries.RemoveAt(i);
                    i--;
                }
            }
        }

        private static void ValidateObjectIds(LootTable table)
        {
            for (int i = 0; i < table.Entries.Count; i++)
            {
                if (table.Entries[i].Type == TableEntry.TableType.TABLE_REFERENCE)
                {
                    continue;
                }
                var ids = table.Entries[i].LootIds;
                for (int j = 0; j < ids.Count; j++)
                {
                    var item1 = ids[j];
                    if (!IM.OD.ContainsKey(item1))
                    {
                        /*Lootations.Logger.LogError("Unknown object id " + item1 + " in table " + table.Name);
                        ids.RemoveAt(j);
                        j--;*/

                        // TODO: Could be mod item, need to hook it better
                        Lootations.Logger.LogDebug("Unknown object id " + item1 + " in table " + table.Name);
                    }
                }
                if (ids.Count == 0)
                {
                    table.Entries.RemoveAt(i);
                    table.CalculateTotalWeight();
                    i--;
                }
            }
        }

        private static void ValidateReferences(LootTable table)
        {
            for (int i = 0; i < table.Entries.Count; i++)
            {
                var entry = table.Entries[i];
                if (entry.Type == TableEntry.TableType.TABLE_REFERENCE )
                {
                    if (GetTable(entry.LootIds[0]) == NullTable)
                    {
                        Lootations.Logger.LogWarning("Reference to non-existant table " + entry.LootIds[0] + " in " + table.Name);
                        table.Entries.RemoveAt(i);
                        table.CalculateTotalWeight();
                        i--;
                    }
                    else if (GetTable(entry.LootIds[0]).TotalWeight == 0)
                    {
                        Lootations.Logger.LogDebug("Deleting reference entry to empty table " + entry.LootIds[0] + " in " + table.Name);
                        table.Entries.RemoveAt(i);
                        table.CalculateTotalWeight();
                        i--;
                    }
                }
            }
        }

        public static void AddLootTable(LootTable table)
        {
            table.Tags = MetaTags.ParseTableTags(table);
            MetaTags tags = table.Tags;
            foreach (IValidator validator in tags.Validators)
            {
                if (!validator.IsValid())
                {
                    Lootations.Logger.LogInfo("Skipping loading table due to failed validator: " + table.Name);
                    return;
                }
            }
            if (_lootTables.ContainsKey(table.Name)) 
            {
                Lootations.Logger.LogInfo("Appending to table " + table.Name);
                LootTable existingTable = _lootTables[table.Name];
                foreach (TableEntry entry in table.Entries)
                {
                    // TODO: Collapse same entries to be same weight
                    existingTable.Entries.Add(entry);
                }
                return;
            }
            for (int i = 0; i < table.Entries.Count; i++)
            {
                TableEntry entry = table.Entries[i];
                if (entry.Weight == 0)
                {
                    table.Entries.Remove(entry);
                    i--;
                }
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
    }
}
