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
            var badRequestObjecResult = new BadRequestObjectResult(obj);

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestObjecResult.StatusCode);
            Assert.Equal(obj, badRequestObjecResult.Value);
        }

        [Fact]
        public void BadRequestObjectResult_ModelState_SetsStatusCodeAndValue()
        {
            // Arrange & Act
            var badRequestObjecResult = new BadRequestObjectResult(new ModelStateDictionary());

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestObjecResult.StatusCode);
            var errors = Assert.IsType<SerializableError>(badRequestObjecResult.Value);
            Assert.Empty(errors);
        }
    }
}