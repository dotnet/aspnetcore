// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class NavigateToReturnAnalyzerTest : DiagnosticVerifier
{
    private const string DisableThrowNavigationExceptionProperty = "build_property.BlazorDisableThrowNavigationException";

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new NavigateToReturnAnalyzer();

    private static AnalyzerOptions EnabledAnalyzerOptions { get; } = CreateAnalyzerOptions("true");

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

        VerifyCSharpDiagnostic(test, EnabledAnalyzerOptions, CreateExpectedDiagnostic(12, 17));
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

        VerifyCSharpDiagnostic(test, EnabledAnalyzerOptions);
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

        VerifyCSharpDiagnostic(test, EnabledAnalyzerOptions);
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

        VerifyCSharpDiagnostic(test, EnabledAnalyzerOptions);
    }

    [Fact]
    public void DiagnosticForCodeAfterNavigateToInSwitchSection()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestClass
        {
            private NavigationManager Navigation;

            public void Handle(int value)
            {
                switch (value)
                {
                    case 0:
                        Navigation.NavigateTo(""/"");
                        System.Console.WriteLine(""this still runs"");
                        break;
                }
            }
        }
    }" + NavigationManagerDeclaration;

        VerifyCSharpDiagnostic(test, EnabledAnalyzerOptions, CreateExpectedDiagnostic(15, 25));
    }

    [Fact]
    public void OneDiagnosticForMultipleStatementsAfterNavigateTo()
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
                System.Console.WriteLine(""first"");
                System.Console.WriteLine(""second"");
            }
        }
    }" + NavigationManagerDeclaration;

        VerifyCSharpDiagnostic(test, EnabledAnalyzerOptions, CreateExpectedDiagnostic(12, 17));
    }

    [Fact]
    public void DiagnosticWhenCodeBeforeTrailingReturn()
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
                return;
            }
        }
    }" + NavigationManagerDeclaration;

        VerifyCSharpDiagnostic(test, EnabledAnalyzerOptions, CreateExpectedDiagnostic(12, 17));
    }

    [Fact]
    public void DiagnosticForNavigateToReceiverExpressions()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestClass
        {
            private NavigationManager Navigation;

            private NavigationManager GetNavigationManager() => Navigation;

            public void Handle()
            {
                GetNavigationManager().NavigateTo(""/first"");
                System.Console.WriteLine(""first still runs"");
                this.Navigation.NavigateTo(""/second"");
                System.Console.WriteLine(""second still runs"");
            }
        }
    }" + NavigationManagerDeclaration;

        VerifyCSharpDiagnostic(
            test,
            EnabledAnalyzerOptions,
            CreateExpectedDiagnostic(14, 17),
            CreateExpectedDiagnostic(16, 17));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    public void NoDiagnosticWhenNavigateToStillThrows(string propertyValue)
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
                System.Console.WriteLine(""unreachable when NavigateTo throws"");
            }
        }
    }" + NavigationManagerDeclaration;

        VerifyCSharpDiagnostic(test, CreateAnalyzerOptions(propertyValue));
    }

    private static DiagnosticResult CreateExpectedDiagnostic(int line, int column) => new()
    {
        Id = DiagnosticDescriptors.CodeAfterNavigateToWillExecute.Id,
        Message = "Code after this 'NavigateTo' call will still execute because 'NavigateTo' does not stop execution. Add a 'return' statement after 'NavigateTo' if the following code should not run.",
        Severity = DiagnosticSeverity.Warning,
        Locations = new[]
        {
            new DiagnosticResultLocation("Test0.cs", line, column)
        }
    };

    private static AnalyzerOptions CreateAnalyzerOptions(string propertyValue)
    {
        var options = new Dictionary<string, string>();
        if (propertyValue is not null)
        {
            options.Add(DisableThrowNavigationExceptionProperty, propertyValue);
        }

        return new AnalyzerOptions(
            ImmutableArray<AdditionalText>.Empty,
            new TestAnalyzerConfigOptionsProvider(options));
    }

    private sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly TestAnalyzerConfigOptions _globalOptions;

        public TestAnalyzerConfigOptionsProvider(IReadOnlyDictionary<string, string> globalOptions)
        {
            _globalOptions = new TestAnalyzerConfigOptions(globalOptions);
        }

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _globalOptions;
    }

    private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly IReadOnlyDictionary<string, string> _options;

        public TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string> options)
        {
            _options = options;
        }

        public override bool TryGetValue(string key, out string value) => _options.TryGetValue(key, out value);
    }
}
