global using Vector2 = UnityEngine.Vector2;
global using Vector3 = UnityEngine.Vector3;
global using Color = UnityEngine.Color;
global using OpCodes = Mono.Cecil.Cil.OpCodes;
using System;
using System.Linq;
using System.Security.Permissions;
using System.Security;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx;
using Menu.Remix.MixedUI;
using MonoMod.Cil;
using BepInEx.Logging;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using ColorConfig.WeakUITable;
using ColorConfig.Hooks;
using ColorConfig.MenuUI.Objects;
#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ColorConfig
{
    [BepInPlugin(id, modName, version)]
    public sealed class ColorConfigMod : BaseUnityPlugin
    {
        public const string id = "dusty.colorconfig", modName = "Extended Color Config", version = "1.3.8";
        public static readonly ConditionalWeakTable<object, InputExtras> inputExtras = new();
        public static readonly ConditionalWeakTable<object, ExtraSSMInterfaces> extraSSMInterfaces = new();
        public static readonly ConditionalWeakTable<object, ExtraExpeditionInterfaces> extraEXPInterfaces = new();
        public static readonly ConditionalWeakTable<JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider, JollyCoopOOOConfig> extraJollyInterfaces = new();
        public static readonly ConditionalWeakTable<OpColorPicker, ColorPickerExtras> extraColorPickerStuff = new();
        public static bool IsLukkyRGBColorSliderModOn { get; private set; }
        public static bool IsRainMeadowOn { get; private set; }
        public static bool IsBingoOn { get; private set; }
        public static new ManualLogSource Logger { get; private set; }
        private bool IsApplied { get; set; } = false;
        private static readonly bool shouldEnableCursorDebug = false;
        public void OnEnable()
        {
            Logger = base.Logger;
            Logger.LogMessage("Getting ready to add new color configs!");;
            On.RainWorld.OnModsInit += On_RainWorld_OnModsInit;
            Logger.LogMessage("Beep boop beep!");
        }
        public void On_RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            if (!IsApplied)
            {
                IsApplied = true;
                try
                {
                    CheckIfModsOn();
                    MachineConnector.SetRegisteredOI("dusty.colorconfig", new ModOptions());
                    Futile.atlasManager.LoadAtlas("atlases/colorconfig_symbols");
                    ColorConfigHooks.Init();
                    DebugLog("OnModsinit Success");
                }
                catch (Exception ex)
                {
                    DebugException("Oopsies, error occured", ex);
                }
            }
        }
        public void CheckIfModsOn()
        {
            IsLukkyRGBColorSliderModOn = CheckIfModOn("vultumast.rgbslider");
            IsRainMeadowOn = CheckIfModOn("henpemaz_rainmeadow");
            IsBingoOn = CheckIfModOn("nacu.bingomode");
        }
        public bool CheckIfModOn(string id) => ModManager.ActiveMods.Any(x => x.id == id);
        public static string GetMethodName(int skipframes = 2, bool getAssembly = false)
        {
            StackFrame frame = new(skipframes, true);
            MethodBase method = frame.GetMethod();
            return $"{(getAssembly ? method.DeclaringType.FullName : method.DeclaringType.Name)}.{method.Name}";
        }
        public static void DebugLog(object message) => Logger.LogInfo($"{GetMethodName()}: {message}");
        public static void DebugWarning(object message) => Logger.LogWarning($"{GetMethodName()}: {message}");
        public static void DebugError(object message) => Logger.LogError($"{GetMethodName()}: {message}");
        public static void DebugException(object message, Exception ex)
        {
            Logger.LogError($"{GetMethodName()}: {message}");
            Logger.LogError(ex);
        }
        public static void DebugILCursor(ILCursor cursor, string message = "")
        {
            Logger.LogInfo($"{GetMethodName()}: {message}{(shouldEnableCursorDebug ?
                $"{cursor}" : $"Index: {cursor.Index}")}");
        }
    }


}
