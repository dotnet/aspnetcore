using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.Configure(app =>
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                Todo EchoTodo([FromBody] Todo todo) => todo;
                endpoints.MapPost("/EchoTodo", (Func<Todo, Todo>)EchoTodo);

                string Plaintext() => "Hello, World!";
                endpoints.MapGet("/plaintext", (Func<string>)Plaintext);

                object Json() => new { message = "Hello, World!" };
                endpoints.MapGet("/json", (Func<object>)Json);
            });

        });
    })
    .Build();

await host.RunAsync();

record Todo(int Id, string Name, bool IsComplete);
