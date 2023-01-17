// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite.PatternSegments;

internal sealed class IsHttpsUrlSegment : PatternSegment
{
    // Note: Mod rewrite pattern matches on lower case "on" and "off"
    // while IIS looks for capitalized "ON" and "OFF"
    public override string? Evaluate(RewriteContext context, BackReferenceCollection? ruleBackReferences, BackReferenceCollection? conditionBackReferences)
    {
        return context.HttpContext.Request.IsHttps ? "ON" : "OFF";
    }
}
