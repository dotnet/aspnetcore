// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.UrlRewrite.PatternSegments
{
    public class LiteralSegment : PatternSegment
    {
        public string Literal { get; set; }

        public LiteralSegment(string literal)
        {
            Literal = literal;
        }

        public override string Evaluate(HttpContext context, Match ruleMatch, Match condMatch)
        {
            return Literal;
        }
    }
}
