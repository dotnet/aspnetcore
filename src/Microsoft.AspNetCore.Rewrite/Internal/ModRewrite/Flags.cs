// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    // For more information of flags, and what flags we currently support:
    // https://github.com/aspnet/BasicMiddleware/issues/66
    // http://httpd.apache.org/docs/current/expr.html#vars
    public class Flags
    {
        private static IDictionary<string, FlagType> _ruleFlagLookup = new Dictionary<string, FlagType>(StringComparer.OrdinalIgnoreCase) {
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

        public IDictionary<FlagType, string> FlagDictionary { get; }

        public Flags(IDictionary<FlagType, string> flags)
        {
            FlagDictionary = flags;
        }

        public Flags()
        {
            FlagDictionary = new Dictionary<FlagType, string>();
        }

        public void SetFlag(string flag, string value)
        {
            FlagType res;
            if (!_ruleFlagLookup.TryGetValue(flag, out res))
            {
                throw new FormatException("Unrecognized flag");
            }
            SetFlag(res, value);
        }

        public void SetFlag(FlagType flag, string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }
            FlagDictionary[flag] = value;
        }

        public bool GetValue(FlagType flag, out string value)
        {
            string res;
            if (!FlagDictionary.TryGetValue(flag, out res))
            {
                value = null;
                return false;
            }
            value = res;
            return true;
        }

        public string this[FlagType flag]
        {
            get
            {
                string res;
                if (!FlagDictionary.TryGetValue(flag, out res))
                {
                    return null;
                }
                return res;
            }
            set
            {
                FlagDictionary[flag] = value ?? string.Empty;
            }
        }

        public bool HasFlag(FlagType flag)
        {
            string res;
            return FlagDictionary.TryGetValue(flag, out res);
        }
    }
}
