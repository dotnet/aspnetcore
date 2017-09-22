// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
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

        [Fact]
        public void BadRequestObjectResult_ModelState_SetsStatusCodeAndValue()
        {
            // Arrange & Act
            var badRequestObjectResult = new BadRequestObjectResult(new ModelStateDictionary());

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestObjectResult.StatusCode);
            var errors = Assert.IsType<SerializableError>(badRequestObjectResult.Value);
            Assert.Empty(errors);
        }
    }
}
