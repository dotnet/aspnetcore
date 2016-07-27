// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    public static class RuleRegexParser
    {
        public static ParsedModRewriteExpression ParseRuleRegex(string regex)
        {
            if (regex == null || regex == String.Empty)
            {
                throw new FormatException();
            }
            if (regex.StartsWith("!"))
            {
                return new ParsedModRewriteExpression { Invert = true, Operand = regex.Substring(1) };
            }
            else
            {
                return new ParsedModRewriteExpression { Invert = false, Operand = regex};
            }
        }
    }
}
