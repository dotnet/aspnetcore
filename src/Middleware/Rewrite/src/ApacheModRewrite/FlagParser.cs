// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite
{
    internal class FlagParser
    {
        private readonly IDictionary<string, FlagType> _ruleFlagLookup = new Dictionary<string, FlagType>(StringComparer.OrdinalIgnoreCase) {
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

        public Flags Parse(string flagString)
        {
            if (string.IsNullOrEmpty(flagString))
            {
                throw new ArgumentException(nameof(flagString));
            }

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
            foreach (var token in tokens)
            {
                var hasPayload = token.Split('=');

                FlagType flag;
                if (!_ruleFlagLookup.TryGetValue(hasPayload[0], out flag))
                {
                    throw new FormatException($"Unrecognized flag: '{hasPayload[0]}'");
                }

                if (hasPayload.Length == 2)
                {
                    flags.SetFlag(flag, hasPayload[1]);
                }
                else
                {
                    flags.SetFlag(flag, string.Empty);
                }
            }
            return flags;
        }
    }
}
