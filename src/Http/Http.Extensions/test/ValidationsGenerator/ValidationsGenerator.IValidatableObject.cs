// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.ValidationsGenerator.Tests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    [Fact]
    public async Task CanValidateIValidatableObject()
    {
        var source = """
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();
builder.Services.AddSingleton<IRangeService, RangeService>();
builder.Services.AddValidation();

var app = builder.Build();

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
            yield return new ValidationResult($"The field {nameof(Value1)} must be between {minimum} and {maximum}.", [nameof(Value1)]);
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
        await VerifyEndpoint(compilation, "/validatable-object", async (endpoint, serviceProvider) =>
        {
            await ValidateMethodCalledIfPropertyValidationsFail();
            await ValidateForSubtypeInvokedFirst();
            await ValidateForTopLevelInvoked();

            async Task ValidateMethodCalledIfPropertyValidationsFail()
            {
                var httpContext = CreateHttpContextWithPayload("""
                {
                    "Value1": 5,
                    "Value2": "",
                    "SubType": {
                        "Value3": "foo",
                        "RequiredProperty": "",
                        "StringWithLength": ""
                    }
                }
                """, serviceProvider);

                await endpoint.RequestDelegate(httpContext);

                var problemDetails = await AssertBadRequest(httpContext);
                Assert.Collection(problemDetails.Errors,
                    error =>
                    {
                        Assert.Equal("Value2", error.Key);
                        Assert.Collection(error.Value,
                            msg => Assert.Equal("The Value2 field is required.", msg));
                    },
                    error =>
                    {
                        Assert.Equal("SubType.RequiredProperty", error.Key);
                        Assert.Equal("The RequiredProperty field is required.", error.Value.Single());
                    },
                    error =>
                    {
                        Assert.Equal("SubType.Value3", error.Key);
                        Assert.Equal("The field ValidatableSubType must be 'some-value'.", error.Value.Single());
                    },
                    error =>
                    {
                        Assert.Equal("Value1", error.Key);
                        Assert.Equal("The field Value1 must be between 10 and 100.", error.Value.Single());
                    });
            }

            async Task ValidateForSubtypeInvokedFirst()
            {
                var httpContext = CreateHttpContextWithPayload("""
                {
                    "Value1": 5,
                    "Value2": "test@test.com",
                    "SubType": {
                        "Value3": "foo",
                        "RequiredProperty": "some-value-2",
                        "StringWithLength": "element"
                    }
                }
                """, serviceProvider);

                await endpoint.RequestDelegate(httpContext);

                var problemDetails = await AssertBadRequest(httpContext);
                Assert.Collection(problemDetails.Errors,
                    error =>
                    {
                        Assert.Equal("SubType.Value3", error.Key);
                        Assert.Equal("The field ValidatableSubType must be 'some-value'.", error.Value.Single());
                    },
                    error =>
                    {
                        Assert.Equal("Value1", error.Key);
                        Assert.Equal("The field Value1 must be between 10 and 100.", error.Value.Single());
                    });
            }

            async Task ValidateForTopLevelInvoked()
            {
                var httpContext = CreateHttpContextWithPayload("""
                {
                    "Value1": 5,
                    "Value2": "test@test.com",
                    "SubType": {
                        "Value3": "some-value",
                        "RequiredProperty": "some-value-2",
                        "StringWithLength": "element"
                    }
                }
                """, serviceProvider);

                await endpoint.RequestDelegate(httpContext);

                var problemDetails = await AssertBadRequest(httpContext);
                Assert.Collection(problemDetails.Errors,
                    error =>
                    {
                        Assert.Equal("Value1", error.Key);
                        Assert.Equal("The field Value1 must be between 10 and 100.", error.Value.Single());
                    });
            }
        });
    }
}
