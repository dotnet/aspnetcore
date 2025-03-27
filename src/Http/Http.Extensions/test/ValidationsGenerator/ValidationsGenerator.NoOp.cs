// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.ValidationsGenerator.Tests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    [Fact]
    public async Task DoesNotEmitIfNoAddValidationCallExists()
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

var app = builder.Build();

app.MapPost("/complex-type", (ComplexType complexType) => Results.Ok("Passed"));

app.Run();

public class ComplexType
{
    [Range(10, 100)]
    public int IntegerWithRange { get; set; } = 10;
}
""";
        await Verify(source, out var compilation);
        // Verify that we don't validate types if no AddValidation call exists
        await VerifyEndpoint(compilation, "/complex-type", async (endpoint, serviceProvider) =>
        {
            var payload = """
                {
                    "IntegerWithRange": 5
                }
                """;
            var context = CreateHttpContextWithPayload(payload, serviceProvider);

            await endpoint.RequestDelegate(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        });
    }

    [Fact]
    public async Task DoesNotEmitIfNotCorrectAddValidationCallExists()
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

builder.Services.AddValidation("example");
SomeExtensions.AddValidation(builder.Services);

var app = builder.Build();

app.MapPost("/complex-type", (ComplexType complexType) => Results.Ok("Passed"));

app.Run();

public class ComplexType
{
    [Range(10, 100)]
    public int IntegerWithRange { get; set; } = 10;
}

public static class SomeExtensions
{
    public static IServiceCollection AddValidation(this IServiceCollection services, string someString)
    {
        // This is not the correct AddValidation method
        return services;
    }

    public static IServiceCollection AddValidation(this IServiceCollection services, Action<ValidationOptions>? configureOptions = null)
    {
        // This is not the correct AddValidation method
        return services;
    }
}
""";
        await Verify(source, out var compilation);
        // Verify that we don't validate types if no AddValidation call exists
        await VerifyEndpoint(compilation, "/complex-type", async (endpoint, serviceProvider) =>
        {
            var payload = """
                {
                    "IntegerWithRange": 5
                }
                """;
            var context = CreateHttpContextWithPayload(payload, serviceProvider);

            await endpoint.RequestDelegate(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        });
    }

    [Fact]
    public async Task DoesNotEmitForExemptTypes()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Validation;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapGet("/exempt-1", (HttpContext context) => Results.Ok("Exempt Passed!"));
app.MapGet("/exempt-2", (HttpRequest request) => Results.Ok("Exempt Passed!"));
app.MapGet("/exempt-3", (HttpResponse response) => Results.Ok("Exempt Passed!"));
app.MapGet("/exempt-4", (IFormCollection formCollection) => Results.Ok("Exempt Passed!"));
app.MapGet("/exempt-5", (IFormFileCollection formFileCollection) => Results.Ok("Exempt Passed!"));
app.MapGet("/exempt-6", (IFormFile formFile) => Results.Ok("Exempt Passed!"));
app.MapGet("/exempt-7", (Stream stream) => Results.Ok("Exempt Passed!"));
app.MapGet("/exempt-8", (PipeReader pipeReader) => Results.Ok("Exempt Passed!"));
app.MapGet("/exempt-9", (CancellationToken cancellationToken) => Results.Ok("Exempt Passed!"));
app.MapPost("/complex-type", (ComplexType complexType) => Results.Ok("Passed"));

app.Run();

public class ComplexType
{
    [Range(10, 100)]
    public int IntegerWithRange { get; set; } = 10;
}
""";
        await Verify(source, out var compilation);
        // Verify that we can validate non-exempt types
        await VerifyEndpoint(compilation, "/complex-type", async (endpoint, serviceProvider) =>
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
        });
    }
}
