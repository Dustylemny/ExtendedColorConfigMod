using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorConfig.MenuUI;
using Menu;
using Menu.Remix;
namespace ColorConfig.Hooks
{
    public static partial class ColorConfigHooks
    {
        public static void Menu_Hooks()
        {
            try
            {
                On.Menu.Menu.Update += On_Menu_Update;
                On.Menu.MenuObject.Update += On_MenuObject_Update;
                ColorConfigMod.DebugLog("Successfully initialized Menuobject hooks");
            }
            catch (Exception ex)
            {
                ColorConfigMod.DebugException("Failed to initialize Menuobject hooks", ex);
            }
        }
        public static void On_Menu_Update(On.Menu.Menu.orig_Update orig, Menu.Menu self)
        {
            self.GetInputExtras().UpdateInputs();
            orig(self);
        }
        public static void On_MenuObject_Update(On.Menu.MenuObject.orig_Update orig, MenuObject self)
        {
            orig(self);
            //need to check owner if null for page cuz rw was stupid to not put a fucking null check
            bool isSelectable = self.menu?.FreezeMenuFunctions == false && (self.owner != null && self.page == self.menu.pages.ValueOrDefault(self.menu.currentPage));
            if (self is ICopyPasteConfig cpyPstConfig && cpyPstConfig.ShouldCopyPaste && isSelectable)
            {
                if (self.menu.CopyShortcutPressed())
                    cpyPstConfig.Clipboard = cpyPstConfig.Copy();
                if (self.menu.PasteShortcutPressed())
                    cpyPstConfig.Paste(cpyPstConfig.Clipboard);
            }
        }
    }
}
