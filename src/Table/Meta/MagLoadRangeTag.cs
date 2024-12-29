using FistVR;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Lootations
{
    public class MagLoadRangeTag
    {
        public float x = 1.0f;
        public float y = 1.0f;

        public void Apply(GameObject obj)
        {
            FVRFireArmMagazine mag = obj.GetComponent<FVRFireArmMagazine>();
            if (mag != null)
            {
                Apply(mag);
            }
        }

        public void Apply(GameObject obj, FireArmRoundClass fireArmClass)
        {
            FVRFireArmMagazine mag = obj.GetComponent<FVRFireArmMagazine>();
            if (mag != null)
            {
                Apply(mag, fireArmClass);
            }
        }
        
        public void Apply(FVRFireArmMagazine mag)
        {
            // Assuming no value is supplied, and that the magazine hasn't been emptied, we use the supplied default bullets as 
            // a base.
            if (mag.LoadedRounds.Length == 0)
            {
                Lootations.Logger.LogError("Unable to apply MagRange to dropped item: No Default Rounds");
                return;
            }

            Apply(mag, mag.LoadedRounds.First().LR_Class);
        }

        public void Apply(FVRFireArmMagazine mag, FireArmRoundClass fireArmClass)
        {
            if (!IsEnabled())
            {
                // Base H3VR solves filling a mag with some base amount of rounds, so do nothing here.
                return;
            }

            if (x == 0.0f && y == 0.0f)
            {
                mag.ForceEmpty();
                mag.UpdateBulletDisplay();
                return;
            }

            float perc;
            // Unsure if this is needed, but being safe incase of floating point trickery
            if (x == y)
            {
                perc = x;
            }
            else
            {
                perc = UnityEngine.Random.Range(x, y);
            }

            mag.ReloadMagWithTypeUpToPercentage(fireArmClass, perc);
            mag.UpdateBulletDisplay();
        }

        private static void Parser(string s, ref MetaTags tags)
        {
            string[] stringValues = s.Split(',');

            if (stringValues.Length == 0 || stringValues.Length > 2)
            {
                Lootations.Logger.LogError("Invalid mag load range parameter amount");
                return;
            }

            float v1;
            if (!float.TryParse(stringValues[0], out v1))
            {
                Lootations.Logger.LogError("Failed converting mag load range into float");
                return;
            }

            if (stringValues.Length == 1)
            {
                SetAsStatic(v1, ref tags);
                return;
            }

            float v2;
            if (!float.TryParse(stringValues[1], out v2))
            {
                Lootations.Logger.LogError("Failed converting mag load range into float");
                return;
            }

            SetAsRange(v1, v2, ref tags);
        }

        public static void SetAsStatic(float v1, ref MetaTags tags)
        {
            tags.MagLoadRange.x = v1;
            tags.MagLoadRange.y = v1;
        }

        public static void SetAsRange(float v1, float v2, ref MetaTags tags)
        {
            if (v2 < v1)
            {
                float temp = v2;
                v2 = v1;
                v1 = temp;
            }

            tags.MagLoadRange.x = v1;
            tags.MagLoadRange.y = v2;
        }

        private bool IsEnabled()
        {
            return x != 1.0f || y != 1.0f;
        }

        public static MetaTags.MetaTagsFunction GetMetaTagParser()
        {
            return Parser;
        }
    }
}
