// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection;

public static class RazorComponentsServiceCollectionExtensions
{
    public static IRazorComponentsBuilder AddRazorComponents(this IServiceCollection services)
    {
        // TODO: Register common services required for server side rendering
        return new DefaultRazorcomponentsBuilder(services);
    }

    private sealed class DefaultRazorcomponentsBuilder : IRazorComponentsBuilder
    {
        public DefaultRazorcomponentsBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}
