using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorConfig.WeakUITable;
using Menu;
using UnityEngine;

namespace ColorConfig.MenuUI.Objects
{
    public class SliderPages : PositionedMenuObject, ICopyPasteConfig
    {
        public const string PREVSINGAL = "_BackPageSliders", NEXTSINGAL = "_NextPageSliders";
        private int currentOffset, decimalCount;
        public bool showValues, showSign, copyPaste;
        public Slider[] sliderOOOs;
        public Vector2 setPrevButtonPos;
        public BigSimpleButton? prevButton, nextButton;
        public MidpointRounding roundingType;
        public bool ShouldCopyPaste => ModOptions.Instance.CopyPasteForSliders.Value && CopyPasteSlider != null;
        public string? Clipboard
        {
            get => ClipboardManager.Clipboard;
            set => ClipboardManager.Clipboard = value;
        }
        public string[] SliderVisualValues
        {
            get
            {
                List<string> list = [];
                for (int i = 0; i < sliderOOOs.Length; i++)
                {
                    Slider slider = sliderOOOs[i];
                    list.Add(GetVisualSliderValue((slider.floatValue) * CurrentGroup.SafeMultipler(i), CurrentGroup.SafeShowInt(i) ? 0 : decimalCount, showSign ? CurrentGroup.SafeSigns(i) : "", roundingType));
                }
                return [.. list];
            }
        }
        public bool PagesOn => OOOIDGroups.Count > 1;
        public Slider? CopyPasteSlider => sliderOOOs.FirstOrDefault(x => x.MouseOver == true || x.Selected); //shouldcopypaste should tell whether to copy/paste to this slider
        public int CurrentOffset { get => currentOffset; set => currentOffset = Mathf.Clamp(value, 0, OOOIDGroups.Count > 0 ? OOOIDGroups.Count - 1 : 0); }
        public int DecimalCount { get => decimalCount; set => decimalCount = Mathf.Clamp(value, 0, 15); }
        public List<SliderIDGroup> OOOIDGroups { get; protected set; }
        public SliderIDGroup CurrentGroup => OOOIDGroups.ValueOrDefault(CurrentOffset);
        public SliderPages(Menu.Menu menu, MenuObject owner, Slider[] sliders, List<SliderIDGroup> sliderOOOIDGroups, Vector2 buttonOffset = default, Vector2 pos = default) : base(menu, owner, pos == default ? Vector2.zero : pos)
        {
            if (sliders == null || sliders.Length == 0)
            {
                throw new ArgumentNullException(nameof(sliders));
            }
            showValues = true;
            showSign = true;
            copyPaste = ModOptions.Instance.CopyPasteForSliders.Value;
            decimalCount = 2;
            roundingType = MidpointRounding.AwayFromZero;
            sliderOOOs = sliders;
            OOOIDGroups = sliderOOOIDGroups.Count == 0 ? [new([.. sliderOOOs.Select(x => x?.ID)], [.. sliderOOOs.Select(x => x?.menuLabel).Select(x => x == null ? "" : x.text)])] : sliderOOOIDGroups;
            CurrentOffset = 0;
            buttonOffset = buttonOffset == default ? new(0, 0) : buttonOffset;
            setPrevButtonPos = new(buttonOffset.x + sliderOOOs.Last().pos.x, -(sliderOOOs.Last().size.y * 2) + buttonOffset.y + sliderOOOs.Last().pos.y);
            if (PagesOn)
                ActivateButtons();
        }
        public void LoadMethodToAllSliders(Action<Slider, SliderPages, int> action)
        {
            for (int i = 0; i < sliderOOOs?.Length; i++)
                action.Invoke(sliderOOOs[i], this, i);
        }
        public virtual void PopulatePage(int offset)
        {
            CurrentOffset = offset;
            for (int i = 0; i < sliderOOOs.Length; i++)
                sliderOOOs[i].ChangeSliderID(CurrentGroup.SafeID(i));
        }
        public virtual void NextPage()
        {
            menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            PopulatePage(SmallUtils.TryGoToNextPage(OOOIDGroups.Count, CurrentOffset));
        }
        public virtual void PrevPage()
        {
            menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            PopulatePage(SmallUtils.TryGoToPrevPage(OOOIDGroups.Count, CurrentOffset));
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
        public virtual string GetVisualSliderValue(int o)
        {
            Slider? slider = sliderOOOs.ValueOrDefault(o, default);
            return slider != null ? GetVisualSliderValue(slider.floatValue * CurrentGroup.SafeMultipler(o), CurrentGroup.SafeShowInt(o) ? 0 : decimalCount, showSign ? CurrentGroup.SafeSigns(o) : "", roundingType) : "";
        }
        public virtual string Copy()
        {
            Slider? slider = CopyPasteSlider;
            if (slider == null) return 0.ToString();
            float copyValue = slider.floatValue;
            copyValue = ChangeValueBasedOnMultipler(copyValue, CurrentGroup.SafeMultipler(sliderOOOs.IndexOf(slider)));
            menu.PlaySound(SoundID.MENU_Player_Join_Game);
            ColorConfigMod.DebugLog($"Copied.. {copyValue}");
            return copyValue.ToString();

        }
        public virtual void Paste(string? clipboard)
        {
            Slider? slider = CopyPasteSlider;
            if (slider == null) return;
            if (clipboard != null && float.TryParse(clipboard, NumberStyles.Integer | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out float newValue))
            {
                ColorConfigMod.DebugLog($"Got.. {newValue}");
                newValue = ChangeValueBasedOnMultipler(newValue, CurrentGroup.SafeMultipler(sliderOOOs.IndexOf(slider)), true);
                ColorConfigMod.DebugLog($"Slider value parse.. {newValue}");
                if (slider.floatValue != newValue)
                {
                    slider.floatValue = newValue;
                    menu.PlaySound(SoundID.MENU_Switch_Page_In);
                    return;
                }
            }
            menu.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
        }
        public virtual void GetMenuInput()
        {
            if (!menu.manager.menuesMouseMode && PagesOn && (prevButton?.Selected == true || nextButton?.Selected == true || sliderOOOs.Any(x => x.Selected)))
            {
                InputExtras inputExtras = menu.GetInputExtras();
                //use extraInput since normal ui input is stingy af and doesnt check for grab, spec in UiInput
                if (inputExtras.fixedInput.mp && !inputExtras.lastFixedInput.mp)
                    PrevPage();
                else if (inputExtras.fixedInput.pckp && !inputExtras.lastFixedInput.pckp)
                    NextPage();
            }

        }
        public override void Update()
        {
            base.Update();
            LoadMethodToAllSliders((slider, self, i) => UpdateSliderMenuLabelText(slider, CurrentGroup.SafeNames(i), GetVisualSliderValue(i), showValues));
            GetMenuInput();
        }
        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == PREVSINGAL)
                PrevPage();
            if (message == NEXTSINGAL)
                NextPage();
        }
        public static void UpdateSliderMenuLabelText(Slider slider, string name, string visualValue, bool showVisuals)
        {
            if (slider?.menuLabel != null && slider.menu != null && (!showVisuals && slider.menuLabel.text != slider.menu.Translate(name) || showVisuals))
                slider.menuLabel.text = slider.menu.Translate(name) + (showVisuals ? $" {visualValue}" : "");
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

    }
}
