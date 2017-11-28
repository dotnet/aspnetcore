// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.TestCommon;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public class JsonInputFormatterTest
    {
        private static readonly ObjectPoolProvider _objectPoolProvider = new DefaultObjectPoolProvider();
        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings();

        [Fact]
        public async Task BuffersRequestBody_ByDefault()
        {
            // Arrange
            var content = "{name: 'Person Name', Age: '30'}";
            var logger = GetLogger();
#pragma warning disable CS0618
            var formatter =
                new JsonInputFormatter(logger, _serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider);
#pragma warning restore CS0618
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = new DefaultHttpContext();
            httpContext.Features.Set<IHttpResponseFeature>(new TestResponseFeature());
            httpContext.Request.Body = new NonSeekableReadStream(contentBytes);
            httpContext.Request.ContentType = "application/json";
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(User));
            var context = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.False(result.HasError);
            var userModel = Assert.IsType<User>(result.Model);
            Assert.Equal("Person Name", userModel.Name);
            Assert.Equal(30, userModel.Age);

            Assert.True(httpContext.Request.Body.CanSeek);
            httpContext.Request.Body.Seek(0L, SeekOrigin.Begin);

            result = await formatter.ReadAsync(context);

            // Assert
            Assert.False(result.HasError);
            userModel = Assert.IsType<User>(result.Model);
            Assert.Equal("Person Name", userModel.Name);
            Assert.Equal(30, userModel.Age);
        }

        [Fact]
        public async Task BuffersRequestBody_UsingDefaultOptions()
        {
            // Arrange
            var content = "{name: 'Person Name', Age: '30'}";
            var logger = GetLogger();
            var formatter = new JsonInputFormatter(
                logger,
                _serializerSettings,
                ArrayPool<char>.Shared,
                _objectPoolProvider,
                new MvcOptions());
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = new DefaultHttpContext();
            httpContext.Features.Set<IHttpResponseFeature>(new TestResponseFeature());
            httpContext.Request.Body = new NonSeekableReadStream(contentBytes);
            httpContext.Request.ContentType = "application/json";
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(User));
            var context = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.False(result.HasError);
            var userModel = Assert.IsType<User>(result.Model);
            Assert.Equal("Person Name", userModel.Name);
            Assert.Equal(30, userModel.Age);

            Assert.True(httpContext.Request.Body.CanSeek);
            httpContext.Request.Body.Seek(0L, SeekOrigin.Begin);

            result = await formatter.ReadAsync(context);

            // Assert
            Assert.False(result.HasError);
            userModel = Assert.IsType<User>(result.Model);
            Assert.Equal("Person Name", userModel.Name);
            Assert.Equal(30, userModel.Age);
        }

        [Fact]
        public async Task SuppressInputFormatterBufferingSetToTrue_DoesNotBufferRequestBody()
        {
            // Arrange
            var content = "{name: 'Person Name', Age: '30'}";
            var logger = GetLogger();
#pragma warning disable CS0618
            var formatter =
                new JsonInputFormatter(logger, _serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider, suppressInputFormatterBuffering: true);
#pragma warning restore CS0618
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = new DefaultHttpContext();
            httpContext.Features.Set<IHttpResponseFeature>(new TestResponseFeature());
            httpContext.Request.Body = new NonSeekableReadStream(contentBytes);
            httpContext.Request.ContentType = "application/json";
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(User));
            var context = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.False(result.HasError);
            var userModel = Assert.IsType<User>(result.Model);
            Assert.Equal("Person Name", userModel.Name);
            Assert.Equal(30, userModel.Age);

            Assert.False(httpContext.Request.Body.CanSeek);
            result = await formatter.ReadAsync(context);

            // Assert
            Assert.False(result.HasError);
            Assert.Null(result.Model);
        }

        [Fact]
        public async Task SuppressInputFormatterBufferingSetToTrue_UsingMvcOptions_DoesNotBufferRequestBody()
        {
            // Arrange
            var content = "{name: 'Person Name', Age: '30'}";
            var logger = GetLogger();
            var mvcOptions = new MvcOptions();
            mvcOptions.SuppressInputFormatterBuffering = true;
            var formatter = new JsonInputFormatter(
                logger,
                _serializerSettings,
                ArrayPool<char>.Shared,
                _objectPoolProvider,
                mvcOptions);
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = new DefaultHttpContext();
            httpContext.Features.Set<IHttpResponseFeature>(new TestResponseFeature());
            httpContext.Request.Body = new NonSeekableReadStream(contentBytes);
            httpContext.Request.ContentType = "application/json";
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(User));
            var context = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.False(result.HasError);
            var userModel = Assert.IsType<User>(result.Model);
            Assert.Equal("Person Name", userModel.Name);
            Assert.Equal(30, userModel.Age);

            Assert.False(httpContext.Request.Body.CanSeek);
            result = await formatter.ReadAsync(context);

            // Assert
            Assert.False(result.HasError);
            Assert.Null(result.Model);
        }

        [Fact]
        public async Task SuppressInputFormatterBufferingSetToTrue_UsingMutatedOptions()
        {
            // Arrange
            var content = "{name: 'Person Name', Age: '30'}";
            var logger = GetLogger();
            var mvcOptions = new MvcOptions();
            mvcOptions.SuppressInputFormatterBuffering = false;
            var formatter = 
                new JsonInputFormatter(logger, _serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider, mvcOptions);
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = new DefaultHttpContext();
            httpContext.Features.Set<IHttpResponseFeature>(new TestResponseFeature());
            httpContext.Request.Body = new NonSeekableReadStream(contentBytes);
            httpContext.Request.ContentType = "application/json";
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(User));
            var context = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            // Mutate options after passing into the constructor to make sure that the value type is not store in the constructor
            mvcOptions.SuppressInputFormatterBuffering = true;
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.False(result.HasError);
            var userModel = Assert.IsType<User>(result.Model);
            Assert.Equal("Person Name", userModel.Name);
            Assert.Equal(30, userModel.Age);

            Assert.False(httpContext.Request.Body.CanSeek);
            result = await formatter.ReadAsync(context);

            // Assert
            Assert.False(result.HasError);
            Assert.Null(result.Model);
        }

        [Theory]
        [InlineData("application/json", true)]
        [InlineData("application/*", false)]
        [InlineData("*/*", false)]
        [InlineData("text/json", true)]
        [InlineData("text/*", false)]
        [InlineData("text/xml", false)]
        [InlineData("application/xml", false)]
        [InlineData("application/some.entity+json", true)]
        [InlineData("application/some.entity+json;v=2", true)]
        [InlineData("application/some.entity+xml", false)]
        [InlineData("application/some.entity+*", false)]
        [InlineData("text/some.entity+json", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("invalid", false)]
        public void CanRead_ReturnsTrueForAnySupportedContentType(string requestContentType, bool expectedCanRead)
        {
            // Arrange
            var loggerMock = GetLogger();

            var formatter =
                new JsonInputFormatter(loggerMock, _serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider, new MvcOptions());
            var contentBytes = Encoding.UTF8.GetBytes("content");

            var httpContext = GetHttpContext(contentBytes, contentType: requestContentType);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(string));
            var formatterContext = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: new ModelStateDictionary(),
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = formatter.CanRead(formatterContext);

            // Assert
            Assert.Equal(expectedCanRead, result);
        }

        [Fact]
        public void DefaultMediaType_ReturnsApplicationJson()
        {
            // Arrange
            var loggerMock = GetLogger();
            var formatter =
                new JsonInputFormatter(loggerMock, _serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider, new MvcOptions());

            // Act
            var mediaType = formatter.SupportedMediaTypes[0];

            // Assert
            Assert.Equal("application/json", mediaType.ToString());
        }

        public static IEnumerable<object[]> JsonFormatterReadSimpleTypesData
        {
            get
            {
                yield return new object[] { "100", typeof(int), 100 };
                yield return new object[] { "'abcd'", typeof(string), "abcd" };
                yield return new object[] { "'2012-02-01 12:45 AM'", typeof(DateTime),
                                            new DateTime(2012, 02, 01, 00, 45, 00) };
            }
        }

        [Theory]
        [MemberData(nameof(JsonFormatterReadSimpleTypesData))]
        public async Task JsonFormatterReadsSimpleTypes(string content, Type type, object expected)
        {
            // Arrange
            var logger = GetLogger();
            var formatter =
                new JsonInputFormatter(logger, _serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider, new MvcOptions());
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var httpContext = GetHttpContext(contentBytes);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(type);
            var context = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: new ModelStateDictionary(),
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.False(result.HasError);
            Assert.Equal(expected, result.Model);
        }

        [Fact]
        public async Task JsonFormatterReadsComplexTypes()
        {
            // Arrange
            var content = "{name: 'Person Name', Age: '30'}";
            var logger = GetLogger();
            var formatter =
                new JsonInputFormatter(logger, _serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider, new MvcOptions());
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var httpContext = GetHttpContext(contentBytes);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(User));
            var context = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: new ModelStateDictionary(),
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.False(result.HasError);
            var userModel = Assert.IsType<User>(result.Model);
            Assert.Equal("Person Name", userModel.Name);
            Assert.Equal(30, userModel.Age);
        }

        [Fact]
        public async Task ReadAsync_ReadsValidArray()
        {
            // Arrange
            var content = "[0, 23, 300]";
            var logger = GetLogger();
            var formatter =
                new JsonInputFormatter(logger, _serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider, new MvcOptions());
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(int[]));
            var context = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.False(result.HasError);
            var integers = Assert.IsType<int[]>(result.Model);
            Assert.Equal(new int[] { 0, 23, 300 }, integers);
        }

        [Theory]
        [InlineData(typeof(ICollection<int>))]
        [InlineData(typeof(IEnumerable<int>))]
        [InlineData(typeof(IList<int>))]
        [InlineData(typeof(List<int>))]
        public async Task ReadAsync_ReadsValidArray_AsList(Type requestedType)
        {
            // Arrange
            var content = "[0, 23, 300]";
            var logger = GetLogger();
            var formatter =
                new JsonInputFormatter(logger, _serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider, new MvcOptions());
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(requestedType);
            var context = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.False(result.HasError);
            var integers = Assert.IsType<List<int>>(result.Model);
            Assert.Equal(new int[] { 0, 23, 300 }, integers);
        }

        [Fact]
        public async Task ReadAsync_AddsModelValidationErrorsToModelState()
        {
            // Arrange
            var content = "{name: 'Person Name', Age: 'not-an-age'}";
            var logger = GetLogger();
            var formatter =
                new JsonInputFormatter(logger, _serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider, new MvcOptions());
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(User));
            var context = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.True(result.HasError);
            Assert.Equal(
                "Could not convert string to decimal: not-an-age. Path 'Age', line 1, position 39.",
                modelState["Age"].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task ReadAsync_InvalidArray_AddsOverflowErrorsToModelState()
        {
            // Arrange
            var content = "[0, 23, 300]";
            var logger = GetLogger();
            var formatter =
                new JsonInputFormatter(logger, _serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider, new MvcOptions());
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(byte[]));
            var context = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.True(result.HasError);
            Assert.Equal("The supplied value is invalid.", modelState["[2]"].Errors[0].ErrorMessage);
            Assert.Null(modelState["[2]"].Errors[0].Exception);
        }

        [Fact]
        public async Task ReadAsync_InvalidComplexArray_AddsOverflowErrorsToModelState()
        {
            // Arrange
            var content = "[{name: 'Name One', Age: 30}, {name: 'Name Two', Small: 300}]";
            var logger = GetLogger();
            var formatter =
                new JsonInputFormatter(logger, _serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider, new MvcOptions());
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(User[]));
            var context = new InputFormatterContext(
                httpContext,
                modelName: "names",
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.True(result.HasError);
            Assert.Equal(
                "Error converting value 300 to type 'System.Byte'. Path '[1].Small', line 1, position 59.",
                modelState["names[1].Small"].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task ReadAsync_UsesTryAddModelValidationErrorsToModelState()
        {
            // Arrange
            var content = "{name: 'Person Name', Age: 'not-an-age'}";
            var logger = GetLogger();
            var formatter =
                new JsonInputFormatter(logger, _serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider, new MvcOptions());
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(User));
            var context = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            modelState.MaxAllowedErrors = 3;
            modelState.AddModelError("key1", "error1");
            modelState.AddModelError("key2", "error2");

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.True(result.HasError);
            Assert.False(modelState.ContainsKey("age"));
            var error = Assert.Single(modelState[""].Errors);
            Assert.IsType<TooManyModelErrorsException>(error.Exception);
        }

        [Theory]
        [InlineData("null", true, true)]
        [InlineData("null", false, false)]
        [InlineData(" ", true, true)]
        [InlineData(" ", false, false)]
        public async Task ReadAsync_WithInputThatDeserializesToNull_SetsModelOnlyIfAllowingEmptyInput(string content, bool allowEmptyInput, bool expectedIsModelSet)
        {
            // Arrange
            var logger = GetLogger();
            var formatter =
                new JsonInputFormatter(logger, _serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider, new MvcOptions());
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(object));
            var context = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader,
                treatEmptyInputAsDefaultValue: allowEmptyInput);

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.False(result.HasError);
            Assert.Equal(expectedIsModelSet, result.IsModelSet);
            Assert.Null(result.Model);
        }

        [Fact]
        public void Constructor_UsesSerializerSettings()
        {
            // Arrange
            var serializerSettings = new JsonSerializerSettings();

            // Act
            var jsonFormatter = new TestableJsonInputFormatter(serializerSettings);

            // Assert
            Assert.Same(serializerSettings, jsonFormatter.SerializerSettings);
        }

        [Fact]
        public async Task CustomSerializerSettingsObject_TakesEffect()
        {
            // Arrange
            // missing password property here
            var contentBytes = Encoding.UTF8.GetBytes("{ \"UserName\" : \"John\"}");
            var logger = GetLogger();

            // by default we ignore missing members, so here explicitly changing it
            var serializerSettings = new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error };
            var jsonFormatter =
                new JsonInputFormatter(logger, serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider, new MvcOptions());

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes, "application/json;charset=utf-8");
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(UserLogin));
            var inputFormatterContext = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await jsonFormatter.ReadAsync(inputFormatterContext);

            // Assert
            Assert.True(result.HasError);
            Assert.False(modelState.IsValid);

            var modelErrorMessage = modelState.Values.First().Errors[0].ErrorMessage;
            Assert.Contains("Required property 'Password' not found in JSON", modelErrorMessage);
        }

        [Fact]
        public void CreateJsonSerializer_UsesJsonSerializerSettings()
        {
            // Arrange
            var settings = new JsonSerializerSettings
            {
                ContractResolver = Mock.Of<IContractResolver>(),
                MaxDepth = 2,
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
            };
            var formatter = new TestableJsonInputFormatter(settings);

            // Act
            var actual = formatter.CreateJsonSerializer();

            // Assert
            Assert.Same(settings.ContractResolver, actual.ContractResolver);
            Assert.Equal(settings.MaxDepth, actual.MaxDepth);
            Assert.Equal(settings.DateTimeZoneHandling, actual.DateTimeZoneHandling);
        }

        [Theory]
        [InlineData("{", "", "Unexpected end when reading JSON. Path '', line 1, position 1.")]
        [InlineData("{\"a\":{\"b\"}}", "a", "Invalid character after parsing property name. Expected ':' but got: }. Path 'a', line 1, position 9.")]
        [InlineData("{\"age\":\"x\"}", "age", "Could not convert string to decimal: x. Path 'age', line 1, position 10.")]
        [InlineData("{\"login\":1}", "login", "Error converting value 1 to type 'Microsoft.AspNetCore.Mvc.Formatters.JsonInputFormatterTest+UserLogin'. Path 'login', line 1, position 10.")]
        [InlineData("{\"login\":{\"username\":\"somevalue\"}}", "login", "Required property 'Password' not found in JSON. Path 'login', line 1, position 33.")]
        public async Task ReadAsync_RegistersJsonInputExceptionsAsInputFormatterException(
            string content,
            string modelStateKey,
            string expectedMessage)
        {
            // Arrange
            var logger = GetLogger();
            var formatter =
                new JsonInputFormatter(logger, _serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider, new MvcOptions());
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(User));
            var context = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.True(result.HasError);
            Assert.True(!modelState.IsValid);
            Assert.True(modelState.ContainsKey(modelStateKey));

            var modelError = modelState[modelStateKey].Errors.Single();
            Assert.Equal(expectedMessage, modelError.ErrorMessage);
        }

        [Fact]
        public async Task ReadAsync_WhenSuppressJsonDeserializationExceptionMessagesIsTrue_DoesNotWrapJsonInputExceptions()
        {
            // Arrange
            var logger = GetLogger();
            var formatter = new JsonInputFormatter(
                logger,
                _serializerSettings,
                ArrayPool<char>.Shared,
                _objectPoolProvider,
                new MvcOptions()
                {
                    SuppressInputFormatterBuffering = false,
                    SuppressJsonDeserializationExceptionMessagesInModelState = true
                });
            var contentBytes = Encoding.UTF8.GetBytes("{");
            var modelStateKey = string.Empty;

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(User));
            var context = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.True(result.HasError);
            Assert.True(!modelState.IsValid);
            Assert.True(modelState.ContainsKey(modelStateKey));

            var modelError = modelState[modelStateKey].Errors.Single();
            Assert.IsNotType<InputFormatterException>(modelError.Exception);
            Assert.Empty(modelError.ErrorMessage);
        }

        private class TestableJsonInputFormatter : JsonInputFormatter
        {
            public TestableJsonInputFormatter(JsonSerializerSettings settings)
                : base(GetLogger(), settings, ArrayPool<char>.Shared, _objectPoolProvider, new MvcOptions())
            {
            }

            public new JsonSerializerSettings SerializerSettings => base.SerializerSettings;

            public new JsonSerializer CreateJsonSerializer() => base.CreateJsonSerializer();
        }

        private static ILogger GetLogger()
        {
            return NullLogger.Instance;
        }

        private static HttpContext GetHttpContext(
            byte[] contentBytes,
            string contentType = "application/json")
        {
            return GetHttpContext(new MemoryStream(contentBytes), contentType);
        }

        private static HttpContext GetHttpContext(
            Stream requestStream,
            string contentType = "application/json")
        {
            var request = new Mock<HttpRequest>();
            var headers = new Mock<IHeaderDictionary>();
            request.SetupGet(r => r.Headers).Returns(headers.Object);
            request.SetupGet(f => f.Body).Returns(requestStream);
            request.SetupGet(f => f.ContentType).Returns(contentType);

            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            return httpContext.Object;
        }

        private IEnumerable<string> GetModelStateErrorMessages(ModelStateDictionary modelStateDictionary)
        {
            var allErrorMessages = new List<string>();
            foreach (var keyModelStatePair in modelStateDictionary)
            {
                var key = keyModelStatePair.Key;
                var errors = keyModelStatePair.Value.Errors;
                if (errors != null && errors.Count > 0)
                {
                    foreach (var modelError in errors)
                    {
                        if (string.IsNullOrEmpty(modelError.ErrorMessage))
                        {
                            if (modelError.Exception != null)
                            {
                                allErrorMessages.Add(modelError.Exception.Message);
                            }
                        }
                        else
                        {
                            allErrorMessages.Add(modelError.ErrorMessage);
                        }
                    }
                }
            }

            return allErrorMessages;
        }

        private sealed class User
        {
            public string Name { get; set; }

            public decimal Age { get; set; }

            public byte Small { get; set; }

            public UserLogin Login { get; set; }
        }

        private sealed class UserLogin
        {
            [JsonProperty(Required = Required.Always)]
            public string UserName { get; set; }

            [JsonProperty(Required = Required.Always)]
            public string Password { get; set; }
        }

        private class Location
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        private class TestResponseFeature : HttpResponseFeature
        {
            public override void OnCompleted(Func<object, Task> callback, object state)
            {
                // do not do anything
            }
        }
    }
}
