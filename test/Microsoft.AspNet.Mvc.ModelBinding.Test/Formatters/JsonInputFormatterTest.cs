// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class JsonInputFormatterTest
    {
        [Fact]
        public void DefaultMediaType_ReturnsApplicationJson()
        {
            // Arrange 
            var formatter = new JsonInputFormatter();

            // Act
            var mediaType = formatter.SupportedMediaTypes[0];

            // Assert
            Assert.Equal("application/json", mediaType.RawValue);
        }

        public static IEnumerable<object[]> JsonFormatterReadSimpleTypesData
        {
            get
            {
                yield return new object[] { "100", typeof(int), 100 };
                yield return new object[] { "'abcd'", typeof(string), "abcd" };
                yield return new object[] { "'2012-02-01 12:45 AM'", typeof(DateTime), 
                                            new DateTime(2012, 02, 01, 00, 45, 00) };
            }
        }

        [Theory]
        [MemberData("JsonFormatterReadSimpleTypesData")]
        public async Task JsonFormatterReadsSimpleTypes(string content, Type type, object expected)
        {
            // Arrange
            var formatter = new JsonInputFormatter();
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var httpContext = GetHttpContext(contentBytes);
            var modelState = new ModelStateDictionary();
            var metadata = new EmptyModelMetadataProvider().GetMetadataForType(null, type);
            var context = new InputFormatterContext(httpContext, metadata, modelState);

            // Act
            await formatter.ReadAsync(context);

            // Assert
            Assert.Equal(expected, context.Model);
        }

        [Fact]
        public async Task JsonFormatterReadsComplexTypes()
        {
            // Arrange
            var content = "{name: 'Person Name', Age: '30'}";
            var formatter = new JsonInputFormatter();
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var httpContext = GetHttpContext(contentBytes);
            var modelState = new ModelStateDictionary();
            var metadata = new EmptyModelMetadataProvider().GetMetadataForType(null, typeof(User));
            var context = new InputFormatterContext(httpContext, metadata, modelState);

            // Act
            await formatter.ReadAsync(context);

            // Assert
            var model = Assert.IsType<User>(context.Model);
            Assert.Equal("Person Name", model.Name);
            Assert.Equal(30, model.Age);
        }

        [Fact]
        public async Task ReadAsync_ThrowsOnDeserializationErrors()
        {
            // Arrange
            var content = "{name: 'Person Name', Age: 'not-an-age'}";
            var formatter = new JsonInputFormatter();
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var httpContext = GetHttpContext(contentBytes);
            var modelState = new ModelStateDictionary();
            var metadata = new EmptyModelMetadataProvider().GetMetadataForType(null, typeof(User));
            var context = new InputFormatterContext(httpContext, metadata, modelState);

            // Act and Assert
            await Assert.ThrowsAsync<JsonReaderException>(() => formatter.ReadAsync(context));
        }

        [Fact]
        public async Task ReadAsync_AddsModelValidationErrorsToModelState_WhenCaptureErrorsIsSet()
        {
            // Arrange
            var content = "{name: 'Person Name', Age: 'not-an-age'}";
            var formatter = new JsonInputFormatter { CaptureDeserilizationErrors = true };
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var httpContext = GetHttpContext(contentBytes);
            var modelState = new ModelStateDictionary();
            var metadata = new EmptyModelMetadataProvider().GetMetadataForType(null, typeof(User));
            var context = new InputFormatterContext(httpContext, metadata, modelState);

            // Act
            await formatter.ReadAsync(context);

            // Assert
            Assert.Equal("Could not convert string to decimal: not-an-age. Path 'Age', line 1, position 39.", 
                         modelState["Age"].Errors[0].Exception.Message);
        }

        private static HttpContext GetHttpContext(byte[] contentBytes, 
                                                        string contentType = "application/json")
        {
            var request = new Mock<HttpRequest>();
            var headers = new Mock<IHeaderDictionary>();
            headers.SetupGet(h => h["Content-Type"]).Returns(contentType);
            request.SetupGet(r => r.Headers).Returns(headers.Object);
            request.SetupGet(f => f.Body).Returns(new MemoryStream(contentBytes));

            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            return httpContext.Object;
        }

        private sealed class User
        {
            public string Name { get; set; }

            public decimal Age { get; set; }
        }
    }
}
#endif