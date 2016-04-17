// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.ObjectPool;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public class JsonPatchInputFormatterTest
    {
        private static readonly ObjectPoolProvider _objectPoolProvider = new DefaultObjectPoolProvider();
        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings();

        [Fact]
        public async Task JsonPatchInputFormatter_ReadsOneOperation_Successfully()
        {
            // Arrange
            var logger = GetLogger();
            var formatter =
                new JsonPatchInputFormatter(logger, _serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider);
            var content = "[{\"op\":\"add\",\"path\":\"Customer/Name\",\"value\":\"John\"}]";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(JsonPatchDocument<Customer>));
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
            var patchDoc = Assert.IsType<JsonPatchDocument<Customer>>(result.Model);
            Assert.Equal("add", patchDoc.Operations[0].op);
            Assert.Equal("Customer/Name", patchDoc.Operations[0].path);
            Assert.Equal("John", patchDoc.Operations[0].value);
        }

        [Fact]
        public async Task JsonPatchInputFormatter_ReadsMultipleOperations_Successfully()
        {
            // Arrange
            var logger = GetLogger();
            var formatter =
                new JsonPatchInputFormatter(logger, _serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider);
            var content = "[{\"op\": \"add\", \"path\" : \"Customer/Name\", \"value\":\"John\"}," +
                "{\"op\": \"remove\", \"path\" : \"Customer/Name\"}]";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(JsonPatchDocument<Customer>));
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
            var patchDoc = Assert.IsType<JsonPatchDocument<Customer>>(result.Model);
            Assert.Equal("add", patchDoc.Operations[0].op);
            Assert.Equal("Customer/Name", patchDoc.Operations[0].path);
            Assert.Equal("John", patchDoc.Operations[0].value);
            Assert.Equal("remove", patchDoc.Operations[1].op);
            Assert.Equal("Customer/Name", patchDoc.Operations[1].path);
        }

        [Theory]
        [InlineData("application/json-patch+json", true)]
        [InlineData("application/json", false)]
        [InlineData("application/*", false)]
        [InlineData("*/*", false)]
        public void CanRead_ReturnsTrueOnlyForJsonPatchContentType(string requestContentType, bool expectedCanRead)
        {
            // Arrange
            var logger = GetLogger();
            var formatter =
                new JsonPatchInputFormatter(logger, _serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider);
            var content = "[{\"op\": \"add\", \"path\" : \"Customer/Name\", \"value\":\"John\"}]";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes, contentType: requestContentType);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(JsonPatchDocument<Customer>));
            var formatterContext = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = formatter.CanRead(formatterContext);

            // Assert
            Assert.Equal(expectedCanRead, result);
        }

        [Theory]
        [InlineData(typeof(Customer))]
        [InlineData(typeof(IJsonPatchDocument))]
        public void CanRead_ReturnsFalse_NonJsonPatchContentType(Type modelType)
        {
            // Arrange
            var logger = GetLogger();
            var formatter =
                new JsonPatchInputFormatter(logger, _serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider);
            var content = "[{\"op\": \"add\", \"path\" : \"Customer/Name\", \"value\":\"John\"}]";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes, contentType: "application/json-patch+json");
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(modelType);
            var formatterContext = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = formatter.CanRead(formatterContext);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task JsonPatchInputFormatter_ReturnsModelStateErrors_InvalidModelType()
        {
            // Arrange
            var exceptionMessage = "Cannot deserialize the current JSON array (e.g. [1,2,3]) into type " +
                $"'{typeof(Customer).FullName}' because the type requires a JSON object ";

            var logger = GetLogger();
            var formatter =
                new JsonPatchInputFormatter(logger, _serializerSettings, ArrayPool<char>.Shared, _objectPoolProvider);
            var content = "[{\"op\": \"add\", \"path\" : \"Customer/Name\", \"value\":\"John\"}]";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes, contentType: "application/json-patch+json");
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(Customer));
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
            Assert.Contains(exceptionMessage, modelState[""].Errors[0].Exception.Message);
        }

        private static ILogger GetLogger()
        {
            return NullLogger.Instance;
        }

        private static HttpContext GetHttpContext(
            byte[] contentBytes,
            string contentType = "application/json-patch+json")
        {
            var request = new Mock<HttpRequest>();
            var headers = new Mock<IHeaderDictionary>();
            request.SetupGet(r => r.Headers).Returns(headers.Object);
            request.SetupGet(f => f.Body).Returns(new MemoryStream(contentBytes));
            request.SetupGet(f => f.ContentType).Returns(contentType);

            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            return httpContext.Object;
        }

        private class Customer
        {
            public string Name { get; set; }
        }
    }
}