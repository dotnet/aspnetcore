// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.PatternSegments
{
    internal class RuleMatchSegment : PatternSegment
    {
        private readonly int _index;

        public RuleMatchSegment(int index)
        {
            _index = index;
        }

        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences)
        {
            return ruleBackReferences[_index];
        }
    }
}
