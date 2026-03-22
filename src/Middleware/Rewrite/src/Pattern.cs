// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite;

internal sealed class Pattern
{
    public IList<PatternSegment> PatternSegments { get; }
    public Pattern(IList<PatternSegment> patternSegments)
    {
        PatternSegments = patternSegments;
    }

    public string Evaluate(RewriteContext context, BackReferenceCollection? ruleBackReferences, BackReferenceCollection? conditionBackReferences)
    {
        // Use the starting length to handle recursive pattern evaluations correctly.
        // RewriteMapSegment and other segments may recursively call Evaluate on inner
        // patterns, which would corrupt the shared StringBuilder if we always Clear() it.
        var startIndex = context.Builder.Length;
        foreach (var pattern in PatternSegments)
        {
            context.Builder.Append(pattern.Evaluate(context, ruleBackReferences, conditionBackReferences));
        }
        var retVal = context.Builder.ToString(startIndex, context.Builder.Length - startIndex);
        context.Builder.Length = startIndex;
        return retVal;
    }
}
