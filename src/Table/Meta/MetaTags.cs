using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lootations
{
    public class MetaTags
    {
        public delegate void MetaTagsFunction(string s, ref MetaTags tags);

        private static Dictionary<string, MetaTagsFunction> keyToMethod = new() {
            {
                "ModDependency", (string s, ref MetaTags tags) =>
                {
                    tags.Validators.Add(new ModDependency(s));
                }
            },
            {
                "CartridgesAsAmmoBoxes", (string s, ref MetaTags tags) =>
                {
                    tags.CartridgesAsAmmoBoxes = s.ToLower() == "true";
                }
            },
            {
                "OneShotLevelSwitch", (string s, ref MetaTags tags) =>
                {
                    tags.LevelSwitch = s.ToLower() == "true";
                }
            },
            {
                "MagLoadRange", MagLoadRangeTag.GetMetaTagParser()
            }
        };

        public static MetaTags ParseTableTags(LootTable table)
        {
            MetaTags result = new();
            UpdateTags(table, ref result);
            return result;
        }

        private static void ResetOneShotTags(ref MetaTags tags)
        {
            tags.LevelSwitch = false;
        }

        public static void UpdateTags(LootTable table, ref MetaTags tags)
        {
            ResetOneShotTags(ref tags);

            string[] tagsString = table.Meta;
            foreach (string tag in tagsString)
            {
                string[] splitTags = tag.Split(':');
                if (splitTags.Length != 2)
                {
                    Lootations.Logger.LogWarning("Skipping meta tag with ambiguous key:value pair");
                    continue;
                }

                string key = splitTags[0].Trim();
                if (!keyToMethod.ContainsKey(key))
                {
                    Lootations.Logger.LogWarning("Skipping meta tag with non-existing meta key: " + key);
                    continue;
                }

                string value = splitTags[1].Trim();
                keyToMethod[key](value, ref tags);
            }
        }

        // For when a zero weight entry isn't invalid, like in a level switch
        public bool IsZeroWeightEntriesInvalid()
        {
            return !LevelSwitch;
        }

        public List<IValidator> Validators = new();
        public bool CartridgesAsAmmoBoxes = false;
        public MagLoadRangeTag MagLoadRange = new MagLoadRangeTag();

        /* One Shot Tags */
        public bool LevelSwitch = false;
    }
}
