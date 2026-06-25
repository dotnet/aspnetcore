// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class ComponentDisposableSanityAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ComponentDisposableSanityAnalyzer();

    private static readonly string ComponentDeclarations = $@"
    namespace {typeof(IComponent).Namespace}
    {{
        public interface {typeof(IComponent).Name} {{ }}
        public abstract class ComponentBase : {typeof(IComponent).Name} {{ }}
    }}
";

    // -------------------------------------------------------------------------
    // BL0012 – Dispose() without IDisposable
    // -------------------------------------------------------------------------

    [Fact]
    public void ComponentWithDisposeButWithoutIDisposable_ReportsBL0012()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            public void Dispose() { }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = "BL0012",
                Message = "Component 'ConsoleApplication1.TestComponent' has a 'Dispose()' method but does not implement 'IDisposable'. The runtime will not call 'Dispose()' automatically.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 25)
                }
            });
    }

    [Fact]
    public void ComponentThatImplementsIDisposableAndHasDispose_NoBL0012()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using System;
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase, IDisposable
        {
            public void Dispose() { }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void NonComponentClassWithDisposeButWithoutIDisposable_NoBL0012()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        class NotAComponent
        {
            public void Dispose() { }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void ComponentWithPrivateDisposeButWithoutIDisposable_NoBL0012()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            private void Dispose() { }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    // -------------------------------------------------------------------------
    // BL0013 – DisposeAsync() without IAsyncDisposable
    // -------------------------------------------------------------------------

    [Fact]
    public void ComponentWithDisposeAsyncButWithoutIAsyncDisposable_ReportsBL0013()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = "BL0013",
                Message = "Component 'ConsoleApplication1.TestComponent' has a 'DisposeAsync()' method but does not implement 'IAsyncDisposable'. The runtime will not call 'DisposeAsync()' automatically.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 9, 30)
                }
            });
    }

    [Fact]
    public void ComponentThatImplementsIAsyncDisposableAndHasDisposeAsync_NoBL0013()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using System;
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase, IAsyncDisposable
        {
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void NonComponentClassWithDisposeAsyncButWithoutIAsyncDisposable_NoBL0013()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using System.Threading.Tasks;

        class NotAComponent
        {
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    // -------------------------------------------------------------------------
    // Both missing – component has both Dispose() and DisposeAsync() but
    // implements neither interface
    // -------------------------------------------------------------------------

    [Fact]
    public void ComponentMissingBothInterfaces_ReportsBothBL0012AndBL0013()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            public void Dispose() { }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = "BL0012",
                Message = "Component 'ConsoleApplication1.TestComponent' has a 'Dispose()' method but does not implement 'IDisposable'. The runtime will not call 'Dispose()' automatically.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 9, 25)
                }
            },
            new DiagnosticResult
            {
                Id = "BL0013",
                Message = "Component 'ConsoleApplication1.TestComponent' has a 'DisposeAsync()' method but does not implement 'IAsyncDisposable'. The runtime will not call 'DisposeAsync()' automatically.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 10, 30)
                }
            });
    }
}
