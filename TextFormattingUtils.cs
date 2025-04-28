using System.Text.RegularExpressions;

namespace TextDecorator
{
    public static class TextFormatterUtils
    {
        private static readonly Regex tagRegex = new Regex(@"<.*?>", RegexOptions.Compiled);

        public static string ApplyTag(string richText, int plainStart, int plainEnd, string openTag, string closeTag)
        {
            if (plainStart >= plainEnd) return richText;

            // strip tags to get raw plain text
            string plainText = tagRegex.Replace(richText, "");

            if (plainStart < 0 || plainEnd > plainText.Length) return richText;

            // map plainStart and plainEnd back to richText indices
            int richStart = GetRichTextIndex(richText, plainStart);
            int richEnd = GetRichTextIndex(richText, plainEnd);

            if (richStart == -1 || richEnd == -1 || richStart >= richEnd) return richText;

            // inject tags at the mapped positions
            string before = richText.Substring(0, richStart);
            string middle = richText.Substring(richStart, richEnd - richStart);
            string after = richText.Substring(richEnd);

            return before + openTag + middle + closeTag + after;
        }

        // map index from plain (tagless) text to rich text
        private static int GetRichTextIndex(string richText, int plainIndex)
        {
            int rIndex = 0;
            int pIndex = 0;
            bool insideTag = false;

            while (rIndex < richText.Length)
            {
                char c = richText[rIndex];
                if (c == '<') insideTag = true;
                if (!insideTag)
                {
                    if (pIndex == plainIndex) return rIndex;
                    pIndex++;
                }
                if (c == '>') insideTag = false;
                rIndex++;
            }

            return (pIndex == plainIndex) ? rIndex : -1;
        }
    }
}
