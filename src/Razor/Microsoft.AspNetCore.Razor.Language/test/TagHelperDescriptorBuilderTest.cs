// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class TagHelperDescriptorBuilderTest
{
    [Fact]
    public void DisplayName_SetsDescriptorsDisplayName()
    {
        // Arrange
        var expectedDisplayName = "ExpectedDisplayName";
        var builder = TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly");

        // Act
        var descriptor = builder.DisplayName(expectedDisplayName).Build();

        // Assert
        Assert.Equal(expectedDisplayName, descriptor.DisplayName);
    }

    [Fact]
    public void DisplayName_DefaultsToTypeName()
    {
        // Arrange
        var expectedDisplayName = "TestTagHelper";
        var builder = TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly");

        // Act
        var descriptor = builder.Build();

        // Assert
        Assert.Equal(expectedDisplayName, descriptor.DisplayName);
    }
}
