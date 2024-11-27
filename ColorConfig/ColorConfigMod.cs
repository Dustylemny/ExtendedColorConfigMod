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
            On.RainWorld.OnModsInit += RainWorld_OnMods;
            Logger.LogMessage("Beep boop beep!");
        }
        private void RainWorld_OnMods(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            if (!IsApplied)
            {
                IsApplied = true;
                try
                {
                    ModOptions modOptions = new();
                    MachineConnector.SetRegisteredOI("dusty.colorconfig", modOptions);
                    SlugcatSelectScreenHooks selectScreenHooks = new();
                    JollyMenuHooks jollyMenuHooks = new();
                    selectScreenHooks.Init();
                    jollyMenuHooks.Init();
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

            DrawTexture(ref Tabs[0], new(340, 460), "HSL",CustomColorModel.HSL);
            DrawTexture(ref Tabs[0], new(340, 320), "HSV",CustomColorModel.HSV);

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
        protected void DrawTexture(ref OpTab tab, Vector2 pos, string name, CustomColorModel colorModel)
        {
            Texture2D texture = new(101, 101)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int i = 0; i < 101; i++)
            {
                for (int j = 0; j < 101; j++)
                {
                    Color color = colorModel == CustomColorModel.HSL ?
                    ColConversions.HSL2RGB(new(0.5f, i / 100f, j / 100f)) : ColConversions.HSV2RGB(new(0.5f, i / 100f, j / 100f));
                    texture.SetPixel(i, j, color);
                }
            }
            texture.Apply();
            OpImage image = new(pos, texture);
            OpLabel label = new(new(pos.x + 110, pos.y), new(30, 60), name, FLabelAlignment.Left);
            tab.AddItems(image, label);
        }
        public static bool ShowVisual
        { get => enableVisualisers.Value; }
        public static int Digit
        { get => decimalCount.Value; }
        public static bool EnableJollyPages
        {
            get
            {
                return EnablePages &&  EnableSliders;
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
    }


}
