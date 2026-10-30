// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class NavigateToReturnAnalyzerTest : DiagnosticVerifier
{
    private const string DiagnosticId = "BL0020";
    private const string DisableThrowNavigationExceptionProperty = "build_property.BlazorDisableThrowNavigationException";
    private const string DiagnosticMessage = "This project uses exception-driven navigation during prerendering. This behavior is obsolete and differs from interactive rendering, where NavigationManager.NavigateTo returns normally. Set BlazorDisableThrowNavigationException to true and make the intended control flow after navigation explicit.";

    private const string BlazorSsrApplication = """
        namespace Microsoft.AspNetCore.Builder
        {
            public static class RazorComponentsEndpointRouteBuilderExtensions
            {
                public static object MapRazorComponents<TComponent>(this object endpoints) => endpoints;
            }
        }

        namespace TestApplication
        {
            using Microsoft.AspNetCore.Builder;

            public class App { }

            public static class Program
            {
                public static void Configure(object endpoints)
                {
                    endpoints.MapRazorComponents<App>();
                }
            }
        }
        """;

    private const string InteractiveOnlyApplication = """
        namespace Microsoft.AspNetCore.Components
        {
            public class NavigationManager
            {
                public void NavigateTo(string uri) { }
            }
        }

        namespace TestApplication
        {
            using Microsoft.AspNetCore.Components;

            public class Component
            {
                private readonly NavigationManager _navigation = new();

                public void Handle()
                {
                    _navigation.NavigateTo("/");
                    System.Console.WriteLine("This is valid in interactive rendering.");
                }
            }
        }
        """;

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new NavigateToReturnAnalyzer();

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    [InlineData("False")]
    public void ReportsProjectDiagnosticWhenExceptionDrivenNavigationIsEnabled(string propertyValue)
    {
        VerifyCSharpDiagnostic(
            BlazorSsrApplication,
            CreateAnalyzerOptions(propertyValue),
            OutputKind.ConsoleApplication,
            CreateExpectedDiagnostic());
    }

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    public void NoDiagnosticWhenExceptionDrivenNavigationIsDisabled(string propertyValue)
    {
        VerifyCSharpDiagnostic(BlazorSsrApplication, CreateAnalyzerOptions(propertyValue), OutputKind.ConsoleApplication);
    }

    [Fact]
    public void ReportsOneDiagnosticForMultipleRazorComponentMappings()
    {
        const string applicationWithMultipleMappings = """
            namespace Microsoft.AspNetCore.Builder
            {
                public static class RazorComponentsEndpointRouteBuilderExtensions
                {
                    public static object MapRazorComponents<TComponent>(this object endpoints) => endpoints;
                }
            }

            namespace TestApplication
            {
                using Microsoft.AspNetCore.Builder;

                public class FirstApp { }
                public class SecondApp { }

                public static class Program
                {
                    public static void Configure(object endpoints)
                    {
                        endpoints.MapRazorComponents<FirstApp>();
                        endpoints.MapRazorComponents<SecondApp>();
                    }
                }
            }
            """;

        VerifyCSharpDiagnostic(
            applicationWithMultipleMappings,
            CreateAnalyzerOptions(propertyValue: null),
            OutputKind.ConsoleApplication,
            CreateExpectedDiagnostic());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    [InlineData("true")]
    public void NoDiagnosticForProjectWithoutServerSideRazorComponents(string propertyValue)
    {
        VerifyCSharpDiagnostic(InteractiveOnlyApplication, CreateAnalyzerOptions(propertyValue), OutputKind.ConsoleApplication);
    }

    [Fact]
    public void ReportsForApplicationUsingMappingWrapper()
    {
        const string applicationWithMappingWrapper = """
            namespace Microsoft.AspNetCore.Builder
            {
                public static class RazorComponentsEndpointRouteBuilderExtensions
                {
                    public static object MapRazorComponents<TComponent>(this object endpoints) => endpoints;
                }
            }

            namespace Microsoft.Extensions.DependencyInjection
            {
                public static class RazorComponentsServiceCollectionExtensions
                {
                    public static object AddRazorComponents(this object services) => services;
                }
            }

            namespace TestApplication
            {
                using Microsoft.Extensions.DependencyInjection;

                public class App { }

                public static class Program
                {
                    public static void Main()
                    {
                        new object().AddRazorComponents();
                        // The application calls a wrapper implemented in a referenced library.
                        MappingLibrary.Configure();
                    }
                }

                public static class MappingLibrary
                {
                    public static void Configure() { }
                }
            }
            """;

        VerifyCSharpDiagnostic(
            applicationWithMappingWrapper,
            CreateAnalyzerOptions(propertyValue: null, hasRazorFile: false),
            OutputKind.ConsoleApplication,
            CreateExpectedDiagnostic());
    }

    [Fact]
    public void NoDiagnosticForWebProjectWithRazorFileButNoServerRendering()
    {
        const string applicationWithoutServerRendering = """
            namespace Microsoft.AspNetCore.Builder
            {
                public static class RazorComponentsEndpointRouteBuilderExtensions
                {
                    public static object MapRazorComponents<TComponent>(this object endpoints) => endpoints;
                }
            }

            namespace Microsoft.Extensions.DependencyInjection
            {
                public static class RazorComponentsServiceCollectionExtensions
                {
                    public static object AddRazorComponents(this object services) => services;
                }
            }

            namespace TestApplication
            {
                public static class Program
                {
                    public static void Main() { }
                }
            }
            """;

        VerifyCSharpDiagnostic(
            applicationWithoutServerRendering,
            CreateAnalyzerOptions(propertyValue: null),
            OutputKind.ConsoleApplication);
    }

    [Fact]
    public void ReportsForDirectMappingWithoutLocalRazorFile()
    {
        VerifyCSharpDiagnostic(
            BlazorSsrApplication,
            CreateAnalyzerOptions(propertyValue: null, hasRazorFile: false),
            OutputKind.ConsoleApplication,
            CreateExpectedDiagnostic());
    }

    [Fact]
    public void NoDiagnosticForMappingLibrary()
    {
        VerifyCSharpDiagnostic(
            BlazorSsrApplication,
            CreateAnalyzerOptions(propertyValue: null),
            OutputKind.DynamicallyLinkedLibrary);
    }

    private static DiagnosticResult CreateExpectedDiagnostic() => new()
    {
        Id = DiagnosticId,
        Message = DiagnosticMessage,
        Severity = DiagnosticSeverity.Warning,
    };

    private static AnalyzerOptions CreateAnalyzerOptions(string propertyValue, bool hasRazorFile = true)
    {
        var options = new Dictionary<string, string>();
        if (propertyValue is not null)
        {
            options.Add(DisableThrowNavigationExceptionProperty, propertyValue);
        }

        var additionalFiles = hasRazorFile
            ? ImmutableArray.Create<AdditionalText>(new RazorComponentAdditionalText())
            : ImmutableArray<AdditionalText>.Empty;
        return new AnalyzerOptions(additionalFiles, new TestAnalyzerConfigOptionsProvider(options));
    }

    private sealed class RazorComponentAdditionalText : AdditionalText
    {
        public override string Path => "App.razor";

        public override SourceText GetText(System.Threading.CancellationToken cancellationToken = default) =>
            SourceText.From("<h1>App</h1>");
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
