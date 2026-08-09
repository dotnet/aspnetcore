// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators.Tests;

[TestClass]
public sealed class RazorComponentsMetadataGeneratorDiagnosticTests : RazorComponentsMetadataGeneratorTestBase
{
    [TestMethod]
    [DataRow(null, DiagnosticSeverity.Warning)]
    [DataRow(false, DiagnosticSeverity.Error)]
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
        StringAssert.Contains(diagnostic.GetMessage(CultureInfo.InvariantCulture), "TestComponents.BrokenComponent");
        StringAssert.Contains(diagnostic.GetMessage(CultureInfo.InvariantCulture), "missing a getter or a setter");

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            Assert.IsEmpty(GetReferencedComponents(context, result));
        }
    }

    [TestMethod]
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
        StringAssert.Contains(diagnostic.GetMessage(CultureInfo.InvariantCulture), "TestHost.TestMetadata.PrivateModel");
        StringAssert.Contains(diagnostic.GetMessage(CultureInfo.InvariantCulture), "not accessible from the application");
        StringAssert.Contains(GetGeneratedSource(result), "BindableTypes = []");
    }

    [TestMethod]
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
        StringAssert.Contains(diagnostic.GetMessage(CultureInfo.InvariantCulture), "TestHost.InvalidMetadata");
        StringAssert.Contains(diagnostic.GetMessage(CultureInfo.InvariantCulture), "must be declared partial");
        Assert.IsEmpty(result.MetadataGeneratedSources);
        Assert.DoesNotContain(item => item.Id == "CS0260", result.UpdatedCompilation.GetDiagnostics());
    }

    [TestMethod]
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
        StringAssert.Contains(diagnostic.GetMessage(CultureInfo.InvariantCulture), "TestHost.Container");
        StringAssert.Contains(diagnostic.GetMessage(CultureInfo.InvariantCulture), "must be declared partial");
        Assert.IsEmpty(result.MetadataGeneratedSources);
        Assert.DoesNotContain(item => item.Id == "CS0260", result.UpdatedCompilation.GetDiagnostics());
    }

    [TestMethod]
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
        StringAssert.Contains(diagnostic.GetMessage(CultureInfo.InvariantCulture), "TestComponents.RenderedComponent");
        StringAssert.Contains(diagnostic.GetMessage(CultureInfo.InvariantCulture), "PrivateModeAttribute");

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            Assert.IsEmpty(GetReferencedComponents(context, result));
        }
    }

    [TestMethod]
    [DataRow(null, DiagnosticSeverity.Warning)]
    [DataRow(true, DiagnosticSeverity.Warning)]
    [DataRow(false, DiagnosticSeverity.Error)]
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
        StringAssert.Contains(GetGeneratedSource(result), "ValidComponent");
        Assert.IsFalse(GetGeneratedSource(result).Contains("OmittedComponent", StringComparison.Ordinal));
    }

    [TestMethod]
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
            Assert.AreEqual(
                "TestComponents.FriendComponent",
                Assert.ContainsSingle(GetReferencedComponents(context, result)).Type.FullName);
        }
    }

    [TestMethod]
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
        StringAssert.Contains(source, "UnsafeAccessorKind.Method, Name = \"get_Value\"");
        StringAssert.Contains(source, "UnsafeAccessorKind.Method, Name = \"set_Value\"");
        StringAssert.Contains(source, "UnsafeAccessorKind.Method, Name = \"set_Service\"");
        StringAssert.Contains(source, "UnsafeAccessorKind.Field, Name = \"_field\"");
        StringAssert.Contains(source, "UnsafeAccessorKind.Method, Name = \"get_Property\"");
    }
}
