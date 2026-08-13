// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace GetDocumentSample;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                return TransformDocument(document, builder);
            });
        });

        builder.Services.AddOpenApi("internal", options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                return TransformDocument(document, builder);
            });
        });

        var app = builder.Build();

        app.MapOpenApi();

        app.MapGet("/hello/{name}", (string name) => $"Hello {name}!");
        app.MapGet("/bye/{name}", (string name) => $"Bye {name}!")
            .WithGroupName("internal");

        app.Run();
    }

    private static Task TransformDocument(
        Microsoft.OpenApi.OpenApiDocument document,
        WebApplicationBuilder builder)
    {
        var env = builder.Environment.EnvironmentName;
        if (!string.IsNullOrEmpty(env))
        {
            document.Info.Summary += $"Running in '{env}' environment";
        }
        return Task.CompletedTask;
    }
}
