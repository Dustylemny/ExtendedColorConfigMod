using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using ColorConfig.MenuUI;
using ColorConfig.WeakUITable;

namespace ColorConfig.Hooks
{
    public static partial class ColorConfigHooks
    {
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
                    extraInterfaces.hexInterface = new(ssM, ssM.pages[0], ssM.defaultColorButton.pos + new Vector2(ssM.defaultColorButton.size.x + 10, 0));
                    extraInterfaces.hexInterface.OnSaveNewTypedColor += (hexTyper, hsl, rgb) => { ssM.SaveHSLString_Story(hsl); ssM.hueSlider.UpdateSliderValue(); ssM.satSlider.UpdateSliderValue(); ssM.litSlider.UpdateSliderValue(); };
                    ssM.pages[0].subObjects.Add(extraInterfaces.hexInterface);
                    extraInterfaces.hexInterface.elementWrapper.MenuObjectBind(extraInterfaces.sliderPages?.PagesOn == true ? extraInterfaces.sliderPages.nextButton : ssM.litSlider, top: true);
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

    }
}
