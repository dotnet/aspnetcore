// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Provides information about the web hosting environment an application is running in.
/// </summary>
public interface IWebHostEnvironment : IHostEnvironment
{
    /// <summary>
    /// Gets or sets the absolute path to the directory that contains the web-servable application content files.
    /// This defaults to the 'wwwroot' subfolder.
    /// </summary>
    string WebRootPath { get; set; }

    /// <summary>
    /// Gets or sets an <see cref="IFileProvider"/> pointing at <see cref="WebRootPath"/>.
    /// This defaults to referencing files from the 'wwwroot' subfolder.
    /// </summary>
    IFileProvider WebRootFileProvider { get; set; }
}
