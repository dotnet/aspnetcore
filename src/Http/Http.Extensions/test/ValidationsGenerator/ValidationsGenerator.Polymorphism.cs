// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.ValidationsGenerator.Tests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    [Fact]
    public async Task CanValidatePolymorphicTypes()
    {
        var source = """
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();
builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/basic-polymorphism", (BaseType model) => Results.Ok());
app.MapPost("/validatable-polymorphism", (BaseValidatableType model) => Results.Ok());
app.MapPost("/polymorphism-container", (ContainerType model) => Results.Ok());

app.Run();

public class ContainerType
{
    public BaseType BaseType { get; set; } = new BaseType();
    public BaseValidatableType BaseValidatableType { get; set; } = new BaseValidatableType();
}

[JsonDerivedType(typeof(BaseType), typeDiscriminator: "base")]
[JsonDerivedType(typeof(DerivedType), typeDiscriminator: "derived")]
public class BaseType
{
    [Display(Name = "Value 1")]
    [Range(10, 100)]
    public int Value1 { get; set; }

    [EmailAddress]
    [Required]
    public string Value2 { get; set; } = "test@example.com";
}

public class DerivedType : BaseType
{
    [Base64String]
    public string? Value3 { get; set; }
}

[JsonDerivedType(typeof(BaseValidatableType), typeDiscriminator: "base")]
[JsonDerivedType(typeof(DerivedValidatableType), typeDiscriminator: "derived")]
public class BaseValidatableType : IValidatableObject
{
    [Display(Name = "Value 1")]
    public int Value1 { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Value1 < 10 || Value1 > 100)
        {
            yield return new ValidationResult("The field Value 1 must be between 10 and 100.", new[] { nameof(Value1) });
        }
    }
}

public class DerivedValidatableType : BaseValidatableType
{
    [EmailAddress]
    public required string Value3 { get; set; }
}
""";
        await Verify(source, out var compilation);

        await VerifyEndpoint(compilation, "/basic-polymorphism", async (endpoint, serviceProvider) =>
        {
            var httpContext = CreateHttpContextWithPayload("""
            {
                "$type": "derived",
                "Value1": 5,
                "Value2": "invalid-email",
                "Value3": "invalid-base64"
            }
            """, serviceProvider);

            await endpoint.RequestDelegate(httpContext);

            var problemDetails = await AssertBadRequest(httpContext);
            Assert.Collection(problemDetails.Errors,
                error =>
                {
                    Assert.Equal("Value3", error.Key);
                    Assert.Equal("The Value3 field is not a valid Base64 encoding.", error.Value.Single());
                },
                error =>
                {
                    Assert.Equal("Value1", error.Key);
                    Assert.Equal("The field Value 1 must be between 10 and 100.", error.Value.Single());
                },
                error =>
                {
                    Assert.Equal("Value2", error.Key);
                    Assert.Equal("The Value2 field is not a valid e-mail address.", error.Value.Single());
                });
        });

        await VerifyEndpoint(compilation, "/validatable-polymorphism", async (endpoint, serviceProvider) =>
        {
            var httpContext = CreateHttpContextWithPayload("""
            {
                "$type": "derived",
                "Value1": 5,
                "Value3": "invalid-email"
            }
            """, serviceProvider);

            await endpoint.RequestDelegate(httpContext);

            var problemDetails = await AssertBadRequest(httpContext);
            Assert.Collection(problemDetails.Errors,
                error =>
                {
                    Assert.Equal("Value3", error.Key);
                    Assert.Equal("The Value3 field is not a valid e-mail address.", error.Value.Single());
                },
                error =>
                {
                    Assert.Equal("Value1", error.Key);
                    Assert.Equal("The field Value 1 must be between 10 and 100.", error.Value.Single());
                });

            httpContext = CreateHttpContextWithPayload("""
            {
                "$type": "derived",
                "Value1": 5,
                "Value3": "test@example.com"
            }
            """, serviceProvider);

            await endpoint.RequestDelegate(httpContext);

            var problemDetails1 = await AssertBadRequest(httpContext);
            Assert.Collection(problemDetails1.Errors,
                error =>
                {
                    Assert.Equal("Value1", error.Key);
                    Assert.Equal("The field Value 1 must be between 10 and 100.", error.Value.Single());
                });
        });

        await VerifyEndpoint(compilation, "/polymorphism-container", async (endpoint, serviceProvider) =>
        {
            var httpContext = CreateHttpContextWithPayload("""
            {
                "BaseType": {
                    "$type": "derived",
                    "Value1": 5,
                    "Value2": "invalid-email",
                    "Value3": "invalid-base64"
                },
                "BaseValidatableType": {
                    "$type": "derived",
                    "Value1": 5,
                    "Value3": "test@example.com"
                }
            }
            """, serviceProvider);

            await endpoint.RequestDelegate(httpContext);

            var problemDetails = await AssertBadRequest(httpContext);
            Assert.Collection(problemDetails.Errors,
            error =>
                {
                    Assert.Equal("BaseType.Value3", error.Key);
                    Assert.Equal("The Value3 field is not a valid Base64 encoding.", error.Value.Single());
                },
                error =>
                {
                    Assert.Equal("BaseType.Value1", error.Key);
                    Assert.Equal("The field Value 1 must be between 10 and 100.", error.Value.Single());
                },
                error =>
                {
                    Assert.Equal("BaseType.Value2", error.Key);
                    Assert.Equal("The Value2 field is not a valid e-mail address.", error.Value.Single());
                },
                error =>
                {
                    Assert.Equal("BaseValidatableType.Value1", error.Key);
                    Assert.Equal("The field Value 1 must be between 10 and 100.", error.Value.Single());
                });
        });
    }
}
