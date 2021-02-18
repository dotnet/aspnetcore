// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor
{
    public class ComponentTagHelperDescriptorProviderTest : TagHelperDescriptorProviderTestBase
    {
        [Fact]
        public void Execute_FindsIComponentType_CreatesDescriptor()
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 1);
            var component = Assert.Single(components);

            // These are features Components don't use. Verifying them once here and
            // then ignoring them.
            Assert.Empty(component.AllowedChildTags);
            Assert.Null(component.TagOutputHint);

            // These are features that are invariants of all Components. Verifying them once
            // here and then ignoring them.
            Assert.Empty(component.Diagnostics);
            Assert.False(component.HasErrors);
            Assert.Equal(ComponentMetadata.Component.TagHelperKind, component.Kind);
            Assert.False(component.IsDefaultKind());
            Assert.False(component.KindUsesDefaultTagHelperRuntime());
            Assert.True(component.IsComponentOrChildContentTagHelper());
            Assert.True(component.CaseSensitive);

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
                kvp => { Assert.Equal(TagHelperMetadata.Runtime.Name, kvp.Key); Assert.Equal("Components.IComponent", kvp.Value); });

            // Our use of bound attributes is what tests will focus on. As you might expect right now, this test
            // is going to cover a lot of trivial stuff that will be true for all components/component-properties.
            var attribute = Assert.Single(component.BoundAttributes);

            // Invariants
            Assert.Empty(attribute.Diagnostics);
            Assert.False(attribute.HasErrors);
            Assert.Equal("Components.Component", attribute.Kind);
            Assert.False(attribute.IsDefaultKind());

            // Related to dictionaries/indexers, not supported currently, not sure if we ever will
            Assert.False(attribute.HasIndexer);
            Assert.Null(attribute.IndexerNamePrefix);
            Assert.Null(attribute.IndexerTypeName);
            Assert.False(attribute.IsIndexerBooleanProperty);
            Assert.False(attribute.IsIndexerStringProperty);
            Assert.True(component.CaseSensitive);

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
        public void Execute_FindsIComponentType_CreatesDescriptor_Generic()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<T> : IComponent
    {
        public void Attach(RenderHandle renderHandle) { }

        public Task SetParametersAsync(ParameterView parameters)
        {
            return Task.CompletedTask;
        }

        [Parameter]
        public string MyProperty { get; set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 1);
            var component = Assert.Single(components);

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent<T>", component.Name);
            Assert.Equal("Test.MyComponent<T>", component.DisplayName);
            Assert.Equal("Test.MyComponent<T>", component.GetTypeName());

            Assert.True(component.IsGenericTypedComponent());

            var rule = Assert.Single(component.TagMatchingRules);
            Assert.Equal("MyComponent", rule.TagName);

            Assert.Collection(
                component.BoundAttributes.OrderBy(a => a.Name),
                a =>
                {
                    Assert.Equal("MyProperty", a.Name);
                    Assert.Equal("MyProperty", a.GetPropertyName());
                    Assert.Equal("string Test.MyComponent<T>.MyProperty", a.DisplayName);
                    Assert.Equal("System.String", a.TypeName);

                },
                a =>
                {
                    Assert.Equal("T", a.Name);
                    Assert.Equal("T", a.GetPropertyName());
                    Assert.Equal("T", a.DisplayName);
                    Assert.Equal("System.Type", a.TypeName);
                    Assert.True(a.IsTypeParameterProperty());
                });
        }

        [Fact]
        public void Execute_FindsBlazorComponentType_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public string MyProperty { get; set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 1);
            var component = Assert.Single(components);

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent", component.Name);

            var attribute = Assert.Single(component.BoundAttributes);
            Assert.Equal("MyProperty", attribute.Name);
            Assert.Equal("System.String", attribute.TypeName);
        }

        [Fact]
        public void Execute_IgnoresPrivateParameters_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 1);
            var component = Assert.Single(components);

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent", component.Name);

            Assert.Empty(component.BoundAttributes);
        }

        [Fact]
        public void Execute_IgnoresParametersWithPrivateSetters_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public string MyProperty { get; private set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 1);
            var component = Assert.Single(components);

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent", component.Name);

            Assert.Empty(component.BoundAttributes);
        }

        [Fact] // bool properties support minimized attributes
        public void Execute_BoolProperty_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public bool MyProperty { get; set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 1);
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
        public void Execute_EnumProperty_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public enum MyEnum
    {
        One,
        Two
    }

    public class MyComponent : ComponentBase
    {
        [Parameter]
        public MyEnum MyProperty { get; set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 1);
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
        public void Execute_GenericProperty_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<T> : ComponentBase
    {
        [Parameter]
        public T MyProperty { get; set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 1);
            var component = Assert.Single(components);

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent<T>", component.Name);

            Assert.Collection(
                component.BoundAttributes.OrderBy(a => a.Name),
                a =>
                {
                    Assert.Equal("MyProperty", a.Name);
                    Assert.Equal("MyProperty", a.GetPropertyName());
                    Assert.Equal("T Test.MyComponent<T>.MyProperty", a.DisplayName);
                    Assert.Equal("T", a.TypeName);
                    Assert.True(a.IsGenericTypedProperty());

                },
                a =>
                {
                    Assert.Equal("T", a.Name);
                    Assert.Equal("T", a.GetPropertyName());
                    Assert.Equal("T", a.DisplayName);
                    Assert.Equal("System.Type", a.TypeName);
                    Assert.True(a.IsTypeParameterProperty());
                });
        }

        [Fact]
        public void Execute_MultipleGenerics_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<T, U, V> : ComponentBase
    {
        [Parameter]
        public T MyProperty1 { get; set; }

        [Parameter]
        public U MyProperty2 { get; set; }

        [Parameter]
        public V MyProperty3 { get; set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 1);
            var component = Assert.Single(components);

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent<T, U, V>", component.Name);

            Assert.Collection(
                component.BoundAttributes.OrderBy(a => a.Name),
                a =>
                {
                    Assert.Equal("MyProperty1", a.Name);
                    Assert.Equal("T", a.TypeName);
                    Assert.True(a.IsGenericTypedProperty());
                },
                a =>
                {
                    Assert.Equal("MyProperty2", a.Name);
                    Assert.Equal("U", a.TypeName);
                    Assert.True(a.IsGenericTypedProperty());
                },
                a =>
                {
                    Assert.Equal("MyProperty3", a.Name);
                    Assert.Equal("V", a.TypeName);
                    Assert.True(a.IsGenericTypedProperty());
                },
                a =>
                {
                    Assert.Equal("T", a.Name);
                    Assert.True(a.IsTypeParameterProperty());
                },
                a =>
                {
                    Assert.Equal("U", a.Name);
                    Assert.True(a.IsTypeParameterProperty());
                },
                a =>
                {
                    Assert.Equal("V", a.Name);
                    Assert.True(a.IsTypeParameterProperty());
                });
        }

        [Fact]
        public void Execute_DelegateProperty_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public Action<EventArgs> OnClick { get; set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 1);
            var component = Assert.Single(components);

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent", component.Name);

            var attribute = Assert.Single(component.BoundAttributes);
            Assert.Equal("OnClick", attribute.Name);
            Assert.Equal("System.Action<System.EventArgs>", attribute.TypeName);

            Assert.False(attribute.HasIndexer);
            Assert.False(attribute.IsBooleanProperty);
            Assert.False(attribute.IsEnum);
            Assert.False(attribute.IsStringProperty);
            Assert.True(attribute.IsDelegateProperty());
            Assert.False(attribute.IsChildContentProperty());
        }

        [Fact]
        public void Execute_DelegateProperty_CreatesDescriptor_Generic()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<T> : ComponentBase
    {
        [Parameter]
        public Action<T> OnClick { get; set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 1);
            var component = Assert.Single(components);

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent<T>", component.Name);

            Assert.Collection(
                component.BoundAttributes.OrderBy(a => a.Name),
                a =>
                {
                    Assert.Equal("OnClick", a.Name);
                    Assert.Equal("System.Action<T>", a.TypeName);
                    Assert.False(a.HasIndexer);
                    Assert.False(a.IsBooleanProperty);
                    Assert.False(a.IsEnum);
                    Assert.False(a.IsStringProperty);
                    Assert.True(a.IsDelegateProperty());
                    Assert.False(a.IsChildContentProperty());
                    Assert.True(a.IsGenericTypedProperty());

                },
                a =>
                {
                    Assert.Equal("T", a.Name);
                    Assert.Equal("T", a.GetPropertyName());
                    Assert.Equal("T", a.DisplayName);
                    Assert.Equal("System.Type", a.TypeName);
                    Assert.True(a.IsTypeParameterProperty());
                });
        }

        [Fact]
        public void Execute_EventCallbackProperty_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public EventCallback OnClick { get; set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 1);
            var component = Assert.Single(components);

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent", component.Name);

            var attribute = Assert.Single(component.BoundAttributes);
            Assert.Equal("OnClick", attribute.Name);
            Assert.Equal("Microsoft.AspNetCore.Components.EventCallback", attribute.TypeName);

            Assert.False(attribute.HasIndexer);
            Assert.False(attribute.IsBooleanProperty);
            Assert.False(attribute.IsEnum);
            Assert.False(attribute.IsStringProperty);
            Assert.True(attribute.IsEventCallbackProperty());
            Assert.False(attribute.IsDelegateProperty());
            Assert.False(attribute.IsChildContentProperty());
        }

        [Fact]
        public void Execute_EventCallbackProperty_CreatesDescriptor_ClosedGeneric()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public EventCallback<EventArgs> OnClick { get; set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 1);
            var component = Assert.Single(components);

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent", component.Name);

            Assert.Collection(
                component.BoundAttributes.OrderBy(a => a.Name),
                a =>
                {
                    Assert.Equal("OnClick", a.Name);
                    Assert.Equal("Microsoft.AspNetCore.Components.EventCallback<System.EventArgs>", a.TypeName);
                    Assert.False(a.HasIndexer);
                    Assert.False(a.IsBooleanProperty);
                    Assert.False(a.IsEnum);
                    Assert.False(a.IsStringProperty);
                    Assert.True(a.IsEventCallbackProperty());
                    Assert.False(a.IsDelegateProperty());
                    Assert.False(a.IsChildContentProperty());
                    Assert.False(a.IsGenericTypedProperty());

                });
        }

        [Fact]
        public void Execute_EventCallbackProperty_CreatesDescriptor_OpenGeneric()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<T> : ComponentBase
    {
        [Parameter]
        public EventCallback<T> OnClick { get; set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 1);
            var component = Assert.Single(components);

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent<T>", component.Name);

            Assert.Collection(
                component.BoundAttributes.OrderBy(a => a.Name),
                a =>
                {
                    Assert.Equal("OnClick", a.Name);
                    Assert.Equal("Microsoft.AspNetCore.Components.EventCallback<T>", a.TypeName);
                    Assert.False(a.HasIndexer);
                    Assert.False(a.IsBooleanProperty);
                    Assert.False(a.IsEnum);
                    Assert.False(a.IsStringProperty);
                    Assert.True(a.IsEventCallbackProperty());
                    Assert.False(a.IsDelegateProperty());
                    Assert.False(a.IsChildContentProperty());
                    Assert.True(a.IsGenericTypedProperty());

                },
                a =>
                {
                    Assert.Equal("T", a.Name);
                    Assert.Equal("T", a.GetPropertyName());
                    Assert.Equal("T", a.DisplayName);
                    Assert.Equal("System.Type", a.TypeName);
                    Assert.True(a.IsTypeParameterProperty());
                });
        }

        [Fact]
        public void Execute_RenderFragmentProperty_CreatesDescriptors()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent2 { get; set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 2);
            var component = Assert.Single(components, c => c.IsComponentTagHelper());

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent", component.Name);

            var attribute = Assert.Single(component.BoundAttributes);
            Assert.Equal("ChildContent2", attribute.Name);
            Assert.Equal("Microsoft.AspNetCore.Components.RenderFragment", attribute.TypeName);

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
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public RenderFragment<string> ChildContent2 { get; set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 2);
            var component = Assert.Single(components, c => c.IsComponentTagHelper());

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent", component.Name);

            Assert.Collection(
                component.BoundAttributes,
                a =>
                {
                    Assert.Equal("ChildContent2", a.Name);
                    Assert.Equal("Microsoft.AspNetCore.Components.RenderFragment<System.String>", a.TypeName);

                    Assert.False(a.HasIndexer);
                    Assert.False(a.IsBooleanProperty);
                    Assert.False(a.IsEnum);
                    Assert.False(a.IsStringProperty);
                    Assert.False(a.IsDelegateProperty()); // We treat RenderFragment as separate from generalized delegates
                    Assert.True(a.IsChildContentProperty());
                    Assert.True(a.IsParameterizedChildContentProperty());
                    Assert.False(a.IsGenericTypedProperty());
                },
                a =>
                {
                    Assert.Equal(ComponentMetadata.ChildContent.ParameterAttributeName, a.Name);
                    Assert.True(a.IsChildContentParameterNameProperty());
                });

            var childContent = Assert.Single(components, c => c.IsChildContentTagHelper());

            Assert.Equal("TestAssembly", childContent.AssemblyName);
            Assert.Equal("Test.MyComponent.ChildContent2", childContent.Name);

            // A RenderFragment<T> tag helper has a parameter to allow you to set the lambda parameter name.
            var contextAttribute = Assert.Single(childContent.BoundAttributes);
            Assert.Equal(ComponentMetadata.ChildContent.ParameterAttributeName, contextAttribute.Name);
            Assert.Equal("System.String", contextAttribute.TypeName);
            Assert.Equal("Specifies the parameter name for the 'ChildContent2' child content expression.", contextAttribute.Documentation);
            Assert.True(contextAttribute.IsChildContentParameterNameProperty());
        }

        [Fact]
        public void Execute_RenderFragmentOfTProperty_ComponentDefinesContextParameter()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public RenderFragment<string> ChildContent2 { get; set; }

        [Parameter]
        public string Context { get; set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 2);
            var component = Assert.Single(components, c => c.IsComponentTagHelper());

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent", component.Name);

            Assert.Collection(
                component.BoundAttributes,
                a =>
                {
                    Assert.Equal("ChildContent2", a.Name);
                    Assert.Equal("Microsoft.AspNetCore.Components.RenderFragment<System.String>", a.TypeName);

                    Assert.False(a.HasIndexer);
                    Assert.False(a.IsBooleanProperty);
                    Assert.False(a.IsEnum);
                    Assert.False(a.IsStringProperty);
                    Assert.False(a.IsDelegateProperty()); // We treat RenderFragment as separate from generalized delegates
                    Assert.True(a.IsChildContentProperty());
                    Assert.True(a.IsParameterizedChildContentProperty());
                    Assert.False(a.IsGenericTypedProperty());
                },
                a =>
                {
                    Assert.Equal(ComponentMetadata.ChildContent.ParameterAttributeName, a.Name);
                    Assert.False(a.IsChildContentParameterNameProperty());
                });

            var childContent = Assert.Single(components, c => c.IsChildContentTagHelper());

            Assert.Equal("TestAssembly", childContent.AssemblyName);
            Assert.Equal("Test.MyComponent.ChildContent2", childContent.Name);

            // A RenderFragment<T> tag helper has a parameter to allow you to set the lambda parameter name.
            var contextAttribute = Assert.Single(childContent.BoundAttributes);
            Assert.Equal(ComponentMetadata.ChildContent.ParameterAttributeName, contextAttribute.Name);
            Assert.Equal("System.String", contextAttribute.TypeName);
            Assert.Equal("Specifies the parameter name for the 'ChildContent2' child content expression.", contextAttribute.Documentation);
            Assert.True(contextAttribute.IsChildContentParameterNameProperty());
        }

        [Fact]
        public void Execute_RenderFragmentGenericProperty_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<T> : ComponentBase
    {
        [Parameter]
        public RenderFragment<T> ChildContent2 { get; set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 2);
            var component = Assert.Single(components, c => c.IsComponentTagHelper());

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent<T>", component.Name);

            Assert.Collection(
                component.BoundAttributes.OrderBy(a => a.Name),
                a =>
                {
                    Assert.Equal("ChildContent2", a.Name);
                    Assert.Equal("Microsoft.AspNetCore.Components.RenderFragment<T>", a.TypeName);

                    Assert.False(a.HasIndexer);
                    Assert.False(a.IsBooleanProperty);
                    Assert.False(a.IsEnum);
                    Assert.False(a.IsStringProperty);
                    Assert.False(a.IsDelegateProperty()); // We treat RenderFragment as separate from generalized delegates
                    Assert.True(a.IsChildContentProperty());
                    Assert.True(a.IsParameterizedChildContentProperty());
                    Assert.True(a.IsGenericTypedProperty());

                },
                a =>
                {
                    Assert.Equal(ComponentMetadata.ChildContent.ParameterAttributeName, a.Name);
                    Assert.True(a.IsChildContentParameterNameProperty());
                },
                a =>
                {
                    Assert.Equal("T", a.Name);
                    Assert.Equal("T", a.GetPropertyName());
                    Assert.Equal("T", a.DisplayName);
                    Assert.Equal("System.Type", a.TypeName);
                    Assert.True(a.IsTypeParameterProperty());
                });

            var childContent = Assert.Single(components, c => c.IsChildContentTagHelper());

            Assert.Equal("TestAssembly", childContent.AssemblyName);
            Assert.Equal("Test.MyComponent<T>.ChildContent2", childContent.Name);

            // A RenderFragment<T> tag helper has a parameter to allow you to set the lambda parameter name.
            var contextAttribute = Assert.Single(childContent.BoundAttributes);
            Assert.Equal(ComponentMetadata.ChildContent.ParameterAttributeName, contextAttribute.Name);
            Assert.Equal("System.String", contextAttribute.TypeName);
            Assert.Equal("Specifies the parameter name for the 'ChildContent2' child content expression.", contextAttribute.Documentation);
            Assert.True(contextAttribute.IsChildContentParameterNameProperty());
        }

        [Fact]
        public void Execute_RenderFragmentClosedGenericListProperty_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<T> : ComponentBase
    {
        [Parameter]
        public RenderFragment<List<string>> ChildContent2 { get; set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 2);
            var component = Assert.Single(components, c => c.IsComponentTagHelper());

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent<T>", component.Name);

            Assert.Collection(
                component.BoundAttributes.OrderBy(a => a.Name),
                a =>
                {
                    Assert.Equal("ChildContent2", a.Name);
                    Assert.Equal("Microsoft.AspNetCore.Components.RenderFragment<System.Collections.Generic.List<System.String>>", a.TypeName);

                    Assert.False(a.HasIndexer);
                    Assert.False(a.IsBooleanProperty);
                    Assert.False(a.IsEnum);
                    Assert.False(a.IsStringProperty);
                    Assert.False(a.IsDelegateProperty()); // We treat RenderFragment as separate from generalized delegates
                    Assert.True(a.IsChildContentProperty());
                    Assert.True(a.IsParameterizedChildContentProperty());
                    Assert.False(a.IsGenericTypedProperty());

                },
                a =>
                {
                    Assert.Equal(ComponentMetadata.ChildContent.ParameterAttributeName, a.Name);
                    Assert.True(a.IsChildContentParameterNameProperty());
                },
                a =>
                {
                    Assert.Equal("T", a.Name);
                    Assert.Equal("T", a.GetPropertyName());
                    Assert.Equal("T", a.DisplayName);
                    Assert.Equal("System.Type", a.TypeName);
                    Assert.True(a.IsTypeParameterProperty());
                });

            var childContent = Assert.Single(components, c => c.IsChildContentTagHelper());

            Assert.Equal("TestAssembly", childContent.AssemblyName);
            Assert.Equal("Test.MyComponent<T>.ChildContent2", childContent.Name);

            // A RenderFragment<T> tag helper has a parameter to allow you to set the lambda parameter name.
            var contextAttribute = Assert.Single(childContent.BoundAttributes);
            Assert.Equal(ComponentMetadata.ChildContent.ParameterAttributeName, contextAttribute.Name);
            Assert.Equal("System.String", contextAttribute.TypeName);
            Assert.Equal("Specifies the parameter name for the 'ChildContent2' child content expression.", contextAttribute.Documentation);
            Assert.True(contextAttribute.IsChildContentParameterNameProperty());
        }

        [Fact]
        public void Execute_RenderFragmentGenericListProperty_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<T> : ComponentBase
    {
        [Parameter]
        public RenderFragment<List<T>> ChildContent2 { get; set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 2);
            var component = Assert.Single(components, c => c.IsComponentTagHelper());

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent<T>", component.Name);

            Assert.Collection(
                component.BoundAttributes.OrderBy(a => a.Name),
                a =>
                {
                    Assert.Equal("ChildContent2", a.Name);
                    Assert.Equal("Microsoft.AspNetCore.Components.RenderFragment<System.Collections.Generic.List<T>>", a.TypeName);

                    Assert.False(a.HasIndexer);
                    Assert.False(a.IsBooleanProperty);
                    Assert.False(a.IsEnum);
                    Assert.False(a.IsStringProperty);
                    Assert.False(a.IsDelegateProperty()); // We treat RenderFragment as separate from generalized delegates
                    Assert.True(a.IsChildContentProperty());
                    Assert.True(a.IsParameterizedChildContentProperty());
                    Assert.True(a.IsGenericTypedProperty());

                },
                a =>
                {
                    Assert.Equal(ComponentMetadata.ChildContent.ParameterAttributeName, a.Name);
                    Assert.True(a.IsChildContentParameterNameProperty());
                },
                a =>
                {
                    Assert.Equal("T", a.Name);
                    Assert.Equal("T", a.GetPropertyName());
                    Assert.Equal("T", a.DisplayName);
                    Assert.Equal("System.Type", a.TypeName);
                    Assert.True(a.IsTypeParameterProperty());
                });
            
            var childContent = Assert.Single(components, c => c.IsChildContentTagHelper());

            Assert.Equal("TestAssembly", childContent.AssemblyName);
            Assert.Equal("Test.MyComponent<T>.ChildContent2", childContent.Name);

            // A RenderFragment<T> tag helper has a parameter to allow you to set the lambda parameter name.
            var contextAttribute = Assert.Single(childContent.BoundAttributes);
            Assert.Equal(ComponentMetadata.ChildContent.ParameterAttributeName, contextAttribute.Name);
            Assert.Equal("System.String", contextAttribute.TypeName);
            Assert.Equal("Specifies the parameter name for the 'ChildContent2' child content expression.", contextAttribute.Documentation);
            Assert.True(contextAttribute.IsChildContentParameterNameProperty());
        }

        [Fact]
        public void Execute_RenderFragmentGenericContextProperty_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<T> : ComponentBase
    {
        [Parameter]
        public RenderFragment<Context> ChildContent2 { get; set; }

        public class Context
        {
            public T Item { get; set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 2);
            var component = Assert.Single(components, c => c.IsComponentTagHelper());

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent<T>", component.Name);

            Assert.Collection(
                component.BoundAttributes.OrderBy(a => a.Name),
                a =>
                {
                    Assert.Equal("ChildContent2", a.Name);
                    Assert.Equal("Microsoft.AspNetCore.Components.RenderFragment<Test.MyComponent<T>.Context>", a.TypeName);

                    Assert.False(a.HasIndexer);
                    Assert.False(a.IsBooleanProperty);
                    Assert.False(a.IsEnum);
                    Assert.False(a.IsStringProperty);
                    Assert.False(a.IsDelegateProperty()); // We treat RenderFragment as separate from generalized delegates
                    Assert.True(a.IsChildContentProperty());
                    Assert.True(a.IsParameterizedChildContentProperty());
                    Assert.True(a.IsGenericTypedProperty());

                },
                a =>
                {
                    Assert.Equal(ComponentMetadata.ChildContent.ParameterAttributeName, a.Name);
                    Assert.True(a.IsChildContentParameterNameProperty());
                },
                a =>
                {
                    Assert.Equal("T", a.Name);
                    Assert.Equal("T", a.GetPropertyName());
                    Assert.Equal("T", a.DisplayName);
                    Assert.Equal("System.Type", a.TypeName);
                    Assert.True(a.IsTypeParameterProperty());
                });

            var childContent = Assert.Single(components, c => c.IsChildContentTagHelper());

            Assert.Equal("TestAssembly", childContent.AssemblyName);
            Assert.Equal("Test.MyComponent<T>.ChildContent2", childContent.Name);

            // A RenderFragment<T> tag helper has a parameter to allow you to set the lambda parameter name.
            var contextAttribute = Assert.Single(childContent.BoundAttributes);
            Assert.Equal(ComponentMetadata.ChildContent.ParameterAttributeName, contextAttribute.Name);
            Assert.Equal("System.String", contextAttribute.TypeName);
            Assert.Equal("Specifies the parameter name for the 'ChildContent2' child content expression.", contextAttribute.Documentation);
        }

        [Fact]
        public void Execute_MultipleRenderFragmentProperties_CreatesDescriptor()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public RenderFragment<string> Header { get; set; }

        [Parameter]
        public RenderFragment<string> Footer { get; set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 4);
            var component = Assert.Single(components, c => c.IsComponentTagHelper());

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent", component.Name);

            Assert.Collection(
                component.BoundAttributes.OrderBy(a => a.Name),
                a =>
                {
                    Assert.Equal("ChildContent", a.Name);
                    Assert.Equal("Microsoft.AspNetCore.Components.RenderFragment", a.TypeName);
                    Assert.True(a.IsChildContentProperty());
                },
                a =>
                {
                    Assert.Equal(ComponentMetadata.ChildContent.ParameterAttributeName, a.Name);
                    Assert.True(a.IsChildContentParameterNameProperty());
                },
                a =>
                {
                    Assert.Equal("Footer", a.Name);
                    Assert.Equal("Microsoft.AspNetCore.Components.RenderFragment<System.String>", a.TypeName);
                    Assert.True(a.IsChildContentProperty());
                },
                a =>
                {
                    Assert.Equal("Header", a.Name);
                    Assert.Equal("Microsoft.AspNetCore.Components.RenderFragment<System.String>", a.TypeName);
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
using Microsoft.AspNetCore.Components;

namespace Test
{
    public abstract class MyBase : ComponentBase
    {
        [Parameter]
        public string Hidden { get; set; }
    }

    public class MyComponent : MyBase
    {
        [Parameter]
        public string NoSetter { get; }

        [Parameter]
        public static string StaticProperty { get; set; }

        public string NoParameterAttribute { get; set; }

        // No attribute here, hides base-class property of the same name.
        public new int Hidden { get; set; }

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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 1);
            var component = Assert.Single(components);

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyComponent", component.Name);

            Assert.Empty(component.BoundAttributes);
        }

        [Fact] // Testing multilevel overrides with the [Parameter] attribute on different levels.
        public void Execute_MultiLevelOverriddenProperties_CreatesDescriptorCorrectly()
        {
            // Arrange

            var compilation = BaseCompilation.AddSyntaxTrees(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public abstract class MyBaseComponent : ComponentBase
    {
        [Parameter]
        public virtual string Header { get; set; }

        public virtual string Footer { get; set; }
    }

    public abstract class MyDerivedComponent1 : MyBaseComponent
    {
        public override string Header { get; set; }

        [Parameter]
        public override string Footer { get; set; }
    }

    public class MyDerivedComponent2 : MyDerivedComponent1
    {
        public override string Header { get; set; }

        public override string Footer { get; set; }
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
            components = AssertAndExcludeFullyQualifiedNameMatchComponents(components, expectedCount: 1);
            var component = Assert.Single(components, c => c.IsComponentTagHelper());

            Assert.Equal("TestAssembly", component.AssemblyName);
            Assert.Equal("Test.MyDerivedComponent2", component.Name);

            Assert.Collection(
                component.BoundAttributes.OrderBy(a => a.Name),
                a =>
                {
                    Assert.Equal("Footer", a.Name);
                    Assert.Equal("System.String", a.TypeName);
                },
                a =>
                {
                    Assert.Equal("Header", a.Name);
                    Assert.Equal("System.String", a.TypeName);
                });
        }

        [Fact]
        public void Execute_WithFilterAssemblyDoesNotDiscoverTagHelpersFromReferences()
        {
            // Arrange
            var testComponent = "Test.MyComponent";
            var routerComponent = "Microsoft.AspNetCore.Components.Routing.Router";
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
    }
}

"));

            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);
            context.Items.SetTagHelperDiscoveryFilter(TagHelperDiscoveryFilter.CurrentCompilation);
            var provider = new ComponentTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            Assert.NotNull(compilation.GetTypeByMetadataName(testComponent));
            Assert.NotEmpty(context.Results.Where(f => f.GetTypeName() == testComponent));
            Assert.Empty(context.Results.Where(f => f.GetTypeName() == routerComponent));
        }

        [Fact]
        public void Execute_WithFilterReferenceDoesNotDiscoverTagHelpersFromAssembly()
        {
            // Arrange
            var testComponent = "Test.MyComponent";
            var routerComponent = "Microsoft.AspNetCore.Components.Routing.Router";
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
    }
}

"));

            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);
            context.Items.SetTagHelperDiscoveryFilter(TagHelperDiscoveryFilter.ReferenceAssemblies);
            var provider = new ComponentTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            Assert.NotNull(compilation.GetTypeByMetadataName(testComponent));
            Assert.NotEmpty(context.Results);
            Assert.Empty(context.Results.Where(f => f.GetTypeName() == testComponent));
            Assert.NotEmpty(context.Results.Where(f => f.GetTypeName() == routerComponent));
        }

        [Fact]
        public void Execute_WithDefaultDiscoversTagHelpersFromAssemblyAndReference()
        {
            // Arrange
            var testComponent = "Test.MyComponent";
            var routerComponent
                = "Microsoft.AspNetCore.Components.Routing.Router";
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
    }
}

"));

            Assert.Empty(compilation.GetDiagnostics());

            var context = TagHelperDescriptorProviderContext.Create();
            context.SetCompilation(compilation);
            context.Items.SetTagHelperDiscoveryFilter(TagHelperDiscoveryFilter.Default);
            var provider = new ComponentTagHelperDescriptorProvider();

            // Act
            provider.Execute(context);

            // Assert
            Assert.NotNull(compilation.GetTypeByMetadataName(testComponent));
            Assert.NotEmpty(context.Results);
            Assert.NotEmpty(context.Results.Where(f => f.GetTypeName() == testComponent));
            Assert.NotEmpty(context.Results.Where(f => f.GetTypeName() == routerComponent));
        }
    }
}
