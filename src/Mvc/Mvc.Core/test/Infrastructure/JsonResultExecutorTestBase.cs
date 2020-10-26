// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public abstract class JsonResultExecutorTestBase
    {
        [Fact]
        public async Task ExecuteAsync_UsesDefaultContentType_IfNoContentTypeSpecified()
        {
            // Arrange
            var expected = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { foo = "abcd" }));

            var context = GetActionContext();

            var result = new JsonResult(new { foo = "abcd" });
            var executor = CreateExecutor();

            // Act
            await executor.ExecuteAsync(context, result);

            // Assert
            var written = GetWrittenBytes(context.HttpContext);
            Assert.Equal(expected, written);
            Assert.Equal("application/json; charset=utf-8", context.HttpContext.Response.ContentType);
        }

        [Fact]
        public async Task ExecuteAsync_NullEncoding_DoesNotSetCharsetOnContentType()
        {
            // Arrange
            var expected = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { foo = "abcd" }));

            var context = GetActionContext();

            var result = new JsonResult(new { foo = "abcd" });
            result.ContentType = "text/json";
            var executor = CreateExecutor();

            // Act
            await executor.ExecuteAsync(context, result);

            // Assert
            var written = GetWrittenBytes(context.HttpContext);
            Assert.Equal(expected, written);
            Assert.Equal("text/json", context.HttpContext.Response.ContentType);
        }

        [Fact]
        public async Task ExecuteAsync_UsesEncodingSpecifiedInContentType()
        {
            // Arrange
            var expected = Encoding.Unicode.GetBytes(JsonSerializer.Serialize(new { foo = "abcd" }));

            var context = GetActionContext();
            context.HttpContext.Response.ContentType = "text/json; charset=utf-8";

            var result = new JsonResult(new { foo = "abcd" })
            {
                ContentType = "text/json; charset=utf-16",
            };
            var executor = CreateExecutor();

            // Act
            await executor.ExecuteAsync(context, result);

            // Assert
            var written = GetWrittenBytes(context.HttpContext);
            Assert.Equal(expected, written);
            Assert.Equal("text/json; charset=utf-16", context.HttpContext.Response.ContentType);
        }

        [Fact]
        public async Task ExecuteAsync_UsesEncodingSpecifiedInResponseContentType()
        {
            // Arrange
            var expected = Encoding.Unicode.GetBytes(JsonSerializer.Serialize(new { foo = "abcd" }));

            var context = GetActionContext();
            context.HttpContext.Response.ContentType = "text/json; charset=utf-16";
            var result = new JsonResult(new { foo = "abcd" });
            var executor = CreateExecutor();

            // Act
            await executor.ExecuteAsync(context, result);

            // Assert
            var written = GetWrittenBytes(context.HttpContext);
            Assert.Equal(expected, written);
            Assert.Equal("text/json; charset=utf-16", context.HttpContext.Response.ContentType);
        }

        [Fact]
        public async Task ExecuteAsync_SetsContentTypeAndEncoding()
        {
            // Arrange
            var expected = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { foo = "abcd" }));

            var context = GetActionContext();

            var result = new JsonResult(new { foo = "abcd" });
            result.ContentType = new MediaTypeHeaderValue("text/json")
            {
                Encoding = Encoding.ASCII
            }.ToString();
            var executor = CreateExecutor();

            // Act
            await executor.ExecuteAsync(context, result);

            // Assert
            var written = GetWrittenBytes(context.HttpContext);
            Assert.Equal(expected, written);
            Assert.Equal("text/json; charset=us-ascii", context.HttpContext.Response.ContentType);
        }

        [Fact]
        public async Task ExecuteAsync_NoResultContentTypeSet_UsesResponseContentType_AndSuppliedEncoding()
        {
            // Arrange
            var expected = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { foo = "abcd" }));
            var expectedContentType = "text/foo; p1=p1-value; charset=us-ascii";

            var context = GetActionContext();
            context.HttpContext.Response.ContentType = expectedContentType;

            var result = new JsonResult(new { foo = "abcd" });
            var executor = CreateExecutor();

            // Act
            await executor.ExecuteAsync(context, result);

            // Assert
            var written = GetWrittenBytes(context.HttpContext);
            Assert.Equal(expected, written);
            Assert.Equal(expectedContentType, context.HttpContext.Response.ContentType);
        }

        [Theory]
        [InlineData("text/foo", "text/foo")]
        [InlineData("text/foo; p1=p1-value", "text/foo; p1=p1-value")]
        public async Task ExecuteAsync_NoResultContentTypeSet_UsesDefaultEncoding_DoesNotSetCharset(
            string responseContentType,
            string expectedContentType)
        {
            // Arrange
            var expected = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { foo = "abcd" }));

            var context = GetActionContext();
            context.HttpContext.Response.ContentType = responseContentType;

            var result = new JsonResult(new { foo = "abcd" });
            var executor = CreateExecutor();

            // Act
            await executor.ExecuteAsync(context, result);

            // Assert
            var written = GetWrittenBytes(context.HttpContext);
            Assert.Equal(expected, written);
            Assert.Equal(expectedContentType, context.HttpContext.Response.ContentType);
        }

        [Fact]
        public async Task ExecuteAsync_UsesPassedInSerializerSettings()
        {
            // Arrange
            var expected = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                new { foo = "abcd" },
                new JsonSerializerOptions { WriteIndented = true }));

            var context = GetActionContext();

            var serializerSettings = GetIndentedSettings();

            var result = new JsonResult(new { foo = "abcd" }, serializerSettings);
            var executor = CreateExecutor();

            // Act
            await executor.ExecuteAsync(context, result);

            // Assert
            var written = GetWrittenBytes(context.HttpContext);
            Assert.Equal(expected, written);
            Assert.Equal("application/json; charset=utf-8", context.HttpContext.Response.ContentType);
        }

        protected abstract object GetIndentedSettings();

        [Fact]
        public async Task ExecuteAsync_ErrorDuringSerialization_DoesNotWriteContent()
        {
            // Arrange
            var context = GetActionContext();
            var result = new JsonResult(new ModelWithSerializationError());
            var executor = CreateExecutor();

            // Act
            try
            {
                await executor.ExecuteAsync(context, result);
            }
            catch (NotImplementedException ex)
            {
                Assert.Equal("Property Age has not been implemented", ex.Message);
            }
            catch (Exception serializerException)
            {
                var expectedException = Assert.IsType<NotImplementedException>(serializerException.InnerException);
                Assert.Equal("Property Age has not been implemented", expectedException.Message);
            }

            // Assert
            var written = GetWrittenBytes(context.HttpContext);
            Assert.Empty(written);
        }

        [Fact]
        public async Task ExecuteAsync_NonNullResult_LogsResultType()
        {
            // Arrange
            var expected = "Executing JsonResult, writing value of type 'System.String'.";
            var context = GetActionContext();
            var sink = new TestSink();
            var executor = CreateExecutor(new TestLoggerFactory(sink, enabled: true));
            var result = new JsonResult("result_value");

            // Act
            await executor.ExecuteAsync(context, result);

            // Assert
            var write = Assert.Single(sink.Writes);
            Assert.Equal(expected, write.State.ToString());
        }

        [Fact]
        public async Task ExecuteAsync_NullResult_LogsNull()
        {
            // Arrange
            var expected = "Executing JsonResult, writing value of type 'null'.";
            var context = GetActionContext();
            var sink = new TestSink();
            var executor = CreateExecutor(new TestLoggerFactory(sink, enabled: true));
            var result = new JsonResult(null);

            // Act
            await executor.ExecuteAsync(context, result);

            // Assert
            var write = Assert.Single(sink.Writes);
            Assert.Equal(expected, write.State.ToString());
        }

        [Fact]
        public async Task ExecuteAsync_LargePayload_DoesNotPerformSynchronousWrites()
        {
            // Arrange
            var model = Enumerable.Range(0, 1000).Select(p => new TestModel { Property = new string('a', 5000) }).ToArray();

            var stream = new Mock<Stream>();
            stream.Setup(v => v.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            stream.SetupGet(s => s.CanWrite).Returns(true);
            var context = GetActionContext();
            context.HttpContext.Response.Body = stream.Object;

            var executor = CreateExecutor();
            var result = new JsonResult(model);

            // Act
            await executor.ExecuteAsync(context, result);

            // Assert
            stream.Verify(v => v.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never());
            stream.Verify(v => v.Flush(), Times.Never());
        }

        [Fact]
        public async Task ExecuteAsync_ThrowsIfSerializerSettingIsNotTheCorrectType()
        {
            // Arrange
            var context = GetActionContext();

            var result = new JsonResult(new { foo = "abcd" }, new object());
            var executor = CreateExecutor();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => executor.ExecuteAsync(context, result));

            // Assert
            Assert.StartsWith("Property 'JsonResult.SerializerSettings' must be an instance of type", ex.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithNullValue()
        {
            // Arrange
            var expected = Encoding.UTF8.GetBytes("null");

            var context = GetActionContext();
            var result = new JsonResult(value: null);
            var executor = CreateExecutor();

            // Act
            await executor.ExecuteAsync(context, result);

            // Assert
            var written = GetWrittenBytes(context.HttpContext);
            Assert.Equal(expected, written);
        }

        [Fact]
        public async Task ExecuteAsync_SerializesAsyncEnumerables()
        {
            // Arrange
            var expected = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[] { "Hello", "world" }));

            var context = GetActionContext();
            var result = new JsonResult(TestAsyncEnumerable());
            var executor = CreateExecutor();

            // Act
            await executor.ExecuteAsync(context, result);

            // Assert
            var written = GetWrittenBytes(context.HttpContext);
            Assert.Equal(expected, written);
        }

        [Fact]
        public async Task ExecuteAsync_SerializesAsyncEnumerablesOfPrimtives()
        {
            // Arrange
            var expected = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[] { 1, 2 }));

            var context = GetActionContext();
            var result = new JsonResult(TestAsyncPrimitiveEnumerable());
            var executor = CreateExecutor();

            // Act
            await executor.ExecuteAsync(context, result);

            // Assert
            var written = GetWrittenBytes(context.HttpContext);
            Assert.Equal(expected, written);
        }

        protected IActionResultExecutor<JsonResult> CreateExecutor() => CreateExecutor(NullLoggerFactory.Instance);

        protected abstract IActionResultExecutor<JsonResult> CreateExecutor(ILoggerFactory loggerFactory);

        private static HttpContext GetHttpContext()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();

            var services = new ServiceCollection();
            services.AddOptions();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            httpContext.RequestServices = services.BuildServiceProvider();

            return httpContext;
        }

        private static ActionContext GetActionContext()
        {
            return new ActionContext(GetHttpContext(), new RouteData(), new ActionDescriptor());
        }

        private static byte[] GetWrittenBytes(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            return Assert.IsType<MemoryStream>(context.Response.Body).ToArray();
        }

        private class ModelWithSerializationError
        {
            public string Name { get; } = "Robert";
            public int Age
            {
                get
                {
                    throw new NotImplementedException($"Property {nameof(Age)} has not been implemented");
                }
            }
        }

        private class TestModel
        {
            public string Property { get; set; }
        }

        private async IAsyncEnumerable<string> TestAsyncEnumerable()
        {
            await Task.Yield();
            yield return "Hello";
            yield return "world";
        }

        private async IAsyncEnumerable<int> TestAsyncPrimitiveEnumerable()
        {
            await Task.Yield();
            yield return 1;
            yield return 2;
        }
    }
}
