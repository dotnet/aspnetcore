// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal static class CacheBoundaryKeyResolver
{
    private static readonly char[] _separator = [','];

    internal static string ComputeKey(CacheBoundary cacheBoundary, HttpContext httpContext)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendString(hash, cacheBoundary.TreePositionKey);

        if (cacheBoundary.CacheKey is not null)
        {
            AppendString(hash, "||CacheKey||");
            AppendString(hash, cacheBoundary.CacheKey);
        }

        var request = httpContext.Request;
        if (cacheBoundary.VaryBy is { } varyBy)
        {
            AppendString(hash, "||VaryBy||");
            AppendString(hash, varyBy);
        }

        if (cacheBoundary.VaryByQuery is not null)
        {
            AppendDelimitedValues(hash, "VaryByQuery", cacheBoundary.VaryByQuery, name => (string?)request.Query[name]);
        }

        if (cacheBoundary.VaryByRoute is not null)
        {
            AppendDelimitedValues(hash, "VaryByRoute", cacheBoundary.VaryByRoute, name => request.RouteValues[name]?.ToString());
        }

        if (cacheBoundary.VaryByHeader is not null)
        {
            AppendDelimitedValues(hash, "VaryByHeader", cacheBoundary.VaryByHeader, name => (string?)request.Headers[name]);
        }

        if (cacheBoundary.VaryByCookie is not null)
        {
            AppendDelimitedValues(hash, "VaryByCookie", cacheBoundary.VaryByCookie, name => request.Cookies[name]);
        }

        if (cacheBoundary.VaryByUser is true)
        {
            AppendString(hash, "||VaryByUser||");
            AppendString(hash, httpContext.User.Identity?.Name);
        }

        if (cacheBoundary.VaryByCulture is true)
        {
            AppendString(hash, "||VaryByCulture||");
            AppendString(hash, CultureInfo.CurrentCulture.Name);
            AppendString(hash, "||");
            AppendString(hash, CultureInfo.CurrentUICulture.Name);
        }

        Span<byte> hashOutput = stackalloc byte[SHA256.HashSizeInBytes];
        var hashSucceeded = hash.TryGetHashAndReset(hashOutput, out _);
        Debug.Assert(hashSucceeded);
        return Convert.ToBase64String(hashOutput);
    }

    private static void AppendDelimitedValues(
        IncrementalHash hash,
        string collectionName, string separatedValues, Func<string, string?> valueAccessor)
    {
        var names = separatedValues.Split(_separator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (names.Length == 0)
        {
            return;
        }

        AppendString(hash, "||");
        AppendString(hash, collectionName);
        AppendString(hash, "(");

        for (var i = 0; i < names.Length; i++)
        {
            if (string.IsNullOrEmpty(valueAccessor(names[i])))
            {
                continue;
            }
            AppendString(hash, "||");
            AppendString(hash, names[i]);
            AppendString(hash, "||");
            AppendString(hash, valueAccessor(names[i]));
        }

        AppendString(hash, ")");
    }

    private static void AppendString(IncrementalHash hash, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(value));
        }
    }
}
