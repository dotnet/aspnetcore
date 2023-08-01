// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAntiforgery();

var app = builder.Build();

app.UseAntiforgery();

app.MapGet("/antiforgery", (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context);
    var html = $"""
        <html>
            <body>
                <form action="/todo" method="POST" enctype="multipart/form-data">
                    <input name="{token.FormFieldName}" type="hidden" value="{token.RequestToken}" />
                    <input type="text" name="name" />
                    <input type="date" name="dueDate" />
                    <input type="checkbox" name="isCompleted" />
                    <input type="submit" />
                </form>
            </body>
        </html>
    """;
    return Results.Content(html, "text/html");
});

app.MapGet("/no-antiforgery", () =>
{
    var html = """
        <html>
            <body>
                <form action="/todo" method="POST" enctype="multipart/form-data">
                    <input type="text" name="name" />
                    <input type="date" name="dueDate" />
                    <input type="checkbox" name="isCompleted" />
                    <input type="submit" />
                </form>
            </body>
        </html>
    """;
    return Results.Content(html, "text/html");
});

app.MapPost("/todo", [ValidateAntiForgeryToken] ([FromForm] Todo todo) => Results.Ok(todo));

app.MapPost("/todo-raw", async context =>
{
    var form = await context.Request.ReadFormAsync();
    var name = form["name"].ToString();
    var dueDate = DateTime.Parse(form["dueDate"].ToString(), CultureInfo.InvariantCulture);
    var isCompleted = bool.Parse(form["isCompleted"].ToString());
    var result = Results.Ok(new Todo(name, isCompleted, dueDate));
    await result.ExecuteAsync(context);
}).WithMetadata(new AntiforgeryMetadata(true));

app.Run();

class Todo(string name, bool isCompleted, DateTime dueDate)
{
    public string Name { get; set; } = name;
    public bool IsCompleted { get; set; } = isCompleted;
    public DateTime DueDate { get; set; } = dueDate;
}

class AntiforgeryMetadata: IAntiforgeryMetadata
{
    public AntiforgeryMetadata(bool required)
    {
        RequiresValidation = required;
    }

    public bool RequiresValidation { get; }
}
