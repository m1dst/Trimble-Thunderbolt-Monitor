using System;

namespace TrimbleMonitor
{
    public static class StringExtension
    {

        public static string PadLeft(this string text, int totalWidth)
        {
            return PadLeft(text, totalWidth, ' ');
        }

        public static string PadLeft(this string text, int totalWidth, char paddingChar)
        {
            if (totalWidth < 0)
                throw new ArgumentOutOfRangeException("totalWidth", "< 0");

            if (totalWidth < text.Length)
                return text;
            if (totalWidth == 0)
                return string.Empty;

            while (totalWidth > text.Length)
            {
                text = paddingChar + text;
            }

            return text;
        }


        public static string PadRight(this string text, int totalWidth)
        {
            return PadRight(text, totalWidth, ' ');
        }

        public static string PadRight(this string text, int totalWidth, char paddingChar)
        {
            if (totalWidth < 0)
                throw new ArgumentOutOfRangeException("totalWidth", "< 0");

            if (totalWidth < text.Length)
                return text;
            if (totalWidth == 0)
                return string.Empty;

            while (totalWidth > text.Length)
            {
                text += paddingChar;
            }

            return text;
        }

    
    }
}
