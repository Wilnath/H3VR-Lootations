using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.Newtonsoft.Json;

namespace Lootations
{
    public class LootTable
    {
        public string Name { get; private set; }
        public string[] Meta { get; set; }
        public List<TableEntry> Entries { get; private set; } = new List<TableEntry>();

        public int TotalWeight { get; set; } = -1;
        public MetaTags Tags;

        public LootTable(string name, TableEntry[] entries)
        {
            foreach (var item in entries)
            {
                AddEntry(item);
            }
            Name = name;
        }

        public void AddEntry(TableEntry entry)
        {
            Entries.Add(entry);
        }

        public List<string> RollObjectId(ref MetaTags tags)
        {
            MetaTags.UpdateTags(this, ref tags);

            if (TotalWeight == -1)
                CalculateTotalWeight();
            if (TotalWeight == 0)
            {
                Lootations.Logger.LogError("Total weight calculated to be 0 in " + Name);
                return [];
            }

            if (tags.LevelSwitch)
            {
                int max = Entries.Count - 1;
                int levelToRoll = Mathf.Clamp(LootManager.CurrentLevel, 0, max);
                foreach (var entry in Entries)
                {
                    if (entry.Weight == levelToRoll)
                    {
                        return entry.RollObjectId(ref tags);
                    }
                }
            }

            int currentWeight = UnityEngine.Random.Range(1, TotalWeight+1);
            foreach (var entry in Entries)
            {
                if (currentWeight <= entry.Weight)
                {
                    return entry.RollObjectId(ref tags);
                }
                currentWeight -= entry.Weight;
            }
            Lootations.Logger.LogError("Could not roll any ids in " + Name);
            return [];
        }

        public void CalculateTotalWeight()
        {
            TotalWeight = 0;
            foreach(var entry in Entries)
            {
                TotalWeight += entry.Weight;
            }
        }
    }
}
