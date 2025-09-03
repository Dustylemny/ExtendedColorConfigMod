using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using UnityEngine;

namespace ColorConfig.MenuUI.Objects
{
    public class HexTypeBoxPages : PositionedMenuObject
    {
        public const string PREVSINGAL = "PREVPAGE_HEXBOXES_DUSTY", NEXTSINGAL = "NEXTPAGE_HEXBOXES_DUSTY";
        public int perPage, currentOffset;
        public List<string> names;
        public Dictionary<HexTypeBox, int> hexBoxes = [];
        public SymbolButton? prevBtn, nextBtn;
        public Action<HexTypeBox, Vector3, Color, int>? applyChanges;
        public int CurrentOffset { get => currentOffset; set => currentOffset = Mathf.Clamp(value, 0, names?.Count > 0 ? (names.Count - 1) / perPage : 0); }
        public bool PagesOn => names.Count > perPage;
        public HexTypeBoxPages(Menu.Menu menu, MenuObject owner, Vector2 pos, List<string> names, int perPage = 2) : base(menu, owner, pos)
        {
            this.perPage = perPage;
            this.names = names;
            if (PagesOn)
                ActivateButtons();
            PopulatePage(CurrentOffset);
        }
        public void SaveNewHSLs(IList<Vector3> hsls)
        {
            foreach (KeyValuePair<HexTypeBox, int> pair in hexBoxes)
                pair.Key.SaveNewHSL(hsls.ValueOrDefault(pair.Value, Vector3.one));
        }
        public void NextPage()
        {

            menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            PopulatePage(SmallUtils.TryGoToNextPage(names.Count, CurrentOffset, perPage));
        }
        public void PrevPage()
        {
            menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            PopulatePage(SmallUtils.TryGoToPrevPage(names.Count, CurrentOffset, perPage));
        }
        public void PopulatePage(int offset)
        {
            this.ClearMenuObjectList([.. hexBoxes.Keys]);
            hexBoxes.Clear();
            CurrentOffset = offset;
            MenuObject? toBindWith = null;
            SmallUtils.HelpPopulatePage(names.Count, CurrentOffset, perPage, (num) =>
            {
                ColorConfigMod.DebugLog(num);
                HexTypeBox hexTypeBox = GetHexTypeBox(num);
                hexBoxes.Add(hexTypeBox, num);
                subObjects.Add(hexTypeBox);
                menu.MutualMenuObjectBind(toBindWith, hexTypeBox.elementWrapper, true);

                toBindWith = hexTypeBox.elementWrapper;
            });
            UpdateButtonSelectable();
        }
        public HexTypeBox GetHexTypeBox(int num)
        {
            HexTypeBox hexTypeBox = new(menu, this, new(70 * (num % perPage) + (PagesOn ? 34 : 0), 0));
            hexTypeBox.OnSaveNewTypedColor += (hexTyper, hsl, rgb) =>
            {
                if (hexBoxes?.ContainsKey(hexTyper) == true)
                    applyChanges?.Invoke(hexTyper, hsl, rgb, hexBoxes[hexTyper]);
            };
            hexTypeBox.hexTyper.description = menu.Translate("Type in hex code for <BODYNAME>").Replace("<BODYNAME>", menu.Translate(names[num]));
            return hexTypeBox;
        }
        public void ActivateButtons()
        {
            if (prevBtn == null)
                subObjects.Add(prevBtn = new(menu, this, "Menu_Symbol_Arrow", PREVSINGAL, Vector2.zero));
            if (nextBtn == null)
                subObjects.Add(nextBtn = new(menu, this, "Menu_Symbol_Arrow", NEXTSINGAL, new(perPage * 70, 0)));
            prevBtn.symbolSprite.rotation = 270;
            nextBtn.symbolSprite.rotation = 90;
            if (hexBoxes.Count == 0) return;
            UpdateButtonSelectable();
        }
        public void DeactivateButtons()
        {
            this.ClearMenuObject(ref prevBtn);
            this.ClearMenuObject(ref nextBtn);
        }
        public void UpdateButtonSelectable()
        {
            if (hexBoxes.Count == 0)
            {
                menu.MutualMenuObjectBind(prevBtn, nextBtn, true);
                return;
            }
            menu.MutualMenuObjectBind(prevBtn, hexBoxes.Keys.First(), true);
            menu.MutualMenuObjectBind(hexBoxes.Keys.Last().elementWrapper, nextBtn, true);

        }
        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == PREVSINGAL)
                PrevPage();
            if (message == NEXTSINGAL)
                NextPage();
        }

    }
}
