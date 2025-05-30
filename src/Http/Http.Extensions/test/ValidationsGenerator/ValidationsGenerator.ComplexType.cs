// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.ValidationsGenerator.Tests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    [Fact]
    public async Task CanValidateComplexTypes()
    {
        // Arrange
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/complex-type", (ComplexType complexType) => Results.Ok("Passed"!));

app.Run();

public class ComplexType
{
    [Range(10, 100)]
    public int IntegerWithRange { get; set; } = 10;

    [Range(10, 100), Display(Name = "Valid identifier")]
    public int IntegerWithRangeAndDisplayName { get; set; } = 50;

    [Required]
    public SubType PropertyWithMemberAttributes { get; set; } = new SubType("some-value", default);

    public SubType PropertyWithoutMemberAttributes { get; set; } = new SubType("some-value", default);

    public SubTypeWithInheritance PropertyWithInheritance { get; set; } = new SubTypeWithInheritance("some-value", default);

    // Nullable to validate https://github.com/dotnet/aspnetcore/issues/61737
    public List<SubType>? ListOfSubTypes { get; set; } = [];

    [DerivedValidation(ErrorMessage = "Value must be an even number")]
    public int IntegerWithDerivedValidationAttribute { get; set; }

    [CustomValidation(typeof(CustomValidators), nameof(CustomValidators.Validate))]
    public int IntegerWithCustomValidation { get; set; } = 0;

    [DerivedValidation, Range(10, 100)]
    public int PropertyWithMultipleAttributes { get; set; } = 10;
}

public class DerivedValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is int number && number % 2 == 0;
}

public class SubType(string? requiredProperty, string? stringWithLength)
{
    [Required]
    public string RequiredProperty { get; } = requiredProperty;

    [StringLength(10)]
    public string? StringWithLength { get; } = stringWithLength;
}

public class SubTypeWithInheritance(string? requiredProperty, string? stringWithLength) : SubType(requiredProperty, stringWithLength)
{
    [EmailAddress]
    public string? EmailString { get; set; }
}

public static class CustomValidators
{
    public static ValidationResult Validate(int number, ValidationContext validationContext)
    {
        var parent = (ComplexType)validationContext.ObjectInstance;

        if (parent.IntegerWithRange == number)
        {
            return new ValidationResult(
                "Can't use the same number value in two properties on the same class.",
                new[] { validationContext.MemberName });
        }

        return ValidationResult.Success;
    }
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/complex-type", async (endpoint, serviceProvider) =>
        {
            await InvalidIntegerWithRangeProducesError(endpoint);
            await InvalidIntegerWithRangeAndDisplayNameProducesError(endpoint);
            await MissingRequiredSubtypePropertyProducesError(endpoint);
            await InvalidRequiredSubtypePropertyProducesError(endpoint);
            await InvalidSubTypeWithInheritancePropertyProducesError(endpoint);
            await InvalidListOfSubTypesProducesError(endpoint);
            await InvalidPropertyWithDerivedValidationAttributeProducesError(endpoint);
            await InvalidPropertyWithMultipleAttributesProducesError(endpoint);
            await InvalidPropertyWithCustomValidationProducesError(endpoint);
            await ValidInputProducesNoWarnings(endpoint);

            async Task InvalidIntegerWithRangeProducesError(Endpoint endpoint)
            {

                var payload = """
                {
                    "IntegerWithRange": 5
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("integerWithRange", kvp.Key);
                    Assert.Equal("The field integerWithRange must be between 10 and 100.", kvp.Value.Single());
                });
            }

            async Task InvalidIntegerWithRangeAndDisplayNameProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "IntegerWithRangeAndDisplayName": 5
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("integerWithRangeAndDisplayName", kvp.Key);
                    Assert.Equal("The field Valid identifier must be between 10 and 100.", kvp.Value.Single());
                });
            }

            async Task MissingRequiredSubtypePropertyProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "PropertyWithMemberAttributes": null
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("propertyWithMemberAttributes", kvp.Key);
                    Assert.Equal("The propertyWithMemberAttributes field is required.", kvp.Value.Single());
                });
            }

            async Task InvalidRequiredSubtypePropertyProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "PropertyWithMemberAttributes": {
                        "RequiredProperty": "",
                        "StringWithLength": "way-too-long"
                    }
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors,
                kvp =>
                {
                    Assert.Equal("propertyWithMemberAttributes.requiredProperty", kvp.Key);
                    Assert.Equal("The requiredProperty field is required.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("propertyWithMemberAttributes.stringWithLength", kvp.Key);
                    Assert.Equal("The field stringWithLength must be a string with a maximum length of 10.", kvp.Value.Single());
                });
            }

            async Task InvalidSubTypeWithInheritancePropertyProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "PropertyWithInheritance": {
                        "RequiredProperty": "",
                        "StringWithLength": "way-too-long",
                        "EmailString": "not-an-email"
                    }
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors,
                kvp =>
                {
                    Assert.Equal("propertyWithInheritance.emailString", kvp.Key);
                    Assert.Equal("The emailString field is not a valid e-mail address.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("propertyWithInheritance.requiredProperty", kvp.Key);
                    Assert.Equal("The requiredProperty field is required.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("propertyWithInheritance.stringWithLength", kvp.Key);
                    Assert.Equal("The field stringWithLength must be a string with a maximum length of 10.", kvp.Value.Single());
                });
            }

            async Task InvalidListOfSubTypesProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "ListOfSubTypes": [
                        {
                            "RequiredProperty": "",
                            "StringWithLength": "way-too-long"
                        },
                        {
                            "RequiredProperty": "valid",
                            "StringWithLength": "way-too-long"
                        },
                        {
                            "RequiredProperty": "valid",
                            "StringWithLength": "valid"
                        }
                    ]
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors,
                kvp =>
                {
                    Assert.Equal("listOfSubTypes[0].requiredProperty", kvp.Key);
                    Assert.Equal("The requiredProperty field is required.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("listOfSubTypes[0].stringWithLength", kvp.Key);
                    Assert.Equal("The field stringWithLength must be a string with a maximum length of 10.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("listOfSubTypes[1].stringWithLength", kvp.Key);
                    Assert.Equal("The field stringWithLength must be a string with a maximum length of 10.", kvp.Value.Single());
                });
            }

            async Task InvalidPropertyWithDerivedValidationAttributeProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "IntegerWithDerivedValidationAttribute": 5
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("integerWithDerivedValidationAttribute", kvp.Key);
                    Assert.Equal("Value must be an even number", kvp.Value.Single());
                });
            }

            async Task InvalidPropertyWithMultipleAttributesProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "PropertyWithMultipleAttributes": 5
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("propertyWithMultipleAttributes", kvp.Key);
                    Assert.Collection(kvp.Value,
                    error =>
                    {
                        Assert.Equal("The field propertyWithMultipleAttributes is invalid.", error);
                    },
                    error =>
                    {
                        Assert.Equal("The field propertyWithMultipleAttributes must be between 10 and 100.", error);
                    });
                });
            }

            async Task InvalidPropertyWithCustomValidationProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "IntegerWithRange": 42,
                    "IntegerWithCustomValidation": 42
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("integerWithCustomValidation", kvp.Key);
                    var error = Assert.Single(kvp.Value);
                    Assert.Equal("Can't use the same number value in two properties on the same class.", error);
                });
            }

            async Task ValidInputProducesNoWarnings(Endpoint endpoint)
            {
                var payload = """
                {
                    "IntegerWithRange": 50,
                    "IntegerWithRangeAndDisplayName": 50,
                    "PropertyWithMemberAttributes": {
                        "RequiredProperty": "valid",
                        "StringWithLength": "valid"
                    },
                    "PropertyWithoutMemberAttributes": {
                        "RequiredProperty": "valid",
                        "StringWithLength": "valid"
                    },
                    "PropertyWithInheritance": {
                        "RequiredProperty": "valid",
                        "StringWithLength": "valid",
                        "EmailString": "test@example.com"
                    },
                    "ListOfSubTypes": [],
                    "IntegerWithDerivedValidationAttribute": 2,
                    "IntegerWithCustomValidation": 0,
                    "PropertyWithMultipleAttributes": 12
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);
                await endpoint.RequestDelegate(context);

                Assert.Equal(200, context.Response.StatusCode);
            }
        });
    }
}
