// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

using static Microsoft.Extensions.Validation.Tests.ValidationTestBase;

namespace Microsoft.Extensions.Validation.GeneratorTests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanSkipValidationForUnionCase(bool useAsync)
    {
        var source = """
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Validation;

var builder = WebApplication.CreateBuilder();
builder.Services.AddValidation();

var app = builder.Build();
app.MapPost("/", (Command command) => Results.Ok());
app.Run();

[SkipValidation]
public sealed class IgnoredCase
{
    [Range(1, 10)]
    public int Count { get; init; }
}

public sealed class CheckedCase
{
    [Range(1, 10)]
    public int Count { get; init; }
}

[JsonUnion(TypeClassifier = typeof(CommandClassifierFactory))]
public union Command(IgnoredCase, CheckedCase);

public sealed class CommandClassifierFactory : JsonTypeClassifierFactory<Command>
{
    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options)
        => static (ref Utf8JsonReader reader) =>
        {
            var clone = reader;
            clone.Read();
            return clone.ValueTextEquals("ignored") ? typeof(IgnoredCase) : typeof(CheckedCase);
        };
}
""";

        await Verify(source, out var compilation);

        await VerifyValidatableType(compilation, "Command", async (validationOptions, unionType) =>
        {
            Assert.True(validationOptions.TryGetValidatableTypeInfo(unionType, out var validatableTypeInfo));

            var checkedType = unionType.Assembly.GetType("CheckedCase")!;
            var checkedValue = Activator.CreateInstance(checkedType)!;
            var checkedUnion = Activator.CreateInstance(unionType, checkedValue)!;
            var checkedContext = new ValidateContext { ValidationOptions = validationOptions };

            await ValidateAsync(validatableTypeInfo, checkedUnion, checkedContext, useAsync, default);

            var error = Assert.Single(checkedContext.ValidationErrors!);
            Assert.Equal("Value.Count", error.Key);
            Assert.Equal("The field Count must be between 1 and 10.", Assert.Single(error.Value).ErrorMessage);

            checkedType.GetProperty("Count")!.SetValue(checkedValue, 1);
            var validContext = new ValidateContext { ValidationOptions = validationOptions };

            await ValidateAsync(validatableTypeInfo, checkedUnion, validContext, useAsync, default);

            Assert.Null(validContext.ValidationErrors);

            var ignoredType = unionType.Assembly.GetType("IgnoredCase")!;
            var ignoredValue = Activator.CreateInstance(ignoredType)!;
            var ignoredUnion = Activator.CreateInstance(unionType, ignoredValue)!;
            var ignoredContext = new ValidateContext { ValidationOptions = validationOptions };

            await ValidateAsync(validatableTypeInfo, ignoredUnion, ignoredContext, useAsync, default);

            Assert.Null(ignoredContext.ValidationErrors);
        });

        await VerifyEndpoint(compilation, "/", async (endpoint, serviceProvider) =>
        {
            var context = CreateHttpContextWithPayload("""{"ignored":true,"Count":0}""", serviceProvider);

            await endpoint.RequestDelegate(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

            var invalidContext = CreateHttpContextWithPayload("""{"Count":0}""", serviceProvider);

            await endpoint.RequestDelegate(invalidContext);

            var problemDetails = await AssertBadRequest(invalidContext);
            var error = Assert.Single(problemDetails.Errors);
            Assert.Equal("Value.Count", error.Key);
            Assert.Equal("The field Count must be between 1 and 10.", Assert.Single(error.Value));

            var validContext = CreateHttpContextWithPayload("""{"Count":1}""", serviceProvider);

            await endpoint.RequestDelegate(validContext);

            Assert.Equal(StatusCodes.Status200OK, validContext.Response.StatusCode);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanValidateUnionCollectionCases(bool useAsync)
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
app.MapPost("/", (Batch batch) => Results.StatusCode(201));
app.Run();

public sealed class CheckedCase
{
    [Range(1, 10)]
    public int Count { get; init; }
}

public union Batch(CheckedCase[], bool);
""";

        await Verify(source, out var compilation);

        await VerifyValidatableType(compilation, "Batch", async (validationOptions, unionType) =>
        {
            Assert.True(validationOptions.TryGetValidatableTypeInfo(unionType, out var validatableTypeInfo));

            var primitiveUnion = Activator.CreateInstance(unionType, [true])!;
            Assert.True(Assert.IsType<bool>(unionType.GetProperty("Value")!.GetValue(primitiveUnion)));
            var primitiveContext = new ValidateContext { ValidationOptions = validationOptions };

            await ValidateAsync(validatableTypeInfo, primitiveUnion, primitiveContext, useAsync, default);

            Assert.Null(primitiveContext.ValidationErrors);

            var checkedType = unionType.Assembly.GetType("CheckedCase")!;
            var emptyUnion = Activator.CreateInstance(unionType, [Array.CreateInstance(checkedType, 0)])!;
            var emptyContext = new ValidateContext { ValidationOptions = validationOptions };

            await ValidateAsync(validatableTypeInfo, emptyUnion, emptyContext, useAsync, default);

            Assert.Null(emptyContext.ValidationErrors);

            var checkedValue = Activator.CreateInstance(checkedType)!;
            checkedType.GetProperty("Count")!.SetValue(checkedValue, 10);
            var array = Array.CreateInstance(checkedType, 2);
            array.SetValue(checkedValue, 0);
            var arrayUnion = Activator.CreateInstance(unionType, [array])!;
            var validContext = new ValidateContext { ValidationOptions = validationOptions };

            await ValidateAsync(validatableTypeInfo, arrayUnion, validContext, useAsync, default);

            Assert.Null(validContext.ValidationErrors);

            checkedType.GetProperty("Count")!.SetValue(checkedValue, 0);
            var invalidContext = new ValidateContext { ValidationOptions = validationOptions };

            await ValidateAsync(validatableTypeInfo, arrayUnion, invalidContext, useAsync, default);

            var error = Assert.Single(invalidContext.ValidationErrors!);
            Assert.Equal("Value[0].Count", error.Key);
            Assert.Equal("The field Count must be between 1 and 10.", Assert.Single(error.Value).ErrorMessage);
        });

        await VerifyEndpoint(compilation, "/", async (endpoint, serviceProvider) =>
        {
            var invalidContext = CreateHttpContextWithPayload("""[{"Count":0}]""", serviceProvider);

            await endpoint.RequestDelegate(invalidContext);

            var problemDetails = await AssertBadRequest(invalidContext);
            var error = Assert.Single(problemDetails.Errors);
            Assert.Equal("Value[0].Count", error.Key);
            Assert.Equal("The field Count must be between 1 and 10.", Assert.Single(error.Value));

            foreach (var payload in new[] { """[{"Count":10}]""", "[]", "true" })
            {
                var validContext = CreateHttpContextWithPayload(payload, serviceProvider);

                await endpoint.RequestDelegate(validContext);

                Assert.Equal(StatusCodes.Status201Created, validContext.Response.StatusCode);
            }
        });
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task CanSkipValidationForUnionCollections(bool useAsync, bool skipCollection)
    {
        var source = """
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Validation;

var builder = WebApplication.CreateBuilder();
builder.Services.AddValidation();
var app = builder.Build();
app.MapPost("/collection", (CollectionCommand command) => Results.StatusCode(201));
app.MapPost("/elements", (ElementsCommand command) => Results.StatusCode(201));
app.Run();

public sealed class CheckedCase
{
    [Range(1, 10)]
    public int Count { get; set; }
}

[SkipValidation]
public sealed class IgnoredCases : List<CheckedCase>
{
    public IgnoredCases()
    {
        Add(new CheckedCase());
    }
}

[SkipValidation]
public sealed class IgnoredCase
{
    [Range(1, 10)]
    public int Count { get; set; }
}

public union CollectionCommand(IgnoredCases, CheckedCase);
public union ElementsCommand(IgnoredCase[], CheckedCase);
""";

        await Verify(source, out var compilation);

        await VerifyValidatableType(compilation, skipCollection ? "CollectionCommand" : "ElementsCommand", async (validationOptions, unionType) =>
        {
            Assert.True(validationOptions.TryGetValidatableTypeInfo(unionType, out var validatableTypeInfo));
            var checkedType = unionType.Assembly.GetType("CheckedCase")!;
            var checkedValue = Activator.CreateInstance(checkedType)!;
            var checkedUnion = Activator.CreateInstance(unionType, [checkedValue])!;
            var checkedContext = new ValidateContext { ValidationOptions = validationOptions };

            await ValidateAsync(validatableTypeInfo, checkedUnion, checkedContext, useAsync, default);

            Assert.Equal("Value.Count", Assert.Single(checkedContext.ValidationErrors!).Key);

            checkedType.GetProperty("Count")!.SetValue(checkedValue, 1);
            var validContext = new ValidateContext { ValidationOptions = validationOptions };

            await ValidateAsync(validatableTypeInfo, checkedUnion, validContext, useAsync, default);

            Assert.Null(validContext.ValidationErrors);

            var ignoredType = unionType.Assembly.GetType(skipCollection ? "IgnoredCases" : "IgnoredCase")!;
            object ignoredValue;
            if (skipCollection)
            {
                ignoredValue = Activator.CreateInstance(ignoredType)!;
            }
            else
            {
                var array = Array.CreateInstance(ignoredType, 1);
                array.SetValue(Activator.CreateInstance(ignoredType), 0);
                ignoredValue = array;
            }

            var ignoredUnion = Activator.CreateInstance(unionType, [ignoredValue])!;
            var ignoredContext = new ValidateContext { ValidationOptions = validationOptions };

            await ValidateAsync(validatableTypeInfo, ignoredUnion, ignoredContext, useAsync, default);

            Assert.Null(ignoredContext.ValidationErrors);
            Assert.False(validationOptions.TryGetValidatableTypeInfo(ignoredType, out _));
        });

        await VerifyEndpoint(compilation, skipCollection ? "/collection" : "/elements", async (endpoint, serviceProvider) =>
        {
            var ignoredContext = CreateHttpContextWithPayload("""[{"Count":0}]""", serviceProvider);

            await endpoint.RequestDelegate(ignoredContext);

            Assert.Equal(StatusCodes.Status201Created, ignoredContext.Response.StatusCode);

            var checkedContext = CreateHttpContextWithPayload("""{"Count":0}""", serviceProvider);

            await endpoint.RequestDelegate(checkedContext);

            var problemDetails = await AssertBadRequest(checkedContext);
            Assert.Equal("Value.Count", Assert.Single(problemDetails.Errors).Key);
        });
    }

    [Theory]
    [InlineData(true, "Property")]
    [InlineData(false, "Property")]
    [InlineData(true, "Type")]
    [InlineData(false, "Type")]
    [InlineData(true, "Empty")]
    [InlineData(false, "Empty")]
    [InlineData(true, "Element")]
    [InlineData(false, "Element")]
    [InlineData(true, "Valid")]
    [InlineData(false, "Valid")]
    public async Task CanValidateUnionCollectionObject(bool useAsync, string scenario)
    {
        var source = """
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Validation;

var builder = WebApplication.CreateBuilder();
builder.Services.AddValidation();
var app = builder.Build();
app.MapPost("/", (Batch batch) => Results.StatusCode(201));
app.Run();

public sealed class CheckedCase
{
    [Range(1, 10)]
    public int Count { get; set; }
}

[CustomValidation(typeof(NonEmptyList), nameof(ValidateType))]
public sealed class NonEmptyList : List<CheckedCase>, IValidatableObject
{
    public static int ValidationCalls;

    [Range(1, 10)]
    public int Priority { get; set; } = 1;

    public bool IsAllowed { get; set; } = true;

    public static ValidationResult? ValidateType(NonEmptyList value)
        => value.IsAllowed ? ValidationResult.Success : new ValidationResult("The collection is not allowed.");

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        ValidationCalls++;
        if (Count == 0)
        {
            yield return new ValidationResult("The collection cannot be empty.", [nameof(Count)]);
        }
    }
}

public union Batch(NonEmptyList, bool);
""";

        await Verify(source, out var compilation);

        await VerifyValidatableType(compilation, "Batch", async (validationOptions, unionType) =>
        {
            Assert.True(validationOptions.TryGetValidatableTypeInfo(unionType, out var validatableTypeInfo));
            var collectionType = unionType.Assembly.GetType("NonEmptyList")!;
            var collection = Activator.CreateInstance(collectionType)!;
            if (scenario != "Empty")
            {
                var checkedType = unionType.Assembly.GetType("CheckedCase")!;
                var item = Activator.CreateInstance(checkedType)!;
                checkedType.GetProperty("Count")!.SetValue(item, scenario == "Element" ? 0 : 1);
                ((System.Collections.IList)collection).Add(item);
            }

            collectionType.GetProperty("Priority")!.SetValue(collection, scenario == "Property" ? 0 : 1);
            collectionType.GetProperty("IsAllowed")!.SetValue(collection, scenario != "Type");
            var union = Activator.CreateInstance(unionType, [collection])!;
            var context = new ValidateContext { ValidationOptions = validationOptions };

            await ValidateAsync(validatableTypeInfo, union, context, useAsync, default);

            if (scenario == "Valid")
            {
                Assert.Null(context.ValidationErrors);
            }
            else
            {
                var error = Assert.Single(context.ValidationErrors!);
                var (path, message) = scenario switch
                {
                    "Property" => ("Value.Priority", "The field Priority must be between 1 and 10."),
                    "Type" => ("Value", "The collection is not allowed."),
                    "Empty" => ("Value.Count", "The collection cannot be empty."),
                    "Element" => ("Value[0].Count", "The field Count must be between 1 and 10."),
                    _ => throw new InvalidOperationException($"Unexpected scenario '{scenario}'."),
                };
                Assert.Equal(path, error.Key);
                Assert.Equal(message, Assert.Single(error.Value).ErrorMessage);
            }

            Assert.Equal(scenario is "Property" or "Type" ? 0 : 1, collectionType.GetField("ValidationCalls")!.GetValue(null));
        });

        await VerifyEndpoint(compilation, "/", async (endpoint, serviceProvider) =>
        {
            var emptyContext = CreateHttpContextWithPayload("[]", serviceProvider);

            await endpoint.RequestDelegate(emptyContext);

            var problemDetails = await AssertBadRequest(emptyContext);
            var error = Assert.Single(problemDetails.Errors);
            Assert.Equal("Value.Count", error.Key);
            Assert.Equal("The collection cannot be empty.", Assert.Single(error.Value));

            var invalidContext = CreateHttpContextWithPayload("""[{"Count":0}]""", serviceProvider);

            await endpoint.RequestDelegate(invalidContext);

            var invalidProblemDetails = await AssertBadRequest(invalidContext);
            Assert.Equal("Value[0].Count", Assert.Single(invalidProblemDetails.Errors).Key);

            var validContext = CreateHttpContextWithPayload("""[{"Count":1}]""", serviceProvider);

            await endpoint.RequestDelegate(validContext);

            Assert.Equal(StatusCodes.Status201Created, validContext.Response.StatusCode);
        });
    }

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
