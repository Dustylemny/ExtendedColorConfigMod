using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Menu;
using Menu.Remix.MixedUI;
using JollyCoop.JollyMenu;
using RWCustom;
using UnityEngine;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ColorConfig.MenuUI;
using ColorConfig.WeakUITable;
namespace ColorConfig
{
    public static partial class ColorConfigHooks
    {
        public static void Init()
        {
            Menu_Hooks();
            SlugcatSelectMenu_Hooks();
            ExpeditionMenu_Hooks();
            JollyCoopMenu_Hooks();
            OpColorPicker_Hooks();
            ExternalModHooks();
        }
        public static void Menu_Hooks()
        {
            try
            {
                On.Menu.Menu.Update += On_Menu_Update;
                On.Menu.MenuObject.Update += On_MenuObject_Update;
                ColorConfigMod.DebugLog("Successfully initialized Menuobject hooks");
            }
            catch (Exception ex)
            {
                ColorConfigMod.DebugException("Failed to initialize Menuobject hooks", ex);
            }
        }
        public static void On_Menu_Update(On.Menu.Menu.orig_Update orig, Menu.Menu self)
        {
            self.GetInputExtras().UpdateInputs();
            orig(self);
        }
        public static void On_MenuObject_Update(On.Menu.MenuObject.orig_Update orig, MenuObject self)
        {
            orig(self);
            self.UpdateExtraInterfaces(self.menu);
        }

        //slugcat select menu hooks
        public static void SlugcatSelectMenu_Hooks()
        {
            try
            {
                On.Menu.SlugcatSelectMenu.AddColorButtons += On_SlugcatSelectMenu_AddColorButtons;
                On.Menu.SlugcatSelectMenu.RemoveColorButtons += On_SlugcatSelectMenu_RemoveColorButtons;
                On.Menu.SlugcatSelectMenu.AddColorInterface += On_SlugcatSelectMenu_AddColorInterface;
                On.Menu.SlugcatSelectMenu.RemoveColorInterface += On_SlugcatSelectMenu_RemoveColorInterface;
                On.Menu.SlugcatSelectMenu.Update += On_SlugcatSelectMenu_Update;
                On.Menu.SlugcatSelectMenu.ValueOfSlider += On_SlugcatSelectMenu_ValueOfSlider;
                On.Menu.SlugcatSelectMenu.SliderSetValue += On_SlugcatSelectMenu_SliderSetValue;
                ColorConfigMod.DebugLog("Sucessfully extended color interface for slugcat select menu!");
            }
            catch (Exception ex)
            {
                ColorConfigMod.DebugException("Failed to initialize slugcat select menu hooks", ex);
            }
        }
        public static void On_SlugcatSelectMenu_AddColorButtons(On.Menu.SlugcatSelectMenu.orig_AddColorButtons orig, SlugcatSelectMenu self)
        {
            orig(self);
            if (ModOptions.Instance.EnableSlugcatDisplay.Value && ModOptions.Instance.EnableLegacyIdeaSlugcatDisplay.Value && self.GetExtraSSMInterface().slugcatDisplay == null)
            {
                Vector2 vector = self.manager.BaseScreenColorInterfacePos();
                vector.y -= (ModManager.JollyCoop ? 40 : 0) + (self.colorInterface != null ? self.colorInterface.bodyColors.Length * 40 : 0);
                self.GetExtraSSMInterface().slugcatDisplay = new(self, self.pages[0], new(vector.x + 140, vector.y + 40), new(45f, 45f), self.StorySlugcat());
                self.pages[0].subObjects.Add(self.GetExtraSSMInterface().slugcatDisplay);
            }
        }
        public static void On_SlugcatSelectMenu_RemoveColorButtons(On.Menu.SlugcatSelectMenu.orig_RemoveColorButtons orig, SlugcatSelectMenu self)
        {
            //self.TryFixColorChoices(self.StorySlugcat());
            orig(self);
            self.pages[0].ClearMenuObject(ref self.GetExtraSSMInterface().slugcatDisplay);
        }
        public static void On_SlugcatSelectMenu_AddColorInterface(On.Menu.SlugcatSelectMenu.orig_AddColorInterface orig, SlugcatSelectMenu self)
        {
            orig(self);
            AddExtraSSMInterfaces(self, self.GetExtraSSMInterface(), self.StorySlugcat());
        }
        public static void On_SlugcatSelectMenu_RemoveColorInterface(On.Menu.SlugcatSelectMenu.orig_RemoveColorInterface orig, SlugcatSelectMenu self)
        {
            orig(self);
            //self.TryFixColorChoices(self.StorySlugcat());
            RemoveExtraSSMInterface_ColorInterface(self, self.GetExtraSSMInterface());
        }
        public static void On_SlugcatSelectMenu_Update(On.Menu.SlugcatSelectMenu.orig_Update orig, SlugcatSelectMenu self)
        {
            orig(self);
            UpdateExtraSSMInterfaces(self, self.GetExtraSSMInterface());
        }
        public static float On_SlugcatSelectMenu_ValueOfSlider(On.Menu.SlugcatSelectMenu.orig_ValueOfSlider orig, SlugcatSelectMenu self, Slider slider)
        {
            if (MenuToolObj.CustomHSLValueOfSlider(slider, self.SlugcatSelectMenuHSL(), out float f))
            {
                return f;
            }
            return orig(self, slider);
        }
        public static void On_SlugcatSelectMenu_SliderSetValue(On.Menu.SlugcatSelectMenu.orig_SliderSetValue orig, SlugcatSelectMenu self, Slider slider, float f)
        {
            MenuToolObj.CustomSliderSetHSL(slider, f, self.SlugcatSelectMenuHSL(), self.SaveHSLString_Story);
            orig(self, slider, f);
        }
        public static void UpdateExtraSSMInterfaces(SlugcatSelectMenu ssM, ExtraSSMInterfaces extraInterfaces)
        {
            extraInterfaces.hexInterface?.SaveNewHSL(ssM.SlugcatSelectMenuHSL());
            extraInterfaces.legacyHexInterface?.SaveNewHSLs(ssM.SlugcatSelectMenuHSLs());
            extraInterfaces.slugcatDisplay?.LoadNewHSLStringSlugcat(ssM.manager.rainWorld.progression.miscProgressionData.colorChoices[ssM.StorySlugcat().value]/*, ssM.slugcatColorOrder[ssM.slugcatPageIndex]*/);
        }
        public static void AddExtraSSMInterfaces(SlugcatSelectMenu ssM, ExtraSSMInterfaces extraSSMInterfaces, SlugcatStats.Name name)
        {
            AddSSMSliderInterface(ssM, extraSSMInterfaces, SSMSliderIDGroups);
            AddOtherSSMInterface(ssM, extraSSMInterfaces, name);
        }
        public static void AddSSMSliderInterface(SlugcatSelectMenu ssM, ExtraSSMInterfaces extraInterfaces, List<SliderIDGroup> sliderOOOIDGroups)
        {
            if (ModOptions.ShouldAddSSMLegacySliders && extraInterfaces.legacySliders == null)
            {
                extraInterfaces.legacySliders = new(ssM, ssM.pages[0], ssM.defaultColorButton.pos + new Vector2(0, -40), new(0, -40), new(200, 40), [.. sliderOOOIDGroups.Exclude(0)], showValue: ModOptions.ShowVisual, rounding: ModOptions.Instance.SliderRounding.Value, dec: ModOptions.DeCount);
                ssM.pages[0].subObjects.Add(extraInterfaces.legacySliders);
                ssM.MutualVerticalButtonBind(extraInterfaces.legacySliders.sliderO, ssM.defaultColorButton);
                ssM.MutualVerticalButtonBind(ssM.nextButton, extraInterfaces.legacySliders.oOOPages.PagesOn ? extraInterfaces.legacySliders.oOOPages.prevButton : extraInterfaces.legacySliders.sliderOOO);
            }
            if (extraInterfaces.sliderPages == null)
            {
                extraInterfaces.sliderPages = new(ssM, ssM.pages[0], [ssM.hueSlider, ssM.satSlider, ssM.litSlider], extraInterfaces.legacySliders != null ? [sliderOOOIDGroups[0]] : sliderOOOIDGroups, new(0, 25))
                {
                    showValues = ModOptions.ShowVisual,
                    roundingType = ModOptions.Instance.SliderRounding.Value,
                    DecimalCount = ModOptions.DeCount,
                };
                ssM.pages[0].subObjects.Add(extraInterfaces.sliderPages);
                extraInterfaces.sliderPages.PopulatePage(extraInterfaces.sliderPages.CurrentOffset);
                if (extraInterfaces.sliderPages.PagesOn)
                {
                    ssM.defaultColorButton.pos.y -= 40;
                    ssM.MutualVerticalButtonBind(ssM.defaultColorButton, extraInterfaces.sliderPages.prevButton);
                    ssM.MutualVerticalButtonBind(extraInterfaces.sliderPages.nextButton, ssM.litSlider);
                    extraInterfaces.sliderPages.nextButton.MenuObjectBind(ssM.defaultColorButton, bottom: true);
                    extraInterfaces.sliderPages.prevButton.MenuObjectBind(ssM.litSlider, top: true);
                }
            }
        }
        public static void AddOtherSSMInterface(SlugcatSelectMenu ssM, ExtraSSMInterfaces extraInterfaces, SlugcatStats.Name name)
        {
            if (ModOptions.Instance.EnableSlugcatDisplay.Value && extraInterfaces.slugcatDisplay == null)
            {
                extraInterfaces.slugcatDisplay = new(ssM, ssM.pages[0], new(ssM.satSlider.pos.x + 140, ssM.satSlider.pos.y + 80), new(45f, 45f), name);
                ssM.pages[0].subObjects.Add(extraInterfaces.slugcatDisplay);
            }
            if (ModOptions.Instance.EnableHexCodeTypers.Value)
            {
                if (ModOptions.Instance.EnableLegacyHexCodeTypers.Value && extraInterfaces.legacyHexInterface == null)
                {
                    extraInterfaces.legacyHexInterface = new(ssM, ssM.pages[0], ssM.defaultColorButton.pos + new Vector2(ssM.defaultColorButton.size.x + 10, 0), PlayerGraphics.ColoredBodyPartList(name))
                    {
                        applyChanges = (hexTyper, hsl, rgb, bodyNum) =>
                        {
                            ssM.SaveHSLString_Story_Int(bodyNum, hsl);
                            ssM.hueSlider.UpdateSliderValue();
                            ssM.satSlider.UpdateSliderValue();
                            ssM.litSlider.UpdateSliderValue();
                            ssM.colorInterface?.UpdateInterfaceColor(bodyNum);
                        }
                    };
                    ssM.pages[0].subObjects.Add(extraInterfaces.legacyHexInterface);
                }
                if (extraInterfaces.legacyHexInterface == null && extraInterfaces.hexInterface == null)
                {
                    extraInterfaces.hexInterface = new(ssM, ssM.pages[0], ssM.defaultColorButton.pos + new Vector2(ssM.defaultColorButton.size.x + 10, 0))
                    {
                        saveNewTypedColor = (hexTyper, hsl, rgb) => { ssM.SaveHSLString_Story(hsl); ssM.hueSlider.UpdateSliderValue(); ssM.satSlider.UpdateSliderValue(); ssM.litSlider.UpdateSliderValue(); }
                    };
                    ssM.pages[0].subObjects.Add(extraInterfaces.hexInterface);
                    extraInterfaces.hexInterface.elementWrapper.MenuObjectBind(extraInterfaces.sliderPages?.PagesOn == true? extraInterfaces.sliderPages.nextButton : ssM.litSlider, top: true);
                    extraInterfaces.hexInterface.elementWrapper.MenuObjectBind(extraInterfaces.legacySliders != null ? extraInterfaces.legacySliders.sliderO : ssM.nextButton, bottom: true);
                }
            }
        }
        public static void RemoveExtraSSMInterface_ColorInterface(SlugcatSelectMenu ssM, ExtraSSMInterfaces extraInterfaces)
        {
            ssM.pages[0].ClearMenuObject(ref extraInterfaces.legacySliders);
            ssM.pages[0].ClearMenuObject(ref extraInterfaces.sliderPages);
            if (!ModOptions.Instance.EnableLegacyIdeaSlugcatDisplay.Value)
            {
                ssM.pages[0].ClearMenuObject(ref extraInterfaces.slugcatDisplay);
            }
            ssM.pages[0].ClearMenuObject(ref extraInterfaces.hexInterface);
            ssM.pages[0].ClearMenuObject(ref extraInterfaces.legacyHexInterface);
        }
        public static List<SliderIDGroup> SSMSliderIDGroups
        {
            get
            {
                List<SliderIDGroup> result = [];
                SmallUtils.AddSSMSliderIDGroups(result, ModOptions.ShouldRemoveHSLSliders);
                return result;
            }
        }

        //jolly-menu
        public static void JollyCoopMenu_Hooks()
        {
            try
            {
                On.JollyCoop.JollyMenu.ColorChangeDialog.ValueOfSlider += On_ColorChangeDialog_ValueOfSlider;
                On.JollyCoop.JollyMenu.ColorChangeDialog.SliderSetValue += On_ColorChangeDialog_SliderSetValue;
                On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.ctor += On_ColorChangeDialog_ColorSlider_ctor;
                On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.RemoveSprites += On_ColorChangeDialog_ColorSlider_RemoveSprites;
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
        public static void On_ColorChangeDialog_ColorSlider_RemoveSprites(On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_RemoveSprites orig, ColorChangeDialog.ColorSlider self)
        {
            orig(self);
            self.RemoveExtraJollyInterface();
        }
        public static float On_ColorChangeDialog_ValueOfSlider(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_ValueOfSlider orig, ColorChangeDialog self, Slider slider)
        {
            if (ValueOfCustomSliders(slider, out float f))
            {
                return f;
            }
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

        //opcolorconfig
        public static void OpColorPicker_Hooks()
        {
            try
            {
                OtherOpColorPickerHooks();
                IL.Menu.Remix.MixedUI.OpColorPicker.Change += IL_OPColorPicker_Change;
                IL.Menu.Remix.MixedUI.OpColorPicker._HSLSetValue += IL_OPColorPicker__HSLSetValue;
                IL.Menu.Remix.MixedUI.OpColorPicker.MouseModeUpdate += IL_OPColorPicker__MouseModeUpdate;
                IL.Menu.Remix.MixedUI.OpColorPicker._NonMouseModeUpdate += IL_OPColorPicker__NonMouseModeUpdate;
                IL.Menu.Remix.MixedUI.OpColorPicker.GrafUpdate += IL_OPColorPicker_GrafUpdate;
                On.Menu.Remix.MixedUI.OpColorPicker.ctor += On_OPColorPicker_Ctor;
                On.Menu.Remix.MixedUI.OpColorPicker._MouseTrySwitchMode += On_OPColorPicker__MouseTrySwitchMode;
                On.Menu.Remix.MixedUI.OpColorPicker._RecalculateTexture += On_OPColorPickerRecalculateTexture;
                On.Menu.Remix.MixedUI.OpColorPicker.Update += On_OPColorPicker_Update;
                On.Menu.Remix.MixedUI.OpColorPicker.GrafUpdate += On_OPColorPicker_GrafUpdate;
                ColorConfigMod.DebugLog("Successfully extended color interface for OpColorPicker!");
            }
            catch (Exception ex)
            {
                ColorConfigMod.DebugException("Failed to initialize colorpicker hooks", ex);
            }
        }
        public static void OtherOpColorPickerHooks()
        {
            typeof(OpColorPicker).GetProperty(nameof(OpColorPicker.@value)).GetSetMethod().ILHookMethod(IL_OPColorPicker_set_value);
            typeof(OpColorPicker).GetProperty(nameof(OpColorPicker.@value)).GetSetMethod().HookMethod(delegate (Action<OpColorPicker, string> orig, OpColorPicker self, string newValue)
            {
                if ((ModOptions.Instance.EnableBetterOPColorPicker.Value || ModOptions.Instance.EnableDiffOpColorPickerHSL.Value) && self._mode == OpColorPicker.PickerMode.HSL)
                    self.RefreshTexture();
                orig(self, newValue);
            });
        }
        public static void IL_OPColorPicker_set_value(ILContext il)
        {
            try
            {
                ILCursor cursor = new(il);
                cursor.TryGotoNext(MoveType.After, (x) => x.MatchCall<RXColor>("HSLFromColor"));
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate(delegate (RXColorHSL rXHSL, OpColorPicker self, string newVal)
                {
                    rXHSL = ModOptions.PickerHSVMode ? ColConversions.HSL2HSV(rXHSL.RXHSl2Vector3()).Vector32RXHSL() : rXHSL;
                    if (ModOptions.Instance.EnableBetterOPColorPicker.Value)
                    {
                        if (self._mode == OpColorPicker.PickerMode.HSL && (self._curFocus == OpColorPicker.MiniFocus.HSL_Hue || self._curFocus == OpColorPicker.MiniFocus.HSL_Saturation || self._curFocus == OpColorPicker.MiniFocus.HSL_Lightness))
                        {
                            rXHSL = self.GetHSL01().Vector32RXHSL();
                        }
                    }
                    return rXHSL;
                });
                ColorConfigMod.DebugLog("Patched RXHSLValues from hsv to hsl and that matches the values selected");
            }
            catch (Exception e)
            {
                ColorConfigMod.DebugException("Failed to patch set_value for hsv and better opcolor pickers", e);
            }
        }
        public static void IL_OPColorPicker_Change(ILContext il)
        {
            try
            {
                ILCursor cursor = new(il);
                cursor.GotoNext(MoveType.After, (x) => x.MatchCall(typeof(Custom), "HSL2RGB"));
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(delegate (Color hslRGB, OpColorPicker self)
                {
                    return self.IsHSVMode()? ColConversions.HSV2RGB(self.GetHSL01()) : self._h == 100 ? ColConversions.HSL2RGB(self.GetHSL01()) : hslRGB;
                });
                ColorConfigMod.DebugLog("Sucessfully patched _cdis0 Color for HSV and rect hue selector!");
            }
            catch (Exception e)
            {
                ColorConfigMod.DebugException("Failed to patch _cdis0 Color for HSV and rect hue selector", e);
            }
        }
        public static void IL_OPColorPicker__HSLSetValue(ILContext il)
        {
            try
            {
                ILCursor cursor = new(il);
                cursor.GotoNext(MoveType.After, x => x.MatchCall(typeof(Custom), "HSL2RGB"));
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(delegate (Color hslCol, OpColorPicker self)
                {
                    return self.IsHSVMode() ? ColConversions.HSV2RGB(self.GetHSL01()) : ModOptions.Instance.EnableBetterOPColorPicker.Value && self._h == 100 ? ColConversions.HSL2RGB(self.GetHSL01()) : hslCol;
                });
                ColorConfigMod.DebugLog("Sucessfully patched RGB Color to take hsv or not turn grey!");
            }
            catch (Exception e)
            {
                ColorConfigMod.DebugException("Failed to patch RGB Color ", e);
            }
        }
        public static void IL_OPColorPicker__MouseModeUpdate(ILContext il)
        {
            try
            {
                ILCursor cursor = new(il);
                GotoHSL(cursor);
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc_3);
                cursor.Emit(OpCodes.Ldloc, 4);
                cursor.EmitDelegate(delegate (OpColorPicker self, int hueSat, int satLit)
                {
                    return self.OpColorPickerPatchMiniFocusHSLColor(hueSat, satLit, 0, true);
                });
                MakeAReturn(cursor);
                ColorConfigMod.DebugLog("Sucessfully patched MiniFocus HueSat to SatHue!");
                GotoHSL(cursor);
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc, 5);
                cursor.EmitDelegate(delegate (OpColorPicker self, int litHue)
                {
                    return self.OpColorPickerPatchMiniFocusHSLColor(0, 0, litHue, false);
                });
                ColorConfigMod.DebugLog("Sucessfully patched MiniFocus for Lit to Hue!");
                MakeAReturn(cursor);
                GotoHSL(cursor);//, false);
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc, 11);
                cursor.EmitDelegate(delegate (OpColorPicker self, int litHue)
                {
                    self.OpColorPickerPatchHoverMouseHSLColor(0, 0, litHue, false);
                });
                ColorConfigMod.DebugLog("Sucessfully patched HoverMouse for Lit to Hue!");
                cursor.GotoNext(x => x.MatchCall(typeof(Custom), "HSL2RGB"));
                cursor.GotoNext(MoveType.After, x => x.MatchCallvirt<FSprite>("set_color"));
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc, 12);
                cursor.Emit(OpCodes.Ldloc, 13);
                cursor.EmitDelegate(delegate (OpColorPicker self, int hueSat, int satLit)
                {
                    self.OpColorPickerPatchHoverMouseHSLColor(hueSat, satLit, 0, true);
                });
                ColorConfigMod.DebugLog("Sucessfully patched HoverMouse for HueSat to SatLit!");
            }
            catch (Exception ex)
            {
                ColorConfigMod.DebugException("Failed to patch for changing colors using mouse", ex);
            }

        }
        public static void IL_OPColorPicker__NonMouseModeUpdate(ILContext il)
        {
            try
            {
                ILCursor cursor = new(il);
                cursor.GotoNext(MoveType.After, x => x.MatchStloc(1));
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc_1);
                cursor.EmitDelegate(delegate (OpColorPicker self, OpColorPicker.PickerMode newMode)
                {
                    if (ModOptions.Instance.EnableRotatingOPColorPicker.Value && self._mode == OpColorPicker.PickerMode.HSL && newMode == self._mode && (ModOptions.PickerHSVMode || ModOptions.Instance.EnableDiffOpColorPickerHSL.Value))
                    {
                        self.SwitchHSLCustomMode();
                        ColorConfigMod.DebugLog("Sucessfully patched for switch mode using non-mouse");
                        return true;
                    }
                    return false;
                });
                MakeAReturn(cursor);
            }
            catch (Exception ex)
            {
                ColorConfigMod.DebugException("Failed to patch for switch mode using non-mouse", ex);
            }
        }
        public static void IL_OPColorPicker_GrafUpdate(ILContext il)
        {
            try
            {
                ILCursor cursor = new(il);
                cursor.GotoNext(x => x.Match(OpCodes.Sub), (x) => x.Match(OpCodes.Switch));
                cursor.GotoNext(x => x.MatchCall<UIelement>("get_MenuMouseMode"));
                cursor.GotoNext(MoveType.After, x => x.MatchCallvirt<FLabel>("set_color"));
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(delegate (OpColorPicker self)
                {
                    //hueSatToSatHueTextFix
                    if (ModOptions.Instance.EnableDiffOpColorPickerHSL.Value)
                    {
                        self._lblB.color = self._lblR.color;
                        self._lblR.color = self.colorText;
                    }
                });
                ColorConfigMod.DebugLog("Successfully patched hue2lit text color");
                cursor.GotoNext(x => x.MatchCallvirt<GlowGradient>("set_pos"));
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(delegate (Vector2 origFocus, OpColorPicker self)
                {
                    return ModOptions.Instance.EnableDiffOpColorPickerHSL.Value && self.MenuMouseMode ? new(104, 25) : origFocus;
                });
                ColorConfigMod.DebugLog("Successfully patched focus glow for hue2lit text");
                cursor.GotoNext(x => x.MatchLdarg(0), x => x.MatchLdfld<OpColorPicker>(nameof(OpColorPicker._lblB)));
                cursor.GotoNext(MoveType.After, x => x.MatchCallvirt<FLabel>("set_color"));
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(delegate (OpColorPicker self)
                {
                    if (ModOptions.Instance.EnableDiffOpColorPickerHSL.Value && self.MenuMouseMode)
                    {
                        self._lblR.color = self._lblB.color;
                        self._lblB.color = self.colorText;
                    }
                });
                ColorConfigMod.DebugLog("Successfully patched lit2hue text color");
                cursor.GotoNext((x) => x.MatchCallvirt<GlowGradient>("set_pos"));
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(delegate (Vector2 origFocus, OpColorPicker self)
                {
                    return ModOptions.Instance.EnableDiffOpColorPickerHSL.Value && self.MenuMouseMode ? new(104f, 105f) : origFocus;
                });
                ColorConfigMod.DebugLog("Successfully patched focus glow for lit2hue text");
            }
            catch (Exception ex)
            {
                ColorConfigMod.DebugException("Failed to fix focus glow pos and text color, not a big issue tho", ex);
            }
        }
        public static void On_OPColorPicker_Ctor(On.Menu.Remix.MixedUI.OpColorPicker.orig_ctor orig, OpColorPicker self, Configurable<Color> config, Vector2 pos)
        {
            self.GetColorPickerExtras()._IsHSVMode = ModOptions.PickerHSVMode;
            self.GetColorPickerExtras()._IsDifferentHSLHSVMode = ModOptions.Instance.EnableDiffOpColorPickerHSL.Value;
            orig(self, config, pos);
            self._lblHSL.text = self.IsHSVMode() ? "HSV" : self._lblHSL.text;
        }
        public static void On_OPColorPicker__MouseTrySwitchMode(On.Menu.Remix.MixedUI.OpColorPicker.orig__MouseTrySwitchMode orig, OpColorPicker self, OpColorPicker.PickerMode newMode)
        {
            if (ModOptions.Instance.EnableRotatingOPColorPicker.Value && self._mode == OpColorPicker.PickerMode.HSL && newMode == self._mode && (ModOptions.PickerHSVMode || ModOptions.Instance.EnableDiffOpColorPickerHSL.Value))
            {
                self.SwitchHSLCustomMode();
                return;
            }
            orig(self, newMode);
        }
        public static void On_OPColorPickerRecalculateTexture(On.Menu.Remix.MixedUI.OpColorPicker.orig__RecalculateTexture orig, OpColorPicker self)
        {
            bool diff = self.IsDiffHSLHSVMode();
            if (self._mode == OpColorPicker.PickerMode.HSL && (diff || self.IsHSVMode()))
            {
                self._ttre1 = new(diff ? 101 : 100, 101)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point
                };
                self._ttre2 = new(10, 101)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point
                };
                Vector3Int hsvHSL = self.GetHSVOrHSL100();
                for (int height = 0; height <= 100; height++)
                {
                    for (int width = 0; width < self._ttre1.width; width++)
                    {
                        float? h = diff ? null : width / 100f, s = diff ? width / 100f : height / 100f, l = diff ? height / 100f : null;
                        self._ttre1.SetPixel(width, height, self.ColorPicker2RGB().Invoke(self.ParseGetHSLORHSV01(h, s , l)));
                        if (width < self._ttre2.height)
                        {
                            h = diff ? height / 100f : null;
                            s = null;
                            l = diff ? null : height / 100f;
                            self._ttre2.SetPixel(width, height, self.ColorPicker2RGB().Invoke(self.ParseGetHSLORHSV01(h, s ,l)));
                        }

                    }
                }
                //Arrows
                Color hueArrowCol = new(1 - self._r / 100f, 1f - self._g / 100f, 1f - self._b / 100f);
                hueArrowCol = Color.Lerp(Color.white, hueArrowCol, Mathf.Pow(Mathf.Abs(hueArrowCol.grayscale - 0.5f) * 2f, 0.3f));
                SmallUtils.ApplyHSLArrow(self._ttre1, diff ? hsvHSL.y : hsvHSL.x, hueArrowCol, false, diff ? hsvHSL.z : hsvHSL.y); //first leftright arrow, diff -> s, h
                SmallUtils.ApplyHSLArrow(self._ttre1, diff ? hsvHSL.z : hsvHSL.y, hueArrowCol, true, diff ? hsvHSL.y : hsvHSL.x); //second downright arrow square texture, diff -> l, s
                SmallUtils.ApplyHSLArrow(self._ttre2, diff ? hsvHSL.x : hsvHSL.z, hueArrowCol, true, 51); //last rect texture, diff -> h, l
                self._ttre1.Apply();
                self._ttre2.Apply();
                return;
            }
            orig(self);
        }
        public static void On_OPColorPicker_Update(On.Menu.Remix.MixedUI.OpColorPicker.orig_Update orig, OpColorPicker self)
        {
            orig(self);
            if (self.CurrentlyFocusableMouse && self.MenuMouseMode)
            {
                if (self._MouseOverHex())
                {
                    ((Action?)(SmallUtils.CopyShortcutPressed(self.Menu) ? self.CopyHexCPicker : SmallUtils.PasteShortcutPressed(self.Menu) ? self.PasteHexCPicker : null))?.Invoke();
                }
                if (ModOptions.Instance.CopyPasteForColorPickerNumbers.Value && self.IfCPickerNumberHovered(out int oOO))
                {
                    ((Action<int>?)(SmallUtils.CopyShortcutPressed(self.Menu) ? self.CopyNumberCPicker : SmallUtils.PasteShortcutPressed(self.Menu) ? self.PasteNumberCPicker : null))?.Invoke(oOO);
                }
            }
        }
        public static void On_OPColorPicker_GrafUpdate(On.Menu.Remix.MixedUI.OpColorPicker.orig_GrafUpdate orig, OpColorPicker self, float timeStacker)
        {
            orig(self, timeStacker);
            if (!self.greyedOut)
            {
                if (ModOptions.Instance.CopyPasteForColorPickerNumbers.Value)
                {
                    self._lblR.color = self.CurrentlyFocusableMouse && self._lblR.IsFLabelHovered(self.MousePos) ? Color.Lerp(MenuColorEffect.rgbWhite, self.colorText, self.bumpBehav.Sin(10f)) :
                        self._lblR.color;
                    self._lblG.color = self.CurrentlyFocusableMouse && self._lblG.IsFLabelHovered(self.MousePos) ? Color.Lerp(MenuColorEffect.rgbWhite, self.colorText, self.bumpBehav.Sin(10f)) :
                        self._lblG.color;
                    self._lblB.color = self.CurrentlyFocusableMouse && self._lblB.IsFLabelHovered(self.MousePos) ? Color.Lerp(MenuColorEffect.rgbWhite, self.colorText, self.bumpBehav.Sin(10f)) :
                        self._lblB.color;
                }
            }
        }
        private static void GotoHSL(ILCursor cursor)
        {
            cursor.GotoNext(x => x.MatchCall(typeof(Custom), "HSL2RGB"));
            cursor.GotoNext(MoveType.After, x => x.MatchCallvirt<FNode>("set_isVisible"));
        }
        private static void MakeAReturn(ILCursor cursor)
        {
            cursor.Emit(OpCodes.Brfalse, cursor.Next);
            cursor.Emit(OpCodes.Ret);
        }

        //expeditionhooks
        public static void ExpeditionMenu_Hooks()
        {
            try
            {
                On.Menu.ExpeditionMenu.Singal += On_ExpeditionMenu_Singal;
                On.Menu.CharacterSelectPage.ctor += On_CharacterSelectPage_Ctor;
                On.Menu.CharacterSelectPage.Singal += On_CharacterSelectPage_Singal;
                On.Menu.CharacterSelectPage.SetUpSelectables += On_CharacterSelectPage_SetUpSelectables;
                On.Menu.CharacterSelectPage.UpdateChallengePreview += On_CharacterSelectPage_UpdateChallengePreview;
                On.Menu.CharacterSelectPage.RemoveSprites += On_CharacterSelectPage_RemoveSprites;
                On.Menu.CharacterSelectPage.LoadGame += On_CharacterSelectPage_LoadGame;
                On.Menu.ChallengeSelectPage.StartGame += On_ChallengeSelectPage_StartGame;
                ColorConfigMod.DebugLog("Sucessfully extended color interface for expedition mode!");
            }
            catch (Exception ex)
            {
                ColorConfigMod.DebugException("Failed to extend color config for expedition", ex);
            }

        }
        public static void On_ExpeditionMenu_Singal(On.Menu.ExpeditionMenu.orig_Singal orig, ExpeditionMenu self, MenuObject sender, string message)
        {
            if (ColorConfigMod.IsBingoOn)
            {
                BingoModUtils.TryApplyBingoColors(self, message);
            }
            orig(self, sender, message);
        }
        public static void On_CharacterSelectPage_Ctor(On.Menu.CharacterSelectPage.orig_ctor orig, CharacterSelectPage self, Menu.Menu menu, MenuObject owner, Vector2 pos)
        {
            orig(self, menu, owner, pos);
            SymbolButton? colorConfig = self.GetExtraEXPInterface().colorConfig;
            if (!ModManager.JollyCoop && ModManager.MMF && colorConfig == null && ModOptions.Instance.EnableExpeditionColorConfig.Value)
            {
                colorConfig = new(menu, self, "colorconfig_slugcat_noncoloured", "DUSTY_EXPEDITION_CONFIG", new(440 + (self.jollyToggleConfigMenu?.pos == new Vector2(440, 550) ? -self.jollyToggleConfigMenu.size.x - 10 : 0), 550));
                colorConfig.roundedRect.size = new(50, 50);
                colorConfig.size = colorConfig.roundedRect.size;
                self.subObjects.Add(colorConfig);
                self.GetExtraEXPInterface().colorConfig = colorConfig;

            }
        }
        public static void On_CharacterSelectPage_SetUpSelectables(On.Menu.CharacterSelectPage.orig_SetUpSelectables orig, CharacterSelectPage self)
        {
            orig(self);
            SymbolButton? colorConfig = self.GetExtraEXPInterface().colorConfig;
            if (colorConfig != null)
            {
                colorConfig.MenuObjectBind((self.menu as ExpeditionMenu)?.muteButton, left: true, top: true);
                colorConfig.MenuObjectBind(self.jollyToggleConfigMenu != null ? self.jollyToggleConfigMenu : self.slugcatButtons.ValueOrDefault(0), true);
                colorConfig.MenuObjectBind(self.slugcatButtons.Length > 3 ? self.slugcatButtons[3] : self.confirmExpedition, bottom: true);
            }
        }
        public static void On_CharacterSelectPage_Singal(On.Menu.CharacterSelectPage.orig_Singal orig, CharacterSelectPage self, MenuObject sender, string message)
        {
            orig(self, sender, message);
            if (self.menu is ExpeditionMenu { pagesMoving: false } && message == "DUSTY_EXPEDITION_CONFIG")
            {
                self.menu.PlaySound(SoundID.MENU_Player_Join_Game);
                self.menu.manager.ShowDialog(new ExpeditionColorDialog(self.menu, SmallUtils.ExpeditionSlugcat(), () =>
                {
                    self.GetExtraEXPInterface().colorConfig?.symbolSprite?.SetElementByName(GetColorEnabledSprite(self.menu));
                }, ModOptions.Instance.EnableHexCodeTypers.Value, showSlugcatDisplay: ModOptions.Instance.EnableSlugcatDisplay.Value));

            }

        }
        public static void On_CharacterSelectPage_UpdateChallengePreview(On.Menu.CharacterSelectPage.orig_UpdateChallengePreview orig, CharacterSelectPage self)
        {
            orig(self);
            self.GetExtraEXPInterface().colorConfig?.symbolSprite?.SetElementByName(GetColorEnabledSprite(self.menu));
        }
        public static void On_CharacterSelectPage_RemoveSprites(On.Menu.CharacterSelectPage.orig_RemoveSprites orig, CharacterSelectPage self)
        {
            orig(self);
            self.ClearMenuObject(ref self.GetExtraEXPInterface().colorConfig);
        }
        public static void On_CharacterSelectPage_LoadGame(On.Menu.CharacterSelectPage.orig_LoadGame orig, CharacterSelectPage self)
        {
            self.menu.TrySaveExpeditionColorsToCustomColors(SmallUtils.ExpeditionSlugcat());
            orig(self);
        }
        public static void On_ChallengeSelectPage_StartGame(On.Menu.ChallengeSelectPage.orig_StartGame orig, ChallengeSelectPage self)
        {
            self.menu.TrySaveExpeditionColorsToCustomColors(SmallUtils.ExpeditionSlugcat());
            orig(self);
        }
        public static string GetColorEnabledSprite(Menu.Menu menu) => menu.IsCustomColorEnabled(Expedition.ExpeditionData.slugcatPlayer) ? "colorconfig_slugcat_coloured" : "colorconfig_slugcat_noncoloured";
    }
}
