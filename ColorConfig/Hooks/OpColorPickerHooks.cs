using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;
using MonoMod.Cil;

namespace ColorConfig.Hooks
{
    public static class OPColor
    {
        public static void OpColorPicker_Hooks()
        {
            try
            {
                OtherOpColorPickerHooks();
                IL.Menu.Remix.MixedUI.OpColorPicker.Change += IL_OPColorPicker_Change;
                IL.Menu.Remix.MixedUI.OpColorPicker._HSLSetValue += IL_OPColorPicker__HSLSetValue;
                IL.Menu.Remix.MixedUI.OpColorPicker.MouseModeUpdate += IL_OPColorPicker__MouseModeUpdate;
                IL.Menu.Remix.MixedUI.OpColorPicker._NonMouseModeUpdate += IL_OPColorPicker__NonMouseModeUpdate;
                IL.Menu.Remix.MixedUI.OpColorPicker.GrafUpdate += IL_OPColorPicker_GrafUpdate;
                On.Menu.Remix.MixedUI.OpColorPicker.ctor += On_OPColorPicker_Ctor;
                On.Menu.Remix.MixedUI.OpColorPicker._MouseTrySwitchMode += On_OPColorPicker__MouseTrySwitchMode;
                On.Menu.Remix.MixedUI.OpColorPicker._RecalculateTexture += On_OPColorPickerRecalculateTexture;
                On.Menu.Remix.MixedUI.OpColorPicker.Update += On_OPColorPicker_Update;
                On.Menu.Remix.MixedUI.OpColorPicker.GrafUpdate += On_OPColorPicker_GrafUpdate;
                ColorConfigMod.DebugLog("Successfully extended color interface for OpColorPicker!");
            }
            catch (Exception ex)
            {
                ColorConfigMod.DebugException("Failed to initialize colorpicker hooks", ex);
            }
        }
        public static void OtherOpColorPickerHooks()
        {
            typeof(OpColorPicker).GetProperty(nameof(OpColorPicker.@value)).GetSetMethod().ILHookMethod(IL_OPColorPicker_set_value);
            typeof(OpColorPicker).GetProperty(nameof(OpColorPicker.@value)).GetSetMethod().HookMethod(delegate (Action<OpColorPicker, string> orig, OpColorPicker self, string newValue)
            {
                if ((ModOptions.Instance.EnableBetterOPColorPicker.Value || ModOptions.Instance.EnableDiffOpColorPickerHSL.Value) && self._mode == OpColorPicker.PickerMode.HSL)
                    self.RefreshTexture();
                orig(self, newValue);
            });
        }
        public static void IL_OPColorPicker_set_value(ILContext il)
        {
            try
            {
                ILCursor cursor = new(il);
                cursor.TryGotoNext(MoveType.After, (x) => x.MatchCall<RXColor>("HSLFromColor"));
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate(delegate (RXColorHSL rXHSL, OpColorPicker self, string newVal)
                {
                    rXHSL = ModOptions.PickerHSVMode ? ColConversions.HSL2HSV(rXHSL.RXHSl2Vector3()).Vector32RXHSL() : rXHSL;
                    if (ModOptions.Instance.EnableBetterOPColorPicker.Value)
                    {
                        if (self._mode == OpColorPicker.PickerMode.HSL && (self._curFocus == OpColorPicker.MiniFocus.HSL_Hue || self._curFocus == OpColorPicker.MiniFocus.HSL_Saturation || self._curFocus == OpColorPicker.MiniFocus.HSL_Lightness))
                             rXHSL = self.GetHSL01().Vector32RXHSL();
                    }
                    return rXHSL;
                });
                ColorConfigMod.DebugLog("Patched RXHSLValues from hsv to hsl and that matches the values selected");
            }
            catch (Exception e)
            {
                ColorConfigMod.DebugException("Failed to patch set_value for hsv and better opcolor pickers", e);
            }
        }
        public static void IL_OPColorPicker_Change(ILContext il)
        {
            try
            {
                ILCursor cursor = new(il);
                cursor.GotoNext(MoveType.After, (x) => x.MatchCall(typeof(Custom), "HSL2RGB"));
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(delegate (Color hslRGB, OpColorPicker self)
                {
                    return self.IsHSVMode() ? ColConversions.HSV2RGB(self.GetHSL01()) : self._h == 100 ? ColConversions.HSL2RGB(self.GetHSL01()) : hslRGB;
                });
                ColorConfigMod.DebugLog("Sucessfully patched _cdis0 Color for HSV and rect hue selector!");
            }
            catch (Exception e)
            {
                ColorConfigMod.DebugException("Failed to patch _cdis0 Color for HSV and rect hue selector", e);
            }
        }
        public static void IL_OPColorPicker__HSLSetValue(ILContext il)
        {
            try
            {
                ILCursor cursor = new(il);
                cursor.GotoNext(MoveType.After, x => x.MatchCall(typeof(Custom), "HSL2RGB"));
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(delegate (Color hslCol, OpColorPicker self)
                {
                    return self.IsHSVMode() ? ColConversions.HSV2RGB(self.GetHSL01()) : ModOptions.Instance.EnableBetterOPColorPicker.Value && self._h == 100 ? ColConversions.HSL2RGB(self.GetHSL01()) : hslCol;
                });
                ColorConfigMod.DebugLog("Sucessfully patched RGB Color to take hsv or not turn grey!");
            }
            catch (Exception e)
            {
                ColorConfigMod.DebugException("Failed to patch RGB Color ", e);
            }
        }
        public static void IL_OPColorPicker__MouseModeUpdate(ILContext il)
        {
            try
            {
                ILCursor cursor = new(il);
                GotoHSL(cursor);
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc_3);
                cursor.Emit(OpCodes.Ldloc, 4);
                cursor.EmitDelegate(delegate (OpColorPicker self, int hueSat, int satLit)
                {
                    return self.OpColorPickerPatchMiniFocusHSLColor(hueSat, satLit, 0, true);
                });
                MakeAReturn(cursor);
                ColorConfigMod.DebugLog("Sucessfully patched MiniFocus HueSat to SatHue!");
                GotoHSL(cursor);
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc, 5);
                cursor.EmitDelegate(delegate (OpColorPicker self, int litHue)
                {
                    return self.OpColorPickerPatchMiniFocusHSLColor(0, 0, litHue, false);
                });
                ColorConfigMod.DebugLog("Sucessfully patched MiniFocus for Lit to Hue!");
                MakeAReturn(cursor);
                GotoHSL(cursor);//, false);
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc, 11);
                cursor.EmitDelegate(delegate (OpColorPicker self, int litHue)
                {
                    self.OpColorPickerPatchHoverMouseHSLColor(0, 0, litHue, false);
                });
                ColorConfigMod.DebugLog("Sucessfully patched HoverMouse for Lit to Hue!");
                cursor.GotoNext(x => x.MatchCall(typeof(Custom), "HSL2RGB"));
                cursor.GotoNext(MoveType.After, x => x.MatchCallvirt<FSprite>("set_color"));
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc, 12);
                cursor.Emit(OpCodes.Ldloc, 13);
                cursor.EmitDelegate(delegate (OpColorPicker self, int hueSat, int satLit)
                {
                    self.OpColorPickerPatchHoverMouseHSLColor(hueSat, satLit, 0, true);
                });
                ColorConfigMod.DebugLog("Sucessfully patched HoverMouse for HueSat to SatLit!");
            }
            catch (Exception ex)
            {
                ColorConfigMod.DebugException("Failed to patch for changing colors using mouse", ex);
            }

        }
        public static void IL_OPColorPicker__NonMouseModeUpdate(ILContext il)
        {
            try
            {
                ILCursor cursor = new(il);
                cursor.GotoNext(MoveType.After, x => x.MatchStloc(1));
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc_1);
                cursor.EmitDelegate(delegate (OpColorPicker self, OpColorPicker.PickerMode newMode)
                {
                    if (ModOptions.Instance.EnableRotatingOPColorPicker.Value && self._mode == OpColorPicker.PickerMode.HSL && newMode == self._mode && (ModOptions.PickerHSVMode || ModOptions.Instance.EnableDiffOpColorPickerHSL.Value))
                    {
                        self.SwitchHSLCustomMode();
                        ColorConfigMod.DebugLog("Sucessfully patched for switch mode using non-mouse");
                        return true;
                    }
                    return false;
                });
                MakeAReturn(cursor);
            }
            catch (Exception ex)
            {
                ColorConfigMod.DebugException("Failed to patch for switch mode using non-mouse", ex);
            }
        }
        public static void IL_OPColorPicker_GrafUpdate(ILContext il)
        {
            try
            {
                ILCursor cursor = new(il);
                cursor.GotoNext(x => x.Match(OpCodes.Sub), (x) => x.Match(OpCodes.Switch));
                cursor.GotoNext(x => x.MatchCall<UIelement>("get_MenuMouseMode"));
                cursor.GotoNext(MoveType.After, x => x.MatchCallvirt<FLabel>("set_color"));
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(delegate (OpColorPicker self)
                {
                    //hueSatToSatHueTextFix
                    if (ModOptions.Instance.EnableDiffOpColorPickerHSL.Value)
                    {
                        self._lblB.color = self._lblR.color;
                        self._lblR.color = self.colorText;
                    }
                });
                ColorConfigMod.DebugLog("Successfully patched hue2lit text color");
                cursor.GotoNext(x => x.MatchCallvirt<GlowGradient>("set_pos"));
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(delegate (Vector2 origFocus, OpColorPicker self)
                {
                    return ModOptions.Instance.EnableDiffOpColorPickerHSL.Value && self.MenuMouseMode ? new(104, 25) : origFocus;
                });
                ColorConfigMod.DebugLog("Successfully patched focus glow for hue2lit text");
                cursor.GotoNext(x => x.MatchLdarg(0), x => x.MatchLdfld<OpColorPicker>(nameof(OpColorPicker._lblB)));
                cursor.GotoNext(MoveType.After, x => x.MatchCallvirt<FLabel>("set_color"));
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(delegate (OpColorPicker self)
                {
                    if (ModOptions.Instance.EnableDiffOpColorPickerHSL.Value && self.MenuMouseMode)
                    {
                        self._lblR.color = self._lblB.color;
                        self._lblB.color = self.colorText;
                    }
                });
                ColorConfigMod.DebugLog("Successfully patched lit2hue text color");
                cursor.GotoNext((x) => x.MatchCallvirt<GlowGradient>("set_pos"));
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(delegate (Vector2 origFocus, OpColorPicker self)
                {
                    return ModOptions.Instance.EnableDiffOpColorPickerHSL.Value && self.MenuMouseMode ? new(104f, 105f) : origFocus;
                });
                ColorConfigMod.DebugLog("Successfully patched focus glow for lit2hue text");
            }
            catch (Exception ex)
            {
                ColorConfigMod.DebugException("Failed to fix focus glow pos and text color, not a big issue tho", ex);
            }
        }
        public static void On_OPColorPicker_Ctor(On.Menu.Remix.MixedUI.OpColorPicker.orig_ctor orig, OpColorPicker self, Configurable<Color> config, Vector2 pos)
        {
            self.GetColorPickerExtras()._IsHSVMode = ModOptions.PickerHSVMode;
            self.GetColorPickerExtras()._IsDifferentHSLHSVMode = ModOptions.Instance.EnableDiffOpColorPickerHSL.Value;
            orig(self, config, pos);
            self._lblHSL.text = self.IsHSVMode() ? "HSV" : self._lblHSL.text;
        }
        public static void On_OPColorPicker__MouseTrySwitchMode(On.Menu.Remix.MixedUI.OpColorPicker.orig__MouseTrySwitchMode orig, OpColorPicker self, OpColorPicker.PickerMode newMode)
        {
            if (ModOptions.Instance.EnableRotatingOPColorPicker.Value && self._mode == OpColorPicker.PickerMode.HSL && newMode == self._mode && (ModOptions.PickerHSVMode || ModOptions.Instance.EnableDiffOpColorPickerHSL.Value))
            {
                self.SwitchHSLCustomMode();
                return;
            }
            orig(self, newMode);
        }
        public static void On_OPColorPickerRecalculateTexture(On.Menu.Remix.MixedUI.OpColorPicker.orig__RecalculateTexture orig, OpColorPicker self)
        {
            bool diff = self.IsDiffHSLHSVMode();
            if (self._mode == OpColorPicker.PickerMode.HSL && (diff || self.IsHSVMode()))
            {
                self._ttre1 = new(diff ? 101 : 100, 101)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point
                };
                self._ttre2 = new(10, 101)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point
                };
                Vector3Int hsvHSL = self.GetHSVOrHSL100();
                for (int height = 0; height <= 100; height++)
                {
                    for (int width = 0; width < self._ttre1.width; width++)
                    {
                        float? h = diff ? null : width / 100f, s = diff ? width / 100f : height / 100f, l = diff ? height / 100f : null;
                        self._ttre1.SetPixel(width, height, self.ColorPicker2RGB().Invoke(self.ParseGetHSLORHSV01(h, s, l)));
                        if (width < self._ttre2.height)
                        {
                            h = diff ? height / 100f : null;
                            s = null;
                            l = diff ? null : height / 100f;
                            self._ttre2.SetPixel(width, height, self.ColorPicker2RGB().Invoke(self.ParseGetHSLORHSV01(h, s, l)));
                        }

                    }
                }
                //Arrows
                Color hueArrowCol = new(1 - self._r / 100f, 1f - self._g / 100f, 1f - self._b / 100f);
                hueArrowCol = Color.Lerp(Color.white, hueArrowCol, Mathf.Pow(Mathf.Abs(hueArrowCol.grayscale - 0.5f) * 2f, 0.3f));
                SmallUtils.ApplyHSLArrow(self._ttre1, diff ? hsvHSL.y : hsvHSL.x, hueArrowCol, false, diff ? hsvHSL.z : hsvHSL.y); //first leftright arrow, diff -> s, h
                SmallUtils.ApplyHSLArrow(self._ttre1, diff ? hsvHSL.z : hsvHSL.y, hueArrowCol, true, diff ? hsvHSL.y : hsvHSL.x); //second downright arrow square texture, diff -> l, s
                SmallUtils.ApplyHSLArrow(self._ttre2, diff ? hsvHSL.x : hsvHSL.z, hueArrowCol, true, 51); //last rect texture, diff -> h, l
                self._ttre1.Apply();
                self._ttre2.Apply();
                return;
            }
            orig(self);
        }
        public static void On_OPColorPicker_Update(On.Menu.Remix.MixedUI.OpColorPicker.orig_Update orig, OpColorPicker self)
        {
            orig(self);
            if (self.CurrentlyFocusableMouse && self.MenuMouseMode)
            {
                if (self._MouseOverHex() || self._curFocus == OpColorPicker.MiniFocus.HEX)
                    ((Action?)(ClipboardManager.CopyShortcutPressed(self.Menu) ? self.CopyHexCPicker : ClipboardManager.PasteShortcutPressed(self.Menu) ? self.PasteHexCPicker : null))?.Invoke();
                if (ModOptions.Instance.CopyPasteForColorPickerNumbers.Value && self.IfCPickerNumberHovered(out int oOO))
                    ((Action<int>?)(ClipboardManager.CopyShortcutPressed(self.Menu) ? self.CopyNumberCPicker : ClipboardManager.PasteShortcutPressed(self.Menu) ? self.PasteNumberCPicker : null))?.Invoke(oOO);
            }
        }
        public static void On_OPColorPicker_GrafUpdate(On.Menu.Remix.MixedUI.OpColorPicker.orig_GrafUpdate orig, OpColorPicker self, float timeStacker)
        {
            orig(self, timeStacker);
            if (self.greyedOut) return;
            if (!ModOptions.Instance.CopyPasteForColorPickerNumbers.Value) return;
            
                self._lblR.color = self.CurrentlyFocusableMouse && self._lblR.IsFLabelHovered(self.MousePos) ? Color.Lerp(MenuColorEffect.rgbWhite, self.colorText, self.bumpBehav.Sin(10f)) :
                    self._lblR.color;
                self._lblG.color = self.CurrentlyFocusableMouse && self._lblG.IsFLabelHovered(self.MousePos) ? Color.Lerp(MenuColorEffect.rgbWhite, self.colorText, self.bumpBehav.Sin(10f)) :
                    self._lblG.color;
                self._lblB.color = self.CurrentlyFocusableMouse && self._lblB.IsFLabelHovered(self.MousePos) ? Color.Lerp(MenuColorEffect.rgbWhite, self.colorText, self.bumpBehav.Sin(10f)) :
                    self._lblB.color;  
        }
        private static void GotoHSL(ILCursor cursor)
        {
            cursor.GotoNext(x => x.MatchCall(typeof(Custom), "HSL2RGB"));
            cursor.GotoNext(MoveType.After, x => x.MatchCallvirt<FNode>("set_isVisible"));
        }
        private static void MakeAReturn(ILCursor cursor)
        {
            cursor.Emit(OpCodes.Brfalse, cursor.Next);
            cursor.Emit(OpCodes.Ret);
        }
    }
}
