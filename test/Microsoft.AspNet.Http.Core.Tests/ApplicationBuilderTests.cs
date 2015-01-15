// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Xunit;

namespace Microsoft.AspNet.Builder.Tests
{
    public class ApplicationBuilderTests
    {
        [Fact]
        public void BuildReturnsCallableDelegate()
        {
            var builder = new ApplicationBuilder(null);
            var app = builder.Build();

            var mockHttpContext = new Moq.Mock<HttpContext>();
            var mockHttpResponse = new Moq.Mock<HttpResponse>();
            mockHttpContext.SetupGet(x => x.Response).Returns(mockHttpResponse.Object);
            mockHttpResponse.SetupProperty(x => x.StatusCode);

            app.Invoke(mockHttpContext.Object);
            Assert.Equal(mockHttpContext.Object.Response.StatusCode, 404);
        }
    }
}