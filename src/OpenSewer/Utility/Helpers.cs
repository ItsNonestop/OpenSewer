using System.Text.RegularExpressions;

namespace OpenSewer.Utility
{
    internal static class Helpers
    {
        internal static string ColorizedMatch(string searchTerm, string text, RichtextColor color)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return text;

            var pattern = Regex.Escape(searchTerm); // literal match, no regex injection

            return Regex.Replace(
                text,
                pattern,
                m => $"<color={color}>{m.Value}</color>",
                RegexOptions.IgnoreCase
            );
        }

        internal static string ColorizedMatch(string findText, int inValue, RichtextColor color)
        {
            var searchText = inValue.ToString();
            var pattern = "^" + Regex.Escape(findText); // prefix only

            return Regex.Replace(
                inValue.ToString(),
                pattern,
                m => $"<color={color}>{m.Value}</color>",
                RegexOptions.IgnoreCase
            );
        }
    }
}

