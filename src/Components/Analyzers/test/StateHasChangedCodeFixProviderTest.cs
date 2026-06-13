// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class StateHasChangedCodeFixProviderTest : CodeFixVerifier
{
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
    public void OnLifecycleMethod_RemovesUnnecessaryStateHasChangedCall()
    {
        var oldSource = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            protected override void OnInitialized()
            {
                StateHasChanged();
            }
        }
    }" + ComponentDeclarations;

        var newSource = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            protected override void OnInitialized()
            {
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpFix(oldSource, newSource);
    }

    [Fact]
    public void OnAsyncLifecycleMethod_RemovesOnlyReportedStateHasChangedCalls()
    {
        var oldSource = @"
    namespace ConsoleApplication1
    {
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            protected override async Task OnInitializedAsync()
            {
                StateHasChanged();
                await Task.Delay(1);
                StateHasChanged();
                await Task.Delay(1);
                StateHasChanged();
            }
        }
    }" + ComponentDeclarations;

        var newSource = @"
    namespace ConsoleApplication1
    {
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            protected override async Task OnInitializedAsync()
            {
                await Task.Delay(1);
                StateHasChanged();
                await Task.Delay(1);
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpFix(oldSource, newSource);
    }

    protected override CodeFixProvider GetCSharpCodeFixProvider() => new StateHasChangedCodeFixProvider();

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new StateHasChangedAnalyzer();
}
