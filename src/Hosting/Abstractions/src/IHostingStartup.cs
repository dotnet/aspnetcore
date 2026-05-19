// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Represents platform specific configuration that will be applied to a <see cref="IWebHostBuilder"/> when building an <see cref="IWebHost"/>.
/// </summary>
public interface IHostingStartup
{
    /// <summary>
    /// Configure the <see cref="IWebHostBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Configure is intended to be called before user code, allowing a user to overwrite any changes made.
    /// </remarks>
    /// <param name="builder"></param>
    void Configure(IWebHostBuilder builder);
}
