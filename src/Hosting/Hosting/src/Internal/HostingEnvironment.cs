// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Hosting;

[DebuggerDisplay("ApplicationName = {ApplicationName}, EnvironmentName = {EnvironmentName}")]
#pragma warning disable CS0618 // Type or member is obsolete
internal sealed class HostingEnvironment : IHostingEnvironment, Extensions.Hosting.IHostingEnvironment, IWebHostEnvironment
#pragma warning restore CS0618 // Type or member is obsolete
{
    public string EnvironmentName { get; set; } = Extensions.Hosting.Environments.Production;

#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    public string? ApplicationName { get; set; }
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).

    public string WebRootPath { get; set; } = default!;

    public IFileProvider WebRootFileProvider { get; set; } = default!;

    public string ContentRootPath { get; set; } = default!;

    public IFileProvider ContentRootFileProvider { get; set; } = default!;
}
