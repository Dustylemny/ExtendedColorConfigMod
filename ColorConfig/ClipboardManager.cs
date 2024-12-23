using Menu.Remix;
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
        public static string Clipboard
        { 
            get
            {
                return GUIUnityClipboard;
            }
            set
            {
                GUIUnityClipboard = value;
            }
        }
        public static string GUIUnityClipboard
        {
            get => GUIUtility.systemCopyBuffer;
            set => GUIUtility.systemCopyBuffer = value;
        }

    }
}
