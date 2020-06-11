// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class DefaultPageLoaderTest
    {
        private readonly IOptions<RazorPagesOptions> RazorPagesOptions = Options.Create(new RazorPagesOptions { Conventions = new PageConventionCollection(Mock.Of<IServiceProvider>()) });
        private readonly IActionDescriptorCollectionProvider ActionDescriptorCollectionProvider;

        public DefaultPageLoaderTest()
        {
            var actionDescriptors = new ActionDescriptorCollection(Array.Empty<ActionDescriptor>(), 1);
            ActionDescriptorCollectionProvider = Mock.Of<IActionDescriptorCollectionProvider>(v => v.ActionDescriptors == actionDescriptors);
        }

        [Fact]
        public async Task LoadAsync_InvokesApplicationModelProviders()
        {
            // Arrange
            var descriptor = new PageActionDescriptor();

            var compilerProvider = GetCompilerProvider();

            var mvcOptions = Options.Create(new MvcOptions());
            var endpointFactory = new ActionEndpointFactory(Mock.Of<RoutePatternTransformer>());

            var provider1 = new Mock<IPageApplicationModelProvider>();
            var provider2 = new Mock<IPageApplicationModelProvider>();

            var sequence = 0;
            var pageApplicationModel1 = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());
            var pageApplicationModel2 = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());

            provider1.Setup(p => p.OnProvidersExecuting(It.IsAny<PageApplicationModelProviderContext>()))
                .Callback((PageApplicationModelProviderContext c) =>
                {
                    Assert.Equal(0, sequence++);
                    Assert.Null(c.PageApplicationModel);
                    c.PageApplicationModel = pageApplicationModel1;
                })
                .Verifiable();

            provider2.Setup(p => p.OnProvidersExecuting(It.IsAny<PageApplicationModelProviderContext>()))
                .Callback((PageApplicationModelProviderContext c) =>
                {
                    Assert.Equal(1, sequence++);
                    Assert.Same(pageApplicationModel1, c.PageApplicationModel);
                    c.PageApplicationModel = pageApplicationModel2;
                })
                .Verifiable();

            provider1.Setup(p => p.OnProvidersExecuted(It.IsAny<PageApplicationModelProviderContext>()))
                .Callback((PageApplicationModelProviderContext c) =>
                {
                    Assert.Equal(3, sequence++);
                    Assert.Same(pageApplicationModel2, c.PageApplicationModel);
                })
                .Verifiable();

            provider2.Setup(p => p.OnProvidersExecuted(It.IsAny<PageApplicationModelProviderContext>()))
                .Callback((PageApplicationModelProviderContext c) =>
                {
                    Assert.Equal(2, sequence++);
                    Assert.Same(pageApplicationModel2, c.PageApplicationModel);
                })
                .Verifiable();

            var providers = new[]
            {
                provider1.Object, provider2.Object
            };

            var loader = new DefaultPageLoader(
                ActionDescriptorCollectionProvider,
                providers,
                compilerProvider,
                endpointFactory,
                RazorPagesOptions,
                mvcOptions);

            // Act
            var result = await loader.LoadAsync(new PageActionDescriptor());

            // Assert
            provider1.Verify();
            provider2.Verify();
        }

        [Fact]
        public async Task LoadAsync_CreatesEndpoint_WithRoute()
        {
            // Arrange
            var descriptor = new PageActionDescriptor()
            {
                AttributeRouteInfo = new AttributeRouteInfo()
                {
                    Template = "/test",
                },
            };

            var transformer = new Mock<RoutePatternTransformer>();
            transformer
                .Setup(t => t.SubstituteRequiredValues(It.IsAny<RoutePattern>(), It.IsAny<object>()))
                .Returns<RoutePattern, object>((p, v) => p);

            var compilerProvider = GetCompilerProvider();

            var mvcOptions = Options.Create(new MvcOptions());
            var endpointFactory = new ActionEndpointFactory(transformer.Object);

            var provider = new Mock<IPageApplicationModelProvider>();

            var pageApplicationModel = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());

            provider.Setup(p => p.OnProvidersExecuting(It.IsAny<PageApplicationModelProviderContext>()))
                .Callback((PageApplicationModelProviderContext c) =>
                {
                    Assert.Null(c.PageApplicationModel);
                    c.PageApplicationModel = pageApplicationModel;
                })
                .Verifiable();

            var providers = new[]
            {
                provider.Object,
            };

            var loader = new DefaultPageLoader(
                ActionDescriptorCollectionProvider,
                providers,
                compilerProvider,
                endpointFactory,
                RazorPagesOptions,
                mvcOptions);

            // Act
            var result = await loader.LoadAsync(descriptor);

            // Assert
            Assert.NotNull(result.Endpoint);
        }

        [Fact]
        public async Task LoadAsync_InvokesApplicationModelProviders_WithTheRightOrder()
        {
            // Arrange
            var descriptor = new PageActionDescriptor();
            var compilerProvider = GetCompilerProvider();
            var mvcOptions = Options.Create(new MvcOptions());
            var endpointFactory = new ActionEndpointFactory(Mock.Of<RoutePatternTransformer>());

            var provider1 = new Mock<IPageApplicationModelProvider>();
            provider1.SetupGet(p => p.Order).Returns(10);
            var provider2 = new Mock<IPageApplicationModelProvider>();
            provider2.SetupGet(p => p.Order).Returns(-5);

            var sequence = 0;
            provider1.Setup(p => p.OnProvidersExecuting(It.IsAny<PageApplicationModelProviderContext>()))
                .Callback((PageApplicationModelProviderContext c) =>
                {
                    Assert.Equal(1, sequence++);
                    c.PageApplicationModel = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());
                })
                .Verifiable();

            provider2.Setup(p => p.OnProvidersExecuting(It.IsAny<PageApplicationModelProviderContext>()))
                .Callback((PageApplicationModelProviderContext c) =>
                {
                    Assert.Equal(0, sequence++);
                })
                .Verifiable();

            provider1.Setup(p => p.OnProvidersExecuted(It.IsAny<PageApplicationModelProviderContext>()))
                .Callback((PageApplicationModelProviderContext c) =>
                {
                    Assert.Equal(2, sequence++);
                })
                .Verifiable();

            provider2.Setup(p => p.OnProvidersExecuted(It.IsAny<PageApplicationModelProviderContext>()))
                .Callback((PageApplicationModelProviderContext c) =>
                {
                    Assert.Equal(3, sequence++);
                })
                .Verifiable();

            var providers = new[]
            {
                provider1.Object, provider2.Object
            };

            var loader = new DefaultPageLoader(
                ActionDescriptorCollectionProvider,
                providers,
                compilerProvider,
                endpointFactory,
                RazorPagesOptions,
                mvcOptions);

            // Act
            var result = await loader.LoadAsync(new PageActionDescriptor());

            // Assert
            provider1.Verify();
            provider2.Verify();
        }

        [Fact]
        public async Task LoadAsync_CachesResults()
        {
            // Arrange
            var descriptor = new PageActionDescriptor()
            {
                AttributeRouteInfo = new AttributeRouteInfo()
                {
                    Template = "/test",
                },
            };

            var transformer = new Mock<RoutePatternTransformer>();
            transformer
                .Setup(t => t.SubstituteRequiredValues(It.IsAny<RoutePattern>(), It.IsAny<object>()))
                .Returns<RoutePattern, object>((p, v) => p);

            var compilerProvider = GetCompilerProvider();

            var mvcOptions = Options.Create(new MvcOptions());
            var endpointFactory = new ActionEndpointFactory(transformer.Object);

            var provider = new Mock<IPageApplicationModelProvider>();

            var pageApplicationModel = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());

            provider.Setup(p => p.OnProvidersExecuting(It.IsAny<PageApplicationModelProviderContext>()))
                .Callback((PageApplicationModelProviderContext c) =>
                {
                    Assert.Null(c.PageApplicationModel);
                    c.PageApplicationModel = pageApplicationModel;
                })
                .Verifiable();

            var providers = new[]
            {
                provider.Object,
            };

            var loader = new DefaultPageLoader(
                ActionDescriptorCollectionProvider,
                providers,
                compilerProvider,
                endpointFactory,
                RazorPagesOptions,
                mvcOptions);

            // Act
            var result1 = await loader.LoadAsync(descriptor);
            var result2 = await loader.LoadAsync(descriptor);

            // Assert
            Assert.Same(result1, result2);
        }

        [Fact]
        public async Task LoadAsync_UpdatesResults()
        {
            // Arrange
            var descriptor = new PageActionDescriptor()
            {
                AttributeRouteInfo = new AttributeRouteInfo()
                {
                    Template = "/test",
                },
            };

            var transformer = new Mock<RoutePatternTransformer>();
            transformer
                .Setup(t => t.SubstituteRequiredValues(It.IsAny<RoutePattern>(), It.IsAny<object>()))
                .Returns<RoutePattern, object>((p, v) => p);

            var compilerProvider = GetCompilerProvider();

            var mvcOptions = Options.Create(new MvcOptions());
            var endpointFactory = new ActionEndpointFactory(transformer.Object);

            var provider = new Mock<IPageApplicationModelProvider>();

            var pageApplicationModel = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());

            provider.Setup(p => p.OnProvidersExecuting(It.IsAny<PageApplicationModelProviderContext>()))
                .Callback((PageApplicationModelProviderContext c) =>
                {
                    Assert.Null(c.PageApplicationModel);
                    c.PageApplicationModel = pageApplicationModel;
                })
                .Verifiable();

            var providers = new[]
            {
                provider.Object,
            };

            var descriptorCollection1 = new ActionDescriptorCollection(new[] { descriptor }, version: 1);
            var descriptorCollection2 = new ActionDescriptorCollection(new[] { descriptor }, version: 2);

            var actionDescriptorCollectionProvider = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorCollectionProvider
                .SetupSequence(p => p.ActionDescriptors)
                .Returns(descriptorCollection1)
                .Returns(descriptorCollection2);

            var loader = new DefaultPageLoader(
                actionDescriptorCollectionProvider.Object,
                providers,
                compilerProvider,
                endpointFactory,
                RazorPagesOptions,
                mvcOptions);

            // Act
            var result1 = await loader.LoadAsync(descriptor);
            var result2 = await loader.LoadAsync(descriptor);

            // Assert
            Assert.NotSame(result1, result2);
        }

        [Fact]
        public void ApplyConventions_InvokesApplicationModelConventions()
        {
            // Arrange
            var descriptor = new PageActionDescriptor();
            var model = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());

            var convention = new Mock<IPageApplicationModelConvention>();
            convention.Setup(c => c.Apply(It.IsAny<PageApplicationModel>()))
                .Callback((PageApplicationModel m) =>
                {
                    Assert.Same(model, m);
                })
                .Verifiable();
            var conventionCollection = new PageConventionCollection(Mock.Of<IServiceProvider>())
            {
                convention.Object,
            };

            // Act
            DefaultPageLoader.ApplyConventions(conventionCollection, model);

            // Assert
            convention.Verify();
        }

        [Fact]
        public void ApplyConventions_InvokesApplicationModelConventions_SpecifiedOnHandlerType()
        {
            // Arrange
            var descriptor = new PageActionDescriptor();
            var handlerConvention = new Mock<IPageApplicationModelConvention>();
            var model = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), new[] { handlerConvention.Object });

            var globalConvention = new Mock<IPageApplicationModelConvention>();
            globalConvention.Setup(c => c.Apply(It.IsAny<PageApplicationModel>()))
                .Callback((PageApplicationModel m) =>
                {
                    Assert.Same(model, m);
                })
                .Verifiable();

            handlerConvention.Setup(c => c.Apply(It.IsAny<PageApplicationModel>()))
                .Callback((PageApplicationModel m) =>
                {
                    Assert.Same(model, m);
                })
                .Verifiable();
            var conventionCollection = new PageConventionCollection(Mock.Of<IServiceProvider>())
            {
                globalConvention.Object,
            };

            // Act
            DefaultPageLoader.ApplyConventions(conventionCollection, model);

            // Assert
            globalConvention.Verify();
            handlerConvention.Verify();
        }

        [Fact]
        public void ApplyConventions_InvokesHandlerModelConventions()
        {
            // Arrange
            var descriptor = new PageActionDescriptor();
            var methodInfo = GetType().GetMethod(nameof(OnGet), BindingFlags.Instance | BindingFlags.NonPublic);
            var handlerModelConvention = new Mock<IPageHandlerModelConvention>();

            var applicationModel = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());
            var handlerModel = new PageHandlerModel(methodInfo, new[] { handlerModelConvention.Object });

            applicationModel.HandlerMethods.Add(handlerModel);

            handlerModelConvention.Setup(p => p.Apply(It.IsAny<PageHandlerModel>()))
                .Callback((PageHandlerModel m) =>
                {
                    Assert.Same(handlerModel, m);
                })
                .Verifiable();
            var conventionCollection = new PageConventionCollection(Mock.Of<IServiceProvider>());

            // Act
            DefaultPageLoader.ApplyConventions(conventionCollection, applicationModel);

            // Assert
            handlerModelConvention.Verify();
        }

        [Fact]
        public void ApplyConventions_InvokesHandlerModelConventions_DefinedGlobally()
        {
            // Arrange
            var descriptor = new PageActionDescriptor();
            var methodInfo = GetType().GetMethod(nameof(OnGet), BindingFlags.Instance | BindingFlags.NonPublic);

            var applicationModel = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());
            var handlerModel = new PageHandlerModel(methodInfo, Array.Empty<object>());
            applicationModel.HandlerMethods.Add(handlerModel);

            var handlerModelConvention = new Mock<IPageHandlerModelConvention>();
            handlerModelConvention.Setup(p => p.Apply(It.IsAny<PageHandlerModel>()))
                .Callback((PageHandlerModel m) =>
                {
                    Assert.Same(handlerModel, m);
                })
                .Verifiable();
            var conventionCollection = new PageConventionCollection(Mock.Of<IServiceProvider>()) { handlerModelConvention.Object };

            // Act
            DefaultPageLoader.ApplyConventions(conventionCollection, applicationModel);

            // Assert
            handlerModelConvention.Verify();
        }

        [Fact]
        public void ApplyConventions_RemovingHandlerAsPartOfHandlerModelConvention_Works()
        {
            // Arrange
            var descriptor = new PageActionDescriptor();
            var methodInfo = GetType().GetMethod(nameof(OnGet), BindingFlags.Instance | BindingFlags.NonPublic);
            var handlerModelConvention = new Mock<IPageHandlerModelConvention>();

            var applicationModel = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());
            var handlerModel = new PageHandlerModel(methodInfo, new[] { handlerModelConvention.Object })
            {
                Page = applicationModel,
            };

            applicationModel.HandlerMethods.Add(handlerModel);

            handlerModelConvention.Setup(p => p.Apply(It.IsAny<PageHandlerModel>()))
                .Callback((PageHandlerModel m) =>
                {
                    m.Page.HandlerMethods.Remove(m);
                })
                .Verifiable();
            var conventionCollection = new PageConventionCollection(Mock.Of<IServiceProvider>());

            // Act
            DefaultPageLoader.ApplyConventions(conventionCollection, applicationModel);

            // Assert
            handlerModelConvention.Verify();
        }

        [Fact]
        public void ApplyConventions_InvokesParameterModelConventions()
        {
            // Arrange
            var descriptor = new PageActionDescriptor();
            var methodInfo = GetType().GetMethod(nameof(OnGet), BindingFlags.Instance | BindingFlags.NonPublic);
            var parameterInfo = methodInfo.GetParameters()[0];

            var applicationModel = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());
            var handlerModel = new PageHandlerModel(methodInfo, Array.Empty<object>());
            var parameterModelConvention = new Mock<IParameterModelBaseConvention>();
            var parameterModel = new PageParameterModel(parameterInfo, new[] { parameterModelConvention.Object });

            applicationModel.HandlerMethods.Add(handlerModel);
            handlerModel.Parameters.Add(parameterModel);

            parameterModelConvention.Setup(p => p.Apply(It.IsAny<ParameterModelBase>()))
                .Callback((ParameterModelBase m) =>
                {
                    Assert.Same(parameterModel, m);
                })
                .Verifiable();
            var conventionCollection = new PageConventionCollection(Mock.Of<IServiceProvider>());

            // Act
            DefaultPageLoader.ApplyConventions(conventionCollection, applicationModel);

            // Assert
            parameterModelConvention.Verify();
        }

        [Fact]
        public void ApplyConventions_InvokesParameterModelConventions_DeclaredGlobally()
        {
            // Arrange
            var descriptor = new PageActionDescriptor();
            var methodInfo = GetType().GetMethod(nameof(OnGet), BindingFlags.Instance | BindingFlags.NonPublic);
            var parameterInfo = methodInfo.GetParameters()[0];

            var applicationModel = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());
            var handlerModel = new PageHandlerModel(methodInfo, Array.Empty<object>());
            var parameterModel = new PageParameterModel(parameterInfo, Array.Empty<object>());

            applicationModel.HandlerMethods.Add(handlerModel);
            handlerModel.Parameters.Add(parameterModel);

            var parameterModelConvention = new Mock<IParameterModelBaseConvention>();
            parameterModelConvention.Setup(p => p.Apply(It.IsAny<ParameterModelBase>()))
                .Callback((ParameterModelBase m) =>
                {
                    Assert.Same(parameterModel, m);
                })
                .Verifiable();
            var conventionCollection = new PageConventionCollection(Mock.Of<IServiceProvider>()) { parameterModelConvention.Object };

            // Act
            DefaultPageLoader.ApplyConventions(conventionCollection, applicationModel);

            // Assert
            parameterModelConvention.Verify();
        }

        [Fact]
        public void ApplyConventions_RemovingParameterModelAsPartOfConventionWorks()
        {
            // Arrange
            var descriptor = new PageActionDescriptor();
            var methodInfo = GetType().GetMethod(nameof(OnGet), BindingFlags.Instance | BindingFlags.NonPublic);
            var parameterInfo = methodInfo.GetParameters()[0];

            var applicationModel = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());
            var handlerModel = new PageHandlerModel(methodInfo, Array.Empty<object>());
            var parameterModelConvention = new Mock<IParameterModelBaseConvention>();
            var parameterModel = new PageParameterModel(parameterInfo, new[] { parameterModelConvention.Object })
            {
                Handler = handlerModel,
            };

            applicationModel.HandlerMethods.Add(handlerModel);
            handlerModel.Parameters.Add(parameterModel);

            parameterModelConvention.Setup(p => p.Apply(It.IsAny<ParameterModelBase>()))
                .Callback((ParameterModelBase m) =>
                {
                    var model = Assert.IsType<PageParameterModel>(m);
                    model.Handler.Parameters.Remove(model);
                })
                .Verifiable();
            var conventionCollection = new PageConventionCollection(Mock.Of<IServiceProvider>());

            // Act
            DefaultPageLoader.ApplyConventions(conventionCollection, applicationModel);

            // Assert
            parameterModelConvention.Verify();
        }

        [Fact]
        public void ApplyConventions_InvokesPropertyModelConventions()
        {
            // Arrange
            var descriptor = new PageActionDescriptor();
            var methodInfo = GetType().GetMethod(nameof(OnGet), BindingFlags.Instance | BindingFlags.NonPublic);
            var propertyInfo = GetType().GetProperty(nameof(TestProperty), BindingFlags.Instance | BindingFlags.NonPublic);
            var parameterInfo = methodInfo.GetParameters()[0];

            var applicationModel = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());
            var handlerModel = new PageHandlerModel(methodInfo, Array.Empty<object>());
            var parameterModel = new PageParameterModel(parameterInfo, Array.Empty<object>());
            var propertyModelConvention = new Mock<IParameterModelBaseConvention>();
            var propertyModel = new PagePropertyModel(propertyInfo, new[] { propertyModelConvention.Object });

            applicationModel.HandlerMethods.Add(handlerModel);
            applicationModel.HandlerProperties.Add(propertyModel);
            handlerModel.Parameters.Add(parameterModel);

            propertyModelConvention.Setup(p => p.Apply(It.IsAny<ParameterModelBase>()))
                .Callback((ParameterModelBase m) =>
                {
                    Assert.Same(propertyModel, m);
                })
                .Verifiable();
            var conventionCollection = new PageConventionCollection(Mock.Of<IServiceProvider>());

            // Act
            DefaultPageLoader.ApplyConventions(conventionCollection, applicationModel);

            // Assert
            propertyModelConvention.Verify();
        }

        [Fact]
        public void ApplyConventions_InvokesPropertyModelConventions_DeclaredGlobally()
        {
            // Arrange
            var descriptor = new PageActionDescriptor();
            var methodInfo = GetType().GetMethod(nameof(OnGet), BindingFlags.Instance | BindingFlags.NonPublic);
            var propertyInfo = GetType().GetProperty(nameof(TestProperty), BindingFlags.Instance | BindingFlags.NonPublic);

            var applicationModel = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());
            var handlerModel = new PageHandlerModel(methodInfo, Array.Empty<object>());
            var propertyModel = new PagePropertyModel(propertyInfo, Array.Empty<object>());

            applicationModel.HandlerMethods.Add(handlerModel);
            applicationModel.HandlerProperties.Add(propertyModel);

            var propertyModelConvention = new Mock<IParameterModelBaseConvention>();
            propertyModelConvention.Setup(p => p.Apply(It.IsAny<ParameterModelBase>()))
                .Callback((ParameterModelBase m) =>
                {
                    Assert.Same(propertyModel, m);
                })
                .Verifiable();
            var conventionCollection = new PageConventionCollection(Mock.Of<IServiceProvider>()) { propertyModelConvention.Object };

            // Act
            DefaultPageLoader.ApplyConventions(conventionCollection, applicationModel);

            // Assert
            propertyModelConvention.Verify();
        }

        [Fact]
        public void ApplyConventions_RemovingPropertyModelAsPartOfConvention_Works()
        {
            // Arrange
            var descriptor = new PageActionDescriptor();
            var propertyInfo = GetType().GetProperty(nameof(TestProperty), BindingFlags.Instance | BindingFlags.NonPublic);

            var applicationModel = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), Array.Empty<object>());
            var propertyModelConvention = new Mock<IParameterModelBaseConvention>();
            var propertyModel = new PagePropertyModel(propertyInfo, new[] { propertyModelConvention.Object })
            {
                Page = applicationModel,
            };

            applicationModel.HandlerProperties.Add(propertyModel);

            propertyModelConvention.Setup(p => p.Apply(It.IsAny<ParameterModelBase>()))
                .Callback((ParameterModelBase m) =>
                {
                    var model = Assert.IsType<PagePropertyModel>(m);
                    model.Page.HandlerProperties.Remove(model);
                })
                .Verifiable();
            var conventionCollection = new PageConventionCollection(Mock.Of<IServiceProvider>());

            // Act
            DefaultPageLoader.ApplyConventions(conventionCollection, applicationModel);

            // Assert
            propertyModelConvention.Verify();
        }

        private static IViewCompilerProvider GetCompilerProvider()
        {
            var compiledItem = TestRazorCompiledItem.CreateForView(typeof(object), "/Views/Index.cshtml");
            var descriptor = new CompiledViewDescriptor(compiledItem);
            var compiler = new Mock<IViewCompiler>();
            compiler.Setup(c => c.CompileAsync(It.IsAny<string>()))
                .ReturnsAsync(descriptor);
            var compilerProvider = new Mock<IViewCompilerProvider>();
            compilerProvider.Setup(p => p.GetCompiler())
                .Returns(compiler.Object);
            return compilerProvider.Object;
        }

        private void OnGet(string parameter)
        {
        }

        private string TestProperty { get; set; }
    }
}
