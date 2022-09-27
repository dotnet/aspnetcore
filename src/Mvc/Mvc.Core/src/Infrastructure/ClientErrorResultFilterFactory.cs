// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal sealed class ClientErrorResultFilterFactory : IFilterFactory, IOrderedFilter
{
    public int Order => ClientErrorResultFilter.FilterOrder;

    public bool IsReusable => true;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var resultFilter = ActivatorUtilities.CreateInstance<ClientErrorResultFilter>(serviceProvider);
        return resultFilter;
    }
}
