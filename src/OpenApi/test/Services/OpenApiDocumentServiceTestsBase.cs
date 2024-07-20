// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Moq;
using static Microsoft.AspNetCore.OpenApi.Tests.OpenApiOperationGeneratorTests;

public abstract class OpenApiDocumentServiceTestBase
{
    public static async Task VerifyOpenApiDocument(IEndpointRouteBuilder builder, Action<OpenApiDocument> verifyOpenApiDocument, CancellationToken cancellationToken = default)
        => await VerifyOpenApiDocument(builder, new OpenApiOptions(), verifyOpenApiDocument, cancellationToken);

    public static async Task VerifyOpenApiDocument(IEndpointRouteBuilder builder, OpenApiOptions openApiOptions, Action<OpenApiDocument> verifyOpenApiDocument, CancellationToken cancellationToken = default)
    {
        var documentService = CreateDocumentService(builder, openApiOptions);
        var document = await documentService.GetOpenApiDocumentAsync(cancellationToken);
        verifyOpenApiDocument(document);
    }

    public static async Task VerifyOpenApiDocument(ActionDescriptor action, Action<OpenApiDocument> verifyOpenApiDocument)
    {
        var documentService = CreateDocumentService(action);
        var document = await documentService.GetOpenApiDocumentAsync();
        verifyOpenApiDocument(document);
    }

    internal static OpenApiDocumentService CreateDocumentService(ActionDescriptor actionDescriptor)
    {
        var builder = CreateBuilder();
        var context = new ApiDescriptionProviderContext([actionDescriptor]);
        var hostEnvironment = new HostEnvironment
        {
            ApplicationName = nameof(OpenApiDocumentServiceTests)
        };

        var options = new MvcOptions();
        var optionsAccessor = Options.Create(options);

        var constraintResolver = new Mock<IInlineConstraintResolver>();
        constraintResolver.Setup(c => c.ResolveConstraint("int"))
            .Returns(new IntRouteConstraint());

        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

        var routeOptions = new RouteOptions();
        routeOptions.ConstraintMap["regex"] = typeof(RegexInlineRouteConstraint);

        var provider = new DefaultApiDescriptionProvider(
            optionsAccessor,
            constraintResolver.Object,
            modelMetadataProvider,
            new ActionResultTypeMapper(),
            Options.Create(routeOptions));
        provider.OnProvidersExecuting(context);
        provider.OnProvidersExecuted(context);

        var apiDescriptionGroupCollectionProvider = CreateApiDescriptionGroupCollectionProvider(context.Results);

        var openApiOptions = new Mock<IOptionsMonitor<OpenApiOptions>>();
        openApiOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new OpenApiOptions());

        var schemaService = new OpenApiSchemaService("Test", Options.Create(new Microsoft.AspNetCore.Http.Json.JsonOptions()), builder.ServiceProvider, openApiOptions.Object);
        ((TestServiceProvider)builder.ServiceProvider).TestSchemaService = schemaService;
        var documentService = new OpenApiDocumentService("Test", apiDescriptionGroupCollectionProvider, hostEnvironment, openApiOptions.Object, builder.ServiceProvider, new OpenApiTestServer());
        ((TestServiceProvider)builder.ServiceProvider).TestDocumentService = documentService;

        return documentService;
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
        var jsonOptions = builder.ServiceProvider.GetService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>() ?? Options.Create(new Microsoft.AspNetCore.Http.Json.JsonOptions());

        var schemaService = new OpenApiSchemaService("Test", jsonOptions, builder.ServiceProvider, options.Object);
        ((TestServiceProvider)builder.ServiceProvider).TestSchemaService = schemaService;
        var documentService = new OpenApiDocumentService("Test", apiDescriptionGroupCollectionProvider, hostEnvironment, options.Object, builder.ServiceProvider, new OpenApiTestServer());
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

    private static EndpointMetadataApiDescriptionProvider CreateEndpointMetadataApiDescriptionProvider(EndpointDataSource endpointDataSource)
    {
        var options = new RouteOptions();
        options.ConstraintMap["regex"] = typeof(RegexInlineRouteConstraint);

        return new EndpointMetadataApiDescriptionProvider(
            endpointDataSource,
            new HostEnvironment { ApplicationName = nameof(OpenApiDocumentServiceTests) },
            new DefaultParameterPolicyFactory(Options.Create(options), new TestServiceProvider()),
            new ServiceProviderIsService());
    }

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

    public ControllerActionDescriptor CreateActionDescriptor(string methodName = null, Type controllerType = null)
    {
        var action = new ControllerActionDescriptor();
        action.SetProperty(new ApiDescriptionActionData());

        if (controllerType != null)
        {
            action.MethodInfo = controllerType.GetMethod(
                methodName ?? "ReturnsObject",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            action.ControllerTypeInfo = controllerType.GetTypeInfo();
            action.BoundProperties = new List<ParameterDescriptor>();

            foreach (var property in controllerType.GetProperties())
            {
                var bindingInfo = BindingInfo.GetBindingInfo(property.GetCustomAttributes().OfType<object>());
                if (bindingInfo != null)
                {
                    action.BoundProperties.Add(new ParameterDescriptor()
                    {
                        BindingInfo = bindingInfo,
                        Name = property.Name,
                        ParameterType = property.PropertyType,
                    });
                }
            }
        }
        else
        {
            action.MethodInfo = GetType().GetMethod(
                methodName ?? "ReturnsObject",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        action.AttributeRouteInfo = new()
        {
            Template = action.MethodInfo.GetCustomAttribute<RouteAttribute>()?.Template,
            Name = action.MethodInfo.GetCustomAttribute<RouteAttribute>()?.Name,
            Order = action.MethodInfo.GetCustomAttribute<RouteAttribute>()?.Order ?? 0,
        };
        action.RouteValues.Add("controller", "Test");
        action.RouteValues.Add("action", action.MethodInfo.Name);
        action.ActionConstraints = [new HttpMethodActionConstraint(["GET"])];

        action.Parameters = [];
        foreach (var parameter in action.MethodInfo.GetParameters())
        {
            action.Parameters.Add(new ControllerParameterDescriptor()
            {
                Name = parameter.Name,
                ParameterType = parameter.ParameterType,
                BindingInfo = BindingInfo.GetBindingInfo(parameter.GetCustomAttributes().OfType<object>()),
                ParameterInfo = parameter
            });
        }

        return action;
    }

    private class TestServiceProvider : IServiceProvider, IKeyedServiceProvider
    {
        public static TestServiceProvider Instance { get; } = new TestServiceProvider();
        private IKeyedServiceProvider _serviceProvider;
        internal OpenApiDocumentService TestDocumentService { get; set; }
        internal OpenApiSchemaStore TestSchemaStoreService { get; } = new OpenApiSchemaStore();
        internal OpenApiSchemaService TestSchemaService { get; set; }

        public void SetInternalServiceProvider(IServiceCollection serviceCollection)
        {
            serviceCollection.AddKeyedSingleton<OpenApiSchemaStore>("Test");
            serviceCollection.Configure<OpenApiOptions>("Test", options =>
            {
                options.DocumentName = "Test";
            });
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        public object GetKeyedService(Type serviceType, object serviceKey)
        {
            if (serviceType == typeof(OpenApiDocumentService))
            {
                return TestDocumentService;
            }
            if (serviceType == typeof(OpenApiSchemaService))
            {
                return TestSchemaService;
            }
            if (serviceType == typeof(OpenApiSchemaStore))
            {
                return TestSchemaStoreService;
            }

            return _serviceProvider.GetKeyedService(serviceType, serviceKey);
        }

        public object GetRequiredKeyedService(Type serviceType, object serviceKey)
        {
            if (serviceType == typeof(OpenApiDocumentService))
            {
                return TestDocumentService;
            }
            if (serviceType == typeof(OpenApiSchemaService))
            {
                return TestSchemaService;
            }
            if (serviceType == typeof(OpenApiSchemaStore))
            {
                return TestSchemaStoreService;
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

    internal class OpenApiTestServer(string[] addresses = null) : IServer
    {
        public IFeatureCollection Features => GenerateFeatures();

        public void Dispose()
        {
            return;
        }

        internal virtual IFeatureCollection GenerateFeatures()
        {
            var features = new FeatureCollection();
            features.Set<IServerAddressesFeature>(new TestServerAddressesFeature { Addresses = addresses });
            return features;
        }

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private class TestServerAddressesFeature : IServerAddressesFeature
    {
        public ICollection<string> Addresses { get; set; }
        public bool PreferHostingUrls { get; set; }
    }
}
