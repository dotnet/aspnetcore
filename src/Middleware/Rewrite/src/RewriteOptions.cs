// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Rewrite;

/// <summary>
/// Options for the <see cref="RewriteMiddleware"/>
/// </summary>
public class RewriteOptions
{
    /// <summary>
    /// A list of <see cref="IRule"/> that will be applied in order upon a request.
    /// </summary>
    public IList<IRule> Rules { get; } = new List<IRule>();

    /// <summary>
    /// Gets and sets the File Provider for file and directory checks.
    /// </summary>
    /// <value>
    /// Defaults to <see cref="IHostingEnvironment.WebRootFileProvider"/>.
    /// </value>
    public IFileProvider StaticFileProvider { get; set; } = default!;

    internal RequestDelegate? BranchedNext { get; set; }
}
