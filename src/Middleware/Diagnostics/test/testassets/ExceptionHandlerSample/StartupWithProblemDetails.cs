// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Metadata;

namespace ExceptionHandlerSample;

public class StartupWithProblemDetails
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddProblemDetails();
    }

    public void Configure(IApplicationBuilder app)
    {
        // Configure the error handler to produces a ProblemDetails.
        app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            StatusCodeSelector = ex => ex is ConflictException
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status500InternalServerError,
        });

        // The broken section of our application.
        app.Map("/throw", throwApp =>
        {
            throwApp.Map("/conflict", throwConflictApp =>
            {
                throwConflictApp.Run(_ => throw new ConflictException("Conflict Exception"));
            });

            throwApp.Run(_ => throw new Exception("Application Exception"));
        });

        app.UseStaticFiles();

        // The home page.
        app.Run(async context =>
        {
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync("<html><body>Welcome to the sample<br><br>\r\n");
            await context.Response.WriteAsync("Click here to throw an exception: <a href=\"/throw\">throw</a><br>\r\n");
            await context.Response.WriteAsync("Click here to throw a conflict exception: <a href=\"/throw/conflict\">throw conflict</a>\r\n");
            await context.Response.WriteAsync("</body></html>\r\n");
        });
    }
}

