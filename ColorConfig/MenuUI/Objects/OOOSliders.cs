using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;

namespace ColorConfig.MenuUI.Objects
{
    public class OOOSliders : PositionedMenuObject
    {
        public OOOSliders(Menu.Menu menu, MenuObject owner, Vector2 startPos, Vector2 offset, Vector2 size, List<SliderIDGroup> oOOIDGroups, Vector2 buttonOffset = default, bool subtle = false, bool showValue = true, int dec = 2, MidpointRounding rounding = MidpointRounding.AwayFromZero) : base(menu, owner, startPos)
        {
            SetUpSliders(offset, size, subtle);
            SetUpPages(oOOIDGroups, buttonOffset, showValue, dec, rounding);
        }
        public void SetUpSliders(Vector2 offset, Vector2 size, bool subtle)
        {
            sliderO = new(menu, this, "", Vector2.zero, size, null, subtle);
            sliderOO = new(menu, this, "", Vector2.zero + offset, size, null, subtle);
            sliderOOO = new(menu, this, "", Vector2.zero + offset * 2, size, null, subtle);
            subObjects.AddRange([sliderO, sliderOO, sliderOOO]);
            menu.MutualVerticalButtonBind(sliderOOO, sliderOO);
            menu.MutualVerticalButtonBind(sliderOO, sliderO);
        }
        public void SetUpPages(List<SliderIDGroup> oOOIDGroups, Vector2 buttonOffset, bool showValues, int dec, MidpointRounding rounding)
        {
            oOOPages = new(menu, this, [sliderO, sliderOO, sliderOOO], oOOIDGroups, buttonOffset == default ? new(0, 40) : buttonOffset)
            {
                showValues = showValues,
                roundingType = rounding,
                DecimalCount = dec,
            };
            subObjects.Add(oOOPages);
            oOOPages.PopulatePage(oOOPages.CurrentOffset);
            if (oOOPages.PagesOn)
            {
                menu.MutualVerticalButtonBind(oOOPages.nextButton, sliderOOO);
                oOOPages.prevButton.MenuObjectBind(sliderOOO, top: true);
            }
        }

        public SliderPages oOOPages;
        public HorizontalSlider sliderO, sliderOO, sliderOOO;
    }
}
