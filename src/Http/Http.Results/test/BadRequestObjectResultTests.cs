// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Http.Result
{
    public class BadRequestObjectResultTests
    {
        [Fact]
        public void BadRequestObjectResult_SetsStatusCodeAndValue()
        {
            // Arrange & Act
            var obj = new object();
            var badRequestObjectResult = new BadRequestObjectResult(obj);

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestObjectResult.StatusCode);
            Assert.Equal(obj, badRequestObjectResult.Value);
        }
    }
}
