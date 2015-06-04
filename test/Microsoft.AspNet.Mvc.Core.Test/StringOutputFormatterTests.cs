// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class TextPlainFormatterTests
    {
        public static IEnumerable<object[]> OutputFormatterContextValues
        {
            get
            {
                // object value, bool useDeclaredTypeAsString, bool expectedCanWriteResult
                yield return new object[] { "valid value", true, true };
                yield return new object[] { "valid value", false, true };
                yield return new object[] { null, true, true };
                yield return new object[] { null, false, false };
                yield return new object[] { new object(), false, false };
            }
        }

        [Theory]
        [MemberData(nameof(OutputFormatterContextValues))]
        public void CanWriteResult_ReturnsTrueForStringTypes(object value, bool useDeclaredTypeAsString, bool expectedCanWriteResult)
        {
            // Arrange
            var formatter = new StringOutputFormatter();
            var typeToUse = useDeclaredTypeAsString ? typeof(string) : typeof(object);
            var formatterContext = new OutputFormatterContext()
            {
                Object = value,
                DeclaredType = typeToUse
            };

            // Act
            var result = formatter.CanWriteResult(formatterContext, null);

            // Assert
            Assert.Equal(expectedCanWriteResult, result);
        }

        [Fact]
        public async Task WriteAsync_DoesNotWriteNullStrings()
        {
            // Arrange
            Encoding encoding = Encoding.UTF8;
            var memoryStream = new MemoryStream();
            var response = new Mock<HttpResponse>();
            response.SetupProperty<long?>(o => o.ContentLength);
            response.SetupGet(r => r.Body).Returns(memoryStream);
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(o => o.Response).Returns(response.Object);

            var formatter = new StringOutputFormatter();
            var formatterContext = new OutputFormatterContext()
            {
                Object = null,
                DeclaredType = typeof(string),
                HttpContext = mockHttpContext.Object,
                SelectedEncoding = encoding
            };

            // Act
            await formatter.WriteResponseBodyAsync(formatterContext);

            // Assert
            Assert.Equal(0, memoryStream.Length);
            response.VerifySet(r => r.ContentLength = It.IsAny<long?>(), Times.Never());
        }
    }
}
