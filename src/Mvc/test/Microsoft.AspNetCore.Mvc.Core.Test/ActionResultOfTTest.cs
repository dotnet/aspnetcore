// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class ActionResultOfTTest
    {
        [Fact]
        public void Convert_ReturnsResultIfSet()
        {
            // Arrange
            var expected = new OkResult();
            var actionResultOfT = new ActionResult<string>(expected);
            var convertToActionResult = (IConvertToActionResult)actionResultOfT;

            // Act
            var result = convertToActionResult.Convert();

            // Assert
            Assert.Same(expected, result);
        }

        [Fact]
        public void Convert_ReturnsObjectResultWrappingValue()
        {
            // Arrange
            var value = new BaseItem();
            var actionResultOfT = new ActionResult<BaseItem>(value);
            var convertToActionResult = (IConvertToActionResult)actionResultOfT;

            // Act
            var result = convertToActionResult.Convert();

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Same(value, objectResult.Value);
            Assert.Equal(typeof(BaseItem), objectResult.DeclaredType);
        }

        [Fact]
        public void Convert_InfersDeclaredTypeFromActionResultTypeParameter()
        {
            // Arrange
            var value = new DeriviedItem();
            var actionResultOfT = new ActionResult<BaseItem>(value);
            var convertToActionResult = (IConvertToActionResult)actionResultOfT;

            // Act
            var result = convertToActionResult.Convert();

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Same(value, objectResult.Value);
            Assert.Equal(typeof(BaseItem), objectResult.DeclaredType);
        }

        private class BaseItem
        {
        }

        private class DeriviedItem : BaseItem
        {
        }
    }
}
