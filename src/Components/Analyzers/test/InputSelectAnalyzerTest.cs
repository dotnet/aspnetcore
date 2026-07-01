// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class InputSelectAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        => new InputSelectAnalyzer();
    private static readonly string TestDeclarations = @"
namespace Microsoft.AspNetCore.Components.Forms
{
    public abstract class InputSelect<TValue> : ComponentBase
    {
    }
}

namespace Microsoft.AspNetCore.Components
{
    public abstract class ComponentBase { }
}
";

    private static string CreateTestSource(string body, bool enableNullable = false)
    {
        var prefix = enableNullable ? "#nullable enable\n" : string.Empty;
        return prefix + body + TestDeclarations;
    }

    private static DiagnosticResult CreateWarning(string type, int line)
        => new DiagnosticResult
        {
            Id = "BL0015",
            Message = $"InputSelect<{type}> with nullable type requires an empty option element to represent the null value. Add <option value=\"\">Select an option</option> as the first option.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", line, 24)
            }
        };

    // BASIC TYPES

    [Fact]
    public void Analyze_NullableIntProperty_ReportsWarning()
    {
        var test = CreateTestSource(@"
class SampleComponent
{
    public InputSelect<int?> NullableIntegerValue { get; set; }
}");

        VerifyCSharpDiagnostic(test,
            CreateWarning("System.Nullable<System.Int32>", 4));
    }

    [Fact]
    public void Analyze_NullableStringProperty_ReportsWarning()
    {
        var test = CreateTestSource(@"
class SampleComponent
{
    public InputSelect<string?> NullableStringValue { get; set; }
}", true);

        VerifyCSharpDiagnostic(test,
            CreateWarning("System.String", 5));
    }

    [Fact]
    public void Analyze_NonNullableIntProperty_DoesNotReport()
    {
        var test = CreateTestSource(@"
class SampleComponent
{
    public InputSelect<int> NonNullableIntegerValue { get; set; }
}");

        VerifyCSharpDiagnostic(test);
    }

    // ENUM

    [Fact]
    public void Analyze_NullableEnumProperty_ReportsWarning()
    {
        var test = CreateTestSource(@"
enum Status { Active, Inactive }

class SampleComponent
{
    public InputSelect<Status?> NullableStatus { get; set; }
}", true);

        VerifyCSharpDiagnostic(test,
            CreateWarning("Status", 7));
    }

    // ADDITIONAL VALUE TYPES

    [Fact]
    public void Analyze_NullableLongProperty_ReportsWarning()
    {
        var test = CreateTestSource(@"
class SampleComponent
{
    public InputSelect<long?> NullableLongValue { get; set; }
}");

        VerifyCSharpDiagnostic(test,
            CreateWarning("System.Nullable<System.Int64>", 4));
    }

    [Fact]
    public void Analyze_NullableDecimalProperty_ReportsWarning()
    {
        var test = CreateTestSource(@"
class SampleComponent
{
    public InputSelect<decimal?> NullableDecimalValue { get; set; }
}");

        VerifyCSharpDiagnostic(test,
            CreateWarning("System.Nullable<System.Decimal>", 4));
    }

    [Fact]
    public void Analyze_NullableBoolProperty_ReportsWarning()
    {
        var test = CreateTestSource(@"
class SampleComponent
{
    public InputSelect<bool?> NullableBooleanValue { get; set; }
}");

        VerifyCSharpDiagnostic(test,
            CreateWarning("System.Nullable<System.Boolean>", 4));
    }

    [Fact]
    public void Analyze_NullableGuidProperty_ReportsWarning()
    {
        var test = CreateTestSource(@"
class SampleComponent
{
    public InputSelect<System.Guid?> NullableGuidValue { get; set; }
}", true);

        VerifyCSharpDiagnostic(test,
            CreateWarning("System.Nullable<System.Guid>", 5));
    }

    // CUSTOM TYPES

    [Fact]
    public void Analyze_NullableCustomTypeProperty_ReportsWarning()
    {
        var test = CreateTestSource(@"
class Model {}

class SampleComponent
{
    public InputSelect<Model?> NullableModelValue { get; set; }
}", true);

        VerifyCSharpDiagnostic(test,
            CreateWarning("Model", 7));
    }

    // INITIALIZER

    [Fact]
    public void Analyze_InitializerWithNullableIntProperty_ReportsWarning()
    {
        var test = CreateTestSource(@"
class SampleComponent
{
    public InputSelect<int?> NullableIntegerValue { get; set; } = new();
}");

        VerifyCSharpDiagnostic(test,
            CreateWarning("System.Nullable<System.Int32>", 4));
    }

    [Fact]
    public void Analyze_InitializerWithNullableStringProperty_ReportsWarning()
    {
        var test = CreateTestSource(@"
class SampleComponent
{
    public InputSelect<string?> NullableStringValue { get; set; } = new();
}", true);

        VerifyCSharpDiagnostic(test,
            CreateWarning("System.String", 5));
    }

    // MULTIPLE

    [Fact]
    public void Analyze_MultipleNullableProperties_ReportsWarnings()
    {
        var test = CreateTestSource(@"
class SampleComponent
{
    public InputSelect<int?> NullableId { get; set; }
    public InputSelect<string?> NullableName { get; set; }
}", true);

        VerifyCSharpDiagnostic(test,
            CreateWarning("System.Nullable<System.Int32>", 5),
            CreateWarning("System.String", 6));
    }

    [Fact]
    public void Analyze_MultipleDifferentNullableTypes_ReportsAllWarnings()
    {
        var test = CreateTestSource(@"
class SampleComponent
{
    public InputSelect<int?> NullableId { get; set; }
    public InputSelect<string?> NullableName { get; set; }
    public InputSelect<System.DateTime?> NullableDate { get; set; }
}", true);

        VerifyCSharpDiagnostic(test,
            CreateWarning("System.Nullable<System.Int32>", 5),
            CreateWarning("System.String", 6),
            CreateWarning("System.Nullable<System.DateTime>", 7));
    }

    // MIXED

    [Fact]
    public void Analyze_MixedNullableAndNonNullableProperties_ReportsOnlyNullableWarnings()
    {
        var test = CreateTestSource(@"
class SampleComponent
{
    public InputSelect<int> NonNullableId { get; set; }
    public InputSelect<int?> NullableId { get; set; }
}", true);

        VerifyCSharpDiagnostic(test,
            CreateWarning("System.Nullable<System.Int32>", 6));
    }

    // DATETIME

    [Fact]
    public void Analyze_NullableDateTimeProperty_ReportsWarning()
    {
        var test = CreateTestSource(@"
class SampleComponent
{
    public InputSelect<System.DateTime?> NullableDate { get; set; }
}", true);

        VerifyCSharpDiagnostic(test,
            CreateWarning("System.Nullable<System.DateTime>", 5));
    }

    [Fact]
    public void Analyze_NonNullableDateTimeProperty_DoesNotReport()
    {
        var test = CreateTestSource(@"
class SampleComponent
{
    public InputSelect<System.DateTime> NonNullableDate { get; set; }
}");

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void Analyze_NonNullableStringProperty_DoesNotReport()
    {
        var test = CreateTestSource(@"
class SampleComponent
{
    public InputSelect<string> NonNullableStringValue { get; set; }
}", true);

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void Analyze_NullableContextWithNonNullableType_DoesNotReport()
    {
        var test = CreateTestSource(@"
class SampleComponent
{
    public InputSelect<string> NonNullableStringValue { get; set; }
}", true);

        VerifyCSharpDiagnostic(test);
    }
}
