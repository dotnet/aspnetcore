// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

using static Microsoft.Extensions.Validation.Tests.ValidationTestBase;

namespace Microsoft.Extensions.Validation.GeneratorTests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    [Fact]
    public async Task CanValidateUnionCases()
    {
        var source = """
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Validation;

var builder = WebApplication.CreateBuilder();
builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/direct", (DirectCommand command) => Results.StatusCode(201));
app.MapPost("/nested", (CommandEnvelope envelope) => Results.Ok());
app.MapPost("/multiple", (MultipleCommand command) => Results.Ok());

app.Run();

public sealed class CreateUser
{
    [Required]
    public string? Name { get; init; }
}

public union DirectCommand(CreateUser, bool);

public sealed class CommandEnvelope
{
    public required DirectCommand Command { get; init; }
}

public sealed class RenameUser
{
    [StringLength(5)]
    public string? Name { get; init; }
}

public readonly record struct SetPriority(
    [property: Range(1, 10)] int Priority);

public union MultipleCommand(RenameUser, SetPriority);
""";

        await Verify(source, out var compilation);

        await VerifyEndpoint(compilation, "/direct", async (endpoint, serviceProvider) =>
        {
            var invalidContext = CreateHttpContextWithPayload("""{"Name":null}""", serviceProvider);

            await endpoint.RequestDelegate(invalidContext);

            var problemDetails = await AssertBadRequest(invalidContext);
            var error = Assert.Single(problemDetails.Errors);
            Assert.Equal("Value.Name", error.Key);
            Assert.Equal("The Name field is required.", Assert.Single(error.Value));

            var validContext = CreateHttpContextWithPayload("""{"Name":"Ada"}""", serviceProvider);

            await endpoint.RequestDelegate(validContext);

            Assert.Equal(StatusCodes.Status201Created, validContext.Response.StatusCode);

            var primitiveContext = CreateHttpContextWithPayload("true", serviceProvider);

            await endpoint.RequestDelegate(primitiveContext);

            Assert.Equal(StatusCodes.Status201Created, primitiveContext.Response.StatusCode);
        });

        await VerifyEndpoint(compilation, "/nested", async (endpoint, serviceProvider) =>
        {
            var invalidContext = CreateHttpContextWithPayload("""{"Command":{"Name":null}}""", serviceProvider);

            await endpoint.RequestDelegate(invalidContext);

            var problemDetails = await AssertBadRequest(invalidContext);
            var error = Assert.Single(problemDetails.Errors);
            Assert.Equal("Command.Value.Name", error.Key);
            Assert.Equal("The Name field is required.", Assert.Single(error.Value));

            var validContext = CreateHttpContextWithPayload("""{"Command":{"Name":"Ada"}}""", serviceProvider);

            await endpoint.RequestDelegate(validContext);

            Assert.Equal(StatusCodes.Status200OK, validContext.Response.StatusCode);
        });

        await VerifyValidatableType(compilation, "MultipleCommand", async (validationOptions, unionType) =>
        {
            Assert.True(validationOptions.TryGetValidatableTypeInfo(unionType, out var validatableTypeInfo));

            var renameType = unionType.Assembly.GetType("RenameUser")!;
            var invalidRename = Activator.CreateInstance(renameType)!;
            renameType.GetProperty("Name")!.SetValue(invalidRename, "Too long");
            var invalidRenameUnion = Activator.CreateInstance(unionType, invalidRename)!;
            var renameContext = new ValidateContext
            {
                ValidationOptions = validationOptions,
            };

            await ValidateAsync(validatableTypeInfo, invalidRenameUnion, renameContext, useAsync: true, default);

            var renameError = Assert.Single(renameContext.ValidationErrors!);
            Assert.Equal("Value.Name", renameError.Key);
            Assert.Equal("The field Name must be a string with a maximum length of 5.", Assert.Single(renameError.Value).ErrorMessage);

            var priorityType = unionType.Assembly.GetType("SetPriority")!;
            var invalidPriority = Activator.CreateInstance(priorityType, 0)!;
            var invalidPriorityUnion = Activator.CreateInstance(unionType, invalidPriority)!;
            var priorityContext = new ValidateContext
            {
                ValidationOptions = validationOptions,
            };

            await ValidateAsync(validatableTypeInfo, invalidPriorityUnion, priorityContext, useAsync: true, default);

            var priorityError = Assert.Single(priorityContext.ValidationErrors!);
            Assert.Equal("Value.Priority", priorityError.Key);
            Assert.Equal("The field Priority must be between 1 and 10.", Assert.Single(priorityError.Value).ErrorMessage);
        });
    }
}
