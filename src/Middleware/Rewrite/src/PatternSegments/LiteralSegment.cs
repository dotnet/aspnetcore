// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.PatternSegments
{
    internal class LiteralSegment : PatternSegment
    {
        private readonly string _literal;

        public LiteralSegment(string literal)
        {
            _literal = literal;
        }

        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences)
        {
            return _literal;
        }
    }
}
