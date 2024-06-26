// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

public class SchemaTransformerTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task SchemaTransformer_CanAccessTypeAndParameterDescriptionForParameter()
    {
        var builder = CreateBuilder();

        builder.MapPost("/todo", (Todo todo) => { });

        var options = new OpenApiOptions();
        options.UseSchemaTransformer((schema, context, cancellationToken) =>
        {
            Assert.Equal(typeof(Todo), context.Type);
            Assert.Equal("todo", context.ParameterDescription.Name);
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document => { });
    }

    [Fact]
    public async Task SchemaTransformer_CanAccessTypeForResponse()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        options.UseSchemaTransformer((schema, context, cancellationToken) =>
        {
            Assert.Equal(typeof(Todo), context.Type);
            Assert.Null(context.ParameterDescription);
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document => { });
    }

    [Fact]
    public async Task SchemaTransformer_CanAccessApplicationServicesAndDocumentName()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        options.UseSchemaTransformer((schema, context, cancellationToken) =>
        {
            var service = context.ApplicationServices.GetKeyedService<OpenApiDocumentService>(context.DocumentName);
            Assert.NotNull(service);
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document => { });
    }

    [Fact]
    public async Task SchemaTransformer_RespectsCancellationToken()
    {
        var builder = CreateBuilder();

        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var options = new OpenApiOptions();
        options.UseSchemaTransformer((schema, context, cancellationToken) =>
        {
            Assert.Equal(cts.Token, cancellationToken);
            Assert.True(cancellationToken.IsCancellationRequested);
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document => { }, cts.Token);
    }

    [Fact]
    public async Task SchemaTransformer_RunsInRegisteredOrder()
    {
        var builder = CreateBuilder();

        builder.MapPost("/todo", (Todo todo) => { });

        var options = new OpenApiOptions();
        options.UseSchemaTransformer((schema, context, cancellationToken) =>
        {
            schema.Extensions["x-my-extension"] = new OpenApiString("1");
            return Task.CompletedTask;
        });
        options.UseSchemaTransformer((schema, context, cancellationToken) =>
        {
            Assert.Equal("1", ((OpenApiString)schema.Extensions["x-my-extension"]).Value);
            schema.Extensions["x-my-extension"] = new OpenApiString("2");
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var operation = Assert.Single(document.Paths.Values).Operations.Values.Single();
            var schema = operation.RequestBody.Content["application/json"].Schema;
            Assert.Equal("2", ((OpenApiString)schema.Extensions["x-my-extension"]).Value);
        });
    }

    [Fact]
    public async Task SchemaTransformer_OnTypeModifiesBothRequestAndResponse()
    {
        var builder = CreateBuilder();

        builder.MapPost("/todo", (Todo todo) => { });
        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        options.UseSchemaTransformer((schema, context, cancellationToken) =>
        {
            if (context.Type == typeof(Todo))
            {
                schema.Extensions["x-my-extension"] = new OpenApiString("1");
            }
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            var postOperation = path.Operations[OperationType.Post];
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema.GetEffective(document);
            Assert.Equal("1", ((OpenApiString)requestSchema.Extensions["x-my-extension"]).Value);
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema.GetEffective(document);
            Assert.Equal("1", ((OpenApiString)responseSchema.Extensions["x-my-extension"]).Value);
        });
    }

    [Fact]
    public async Task SchemaTransformer_WithDescriptionOnlyModifiesParameter()
    {
        var builder = CreateBuilder();

        builder.MapPost("/todo", (Todo todo) => { });
        builder.MapGet("/todo", () => new Todo(1, "Item1", false, DateTime.Now));

        var options = new OpenApiOptions();
        options.UseSchemaTransformer((schema, context, cancellationToken) =>
        {
            if (context.Type == typeof(Todo) && context.ParameterDescription is not null)
            {
                schema.Extensions["x-my-extension"] = new OpenApiString(context.ParameterDescription.Name);
            }
            return Task.CompletedTask;
        });

        await VerifyOpenApiDocument(builder, options, document =>
        {
            var path = Assert.Single(document.Paths.Values);
            var postOperation = path.Operations[OperationType.Post];
            var requestSchema = postOperation.RequestBody.Content["application/json"].Schema;
            Assert.Equal("todo", ((OpenApiString)requestSchema.Extensions["x-my-extension"]).Value);
            var getOperation = path.Operations[OperationType.Get];
            var responseSchema = getOperation.Responses["200"].Content["application/json"].Schema;
            Assert.False(responseSchema.Extensions.TryGetValue("x-my-extension", out var _));
        });
    }
}
