// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.Validation.GeneratorTests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    // On main, [ValidatableType] on an open generic emits typeof(global::Wrapper<T>) into
    // the non-generic resolver and the generated code fails to compile (CS0246). The open
    // declaration must be skipped; the closed Wrapper<string> from the endpoint call site
    // still registers and validates.
    [Fact]
    public async Task DoesNotEmitTypeofForOpenGenericWithValidatableTypeAttribute()
    {
        var source = """
#pragma warning disable ASP0029
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Validation;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/wrappers", (Wrapper<string> w) => Results.Ok());

app.Run();

[ValidatableType]
public class Wrapper<T> where T : class
{
    [Required]
    public T? Value { get; set; }

    [Required]
    public string Label { get; set; } = "default";
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/wrappers", async (endpoint, serviceProvider) =>
        {
            await MissingValueProducesError(endpoint);
            await EmptyLabelProducesError(endpoint);
            await ValidInputProducesNoErrors(endpoint);

            async Task MissingValueProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "Label": "some-label"
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("Value", kvp.Key);
                    Assert.Equal("The Value field is required.", kvp.Value.Single());
                });
            }

            async Task EmptyLabelProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "Value": "some-value",
                    "Label": ""
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("Label", kvp.Key);
                    Assert.Equal("The Label field is required.", kvp.Value.Single());
                });
            }

            async Task ValidInputProducesNoErrors(Endpoint endpoint)
            {
                var payload = """
                {
                    "Value": "some-value",
                    "Label": "some-label"
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);
                await endpoint.RequestDelegate(context);

                Assert.Equal(200, context.Response.StatusCode);
            }
        });
    }

    // typeof(global::Outer<T>.Inner) is CS0246 too: Inner declares no type parameters of
    // its own, but its emitted name carries the outer's, so the guard must recurse into
    // ContainingType. The closed Outer<string>.Inner still registers and validates.
    [Fact]
    public async Task DoesNotEmitTypeofForValidatableTypeNestedInOpenGeneric()
    {
        var source = """
#pragma warning disable ASP0029
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Validation;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/inners", (Outer<string>.Inner inner) => Results.Ok());

app.Run();

public class Outer<T> where T : class
{
    [ValidatableType]
    public class Inner
    {
        [Required]
        public string Name { get; set; } = "default";
    }
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/inners", async (endpoint, serviceProvider) =>
        {
            var payload = """
            {
                "Name": ""
            }
            """;
            var context = CreateHttpContextWithPayload(payload, serviceProvider);

            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            Assert.Collection(problemDetails.Errors, kvp =>
            {
                Assert.Equal("Name", kvp.Key);
                Assert.Equal("The Name field is required.", kvp.Value.Single());
            });
        });
    }
}
