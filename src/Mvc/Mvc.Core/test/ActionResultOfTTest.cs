// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class ActionResultOfTTest
    {
        [Fact]
        public void Constructor_WithValue_ThrowsForInvalidType()
        {
            // Arrange
            var input = new FileStreamResult(Stream.Null, "application/json");

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new ActionResult<FileStreamResult>(value: input));
            Assert.Equal($"Invalid type parameter '{typeof(FileStreamResult)}' specified for 'ActionResult<T>'.", ex.Message);
        }

        [Fact]
        public void Constructor_WithActionResult_ThrowsForInvalidType()
        {
            // Arrange
            var actionResult = new OkResult();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new ActionResult<FileStreamResult>(result: actionResult));
            Assert.Equal($"Invalid type parameter '{typeof(FileStreamResult)}' specified for 'ActionResult<T>'.", ex.Message);
        }

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
            var value = new DerivedItem();
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

        private class DerivedItem : BaseItem
        {
        }
    }
}
