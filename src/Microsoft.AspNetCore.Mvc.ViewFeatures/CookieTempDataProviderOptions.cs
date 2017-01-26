// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Provides programmatic configuration for cookies set by <see cref="CookieTempDataProvider"/>
    /// </summary>
    public class CookieTempDataProviderOptions
    {
        /// <summary>
        /// The path set on the cookie. If set to <c>null</c>, the "path" attribute on the cookie is set to the current
        /// request's <see cref="HttpRequest.PathBase"/> value. If the value of <see cref="HttpRequest.PathBase"/> is
        /// <c>null</c> or empty, then the "path" attribute is set to the value of <see cref="CookieOptions.Path"/>.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The domain set on a cookie. Defaults to <c>null</c>.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// The name of the cookie which stores TempData. Defaults to <see cref="CookieTempDataProvider.CookieName"/>. 
        /// </summary>
        public string CookieName { get; set; } = CookieTempDataProvider.CookieName;
    }
}
