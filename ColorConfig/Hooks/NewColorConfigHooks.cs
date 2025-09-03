using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Menu;
using Menu.Remix.MixedUI;
using JollyCoop.JollyMenu;
using RWCustom;
using UnityEngine;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ColorConfig.MenuUI;
using ColorConfig.WeakUITable;
namespace ColorConfig.Hooks
{
    public static partial class ColorConfigHooks
    {
        public static void Init()
        {
            Menu_Hooks();
            SlugcatSelectMenu_Hooks();
            ExpeditionMenu_Hooks();
            JollyCoopMenu_Hooks();
            OpColorPicker_Hooks();
            ExternalModHooks();
        }
       
    }
}
