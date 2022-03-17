// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.StaticFiles;

internal static class Helpers
{
    internal static IFileProvider ResolveFileProvider(IWebHostEnvironment hostingEnv)
    {
        if (hostingEnv.WebRootFileProvider == null)
        {
            throw new InvalidOperationException("Missing FileProvider.");
        }
        return hostingEnv.WebRootFileProvider;
    }

    internal static bool IsGetOrHeadMethod(string method)
    {
        return HttpMethods.IsGet(method) || HttpMethods.IsHead(method);
    }

    internal static bool PathEndsInSlash(PathString path)
    {
        return path.HasValue && path.Value!.EndsWith("/", StringComparison.Ordinal);
    }

    internal static string GetPathValueWithSlash(PathString path)
    {
        if (!PathEndsInSlash(path))
        {
            return path.Value + "/";
        }
        return path.Value!;
    }

    internal static void RedirectToPathWithSlash(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
        var request = context.Request;
        var redirect = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase, request.Path + "/", request.QueryString);
        context.Response.Headers.Location = redirect;
    }

    internal static bool TryMatchPath(HttpContext context, PathString matchUrl, bool forDirectory, out PathString subpath)
    {
        var path = context.Request.Path;

        if (forDirectory && !PathEndsInSlash(path))
        {
            path += new PathString("/");
        }

        if (path.StartsWithSegments(matchUrl, out subpath))
        {
            return true;
        }
        return false;
    }
}
