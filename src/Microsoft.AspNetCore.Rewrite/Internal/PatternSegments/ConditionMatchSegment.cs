// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.Internal.PatternSegments
{
    public class ConditionMatchSegment : PatternSegment
    {
        private readonly int _index;

        public ConditionMatchSegment(int index)
        {
            _index = index;
        }

        public override string Evaluate(RewriteContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            return condMatch?.BackReference[_index].Value;
        }
    }
}
