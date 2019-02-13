// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ActionResultTypeMapperTest
    {
        [Fact]
        public void Convert_WithIConvertToActionResult_DelegatesToInterface()
        {
            // Arrange
            var mapper = new ActionResultTypeMapper();

            var expected = new EmptyResult();
            var returnValue = Mock.Of<IConvertToActionResult>(r => r.Convert() == expected);

            // Act
            var result = mapper.Convert(returnValue, typeof(string));

            // Assert
            Assert.Same(expected, result);
        }

        [Fact]
        public void Convert_WithRegularType_CreatesObjectResult()
        {
            // Arrange
            var mapper = new ActionResultTypeMapper();

            var returnValue = "hello";

            // Act
            var result = mapper.Convert(returnValue, typeof(string));

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Same(returnValue, objectResult.Value);
            Assert.Equal(typeof(string), objectResult.DeclaredType);
        }

        [Fact]
        public void GetResultDataType_WithActionResultOfT_UnwrapsType()
        {
            // Arrange
            var mapper = new ActionResultTypeMapper();

            var returnType = typeof(ActionResult<string>);

            // Act
            var result = mapper.GetResultDataType(returnType);

            // Assert
            Assert.Equal(typeof(string), result);
        }

        [Fact]
        public void GetResultDataType_WithRegularType_ReturnsType()
        {
            // Arrange
            var mapper = new ActionResultTypeMapper();

            var returnType = typeof(string);

            // Act
            var result = mapper.GetResultDataType(returnType);

            // Assert
            Assert.Equal(typeof(string), result);
        }
    }
}
