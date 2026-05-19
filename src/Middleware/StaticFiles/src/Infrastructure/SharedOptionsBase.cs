// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.StaticFiles.Infrastructure;

/// <summary>
/// Options common to several middleware components
/// </summary>
public abstract class SharedOptionsBase
{
    /// <summary>
    /// Creates an new instance of the SharedOptionsBase.
    /// </summary>
    /// <param name="sharedOptions"></param>
    protected SharedOptionsBase(SharedOptions sharedOptions)
    {
        ArgumentNullException.ThrowIfNull(sharedOptions);

        SharedOptions = sharedOptions;
    }

    /// <summary>
    /// Options common to several middleware components
    /// </summary>
    protected SharedOptions SharedOptions { get; private set; }

    /// <summary>
    /// The relative request path that maps to static resources.
    /// This defaults to the site root '/'.
    /// </summary>
    public PathString RequestPath
    {
        get { return SharedOptions.RequestPath; }
        set { SharedOptions.RequestPath = value; }
    }

    /// <summary>
    /// The file system used to locate resources
    /// </summary>
    /// <remarks>
    /// Files are served from the path specified in <see cref="IWebHostEnvironment.WebRootPath"/>
    /// or <see cref="IWebHostEnvironment.WebRootFileProvider"/> which defaults to the 'wwwroot' subfolder.
    /// </remarks>
    public IFileProvider? FileProvider
    {
        get { return SharedOptions.FileProvider; }
        set { SharedOptions.FileProvider = value; }
    }

    /// <summary>
    /// Indicates whether to redirect to add a trailing slash at the end of path. Relative resource links may require this.
    /// </summary>
    public bool RedirectToAppendTrailingSlash
    {
        get { return SharedOptions.RedirectToAppendTrailingSlash; }
        set { SharedOptions.RedirectToAppendTrailingSlash = value; }
    }
}
