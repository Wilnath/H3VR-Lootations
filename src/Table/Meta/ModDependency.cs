using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lootations
{
    public class ModDependency : IValidator
    {
        private string _modDependency;

        public ModDependency(string modDependency)
        {
            _modDependency = modDependency;
        }

        public bool IsValid() 
        { 
            if (Lootations.DisabledModDrops != null && Lootations.DisabledModDrops.Value.Split(',').Contains(_modDependency))
            {
                return false;
            }
            return Chainloader.PluginInfos.ContainsKey(_modDependency); 
        }
    }
}
