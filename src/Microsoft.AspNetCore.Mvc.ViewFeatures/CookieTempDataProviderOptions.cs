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
        /// The path set for a cookie. If not specified, current request's <see cref="HttpRequest.PathBase"/> value is used.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The domain set on a cookie. Defaults to <c>null</c>.
        /// </summary>
        public string Domain { get; set; }
    }
}
