// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Infrastructure;

namespace Microsoft.AspNet.PipelineCore.Collections
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
                throw new ArgumentNullException("headers");
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
            Headers.AppendValues(Constants.Headers.SetCookie, Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value) + "; path=/");
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
                throw new ArgumentNullException("options");
            }

            bool domainHasValue = !string.IsNullOrEmpty(options.Domain);
            bool pathHasValue = !string.IsNullOrEmpty(options.Path);
            bool expiresHasValue = options.Expires.HasValue;

            string setCookieValue = string.Concat(
                Uri.EscapeDataString(key),
                "=",
                Uri.EscapeDataString(value ?? string.Empty),
                !domainHasValue ? null : "; domain=",
                !domainHasValue ? null : options.Domain,
                !pathHasValue ? null : "; path=",
                !pathHasValue ? null : options.Path,
                !expiresHasValue ? null : "; expires=",
                !expiresHasValue ? null : options.Expires.Value.ToString("ddd, dd-MMM-yyyy HH:mm:ss ", CultureInfo.InvariantCulture) + "GMT",
                !options.Secure ? null : "; secure",
                !options.HttpOnly ? null : "; HttpOnly");
            Headers.AppendValues("Set-Cookie", setCookieValue);
        }

        /// <summary>
        /// Sets an expired cookie
        /// </summary>
        /// <param name="key"></param>
        public void Delete(string key)
        {
            Func<string, bool> predicate = value => value.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase);

            var deleteCookies = new[] { Uri.EscapeDataString(key) + "=; expires=Thu, 01-Jan-1970 00:00:00 GMT" };
            IList<string> existingValues = Headers.GetValues(Constants.Headers.SetCookie);
            if (existingValues == null || existingValues.Count == 0)
            {
                Headers.SetValues(Constants.Headers.SetCookie, deleteCookies);
            }
            else
            {
                Headers.SetValues(Constants.Headers.SetCookie, existingValues.Where(value => !predicate(value)).Concat(deleteCookies).ToArray());
            }
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
                throw new ArgumentNullException("options");
            }

            bool domainHasValue = !string.IsNullOrEmpty(options.Domain);
            bool pathHasValue = !string.IsNullOrEmpty(options.Path);

            Func<string, bool> rejectPredicate;
            if (domainHasValue)
            {
                rejectPredicate = value =>
                    value.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase) &&
                        value.IndexOf("domain=" + options.Domain, StringComparison.OrdinalIgnoreCase) != -1;
            }
            else if (pathHasValue)
            {
                rejectPredicate = value =>
                    value.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase) &&
                        value.IndexOf("path=" + options.Path, StringComparison.OrdinalIgnoreCase) != -1;
            }
            else
            {
                rejectPredicate = value => value.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase);
            }

            IList<string> existingValues = Headers.GetValues(Constants.Headers.SetCookie);
            if (existingValues != null)
            {
                Headers.SetValues(Constants.Headers.SetCookie, existingValues.Where(value => !rejectPredicate(value)).ToArray());
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
