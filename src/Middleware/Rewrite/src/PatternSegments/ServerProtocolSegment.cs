// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Rewrite.PatternSegments;

internal sealed class ServerProtocolSegment : PatternSegment
{
    public override string? Evaluate(RewriteContext context, BackReferenceCollection? ruleBackReferences, BackReferenceCollection? conditionBackReferences)
    {
        return context.HttpContext.Features.Get<IHttpRequestFeature>()?.Protocol;
    }
}
