using System.Text;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// Template engine helpers (formatting).
    /// Split from TemplateEngine.cs to keep each file under the line limit.
    /// </summary>
    public static partial class TemplateEngine
    {
        /// <summary>
        /// Convert ASCII digits in the input string to Unicode bold digits for visual emphasis.
        /// Note: Bold digits are surrogate pairs in UTF-16, so this method appends strings (not chars).
        /// </summary>
        private static string ToBoldDigits(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";

            var bold = new[] { "𝟎", "𝟏", "𝟐", "𝟑", "𝟒", "𝟓", "𝟔", "𝟕", "𝟖", "𝟗" };
            var sb = new StringBuilder();

            foreach (var ch in s)
            {
                if (ch >= '0' && ch <= '9')
                    sb.Append(bold[ch - '0']);
                else
                    sb.Append(ch);
            }

            return sb.ToString();
        }
    }
}


