// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.SpaServices;

/// <summary>
/// Defines a class that provides mechanisms for configuring the hosting
/// of a Single Page Application (SPA) and attaching middleware.
/// </summary>
public interface ISpaBuilder
{
    /// <summary>
    /// The <see cref="IApplicationBuilder"/> representing the middleware pipeline
    /// in which the SPA is being hosted.
    /// </summary>
    IApplicationBuilder ApplicationBuilder { get; }

    /// <summary>
    /// Describes configuration options for hosting a SPA.
    /// </summary>
    SpaOptions Options { get; }
}
