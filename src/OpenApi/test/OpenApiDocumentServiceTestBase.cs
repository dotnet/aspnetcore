using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Moq;
using static Microsoft.AspNetCore.OpenApi.Tests.OpenApiOperationGeneratorTests;

public abstract class OpenApiDocumentServiceTestBase
{
    public static async Task VerifyOpenApiDocument(string route, Delegate handler, IEnumerable<string> httpMethods = null)
    {
        // Arrange
        var apiDescriptionGroupCollectionProvider = GetApiDescriptionGroupCollectionProvider(GetApiDescriptions(handler, route, httpMethods: httpMethods).AsReadOnly());
        var componentService = new OpenApiComponentService(Options.Create(new JsonOptions()));
        var server = new TestServer();
        var openApiDocumentService = new OpenApiDocumentService(apiDescriptionGroupCollectionProvider, componentService, server);

        // Act
        var document = openApiDocumentService.GenerateOpenApiDocument();

        // Assert
        await Verify(GetOpenApiJson(document));
    }

    private static string GetOpenApiJson(OpenApiDocument document)
    {
        using var textWriter = new StringWriter(CultureInfo.InvariantCulture);
        var jsonWriter = new OpenApiJsonWriter(textWriter);
        document.SerializeAsV31(jsonWriter);
        return textWriter.ToString();
    }

    private static IApiDescriptionGroupCollectionProvider GetApiDescriptionGroupCollectionProvider(IReadOnlyList<ApiDescription> apiDescriptions)
    {
        var apiDescriptionGroupCollectionProvider = new Mock<IApiDescriptionGroupCollectionProvider>();
        var apiDescriptionGroup = new ApiDescriptionGroup("testGroupName", apiDescriptions);
        var apiDescriptionGroupCollection = new ApiDescriptionGroupCollection([apiDescriptionGroup], 1);
        apiDescriptionGroupCollectionProvider.Setup(p => p.ApiDescriptionGroups).Returns(apiDescriptionGroupCollection);
        return apiDescriptionGroupCollectionProvider.Object;
    }

    private static IList<ApiDescription> GetApiDescriptions(
        Delegate action,
        string pattern = null,
        IEnumerable<string> httpMethods = null,
        string displayName = null)
    {
        var methodInfo = action.Method;
        var attributes = methodInfo.GetCustomAttributes();
        var context = new ApiDescriptionProviderContext([]);

        var httpMethodMetadata = new HttpMethodMetadata(httpMethods ?? ["GET"]);
        var metadataItems = new List<object>(attributes) { methodInfo, httpMethodMetadata };
        var endpointMetadata = new EndpointMetadataCollection([.. metadataItems]);
        var routePattern = RoutePatternFactory.Parse(pattern ?? "/");

        var endpoint = new RouteEndpoint(httpContext => Task.CompletedTask, routePattern, 0, endpointMetadata, displayName);
        var endpointDataSource = new DefaultEndpointDataSource(endpoint);

        var provider = CreateEndpointMetadataApiDescriptionProvider(endpointDataSource);

        provider.OnProvidersExecuting(context);
        provider.OnProvidersExecuted(context);

        return context.Results;
    }

    private static EndpointMetadataApiDescriptionProvider CreateEndpointMetadataApiDescriptionProvider(EndpointDataSource endpointDataSource) => new EndpointMetadataApiDescriptionProvider(
            endpointDataSource,
            new HostEnvironment { ApplicationName = nameof(OpenApiDocumentServiceTest) },
            new DefaultParameterPolicyFactory(CreateRouteOptions(), new TestServiceProvider()),
            new ServiceProviderIsService());

    private static ApiDescription GetApiDescription(Delegate action, string pattern = null, string displayName = null, IEnumerable<string> httpMethods = null) =>
        Assert.Single(GetApiDescriptions(action, pattern, displayName: displayName, httpMethods: httpMethods));

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

    private static IOptions<RouteOptions> CreateRouteOptions()
    {
        var options = new RouteOptions();
        options.ConstraintMap["regex"] = typeof(RegexInlineRouteConstraint);
        return Options.Create(options);
    }

    private class TestServer : IServer
    {
        IFeatureCollection IServer.Features { get; } = CreateTestFeatureCollection();
        public RequestDelegate RequestDelegate { get; private set; }
        private static FeatureCollection CreateTestFeatureCollection()
        {
            var features = new FeatureCollection();
            features.Set<IServerAddressesFeature>(new TestServerAddressFeature());
            return features;
        }

        public void Dispose() { }
        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            RequestDelegate = async ctx =>
            {
                var httpContext = application.CreateContext(ctx.Features);
                try
                {
                    await application.ProcessRequestAsync(httpContext);
                }
                catch (Exception ex)
                {
                    application.DisposeContext(httpContext, ex);
                    throw;
                }
                application.DisposeContext(httpContext, null);
            };
            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public class TestServerAddressFeature() : IServerAddressesFeature
    {
        public ICollection<string> Addresses { get; set; } = ["https://localhost:5001", "http://localhost:5000"];
        public bool PreferHostingUrls => true;
        bool IServerAddressesFeature.PreferHostingUrls { get; set; }
    }
}
