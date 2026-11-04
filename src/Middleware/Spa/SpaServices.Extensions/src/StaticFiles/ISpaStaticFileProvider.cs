// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.SpaServices.StaticFiles;

/// <summary>
/// Represents a service that can provide static files to be served for a Single Page
/// Application (SPA).
/// </summary>
public interface ISpaStaticFileProvider
{
    /// <summary>
    /// Gets the file provider, if available, that supplies the static files for the SPA.
    /// The value is <c>null</c> if no file provider is available.
    /// </summary>
    IFileProvider? FileProvider { get; }
}
