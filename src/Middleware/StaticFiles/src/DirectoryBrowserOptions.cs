// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Directory browsing options
/// </summary>
public class DirectoryBrowserOptions : SharedOptionsBase
{
    /// <summary>
    /// Enabled directory browsing for all request paths
    /// </summary>
    public DirectoryBrowserOptions()
        : this(new SharedOptions())
    {
    }

    /// <summary>
    /// Enabled directory browsing all request paths
    /// </summary>
    /// <param name="sharedOptions"></param>
    public DirectoryBrowserOptions(SharedOptions sharedOptions)
        : base(sharedOptions)
    {
    }

    /// <summary>
    /// The component that generates the view.
    /// </summary>
    public IDirectoryFormatter? Formatter { get; set; }
}
