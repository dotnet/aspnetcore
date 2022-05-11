// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite.PatternSegments;

internal sealed class QueryStringSegment : PatternSegment
{
    public override string? Evaluate(RewriteContext context, BackReferenceCollection? ruleBackRefernces, BackReferenceCollection? conditionBackReferences)
    {
        var queryString = context.HttpContext.Request.QueryString.ToString();

        if (!string.IsNullOrEmpty(queryString))
        {
            return queryString.Substring(1);
        }

        return queryString;
    }
}
