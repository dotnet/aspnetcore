// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class RazorComponentResultParameterAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new RazorComponentResultParameterAnalyzer();

    // Minimal stand-ins for the framework types the analyzer looks up by name. The analyzed compilation does not
    // reference the real Components/Endpoints assemblies, so redeclaring them here avoids type conflicts.
    private static readonly string RazorComponentResultDeclarations = $@"
    namespace {typeof(ParameterAttribute).Namespace}
    {{
        public class {typeof(ParameterAttribute).Name} : System.Attribute
        {{
            public bool CaptureUnmatchedValues {{ get; set; }}
        }}

        public class {typeof(CascadingParameterAttribute).Name} : System.Attribute
        {{
        }}

        public interface {typeof(IComponent).Name}
        {{
        }}
    }}

    namespace Microsoft.AspNetCore.Http.HttpResults
    {{
        public class RazorComponentResult<TComponent>
            where TComponent : {typeof(IComponent).Namespace}.{typeof(IComponent).Name}
        {{
            public RazorComponentResult() {{ }}
            public RazorComponentResult(object parameters) {{ }}
            public RazorComponentResult(System.Collections.Generic.IReadOnlyDictionary<string, object> parameters) {{ }}
        }}
    }}
";

    [Fact]
    public void ReportsWarningWhenParameterNameDoesNotExist()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        using Microsoft.AspNetCore.Http.HttpResults;

        class TestComponent : IComponent
        {{
            [Parameter] public string UserId {{ get; set; }}
        }}

        class TestEndpoints
        {{
            object Get()
            {{
                var result = new RazorComponentResult<TestComponent>(new
                {{
                    AuthorId = 5,
                }});

                return result;
            }}
        }}
    }}" + RazorComponentResultDeclarations;

        var expected = new DiagnosticResult
        {
            Id = "BL0017",
            Message = "Component 'ConsoleApplication1.TestComponent' does not have a [Parameter] property matching the name 'AuthorId'.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 18, 21)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void ReportsWarningForEachUnknownParameterName()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        using Microsoft.AspNetCore.Http.HttpResults;

        class TestComponent : IComponent
        {{
            [Parameter] public string UserId {{ get; set; }}
        }}

        class TestEndpoints
        {{
            object Get()
            {{
                var result = new RazorComponentResult<TestComponent>(new
                {{
                    AuthorId = 5,
                    Missing = 6,
                }});

                return result;
            }}
        }}
    }}" + RazorComponentResultDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = "BL0017",
                Message = "Component 'ConsoleApplication1.TestComponent' does not have a parameter matching the name 'AuthorId'.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 18, 21) }
            },
            new DiagnosticResult
            {
                Id = "BL0017",
                Message = "Component 'ConsoleApplication1.TestComponent' does not have a parameter matching the name 'Missing'.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 19, 21) }
            });
    }

    [Fact]
    public void DoesNotReportWhenAllParameterNamesExist()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        using Microsoft.AspNetCore.Http.HttpResults;

        class TestComponent : IComponent
        {{
            [Parameter] public string UserId {{ get; set; }}
            [Parameter] public int Count {{ get; set; }}
        }}

        class TestEndpoints
        {{
            object Get()
            {{
                return new RazorComponentResult<TestComponent>(new {{ UserId = ""abc"", Count = 5 }});
            }}
        }}
    }}" + RazorComponentResultDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void DoesNotReportForParameterlessConstructor()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        using Microsoft.AspNetCore.Http.HttpResults;

        class TestComponent : IComponent
        {{
            [Parameter] public string UserId {{ get; set; }}
        }}

        class TestEndpoints
        {{
            object Get()
            {{
                return new RazorComponentResult<TestComponent>();
            }}
        }}
    }}" + RazorComponentResultDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void RecognizesInheritedParameters()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        using Microsoft.AspNetCore.Http.HttpResults;

        class BaseComponent : IComponent
        {{
            [Parameter] public string InheritedParam {{ get; set; }}
        }}

        class TestComponent : BaseComponent
        {{
            [Parameter] public string UserId {{ get; set; }}
        }}

        class TestEndpoints
        {{
            object Get()
            {{
                return new RazorComponentResult<TestComponent>(new {{ InheritedParam = ""abc"" }});
            }}
        }}
    }}" + RazorComponentResultDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void DoesNotReportWhenComponentCapturesUnmatchedValues()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        using Microsoft.AspNetCore.Http.HttpResults;

        class TestComponent : IComponent
        {{
            [Parameter(CaptureUnmatchedValues = true)]
            public System.Collections.Generic.Dictionary<string, object> Attributes {{ get; set; }}
        }}

        class TestEndpoints
        {{
            object Get()
            {{
                return new RazorComponentResult<TestComponent>(new {{ AnythingGoes = 5 }});
            }}
        }}
    }}" + RazorComponentResultDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void IgnoresDictionaryParameters()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        using Microsoft.AspNetCore.Http.HttpResults;

        class TestComponent : IComponent
        {{
            [Parameter] public string UserId {{ get; set; }}
        }}

        class TestEndpoints
        {{
            object Get(System.Collections.Generic.IReadOnlyDictionary<string, object> parameters)
            {{
                return new RazorComponentResult<TestComponent>(parameters);
            }}
        }}
    }}" + RazorComponentResultDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void IgnoresUnrelatedObjectCreations()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};

        class TestComponent : IComponent
        {{
            [Parameter] public string UserId {{ get; set; }}
        }}

        class NotAResult<TComponent>
        {{
            public NotAResult(object parameters) {{ }}
        }}

        class TestEndpoints
        {{
            object Get()
            {{
                return new NotAResult<TestComponent>(new {{ AuthorId = 5 }});
            }}
        }}
    }}" + RazorComponentResultDeclarations;

        VerifyCSharpDiagnostic(test);
    }
}
