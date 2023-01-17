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
        foreach (var pattern in PatternSegments)
        {
            context.Builder.Append(pattern.Evaluate(context, ruleBackReferences, conditionBackReferences));
        }
        var retVal = context.Builder.ToString();
        context.Builder.Clear();
        return retVal;
    }
}
