// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class InputSelectAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new InputSelectAnalyzer();

    private static readonly string TestDeclarations = $@"
    namespace Microsoft.AspNetCore.Components.Forms
    {{
        public abstract class InputSelect<TValue> : ComponentBase
        {{
        }}
    }}

    namespace Microsoft.AspNetCore.Components
    {{
        public class ParameterAttribute : System.Attribute
        {{
            public bool CaptureUnmatchedValues {{ get; set; }}
        }}

        public class CascadingParameterAttribute : System.Attribute
        {{
        }}

        public interface IComponent
        {{
        }}

        public abstract class ComponentBase : IComponent
        {{
        }}
    }}
";

    [Fact]
    public void IgnoresNonComponentBaseClasses()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        class TestComponent
        {{
            public InputSelect<int?> MyProperty {{ get; set; }}
        }}
    }}" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void IgnoresInputSelectWithNonNullableValueType()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        class TestComponent : ComponentBase
        {{
            public InputSelect<int> MyProperty {{ get; set; }}
        }}
    }}" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void IgnoresInputSelectWithNonNullableString()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        class TestComponent : ComponentBase
        {{
            public InputSelect<string> MyProperty {{ get; set; }}
        }}
    }}" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void ReportsWarningForInputSelectWithNullableIntValueType()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        class TestComponent : ComponentBase
        {{
            public InputSelect<int?> MyProperty {{ get; set; }}
        }}
    }}" + TestDeclarations;

        var expected = new DiagnosticResult
        {
            Id = "BL0012",
            Message = "InputSelect<System.Nullable<System.Int32>> with nullable type requires an empty option element to represent the null value. Add <option value=\"\">Select an option</option> as the first option.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 7, 32)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void ReportsWarningForInputSelectWithNullableStringValueType()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        class TestComponent : ComponentBase
        {{
            public InputSelect<string?> MyProperty {{ get; set; }}
        }}
    }}" + TestDeclarations;

        var expected = new DiagnosticResult
        {
            Id = "BL0012",
            Message = "InputSelect<System.String> with nullable type requires an empty option element to represent the null value. Add <option value=\"\">Select an option</option> as the first option.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 7, 32)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void ReportsWarningForInputSelectWithNullableValueTypePropertyInitialized()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        class TestComponent : ComponentBase
        {{
            public InputSelect<int?> MyProperty {{ get; set; }} = null;
        }}
    }}" + TestDeclarations;

        var expected = new DiagnosticResult
        {
            Id = "BL0012",
            Message = "InputSelect<System.Nullable<System.Int32>> with nullable type requires an empty option element to represent the null value. Add <option value=\"\">Select an option</option> as the first option.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 7, 32)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void ReportsWarningForMultipleNullableInputSelectProperties()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        class TestComponent : ComponentBase
        {{
            public InputSelect<int?> Id {{ get; set; }}
            public InputSelect<string?> Name {{ get; set; }}
        }}
    }}" + TestDeclarations;

        var expected1 = new DiagnosticResult
        {
            Id = "BL0012",
            Message = "InputSelect<System.Nullable<System.Int32>> with nullable type requires an empty option element to represent the null value. Add <option value=\"\">Select an option</option> as the first option.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 7, 25)
            }
        };

        var expected2 = new DiagnosticResult
        {
            Id = "BL0012",
            Message = "InputSelect<System.String> with nullable type requires an empty option element to represent the null value. Add <option value=\"\">Select an option</option> as the first option.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 8, 28)
            }
        };

        VerifyCSharpDiagnostic(test, expected1, expected2);
    }

    [Fact]
    public void IgnoresInputSelectWithNullableButNonNullValueType()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        class TestComponent : ComponentBase
        {{
            // Non-nullable value type - should NOT trigger warning
            public InputSelect<int> MyIntProperty {{ get; set; }}
            // Non-nullable string - should NOT trigger warning
            public InputSelect<string> MyStringProperty {{ get; set; }}
        }}
    }}" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void ReportsWarningForInputSelectWithNullableStringReferenceType()
    {
        var test = $@"#nullable enable
    namespace ConsoleApplication1
    {{
        class TestComponent : ComponentBase
        {{
            public InputSelect<string?> MyProperty {{ get; set; }}
        }}
    }}" + TestDeclarations;

        var expected = new DiagnosticResult
        {
            Id = "BL0012",
            Message = "InputSelect<System.String?> with nullable type requires an empty option element to represent the null value. Add <option value=\"\">Select an option</option> as the first option.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 8, 32)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void ReportsWarningForInputSelectWithNullableClassReferenceType()
    {
        var test = $@"#nullable enable
    namespace ConsoleApplication1
    {{
        public class MyModel
        {{
            public int Id {{ get; set; }}
        }}

        class TestComponent : ComponentBase
        {{
            public InputSelect<MyModel?> MyProperty {{ get; set; }}
        }}
    }}" + TestDeclarations;

        var expected = new DiagnosticResult
        {
            Id = "BL0012",
            Message = "InputSelect<ConsoleApplication1.MyModel?> with nullable type requires an empty option element to represent the null value. Add <option value=\"\">Select an option</option> as the first option.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 11, 32)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void IgnoresInputSelectWithEnableNullableButNonNullType()
    {
        var test = $@"#nullable enable
    namespace ConsoleApplication1
    {{
        class TestComponent : ComponentBase
        {{
            public InputSelect<string> MyProperty {{ get; set; }}
        }}
    }}" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void ReportsWarningForInputSelectPropertyWithNullableEnumType()
    {
        var test = $@"#nullable enable
    namespace ConsoleApplication1
    {{
        public enum Status
        {{
            Active = 1,
            Inactive = 2
        }}

        class TestComponent : ComponentBase
        {{
            public InputSelect<Status?> MyProperty {{ get; set; }}
        }}
    }}" + TestDeclarations;

        var expected = new DiagnosticResult
        {
            Id = "BL0012",
            Message = "InputSelect<ConsoleApplication1.Status?> with nullable type requires an empty option element to represent the null value. Add <option value=\"\">Select an option</option> as the first option.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 12, 32)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void ReportsWarningForMultipleMixedNullableInputSelectProperties()
    {
        var test = $@"#nullable enable
    namespace ConsoleApplication1
    {{
        public class Category
        {{
            public int Id {{ get; set; }}
        }}

        class TestComponent : ComponentBase
        {{
            public InputSelect<int?> Id {{ get; set; }}
            public InputSelect<string?> Name {{ get; set; }}
            public InputSelect<Category?> Category {{ get; set; }}
        }}
    }}" + TestDeclarations;

        var expected1 = new DiagnosticResult
        {
            Id = "BL0012",
            Message = "InputSelect<System.Nullable<System.Int32>> with nullable type requires an empty option element to represent the null value. Add <option value=\"\">Select an option</option> as the first option.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 11, 25)
            }
        };

        var expected2 = new DiagnosticResult
        {
            Id = "BL0012",
            Message = "InputSelect<System.String?> with nullable type requires an empty option element to represent the null value. Add <option value=\"\">Select an option</option> as the first option.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 12, 28)
            }
        };

        var expected3 = new DiagnosticResult
        {
            Id = "BL0012",
            Message = "InputSelect<ConsoleApplication1.Category?> with nullable type requires an empty option element to represent the null value. Add <option value=\"\">Select an option</option> as the first option.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 13, 32)
            }
        };

        VerifyCSharpDiagnostic(test, expected1, expected2, expected3);
    }

    [Fact]
    public void IgnoresInputSelectWithNullableButHasInitializer()
    {
        var test = $@"#nullable enable
    namespace ConsoleApplication1
    {{
        class TestComponent : ComponentBase
        {{
            // Even with initializer, nullable type should warn
            public InputSelect<string?> MyProperty {{ get; set; }} = new();
        }}
    }}" + TestDeclarations;

        var expected = new DiagnosticResult
        {
            Id = "BL0012",
            Message = "InputSelect<System.String?> with nullable type requires an empty option element to represent the null value. Add <option value=\"\">Select an option</option> as the first option.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 8, 32)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void ReportsWarningForInputSelectWithGuidNullableType()
    {
        var test = $@"#nullable enable
    namespace ConsoleApplication1
    {{
        class TestComponent : ComponentBase
        {{
            public InputSelect<System.Guid?> MyProperty {{ get; set; }}
        }}
    }}" + TestDeclarations;

        var expected = new DiagnosticResult
        {
            Id = "BL0012",
            Message = "InputSelect<System.Nullable<System.Guid>> with nullable type requires an empty option element to represent the null value. Add <option value=\"\">Select an option</option> as the first option.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 7, 32)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void ReportsWarningForInputSelectWithLongNullableType()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        class TestComponent : ComponentBase
        {{
            public InputSelect<long?> MyProperty {{ get; set; }}
        }}
    }}" + TestDeclarations;

        var expected = new DiagnosticResult
        {
            Id = "BL0012",
            Message = "InputSelect<System.Nullable<System.Int64>> with nullable type requires an empty option element to represent the null value. Add <option value=\"\">Select an option</option> as the first option.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 7, 32)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void ReportsWarningForInputSelectWithDecimalNullableType()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        class TestComponent : ComponentBase
        {{
            public InputSelect<decimal?> MyProperty {{ get; set; }}
        }}
    }}" + TestDeclarations;

        var expected = new DiagnosticResult
        {
            Id = "BL0012",
            Message = "InputSelect<System.Nullable<System.Decimal>> with nullable type requires an empty option element to represent the null value. Add <option value=\"\">Select an option</option> as the first option.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 7, 32)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void ReportsWarningForInputSelectWithBoolNullableType()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        class TestComponent : ComponentBase
        {{
            public InputSelect<bool?> MyProperty {{ get; set; }}
        }}
    }}" + TestDeclarations;

        var expected = new DiagnosticResult
        {
            Id = "BL0012",
            Message = "InputSelect<System.Nullable<System.Boolean>> with nullable type requires an empty option element to represent the null value. Add <option value=\"\">Select an option</option> as the first option.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 7, 32)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void IgnoresInputSelectWithDateTimeNonNullableType()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        class TestComponent : ComponentBase
        {{
            public InputSelect<System.DateTime> MyProperty {{ get; set; }}
        }}
    }}" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void ReportsWarningForInputSelectWithDateTimeNullableType()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        class TestComponent : ComponentBase
        {{
            public InputSelect<System.DateTime?> MyProperty {{ get; set; }}
        }}
    }}" + TestDeclarations;

        var expected = new DiagnosticResult
        {
            Id = "BL0012",
            Message = "InputSelect<System.Nullable<System.DateTime>> with nullable type requires an empty option element to represent the null value. Add <option value=\"\">Select an option</option> as the first option.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 7, 32)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void MixedNullableAndNonNullableInputSelectProperties()
    {
        var test = $@"#nullable enable
    namespace ConsoleApplication1
    {{
        class TestComponent : ComponentBase
        {{
            public InputSelect<int> NonNullableInt {{ get; set; }}
            public InputSelect<int?> NullableInt {{ get; set; }}
            public InputSelect<string> NonNullableString {{ get; set; }}
            public InputSelect<string?> NullableString {{ get; set; }}
        }}
    }}" + TestDeclarations;

        var expected1 = new DiagnosticResult
        {
            Id = "BL0012",
            Message = "InputSelect<System.Nullable<System.Int32>> with nullable type requires an empty option element to represent the null value. Add <option value=\"\">Select an option</option> as the first option.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 8, 32)
            }
        };

        var expected2 = new DiagnosticResult
        {
            Id = "BL0012",
            Message = "InputSelect<System.String?> with nullable type requires an empty option element to represent the null value. Add <option value=\"\">Select an option</option> as the first option.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 9, 32)
            }
        };

        VerifyCSharpDiagnostic(test, expected1, expected2);
    }
}