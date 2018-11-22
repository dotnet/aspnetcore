// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;
using Xunit;

namespace Microsoft.AspNetCore.Components.Razor
{
    public class BindTagHelperDescriptorProviderTest : BaseTagHelperDescriptorProviderTest
    {
        [Fact]
        public void Execute_FindsBindTagHelperOnComponentType_CreatesDescriptor()
        {
            // Arrange
            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : IComponent
    {
        public void Init(RenderHandle renderHandle) { }

        public void SetParameters(ParameterCollection parameters) { }

        [Parameter]
        string MyProperty { get; set; }

        [Parameter]
        Action<string> MyPropertyChanged { get; set; }
    }
}
"));

            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            // We run after component discovery and depend on the results.
            var componentProvider = new ComponentTagHelperDescriptorProvider();
            componentProvider.Execute(context);

            var provider = new BindTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            var matches = GetBindTagHelpers(context);
            var bind = Assert.Single(matches);

            // These are features Bind Tags Helpers don't use. Verifying them once here and
            // then ignoring them.
            Assert.Empty(bind.AllowedChildTags);
            Assert.Null(bind.TagOutputHint);

            // These are features that are invariants of all Bind Tag Helpers. Verifying them once
            // here and then ignoring them.
            Assert.Empty(bind.Diagnostics);
            Assert.False(bind.HasErrors);
            Assert.Equal(BlazorMetadata.Bind.TagHelperKind, bind.Kind);
            Assert.Equal(BlazorMetadata.Bind.RuntimeName, bind.Metadata[TagHelperMetadata.Runtime.Name]);
            Assert.False(bind.IsDefaultKind());
            Assert.False(bind.KindUsesDefaultTagHelperRuntime());

            Assert.Equal("MyProperty", bind.Metadata[BlazorMetadata.Bind.ValueAttribute]);
            Assert.Equal("MyPropertyChanged", bind.Metadata[BlazorMetadata.Bind.ChangeAttribute]);

            Assert.Equal(
                "Binds the provided expression to the 'MyProperty' property and a change event " +
                    "delegate to the 'MyPropertyChanged' property of the component.",
                bind.Documentation);

            // These are all trivially derived from the assembly/namespace/type name
            Assert.Equal("TestAssembly", bind.AssemblyName);
            Assert.Equal("Test.MyComponent", bind.Name);
            Assert.Equal("Test.MyComponent", bind.DisplayName);
            Assert.Equal("Test.MyComponent", bind.GetTypeName());

            var rule = Assert.Single(bind.TagMatchingRules);
            Assert.Empty(rule.Diagnostics);
            Assert.False(rule.HasErrors);
            Assert.Null(rule.ParentTag);
            Assert.Equal("MyComponent", rule.TagName);
            Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

            var requiredAttribute = Assert.Single(rule.Attributes);
            Assert.Empty(requiredAttribute.Diagnostics);
            Assert.Equal("bind-MyProperty", requiredAttribute.DisplayName);
            Assert.Equal("bind-MyProperty", requiredAttribute.Name);
            Assert.Equal(RequiredAttributeDescriptor.NameComparisonMode.FullMatch, requiredAttribute.NameComparison);
            Assert.Null(requiredAttribute.Value);
            Assert.Equal(RequiredAttributeDescriptor.ValueComparisonMode.None, requiredAttribute.ValueComparison);

            var attribute = Assert.Single(bind.BoundAttributes);

            // Invariants
            Assert.Empty(attribute.Diagnostics);
            Assert.False(attribute.HasErrors);
            Assert.Equal(BlazorMetadata.Bind.TagHelperKind, attribute.Kind);
            Assert.False(attribute.IsDefaultKind());
            Assert.False(attribute.HasIndexer);
            Assert.Null(attribute.IndexerNamePrefix);
            Assert.Null(attribute.IndexerTypeName);
            Assert.False(attribute.IsIndexerBooleanProperty);
            Assert.False(attribute.IsIndexerStringProperty);

            Assert.Equal(
                "Binds the provided expression to the 'MyProperty' property and a change event " +
                    "delegate to the 'MyPropertyChanged' property of the component.",
                attribute.Documentation);

            Assert.Equal("bind-MyProperty", attribute.Name);
            Assert.Equal("MyProperty", attribute.GetPropertyName());
            Assert.Equal("string Test.MyComponent.MyProperty", attribute.DisplayName);

            // Defined from the property type
            Assert.Equal("System.String", attribute.TypeName);
            Assert.True(attribute.IsStringProperty);
            Assert.False(attribute.IsBooleanProperty);
            Assert.False(attribute.IsEnum);
        }

        [Fact]
        public void Execute_NoMatchedPropertiesOnComponent_IgnoresComponent()
        {
            // Arrange
            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : IComponent
    {
        public void Init(RenderHandle renderHandle) { }

        public void SetParameters(ParameterCollection parameters) { }

        public string MyProperty { get; set; }

        public Action<string> MyPropertyChangedNotMatch { get; set; }
    }
}
"));

            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            // We run after component discovery and depend on the results.
            var componentProvider = new ComponentTagHelperDescriptorProvider();
            componentProvider.Execute(context);

            var provider = new BindTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            var matches = GetBindTagHelpers(context);
            Assert.Empty(matches);
        }

        [Fact]
        public void Execute_BindOnElement_CreatesDescriptor()
        {
            // Arrange
            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", null, ""myprop"", ""myevent"")]
    public class BindAttributes
    {
    }
}
"));

            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            var provider = new BindTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            var matches = GetBindTagHelpers(context);
            var bind = Assert.Single(matches);

            // These are features Bind Tags Helpers don't use. Verifying them once here and
            // then ignoring them.
            Assert.Empty(bind.AllowedChildTags);
            Assert.Null(bind.TagOutputHint);

            // These are features that are invariants of all Bind Tag Helpers. Verifying them once
            // here and then ignoring them.
            Assert.Empty(bind.Diagnostics);
            Assert.False(bind.HasErrors);
            Assert.Equal(BlazorMetadata.Bind.TagHelperKind, bind.Kind);
            Assert.Equal(BlazorMetadata.Bind.RuntimeName, bind.Metadata[TagHelperMetadata.Runtime.Name]);
            Assert.False(bind.IsDefaultKind());
            Assert.False(bind.KindUsesDefaultTagHelperRuntime());

            Assert.Equal("myprop", bind.Metadata[BlazorMetadata.Bind.ValueAttribute]);
            Assert.Equal("myevent", bind.Metadata[BlazorMetadata.Bind.ChangeAttribute]);
            Assert.False(bind.IsInputElementBindTagHelper());
            Assert.False(bind.IsInputElementFallbackBindTagHelper());

            Assert.Equal(
                "Binds the provided expression to the 'myprop' attribute and a change event " +
                    "delegate to the 'myevent' attribute.",
                bind.Documentation);

            // These are all trivially derived from the assembly/namespace/type name
            Assert.Equal("TestAssembly", bind.AssemblyName);
            Assert.Equal("Bind", bind.Name);
            Assert.Equal("Test.BindAttributes", bind.DisplayName);
            Assert.Equal("Test.BindAttributes", bind.GetTypeName());

            // The tag matching rule for a bind-Component is always the component name + the attribute name
            var rule = Assert.Single(bind.TagMatchingRules);
            Assert.Empty(rule.Diagnostics);
            Assert.False(rule.HasErrors);
            Assert.Null(rule.ParentTag);
            Assert.Equal("div", rule.TagName);
            Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

            var requiredAttribute = Assert.Single(rule.Attributes);
            Assert.Empty(requiredAttribute.Diagnostics);
            Assert.Equal("bind", requiredAttribute.DisplayName);
            Assert.Equal("bind", requiredAttribute.Name);
            Assert.Equal(RequiredAttributeDescriptor.NameComparisonMode.FullMatch, requiredAttribute.NameComparison);
            Assert.Null(requiredAttribute.Value);
            Assert.Equal(RequiredAttributeDescriptor.ValueComparisonMode.None, requiredAttribute.ValueComparison);

            var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("bind"));

            // Invariants
            Assert.Empty(attribute.Diagnostics);
            Assert.False(attribute.HasErrors);
            Assert.Equal(BlazorMetadata.Bind.TagHelperKind, attribute.Kind);
            Assert.False(attribute.IsDefaultKind());
            Assert.False(attribute.HasIndexer);
            Assert.Null(attribute.IndexerNamePrefix);
            Assert.Null(attribute.IndexerTypeName);
            Assert.False(attribute.IsIndexerBooleanProperty);
            Assert.False(attribute.IsIndexerStringProperty);

            Assert.Equal(
                "Binds the provided expression to the 'myprop' attribute and a change event " +
                    "delegate to the 'myevent' attribute.",
                attribute.Documentation);

            Assert.Equal("bind", attribute.Name);
            Assert.Equal("Bind", attribute.GetPropertyName());
            Assert.Equal("object Test.BindAttributes.Bind", attribute.DisplayName);

            // Defined from the property type
            Assert.Equal("System.Object", attribute.TypeName);
            Assert.False(attribute.IsStringProperty);
            Assert.False(attribute.IsBooleanProperty);
            Assert.False(attribute.IsEnum);

            attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("format"));

            // Invariants
            Assert.Empty(attribute.Diagnostics);
            Assert.False(attribute.HasErrors);
            Assert.Equal(BlazorMetadata.Bind.TagHelperKind, attribute.Kind);
            Assert.False(attribute.IsDefaultKind());
            Assert.False(attribute.HasIndexer);
            Assert.Null(attribute.IndexerNamePrefix);
            Assert.Null(attribute.IndexerTypeName);
            Assert.False(attribute.IsIndexerBooleanProperty);
            Assert.False(attribute.IsIndexerStringProperty);

            Assert.Equal(
                "Specifies a format to convert the value specified by the 'bind' attribute. " + 
                "The format string can currently only be used with expressions of type <code>DateTime</code>.",
                attribute.Documentation);

            Assert.Equal("format-myprop", attribute.Name);
            Assert.Equal("Format_myprop", attribute.GetPropertyName());
            Assert.Equal("string Test.BindAttributes.Format_myprop", attribute.DisplayName);

            // Defined from the property type
            Assert.Equal("System.String", attribute.TypeName);
            Assert.True(attribute.IsStringProperty);
            Assert.False(attribute.IsBooleanProperty);
            Assert.False(attribute.IsEnum);
        }

        [Fact]
        public void Execute_BindOnElementWithSuffix_CreatesDescriptor()
        {
            // Arrange
            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", ""myprop"", ""myprop"", ""myevent"")]
    public class BindAttributes
    {
    }
}
"));

            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            var provider = new BindTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            var matches = GetBindTagHelpers(context);
            var bind = Assert.Single(matches);

            Assert.Equal("myprop", bind.Metadata[BlazorMetadata.Bind.ValueAttribute]);
            Assert.Equal("myevent", bind.Metadata[BlazorMetadata.Bind.ChangeAttribute]);
            Assert.False(bind.IsInputElementBindTagHelper());
            Assert.False(bind.IsInputElementFallbackBindTagHelper());

            var rule = Assert.Single(bind.TagMatchingRules);
            Assert.Equal("div", rule.TagName);
            Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

            var requiredAttribute = Assert.Single(rule.Attributes);
            Assert.Equal("bind-myprop", requiredAttribute.DisplayName);
            Assert.Equal("bind-myprop", requiredAttribute.Name);

            var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("bind"));
            Assert.Equal("bind-myprop", attribute.Name);
            Assert.Equal("Bind_myprop", attribute.GetPropertyName());
            Assert.Equal("object Test.BindAttributes.Bind_myprop", attribute.DisplayName);

            attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("format"));
            Assert.Equal("format-myprop", attribute.Name);
            Assert.Equal("Format_myprop", attribute.GetPropertyName());
            Assert.Equal("string Test.BindAttributes.Format_myprop", attribute.DisplayName);
        }

        [Fact]
        public void Execute_BindOnInputElementWithoutTypeAttribute_CreatesDescriptor()
        {
            // Arrange
            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindInputElement(null, null, ""myprop"", ""myevent"")]
    public class BindAttributes
    {
    }
}
"));

            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            var provider = new BindTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            var matches = GetBindTagHelpers(context);
            var bind = Assert.Single(matches);

            Assert.Equal("myprop", bind.Metadata[BlazorMetadata.Bind.ValueAttribute]);
            Assert.Equal("myevent", bind.Metadata[BlazorMetadata.Bind.ChangeAttribute]);
            Assert.False(bind.Metadata.ContainsKey(BlazorMetadata.Bind.TypeAttribute));
            Assert.True(bind.IsInputElementBindTagHelper());
            Assert.True(bind.IsInputElementFallbackBindTagHelper());

            var rule = Assert.Single(bind.TagMatchingRules);
            Assert.Equal("input", rule.TagName);
            Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

            var requiredAttribute = Assert.Single(rule.Attributes);
            Assert.Equal("bind", requiredAttribute.DisplayName);
            Assert.Equal("bind", requiredAttribute.Name);

            var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("bind"));
            Assert.Equal("bind", attribute.Name);
            Assert.Equal("Bind", attribute.GetPropertyName());
            Assert.Equal("object Test.BindAttributes.Bind", attribute.DisplayName);

            attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("format"));
            Assert.Equal("format-myprop", attribute.Name);
            Assert.Equal("Format_myprop", attribute.GetPropertyName());
            Assert.Equal("string Test.BindAttributes.Format_myprop", attribute.DisplayName);
        }

        [Fact]
        public void Execute_BindOnInputElementWithTypeAttribute_CreatesDescriptor()
        {
            // Arrange
            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindInputElement(""checkbox"", null, ""myprop"", ""myevent"")]
    public class BindAttributes
    {
    }
}
"));

            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            var provider = new BindTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            var matches = GetBindTagHelpers(context);
            var bind = Assert.Single(matches);

            Assert.Equal("myprop", bind.Metadata[BlazorMetadata.Bind.ValueAttribute]);
            Assert.Equal("myevent", bind.Metadata[BlazorMetadata.Bind.ChangeAttribute]);
            Assert.Equal("checkbox", bind.Metadata[BlazorMetadata.Bind.TypeAttribute]);
            Assert.True(bind.IsInputElementBindTagHelper());
            Assert.False(bind.IsInputElementFallbackBindTagHelper());

            var rule = Assert.Single(bind.TagMatchingRules);
            Assert.Equal("input", rule.TagName);
            Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

            Assert.Collection(
                rule.Attributes,
                a =>
                {
                    Assert.Equal("type", a.DisplayName);
                    Assert.Equal("type", a.Name);
                    Assert.Equal(RequiredAttributeDescriptor.NameComparisonMode.FullMatch, a.NameComparison);
                    Assert.Equal("checkbox", a.Value);
                    Assert.Equal(RequiredAttributeDescriptor.ValueComparisonMode.FullMatch, a.ValueComparison);
                },
                a =>
                {
                    Assert.Equal("bind", a.DisplayName);
                    Assert.Equal("bind", a.Name);
                });

            var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("bind"));
            Assert.Equal("bind", attribute.Name);
            Assert.Equal("Bind", attribute.GetPropertyName());
            Assert.Equal("object Test.BindAttributes.Bind", attribute.DisplayName);

            attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("format"));
            Assert.Equal("format-myprop", attribute.Name);
            Assert.Equal("Format_myprop", attribute.GetPropertyName());
            Assert.Equal("string Test.BindAttributes.Format_myprop", attribute.DisplayName);
        }

        [Fact]
        public void Execute_BindOnInputElementWithTypeAttributeAndSuffix_CreatesDescriptor()
        {
            // Arrange
            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindInputElement(""checkbox"", ""somevalue"", ""myprop"", ""myevent"")]
    public class BindAttributes
    {
    }
}
"));

            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            var provider = new BindTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            var matches = GetBindTagHelpers(context);
            var bind = Assert.Single(matches);

            Assert.Equal("myprop", bind.Metadata[BlazorMetadata.Bind.ValueAttribute]);
            Assert.Equal("myevent", bind.Metadata[BlazorMetadata.Bind.ChangeAttribute]);
            Assert.Equal("checkbox", bind.Metadata[BlazorMetadata.Bind.TypeAttribute]);
            Assert.True(bind.IsInputElementBindTagHelper());
            Assert.False(bind.IsInputElementFallbackBindTagHelper());

            var rule = Assert.Single(bind.TagMatchingRules);
            Assert.Equal("input", rule.TagName);
            Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

            Assert.Collection(
                rule.Attributes,
                a =>
                {
                    Assert.Equal("type", a.DisplayName);
                    Assert.Equal("type", a.Name);
                    Assert.Equal(RequiredAttributeDescriptor.NameComparisonMode.FullMatch, a.NameComparison);
                    Assert.Equal("checkbox", a.Value);
                    Assert.Equal(RequiredAttributeDescriptor.ValueComparisonMode.FullMatch, a.ValueComparison);
                },
                a =>
                {
                    Assert.Equal("bind-somevalue", a.DisplayName);
                    Assert.Equal("bind-somevalue", a.Name);
                });

            var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("bind"));
            Assert.Equal("bind-somevalue", attribute.Name);
            Assert.Equal("Bind_somevalue", attribute.GetPropertyName());
            Assert.Equal("object Test.BindAttributes.Bind_somevalue", attribute.DisplayName);

            attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("format"));
            Assert.Equal("format-somevalue", attribute.Name);
            Assert.Equal("Format_somevalue", attribute.GetPropertyName());
            Assert.Equal("string Test.BindAttributes.Format_somevalue", attribute.DisplayName);
        }

        [Fact]
        public void Execute_BindFallback_CreatesDescriptor()
        {
            // Arrange
            var compilation = BaseCompilation;
            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            var provider = new BindTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            var bind = Assert.Single(context.Results, r => r.IsFallbackBindTagHelper());

            // These are features Bind Tags Helpers don't use. Verifying them once here and
            // then ignoring them.
            Assert.Empty(bind.AllowedChildTags);
            Assert.Null(bind.TagOutputHint);

            // These are features that are invariants of all Bind Tag Helpers. Verifying them once
            // here and then ignoring them.
            Assert.Empty(bind.Diagnostics);
            Assert.False(bind.HasErrors);
            Assert.Equal(BlazorMetadata.Bind.TagHelperKind, bind.Kind);
            Assert.Equal(BlazorMetadata.Bind.RuntimeName, bind.Metadata[TagHelperMetadata.Runtime.Name]);
            Assert.False(bind.IsDefaultKind());
            Assert.False(bind.KindUsesDefaultTagHelperRuntime());

            Assert.False(bind.Metadata.ContainsKey(BlazorMetadata.Bind.ValueAttribute));
            Assert.False(bind.Metadata.ContainsKey(BlazorMetadata.Bind.ChangeAttribute));
            Assert.True(bind.IsFallbackBindTagHelper());

            Assert.Equal(
                "Binds the provided expression to an attribute and a change event, based on the naming of " +
                    "the bind attribute. For example: <code>bind-value-onchange=\"...\"</code> will assign the " +
                    "current value of the expression to the 'value' attribute, and assign a delegate that attempts " +
                    "to set the value to the 'onchange' attribute.",
                bind.Documentation);

            // These are all trivially derived from the assembly/namespace/type name
            Assert.Equal("Microsoft.AspNetCore.Components", bind.AssemblyName);
            Assert.Equal("Bind", bind.Name);
            Assert.Equal("Microsoft.AspNetCore.Components.Bind", bind.DisplayName);
            Assert.Equal("Microsoft.AspNetCore.Components.Bind", bind.GetTypeName());

            // The tag matching rule for a bind-Component is always the component name + the attribute name
            var rule = Assert.Single(bind.TagMatchingRules);
            Assert.Empty(rule.Diagnostics);
            Assert.False(rule.HasErrors);
            Assert.Null(rule.ParentTag);
            Assert.Equal("*", rule.TagName);
            Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

            var requiredAttribute = Assert.Single(rule.Attributes);
            Assert.Empty(requiredAttribute.Diagnostics);
            Assert.Equal("bind-...", requiredAttribute.DisplayName);
            Assert.Equal("bind-", requiredAttribute.Name);
            Assert.Equal(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch, requiredAttribute.NameComparison);
            Assert.Null(requiredAttribute.Value);
            Assert.Equal(RequiredAttributeDescriptor.ValueComparisonMode.None, requiredAttribute.ValueComparison);

            var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("bind"));

            // Invariants
            Assert.Empty(attribute.Diagnostics);
            Assert.False(attribute.HasErrors);
            Assert.Equal(BlazorMetadata.Bind.TagHelperKind, attribute.Kind);
            Assert.False(attribute.IsDefaultKind());
            Assert.False(attribute.IsIndexerBooleanProperty);
            Assert.False(attribute.IsIndexerStringProperty);

            Assert.True(attribute.HasIndexer);
            Assert.Equal("bind-", attribute.IndexerNamePrefix);
            Assert.Equal("System.Object", attribute.IndexerTypeName);

            Assert.Equal(
                "Binds the provided expression to an attribute and a change event, based on the naming of " +
                    "the bind attribute. For example: <code>bind-value-onchange=\"...\"</code> will assign the " +
                    "current value of the expression to the 'value' attribute, and assign a delegate that attempts " +
                    "to set the value to the 'onchange' attribute.",
                attribute.Documentation);

            Assert.Equal("bind-...", attribute.Name);
            Assert.Equal("Bind", attribute.GetPropertyName());
            Assert.Equal(
                "System.Collections.Generic.Dictionary<string, object> Microsoft.AspNetCore.Components.Bind.Bind",
                attribute.DisplayName);

            // Defined from the property type
            Assert.Equal("System.Collections.Generic.Dictionary<string, object>", attribute.TypeName);
            Assert.False(attribute.IsStringProperty);
            Assert.False(attribute.IsBooleanProperty);
            Assert.False(attribute.IsEnum);

            attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("format"));

            // Invariants
            Assert.Empty(attribute.Diagnostics);
            Assert.False(attribute.HasErrors);
            Assert.Equal(BlazorMetadata.Bind.TagHelperKind, attribute.Kind);
            Assert.False(attribute.IsDefaultKind());
            Assert.True(attribute.HasIndexer);
            Assert.Equal("format-", attribute.IndexerNamePrefix);
            Assert.Equal("System.String", attribute.IndexerTypeName);
            Assert.False(attribute.IsIndexerBooleanProperty);
            Assert.True(attribute.IsIndexerStringProperty);

            Assert.Equal(
                "Specifies a format to convert the value specified by the corresponding bind attribute. " +
                    "For example: <code>format-value=\"...\"</code> will apply a format string to the value " +
                    "specified in <code>bind-value-...</code>. The format string can currently only be used with " +
                    "expressions of type <code>DateTime</code>.",
                attribute.Documentation);

            Assert.Equal("format-...", attribute.Name);
            Assert.Equal("Format", attribute.GetPropertyName());
            Assert.Equal(
                "System.Collections.Generic.Dictionary<string, string> Microsoft.AspNetCore.Components.Bind.Format",
                attribute.DisplayName);

            // Defined from the property type
            Assert.Equal("System.Collections.Generic.Dictionary<string, string>", attribute.TypeName);
            Assert.False(attribute.IsStringProperty);
            Assert.False(attribute.IsBooleanProperty);
            Assert.False(attribute.IsEnum);
        }


        private static TagHelperDescriptor[] GetBindTagHelpers(TagHelperDescriptorProviderContext context)
        {
            return ExcludeBuiltInComponents(context).Where(t => t.IsBindTagHelper()).ToArray();
        }
    }
}
