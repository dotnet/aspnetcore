// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.Validation.GeneratorTests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    [Fact]
    public async Task CanValidateComplexTypesWithJsonIgnore()
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
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/complex-type-with-json-ignore", (ComplexTypeWithJsonIgnore complexType) => Results.Ok("Passed"!));
app.MapPost("/record-type-with-json-ignore", (RecordTypeWithJsonIgnore recordType) => Results.Ok("Passed"!));

app.Run();

public class ComplexTypeWithJsonIgnore
{
    [Range(10, 100)]
    public int ValidatedProperty { get; set; } = 10;

    [JsonIgnore]
    [Required] // This should be ignored because of [JsonIgnore]
    public string IgnoredProperty { get; set; } = null!;

    [JsonIgnore]
    public CircularReferenceType? CircularReference { get; set; }
}

public class CircularReferenceType
{
    [JsonIgnore]
    public ComplexTypeWithJsonIgnore? Parent { get; set; }
    
    public string Name { get; set; } = "test";
}

public record RecordTypeWithJsonIgnore
{
    [Range(10, 100)]
    public int ValidatedProperty { get; set; } = 10;

    [JsonIgnore]
    [Required] // This should be ignored because of [JsonIgnore]
    public string IgnoredProperty { get; set; } = null!;

    [JsonIgnore]
    public CircularReferenceRecord? CircularReference { get; set; }
}

public record CircularReferenceRecord
{
    [JsonIgnore]
    public RecordTypeWithJsonIgnore? Parent { get; set; }
    
    public string Name { get; set; } = "test";
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/complex-type-with-json-ignore", async (endpoint, serviceProvider) =>
        {
            await ValidInputWithJsonIgnoreProducesNoWarnings(endpoint);
            await InvalidValidatedPropertyProducesError(endpoint);

            async Task ValidInputWithJsonIgnoreProducesNoWarnings(Endpoint endpoint)
            {
                var payload = """
                {
                    "ValidatedProperty": 50
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);
                await endpoint.RequestDelegate(context);

                Assert.Equal(200, context.Response.StatusCode);
            }

            async Task InvalidValidatedPropertyProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "ValidatedProperty": 5
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("ValidatedProperty", kvp.Key);
                    Assert.Equal("The field ValidatedProperty must be between 10 and 100.", kvp.Value.Single());
                });
            }
        });
        
        await VerifyEndpoint(compilation, "/record-type-with-json-ignore", async (endpoint, serviceProvider) =>
        {
            await ValidInputWithJsonIgnoreProducesNoWarningsForRecord(endpoint);
            await InvalidValidatedPropertyProducesErrorForRecord(endpoint);

            async Task ValidInputWithJsonIgnoreProducesNoWarningsForRecord(Endpoint endpoint)
            {
                var payload = """
                {
                    "ValidatedProperty": 50
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);
                await endpoint.RequestDelegate(context);

                Assert.Equal(200, context.Response.StatusCode);
            }

            async Task InvalidValidatedPropertyProducesErrorForRecord(Endpoint endpoint)
            {
                var payload = """
                {
                    "ValidatedProperty": 5
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("ValidatedProperty", kvp.Key);
                    Assert.Equal("The field ValidatedProperty must be between 10 and 100.", kvp.Value.Single());
                });
            }
        });
    }
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
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();
builder.Services.AddSingleton<TestService>();

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

    [FromServices]
    [Required] // This should be ignored because of [FromServices]
    public TestService ServiceProperty { get; set; } = null!;
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

public class TestService
{
    [Range(10, 100)]
    public int Value { get; set; } = 4;
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
                    Assert.Equal("PropertyWithMemberAttributes", kvp.Key);
                    Assert.Equal("The PropertyWithMemberAttributes field is required.", kvp.Value.Single());
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
    public async Task SkipsClassesWithNonAccessibleTypes()
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

var app = builder.Build();

app.MapPost("/accessibility-test", (AccessibilityTestType accessibilityTest) => Results.Ok("Passed"!));

app.Run();

public class AccessibilityTestType
{
    [Required]
    public string PublicProperty { get; set; } = "";

    [Required]
    private string PrivateProperty { get; set; } = "";

    [Required]
    protected string ProtectedProperty { get; set; } = "";

    [Required]
    private PrivateNestedType PrivateNestedProperty { get; set; } = new();

    [Required]
    protected ProtectedNestedType ProtectedNestedProperty { get; set; } = new();

    [Required]
    internal InternalNestedType InternalNestedProperty { get; set; } = new();

    private class PrivateNestedType
    {
        [Required]
        public string RequiredProperty { get; set; } = "";
    }

    protected class ProtectedNestedType
    {
        [Required]
        public string RequiredProperty { get; set; } = "";
    }

    internal class InternalNestedType
    {
        [Required]
        public string RequiredProperty { get; set; } = "";
    }
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/accessibility-test", async (endpoint, serviceProvider) =>
        {
            await ValidPublicPropertyStillValidated(endpoint);

            async Task ValidPublicPropertyStillValidated(Endpoint endpoint)
            {
                var payload = """
                {
                    "PublicProperty": ""
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("PublicProperty", kvp.Key);
                    Assert.Equal("The PublicProperty field is required.", kvp.Value.Single());
                });
            }
        });
    }

    [Fact]
    public async Task ValidatesPropertiesWithJsonIgnoreWhenWritingConditions()
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
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/json-ignore-conditions", (JsonIgnoreConditionsModel model) => Results.Ok("Passed"!));

app.Run();

public class JsonIgnoreConditionsModel
{
    // JsonIgnore without Condition defaults to Always - should be ignored
    [JsonIgnore]
    [MaxLength(10)]
    public string? PropertyWithJsonIgnoreOnly { get; set; }

    // JsonIgnoreCondition.Always - should be ignored
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    [MaxLength(10)]
    public string? PropertyWithAlways { get; set; }

    // JsonIgnoreCondition.WhenWritingDefault - should be validated (only affects writing)
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [MaxLength(10)]
    public string? PropertyWithWhenWritingDefault { get; set; }

    // JsonIgnoreCondition.WhenWritingNull - should be validated (only affects writing)
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [MaxLength(10)]
    public string? PropertyWithWhenWritingNull { get; set; }

    // JsonIgnoreCondition.Never - should be validated (never ignored)
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [MaxLength(10)]
    public string? PropertyWithNever { get; set; }
    
    // JsonIgnoreCondition.WhenReading - should be ignored (affects reading/deserialization)
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenReading)]
    [MaxLength(10)]
    public string? PropertyWithWhenReading { get; set; }
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/json-ignore-conditions", async (endpoint, serviceProvider) =>
        {
            // Test that WhenWritingDefault is validated
            await InvalidPropertyWithWhenWritingDefaultProducesError(endpoint);
            
            // Test that WhenWritingNull is validated
            await InvalidPropertyWithWhenWritingNullProducesError(endpoint);
            
            // Test that Never is validated
            await InvalidPropertyWithNeverProducesError(endpoint);
            
            // Test that Always and JsonIgnore (without condition) are NOT validated (no error expected)
            await InvalidPropertiesWithAlwaysAndDefaultAreIgnored(endpoint);
            
            // Test that WhenReading is NOT validated (no error expected)
            await InvalidPropertyWithWhenReadingIsIgnored(endpoint);

            async Task InvalidPropertyWithWhenWritingDefaultProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "PropertyWithWhenWritingDefault": "ExceedsMaxLength"
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("PropertyWithWhenWritingDefault", kvp.Key);
                    Assert.Contains("maximum length", kvp.Value.Single());
                });
            }

            async Task InvalidPropertyWithWhenWritingNullProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "PropertyWithWhenWritingNull": "ExceedsMaxLength"
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("PropertyWithWhenWritingNull", kvp.Key);
                    Assert.Contains("maximum length", kvp.Value.Single());
                });
            }

            async Task InvalidPropertyWithNeverProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "PropertyWithNever": "ExceedsMaxLength"
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("PropertyWithNever", kvp.Key);
                    Assert.Contains("maximum length", kvp.Value.Single());
                });
            }

            async Task InvalidPropertiesWithAlwaysAndDefaultAreIgnored(Endpoint endpoint)
            {
                var payload = """
                {
                    "PropertyWithJsonIgnoreOnly": "ExceedsMaxLength",
                    "PropertyWithAlways": "ExceedsMaxLength"
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                // Should succeed because these properties are ignored during validation
                Assert.Equal(200, context.Response.StatusCode);
            }

            async Task InvalidPropertyWithWhenReadingIsIgnored(Endpoint endpoint)
            {
                var payload = """
                {
                    "PropertyWithWhenReading": "ExceedsMaxLength"
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                // Should succeed because this property is ignored during reading/deserialization
                Assert.Equal(200, context.Response.StatusCode);
            }
        });
    }
}
