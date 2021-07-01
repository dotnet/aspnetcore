// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Http.Result
{
    public class ConflictObjectResultTest
    {
        [Fact]
        public void ConflictObjectResult_SetsStatusCodeAndValue()
        {
            // Arrange & Act
            var obj = new object();
            var conflictObjectResult = new ConflictObjectResult(obj);

            // Assert
            Assert.Equal(StatusCodes.Status409Conflict, conflictObjectResult.StatusCode);
            Assert.Equal(obj, conflictObjectResult.Value);
        }
    }
}
