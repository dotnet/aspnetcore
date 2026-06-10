// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class StateHasChangedAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new StateHasChangedAnalyzer();

    private static readonly string ComponentDeclarations = @"
    namespace Microsoft.AspNetCore.Components
    {
        using System;
        using System.Threading.Tasks;

        public struct ParameterView
        {
        }

        public sealed class ParameterAttribute : Attribute
        {
            public bool CaptureUnmatchedValues { get; set; }
        }

        public sealed class CascadingParameterAttribute : Attribute
        {
        }

        public interface IComponent { }

        public struct EventCallback
        {
            public EventCallback(Func<Task> callback) { }
        }

        public sealed class EventCallbackFactory
        {
            public EventCallback Create(object receiver, Func<Task> callback) => default;
        }

        public abstract class ComponentBase : IComponent
        {
            public EventCallbackFactory EventCallbackFactory => default;

            protected virtual void OnInitialized() { }

            protected virtual Task OnInitializedAsync() => Task.CompletedTask;

            protected virtual void OnParametersSet() { }

            protected virtual Task OnParametersSetAsync() => Task.CompletedTask;

            public virtual Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;

            protected void StateHasChanged() { }
        }
    }
";

    [Fact]
    public void OnLifecycleEvents_WithStateHasChanged_ReportsDiagnostic()
    {

        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            protected override void OnInitialized()
            {
                // unnecesary
                StateHasChanged();
            }

            protected override void OnParametersSet()
            {
                // unnecesary
                StateHasChanged();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary here and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 17) }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary here and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 17, 17) }
            });
    }

    [Fact]
    public void OnAsyncLifecycleEvents_WithStateHasChanged_ReportsDiagnostic()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            private int _refreshCount;

            protected override async Task OnInitializedAsync()
            {
                _refreshCount++;
                // unnecesary
                StateHasChanged();

                await Task.Delay(1);

                _refreshCount++;
                // necesary
                StateHasChanged();

                await Task.Delay(1);

                _refreshCount++;
                // unnecesary
                StateHasChanged();
            }

            protected override async Task OnParametersSetAsync()
            {
                _refreshCount++;
                // unnecesary
                StateHasChanged();

                await Task.Delay(1);

                _refreshCount++;
                // necesary
                StateHasChanged();

                await Task.Delay(1);

                _refreshCount++;
                // unnecesary
                StateHasChanged();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary here and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 17) }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary here and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 26, 17) }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary here and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 33, 17) }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary here and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 45, 17) }
            });
    }

    [Fact]
    public void EventHandler_ReportsRedundantStateHasChangedCalls()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            private int _refreshCount;

            public EventCallback OnRefresh => EventCallbackFactory.Create(this, OnRefreshClicked);

            private async Task OnRefreshClicked()
            {
                _refreshCount++;
                // unnecesary
                StateHasChanged();

                await Task.Delay(1);

                _refreshCount++;
                // necesary
                StateHasChanged();

                await Task.Delay(1);

                _refreshCount++;
                // unnecesary
                StateHasChanged();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary here and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 17, 17) }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary here and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 29, 17) }
            });
    }
}
