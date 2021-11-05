// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class DefaultAllowedChildTagDescriptorBuilderTest
{
    [Fact]
    public void Build_DisplayNameIsName()
    {
        // Arrange
        var builder = new DefaultAllowedChildTagDescriptorBuilder(null);
        builder.Name = "foo";

        // Act
        var descriptor = builder.Build();

        // Assert
        Assert.Equal("foo", descriptor.DisplayName);
    }
}
