// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal static class CacheComponentKeyResolver
{
    private static readonly char[] _separator = [','];

    internal static string ComputeKey(CacheComponent cacheComponent, HttpContext httpContext)
    {
        var sb = new StringBuilder();
        var request = httpContext.Request;

        if (cacheComponent.ChildContent is { } childContent)
        {
            sb.Append(childContent.Method.DeclaringType?.FullName)
              .Append('.')
              .Append(childContent.Method.Name);
        }

        if (cacheComponent.CacheKey is { } cacheKey)
        {
            sb.Append("||").Append(cacheKey);
        }

        if (cacheComponent.VaryBy is { } varyBy)
        {
            sb.Append("||VaryBy||").Append(varyBy);
        }

        AppendDelimitedValues(sb, "VaryByQuery", cacheComponent.VaryByQuery, name => request.Query[name]);
        AppendDelimitedValues(sb, "VaryByRoute", cacheComponent.VaryByRoute, name => request.RouteValues[name]);
        AppendDelimitedValues(sb, "VaryByHeader", cacheComponent.VaryByHeader, name => request.Headers[name]);
        AppendDelimitedValues(sb, "VaryByCookie", cacheComponent.VaryByCookie, name => request.Cookies[name]);

        if (cacheComponent.VaryByUser is true)
        {
            sb.Append("||VaryByUser||").Append(httpContext.User.Identity?.Name);
        }

        if (cacheComponent.VaryByCulture is true)
        {
            sb.Append("||VaryByCulture||")
              .Append(CultureInfo.CurrentCulture.Name)
              .Append("||")
              .Append(CultureInfo.CurrentUICulture.Name);
        }

        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
    }

    private static void AppendDelimitedValues(
        StringBuilder sb,
        string collectionName,
        string? commaSeparated,
        Func<string, object?> valueAccessor)
    {
        if (commaSeparated is null)
        {
            return;
        }

        var names = commaSeparated.Split(_separator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (names.Length == 0)
        {
            return;
        }

        sb.Append("||").Append(collectionName).Append('(');

        for (var i = 0; i < names.Length; i++)
        {
            if (i > 0)
            {
                sb.Append("||");
            }

            sb.Append(names[i]).Append("||").Append(valueAccessor(names[i]));
        }

        sb.Append(')');
    }
}
