// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.ValidationsGenerator.Tests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    [Fact]
    public async Task CanValidateParameters()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapGet("/params", (
    [Range(10, 100)] int value1,
    [Range(10, 100), Display(Name = "Valid identifier")] int value2,
    [Required] string value3 = "some-value",
    [CustomValidation(ErrorMessage = "Value must be an even number")] int value4 = 4,
    [CustomValidation, Range(10, 100)] int value5 = 10) => "OK");

app.Run();

public class CustomValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is int number && number % 2 == 0;
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/params", async (endpoint, serviceProvider) =>
        {
            var context = CreateHttpContext(serviceProvider);
            context.Request.QueryString = new QueryString("?value1=5&value2=5&value3=&value4=3&value5=5");
            await endpoint.RequestDelegate(context);
            var problemDetails = await AssertBadRequest(context);
            Assert.Collection(problemDetails.Errors,
                error =>
                {
                    Assert.Equal("value1", error.Key);
                    Assert.Equal("The field value1 must be between 10 and 100.", error.Value.Single());
                },
                error =>
                {
                    Assert.Equal("value2", error.Key);
                    Assert.Equal("The field Valid identifier must be between 10 and 100.", error.Value.Single());
                },
                error =>
                {
                    Assert.Equal("value3", error.Key);
                    Assert.Equal("The value3 field is required.", error.Value.Single());
                },
                error =>
                {
                    Assert.Equal("value4", error.Key);
                    Assert.Equal("Value must be an even number", error.Value.Single());
                },
                error =>
                {
                    Assert.Equal("value5", error.Key);
                    Assert.Collection(error.Value, error =>
                    {
                        Assert.Equal("The field value5 is invalid.", error);
                    },
                    error =>
                    {
                        Assert.Equal("The field value5 must be between 10 and 100.", error);
                    });
                });
        });
    }
}
