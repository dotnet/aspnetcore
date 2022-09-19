// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

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
    public void Convert_WithIResult_DelegatesToInterface()
    {
        // Arrange
        var mapper = new ActionResultTypeMapper();

        var returnValue = Mock.Of<IResult>();

        // Act
        var result = mapper.Convert(returnValue, returnValue.GetType());

        // Assert
        var httpResult = Assert.IsType<HttpActionResult>(result);
        Assert.Same(returnValue, httpResult.Result);
    }

    [Fact]
    public void Convert_WithIConvertToActionResultAndIResult_DelegatesToInterface()
    {
        // Arrange
        var mapper = new ActionResultTypeMapper();

        var returnValue = new CustomConvertibleIResult();

        // Act
        var result = mapper.Convert(returnValue, returnValue.GetType());

        // Assert
        Assert.IsType<EmptyResult>(result);
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

    private class CustomConvertibleIResult : IConvertToActionResult, IResult
    {
        public IActionResult Convert() => new EmptyResult();

        public Task ExecuteAsync(HttpContext httpContext) => throw new NotImplementedException();
    }
}
