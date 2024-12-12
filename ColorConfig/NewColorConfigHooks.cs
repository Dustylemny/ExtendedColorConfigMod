using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace ColorConfig
{
    public static class NewColorConfigHooks
    {
        public class SlugcatSelectMenuScreenHooks
        {
            public void Init()
            {
                try
                {
                    On.Menu.SlugcatSelectMenu.AddColorInterface += On_AddColorInterface;
                    On.Menu.SlugcatSelectMenu.RemoveColorInterface += On_RemoveColorInterface;
                    On.Menu.SlugcatSelectMenu.Update += On_Update;
                    On.Menu.SlugcatSelectMenu.ValueOfSlider += On_ValueOfSlider;
                    On.Menu.SlugcatSelectMenu.SliderSetValue += On_SliderSetValue;
                    ColorConfigMod.DebugLog("Sucessfully extended color interface for slugcat select menu!");
                }
                catch (Exception ex)
                {
                    ColorConfigMod.DebugException("Failed to initalise hooks for SlugcatSelectMenu color interface!", ex);
                }
            }
            private void On_AddColorInterface(On.Menu.SlugcatSelectMenu.orig_AddColorInterface orig, SlugcatSelectMenu self)
            {
                orig(self);
                if (oOOPages == null)
                {
                    oOOPages = new(self, self.pages[0], self.hueSlider, self.satSlider, self.litSlider, SliderIDGroups, new(0, 25));
                    oOOPages.ForceOwnerSlider();
                    self.pages[0].subObjects.Add(oOOPages);
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
                            self.slugcatColorOrder[self.slugcatPageIndex],
                         self.manager.rainWorld.progression.miscProgressionData.colorChoices[self.slugcatColorOrder[self.slugcatPageIndex].value]);
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
            private void On_RemoveColorInterface(On.Menu.SlugcatSelectMenu.orig_RemoveColorInterface orig, SlugcatSelectMenu self)
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
            private void On_Update(On.Menu.SlugcatSelectMenu.orig_Update orig, SlugcatSelectMenu self)
            {
                orig(self);
                if (hexInterface != null)
                {
                    hexInterface.SaveNewHSL(SmallUtils.SlugcatSelectMenuHSL(self));
                    if (hexInterface.shouldUpdateNewHSL)
                    {
                        hexInterface.shouldUpdateNewHSL = false;
                        SaveHSLString(self, SmallUtils.SetHSLSaveString(hexInterface.newPendingHSL));
                        if (self.hueSlider != null)
                        {
                            self.SliderSetValue(self.hueSlider, self.ValueOfSlider(self.hueSlider));
                        }
                        if (self.satSlider != null)
                        {
                            self.SliderSetValue(self.satSlider, self.ValueOfSlider(self.satSlider));
                        }
                        if (self.litSlider != null)
                        {
                            self.SliderSetValue(self.litSlider, self.ValueOfSlider(self.litSlider));
                        }
                    }
                }
                slugcatDisplay?.LoadNewHSLStringSlugcat(
                    self.manager.rainWorld.progression.miscProgressionData.colorChoices[self.slugcatColorOrder[self.slugcatPageIndex].value], 
                    self.slugcatColorOrder[self.slugcatPageIndex]);

            }
            private float On_ValueOfSlider(On.Menu.SlugcatSelectMenu.orig_ValueOfSlider orig, SlugcatSelectMenu self, Slider slider)
            {
                if (ValueOfCustomSliders(self, slider, out float f))
                {
                    return f;
                }
                return orig(self, slider);
            }
            private void On_SliderSetValue(On.Menu.SlugcatSelectMenu.orig_SliderSetValue orig, SlugcatSelectMenu self, Slider slider, float f)
            {
                CustomSlidersSetValue(self, slider, f);
                orig(self, slider, f);
            }
            private void CustomSlidersSetValue(SlugcatSelectMenu ssM, Slider slider, float f)
            {
                Vector3 ssMHSL = SmallUtils.SlugcatSelectMenuHSL(ssM);
                if (slider != null)
                {

                    if (MenuToolObj.RGBSliderIDS.Contains(slider.ID))
                    {
                        Color color = ColConversions.HSL2RGB(ssMHSL);
                        if (slider.ID == MenuToolObj.RedRGB)
                        {
                            color.r = f;
                        }
                        else if (slider.ID == MenuToolObj.GreenRGB)
                        {
                            color.g = f;
                        }
                        else if (slider.ID == MenuToolObj.BlueRGB)
                        {
                            color.b = f;
                        }
                        SmallUtils.Vector32RGB(SmallUtils.RWIIIClamp(SmallUtils.RGB2Vector3(SmallUtils.RGBClamp01(color)),
                            CustomColorModel.RGB, out Vector3 hsl));
                        SaveHSLString(ssM, SmallUtils.SetHSLSaveString(hsl));

                    }
                    if (MenuToolObj.HSVSliderIDS.Contains(slider.ID))
                    {
                        Vector3 hsv = ColConversions.HSL2HSV(ssMHSL);

                        if (slider.ID == MenuToolObj.HueHSV)
                        {
                            hsv.x = Mathf.Clamp(f, 0, 0.99f);
                        }
                        else if (slider.ID == MenuToolObj.SatHSV)
                        {
                            hsv.y = f;
                        }
                        else if (slider.ID == MenuToolObj.ValHSV)
                        {
                            hsv.z = f;
                        }
                        SmallUtils.RWIIIClamp(hsv,
                           CustomColorModel.HSV, out Vector3 newHSL);
                        SaveHSLString(ssM, SmallUtils.SetHSLSaveString(newHSL));
                    }
                }

            }
            private bool ValueOfCustomSliders(SlugcatSelectMenu ssM, Slider slider, out float f)
            {
                f = 0;
                Vector3 ssMHSL = SmallUtils.SlugcatSelectMenuHSL(ssM);
                if (slider != null)
                {
                    if (MenuToolObj.RGBSliderIDS.Contains(slider.ID))
                    {
                        Color color = ColConversions.HSL2RGB(ssMHSL);
                        if (slider.ID == MenuToolObj.RedRGB)
                        {
                            f = color.r;
                            return true;
                        }
                        if (slider.ID == MenuToolObj.GreenRGB)
                        {
                            f = color.g;
                            return true;
                        }
                        if (slider.ID == MenuToolObj.BlueRGB)
                        {
                            f = color.b;
                            return true;
                        }
                    }
                    if (MenuToolObj.HSVSliderIDS.Contains(slider.ID))
                    {
                        Vector3 hsv = ColConversions.HSL2HSV(ssMHSL);
                        if (slider.ID == MenuToolObj.HueHSV)
                        {
                            f = hsv.x;
                            return true;
                        }
                        if (slider.ID == MenuToolObj.SatHSV)
                        {
                            f = hsv.y;
                            return true;
                        }
                        if (slider.ID == MenuToolObj.ValHSV)
                        {
                            f = hsv.z;
                            return true;
                        }
                    }
                }
                return false;
            }
            private void SaveHSLString(SlugcatSelectMenu ssM, string newHSL)
            {
                ssM.manager.rainWorld.progression.miscProgressionData.colorChoices[ssM.slugcatColorOrder[ssM.slugcatPageIndex].value]
                [ssM.activeColorChooser] = newHSL;
            }
            private List<NewMenuInterfaces.SliderOOOIDGroup> SliderIDGroups
            {
                get
                {
                    List<NewMenuInterfaces.SliderOOOIDGroup> IDGroups = new()
                    { new(MMFEnums.SliderID.Hue, MMFEnums.SliderID.Saturation, MMFEnums.SliderID.Lightness, MenuToolObj.HSLNames, MenuToolObj.hueOOShowInt,
                    MenuToolObj.hueOOMultipler)};
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
            private NewMenuInterfaces.HexTypeBox hexInterface;
            private NewMenuInterfaces.SlugcatDisplay slugcatDisplay;
            private NewMenuInterfaces.SliderOOOPages oOOPages;
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
                    ColorConfigMod.DebugLog("Sucessfully extended color interface for jolly coop menu!");
                }
                catch (Exception ex)
                {
                    ColorConfigMod.DebugException("Failed to initalise hooks for JollyMenu color interface!", ex);
                }
            }
            private float OnDialog_ValueOfSlider(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_ValueOfSlider orig, ColorChangeDialog self, Slider slider)
            {
                if (FindValueOfSlider(slider, out float f))
                {
                    return f;
                }
                return orig(self, slider);
            }
            private void OnDialog_SliderSetValue(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_SliderSetValue orig, ColorChangeDialog self, Slider slider, float f)
            {
                if (slider.ID.value.Contains("DUSTYLEMNY"))
                {
                    string[] array = slider.ID.value.Split(new char[]
                    {
                       '_'
                    });
                    if (int.TryParse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture, out int num))
                    {
                        ColorChangeDialog.ColorSlider colSlider = colSliders[num];
                        if (slider.ID.value.Contains("RGB"))
                        {
                            if (FindOOO(slider.ID.value, CustomColorModel.RGB, out int ooo))
                            {
                                SetNewCustomColor(f, ref colSlider, ooo, CustomColorModel.RGB);
                            }

                        }
                        else if (slider.ID.value.Contains("HSV"))
                        {
                            if (FindOOO(slider.ID.value, CustomColorModel.HSV, out int ooo))
                            {
                                SetNewCustomColor(f, ref colSlider, ooo, CustomColorModel.HSV);
                            }
                        }
                    }
                }
                orig(self, slider, f);
            }
            private void On_ColorSliderctor(On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_ctor orig, ColorChangeDialog.ColorSlider self, Menu.Menu menu, MenuObject owner, Vector2 pos, int playerNumber, int bodyPart, string sliderTitle)
            {
                orig(self, menu, owner, pos, playerNumber, bodyPart, sliderTitle);
                colSliders.Add(self);
                NewMenuInterfaces.JollyCoopOOOConfigPages jollyCoopOOOConfigPages = new(menu, self, bodyPart);
                self.subObjects.Add(jollyCoopOOOConfigPages);
                configPages.Add(self, jollyCoopOOOConfigPages);
            }
            private void On_ColorSliderRemoveSprites(On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_RemoveSprites orig, ColorChangeDialog.ColorSlider self)
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
            private bool FindValueOfSlider(Slider slider, out float f)
            {
                f = 0;
                if (slider?.ID?.value.Contains("DUSTY") == true)
                {
                    string[] array = slider.ID.value.Split(new char[]
                    {
                    '_'
                    });
                    if (int.TryParse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture, out int num))
                    {
                        if (slider.ID.value.Contains("RGB"))
                        {
                            if (FindOOO(slider.ID.value, CustomColorModel.RGB, out int ooo))
                            {
                                f = ValueOfOOO(colSliders[num], ooo, CustomColorModel.RGB);
                                return true;
                            }
                        }
                        else if (slider.ID.value.Contains("HSV"))
                        {
                            if (FindOOO(slider.ID.value, CustomColorModel.HSV, out int ooo))
                            {
                                f = ValueOfOOO(colSliders[num], ooo, CustomColorModel.HSV);
                                return true;
                            }
                        }

                    }
                }
                return false;
            }
            public static bool FindOOO(string value, CustomColorModel colormodel, out int OOO)
            {
                OOO = -1;
                if (colormodel == CustomColorModel.RGB)
                {
                    if (value.Contains("RED"))
                    {
                        OOO = 0;
                    }
                    if (value.Contains("GREEN"))
                    {
                        OOO = 1;
                    }
                    if (value.Contains("BLUE"))
                    {
                        OOO = 2;
                    }
                }
                if (colormodel == CustomColorModel.HSV)
                {
                    if (value.Contains("HUE"))
                    {
                        OOO = 0;
                    }
                    if (value.Contains("SAT"))
                    {
                        OOO = 1;
                    }
                    if (value.Contains("VALUE"))
                    {
                        OOO = 2;
                    }
                }
                if (colormodel == CustomColorModel.HSL)
                {
                    if (value.Contains("HUE"))
                    {
                        OOO = 0;
                    }
                    if (value.Contains("SAT"))
                    {
                        OOO = 1;
                    }
                    if (value.Contains("LIT"))
                    {
                        OOO = 2;
                    }
                }
                return OOO > -1;
            }
            public static float ValueOfOOO(ColorChangeDialog.ColorSlider colSlider, int OOO, CustomColorModel colormodel)
            {
                return colormodel switch
                {
                    CustomColorModel.RGB => OOOSwitchResult(OOO, SmallUtils.RGB2Vector3(colSlider.color)),
                    CustomColorModel.HSL => OOOSwitchResult(OOO, ColConversions.HSL2HSV(SmallUtils.HSL2Vector3(colSlider.hslColor)), maxClamp: new(0.99f, 1, 1)),
                    _ => OOOSwitchResult(OOO, SmallUtils.HSL2Vector3(colSlider.hslColor), new(0, 0, 0.01f), new(0.99f, 1, 1))
                };
            }
            public static void SetNewCustomColor(float f, ref ColorChangeDialog.ColorSlider colSlider, int OOO, CustomColorModel colormodel)
            {
                if (colormodel == CustomColorModel.RGB)
                {
                    switch (OOO)
                    {
                        default:
                            colSlider.color.r = f;
                            break;
                        case 1:
                            colSlider.color.g = f;
                            break;
                        case 2:
                            colSlider.color.b = f;
                            break;
                    }
                    colSlider.color = SmallUtils.Vector32RGB(SmallUtils.RWIIIClamp(SmallUtils.RGB2Vector3(colSlider.color), colormodel, out Vector3 clampedHSL));
                    colSlider.hslColor = SmallUtils.Vector32HSL(clampedHSL);
                }
                else if (colormodel == CustomColorModel.HSV)
                {
                    Vector3 hsv = ColConversions.HSL2HSV(SmallUtils.HSL2Vector3(colSlider.hslColor));
                    switch (OOO)
                    {
                        default:
                            hsv.x = Mathf.Clamp(f, 0, 0.99f);
                            break;
                        case 1:
                            hsv.y = f;
                            break;
                        case 2:
                            hsv.z = f;
                            break;
                    }
                    SmallUtils.RWIIIClamp(hsv, colormodel, out Vector3 clampedHSL);
                    colSlider.hslColor = SmallUtils.Vector32HSL(SmallUtils.RWIIIClamp(hsv, colormodel, out _));
                    colSlider.HSL2RGB();
                }
            }
            public static float OOOSwitchResult(int OOO, Vector3 values, Vector3 minClamp = default, Vector3 maxClamp = default)
            {
                minClamp = minClamp == default ? new Vector3(0, 0, 0) : minClamp;
                maxClamp = maxClamp == default ? new Vector3(1, 1, 1) : maxClamp;
                return OOO switch
                {
                    0 => Mathf.Clamp(values.x, minClamp.x, maxClamp.x),
                    1 => Mathf.Clamp(values.y, minClamp.y, maxClamp.y),
                    2 => Mathf.Clamp(values.z, minClamp.z, maxClamp.z),
                    _ => 0
                };
            }
            private readonly Dictionary<ColorChangeDialog.ColorSlider, NewMenuInterfaces.JollyCoopOOOConfigPages> configPages = new();
            private readonly List<ColorChangeDialog.ColorSlider> colSliders = new();
        }
        public class OpColorPickerHooks
        {
            public void Init()
            {
                try
                {
                    OtherOpColorPickerHooks();
                    IL.Menu.Remix.MixedUI.OpColorPicker.Change += IL_OPColorPicker_Change;
                    IL.Menu.Remix.MixedUI.OpColorPicker.MouseModeUpdate += IL_OPColorPickerMouseModeUpdate;
                    IL.Menu.Remix.MixedUI.OpColorPicker.GrafUpdate += IL_OPColorPickerGrafUpdate;
                    On.Menu.Remix.MixedUI.OpColorPicker.ctor += On_OPColorPickerCtor;
                    On.Menu.Remix.MixedUI.OpColorPicker._HSLSetValue += On_OPColorPicker_HSLSetValue;
                    On.Menu.Remix.MixedUI.OpColorPicker._RecalculateTexture += On_OPColorPickerRecalculateTexture;
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
            }
            private void IL_OPColorPicker_set_value(ILContext iL)
            {
                ILCursor cursor = new(iL);
                if (!cursor.TryGotoNext((x) => x.MatchLdarg(0), (x) => x.MatchLdarg(1), (x) => x.MatchCall<UIconfig>("set_value")))
                {
                    ColorConfigMod.DebugError("IL_OPColorPicker_set_value: Failed to rahhhh");
                    return;
                }
                ColorConfigMod.DebugILCursor("IL_OPColorPicker_Change: ", cursor);
                try
                {
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(new Action<OpColorPicker>((self) =>
                    {
                        if (ModOptions.HSVOpColorPicker)
                        {
                            Vector3 hsv = ColConversions.RGB2HSV(new(self._r / 100f, self._g / 100f, self._b / 100f));
                            self._h = Mathf.RoundToInt(hsv.x * 100f);
                            self._s = Mathf.RoundToInt(hsv.y * 100f);
                            self._l = Mathf.RoundToInt(hsv.z * 100f);
                        }
                    }));
                    ColorConfigMod.DebugLog("IL_OPColorPicker_Change: Successfully patched rahhh");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_OPColorPicker_set_value: Failed to patch rahhhhhh", e);
                }
            }
            private void IL_OPColorPicker_Change(ILContext iL)
            {
                ILCursor cursor = new(iL);
                if (!cursor.TryGotoNext(MoveType.After,(x) => x.MatchCall(typeof(Custom), "HSL2RGB"), 
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
                        if (ModOptions.HSVOpColorPicker)
                        {
                            self._cdis0.color = ColConversions.HSV2RGB(new(self._h / 100f, self._s / 100f, self._l / 100f));
                        }
                    }));
                    ColorConfigMod.DebugLog("IL_OPColorPicker_Change: Successfully patched _cdis0 Color");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_OPColorPicker_Change: Failed to patch _cdis0 Color", e);
                }
            }
            private void IL_OPColorPickerMouseModeUpdate(ILContext iL)
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
                        if (ModOptions.ChangeHSLOpColorPicker)
                        {
                            int sat = Mathf.RoundToInt(Mathf.Clamp(self.MousePos.x - 10f, 0f, 100f));
                            int litVal = Mathf.RoundToInt(Mathf.Clamp(self.MousePos.y - 30f, 0f, 100f));

                            self._lblR.text = self._h.ToString();
                            self._lblG.text = sat.ToString();
                            self._lblB.text = litVal.ToString();
                            self._cdis1.color = ModOptions.HSVOpColorPicker ?
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
                        if (ModOptions.ChangeHSLOpColorPicker)
                        {
                            int hue = Mathf.RoundToInt(Mathf.Clamp(self.MousePos.y - 30f, 0f, 99f));
                            self._lblR.text = hue.ToString();
                            self._lblG.text = self._s.ToString();
                            self._lblB.text = self._l.ToString();
                            self._cdis1.color = ModOptions.HSVOpColorPicker ?
                            ColConversions.HSV2RGB(new(hue / 100f, self._s / 100f, self._l / 100f)) :
                            ColConversions.HSL2RGB(new(hue / 100f, self._s / 100f, self._l / 100f));
                            if (self._h != hue)
                            {
                                self._h = hue;
                                self.PlaySound(SoundID.MENU_Scroll_Tick);
                                self._HSLSetValue();
                            }
                        }
                        return ModOptions.ChangeHSLOpColorPicker;

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
                        if (ModOptions.ChangeHSLOpColorPicker)
                        {
                            int hue = Mathf.RoundToInt(Mathf.Clamp(self.MousePos.y - 30f, 0f, 100f));
                            self._lblR.text = hue.ToString();
                            self._lblB.text = self._l.ToString();
                            self._cdis1.color = ModOptions.HSVOpColorPicker ? ColConversions.HSV2RGB(new(hue / 100f, self._s / 100f, self._l / 100f)) :
                            ColConversions.HSL2RGB(new(hue / 100f, self._s / 100f, self._l / 100f));
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
                        if (ModOptions.ChangeHSLOpColorPicker)
                        {
                            int sat = Mathf.RoundToInt(self.MousePos.x - 10f);
                            int lit = Mathf.RoundToInt(self.MousePos.y - 30f);
                            self._lblR.text = self._h.ToString();
                            self._lblG.text = sat.ToString();
                            self._lblB.text = lit.ToString();
                            self._cdis1.color = ModOptions.HSVOpColorPicker ? ColConversions.HSV2RGB(new(self._h / 100f, sat / 100f, lit / 100f)) : 
                            ColConversions.HSL2RGB(new(self._h / 100f, sat / 100f, lit / 100f));
                        }
                    }));
                    ColorConfigMod.DebugLog("IL_OPColorPickerMouseModeUpdate: Sucessfully patched HoverMouse for Sat and Lit!");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_OPColorPickerMouseModeUpdate: Failed to patch HoverMouse for Sat and Lit", e);
                }

            }
            private void IL_OPColorPickerGrafUpdate(ILContext iL)
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
                        if (ModOptions.ChangeHSLOpColorPicker)
                        {
                            self._lblR.color = self._lblB.color;
                            self._lblB.color = self.colorText;
                        }
                    }));
                    ColorConfigMod.DebugLog("IL_OPColorPickerGrafUpdate: Successfully patched Hue2Lit text color");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_OPColorPickerGrafUpdate: Failed to patch Hue2Lit text color", e);
                }
                try
                {
                    cursor.GotoNext(MoveType.After, (x) => x.MatchNewobj<Vector2>(), (x) => x.MatchCallvirt<GlowGradient>("set_pos"));
                    ColorConfigMod.DebugILCursor("IL_OPColorPickerGrafUpdate: ", cursor);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(new Action<OpColorPicker>((self) =>
                    {
                        if (ModOptions.ChangeHSLOpColorPicker && self.MenuMouseMode)
                        {
                            self._focusGlow.pos = new Vector2(104f, 25f);
                        }
                    }));
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
                        if (ModOptions.ChangeHSLOpColorPicker && self.MenuMouseMode)
                        {
                            self._lblR.color = self._lblB.color;
                            self._lblB.color = self.colorText;
                        }
                    }));
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
                        if (ModOptions.ChangeHSLOpColorPicker && self.MenuMouseMode)
                        {
                            self._focusGlow.pos = new Vector2(104f, 105f);
                        }
                    }));
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_OPColorPickerGrafUpdate: Failed to fix focus glow for lit2hue text", e);
                }
            }
            private void On_OPColorPickerCtor(On.Menu.Remix.MixedUI.OpColorPicker.orig_ctor orig, OpColorPicker self, Configurable<Color> config, Vector2 pos)
            {
                orig(self, config, pos);
                if (ModOptions.hsl2HSVOPColorPicker.Value)
                {
                    self._lblHSL.text = "HSV";
                }
            }
            private void On_OPColorPicker_HSLSetValue(On.Menu.Remix.MixedUI.OpColorPicker.orig__HSLSetValue orig, OpColorPicker self)
            {
                if (ModOptions.HSVOpColorPicker)
                {
                    Color color = ColConversions.HSV2RGB(new(self._h / 100f, self._s / 100f, self._l / 100f));
                    self._r = Mathf.RoundToInt(color.r * 100f);
                    self._g = Mathf.RoundToInt(color.g * 100f);
                    self._b = Mathf.RoundToInt(color.b * 100f);
                    self.value = Mathf.RoundToInt(self._r * 255f / 100f).ToString("X2") + 
                        Mathf.RoundToInt(self._g * 255f / 100f).ToString("X2") + 
                        Mathf.RoundToInt(self._b * 255f / 100f).ToString("X2");
                    return;
                }
                orig(self);
            }
            private void On_OPColorPickerRecalculateTexture(On.Menu.Remix.MixedUI.OpColorPicker.orig__RecalculateTexture orig, OpColorPicker self)
            {
                if (self._mode == OpColorPicker.PickerMode.HSL)
                {
                    if (ModOptions.ChangeHSLOpColorPicker)
                    {
                        self._ttre1 = new Texture2D(100, 101)
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
                            for (int sqrWidth = 0; sqrWidth < 100; sqrWidth++)
                            {
                                self._ttre1.SetPixel(sqrWidth, height, ModOptions.HSVOpColorPicker? 
                                    ColConversions.HSV2RGB(new(self._h / 100f, sqrWidth / 100f, height / 100f)):
                                    ColConversions.HSL2RGB(new(self._h / 100f, sqrWidth / 100f, height / 100f)));
                            }
                            for (int rectWidth = 0; rectWidth < 10; rectWidth++)
                            {
                                self._ttre2.SetPixel(rectWidth, height, ModOptions.HSVOpColorPicker ?
                                    ColConversions.HSV2RGB(new(height / 100f, self._s / 100f, self._l / 100f)) : 
                                    ColConversions.HSL2RGB(new(height / 100f, self._s / 100f, self._l / 100f)));
                            }
                        }
                        self._ttre1.Apply();
                        self._ttre2.Apply();
                        return;
                    }
                }
                orig(self);
            }
            private void On_OPColorPickerUpdate(On.Menu.Remix.MixedUI.OpColorPicker.orig_Update orig, OpColorPicker self)
            {
                orig(self);
                if (!self.greyedOut && !self.held && self._MouseOverHex())
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
            private void CopyCPicker(OpColorPicker cPicker)
            {
                MenuToolObj.SystemClipboard = cPicker.value;
                cPicker?.PlaySound(SoundID.MENU_Player_Join_Game);
            }
            private void PasteCPicker(OpColorPicker cPicker)
            {
                if (cPicker.CopyFromClipboard(MenuToolObj.SystemClipboard))
                {
                    cPicker.value = MenuToolObj.SystemClipboard.TrimStart('#').Substring(0, 6).ToUpper();
                    cPicker.PlaySound(SoundID.MENU_Switch_Page_In);
                    return;
                }
                cPicker.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);

            }

        }
    }
    public static class NewMenuInterfaces
    {
        public class JollyCoopOOOConfigPages : MenuObject
        {
            public JollyCoopOOOConfigPages(Menu.Menu menu, ColorChangeDialog.ColorSlider owner, int bodyPartNum) : base(menu, owner)
            {
                if (pages == null)
                {
                    
                }
            }
            public SliderOOOPages pages;
        }
        public class SlugcatDisplay : RectangularMenuObject
        {
            public SlugcatDisplay(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, SlugcatStats.Name current, List<string> bodyCols) : base(menu, owner, pos, size)
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
                List<string> txtFilepaths = AssetManager.ListDirectory("colorconfig").Where(x => x.EndsWith(".txt")).ToList();
                foreach (string txtpath in txtFilepaths)
                {
                    string resolvedPath = AssetManager.ResolveFilePath(txtpath);
                    if (File.Exists(resolvedPath))
                    {
                        foreach (string line in File.ReadAllLines(resolvedPath, System.Text.Encoding.UTF8))
                        {
                            if (line.StartsWith(name.value) && line.Contains(":"))
                            {
                                string[] bodyDirectory = line.Split(':');
                                if (bodyDirectory.Length > 1 && bodyDirectory[1]?.Contains("|") == true)
                                {
                                    foreach (string body in bodyDirectory[1].Split(','))
                                    {
                                        if (body.Contains("|"))
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
                    string file = "";
                    if (!preSetFilesToLoad.ContainsKey(bodyNames[i]))
                    {
                        file = i switch
                        {
                            0 => File.Exists(AssetManager.ResolveFilePath("illustrations/" + name.value + "_pup_off.png")) ? name.value + "_pup_off" : "pup_off",
                            1 => File.Exists(AssetManager.ResolveFilePath("illustrations/" + "face_" + name.value + "_pup_off.png")) ? $"face_{name.value}_pup_off" : "face_pup_off",
                            2 => File.Exists(AssetManager.ResolveFilePath($"illustrations/unique_{name.value}_pup_off.png"))? $"unique_{ name.value }_pup_off" : "colorconfig_showcasesquare",
                            _ => File.Exists(AssetManager.ResolveFilePath($"illustrations/{bodyNames[i]}_{name.value}_pup_off"))? $"{bodyNames[i]}_{name.value}_pup_off" : "colorconfig_showcasesquare",
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
                    MenuIllustration body = new(menu, this, folder, file, file == "colorconfig_showcasesquare"? new(i * 10, -0.7f) : size / 2, true, true);
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
            public void LoadNewHSLStringSlugcat(List<string> slugcatColos, SlugcatStats.Name name)
            {
                bodyColors = slugcatColos;
                currentSlugcat = name;
                if (currentSlugcat != prevSlugcat)
                {
                    bodyNames = PlayerGraphics.ColoredBodyPartList(name);
                }
                List<Vector3> hsls = new();
                for (int i = 0; i < bodyNames.Count; i++)
                {
                    hsls.Add(new(1, 1, 1));
                }
                for (int i = 0; i < bodyColors.Count; i++)
                {
                    if (bodyColors[i].Contains(","))
                    {
                        string[] hslArray = bodyColors[i].Split(',');
                        hsls[i] = new Vector3(float.Parse(hslArray[0], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(hslArray[1], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(hslArray[2], NumberStyles.Any, CultureInfo.InvariantCulture));
                    }
                }
                currentHSLs = hsls;
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                if (prevSlugcat != currentSlugcat)
                {
                    prevSlugcat = currentSlugcat;
                    LoadIcon(currentSlugcat, bodyNames);
                }
                if (sprites?.Count > 0 && currentHSLs != null)
                {
                    if (currentHSLs != prevHSLs)
                    {
                        prevHSLs = currentHSLs;
                        for (int i = 0; i < currentHSLs.Count; i++)
                        {
                            if (sprites.Count - 1 < i)
                            {
                                continue;
                            }
                            sprites[i].color = ColConversions.HSL2RGB(currentHSLs[i]);
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
            public List<Vector3> currentHSLs, prevHSLs;
            public List<string> bodyNames;
            public List<string> bodyColors;
            public List<MenuIllustration> sprites;
        }
        public class SliderOOOPages : MenuObject, ICanTurnPages
        {
            public SliderOOOPages(Menu.Menu menu, MenuObject owner, HorizontalSlider slider1, HorizontalSlider slider2, HorizontalSlider slider3, List<SliderOOOIDGroup> sliderOOOIDGroups,Vector2 buttonOffset = default) : base(menu, owner)
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
                OOOIDGroups = sliderOOOIDGroups?.Count == 0? new() 
                { new(slider1.ID, slider2.ID, slider3.ID, null, null)} : sliderOOOIDGroups;
                buttonOffset = buttonOffset == default ? new(0, 0) : buttonOffset;
                setPrevButtonPos = new(buttonOffset.x + slider3.pos.x, -(slider3.size.y * 2) + buttonOffset.y + slider3.pos.y);
                if (PagesOn)
                {
                    ActivateButtons();
                }
            }
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
                    if (OOOIDGroups?.Count > 0 && OOOIDGroups[currentOffset] != null && showValues)
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
            public BigSimpleButton PrevButton
            {
                get => prevButton;
            }
            public BigSimpleButton NextButton
            {
                get => nextButton;
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
            public void NextPage()
            {
                currentOffset++;
                if (OOOIDGroups == null || OOOIDGroups?.Count == 0 ||currentOffset > OOOIDGroups.Count - 1)
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
            public void ForceOwnerSlider()
            {

                if (sliderO != null && !subObjects.Contains(sliderO))
                {
                    sliderO.menu = menu;
                    sliderO.owner = this;
                    subObjects.Add(sliderO);
                }
                if (sliderOO != null && !subObjects.Contains(sliderOO))
                {
                    sliderOO.menu = owner.menu;
                    sliderOO.owner = this;
                    subObjects.Add(sliderOO);
                }
                if (sliderOOO != null && !subObjects.Contains(sliderOOO))
                {
                    sliderOOO.menu = owner.menu;
                    sliderOOO.owner = this;
                    subObjects.Add(sliderOOO);
                }
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                if (OOOIDGroups != null && OOOIDGroups[currentOffset] != null)
                {
                    if (sliderO != null)
                    {
                        if (sliderO.menuLabel != null)
                        {
                            sliderO.menuLabel.text = menu.Translate(OOOIDGroups[currentOffset].name1) + " " + SliderValues[0];
                        }
                    }
                    if (sliderOO != null)
                    {
                        if (sliderOO.menuLabel != null)
                        {
                            sliderOO.menuLabel.text = menu.Translate(OOOIDGroups[currentOffset].name2) + " " + SliderValues[1];
                        }
                    }
                    if (sliderOOO != null)
                    {
                        if (sliderOOO.menuLabel != null)
                        {
                            sliderOOO.menuLabel.text = menu.Translate(OOOIDGroups[currentOffset].name3) + " " + SliderValues[2];
                        }
                    }
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

            private int currentOffset;
            public Vector2 setPrevButtonPos;
            public bool showValues = true, showSign = true;
            public HorizontalSlider sliderO, sliderOO, sliderOOO;
            private BigSimpleButton prevButton, nextButton;
            public List<SliderOOOIDGroup> OOOIDGroups { get; private set; }
        }
        public class HexTypeBox : PositionedMenuObject, ICopyPasteConfig
        {
            public HexTypeBox(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
            {
                shouldUpdateNewHSL = false;
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
                get => MenuToolObj.SystemClipboard;
                set => MenuToolObj.SystemClipboard = value;
            }
            public void SaveNewHSL(Vector3 hsl)
            {
                currentHSL = hsl;
            }
            public void Copy()
            {
                MenuToolObj.SystemClipboard = hexTyper.value;
                menu.PlaySound(SoundID.MENU_Player_Join_Game);
            }
            public void Paste()
            {
                string pendingVal = new(MenuToolObj.SystemClipboard.Where(x => x != '#').ToArray());
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
                    newPendingHSL = SmallUtils.FixHexSliderWonkiness(newClampedHSL, currentHSL);
                    shouldUpdateNewHSL = true;
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
                                hexTyper.value = ColorUtility.ToHtmlStringRGB(
                                    SmallUtils.Vector32RGB(SmallUtils.RWIIIClamp(
                                        SmallUtils.RGB2Vector3(hexCol), CustomColorModel.RGB, out Vector3 clampedHSLHex)));
                                if (clampedHSLHex != currentHSL)
                                {
                                    newPendingHSL = SmallUtils.FixHexSliderWonkiness(clampedHSLHex, currentHSL);
                                    shouldUpdateNewHSL = true;
                                    currentHSL = newPendingHSL;
                                    prevHSL = newPendingHSL;
                                }
                                lastValue = hexTyper.value;
                            }
                        }
                    }
                    if (hexTyper.MouseOver)
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
            public bool shouldUpdateNewHSL;
            public Vector3 currentHSL, prevHSL, newPendingHSL;
            public MenuTabWrapper tabWrapper;
            public UIelementWrapper elementWrapper;
            public OpTextBox hexTyper;
        }
        public class SliderOOOIDGroup
        {
            public SliderOOOIDGroup(Slider.SliderID sliderID1, Slider.SliderID sliderID2, Slider.SliderID sliderID3,
                string[] names,bool[] showInts, Vector3 multipler = default)
            {
                ID1 = sliderID1;
                ID2 = sliderID2;
                ID3 = sliderID3;
                name1 = names?.Length > 0? names[0] : "";
                name2 = names?.Length > 1? names[1] : "";
                name3 = names?.Length > 2? names[2] : "";
                showInt1 = showInts?.Length > 0 && showInts[0];
                showInt2 = showInts?.Length > 1 && showInts[1];
                showInt3 = showInts?.Length > 2 && showInts[2];
                showMultipler = multipler == default? new(1, 1, 1) : multipler;
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
        public interface ICanTurnPages
        {
            bool PagesOn { get;}
            void PopulatePage(int offset);
            void NextPage();
            void PrevPage();
        }
        public interface ICopyPasteConfig
        {
            string Clipboard { get; set; }
            void Copy();
            void Paste();
        }
    }
}
