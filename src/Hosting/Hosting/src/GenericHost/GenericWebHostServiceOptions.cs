// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Hosting;

internal sealed class GenericWebHostServiceOptions
{
    public Action<IApplicationBuilder>? ConfigureApplication { get; set; }

    public WebHostOptions WebHostOptions { get; set; } = default!; // Always set when options resolved by DI

    public AggregateException? HostingStartupExceptions { get; set; }
}
