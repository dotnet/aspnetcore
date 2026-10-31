// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Validation.GeneratorTests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    [Fact]
    public async Task CanValidateTypeWithParsableProperties()
    {
        // Arrange
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/complex-type-with-parsable-properties", (ComplexTypeWithParsableProperties complexType) => Results.Ok("Passed"!));

app.Run();

public class ComplexTypeWithParsableProperties
{
    [RegularExpression("^((?!00000000-0000-0000-0000-000000000000).)*$", ErrorMessage = "Cannot use default Guid")]
    public Guid? GuidWithRegularExpression { get; set; } = default;

    [Required]
    public TimeOnly? TimeOnlyWithRequiredValue { get; set; } = TimeOnly.FromDateTime(DateTime.UtcNow);

    [Url(ErrorMessage = "The field Url must be a valid URL.")]
    public string? Url { get; set; } = "https://example.com";

    [Required]
    [Range(typeof(DateOnly), "2023-01-01", "2025-12-31", ErrorMessage = "Date must be between 2023-01-01 and 2025-12-31")]
    public DateOnly? DateOnlyWithRange { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Range(typeof(DateTime), "2023-01-01", "2025-12-31", ErrorMessage = "DateTime must be between 2023-01-01 and 2025-12-31")]
    public DateTime? DateTimeWithRange { get; set; } = DateTime.UtcNow;

    [Range(typeof(decimal), "0.1", "100.5", ErrorMessage = "Amount must be between 0.1 and 100.5", ParseLimitsInInvariantCulture = true)]
    public decimal? DecimalWithRange { get; set; } = 50.5m;

    [Range(0, 12, ErrorMessage = "Hours must be between 0 and 12")]
    public TimeSpan? TimeSpanWithHourRange { get; set; } = TimeSpan.FromHours(12);

    [Range(0, 1, ErrorMessage = "Boolean value must be 0 or 1")]
    public bool BooleanWithRange { get; set; } = true;

    [RegularExpression(@"^\d+\.\d+\.\d+$", ErrorMessage = "Must be a valid version number (e.g. 1.0.0)")]
    public Version? VersionWithRegex { get; set; } = new Version(1, 0, 0);
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/complex-type-with-parsable-properties", async (endpoint, serviceProvider) =>
        {
            var payload = """
            {
              "TimeOnlyWithRequiredValue": null,
              "IntWithRange": 150,
              "StringWithLength": "AB",
              "Email": "invalid-email",
              "Url": "invalid-url",
              "DateOnlyWithRange": "2026-05-01",
              "DateTimeWithRange": "2026-05-01T10:00:00",
              "DecimalWithRange": "150.75",
              "TimeSpanWithHourRange": "22:00:00",
              "VersionWithRegex": "1.0",
              "EnumProperty": "Invalid"
            }
            """;
            var context = CreateHttpContextWithPayload(payload, serviceProvider);

            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);

            // Assert on each error with Assert.Collection
            Assert.Collection(problemDetails.Errors.OrderBy(kvp => kvp.Key),
                error =>
                {
                    Assert.Equal("DateOnlyWithRange", error.Key);
                    Assert.Contains("Date must be between 2023-01-01 and 2025-12-31", error.Value);
                },
                error =>
                {
                    Assert.Equal("DateTimeWithRange", error.Key);
                    Assert.Contains("DateTime must be between 2023-01-01 and 2025-12-31", error.Value);
                },
                error =>
                {
                    Assert.Equal("DecimalWithRange", error.Key);
                    Assert.Contains("Amount must be between 0.1 and 100.5", error.Value);
                },
                error =>
                {
                    Assert.Equal("TimeOnlyWithRequiredValue", error.Key);
                    Assert.Contains("The TimeOnlyWithRequiredValue field is required.", error.Value);
                },
                error =>
                {
                    Assert.Equal("TimeSpanWithHourRange", error.Key);
                    Assert.Contains("Hours must be between 0 and 12", error.Value);
                },
                error =>
                {
                    Assert.Equal("Url", error.Key);
                    Assert.Contains("The field Url must be a valid URL.", error.Value);
                },
                error =>
                {
                    Assert.Equal("VersionWithRegex", error.Key);
                    Assert.Contains("Must be a valid version number (e.g. 1.0.0)", error.Value);
                }
            );
        });
    }
}
