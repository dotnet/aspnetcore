// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.Validation.GeneratorTests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    // Repro for the silent-skip half of issue dotnet/aspnetcore#65418. When the
    // route-handler delegate parameter resolves to an open ITypeParameterSymbol —
    // as happens inside the body of a generic endpoint extension method like
    // MapValidated<T>(this IEndpointRouteBuilder, string) — TryExtractValidatableType
    // on main hits the DeclaredAccessibility check (NotApplicable for type parameters)
    // and silently returns false. The concrete constraint type is therefore never
    // discovered and no typeof(...) check is emitted for it, even though it carries
    // [Required] / [Range] attributes. With the fix the generator walks the type
    // parameter's ConstraintTypes and discovers the concrete validatable type.
    //
    // The snapshot is the bug witness: on main the resolver body is empty (no
    // typeof(global::UserRequest) check); with the fix the resolver contains the
    // type and its members.
    [Fact]
    public async Task CanValidateOpenTypeParameterReachableThroughConstraint()
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

app.MapValidated<UserRequest>("/users");

app.Run();

public class UserRequest
{
    [Required]
    public string Name { get; set; } = "default";

    [Range(1, 120)]
    public int Age { get; set; } = 25;
}

public static class GenericEndpointExtensions
{
    public static RouteHandlerBuilder MapValidated<T>(this IEndpointRouteBuilder endpoints, string pattern)
        where T : UserRequest
        => endpoints.MapPost(pattern, (T req) => Results.Ok());
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/users", async (endpoint, serviceProvider) =>
        {
            await InvalidNameProducesError(endpoint);
            await InvalidAgeProducesError(endpoint);
            await ValidInputProducesNoErrors(endpoint);

            async Task InvalidNameProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "Name": "",
                    "Age": 30
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
            }

            async Task InvalidAgeProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "Name": "Alice",
                    "Age": 0
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("Age", kvp.Key);
                    Assert.Equal("The field Age must be between 1 and 120.", kvp.Value.Single());
                });
            }

            async Task ValidInputProducesNoErrors(Endpoint endpoint)
            {
                var payload = """
                {
                    "Name": "Alice",
                    "Age": 30
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);
                await endpoint.RequestDelegate(context);

                Assert.Equal(200, context.Response.StatusCode);
            }
        });
    }

    [Fact]
    public async Task CanValidateTypesWithGenericBaseClass()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/crtp-class", (CreateOrderCommand request) => Results.Ok("Passed"!));
app.MapPost("/crtp-record", (CreateRecordCommand request) => Results.Ok("Passed"!));

app.Run();

public class CommandBase<TSelf> where TSelf : CommandBase<TSelf>
{
    [Required]
    public string Name { get; set; } = "default";
}

public class CreateOrderCommand : CommandBase<CreateOrderCommand>
{
    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;
}

public record RecordCommandBase<TSelf> where TSelf : RecordCommandBase<TSelf>
{
    [Required]
    public string Title { get; set; } = "default";
}

public record CreateRecordCommand : RecordCommandBase<CreateRecordCommand>
{
    [Range(1, 100)]
    public int Count { get; set; } = 1;
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/crtp-class", async (endpoint, serviceProvider) =>
        {
            await InvalidNameProducesError(endpoint);
            await InvalidQuantityProducesError(endpoint);
            await ValidInputProducesNoErrors(endpoint);

            async Task InvalidNameProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "Name": "",
                    "Quantity": 5
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
            }

            async Task InvalidQuantityProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "Name": "valid",
                    "Quantity": 0
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("Quantity", kvp.Key);
                    Assert.Equal("The field Quantity must be between 1 and 1000.", kvp.Value.Single());
                });
            }

            async Task ValidInputProducesNoErrors(Endpoint endpoint)
            {
                var payload = """
                {
                    "Name": "valid",
                    "Quantity": 5
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);
                await endpoint.RequestDelegate(context);

                Assert.Equal(200, context.Response.StatusCode);
            }
        });

        await VerifyEndpoint(compilation, "/crtp-record", async (endpoint, serviceProvider) =>
        {
            await InvalidTitleProducesError(endpoint);
            await InvalidCountProducesError(endpoint);
            await ValidRecordInputProducesNoErrors(endpoint);

            async Task InvalidTitleProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "Title": "",
                    "Count": 5
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("Title", kvp.Key);
                    Assert.Equal("The Title field is required.", kvp.Value.Single());
                });
            }

            async Task InvalidCountProducesError(Endpoint endpoint)
            {
                var payload = """
                {
                    "Title": "valid",
                    "Count": 0
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(context);

                var problemDetails = await AssertBadRequest(context);
                Assert.Collection(problemDetails.Errors, kvp =>
                {
                    Assert.Equal("Count", kvp.Key);
                    Assert.Equal("The field Count must be between 1 and 100.", kvp.Value.Single());
                });
            }

            async Task ValidRecordInputProducesNoErrors(Endpoint endpoint)
            {
                var payload = """
                {
                    "Title": "valid",
                    "Count": 5
                }
                """;
                var context = CreateHttpContextWithPayload(payload, serviceProvider);
                await endpoint.RequestDelegate(context);

                Assert.Equal(200, context.Response.StatusCode);
            }
        });
    }

    // Coverage for the ContainsTypeParameter guard — the typeof(TSelf) compile-error half of
    // issue dotnet/aspnetcore#65418. A generic endpoint helper constrains its request type to a
    // CRTP base (where T : Node<T>) whose only validatable member is itself of the type
    // parameter. Reached through the constraint walk as an open type, that member's type is the
    // unresolved parameter, so ContainsTypeParameter is true and the member is dropped — at the
    // regular-property guard for the class (Node<TSelf>.Next) and at the record primary-constructor
    // guard for the record (RecordNode<TSelf>(TSelf Link)). With the member dropped, the base has no
    // validatable members and is never emitted as a ValidatableType, so the snapshot resolver
    // contains neither Node<T> nor RecordNode<T>.
    //
    // Without the guard the generator keeps the member and adds the open base with
    // typeof(global::Node<T>) — T is not in scope in the generated resolver — and the generated
    // code fails to compile (CS0246). VerifyEndpoint re-emits source + generated code and asserts
    // the emit succeeds, so this test is exactly that compile-error repro.
    [Fact]
    public async Task DoesNotEmitTypeofForTypeParameterMembersReachedThroughConstraint()
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

app.MapValidatedNode<ConcreteNode>("/nodes");
app.MapValidatedRecord<ConcreteRecord>("/records");

app.Run();

public class Node<TSelf> where TSelf : Node<TSelf>
{
    [Required]
    public TSelf Next { get; set; } = default!;
}

public class ConcreteNode : Node<ConcreteNode>
{
}

public record RecordNode<TSelf>(TSelf Link) where TSelf : RecordNode<TSelf>;

public record ConcreteRecord() : RecordNode<ConcreteRecord>(default!);

public static class GenericCrtpEndpointExtensions
{
    public static RouteHandlerBuilder MapValidatedNode<T>(this IEndpointRouteBuilder endpoints, string pattern)
        where T : Node<T>
        => endpoints.MapPost(pattern, (T req) => Results.Ok());

    public static RouteHandlerBuilder MapValidatedRecord<T>(this IEndpointRouteBuilder endpoints, string pattern)
        where T : RecordNode<T>
        => endpoints.MapPost(pattern, (T req) => Results.Ok());
}
""";
        await Verify(source, out var compilation);

        // Reaching either callback proves source + generated code compiled and the host started;
        // without the guard the generated resolver references typeof(global::Node<T>) and fails to
        // compile at the VerifyEndpoint emit step.
        await VerifyEndpoint(compilation, "/nodes", (endpoint, _) =>
        {
            Assert.NotNull(endpoint);
            return Task.CompletedTask;
        });

        await VerifyEndpoint(compilation, "/records", (endpoint, _) =>
        {
            Assert.NotNull(endpoint);
            return Task.CompletedTask;
        });
    }
}
