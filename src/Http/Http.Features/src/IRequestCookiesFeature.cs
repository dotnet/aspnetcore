// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

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
