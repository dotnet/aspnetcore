// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.Hosting
{
    public class ConventionBasedStartup : IStartup
    {
        private readonly StartupMethods _methods;

        public ConventionBasedStartup(StartupMethods methods)
        {
            _methods = methods;
        }
        
        public void Configure(IApplicationBuilder app)
        {
            _methods.ConfigureDelegate(app);
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            return _methods.ConfigureServicesDelegate(services);
        }
    }
}