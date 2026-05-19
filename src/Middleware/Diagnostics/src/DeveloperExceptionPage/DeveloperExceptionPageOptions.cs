// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Options for the <see cref="DeveloperExceptionPageMiddleware"/>.
/// </summary>
public class DeveloperExceptionPageOptions
{
    /// <summary>
    /// Create an instance with the default options settings.
    /// </summary>
    public DeveloperExceptionPageOptions()
    {
        SourceCodeLineCount = 6;
    }

    /// <summary>
    /// Determines how many lines of code to include before and after the line of code
    /// present in an exception's stack frame. Only applies when symbols are available and
    /// source code referenced by the exception stack trace is present on the server.
    /// </summary>
    public int SourceCodeLineCount { get; set; }

    /// <summary>
    /// Provides files containing source code used to display contextual information of an exception.
    /// </summary>
    /// <remarks>
    /// If <c>null</c> <see cref="DeveloperExceptionPageMiddleware" /> will use a <see cref="PhysicalFileProvider"/>.
    /// </remarks>
    public IFileProvider? FileProvider { get; set; }
}
