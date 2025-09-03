using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Menu;
using UnityEngine;

namespace ColorConfig.MenuUI.Objects
{
    public class ExpeditionColorInterface : PositionedMenuObject
    {
        public const string OPENINTERFACESINGAL = "DUSTYEXPEDITIONCUSTOMCOLOR", PREVSINGAL = "PrevPageColors_DUSTYEXPEDITIONCUSTOMCOLOR", NEXTSINGAL = "NextPageColors_DUSTYEXPEDITIONCUSTOMCOLOR";
        public int perPage, currentOffset;
        public SlugcatDisplay? slugcatDisplay;
        public SymbolButton? prevBtn, nextBtn;
        public SimpleButton[] bodyButtons = [];
        public RoundedRect[] bodyColorBorders = [];
        public SlugcatStats.Name slugcatID;
        public Dictionary<MenuIllustration, int> bodyColors = [];
        public List<string> defaultColors, bodyNames;
        public int CurrentOffset { get => currentOffset; set => currentOffset = Mathf.Clamp(value, 0, bodyNames?.Count > 0 ? (bodyNames.Count - 1) / perPage : 0); }
        public bool PagesOn => bodyNames?.Count > perPage;
        public ExpeditionColorInterface(Menu.Menu menu, MenuObject owner, Vector2 pos, SlugcatStats.Name slugcatID, List<string> names, List<string> defaultColors, bool showDisplay = false) : base(menu, owner, pos)
        {
            perPage = 3;
            currentOffset = 0;
            bodyNames = names;
            this.slugcatID = slugcatID;
            this.defaultColors = defaultColors;
            SafeSaveColor();
            if (showDisplay)
            {
                slugcatDisplay = new(menu, this, new(perPage * 80 + 80, -60), new(45, 45), slugcatID);
                subObjects.Add(slugcatDisplay);
            }
            PopulatePage(CurrentOffset);
            if (PagesOn)
                ActivateButtons();
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            slugcatDisplay?.LoadNewHSLStringSlugcat(menu.manager.rainWorld.progression.miscProgressionData.colorChoices[slugcatID.value]);
            foreach (KeyValuePair<MenuIllustration, int> illu in bodyColors)
                illu.Key.color = ColConversions.HSL2RGB(menu.MenuHSL(slugcatID, illu.Value));
        }
        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == PREVSINGAL)
                PrevPage();
            if (message == NEXTSINGAL)
                NextPage();
        }
        public void PrevPage()
        {
            menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            PopulatePage(SmallUtils.TryGoToPrevPage(bodyNames.Count, CurrentOffset, perPage));
        }
        public void NextPage()
        {
            menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            PopulatePage(SmallUtils.TryGoToNextPage(bodyNames.Count, CurrentOffset, perPage));
        }
        public void PopulatePage(int offset)
        {
            RefreshInterface();
            CurrentOffset = offset;
            SmallUtils.HelpPopulatePage(bodyNames.Count, CurrentOffset, perPage, (i) =>
            {
                string name = bodyNames[i];
                SimpleButton bodyButton = new(menu, this, menu.Translate(name), OPENINTERFACESINGAL + i.ToString(CultureInfo.InvariantCulture), new Vector2(i % perPage * 90, -50), new(80, 30));
                RoundedRect bodyColorBorder = new(menu, this, new(bodyButton.pos.x + bodyButton.size.x / 4, bodyButton.pos.y + 50), new(40, 40), false);
                MenuIllustration bodyColor = new(menu, this, "", "square", bodyColorBorder.pos + new Vector2(2, 2), false, false);
                subObjects.AddRange([bodyButton, bodyColorBorder, bodyColor]);
                menu.MutualMenuObjectBind(bodyButtons.ValueOrDefault((i - 1) % perPage), bodyButton, true);
                bodyButtons.AddToArray(bodyButton);
                bodyColorBorders.AddToArray(bodyColorBorder);
                bodyColors.Add(bodyColor, i);
            });
            UpdateButtonSelectable();
            (menu as ExpeditionColorDialog)?.RemoveColorInterface();
        }
        public void SafeSaveColor()
        {
            if (!menu.manager.rainWorld.progression.miscProgressionData.colorChoices.ContainsKey(slugcatID.value))
                menu.manager.rainWorld.progression.miscProgressionData.colorChoices.Add(slugcatID.value, []);
            menu.manager.rainWorld.progression.miscProgressionData.colorChoices[slugcatID.value] ??= [];
            while (menu.manager.rainWorld.progression.miscProgressionData.colorChoices[slugcatID.value].Count < bodyNames?.Count)
                menu.manager.rainWorld.progression.miscProgressionData.colorChoices[slugcatID.value].Add(defaultColors[menu.manager.rainWorld.progression.miscProgressionData.colorChoices[slugcatID.value].Count]);
        }
        public void RefreshInterface()
        {
            this.ClearMenuObjectList(bodyButtons);
            this.ClearMenuObjectList(bodyColorBorders);
            this.ClearMenuObjectList([.. bodyColors.Keys]);
            bodyButtons = [];
            bodyColorBorders = [];
            bodyColors = [];

        }
        public void ActivateButtons()
        {
            if (prevBtn == null)
                subObjects.Add(prevBtn = new(menu, this, "Menu_Symbol_Arrow", PREVSINGAL, new(-34, -50)));
            if (nextBtn == null)
                subObjects.Add(nextBtn = new(menu, this, "Menu_Symbol_Arrow", NEXTSINGAL, new(20 + perPage * 80, 0)));
            prevBtn.symbolSprite.rotation = 270;
            nextBtn.symbolSprite.rotation = 90;
            UpdateButtonSelectable();
        }
        public void DeactivateButtons()
        {
            this.ClearMenuObject(ref prevBtn);
            this.ClearMenuObject(ref nextBtn);
        }
        public void UpdateButtonSelectable()
        {
            if (bodyButtons.Length == 0)
            {
                menu.MutualMenuObjectBind(prevBtn, nextBtn, true);
                return;
            }
            menu.MutualMenuObjectBind(prevBtn, bodyButtons.First(), true);
            menu.MutualMenuObjectBind(bodyButtons.Last(), nextBtn, true);
        }


    }
}
