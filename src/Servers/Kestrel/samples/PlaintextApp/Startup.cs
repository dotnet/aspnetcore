// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = WebApplication.CreateBuilder();

var app = builder.Build();

var payload = "Hello, World!"u8.ToArray();

app.Run(context =>
{
    return Task.RunAsGreenThread(() =>
    {
        var response = context.Response;

        response.StatusCode = 200;
        response.ContentType = "text/plain";
        response.ContentLength = payload.Length;

        // This is async IO under the covers!
        response.Body.Write(payload);
    });
});

app.Run();
