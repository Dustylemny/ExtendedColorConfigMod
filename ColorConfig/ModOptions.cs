using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;

namespace ColorConfig
{
    public sealed class ModOptions : OptionInterface
    { 

        public const float sBoxLXOffset = 35;
        public Queue<OpRect> opRects = [];
        public List<UIelement> pendingUiElements = [];
        public float lastHeaderY;
        public Vector2 control = new(25, 540);
        public readonly Configurable<int> DecimalCount;
        public readonly Configurable<MidpointRounding> SliderRounding;
        public readonly Configurable<bool> EnableVisualisers, EnableSlugcatDisplay, IntToFloatColorValues, RemoveHSLSliders, EnableRGBSliders, EnableHSVSliders, CopyPasteForSliders, DisableHueSliderMaxClamp,
            EnableHexCodeTypers,
            EnableExpeditionColorConfig,
            EnableColorPickers, EnableBetterOPColorPicker, EnableDiffOpColorPickerHSL, HSL2HSVOPColorPicker, EnableRotatingOPColorPicker, CopyPasteForColorPickerNumbers,
            EnableLegacyIdeaSlugcatDisplay, EnableLegacySSMSliders, EnableLegacyHexCodeTypers,
            LukkyRGBJollySliderMeansBusiness, EnableRainMeadowArenaConfigExtension;
        public static ModOptions Instance { get; private set; }
        public static void FindSlidersAdded(List<bool> bools, bool includeHSL = true)
        {
            if (includeHSL)
            {
                bools.Add(!Instance.RemoveHSLSliders.Value);
            }
            bools.AddRange([Instance.EnableRGBSliders.Value, Instance.EnableHSVSliders.Value]);

        }
        public Configurable<T> Bind<T>(string saveName, T defaultVal, string description, string configName, ConfigAcceptableBase? configAccept = null, string auto = "")
        {
            return config.Bind(saveName, defaultVal, new ConfigurableInfo(description + $"  DEFAULT - {defaultVal}", configAccept, auto, tags:
            [
                configName,
            ]));
        }
        public ModOptions()
        {
            Instance = this;
            EnableVisualisers = Bind("EnableVisualisers", true, "Shows values of color config sliders (Based on color space).", "Enable Visualisers?");
            DecimalCount = Bind("DecimalCount", 2, "Shows the number of decimal places in color config slider values.", "How many decimal places?", new IntAcceptable(max: 15));
            SliderRounding = Bind("SliderVisualRounding", MidpointRounding.AwayFromZero, "Changes how values of color config sliders are rounded.", "Rounding Type?");
            EnableSlugcatDisplay = Bind("enableSlugcatDisplay", true, "Shows slugcat display on story menu.", "Enable Slugcat Display?");
            IntToFloatColorValues = Bind("Int2FloatColorValues", true, "Adds decimals to hue and RGB values to sliders, with 'Enable Visualisers?' on.", "More accurate slider color values?");
            RemoveHSLSliders = Bind("RemoveHSLSliders", false, "Removes HSL Sliders in story and jolly-coop menu, will remove if other sliders are turned on.", "Remove HSL Sliders?");
            EnableRGBSliders = Bind("EnableRGBSliders", true, "Enables RGB Sliders in story and jolly-coop menu.", "Enable RGB Sliders?");
            EnableHSVSliders = Bind("EnableHSVSliders", false, "Enables HSV Sliders in story and jolly-coop menu.", "Enable HSV Sliders?");
            CopyPasteForSliders = Bind("CopyPasteForSliders", false, "Adds copy paste support to color sliders, is experimental and changes slider value based on color space<LINE>RGB follows 255, Hue in HSL follows 360.", "Copy Paste support for color sliders?");
            DisableHueSliderMaxClamp = Bind("DisableHueSliderMaxValue", false, "An experiemental feature that increases the hue slider max value you can set to 100% instead of 99% in story menu and expedition menu (if Enable Config for Expedition? is on)\nCAUTION as if you disable the mod after you have saved hue to 100%, it may make your saved color grey , due to rw's hue conversion. (This mod tries to convert back whenever the color config closes)", "Disable Hue Slider max slider value?");

            EnableHexCodeTypers = Bind("EnableHexCodeTypers", true, "Enables HexCode Typers in story and jolly-coop menu.", "Enable HexCode Typers?");
            EnableExpeditionColorConfig = Bind("EnableExpeditionColorConfig", false, "Experiemental, Adds custom color config to expedition.", "Enable Config for Expedition?");

            EnableColorPickers = Bind("EnableColorPickers", false, "Enables Color Pickers, will remove hex code typers since its unneccesary.", "Enable Color Pickers?");
            EnableBetterOPColorPicker = Bind("EnableBetterOPColorPicker", true, "Makes OP-ColorPicker selectors a bit less annoying.", "Enable less annoying OP-ColorPickers?");
            EnableDiffOpColorPickerHSL = Bind("EnableDifferentOPColorPicker", true, "Changes HueSat Square Picker in HSL/HSV Mode to SatLit Square picker and vice versa.", "Enable Different OP-ColorPickers?");
            HSL2HSVOPColorPicker = Bind("HSL2HSVOPColorPicker", true, "Replaces OP-ColorPickers' HSL mode to HSV mode.", "Enable HSV OP-ColorPickers?");
            EnableRotatingOPColorPicker = Bind("EnableRotatingOPColorPicker", true, "Allows OP-ColorPickers to rotate between custom modes. Press on the HSL/HSV Button in HSL/HSV mode to change modes if respective options are on. <LINE>'Less annoying OP-ColorPickers' will make toggled HSL mode ignore 'Different OP-ColorPickers'", "Switch modes for OP-ColorPickers?");
            CopyPasteForColorPickerNumbers = Bind("CopyPatseOPColorPickerNumbers", false, "Allows copy paste for Color Picker numbers.", "Copy Paste OP-ColorPicker Numbers?");

            EnableLegacyIdeaSlugcatDisplay = Bind("EnableLegacySlugcatDisplay", true, "Makes slugcat display in story menu to appear when the custom color checkbox is checked\ninstead of when configuring colors.", "Enable Original Idea Slugcat Display?");
            EnableLegacySSMSliders = Bind("enableLegacyVersionSliders", false, "Overrides color sliders with a replica of the early versions in story menu.", "Enable Legacy Sliders?");
            EnableLegacyHexCodeTypers = Bind("EnableLegacyHexCodeTypes", false, "Overrides hex code typers with a slight replica of what I had planned at the start of this mod.", "Enable Legacy HexCode Typers?");
            EnableRainMeadowArenaConfigExtension = Bind("EnableRainMeadowArenaConfigExtension", true, "Adds hex typers, rgb sliders (anything related to adjust custom color config in story/jolly) to RainMeadow Arena Color Config.", "Add Custom Configs for RainMeadow Arena?");
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
            PreAddRect(260);
            AddCheckBoxLabel(EnableVisualisers);
            AddIntDragger(DecimalCount);
            AddEnumList(SliderRounding);
            AddCheckBoxLabel(IntToFloatColorValues);
            AddCheckBoxLabel(EnableSlugcatDisplay);
            AddPendingRect();

            //sliders
            AddHeader("Sliders");
            PreAddRect(260);
            AddCheckBoxLabel(RemoveHSLSliders);
            AddCheckBoxLabel(EnableRGBSliders);
            AddCheckBoxLabel(EnableHSVSliders);
            AddCheckBoxLabel(CopyPasteForSliders);
            AddPendingRect(-10, false);
            //AddCheckBoxLabel(DisableHueSliderMaxClamp);

            //RIGHT SIDE
            ResetControl(325);
            //Utility
            AddHeader("Utility", false);
            PreAddRect(265);
            AddCheckBoxLabel(EnableHexCodeTypers);
            AddCheckBoxLabel(EnableExpeditionColorConfig);
            AddPendingRect();

            //OPColorPicker
            AddHeader("Color Pickers");
            PreAddRect(265);
            AddCheckBoxLabel(EnableBetterOPColorPicker);
            AddCheckBoxLabel(EnableDiffOpColorPickerHSL);
            AddCheckBoxLabel(HSL2HSVOPColorPicker);
            AddCheckBoxLabel(EnableRotatingOPColorPicker);
            AddCheckBoxLabel(CopyPasteForColorPickerNumbers);
            AddPendingRect();

            //Legacy
            AddHeader("Legacy");
            PreAddRect(265);
            AddCheckBoxLabel(EnableLegacyIdeaSlugcatDisplay);
            AddCheckBoxLabel(EnableLegacySSMSliders);
            AddCheckBoxLabel(EnableLegacyHexCodeTypers);
            AddPendingRect(nextHeaderSpacing: false);
            DrawAllPendingUI(ref Tabs[0]);

            //NEWTAB LEFT
            ResetControl();
            AddHeader("Mod Support", false);
            PreAddRect(320);
            AddCheckBoxLabel(EnableRainMeadowArenaConfigExtension);
            AddCheckBoxLabel(LukkyRGBJollySliderMeansBusiness);
            AddPendingRect(nextHeaderSpacing: false);
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
            lastHeaderY = control.y;
        }
        public void AddCheckBoxLabel(Configurable<bool> config)
        {
            OpCheckBox checkBox = new(config, control)
            {
                description = Custom.ReplaceLineDelimeters(Translate(config.info.description)),
            };
            OpLabel label = new(new(control.x + sBoxLXOffset, control.y), new(60, 25), Translate((string)config.info.Tags[0]), FLabelAlignment.Left);
            AddPendingUI(checkBox, label);
            control.y -= 40;
        }
        public void AddIntDragger(Configurable<int> config)
        {
            OpDragger dragger = new(config, control)
            {
                description = Custom.ReplaceLineDelimeters(Translate(config.info.description)),
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
                description = Custom.ReplaceLineDelimeters(Translate(config.info.description)),
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
                description = Custom.ReplaceLineDelimeters(Translate(config.info.description)),
            };
            OpLabel label = new(new(control.x + width + 10, control.y), new(60, 25), Translate((string)config.info.Tags[0]), FLabelAlignment.Left);
            AddPendingUI(resource, label);
            control.y -= followListSize ? (20 * Mathf.Clamp(resource._itemList.Length, 1, resource._listHeight) + 50) : 40;
        }
        public void PreAddRect(float sizeX, float posXOffset = 0, float posYOffset = 0, float alpha = 0.3f)
        {
            OpRect opRect = new(new(control.x - 10 + posXOffset, control.y + 35 + posYOffset), new(sizeX, 0), alpha);
            pendingUiElements.Add(opRect);
            opRects.Enqueue(opRect);
        }
        public void AddPendingRect(float bottomPosYOffset = 0,bool nextHeaderSpacing = true)
        {
            OpRect opRect = opRects.Dequeue();
            float newPosY = control.y + 30;
            newPosY += bottomPosYOffset;
            if (nextHeaderSpacing)
                newPosY -= 10;
            float newSizeY = opRect.PosY - newPosY;
            opRect.PosY = newPosY;
            opRect.size = new(opRect.size.x, newSizeY);
        }
        public void AddColorPicker(Configurable<Color> config)
        {
            control.y -= 125;
            OpColorPicker colorPicker = new(config, control)
            {
                description = Custom.ReplaceLineDelimeters(Translate(config.info.description)),
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
        public static bool ShowVisual => Instance.EnableVisualisers.Value;
        public static bool PickerHSVMode => Instance.HSL2HSVOPColorPicker.Value;
        public static bool ShouldAddSSMLegacySliders => Instance.EnableLegacySSMSliders.Value && AllSlidersAdded.Count(x => x == true) > 1;
        public static bool ShouldRemoveHSLSliders => Instance.RemoveHSLSliders.Value && OtherCustomSlidersAdded.Any(x => x == true);
        public static bool EnableJollyRGBSliders => Instance.EnableRGBSliders.Value || FollowLukkyRGBSliders;
        public static bool FollowLukkyRGBSliders => (ColorConfigMod.IsLukkyRGBColorSliderModOn && Instance.LukkyRGBJollySliderMeansBusiness.Value);
        public static int DeCount => Instance.DecimalCount.Value;
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
            public override bool IsValid(object value) => value.GetType() == typeof(T);
            public override object Clamp(object value) => IsValid(value) ? ClampT((T)value)! : value;
            public abstract T ClampT(T value);
        }
    }
}
