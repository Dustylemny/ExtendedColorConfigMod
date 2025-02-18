using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using Menu;
using RWCustom;
using UnityEngine;
using Menu.Remix.MixedUI;
using MoreSlugcats;
using JollyCoop.JollyMenu;
using static ColorConfig.ColConversions;
using System.Reflection;
using System.Linq.Expressions;
using System.Diagnostics;

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
        public static bool[] RGBShowInt => ModOptions.IntToFloatColorValues.Value? null : [true, true, true];
        public static bool[] HueOOShowInt => [!ModOptions.IntToFloatColorValues.Value];

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

        //RWStuff Clamp
        public static readonly Vector3 hslClampMax = new(0.99f, 1, 1);
        public static readonly Vector3 hslClampMin = new(0, 0, 0.01f);
    }
    public static class SmallUtils
    {
        //main
        public const string id = "dusty.colorconfig";
        public const string name = "Extended Color Config";
        public const string version = "1.2.9";

        //for faster getting collection values and defaulting if failed to find
        public static IList<T> ToSingleList<T>(this T obj)
        {
            return [obj];
        }
        public static List<T> Exclude<T>(this List<T> list, int index)
        {
            int listCount = list.Count;
            T[] result = new T[listCount - 1];
            list.CopyTo(0, result, 0, index);
            list.CopyTo(index + 1, result, index, listCount - 1 - index);
            return [..result];
        }
        public static T GetValueOrDefault<T>(this IList<T> list, int index, T defaultValue)
        {
            return (list != null && list.Count > index) ? list[index] : defaultValue;
        }


        //Fixed Backtrack issues
        public static string[] FindFilePaths(string directoryName, string fileFormat = "", bool directories = false, bool includeAll = false)
        {
            return AssetManager.ListDirectory(directoryName, directories, includeAll).Where(x => fileFormat == null || fileFormat == "" || x.EndsWith(fileFormat)).ToArray();
        }

        //For inputs
        public static MenuInterfaces.ExtraFixedMenuInput GetFixedExtraMenuInput()
        {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
                cpy = ctrl && Input.GetKey(KeyCode.C),
                pst = ctrl && Input.GetKey(KeyCode.V);
            return new(cpy, pst);
        }
        public static Player.InputPackage FixedPlayerUIInput(int playerNumber)
        {
            //fixes grab not working
            playerNumber = (Custom.rainWorld.processManager == null || !Custom.rainWorld.processManager.IsGameInMultiplayerContext()) && playerNumber < 0? 0 : playerNumber;
            if (playerNumber >= 0)
            {
                return RWInput.PlayerInputLogic(0, playerNumber);
            }
            Player.InputPackage[] inputs =
            [
                RWInput.PlayerInputLogic(0, 0),
                RWInput.PlayerInputLogic(0, 1),
                RWInput.PlayerInputLogic(0, 2),
                RWInput.PlayerInputLogic(0, 3),
            ];
            return MultiplayerInput(inputs);
        }
        public static Player.InputPackage MultiplayerInput(Player.InputPackage[] inputPackages)
        {
            int x = 0, y = 0, dDiag = 0;
            bool gamePad = false, jmp = false, thrw = false, grab = false, map = false, crouchTog = false;
            Options.ControlSetup.Preset preset = Options.ControlSetup.Preset.KeyboardSinglePlayer;
            Vector2 analogue = Vector2.zero;
            for (int i = 0; i < inputPackages.Length; i++)
            {
                GetInputMultiSupport(inputPackages[i], ref gamePad, ref preset, ref x, ref y, ref analogue, 
                    ref dDiag, ref jmp, ref thrw, ref grab, ref map, ref crouchTog, i);
            }
            return new(gamePad, preset, x, y, jmp, thrw, grab, map, crouchTog)
            {
                analogueDir = analogue,
                downDiagonal = dDiag
            };
        }
        public static void GetInputMultiSupport(Player.InputPackage input, ref bool gamePad, 
            ref Options.ControlSetup.Preset controlType, ref int x, ref int y, ref Vector2 analogue, ref int downDiag,
            ref bool jmp, ref bool thw, ref bool grab, ref bool map, ref bool crouch, int num = -1)
        {
            input.analogueDir.x *= (num > -1 && Custom.rainWorld.options.controls.Length > num && Custom.rainWorld.options.controls[num].xInvert) ? - 1 : 1;
            input.analogueDir.y *= (num > -1 && Custom.rainWorld.options.controls.Length > num && Custom.rainWorld.options.controls[num].yInvert) ? -1 : 1;
            bool getControlStuff = Math.Abs(input.x) > Math.Abs(x) || Math.Abs(input.y) > Math.Abs(y) ||
                Math.Abs(input.x) > Math.Abs(analogue.x) || (Math.Abs(input.analogueDir.y) > Math.Abs(input.y)) || Math.Abs(input.downDiagonal) > Math.Abs(downDiag)
                || input.jmp || input.thrw || input.pckp || input.mp || input.crouchToggle;
            x = Math.Abs(input.x) > Math.Abs(x)? input.x : x;
            y = Math.Abs(input.y) > Math.Abs(y)? input.y : y;
            jmp = input.jmp || jmp;
            thw = input.thrw || thw;
            grab = input.pckp || grab;
            map = input.mp || map;
            crouch = input.crouchToggle || crouch;
            controlType = getControlStuff ? input.controllerType : controlType;
            gamePad = (getControlStuff && input.gamePad) || gamePad;

        }

        //menu stuff
        public static void ClearMenuObjectList<T>(this MenuObject container, ref T[] list, bool refresh = false) where T : MenuObject
        {
            if (list != null)
            {
                foreach (T menuObject in list)
                {
                    menuObject?.RemoveSprites();
                    container.RemoveSubObject(menuObject);
                }
            }
            list = refresh ? [] : null;
        }
        public static void ClearMenuObject<T>(this MenuObject container, ref T desiredMenuObj) where T : MenuObject
        {
            if (desiredMenuObj != null)
            {
                desiredMenuObj.RemoveSprites();
                container.RemoveSubObject(desiredMenuObj);
                desiredMenuObj = null;
            }
        }
        public static void MenuObjectBind(this MenuObject menuObject, MenuObject bindWith, bool right = false, bool left = false, bool top = false, bool bottom = false)
        {
            if (menuObject == null)
            {
                return;
            }
            if (left)
            {
                menuObject.nextSelectable[0] = bindWith;
            }
            if (top)
            {
                menuObject.nextSelectable[1] = bindWith;
            }
            if (right)
            {
                menuObject.nextSelectable[2] = bindWith;
            }
            if (bottom)
            {
                menuObject.nextSelectable[3] = bindWith;
            }
        }
        public static Vector3 MenuHSL(this Menu.Menu menu, SlugcatStats.Name id, int bodyPart)
        {
            Vector3 color = new(1, 1, 1);
            if (menu.manager.rainWorld.progression.miscProgressionData.colorChoices.ContainsKey(id?.value) &&
                menu.manager.rainWorld.progression.miscProgressionData.colorChoices[id.value].Count > bodyPart &&
                menu.manager.rainWorld.progression.miscProgressionData.colorChoices[id.value][bodyPart].Contains(","))
            {
                string[] hslArray = menu.manager.rainWorld.progression.miscProgressionData.colorChoices
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
            if (!menu.manager.rainWorld.progression.miscProgressionData.colorChoices.ContainsKey(id.value))
            {
                ColorConfigMod.DebugError("SaveHSLString_Menu_bodyIndex: Failed to save color choices due to slugcat not saved");
                return;
            }
            if (menu.manager.rainWorld.progression.miscProgressionData.colorChoices[id.value].Count <= bodyIndex)
            {
                ColorConfigMod.DebugLog("SaveHSLString_Menu_bodyIndex: Failed to save color choices due to index being more than body count!");
                return;
            }
            menu.manager.rainWorld.progression.miscProgressionData.colorChoices[id.value][bodyIndex] = newHSL;
        }

        //ssmstuff

        public static void AddSSMSliderIDGroups(List<MenuInterfaces.SliderOOOIDGroup> IDGroups, bool shouldRemoveHSL)
        {
            if (!shouldRemoveHSL)
            {
                IDGroups.Add(new(MMFEnums.SliderID.Hue, MMFEnums.SliderID.Saturation, MMFEnums.SliderID.Lightness, MenuToolObj.HSLNames,
                    MenuToolObj.HueOOShowInt, MenuToolObj.hueOOMultipler, MenuToolObj.HueOOSigns));
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
            ColorConfigMod.DebugLog("SaveHSLString_SSM_bodyIndex: Failed to save color choices due to index being more than body count!");
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
            if (ModOptions.EnableJollyRGBSliders)
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
        public static bool IsFLabelHovered(this FLabel label, Vector2 mousePosition)
        {
            if (label != null)
            {
                Vector2 pos = label.GetPosition();
                return mousePosition.y >= pos.y + label.textRect.y &&
                    mousePosition.y <= pos.y + label.textRect.y + label.textRect.height &&
                    mousePosition.x >= pos.x + label.textRect.x && mousePosition.x <= pos.x + label.textRect.x + label.textRect.width;
            }
            return false;
        }
        public static Vector3 HslFromColorPicker(this OpColorPicker cPicker)
        {
            if (cPicker == null)
            {
                return default;
            }
            return new(cPicker._h / 100f, cPicker._s / 100f, cPicker._l / 100f);
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
        public static void CopyNumberCPicker(this OpColorPicker cPicker, int oOO)
        {
            if (cPicker != null)
            {
               switch (oOO)
                {
                    case 0:
                        Manager.Clipboard = cPicker._lblR.text;
                        break;
                    case 1:
                        Manager.Clipboard = cPicker._lblG.text;
                        break;
                    case '_':
                        Manager.Clipboard = cPicker._lblB.text;
                        break;
                }
                cPicker.PlaySound(SoundID.MENU_Player_Join_Game);
            }
        }
        public static void PasteNumberCPicker(this OpColorPicker cPicker, int oOO)
        {
            if (cPicker != null)
            {
                string newValue = Manager.Clipboard?.Trim();
                if (float.TryParse(newValue, NumberStyles.Integer | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float result))
                {
                    int toPut = Mathf.Clamp(Mathf.RoundToInt(result), 0, 100);
                    switch (oOO)
                    {
                        case 0:
                            if (cPicker._mode == OpColorPicker.PickerMode.HSL && cPicker._h != toPut)
                            {
                                cPicker._h = toPut;
                                cPicker._HSLSetValue();
                                cPicker.PlaySound(SoundID.MENU_Player_Join_Game);
                                return;
                            }
                            if (cPicker._mode == OpColorPicker.PickerMode.RGB && cPicker._r != toPut)
                            {
                                cPicker._r = toPut;
                                cPicker._RGBSetValue();
                                cPicker.PlaySound(SoundID.MENU_Player_Join_Game);
                                return;
                            }
                            break;
                        case 1:
                            if (cPicker._mode == OpColorPicker.PickerMode.HSL && cPicker._s != toPut)
                            {
                                cPicker._s = toPut;
                                cPicker._HSLSetValue();
                                cPicker.PlaySound(SoundID.MENU_Player_Join_Game);
                                return;
                            }
                            if (cPicker._mode == OpColorPicker.PickerMode.RGB && cPicker._g != toPut)
                            {
                                cPicker._g = toPut;
                                cPicker._RGBSetValue();
                                cPicker.PlaySound(SoundID.MENU_Player_Join_Game);
                                return;
                            }
                            break;
                        case 2:
                            if (cPicker._mode == OpColorPicker.PickerMode.HSL && cPicker._l != toPut)
                            {
                                cPicker._l = toPut;
                                cPicker._HSLSetValue();
                                cPicker.PlaySound(SoundID.MENU_Player_Join_Game);
                                return;
                            }
                            if (cPicker._mode == OpColorPicker.PickerMode.RGB && cPicker._b != toPut)
                            {
                                cPicker._b = toPut;
                                cPicker._RGBSetValue();
                                cPicker.PlaySound(SoundID.MENU_Player_Join_Game);
                                return;
                            }
                            break;
                    }

                }
                cPicker.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
            }
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
                string newValue = Manager.Clipboard?.Trim()?.TrimStart('#');
                if (MenuColorEffect.IsStringHexColor(newValue))
                {
                    cPicker.value = newValue.Substring(0, 6).ToUpper();
                    cPicker.PlaySound(SoundID.MENU_Switch_Page_In);
                    return;
                }
                cPicker.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
            }

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
        public static Vector3 FixNonHueSliderWonkiness(Vector3 pendingHSL, Vector3 currentHSL)
        {
            if ((pendingHSL.y == 0 && pendingHSL.y == currentHSL.y) || pendingHSL.x == 0 && currentHSL.x == 1)
            {
                return new(currentHSL.x, pendingHSL.y, pendingHSL.z);
            }
            return pendingHSL;
        }

        //copypaste
        public static bool CopyShortcutPressed()
        {
            return ColorConfigMod.femInput.cpy && !ColorConfigMod.lastFemInput.cpy;
        }
        public static bool PasteShortcutPressed()
        {
            return ColorConfigMod.femInput.pste && !ColorConfigMod.lastFemInput.pste;
        }


        //basic conversion stuff
        public static string SetHSLSaveString(Vector3 hsl) => $"{hsl.x},{hsl.y},{hsl.z}";
        public static bool IsHexCodesSame(this string value, string newvalue, bool rGBA = false, bool capSensitive = false)
        {
            string one = value?.TrimStart('#');
            string two = newvalue?.TrimStart('#');
            if (!rGBA)
            {
                one = one?.Substring(0, 6);
                two = two?.Substring(0, 6);
            }
            return one.Equals(two, capSensitive? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
        }
        public static bool IfHexCodeValid(string value, out Color result)
        {
            return ColorUtility.TryParseHtmlString("#" + value?.TrimStart('#'), out result);
        }
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
        public static bool IsVector3MoreOrEqualThan(this Vector3 value, Vector3 compare)
        {
            return value.x >= compare.x && value.y >= compare.y && value.z >= compare.z;
        }
        public static bool IsVector3LessOrEqualThan(this Vector3 value, Vector3 compare)
        {
            return value.x <= compare.x && value.y <= compare.y && value.z <= compare.z;
        }
        //modoptions
        public static bool IsEnumORExtEnum(this Type type)
        {
            return type.IsEnum || type.IsExtEnum();
        }
    }
    public static class ColConversions
    {
        //converts based on 0-1
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
            return color.RGB2Hex();
        }
        public static string HSL2Hex(Vector3 hsl)
        {
            Color color = HSL2RGB(hsl);
            return color.RGB2Hex();
        }
    }
    public enum CustomColorModel
    {
        RGB,
        HSL,
        HSV,
    }
}
