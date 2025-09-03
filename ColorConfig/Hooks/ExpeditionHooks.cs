using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using ColorConfig.MenuUI;

namespace ColorConfig.Hooks
{
    public static partial class ColorConfigHooks
    {
        public static void ExpeditionMenu_Hooks()
        {
            try
            {
                On.Menu.ExpeditionMenu.Singal += On_ExpeditionMenu_Singal;
                On.Menu.CharacterSelectPage.ctor += On_CharacterSelectPage_Ctor;
                On.Menu.CharacterSelectPage.Singal += On_CharacterSelectPage_Singal;
                On.Menu.CharacterSelectPage.SetUpSelectables += On_CharacterSelectPage_SetUpSelectables;
                On.Menu.CharacterSelectPage.UpdateChallengePreview += On_CharacterSelectPage_UpdateChallengePreview;
                On.Menu.CharacterSelectPage.RemoveSprites += On_CharacterSelectPage_RemoveSprites;
                On.Menu.CharacterSelectPage.LoadGame += On_CharacterSelectPage_LoadGame;
                On.Menu.ChallengeSelectPage.StartGame += On_ChallengeSelectPage_StartGame;
                ColorConfigMod.DebugLog("Sucessfully extended color interface for expedition mode!");
            }
            catch (Exception ex)
            {
                ColorConfigMod.DebugException("Failed to extend color config for expedition", ex);
            }

        }
        public static void On_ExpeditionMenu_Singal(On.Menu.ExpeditionMenu.orig_Singal orig, ExpeditionMenu self, MenuObject sender, string message)
        {
            if (ColorConfigMod.IsBingoOn)
                BingoModUtils.TryApplyBingoColors(self, message);
            orig(self, sender, message);
        }
        public static void On_CharacterSelectPage_Ctor(On.Menu.CharacterSelectPage.orig_ctor orig, CharacterSelectPage self, Menu.Menu menu, MenuObject owner, Vector2 pos)
        {
            orig(self, menu, owner, pos);
            SymbolButton? colorConfig = self.GetExtraEXPInterface().colorConfig;
            if (!ModManager.JollyCoop && ModManager.MMF && colorConfig == null && ModOptions.Instance.EnableExpeditionColorConfig.Value)
            {
                colorConfig = new(menu, self, "colorconfig_slugcat_noncoloured", "DUSTY_EXPEDITION_CONFIG", new(440 + (self.jollyToggleConfigMenu?.pos == new Vector2(440, 550) ? -self.jollyToggleConfigMenu.size.x - 10 : 0), 550));
                colorConfig.roundedRect.size = new(50, 50);
                colorConfig.size = colorConfig.roundedRect.size;
                self.subObjects.Add(colorConfig);
                self.GetExtraEXPInterface().colorConfig = colorConfig;

            }
        }
        public static void On_CharacterSelectPage_SetUpSelectables(On.Menu.CharacterSelectPage.orig_SetUpSelectables orig, CharacterSelectPage self)
        {
            orig(self);
            SymbolButton? colorConfig = self.GetExtraEXPInterface().colorConfig;
            if (colorConfig != null)
            {
                colorConfig.MenuObjectBind((self.menu as ExpeditionMenu)?.muteButton, left: true, top: true);
                colorConfig.MenuObjectBind(self.jollyToggleConfigMenu != null ? self.jollyToggleConfigMenu : self.slugcatButtons.ValueOrDefault(0), true);
                colorConfig.MenuObjectBind(self.slugcatButtons.Length > 3 ? self.slugcatButtons[3] : self.confirmExpedition, bottom: true);
            }
        }
        public static void On_CharacterSelectPage_Singal(On.Menu.CharacterSelectPage.orig_Singal orig, CharacterSelectPage self, MenuObject sender, string message)
        {
            orig(self, sender, message);
            if (self.menu is ExpeditionMenu { pagesMoving: false } && message == "DUSTY_EXPEDITION_CONFIG")
            {
                self.menu.PlaySound(SoundID.MENU_Player_Join_Game);
                self.menu.manager.ShowDialog(new ExpeditionColorDialog(self.menu, SmallUtils.ExpeditionSlugcat(), () =>
                {
                    self.GetExtraEXPInterface().colorConfig?.symbolSprite?.SetElementByName(GetColorEnabledSprite(self.menu));
                }, ModOptions.Instance.EnableHexCodeTypers.Value, showSlugcatDisplay: ModOptions.Instance.EnableSlugcatDisplay.Value));

            }

        }
        public static void On_CharacterSelectPage_UpdateChallengePreview(On.Menu.CharacterSelectPage.orig_UpdateChallengePreview orig, CharacterSelectPage self)
        {
            orig(self);
            SymbolButton? colorConfigBtn = self.GetExtraEXPInterface().colorConfig;
            if (colorConfigBtn != null)
                colorConfigBtn.symbolSprite.SetElementByName(GetColorEnabledSprite(self.menu));
        }
        public static void On_CharacterSelectPage_RemoveSprites(On.Menu.CharacterSelectPage.orig_RemoveSprites orig, CharacterSelectPage self)
        {
            orig(self);
            self.ClearMenuObject(ref self.GetExtraEXPInterface().colorConfig);
        }
        public static void On_CharacterSelectPage_LoadGame(On.Menu.CharacterSelectPage.orig_LoadGame orig, CharacterSelectPage self)
        {
            self.menu.TrySaveExpeditionColorsToCustomColors(SmallUtils.ExpeditionSlugcat());
            orig(self);
        }
        public static void On_ChallengeSelectPage_StartGame(On.Menu.ChallengeSelectPage.orig_StartGame orig, ChallengeSelectPage self)
        {
            self.menu.TrySaveExpeditionColorsToCustomColors(SmallUtils.ExpeditionSlugcat());
            orig(self);
        }
        public static string GetColorEnabledSprite(Menu.Menu menu) => menu.IsCustomColorEnabled(Expedition.ExpeditionData.slugcatPlayer) ? "colorconfig_slugcat_coloured" : "colorconfig_slugcat_noncoloured";
    }
}
