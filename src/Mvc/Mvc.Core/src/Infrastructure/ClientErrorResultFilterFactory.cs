// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
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
}
