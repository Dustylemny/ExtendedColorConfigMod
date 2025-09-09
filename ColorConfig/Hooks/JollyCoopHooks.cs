using ColorConfig.MenuUI.Objects;
using JollyCoop.JollyMenu;
using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorConfig.Hooks
{
    public static partial class ColorConfigHooks
    {
        public static void JollyCoopMenu_Hooks()
        {
            try
            {
                On.JollyCoop.JollyMenu.ColorChangeDialog.ValueOfSlider += On_ColorChangeDialog_ValueOfSlider;
                On.JollyCoop.JollyMenu.ColorChangeDialog.SliderSetValue += On_ColorChangeDialog_SliderSetValue;
                On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.ctor += On_ColorChangeDialog_ColorSlider_ctor;
                //On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.RemoveSprites += On_ColorChangeDialog_ColorSlider_RemoveSprites;
                //colSlider will have extension as subobject so it will remove normally
                ColorConfigMod.DebugLog("Sucessfully extended color interface for jolly coop menu!");
            }
            catch (Exception ex)
            {
                ColorConfigMod.DebugException("Failed to initialize jolly-coop menu hooks", ex);
            }
        }
        public static void On_ColorChangeDialog_ColorSlider_ctor(On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_ctor orig, ColorChangeDialog.ColorSlider self, Menu.Menu menu, MenuObject owner, Vector2 pos, int playerNumber, int bodyPart, string sliderTitle)
        {
            orig(self, menu, owner, pos, playerNumber, bodyPart, sliderTitle);
            self.GetExtraJollyInterface(/*bodyPart,*/ ModOptions.ShouldRemoveHSLSliders || ModOptions.FollowLukkyRGBSliders, ModOptions.Instance.EnableHexCodeTypers.Value);
        }
        public static float On_ColorChangeDialog_ValueOfSlider(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_ValueOfSlider orig, ColorChangeDialog self, Slider slider)
        {
            if (ValueOfCustomSliders(slider, out float f))
                return f;
            return orig(self, slider);
        }
        public static void On_ColorChangeDialog_SliderSetValue(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_SliderSetValue orig, ColorChangeDialog self, Slider slider, float f)
        {
            CustomSliderSetValue(slider, f);
            orig(self, slider, f);
        }
        public static void CustomSliderSetValue(Slider slider, float f)
        {
            if (slider?.ID != null && slider.owner is ColorChangeDialog.ColorSlider colSlider)
            {
                if (MenuToolObj.RGBSliderIDS.Contains(slider.ID))
                {
                    Color color = colSlider.color;
                    color[MenuToolObj.RGBSliderIDS.IndexOf(slider.ID)] = f;
                    colSlider.color = SmallUtils.RWIIIClamp(color.RGB2Vector3(), CustomColorModel.RGB, out Vector3 newHSL).Vector32RGB();
                    colSlider.hslColor = SmallUtils.FixNonHueSliderWonkiness(newHSL, colSlider.hslColor.HSL2Vector3()).Vector32HSL();
                }
                if (MenuToolObj.HSVSliderIDS.Contains(slider.ID))
                {
                    Vector3 hsv = ColConversions.HSL2HSV(colSlider.hslColor.HSL2Vector3());
                    hsv[MenuToolObj.HSVSliderIDS.IndexOf(slider.ID)] = f;
                    SmallUtils.RWIIIClamp(hsv, CustomColorModel.HSV, out Vector3 newHSL);
                    colSlider.hslColor = newHSL.Vector32HSL();
                    colSlider.HSL2RGB();
                }
            }
            /*if (slider?.ID?.value != null && slider.ID.value.StartsWith("DUSTY") && slider.ID.value.Contains('_') && slider.owner is ColorChangeDialog.ColorSlider colSlider)
            {
                string[] array = slider.ID.value.Split('_');
                if (array.Length > 3)
                {
                    if (array[2] == "RGB" && MenuToolObj.RGBNames.Contains(array[3]))
                    {
                        Color color = colSlider.color;
                        color[MenuToolObj.RGBNames.FindIndex(array[3])] = f;
                        colSlider.color = SmallUtils.RWIIIClamp(color.RGB2Vector3(), CustomColorModel.RGB, out Vector3 newHSL).Vector32RGB();
                        colSlider.hslColor = SmallUtils.FixNonHueSliderWonkiness(newHSL, colSlider.hslColor.HSL2Vector3()).Vector32HSL();
                    }
                    if (array[2] == "HSV" && MenuToolObj.HSVNames.Contains(array[3]))
                    {
                        Vector3 hsv = ColConversions.HSL2HSV(colSlider.hslColor.HSL2Vector3());
                        hsv[MenuToolObj.HSVNames.FindIndex(array[3])] = f;
                        SmallUtils.RWIIIClamp(hsv, CustomColorModel.HSV, out Vector3 newHSL);
                        colSlider.hslColor = newHSL.Vector32HSL();
                        colSlider.HSL2RGB();
                    }
                }
            }*/
        }
        public static bool ValueOfCustomSliders(Slider slider, out float f)
        {
            f = -1;
            if (slider?.ID != null && slider.owner is ColorChangeDialog.ColorSlider colSlider)
            {
                if (MenuToolObj.RGBSliderIDS.Contains(slider.ID))
                    f = colSlider.color[MenuToolObj.RGBSliderIDS.IndexOf(slider.ID)];
                if (MenuToolObj.HSVSliderIDS.Contains(slider.ID))
                    f = ColConversions.HSL2HSV(colSlider.hslColor.HSL2Vector3())[MenuToolObj.HSVSliderIDS.IndexOf(slider.ID)];
            }
            /*if (slider?.ID?.value != null && slider.ID.value.StartsWith("DUSTY") && slider.ID.value.Contains('_') && slider.owner is ColorChangeDialog.ColorSlider colSlider)
            {
                string[] array = slider.ID.value.Split('_');
                if (array.Length > 3)
                {
                    if (array[2] == "RGB" && MenuToolObj.RGBNames.Contains(array[3]))
                    {
                        f = colSlider.color[MenuToolObj.RGBNames.FindIndex(array[3])];
                        return true;
                    }
                    else if (array[2] == "HSV" && MenuToolObj.HSVNames.Contains(array[3]))
                    {
                        Vector3 hsv = ColConversions.HSL2HSV(colSlider.hslColor.HSL2Vector3());
                        f = hsv[MenuToolObj.HSVNames.FindIndex(array[3])];
                        return true;
                    }
                }

            }*/
            return f >= 0;
        }
    }
}
