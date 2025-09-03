using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;

namespace ColorConfig
{
    public static class MenuToolObj
    {

        public static Slider.SliderID RedRGB = new("RedRGB"), GreenRGB = new("GreenRGB"), BlueRGB = new("BlueRGB"), 
            HueHSV = new("HueHSV"), SatHSV = new("SatHSV"), ValHSV = new("ValHSV");
        public static readonly Vector3 hslClampMax = new(0.99f, 1, 1), hslClampMin = new(0, 0, 0.01f);
        public const string red = "RED", green = "GREEN", blue = "BLUE", value = "VALUE", hue = "HUE", sat = "SAT", lit = "LIT";

        public static Slider.SliderID[] RGBSliderIDS => [RedRGB, GreenRGB, BlueRGB];
        public static Slider.SliderID[] HSVSliderIDS => [HueHSV, SatHSV, ValHSV];

        //names
        public static string[] HSLNames => [hue, sat, lit];
        public static string[] RGBNames => [red, green, blue];
        public static string[] HSVNames => [hue, sat, value];
        public static string[] HueOOSigns => [" °", "%", "%"];
        public static bool[]? RGBShowInt => ModOptions.Instance.IntToFloatColorValues.Value ? null : [true, true, true];
        public static bool[] HueOOShowInt => [!ModOptions.Instance.IntToFloatColorValues.Value];

        public static readonly float[] rgbMultipler = [255, 255, 255], hueOOMultipler = [360, 100, 100];
        public static void CustomSliderSetHSL(Slider slider, float f, Vector3 hsl, Action<Vector3> applyHSL, bool clampHue = true)
        {
            if (slider?.ID == null || applyHSL == null) return;

            if (RGBSliderIDS.Contains(slider.ID))
            {
                Color color = ColConversions.HSL2RGB(hsl);
                color[RGBSliderIDS.IndexOf(slider.ID)] = f;
                SmallUtils.Vector32RGB(SmallUtils.RWIIIClamp(SmallUtils.RGB2Vector3(SmallUtils.RGBClamp01(color)), CustomColorModel.RGB, out Vector3 newRGBHSL));
                applyHSL.Invoke(SmallUtils.FixNonHueSliderWonkiness(newRGBHSL, hsl));
            }
            if (HSVSliderIDS.Contains(slider.ID))
            {
                Vector3 hsv = ColConversions.HSL2HSV(hsl);
                hsv[HSVSliderIDS.IndexOf(slider.ID)] = f;
                SmallUtils.RWIIIClamp(hsv, CustomColorModel.HSV, out Vector3 newHSVHSL, clampHue);
                applyHSL.Invoke(newHSVHSL);
            }
        }
        public static bool CustomHSLValueOfSlider(Slider slider, Vector3 hsl, out float f)
        {
            f = -1;
            if (slider?.ID == null) return false;

            if (RGBSliderIDS.Contains(slider.ID))
                f = ColConversions.HSL2RGB(hsl)[RGBSliderIDS.IndexOf(slider.ID)];
            if (HSVSliderIDS.Contains(slider.ID))
                f = ColConversions.HSL2HSV(hsl)[HSVSliderIDS.IndexOf(slider.ID)];

            return f >= 0;
        }
    }
}
