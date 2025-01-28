global using Vector2 = UnityEngine.Vector2;
global using Vector3 = UnityEngine.Vector3;
global using Color = UnityEngine.Color;
using System;
using System.Linq;
using System.Security.Permissions;
using System.Security;
using BepInEx;
using Menu.Remix.MixedUI;
using MonoMod.Cil;
using System.Collections.Generic;
using Menu.Remix.MixedUI.ValueTypes;
using BepInEx.Logging;
using System.Diagnostics;
using System.Reflection;
#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ColorConfig
{
    [BepInPlugin("dusty.colorconfig", "Extended Color Config", "1.2.8")]
    public sealed class ColorConfigMod : BaseUnityPlugin
    {
        public void OnEnable()
        {
            Logger = base.Logger;
            Logger.LogMessage("Getting ready to add new color configs!");
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            Logger.LogMessage("Beep boop beep!");
        }
        public void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            if (!IsApplied)
            {
                IsApplied = true;
                try
                {
                    ModOptions modOptions = new();
                    MachineConnector.SetRegisteredOI("dusty.colorconfig", modOptions);
                    ColorConfigHooks.Init();
                }
                catch (Exception ex)
                {
                    Logger.LogMessage("Oopsies, error occured");
                    Logger.LogError(ex);
                }
            }
        }
        public static void DebugLog(string message)
        {
            Logger.LogInfo(message);
        }
        public static void DebugError(string message)
        {
            Logger.LogError(message);
        }
        public static void DebugException(string message, Exception ex)
        {
            Logger.LogError(message);
            Logger.LogError(ex);
        }
        public static void DebugILCursor(string message,ILCursor cursor)
        {
            if (shouldEnableCursorDebug)
            {
                DebugLog(message + $"\nPrev : {cursor.Prev},\nNext : {cursor.Next}");
                return;
            }
            DebugLog(message + $"Index: {cursor.Index}");
        }
        private bool IsApplied { get; set; } = false;
        private static readonly bool shouldEnableCursorDebug = false;
        public static bool IsLukkyRGBColorSliderModOn => ModManager.ActiveMods.Any(x => x.id == "vultumast.rgbslider");
        public static bool IsRainMeadowOn  => ModManager.ActiveMods.Any(x => x.id == "henpemaz_rainmeadow");
        public static new ManualLogSource Logger { get; private set; }
    }
    public sealed class ModOptions : OptionInterface
    {
        public ModOptions()
        {
            EnableVisualisers = config.Bind("EnableVisualisers", true, new ConfigurableInfo("Shows values of color config sliders (Based on color space)", tags:
            [
                "Enable Visualisers?"
            ]));
            EnableSlugcatDisplay = config.Bind("enableSlugcatDisplay", true, new ConfigurableInfo("Shows slugcat display on slugcat select menu", tags:
            [
                "Enable Slugcat Display?"
            ]));
            DecimalCount = config.Bind("DecimalCount", 2, new ConfigurableInfo("Adjusts the amount decimal places to show", tags:
            [
                "How many decimal places?"
            ]));
            RemoveHSLSliders = config.Bind("RemoveHSLSliders", false, new ConfigurableInfo("Removes HSL Sliders, will remove if other sliders are turned on", tags:
            [
                "Remove HSL Sliders?"
            ]));
            EnableRGBSliders = config.Bind("EnableRGBSliders", true, new ConfigurableInfo("Enables RGB Sliders", tags:
            [
                "Enable RGB Sliders?"
            ]));
            EnableHSVSliders = config.Bind("EnableHSVSliders", false, new ConfigurableInfo("Enables HSV Sliders", tags:
            [
                "Enable HSV Sliders?"
            ]));
            EnableHexCodeTypers = config.Bind("EnableHexCodeTypers", true, new ConfigurableInfo("Enables Hex Typers", tags:
            [
                "Enable HexCode Typers?"
            ]));
            EnableBetterOPColorPicker = config.Bind("EnableBetterOPColorPicker", true, new ConfigurableInfo("Makes OP-ColorPicker selectors a bit less annoying", tags:
            [
                "Enable less annoying OP-ColorPickers"
            ]));
            EnableDifferentOpColorPickerHSLPos = config.Bind("EnableDifferentOPColorPicker", true, new ConfigurableInfo("Changes HueSat Square Picker in HSL/HSV Mode to SatLit Square picker and vice versa", tags:
            [
                "Enable Different OP-ColorPickers",
            ]));
            HSL2HSVOPColorPicker = config.Bind("HSL2HSVOPColorPicker", true, new ConfigurableInfo("Replaces OP-ColorPickers' HSL mode to HSV mode", tags:
            [
                "Enable HSV OP-ColorPickers?"
            ]));
            RMStoryMenuSlugcatFix = config.Bind("RainMeadowStoryMenuSlugcatFix", true, new ConfigurableInfo("Fixes a small bug in rain meadow where color interface does not match slugcat campaign if the host enables campaign slugcat only \n will be applied if ran meadow has been enabled", tags: [
                "Fix Color Interface in Rain Meadow Story Mode?",
            ]));
            IntToFloatColValues = config.Bind("Int2FloatColValues", true, new ConfigurableInfo("Adds decimals to hue and RGB values to sliders, with 'Enable Visualisers' on", tags: [
                "Fix Color Interface in Rain Meadow Story Mode?",
            ]));
            /*DisableHueSliderMaxClamp = config.Bind("DisableHueSliderMaxClamp", false, new ConfigurableInfo("An experiemental feature that Increases the hue slider max value you can set to 100% instead of 99% in story menu\nCAUTION as if you disable the mod after you have saved hue to 100%, it may make your saved color grey in story menu, due to rw's hue conversion.", tags:
            [
                "Disable Hue Slider max slider value?"
            ]));*/
            CopyPasteForSliders = config.Bind("CopyPasteForSliders", false, new ConfigurableInfo("Adds copy paste support to color sliders, is experimental and changes slider value based on color space\nRGB follows 255, Hue in HSL follows 360", tags: [
                "Copy Paste support for color sliders?"
            ]));
            CopyPasteForColorPickerNumbers = config.Bind("CopyPasteForColorPickerNumbers", false, new ConfigurableInfo("Adds copy paste support to color picker numbers, is experimental and changes value based on 0-100 in RGB and HSL/HSV Mode \n Recommend to use non-mouse (movement buttons)", tags: [
                "Copy Paste support for OP-ColorPicker Numbers?"
            ]));
            EnableLegacyIdeaSlugcatDisplay = config.Bind("EnableLegacyIdeaSlugcatDisplay", false, new ConfigurableInfo("Makes slugcat display in story menu to appear when the custom color checkbox is checked\ninstead of when configuring colors", tags: [
                "Enable Original Idea Slugcat Display?"
            ]));
            EnableLegacyHexTypers = config.Bind("enableLegacyVersionHexTypers", false, new ConfigurableInfo("Overrides hex code typers with a replica of the early versions for hex code type boxes", tags:
            [
               "Enable Legacy HexCode Type Boxes?",
            ]));
            EnableLegacySliders = config.Bind("enableLegacyVersionSliders", false, new ConfigurableInfo("Overrides non-hsl sliders with a replica of the early versions for extra sliders", tags:
            [
               "Enable Legacy Sliders?",
            ]));
        }
        public override void Initialize()
        {
            base.Initialize();
            Tabs = [new(this, Translate("Main")), new(this, Translate("Legacy"))];
            //Visualiser
            DrawCheckBoxLabel(ref Tabs[0], EnableVisualisers, new(50, 540));
            //SlugcatDisply
            DrawCheckBoxLabel(ref Tabs[0], EnableSlugcatDisplay, new(50, 500));
            //MaxDecimal
            DrawIntDragger(ref Tabs[0], DecimalCount, new(50, 460));
            //RemoveHSLSlider
            DrawCheckBoxLabel(ref Tabs[0], RemoveHSLSliders, new(50, 420), out RemoveHSLSliderBox);
            //RGBSlider
            DrawCheckBoxLabel(ref Tabs[0], EnableRGBSliders, new(50, 380), out OpCheckBox rgbCheckBox);
            //HSVSlider
            DrawCheckBoxLabel(ref Tabs[0], EnableHSVSliders, new(50, 340), out OpCheckBox hsvCheckBox);
            //HexTyper
            DrawCheckBoxLabel(ref Tabs[0], EnableHexCodeTypers, new(50, 300));
            //OPColorPicker
            DrawCheckBoxLabel(ref Tabs[0], EnableBetterOPColorPicker, new(50, 260));
            DrawCheckBoxLabel(ref Tabs[0], EnableDifferentOpColorPickerHSLPos, new(50, 220));
            DrawCheckBoxLabel(ref Tabs[0], HSL2HSVOPColorPicker, new(50, 180));
            //other mod stuff
            DrawCheckBoxLabel(ref Tabs[0], RMStoryMenuSlugcatFix, new(50, 140));
            //Extras
            DrawCheckBoxLabel(ref Tabs[0], IntToFloatColValues, new(50, 100));
            //DrawCheckBoxLabel(ref Tabs[0], DisableHueSliderMaxClamp, new(50, 100));
            DrawCheckBoxLabel(ref Tabs[0], CopyPasteForSliders, new(50, 60));
            DrawCheckBoxLabel(ref Tabs[0], CopyPasteForColorPickerNumbers, new(50, 20));
            //Legacy
            DrawCheckBoxLabel(ref Tabs[1], EnableLegacyIdeaSlugcatDisplay, new(50, 500));
            DrawCheckBoxLabel(ref Tabs[1], EnableLegacyHexTypers, new(50, 460));

            ExtraSlidersBoxes.AddRange([rgbCheckBox, hsvCheckBox]);
        }
        public override void Update()
        {
            base.Update();
            if (RemoveHSLSliderBox != null)
            {
                if (ExtraSlidersBoxes.All((x) => x == null || !x.GetValueBool()))
                {
                    RemoveHSLSliderBox.greyedOut = true;
                    RemoveHSLSliderBox.SetValueBool(false);
                }
                else if (RemoveHSLSliderBox.greyedOut)
                {
                    RemoveHSLSliderBox.greyedOut = false;
                }
            }
        }
        public void DrawCheckBoxLabel(ref OpTab tab, Configurable<bool> config, Vector2 pos, out OpCheckBox checkBox)
        {
            checkBox = new(config, pos)
            {
                description = Translate(config.info.description),
            };
            OpLabel label = new(new(pos.x + 45, pos.y), new(60, 25), Translate((string)config.info.Tags[0]), FLabelAlignment.Left);
            tab.AddItems(checkBox, label);
        }
        public void DrawCheckBoxLabel(ref OpTab tab, Configurable<bool> config, Vector2 pos)
        {
            OpCheckBox checkBox = new(config, pos)
            {
                description = Translate(config.info.description),
            };
            OpLabel label = new(new(pos.x + 45, pos.y), new(60, 25), Translate((string)config.info.Tags[0]), FLabelAlignment.Left);
            tab.AddItems(checkBox, label);
        }
        public void DrawIntDragger(ref OpTab tab, Configurable<int> config, Vector2 pos)
        {
            OpDragger dragger = new(config, pos)
            {
                description = Translate(config.info.description)
            };
            OpLabel label = new(new(pos.x + 45, pos.y), new(60, 25), Translate((string)config.info.Tags[0]), FLabelAlignment.Left);
            tab.AddItems(dragger, label);
        }
        public static bool ShowVisual => EnableVisualisers.Value;
        public static bool ShouldRemoveHSLSliders => RemoveHSLSliders.Value && OtherCustomSlidersAdded.Any(x => x == true);
        public static bool[] OtherCustomSlidersAdded
        {
            get
            {
                bool[] array = [EnableRGBSliders.Value, EnableHSVSliders.Value];
                return array;
            }
        }
        public static bool EnableJollyRGBSliders => ColorConfigMod.IsLukkyRGBColorSliderModOn || EnableRGBSliders.Value;
        public static int Digit
        { get => DecimalCount.Value; }
       

        public static Configurable<int> DecimalCount { get; private set; }
        public static Configurable<bool> EnableVisualisers { get; private set; }
        public static Configurable<bool> EnableSlugcatDisplay { get; private set; }
        public static Configurable<bool> RemoveHSLSliders { get; private set; }
        public static Configurable<bool> EnableRGBSliders { get; private set; }
        public static Configurable<bool> EnableHSVSliders { get; private set; }
        public static Configurable<bool> EnableHexCodeTypers { get; private set; }
        public static Configurable<bool> HSL2HSVOPColorPicker { get; private set; }
        public static Configurable<bool> EnableDifferentOpColorPickerHSLPos { get; private set; }
        public static Configurable<bool> EnableBetterOPColorPicker { get; private set; }
        public static Configurable<bool> RMStoryMenuSlugcatFix{ get; private set; }

        public static Configurable<bool> IntToFloatColValues { get; private set; }
        //public static Configurable<bool> DisableHueSliderMaxClamp { get; private set;}
        public static Configurable<bool> CopyPasteForSliders { get; private set; }
        public static Configurable<bool> CopyPasteForColorPickerNumbers{ get; private set; }
        public static Configurable<bool> EnableLegacyIdeaSlugcatDisplay { get; private set; }
        public static Configurable<bool> EnableLegacyHexTypers { get; private set; }
        public static Configurable<bool> EnableLegacySliders { get; private set; }

        public OpCheckBox RemoveHSLSliderBox;
        public readonly List<OpCheckBox> ExtraSlidersBoxes = [];
    }


}
