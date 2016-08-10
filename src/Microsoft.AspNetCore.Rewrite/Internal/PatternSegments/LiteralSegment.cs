// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.Internal.PatternSegments
{
    public class LiteralSegment : PatternSegment
    {
        public string Literal { get; set; }

        public LiteralSegment(string literal)
        {
            Literal = literal;
        }

        public override string Evaluate(RewriteContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            return Literal;
        }
    }
}
