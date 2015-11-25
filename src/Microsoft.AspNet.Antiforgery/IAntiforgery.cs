// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Html.Abstractions;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Antiforgery
{
    /// <summary>
    /// Provides access to the antiforgery system, which provides protection against
    /// Cross-site Request Forgery (XSRF, also called CSRF) attacks.
    /// </summary>
    public interface IAntiforgery
    {
        /// <summary>
        /// Generates an &lt;input type="hidden"&gt; element for an antiforgery token.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> associated with the current request.</param>
        /// <returns>
        /// A <see cref="IHtmlContent"/> containing an &lt;input type="hidden"&gt; element. This element should be put
        /// inside a &lt;form&gt;.
        /// </returns>
        /// <remarks>
        /// This method has a side effect:
        /// A response cookie is set if there is no valid cookie associated with the request.
        /// </remarks>
        IHtmlContent GetHtml(HttpContext context);

        /// <summary>
        /// Generates an <see cref="AntiforgeryTokenSet"/> for this request and stores the cookie token
        /// in the response.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> associated with the current request.</param>
        /// <returns>An <see cref="AntiforgeryTokenSet" /> with tokens for the response.</returns>
        /// <remarks>
        /// This method has a side effect:
        /// A response cookie is set if there is no valid cookie associated with the request.
        /// </remarks>
        AntiforgeryTokenSet GetAndStoreTokens(HttpContext context);

        /// <summary>
        /// Generates an <see cref="AntiforgeryTokenSet"/> for this request.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> associated with the current request.</param>
        /// <remarks>
        /// Unlike <see cref="GetAndStoreTokens(HttpContext)"/>, this method has no side effect. The caller
        /// is responsible for setting the response cookie and injecting the returned
        /// form token as appropriate.
        /// </remarks>
        AntiforgeryTokenSet GetTokens(HttpContext context);

        /// <summary>
        /// Validates an antiforgery token that was supplied as part of the request.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> associated with the current request.</param>
        Task ValidateRequestAsync(HttpContext context);

        /// <summary>
        /// Validates an <see cref="AntiforgeryTokenSet"/> for the current request.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> associated with the current request.</param>
        /// <param name="antiforgeryTokenSet">
        /// The <see cref="AntiforgeryTokenSet"/> (cookie and form token) for this request.
        /// </param>
        void ValidateTokens(HttpContext context, AntiforgeryTokenSet antiforgeryTokenSet);

        /// <summary>
        /// Generates and stores an antiforgery cookie token if one is not available or not valid.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> associated with the current request.</param>
        void SetCookieTokenAndHeader(HttpContext context);
    }
}
