using ColorConfig.ColorPresets;
using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorConfig.MenuUI
{
    public class ColorPresetDialog : Dialog
    {
        public RoundedRect colorPresetDescriptionRect;
        public ColorPresetDialog(ProcessManager manager) : base(manager)
        {
            ColorPresetManager.Load();
            darkSprite.alpha = 0.7f;
            float middleX = manager.rainWorld.screenSize.x * 0.5f, middleY = manager.rainWorld.screenSize.y * 0.5f,
                sizeX = manager.rainWorld.screenSize.x * 0.8f,
                sizeY = manager.rainWorld.screenSize.y * 0.65f;
            roundedRect = new(this, pages[0], new(middleX - (sizeX * 0.5f), middleY - (sizeY * 0.5f)), new(sizeX * 0.6f, sizeY), true);
            colorPresetDescriptionRect = new(this, pages[0], new(roundedRect.pos.x + roundedRect.size.x + 10, roundedRect.pos.y), new((sizeX * 0.4f) - 10, sizeY), true);
        }
    }
}
