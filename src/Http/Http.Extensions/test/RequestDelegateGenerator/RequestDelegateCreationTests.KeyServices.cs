// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

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
    public async Task ThrowsIfKeyedAndNonKeyedAttributesOnSameParameter()
    {
        var source = """
app.MapGet("/", (HttpContext context, [FromKeyedServices("service1")] [FromServices] TestService arg) => context.Items["arg"] = arg);
""";
        var myOriginalService = new TestService();
        var serviceProvider = CreateServiceProvider((serviceCollection) => serviceCollection.AddKeyedSingleton("service1", myOriginalService));

        var (result, compilation) = await RunGeneratorAsync(source);

        // When the generator is enabled, you'll get a compile-time warning in addition to the exception thrown at
        // runtime.
        if (IsGeneratorEnabled)
        {
            Assert.Contains(result.Value.Diagnostics, diagnostic => diagnostic.Id == DiagnosticDescriptors.KeyedAndNotKeyedServiceAttributesNotSupported.Id);
        }

        // Throw during endpoint construction
        var exception = Assert.Throws<NotSupportedException>(() => GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider));
        Assert.Equal($"The {nameof(FromKeyedServicesAttribute)} is not supported on parameters that are also annotated with {nameof(IFromServiceMetadata)}.", exception.Message);
    }

    [Fact]
    public async Task SupportsKeyedServicesWithNullAndStringEmptyKeys()
    {
        var source = """
app.MapGet("/string-empty", (HttpContext context, [FromKeyedServices("")] TestService arg) => context.Items["arg"] = arg);
app.MapGet("/null", (HttpContext context, [FromKeyedServices(null!)] TestService arg) => context.Items["arg"] = arg);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var myOriginalService1 = new TestService();
        var myOriginalService2 = new TestService();
        var serviceProvider = CreateServiceProvider((serviceCollection) =>
        {
            serviceCollection.AddKeyedSingleton("", myOriginalService1);
            serviceCollection.AddSingleton(myOriginalService2);
        });
        var endpoints = GetEndpointsFromCompilation(compilation, serviceProvider: serviceProvider);

        var httpContext1 = CreateHttpContext(serviceProvider);
        await endpoints[0].RequestDelegate(httpContext1);
        var httpContext2 = CreateHttpContext(serviceProvider);
        await endpoints[1].RequestDelegate(httpContext2);

        Assert.Same(myOriginalService1, httpContext1.Items["arg"]);
        Assert.Same(myOriginalService2, httpContext2.Items["arg"]);
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
app.MapGet("/", (HttpContext context, [FromKeyedServices({{Convert.ToString(key, CultureInfo.InvariantCulture)?.ToLowerInvariant()}})] TestService arg) => context.Items["arg"] = arg);
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

    [Fact]
    public async Task ThrowsIfDiContainerDoesNotSupportKeyedServices()
    {
        var source = """
 app.MapGet("/", (HttpContext context, [FromKeyedServices("service1")] TestService arg1) =>
 {
     context.Items["arg1"] = arg1;
 });
 """;
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = new MockServiceProvider();
        if (!IsGeneratorEnabled)
        {
            var runtimeException = Assert.Throws<InvalidOperationException>(() => GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider));
            Assert.Equal("Unable to resolve service referenced by FromKeyedServicesAttribute. The service provider doesn't support keyed services.", runtimeException.Message);
            return;
        }
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var httpContext = CreateHttpContext(serviceProvider);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await endpoint.RequestDelegate(httpContext));
        Assert.Equal("Unable to resolve service referenced by FromKeyedServicesAttribute. The service provider doesn't support keyed services.", exception.Message);
    }

    // See: https://github.com/dotnet/aspnetcore/issues/58633
    [Fact]
    public async Task RequestDelegateGeneratesCompilableCodeForKeyedServiceInNamespaceHttp()
    {
        var source = """
app.MapGet("/hello", ([FromKeyedServices("example")] global::Http.ExampleService e) => e.Act("To be or not to be…"));
""";
        var (results, compilation) = await RunGeneratorAsync(source);

        // Ironically, the same error this is testing would bite us here, so we must globally qualify the type name.
        var serviceProvider = CreateServiceProvider((serviceCollection) => serviceCollection.AddKeyedSingleton<global::Http.ExampleService>("example"));
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        VerifyStaticEndpointModel(results, endpointModel =>
        {
            Assert.Equal("MapGet", endpointModel.HttpMethod);
            var p = Assert.Single(endpointModel.Parameters);
            Assert.Equal(EndpointParameterSource.KeyedService, p.Source);
            Assert.Equal("e", p.SymbolName);
        });

        var httpContext = CreateHttpContext(serviceProvider);
        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, "To be or not to be…");
    }

    private class MockServiceProvider : IServiceProvider, ISupportRequiredService
    {
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(Microsoft.Extensions.Logging.ILoggerFactory))
            {
                return NullLoggerFactory.Instance;
            }
            return null;
        }
        public object GetRequiredService(Type serviceType)
        {
            return GetService(serviceType);
        }
    }
}
