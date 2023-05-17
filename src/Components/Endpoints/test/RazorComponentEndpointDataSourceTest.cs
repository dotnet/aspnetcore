// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class RazorComponentEndpointDataSourceTest
{
    [Fact]
    public void RegistersEndpoints()
    {
        var endpointDataSource = CreateDataSource<App>();

        var endpoints = endpointDataSource.Endpoints;
        Assert.Equal(2, endpoints.Count);
    }

    [Fact]
    public void RegistersEndpoints_CallsCustomImplementation()
    {
        var endpointDataSource = CreateDataSource<CustomApp>();

        Assert.Equal(0, endpointDataSource.Endpoints.Count);
    }

    private RazorComponentEndpointDataSource<TComponent> CreateDataSource<TComponent>()
    {
        return new RazorComponentEndpointDataSource<TComponent>(
            DefaultRazorComponentApplication<TComponent>.Instance.GetBuilder(),
            Array.Empty<RenderModeEndpointProvider>(),
            new ApplicationBuilder(new ServiceCollection().BuildServiceProvider()),
            new RazorComponentEndpointFactory());
    }
}

public class CustomApp : IComponent
{
    public void Attach(RenderHandle renderHandle)
    {
        throw new NotImplementedException();
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        throw new NotImplementedException();
    }
}

public class App : IComponent
{
    public void Attach(RenderHandle renderHandle)
    {
        throw new NotImplementedException();
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        throw new NotImplementedException();
    }
}

[Route("/")]
public class Index : IComponent
{
    public void Attach(RenderHandle renderHandle)
    {
        throw new NotImplementedException();
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        throw new NotImplementedException();
    }
}

[Route("/counter")]
public class Counter : IComponent
{
    public void Attach(RenderHandle renderHandle)
    {
        throw new NotImplementedException();
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        throw new NotImplementedException();
    }
}
