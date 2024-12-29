using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lootations
{
    internal interface ITriggerDataReceiver
    {
        void ReceiveTriggerData(List<byte> data);
    }
}
