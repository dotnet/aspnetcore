// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Primitives;
using Microsoft.Framework.WebEncoders;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Http.Internal
{
    /// <summary>
    /// A wrapper for the response Set-Cookie header
    /// </summary>
    public class ResponseCookies : IResponseCookies
    {
        /// <summary>
        /// Create a new wrapper
        /// </summary>
        /// <param name="headers"></param>
        public ResponseCookies([NotNull] IHeaderDictionary headers)
        {
            Headers = headers;
        }

        private IHeaderDictionary Headers { get; set; }

        /// <summary>
        /// Add a new cookie and value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Append(string key, string value)
        {
            var setCookieHeaderValue = new SetCookieHeaderValue(
                    UrlEncoder.Default.UrlEncode(key),
                    UrlEncoder.Default.UrlEncode(value))
            {
                Path = "/"
            };

            Headers[HeaderNames.SetCookie] = StringValues.Concat(Headers[HeaderNames.SetCookie], setCookieHeaderValue.ToString());
        }

        /// <summary>
        /// Add a new cookie
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public void Append(string key, string value, [NotNull] CookieOptions options)
        {
            var setCookieHeaderValue = new SetCookieHeaderValue(
                    UrlEncoder.Default.UrlEncode(key),
                    UrlEncoder.Default.UrlEncode(value))
            {
                Domain = options.Domain,
                Path = options.Path,
                Expires = options.Expires,
                Secure = options.Secure,
                HttpOnly = options.HttpOnly,
            };

            Headers[HeaderNames.SetCookie] = StringValues.Concat(Headers[HeaderNames.SetCookie], setCookieHeaderValue.ToString());
        }

        /// <summary>
        /// Sets an expired cookie
        /// </summary>
        /// <param name="key"></param>
        public void Delete(string key)
        {
            var encodedKeyPlusEquals = UrlEncoder.Default.UrlEncode(key) + "=";
            Func<string, bool> predicate = value => value.StartsWith(encodedKeyPlusEquals, StringComparison.OrdinalIgnoreCase);

            StringValues deleteCookies = encodedKeyPlusEquals + "; expires=Thu, 01-Jan-1970 00:00:00 GMT";
            var existingValues = Headers[HeaderNames.SetCookie];
            if (StringValues.IsNullOrEmpty(existingValues))
            {
                Headers[HeaderNames.SetCookie] = deleteCookies;
            }
            else
            {
                Headers[HeaderNames.SetCookie] = existingValues.Where(value => !predicate(value)).Concat(deleteCookies).ToArray();
            }
        }

        /// <summary>
        /// Sets an expired cookie
        /// </summary>
        /// <param name="key"></param>
        /// <param name="options"></param>
        public void Delete(string key, [NotNull] CookieOptions options)
        {
            var encodedKeyPlusEquals = UrlEncoder.Default.UrlEncode(key) + "=";
            bool domainHasValue = !string.IsNullOrEmpty(options.Domain);
            bool pathHasValue = !string.IsNullOrEmpty(options.Path);

            Func<string, bool> rejectPredicate;
            if (domainHasValue)
            {
                rejectPredicate = value =>
                    value.StartsWith(encodedKeyPlusEquals, StringComparison.OrdinalIgnoreCase) &&
                        value.IndexOf("domain=" + options.Domain, StringComparison.OrdinalIgnoreCase) != -1;
            }
            else if (pathHasValue)
            {
                rejectPredicate = value =>
                    value.StartsWith(encodedKeyPlusEquals, StringComparison.OrdinalIgnoreCase) &&
                        value.IndexOf("path=" + options.Path, StringComparison.OrdinalIgnoreCase) != -1;
            }
            else
            {
                rejectPredicate = value => value.StartsWith(encodedKeyPlusEquals, StringComparison.OrdinalIgnoreCase);
            }

            var existingValues = Headers[HeaderNames.SetCookie];
            if (!StringValues.IsNullOrEmpty(existingValues))
            {
                Headers[HeaderNames.SetCookie] = existingValues.Where(value => !rejectPredicate(value)).ToArray();
            }

            Append(key, string.Empty, new CookieOptions
            {
                Path = options.Path,
                Domain = options.Domain,
                Expires = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            });
        }
    }
}
