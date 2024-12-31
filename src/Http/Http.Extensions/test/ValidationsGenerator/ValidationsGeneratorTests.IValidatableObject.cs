// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestsBase
{
    [Fact]
    public async Task CanValidateIValidatableObject()
    {
        var source = """
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddSingleton<IRangeService, RangeService>();

var app = builder.Build();

app.Conventions.WithValidation();

app.MapPost("/validatable-object", (ComplexValidatableType model) => Results.Ok());

app.Run();

public class ComplexValidatableType: IValidatableObject
{
    [Display(Name = "Value 1")]
    public int Value1 { get; set; }

    [EmailAddress]
    [Required]
    public required string Value2 { get; set; } = "test@example.com";

    public ValidatableSubType SubType { get; set; } = new ValidatableSubType();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var rangeService = (IRangeService?)validationContext.GetService(typeof(IRangeService));
        var minimum = rangeService?.GetMinimum();
        var maximum = rangeService?.GetMaximum();
        if (Value1 < minimum || Value1 > maximum)
        {
            yield return new ValidationResult($"The field {validationContext.DisplayName} must be between {minimum} and {maximum}.", [nameof(Value1)]);
        }
    }
}

public class SubType
{
    [Required]
    public string RequiredProperty { get; set; } = "some-value";

    [StringLength(10)]
    public string? StringWithLength { get; set; }
}

public class ValidatableSubType : SubType, IValidatableObject
{
    public string Value3 { get; set; } = "some-value";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Value3 != "some-value")
        {
            yield return new ValidationResult($"The field {validationContext.DisplayName} must be 'some-value'.", [nameof(Value3)]);
        }
    }
}

public interface IRangeService
{
    int GetMinimum();
    int GetMaximum();
}

public class RangeService : IRangeService
{
    public int GetMinimum() => 10;
    public int GetMaximum() => 100;
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, async client =>
        {
            await ValidateMethodSkippedIfPropertyValidationsFail(client);
            await ValidateForSubtypeInvokedFirst(client);
            await ValidateForTopLevelInvoked(client);

            static async Task ValidateMethodSkippedIfPropertyValidationsFail(HttpClient client)
            {
                var payload = """
                {
                    "Value1": 5,
                    "Value2": "",
                    "SubType": {
                        "Value3": "foo",
                        "RequiredProperty": "",
                        "StringWithLength": ""
                    }
                }
                """;
                var content = new StringContent(payload, new MediaTypeHeaderValue("application/json"));
                var response = await client.PostAsync("/validatable-object", content);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                var problemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
                Assert.Collection(problemDetails.Errors,
                    error =>
                    {
                        Assert.Equal("Value2", error.Key);
                        Assert.Collection(error.Value,
                        error =>
                        {
                            Assert.Equal("The Value2 field is not a valid e-mail address.", error);

                        },
                        error =>
                        {
                            Assert.Equal("The Value2 field is required.", error);
                        });
                    },
                    error =>
                    {
                        Assert.Equal("SubType.RequiredProperty", error.Key);
                        Assert.Equal("The RequiredProperty field is required.", error.Value.Single());
                    });
            }

            static async Task ValidateForSubtypeInvokedFirst(HttpClient client)
            {
                var payload = """
                {
                    "Value1": 5,
                    "Value2": "test@test.com",
                    "SubType": {
                        "Value3": "foo",
                        "RequiredProperty": "some-value-2",
                        "StringWithLength": "element"
                    }
                }
                """;
                var content = new StringContent(payload, new MediaTypeHeaderValue("application/json"));
                var response = await client.PostAsync("/validatable-object", content);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                var problemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
                Assert.Collection(problemDetails.Errors,
                    error =>
                    {
                        Assert.Equal("SubType.Value3", error.Key);
                        Assert.Equal("The field ValidatableSubType must be 'some-value'.", error.Value.Single());
                    });
            }

            static async Task ValidateForTopLevelInvoked(HttpClient client)
            {
                var payload = """
                {
                    "Value1": 5,
                    "Value2": "test@test.com",
                    "SubType": {
                        "Value3": "some-value",
                        "RequiredProperty": "some-value-2",
                        "StringWithLength": "element"
                    }
                }
                """;
                var content = new StringContent(payload, new MediaTypeHeaderValue("application/json"));
                var response = await client.PostAsync("/validatable-object", content);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                var problemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
                Assert.Collection(problemDetails.Errors,
                    error =>
                    {
                        Assert.Equal("Value1", error.Key);
                        Assert.Equal("The field ComplexValidatableType must be between 10 and 100.", error.Value.Single());
                    });
            }
        });
    }
}
