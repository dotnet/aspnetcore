// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Rewrite
{
    internal class Pattern
    {
        public IList<PatternSegment> PatternSegments { get; }
        public Pattern(IList<PatternSegment> patternSegments)
        {
            PatternSegments = patternSegments;
        }

        public string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences)
        {
            foreach (var pattern in PatternSegments)
            {
                context.Builder.Append(pattern.Evaluate(context, ruleBackReferences, conditionBackReferences));
            }
            var retVal = context.Builder.ToString();
            context.Builder.Clear();
            return retVal;
        }
    }
}
