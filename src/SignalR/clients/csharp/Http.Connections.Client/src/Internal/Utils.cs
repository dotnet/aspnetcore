// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal;

internal static class Utils
{
    public static Uri AppendPath(Uri url, string path)
    {
        var builder = new UriBuilder(url);
        if (!builder.Path.EndsWith("/", StringComparison.Ordinal))
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
        var newQueryString = builder.Query;
        if (!string.IsNullOrEmpty(builder.Query))
        {
            newQueryString += "&";
        }
        newQueryString += qs;

        if (newQueryString.Length > 0 && newQueryString[0] == '?')
        {
            newQueryString = newQueryString.Substring(1);
        }

        builder.Query = newQueryString;
        return builder.Uri;
    }
}
