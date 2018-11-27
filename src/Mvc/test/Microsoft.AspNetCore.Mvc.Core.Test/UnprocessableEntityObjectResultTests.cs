// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
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

        [Fact]
        public void UnprocessableEntityObjectResult_ModelState_SetsStatusCodeAndValue()
        {
            // Arrange & Act
            var result = new UnprocessableEntityObjectResult(new ModelStateDictionary());

            // Assert
            Assert.Equal(StatusCodes.Status422UnprocessableEntity, result.StatusCode);
            var errors = Assert.IsType<SerializableError>(result.Value);
            Assert.Empty(errors);
        }
    }
}
