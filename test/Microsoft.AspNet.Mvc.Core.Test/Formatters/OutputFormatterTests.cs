// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Test
{
    public class OutputFormatterTests
    {
        public static IEnumerable<object[]> SelectResponseCharacterEncodingData
        {
            get
            {
                // string acceptEncodings, string requestEncoding, string[] supportedEncodings, string expectedEncoding
                yield return new object[] { "", null, new string[] { "utf-8", "utf-16" }, "utf-8" };
                yield return new object[] { "", "utf-16", new string[] { "utf-8", "utf-16" }, "utf-16" };

                yield return new object[] { "utf-8", null, new string[] { "utf-8", "utf-16" }, "utf-8" };
                yield return new object[] { "utf-16", "utf-8", new string[] { "utf-8", "utf-16" }, "utf-16" };
                yield return new object[] { "utf-16; q=0.5", "utf-8", new string[] { "utf-8", "utf-16" }, "utf-16" };

                yield return new object[] { "utf-8; q=0.0", null, new string[] { "utf-8", "utf-16" }, "utf-8" };
                yield return new object[] { "utf-8; q=0.0", "utf-16", new string[] { "utf-8", "utf-16" }, "utf-16" };
                yield return new object[]
                    { "utf-8; q=0.0, utf-16; q=0.0", "utf-16", new string[] { "utf-8", "utf-16" }, "utf-16" };
                yield return new object[]
                    { "utf-8; q=0.0, utf-16; q=0.0", null, new string[] { "utf-8", "utf-16" }, "utf-8" };

                yield return new object[] { "*; q=0.0", null, new string[] { "utf-8", "utf-16" }, "utf-8" };
                yield return new object[] { "*; q=0.0", "utf-16", new string[] { "utf-8", "utf-16" }, "utf-16" };
            }
        }

        [Theory]
        [MemberData(nameof(SelectResponseCharacterEncodingData))]
        public void SelectResponseCharacterEncoding_SelectsEncoding(string acceptCharsetHeaders,
                                                                    string requestEncoding,
                                                                    string[] supportedEncodings,
                                                                    string expectedEncoding)
        {
            // Arrange
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.SetupGet(o => o.Request.AcceptCharset)
                           .Returns(acceptCharsetHeaders);
            mockHttpContext.SetupGet(o => o.Request.ContentType)
                           .Returns("application/acceptCharset;charset=" + requestEncoding);
            var actionContext = new ActionContext(mockHttpContext.Object, new RouteData(), new ActionDescriptor());
            var formatter = new TestOutputFormatter();
            foreach (string supportedEncoding in supportedEncodings)
            {
                formatter.SupportedEncodings.Add(Encoding.GetEncoding(supportedEncoding));
            }

            var formatterContext = new OutputFormatterContext()
            {
                Object = "someValue",
                ActionContext = actionContext,
                DeclaredType = typeof(string)
            };

            // Act
            var actualEncoding = formatter.SelectCharacterEncoding(formatterContext);

            // Assert
            Assert.Equal(Encoding.GetEncoding(expectedEncoding), actualEncoding);
        }

        [Fact]
        public void WriteResponseContentHeaders_FormatterWithNoEncoding_Throws()
        {
            // Arrange
            var testFormatter = new TestOutputFormatter();
            var testContentType = MediaTypeHeaderValue.Parse("text/invalid");
            var formatterContext = new OutputFormatterContext();
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.SetupGet(o => o.Request.AcceptCharset)
                           .Returns(string.Empty);
            var actionContext = new ActionContext(mockHttpContext.Object, new RouteData(), new ActionDescriptor());
            formatterContext.ActionContext = actionContext;

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                        () => testFormatter.WriteResponseContentHeaders(formatterContext));
            Assert.Equal("No encoding found for output formatter " +
                         "'Microsoft.AspNet.Mvc.Test.OutputFormatterTests+TestOutputFormatter'." +
                         " There must be at least one supported encoding registered in order for the" +
                         " output formatter to write content.", ex.Message);
        }

        [Fact]
        public void WriteResponseContentHeaders_NoSelectedContentType_SetsOutputFormatterContext()
        {
            // Arrange
            var testFormatter = new DoesNotSetContext();
            var testContentType = MediaTypeHeaderValue.Parse("application/doesNotSetContext");
            var formatterContext = new OutputFormatterContext();
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.SetupGet(o => o.Request.AcceptCharset)
                           .Returns(string.Empty);
            mockHttpContext.SetupProperty(o => o.Response.ContentType);
            var actionContext = new ActionContext(mockHttpContext.Object, new RouteData(), new ActionDescriptor());
            formatterContext.ActionContext = actionContext;

            // Act
            testFormatter.WriteResponseContentHeaders(formatterContext);

            // Assert
            Assert.Equal(Encodings.UTF16EncodingLittleEndian.WebName, formatterContext.SelectedEncoding.WebName);
            Assert.Equal(Encodings.UTF16EncodingLittleEndian, formatterContext.SelectedEncoding);
            Assert.Equal("application/doesNotSetContext;charset=" + Encodings.UTF16EncodingLittleEndian.WebName,
                         formatterContext.SelectedContentType.RawValue);
        }

        private class TestOutputFormatter : OutputFormatter
        {
            public TestOutputFormatter()
            {
                SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/acceptCharset"));
            }

            public override Task WriteResponseBodyAsync(OutputFormatterContext context)
            {
                return Task.FromResult(true);
            }
        }

        private class DoesNotSetContext : OutputFormatter
        {
            public DoesNotSetContext()
            {
                SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/doesNotSetContext"));
                SupportedEncodings.Add(Encodings.UTF16EncodingLittleEndian);
            }

            public override bool CanWriteResult(OutputFormatterContext context, MediaTypeHeaderValue contentType)
            {
                // Do not set the selected media Type.
                // The WriteResponseContentHeader should do it for you. 
                return true;
            }

            public override Task WriteResponseBodyAsync(OutputFormatterContext context)
            {
                return Task.FromResult(true);
            }
        }
    }
}
