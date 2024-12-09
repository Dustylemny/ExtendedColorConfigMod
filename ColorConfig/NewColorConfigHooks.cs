using System;
using System.Collections.Generic;
using System.Linq;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using MoreSlugcats;
using UnityEngine;

namespace ColorConfig
{
    public static class NewColorConfigHooks
    {
        public class SlugcatSelectMenuScreenHooks
        {
            public void Init()
            {
                On.Menu.SlugcatSelectMenu.AddColorInterface += On_AddColorInterface;
                On.Menu.SlugcatSelectMenu.RemoveColorInterface += On_RemoveColorInterface;
                On.Menu.SlugcatSelectMenu.Update += On_Update;
                On.Menu.SlugcatSelectMenu.ValueOfSlider += On_ValueOfSlider;
                On.Menu.SlugcatSelectMenu.SliderSetValue += On_SliderSetValue;
            }
            private void On_AddColorInterface(On.Menu.SlugcatSelectMenu.orig_AddColorInterface orig, SlugcatSelectMenu self)
            {
                orig(self);
                if (oOOPages == null)
                {
                    oOOPages = new(self, self.pages[0], self.hueSlider, self.satSlider, self.litSlider, SliderIDGroups, new(0, 10))
                    {
                        showValues = ModOptions.ShowVisual
                    };
                    self.pages[0].subObjects.Add(oOOPages);
                    if (oOOPages.PagesOn)
                    {
                        self.MutualVerticalButtonBind(self.defaultColorButton, oOOPages.PrevButton);
                        self.MutualVerticalButtonBind(oOOPages.NextButton, self.litSlider);
                    }

                }
                if (oOOPages.PagesOn && !ModManager.JollyCoop && !changedPos)
                {
                    changedPos = true;
                    self.defaultColorButton.pos.y -= 40;
                }
                if (ModOptions.enableHexCodeTypers.Value)
                {
                    if (hexInterface == null)
                    {
                        hexInterface = new(self, self.pages[0], self.defaultColorButton.pos + new Vector2(120, 0));
                        self.pages[0].subObjects.Add(hexInterface);
                    }
                }
            }
            private void On_RemoveColorInterface(On.Menu.SlugcatSelectMenu.orig_RemoveColorInterface orig, SlugcatSelectMenu self)
            {
                orig(self);
                changedPos = false;
                if (oOOPages != null)
                {
                    oOOPages.RemoveSprites();
                    self.pages[0].RemoveSubObject(oOOPages);
                    oOOPages = null;
                }
                if (hexInterface != null)
                {
                    hexInterface.RemoveSprites();
                    self.pages[0].RemoveSubObject(hexInterface);
                    hexInterface = null;
                }
            }
            private void On_Update(On.Menu.SlugcatSelectMenu.orig_Update orig, SlugcatSelectMenu self)
            {
                orig(self);
                if (hexInterface != null)
                {
                    hexInterface.SaveNewHSL(SmallUtils.SlugcatSelectMenuHSL(self));
                    if (hexInterface.shouldUpdateNewHSL == true)
                    {
                        SaveHSLString(self, SmallUtils.SetHSLSaveString(hexInterface.newPendingHSL));
                        if (self.hueSlider != null)
                        {
                            self.SliderSetValue(self.hueSlider, self.ValueOfSlider(self.hueSlider));
                        }
                        if (self.satSlider != null)
                        {
                            self.SliderSetValue(self.satSlider, self.ValueOfSlider(self.satSlider));
                        }
                        if (self.litSlider != null)
                        {
                            self.SliderSetValue(self.litSlider, self.ValueOfSlider(self.litSlider));
                        }
                        hexInterface.shouldUpdateNewHSL = false;
                    }
                }
 
            }
            private float On_ValueOfSlider(On.Menu.SlugcatSelectMenu.orig_ValueOfSlider orig, SlugcatSelectMenu self, Slider slider)
            {
                if (ValueOfCustomSliders(self, slider, out float f))
                {
                    return f;
                }
                return orig(self, slider);
            }
            private void On_SliderSetValue(On.Menu.SlugcatSelectMenu.orig_SliderSetValue orig, SlugcatSelectMenu self, Slider slider, float f)
            {
                CustomSlidersSetValue(self, slider, f);
                orig(self, slider, f);
            }
            private void CustomSlidersSetValue(SlugcatSelectMenu ssM, Slider slider, float f)
            {
                Vector3 ssMHSL = SmallUtils.SlugcatSelectMenuHSL(ssM);
                if (slider != null)
                {

                    if (MenuToolObj.RGBSliderIDS.Contains(slider.ID))
                    {
                        Color color = ColConversions.HSL2RGB(ssMHSL);
                        if (slider.ID == MenuToolObj.RedRGB)
                        {
                            color.r = f;
                        }
                        else if (slider.ID == MenuToolObj.GreenRGB)
                        {
                            color.g = f;
                        }
                        else if (slider.ID == MenuToolObj.BlueRGB)
                        {
                            color.b = f;
                        }
                        SmallUtils.Vector32RGB(SmallUtils.RWIIIClamp(SmallUtils.RGB2Vector3(SmallUtils.RGBClamp01(color)),
                            CustomColorModel.RGB, out Vector3 hsl));
                        SaveHSLString(ssM, SmallUtils.SetHSLSaveString(hsl));

                    }
                    if (MenuToolObj.HSVSliderIDS.Contains(slider.ID))
                    {
                        Vector3 hsv = ColConversions.HSL2HSV(ssMHSL);

                        if (slider.ID == MenuToolObj.HueHSV)
                        {
                            hsv.x = Mathf.Clamp(f, 0, 0.99f);
                        }
                        else if (slider.ID == MenuToolObj.SatHSV)
                        {
                            hsv.y = f;
                        }
                        else if (slider.ID == MenuToolObj.ValHSV)
                        {
                            hsv.z = f;
                        }
                        SmallUtils.RWIIIClamp(hsv,
                           CustomColorModel.HSV, out Vector3 newHSL);
                        SaveHSLString(ssM, SmallUtils.SetHSLSaveString(newHSL));
                    }
                }

            }
            private bool ValueOfCustomSliders(SlugcatSelectMenu ssM, Slider slider, out float f)
            {
                f = 0;
                Vector3 ssMHSL = SmallUtils.SlugcatSelectMenuHSL(ssM);
                if (slider != null)
                {
                    if (MenuToolObj.RGBSliderIDS.Contains(slider.ID))
                    {
                        Color color = ColConversions.HSL2RGB(ssMHSL);
                        if (slider.ID == MenuToolObj.RedRGB)
                        {
                            f = color.r;
                            return true;
                        }
                        if (slider.ID == MenuToolObj.GreenRGB)
                        {
                            f = color.g;
                            return true;
                        }
                        if (slider.ID == MenuToolObj.BlueRGB)
                        {
                            f = color.b;
                            return true;
                        }
                    }
                    if (MenuToolObj.HSVSliderIDS.Contains(slider.ID))
                    {
                        Vector3 hsv = ColConversions.HSL2HSV(ssMHSL);
                        if (slider.ID == MenuToolObj.HueHSV)
                        {
                            f = hsv.x;
                            return true;
                        }
                        if (slider.ID == MenuToolObj.SatHSV)
                        {
                            f = hsv.y;
                            return true;
                        }
                        if (slider.ID == MenuToolObj.ValHSV)
                        {
                            f = hsv.z;
                            return true;
                        }
                    }
                }
                return false;
            }
            private void SaveHSLString(SlugcatSelectMenu ssM, string newHSL)
            {
                ssM.manager.rainWorld.progression.miscProgressionData.colorChoices[ssM.slugcatColorOrder[ssM.slugcatPageIndex].value]
                    [ssM.activeColorChooser] = newHSL;
            }
            private List<NewMenuInterfaces.SliderOOOIDGroup> SliderIDGroups
            {
                get
                {
                    List<NewMenuInterfaces.SliderOOOIDGroup> IDGroups = new()
                    { new(MMFEnums.SliderID.Hue, MMFEnums.SliderID.Saturation, MMFEnums.SliderID.Lightness, MenuToolObj.HSLNames, MenuToolObj.hueOOShowInt,
                    MenuToolObj.hueOOMultipler)};
                    if (ModOptions.enableRGBSliders.Value)
                    {
                        IDGroups.Add(new(MenuToolObj.RedRGB, MenuToolObj.GreenRGB, MenuToolObj.BlueRGB,
                            MenuToolObj.RGBNames, MenuToolObj.rgbShowInt, MenuToolObj.rgbMultipler));
                    }
                    if (ModOptions.enableHSVSliders.Value)
                    {
                        IDGroups.Add(new(MenuToolObj.HueHSV, MenuToolObj.SatHSV, MenuToolObj.ValHSV,
                           MenuToolObj.HSVNames, MenuToolObj.hueOOShowInt, MenuToolObj.hueOOMultipler));
                    }
                    return IDGroups;
                }
            }
            private bool changedPos = false;
            private NewMenuInterfaces.HexTypeBox hexInterface;
            private NewMenuInterfaces.SliderOOOPages oOOPages;
        }
        public class OpColorPickerHooks
        {

        }
    }
    public static class NewMenuInterfaces
    {
        public class SliderOOOPages : MenuObject, ICanTurnPages
        {
            public SliderOOOPages(Menu.Menu menu, MenuObject owner, HorizontalSlider slider1, HorizontalSlider slider2, HorizontalSlider slider3, List<SliderOOOIDGroup> sliderOOOIDGroups,Vector2 buttonOffset = default) : base(menu, owner)
            {
                if (slider1 is null || slider2 is null || slider3 is null)
                {
                    ColorConfigMod.DebugError("Sliders in pages are null!");
                    return;
                }
                CurrentOffset = 0;
                sliderO = slider1;
                sliderOO = slider2;
                sliderOOO = slider3;
                OOOIDGroups = sliderOOOIDGroups?.Count == 0? new() 
                { new(slider1.ID, slider2.ID, slider3.ID, null, null)} : sliderOOOIDGroups;
                buttonOffset = buttonOffset == default ? new(0, 0) : buttonOffset;
                setPrevButtonPos = new(buttonOffset.x + slider3.pos.x, -(slider3.size.y * 2) + buttonOffset.y + slider3.pos.y);
                if (PagesOn)
                {
                    ActivateButtons();
                }
            }
            public bool PagesOn
            {
                get => OOOIDGroups?.Count > 1;
            }
            public bool Slider1ShowInt
            { get => OOOIDGroups?[currentOffset]?.showInt1 == true; }
            public bool Slider2ShowInt
            { get => OOOIDGroups?[currentOffset]?.showInt2 == true; }
            public bool Slider3ShowInt
            { get => OOOIDGroups?[currentOffset]?.showInt3 == true; }
            public string[] SliderValues
            {
                get
                {
                    string slider1Amt = "";
                    string slider2Amt = "";
                    string slider3Amt = "";
                    if (OOOIDGroups?.Count > 0 && OOOIDGroups[currentOffset] != null && showValues)
                    {
                        if (sliderO != null)
                        {
                            float value = sliderO.floatValue * OOOIDGroups[currentOffset].showMultipler.x;
                            double amt = Slider1ShowInt ? Math.Round(value, 0, MidpointRounding.AwayFromZero) : Math.Round(value, ModOptions.Digit, MidpointRounding.AwayFromZero);
                            string sign = showSign ? OOOIDGroups[currentOffset].showMultipler.x == 100 ? "%" : OOOIDGroups[currentOffset].showMultipler.x == 360 && Slider1ShowInt ? MenuToolObj.degreeSign : "" : "";
                            slider1Amt = $"{amt}{sign}";
                        }
                        if (sliderOO != null)
                        {
                            float value = sliderOO.floatValue * OOOIDGroups[currentOffset].showMultipler.y;
                            double amt = Slider2ShowInt ? Math.Round(value, 0, MidpointRounding.AwayFromZero) : Math.Round(value, ModOptions.Digit, MidpointRounding.AwayFromZero);
                            string sign = showSign ? OOOIDGroups[currentOffset].showMultipler.y == 100 ? "%" : OOOIDGroups[currentOffset].showMultipler.y == 360 && Slider2ShowInt ? MenuToolObj.degreeSign : "" : "";
                            slider2Amt = $"{amt}{sign}";
                        }
                        if (sliderOOO != null)
                        {
                            float value = sliderOOO.floatValue * OOOIDGroups[currentOffset].showMultipler.z;
                            double amt = Slider3ShowInt ? Math.Round(value, 0, MidpointRounding.AwayFromZero) : Math.Round(value, ModOptions.Digit, MidpointRounding.AwayFromZero);
                            string sign = showSign ? OOOIDGroups[currentOffset].showMultipler.z == 100 ? "%" : OOOIDGroups[currentOffset].showMultipler.y == 360 && Slider3ShowInt ? MenuToolObj.degreeSign : "" : "";
                            slider3Amt = $"{amt}{sign}";
                        }
                    }

                    return new[] { slider1Amt, slider2Amt, slider3Amt };
                }
            }
            public int CurrentOffset
            {
                get
                {
                    return CurrentOffset;
                }
                set
                {
                    if (OOOIDGroups == null || OOOIDGroups.Count == 0 || value > OOOIDGroups.Count - 1)
                    {
                        currentOffset = 0;
                        return;
                    }
                    else if (value < 0)
                    {
                        currentOffset = OOOIDGroups.Count - 1;
                    }
                    currentOffset = value;
                }
            }
            public BigSimpleButton PrevButton
            {
                get => prevButton;
            }
            public BigSimpleButton NextButton
            {
                get => nextButton;
            }
            public void ActivateButtons()
            {
                if (prevButton == null)
                {
                    prevButton = new(menu, this, menu.Translate("Prev"), "_BackPageSliders", setPrevButtonPos, new(40, 26), FLabelAlignment.Center, false);
                    subObjects.Add(prevButton);

                }
                if (nextButton == null)
                {
                    nextButton = new(menu, this, menu.Translate("Next"), "_NextPageSliders", new(setPrevButtonPos.x + 60, setPrevButtonPos.y), new(40, 26), FLabelAlignment.Center, false);
                    subObjects.Add(nextButton);
                }
                menu.MutualHorizontalButtonBind(prevButton, nextButton);
            }
            public void DeactivateButtons()
            {
                if (prevButton != null)
                {
                    prevButton.RemoveSprites();
                    RemoveSubObject(prevButton);
                    prevButton = null;
                }
                if (nextButton != null)
                {
                    nextButton.RemoveSprites();
                    RemoveSubObject(nextButton);
                    nextButton = null;
                }
            }
            public void NextPage()
            {
                CurrentOffset++;
                PopulatePage(CurrentOffset);
            }
            public void PrevPage()
            {
                PopulatePage(CurrentOffset++);
            }
            public void PopulatePage(int offset)
            {
                currentOffset = offset;
                if (OOOIDGroups?.Count > 0 && OOOIDGroups[offset] != null)
                {
                    if (sliderO != null)
                    {
                        sliderO.ID = OOOIDGroups[currentOffset].ID1;
                    }
                    if (sliderOO != null)
                    {
                        sliderOO.ID = OOOIDGroups[currentOffset].ID2;
                    }
                    if (sliderOOO != null)
                    {
                        sliderOOO.ID = OOOIDGroups[currentOffset].ID3;
                    }
                }
            }
            public void ForceOwnerSlider()
            {
                if (sliderO != null && !subObjects.Contains(sliderO))
                {
                    sliderO.menu = menu;
                    sliderO.owner = this;
                    subObjects.Add(sliderO);
                }
                if (sliderOO != null && !subObjects.Contains(sliderOO))
                {
                    sliderOO.menu = owner.menu;
                    sliderOO.owner = this;
                    subObjects.Add(sliderOO);
                }
                if (sliderOOO != null && !subObjects.Contains(sliderOOO))
                {
                    sliderOOO.menu = owner.menu;
                    sliderOOO.owner = this;
                    subObjects.Add(sliderOOO);
                }
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                if (OOOIDGroups != null && OOOIDGroups[currentOffset] != null)
                {
                    if (sliderO != null)
                    {
                        if (sliderO.menuLabel != null)
                        {
                            sliderO.menuLabel.text = menu.Translate(OOOIDGroups[currentOffset].name1) + " " + SliderValues[0];
                        }
                    }
                    if (sliderOO != null)
                    {
                        if (sliderOO.menuLabel != null)
                        {
                            sliderOO.menuLabel.text = menu.Translate(OOOIDGroups[currentOffset].name2) + " " + SliderValues[1];
                        }
                    }
                    if (sliderOOO != null)
                    {
                        if (sliderOOO.menuLabel != null)
                        {
                            sliderOOO.menuLabel.text = menu.Translate(OOOIDGroups[currentOffset].name3) + " " + SliderValues[2];
                        }
                    }
                }
            }
            public override void Singal(MenuObject sender, string message)
            {
                base.Singal(sender, message);
                if (sender == prevButton)
                {
                    menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                    PrevPage();
                }
                if (sender == nextButton)
                {
                    menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                    NextPage();
                }
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                DeactivateButtons();
                if (sliderO != null)
                {
                    sliderO.RemoveSprites();
                    RemoveSubObject(sliderO);
                    sliderO = null;
                }
                if (sliderOO != null)
                {
                    sliderOO.RemoveSprites();
                    RemoveSubObject(sliderOO);
                    sliderOO = null;
                }
                if (sliderOOO != null)
                {
                    sliderOOO.RemoveSprites();
                    RemoveSubObject(sliderOOO);
                    sliderOOO = null;
                }
                if (OOOIDGroups?.Count > 0)
                {
                    OOOIDGroups.Clear();
                    OOOIDGroups = null;
                }
            }

            private int currentOffset;
            public Vector2 setPrevButtonPos;
            public bool showValues = true, showSign = true;
            public HorizontalSlider sliderO, sliderOO, sliderOOO;
            private BigSimpleButton prevButton, nextButton;
            public List<SliderOOOIDGroup> OOOIDGroups { get; private set; }
        }
        public class HexTypeBox : PositionedMenuObject, ICopyPasteConfig
        {
            public HexTypeBox(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
            {
                copying = false;
                pasting = false;
                shouldUpdateNewHSL = false;
                if (tabWrapper == null)
                {
                    tabWrapper = new(menu, this);
                    subObjects.Add(tabWrapper);
                }
                if (hexTyper == null)
                {
                    lastValue = "";
                    hexTyper = new(new Configurable<string>(""), Vector2.zero, 60)
                    {
                        maxLength = 6,
                    };
                }
                if (elementWrapper == null)
                {
                    elementWrapper = new(tabWrapper, hexTyper);
                    subObjects.Add(elementWrapper);
                }
            }

            public string Clipboard 
            { 
                get => GUIUtility.systemCopyBuffer;
                set => GUIUtility.systemCopyBuffer = value;
            }
            public void SaveNewHSL(Vector3 hsl)
            {
                currentHSL = hsl;
            }
            public void Copy()
            {
                MenuToolObj.Clipboard = hexTyper.value;
                menu.PlaySound(SoundID.MENU_Player_Join_Game);
            }
            public void Paste()
            {
                string pendingVal = new(MenuToolObj.Clipboard.Where(x => x != '#').ToArray());
                if (!SmallUtils.IfHexCodeValid(pendingVal, out Color fromPaste))
                {
                    menu.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
                    return;
                }
                hexTyper.value = ColorUtility.ToHtmlStringRGB(SmallUtils.Vector32RGB(
                    SmallUtils.RWIIIClamp(SmallUtils.RGB2Vector3(fromPaste), CustomColorModel.RGB,
                    out Vector3 newClampedHSL)));
                if (newClampedHSL != currentHSL)
                {
                    newPendingHSL = SmallUtils.FixHexSliderWonkiness(newClampedHSL, currentHSL);
                    shouldUpdateNewHSL = true;
                    currentHSL = newPendingHSL;
                    prevHSL = newPendingHSL;
                }
                lastValue = hexTyper.value;
                menu.PlaySound(SoundID.MENU_Switch_Page_In);
            }
            public override void Update()
            {
                base.Update();
                if (hexTyper != null)
                {
                    if (prevHSL != currentHSL)
                    {
                        prevHSL = currentHSL;
                        hexTyper.value = ColConversions.HSL2Hex(currentHSL);
                        lastValue = hexTyper.value;
                    }
                    if (!hexTyper.held)
                    {
                        if (hexTyper.value != lastValue)
                        {
                            if (!SmallUtils.IfHexCodeValid(hexTyper.value, out Color hexCol))
                            {
                                Debug.LogError($"Failed to parse from new value \"{hexTyper.value}\"");
                                hexTyper.value = lastValue;
                                return;
                            }
                            hexTyper.value = ColorUtility.ToHtmlStringRGB(
                                SmallUtils.Vector32RGB(SmallUtils.RWIIIClamp(
                                    SmallUtils.RGB2Vector3(hexCol), CustomColorModel.RGB, out Vector3 clampedHSLHex)));
                            if (clampedHSLHex != currentHSL)
                            {
                                newPendingHSL = SmallUtils.FixHexSliderWonkiness(clampedHSLHex, currentHSL);
                                shouldUpdateNewHSL = true;
                                currentHSL = newPendingHSL;
                                prevHSL = newPendingHSL;
                            }
                            lastValue = hexTyper.value;
                        }
                    }
                    if (hexTyper.MouseOver)
                    {
                        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C) && !copying && !pasting)
                        {
                            copying = true;
                            Copy();
                            copying = false;
                        }
                        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.V) && !pasting && !copying)
                        {
                            pasting = true;
                            Paste();
                            pasting = false;
                        }
                    }
                }
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                if (elementWrapper != null)
                {
                    elementWrapper.RemoveSprites();
                    RemoveSubObject(elementWrapper);
                    elementWrapper = null;
                }
                if (hexTyper != null)
                {
                    hexTyper.label.alpha = 0;
                    hexTyper.label.RemoveFromContainer();
                    hexTyper.rect.container.RemoveFromContainer();
                    hexTyper = null;
                }
                if (tabWrapper != null)
                {
                    RemoveSubObject(tabWrapper);
                    tabWrapper.RemoveSprites();
                    tabWrapper = null;
                }
            }

            public string lastValue;
            public bool shouldUpdateNewHSL, copying, pasting;
            public Vector3 currentHSL, prevHSL, newPendingHSL;
            public MenuTabWrapper tabWrapper;
            public UIelementWrapper elementWrapper;
            public OpTextBox hexTyper;
        }
        public class SliderOOOIDGroup
        {
            public SliderOOOIDGroup(Slider.SliderID sliderID1, Slider.SliderID sliderID2, Slider.SliderID sliderID3,
                string[] names,bool[] showInts, Vector3 multipler = default)
            {
                ID1 = sliderID1;
                ID2 = sliderID2;
                ID3 = sliderID3;
                name1 = names?.Length > 0? names[0] : "";
                name2 = names?.Length > 1? names[1] : "";
                name3 = names?.Length > 2? names[2] : "";
                showInt1 = showInts?.Length > 0 && showInts[0];
                showInt2 = showInts?.Length > 1 && showInts[1];
                showInt3 = showInts?.Length > 2 && showInts[2];
                showMultipler = multipler == default? new(1, 1, 1) : multipler;
            }
            public List<Slider.SliderID> SliderIDs
            {
                get => new() { ID1, ID2, ID3 };
            }
            public Vector3 showMultipler;
            public Slider.SliderID ID1, ID2, ID3;
            public string name1, name2, name3;
            public bool showInt1, showInt2, showInt3;
        }
        public interface ICanTurnPages
        {
            bool PagesOn { get;}
            int CurrentOffset { get; set; }
            void PopulatePage(int offset);
            void NextPage();
            void PrevPage();
        }
        public interface ICopyPasteConfig
        {
            string Clipboard { get; set; }
            void Copy();
            void Paste();
        }
    }
}
