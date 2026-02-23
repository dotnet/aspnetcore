// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.Validation.GeneratorTests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    [Fact]
    public async Task CanValidateMultipleNamespaces()
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

app.MapPost("/namespace-one", (NamespaceOne.Type obj) => Results.Ok("Passed"));
app.MapPost("/namespace-two", (NamespaceTwo.Type obj) => Results.Ok("Passed"));

app.Run();

namespace NamespaceOne {
    public class Type
    {
        [StringLength(10)]
        public string StringWithLength { get; set; } = string.Empty;
    }
}

namespace NamespaceTwo {
    public class Type
    {
        [StringLength(20)]
        public string StringWithLength { get; set; } = string.Empty;
    }
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/namespace-one", async (endpoint, serviceProvider) =>
        {
            await InvalidStringWithLengthProducesError(endpoint);
            await ValidInputProducesNoWarnings(endpoint);

            async Task InvalidStringWithLengthProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "StringWithLength": "abcdefghijk"
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("StringWithLength", kvp.Key);
                    Assert.Equal("The field StringWithLength must be a string with a maximum length of 10.", kvp.Value.Single());
                });
            }

            async Task ValidInputProducesNoWarnings(Endpoint endpoint)
            {
                var payload = """
                {
                    "StringWithLength": "abc"
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);
                await endpoint.RequestDelegate(context);

                Assert.Equal(200, context.Response.StatusCode);
            }
        });
        await VerifyEndpoint(compilation, "/namespace-two", async (endpoint, serviceProvider) =>
        {
            await InvalidStringWithLengthProducesError(endpoint);
            await ValidInputProducesNoWarnings(endpoint);

            async Task InvalidStringWithLengthProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "StringWithLength": "abcdefghijklmnopqrstu"
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("StringWithLength", kvp.Key);
                    Assert.Equal("The field StringWithLength must be a string with a maximum length of 20.", kvp.Value.Single());
                });
            }

            async Task ValidInputProducesNoWarnings(Endpoint endpoint)
            {
                var payload = """
                {
                    "StringWithLength": "abcdefghijk"
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);
                await endpoint.RequestDelegate(context);

                Assert.Equal(200, context.Response.StatusCode);
            }
        });
    }
}
