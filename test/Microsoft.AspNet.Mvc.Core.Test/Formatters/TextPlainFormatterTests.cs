// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;
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
            var formatter = new TextPlainFormatter();
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
            var formatter = new TextPlainFormatter();
            var formatterContext = new OutputFormatterContext()
            {
                Object = null,
                DeclaredType = typeof(string), 
            };

            var tempMemoryStream = new MemoryStream();
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.SetupGet(o => o.Response.Body)
                           .Returns(tempMemoryStream);
            // Act
            await formatter.WriteResponseBodyAsync(formatterContext);

            // Assert
            Assert.Equal(0, tempMemoryStream.Length);
        }
    }
}
