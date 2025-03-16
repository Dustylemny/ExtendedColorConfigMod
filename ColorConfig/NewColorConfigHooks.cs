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
using static ColorConfig.MenuInterfaces;
using Menu.Remix.MixedUI.ValueTypes;
using System.Globalization;
namespace ColorConfig
{
    public static class ColorConfigHooks
    {
        public static void Init()
        {
            MenuHooks menuHooks = new();
            SlugcatSelectMenuHooks ssmHooks = new();
            ExpeditionMenuHooks expHooks = new();
            JollyConfigHooks jollyConfigHooks = new();
            OpConfigHooks opColorPickerHooks = new();
            menuHooks.Init();
            ssmHooks.Init();
            expHooks.Init();
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
                UpdateExtraInterfaces(self);

            }
            public void On_UiElement_Update(On.Menu.Remix.MixedUI.UIelement.orig_Update orig, UIelement self)
            {
                orig(self);
                UpdateExtraInterfaces(self);
            }
            public static void UpdateExtraInterfaces(object obj)
            {
                GetOwnInput(obj as IGetOwnInput);
                CopyPaste(obj as ICopyPasteConfig);
            }
            public static void GetOwnInput(IGetOwnInput non_MouseInput)
            {
                if (non_MouseInput != null)
                {
                    non_MouseInput.LastInput = non_MouseInput.Input;
                    non_MouseInput.Input = SmallUtils.FixedPlayerUIInput(-1);
                    non_MouseInput.TryGetInput();
                }
            }
            public static void CopyPaste(ICopyPasteConfig copyPasteConfig)
            {
                if (copyPasteConfig != null && copyPasteConfig.ShouldCopyPaste)
                {
                    if (SmallUtils.CopyShortcutPressed())
                    {
                        copyPasteConfig.Clipboard = copyPasteConfig.Copy();
                    }
                    if (SmallUtils.PasteShortcutPressed())
                    {
                        copyPasteConfig.Paste(copyPasteConfig.Clipboard);
                    }
                }
            }
        }
        public class SlugcatSelectMenuHooks
        {
            public void Init()
            {
                try
                {
                    //IL.Menu.SlugcatSelectMenu.SliderSetValue += IL_SlugcatSelectMenu_SliderSetValue;
                    //On.Menu.SlugcatSelectMenu.StartGame += On_SlugcatSelectMenu_StartGame;
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
            /*public void IL_SlugcatSelectMenu_SliderSetValue(ILContext il)
            {
                try
                {
                    ILCursor cursor = new(il);
                    cursor.GotoNext(MoveType.After, x => x.MatchLdcR4(0.99f));
                    cursor.EmitDelegate(delegate (float maxClamp)
                    {
                        return ModOptions.DisableHueSliderMaxClamp.Value && (!ColorConfigMod.IsRainMeadowOn || !RainMeadowHooks.IsInRainMeadowStoryLobby())? 1 : maxClamp;
                    });
                    cursor.GotoNext(x => x.MatchCallvirt<MenuIllustration>("set_color"));
                    cursor.Emit(OpCodes.Ldloc_2);
                    cursor.EmitDelegate(delegate (Color col, Vector3 hsl)
                    {
                        return hsl.x == 1 ? ColConversions.HSL2RGB(hsl) : col;
                    });
                }
                catch (Exception ex)
                {
                    ColorConfigMod.DebugException("Failed to patch desired code", ex);
                }

            }
            public void On_SlugcatSelectMenu_StartGame(On.Menu.SlugcatSelectMenu.orig_StartGame orig, SlugcatSelectMenu self, SlugcatStats.Name name)
            {
                self.TryFixAllColorChoices();
                orig(self, name);
            }*/
            public void On_SlugcatSelectMenu_AddColorButtons(On.Menu.SlugcatSelectMenu.orig_AddColorButtons orig, SlugcatSelectMenu self)
            {
                orig(self);
                if (ModOptions.EnableSlugcatDisplay.Value && ModOptions.EnableLegacyIdeaSlugcatDisplay.Value && self.GetExtraSSMInterface().slugcatDisplay == null)
                {
                    Vector2 vector = self.manager.BaseScreenColorInterfacePos();
                    vector.y -= (ModManager.JollyCoop ? 40 : 0) + (self.colorInterface != null ? self.colorInterface.bodyColors.Length * 40 : 0);
                    self.GetExtraSSMInterface().slugcatDisplay = new(self, self.pages[0], new(vector.x + 140, vector.y + 40), new(45f, 45f), self.StorySlugcat());
                    self.pages[0].subObjects.Add(self.GetExtraSSMInterface().slugcatDisplay);
                }
            }
            public void On_SlugcatSelectMenu_RemoveColorButtons(On.Menu.SlugcatSelectMenu.orig_RemoveColorButtons orig, SlugcatSelectMenu self)
            {
                //self.TryFixColorChoices(self.StorySlugcat());
                orig(self);
                self.pages[0].ClearMenuObject(ref self.GetExtraSSMInterface().slugcatDisplay);
            }
            public void On_SlugcatSelectMenu_AddColorInterface(On.Menu.SlugcatSelectMenu.orig_AddColorInterface orig, SlugcatSelectMenu self)
            {
                orig(self);
                AddInterfaces(self, self.GetExtraSSMInterface(),self.StorySlugcat());
            }
            public void On_SlugcatSelectMenu_RemoveColorInterface(On.Menu.SlugcatSelectMenu.orig_RemoveColorInterface orig, SlugcatSelectMenu self)
            {
                orig(self);
                //self.TryFixColorChoices(self.StorySlugcat());
                RemoveConfigInterface(self, self.GetExtraSSMInterface());
            }
            public void On_SlugcatSelectMenu_Update(On.Menu.SlugcatSelectMenu.orig_Update orig, SlugcatSelectMenu self)
            {
                orig(self);
                UpdateMenuInterfaces(self, self.GetExtraSSMInterface());
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

            //functions
            public static void CustomSlidersSetValue(SlugcatSelectMenu ssM, Slider slider, float f)
            {
                MenuToolObj.CustomSliderSetHSL(slider, f, ssM.SlugcatSelectMenuHSL(), ssM.SaveHSLString_Story);
            }
            public static bool ValueOfCustomSliders(SlugcatSelectMenu ssM, Slider slider, out float f)
            {
                return MenuToolObj.CustomHSLValueOfSlider(slider, ssM.SlugcatSelectMenuHSL(), out f);
            }

            //extraInterfaces
            public static void UpdateMenuInterfaces(SlugcatSelectMenu ssM, ExtraSSMInterfaces extraInterfaces)
            {
                extraInterfaces.hexInterface?.SaveNewHSL(ssM.SlugcatSelectMenuHSL());
                extraInterfaces.legacyHexInterface?.SaveNewHSLs(ssM.SlugcatSelectMenuHSLs());
                extraInterfaces.slugcatDisplay?.LoadNewHSLStringSlugcat(ssM.manager.rainWorld.progression.miscProgressionData.colorChoices[ssM.StorySlugcat().value]/*, ssM.slugcatColorOrder[ssM.slugcatPageIndex]*/);
            }
            public static void AddInterfaces(SlugcatSelectMenu ssM, ExtraSSMInterfaces extraSSMInterfaces, SlugcatStats.Name name)
            {
                AddSliderInterface(ssM, extraSSMInterfaces, SSMSliderIDGroups);
                AddOtherInterface(ssM, extraSSMInterfaces, name);
            }
            public static void AddSliderInterface(SlugcatSelectMenu ssM, ExtraSSMInterfaces extraInterfaces, List<MenuInterfaces.SliderIDGroup> sliderOOOIDGroups)
            {
                if (ModOptions.ShouldAddSSMLegacySliders && extraInterfaces.legacySliders == null)
                {
                    extraInterfaces.legacySliders = new(ssM, ssM.pages[0], ssM.defaultColorButton.pos + new Vector2(0, -40), new(0, -40), new(200, 40), [.. sliderOOOIDGroups.Exclude(0)], showValue: ModOptions.ShowVisual, rounding: ModOptions.SliderRounding.Value, dec: ModOptions.DeCount);
                    ssM.pages[0].subObjects.Add(extraInterfaces.legacySliders);
                    ssM.MutualVerticalButtonBind(extraInterfaces.legacySliders.sliderO, ssM.defaultColorButton);
                    ssM.MutualVerticalButtonBind(ssM.nextButton, extraInterfaces.legacySliders.oOOPages.PagesOn ? extraInterfaces.legacySliders.oOOPages.prevButton : extraInterfaces.legacySliders.sliderOOO);
                }
                if (extraInterfaces.sliderPages == null)
                {
                    extraInterfaces.sliderPages = new(ssM, ssM.pages[0], [ssM.hueSlider, ssM.satSlider, ssM.litSlider], extraInterfaces.legacySliders != null ? [.. sliderOOOIDGroups[0].ToSingleList()] : sliderOOOIDGroups, new(0, 25))
                    {
                        showValues = ModOptions.ShowVisual,
                        roundingType = ModOptions.SliderRounding.Value,
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
            public static void AddOtherInterface(SlugcatSelectMenu ssM, ExtraSSMInterfaces extraInterfaces, SlugcatStats.Name name)
            {
                if (ModOptions.EnableSlugcatDisplay.Value && extraInterfaces.slugcatDisplay == null)
                {
                    extraInterfaces.slugcatDisplay = new(ssM, ssM.pages[0], new(ssM.satSlider.pos.x + 140, ssM.satSlider.pos.y + 80), new(45f, 45f),
                        name);
                    ssM.pages[0].subObjects.Add(extraInterfaces.slugcatDisplay);

                }
                if (ModOptions.EnableHexCodeTypers.Value)
                {
                    if (ModOptions.EnableLegacyHexCodeTypers.Value && extraInterfaces.legacyHexInterface == null)
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
                            saveNewTypedColor = (hexTyper, hsl, rgb) => { ssM.SaveHSLString_Story(hsl); ssM.hueSlider.UpdateSliderValue(); ssM.satSlider.UpdateSliderValue(); ssM.litSlider.UpdateSliderValue();}
                        };
                        ssM.pages[0].subObjects.Add(extraInterfaces.hexInterface);
                        extraInterfaces.hexInterface.elementWrapper.MenuObjectBind(extraInterfaces.sliderPages.PagesOn ? extraInterfaces.sliderPages.nextButton : ssM.litSlider, top: true);
                        extraInterfaces.hexInterface.elementWrapper.MenuObjectBind(extraInterfaces.legacySliders != null ? extraInterfaces.legacySliders.sliderO : ssM.nextButton, bottom: true);
                    }
                }
            }
            public static void RemoveConfigInterface(SlugcatSelectMenu ssM, ExtraSSMInterfaces extraInterfaces)
            {
                ssM.pages[0].ClearMenuObject(ref extraInterfaces.legacySliders);
                ssM.pages[0].ClearMenuObject(ref extraInterfaces.sliderPages);
                if (!ModOptions.EnableLegacyIdeaSlugcatDisplay.Value)
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
                    List<MenuInterfaces.SliderIDGroup> result = [];
                    SmallUtils.AddSSMSliderIDGroups(result, ModOptions.ShouldRemoveHSLSliders);
                    return result;
                }
            }
            public class ExtraSSMInterfaces
            {
                public HexTypeBox hexInterface;
                public SlugcatDisplay slugcatDisplay;
                public SliderPages sliderPages;
                //Legacy versions stuff
                public OOOSliders legacySliders;
                public HexTypeBoxPages legacyHexInterface;
            }
        }
        public class JollyConfigHooks
        {
            public void Init()
            {
                try
                {
                    On.JollyCoop.JollyMenu.ColorChangeDialog.ValueOfSlider += On_Dialog_ValueOfSlider;
                    On.JollyCoop.JollyMenu.ColorChangeDialog.SliderSetValue += On_Dialog_SliderSetValue;
                    On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.ctor += On_Color_Slider_ctor;
                    On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.RemoveSprites += On_ColorSlider_RemoveSprites;
                    ColorConfigMod.DebugLog("Sucessfully extended color interface for jolly coop menu!");
                    if (ColorConfigMod.IsLukkyRGBColorSliderModOn)
                    {
                        LukkyRGBModHooks.ApplyLukkyModHooks();
                    }
                }
                catch (Exception ex)
                {
                    ColorConfigMod.DebugException("Failed to initialize jolly-coop menu hooks", ex);
                }
            }
            //BodyColorSlider
            public void On_Color_Slider_ctor(On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_ctor orig, ColorChangeDialog.ColorSlider self, Menu.Menu menu, MenuObject owner, Vector2 pos, int playerNumber, int bodyPart, string sliderTitle)
            {
                orig(self, menu, owner, pos, playerNumber, bodyPart, sliderTitle);
                self.GetExtraJollyInterface(/*bodyPart,*/ ModOptions.ShouldRemoveHSLSliders || ModOptions.FollowLukkyRGBSliders, ModOptions.EnableHexCodeTypers.Value);
            }
            public void On_ColorSlider_RemoveSprites(On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_RemoveSprites orig, ColorChangeDialog.ColorSlider self)
            {
                orig(self);
                self.RemoveExtraJollyInterface();
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
                if (slider?.ID != null && slider.owner is ColorChangeDialog.ColorSlider colSlider)
                {
                    if (MenuToolObj.RGBSliderIDS.Contains(slider.ID))
                    {
                        Color color = colSlider.color;
                        color[MenuToolObj.RGBSliderIDS.FindIndex(slider.ID)] = f;
                        colSlider.color = SmallUtils.RWIIIClamp(color.RGB2Vector3(), CustomColorModel.RGB, out Vector3 newHSL).Vector32RGB();
                        colSlider.hslColor = SmallUtils.FixNonHueSliderWonkiness(newHSL, colSlider.hslColor.HSL2Vector3()).Vector32HSL();
                    }
                    if (MenuToolObj.HSVSliderIDS.Contains(slider.ID))
                    {
                        Vector3 hsv = ColConversions.HSL2HSV(colSlider.hslColor.HSL2Vector3());
                        hsv[MenuToolObj.HSVSliderIDS.FindIndex(slider.ID)] = f;
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
            public bool ValueOfCustomSliders(Slider slider, out float f)
            {
                f = -1;
                if (slider?.ID != null && slider.owner is ColorChangeDialog.ColorSlider colSlider)
                {
                    if (MenuToolObj.RGBSliderIDS.Contains(slider.ID))
                    {
                        f = colSlider.color[MenuToolObj.RGBSliderIDS.FindIndex(slider.ID)];
                    }
                    if (MenuToolObj.HSVSliderIDS.Contains(slider.ID))
                    {
                        f = ColConversions.HSL2HSV(colSlider.hslColor.HSL2Vector3())[MenuToolObj.HSVSliderIDS.FindIndex(slider.ID)];
                    }
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

            public static class LukkyRGBModHooks
            {
                public static void ApplyLukkyModHooks()
                {
                    ColorConfigMod.DebugLog("Initialising Lukky RGB Slider Hooks");
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
            }
        }
        public class OpConfigHooks
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
                    On.Menu.Remix.MixedUI.OpColorPicker.ctor += On_OPColorPicker_Ctor;
                    On.Menu.Remix.MixedUI.OpColorPicker._RecalculateTexture += On_OPColorPickerRecalculateTexture;
                    On.Menu.Remix.MixedUI.OpColorPicker.Update += On_OPColorPicker_Update;
                    On.Menu.Remix.MixedUI.OpColorPicker.GrafUpdate += On_OPColorPicker_GrafUpdate;
                    ColorConfigMod.DebugLog("Successfully extended color interface for OpColorPicker!");
                }
                catch(Exception ex)
                {
                    ColorConfigMod.DebugException("Failed to initialize colorpicker hooks", ex);
                }
            }
            //op-colorpicker oghooks
            public void OtherOpColorPickerHooks()
            {
                typeof(OpColorPicker).GetProperty(nameof(OpColorPicker.@value)).GetSetMethod().ILHookMethod(IL_OPColorPicker_set_value);
                typeof(OpColorPicker).GetProperty(nameof(OpColorPicker.@value)).GetSetMethod().HookMethod(delegate(Action<OpColorPicker, string> orig, OpColorPicker self, string newValue)
                {
                    if (self.value == newValue && (ModOptions.EnableBetterOPColorPicker.Value || ModOptions.EnableDiffOpColorPickerHSL.Value) && self._mode == OpColorPicker.PickerMode.HSL)
                    {
                        self._RecalculateTexture();
                        self._ftxr1.SetTexture(self._ttre1);
                        self._ftxr2.SetTexture(self._ttre2);
                    }
                    orig(self, newValue);
                });
            }
            public void IL_OPColorPicker_set_value(ILContext il)
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
                        if (ModOptions.EnableBetterOPColorPicker.Value)
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
            public void IL_OPColorPicker_Change(ILContext il)
            {
                try
                {
                    ILCursor cursor = new(il);
                    cursor.GotoNext(MoveType.After, (x) => x.MatchCall(typeof(Custom), "HSL2RGB"));
                    ColorConfigMod.DebugILCursor(cursor);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(delegate(Color hslRGB, OpColorPicker self)
                    {
                        return ModOptions.HSL2HSVOPColorPicker.Value ? ColConversions.HSV2RGB(self.GetHSL01()) : self._h == 100? ColConversions.HSL2RGB(self.GetHSL01()): hslRGB;
                    });
                    ColorConfigMod.DebugLog("Sucessfully patched _cdis0 Color for HSV and rect hue selector!");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("Failed to patch _cdis0 Color for HSV and rect hue selector", e);
                }
            }
            public void IL_OPColorPicker__HSLSetValue(ILContext il)
            {
                try
                {
                    ILCursor cursor = new(il);
                    cursor.GotoNext(MoveType.After, x => x.MatchCall(typeof(Custom), "HSL2RGB"));
                    ColorConfigMod.DebugILCursor(cursor);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(delegate (Color hslCol, OpColorPicker self)
                    {
                        return ModOptions.PickerHSVMode? ColConversions.HSV2RGB(self.GetHSL01()) : ModOptions.EnableBetterOPColorPicker.Value && self._h == 100 ? ColConversions.HSL2RGB(self.GetHSL01()) : hslCol;
                    });
                    ColorConfigMod.DebugLog("Sucessfully patched RGB Color to take hsv or not turn grey!");
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("Failed to patch RGB Color ", e);
                }
            }
            public void IL_OPColorPicker_MouseModeUpdate(ILContext il)
            {
                try 
                {
                    ILCursor cursor = new(il);
                    GotoHSL(cursor);
                    ColorConfigMod.DebugILCursor(cursor);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldloc_3);
                    cursor.Emit(OpCodes.Ldloc, 4);
                    cursor.EmitDelegate(delegate(OpColorPicker self, int hueSat, int satLit)
                    {
                        if (ModOptions.EnableDiffOpColorPickerHSL.Value)
                        {
                            hueSat = Mathf.RoundToInt(Mathf.Clamp(self.MousePos.x - 10f, 0f, 100f));
                            self._lblR.text = self.GetHSVOrHSL100().x.ToString();
                            self._lblG.text = hueSat.ToString();
                            self._lblB.text = satLit.ToString();
                            self._cdis1.color = ColorPicker2RGB.Invoke(self.ParseGetHSLORHSV01(null, hueSat / 100f, satLit / 100f));
                            self.SetHSLORHSV100(null, hueSat, satLit);

                            return true;
                        }
                        else if (ModOptions.HSL2HSVOPColorPicker.Value)
                        {
                            self._cdis1.color = ColorPicker2RGB.Invoke(self.ParseGetHSLORHSV01(hueSat / 100f, satLit / 100f, null));
                        }
                        return false;
                    });
                    MakeAReturn(cursor);
                    ColorConfigMod.DebugLog("Sucessfully patched MiniFocus SatHue!");
                    GotoHSL(cursor);
                    ColorConfigMod.DebugILCursor(cursor);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldloc, 5);
                    cursor.EmitDelegate(delegate(OpColorPicker self, int litHue)
                    {
                        if (ModOptions.EnableDiffOpColorPickerHSL.Value)
                        {
                            litHue = litHue > 99 ? 99 : litHue;
                            self._lblR.text = litHue.ToString();
                            self._lblG.text = self.GetHSVOrHSL100().y.ToString();
                            self._lblB.text = self.GetHSVOrHSL100().z.ToString();
                            self._cdis1.color = ColorPicker2RGB.Invoke(self.ParseGetHSLORHSV01(litHue / 100f, null, null));
                            self.SetHSLORHSV100(litHue, null, null);
                            return true;
                        }
                        else if (ModOptions.HSL2HSVOPColorPicker.Value)
                        {
                            self._cdis1.color = ColorPicker2RGB.Invoke(self.ParseGetHSLORHSV01(null, null, litHue / 100f));
                        }
                        return false;

                    });
                    ColorConfigMod.DebugLog("Sucessfully patched MiniFocus for Lit!");
                    MakeAReturn(cursor);
                    GotoHSL(cursor);//, false);
                    ColorConfigMod.DebugILCursor(cursor);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldloc, 11);
                    cursor.EmitDelegate(delegate(OpColorPicker self, int litHue)
                    {
                        if (ModOptions.EnableDiffOpColorPickerHSL.Value)
                        {
                            litHue = litHue > 99 ? 99 : litHue;
                            self._lblR.text = litHue.ToString();
                            self._lblB.text = self.GetHSVOrHSL100().z.ToString();
                            self._cdis1.color = ColorPicker2RGB.Invoke(self.ParseGetHSLORHSV01(litHue / 100f, null, null));
                        }
                        else if (ModOptions.HSL2HSVOPColorPicker.Value)
                        {
                            self._cdis1.color = ColorPicker2RGB.Invoke(self.ParseGetHSLORHSV01(null, null, litHue / 100f));
                        }

                    });
                    ColorConfigMod.DebugLog("Sucessfully patched HoverMouse for Hue!");
                    cursor.GotoNext(x => x.MatchCall(typeof(Custom), "HSL2RGB"));
                    cursor.GotoNext(MoveType.After, x => x.MatchCallvirt<FSprite>("set_color"));
                    ColorConfigMod.DebugILCursor(cursor);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldloc, 12);
                    cursor.Emit(OpCodes.Ldloc, 13);
                    cursor.EmitDelegate(delegate(OpColorPicker self, int hueSat, int satLit)
                    {
                        if (ModOptions.EnableDiffOpColorPickerHSL.Value)
                        {
                            hueSat = Mathf.RoundToInt(Mathf.Clamp(self.MousePos.x - 10f, 0f, 100f));
                            self._lblR.text = self.GetHSVOrHSL100().x.ToString();
                            self._lblG.text = hueSat.ToString();
                            self._lblB.text = satLit.ToString();
                            self._cdis1.color = ColorPicker2RGB.Invoke(self.ParseGetHSLORHSV01(null, hueSat/ 100f, satLit/ 100f));
                        }
                        else if (ModOptions.HSL2HSVOPColorPicker.Value)
                        {
                            self._cdis1.color = ColorPicker2RGB.Invoke(self.ParseGetHSLORHSV01(hueSat / 100f, satLit / 100f, null));
                        }
                    });
                    ColorConfigMod.DebugLog("Sucessfully patched HoverMouse for Sat and Lit!");
                }
                catch (Exception ex)
                {
                    ColorConfigMod.DebugException("Failed to patch for changing colors using mouse", ex);
                }
              
            }
            public void IL_OPColorPicker_GrafUpdate(ILContext il)
            {
                try
                {
                    ILCursor cursor = new(il);
                    cursor.GotoNext(x => x.Match(OpCodes.Sub), (x) => x.Match(OpCodes.Switch));
                    cursor.GotoNext(x => x.MatchCall<UIelement>("get_MenuMouseMode"));
                    cursor.GotoNext(MoveType.After, x => x.MatchCallvirt<FLabel>("set_color"));
                    ColorConfigMod.DebugILCursor(cursor);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(delegate(OpColorPicker self)
                    {
                        //hueSatToSatHueTextFix
                        if (ModOptions.EnableDiffOpColorPickerHSL.Value)
                        {
                            self._lblB.color = self._lblR.color;
                            self._lblR.color = self.colorText;
                        }
                    });
                    ColorConfigMod.DebugLog("Successfully patched hue2lit text color");
                    cursor.GotoNext(x => x.MatchCallvirt<GlowGradient>("set_pos"));
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(delegate(Vector2 origFocus, OpColorPicker self)
                    {
                        return ModOptions.EnableDiffOpColorPickerHSL.Value && self.MenuMouseMode ? new(104, 25) : origFocus;
                    });
                    ColorConfigMod.DebugLog("Successfully patched focus glow for hue2lit text");
                    cursor.GotoNext(x => x.MatchLdarg(0), x => x.MatchLdfld<OpColorPicker>(nameof(OpColorPicker._lblB)));
                    cursor.GotoNext(MoveType.After, x => x.MatchCallvirt<FLabel>("set_color"));
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(delegate (OpColorPicker self)
                    {
                        if (ModOptions.EnableDiffOpColorPickerHSL.Value && self.MenuMouseMode)
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
                        return ModOptions.EnableDiffOpColorPickerHSL.Value && self.MenuMouseMode ? new(104f, 105f) : origFocus;
                    });
                    ColorConfigMod.DebugLog("Successfully patched focus glow for lit2hue text");
                }
                catch (Exception ex)
                {
                    ColorConfigMod.DebugException("Failed to fix focus glow pos and text color, not a big issue tho", ex);
                }
            }
            public void On_OPColorPicker_Ctor(On.Menu.Remix.MixedUI.OpColorPicker.orig_ctor orig, OpColorPicker self, Configurable<Color> config, Vector2 pos)
            {
                orig(self, config, pos);
                self._lblHSL.text = ModOptions.PickerHSVMode? "HSV" : self._lblHSL.text;
            }
            public void On_OPColorPickerRecalculateTexture(On.Menu.Remix.MixedUI.OpColorPicker.orig__RecalculateTexture orig, OpColorPicker self)
            {
                 if (self._mode == OpColorPicker.PickerMode.HSL && (ModOptions.EnableDiffOpColorPickerHSL.Value || ModOptions.HSL2HSVOPColorPicker.Value || ModOptions.EnableBetterOPColorPicker.Value))
                {
                    self._ttre1 = new(ModOptions.EnableDiffOpColorPickerHSL.Value ? 101 : 100, 101)
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
                        for (int width = 0; width < self._ttre1.width; width++)
                        {
                            self._ttre1.SetPixel(width, height, ColorPicker2RGB.Invoke(ModOptions.EnableDiffOpColorPickerHSL.Value ? self.ParseGetHSLORHSV01(null, width/100f, height/100f) : self.ParseGetHSLORHSV01(width / 100f, height / 100f, null)));
                            if (width < self._ttre2.height)
                            {
                                self._ttre2.SetPixel(width, height, ColorPicker2RGB.Invoke(ModOptions.EnableDiffOpColorPickerHSL.Value ? self.ParseGetHSLORHSV01(height / 100f, null, null) : self.ParseGetHSLORHSV01(null, null, height / 100f)));
                            }

                        }
                    }
                    //Arrows
                    Color hueArrowCol = SmallUtils.FindHSLArrowColor(self);
                    SmallUtils.ApplyHSLArrows(ModOptions.EnableDiffOpColorPickerHSL.Value ? self._ttre2 : self._ttre1, self._h, hueArrowCol, ModOptions.EnableDiffOpColorPickerHSL.Value, ModOptions.EnableDiffOpColorPickerHSL.Value ? 51 : self._s);
                    SmallUtils.ApplyHSLArrows(self._ttre1, self._s, hueArrowCol, !ModOptions.EnableDiffOpColorPickerHSL.Value, ModOptions.EnableDiffOpColorPickerHSL.Value ? self._l : self._h);
                    SmallUtils.ApplyHSLArrows(ModOptions.EnableDiffOpColorPickerHSL.Value ? self._ttre1 : self._ttre2, self._l, hueArrowCol, true, ModOptions.EnableDiffOpColorPickerHSL.Value ? self._s : 51);
                    self._ttre1.Apply();
                    self._ttre2.Apply();
                    return;
                }
                orig(self);
            }
            public void On_OPColorPicker_Update(On.Menu.Remix.MixedUI.OpColorPicker.orig_Update orig, OpColorPicker self)
            {
                orig(self);
                if (self.IsMouseMode())
                {
                    if (self._MouseOverHex())
                    {
                        ((Action)(SmallUtils.CopyShortcutPressed() ? self.CopyHexCPicker : SmallUtils.PasteShortcutPressed() ? self.PasteHexCPicker : null))?.Invoke();
                    }
                    if (ModOptions.CopyPasteForColorPickerNumbers.Value && self.IfCPickerNumberHovered(out int oOO))
                    {
                        ((Action<int>)(SmallUtils.CopyShortcutPressed() ? self.CopyNumberCPicker : SmallUtils.PasteShortcutPressed() ? self.PasteNumberCPicker : null))?.Invoke(oOO);
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
            private void GotoHSL(ILCursor cursor)
            {
                cursor.GotoNext(x => x.MatchCall(typeof(Custom), "HSL2RGB"));
                cursor.GotoNext(MoveType.After, x => x.MatchCallvirt<FNode>("set_isVisible"));
            }
            private void MakeAReturn(ILCursor cursor)
            {
                cursor.Emit(OpCodes.Brfalse, cursor.Next);
                cursor.Emit(OpCodes.Ret);
            }
            public static Func<Vector3, Color> ColorPicker2RGB
            {
                get => ModOptions.HSL2HSVOPColorPicker.Value ? ColConversions.HSV2RGB : ColConversions.HSL2RGB;
            }
        }
        public class ExpeditionMenuHooks
        {
            public void Init()
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
            public void On_ExpeditionMenu_Singal(On.Menu.ExpeditionMenu.orig_Singal orig, ExpeditionMenu self, MenuObject sender, string message)
            {
                if (ColorConfigMod.IsBingoOn)
                {
                    BingoUtils.TryApplyBingoColors(self, message);
                }
                orig(self, sender, message);
            }
            public void On_CharacterSelectPage_Ctor(On.Menu.CharacterSelectPage.orig_ctor orig, CharacterSelectPage self, Menu.Menu menu, MenuObject owner, Vector2 pos)
            {
                orig(self, menu, owner, pos);
                SymbolButton colorConfig = self.GetExtraEXPInterface().colorConfig;
                if (!ModManager.JollyCoop && ModManager.MMF && colorConfig == null && ModOptions.EnableExpeditionColorConfig.Value)
                {
                    colorConfig = new(menu, self, "colorconfig_slugcat_noncoloured", "DUSTY_EXPEDITION_CONFIG", new(440 + (self.jollyToggleConfigMenu?.pos == new Vector2(440, 550) ? -self.jollyToggleConfigMenu.size.x - 10 : 0), 550));
                    colorConfig.roundedRect.size = new(50, 50);
                    colorConfig.size = colorConfig.roundedRect.size;
                    self.subObjects.Add(colorConfig);
                    self.GetExtraEXPInterface().colorConfig = colorConfig;

                }
            }
            public void On_CharacterSelectPage_SetUpSelectables(On.Menu.CharacterSelectPage.orig_SetUpSelectables orig, CharacterSelectPage self)
            {
                orig(self);
                SymbolButton colorConfig = self.GetExtraEXPInterface().colorConfig;
                if (colorConfig != null)
                {
                    colorConfig.MenuObjectBind((self.menu as ExpeditionMenu).muteButton, left: true, top: true);
                    colorConfig.MenuObjectBind(self.jollyToggleConfigMenu != null ? self.jollyToggleConfigMenu : self.slugcatButtons.GetValueOrDefault(0), true);
                    colorConfig.MenuObjectBind(self.slugcatButtons.Length > 3 ? self.slugcatButtons[3] : self.confirmExpedition, bottom: true);
                }
            }
            public void On_CharacterSelectPage_Singal(On.Menu.CharacterSelectPage.orig_Singal orig, CharacterSelectPage self, MenuObject sender, string message)
            {
                orig(self, sender, message);
                if (self.menu is ExpeditionMenu { pagesMoving: false } && message == "DUSTY_EXPEDITION_CONFIG")
                {
                    self.menu.PlaySound(SoundID.MENU_Player_Join_Game);
                    self.menu.manager.ShowDialog(new ExpeditionColorDialog(self.menu, SmallUtils.ExpeditionSlugcat(), () =>
                    {
                        self.GetExtraEXPInterface().colorConfig?.symbolSprite?.SetElementByName(GetColorEnabledSprite(self.menu));
                    }, ModOptions.EnableHexCodeTypers.Value, showSlugcatDisplay: ModOptions.EnableSlugcatDisplay.Value));

                }

            }
            public void On_CharacterSelectPage_UpdateChallengePreview(On.Menu.CharacterSelectPage.orig_UpdateChallengePreview orig, CharacterSelectPage self)
            {
                orig(self);
                self.GetExtraEXPInterface().colorConfig?.symbolSprite?.SetElementByName(GetColorEnabledSprite(self.menu));
            }
            public void On_CharacterSelectPage_RemoveSprites(On.Menu.CharacterSelectPage.orig_RemoveSprites orig, CharacterSelectPage self)
            {
                orig(self);
                self.ClearMenuObject(ref self.GetExtraEXPInterface().colorConfig);
            }
            public void On_CharacterSelectPage_LoadGame(On.Menu.CharacterSelectPage.orig_LoadGame orig, CharacterSelectPage self)
            {
                self.menu.TrySaveExpeditionColorsToCustomColors(SmallUtils.ExpeditionSlugcat());
                orig(self);
            }
            public void On_ChallengeSelectPage_StartGame(On.Menu.ChallengeSelectPage.orig_StartGame orig, ChallengeSelectPage self)
            {
                self.menu.TrySaveExpeditionColorsToCustomColors(SmallUtils.ExpeditionSlugcat());
                orig(self);
            }
            public static string GetColorEnabledSprite(Menu.Menu menu) => menu.IsCustomColorEnabled(Expedition.ExpeditionData.slugcatPlayer) ? "colorconfig_slugcat_coloured" : "colorconfig_slugcat_noncoloured";
            public class ExtraExpeditionInterfaces
            {
                public SymbolButton colorConfig;
            }
            public static class BingoUtils
            {
                public static void TryApplyBingoColors(Menu.Menu menu, string message)
                {
                    if (message == "STARTBINGO" || message == "LOADBINGO")
                    {
                        menu.TrySaveExpeditionColorsToCustomColors(SmallUtils.ExpeditionSlugcat(), !BingoMode.BingoData.MultiplayerGame);
                    }
                }
            }
        }
    }
}
