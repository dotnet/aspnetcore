// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
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
using Microsoft.OpenApi.Extensions;
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

        var documentService = new OpenApiDocumentService("Test", apiDescriptionGroupCollectionProvider, hostEnvironment, openApiOptions.Object, builder.ServiceProvider);
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
            Template = action.MethodInfo.GetCustomAttribute<RouteAttribute>()?.Template
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
        internal OpenApiComponentService TestComponentService { get; set; } = new OpenApiComponentService(Options.Create(new Microsoft.AspNetCore.Http.Json.JsonOptions()));

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
