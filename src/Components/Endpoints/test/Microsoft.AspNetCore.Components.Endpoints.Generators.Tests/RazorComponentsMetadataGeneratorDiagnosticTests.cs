// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators.Tests;

public class RazorComponentsMetadataGeneratorDiagnosticTests : RazorComponentsMetadataGeneratorTestBase
{
    [Theory]
    [InlineData(null, DiagnosticSeverity.Warning)]
    [InlineData(false, DiagnosticSeverity.Error)]
    public void BlazorAot001_IncompleteParameter_SeverityFollowsReflectionMode(
        bool? reflectionEnabledByDefault,
        DiagnosticSeverity expectedSeverity)
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public sealed class BrokenComponent : Microsoft.AspNetCore.Components.ComponentBase
            {
                [Microsoft.AspNetCore.Components.Parameter]
                private int ReadOnly { get; } = 1;
            }
            """,
            razorComponentsReflectionEnabledByDefault: reflectionEnabledByDefault,
            expectedDiagnosticIds: ["BLAZORAOT001"]);

        var diagnostic = AssertDiagnostic(result, "BLAZORAOT001", expectedSeverity);
        Assert.Contains("TestComponents.BrokenComponent", diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.Contains("missing a getter or a setter", diagnostic.GetMessage(CultureInfo.InvariantCulture));

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            Assert.Empty(GetReferencedComponents(context, result));
        }
    }

    [Fact]
    public void BlazorAot002_InaccessibleBindableRoot_ReportsWarning()
    {
        var result = RunGenerator(
            "namespace TestComponents;",
            """
            namespace TestHost;

            [Microsoft.AspNetCore.Components.Web.BindableModel(ModelType = typeof(TestMetadata.PrivateModel))]
            public abstract partial class TestMetadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
            {
                private sealed class PrivateModel
                {
                    public int Value { get; set; }
                }
            }
            """,
            expectedDiagnosticIds: ["BLAZORAOT002"]);

        var diagnostic = AssertDiagnostic(result, "BLAZORAOT002", DiagnosticSeverity.Warning);
        Assert.Contains("TestHost.TestMetadata.PrivateModel", diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.Contains("not accessible from the application", diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.Contains("BindableTypes = []", GetGeneratedSource(result));
    }

    [Fact]
    public void BlazorAot003_NonPartialContext_ReportsErrorWithoutGeneratingPartial()
    {
        var result = RunGenerator(
            "namespace TestComponents;",
            """
            namespace TestHost;

            public abstract class InvalidMetadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
            {
            }
            """,
            expectedDiagnosticIds: ["BLAZORAOT003"]);

        var diagnostic = AssertDiagnostic(result, "BLAZORAOT003", DiagnosticSeverity.Error);
        Assert.Contains("TestHost.InvalidMetadata", diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.Contains("must be declared partial", diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.Empty(result.MetadataGeneratedSources);
        Assert.DoesNotContain(result.UpdatedCompilation.GetDiagnostics(TestContext.Current.CancellationToken), item => item.Id == "CS0260");
    }

    [Fact]
    public void BlazorAot003_NonPartialContainingType_ReportsErrorWithoutGeneratingPartial()
    {
        var result = RunGenerator(
            "namespace TestComponents;",
            """
            namespace TestHost;

            public class Container
            {
                public abstract partial class NestedMetadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
                {
                }
            }
            """,
            expectedDiagnosticIds: ["BLAZORAOT003"]);

        var diagnostic = AssertDiagnostic(result, "BLAZORAOT003", DiagnosticSeverity.Error);
        Assert.Contains("TestHost.Container", diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.Contains("must be declared partial", diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.Empty(result.MetadataGeneratedSources);
        Assert.DoesNotContain(result.UpdatedCompilation.GetDiagnostics(TestContext.Current.CancellationToken), item => item.Id == "CS0260");
    }

    [Fact]
    public void BlazorAot004_InaccessibleRenderModeAttribute_ReportsWarningAndOmitsPartialDescriptor()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            [PrivateMode]
            public sealed class RenderedComponent : Microsoft.AspNetCore.Components.ComponentBase
            {
                private sealed class PrivateModeAttribute : Microsoft.AspNetCore.Components.RenderModeAttribute
                {
                    public override Microsoft.AspNetCore.Components.IComponentRenderMode Mode => new PrivateMode();
                }

                private sealed class PrivateMode : Microsoft.AspNetCore.Components.IComponentRenderMode
                {
                }
            }
            """,
            expectedDiagnosticIds: ["BLAZORAOT004"]);

        var diagnostic = AssertDiagnostic(result, "BLAZORAOT004", DiagnosticSeverity.Warning);
        Assert.Contains("TestComponents.RenderedComponent", diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.Contains("PrivateModeAttribute", diagnostic.GetMessage(CultureInfo.InvariantCulture));

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            Assert.Empty(GetReferencedComponents(context, result));
        }
    }

    [Theory]
    [InlineData(null, DiagnosticSeverity.Warning)]
    [InlineData(true, DiagnosticSeverity.Warning)]
    [InlineData(false, DiagnosticSeverity.Error)]
    public void BlazorAot004_PartialAssembly_SeverityFollowsReflectionMode(
        bool? reflectionEnabledByDefault,
        DiagnosticSeverity expectedSeverity)
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public sealed class ValidComponent : Microsoft.AspNetCore.Components.ComponentBase
            {
            }

            [PrivateMode]
            public sealed class OmittedComponent : Microsoft.AspNetCore.Components.ComponentBase
            {
                private sealed class PrivateModeAttribute : Microsoft.AspNetCore.Components.RenderModeAttribute
                {
                    public override Microsoft.AspNetCore.Components.IComponentRenderMode Mode => new PrivateMode();
                }

                private sealed class PrivateMode : Microsoft.AspNetCore.Components.IComponentRenderMode
                {
                }
            }
            """,
            razorComponentsReflectionEnabledByDefault: reflectionEnabledByDefault,
            expectedDiagnosticIds: ["BLAZORAOT004"]);

        AssertDiagnostic(result, "BLAZORAOT004", expectedSeverity);
        Assert.Contains("ValidComponent", GetGeneratedSource(result));
        Assert.DoesNotContain("OmittedComponent", GetGeneratedSource(result));
    }

    [Fact]
    public void AccessibilityMatrix_EmitsFriendInternalTypeAndRejectsUnnameablePointer()
    {
        var result = RunGenerator(
            """
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("GeneratorHost")]

            namespace TestComponents;

            internal sealed class FriendComponent : Microsoft.AspNetCore.Components.ComponentBase
            {
                [Microsoft.AspNetCore.Components.Parameter]
                internal int Value { get; set; }
            }

            public unsafe sealed class PointerComponent : Microsoft.AspNetCore.Components.ComponentBase
            {
                [Microsoft.AspNetCore.Components.Parameter]
                public int* Value { get; set; }
            }

            public sealed class Container
            {
                private sealed class NestedPrivateComponent : Microsoft.AspNetCore.Components.ComponentBase
                {
                }
            }
            """,
            hostAssemblyName: "GeneratorHost",
            expectedDiagnosticIds: ["BLAZORAOT001"]);

        AssertDiagnostic(result, "BLAZORAOT001", DiagnosticSeverity.Warning);
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            Assert.Equal(
                "TestComponents.FriendComponent",
                Assert.Single(GetReferencedComponents(context, result)).Type.FullName);
        }
    }

    [Fact]
    public void UnsafeAccessorMatrix_EmitsExactMethodAndFieldKindsAndClrNames()
    {
        var result = RunGenerator(
            """
            namespace TestComponents;

            public sealed class AccessorComponent : Microsoft.AspNetCore.Components.ComponentBase
            {
                [Microsoft.AspNetCore.Components.Parameter]
                private string? Value { get; set; }

                [Microsoft.AspNetCore.Components.Inject]
                protected object? Service { get; set; }
            }

            public sealed class AccessorModel
            {
                private int _field = 8;
                private string Property { get; } = "value";
            }
            """,
            """
            namespace TestHost;

            [Microsoft.AspNetCore.Components.Web.BindableModel(ModelType = typeof(TestComponents.AccessorModel))]
            public sealed partial class TestMetadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
            {
            }
            """);

        var source = GetGeneratedSource(result);
        Assert.Contains("UnsafeAccessorKind.Method, Name = \"get_Value\"", source);
        Assert.Contains("UnsafeAccessorKind.Method, Name = \"set_Value\"", source);
        Assert.Contains("UnsafeAccessorKind.Method, Name = \"set_Service\"", source);
        Assert.Contains("UnsafeAccessorKind.Field, Name = \"_field\"", source);
        Assert.Contains("UnsafeAccessorKind.Method, Name = \"get_Property\"", source);
    }
}
