using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ColorConfig.ColorPresets
{
    [Serializable]
    public class ColorPreset
    {
        public ColorPreset() { }
        public ColorPreset(string presetName) 
        {
            this.presetName = presetName;
        }
        public string presetName = "";
        public Dictionary<string, List<ColorSlot>> colorSlots = [];
    }
}
