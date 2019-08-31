// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class HttpForbiddenObjectResultTest
    {
        [Fact]
        public void HttpForbiddenObjectResult_InitializesStatusCode()
        {
            // Arrange & act
            var forbidden = new ForbiddenObjectResult(null);

            // Assert
            Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        }

        [Fact]
        public void HttpForbiddenObjectResult_InitializesStatusCodeAndResponseContent()
        {
            // Arrange & act
            var forbidden = new ForbiddenObjectResult("Test Content");

            // Assert
            Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
            Assert.Equal("Test Content", forbidden.Value);
        }
    }
}