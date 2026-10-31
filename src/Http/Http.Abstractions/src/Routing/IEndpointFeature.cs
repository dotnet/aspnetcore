// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// A feature interface for endpoint routing. Use <see cref="HttpContext.Features"/>
/// to access an instance associated with the current request.
/// </summary>
public interface IEndpointFeature
{
    /// <summary>
    /// Gets or sets the selected <see cref="Http.Endpoint"/> for the current
    /// request.
    /// </summary>
    Endpoint? Endpoint { get; set; }
}
