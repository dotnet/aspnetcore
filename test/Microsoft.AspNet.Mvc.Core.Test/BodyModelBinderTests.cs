// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class BodyModelBinderTests
    {
        [Fact]
        public async Task BindModel_CallsSelectedInputFormatterOnce()
        {
            // Arrange
            var mockInputFormatter = new Mock<IInputFormatter>();
            mockInputFormatter.Setup(o => o.ReadAsync(It.IsAny<InputFormatterContext>()))
                              .Returns(Task.FromResult<object>(new Person()))
                              .Verifiable();

            var bindingContext = GetBindingContext(typeof(Person), inputFormatter: mockInputFormatter.Object);
            bindingContext.ModelMetadata.BinderMetadata = new FromBodyAttribute();

            var binder = GetBodyBinder(mockInputFormatter.Object);

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            mockInputFormatter.Verify(v => v.ReadAsync(It.IsAny<InputFormatterContext>()), Times.Once);
        }

        [Fact]
        public async Task BindModel_NoInputFormatterFound_SetsModelStateError()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(Person), inputFormatter: null);
            bindingContext.ModelMetadata.BinderMetadata = new FromBodyAttribute();

            var binder = bindingContext.OperationBindingContext.ModelBinder;

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert

            // Returns true because it understands the metadata type.
            Assert.NotNull(binderResult);
            Assert.False(binderResult.IsModelSet);
            Assert.Null(binderResult.Model);
            Assert.True(bindingContext.ModelState.ContainsKey("someName"));
        }

        [Fact]
        public async Task BindModel_IsGreedy()
        {
            // Arrange
            var metadata = new Mock<IBindingSourceMetadata>();
            metadata.SetupGet(m => m.BindingSource).Returns(BindingSource.Body);

            var bindingContext = GetBindingContext(typeof(Person), inputFormatter: null);
            bindingContext.ModelMetadata.BinderMetadata = metadata.Object;

            var binder = bindingContext.OperationBindingContext.ModelBinder;

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(binderResult);
            Assert.False(binderResult.IsModelSet);
        }

        [Fact]
        public async Task BindModel_IsGreedy_IgnoresWrongSource()
        {
            // Arrange
            var metadata = new Mock<IBindingSourceMetadata>();
            metadata.SetupGet(m => m.BindingSource).Returns(BindingSource.Header);

            var bindingContext = GetBindingContext(typeof(Person), inputFormatter: null);
            bindingContext.ModelMetadata.BinderMetadata = metadata.Object;

            var binder = bindingContext.OperationBindingContext.ModelBinder;

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(binderResult);
        }

        [Fact]
        public async Task BindModel_IsGreedy_IgnoresMetadataWithNoSource()
        {
            // Arrange
            var metadata = new Mock<IBindingSourceMetadata>();
            metadata.SetupGet(m => m.BindingSource).Returns((BindingSource)null);

            var bindingContext = GetBindingContext(typeof(Person), inputFormatter: null);
            bindingContext.ModelMetadata.BinderMetadata = metadata.Object;

            var binder = bindingContext.OperationBindingContext.ModelBinder;

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(binderResult);
        }

        private static ModelBindingContext GetBindingContext(Type modelType, IInputFormatter inputFormatter)
        {
            var metadataProvider = new EmptyModelMetadataProvider();
            var operationBindingContext = new OperationBindingContext
            {
                ModelBinder = GetBodyBinder(inputFormatter),
                MetadataProvider = metadataProvider,
                HttpContext = new DefaultHttpContext(),
            };

            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(modelType),
                ModelName = "someName",
                ValueProvider = Mock.Of<IValueProvider>(),
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = operationBindingContext,
            };

            return bindingContext;
        }

        private static BodyModelBinder GetBodyBinder(IInputFormatter inputFormatter)
        {
            var actionContext = CreateActionContext(new DefaultHttpContext());
            var inputFormatterSelector = new Mock<IInputFormatterSelector>();
            inputFormatterSelector
                .Setup(o => o.SelectFormatter(
                    It.IsAny<IReadOnlyList<IInputFormatter>>(),
                    It.IsAny<InputFormatterContext>()))
                .Returns(inputFormatter);

            var bodyValidationPredicatesProvider = new Mock<IValidationExcludeFiltersProvider>();
            bodyValidationPredicatesProvider.SetupGet(o => o.ExcludeFilters)
                                             .Returns(new List<IExcludeTypeValidationFilter>());

            var bindingContext = new ActionBindingContext()
            {
                InputFormatters = new List<IInputFormatter>(),
            };

            var bindingContextAccessor = new MockScopedInstance<ActionBindingContext>()
            {
                Value = bindingContext,
            };

            var binder = new BodyModelBinder(
                actionContext,
                bindingContextAccessor,
                inputFormatterSelector.Object,
                bodyValidationPredicatesProvider.Object);

            return binder;
        }

        private static IScopedInstance<ActionContext> CreateActionContext(HttpContext context)
        {
            return CreateActionContext(context, (new Mock<IRouter>()).Object);
        }

        private static IScopedInstance<ActionContext> CreateActionContext(HttpContext context, IRouter router)
        {
            var routeData = new RouteData();
            routeData.Routers.Add(router);

            var actionContext = new ActionContext(context,
                                                  routeData,
                                                  new ActionDescriptor());
            var contextAccessor = new Mock<IScopedInstance<ActionContext>>();
            contextAccessor.SetupGet(c => c.Value)
                           .Returns(actionContext);
            return contextAccessor.Object;
        }

        private class Person
        {
            public string Name { get; set; }
        }
    }
}