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
    public async Task CanValidateComplexTypes()
    {
        // Arrange
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

var builder = WebApplication.CreateBuilder();

var app = builder.Build();

app.Conventions.WithValidation();

app.MapPost("/complex-type", (ComplexType complexType) => Results.Ok());

app.Run();

public class ComplexType
{
    [Range(10, 100)]
    public int IntegerWithRange { get; set; } = 10;

    [Range(10, 100), Display(Name = "Valid identifier")]
    public int IntegerWithRangeAndDisplayName { get; set; } = 50;

    [Required]
    public SubType PropertyWithMemberAttributes { get; set; } = new SubType();

    public SubType PropertyWithoutMemberAttributes { get; set; } = new SubType();

    public SubTypeWithInheritance PropertyWithInheritance { get; set; } = new SubTypeWithInheritance();

    public List<SubType> ListOfSubTypes { get; set; } = [];

    [CustomValidation(ErrorMessage = "Value must be an even number")]
    public int IntegerWithCustomValidationAttribute { get; set; }

    [CustomValidation, Range(10, 100)]
    public int PropertyWithMultipleAttributes { get; set; } = 10;
}

public class CustomValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is int number && number % 2 == 0;
}

public class SubType
{
    [Required]
    public string RequiredProperty { get; set; } = "some-value";

    [StringLength(10)]
    public string? StringWithLength { get; set; }
}

public class SubTypeWithInheritance : SubType
{
    [EmailAddress]
    public string? EmailString { get; set; }
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, async client =>
        {
            await InvalidIntegerWithRangeProducesError(client);
            await InvalidIntegerWithRangeAndDisplayNameProducesError(client);
            await MissingRequiredSubtypePropertyProducesError(client);
            await InvalidRequiredSubtypePropertyProducesError(client);
            await InvalidSubTypeWithInheritancePropertyProducesError(client);
            await InvalidListOfSubTypesProducesError(client);
            await InvalidPropertyWithCustomValidationAttributeProducesError(client);
            await InvalidPropertyWithMultipleAttributesProducesError(client);

            static async Task InvalidIntegerWithRangeProducesError(HttpClient client)
            {
                var payload = """
                {
                    "IntegerWithRange": 5
                }
                """;
                var content = new StringContent(payload, new MediaTypeHeaderValue("application/json"));
                var response = await client.PostAsync("/complex-type", content);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                var problemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("IntegerWithRange", kvp.Key);
                    Assert.Equal("The field IntegerWithRange must be between 10 and 100.", kvp.Value.Single());
                });
            }

            static async Task InvalidIntegerWithRangeAndDisplayNameProducesError(HttpClient client)
            {
                var payload = """
                {
                    "IntegerWithRangeAndDisplayName": 5
                }
                """;
                var content = new StringContent(payload, new MediaTypeHeaderValue("application/json"));
                var response = await client.PostAsync("/complex-type", content);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                var problemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("IntegerWithRangeAndDisplayName", kvp.Key);
                    Assert.Equal("The field Valid identifier must be between 10 and 100.", kvp.Value.Single());
                });
            }

            static async Task MissingRequiredSubtypePropertyProducesError(HttpClient client)
            {
                var payload = """
                {
                    "PropertyWithMemberAttributes": null
                }
                """;
                var content = new StringContent(payload, new MediaTypeHeaderValue("application/json"));
                var response = await client.PostAsync("/complex-type", content);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                var problemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("PropertyWithMemberAttributes", kvp.Key);
                    Assert.Equal("The PropertyWithMemberAttributes field is required.", kvp.Value.Single());
                });
            }

            static async Task InvalidRequiredSubtypePropertyProducesError(HttpClient client)
            {
                var payload = """
                {
                    "PropertyWithMemberAttributes": {
                        "RequiredProperty": "",
                        "StringWithLength": "way-too-long"
                    }
                }
                """;
                var content = new StringContent(payload, new MediaTypeHeaderValue("application/json"));
                var response = await client.PostAsync("/complex-type", content);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                var problemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
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

            static async Task InvalidSubTypeWithInheritancePropertyProducesError(HttpClient client)
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
                var content = new StringContent(payload, new MediaTypeHeaderValue("application/json"));
                var response = await client.PostAsync("/complex-type", content);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                var problemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
                Assert.Collection(problemDetails.Errors,
                kvp =>
                {
                    Assert.Equal("PropertyWithInheritance.RequiredProperty", kvp.Key);
                    Assert.Equal("The RequiredProperty field is required.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("PropertyWithInheritance.StringWithLength", kvp.Key);
                    Assert.Equal("The field StringWithLength must be a string with a maximum length of 10.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("PropertyWithInheritance.EmailString", kvp.Key);
                    Assert.Equal("The EmailString field is not a valid e-mail address.", kvp.Value.Single());
                });
            }

            static async Task InvalidListOfSubTypesProducesError(HttpClient client)
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
                var content = new StringContent(payload, new MediaTypeHeaderValue("application/json"));
                var response = await client.PostAsync("/complex-type", content);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                var problemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
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

            static async Task InvalidPropertyWithCustomValidationAttributeProducesError(HttpClient client)
            {
                var payload = """
                {
                    "IntegerWithCustomValidationAttribute": 5
                }
                """;
                var content = new StringContent(payload, new MediaTypeHeaderValue("application/json"));
                var response = await client.PostAsync("/complex-type", content);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                var problemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("IntegerWithCustomValidationAttribute", kvp.Key);
                    Assert.Equal("Value must be an even number", kvp.Value.Single());
                });
            }

            static async Task InvalidPropertyWithMultipleAttributesProducesError(HttpClient client)
            {
                var payload = """
                {
                    "PropertyWithMultipleAttributes": 5
                }
                """;
                var content = new StringContent(payload, new MediaTypeHeaderValue("application/json"));
                var response = await client.PostAsync("/complex-type", content);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                var problemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
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
        });

    }
}
