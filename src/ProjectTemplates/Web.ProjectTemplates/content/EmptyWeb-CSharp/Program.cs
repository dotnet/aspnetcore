using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

await using var app = WebApplication.Create(args);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapGet("/", (Func<string>)(() => "Hello World!"));

await app.RunAsync();
