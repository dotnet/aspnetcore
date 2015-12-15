// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Antiforgery
{
    // Provides an abstraction around how tokens are persisted and retrieved for a request
    public interface IAntiforgeryTokenStore
    {
        AntiforgeryToken GetCookieToken(HttpContext httpContext);

        /// <summary>
        /// Gets the cookie and request tokens from the request. Will throw an exception if either token is
        /// not present.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> for the current request.</param>
        /// <returns>The <see cref="AntiforgeryTokenSet"/>.</returns>
        Task<AntiforgeryTokenSet> GetRequestTokensAsync(HttpContext httpContext);

        void SaveCookieToken(HttpContext httpContext, AntiforgeryToken token);
    }
}