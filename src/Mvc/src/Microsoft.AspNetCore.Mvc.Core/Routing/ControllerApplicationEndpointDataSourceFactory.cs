// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal class ControllerApplicationDataSourceFactory : ApplicationDataSourceFactory
    {
        public override IEndpointConventionBuilder GetOrCreateApplication(IEndpointRouteBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var dataSource = GetOrCreateDataSource(builder);
            return dataSource.AddApplicationAssemblies();
        }

        public override IEndpointConventionBuilder GetOrCreateAssembly(IEndpointRouteBuilder builder, Assembly assembly)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            var dataSource = GetOrCreateDataSource(builder);
            return dataSource.AddAssembly(assembly);
        }
        
        // Controllers use a single data source.
        public ControllerEndpointDataSource GetOrCreateDataSource(IEndpointRouteBuilder builder)
        {
            var controllerDataSource = builder.DataSources.OfType<ControllerEndpointDataSource>().SingleOrDefault();
            if (controllerDataSource == null)
            {
                controllerDataSource = builder.ServiceProvider.GetRequiredService<ControllerEndpointDataSource>();
                builder.DataSources.Add(controllerDataSource);
            }

            return controllerDataSource;
        }
    }
}
