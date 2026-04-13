// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal static class CacheComponentKeyResolver
{
    internal static string ComputeKey(CacheComponent cacheComponent, HttpContext httpContext)
    {
        var sb = new StringBuilder();

        if (cacheComponent.ChildContent is { } childContent)
        {
            sb.Append(childContent.Method.DeclaringType?.FullName)
              .Append('.')
              .Append(childContent.Method.Name);
        }
        else
        {
            sb.Append(nameof(CacheComponent));
        }

        if (cacheComponent.CacheKey is { } cacheKey)
        {
            sb.Append('.').Append(cacheKey);
        }

        AppendVaryByValues(sb, cacheComponent, httpContext);

        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
    }

    private static void AppendVaryByValues(StringBuilder sb, CacheComponent cacheComponent, HttpContext httpContext)
    {
        var request = httpContext.Request;

        if (cacheComponent.VaryByQuery is { } varyByQuery)
        {
            foreach (var name in varyByQuery.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                sb.Append('.').Append(name).Append('=').Append(request.Query[name]);
            }
        }

        if (cacheComponent.VaryByRoute is { } varyByRoute)
        {
            foreach (var name in varyByRoute.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                sb.Append('.').Append(name).Append('=').Append(request.RouteValues[name]);
            }
        }

        if (cacheComponent.VaryByHeader is { } varyByHeader)
        {
            foreach (var name in varyByHeader.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                sb.Append('.').Append(name).Append('=').Append(request.Headers[name]);
            }
        }

        if (cacheComponent.VaryByCookie is { } varyByCookie)
        {
            foreach (var name in varyByCookie.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                sb.Append('.').Append(name).Append('=').Append(request.Cookies[name]);
            }
        }

        if (cacheComponent.VaryByUser is true)
        {
            sb.Append(".user=").Append(httpContext.User.Identity?.Name);
        }

        if (cacheComponent.VaryByCulture is true)
        {
            sb.Append(".culture=").Append(CultureInfo.CurrentCulture.Name)
              .Append('.').Append(CultureInfo.CurrentUICulture.Name);
        }

        if (cacheComponent.VaryBy is { } varyBy)
        {
            sb.Append('.').Append(varyBy);
        }
    }
}
