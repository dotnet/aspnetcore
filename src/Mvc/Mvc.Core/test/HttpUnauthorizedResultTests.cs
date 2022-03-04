// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class HttpUnauthorizedResultTests
    {
        [Fact]
        public void HttpUnauthorizedResult_InitializesStatusCode()
        {
            // Arrange & act
            var result = new UnauthorizedResult();

            // Assert
            Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
        }
    }
}