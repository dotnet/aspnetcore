// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Internal;

internal static class SharedUrlHelper
{
    [return: NotNullIfNotNull("contentPath")]
    internal static string? Content(HttpContext httpContext, string? contentPath)
    {
        if (string.IsNullOrEmpty(contentPath))
        {
            return null;
        }
        else if (contentPath[0] == '~')
        {
            var segment = new PathString(contentPath.Substring(1));
            var applicationPath = httpContext.Request.PathBase;

            var path = applicationPath.Add(segment);
            Debug.Assert(path.HasValue);
            return path.Value;
        }

        return contentPath;
    }

    internal static bool IsLocalUrl([NotNullWhen(true)] string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        var urlSpan = url.AsSpan();

        // Allows "/" or "/foo" but not "//" or "/\".
        if (urlSpan.StartsWith('/'))
        {
            // url is exactly "/"
            if (urlSpan.Length < 2)
            {
                return true;
            }

            // url doesn't start with "//" or "/\"
            if (urlSpan[1] is not '/' and '\\')
            {
                return !HasControlCharacter(urlSpan.Slice(1));
            }

            return false;
        }

        // Allows "~/" or "~/foo" but not "~//" or "~/\".
        if (urlSpan.StartsWith("~/"))
        {
            // url is exactly "~/"
            if (urlSpan.Length < 3)
            {
                return true;
            }

            // url doesn't start with "~//" or "~/\"
            if (urlSpan[2] is not '/' and '\\')
            {
                return !HasControlCharacter(urlSpan.Slice(2));
            }
        }

        return false;

        static bool HasControlCharacter(ReadOnlySpan<char> readOnlySpan)
        {
            // URLs may not contain ASCII control characters.
            for (var i = 0; i < readOnlySpan.Length; i++)
            {
                if (char.IsControl(readOnlySpan[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
