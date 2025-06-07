// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;
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

var versions = new[]
{
    OpenApiSpecVersion.OpenApi3_0,
    OpenApiSpecVersion.OpenApi3_1,
};

var documentNames = new[]
{
    "controllers",
    "responses",
    "forms",
    "schemas-by-ref",
    "xml",
};

foreach (var version in versions)
{
    builder.Services.AddOpenApi($"v1-{version}", options =>
    {
        options.OpenApiVersion = version;
        options.ShouldInclude = (description) => description.GroupName == null || description.GroupName == "v1";
        options.AddHeader("X-Version", "1.0");
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    });
    builder.Services.AddOpenApi($"v2-{version}", options =>
    {
        options.OpenApiVersion = version;
        options.ShouldInclude = (description) => description.GroupName == null || description.GroupName == "v2";
        options.AddSchemaTransformer<AddExternalDocsTransformer>();
        options.AddOperationTransformer<AddExternalDocsTransformer>();
        options.AddDocumentTransformer(new AddContactTransformer());
        options.AddDocumentTransformer((document, context, token) =>
        {
            document.Info.License = new OpenApiLicense { Name = "MIT" };
            return Task.CompletedTask;
        });
    });

    foreach (var name in documentNames)
    {
        builder.Services.AddOpenApi($"{name}-{version}", options =>
        {
            options.OpenApiVersion = version;
            options.ShouldInclude = (description) => description.GroupName == null || description.GroupName == name;
        });
    }
}

var app = builder.Build();

// Run requests with a culture that uses commas to format decimals to
// verify the invariant culture is used to generate the OpenAPI document.
app.Use((next) =>
{
    return async context =>
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;

        var newCulture = new CultureInfo("fr-FR");

        try
        {
            CultureInfo.CurrentCulture = newCulture;
            CultureInfo.CurrentUICulture = newCulture;

            await next(context);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    };
});

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
