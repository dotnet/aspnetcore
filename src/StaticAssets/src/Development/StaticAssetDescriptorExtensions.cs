// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.StaticAssets;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Builder;

internal static class StaticAssetDescriptorExtensions
{
    internal static long GetContentLength(this StaticAssetDescriptor descriptor)
    {
        foreach (var header in descriptor.ResponseHeaders)
        {
            if (string.Equals(header.Name, HeaderNames.ContentLength, StringComparison.OrdinalIgnoreCase))
            {
                return long.Parse(header.Value, CultureInfo.InvariantCulture);
            }
        }

        throw new InvalidOperationException("Content-Length header not found.");
    }

    internal static DateTimeOffset GetLastModified(this StaticAssetDescriptor descriptor)
    {
        foreach (var header in descriptor.ResponseHeaders)
        {
            if (string.Equals(header.Name, HeaderNames.LastModified, StringComparison.OrdinalIgnoreCase))
            {
                return DateTimeOffset.Parse(header.Value, CultureInfo.InvariantCulture);
            }
        }

        throw new InvalidOperationException("Last-Modified header not found.");
    }

    internal static EntityTagHeaderValue GetWeakETag(this StaticAssetDescriptor descriptor)
    {
        foreach (var header in descriptor.ResponseHeaders)
        {
            if (string.Equals(header.Name, HeaderNames.ETag, StringComparison.OrdinalIgnoreCase))
            {
                var eTag = EntityTagHeaderValue.Parse(header.Value);
                if (eTag.IsWeak)
                {
                    return eTag;
                }
            }
        }

        throw new InvalidOperationException("ETag header not found.");
    }

    internal static bool HasContentEncoding(this StaticAssetDescriptor descriptor)
    {
        foreach (var selector in descriptor.Selectors)
        {
            if (string.Equals(selector.Name, HeaderNames.ContentEncoding, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool HasETag(this StaticAssetDescriptor descriptor, string tag)
    {
        foreach (var header in descriptor.ResponseHeaders)
        {
            if (string.Equals(header.Name, HeaderNames.ETag, StringComparison.OrdinalIgnoreCase))
            {
                var eTag = EntityTagHeaderValue.Parse(header.Value);
                if (!eTag.IsWeak && eTag.Tag == tag)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
