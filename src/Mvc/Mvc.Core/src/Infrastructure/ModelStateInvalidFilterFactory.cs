// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    internal class ModelStateInvalidFilterFactory : IFilterFactory, IOrderedFilter
    {
        public int Order => ModelStateInvalidFilter.FilterOrder;

        public bool IsReusable => true;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<IOptions<ApiBehaviorOptions>>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            return new ModelStateInvalidFilter(options.Value, loggerFactory.CreateLogger<ModelStateInvalidFilter>());
        }
    }
}
