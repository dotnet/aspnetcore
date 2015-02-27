using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace DefinedCharListGenerator
{
    /// <summary>
    /// This program outputs the 'unicode-defined-chars.bin' bitmap file.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // The input file should be UnicodeData.txt from the UCD corresponding to the
            // version of the Unicode spec we're consuming.
            // More info: http://www.unicode.org/reports/tr44/tr44-14.html#UCD_Files
            // Latest UnicodeData.txt: http://www.unicode.org/Public/UCD/latest/ucd/UnicodeData.txt

            const uint MAX_UNICODE_CHAR = 0x10FFFF; // Unicode range is U+0000 .. U+10FFFF
            bool[] definedChars = new bool[MAX_UNICODE_CHAR + 1];

            // Read all defined characters from the input file.
            string[] allLines = File.ReadAllLines("UnicodeData.txt");

            // Each line is a semicolon-delimited list of information:
            // <value>;<name>;<category>;...
            foreach (string line in allLines)
            {
                string[] splitLine = line.Split(new char[] { ';' }, 4);

                // We only allow certain categories of code points.
                // Zs (space separators) aren't included, but we allow U+0020 SPACE as a special case
                uint codepoint = uint.Parse(splitLine[0], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                string category = splitLine[2];
                if (!(codepoint == (uint)' ' || IsAllowedUnicodeCategory(category)))
                {
                    continue;
                }

                Debug.Assert(codepoint <= MAX_UNICODE_CHAR);
                definedChars[codepoint] = true;
            }

            // Finally, write the list of defined characters out as a bitmap.
            // Each consecutive block of 8 chars is written as a single byte.
            // For instance, the first byte of the output file contains the
            // bitmap for the following codepoints:
            // - (bit 7) U+0007 [MSB]
            // - (bit 6) U+0006
            // - (bit 5) U+0005
            // - (bit 4) U+0004
            // - (bit 3) U+0003
            // - (bit 2) U+0002
            // - (bit 1) U+0001
            // - (bit 0) U+0000 [LSB]
            // The next byte will contain the bitmap for U+000F to U+0008,
            // and so on until the last byte, which is U+FFFF to U+FFF8.
            // The bytes are written out in little-endian order.
            // We're only concerned about the BMP (U+0000 .. U+FFFF) for now.
            MemoryStream outBuffer = new MemoryStream();
            for (int i = 0; i < 0x10000; i += 8)
            {
                int thisByte = 0;
                for (int j = 7; j >= 0; j--)
                {
                    thisByte <<= 1;
                    if (definedChars[i + j])
                    {
                        thisByte |= 0x1;
                    }
                }
                outBuffer.WriteByte((byte)thisByte);
            }

            File.WriteAllBytes("unicode-defined-chars.bin", outBuffer.ToArray());
        }

        private static bool IsAllowedUnicodeCategory(string category)
        {
            // We only allow certain classes of characters
            return category == "Lu" /* letters */
                || category == "Ll"
                || category == "Lt"
                || category == "Lm"
                || category == "Lo"
                || category == "Mn" /* marks */
                || category == "Mc"
                || category == "Me"
                || category == "Nd" /* numbers */
                || category == "Nl"
                || category == "No"
                || category == "Pc" /* punctuation */
                || category == "Pd"
                || category == "Ps"
                || category == "Pe"
                || category == "Pi"
                || category == "Pf"
                || category == "Po"
                || category == "Sm" /* symbols */
                || category == "Sc"
                || category == "Sk"
                || category == "So"
                || category == "Cf"; /* other */
        }
    }
}
