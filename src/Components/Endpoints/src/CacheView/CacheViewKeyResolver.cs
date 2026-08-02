// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal static class CacheViewKeyResolver
{
    private static readonly ConcurrentDictionary<string, string[]> _sortedNamesByRawValue = new(StringComparer.Ordinal);

    static CacheViewKeyResolver()
    {
        if (HotReloadManager.IsSupported)
        {
            HotReloadManager.Default.OnDeltaApplied += _sortedNamesByRawValue.Clear;
        }
    }

    internal static string ComputeKey(CacheView cacheView, HttpContext httpContext)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        AppendLengthPrefixedString(hash, cacheView.TreePositionKey ?? "");

        if (cacheView.CacheKey is not null)
        {
            AppendString(hash, "||CacheKey||");
            AppendLengthPrefixedString(hash, cacheView.CacheKey);
        }

        var request = httpContext.Request;
        if (cacheView.VaryBy is { } varyBy)
        {
            AppendString(hash, "||VaryBy||");
            AppendLengthPrefixedString(hash, varyBy);
        }

        if (!string.IsNullOrEmpty(cacheView.VaryByQuery))
        {
            if (cacheView.VaryByQuery.Trim() is "*")
            {
                AppendAllQueryValues(hash, request);
            }
            else
            {
                AppendDelimitedQueryValues(hash, cacheView.VaryByQuery, request);
            }
        }

        if (!string.IsNullOrEmpty(cacheView.VaryByRoute))
        {
            AppendDelimitedRouteValues(hash, cacheView.VaryByRoute, request);
        }

        if (!string.IsNullOrEmpty(cacheView.VaryByHeader))
        {
            AppendDelimitedHeaderValues(hash, cacheView.VaryByHeader, request);
        }

        if (!string.IsNullOrEmpty(cacheView.VaryByCookie))
        {
            AppendDelimitedCookieValues(hash, cacheView.VaryByCookie, request);
        }

        if (cacheView.VaryByUser)
        {
            AppendString(hash, "||VaryByUser||");
            AppendUserIdentity(hash, httpContext.User);
        }

        if (cacheView.VaryByCulture)
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

        foreach (var nameString in CollectSortedNames(separatedValues))
        {
            AppendNameStringValues(hash, nameString, request.Query[nameString]);
        }

        AppendString(hash, ")");
    }

    private static void AppendDelimitedRouteValues(IncrementalHash hash, string separatedValues, HttpRequest request)
    {
        AppendString(hash, "||");
        AppendString(hash, "VaryByRoute");
        AppendString(hash, "(");

        foreach (var nameString in CollectSortedNames(separatedValues))
        {
            var value = request.RouteValues[nameString]?.ToString();
            if (value is null)
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

        foreach (var nameString in CollectSortedNames(separatedValues))
        {
            AppendNameStringValues(hash, nameString, request.Headers[nameString]);
        }

        AppendString(hash, ")");
    }

    private static void AppendDelimitedCookieValues(IncrementalHash hash, string separatedValues, HttpRequest request)
    {
        AppendString(hash, "||");
        AppendString(hash, "VaryByCookie");
        AppendString(hash, "(");

        foreach (var nameString in CollectSortedNames(separatedValues))
        {
            var value = request.Cookies[nameString];
            if (value is null)
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

    private static string[] CollectSortedNames(string separatedValues)
        => _sortedNamesByRawValue.GetOrAdd(separatedValues, static raw =>
        {
            var names = new List<string>();
            foreach (var segment in raw.AsSpan().Split(','))
            {
                var name = raw.AsSpan()[segment].Trim();
                if (!name.IsEmpty)
                {
                    names.Add(name.ToString());
                }
            }

            names.Sort(StringComparer.Ordinal);
            return names.ToArray();
        });

    private static void AppendAllQueryValues(IncrementalHash hash, HttpRequest request)
    {
        AppendString(hash, "||");
        AppendString(hash, "VaryByQuery");
        AppendString(hash, "(*)");

        var names = new List<string>(request.Query.Count);
        foreach (var pair in request.Query)
        {
            names.Add(pair.Key);
        }

        names.Sort(StringComparer.Ordinal);

        foreach (var name in names)
        {
            AppendNameStringValues(hash, name, request.Query[name]);
        }
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

        var nameIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (nameIdClaim is not null && !string.IsNullOrEmpty(nameIdClaim.Value))
        {
            AppendLengthPrefixedString(hash, "nameid");
            AppendLengthPrefixedString(hash, nameIdClaim.Value);
            AppendLengthPrefixedString(hash, nameIdClaim.Issuer);
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

    private static void AppendNameStringValues(IncrementalHash hash, string name, StringValues values)
    {
        if (values.Count == 0)
        {
            return;
        }

        AppendString(hash, "||");
        AppendLengthPrefixedString(hash, name);
        AppendInt32(hash, values.Count);
        foreach (var value in values)
        {
            AppendLengthPrefixedString(hash, value ?? "");
        }
    }

    private static void AppendInt32(IncrementalHash hash, int value)
    {
        Span<byte> buffer = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        hash.AppendData(buffer);
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
