// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Actions;
using Microsoft.AspNet.Routing;
using Xunit;

namespace Microsoft.AspNet.Mvc.ActionResults
{
    public class HttpOkResultTest
    {
        [Fact]
        public void HttpOkResult_InitializesStatusCode()
        {
            // Arrange & Act
            var result = new HttpOkResult();

            // Assert
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }

        [Fact]
        public async Task HttpOkResult_SetsStatusCode()
        {
            // Arrange
            var context = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            var result = new HttpOkResult();

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            Assert.Equal(StatusCodes.Status200OK, context.HttpContext.Response.StatusCode);
        }
    }
}
