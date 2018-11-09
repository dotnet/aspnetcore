// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class DefaultPageLoaderTest
    {
        [Fact]
        public void Load_InvokesApplicationModelProviders()
        {
            // Arrange
            var descriptor = new PageActionDescriptor();

            var compilerProvider = GetCompilerProvider();

            var razorPagesOptions = Options.Create(new RazorPagesOptions());
            var mvcOptions = Options.Create(new MvcOptions());

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
                providers,
                compilerProvider,
                razorPagesOptions,
                mvcOptions);

            // Act
            var result = loader.Load(new PageActionDescriptor());

            // Assert
            provider1.Verify();
            provider2.Verify();
        }

        [Fact]
        public void Load_InvokesApplicationModelProviders_WithTheRightOrder()
        {
            // Arrange
            var descriptor = new PageActionDescriptor();
            var compilerProvider = GetCompilerProvider();
            var razorPagesOptions = Options.Create(new RazorPagesOptions());
            var mvcOptions = Options.Create(new MvcOptions());

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
                providers,
                compilerProvider,
                razorPagesOptions,
                mvcOptions);

            // Act
            var result = loader.Load(new PageActionDescriptor());

            // Assert
            provider1.Verify();
            provider2.Verify();
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
            var conventionCollection = new PageConventionCollection
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
            var conventionCollection = new PageConventionCollection
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
            var conventionCollection = new PageConventionCollection();

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
            var conventionCollection = new PageConventionCollection { handlerModelConvention.Object };

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
            var conventionCollection = new PageConventionCollection();

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
            var conventionCollection = new PageConventionCollection();

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
            var conventionCollection = new PageConventionCollection { parameterModelConvention.Object };

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
            var conventionCollection = new PageConventionCollection();

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
            var conventionCollection = new PageConventionCollection();

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
            var conventionCollection = new PageConventionCollection { propertyModelConvention.Object };

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
            var conventionCollection = new PageConventionCollection();

            // Act
            DefaultPageLoader.ApplyConventions(conventionCollection, applicationModel);

            // Assert
            propertyModelConvention.Verify();
        }

        private static IViewCompilerProvider GetCompilerProvider()
        {
            var descriptor = new CompiledViewDescriptor
            {
                ViewAttribute = new RazorPageAttribute("/Views/Index.cshtml", typeof(object), null),
            };

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
