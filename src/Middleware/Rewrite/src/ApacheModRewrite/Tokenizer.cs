// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

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
            tokens[i] = Regex.Unescape(trimmed);
        }
    }
}
