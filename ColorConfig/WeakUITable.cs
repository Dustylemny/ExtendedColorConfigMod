using ColorConfig.MenuUI;
using Menu;
using Menu.Remix.MixedUI;
using System;
using UnityEngine;

namespace ColorConfig.WeakUITable
{
    public class InputExtras
    {
        public void UpdateInputs()
        {
            lastPMInput = pMInput;
            lastFemInput = femInput;
            pMInput = SmallUtils.FixedPlayerUIInput(-1);
            femInput = SmallUtils.GetFixedExtraMenuInput();
        }
        public Player.InputPackage pMInput = new(), lastPMInput = new();
        public ExtraFixedMenuInput femInput = new(), lastFemInput = new();
    }
    public class ExtraSSMInterfaces
    {
        public HexTypeBox? hexInterface;
        public SlugcatDisplay? slugcatDisplay;
        public SliderPages? sliderPages;
        //Legacy versions stuff
        public OOOSliders? legacySliders;
        public HexTypeBoxPages? legacyHexInterface;
    }
    public class ExtraExpeditionInterfaces
    {
        public SymbolButton? colorConfig;
    }
    public class ColorPickerExtras(OpColorPicker cPicker)
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
        public void ChangeHSLHSVMode(bool changeToHSV)
        {
            cPicker._lblHSL.text = changeToHSV ? "HSV" : "HSL";
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

        public OpColorPicker cPicker = cPicker;
        public bool _IsHSVMode, _IsDifferentHSLHSVMode;
        public int h, s, v;
    }
}
