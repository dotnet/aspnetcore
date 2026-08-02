// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class SupplyParameterFromFormAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new SupplyParameterFromFormAnalyzer();

    private static readonly string TestDeclarations = $@"
    namespace {typeof(ParameterAttribute).Namespace}
    {{
        public class {typeof(ParameterAttribute).Name} : System.Attribute
        {{
            public bool CaptureUnmatchedValues {{ get; set; }}
        }}

        public class {typeof(CascadingParameterAttribute).Name} : System.Attribute
        {{
        }}

        public class SupplyParameterFromFormAttribute : System.Attribute
        {{
            public string Name {{ get; set; }}
            public string FormName {{ get; set; }}
        }}

        public interface {typeof(IComponent).Name}
        {{
        }}

        public abstract class ComponentBase : {typeof(IComponent).Name}
        {{
        }}
    }}
";

    [Fact]
    public void IgnoresPropertiesWithoutSupplyParameterFromFormAttribute()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TestComponent : ComponentBase
        {{
            public string MyProperty {{ get; set; }} = ""initial-value"";
        }}
    }}" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void IgnoresSupplyParameterFromFormWithoutInitializer()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TestComponent : ComponentBase
        {{
            [SupplyParameterFromForm] public string MyProperty {{ get; set; }}
        }}
    }}" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void IgnoresNonComponentBaseClasses()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class NotAComponent
        {{
            [SupplyParameterFromForm] public string MyProperty {{ get; set; }} = ""initial-value"";
        }}
    }}" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void ReportsWarningForSupplyParameterFromFormWithInitializer()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TestComponent : ComponentBase
        {{
            [SupplyParameterFromForm] public string MyProperty {{ get; set; }} = ""initial-value"";
        }}
    }}" + TestDeclarations;

        var expected = new DiagnosticResult
        {
            Id = "BL0008",
            Message = "Property 'ConsoleApplication1.TestComponent.MyProperty' has [SupplyParameterFromForm] and a property initializer. This can be overwritten with null during form posts.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 7, 53)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void ReportsWarningForSupplyParameterFromFormWithObjectInitializer()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TestComponent : ComponentBase
        {{
            [SupplyParameterFromForm] public InputModel Input {{ get; set; }} = new InputModel();
        }}

        class InputModel
        {{
            public string Value {{ get; set; }} = """";
        }}
    }}" + TestDeclarations;

        var expected = new DiagnosticResult
        {
            Id = "BL0008",
            Message = "Property 'ConsoleApplication1.TestComponent.Input' has [SupplyParameterFromForm] and a property initializer. This can be overwritten with null during form posts.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 7, 57)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void IgnoresSupplyParameterFromFormWithNullInitializer()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TestComponent : ComponentBase
        {{
            [SupplyParameterFromForm] public string MyProperty {{ get; set; }} = null;
        }}
    }}" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void IgnoresSupplyParameterFromFormWithNullForgivingInitializer()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TestComponent : ComponentBase
        {{
            [SupplyParameterFromForm] public string MyProperty {{ get; set; }} = null!;
        }}
    }}" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void IgnoresSupplyParameterFromFormWithDefaultInitializer()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TestComponent : ComponentBase
        {{
            [SupplyParameterFromForm] public string MyProperty {{ get; set; }} = default;
        }}
    }}" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void IgnoresSupplyParameterFromFormWithDefaultForgivingInitializer()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TestComponent : ComponentBase
        {{
            [SupplyParameterFromForm] public string MyProperty {{ get; set; }} = default!;
        }}
    }}" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void WorksWithInheritedComponentBase()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class BaseComponent : ComponentBase
        {{
        }}

        class TestComponent : BaseComponent
        {{
            [SupplyParameterFromForm] public string MyProperty {{ get; set; }} = ""initial-value"";
        }}
    }}" + TestDeclarations;

        var expected = new DiagnosticResult
        {
            Id = "BL0008",
            Message = "Property 'ConsoleApplication1.TestComponent.MyProperty' has [SupplyParameterFromForm] and a property initializer. This can be overwritten with null during form posts.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 11, 53)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }
}