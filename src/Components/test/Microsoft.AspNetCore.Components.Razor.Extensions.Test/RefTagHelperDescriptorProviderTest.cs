// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Components.Razor
{
    public class RefTagHelperDescriptorProviderTest : BaseTagHelperDescriptorProviderTest
    {
        [Fact]
        public void Execute_CreatesDescriptor()
        {
            // Arrange
            var context = TagHelperDescriptorProviderContext.Create();
            var provider = new RefTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            var matches = context.Results.Where(result => result.IsRefTagHelper());
            var item = Assert.Single(matches);

            Assert.Empty(item.AllowedChildTags);
            Assert.Null(item.TagOutputHint);
            Assert.Empty(item.Diagnostics);
            Assert.False(item.HasErrors);
            Assert.Equal(BlazorMetadata.Ref.TagHelperKind, item.Kind);
            Assert.Equal(BlazorMetadata.Ref.RuntimeName, item.Metadata[TagHelperMetadata.Runtime.Name]);
            Assert.False(item.IsDefaultKind());
            Assert.False(item.KindUsesDefaultTagHelperRuntime());

            Assert.Equal(
                "Populates the specified field or property with a reference to the element or component.",
                item.Documentation);

            Assert.Equal("Microsoft.AspNetCore.Components", item.AssemblyName);
            Assert.Equal("Ref", item.Name);
            Assert.Equal("Microsoft.AspNetCore.Components.Ref", item.DisplayName);
            Assert.Equal("Microsoft.AspNetCore.Components.Ref", item.GetTypeName());

            // The tag matching rule for a ref is just the attribute name "ref"
            var rule = Assert.Single(item.TagMatchingRules);
            Assert.Empty(rule.Diagnostics);
            Assert.False(rule.HasErrors);
            Assert.Null(rule.ParentTag);
            Assert.Equal("*", rule.TagName);
            Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

            var requiredAttribute = Assert.Single(rule.Attributes);
            Assert.Empty(requiredAttribute.Diagnostics);
            Assert.Equal("ref", requiredAttribute.DisplayName);
            Assert.Equal("ref", requiredAttribute.Name);
            Assert.Equal(RequiredAttributeDescriptor.NameComparisonMode.FullMatch, requiredAttribute.NameComparison);
            Assert.Null(requiredAttribute.Value);
            Assert.Equal(RequiredAttributeDescriptor.ValueComparisonMode.None, requiredAttribute.ValueComparison);

            var attribute = Assert.Single(item.BoundAttributes);
            Assert.Empty(attribute.Diagnostics);
            Assert.False(attribute.HasErrors);
            Assert.Equal(BlazorMetadata.Ref.TagHelperKind, attribute.Kind);
            Assert.False(attribute.IsDefaultKind());
            Assert.False(attribute.HasIndexer);
            Assert.Null(attribute.IndexerNamePrefix);
            Assert.Null(attribute.IndexerTypeName);
            Assert.False(attribute.IsIndexerBooleanProperty);
            Assert.False(attribute.IsIndexerStringProperty);

            Assert.Equal(
                "Populates the specified field or property with a reference to the element or component.",
                attribute.Documentation);

            Assert.Equal("ref", attribute.Name);
            Assert.Equal("Ref", attribute.GetPropertyName());
            Assert.Equal("object Microsoft.AspNetCore.Components.Ref.Ref", attribute.DisplayName);
            Assert.Equal("System.Object", attribute.TypeName);
            Assert.False(attribute.IsStringProperty);
            Assert.False(attribute.IsBooleanProperty);
            Assert.False(attribute.IsEnum);
        }
    }
}
