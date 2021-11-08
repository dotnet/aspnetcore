// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.StaticFiles;

internal static class CacheHeaderSettings
{
    internal static void SetCacheHeaders(StaticFileResponseContext ctx)
    {
        // By setting "Cache-Control: no-cache", we're allowing the browser to store
        // a cached copy of the response, but telling it that it must check with the
        // server for modifications (based on Etag) before using that cached copy.
        // Longer term, we should generate URLs based on content hashes (at least
        // for published apps) so that the browser doesn't need to make any requests
        // for unchanged files.
        var headers = ctx.Context.Response.GetTypedHeaders();
        if (headers.CacheControl == null)
        {
            headers.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true
            };
        }
    }
}
