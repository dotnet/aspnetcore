// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.StaticFiles
{
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
            return path.Value.EndsWith("/", StringComparison.Ordinal);
        }

        internal static string GetPathValueWithSlash(PathString path)
        {
            if (!PathEndsInSlash(path))
            {
                return path.Value + "/";
            }
            return path.Value;
        }

        internal static void RedirectToPathWithSlash(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            var request = context.Request;
            var redirect = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase, request.Path + "/", request.QueryString);
            context.Response.Headers[HeaderNames.Location] = redirect;
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
}
