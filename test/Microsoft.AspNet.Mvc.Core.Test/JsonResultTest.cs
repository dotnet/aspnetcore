// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;


namespace Microsoft.AspNet.Mvc
{
    public class JsonResultTest
    {
        private static readonly byte[] _abcdUTF8Bytes 
            = new byte[] { 123, 34, 102, 111, 111, 34, 58, 34, 97, 98, 99, 100, 34, 125 };

        [Fact]
        public async Task ExecuteResult_GeneratesResultsWithoutBOMByDefault()
        {
            // Arrange
            var expected = _abcdUTF8Bytes;
            var memoryStream = new MemoryStream();
            var response = new Mock<HttpResponse>();
            response.SetupGet(r => r.Body)
                   .Returns(memoryStream);
            var context = new Mock<HttpContext>();
            context.SetupGet(c => c.Response)
                   .Returns(response.Object);
            var actionContext = new ActionContext(context.Object,
                                                  new RouteData(),
                                                  new ActionDescriptor());
            var result = new JsonResult(new { foo = "abcd" });

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(expected, memoryStream.ToArray());
        }

        [Fact]
        public async Task ExecuteResult_UsesEncoderIfSpecified()
        {
            // Arrange
            var expected = Enumerable.Concat(Encoding.UTF8.GetPreamble(), _abcdUTF8Bytes);
            var memoryStream = new MemoryStream();
            var response = new Mock<HttpResponse>();
            response.SetupGet(r => r.Body)
                   .Returns(memoryStream);
            var context = new Mock<HttpContext>();
            context.SetupGet(c => c.Response)
                   .Returns(response.Object);
            var actionContext = new ActionContext(context.Object,
                                                  new RouteData(),
                                                  new ActionDescriptor());
            var result = new JsonResult(new { foo = "abcd" })
            {
                Encoding = Encoding.UTF8
            };

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(expected, memoryStream.ToArray());
        }
    }
}