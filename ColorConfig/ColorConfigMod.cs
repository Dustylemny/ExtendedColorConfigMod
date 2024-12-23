global using Vector2 = UnityEngine.Vector2;
global using Vector3 = UnityEngine.Vector3;
global using Color = UnityEngine.Color;
using System;
using System.Linq;
using System.Security.Permissions;
using System.Security;
using BepInEx;
using Menu.Remix.MixedUI;
using UnityEngine;
using MonoMod.Cil;
using RainMeadow;
using BepInEx.Logging;
#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ColorConfig
{
    [BepInPlugin("dusty.colorconfig", "Extended Color Config", "1.0.6")]
    [BepInDependency("henpemaz.rainmeadow", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("vultumast.lukkyqolmod", BepInDependency.DependencyFlags.SoftDependency)]
    public class ColorConfigMod : BaseUnityPlugin
    {
        public void OnEnable()
        {
            PluginLogger = Logger;
            PluginLogger.LogMessage("Getting ready to add new color configs!");
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            PluginLogger.LogMessage("Beep boop beep!");
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
                    ColorConfigHooks.SlugcatSelectMenuHooks selectScreenHooks = new();
                    ColorConfigHooks.OpColorPickerHooks opColorPickerHooks = new();
                    ColorConfigHooks.JollyCoopConfigHooks jollyCoopConfigHooks = new();
                    selectScreenHooks.Init();
                    jollyCoopConfigHooks.Init();
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
            if (shouldEnableCursorDebug)
            {
                DebugLog(message + $"\nPrev : {cursor.Prev},\nNext : {cursor.Next}");
                return;
            }
            DebugLog(message + $"Index: {cursor.Index}");
        }
        private bool IsApplied { get; set; } = false;
        private static bool shouldEnableCursorDebug = false;
        public static ManualLogSource PluginLogger
        { get; private set; }
        public static bool IsLukkyRGBColorSliderModOn
        {
            get => ModManager.ActiveMods.Any(x => x.id == "vultumast.rgbslider");
        }
        public static bool IsRainMeadowOn
        { get => ModManager.ActiveMods.Any(x => x.id == "henpemaz_rainmeadow"); }
    }
    public class ModOptions : OptionInterface
    {
        public ModOptions()
        {
            enableVisualisers = config.Bind("EnableVisualisers", true, new ConfigurableInfo("Shows values of color config sliders (Based on color space)", tags: new[]
            {
                "Enable Visualisers?"
            }));
            enableSlugcatDisplay = config.Bind("enableSlugcatDisplay", true, new ConfigurableInfo("Shows slugcat display on slugcat select menu", tags: new[]
            {
                "Enable Slugcat Display?"
            }));
            decimalCount = config.Bind("DecimalCount", 2, new ConfigurableInfo("Adjusts the amount decimal places to show", tags: new[]
            {
                "How many decimal places?"
            }));
            removeHSLSliders = config.Bind("RemoveHSLSliders", false, new ConfigurableInfo("Removes HSL Sliders, will remove if other sliders are turned on", tags: new[]
            {
                "Remove HSL Sliders?"
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
            enableBetterOPColorPicker = config.Bind("EnableBetterOPColorPicker", true, new ConfigurableInfo("Makes OP-ColorPicker selectors a bit less annoying", tags: new[]
            {
                "Enable Better OP-ColorPickers"
            }));
            enableDifferentOpColorPickerHSLPos = config.Bind("EnableDifferentOPColorPicker", true, new ConfigurableInfo("Makes some changes to OP-ColorPickers' HSL/HSV Mode", tags: new[]
            {
                "Enable Different OP-ColorPickers",
            }));
            hsl2HSVOPColorPicker = config.Bind("HSL2HSVOPColorPicker", true, new ConfigurableInfo("Replaces OP-ColorPickers' HSL mode to HSV mode", tags: new[]
            { 
                "Enable HSV OP-ColorPickers?"
            }));
            nextPageKeyBind = config.Bind("SliderPages_NextPage_Controller", KeyCode.Joystick1Button3, new ConfigurableInfo("KeyBind for going to next slider page", tags: new[]
            {
                "Next Page Keybind"
            }));
            prevPageKeyBind = config.Bind("SliderPages_PrevPage_Controller", KeyCode.Joystick1Button0, new ConfigurableInfo("KeyBind for going to prev slider page", tags: new[]
            {
                "Prev Page Keybind"
            }));
        }
        public override void Initialize()
        {
            base.Initialize();
            Tabs = new OpTab[] { new(this, Translate("MAIN")), new(this, Translate("KEYBINDS"))};
            //Visualiser
            DrawCheckBoxLabel(ref Tabs[0], enableVisualisers, new(50, 500));
            //SlugcatDisply
            DrawCheckBoxLabel(ref Tabs[0], enableSlugcatDisplay, new(50, 460));
            //MaxDecimal
            DrawIntDragger(ref Tabs[0], decimalCount, new(50, 420));
            //RemoveHSLSlider
            DrawCheckBoxLabel(ref Tabs[0], ref RemoveHSLSliderBox, removeHSLSliders, new(50, 380));
            //RGBSlider
            DrawCheckBoxLabel(ref Tabs[0], enableRGBSliders, new(50, 340));
            //HSVSlider
            DrawCheckBoxLabel(ref Tabs[0], enableHSVSliders, new(50, 300));
            //HexTyper
            DrawCheckBoxLabel(ref Tabs[0], enableHexCodeTypers, new(50, 260));
            //OPColorPicker
            DrawCheckBoxLabel(ref Tabs[0], enableBetterOPColorPicker, new(50, 220));
            DrawCheckBoxLabel(ref Tabs[0], enableDifferentOpColorPickerHSLPos, new(50, 180));
            DrawCheckBoxLabel(ref Tabs[0], hsl2HSVOPColorPicker, new(50, 140));

            //SliderPagesKeyBinds
            DrawKeyBinder(ref Tabs[1], nextPageKeyBind, new(60, 500));
            DrawKeyBinder(ref Tabs[1], prevPageKeyBind, new(60, 460));
        }
        public override void Update()
        {
            base.Update();
            if (RemoveHSLSliderBox != null)
            {
                RemoveHSLSliderBox.greyedOut = !(enableRGBSliders.Value || enableHSVSliders.Value);
            }
        }
        protected void DrawCheckBoxLabel(ref OpTab tab, ref OpCheckBox checkBox, Configurable<bool> config, Vector2 pos)
        {
            checkBox = new(config, pos)
            {
                description = Translate(config.info.description),
            };
            OpLabel label = new(new(pos.x + 45, pos.y), new(60, 25), Translate((string)config.info.Tags[0]), FLabelAlignment.Left);
            tab.AddItems(checkBox, label);
        }
        protected void DrawCheckBoxLabel(ref OpTab tab, Configurable<bool> config, Vector2 pos)
        {
            OpCheckBox checkBox = new(config, pos)
            {
                description = Translate(config.info.description),
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
        protected void DrawKeyBinder(ref OpTab tab, Configurable<KeyCode> config, Vector2 pos, bool conflict = true, OpKeyBinder.BindController controllerNumber = OpKeyBinder.BindController.AnyController)
        {
            OpKeyBinder keyBinder = new(config, pos, new(90, 40), conflict, controllerNumber)
            {
                description = Translate(config.info.description),
            };
            OpLabel label = new(new(pos.x + 85, pos.y), new(60, 25), Translate((string)config.info.Tags[0]), FLabelAlignment.Left);
            tab.AddItems(keyBinder, label);
        }
        public static bool ShowVisual
        { get => enableVisualisers.Value; }
        public static bool RemoveHSLSliders
        { get =>  (enableRGBSliders.Value || enableHSVSliders.Value) && removeHSLSliders.Value; }
        public static int Digit
        { get => decimalCount.Value; }
        public static bool EnableJollyRGBSliders
        { get => ColorConfigMod.IsLukkyRGBColorSliderModOn || enableRGBSliders.Value; }
        public static OpCheckBox RemoveHSLSliderBox;
        public static Configurable<bool> enableSlugcatDisplay, enableVisualisers, 
            removeHSLSliders, enableRGBSliders, enableHSVSliders, enableHexCodeTypers, 
            hsl2HSVOPColorPicker, enableDifferentOpColorPickerHSLPos, enableBetterOPColorPicker;
        public static Configurable<int> decimalCount;
        public static Configurable<KeyCode> nextPageKeyBind, prevPageKeyBind;
    }


}
