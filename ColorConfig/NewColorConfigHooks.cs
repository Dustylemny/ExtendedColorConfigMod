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
using BepInEx;
namespace ColorConfig
{
    public static class ColorConfigHooks
    {
        public static void Init()
        {
            MenuHooks menuHooks = new();
            SlugcatSelectMenuHooks ssmHooks = new();
            JollyConfigHooks jollyConfigHooks = new();
            OpConfigHooks opColorPickerHooks = new();
            menuHooks.Init();
            ssmHooks.Init();
            jollyConfigHooks.Init();
            opColorPickerHooks.Init();

        }
        public class MenuHooks
        {
            public void Init()
            {
                try
                {
                    On.RainWorld.Update += On_RainWorld_Update;
                    On.Menu.MenuObject.Update += On_MenuObject_Update;
                    On.Menu.MenuObject.Singal += On_MenuObject_Singal;
                    ColorConfigMod.DebugLog("Successfully initialized Menuobject hooks");
                }
                catch (Exception ex)
                {
                    ColorConfigMod.DebugException("Failed to initialize Menuobject hooks", ex);
                }
            }
            public void On_RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
            {
                orig(self);
                ColorConfigMod.lastFemInput = ColorConfigMod.femInput;
                ColorConfigMod.femInput = SmallUtils.GetFixedExtraMenuInput();
            }
            public void On_MenuObject_Update(On.Menu.MenuObject.orig_Update orig, MenuObject self)
            {
                orig(self);
                if (self is MenuInterfaces.IGetOwnInput non_MouseInput)
                {
                    non_MouseInput.TryGetInput();
                }
                if (self is MenuInterfaces.ICopyPasteConfig copyPasteConfig)
                {
                    copyPasteConfig.TryGetCopyPaste();
                }

            }
            public void On_MenuObject_Singal(On.Menu.MenuObject.orig_Singal orig, MenuObject self, MenuObject sender, string message)
            {
                orig(self, sender, message);
                if (self is MenuInterfaces.ICanTurnPages turningPages)
                {
                    turningPages.TrySingal(sender, message);
                }
            }

        }
        public class SlugcatSelectMenuHooks
        {
            public void Init()
            {
                try
                {
                    //IL.Menu.SlugcatSelectMenu.CustomColorInterface.ctor += IL_SlugcatSelectMenu_CustomColorInterface_ctor;
                    //IL.Menu.SlugcatSelectMenu.StartGame += IL_SlugcatSelectMenu_StartGame;
                    //IL.Menu.SlugcatSelectMenu.SliderSetValue += IL_SlugcatSelectMenu_SliderSetValue;
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

            //Slugcat Select Menu Hooks
            public void On_SlugcatSelectMenu_AddColorButtons(On.Menu.SlugcatSelectMenu.orig_AddColorButtons orig, SlugcatSelectMenu self)
            {
                orig(self);
                if (ModOptions.EnableSlugcatDisplay.Value && ModOptions.EnableLegacyIdeaSlugcatDisplay.Value && ssMslugcatDisplay == null)
                {
                    Vector2 vector = new(1000f - (1366f - self.manager.rainWorld.options.ScreenSize.x) / 2f, self.manager.rainWorld.options.ScreenSize.y - 100f);
                    vector.y -= ModManager.JollyCoop ? 40 : 0;
                    vector.y -= self.colorInterface != null ? self.colorInterface.bodyColors.Length * 40 : 0;
                    ssMslugcatDisplay = new(self, self.pages[0], new(vector.x + 140, vector.y + 40), new(45f, 45f),
                    self.slugcatColorOrder[self.slugcatPageIndex]);
                    self.pages[0].subObjects.Add(ssMslugcatDisplay);
                }
            }
            public void On_SlugcatSelectMenu_RemoveColorButtons(On.Menu.SlugcatSelectMenu.orig_RemoveColorButtons orig, SlugcatSelectMenu self)
            {
                orig(self);
                self.pages[0].ClearMenuObject(ref ssMslugcatDisplay);
            }
            public void On_SlugcatSelectMenu_AddColorInterface(On.Menu.SlugcatSelectMenu.orig_AddColorInterface orig, SlugcatSelectMenu self)
            {
                orig(self);
                AddSliderInterface(self, SSMSliderIDGroups);
                if (ModOptions.EnableSlugcatDisplay.Value && ssMslugcatDisplay == null)
                {
                    ssMslugcatDisplay = new(self, self.pages[0], new(self.satSlider.pos.x + 140, self.satSlider.pos.y + 80), new(45f, 45f),
                        self.slugcatColorOrder[self.slugcatPageIndex]);
                    self.pages[0].subObjects.Add(ssMslugcatDisplay);

                }
                if (ModOptions.EnableHexCodeTypers.Value && ssMhexInterface == null)
                {
                    ssMhexInterface = new(self, self.pages[0], self.defaultColorButton.pos + new Vector2(120, 0));
                    self.pages[0].subObjects.Add(ssMhexInterface);
                    ssMhexInterface.elementWrapper.MenuObjectBind(ssMOOOPages.PagesOn ? ssMOOOPages.nextButton : self.litSlider, top: true);
                    ssMhexInterface.elementWrapper.MenuObjectBind(ssMLegacySliders != null ? ssMLegacySliders.sliderO : self.nextButton, bottom: true);
                }
            }
            public void On_SlugcatSelectMenu_RemoveColorInterface(On.Menu.SlugcatSelectMenu.orig_RemoveColorInterface orig, SlugcatSelectMenu self)
            {
                orig(self);
                self.pages[0].ClearMenuObject(ref ssMLegacySliders);
                self.pages[0].ClearMenuObject(ref ssMOOOPages);
                if (!ModOptions.EnableLegacyIdeaSlugcatDisplay.Value)
                {
                    self.pages[0].ClearMenuObject(ref ssMslugcatDisplay);
                }
                self.pages[0].ClearMenuObject(ref ssMhexInterface);
                //self.pages[0].ClearMenuObject(ref ssMLegacyHexInterface);
            }
            public void On_SlugcatSelectMenu_Update(On.Menu.SlugcatSelectMenu.orig_Update orig, SlugcatSelectMenu self)
            {
                orig(self);
                /*if (ModOptions.RMStoryMenuSlugcatFix.Value && ColorConfigMod.IsRainMeadowOn)
                {
                    RainMeadowHooks.RainMeadowSyncSlugcatWithUpdate(self);
                }*/
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
            public void AddSliderInterface(SlugcatSelectMenu ssM, List<MenuInterfaces.SliderOOOIDGroup> sliderOOOIDGroups)
            {
                if (ModOptions.ShouldAddSSMLegacySliders && ssMLegacySliders == null)
                {
                    ssMLegacySliders = new(ssM, ssM.pages[0], ssM.defaultColorButton.pos + new Vector2(0, -40), new(0, -40), new(200, 40), [..sliderOOOIDGroups.Exclude(0)]);
                    ssM.pages[0].subObjects.Add(ssMLegacySliders);
                    ssM.MutualVerticalButtonBind(ssMLegacySliders.sliderO, ssM.defaultColorButton);
                    ssM.MutualVerticalButtonBind(ssM.nextButton, ssMLegacySliders.oOOPages.PagesOn ? ssMLegacySliders.oOOPages.prevButton : ssMLegacySliders.sliderOOO);
                }
                if (ssMOOOPages == null)
                {
                    ssMOOOPages = new(ssM, ssM.pages[0], ssM.hueSlider, ssM.satSlider, ssM.litSlider, ssMLegacySliders != null? [..sliderOOOIDGroups[0].ToSingleList()] : sliderOOOIDGroups, new(0, 25))
                    {
                        showValues = ModOptions.ShowVisual,
                        roundingType = ModOptions.SliderRounding.Value,
                    };
                    ssM.pages[0].subObjects.Add(ssMOOOPages);
                    ssMOOOPages.PopulatePage(ssMOOOPages.CurrentOffset);
                    if (ssMOOOPages.PagesOn)
                    {
                        ssM.defaultColorButton.pos.y -= 40;
                        ssM.MutualVerticalButtonBind(ssM.defaultColorButton, ssMOOOPages.prevButton);
                        ssM.MutualVerticalButtonBind(ssMOOOPages.nextButton, ssM.litSlider);
                    }
                }
            }
            public void CustomSlidersSetValue(SlugcatSelectMenu ssM, Slider slider, float f)
            {
                Vector3 ssMHSL = ssM.SlugcatSelectMenuHSL();
                if (MenuToolObj.RGBSliderIDS.Contains(slider?.ID))
                {
                    Color color = ColConversions.HSL2RGB(ssMHSL);
                    color = slider.ID == MenuToolObj.RedRGB ? new(f, color.g, color.b) : slider.ID == MenuToolObj.GreenRGB ? new(color.r, f, color.b) : new(color.r, color.g, f);
                    SmallUtils.Vector32RGB(SmallUtils.RWIIIClamp(SmallUtils.RGB2Vector3(SmallUtils.RGBClamp01(color)), CustomColorModel.RGB, out Vector3 newRGBHSL));
                    ssM.SaveHSLString(SmallUtils.SetHSLSaveString(SmallUtils.FixNonHueSliderWonkiness(newRGBHSL, ssMHSL)));

                }
                if (MenuToolObj.HSVSliderIDS.Contains(slider?.ID))
                {
                    Vector3 hsv = ColConversions.HSL2HSV(ssMHSL);
                    hsv = slider.ID == MenuToolObj.HueHSV ? new(f, hsv.y, hsv.z) : slider.ID == MenuToolObj.SatHSV ? new(hsv.x, f, hsv.z) : new(hsv.x, hsv.y, f);
                    SmallUtils.RWIIIClamp(hsv, CustomColorModel.HSV, out  Vector3 newHSVHSL, !ModOptions.DisableHueSliderMaxClamp.Value);
                    ssM.SaveHSLString(SmallUtils.SetHSLSaveString(newHSVHSL));
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
                if (ssMhexInterface != null)
                {
                    ssMhexInterface.SaveNewHSL(SmallUtils.SlugcatSelectMenuHSL(ssM));
                    if (ssMhexInterface.shouldUpdateNewColor)
                    {
                        ssMhexInterface.shouldUpdateNewColor = false;
                        ssM.SaveHSLString(SmallUtils.SetHSLSaveString(ssMhexInterface.newPendingHSL));
                        ssM.SliderSetValue(ssM.hueSlider, ssM.ValueOfSlider(ssM.hueSlider));
                        ssM.SliderSetValue(ssM.satSlider, ssM.ValueOfSlider(ssM.satSlider));
                        ssM.SliderSetValue(ssM.litSlider, ssM.ValueOfSlider(ssM.litSlider));

                    }
                }
                /*if (ssMLegacyHexInterface != null)
                {
                    ssMLegacyHexInterface.SaveNewHSLs(ssM.SlugcatSelectMenuHSLs(), ssM.activeColorChooser);
                    if (ssMLegacyHexInterface.hexTypeBoxes?.Count > 0)
                    {
                        for (int i = 0; i < ssMLegacyHexInterface.hexTypeBoxes.Count; i++)
                        {
                            if (ssMLegacyHexInterface.hexTypeBoxes[i]?.shouldUpdateNewColor == true)
                            {
                                ssMLegacyHexInterface.hexTypeBoxes[i].shouldUpdateNewColor = false;
                                ssM.SaveHSLString(i, SmallUtils.SetHSLSaveString(ssMLegacyHexInterface.hexTypeBoxes[i].newPendingHSL));
                                if (ssM.activeColorChooser == i)
                                {
                                    ssM.SliderSetValue(ssM.hueSlider, ssM.ValueOfSlider(ssM.hueSlider));
                                    ssM.SliderSetValue(ssM.satSlider, ssM.ValueOfSlider(ssM.satSlider));
                                    ssM.SliderSetValue(ssM.litSlider, ssM.ValueOfSlider(ssM.litSlider));
                                }
                                else if (ssM.colorInterface?.bodyColors?.Length > i)
                                {
                                    ssM.colorInterface.bodyColors[i].color = ColConversions.HSL2RGB(ssMLegacyHexInterface.hexTypeBoxes[i].newPendingHSL);
                                }
                            }
                        }


                    }
                }*/
                ssMslugcatDisplay?.LoadNewHSLStringSlugcat(ssM.manager.rainWorld.progression.miscProgressionData.colorChoices[ssM.slugcatColorOrder[ssM.slugcatPageIndex].value]/*, ssM.slugcatColorOrder[ssM.slugcatPageIndex]*/);
            }
            public static List<MenuInterfaces.SliderOOOIDGroup> SSMSliderIDGroups
            {
                get
                {
                    List<MenuInterfaces.SliderOOOIDGroup> result = [];
                    SmallUtils.AddSSMSliderIDGroups(result, ModOptions.ShouldRemoveHSLSliders);
                    return result;
                }
            }
            public MenuInterfaces.HexTypeBox ssMhexInterface;
            public MenuInterfaces.SlugcatDisplay ssMslugcatDisplay;
            public MenuInterfaces.SliderOOOPages ssMOOOPages;

            //Legacy versions stuff
            public MenuInterfaces.LegacySSMSliders ssMLegacySliders;
            //public MenuInterfaces.LegacySSMHexTypeBoxes ssMLegacyHexInterface; removed legacyHexCodes
            public static class RainMeadowHooks
            {
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
        public class JollyConfigHooks
        {
            public void Init()
            {
                //IL.JollyCoop.JollyMenu.ColorChangeDialog.ValueOfSlider += IL_Dialog_ValueOfSlider;
                //IL.JollyCoop.JollyMenu.ColorChangeDialog.SliderSetValue += IL_Dialog_SliderSetValue;
                On.JollyCoop.JollyMenu.ColorChangeDialog.ValueOfSlider += On_Dialog_ValueOfSlider;
                On.JollyCoop.JollyMenu.ColorChangeDialog.SliderSetValue += On_Dialog_SliderSetValue;
                On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.ctor += On_Color_Slider_ctor;
                On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.RemoveSprites += On_ColorSlider_RemoveSprites;
                //On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.HSL2RGB += On_ColorSlider_HSL2RGB;
                //On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.RGB2HSL += On_ColorSlider_RGB2HSL;
                ColorConfigMod.DebugLog("Sucessfully extended color interface for jolly coop menu!");
                if (ColorConfigMod.IsLukkyRGBColorSliderModOn)
                {
                    LukkyRGBModHooks.ApplyLukkyModHooks();
                }
            }
            //jolly-coop
            public void On_Color_Slider_ctor(On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_ctor orig, ColorChangeDialog.ColorSlider self, Menu.Menu menu, MenuObject owner, Vector2 pos, int playerNumber, int bodyPart, string sliderTitle)
            {
                orig(self, menu, owner, pos, playerNumber, bodyPart, sliderTitle);
                if (!jollyColSliders.Contains(self))
                {
                    jollyColSliders.Add(self);
                }
                if (!jollyConfigPages.ContainsKey(self))
                {
                    MenuInterfaces.JollyCoopOOOConfigPages jollyCoopOOOConfigPages = new(menu, self, bodyPart, ModOptions.ShouldRemoveHSLSliders || ModOptions.FollowLukkyRGBSliders);
                    self.subObjects.Add(jollyCoopOOOConfigPages);
                    jollyConfigPages.Add(self, jollyCoopOOOConfigPages);
                    jollyCoopOOOConfigPages.oOOPages.PopulatePage(jollyCoopOOOConfigPages.oOOPages.CurrentOffset);

                }
            }
            public void On_ColorSlider_RemoveSprites(On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_RemoveSprites orig, ColorChangeDialog.ColorSlider self)
            {
                orig(self);
                if (jollyColSliders.Contains(self))
                {
                    jollyColSliders.Remove(self);
                }
                if (jollyConfigPages.ContainsKey(self))
                {
                    jollyConfigPages[self].RemoveSprites();
                    self.RemoveSubObject(jollyConfigPages[self]);
                    jollyConfigPages.Remove(self);
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
                        jollyColSliders.Count > colSliderNum &&
                        jollyColSliders[colSliderNum] != null)
                    {
                        if (array[2] == "RGB" && MenuToolObj.RGBNames.Contains(array[3]))
                        {
                            Color color = jollyColSliders[colSliderNum].color;
                            color = array[3] == "RED" ? new(f, color.g, color.b) : array[3] == "GREEN" ? new(color.r, f, color.b) : new(color.r, color.g, f);
                            jollyColSliders[colSliderNum].color = SmallUtils.RWIIIClamp(color.RGB2Vector3(), CustomColorModel.RGB, out Vector3 newHSL).Vector32RGB();
                            jollyColSliders[colSliderNum].hslColor = SmallUtils.FixNonHueSliderWonkiness(newHSL, jollyColSliders[colSliderNum].hslColor.HSL2Vector3()).Vector32HSL();
                        }
                        if (array[2] == "HSV" && MenuToolObj.HSVNames.Contains(array[3]))
                        {
                            Vector3 hsv = ColConversions.HSL2HSV(jollyColSliders[colSliderNum].hslColor.HSL2Vector3());
                            hsv = array[3] == "HUE" ? new(f, hsv.y, hsv.z) : array[3] == "SAT" ? new(hsv.x, f, hsv.z) : new(hsv.x, hsv.y, f);
                            SmallUtils.RWIIIClamp(hsv, CustomColorModel.HSV, out Vector3 newHSL, !shouldGetfullHue);
                            jollyColSliders[colSliderNum].hslColor = newHSL.Vector32HSL();
                            jollyColSliders[colSliderNum].HSL2RGB();
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
                        jollyColSliders.Count > colSliderNum &&
                        jollyColSliders[colSliderNum] != null)
                    {
                        if (array[2] == "RGB" && MenuToolObj.RGBNames.Contains(array[3]))
                        {
                            f = array[3] == "RED" ? jollyColSliders[colSliderNum].color.r : array[3] == "GREEN" ? jollyColSliders[colSliderNum].color.g : jollyColSliders[colSliderNum].color.b;
                            return true;
                        }
                        else if (array[2] == "HSV" && MenuToolObj.HSVNames.Contains(array[3]))
                        {
                            Vector3 hsv = ColConversions.HSL2HSV(jollyColSliders[colSliderNum].hslColor.HSL2Vector3());
                            f = array[3] == "HUE" ? hsv.x : array[3] == "SAT" ? hsv.y : hsv.z;
                            return true;
                        }
                    }

                }
                return false;
            }

            public static bool shouldGetfullHue = false;

            public readonly Dictionary<ColorChangeDialog.ColorSlider, MenuInterfaces.JollyCoopOOOConfigPages> jollyConfigPages = [];
            public readonly List<ColorChangeDialog.ColorSlider> jollyColSliders = [];
            public static class LukkyRGBModHooks
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
        public class OpConfigHooks
        {
            public void Init()
            {
                OtherOpColorPickerHooks();
                IL.Menu.Remix.MixedUI.OpColorPicker.Change += IL_OPColorPicker_Change;
                IL.Menu.Remix.MixedUI.OpColorPicker._HSLSetValue += IL_OPColorPicker__HSLSetValue;
                IL.Menu.Remix.MixedUI.OpColorPicker.MouseModeUpdate += IL_OPColorPicker_MouseModeUpdate;
                IL.Menu.Remix.MixedUI.OpColorPicker.GrafUpdate += IL_OPColorPicker_GrafUpdate;
                On.Menu.Remix.MixedUI.OpColorPicker.ctor += On_OPColorPicker_Ctor;
                On.Menu.Remix.MixedUI.OpColorPicker._RecalculateTexture += On_OPColorPickerRecalculateTexture;
                On.Menu.Remix.MixedUI.OpColorPicker.Update += On_OPColorPicker_Update;
                On.Menu.Remix.MixedUI.OpColorPicker.GrafUpdate += On_OPColorPicker_GrafUpdate;
                ColorConfigMod.DebugLog("Successfully extended color interface for OpColorPicker!");
            }

            //op-colorpicker
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
                          (ModOptions.EnableDifferentOpColorPickerHSLPos.Value && self._mode == OpColorPicker.PickerMode.HSL))
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
                    ColorConfigMod.DebugError("Failed to find after hsl set value");
                    return;
                }
                ColorConfigMod.DebugILCursor(cursor);
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
                    //ColorConfigMod.DebugLog("Successfully patched set_value for hsv");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("Failed to patch set_value for hsv", e);
                }
            }
            public void IL_OPColorPicker_Change(ILContext iL)
            {
                ILCursor cursor = new(iL);
                if (!cursor.TryGotoNext(MoveType.After, (x) => x.MatchCall(typeof(Custom), "HSL2RGB"),
                    (x) => x.MatchCallvirt<FSprite>("set_color")))
                {
                    ColorConfigMod.DebugError("Failed to find set HSL Color");
                    return;
                }
                ColorConfigMod.DebugILCursor(cursor);
                try
                {
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(new Action<OpColorPicker>((self) =>
                    {
                        self._cdis0.color = ModOptions.HSL2HSVOPColorPicker.Value ? ColConversions.HSV2RGB(self.HslFromColorPicker()) : self._cdis0.color;
                    }));
                    //ColorConfigMod.DebugLog("IL_OPColorPicker_Change: Successfully patched _cdis0 Color");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("Failed to patch _cdis0 Color", e);
                }
            }
            public void IL_OPColorPicker__HSLSetValue(ILContext il)
            {
                ILCursor cursor = new(il);
                if (!cursor.TryGotoNext(MoveType.After, (x) => x.MatchCall(typeof(Custom), "HSL2RGB"), (x) => x.MatchStloc(0)))
                {
                    ColorConfigMod.DebugError("Failed to find set RGB Color");
                    return;
                }
                ColorConfigMod.DebugILCursor(cursor);
                try
                {
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldloca, 0);
                    cursor.EmitDelegate(delegate (OpColorPicker self, ref Color col)
                    {
                        col = ModOptions.HSL2HSVOPColorPicker.Value ? ColConversions.HSV2RGB(self.HslFromColorPicker()) : 
                        ModOptions.EnableBetterOPColorPicker.Value && self._h == 100? ColConversions.HSL2RGB(self.HslFromColorPicker()): col;
                    });
                    //ColorConfigMod.DebugLog("IL_OPColorPicker__HSLSetValue: Successfully patched RGB Color");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("Failed to patch RGB Color", e);
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
                    ColorConfigMod.DebugILCursor(cursor);
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
                    //ColorConfigMod.DebugLog("Sucessfully patched MiniFocus SatHue!");
                    // ^^^^^^
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("Failed to patch for MiniFocus SatHue", e);
                }
                try
                {
                    cursor.GotoNext(MoveType.After, visiblityMatch);
                    ColorConfigMod.DebugILCursor(cursor);
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
                    ColorConfigMod.DebugException("Failed to patch Minifocus for Lit", e);
                }
                try
                {
                    cursor.GotoNext(MoveType.After, PickerModeSwitch);
                    cursor.GotoNext(MoveType.After, colorMatch);
                    ColorConfigMod.DebugILCursor(cursor);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldloc, 11);
                    cursor.EmitDelegate(new Action<OpColorPicker, int>((self, litHue) =>
                    {
                        if (ModOptions.EnableDifferentOpColorPickerHSLPos.Value)
                        {
                            litHue = litHue > 99 ? 99 : litHue;
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
                    ColorConfigMod.DebugException("Failed to patch HoverMouse for Hue", e);
                }
                try
                {
                    cursor.GotoNext(MoveType.After, colorMatch);
                    ColorConfigMod.DebugILCursor(cursor);
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
                    ColorConfigMod.DebugException("Failed to patch HoverMouse for Sat and Lit", e);
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
                    ColorConfigMod.DebugILCursor(cursor);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(new Action<OpColorPicker>((self) =>
                    {
                        if (ModOptions.EnableDifferentOpColorPickerHSLPos.Value)
                        {
                            self._lblB.color = self._lblR.color;
                            self._lblR.color = self.colorText;
                        }
                    }));
                    //ColorConfigMod.DebugLog("Successfully patched hue2lit text color");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("Failed to patch hue2lit text color", e);
                }
                try
                {
                    cursor.GotoNext(MoveType.After, (x) => x.MatchNewobj<Vector2>(), (x) => x.MatchCallvirt<GlowGradient>("set_pos"));
                    ColorConfigMod.DebugILCursor(cursor);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(new Action<OpColorPicker>((self) =>
                    {
                        if (ModOptions.EnableDifferentOpColorPickerHSLPos.Value && self.MenuMouseMode)
                        {
                            self._focusGlow.pos = new(104f, 25f);
                        }
                    }));
                    //ColorConfigMod.DebugLog("Successfully patched focus glow for hue2lit text");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("Failed to fix focus glow for hue2lit text", e);
                }
                try
                {
                    cursor.GotoNext((x) => x.MatchLdarg(0), (x) => x.MatchLdfld<OpColorPicker>(nameof(OpColorPicker._lblB)),
                     (x) => x.MatchLdarg(0), (x) => x.MatchLdfld<OpColorPicker>(nameof(OpColorPicker.colorText)));
                    cursor.GotoNext((x) => x.MatchLdarg(0), (x) => x.MatchLdfld<OpColorPicker>(nameof(OpColorPicker._focusGlow)));
                    ColorConfigMod.DebugILCursor(cursor);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(new Action<OpColorPicker>((self) =>
                    {
                        if (ModOptions.EnableDifferentOpColorPickerHSLPos.Value && self.MenuMouseMode)
                        {
                            self._lblR.color = self._lblB.color;
                            self._lblB.color = self.colorText;
                        }
                    }));
                    //ColorConfigMod.DebugLog("Successfully patched lit2hue text color");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("Failed to fix lit2hue text color", e);
                }
                try
                {
                    cursor.GotoNext(MoveType.After, (x) => x.MatchNewobj<Vector2>(), (x) => x.MatchCallvirt<GlowGradient>("set_pos"));
                    ColorConfigMod.DebugILCursor(cursor);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(new Action<OpColorPicker>((self) =>
                    {
                        if (ModOptions.EnableDifferentOpColorPickerHSLPos.Value && self.MenuMouseMode)
                        {
                            self._focusGlow.pos = new Vector2(104f, 105f);
                        }
                    }));
                    //ColorConfigMod.DebugLog("Successfully patched focus glow for lit2hue text");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("Failed to fix focus glow for lit2hue text", e);
                }
            }
            public void On_OPColorPicker_Ctor(On.Menu.Remix.MixedUI.OpColorPicker.orig_ctor orig, OpColorPicker self, Configurable<Color> config, Vector2 pos)
            {
                orig(self, config, pos);
                self._lblHSL.text = ModOptions.HSL2HSVOPColorPicker.Value ? "HSV" : self._lblHSL.text;
            }
            public void On_OPColorPickerRecalculateTexture(On.Menu.Remix.MixedUI.OpColorPicker.orig__RecalculateTexture orig, OpColorPicker self)
            {
                if (self._mode == OpColorPicker.PickerMode.HSL && (ModOptions.EnableDifferentOpColorPickerHSLPos.Value || ModOptions.HSL2HSVOPColorPicker.Value))
                {
                    self._ttre1 = new(ModOptions.EnableDifferentOpColorPickerHSLPos.Value ? 101 : 100, 101)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Point
                    };
                    self._ttre2 = new(10, 101)
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
            public void On_OPColorPicker_Update(On.Menu.Remix.MixedUI.OpColorPicker.orig_Update orig, OpColorPicker self)
            {
                orig(self);
                if (!self.greyedOut && self.CurrentlyFocusableMouse)
                {
                    if (SmallUtils.CopyShortcutPressed())
                    {
                        if (self._MouseOverHex())
                        {
                            self.CopyHexCPicker();
                        }
                        if (ModOptions.CopyPasteForColorPickerNumbers.Value && self.IfCPickerNumberHovered(out int oOO))
                        {
                            self.CopyNumberCPicker(oOO);
                        }
                    }
                    if (SmallUtils.PasteShortcutPressed())
                    {
                        if (self._MouseOverHex())
                        {
                            self.PasteHexCPicker();
                        }
                        if (ModOptions.CopyPasteForColorPickerNumbers.Value && self.IfCPickerNumberHovered(out int oOO))
                        {
                            self.PasteNumberCPicker(oOO);
                        }
                    }
                }
            }
            public void On_OPColorPicker_GrafUpdate(On.Menu.Remix.MixedUI.OpColorPicker.orig_GrafUpdate orig, OpColorPicker self, float timeStacker)
            {
                orig(self, timeStacker);
                if (!self.greyedOut)
                {
                    if (ModOptions.CopyPasteForColorPickerNumbers.Value)
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
        }
    }
    public static class MenuInterfaces
    {
        public class JollyCoopOOOConfigPages : PositionedMenuObject
        {
            public JollyCoopOOOConfigPages(Menu.Menu menu, ColorChangeDialog.ColorSlider owner, int bodyPartNum, bool removeHSL = false) : base(menu, owner, owner.pos)
            {
                colSlider = owner;
                if (oOOPages == null)
                {
                    oOOIDGroups = [];
                    SmallUtils.AddJollySliderIDGroups(oOOIDGroups, owner, bodyPartNum, removeHSL);
                    oOOPages = new(menu, this, owner.hueSlider, owner.satSlider, owner.litSlider, oOOIDGroups, new Vector2(0, 39.5f), -pos)
                    {
                        roundingType = ModOptions.SliderRounding.Value,
                        showValues = false
                    };
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
                this.ClearMenuObject(ref hexInterface);
                this.ClearMenuObject(ref valueLabel);
                this.ClearMenuObject(ref oOOPages);
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
            //removed current and prev slugcat, assuming slugcat doesnt change midway while updating (meant for Story menu)
            public SlugcatDisplay(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, SlugcatStats.Name current) : base(menu, owner, pos, size)
            {
                /*currentSlugcat = current;
                prevSlugcat = current;*/
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
                    ColorConfigMod.DebugLog($"Slugcat Display loader.. BodyPart: {bodyNames[i]},Folder: {(folder == ""? "Illustrations" : folder)}, File: {file}");
                    MenuIllustration body = new(menu, this, folder, file, file == "colorconfig_showcasesquare" ? new(i * 10, -0.7f) : size / 2, true, true);
                    subObjects.Add(body);
                    illus.Add(body);

                }
                sprites = [.. illus];
            }
            public void LoadIcon(SlugcatStats.Name current, List<string> bodyNames)
            {
                this.ClearMenuObjectList(ref sprites, true);
                LoadSlugcatSprites(current, bodyNames);

            }
            public void LoadNewColorSlugcat(List<Color> slugcatCols/*, SlugcatStats.Name name*/)
            {
                while (slugcatCols.Count < bodyNames.Count)
                {
                    slugcatCols.Add(Color.white);
                }
                currentRGBs = slugcatCols;
            }
            public void LoadNewHSLStringSlugcat(List<string> slugcatHSLColos/*, SlugcatStats.Name name*/)
            {
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
                this.ClearMenuObjectList(ref sprites);
            }

            //no need to find for currentslugcat and prev if unchanged in slugcat select menu
            //public SlugcatStats.Name currentSlugcat, prevSlugcat;
            public List<Color> currentRGBs, prevRGBs;
            public List<string> bodyNames;
            public MenuIllustration[] sprites;
        }
        public class LegacySSMSliders : PositionedMenuObject
        {
            public LegacySSMSliders(Menu.Menu menu, MenuObject owner, Vector2 startPos, Vector2 offset, Vector2 size, List<SliderOOOIDGroup> oOOIDGroups, Vector2 buttonOffset = default, 
                bool subtle = false) : base(menu, owner, startPos)
            {
                SetUpSliders(offset, size, subtle, oOOIDGroups, buttonOffset);
            }
            public void SetUpSliders(Vector2 offset, Vector2 size, bool subtle, List<SliderOOOIDGroup> oOOIDGroups, Vector2 buttonOffset)
            {
                if (sliderO == null)
                {
                    sliderO = new(menu, this, "", Vector2.zero, size, null, subtle);
                    subObjects.Add(sliderO);
                }
                if (sliderOO == null)
                {
                    sliderOO = new(menu, this, "", Vector2.zero + offset, size, null, subtle);
                    subObjects.Add(sliderOO);
                }
                if (sliderOOO == null)
                {
                    sliderOOO = new(menu, this, "", Vector2.zero + (offset * 2), size, null, subtle);
                    subObjects.Add(sliderOOO);
                }
                menu.MutualVerticalButtonBind(sliderOOO, sliderOO);
                menu.MutualVerticalButtonBind(sliderOO, sliderO);
                if (oOOPages == null)
                {
                    oOOPages = new(menu, this, sliderO, sliderOO, sliderOOO, oOOIDGroups, buttonOffset == default ? new(0, 40) : buttonOffset)
                    {
                        showValues = ModOptions.ShowVisual,
                        roundingType = ModOptions.SliderRounding.Value,
                    };
                    subObjects.Add(oOOPages);
                    oOOPages.PopulatePage(oOOPages.CurrentOffset);
                }
                if (oOOPages.PagesOn)
                {
                    menu.MutualVerticalButtonBind(oOOPages.nextButton, sliderOOO);
                }
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                this.ClearMenuObject(ref oOOPages);
                this.ClearMenuObject(ref sliderO);
                this.ClearMenuObject(ref sliderOO);
                this.ClearMenuObject(ref sliderOOO);
            }
            public SliderOOOPages oOOPages;
            public HorizontalSlider sliderO, sliderOO, sliderOOO;
        }
        public class HexTypeBox : PositionedMenuObject, ICopyPasteConfig
        {
            public HexTypeBox(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
            {
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
            public virtual string Clipboard
            {
                get => Manager.Clipboard;
                set => Manager.Clipboard = value;
            }
            public virtual void SaveNewHSL(Vector3 hsl)
            {
                currentHSL = hsl;
            }
            public virtual void SaveNewRGB(Color rgb)
            {
                currentHSL = Custom.RGB2HSL(rgb);
            }
            public virtual void Copy()
            {
                Clipboard = hexTyper.value;
                menu.PlaySound(SoundID.MENU_Player_Join_Game);
            }
            public virtual void Paste()
            {
                string unParsedValue = Clipboard?.Trim();
                if (!SmallUtils.IfHexCodeValid(unParsedValue, out Color fromPaste) || hexTyper.value.IsHexCodesSame(unParsedValue))
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
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                this.ClearMenuObject(ref elementWrapper);
                this.ClearMenuObject(ref tabWrapper);
                if (hexTyper != null)
                {
                    hexTyper.label.alpha = 0;
                    hexTyper.label.RemoveFromContainer();
                    hexTyper.rect.container.RemoveFromContainer();
                    hexTyper = null;
                }
            }
            public virtual void TryGetCopyPaste()
            {
                if (hexTyper?.MouseOver == true)
                {
                    //More accurate getting if copy or paste is released instead of Input.GetKeyUp()
                    if (SmallUtils.CopyShortcutPressed())
                    {
                        Copy();
                    }
                    if (SmallUtils.PasteShortcutPressed())
                    {
                        Paste();
                    }
                }

            }

            public string lastValue;
            public bool shouldUpdateNewColor = false;
            public Color newPendingRGB;
            public Vector3 currentHSL, prevHSL, newPendingHSL;
            public MenuTabWrapper tabWrapper;
            public UIelementWrapper elementWrapper;
            public OpTextBox hexTyper;
        }
        public class SliderOOOPages : PositionedMenuObject, ICanTurnPages, IGetOwnInput, ICopyPasteConfig
        {
            public static float ChangeValueBasedOnMultipler(float newValue, float multipler, bool recieve = false)
            {
                float result = recieve ? newValue / multipler : newValue * multipler;
                return recieve ? Mathf.Clamp01(result) : result;
            }
            public static string GetVisualSliderValue(float visualValue, int decimalPlaces, string sign, MidpointRounding roundType = MidpointRounding.AwayFromZero)
            {
                //basically last digit is less than 5, go to 0, more than five go to 10
                double amt = Math.Round(visualValue, decimalPlaces, roundType);
                return amt.ToString() + sign;
            }
            public SliderOOOPages(Menu.Menu menu, MenuObject owner, HorizontalSlider slider1, HorizontalSlider slider2, HorizontalSlider slider3, 
                List<SliderOOOIDGroup> sliderOOOIDGroups, Vector2 buttonOffset = default, Vector2 pos = default) : base(menu, owner, pos == default? Vector2.zero : pos)
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
                this.ClearMenuObject(ref sliderO);
                this.ClearMenuObject(ref sliderOO);
                this.ClearMenuObject(ref sliderOOO);
                if (OOOIDGroups?.Count > 0)
                {
                    OOOIDGroups.Clear();
                    OOOIDGroups = null;
                }
            }
            public override void Update()
            {
                base.Update();
                TryGetInput();
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
            public void TrySingal(MenuObject sender, string message)
            {
                if (sender == prevButton)
                {
                    PrevPage();
                }
                if (sender == nextButton)
                {
                    NextPage();
                }

            }
            public void NextPage()
            {
                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                CurrentOffset++;
                if (OOOIDGroups == null || OOOIDGroups?.Count == 0 || CurrentOffset > OOOIDGroups.Count - 1)
                {
                    CurrentOffset = 0;
                }
                PopulatePage(CurrentOffset);
            }
            public void PrevPage()
            {
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
            }
            public void PopulatePage(int offset)
            {
                if (offset < 0 || offset > OOOIDGroups?.Count)
                {
                    ColorConfigMod.DebugWarning("offset is more than sliderIDs count or less than 0!");
                    return;
                }
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
                    sliderO.floatValue = sliderO.floatValue;
                }
                if (sliderOO != null)
                {
                    sliderOO.floatValue = sliderOO.floatValue;
                }
                if (sliderOOO != null)
                {
                    sliderOOO.floatValue = sliderOOO.floatValue;
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
                this.ClearMenuObject(ref prevButton);
                this.ClearMenuObject(ref nextButton);
            }
            public void TryGetInput()
            {
                lastInput = input;
                input = SmallUtils.FixedPlayerUIInput(-1); //makes it so map and grab is counted
                if (!menu.manager.menuesMouseMode && PagesOn && (prevButton?.Selected == true || nextButton?.Selected == true || sliderO?.Selected == true || sliderOO?.Selected == true || sliderOOO?.Selected == true))
                {
                    //changes to map and grab since normal ui input doesnt count it in menu in normal circumstances
                    if (!input.mp && lastInput.mp)
                    {
                        PrevPage();
                    }
                    if (!input.pckp && lastInput.pckp)
                    {
                        NextPage();
                    }
                }

            }
            public void TryGetCopyPaste()
            {
                if (ShouldCopyOrPaste(out HorizontalSlider slider))
                {
                    if (SmallUtils.CopyShortcutPressed())
                    {
                        Copy(slider);
                    }
                    if (SmallUtils.PasteShortcutPressed())
                    {
                        Paste(slider);
                    }
                }
            }
            public void Copy(HorizontalSlider horizontalSlider)
            {
                float copyValue = horizontalSlider.floatValue;
                copyValue = ChangeValueBasedOnMultipler(copyValue, GetMultipler(horizontalSlider));
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
                    newValue = ChangeValueBasedOnMultipler(newValue, GetMultipler(horizontalSlider), true);
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
            public bool showValues = true, showSign = true;
            public BigSimpleButton prevButton;
            public BigSimpleButton nextButton;
            public HorizontalSlider sliderO, sliderOO, sliderOOO;
            public Player.InputPackage input, lastInput;
            public MidpointRounding roundingType = MidpointRounding.AwayFromZero;
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
                            slider1Amt = GetVisualSliderValue(sliderO.floatValue * OOOIDGroups[CurrentOffset].showMultipler.x,
                                GetSliderShowInt(0) ? 0 : ModOptions.DeCount, showSign ? OOOIDGroups[CurrentOffset].sign1 : "", roundingType);
                        }
                        if (sliderOO != null)
                        {
                            slider2Amt = GetVisualSliderValue(sliderOO.floatValue * OOOIDGroups[CurrentOffset].showMultipler.y,
                                GetSliderShowInt(1) ? 0 : ModOptions.DeCount, showSign ? OOOIDGroups[CurrentOffset].sign2 : "", roundingType);
                        }
                        if (sliderOOO != null)
                        {
                            slider3Amt = GetVisualSliderValue(sliderOOO.floatValue * OOOIDGroups[CurrentOffset].showMultipler.z,
                                GetSliderShowInt(2) ? 0 : ModOptions.DeCount, showSign ? OOOIDGroups[CurrentOffset].sign3 : "", roundingType);
                        }
                    }
                    return [slider1Amt, slider2Amt, slider3Amt];
                }
            }
            public bool PagesOn => OOOIDGroups?.Count > 1;
            public int CurrentOffset { get; private set; }
            public List<SliderOOOIDGroup> OOOIDGroups { get; private set; }
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
        public interface IGetOwnInput
        {
            void TryGetInput();
        }
        public interface ICanTurnPages
        {
            int CurrentOffset { get; }
            bool PagesOn { get; }
            void PopulatePage(int offset);
            void NextPage();
            void PrevPage();
            void TrySingal(MenuObject sender, string message);
        }
        public interface ICopyPasteConfig
        {
            string Clipboard { get; set; }
            void TryGetCopyPaste();
        }
        public struct ExtraFixedMenuInput(bool cpy = false, bool paste = false)
        {
            public bool cpy = cpy, pste = paste;
        }

    }
}
