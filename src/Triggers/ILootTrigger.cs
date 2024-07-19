using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lootations
{
    // change reset into diff interface
    public interface ILootTrigger
    {
        public delegate void OnTriggeredDelegate(ILootTrigger trigger);
        public event OnTriggeredDelegate OnTriggered;
        public void Trigger();
        public void LootReset();
    }
}
