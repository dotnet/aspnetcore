// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Http.Result
{
    public class UnauthorizedResultTests
    {
        [Fact]
        public void UnauthorizedResult_InitializesStatusCode()
        {
            // Arrange & act
            var result = new UnauthorizedResult();

            // Assert
            Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
        }
    }
}
