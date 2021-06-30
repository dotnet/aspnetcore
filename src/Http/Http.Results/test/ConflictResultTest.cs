// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Http.Result
{
    public class ConflictResultTest
    {
        [Fact]
        public void ConflictResult_InitializesStatusCode()
        {
            // Arrange & act
            var conflictResult = new ConflictResult();

            // Assert
            Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
        }
    }
}
