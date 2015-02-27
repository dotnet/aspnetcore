using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UnicodeTablesGenerator
{
    /// <summary>
    /// This program outputs the 'UnicodeBlocks.generated.txt' and
    /// 'UnicodeBlocksTests.generated.txt' source files.
    /// </summary>
    /// <remarks>
    /// The generated files require some hand-tweaking. For instance, you'll need
    /// to remove surrogates and private use blocks. The files can then be merged
    /// into the *.generated.cs files as appropriate.
    /// </remarks>
    class Program
    {
        private const string _codePointFiltersGeneratedFormat = @"
/// <summary>
/// Represents the '{0}' Unicode block (U+{1}..U+{2}).
/// </summary>
/// <remarks>
/// See http://www.unicode.org/charts/PDF/U{1}.pdf for the full set of characters in this block.
/// </remarks>
public static UnicodeBlock {3}
{{
    get
    {{
        return Volatile.Read(ref _{4}) ?? CreateBlock(ref _{4}, first: '\u{1}', last: '\u{2}');
    }}
}}
private static UnicodeBlock _{4};
";

        private const string _codePointFiltersTestsGeneratedFormat = @"[InlineData('\u{1}', '\u{2}', nameof(UnicodeBlocks.{0}))]";

        private static void Main()
        {
            // The input file should be Blocks.txt from the UCD corresponding to the
            // version of the Unicode spec we're consuming.
            // More info: http://www.unicode.org/reports/tr44/
            // Latest Blocks.txt: http://www.unicode.org/Public/UCD/latest/ucd/Blocks.txt

            StringBuilder runtimeCodeBuilder = new StringBuilder();
            StringBuilder testCodeBuilder = new StringBuilder();
            string[] allLines = File.ReadAllLines("Blocks.txt");

            Regex regex = new Regex(@"^(?<startCode>[0-9A-F]{4})\.\.(?<endCode>[0-9A-F]{4}); (?<blockName>.+)$");

            foreach (var line in allLines)
            {
                // We only care about lines of the form "XXXX..XXXX; Block name"
                var match = regex.Match(line);
                if (match == null || !match.Success)
                {
                    continue;
                }

                string startCode = match.Groups["startCode"].Value;
                string endCode = match.Groups["endCode"].Value;
                string blockName = match.Groups["blockName"].Value;
                string blockNameAsProperty = RemoveAllNonAlphanumeric(blockName);
                string blockNameAsField = WithDotNetFieldCasing(blockNameAsProperty);

                runtimeCodeBuilder.AppendFormat(CultureInfo.InvariantCulture, _codePointFiltersGeneratedFormat,
                    blockName, startCode, endCode, blockNameAsProperty, blockNameAsField);

                testCodeBuilder.AppendFormat(CultureInfo.InvariantCulture, _codePointFiltersTestsGeneratedFormat,
                    blockNameAsProperty, startCode, endCode);
                testCodeBuilder.AppendLine();
            }

            File.WriteAllText("UnicodeBlocks.generated.txt", runtimeCodeBuilder.ToString());
            File.WriteAllText("UnicodeBlocksTests.generated.txt", testCodeBuilder.ToString());
        }

        private static string RemoveAllNonAlphanumeric(string blockName)
        {
            // Allow only A-Z 0-9
            return new String(blockName.ToCharArray().Where(c => ('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z') || ('0' <= c && c <= '9')).ToArray());
        }

        private static string WithDotNetFieldCasing(string input)
        {
            char[] chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (Char.IsLower(chars[i]))
                {
                    if (i > 1)
                    {
                        // restore original casing for the previous char unless the previous
                        // char was at the front of the string
                        chars[i - 1] = input[i - 1];
                    }
                    break;
                }
                else
                {
                    chars[i] = Char.ToLowerInvariant(chars[i]);
                }
            }
            return new String(chars);
        }
    }
}
