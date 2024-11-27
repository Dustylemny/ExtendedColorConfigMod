using System;
using System.Globalization;
using Menu;
using RWCustom;
using UnityEngine;
using static ColorConfig.ColConversions;

namespace ColorConfig
{
    public static class MenuToolObj
    {
        public static string Clipboard
        {
            get => GUIUtility.systemCopyBuffer;
            set => GUIUtility.systemCopyBuffer = value;
        }

        public static Slider.SliderID RedRGB = new("RedRGB", true);
        public static Slider.SliderID GreenRGB = new("GreenRGB", true);
        public static Slider.SliderID BlueRGB = new("BlueRGB", true);

        public static Slider.SliderID HueHSV = new("HueHSV", true);
        public static Slider.SliderID SatHSV = new("SatHSV", true);
        public static Slider.SliderID ValHSV = new("ValHSV", true);

        public const string red = "RED";
        public const string green = "GREEN";
        public const string blue = "BLUE";
        public const string value = "VALUE";

        public const string hue = "HUE";
        public const string sat = "SAT";
        public const string lit = "LIT";

        public const string degreeSign = " °";

        public static bool[] rgbShowInt = new[] { true, true, true };
        public static bool[] hueOOShowInt = new[] { true, false, false };

        public static readonly Vector3 hslClampMax = new(0.99f, 1, 1);
        public static readonly Vector3 hslClampMin = new(0, 0, 0.01f);

        public static readonly Vector3 rgbMultipler = new(255, 255, 255);
        public static readonly Vector3 hueOOMultipler = new(360, 100, 100);

    }
    public static class SmallUtils
    {

        public static Color RGBClamp01(Color color)
        {
            return new(Mathf.Clamp01(color.r), Mathf.Clamp01(color.g), Mathf.Clamp01(color.b));
        }
        public static Color Vector32RGB(Vector3 vector)
        {
            return new(vector.x, vector.y, vector.z);
        }
        public static Vector3 RGB2Vector3(Color color)
        {
            return new(color.r, color.g, color.b);
        }
        public static Vector3 HSL2Vector3(HSLColor hSLColor) => new(hSLColor.hue, hSLColor.saturation, hSLColor.lightness);
        public static HSLColor Vector32HSL(Vector3 hsl) => new(hsl.x, hsl.y, hsl.z);
        public static Vector3 FixHexSliderWonkiness(Vector3 hexHSL, Vector3 currentHSL)
        {
            //stops sliders from changing if anything changes but hex code never changes
            if (HSL2Hex(hexHSL) == HSL2Hex(currentHSL))
            {
                return currentHSL;
            }
            if (HSL2Hex(hexHSL) == HSL2Hex(new(currentHSL.x, hexHSL.y, currentHSL.z)))
            {
                return new(currentHSL.x, hexHSL.y, currentHSL.z);
            }
            return hexHSL;
        }
        public static Vector3 SlugcatSelectMenuHSL(SlugcatSelectMenu selM)
        {
            Vector3 color = new(1, 1, 1);
            if (selM.manager.rainWorld.progression.miscProgressionData.colorChoices[selM.slugcatColorOrder[selM.slugcatPageIndex].value]
                [selM.activeColorChooser].Contains(","))
            {
                string[] hslArray = selM.manager.rainWorld.progression.miscProgressionData.colorChoices
                    [selM.slugcatColorOrder[selM.slugcatPageIndex].value]
                [selM.activeColorChooser].Split(',');

                color = new Vector3(float.Parse(hslArray[0], NumberStyles.Any, CultureInfo.InvariantCulture),
                    float.Parse(hslArray[1], NumberStyles.Any, CultureInfo.InvariantCulture),
                    float.Parse(hslArray[2], NumberStyles.Any, CultureInfo.InvariantCulture));
            }

            return color;
        }
        public static Vector3 RWIIIClamp(Vector3 iii, CustomColorModel colorSpace, out Vector3 hsl)
        {
            if (colorSpace == CustomColorModel.RGB)
            {
                Color col = Vector32RGB(iii);
                hsl = Custom.RGB2HSL(new(iii.x, iii.y, iii.z));
                if (hsl.z < MenuToolObj.hslClampMin.z)
                {
                    hsl = HOOClamp(hsl, MenuToolObj.hslClampMin, MenuToolObj.hslClampMax);
                    col = HSL2RGB(hsl);
                }
                return new(col.r, col.g, col.b);
            }
            else if (colorSpace == CustomColorModel.HSV)
            {
                Vector3 hsv2HsL = HSV2HSL(iii);
                hsl = HOOClamp(hsv2HsL, MenuToolObj.hslClampMin, MenuToolObj.hslClampMax);
                return HSL2HSV(hsl);
            }
            hsl = HOOClamp(iii, MenuToolObj.hslClampMin, MenuToolObj.hslClampMax);
            return HOOClamp(hsl);
        }
        public static Vector3 HOOClamp(Vector3 value, Vector3 min = default, Vector3 max = default)
        {
            min = min == default? new(0, 0, 0) : min;
            max = max == default? new(1, 1, 1) : max;
            return new(Mathf.Clamp(value.x, min.x, max.x),
                Mathf.Clamp(value.y, min.y, max.y), Mathf.Clamp(value.z, min.z, max.z));
        }
        public static bool IfHexCodeValid(string value, out Color result)
        {

            return ColorUtility.TryParseHtmlString("#" + value, out result);
        }
        public static string SetHSLSaveString(Vector3 hsl)
        {
            return $"{hsl.x},{hsl.y},{hsl.z}";
        }

    }
    public static class ColConversions
    {
        //2RGB
        public static Color HSV2RGB(Vector3 hsv)
        {
            return Color.HSVToRGB(hsv.x, hsv.y, hsv.z);
        }
        public static Color HSL2RGB(Vector3 hsl)
        {
            return Custom.HSL2RGB(hsl.x, hsl.y, hsl.z);
        }
        //2HSV
        public static Vector3 RGB2HSV(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            return new Vector3(h, s, v);
        }
        public static Vector3 HSL2HSV(Vector3 hsl)
        {
            float val = hsl.z + hsl.y * Mathf.Min(hsl.z, 1 - hsl.z);
            float sat = val == 0 ? 0 : 2 * (1 - (hsl.z / val));

            return new(hsl.x, sat, val);
        }
        //2HSL
        public static Vector3 HSV2HSL(Vector3 hsv)
        {
            float lit = hsv.z * (1 - (hsv.y / 2));
            float sat = lit == 0 || lit == 1? 0 : (hsv.z - lit) / Mathf.Min(lit, 1 - lit);
            return new(hsv.x, sat, lit);
        }
        //2Hex
        public static string HSL2Hex(Vector3 hsl, out Color color)
        {
            color = HSL2RGB(hsl);
            return ColorUtility.ToHtmlStringRGB(color);
        }
        public static string HSL2Hex(Vector3 hsl)
        {
            Color color = HSL2RGB(hsl);
            return ColorUtility.ToHtmlStringRGB(color);
        }
        public static readonly float segment = (1f / 6f);
    }
    public enum CustomColorModel
    {
        RGB,
        HSL,
        HSV,
    }
}
