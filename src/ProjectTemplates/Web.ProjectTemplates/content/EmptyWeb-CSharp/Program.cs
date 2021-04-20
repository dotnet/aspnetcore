using System;
using Microsoft.AspNetCore.Builder;

await using var app = WebApplication.Create(args);

app.MapGet("/", (Func<string>)(() => "Hello World!"));

await app.RunAsync();
