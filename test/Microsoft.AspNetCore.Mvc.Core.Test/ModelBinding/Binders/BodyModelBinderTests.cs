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
using Microsoft.Extensions.Logging.Testing;
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
        public async Task BindModel_NoValueResult_SetsModelStateError()
        {
            // Arrange
            var mockInputFormatter = new Mock<IInputFormatter>();
            mockInputFormatter.Setup(f => f.CanRead(It.IsAny<InputFormatterContext>()))
                .Returns(true);
            mockInputFormatter.Setup(o => o.ReadAsync(It.IsAny<InputFormatterContext>()))
                .Returns(InputFormatterResult.NoValueAsync());
            var inputFormatter = mockInputFormatter.Object;

            var provider = new TestModelMetadataProvider();
            provider.ForType<Person>().BindingDetails(d =>
            {
                d.BindingSource = BindingSource.Body;
                d.ModelBindingMessageProvider.SetMissingRequestBodyRequiredValueAccessor(
                    () => "Customized error message");
            });

            var bindingContext = GetBindingContext(
                typeof(Person),
                metadataProvider: provider);
            bindingContext.BinderModelName = "custom";

            var binder = CreateBinder(new[] { inputFormatter });

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(bindingContext.Result.Model);
            Assert.False(bindingContext.Result.IsModelSet);
            Assert.False(bindingContext.ModelState.IsValid);

            // Key is the bindermodelname because this was a top-level binding.
            var entry = Assert.Single(bindingContext.ModelState);
            Assert.Equal("custom", entry.Key);
            Assert.Equal("Customized error message", entry.Value.Errors.Single().ErrorMessage);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BindModel_PassesAllowEmptyInputOptionViaContext(bool treatEmptyInputAsDefaultValueOption)
        {
            // Arrange
            var mockInputFormatter = new Mock<IInputFormatter>();
            mockInputFormatter.Setup(f => f.CanRead(It.IsAny<InputFormatterContext>()))
                .Returns(true);
            mockInputFormatter.Setup(o => o.ReadAsync(It.IsAny<InputFormatterContext>()))
                .Returns(InputFormatterResult.NoValueAsync())
                .Verifiable();
            var inputFormatter = mockInputFormatter.Object;

            var provider = new TestModelMetadataProvider();
            provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

            var bindingContext = GetBindingContext(
                typeof(Person),
                metadataProvider: provider);
            bindingContext.BinderModelName = "custom";

            var binder = CreateBinder(new[] { inputFormatter }, treatEmptyInputAsDefaultValueOption);

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            mockInputFormatter.Verify(formatter => formatter.ReadAsync(
                It.Is<InputFormatterContext>(ctx => ctx.TreatEmptyInputAsDefaultValue == treatEmptyInputAsDefaultValueOption)),
                Times.Once);
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

        [Fact]
        public async Task BindModelAsync_LogsFormatterRejectionAndSelection()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);
            var inputFormatters = new List<IInputFormatter>()
            {
                new TestInputFormatter(canRead: false),
                new TestInputFormatter(canRead: true),
            };

            var provider = new TestModelMetadataProvider();
            provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);
            var bindingContext = GetBindingContext(typeof(Person), metadataProvider: provider);
            bindingContext.HttpContext.Request.ContentType = "application/json";
            var binder = new BodyModelBinder(inputFormatters, new TestHttpRequestStreamReaderFactory(), loggerFactory);

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Equal($"Rejected input formatter '{typeof(TestInputFormatter)}' for content type 'application/json'.", sink.Writes[0].State.ToString());
            Assert.Equal($"Selected input formatter '{typeof(TestInputFormatter)}' for content type 'application/json'.", sink.Writes[1].State.ToString());
        }

        [Fact]
        public async Task BindModelAsync_LogsNoFormatterSelectedAndRemoveFromBodyAttribute()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);
            var inputFormatters = new List<IInputFormatter>()
            {
                new TestInputFormatter(canRead: false),
                new TestInputFormatter(canRead: false),
            };

            var provider = new TestModelMetadataProvider();
            provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);
            var bindingContext = GetBindingContext(typeof(Person), metadataProvider: provider);
            bindingContext.HttpContext.Request.ContentType = "multipart/form-data";
            bindingContext.BinderModelName = bindingContext.ModelName;
            var binder = new BodyModelBinder(inputFormatters, new TestHttpRequestStreamReaderFactory(), loggerFactory);

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Collection(
                sink.Writes,
                write => Assert.Equal(
                    $"Rejected input formatter '{typeof(TestInputFormatter)}' for content type 'multipart/form-data'.", write.State.ToString()),
                write => Assert.Equal(
                    $"Rejected input formatter '{typeof(TestInputFormatter)}' for content type 'multipart/form-data'.", write.State.ToString()),
                write => Assert.Equal(
                    "No input formatter was found to support the content type 'multipart/form-data' for use with the [FromBody] attribute.", write.State.ToString()),
                write => Assert.Equal(
                    $"To use model binding, remove the [FromBody] attribute from the property or parameter named '{bindingContext.ModelName}' with model type '{bindingContext.ModelType}'.", write.State.ToString()));
        }

        [Fact]
        public async Task BindModelAsync_DoesNotThrowNullReferenceException()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var provider = new TestModelMetadataProvider();
            provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);
            var bindingContext = GetBindingContext(
                typeof(Person),
                httpContext: httpContext,
                metadataProvider: provider);
            var binder = new BodyModelBinder(new List<IInputFormatter>(), new TestHttpRequestStreamReaderFactory());

            // Act & Assert (does not throw)
            await binder.BindModelAsync(bindingContext);
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

        private static BodyModelBinder CreateBinder(IList<IInputFormatter> formatters, bool treatEmptyInputAsDefaultValueOption = false)
        {
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);
            var options = new MvcOptions { AllowEmptyInputInBodyModelBinding = treatEmptyInputAsDefaultValueOption };
            return new BodyModelBinder(formatters, new TestHttpRequestStreamReaderFactory(), loggerFactory, options);
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