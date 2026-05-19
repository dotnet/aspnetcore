// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Defines a set of settings which can be used for response caching.
/// </summary>
public class CacheProfile
{
    /// <summary>
    /// Gets or sets the duration in seconds for which the response is cached.
    /// If this property is set to a non null value,
    /// the "max-age" in "Cache-control" header is set in the
    /// <see cref="Microsoft.AspNetCore.Http.HttpContext.Response" />.
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// Gets or sets the location where the data from a particular URL must be cached.
    /// If this property is set to a non null value,
    /// the "Cache-control" header is set in the <see cref="Microsoft.AspNetCore.Http.HttpContext.Response" />.
    /// </summary>
    public ResponseCacheLocation? Location { get; set; }

    /// <summary>
    /// Gets or sets the value which determines whether the data should be stored or not.
    /// When set to <see langword="true"/>, it sets "Cache-control" header in
    /// <see cref="Microsoft.AspNetCore.Http.HttpContext.Response" /> to "no-store".
    /// Ignores the "Location" parameter for values other than "None".
    /// Ignores the "Duration" parameter.
    /// </summary>
    public bool? NoStore { get; set; }

    /// <summary>
    /// Gets or sets the value for the Vary header in <see cref="Microsoft.AspNetCore.Http.HttpContext.Response" />.
    /// </summary>
    public string? VaryByHeader { get; set; }

    /// <summary>
    /// Gets or sets the query keys to vary by.
    /// </summary>
    /// <remarks>
    /// <see cref="VaryByQueryKeys"/> requires the response cache middleware.
    /// </remarks>
    public string[]? VaryByQueryKeys { get; set; }
}
