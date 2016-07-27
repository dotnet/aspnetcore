// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    /// <summary>
    /// Parses the flags 
    /// </summary>
    public class FlagParser
    {
        // TODO Refactor Rule and Condition Flags under IFlags
        public static RuleFlags ParseRuleFlags(string flagString)
        {
            var flags = new RuleFlags();
            ParseRuleFlags(flagString, flags);
            return flags;
        }

        public static void ParseRuleFlags(string flagString, RuleFlags flags)
        {
            if (string.IsNullOrEmpty(flagString))
            {
                return;
            }
            // Check that flags are contained within []
            if (!flagString.StartsWith("[") || !flagString.EndsWith("]"))
            {
                throw new FormatException();
            }
            // Illegal syntax to have any spaces.
            var tokens = flagString.Substring(1, flagString.Length - 2).Split(',');
            // Go through tokens and verify they have meaning.
            // Flags can be KVPs, delimited by '='.
            foreach (string token in tokens)
            {
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }
                string[] kvp = token.Split('=');
                if (kvp.Length == 1)
                {
                    flags.SetFlag(kvp[0], null);
                }
                else if (kvp.Length == 2)
                {
                    flags.SetFlag(kvp[0], kvp[1]);
                }
                else
                {
                    throw new FormatException();
                }
            }
        }

        public static ConditionFlags ParseConditionFlags(string flagString)
        {
            var flags = new ConditionFlags();
            ParseConditionFlags(flagString, flags);
            return flags;
        }

        public static void ParseConditionFlags(string flagString, ConditionFlags flags)
        {
            if (string.IsNullOrEmpty(flagString))
            {
                return;
            }
            // Check that flags are contained within []
            if (!flagString.StartsWith("[") || !flagString.EndsWith("]"))
            {
                throw new FormatException();
            }
            // Lexing esque step to split all flags.
            // Illegal syntax to have any spaces.
            var tokens = flagString.Substring(1, flagString.Length - 2).Split(',');
            // Go through tokens and verify they have meaning.
            // Flags can be KVPs, delimited by '='.
            foreach (string token in tokens)
            {
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }
                string[] kvp = token.Split('=');
                if (kvp.Length == 1)
                {
                    flags.SetFlag(kvp[0], null);
                }
                else if (kvp.Length == 2)
                {
                    flags.SetFlag(kvp[0], kvp[1]);
                }
                else
                {
                    throw new FormatException();
                }
            }
        }
    }
}
