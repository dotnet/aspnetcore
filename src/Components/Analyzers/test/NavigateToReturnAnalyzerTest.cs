// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class NavigateToReturnAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new NavigateToReturnAnalyzer();

    private static readonly string NavigationManagerDeclaration = @"
    namespace Microsoft.AspNetCore.Components
    {
        public class NavigationManager
        {
            public void NavigateTo(string uri, bool forceLoad = false) { }
        }
    }
";

    [Fact]
    public void DiagnosticForCodeAfterNavigateTo()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestClass
        {
            private NavigationManager Navigation;

            public void Handle()
            {
                Navigation.NavigateTo(""/"");
                System.Console.WriteLine(""this still runs"");
            }
        }
    }" + NavigationManagerDeclaration;

        var expected = new DiagnosticResult
        {
            Id = DiagnosticDescriptors.CodeAfterNavigateToWillExecute.Id,
            Message = "Code after this 'NavigateTo' call will still execute because 'NavigateTo' does not stop execution. Add a 'return' statement after 'NavigateTo' if the following code should not run.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 12, 17)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void NoDiagnosticWhenReturnFollowsNavigateTo()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestClass
        {
            private NavigationManager Navigation;

            public void Handle()
            {
                Navigation.NavigateTo(""/"");
                return;
            }
        }
    }" + NavigationManagerDeclaration;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void NoDiagnosticWhenNavigateToIsLastStatement()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestClass
        {
            private NavigationManager Navigation;

            public void Handle()
            {
                Navigation.NavigateTo(""/"");
            }
        }
    }" + NavigationManagerDeclaration;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void NoDiagnosticForUnrelatedNavigateTo()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        class NavigationManager
        {
            public void NavigateTo(string uri) { }
        }

        class TestClass
        {
            private NavigationManager Navigation;

            public void Handle()
            {
                Navigation.NavigateTo(""/"");
                System.Console.WriteLine(""this still runs"");
            }
        }
    }";

        VerifyCSharpDiagnostic(test);
    }
}
