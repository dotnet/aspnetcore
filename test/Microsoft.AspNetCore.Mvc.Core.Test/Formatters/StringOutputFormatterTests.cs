// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public class TextPlainFormatterTests
    {
        public static IEnumerable<object[]> OutputFormatterContextValues
        {
            get
            {
                // object value, bool useDeclaredTypeAsString, bool expectedCanWriteResult
                yield return new object[] { "valid value", true, true };
                yield return new object[] { null, true, true };
                yield return new object[] { null, false, false };
                yield return new object[] { new object(), false, false };
            }
        }

        [Fact]
        public void CanWriteResult_SetsAcceptContentType()
        {
            // Arrange
            var formatter = new StringOutputFormatter();
            var expectedContentType = new StringSegment("application/json");

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                typeof(string),
                "Thisisastring");
            context.ContentType = expectedContentType;

            // Act
            var result = formatter.CanWriteResult(context);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedContentType, context.ContentType);
        }

        [Fact]
        public void CanWriteResult_DefaultContentType()
        {
            // Arrange
            var formatter = new StringOutputFormatter();

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                typeof(string),
                "Thisisastring");

            // Act
            var result = formatter.CanWriteResult(context);

            // Assert
            Assert.True(result);
            Assert.Equal(new StringSegment("text/plain; charset=utf-8"), context.ContentType);
        }

        [Theory]
        [MemberData(nameof(OutputFormatterContextValues))]
        public void CanWriteResult_ReturnsTrueForStringTypes(
            object value,
            bool useDeclaredTypeAsString,
            bool expectedCanWriteResult)
        {
            // Arrange
            var expectedContentType = new StringSegment("application/json");

            var formatter = new StringOutputFormatter();
            var type = useDeclaredTypeAsString ? typeof(string) : typeof(object);

            var context = new OutputFormatterWriteContext(
                new DefaultHttpContext(),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                type,
                value);
            context.ContentType = expectedContentType;

            // Act
            var result = formatter.CanWriteResult(context);

            // Assert
            Assert.Equal(expectedCanWriteResult, result);
            Assert.Equal(expectedContentType, context.ContentType);
        }

        [Fact]
        public async Task WriteAsync_DoesNotWriteNullStrings()
        {
            // Arrange
            Encoding encoding = Encoding.UTF8;
            var memoryStream = new MemoryStream();
            var response = new Mock<HttpResponse>();
            response.SetupProperty(o => o.ContentLength);
            response.SetupGet(r => r.Body).Returns(memoryStream);
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(o => o.Response).Returns(response.Object);

            var formatter = new StringOutputFormatter();
            var context = new OutputFormatterWriteContext(
                httpContext.Object,
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                typeof(string),
                @object: null);

            // Act
            await formatter.WriteResponseBodyAsync(context, encoding);

            // Assert
            Assert.Equal(0, memoryStream.Length);
            response.VerifySet(r => r.ContentLength = It.IsAny<long?>(), Times.Never());
        }
    }
}
