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
            AppendLengthPrefixedString(hash, cacheBoundary.CacheKey);
        }

        var request = httpContext.Request;
        if (cacheBoundary.VaryBy is { } varyBy)
        {
            AppendString(hash, "||VaryBy||");
            AppendLengthPrefixedString(hash, varyBy);
        }

        if (!string.IsNullOrEmpty(cacheBoundary.VaryByQuery))
        {
            AppendDelimitedValues(hash, "VaryByQuery", cacheBoundary.VaryByQuery, name => (string?)request.Query[name]);
        }

        if (!string.IsNullOrEmpty(cacheBoundary.VaryByRoute))
        {
            AppendDelimitedValues(hash, "VaryByRoute", cacheBoundary.VaryByRoute, name => request.RouteValues[name]?.ToString());
        }

        if (!string.IsNullOrEmpty(cacheBoundary.VaryByHeader))
        {
            AppendDelimitedValues(hash, "VaryByHeader", cacheBoundary.VaryByHeader, name => (string?)request.Headers[name]);
        }

        if (!string.IsNullOrEmpty(cacheBoundary.VaryByCookie))
        {
            AppendDelimitedValues(hash, "VaryByCookie", cacheBoundary.VaryByCookie, name => request.Cookies[name]);
        }

        if (cacheBoundary.VaryByUser is true)
        {
            AppendString(hash, "||VaryByUser||");
            AppendLengthPrefixedString(hash, httpContext.User.Identity?.Name ?? "");
        }

        if (cacheBoundary.VaryByCulture is true)
        {
            AppendString(hash, "||VaryByCulture||");
            AppendLengthPrefixedString(hash, CultureInfo.CurrentCulture.Name);
            AppendLengthPrefixedString(hash, CultureInfo.CurrentUICulture.Name);
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
            var value = valueAccessor(names[i]);
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }
            AppendString(hash, "||");
            AppendString(hash, names[i]);
            AppendString(hash, "||");
            AppendLengthPrefixedString(hash, value);
        }

        AppendString(hash, ")");
    }

    private static void AppendLengthPrefixedString(IncrementalHash hash, string value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        Span<byte> lengthPrefix = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(lengthPrefix, byteCount);
        hash.AppendData(lengthPrefix);
        hash.AppendData(Encoding.UTF8.GetBytes(value));
    }

    private static void AppendString(IncrementalHash hash, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            hash.AppendData("\0"u8);
        }
        else
        {
            hash.AppendData(Encoding.UTF8.GetBytes(value));
        }
    }
}
