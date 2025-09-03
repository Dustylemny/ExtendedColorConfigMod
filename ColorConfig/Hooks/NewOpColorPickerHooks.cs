using Menu.Remix.MixedUI;
using MonoMod.Cil;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using OnOpColorPicker = On.Menu.Remix.MixedUI.OpColorPicker;
using ILOpColorPicker = IL.Menu.Remix.MixedUI.OpColorPicker;
using Mono.Cecil.Cil;
using ColorConfig.WeakUITable;
using Menu;

namespace ColorConfig.Hooks
{
    public static partial class ColorConfigHooks
    {
        public static void OpColorPicker_Hooks()
        {
            try
            {
                OnOpColorPicker.ctor += On_OPColorPicker_ctor;
                OnOpColorPicker._RecalculateTexture += On_OPColorPicker_RecalculateTexture;
                OnOpColorPicker.Change += On_OPColorPicker_Change;
                OnOpColorPicker._MouseTrySwitchMode += On_OPColorPicker__MouseTrySwitchMode;
                OnOpColorPicker.GrafUpdate += On_OPColorPicker_GrafUpdate;
                OnOpColorPicker.Update += On_OPColorPicker_Update;
                ILOpColorPicker._NonMouseModeUpdate += IL_OPColorPicker__NonMouseModeUpdate;
                ILOpColorPicker.MouseModeUpdate += IL_OPColorPicker_MouseModeUpdate;
                ILOpColorPicker._HSLSetValue += IL_OPColorPicker__HSLSetValue;
                SetUpMoreHooks();

                ColorConfigMod.DebugLog("Successfully extended color interface for OpColorPicker!");
            }
            catch (Exception ex)
            {
                ColorConfigMod.DebugException("Failed to initialize colorpicker hooks", ex);
            }
        }
        public static void On_OPColorPicker_Change(OnOpColorPicker.orig_Change orig, OpColorPicker self)
        {
            orig(self);
            if (!self._ctor) return;
            if (self._mode != OpColorPicker.PickerMode.HSL) return;
            Vector3 hslHSV = self.GetHSVOrHSL01();
            self._cdis0.color = self.IsHSVMode() ? ColConversions.HSV2RGB(hslHSV) : 
                    ModOptions.Instance.EnableBetterOPColorPicker.Value && self._h == 100 ? ColConversions.HSL2RGB(hslHSV) : self._cdis0.color;
        }
        public static void On_OPColorPicker_ctor(OnOpColorPicker.orig_ctor orig, OpColorPicker self, Configurable<Color> config, Vector2 pos)
        {
            ColorPickerExtras extras = self.GetColorPickerExtras();
            extras._IsHSVMode = ModOptions.PickerHSVMode;
            extras._IsDifferentHSLHSVMode = ModOptions.Instance.EnableDiffOpColorPickerHSL.Value;
            orig(self, config, pos);
            self._lblHSL.text = self.IsHSVMode() ? "HSV" : self._lblHSL.text;
        }
        public static void On_OPColorPicker_RecalculateTexture(OnOpColorPicker.orig__RecalculateTexture orig, OpColorPicker self)
        {
            ColorPickerExtras extras = self.GetColorPickerExtras();
            bool diff = extras.IsDifferentHSVHSLMode, hsvMode = extras.IsHSVMode;
            if (self._mode != OpColorPicker.PickerMode.HSL || (!diff && !hsvMode))
            {
                orig(self);
                return;
            }
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
            Func<Vector3, Color> hsvHSL2rgb = self.ColorPicker2RGB();
            Vector3 hslHsv01 = self.GetHSVOrHSL01();
            for (int height = 0; height <= 100; height++)
            {
                for (int width = 0; width < self._ttre1.width; width++)
                {
                    float heightFloat = height * 0.01f, widthFloat = width * 0.01f;
                    float h = diff ? hslHsv01.x : widthFloat, 
                        s = diff ? widthFloat : heightFloat, 
                        l = diff ? heightFloat : hslHsv01.y;
                    self._ttre1.SetPixel(width, height, hsvHSL2rgb(new(h, s, l)));
                    if (width < self._ttre2.width)
                    {
                        h = diff ? heightFloat : hslHsv01.x;
                        s = hslHsv01.y;
                        l = diff ? hslHsv01.z : heightFloat;
                        self._ttre2.SetPixel(width, height, hsvHSL2rgb(new(h, s, l)));
                    }

                }
            }
            Vector3Int hsvHSL = self.GetHSVOrHSL100();
            Color hueArrowCol = new(1 - self._r * 0.01f, 1f - self._g * 0.01f, 1f - self._b * 0.01f);
            hueArrowCol = Color.Lerp(Color.white, hueArrowCol, Mathf.Pow(Mathf.Abs(hueArrowCol.grayscale - 0.5f) * 2, 0.3f));
            SmallUtils.ApplyHSLArrow(self._ttre1, diff ? hsvHSL.y : hsvHSL.x, hueArrowCol, false, diff ? hsvHSL.z : hsvHSL.y); //first leftright arrow, diff -> s, h
            SmallUtils.ApplyHSLArrow(self._ttre1, diff ? hsvHSL.z : hsvHSL.y, hueArrowCol, true, diff ? hsvHSL.y : hsvHSL.x); //second downright arrow square texture, diff -> l, s
            SmallUtils.ApplyHSLArrow(self._ttre2, diff ? hsvHSL.x : hsvHSL.z, hueArrowCol, true, 51); //last rect texture, diff -> h, l
            self._ttre1.Apply();
            self._ttre2.Apply();
        }
        public static void On_OPColorPicker__MouseTrySwitchMode(OnOpColorPicker.orig__MouseTrySwitchMode orig, OpColorPicker self, OpColorPicker.PickerMode newMode)
        {
            if (self.TrySwitchCustomMode(newMode))
                return;
            orig(self, newMode);
        }
        public static void On_OPColorPicker_GrafUpdate(OnOpColorPicker.orig_GrafUpdate orig, OpColorPicker self, float timeStacker)
        {
            orig(self, timeStacker);
            if (self.greyedOut) return;
            if (self.GetColorPickerExtras().IsDifferentHSVHSLMode && self.MenuMouseMode)
            {
                if (self._curFocus == OpColorPicker.MiniFocus.HSL_Hue)
                {
                    self._lblB.color = self._lblR.color;
                    self._lblR.color = self.colorText;
                    self._focusGlow.pos = new(self._focusGlow.pos.x, 25);
                }
                else if (self._curFocus == OpColorPicker.MiniFocus.HSL_Lightness)
                {
                    self._lblR.color = self._lblB.color;
                    self._lblB.color = self.colorText;
                    self._focusGlow.pos = new(self._focusGlow.pos.x, 105);
                }
            }
            if (ModOptions.Instance.CopyPasteForColorPickerNumbers.Value)
            {
                Color selectedColor = Color.Lerp(MenuColorEffect.rgbWhite, self.colorText, self.bumpBehav.Sin(10f));
                self._lblR.color = self._lblR.IsFLabelHovered(self.MousePos) ? selectedColor :
                  self._lblR.color;
                self._lblG.color = self._lblG.IsFLabelHovered(self.MousePos) ? selectedColor :
                    self._lblG.color;
                self._lblB.color = self._lblB.IsFLabelHovered(self.MousePos) ? selectedColor :
                    self._lblB.color;
            }
        }
        public static void On_OPColorPicker_Update(OnOpColorPicker.orig_Update orig, OpColorPicker self)
        {
            orig(self);
            if (!self.CurrentlyFocusableMouse && !self.MenuMouseMode) return;
            ColorPickerExtras extras = self.GetColorPickerExtras();
            if (self.Menu.CopyShortcutPressed())
            {
                extras.Copy();
            }
            if (self.Menu.PasteShortcutPressed())
            {
                extras.Paste();
            }
            //insert copy paste for hex and numbers

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
                    ColorPickerExtras extras = self.GetColorPickerExtras();
                    return extras.IsHSVMode ? 
                    ColConversions.HSV2RGB(extras.GetHSV) : 
                    ModOptions.Instance.EnableBetterOPColorPicker.Value && self._h == 100 ? 
                    ColConversions.HSL2RGB(extras.GetHSL) : hslCol;
                });
                ColorConfigMod.DebugLog("Sucessfully patched RGB Color to take hsv or not turn grey!");
            }
            catch (Exception e)
            {
                ColorConfigMod.DebugException("Failed to patch RGB Color ", e);
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
                    if (self.TrySwitchCustomMode(newMode))
                        return true;
                    return false;
                });
                cursor.Emit(OpCodes.Brfalse, cursor.Next);
                cursor.Emit(OpCodes.Ret);
                ColorConfigMod.DebugLog("Sucessfully patched for switch mode using non-mouse");
            }
            catch (Exception ex)
            {
                ColorConfigMod.DebugException("Failed to patch for switch mode using non-mouse", ex);
            }
        }
        public static void IL_OPColorPicker_MouseModeUpdate(ILContext il)
        {
            try
            {
                ILCursor cursor = new(il);

                cursor.GotoNext(x => x.MatchCall(typeof(Custom), "HSL2RGB"));
                cursor.GotoNext(MoveType.After, x => x.MatchCallvirt<FNode>("set_isVisible"));
                ColorConfigMod.DebugILCursor(cursor);

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc_3);
                cursor.Emit(OpCodes.Ldloc, 4);
                cursor.EmitDelegate(delegate (OpColorPicker self, int hueSat, int satLit)
                {
                    return self.OpColorPickerPatchMiniFocusHSLColor(hueSat, satLit, 0, true);
                });
                cursor.Emit(OpCodes.Brfalse, cursor.Next);
                cursor.Emit(OpCodes.Ret);
                ColorConfigMod.DebugLog("Sucessfully patched MiniFocus HueSat to SatHue!");

                cursor.GotoNext(x => x.MatchCall(typeof(Custom), "HSL2RGB"));
                cursor.GotoNext(MoveType.After, x => x.MatchCallvirt<FNode>("set_isVisible"));
                ColorConfigMod.DebugILCursor(cursor);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc, 5);
                cursor.EmitDelegate(delegate (OpColorPicker self, int litHue)
                {
                    return self.OpColorPickerPatchMiniFocusHSLColor(0, 0, litHue, false);
                });
                ColorConfigMod.DebugLog("Sucessfully patched MiniFocus for Lit to Hue!");

                cursor.Emit(OpCodes.Brfalse, cursor.Next);
                cursor.Emit(OpCodes.Ret);


                cursor.GotoNext(x => x.MatchCall(typeof(Custom), "HSL2RGB"));
                cursor.GotoNext(MoveType.After, x => x.MatchCallvirt<FNode>("set_isVisible"));
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
                    ColorPickerExtras extras = self.GetColorPickerExtras();
                    if (extras.IsHSVMode)
                        rXHSL = ColConversions.HSL2HSV(rXHSL.RXHSl2Vector3()).Vector32RXHSL();
                    if (ModOptions.Instance.EnableBetterOPColorPicker.Value)
                    {
                        if (self._mode == OpColorPicker.PickerMode.HSL && (self._curFocus == OpColorPicker.MiniFocus.HSL_Hue || self._curFocus == OpColorPicker.MiniFocus.HSL_Saturation || self._curFocus == OpColorPicker.MiniFocus.HSL_Lightness))
                            rXHSL = extras.GetHSLorHSV01.Vector32RXHSL(); //prevent weird rgb + hsl interactions
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
        public static void SetUpMoreHooks()
        {
            typeof(OpColorPicker).GetProperty(nameof(OpColorPicker.@value)).GetSetMethod().ILHookMethod(IL_OPColorPicker_set_value);
            typeof(OpColorPicker).GetProperty(nameof(OpColorPicker.@value)).GetSetMethod().HookMethod(delegate (Action<OpColorPicker, string> orig, OpColorPicker self, string newValue)
            {
                ColorPickerExtras extra = self.GetColorPickerExtras();
                if ((ModOptions.Instance.EnableBetterOPColorPicker.Value || extra.IsDifferentHSVHSLMode) && self._mode == OpColorPicker.PickerMode.HSL && extra.HasHSLChanged)
                    self.RefreshTexture();
                orig(self, newValue);
                extra.SetLastHSL();
            });
        }
    }
}
