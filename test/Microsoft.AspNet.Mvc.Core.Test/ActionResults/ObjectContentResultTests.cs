// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test.ActionResults
{
    public class ObjectContentResultTests
    {
        [Fact]
        public void ObjectContentResult_Create_CallsContentResult_InitializesValue()
        {
            // Arrange
            var input = "testInput";
            var actionContext = CreateMockActionContext();

            // Act
            var result = new ObjectContentResult(input);

            // Assert
            Assert.Equal(input, result.Value);
        }

        [Fact]
        public async Task ObjectContentResult_Execute_CallsContentResult_SetsContent()
        {
            // Arrange
            var expectedContentType = "text/plain";
            var input = "testInput";
            var stream = new MemoryStream();

            var httpResponse = new Mock<HttpResponse>();
            httpResponse.SetupSet(r => r.ContentType = expectedContentType).Verifiable();
            httpResponse.SetupGet(r => r.Body).Returns(stream);

            var actionContext = CreateMockActionContext(httpResponse.Object);

            // Act
            var result = new ObjectContentResult(input);
            await result.ExecuteResultAsync(actionContext);

            // Assert
            httpResponse.VerifySet(r => r.ContentType = expectedContentType);
            // The following verifies the correct Content was written to Body
            Assert.Equal(input.Length, httpResponse.Object.Body.Length);
        }

        [Fact]
        public async Task ObjectContentResult_Execute_CallsJsonResult_SetsContent()
        {
            // Arrange
            var expectedContentType = "application/json";
            var nonStringValue = new { x1 = 10, y1 = "Hello" };
            var httpResponse = Mock.Of<HttpResponse>();
            httpResponse.Body = new MemoryStream();
            var actionContext = CreateMockActionContext(httpResponse);

            var tempStream = new MemoryStream();
            using (var writer = new StreamWriter(tempStream, Encodings.UTF8EncodingWithoutBOM, 1024, leaveOpen: true))
            {
                var formatter = new JsonOutputFormatter(JsonOutputFormatter.CreateDefaultSettings(), false);
                formatter.WriteObject(writer, nonStringValue);
            }

            // Act
            var result = new ObjectContentResult(nonStringValue);
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(expectedContentType, httpResponse.ContentType);
            Assert.Equal(tempStream.ToArray(), ((MemoryStream)actionContext.HttpContext.Response.Body).ToArray());
        }

        private static ActionContext CreateMockActionContext(HttpResponse response = null)
        {
            var httpContext = new Mock<HttpContext>();
            if (response != null)
            {
                httpContext.Setup(o => o.Response).Returns(response);
            }
            
            return new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());
        }
    }
}