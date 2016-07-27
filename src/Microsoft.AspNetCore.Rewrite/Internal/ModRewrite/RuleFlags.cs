// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    public class RuleFlags
    {
        private IDictionary<string, RuleFlagType> _ruleFlagLookup = new Dictionary<string, RuleFlagType>(StringComparer.OrdinalIgnoreCase) {
            { "b", RuleFlagType.EscapeBackreference},
            { "c", RuleFlagType.Chain },
            { "chain", RuleFlagType.Chain},
            { "co", RuleFlagType.Cookie },
            { "cookie", RuleFlagType.Cookie },
            { "dpi", RuleFlagType.DiscardPath },
            { "discardpath", RuleFlagType.DiscardPath },
            { "e", RuleFlagType.Env},
            { "env", RuleFlagType.Env},
            { "end", RuleFlagType.End },
            { "f", RuleFlagType.Forbidden },
            { "forbidden", RuleFlagType.Forbidden },
            { "g", RuleFlagType.Gone },
            { "gone", RuleFlagType.Gone },
            { "h", RuleFlagType.Handler },
            { "handler", RuleFlagType.Handler },
            { "l", RuleFlagType.Last },
            { "last", RuleFlagType.Last },
            { "n", RuleFlagType.Next },
            { "next", RuleFlagType.Next },
            { "nc", RuleFlagType.NoCase },
            { "nocase", RuleFlagType.NoCase },
            { "ne", RuleFlagType.NoEscape },
            { "noescape", RuleFlagType.NoEscape },
            { "ns", RuleFlagType.NoSubReq },
            { "nosubreq", RuleFlagType.NoSubReq },
            { "p", RuleFlagType.Proxy },
            { "proxy", RuleFlagType.Proxy },
            { "pt", RuleFlagType.PassThrough },
            { "passthrough", RuleFlagType.PassThrough },
            { "qsa", RuleFlagType.QSAppend },
            { "qsappend", RuleFlagType.QSAppend },
            { "qsd", RuleFlagType.QSDiscard },
            { "qsdiscard", RuleFlagType.QSDiscard },
            { "qsl", RuleFlagType.QSLast },
            { "qslast", RuleFlagType.QSLast },
            { "r", RuleFlagType.Redirect },
            { "redirect", RuleFlagType.Redirect },
            { "s", RuleFlagType.Skip },
            { "skip", RuleFlagType.Skip },
            { "t", RuleFlagType.Type },
            { "type", RuleFlagType.Type },
            // TODO make this a load bool instead of a flag for the file and rules.
            { "u", RuleFlagType.FullUrl },
            { "url", RuleFlagType.FullUrl }
            };

        public IDictionary<RuleFlagType, string> FlagDictionary { get; }

        public RuleFlags(IDictionary<RuleFlagType, string> flags)
        {
            // TODO use ref to check dictionary equality
            FlagDictionary = flags;
        }

        public RuleFlags()
        {
            FlagDictionary = new Dictionary<RuleFlagType, string>();
        }

        public void SetFlag(string flag, string value)
        {
            RuleFlagType res;
            if (!_ruleFlagLookup.TryGetValue(flag, out res))
            {
                throw new FormatException("Invalid flag");
            }
            SetFlag(res, value);
        }
        public void SetFlag(RuleFlagType flag, string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }
            FlagDictionary[flag] = value;
        }

        public string GetValue(RuleFlagType flag)
        {
            CleanupResources();
            string res;
            if (!FlagDictionary.TryGetValue(flag, out res))
            {
                return null;
            }
            return res;
        }

        public string this[RuleFlagType flag]
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

        public bool HasFlag(RuleFlagType flag)
        {
            CleanupResources();
            string res;
            return FlagDictionary.TryGetValue(flag, out res);
        }

        private void CleanupResources()
        {
            if (_ruleFlagLookup != null)
            {
                _ruleFlagLookup = null;
            }
        }
    }
}
