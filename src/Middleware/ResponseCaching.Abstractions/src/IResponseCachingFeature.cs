// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.ResponseCaching;

/// <summary>
/// A feature for configuring additional response cache options on the HTTP response.
/// </summary>
public interface IResponseCachingFeature
{
    /// <summary>
    /// Gets or sets the query keys used by the response cache middleware for calculating secondary vary keys.
    /// </summary>
    string[]? VaryByQueryKeys { get; set; }
}
