// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class FileResultTest
    {
        [Fact]
        public void Constructor_SetsContentType()
        {
            // Act
            var result = new EmptyFileResult("text/plain");

            // Assert
            Assert.Equal("text/plain", result.ContentType.ToString());
        }

        [Fact]
        public async Task ContentDispositionHeader_IsEncodedCorrectly()
        {
            // See comment in FileResult.cs detailing how the FileDownloadName should be encoded.

            // Arrange
            var httpContext = GetHttpContext();
            var actionContext = CreateActionContext(httpContext);

            var result = new EmptyFileResult("application/my-type")
            {
                FileDownloadName = @"some\file"
            };

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.True(result.WasWriteFileCalled);

            Assert.Equal("application/my-type", httpContext.Response.Headers["Content-Type"]);
            Assert.Equal(@"attachment; filename=""some\\file""; filename*=UTF-8''some%5Cfile", httpContext.Response.Headers["Content-Disposition"]);
        }

        [Fact]
        public async Task ContentDispositionHeader_IsEncodedCorrectly_ForUnicodeCharacters()
        {
            // Arrange
            var httpContext = GetHttpContext();
            var actionContext = CreateActionContext(httpContext);

            var result = new EmptyFileResult("application/my-type")
            {
                FileDownloadName = "ABCXYZabcxyz012789!@#$%^&*()-=_+.:~Δ"
            };

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.True(result.WasWriteFileCalled);
            Assert.Equal("application/my-type", httpContext.Response.Headers["Content-Type"]);
            Assert.Equal(@"attachment; filename=""ABCXYZabcxyz012789!@#$%^&*()-=_+.:~_""; filename*=UTF-8''ABCXYZabcxyz012789!%40#$%25^&%2A%28%29-%3D_+.%3A~%CE%94",
                httpContext.Response.Headers["Content-Disposition"]);
        }

        [Fact]
        public async Task ExecuteResultAsync_DoesNotSetContentDisposition_IfNotSpecified()
        {
            // Arrange
            var provider = new ServiceCollection()
                .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
                .AddSingleton<EmptyFileResultExecutor>()
                .BuildServiceProvider();

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = provider;

            var actionContext = CreateActionContext(httpContext);

            var result = new EmptyFileResult("application/my-type");

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.True(result.WasWriteFileCalled);
            Assert.Equal("application/my-type", httpContext.Response.ContentType);
            Assert.Equal(Stream.Null, httpContext.Response.Body);
        }

        [Fact]
        public async Task ExecuteResultAsync_SetsContentDisposition_IfSpecified()
        {
            // Arrange
            var httpContext = GetHttpContext();
            var actionContext = CreateActionContext(httpContext);

            var result = new EmptyFileResult("application/my-type")
            {
                FileDownloadName = "filename.ext"
            };

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.True(result.WasWriteFileCalled);
            Assert.Equal("application/my-type", httpContext.Response.ContentType);
            Assert.Equal("attachment; filename=filename.ext; filename*=UTF-8''filename.ext", httpContext.Response.Headers["Content-Disposition"]);
        }

        [Fact]
        public async Task ExecuteResultAsync_ThrowsException_IfCannotResolveLoggerFactory()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = new ServiceCollection().BuildServiceProvider();
            var actionContext = CreateActionContext(httpContext);
            var result = new EmptyFileResult("application/my-type");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => result.ExecuteResultAsync(actionContext));
        }

        [Fact]
        public async Task ExecuteResultAsync_LogsInformation_IfCanResolveLoggerFactory()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var services = new ServiceCollection();
            var loggerSink = new TestSink();
            services.AddSingleton<ILoggerFactory>(new TestLoggerFactory(loggerSink, true));
            services.AddSingleton<EmptyFileResultExecutor>();
            httpContext.RequestServices = services.BuildServiceProvider();

            var actionContext = CreateActionContext(httpContext);
            var result = new EmptyFileResult("application/my-type");

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Single(loggerSink.Writes);
        }

        public static TheoryData<string, string> ContentDispositionData
        {
            get
            {
                return new TheoryData<string, string>
                {
                    // Non quoted values
                    { "09aAzZ", "attachment; filename=09aAzZ; filename*=UTF-8''09aAzZ" },
                    { "a.b", "attachment; filename=a.b; filename*=UTF-8''a.b" },
                    { "#", "attachment; filename=#; filename*=UTF-8''#" },
                    { "-", "attachment; filename=-; filename*=UTF-8''-" },
                    { "_", "attachment; filename=_; filename*=UTF-8''_" },
                    { "~", "attachment; filename=~; filename*=UTF-8''~" },
                    { "$", "attachment; filename=$; filename*=UTF-8''$" },
                    { "&", "attachment; filename=&; filename*=UTF-8''&" },
                    { "+", "attachment; filename=+; filename*=UTF-8''+" },
                    { "!", "attachment; filename=!; filename*=UTF-8''!" },
                    { "^", "attachment; filename=^; filename*=UTF-8''^" },
                    { "`", "attachment; filename=`; filename*=UTF-8''`" },
                    { "|", "attachment; filename=|; filename*=UTF-8''|" },

                    // Values that need to be quoted
                    { ": :", "attachment; filename=\": :\"; filename*=UTF-8''%3A%20%3A" },
                    { "(", "attachment; filename=\"(\"; filename*=UTF-8''%28" },
                    { ")", "attachment; filename=\")\"; filename*=UTF-8''%29" },
                    { "<", "attachment; filename=\"<\"; filename*=UTF-8''%3C" },
                    { ">", "attachment; filename=\">\"; filename*=UTF-8''%3E" },
                    { "@", "attachment; filename=\"@\"; filename*=UTF-8''%40" },
                    { ",", "attachment; filename=\",\"; filename*=UTF-8''%2C" },
                    { ";", "attachment; filename=\";\"; filename*=UTF-8''%3B" },
                    { ":", "attachment; filename=\":\"; filename*=UTF-8''%3A" },
                    { "/", "attachment; filename=\"/\"; filename*=UTF-8''%2F" },
                    { "[", "attachment; filename=\"[\"; filename*=UTF-8''%5B" },
                    { "]", "attachment; filename=\"]\"; filename*=UTF-8''%5D" },
                    { "?", "attachment; filename=\"?\"; filename*=UTF-8''%3F" },
                    { "=", "attachment; filename=\"=\"; filename*=UTF-8''%3D" },
                    { "{", "attachment; filename=\"{\"; filename*=UTF-8''%7B" },
                    { "}", "attachment; filename=\"}\"; filename*=UTF-8''%7D" },
                    { " ", "attachment; filename=\" \"; filename*=UTF-8''%20" },
                    { "a\tb", "attachment; filename=\"a\tb\"; filename*=UTF-8''a%09b" },
                    { "a b", "attachment; filename=\"a b\"; filename*=UTF-8''a%20b" },

                    // Values that need to be escaped
                    { "\"", "attachment; filename=\"\\\"\"; filename*=UTF-8''%22" },
                    { "\\", "attachment; filename=\"\\\\\"; filename*=UTF-8''%5C" },

                    // Values that need to be specially encoded (Base64, see rfc2047)
                    { "a\nb", "attachment; filename=\"a\nb\"; filename*=UTF-8''a%0Ab" },

                    // Values with non unicode characters
                    { "résumé.txt", "attachment; filename=r_sum_.txt; filename*=UTF-8''r%C3%A9sum%C3%A9.txt" },
                    { "Δ", "attachment; filename=_; filename*=UTF-8''%CE%94" },
                    { "Δ\t", "attachment; filename=\"_\t\"; filename*=UTF-8''%CE%94%09" },
                    { "ABCXYZabcxyz012789!@#$%^&*()-=_+.:~Δ", @"attachment; filename=""ABCXYZabcxyz012789!@#$%^&*()-=_+.:~_""; filename*=UTF-8''ABCXYZabcxyz012789!%40#$%25^&%2A%28%29-%3D_+.%3A~%CE%94" },
                };
            }
        }

        public static TheoryData<string, string> ContentDispositionControlCharactersData
        {
            get
            {
                var data = new TheoryData<string, string>();
                for (var i = 0; i < 32; i++)
                {
                    if (i == 10)
                    {
                        // skip \n as it has a special encoding
                        continue;
                    }

                    data.Add(char.ConvertFromUtf32(i), "attachment; filename=\"" + char.ConvertFromUtf32(i) + "\"; filename*=UTF-8''%" + i.ToString("X2"));
                }

                data.Add(char.ConvertFromUtf32(127), "attachment; filename=\"" + char.ConvertFromUtf32(127) + "\"; filename*=UTF-8''%7F");

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(ContentDispositionData))]
        [MemberData(nameof(ContentDispositionControlCharactersData))]
        public void GetHeaderValue_Produces_Correct_ContentDisposition(string input, string expectedOutput)
        {
            // Arrange & Act
            var cd = new ContentDispositionHeaderValue("attachment");
            cd.SetHttpFileName(input);
            var actual = cd.ToString();

            // Assert
            Assert.Equal(expectedOutput, actual);
        }

        [Fact]
        public async Task SetsAcceptRangeHeader()
        {
            // Arrange
            var httpContext = GetHttpContext();
            var actionContext = CreateActionContext(httpContext);

            var result = new EmptyFileResult("application/my-type");

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal("bytes", httpContext.Response.Headers[HeaderNames.AcceptRanges]);
        }

        [Theory]
        [InlineData("\"Etag\"", "\"NotEtag\"", "\"Etag\"")]
        [InlineData("\"Etag\"", null, null)]
        [InlineData(null, "\"NotEtag\"", "\"Etag\"")]
        public void GetPreconditionState_ShouldProcess(string ifMatch, string ifNoneMatch, string ifRange)
        {
            // Arrange
            var actionContext = new ActionContext();
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = HttpMethods.Get;
            var httpRequestHeaders = httpContext.Request.GetTypedHeaders();
            var lastModified = DateTimeOffset.MinValue;
            lastModified = new DateTimeOffset(lastModified.Year, lastModified.Month, lastModified.Day, lastModified.Hour, lastModified.Minute, lastModified.Second, TimeSpan.FromSeconds(0));
            var etag = new EntityTagHeaderValue("\"Etag\"");
            httpRequestHeaders.IfMatch = ifMatch == null ? null : new[]
            {
                new EntityTagHeaderValue(ifMatch),
            };

            httpRequestHeaders.IfNoneMatch = ifNoneMatch == null ? null : new[]
            {
                new EntityTagHeaderValue(ifNoneMatch),
            };
            httpRequestHeaders.IfRange = ifRange == null ? null : new RangeConditionHeaderValue(ifRange);
            httpRequestHeaders.IfUnmodifiedSince = lastModified;
            httpRequestHeaders.IfModifiedSince = DateTimeOffset.MinValue.AddDays(1);
            actionContext.HttpContext = httpContext;

            // Act
            var state = FileResultExecutorBase.GetPreconditionState(
                httpRequestHeaders,
                lastModified,
                etag);

            // Assert
            Assert.Equal(FileResultExecutorBase.PreconditionState.ShouldProcess, state);
        }

        [Theory]
        [InlineData("\"NotEtag\"", null)]
        [InlineData("\"Etag\"", "\"Etag\"")]
        [InlineData(null, null)]
        public void GetPreconditionState_ShouldNotProcess_PreconditionFailed(string ifMatch, string ifNoneMatch)
        {
            // Arrange
            var actionContext = new ActionContext();
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = HttpMethods.Delete;
            var httpRequestHeaders = httpContext.Request.GetTypedHeaders();
            var lastModified = DateTimeOffset.MinValue.AddDays(1);
            var etag = new EntityTagHeaderValue("\"Etag\"");
            httpRequestHeaders.IfMatch = ifMatch == null ? null : new[]
            {
                new EntityTagHeaderValue(ifMatch),
            };

            httpRequestHeaders.IfNoneMatch = ifNoneMatch == null ? null : new[]
            {
                new EntityTagHeaderValue(ifNoneMatch),
            };
            httpRequestHeaders.IfUnmodifiedSince = DateTimeOffset.MinValue;
            httpRequestHeaders.IfModifiedSince = DateTimeOffset.MinValue.AddDays(2);
            actionContext.HttpContext = httpContext;

            // Act
            var state = FileResultExecutorBase.GetPreconditionState(
                httpRequestHeaders,
                lastModified,
                etag);

            // Assert
            Assert.Equal(FileResultExecutorBase.PreconditionState.PreconditionFailed, state);
        }

        [Theory]
        [InlineData(null, "\"Etag\"")]
        [InlineData(null, null)]
        public void GetPreconditionState_ShouldNotProcess_NotModified(string ifMatch, string ifNoneMatch)
        {
            // Arrange
            var actionContext = new ActionContext();
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = HttpMethods.Get;
            var httpRequestHeaders = httpContext.Request.GetTypedHeaders();
            var lastModified = DateTimeOffset.MinValue;
            lastModified = new DateTimeOffset(lastModified.Year, lastModified.Month, lastModified.Day, lastModified.Hour, lastModified.Minute, lastModified.Second, TimeSpan.FromSeconds(0));
            var etag = new EntityTagHeaderValue("\"Etag\"");
            httpRequestHeaders.IfMatch = ifMatch == null ? null : new[]
            {
                new EntityTagHeaderValue(ifMatch),
            };

            httpRequestHeaders.IfNoneMatch = ifNoneMatch == null ? null : new[]
            {
                new EntityTagHeaderValue(ifNoneMatch),
            };
            httpRequestHeaders.IfModifiedSince = lastModified;
            actionContext.HttpContext = httpContext;

            // Act
            var state = FileResultExecutorBase.GetPreconditionState(
                httpRequestHeaders,
                lastModified,
                etag);

            // Assert
            Assert.Equal(FileResultExecutorBase.PreconditionState.NotModified, state);
        }

        [Theory]
        [InlineData("\"NotEtag\"", false)]
        [InlineData("\"Etag\"", true)]
        public void IfRangeValid_IgnoreRangeRequest(string ifRangeString, bool expected)
        {
            // Arrange
            var actionContext = new ActionContext();
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = HttpMethods.Get;
            var httpRequestHeaders = httpContext.Request.GetTypedHeaders();
            var lastModified = DateTimeOffset.MinValue;
            lastModified = new DateTimeOffset(lastModified.Year, lastModified.Month, lastModified.Day, lastModified.Hour, lastModified.Minute, lastModified.Second, TimeSpan.FromSeconds(0));
            var etag = new EntityTagHeaderValue("\"Etag\"");
            httpRequestHeaders.IfRange = new RangeConditionHeaderValue(ifRangeString);
            httpRequestHeaders.IfModifiedSince = lastModified;
            actionContext.HttpContext = httpContext;

            // Act
            var ifRangeIsValid = FileResultExecutorBase.IfRangeValid(
                httpRequestHeaders,
                lastModified,
                etag);

            // Assert
            Assert.Equal(expected, ifRangeIsValid);
        }

        private static IServiceCollection CreateServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<EmptyFileResultExecutor>();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            return services;
        }

        private static HttpContext GetHttpContext()
        {
            var services = CreateServices();

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = services.BuildServiceProvider();

            return httpContext;
        }

        private static ActionContext CreateActionContext(HttpContext context)
        {
            return new ActionContext(context, new RouteData(), new ActionDescriptor());
        }

        private class EmptyFileResult : FileResult
        {
            public bool WasWriteFileCalled;

            public EmptyFileResult()
                : base("application/octet")
            {
            }

            public EmptyFileResult(string contentType)
                : base(contentType)
            {
            }

            public override Task ExecuteResultAsync(ActionContext context)
            {
                var executor = context.HttpContext.RequestServices.GetRequiredService<EmptyFileResultExecutor>();
                return executor.ExecuteAsync(context, this);
            }
        }

        private class EmptyFileResultExecutor : FileResultExecutorBase
        {
            public EmptyFileResultExecutor(ILoggerFactory loggerFactory)
                : base(CreateLogger<EmptyFileResultExecutor>(loggerFactory))
            {
            }

            public Task ExecuteAsync(ActionContext context, EmptyFileResult result)
            {
                SetHeadersAndLog(context, result, 0L, true);
                result.WasWriteFileCalled = true;
                return Task.FromResult(0);
            }
        }
    }
}