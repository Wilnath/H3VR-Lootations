using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Lootations
{
    public interface IDistantManipulable
    {
        public void OnDistantInteract(FVRViveHand hand);
    }
}
