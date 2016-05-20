// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
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
                              .Returns(InputFormatterResult.SuccessAsync(new Person()))
                              .Verifiable();
            var inputFormatter = mockInputFormatter.Object;

            var provider = new TestModelMetadataProvider();
            provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

            var bindingContext = GetBindingContext(
                typeof(Person),
                metadataProvider: provider);

            var binder = CreateBinder(new[] { inputFormatter });

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            mockInputFormatter.Verify(v => v.CanRead(It.IsAny<InputFormatterContext>()), Times.Once);
            mockInputFormatter.Verify(v => v.ReadAsync(It.IsAny<InputFormatterContext>()), Times.Once);
            Assert.True(bindingContext.Result.IsModelSet);
        }

        [Fact]
        public async Task BindModel_NoInputFormatterFound_SetsModelStateError()
        {
            // Arrange
            var provider = new TestModelMetadataProvider();
            provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

            var bindingContext = GetBindingContext(typeof(Person), metadataProvider: provider);

            var binder = CreateBinder(new List<IInputFormatter>());

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.IsModelSet);
            Assert.Null(bindingContext.Result.Model);

            // Key is the empty string because this was a top-level binding.
            var entry = Assert.Single(bindingContext.ModelState);
            Assert.Equal(string.Empty, entry.Key);
            Assert.Single(entry.Value.Errors);
        }

        [Fact]
        public async Task BindModel_NoInputFormatterFound_SetsModelStateError_RespectsBinderModelName()
        {
            // Arrange
            var provider = new TestModelMetadataProvider();
            provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

            var bindingContext = GetBindingContext(typeof(Person), metadataProvider: provider);
            bindingContext.BinderModelName = "custom";

            var binder = CreateBinder(new List<IInputFormatter>());

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.IsModelSet);
            Assert.Null(bindingContext.Result.Model);

            // Key is the bindermodelname because this was a top-level binding.
            var entry = Assert.Single(bindingContext.ModelState);
            Assert.Equal("custom", entry.Key);
            Assert.Single(entry.Value.Errors);
        }

        [Fact]
        public async Task BindModel_IsGreedy()
        {
            // Arrange
            var provider = new TestModelMetadataProvider();
            provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

            var bindingContext = GetBindingContext(typeof(Person), metadataProvider: provider);

            var binder = CreateBinder(new List<IInputFormatter>());

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.IsModelSet);
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
                httpContext: httpContext,
                metadataProvider: provider);

            var binder = CreateBinder(new[] { new XyzFormatter() });

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.IsModelSet);
            Assert.Null(bindingContext.Result.Model);

            // Key is the empty string because this was a top-level binding.
            var entry = Assert.Single(bindingContext.ModelState);
            Assert.Equal(string.Empty, entry.Key);
            var errorMessage = Assert.Single(entry.Value.Errors).Exception.Message;
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
                httpContext: httpContext,
                metadataProvider: provider);

            var binder = CreateBinder(new List<IInputFormatter>());

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.IsModelSet);
            Assert.Null(bindingContext.Result.Model);

            // Key is the empty string because this was a top-level binding.
            var entry = Assert.Single(bindingContext.ModelState);
            Assert.Equal(string.Empty, entry.Key);
            var errorMessage = Assert.Single(entry.Value.Errors).Exception.Message;
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
            var bindingContext = GetBindingContext(typeof(Person), metadataProvider: provider);

            var binder = CreateBinder(inputFormatters);

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            Assert.Same(canReadFormatter1, bindingContext.Result.Model);
        }

        private static DefaultModelBindingContext GetBindingContext(
            Type modelType,
            HttpContext httpContext = null,
            IModelMetadataProvider metadataProvider = null)
        {
            if (httpContext == null)
            {
                httpContext = new DefaultHttpContext();
            }

            if (metadataProvider == null)
            {
                metadataProvider = new EmptyModelMetadataProvider();
            }

            var bindingContext = new DefaultModelBindingContext
            {
                ActionContext = new ActionContext()
                {
                    HttpContext = httpContext,
                },
                FieldName = "someField",
                IsTopLevelObject = true,
                ModelMetadata = metadataProvider.GetMetadataForType(modelType),
                ModelName = "someName",
                ValueProvider = Mock.Of<IValueProvider>(),
                ModelState = new ModelStateDictionary(),
                BindingSource = BindingSource.Body,
            };

            return bindingContext;
        }

        private static BodyModelBinder CreateBinder(IList<IInputFormatter> formatters)
        {
            return new BodyModelBinder(formatters, new TestHttpRequestStreamReaderFactory());
        }

        private class Person
        {
            public string Name { get; set; }
        }

        private class XyzFormatter : TextInputFormatter
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

            public override Task<InputFormatterResult> ReadRequestBodyAsync(
                InputFormatterContext context,
                Encoding effectiveEncoding)
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

            public Task<InputFormatterResult> ReadAsync(InputFormatterContext context)
            {
                return InputFormatterResult.SuccessAsync(this);
            }
        }
    }
}