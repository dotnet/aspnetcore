// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
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
            var httpContext = new Mock<HttpContext>(MockBehavior.Strict);

            httpContext.SetupSet(c => c.Response.ContentType = "application/my-type").Verifiable();
            httpContext.Setup(c => c.Response.Body).Returns(Stream.Null);
            httpContext
                .Setup(c => c.RequestServices.GetService(typeof(ILoggerFactory)))
                .Returns(NullLoggerFactory.Instance);

            var actionContext = CreateActionContext(httpContext.Object);

            var result = new EmptyFileResult("application/my-type");

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.True(result.WasWriteFileCalled);
            httpContext.Verify();
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
            httpContext.RequestServices = services.BuildServiceProvider();

            var actionContext = CreateActionContext(httpContext);
            var result = new EmptyFileResult("application/my-type");

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(1, loggerSink.Writes.Count);
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

        private static IServiceCollection CreateServices()
        {
            var services = new ServiceCollection();
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
                : this(MediaTypeNames.Application.Octet)
            {
            }

            public EmptyFileResult(string contentType)
                : base(contentType)
            {
            }

            protected override Task WriteFileAsync(HttpResponse response)
            {
                WasWriteFileCalled = true;
                return Task.FromResult(0);
            }
        }
    }
}