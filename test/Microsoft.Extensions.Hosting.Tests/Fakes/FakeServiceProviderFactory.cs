// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting.Fakes
{
    public class FakeServiceProviderFactory : IServiceProviderFactory<FakeServiceCollection>
    {
        public FakeServiceCollection CreateBuilder(IServiceCollection services)
        {
            var container = new FakeServiceCollection();
            container.Populate(services);
            return container;
        }

        public IServiceProvider CreateServiceProvider(FakeServiceCollection containerBuilder)
        {
            containerBuilder.Build();
            return containerBuilder;
        }
    }
}
