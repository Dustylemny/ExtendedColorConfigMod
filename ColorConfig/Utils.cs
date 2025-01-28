using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using Menu;
using RWCustom;
using UnityEngine;
using static ColorConfig.ColConversions;
using Menu.Remix.MixedUI;
using MoreSlugcats;
using JollyCoop.JollyMenu;
using Menu.Remix;

namespace ColorConfig
{
    public static class Manager
    {
        //Clipboard
        public static string Clipboard
        {
            get
            {
                return GUIUnityClipboard;
            }
            set
            {
                GUIUnityClipboard = value;
            }
        }
        public static string GUIUnityClipboard
        {
            get => GUIUtility.systemCopyBuffer;
            set => GUIUtility.systemCopyBuffer = value;
        }
    }
    public static class MenuToolObj
    {
        //Enums
        public static Slider.SliderID RedRGB = new("RedRGB", true);
        public static Slider.SliderID GreenRGB = new("GreenRGB", true);
        public static Slider.SliderID BlueRGB = new("BlueRGB", true);

        public static Slider.SliderID HueHSV = new("HueHSV", true);
        public static Slider.SliderID SatHSV = new("SatHSV", true);
        public static Slider.SliderID ValHSV = new("ValHSV", true);

        //slider IDS
        public static Slider.SliderID[] RGBSliderIDS  => [RedRGB, GreenRGB, BlueRGB];
        public static Slider.SliderID[] HSVSliderIDS => [HueHSV, SatHSV, ValHSV];

        //names
        public static string[] HSLNames  => [hue, sat, lit];
        public static string[] RGBNames => [red, green, blue]; 
        public static string[] HSVNames => [hue, sat, value]; 

        //signs
        public static string[] HueOOSigns => [" °", "%", "%"];

        //showInts
        public static bool[] RGBShowInt => [true, true, true];
        public static bool[] HueOOShowInt => [true, false, false];

        //OOO Multiplers;
        public static readonly Vector3 rgbMultipler = new(255, 255, 255);
        public static readonly Vector3 hueOOMultipler = new(360, 100, 100);

        //const stuff
        public const string red = "RED";
        public const string green = "GREEN";
        public const string blue = "BLUE";
        public const string value = "VALUE";

        public const string hue = "HUE";
        public const string sat = "SAT";
        public const string lit = "LIT";

        //public const string degreeSign = " °";

        //RWStuff Clamp
        public static readonly Vector3 hslClampMax = new(0.99f, 1, 1);
        public static readonly Vector3 hslClampMin = new(0, 0, 0.01f);
    }
    public static class SmallUtils
    {


        //for faster getting collection values and defaulting if failed to find
        public static T GetValueOrDefault<T>(this IList<T> collection, int index, T defaultValue)
        {
            return (collection != null && collection.Count > index) ? collection[index] : defaultValue;
        }

        //bascially for back track support
        public static string[] FindFilePaths(string directoryName, string fileFormat = "", bool directories = false, bool includeAll = false)
        {
            return AssetManager.ListDirectory(directoryName, directories, includeAll).Where(x => fileFormat == null || fileFormat == "" || x.EndsWith(fileFormat)).ToArray();
        }

        //menu stuff
        public static void ClearMenuObject(this MenuObject desiredMenuObj, MenuObject container)
        {
            desiredMenuObj?.RemoveSprites();
            container.RemoveSubObject(desiredMenuObj);

        }
        public static Vector3 MenuHSL(this Menu.Menu selM, SlugcatStats.Name id, int bodyPart)
        {
            Vector3 color = new(1, 1, 1);
            if (selM.manager.rainWorld.progression.miscProgressionData.colorChoices.ContainsKey(id?.value) &&
                selM.manager.rainWorld.progression.miscProgressionData.colorChoices[id.value].Count > bodyPart &&
                selM.manager.rainWorld.progression.miscProgressionData.colorChoices[id.value][bodyPart].Contains(","))
            {
                string[] hslArray = selM.manager.rainWorld.progression.miscProgressionData.colorChoices
                    [id.value]
                [bodyPart].Split(',');

                color = new Vector3(float.Parse(hslArray[0], NumberStyles.Any, CultureInfo.InvariantCulture),
                    float.Parse(hslArray[1], NumberStyles.Any, CultureInfo.InvariantCulture),
                    float.Parse(hslArray[2], NumberStyles.Any, CultureInfo.InvariantCulture));
            }
            return color;
        }
        public static void SaveHSLString(this Menu.Menu menu, SlugcatStats.Name id, int bodyIndex, string newHSL)
        {
            if (id == null)
            {
                ColorConfigMod.DebugError("Failed to save color choices due to slugcatID being null!");
                return;
            }
            if (!menu.manager.rainWorld.progression.miscProgressionData.colorChoices.ContainsKey(id.value))
            {
                ColorConfigMod.DebugError("Failed to save color choices due to color choices not containing slugcatID!");
                return;
            }
            if (menu.manager.rainWorld.progression.miscProgressionData.colorChoices[id.value].Count > bodyIndex)
            {
                menu.manager.rainWorld.progression.miscProgressionData.colorChoices[id.value][bodyIndex] = newHSL;
                return;
            }
            ColorConfigMod.DebugError("Failed to save color choices due to index being more than body count!");
        }

        //ssmstuff
        public static void AddSSMSliderIDGroups(List<MenuInterfaces.SliderOOOIDGroup> IDGroups, bool shouldRemoveHSL, bool shouldRemoveCustomSliders = false)
        {
            if (!shouldRemoveHSL)
            {
                IDGroups.Add(new(MMFEnums.SliderID.Hue, MMFEnums.SliderID.Saturation, MMFEnums.SliderID.Lightness, MenuToolObj.HSLNames,
                    MenuToolObj.HueOOShowInt, MenuToolObj.hueOOMultipler, MenuToolObj.HueOOSigns));
            }
            if (shouldRemoveCustomSliders)
            {
                return;
            }
            if (ModOptions.EnableRGBSliders.Value)
            {
                IDGroups.Add(new(MenuToolObj.RedRGB, MenuToolObj.GreenRGB, MenuToolObj.BlueRGB,
                    MenuToolObj.RGBNames, MenuToolObj.RGBShowInt, MenuToolObj.rgbMultipler));
            }
            if (ModOptions.EnableHSVSliders.Value)
            {
                IDGroups.Add(new(MenuToolObj.HueHSV, MenuToolObj.SatHSV, MenuToolObj.ValHSV,
                   MenuToolObj.HSVNames, MenuToolObj.HueOOShowInt, MenuToolObj.hueOOMultipler, MenuToolObj.HueOOSigns));
            }

        }
        public static void ChangeColorOrderIndex(this SlugcatSelectMenu ssM, SlugcatStats.Name name)
        {
            for (int i = 0; i < ssM.slugcatColorOrder.Count; i++)
            {
                if (ssM.slugcatColorOrder[i] == name)
                {
                    ssM.slugcatPageIndex = i;
                    if (ssM.colorChecked)
                    {
                        ssM.RemoveColorButtons();
                        ssM.AddColorButtons();
                    }
                    break;
                }
            }
        }
        public static List<Vector3> SlugcatSelectMenuHSLs(this SlugcatSelectMenu selM)
        {
            List<Vector3> result = [];
            for (int i = 0; i < selM.manager.rainWorld.progression.miscProgressionData.colorChoices[selM.slugcatColorOrder[selM.slugcatPageIndex].value].Count; i++)
            {
                Vector3 color = new(1, 1, 1);
                if (selM.manager.rainWorld.progression.miscProgressionData.colorChoices[selM.slugcatColorOrder[selM.slugcatPageIndex].value][i].Contains(","))
                {
                    string[] hslArray = selM.manager.rainWorld.progression.miscProgressionData.colorChoices[selM.slugcatColorOrder[selM.slugcatPageIndex].value][i].Split(',');
                    color = new(float.Parse(hslArray[0], NumberStyles.Any, CultureInfo.InvariantCulture),
                        float.Parse(hslArray[1], NumberStyles.Any, CultureInfo.InvariantCulture),
                        float.Parse(hslArray[2], NumberStyles.Any, CultureInfo.InvariantCulture));

                }
                result.Add(color);
            }
            return result;
        }
        public static Vector3 SlugcatSelectMenuHSL(this SlugcatSelectMenu selM)
        {
            Vector3 color = new(1, 1, 1);
            if (selM.activeColorChooser > -1 &&
                selM.manager.rainWorld.progression.miscProgressionData.colorChoices[selM.slugcatColorOrder[selM.slugcatPageIndex].value]
                [selM.activeColorChooser].Contains(","))
            {
                string[] hslArray = selM.manager.rainWorld.progression.miscProgressionData.colorChoices
                    [selM.slugcatColorOrder[selM.slugcatPageIndex].value]
                [selM.activeColorChooser].Split(',');

                color = new Vector3(float.Parse(hslArray[0], NumberStyles.Any, CultureInfo.InvariantCulture),
                    float.Parse(hslArray[1], NumberStyles.Any, CultureInfo.InvariantCulture),
                    float.Parse(hslArray[2], NumberStyles.Any, CultureInfo.InvariantCulture));
            }

            return color;
        }
        public static void SaveHSLString(this SlugcatSelectMenu ssM, int bodyIndex, string newHSL)
        {
            if (ssM.manager.rainWorld.progression.miscProgressionData.colorChoices[ssM.slugcatColorOrder[ssM.slugcatPageIndex].value].Count > bodyIndex)
            {
                ssM.manager.rainWorld.progression.miscProgressionData.colorChoices[ssM.slugcatColorOrder[ssM.slugcatPageIndex].value]
                [bodyIndex] = newHSL;
                return;
            }
            ColorConfigMod.DebugLog("Failed to save color choices due to index being more than body count!");
        }
        public static void SaveHSLString(this SlugcatSelectMenu ssM, string newHSL)
        {
            ssM.manager.rainWorld.progression.miscProgressionData.colorChoices[ssM.slugcatColorOrder[ssM.slugcatPageIndex].value]
            [ssM.activeColorChooser] = newHSL;
        }

        //jolly-coop stuff
        public static void AddJollySliderIDGroups(List<MenuInterfaces.SliderOOOIDGroup> IDGroups, ColorChangeDialog.ColorSlider colSlider, int bodyPart, bool shouldRemoveHSL)
        {
            if (!shouldRemoveHSL)
            {
                IDGroups.Add(new(colSlider.Hue, colSlider.Sat, colSlider.Lit, MenuToolObj.HSLNames,
                    MenuToolObj.HueOOShowInt, MenuToolObj.hueOOMultipler, MenuToolObj.HueOOSigns));
            }
            Slider.SliderID[] sliderIDs;
            if (ModOptions.EnableRGBSliders.Value)
            {
                sliderIDs = RegisterOOOSliderGroups("RGB", MenuToolObj.RGBNames, bodyPart);
                IDGroups.Add(new(sliderIDs[0], sliderIDs[1], sliderIDs[2], MenuToolObj.RGBNames, MenuToolObj.RGBShowInt, MenuToolObj.rgbMultipler));
            }
            if (ModOptions.EnableHSVSliders.Value)
            {
                sliderIDs = RegisterOOOSliderGroups("HSV", MenuToolObj.HSVNames, bodyPart);
                IDGroups.Add(new(sliderIDs[0], sliderIDs[1], sliderIDs[2], MenuToolObj.HSVNames, MenuToolObj.HueOOShowInt, MenuToolObj.hueOOMultipler, MenuToolObj.HueOOSigns));
            }

        }
        public static Slider.SliderID[] RegisterOOOSliderGroups(string colorSpaceName, string[] oOONames, int bodyPart)
        {
            Slider.SliderID[] sliderIDs =
            [
                new($"DUSTY_{bodyPart}_{colorSpaceName}_{oOONames.GetValueOrDefault(0, "")}", true),
                    new($"DUSTY_{bodyPart}_{colorSpaceName}_{oOONames.GetValueOrDefault(1, "")}", true),
                    new($"DUSTY_{bodyPart}_{colorSpaceName}_{oOONames.GetValueOrDefault(2, "")}", true),
                ];
            return sliderIDs;
        }
        public static float ValueOfBodyPart(HSLColor col, int bodyPart)
        {
            return bodyPart switch
            {
                1 => col.saturation,
                2 => col.lightness,
                _ => col.hue
            };

        }

        //opcolor picker stuff
        public static void TryUpdateNonGreyCPicker(this OpColorPicker cPicker)
        {
            if (cPicker._MouseOverHex())
            {
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.C))
                {
                    cPicker.CopyHexCPicker();
                }
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.V))
                {
                    cPicker.PasteHexCPicker();
                }
            }
            if (ModOptions.CopyPasteForColorPickerNumbers.Value && cPicker.IfLBLTextHovered(out int oOO))
            {
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.C))
                {
                    cPicker.CopyOOOCPicker(oOO);
                }
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.V))
                {
                    cPicker.PasteOOOCPicker(oOO);
                }
            }
        }
        public static bool IfLBLTextHovered(this OpColorPicker cPicker, out int oOO)
        {
            oOO = -1;
            if (cPicker.IfCPickerOOO(0))
            {
                oOO = 0;
            }
            else if (cPicker.IfCPickerOOO(1))
            {
                oOO = 1;
            }
            else if (cPicker.IfCPickerOOO(2))
            {
                oOO = 2;
            }
            return oOO > -1;
        }
        public static void CopyOOOCPicker(this OpColorPicker cPicker, int oOO)
        {
            if (cPicker != null)
            {
                Manager.Clipboard = (oOO == 0 ? cPicker._lblR : oOO == 1 ? cPicker._lblG : cPicker._lblB).text;
                cPicker.PlaySound(SoundID.MENU_Player_Join_Game);
            }
        }
        public static void PasteOOOCPicker(this OpColorPicker cPicker, int oOO)
        {
            if (cPicker != null)
            {
                if (float.TryParse(Manager.Clipboard, NumberStyles.Integer | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out float newValue))
                {
                    if (TrySetOOOCPicker(cPicker, oOO, Mathf.RoundToInt(newValue)))
                    {
                        cPicker.PlaySound(SoundID.MENU_Switch_Page_In);
                        return;
                    }
                }
                cPicker.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
            }

        }
        public static bool TrySetOOOCPicker(this OpColorPicker cPicker, int oOO, int newValue)
        {
            if (cPicker != null)
            {
                switch (oOO)
                {
                    case 0: 
                        if (cPicker._mode == OpColorPicker.PickerMode.RGB)
                        {
                            if (cPicker._r != newValue)
                            {
                                cPicker._r = newValue;
                                cPicker._RGBSetValue();
                                return true;
                            }
                            break;
                        }
                        if (cPicker._mode == OpColorPicker.PickerMode.HSL)
                        {
                            if (cPicker._h != newValue)
                            {
                                cPicker._h = newValue;
                                cPicker._HSLSetValue();
                                return true;
                            }
                        }
                        break;
                    case 1:
                        if (cPicker._mode == OpColorPicker.PickerMode.RGB)
                        {
                            if (cPicker._g != newValue)
                            {
                                cPicker._g = newValue;
                                cPicker._RGBSetValue();
                                return true;
                            }
                        }
                        if (cPicker._mode == OpColorPicker.PickerMode.HSL)
                        {
                            if (cPicker._s != newValue)
                            {
                                cPicker._s = newValue;
                                cPicker._HSLSetValue();
                                return true;
                            }
                        }
                        break;
                    default :
                        if (cPicker._mode == OpColorPicker.PickerMode.RGB)
                        {
                            if (cPicker._b != newValue)
                            {
                                cPicker._b = newValue;
                                cPicker._RGBSetValue();
                                return true;
                            }
                        }
                        if (cPicker._mode == OpColorPicker.PickerMode.HSL)
                        {
                            if (cPicker._l != newValue)
                            {
                                cPicker._l = newValue;
                                cPicker._HSLSetValue();
                                return true;
                            }
                        }
                        break;
                }
            }
            return false;
        }
        public static void CopyHexCPicker(this OpColorPicker cPicker)
        {
            if (cPicker != null)
            {
                Manager.Clipboard = cPicker.value;
                cPicker.PlaySound(SoundID.MENU_Player_Join_Game);
            }
        }
        public static void PasteHexCPicker(this OpColorPicker cPicker)
        {
            if (cPicker != null)
            {
                if (cPicker.CopyFromClipboard(Manager.Clipboard))
                {
                    string newValue = Manager.Clipboard.Trim().TrimStart('#').Substring(0, 6).ToUpper();
                    if (cPicker.value != newValue)
                    {
                        cPicker.value = newValue;
                        cPicker.PlaySound(SoundID.MENU_Switch_Page_In);
                        return;
                    }
                }
                cPicker.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
            }

        }
        public static bool IfCPickerOOO(this OpColorPicker cPicker, int oOO)
        {
            bool result = false;
            if (cPicker != null)
            {
                if (cPicker._mode == OpColorPicker.PickerMode.RGB)
                {
                    result = oOO switch
                    {
                        0 => cPicker._curFocus == OpColorPicker.MiniFocus.RGB_Red,
                        1 => cPicker._curFocus == OpColorPicker.MiniFocus.RGB_Green,
                        2 => cPicker._curFocus == OpColorPicker.MiniFocus.RGB_Blue,
                        _ => false
                    };
                }
                if (cPicker._mode == OpColorPicker.PickerMode.HSL)
                {
                    if (cPicker.MenuMouseMode && ModOptions.EnableDifferentOpColorPickerHSLPos.Value)
                    {
                        result = oOO switch
                        {
                            0 => cPicker._curFocus == OpColorPicker.MiniFocus.HSL_Lightness,
                            1 => cPicker._curFocus == OpColorPicker.MiniFocus.HSL_Hue,
                            2 => cPicker._curFocus == OpColorPicker.MiniFocus.HSL_Saturation,
                            _ => false
                        };
                        return result;
                    }
                    else
                    {
                        result = oOO switch
                        {
                            0 => cPicker._curFocus == OpColorPicker.MiniFocus.HSL_Hue,
                            1 => cPicker._curFocus == OpColorPicker.MiniFocus.HSL_Saturation,
                            2 => cPicker._curFocus == OpColorPicker.MiniFocus.HSL_Lightness,
                            _ => false
                        };
                    }
                }
            }
            return result;
        }
        public static Color FindArrowColor(this OpColorPicker cPicker)
        {
            if (cPicker != null)
            {
                Color result = new(1 - cPicker._r / 100f, 1f - cPicker._g / 100f, 1f - cPicker._b / 100f);
                result = Color.Lerp(Color.white, result, Mathf.Pow(Mathf.Abs(result.grayscale - 0.5f) * 2f, 0.3f));
                return result;
            }
            return default;
        }
        public static void ApplyArrows(Texture2D texture, int oOO, Color arrowColor, bool moveUpDown, int positioner = 0)
        {
            if (texture == null)
            {
                ColorConfigMod.DebugError("ApplyArrows: Texture is null! Dont play tricks with me.");
                return;
            }
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

        //slider wonkiness (Assuming one slider is dragged only)
        public static Vector3 FixHueSliderWonkiness(Vector3 pendingHSL, Vector3 currentHSL)
        {
            if (pendingHSL.z == 1 && currentHSL.z ==  pendingHSL.z)
            {
                return new(pendingHSL.x, currentHSL.y, pendingHSL.z);
            }
            return pendingHSL;
        }
        public static Vector3 FixNonHueSliderWonkiness(Vector3 pendingHSL, Vector3 currentHSL)
        {
            if ((pendingHSL.y == 0 && pendingHSL.y == currentHSL.y) || pendingHSL.x == 0 && currentHSL.x == 1)
            {
                return new(currentHSL.x, pendingHSL.y, pendingHSL.z);
            }
            return pendingHSL;
        }
        //slider pages
        public static string GetVisualSliderValue(float visualValue, int decimalPlaces, string sign, MidpointRounding roundType = MidpointRounding.AwayFromZero)
        {
            //default is away from zero basically last digit is less than 5, go to 0, more than five go to 10
            double amt = Math.Round(visualValue, decimalPlaces, roundType);
            return amt.ToString() + sign;
        }
        public static float ChangeValueBasedOnMultipler(float newValue, float multipler, bool recieve = false)
        {
            float result = GetDivideOrMultiply(newValue, multipler, recieve);
            return recieve ? Mathf.Clamp01(result) : result;
        }
        public static float GetDivideOrMultiply(float a, float b, bool divide) => divide ? a / b : a * b;

        //basic conversion stuff
        public static string SetHSLSaveString(Vector3 hsl) => $"{hsl.x},{hsl.y},{hsl.z}";
        public static bool IfHexCodeValid(string value, out Color result) => ColorUtility.TryParseHtmlString("#" + value?.Trim()?.TrimStart('#'), out result);
        public static Color RGBClamp01(Color color) => new(Mathf.Clamp01(color.r), Mathf.Clamp01(color.g), Mathf.Clamp01(color.b));
        public static Color Vector32RGB(this Vector3 vector) => new(vector.x, vector.y, vector.z);
        public static Vector3 RGB2Vector3(this Color color) => new(color.r, color.g, color.b);
        public static Vector3 HSL2Vector3(this HSLColor hSLColor) => new(hSLColor.hue, hSLColor.saturation, hSLColor.lightness);
        public static Vector3 RXHSl2Vector3(this RXColorHSL colorHSL) => colorHSL == null? default : new(colorHSL.h, colorHSL.s, colorHSL.l);
        public static HSLColor Vector32HSL(this Vector3 hsl) => new(hsl.x, hsl.y, hsl.z);
        public static RXColorHSL Vector32RXHSL(this Vector3 hsl) => new(hsl.x, hsl.y, hsl.z);
      
        //for rw clamp
        public static Vector3 RWIIIClamp(Vector3 iii, CustomColorModel colorSpace, out Vector3 hsl, bool shouldClampHue = true)
        {
            if (colorSpace == CustomColorModel.RGB)
            {
                Color col = Vector32RGB(iii);
                hsl = Custom.RGB2HSL(new(iii.x, iii.y, iii.z));
                if (hsl.z < MenuToolObj.hslClampMin.z)
                {
                    hsl = HOOClamp(hsl, MenuToolObj.hslClampMin, MenuToolObj.hslClampMax);
                    col = HSL2RGB(hsl);
                }
                return new(col.r, col.g, col.b);
            }
            if (colorSpace == CustomColorModel.HSV)
            {
                hsl = HSV2HSL(iii);
                if ((iii.x > MenuToolObj.hslClampMax.x && shouldClampHue) || hsl.z < MenuToolObj.hslClampMin.z)
                {
                    hsl = HOOClamp(hsl, MenuToolObj.hslClampMin, shouldClampHue ? MenuToolObj.hslClampMax : default);
                    return HSL2HSV(hsl);
                }
                return iii;
            }
            hsl = HOOClamp(iii, MenuToolObj.hslClampMin, shouldClampHue ? MenuToolObj.hslClampMax : default);
            return hsl;
        }
        public static Vector3 HOOClamp(Vector3 value, Vector3 min = default, Vector3 max = default)
        {
            min = min == default ? new(0, 0, 0) : min;
            max = max == default ? new(1, 1, 1) : max;
            return new(Mathf.Clamp(value.x, min.x, max.x),
                Mathf.Clamp(value.y, min.y, max.y), Mathf.Clamp(value.z, min.z, max.z));
        }
    }
    public static class ColConversions
    {
        //2RGB
        public static Color HSV2RGB(Vector3 hsv)
        {
            return Color.HSVToRGB(hsv.x == 1 ? 0 : hsv.x, hsv.y, hsv.z);
        }
        public static Color HSL2RGB(Vector3 hsl)
        {
            return Custom.HSL2RGB(hsl.x == 1 ? 0 : hsl.x, hsl.y, hsl.z);
        }
        //2HSL
        public static Vector3 HSV2HSL(Vector3 hsv)
        {
            float lit = hsv.z * (1 - (hsv.y / 2));
            float sat = lit == 0 || lit == 1 ? 0 : (hsv.z - lit) / Mathf.Min(lit, 1 - lit);
            return new(hsv.x, sat, lit);
        }
        //2HSV
        public static Vector3 RGB2HSV(Color rgb)
        {
            Color.RGBToHSV(rgb, out float h, out float s, out float v);
            return new Vector3(h, s, v);
        }
        public static Vector3 HSL2HSV(Vector3 hsl)
        {
            float val = hsl.z + hsl.y * Mathf.Min(hsl.z, 1 - hsl.z);
            float sat = val == 0 ? 0 : 2 * (1 - (hsl.z / val));

            return new(hsl.x, sat, val);
        }
        //2HCY
        //2Hex
        public static string RGB2Hex(this Color rgb)
        {
            return ColorUtility.ToHtmlStringRGB(rgb);
        }
        public static string HSV2Hex(Vector3 hsv)
        {
            Color color = HSV2RGB(hsv);
            return ColorUtility.ToHtmlStringRGB(color);
        }
        public static string HSL2Hex(Vector3 hsl)
        {
            Color color = HSL2RGB(hsl);
            return ColorUtility.ToHtmlStringRGB(color);
        }
    }
    public enum CustomColorModel
    {
        RGB,
        HSL,
        HSV,
    }
}
