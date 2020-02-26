// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Tests.Internal
{
    public class MyContainerFactory : IServiceProviderFactory<MyContainer>
    {
        public MyContainer CreateBuilder(IServiceCollection services)
        {
            var container = new MyContainer();
            container.Populate(services);
            return container;
        }

        public IServiceProvider CreateServiceProvider(MyContainer containerBuilder)
        {
            containerBuilder.Build();
            return containerBuilder;
        }
    }
}
