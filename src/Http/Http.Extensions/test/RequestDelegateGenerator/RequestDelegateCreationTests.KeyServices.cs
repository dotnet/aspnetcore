// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.Extensions.DependencyInjection;
namespace Microsoft.AspNetCore.Http.Generators.Tests;

public partial class RequestDelegateCreationTests : RequestDelegateCreationTestBase
{
    [Fact]
    public async Task SupportsSingleKeyedServiceWithStringKey()
    {
        var source = """
app.MapGet("/", (HttpContext context, [FromKeyedServices("service1")] TestService arg) => context.Items["arg"] = arg);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var myOriginalService = new TestService();
        var serviceProvider = CreateServiceProvider((serviceCollection) => serviceCollection.AddKeyedSingleton("service1", myOriginalService));
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var httpContext = CreateHttpContext(serviceProvider);
        await endpoint.RequestDelegate(httpContext);

        Assert.Same(myOriginalService, httpContext.Items["arg"]);
    }

    [Fact]
    public async Task SupportsSingleKeyedServiceWithCharKey()
    {
        var source = """
app.MapGet("/", (HttpContext context, [FromKeyedServices('a')] TestService arg) => context.Items["arg"] = arg);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var myOriginalService = new TestService();
        var serviceProvider = CreateServiceProvider((serviceCollection) => serviceCollection.AddKeyedSingleton('a', myOriginalService));
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var httpContext = CreateHttpContext(serviceProvider);
        await endpoint.RequestDelegate(httpContext);

        Assert.Same(myOriginalService, httpContext.Items["arg"]);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(false)]
    [InlineData(12.3)]
    public async Task SupportsSingleKeyedServiceWithPrimitiveKeyTypes(object key)
    {
        var source = $$"""
app.MapGet("/", (HttpContext context, [FromKeyedServices({{key.ToString()?.ToLowerInvariant()}})] TestService arg) => context.Items["arg"] = arg);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var myOriginalService = new TestService();
        var serviceProvider = CreateServiceProvider((serviceCollection) => serviceCollection.AddKeyedSingleton(key, myOriginalService));
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var httpContext = CreateHttpContext(serviceProvider);
        await endpoint.RequestDelegate(httpContext);

        Assert.Same(myOriginalService, httpContext.Items["arg"]);
    }

    [Fact]
    public async Task ThrowsForUnregisteredRequiredKeyService()
    {
        var source = """
app.MapGet("/", (HttpContext context, [FromKeyedServices("service1")] TestService arg) => context.Items["arg"] = arg);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await endpoint.RequestDelegate(httpContext));

        Assert.Equal("No service for type 'Microsoft.AspNetCore.Http.Generators.Tests.TestService' has been registered.", exception.Message);
    }

    [Fact]
    public async Task DoesNotThrowForUnregisteredOptionalKeyService()
    {
        var source = """
app.MapGet("/", (HttpContext context, [FromKeyedServices("service1")] TestService? arg) => context.Items["arg"] = arg);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        await endpoint.RequestDelegate(httpContext);

        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Null(httpContext.Items["arg"]);
    }

    [Fact]
    public async Task SupportsMultipleKeyedServiceWithStringKey()
    {
        var source = """
app.MapGet("/", (HttpContext context, [FromKeyedServices("service1")] TestService arg1, [FromKeyedServices("service2")] TestService arg2) =>
{
    context.Items["arg1"] = arg1;
    context.Items["arg2"] = arg2;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var myOriginalService1 = new TestService();
        var serviceProvider = CreateServiceProvider((serviceCollection) =>
        {
            serviceCollection.AddKeyedSingleton("service1", myOriginalService1);
            serviceCollection.AddKeyedScoped<TestService>("service2");
        });
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var httpContext = CreateHttpContext(serviceProvider);
        await endpoint.RequestDelegate(httpContext);

        Assert.Same(myOriginalService1, httpContext.Items["arg1"]);
        Assert.IsType<TestService>(httpContext.Items["arg2"]);
    }

    [Fact]
    public async Task SupportsMultipleKeyedAndNonKeyedServices()
    {
        var source = """
app.MapGet("/", (HttpContext context, [FromKeyedServices("service1")] TestService arg1, [FromKeyedServices("service2")] TestService arg2, TestService arg3) =>
{
    context.Items["arg1"] = arg1;
    context.Items["arg2"] = arg2;
    context.Items["arg3"] = arg3;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var myOriginalService1 = new TestService();
        var myOriginalService2 = new TestService();
        var serviceProvider = CreateServiceProvider((serviceCollection) =>
        {
            serviceCollection.AddKeyedSingleton("service1", myOriginalService1);
            serviceCollection.AddKeyedScoped<TestService>("service2");
            serviceCollection.AddSingleton(myOriginalService2);
        });
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var httpContext = CreateHttpContext(serviceProvider);
        await endpoint.RequestDelegate(httpContext);

        Assert.Same(myOriginalService1, httpContext.Items["arg1"]);
        Assert.IsType<TestService>(httpContext.Items["arg2"]);
        Assert.Same(myOriginalService2, httpContext.Items["arg3"]);
    }
}
