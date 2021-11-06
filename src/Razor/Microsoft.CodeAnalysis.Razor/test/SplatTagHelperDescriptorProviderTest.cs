// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor;

public class SplatTagHelperDescriptorProviderTest : TagHelperDescriptorProviderTestBase
{
    [Fact]
    public void Execute_CreatesDescriptor()
    {
        // Arrange
        var context = TagHelperDescriptorProviderContext.Create();
        context.SetCompilation(BaseCompilation);

        var provider = new SplatTagHelperDescriptorProvider();

        // Act
        provider.Execute(context);

        // Assert
        var matches = context.Results.Where(result => result.IsSplatTagHelper());
        var item = Assert.Single(matches);

        Assert.Empty(item.AllowedChildTags);
        Assert.Null(item.TagOutputHint);
        Assert.Empty(item.Diagnostics);
        Assert.False(item.HasErrors);
        Assert.Equal(ComponentMetadata.Splat.TagHelperKind, item.Kind);
        Assert.Equal(bool.TrueString, item.Metadata[TagHelperMetadata.Common.ClassifyAttributesOnly]);
        Assert.Equal(ComponentMetadata.Splat.RuntimeName, item.Metadata[TagHelperMetadata.Runtime.Name]);
        Assert.False(item.IsDefaultKind());
        Assert.False(item.KindUsesDefaultTagHelperRuntime());
        Assert.True(item.CaseSensitive);

        Assert.Equal(
            "Merges a collection of attributes into the current element or component.",
            item.Documentation);

        Assert.Equal("Microsoft.AspNetCore.Components", item.AssemblyName);
        Assert.Equal("Attributes", item.Name);
        Assert.Equal("Microsoft.AspNetCore.Components.Attributes", item.DisplayName);
        Assert.Equal("Microsoft.AspNetCore.Components.Attributes", item.GetTypeName());

        var rule = Assert.Single(item.TagMatchingRules);
        Assert.Empty(rule.Diagnostics);
        Assert.False(rule.HasErrors);
        Assert.Null(rule.ParentTag);
        Assert.Equal("*", rule.TagName);
        Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

        var requiredAttribute = Assert.Single(rule.Attributes);
        Assert.Empty(requiredAttribute.Diagnostics);
        Assert.Equal("@attributes", requiredAttribute.DisplayName);
        Assert.Equal("@attributes", requiredAttribute.Name);
        Assert.Equal(RequiredAttributeDescriptor.NameComparisonMode.FullMatch, requiredAttribute.NameComparison);
        Assert.Null(requiredAttribute.Value);
        Assert.Equal(RequiredAttributeDescriptor.ValueComparisonMode.None, requiredAttribute.ValueComparison);

        var attribute = Assert.Single(item.BoundAttributes);
        Assert.Empty(attribute.Diagnostics);
        Assert.False(attribute.HasErrors);
        Assert.Equal(ComponentMetadata.Splat.TagHelperKind, attribute.Kind);
        Assert.False(attribute.IsDefaultKind());
        Assert.False(attribute.HasIndexer);
        Assert.Null(attribute.IndexerNamePrefix);
        Assert.Null(attribute.IndexerTypeName);
        Assert.False(attribute.IsIndexerBooleanProperty);
        Assert.False(attribute.IsIndexerStringProperty);

        Assert.Equal(
            "Merges a collection of attributes into the current element or component.",
            attribute.Documentation);

        Assert.Equal("@attributes", attribute.Name);
        Assert.Equal("Attributes", attribute.GetPropertyName());
        Assert.Equal("object Microsoft.AspNetCore.Components.Attributes.Attributes", attribute.DisplayName);
        Assert.Equal("System.Object", attribute.TypeName);
        Assert.False(attribute.IsStringProperty);
        Assert.False(attribute.IsBooleanProperty);
        Assert.False(attribute.IsEnum);
    }
}
