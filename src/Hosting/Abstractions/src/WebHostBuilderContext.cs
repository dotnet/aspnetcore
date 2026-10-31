// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Context containing the common services on the <see cref="IWebHost" />. Some properties may be null until set by the <see cref="IWebHost" />.
/// </summary>
public class WebHostBuilderContext
{
    /// <summary>
    /// The <see cref="IWebHostEnvironment" /> initialized by the <see cref="IWebHost" />.
    /// </summary>
    public IWebHostEnvironment HostingEnvironment { get; set; } = default!;

    /// <summary>
    /// The <see cref="IConfiguration" /> containing the merged configuration of the application and the <see cref="IWebHost" />.
    /// </summary>
    public IConfiguration Configuration { get; set; } = default!;
}
