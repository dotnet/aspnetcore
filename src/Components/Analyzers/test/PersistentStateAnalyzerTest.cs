// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class PersistentStateAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new PersistentStateAnalyzer();

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

        public abstract class CascadingParameterAttributeBase : System.Attribute
        {{
        }}

        public class PersistentStateAttribute : CascadingParameterAttributeBase
        {{
            public RestoreBehavior RestoreBehavior {{ get; set; }}
            public bool AllowUpdates {{ get; set; }}
        }}

        public enum RestoreBehavior
        {{
            Default,
            SkipInitialValue,
            SkipLastSnapshot
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
    public void IgnoresPropertiesWithoutPersistentStateAttribute()
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
    public void IgnoresPersistentStateWithoutInitializer()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TestComponent : ComponentBase
        {{
            [PersistentState] public string MyProperty {{ get; set; }}
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
            [PersistentState] public string MyProperty {{ get; set; }} = ""initial-value"";
        }}
    }}" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void ReportsWarningForPersistentStateWithInitializer()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TestComponent : ComponentBase
        {{
            [PersistentState] public string MyProperty {{ get; set; }} = ""initial-value"";
        }}
    }}" + TestDeclarations;

        var expected = new DiagnosticResult
        {
            Id = "BL0009",
            Message = "Property 'ConsoleApplication1.TestComponent.MyProperty' has [PersistentState] and a property initializer. This can be overwritten during parameter binding.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 7, 45)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void ReportsWarningForPersistentStateWithObjectInitializer()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TestComponent : ComponentBase
        {{
            [PersistentState] public InputModel Input {{ get; set; }} = new InputModel();
        }}

        class InputModel
        {{
            public string Value {{ get; set; }} = """";
        }}
    }}" + TestDeclarations;

        var expected = new DiagnosticResult
        {
            Id = "BL0009",
            Message = "Property 'ConsoleApplication1.TestComponent.Input' has [PersistentState] and a property initializer. This can be overwritten during parameter binding.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 7, 49)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void IgnoresPersistentStateWithNullInitializer()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TestComponent : ComponentBase
        {{
            [PersistentState] public string MyProperty {{ get; set; }} = null;
        }}
    }}" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void IgnoresPersistentStateWithNullForgivingInitializer()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TestComponent : ComponentBase
        {{
            [PersistentState] public string MyProperty {{ get; set; }} = null!;
        }}
    }}" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void IgnoresPersistentStateWithDefaultInitializer()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TestComponent : ComponentBase
        {{
            [PersistentState] public string MyProperty {{ get; set; }} = default;
        }}
    }}" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void IgnoresPersistentStateWithDefaultForgivingInitializer()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TestComponent : ComponentBase
        {{
            [PersistentState] public string MyProperty {{ get; set; }} = default!;
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
            [PersistentState] public string MyProperty {{ get; set; }} = ""initial-value"";
        }}
    }}" + TestDeclarations;

        var expected = new DiagnosticResult
        {
            Id = "BL0009",
            Message = "Property 'ConsoleApplication1.TestComponent.MyProperty' has [PersistentState] and a property initializer. This can be overwritten during parameter binding.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 11, 45)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void WorksWithPersistentStateAttributeWithParameters()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TestComponent : ComponentBase
        {{
            [PersistentState(RestoreBehavior = RestoreBehavior.SkipInitialValue, AllowUpdates = true)] 
            public string MyProperty {{ get; set; }} = ""initial-value"";
        }}
    }}" + TestDeclarations;

        var expected = new DiagnosticResult
        {
            Id = "BL0009",
            Message = "Property 'ConsoleApplication1.TestComponent.MyProperty' has [PersistentState] and a property initializer. This can be overwritten during parameter binding.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 8, 27)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }
}