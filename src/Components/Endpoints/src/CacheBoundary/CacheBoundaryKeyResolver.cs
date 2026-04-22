// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
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
        Span<byte> hashOutput = stackalloc byte[SHA256.HashSizeInBytes];
        byte[]? pool = null;
        try
        {
            Span<byte> buffer = stackalloc byte[1024];
            var pos = 0;

            // Tree-position key (computed at EndpointComponentState constructor time)
            AppendUtf8(ref buffer, ref pool, ref pos, cacheBoundary.TreePositionKey);

            // User-provided CacheKey parameter
            if (cacheBoundary.CacheKey is not null)
            {
                AppendUtf8(ref buffer, ref pool, ref pos, "||CacheKey||");
                AppendUtf8(ref buffer, ref pool, ref pos, cacheBoundary.CacheKey);
            }

            // VaryBy dimensions
            var request = httpContext.Request;

            if (cacheBoundary.VaryBy is { } varyBy)
            {
                AppendUtf8(ref buffer, ref pool, ref pos, "||VaryBy||");
                AppendUtf8(ref buffer, ref pool, ref pos, varyBy);
            }

            if (cacheBoundary.VaryByQuery is not null)
            {
                AppendDelimitedValues(ref buffer, ref pool, ref pos, "VaryByQuery", cacheBoundary.VaryByQuery, name => (string?)request.Query[name]);
            }

            if (cacheBoundary.VaryByRoute is not null)
            {
                AppendDelimitedValues(ref buffer, ref pool, ref pos, "VaryByRoute", cacheBoundary.VaryByRoute, name => request.RouteValues[name]?.ToString());
            }

            if (cacheBoundary.VaryByHeader is not null)
            {
                AppendDelimitedValues(ref buffer, ref pool, ref pos, "VaryByHeader", cacheBoundary.VaryByHeader, name => (string?)request.Headers[name]);
            }

            if (cacheBoundary.VaryByCookie is not null)
            {
                AppendDelimitedValues(ref buffer, ref pool, ref pos, "VaryByCookie", cacheBoundary.VaryByCookie, name => request.Cookies[name]);
            }

            if (cacheBoundary.VaryByUser is true)
            {
                AppendUtf8(ref buffer, ref pool, ref pos, "||VaryByUser||");
                AppendUtf8(ref buffer, ref pool, ref pos, httpContext.User.Identity?.Name);
            }

            if (cacheBoundary.VaryByCulture is true)
            {
                AppendUtf8(ref buffer, ref pool, ref pos, "||VaryByCulture||");
                AppendUtf8(ref buffer, ref pool, ref pos, CultureInfo.CurrentCulture.Name);
                AppendUtf8(ref buffer, ref pool, ref pos, "||");
                AppendUtf8(ref buffer, ref pool, ref pos, CultureInfo.CurrentUICulture.Name);
            }

            var hashSucceeded = SHA256.TryHashData(buffer[..pos], hashOutput, out _);
            Debug.Assert(hashSucceeded);
            return Convert.ToBase64String(hashOutput);
        }
        finally
        {
            if (pool is not null)
            {
                ArrayPool<byte>.Shared.Return(pool, clearArray: true);
            }
        }
    }

    private static void AppendUtf8(ref Span<byte> buffer, ref byte[]? pool, ref int pos, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        int written;
        while (!Encoding.UTF8.TryGetBytes(value, buffer[pos..], out written))
        {
            GrowBuffer(ref pool, ref buffer, value.Length * 4 + pos);
        }

        pos += written;
    }

    private static void AppendDelimitedValues(
        ref Span<byte> buffer, ref byte[]? pool, ref int pos,
        string collectionName, string commaSeparated, Func<string, string?> valueAccessor)
    {
        var names = commaSeparated.Split(_separator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (names.Length == 0)
        {
            return;
        }

        AppendUtf8(ref buffer, ref pool, ref pos, "||");
        AppendUtf8(ref buffer, ref pool, ref pos, collectionName);
        AppendUtf8(ref buffer, ref pool, ref pos, "(");

        for (var i = 0; i < names.Length; i++)
        {
            if (i > 0)
            {
                AppendUtf8(ref buffer, ref pool, ref pos, "||");
            }

            AppendUtf8(ref buffer, ref pool, ref pos, names[i]);
            AppendUtf8(ref buffer, ref pool, ref pos, "||");
            AppendUtf8(ref buffer, ref pool, ref pos, valueAccessor(names[i]));
        }

        AppendUtf8(ref buffer, ref pool, ref pos, ")");
    }

    private static void GrowBuffer(ref byte[]? pool, ref Span<byte> buffer, int? size = null)
    {
        var newPool = pool is null
            ? ArrayPool<byte>.Shared.Rent(size ?? 2048)
            : ArrayPool<byte>.Shared.Rent(Math.Max(size ?? pool.Length * 2, pool.Length * 2));
        buffer.CopyTo(newPool);
        if (pool is not null)
        {
            ArrayPool<byte>.Shared.Return(pool, clearArray: true);
        }

        pool = newPool;
        buffer = newPool;
    }
}
