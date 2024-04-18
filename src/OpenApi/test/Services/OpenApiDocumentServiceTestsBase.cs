// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Moq;
using static Microsoft.AspNetCore.OpenApi.Tests.OpenApiOperationGeneratorTests;

public abstract class OpenApiDocumentServiceTestBase
{
    public static async Task VerifyOpenApiDocument(IEndpointRouteBuilder builder, Action<OpenApiDocument> verifyOpenApiDocument)
        => await VerifyOpenApiDocument(builder, new OpenApiOptions(), verifyOpenApiDocument);

    public static async Task VerifyOpenApiDocument(IEndpointRouteBuilder builder, OpenApiOptions openApiOptions, Action<OpenApiDocument> verifyOpenApiDocument)
    {
        var documentService = CreateDocumentService(builder, openApiOptions);
        var document = await documentService.GetOpenApiDocumentAsync();
        verifyOpenApiDocument(document);
    }

    internal static OpenApiDocumentService CreateDocumentService(IEndpointRouteBuilder builder, OpenApiOptions openApiOptions)
    {
        var context = new ApiDescriptionProviderContext([]);

        var endpointDataSource = builder.DataSources.OfType<EndpointDataSource>().Single();
        var hostEnvironment = new HostEnvironment
        {
            ApplicationName = nameof(OpenApiDocumentServiceTests)
        };
        var options = new Mock<IOptionsMonitor<OpenApiOptions>>();
        options.Setup(o => o.Get(It.IsAny<string>())).Returns(openApiOptions);

        var provider = CreateEndpointMetadataApiDescriptionProvider(endpointDataSource);
        provider.OnProvidersExecuting(context);
        provider.OnProvidersExecuted(context);

        var apiDescriptionGroupCollectionProvider = CreateApiDescriptionGroupCollectionProvider(context.Results);

        var documentService = new OpenApiDocumentService("Test", apiDescriptionGroupCollectionProvider, hostEnvironment, options.Object, builder.ServiceProvider);
        ((TestServiceProvider)builder.ServiceProvider).TestDocumentService = documentService;

        return documentService;
    }

    public static IApiDescriptionGroupCollectionProvider CreateApiDescriptionGroupCollectionProvider(IList<ApiDescription> apiDescriptions = null)
    {
        var apiDescriptionGroup = new ApiDescriptionGroup("testGroupName", (apiDescriptions ?? Array.Empty<ApiDescription>()).AsReadOnly());
        var apiDescriptionGroupCollection = new ApiDescriptionGroupCollection([apiDescriptionGroup], 1);
        var apiDescriptionGroupCollectionProvider = new Mock<IApiDescriptionGroupCollectionProvider>();
        apiDescriptionGroupCollectionProvider.Setup(p => p.ApiDescriptionGroups).Returns(apiDescriptionGroupCollection);
        return apiDescriptionGroupCollectionProvider.Object;
    }

    private static EndpointMetadataApiDescriptionProvider CreateEndpointMetadataApiDescriptionProvider(EndpointDataSource endpointDataSource) => new EndpointMetadataApiDescriptionProvider(
        endpointDataSource,
        new HostEnvironment { ApplicationName = nameof(OpenApiDocumentServiceTests) },
        new DefaultParameterPolicyFactory(Options.Create(new RouteOptions()), new TestServiceProvider()),
        new ServiceProviderIsService());

    internal static TestEndpointRouteBuilder CreateBuilder(IServiceCollection serviceCollection = null)
    {
        var serviceProvider = new TestServiceProvider();
        serviceProvider.SetInternalServiceProvider(serviceCollection ?? new ServiceCollection());
        return new TestEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
    }

    internal class TestEndpointRouteBuilder : IEndpointRouteBuilder
    {
        public TestEndpointRouteBuilder(IApplicationBuilder applicationBuilder)
        {
            ApplicationBuilder = applicationBuilder ?? throw new ArgumentNullException(nameof(applicationBuilder));
            DataSources = new List<EndpointDataSource>();
        }

        public IApplicationBuilder ApplicationBuilder { get; }

        public IApplicationBuilder CreateApplicationBuilder() => ApplicationBuilder.New();

        public ICollection<EndpointDataSource> DataSources { get; }

        public IServiceProvider ServiceProvider => ApplicationBuilder.ApplicationServices;
    }

    private class TestServiceProvider : IServiceProvider, IKeyedServiceProvider
    {
        public static TestServiceProvider Instance { get; } = new TestServiceProvider();
        private IKeyedServiceProvider _serviceProvider;
        internal OpenApiDocumentService TestDocumentService { get; set; }
        internal OpenApiComponentService TestComponentService { get; set; } = new OpenApiComponentService();

        public void SetInternalServiceProvider(IServiceCollection serviceCollection)
        {
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        public object GetKeyedService(Type serviceType, object serviceKey)
        {
            if (serviceType == typeof(OpenApiDocumentService))
            {
                return TestDocumentService;
            }
            if (serviceType == typeof(OpenApiComponentService))
            {
                return TestComponentService;
            }

            return _serviceProvider.GetKeyedService(serviceType, serviceKey);
        }

        public object GetRequiredKeyedService(Type serviceType, object serviceKey)
        {
            if (serviceType == typeof(OpenApiDocumentService))
            {
                return TestDocumentService;
            }
            if (serviceType == typeof(OpenApiComponentService))
            {
                return TestComponentService;
            }

            return _serviceProvider.GetRequiredKeyedService(serviceType, serviceKey);
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IOptions<RouteHandlerOptions>))
            {
                return Options.Create(new RouteHandlerOptions());
            }

            return _serviceProvider.GetService(serviceType);
        }
    }
}
