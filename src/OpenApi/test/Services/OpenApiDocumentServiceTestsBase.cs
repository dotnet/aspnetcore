// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Moq;
using static Microsoft.AspNetCore.OpenApi.Tests.OpenApiOperationGeneratorTests;

public abstract class OpenApiDocumentServiceTestBase
{
    public static async Task VerifyOpenApiDocument(IEndpointRouteBuilder builder, Action<OpenApiDocument> verifyOpenApiDocument)
    {
        var context = new ApiDescriptionProviderContext([]);

        var endpointDataSource = builder.DataSources.OfType<EndpointDataSource>().Single();
        var hostEnvironment = new HostEnvironment
        {
            ApplicationName = nameof(OpenApiDocumentServiceTests)
        };
        var options = new Mock<IOptionsMonitor<OpenApiOptions>>();
        options.Setup(o => o.Get(It.IsAny<string>())).Returns(new OpenApiOptions());

        var provider = CreateEndpointMetadataApiDescriptionProvider(endpointDataSource);
        provider.OnProvidersExecuting(context);
        provider.OnProvidersExecuted(context);

        var apiDescriptionGroupCollectionProvider = CreateApiDescriptionGroupCollectionProvider(context.Results);

        var documentService = new OpenApiDocumentService("Test", apiDescriptionGroupCollectionProvider, hostEnvironment, options.Object);
        var document = await documentService.GetOpenApiDocumentAsync();
        verifyOpenApiDocument(document);
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

    internal static TestEndpointRouteBuilder CreateBuilder() =>
        new TestEndpointRouteBuilder(new ApplicationBuilder(TestServiceProvider.Instance));

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

    private class TestServiceProvider : IServiceProvider
    {
        public static TestServiceProvider Instance { get; } = new TestServiceProvider();

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IOptions<RouteHandlerOptions>))
            {
                return Options.Create(new RouteHandlerOptions());
            }

            return null;
        }
    }
}
