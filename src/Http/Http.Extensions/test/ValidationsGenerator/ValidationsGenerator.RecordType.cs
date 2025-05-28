// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.ValidationsGenerator.Tests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    [Fact]
    public async Task CanValidateRecordTypes()
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

app.MapPost("/validatable-record", (ValidatableRecord validatableRecord) => Results.Ok("Passed"!));

app.Run();

public class DerivedValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is int number && number % 2 == 0;
}

public record SubType([Required] string RequiredProperty = "some-value", [StringLength(10)] string? StringWithLength = default);

public record SubTypeWithInheritance([EmailAddress] string? EmailString, string RequiredProperty, string? StringWithLength) : SubType(RequiredProperty, StringWithLength);

public record SubTypeWithoutConstructor
{
    [Required]
    public string RequiredProperty { get; set; } = "some-value";

    [StringLength(10)]
    public string? StringWithLength { get; set; }
}

public static class CustomValidators
{
    public static ValidationResult Validate(int number, ValidationContext validationContext)
    {
        var parent = (ValidatableRecord)validationContext.ObjectInstance;
        if (number == parent.IntegerWithRange)
        {
            return new ValidationResult(
                "Can't use the same number value in two properties on the same class.",
                new[] { validationContext.MemberName });
        }

        return ValidationResult.Success;
    }
}

public record ValidatableRecord(
    [Range(10, 100)]
    int IntegerWithRange = 10,
    [Range(10, 100), Display(Name = "Valid identifier")]
    int IntegerWithRangeAndDisplayName = 50,
    SubType PropertyWithMemberAttributes = default,
    SubType PropertyWithoutMemberAttributes = default,
    SubTypeWithInheritance PropertyWithInheritance = default,
    SubTypeWithoutConstructor PropertyOfSubtypeWithoutConstructor = default,
    List<SubType> ListOfSubTypes = default,
    [DerivedValidation(ErrorMessage = "Value must be an even number")]
    int IntegerWithDerivedValidationAttribute = 0,
    [CustomValidation(typeof(CustomValidators), nameof(CustomValidators.Validate))]
    int IntegerWithCustomValidation = 0,
    [DerivedValidation, Range(10, 100)]
    int PropertyWithMultipleAttributes = 10
);
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/validatable-record", async (endpoint, serviceProvider) =>
        {
            await InvalidIntegerWithRangeProducesError(endpoint);
            await InvalidIntegerWithRangeAndDisplayNameProducesError(endpoint);
            await InvalidRequiredSubtypePropertyProducesError(endpoint);
            await InvalidSubTypeWithInheritancePropertyProducesError(endpoint);
            await InvalidListOfSubTypesProducesError(endpoint);
            await InvalidPropertyWithDerivedValidationAttributeProducesError(endpoint);
            await InvalidPropertyWithMultipleAttributesProducesError(endpoint);
            await InvalidPropertyWithCustomValidationProducesError(endpoint);
            await InvalidPropertyOfSubtypeWithoutConstructorProducesError(endpoint);
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

            async Task InvalidPropertyOfSubtypeWithoutConstructorProducesError(Endpoint endpoint)
            {
                var payload = """
                    {
                        "PropertyOfSubtypeWithoutConstructor": {
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
                    Assert.Equal("propertyOfSubtypeWithoutConstructor.requiredProperty", kvp.Key);
                    Assert.Equal("The requiredProperty field is required.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("propertyOfSubtypeWithoutConstructor.stringWithLength", kvp.Key);
                    Assert.Equal("The field stringWithLength must be a string with a maximum length of 10.", kvp.Value.Single());
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
