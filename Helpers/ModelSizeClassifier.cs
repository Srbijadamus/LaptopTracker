using System.Linq;
using System.Text.RegularExpressions;
using LaptopTracker.Models;

namespace LaptopTracker.Helpers
{
    // Single source of truth for mapping a raw device model string to a standard
    // size class (S=13", M=14", L=15", D=Desktop). No other file may parse model
    // strings — route all such logic through Classify().
    public static class ModelSizeClassifier
    {
        private static readonly string[] DesktopKeywords =
        {
            "DESKTOP", "TOWER", "SFF", "OPTIPLEX", "ELITEDESK", "PRODESK", "THINKCENTRE", "PRECISION"
        };

        private static readonly Regex FamilyNumber = new(@"\b(\d{3})\b", RegexOptions.Compiled);

        // Returns null when the model string cannot be classified — callers must
        // route null results into an explicit "unmapped" bucket, never guess.
        public static SizeClass? Classify(string? rawModel)
        {
            if (string.IsNullOrWhiteSpace(rawModel)) return null;

            var normalized = Regex.Replace(rawModel.Trim().ToUpperInvariant(), @"\s+", " ");

            if (DesktopKeywords.Any(normalized.Contains))
                return SizeClass.D;

            var match = FamilyNumber.Match(normalized);
            if (!match.Success) return null;

            // Family number's last two digits identify the screen size across
            // generations (e.g. 830/835 -> S, 840/845 -> M, 850/855/650 -> L)
            // without hardcoding every G-generation.
            return (int.Parse(match.Value) % 100) switch
            {
                30 or 35 => SizeClass.S,
                40 or 45 => SizeClass.M,
                50 or 55 => SizeClass.L,
                _ => null
            };
        }
    }
}
