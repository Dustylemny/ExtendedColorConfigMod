using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RWCustom;
using UnityEngine;

namespace ColorConfig
{
    public static class ColConversions
    {
        public static Vector3 HSL1002HSV100(Vector3 hsl)
        {
            float val = hsl.z + hsl.y * (Mathf.Min(hsl.z, 100 - hsl.z) / 100);
            float sat = val == 0 ? 0 : 200 * (1 - (hsl.z / val));
            return new(hsl.x, sat, val);
        }
        public static Vector3 HSV1002HSL100(Vector3 hsv)
        {
            float lit = hsv.z * (1 - (hsv.y / 200));
            float sat = lit == 0 || lit == 100 ? 0 : (hsv.z - lit) / (Mathf.Min(lit, 100 - lit) / 100);
            return new(hsv.x, sat, lit);
        }
        public static Color HSV2RGB(Vector3 hsv) => Color.HSVToRGB(hsv.x % 1, hsv.y, hsv.z);
        public static Color HSL2RGB(Vector3 hsl) => Custom.HSL2RGB(hsl.x % 1, hsl.y, hsl.z);
        public static Vector3 HSV2HSL(Vector3 hsv)
        {
            float lit = hsv.z * (1 - (hsv.y / 2));
            float sat = lit == 0 || lit == 1 ? 0 : (hsv.z - lit) / (Mathf.Min(lit, 1 - lit));
            return new(hsv.x, sat, lit);
        }
        public static Vector3 RGB2HSV(Color rgb)
        {
            Color.RGBToHSV(rgb, out float h, out float s, out float v);
            return new(h, s, v);
        }
        public static Vector3 HSL2HSV(Vector3 hsl)
        {
            float val = hsl.z + hsl.y * Mathf.Min(hsl.z, 1 - hsl.z);
            float sat = val == 0 ? 0 : 2 * (1 - (hsl.z / val));
            return new(hsl.x, sat, val);
        }
        public static string RGB2Hex(this Color rgb) => ColorUtility.ToHtmlStringRGB(rgb);
        public static string HSV2Hex(Vector3 hsv) => HSV2RGB(hsv).RGB2Hex();
        public static string HSL2Hex(Vector3 hsl) => HSL2RGB(hsl).RGB2Hex();

    }
}
