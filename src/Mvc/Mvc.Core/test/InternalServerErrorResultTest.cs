// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class InternalServerErrorResultTest
    {
        [Fact]
        public void InternalServerErrorResult_SetsStatusCode()
        {
            // Arrange & act
            var internalServerErrorResult = new InternalServerErrorResult();

            // Assert
            Assert.Equal(StatusCodes.Status500InternalServerError, internalServerErrorResult.StatusCode);
        }
    }
}
