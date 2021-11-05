// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class RazorDiagnosticDescriptorTest
{
    [Fact]
    public void RazorDiagnosticDescriptor_Ctor()
    {
        // Arrange & Act
        var descriptor = new RazorDiagnosticDescriptor("RZ0001", () => "Hello, World!", RazorDiagnosticSeverity.Error);

        // Assert
        Assert.Equal("RZ0001", descriptor.Id);
        Assert.Equal(RazorDiagnosticSeverity.Error, descriptor.Severity);
        Assert.Equal("Hello, World!", descriptor.GetMessageFormat());
    }

    [Fact]
    public void RazorDiagnosticDescriptor_Equals()
    {
        // Arrange
        var descriptor1 = new RazorDiagnosticDescriptor("RZ0001", () => "a!", RazorDiagnosticSeverity.Error);
        var descriptor2 = new RazorDiagnosticDescriptor("RZ0001", () => "b!", RazorDiagnosticSeverity.Error);

        // Act
        var result = descriptor1.Equals(descriptor2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RazorDiagnosticDescriptor_NotEquals()
    {
        // Arrange
        var descriptor1 = new RazorDiagnosticDescriptor("RZ0001", () => "a!", RazorDiagnosticSeverity.Error);
        var descriptor2 = new RazorDiagnosticDescriptor("RZ0002", () => "b!", RazorDiagnosticSeverity.Error);

        // Act
        var result = descriptor1.Equals(descriptor2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RazorDiagnosticDescriptor_HashCodesEqual()
    {
        // Arrange
        var descriptor1 = new RazorDiagnosticDescriptor("RZ0001", () => "a!", RazorDiagnosticSeverity.Error);
        var descriptor2 = new RazorDiagnosticDescriptor("RZ0001", () => "b!", RazorDiagnosticSeverity.Error);

        // Act
        var result = descriptor1.GetHashCode() == descriptor2.GetHashCode();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RazorDiagnosticDescriptor_HashCodesNotEqual()
    {
        // Arrange
        var descriptor1 = new RazorDiagnosticDescriptor("RZ0001", () => "a!", RazorDiagnosticSeverity.Error);
        var descriptor2 = new RazorDiagnosticDescriptor("RZ0002", () => "b!", RazorDiagnosticSeverity.Error);

        // Act
        var result = descriptor1.GetHashCode() == descriptor2.GetHashCode();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RazorDiagnosticDescriptor_NullMessage()
    {
        // Arrange & Act
        var descriptor = new RazorDiagnosticDescriptor("RZ0001", () => null, RazorDiagnosticSeverity.Error);

        // Assert
        Assert.Equal("RZ0001", descriptor.Id);
        Assert.Equal(RazorDiagnosticSeverity.Error, descriptor.Severity);
        Assert.Equal("Encountered diagnostic 'RZ0001'.", descriptor.GetMessageFormat());
    }
}
