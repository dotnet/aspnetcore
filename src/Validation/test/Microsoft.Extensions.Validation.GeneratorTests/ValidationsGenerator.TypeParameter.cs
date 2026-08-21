// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
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

    // A generic registration helper is invisible to endpoint discovery (MapCommand is not a
    // known method name); the only discovered call site is the inner MapPost, where the
    // handler is still Func<Command<TRequest>, IResult> with TRequest open. Since #67821 the
    // delegate's invoke method resolves, handing the parser the open Command<TRequest>, a
    // named type that passes the accessibility check, unlike a bare TRequest. Without the
    // type-parameter guard the generator emits typeof(Command<TRequest>) into the resolver
    // and the generated code fails to compile (CS0246). With the guard the type is skipped:
    // the app compiles and the endpoint runs without validation.
    [Fact]
    public async Task DoesNotEmitTypeofForOpenGenericFromDelegateParameterHandler()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Validation;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapCommand<string>("/commands", cmd => Results.Ok());

app.Run();

public static class CommandEndpoints
{
    public static IEndpointRouteBuilder MapCommand<TRequest>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Func<Command<TRequest>, IResult> handler)
    {
        endpoints.MapPost(pattern, handler);
        return endpoints;
    }
}

public class Command<T>
{
    [Required]
    public string Id { get; set; } = string.Empty;

    public T? Payload { get; set; }
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/commands", async (endpoint, serviceProvider) =>
        {
            // The closed Command<string> is only known at runtime, so no validation is
            // registered for it: an invalid payload passes through with a 200 rather than
            // producing a 400. This is the silent skip discussed in #67122.
            var payload = """
                {
                    "Id": ""
                }
                """;
            var context = CreateHttpContextWithPayload(payload, serviceProvider);

            await endpoint.RequestDelegate(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        });
    }

    // ASP0039 is the author-facing half of the guard above: the generator can never emit
    // validation info for a [ValidatableType] whose type tree still contains a type
    // parameter, so the analyzer reports the silent skip at the declaration. Uses the same
    // ContainsTypeParameter walk as the emit-time guard so the two can't drift apart.
    [Fact]
    public async Task ReportsOpenGenericValidatableType_ForGenericTypeDeclaration()
    {
        var source = AnalyzerPreamble + """
#nullable enable

[ValidatableType]
public class Wrapper<T>
{
    [Required]
    public T? Value { get; set; }

    [Required]
    public string? Label { get; set; }
}
""";
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("ASP0039", diagnostic.Id);
        Assert.Contains("Wrapper", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ReportsOpenGenericValidatableType_ForTypeNestedInGenericType()
    {
        var source = AnalyzerPreamble + """
#nullable enable

public class Outer<T>
{
    [ValidatableType]
    public class Inner
    {
        [Required]
        public string? Name { get; set; }
    }
}
""";
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("ASP0039", diagnostic.Id);
        Assert.Contains("Inner", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task DoesNotReportOpenGenericValidatableType_ForNonGenericType()
    {
        var source = AnalyzerPreamble + """
#nullable enable

[ValidatableType]
public class Plain
{
    [Required]
    public string? Name { get; set; }
}
""";
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }
}
