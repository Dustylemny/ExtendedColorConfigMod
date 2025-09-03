using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ColorConfig.ColorPresets
{
    public static class ColorPresetManager
    {
        public static string SavePath => Application.persistentDataPath + Path.DirectorySeparatorChar.ToString() + "customColorPresets.dat";
        public static List<ColorPreset> colorPresets = [];
        public static void Load()
        {
            if (!Directory.Exists(SavePath))
                Save();
            using FileStream fileStream = new(SavePath, FileMode.Open);
            BinaryFormatter binaryFormatter = new();
            colorPresets = (List<ColorPreset>)binaryFormatter.Deserialize(fileStream);
        }
        public static void Save()
        {
            using FileStream fileStream = new(SavePath, FileMode.Create);
            BinaryFormatter binaryFormatter = new();
            binaryFormatter.Serialize(fileStream, colorPresets);
        }
    }
}
