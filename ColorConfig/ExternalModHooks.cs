using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Menu;
using JollyCoop.JollyMenu;
using RainMeadow;
using ColorConfig.WeakUITable;
using UnityEngine;

namespace ColorConfig
{
    public static partial class ColorConfigHooks
    {
        public static void ExternalModHooks()
        {
            if (ColorConfigMod.IsLukkyRGBColorSliderModOn) LukkyRGBModHooks.ApplyLukkyModHooks();
            if (ColorConfigMod.IsRainMeadowOn)
            {
                try
                {
                    ColorConfigMod.DebugLog("Rain meadow found! trying to apply hooks!");
                    RainMeadowHooks.ApplyRainMeadowHooks();
                    ColorConfigMod.DebugLog("Rain meadow hooks success!");
                }
                catch (Exception ex)
                {
                    ColorConfigMod.DebugException("Unable to load hooks on to rain meadow, either classes were renamed or non-existant", ex);
                }
            }
        }
        public static class LukkyRGBModHooks
        {
            public static void ApplyLukkyModHooks()
            {
                ColorConfigMod.DebugLog("Initialising Lukky RGB Slider Hooks");
                try
                {
                    typeof(LukkyMod.Main).GetMethod("ColorSlider_RGB2HSL", BindingFlags.Instance | BindingFlags.NonPublic).HookMethod(delegate (Action<LukkyMod.Main, On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_RGB2HSL,
                 ColorChangeDialog.ColorSlider> orig, LukkyMod.Main main, On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_RGB2HSL origCode,
                 ColorChangeDialog.ColorSlider colSlider)
                    {
                        origCode(colSlider);
                    });
                    typeof(LukkyMod.Main).GetMethod("ColorSlider_HSL2RGB", BindingFlags.Instance | BindingFlags.NonPublic).HookMethod(delegate (Action<LukkyMod.Main, On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_HSL2RGB,
                       ColorChangeDialog.ColorSlider> orig, LukkyMod.Main main, On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_HSL2RGB origCode,
                       ColorChangeDialog.ColorSlider colSlider)
                    {
                        origCode(colSlider);
                    });
                    ColorConfigMod.DebugLog("Sucessfully Initialised Lukky RGB Slider Hooks");
                }
                catch (Exception ex)
                {
                    ColorConfigMod.DebugException("Unable to load hooks on to lukky RGB Sliders, either renamed or non-existant", ex);
                }
            
            }
        }
        public static class RainMeadowHooks
        {
            public static void ApplyRainMeadowHooks() => ApplyRainMeadowArenaConfigHooks();
            public static void ApplyRainMeadowArenaConfigHooks()
            {
                try
                {
                    typeof(ColorSlugcatDialog).GetMethod("AddColorButtons", BindingFlags.Instance | BindingFlags.Public).HookMethod(new Action<Action<ColorSlugcatDialog>, ColorSlugcatDialog>((orig, self) =>
                    {
                        orig(self);
                        if (ModOptions.Instance.EnableRainMeadowArenaConfigExtension.Value && ModOptions.Instance.EnableSlugcatDisplay.Value && ModOptions.Instance.EnableLegacyIdeaSlugcatDisplay.Value && self.GetExtraSSMInterface().slugcatDisplay == null)
                        {
                            self.GetExtraSSMInterface().slugcatDisplay = new(self, self.pages[0], self.bodyInterface!.pos + new Vector2(self.bodyInterface.PerPage * 80 + 80, -60), new(45, 45), self.id);
                            self.pages[0].subObjects.Add(self.GetExtraSSMInterface().slugcatDisplay);
                        }
                    }));
                    typeof(ColorSlugcatDialog).GetMethod("RemoveColorButtons", BindingFlags.Instance | BindingFlags.Public).HookMethod(new Action<Action<ColorSlugcatDialog>, ColorSlugcatDialog>((orig, self) =>
                    {
                        orig(self);
                        SmallUtils.ClearMenuObject(self.pages[0], ref self.GetExtraSSMInterface().slugcatDisplay);
                    }));
                    typeof(ColorSlugcatDialog).GetMethod("AddColorInterface", BindingFlags.Instance | BindingFlags.Public).HookMethod(new Action<Action<ColorSlugcatDialog>, ColorSlugcatDialog>((orig, self) =>
                    {
                        orig(self);
                        if (!ModOptions.Instance.EnableRainMeadowArenaConfigExtension.Value) return;
                        ExtraSSMInterfaces extraInterfaces = self.GetExtraSSMInterface();
                        if (extraInterfaces.sliderPages == null)
                        {
                            extraInterfaces.sliderPages = new(self, self.pages[0], [self.hueSlider!, self.satSlider!, self.litSlider!], SSMSliderIDGroups, new Vector2(0f, 30f))
                            {
                                showValues = ModOptions.ShowVisual,
                                roundingType = ModOptions.Instance.SliderRounding.Value,
                                DecimalCount = ModOptions.DeCount,
                            };
                            self.pages[0].subObjects.Add(extraInterfaces.sliderPages);
                            extraInterfaces.sliderPages.PopulatePage(extraInterfaces.sliderPages.CurrentOffset);
                            if (extraInterfaces.sliderPages.PagesOn)
                            {
                                self.defaultColor!.pos.y -= 30;
                                self.MutualVerticalButtonBind(self.defaultColor, extraInterfaces.sliderPages.prevButton);
                                self.MutualVerticalButtonBind(extraInterfaces.sliderPages.nextButton, self.litSlider);
                                extraInterfaces.sliderPages.nextButton.MenuObjectBind(self.defaultColor, bottom: true);
                                extraInterfaces.sliderPages.prevButton.MenuObjectBind(self.litSlider, top: true);
                            }
                        }
                        if (ModOptions.Instance.EnableSlugcatDisplay.Value && extraInterfaces.slugcatDisplay == null)
                        {
                            extraInterfaces.slugcatDisplay = new(self, self.pages[0], self.bodyInterface!.pos + new Vector2(self.bodyInterface.PerPage * 80 + 80, -60), new(45, 45), self.id);
                            self.pages[0].subObjects.Add(extraInterfaces.slugcatDisplay);
                        }
                        if (extraInterfaces.hexInterface == null && ModOptions.Instance.EnableHexCodeTypers.Value)
                        {
                            extraInterfaces.hexInterface = new(self, self.pages[0], new(self.defaultColor!.pos.x + self.defaultColor.size.x + 10, self.defaultColor.pos.y))
                            {
                                saveNewTypedColor = (hexTyper, hsl, rgb) => { self.ApplyHSL(hsl); self.hueSlider.UpdateSliderValue(); self.satSlider.UpdateSliderValue(); self.litSlider.UpdateSliderValue(); }
                            };
                            self.pages[0].subObjects.Add(extraInterfaces.hexInterface);
                        }

                    }));
                    typeof(ColorSlugcatDialog).GetMethod("RemoveColorInterface", BindingFlags.Instance | BindingFlags.Public).HookMethod(new Action<Action<ColorSlugcatDialog>, ColorSlugcatDialog>((orig, self) =>
                    {
                        ExtraSSMInterfaces extraInterfaces = self.GetExtraSSMInterface();
                        SmallUtils.ClearMenuObject(self.pages[0], ref extraInterfaces.sliderPages);
                        if (!ModOptions.Instance.EnableLegacyIdeaSlugcatDisplay.Value)
                            SmallUtils.ClearMenuObject(self.pages[0], ref extraInterfaces.slugcatDisplay);
                        SmallUtils.ClearMenuObject(self.pages[0], ref extraInterfaces.hexInterface);
                        orig(self);
                    }));
                    typeof(ColorSlugcatDialog).GetMethod("Update", BindingFlags.Instance | BindingFlags.Public).HookMethod(new Action<Action<ColorSlugcatDialog>, ColorSlugcatDialog>((orig, self) =>
                    {
                        orig(self);
                        ExtraSSMInterfaces extraInterfaces = self.GetExtraSSMInterface();
                        extraInterfaces.hexInterface?.SaveNewHSL(self.GetHSL());
                        extraInterfaces.slugcatDisplay?.LoadNewHSLStringSlugcat(self.manager.rainWorld.progression.miscProgressionData.colorChoices[self.id.value]/*, ssM.slugcatColorOrder[ssM.slugcatPageIndex]*/);
                    }));
                    typeof(ColorSlugcatDialog).GetMethod("SliderSetValue", BindingFlags.Instance | BindingFlags.Public).HookMethod(new Action<Action<ColorSlugcatDialog, Slider, float>, ColorSlugcatDialog, Slider, float>((orig, self, slider, f) =>
                    {
                        MenuToolObj.CustomSliderSetHSL(slider, f, self.GetHSL(), self.ApplyHSL);
                        orig(self, slider, f);
                    }));
                    typeof(ColorSlugcatDialog).GetMethod("ValueOfSlider", BindingFlags.Instance | BindingFlags.Public).HookMethod(new Func<Func<ColorSlugcatDialog, Slider, float>, ColorSlugcatDialog, Slider, float>((orig, self, slider) =>
                    {
                        if (MenuToolObj.CustomHSLValueOfSlider(slider, self.GetHSL(), out float f)) return f;
                        return orig(self, slider);
                    }));
                    ColorConfigMod.DebugLog("successfully applied arena color config hooks!");
                }
                catch (Exception ex)
                {
                    ColorConfigMod.DebugException("Unable to load hooks on to rain meadow arena color config, either renamed or non-existant", ex);
                }
            }
        }
        public static class BingoModUtils
        {
            public static void TryApplyBingoColors(Menu.Menu menu, string message)
            {
                if (message == "STARTBINGO" || message == "LOADBINGO")
                    menu.TrySaveExpeditionColorsToCustomColors(SmallUtils.ExpeditionSlugcat());
            }
        }
    }
}
