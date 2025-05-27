// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    // if not present, will throw similar exception:
    //   Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException: Request body too large. The max request body size is 30000000 bytes.
    options.Limits.MaxRequestBodySize = 6L * 1024 * 1024 * 1024; // 6 GB

    // optional: timeout settings
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10L * 1024 * 1024 * 1024; // 10 GB
});

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
builder.Services.AddControllers();
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

var app = builder.Build();
app.Logger.LogInformation($"Current process ID: {Environment.ProcessId}");

app.MapGet("/plaintext", () => "Hello, World!");

// curl -X POST -F "file=@D:\.other\big-files\bigfile.dat" http://localhost:5000/upload-cts
app.MapPost("/upload-cts", (HttpRequest request, CancellationToken cancellationToken) =>
{
    // 1. endpoint handler
    // 2. form feature initialization
    // 3. calling `Request.Form.Files.First()`
    // 4. calling `FormFeature.InnerReadFormAsync()`

    if (!request.HasFormContentType)
    {
        return Results.BadRequest("The request does not contain a valid form.");
    }

    var file = request.Form.Files.First();
    return Results.Ok($"File '{file.Name}' uploaded.");
});

// curl -X POST -F "file=@D:\.other\big-files\bigfile.dat" http://localhost:5000/upload
app.MapPost("/upload", (HttpRequest request) =>
{
    // 1. endpoint handler
    // 2. form feature initialization
    // 3. calling `Request.Form.Files.First()`
    // 4. calling `FormFeature.InnerReadFormAsync()`

    if (!request.HasFormContentType)
    {
        return Results.BadRequest("The request does not contain a valid form.");
    }

    var file = request.Form.Files.First();
    return Results.Ok($"File '{file.Name}' uploaded.");
});

app.MapControllers();

app.Run();
