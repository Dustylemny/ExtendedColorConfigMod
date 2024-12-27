using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JollyCoop.JollyMenu;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using System.Drawing;
namespace ColorConfig
{
    public static class ColorConfigHooks
    {
        public class SlugcatSelectMenuHooks
        {
            public void Init()
            {
                try
                {
                    On.Menu.SlugcatSelectMenu.AddColorInterface += On_SlugcatSelectMenu_AddColorInterface;
                    On.Menu.SlugcatSelectMenu.RemoveColorInterface += On_SlugcatSelectMenu_RemoveColorInterface;
                    On.Menu.SlugcatSelectMenu.Update += On_SlugcatSelectMenu_Update;
                    On.Menu.SlugcatSelectMenu.ValueOfSlider += On_SlugcatSelectMenu_ValueOfSlider;
                    On.Menu.SlugcatSelectMenu.SliderSetValue += On_SlugcatSelectMenu_SliderSetValue;
                    if (ColorConfigMod.IsRainMeadowOn)
                    {
                        RainMeadowHooks.ApplyRainMeadowHooks();
                    }
                    ColorConfigMod.DebugLog("Sucessfully extended color interface for slugcat select menu!");
                }
                catch (Exception ex)
                {
                    ColorConfigMod.DebugException("Failed to initalise hooks for SlugcatSelectMenu color interface!", ex);
                }
            }
            public void On_SlugcatSelectMenu_AddColorInterface(On.Menu.SlugcatSelectMenu.orig_AddColorInterface orig, SlugcatSelectMenu self)
            {
                orig(self);
                if (oOOPages == null)
                {
                    oOOPages = new(self, self.pages[0], self.hueSlider, self.satSlider, self.litSlider, SliderIDGroups, ModOptions.nextPageKeyBind.Value, ModOptions.prevPageKeyBind.Value, new(0, 25))
                    {
                        showValues = ModOptions.ShowVisual
                    };
                    self.pages[0].subObjects.Add(oOOPages);
                    oOOPages.PopulatePage(oOOPages.CurrentOffset);
                    if (oOOPages.PagesOn)
                    {
                        self.defaultColorButton.pos.y -= 40;
                        self.MutualVerticalButtonBind(self.defaultColorButton, oOOPages.PrevButton);
                        self.MutualVerticalButtonBind(oOOPages.NextButton, self.litSlider);
                    }
                }
                if (ModOptions.enableSlugcatDisplay.Value)
                {
                    if (slugcatDisplay == null)
                    {
                        slugcatDisplay = new(self, self.pages[0], new(self.satSlider.pos.x + 140, self.satSlider.pos.y + 80), new(45f, 45f),
                            self.slugcatColorOrder[self.slugcatPageIndex]);
                        self.pages[0].subObjects.Add(slugcatDisplay);
                    }
                }
                if (ModOptions.enableHexCodeTypers.Value)
                {
                    if (hexInterface == null)
                    {
                        hexInterface = new(self, self.pages[0], self.defaultColorButton.pos + new Vector2(120, 0));
                        self.pages[0].subObjects.Add(hexInterface);
                    }
                }
            }
            public void On_SlugcatSelectMenu_RemoveColorInterface(On.Menu.SlugcatSelectMenu.orig_RemoveColorInterface orig, SlugcatSelectMenu self)
            {
                orig(self);
                if (oOOPages != null)
                {
                    oOOPages.RemoveSprites();
                    self.pages[0].RemoveSubObject(oOOPages);
                    oOOPages = null;
                }
                if (slugcatDisplay != null)
                {
                    slugcatDisplay.RemoveSprites();
                    self.pages[0].RemoveSubObject(slugcatDisplay);
                    slugcatDisplay = null;
                }
                if (hexInterface != null)
                {
                    hexInterface.RemoveSprites();
                    self.pages[0].RemoveSubObject(hexInterface);
                    hexInterface = null;
                }
            }
            public void On_SlugcatSelectMenu_Update(On.Menu.SlugcatSelectMenu.orig_Update orig, SlugcatSelectMenu self)
            {
                orig(self);
                if (hexInterface != null)
                {
                    hexInterface.SaveNewHSL(SmallUtils.SlugcatSelectMenuHSL(self));
                    if (hexInterface.shouldUpdateNewColor)
                    {
                        hexInterface.shouldUpdateNewColor = false;
                        self.SaveHSLString(SmallUtils.SetHSLSaveString(hexInterface.newPendingHSL));
                        self.SliderSetValue(self.hueSlider, self.ValueOfSlider(self.hueSlider));
                        self.SliderSetValue(self.satSlider, self.ValueOfSlider(self.satSlider));
                        self.SliderSetValue(self.litSlider, self.ValueOfSlider(self.litSlider));

                    }
                }
                slugcatDisplay?.LoadNewHSLStringSlugcat(self.manager.rainWorld.progression.miscProgressionData.colorChoices[self.slugcatColorOrder[self.slugcatPageIndex].value], self.slugcatColorOrder[self.slugcatPageIndex]);
            }
            public float On_SlugcatSelectMenu_ValueOfSlider(On.Menu.SlugcatSelectMenu.orig_ValueOfSlider orig, SlugcatSelectMenu self, Slider slider)
            {
                if (ValueOfCustomSliders(self, slider, out float f))
                {
                    return f;
                }
                return orig(self, slider);
            }
            public void On_SlugcatSelectMenu_SliderSetValue(On.Menu.SlugcatSelectMenu.orig_SliderSetValue orig, SlugcatSelectMenu self, Slider slider, float f)
            {
                CustomSlidersSetValue(self, slider, f);
                orig(self, slider, f);
            }
            public void CustomSlidersSetValue(SlugcatSelectMenu ssM, Slider slider, float f)
            {
                if (MenuToolObj.RGBSliderIDS.Contains(slider?.ID))
                {
                    Vector3 ssMHSL = ssM.SlugcatSelectMenuHSL();
                    Color color = ColConversions.HSL2RGB(ssMHSL);
                    color = slider.ID == MenuToolObj.RedRGB ? new(f, color.g, color.b) : slider.ID == MenuToolObj.GreenRGB ? new(color.r, f, color.b) : new(color.r, color.g, f);
                    SmallUtils.Vector32RGB(SmallUtils.RWIIIClamp(SmallUtils.RGB2Vector3(SmallUtils.RGBClamp01(color)), CustomColorModel.RGB, out Vector3 hsl));
                    ssM.SaveHSLString(SmallUtils.SetHSLSaveString(SmallUtils.SliderWonkiness.FixConvHueSliderWonkiness(hsl, ssMHSL)));

                }
                if (MenuToolObj.HSVSliderIDS.Contains(slider?.ID))
                {
                    Vector3 hsv = ColConversions.HSL2HSV(ssM.SlugcatSelectMenuHSL());
                    hsv = slider.ID == MenuToolObj.HueHSV ? new(Mathf.Clamp(f, 0, 0.99f), hsv.y, hsv.z) : slider.ID == MenuToolObj.SatHSV ? new(hsv.x, f, hsv.z) : new(hsv.x, hsv.y, f);
                    SmallUtils.RWIIIClamp(hsv, CustomColorModel.HSV, out Vector3 newHSL);
                    ssM.SaveHSLString(SmallUtils.SetHSLSaveString(newHSL));
                }
            }
            public bool ValueOfCustomSliders(SlugcatSelectMenu ssM, Slider slider, out float f)
            {
                f = 0;
                if (MenuToolObj.RGBSliderIDS.Contains(slider?.ID))
                {
                    Color color = ColConversions.HSL2RGB(ssM.SlugcatSelectMenuHSL());
                    f = slider.ID == MenuToolObj.RedRGB ? color.r : slider.ID == MenuToolObj.GreenRGB ? color.g : color.b;
                    return true;
                }
                if (MenuToolObj.HSVSliderIDS.Contains(slider?.ID))
                {
                    Vector3 hsv = ColConversions.HSL2HSV(ssM.SlugcatSelectMenuHSL());
                    f = slider.ID == MenuToolObj.HueHSV ? hsv.x : slider.ID == MenuToolObj.SatHSV ? hsv.y : hsv.z;
                    return true;
                }
                return false;
            }
            public List<NewMenuInterfaces.SliderOOOIDGroup> SliderIDGroups
            {
                get
                {
                    List<NewMenuInterfaces.SliderOOOIDGroup> IDGroups = new();
                    if (!ModOptions.RemoveHSLSliders)
                    {
                        IDGroups.Add(new(MMFEnums.SliderID.Hue, MMFEnums.SliderID.Saturation, MMFEnums.SliderID.Lightness, MenuToolObj.HSLNames,
                            MenuToolObj.hueOOShowInt, MenuToolObj.hueOOMultipler));
                    }
                    if (ModOptions.enableRGBSliders.Value)
                    {
                        IDGroups.Add(new(MenuToolObj.RedRGB, MenuToolObj.GreenRGB, MenuToolObj.BlueRGB,
                            MenuToolObj.RGBNames, MenuToolObj.rgbShowInt, MenuToolObj.rgbMultipler));
                    }
                    if (ModOptions.enableHSVSliders.Value)
                    {
                        IDGroups.Add(new(MenuToolObj.HueHSV, MenuToolObj.SatHSV, MenuToolObj.ValHSV,
                           MenuToolObj.HSVNames, MenuToolObj.hueOOShowInt, MenuToolObj.hueOOMultipler));
                    }
                    return IDGroups;
                }
            }
            public NewMenuInterfaces.HexTypeBox hexInterface;
            public NewMenuInterfaces.SlugcatDisplay slugcatDisplay;
            public NewMenuInterfaces.SliderOOOPages oOOPages;

            public class RainMeadowHooks
            {
                public static void ApplyRainMeadowHooks()
                {
                    ColorConfigMod.DebugLog("Initialising Rain Meadow Hooks");
                    ILHook ON_RainMeadow_ssM_SliderSetValue = new(MethodBase.GetMethodFromHandle(typeof(RainMeadow.RainMeadow).
                        GetMethod("SlugcatSelectMenu_SliderSetValue", BindingFlags.Instance | BindingFlags.NonPublic).MethodHandle),
                        IL_RainMeadow_SSM_SliderSetValue);

                    Hook ON_RainMeadow_ssM_CCI_Ctor = new(MethodBase.GetMethodFromHandle(typeof(RainMeadow.RainMeadow).
                        GetMethod("CustomColorInterface_ctor", BindingFlags.Instance | BindingFlags.NonPublic).MethodHandle),
                        new Action<Action<RainMeadow.RainMeadow, On.Menu.SlugcatSelectMenu.CustomColorInterface.orig_ctor, SlugcatSelectMenu.CustomColorInterface,
                        Menu.Menu, MenuObject, Vector2, SlugcatStats.Name, List<string>, List<string>>,
                        RainMeadow.RainMeadow, On.Menu.SlugcatSelectMenu.CustomColorInterface.orig_ctor, SlugcatSelectMenu.CustomColorInterface,
                        Menu.Menu, MenuObject, Vector2, SlugcatStats.Name, List<string>, List<string>>((orig, rainMeadow, origCode, ssM_CCI, menu, owner, pos, slugcatID, names, defaultColors) =>
                        {
                            orig(rainMeadow, origCode, ssM_CCI, menu, owner, pos, slugcatID, names, defaultColors);
                            if (RainMeadow.RainMeadow.isStoryMode(out _) && menu is SlugcatSelectMenu ssM)
                            {
                                ssM.SaveHSLString(0, SmallUtils.SetHSLSaveString(Custom.RGB2HSL(RainMeadow.RainMeadow.rainMeadowOptions.BodyColor.Value)));
                                ssM.SaveHSLString(1, SmallUtils.SetHSLSaveString(Custom.RGB2HSL(RainMeadow.RainMeadow.rainMeadowOptions.EyeColor.Value)));
                            }
                        }));
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
            }
        }
        public class JollyCoopConfigHooks
        {
            public void Init()
            {
                try
                {
                    On.JollyCoop.JollyMenu.ColorChangeDialog.ValueOfSlider += OnDialog_ValueOfSlider;
                    On.JollyCoop.JollyMenu.ColorChangeDialog.SliderSetValue += OnDialog_SliderSetValue;
                    On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.ctor += On_ColorSliderctor;
                    On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.RemoveSprites += On_ColorSliderRemoveSprites;
                    if (ColorConfigMod.IsLukkyRGBColorSliderModOn)
                    {
                        LukkyRGBModHooks.ApplyLukkyModHooks();
                    }
                    ColorConfigMod.DebugLog("Sucessfully extended color interface for jolly coop menu!");
                }
                catch (Exception ex)
                {
                    ColorConfigMod.DebugException("Failed to initalise hooks for JollyMenu color interface!", ex);
                }
            }
            public void On_ColorSliderctor(On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_ctor orig, ColorChangeDialog.ColorSlider self, Menu.Menu menu, MenuObject owner, Vector2 pos, int playerNumber, int bodyPart, string sliderTitle)
            {
                orig(self, menu, owner, pos, playerNumber, bodyPart, sliderTitle);
                if (!colSliders.Contains(self))
                {
                    colSliders.Add(self);
                }
                if (!configPages.ContainsKey(self))
                {
                    NewMenuInterfaces.JollyCoopOOOConfigPages jollyCoopOOOConfigPages = new(menu, self, bodyPart);
                    configPages.Add(self, jollyCoopOOOConfigPages);
                    self.subObjects.Add(jollyCoopOOOConfigPages);

                }
            }
            public void On_ColorSliderRemoveSprites(On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_RemoveSprites orig, ColorChangeDialog.ColorSlider self)
            {
                orig(self);
                if (colSliders.Contains(self))
                {
                    colSliders.Remove(self);
                }
                if (configPages.ContainsKey(self))
                {
                    configPages[self].RemoveSprites();
                    self.RemoveSubObject(configPages[self]);
                    configPages.Remove(self);
                }
            }
            public float OnDialog_ValueOfSlider(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_ValueOfSlider orig, ColorChangeDialog self, Slider slider)
            {
                if (ValueOfCustomSliders(slider, out float f))
                {
                    return f;
                }
                return orig(self, slider);
            }
            public void OnDialog_SliderSetValue(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_SliderSetValue orig, ColorChangeDialog self, Slider slider, float f)
            {
                CustomSliderSetValue(slider, f);
                orig(self, slider, f);
            }
            public void CustomSliderSetValue(Slider slider, float f)
            {
                if (slider?.ID?.value != null)
                {
                    if (slider.ID.value.StartsWith("DUSTY") && slider.ID.value.Contains('_'))
                    {
                        string[] array = slider.ID.value.Split('_');
                        if (int.TryParse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture, out int colSliderNum) && array.Length > 3 &&
                            colSliders.Count > colSliderNum &&
                            colSliders[colSliderNum] != null)
                        {
                            if (array[2] == "RGB" && MenuToolObj.RGBNames.Contains(array[3]))
                            {
                                Color color = colSliders[colSliderNum].color;
                                color = array[3] == "RED" ? new(f, color.g, color.b) : array[3] == "GREEN" ? new(color.r, f, color.b) : new(color.r, color.g, f);
                                if (color != colSliders[colSliderNum].color)
                                {
                                    colSliders[colSliderNum].color = SmallUtils.RWIIIClamp(color.RGB2Vector3(), CustomColorModel.RGB, out Vector3 newHSL).Vector32RGB();
                                    colSliders[colSliderNum].hslColor = SmallUtils.SliderWonkiness.FixConvHueSliderWonkiness(newHSL, colSliders[colSliderNum].hslColor.HSL2Vector3()).Vector32HSL();
                                }
                            }
                            else if (array[2] == "HSV" && MenuToolObj.HSVNames.Contains(array[3]))
                            {
                                Vector3 hsv = ColConversions.HSL2HSV(colSliders[colSliderNum].hslColor.HSL2Vector3());
                                Vector3 oldHSV = hsv;
                                hsv = array[3] == "HUE" ? new(Mathf.Clamp(f, 0, 0.99f), hsv.y, hsv.z) : array[3] == "SAT" ? new(hsv.x, f, hsv.z) : new(hsv.x, hsv.y, f);
                                if (oldHSV != SmallUtils.RWIIIClamp(hsv, CustomColorModel.HSV, out Vector3 newHSL))
                                {
                                    colSliders[colSliderNum].hslColor = newHSL.Vector32HSL();
                                    colSliders[colSliderNum].HSL2RGB();
                                }
                            }

                        }
                    }
                }
            }
            public bool ValueOfCustomSliders(Slider slider, out float f)
            {
                f = 0;
                if (slider?.ID?.value != null)
                {
                    if (slider.ID.value.StartsWith("DUSTY") && slider.ID.value.Contains('_'))
                    {
                        string[] array = slider.ID.value.Split('_');
                        if (int.TryParse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture, out int colSliderNum) && array.Length > 3 &&
                            colSliders.Count > colSliderNum &&
                            colSliders[colSliderNum] != null)
                        {
                            if (array[2] == "RGB" && MenuToolObj.RGBNames.Contains(array[3]))
                            {
                                f = array[3] == "RED" ? colSliders[colSliderNum].color.r : array[3] == "GREEN" ? colSliders[colSliderNum].color.g : colSliders[colSliderNum].color.b;
                                return true;
                            }
                            else if (array[2] == "HSV" && MenuToolObj.HSVNames.Contains(array[3]))
                            {
                                Vector3 hsv = ColConversions.HSL2HSV(colSliders[colSliderNum].hslColor.HSL2Vector3());
                                f = array[3] == "HUE" ? hsv.x : array[3] == "SAT" ? hsv.y : hsv.z;
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            private readonly Dictionary<ColorChangeDialog.ColorSlider, NewMenuInterfaces.JollyCoopOOOConfigPages> configPages = new();
            private readonly List<ColorChangeDialog.ColorSlider> colSliders = new();

            public class LukkyRGBModHooks
            {
                public static void ApplyLukkyModHooks()
                {
                    ColorConfigMod.DebugLog("Initialising Lukky RGB Slider Hooks");
                    Hook On_LukkyRGBSlider_ColSlider_RGB2HSLHook =
                        new(MethodBase.GetMethodFromHandle(typeof(LukkyMod.Main).GetMethod("ColorSlider_RGB2HSL",
                        BindingFlags.Instance | BindingFlags.NonPublic).MethodHandle), new Action<Action<LukkyMod.Main, On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_RGB2HSL,
                        ColorChangeDialog.ColorSlider>, LukkyMod.Main, On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_RGB2HSL,
                        ColorChangeDialog.ColorSlider>((orig, lukkyRGBSlider, origCode, ccSlider) =>
                        {
                            origCode(ccSlider);
                        }));
                    Hook On_LukkyRGBSlider_ColSlider_HSL2RGBHook =
                       new(MethodBase.GetMethodFromHandle(typeof(LukkyMod.Main).GetMethod("ColorSlider_HSL2RGB",
                       BindingFlags.Instance | BindingFlags.NonPublic).MethodHandle), new Action<Action<LukkyMod.Main, On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_HSL2RGB,
                       ColorChangeDialog.ColorSlider>, LukkyMod.Main, On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_HSL2RGB,
                       ColorChangeDialog.ColorSlider>((orig, lukkyRGBSlider, origCode, ccSlider) =>
                       {
                           origCode(ccSlider);
                       }));
                    ColorConfigMod.DebugLog("Sucessfully Initialised Lukky RGB Slider Hooks");
                }
            }
        }
        public class OpColorPickerHooks
        {
            public void Init()
            {
                try
                {
                    OtherOpColorPickerHooks();
                    IL.Menu.Remix.MixedUI.OpColorPicker.Change += IL_OPColorPicker_Change;
                    IL.Menu.Remix.MixedUI.OpColorPicker._HSLSetValue += IL_OPColorPicker__HSLSetValue;
                    IL.Menu.Remix.MixedUI.OpColorPicker.MouseModeUpdate += IL_OPColorPicker_MouseModeUpdate;
                    IL.Menu.Remix.MixedUI.OpColorPicker.GrafUpdate += IL_OPColorPicker_GrafUpdate;
                    On.Menu.Remix.MixedUI.OpColorPicker.ctor += On_OPColorPickerCtor;
                    On.Menu.Remix.MixedUI.OpColorPicker._RecalculateTexture += On_OPColorPickerRecalculateTexture;
                    On.Menu.Remix.MixedUI.OpColorPicker._HSLSetValue += On_OPColorPicker__HSLSetValue;
                    On.Menu.Remix.MixedUI.OpColorPicker.Update += On_OPColorPickerUpdate;

                    ColorConfigMod.DebugLog("Successfully extended color interface for OpColorPicker!");
                }
                catch (Exception ex)
                {
                    ColorConfigMod.DebugException("Failed to initalise OpColorPicker hooks for OpColorPicker!", ex);
                }
            }
            private void OtherOpColorPickerHooks()
            {
                ILHook iLHook = new(MethodBase.GetMethodFromHandle(typeof(OpColorPicker).
                   GetProperty(nameof(OpColorPicker.@value)).GetSetMethod().MethodHandle),
                   IL_OPColorPicker_set_value);
                Hook onHook = new(MethodBase.GetMethodFromHandle(typeof(OpColorPicker).
                   GetProperty(nameof(OpColorPicker.@value)).GetSetMethod().MethodHandle),
                   new Action<Action<OpColorPicker, string>, OpColorPicker, string>((orig, self, newValue) =>
                   {
                       if (self.value == newValue && ModOptions.enableBetterOPColorPicker.Value &&
                          ((ModOptions.enableDifferentOpColorPickerHSLPos.Value &&
                          (self._curFocus == OpColorPicker.MiniFocus.HSL_Lightness || self._curFocus == OpColorPicker.MiniFocus.HSL_Saturation)) ||
                          self._curFocus == OpColorPicker.MiniFocus.HSL_Hue || self._curFocus == OpColorPicker.MiniFocus.HSL_Saturation))
                       {
                           self._RecalculateTexture();
                           self._ftxr1.SetTexture(self._ttre1);
                           self._ftxr2.SetTexture(self._ttre2);
                       }
                       orig(self, newValue);
                   }));
            }
            private void IL_OPColorPicker_set_value(ILContext iL)
            {
                ILCursor cursor = new(iL);
                if (cursor.TryGotoNext(MoveType.After, (x) => x.MatchDiv(), (x) => x.MatchNewobj<Color>(), (x) => x.MatchCall<RXColor>("HSLFromColor"),
                    (x) => x.MatchStloc(0)))
                {
                    ColorConfigMod.DebugILCursor("IL_OPColorPicker_set_value: ", cursor);
                    try
                    {
                        cursor.Emit(OpCodes.Ldarg_0);
                        cursor.Emit(OpCodes.Ldarg_1);
                        cursor.Emit(OpCodes.Ldloc_0);
                        cursor.EmitDelegate(new Func<OpColorPicker, string, RXColorHSL, RXColorHSL>((self, newValue, hsl) =>
                        {
                            hsl = ModOptions.hsl2HSVOPColorPicker.Value ? ColConversions.HSL2HSV(hsl.RXHSl2Vector3()).Vector32RXHSL() : hsl;
                            if (ModOptions.enableBetterOPColorPicker.Value)
                            {
                                if (self._mode == OpColorPicker.PickerMode.HSL)
                                {
                                    if (ModOptions.enableDifferentOpColorPickerHSLPos.Value && self.MenuMouseMode)
                                    {
                                        hsl.h = self._curFocus == OpColorPicker.MiniFocus.HSL_Saturation || self._curFocus == OpColorPicker.MiniFocus.HSL_Hue ? self._h / 100f : hsl.h;
                                        hsl.s = self._curFocus == OpColorPicker.MiniFocus.HSL_Hue || self._curFocus == OpColorPicker.MiniFocus.HSL_Lightness ? self._s / 100f : hsl.s;
                                    }
                                    else
                                    {
                                        hsl.h = self._curFocus == OpColorPicker.MiniFocus.HSL_Lightness || self._curFocus == OpColorPicker.MiniFocus.HSL_Saturation ? self._h / 100f : hsl.h;
                                        hsl.s = self._curFocus == OpColorPicker.MiniFocus.HSL_Lightness || self._curFocus == OpColorPicker.MiniFocus.HSL_Hue ? self._s / 100f : hsl.s;
                                    }
                                }
                            }
                            return hsl;
                        }));
                        cursor.Emit(OpCodes.Stloc_0);
                        ColorConfigMod.DebugLog("IL_OPColorPicker_set_value: Successfully patched set_value for hsv");
                    }
                    catch (Exception e)
                    {
                        ColorConfigMod.DebugException("IL_OPColorPicker_set_value: Failed to patch set_value for hsv", e);
                    }
                }
                else
                {
                    ColorConfigMod.DebugError("IL_OPColorPicker_set_value: Failed to find after hsl set value");
                }

            }
            private void IL_OPColorPicker_Change(ILContext iL)
            {
                ILCursor cursor = new(iL);
                if (!cursor.TryGotoNext(MoveType.After, (x) => x.MatchCall(typeof(Custom), "HSL2RGB"),
                    (x) => x.MatchCallvirt<FSprite>("set_color")))
                {
                    ColorConfigMod.DebugError("IL_OPColorPicker_Change: Failed to find set HSL Color");
                    return;
                }
                ColorConfigMod.DebugILCursor("IL_OPColorPicker_Change: ", cursor);
                try
                {
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(new Action<OpColorPicker>((self) =>
                    {
                        self._cdis0.color = ModOptions.hsl2HSVOPColorPicker.Value ? ColConversions.HSV2RGB(new(self._h / 100f, self._s / 100f, self._l / 100f)) : self._cdis0.color;
                    }));
                    ColorConfigMod.DebugLog("IL_OPColorPicker_Change: Successfully patched _cdis0 Color");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_OPColorPicker_Change: Failed to patch _cdis0 Color", e);
                }
            }
            private void IL_OPColorPicker__HSLSetValue(ILContext il)
            {
                ILCursor cursor = new(il);
                if (!cursor.TryGotoNext(MoveType.After, (x) => x.MatchCall(typeof(Custom), "HSL2RGB"), (x) => x.MatchStloc(0)))
                {
                    ColorConfigMod.DebugError("IL_OPColorPicker__HSLSetValue: Failed to find set RGB Color");
                    return;
                }
                ColorConfigMod.DebugILCursor("IL_OPColorPicker__HSLSetValue: ", cursor);
                try
                {
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldloc_0);
                    cursor.EmitDelegate(new Func<OpColorPicker, Color, Color>((self, col) =>
                    {
                        return ModOptions.hsl2HSVOPColorPicker.Value ? ColConversions.HSV2RGB(new(self._h / 100f, self._s / 100f, self._l / 100f)) : col;
                    }));
                    cursor.Emit(OpCodes.Stloc_0);
                    ColorConfigMod.DebugLog("IL_OPColorPicker__HSLSetValue: Successfully patched RGB Color");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_OPColorPicker__HSLSetValue: Failed to patch RGB Color", e);
                }
            }
            private void IL_OPColorPicker_MouseModeUpdate(ILContext iL)
            {
                ILCursor cursor = new(iL);
                Func<Instruction, bool>[] PickerModeSwitch = new Func<Instruction, bool>[]
                {
                    (x) => x.MatchLdarg(0),
                    (x) => x.MatchLdfld<OpColorPicker>(nameof(OpColorPicker._mode)),
                    (x) => x.Match(OpCodes.Stloc_S),
                    (x) => x.Match(OpCodes.Ldloc_S),
                    (x) => x.Match(OpCodes.Switch),
                };
                Func<Instruction, bool>[] colorMatch = new Func<Instruction, bool>[]
                {
                    (x) => x.MatchCall(typeof(Custom), "HSL2RGB"),
                    (x) => x.MatchCallvirt(typeof(FSprite).GetMethod("set_color")),

                };
                Func<Instruction, bool>[] visiblityMatch = new Func<Instruction, bool>[]
                {
                    (x) => x.MatchLdarg(0),
                    (x) => x.MatchLdfld<OpColorPicker>(nameof(OpColorPicker._cdis1)),
                    (x) => x.MatchLdcI4(1),
                    (x) => x.MatchCallvirt<FNode>("set_isVisible"),
                };
                try
                {
                    cursor.GotoNext((x) => x.MatchSub(), (x) => x.Match(OpCodes.Switch));
                    cursor.GotoNext(colorMatch);
                    cursor.GotoNext(MoveType.After, visiblityMatch);
                    ColorConfigMod.DebugILCursor("IL_OPColorPickerMouseModeUpdateUpdate: ", cursor);
                    //huesat2SatLittextFix vvvvvv
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(new Func<OpColorPicker, bool>((self) =>
                    {
                        if (ModOptions.enableDifferentOpColorPickerHSLPos.Value)
                        {
                            int sat = Mathf.RoundToInt(Mathf.Clamp(self.MousePos.x - 10f, 0f, 100f));
                            int litVal = Mathf.RoundToInt(Mathf.Clamp(self.MousePos.y - 30f, 0f, 100f));

                            self._lblR.text = self._h.ToString();
                            self._lblG.text = sat.ToString();
                            self._lblB.text = litVal.ToString();
                            self._cdis1.color = ModOptions.hsl2HSVOPColorPicker.Value ?
                            ColConversions.HSV2RGB(new(self._h / 100f, sat / 100f, litVal / 100f)) :
                            ColConversions.HSL2RGB(new(self._h / 100f, sat / 100f, litVal / 100f));
                            if (self._s != sat || self._l != litVal)
                            {
                                self._s = sat;
                                self._l = litVal;
                                self.PlaySound(SoundID.MENU_Scroll_Tick);
                                self._HSLSetValue();
                            }
                            return true;
                        }
                        else if (ModOptions.hsl2HSVOPColorPicker.Value)
                        {
                            int hue = Mathf.RoundToInt(Mathf.Clamp(self.MousePos.x - 10f, 0f, 100f));
                            int sat = Mathf.RoundToInt(Mathf.Clamp(self.MousePos.y - 30f, 0f, 100f));
                            self._cdis1.color = ColConversions.HSV2RGB(new(hue / 100f, sat / 100f, self._l / 100f));
                        }
                        return false;
                    }));
                    cursor.Emit(OpCodes.Brfalse, cursor.Next);
                    cursor.Emit(OpCodes.Ret);
                    ColorConfigMod.DebugLog("IL_OPColorPickerMouseModeUpdate: Sucessfully patched MiniFocus SatHue!");
                    // ^^^^^^
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_OPColorPickerMouseModeUpdate: Failed to patch for MiniFocus SatHue", e);
                }
                try
                {
                    cursor.GotoNext(MoveType.After, visiblityMatch);
                    ColorConfigMod.DebugILCursor("IL_OPColorPickerMouseModeUpdateUpdate: ", cursor);
                    //colorfix vvvvvv
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(new Func<OpColorPicker, bool>((self) =>
                    {
                        if (ModOptions.enableDifferentOpColorPickerHSLPos.Value)
                        {
                            int hue = Mathf.RoundToInt(Mathf.Clamp(self.MousePos.y - 30f, 0f, 99f));
                            self._lblR.text = hue.ToString();
                            self._lblG.text = self._s.ToString();
                            self._lblB.text = self._l.ToString();
                            self._cdis1.color = ModOptions.hsl2HSVOPColorPicker.Value ?
                            ColConversions.HSV2RGB(new(hue / 100f, self._s / 100f, self._l / 100f)) :
                            ColConversions.HSL2RGB(new(hue / 100f, self._s / 100f, self._l / 100f));
                            if (self._h != hue)
                            {
                                self._h = hue;
                                self.PlaySound(SoundID.MENU_Scroll_Tick);
                                self._HSLSetValue();
                            }
                            return true;
                        }
                        else if (ModOptions.hsl2HSVOPColorPicker.Value)
                        {
                            int lit = Mathf.RoundToInt(Mathf.Clamp(self.MousePos.y - 30f, 0f, 99f));
                            self._cdis1.color = ColConversions.HSV2RGB(new(self._h / 100f, self._s / 100f, lit / 100f));
                        }
                        return false;

                    }));
                    cursor.Emit(OpCodes.Brfalse, cursor.Next);
                    cursor.Emit(OpCodes.Ret);
                    ColorConfigMod.DebugLog("IL_OPColorPickerMouseModeUpdate: Sucessfully patched MiniFocus for Lit!");
                    // ^^^^^^
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_OPColorPickerMouseModeUpdate: Failed to patch Minifocus for Lit", e);
                }
                try
                {
                    cursor.GotoNext(MoveType.After, PickerModeSwitch);
                    cursor.GotoNext(MoveType.After, colorMatch);
                    ColorConfigMod.DebugILCursor("IL_OPColorPickerMouseModeUpdateUpdate: ", cursor);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(new Action<OpColorPicker>((self) =>
                    {
                        if (ModOptions.enableDifferentOpColorPickerHSLPos.Value)
                        {
                            int hue = Mathf.RoundToInt(Mathf.Clamp(self.MousePos.y - 30f, 0f, 100f));
                            self._lblR.text = hue.ToString();
                            self._lblB.text = self._l.ToString();
                            self._cdis1.color = ModOptions.hsl2HSVOPColorPicker.Value ? ColConversions.HSV2RGB(new(hue / 100f, self._s / 100f, self._l / 100f)) :
                            ColConversions.HSL2RGB(new(hue / 100f, self._s / 100f, self._l / 100f));
                        }
                        else if (ModOptions.hsl2HSVOPColorPicker.Value)
                        {
                            int lit = Mathf.RoundToInt(Mathf.Clamp(self.MousePos.y - 30f, 0f, 100f));
                            self._cdis1.color = ColConversions.HSV2RGB(new(self._h / 100f, self._s / 100f, lit / 100f));
                        }

                    }));
                    ColorConfigMod.DebugLog("IL_OPColorPickerMouseModeUpdate: Sucessfully patched HoverMouse for Hue!");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_OPColorPickerMouseModeUpdate: Failed to patch HoverMouse for Hue", e);
                }
                try
                {
                    cursor.GotoNext(MoveType.After, colorMatch);
                    ColorConfigMod.DebugILCursor("IL_OPColorPickerMouseModeUpdateUpdate: ", cursor);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(new Action<OpColorPicker>((self) =>
                    {
                        if (ModOptions.enableDifferentOpColorPickerHSLPos.Value)
                        {
                            int sat = Mathf.RoundToInt(self.MousePos.x - 10f);
                            int lit = Mathf.RoundToInt(self.MousePos.y - 30f);
                            self._lblR.text = self._h.ToString();
                            self._lblG.text = sat.ToString();
                            self._lblB.text = lit.ToString();
                            self._cdis1.color = ModOptions.hsl2HSVOPColorPicker.Value ? ColConversions.HSV2RGB(new(self._h / 100f, sat / 100f, lit / 100f)) :
                            ColConversions.HSL2RGB(new(self._h / 100f, sat / 100f, lit / 100f));
                        }
                        else if (ModOptions.hsl2HSVOPColorPicker.Value)
                        {
                            int hue = Mathf.RoundToInt(self.MousePos.x - 10f);
                            int sat = Mathf.RoundToInt(self.MousePos.y - 30f);
                            self._cdis1.color = ColConversions.HSV2RGB(new(hue / 100f, sat / 100f, self._l / 100f));
                        }
                    }));
                    ColorConfigMod.DebugLog("IL_OPColorPickerMouseModeUpdate: Sucessfully patched HoverMouse for Sat and Lit!");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_OPColorPickerMouseModeUpdate: Failed to patch HoverMouse for Sat and Lit", e);
                }

            }
            private void IL_OPColorPicker_GrafUpdate(ILContext iL)
            {
                ILCursor cursor = new(iL);
                try
                {
                    cursor.GotoNext((x) => x.Match(OpCodes.Sub),
                    (x) => x.Match(OpCodes.Switch));
                    //hueSatTextFix
                    cursor.GotoNext((x) => x.MatchLdarg(0), (x) => x.MatchCall<UIelement>("get_MenuMouseMode"),
                        (x) => x.Match(OpCodes.Brfalse_S));
                    cursor.GotoNext(MoveType.After, (x) => x.MatchCallvirt<FLabel>("get_color"), (x) => x.MatchCallvirt<FLabel>("set_color"));
                    ColorConfigMod.DebugILCursor("IL_OPColorPickerGrafUpdate: ", cursor);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(new Action<OpColorPicker>((self) =>
                    {
                        if (ModOptions.enableDifferentOpColorPickerHSLPos.Value)
                        {
                            self._lblB.color = self._lblR.color;
                            self._lblR.color = self.colorText;
                        }
                    }));
                    ColorConfigMod.DebugLog("IL_OPColorPickerGrafUpdate: Successfully patched hue2lit text color");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_OPColorPickerGrafUpdate: Failed to patch hue2lit text color", e);
                }
                try
                {
                    cursor.GotoNext(MoveType.After, (x) => x.MatchNewobj<Vector2>(), (x) => x.MatchCallvirt<GlowGradient>("set_pos"));
                    ColorConfigMod.DebugILCursor("IL_OPColorPickerGrafUpdate: ", cursor);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(new Action<OpColorPicker>((self) =>
                    {
                        if (ModOptions.enableDifferentOpColorPickerHSLPos.Value && self.MenuMouseMode)
                        {
                            self._focusGlow.pos = new Vector2(104f, 25f);
                        }
                    }));
                    ColorConfigMod.DebugLog("IL_OPColorPickerGrafUpdate: Successfully patched focus glow for hue2lit text");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_OPColorPickerGrafUpdate: Failed to fix focus glow for hue2lit text", e);
                }
                try
                {
                    cursor.GotoNext((x) => x.MatchLdarg(0), (x) => x.MatchLdfld<OpColorPicker>(nameof(OpColorPicker._lblB)),
                     (x) => x.MatchLdarg(0), (x) => x.MatchLdfld<OpColorPicker>(nameof(OpColorPicker.colorText)));
                    cursor.GotoNext((x) => x.MatchLdarg(0), (x) => x.MatchLdfld<OpColorPicker>(nameof(OpColorPicker._focusGlow)));
                    ColorConfigMod.DebugILCursor("IL_OPColorPickerGrafUpdate: ", cursor);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(new Action<OpColorPicker>((self) =>
                    {
                        if (ModOptions.enableDifferentOpColorPickerHSLPos.Value && self.MenuMouseMode)
                        {
                            self._lblR.color = self._lblB.color;
                            self._lblB.color = self.colorText;
                        }
                    }));
                    ColorConfigMod.DebugLog("IL_OPColorPickerGrafUpdate: Successfully patched lit2hue text color");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_OPColorPickerGrafUpdate: Failed to fix lit2hue text color", e);
                }
                try
                {
                    cursor.GotoNext(MoveType.After, (x) => x.MatchNewobj<Vector2>(), (x) => x.MatchCallvirt<GlowGradient>("set_pos"));
                    ColorConfigMod.DebugILCursor("IL_OPColorPickerGrafUpdate: ", cursor);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(new Action<OpColorPicker>((self) =>
                    {
                        if (ModOptions.enableDifferentOpColorPickerHSLPos.Value && self.MenuMouseMode)
                        {
                            self._focusGlow.pos = new Vector2(104f, 105f);
                        }
                    }));
                    ColorConfigMod.DebugLog("IL_OPColorPickerGrafUpdate: Successfully patched focus glow for lit2hue text");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_OPColorPickerGrafUpdate: Failed to fix focus glow for lit2hue text", e);
                }
            }
            private void On_OPColorPickerCtor(On.Menu.Remix.MixedUI.OpColorPicker.orig_ctor orig, OpColorPicker self, Configurable<Color> config, Vector2 pos)
            {
                orig(self, config, pos);
                self._lblHSL.text = ModOptions.hsl2HSVOPColorPicker.Value ? "HSV" : self._lblHSL.text;
            }
            private void On_OPColorPickerRecalculateTexture(On.Menu.Remix.MixedUI.OpColorPicker.orig__RecalculateTexture orig, OpColorPicker self)
            {
                if (self._mode == OpColorPicker.PickerMode.HSL && (ModOptions.enableDifferentOpColorPickerHSLPos.Value || ModOptions.hsl2HSVOPColorPicker.Value))
                {
                    self._ttre1 = new Texture2D(ModOptions.enableDifferentOpColorPickerHSLPos.Value ? 101 : 100, 101)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Point
                    };
                    self._ttre2 = new Texture2D(10, 101)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Point
                    };
                    for (int height = 0; height <= 100; height++)
                    {
                        for (int sqrWidth = 0; sqrWidth < self._ttre1.width; sqrWidth++)
                        {
                            if (ModOptions.enableDifferentOpColorPickerHSLPos.Value)
                            {
                                self._ttre1.SetPixel(sqrWidth, height, ModOptions.hsl2HSVOPColorPicker.Value ? ColConversions.HSV2RGB(new(self._h / 100f, sqrWidth / 100f, height / 100f)) : ColConversions.HSL2RGB(new(self._h / 100f, sqrWidth / 100f, height / 100f)));
                                continue;
                            }
                            self._ttre1.SetPixel(sqrWidth, height, ColConversions.HSV2RGB(new(sqrWidth / 100f, height / 100f, self._l / 100f)));

                        }
                        for (int rectWidth = 0; rectWidth < 10; rectWidth++)
                        {
                            if (ModOptions.enableDifferentOpColorPickerHSLPos.Value)
                            {
                                self._ttre2.SetPixel(rectWidth, height, ModOptions.hsl2HSVOPColorPicker.Value ? ColConversions.HSV2RGB(new(height / 100f, self._s / 100f, self._l / 100f)) : ColConversions.HSL2RGB(new(height / 100f, self._s / 100f, self._l / 100f)));
                                continue;
                            }
                            self._ttre2.SetPixel(rectWidth, height, ColConversions.HSV2RGB(new(self._h / 100f, self._s / 100f, height / 100f)));
                        }
                    }
                    //Arrows
                    Color hueArrowCol = new(1 - self._r / 100f, 1f - self._g / 100f, 1f - self._b / 100f);
                    hueArrowCol = Color.Lerp(Color.white, hueArrowCol, Mathf.Pow(Mathf.Abs(hueArrowCol.grayscale - 0.5f) * 2f, 0.3f));

                    for (int huePointer = Math.Max(0, self._h - 4); huePointer <= Math.Min(100, self._h + 4); huePointer++)
                    {
                        int middleOfHue = 5 - Math.Abs(self._h - huePointer);
                        if (ModOptions.enableDifferentOpColorPickerHSLPos.Value)
                        {
                            for (int rectWidth = 0; rectWidth < middleOfHue; rectWidth++)
                            {
                                self._ttre2.SetPixel(rectWidth, huePointer, hueArrowCol);
                            }
                            continue;
                        }

                        int control = self._s > 50 ? middleOfHue : 101;
                        for (int sqrHeight = self._s > 50? 0 : 101 - middleOfHue; sqrHeight < control; sqrHeight++)
                        {
                            self._ttre1.SetPixel(huePointer, sqrHeight, hueArrowCol);
                        }
                    }
                    for (int satPointer = Math.Max(0, self._s - 4); satPointer <= Math.Min(100, self._s + 4); satPointer++)
                    {
                        int middleOfSat = 5 - Math.Abs(self._s - satPointer);
                        if (ModOptions.enableDifferentOpColorPickerHSLPos.Value)
                        {
                            int control = self._l > 50 ? middleOfSat : 101;
                            for (int sqrHeight = self._l > 50? 0 : 101 - middleOfSat; sqrHeight < control; sqrHeight++)
                            {
                                self._ttre1.SetPixel(satPointer, sqrHeight, hueArrowCol);
                            }
                            continue;
                        }
                        int control2 = self._h > 50 ? middleOfSat : 101;
                        for (int sqrWidth = self._h > 50 ? 0 : 101 - middleOfSat; sqrWidth < control2; sqrWidth++)
                        {
                            self._ttre1.SetPixel(sqrWidth, satPointer, hueArrowCol);
                        }
                    }
                    for (int litPointer = Math.Max(0, self._l - 4); litPointer <= Math.Min(100, self._l + 4); litPointer++)
                    {
                        int middleOfLit = 5 - Math.Abs(self._l - litPointer);
                        if (ModOptions.enableDifferentOpColorPickerHSLPos.Value)
                        {
                            int control = self._s > 50 ? middleOfLit : 101;
                            for (int sqrWidth = self._s > 50 ? 0 : 101 - middleOfLit; sqrWidth < control; sqrWidth++)
                            {
                                self._ttre1.SetPixel(sqrWidth, litPointer, hueArrowCol);
                            }
                            continue;
                        }
                        for (int rectWidth = 0; rectWidth < middleOfLit; rectWidth++)
                        {
                            self._ttre2.SetPixel(rectWidth, litPointer, hueArrowCol);
                        }
                    }
                    self._ttre1.Apply();
                    self._ttre2.Apply();
                    return;
                }
                orig(self);
            }
            private void On_OPColorPicker__HSLSetValue(On.Menu.Remix.MixedUI.OpColorPicker.orig__HSLSetValue orig, OpColorPicker self)
            {
                self._h = self._h == 100 ? 0 : self._h;
                orig(self);
            }
            private void On_OPColorPickerUpdate(On.Menu.Remix.MixedUI.OpColorPicker.orig_Update orig, OpColorPicker self)
            {
                orig(self);
                if (!self.greyedOut && !self.held)
                {
                    if (self._MouseOverHex())
                    {
                        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C))
                        {
                            CopyCPicker(self);
                        }
                        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.V))
                        {
                            PasteCPicker(self);
                        }
                    }
                }
            }
            private void CopyCPicker(OpColorPicker cPicker)
            {
                Clipboard = cPicker.value;
                cPicker?.PlaySound(SoundID.MENU_Player_Join_Game);
            }
            private void PasteCPicker(OpColorPicker cPicker)
            {
                if (cPicker.CopyFromClipboard(Clipboard))
                {
                    cPicker.value = Clipboard.TrimStart('#').Substring(0, 6).ToUpper();
                    cPicker.PlaySound(SoundID.MENU_Switch_Page_In);
                    return;
                }
                cPicker.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);

            }
            public static string Clipboard
            {
                get => ClipboardManager.Clipboard;
                set => ClipboardManager.Clipboard = value;
            }
        }
        public static class NewMenuInterfaces
        {
            public class JollyCoopOOOConfigPages : PositionedMenuObject
            {
                public JollyCoopOOOConfigPages(Menu.Menu menu, ColorChangeDialog.ColorSlider owner, int bodyPartNum) : base(menu, owner, owner.pos)
                {
                    colSlider = owner;
                    if (oOOPages == null)
                    {
                        oOOIDGroups = new();
                        RegisterIDGroups(bodyPartNum);
                        oOOPages = new(menu, this, owner.hueSlider, owner.satSlider, owner.litSlider, oOOIDGroups, ModOptions.nextPageKeyBind.Value, ModOptions.prevPageKeyBind.Value, pos + new Vector2(0, 39.5f))
                        {
                            showValues = false
                        };
                        owner.hueSlider.ID = oOOIDGroups[0].ID1;
                        owner.satSlider.ID = oOOIDGroups[0].ID2;
                        owner.litSlider.ID = oOOIDGroups[0].ID3;
                        subObjects.Add(oOOPages);
                    }
                    if (valueLabel == null)
                    {
                        valueLabel = new(menu, this, "", new(120, 23), new(80, 30), false);
                        subObjects.Add(valueLabel);
                    }
                    if (ModOptions.enableHexCodeTypers.Value && hexInterface == null)
                    {
                        hexInterface = new(menu, this, new(120f, -100f));
                        subObjects.Add(hexInterface);
                    }
                }
                public override void RemoveSprites()
                {
                    base.RemoveSprites();
                    if (hexInterface != null)
                    {
                        hexInterface.RemoveSprites();
                        RemoveSubObject(hexInterface);
                        hexInterface = null;
                    }
                    if (valueLabel != null)
                    {
                        valueLabel.RemoveSprites();
                        RemoveSubObject(valueLabel);
                        valueLabel = null;
                    }
                    if (oOOPages != null)
                    {
                        oOOPages.RemoveSprites();
                        RemoveSubObject(oOOPages);
                        oOOPages = null;
                    }
                    if (oOOIDGroups != null)
                    {
                        foreach (SliderOOOIDGroup idGroups in oOOIDGroups)
                        {
                            foreach (Slider.SliderID id in idGroups.SliderIDs)
                            {
                                id.Unregister();
                            }
                        }
                        oOOIDGroups = null;
                    }
                }
                public override void GrafUpdate(float timeStacker)
                {
                    base.GrafUpdate(timeStacker);
                    if (valueLabel != null)
                    {
                        if (valueLabel.label?.color != color)
                        {
                            valueLabel.label.color = color;
                        }
                        if (ModOptions.ShowVisual && oOOPages != null)
                        {
                            string[] values = oOOPages.SliderValues;
                            valueLabel.text = $"({values.GetValueOrDefault(0, "")},{values.GetValueOrDefault(1, "")},{values.GetValueOrDefault(2, "")})";
                        }
                        else if (valueLabel.text != "")
                        {
                            valueLabel.text = "";
                        }
                    }
                }
                public override void Update()
                {
                    base.Update();
                    if (colSlider != null)
                    {
                        if (hexInterface != null)
                        {
                            hexInterface.SaveNewHSL(colSlider.hslColor.HSL2Vector3());
                            if (hexInterface.shouldUpdateNewColor)
                            {
                                hexInterface.shouldUpdateNewColor = false;
                                colSlider.hslColor = hexInterface.newPendingHSL.Vector32HSL();
                                colSlider.color = hexInterface.newPendingRGB;
                                colSlider.menu.SliderSetValue(colSlider.hueSlider, colSlider.menu.ValueOfSlider(colSlider.hueSlider));
                                colSlider.menu.SliderSetValue(colSlider.satSlider, colSlider.menu.ValueOfSlider(colSlider.satSlider));
                                colSlider.menu.SliderSetValue(colSlider.litSlider, colSlider.menu.ValueOfSlider(colSlider.litSlider));
                            }
                        }
                    }
                }
                public void RegisterIDGroups(int bodyPart)
                {
                    if (!(ColorConfigMod.IsLukkyRGBColorSliderModOn || ModOptions.RemoveHSLSliders))
                    {
                        oOOIDGroups.Add(new(colSlider.Hue, colSlider.Sat, colSlider.Lit, MenuToolObj.HSLNames, MenuToolObj.hueOOShowInt, MenuToolObj.hueOOMultipler));
                    }
                    if (ModOptions.EnableJollyRGBSliders)
                    {
                        Slider.SliderID[] sliderIDs = RegisterOOOSliderGroups("RGB", MenuToolObj.RGBNames, bodyPart);
                        oOOIDGroups.Add(new(sliderIDs[0], sliderIDs[1], sliderIDs[2], MenuToolObj.RGBNames, MenuToolObj.rgbShowInt, MenuToolObj.rgbMultipler));

                    }
                    if (ModOptions.enableHSVSliders.Value)
                    {
                        Slider.SliderID[] sliderIDs = RegisterOOOSliderGroups("HSV", MenuToolObj.HSVNames, bodyPart);
                        oOOIDGroups.Add(new(sliderIDs[0], sliderIDs[1], sliderIDs[2], MenuToolObj.HSVNames, MenuToolObj.hueOOShowInt, MenuToolObj.hueOOMultipler));
                    }
                }
                public Slider.SliderID[] RegisterOOOSliderGroups(string colorSpaceName, string[] oOONames, int bodyPart)
                {
                    Slider.SliderID[] sliderIDs = new Slider.SliderID[3];
                    sliderIDs[0] = new($"DUSTY_{bodyPart}_{colorSpaceName}_{oOONames.GetValueOrDefault(0, "")}", true);
                    sliderIDs[1] = new($"DUSTY_{bodyPart}_{colorSpaceName}_{oOONames.GetValueOrDefault(1, "")}", true);
                    sliderIDs[2] = new($"DUSTY_{bodyPart}_{colorSpaceName}_{oOONames.GetValueOrDefault(2, "")}", true);
                    return sliderIDs;
                }

                public Color color = MenuColorEffect.rgbMediumGrey;
                public MenuLabel valueLabel;
                public SliderOOOPages oOOPages;
                public List<SliderOOOIDGroup> oOOIDGroups;
                public ColorChangeDialog.ColorSlider colSlider;
                public HexTypeBox hexInterface;
            }
            public class SlugcatDisplay : RectangularMenuObject
            {
                public SlugcatDisplay(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, SlugcatStats.Name current) : base(menu, owner, pos, size)
                {
                    currentSlugcat = current;
                    prevSlugcat = current;
                    bodyNames = PlayerGraphics.ColoredBodyPartList(current);
                    sprites = new();
                    LoadIcon(current, bodyNames);

                }
                public Dictionary<string, string> LoadFileNames(SlugcatStats.Name name, List<string> bodyNames)
                {
                    Dictionary<string, string> bodyPaths = new();
                    foreach (string txtpath in SmallUtils.FindFilePaths("colorconfig", ".png"))
                    {
                        string resolvedPath = AssetManager.ResolveFilePath(txtpath);
                        if (File.Exists(resolvedPath))
                        {
                            foreach (string line in File.ReadAllLines(resolvedPath, System.Text.Encoding.UTF8))
                            {
                                if (line.StartsWith(name.value) && line.Split(':').GetValueOrDefault(1, "").Contains('|'))
                                {
                                    foreach (string body in line.Split(':')[1].Split(','))
                                    {
                                        string[] bodyLine = body.Split('|');
                                        if (bodyNames.Contains(bodyLine[0]) && bodyLine.Length > 1)
                                        {
                                            ColorConfigMod.DebugLog(bodyLine[1]);
                                            if (bodyPaths.ContainsKey(bodyLine[0]))
                                            {
                                                bodyPaths[bodyLine[0]] = bodyLine[1];
                                                continue;
                                            }
                                            bodyPaths.Add(bodyLine[0], bodyLine[1]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return bodyPaths;
                }
                public void LoadSlugcatSprites(SlugcatStats.Name name, List<string> bodyNames)
                {
                    Dictionary<string, string> preSetFilesToLoad = LoadFileNames(name, bodyNames);
                    for (int i = 0; i < bodyNames.Count; i++)
                    {
                        string folder = "";
                        string file;
                        if (!preSetFilesToLoad.ContainsKey(bodyNames[i]))
                        {
                            file = i switch
                            {
                                0 => File.Exists(AssetManager.ResolveFilePath("illustrations/" + name.value + "_pup_off.png")) ? name.value + "_pup_off" : "pup_off",
                                1 => File.Exists(AssetManager.ResolveFilePath("illustrations/" + "face_" + name.value + "_pup_off.png")) ? $"face_{name.value}_pup_off" : "face_pup_off",
                                2 => File.Exists(AssetManager.ResolveFilePath($"illustrations/unique_{name.value}_pup_off.png")) ? $"unique_{name.value}_pup_off" : "colorconfig_showcasesquare",
                                _ => File.Exists(AssetManager.ResolveFilePath($"illustrations/{bodyNames[i]}_{name.value}_pup_off.png")) ? $"{bodyNames[i]}_{name.value}_pup_off" : "colorconfig_showcasesquare",
                            };
                        }
                        else
                        {
                            string path = preSetFilesToLoad[bodyNames[i]];
                            file = path;
                            if (path.Contains("/"))
                            {
                                file = path.Split('|').Last();
                                folder = path.Replace(file + "/", string.Empty);
                            }
                        }
                        ColorConfigMod.DebugLog($"BodyPart: {bodyNames[i]},Folder: {folder}, File: {file}");
                        MenuIllustration body = new(menu, this, folder, file, file == "colorconfig_showcasesquare" ? new(i * 10, -0.7f) : size / 2, true, true);
                        subObjects.Add(body);
                        sprites.Add(body);

                    }
                }
                public void LoadIcon(SlugcatStats.Name current, List<string> bodyNames)
                {
                    if (sprites?.Count > 0)
                    {
                        foreach (MenuIllustration slugcatSprite in sprites)
                        {
                            slugcatSprite.RemoveSprites();
                            RemoveSubObject(slugcatSprite);
                        }
                    }
                    sprites = new();
                    LoadSlugcatSprites(current, bodyNames);

                }
                public void LoadNewColorSlugcat(List<Color> slugcatCols, SlugcatStats.Name name)
                {
                    currentSlugcat = name;
                    if (currentSlugcat != prevSlugcat)
                    {
                        bodyNames = PlayerGraphics.ColoredBodyPartList(name);
                    }
                    while (slugcatCols.Count < bodyNames.Count)
                    {
                        slugcatCols.Add(Color.white);
                    }
                    currentRGBs = slugcatCols;
                }
                public void LoadNewHSLStringSlugcat(List<string> slugcatHSLColos, SlugcatStats.Name name)
                {
                    currentSlugcat = name;
                    if (currentSlugcat != prevSlugcat)
                    {
                        bodyNames = PlayerGraphics.ColoredBodyPartList(name);
                    }
                    List<Color> rgbs = new();
                    for (int i = 0; i < slugcatHSLColos.Count; i++)
                    {
                        if (slugcatHSLColos[i].Contains(","))
                        {
                            string[] hslArray = slugcatHSLColos[i].Split(',');
                            rgbs.Add(ColConversions.HSL2RGB(new(float.Parse(hslArray[0], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(hslArray[1], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(hslArray[2], NumberStyles.Any, CultureInfo.InvariantCulture))));
                        }
                    }
                    while (rgbs.Count < bodyNames.Count)
                    {
                        rgbs.Add(Color.white);
                    }
                    currentRGBs = rgbs;
                }
                public override void GrafUpdate(float timeStacker)
                {
                    base.GrafUpdate(timeStacker);
                    if (prevSlugcat != currentSlugcat)
                    {
                        prevSlugcat = currentSlugcat;
                        LoadIcon(currentSlugcat, bodyNames);
                    }
                    if (sprites?.Count > 0)
                    {
                        if (currentRGBs != null)
                        {
                            if (currentRGBs != prevRGBs)
                            {
                                prevRGBs = currentRGBs;
                                for (int i = 0; i < currentRGBs.Count; i++)
                                {
                                    if (sprites.Count - 1 < i)
                                    {
                                        continue;
                                    }
                                    sprites[i].color = currentRGBs[i];
                                }
                            }
                        }
                    }
                }
                public override void RemoveSprites()
                {
                    base.RemoveSprites();
                    if (sprites?.Count > 0)
                    {
                        foreach (MenuIllustration illu in sprites)
                        {
                            illu.RemoveSprites();
                            RemoveSubObject(illu);
                        }
                        sprites = null;
                    }
                }

                public SlugcatStats.Name currentSlugcat, prevSlugcat;
                public List<Color> currentRGBs, prevRGBs;
                public List<string> bodyNames;
                public List<MenuIllustration> sprites;
            }
            public class SliderOOOPages : MenuObject, ICanTurnPages, ISingleKeyCodeInput
            {
                public SliderOOOPages(Menu.Menu menu, MenuObject owner, HorizontalSlider slider1, HorizontalSlider slider2, HorizontalSlider slider3, List<SliderOOOIDGroup> sliderOOOIDGroups, Vector2 buttonOffset = default) : base(menu, owner)
                {
                    if (slider1 is null || slider2 is null || slider3 is null)
                    {
                        ColorConfigMod.DebugError("Sliders in pages are null!");
                        return;
                    }
                    currentOffset = 0;
                    sliderO = slider1;
                    sliderOO = slider2;
                    sliderOOO = slider3;
                    OOOIDGroups = sliderOOOIDGroups?.Count == 0 ? new()
                    { new(slider1.ID, slider2.ID, slider3.ID, null, null)} : sliderOOOIDGroups;
                    buttonOffset = buttonOffset == default ? new(0, 0) : buttonOffset;
                    setPrevButtonPos = new(buttonOffset.x + slider3.pos.x, -(slider3.size.y * 2) + buttonOffset.y + slider3.pos.y);
                    if (PagesOn)
                    {
                        ActivateButtons();
                    }
                }
                public SliderOOOPages(Menu.Menu menu, MenuObject owner, HorizontalSlider slider1, HorizontalSlider slider2, HorizontalSlider slider3, List<SliderOOOIDGroup> sliderOOOIDGroups, KeyCode nextPageKey, KeyCode prevPageKey, Vector2 buttonOffset = default) : base(menu, owner)
                {
                    if (slider1 is null || slider2 is null || slider3 is null)
                    {
                        ColorConfigMod.DebugError("Sliders in pages are null!");
                        return;
                    }
                    currentOffset = 0;
                    sliderO = slider1;
                    sliderOO = slider2;
                    sliderOOO = slider3;
                    OOOIDGroups = sliderOOOIDGroups?.Count == 0 ? new()
                    { new(slider1.ID, slider2.ID, slider3.ID, null, null)} : sliderOOOIDGroups;
                    buttonOffset = buttonOffset == default ? new(0, 0) : buttonOffset;
                    setPrevButtonPos = new(buttonOffset.x + slider3.pos.x, -(slider3.size.y * 2) + buttonOffset.y + slider3.pos.y);
                    nextKeyPage = nextPageKey;
                    prevKeyPage = prevPageKey;
                    if (PagesOn)
                    {
                        ActivateButtons();
                    }
                    keyBind = true;
                }
                public List<SliderOOOIDGroup> OOOIDGroups { get; private set; }
                public BigSimpleButton PrevButton
                {
                    get => prevButton;
                }
                public BigSimpleButton NextButton
                {
                    get => nextButton;
                }
                public bool ShouldGetInput
                {
                    get => PagesOn && keyBind && (prevButton?.Selected == true || nextButton?.Selected == true || sliderO?.Selected == true || sliderOO?.Selected == true || sliderOOO?.Selected == true);
                }
                public int CurrentOffset
                { get => currentOffset; }
                public bool PagesOn
                {
                    get => OOOIDGroups?.Count > 1;
                }
                public bool Slider1ShowInt
                { get => OOOIDGroups?[currentOffset]?.showInt1 == true; }
                public bool Slider2ShowInt
                { get => OOOIDGroups?[currentOffset]?.showInt2 == true; }
                public bool Slider3ShowInt
                { get => OOOIDGroups?[currentOffset]?.showInt3 == true; }
                public string[] SliderValues
                {
                    get
                    {
                        string slider1Amt = "";
                        string slider2Amt = "";
                        string slider3Amt = "";
                        if (OOOIDGroups?.Count > 0 && OOOIDGroups[currentOffset] != null)
                        {
                            if (sliderO != null)
                            {
                                float value = sliderO.floatValue * OOOIDGroups[currentOffset].showMultipler.x;
                                double amt = Slider1ShowInt ? Math.Round(value, 0, MidpointRounding.AwayFromZero) : Math.Round(value, ModOptions.Digit, MidpointRounding.AwayFromZero);
                                string sign = showSign ? OOOIDGroups[currentOffset].showMultipler.x == 100 ? "%" : OOOIDGroups[currentOffset].showMultipler.x == 360 && Slider1ShowInt ? MenuToolObj.degreeSign : "" : "";
                                slider1Amt = $"{amt}{sign}";
                            }
                            if (sliderOO != null)
                            {
                                float value = sliderOO.floatValue * OOOIDGroups[currentOffset].showMultipler.y;
                                double amt = Slider2ShowInt ? Math.Round(value, 0, MidpointRounding.AwayFromZero) : Math.Round(value, ModOptions.Digit, MidpointRounding.AwayFromZero);
                                string sign = showSign ? OOOIDGroups[currentOffset].showMultipler.y == 100 ? "%" : OOOIDGroups[currentOffset].showMultipler.y == 360 && Slider2ShowInt ? MenuToolObj.degreeSign : "" : "";
                                slider2Amt = $"{amt}{sign}";
                            }
                            if (sliderOOO != null)
                            {
                                float value = sliderOOO.floatValue * OOOIDGroups[currentOffset].showMultipler.z;
                                double amt = Slider3ShowInt ? Math.Round(value, 0, MidpointRounding.AwayFromZero) : Math.Round(value, ModOptions.Digit, MidpointRounding.AwayFromZero);
                                string sign = showSign ? OOOIDGroups[currentOffset].showMultipler.z == 100 ? "%" : OOOIDGroups[currentOffset].showMultipler.y == 360 && Slider3ShowInt ? MenuToolObj.degreeSign : "" : "";
                                slider3Amt = $"{amt}{sign}";
                            }
                        }

                        return new[] { slider1Amt, slider2Amt, slider3Amt };
                    }
                }
                public override void RemoveSprites()
                {
                    base.RemoveSprites();
                    DeactivateButtons();
                    if (sliderO != null)
                    {
                        sliderO.RemoveSprites();
                        RemoveSubObject(sliderO);
                        sliderO = null;
                    }
                    if (sliderOO != null)
                    {
                        sliderOO.RemoveSprites();
                        RemoveSubObject(sliderOO);
                        sliderOO = null;
                    }
                    if (sliderOOO != null)
                    {
                        sliderOOO.RemoveSprites();
                        RemoveSubObject(sliderOOO);
                        sliderOOO = null;
                    }
                    if (OOOIDGroups?.Count > 0)
                    {
                        OOOIDGroups.Clear();
                        OOOIDGroups = null;
                    }
                }
                public override void Singal(MenuObject sender, string message)
                {
                    base.Singal(sender, message);
                    if (sender == prevButton)
                    {
                        menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                        PrevPage();
                    }
                    if (sender == nextButton)
                    {
                        menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                        NextPage();
                    }
                }
                public override void GrafUpdate(float timeStacker)
                {
                    base.GrafUpdate(timeStacker);
                    if (OOOIDGroups != null && OOOIDGroups[currentOffset] != null)
                    {
                        if (sliderO != null)
                        {
                            if (sliderO.menuLabel != null && ((!showValues && sliderO.menuLabel.text != menu.Translate(OOOIDGroups[currentOffset].name1)) || showValues))
                            {
                                sliderO.menuLabel.text = menu.Translate(OOOIDGroups[currentOffset].name1) + (showValues ? $" {SliderValues[0]}" : "");
                            }
                        }
                        if (sliderOO != null)
                        {
                            if (sliderOO.menuLabel != null && ((!showValues && sliderOO.menuLabel.text != menu.Translate(OOOIDGroups[currentOffset].name2)) || showValues))
                            {
                                sliderOO.menuLabel.text = menu.Translate(OOOIDGroups[currentOffset].name2) + (showValues ? $" {SliderValues[1]}" : "");
                            }
                        }
                        if (sliderOOO != null)
                        {
                            if (sliderOOO.menuLabel != null && ((!showValues && sliderOOO.menuLabel.text != menu.Translate(OOOIDGroups[currentOffset].name3)) || showValues))
                            {
                                sliderOOO.menuLabel.text = menu.Translate(OOOIDGroups[currentOffset].name3) + (showValues ? $" {SliderValues[2]}" : "");
                            }
                        }
                    }
                }
                public override void Update()
                {
                    base.Update();
                    GetInput();
                }
                public void NextPage()
                {
                    currentOffset++;
                    if (OOOIDGroups == null || OOOIDGroups?.Count == 0 || currentOffset > OOOIDGroups.Count - 1)
                    {
                        currentOffset = 0;
                    }
                    PopulatePage(currentOffset);
                }
                public void PrevPage()
                {
                    currentOffset--;
                    if (currentOffset < 0)
                    {
                        if (OOOIDGroups != null && OOOIDGroups.Count > 0)
                        {
                            currentOffset = OOOIDGroups.Count - 1;
                        }
                        else
                        {
                            currentOffset = 0;
                        }
                    }
                    PopulatePage(currentOffset);
                }
                public void PopulatePage(int offset)
                {
                    currentOffset = offset;
                    if (OOOIDGroups?.Count > 0 && OOOIDGroups[offset] != null)
                    {
                        if (sliderO != null)
                        {
                            sliderO.ID = OOOIDGroups[currentOffset].ID1;
                        }
                        if (sliderOO != null)
                        {
                            sliderOO.ID = OOOIDGroups[currentOffset].ID2;
                        }
                        if (sliderOOO != null)
                        {
                            sliderOOO.ID = OOOIDGroups[currentOffset].ID3;
                        }
                    }
                    if (sliderO != null)
                    {
                        menu.SliderSetValue(sliderO, menu.ValueOfSlider(sliderO));
                    }
                    if (sliderOO != null)
                    {
                        menu.SliderSetValue(sliderOO, menu.ValueOfSlider(sliderOO));
                    }
                    if (sliderOOO != null)
                    {
                        menu.SliderSetValue(sliderOOO, menu.ValueOfSlider(sliderOOO));
                    }
                }
                public void ActivateButtons()
                {
                    if (prevButton == null)
                    {
                        prevButton = new(menu, this, menu.Translate("Prev"), "_BackPageSliders", setPrevButtonPos, new(40, 26), FLabelAlignment.Center, false);
                        subObjects.Add(prevButton);

                    }
                    if (nextButton == null)
                    {
                        nextButton = new(menu, this, menu.Translate("Next"), "_NextPageSliders", new(setPrevButtonPos.x + 60, setPrevButtonPos.y), new(40, 26), FLabelAlignment.Center, false);
                        subObjects.Add(nextButton);
                    }
                    menu.MutualHorizontalButtonBind(prevButton, nextButton);
                }
                public void DeactivateButtons()
                {
                    if (prevButton != null)
                    {
                        prevButton.RemoveSprites();
                        RemoveSubObject(prevButton);
                        prevButton = null;
                    }
                    if (nextButton != null)
                    {
                        nextButton.RemoveSprites();
                        RemoveSubObject(nextButton);
                        nextButton = null;
                    }
                }
                public void GetInput()
                {
                    if (ShouldGetInput && !changingPage)
                    {
                        if (Input.GetKeyUp(prevKeyPage))
                        {
                            changingPage = true;
                            menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                            PrevPage();
                            changingPage = false;
                        }
                        if (Input.GetKeyUp(nextKeyPage))
                        {
                            changingPage = true;
                            menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                            NextPage();
                            changingPage = false;
                        }
                    }
                }

                private int currentOffset;
                public Vector2 setPrevButtonPos;
                public bool changingPage = false, keyBind = false, showValues = true, showSign = true;
                public HorizontalSlider sliderO, sliderOO, sliderOOO;
                private BigSimpleButton prevButton, nextButton;
                public KeyCode nextKeyPage, prevKeyPage;
            }
            public class HexTypeBox : PositionedMenuObject, ICopyPasteConfig
            {
                public HexTypeBox(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
                {
                    shouldUpdateNewColor = false;
                    if (tabWrapper == null)
                    {
                        tabWrapper = new(menu, this);
                        subObjects.Add(tabWrapper);
                    }
                    if (hexTyper == null)
                    {
                        lastValue = "";
                        hexTyper = new(new Configurable<string>(""), Vector2.zero, 60)
                        {
                            maxLength = 6,
                        };
                    }
                    if (elementWrapper == null)
                    {
                        elementWrapper = new(tabWrapper, hexTyper);
                        subObjects.Add(elementWrapper);
                    }
                }
                public string Clipboard
                {
                    get => ClipboardManager.Clipboard;
                    set => ClipboardManager.Clipboard = value;
                }
                public bool ShouldCopyOrPaste
                { get => hexTyper?.MouseOver == true; }
                public void SaveNewHSL(Vector3 hsl)
                {
                    currentHSL = hsl;
                }
                public void SaveNewRGB(Color rgb)
                {
                    currentHSL = Custom.RGB2HSL(rgb);
                }
                public void Copy()
                {
                    Clipboard = hexTyper.value;
                    menu.PlaySound(SoundID.MENU_Player_Join_Game);
                }
                public void Paste()
                {
                    string pendingVal = new(Clipboard.Where(x => x != '#').ToArray());
                    if (!SmallUtils.IfHexCodeValid(pendingVal, out Color fromPaste))
                    {
                        menu.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
                        return;
                    }
                    hexTyper.value = ColorUtility.ToHtmlStringRGB(SmallUtils.Vector32RGB(
                        SmallUtils.RWIIIClamp(SmallUtils.RGB2Vector3(fromPaste), CustomColorModel.RGB,
                        out Vector3 newClampedHSL)));
                    if (newClampedHSL != currentHSL)
                    {
                        newPendingHSL = SmallUtils.SliderWonkiness.FixHexSliderWonkiness(newClampedHSL, currentHSL);
                        newPendingRGB = ColConversions.HSL2RGB(newPendingHSL);
                        shouldUpdateNewColor = true;
                        currentHSL = newPendingHSL;
                        prevHSL = newPendingHSL;
                    }
                    lastValue = hexTyper.value;
                    menu.PlaySound(SoundID.MENU_Switch_Page_In);
                }
                public override void Update()
                {
                    base.Update();
                    if (hexTyper != null)
                    {
                        if (prevHSL != currentHSL)
                        {
                            prevHSL = currentHSL;
                            hexTyper.value = ColConversions.HSL2Hex(currentHSL);
                            lastValue = hexTyper.value;
                        }
                        if (!hexTyper.held)
                        {
                            if (hexTyper.value != lastValue)
                            {
                                if (!SmallUtils.IfHexCodeValid(hexTyper.value, out Color hexCol))
                                {
                                    Debug.LogError($"Failed to parse from new value \"{hexTyper.value}\"");
                                    hexTyper.value = lastValue;
                                }
                                else
                                {
                                    hexTyper.value = ColorUtility.ToHtmlStringRGB(SmallUtils.Vector32RGB(SmallUtils.RWIIIClamp(SmallUtils.RGB2Vector3(hexCol), CustomColorModel.RGB, out Vector3 clampedHSLHex)));
                                    if (clampedHSLHex != currentHSL)
                                    {
                                        newPendingHSL = SmallUtils.SliderWonkiness.FixHexSliderWonkiness(clampedHSLHex, currentHSL);
                                        newPendingRGB = ColConversions.HSL2RGB(newPendingHSL);
                                        shouldUpdateNewColor = true;
                                        currentHSL = newPendingHSL;
                                        prevHSL = newPendingHSL;
                                    }
                                    lastValue = hexTyper.value;
                                }
                            }
                        }
                        if (ShouldCopyOrPaste)
                        {
                            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C))
                            {
                                Copy();
                            }
                            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.V))
                            {
                                Paste();
                            }
                        }
                    }
                }
                public override void RemoveSprites()
                {
                    base.RemoveSprites();
                    if (elementWrapper != null)
                    {
                        elementWrapper.RemoveSprites();
                        RemoveSubObject(elementWrapper);
                        elementWrapper = null;
                    }
                    if (hexTyper != null)
                    {
                        hexTyper.label.alpha = 0;
                        hexTyper.label.RemoveFromContainer();
                        hexTyper.rect.container.RemoveFromContainer();
                        hexTyper = null;
                    }
                    if (tabWrapper != null)
                    {
                        RemoveSubObject(tabWrapper);
                        tabWrapper.RemoveSprites();
                        tabWrapper = null;
                    }
                }

                public string lastValue;
                public bool shouldUpdateNewColor;
                public Color newPendingRGB;
                public Vector3 currentHSL, prevHSL, newPendingHSL;
                public MenuTabWrapper tabWrapper;
                public UIelementWrapper elementWrapper;
                public OpTextBox hexTyper;
            }
            public class SliderOOOIDGroup
            {
                public SliderOOOIDGroup(Slider.SliderID sliderID1, Slider.SliderID sliderID2, Slider.SliderID sliderID3,
                    string[] names, bool[] showInts, Vector3 multipler = default)
                {
                    ID1 = sliderID1;
                    ID2 = sliderID2;
                    ID3 = sliderID3;
                    name1 = names.GetValueOrDefault(0, "");
                    name2 = names.GetValueOrDefault(1, "");
                    name3 = names.GetValueOrDefault(2, "");
                    showInt1 = showInts.GetValueOrDefault(0, false);
                    showInt2 = showInts.GetValueOrDefault(1, false);
                    showInt3 = showInts.GetValueOrDefault(2, false);
                    showMultipler = multipler == default ? new(1, 1, 1) : multipler;
                }
                public List<Slider.SliderID> SliderIDs
                {
                    get => new() { ID1, ID2, ID3 };
                }
                public Vector3 showMultipler;
                public Slider.SliderID ID1, ID2, ID3;
                public string name1, name2, name3;
                public bool showInt1, showInt2, showInt3;
            }
            public interface ISingleKeyCodeInput
            {
                bool ShouldGetInput { get; }
                void GetInput();
            }
            public interface ICanTurnPages
            {
                int CurrentOffset { get; }
                bool PagesOn { get; }
                void PopulatePage(int offset);
                void NextPage();
                void PrevPage();
            }
            public interface ICopyPasteConfig
            {
                string Clipboard { get; set; }
                bool ShouldCopyOrPaste { get; }
                void Copy();
                void Paste();
            }
        }

    }
}
