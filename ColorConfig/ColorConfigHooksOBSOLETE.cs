using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using JollyCoop.JollyMenu;
using RWCustom;
using MoreSlugcats;
using UnityEngine;
using static ColorConfig.MenuToolObj;
using static ColorConfig.ColConversions;
using static ColorConfig.MenuInterfacesOBSOLETE;

namespace ColorConfig
{
    public static class Hooks
    {
        public class SlugcatSelectScreenHooksOBSOLETE
        {
            public void Init()
            {
                try
                {
                    On.Menu.SlugcatSelectMenu.AddColorInterface += On_AddColorInterface;
                    On.Menu.SlugcatSelectMenu.RemoveColorInterface += On_RemoveColorInterface;
                    On.Menu.SlugcatSelectMenu.Singal += On_Singal;
                    On.Menu.SlugcatSelectMenu.ValueOfSlider += On_ValueOfSlider;
                    On.Menu.SlugcatSelectMenu.SliderSetValue += On_SliderSetValue;
                    On.Menu.SlugcatSelectMenu.Update += On_Update;
                    On.Menu.HorizontalSlider.GrafUpdate += OnHorizontalGrafUpdate;

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
                Vector2 slidersPos = new(1000f - (1366f - self.manager.rainWorld.options.ScreenSize.x) / 2f, self.manager.rainWorld.options.ScreenSize.y - 100f);
                if (self.colorInterface != null)
                {
                    slidersPos[1] -= self.colorInterface.bodyColors.Length * 40;
                }
                if (ModOptions.enableSlugcatDisplay.Value)
                {
                    if (slugcatDisplay == null)
                    {
                        slugcatDisplay = new(self, self.pages[0], new(slidersPos.x + 130, slidersPos.y + 40), new(45f, 45f), self.slugcatColorOrder[self.slugcatPageIndex],
                         self.manager.rainWorld.progression.miscProgressionData.colorChoices[self.slugcatColorOrder[self.slugcatPageIndex].value]);
                        self.pages[0].subObjects.Add(slugcatDisplay);
                    }
                }
                AddSliderInterface(self, slidersPos);
                if (ModOptions.enableHexCodeTypers.Value)
                {
                    if (hexInterface == null)
                    {
                        Vector2 hexPos = slidersPos + new Vector2(120, 90);
                        if (self.defaultColorButton != null)
                        {
                            hexPos = self.defaultColorButton.pos;
                            hexPos += new Vector2(120, 0);
                        }
                        hexInterface = new(self, self.pages[0], hexPos, ModOptions.ShowVisual);
                        self.pages[0].subObjects.Add(hexInterface);
                    }
                }
            }
            private void On_RemoveColorInterface(On.Menu.SlugcatSelectMenu.orig_RemoveColorInterface orig, SlugcatSelectMenu self)
            {
                orig(self);
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
                RemoveCustomInterfaces(self);
            }
            private void On_Update(On.Menu.SlugcatSelectMenu.orig_Update orig, SlugcatSelectMenu self)
            {
                orig(self);
                UpdateConfigs(self);
            }
            private void On_Singal(On.Menu.SlugcatSelectMenu.orig_Singal orig, SlugcatSelectMenu self, MenuObject sender, string message)
            {
                orig(self, sender, message);
                CustomSlidersSingal(self, message);
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
            private void OnHorizontalGrafUpdate(On.Menu.HorizontalSlider.orig_GrafUpdate orig, HorizontalSlider self, float timeStacker)
            {
                orig(self, timeStacker);
                if (self.menu is SlugcatSelectMenu)
                {
                    if ((ModManager.MMF || ModManager.JollyCoop) && ModOptions.ShowVisual && !ModOptions.EnablePages)
                    {
                        if (self.ID == MMFEnums.SliderID.Hue && self.menuLabel != null)
                        {
                            self.menuLabel.text = self.menu.Translate("HUE") + $" {Math.Round(self.floatValue * 360, 0, MidpointRounding.AwayFromZero)}" + degreeSign;
                        }
                        if (self.ID == MMFEnums.SliderID.Saturation && self.menuLabel != null)
                        {
                            self.menuLabel.text = self.menu.Translate("SAT") + $" {Math.Round(self.floatValue * 100, ModOptions.Digit, MidpointRounding.AwayFromZero)}%";
                        }
                        if (self.ID == MMFEnums.SliderID.Lightness && self.menuLabel != null)
                        {
                            self.menuLabel.text = self.menu.Translate("LIT") + $" {Math.Round(self.floatValue * 100, ModOptions.Digit, MidpointRounding.AwayFromZero)}%";
                        }
                    }
                }
            }

            //CustomSlidersHooks
            private void AddSliderInterface(SlugcatSelectMenu ssM, Vector2 pos)
            {
                if (ModOptions.EnablePages)
                {
                    if (sliderPages == null)
                    {
                        sliderPages = new(ssM, ssM.pages[0], pos, new(200, 30), 0, -2.5f, showAmtAll: ModOptions.ShowVisual);
                        ssM.pages[0].subObjects.Add(sliderPages);
                    }
                    if (ModOptions.enableRGBSliders.Value)
                    {
                        if (rgbInterface == null)
                        {
                            rgbInterface = new(ssM, ssM.pages[0], pos, new(200, 30),
                                new[] { RedRGB, GreenRGB, BlueRGB },
                                new[] { red, green, blue }, showAmt: ModOptions.ShowVisual, showInt: rgbShowInt,
                                showMultipler: rgbMultipler);
                            ssM.pages[0].subObjects.Add(rgbInterface);
                            sliderPages.AddSliderInterface(rgbInterface);
                        }
                    }
                    if (ssM.hueSlider != null && ssM.satSlider != null && ssM.litSlider != null)
                    {
                        if (hslInterface == null)
                        {
                            hslInterface = new(ssM, sliderPages, pos, new(200, 30),
                                new[] { MMFEnums.SliderID.Hue, MMFEnums.SliderID.Saturation, MMFEnums.SliderID.Lightness },
                                new[] { hue, sat, lit }, showAmt: ModOptions.ShowVisual,
                                showInt: hueOOShowInt,
                                showMultipler: hueOOMultipler);
                            hslInterface.Deactivate();
                            hslInterface.slider1 = ssM.hueSlider;
                            hslInterface.slider2 = ssM.satSlider;
                            hslInterface.slider3 = ssM.litSlider;
                            hslInterface.ForceOwnerSlider();
                            sliderPages.AddSliderInterface(hslInterface);
                        }
                    }
                    if (ModOptions.enableHSVSliders.Value)
                    {
                        if (hsvInterface == null)
                        {
                            hsvInterface = new(ssM, ssM.pages[0], pos, new(200, 30),
                                new[] { HueHSV, SatHSV, ValHSV },
                                new[] { hue, sat, value }, showAmt: ModOptions.ShowVisual, showInt: hueOOShowInt,
                                showMultipler: hueOOMultipler);
                            sliderPages.AddSliderInterface(hsvInterface);
                        }
                    }
                    if (!ModManager.JollyCoop && !hasChangedPos)
                    {
                        hasChangedPos = true;
                        ssM.defaultColorButton.pos.y -= 40;
                    }
                    ssM.MutualVerticalButtonBind(sliderPages.SliderInterfaces[sliderPages.currentOffset].slider1, ssM.colorInterface.bodyButtons[ssM.colorInterface.bodyButtons.Length - 1]);
                    ssM.MutualVerticalButtonBind(ssM.defaultColorButton, sliderPages.prevButton != null ? sliderPages.prevButton : sliderPages.SliderInterfaces[sliderPages.currentOffset].slider3);
                }

            }
            private void RemoveCustomInterfaces(SlugcatSelectMenu ssM)
            {
                if (sliderPages != null)
                {
                    sliderPages.RemoveSprites();
                    ssM.pages[0].RemoveSubObject(sliderPages);
                    sliderPages = null;
                }
                if (rgbInterface != null)
                {
                    rgbInterface.RemoveSprites();
                    ssM.pages[0].RemoveSubObject(rgbInterface);
                    rgbInterface = null;
                }
                if (hslInterface != null)
                {
                    hslInterface.RemoveSprites();
                    hslInterface = null;
                }
                if (hsvInterface != null)
                {
                    hsvInterface.RemoveSprites();
                    ssM.pages[0].RemoveSubObject(hsvInterface);
                    hsvInterface = null;
                }
                hasChangedPos = false;
            }
            private void CustomSlidersSingal(SlugcatSelectMenu ssM, string message)
            {
                if (message == "DEFAULTCOL")
                {
                    if (rgbInterface != null && !rgbInterface.Deactivated)
                    {
                        ssM.SliderSetValue(rgbInterface.slider1, ssM.ValueOfSlider(rgbInterface.slider1));
                        ssM.SliderSetValue(rgbInterface.slider2, ssM.ValueOfSlider(rgbInterface.slider2));
                        ssM.SliderSetValue(rgbInterface.slider3, ssM.ValueOfSlider(rgbInterface.slider3));
                    }
                    if (hsvInterface != null && !hsvInterface.Deactivated)
                    {
                        ssM.SliderSetValue(hsvInterface.slider1, ssM.ValueOfSlider(hsvInterface.slider1));
                        ssM.SliderSetValue(hsvInterface.slider2, ssM.ValueOfSlider(hsvInterface.slider2));
                        ssM.SliderSetValue(hsvInterface.slider3, ssM.ValueOfSlider(hsvInterface.slider3));
                    }
                }
            }
            private bool ValueOfCustomSliders(SlugcatSelectMenu ssM, Slider slider, out float f)
            {
                f = 0;
                Vector3 ssMHSL = SmallUtils.SlugcatSelectMenuHSL(ssM);
                if (rgbInterface?.SliderIDs?.Contains(slider.ID) == true)
                {
                    Color color = ColConversions.HSL2RGB(ssMHSL);
                    if (slider.ID == RedRGB)
                    {
                        f = color.r;
                        return true;
                    }
                    if (slider.ID == GreenRGB)
                    {
                        f = color.g;
                        return true;
                    }
                    if (slider.ID == BlueRGB)
                    {
                        f = color.b;
                        return true;
                    }
                }
                if (hsvInterface?.SliderIDs?.Contains(slider.ID) == true)
                {
                    Vector3 hsv = ColConversions.HSL2HSV(ssMHSL);
                    if (slider.ID == HueHSV)
                    {
                        f = hsv.x;
                        return true;
                    }
                    if (slider.ID == SatHSV)
                    {
                        f = hsv.y;
                        return true;
                    }
                    if (slider.ID == ValHSV)
                    {
                        f = hsv.z;
                        return true;
                    }
                }
                return false;
            }
            private void CustomSlidersSetValue(SlugcatSelectMenu ssM, Slider slider, float f)
            {
                Vector3 ssMHSL = SmallUtils.SlugcatSelectMenuHSL(ssM);
                if (rgbInterface != null && rgbInterface.SliderIDs.Contains(slider.ID))
                {
                    Color color = ColConversions.HSL2RGB(ssMHSL);
                    if (slider.ID == RedRGB)
                    {
                        color.r = f;
                    }
                    else if (slider.ID == GreenRGB)
                    {
                        color.g = f;
                    }
                    else if (slider.ID == BlueRGB)
                    {
                        color.b = f;
                    }
                    SmallUtils.Vector32RGB(SmallUtils.RWIIIClamp(SmallUtils.RGB2Vector3(SmallUtils.RGBClamp01(color)),
                        CustomColorModel.RGB, out Vector3 hsl));
                    SaveHSL(ssM, SmallUtils.SetHSLSaveString(hsl));

                }
                if (hsvInterface != null && hsvInterface.SliderIDs.Contains(slider.ID))
                {
                    Vector3 hsv = ColConversions.HSL2HSV(ssMHSL);

                    if (slider.ID == HueHSV)
                    {
                        hsv.x = Mathf.Clamp(f, 0, 0.99f);
                    }
                    else if (slider.ID == SatHSV)
                    {
                        hsv.y = f;
                    }
                    else if (slider.ID == ValHSV)
                    {
                        hsv.z = f;
                    }
                    SmallUtils.RWIIIClamp(hsv,
                       CustomColorModel.HSV, out Vector3 newHSL);
                    SaveHSL(ssM, SmallUtils.SetHSLSaveString(newHSL));
                }

            }
            private void UpdateConfigs(SlugcatSelectMenu ssM)
            {
                if (sliderPages != null && sliderPages.updateMutualButton)
                {
                    ssM.MutualVerticalButtonBind(sliderPages.SliderInterfaces[sliderPages.currentOffset].slider1, ssM.colorInterface.bodyButtons[ssM.colorInterface.bodyButtons.Length - 1]);
                    ssM.MutualVerticalButtonBind(ssM.defaultColorButton, sliderPages.prevButton != null ? sliderPages.prevButton : sliderPages.SliderInterfaces[sliderPages.currentOffset].slider3);
                }
                if (hexInterface != null)
                {
                    Vector3 vector = SmallUtils.SlugcatSelectMenuHSL(ssM);
                    hexInterface.SetNewHSLColor(vector);
                    if (hexInterface.updateCol == true)
                    {
                        ssM.manager.rainWorld.progression.miscProgressionData.colorChoices[ssM.slugcatColorOrder[ssM.slugcatPageIndex].value]
                                [ssM.activeColorChooser] = SmallUtils.SetHSLSaveString(hexInterface.pendingNewHSL);
                        if (ssM.colorInterface != null)
                        {
                            ssM.colorInterface.bodyColors[ssM.activeColorChooser].color =
                            Custom.HSL2RGB(hexInterface.pendingNewHSL.x, hexInterface.pendingNewHSL.y, hexInterface.pendingNewHSL.z);
                        }
                        hexInterface.updateCol = false;
                    }
                }
                slugcatDisplay?.LoadNewHSL(ssM.manager.rainWorld.progression.miscProgressionData.colorChoices[ssM.slugcatColorOrder[ssM.slugcatPageIndex].value], ssM.slugcatColorOrder[ssM.slugcatPageIndex]);
            }
            private void SaveHSL(SlugcatSelectMenu ssM, string newHSL)
            {
                ssM.manager.rainWorld.progression.miscProgressionData.colorChoices[ssM.slugcatColorOrder[ssM.slugcatPageIndex].value]
                    [ssM.activeColorChooser] = newHSL;
            }

            private bool hasChangedPos = false;
            private SlugcatDisplay slugcatDisplay;
            private HexInterface hexInterface;
            private SliderIIIInterface hslInterface, rgbInterface, hsvInterface;
            private SlidersInterfacePages sliderPages;
        }
        public class JollyMenuHooksOBSOLETE
        {
            public void Init()
            {
                try
                {
                    On.JollyCoop.JollyMenu.ColorChangeDialog.ValueOfSlider += OnDialog_ValueOfSlider;
                    On.JollyCoop.JollyMenu.ColorChangeDialog.SliderSetValue += OnDialog_SliderSetValue;
                    On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.ctor += OnColorSlider_ctor;
                    On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.HSL2RGB += OnColorSlider_HSL2RGB;
                    On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.RGB2HSL += OnColorSlider_RGB2HSL;
                    On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.RemoveSprites += OnColorSlider_RemoveSprites;
                    On.Menu.PositionedMenuObject.Update += OnPosMenuObject_Update;
                    ColorConfigMod.DebugLog("Sucessfully extended color interface for jolly coop menu!");
                }
                catch (Exception ex)
                {
                    ColorConfigMod.DebugException("Failed to initalise hooks for JollyMenu color interface!", ex);
                }
            }
            //JollyColorConfigHooks
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
                if (slider.ID.value.Contains("DUSTY"))
                {
                    string[] array = slider.ID.value.Split(new char[]
                    {
                  '_'
                    });
                    if (int.TryParse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture, out int num))
                    {
                        ColorChangeDialog.ColorSlider colSlider = colorInterfaces[num];
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
            //colorSliderHooks
            private void OnColorSlider_ctor(On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_ctor orig, ColorChangeDialog.ColorSlider self, Menu.Menu menu, MenuObject owner, Vector2 pos, int playerNumber, int bodyPart, string sliderTitle)
            {
                orig(self, menu, owner, pos, playerNumber, bodyPart, sliderTitle);
                colorInterfaces.Add(self);
                Vector2 sliderSize = new(ColorChangeDialog.ColorSlider.GetSliderWidth(menu.CurrLang), 30);
                JollyPageSliderInterface colorPageInterface = new(menu, self, pos, sliderSize, bodyPart);
                colorPageInterface.AddHSLInterface(self.hueSlider, self.satSlider, self.litSlider, bodyPart, ColorConfigMod.IsRGBColorSliderModOn);
                self.subObjects.Add(colorPageInterface);
                if (ModOptions.enableHexCodeTypers.Value)
                {
                    HexInterface hexInterface = new(menu, self, pos + new Vector2(135, -107f), false);
                    colorHexInterface.Add(self, hexInterface);
                    self.subObjects.Add(hexInterface);
                }
            }
            private void OnColorSlider_RemoveSprites(On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_RemoveSprites orig, ColorChangeDialog.ColorSlider self)
            {
                if (colorHexInterface.ContainsKey(self))
                {
                    if (colorHexInterface[self] != null)
                    {
                        colorHexInterface[self].RemoveSprites();
                        self.RemoveSubObject(colorHexInterface[self]);
                    }
                    colorHexInterface.Remove(self);
                }
                if (colorInterfaces.Contains(self))
                {
                    colorInterfaces.Remove(self);
                }
                orig(self);
            }
            private void OnColorSlider_HSL2RGB(On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_HSL2RGB orig, ColorChangeDialog.ColorSlider self)
            {
                orig(self);
                if (self.hslColor.rgb != self.color)
                {
                    self.color = self.hslColor.rgb;
                }
            }
            private void OnColorSlider_RGB2HSL(On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_RGB2HSL orig, ColorChangeDialog.ColorSlider self)
            {
                orig(self);
                Vector3 hslCol = SmallUtils.HSL2Vector3(JollyCoop.JollyCustom.RGB2HSL(self.color));
                if (SmallUtils.HSL2Vector3(self.hslColor) != hslCol)
                {
                    self.hslColor = SmallUtils.Vector32HSL(hslCol);
                }
            }
            private void OnPosMenuObject_Update(On.Menu.PositionedMenuObject.orig_Update orig, PositionedMenuObject self)
            {
                orig(self);
                UpdateColorSlider(self);
            }
            //Utils
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
                                f = ValueOfOOO(colorInterfaces[num], ooo, CustomColorModel.RGB);
                                return true;
                            }
                        }
                        else if (slider.ID.value.Contains("HSV"))
                        {
                            if (FindOOO(slider.ID.value, CustomColorModel.HSV, out int ooo))
                            {
                                f = ValueOfOOO(colorInterfaces[num], ooo, CustomColorModel.HSV);
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
                    CustomColorModel.HSL => OOOSwitchResult(OOO, HSL2HSV(SmallUtils.HSL2Vector3(colSlider.hslColor)), maxClamp: new(0.99f, 1, 1)),
                    _ => OOOSwitchResult(OOO, SmallUtils.HSL2Vector3(colSlider.hslColor),new(0, 0, 0.01f), new(0.99f, 1, 1))
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
                    Vector3 hsv = HSL2HSV(SmallUtils.HSL2Vector3(colSlider.hslColor));
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
            //HexInterface Update
            private void UpdateColorSlider(PositionedMenuObject posObj)
            {
                if (posObj is ColorChangeDialog.ColorSlider colSlider && posObj.menu is ColorChangeDialog)
                {
                    if (colorHexInterface.ContainsKey(colSlider))
                    {

                        colorHexInterface[colSlider].SetNewHSLColor(SmallUtils.HSL2Vector3(colSlider.hslColor));
                        if (colorHexInterface[colSlider].updateCol == true)
                        {
                            colorHexInterface[colSlider].updateCol = false;
                            colSlider.hslColor = SmallUtils.Vector32HSL(colorHexInterface[colSlider].pendingNewHSL);
                            if (colSlider.hueSlider != null)
                            {
                                colSlider.menu.SliderSetValue(colSlider.hueSlider, colSlider.menu.ValueOfSlider(colSlider.hueSlider));
                            }
                            if (colSlider.litSlider != null)
                            {
                                colSlider.menu.SliderSetValue(colSlider.satSlider, colSlider.menu.ValueOfSlider(colSlider.satSlider));
                            }
                            if (colSlider.satSlider != null)
                            {
                                colSlider.menu.SliderSetValue(colSlider.litSlider, colSlider.menu.ValueOfSlider(colSlider.litSlider));
                            }
                            colSlider.HSL2RGB();
                        }
                    }
                }
            }

            public static readonly List<ColorChangeDialog.ColorSlider> colorInterfaces = new();
            private readonly Dictionary<ColorChangeDialog.ColorSlider, HexInterface> colorHexInterface = new();

        }
        public class OpColorPickerHooksOBSOLETE
        {
            public void Init()
            {
                On.Menu.Remix.MixedUI.OpColorPicker.Update += ColorPickerUpdate;
            }

            private void ColorPickerUpdate(On.Menu.Remix.MixedUI.OpColorPicker.orig_Update orig, OpColorPicker self)
            {
                orig(self);
                if (!self.greyedOut && !self.held)
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
                SystemClipboard = cPicker.value;
                cPicker?.PlaySound(SoundID.MENU_Player_Join_Game);
            }
            public void PasteCPicker(OpColorPicker cPicker)
            {
                string pendingVal = new(SystemClipboard.Where(x => x != '#').ToArray());
                if (!SmallUtils.IfHexCodeValid(pendingVal, out Color pasteHex))
                {
                    cPicker?.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
                    return;
                }
                string val = ColorUtility.ToHtmlStringRGB(SmallUtils.Vector32RGB(SmallUtils.RWIIIClamp(SmallUtils.RGB2Vector3(pasteHex), CustomColorModel.RGB, out _)));
                cPicker._lblHex.text = "#" + val;
                cPicker.value = val;
                cPicker.PlaySound(SoundID.MENU_Switch_Page_In);

            }
        }
    }

    public static class MenuInterfacesOBSOLETE
    {
        public class SlugcatDisplay : RectangularMenuObject
        {
            public SlugcatDisplay(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, SlugcatStats.Name current, List<string> bodyCols) : base(menu, owner, pos, size)
            {
                currentSlugcat = current;
                lastSlugcat = current;
                currentBodyNames = PlayerGraphics.ColoredBodyPartList(current);
                currentBodyColors = bodyCols;
                slugcatSprites = new();
                LoadIcon(current, currentBodyNames);

            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                if (slugcatSprites?.Count > 0)
                {
                    foreach (MenuIllustration illu in slugcatSprites)
                    {
                        illu.RemoveSprites();
                        RemoveSubObject(illu);
                    }
                    slugcatSprites = null;
                }
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                if (lastSlugcat != currentSlugcat)
                {
                    lastSlugcat = currentSlugcat;
                    LoadIcon(currentSlugcat, currentBodyNames);
                }
                if (slugcatSprites?.Count > 0 && currentHSLs != null)
                {
                    if (currentHSLs != lastHSLs)
                    {
                        lastHSLs = currentHSLs;
                        for (int i = 0; i < currentHSLs.Count; i++)
                        {
                            if (slugcatSprites.Count - 1 < i)
                            {
                                continue;
                            }
                            slugcatSprites[i].color = HSL2RGB(currentHSLs[i]);
                        }
                    }
                }
            }
            public void LoadIcon(SlugcatStats.Name name, List<string> bodyNames)
            {

                if (slugcatSprites?.Count > 0)
                {
                    foreach (MenuIllustration slugcatSprite in slugcatSprites)
                    {
                        slugcatSprite.RemoveSprites();
                        RemoveSubObject(slugcatSprite);
                    }
                }
                slugcatSprites = new();
                LoadSlugcatImage(name, bodyNames);
                currentHSLs = lastHSLs;
            }
            public void LoadNewHSL(List<string> slugcatColos, SlugcatStats.Name name)
            {
                currentBodyColors = slugcatColos;
                currentSlugcat = name;
                currentBodyNames = PlayerGraphics.ColoredBodyPartList(name);
                List<Vector3> hsls = new();
                for (int i = 0; i < currentBodyNames.Count; i++)
                {
                    hsls.Add(new(1, 1, 1));
                }
                for (int i = 0; i < currentBodyColors.Count; i++)
                {
                    if (currentBodyColors[i].Contains(","))
                    {
                        string[] hslArray = currentBodyColors[i].Split(',');

                        hsls[i] = new Vector3(float.Parse(hslArray[0], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(hslArray[1], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(hslArray[2], NumberStyles.Any, CultureInfo.InvariantCulture));
                    }
                }
                currentHSLs = hsls;
            }
            public void LoadSlugcatImage(SlugcatStats.Name name, List<string> bodyNames)
            {
                try
                {
                    ColorConfigMod.DebugLog("slugcat: " + name.value);
                    Dictionary<string, string> bodyPaths = new();
                    string[] paths = AssetManager.ListDirectory("colorconfig").Where(x => x.EndsWith(".txt")).ToArray();
                    if (paths?.Length > 0)
                    {
                        foreach (string path in paths)
                        {
                            string resolvedPath = AssetManager.ResolveFilePath(path);
                            if (File.Exists(resolvedPath))
                            {
                                ColorConfigMod.DebugLog("Reading " + resolvedPath);
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
                    }
                    for (int i = 0; i < bodyNames.Count; i++)
                    {
                        string folder = "";
                        string file = "";
                        if (!bodyPaths.ContainsKey(bodyNames[i]))
                        {
                            file = i switch
                            {
                                0 => (File.Exists(AssetManager.ResolveFilePath(name.value + "_pup_off.png"))? $"{name.value}_" : "") + "pup_off",
                                1 => "face_" + (File.Exists(AssetManager.ResolveFilePath("face_" + name.value + "_pup_off.png"))? $"{name.value}_" : "") + "pup_off",
                                2 => $"unique_{name.value}_pup_off",
                                _ => $"{bodyNames[i]}_{name.value}_pup_off",
                            };
                        }
                        else
                        {
                            string path = bodyPaths[bodyNames[i]];
                            file = path;
                            if (path.Contains("/"))
                            {
                                file = path.Split('|').Last();
                                folder = path.Replace(file + "/", string.Empty);
                            }
                        }
                        ColorConfigMod.DebugLog($"BodyPart: {bodyNames[i]},Folder: {folder}, File: {file}");
                        MenuIllustration body = new(menu, this, folder, file, size / 2, true, true);
                        subObjects.Add(body);
                        slugcatSprites.Add(body);
                    }

                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("Failed to change new sprites", e);
                }
            }
            public List<Vector3> currentHSLs, lastHSLs;
            public SlugcatStats.Name currentSlugcat, lastSlugcat;
            public List<MenuIllustration> slugcatSprites;
            public List<string> currentBodyNames;
            public List<string> currentBodyColors;
        }
        public class JollyPageSliderInterface : PositionedMenuObject
        {
            public JollyPageSliderInterface(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 sliderSize, int bodyPart) : base(menu, owner, pos)
            {
                sliderSizes = sliderSize;
                sliderIDs ??= new();
                if (sliderPages == null)
                {
                    sliderPages = new(menu, this, Vector2.zero, sliderSize, 5, showAmtAll: false);
                    subObjects.Add(sliderPages);
                }
                if (ModOptions.EnableSliders)
                {
                    if (ModOptions.enableRGBSliders.Value)
                    {
                        if (rgbInterface == null)
                        {
                            Slider.SliderID redID = new($"{bodyPart}_DUSTY_RGB_RED", true);
                            Slider.SliderID greenID = new($"{bodyPart}_DUSTY_RGB_GREEN", true);
                            Slider.SliderID blueID = new($"{bodyPart}_DUSTY_RGB_BLUE", true);
                            sliderIDs.AddRange(new[] { redID, greenID, blueID });
                            rgbInterface = new(menu, sliderPages, Vector2.zero, sliderSize, new[] { redID, greenID, blueID }, new[] { red, green, blue },
                                showAmt: ModOptions.ShowVisual, showInt: rgbShowInt, showMultipler: rgbMultipler);
                            sliderPages.AddSliderInterface(rgbInterface, "", false);
                        }
                    }
                }
                if (ModOptions.enableHSVSliders.Value)
                {
                    if (hsvInterface == null)
                    {
                        Slider.SliderID hsvHue = new($"{bodyPart}_DUSTY_HSV_HUE", true);
                        Slider.SliderID hsvSat = new($"{bodyPart}_DUSTY_HSV_SAT", true);
                        Slider.SliderID hsvLit = new($"{bodyPart}_DUSTY_HSV_VALUE", true);
                        sliderIDs.AddRange(new[] { hsvHue, hsvSat, hsvLit });
                        hsvInterface = new(menu, sliderPages, Vector2.zero, sliderSize, new[] { hsvHue, hsvSat, hsvLit }, new[] { hue, sat, value },
                            showAmt: ModOptions.ShowVisual, showInt: hueOOShowInt, showMultipler: hueOOMultipler);
                        sliderPages.AddSliderInterface(hsvInterface, "", false);
                    }
                }

                sliderAmtLabel = new(menu, this, "", new(sliderSize.x / 3 * 1.7f, 23.5f), new(80, 30), false);
                subObjects.Add(sliderAmtLabel);
            }
            public void AddHSLInterface(HorizontalSlider hue, HorizontalSlider sat, HorizontalSlider lit, int bodyPart, bool isRGB = false)
            {
                if (hue != null && sat != null && lit != null && sliderPages != null && hslInterface == null)
                {
                    if (!isRGB)
                    {

                        hslInterface = new(menu, sliderPages, Vector2.zero, sliderSizes, new[] { hue.ID, sat.ID, lit.ID },
                       new[] { "HUE", "SAT", "LIT" }, showAmt: ModOptions.ShowVisual, showInt: hueOOShowInt,
                       showMultipler: hueOOMultipler);
                    }
                    else
                    {
                        Slider.SliderID redID = new($"{bodyPart}_DUSTY_RGB_RED", true);
                        Slider.SliderID greenID = new($"{bodyPart}_DUSTY_RGB_GREEN", true);
                        Slider.SliderID blueID = new($"{bodyPart}_DUSTY_RGB_BLUE", true);

                        sliderIDs.AddRange(new[] { redID, greenID, blueID });

                        hue.ID = redID;
                        sat.ID = greenID;
                        lit.ID = blueID;

                        hslInterface = new(menu, sliderPages, Vector2.zero, sliderSizes, new[] { hue.ID, sat.ID, lit.ID },
                       new[] { "RED", "GREEN", "BLUE" }, showAmt: ModOptions.ShowVisual, showInt: rgbShowInt,
                       showMultipler: rgbMultipler);
                    }
                    hslInterface.Deactivate();
                    hslInterface.slider1 = hue;
                    hslInterface.slider2 = sat;
                    hslInterface.slider3 = lit;
                    hslInterface.ForceOwnerSlider();
                    sliderPages.AddSliderInterface(hslInterface);
                    if (!ModOptions.EnableJollyPages)
                    {
                        sliderPages.DeactivateSliderInterface();
                        sliderPages.ActivateSliderInterface();
                        sliderPages.DeactivateButtons();
                    }
                }
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                if (sliderAmtLabel != null)
                {
                    sliderAmtLabel.RemoveSprites();
                    RemoveSubObject(sliderAmtLabel);
                    sliderAmtLabel = null;
                }
                if (sliderPages != null)
                {
                    sliderPages.RemoveSprites();
                    RemoveSubObject(sliderPages);
                    sliderPages = null;
                }
                if (sliderIDs != null)
                {
                    sliderIDs.ForEach(x => x.Unregister());
                    sliderIDs = null;
                }
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                if (sliderAmtLabel != null)
                {
                    sliderAmtLabel.text = "";
                    sliderAmtLabel.label.color = colorText;
                    if (ModOptions.ShowVisual)
                    {
                        if (sliderPages?.SliderInterfaces != null)
                        {
                            if (sliderPages.SliderInterfaces.Count > 0)
                            {
                                string[] sliderAmts = sliderPages.SliderInterfaces[sliderPages.currentOffset].SliderStringAmts();

                                sliderAmtLabel.text = $"({sliderAmts[0]}, {sliderAmts[1]}, {sliderAmts[2]})";
                            }
                        }
                    }
                }

            }
            public List<Slider.SliderID> SliderIDs
            { get => sliderIDs; }

            public Color colorText = MenuColorEffect.rgbMediumGrey;
            private Vector2 sliderSizes;
            private MenuLabel sliderAmtLabel;
            private SlidersInterfacePages sliderPages;
            private List<Slider.SliderID> sliderIDs;
            private SliderIIIInterface rgbInterface, hslInterface, hsvInterface;
        }
        public class SlidersInterfacePages : PositionedMenuObject
        {
            public SlidersInterfacePages(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 sliderSizes, float xButtonOffset = 0, float yButtonOffset = 0, bool allSubtle = false, bool sliderVertAlignment = true, bool showAmtAll = true) : base(menu, owner, pos)
            {
                sliderSize = sliderSizes;
                subtle = allSubtle;
                showAmt = showAmtAll;
                currentOffset = 0;
                verticalAlignment = sliderVertAlignment;
                sliderIIIInterfaces = new();
                Vector2 vector = Vector2.zero;
                vector.x += xButtonOffset;
                vector.y += -((verticalAlignment ? sliderSize.y * 2 : sliderSize.y) + 48.5f) + yButtonOffset;
                buttonPos = vector;
                ActivateButtons();
                if (sliderNameLabel == null)
                {
                    sliderNameLabel = new(menu, this, "", new(sliderSize.x / 2, 100), new(80, 50), true);
                    subObjects.Add(sliderNameLabel);
                }
            }
            public void AddSliderInterface(SliderIIIInterface iIIInterface, string name = "", bool changePos = true)
            {
                if (sliderIIIInterfaces != null)
                {
                    iIIInterface.menu = menu;
                    iIIInterface.owner = this;
                    if (changePos)
                    {
                        iIIInterface.pos = Vector2.zero;
                    }
                    iIIInterface.showSliderAmt = showAmt;
                    sliderIIIInterfaces.Add(iIIInterface, name);
                    subObjects.Add(iIIInterface);
                    if (SliderInterfaces[currentOffset] != iIIInterface)
                    {
                        iIIInterface.Deactivate();
                        subObjects.Remove(iIIInterface);
                    }
                }
            }
            public void ActivateSliderInterface()
            {
                if (sliderIIIInterfaces != null)
                {
                    SliderInterfaces[currentOffset].Activate();
                    subObjects.Add(SliderInterfaces[currentOffset]);
                    if (verticalAlignment && nextButton != null)
                    {
                        menu.MutualVerticalButtonBind(nextButton, SliderInterfaces[currentOffset].slider3);
                        updateMutualButton = true;
                    }
                }
            }
            public void DeactivateSliderInterface()
            {
                if (sliderIIIInterfaces != null)
                {
                    SliderInterfaces[currentOffset].Deactivate();
                    RemoveSubObject(SliderInterfaces[currentOffset]);
                }
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
                updateMutualButton = true;
            }
            public void ActivateButtons()
            {
                if (prevButton == null)
                {
                    prevButton = new(menu, this, menu.Translate("Prev"), "_BackPageSliders", buttonPos, new(40, 26), FLabelAlignment.Center, false);
                    subObjects.Add(prevButton);
                    
                }
                if (nextButton == null)
                {
                    nextButton = new(menu, this, menu.Translate("Next"), "_NextPageSliders", new(buttonPos.x + 60, buttonPos.y), new(40, 26), FLabelAlignment.Center, false);
                    subObjects.Add(nextButton);
                }
                menu.MutualHorizontalButtonBind(prevButton, nextButton);
                updateMutualButton = true;
            }
            public void PopulateSliderInterface(int offset)
            {
                if (sliderIIIInterfaces != null)
                {
                    DeactivateSliderInterface();
                    currentOffset = offset;
                    ActivateSliderInterface();
                }
            }
            public void PrevPage()
            {
                int offset = currentOffset - 1;
                if (offset < 0)
                {
                    offset = sliderIIIInterfaces.Count - 1;
                }
                PopulateSliderInterface(offset);
            }
            public void NextPage()
            {
                int offset = currentOffset + 1;
                if (offset >= sliderIIIInterfaces.Count)
                {
                    offset = 0;
                }
                PopulateSliderInterface(offset);
            }
            public override void Update()
            {
                base.Update();
                foreach (SliderIIIInterface sliderIIIInterface in sliderIIIInterfaces.Keys)
                {
                    sliderIIIInterface.showSliderAmt = showAmt;
                }
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                if (sliderNameLabel != null && showSliderName)
                {
                    sliderNameLabel.text = "";
                    if (SliderInterfaces?.Count > 0)
                    {
                        string newText = menu.Translate(sliderIIIInterfaces[SliderInterfaces[currentOffset]]);
                        if (sliderNameLabel.text != newText)
                        {
                            sliderNameLabel.text = newText;
                        }
                    }
                }
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                if (sliderNameLabel != null)
                {
                    sliderNameLabel.RemoveSprites();
                    RemoveSubObject(sliderNameLabel);
                    sliderNameLabel = null;
                }
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
                if (sliderIIIInterfaces != null)
                {
                    SliderInterfaces.ForEach(x => x.RemoveSprites());
                    SliderInterfaces.ForEach(RemoveSubObject);
                    SliderInterfaces.ForEach(x => x = null);
                    sliderIIIInterfaces = null;
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

            public List<SliderIIIInterface> SliderInterfaces
            { get => sliderIIIInterfaces.Keys.ToList(); }

            public int currentOffset;
            public Vector2 sliderSize, buttonPos;
            public bool subtle, verticalAlignment, showAmt, updateMutualButton = false, showSliderName = false;
            public MenuLabel sliderNameLabel;
            public BigSimpleButton prevButton, nextButton;
            private Dictionary<SliderIIIInterface, string> sliderIIIInterfaces;
        }
        public class SliderIIIInterface : PositionedMenuObject
        {
            public SliderIIIInterface(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, Slider.SliderID[] sliderIDs, string[] names, bool verticalAlignment = true, bool allSubtle = false,
                bool showAmt = true, bool[] showInt = default, Vector3 showMultipler = default) : base(menu, owner, pos)
            {
                if (sliderIDs.Length < 3)
                {
                    return;
                }
                vert = verticalAlignment;
                this.sliderIDs = sliderIDs;
                sliderNames = names != null ? names.Length < 3 ? names.Length < 2 ? names.Length < 1 ? new[] { "", "", "" } :
                    new[] { names[0], "", "" } : new[] { names[0], names[1], "" } : names : new[] { "", "", "" };
                this.showMultipler = showMultipler == default ? new(1, 1, 1) : showMultipler;
                showSliderAmt = showAmt;
                this.showInt = showInt;
                subtle = allSubtle;
                sliderSize = size;
                Vector2 divide = verticalAlignment ? new(0, -(size.y + 10)) : new(size.x + 10, 0);
                posX = new(0, divide.x, divide.x * 2);
                posY = new(0, divide.y, divide.y * 2);
                if (slider1 == null)
                {
                    slider1 = new(menu, this, menu.Translate(sliderNames[0]), Vector2.zero, size, sliderIDs[0], allSubtle);
                    subObjects.Add(slider1);
                }
                if (slider2 == null)
                {
                    slider2 = new(menu, this, menu.Translate(sliderNames[1]), Vector2.zero + divide, size, sliderIDs[1], allSubtle);
                    subObjects.Add(slider2);
                }
                if (slider3 == null)
                {
                    slider3 = new(menu, this, menu.Translate(sliderNames[2]), Vector2.zero + (divide * 2), size, sliderIDs[2], allSubtle);
                    subObjects.Add(slider3);
                }
                if (verticalAlignment)
                {
                    menu.MutualVerticalButtonBind(slider3, slider2);
                    menu.MutualVerticalButtonBind(slider2, slider1);
                }
            }
            public bool Slider1ShowInt
            { get => showInt?.Length > 0 && showInt[0]; }
            public bool Slider2ShowInt
            { get => showInt?.Length > 1 && showInt[1]; }
            public bool Slider3ShowInt
            { get => showInt?.Length > 2 && showInt[2]; }
            public bool Deactivated
            { get; private set; } = false;
            public Slider.SliderID[] SliderIDs
            { get => sliderIDs; }
            public string[] SliderStringAmts(bool showSign = true)
            {
                string slider1Amt = "";
                string slider2Amt = "";
                string slider3Amt = "";
                if (slider1 != null)
                {
                    float value = slider1.floatValue * showMultipler.x;
                    double amt = Slider1ShowInt ? Math.Round(value, 0, MidpointRounding.AwayFromZero) : Math.Round(value, ModOptions.Digit, MidpointRounding.AwayFromZero);
                    string sign = showSign ? showMultipler.x == 100 ? "%" : showMultipler.x == 360 && Slider1ShowInt ? degreeSign : "" : "";
                    slider1Amt = $"{amt}{sign}";
                }
                if (slider2 != null)
                {
                    float value = slider2.floatValue * showMultipler.y;
                    double amt = Slider2ShowInt ? Math.Round(value, 0, MidpointRounding.AwayFromZero) : Math.Round(value, ModOptions.Digit, MidpointRounding.AwayFromZero);
                    string sign = showSign ? showMultipler.y == 100 ? "%" : showMultipler.y == 360 && Slider2ShowInt ? degreeSign : "" : "";
                    slider2Amt = $"{amt}{sign}";
                }
                if (slider3 != null)
                {
                    float value = slider3.floatValue * showMultipler.z;
                    double amt = Slider3ShowInt ? Math.Round(value, 0, MidpointRounding.AwayFromZero) : Math.Round(value, ModOptions.Digit, MidpointRounding.AwayFromZero);
                    string sign = showSign ? showMultipler.z == 100 ? "%" : showMultipler.y == 360 && Slider3ShowInt ? degreeSign : "" : "";
                    slider3Amt = $"{amt}{sign}";
                }

                return new[] { slider1Amt, slider2Amt, slider3Amt };

            }
            public void Deactivate()
            {
                Deactivated = true;
                if (slider1 != null)
                {
                    slider1.RemoveSprites();
                    RemoveSubObject(slider1);
                    slider1 = null;
                }
                if (slider2 != null)
                {
                    slider2.RemoveSprites();
                    RemoveSubObject(slider2);
                    slider2 = null;
                }
                if (slider3 != null)
                {
                    slider3.RemoveSprites();
                    RemoveSubObject(slider3);
                    slider3 = null;
                }
            }
            public void Activate()
            {
                Deactivated = false;
                if (slider1 == null)
                {
                    slider1 = new(menu, this, menu.Translate(sliderNames[0]), new(posX.x, posY.x), sliderSize, sliderIDs[0], subtle);
                    subObjects.Add(slider1);
                }
                if (slider2 == null)
                {
                    slider2 = new(menu, this, menu.Translate(sliderNames[1]), new(posX.y, posY.y), sliderSize, sliderIDs[1], subtle);
                    subObjects.Add(slider2);
                }
                if (slider3 == null)
                {
                    slider3 = new(menu, this, menu.Translate(sliderNames[2]), new(posX.z, posY.z), sliderSize, sliderIDs[2], subtle);
                    subObjects.Add(slider3);
                }
                if (vert)
                {
                    menu.MutualVerticalButtonBind(slider3, slider2);
                    menu.MutualVerticalButtonBind(slider2, slider1);
                }
            }
            public void ForceOwnerSlider()
            {
                if (slider1 != null && !subObjects.Contains(slider1))
                {
                    slider1.menu = menu;
                    slider1.owner = this;
                    subObjects.Add(slider1);
                }
                if (slider2 != null && !subObjects.Contains(slider2))
                {
                    slider2.menu = owner.menu;
                    slider2.owner = this;
                    subObjects.Add(slider2);
                }
                if (slider3 != null && !subObjects.Contains(slider3))
                {
                    slider3.menu = owner.menu;
                    slider3.owner = this;
                    subObjects.Add(slider3);
                }
                menu.MutualVerticalButtonBind(slider3, slider2);
                menu.MutualVerticalButtonBind(slider2, slider1);
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                Deactivate();
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                if (showSliderAmt && !Deactivated)
                {
                    if (slider1 != null)
                    {
                        if (slider1.menuLabel != null)
                        {
                            slider1.menuLabel.text = menu.Translate(sliderNames[0]) + " " + SliderStringAmts(showSigns)[0];
                        }
                    }
                    if (slider2 != null)
                    {
                        if (slider2.menuLabel != null)
                        {
                            slider2.menuLabel.text = menu.Translate(sliderNames[1]) + " " + SliderStringAmts(showSigns)[1];
                        }
                    }
                    if (slider3 != null)
                    {
                        if (slider3.menuLabel != null)
                        {
                            slider3.menuLabel.text = menu.Translate(sliderNames[2]) + " " + SliderStringAmts(showSigns)[2];
                        }
                    }

                }
            }

            public string[] sliderNames;
            public bool subtle, showSliderAmt, showSigns = true, copying = false, pasting = false;
            private bool vert;
            public bool[] showInt;
            public Vector2 sliderSize;
            public Vector3 posX, posY;
            public Vector3 showMultipler;
            private Slider.SliderID[] sliderIDs;
            public HorizontalSlider slider1, slider2, slider3;
        }
        public class HexInterface : PositionedMenuObject, ICopyPasteConfig
        {
            public HexInterface(Menu.Menu menu, MenuObject owner, Vector2 pos, bool showLabel = true) : base(menu, owner, pos)
            {
                Vector2 vector = Vector2.zero;
                if (tabWrapper == null)
                {
                    tabWrapper = new(menu, this);
                    subObjects.Add(tabWrapper);
                }
                if (showLabel && label == null)
                {
                    label = new(new(vector.x, vector.y - 0.6f), new(30, 30), menu.Translate("HEX"), FLabelAlignment.Left);
                    vector[0] += 34;

                }
                if (showLabel && labelWrapper == null)
                {
                    labelWrapper = new(tabWrapper, label);
                    subObjects.Add(labelWrapper);
                }
                if (hexTyper == null)
                {
                    Configurable<string> hexConfig = new("");
                    hexTyper = new(hexConfig, vector, 60)
                    {
                        maxLength = 6,
                    };
                }
                if (typerWrapper == null)
                {
                    typerWrapper = new(tabWrapper, hexTyper);
                    subObjects.Add(typerWrapper);
                }
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                if (typerWrapper != null)
                {
                    typerWrapper.RemoveSprites();
                    RemoveSubObject(typerWrapper);
                    typerWrapper = null;
                }
                if (hexTyper != null)
                {
                    hexTyper.label.alpha = 0;
                    hexTyper.label.RemoveFromContainer();
                    hexTyper.rect.Hide();
                    hexTyper = null;
                }
                if (labelWrapper != null)
                {
                    labelWrapper.RemoveSprites();
                    RemoveSubObject(labelWrapper);
                    labelWrapper = null;
                }
                if (label != null)
                {
                    label.label.RemoveFromContainer();
                    label.Hide();
                    label = null;
                }
                if (tabWrapper != null)
                {
                    RemoveSubObject(tabWrapper);
                    tabWrapper.RemoveSprites();
                    tabWrapper = null;
                }
            }
            public override void Update()
            {
                base.Update();
                if (hexTyper != null)
                {
                    if (!hasSet || currentHSLColor != prevHSLColor || forceUpdate)
                    {
                        hasSet = true;
                        forceUpdate = false;
                        prevHSLColor = currentHSLColor;
                        hexTyper.value = HSL2Hex(currentHSLColor);
                        lastValue = hexTyper.value;
                    }
                    if (!hexTyper.held)
                    {
                        if (lastValue != hexTyper.value)
                        {
                            if (!SmallUtils.IfHexCodeValid(hexTyper.value, out Color hexCol))
                            {
                                Debug.LogError($"Failed to parse from new value \"{hexTyper.value}\"");
                                hexTyper.value = lastValue;
                                return;
                            }
                            Vector3 hslFromHex = SmallUtils.HOOClamp(Custom.RGB2HSL(hexCol), new(0, 0, 0.01f), new(1, 1, 1));
                            hexTyper.value = HSL2Hex(hslFromHex);
                            if (hslFromHex != currentHSLColor)
                            {
                                pendingNewHSL = SmallUtils.FixHexSliderWonkiness(hslFromHex, currentHSLColor);
                                updateCol = true;
                                currentHSLColor = hslFromHex;
                                prevHSLColor = hslFromHex;
                            }
                            lastValue = hexTyper.value;
                        }
                    }
                    if (hexTyper.MouseOver)
                    {
                        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C) && !copying && !pasting)
                        {
                            copying = true;
                            Copy();
                            copying = false;
                        }
                        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.V) && !pasting && !copying)
                        {
                            pasting = true;
                            Paste();
                            pasting = false;
                        }
                    }
                }
            }
            public void Copy()
            {
                SystemClipboard = hexTyper.value;
                menu.PlaySound(SoundID.MENU_Player_Join_Game);
            }
            public void Paste()
            {
                string pendingVal = new(SystemClipboard.Where(x => x != '#').ToArray());
                if (!SmallUtils.IfHexCodeValid(pendingVal, out Color fromPaste))
                {
                    menu.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
                    return;
                }
                Vector3 hslFromPaste = SmallUtils.HOOClamp(Custom.RGB2HSL(fromPaste), new(0, 0, 0.01f), new(1, 1, 1));
                hexTyper.value = HSL2Hex(hslFromPaste);
                if (hslFromPaste != currentHSLColor)
                {
                    pendingNewHSL = SmallUtils.FixHexSliderWonkiness(hslFromPaste, currentHSLColor);
                    updateCol = true;
                    currentHSLColor = hslFromPaste;
                    prevHSLColor = hslFromPaste;
                }
                lastValue = hexTyper.value;
                menu.PlaySound(SoundID.MENU_Switch_Page_In);

            }
            public void SetNewHSLColor(Vector3 hsl)
            {
                currentHSLColor = hsl;
            }

            public string lastValue;
            public bool hasSet = false, forceUpdate = false, updateCol = false, copying = false, pasting = false;
            public Vector3 prevHSLColor, currentHSLColor, pendingNewHSL;
            public OpLabel label;
            public OpTextBox hexTyper;
            public UIelementWrapper typerWrapper, labelWrapper;
            public MenuTabWrapper tabWrapper;
        }
        public interface ICopyPasteConfig
        {
            void Copy();
            void Paste();
        }
    }

}
