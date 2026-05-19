// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal sealed class ModelStateInvalidFilterFactory : IFilterFactory, IOrderedFilter
{
    public int Order => ModelStateInvalidFilter.FilterOrder;

    public bool IsReusable => true;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ApiBehaviorOptions>>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        return new ModelStateInvalidFilter(options.Value, loggerFactory.CreateLogger(typeof(ModelStateInvalidFilter)));
    }
}
