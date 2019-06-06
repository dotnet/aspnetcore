// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
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

        [Fact]
        public void ConflictObjectResult_ModelState_SetsStatusCodeAndValue()
        {
            // Arrange & Act
            var conflictObjectResult = new ConflictObjectResult(new ModelStateDictionary());

            // Assert
            Assert.Equal(StatusCodes.Status409Conflict, conflictObjectResult.StatusCode);
            var errors = Assert.IsType<SerializableError>(conflictObjectResult.Value);
            Assert.Empty(errors);
        }
    }
}
