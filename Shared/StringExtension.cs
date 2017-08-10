using System;
using Microsoft.SPOT;

namespace TrimbleMonitor
{
    public static class StringExtension
    {

        public static string PadLeft(string text, int totalWidth)
        {
            return PadLeft(text, totalWidth, ' ');
        }

        public static string PadLeft(string text, int totalWidth, char paddingChar)
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


        public static string PadRight(string text, int totalWidth)
        {
            return PadRight(text, totalWidth, ' ');
        }

        public static string PadRight(string text, int totalWidth, char paddingChar)
        {
            if (totalWidth < 0)
                throw new ArgumentOutOfRangeException("totalWidth", "< 0");

            if (totalWidth < text.Length)
                return text;
            if (totalWidth == 0)
                return string.Empty;

            while (totalWidth > text.Length)
            {
                text = text + paddingChar;
            }

            return text;
        }

    
    }
}
