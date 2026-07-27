using PdfSharp.Fonts;

namespace LaptopTracker.Helpers
{
    public class WindowsFontResolver : IFontResolver
    {
        private static readonly Dictionary<string, string> _faceToFile = new(StringComparer.OrdinalIgnoreCase)
        {
            ["SegoeUI-Regular"]    = "segoeui.ttf",
            ["SegoeUI-Bold"]       = "segoeuib.ttf",
            ["SegoeUI-Italic"]     = "segoeuii.ttf",
            ["SegoeUI-BoldItalic"] = "segoeuiz.ttf",
        };

        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            var norm = familyName.Replace(" ", "");
            var suffix = isBold && isItalic ? "BoldItalic"
                       : isBold             ? "Bold"
                       : isItalic           ? "Italic"
                       :                      "Regular";
            var key = $"{norm}-{suffix}";
            if (_faceToFile.ContainsKey(key))
                return new FontResolverInfo(key);
            var regularKey = $"{norm}-Regular";
            return _faceToFile.ContainsKey(regularKey) ? new FontResolverInfo(regularKey) : null;
        }

        public byte[]? GetFont(string faceName)
        {
            if (_faceToFile.TryGetValue(faceName, out var file))
            {
                var path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Fonts), file);
                if (File.Exists(path))
                    return File.ReadAllBytes(path);
            }
            return null;
        }
    }
}
