// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class BodyModelBinderTests
    {
        [Fact]
        public async Task BindModel_CallsSelectedInputFormatterOnce()
        {
            // Arrange
            var mockInputFormatter = new Mock<IInputFormatter>();
            mockInputFormatter.Setup(f => f.CanRead(It.IsAny<InputFormatterContext>()))
                .Returns(true)
                .Verifiable();
            mockInputFormatter.Setup(o => o.ReadAsync(It.IsAny<InputFormatterContext>()))
                              .Returns(Task.FromResult<object>(new Person()))
                              .Verifiable();
            var inputFormatter = mockInputFormatter.Object;

            var provider = new TestModelMetadataProvider();
            provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

            var bindingContext = GetBindingContext(
                typeof(Person),
                new[] { inputFormatter },
                metadataProvider: provider);

            var binder = new BodyModelBinder();

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            mockInputFormatter.Verify(v => v.CanRead(It.IsAny<InputFormatterContext>()), Times.Once);
            mockInputFormatter.Verify(v => v.ReadAsync(It.IsAny<InputFormatterContext>()), Times.Once);
            Assert.NotNull(binderResult);
            Assert.True(binderResult.IsModelSet);
            Assert.NotNull(binderResult.ValidationNode);
            Assert.True(binderResult.ValidationNode.ValidateAllProperties);
            Assert.False(binderResult.ValidationNode.SuppressValidation);
            Assert.Empty(binderResult.ValidationNode.ChildNodes);
            Assert.Equal(binderResult.Key, binderResult.ValidationNode.Key);
            Assert.Equal(bindingContext.ModelMetadata, binderResult.ValidationNode.ModelMetadata);
            Assert.Same(binderResult.Model, binderResult.ValidationNode.Model);
        }

        [Fact]
        public async Task BindModel_NoInputFormatterFound_SetsModelStateError()
        {
            // Arrange
            var provider = new TestModelMetadataProvider();
            provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

            var bindingContext = GetBindingContext(typeof(Person), metadataProvider: provider);

            var binder = bindingContext.OperationBindingContext.ModelBinder;

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert

            // Returns true because it understands the metadata type.
            Assert.NotNull(binderResult);
            Assert.False(binderResult.IsModelSet);
            Assert.Null(binderResult.ValidationNode);
            Assert.Null(binderResult.Model);
            Assert.True(bindingContext.ModelState.ContainsKey("someName"));
        }

        [Fact]
        public async Task BindModel_IsGreedy()
        {
            // Arrange
            var provider = new TestModelMetadataProvider();
            provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

            var bindingContext = GetBindingContext(typeof(Person), metadataProvider: provider);

            var binder = bindingContext.OperationBindingContext.ModelBinder;

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(binderResult);
            Assert.False(binderResult.IsModelSet);
            Assert.Null(binderResult.ValidationNode);
        }

        [Fact]
        public async Task BindModel_IsGreedy_IgnoresWrongSource()
        {
            // Arrange
            var provider = new TestModelMetadataProvider();
            provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Header);

            var bindingContext = GetBindingContext(typeof(Person), metadataProvider: provider);
            bindingContext.BindingSource = BindingSource.Header;

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
            var provider = new TestModelMetadataProvider();
            provider.ForType<Person>().BindingDetails(d => d.BindingSource = null);

            var bindingContext = GetBindingContext(typeof(Person), metadataProvider: provider);
            bindingContext.BindingSource = null;

            var binder = bindingContext.OperationBindingContext.ModelBinder;

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(binderResult);
        }

        [Fact]
        public async Task CustomFormatterDeserializationException_AddedToModelState()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("Bad data!"));
            httpContext.Request.ContentType = "text/xyz";

            var provider = new TestModelMetadataProvider();
            provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

            var bindingContext = GetBindingContext(
                typeof(Person),
                inputFormatters: new[] { new XyzFormatter() },
                httpContext: httpContext,
                metadataProvider: provider);

            var binder = bindingContext.OperationBindingContext.ModelBinder;

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert

            // Returns true because it understands the metadata type.
            Assert.NotNull(binderResult);
            Assert.False(binderResult.IsModelSet);
            Assert.Null(binderResult.ValidationNode);
            Assert.Null(binderResult.Model);
            Assert.True(bindingContext.ModelState.ContainsKey("someName"));
            var errorMessage = bindingContext.ModelState["someName"].Errors[0].Exception.Message;
            Assert.Equal("Your input is bad!", errorMessage);
        }

        [Fact]
        public async Task NullFormatterError_AddedToModelState()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "text/xyz";

            var provider = new TestModelMetadataProvider();
            provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

            var bindingContext = GetBindingContext(
                typeof(Person),
                inputFormatters: null,
                httpContext: httpContext,
                metadataProvider: provider);

            var binder = bindingContext.OperationBindingContext.ModelBinder;

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert

            // Returns true because it understands the metadata type.
            Assert.NotNull(binderResult);
            Assert.False(binderResult.IsModelSet);
            Assert.Null(binderResult.Model);
            Assert.Null(binderResult.ValidationNode);
            Assert.True(bindingContext.ModelState.ContainsKey("someName"));
            var errorMessage = bindingContext.ModelState["someName"].Errors[0].ErrorMessage;
            Assert.Equal("Unsupported content type 'text/xyz'.", errorMessage);
        }

        [Fact]
        public async Task BindModelCoreAsync_UsesFirstFormatterWhichCanRead()
        {
            // Arrange
            var canReadFormatter1 = new TestInputFormatter(canRead: true);
            var canReadFormatter2 = new TestInputFormatter(canRead: true);
            var inputFormatters = new List<IInputFormatter>()
            {
                new TestInputFormatter(canRead: false),
                new TestInputFormatter(canRead: false),
                canReadFormatter1,
                canReadFormatter2
            };
            var provider = new TestModelMetadataProvider();
            provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);
            var bindingContext = GetBindingContext(typeof(Person), inputFormatters, metadataProvider: provider);
            var binder = bindingContext.OperationBindingContext.ModelBinder;

            // Act
            var binderResult = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(binderResult.IsModelSet);
            Assert.Same(canReadFormatter1, binderResult.Model);
        }

        private static ModelBindingContext GetBindingContext(
            Type modelType,
            IEnumerable<IInputFormatter> inputFormatters = null,
            HttpContext httpContext = null,
            IModelMetadataProvider metadataProvider = null)
        {
            if (httpContext == null)
            {
                httpContext = new DefaultHttpContext();
            }

            if (inputFormatters == null)
            {
                inputFormatters = Enumerable.Empty<IInputFormatter>();
            }

            if (metadataProvider == null)
            {
                metadataProvider = new EmptyModelMetadataProvider();
            }

            var operationBindingContext = new OperationBindingContext
            {
                InputFormatters = inputFormatters.ToList(),
                ModelBinder = new BodyModelBinder(),
                MetadataProvider = metadataProvider,
                HttpContext = httpContext,
            };

            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(modelType),
                ModelName = "someName",
                ValueProvider = Mock.Of<IValueProvider>(),
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = operationBindingContext,
                BindingSource = BindingSource.Body,
            };

            return bindingContext;
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

        private class XyzFormatter : InputFormatter
        {
            public XyzFormatter()
            {
                SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/xyz"));
                SupportedEncodings.Add(Encoding.UTF8);
            }

            protected override bool CanReadType(Type type)
            {
                return true;
            }

            public override Task<object> ReadRequestBodyAsync(InputFormatterContext context)
            {
                throw new InvalidOperationException("Your input is bad!");
            }
        }

        private class TestInputFormatter : IInputFormatter
        {
            private readonly bool _canRead;

            public TestInputFormatter(bool canRead)
            {
                _canRead = canRead;
            }

            public bool CanRead(InputFormatterContext context)
            {
                return _canRead;
            }

            public Task<object> ReadAsync(InputFormatterContext context)
            {
                return Task.FromResult<object>(this);
            }
        }
    }
}