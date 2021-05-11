// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class CompiledPageActionDescriptorFactoryTest
    {
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
            CompiledPageActionDescriptorFactory.ApplyConventions(conventionCollection, model);

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
            CompiledPageActionDescriptorFactory.ApplyConventions(conventionCollection, model);

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
            CompiledPageActionDescriptorFactory.ApplyConventions(conventionCollection, applicationModel);

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
            CompiledPageActionDescriptorFactory.ApplyConventions(conventionCollection, applicationModel);

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
            CompiledPageActionDescriptorFactory.ApplyConventions(conventionCollection, applicationModel);

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
            CompiledPageActionDescriptorFactory.ApplyConventions(conventionCollection, applicationModel);

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
            CompiledPageActionDescriptorFactory.ApplyConventions(conventionCollection, applicationModel);

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
            CompiledPageActionDescriptorFactory.ApplyConventions(conventionCollection, applicationModel);

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
            CompiledPageActionDescriptorFactory.ApplyConventions(conventionCollection, applicationModel);

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
            CompiledPageActionDescriptorFactory.ApplyConventions(conventionCollection, applicationModel);

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
            CompiledPageActionDescriptorFactory.ApplyConventions(conventionCollection, applicationModel);

            // Assert
            propertyModelConvention.Verify();
        }

        private void OnGet(string parameter)
        {
        }

        private string TestProperty { get; set; }
    }
}
