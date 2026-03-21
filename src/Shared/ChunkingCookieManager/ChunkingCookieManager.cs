// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

// Keep the type public for Security repo as it would be a breaking change to change the accessor now.
// Make this type internal for other repos as it could be used by multiple projects and having it public causes type conflicts.
#if SECURITY
namespace Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>
/// This handles cookies that are limited by per cookie length. It breaks down long cookies for responses, and reassembles them
/// from requests.
/// </summary>
public class ChunkingCookieManager : ICookieManager
{
#else
namespace Microsoft.AspNetCore.Internal;

/// <summary>
/// This handles cookies that are limited by per cookie length. It breaks down long cookies for responses, and reassembles them
/// from requests.
/// </summary>
internal sealed class ChunkingCookieManager
{
#endif
    /// <summary>
    /// The default maximum size of characters in a cookie to send back to the client.
    /// </summary>
    public const int DefaultChunkSize = 4050;

    private const string ChunkKeySuffix = "C";
    private const string ChunkCountPrefix = "chunks-";

    /// <summary>
    /// Initializes a new instance of <see cref="ChunkingCookieManager"/>.
    /// </summary>
    public ChunkingCookieManager()
    {
        // Lowest common denominator. Safari has the lowest known limit (4093), and we leave little extra just in case.
        // See http://browsercookielimits.x64.me/.
        // Leave at least 40 in case CookiePolicy tries to add 'secure', 'samesite=strict' and/or 'httponly'.
        ChunkSize = DefaultChunkSize;
    }

    /// <summary>
    /// The maximum size of cookie to send back to the client. If a cookie exceeds this size it will be broken down into multiple
    /// cookies. Set this value to null to disable this behavior. The default is 4050 characters, which is supported by all
    /// common browsers.
    ///
    /// Note that browsers may also have limits on the total size of all cookies per domain, and on the number of cookies per domain.
    /// </summary>
    public int? ChunkSize { get; set; }

    /// <summary>
    /// Throw if not all chunks of a cookie are available on a request for re-assembly.
    /// </summary>
    public bool ThrowForPartialCookies { get; set; }

    // Parse the "chunks-XX" to determine how many chunks there should be.
    private static int ParseChunksCount(string? value)
    {
        if (value != null && value.StartsWith(ChunkCountPrefix, StringComparison.Ordinal))
        {
            if (int.TryParse(value.AsSpan(ChunkCountPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var chunksCount))
            {
                return chunksCount;
            }
        }
        return 0;
    }

    /// <summary>
    /// Get the reassembled cookie. Non chunked cookies are returned normally.
    /// Cookies with missing chunks just have their "chunks-XX" header returned.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="key"></param>
    /// <returns>The reassembled cookie, if any, or null.</returns>
    public string? GetRequestCookie(HttpContext context, string key)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(key);

        var requestCookies = context.Request.Cookies;
        var value = requestCookies[key];
        var chunksCount = ParseChunksCount(value);
        if (chunksCount > 0)
        {
            var chunks = new List<string>(10); // The client may not have sent all of the chunks, don't allocate based on chunksCount.
            for (var chunkId = 1; chunkId <= chunksCount; chunkId++)
            {
                var chunk = requestCookies[key + ChunkKeySuffix + chunkId.ToString(CultureInfo.InvariantCulture)];
                if (string.IsNullOrEmpty(chunk))
                {
                    if (ThrowForPartialCookies)
                    {
                        var totalSize = 0;
                        for (int i = 0; i < chunkId - 1; i++)
                        {
                            totalSize += chunks[i].Length;
                        }
                        throw new FormatException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                "The chunked cookie is incomplete. Only {0} of the expected {1} chunks were found, totaling {2} characters. A client size limit may have been exceeded.",
                                chunkId - 1,
                                chunksCount,
                                totalSize));
                    }
                    // Missing chunk, abort by returning the original cookie value. It may have been a false positive?
                    return value;
                }

                chunks.Add(chunk);
            }

            return string.Join(string.Empty, chunks);
        }
        return value;
    }

    /// <summary>
    /// Appends a new response cookie to the Set-Cookie header. If the cookie is larger than the given size limit
    /// then it will be broken down into multiple cookies as follows:
    /// Set-Cookie: CookieName=chunks-3; path=/
    /// Set-Cookie: CookieNameC1=Segment1; path=/
    /// Set-Cookie: CookieNameC2=Segment2; path=/
    /// Set-Cookie: CookieNameC3=Segment3; path=/
    /// </summary>
    /// <param name="context"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public void AppendResponseCookie(HttpContext context, string key, string? value, CookieOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(options);

        var responseCookies = context.Response.Cookies;

        if (string.IsNullOrEmpty(value))
        {
            responseCookies.Append(key, string.Empty, options);
            return;
        }

        var templateLength = options.CreateCookieHeader(key, string.Empty).ToString().Length;

        var requestCookies = context.Request.Cookies;
        var requestCookie = requestCookies[key];
        var requestChunks = ParseChunksCount(requestCookie);

        // Normal cookie
        if (!ChunkSize.HasValue || ChunkSize.Value > templateLength + value.Length)
        {
            if (requestChunks > 0)
            {
                // If the cookie was previously chunked but no longer is, delete the chunks.
                DeleteChunks(context, requestCookies, options, key, startChunk: 1, endChunk: requestChunks);
            }

            responseCookies.Append(key, value, options);
        }
        else if (ChunkSize.Value < templateLength + 10)
        {
            // 10 is the minimum data we want to put in an individual cookie, including the cookie chunk identifier "CXX".
            // No room for data, we can't chunk the options and name
            throw new InvalidOperationException("The cookie key and options are larger than ChunksSize, leaving no room for data.");
        }
        else
        {
            // Break the cookie down into multiple cookies.
            // Key = CookieName, value = "Segment1Segment2Segment2"
            // Set-Cookie: CookieName=chunks-3; path=/
            // Set-Cookie: CookieNameC1="Segment1"; path=/
            // Set-Cookie: CookieNameC2="Segment2"; path=/
            // Set-Cookie: CookieNameC3="Segment3"; path=/
            var dataSizePerCookie = ChunkSize.Value - templateLength - 3; // Budget 3 chars for the chunkid.
            var cookieChunkCount = (int)Math.Ceiling(value.Length * 1.0 / dataSizePerCookie);

            if (requestChunks > cookieChunkCount)
            {
                // If the cookie was previously chunked but is now smaller, delete the chunks.
                DeleteChunks(context, requestCookies, options, key, startChunk: cookieChunkCount + 1, endChunk: requestChunks);
            }

            var keyValuePairs = new KeyValuePair<string, string>[cookieChunkCount + 1];
            keyValuePairs[0] = KeyValuePair.Create(key, ChunkCountPrefix + cookieChunkCount.ToString(CultureInfo.InvariantCulture));

            var offset = 0;
            for (var chunkId = 1; chunkId <= cookieChunkCount; chunkId++)
            {
                var remainingLength = value.Length - offset;
                var length = Math.Min(dataSizePerCookie, remainingLength);
                var segment = value.Substring(offset, length);
                offset += length;
                keyValuePairs[chunkId] = KeyValuePair.Create(string.Concat(key, ChunkKeySuffix, chunkId.ToString(CultureInfo.InvariantCulture)), segment);
            }

            responseCookies.Append(keyValuePairs, options);
        }
    }

    /// <summary>
    /// Deletes the cookie with the given key by setting an expired state. If a matching chunked cookie exists on
    /// the request, delete each chunk.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="key"></param>
    /// <param name="options"></param>
#pragma warning disable CA1822 // Mark members as static - Shared src file
    public void DeleteCookie(HttpContext context, string key, CookieOptions options)
#pragma warning restore CA1822 // Mark members as static
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(options);

        var keys = new List<string>
        {
            key + "="
        };

        var requestCookies = context.Request.Cookies;
        var requestCookie = requestCookies[key];
        long chunks = ParseChunksCount(requestCookie);
        if (chunks > 0)
        {
            for (var i = 1; i <= chunks + 1; i++)
            {
                var subkey = key + ChunkKeySuffix + i.ToString(CultureInfo.InvariantCulture);

                // Only delete cookies we received. We received the chunk count cookie so we should have received the others too.
                if (string.IsNullOrEmpty(requestCookies[subkey]))
                {
                    chunks = i - 1;
                    break;
                }

                keys.Add(subkey + "=");
            }
        }

        var domainHasValue = !string.IsNullOrEmpty(options.Domain);
        var pathHasValue = !string.IsNullOrEmpty(options.Path);

        Func<string, bool> rejectPredicate;
        Func<string, bool> predicate = value => keys.Any(k => value.StartsWith(k, StringComparison.OrdinalIgnoreCase));
        if (domainHasValue)
        {
            rejectPredicate = value => predicate(value) && value.Contains("domain=" + options.Domain, StringComparison.OrdinalIgnoreCase);
        }
        else if (pathHasValue)
        {
            rejectPredicate = value => predicate(value) && value.Contains("path=" + options.Path, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            rejectPredicate = value => predicate(value);
        }

        var responseHeaders = context.Response.Headers;
        var existingValues = responseHeaders.SetCookie;

        if (!StringValues.IsNullOrEmpty(existingValues))
        {
            var values = existingValues.ToArray();
            var newValues = new List<string>();

            for (var i = 0; i < values.Length; i++)
            {
                var value = values[i]!;
                if (!rejectPredicate(value))
                {
                    newValues.Add(value);
                }
            }

            responseHeaders.SetCookie = new StringValues(newValues.ToArray());
        }

        var responseCookies = context.Response.Cookies;

        var keyValuePairs = new KeyValuePair<string, string>[chunks + 1];
        keyValuePairs[0] = KeyValuePair.Create(key, string.Empty);

        for (var i = 1; i <= chunks; i++)
        {
            keyValuePairs[i] = KeyValuePair.Create(string.Concat(key, "C", i.ToString(CultureInfo.InvariantCulture)), string.Empty);
        }

        responseCookies.Append(keyValuePairs, new CookieOptions(options)
        {
            Expires = DateTimeOffset.UnixEpoch,
            MaxAge = null, // Some browsers require this (https://github.com/dotnet/aspnetcore/issues/52159)
        });
    }

    // Deletes unneeded cookie chunks, but not the primary cookie.
    private static void DeleteChunks(HttpContext context, IRequestCookieCollection requestCookies, CookieOptions options, string key, int startChunk, int endChunk)
    {
        // Don't pre-allocate, we don't trust the input.
        var keyValuePairs = new List<KeyValuePair<string, string>>();

        for (var i = startChunk; i <= endChunk; i++)
        {
            var subkey = key + ChunkKeySuffix + i.ToString(CultureInfo.InvariantCulture);

            // Only delete cookies we received. We received the chunk count cookie so we should have received the others too.
            if (string.IsNullOrEmpty(requestCookies[subkey]))
            {
                break;
            }

            keyValuePairs.Add(KeyValuePair.Create(subkey, string.Empty));
        }

        if (keyValuePairs.Count > 0)
        {
            context.Response.Cookies.Append(keyValuePairs.ToArray(), new CookieOptions(options)
            {
                Expires = DateTimeOffset.UnixEpoch,
                MaxAge = null, // Some browsers require this (https://github.com/dotnet/aspnetcore/issues/52159)
            });
        }
    }
}

