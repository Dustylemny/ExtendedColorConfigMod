using System;
using System.Linq;
using Menu;
using RWCustom;
using MoreSlugcats;
using UnityEngine;
using static ColorConfig.MenuToolObj;
using static ColorConfig.MenuInterfaces;

namespace ColorConfig
{
    public class SlugcatSelectScreenHooks
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
            UpdateStuffWithNewHSL(self);
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
                        self.menuLabel.text = self.menu.Translate("HUE") + $" {Math.Round(self.floatValue * 360, 0 , MidpointRounding.AwayFromZero)}" + degreeSign;
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
                    ssM.MutualVerticalButtonBind(ssM.defaultColorButton, sliderPages);
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
        private void UpdateStuffWithNewHSL(SlugcatSelectMenu ssM)
        {       
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


}
