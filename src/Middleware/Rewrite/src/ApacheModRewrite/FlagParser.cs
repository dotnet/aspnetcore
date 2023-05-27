// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite;

internal static class FlagParser
{
    private static readonly IDictionary<string, FlagType> _ruleFlagLookup = new Dictionary<string, FlagType>(StringComparer.OrdinalIgnoreCase) {
            { "b", FlagType.EscapeBackreference},
            { "c", FlagType.Chain },
            { "chain", FlagType.Chain},
            { "co", FlagType.Cookie },
            { "cookie", FlagType.Cookie },
            { "dpi", FlagType.DiscardPath },
            { "discardpath", FlagType.DiscardPath },
            { "e", FlagType.Env},
            { "env", FlagType.Env},
            { "end", FlagType.End },
            { "f", FlagType.Forbidden },
            { "forbidden", FlagType.Forbidden },
            { "g", FlagType.Gone },
            { "gone", FlagType.Gone },
            { "h", FlagType.Handler },
            { "handler", FlagType.Handler },
            { "l", FlagType.Last },
            { "last", FlagType.Last },
            { "n", FlagType.Next },
            { "next", FlagType.Next },
            { "nc", FlagType.NoCase },
            { "nocase", FlagType.NoCase },
            { "ne", FlagType.NoEscape },
            { "noescape", FlagType.NoEscape },
            { "ns", FlagType.NoSubReq },
            { "nosubreq", FlagType.NoSubReq },
            { "or", FlagType.Or },
            { "ornext", FlagType.Or },
            { "p", FlagType.Proxy },
            { "proxy", FlagType.Proxy },
            { "pt", FlagType.PassThrough },
            { "passthrough", FlagType.PassThrough },
            { "qsa", FlagType.QSAppend },
            { "qsappend", FlagType.QSAppend },
            { "qsd", FlagType.QSDiscard },
            { "qsdiscard", FlagType.QSDiscard },
            { "qsl", FlagType.QSLast },
            { "qslast", FlagType.QSLast },
            { "r", FlagType.Redirect },
            { "redirect", FlagType.Redirect },
            { "s", FlagType.Skip },
            { "skip", FlagType.Skip },
            { "t", FlagType.Type },
            { "type", FlagType.Type },
        };

    public static Flags Parse(string flagString)
    {
        ArgumentException.ThrowIfNullOrEmpty(flagString);

        // Check that flags are contained within []
        // Guaranteed to have a length of at least 1 here, so this will never throw for indexing.
        if (!(flagString[0] == '[' && flagString[flagString.Length - 1] == ']'))
        {
            throw new FormatException("Flags should start and end with square brackets: [flags]");
        }

        // Lexing esque step to split all flags.
        // Invalid syntax to have any spaces.
        var tokens = flagString.Substring(1, flagString.Length - 2).Split(',');
        var flags = new Flags();
        Span<Range> hasPayload = stackalloc Range[3];
        foreach (var token in tokens)
        {
            var tokenSpan = token.AsSpan();
            var length = tokenSpan.Split(hasPayload, '=');

            FlagType flag;
            if (!_ruleFlagLookup.TryGetValue(tokenSpan[hasPayload[0]].ToString(), out flag))
            {
                throw new FormatException($"Unrecognized flag: '{tokenSpan[hasPayload[0]]}'");
            }

            if (length == 2)
            {
                flags.SetFlag(flag, tokenSpan[hasPayload[1]].ToString());
            }
            else
            {
                flags.SetFlag(flag, string.Empty);
            }
        }
        return flags;
    }
}
