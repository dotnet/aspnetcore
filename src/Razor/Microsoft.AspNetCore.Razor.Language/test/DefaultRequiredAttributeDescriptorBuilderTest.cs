// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class DefaultRequiredAttributeDescriptorBuilderTest
{
    [Fact]
    public void Build_DisplayNameIsName_NameComparisonFullMatch()
    {
        // Arrange
        var tagHelperBuilder = new DefaultTagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
        var tagMatchingRuleBuilder = new DefaultTagMatchingRuleDescriptorBuilder(tagHelperBuilder);
        var builder = new DefaultRequiredAttributeDescriptorBuilder(tagMatchingRuleBuilder);

        builder
            .Name("asp-action")
            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch);

        // Act
        var descriptor = builder.Build();

        // Assert
        Assert.Equal("asp-action", descriptor.DisplayName);
    }

    [Fact]
    public void Build_DisplayNameIsNameWithDots_NameComparisonPrefixMatch()
    {
        // Arrange
        var tagHelperBuilder = new DefaultTagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
        var tagMatchingRuleBuilder = new DefaultTagMatchingRuleDescriptorBuilder(tagHelperBuilder);
        var builder = new DefaultRequiredAttributeDescriptorBuilder(tagMatchingRuleBuilder);

        builder
            .Name("asp-route-")
            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch);

        // Act
        var descriptor = builder.Build();

        // Assert
        Assert.Equal("asp-route-...", descriptor.DisplayName);
    }
}
