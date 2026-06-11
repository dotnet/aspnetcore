// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        Assert.True(path.Operations.TryGetValue(method, out var operation));
        return operation;
    }

    public static string GetSchemaId(IOpenApiSchema schema)
    {
        return schema switch
        {
            OpenApiSchemaReference reference => reference.Reference.Id,
            OpenApiSchema openApiSchema => openApiSchema.Id,
            _ => null,
        };
    }

    public static IOpenApiSchema ResolveSchema(IOpenApiSchema schema)
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

        if (schemaId is not null && document.Components.Schemas.TryGetValue(schemaId, out var resolvedSchema))
        {
            return resolvedSchema;
        }

        return ResolveSchema(schema);
    }

    public static IOpenApiSchema ResolveSchema(SchemaRepository repository, IOpenApiSchema schema)
    {
        var schemaId = GetSchemaId(schema);

        if (schemaId is not null && repository.Schemas.TryGetValue(schemaId, out var resolvedSchema))
        {
            return resolvedSchema;
        }

        return ResolveSchema(schema);
    }

    public static void AssertSchemaType(JsonSchemaType expected, IOpenApiSchema schema)
    {
        Assert.Equal(expected, schema.Type);
    }
}
