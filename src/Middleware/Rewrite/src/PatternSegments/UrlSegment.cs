// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Rewrite.IISUrlRewrite;

namespace Microsoft.AspNetCore.Rewrite.PatternSegments;

internal sealed class UrlSegment : PatternSegment
{
    private readonly UriMatchPart _uriMatchPart;

    public UrlSegment()
        : this(UriMatchPart.Path)
    {
    }

    public UrlSegment(UriMatchPart uriMatchPart)
    {
        _uriMatchPart = uriMatchPart;
    }

    public override string? Evaluate(RewriteContext context, BackReferenceCollection? ruleBackReferences, BackReferenceCollection? conditionBackReferences)
    {
        return _uriMatchPart == UriMatchPart.Full ? context.HttpContext.Request.GetEncodedUrl() : (string)context.HttpContext.Request.Path;
    }
}
