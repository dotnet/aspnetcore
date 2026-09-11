// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite;

/// <summary>
/// Tokenizes a mod_rewrite rule, delimited by spaces.
/// </summary>
internal sealed class Tokenizer
{
    private const char Space = ' ';
    private const char Escape = '\\';
    private const char Tab = '\t';
    private const char Quote = '"';

    /// <summary>
    /// Splits a string on whitespace, ignoring spaces, creating into a list of strings.
    /// </summary>
    /// <param name="rule">The rule to tokenize.</param>
    /// <returns>A list of tokens.</returns>
    public static IList<string>? Tokenize(string rule)
    {
        // TODO make list of strings a reference to the original rule? (run into problems with escaped spaces).
        // TODO handle "s and probably replace \ character with no slash.
        if (string.IsNullOrEmpty(rule))
        {
            return null;
        }
        var context = new ParserContext(rule);
        context.Next();

        var tokens = new List<string>();
        context.Mark();
        while (true)
        {
            switch (context.Current)
            {
                case Escape:
                    // Need to progress such that the next character is not evaluated.
                    if (!context.Next())
                    {
                        // Means that a character was not escaped appropriately Ex: "foo\"
                        throw new FormatException($"Invalid escaper character in string: {rule}");
                    }
                    break;
                case Quote:
                    // Ignore all characters until the next quote is hit
                    if (!context.Next())
                    {
                        throw new FormatException($"Mismatched number of quotes: {rule}");
                    }

                    while (context.Current != Quote)
                    {
                        if (!context.Next())
                        {
                            throw new FormatException($"Mismatched number of quotes: {rule}");
                        }
                    }
                    break;
                case Space:
                case Tab:
                    // time to capture!
                    var token = context.Capture();
                    if (!string.IsNullOrEmpty(token))
                    {
                        tokens.Add(token);
                        do
                        {
                            if (!context.Next())
                            {
                                // At end of string, we can return at this point.
                                RemoveQuotesAndEscapeCharacters(tokens);
                                return tokens;
                            }
                        } while (context.Current == Space || context.Current == Tab);
                        context.Mark();
                        context.Back();
                    }
                    break;
            }
            if (!context.Next())
            {
                // End of string. Capture.
                break;
            }
        }
        var done = context.Capture();
        if (!string.IsNullOrEmpty(done))
        {
            tokens.Add(done);
        }

        RemoveQuotesAndEscapeCharacters(tokens);
        return tokens;
    }

    // Need to remove leading and trailing slashes if they exist.
    // This is on start-up, so more forgivening towards substrings/ new strings
    // If this is a perf/memory problem, discuss later.
    private static void RemoveQuotesAndEscapeCharacters(IList<string> tokens)
    {
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            var trimmed = token.Trim('\"');
            tokens[i] = UnescapeToken(trimmed);
        }
    }

    // Unescapes characters escaped for the mod_rewrite file format while preserving regex escape
    // sequences such as \d, which must be passed through unaltered to the regex engine.
    private static string UnescapeToken(string token)
    {
        if (!token.Contains(Escape))
        {
            return token;
        }

        var builder = new StringBuilder(token.Length);
        for (var i = 0; i < token.Length; i++)
        {
            var current = token[i];
            if (current != Escape)
            {
                builder.Append(current);
                continue;
            }

            if (i == token.Length - 1)
            {
                // A trailing escape character can only occur within a quoted token, since
                // Tokenize throws otherwise. It is removed, matching the previous behavior.
                break;
            }

            var escaped = token[++i];
            switch (escaped)
            {
                case 'a':
                    builder.Append('\a');
                    break;
                case 'f':
                    builder.Append('\f');
                    break;
                case 'n':
                    builder.Append('\n');
                    break;
                case 'r':
                    builder.Append('\r');
                    break;
                case 't':
                    builder.Append('\t');
                    break;
                case 'v':
                    builder.Append('\v');
                    break;
                case 'x':
                case 'u':
                {
                    var length = escaped == 'x' ? 2 : 4;
                    var value = 0;
                    var valid = true;
                    for (var j = 1; j <= length; j++)
                    {
                        var digit = i + j < token.Length ? GetHexDigitValue(token[i + j]) : -1;
                        if (digit < 0)
                        {
                            valid = false;
                            break;
                        }
                        value = (value << 4) | digit;
                    }
                    if (valid)
                    {
                        builder.Append((char)value);
                        i += length;
                    }
                    else
                    {
                        builder.Append(Escape).Append(escaped);
                    }
                    break;
                }
                case >= '0' and <= '7':
                {
                    var value = escaped - '0';
                    var count = 1;
                    while (count < 3 && i + 1 < token.Length && token[i + 1] >= '0' && token[i + 1] <= '7')
                    {
                        value = (value << 3) | (token[++i] - '0');
                        count++;
                    }
                    builder.Append((char)(value & 0xFF));
                    break;
                }
                default:
                    if ((escaped >= 'a' && escaped <= 'z') || (escaped >= 'A' && escaped <= 'Z') || escaped is '8' or '9')
                    {
                        // Preserve regex shorthand escapes and backreferences so they are
                        // interpreted by the regex engine rather than unescaped here.
                        builder.Append(Escape).Append(escaped);
                    }
                    else
                    {
                        builder.Append(escaped);
                    }
                    break;
            }
        }

        return builder.ToString();
    }

    private static int GetHexDigitValue(char character) => character switch
    {
        >= '0' and <= '9' => character - '0',
        >= 'a' and <= 'f' => character - 'a' + 10,
        >= 'A' and <= 'F' => character - 'A' + 10,
        _ => -1,
    };
}
