// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.CookiePolicy;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides programmatic configuration for the <see cref="CookiePolicyMiddleware"/>.
    /// </summary>
    public class CookiePolicyOptions
    {
        /// <summary>
        /// Affects whether cookies must be HttpOnly.
        /// </summary>
        public HttpOnlyPolicy HttpOnly { get; set; } = HttpOnlyPolicy.None;
        /// <summary>
        /// Affects whether cookies must be Secure.
        /// </summary>
        public SecurePolicy Secure { get; set; } = SecurePolicy.None;

        /// <summary>
        /// Called when a cookie is appended.
        /// </summary>
        public Action<AppendCookieContext> OnAppendCookie { get; set; }

        /// <summary>
        /// Called when a cookie is deleted.
        /// </summary>
        public Action<DeleteCookieContext> OnDeleteCookie { get; set; }
    }
}