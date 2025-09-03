using ColorConfig.WeakUITable;
using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using UnityEngine;
using static ColorConfig.ColConversions;

namespace ColorConfig
{
    public static partial class SmallUtils
    {
        public static ColorPickerExtras GetColorPickerExtras(this OpColorPicker cPicker) => ColorConfigMod.extraColorPickerStuff.GetValue(cPicker, (_) => new(cPicker));
        public static bool IsHSVMode(this OpColorPicker cPicker) => cPicker.GetColorPickerExtras()._IsHSVMode;
        public static bool IsDiffHSLHSVMode(this OpColorPicker cPicker) => cPicker.GetColorPickerExtras().IsDifferentHSVHSLMode;
        public static bool TrySwitchCustomMode(this OpColorPicker self, OpColorPicker.PickerMode newMode)
        {
            bool canSwitch = (ModOptions.PickerHSVMode || ModOptions.Instance.EnableDiffOpColorPickerHSL.Value);
            if (ModOptions.Instance.EnableRotatingOPColorPicker.Value && self._mode == OpColorPicker.PickerMode.HSL && newMode == self._mode && canSwitch)
            {
                self.GetColorPickerExtras().IsHSVMode = (!ModOptions.PickerHSVMode && !self.GetColorPickerExtras().IsHSVMode) || (!ModOptions.Instance.EnableBetterOPColorPicker.Value && ModOptions.Instance.EnableDiffOpColorPickerHSL.Value && !self.GetColorPickerExtras().IsDifferentHSVHSLMode) ? self.GetColorPickerExtras().IsHSVMode : !self.GetColorPickerExtras().IsHSVMode;
                self.GetColorPickerExtras().IsDifferentHSVHSLMode = !ModOptions.Instance.EnableDiffOpColorPickerHSL.Value && !self.GetColorPickerExtras().IsDifferentHSVHSLMode ? self.GetColorPickerExtras().IsDifferentHSVHSLMode : !self.GetColorPickerExtras().IsDifferentHSVHSLMode;
                self.PlaySound(SoundID.MENU_Player_Join_Game);
                return true;
            }
            return false;
        }
        public static void SwitchHSLCustomMode(this OpColorPicker self)
        {
            //switch hsl, hsl_diff, hsv, hsv_diff if all except betterColorPickers. else hsl, hsv_diff. hsl, hsv if only HSV mode. hsl, hsl_diff if only diff
            // so if diff is on, it just switches, hsv switches if diff is off or _diff is on
            // allow change if ex: diff is off but _diff is still on
            self.GetColorPickerExtras().IsHSVMode = (!ModOptions.PickerHSVMode && !self.GetColorPickerExtras().IsHSVMode) || (!ModOptions.Instance.EnableBetterOPColorPicker.Value && ModOptions.Instance.EnableDiffOpColorPickerHSL.Value && !self.GetColorPickerExtras().IsDifferentHSVHSLMode) ? self.GetColorPickerExtras().IsHSVMode : !self.GetColorPickerExtras().IsHSVMode;
            self.GetColorPickerExtras().IsDifferentHSVHSLMode = !ModOptions.Instance.EnableDiffOpColorPickerHSL.Value && !self.GetColorPickerExtras().IsDifferentHSVHSLMode ? self.GetColorPickerExtras().IsDifferentHSVHSLMode : !self.GetColorPickerExtras().IsDifferentHSVHSLMode;
            self.PlaySound(SoundID.MENU_Player_Join_Game);
        }
        public static bool OpColorPickerPatchMiniFocusHSLColor(this OpColorPicker self, int hueSat, int satLit, int litHue, bool squareTexture)
        {
            Vector3Int hsvHSL = self.GetHSVOrHSL100();
            int h = hsvHSL.x, s = hsvHSL.y, l = hsvHSL.z;
            if (self.IsDiffHSLHSVMode()) //HueSat becomes SatLit and Lit becomes Hue
            {
                hueSat = squareTexture ? Mathf.RoundToInt(Mathf.Clamp(self.MousePos.x - 10f, 0f, 100f)) : hueSat;
                litHue = litHue > 99 ? 99 : litHue;
                h = squareTexture ? h : litHue;
                s = squareTexture ? hueSat : s;
                l = squareTexture ? satLit : l;
                self._lblR.text = h.ToString();
                self._lblG.text = s.ToString();
                self._lblB.text = l.ToString();
                self._cdis1.color = self.ColorPicker2RGB().Invoke(new(h * 0.01f, s * 0.01f, l * 0.01f));
                self.SetHSLORHSV100(h, s, l);
                return true;
            }
            if (self.IsHSVMode())
            {
                h = squareTexture ? hueSat : h;
                s = squareTexture ? satLit : s;
                l = squareTexture ? l : litHue;
                self._cdis1.color = HSV2RGB(new(h * 0.01f, s * 0.01f, l * 0.01f));
            }
            return false;
        }
        public static void OpColorPickerPatchHoverMouseHSLColor(this OpColorPicker self, int hueSat, int satLit, int litHue, bool squareTexture)
        {
            Vector3Int hsvHSL = self.GetHSVOrHSL100();
            int h = hsvHSL.x, s = hsvHSL.y, l = hsvHSL.z;
            if (self.IsDiffHSLHSVMode()) //When mouse just hovers over texture
            {
                hueSat = squareTexture ? Mathf.RoundToInt(Mathf.Clamp(self.MousePos.x - 10f, 0f, 100f)) : hueSat;
                litHue = litHue > 99 ? 99 : litHue;
                h = squareTexture ? h : litHue;
                s = squareTexture ? hueSat : s;
                l = squareTexture ? satLit : l;
                self._lblR.text = h.ToString();
                self._lblG.text = s.ToString();
                self._lblB.text = l.ToString();
                self._cdis1.color = self.ColorPicker2RGB().Invoke(new(h / 100f, s / 100f, l / 100f));
            }
            else if (self.IsHSVMode())
            {
                h = squareTexture ? hueSat : h;
                s = squareTexture ? satLit : s;
                l = squareTexture ? l : litHue;
                self._cdis1.color = self.ColorPicker2RGB().Invoke(new(h / 100f, s / 100f, l / 100f));
            }
        }
        public static Func<Vector3, Color> ColorPicker2RGB(this OpColorPicker cPicker) => cPicker.IsHSVMode() ? ColConversions.HSV2RGB : ColConversions.HSL2RGB;
        public static void RefreshTexture(this OpColorPicker cPicker)
        {
            if (cPicker != null)
            {
                cPicker._RecalculateTexture();
                if (cPicker._mode == OpColorPicker.PickerMode.RGB)
                {
                    cPicker._ftxr1.SetTexture(cPicker._ttre1);
                    cPicker._ftxr2.SetTexture(cPicker._ttre2);
                    cPicker._ftxr3.SetTexture(cPicker._ttre3);
                }
                else if (cPicker._mode == OpColorPicker.PickerMode.HSL)
                {
                    cPicker._ftxr1.SetTexture(cPicker._ttre1);
                    cPicker._ftxr2.SetTexture(cPicker._ttre2);
                }
                else
                {
                    cPicker._ftxr2.SetPosition(OpColorPicker._GetPICenterPos(cPicker._pi));
                }
            }
        }
        public static Vector3Int GetHSL(this OpColorPicker cPicker)
        {
            return new(cPicker._h, cPicker._s, cPicker._l);
        }
        public static Vector3 GetHSL01(this OpColorPicker cPicker) => new(cPicker._h * 0.01f, cPicker._s * 0.01f, cPicker._l * 0.01f);
        public static Vector3Int GetHSVOrHSL100(this OpColorPicker cPicker) => cPicker.GetHSL();
        public static Vector3 GetHSVOrHSL01(this OpColorPicker cPicker) => cPicker.GetHSL01();
        public static Vector3 ParseGetHSLORHSV01(this OpColorPicker cPicker, float? h, float? s, float? l)
        {
            Vector3 hsvHSL = cPicker.GetHSVOrHSL01();
            return new(h.GetValueOrDefault(hsvHSL.x), s.GetValueOrDefault(hsvHSL.y), l.GetValueOrDefault(hsvHSL.z));
        }
        public static Vector3 ParseGetHSLORHSV100(this OpColorPicker cPicker, float? h, float? s, float? l)
        {
            Vector3 hsvHSL = cPicker.GetHSVOrHSL100();
            return new(h.GetValueOrDefault(hsvHSL.x), s.GetValueOrDefault(hsvHSL.y), l.GetValueOrDefault(hsvHSL.z));
        }
        public static void SetHSLORHSV100(this OpColorPicker cPicker, float h, float s, float l)
        {
            Vector3 hsvHSL100 = cPicker.GetHSVOrHSL100();
            if (h != hsvHSL100.x || s != hsvHSL100.y || l != hsvHSL100.z)
            {
                cPicker.SetDirectHSLORHSV100(Mathf.RoundToInt(h), Mathf.RoundToInt(s), Mathf.RoundToInt(l));
                cPicker.PlaySound(SoundID.MENU_Scroll_Tick);
                cPicker._HSLSetValue();
            }
        }
        public static void SetDirectHSLORHSV100(this OpColorPicker cPicker, int h, int s, int l)
        {
            cPicker._lblR.text = h.ToString();
            cPicker._lblG.text = s.ToString();
            cPicker._lblB.text = l.ToString();
            cPicker._cdis1.color = cPicker.ColorPicker2RGB().Invoke(new(h * 0.01f, s * 0.01f, l * 0.01f));
            cPicker._h = h;
            cPicker._s = s;
            cPicker._l = l;
        }
        public static void SetHSLRGB(this OpColorPicker cPicker, int? o1, int? o2, int? o3, bool soundIfSame = true)
        {
            if (cPicker._mode == OpColorPicker.PickerMode.HSL)
            {
                if (o1?.Equals(cPicker._h) == false || o2?.Equals(cPicker._s) == false || o3?.Equals(cPicker._l) == false)
                {
                    cPicker._h = o1 ?? cPicker._h;
                    cPicker._s = o2 ?? cPicker._s;
                    cPicker._l = o3 ?? cPicker._l;
                    cPicker.CPickerSetValue();
                    return;
                }
            }
            if (cPicker._mode == OpColorPicker.PickerMode.RGB)
            {
                if (o1?.Equals(cPicker._r) == false || o2?.Equals(cPicker._g) == false || o3?.Equals(cPicker._b) == false)
                {
                    cPicker._r = o1 ?? cPicker._r;
                    cPicker._g = o2 ?? cPicker._g;
                    cPicker._b = o3 ?? cPicker._b;
                    cPicker.CPickerSetValue();
                    return;
                }
            }
            if (soundIfSame)
            {
                cPicker.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
            }
        }
        public static bool IsFLabelHovered(this FLabel label, Vector2 mousePosition)
        {
            if (label != null)
            {
                Vector2 pos = label.GetPosition(), startRange = new(pos.x + label.textRect.x, pos.y + label.textRect.y),
                    endRange = startRange + new Vector2(label.textRect.width, label.textRect.height);
                return mousePosition.x >= startRange.x && mousePosition.y >= startRange.y && mousePosition.x <= endRange.x && mousePosition.y <= endRange.y;
            }
            return false;
        }
        public static bool IfCPickerNumberHovered(this OpColorPicker cPicker, out int ooo)
        {
            ooo = -1;
            if (cPicker != null)
            {
                ooo = cPicker._lblR.IsFLabelHovered(cPicker.MousePos) ? 0 :
                    cPicker._lblG.IsFLabelHovered(cPicker.MousePos) ? 1 :
                    cPicker._lblB.IsFLabelHovered(cPicker.MousePos) ? 2 : ooo;
            }
            return ooo > -1;
        }
        public static void CopyNumberCPicker(this OpColorPicker cPicker, int oOO)
        {
            if (cPicker == null) return;

            switch (oOO)
            {
                case 0:
                    ClipboardManager.Clipboard = cPicker._lblR.text;
                    break;
                case 1:
                    ClipboardManager.Clipboard = cPicker._lblG.text;
                    break;
                case 2:
                    ClipboardManager.Clipboard = cPicker._lblB.text;
                    break;
            }
            cPicker.PlaySound(SoundID.MENU_Player_Join_Game);
        }
        public static void PasteNumberCPicker(this OpColorPicker cPicker, int oOO)
        {
            if (cPicker == null) return;
            string? newValue = ClipboardManager.Clipboard?.Trim();
            if (float.TryParse(newValue, NumberStyles.Integer | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float result))
            {
                int toPut = Mathf.Clamp(Mathf.RoundToInt(result), 0, 100);
                cPicker.SetHSLRGB(oOO == 0 ? toPut : null, oOO == 1 ? toPut : null, oOO == 2 ? toPut : null);
                return;
            }
            cPicker.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
        }
        public static void CPickerSetValue(this OpColorPicker cPicker)
        {
            if (cPicker == null) return;
            if (cPicker._mode == OpColorPicker.PickerMode.RGB) cPicker._RGBSetValue();
            if (cPicker._mode == OpColorPicker.PickerMode.HSL) cPicker._HSLSetValue();
            cPicker.PlaySound(SoundID.MENU_Player_Join_Game);

        }
        public static void CopyHexCPicker(this OpColorPicker cPicker)
        {
            if (cPicker == null) return;
            ClipboardManager.Clipboard = cPicker.value;
            cPicker.PlaySound(SoundID.MENU_Player_Join_Game);

        }
        public static void PasteHexCPicker(this OpColorPicker cPicker)
        {
            if (cPicker == null) return;
            string? newValue = ClipboardManager.Clipboard?.Trim()?.TrimStart('#');
            if (newValue != null && MenuColorEffect.IsStringHexColor(newValue) && !newValue.IsHexCodesSame(cPicker.value))
            {
                cPicker.value = newValue.Substring(0, 6).ToUpper();
                cPicker.PlaySound(SoundID.MENU_Switch_Page_In);
                return;
            }
            cPicker.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);


        }
        public static void ApplyHSLArrow(Texture2D texture, int oOO, Color arrowColor, bool moveUpDown, int positioner = 0)
        {
            for (int pointer = Math.Max(0, oOO - 4); pointer <= Math.Min(100, oOO + 4); pointer++)
            {
                int middleOfoOO = 5 - Math.Abs(oOO - pointer), start = positioner > 50 ? 0 : 101 - middleOfoOO, control = positioner > 50 ? middleOfoOO : 101;
                for (int widthOrHeight = start; widthOrHeight < control; widthOrHeight++)
                {
                    int desiredX = moveUpDown ? widthOrHeight : pointer, desiredY = moveUpDown ? pointer : widthOrHeight;
                    texture.SetPixel(desiredX, desiredY, arrowColor);
                }

            }
        }
    }
}
