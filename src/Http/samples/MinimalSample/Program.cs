using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

await using var app = WebApplication.Create();

Todo EchoTodo([FromBody] Todo todo) => todo;
app.MapPost("/EchoTodo", (Func<Todo, Todo>)EchoTodo);

string Plaintext() => "Hello, World!";
app.MapGet("/plaintext", (Func<string>)Plaintext);

object Json() => new { message = "Hello, World!" };
app.MapGet("/json", (Func<object>)Json);

string SayHello(string name) => $"Hello {name}";
app.MapGet("/hello/{name}", (Func<string, string>)SayHello);

await app.RunAsync();

record Todo(int Id, string Name, bool IsComplete);
