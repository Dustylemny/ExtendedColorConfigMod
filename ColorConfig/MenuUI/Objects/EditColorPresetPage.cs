using ColorConfig.ColorPresets;
using Menu;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorConfig.MenuUI.Objects
{
    public class EditColorPresetPage : PositionedMenuObject
    {
        public MenuLabel? colorPresetNameLabel;
        public MenuLabel colorPresetNotSelectedLabel;
        public SimpleButton? renameColorPresetButton, deleteColorPresetButton;
        public string viewingColorSlots = "";
        public ColorPreset? selectedColorPreset;
        public EditColorPresetPage(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos)
        {
            colorPresetNotSelectedLabel = new(menu, this, Custom.ReplaceWordWrapLineDelimeters(menu.Translate("(A color preset has not been selected)")), new(0, 0), size, false);
            subObjects.Add(colorPresetNotSelectedLabel);
        }
        public void RefreshUI()
        {
            if (selectedColorPreset == null)
            {
                colorPresetNotSelectedLabel.label.alpha = 1;
                return;
            }
            colorPresetNotSelectedLabel.label.alpha = 0;
        }
        public void SelectColorPreset(ColorPreset colorPreset)
        {
            if (colorPreset == null || colorPreset == selectedColorPreset) return;
            selectedColorPreset = colorPreset;
            colorPresetNotSelectedLabel.label.alpha = 0;

        }

    }
}
