// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace GetDocumentSample;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddOpenApi();
        builder.Services.AddOpenApi("internal");

        var app = builder.Build();

        app.MapOpenApi();

        app.MapGet("/hello/{name}", (string name) => $"Hello {name}!");
        app.MapGet("/bye/{name}", (string name) => $"Bye {name}!")
            .WithGroupName("internal");

        app.Run();
    }
}
