// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.WebUtilities;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class HttpNotFoundResultTests
    {
        [Fact]
        public void HttpNotFoundResult_InitializesStatusCode()
        {
            // Arrange & act
            var notFound = new HttpNotFoundResult();

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
        }
    }
}