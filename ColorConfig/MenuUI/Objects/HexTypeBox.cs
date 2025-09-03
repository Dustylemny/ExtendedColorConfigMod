using System;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;

namespace ColorConfig.MenuUI.Objects
{
    public class HexTypeBox : PositionedMenuObject, ICopyPasteConfig
    {
        public string lastValue;
        public bool shouldSaveNewTypedColor = false;
        public Color newPendingRGB;
        public Vector3 currentHSL, prevHSL, newPendingHSL;
        public MenuTabWrapper tabWrapper;
        public UIelementWrapper elementWrapper;
        public OpTextBox hexTyper;
        public event Action<HexTypeBox, Color>? OnClampHex = null;
        public event Action<HexTypeBox, Vector3, Color>? OnSaveNewTypedColor = null;
        public Action<HexTypeBox, Color> ClampHex => OnClampHex ?? ClampHSL;
        public string? Clipboard
        { get => ClipboardManager.Clipboard?.Trim(); set => ClipboardManager.Clipboard = value; }
        public bool ShouldCopyPaste => hexTyper.MouseOver || hexTyper.Focused;
        public HexTypeBox(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
        {
            tabWrapper = new(menu, this);
            lastValue = "";
            hexTyper = new(new Configurable<string>(""), Vector2.zero, 60)
            {
                maxLength = 6
            };
            elementWrapper = new(tabWrapper, hexTyper);
            subObjects.Add(tabWrapper);
        }
        public void SaveNewHSL(Vector3 hsl) => currentHSL = hsl;
        public void SaveNewRGB(Color rgb) => currentHSL = Custom.RGB2HSL(rgb);
        public string? Copy()
        {
            menu.PlaySound(SoundID.MENU_Player_Join_Game);
            return hexTyper.value;
        }
        public void Paste(string? clipboard)
        {
            if (clipboard == null || !SmallUtils.IfHexCodeValid(clipboard, out Color fromPaste) || hexTyper.value.IsHexCodesSame(clipboard))
            {
                menu.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
                return;
            }
            ClampHex.Invoke(this, fromPaste);
            shouldSaveNewTypedColor = true;
            lastValue = hexTyper.value;
            menu.PlaySound(SoundID.MENU_Switch_Page_In);
        }
        public override void Update()
        {
            base.Update();
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
                    ClampHex.Invoke(this, hexCol);
                    shouldSaveNewTypedColor = true;
                    lastValue = hexTyper.value;
                }
            }
            TrySave();
        }
        public void ClampHSL(HexTypeBox hexTypeBox, Color rgb)
        {
            hexTyper.value = ColorUtility.ToHtmlStringRGB(SmallUtils.RWIIIClamp(rgb.RGB2Vector3(), CustomColorModel.RGB, out Vector3 newClampedHSL).Vector32RGB());
            newClampedHSL = SmallUtils.FixNonHueSliderWonkiness(newClampedHSL, currentHSL);
            if (newClampedHSL == currentHSL) return;

            newPendingHSL = newClampedHSL;
            newPendingRGB = ColConversions.HSL2RGB(newPendingHSL);
            currentHSL = newPendingHSL;
            prevHSL = currentHSL;

        }
        public void TrySave()
        {
            if (!shouldSaveNewTypedColor || OnSaveNewTypedColor == null) return;
            shouldSaveNewTypedColor = false;
            OnSaveNewTypedColor.Invoke(this, newPendingHSL, newPendingRGB);

        }
    }
}
