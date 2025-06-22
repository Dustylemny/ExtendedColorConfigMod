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

namespace ColorConfig
{
    public static class Manager
    {
        //Clipboard
        public static string? Clipboard
        {
            get => GUIUtility.systemCopyBuffer; set => GUIUtility.systemCopyBuffer = value;
        }
    }
    public static class SmallUtils
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
        public static T? ValueOrDefault<T>(this IList<T>? list, int index, T defaultValue) => (list != null && list.Count > index && index >= 0) ? list[index] : defaultValue;
        public static T? ValueOrDefault<T>(this IList<T>? list, int index) => ValueOrDefault(list!, index, default);

        //Fixed Backtrack issues
        public static string[] FindFilePaths(string directoryName, string fileFormat = "", bool directories = false, bool includeAll = false) => [.. AssetManager.ListDirectory(directoryName, directories, includeAll).Where(x => x.EndsWith(fileFormat))];

        //For inputs
        public static ExtraFixedMenuInput GetFixedExtraMenuInput()
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
        public static void UpdateExtraInterfaces(this object? obj, object? inputHolder)
        {
            (obj as IGetOwnInput)?.GetOwnInput(inputHolder?.GetInputExtras()?.pMInput ?? new(), inputHolder?.GetInputExtras()?.lastPMInput ?? new());
            (obj as ICopyPasteConfig)?.CopyPaste(inputHolder);
        }
        public static InputExtras GetInputExtras(this object? obj) => obj != null ?ColorConfigMod.inputExtras.GetValue(obj, _ => new()) : new();
        public static void GetOwnInput(this IGetOwnInput non_MouseInput, Player.InputPackage input, Player.InputPackage lastInput) => non_MouseInput?.TryGetInput(input, lastInput);
        public static void CopyPaste(this ICopyPasteConfig copyPasteConfig, object? inputHolder)
        {
            if (copyPasteConfig != null && copyPasteConfig.ShouldCopyPaste)
            {
                if (CopyShortcutPressed(inputHolder))
                    copyPasteConfig.Clipboard = copyPasteConfig.Copy();
                else if (PasteShortcutPressed(inputHolder))
                    copyPasteConfig.Paste(copyPasteConfig.Clipboard);
            }
        }
        public static void ApplyPerPageList<T>(this IDoPerPage perPage, IList<T> list, Action<T, int> action)
        {
            if (perPage == null) return;
            int num = perPage.CurrentOffset * perPage.PerPage;
            while (num < list?.Count && num < (perPage.CurrentOffset + 1) * perPage.PerPage)
            {
                action?.Invoke(list[num], num);
                num++;
            }
        }
        public static int NextPageLoopListPerPage<T>(this IDoPerPage perPage, IList<T> list) => (list == null || list.Count == 0 || perPage.CurrentOffset + 1 > (list.Count - 1) / perPage.PerPage) ? 0 : perPage.CurrentOffset + 1;
        public static int PrevPageLoopListPerPage<T>(this IDoPerPage perPage, IList<T> list) => (perPage.CurrentOffset - 1 < 0 ? list?.Count > 0 ? (list.Count - 1) / perPage.PerPage : 0 : perPage.CurrentOffset - 1);
        public static int NextPageLoopList<T>(this ICanTurnPages turnPages, IList<T> list) => list == null || list.Count == 0 || turnPages.CurrentOffset + 1 >= list.Count ? 0 : turnPages.CurrentOffset + 1;
        public static int PrevPageLoopList<T>(this ICanTurnPages turnPages, IList<T> list) => turnPages.CurrentOffset - 1 < 0 ? list?.Count > 0 ? list.Count - 1 : 0 : turnPages.CurrentOffset - 1;

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
            if (menu?.manager?.rainWorld?.progression?.miscProgressionData?.colorChoices == null) return;
            List<string> slugcats = new(menu.manager.rainWorld.progression.miscProgressionData.colorChoices.Keys);
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
            if (ccI?.bodyColors != null && bodyIndex <= ccI.bodyColors.Length) ccI.bodyColors[bodyIndex].color = HSL2RGB(ccI.menu.MenuHSL(ccI.slugcatID, bodyIndex));
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
            {
                IDGroups.Add(new([MMFEnums.SliderID.Hue, MMFEnums.SliderID.Saturation, MMFEnums.SliderID.Lightness], MenuToolObj.HSLNames,
                    MenuToolObj.HueOOShowInt, MenuToolObj.hueOOMultipler, MenuToolObj.HueOOSigns));
            }
            if (ModOptions.Instance.EnableRGBSliders.Value)
            {
                IDGroups.Add(new([MenuToolObj.RedRGB, MenuToolObj.GreenRGB, MenuToolObj.BlueRGB],
                    MenuToolObj.RGBNames, MenuToolObj.RGBShowInt, MenuToolObj.rgbMultipler));
            }
            if (ModOptions.Instance.EnableHSVSliders.Value)
            {
                IDGroups.Add(new([MenuToolObj.HueHSV, MenuToolObj.SatHSV, MenuToolObj.ValHSV],
                   MenuToolObj.HSVNames, MenuToolObj.HueOOShowInt, MenuToolObj.hueOOMultipler, MenuToolObj.HueOOSigns));
            }
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
        public static void AddEXPSliderIDGroups(List<SliderIDGroup> IDGroups, bool shouldRemoveHSL) //remix is required
        {
            if (!shouldRemoveHSL)
            {
                IDGroups.Add(new([MMFEnums.SliderID.Hue, MMFEnums.SliderID.Saturation, MMFEnums.SliderID.Lightness], MenuToolObj.HSLNames,
                    MenuToolObj.HueOOShowInt, MenuToolObj.hueOOMultipler, MenuToolObj.HueOOSigns));
            }
            AddSSMSliderIDGroups(IDGroups, true);
        }
        public static void TrySaveExpeditionColorsToCustomColors(this Menu.Menu menu, SlugcatStats.Name name) => PlayerGraphics.customColors = !ModManager.JollyCoop && menu.IsCustomColorEnabled(name) && ModOptions.Instance.EnableExpeditionColorConfig.Value ? [.. menu.MenuHSLs(name).Select(ColConversions.HSL2RGB)] : null;

        //opcolor picker stuff
        public static ColorPickerExtras GetColorPickerExtras(this OpColorPicker cPicker) => ColorConfigMod.extraColorPickerStuff.GetValue(cPicker, (_) => new(cPicker));
        public static bool IsHSVMode(this OpColorPicker cPicker) => cPicker.GetColorPickerExtras()._IsHSVMode;
        public static bool IsDiffHSLHSVMode(this OpColorPicker cPicker) => cPicker.GetColorPickerExtras().IsDifferentHSVHSLMode;
        public static void SwitchHSLCustomMode(this OpColorPicker self)
        {
            //switch hsl, hsl_diff, hsv, hsv_diff if all except betterColorPickers. else hsl, hsv_diff. hsl, hsv if only HSV mode. hsl, hsl_diff if only diff
            // so if diff is on, it just switches, hsv switches if diff is off or _diff is on
            // allow change if ex: diff is off but _diff is still on
            self.GetColorPickerExtras().IsHSVMode = (!ModOptions.PickerHSVMode && !self.GetColorPickerExtras().IsHSVMode) || (!ModOptions.Instance.EnableBetterOPColorPicker.Value && ModOptions.Instance.EnableDiffOpColorPickerHSL.Value && !self.GetColorPickerExtras().IsDifferentHSVHSLMode) ? self.GetColorPickerExtras().IsHSVMode : !self.GetColorPickerExtras().IsHSVMode;
            self.GetColorPickerExtras().IsDifferentHSVHSLMode = !ModOptions.Instance.EnableDiffOpColorPickerHSL.Value && !self.GetColorPickerExtras().IsDifferentHSVHSLMode ? self.GetColorPickerExtras().IsDifferentHSVHSLMode : !self.GetColorPickerExtras().IsDifferentHSVHSLMode;
            self.PlaySound(SoundID.MENU_Player_Join_Game);
        }
        public static bool OpColorPickerPatchMiniFocusHSLColor(this OpColorPicker self, int hueSat, int satLit, int litHue, bool squareTexture)
        {
            Vector3Int hsvHSL = self.GetHSVOrHSL100();
            int h = hsvHSL.x, s = hsvHSL.y, l = hsvHSL.z;
            if (self.IsDiffHSLHSVMode()) //HueSat becomes SatLit and Lit becomes Hue
            {
                hueSat = squareTexture ? Mathf.RoundToInt(Mathf.Clamp(self.MousePos.x - 10f, 0f, 100f)) : hueSat;
                litHue = litHue > 99 ? 99 : litHue;
                h = squareTexture ? h : litHue;
                s = squareTexture ? hueSat : s;
                l = squareTexture ? satLit : l;
                self._lblR.text = h.ToString();
                self._lblG.text = s.ToString();
                self._lblB.text = l.ToString();
                self._cdis1.color = self.ColorPicker2RGB().Invoke(new(h / 100f, s / 100f, l/ 100f));
                self.SetHSLORHSV100(h, s, l);
                return true;
            }
            else if (self.IsHSVMode())
            {
                h = squareTexture ? hueSat : h;
                s = squareTexture ? satLit : s;
                l = squareTexture ? l : litHue;
                self._cdis1.color = self.ColorPicker2RGB().Invoke(new(h / 100f, s / 100f, l/ 100f));
            }
            return false;
        }
        public static void OpColorPickerPatchHoverMouseHSLColor(this OpColorPicker self, int hueSat, int satLit, int litHue, bool squareTexture)
        {
            Vector3Int hsvHSL = self.GetHSVOrHSL100();
            int h = hsvHSL.x, s = hsvHSL.y, l = hsvHSL.z;
            if (self.IsDiffHSLHSVMode()) //When mouse just hovers over texture
            {
                hueSat = squareTexture ? Mathf.RoundToInt(Mathf.Clamp(self.MousePos.x - 10f, 0f, 100f)) : hueSat;
                litHue = litHue > 99 ? 99 : litHue;
                h = squareTexture ? h : litHue;
                s = squareTexture ? hueSat : s;
                l = squareTexture ? satLit : l;
                self._lblR.text = h.ToString();
                self._lblG.text = s.ToString();
                self._lblB.text = l.ToString();
                self._cdis1.color = self.ColorPicker2RGB().Invoke(new(h / 100f, s / 100f, l / 100f));
            }
            else if (self.IsHSVMode())
            {
                h = squareTexture ? hueSat : h;
                s = squareTexture ? satLit : s;
                l = squareTexture ? l : litHue;
                self._cdis1.color = self.ColorPicker2RGB().Invoke(new(h / 100f, s / 100f, l / 100f));
            }
        }
        public static Func<Vector3, Color> ColorPicker2RGB(this OpColorPicker cPicker) => cPicker.IsHSVMode() ? ColConversions.HSV2RGB : ColConversions.HSL2RGB;
        public static void RefreshTexture(this OpColorPicker cPicker)
        {
            if (cPicker != null)
            {
                cPicker._RecalculateTexture();
                if (cPicker._mode == OpColorPicker.PickerMode.RGB)
                {
                    cPicker._ftxr1.SetTexture(cPicker._ttre1);
                    cPicker._ftxr2.SetTexture(cPicker._ttre2);
                    cPicker._ftxr3.SetTexture(cPicker._ttre3);
                }
                else if (cPicker._mode == OpColorPicker.PickerMode.HSL)
                {
                    cPicker._ftxr1.SetTexture(cPicker._ttre1);
                    cPicker._ftxr2.SetTexture(cPicker._ttre2);
                }
                else
                {
                    cPicker._ftxr2.SetPosition(OpColorPicker._GetPICenterPos(cPicker._pi));
                }
            }
        }
        public static Vector3Int GetHSL(this OpColorPicker cPicker)
        {
            return new(cPicker._h, cPicker._s, cPicker._l);
        }
        public static Vector3 GetHSL01(this OpColorPicker cPicker) => new(cPicker._h / 100f, cPicker._s / 100f, cPicker._l / 100f);
        public static Vector3Int GetHSVOrHSL100(this OpColorPicker cPicker) => cPicker.GetHSL();
        public static Vector3 GetHSVOrHSL01(this OpColorPicker cPicker) => cPicker.GetHSL01();
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
        public static void SetHSLORHSV100(this OpColorPicker cPicker, float h, float s, float l)
        {
            Vector3 hsvHSL100 = cPicker.GetHSVOrHSL100();
            if (h != hsvHSL100.x || s != hsvHSL100.y || l != hsvHSL100.z)
            {
                cPicker.SetDirectHSLORHSV100(Mathf.RoundToInt(h), Mathf.RoundToInt(s), Mathf.RoundToInt(l));
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
                return mousePosition.x >= startRange.x && mousePosition.y >= startRange.y && mousePosition.x <= endRange.x && mousePosition.y <= endRange.y;
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
            if (cPicker == null) return;

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
        public static void PasteNumberCPicker(this OpColorPicker cPicker, int oOO)
        {
            if (cPicker == null) return;
            string? newValue = Manager.Clipboard?.Trim();
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
            if (cPicker == null) return;
            if (cPicker._mode == OpColorPicker.PickerMode.RGB) cPicker._RGBSetValue();
            if (cPicker._mode == OpColorPicker.PickerMode.HSL) cPicker._HSLSetValue();
            cPicker.PlaySound(SoundID.MENU_Player_Join_Game);

        }
        public static void CopyHexCPicker(this OpColorPicker cPicker)
        {
            if (cPicker == null) return;
            Manager.Clipboard = cPicker.value;
            cPicker.PlaySound(SoundID.MENU_Player_Join_Game);

        }
        public static void PasteHexCPicker(this OpColorPicker cPicker)
        {
            if (cPicker == null) return;
            string? newValue = Manager.Clipboard?.Trim()?.TrimStart('#');
            if (newValue != null && MenuColorEffect.IsStringHexColor(newValue) && !newValue.IsHexCodesSame(cPicker.value))
            {
                cPicker.value = newValue.Substring(0, 6).ToUpper();
                cPicker.PlaySound(SoundID.MENU_Switch_Page_In);
                return;
            }
            cPicker.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);


        }
        public static void ApplyHSLArrow(Texture2D texture, int oOO, Color arrowColor, bool moveUpDown, int positioner = 0)
        {
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

        //copypaste
        public static bool CopyShortcutPressed(this object? inputHolder) => inputHolder != null && inputHolder.GetInputExtras().femInput.cpy && !inputHolder.GetInputExtras().lastFemInput.cpy;
        public static bool PasteShortcutPressed(this object? inputHolder) => inputHolder != null && inputHolder.GetInputExtras().femInput.pste && !inputHolder.GetInputExtras().lastFemInput.pste;


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
    }
    public static class ColConversions
    {
        public static Vector3 HSL1002HSV100(Vector3 hsl)
        {
            float val = hsl.z + hsl.y * (Mathf.Min(hsl.z, 100 - hsl.z) / 100);
            float sat = val == 0 ? 0 : 200 * (1 - (hsl.z / val));
            return new(hsl.x, sat, val);
        }
        public static Vector3 HSV1002HSL100(Vector3 hsv)
        {
            float lit = hsv.z * (1 - (hsv.y / 200));
            float sat = lit == 0 || lit == 100 ? 0 : (hsv.z - lit) / (Mathf.Min(lit, 100 - lit) / 100);
            return new(hsv.x, sat, lit);
        }
        public static Color HSV2RGB(Vector3 hsv) => Color.HSVToRGB(hsv.x == 1 ? 0 : hsv.x, hsv.y, hsv.z);
        public static Color HSL2RGB(Vector3 hsl) => Custom.HSL2RGB(hsl.x == 1 ? 0 : hsl.x, hsl.y, hsl.z);
        public static Vector3 HSV2HSL(Vector3 hsv)
        {
            float lit = hsv.z * (1 - (hsv.y / 2));
            float sat = lit == 0 || lit == 1 ? 0 : (hsv.z - lit) / (Mathf.Min(lit, 1 - lit));
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
            float sat = val == 0 ? 0 : 2 * 1 * (1 - (hsl.z / val));
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
