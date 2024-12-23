using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Menu;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour;
using RWCustom;

namespace ColorConfig
{
    public static class ModHooks
    {
        public static class LukkyRGBModHooks
        {
            public static void ApplyLukkyModHooks()
            {
                ColorConfigMod.DebugLog("Initialising Lukky RGB Slider Hooks");
                Hook On_LukkyRGBSlider_ColSlider_RGB2HSLHook =
                    new(MethodBase.GetMethodFromHandle(typeof(LukkyMod.Main).GetMethod("ColorSlider_RGB2HSL",
                    BindingFlags.Instance | BindingFlags.NonPublic).MethodHandle), On_LukkyRGBSlider_ColorSlider_RGB2HSL);
                Hook On_LukkyRGBSlider_ColSlider_HSL2RGBHook =
                   new(MethodBase.GetMethodFromHandle(typeof(LukkyMod.Main).GetMethod("ColorSlider_HSL2RGB",
                   BindingFlags.Instance | BindingFlags.NonPublic).MethodHandle), On_LukkyRGBSlider_ColorSlider_HSL2RGB);
                ColorConfigMod.DebugLog("Sucessfully Initialised Lukky RGB Slider Hooks");
            }
            public static void On_LukkyRGBSlider_ColorSlider_RGB2HSL(LukkyRGBSlider_ColorSlider_RGB2HSL orig, LukkyMod.Main lukkyRGBSlider, On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_RGB2HSL origCode, JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider ccSlider)
            {
                origCode(ccSlider);
            }
            public static void On_LukkyRGBSlider_ColorSlider_HSL2RGB(LukkyRGBSlider_ColorSlider_HSL2RGB orig, LukkyMod.Main lukkyRGBSlider, On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_HSL2RGB origCode, JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider ccSlider)
            {
                origCode(ccSlider);
            }

            public delegate void LukkyRGBSlider_ColorSlider_HSL2RGB(LukkyMod.Main lukkyRGBSlider, On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_HSL2RGB orig, JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider ccSlider);
            public delegate void LukkyRGBSlider_ColorSlider_RGB2HSL(LukkyMod.Main lukkyRGBSlider, On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_RGB2HSL orig, JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider ccSlider);
        }
        public static class RainMeadowHooks
        {
            public static void ApplyRainMeadowHooks()
            {
                ColorConfigMod.DebugLog("Initialising Rain Meadow Hooks");
                ILHook ON_RainMeadow_ssM_SliderSetValue = new(MethodBase.GetMethodFromHandle(typeof(RainMeadow.RainMeadow).
                    GetMethod("SlugcatSelectMenu_SliderSetValue", BindingFlags.Instance | BindingFlags.NonPublic).MethodHandle),
                    IL_RainMeadow_SSM_SliderSetValue);

                Hook ON_RainMeadow_ssM_CCI_Ctor = new(MethodBase.GetMethodFromHandle(typeof(RainMeadow.RainMeadow).
                    GetMethod("CustomColorInterface_ctor", BindingFlags.Instance | BindingFlags.NonPublic).MethodHandle),
                    ON_RainMeadow_SSM_CCI_Ctor);
                ColorConfigMod.DebugLog("Sucessfully Initialised Rain Meadow Hooks");
            }
            public static void IL_RainMeadow_SSM_SliderSetValue(ILContext il)
            {
                ILCursor cursor = new(il);
                if (!cursor.TryGotoNext(MoveType.After, x => x.MatchCallvirt<MenuIllustration>("set_color")))
                {
                    ColorConfigMod.DebugError("Failed to find desired Rain Meadow Slider Set Value");
                    return;
                }
                try
                {
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldarg_2);
                    cursor.EmitDelegate(new Action<RainMeadow.RainMeadow, SlugcatSelectMenu>((rainMeadow, ssM) =>
                    {
                        if (ssM != null)
                        {
                            if (ssM.hueSlider != null && ssM.satSlider != null && ssM.litSlider != null)
                            {
                                if (ssM.hueSlider.ID == MenuToolObj.RedRGB && ssM.satSlider.ID == MenuToolObj.GreenRGB && ssM.litSlider.ID == MenuToolObj.BlueRGB)
                                {
                                    ssM.colorInterface.bodyColors[ssM.activeColorChooser].color = new(ssM.hueSlider.floatValue, ssM.satSlider.floatValue, ssM.litSlider.floatValue);
                                }
                                else if (ssM.hueSlider.ID == MenuToolObj.HueHSV && ssM.satSlider.ID == MenuToolObj.SatHSV && ssM.litSlider.ID == MenuToolObj.ValHSV)
                                {
                                    ssM.colorInterface.bodyColors[ssM.activeColorChooser].color = ColConversions.HSV2RGB(new(ssM.hueSlider.floatValue, ssM.satSlider.floatValue, ssM.litSlider.floatValue));
                                }
                            }
                        }
                    }));
                    ColorConfigMod.DebugLog("Sucessfully patched desired Rain Meadow Slider Set Value");
                }
                catch (Exception ex)
                {
                    ColorConfigMod.DebugException("Failed to patch desired Rain Meadow Slider Set Value ", ex);
                }

            }
            public static void ON_RainMeadow_SSM_CCI_Ctor(RainMeadow_SSMCCI_Ctor orig, RainMeadow.RainMeadow rainMeadow, On.Menu.SlugcatSelectMenu.CustomColorInterface.orig_ctor origCode, SlugcatSelectMenu.CustomColorInterface ssM_CCI, Menu.Menu menu, MenuObject owner, Vector2 pos, SlugcatStats.Name slugcatID, List<string> names, List<string> defaultColors)
            {
                orig(rainMeadow, origCode, ssM_CCI, menu, owner, pos, slugcatID, names, defaultColors);
                if (RainMeadow.RainMeadow.isStoryMode(out _) && menu is SlugcatSelectMenu ssM)
                {
                    ssM.SaveHSLString(0, SmallUtils.SetHSLSaveString(Custom.RGB2HSL(RainMeadow.RainMeadow.rainMeadowOptions.BodyColor.Value)));
                    ssM.SaveHSLString(1, SmallUtils.SetHSLSaveString(Custom.RGB2HSL(RainMeadow.RainMeadow.rainMeadowOptions.EyeColor.Value)));
                }
            }

            public delegate void RainMeadow_SSMCCI_Ctor(RainMeadow.RainMeadow rainMeadow, On.Menu.SlugcatSelectMenu.CustomColorInterface.orig_ctor orig, SlugcatSelectMenu.CustomColorInterface self, Menu.Menu menu, MenuObject owner, Vector2 pos, SlugcatStats.Name slugcatID, List<string> names, List<string> defaultColors);
        }
    
       
    }
}
