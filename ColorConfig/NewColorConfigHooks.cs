using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using UnityEngine;
using RWCustom;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using JollyCoop.JollyMenu;
namespace ColorConfig
{
    public static class ColorConfigHooks
    {
        public static void Init()
        {
            SlugcatSelectMenuHooks selectScreenHooks = new();
            JollyCoopConfigHooks jollyCoopConfigHooks = new();
            OpColorPickerHooks opColorPickerHooks = new();
            selectScreenHooks.Init();
            jollyCoopConfigHooks.Init();
            opColorPickerHooks.Init();
        }
        public class SlugcatSelectMenuHooks
        {
            public void Init()
            {
                try
                {
                    IL.Menu.SlugcatSelectMenu.CustomColorInterface.ctor += IL_SlugcatSelectMenu_CustomColorInterface_ctor;
                    IL.Menu.SlugcatSelectMenu.StartGame += IL_SlugcatSelectMenu_StartGame;
                    IL.Menu.SlugcatSelectMenu.SliderSetValue += IL_SlugcatSelectMenu_SliderSetValue;
                    On.Menu.SlugcatSelectMenu.AddColorButtons += On_SlugcatSelectMenu_AddColorButtons;
                    On.Menu.SlugcatSelectMenu.AddColorInterface += On_SlugcatSelectMenu_AddColorInterface;
                    On.Menu.SlugcatSelectMenu.RemoveColorInterface += On_SlugcatSelectMenu_RemoveColorInterface;
                    On.Menu.SlugcatSelectMenu.Update += On_SlugcatSelectMenu_Update;
                    On.Menu.SlugcatSelectMenu.ValueOfSlider += On_SlugcatSelectMenu_ValueOfSlider;
                    On.Menu.SlugcatSelectMenu.SliderSetValue += On_SlugcatSelectMenu_SliderSetValue;
                    ColorConfigMod.DebugLog("Sucessfully extended color interface for slugcat select menu!");
                    if (ColorConfigMod.IsRainMeadowOn)
                    {
                        RainMeadowHooks.ApplyRainMeadowHooks();
                    }

                }
                catch (Exception ex)
                {
                    ColorConfigMod.DebugException("Failed to initalise hooks for SlugcatSelectMenu color interface!", ex);
                }
            }
            //base stuff
            public void IL_SlugcatSelectMenu_CustomColorInterface_ctor(ILContext il)
            {
                ILCursor cursor = new(il);
                if (!cursor.TryGotoNext(MoveType.After, (x) => x.MatchCall(typeof(Custom), "HSL2RGB"), (x) => x.MatchCallvirt<MenuIllustration>("set_color")))
                {
                    ColorConfigMod.DebugError("IL_SlugcatSelectMenu_CustomColorInterface_ctor: Failed to find desired hsl2rgb color to patch");
                    return;
                }
                ColorConfigMod.DebugILCursor("IL_SlugcatSelectMenu_CustomColorInterface_ctor: ", cursor);
                try
                {
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldarg_1);
                    cursor.Emit(OpCodes.Ldloc_0);
                    cursor.EmitDelegate(new Action<SlugcatSelectMenu.CustomColorInterface, Menu.Menu, int>((self, menu, i) =>
                    {
                        Vector3 hsl = menu.MenuHSL(self.slugcatID, i);
                        if (hsl.x == 1)
                        {
                            self.bodyColors[i].color = ColConversions.HSL2RGB(hsl);
                        }
                    }));
                    //ColorConfigMod.DebugLog("IL_SlugcatSelectMenu_CustomColorInterface_ctor: Successfully patched desired hsl2rgb color!");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_SlugcatSelectMenu_CustomColorInterface_ctor: Failed to patch desired hsl2rgb color", e);
                }
            }
            public void IL_SlugcatSelectMenu_StartGame(ILContext il)
            {
                ILCursor cursor = new(il);
                if (!cursor.TryGotoNext((x) => x.MatchLdloc(0), (x) => x.MatchLdloca(2), (x) => x.MatchLdcI4(0), (x) => x.MatchCall<Vector3>("get_Item")))
                {
                    ColorConfigMod.DebugError("IL_SlugcatSelectMenu_StartGame: Failed to find desired get-custom hsl to patch");
                    return;
                }
                ColorConfigMod.DebugILCursor("IL_SlugcatSelectMenu_StartGame: ", cursor);
                try
                {
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldloc_1);
                    cursor.Emit(OpCodes.Ldloca, 2);
                    cursor.EmitDelegate(delegate (SlugcatSelectMenu self, int i, ref Vector3 hsl)
                    {
                        if (hsl[0] == 1)
                        {
                            hsl[0] = 0;
                            self.SaveHSLString(i, SmallUtils.SetHSLSaveString(hsl));
                        }
                    });
                    //ColorConfigMod.DebugLog("IL_SlugcatSelectMenu_StartGame: Successfully patched desired get-custom hsl!");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_SlugcatSelectMenu_StartGame: Failed to patch desired get-custom hsl", e);
                }
            }
            public void IL_SlugcatSelectMenu_SliderSetValue(ILContext il)
            {
                ILCursor cursor = new(il);
                if (!cursor.TryGotoNext(MoveType.After, (x) => x.MatchLdcR4(0), (x) => x.MatchLdcR4(0.99f), (x) => x.MatchCall<Mathf>("Clamp"), (x) => x.MatchCall<Vector3>("set_Item")))
                {
                    ColorConfigMod.DebugError("IL_SlugcatSelectMenu_SliderSetValue: Failed to find desired HSL(Hue) slider clamp to patch");
                    return;
                }
                ColorConfigMod.DebugILCursor("IL_SlugcatSelectMenu_SliderSetValue: ", cursor);
                try
                {
                    cursor.Emit(OpCodes.Ldarg_2);
                    cursor.Emit(OpCodes.Ldloca, 2);
                    cursor.EmitDelegate(delegate (float f, ref Vector3 hsl)
                    {
                        //hsl = (f > 0.99f && ModOptions.DisableHueSliderMaxClamp.Value) ? new(Mathf.Clamp01(f), hsl.y, hsl.z) : hsl;
                    });
                    //ColorConfigMod.DebugLog("IL_SlugcatSelectMenu_SliderSetValue: Successfully patched desired HSL(Hue) slider clamp!");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_SlugcatSelectMenu_SliderSetValue: Failed to patch desired HSL(Hue) slider clamp", e);
                }
                if (!cursor.TryGotoNext(MoveType.After, (x) => x.MatchCall(typeof(Custom), "HSL2RGB"), (x) => x.MatchCallvirt<MenuIllustration>("set_color")))
                {
                    ColorConfigMod.DebugError("IL_SlugcatSelectMenu_SliderSetValue: Failed to find desired new hsl to rgb to patch");
                    return;
                }
                try
                {
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldarg_2);
                    cursor.Emit(OpCodes.Ldloc_1);
                    cursor.Emit(OpCodes.Ldloc_2);
                    cursor.EmitDelegate(new Action<SlugcatSelectMenu, float, int, Vector3>((self, f, colChooser, newHSL) =>
                    {
                        self.colorInterface.bodyColors[colChooser].color = newHSL[0] == 1 ? ColConversions.HSL2RGB(newHSL) : self.colorInterface.bodyColors[colChooser].color;
                    }));
                    //ColorConfigMod.DebugLog("IL_SlugcatSelectMenu_SliderSetValue: Successfully patched desired new hsl to rgb!");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_SlugcatSelectMenu_SliderSetValue: Failed to patch desired new hsl to rgb to patch", e);
                }
            }
            public void On_SlugcatSelectMenu_AddColorButtons(On.Menu.SlugcatSelectMenu.orig_AddColorButtons orig, SlugcatSelectMenu self)
            {
                orig(self);
                if (ModOptions.EnableSlugcatDisplay.Value && ModOptions.EnableLegacyIdeaSlugcatDisplay.Value && slugcatDisplay == null)
                {
                    Vector2 vector = new(1000f - (1366f - self.manager.rainWorld.options.ScreenSize.x) / 2f, self.manager.rainWorld.options.ScreenSize.y - 100f);
                    vector.y -= ModManager.JollyCoop ? 40 : 0;
                    vector.y -= self.colorInterface != null? self.colorInterface.bodyColors.Length * 40 : 0;
                    slugcatDisplay = new(self, self.pages[0], new(vector.x + 140, vector.y + 40), new(45f, 45f),
                    self.slugcatColorOrder[self.slugcatPageIndex]);
                    self.pages[0].subObjects.Add(slugcatDisplay);
                }
            }
            public void On_SlugcatSelectMenu_AddColorInterface(On.Menu.SlugcatSelectMenu.orig_AddColorInterface orig, SlugcatSelectMenu self)
            {
                orig(self);
                /*if (ModOptions.ShouldEnableExtraLegacySliders && extraSliders == null)
                {
                    NewMenuInterfaces.SliderOOOIDGroup sliderOOOIDGroup = ExtraSliderIDGroups.GetValueOrDefault(0, null);
                    if (sliderOOOIDGroup != null)
                    {
                        extraSliders = new(self, self.pages[0], self.defaultColorButton.pos + new Vector2(0, -40), new(0, -40), new(200, 40),
                            [.. sliderOOOIDGroup.Names], false, 
                            [.. sliderOOOIDGroup.SliderIDs]);
                        self.pages[0].subObjects.Add(extraSliders);
                    }
                }
                if (extraSliders?.sliders?.Count > 2 && extraOOOPages == null)
                {
                    extraOOOPages = new(self, self.pages[0], extraSliders.sliders[0], extraSliders.sliders[1], extraSliders.sliders[2], ExtraSliderIDGroups, new(0, 25))
                    {
                        showValues = ModOptions.ShowVisual,
                    };
                    self.pages[0].subObjects.Add(extraOOOPages);
                    extraOOOPages.PopulatePage(extraOOOPages.CurrentOffset);

                }*/
                if (oOOPages == null)
                {
                    oOOPages = new(self, self.pages[0], self.hueSlider, self.satSlider, self.litSlider, SliderIDGroups, new(0, 25))
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
                if (ModOptions.EnableSlugcatDisplay.Value && slugcatDisplay == null)
                {
                    slugcatDisplay = new(self, self.pages[0], new(self.satSlider.pos.x + 140, self.satSlider.pos.y + 80), new(45f, 45f),
                        self.slugcatColorOrder[self.slugcatPageIndex]);
                    self.pages[0].subObjects.Add(slugcatDisplay);

                }
                if (ModOptions.EnableHexCodeTypers.Value)
                {
                    if (ModOptions.EnableLegacyHexTypers.Value)
                    {
                        if (legacyHexInterface == null)
                        {
                            legacyHexInterface = new(self, self.pages[0], self.defaultColorButton.pos + new Vector2(120, 0), self.slugcatColorOrder[self.slugcatPageIndex]);
                            self.pages[0].subObjects.Add(legacyHexInterface);
                        }
                    }
                    else if (hexInterface == null)
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
                    oOOPages.ClearMenuObject(self.pages[0]);
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
                if (legacyHexInterface != null)
                {
                    legacyHexInterface.RemoveSprites();
                    self.pages[0].RemoveSubObject(legacyHexInterface);
                    legacyHexInterface = null;
                }
            }
            public void On_SlugcatSelectMenu_Update(On.Menu.SlugcatSelectMenu.orig_Update orig, SlugcatSelectMenu self)
            {
                orig(self);
                if (ModOptions.RMStoryMenuSlugcatFix.Value && RainMeadowOn)
                {
                    RainMeadowHooks.RainMeadowSyncSlugcatWithUpdate(self);
                }
                UpdateMenuInterfaces(self);
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
                    ssM.SaveHSLString(SmallUtils.SetHSLSaveString(SmallUtils.FixNonHueSliderWonkiness(hsl, ssMHSL)));

                }
                if (MenuToolObj.HSVSliderIDS.Contains(slider?.ID))
                {

                    Vector3 hsv = ColConversions.HSL2HSV(ssM.SlugcatSelectMenuHSL());
                    hsv = slider.ID == MenuToolObj.HueHSV ? new(f, hsv.y, hsv.z) : slider.ID == MenuToolObj.SatHSV ? new(hsv.x, f, hsv.z) : new(hsv.x, hsv.y, f);
                    SmallUtils.RWIIIClamp(hsv, CustomColorModel.HSV, out Vector3 newHSL/*, !ModOptions.DisableHueSliderMaxClamp.Value*/);
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
            public void UpdateMenuInterfaces(SlugcatSelectMenu ssM)
            {
                if (hexInterface != null)
                {
                    hexInterface.SaveNewHSL(SmallUtils.SlugcatSelectMenuHSL(ssM));
                    if (hexInterface.shouldUpdateNewColor)
                    {
                        hexInterface.shouldUpdateNewColor = false;
                        ssM.SaveHSLString(SmallUtils.SetHSLSaveString(hexInterface.newPendingHSL));
                        ssM.SliderSetValue(ssM.hueSlider, ssM.ValueOfSlider(ssM.hueSlider));
                        ssM.SliderSetValue(ssM.satSlider, ssM.ValueOfSlider(ssM.satSlider));
                        ssM.SliderSetValue(ssM.litSlider, ssM.ValueOfSlider(ssM.litSlider));

                    }
                }
                if (legacyHexInterface != null)
                {
                    legacyHexInterface.SaveNewHSLs(ssM.SlugcatSelectMenuHSLs(), ssM.activeColorChooser);
                    if (legacyHexInterface.hexTypeBoxes?.Count > 0)
                    {
                        for (int i = 0; i < legacyHexInterface.hexTypeBoxes.Count; i++)
                        {
                            if (legacyHexInterface.hexTypeBoxes[i]?.shouldUpdateNewColor == true)
                            {
                                legacyHexInterface.hexTypeBoxes[i].shouldUpdateNewColor = false;
                                ssM.SaveHSLString(i, SmallUtils.SetHSLSaveString(legacyHexInterface.hexTypeBoxes[i].newPendingHSL));
                                if (ssM.activeColorChooser == i)
                                {
                                    ssM.SliderSetValue(ssM.hueSlider, ssM.ValueOfSlider(ssM.hueSlider));
                                    ssM.SliderSetValue(ssM.satSlider, ssM.ValueOfSlider(ssM.satSlider));
                                    ssM.SliderSetValue(ssM.litSlider, ssM.ValueOfSlider(ssM.litSlider));
                                }
                                else if (ssM.colorInterface?.bodyColors?.Length > i)
                                {
                                    ssM.colorInterface.bodyColors[i].color = ColConversions.HSL2RGB(legacyHexInterface.hexTypeBoxes[i].newPendingHSL);
                                }
                            }
                        }


                    }
                }
                slugcatDisplay?.LoadNewHSLStringSlugcat(ssM.manager.rainWorld.progression.miscProgressionData.colorChoices[ssM.slugcatColorOrder[ssM.slugcatPageIndex].value], ssM.slugcatColorOrder[ssM.slugcatPageIndex]);
            }
            public List<MenuInterfaces.SliderOOOIDGroup> SliderIDGroups
            {
                get
                {
                    List<MenuInterfaces.SliderOOOIDGroup> IDGroups = [];
                    SmallUtils.AddSSMSliderIDGroups(IDGroups, ModOptions.ShouldRemoveHSLSliders);
                    return IDGroups;
                }
            }

            public MenuInterfaces.HexTypeBox hexInterface;
            public MenuInterfaces.SlugcatDisplay slugcatDisplay;
            public MenuInterfaces.SliderOOOPages oOOPages;

            //Legacy versions stuff
            public List<MenuInterfaces.SliderOOOIDGroup> ExtraSliderIDGroups
            {
                get
                {
                    List<MenuInterfaces.SliderOOOIDGroup> IDGroups = [];
                    if (ModOptions.EnableRGBSliders.Value)
                    {
                        IDGroups.Add(new(MenuToolObj.RedRGB, MenuToolObj.GreenRGB, MenuToolObj.BlueRGB,
                            MenuToolObj.RGBNames, MenuToolObj.RGBShowInt, MenuToolObj.rgbMultipler));
                    }
                    if (ModOptions.EnableHSVSliders.Value)
                    {
                        IDGroups.Add(new(MenuToolObj.HueHSV, MenuToolObj.SatHSV, MenuToolObj.ValHSV,
                           MenuToolObj.HSVNames, MenuToolObj.HueOOShowInt, MenuToolObj.hueOOMultipler));
                    }

                    return IDGroups;
                }
            }
            public MenuInterfaces.LegacyHexTypeBoxes legacyHexInterface;
            public MenuInterfaces.SliderOOOPages extraOOOPages;
            public static bool RainMeadowOn { get; private set; }
            public static class RainMeadowHooks
            {
                public static void ApplyRainMeadowHooks()
                {
                    RainMeadowOn = true;
                    ColorConfigMod.DebugLog("Initialising Rain Meadow Hooks");
                    ILHook IL_RainMeadow_ssM_SliderSetValue = new(MethodBase.GetMethodFromHandle(typeof(RainMeadow.RainMeadow).
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
                            if (RainMeadow.RainMeadow.isStoryMode(out _))
                            {
                                menu.SaveHSLString(ssM_CCI.slugcatID, 0, SmallUtils.SetHSLSaveString(Custom.RGB2HSL(RainMeadow.RainMeadow.rainMeadowOptions.BodyColor.Value)));
                                menu.SaveHSLString(ssM_CCI.slugcatID, 1, SmallUtils.SetHSLSaveString(Custom.RGB2HSL(RainMeadow.RainMeadow.rainMeadowOptions.EyeColor.Value)));
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
                                    else if (ssM.hueSlider.floatValue == 1)
                                    {
                                        ssM.colorInterface.bodyColors[ssM.activeColorChooser].color = ColConversions.HSL2RGB(new(ssM.hueSlider.floatValue, ssM.satSlider.floatValue, ssM.litSlider.floatValue));
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
                public static void RainMeadowSyncSlugcatWithUpdate(SlugcatSelectMenu ssM)
                {
                    if (RainMeadow.RainMeadow.isStoryMode(out RainMeadow.StoryGameMode storyGameMode) && RainMeadow.OnlineManager.lobby?.isOwner == false)
                    {
                        if (storyGameMode.requireCampaignSlugcat)
                        {
                            if (ssM.slugcatColorOrder[ssM.slugcatPageIndex] != storyGameMode.currentCampaign)
                            {
                                SmallUtils.ChangeColorOrderIndex(ssM, storyGameMode.currentCampaign);
                                ColorConfigMod.DebugLog($"Meadow Story Menu -> slugcat is forcefully set to {storyGameMode.currentCampaign.value}");
                            }
                        }
                        else if (ssM.slugcatColorOrder[ssM.slugcatPageIndex] != storyGameMode.avatarSettings.playingAs)
                        {
                            SmallUtils.ChangeColorOrderIndex(ssM, storyGameMode.avatarSettings.playingAs);
                            ColorConfigMod.DebugLog($"Meadow Story Menu -> slugcat is now set back to {storyGameMode.avatarSettings.playingAs.value}");
                        }
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
                    IL.JollyCoop.JollyMenu.ColorChangeDialog.ValueOfSlider += IL_Dialog_ValueOfSlider;
                    IL.JollyCoop.JollyMenu.ColorChangeDialog.SliderSetValue += IL_Dialog_SliderSetValue;
                    On.JollyCoop.JollyMenu.ColorChangeDialog.ValueOfSlider += On_Dialog_ValueOfSlider;
                    On.JollyCoop.JollyMenu.ColorChangeDialog.SliderSetValue += On_Dialog_SliderSetValue;
                    On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.ctor += On_Color_Slider_ctor;
                    On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.RemoveSprites += On_ColorSlider_RemoveSprites;
                    On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.HSL2RGB += On_ColorSlider_HSL2RGB;
                    On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.RGB2HSL += On_ColorSlider_RGB2HSL;
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
            public void IL_Dialog_SliderSetValue(ILContext il)
            {
                ILCursor cursor = new(il);
                if (!cursor.TryGotoNext(MoveType.After, (x) => x.MatchCall<ColorChangeDialog>("<SliderSetValue>g__AssignCorrectColorDimension|15_0")))
                {
                    ColorConfigMod.DebugError("IL_Dialog_SliderSetValue: Failed to find desired set hue to patch");
                    return;
                }
                ColorConfigMod.DebugILCursor("IL_Dialog_SliderSetValue: ", cursor);
                try
                {
                    cursor.Emit(OpCodes.Ldarg_2);
                    cursor.Emit(OpCodes.Ldloc_3);
                    cursor.Emit(OpCodes.Ldloc_2);
                    cursor.EmitDelegate(new Action<float, ColorChangeDialog.ColorSlider, int>((f, colSlider, oOO) =>
                    {
                        colSlider.hslColor.hue = shouldGetfullHue && oOO != 1 && oOO != 2 && f > 0.99f ? Mathf.Clamp01(f) : colSlider.hslColor.hue;
                    }));
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("Failed to patch desired set hue", e);
                }
            }
            public void IL_Dialog_ValueOfSlider(ILContext il)
            {
                ILCursor cursor = new(il);
                if (!cursor.TryGotoNext(MoveType.After, (x) => x.MatchLdloc(5), (x) => x.MatchLdfld<ColorChangeDialog.ColorSlider>(nameof(ColorChangeDialog.ColorSlider.hslColor)), (x) => x.MatchLdloc(2),
                    (x) => x.MatchCall<ColorChangeDialog>("<ValueOfSlider>g__GetCorrectColorDimension|16_0")))
                {
                    ColorConfigMod.DebugError("IL_Dialog_ValueOfSlider: Failed to find desired non clamped hue to patch");
                    return;
                }
                ColorConfigMod.DebugILCursor("IL_Dialog_ValueOfSlider: ", cursor);
                try
                {
                    cursor.Emit(OpCodes.Ldloc, 5);
                    cursor.Emit(OpCodes.Ldloc_2);
                    cursor.EmitDelegate(new Func<float, ColorChangeDialog.ColorSlider, int, float>((origFloat, colSlider, oOO) =>
                    {
                        return SmallUtils.ValueOfBodyPart(colSlider.hslColor, oOO);
                    }));
                    cursor.Emit(OpCodes.Ret);
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("Failed to patch desired non clamped hue", e);
                }
            }
            public void On_Color_Slider_ctor(On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_ctor orig, ColorChangeDialog.ColorSlider self, Menu.Menu menu, MenuObject owner, Vector2 pos, int playerNumber, int bodyPart, string sliderTitle)
            {
                orig(self, menu, owner, pos, playerNumber, bodyPart, sliderTitle);
                if (!colSliders.Contains(self))
                {
                    colSliders.Add(self);
                }
                if (!configPages.ContainsKey(self))
                {
                    MenuInterfaces.JollyCoopOOOConfigPages jollyCoopOOOConfigPages = new(menu, self, bodyPart);
                    configPages.Add(self, jollyCoopOOOConfigPages);
                    self.subObjects.Add(jollyCoopOOOConfigPages);

                }
            }
            public void On_ColorSlider_RemoveSprites(On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_RemoveSprites orig, ColorChangeDialog.ColorSlider self)
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
            public void On_ColorSlider_HSL2RGB(On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_HSL2RGB orig, ColorChangeDialog.ColorSlider self)
            {
                orig(self);
                self.color = self.hslColor.hue == 1 ? ColConversions.HSL2RGB(self.hslColor.HSL2Vector3()) : self.color;
            }
            public void On_ColorSlider_RGB2HSL(On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_RGB2HSL orig, ColorChangeDialog.ColorSlider self)
            {
                orig(self);
                if (shouldGetfullHue)
                {
                    SmallUtils.RWIIIClamp(self.color.RGB2Vector3(), CustomColorModel.RGB, out Vector3 newHSL, !shouldGetfullHue);
                    self.hslColor = newHSL.Vector32HSL();
                }
            }
            public float On_Dialog_ValueOfSlider(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_ValueOfSlider orig, ColorChangeDialog self, Slider slider)
            {
                if (ValueOfCustomSliders(slider, out float f))
                {
                    return f;
                }
                return orig(self, slider);
            }
            public void On_Dialog_SliderSetValue(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_SliderSetValue orig, ColorChangeDialog self, Slider slider, float f)
            {
                CustomSliderSetValue(slider, f);
                orig(self, slider, f);
            }
            public void CustomSliderSetValue(Slider slider, float f)
            {
                if (slider?.ID?.value != null && slider.ID.value.StartsWith("DUSTY") && slider.ID.value.Contains('_'))
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
                            colSliders[colSliderNum].color = SmallUtils.RWIIIClamp(color.RGB2Vector3(), CustomColorModel.RGB, out Vector3 newHSL).Vector32RGB();
                            colSliders[colSliderNum].hslColor = SmallUtils.FixNonHueSliderWonkiness(newHSL, colSliders[colSliderNum].hslColor.HSL2Vector3()).Vector32HSL();
                        }
                        if (array[2] == "HSV" && MenuToolObj.HSVNames.Contains(array[3]))
                        {
                            Vector3 hsv = ColConversions.HSL2HSV(colSliders[colSliderNum].hslColor.HSL2Vector3());
                            hsv = array[3] == "HUE" ? new(f, hsv.y, hsv.z) : array[3] == "SAT" ? new(hsv.x, f, hsv.z) : new(hsv.x, hsv.y, f);
                            SmallUtils.RWIIIClamp(hsv, CustomColorModel.HSV, out Vector3 newHSL, !shouldGetfullHue);
                            colSliders[colSliderNum].hslColor = newHSL.Vector32HSL();
                            colSliders[colSliderNum].HSL2RGB();
                        }
                    }
                }
            }
            public bool ValueOfCustomSliders(Slider slider, out float f)
            {
                f = 0;
                if (slider?.ID?.value != null && slider.ID.value.StartsWith("DUSTY") && slider.ID.value.Contains('_'))
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
                return false;
            }

            public static bool shouldGetfullHue = false;
            public static bool LukkyRGBSliderModOn { get; private set; }

            public readonly Dictionary<ColorChangeDialog.ColorSlider, MenuInterfaces.JollyCoopOOOConfigPages> configPages = [];
            public readonly List<ColorChangeDialog.ColorSlider> colSliders = [];
            public static class LukkyRGBModHooks
            {
                public static void ApplyLukkyModHooks()
                {
                    LukkyRGBSliderModOn = true;
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
            public void OtherOpColorPickerHooks()
            {
                ILHook iLHook = new(MethodBase.GetMethodFromHandle(typeof(OpColorPicker).
                   GetProperty(nameof(OpColorPicker.@value)).GetSetMethod().MethodHandle),
                   IL_OPColorPicker_set_value);
                Hook onHook = new(MethodBase.GetMethodFromHandle(typeof(OpColorPicker).
                   GetProperty(nameof(OpColorPicker.@value)).GetSetMethod().MethodHandle),
                   new Action<Action<OpColorPicker, string>, OpColorPicker, string>((orig, self, newValue) =>
                   {
                       if (self.value == newValue && ModOptions.EnableBetterOPColorPicker.Value &&
                          ((ModOptions.EnableDifferentOpColorPickerHSLPos.Value &&
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
            public void IL_OPColorPicker_set_value(ILContext iL)
            {
                ILCursor cursor = new(iL);
                if (!cursor.TryGotoNext(MoveType.After, (x) => x.MatchDiv(), (x) => x.MatchNewobj<Color>(), (x) => x.MatchCall<RXColor>("HSLFromColor"),
                    (x) => x.MatchStloc(0)))
                {
                    ColorConfigMod.DebugError("IL_OPColorPicker_set_value: Failed to find after hsl set value");
                    return;
                }
                ColorConfigMod.DebugILCursor("IL_OPColorPicker_set_value: ", cursor);
                try
                {
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldarg_1);
                    cursor.Emit(OpCodes.Ldloca, 0);
                    cursor.EmitDelegate(delegate (OpColorPicker self, string newValue, ref RXColorHSL hsl)
                    {
                        hsl = ModOptions.HSL2HSVOPColorPicker.Value ? ColConversions.HSL2HSV(hsl.RXHSl2Vector3()).Vector32RXHSL() : hsl;
                        if (ModOptions.EnableBetterOPColorPicker.Value)
                        {
                            if (self._mode == OpColorPicker.PickerMode.HSL && (self._curFocus == OpColorPicker.MiniFocus.HSL_Hue || 
                            self._curFocus == OpColorPicker.MiniFocus.HSL_Saturation || 
                            self._curFocus == OpColorPicker.MiniFocus.HSL_Lightness))
                            {
                                hsl.h = self._h / 100f;
                                hsl.s = self._s / 100f;
                                hsl.l = self._l / 100f;

                            }
                        }

                    });
                    //ColorConfigMod.DebugLog("IL_OPColorPicker_set_value: Successfully patched set_value for hsv");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_OPColorPicker_set_value: Failed to patch set_value for hsv", e);
                }
            }
            public void IL_OPColorPicker_Change(ILContext iL)
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
                        self._cdis0.color = ModOptions.HSL2HSVOPColorPicker.Value ? ColConversions.HSV2RGB(new(self._h / 100f, self._s / 100f, self._l / 100f)) : self._cdis0.color;
                    }));
                    //ColorConfigMod.DebugLog("IL_OPColorPicker_Change: Successfully patched _cdis0 Color");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_OPColorPicker_Change: Failed to patch _cdis0 Color", e);
                }
            }
            public void IL_OPColorPicker__HSLSetValue(ILContext il)
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
                    cursor.Emit(OpCodes.Ldloca, 0);
                    cursor.EmitDelegate(delegate (OpColorPicker self, ref Color col)
                    {
                        col = ModOptions.HSL2HSVOPColorPicker.Value ? ColConversions.HSV2RGB(new(self._h / 100f, self._s / 100f, self._l / 100f)) : col;
                    });
                    //ColorConfigMod.DebugLog("IL_OPColorPicker__HSLSetValue: Successfully patched RGB Color");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_OPColorPicker__HSLSetValue: Failed to patch RGB Color", e);
                }
            }
            public void IL_OPColorPicker_MouseModeUpdate(ILContext iL)
            {
                ILCursor cursor = new(iL);
                Func<Instruction, bool>[] PickerModeSwitch =
                [
                    (x) => x.MatchLdarg(0),
                    (x) => x.MatchLdfld<OpColorPicker>(nameof(OpColorPicker._mode)),
                    (x) => x.Match(OpCodes.Stloc_S),
                    (x) => x.Match(OpCodes.Ldloc_S),
                    (x) => x.Match(OpCodes.Switch),
                ];
                Func<Instruction, bool>[] colorMatch =
                [
                    (x) => x.MatchCall(typeof(Custom), "HSL2RGB"),
                    (x) => x.MatchCallvirt(typeof(FSprite).GetMethod("set_color")),

                ];
                Func<Instruction, bool>[] visiblityMatch =
                [
                    (x) => x.MatchLdarg(0),
                    (x) => x.MatchLdfld<OpColorPicker>(nameof(OpColorPicker._cdis1)),
                    (x) => x.MatchLdcI4(1),
                    (x) => x.MatchCallvirt<FNode>("set_isVisible"),
                ];
                try
                {
                    cursor.GotoNext((x) => x.MatchSub(), (x) => x.Match(OpCodes.Switch));
                    cursor.GotoNext(colorMatch);
                    cursor.GotoNext(MoveType.After, visiblityMatch);
                    ColorConfigMod.DebugILCursor("IL_OPColorPickerMouseModeUpdateUpdate: ", cursor);
                    //huesat2SatLittextFix vvvvvv
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldloc_3);
                    cursor.Emit(OpCodes.Ldloc, 4);
                    cursor.EmitDelegate(new Func<OpColorPicker, int, int, bool>((self, hueSat, satLit) =>
                    {
                        if (ModOptions.EnableDifferentOpColorPickerHSLPos.Value)
                        {
                            hueSat = Mathf.RoundToInt(Mathf.Clamp(self.MousePos.x - 10f, 0f, 100f));
                            self._lblR.text = self._h.ToString();
                            self._lblG.text = hueSat.ToString();
                            self._lblB.text = satLit.ToString();
                            self._cdis1.color = ModOptions.HSL2HSVOPColorPicker.Value ?
                            ColConversions.HSV2RGB(new(self._h / 100f, hueSat / 100f, satLit / 100f)) :
                            ColConversions.HSL2RGB(new(self._h / 100f, hueSat / 100f, satLit / 100f));
                            if (self._s != hueSat || self._l != satLit)
                            {
                                self._s = hueSat;
                                self._l = satLit;
                                self.PlaySound(SoundID.MENU_Scroll_Tick);
                                self._HSLSetValue();
                            }
                            return true;
                        }
                        else if (ModOptions.HSL2HSVOPColorPicker.Value)
                        {
                            self._cdis1.color = ColConversions.HSV2RGB(new(hueSat / 100f, satLit / 100f, self._l / 100f));
                        }
                        return false;
                    }));
                    cursor.Emit(OpCodes.Brfalse, cursor.Next);
                    cursor.Emit(OpCodes.Ret);
                    //ColorConfigMod.DebugLog("IL_OPColorPickerMouseModeUpdate: Sucessfully patched MiniFocus SatHue!");
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
                    cursor.Emit(OpCodes.Ldloc, 5);
                    cursor.EmitDelegate(new Func<OpColorPicker, int, bool>((self, litHue) =>
                    {
                        if (ModOptions.EnableDifferentOpColorPickerHSLPos.Value)
                        {
                            litHue = litHue > 99 ? 99 : litHue;
                            self._lblR.text = litHue.ToString();
                            self._lblG.text = self._s.ToString();
                            self._lblB.text = self._l.ToString();
                            self._cdis1.color = ModOptions.HSL2HSVOPColorPicker.Value ?
                            ColConversions.HSV2RGB(new(litHue / 100f, self._s / 100f, self._l / 100f)) :
                            ColConversions.HSL2RGB(new(litHue / 100f, self._s / 100f, self._l / 100f));
                            if (self._h != litHue)
                            {
                                self._h = litHue;
                                self.PlaySound(SoundID.MENU_Scroll_Tick);
                                self._HSLSetValue();
                            }
                            return true;
                        }
                        else if (ModOptions.HSL2HSVOPColorPicker.Value)
                        {
                            self._cdis1.color = ColConversions.HSV2RGB(new(self._h / 100f, self._s / 100f, litHue / 100f));
                        }
                        return false;

                    }));
                    cursor.Emit(OpCodes.Brfalse, cursor.Next);
                    cursor.Emit(OpCodes.Ret);
                    //ColorConfigMod.DebugLog("IL_OPColorPickerMouseModeUpdate: Sucessfully patched MiniFocus for Lit!");
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
                    cursor.Emit(OpCodes.Ldloc, 11);
                    cursor.EmitDelegate(new Action<OpColorPicker, int>((self, litHue) =>
                    {
                        if (ModOptions.EnableDifferentOpColorPickerHSLPos.Value)
                        {
                            litHue = litHue > 99? 99 : litHue;
                            self._lblR.text = litHue.ToString();
                            self._lblB.text = self._l.ToString();
                            self._cdis1.color = ModOptions.HSL2HSVOPColorPicker.Value ? ColConversions.HSV2RGB(new(litHue / 100f, self._s / 100f, self._l / 100f)) :
                            ColConversions.HSL2RGB(new(litHue / 100f, self._s / 100f, self._l / 100f));
                        }
                        else if (ModOptions.HSL2HSVOPColorPicker.Value)
                        {
                            self._cdis1.color = ColConversions.HSV2RGB(new(self._h / 100f, self._s / 100f, litHue / 100f));
                        }

                    }));
                    //ColorConfigMod.DebugLog("IL_OPColorPickerMouseModeUpdate: Sucessfully patched HoverMouse for Hue!");
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
                    cursor.Emit(OpCodes.Ldloc, 12);
                    cursor.Emit(OpCodes.Ldloc, 13);
                    cursor.EmitDelegate(new Action<OpColorPicker, int, int>((self, hueSat, satLit) =>
                    {
                        if (ModOptions.EnableDifferentOpColorPickerHSLPos.Value)
                        {
                            hueSat = Mathf.RoundToInt(Mathf.Clamp(self.MousePos.x - 10f, 0f, 100f));
                            self._lblR.text = self._h.ToString();
                            self._lblG.text = hueSat.ToString();
                            self._lblB.text = satLit.ToString();
                            self._cdis1.color = ModOptions.HSL2HSVOPColorPicker.Value ? ColConversions.HSV2RGB(new(self._h / 100f, hueSat / 100f, satLit / 100f)) :
                            ColConversions.HSL2RGB(new(self._h / 100f, hueSat / 100f, satLit / 100f));
                        }
                        else if (ModOptions.HSL2HSVOPColorPicker.Value)
                        {
                            self._cdis1.color = ColConversions.HSV2RGB(new(hueSat / 100f, satLit / 100f, self._l / 100f));
                        }
                    }));
                    //ColorConfigMod.DebugLog("IL_OPColorPickerMouseModeUpdate: Sucessfully patched HoverMouse for Sat and Lit!");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_OPColorPickerMouseModeUpdate: Failed to patch HoverMouse for Sat and Lit", e);
                }

            }
            public void IL_OPColorPicker_GrafUpdate(ILContext iL)
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
                        if (ModOptions.EnableDifferentOpColorPickerHSLPos.Value)
                        {
                            self._lblB.color = self._lblR.color;
                            self._lblR.color = self.colorText;
                        }
                    }));
                    //ColorConfigMod.DebugLog("IL_OPColorPickerGrafUpdate: Successfully patched hue2lit text color");
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
                        if (ModOptions.EnableDifferentOpColorPickerHSLPos.Value && self.MenuMouseMode)
                        {
                            self._focusGlow.pos = new Vector2(104f, 25f);
                        }
                    }));
                    //ColorConfigMod.DebugLog("IL_OPColorPickerGrafUpdate: Successfully patched focus glow for hue2lit text");
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
                        if (ModOptions.EnableDifferentOpColorPickerHSLPos.Value && self.MenuMouseMode)
                        {
                            self._lblR.color = self._lblB.color;
                            self._lblB.color = self.colorText;
                        }
                    }));
                    //ColorConfigMod.DebugLog("IL_OPColorPickerGrafUpdate: Successfully patched lit2hue text color");
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
                        if (ModOptions.EnableDifferentOpColorPickerHSLPos.Value && self.MenuMouseMode)
                        {
                            self._focusGlow.pos = new Vector2(104f, 105f);
                        }
                    }));
                    //ColorConfigMod.DebugLog("IL_OPColorPickerGrafUpdate: Successfully patched focus glow for lit2hue text");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("IL_OPColorPickerGrafUpdate: Failed to fix focus glow for lit2hue text", e);
                }
            }
            public void On_OPColorPickerCtor(On.Menu.Remix.MixedUI.OpColorPicker.orig_ctor orig, OpColorPicker self, Configurable<Color> config, Vector2 pos)
            {
                orig(self, config, pos);
                self._lblHSL.text = ModOptions.HSL2HSVOPColorPicker.Value ? "HSV" : self._lblHSL.text;
            }
            public void On_OPColorPickerRecalculateTexture(On.Menu.Remix.MixedUI.OpColorPicker.orig__RecalculateTexture orig, OpColorPicker self)
            {
                if (self._mode == OpColorPicker.PickerMode.HSL && (ModOptions.EnableDifferentOpColorPickerHSLPos.Value || ModOptions.HSL2HSVOPColorPicker.Value))
                {
                    self._ttre1 = new Texture2D(ModOptions.EnableDifferentOpColorPickerHSLPos.Value ? 101 : 100, 101)
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
                            if (ModOptions.EnableDifferentOpColorPickerHSLPos.Value)
                            {
                                self._ttre1.SetPixel(sqrWidth, height, ModOptions.HSL2HSVOPColorPicker.Value ? ColConversions.HSV2RGB(new(self._h / 100f, sqrWidth / 100f, height / 100f)) : ColConversions.HSL2RGB(new(self._h / 100f, sqrWidth / 100f, height / 100f)));
                                continue;
                            }
                            self._ttre1.SetPixel(sqrWidth, height, ColConversions.HSV2RGB(new(sqrWidth / 100f, height / 100f, self._l / 100f)));

                        }
                        for (int rectWidth = 0; rectWidth < 10; rectWidth++)
                        {
                            if (ModOptions.EnableDifferentOpColorPickerHSLPos.Value)
                            {
                                self._ttre2.SetPixel(rectWidth, height, ModOptions.HSL2HSVOPColorPicker.Value ? ColConversions.HSV2RGB(new(height / 100f, self._s / 100f, self._l / 100f)) : ColConversions.HSL2RGB(new(height / 100f, self._s / 100f, self._l / 100f)));
                                continue;
                            }
                            self._ttre2.SetPixel(rectWidth, height, ColConversions.HSV2RGB(new(self._h / 100f, self._s / 100f, height / 100f)));
                        }
                    }
                    //Arrows
                    Color hueArrowCol = SmallUtils.FindArrowColor(self);
                    SmallUtils.ApplyArrows(ModOptions.EnableDifferentOpColorPickerHSLPos.Value ? self._ttre2 : self._ttre1, self._h, hueArrowCol, ModOptions.EnableDifferentOpColorPickerHSLPos.Value, ModOptions.EnableDifferentOpColorPickerHSLPos.Value ? 51 : self._s);
                    SmallUtils.ApplyArrows(self._ttre1, self._s, hueArrowCol, !ModOptions.EnableDifferentOpColorPickerHSLPos.Value, ModOptions.EnableDifferentOpColorPickerHSLPos.Value ? self._l : self._h);
                    SmallUtils.ApplyArrows(ModOptions.EnableDifferentOpColorPickerHSLPos.Value ? self._ttre1 : self._ttre2, self._l, hueArrowCol, true, ModOptions.EnableDifferentOpColorPickerHSLPos.Value ? self._s : 51);
                    self._ttre1.Apply();
                    self._ttre2.Apply();
                    return;
                }
                orig(self);
            }
            public void On_OPColorPicker__HSLSetValue(On.Menu.Remix.MixedUI.OpColorPicker.orig__HSLSetValue orig, OpColorPicker self)
            {
                self._h = self._h == 100 ? 0 : self._h;
                orig(self);
            }
            public void On_OPColorPickerUpdate(On.Menu.Remix.MixedUI.OpColorPicker.orig_Update orig, OpColorPicker self)
            {
                orig(self);
                if (!self.greyedOut)
                {
                    self.TryUpdateNonGreyCPicker();
                }
            }
        }
    }
    public static class MenuInterfaces
    {
       
        public class JollyCoopOOOConfigPages : PositionedMenuObject
        {
            public JollyCoopOOOConfigPages(Menu.Menu menu, ColorChangeDialog.ColorSlider owner, int bodyPartNum) : base(menu, owner, owner.pos)
            {
                colSlider = owner;
                if (oOOPages == null)
                {
                    oOOIDGroups = [];
                    SmallUtils.AddJollySliderIDGroups(oOOIDGroups, owner, bodyPartNum, ColorConfigHooks.JollyCoopConfigHooks.LukkyRGBSliderModOn || ModOptions.ShouldRemoveHSLSliders);
                    oOOPages = new(menu, this, owner.hueSlider, owner.satSlider, owner.litSlider, oOOIDGroups, pos + new Vector2(0, 39.5f))
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
                if (ModOptions.EnableHexCodeTypers.Value && hexInterface == null)
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
                        string[] values = oOOPages.SliderVisualValues;
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
                sprites = [];
                LoadIcon(current, bodyNames);

            }
            public static Dictionary<string, string> LoadFileNames(SlugcatStats.Name name, List<string> bodyNames)
            {
                Dictionary<string, string> bodyPaths = [];
                foreach (string txtpath in SmallUtils.FindFilePaths("colorconfig", ".txt"))
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
                List<MenuIllustration> illus = [];
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
                            file = path.Split('/').Last();
                            folder = path.Replace("/" + file, string.Empty);
                        }
                    }
                    ColorConfigMod.DebugLog($"BodyPart: {bodyNames[i]},Folder: {folder}, File: {file}");
                    MenuIllustration body = new(menu, this, folder, file, file == "colorconfig_showcasesquare" ? new(i * 10, -0.7f) : size / 2, true, true);
                    subObjects.Add(body);
                    illus.Add(body);

                }
                sprites = [.. illus];
            }
            public void LoadIcon(SlugcatStats.Name current, List<string> bodyNames)
            {
                if (sprites != null)
                {
                    foreach (MenuIllustration slugcatSprite in sprites)
                    {
                        slugcatSprite.RemoveSprites();
                        RemoveSubObject(slugcatSprite);
                    }
                }
                sprites = [];
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
                List<Color> rgbs = [];
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
                if (sprites != null && currentRGBs != null && currentRGBs != prevRGBs)
                {
                    prevRGBs = currentRGBs;
                    for (int i = 0; i < currentRGBs.Count; i++)
                    {
                        if (sprites.Length <= i)
                        {
                            continue;
                        }
                        sprites[i].color = currentRGBs[i];
                    }

                }
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                if (sprites != null)
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
            public MenuIllustration[] sprites;
        }
        public class LegacyHexTypeBoxes : PositionedMenuObject
        {
            public LegacyHexTypeBoxes(Menu.Menu menu, MenuObject owner, Vector2 startPos, SlugcatStats.Name current, int batch = 3) : base(menu, owner, startPos)
            {
                batchPerX = batch;
                LoadHexTypeBoxes(current);

            }
            public void LoadHexTypeBoxes(SlugcatStats.Name slugcat)
            {
                if (hexTypeBoxes?.Count > 0)
                {
                    foreach (HexTypeBox hexTypeBox in hexTypeBoxes)
                    {
                        hexTypeBox.RemoveSprites();
                        RemoveSubObject(hexTypeBox);
                    }
                }
                hexTypeBoxes = [];
                for (int i = 0; i < PlayerGraphics.ColoredBodyPartList(slugcat).Count; i++)
                {
                    HexTypeBox hexTypeBox = new(menu, this, new(70 * (i % batchPerX), -40 * (i / batchPerX)));
                    subObjects.Add(hexTypeBox);
                    hexTypeBoxes.Add(hexTypeBox);
                }
            }
            public void SaveNewHSLs(List<Vector3> newHSLs, int colorSwitcher)
            {
                if (hexTypeBoxes?.Count > 0 && newHSLs?.Count > 0)
                {
                    for (int i = 0; i < hexTypeBoxes.Count; i++)
                    {
                        if (newHSLs.Count > i)
                        {
                            hexTypeBoxes[i].SaveNewHSL(newHSLs[i]);
                        }
                    }
                }
                colorSelector = colorSwitcher;
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                if (hexTypeBoxes?.Count > 0)
                {
                    foreach (HexTypeBox hexTypeBox in hexTypeBoxes)
                    {
                        hexTypeBox.RemoveSprites();
                        RemoveSubObject(hexTypeBox);
                    }
                    hexTypeBoxes = null;
                }
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                if (hexTypeBoxes != null)
                {
                    for (int i = 0; i < hexTypeBoxes.Count; i++)
                    {
                        hexTypeBoxes[i].hexTyper.label.scale = i == colorSelector ? Mathf.Lerp(hexTypeBoxes[i].hexTyper.label.scale, 1.06f, timeStacker) : 1;
                    }
                }
            }

            public int batchPerX = 1;
            public int colorSelector = 0;
            public List<HexTypeBox> hexTypeBoxes;
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
                get => Manager.Clipboard;
                set => Manager.Clipboard = value;
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
            public void TryUpdate()
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
                            ColorConfigMod.DebugError($"Failed to parse from new value \"{hexTyper.value}\"");
                            hexTyper.value = lastValue;
                            return;
                        }
                        hexTyper.value = ColorUtility.ToHtmlStringRGB(SmallUtils.Vector32RGB(SmallUtils.RWIIIClamp(SmallUtils.RGB2Vector3(hexCol), CustomColorModel.RGB, out Vector3 clampedHSLHex)));
                        clampedHSLHex = SmallUtils.FixNonHueSliderWonkiness(clampedHSLHex, currentHSL);
                        if (clampedHSLHex != currentHSL)
                        {
                            newPendingHSL = clampedHSLHex;
                            newPendingRGB = ColConversions.HSL2RGB(newPendingHSL);
                            shouldUpdateNewColor = true;
                            currentHSL = newPendingHSL;
                            prevHSL = newPendingHSL;
                        }
                        lastValue = hexTyper.value;
                    }
                }
                if (ShouldCopyOrPaste)
                {
                    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.C))
                    {
                        Copy();
                    }
                    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.V))
                    {
                        Paste();
                    }
                }
            }
            public void Copy()
            {
                Clipboard = hexTyper.value;
                menu.PlaySound(SoundID.MENU_Player_Join_Game);
            }
            public void Paste()
            {
                if (!SmallUtils.IfHexCodeValid(Clipboard, out Color fromPaste))
                {
                    menu.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
                    return;
                }
                hexTyper.value = ColorUtility.ToHtmlStringRGB(SmallUtils.Vector32RGB(
                    SmallUtils.RWIIIClamp(SmallUtils.RGB2Vector3(fromPaste), CustomColorModel.RGB,
                    out Vector3 newClampedHSL)));
                newClampedHSL = SmallUtils.FixNonHueSliderWonkiness(newClampedHSL, currentHSL);
                if (newClampedHSL != currentHSL)
                {
                    newPendingHSL = newClampedHSL;
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
                    TryUpdate();
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
        public class SliderOOOPages : MenuObject, ICanTurnPages, ISingleKeyCodeInput
        {
            public SliderOOOPages(Menu.Menu menu, MenuObject owner, HorizontalSlider slider1, HorizontalSlider slider2, HorizontalSlider slider3, List<SliderOOOIDGroup> sliderOOOIDGroups, Vector2 buttonOffset = default) : base(menu, owner)
            {
                if (slider1 is null || slider2 is null || slider3 is null)
                {
                    ColorConfigMod.DebugError("Sliders in pages are null!");
                    return;
                }
                CurrentOffset = 0;
                sliderO = slider1;
                sliderOO = slider2;
                sliderOOO = slider3;
                OOOIDGroups = sliderOOOIDGroups?.Count == 0 ? [new(slider1.ID, slider2.ID, slider3.ID, null, null)] : sliderOOOIDGroups;
                buttonOffset = buttonOffset == default ? new(0, 0) : buttonOffset;
                setPrevButtonPos = new(buttonOffset.x + slider3.pos.x, -(slider3.size.y * 2) + buttonOffset.y + slider3.pos.y);
                if (PagesOn)
                {
                    ActivateButtons();
                }
            }
            public List<SliderOOOIDGroup> OOOIDGroups { get; private set; }
            public bool PagesOn => OOOIDGroups?.Count > 1;
            public bool ShouldGetInput => PagesOn && !menu.manager.menuesMouseMode && (PrevButton?.Selected == true || NextButton?.Selected == true || sliderO?.Selected == true || sliderOO?.Selected == true || sliderOOO?.Selected == true);
            public string Clipboard
            {
                get => Manager.Clipboard;
                set => Manager.Clipboard = value;
            }
            public string[] SliderVisualValues
            {
                get
                {
                    string slider1Amt = "";
                    string slider2Amt = "";
                    string slider3Amt = "";
                    if (OOOIDGroups?.Count > 0 && OOOIDGroups[CurrentOffset] != null)
                    {
                        if (sliderO != null)
                        {
                            slider1Amt = SmallUtils.GetVisualSliderValue(sliderO.floatValue * OOOIDGroups[CurrentOffset].showMultipler.x, 
                                GetSliderShowInt(0) ? 0 : ModOptions.Digit, showSign ? OOOIDGroups[CurrentOffset].sign1 : "");
                        }
                        if (sliderOO != null)
                        {
                            slider2Amt = SmallUtils.GetVisualSliderValue(sliderOO.floatValue * OOOIDGroups[CurrentOffset].showMultipler.y,
                                GetSliderShowInt(1) ? 0 : ModOptions.Digit, showSign ? OOOIDGroups[CurrentOffset].sign2 : "");
                        }
                        if (sliderOOO != null)
                        {
                            slider3Amt = SmallUtils.GetVisualSliderValue(sliderOOO.floatValue * OOOIDGroups[CurrentOffset].showMultipler.z,
                                GetSliderShowInt(2)? 0 : ModOptions.Digit, showSign ? OOOIDGroups[CurrentOffset].sign3 : "");
                        }
                    }
                    return [slider1Amt, slider2Amt, slider3Amt];
                }
            }
            public bool GetSliderShowInt(int oOO)
            {
                return oOO switch
                {
                    0 => OOOIDGroups?[CurrentOffset]?.showInt1 == true,
                    1 => OOOIDGroups?[CurrentOffset]?.showInt2 == true,
                    2 => OOOIDGroups?[CurrentOffset]?.showInt3 == true,
                    _ => false
                };
            }
            public bool ShouldCopyOrPaste(out HorizontalSlider slider)
            {
                slider = null;
                if (ModOptions.CopyPasteForSliders.Value)
                {
                    if (sliderO?.MouseOver == true)
                    {
                        slider = sliderO;
                    }
                    else if (sliderOO?.MouseOver == true)
                    {
                        slider = sliderOO;

                    }
                    else if (sliderOOO?.MouseOver == true)
                    {
                        slider = sliderOOO;
                    }
                }
                return slider != null;
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
                if (!changingPage)
                {
                    if (sender == PrevButton)
                    {
                        PrevPage();
                    }
                    if (sender == NextButton)
                    {
                        NextPage();
                    }
                }
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                if (OOOIDGroups != null && OOOIDGroups[CurrentOffset] != null)
                {
                    if (sliderO != null)
                    {
                        if (sliderO.menuLabel != null && ((!showValues && sliderO.menuLabel.text != menu.Translate(OOOIDGroups[CurrentOffset].name1)) || showValues))
                        {
                            sliderO.menuLabel.text = menu.Translate(OOOIDGroups[CurrentOffset].name1) + (showValues ? $" {SliderVisualValues[0]}" : "");
                        }
                    }
                    if (sliderOO != null)
                    {
                        if (sliderOO.menuLabel != null && ((!showValues && sliderOO.menuLabel.text != menu.Translate(OOOIDGroups[CurrentOffset].name2)) || showValues))
                        {
                            sliderOO.menuLabel.text = menu.Translate(OOOIDGroups[CurrentOffset].name2) + (showValues ? $" {SliderVisualValues[1]}" : "");
                        }
                    }
                    if (sliderOOO != null)
                    {
                        if (sliderOOO.menuLabel != null && ((!showValues && sliderOOO.menuLabel.text != menu.Translate(OOOIDGroups[CurrentOffset].name3)) || showValues))
                        {
                            sliderOOO.menuLabel.text = menu.Translate(OOOIDGroups[CurrentOffset].name3) + (showValues ? $" {SliderVisualValues[2]}" : "");
                        }
                    }
                }
            }
            public override void Update()
            {
                base.Update();
                TryGetInput();
                TryGetCopyPaste();
            }
            public void NextPage()
            {
                changingPage = true;
                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                CurrentOffset++;
                if (OOOIDGroups == null || OOOIDGroups?.Count == 0 || CurrentOffset > OOOIDGroups.Count - 1)
                {
                    CurrentOffset = 0;
                }
                PopulatePage(CurrentOffset);
                changingPage = false;
            }
            public void PrevPage()
            {
                changingPage = true;
                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                CurrentOffset--;
                if (CurrentOffset < 0)
                {
                    if (OOOIDGroups != null && OOOIDGroups.Count > 0)
                    {
                        CurrentOffset = OOOIDGroups.Count - 1;
                    }
                    else
                    {
                        CurrentOffset = 0;
                    }
                }
                PopulatePage(CurrentOffset);
                changingPage = false;
            }
            public void PopulatePage(int offset)
            {
                CurrentOffset = offset;
                if (OOOIDGroups?.Count > 0 && OOOIDGroups[offset] != null)
                {
                    if (sliderO != null)
                    {
                        sliderO.ID = OOOIDGroups[CurrentOffset].ID1;
                    }
                    if (sliderOO != null)
                    {
                        sliderOO.ID = OOOIDGroups[CurrentOffset].ID2;
                    }
                    if (sliderOOO != null)
                    {
                        sliderOOO.ID = OOOIDGroups[CurrentOffset].ID3;
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
                if (PrevButton == null)
                {
                    PrevButton = new(menu, this, menu.Translate("Prev"), "_BackPageSliders", setPrevButtonPos, new(40, 26), FLabelAlignment.Center, false);
                    subObjects.Add(PrevButton);

                }
                if (NextButton == null)
                {
                    NextButton = new(menu, this, menu.Translate("Next"), "_NextPageSliders", new(setPrevButtonPos.x + 60, setPrevButtonPos.y), new(40, 26), FLabelAlignment.Center, false);
                    subObjects.Add(NextButton);
                }
                menu.MutualHorizontalButtonBind(PrevButton, NextButton);
            }
            public void DeactivateButtons()
            {
                if (PrevButton != null)
                {
                    PrevButton.RemoveSprites();
                    RemoveSubObject(PrevButton);
                    PrevButton = null;
                }
                if (NextButton != null)
                {
                    NextButton.RemoveSprites();
                    RemoveSubObject(NextButton);
                    NextButton = null;
                }
            }
            public void TryGetInput()
            {
                if (ShouldGetInput && !changingPage)
                {
                    if (RWInput.PlayerUIInput(-1).pckp)
                    {
                        PrevPage();
                    }
                    if (RWInput.PlayerUIInput(-1).thrw)
                    {
                        NextPage();
                    }
                }
            }
            public void TryGetCopyPaste()
            {
                if (ShouldCopyOrPaste(out HorizontalSlider slider))
                {
                    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.C))
                    {
                        Copy(slider);
                    }
                    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.V))
                    {
                        Paste(slider);
                    }
                }
            }
            public void Copy(HorizontalSlider horizontalSlider)
            {
                float copyValue = horizontalSlider.floatValue;
                copyValue = SmallUtils.ChangeValueBasedOnMultipler(copyValue, GetMultipler(horizontalSlider));
                Clipboard = copyValue.ToString();
                ColorConfigMod.DebugLog($"Slider Copier: Copied.. {copyValue}");
                menu.PlaySound(SoundID.MENU_Player_Join_Game);
            }
            public void Paste(HorizontalSlider horizontalSlider)
            {
                if (float.TryParse(Clipboard, NumberStyles.Integer | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                    CultureInfo.InvariantCulture, out float newValue))
                {
                    ColorConfigMod.DebugLog($"Slider Paster: Got.. {newValue}");
                    newValue = SmallUtils.ChangeValueBasedOnMultipler(newValue, GetMultipler(horizontalSlider), true);
                    ColorConfigMod.DebugLog($"Slider Paster: Slider value parse.. {newValue}");
                    if (horizontalSlider.floatValue != newValue)
                    {
                        horizontalSlider.floatValue = newValue;
                        menu.PlaySound(SoundID.MENU_Switch_Page_In);
                        return;
                    }
                }
                menu.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
            }
            public float GetMultipler(HorizontalSlider horizontalSlider)
            {
                if (OOOIDGroups?[CurrentOffset] != null)
                {

                    if (horizontalSlider == sliderO)
                    {
                        return OOOIDGroups[CurrentOffset].showMultipler.x;
                    }
                    if (horizontalSlider == sliderOO)
                    {
                        return OOOIDGroups[CurrentOffset].showMultipler.y;
                    }
                    if (horizontalSlider == sliderOOO)
                    {
                        return OOOIDGroups[CurrentOffset].showMultipler.z;
                    }
                }
                return 1;
            }
            public Vector2 setPrevButtonPos;
            public bool changingPage = false, showValues = true, showSign = true;
            public HorizontalSlider sliderO, sliderOO, sliderOOO;
            public int CurrentOffset { get; private set; }
            public BigSimpleButton PrevButton { get; private set; } 
            public BigSimpleButton NextButton { get; private set; }
        }
        public class SliderOOOIDGroup(Slider.SliderID sliderID1, Slider.SliderID sliderID2, Slider.SliderID sliderID3,
            string[] names, bool[] showInts, Vector3 multipler = default, string[] signs = null)
        {
            public List<string> Names => [name1, name2, name3];
            public List<Slider.SliderID> SliderIDs => [ID1, ID2, ID3];

            public Slider.SliderID ID1 = sliderID1, ID2 = sliderID2, ID3 = sliderID3;
            public Vector3 showMultipler = multipler == default ? new(1, 1, 1) : multipler;
            public string name1 = names.GetValueOrDefault(0, ""), name2 = names.GetValueOrDefault(1, ""), name3 = names.GetValueOrDefault(2, ""), 
                sign1 = signs.GetValueOrDefault(0, ""), sign2 = signs.GetValueOrDefault(1, ""), sign3 = signs.GetValueOrDefault(2, "");
            public bool showInt1 = showInts.GetValueOrDefault(0, false), showInt2 = showInts.GetValueOrDefault(1, false), showInt3 = showInts.GetValueOrDefault(2, false);
        }
        public interface ISingleKeyCodeInput
        {
            bool ShouldGetInput { get; }
            void TryGetInput();
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
