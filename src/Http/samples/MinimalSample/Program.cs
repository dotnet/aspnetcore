using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

var app = WebApplication.Create(args);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

string Plaintext() => "Hello, World!";
app.MapGet("/plaintext", (Func<string>)Plaintext);

object Json() => new { message = "Hello, World!" };
app.MapGet("/json", (Func<object>)Json);

string SayHello(string name) => $"Hello, {name}!";
app.MapGet("/hello/{name}", (Func<string, string>)SayHello);

app.Run();
