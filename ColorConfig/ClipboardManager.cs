using ColorConfig.MenuUI;
using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ColorConfig
{
    public static class ClipboardManager
    {
        public static string? Clipboard
        {
            get => GUIUtility.systemCopyBuffer; set => GUIUtility.systemCopyBuffer = value;
        }
        public static CopyPasteInput GetCopyPasteInput()
        {

            bool ctrl = SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX ? (Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)) :
                (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)),
                cpy = ctrl && Input.GetKey(KeyCode.C),
                pst = ctrl && Input.GetKey(KeyCode.V);
            return new(cpy, pst);
        }
        public static bool CopyShortcutPressed(this object? inputHolder) => inputHolder != null && inputHolder.GetInputExtras().cpyPsteInput.cpy && !inputHolder.GetInputExtras().lastCpyPsteInput.cpy;
        public static bool PasteShortcutPressed(this object? inputHolder) => inputHolder != null && inputHolder.GetInputExtras().cpyPsteInput.pste && !inputHolder.GetInputExtras().lastCpyPsteInput.pste;
    }
}
