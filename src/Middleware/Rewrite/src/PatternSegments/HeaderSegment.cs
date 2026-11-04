// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite.PatternSegments;

internal sealed class HeaderSegment : PatternSegment
{
    private readonly string _header;

    public HeaderSegment(string header)
    {
        _header = header;
    }

    public override string? Evaluate(RewriteContext context, BackReferenceCollection? ruleBackReferences, BackReferenceCollection? conditionBackReferences)
    {
        return context.HttpContext.Request.Headers[_header];
    }
}
