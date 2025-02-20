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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

var builder = WebApplication.CreateBuilder();

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
        VerifyEndpoint(compilation, "/params", async endpoint =>
        {
            var context = CreateHttpContext();
            context.Request.QueryString = new QueryString("?value1=5&value2=5&value3=&value4=3&value5=5");
            await endpoint.RequestDelegate(context);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        });
    }
}
