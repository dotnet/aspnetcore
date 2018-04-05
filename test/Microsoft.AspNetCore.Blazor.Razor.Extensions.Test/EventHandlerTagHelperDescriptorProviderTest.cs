// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Razor.Extensions
{
    public class EventHandlerTagHelperDescriptorProviderTest : BaseTagHelperDescriptorProviderTest
    {
        [Fact]
        public void Excecute_EventHandler_CreatesDescriptor()
        {
            // Arrange
            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(@"
using System;
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    [EventHandler(""onclick"", typeof(Action<UIMouseEventArgs>))]
    public class EventHandlers
    {
    }
}
"));

            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            var provider = new EventHandlerTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            var matches = GetEventHandlerTagHelpers(context);
            var item = Assert.Single(matches);

            // These are features Event Handler Tag Helpers don't use. Verifying them once here and
            // then ignoring them.
            Assert.Empty(item.AllowedChildTags);
            Assert.Null(item.TagOutputHint);

            // These are features that are invariants of all Event Handler Helpers. Verifying them once
            // here and then ignoring them.
            Assert.Empty(item.Diagnostics);
            Assert.False(item.HasErrors);
            Assert.Equal(BlazorMetadata.EventHandler.TagHelperKind, item.Kind);
            Assert.Equal(BlazorMetadata.EventHandler.RuntimeName, item.Metadata[TagHelperMetadata.Runtime.Name]);
            Assert.False(item.IsDefaultKind());
            Assert.False(item.KindUsesDefaultTagHelperRuntime());

            Assert.Equal(
                "Sets the 'onclick' attribute to the provided string or delegate value. " +
                "A delegate value should be of type 'System.Action<Microsoft.AspNetCore.Blazor.UIMouseEventArgs>'.",
                item.Documentation);

            // These are all trivally derived from the assembly/namespace/type name
            Assert.Equal("TestAssembly", item.AssemblyName);
            Assert.Equal("onclick", item.Name);
            Assert.Equal("Test.EventHandlers", item.DisplayName);
            Assert.Equal("Test.EventHandlers", item.GetTypeName());

            // The tag matching rule for an event handler is just the attribute name
            var rule = Assert.Single(item.TagMatchingRules);
            Assert.Empty(rule.Diagnostics);
            Assert.False(rule.HasErrors);
            Assert.Null(rule.ParentTag);
            Assert.Equal("*", rule.TagName);
            Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

            var requiredAttribute = Assert.Single(rule.Attributes);
            Assert.Empty(requiredAttribute.Diagnostics);
            Assert.Equal("onclick", requiredAttribute.DisplayName);
            Assert.Equal("onclick", requiredAttribute.Name);
            Assert.Equal(RequiredAttributeDescriptor.NameComparisonMode.FullMatch, requiredAttribute.NameComparison);
            Assert.Null(requiredAttribute.Value);
            Assert.Equal(RequiredAttributeDescriptor.ValueComparisonMode.None, requiredAttribute.ValueComparison);

            var attribute = Assert.Single(item.BoundAttributes);

            // Invariants
            Assert.Empty(attribute.Diagnostics);
            Assert.False(attribute.HasErrors);
            Assert.Equal(BlazorMetadata.EventHandler.TagHelperKind, attribute.Kind);
            Assert.False(attribute.IsDefaultKind());
            Assert.False(attribute.HasIndexer);
            Assert.Null(attribute.IndexerNamePrefix);
            Assert.Null(attribute.IndexerTypeName);
            Assert.False(attribute.IsIndexerBooleanProperty);
            Assert.False(attribute.IsIndexerStringProperty);

            Assert.Equal(
                "Sets the 'onclick' attribute to the provided string or delegate value. " +
                "A delegate value should be of type 'System.Action<Microsoft.AspNetCore.Blazor.UIMouseEventArgs>'.",
                attribute.Documentation);

            Assert.Equal("onclick", attribute.Name);
            Assert.Equal("onclick", attribute.GetPropertyName());
            Assert.Equal("string Test.EventHandlers.onclick", attribute.DisplayName);

            // Defined from the property type
            Assert.Equal("System.String", attribute.TypeName);
            Assert.True(attribute.IsStringProperty);
            Assert.False(attribute.IsBooleanProperty);
            Assert.False(attribute.IsEnum);
        }

        private static TagHelperDescriptor[] GetEventHandlerTagHelpers(TagHelperDescriptorProviderContext context)
        {
            return ExcludeBuiltInComponents(context).Where(t => t.IsEventHandlerTagHelper()).ToArray();
        }
    }
}
