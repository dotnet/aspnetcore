// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Sample.Transformers;

var builder = WebApplication.CreateBuilder(args);

#pragma warning disable IL2026 // MVC isn't trim-friendly yet
builder.Services.AddControllers();
#pragma warning restore IL2026
builder.Services.AddAuthentication().AddJwtBearer();

// Supports representing integer formats as strictly numerically values
// inside the schema.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});

builder.Services.AddOpenApiCore();
builder.Services.AddSingleton<IAdditionalOpenApiDocumentNameResolver, AdditionalDocumentNamesResolver>();

builder.Services.AddOpenApi("v1", options =>
{
    options.AddHeader("X-Version", "1.0");
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});
builder.Services.AddOpenApi("v2", options =>
{
    options.AddSchemaTransformer<AddExternalDocsTransformer>();
    options.AddOperationTransformer<AddExternalDocsTransformer>();
    options.AddDocumentTransformer(new AddContactTransformer());
    options.AddDocumentTransformer((document, context, token) =>
    {
        document.Info.License = new OpenApiLicense { Name = "MIT" };
        return Task.CompletedTask;
    });
});

builder.Services.AddOpenApi("controllers");
builder.Services.AddOpenApi("responses");
builder.Services.AddOpenApi("forms");
builder.Services.AddOpenApi("schemas-by-ref");
builder.Services.AddOpenApi("xml");
builder.Services.AddOpenApi("unions");
builder.Services.AddOpenApi("enum-pascalcase-nonnullable-param");
builder.Services.AddOpenApi("enum-pascalcase-nullable-param");
builder.Services.AddOpenApi("enum-camelcase-nonnullable-param");
builder.Services.AddOpenApi("enum-camelcase-nullable-param");
builder.Services.AddOpenApi("enum-pascalcase-nonnullable-body-model");
builder.Services.AddOpenApi("enum-pascalcase-nullable-body-model");
builder.Services.AddOpenApi("enum-camelcase-nonnullable-body-model");
builder.Services.AddOpenApi("enum-camelcase-nullable-body-model");
builder.Services.AddOpenApi("enum-pascalcase-nonnullable-body-direct");
builder.Services.AddOpenApi("enum-pascalcase-nullable-body-direct");
builder.Services.AddOpenApi("enum-camelcase-nonnullable-body-direct");
builder.Services.AddOpenApi("enum-camelcase-nullable-body-direct");
builder.Services.AddOpenApi("localized", options =>
{
    options.ShouldInclude = _ => true;
    options.AddDocumentTransformer((document, context, token) =>
    {
        document.Info.Description = $"This is a localized OpenAPI document for {CultureInfo.CurrentUICulture.NativeName}.";
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.MapOpenApi();
app.MapOpenApi("/openapi/{documentName}.yaml");
if (app.Environment.IsDevelopment())
{
    app.MapSwaggerUi();
}

app.MapFormEndpoints();
app.MapV1Endpoints();
app.MapV2Endpoints();
app.MapXmlEndpoints();
app.MapSchemasEndpoints();
app.MapResponseEndpoints();
app.MapUnionsEndpoints();
app.MapEnumsEndpoints();

app.MapGet("/first-doc/get1", () => "Hello, world").WithGroupName("first-doc");
app.MapGet("/first-doc/get2", () => "Hello, world").WithGroupName("first-doc");
app.MapGet("/second-doc/get1", () => "Hello, world").WithGroupName("second-doc");
app.MapGet("/second-doc/get2", () => "Hello, world").WithGroupName("second-doc");

app.MapControllers();

app.Run();

// Make Program class public to support snapshot testing
// against sample app using WebApplicationFactory.
public partial class Program { }

internal sealed class AdditionalDocumentNamesResolver : IAdditionalOpenApiDocumentNameResolver
{
    public IEnumerable<string> ResolveDocumentNames()
        => ["first-doc", "second-doc"];
}
