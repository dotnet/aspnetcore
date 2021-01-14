// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Provides access to request cookie collection.
    /// </summary>
    public interface IRequestCookiesFeature
    {
        /// <summary>
        /// Gets or sets the request cookies.
        /// </summary>
        IRequestCookieCollection Cookies { get; set; }
    }
}
