// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class VirtualizeItemKeyAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new VirtualizeItemKeyAnalyzer();

    private static readonly string VirtualizeDeclarations = @"
    namespace Microsoft.AspNetCore.Components.Rendering
    {
        public class RenderTreeBuilder
        {
            public void OpenComponent<TComponent>(int sequence) where TComponent : IComponent { }
            public void AddComponentParameter(int sequence, string name, object value) { }
            public void CloseComponent() { }
        }
    }

    namespace Microsoft.AspNetCore.Components
    {
        public interface IComponent { }
    }

    namespace Microsoft.AspNetCore.Components.Web.Virtualization
    {
        public class Virtualize<TItem> : Microsoft.AspNetCore.Components.IComponent
        {
            public object ItemsProvider { get; set; }
            public object Items { get; set; }
            public object ItemKey { get; set; }
            public float ItemSize { get; set; }
        }

        public delegate System.Threading.Tasks.ValueTask<ItemsProviderResult<TItem>> ItemsProviderDelegate<TItem>(ItemsProviderRequest request);

        public struct ItemsProviderRequest { }
        public struct ItemsProviderResult<TItem> { }
    }
";

    [Fact]
    public void ItemsProviderWithoutItemKey_ReportsDiagnostic()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder __builder)
            {
                __builder.OpenComponent<Virtualize<string>>(0);
                __builder.AddComponentParameter(1, ""ItemsProvider"", (object)null);
                __builder.AddComponentParameter(2, ""ItemSize"", (object)50f);
                __builder.CloseComponent();
            }
        }
    }" + VirtualizeDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.VirtualizeItemsProviderRequiresItemKey.Id,
                Message = "Virtualize uses 'ItemsProvider' without 'ItemKey'. Set ItemKey to a function returning a unique key per item (e.g., ItemKey=\"@(item => item.Id)\").",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 12, 17)
                }
            });
    }

    [Fact]
    public void ItemsProviderWithItemKey_NoDiagnostic()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder __builder)
            {
                __builder.OpenComponent<Virtualize<string>>(0);
                __builder.AddComponentParameter(1, ""ItemsProvider"", (object)null);
                __builder.AddComponentParameter(2, ""ItemKey"", (object)null);
                __builder.CloseComponent();
            }
        }
    }" + VirtualizeDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void ItemsCollectionWithoutItemKey_NoDiagnostic()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components.Rendering;
        using Microsoft.AspNetCore.Components.Web.Virtualization;

        class TestComponent
        {
            void BuildRenderTree(RenderTreeBuilder __builder)
            {
                __builder.OpenComponent<Virtualize<string>>(0);
                __builder.AddComponentParameter(1, ""Items"", (object)null);
                __builder.CloseComponent();
            }
        }
    }" + VirtualizeDeclarations;

        VerifyCSharpDiagnostic(test);
    }
}
