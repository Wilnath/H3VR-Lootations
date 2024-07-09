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
        private int TotalWeight { get; set; } = -1;
        public List<TableEntry> Entries { get; private set; } = new List<TableEntry>();

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

        public string[] RollObjectId()
        {
            if (TotalWeight == -1)
                CalculateTotalWeight();
            if (TotalWeight == 0)
            {
                Lootations.Logger.LogError("Total weight calculated to be 0 in " + Name);
                return [];
            }
            int currentWeight = UnityEngine.Random.Range(1, TotalWeight+1);
            foreach (var entry in Entries)
            {
                if (currentWeight <= entry.Weight)
                {
                    return entry.RollObjectId();
                }
                currentWeight -= entry.Weight;
            }
            Lootations.Logger.LogError("Could not roll any ids in " + Name);
            return [];
        }

        private void CalculateTotalWeight()
        {
            TotalWeight = 0;
            foreach(var entry in Entries)
            {
                TotalWeight += entry.Weight;
            }
        }
    }
}
