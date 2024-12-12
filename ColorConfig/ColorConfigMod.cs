using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Security;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using Menu.Remix.MixedUI;
using UnityEngine;
using static ColorConfig.Hooks;
using MonoMod.Cil;
#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ColorConfig
{
    [BepInPlugin("dusty.colorconfig", "Extended Color Config", "1.0.5")]
    public class ColorConfigMod : BaseUnityPlugin
    {
        public void OnEnable()
        {
            Logger.LogMessage("Getting ready to add new color configs!");
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            Logger.LogMessage("Beep boop beep!");
        }
        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            if (!IsApplied)
            {
                IsApplied = true;
                try
                {
                    ModOptions modOptions = new();
                    MachineConnector.SetRegisteredOI("dusty.colorconfig", modOptions);
                    NewColorConfigHooks.SlugcatSelectMenuScreenHooks selectScreenHooks = new();
                    NewColorConfigHooks.OpColorPickerHooks opColorPickerHooks = new();
                    NewColorConfigHooks.JollyCoopConfigHooks jollyMenuHooks = new();
                    selectScreenHooks.Init();
                    jollyMenuHooks.Init();
                    opColorPickerHooks.Init();
                    //opColorPickerHooks.Init();
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
            Debug.Log("[LOG]Extended Color Config: " + message);
        }
        public static void DebugError(string message)
        {
            Debug.LogError("[ERROR]Extended Color Config: " + message);
        }
        public static void DebugException(string message, Exception ex)
        {
            DebugError("[EXCEPTION]Extended Color Config: " + message);
            Debug.LogException(ex);
        }
        public static void DebugILCursor(string message,ILCursor cursor)
        {
            DebugLog(message + $"\nPrev : {cursor.Prev},\nNext : {cursor.Next}");
        }
        private bool IsApplied { get; set; } = false;
        public static bool IsRGBColorSliderModOn
        {
            get => ModManager.ActiveMods.Any(x => x.id == "vultumast.rgbslider");
        }
    }
    public class ModOptions : OptionInterface
    {
        public ModOptions()
        {
            enableVisualisers = config.Bind("EnableVisualisers", true, new ConfigurableInfo("Shows values of color config sliders (Based on color space)", tags: new[]
            {
                "Enable Visualisers?"
            }));
            enableSlugcatDisplay = config.Bind("enableSlugcatDisplay", false, new ConfigurableInfo("Shows slugcat display on slugcat select menu when on (BETA RIGHT NOW)", tags: new[]
            {
                "Enable Slugcat Display?"
            }));
            decimalCount = config.Bind("DecimalCount", 2, new ConfigurableInfo("Adjusts the amount decimal places to show", tags: new[]
            {
                "How many decimal places?"
            }));
            enableRGBSliders = config.Bind("EnableRGBSliders", true, new ConfigurableInfo("Enables RGB Sliders", tags: new[]
            {
                "Enable RGB Sliders?"
            }));
            enableHSVSliders = config.Bind("EnableHSVSliders", false, new ConfigurableInfo("Enables HSV Sliders", tags: new[]
            {
                "Enable HSV Sliders?"
            }));
            enableHexCodeTypers = config.Bind("EnableHexCodeTypers", true, new ConfigurableInfo("Enables Hex Typers", tags: new[]
            {
                "Enable HexCode Typers?"
            }));
            enableBetterOPColorPicker = config.Bind("EnableBetterOPColorPicker", true, new ConfigurableInfo("Makes OPColor picker selectors more readable", tags: new[]
            {
                "Enable Better OP-ColorPickers"
            }));
            hsl2HSVOPColorPicker = config.Bind("HSL2HSVOPColorPicker", true, new ConfigurableInfo("Replaces OP-ColorPicker's HSL mode to HSV mode", tags: new[]
            { 
                "Replace HSLColorPicker?"
            }));
        }
        public override void Initialize()
        {
            base.Initialize();
            Tabs = new OpTab[] { new(this, "MAIN")};

            //Visualiser
            DrawCheckBoxLabel(ref Tabs[0], enableVisualisers, new(90, 500));
            //SlugcatDisply
            DrawCheckBoxLabel(ref Tabs[0], enableSlugcatDisplay, new(90, 460));
            //MaxDecimal
            DrawIntDragger(ref Tabs[0], decimalCount, new(90, 420));
            //RGBSlider
            DrawCheckBoxLabel(ref Tabs[0], enableRGBSliders, new(90, 380));
            //HSVSlider
            DrawCheckBoxLabel(ref Tabs[0], enableHSVSliders, new(90, 340));
            //HexTyper
            DrawCheckBoxLabel(ref Tabs[0], enableHexCodeTypers, new(90, 300));
            //OPColorPicker
            DrawCheckBoxLabel(ref Tabs[0], enableBetterOPColorPicker, new(90, 260));
            DrawCheckBoxLabel(ref Tabs[0], hsl2HSVOPColorPicker, new(90, 220));

            OpColorPicker colorPicker = new(new(Color.cyan), new(200, 300));
            Tabs[0].AddItems(colorPicker);
        }
        protected void DrawCheckBoxLabel(ref OpTab tab, Configurable<bool> config, Vector2 pos, bool forceGreyOut = false)
        {
            OpCheckBox checkBox = new(config, pos)
            {
                description = Translate(config.info.description),
                greyedOut = forceGreyOut
            };
            OpLabel label = new(new(pos.x + 45, pos.y), new(60, 25), Translate((string)config.info.Tags[0]), FLabelAlignment.Left);
            tab.AddItems(checkBox, label);
        }
        protected void DrawIntDragger(ref OpTab tab, Configurable<int> config, Vector2 pos)
        {
            OpDragger dragger = new(config, pos)
            {
                description = Translate(config.info.description)
            };
            OpLabel label = new(new(pos.x + 45, pos.y), new(60, 25), Translate((string)config.info.Tags[0]), FLabelAlignment.Left);
            tab.AddItems(dragger, label);
        }
        public static bool ShowVisual
        { get => enableVisualisers.Value; }
        public static bool  HSVOpColorPicker
        { get => hsl2HSVOPColorPicker.Value; }
        public static bool ChangeHSLOpColorPicker
        { get => HSVOpColorPicker || enableBetterOPColorPicker.Value; }
        public static int Digit
        { get => decimalCount.Value; }
        public static bool EnableJollyPages
        {
            get
            {
                return  (new bool[] { (enableRGBSliders.Value && EnableSliders), enableHSVSliders.Value }).Where(x => x is true).Count() > 0;
            }
        }
        public static bool EnablePages
        {
            get
            {
                return (new bool[] { enableRGBSliders.Value, enableHSVSliders.Value }).Where(x => x is true).Count() > 0;
            }
        }
        public static bool EnableSliders
        { get => !ColorConfigMod.IsRGBColorSliderModOn; }

        public static Configurable<bool> enableSlugcatDisplay;
        public static Configurable<bool> enableVisualisers;
        public static Configurable<int> decimalCount;
        public static Configurable<bool> enableRGBSliders;
        public static Configurable<bool> enableHSVSliders;
        public static Configurable<bool> enableHexCodeTypers;
        public static Configurable<bool> hsl2HSVOPColorPicker;
        public static Configurable<bool> enableBetterOPColorPicker;
    }


}
