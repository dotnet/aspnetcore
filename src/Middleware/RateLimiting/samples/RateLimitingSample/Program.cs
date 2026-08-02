// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using RateLimitingSample;
using Microsoft.Extensions.Logging.Abstractions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
// Inject an ILogger<SampleRateLimiterPolicy>
builder.Services.AddLogging();

var todoName = "todoPolicy";
var completeName = "completePolicy";
var helloName = "helloPolicy";

builder.Services.AddRateLimiter(options =>
{
    // Define endpoint limiters and a global limiter.
    options.AddTokenBucketLimiter(todoName, options =>
    {
        options.TokenLimit = 1;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 1;
        options.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        options.TokensPerPeriod = 1;
    })
    .AddPolicy<string>(completeName, new SampleRateLimiterPolicy(NullLogger<SampleRateLimiterPolicy>.Instance))
    .AddPolicy<string, SampleRateLimiterPolicy>(helloName);
    // The global limiter will be a concurrency limiter with a max permit count of 10 and a queue depth of 5.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        return RateLimitPartition.GetConcurrencyLimiter<string>("globalLimiter", key => new ConcurrencyLimiterOptions
        {
            PermitLimit = 10,
            QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
            QueueLimit = 5
        });
    });
});

var app = builder.Build();

app.UseRateLimiter();

// The limiter on this endpoint allows 1 request every 5 seconds
app.MapGet("/", () => "Hello World!").RequireRateLimiting(helloName);

// Requests to this endpoint will be processed in 10 second intervals
app.MapGet("/todoitems", async (TodoDb db) =>
    await db.Todos.ToListAsync())
    .RequireRateLimiting(todoName);

// The limiter on this endpoint allows 1 request every 5 seconds
app.MapGet("/todoitems/complete", async (TodoDb db) =>
    await db.Todos.Where(t => t.IsComplete).ToListAsync())
    .RequireRateLimiting(completeName);

app.MapGet("/todoitems/{id}", async (int id, TodoDb db) =>
    await db.Todos.FindAsync(id)
        is Todo todo
            ? Results.Ok(todo)
            : Results.NotFound());

app.MapPost("/todoitems", async (Todo todo, TodoDb db) =>
{
    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    return Results.Created($"/todoitems/{todo.Id}", todo);
});

app.MapPut("/todoitems/{id}", async (int id, Todo inputTodo, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null)
    {
        return Results.NotFound();
    }

    todo.Name = inputTodo.Name;
    todo.IsComplete = inputTodo.IsComplete;

    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapDelete("/todoitems/{id}", async (int id, TodoDb db) =>
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return Results.Ok(todo);
    }

    return Results.NotFound();
});

app.Run();

class Todo
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
}

class TodoDb : DbContext
{
    public TodoDb(DbContextOptions<TodoDb> options)
        : base(options) { }

    public DbSet<Todo> Todos => Set<Todo>();
}
