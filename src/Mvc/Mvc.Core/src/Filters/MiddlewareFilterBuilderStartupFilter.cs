// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Filters;

internal sealed class MiddlewareFilterBuilderStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return MiddlewareFilterBuilder;

        void MiddlewareFilterBuilder(IApplicationBuilder builder)
        {
            var middlewarePipelineBuilder = builder.ApplicationServices.GetRequiredService<MiddlewareFilterBuilder>();
            middlewarePipelineBuilder.ApplicationBuilder = builder.New();

            next(builder);
        }
    }
}
