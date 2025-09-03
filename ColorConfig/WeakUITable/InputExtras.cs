using ColorConfig.MenuUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorConfig.WeakUITable
{
    public class InputExtras
    {
        public void UpdateInputs()
        {
            lastFixedInput = fixedInput;
            lastCpyPsteInput = cpyPsteInput;

            fixedInput = SmallUtils.FixedPlayerUIInput(-1);
            cpyPsteInput = ClipboardManager.GetCopyPasteInput();
        }
        public Player.InputPackage fixedInput = new(), lastFixedInput = new();
        public CopyPasteInput cpyPsteInput = new(), lastCpyPsteInput = new();
    }
}
