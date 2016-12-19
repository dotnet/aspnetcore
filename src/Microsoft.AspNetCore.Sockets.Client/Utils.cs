// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Sockets.Client
{
    internal static class Utils
    {
        public static Uri AppendPath(Uri url, string path)
        {
            var builder = new UriBuilder(url);
            if (!builder.Path.EndsWith("/"))
            {
                builder.Path += "/";
            }
            builder.Path += path;
            return builder.Uri;
        }

        internal static Uri AppendQueryString(Uri url, string qs)
        {
            if (string.IsNullOrEmpty(qs))
            {
                return url;
            }

            var builder = new UriBuilder(url);
            if (!string.IsNullOrEmpty(builder.Query))
            {
                builder.Query += "&";
            }
            builder.Query += qs;
            return builder.Uri;
        }
    }
}
