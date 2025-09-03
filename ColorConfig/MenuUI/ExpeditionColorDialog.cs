using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorConfig.MenuUI.Objects;
using Menu;
using RWCustom;
using UnityEngine;

namespace ColorConfig.MenuUI
{
    public class ExpeditionColorDialog : DialogNotify, CheckBox.IOwnCheckBox
    {
        //if remix is on
        public List<SliderIDGroup> SliderIDGroups
        {
            get
            {
                List<SliderIDGroup> idGroup = [];
                SmallUtils.AddSSMSliderIDGroups(idGroup, ModOptions.ShouldRemoveHSLSliders);
                return idGroup;
            }
        }
        public float SizeXOfDefaultCol => CurrLang == InGameTranslator.LanguageID.Japanese || CurrLang == InGameTranslator.LanguageID.French ? 110f : CurrLang == InGameTranslator.LanguageID.Italian || CurrLang == InGameTranslator.LanguageID.Spanish ? 180 : 110;
        public ExpeditionColorDialog(Menu.Menu translator, SlugcatStats.Name name, Action action, bool? openHexInterface = null, bool? clampHue = null, bool? showSlugcatDisplay = null) : base("", translator.Translate("Custom colors"), new(500, 400), translator.manager, action)
        {
            shouldClampHue = clampHue.GetValueOrDefault(!ModOptions.Instance.DisableHueSliderMaxClamp.Value);
            openHex = openHexInterface.GetValueOrDefault(ModOptions.Instance.EnableHexCodeTypers.Value);
            showDisplay = showSlugcatDisplay.GetValueOrDefault(ModOptions.Instance.EnableSlugcatDisplay.Value);
            id = name;
            sliderSize = new(200, 30);
            offset = new(0, -40);
            colorChooser = -1;
            GetSaveColorEnabled();
            colorCheckbox = new(this, pages[0], this, new(size.x + 40, size.y + offset.y * 5), 0, "", colorCheckboxSingal);
            pages[0].subObjects.Add(colorCheckbox);
            this.MutualMenuObjectBind(colorCheckbox, okButton, true);
        }
        public override void ShutDownProcess()
        {
            base.ShutDownProcess();
        }
        public override void Update()
        {
            base.Update();
            hexTypeBox?.SaveNewHSL(ExpeditionHSL());
        }
        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == "DEFAULTCOL" && bodyInterface != null)
            {
                PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                manager.rainWorld.progression.miscProgressionData.colorChoices[id.value][colorChooser] = bodyInterface.defaultColors[colorChooser];
            }
            if (message.StartsWith(ExpeditionColorInterface.OPENINTERFACESINGAL) && int.TryParse(message.Substring(ExpeditionColorInterface.OPENINTERFACESINGAL.Length), NumberStyles.Any, CultureInfo.InvariantCulture, out int result))
                AddOrRemoveColorInterface(result);
        }
        public override void SliderSetValue(Slider slider, float f) => MenuToolObj.CustomSliderSetHSL(slider, f, ExpeditionHSL(), SaveHSLString, shouldClampHue);
        public override float ValueOfSlider(Slider slider) => MenuToolObj.CustomHSLValueOfSlider(slider, ExpeditionHSL(), out float f) ? f : 0;
        public bool GetChecked(CheckBox box) => colorChecked;
        public void SetChecked(CheckBox box, bool c) => SaveColorChoicesEnabled(c);
        public void GetSaveColorEnabled()
        {
            manager.rainWorld.progression.miscProgressionData.colorChoices ??= [];
            manager.rainWorld.progression.miscProgressionData.colorsEnabled ??= [];
            if (!manager.rainWorld.progression.miscProgressionData.colorsEnabled.ContainsKey(id.value))
            {
                manager.rainWorld.progression.miscProgressionData.colorsEnabled.Add(id.value, colorChecked);
                return;
            }
            colorChecked = manager.rainWorld.progression.miscProgressionData.colorsEnabled[id.value];
            ((Action?)(colorChecked ? AddColorButtons : null))?.Invoke();
        }
        public void SaveColorChoicesEnabled(bool colorChecked)
        {
            manager.rainWorld.progression.miscProgressionData.colorChoices ??= [];
            manager.rainWorld.progression.miscProgressionData.colorsEnabled ??= [];
            if (!manager.rainWorld.progression.miscProgressionData.colorsEnabled.ContainsKey(id.value))
            {
                manager.rainWorld.progression.miscProgressionData.colorsEnabled.Add(id.value, colorChecked);
            }
            else
            {
                manager.rainWorld.progression.miscProgressionData.colorsEnabled[id.value] = colorChecked;
            }
            this.colorChecked = colorChecked;
            ((Action)(colorChecked ? AddColorButtons : RemoveColorButtons)).Invoke();
        }
        public Vector3 ExpeditionHSL() => this.MenuHSL(id, colorChooser);
        public void SaveHSLString(Vector3 hsl) => this.SaveHSLString_Menu_Vector3(id, colorChooser, hsl);
        public void AddOrRemoveColorInterface(int num)
        {
            if (num == colorChooser)
            {
                RemoveColorInterface();
                PlaySound(SoundID.MENU_Remove_Level);
                return;
            }
            colorChooser = num;
            AddColorInterface();
            PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
        }
        public void AddColorButtons()
        {
            if (bodyInterface != null) return;

            bodyInterface = GetColorInterface(id, new(size.x, size.y + 90));
            pages[0].subObjects.Add(bodyInterface);
        }
        public void AddColorInterface()
        {
            if (sliders == null)
                pages[0].subObjects.Add(sliders = new(this, pages[0], new(size.x, size.y - 10), offset, sliderSize, SliderIDGroups, new(0, 30)));
            if (defaultColor == null)
                pages[0].subObjects.Add(defaultColor = new(this, pages[0], Translate("Restore Default"), "DEFAULTCOL", size + sliders.sliderOOO.pos + offset + new Vector2(0, sliders.oOOPages?.PagesOn == true ? -40 : 0), new(SizeXOfDefaultCol, 30)));
            if (hexTypeBox == null && openHex)
            {
                hexTypeBox = new(this, pages[0], defaultColor.pos + new Vector2(defaultColor.size.x + 10, 0));
                hexTypeBox.OnSaveNewTypedColor += (hex, hsl, rgb) =>
                {
                    SaveHSLString(hex.newPendingHSL);
                    if (sliders == null) return;

                    sliders.sliderO.UpdateSliderValue();
                    sliders.sliderOO.UpdateSliderValue();
                    sliders.sliderOOO.UpdateSliderValue();
                };
                pages[0].subObjects.Add(hexTypeBox);
            }
            for (int i = 0; i < bodyInterface?.bodyButtons?.Length; i++)
                bodyInterface.bodyButtons[i].MenuObjectBind(sliders.sliderO, bottom: true);

            this.MutualMenuObjectBind(sliders.sliderO, bodyInterface?.bodyButtons?.FirstOrDefault(), bottomTop: true);
            MutualVerticalButtonBind(colorCheckbox, defaultColor);
            this.MutualMenuObjectBind(okButton, hexTypeBox != null ? hexTypeBox.elementWrapper : defaultColor, bottomTop: true);
            if (sliders.oOOPages?.PagesOn == true)
            {
                MutualVerticalButtonBind(defaultColor, sliders.oOOPages.prevButton);
                MutualVerticalButtonBind(defaultColor, sliders.oOOPages.nextButton);
                return;
            }
            MutualVerticalButtonBind(defaultColor, sliders.sliderOOO);

        }
        public void RemoveColorButtons()
        {
            RemoveColorInterface();
            pages[0].ClearMenuObject(ref bodyInterface);
        }
        public void RemoveColorInterface()
        {
            this.TryFixColorChoices(id);
            for (int i = 0; i < bodyInterface?.bodyButtons?.Length; i++)
            {
                if (i == 0)
                {
                    this.MutualMenuObjectBind(colorCheckbox, bodyInterface.bodyButtons[i], bottomTop: true);
                    continue;
                }
                bodyInterface.bodyButtons[i].MenuObjectBind(colorCheckbox, bottom: true);
            }
            pages[0].ClearMenuObject(ref sliders);
            pages[0].ClearMenuObject(ref defaultColor);
            pages[0].ClearMenuObject(ref hexTypeBox);
            colorChooser = -1;
        }
        public ExpeditionColorInterface GetColorInterface(SlugcatStats.Name slugcatID, Vector2 pos)
        {
            List<string> names = PlayerGraphics.ColoredBodyPartList(slugcatID);
            List<string> list = PlayerGraphics.DefaultBodyPartColorHex(slugcatID);
            for (int i = 0; i < list.Count; i++)
            {
                Vector3 hsl = Custom.RGB2HSL(Custom.hexToColor(list[i]));
                list[i] = SmallUtils.SetHSLSaveString(hsl);
            }
            return new ExpeditionColorInterface(this, pages[0], pos, slugcatID, names, list, showDisplay);
        }
        public const string colorCheckboxSingal = "COLORCHECKED";
        public int colorChooser;
        public bool colorChecked, shouldClampHue, openHex, showDisplay;
        public Vector2 sliderSize = new(200, 30), offset = new(0, -40);
        public CheckBox colorCheckbox;
        public SlugcatStats.Name id;
        public OOOSliders? sliders;
        public SimpleButton? defaultColor;
        public HexTypeBox? hexTypeBox;
        public ExpeditionColorInterface? bodyInterface;
    }
}
