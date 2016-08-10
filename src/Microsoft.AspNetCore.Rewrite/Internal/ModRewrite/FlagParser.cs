// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    public class FlagParser
    { 
        public static Flags Parse(string flagString)
        {
            if (string.IsNullOrEmpty(flagString))
            {
                return null;
            }

            // Check that flags are contained within []
            if (!flagString.StartsWith("[") || !flagString.EndsWith("]"))
            {
                throw new FormatException();
            }

            // Lexing esque step to split all flags.
            // Invalid syntax to have any spaces.
            var tokens = flagString.Substring(1, flagString.Length - 2).Split(',');
            var flags = new Flags();
            foreach (string token in tokens)
            {
                var hasPayload = token.Split('=');
                if (hasPayload.Length == 2)
                {
                    flags.SetFlag(hasPayload[0], hasPayload[1]);
                }
                else
                {
                    flags.SetFlag(hasPayload[0], string.Empty);
                }
            }
            return flags;
        }
    }
}
