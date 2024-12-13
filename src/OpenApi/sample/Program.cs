// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.ComponentModel;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Sample.Transformers;

var builder = WebApplication.CreateBuilder(args);

#pragma warning disable IL2026 // MVC isn't trim-friendly yet
builder.Services.AddControllers();
#pragma warning restore IL2026
builder.Services.AddAuthentication().AddJwtBearer();

builder.Services.AddOpenApi("v1", options =>
{
    options.AddHeader("X-Version", "1.0");
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});
builder.Services.AddOpenApi("v2", options => {
    options.AddSchemaTransformer<AddExternalDocsTransformer>();
    options.AddOperationTransformer<AddExternalDocsTransformer>();
    options.AddDocumentTransformer(new AddContactTransformer());
    options.AddDocumentTransformer((document, context, token) => {
        document.Info.License = new OpenApiLicense { Name = "MIT" };
        return Task.CompletedTask;
    });
});
builder.Services.AddOpenApi("controllers");
builder.Services.AddOpenApi("responses");
builder.Services.AddOpenApi("forms");
builder.Services.AddOpenApi("schemas-by-ref");

var app = builder.Build();

app.MapOpenApi();
app.MapOpenApi("/openapi/{documentName}.yaml");
if (app.Environment.IsDevelopment())
{
    app.MapSwaggerUi();
}

var forms = app.MapGroup("forms")
    .WithGroupName("forms");

var schemas = app.MapGroup("schemas-by-ref")
    .WithGroupName("schemas-by-ref");

if (app.Environment.IsDevelopment())
{
    forms.DisableAntiforgery();
}

forms.MapPost("/form-file", (IFormFile resume) => Results.Ok(resume.FileName));
forms.MapPost("/form-files", (IFormFileCollection files) => Results.Ok(files.Count));
forms.MapPost("/form-file-multiple", (IFormFile resume, IFormFileCollection files) => Results.Ok(files.Count + resume.FileName));
// Disable warnings because RDG does not support complex form binding yet.
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#pragma warning disable RDG003 // Unable to resolve parameter
forms.MapPost("/form-todo", ([FromForm] Todo todo) => Results.Ok(todo));
forms.MapPost("/forms-pocos-and-files", ([FromForm] Todo todo, IFormFile file) => Results.Ok(new { Todo = todo, File = file.FileName }));
#pragma warning restore RDG003 // Unable to resolve parameter
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

var v1 = app.MapGroup("v1")
    .WithGroupName("v1");
var v2 = app.MapGroup("v2")
    .WithGroupName("v2");
var responses = app.MapGroup("responses")
    .WithGroupName("responses");

v1.MapGet("/array-of-guids", (Guid[] guids) => guids);

v1.MapPost("/todos", (Todo todo) => Results.Created($"/todos/{todo.Id}", todo))
    .WithSummary("Creates a new todo item.");
v1.MapGet("/todos/{id}", (int id) => new TodoWithDueDate(1, "Test todo", false, DateTime.Now.AddDays(1), DateTime.Now))
    .WithDescription("Returns a specific todo item.");

v2.MapGet("/users", () => new [] { "alice", "bob" })
    .WithTags("users");

v2.MapPost("/users", () => Results.Created("/users/1", new { Id = 1, Name = "Test user" }))
  .WithName("CreateUser");

responses.MapGet("/200-add-xml", () => new TodoWithDueDate(1, "Test todo", false, DateTime.Now.AddDays(1), DateTime.Now))
    .Produces<Todo>(additionalContentTypes: "text/xml");

responses.MapGet("/200-only-xml", () => new TodoWithDueDate(1, "Test todo", false, DateTime.Now.AddDays(1), DateTime.Now))
    .Produces<Todo>(contentType: "text/xml");

responses.MapGet("/triangle", () => new Triangle { Color = "red", Sides = 3, Hypotenuse = 5.0 });
responses.MapGet("/shape", Shape () => new Triangle { Color = "blue", Sides = 4 });

schemas.MapGet("/typed-results", () => TypedResults.Ok(new Triangle { Color = "red", Sides = 3, Hypotenuse = 5.0 }));
schemas.MapGet("/multiple-results", Results<Ok<Triangle>, NotFound<string>> () => Random.Shared.Next(0, 2) == 0
    ? TypedResults.Ok(new Triangle { Color = "red", Sides = 3, Hypotenuse = 5.0 })
    : TypedResults.NotFound<string>("Item not found."));
schemas.MapGet("/iresult-no-produces", () => Results.Ok(new Triangle { Color = "red", Sides = 3, Hypotenuse = 5.0 }));
schemas.MapGet("/iresult-with-produces", () => Results.Ok(new Triangle { Color = "red", Sides = 3, Hypotenuse = 5.0 }))
    .Produces<Triangle>(200, "text/xml");
schemas.MapGet("/primitives", ([Description("The ID associated with the Todo item.")] int id, [Description("The number of Todos to fetch")] int size) => { });
schemas.MapGet("/product", (Product product) => TypedResults.Ok(product));
schemas.MapGet("/account", (Account account) => TypedResults.Ok(account));
schemas.MapPost("/array-of-ints", (int[] values) => values.Sum());
schemas.MapPost("/list-of-ints", (List<int> values) => values.Count);
schemas.MapPost("/ienumerable-of-ints", (IEnumerable<int> values) => values.Count());
schemas.MapGet("/dictionary-of-ints", () => new Dictionary<string, int> { { "one", 1 }, { "two", 2 } });
schemas.MapGet("/frozen-dictionary-of-ints", () => ImmutableDictionary.CreateRange(new Dictionary<string, int> { { "one", 1 }, { "two", 2 } }));
schemas.MapPost("/shape", (Shape shape) => { });
schemas.MapPost("/weatherforecastbase", (WeatherForecastBase forecast) => { });
schemas.MapPost("/person", (Person person) => { });

app.MapControllers();

app.Run();

// Make Program class public to support snapshot testing
// against sample app using WebApplicationFactory.
public partial class Program { }
