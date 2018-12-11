// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.StaticFiles
{
    internal static class Helpers
    {
        internal static IFileProvider ResolveFileProvider(IHostingEnvironment hostingEnv)
        {
            if (hostingEnv.WebRootFileProvider == null) {
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
