using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;
using static ColorConfig.MenuToolObj;
using static ColorConfig.ColConversions;
using System.Globalization;

namespace ColorConfig
{
    public static class MenuInterfaces
    {
        public class JollyPageSliderInterface : PositionedMenuObject
        {
            public JollyPageSliderInterface(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 sliderSize, int bodyPart) : base(menu, owner, pos)
            {
                sliderSizes = sliderSize;
                sliderIDs ??= new();
                if (sliderPages == null)
                {
                    sliderPages = new(menu, this, Vector2.zero, sliderSize, 5, showAmtAll: false);
                    subObjects.Add(sliderPages);
                }
                if (ModOptions.EnableSliders)
                {
                    if (ModOptions.enableRGBSliders.Value)
                    {
                        if (rgbInterface == null)
                        {
                            Slider.SliderID redID = new($"{bodyPart}_DUSTY_RGB_RED", true);
                            Slider.SliderID greenID = new($"{bodyPart}_DUSTY_RGB_GREEN", true);
                            Slider.SliderID blueID = new($"{bodyPart}_DUSTY_RGB_BLUE", true);
                            sliderIDs.AddRange(new[] { redID, greenID, blueID });
                            rgbInterface = new(menu, sliderPages, Vector2.zero, sliderSize, new[] { redID, greenID, blueID }, new[] { red, green, blue },
                                showAmt: ModOptions.ShowVisual, showInt: rgbShowInt, showMultipler: rgbMultipler);
                            sliderPages.AddSliderInterface(rgbInterface, "", false);
                        }
                    }
                    if (ModOptions.enableHSVSliders.Value)
                    {
                        if (hsvInterface == null)
                        {
                            Slider.SliderID hsvHue = new($"{bodyPart}_DUSTY_HSV_HUE", true);
                            Slider.SliderID hsvSat = new($"{bodyPart}_DUSTY_HSV_SAT", true);
                            Slider.SliderID hsvLit = new($"{bodyPart}_DUSTY_HSV_VALUE", true);
                            sliderIDs.AddRange(new[] { hsvHue, hsvSat, hsvLit });
                            hsvInterface = new(menu, sliderPages, Vector2.zero, sliderSize, new[] { hsvHue, hsvSat, hsvLit }, new[] { hue, sat, value },
                                showAmt: ModOptions.ShowVisual, showInt: MenuToolObj.hueOOShowInt, showMultipler: MenuToolObj.hueOOMultipler);
                            sliderPages.AddSliderInterface(hsvInterface, "", false);
                        }
                    }
                }
                
                sliderAmtLabel = new(menu, this, "", new(sliderSize.x / 3 * 1.7f, 23.5f), new(80, 30), false);
                subObjects.Add(sliderAmtLabel);
            }
            public void AddHSLInterface(HorizontalSlider hue, HorizontalSlider sat, HorizontalSlider lit, int bodyPart, bool isRGB = false)
            {
                if (hue != null && sat != null && lit != null && sliderPages != null && hslInterface == null)
                {
                    if (!isRGB)
                    {

                        hslInterface = new(menu, sliderPages, Vector2.zero, sliderSizes, new[] { hue.ID, sat.ID, lit.ID },
                       new[] { "HUE", "SAT", "LIT" }, showAmt: ModOptions.ShowVisual, showInt: hueOOShowInt,
                       showMultipler: hueOOMultipler);
                    }
                    else
                    {
                        Slider.SliderID redID = new($"{bodyPart}_DUSTY_RGB_RED", true);
                        Slider.SliderID greenID = new($"{bodyPart}_DUSTY_RGB_GREEN", true);
                        Slider.SliderID blueID = new($"{bodyPart}_DUSTY_RGB_BLUE", true);

                        sliderIDs.AddRange(new[] { redID, greenID, blueID });

                        hue.ID = redID;
                        sat.ID = greenID;
                        lit.ID = blueID;

                        hslInterface = new(menu, sliderPages, Vector2.zero, sliderSizes, new[] { hue.ID, sat.ID, lit.ID },
                       new[] { "RED", "GREEN", "BLUE" }, showAmt: ModOptions.ShowVisual, showInt: rgbShowInt,
                       showMultipler: rgbMultipler);
                    }
                    hslInterface.Deactivate();
                    hslInterface.slider1 = hue;
                    hslInterface.slider2 = sat;
                    hslInterface.slider3 = lit;
                    hslInterface.ForceOwnerSlider();
                    sliderPages.AddSliderInterface(hslInterface);
                    if (!ModOptions.EnableJollyPages)
                    {
                        sliderPages.DeactivateSliderInterface();
                        sliderPages.ActivateSliderInterface();
                        sliderPages.DeactivateButtons();
                    }
                }
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                if (sliderAmtLabel != null)
                {
                    sliderAmtLabel.RemoveSprites();
                    RemoveSubObject(sliderAmtLabel);
                    sliderAmtLabel = null;
                }
                if (sliderPages != null)
                {
                    sliderPages.RemoveSprites();
                    RemoveSubObject(sliderPages);
                    sliderPages = null;
                }
                if (sliderIDs != null)
                {
                    sliderIDs.ForEach(x => x.Unregister());
                    sliderIDs = null;
                }
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                if (sliderAmtLabel != null)
                {
                    sliderAmtLabel.text = "";
                    sliderAmtLabel.label.color = colorText;
                    if (ModOptions.ShowVisual)
                    {
                        if (sliderPages?.SliderInterfaces != null)
                        {
                            if (sliderPages.SliderInterfaces.Count > 0)
                            {
                                string[] sliderAmts = sliderPages.SliderInterfaces[sliderPages.currentOffset].SliderStringAmts();

                                sliderAmtLabel.text = $"({sliderAmts[0]}, {sliderAmts[1]}, {sliderAmts[2]})";
                            }
                        }
                    }
                }
               
            }
            public List<Slider.SliderID> SliderIDs
            { get => sliderIDs; }

            public Color colorText = MenuColorEffect.rgbMediumGrey;
            private Vector2 sliderSizes;
            private MenuLabel sliderAmtLabel;
            private SlidersInterfacePages sliderPages;
            private List<Slider.SliderID> sliderIDs;
            private SliderIIIInterface rgbInterface, hslInterface, hsvInterface;
        }
        public class SlugcatDisplay : RectangularMenuObject
        {
            public SlugcatDisplay(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, SlugcatStats.Name current, List<string> bodyCols) : base(menu, owner, pos, size)
            {
                currentSlugcat = current;
                lastSlugcat = current;
                currentBodyNames = PlayerGraphics.ColoredBodyPartList(current);
                currentBodyColors = bodyCols;
                slugcatSprites = new();
                LoadIcon(current, currentBodyNames);

            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                if (slugcatSprites?.Count > 0)
                {
                    foreach (MenuIllustration illu in slugcatSprites)
                    {
                        illu.RemoveSprites();
                        RemoveSubObject(illu);
                    }
                    slugcatSprites = null;
                }
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                if (lastSlugcat != currentSlugcat)
                {
                    lastSlugcat = currentSlugcat;
                    LoadIcon(currentSlugcat, currentBodyNames);
                }
                if (slugcatSprites?.Count > 0 && currentHSLs != null)
                {
                    if (currentHSLs != lastHSLs)
                    {
                        lastHSLs = currentHSLs;
                        for (int i = 0; i < currentHSLs.Count; i++)
                        {
                            if (slugcatSprites.Count - 1 < i)
                            {
                                continue;
                            }
                            slugcatSprites[i].color = HSL2RGB(currentHSLs[i]);
                        }
                    }
                }
            }
            public void LoadIcon(SlugcatStats.Name name, List<string> bodyNames)
            {

                if (slugcatSprites?.Count > 0)
                {
                    foreach (MenuIllustration slugcatSprite in slugcatSprites)
                    {
                        slugcatSprite.RemoveSprites();
                        RemoveSubObject(slugcatSprite);
                    }
                }
                slugcatSprites = new();
                LoadSlugcatImage(name, bodyNames);
                currentHSLs = lastHSLs;
            }
            public void LoadNewHSL(List<string> slugcatColos, SlugcatStats.Name name)
            {
                currentBodyColors = slugcatColos;
                currentSlugcat = name;
                currentBodyNames = PlayerGraphics.ColoredBodyPartList(name);
                List<Vector3> hsls = new();
                for (int i = 0; i < currentBodyNames.Count; i++)
                {
                    hsls.Add(new(1, 1, 1));
                }
                for (int i = 0; i < currentBodyColors.Count; i++)
                {
                    if (currentBodyColors[i].Contains(","))
                    {
                        string[] hslArray = currentBodyColors[i].Split(',');

                        hsls[i] = new Vector3(float.Parse(hslArray[0], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(hslArray[1], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(hslArray[2], NumberStyles.Any, CultureInfo.InvariantCulture));
                    }
                }
                currentHSLs = hsls;
            }
            public void LoadSlugcatImage(SlugcatStats.Name name, List<string> bodyNames)
            {
                try
                {
                    string file = GetPupButtonOffName(name);
                    List<string> bodyFiles = new()
                    {
                        file,
                        "face_" + file,
                    };
                    if (bodyNames.Count > 2)
                    {
                        bodyFiles.Add("unique_" + file);
                        if (bodyNames.Count > 3)
                        {
                            bodyFiles.Add(FindExtras(bodyNames, file));
                        }
                    }
                    for (int i = 0; i < bodyFiles.Count; i++)
                    {
                        MenuIllustration sprites = new(menu, this, "", bodyFiles[i], size / 2, true, true);

                        slugcatSprites.Add(sprites);
                        subObjects.Add(sprites);
                    }
                }
                catch (Exception e)
                {
                    ColorConfigMod.DebugException("Failed to change new sprites", e);
                }
            }
            public string FindExtras(List<string> bodyNames, string file)
            {
                for (int i = 3; i < bodyNames.Count; i++)
                {
                    string test1 = $"{bodyNames[i]}_" + file;
                    string test2 = $"unique{i - 1}_" + file;
                    string test3 = $"unique{i - 2}_" + file;
                    bool startAt2 = false;

                    if (File.Exists(AssetManager.ResolveFilePath("Illustrations/" + test1 + ".png")))
                    {
                        return test1;
                    }
                    else if (i == 3 && File.Exists(AssetManager.ResolveFilePath("Illustrations/" + test2 + file)) && !File.Exists(AssetManager.ResolveFilePath("Illustrations/" + test3 + file)))
                    {
                        if (i == 3 && !File.Exists(AssetManager.ResolveFilePath("Illustrations/" + test3 + file)))
                        {
                            startAt2 = true;
                        }
                    }
                    if (startAt2)
                    {
                        return test2;
                    }
                    else if (File.Exists(AssetManager.ResolveFilePath("Illustrations/" + test3 + file)))
                    {
                        return test3;
                    }
                }
                return "empty";
            }
            public string GetPupButtonOffName(SlugcatStats.Name name)
            {
                string getFromJolly = "pup_off";
                if (name != null && !ignoredSlugcats.Contains(name.value))
                {
                    string varied = name.value + "_" + getFromJolly;
                    if (File.Exists(AssetManager.ResolveFilePath("Illustrations" + "/" + varied + ".png")))
                    {
                        return varied;
                    }
                }
                return getFromJolly;

            }
            public List<Vector3> currentHSLs, lastHSLs;
            public SlugcatStats.Name currentSlugcat, lastSlugcat;
            public List<MenuIllustration> slugcatSprites;
            public List<string> currentBodyNames;
            public List<string> currentBodyColors;
            public readonly List<string> ignoredSlugcats = new()
            {
                "White",
                "Yellow",
                "Red",
            };
        }
        public class SlidersInterfacePages : PositionedMenuObject
        {
            public SlidersInterfacePages(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 sliderSizes, float xButtonOffset = 0, float yButtonOffset = 0, bool allSubtle = false, bool sliderVertAlignment = true, bool showAmtAll = true) : base(menu, owner, pos)
            {
                sliderSize = sliderSizes;
                subtle = allSubtle;
                showAmt = showAmtAll;
                currentOffset = 0;
                verticalAlignment = sliderVertAlignment;
                sliderIIIInterfaces = new();
                Vector2 vector = Vector2.zero;
                vector.x += xButtonOffset;
                vector.y += -((verticalAlignment ? sliderSize.y * 2 : sliderSize.y) + 48.5f) + yButtonOffset;
                buttonPos = vector;
                ActivateButtons();
                if (sliderNameLabel == null)
                {
                    sliderNameLabel = new(menu, this, "", new(sliderSize.x / 2, 100), new(80, 50), true);
                    subObjects.Add(sliderNameLabel);
                }
            }
            public void AddSliderInterface(SliderIIIInterface iIIInterface, string name = "", bool changePos = true)
            {
                if (sliderIIIInterfaces != null)
                {
                    iIIInterface.menu = menu;
                    iIIInterface.owner = this;
                    if (changePos)
                    {
                        iIIInterface.pos = Vector2.zero;
                    }
                    iIIInterface.showSliderAmt = showAmt;
                    sliderIIIInterfaces.Add(iIIInterface, name);
                    subObjects.Add(iIIInterface);
                    if (SliderInterfaces[currentOffset] != iIIInterface)
                    {
                        iIIInterface.Deactivate();
                        subObjects.Remove(iIIInterface);
                    }
                }
            }
            public void ActivateSliderInterface()
            {
                if (sliderIIIInterfaces != null)
                {
                    SliderInterfaces[currentOffset].Activate();
                    subObjects.Add(SliderInterfaces[currentOffset]);
                }
            }
            public void DeactivateSliderInterface()
            {
                if (sliderIIIInterfaces != null)
                {
                    SliderInterfaces[currentOffset].Deactivate();
                    RemoveSubObject(SliderInterfaces[currentOffset]);
                }
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
            public void ActivateButtons()
            {
                if (prevButton == null)
                {
                    prevButton = new(menu, this, menu.Translate("Prev"), "_BackPageSliders", buttonPos, new(40, 26), FLabelAlignment.Center, false);
                    subObjects.Add(prevButton);
                }
                if (nextButton == null)
                {
                    nextButton = new(menu, this, menu.Translate("Next"), "_NextPageSliders", new(buttonPos.x + 60, buttonPos.y), new(40, 26), FLabelAlignment.Center, false);
                    subObjects.Add(nextButton);
                }
            }
            public void PopulateSliderInterface(int offset)
            {
                if (sliderIIIInterfaces != null)
                {
                    DeactivateSliderInterface();
                    currentOffset = offset;
                    ActivateSliderInterface();
                }
            }
            public void PrevPage()
            {
                int offset = currentOffset - 1;
                if (offset < 0)
                {
                    offset = sliderIIIInterfaces.Count - 1;
                }
                PopulateSliderInterface(offset);
            }
            public void NextPage()
            {
                int offset = currentOffset + 1;
                if (offset >= sliderIIIInterfaces.Count)
                {
                    offset = 0;
                }
                PopulateSliderInterface(offset);
            }
            public override void Update()
            {
                base.Update();
                foreach (SliderIIIInterface sliderIIIInterface in sliderIIIInterfaces.Keys)
                {
                    sliderIIIInterface.showSliderAmt = showAmt;
                }
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                if (sliderNameLabel != null && showSliderName)
                {
                    sliderNameLabel.text = "";
                    if (SliderInterfaces?.Count > 0)
                    {
                        string newText = menu.Translate(sliderIIIInterfaces[SliderInterfaces[currentOffset]]);
                        if (sliderNameLabel.text != newText)
                        {
                            sliderNameLabel.text = newText;
                        }
                    }
                }
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                if (sliderNameLabel != null)
                {
                    sliderNameLabel.RemoveSprites();
                    RemoveSubObject(sliderNameLabel);
                    sliderNameLabel = null;
                }
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
                if (sliderIIIInterfaces != null)
                {
                    SliderInterfaces.ForEach(x => x.RemoveSprites());
                    SliderInterfaces.ForEach(RemoveSubObject);
                    SliderInterfaces.ForEach(x => x = null);
                    sliderIIIInterfaces = null;
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

            public List<SliderIIIInterface> SliderInterfaces
            { get => sliderIIIInterfaces.Keys.ToList(); }

            public int currentOffset;
            public Vector2 sliderSize, buttonPos;
            public bool subtle, verticalAlignment, showAmt, showSliderName = false;
            public MenuLabel sliderNameLabel;
            public BigSimpleButton prevButton, nextButton;
            private Dictionary<SliderIIIInterface, string> sliderIIIInterfaces;
        }
        public class SliderIIIInterface : PositionedMenuObject
        {
            public SliderIIIInterface(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, Slider.SliderID[] sliderIDs, string[] names, bool verticalAlignment = true, bool allSubtle = false,
                bool showAmt = true, bool[] showInt = default, Vector3 showMultipler = default) : base(menu, owner, pos)
            {
                if (sliderIDs.Length < 3)
                {
                    return;
                }
                vert = verticalAlignment;
                this.sliderIDs = sliderIDs;
                sliderNames = names != null ? names.Length < 3 ? names.Length < 2 ? names.Length < 1? new[] { "", "", "" } : 
                    new[] { names[0], "", "" } : new[] { names[0], names[1], "" } : names : new[] {"", "", ""};
                this.showMultipler = showMultipler == default ? new(1, 1, 1) : showMultipler;
                showSliderAmt = showAmt;
                this.showInt = showInt;
                subtle = allSubtle;
                sliderSize = size;
                Vector2 divide = verticalAlignment ? new(0, -(size.y + 10)) : new(size.x + 10, 0);
                posX = new(0, divide.x, divide.x * 2);
                posY = new(0, divide.y, divide.y * 2);
                if (slider1 == null)
                {
                    slider1 = new(menu, this, menu.Translate(sliderNames[0]), Vector2.zero, size, sliderIDs[0], allSubtle);
                    subObjects.Add(slider1);
                }
                if (slider2 == null)
                {
                    slider2 = new(menu, this, menu.Translate(sliderNames[1]), Vector2.zero + divide, size, sliderIDs[1], allSubtle);
                    subObjects.Add(slider2);
                }
                if (slider3 == null)
                {
                    slider3 = new(menu, this, menu.Translate(sliderNames[2]), Vector2.zero + (divide * 2), size, sliderIDs[2], allSubtle);
                    subObjects.Add(slider3);
                }
                if (verticalAlignment)
                {
                    menu.MutualVerticalButtonBind(slider3, slider1);
                    menu.MutualVerticalButtonBind(slider2, slider1);
                }
            }
            public bool Slider1ShowInt
            { get => showInt?.Length > 0 && showInt[0]; }
            public bool Slider2ShowInt
            { get => showInt?.Length > 1 && showInt[1]; }
            public bool Slider3ShowInt
            { get => showInt?.Length > 2 && showInt[2]; }
            public bool Deactivated
            { get; private set; } = false;
            public Slider.SliderID[] SliderIDs
            { get => sliderIDs; }
            public string[] SliderStringAmts(bool showSign = true)
            {
                string slider1Amt = "";
                string slider2Amt = "";
                string slider3Amt = "";
                if (slider1 != null)
                {
                    float value = slider1.floatValue * showMultipler.x;
                    double amt = Slider1ShowInt ? Math.Round(value, 0, MidpointRounding.AwayFromZero) : Math.Round(value, ModOptions.Digit, MidpointRounding.AwayFromZero);
                    string sign = showSign ? showMultipler.x == 100 ? "%" : showMultipler.x == 360 && Slider1ShowInt ? degreeSign : "" : "";
                    slider1Amt = $"{amt}{sign}";
                }
                if (slider2 != null)
                {
                    float value = slider2.floatValue * showMultipler.y;
                    double amt = Slider2ShowInt ? Math.Round(value, 0, MidpointRounding.AwayFromZero) : Math.Round(value, ModOptions.Digit, MidpointRounding.AwayFromZero);
                    string sign = showSign ? showMultipler.y == 100 ? "%" : showMultipler.y == 360 && Slider2ShowInt ? degreeSign : "" : "";
                    slider2Amt = $"{amt}{sign}";
                }
                if (slider3 != null)
                {
                    float value = slider3.floatValue * showMultipler.z;
                    double amt = Slider3ShowInt ? Math.Round(value, 0, MidpointRounding.AwayFromZero) : Math.Round(value, ModOptions.Digit, MidpointRounding.AwayFromZero);
                    string sign = showSign ? showMultipler.z == 100 ? "%" : showMultipler.y == 360 && Slider3ShowInt ? degreeSign : "" : "";
                    slider3Amt = $"{amt}{sign}";
                }

                return new[] { slider1Amt, slider2Amt, slider3Amt };

            }
            public void Deactivate()
            {
                Deactivated = true;
                if (slider1 != null)
                {
                    slider1.RemoveSprites();
                    RemoveSubObject(slider1);
                    slider1 = null;
                }
                if (slider2 != null)
                {
                    slider2.RemoveSprites();
                    RemoveSubObject(slider2);
                    slider2 = null;
                }
                if (slider3 != null)
                {
                    slider3.RemoveSprites();
                    RemoveSubObject(slider3);
                    slider3 = null;
                }
            }
            public void Activate()
            {
                Deactivated = false;
                if (slider1 == null)
                {
                    slider1 = new(menu, this, menu.Translate(sliderNames[0]), new(posX.x, posY.x), sliderSize, sliderIDs[0], subtle);
                    subObjects.Add(slider1);
                }
                if (slider2 == null)
                {
                    slider2 = new(menu, this, menu.Translate(sliderNames[1]), new(posX.y, posY.y), sliderSize, sliderIDs[1], subtle);
                    subObjects.Add(slider2);
                }
                if (slider3 == null)
                {
                    slider3 = new(menu, this, menu.Translate(sliderNames[2]), new(posX.z, posY.z), sliderSize, sliderIDs[2], subtle);
                    subObjects.Add(slider3);
                }
                if (vert)
                {
                    menu.MutualVerticalButtonBind(slider3, slider1);
                    menu.MutualVerticalButtonBind(slider2, slider1);
                }
            }
            public void ForceOwnerSlider()
            {
                if (slider1 != null && !subObjects.Contains(slider1))
                {
                    slider1.menu = menu;
                    slider1.owner = this;
                    subObjects.Add(slider1);
                }
                if (slider2 != null && !subObjects.Contains(slider2))
                {
                    slider2.menu = owner.menu;
                    slider2.owner = this;
                    subObjects.Add(slider2);
                }
                if (slider3 != null && !subObjects.Contains(slider3))
                {
                    slider3.menu = owner.menu;
                    slider3.owner = this;
                    subObjects.Add(slider3);
                }
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                Deactivate();
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                if (showSliderAmt && !Deactivated)
                {
                    if (slider1 != null)
                    {
                        if (slider1.menuLabel != null)
                        {
                            slider1.menuLabel.text = menu.Translate(sliderNames[0]) + " " + SliderStringAmts(showSigns)[0];
                        }
                    }
                    if (slider2 != null)
                    {
                        if (slider2.menuLabel != null)
                        {
                            slider2.menuLabel.text = menu.Translate(sliderNames[1]) + " " + SliderStringAmts(showSigns)[1];
                        }
                    }
                    if (slider3 != null)
                    {
                        if (slider3.menuLabel != null)
                        {
                            slider3.menuLabel.text = menu.Translate(sliderNames[2]) + " " + SliderStringAmts(showSigns)[2];
                        }
                    }

                }
            }

            public string[] sliderNames;
            public bool subtle, showSliderAmt, showSigns = true, copying = false, pasting = false;
            private bool vert;
            public bool[] showInt;
            public Vector2 sliderSize;
            public Vector3 posX, posY;
            public Vector3 showMultipler;
            private Slider.SliderID[] sliderIDs;
            public HorizontalSlider slider1, slider2, slider3;
        }
        public class HexInterface : PositionedMenuObject, ICopyPasteConfig
        {
            public HexInterface(Menu.Menu menu, MenuObject owner, Vector2 pos, bool showLabel = true) : base(menu, owner, pos)
            {
                Vector2 vector = Vector2.zero;
                if (tabWrapper == null)
                {
                    tabWrapper = new(menu, this);
                    subObjects.Add(tabWrapper);
                }
                if (showLabel && label == null)
                {
                    label = new(new(vector.x, vector.y - 0.6f), new(30, 30), menu.Translate("HEX"), FLabelAlignment.Left);
                    vector[0] += 34;

                }
                if (showLabel && labelWrapper == null)
                {
                    labelWrapper = new(tabWrapper, label);
                    subObjects.Add(labelWrapper);
                }
                if (hexTyper == null)
                {
                    Configurable<string> hexConfig = new("");
                    hexTyper = new(hexConfig, vector, 60)
                    {
                        maxLength = 6,
                    };
                }
                if (typerWrapper == null)
                {
                    typerWrapper = new(tabWrapper, hexTyper);
                    subObjects.Add(typerWrapper);
                }
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                if (typerWrapper != null)
                {
                    typerWrapper.RemoveSprites();
                    RemoveSubObject(typerWrapper);
                    typerWrapper = null;
                }
                if (hexTyper != null)
                {
                    hexTyper.label.alpha = 0;
                    hexTyper.label.RemoveFromContainer();
                    hexTyper.rect.Hide();
                    hexTyper = null;
                }
                if (labelWrapper != null)
                {
                    labelWrapper.RemoveSprites();
                    RemoveSubObject(labelWrapper);
                    labelWrapper = null;
                }
                if (label != null)
                {
                    label.label.RemoveFromContainer();
                    label.Hide();
                    label = null;
                }
                if (tabWrapper != null)
                {
                    RemoveSubObject(tabWrapper);
                    tabWrapper.RemoveSprites();
                    tabWrapper = null;
                }
            }
            public override void Update()
            {
                base.Update();
                if (hexTyper != null)
                {
                    if (!hasSet || currentHSLColor != prevHSLColor || forceUpdate)
                    {
                        hasSet = true;
                        forceUpdate = false;
                        prevHSLColor = currentHSLColor;
                        hexTyper.value = HSL2Hex(currentHSLColor);
                        lastValue = hexTyper.value;
                    }
                    if (!hexTyper.held)
                    {
                        if (lastValue != hexTyper.value)
                        {
                            if (!SmallUtils.IfHexCodeValid(hexTyper.value, out Color hexCol))
                            {
                                Debug.LogError($"Failed to parse from new value \"{hexTyper.value}\"");
                                hexTyper.value = lastValue;
                                return;
                            }
                            Vector3 hslFromHex = SmallUtils.HOOClamp(Custom.RGB2HSL(hexCol), new(0, 0, 0.01f), new(1, 1, 1));
                            hexTyper.value = HSL2Hex(hslFromHex);
                            if (hslFromHex != currentHSLColor)
                            {
                                pendingNewHSL = SmallUtils.FixHexSliderWonkiness(hslFromHex, currentHSLColor);
                                updateCol = true;
                                currentHSLColor = hslFromHex;
                                prevHSLColor = hslFromHex;
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
            public void Copy()
            {
                Clipboard = hexTyper.value;
                menu.PlaySound(SoundID.MENU_Player_Join_Game);
            }
            public void Paste()
            {
                string pendingVal = new(Clipboard.Where(x => x != '#').ToArray());
                if (!SmallUtils.IfHexCodeValid(pendingVal, out Color fromPaste))
                {
                    menu.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
                    return;
                }
                Vector3 hslFromPaste = SmallUtils.HOOClamp(Custom.RGB2HSL(fromPaste), new(0, 0, 0.01f), new(1, 1, 1));
                hexTyper.value = HSL2Hex(hslFromPaste);
                if (hslFromPaste != currentHSLColor)
                {
                    pendingNewHSL = SmallUtils.FixHexSliderWonkiness(hslFromPaste, currentHSLColor);
                    updateCol = true;
                    currentHSLColor = hslFromPaste;
                    prevHSLColor = hslFromPaste;
                }
                lastValue = hexTyper.value;
                menu.PlaySound(SoundID.MENU_Switch_Page_In);

            }
            public void SetNewHSLColor(Vector3 hsl)
            {
                currentHSLColor = hsl;
            }

            public string lastValue;
            public bool hasSet = false, forceUpdate = false, updateCol = false, copying = false, pasting = false;
            public Vector3 prevHSLColor, currentHSLColor, pendingNewHSL;
            public OpLabel label;
            public OpTextBox hexTyper;
            public UIelementWrapper typerWrapper, labelWrapper;
            public MenuTabWrapper tabWrapper;
        }
        public interface ICopyPasteConfig
        {
            void Copy();
            void Paste();
        }
    }
}
