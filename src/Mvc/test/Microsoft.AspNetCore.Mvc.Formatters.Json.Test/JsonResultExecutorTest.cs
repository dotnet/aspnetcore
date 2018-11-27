// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public class JsonResultExecutorTest
    {
        [Fact]
        public async Task ExecuteAsync_UsesDefaultContentType_IfNoContentTypeSpecified()
        {
            // Arrange
            var expected = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { foo = "abcd" }));

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
            var expected = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { foo = "abcd" }));

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
        public async Task ExecuteAsync_SetsContentTypeAndEncoding()
        {
            // Arrange
            var expected = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { foo = "abcd" }));

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
            var expected = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { foo = "abcd" }));
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
            var expected = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { foo = "abcd" }));

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
            var expected = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
                new { foo = "abcd" },
                Formatting.Indented));

            var context = GetActionContext();

            var serializerSettings = new JsonSerializerSettings();
            serializerSettings.Formatting = Formatting.Indented;

            var result = new JsonResult(new { foo = "abcd" }, serializerSettings);
            var executor = CreateExecutor();

            // Act
            await executor.ExecuteAsync(context, result);

            // Assert
            var written = GetWrittenBytes(context.HttpContext);
            Assert.Equal(expected, written);
            Assert.Equal("application/json; charset=utf-8", context.HttpContext.Response.ContentType);
        }

        [Fact]
        public async Task ExecuteAsync_ErrorDuringSerialization_DoesNotCloseTheBrackets()
        {
            // Arrange
            var expected = Encoding.UTF8.GetBytes("{\"name\":\"Robert\"");
            var context = GetActionContext();
            var result = new JsonResult(new ModelWithSerializationError());
            var executor = CreateExecutor();

            // Act
            try
            {
                await executor.ExecuteAsync(context, result);
            }
            catch (JsonSerializationException serializerException)
            {
                var expectedException = Assert.IsType<NotImplementedException>(serializerException.InnerException);
                Assert.Equal("Property Age has not been implemented", expectedException.Message);
            }

            // Assert
            var written = GetWrittenBytes(context.HttpContext);
            Assert.Equal(expected, written);
        }

        [Fact]
        public async Task ExecuteAsync_NonNullResult_LogsResultType()
        {
            // Arrange
            var expected = "Executing JsonResult, writing value of type 'System.String'.";
            var context = GetActionContext();
            var logger = new StubLogger();
            var executer = CreateExecutor(logger);
            var result = new JsonResult("result_value");

            // Act
            await executer.ExecuteAsync(context, result);

            // Assert
            Assert.Equal(expected, logger.MostRecentMessage);
        }

        [Fact]
        public async Task ExecuteAsync_NullResult_LogsNull()
        {
            // Arrange
            var expected = "Executing JsonResult, writing value of type 'null'.";
            var context = GetActionContext();
            var logger = new StubLogger();
            var executer = CreateExecutor(logger);
            var result = new JsonResult(null);

            // Act
            await executer.ExecuteAsync(context, result);

            // Assert
            Assert.Equal(expected, logger.MostRecentMessage);
        }

        [Fact]
        public async Task ExecuteAsync_WritesToTheResponseStream_WhenContentIsLargerThanBuffer()
        {
            // Arrange
            var writeLength = 2 * TestHttpResponseStreamWriterFactory.DefaultBufferSize + 4;
            var text = new string('a', writeLength);
            var expectedWriteCallCount = Math.Ceiling((double)writeLength / TestHttpResponseStreamWriterFactory.DefaultBufferSize);

            var stream = new Mock<Stream>();
            stream.SetupGet(s => s.CanWrite).Returns(true);
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = stream.Object;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var result = new JsonResult(text);
            var executor = CreateExecutor();

            // Act
            await executor.ExecuteAsync(actionContext, result);

            // Assert
            // HttpResponseStreamWriter buffers content up to the buffer size (16k). When writes exceed the buffer size, it'll perform a synchronous
            // write to the response stream.
            stream.Verify(s => s.Write(It.IsAny<byte[]>(), It.IsAny<int>(), TestHttpResponseStreamWriterFactory.DefaultBufferSize), Times.Exactly(2));

            // Remainder buffered content is written asynchronously as part of the FlushAsync.
            stream.Verify(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once());

            // Dispose does not call Flush
            stream.Verify(s => s.Flush(), Times.Never());
        }

        [Theory]
        [InlineData(5)]
        [InlineData(TestHttpResponseStreamWriterFactory.DefaultBufferSize - 30)]
        public async Task ExecuteAsync_DoesNotWriteSynchronouslyToTheResponseBody_WhenContentIsSmallerThanBufferSize(int writeLength)
        {
            // Arrange
            var text = new string('a', writeLength);

            var stream = new Mock<Stream>();
            stream.SetupGet(s => s.CanWrite).Returns(true);
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = stream.Object;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var result = new JsonResult(text);
            var executor = CreateExecutor();

            // Act
            await executor.ExecuteAsync(actionContext, result);

            // Assert
            // HttpResponseStreamWriter buffers content up to the buffer size (16k) and will asynchronously write content to the response as part
            // of the FlushAsync call if the content written to it is smaller than the buffer size.
            // This test verifies that no synchronous writes are performed in this scenario.
            stream.Verify(s => s.Flush(), Times.Never());
            stream.Verify(s => s.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never());
        }

        private static JsonResultExecutor CreateExecutor(ILogger<JsonResultExecutor> logger = null)
        {
            return new JsonResultExecutor(
                new TestHttpResponseStreamWriterFactory(),
                logger ?? NullLogger<JsonResultExecutor>.Instance,
                Options.Create(new MvcJsonOptions()),
                ArrayPool<char>.Shared);
        }

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

        private class StubLogger : ILogger<JsonResultExecutor>
        {
            public string MostRecentMessage { get; private set; }

            public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                MostRecentMessage = formatter(state, exception);
            }
        }
    }
}
