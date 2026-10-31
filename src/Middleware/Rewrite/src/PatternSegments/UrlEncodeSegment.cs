// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Rewrite.PatternSegments;

internal sealed class UrlEncodeSegment : PatternSegment
{
    private readonly Pattern _pattern;

    public UrlEncodeSegment(Pattern pattern)
    {
        _pattern = pattern;
    }

    public override string? Evaluate(RewriteContext context, BackReferenceCollection? ruleBackReferences, BackReferenceCollection? conditionBackReferences)
    {
        var oldBuilder = context.Builder;
        // PERF
        // Because we need to be able to evaluate multiple nested patterns,
        // we provided a new string builder and evaluate the new pattern,
        // and restore it after evaluation.
        context.Builder = new StringBuilder(64);
        var pattern = _pattern.Evaluate(context, ruleBackReferences, conditionBackReferences);
        context.Builder = oldBuilder;
        return Uri.EscapeDataString(pattern);
    }
}
