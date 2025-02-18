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
using UnityEngine;
using Menu;
using System.Reflection.Emit;
#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ColorConfig
{
    [BepInPlugin(SmallUtils.id, SmallUtils.name, SmallUtils.version)]
    public sealed class ColorConfigMod : BaseUnityPlugin
    {
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
            IsLukkyRGBColorSliderModOn = ModManager.ActiveMods.Any(x => x.id == "vultumast.rgbslider");
            IsRainMeadowOn = ModManager.ActiveMods.Any(x => x.id == "henpemaz_rainmeadow");
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
                $"\nPrev : {cursor.Prev},\nNext : {cursor.Next}" : $"Index: {cursor.Index}")}");
        }
        private bool IsApplied { get; set; } = false;
        private static readonly bool shouldEnableCursorDebug = false;
        public static bool IsLukkyRGBColorSliderModOn { get; private set; }
        public static bool IsRainMeadowOn { get; private set; }
        public static new ManualLogSource Logger { get; private set; }

        public static MenuInterfaces.ExtraFixedMenuInput femInput = new(), lastFemInput = new();
    }
    public sealed class ModOptions : OptionInterface
    {
        public static void FindSlidersAdded(List<bool> bools, bool includeHSL = true)
        {
            if (includeHSL)
            {
                bools.Add(!ModOptions.RemoveHSLSliders.Value);
            }
            bools.AddRange([ModOptions.EnableRGBSliders.Value, ModOptions.EnableHSVSliders.Value]);

        }
        public ModOptions()
        {
            EnableVisualisers = config.Bind("EnableVisualisers", true, new ConfigurableInfo("Shows values of color config sliders (Based on color space)", tags:
            [
                "Enable Visualisers?"
            ]));
            DecimalCount = config.Bind("DecimalCount", 2, new ConfigurableInfo("Shows the number of decimal places in color config slider values", new IntAcceptable(max : 15), tags:
            [
                "How many decimal places?"
            ]));
            SliderRounding = config.Bind("SliderVisualRounding", MidpointRounding.AwayFromZero, new ConfigurableInfo("Changes how values of color config sliders are rounded", tags:
            [
                "Rounding Type?"
            ]));
            EnableSlugcatDisplay = config.Bind("enableSlugcatDisplay", true, new ConfigurableInfo("Shows slugcat display on story menu", tags:
            [
                "Enable Slugcat Display?"
            ]));
            IntToFloatColorValues = config.Bind("Int2FloatColorValues", true, new ConfigurableInfo("Adds decimals to hue and RGB values to sliders, with 'Enable Visualisers?' on", tags: [
                "More accurate slider color values?",
            ]));

            RemoveHSLSliders = config.Bind("RemoveHSLSliders", false, new ConfigurableInfo("Removes HSL Sliders in story and jolly-coop menu, will remove if other sliders are turned on", tags:
            [
                "Remove HSL Sliders?"
            ]));
            EnableRGBSliders = config.Bind("EnableRGBSliders", true, new ConfigurableInfo("Enables RGB Sliders in story and jolly-coop menu", tags:
            [
                "Enable RGB Sliders?"
            ]));
            EnableHSVSliders = config.Bind("EnableHSVSliders", false, new ConfigurableInfo("Enables HSV Sliders in story and jolly-coop menu", tags:
            [
                "Enable HSV Sliders?"
            ]));
            CopyPasteForSliders = config.Bind("CopyPasteForSliders", false, new ConfigurableInfo("Adds copy paste support to color sliders, is experimental and changes slider value based on color space\nRGB follows 255, Hue in HSL follows 360", tags: [
               "Copy Paste support for color sliders?"
           ]));
           
            EnableHexCodeTypers = config.Bind("EnableHexCodeTypers", true, new ConfigurableInfo("Enables HexCode Typers in story and jolly-coop menu", tags:
            [
                "Enable HexCode Typers?"
            ]));


            EnableColorPickers = config.Bind("EnableColorPickers", false, new ConfigurableInfo("Enables Color Pickers, will remove hex code typers since its unneccesary", tags:
            [
                "Enable Color Pickers?"
            ]));
            EnableBetterOPColorPicker = config.Bind("EnableBetterOPColorPicker", true, new ConfigurableInfo("Makes OP-ColorPicker selectors a bit less annoying", tags:
            [
                "Enable less annoying OP-ColorPickers?"
            ]));
            EnableDifferentOpColorPickerHSLPos = config.Bind("EnableDifferentOPColorPicker", true, new ConfigurableInfo("Changes HueSat Square Picker in HSL/HSV Mode to SatLit Square picker and vice versa", tags:
            [
                "Enable Different OP-ColorPickers?",
            ]));
            HSL2HSVOPColorPicker = config.Bind("HSL2HSVOPColorPicker", true, new ConfigurableInfo("Replaces OP-ColorPickers' HSL mode to HSV mode", tags:
            [
                "Enable HSV OP-ColorPickers?"
            ]));
            CopyPasteForColorPickerNumbers = config.Bind("CopyPatseOPColorPickerNumbers", false, new ConfigurableInfo("Experimental, Allows copy paste for Color Picker numbers", tags:
            [
                "Copy Paste OP-ColorPicker Numbers?"
            ]));

            EnableLegacyIdeaSlugcatDisplay = config.Bind("EnableLegacyIdeaSlugcatDisplay", false, new ConfigurableInfo("Makes slugcat display in story menu to appear when the custom color checkbox is checked\ninstead of when configuring colors", tags: [
                "Enable Original Idea Slugcat Display?"
            ]));
            EnableLegacySSMSliders = config.Bind("enableLegacyVersionSliders", false, new ConfigurableInfo("Overrides non-hsl sliders with a replica of the early versions for extra sliders in story menu\nWill be disabled if 'Remove HSL Sliders?' is on", tags:
            [
               "Enable Legacy Sliders?",
            ]));
            
            RMStoryMenuSlugcatFix = config.Bind("RainMeadowStoryMenuSlugcatFix", true, new ConfigurableInfo("Fixes a small bug in rain meadow where color interface does not match slugcat campaign if the host enables campaign slugcat only \n will be applied if rain meadow has been enabled", tags: 
            [
               "Fix Color Interface in Rain Meadow Story Mode?",
            ]));
            LukkyRGBJollySliderMeansBusiness = config.Bind("LukkyRGBSliderModActiveReason", true, new ConfigurableInfo("Removes HSL Sliders and adds RGB Sliders when RGB Sliders mod is on, else follows remix options", tags: 
            [
                "Follow what RGB Sliders mod intended to do?",
            ]));
            DisableHueSliderMaxClamp = config.Bind("DisableHueSliderMaxValueClamp", false, new ConfigurableInfo("An experiemental feature that Increases the hue slider max value you can set to 100% instead of 99% in story menu\nCAUTION as if you disable the mod after you have saved hue to 100%, it may make your saved color grey in story menu, due to rw's hue conversion.", tags:
            [
                 "Disable Hue Slider max slider value?"
            ]));
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

            //RIGHT SIDE
            ResetControl(325);
            //Utility
            AddHeader("Utility", false);
            AddCheckBoxLabel(EnableHexCodeTypers);

            //OPColorPicker
            AddHeader("Color Pickers");
            AddCheckBoxLabel(EnableBetterOPColorPicker);
            AddCheckBoxLabel(EnableDifferentOpColorPickerHSLPos);
            AddCheckBoxLabel(HSL2HSVOPColorPicker);
            AddCheckBoxLabel(CopyPasteForColorPickerNumbers);

            //Legacy
            AddHeader("Legacy");
            AddCheckBoxLabel(EnableLegacyIdeaSlugcatDisplay);
            AddCheckBoxLabel(EnableLegacySSMSliders);
            DrawAllPendingUI(ref Tabs[0]);

            //NEWTAB LEFT
            ResetControl();
            AddHeader("Mod Support", false);
            //AddCheckBoxLabel(RMStoryMenuSlugcatFix);
            AddCheckBoxLabel(LukkyRGBJollySliderMeansBusiness);
            //AddCheckBoxLabel(DisableHueSliderMaxClamp);
            DrawAllPendingUI(ref Tabs[1]);

        }
        public void ResetControl(float x = 25)
        {
            control = new(x, 540);
        }
        public void AddModStuff()
        {
            OpLabel opLabel = new(new Vector2(600, 580), new(70, 10), "version: " + SmallUtils.version);
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
        public void AddEnumList<T>(Configurable<T> config, float width = 130)
        {
            if (!config.settingType.IsEnumORExtEnum())
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
        public void AddEnumSelector<T>(Configurable<T> config, float width = 130, bool followListSize = true)
        {
            if (!config.settingType.IsEnumORExtEnum())
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
        public static Configurable<bool> EnableColorPickers { get; private set; }
        public static Configurable<bool> HSL2HSVOPColorPicker { get; private set; }
        public static Configurable<bool> EnableDifferentOpColorPickerHSLPos { get; private set; }
        public static Configurable<bool> EnableBetterOPColorPicker { get; private set; }
        public static Configurable<bool> IntToFloatColorValues { get; private set; }
        public static Configurable<bool> DisableHueSliderMaxClamp { get; private set; }
        public static Configurable<bool> CopyPasteForSliders { get; private set; }
        public static Configurable<bool> CopyPasteForColorPickerNumbers{ get; private set; }
        public static Configurable<bool> EnableLegacyIdeaSlugcatDisplay { get; private set; }
        public static Configurable<bool> EnableLegacySSMSliders { get; private set; }
        public static Configurable<bool> RMStoryMenuSlugcatFix { get; private set; }
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
        public class GenericAcceptable<T> : ConfigAcceptableBase
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
                if (IsValid(value))
                {
                    return ClampT((T)value);
                }
                return value;
            }
            public virtual T ClampT(T value)
            {
                return value;
            }
        }
    }


}
