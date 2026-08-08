// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators.Tests;

#nullable enable

public class RazorComponentsMetadataGeneratorComponentTests : RazorComponentsMetadataGeneratorTestBase
{
    [Fact]
    public void ExplicitClosedGenericComponent_EmitsDescriptor()
    {
        var result = RunGenerator(
            "namespace TestComponents;",
            """
            namespace TestHost;

            [Microsoft.AspNetCore.Components.Web.ComponentTypeInfo(
                typeof(Microsoft.AspNetCore.Components.CascadingValue<string>))]
            public sealed partial class TestMetadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
            {
            }
            """);

        var source = GetGeneratedSource(result);
        Assert.Contains("typeof(global::Microsoft.AspNetCore.Components.CascadingValue<string>)", source);
    }

    [Fact]
    public void ExplicitKnownGenericComponent_EmitsBuiltInFactoryCall()
    {
        var result = RunGenerator(
            "namespace TestComponents;",
            """
            namespace TestHost;

            [Microsoft.AspNetCore.Components.Web.ComponentTypeInfo(
                typeof(Microsoft.AspNetCore.Components.Forms.ValidationMessage<string>))]
            public sealed partial class TestMetadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
            {
            }
            """);

        var source = GetGeneratedSource(result);
        Assert.Matches(@"GetBuiltInComponentDescriptorFactory_\d+<string>\(null\)", source);
        Assert.Contains("Name = \"CreateValidationMessageDescriptors\"", source);
        Assert.Contains(
            "UnsafeAccessorType(\"Microsoft.AspNetCore.Components.Infrastructure.BuiltInComponentDescriptors, Microsoft.AspNetCore.Components.Web\")",
            source);
    }

    [Fact]
    public void ReferencedFrameworkComponents_IncludeFullyDescribableComponents()
    {
        var result = RunGenerator("namespace TestComponents;");

        var source = GetGeneratedSource(result);
        Assert.Contains("typeof(global::Microsoft.AspNetCore.Components.Web.HeadOutlet)", source);
        Assert.Contains("typeof(global::Microsoft.AspNetCore.Components.Sections.SectionOutlet)", source);
        Assert.Contains("typeof(global::Microsoft.AspNetCore.Components.ResourcePreloader)", source);
        Assert.Contains("UnsafeAccessorType(\"Microsoft.AspNetCore.Components.Infrastructure.BuiltInComponentDescriptors, Microsoft.AspNetCore.Components\")", source);
    }

    [Theory]
    [InlineData("Microsoft.AspNetCore.Components.Endpoints")]
    [InlineData("Microsoft.AspNetCore.Components.Forms")]
    public void ReferencedKnownFrameworkAssembly_EmitsProviderAccessor(string assemblyName)
    {
        var result = RunGenerator(
            "namespace TestComponents;",
            referencedAssemblyName: assemblyName);

        var source = GetGeneratedSource(result);
        Assert.Contains(
            $"UnsafeAccessorType(\"Microsoft.AspNetCore.Components.Infrastructure.BuiltInComponentDescriptors, {assemblyName}\")",
            source);
    }

    [Fact]
    public void DynamicallyAccessedMembersParameter_EmitsSuppressedGeneratedBridge()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public sealed class DynamicComponent : Microsoft.AspNetCore.Components.ComponentBase
            {
                [Microsoft.AspNetCore.Components.Parameter]
                [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
                    System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
                public System.Type Type { get; set; } = default!;
            }
            """);

        var source = GetGeneratedSource(result);
        Assert.Contains("UnconditionalSuppressMessage(\"Trimming\", \"IL2067\"", source);
        Assert.Contains("UnconditionalSuppressMessage(\"Trimming\", \"IL2111\"", source);
        Assert.Contains("target.Type = value;", source);
    }

    [Fact]
    public void PublicComponent_EmitsWorkingActivationAndParameterDescriptor()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public sealed class Greeting : Microsoft.AspNetCore.Components.ComponentBase
            {
                [Microsoft.AspNetCore.Components.Parameter]
                public string? Message { get; set; }
            }
            """);

        var source = GetGeneratedSource(result);
        Assert.Contains("typeof(global::TestComponents.Greeting)", source);
        Assert.Contains("CreateInstance = static __services => new global::TestComponents.Greeting()", source);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var component = Assert.Single(GetReferencedComponents(context, result));
            Assert.Equal("TestComponents.Greeting", component.Type.FullName);
            var instance = component.CreateInstance!(new EmptyServiceProvider());
            Assert.Equal(component.Type, instance.GetType());

            var parameter = Assert.Single(component.Parameters);
            Assert.Equal("Message", parameter.Name);
            Assert.Equal(typeof(string), parameter.ParameterType);
            Assert.IsType<ParameterAttribute>(parameter.Attribute);
            parameter.SetValue(instance, "Hello");
            Assert.Equal("Hello", parameter.GetValue(instance));
        }
    }

    [Fact]
    public void ComponentWithoutPublicParameterlessConstructor_RemainsDiscoverableWithoutFactory()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public sealed class NeedsArgument : Microsoft.AspNetCore.Components.ComponentBase
            {
                public NeedsArgument(string value) { }
            }
            """);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(GetReferencedComponents(context, result));
            Assert.Equal("TestComponents.NeedsArgument", descriptor.Type.FullName);
            Assert.Null(descriptor.CreateInstance);
        }
    }

    [Fact]
    public void InheritedAndHiddenParameters_UseMostDerivedMemberOnce()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public class BaseComponent : Microsoft.AspNetCore.Components.ComponentBase
            {
                [Microsoft.AspNetCore.Components.Parameter]
                public string? Shared { get; set; }

                [Microsoft.AspNetCore.Components.Parameter]
                public int Inherited { get; set; }
            }

            public sealed class DerivedComponent : BaseComponent
            {
                [Microsoft.AspNetCore.Components.Parameter]
                public new int Shared { get; set; }
            }
            """);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(context.Components, item => item.Type.Name == "DerivedComponent");
            Assert.Equal(2, descriptor.Parameters.Count);
            Assert.Equal(typeof(int), Assert.Single(descriptor.Parameters, item => item.Name == "Shared").ParameterType);
            Assert.Equal(typeof(int), Assert.Single(descriptor.Parameters, item => item.Name == "Inherited").ParameterType);
        }
    }

    [Fact]
    public void InjectedProperties_ReconstructKeyAndUsePublicAndPrivateSetters()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public sealed class InjectedComponent : Microsoft.AspNetCore.Components.ComponentBase
            {
                [Microsoft.AspNetCore.Components.Inject]
                public string? PublicService { get; set; }

                [Microsoft.AspNetCore.Components.Inject(Key = "primary")]
                private object? KeyedService { get; set; }

                public object? ReadKeyedService() => KeyedService;
            }

            """);

        Assert.Equal(MetadataImportOptions.Public, result.InputCompilation.Options.MetadataImportOptions);
        var source = GetGeneratedSource(result);
        Assert.Contains("UnsafeAccessorKind.Method, Name = \"set_KeyedService\"", source);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(GetReferencedComponents(context, result));
            var instance = descriptor.CreateInstance!(new EmptyServiceProvider());
            var publicInjection = Assert.Single(descriptor.Injectables, item => item.Name == "PublicService");
            var keyedInjection = Assert.Single(descriptor.Injectables, item => item.Name == "KeyedService");
            Assert.Null(publicInjection.Attribute.Key);
            Assert.Equal("primary", keyedInjection.Attribute.Key);

            publicInjection.SetValue(instance, "service");
            keyedInjection.SetValue(instance, 42);
            Assert.Equal("service", descriptor.Type.GetProperty("PublicService")!.GetValue(instance));
            Assert.Equal(42, descriptor.Type.GetMethod("ReadKeyedService")!.Invoke(instance, null));
        }
    }

    [Fact]
    public void InheritedPrivateSetters_UseAccessorsDeclaredOnBaseType()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public abstract class BaseComponent : Microsoft.AspNetCore.Components.ComponentBase
            {
                [Microsoft.AspNetCore.Components.Parameter]
                public string? InheritedParameter { get; private set; }

                [Microsoft.AspNetCore.Components.Inject]
                private object? InheritedService { get; set; }

                public object? ReadInheritedService() => InheritedService;
            }

            public sealed class DerivedComponent : BaseComponent
            {
            }
            """);

        var source = GetGeneratedSource(result);
        Assert.Contains("global::TestComponents.BaseComponent target", source);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(context.Components, item => item.Type.Name == "DerivedComponent");
            var instance = descriptor.CreateInstance!(new EmptyServiceProvider());
            var parameter = Assert.Single(descriptor.Parameters);
            var injectable = Assert.Single(descriptor.Injectables);

            parameter.SetValue(instance, "inherited");
            injectable.SetValue(instance, "service");

            Assert.Equal("inherited", parameter.GetValue(instance));
            Assert.Equal("service", descriptor.Type.GetMethod("ReadInheritedService")!.Invoke(instance, null));
        }
    }

    [Fact]
    public void CascadingQueryAndPrivateParameters_RetainDerivedAttributesAndWorkingAccessors()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public sealed class ParameterComponent : Microsoft.AspNetCore.Components.ComponentBase
            {
                [Microsoft.AspNetCore.Components.CascadingParameter(Name = "theme")]
                public string? Theme { get; set; }

                [Microsoft.AspNetCore.Components.Parameter]
                [Microsoft.AspNetCore.Components.SupplyParameterFromQuery(Name = "page")]
                public int Page { get; set; }

                [Microsoft.AspNetCore.Components.Parameter]
                private string? Secret { get; set; }

                public string? ReadSecret() => Secret;
            }
            """);

        var source = GetGeneratedSource(result);
        Assert.Contains("Name = \"get_Secret\"", source);
        Assert.Contains("Name = \"set_Secret\"", source);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(GetReferencedComponents(context, result));
            Assert.Equal(3, descriptor.Parameters.Count);
            var theme = Assert.Single(descriptor.Parameters, item => item.Name == "Theme");
            var themeAttribute = Assert.IsType<CascadingParameterAttribute>(theme.Attribute);
            Assert.Equal("theme", themeAttribute.Name);

            var page = Assert.Single(descriptor.Parameters, item => item.Name == "Page");
            var queryAttribute = Assert.IsType<SupplyParameterFromQueryAttribute>(page.Attribute);
            Assert.Equal("page", queryAttribute.Name);

            var instance = descriptor.CreateInstance!(new EmptyServiceProvider());
            var secret = Assert.Single(descriptor.Parameters, item => item.Name == "Secret");
            secret.SetValue(instance, "hidden");
            Assert.Equal("hidden", secret.GetValue(instance));
            Assert.Equal("hidden", descriptor.Type.GetMethod("ReadSecret")!.Invoke(instance, null));
        }
    }

    [Fact]
    public void RoutingMetadata_ReconstructsRouteLayoutRenderModeAndExclusion()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public sealed class Layout : Microsoft.AspNetCore.Components.ComponentBase
            {
            }

            public sealed class TestMode : Microsoft.AspNetCore.Components.IComponentRenderMode
            {
            }

            public sealed class TestModeAttribute : Microsoft.AspNetCore.Components.RenderModeAttribute
            {
                public TestModeAttribute(string name) => Name = name;
                public string Name { get; }
                public int Order { get; set; }
                public override Microsoft.AspNetCore.Components.IComponentRenderMode Mode => new TestMode();
            }

            [Microsoft.AspNetCore.Components.Route("/dashboard/{id:int}")]
            [Microsoft.AspNetCore.Components.Layout(typeof(Layout))]
            [TestMode("server", Order = 7)]
            [Microsoft.AspNetCore.Components.ExcludeFromInteractiveRouting]
            public sealed class RoutedComponent : Microsoft.AspNetCore.Components.ComponentBase
            {
            }
            """);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(context.Components, item => item.Type.Name == "RoutedComponent");
            Assert.Equal(4, descriptor.Metadata.Count);
            Assert.Equal("/dashboard/{id:int}", Assert.Single(descriptor.Metadata.OfType<RouteAttribute>()).Template);
            Assert.Equal("TestComponents.Layout", Assert.Single(descriptor.Metadata.OfType<LayoutAttribute>()).LayoutType.FullName);
            Assert.Single(descriptor.Metadata.OfType<ExcludeFromInteractiveRoutingAttribute>());

            var renderModeAttribute = Assert.Single(descriptor.Metadata.OfType<RenderModeAttribute>());
            Assert.Equal("TestModeAttribute", renderModeAttribute.GetType().Name);
            Assert.Equal("server", renderModeAttribute.GetType().GetProperty("Name")!.GetValue(renderModeAttribute));
            Assert.Equal(7, renderModeAttribute.GetType().GetProperty("Order")!.GetValue(renderModeAttribute));
        }
    }

    [Fact]
    public void EndpointMetadata_ReconstructsArbitraryInheritedSecurityAndCacheAttributes()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
            public sealed class SecurityPolicyAttribute(string policy) : System.Attribute
            {
                public string Policy { get; } = policy;
            }

            [System.AttributeUsage(System.AttributeTargets.Class, Inherited = true)]
            public sealed class CachePolicyAttribute(int seconds) : System.Attribute
            {
                public int Seconds { get; } = seconds;
            }

            [SecurityPolicy("base")]
            [CachePolicy(30)]
            public class BasePage : Microsoft.AspNetCore.Components.ComponentBase
            {
            }

            [SecurityPolicy("derived")]
            [Microsoft.AspNetCore.Components.StreamRendering(false)]
            public sealed class Page : BasePage
            {
            }
            """);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(context.Components, item => item.Type.Name == "Page");
            var securityPolicies = descriptor.Metadata
                .Where(item => item.GetType().Name == "SecurityPolicyAttribute")
                .Select(item => (string)item.GetType().GetProperty("Policy")!.GetValue(item)!)
                .ToArray();
            var cachePolicy = Assert.Single(
                descriptor.Metadata,
                item => item.GetType().Name == "CachePolicyAttribute");

            Assert.Equal(["derived", "base"], securityPolicies);
            Assert.Equal(30, cachePolicy.GetType().GetProperty("Seconds")!.GetValue(cachePolicy));
            Assert.False(Assert.Single(descriptor.Metadata.OfType<StreamRenderingAttribute>()).Enabled);
        }
    }

    [Fact]
    public void InheritedMetadata_PreservesInheritedAttributesAndExcludesNonInheritedAttributes()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public sealed class Layout : Microsoft.AspNetCore.Components.ComponentBase
            {
            }

            public sealed class TestMode : Microsoft.AspNetCore.Components.IComponentRenderMode
            {
            }

            public sealed class TestModeAttribute : Microsoft.AspNetCore.Components.RenderModeAttribute
            {
                public override Microsoft.AspNetCore.Components.IComponentRenderMode Mode => new TestMode();
            }

            [TestMode]
            [Microsoft.AspNetCore.Components.Layout(typeof(Layout))]
            [Microsoft.AspNetCore.Components.ExcludeFromInteractiveRouting]
            [Microsoft.AspNetCore.Components.Route("/base")]
            public class BaseComponent : Microsoft.AspNetCore.Components.ComponentBase
            {
            }

            public sealed class DerivedComponent : BaseComponent
            {
            }

            [TestMode]
            public sealed class OverrideComponent : BaseComponent
            {
            }
            """);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(context.Components, item => item.Type.Name == "DerivedComponent");
            Assert.Single(descriptor.Metadata.OfType<RenderModeAttribute>());
            Assert.Single(descriptor.Metadata.OfType<LayoutAttribute>());
            Assert.Single(descriptor.Metadata.OfType<ExcludeFromInteractiveRoutingAttribute>());
            Assert.Empty(descriptor.Metadata.OfType<RouteAttribute>());

            var overrideDescriptor = Assert.Single(context.Components, item => item.Type.Name == "OverrideComponent");
            Assert.Single(overrideDescriptor.Metadata.OfType<RenderModeAttribute>());
        }
    }

    [Fact]
    public void InheritedMetadata_PreservesDistinctBaseAndDerivedRenderModes()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public sealed class TestMode : Microsoft.AspNetCore.Components.IComponentRenderMode
            {
            }

            public sealed class BaseModeAttribute : Microsoft.AspNetCore.Components.RenderModeAttribute
            {
                public override Microsoft.AspNetCore.Components.IComponentRenderMode Mode => new TestMode();
            }

            public sealed class DerivedModeAttribute : Microsoft.AspNetCore.Components.RenderModeAttribute
            {
                public override Microsoft.AspNetCore.Components.IComponentRenderMode Mode => new TestMode();
            }

            [BaseMode]
            public class BaseComponent : Microsoft.AspNetCore.Components.ComponentBase
            {
            }

            [DerivedMode]
            public sealed class DerivedComponent : BaseComponent
            {
            }
            """);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(context.Components, item => item.Type.Name == "DerivedComponent");
            Assert.Equal(2, descriptor.Metadata.OfType<RenderModeAttribute>().Count());
        }
    }

    [Fact]
    public void MultipleNestedAndRecordContexts_EmitUniqueDeterministicSources()
    {
        const string hostSource = """
            namespace TestHost;

            public partial class FirstMetadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
            {
            }

            public partial class SecondMetadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
            {
            }

            public partial record Container
            {
                public sealed partial class NestedMetadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
                {
                }
            }
            """;

        var result = RunGenerator("""
            namespace TestComponents;
            public sealed class OneComponent : Microsoft.AspNetCore.Components.ComponentBase { }
            """, hostSource);

        Assert.Equal(
            ["TestHost.Container.NestedMetadata.Metadata.g.cs", "TestHost.FirstMetadata.Metadata.g.cs", "TestHost.SecondMetadata.Metadata.g.cs"],
            result.MetadataGeneratedSources.Select(source => source.HintName).OrderBy(name => name));
        Assert.All(result.MetadataGeneratedSources, source =>
        {
            var text = source.SourceText.ToString();
            Assert.Equal(1, text.Split("typeof(global::TestComponents.OneComponent)").Length - 1);
        });
        Assert.Contains("partial record Container", result.MetadataGeneratedSources.Single(source => source.HintName.Contains("NestedMetadata")).SourceText.ToString());
    }

    [Fact]
    public void NestedContexts_PreserveGenericContainingTypeKindsAndConstraints()
    {
        var result = RunGenerator(
            "namespace TestComponents;",
            """
            namespace TestHost;

            public partial class GenericContainer<T>
                where T : class, new()
            {
                public abstract partial class ClassMetadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
                {
                }
            }

            public partial struct StructContainer
            {
                public abstract partial class StructMetadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
                {
                }
            }

            public partial record struct RecordStructContainer<T>
                where T : unmanaged
            {
                public abstract partial class RecordStructMetadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
                {
                }
            }

            public partial interface InterfaceContainer<T>
                where T : class
            {
                public abstract partial class InterfaceMetadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
                {
                }
            }
            """);

        Assert.Equal(4, result.MetadataGeneratedSources.Length);
        var source = string.Join(Environment.NewLine, result.MetadataGeneratedSources.Select(item => item.SourceText.ToString()));
        Assert.Contains("partial class GenericContainer<T>", source);
        Assert.Contains("where T : class, new()", source);
        Assert.Contains("partial struct StructContainer", source);
        Assert.Contains("partial record struct RecordStructContainer<T>", source);
        Assert.Contains("where T : unmanaged", source);
        Assert.Contains("partial interface InterfaceContainer<T>", source);
    }

    [Fact]
    public void NestedContexts_WithSameContainerNameAndDifferentArity_HaveDistinctHintNames()
    {
        var result = RunGenerator(
            "namespace TestComponents;",
            """
            namespace TestHost;

            public partial class Container
            {
                public abstract partial class Metadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
                {
                }
            }

            public partial class Container<T>
            {
                public abstract partial class Metadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
                {
                }
            }
            """);

        Assert.Equal(
            ["TestHost.Container_1.Metadata.Metadata.g.cs", "TestHost.Container.Metadata.Metadata.g.cs"],
            result.MetadataGeneratedSources.Select(source => source.HintName).OrderBy(name => name));
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
