// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

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
        where TComponent : IRazorComponentApplication<TComponent>
    {
        return new RazorComponentEndpointDataSource<TComponent>(TComponent.GetBuilder(), new RazorComponentEndpointFactory());
    }
}

public class CustomApp : IComponent, IRazorComponentApplication<CustomApp>
{
    public void Attach(RenderHandle renderHandle)
    {
        throw new NotImplementedException();
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        throw new NotImplementedException();
    }

    static ComponentApplicationBuilder IRazorComponentApplication<CustomApp>.GetBuilder()
    {
        return new ComponentApplicationBuilder();
    }
}

public class App : IComponent, IRazorComponentApplication<App>
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
