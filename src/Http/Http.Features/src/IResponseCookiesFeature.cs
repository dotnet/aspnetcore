// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// A helper for creating the response Set-Cookie header.
    /// </summary>
    public interface IResponseCookiesFeature
    {
        /// <summary>
        /// Gets the wrapper for the response Set-Cookie header.
        /// </summary>
        IResponseCookies Cookies { get; }
    }
}