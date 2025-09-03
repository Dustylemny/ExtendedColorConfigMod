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
using ColorConfig.MenuUI;
using ColorConfig.WeakUITable;
using static ColorConfig.ColConversions;
using System.Data.SqlTypes;
using ColorConfig.MenuUI.Objects;

namespace ColorConfig
{
    public static partial class SmallUtils
    {
        public static void HookMethod(this MethodInfo info, Delegate action)
        {
            if (info == null)
            {
                ColorConfigMod.DebugWarning("MethodInfo is null!");
                return;
            }
            if (action == null)
            {
                ColorConfigMod.DebugWarning("Action to hook with is null!");
                return;
            }
            new Hook(MethodBase.GetMethodFromHandle(info.MethodHandle), action);
            
        }
        public static void ILHookMethod(this MethodInfo info, ILContext.Manipulator action)
        {
            if (info == null)
            {
                ColorConfigMod.DebugWarning("MethodInfo is null!");
                return;
            }
            if (action == null)
            {
                ColorConfigMod.DebugWarning("Action to hook with is null!");
                return;
            }
            new ILHook(MethodBase.GetMethodFromHandle(info.MethodHandle), action);

        }

        //for faster getting collection values
        public static List<T> Exclude<T>(this List<T> list, int index)
        {
            T[] result = new T[list.Count - 1];
            list.CopyTo(0, result, 0, index);
            list.CopyTo(index + 1, result, index, list.Count - 1 - index);
            return [..result];
        }
        public static T? ValueOrDefault<T>(this IList<T>? list, int index, T? defaultValue) => (list != null && list.Count > index && index >= 0) ? list[index] : defaultValue;
        public static T? ValueOrDefault<T>(this IList<T>? list, int index) => ValueOrDefault(list!, index, default);

        //Fixed Backtrack issues
        public static string[] FindFilePaths(string directoryName, string fileFormat = "", bool directories = false, bool includeAll = false) => [.. AssetManager.ListDirectory(directoryName, directories, includeAll).Where(x => x.EndsWith(fileFormat))];

        //For inputs
      
        //use this since normal ui input is stingy af and doesnt check for grab, spec, and map is strangely another mystery bind in UiInput
        public static Player.InputPackage FixedPlayerUIInput(int playerNumber)
        {
            //made this cuz rw ui input is stingy af
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
                GetInputMultiSupport(inputPackages[i], ref gamePad, ref preset, ref x, ref y, ref analogue, ref dDiag, ref jmp, ref thrw, ref grab, ref map, ref crouchTog, i);
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
        public static InputExtras GetInputExtras(this object? obj) => obj != null ?ColorConfigMod.inputExtras.GetValue(obj, _ => new()) : new();

        public static void HelpPopulatePage(int listCount, int currentOffset, int perPage, Action<int> action)
        {
            int num = currentOffset * perPage;
            int max = Mathf.Min(listCount - num, perPage);
            for (int i = 0; i < max; i++)
                action(i + num);
        }
        public static int TryGoToNextPage(int listCount, int currentOffset, int objectsPerPage = 1)
        {
            int newOffset = currentOffset + 1;
            if (listCount == 0 || newOffset > (listCount - 1) / objectsPerPage)
                return 0;
            return newOffset;
        }
        public static int TryGoToPrevPage(int listCount, int currentOffset, int objectsPerPage = 1)
        {
            int newOffset = currentOffset - 1;
            if (newOffset < 0)
                return Mathf.Max((listCount - 1) / objectsPerPage, 0);
            return newOffset;
        }

        //menu stuff
        public static void ClearMenuObjectList(this MenuObject? container, MenuObject[] list)
        {
            for (int i = 0; i < list.Length; i++)
                container?.ClearMenuObject(list[i]);
        }
        public static void ClearMenuObject<T>(this MenuObject container, ref T? desiredMenuObj) where T : MenuObject
        {
            container.ClearMenuObject(desiredMenuObj);
            desiredMenuObj = null;
        }
        public static void ClearMenuObject(this MenuObject container, MenuObject? obj)
        {
            if (obj == null) return;
            obj.RemoveSprites();
            container.RemoveSubObject(obj);
        }
        public static void SafeAddSubObjects(this MenuObject? container, params MenuObject?[] menuobjs) => container?.subObjects?.AddRange(menuobjs.Where(x => x != null));
        public static void MutualMenuObjectBind(this MenuObject menuObject, MenuObject bindWith, bool bindIsLast, bool leftRight = false, bool bottomTop = false)
        {
            if (menuObject == null || menuObject.menu == null || bindWith == null|| bindWith.menu != menuObject.menu) return;
            MenuObject first = bindIsLast ? menuObject : bindWith, second = bindIsLast ? bindWith : menuObject;
            if (leftRight)
                menuObject.menu.MutualHorizontalButtonBind(first, second);
            if (bottomTop)
                menuObject.menu.MutualVerticalButtonBind(first, second);
        }
        public static void MutualMenuObjectBind(this Menu.Menu menu, MenuObject? firstBind, MenuObject? secondBind, bool leftRight = false, bool bottomTop = false)
        {
            if (firstBind == null || secondBind == null) return;
            if (leftRight)
                menu.MutualHorizontalButtonBind(firstBind, secondBind);
            if (bottomTop)
                menu.MutualVerticalButtonBind(firstBind, secondBind);
        }
        public static void MenuObjectBind(this MenuObject? menuObject, MenuObject? bindWith, bool right = false, bool left = false, bool top = false, bool bottom = false)
        {
            if (menuObject == null || bindWith == null) return;
            menuObject.nextSelectable[0] = left? bindWith : menuObject.nextSelectable[0];
            menuObject.nextSelectable[1] = top ? bindWith : menuObject.nextSelectable[1];
            menuObject.nextSelectable[2] = right ? bindWith : menuObject.nextSelectable[2];
            menuObject.nextSelectable[3] = bottom ? bindWith : menuObject.nextSelectable[3];
        }
        public static bool IsCustomColorEnabled(this Menu.Menu menu, SlugcatStats.Name id) => menu.manager.rainWorld.progression.miscProgressionData.colorsEnabled != null && menu.manager.rainWorld.progression.miscProgressionData.colorsEnabled.ContainsKey(id.value) && menu.manager.rainWorld.progression.miscProgressionData.colorsEnabled[id.value];
        public static List<Vector3> MenuHSLs(this Menu.Menu menu, SlugcatStats.Name? id)
        {
            List<Vector3> list = [];
            if (id != null && menu.manager.rainWorld.progression.miscProgressionData.colorChoices.ContainsKey(id.value) == true && menu.manager.rainWorld.progression.miscProgressionData.colorChoices[id.value] != null)
            {
                List<string>? hslStrings = menu.manager.rainWorld.progression.miscProgressionData.colorChoices[id.value];
                for (int i = 0; i < hslStrings?.Count; i++)
                {
                    Vector3 hsl = Vector3.one;
                    string[] hslArray = hslStrings[i].Split(',');
                    if (hslArray.Length > 2)
                        hsl = new(float.Parse(hslArray[0], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(hslArray[1], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(hslArray[2], NumberStyles.Any, CultureInfo.InvariantCulture));
                    list.Add(hsl);
                }
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
            if (menu?.manager?.rainWorld?.progression?.miscProgressionData?.colorChoices == null) return;
            List<string> slugcats = [.. menu.manager.rainWorld.progression.miscProgressionData.colorChoices.Keys];
            for (int i = 0; i < slugcats.Count; i++)
                menu.manager.rainWorld.progression.miscProgressionData.colorChoices[slugcats[i]] = [.. menu.manager.rainWorld.progression.miscProgressionData.colorChoices[slugcats[i]].Select(ParseHSLString).Select(x => SetHSLSaveString(new(x.x == 1 ? 0 : x.x, x.y, x.z)))];
        }
        public static void TryFixColorChoices(this Menu.Menu? menu, SlugcatStats.Name? name)
        {
            if (name != null && menu?.manager?.rainWorld?.progression?.miscProgressionData?.colorChoices?.ContainsKey(name.value) == true && menu.manager.rainWorld.progression.miscProgressionData.colorChoices[name.value] != null)
                menu.manager.rainWorld.progression.miscProgressionData.colorChoices[name.value] = [.. menu.manager.rainWorld.progression.miscProgressionData.colorChoices[name.value].Select(ParseHSLString).Select(x => SetHSLSaveString(new(x.x == 1 ? 0 : x.x, x.y, x.z)))];
        }

        //ssmstuff
        public static SlugcatStats.Name StorySlugcat(this SlugcatSelectMenu ssM) => ssM.colorInterface != null ? ssM.colorInterface.slugcatID : ssM.slugcatColorOrder[ssM.slugcatPageIndex];
        public static ExtraSSMInterfaces GetExtraSSMInterface(this object ssM) => ColorConfigMod.extraSSMInterfaces.GetValue(ssM, (_) => new());
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
                ccI.bodyColors[bodyIndex].color = HSL2RGB(ccI.menu.MenuHSL(ccI.slugcatID, bodyIndex));
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
        public static void AddSSMSliderIDGroups(List<SliderIDGroup> IDGroups, bool shouldRemoveHSL)
        {
            if (!shouldRemoveHSL)
                IDGroups.Add(new([MMFEnums.SliderID.Hue, MMFEnums.SliderID.Saturation, MMFEnums.SliderID.Lightness], MenuToolObj.HSLNames, MenuToolObj.HueOOShowInt, MenuToolObj.hueOOMultipler, MenuToolObj.HueOOSigns));
            if (ModOptions.Instance.EnableRGBSliders.Value)
                IDGroups.Add(new(MenuToolObj.RGBSliderIDS, MenuToolObj.RGBNames, MenuToolObj.RGBShowInt, MenuToolObj.rgbMultipler));
            if (ModOptions.Instance.EnableHSVSliders.Value)
                IDGroups.Add(new(MenuToolObj.HSVSliderIDS, MenuToolObj.HSVNames, MenuToolObj.HueOOShowInt, MenuToolObj.hueOOMultipler, MenuToolObj.HueOOSigns));
        }
      
        public static Vector2 BaseScreenColorInterfacePos(this ProcessManager manager) => new(1000 - (1366 - manager.rainWorld.options.ScreenSize.x) / 2, manager.rainWorld.options.ScreenSize.y - 100);
        //jolly-coop stuff
        public static Slider[] JollyHSLSliders(this ColorChangeDialog.ColorSlider colSlider)
        {
            return [colSlider.hueSlider, colSlider.satSlider, colSlider.litSlider];
        }
        public static JollyCoopOOOConfig GetExtraJollyInterface(this ColorChangeDialog.ColorSlider colSlider, /*int bodyPart,*/ bool removeHSL, bool addHexInterface)
        {
            return ColorConfigMod.extraJollyInterfaces.GetValue(colSlider, (ColorChangeDialog.ColorSlider _) =>
            {
                JollyCoopOOOConfig config = new(colSlider.menu, colSlider, /*bodyPart,*/ removeHSL, addHexInterface);
                colSlider.subObjects.Add(config);
                return config;
            });
        }
        public static void RemoveExtraJollyInterface(this ColorChangeDialog.ColorSlider colSlider)
        {
            if (ColorConfigMod.extraJollyInterfaces.TryGetValue(colSlider, out JollyCoopOOOConfig configPage))
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
        public static ExtraExpeditionInterfaces GetExtraEXPInterface(this object charSelectPage) => ColorConfigMod.extraEXPInterfaces.GetValue(charSelectPage, (_) => new());
        public static SlugcatStats.Name ExpeditionSlugcat() => Expedition.ExpeditionData.slugcatPlayer;
        public static void TrySaveExpeditionColorsToCustomColors(this Menu.Menu menu, SlugcatStats.Name name) => PlayerGraphics.customColors = !ModManager.JollyCoop && menu.IsCustomColorEnabled(name) && ModOptions.Instance.EnableExpeditionColorConfig.Value ? [.. menu.MenuHSLs(name).Select(ColConversions.HSL2RGB)] : null;

        //slider wonkiness (Assuming one slider is dragged only)
        public static Vector3 FixNonHueSliderWonkiness(Vector3 pendingHSL, Vector3 currentHSL)
        {
            if ((pendingHSL.y == 0 && pendingHSL.y == currentHSL.y) || pendingHSL.x == 0 && currentHSL.x == 1) return new(currentHSL.x, pendingHSL.y, pendingHSL.z);
            return pendingHSL;
        }

        //sliderpages
        public static void ChangeSliderID<T>(this T? slider, Slider.SliderID id) where T : Slider
        {
            if (slider == null) return;
            slider.ID = id;
        }
        public static void UpdateSliderValue<T>(this T? slider) where T : Slider
        {
            if (slider == null) return;
            slider.floatValue = slider.floatValue;
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
        public static bool IfHexCodeValid(string? value, out Color result) => ColorUtility.TryParseHtmlString("#" + value?.TrimStart('#'), out result);
        public static Color RGBClamp01(Color color) => new(Mathf.Clamp01(color.r), Mathf.Clamp01(color.g), Mathf.Clamp01(color.b));
        public static Color Vector32RGB(this Vector3 vector) => new(vector.x, vector.y, vector.z);
        public static Vector3 RGB2Vector3(this Color color) => new(color.r, color.g, color.b);
        public static Vector3 HSL2Vector3(this HSLColor hSLColor) => new(hSLColor.hue, hSLColor.saturation, hSLColor.lightness);
        public static Vector3 RXHSl2Vector3(this RXColorHSL colorHSL) =>  new(colorHSL.h, colorHSL.s, colorHSL.l);
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
    }

}
