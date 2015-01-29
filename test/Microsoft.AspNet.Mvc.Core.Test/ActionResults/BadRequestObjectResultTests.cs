// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
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
            Assert.Equal(400, badRequestObjecResult.StatusCode);
            Assert.Equal(obj, badRequestObjecResult.Value);
        }

        [Fact]
        public void BadRequestObjectResult_ModelState_SetsStatusCodeAndValue()
        {
            // Arrange & Act
            var badRequestObjecResult = new BadRequestObjectResult(new ModelStateDictionary());

            // Assert
            Assert.Equal(400, badRequestObjecResult.StatusCode);
            var errors = Assert.IsType<SerializableError>(badRequestObjecResult.Value);
            Assert.Equal(0, errors.Count);
        }
    }
}