// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Http.Result
{
    public class RedirectResultTest : RedirectResultTestBase
    {
        [Fact]
        public void RedirectResult_Constructor_WithParameterUrlPermanentAndPreservesMethod_SetsResultUrlPermanentAndPreservesMethod()
        {
            // Arrange
            var url = "/test/url";

            // Act
            var result = new RedirectResult(url, permanent: true, preserveMethod: true);

            // Assert
            Assert.True(result.PreserveMethod);
            Assert.True(result.Permanent);
            Assert.Same(url, result.Url);
        }

        protected override Task ExecuteAsync(HttpContext httpContext, string contentPath)
        {
            var redirectResult = new RedirectResult(contentPath, false, false);
            return redirectResult.ExecuteAsync(httpContext);
        }
    }
}
