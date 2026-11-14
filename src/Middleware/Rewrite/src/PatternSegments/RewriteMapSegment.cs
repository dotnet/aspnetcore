// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Rewrite.IISUrlRewrite;

namespace Microsoft.AspNetCore.Rewrite.PatternSegments;

internal sealed class RewriteMapSegment : PatternSegment
{
    private readonly IISRewriteMap _rewriteMap;
    private readonly Pattern _pattern;

    public RewriteMapSegment(IISRewriteMap rewriteMap, Pattern pattern)
    {
        _rewriteMap = rewriteMap;
        _pattern = pattern;
    }

    public override string? Evaluate(RewriteContext context, BackReferenceCollection? ruleBackReferences, BackReferenceCollection? conditionBackReferences)
    {
        // PERF as we share the string builder across the context, we need to make a new one here to
        // evaluate the rewrite map key, which may itself contain nested pattern segments.
        var tempBuilder = context.Builder;
        context.Builder = new StringBuilder(64);
        var key = _pattern.Evaluate(context, ruleBackReferences, conditionBackReferences).ToLowerInvariant();
        context.Builder = tempBuilder;
        return _rewriteMap[key];
    }
}
