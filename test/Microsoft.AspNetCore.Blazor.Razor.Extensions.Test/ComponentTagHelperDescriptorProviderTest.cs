// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    public class ComponentTagHelperDescriptorProviderTest : BaseTagHelperDescriptorProviderTest
    {
        [Fact]
        public void Excecute_FindsIComponentType_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : IComponent
    {
        public void Init(RenderHandle renderHandle) { }

        public void SetParameters(ParameterCollection parameters) { }

        [Parameter]
        private string MyProperty { get; set; }
    }
}

"));

            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            var provider = new ComponentTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            var components = ExcludeBuiltInComponents(context);
            var component = Assert.Single(components);

            // These are features Components don't use. Verifying them once here and
            // then ignoring them.
            Assert.Empty(component.AllowedChildTags);
            Assert.Null(component.TagOutputHint);

            // These are features that are invariants of all Components. Verifying them once
            // here and then ignoring them.
            Assert.Empty(component.Diagnostics);
            Assert.False(component.HasErrors);
            Assert.Equal(BlazorMetadata.Component.TagHelperKind, component.Kind);
            Assert.False(component.IsDefaultKind());
            Assert.False(component.KindUsesDefaultTagHelperRuntime());

            // No documentation in this test
            Assert.Null(component.Documentation);

            // These are all trivially derived from the assembly/namespace/type name
            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent", component.Name);
            Assert.Equal("Test.MyComponent", component.DisplayName);
            Assert.Equal("Test.MyComponent", component.GetTypeName());

            // Our use of matching rules is also very simple, and derived from the name. Verifying
            // it once in detail here and then ignoring it.
            var rule = Assert.Single(component.TagMatchingRules);
            Assert.Empty(rule.Attributes);
            Assert.Empty(rule.Diagnostics);
            Assert.False(rule.HasErrors);
            Assert.Null(rule.ParentTag);
            Assert.Equal("MyComponent", rule.TagName);
            Assert.Equal(TagStructure.Unspecified, rule.TagStructure);

            // Our use of metadata is also (for now) an invariant for all Components - other than the type name
            // which is trivial. Verifying it once in detail and then ignoring it.
            Assert.Collection(
                component.Metadata.OrderBy(kvp => kvp.Key),
                kvp => { Assert.Equal(TagHelperMetadata.Common.TypeName, kvp.Key); Assert.Equal("Test.MyComponent", kvp.Value); },
                kvp => { Assert.Equal(TagHelperMetadata.Runtime.Name, kvp.Key); Assert.Equal("Blazor.IComponent", kvp.Value); });

            // Our use of bound attributes is what tests will focus on. As you might expect right now, this test
            // is going to cover a lot of trivial stuff that will be true for all components/component-properties.
            var attribute = Assert.Single(component.BoundAttributes);

            // Invariants
            Assert.Empty(attribute.Diagnostics);
            Assert.False(attribute.HasErrors);
            Assert.Equal("Blazor.Component", attribute.Kind);
            Assert.False(attribute.IsDefaultKind());

            // Related to dictionaries/indexers, not supported currently, not sure if we ever will
            Assert.False(attribute.HasIndexer);
            Assert.Null(attribute.IndexerNamePrefix);
            Assert.Null(attribute.IndexerTypeName);
            Assert.False(attribute.IsIndexerBooleanProperty);
            Assert.False(attribute.IsIndexerStringProperty);

            // No documentation in this test
            Assert.Null(attribute.Documentation);

            // Names are trivially derived from the property name
            Assert.Equal("MyProperty", attribute.Name);
            Assert.Equal("MyProperty", attribute.GetPropertyName());
            Assert.Equal("string Test.MyComponent.MyProperty", attribute.DisplayName);

            // Defined from the property type
            Assert.Equal("System.String", attribute.TypeName);
            Assert.True(attribute.IsStringProperty);
            Assert.False(attribute.IsBooleanProperty);
            Assert.False(attribute.IsEnum);

            // Our use of metadata is also (for now) an invariant for all Component properties - other than the type name
            // which is trivial. Verifying it once in detail and then ignoring it.
            Assert.Collection(
                attribute.Metadata.OrderBy(kvp => kvp.Key),
                kvp => { Assert.Equal(TagHelperMetadata.Common.PropertyName, kvp.Key); Assert.Equal("MyProperty", kvp.Value); });
        }

        [Fact]
        public void Excecute_FindsBlazorComponentType_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        [Parameter]
        string MyProperty { get; set; }
    }
}

"));

            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            var provider = new ComponentTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            var components = ExcludeBuiltInComponents(context);
            var component = Assert.Single(components);

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent", component.Name);

            var attribute = Assert.Single(component.BoundAttributes);
            Assert.Equal("MyProperty", attribute.Name);
            Assert.Equal("System.String", attribute.TypeName);
        }

        [Fact] // bool properties support minimized attributes
        public void Excecute_BoolProperty_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        [Parameter]
        bool MyProperty { get; set; }
    }
}

"));

            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            var provider = new ComponentTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            var components = ExcludeBuiltInComponents(context);
            var component = Assert.Single(components);

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent", component.Name);

            var attribute = Assert.Single(component.BoundAttributes);
            Assert.Equal("MyProperty", attribute.Name);
            Assert.Equal("System.Boolean", attribute.TypeName);

            Assert.False(attribute.HasIndexer);
            Assert.True(attribute.IsBooleanProperty);
            Assert.False(attribute.IsEnum);
            Assert.False(attribute.IsStringProperty);
        }

        [Fact] // enum properties have some special intellisense behavior
        public void Excecute_EnumProperty_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public enum MyEnum
    {
        One,
        Two
    }

    public class MyComponent : BlazorComponent
    {
        [Parameter]
        MyEnum MyProperty { get; set; }
    }
}

"));

            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            var provider = new ComponentTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            var components = ExcludeBuiltInComponents(context);
            var component = Assert.Single(components);

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent", component.Name);

            var attribute = Assert.Single(component.BoundAttributes);
            Assert.Equal("MyProperty", attribute.Name);
            Assert.Equal("Test.MyEnum", attribute.TypeName);

            Assert.False(attribute.HasIndexer);
            Assert.False(attribute.IsBooleanProperty);
            Assert.True(attribute.IsEnum);
            Assert.False(attribute.IsStringProperty);
        }

        [Fact]
        public void Execute_DelegateProperty_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using System;
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        [Parameter]
        Action<UIMouseEventArgs> OnClick { get; set; }
    }
}

"));

            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            var provider = new ComponentTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            var components = ExcludeBuiltInComponents(context);
            var component = Assert.Single(components);

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent", component.Name);

            var attribute = Assert.Single(component.BoundAttributes);
            Assert.Equal("OnClick", attribute.Name);
            Assert.Equal("System.Action<Microsoft.AspNetCore.Blazor.UIMouseEventArgs>", attribute.TypeName);

            Assert.False(attribute.HasIndexer);
            Assert.False(attribute.IsBooleanProperty);
            Assert.False(attribute.IsEnum);
            Assert.False(attribute.IsStringProperty);
            Assert.True(attribute.IsDelegateProperty());
            Assert.False(attribute.IsChildContentProperty());
        }

        [Fact]
        public void Execute_RenderFragmentProperty_CreatesDescriptors()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        [Parameter]
        RenderFragment ChildContent2 { get; set; }
    }
}

"));

            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            var provider = new ComponentTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            var components = ExcludeBuiltInComponents(context);
            var component = Assert.Single(components, c => c.IsComponentTagHelper());

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent", component.Name);

            var attribute = Assert.Single(component.BoundAttributes);
            Assert.Equal("ChildContent2", attribute.Name);
            Assert.Equal("Microsoft.AspNetCore.Blazor.RenderFragment", attribute.TypeName);

            Assert.False(attribute.HasIndexer);
            Assert.False(attribute.IsBooleanProperty);
            Assert.False(attribute.IsEnum);
            Assert.False(attribute.IsStringProperty);
            Assert.False(attribute.IsDelegateProperty()); // We treat RenderFragment as separate from generalized delegates
            Assert.True(attribute.IsChildContentProperty());
            Assert.False(attribute.IsParameterizedChildContentProperty());

            var childContent = Assert.Single(components, c => c.IsChildContentTagHelper());

            Assert.Equal("TestAssembly", childContent.AssemblyName);
            Assert.Equal("Test.MyComponent.ChildContent2", childContent.Name);

            Assert.Empty(childContent.BoundAttributes);
        }

        [Fact]
        public void Execute_RenderFragmentOfTProperty_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        [Parameter]
        RenderFragment<string> ChildContent2 { get; set; }
    }
}

"));

            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            var provider = new ComponentTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            var components = ExcludeBuiltInComponents(context);
            var component = Assert.Single(components, c => c.IsComponentTagHelper());

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent", component.Name);

            var attribute = Assert.Single(component.BoundAttributes);
            Assert.Equal("ChildContent2", attribute.Name);
            Assert.Equal("Microsoft.AspNetCore.Blazor.RenderFragment<System.String>", attribute.TypeName);

            Assert.False(attribute.HasIndexer);
            Assert.False(attribute.IsBooleanProperty);
            Assert.False(attribute.IsEnum);
            Assert.False(attribute.IsStringProperty);
            Assert.False(attribute.IsDelegateProperty()); // We treat RenderFragment as separate from generalized delegates
            Assert.True(attribute.IsChildContentProperty());
            Assert.True(attribute.IsParameterizedChildContentProperty());

            var childContent = Assert.Single(components, c => c.IsChildContentTagHelper());

            Assert.Equal("TestAssembly", childContent.AssemblyName);
            Assert.Equal("Test.MyComponent.ChildContent2", childContent.Name);

            // A RenderFragment<T> tag helper has a parameter to allow you to set the lambda parameter name.
            var contextAttribute = Assert.Single(childContent.BoundAttributes);
            Assert.Equal("Context", contextAttribute.Name);
            Assert.Equal("System.String", contextAttribute.TypeName);
            Assert.Equal("Specifies the parameter name for the 'ChildContent2' lambda expression.", contextAttribute.Documentation);
        }

        [Fact]
        public void Execute_MultipleRenderFragmentProperties_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        [Parameter]
        RenderFragment ChildContent { get; set; }

        [Parameter]
        RenderFragment<string> Header { get; set; }

        [Parameter]
        RenderFragment<string> Footer { get; set; }
    }
}

"));

            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            var provider = new ComponentTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            var components = ExcludeBuiltInComponents(context);
            var component = Assert.Single(components, c => c.IsComponentTagHelper());

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent", component.Name);

            Assert.Collection(
                component.BoundAttributes.OrderBy(a => a.Name),
                a =>
                {
                    Assert.Equal("ChildContent", a.Name);
                    Assert.Equal("Microsoft.AspNetCore.Blazor.RenderFragment", a.TypeName);
                    Assert.True(a.IsChildContentProperty());
                },
                a =>
                {
                    Assert.Equal("Footer", a.Name);
                    Assert.Equal("Microsoft.AspNetCore.Blazor.RenderFragment<System.String>", a.TypeName);
                    Assert.True(a.IsChildContentProperty());
                },
                a =>
                {
                    Assert.Equal("Header", a.Name);
                    Assert.Equal("Microsoft.AspNetCore.Blazor.RenderFragment<System.String>", a.TypeName);
                    Assert.True(a.IsChildContentProperty());
                });


            var childContents = components.Where(c => c.IsChildContentTagHelper()).OrderBy(c => c.Name);
            Assert.Collection(
                childContents,
                c => Assert.Equal("Test.MyComponent.ChildContent", c.Name),
                c => Assert.Equal("Test.MyComponent.Footer", c.Name),
                c => Assert.Equal("Test.MyComponent.Header", c.Name));
        }

        [Fact] // This component has lots of properties that don't become components.
        public void Execute_IgnoredProperties_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public abstract class MyBase : BlazorComponent
    {
        [Parameter]
        protected string Hidden { get; set; }
    }

    public class MyComponent : MyBase
    {
        [Parameter]
        string NoSetter { get; }

        [Parameter]
        static string StaticProperty { get; set; }

        public string NoParameterAttribute { get; set; }

        // No attribute here, hides base-class property of the same name.
        protected new int Hidden { get; set; }

        public string this[int i]
        {
            get { throw null; }
            set { throw null; }
        }
    }
}

"));

            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);

            var provider = new ComponentTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            var components = ExcludeBuiltInComponents(context);
            var component = Assert.Single(components);

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent", component.Name);

            Assert.Empty(component.BoundAttributes);
        }
    }
}
