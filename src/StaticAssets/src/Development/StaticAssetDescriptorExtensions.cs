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
            if (header.Name == "Content-Length")
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
            if (header.Name == "Last-Modified")
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
            if (header.Name == "ETag")
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
            if (selector.Name == "Content-Encoding")
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
            if (header.Name == "ETag")
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
