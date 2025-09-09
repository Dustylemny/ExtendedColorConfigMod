using ColorConfig.ColorPresets;
using ColorConfig.MenuUI.Objects;
using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ColorConfig.MenuUI
{
    public class ColorPresetDialog : Dialog
    {
        public ColorPreset? selectedColorPreset;
        public SelectColorPresetPage colorPresetsPage;
        public EditColorPresetPage editColorPresetPage;
        public RoundedRect colorPresetDescriptionRect;
        public ColorPresetDialog(ProcessManager manager) : base(manager)
        {
            ColorPresetManager.Load();
            darkSprite.alpha = 0.7f;
            float middleX = manager.rainWorld.screenSize.x * 0.5f, middleY = manager.rainWorld.screenSize.y * 0.5f,
                sizeX = manager.rainWorld.screenSize.x * 0.6f,
                sizeY = manager.rainWorld.screenSize.y * 0.7f;
            float sizeOfSelect = sizeX * 0.65f, sizeOfEdit = sizeX * 0.35f, totalSize = sizeOfSelect + sizeOfEdit + 20;
            pages[0].subObjects.Add(roundedRect = new(this, pages[0], new(middleX - totalSize * 0.5f, middleY - sizeY * 0.5f), new(sizeOfSelect, sizeY), true));
            pages[0].subObjects.Add(colorPresetDescriptionRect = new(this, pages[0], new(roundedRect.pos.x + roundedRect.size.x + 20, roundedRect.pos.y), new(sizeOfEdit, sizeY), true));


            roundedRect.subObjects.Add(colorPresetsPage = new(this, roundedRect, new(0, 0), roundedRect.size));
            colorPresetDescriptionRect.subObjects.Add(editColorPresetPage = new(this, colorPresetDescriptionRect, new(0, 0), colorPresetDescriptionRect.size));
        }
        public void GetNewPreset(ColorPreset newColorPreset)
        {
            if (newColorPreset == selectedColorPreset) return;
        }
        public override void ShutDownProcess()
        {
            base.ShutDownProcess();
            ColorPresetManager.Save();
        }
        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == "BACK")
            {
                PlaySound(SoundID.MENU_Remove_Level);
                manager.StopSideProcess(this);
            }
        }

    }
}
