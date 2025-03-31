global using Vector2 = UnityEngine.Vector2;
global using Vector3 = UnityEngine.Vector3;
global using Color = UnityEngine.Color;
global using OpCodes = Mono.Cecil.Cil.OpCodes;
using System;
using System.Linq;
using System.Security.Permissions;
using System.Security;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx;
using Menu.Remix.MixedUI;
using MonoMod.Cil;
using BepInEx.Logging;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using static ColorConfig.ExtraInterfaces;
using static ColorConfig.MenuInterfaces;
using static ColorConfig.ColorConfigHooks;
#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ColorConfig
{
    [BepInPlugin(id, modName, version)]
    public sealed class ColorConfigMod : BaseUnityPlugin
    {
        public const string id = "dusty.colorconfig", modName = "Extended Color Config", version = "1.3.6";
        public void OnEnable()
        {
            Logger = base.Logger;
            Logger.LogMessage("Getting ready to add new color configs!");
            On.RainWorld.OnModsInit += On_RainWorld_OnModsInit;
            Logger.LogMessage("Beep boop beep!");
        }
        public void On_RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            if (!IsApplied)
            {
                IsApplied = true;
                try
                {
                    CheckIfModsOn();
                    ModOptions modOptions = new();
                    MachineConnector.SetRegisteredOI("dusty.colorconfig", modOptions);
                    Futile.atlasManager.LoadAtlas("atlases/colorconfig_symbols");
                    ColorConfigHooks.Init();
                    DebugLog("OnModsinit Success");
                }
                catch (Exception ex)
                {
                    DebugException("Oopsies, error occured", ex);
                }
            }
        }
        public void CheckIfModsOn()
        {
            IsLukkyRGBColorSliderModOn = CheckIfModOn("vultumast.rgbslider");
            IsRainMeadowOn = CheckIfModOn("henpemaz_rainmeadow");
            IsBingoOn = CheckIfModOn("nacu.bingomode");
        }
        public bool CheckIfModOn(string id)
        {
            return ModManager.ActiveMods.Any(x => x.id == id);
        }
        public static string GetMethodName(int skipframes = 2, bool getAssembly = false)
        {
            StackFrame frame = new(skipframes, true);
            MethodBase method = frame.GetMethod();
            return $"{(getAssembly ? method.DeclaringType.FullName : method.DeclaringType.Name)}.{method.Name}";
        }
        public static void DebugLog(string message)
        {
            Logger.LogInfo($"{GetMethodName()}: {message}");
        }
        public static void DebugWarning(string message)
        {
            Logger.LogWarning($"{GetMethodName()}: {message}");
        }
        public static void DebugError(string message)
        {
            Logger.LogError($"{GetMethodName()}: {message}");
        }
        public static void DebugException(string message, Exception ex)
        {
            Logger.LogError($"{GetMethodName()}: {message}");
            Logger.LogError(ex);
        }
        public static void DebugILCursor(ILCursor cursor, string message = "")
        {
            Logger.LogInfo($"{GetMethodName()}: {message}{(shouldEnableCursorDebug ?
                $"{cursor}" : $"Index: {cursor.Index}")}");
        }
        private bool IsApplied { get; set; } = false;
        private static readonly bool shouldEnableCursorDebug = false;
        public static bool IsLukkyRGBColorSliderModOn { get; private set; }
        public static bool IsRainMeadowOn { get; private set; }
        public static bool IsBingoOn { get; private set; }
        public static new ManualLogSource Logger { get; private set; }

        public static Player.InputPackage pMInput = new(), lastPMInput = new();
        public static ExtraFixedMenuInput femInput = new(), lastFemInput = new();
        public static readonly ConditionalWeakTable<Menu.SlugcatSelectMenu, ExtraSSMInterfaces> extraSSMInterfaces = new();
        public static readonly ConditionalWeakTable<Menu.CharacterSelectPage, ExtraExpeditionInterfaces> extraEXPInterfaces = new();
        public static readonly ConditionalWeakTable<JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider, JollyCoopOOOConfig> extraJollyInterfaces = new();
        public static readonly ConditionalWeakTable<OpColorPicker, ColorPickerExtras> extraColorPickerStuff = new();
    }
    public sealed class ModOptions : OptionInterface
    {
        public static void FindSlidersAdded(List<bool> bools, bool includeHSL = true)
        {
            if (includeHSL)
            {
                bools.Add(!RemoveHSLSliders.Value);
            }
            bools.AddRange([EnableRGBSliders.Value, EnableHSVSliders.Value]);

        }
        public Configurable<T> Bind<T>(string saveName, T defaultVal, string description, string configName, ConfigAcceptableBase configAccept = null, string auto = "")
        {
            return config.Bind(saveName, defaultVal, new ConfigurableInfo(description + $"  DEFAULT - {defaultVal}", configAccept, auto, tags:
            [
                configName,
            ]));
        }
        public ModOptions()
        {
            EnableVisualisers = Bind("EnableVisualisers", true, "Shows values of color config sliders (Based on color space).", "Enable Visualisers?");
            DecimalCount = Bind("DecimalCount", 2, "Shows the number of decimal places in color config slider values.", "How many decimal places?", new IntAcceptable(max: 15));
            SliderRounding = Bind("SliderVisualRounding", MidpointRounding.AwayFromZero, "Changes how values of color config sliders are rounded.", "Rounding Type?");
            EnableSlugcatDisplay = Bind("enableSlugcatDisplay", true, "Shows slugcat display on story menu.", "Enable Slugcat Display?");
            IntToFloatColorValues = Bind("Int2FloatColorValues", true, "Adds decimals to hue and RGB values to sliders, with 'Enable Visualisers?' on.", "More accurate slider color values?");
            RemoveHSLSliders = Bind("RemoveHSLSliders", false, "Removes HSL Sliders in story and jolly-coop menu, will remove if other sliders are turned on.", "Remove HSL Sliders?");
            EnableRGBSliders = Bind("EnableRGBSliders", true, "Enables RGB Sliders in story and jolly-coop menu.", "Enable RGB Sliders?");
            EnableHSVSliders = Bind("EnableHSVSliders", false,"Enables HSV Sliders in story and jolly-coop menu.", "Enable HSV Sliders?");
            CopyPasteForSliders = Bind("CopyPasteForSliders", false, "Adds copy paste support to color sliders, is experimental and changes slider value based on color space\nRGB follows 255, Hue in HSL follows 360.", "Copy Paste support for color sliders?");
            DisableHueSliderMaxClamp = Bind("DisableHueSliderMaxValue", false, "An experiemental feature that increases the hue slider max value you can set to 100% instead of 99% in story menu and expedition menu (if Enable Config for Expedition? is on)\nCAUTION as if you disable the mod after you have saved hue to 100%, it may make your saved color grey , due to rw's hue conversion. (This mod tries to convert back whenever the color config closes)", "Disable Hue Slider max slider value?");

            EnableHexCodeTypers = Bind("EnableHexCodeTypers", true, "Enables HexCode Typers in story and jolly-coop menu.", "Enable HexCode Typers?");
            EnableExpeditionColorConfig = Bind("EnableExpeditionColorConfig", false, "Experiemental, Adds custom color config to expedition.", "Enable Config for Expedition?");

            EnableColorPickers = Bind("EnableColorPickers", false, "Enables Color Pickers, will remove hex code typers since its unneccesary.", "Enable Color Pickers?");
            EnableBetterOPColorPicker = Bind("EnableBetterOPColorPicker", true, "Makes OP-ColorPicker selectors a bit less annoying.", "Enable less annoying OP-ColorPickers?");
            EnableDiffOpColorPickerHSL = Bind("EnableDifferentOPColorPicker", true, "Changes HueSat Square Picker in HSL/HSV Mode to SatLit Square picker and vice versa.", "Enable Different OP-ColorPickers?");
            HSL2HSVOPColorPicker = Bind("HSL2HSVOPColorPicker", true, "Replaces OP-ColorPickers' HSL mode to HSV mode.", "Enable HSV OP-ColorPickers?");
            EnableRotatingOPColorPicker = Bind("EnableRotatingOPColorPicker", true, "Allows OP-ColorPickers to rotate between custom modes. Press on the HSL/HSV Button in HSL/HSV mode to change modes if respective options are on. 'Less annoying OP-ColorPickers' will make toggled HSL mode ignore 'Different OP-ColorPickers'", "Switch custom modes for OP-ColorPickers?");
            CopyPasteForColorPickerNumbers = Bind("CopyPatseOPColorPickerNumbers", false, "Allows copy paste for Color Picker numbers.", "Copy Paste OP-ColorPicker Numbers?");

            EnableLegacyIdeaSlugcatDisplay = Bind("EnableLegacySlugcatDisplay", true, "Makes slugcat display in story menu to appear when the custom color checkbox is checked\ninstead of when configuring colors.", "Enable Original Idea Slugcat Display?");
            EnableLegacySSMSliders = Bind("enableLegacyVersionSliders", false, "Overrides color sliders with a replica of the early versions in story menu.", "Enable Legacy Sliders?");
            EnableLegacyHexCodeTypers = Bind("EnableLegacyHexCodeTypes", false, "Overrides hex code typers with a slight replica of what I had planned at the start of this mod.", "Enable Legacy HexCode Typers?");
            LukkyRGBJollySliderMeansBusiness = Bind("LukkyRGBSliderModActiveReason", true, "Removes HSL Sliders and adds RGB Sliders when RGB Sliders mod is on, else follows remix options.", "Follow what RGB Sliders mod intended to do?");
        }
        public override void Initialize()
        {
            base.Initialize();
            Tabs = [new(this, Translate("Main")), new(this, Translate("Others"))];
            //MOD
            AddModStuff();
            //LEFT SIDE
            ResetControl();
            //Visuals
            AddHeader("Visuals", false);
            AddCheckBoxLabel(EnableVisualisers);
            AddIntDragger(DecimalCount);
            AddEnumList(SliderRounding);
            AddCheckBoxLabel(IntToFloatColorValues);
            AddCheckBoxLabel(EnableSlugcatDisplay);

            //sliders
            AddHeader("Sliders");
            AddCheckBoxLabel(RemoveHSLSliders);
            AddCheckBoxLabel(EnableRGBSliders);
            AddCheckBoxLabel(EnableHSVSliders);
            AddCheckBoxLabel(CopyPasteForSliders);
            //AddCheckBoxLabel(DisableHueSliderMaxClamp);

            //RIGHT SIDE
            ResetControl(325);
            //Utility
            AddHeader("Utility", false);
            AddCheckBoxLabel(EnableHexCodeTypers);
            AddCheckBoxLabel(EnableExpeditionColorConfig);

            //OPColorPicker
            AddHeader("Color Pickers");
            AddCheckBoxLabel(EnableBetterOPColorPicker);
            AddCheckBoxLabel(EnableDiffOpColorPickerHSL);
            AddCheckBoxLabel(HSL2HSVOPColorPicker);
            AddCheckBoxLabel(EnableRotatingOPColorPicker);
            AddCheckBoxLabel(CopyPasteForColorPickerNumbers);

            //Legacy
            AddHeader("Legacy");
            AddCheckBoxLabel(EnableLegacyIdeaSlugcatDisplay);
            AddCheckBoxLabel(EnableLegacySSMSliders);
            AddCheckBoxLabel(EnableLegacyHexCodeTypers);
            DrawAllPendingUI(ref Tabs[0]);

            //NEWTAB LEFT
            ResetControl();
            AddHeader("Mod Support", false);
            AddCheckBoxLabel(LukkyRGBJollySliderMeansBusiness);
            DrawAllPendingUI(ref Tabs[1]);

        }
        public void ResetControl(float x = 25)
        {
            control = new(x, 540);
        }
        public void AddModStuff()
        {
            OpLabel opLabel = new(new Vector2(600, 580), new(70, 10), "version: " + ColorConfigMod.version);
            opLabel.PosX -= opLabel.GetDisplaySize().x + 10;
            AddPendingUI(opLabel);
        }
        public void AddHeader(string text, bool startSpace = true, FLabelAlignment alightment = FLabelAlignment.Left)
        {
            control.y -= startSpace ? 15 : 0;
            OpLabel opLabel = new(control, new(100f, 30f), Translate(text), alightment, true);
            AddPendingUI(opLabel);
            control.y -= 40;
        }
        public void AddCheckBoxLabel(Configurable<bool> config)
        {
            OpCheckBox checkBox = new(config, control)
            {
                description = Translate(config.info.description),
            };
            OpLabel label = new(new(control.x + sBoxLXOffset, control.y), new(60, 25), Translate((string)config.info.Tags[0]), FLabelAlignment.Left);
            AddPendingUI(checkBox, label);
            control.y -= 40;
        }
        public void AddIntDragger(Configurable<int> config)
        {
            OpDragger dragger = new(config, control)
            {
                description = Translate(config.info.description)
            };
            OpLabel label = new(new(control.x + sBoxLXOffset, control.y), new(60, 25), Translate((string)config.info.Tags[0]), FLabelAlignment.Left);
            AddPendingUI(dragger, label);
            control.y -= 40;
        }
        public void AddEnumList(ConfigurableBase config, float width = 130)
        {
            if (!config.settingType.IsEnum && !config.settingType.IsExtEnum())
            {
                throw new NotImplementedException();
            }
            OpResourceList list = new(config, control, width)
            {
                description = Translate(config.info.description),
            };
            float y = list._downward ? 20 * Mathf.Clamp(list._itemList.Length, 1, list._listHeight) + 10 : 0;
            list.PosY -= y;
            OpLabel label = new(new(control.x + width + 10, control.y), new(60, 25), Translate((string)config.info.Tags[0]), FLabelAlignment.Left);
            AddPendingUI(list, label);
            control.y -= y + 40;
        }
        public void AddEnumSelector(ConfigurableBase config, float width = 130, bool followListSize = true)
        {
            if (!config.settingType.IsEnum && !config.settingType.IsExtEnum())
            {
                throw new NotImplementedException();
            }
            OpResourceSelector resource = new(config, control, width)
            {
                description = Translate(config.info.description),
            };
            OpLabel label = new(new(control.x + width + 10, control.y), new(60, 25), Translate((string)config.info.Tags[0]), FLabelAlignment.Left);
            AddPendingUI(resource, label);
            control.y -= followListSize? (20 * Mathf.Clamp(resource._itemList.Length, 1, resource._listHeight) + 50) : 40;
        }
        public void AddColorPicker(Configurable<Color> config)
        {
            control.y -= 125;
            OpColorPicker colorPicker= new(config, control)
            {
                description = Translate(config.info.description),
            };
            AddPendingUI(colorPicker);
            control.y -= 40;
        }
        public void AddPendingUI(params UIelement[] uiElements)
        {
            if (uiElements != null)
            {
                pendingUiElements.AddRange(uiElements);
            }
        }
        public void DrawAllPendingUI(ref OpTab tab)
        {
            tab.AddItems([.. pendingUiElements]);
            pendingUiElements.Clear();
        }
        public static bool[] AllSlidersAdded
        {
            get
            {
                List<bool> list = [];
                FindSlidersAdded(list);
                return [.. list];
            }
        }
        public static bool[] OtherCustomSlidersAdded
        {
            get
            {
                List<bool> list = [];
                FindSlidersAdded(list, false);
                return [.. list];
            }
        }
        public static bool ShowVisual => EnableVisualisers.Value;
        public static bool PickerHSVMode => HSL2HSVOPColorPicker.Value;
        public static bool ShouldAddSSMLegacySliders => EnableLegacySSMSliders.Value && AllSlidersAdded.Count(x => x == true) > 1;
        public static bool ShouldRemoveHSLSliders => RemoveHSLSliders.Value && OtherCustomSlidersAdded.Any(x => x == true);
        public static bool EnableJollyRGBSliders => EnableRGBSliders.Value || FollowLukkyRGBSliders;
        public static bool FollowLukkyRGBSliders => (ColorConfigMod.IsLukkyRGBColorSliderModOn && LukkyRGBJollySliderMeansBusiness.Value);
        public static int DeCount => DecimalCount.Value;
        public static Configurable<int> DecimalCount { get; private set; }
        public static Configurable<MidpointRounding> SliderRounding { get; private set; }
        public static Configurable<bool> EnableVisualisers { get; private set; }
        public static Configurable<bool> EnableSlugcatDisplay { get; private set; }
        public static Configurable<bool> RemoveHSLSliders { get; private set; }
        public static Configurable<bool> EnableRGBSliders { get; private set; }
        public static Configurable<bool> EnableHSVSliders { get; private set; }
        public static Configurable<bool> EnableHexCodeTypers { get; private set; }
        public static Configurable<bool> EnableExpeditionColorConfig { get; private set; }
        public static Configurable<bool> EnableColorPickers { get; private set; }
        public static Configurable<bool> HSL2HSVOPColorPicker { get; private set; }
        public static Configurable<bool> EnableDiffOpColorPickerHSL { get; private set; }
        public static Configurable<bool> EnableBetterOPColorPicker { get; private set; }
        public static Configurable<bool> EnableRotatingOPColorPicker { get; private set; }
        public static Configurable<bool> IntToFloatColorValues { get; private set; }
        public static Configurable<bool> DisableHueSliderMaxClamp { get; private set; }
        public static Configurable<bool> CopyPasteForSliders { get; private set; }
        public static Configurable<bool> CopyPasteForColorPickerNumbers{ get; private set; }
        public static Configurable<bool> EnableLegacyIdeaSlugcatDisplay { get; private set; }
        public static Configurable<bool> EnableLegacySSMSliders { get; private set; }
        public static Configurable<bool> EnableLegacyHexCodeTypers { get; private set; }
        public static Configurable<bool> LukkyRGBJollySliderMeansBusiness { get; private set; }

        public const float sBoxLXOffset = 35;
        public List<UIelement> pendingUiElements = [];
        public Vector2 control = new(25, 540);
        public class IntAcceptable(int min = 0, int max = 99) : GenericAcceptable<int>
        {
            public override int ClampT(int value)
            {
                return Mathf.Clamp(value, min, max);
            }
            public int min = min, max = max;
        }
        public abstract class GenericAcceptable<T> : ConfigAcceptableBase
        {
            public GenericAcceptable() : base(typeof(T))
            {

            }
            public override string ToDescriptionString()
            {
                return $"Acceptable Base for generic values, currently: {typeof(T).Name}";
            }
            public override bool IsValid(object value)
            {
                return value.GetType() == typeof(T);
            }
            public override object Clamp(object value)
            {
                return IsValid(value)? ClampT((T)value) : value;
            }
            public abstract T ClampT(T value);
        }
    }


}
