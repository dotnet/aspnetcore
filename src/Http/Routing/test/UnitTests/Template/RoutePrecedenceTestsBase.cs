// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Template;

public abstract class RoutePrecedenceTestsBase
{
    [Theory]
    [InlineData("Employees/{id}", "Employees/{employeeId}")]
    [InlineData("abc", "def")]
    [InlineData("{x:alpha}", "{x:int}")]
    public void ComputeMatched_IsEqual(string xTemplate, string yTemplate)
    {
        // Arrange & Act
        var xPrededence = ComputeMatched(xTemplate);
        var yPrededence = ComputeMatched(yTemplate);

        // Assert
        Assert.Equal(xPrededence, yPrededence);
    }

    [Theory]
    [InlineData("Employees/{id}", "Employees/{employeeId}")]
    [InlineData("abc", "def")]
    [InlineData("{x:alpha}", "{x:int}")]
    public void ComputeGenerated_IsEqual(string xTemplate, string yTemplate)
    {
        // Arrange & Act
        var xPrededence = ComputeGenerated(xTemplate);
        var yPrededence = ComputeGenerated(yTemplate);

        // Assert
        Assert.Equal(xPrededence, yPrededence);
    }

    [Theory]
    [InlineData("abc", "a{x}")]
    [InlineData("abc", "{x}c")]
    [InlineData("abc", "{x:int}")]
    [InlineData("abc", "{x}")]
    [InlineData("abc", "{*x}")]
    [InlineData("{x:int}", "{x}")]
    [InlineData("{x:int}", "{*x}")]
    [InlineData("a{x}", "{x}")]
    [InlineData("{x}c", "{x}")]
    [InlineData("a{x}", "{*x}")]
    [InlineData("{x}c", "{*x}")]
    [InlineData("{x}", "{*x}")]
    [InlineData("{*x:maxlength(10)}", "{*x}")]
    [InlineData("abc/def", "abc/{x:int}")]
    [InlineData("abc/def", "abc/{x}")]
    [InlineData("abc/def", "abc/{*x}")]
    [InlineData("abc/{x:int}", "abc/{x}")]
    [InlineData("abc/{x:int}", "abc/{*x}")]
    [InlineData("abc/{x}", "abc/{*x}")]
    [InlineData("{x}/{y:int}", "{x}/{y}")]
    public void ComputeMatched_IsLessThan(string xTemplate, string yTemplate)
    {
        // Arrange & Act
        var xPrededence = ComputeMatched(xTemplate);
        var yPrededence = ComputeMatched(yTemplate);

        // Assert
        Assert.True(xPrededence < yPrededence);
    }

    [Theory]
    [InlineData("abc", "a{x}")]
    [InlineData("abc", "{x}c")]
    [InlineData("abc", "{x:int}")]
    [InlineData("abc", "{x}")]
    [InlineData("abc", "{*x}")]
    [InlineData("{x:int}", "{x}")]
    [InlineData("{x:int}", "{*x}")]
    [InlineData("a{x}", "{x}")]
    [InlineData("{x}c", "{x}")]
    [InlineData("a{x}", "{*x}")]
    [InlineData("{x}c", "{*x}")]
    [InlineData("{x}", "{*x}")]
    [InlineData("{*x:maxlength(10)}", "{*x}")]
    [InlineData("abc/def", "abc/{x:int}")]
    [InlineData("abc/def", "abc/{x}")]
    [InlineData("abc/def", "abc/{*x}")]
    [InlineData("abc/{x:int}", "abc/{x}")]
    [InlineData("abc/{x:int}", "abc/{*x}")]
    [InlineData("abc/{x}", "abc/{*x}")]
    [InlineData("{x}/{y:int}", "{x}/{y}")]
    public void ComputeGenerated_IsGreaterThan(string xTemplate, string yTemplate)
    {
        // Arrange & Act
        var xPrecedence = ComputeGenerated(xTemplate);
        var yPrecedence = ComputeGenerated(yTemplate);

        // Assert
        Assert.True(xPrecedence > yPrecedence);
    }

    [Fact]
    public void ComputeGenerated_TooManySegments_ThrowHumaneError()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            // Arrange & Act
            ComputeGenerated("{a}/{b}/{c}/{d}/{e}/{f}/{g}/{h}/{i}/{j}/{k}/{l}/{m}/{n}/{o}/{p}/{q}/{r}/{s}/{t}/{u}/{v}/{w}/{x}/{y}/{z}/{a2}/{b2}/{b3}");
        });

        // Assert
        Assert.Equal("Route exceeds the maximum number of allowed segments of 28 and is unable to be processed.", ex.Message);
    }

    [Fact]
    public void ComputeMatched_TooManySegments_ThrowHumaneError()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            // Arrange & Act
            ComputeMatched("{a}/{b}/{c}/{d}/{e}/{f}/{g}/{h}/{i}/{j}/{k}/{l}/{m}/{n}/{o}/{p}/{q}/{r}/{s}/{t}/{u}/{v}/{w}/{x}/{y}/{z}/{a2}/{b2}/{b3}");
        });

        // Assert
        Assert.Equal("Route exceeds the maximum number of allowed segments of 28 and is unable to be processed.", ex.Message);
    }

    protected abstract decimal ComputeMatched(string template);

    protected abstract decimal ComputeGenerated(string template);
}
