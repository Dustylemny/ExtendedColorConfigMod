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
        public static bool TrySwitchCustomMode(this OpColorPicker self, OpColorPicker.PickerMode newMode)
        {
            bool canSwitch = (ModOptions.PickerHSVMode || ModOptions.Instance.EnableDiffOpColorPickerHSL.Value);
            ColorPickerExtras extras = self.GetColorPickerExtras();
            if (ModOptions.Instance.EnableRotatingOPColorPicker.Value && self._mode == OpColorPicker.PickerMode.HSL && newMode == self._mode && canSwitch)
            {
                extras.IsHSVMode = (!ModOptions.PickerHSVMode && !extras.IsHSVMode) || (!ModOptions.Instance.EnableBetterOPColorPicker.Value && ModOptions.Instance.EnableDiffOpColorPickerHSL.Value && !extras.IsDifferentHSVHSLMode) ? extras.IsHSVMode : !extras.IsHSVMode;
                extras.IsDifferentHSVHSLMode = !ModOptions.Instance.EnableDiffOpColorPickerHSL.Value && !extras.IsDifferentHSVHSLMode ? extras.IsDifferentHSVHSLMode : !extras.IsDifferentHSVHSLMode;
                self.PlaySound(SoundID.MENU_MultipleChoice_Clicked);
                return true;
            }
            return false;
        }
        public static bool OpColorPickerPatchMiniFocusHSLColor(this OpColorPicker self, int hueSat, int satLit, int litHue, bool squareTexture)
        {
            ColorPickerExtras extras = self.GetColorPickerExtras();
            Vector3Int hsvHSL = extras.GetHSLorHSV100;
            int h = hsvHSL.x, s = hsvHSL.y, l = hsvHSL.z;
            if (extras.IsDifferentHSVHSLMode) //HueSat becomes SatLit and Lit becomes Hue
            {
                hueSat = squareTexture ? Mathf.RoundToInt(Mathf.Clamp(self.MousePos.x - 10f, 0f, 100f)) : hueSat;
                litHue = litHue > 99 ? 99 : litHue;
                h = squareTexture ? h : litHue;
                s = squareTexture ? hueSat : s;
                l = squareTexture ? satLit : l;
                self._lblR.text = h.ToString();
                self._lblG.text = s.ToString();
                self._lblB.text = l.ToString();
                self._cdis1.color = extras.HSLorHSV2RGB(new(h * 0.01f, s * 0.01f, l * 0.01f));
                self.SetHSLORHSV100(h, s, l);
                return true;
            }
            if (extras.IsHSVMode)
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
            ColorPickerExtras extras = self.GetColorPickerExtras();
            Vector3Int hsvHSL = extras.GetHSLorHSV100;
            int h = hsvHSL.x, s = hsvHSL.y, l = hsvHSL.z;
            if (extras.IsDifferentHSVHSLMode) //When mouse just hovers over texture
            {
                hueSat = squareTexture ? Mathf.RoundToInt(Mathf.Clamp(self.MousePos.x - 10f, 0f, 100f)) : hueSat;
                litHue = litHue > 99 ? 99 : litHue;
                h = squareTexture ? h : litHue;
                s = squareTexture ? hueSat : s;
                l = squareTexture ? satLit : l;
                self._lblR.text = h.ToString();
                self._lblG.text = s.ToString();
                self._lblB.text = l.ToString();
                self._cdis1.color = extras.HSLorHSV2RGB(new(h * 0.01f, s * 0.01f, l * 0.01f));
            }
            else if (extras.IsHSVMode)
            {
                h = squareTexture ? hueSat : h;
                s = squareTexture ? satLit : s;
                l = squareTexture ? l : litHue;
                self._cdis1.color = HSV2RGB(new(h * 0.01f, s * 0.01f, l * 0.01f));
            }
        }
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
        public static void SetHSLORHSV100(this OpColorPicker cPicker, float h, float s, float l)
        {
            ColorPickerExtras extras = cPicker.GetColorPickerExtras();
            Vector3 hsvHSL100 = extras.GetHSLorHSV100;
            if (h != hsvHSL100.x || s != hsvHSL100.y || l != hsvHSL100.z)
            {
                cPicker.SetDirectHSLORHSV100(Mathf.RoundToInt(h), Mathf.RoundToInt(s), Mathf.RoundToInt(l));
                cPicker.PlaySound(SoundID.MENU_Scroll_Tick);
                cPicker._HSLSetValue();
            }
        }
        public static void SetDirectHSLORHSV100(this OpColorPicker cPicker, int h, int s, int l)
        {
            ColorPickerExtras extras = cPicker.GetColorPickerExtras();
            cPicker._lblR.text = h.ToString();
            cPicker._lblG.text = s.ToString();
            cPicker._lblB.text = l.ToString();
            cPicker._cdis1.color = extras.HSLorHSV2RGB(new(h * 0.01f, s * 0.01f, l * 0.01f));
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
        public static void CPickerSetValue(this OpColorPicker cPicker)
        {
            if (cPicker == null) return;
            if (cPicker._mode == OpColorPicker.PickerMode.RGB) cPicker._RGBSetValue();
            if (cPicker._mode == OpColorPicker.PickerMode.HSL) cPicker._HSLSetValue();
            cPicker.PlaySound(SoundID.MENU_Player_Join_Game);

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
