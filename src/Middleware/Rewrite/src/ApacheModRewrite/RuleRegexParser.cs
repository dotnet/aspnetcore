// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite
{
    internal class RuleRegexParser
    {
        public ParsedModRewriteInput ParseRuleRegex(string regex)
        {
            if (string.IsNullOrEmpty(regex))
            {
                throw new FormatException("Regex expression is null");
            }
            if (regex[0] == '!')
            {
                return new ParsedModRewriteInput { Invert = true, Operand = regex.Substring(1) };
            }
            else
            {
                return new ParsedModRewriteInput { Invert = false, Operand = regex };
            }
        }
    }
}
