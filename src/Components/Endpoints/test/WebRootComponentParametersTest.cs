// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class WebRootComponentParametersTest
{
    [Fact]
    public void WebRootComponentParameters_DefinitelyEquals_ReturnsFalse_ForMismatchedParameterCount()
    {
        // Arrange
        var parameters1 = CreateParameters(new() { ["First"] = 123 });
        var parameters2 = CreateParameters(new() { ["First"] = 123, ["Second"] = "abc" });

        // Act
        var result = parameters1.DefinitelyEquals(parameters2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void WebRootComponentParameters_DefinitelyEquals_ReturnsFalse_ForMismatchedParameterNames()
    {
        // Arrange
        var parameters1 = CreateParameters(new() { ["First"] = 123 });
        var parameters2 = CreateParameters(new() { ["Second"] = 123 });

        // Act
        var result = parameters1.DefinitelyEquals(parameters2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void WebRootComponentParameters_DefinitelyEquals_ReturnsFalse_ForMismatchedParameterValues()
    {
        // Arrange
        var parameters1 = CreateParameters(new() { ["First"] = 123 });
        var parameters2 = CreateParameters(new() { ["First"] = 456 });

        // Act
        var result = parameters1.DefinitelyEquals(parameters2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void WebRootComponentParameters_DefinitelyEquals_ReturnsFalse_ForMismatchedParameterTypes()
    {
        // Arrange
        var parameters1 = CreateParameters(new() { ["First"] = 123 });
        var parameters2 = CreateParameters(new() { ["First"] = 123L });

        // Act
        var result = parameters1.DefinitelyEquals(parameters2);

        // Assert
        Assert.False(result);
    }

    public static readonly object[][] DefinitelyEqualParameterValues =
    [
        [123],
        ["abc"],
        [new { First = 123, Second = "abc" }],
    ];

    [Theory]
    [MemberData(nameof(DefinitelyEqualParameterValues))]
    public void WebRootComponentParameters_DefinitelyEquals_ReturnsTrue_ForSameParameterValues(object value)
    {
        // Arrange
        var parameters1 = CreateParameters(new() { ["First"] = value });
        var parameters2 = CreateParameters(new() { ["First"] = value });

        // Act
        var result = parameters1.DefinitelyEquals(parameters2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void WebRootComponentParameters_DefinitelyEquals_ReturnsTrue_ForEmptySetOfParameters()
    {
        // Arrange
        var parameters1 = CreateParameters(new());
        var parameters2 = CreateParameters(new());

        // Act
        var result = parameters1.DefinitelyEquals(parameters2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void WebRootComponentParameters_DefinitelyEquals_ReturnsFalse_WhenComparingNonJsonElementParameterToJsonElement()
    {
        // Arrange
        var parameters1 = CreateParametersWithNonJsonElements(new() { ["First"] = 123 });
        var parameters2 = CreateParameters(new() { ["First"] = 456 });

        // Act
        var result = parameters1.DefinitelyEquals(parameters2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void WebRootComponentParameters_DefinitelyEquals_ReturnsFalse_WhenComparingJsonElementParameterToNonJsonElement()
    {
        // Arrange
        var parameters1 = CreateParameters(new() { ["First"] = 123 });
        var parameters2 = CreateParametersWithNonJsonElements(new() { ["First"] = 456 });

        // Act
        var result = parameters1.DefinitelyEquals(parameters2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void WebRootComponentParameters_DefinitelyEquals_ReturnsTrue_WhenComparingEqualNonJsonElementParameters()
    {
        // Arrange
        var parameters1 = CreateParametersWithNonJsonElements(new() { ["First"] = 123 });
        var parameters2 = CreateParametersWithNonJsonElements(new() { ["First"] = 123 });

        // Act
        var result = parameters1.DefinitelyEquals(parameters2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void WebRootComponentParameters_DefinitelyEquals_ReturnsFalse_WhenComparingInequalNonJsonElementParameters()
    {
        // Arrange
        var parameters1 = CreateParametersWithNonJsonElements(new() { ["First"] = 123 });
        var parameters2 = CreateParametersWithNonJsonElements(new() { ["First"] = 456 });

        // Act
        var result = parameters1.DefinitelyEquals(parameters2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void WebRootComponentParameters_DefinitelyEquals_ReturnsTrue_WhenComparingNullParameters()
    {
        // Arrange
        var parameters1 = CreateParametersWithNonJsonElements(new() { ["First"] = null });
        var parameters2 = CreateParametersWithNonJsonElements(new() { ["First"] = null });

        // Act
        var result = parameters1.DefinitelyEquals(parameters2);

        // Assert
        Assert.True(result);
    }

    private static WebRootComponentParameters CreateParameters(Dictionary<string, object> parameters)
    {
        var parameterView = ParameterView.FromDictionary(parameters);
        var (parameterDefinitions, parameterValues) = ComponentParameter.FromParameterView(parameterView);
        for (var i = 0; i < parameterValues.Count; i++)
        {
            // WebRootComponentParameters expects parameter values to be JsonElements.
            var jsonElement = JsonSerializer.SerializeToElement(parameterValues[i]);
            parameterValues[i] = jsonElement;
        }
        return new(parameterView, parameterDefinitions.AsReadOnly(), parameterValues.AsReadOnly());
    }

    private static WebRootComponentParameters CreateParametersWithNonJsonElements(Dictionary<string, object> parameters)
    {
        var parameterView = ParameterView.FromDictionary(parameters);
        var (parameterDefinitions, parameterValues) = ComponentParameter.FromParameterView(parameterView);
        return new(parameterView, parameterDefinitions.AsReadOnly(), parameterValues.AsReadOnly());
    }
}
