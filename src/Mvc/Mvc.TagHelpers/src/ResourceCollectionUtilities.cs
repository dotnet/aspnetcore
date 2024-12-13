// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

internal static class ResourceCollectionUtilities
{
    internal static bool TryResolveFromAssetCollection(ViewContext viewContext, string url, out string resolvedUrl)
    {
        var pathBase = viewContext.HttpContext.Request.PathBase;
        var assetCollection = viewContext.HttpContext.GetEndpoint()?.Metadata.GetMetadata<ResourceAssetCollection>();
        if (assetCollection != null)
        {
            var value = url.StartsWith('/') ? url[1..] : url;
            if (assetCollection.IsContentSpecificUrl(value))
            {
                resolvedUrl = url;
                return true;
            }

            var src = assetCollection[value];
            if (!string.Equals(src, value, StringComparison.Ordinal))
            {
                resolvedUrl = url.StartsWith('/') ? $"/{src}" : src;
                return true;
            }

            if (pathBase.HasValue && url.StartsWith(pathBase, StringComparison.OrdinalIgnoreCase))
            {
                var length = pathBase.Value.EndsWith('/') ? pathBase.Value.Length : pathBase.Value.Length + 1;
                var relativePath = url[length..];
                if (assetCollection.IsContentSpecificUrl(relativePath))
                {
                    resolvedUrl = url;
                    return true;
                }

                src = assetCollection[relativePath];
                if (!string.Equals(src, relativePath, StringComparison.Ordinal))
                {
                    if (pathBase.Value.EndsWith('/'))
                    {
                        resolvedUrl = $"{pathBase}{src}";
                        return true;
                    }
                    else
                    {
                        resolvedUrl = $"{pathBase}/{src}";
                        return true;
                    }
                }
            }
        }

        resolvedUrl = null;
        return false;
    }
}
