// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    internal class MiddlewareFilterBuilderStartupFilter : IStartupFilter
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
}
