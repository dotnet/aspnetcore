// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc;

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
    public void Constructor_WithIResult_ThrowsForInvalidType()
    {
        // Arrange
        var result = new TestResult();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new ActionResult<TestResult>(value: result));
        Assert.Equal($"Invalid type parameter '{typeof(TestResult)}' specified for 'ActionResult<T>'.", ex.Message);
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
        Assert.Equal(StatusCodes.Status200OK, objectResult.StatusCode);
    }

    [Fact]
    public void Convert_ReturnsObjectResultWrappingValue_SetsStatusCodeFromProblemDetails()
    {
        // Arrange
        var value = new ProblemDetails { Status = StatusCodes.Status400BadRequest };
        var actionResultOfT = new ActionResult<ProblemDetails>(value);
        var convertToActionResult = (IConvertToActionResult)actionResultOfT;

        // Act
        var result = convertToActionResult.Convert();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Same(value, objectResult.Value);
        Assert.Equal(typeof(ProblemDetails), objectResult.DeclaredType);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
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
        Assert.Equal(StatusCodes.Status200OK, objectResult.StatusCode);
    }

    private class BaseItem
    {
    }

    private class DerivedItem : BaseItem
    {
    }

    private class TestResult : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
            => Task.CompletedTask;
    }
}
