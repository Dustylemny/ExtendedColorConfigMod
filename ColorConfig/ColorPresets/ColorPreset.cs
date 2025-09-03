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
        public Dictionary<string, List<ColorSlot>> colorSlots = [];
    }
}
