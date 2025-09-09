using ColorConfig.ColorPresets;
using HarmonyLib;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ColorConfig.MenuUI.Objects
{
    public class SelectColorPresetPage : PositionedMenuObject
    {
        public Vector2 colorPresetsDividerPos;
        public MenuTabWrapper tabWrapper;
        public UIelementWrapper elementWrapper;
        public OpScrollBox colorPresetsScrollBox;
        public MenuLabel colorPresetsLabel;
        public FSprite colorPresetsLabelDivider;
        public SimpleButton backButton, addPresetButton;
        public SelectColorPresetPage(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos)
        {
            colorPresetsLabel = new(menu, this, menu.Translate("Color Presets"), new(0, size.y - 40), new(size.x, 40), true);
            Container.AddChild(colorPresetsLabelDivider = new("pixel")
            {
                anchorX = 0,
                scaleX = colorPresetsLabel.label.textRect.x + 60,
                scaleY = 2,
            });
            colorPresetsDividerPos = new(size.x - colorPresetsLabelDivider.scaleX * 0.5f, size.y - 35);
            backButton = new(menu, this, menu.Translate("BACK"), "BACK", new(0, -40), new(80, 30));
            addPresetButton = new(menu, this, menu.Translate("ADD PRESET"), "ADDPRESET", new(size.x - 130, 10), new(120, 30));
            tabWrapper = new(menu, this);
            float scrollBoxVisibleSizeY = 100 * 3;
            elementWrapper = new(tabWrapper, colorPresetsScrollBox = new(new(10, size.y * 0.5f - scrollBoxVisibleSizeY * 0.5f), new(size.x - 20, scrollBoxVisibleSizeY), 80 * ColorPresetManager.colorPresets.Count));
            subObjects.AddRange([colorPresetsLabel, backButton, addPresetButton, tabWrapper]);
            PopulateScrollBox();
        }
        public void PopulateScrollBox()
        {
            UIelement[] uielements = [..colorPresetsScrollBox.items];
            OpScrollBox.RemoveItemsFromScrollBox(uielements);
            uielements.Do(x => { x.Deactivate(); x.Unload(); x.wrapper.tabWrapper.ClearMenuObject(x.wrapper); x.wrapper.tabWrapper.wrappers.Remove(x);});
            colorPresetsScrollBox.SetContentSize(100 * (ColorPresetManager.colorPresets.Count / 3), true);
            float sizeX = (colorPresetsScrollBox.size.x - colorPresetsScrollBox._SliderSize.x - 10) / 3; //spacing must be 5
            for (int i = 0; i < ColorPresetManager.colorPresets.Count; i++)
            {
                ColorPreset colorPreset = ColorPresetManager.colorPresets[i];
                int indexInColumn = i % 3;
                Vector2 pos = new(indexInColumn * sizeX, colorPresetsScrollBox.contentSize - 100 * ((i / 3) + 1));
                OpSimpleButton btn = new(pos, new(sizeX, 100), colorPreset.presetName);
                btn.OnClick += tri => OnPresetButtonClick(btn, colorPreset);
                colorPresetsScrollBox.AddItemToWrapped(btn);

            }
        }
        public void OnPresetButtonClick(OpSimpleButton btn,ColorPreset colorPreset)
        {
            (menu as ColorPresetDialog)?.editColorPresetPage.SelectColorPreset(colorPreset);
        }
        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == "ADDPRESET")
            {
                ColorPresetManager.colorPresets.Add(new($"Preset {ColorPresetManager.colorPresets.Count + 1}"));
                PopulateScrollBox();
            }
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            colorPresetsLabelDivider.x = DrawX(timeStacker) + colorPresetsDividerPos.x;
            colorPresetsLabelDivider.y = DrawY(timeStacker) + colorPresetsDividerPos.y;
        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            colorPresetsLabelDivider.RemoveFromContainer();
        }
    }
}
