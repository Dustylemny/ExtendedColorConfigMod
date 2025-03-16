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
using MonoMod.Cil;
using System.Reflection;
using MonoMod.RuntimeDetour;
using static ColorConfig.ColConversions;
using static ColorConfig.ColorConfigHooks.SlugcatSelectMenuHooks;
using static ColorConfig.ColorConfigHooks.ExpeditionMenuHooks;
using Menu.Remix.MixedUI.ValueTypes;

namespace ColorConfig
{
    public static class Manager
    {
        //Clipboard
        public static string Clipboard
        {
            get => GUIUtility.systemCopyBuffer; set => GUIUtility.systemCopyBuffer = value;
        }
    }
    public static class MenuToolObj
    {
        //Enums
        public static Slider.SliderID RedRGB = new("RedRGB", true), GreenRGB = new("GreenRGB", true), BlueRGB = new("BlueRGB", true),
            HueHSV = new("HueHSV", true), SatHSV = new("SatHSV", true), ValHSV = new("ValHSV", true), EXPHueHSL = new("EXPHueHSL", true), 
            EXPSatHSL = new("EXPSatHSL", true), EXPLitHSL = new("EXPLitHSL", true);

        //slider IDS
        public static Slider.SliderID[] RGBSliderIDS  => [RedRGB, GreenRGB, BlueRGB];
        public static Slider.SliderID[] HSVSliderIDS => [HueHSV, SatHSV, ValHSV];
        public static Slider.SliderID[] EXPHSLSliderIDS => [EXPHueHSL, EXPSatHSL, EXPLitHSL];

        //names
        public static string[] HSLNames  => [hue, sat, lit];
        public static string[] RGBNames => [red, green, blue]; 
        public static string[] HSVNames => [hue, sat, value]; 

        //signs
        public static string[] HueOOSigns => [" °", "%", "%"];

        //showInts
        public static bool[] RGBShowInt => ModOptions.IntToFloatColorValues.Value? null : [true, true, true];
        public static bool[] HueOOShowInt => [!ModOptions.IntToFloatColorValues.Value];

        //SliderPages Multiplers;
        public static readonly float[] rgbMultipler = [255, 255, 255], hueOOMultipler = [360, 100, 100];

        //const stuff
        public const string red = "RED", green = "GREEN", blue = "BLUE", value = "VALUE", hue = "HUE", sat = "SAT", lit = "LIT";

        //RWStuff Clamp
        public static readonly Vector3 hslClampMax = new(0.99f, 1, 1);
        public static readonly Vector3 hslClampMin = new(0, 0, 0.01f);
        public static void CustomSliderSetHSL(Slider slider, float f, Vector3 hsl, Action<Vector3> applyHSL, bool clampHue = true)
        {
            if (slider?.ID != null && applyHSL != null)
            {
                if (EXPHSLSliderIDS.Contains(slider.ID))
                {
                    hsl[EXPHSLSliderIDS.FindIndex(slider.ID)] = f;
                    applyHSL.Invoke(SmallUtils.RWIIIClamp(hsl, CustomColorModel.HSL, out _, clampHue));
                }
                if (RGBSliderIDS.Contains(slider.ID))
                {
                    Color color = HSL2RGB(hsl);
                    color[RGBSliderIDS.FindIndex(slider.ID)] = f;
                    SmallUtils.Vector32RGB(SmallUtils.RWIIIClamp(SmallUtils.RGB2Vector3(SmallUtils.RGBClamp01(color)), CustomColorModel.RGB, out Vector3 newRGBHSL));
                    applyHSL.Invoke(SmallUtils.FixNonHueSliderWonkiness(newRGBHSL, hsl));
                }
                if (HSVSliderIDS.Contains(slider.ID))
                {
                    Vector3 hsv = HSL2HSV(hsl);
                    hsv[HSVSliderIDS.FindIndex(slider.ID)] = f;
                    SmallUtils.RWIIIClamp(hsv, CustomColorModel.HSV, out Vector3 newHSVHSL, clampHue);
                    applyHSL.Invoke(newHSVHSL);
                }
            }

        }
        public static bool CustomHSLValueOfSlider(Slider slider, Vector3 hsl, out float f)
        {
            f = -1;
            if (slider?.ID != null)
            {
                if (EXPHSLSliderIDS.Contains(slider.ID))
                {
                    f = hsl[EXPHSLSliderIDS.FindIndex(slider.ID)];
                }
                if (RGBSliderIDS.Contains(slider?.ID))
                {
                    f = HSL2RGB(hsl)[RGBSliderIDS.FindIndex(slider.ID)];
                }
                if (HSVSliderIDS.Contains(slider?.ID))
                {
                    f = HSL2HSV(hsl)[HSVSliderIDS.FindIndex(slider.ID)];
                }
            }
            return f >= 0;
        }
    }
    public static class SmallUtils
    {
        public static void HookMethod(this MethodInfo info, Delegate action)
        {
            if (info != null && action != null)
            {
                new Hook(MethodBase.GetMethodFromHandle(info.MethodHandle), action);
            }
        }
        public static void ILHookMethod(this MethodInfo info, ILContext.Manipulator action)
        {
            if (info != null && action != null)
            {
                new ILHook(MethodBase.GetMethodFromHandle(info.MethodHandle), action);
            }
        }
        //for faster getting collection values
        public static void AddOrReplace<K, V>(this IDictionary<K, V> list, K key, V value)
        {
            if (list == null)
            {
                return;
            }
            if (!list.ContainsKey(key))
            {
                list.Add(key, value);
                return;
            }
            list[key] = value;
        }
        public static void ApplyMethodToDictionary<K, V>(this IDictionary<K, V> dic, Action<K, V> action)
        {
            if (dic == null)
            {
                return;
            }
            foreach (K k in dic.Keys)
            {
                action?.Invoke(k, dic[k]);
            }
        }
        public static void ApplySafeMethodToDictionary<K, V>(this IDictionary<K, V> dic, Action<K, V> action)
        {
            if (dic == null)
            {
                return;
            }
            List<K> list = new(dic.Keys);
            for (int i = 0; i < list.Count; i++)
            {
                action?.Invoke(list[i], dic[list[i]]);
            }
        }
        public static RETURN ApplyReturnMethodToList<T, RETURN>(this IList<T> list, Func<T, int, bool> predidate, Func<T, int, RETURN> returnResult, RETURN ifUnsuccessful = default)
        {
            if (list != null && predidate != null && returnResult != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (predidate.Invoke(list[i], i))
                    {
                        return returnResult.Invoke(list[i], i);
                    }
                }
            }
            return ifUnsuccessful;
        }
        public static void ApplyMethodToList<T>(this IList<T> list, Action<T> action)
        {
            if (list == null)
            {
                return;
            }
            for (int i = 0; i < list.Count; i++)
            {
                action?.Invoke(list[i]);
            }
        }
        public static void ApplyMethodToList<T>(this IList<T> list, Action<T, int> action)
        {
            if (list == null)
            {
                return;
            }
            for (int i = 0; i < list.Count; i++)
            {
                action?.Invoke(list[i], i);
            }
        }
        public static IList<T> ToSingleList<T>(this T obj)
        {
            return [obj];
        }
        public static List<T> Exclude<T>(this List<T> list, int index)
        {
            T[] result = new T[list.Count - 1];
            list.CopyTo(0, result, 0, index);
            list.CopyTo(index + 1, result, index, list.Count - 1 - index);
            return [..result];
        }
        public static T GetValueOrDefault<T>(this IList<T> list, int index, T defaultValue)
        {
            return (list != null && list.Count > index && index >= 0) ? list[index] : defaultValue;
        }
        public static T GetValueOrDefault<T>(this IList<T> list, int index)
        {
            return list.GetValueOrDefault(index, default);
        }
        public static int FindIndex<T>(this IList<T> list, T value) => list.ApplyReturnMethodToList((obj, i) => value?.Equals(list[i]) == true, (obj, i) => i, -1);

        //Fixed Backtrack issues
        public static string[] FindFilePaths(string directoryName, string fileFormat = "", bool directories = false, bool includeAll = false)
        {
            return [.. AssetManager.ListDirectory(directoryName, directories, includeAll).Where(x => x.EndsWith(fileFormat))];
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
            inputPackages.ApplyMethodToList((obj, i) =>
            {
                GetInputMultiSupport(obj, ref gamePad, ref preset, ref x, ref y, ref analogue,
                 ref dDiag, ref jmp, ref thrw, ref grab, ref map, ref crouchTog, i);
            });
            return new(gamePad, preset, x, y, jmp, thrw, grab, map, crouchTog)
            {
                analogueDir = analogue,
                downDiagonal = dDiag
            };
        }
        public static void GetInputMultiSupport(Player.InputPackage input, ref bool gamePad, ref Options.ControlSetup.Preset controlType, ref int x, ref int y, ref Vector2 analogue, ref int downDiag, ref bool jmp, ref bool thw, ref bool grab, ref bool map, ref bool crouch, int num = -1)
        {
            input.analogueDir.x *= (num > -1 && Custom.rainWorld.options.controls.Length > num && Custom.rainWorld.options.controls[num].xInvert) ? - 1 : 1;
            input.analogueDir.y *= (num > -1 && Custom.rainWorld.options.controls.Length > num && Custom.rainWorld.options.controls[num].yInvert) ? -1 : 1;
            bool getControlStuff = Math.Abs(input.x) > Math.Abs(x) || Math.Abs(input.y) > Math.Abs(y) || Math.Abs(input.x) > Math.Abs(analogue.x) || (Math.Abs(input.analogueDir.y) > Math.Abs(input.y)) || Math.Abs(input.downDiagonal) > Math.Abs(downDiag)|| input.jmp || input.thrw || input.pckp || input.mp || input.crouchToggle;
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

        //extra menu stuff
        public static void ApplyPerPage(this MenuInterfaces.IDoPerPage perPage, int max, Action<int> action)
        {
            if (perPage == null)
            {
                return;
            }
            int num = perPage.CurrentOffset * perPage.PerPage;
            while (num < max && num < (perPage.CurrentOffset + 1) * perPage.PerPage)
            {
                action?.Invoke(num);
                num++;
            }
        }
        public static void ApplyPerPageList<T>(this MenuInterfaces.IDoPerPage perPage, IList<T> list, Action<T, int> action)
        {
            if (perPage == null)
            {
                return;
            }
            int num = perPage.CurrentOffset * perPage.PerPage;
            while (num < list?.Count && num < (perPage.CurrentOffset + 1) * perPage.PerPage)
            {
                action?.Invoke(list[num], num);
                num++;
            }
        }
        public static int NextPageLoopListPerPage<T>(this MenuInterfaces.IDoPerPage perPage, IList<T> list) => (list == null || list.Count == 0 || perPage.CurrentOffset + 1 > (list.Count - 1) / perPage.PerPage) ? 0 : perPage.CurrentOffset + 1;
        public static int PrevPageLoopListPerPage<T>(this MenuInterfaces.IDoPerPage perPage, IList<T> list) => (perPage.CurrentOffset - 1 < 0 ? list?.Count > 0 ? (list.Count - 1) / perPage.PerPage : 0 : perPage.CurrentOffset - 1);
        public static int NextPageLoopList<T>(this MenuInterfaces.ICanTurnPages turnPages, IList<T> list) => list == null || list.Count == 0 || turnPages.CurrentOffset + 1 >= list.Count ? 0 : turnPages.CurrentOffset + 1;
        public static int PrevPageLoopList<T>(this MenuInterfaces.ICanTurnPages turnPages, IList<T> list) => turnPages.CurrentOffset - 1 < 0 ? list?.Count > 0 ? list.Count - 1 : 0 : turnPages.CurrentOffset - 1;

        //menu stuff
        public static void ClearMenuObjectDictionary<T, E>(this MenuObject container, ref Dictionary<T, E> dictionary, bool refresh = false) where T : MenuObject
        {
            dictionary.ApplyMethodToDictionary((menuObj, e) =>
            {
                if (menuObj != null)
                {
                    menuObj.RemoveSprites();
                    container.RemoveSubObject(menuObj);
                }
            });
            dictionary = refresh ? [] : null;
        }
        public static void ClearMenuObjectList<T>(this MenuObject container, ref List<T> list, bool refresh = false) where T : MenuObject
        {
            list.ApplyMethodToList((menuObject) =>
            {
                if (menuObject != null)
                {
                    menuObject.RemoveSprites();
                    container.RemoveSubObject(menuObject);
                }
            });
            list = refresh ? [] : null;
        }
        public static void ClearMenuObjectList<T>(this MenuObject container, ref T[] list, bool refresh = false) where T : MenuObject
        {
            list.ApplyMethodToList((menuObject) =>
            {
                if (menuObject != null)
                {
                    menuObject.RemoveSprites();
                    container.RemoveSubObject(menuObject);
                }
            });
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
        public static void SafeAddSubObjects(this MenuObject container, params MenuObject[] menuobjs)
        {
            container?.subObjects?.AddRange(menuobjs.Where(x => x != null));
        }
        public static void MutualMenuObjectBind(this MenuObject menuObject, MenuObject bindWith, bool bindIsLast, bool leftRight = false, bool bottomTop = false)
        {
            if (menuObject == null || menuObject.menu == null || bindWith == null|| bindWith.menu != menuObject.menu)
            {
                return;
            }
            MenuObject first = bindIsLast ? menuObject : bindWith, second = bindIsLast ? bindWith : menuObject;
            if (leftRight)
            {
                menuObject.menu.MutualHorizontalButtonBind(first, second) ;
            }
            if (bottomTop)
            {
                menuObject.menu.MutualVerticalButtonBind(first, second) ;
            }
        }
        public static void MenuObjectBind(this MenuObject menuObject, MenuObject bindWith, bool right = false, bool left = false, bool top = false, bool bottom = false)
        {
            if (menuObject == null || bindWith == null)
            {
                return;
            }
            menuObject.nextSelectable[0] = left? bindWith : menuObject.nextSelectable[0];
            menuObject.nextSelectable[1] = top ? bindWith : menuObject.nextSelectable[1];
            menuObject.nextSelectable[2] = right ? bindWith : menuObject.nextSelectable[2];
            menuObject.nextSelectable[3] = bottom ? bindWith : menuObject.nextSelectable[3];
        }
        public static bool IsCustomColorEnabled(this Menu.Menu menu, SlugcatStats.Name id) => menu.manager.rainWorld.progression.miscProgressionData.colorsEnabled != null && menu.manager.rainWorld.progression.miscProgressionData.colorsEnabled.ContainsKey(id.value) && menu.manager.rainWorld.progression.miscProgressionData.colorsEnabled[id.value];
        public static List<Vector3> MenuHSLs(this Menu.Menu menu, SlugcatStats.Name id)
        {
            List<Vector3> list = [];
            if (menu.manager.rainWorld.progression.miscProgressionData.colorChoices.ContainsKey(id?.value) == true && menu.manager.rainWorld.progression.miscProgressionData.colorChoices[id.value] != null)
            {
                menu.manager.rainWorld.progression.miscProgressionData.colorChoices[id.value].ApplyMethodToList((hslString) =>
                {
                    Vector3 hsl = Vector3.one;
                    string[] hslArray = hslString.Split(',');
                    hsl = new(float.Parse(hslArray[0], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(hslArray[1], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(hslArray[2], NumberStyles.Any, CultureInfo.InvariantCulture));
                    list.Add(hsl);
                });
            }
            return list;
        }
        public static Vector3 MenuHSL(this Menu.Menu menu, SlugcatStats.Name id, int bodyPart)
        {
            Vector3 color = new(1, 1, 1);
            if (bodyPart >= 0 && menu.manager.rainWorld.progression.miscProgressionData.colorChoices.ContainsKey(id.value) &&
                menu.manager.rainWorld.progression.miscProgressionData.colorChoices[id.value].Count > bodyPart &&
                menu.manager.rainWorld.progression.miscProgressionData.colorChoices[id.value][bodyPart].Contains(","))
            {
                string[] hslArray = menu.manager.rainWorld.progression.miscProgressionData.colorChoices
                    [id.value][bodyPart].Split(',');
                color = new(float.Parse(hslArray[0], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(hslArray[1], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(hslArray[2], NumberStyles.Any, CultureInfo.InvariantCulture));
            }
            return color;
        }
        public static void SaveHSLString_Menu_String(this Menu.Menu menu, SlugcatStats.Name id, int bodyIndex, string newHSL)
        {
            if (!menu.manager.rainWorld.progression.miscProgressionData.colorChoices.ContainsKey(id.value))
            {
                ColorConfigMod.DebugWarning("Failed to save color choices due to slugcat not saved");
                return;
            }
            if (menu.manager.rainWorld.progression.miscProgressionData.colorChoices[id.value].Count <= bodyIndex)
            {
                ColorConfigMod.DebugWarning("Failed to save color choices due to index being more than body count!");
                return;
            }
            menu.manager.rainWorld.progression.miscProgressionData.colorChoices[id.value][bodyIndex] = newHSL;
        }
        public static void SaveHSLString_Menu_Vector3(this Menu.Menu menu, SlugcatStats.Name id, int bodyIndex, Vector3 newHSL)
        {
            if (!menu.manager.rainWorld.progression.miscProgressionData.colorChoices.ContainsKey(id.value))
            {
                ColorConfigMod.DebugWarning("Failed to save color choices due to slugcat not saved");
                return;
            }
            if (menu.manager.rainWorld.progression.miscProgressionData.colorChoices[id.value].Count <= bodyIndex)
            {
                ColorConfigMod.DebugWarning("Failed to save color choices due to index being more than body count!");
                return;
            }
            menu.manager.rainWorld.progression.miscProgressionData.colorChoices[id.value][bodyIndex] = SetHSLSaveString(newHSL);
        }
        public static Vector3 ParseHSLString (string hsl)
        {
            Vector3 color = new(1, 1, 1);
            if (hsl.Contains(','))
            {
                string[] hslArray = hsl.Split(',');
                color = new(float.Parse(hslArray[0], NumberStyles.Any, CultureInfo.InvariantCulture),
                    float.Parse(hslArray[1], NumberStyles.Any, CultureInfo.InvariantCulture),
                    float.Parse(hslArray[2], NumberStyles.Any, CultureInfo.InvariantCulture));
            }
            return color;
        }
        public static void TryFixAllColorChoices(this Menu.Menu menu)
        {
            menu?.manager?.rainWorld?.progression?.miscProgressionData?.colorChoices.ApplySafeMethodToDictionary((id, hslStrings) =>
            {
                if (hslStrings != null)
                {
                    menu.manager.rainWorld.progression.miscProgressionData.colorChoices[id] = [.. hslStrings.Select(ParseHSLString).Select(x => SetHSLSaveString(new(x.x == 1 ? 0 : x.x, x.y, x.z)))];
                }
            });
        }
        public static void TryFixColorChoices(this Menu.Menu menu, SlugcatStats.Name name)
        {
            if (menu?.manager?.rainWorld?.progression?.miscProgressionData?.colorChoices?.ContainsKey(name?.value) == true && menu.manager.rainWorld.progression.miscProgressionData.colorChoices[name.value] != null)
            {
                menu.manager.rainWorld.progression.miscProgressionData.colorChoices[name.value] = [.. menu.manager.rainWorld.progression.miscProgressionData.colorChoices[name.value].Select(ParseHSLString).Select(x => SetHSLSaveString(new(x.x == 1 ? 0 : x.x, x.y, x.z)))];

            }
        }

        //ssmstuff
        public static ExtraSSMInterfaces GetExtraSSMInterface(this SlugcatSelectMenu ssM) => ColorConfigMod.extraSSMInterfaces.GetValue(ssM, (SlugcatSelectMenu _) => new());
        public static List<Vector3> SlugcatSelectMenuHSLs(this SlugcatSelectMenu ssM) => ssM.MenuHSLs(ssM.StorySlugcat());
        public static Vector3 SlugcatSelectMenuHSL(this SlugcatSelectMenu ssM)
        {
            Vector3 color = new(1, 1, 1);
            SlugcatStats.Name name = ssM.StorySlugcat();
            if (ssM.activeColorChooser > -1 &&
                ssM.manager.rainWorld.progression.miscProgressionData.colorChoices[name.value]
                [ssM.activeColorChooser].Contains(","))
            {
                string[] hslArray = ssM.manager.rainWorld.progression.miscProgressionData.colorChoices
                    [name.value]
                [ssM.activeColorChooser].Split(',');

                color = new Vector3(float.Parse(hslArray[0], NumberStyles.Any, CultureInfo.InvariantCulture),
                    float.Parse(hslArray[1], NumberStyles.Any, CultureInfo.InvariantCulture),
                    float.Parse(hslArray[2], NumberStyles.Any, CultureInfo.InvariantCulture));
            }

            return color;
        }
        public static void UpdateInterfaceColor(this SlugcatSelectMenu.CustomColorInterface ccI, int bodyIndex)
        {
            if (ccI?.bodyColors != null && bodyIndex <= ccI.bodyColors.Length)
            {
                ccI.bodyColors[bodyIndex].color = HSL2RGB(ccI.menu.MenuHSL(ccI.slugcatID, bodyIndex));
            }
        }
        public static void SaveHSLString_Story(this SlugcatSelectMenu ssM, Vector3 newHSL) => ssM.SaveHSLString_Story_String(SetHSLSaveString(newHSL));
        public static void SaveHSLString_Story_Int(this SlugcatSelectMenu ssM, int bodyIndex, Vector3 newHSL) => ssM.SaveHSLString_Story_Int_String(bodyIndex, SetHSLSaveString(newHSL));
        public static void SaveHSLString_Story_String(this SlugcatSelectMenu ssM, string newHSL) => ssM.manager.rainWorld.progression.miscProgressionData.colorChoices[ssM.StorySlugcat().value][ssM.activeColorChooser] = newHSL;
        public static void SaveHSLString_Story_Int_String(this SlugcatSelectMenu ssM, int bodyIndex, string newHSL)
        {
            if (ssM.manager.rainWorld.progression.miscProgressionData.colorChoices[ssM.StorySlugcat().value].Count > bodyIndex)
            {
                ssM.manager.rainWorld.progression.miscProgressionData.colorChoices[ssM.StorySlugcat().value][bodyIndex] = newHSL;
                return;
            }
            ColorConfigMod.DebugWarning("Failed to save color choices due to index being more than body count!");
        }
        public static void AddSSMSliderIDGroups(List<MenuInterfaces.SliderIDGroup> IDGroups, bool shouldRemoveHSL)
        {
            if (!shouldRemoveHSL)
            {
                IDGroups.Add(new([MMFEnums.SliderID.Hue, MMFEnums.SliderID.Saturation, MMFEnums.SliderID.Lightness], MenuToolObj.HSLNames,
                    MenuToolObj.HueOOShowInt, MenuToolObj.hueOOMultipler, MenuToolObj.HueOOSigns));
            }
            if (ModOptions.EnableRGBSliders.Value)
            {
                IDGroups.Add(new([MenuToolObj.RedRGB, MenuToolObj.GreenRGB, MenuToolObj.BlueRGB],
                    MenuToolObj.RGBNames, MenuToolObj.RGBShowInt, MenuToolObj.rgbMultipler));
            }
            if (ModOptions.EnableHSVSliders.Value)
            {
                IDGroups.Add(new([MenuToolObj.HueHSV, MenuToolObj.SatHSV, MenuToolObj.ValHSV],
                   MenuToolObj.HSVNames, MenuToolObj.HueOOShowInt, MenuToolObj.hueOOMultipler, MenuToolObj.HueOOSigns));
            }
        }
        public static SlugcatStats.Name StorySlugcat(this SlugcatSelectMenu ssM)
        {
            return ssM.colorInterface != null ? ssM.colorInterface.slugcatID : ssM.slugcatColorOrder[ssM.slugcatPageIndex];
        }
        public static Vector2 BaseScreenColorInterfacePos(this ProcessManager manager)
        {
            if (manager == null)
            {
                ColorConfigMod.DebugWarning("Manager is null!");
                return Vector2.zero;
            }
            return new(1000 - (1366 - manager.rainWorld.options.ScreenSize.x) / 2, manager.rainWorld.options.ScreenSize.y - 100);
        }
        //jolly-coop stuff
        public static Slider[] JollyHSLSliders(this ColorChangeDialog.ColorSlider colSlider)
        {
            return [colSlider.hueSlider, colSlider.satSlider, colSlider.litSlider];
        }
        public static MenuInterfaces.JollyCoopOOOConfig GetExtraJollyInterface(this ColorChangeDialog.ColorSlider colSlider, /*int bodyPart,*/ bool removeHSL, bool addHexInterface)
        {
            return ColorConfigMod.extraJollyInterfaces.GetValue(colSlider, (ColorChangeDialog.ColorSlider _) =>
            {
                MenuInterfaces.JollyCoopOOOConfig config = new(colSlider.menu, colSlider, /*bodyPart,*/ removeHSL, addHexInterface);
                colSlider.subObjects.Add(config);
                return config;
            });
        }
        public static void RemoveExtraJollyInterface(this ColorChangeDialog.ColorSlider colSlider)
        {
            if (ColorConfigMod.extraJollyInterfaces.TryGetValue(colSlider, out MenuInterfaces.JollyCoopOOOConfig configPage))
            {
                if (configPage != null)
                {
                    configPage.RemoveSprites();
                    colSlider.RemoveSubObject(configPage);
                }
                ColorConfigMod.extraJollyInterfaces.Remove(colSlider);
            }
        }

        //expedition stuff
        public static ExtraExpeditionInterfaces GetExtraEXPInterface(this CharacterSelectPage charSelectPage)
        {
            return ColorConfigMod.extraEXPInterfaces.GetValue(charSelectPage, (CharacterSelectPage _) => new());
        }
        public static SlugcatStats.Name ExpeditionSlugcat() => Expedition.ExpeditionData.slugcatPlayer;
        public static Vector3 ExpeditionHSL(this MenuInterfaces.ExpeditionColorDialog self)
        {
            return self.MenuHSL(self.id, self.colorChooser);
        }
        public static void AddEXPSliderIDGroups(List<MenuInterfaces.SliderIDGroup> IDGroups, bool shouldRemoveHSL)
        {
            if (!shouldRemoveHSL)
            {
                IDGroups.Add(new([MenuToolObj.EXPHueHSL, MenuToolObj.EXPSatHSL, MenuToolObj.EXPLitHSL], MenuToolObj.HSLNames,
                    MenuToolObj.HueOOShowInt, MenuToolObj.hueOOMultipler, MenuToolObj.HueOOSigns));
            }
            AddSSMSliderIDGroups(IDGroups, true);
        }

        public static void TrySaveExpeditionColorsToCustomColors(this Menu.Menu menu, SlugcatStats.Name name, bool shouldApply = true)
        {
            PlayerGraphics.customColors = !ModManager.JollyCoop && ModManager.MMF && shouldApply && ModOptions.EnableExpeditionColorConfig.Value && menu.IsCustomColorEnabled(name) ? [.. menu.MenuHSLs(name).Select(ColConversions.HSL2RGB)] : null;
        }

        //opconfig stuff
        public static bool IsMouseMode(this UIconfig config) => config.CurrentlyFocusableMouse && config.MenuMouseMode;
        //opcolor picker stuff
        public static Vector3Int GetHSL(this OpColorPicker cPicker)
        {
            return new(cPicker._h, cPicker._s, cPicker._l);
        }
        public static Vector3 GetHSL01(this OpColorPicker cPicker)
        {
            return new(cPicker._h / 100f, cPicker._s / 100f, cPicker._l / 100f);
        }
        public static Vector3 GetHSVOrHSL100(this OpColorPicker cPicker)
        {
            return  cPicker.GetHSL();
        }
        public static Vector3 GetHSVOrHSL01(this OpColorPicker cPicker)
        {
            return cPicker.GetHSL01();
        }
        public static Vector3 ParseGetHSLORHSV01(this OpColorPicker cPicker, float? h, float? s, float? l)
        {
            Vector3 hsvHSL = cPicker.GetHSVOrHSL01();
            return new(h.GetValueOrDefault(hsvHSL.x), s.GetValueOrDefault(hsvHSL.y), l.GetValueOrDefault(hsvHSL.z));
        }
        public static Vector3 ParseGetHSLORHSV100(this OpColorPicker cPicker, float? h, float? s, float? l)
        {
            Vector3 hsvHSL = cPicker.GetHSVOrHSL100();
            return new(h.GetValueOrDefault(hsvHSL.x), s.GetValueOrDefault(hsvHSL.y), l.GetValueOrDefault(hsvHSL.z));
        }
        public static void SetHSLORHSV100(this OpColorPicker cPicker, float? h, float? s, float? l)
        {
            Vector3 hsvHSL100 = cPicker.GetHSVOrHSL100();
            if (h?.Equals(hsvHSL100.x) == false || s?.Equals(hsvHSL100.y) == false|| l?.Equals(hsvHSL100.z) == false)
            {
                cPicker.SetDirectHSLORHSV100(Mathf.RoundToInt(h ?? hsvHSL100.x), Mathf.RoundToInt(s ?? hsvHSL100.y), Mathf.RoundToInt(l ?? hsvHSL100.z));
                cPicker.PlaySound(SoundID.MENU_Scroll_Tick);
                cPicker._HSLSetValue();
            }
        }
        public static void SetDirectHSLORHSV100(this OpColorPicker cPicker, int h, int s, int l)
        {
            cPicker._h = h;
            cPicker._s = s;
            cPicker._l = l;
        }
        public static void SetHSLRGB(this OpColorPicker cPicker, int? o1, int? o2, int? o3, bool soundIfSame = true)
        {
            if (cPicker._mode == OpColorPicker.PickerMode.HSL)
            {
                if (o1?.Equals(cPicker._h) == false || o2?.Equals(cPicker._s) == false || o3?.Equals(cPicker._l) == false)
                {
                    cPicker._h = o1 ?? cPicker._h;
                    cPicker._s  = o2 ?? cPicker._s;
                    cPicker._l = o3 ?? cPicker._l;
                    cPicker.CPickerSetValue();
                    return;
                }
            }
            if (cPicker._mode == OpColorPicker.PickerMode.RGB)
            {
                if (o1?.Equals(cPicker._r) == false || o2?.Equals(cPicker._g) == false || o3?.Equals(cPicker._b) == false)
                {
                    cPicker._r = o1 ?? cPicker._r;
                    cPicker._g = o2 ?? cPicker._g;
                    cPicker._b = o3 ?? cPicker._b;
                    cPicker.CPickerSetValue();
                    return;
                }
            }
            if (soundIfSame)
            {
                cPicker.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
            }
        }
        public static bool IsFLabelHovered(this FLabel label, Vector2 mousePosition)
        {
            if (label != null)
            {
                Vector2 pos = label.GetPosition(), startRange = new(pos.x + label.textRect.x, pos.y + label.textRect.y),
                    endRange = startRange + new Vector2(label.textRect.width, label.textRect.height);
                return mousePosition.IsVector2MoreOrEqualThan(startRange) && mousePosition.IsVector2LessOrEqualThan(endRange);
            }
            return false;
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
                    case 2:
                        Manager.Clipboard = cPicker._lblB.text;
                        break;
                }
                cPicker.PlaySound(SoundID.MENU_Player_Join_Game);
            }
        }
        public static void PasteNumberCPicker(this OpColorPicker cPicker, int oOO)
        {
            if (cPicker == null)
            {
                return;
            }
            string newValue = Manager.Clipboard?.Trim();
            if (float.TryParse(newValue, NumberStyles.Integer | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float result))
            {
                int toPut = Mathf.Clamp(Mathf.RoundToInt(result), 0, 100);
                cPicker.SetHSLRGB(oOO == 0 ? toPut : null, oOO == 1 ? toPut : null, oOO == 2 ? toPut : null);
                return;
            }
            cPicker.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
        }
        public static void CPickerSetValue(this OpColorPicker cPicker)
        {
            if (cPicker == null)
            {
                return;
            }
            if (cPicker._mode == OpColorPicker.PickerMode.RGB)
            {
                cPicker._RGBSetValue();
            }
            if (cPicker._mode == OpColorPicker.PickerMode.HSL)
            {
                cPicker._HSLSetValue();
            }
            cPicker.PlaySound(SoundID.MENU_Player_Join_Game);

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
                if (MenuColorEffect.IsStringHexColor(newValue) && !newValue.IsHexCodesSame(cPicker.value))
                {
                    cPicker.value = newValue.Substring(0, 6).ToUpper();
                    cPicker.PlaySound(SoundID.MENU_Switch_Page_In);
                    return;
                }
                cPicker.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
            }

        }
        public static Color FindHSLArrowColor(this OpColorPicker cPicker)
        {
            if (cPicker != null)
            {
                Color result = new(1 - cPicker._r / 100f, 1f - cPicker._g / 100f, 1f - cPicker._b / 100f);
                result = Color.Lerp(Color.white, result, Mathf.Pow(Mathf.Abs(result.grayscale - 0.5f) * 2f, 0.3f));
                return result;
            }
            return default;
        }
        public static void ApplyHSLArrows(Texture2D texture, int oOO, Color arrowColor, bool moveUpDown, int positioner = 0)
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

        //sliderpages
        public static void ChangeSliderID<T, ID>(this T slider, ID id) where T : Slider where ID : Slider.SliderID
        {
            if (slider != null)
            {
                slider.ID = id;
            }
        }
        public static void ChangeSliderIDValue<T, ID>(this T slider, ID id) where T : Slider where ID : Slider.SliderID
        {
            if (slider != null)
            {
                slider.ID = id;
                slider.floatValue = slider.floatValue;
            }
        }
        public static void UpdateSliderValue<T>(this T slider) where T : Slider
        {
            if (slider != null)
            {
                slider.floatValue = slider.floatValue;
            }
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
            //assuming first and second are hexcodes
            string one = value.TrimStart('#');
            string two = newvalue.TrimStart('#');
            one = one.Substring(0, rGBA? 8 : 6);
            two = two.Substring(0, rGBA ? 8 : 6);
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
                    hsl = Vector3Clamp(hsl, MenuToolObj.hslClampMin, MenuToolObj.hslClampMax);
                    col = HSL2RGB(hsl);
                }
                return new(col.r, col.g, col.b);
            }
            if (colorSpace == CustomColorModel.HSV)
            {
                hsl = HSV2HSL(iii);
                if ((iii.x > MenuToolObj.hslClampMax.x && shouldClampHue) || hsl.z < MenuToolObj.hslClampMin.z)
                {
                    hsl = Vector3Clamp(hsl, MenuToolObj.hslClampMin, shouldClampHue ? MenuToolObj.hslClampMax : default);
                    return HSL2HSV(hsl);
                }
                return iii;
            }
            hsl = Vector3Clamp(iii, MenuToolObj.hslClampMin, shouldClampHue ? MenuToolObj.hslClampMax : default);
            return hsl;
        }
        public static Vector3 Vector3Clamp(Vector3 value, Vector3 min = default, Vector3 max = default)
        {
            min = min == default ? Vector3.zero : min;
            max = max == default ? Vector3.one : max;
            return new(Mathf.Clamp(value.x, min.x, max.x),
                Mathf.Clamp(value.y, min.y, max.y), Mathf.Clamp(value.z, min.z, max.z));
        }
        public static bool IsVector2MoreOrEqualThan(this Vector2 value, Vector2 compare)
        {
            return value.x >= compare.x && value.y >= compare.y;
        }
        public static bool IsVector2LessOrEqualThan(this Vector2 value, Vector2 compare)
        {
            return value.x <= compare.x && value.y <= compare.y;
        }
    }
    public static class ColConversions
    {
        public static Color HSV2RGB(Vector3 hsv) => Color.HSVToRGB(hsv.x == 1 ? 0 : hsv.x, hsv.y, hsv.z);
        public static Color HSL2RGB(Vector3 hsl) => Custom.HSL2RGB(hsl.x == 1 ? 0 : hsl.x, hsl.y, hsl.z);
        public static Vector3 HSV2HSL(Vector3 hsv)
        {
            float lit = hsv.z * (1 - (hsv.y / 2));
            float sat = lit == 0 || lit == 1 ? 0 : (hsv.z - lit) / Mathf.Min(lit, 1 - lit);
            return new(hsv.x, sat, lit);
        }
        public static Vector3 RGB2HSV(Color rgb)
        {
            Color.RGBToHSV(rgb, out float h, out float s, out float v);
            return new(h, s, v);
        }
        public static Vector3 HSL2HSV(Vector3 hsl)
        {
            float val = hsl.z + hsl.y * Mathf.Min(hsl.z, 1 - hsl.z);
            float sat = val == 0 ? 0 : 2 * (1 - (hsl.z / val));
            return new(hsl.x, sat, val);
        }
        public static string RGB2Hex(this Color rgb) => ColorUtility.ToHtmlStringRGB(rgb);
        public static string HSV2Hex(Vector3 hsv) => HSV2RGB(hsv).RGB2Hex();
        public static string HSL2Hex(Vector3 hsl) => HSL2RGB(hsl).RGB2Hex();

    }
    public enum CustomColorModel
    {
        RGB,
        HSL,
        HSV,
    }
}
