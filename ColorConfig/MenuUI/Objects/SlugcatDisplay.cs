using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;

namespace ColorConfig.MenuUI.Objects
{
    public class SlugcatDisplay : RectangularMenuObject
    {
        //removed current and prev slugcat, assuming slugcat doesnt change midway while updating (meant for Story menu)
        public SlugcatDisplay(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, SlugcatStats.Name current) : base(menu, owner, pos, size)
        {
            bodyNames = PlayerGraphics.ColoredBodyPartList(current);
            LoadIcon(current, bodyNames);
        }
        public static Dictionary<string, string> LoadFileNames(SlugcatStats.Name name, List<string> bodyNames)
        {
            Dictionary<string, string> bodyPaths = [];
            foreach (string txtpath in SmallUtils.FindFilePaths("colorconfig", ".txt"))
            {
                string resolvedPath = AssetManager.ResolveFilePath(txtpath);
                if (File.Exists(resolvedPath))
                {
                    foreach (string line in File.ReadAllLines(resolvedPath, Encoding.UTF8))
                    {
                        if (line.StartsWith(name.value) && line.Split(':').ValueOrDefault(1, "").Contains('|'))
                        {
                            foreach (string body in line.Split(':')[1].Split(','))
                            {
                                string[] bodyLine = body.Split('|');
                                if (bodyNames.Contains(bodyLine[0]))
                                {
                                    ColorConfigMod.DebugLog("FileParser: " + bodyLine[1]);
                                    bodyPaths[bodyLine[0]] = bodyLine[1];
                                }

                            }
                        }
                    }
                }
            }
            return bodyPaths;
        }
        public static void ParseFromFileDictionary(Dictionary<string, string> dic, string bodyName, int i, SlugcatStats.Name name, out string folder, out string file)
        {
            folder = "";
            if (dic?.ContainsKey(bodyName) == true)
            {
                string path = dic[bodyName];
                file = path;
                if (path.Contains("/"))
                {
                    file = path.Split('/').Last();
                    folder = path.Replace("/" + file, string.Empty);
                }
                return;
            }
            file = i switch
            {
                0 => File.Exists(AssetManager.ResolveFilePath("illustrations/" + name.value + "_pup_off.png")) ? name.value + "_pup_off" : "pup_off",
                1 => File.Exists(AssetManager.ResolveFilePath("illustrations/" + "face_" + name.value + "_pup_off.png")) ? $"face_{name.value}_pup_off" : "face_pup_off",
                2 => File.Exists(AssetManager.ResolveFilePath($"illustrations/unique_{name.value}_pup_off.png")) ? $"unique_{name.value}_pup_off" : "colorconfig_showcasesquare",
                _ => File.Exists(AssetManager.ResolveFilePath($"illustrations/{bodyName}_{name.value}_pup_off.png")) ? $"{bodyName}_{name.value}_pup_off" : "colorconfig_showcasesquare",
            };

        }
        public void LoadSlugcatSprites(SlugcatStats.Name name, List<string> bodyNames)
        {
            List<MenuIllustration> illus = [];
            Dictionary<string, string> preSetFilesToLoad = LoadFileNames(name, bodyNames);
            for (int i = 0; i < bodyNames.Count; i++)
            {
                ParseFromFileDictionary(preSetFilesToLoad, bodyNames[i], i, name, out string folder, out string file);
                ColorConfigMod.DebugLog($"Slugcat Display loader.. BodyPart: {bodyNames[i]},Folder: {(folder == "" ? "Illustrations" : folder)}, File: {file}");
                MenuIllustration body = new(menu, this, folder, file, file == "colorconfig_showcasesquare" ? new(i * 10, -0.7f) : size / 2, true, true);
                subObjects.Add(body);
                illus.Add(body);
            }
            this.illus = [.. illus];
        }
        public void LoadIcon(SlugcatStats.Name current, List<string> bodyNames)
        {
            this.ClearMenuObjectList(illus);
            LoadSlugcatSprites(current, bodyNames);
        }
        public void LoadNewHSLSlugcat(List<Vector3> slugcatHSLColos/*, SlugcatStats.Name name*/) => currentRGBs = [.. slugcatHSLColos.Select(ColConversions.HSL2RGB)];
        public void LoadNewHSLStringSlugcat(List<string> slugcatHSLColos/*, SlugcatStats.Name name*/) => currentRGBs = [.. slugcatHSLColos.Select(x => ColConversions.HSL2RGB(SmallUtils.ParseHSLString(x)))];
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            for (int i = 0; i < illus.Length; i++)
                illus[i].color = currentRGBs.ValueOrDefault(i, Color.white);
        }

        //no need to find for currentslugcat and prev if unchanged in slugcat select menu
        //public SlugcatStats.Name currentSlugcat, prevSlugcat;
        public List<Color> currentRGBs = [];
        public List<string> bodyNames;
        public MenuIllustration[] illus = [];
    }
}
