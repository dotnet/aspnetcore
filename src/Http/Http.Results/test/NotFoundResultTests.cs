// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Http.Result
{
    public class NotFoundResultTests
    {
        [Fact]
        public void NotFoundResult_InitializesStatusCode()
        {
            // Arrange & act
            var notFound = new NotFoundResult();

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
        }
    }
}
