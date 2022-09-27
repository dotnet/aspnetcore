// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Routing;

internal sealed class DefaultEndpointRouteBuilder : IEndpointRouteBuilder
{
    public DefaultEndpointRouteBuilder(IApplicationBuilder applicationBuilder)
    {
        ApplicationBuilder = applicationBuilder ?? throw new ArgumentNullException(nameof(applicationBuilder));
        DataSources = new List<EndpointDataSource>();
    }

    public IApplicationBuilder ApplicationBuilder { get; }

    public IApplicationBuilder CreateApplicationBuilder() => ApplicationBuilder.New();

    public ICollection<EndpointDataSource> DataSources { get; }

    public IServiceProvider ServiceProvider => ApplicationBuilder.ApplicationServices;
}
