// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
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
            Assert.Equal("text/plain", result.ContentType);
        }

        [Fact]
        public async Task ContentDispositionHeader_IsEncodedCorrectly()
        {
            // See comment in FileResult.cs detailing how the FileDownloadName should be encoded.

            // Arrange
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupSet(c => c.Response.ContentType = "application/my-type").Verifiable();
            httpContext
                .Setup(c => c.Response.Headers.Set("Content-Disposition", @"attachment; filename=""some\\file"""))
                .Verifiable();

            var actionContext = CreateActionContext(httpContext.Object);

            var result = new EmptyFileResult("application/my-type")
            {
                FileDownloadName = @"some\file"
            };

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.True(result.WasWriteFileCalled);
            httpContext.Verify();
        }

        [Fact]
        public async Task ContentDispositionHeader_IsEncodedCorrectly_ForUnicodeCharacters()
        {
            // Arrange
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupSet(c => c.Response.ContentType = "application/my-type").Verifiable();
            httpContext
                .Setup(c => c.Response.Headers.Set(
                    "Content-Disposition",
                    @"attachment; filename*=UTF-8''ABCXYZabcxyz012789!%40%23$%25%5E&%2A%28%29-%3D_+.:~%CE%94"))
                .Verifiable();

            var actionContext = CreateActionContext(httpContext.Object);

            var result = new EmptyFileResult("application/my-type")
            {
                FileDownloadName = "ABCXYZabcxyz012789!@#$%^&*()-=_+.:~Δ"
            };

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.True(result.WasWriteFileCalled);
            httpContext.Verify();
        }

        [Fact]
        public async Task ExecuteResultAsync_DoesNotSetContentDisposition_IfNotSpecified()
        {
            // Arrange
            var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
            httpContext.SetupSet(c => c.Response.ContentType = "application/my-type").Verifiable();
            httpContext.Setup(c => c.Response.Body).Returns(Stream.Null);

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
            var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
            httpContext.SetupSet(c => c.Response.ContentType = "application/my-type").Verifiable();
            httpContext
                .Setup(c => c.Response.Headers.Set("Content-Disposition", "attachment; filename=filename.ext"))
                .Verifiable();

            var actionContext = CreateActionContext(httpContext.Object);

            var result = new EmptyFileResult("application/my-type")
            {
                FileDownloadName = "filename.ext"
            };

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.True(result.WasWriteFileCalled);
            httpContext.Verify();
        }

        public static TheoryData<string, string> ContentDispositionData
        {
            get
            {
                return new TheoryData<string, string>
                {
                    // Non quoted values
                    { "09aAzZ", "attachment; filename=09aAzZ" },
                    { "a.b", "attachment; filename=a.b" },
                    { "#", "attachment; filename=#" },
                    { "-", "attachment; filename=-" },
                    { "_", "attachment; filename=_" },

                    // Values that need to be quoted
                    { ": :", "attachment; filename=\": :\"" },
                    { "~", "attachment; filename=~" },
                    { "$", "attachment; filename=$" },
                    { "&", "attachment; filename=&" },
                    { "+", "attachment; filename=+" },
                    { "(", "attachment; filename=\"(\"" },
                    { ")", "attachment; filename=\")\"" },
                    { "<", "attachment; filename=\"<\"" },
                    { ">", "attachment; filename=\">\"" },
                    { "@", "attachment; filename=\"@\"" },
                    { ",", "attachment; filename=\",\"" },
                    { ";", "attachment; filename=\";\"" },
                    { ":", "attachment; filename=\":\"" },
                    { "/", "attachment; filename=\"/\"" },
                    { "[", "attachment; filename=\"[\"" },
                    { "]", "attachment; filename=\"]\"" },
                    { "?", "attachment; filename=\"?\"" },
                    { "=", "attachment; filename=\"=\"" },
                    { "{", "attachment; filename=\"{\"" },
                    { "}", "attachment; filename=\"}\"" },
                    { " ", "attachment; filename=\" \"" },
                    { "a\tb", "attachment; filename=\"a\tb\"" },
                    { "a b", "attachment; filename=\"a b\"" },

                    // Values that need to be escaped
                    { "\"", "attachment; filename=\"\\\"\"" },
                    { "\\", "attachment; filename=\"\\\\\"" },

                    // Values that need to be specially encoded (Base64, see rfc2047)
                    { "a\nb", "attachment; filename=\"=?utf-8?B?YQpi?=\"" },

                    // Values with non unicode characters
                    { "résumé.txt", "attachment; filename*=UTF-8''r%C3%A9sum%C3%A9.txt" },
                    { "Δ", "attachment; filename*=UTF-8''%CE%94" },
                    { "Δ\t", "attachment; filename*=UTF-8''%CE%94%09" },
                    { "ABCXYZabcxyz012789!@#$%^&*()-=_+.:~Δ", @"attachment; filename*=UTF-8''ABCXYZabcxyz012789!%40%23$%25%5E&%2A%28%29-%3D_+.:~%CE%94" },
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

                    data.Add(char.ConvertFromUtf32(i), "attachment; filename=\"" + char.ConvertFromUtf32(i) + "\"");
                }

                data.Add(char.ConvertFromUtf32(127), "attachment; filename=\"" + char.ConvertFromUtf32(127) + "\"");

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(ContentDispositionData))]
        [MemberData(nameof(ContentDispositionControlCharactersData))]
        public void GetHeaderValue_Produces_Correct_ContentDisposition(string input, string expectedOutput)
        {
            // Arrange & Act
            var actual = FileResult.ContentDispositionUtil.GetHeaderValue(input);

            // Assert
            Assert.Equal(expectedOutput, actual);
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

            protected override Task WriteFileAsync(HttpResponse response, CancellationToken cancellation)
            {
                WasWriteFileCalled = true;
                return Task.FromResult(0);
            }
        }
    }
}