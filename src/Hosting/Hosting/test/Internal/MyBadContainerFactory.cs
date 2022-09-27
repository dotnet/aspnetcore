// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Tests.Internal;

public class MyBadContainerFactory : IServiceProviderFactory<MyContainer>
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
        return null;
    }
}
