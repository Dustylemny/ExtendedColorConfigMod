using Menu;

namespace ColorConfig.MenuUI
{
    public interface ICopyPasteConfig
    {
        bool ShouldCopyPaste { get; }
        string? Clipboard { get; set; }
        string? Copy();
        void Paste(string? clipboard);

    }
    public struct SliderIDGroup(Slider.SliderID?[] sliderIDs, string[]? names = null, bool[]? showInts = null, float[]? multipler = null, string[]? signs = null)
    {
        public readonly float SafeMultipler(int index) => multipler.ValueOrDefault(index, 1);
        public readonly bool SafeShowInt(int index) => showInts.ValueOrDefault(index, false);
        public readonly string SafeNames(int index) => names.ValueOrDefault(index, "")!;
        public readonly string SafeSigns(int index) => signs.ValueOrDefault(index, "")!;
        public readonly Slider.SliderID SafeID(int index) => sliderIDs.ValueOrDefault(index, new("DUSTY_REFISNULL", false))!;

        public float[]? multipler = multipler;
        public bool[]? showInts = showInts;
        public string[]? names = names, signs = signs;
        public Slider.SliderID?[] sliderIDs = sliderIDs;
    }
    public struct CopyPasteInput(bool cpy = false, bool paste = false)
    {
        public bool cpy = cpy, pste = paste;
    }
}
