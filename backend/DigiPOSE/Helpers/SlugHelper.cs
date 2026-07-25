using System.Globalization;
using System.Text;

namespace DigiPOSE.Web.Helpers
{
    public static class SlugHelper
    {
        /// <summary>
        /// Generates an SEO-friendly URL slug from Vietnamese or international text.
        /// Optimized for high performance (Single-pass StringBuilder, zero Regex overhead).
        /// </summary>
        /// <param name="title">Input string to convert into slug</param>
        /// <returns>Cleaned, hyphenated URL slug</returns>
        public static string GenerateSlug(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return string.Empty;

            // 1. Lowercase & trim
            string text = title.Trim().ToLowerInvariant();

            // 2. Explicit replacement for Vietnamese 'đ' / 'Đ'
            text = text.Replace('đ', 'd').Replace('Đ', 'd');

            // 3. Decompose unicode diacritics (FormD)
            string normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            bool prevIsHyphen = false;

            for (int i = 0; i < normalized.Length; i++)
            {
                char c = normalized[i];
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);

                // Skip diacritics / accent marks
                if (uc == UnicodeCategory.NonSpacingMark)
                    continue;

                // Keep ASCII alphanumeric chars
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    sb.Append(c);
                    prevIsHyphen = false;
                }
                // Convert spaces, hyphens, and non-alphanumeric chars into a single hyphen
                else if (sb.Length > 0 && !prevIsHyphen)
                {
                    sb.Append('-');
                    prevIsHyphen = true;
                }
            }

            if (sb.Length > 0 && sb[sb.Length - 1] == '-')
            {
                sb.Length--;
            }

            return sb.ToString();
        }
    }
}