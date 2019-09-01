// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class InternalServerErrorObjectResultTest
    {
        [Fact]
        public void InternalServerErrorObjectResult_SetsStatusCodeAndValue()
        {
            // Arrange & Act
            var obj = new object();
            var internalServerErrorObjectResult = new InternalServerErrorObjectResult(obj);

            // Assert
            Assert.Equal(StatusCodes.Status500InternalServerError, internalServerErrorObjectResult.StatusCode);
            Assert.Equal(obj, internalServerErrorObjectResult.Value);
        }
    }
}
