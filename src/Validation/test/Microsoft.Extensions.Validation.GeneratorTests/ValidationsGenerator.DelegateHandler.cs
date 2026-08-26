// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.Validation.GeneratorTests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    // Regression test for https://github.com/dotnet/aspnetcore/issues/67122
    // When an endpoint handler is passed as a delegate variable (an ILocalReferenceOperation)
    // rather than an inline lambda, the ValidationsGenerator must still discover the delegate's
    // parameter types and run validation for the endpoint. Previously the resolver had no case
    // for delegate variables, so validation discovery was silently skipped and invalid payloads
    // were accepted with a 200 response.
    [Fact]
    public async Task CanValidateParametersFromDelegateVariableHandler()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

// Handler is a delegate variable, not an inline lambda. See issue #67122.
Func<CreateOrderRequest, IResult> handler = req => Results.Ok(req);
app.MapPost("/orders", handler);

app.Run();

public class CreateOrderRequest
{
    [Required]
    [MinLength(3)]
    public string ProductName { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int Quantity { get; set; }
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/orders", async (endpoint, serviceProvider) =>
        {
            await InvalidPayloadProducesError(endpoint);
            await ValidPayloadProducesNoError(endpoint);

            async Task InvalidPayloadProducesError(Endpoint endpoint)
            {
                var payload = """
                    {
                        "ProductName": "ab",
                        "Quantity": 0
                    }
                    """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors,
                    error =>
                    {
                        Assert.Equal("ProductName", error.Key);
                        Assert.Equal("The field ProductName must be a string or array type with a minimum length of '3'.", error.Value.Single());
                    },
                    error =>
                    {
                        Assert.Equal("Quantity", error.Key);
                        Assert.Equal("The field Quantity must be between 1 and 1000.", error.Value.Single());
                    });
            }

            async Task ValidPayloadProducesNoError(Endpoint endpoint)
            {
                var payload = """
                    {
                        "ProductName": "Widget",
                        "Quantity": 5
                    }
                    """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                Assert.Equal(200, context.Response.StatusCode);
            }
        });
    }
}
