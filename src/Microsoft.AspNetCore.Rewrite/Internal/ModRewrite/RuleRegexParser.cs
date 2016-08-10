// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    public static class RuleRegexParser
    {
        public static ParsedModRewriteInput ParseRuleRegex(string regex)
        {
            if (regex == null || regex == string.Empty)
            {
                throw new FormatException("Regex expression is null");
            }
            if (regex.StartsWith("!"))
            {
                return new ParsedModRewriteInput { Invert = true, Operand = regex.Substring(1) };
            }
            else
            {
                return new ParsedModRewriteInput { Invert = false, Operand = regex};
            }
        }
    }
}
