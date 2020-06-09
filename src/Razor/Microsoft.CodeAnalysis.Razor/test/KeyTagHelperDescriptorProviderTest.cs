// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor
{
    public class KeyTagHelperDescriptorProviderTest : TagHelperDescriptorProviderTestBase
    {
        [Fact]
        public void Execute_CreatesDescriptor()
        {
            // Arrange
            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(BaseCompilation);

            var provider = new KeyTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            var matches = context.Results.Where(result => result.IsKeyTagHelper());
            var item = Assert.Single(matches);

            Assert.Empty(item.AllowedChildTags);
            Assert.Null(item.TagOutputHint);
            Assert.Empty(item.Diagnostics);
            Assert.False(item.HasErrors);
            Assert.Equal(ComponentMetadata.Key.TagHelperKind, item.Kind);
            Assert.Equal(bool.TrueString, item.Metadata[TagHelperMetadata.Common.ClassifyAttributesOnly]);
            Assert.Equal(ComponentMetadata.Key.RuntimeName, item.Metadata[TagHelperMetadata.Runtime.Name]);
            Assert.False(item.IsDefaultKind());
            Assert.False(item.KindUsesDefaultTagHelperRuntime());
            Assert.False(item.IsComponentOrChildContentTagHelper());
            Assert.True(item.CaseSensitive);

            Assert.Equal(
                "Ensures that the component or element will be preserved across renders if (and only if) the supplied key value matches.",
                item.Documentation);

            Assert.Equal("Microsoft.AspNetCore.Components", item.AssemblyName);
            Assert.Equal("Key", item.Name);
            Assert.Equal("Microsoft.AspNetCore.Components.Key", item.DisplayName);
            Assert.Equal("Microsoft.AspNetCore.Components.Key", item.GetTypeName());

            // The tag matching rule for a key is just the attribute name "key"
            var rule = Assert.Single(item.TagMatchingRules);
            Assert.Empty(rule.Diagnostics);
            Assert.False(rule.HasErrors);
            Assert.Null(rule.ParentTag);
            Assert.Equal("*", rule.TagName);
            Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

            var requiredAttribute = Assert.Single(rule.Attributes);
            Assert.Empty(requiredAttribute.Diagnostics);
            Assert.Equal("@key", requiredAttribute.DisplayName);
            Assert.Equal("@key", requiredAttribute.Name);
            Assert.Equal(RequiredAttributeDescriptor.NameComparisonMode.FullMatch, requiredAttribute.NameComparison);
            Assert.Null(requiredAttribute.Value);
            Assert.Equal(RequiredAttributeDescriptor.ValueComparisonMode.None, requiredAttribute.ValueComparison);

            var attribute = Assert.Single(item.BoundAttributes);
            Assert.Empty(attribute.Diagnostics);
            Assert.False(attribute.HasErrors);
            Assert.Equal(ComponentMetadata.Key.TagHelperKind, attribute.Kind);
            Assert.False(attribute.IsDefaultKind());
            Assert.False(attribute.HasIndexer);
            Assert.Null(attribute.IndexerNamePrefix);
            Assert.Null(attribute.IndexerTypeName);
            Assert.False(attribute.IsIndexerBooleanProperty);
            Assert.False(attribute.IsIndexerStringProperty);

            Assert.Equal(
                "Ensures that the component or element will be preserved across renders if (and only if) the supplied key value matches.",
                attribute.Documentation);

            Assert.Equal("@key", attribute.Name);
            Assert.Equal("Key", attribute.GetPropertyName());
            Assert.Equal("object Microsoft.AspNetCore.Components.Key.Key", attribute.DisplayName);
            Assert.Equal("System.Object", attribute.TypeName);
            Assert.False(attribute.IsStringProperty);
            Assert.False(attribute.IsBooleanProperty);
            Assert.False(attribute.IsEnum);
        }
    }
}
