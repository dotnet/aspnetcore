// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
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
        public ResponseCookies(IHeaderDictionary headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

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
                    UrlEncoder.Default.Encode(key),
                    UrlEncoder.Default.Encode(value))
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
        public void Append(string key, string value, CookieOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var setCookieHeaderValue = new SetCookieHeaderValue(
                    UrlEncoder.Default.Encode(key),
                    UrlEncoder.Default.Encode(value))
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
            Delete(key, new CookieOptions() { Path = "/" });
        }

        /// <summary>
        /// Sets an expired cookie
        /// </summary>
        /// <param name="key"></param>
        /// <param name="options"></param>
        public void Delete(string key, CookieOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            
            var encodedKeyPlusEquals = UrlEncoder.Default.Encode(key) + "=";
            bool domainHasValue = !string.IsNullOrEmpty(options.Domain);
            bool pathHasValue = !string.IsNullOrEmpty(options.Path);

            Func<string, string, CookieOptions, bool> rejectPredicate;
            if (domainHasValue)
            {
                rejectPredicate = (value, encKeyPlusEquals, opts) =>
                    value.StartsWith(encKeyPlusEquals, StringComparison.OrdinalIgnoreCase) &&
                        value.IndexOf($"domain={opts.Domain}", StringComparison.OrdinalIgnoreCase) != -1;
            }
            else if (pathHasValue)
            {
                rejectPredicate = (value, encKeyPlusEquals, opts) =>
                    value.StartsWith(encKeyPlusEquals, StringComparison.OrdinalIgnoreCase) &&
                        value.IndexOf($"path={opts.Path}", StringComparison.OrdinalIgnoreCase) != -1;
            }
            else
            {
                rejectPredicate = (value, encKeyPlusEquals, opts) => value.StartsWith(encKeyPlusEquals, StringComparison.OrdinalIgnoreCase);
            }

            var existingValues = Headers[HeaderNames.SetCookie];
            if (!StringValues.IsNullOrEmpty(existingValues))
            {
                var values = existingValues.ToArray();
                var newValues = new List<string>();

                for (var i = 0; i < values.Length; i++)
                {
                    if (!rejectPredicate(values[i], encodedKeyPlusEquals, options))
                    {
                        newValues.Add(values[i]);
                    }
                }
                
                Headers[HeaderNames.SetCookie] = new StringValues(newValues.ToArray());
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
