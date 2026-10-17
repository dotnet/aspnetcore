// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite.PatternSegments;

internal sealed class PathInfoSegment : PatternSegment
{
    public override string? Evaluate(RewriteContext context, BackReferenceCollection? ruleBackReferences, BackReferenceCollection? conditionBackReferences)
    {
        var request = context.HttpContext.Request;

        return request.PathBase.Add(request.Path).Value;
    }
}
