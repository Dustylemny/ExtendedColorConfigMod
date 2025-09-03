using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using JollyCoop.JollyMenu;

namespace ColorConfig.MenuUI.Objects
{
    public class JollyCoopOOOConfig : PositionedMenuObject
    {
        public static void AddJollySliderIDGroups(List<SliderIDGroup> IDGroups, ColorChangeDialog.ColorSlider colSlider, bool shouldRemoveHSL)//int bodyPart, bool shouldRemoveHSL)
        {
            if (!shouldRemoveHSL)
                IDGroups.Add(new([colSlider.Hue, colSlider.Sat, colSlider.Lit], MenuToolObj.HSLNames,
                  MenuToolObj.HueOOShowInt, MenuToolObj.hueOOMultipler, MenuToolObj.HueOOSigns));

            if (ModOptions.EnableJollyRGBSliders)
                IDGroups.Add(new(MenuToolObj.RGBSliderIDS, MenuToolObj.RGBNames, MenuToolObj.RGBShowInt, MenuToolObj.rgbMultipler));
            if (ModOptions.Instance.EnableHSVSliders.Value)
                IDGroups.Add(new(MenuToolObj.HSVSliderIDS, MenuToolObj.HSVNames, MenuToolObj.HueOOShowInt, MenuToolObj.hueOOMultipler, MenuToolObj.HueOOSigns));

        }
        /* no need to make new ones if we just find colslider by slider owner
        public static Slider.SliderID[] RegisterOOOSliderGroups(string colorSpaceName, string[] oOONames, int bodyPart)
        {
            return [ new($"DUSTY_{bodyPart}_{colorSpaceName}_{oOONames.GetValueOrDefault(0, "1")}", true), 
                new($"DUSTY_{bodyPart}_{colorSpaceName}_{oOONames.GetValueOrDefault(1, "2")}", true),
                new($"DUSTY_{bodyPart}_{colorSpaceName}_{oOONames.GetValueOrDefault(2, "3")}", true),
            ];
        }*/
        public JollyCoopOOOConfig(Menu.Menu menu, ColorChangeDialog.ColorSlider owner, /*int bodyPartNum,*/ bool removeHSL = false, bool addHexInterface = true) : base(menu, owner, owner.pos)
        {
            //no need bodynum
            List<SliderIDGroup> oOOIDGroups = [];
            AddJollySliderIDGroups(oOOIDGroups, owner, removeHSL);
            oOOPages = new(menu, this, [owner.hueSlider, owner.satSlider, owner.litSlider], oOOIDGroups, new Vector2(0, 39.5f), -pos)
            {
                roundingType = ModOptions.Instance.SliderRounding.Value,
                showValues = false,
                DecimalCount = ModOptions.DeCount,
            };
            oOOPages.LoadMethodToAllSliders((slider, pages, i) => slider.ChangeSliderID(pages.CurrentGroup.SafeID(i)));
            valueLabel = new(menu, this, "", new(120, 23), new(80, 30), false);
            if (addHexInterface)
            {
                hexInterface = new(menu, this, new(120f, -100f));
                hexInterface.OnSaveNewTypedColor += (hex, hsl, rgb) =>
                {
                    owner.hslColor = hsl.Vector32HSL();
                    owner.color = rgb;
                    owner.hueSlider.UpdateSliderValue();
                    owner.satSlider.UpdateSliderValue();
                    owner.litSlider.UpdateSliderValue();
                };
            }
            this.SafeAddSubObjects(oOOPages, valueLabel, hexInterface);
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            valueLabel.label.color = color;
            if (ModOptions.ShowVisual && oOOPages != null)
                valueLabel.text = string.Join(",", oOOPages.SliderVisualValues ?? []);
            else
                valueLabel.text = "";
        }
        public override void Update()
        {
            base.Update();
            if (owner is ColorChangeDialog.ColorSlider colSlider)
                hexInterface?.SaveNewHSL(colSlider.hslColor.HSL2Vector3());
        }

        public Color color = MenuColorEffect.rgbMediumGrey;
        public MenuLabel valueLabel;
        public SliderPages oOOPages;
        //public List<SliderIDGroup> oOOIDGroups; removed that since instead of parsing num, we get slider owner instead
        public HexTypeBox? hexInterface;
    }
}
