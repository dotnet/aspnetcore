// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor
{
    public class BindTagHelperDescriptorProviderTest : TagHelperDescriptorProviderTestBase
    {
        [Fact]
        public void Execute_FindsBindTagHelperOnComponentType_Delegate_CreatesDescriptor()
        {
            // Arrange
            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle) { }

        public Task SetParametersAsync(ParameterView parameters)
        {
            return Task.CompletedTask;
        }

        [Parameter]
        public string MyProperty { get; set; }

        [Parameter]
        public Action<string> MyPropertyChanged { get; set; }

        [Parameter]
        public Expression<Func<string>> MyPropertyExpression { get; set; }
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
            matches = AssertAndExcludeFullyQualifiedNameMatchComponents(matches, expectedCount: 1);
            var bind = Assert.Single(matches);

            // These are features Bind Tags Helpers don't use. Verifying them once here and
            // then ignoring them.
            Assert.Empty(bind.AllowedChildTags);
            Assert.Null(bind.TagOutputHint);

            // These are features that are invariants of all Bind Tag Helpers. Verifying them once
            // here and then ignoring them.
            Assert.Empty(bind.Diagnostics);
            Assert.False(bind.HasErrors);
            Assert.Equal(ComponentMetadata.Bind.TagHelperKind, bind.Kind);
            Assert.Equal(ComponentMetadata.Bind.RuntimeName, bind.Metadata[TagHelperMetadata.Runtime.Name]);
            Assert.False(bind.IsDefaultKind());
            Assert.False(bind.KindUsesDefaultTagHelperRuntime());
            Assert.False(bind.IsComponentOrChildContentTagHelper());
            Assert.True(bind.CaseSensitive);

            Assert.Equal("MyProperty", bind.Metadata[ComponentMetadata.Bind.ValueAttribute]);
            Assert.Equal("MyPropertyChanged", bind.Metadata[ComponentMetadata.Bind.ChangeAttribute]);
            Assert.Equal("MyPropertyExpression", bind.Metadata[ComponentMetadata.Bind.ExpressionAttribute]);

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
            Assert.Equal("@bind-MyProperty", requiredAttribute.DisplayName);
            Assert.Equal("@bind-MyProperty", requiredAttribute.Name);
            Assert.Equal(RequiredAttributeDescriptor.NameComparisonMode.FullMatch, requiredAttribute.NameComparison);
            Assert.Null(requiredAttribute.Value);
            Assert.Equal(RequiredAttributeDescriptor.ValueComparisonMode.None, requiredAttribute.ValueComparison);

            var attribute = Assert.Single(bind.BoundAttributes);

            // Invariants
            Assert.Empty(attribute.Diagnostics);
            Assert.False(attribute.HasErrors);
            Assert.Equal(ComponentMetadata.Bind.TagHelperKind, attribute.Kind);
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

            Assert.Equal("@bind-MyProperty", attribute.Name);
            Assert.Equal("MyProperty", attribute.GetPropertyName());
            Assert.Equal("System.Action<System.String> Test.MyComponent.MyProperty", attribute.DisplayName);

            // Defined from the property type
            Assert.Equal("System.Action<System.String>", attribute.TypeName);
            Assert.False(attribute.IsStringProperty);
            Assert.False(attribute.IsBooleanProperty);
            Assert.False(attribute.IsEnum);
        }

        [Fact]
        public void Execute_FindsBindTagHelperOnComponentType_EventCallback_CreatesDescriptor()
        {
            // Arrange
            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle) { }

        public Task SetParametersAsync(ParameterView parameters)
        {
            return Task.CompletedTask;
        }

        [Parameter]
        public string MyProperty { get; set; }

        [Parameter]
        public EventCallback<string> MyPropertyChanged { get; set; }
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
            matches = AssertAndExcludeFullyQualifiedNameMatchComponents(matches, expectedCount: 1);
            var bind = Assert.Single(matches);

            // These are features Bind Tags Helpers don't use. Verifying them once here and
            // then ignoring them.
            Assert.Empty(bind.AllowedChildTags);
            Assert.Null(bind.TagOutputHint);

            // These are features that are invariants of all Bind Tag Helpers. Verifying them once
            // here and then ignoring them.
            Assert.Empty(bind.Diagnostics);
            Assert.False(bind.HasErrors);
            Assert.Equal(ComponentMetadata.Bind.TagHelperKind, bind.Kind);
            Assert.Equal(ComponentMetadata.Bind.RuntimeName, bind.Metadata[TagHelperMetadata.Runtime.Name]);
            Assert.False(bind.IsDefaultKind());
            Assert.False(bind.KindUsesDefaultTagHelperRuntime());
            Assert.False(bind.IsComponentOrChildContentTagHelper());
            Assert.True(bind.CaseSensitive);

            Assert.Equal("MyProperty", bind.Metadata[ComponentMetadata.Bind.ValueAttribute]);
            Assert.Equal("MyPropertyChanged", bind.Metadata[ComponentMetadata.Bind.ChangeAttribute]);

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
            Assert.Equal("@bind-MyProperty", requiredAttribute.DisplayName);
            Assert.Equal("@bind-MyProperty", requiredAttribute.Name);
            Assert.Equal(RequiredAttributeDescriptor.NameComparisonMode.FullMatch, requiredAttribute.NameComparison);
            Assert.Null(requiredAttribute.Value);
            Assert.Equal(RequiredAttributeDescriptor.ValueComparisonMode.None, requiredAttribute.ValueComparison);

            var attribute = Assert.Single(bind.BoundAttributes);

            // Invariants
            Assert.Empty(attribute.Diagnostics);
            Assert.False(attribute.HasErrors);
            Assert.Equal(ComponentMetadata.Bind.TagHelperKind, attribute.Kind);
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

            Assert.Equal("@bind-MyProperty", attribute.Name);
            Assert.Equal("MyProperty", attribute.GetPropertyName());
            Assert.Equal("Microsoft.AspNetCore.Components.EventCallback<System.String> Test.MyComponent.MyProperty", attribute.DisplayName);

            // Defined from the property type
            Assert.Equal("Microsoft.AspNetCore.Components.EventCallback<System.String>", attribute.TypeName);
            Assert.False(attribute.IsStringProperty);
            Assert.False(attribute.IsBooleanProperty);
            Assert.False(attribute.IsEnum);
        }

        [Fact]
        public void Execute_NoMatchedPropertiesOnComponent_IgnoresComponent()
        {
            // Arrange
            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle) { }

        public Task SetParametersAsync(ParameterView parameters)
        {
            return Task.CompletedTask;
        }

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
            matches = AssertAndExcludeFullyQualifiedNameMatchComponents(matches, expectedCount: 0);
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
            matches = AssertAndExcludeFullyQualifiedNameMatchComponents(matches, expectedCount: 0);
            var bind = Assert.Single(matches);

            // These are features Bind Tags Helpers don't use. Verifying them once here and
            // then ignoring them.
            Assert.Empty(bind.AllowedChildTags);
            Assert.Null(bind.TagOutputHint);

            // These are features that are invariants of all Bind Tag Helpers. Verifying them once
            // here and then ignoring them.
            Assert.Empty(bind.Diagnostics);
            Assert.False(bind.HasErrors);
            Assert.Equal(ComponentMetadata.Bind.TagHelperKind, bind.Kind);
            Assert.Equal(bool.TrueString, bind.Metadata[TagHelperMetadata.Common.ClassifyAttributesOnly]);
            Assert.Equal(ComponentMetadata.Bind.RuntimeName, bind.Metadata[TagHelperMetadata.Runtime.Name]);
            Assert.False(bind.IsDefaultKind());
            Assert.False(bind.KindUsesDefaultTagHelperRuntime());
            Assert.False(bind.IsComponentOrChildContentTagHelper());
            Assert.True(bind.CaseSensitive);

            Assert.Equal("myprop", bind.Metadata[ComponentMetadata.Bind.ValueAttribute]);
            Assert.Equal("myevent", bind.Metadata[ComponentMetadata.Bind.ChangeAttribute]);
            Assert.False(bind.IsInputElementBindTagHelper());
            Assert.False(bind.IsInputElementFallbackBindTagHelper());

            Assert.Equal(
                "Binds the provided expression to the 'myprop' attribute and a change event " +
                    "delegate to the 'myevent' attribute.",
                bind.Documentation);

            // These are all trivially derived from the assembly/namespace/type name
            Assert.Equal("Microsoft.AspNetCore.Components", bind.AssemblyName);
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
            Assert.Equal("@bind", requiredAttribute.DisplayName);
            Assert.Equal("@bind", requiredAttribute.Name);
            Assert.Equal(RequiredAttributeDescriptor.NameComparisonMode.FullMatch, requiredAttribute.NameComparison);
            Assert.Null(requiredAttribute.Value);
            Assert.Equal(RequiredAttributeDescriptor.ValueComparisonMode.None, requiredAttribute.ValueComparison);

            var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("@bind", StringComparison.Ordinal));

            // Invariants
            Assert.Empty(attribute.Diagnostics);
            Assert.False(attribute.HasErrors);
            Assert.Equal(ComponentMetadata.Bind.TagHelperKind, attribute.Kind);
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

            Assert.Equal("@bind", attribute.Name);
            Assert.Equal("Bind", attribute.GetPropertyName());
            Assert.Equal("object Test.BindAttributes.Bind", attribute.DisplayName);

            // Defined from the property type
            Assert.Equal("System.Object", attribute.TypeName);
            Assert.False(attribute.IsStringProperty);
            Assert.False(attribute.IsBooleanProperty);
            Assert.False(attribute.IsEnum);

            var parameter = Assert.Single(attribute.BoundAttributeParameters, a => a.Name.Equals("format"));

            // Invariants
            Assert.Empty(parameter.Diagnostics);
            Assert.False(parameter.HasErrors);
            Assert.Equal(ComponentMetadata.Bind.TagHelperKind, parameter.Kind);
            Assert.False(parameter.IsDefaultKind());

            Assert.Equal(
                "Specifies a format to convert the value specified by the '@bind' attribute. " + 
                "The format string can currently only be used with expressions of type <code>DateTime</code>.",
                parameter.Documentation);

            Assert.Equal("format", parameter.Name);
            Assert.Equal("Format_myprop", parameter.GetPropertyName());
            Assert.Equal(":format", parameter.DisplayName);

            // Defined from the property type
            Assert.Equal("System.String", parameter.TypeName);
            Assert.True(parameter.IsStringProperty);
            Assert.False(parameter.IsBooleanProperty);
            Assert.False(parameter.IsEnum);

            parameter = Assert.Single(attribute.BoundAttributeParameters, a => a.Name.Equals("culture"));

            // Invariants
            Assert.Empty(parameter.Diagnostics);
            Assert.False(parameter.HasErrors);
            Assert.Equal(ComponentMetadata.Bind.TagHelperKind, parameter.Kind);
            Assert.False(parameter.IsDefaultKind());

            Assert.Equal(
                "Specifies the culture to use for conversions.",
                parameter.Documentation);

            Assert.Equal("culture", parameter.Name);
            Assert.Equal("Culture", parameter.GetPropertyName());
            Assert.Equal(":culture", parameter.DisplayName);

            // Defined from the property type
            Assert.Equal("System.Globalization.CultureInfo", parameter.TypeName);
            Assert.False(parameter.IsStringProperty);
            Assert.False(parameter.IsBooleanProperty);
            Assert.False(parameter.IsEnum);
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
            matches = AssertAndExcludeFullyQualifiedNameMatchComponents(matches, expectedCount: 0);
            var bind = Assert.Single(matches);

            Assert.Equal("myprop", bind.Metadata[ComponentMetadata.Bind.ValueAttribute]);
            Assert.Equal("myevent", bind.Metadata[ComponentMetadata.Bind.ChangeAttribute]);
            Assert.False(bind.IsInputElementBindTagHelper());
            Assert.False(bind.IsInputElementFallbackBindTagHelper());

            var rule = Assert.Single(bind.TagMatchingRules);
            Assert.Equal("div", rule.TagName);
            Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

            var requiredAttribute = Assert.Single(rule.Attributes);
            Assert.Equal("@bind-myprop", requiredAttribute.DisplayName);
            Assert.Equal("@bind-myprop", requiredAttribute.Name);

            var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("@bind", StringComparison.Ordinal));
            Assert.Equal("@bind-myprop", attribute.Name);
            Assert.Equal("Bind_myprop", attribute.GetPropertyName());
            Assert.Equal("object Test.BindAttributes.Bind_myprop", attribute.DisplayName);

            attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("format", StringComparison.Ordinal));
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
    [BindInputElement(null, null, ""myprop"", ""myevent"", false, null)]
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
            matches = AssertAndExcludeFullyQualifiedNameMatchComponents(matches, expectedCount: 0);
            var bind = Assert.Single(matches);

            Assert.Equal("myprop", bind.Metadata[ComponentMetadata.Bind.ValueAttribute]);
            Assert.Equal("myevent", bind.Metadata[ComponentMetadata.Bind.ChangeAttribute]);
            Assert.False(bind.Metadata.ContainsKey(ComponentMetadata.Bind.TypeAttribute));
            Assert.True(bind.IsInputElementBindTagHelper());
            Assert.True(bind.IsInputElementFallbackBindTagHelper());

            var rule = Assert.Single(bind.TagMatchingRules);
            Assert.Equal("input", rule.TagName);
            Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

            var requiredAttribute = Assert.Single(rule.Attributes);
            Assert.Equal("@bind", requiredAttribute.DisplayName);
            Assert.Equal("@bind", requiredAttribute.Name);

            var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("@bind", StringComparison.Ordinal));
            Assert.Equal("@bind", attribute.Name);
            Assert.Equal("Bind", attribute.GetPropertyName());
            Assert.Equal("object Test.BindAttributes.Bind", attribute.DisplayName);

            var parameter = Assert.Single(attribute.BoundAttributeParameters, a => a.Name.Equals("format"));
            Assert.Equal("format", parameter.Name);
            Assert.Equal("Format_myprop", parameter.GetPropertyName());
            Assert.Equal(":format", parameter.DisplayName);
        }

        [Fact]
        public void Execute_BindOnInputElementWithTypeAttribute_CreatesDescriptor()
        {
            // Arrange
            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindInputElement(""checkbox"", null, ""myprop"", ""myevent"", false, null)]
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
            matches = AssertAndExcludeFullyQualifiedNameMatchComponents(matches, expectedCount: 0);
            var bind = Assert.Single(matches);

            Assert.Equal("myprop", bind.Metadata[ComponentMetadata.Bind.ValueAttribute]);
            Assert.Equal("myevent", bind.Metadata[ComponentMetadata.Bind.ChangeAttribute]);
            Assert.Equal("checkbox", bind.Metadata[ComponentMetadata.Bind.TypeAttribute]);
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
                    Assert.Equal("@bind", a.DisplayName);
                    Assert.Equal("@bind", a.Name);
                });

            var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("@bind", StringComparison.Ordinal));
            Assert.Equal("@bind", attribute.Name);
            Assert.Equal("Bind", attribute.GetPropertyName());
            Assert.Equal("object Test.BindAttributes.Bind", attribute.DisplayName);

            var parameter = Assert.Single(attribute.BoundAttributeParameters, a => a.Name.Equals("format"));
            Assert.Equal("format", parameter.Name);
            Assert.Equal("Format_myprop", parameter.GetPropertyName());
            Assert.Equal(":format", parameter.DisplayName);
        }

        [Fact]
        public void Execute_BindOnInputElementWithTypeAttributeAndSuffix_CreatesDescriptor()
        {
            // Arrange
            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindInputElement(""checkbox"", ""somevalue"", ""myprop"", ""myevent"", false, null)]
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
            matches = AssertAndExcludeFullyQualifiedNameMatchComponents(matches, expectedCount: 0);
            var bind = Assert.Single(matches);

            Assert.Equal("myprop", bind.Metadata[ComponentMetadata.Bind.ValueAttribute]);
            Assert.Equal("myevent", bind.Metadata[ComponentMetadata.Bind.ChangeAttribute]);
            Assert.Equal("checkbox", bind.Metadata[ComponentMetadata.Bind.TypeAttribute]);
            Assert.True(bind.IsInputElementBindTagHelper());
            Assert.False(bind.IsInputElementFallbackBindTagHelper());
            Assert.False(bind.IsInvariantCultureBindTagHelper());
            Assert.Null(bind.GetFormat());

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
                    Assert.Equal("@bind-somevalue", a.DisplayName);
                    Assert.Equal("@bind-somevalue", a.Name);
                });

            var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("@bind", StringComparison.Ordinal));
            Assert.Equal("@bind-somevalue", attribute.Name);
            Assert.Equal("Bind_somevalue", attribute.GetPropertyName());
            Assert.Equal("object Test.BindAttributes.Bind_somevalue", attribute.DisplayName);

            var parameter = Assert.Single(attribute.BoundAttributeParameters, a => a.Name.Equals("format"));
            Assert.Equal("format", parameter.Name);
            Assert.Equal("Format_somevalue", parameter.GetPropertyName());
            Assert.Equal(":format", parameter.DisplayName);
        }

        [Fact]
        public void Execute_BindOnInputElementWithTypeAttributeAndSuffixAndInvariantCultureAndFormat_CreatesDescriptor()
        {
            // Arrange
            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindInputElement(""number"", null, ""value"", ""onchange"", isInvariantCulture: true, format: ""0.00"")]
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
            matches = AssertAndExcludeFullyQualifiedNameMatchComponents(matches, expectedCount: 0);
            var bind = Assert.Single(matches);

            Assert.Equal("value", bind.Metadata[ComponentMetadata.Bind.ValueAttribute]);
            Assert.Equal("onchange", bind.Metadata[ComponentMetadata.Bind.ChangeAttribute]);
            Assert.Equal("number", bind.Metadata[ComponentMetadata.Bind.TypeAttribute]);
            Assert.True(bind.IsInputElementBindTagHelper());
            Assert.False(bind.IsInputElementFallbackBindTagHelper());
            Assert.True(bind.IsInvariantCultureBindTagHelper());
            Assert.Equal("0.00", bind.GetFormat());
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
            Assert.Equal(ComponentMetadata.Bind.TagHelperKind, bind.Kind);
            Assert.Equal(bool.TrueString, bind.Metadata[TagHelperMetadata.Common.ClassifyAttributesOnly]);
            Assert.Equal(ComponentMetadata.Bind.RuntimeName, bind.Metadata[TagHelperMetadata.Runtime.Name]);
            Assert.False(bind.IsDefaultKind());
            Assert.False(bind.KindUsesDefaultTagHelperRuntime());
            Assert.False(bind.IsComponentOrChildContentTagHelper());
            Assert.True(bind.CaseSensitive);

            Assert.False(bind.Metadata.ContainsKey(ComponentMetadata.Bind.ValueAttribute));
            Assert.False(bind.Metadata.ContainsKey(ComponentMetadata.Bind.ChangeAttribute));
            Assert.True(bind.IsFallbackBindTagHelper());

            Assert.Equal(
                "Binds the provided expression to an attribute and a change event, based on the naming of " +
                    "the bind attribute. For example: <code>@bind-value=\"...\"</code> and <code>@bind-value:event=\"onchange\"</code> will assign the " +
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
            Assert.Equal("@bind-...", requiredAttribute.DisplayName);
            Assert.Equal("@bind-", requiredAttribute.Name);
            Assert.Equal(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch, requiredAttribute.NameComparison);
            Assert.Null(requiredAttribute.Value);
            Assert.Equal(RequiredAttributeDescriptor.ValueComparisonMode.None, requiredAttribute.ValueComparison);

            var attribute = Assert.Single(bind.BoundAttributes, a => a.Name.StartsWith("@bind", StringComparison.Ordinal));

            // Invariants
            Assert.Empty(attribute.Diagnostics);
            Assert.False(attribute.HasErrors);
            Assert.Equal(ComponentMetadata.Bind.TagHelperKind, attribute.Kind);
            Assert.False(attribute.IsDefaultKind());
            Assert.False(attribute.IsIndexerBooleanProperty);
            Assert.False(attribute.IsIndexerStringProperty);

            Assert.True(attribute.HasIndexer);
            Assert.Equal("@bind-", attribute.IndexerNamePrefix);
            Assert.Equal("System.Object", attribute.IndexerTypeName);

            Assert.Equal(
                "Binds the provided expression to an attribute and a change event, based on the naming of " +
                    "the bind attribute. For example: <code>@bind-value=\"...\"</code> and <code>@bind-value:event=\"onchange\"</code> will assign the " +
                    "current value of the expression to the 'value' attribute, and assign a delegate that attempts " +
                    "to set the value to the 'onchange' attribute.",
                attribute.Documentation);

            Assert.Equal("@bind-...", attribute.Name);
            Assert.Equal("Bind", attribute.GetPropertyName());
            Assert.Equal(
                "System.Collections.Generic.Dictionary<string, object> Microsoft.AspNetCore.Components.Bind.Bind",
                attribute.DisplayName);

            // Defined from the property type
            Assert.Equal("System.Collections.Generic.Dictionary<string, object>", attribute.TypeName);
            Assert.False(attribute.IsStringProperty);
            Assert.False(attribute.IsBooleanProperty);
            Assert.False(attribute.IsEnum);

            var parameter = Assert.Single(attribute.BoundAttributeParameters, a => a.Name.Equals("format"));

            // Invariants
            Assert.Empty(parameter.Diagnostics);
            Assert.False(parameter.HasErrors);
            Assert.Equal(ComponentMetadata.Bind.TagHelperKind, parameter.Kind);
            Assert.False(parameter.IsDefaultKind());

            Assert.Equal(
                "Specifies a format to convert the value specified by the corresponding bind attribute. " +
                    "For example: <code>@bind-value:format=\"...\"</code> will apply a format string to the value " +
                    "specified in <code>@bind-value=\"...\"</code>. The format string can currently only be used with " +
                    "expressions of type <code>DateTime</code>.",
                parameter.Documentation);

            Assert.Equal("format", parameter.Name);
            Assert.Equal("Format", parameter.GetPropertyName());
            Assert.Equal(":format", parameter.DisplayName);

            // Defined from the property type
            Assert.Equal("System.String", parameter.TypeName);
            Assert.True(parameter.IsStringProperty);
            Assert.False(parameter.IsBooleanProperty);
            Assert.False(parameter.IsEnum);

            parameter = Assert.Single(attribute.BoundAttributeParameters, a => a.Name.Equals("culture"));

            // Invariants
            Assert.Empty(parameter.Diagnostics);
            Assert.False(parameter.HasErrors);
            Assert.Equal(ComponentMetadata.Bind.TagHelperKind, parameter.Kind);
            Assert.False(parameter.IsDefaultKind());

            Assert.Equal(
                "Specifies the culture to use for conversions.",
                parameter.Documentation);

            Assert.Equal("culture", parameter.Name);
            Assert.Equal("Culture", parameter.GetPropertyName());
            Assert.Equal(":culture", parameter.DisplayName);

            // Defined from the property type
            Assert.Equal("System.Globalization.CultureInfo", parameter.TypeName);
            Assert.False(parameter.IsStringProperty);
            Assert.False(parameter.IsBooleanProperty);
            Assert.False(parameter.IsEnum);
        }

        private static TagHelperDescriptor[] GetBindTagHelpers(TagHelperDescriptorProviderContext context)
        {
            return ExcludeBuiltInComponents(context).Where(t => t.IsBindTagHelper()).ToArray();
        }
    }
}
