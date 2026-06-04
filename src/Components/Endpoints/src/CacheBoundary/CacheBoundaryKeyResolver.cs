// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal static class CacheBoundaryKeyResolver
{

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
            AppendDelimitedQueryValues(hash, cacheBoundary.VaryByQuery, request);
        }

        if (!string.IsNullOrEmpty(cacheBoundary.VaryByRoute))
        {
            AppendDelimitedRouteValues(hash, cacheBoundary.VaryByRoute, request);
        }

        if (!string.IsNullOrEmpty(cacheBoundary.VaryByHeader))
        {
            AppendDelimitedHeaderValues(hash, cacheBoundary.VaryByHeader, request);
        }

        if (!string.IsNullOrEmpty(cacheBoundary.VaryByCookie))
        {
            AppendDelimitedCookieValues(hash, cacheBoundary.VaryByCookie, request);
        }

        if (cacheBoundary.VaryByUser is true)
        {
            AppendString(hash, "||VaryByUser||");
            AppendUserIdentity(hash, httpContext.User);
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

    private static void AppendDelimitedQueryValues(IncrementalHash hash, string separatedValues, HttpRequest request)
    {
        AppendString(hash, "||");
        AppendString(hash, "VaryByQuery");
        AppendString(hash, "(");

        foreach (var segment in separatedValues.AsSpan().Split(','))
        {
            var name = separatedValues.AsSpan()[segment].Trim();
            if (name.IsEmpty)
            {
                continue;
            }
            var nameString = name.ToString();
            var value = (string?)request.Query[nameString];
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }
            AppendString(hash, "||");
            AppendString(hash, nameString);
            AppendString(hash, "||");
            AppendLengthPrefixedString(hash, value);
        }

        AppendString(hash, ")");
    }

    private static void AppendDelimitedRouteValues(IncrementalHash hash, string separatedValues, HttpRequest request)
    {
        AppendString(hash, "||");
        AppendString(hash, "VaryByRoute");
        AppendString(hash, "(");

        foreach (var segment in separatedValues.AsSpan().Split(','))
        {
            var name = separatedValues.AsSpan()[segment].Trim();
            if (name.IsEmpty)
            {
                continue;
            }
            var nameString = name.ToString();
            var value = request.RouteValues[nameString]?.ToString();
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }
            AppendString(hash, "||");
            AppendString(hash, nameString);
            AppendString(hash, "||");
            AppendLengthPrefixedString(hash, value);
        }

        AppendString(hash, ")");
    }

    private static void AppendDelimitedHeaderValues(IncrementalHash hash, string separatedValues, HttpRequest request)
    {
        AppendString(hash, "||");
        AppendString(hash, "VaryByHeader");
        AppendString(hash, "(");

        foreach (var segment in separatedValues.AsSpan().Split(','))
        {
            var name = separatedValues.AsSpan()[segment].Trim();
            if (name.IsEmpty)
            {
                continue;
            }
            var nameString = name.ToString();
            var value = (string?)request.Headers[nameString];
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }
            AppendString(hash, "||");
            AppendString(hash, nameString);
            AppendString(hash, "||");
            AppendLengthPrefixedString(hash, value);
        }

        AppendString(hash, ")");
    }

    private static void AppendDelimitedCookieValues(IncrementalHash hash, string separatedValues, HttpRequest request)
    {
        AppendString(hash, "||");
        AppendString(hash, "VaryByCookie");
        AppendString(hash, "(");

        foreach (var segment in separatedValues.AsSpan().Split(','))
        {
            var name = separatedValues.AsSpan()[segment].Trim();
            if (name.IsEmpty)
            {
                continue;
            }
            var nameString = name.ToString();
            var value = request.Cookies[nameString];
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }
            AppendString(hash, "||");
            AppendString(hash, nameString);
            AppendString(hash, "||");
            AppendLengthPrefixedString(hash, value);
        }

        AppendString(hash, ")");
    }

    private static void AppendUserIdentity(IncrementalHash hash, ClaimsPrincipal user)
    {
        var identity = user.Identity;
        var isAuthenticated = identity?.IsAuthenticated == true;

        AppendLengthPrefixedString(hash, isAuthenticated ? "1" : "0");
        AppendLengthPrefixedString(hash, identity?.AuthenticationType ?? "");

        if (!isAuthenticated)
        {
            // Anonymous: nothing more to mix in.
            AppendLengthPrefixedString(hash, "anonymous");
            return;
        }

        var nameIdentifier = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (nameIdentifier is not null)
        {
            AppendLengthPrefixedString(hash, "nameid");
            AppendLengthPrefixedString(hash, nameIdentifier);
            return;
        }

        AppendLengthPrefixedString(hash, "claims");
        AppendLengthPrefixedString(hash, identity?.Name ?? "");
        foreach (var claim in user.Claims)
        {
            AppendLengthPrefixedString(hash, claim.Type);
            AppendLengthPrefixedString(hash, claim.Value);
            AppendLengthPrefixedString(hash, claim.Issuer);
        }
    }

    private static void AppendLengthPrefixedString(IncrementalHash hash, string value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        Span<byte> lengthPrefix = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(lengthPrefix, byteCount);
        hash.AppendData(lengthPrefix);
        AppendString(hash, value);
    }

    private static void AppendString(IncrementalHash hash, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            hash.AppendData("\0"u8);
            return;
        }

        var byteCount = Encoding.UTF8.GetByteCount(value);
        const int stackAllocThreshold = 256;
        byte[]? rented = null;
        var buffer = byteCount <= stackAllocThreshold
            ? stackalloc byte[stackAllocThreshold]
            : (rented = System.Buffers.ArrayPool<byte>.Shared.Rent(byteCount));

        var written = Encoding.UTF8.GetBytes(value, buffer);
        hash.AppendData(buffer[..written]);

        if (rented is not null)
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(rented);
        }
    }
}
