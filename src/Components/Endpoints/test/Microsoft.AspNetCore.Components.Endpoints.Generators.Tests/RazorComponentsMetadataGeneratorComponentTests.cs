// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators.Tests;

#nullable enable

[TestClass]
public sealed class RazorComponentsMetadataGeneratorComponentTests : RazorComponentsMetadataGeneratorTestBase
{
    [TestMethod]
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
        StringAssert.Contains(source, "typeof(global::Microsoft.AspNetCore.Components.CascadingValue<string>)");
    }

    [TestMethod]
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
        StringAssert.Matches(source, new Regex(@"GetBuiltInComponentDescriptorFactory_\d+<string>\(null\)"));
        StringAssert.Contains(source, "Name = \"CreateValidationMessageDescriptors\"");
        StringAssert.Contains(
            source,
            "UnsafeAccessorType(\"Microsoft.AspNetCore.Components.Infrastructure.BuiltInComponentDescriptors, Microsoft.AspNetCore.Components.Web\")");
    }

    [TestMethod]
    public void ReferencedFrameworkComponents_IncludeFullyDescribableComponents()
    {
        var result = RunGenerator("namespace TestComponents;");

        var source = GetGeneratedSource(result);
        StringAssert.Contains(source, "typeof(global::Microsoft.AspNetCore.Components.Web.HeadOutlet)");
        StringAssert.Contains(source, "typeof(global::Microsoft.AspNetCore.Components.Sections.SectionOutlet)");
        StringAssert.Contains(source, "typeof(global::Microsoft.AspNetCore.Components.ResourcePreloader)");
        StringAssert.Contains(source, "UnsafeAccessorType(\"Microsoft.AspNetCore.Components.Infrastructure.BuiltInComponentDescriptors, Microsoft.AspNetCore.Components\")");
    }

    [TestMethod]
    [DataRow("Microsoft.AspNetCore.Components.Endpoints")]
    [DataRow("Microsoft.AspNetCore.Components.Forms")]
    public void ReferencedKnownFrameworkAssembly_EmitsProviderAccessor(string assemblyName)
    {
        var result = RunGenerator(
            "namespace TestComponents;",
            referencedAssemblyName: assemblyName);

        var source = GetGeneratedSource(result);
        StringAssert.Contains(
            source,
            $"UnsafeAccessorType(\"Microsoft.AspNetCore.Components.Infrastructure.BuiltInComponentDescriptors, {assemblyName}\")");
    }

    [TestMethod]
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
        StringAssert.Contains(source, "UnconditionalSuppressMessage(\"Trimming\", \"IL2067\"");
        StringAssert.Contains(source, "UnconditionalSuppressMessage(\"Trimming\", \"IL2111\"");
        StringAssert.Contains(source, "target.Type = value;");
    }

    [TestMethod]
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
        StringAssert.Contains(source, "typeof(global::TestComponents.Greeting)");
        StringAssert.Contains(source, "CreateInstance = static __services => new global::TestComponents.Greeting()");

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var component = Assert.ContainsSingle(GetReferencedComponents(context, result));
            Assert.AreEqual("TestComponents.Greeting", component.Type.FullName);
            var instance = component.CreateInstance!(new EmptyServiceProvider());
            Assert.AreEqual(component.Type, instance.GetType());

            var parameter = Assert.ContainsSingle(component.Parameters);
            Assert.AreEqual("Message", parameter.Name);
            Assert.AreEqual(typeof(string), parameter.ParameterType);
            Assert.IsInstanceOfType<ParameterAttribute>(parameter.Attribute);
            parameter.SetValue(instance, "Hello");
            Assert.AreEqual("Hello", parameter.GetValue(instance));
        }
    }

    [TestMethod]
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
            var descriptor = Assert.ContainsSingle(GetReferencedComponents(context, result));
            Assert.AreEqual("TestComponents.NeedsArgument", descriptor.Type.FullName);
            Assert.IsNull(descriptor.CreateInstance);
        }
    }

    [TestMethod]
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
            var descriptor = Assert.ContainsSingle(item => item.Type.Name == "DerivedComponent", context.Components);
            Assert.AreEqual(2, descriptor.Parameters.Count);
            Assert.AreEqual(typeof(int), Assert.ContainsSingle(item => item.Name == "Shared", descriptor.Parameters).ParameterType);
            Assert.AreEqual(typeof(int), Assert.ContainsSingle(item => item.Name == "Inherited", descriptor.Parameters).ParameterType);
        }
    }

    [TestMethod]
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

        Assert.AreEqual(MetadataImportOptions.Public, result.InputCompilation.Options.MetadataImportOptions);
        var source = GetGeneratedSource(result);
        StringAssert.Contains(source, "UnsafeAccessorKind.Method, Name = \"set_KeyedService\"");

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.ContainsSingle(GetReferencedComponents(context, result));
            var instance = descriptor.CreateInstance!(new EmptyServiceProvider());
            var publicInjection = Assert.ContainsSingle(item => item.Name == "PublicService", descriptor.Injectables);
            var keyedInjection = Assert.ContainsSingle(item => item.Name == "KeyedService", descriptor.Injectables);
            Assert.IsNull(publicInjection.Attribute.Key);
            Assert.AreEqual("primary", keyedInjection.Attribute.Key);

            publicInjection.SetValue(instance, "service");
            keyedInjection.SetValue(instance, 42);
            Assert.AreEqual("service", descriptor.Type.GetProperty("PublicService")!.GetValue(instance));
            Assert.AreEqual(42, descriptor.Type.GetMethod("ReadKeyedService")!.Invoke(instance, null));
        }
    }

    [TestMethod]
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
        StringAssert.Contains(source, "global::TestComponents.BaseComponent target");

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.ContainsSingle(item => item.Type.Name == "DerivedComponent", context.Components);
            var instance = descriptor.CreateInstance!(new EmptyServiceProvider());
            var parameter = Assert.ContainsSingle(descriptor.Parameters);
            var injectable = Assert.ContainsSingle(descriptor.Injectables);

            parameter.SetValue(instance, "inherited");
            injectable.SetValue(instance, "service");

            Assert.AreEqual("inherited", parameter.GetValue(instance));
            Assert.AreEqual("service", descriptor.Type.GetMethod("ReadInheritedService")!.Invoke(instance, null));
        }
    }

    [TestMethod]
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
        StringAssert.Contains(source, "Name = \"get_Secret\"");
        StringAssert.Contains(source, "Name = \"set_Secret\"");

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.ContainsSingle(GetReferencedComponents(context, result));
            Assert.AreEqual(3, descriptor.Parameters.Count);
            var theme = Assert.ContainsSingle(item => item.Name == "Theme", descriptor.Parameters);
            Assert.IsInstanceOfType<CascadingParameterAttribute>(theme.Attribute);
            var themeAttribute = (CascadingParameterAttribute)theme.Attribute;
            Assert.AreEqual("theme", themeAttribute.Name);

            var page = Assert.ContainsSingle(item => item.Name == "Page", descriptor.Parameters);
            Assert.IsInstanceOfType<SupplyParameterFromQueryAttribute>(page.Attribute);
            var queryAttribute = (SupplyParameterFromQueryAttribute)page.Attribute;
            Assert.AreEqual("page", queryAttribute.Name);

            var instance = descriptor.CreateInstance!(new EmptyServiceProvider());
            var secret = Assert.ContainsSingle(item => item.Name == "Secret", descriptor.Parameters);
            secret.SetValue(instance, "hidden");
            Assert.AreEqual("hidden", secret.GetValue(instance));
            Assert.AreEqual("hidden", descriptor.Type.GetMethod("ReadSecret")!.Invoke(instance, null));
        }
    }

    [TestMethod]
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
            var descriptor = Assert.ContainsSingle(item => item.Type.Name == "RoutedComponent", context.Components);
            Assert.AreEqual(4, descriptor.Metadata.Count);
            Assert.AreEqual("/dashboard/{id:int}", Assert.ContainsSingle(descriptor.Metadata.OfType<RouteAttribute>()).Template);
            Assert.AreEqual("TestComponents.Layout", Assert.ContainsSingle(descriptor.Metadata.OfType<LayoutAttribute>()).LayoutType.FullName);
            Assert.ContainsSingle(descriptor.Metadata.OfType<ExcludeFromInteractiveRoutingAttribute>());

            var renderModeAttribute = Assert.ContainsSingle(descriptor.Metadata.OfType<RenderModeAttribute>());
            Assert.AreEqual("TestModeAttribute", renderModeAttribute.GetType().Name);
            Assert.AreEqual("server", renderModeAttribute.GetType().GetProperty("Name")!.GetValue(renderModeAttribute));
            Assert.AreEqual(7, renderModeAttribute.GetType().GetProperty("Order")!.GetValue(renderModeAttribute));
        }
    }

    [TestMethod]
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
            var descriptor = Assert.ContainsSingle(item => item.Type.Name == "Page", context.Components);
            var securityPolicies = descriptor.Metadata
                .Where(item => item.GetType().Name == "SecurityPolicyAttribute")
                .Select(item => (string)item.GetType().GetProperty("Policy")!.GetValue(item)!)
                .ToArray();
            var cachePolicy = Assert.ContainsSingle(
                item => item.GetType().Name == "CachePolicyAttribute",
                descriptor.Metadata);

            CollectionAssert.AreEqual(new[] { "derived", "base" }, securityPolicies);
            Assert.AreEqual(30, cachePolicy.GetType().GetProperty("Seconds")!.GetValue(cachePolicy));
            Assert.IsFalse(Assert.ContainsSingle(descriptor.Metadata.OfType<StreamRenderingAttribute>()).Enabled);
        }
    }

    [TestMethod]
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
            var descriptor = Assert.ContainsSingle(item => item.Type.Name == "DerivedComponent", context.Components);
            Assert.ContainsSingle(descriptor.Metadata.OfType<RenderModeAttribute>());
            Assert.ContainsSingle(descriptor.Metadata.OfType<LayoutAttribute>());
            Assert.ContainsSingle(descriptor.Metadata.OfType<ExcludeFromInteractiveRoutingAttribute>());
            Assert.IsEmpty(descriptor.Metadata.OfType<RouteAttribute>());

            var overrideDescriptor = Assert.ContainsSingle(item => item.Type.Name == "OverrideComponent", context.Components);
            Assert.ContainsSingle(overrideDescriptor.Metadata.OfType<RenderModeAttribute>());
        }
    }

    [TestMethod]
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
            var descriptor = Assert.ContainsSingle(item => item.Type.Name == "DerivedComponent", context.Components);
            Assert.AreEqual(2, descriptor.Metadata.OfType<RenderModeAttribute>().Count());
        }
    }

    [TestMethod]
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

        CollectionAssert.AreEqual(
            new[] { "TestHost.Container.NestedMetadata.Metadata.g.cs", "TestHost.FirstMetadata.Metadata.g.cs", "TestHost.SecondMetadata.Metadata.g.cs" },
            result.MetadataGeneratedSources.Select(source => source.HintName).OrderBy(name => name).ToArray());
        foreach (var generatedSource in result.MetadataGeneratedSources)
        {
            var text = generatedSource.SourceText.ToString();
            Assert.AreEqual(1, text.Split("typeof(global::TestComponents.OneComponent)").Length - 1);
        }
        StringAssert.Contains(result.MetadataGeneratedSources.Single(source => source.HintName.Contains("NestedMetadata")).SourceText.ToString(), "partial record Container");
    }

    [TestMethod]
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

        Assert.AreEqual(4, result.MetadataGeneratedSources.Length);
        var source = string.Join(Environment.NewLine, result.MetadataGeneratedSources.Select(item => item.SourceText.ToString()));
        StringAssert.Contains(source, "partial class GenericContainer<T>");
        StringAssert.Contains(source, "where T : class, new()");
        StringAssert.Contains(source, "partial struct StructContainer");
        StringAssert.Contains(source, "partial record struct RecordStructContainer<T>");
        StringAssert.Contains(source, "where T : unmanaged");
        StringAssert.Contains(source, "partial interface InterfaceContainer<T>");
    }

    [TestMethod]
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

        CollectionAssert.AreEqual(
            new[] { "TestHost.Container_1.Metadata.Metadata.g.cs", "TestHost.Container.Metadata.Metadata.g.cs" },
            result.MetadataGeneratedSources.Select(source => source.HintName).OrderBy(name => name).ToArray());
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
