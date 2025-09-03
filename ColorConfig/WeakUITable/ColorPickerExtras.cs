using ColorConfig.MenuUI;
using Menu;
using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ColorConfig.WeakUITable
{
    public class ColorPickerExtras
    {
        public bool IsHSVMode
        {
            get => _IsHSVMode;
            set
            {
                if (_IsHSVMode != value)
                {
                    _IsHSVMode = value;
                    ChangeHSLHSVMode(_IsHSVMode);
                    cPicker.RefreshTexture();
                    RefreshText();
                }
            }
        }
        public bool IsDifferentHSVHSLMode
        {
            get => _IsDifferentHSLHSVMode;
            set
            {
                if (_IsDifferentHSLHSVMode != value)
                {
                    _IsDifferentHSLHSVMode = value;
                    cPicker.RefreshTexture();
                }

            }
        }
        public bool HasHSLChanged => lastH != cPicker._h || lastS != cPicker._s || lastL != cPicker._l;
        public Vector3 GetHSL => new(cPicker._h * 0.01f, cPicker._s * 0.01f, cPicker._l * 0.01f);
        public Vector3 GetHSV => new(cPicker._h * 0.01f, cPicker._s * 0.01f, cPicker._l * 0.01f);
        public Vector3 GetHSLorHSV01 => new(cPicker._h * 0.01f, cPicker._s * 0.01f, cPicker._l * 0.01f);
        public Vector3Int GetHSLorHSV100 => new(cPicker._h, cPicker._s, cPicker._l);
        public string? Clipboard { get => ClipboardManager.Clipboard; set => ClipboardManager.Clipboard = value; }
        public ColorPickerExtras(OpColorPicker cPicker)
        {
            this.cPicker = cPicker;
        }
        public void Copy()
        {
            if (cPicker._MouseOverHex())
            {
                Clipboard = cPicker.value;
                cPicker.PlaySound(SoundID.MENU_Player_Join_Game);
                return;
            }
            if (ModOptions.Instance.CopyPasteForColorPickerNumbers.Value && cPicker.IfCPickerNumberHovered(out int oOO))
            {
                Clipboard = oOO == 0 ? cPicker._lblR.text : oOO == 1 ? cPicker._lblG.text : cPicker._lblB.text;
                cPicker.PlaySound(SoundID.MENU_Player_Join_Game);
            }
        }
        public void Paste()
        {
            if (Clipboard == null) return;
            if (cPicker._MouseOverHex())
            {
                string newValue = Clipboard.Trim().TrimStart('#');
                if (MenuColorEffect.IsStringHexColor(newValue) && !newValue.IsHexCodesSame(cPicker.value))
                {
                    cPicker.value = newValue.Substring(0, 6).ToUpper();
                    cPicker.PlaySound(SoundID.MENU_Switch_Page_In);
                }
                else cPicker.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
                return;
            }
            if (ModOptions.Instance.CopyPasteForColorPickerNumbers.Value && cPicker.IfCPickerNumberHovered(out int oOO))
            {
                string? newValue = ClipboardManager.Clipboard?.Trim();
                if (float.TryParse(newValue, NumberStyles.Integer | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float result))
                {
                    int toPut = Mathf.Clamp(Mathf.RoundToInt(result), 0, 100);
                    cPicker.SetHSLRGB(oOO == 0 ? toPut : null, oOO == 1 ? toPut : null, oOO == 2 ? toPut : null);
                    return;
                }
                cPicker.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
            }
        }
        public void ChangeHSLHSVMode(bool changeToHSV)
        {
            cPicker._lblHSL.text = changeToHSV ? "HSV" : "HSL";
            ChangeHSLHSV(changeToHSV);
        }
        public void ChangeHSLHSV(bool changeToHSV)
        {
            Vector3 hslHSV100 = ((Func<Vector3, Vector3>)(changeToHSV ? ColConversions.HSL1002HSV100 : ColConversions.HSV1002HSL100)).Invoke(cPicker.GetHSL());
            cPicker._h = Mathf.RoundToInt(hslHSV100.x);
            cPicker._s = Mathf.RoundToInt(hslHSV100.y);
            cPicker._l = Mathf.RoundToInt(hslHSV100.z);
        }
        public void RefreshText()
        {
            if (cPicker != null && cPicker._mode != OpColorPicker.PickerMode.Palette)
            {
                Vector3Int hsvHSL = cPicker.GetHSVOrHSL100();
                cPicker._lblR.text = (cPicker._mode == OpColorPicker.PickerMode.RGB ? cPicker._r : hsvHSL.x).ToString();
                cPicker._lblG.text = (cPicker._mode == OpColorPicker.PickerMode.RGB ? cPicker._g : hsvHSL.y).ToString();
                cPicker._lblB.text = (cPicker._mode == OpColorPicker.PickerMode.RGB ? cPicker._b : hsvHSL.z).ToString();
            }
        }
        public void SetLastHSL()
        {
            lastH = cPicker._h; lastS = cPicker._s; lastL = cPicker._l;
        }

        public OpColorPicker cPicker;
        public bool _IsHSVMode, _IsDifferentHSLHSVMode;
        //todo: use seperate hsv;
        public int h, s, v, lastH, lastS, lastL;
    }
}
