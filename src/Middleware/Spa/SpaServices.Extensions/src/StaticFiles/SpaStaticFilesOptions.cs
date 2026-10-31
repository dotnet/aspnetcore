// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SpaServices.StaticFiles;

/// <summary>
/// Represents options for serving static files for a Single Page Application (SPA).
/// </summary>
public class SpaStaticFilesOptions
{
    /// <summary>
    /// Gets or sets the path, relative to the application root, of the directory in which
    /// the physical files are located.
    ///
    /// If the specified directory does not exist, then the
    /// <see cref="SpaStaticFilesExtensions.UseSpaStaticFiles(Builder.IApplicationBuilder)"/>
    /// middleware will not serve any static files.
    /// </summary>
    public string RootPath { get; set; } = default!;
}
