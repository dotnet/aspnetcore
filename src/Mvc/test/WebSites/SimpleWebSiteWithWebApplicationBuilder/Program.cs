// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

using static Microsoft.AspNetCore.Http.Results;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddAntiforgery();

var app = builder.Build();

// just to make sure that it does not cause exceptions
app.Urls.Add("http://localhost:8080");

app.UseAntiforgery();

app.MapControllers();

app.MapGet("/", () => "Hello World");

app.MapGet("/json", () => Json(new Person("John", 42)));

app.MapGet("/ok-object", () => Ok(new Person("John", 42)));

app.MapGet("/accepted-object", () => Accepted("/ok-object", new Person("John", 42)));

app.MapGet("/many-results", (int id) =>
{
    if (id == -1)
    {
        return NotFound();
    }

    return Redirect("/json", permanent: true);
});

app.MapGet("/problem", () => Results.Problem("Some problem"));

app.MapGet("/environment", (IHostEnvironment environment) => environment.EnvironmentName);
app.MapGet("/webroot", (IWebHostEnvironment environment) => environment.WebRootPath);

app.MapGet("/greeting", (IConfiguration config) => config["Greeting"]);

app.MapPost("/accepts-default", (Person person) => Results.Ok(person.Name));
app.MapPost("/accepts-xml", () => Accepted()).Accepts<Person>("application/xml");

app.MapPost("/fileupload", async (IFormFile file) =>
{
    await using var uploadStream = file.OpenReadStream();
    return uploadStream.Length;
});

app.Run();

record Person(string Name, int Age);

public class MyController : ControllerBase
{
    [HttpGet("/greet")]
    public string Greet() => $"Hello human";
}

namespace SimpleWebSiteWithWebApplicationBuilder
{
    public partial class Program
    {
    }
}
