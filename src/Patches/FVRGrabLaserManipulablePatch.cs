using FistVR;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Lootations
{
    [HarmonyPatch(typeof(FVRViveHand), nameof(FVRViveHand.Update))]
    public class FVRGrabLaserManipulablePatch
    {
        // TODO: Transpile instead of postfix
        static void Postfix(FVRViveHand __instance)
        {
            if (
                    ((__instance.IsInStreamlinedMode && __instance.Input.BYButtonPressed) || __instance.Input.TouchpadPressed)
                    && __instance.m_state == FVRViveHand.HandState.Empty && __instance.CurrentHoveredQuickbeltSlot == null
                    && __instance.m_grabHit.collider != null
               ) 
            {
                if (__instance.m_grabHit.collider.gameObject == null)
                {
                    Lootations.Logger.LogDebug("go from collider is null");
                    return;
                }
                IDistantManipulable manipulable = __instance.m_grabHit.collider.gameObject.GetComponent<IDistantManipulable>();
                // Assume grablaser rendering is sorted, change which laser is active if manipulable, interact if able
                if (manipulable != null)
                {
                    if (!__instance.BlueLaser.activeSelf)
                    {
                        __instance.BlueLaser.SetActive(true);
                    }
                    if (__instance.RedLaser.activeSelf)
                    {
                        __instance.RedLaser.SetActive(false);
                    }
                    if (__instance.Input.IsGrabDown)
                    {
                        Lootations.Logger.LogDebug("Doing a distant interact");

                        FVRInteractiveObject obj = __instance.m_grabHit.collider.gameObject.GetComponent<FVRInteractiveObject>();
                        if (obj == null)
                        {
                            Lootations.Logger.LogDebug("Could not get interactive object from distant manipulable");
                            return;
                        }

                        __instance.CurrentInteractable = obj;
                        __instance.m_state = FVRViveHand.HandState.GripInteracting;
                        obj.BeginInteraction(__instance);
                        manipulable.OnDistantInteract(__instance);
                        __instance.Buzz(__instance.Buzzer.Buzz_BeginInteraction);
                    }
                } 
            }
        }
    }
}
