using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using Menu;
using JollyCoop.JollyMenu;
using UnityEngine;
using MonoMod.RuntimeDetour;
using static ColorConfig.MenuInterfaces;

namespace ColorConfig
{
    public class JollyMenuHooks
    {
        public void Init()
        {
            try
            {
                On.JollyCoop.JollyMenu.ColorChangeDialog.ValueOfSlider += OnDialog_ValueOfSlider;
                On.JollyCoop.JollyMenu.ColorChangeDialog.SliderSetValue += OnDialog_SliderSetValue;
                On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.ctor += OnColorSlider_ctor;
                On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.RemoveSprites += OnColorSlider_RemoveSprites;
                On.Menu.PositionedMenuObject.Update += OnPosMenuObject_Update;
                ColorConfigMod.DebugLog("Sucessfully extended color interface for jolly coop menu!");
            }
            catch (Exception ex)
            {
                ColorConfigMod.DebugException("Failed to initalise hooks for JollyMenu color interface!", ex);
            }
        }
        //JollyColorConfigHooks
        private float OnDialog_ValueOfSlider(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_ValueOfSlider orig, ColorChangeDialog self, Slider slider)
        {
            if (FindValueOfSlider(slider, out float f))
            {
                return f;
            }
            return orig(self, slider);
        }
        private void OnDialog_SliderSetValue(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_SliderSetValue orig, ColorChangeDialog self, Slider slider, float f)
        {
            if (slider.ID.value.Contains("DUSTY"))
            {
                string[] array = slider.ID.value.Split(new char[]
                {
                  '_'
                });
                if (int.TryParse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture, out int num))
                {
                    ColorChangeDialog.ColorSlider colSlider = colorInterfaces[num];
                    if (slider.ID.value.Contains("RGB"))
                    {
                        if (FindOOO(slider.ID.value, CustomColorModel.RGB, out int ooo))
                        {
                            SetNewCustomColor(f, ref colSlider, ooo, CustomColorModel.RGB);
                        }

                    }
                    else if (slider.ID.value.Contains("HSV"))
                    {
                        if (FindOOO(slider.ID.value, CustomColorModel.HSV, out int ooo))
                        {
                            SetNewCustomColor(f, ref colSlider, ooo, CustomColorModel.HSV);
                        }
                    }
                }
            }
            orig(self, slider, f);
        }
        //colorSliderHooks
        private void OnColorSlider_ctor(On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_ctor orig, ColorChangeDialog.ColorSlider self, Menu.Menu menu, MenuObject owner, Vector2 pos, int playerNumber, int bodyPart, string sliderTitle)
        {
            orig(self, menu, owner, pos, playerNumber, bodyPart, sliderTitle);
            colorInterfaces.Add(self);
            Vector2 sliderSize = new(ColorChangeDialog.ColorSlider.GetSliderWidth(menu.CurrLang), 30);
            JollyPageSliderInterface colorPageInterface = new(menu, self, pos, sliderSize, bodyPart);
            colorPageInterface.AddHSLInterface(self.hueSlider, self.satSlider, self.litSlider, bodyPart, ColorConfigMod.IsRGBColorSliderModOn);
            self.subObjects.Add(colorPageInterface);
            if (ModOptions.enableHexCodeTypers.Value)
            {
                HexInterface hexInterface = new(menu, self, pos + new Vector2(135, -107f), false);
                colorHexInterface.Add(self, hexInterface);
                self.subObjects.Add(hexInterface);
            }
        }
        private void OnColorSlider_RemoveSprites(On.JollyCoop.JollyMenu.ColorChangeDialog.ColorSlider.orig_RemoveSprites orig, ColorChangeDialog.ColorSlider self)
        {
            if (colorHexInterface.ContainsKey(self))
            {
                if (colorHexInterface[self] != null)
                {
                    colorHexInterface[self].RemoveSprites();
                    self.RemoveSubObject(colorHexInterface[self]);
                }
                colorHexInterface.Remove(self);
            }
            if (colorInterfaces.Contains(self))
            {
                colorInterfaces.Remove(self);
            }
            orig(self);
        }
        private void OnPosMenuObject_Update(On.Menu.PositionedMenuObject.orig_Update orig, PositionedMenuObject self)
        {
            orig(self);
            SetHexColor(self);
        }
        //Utils
        private bool FindValueOfSlider(Slider slider, out float f)
        {
            f = 0;
            if (slider.ID.value.Contains("DUSTY"))
            {
                string[] array = slider.ID.value.Split(new char[]
                {
                    '_'
                });
                if (int.TryParse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture, out int num))
                {
                    if (slider.ID.value.Contains("RGB"))
                    {
                        if (FindOOO(slider.ID.value, CustomColorModel.RGB, out int ooo))
                        {
                            f = ValueOfOOO(colorInterfaces[num], ooo, CustomColorModel.RGB);
                            return true;
                        }
                    }
                    else if (slider.ID.value.Contains("HSV"))
                    {
                        if (FindOOO(slider.ID.value, CustomColorModel.HSV, out int ooo))
                        {
                            f = ValueOfOOO(colorInterfaces[num], ooo, CustomColorModel.HSV);
                            return true;
                        }
                    }

                }
            }
            return false;
        }
        public static bool FindOOO(string value, CustomColorModel colormodel, out int OOO)
        {
            OOO = -1;
            if (colormodel == CustomColorModel.RGB)
            {
                if (value.Contains("RED"))
                {
                    OOO = 0;
                }
                if (value.Contains("GREEN"))
                {
                    OOO = 1;
                }
                if (value.Contains("BLUE"))
                {
                    OOO = 2;
                }
            }
            if (colormodel == CustomColorModel.HSV)
            {
                if (value.Contains("HUE"))
                {
                    OOO = 0;
                }
                if (value.Contains("SAT"))
                {
                    OOO = 1;
                }
                if (value.Contains("VALUE"))
                {
                    OOO = 2;
                }
            }
            if (colormodel == CustomColorModel.HSL)
            {
                if (value.Contains("HUE"))
                {
                    OOO = 0;
                }
                if (value.Contains("SAT"))
                {
                    OOO = 1;
                }
                if (value.Contains("LIT"))
                {
                    OOO = 2;
                }
            }
            return OOO > -1;
        }
        private float ValueOfOOO(ColorChangeDialog.ColorSlider colSlider, int OOO, CustomColorModel colormodel)
        {
            float result = 0;
            if (colormodel == CustomColorModel.RGB)
            {
                switch(OOO)
                {
                    case 0:
                        result = colSlider.color.r;
                        break;
                    case 1: result = colSlider.color.g;
                        break;
                    case 2: result = colSlider.color.b;
                        break;
                }
            }
            else if (colormodel == CustomColorModel.HSV)
            {
                Vector3 hsv = ColConversions.HSL2HSV(SmallUtils.HSL2Vector3(colSlider.hslColor));
                switch (OOO)
                {
                    case 0: 
                        result = Mathf.Clamp(hsv.x, 0, 0.99f);
                        break;
                    case 1:
                        result = hsv.y;
                        break;
                    case 2: result = hsv.z;
                        break;
                }
            }
            return result;
        }
        public static void SetNewCustomColor(float f, ref ColorChangeDialog.ColorSlider colSlider, int OOO, CustomColorModel colormodel)
        {
            if (colormodel == CustomColorModel.RGB)
            {
                switch (OOO)
                {
                    default:
                        colSlider.color.r = f;
                        break;
                    case 1:
                        colSlider.color.g = f;
                        break;
                    case 2:
                        colSlider.color.b = f;
                        break;
                }
                colSlider.color = SmallUtils.Vector32RGB(SmallUtils.RWIIIClamp(SmallUtils.RGB2Vector3(colSlider.color), colormodel, out Vector3 clampedHSL));
                colSlider.hslColor = SmallUtils.Vector32HSL(clampedHSL);
            }
            else if (colormodel == CustomColorModel.HSV)
            {
                Vector3 hsv = ColConversions.HSL2HSV(SmallUtils.HSL2Vector3(colSlider.hslColor));
                switch (OOO)
                {
                    default:
                        hsv.x = Mathf.Clamp(f, 0, 0.99f);
                        break;
                    case 1:
                        hsv.y = f;
                        break;
                    case 2:
                        hsv.z = f;
                        break;
                }
                SmallUtils.RWIIIClamp(hsv, colormodel, out Vector3 clampedHSL);
                colSlider.hslColor = new(clampedHSL.x, clampedHSL.y, clampedHSL.z);
                if (ColorConfigMod.IsRGBColorSliderModOn)
                {
                    FixedHSL2RGB(ref colSlider);
                    return;
                }
                colSlider.HSL2RGB();
            }
        }
        private void SetHexColor(PositionedMenuObject posObj)
        {
            if (posObj is ColorChangeDialog.ColorSlider colSlider && posObj.menu is ColorChangeDialog)
            {
                if (colorHexInterface.ContainsKey(colSlider))
                {

                    colorHexInterface[colSlider].SetNewHSLColor(SmallUtils.HSL2Vector3(colSlider.hslColor));
                    if (colorHexInterface[colSlider].updateCol == true)
                    {
                        colorHexInterface[colSlider].updateCol = false;
                        colSlider.hslColor = SmallUtils.Vector32HSL(colorHexInterface[colSlider].pendingNewHSL);
                        if (ColorConfigMod.IsRGBColorSliderModOn)
                        {
                            FixedHSL2RGB(ref colSlider);
                            Color rgbColor = ColConversions.HSL2RGB(colorHexInterface[colSlider].pendingNewHSL);
                            colSlider.menu.SliderSetValue(colSlider.hueSlider, rgbColor.r);
                            colSlider.menu.SliderSetValue(colSlider.satSlider, rgbColor.g);
                            colSlider.menu.SliderSetValue(colSlider.litSlider, rgbColor.b);
                            return;
                        }
                        colSlider.HSL2RGB();
                    }
                }
            }
        }
        //rgb slider mod hooks, replace
        public static void FixedHSL2RGB(ref ColorChangeDialog.ColorSlider colSlider)
        {
            colSlider.color = colSlider.hslColor.rgb;
        }

        public static readonly List<ColorChangeDialog.ColorSlider> colorInterfaces = new();
        private readonly Dictionary<ColorChangeDialog.ColorSlider, HexInterface> colorHexInterface = new();

    }
}
