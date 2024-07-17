using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lootations
{
    public static class MetaTagsManager
    {
        public static Dictionary<string, Func<string, MetaTags, bool>> keyToMethod = new() {
            { 
                "ModDependency", (string s, MetaTags tags) => {
                    tags.Validators.Add(new ModDependency(s));
                    return true;
                } 
            }
        };
        
        public static MetaTags ParseTableMetaTags(LootTable table)
        {
            string[] tags = table.Meta;
            MetaTags result = new();
            foreach (string tag in tags)
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
                    Lootations.Logger.LogWarning("Skipping meta tag with non-existing meta key");
                    continue;
                }

                string value = splitTags[1].Trim();
                keyToMethod[key](value, result);
            }
            return result;
        }
    }
}
