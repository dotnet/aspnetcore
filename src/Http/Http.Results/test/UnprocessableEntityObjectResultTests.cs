// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Http.Result
{
    public class UnprocessableEntityObjectResultTests
    {
        [Fact]
        public void UnprocessableEntityObjectResult_SetsStatusCodeAndValue()
        {
            // Arrange & Act
            var obj = new object();
            var result = new UnprocessableEntityObjectResult(obj);

            // Assert
            Assert.Equal(StatusCodes.Status422UnprocessableEntity, result.StatusCode);
            Assert.Equal(obj, result.Value);
        }
    }
}
