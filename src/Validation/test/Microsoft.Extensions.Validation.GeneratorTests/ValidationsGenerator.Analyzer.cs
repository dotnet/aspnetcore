// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Validation.GeneratorTests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    private const string AnalyzerPreamble = """
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Validation;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();
builder.Services.AddValidation();
var app = builder.Build();
app.Run();
""";

    [Fact]
    public async Task ReportsInaccessibleValidatableType_ForPrivateNestedType()
    {
        var source = AnalyzerPreamble + """

public partial class Home
{
    [ValidatableType]
    private class PrivateModel
    {
        public TheChild Child { get; } = new();
        public class TheChild { [Required] public string? Name { get; set; } }
    }
}
""";
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("ASP0032", diagnostic.Id);
        Assert.Contains("PrivateModel", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ReportsInaccessibleValidatableType_ForFileLocalType()
    {
        var source = AnalyzerPreamble + """

[ValidatableType]
file class FileLocalModel
{
    [Required] public string? Name { get; set; }
}
""";
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("ASP0032", diagnostic.Id);
    }

    [Fact]
    public async Task DoesNotReportInaccessibleValidatableType_ForPublicOrInternalType()
    {
        var source = AnalyzerPreamble + """

[ValidatableType]
public class PublicModel
{
    [Required] public string? Name { get; set; }
}

[ValidatableType]
internal class InternalModel
{
    [Required] public string? Name { get; set; }
}
""";
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ReportsNonPublicProperty_WithValidationAttribute()
    {
        var source = AnalyzerPreamble + """

[ValidatableType]
public class PublicWithInternalChildPropModel
{
    public TheChild Child { get; } = new();
    public class TheChild
    {
        [Required] internal string? Name { get; set; }
    }
}
""";
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("ASP0034", diagnostic.Id);
        Assert.Contains("Name", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ReportsNonPublicProperty_WhoseTypeIsValidatable()
    {
        var source = AnalyzerPreamble + """

[ValidatableType]
public class PublicWithInternalChildModel
{
    internal TheChild Child { get; } = new();
    public class TheChild { [Required] public string? Name { get; set; } }
}
""";
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("ASP0034", diagnostic.Id);
        Assert.Contains("Child", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ReportsTypeNotInGraph_ForConcreteTypeImplementingValidatableInterface()
    {
        var source = AnalyzerPreamble + """

[ValidatableType]
public class ChildWithIValidatableObject
{
    public IChild Child { get; } = new TheChild();
    public interface IChild : IValidatableObject
    {
        public string? Name { get; set; }
    }
    public class TheChild : IChild
    {
        public string? Name { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => [new("Custom", [nameof(Name)])];
    }
}
""";
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);
        var diagnostic = Assert.Single(diagnostics.Where(d => d.Id == "ASP0035"));
        Assert.Contains("TheChild", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task DoesNotReportTypeNotInGraph_WhenConcreteTypeIsMarkedValidatable()
    {
        var source = AnalyzerPreamble + """

[ValidatableType]
public class ChildWithClassWithTrippleAttribute
{
    public IChild Child { get; } = new TheChild();
    public interface IChild
    {
        [Required] public string? Name { get; set; }
    }

    [ValidatableType]
    public class TheChild : IChild
    {
        [Required] public string? Name { get; set; }
    }
}
""";
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ReportsForValidatableType_WithoutAddValidationCall()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

public partial class Home
{
    [ValidatableType]
    private class PrivateModel
    {
        [Required] public string? Name { get; set; }
    }
}
""";
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);
        Assert.Single(diagnostics.Where(d => d.Id == "ASP0032"));
    }
}

