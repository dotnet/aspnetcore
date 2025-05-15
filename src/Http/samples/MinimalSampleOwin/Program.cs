// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Owin;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Logger.LogInformation($"Current process ID: {Environment.ProcessId}");

#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
string Plaintext() => "Hello, World!";
app.MapGet("/plaintext", Plaintext);

app.MapGet("/", () => $"""
    Operating System: {Environment.OSVersion}
    .NET version: {Environment.Version}
    Username: {Environment.UserName}
    Date and Time: {DateTime.Now}
    """);
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.

app.UseOwin(pipeline =>
{
    pipeline(next =>
    {
        return async environment =>
        {
            // if you want to get OWIN environment properties
            //if (environment is OwinEnvironment owin)
            //{
            //    foreach (var prop in owin)
            //    {
            //        app.Logger.LogInformation($"{prop.Key} - {prop.Value}");
            //    }
            //}

            await next(environment);
        };
    });
});

app.Run();
