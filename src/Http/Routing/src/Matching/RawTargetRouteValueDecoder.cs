// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing.Matching;

internal static class RawTargetRouteValueDecoder
{
    public static bool TryGetPathTokenizer(HttpContext httpContext, out PathTokenizer tokenizer, out int segmentOffset)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        tokenizer = default;
        segmentOffset = 0;

        var rawTarget = httpContext.Features.Get<IHttpRequestFeature>()?.RawTarget;
        if (string.IsNullOrEmpty(rawTarget) || rawTarget[0] != '/')
        {
            return false;
        }

        var queryIndex = rawTarget.IndexOf('?');
        var rawPath = queryIndex >= 0 ? rawTarget[..queryIndex] : rawTarget;
        if (!rawPath.Contains('%'))
        {
            return false;
        }

        tokenizer = new PathTokenizer(new PathString(rawPath));
        if (httpContext.Request.PathBase.HasValue)
        {
            // RawTarget still contains the original path base, so skip those segments
            // when aligning raw segments with the matched route path.
            segmentOffset = new PathTokenizer(httpContext.Request.PathBase).Count;
        }

        return true;
    }

    public static string Decode(StringSegment segment)
    {
        var value = segment.AsSpan();
        if (!value.Contains('%'))
        {
            return segment.ToString();
        }

        return Uri.UnescapeDataString(segment.ToString());
    }
}
