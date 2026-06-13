// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Grpc.Swagger.Tests.Infrastructure;

internal static class OpenApiTestHelpers
{
    public static OpenApiDocument GetOpenApiDocument<TService>(ITestOutputHelper testOutputHelper) where TService : class
    {
        var services = new ServiceCollection();
        services.AddGrpcSwagger();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

            var filePath = Path.Combine(System.AppContext.BaseDirectory, "Microsoft.AspNetCore.Grpc.Swagger.Tests.xml");
            c.IncludeXmlComments(filePath);
            c.IncludeGrpcXmlComments(filePath, includeControllerXmlComments: true);
        });
        services.AddRouting();
        services.AddLogging();
        services.AddSingleton<IWebHostEnvironment, TestWebHostEnvironment>();
        var serviceProvider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(serviceProvider);

        app.UseRouting();
        app.UseEndpoints(c =>
        {
            c.MapGrpcService<TService>();
        });

        var swaggerGenerator = serviceProvider.GetRequiredService<ISwaggerProvider>();
        var swagger = swaggerGenerator.GetSwagger("v1");

        using var outputString = new StringWriter();
        swagger.SerializeAsV3(new OpenApiJsonWriter(outputString));
        testOutputHelper.WriteLine(outputString.ToString());

        return swagger;
    }

    public static OpenApiOperation GetOperation(IOpenApiPathItem path, HttpMethod method)
    {
        var operations = Assert.IsAssignableFrom<IDictionary<HttpMethod, OpenApiOperation>>(path.Operations);
        Assert.True(operations.TryGetValue(method, out var operation));
        return Assert.IsType<OpenApiOperation>(operation);
    }

    public static string? GetReferenceId(IOpenApiSchema schema)
    {
        return schema switch
        {
            OpenApiSchemaReference reference => reference.Reference.Id,
            OpenApiSchema => null,
            _ => throw new InvalidOperationException($"Unable to get a schema id for schema type '{schema.GetType().FullName}'."),
        };
    }

    public static string? GetSchemaId(IOpenApiSchema schema)
    {
        return schema switch
        {
            OpenApiSchemaReference => GetReferenceId(schema),
            OpenApiSchema openApiSchema => openApiSchema.Id,
            _ => throw new InvalidOperationException($"Unable to get a schema id for schema type '{schema.GetType().FullName}'."),
        };
    }

    private static IOpenApiSchema GetSchemaOrReference(IOpenApiSchema schema)
    {
        return schema switch
        {
            OpenApiSchemaReference reference => reference.RecursiveTarget is not null ? reference.RecursiveTarget : reference,
            OpenApiSchema openApiSchema => openApiSchema,
            _ => throw new InvalidOperationException($"Unable to resolve schema type '{schema.GetType().FullName}'."),
        };
    }

    public static IOpenApiSchema ResolveSchema(OpenApiDocument document, IOpenApiSchema schema)
    {
        var schemaId = GetSchemaId(schema);

        var schemas = document.Components?.Schemas;

        if (schemaId is not null && schemas is not null && schemas.TryGetValue(schemaId, out var resolvedSchema))
        {
            return Assert.IsAssignableFrom<IOpenApiSchema>(resolvedSchema);
        }

        return GetSchemaOrReference(schema);
    }

    public static IOpenApiSchema ResolveSchema(SchemaRepository repository, IOpenApiSchema schema)
    {
        var schemaId = GetSchemaId(schema);

        if (schemaId is not null && repository.Schemas.TryGetValue(schemaId, out var resolvedSchema))
        {
            return resolvedSchema;
        }

        return GetSchemaOrReference(schema);
    }

    public static void AssertSchemaType(JsonSchemaType expected, IOpenApiSchema schema)
    {
        Assert.Equal(expected, schema.Type);
    }
}
