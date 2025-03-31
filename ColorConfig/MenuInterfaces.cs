using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using JollyCoop.JollyMenu;
using RWCustom;
using UnityEngine;
using static ColorConfig.MenuInterfaces;

namespace ColorConfig
{
    public static class MenuInterfaces
    {
        //menu objects
        public class ExpeditionColorDialog : DialogNotify, CheckBox.IOwnCheckBox
        {
            //if remix is on
            public Page ColorPage => pages[0];
            public List<SliderIDGroup> SliderIDGroups
            {
                get
                {
                    List<SliderIDGroup> idGroup = [];
                    SmallUtils.AddEXPSliderIDGroups(idGroup, ModOptions.ShouldRemoveHSLSliders);
                    return idGroup;
                }
            }
            public float SizeXOfDefaultCol => CurrLang == InGameTranslator.LanguageID.Japanese || CurrLang == InGameTranslator.LanguageID.French ? 110f : CurrLang == InGameTranslator.LanguageID.Italian || CurrLang == InGameTranslator.LanguageID.Spanish ? 180 : 110;
            public ExpeditionColorDialog(Menu.Menu translator, SlugcatStats.Name name, Action action, bool? openHexInterface = null, bool? clampHue = null, bool? showSlugcatDisplay = null) : base("", translator.Translate("Custom colors"), new(500, 400), translator.manager, action)
            {
                shouldClampHue = clampHue.GetValueOrDefault(!ModOptions.DisableHueSliderMaxClamp.Value);
                openHex = openHexInterface.GetValueOrDefault(ModOptions.EnableHexCodeTypers.Value);
                showDisplay = showSlugcatDisplay.GetValueOrDefault(ModOptions.EnableSlugcatDisplay.Value);
                id = name;
                sliderSize = new(200, 30);
                offset = new(0, -40);
                colorChooser = -1;
                GetSaveColorEnabled();
                colorCheckbox = new(this, ColorPage, this, new(size.x + 40, size.y + offset.y * 5), 0, "", colorCheckboxSingal);
                ColorPage.subObjects.Add(colorCheckbox);
                colorCheckbox.MutualMenuObjectBind(okButton, true, true);
            }
            public override void ShutDownProcess()
            {
                base.ShutDownProcess();
                RemoveColorButtons();
            }
            public override void Update()
            {
                base.Update();
                hexTypeBox?.SaveNewHSL(this.ExpeditionHSL());
            }
            public override void Singal(MenuObject sender, string message)
            {
                base.Singal(sender, message);
                if (message == "DEFAULTCOL")
                {
                    PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                    manager.rainWorld.progression.miscProgressionData.colorChoices[id.value][colorChooser] = bodyInterface.defaultColors[colorChooser];
                }
                if (message.StartsWith(ExpeditionColorInterface.OPENINTERFACESINGAL) && int.TryParse(message.Substring(ExpeditionColorInterface.OPENINTERFACESINGAL.Length), NumberStyles.Any, CultureInfo.InvariantCulture, out int result))
                {
                    AddOrRemoveColorInterface(result);
                }
            }
            public override void SliderSetValue(Slider slider, float f)
            {
                MenuToolObj.CustomSliderSetHSL(slider, f, this.ExpeditionHSL(), SaveHSLString, shouldClampHue);
            }
            public override float ValueOfSlider(Slider slider)
            {
                if (MenuToolObj.CustomHSLValueOfSlider(slider, this.ExpeditionHSL(), out float f))
                {
                    return f;
                }
                return 0;

            }
            public bool GetChecked(CheckBox box)
            {
                return colorChecked;
            }
            public void SetChecked(CheckBox box, bool c)
            {
                SaveColorChoicesEnabled(c);
            }
            public void GetSaveColorEnabled()
            {
                manager.rainWorld.progression.miscProgressionData.colorChoices ??= [];
                manager.rainWorld.progression.miscProgressionData.colorsEnabled ??= [];
                if (!manager.rainWorld.progression.miscProgressionData.colorsEnabled.ContainsKey(id.value))
                {
                    manager.rainWorld.progression.miscProgressionData.colorsEnabled.Add(id.value, colorChecked);
                    return;
                }
                colorChecked = manager.rainWorld.progression.miscProgressionData.colorsEnabled[id.value];
                ((Action)(colorChecked ? AddColorButtons : null))?.Invoke();
            }
            public void SaveColorChoicesEnabled(bool colorChecked)
            {
                manager.rainWorld.progression.miscProgressionData.colorChoices ??= [];
                manager.rainWorld.progression.miscProgressionData.colorsEnabled ??= [];
                if (!manager.rainWorld.progression.miscProgressionData.colorsEnabled.ContainsKey(id.value))
                {
                    manager.rainWorld.progression.miscProgressionData.colorsEnabled.Add(id.value, colorChecked);
                }
                else
                {
                    manager.rainWorld.progression.miscProgressionData.colorsEnabled[id.value] = colorChecked;
                }
                this.colorChecked = colorChecked;
                ((Action)(colorChecked ? AddColorButtons : RemoveColorButtons)).Invoke();
            }
            public void SaveHSLString(Vector3 hsl)
            {
                this.SaveHSLString_Menu_Vector3(id, colorChooser, hsl);
            }
            public void AddOrRemoveColorInterface(int num)
            {
                if (num == colorChooser)
                {
                    RemoveColorInterface();
                    PlaySound(SoundID.MENU_Remove_Level);
                    return;
                }
                colorChooser = num;
                AddColorInterface();
                PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            }
            public void AddColorButtons()
            {
                if (bodyInterface == null)
                {
                    bodyInterface = GetColorInterface(id, new(size.x, size.y + 90));
                    ColorPage.subObjects.Add(bodyInterface);
                }
            }
            public void AddColorInterface()
            {
                if (sliders == null)
                {
                    sliders = new(this, ColorPage, new(size.x, size.y - 10), offset, sliderSize, SliderIDGroups, new(0, 30));
                    ColorPage.subObjects.Add(sliders);
                }
                if (defaultColor == null)
                {
                    defaultColor = new(this, ColorPage, Translate("Restore Default"), "DEFAULTCOL", size + sliders.sliderOOO.pos + offset + new Vector2(0, sliders.oOOPages?.PagesOn == true ? -40 : 0), new(SizeXOfDefaultCol, 30));
                    ColorPage.subObjects.Add(defaultColor);
                }
                if (hexTypeBox == null && openHex)
                {
                    hexTypeBox = new(this, ColorPage, defaultColor.pos + new Vector2(defaultColor.size.x + 10, 0))
                    {
                        saveNewTypedColor = (hex, hsl, rgb) =>
                        {
                            SaveHSLString(hexTypeBox.newPendingHSL);
                            if (sliders != null)
                            {
                                sliders.sliderO.UpdateSliderValue();
                                sliders.sliderOO.UpdateSliderValue();
                                sliders.sliderOOO.UpdateSliderValue();
                            }
                        },
                    };
                    ColorPage.subObjects.Add(hexTypeBox);
                }
                bodyInterface?.bodyButtons?.ApplyMethodToList((button) =>
                {
                    button.MenuObjectBind(sliders?.sliderO, bottom: true);
                });
                sliders.sliderO.MutualMenuObjectBind(bodyInterface?.bodyButtons?.FirstOrDefault(), true, bottomTop: true);
                MutualVerticalButtonBind(colorCheckbox, defaultColor);
                okButton?.MutualMenuObjectBind(hexTypeBox != null ? hexTypeBox.elementWrapper : defaultColor, true, bottomTop: true);
                if (sliders.oOOPages?.PagesOn == true)
                {
                    MutualVerticalButtonBind(defaultColor, sliders.oOOPages.prevButton);
                    MutualVerticalButtonBind(defaultColor, sliders.oOOPages.nextButton);
                    return;
                }
                MutualVerticalButtonBind(defaultColor, sliders.sliderOOO);

            }
            public void RemoveColorButtons()
            {
                ColorPage.ClearMenuObject(ref bodyInterface);
                RemoveColorInterface();
            }
            public void RemoveColorInterface()
            {
                this.TryFixColorChoices(id);
                if (bodyInterface?.bodyButtons != null)
                {
                    bodyInterface.bodyButtons.ApplyMethodToList((button, i) =>
                    {
                        if (i == 0)
                        {
                            button.MutualMenuObjectBind(colorCheckbox, false, bottomTop: true);
                            return;
                        }
                        button.MenuObjectBind(colorCheckbox, bottom: true);
                    });
                }
                ColorPage.ClearMenuObject(ref sliders);
                ColorPage.ClearMenuObject(ref defaultColor);
                ColorPage.ClearMenuObject(ref hexTypeBox);
                colorChooser = -1;
            }
            public ExpeditionColorInterface GetColorInterface(SlugcatStats.Name slugcatID, Vector2 pos)
            {
                List<string> names = PlayerGraphics.ColoredBodyPartList(slugcatID);
                List<string> list = PlayerGraphics.DefaultBodyPartColorHex(slugcatID);
                for (int i = 0; i < list.Count; i++)
                {
                    Vector3 hsl = Custom.RGB2HSL(Custom.hexToColor(list[i]));
                    list[i] = SmallUtils.SetHSLSaveString(hsl);
                }
                return new ExpeditionColorInterface(this, ColorPage, pos, slugcatID, names, list, showDisplay);
            }
            public const string colorCheckboxSingal = "COLORCHECKED";
            public int colorChooser;
            public bool colorChecked, shouldClampHue, openHex, showDisplay;
            public Vector2 sliderSize = new(200, 30), offset = new (0, -40);
            public CheckBox colorCheckbox;
            public SlugcatStats.Name id;
            public OOOSliders sliders;
            public SimpleButton defaultColor;
            public HexTypeBox hexTypeBox;
            public ExpeditionColorInterface bodyInterface;
            public class ExpeditionColorInterface : PositionedMenuObject, IDoPerPage
            {
                public int PerPage
                { get => perPage; set => perPage = value < 1 ? 1 : value; }
                public int CurrentOffset
                { get => currentOffset; set => currentOffset = Mathf.Clamp(value, 0, bodyNames?.Count > 0 ? bodyNames.Count - 1 / PerPage : 0); }
                public bool PagesOn => bodyNames?.Count > PerPage;
                public ExpeditionColorInterface(Menu.Menu menu, MenuObject owner, Vector2 pos, SlugcatStats.Name slugcatID, List<string> names, List<string> defaultColors, bool showDisplay = false) : base(menu, owner, pos)
                {
                    PerPage = 2;
                    currentOffset = 0;
                    bodyNames = names;
                    this.slugcatID = slugcatID;
                    this.defaultColors = defaultColors;
                    SafeSaveColor();
                    if (showDisplay)
                    {
                        slugcatDisplay = new(menu, this, new(PerPage * 80 + 80, -60), new(45, 45), slugcatID);
                        subObjects.Add(slugcatDisplay);
                    }
                    PopulatePage(CurrentOffset);
                    if (PagesOn)
                    {
                        ActivateButtons();
                    }
                }
                public override void GrafUpdate(float timeStacker)
                {
                    base.GrafUpdate(timeStacker);
                    slugcatDisplay?.LoadNewHSLStringSlugcat(menu.manager.rainWorld.progression.miscProgressionData.colorChoices[slugcatID.value]);
                    bodyColors.ApplyMethodToDictionary((illu, index) =>
                    {
                        illu.color = ColConversions.HSL2RGB(menu.MenuHSL(slugcatID, index));
                    });
                }
                public override void RemoveSprites()
                {
                    base.RemoveSprites();
                    this.ClearMenuObject(ref slugcatDisplay);
                    DeactivateButtons();
                    ClearInterface();
                }
                public override void Singal(MenuObject sender, string message)
                {
                    base.Singal(sender, message);
                    if (message == PREVSINGAL)
                    {
                        PrevPage();
                    }
                    if (message == NEXTSINGAL)
                    {
                        NextPage();
                    }
                }
                public void PrevPage()
                {
                    menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                    PopulatePage(this.PrevPageLoopListPerPage(bodyNames));
                }
                public void NextPage()
                {
                    menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                    PopulatePage(this.NextPageLoopListPerPage(bodyNames));
                }
                public void PopulatePage(int offset)
                {
                    ClearInterface(true);
                    List<SimpleButton> buttons = [];
                    List<RoundedRect> borders = [];
                    CurrentOffset = offset;
                    this.ApplyPerPageList(bodyNames, (name, num) =>
                    {
                        SimpleButton bodyButton = new(menu, this, menu.Translate(name), OPENINTERFACESINGAL + num.ToString(CultureInfo.InvariantCulture), new Vector2(num % PerPage * 90, -50), new(80, 30));
                        RoundedRect bodyColorBorder = new(menu, this, new(bodyButton.pos.x + (bodyButton.size.x / 4), bodyButton.pos.y + 50), new(40, 40), false);
                        MenuIllustration bodyColor = new(menu, this, "", "square", bodyColorBorder.pos + new Vector2(2, 2), false, false);
                       subObjects.AddRange([bodyButton, bodyColorBorder, bodyColor]);
                        bodyButton.MutualMenuObjectBind(buttons.GetValueOrDefault((num - 1) % PerPage), false, true);
                        buttons.Add(bodyButton);
                        borders.Add(bodyColorBorder);
                        bodyColors.Add(bodyColor, num);
                    });
                    buttons.FirstOrDefault().MutualMenuObjectBind(prevNextButton?.prevButton, false, true);
                    buttons.LastOrDefault().MutualMenuObjectBind(prevNextButton?.nextButton, true, true);
                    bodyButtons = [.. buttons];
                    bodyColorBorders = [.. borders];
                    (menu as ExpeditionColorDialog)?.RemoveColorInterface();
                }
                public void SafeSaveColor()
                {
                    if (!menu.manager.rainWorld.progression.miscProgressionData.colorChoices.ContainsKey(slugcatID.value))
                    {
                        menu.manager.rainWorld.progression.miscProgressionData.colorChoices.Add(slugcatID.value, []);
                    }
                    menu.manager.rainWorld.progression.miscProgressionData.colorChoices[slugcatID.value] ??= [];
                    while (menu.manager.rainWorld.progression.miscProgressionData.colorChoices[slugcatID.value].Count < bodyNames?.Count)
                    {
                        menu.manager.rainWorld.progression.miscProgressionData.colorChoices[slugcatID.value].Add(defaultColors[menu.manager.rainWorld.progression.miscProgressionData.colorChoices[slugcatID.value].Count]);
                    }
                }
                public void ClearInterface(bool refresh = false)
                {
                    this.ClearMenuObjectList(ref bodyButtons, refresh);
                    this.ClearMenuObjectList(ref bodyColorBorders, refresh);
                    this.ClearMenuObjectDictionary(ref bodyColors, refresh);
                }
                public void ActivateButtons()
                {
                    if (prevNextButton == null)
                    {
                        prevNextButton = new(menu, this, new(-34, -50), PREVSINGAL, NEXTSINGAL, new(20 + perPage * 80, 0), true);
                        subObjects.Add(prevNextButton);
                    }
                    bodyButtons.FirstOrDefault().MutualMenuObjectBind(prevNextButton.prevButton, false, true);
                    bodyButtons.LastOrDefault().MutualMenuObjectBind(prevNextButton.nextButton, true, true);
                }
                public void DeactivateButtons()
                {
                    this.ClearMenuObject(ref prevNextButton);
                }

                public const string OPENINTERFACESINGAL = "DUSTYEXPEDITIONCUSTOMCOLOR", PREVSINGAL = "PrevPageColors_DUSTYEXPEDITIONCUSTOMCOLOR", NEXTSINGAL = "NextPageColors_DUSTYEXPEDITIONCUSTOMCOLOR";
                private int perPage, currentOffset;
                public SlugcatDisplay slugcatDisplay;
                public PrevNextSymbolButton prevNextButton;
                public SimpleButton[] bodyButtons;
                public RoundedRect[] bodyColorBorders;
                public SlugcatStats.Name slugcatID;
                public Dictionary<MenuIllustration, int> bodyColors;
                public List<string> defaultColors, bodyNames;
            }
        }
        public class JollyCoopOOOConfig : PositionedMenuObject
        {
            public static void AddJollySliderIDGroups(List<SliderIDGroup> IDGroups, ColorChangeDialog.ColorSlider colSlider, bool shouldRemoveHSL)//int bodyPart, bool shouldRemoveHSL)
            {
                if (!shouldRemoveHSL)
                {
                    IDGroups.Add(new([colSlider.Hue, colSlider.Sat, colSlider.Lit], MenuToolObj.HSLNames,
                        MenuToolObj.HueOOShowInt, MenuToolObj.hueOOMultipler, MenuToolObj.HueOOSigns));
                }
                //Slider.SliderID[] sliderIDs;
                if (ModOptions.EnableJollyRGBSliders)
                {
                    IDGroups.Add(new(MenuToolObj.RGBSliderIDS, MenuToolObj.RGBNames, MenuToolObj.RGBShowInt, MenuToolObj.rgbMultipler));
                }
                if (ModOptions.EnableHSVSliders.Value)
                {
                    IDGroups.Add(new(MenuToolObj.HSVSliderIDS, MenuToolObj.HSVNames, MenuToolObj.HueOOShowInt, MenuToolObj.hueOOMultipler, MenuToolObj.HueOOSigns));
                }

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
                    roundingType = ModOptions.SliderRounding.Value,
                    showValues = false,
                    DecimalCount = ModOptions.DeCount,
                };
                oOOPages.LoadMethodToAllSliders((slider, pages, i) =>
                {
                    slider.ChangeSliderID(pages.CurrentGroup.SafeID(i));
                });
                valueLabel = new(menu, this, "", new(120, 23), new(80, 30), false);
                if (addHexInterface)
                {
                    hexInterface = new(menu, this, new(120f, -100f))
                    {
                        saveNewTypedColor = (hex, hsl, rgb) =>
                        {
                            owner.hslColor = hsl.Vector32HSL();
                            owner.color = rgb;
                            owner.JollyHSLSliders().ApplyMethodToList(slider =>
                            {
                                slider.UpdateSliderValue();
                            });
                        },
                    };
                }
                this.SafeAddSubObjects(oOOPages, valueLabel, hexInterface);
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                this.ClearMenuObject(ref hexInterface);
                this.ClearMenuObject(ref valueLabel);
                this.ClearMenuObject(ref oOOPages);
                /*if (oOOIDGroups != null)
                {
                    foreach (SliderIDGroup idGroups in oOOIDGroups)
                    {
                        foreach (Slider.SliderID id in idGroups.sliderIDs)
                        {
                            id.Unregister();
                        }
                    }
                    oOOIDGroups = null;
                }*/
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                if (valueLabel != null)
                {
                    valueLabel.label.color = color;
                    if (ModOptions.ShowVisual && oOOPages != null)
                    {
                        valueLabel.text = string.Join(",", oOOPages.SliderVisualValues ?? []);
                    }
                    else if (valueLabel.text != "")
                    {
                        valueLabel.text = "";
                    }
                }
            }
            public override void Update()
            {
                base.Update();
                if (owner is ColorChangeDialog.ColorSlider colSlider)
                {
                    hexInterface?.SaveNewHSL(colSlider.hslColor.HSL2Vector3());
                }
            }

            public Color color = MenuColorEffect.rgbMediumGrey;
            public MenuLabel valueLabel;
            public SliderPages oOOPages;
            //public List<SliderIDGroup> oOOIDGroups; removed that since instead of parsing num, we get slider owner instead
            public HexTypeBox hexInterface;
        }
        public class SlugcatDisplay : RectangularMenuObject
        {
            //removed current and prev slugcat, assuming slugcat doesnt change midway while updating (meant for Story menu)
            public SlugcatDisplay(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, SlugcatStats.Name current) : base(menu, owner, pos, size)
            {
                bodyNames = PlayerGraphics.ColoredBodyPartList(current);
                sprites = [];
                LoadIcon(current, bodyNames);
            }
            public static Dictionary<string, string> LoadFileNames(SlugcatStats.Name name, List<string> bodyNames)
            {
                Dictionary<string, string> bodyPaths = [];
                foreach (string txtpath in SmallUtils.FindFilePaths("colorconfig", ".txt"))
                {
                    string resolvedPath = AssetManager.ResolveFilePath(txtpath);
                    if (File.Exists(resolvedPath))
                    {
                        foreach (string line in File.ReadAllLines(resolvedPath, System.Text.Encoding.UTF8))
                        {
                            if (line.StartsWith(name.value) && line.Split(':').GetValueOrDefault(1, "").Contains('|'))
                            {
                                foreach (string body in line.Split(':')[1].Split(','))
                                {
                                    string[] bodyLine = body.Split('|');
                                    if (bodyNames.Contains(bodyLine[0]))
                                    {
                                        ColorConfigMod.DebugLog("FileParser: " + bodyLine[1]);
                                        bodyPaths.AddOrReplace(bodyLine[0], bodyLine[1]);
                                    }

                                }
                            }
                        }
                    }
                }
                return bodyPaths;
            }
            public static void ParseFromFileDictionary(Dictionary<string, string> dic, string bodyName, int i, SlugcatStats.Name name, out string folder, out string file)
            {
                folder = "";
                if (dic?.ContainsKey(bodyName) == true)
                {
                    string path = dic[bodyName];
                    file = path;
                    if (path.Contains("/"))
                    {
                        file = path.Split('/').Last();
                        folder = path.Replace("/" + file, string.Empty);
                    }
                    return;
                }
                file = i switch
                {
                    0 => File.Exists(AssetManager.ResolveFilePath("illustrations/" + name.value + "_pup_off.png")) ? name.value + "_pup_off" : "pup_off",
                    1 => File.Exists(AssetManager.ResolveFilePath("illustrations/" + "face_" + name.value + "_pup_off.png")) ? $"face_{name.value}_pup_off" : "face_pup_off",
                    2 => File.Exists(AssetManager.ResolveFilePath($"illustrations/unique_{name.value}_pup_off.png")) ? $"unique_{name.value}_pup_off" : "colorconfig_showcasesquare",
                    _ => File.Exists(AssetManager.ResolveFilePath($"illustrations/{bodyName}_{name.value}_pup_off.png")) ? $"{bodyName}_{name.value}_pup_off" : "colorconfig_showcasesquare",
                };

            }
            public void LoadSlugcatSprites(SlugcatStats.Name name, List<string> bodyNames)
            {
                List<MenuIllustration> illus = [];
                Dictionary<string, string> preSetFilesToLoad = LoadFileNames(name, bodyNames);
                bodyNames.ApplyMethodToList((bodyName, i) =>
                {
                    ParseFromFileDictionary(preSetFilesToLoad, bodyName, i, name, out string folder, out string file);
                    ColorConfigMod.DebugLog($"Slugcat Display loader.. BodyPart: {bodyName},Folder: {(folder == "" ? "Illustrations" : folder)}, File: {file}");
                    MenuIllustration body = new(menu, this, folder, file, file == "colorconfig_showcasesquare" ? new(i * 10, -0.7f) : size / 2, true, true);
                    subObjects.Add(body);
                    illus.Add(body);
                });
                sprites = [.. illus];
            }
            public void LoadIcon(SlugcatStats.Name current, List<string> bodyNames)
            {
                this.ClearMenuObjectList(ref sprites, true);
                LoadSlugcatSprites(current, bodyNames);
            }
            public void LoadNewColorSlugcat(List<Color> slugcatCols/*, SlugcatStats.Name name*/)
            {
                while (slugcatCols.Count < bodyNames.Count)
                {
                    slugcatCols.Add(Color.white);
                }
                currentRGBs = slugcatCols;
            }
            public void LoadNewHSLSlugcat(List<Vector3> slugcatHSLColos/*, SlugcatStats.Name name*/)
            {
                List<Color> rgbs = [.. slugcatHSLColos?.Select(ColConversions.HSL2RGB)];
                while (rgbs.Count < bodyNames.Count)
                {
                    rgbs.Add(Color.white);
                }
                currentRGBs = rgbs;
            }
            public void LoadNewHSLStringSlugcat(List<string> slugcatHSLColos/*, SlugcatStats.Name name*/)
            {
                List<Color> rgbs = [.. slugcatHSLColos?.Select(x => ColConversions.HSL2RGB(SmallUtils.ParseHSLString(x)))];
                while (rgbs.Count < bodyNames.Count)
                {
                    rgbs.Add(Color.white);
                }
                currentRGBs = rgbs;
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                sprites.ApplyMethodToList((sprite, i) =>
                {
                    sprite.color = currentRGBs.GetValueOrDefault(i, Color.white);
                });
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                this.ClearMenuObjectList(ref sprites);
            }

            //no need to find for currentslugcat and prev if unchanged in slugcat select menu
            //public SlugcatStats.Name currentSlugcat, prevSlugcat;
            public List<Color> currentRGBs;
            public List<string> bodyNames;
            public MenuIllustration[] sprites;
        }
        public class HexTypeBoxPages : PositionedMenuObject, IDoPerPage
        {
            public int PerPage { get => perPage; set => perPage = value < 1 ? 1 : value; }
            public int CurrentOffset { get => currentOffset; set => currentOffset = Mathf.Clamp(value, 0, names?.Count > 0 ? names.Count - 1 / PerPage : 0); }
            public bool PagesOn => names?.Count > PerPage;
            public HexTypeBoxPages(Menu.Menu menu, MenuObject owner, Vector2 pos, List<string> names, int perPage = 2) : base(menu, owner, pos)
            {
                PerPage = perPage;
                CurrentOffset = 0;
                this.names = names;
                if (PagesOn)
                {
                    ActivateButtons();
                }
                PopulatePage(CurrentOffset);
            }
            public override void Singal(MenuObject sender, string message)
            {
                base.Singal(sender, message);
                if (message == PREVSINGAL)
                {
                    PrevPage();
                }
                if (message == NEXTSINGAL)
                {
                    NextPage();
                }
            }
            public void SaveNewHSLs(IList<Vector3> hsls)
            {
                hexBoxes.ApplyMethodToDictionary((hexBox, num) =>
                {
                    hexBox.SaveNewHSL(hsls?.Count > num ? hsls[num] : Vector3.one);
                });
            }
            public void NextPage()
            {

                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                PopulatePage(this.NextPageLoopListPerPage(names));
            }
            public void PrevPage()
            {
                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                PopulatePage(this.PrevPageLoopListPerPage(names));
            }
            public void PopulatePage(int offset)
            {
                this.ClearMenuObjectDictionary(ref hexBoxes, true);
                CurrentOffset = offset;
                MenuObject toBindWith = null;
                this.ApplyPerPageList(names, (name, num) =>
                {
                    HexTypeBox hexTypeBox = GetHexTypeBox(num);
                    hexBoxes.Add(hexTypeBox, num);
                    subObjects.Add(hexTypeBox);
                    hexTypeBox.elementWrapper.MutualMenuObjectBind(toBindWith, false, true);
                    toBindWith = hexTypeBox.elementWrapper;

                });
                hexBoxes?.Keys?.FirstOrDefault()?.elementWrapper.MutualMenuObjectBind(prevNextButton?.prevButton, false, true);
                hexBoxes?.Keys?.LastOrDefault()?.elementWrapper.MutualMenuObjectBind(prevNextButton?.nextButton, true, true);
            }
            public HexTypeBox GetHexTypeBox(int num)
            {
                HexTypeBox hexTypeBox = new(menu, this, new(70 * (num % PerPage) + (PagesOn ? 34 : 0), 0))
                {
                    saveNewTypedColor = (hexTyper, hsl, rgb) =>
                    {
                        if (hexBoxes?.ContainsKey(hexTyper) == true)
                        {
                            applyChanges(hexTyper, hsl, rgb, hexBoxes[hexTyper]);
                        }
                    },
                };
                return hexTypeBox;
            }
            public void ActivateButtons()
            {
                if (prevNextButton == null)
                {
                    prevNextButton = new(menu, this, Vector2.zero, PREVSINGAL, NEXTSINGAL, new(perPage * 70, 0), true);
                    subObjects.Add(prevNextButton);
                }
                hexBoxes?.Keys?.FirstOrDefault()?.elementWrapper.MutualMenuObjectBind(prevNextButton.prevButton, false, true);
                hexBoxes?.Keys?.LastOrDefault()?.elementWrapper.MutualMenuObjectBind(prevNextButton.nextButton, true, true);
            }
            public void DeactivateButtons()
            {
                this.ClearMenuObject(ref prevNextButton);
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                DeactivateButtons();
                this.ClearMenuObjectDictionary(ref hexBoxes);
            }

            public const string PREVSINGAL = "PREVPAGE_HEXBOXES_DUSTY", NEXTSINGAL = "NEXTPAGE_HEXBOXES_DUSTY";
            private int perPage, currentOffset;
            public List<string> names;
            public Dictionary<HexTypeBox, int> hexBoxes;
            public PrevNextSymbolButton prevNextButton;
            public Action<HexTypeBox, Vector3, Color, int> applyChanges;
        }
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
                sliderOOO = new(menu, this, "", Vector2.zero + (offset * 2), size, null, subtle);
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
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                this.ClearMenuObject(ref oOOPages);
                this.ClearMenuObject(ref sliderO);
                this.ClearMenuObject(ref sliderOO);
                this.ClearMenuObject(ref sliderOOO);
            }

            public SliderPages oOOPages;
            public HorizontalSlider sliderO, sliderOO, sliderOOO;
        }
        public class HexTypeBox : PositionedMenuObject, ICopyPasteConfig
        {
            public virtual string Clipboard
            { get => Manager.Clipboard?.Trim(); set => Manager.Clipboard = value; }
            public virtual bool ShouldCopyPaste => hexTyper?.MouseOver == true;
            public HexTypeBox(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
            {
                saveNewTypedColor = null;
                clampHex = ClampHSL;
                tabWrapper = new(menu, this);
                lastValue = "";
                hexTyper = new(new Configurable<string>(""), Vector2.zero, 60)
                {
                    maxLength = 6
                };
                elementWrapper = new(tabWrapper, hexTyper);
                subObjects.AddRange([tabWrapper,elementWrapper]);
            }
            public virtual void SaveNewHSL(Vector3 hsl)
            {
                currentHSL = hsl;
            }
            public virtual void SaveNewRGB(Color rgb)
            {
                currentHSL = Custom.RGB2HSL(rgb);
            }
            public virtual string Copy()
            {
                menu.PlaySound(SoundID.MENU_Player_Join_Game);
                return hexTyper.value;
            }
            public virtual void Paste(string clipboard)
            {
                if (!SmallUtils.IfHexCodeValid(clipboard, out Color fromPaste) || hexTyper.value.IsHexCodesSame(clipboard))
                {
                    menu.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
                    return;
                }
                clampHex?.Invoke(this, fromPaste);
                shouldSaveNewTypedColor = true;
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
                                ColorConfigMod.DebugError($"Failed to parse from new value \"{hexTyper.value}\"");
                                hexTyper.value = lastValue;
                                return;
                            }
                            clampHex?.Invoke(this, hexCol);
                            shouldSaveNewTypedColor = true;
                            lastValue = hexTyper.value;
                        }
                    }
                }
                TrySave();
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                this.ClearMenuObject(ref elementWrapper);
                this.ClearMenuObject(ref tabWrapper);
                if (hexTyper != null)
                {
                    hexTyper.label.alpha = 0;
                    hexTyper.label.RemoveFromContainer();
                    hexTyper.rect.container.RemoveFromContainer();
                    hexTyper = null;
                }
            }
            public virtual void ClampHSL(HexTypeBox hexTypeBox, Color rgb)
            {
                hexTyper.value = ColorUtility.ToHtmlStringRGB(SmallUtils.Vector32RGB(SmallUtils.RWIIIClamp(SmallUtils.RGB2Vector3(rgb), CustomColorModel.RGB, out Vector3 newClampedHSL)));
                newClampedHSL = SmallUtils.FixNonHueSliderWonkiness(newClampedHSL, currentHSL);
                if (newClampedHSL != currentHSL)
                {
                    newPendingHSL = newClampedHSL;
                    newPendingRGB = ColConversions.HSL2RGB(newPendingHSL);
                    currentHSL = newPendingHSL;
                    prevHSL = currentHSL;
                }
            }
            public virtual void TrySave()
            {
                if (shouldSaveNewTypedColor && saveNewTypedColor != null)
                {
                    shouldSaveNewTypedColor = false;
                    saveNewTypedColor.Invoke(this, newPendingHSL, newPendingRGB);
                }
            }
            public string lastValue;
            public bool shouldSaveNewTypedColor = false;
            public Color newPendingRGB;
            public Vector3 currentHSL, prevHSL, newPendingHSL;
            public MenuTabWrapper tabWrapper;
            public UIelementWrapper elementWrapper;
            public OpTextBox hexTyper;
            public Action<HexTypeBox, Color> clampHex;
            public Action<HexTypeBox, Vector3, Color> saveNewTypedColor;

        }
        public class SliderPages : PositionedMenuObject, ICanTurnPages, IGetOwnInput, ICopyPasteConfig
        {
            public static void UpdateSliderMenuLabelText(Slider slider, string name, string visualValue, bool showVisuals)
            {
                if (slider?.menuLabel != null && slider.menu != null && ((!showVisuals && slider.menuLabel.text != slider.menu.Translate(name)) || showVisuals))
                {
                    slider.menuLabel.text = slider.menu.Translate(name) + (showVisuals ? $" {visualValue}" : "");
                }
            }
            public static float ChangeValueBasedOnMultipler(float newValue, float multipler, bool recieve = false)
            {
                float result = recieve ? newValue / multipler : newValue * multipler;
                return recieve ? Mathf.Clamp01(result) : result;
            }
            public static string GetVisualSliderValue(float visualValue, int decimalPlaces, string sign, MidpointRounding roundType = MidpointRounding.AwayFromZero)
            {
                //basically last digit is less than 5, go to 0, more than five go to 10 for roundingAwayFromZero
                double amt = Math.Round(visualValue, Mathf.Clamp(decimalPlaces, 0, 15), roundType);
                return amt.ToString() + sign;
            }
            public bool ShouldCopyPaste => ModOptions.CopyPasteForSliders.Value && CopyPasteSlider != null;
            public  string Clipboard
            {
                get => Manager.Clipboard;
                set => Manager.Clipboard = value;
            }
            public string[] SliderVisualValues
            {
                get
                {
                    List<string> list = [];
                    sliderOOOs.ApplyMethodToList((slider, i) =>
                    {
                        list.Add(GetVisualSliderValue((slider == null ? 0 : slider.floatValue) * CurrentGroup.SafeMultipler(i), CurrentGroup.SafeShowInt(i) ? 0 : decimalCount, showSign ? CurrentGroup.SafeSigns(i) : "", roundingType));
                    });
                    return [.. list];
                }
            }
            public bool PagesOn => OOOIDGroups?.Count > 1;
            public Slider CopyPasteSlider => sliderOOOs?.FirstOrDefault(x => x?.MouseOver == true); //shouldcopypaste should tell whetehr to copy/paste to this slider
            public int CurrentOffset
            { get => currentOffset; set => currentOffset = Mathf.Clamp(value, 0, OOOIDGroups?.Count > 0 ? OOOIDGroups.Count - 1 : 0); }
            public  int DecimalCount
            { get => decimalCount; set => decimalCount = Mathf.Clamp(value, 0, 15); }
            public  List<SliderIDGroup> OOOIDGroups { get; protected set; }
            public SliderIDGroup CurrentGroup => OOOIDGroups.GetValueOrDefault(CurrentOffset);
            public SliderPages(Menu.Menu menu, MenuObject owner, Slider[] sliders, List<SliderIDGroup> sliderOOOIDGroups, Vector2 buttonOffset = default, Vector2 pos = default) : base(menu, owner, pos == default ? Vector2.zero : pos)
            {
                if (sliders == null || sliders.Length == 0)
                {
                    throw new ArgumentNullException(nameof(sliders));
                }
                showValues = true;
                showSign = true;
                copyPaste = ModOptions.CopyPasteForSliders.Value;
                decimalCount = 2;
                roundingType = MidpointRounding.AwayFromZero;
                CurrentOffset = 0;
                sliderOOOs = sliders;
                OOOIDGroups = sliderOOOIDGroups?.Count == 0 ? [new([.. sliderOOOs?.Select(x => x?.ID)], [.. sliderOOOs.Select(x => x?.menuLabel).Select(x => x == null ? "" : x.text)])] : sliderOOOIDGroups;
                buttonOffset = buttonOffset == default ? new(0, 0) : buttonOffset;
                setPrevButtonPos = new(buttonOffset.x + sliderOOOs.Last().pos.x, -(sliderOOOs.Last().size.y * 2) + buttonOffset.y + sliderOOOs.Last().pos.y);
                if (PagesOn)
                {
                    ActivateButtons();
                }
            }
            public override void Singal(MenuObject sender, string message)
            {
                base.Singal(sender, message);
                if (message == PREVSINGAL)
                {
                    PrevPage();
                }
                if (message == NEXTSINGAL)
                {
                    NextPage();
                }
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                LoadMethodToAllSliders((slider, self, i) =>
                {
                    UpdateSliderMenuLabelText(slider, CurrentGroup.SafeNames(i), GetVisualSliderValue(i), showValues);
                });
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                DeactivateButtons();
                this.ClearMenuObjectList(ref sliderOOOs);
                OOOIDGroups?.Clear();
                OOOIDGroups = null;

            }
            public void LoadMethodToAllSliders(Action<Slider, SliderPages, int> action)
            {
                sliderOOOs.ApplyMethodToList((slider, i) =>
                {
                    action?.Invoke(slider, this, i);
                });
            }
            public void LoadMethodToAllTSliders<T>(Action<T, SliderPages, int> action) where T : Slider
            {
                sliderOOOs.ApplyMethodToList((slider, i) =>
                {
                    if (slider is T tSlider)
                    {
                        action?.Invoke(tSlider, this, i);
                    }
                });
            }
            public virtual void PopulatePage(int offset)
            {
                CurrentOffset = offset;
                LoadMethodToAllSliders((slider, self, i) => { slider?.ChangeSliderID(CurrentGroup.SafeID(i)); });

            }
            public virtual void NextPage()
            {
                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                PopulatePage(this.NextPageLoopList(OOOIDGroups));
            }
            public virtual void PrevPage()
            {
                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                PopulatePage(this.PrevPageLoopList(OOOIDGroups));
            }
            public virtual void ActivateButtons()
            {
                if (prevButton == null)
                {
                    prevButton = new(menu, this, menu.Translate("Prev"), PREVSINGAL, setPrevButtonPos, new(40, 26), FLabelAlignment.Center, false);
                    subObjects.Add(prevButton);

                }
                if (nextButton == null)
                {
                    nextButton = new(menu, this, menu.Translate("Next"), NEXTSINGAL, new(setPrevButtonPos.x + 60, setPrevButtonPos.y), new(40, 26), FLabelAlignment.Center, false);
                    subObjects.Add(nextButton);
                }
                menu.MutualHorizontalButtonBind(prevButton, nextButton);
            }
            public virtual void DeactivateButtons()
            {
                this.ClearMenuObject(ref prevButton);
                this.ClearMenuObject(ref nextButton);
            }
            public virtual void TryGetInput(Player.InputPackage input, Player.InputPackage lastInput)
            {
                if (!menu.manager.menuesMouseMode && PagesOn && (prevButton?.Selected == true || nextButton?.Selected == true || sliderOOOs.Any(x => x.Selected)))
                {
                    //changes to map and grab since normal ui input doesnt count it in menu in normal circumstances
                    if (!input.mp && lastInput.mp)
                    {
                        PrevPage();
                    }
                    if (!input.pckp && lastInput.pckp)
                    {
                        PrevPage();
                    }
                }

            }
            public virtual string GetVisualSliderValue(int o)
            {
                Slider slider = sliderOOOs.GetValueOrDefault(o, default);
                return slider != null ? GetVisualSliderValue(slider.floatValue * CurrentGroup.SafeMultipler(o), CurrentGroup.SafeShowInt(o) ? 0 : decimalCount, showSign ? CurrentGroup.SafeSigns(o) : "", roundingType) : "";
            }
            public virtual string Copy()
            {
                Slider slider = CopyPasteSlider;
                if (slider == null)
                {
                    return 0.ToString();
                }
                float copyValue = slider.floatValue;
                copyValue = ChangeValueBasedOnMultipler(copyValue, CurrentGroup.SafeMultipler(sliderOOOs.FindIndex(slider)));
                menu.PlaySound(SoundID.MENU_Player_Join_Game);
                ColorConfigMod.DebugLog($"Slider Copier: Copied.. {copyValue}");
                return copyValue.ToString();

            }
            public virtual void Paste(string clipboard)
            {
                Slider slider = CopyPasteSlider;
                if (slider == null)
                {
                    return;
                }
                if (float.TryParse(clipboard, NumberStyles.Integer | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out float newValue))
                {
                    ColorConfigMod.DebugLog($"Slider Paster: Got.. {newValue}");
                    newValue = ChangeValueBasedOnMultipler(newValue, CurrentGroup.SafeMultipler(sliderOOOs.FindIndex(slider)), true);
                    ColorConfigMod.DebugLog($"Slider Paster: Slider value parse.. {newValue}");
                    if (slider.floatValue != newValue)
                    {
                        slider.floatValue = newValue;
                        menu.PlaySound(SoundID.MENU_Switch_Page_In);
                        return;
                    }
                }
                menu.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
            }

            public const string PREVSINGAL = "_BackPageSliders", NEXTSINGAL = "_NextPageSliders";
            private int currentOffset, decimalCount;
            public bool showValues, showSign, copyPaste;
            public Slider[] sliderOOOs;
            public Vector2 setPrevButtonPos;
            public BigSimpleButton prevButton, nextButton;
            public MidpointRounding roundingType;
        }
        public class PrevNextSymbolButton : PositionedMenuObject
        {
            public PrevNextSymbolButton(Menu.Menu menu, MenuObject owner, Vector2 pos, string prevSingal, string nextSingal, Vector2 nextButtonPosOffset, bool horizontal) : this(menu, owner, pos, "Menu_Symbol_Arrow", "Menu_Symbol_Arrow", prevSingal, nextSingal, nextButtonPosOffset, horizontal)
            {
                nextButton.symbolSprite.rotation = horizontal ? 90 : 0;
                prevButton.symbolSprite.rotation = horizontal? 270 : 180;
            }
            public PrevNextSymbolButton(Menu.Menu menu, MenuObject owner, Vector2 pos, string prevSymbol, string nextSymbol, string prevSingal, string nextSingal, Vector2 nextButtonPosOffset, bool horizontal) : base(menu, owner, pos)
            {
                prevButton = new(menu, this, prevSymbol, prevSingal, Vector2.zero);
                nextButton = new(menu, this, nextSymbol, nextSingal, new Vector2(horizontal ? 34 : 0, horizontal ? 0 : 34) + nextButtonPosOffset);
                subObjects.AddRange([prevButton, nextButton]);
                ((Action<MenuObject, MenuObject>)(horizontal ? menu.MutualHorizontalButtonBind : menu.MutualVerticalButtonBind)).Invoke(prevButton, nextButton);
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                this.ClearMenuObject(ref prevButton);
                this.ClearMenuObject(ref nextButton);
            }
            public SymbolButton prevButton, nextButton;
        }
        public interface IGetOwnInput
        {
            void TryGetInput(Player.InputPackage input, Player.InputPackage lastInput);
        }
        public interface IDoPerPage : ICanTurnPages
        {
            int PerPage { get; }
        }
        public interface ICanTurnPages
        {
            int CurrentOffset { get; }
            bool PagesOn { get; }
            void PopulatePage(int offset);
            void NextPage();
            void PrevPage();
        }
        public interface ICopyPasteConfig
        {
            string Clipboard { get; set; }
            bool ShouldCopyPaste { get; }
            string Copy();
            void Paste(string clipboard);

        }
        public struct SliderIDGroup(Slider.SliderID[] sliderIDs, string[] names = null, bool[] showInts = null, float[] multipler = null, string[] signs = null)
        {
            public readonly float SafeMultipler(int index) => multipler.GetValueOrDefault(index, 1);
            public readonly bool SafeShowInt(int index) => showInts.GetValueOrDefault(index, false);
            public readonly string SafeNames(int index) => names.GetValueOrDefault(index, "");
            public readonly string SafeSigns(int index) => signs.GetValueOrDefault(index, "");
            public readonly Slider.SliderID SafeID(int index) => sliderIDs.GetValueOrDefault(index, new("DUSTY_REFISNULL", false));

            public float[] multipler = multipler;
            public bool[] showInts = showInts;
            public string[] names = names, signs = signs;
            public Slider.SliderID[] sliderIDs = sliderIDs;
        }
        public struct ExtraFixedMenuInput(bool cpy = false, bool paste = false)
        {
            public bool cpy = cpy, pste = paste;
        }
    }
    public static class ExtraInterfaces
    {
        public class ExtraSSMInterfaces
        {
            public HexTypeBox hexInterface;
            public SlugcatDisplay slugcatDisplay;
            public SliderPages sliderPages;
            //Legacy versions stuff
            public OOOSliders legacySliders;
            public HexTypeBoxPages legacyHexInterface;
        }
        public class ExtraExpeditionInterfaces
        {
            public SymbolButton colorConfig;
        }
        public class ColorPickerExtras(OpColorPicker cPicker)
        {
            public bool IsHSVMode 
            {
                get
                {
                    return _IsHSVMode;
                }
                set
                {
                    if (_IsHSVMode != value)
                    {
                        _IsHSVMode = value;
                        cPicker.ChangeHSLHSVMode(_IsHSVMode);
                        cPicker.RefreshTexture();
                        cPicker.RefreshText();
                    }
                }
            }
            public bool IsDifferentHSVHSLMode
            {
                get
                {
                    return _IsDifferentHSLHSVMode;
                }
                set
                {
                    if (_IsDifferentHSLHSVMode != value)
                    {
                        _IsDifferentHSLHSVMode = value;
                        cPicker.RefreshTexture();
                    }

                }
            }
            public OpColorPicker cPicker = cPicker;
            public bool _IsHSVMode;
            public bool _IsDifferentHSLHSVMode;
        }
    }
}
