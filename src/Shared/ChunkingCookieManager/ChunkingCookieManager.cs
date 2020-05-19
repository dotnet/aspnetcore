// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

// Keep the type public for Security repo as it would be a breaking change to change the accessor now.
// Make this type internal for other repos as it could be used by multiple projects and having it public causes type conflicts.
#if SECURITY
namespace Microsoft.AspNetCore.Authentication.Cookies
{
    /// <summary>
    /// This handles cookies that are limited by per cookie length. It breaks down long cookies for responses, and reassembles them
    /// from requests.
    /// </summary>
    public class ChunkingCookieManager : ICookieManager
    {
#else
namespace Microsoft.AspNetCore.Internal
{
    /// <summary>
    /// This handles cookies that are limited by per cookie length. It breaks down long cookies for responses, and reassembles them
    /// from requests.
    /// </summary>
    internal class ChunkingCookieManager
    {
#endif
        /// <summary>
        /// The default maximum size of characters in a cookie to send back to the client.
        /// </summary>
        public const int DefaultChunkSize = 4050;

        private const string ChunkKeySuffix = "C";
        private const string ChunkCountPrefix = "chunks-";

        public ChunkingCookieManager()
        {
            // Lowest common denominator. Safari has the lowest known limit (4093), and we leave little extra just in case.
            // See http://browsercookielimits.x64.me/.
            // Leave at least 40 in case CookiePolicy tries to add 'secure', 'samesite=strict' and/or 'httponly'.
            ChunkSize = DefaultChunkSize;
        }

        /// <summary>
        /// The maximum size of cookie to send back to the client. If a cookie exceeds this size it will be broken down into multiple
        /// cookies. Set this value to null to disable this behavior. The default is 4090 characters, which is supported by all
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
        private static int ParseChunksCount(string value)
        {
            if (value != null && value.StartsWith(ChunkCountPrefix, StringComparison.Ordinal))
            {
                var chunksCountString = value.Substring(ChunkCountPrefix.Length);
                int chunksCount;
                if (int.TryParse(chunksCountString, NumberStyles.None, CultureInfo.InvariantCulture, out chunksCount))
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
        public string GetRequestCookie(HttpContext context, string key)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var requestCookies = context.Request.Cookies;
            var value = requestCookies[key];
            var chunksCount = ParseChunksCount(value);
            if (chunksCount > 0)
            {
                var chunks = new string[chunksCount];
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

                    chunks[chunkId - 1] = chunk;
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
        public void AppendResponseCookie(HttpContext context, string key, string value, CookieOptions options)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var template = new SetCookieHeaderValue(key)
            {
                Domain = options.Domain,
                Expires = options.Expires,
                SameSite = (Net.Http.Headers.SameSiteMode)options.SameSite,
                HttpOnly = options.HttpOnly,
                Path = options.Path,
                Secure = options.Secure,
                MaxAge = options.MaxAge,
            };

            var templateLength = template.ToString().Length;

            value = value ?? string.Empty;

            // Normal cookie
            var responseCookies = context.Response.Cookies;
            if (!ChunkSize.HasValue || ChunkSize.Value > templateLength + value.Length)
            {
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

                responseCookies.Append(key, ChunkCountPrefix + cookieChunkCount.ToString(CultureInfo.InvariantCulture), options);

                var offset = 0;
                for (var chunkId = 1; chunkId <= cookieChunkCount; chunkId++)
                {
                    var remainingLength = value.Length - offset;
                    var length = Math.Min(dataSizePerCookie, remainingLength);
                    var segment = value.Substring(offset, length);
                    offset += length;

                    responseCookies.Append(key + ChunkKeySuffix + chunkId.ToString(CultureInfo.InvariantCulture), segment, options);
                }
            }
        }

        /// <summary>
        /// Deletes the cookie with the given key by setting an expired state. If a matching chunked cookie exists on
        /// the request, delete each chunk.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key"></param>
        /// <param name="options"></param>
        public void DeleteCookie(HttpContext context, string key, CookieOptions options)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var keys = new List<string>();
            keys.Add(key + "=");

            var requestCookie = context.Request.Cookies[key];
            var chunks = ParseChunksCount(requestCookie);
            if (chunks > 0)
            {
                for (int i = 1; i <= chunks + 1; i++)
                {
                    var subkey = key + ChunkKeySuffix + i.ToString(CultureInfo.InvariantCulture);
                    keys.Add(subkey + "=");
                }
            }

            var domainHasValue = !string.IsNullOrEmpty(options.Domain);
            var pathHasValue = !string.IsNullOrEmpty(options.Path);

            Func<string, bool> rejectPredicate;
            Func<string, bool> predicate = value => keys.Any(k => value.StartsWith(k, StringComparison.OrdinalIgnoreCase));
            if (domainHasValue)
            {
                rejectPredicate = value => predicate(value) && value.IndexOf("domain=" + options.Domain, StringComparison.OrdinalIgnoreCase) != -1;
            }
            else if (pathHasValue)
            {
                rejectPredicate = value => predicate(value) && value.IndexOf("path=" + options.Path, StringComparison.OrdinalIgnoreCase) != -1;
            }
            else
            {
                rejectPredicate = value => predicate(value);
            }

            var responseHeaders = context.Response.Headers;
            var existingValues = responseHeaders[HeaderNames.SetCookie];
            if (!StringValues.IsNullOrEmpty(existingValues))
            {
                responseHeaders[HeaderNames.SetCookie] = existingValues.Where(value => !rejectPredicate(value)).ToArray();
            }

            AppendResponseCookie(
                context,
                key,
                string.Empty,
                new CookieOptions()
                {
                    Path = options.Path,
                    Domain = options.Domain,
                    SameSite = options.SameSite,
                    Secure = options.Secure,
                    IsEssential = options.IsEssential,
                    Expires = DateTimeOffset.UnixEpoch,
                    HttpOnly = options.HttpOnly,
                });

            for (int i = 1; i <= chunks; i++)
            {
                AppendResponseCookie(
                    context,
                    key + "C" + i.ToString(CultureInfo.InvariantCulture),
                    string.Empty,
                    new CookieOptions()
                    {
                        Path = options.Path,
                        Domain = options.Domain,
                        SameSite = options.SameSite,
                        Secure = options.Secure,
                        IsEssential = options.IsEssential,
                        Expires = DateTimeOffset.UnixEpoch,
                        HttpOnly = options.HttpOnly,
                    });
            }
        }
    }
}
