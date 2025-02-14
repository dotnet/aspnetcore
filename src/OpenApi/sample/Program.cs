// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
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

builder.Services.AddOpenApi("v1", options =>
{
    options.AddHeader("X-Version", "1.0");
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});
builder.Services.AddOpenApi("v2", options => {
    options.AddSchemaTransformer<AddExternalDocsTransformer>();
    options.AddOperationTransformer<AddExternalDocsTransformer>();
    options.AddDocumentTransformer(new AddContactTransformer());
    options.AddDocumentTransformer((document, context, token) => {
        document.Info.License = new OpenApiLicense { Name = "MIT" };
        return Task.CompletedTask;
    });
});
builder.Services.AddOpenApi("controllers");
builder.Services.AddOpenApi("responses");
builder.Services.AddOpenApi("forms");
builder.Services.AddOpenApi("schemas-by-ref");
builder.Services.AddOpenApi("xml");

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

app.MapControllers();

app.Run();

// Make Program class public to support snapshot testing
// against sample app using WebApplicationFactory.
public partial class Program { }
