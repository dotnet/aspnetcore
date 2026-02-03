// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.Validation.GeneratorTests;

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
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();
builder.Services.AddSingleton<TestService>();

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

    [FromServices]
    [Required]
    public TestService ServiceProperty { get; set; } = null!;
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

public class TestService
{
    [Range(10, 100)]
    public int Value { get; set; } = 4;
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
    int PropertyWithMultipleAttributes = 10,
    [FromServices] [Required] TestService ServiceProperty = null!, // This should be ignored because of [FromServices]
    [FromKeyedServices("serviceKey")] [Range(10, 100)] int KeyedServiceProperty = 5 // This should be ignored because of [FromKeyedServices]
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
                    Assert.Equal("IntegerWithRange", kvp.Key);
                    Assert.Equal("The field IntegerWithRange must be between 10 and 100.", kvp.Value.Single());
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
                    Assert.Equal("IntegerWithRangeAndDisplayName", kvp.Key);
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
                    Assert.Equal("PropertyWithMemberAttributes.RequiredProperty", kvp.Key);
                    Assert.Equal("The RequiredProperty field is required.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("PropertyWithMemberAttributes.StringWithLength", kvp.Key);
                    Assert.Equal("The field StringWithLength must be a string with a maximum length of 10.", kvp.Value.Single());
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
                    Assert.Equal("PropertyWithInheritance.EmailString", kvp.Key);
                    Assert.Equal("The EmailString field is not a valid e-mail address.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("PropertyWithInheritance.RequiredProperty", kvp.Key);
                    Assert.Equal("The RequiredProperty field is required.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("PropertyWithInheritance.StringWithLength", kvp.Key);
                    Assert.Equal("The field StringWithLength must be a string with a maximum length of 10.", kvp.Value.Single());
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
                    Assert.Equal("ListOfSubTypes[0].RequiredProperty", kvp.Key);
                    Assert.Equal("The RequiredProperty field is required.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("ListOfSubTypes[0].StringWithLength", kvp.Key);
                    Assert.Equal("The field StringWithLength must be a string with a maximum length of 10.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("ListOfSubTypes[1].StringWithLength", kvp.Key);
                    Assert.Equal("The field StringWithLength must be a string with a maximum length of 10.", kvp.Value.Single());
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
                    Assert.Equal("IntegerWithDerivedValidationAttribute", kvp.Key);
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
                    Assert.Equal("PropertyWithMultipleAttributes", kvp.Key);
                    Assert.Collection(kvp.Value,
                    error =>
                    {
                        Assert.Equal("The field PropertyWithMultipleAttributes is invalid.", error);
                    },
                    error =>
                    {
                        Assert.Equal("The field PropertyWithMultipleAttributes must be between 10 and 100.", error);
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
                    Assert.Equal("IntegerWithCustomValidation", kvp.Key);
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
                    Assert.Equal("PropertyOfSubtypeWithoutConstructor.RequiredProperty", kvp.Key);
                    Assert.Equal("The RequiredProperty field is required.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("PropertyOfSubtypeWithoutConstructor.StringWithLength", kvp.Key);
                    Assert.Equal("The field StringWithLength must be a string with a maximum length of 10.", kvp.Value.Single());
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

    [Fact]
    public async Task CanValidateRecordStructTypes()
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

app.MapPost("/validatable-record-struct", (ValidatableRecordStruct validatableRecordStruct) => Results.Ok("Passed"));

app.Run();

public record struct SubRecordStruct([Required] string RequiredProperty, [StringLength(10)] string? StringWithLength);

public record struct ValidatableRecordStruct(
    [Range(10, 100)]
    int IntegerWithRange,
    [Range(10, 100), Display(Name = "Valid identifier")]
    int IntegerWithRangeAndDisplayName,
    SubRecordStruct SubProperty
);
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/validatable-record-struct", async (endpoint, serviceProvider) =>
        {
            await InvalidIntegerWithRangeProducesError(endpoint);
            await InvalidIntegerWithRangeAndDisplayNameProducesError(endpoint);
            await InvalidSubPropertyProducesError(endpoint);
            await ValidInputProducesNoWarnings(endpoint);

            async Task InvalidIntegerWithRangeProducesError(Endpoint endpoint)
            {
                var payload = """
                    {
                        "IntegerWithRange": 5,
                        "IntegerWithRangeAndDisplayName": 50,
                        "SubProperty": {
                            "RequiredProperty": "valid",
                            "StringWithLength": "valid"
                        }
                    }
                    """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("IntegerWithRange", kvp.Key);
                    Assert.Equal("The field IntegerWithRange must be between 10 and 100.", kvp.Value.Single());
                });
            }

            async Task InvalidIntegerWithRangeAndDisplayNameProducesError(Endpoint endpoint)
            {
                var payload = """
                    {
                        "IntegerWithRange": 50,
                        "IntegerWithRangeAndDisplayName": 5,
                        "SubProperty": {
                            "RequiredProperty": "valid",
                            "StringWithLength": "valid"
                        }
                    }
                    """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("IntegerWithRangeAndDisplayName", kvp.Key);
                    Assert.Equal("The field Valid identifier must be between 10 and 100.", kvp.Value.Single());
                });
            }

            async Task InvalidSubPropertyProducesError(Endpoint endpoint)
            {
                var payload = """
                    {
                        "IntegerWithRange": 50,
                        "IntegerWithRangeAndDisplayName": 50,
                        "SubProperty": {
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
                    Assert.Equal("SubProperty.RequiredProperty", kvp.Key);
                    Assert.Equal("The RequiredProperty field is required.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("SubProperty.StringWithLength", kvp.Key);
                    Assert.Equal("The field StringWithLength must be a string with a maximum length of 10.", kvp.Value.Single());
                });
            }

            async Task ValidInputProducesNoWarnings(Endpoint endpoint)
            {
                var payload = """
                    {
                        "IntegerWithRange": 50,
                        "IntegerWithRangeAndDisplayName": 50,
                        "SubProperty": {
                            "RequiredProperty": "valid",
                            "StringWithLength": "valid"
                        }
                    }
                    """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);
                await endpoint.RequestDelegate(context);

                Assert.Equal(200, context.Response.StatusCode);
            }
        });

    }
}
